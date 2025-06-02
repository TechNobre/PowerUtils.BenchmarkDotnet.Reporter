using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MarkdownLog;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;
using PowerUtils.BenchmarkDotnet.Reporter.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Helpers.IOHelpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Exporters;

public sealed class MarkdownExporter(FileWriter writer) : IExporter
{
    private readonly FileWriter _writer = writer;

    public void Generate(ComparerReport report, string outputDirectory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# BENCHMARK COMPARISON REPORT");
        sb.AppendLine();

        if(report.Warnings.Count != 0)
        {
            sb.AppendLine("## ⚠️ WARNINGS:");
            sb.AppendLine();
        }

        foreach(var warning in report.Warnings)
        {
            sb.AppendLine($"    * {warning}");
        }

        if(report.Warnings.Count != 0)
        {
            sb.AppendLine();
            sb.AppendLine();
        }

        sb.AppendLine("## 📊 RESULTS:");
        sb.AppendLine();

        if(report.Comparisons.Count == 0)
        {
            sb.Append("    NO COMPARISONS FOUND.");
        }
        else
        {
            var table = new Table
            {
                Columns = [
                    new()
                    {
                        HeaderCell = new TableCell { Text = "Report" }
                    },
                    new()
                    {
                        HeaderCell = new TableCell { Text = "Method" }
                    },
                    new()
                    {
                        HeaderCell = new TableCell { Text = "Mean" },
                        Alignment = TableColumnAlignment.Right,
                    },
                    new()
                    {
                        HeaderCell = new TableCell { Text = "Allocated" },
                        Alignment = TableColumnAlignment.Right,
                    }
                ]
            };


            var rows = new List<TableRow>();
            foreach(var comparison in report.Comparisons)
            {
                rows.Add(new()
                {
                    Cells =
                    [
                        new TableCell { Text = "Baseline" },
                        new TableCell { Text = comparison.Name },
                        new TableCell { Text = comparison.Mean?.Baseline.BeautifyTime() },
                        new TableCell { Text = comparison.Allocated?.Baseline.BeautifyMemory() },
                    ]
                });


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

                rows.Add(new()
                {
                    Cells =
                    [
                        new TableCell { Text = "Target" },
                        new TableCell { Text = comparison.Mean?.Status is ComparisonStatus.Removed or ComparisonStatus.New
                            ? $"[{comparison.Mean?.Status.ToString().ToUpper()}]"
                            : "" },
                        new TableCell { Text = mean },
                        new TableCell { Text = allocated },
                    ]
                });
            }
            table.Rows = rows;
            sb.Append(table.ToMarkdown());
        }


        if(report.HitThresholds.Count != 0)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("## 🚨 THRESHOLD VIOLATIONS:");
            sb.AppendLine();
            foreach(var hitThreshold in report.HitThresholds.Order())
            {
                sb.AppendLine($"    * {hitThreshold};");
            }
        }

        _writer(
            Path.Combine(outputDirectory, "benchmark-comparison-report.md"),
            sb.ToString());
    }
}
