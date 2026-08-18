using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MarkdownLog;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.IOUtils;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;

public sealed class MarkdownExporter(FileWriter writer) : IExporter
{
    private const int RIGHT_ALIGNED_FROM_COLUMN = 3; // "Mean" onward is right-aligned; "Report"/"Type"/"Method" stay left-aligned

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
            var gens = ComparisonTableBuilder.DetectGenPresence(report);

            var columns = ComparisonTableBuilder.BuildHeader(gens)
                .Select((text, index) => new TableColumn
                {
                    HeaderCell = new TableCell { Text = text },
                    Alignment = index >= RIGHT_ALIGNED_FROM_COLUMN
                        ? TableColumnAlignment.Right
                        : default
                })
                .ToList();

            var rows = new List<TableRow>();
            foreach(var comparison in report.Comparisons)
            {
                rows.Add(_toTableRow(ComparisonTableBuilder.BuildBaselineRow(comparison, gens)));
                rows.Add(_toTableRow(ComparisonTableBuilder.BuildTargetRow(comparison, gens)));
            }

            var table = new MarkdownLog.Table
            {
                Columns = columns,
                Rows = rows
            };
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


    private static TableRow _toTableRow(List<string?> cells)
        => new()
        {
            Cells = cells.Select(text => new TableCell { Text = text }).ToList()
        };
}
