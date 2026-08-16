using System.Collections.Generic;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using static PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models.ComparerReport;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;

internal readonly record struct GenPresence(bool Gen0, bool Gen1, bool Gen2);

internal static class ComparisonTableBuilder
{
    public static GenPresence DetectGenPresence(ComparerReport report)
        => new(
            report.Comparisons.Any(c => c.Gen0Collections is not null),
            report.Comparisons.Any(c => c.Gen1Collections is not null),
            report.Comparisons.Any(c => c.Gen2Collections is not null));

    public static List<string> BuildHeader(GenPresence gens)
    {
        var header = new List<string> { "Report", "Type", "Method", "Mean" };

        if(gens.Gen0)
        {
            header.Add("Gen0");
        }
        if(gens.Gen1)
        {
            header.Add("Gen1");
        }
        if(gens.Gen2)
        {
            header.Add("Gen2");
        }

        header.Add("Allocated");

        return header;
    }

    public static List<string?> BuildBaselineRow(Comparison comparison, GenPresence gens)
    {
        var row = new List<string?>
        {
            "Baseline",
            comparison.Type,
            comparison.Name,
            comparison.Mean?.Baseline.BeautifyTime()
        };

        if(gens.Gen0)
        {
            row.Add(comparison.Gen0Collections?.Baseline?.ToString());
        }
        if(gens.Gen1)
        {
            row.Add(comparison.Gen1Collections?.Baseline?.ToString());
        }
        if(gens.Gen2)
        {
            row.Add(comparison.Gen2Collections?.Baseline?.ToString());
        }

        row.Add(comparison.Allocated?.Baseline.BeautifyMemory());

        return row;
    }

    public static List<string?> BuildTargetRow(Comparison comparison, GenPresence gens)
    {
        var status = comparison.Mean?.Status is ComparisonStatus.Removed or ComparisonStatus.New
            ? $"[{comparison.Mean?.Status.ToString().ToUpper()}]"
            : null;

        var row = new List<string?>
        {
            "Target",
            null,
            status,
            _withDiff(comparison.Mean?.Target.BeautifyTime(), comparison.Mean)
        };

        if(gens.Gen0)
        {
            row.Add(_withDiff(comparison.Gen0Collections?.Target.ToString(), comparison.Gen0Collections));
        }
        if(gens.Gen1)
        {
            row.Add(_withDiff(comparison.Gen1Collections?.Target.ToString(), comparison.Gen1Collections));
        }
        if(gens.Gen2)
        {
            row.Add(_withDiff(comparison.Gen2Collections?.Target.ToString(), comparison.Gen2Collections));
        }

        row.Add(_withDiff(comparison.Allocated?.Target.BeautifyMemory(), comparison.Allocated));

        return row;
    }


    private static string? _withDiff(string? value, MetricComparison? metric)
        => metric?.Status is ComparisonStatus.Better or ComparisonStatus.Worse
            ? $"{value} ({metric.DiffPercentage.BeautifyPercentage()})"
            : value;
}
