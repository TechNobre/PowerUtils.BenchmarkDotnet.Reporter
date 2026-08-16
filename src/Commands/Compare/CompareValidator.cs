using System;
using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using static PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models.ComparerReport;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;

public interface ICompareValidator
{
    List<string> ValidateHostEnvironment(BenchmarkReport? baseline, BenchmarkReport? target);
    void EvaluateThresholds(ComparerReport report, string? meanThreshold, string? allocationThreshold);
}

public sealed class CompareValidator : ICompareValidator
{
    private static readonly (string Label, Func<BenchmarkHeader.HostEnvironmentInfoRecord?, object?> Select)[] _hostEnvironmentChecks =
    [
        ("OS Version", info => info?.OsVersion),
        ("Processor Name", info => info?.ProcessorName),
        ("Physical Processor Count", info => info?.PhysicalProcessorCount),
        ("Physical Core Count", info => info?.PhysicalCoreCount),
        ("Logical Core Count", info => info?.LogicalCoreCount),
        ("Runtime Version", info => info?.RuntimeVersion),
        ("Architecture", info => info?.Architecture),
        ("DotNet CLI Version", info => info?.DotNetCliVersion),
        ("Chronometer Frequency", info => info?.ChronometerFrequency?.Hertz)
    ];


    public List<string> ValidateHostEnvironment(BenchmarkReport? baseline, BenchmarkReport? target)
    {
        var messages = new List<string>();

        if(baseline is null)
        {
            return messages;
        }

        if(target is null)
        {
            return messages;
        }

        var baselineInfo = baseline.Header?.HostEnvironmentInfo;
        var targetInfo = target.Header?.HostEnvironmentInfo;

        foreach(var check in _hostEnvironmentChecks)
        {
            var baselineValue = check.Select(baselineInfo);
            var targetValue = check.Select(targetInfo);

            if(!_valuesEquivalent(baselineValue, targetValue))
            {
                messages.Add($"[{baseline.FullName}] {check.Label} is different: '{baselineValue}' != '{targetValue}'");
            }
        }

        if(!"RELEASE".EquivalentTo(baselineInfo?.Configuration))
        {
            messages.Add($"[{baseline.FullName}] The baseline report wasn't executed in RELEASE mode: '{baselineInfo?.Configuration}'");
        }

        if(!"RELEASE".EquivalentTo(targetInfo?.Configuration))
        {
            messages.Add($"[{target.FullName}] The target report wasn't executed in RELEASE mode: '{targetInfo?.Configuration}'");
        }

        return messages;


        static bool _valuesEquivalent(object? left, object? right)
            => left is string || right is string
                ? ((string?)left).EquivalentTo((string?)right)
                : Equals(left, right);
    }


    public void EvaluateThresholds(ComparerReport report, string? meanThreshold, string? allocationThreshold)
    {
        if(!string.IsNullOrWhiteSpace(meanThreshold))
        {
            var threshold = TimeThreshold.Parse(meanThreshold);
            _evaluate(report, threshold.Value, threshold.IsPercentage, "Mean", c => c.Mean);
        }

        if(!string.IsNullOrWhiteSpace(allocationThreshold))
        {
            var threshold = MemoryThreshold.Parse(allocationThreshold);
            _evaluate(report, threshold.Value, threshold.IsPercentage, "Allocation", c => c.Allocated);
        }


        static void _evaluate(
            ComparerReport report,
            decimal thresholdValue,
            bool isPercentage,
            string label,
            Func<Comparison, MetricComparison?> metricSelector)
        {
            foreach(var comparison in report.Comparisons)
            {
                var metric = metricSelector(comparison);

                // If the threshold is a percentage, the validation is done against the percentage difference;
                // otherwise it's validated against the absolute difference
                var exceeded = isPercentage
                    ? metric?.DiffPercentage > thresholdValue
                    : metric?.Diff > thresholdValue;

                if(exceeded)
                {
                    report.HitThresholds.Add($"{label} threshold hit for '{comparison.FullName}'");
                }
            }
        }
    }
}
