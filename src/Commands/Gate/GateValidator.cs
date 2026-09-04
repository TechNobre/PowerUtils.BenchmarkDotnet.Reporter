using System;
using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using BenchmarkReport = PowerUtils.BenchmarkDotnet.Reporter.Common.Models.BenchmarkReport;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;

public interface IGateValidator
{
    List<string> ValidateHostEnvironment(List<BenchmarkReport> benchmarks);
    void EvaluateThresholds(GateReport report, IReadOnlyList<KeyValuePair<string, string>> meanThresholds, IReadOnlyList<KeyValuePair<string, string>> allocationThresholds);
}

public sealed class GateValidator : IGateValidator
{
    public List<string> ValidateHostEnvironment(List<BenchmarkReport> benchmarks)
    {
        var messages = new List<string>();

        foreach(var benchmark in benchmarks)
        {
            messages.AddIfNotRelease(benchmark);
        }

        return messages;
    }


    public void EvaluateThresholds(GateReport report, IReadOnlyList<KeyValuePair<string, string>> meanThresholds, IReadOnlyList<KeyValuePair<string, string>> allocationThresholds)
    {
        _evaluate(report, meanThresholds, "Mean", r => r.Mean, value =>
        {
            var threshold = TimeThreshold.Parse(value);
            return (threshold.Value, false);
        });

        _evaluate(report, allocationThresholds, "Allocation", r => r.Allocated, value =>
        {
            var threshold = MemoryThreshold.Parse(value);
            return (threshold.Value, false);
        });


        static void _evaluate(
            GateReport report,
            IReadOnlyList<KeyValuePair<string, string>> rules,
            string label,
            Func<GateReport.Result, decimal?> metricSelector,
            Func<string, (decimal Value, bool IsPercentage)> parse)
        {
            if(rules.Count == 0)
            {
                return;
            }

            var resolved = ThresholdResolver.Resolve(rules, parse);

            foreach(var result in report.Results)
            {
                var best = ThresholdResolver.FindMatch(resolved, result.FullName);

                if(best is null)
                {
                    continue;
                }

                var metric = metricSelector(result);

                if(metric > best.Value.Value)
                {
                    // The '*' pattern is the implicit catch-all (a bare, unscoped threshold value), so it's omitted from the message.
                    var ruleSuffix = best.Value.Pattern == NamespacesUtils.WILDCARD.ToString()
                        ? ""
                        : $" (rule: {best.Value.Pattern})";

                    report.HitThresholds.Add($"{label} threshold hit for '{result.FullName}'{ruleSuffix}");
                }
            }
        }
    }
}
