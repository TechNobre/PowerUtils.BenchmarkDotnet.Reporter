using System;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;

public sealed class ConsoleExporter : IExporter
{
    public void Generate(ComparerReport report, string outputDirectory)
    {
        Console.WriteLine("══════════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("                        BENCHMARK COMPARISON REPORT");
        Console.WriteLine("══════════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        if(report.Warnings.Count != 0)
        {
            Console.WriteLine("⚠️ WARNINGS:");
            Console.WriteLine();

            foreach(var warning in report.Warnings)
            {
                Console.WriteLine($"   • {warning}");
            }

            Console.WriteLine();
            Console.WriteLine(".................................................................................");
            Console.WriteLine();
        }

        Console.WriteLine("📊 RESULTS:");
        Console.WriteLine();

        if(report.Comparisons.Count == 0)
        {
            Console.WriteLine("   No comparisons found.");
            Console.WriteLine();
        }
        else
        {
            var gens = ComparisonTableBuilder.DetectGenPresence(report);

            var tableBuilder = TableBuilder.Create();
            tableBuilder.AddHeader(ComparisonTableBuilder.BuildHeader(gens));

            foreach(var comparison in report.Comparisons)
            {
                tableBuilder.AddRow(ComparisonTableBuilder.BuildBaselineRow(comparison, gens));
                tableBuilder.AddRow(ComparisonTableBuilder.BuildTargetRow(comparison, gens));
            }

            var table = tableBuilder.Build();

            foreach(var row in table)
            {
                Console.WriteLine(string.Join("", row));
            }
        }

        if(report.HitThresholds.Count != 0)
        {
            Console.WriteLine();
            Console.WriteLine(".................................................................................");
            Console.WriteLine();
            Console.WriteLine("🚨 THRESHOLD VIOLATIONS:");
            Console.WriteLine();

            foreach(var hit in report.HitThresholds)
            {
                Console.WriteLine($"   • {hit}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════════════════════════════════════════════");
    }
}
