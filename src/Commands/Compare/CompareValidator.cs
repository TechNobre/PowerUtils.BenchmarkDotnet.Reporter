using System;
using System.Collections.Generic;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using static PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models.ComparerReport;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;

public interface ICompareValidator
{
    List<string> ValidateHostEnvironment(BenchmarkReport? baseline, BenchmarkReport? target);
    void EvaluateThresholds(ComparerReport report, IReadOnlyList<KeyValuePair<string, string>> meanThresholds, IReadOnlyList<KeyValuePair<string, string>> allocationThresholds);
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


    private readonly record struct ResolvedThreshold(decimal Value, bool IsPercentage, string Pattern);


    public void EvaluateThresholds(ComparerReport report, IReadOnlyList<KeyValuePair<string, string>> meanThresholds, IReadOnlyList<KeyValuePair<string, string>> allocationThresholds)
    {
        _evaluate(report, meanThresholds, "Mean", c => c.Mean, value =>
        {
            var threshold = TimeThreshold.Parse(value);
            return (threshold.Value, threshold.IsPercentage);
        });

        _evaluate(report, allocationThresholds, "Allocation", c => c.Allocated, value =>
        {
            var threshold = MemoryThreshold.Parse(value);
            return (threshold.Value, threshold.IsPercentage);
        });


        static void _evaluate(
            ComparerReport report,
            IReadOnlyList<KeyValuePair<string, string>> rules,
            string label,
            Func<Comparison, MetricComparison?> metricSelector,
            Func<string, (decimal Value, bool IsPercentage)> parse)
        {
            if(rules.Count == 0)
            {
                return;
            }

            // Parse eagerly so malformed threshold syntax fails fast, even when no comparison matches it.
            // Pre-sort by specificity once — pattern specificity is constant across comparisons.
            var resolved = rules
                .Select(rule =>
                {
                    var parsed = parse(rule.Value);
                    return new ResolvedThreshold(parsed.Value, parsed.IsPercentage, rule.Key);
                })
                .OrderByDescending(rule => NamespacesUtils.GetSpecificity(rule.Pattern))
                .ToList();

            foreach(var comparison in report.Comparisons)
            {
                var best = resolved
                    .Where(rule => NamespacesUtils.IsMatch(rule.Pattern, comparison.FullName))
                    .Select(rule => (ResolvedThreshold?)rule)
                    .FirstOrDefault();

                if(best is null)
                {
                    continue;
                }

                var metric = metricSelector(comparison);

                // If the threshold is a percentage, the validation is done against the percentage difference;
                // otherwise it's validated against the absolute difference
                var exceeded = best.Value.IsPercentage
                    ? metric?.DiffPercentage > best.Value.Value
                    : metric?.Diff > best.Value.Value;

                if(exceeded)
                {
                    // The '*' pattern is the implicit catch-all (a bare, unscoped threshold value), so it's omitted from the message.
                    var ruleSuffix = best.Value.Pattern == NamespacesUtils.WILDCARD.ToString()
                        ? ""
                        : $" (rule: {best.Value.Pattern})";

                    report.HitThresholds.Add($"{label} threshold hit for '{comparison.FullName}'{ruleSuffix}");
                }
            }
        }
    }
}
