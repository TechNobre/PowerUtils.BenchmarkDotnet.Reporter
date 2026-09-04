using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;

public sealed class GateHandler(
    Func<string?, List<BenchmarkReport>> readBenchmarks,
    IGateValidator validator,
    IServiceProvider provider)
{
    private readonly Func<string?, List<BenchmarkReport>> _readBenchmarks = readBenchmarks;
    private readonly IGateValidator _validator = validator;
    private readonly IServiceProvider _provider = provider;


    public int Execute(GateOptions options)
    {
        if(string.IsNullOrWhiteSpace(options.Input))
        {
            throw new DomainException("The input path is required. Set it via --input (-i), the PBREPORTER_GATE__INPUT environment variable, or the config file.");
        }

        var benchmarks = _readBenchmarks(options.Input);

        var report = new GateReport();
        foreach(var benchmark in benchmarks)
        {
            report.Results.Add(new GateReport.Result
            {
                Type = benchmark.Type,
                Name = benchmark.Method,
                FullName = benchmark.FullName,
                Mean = benchmark.Statistics?.Mean,
                Allocated = benchmark.Memory?.BytesAllocatedPerOperation
            });
        }

        var validationMessages = _validator.ValidateHostEnvironment(benchmarks);
        if(validationMessages?.Count > 0)
        {
            report.Warnings.AddRange(validationMessages);
        }

        _validator.EvaluateThresholds(
            report,
            options.MeanThreshold,
            options.AllocationThreshold);

        foreach(var format in options.Formats)
        {
            // Generate the final report using the specified exporter
            _provider
                .GetRequiredKeyedService<IExporter>(format.ToLowerInvariant())
                .Generate(report, options.Output);
        }

        // Checking against absolute thresholds is this command's entire purpose, so a hit always fails the run.
        if(report.HitThresholds.Count > 0)
        {
            return Constants.ExitCodes.THRESHOLD_HIT;
        }

        if(options.FailOnWarnings && report.Warnings.Count > 0)
        {
            return Constants.ExitCodes.WARNING;
        }

        return Constants.ExitCodes.SUCCESS;
    }
}
