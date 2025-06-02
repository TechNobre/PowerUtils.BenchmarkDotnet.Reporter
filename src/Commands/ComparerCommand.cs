using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;
using PowerUtils.BenchmarkDotnet.Reporter.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Validations;
using static PowerUtils.BenchmarkDotnet.Reporter.Models.ComparerReport;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands;

public interface IComparerCommand
{
    void Execute(string? baseline, string? target, string? meanThreshold, string? allocationThreshold, string[] formats, string output);
}

public sealed class ComparerCommand(
    Func<string?, BenchmarkFullJsonResport> readFullJsonReport,
    IReportValidation validation,
    IServiceProvider provider) : IComparerCommand
{
    private readonly Func<string?, BenchmarkFullJsonResport> _readFullJsonReport = readFullJsonReport;
    private readonly IReportValidation _validation = validation;
    private readonly IServiceProvider _provider = provider;


    public void Execute(
        string? baseline,
        string? target,
        string? meanThreshold,
        string? allocationThreshold,
        string[] formats,
        string output)
    {
        var baselineReport = _readFullJsonReport(baseline);
        var targetReport = _readFullJsonReport(target);

        var comparerReport = new ComparerReport
        {
            Warnings = _validation.HostEnvironmentValidate(baselineReport, targetReport)
        };



        foreach(var baselineBenchmark in baselineReport.Benchmarks ?? [])
        {
            var targetBenchmark = targetReport.Benchmarks?
                .SingleOrDefault(s => s.FullName.Equivalente(baselineBenchmark.FullName));

            var current = new Comparison
            {
                Name = baselineBenchmark.Method,
                FullName = baselineBenchmark.FullName,

                Mean = MetricComparison.CalculateExecutionTime(
                    baselineBenchmark.Statistics?.Mean,
                    targetBenchmark?.Statistics?.Mean),

                Allocated = MetricComparison.CalculateMemoryUsage(
                    baselineBenchmark.Memory?.BytesAllocatedPerOperation,
                    targetBenchmark?.Memory?.BytesAllocatedPerOperation)
            };

            if(targetBenchmark is not null)
            {
                // Remove a target benchmark because matched with baseline.
                //   The purpose is to keep only the unmatched benchmarks in the target report for next loop only have unmatched benchmarks.
                targetReport.Benchmarks?.Remove(targetBenchmark);
            }

            comparerReport.Add(current);
        }

        // Add the unmatched benchmarks from the target report.
        foreach(var targetBenchmark in targetReport.Benchmarks ?? [])
        {
            comparerReport.Add(new()
            {
                Name = targetBenchmark.Method,
                FullName = targetBenchmark.FullName,

                Mean = MetricComparison.CalculateExecutionTime(
                    null,
                    targetBenchmark?.Statistics?.Mean),

                Allocated = MetricComparison.CalculateMemoryUsage(
                    null,
                    targetBenchmark?.Memory?.BytesAllocatedPerOperation)
            });
        }



        if(!string.IsNullOrWhiteSpace(meanThreshold))
        {
            var meanThresholdValue = TimeThreshold.Parse(meanThreshold);

            foreach(var comparison in comparerReport.Comparisons)
            {
                if(meanThresholdValue.IsPercentage)
                { // If the threshold is a percentage, the validation is done against the percentage difference
                    if(comparison.Mean?.DiffPercentage > meanThresholdValue.Value)
                    {
                        comparerReport.HitThresholds.Add($"Mean threshold hit for '{comparison.FullName}'");
                    }
                }
                else
                { // If the threshold is a value, the validation is done against the absolute difference
                    if(comparison.Mean?.Diff > meanThresholdValue.Value)
                    {
                        comparerReport.HitThresholds.Add($"Mean threshold hit for '{comparison.FullName}'");
                    }
                }
            }
        }


        if(!string.IsNullOrWhiteSpace(allocationThreshold))
        {
            var allocationThresholdValue = MemoryThreshold.Parse(allocationThreshold);

            foreach(var comparison in comparerReport.Comparisons)
            {
                if(allocationThresholdValue.IsPercentage)
                { // If the threshold is a percentage, the validation is done against the percentage difference
                    if(comparison.Allocated?.DiffPercentage > allocationThresholdValue.Value)
                    {
                        comparerReport.HitThresholds.Add($"Allocation threshold hit for '{comparison.FullName}'");
                    }
                }
                else
                { // If the threshold is a value, the validation is done against the absolute difference
                    if(comparison.Allocated?.Diff > allocationThresholdValue.Value)
                    {
                        comparerReport.HitThresholds.Add($"Allocation threshold hit for '{comparison.FullName}'");
                    }
                }
            }
        }

        foreach(var format in formats)
        {
            // Generate the final report using the specified exporter
            _provider
                .GetRequiredKeyedService<IExporter>(format.ToLower())
                .Generate(comparerReport, output);
        }
    }
}
