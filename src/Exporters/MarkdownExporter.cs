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
            var columns = new List<TableColumn>();

            columns.Add(new()
            {
                HeaderCell = new TableCell { Text = "Report" }
            });

            columns.Add(new()
            {
                HeaderCell = new TableCell { Text = "Type" }
            });

            columns.Add(new()
            {
                HeaderCell = new TableCell { Text = "Method" }
            });

            columns.Add(new()
            {
                HeaderCell = new TableCell { Text = "Mean" },
                Alignment = TableColumnAlignment.Right,
            });

            var hasGen0CollectionsValues = report.Comparisons
                .Any(c => c.Gen0Collections is not null);
            if(hasGen0CollectionsValues)
            {
                columns.Add(new()
                {
                    HeaderCell = new TableCell { Text = "Gen0" },
                    Alignment = TableColumnAlignment.Right,
                });
            }

            var hasGen1CollectionsValues = report.Comparisons
                .Any(c => c.Gen1Collections is not null);
            if(hasGen1CollectionsValues)
            {
                columns.Add(new()
                {
                    HeaderCell = new TableCell { Text = "Gen1" },
                    Alignment = TableColumnAlignment.Right,
                });
            }

            var hasGen2CollectionsValues = report.Comparisons
                .Any(c => c.Gen2Collections is not null);
            if(hasGen2CollectionsValues)
            {
                columns.Add(new()
                {
                    HeaderCell = new TableCell { Text = "Gen2" },
                    Alignment = TableColumnAlignment.Right,
                });
            }

            columns.Add(new()
            {
                HeaderCell = new TableCell { Text = "Allocated" },
                Alignment = TableColumnAlignment.Right,
            });


            var table = new MarkdownLog.Table
            {
                Columns = columns
            };


            var rows = new List<TableRow>();
            foreach(var comparison in report.Comparisons)
            {
                var cellsBaseline = new List<TableCell>();
                cellsBaseline.Add(new TableCell { Text = "Baseline" });
                cellsBaseline.Add(new TableCell { Text = comparison.Type });
                cellsBaseline.Add(new TableCell { Text = comparison.Name });
                cellsBaseline.Add(new TableCell { Text = comparison.Mean?.Baseline.BeautifyTime() });
                if(hasGen0CollectionsValues)
                {
                    cellsBaseline.Add(new TableCell { Text = comparison.Gen0Collections?.Baseline?.ToString() });
                }
                if(hasGen1CollectionsValues)
                {
                    cellsBaseline.Add(new TableCell { Text = comparison.Gen1Collections?.Baseline?.ToString() });
                }
                if(hasGen2CollectionsValues)
                {
                    cellsBaseline.Add(new TableCell { Text = comparison.Gen2Collections?.Baseline?.ToString() });
                }
                cellsBaseline.Add(new TableCell { Text = comparison.Allocated?.Baseline.BeautifyMemory() });

                rows.Add(new()
                {
                    Cells = cellsBaseline
                });


                var mean = comparison.Mean?.Target.BeautifyTime();
                if(comparison.Mean?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                {
                    mean = $"{mean} ({comparison.Mean?.DiffPercentage.BeautifyPercentage()})";
                }

                var gen0 = comparison.Gen0Collections?.Target.ToString();
                if(comparison.Gen0Collections?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                {
                    gen0 = $"{gen0} ({comparison.Gen0Collections?.DiffPercentage.BeautifyPercentage()})";
                }

                var gen1 = comparison.Gen1Collections?.Target.ToString();
                if(comparison.Gen1Collections?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                {
                    gen1 = $"{gen1} ({comparison.Gen1Collections?.DiffPercentage.BeautifyPercentage()})";
                }

                var gen2 = comparison.Gen2Collections?.Target.ToString();
                if(comparison.Gen2Collections?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                {
                    gen2 = $"{gen2} ({comparison.Gen2Collections?.DiffPercentage.BeautifyPercentage()})";
                }

                var allocated = comparison.Allocated?.Target.BeautifyMemory();
                if(comparison.Allocated?.Status is ComparisonStatus.Better or ComparisonStatus.Worse)
                {
                    allocated = $"{allocated} ({comparison.Allocated?.DiffPercentage.BeautifyPercentage()})";
                }

                var cellsTarget = new List<TableCell>();
                cellsTarget.Add(new TableCell { Text = "Target" });
                cellsTarget.Add(new TableCell { Text = null });
                cellsTarget.Add(new TableCell
                {
                    Text = comparison.Mean?.Status is ComparisonStatus.Removed or ComparisonStatus.New
                        ? $"[{comparison.Mean?.Status.ToString().ToUpper()}]"
                        : null
                });
                cellsTarget.Add(new TableCell { Text = mean });
                if(hasGen0CollectionsValues)
                {
                    cellsTarget.Add(new TableCell { Text = gen0 });
                }
                if(hasGen1CollectionsValues)
                {
                    cellsTarget.Add(new TableCell { Text = gen1 });
                }
                if(hasGen2CollectionsValues)
                {
                    cellsTarget.Add(new TableCell { Text = gen2 });
                }
                cellsTarget.Add(new TableCell { Text = allocated });

                rows.Add(new()
                {
                    Cells = cellsTarget
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
