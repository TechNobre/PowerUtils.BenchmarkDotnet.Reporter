using System.IO;
using System.Linq;
using System.Text;
using MarkdownLog;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.IOUtils;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;

public sealed class MarkdownExporter(FileWriter writer) : IExporter
{
    private const int RIGHT_ALIGNED_FROM_COLUMN = 2; // "Mean" onward is right-aligned; "Type"/"Method" stay left-aligned

    private readonly FileWriter _writer = writer;

    public void Generate(GateReport report, string outputDirectory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# BENCHMARK GATE REPORT");
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

        if(report.Results.Count == 0)
        {
            sb.Append("    NO BENCHMARKS FOUND.");
        }
        else
        {
            var columns = GateTableBuilder.BuildHeader()
                .Select((text, index) => new TableColumn
                {
                    HeaderCell = new TableCell { Text = text },
                    Alignment = index >= RIGHT_ALIGNED_FROM_COLUMN
                        ? TableColumnAlignment.Right
                        : default
                })
                .ToList();

            var rows = report.Results
                .Select(result => GateTableBuilder.BuildRow(result).ToTableRow())
                .ToList();

            var table = new Table
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
            Path.Combine(outputDirectory, "benchmark-gate-report.md"),
            sb.ToString());
    }
}
