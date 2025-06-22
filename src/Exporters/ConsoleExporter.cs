using System;
using System.Collections.Generic;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;
using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Exporters;

public sealed class ConsoleExporter(IOHelpers.Printer printer) : IExporter
{
    private readonly IOHelpers.Printer _printer = printer;

    public void Generate(ComparerReport report, string outputDirectory)
    {
        _printer(Environment.NewLine);
        _printer("══════════════════════════════════════════════════════════════════════════════════");
        _printer(Environment.NewLine);
        _printer("                        BENCHMARK COMPARISON REPORT");
        _printer(Environment.NewLine);
        _printer("══════════════════════════════════════════════════════════════════════════════════");
        _printer(Environment.NewLine);
        _printer(Environment.NewLine);

        if(report.Warnings.Count != 0)
        {
            _printer("⚠️ WARNINGS:");
            _printer(Environment.NewLine);
            _printer(Environment.NewLine);

            foreach(var warning in report.Warnings)
            {
                _printer($"   • {warning}");
                _printer(Environment.NewLine);
            }

            _printer(Environment.NewLine);
            _printer(".................................................................................");
            _printer(Environment.NewLine);
            _printer(Environment.NewLine);
        }

        _printer("📊 RESULTS:");
        _printer(Environment.NewLine);
        _printer(Environment.NewLine);

        if(report.Comparisons.Count == 0)
        {
            _printer("   No comparisons found.");
            _printer(Environment.NewLine);
        }
        else
        {
            var hasGen0CollectionsValues = report.Comparisons
                .Any(c => c.Gen0Collections is not null);
            var hasGen1CollectionsValues = report.Comparisons
                .Any(c => c.Gen1Collections is not null);
            var hasGen2CollectionsValues = report.Comparisons
                .Any(c => c.Gen2Collections is not null);


            var tableBuilder = TableBuilder.Create();

            var header = new List<string>();
            header.Add("Report");
            header.Add("Type");
            header.Add("Method");
            header.Add("Mean");
            if(hasGen0CollectionsValues)
            {
                header.Add("Gen0");
            }
            if(hasGen1CollectionsValues)
            {
                header.Add("Gen1");
            }
            if(hasGen2CollectionsValues)
            {
                header.Add("Gen2");
            }
            header.Add("Allocated");

            tableBuilder.AddHeader(header);

            foreach(var comparison in report.Comparisons)
            {
                // Add baseline row
                var row = new List<string?>();
                row.Add("Baseline");
                row.Add(comparison.Type);
                row.Add(comparison.Name);
                row.Add(comparison.Mean?.Baseline.BeautifyTime());
                if(hasGen0CollectionsValues)
                {
                    row.Add(comparison.Gen0Collections?.Baseline?.ToString());
                }
                if(hasGen1CollectionsValues)
                {
                    row.Add(comparison.Gen1Collections?.Baseline?.ToString());
                }
                if(hasGen2CollectionsValues)
                {
                    row.Add(comparison.Gen2Collections?.Baseline?.ToString());
                }
                row.Add(comparison.Allocated?.Baseline.BeautifyMemory());
                tableBuilder.AddRow(row);


                // Add target row
                row =
                [
                    "Target",
                    null,
                    comparison.Mean?.Status is ComparisonStatus.Removed or ComparisonStatus.New
                        ? $"[{comparison.Mean?.Status.ToString().ToUpper()}]"
                        : null,
                ];

                var mean = comparison.Mean?.Target.BeautifyTime();
                if(comparison.Mean?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                {
                    mean = $"{mean} ({comparison.Mean?.DiffPercentage.BeautifyPercentage()})";
                }
                row.Add(mean);

                if(hasGen0CollectionsValues)
                {
                    var gen0 = comparison.Gen0Collections?.Target.ToString();
                    if(comparison.Gen0Collections?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                    {
                        gen0 = $"{gen0} ({comparison.Gen0Collections?.DiffPercentage.BeautifyPercentage()})";
                    }
                    row.Add(gen0);
                }

                if(hasGen1CollectionsValues)
                {
                    var gen1 = comparison.Gen1Collections?.Target.ToString();
                    if(comparison.Gen1Collections?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                    {
                        gen1 = $"{gen1} ({comparison.Gen1Collections?.DiffPercentage.BeautifyPercentage()})";
                    }
                    row.Add(gen1);
                }

                if(hasGen2CollectionsValues)
                {
                    var gen2 = comparison.Gen2Collections?.Target.ToString();
                    if(comparison.Gen2Collections?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                    {
                        gen2 = $"{gen2} ({comparison.Gen2Collections?.DiffPercentage.BeautifyPercentage()})";
                    }
                    row.Add(gen2);
                }

                var allocated = comparison.Allocated?.Target.BeautifyMemory();
                if(comparison.Allocated?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                {
                    allocated = $"{allocated} ({comparison.Allocated?.DiffPercentage.BeautifyPercentage()})";
                }
                row.Add(allocated);


                tableBuilder.AddRow(row);
            }

            var table = tableBuilder.Build();

            foreach(var row in table)
            {
                _printer(string.Join("", row));
                _printer(Environment.NewLine);
            }
        }

        if(report.HitThresholds.Count != 0)
        {
            _printer(Environment.NewLine);
            _printer(".................................................................................");
            _printer(Environment.NewLine);
            _printer(Environment.NewLine);
            _printer("🚨 THRESHOLD VIOLATIONS:");
            _printer(Environment.NewLine);
            _printer(Environment.NewLine);

            foreach(var hit in report.HitThresholds)
            {
                _printer($"   • {hit}");
                _printer(Environment.NewLine);
            }
        }

        _printer(Environment.NewLine);
        _printer("══════════════════════════════════════════════════════════════════════════════════");
        _printer(Environment.NewLine);
    }
}
