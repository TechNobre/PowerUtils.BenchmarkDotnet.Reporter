using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using System.Linq;
using static PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models.ComparerReport;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;

public sealed class CompareHandler(
    Func<string?, List<BenchmarkReport>> readBenchmarks,
    ICompareValidator validator,
    IServiceProvider provider)
{
    private readonly Func<string?, List<BenchmarkReport>> _readBenchmarks = readBenchmarks;
    private readonly ICompareValidator _validator = validator;
    private readonly IServiceProvider _provider = provider;


    public int Execute(CompareOptions options)
    {
        var baselineBenchmarks = _readBenchmarks(options.Baseline);
        var targetBenchmarks = _readBenchmarks(options.Target);

        var report = new ComparerReport();

        foreach(var baselineBenchmark in baselineBenchmarks)
        {
            var targetBenchmark = targetBenchmarks
                .SingleOrDefault(s => s.FullName.EquivalentTo(baselineBenchmark.FullName));

            var validationMessages = _validator.ValidateHostEnvironment(baselineBenchmark, targetBenchmark);
            if(validationMessages?.Count > 0)
            {
                report.Warnings.AddRange(validationMessages);
            }

            report.Add(_buildComparison(baselineBenchmark, targetBenchmark));

            if(targetBenchmark is not null)
            {
                // Remove a target benchmark because matched with baseline.
                //   The purpose is to keep only the unmatched benchmarks in the target report for next loop only have unmatched benchmarks.
                targetBenchmarks.Remove(targetBenchmark);
            }
        }

        // Add the unmatched benchmarks from the target report.
        foreach(var targetBenchmark in targetBenchmarks)
        {
            report.Add(_buildComparison(null, targetBenchmark));
        }

        _validator.EvaluateThresholds(report, options.MeanThreshold, options.AllocationThreshold);

        foreach(var format in options.Formats)
        {
            // Generate the final report using the specified exporter
            _provider
                .GetRequiredKeyedService<IExporter>(format.ToLowerInvariant())
                .Generate(report, options.Output);
        }

        // Check for error conditions and return appropriate exit codes
        if(options.FailOnWarnings && report.Warnings.Count > 0)
        {
            return Constants.ExitCodes.WARNING; // Exit with error code when warnings are present and failOnWarnings is enabled
        }

        if(options.FailOnThresholdHit && report.HitThresholds.Count > 0)
        {
            return Constants.ExitCodes.THRESHOLD_HIT; // Exit with error code when thresholds are hit and failOnThresholdHit is enabled
        }

        return Constants.ExitCodes.SUCCESS;
    }


    private static Comparison _buildComparison(BenchmarkReport? baselineBenchmark, BenchmarkReport? targetBenchmark)
        => new()
        {
            Type = baselineBenchmark?.Type ?? targetBenchmark?.Type,
            Name = baselineBenchmark?.Method ?? targetBenchmark?.Method,
            FullName = baselineBenchmark?.FullName ?? targetBenchmark?.FullName,

            Mean = MetricComparison.CalculateExecutionTime(
                baselineBenchmark?.Statistics?.Mean,
                targetBenchmark?.Statistics?.Mean),

            Gen0Collections = MetricComparison.CalculateGarbageCollectionOperations(
                baselineBenchmark?.Memory?.Gen0Collections,
                baselineBenchmark?.Memory?.TotalOperations,
                targetBenchmark?.Memory?.Gen0Collections,
                targetBenchmark?.Memory?.TotalOperations),

            Gen1Collections = MetricComparison.CalculateGarbageCollectionOperations(
                baselineBenchmark?.Memory?.Gen1Collections,
                baselineBenchmark?.Memory?.TotalOperations,
                targetBenchmark?.Memory?.Gen1Collections,
                targetBenchmark?.Memory?.TotalOperations),

            Gen2Collections = MetricComparison.CalculateGarbageCollectionOperations(
                baselineBenchmark?.Memory?.Gen2Collections,
                baselineBenchmark?.Memory?.TotalOperations,
                targetBenchmark?.Memory?.Gen2Collections,
                targetBenchmark?.Memory?.TotalOperations),

            Allocated = MetricComparison.CalculateMemoryUsage(
                baselineBenchmark?.Memory?.BytesAllocatedPerOperation,
                targetBenchmark?.Memory?.BytesAllocatedPerOperation)
        };
}
