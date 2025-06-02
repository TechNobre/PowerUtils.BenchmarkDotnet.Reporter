using System;
using System.Collections.Generic;
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
            const int SPACE_BETWEEN_COLUMNS = 5;
            var rows = new List<string[]>
            {
                new[] { "Report", "Method", "Mean", "Allocated" }
            };

            var reportWidth = rows[0][0].Length + SPACE_BETWEEN_COLUMNS;
            var methodWidth = rows[0][1].Length + SPACE_BETWEEN_COLUMNS;
            var meanWidth = rows[0][2].Length + SPACE_BETWEEN_COLUMNS;
            var allocatedWidth = rows[0][3].Length;


            foreach(var comparison in report.Comparisons)
            {
                var row = new string[] {
                    "Baseline",
                    comparison.Name ?? "",
                    comparison.Mean?.Baseline.BeautifyTime() ?? "",
                    comparison.Allocated?.Baseline.BeautifyMemory() ?? ""
                };
                rows.Add(row);

                reportWidth = Math.Max(reportWidth, row[0].Length + SPACE_BETWEEN_COLUMNS);
                methodWidth = Math.Max(methodWidth, row[1].Length + SPACE_BETWEEN_COLUMNS);
                meanWidth = Math.Max(meanWidth, row[2].Length + SPACE_BETWEEN_COLUMNS);
                allocatedWidth = Math.Max(allocatedWidth, row[3].Length);


                var mean = comparison.Mean?.Target.BeautifyTime();
                if(comparison.Mean?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                {
                    mean = $"{mean} ({comparison.Mean?.DiffPercentage.BeautifyPercentage()})";
                }

                var allocated = comparison.Allocated?.Target.BeautifyMemory();
                if(comparison.Allocated?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                {
                    allocated = $"{allocated} ({comparison.Allocated?.DiffPercentage.BeautifyPercentage()})";
                }


                row = [
                    "Target",
                    comparison.Mean?.Status is ComparisonStatus.Removed or ComparisonStatus.New
                                            ? $"[{comparison.Mean?.Status.ToString().ToUpper()}]"
                                            : "",
                    mean ?? "",
                    allocated ?? ""
                ];

                rows.Add(row);

                reportWidth = Math.Max(reportWidth, row[0].Length + SPACE_BETWEEN_COLUMNS);
                methodWidth = Math.Max(methodWidth, row[1].Length + SPACE_BETWEEN_COLUMNS);
                meanWidth = Math.Max(meanWidth, row[2].Length + SPACE_BETWEEN_COLUMNS);
                allocatedWidth = Math.Max(allocatedWidth, row[3].Length);
            }

            // Header
            _printer($"{rows[0][0].PadRight(reportWidth)}{rows[0][1].PadRight(methodWidth)}{rows[0][2].PadRight(meanWidth)}{rows[0][3]}");
            _printer(Environment.NewLine);
            _printer(new string('─', reportWidth + methodWidth + meanWidth + allocatedWidth));
            _printer(Environment.NewLine);

            for(var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                _printer($"{row[0].PadRight(reportWidth)}{row[1].PadRight(methodWidth)}{row[2].PadRight(meanWidth)}{row[3]}");
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
