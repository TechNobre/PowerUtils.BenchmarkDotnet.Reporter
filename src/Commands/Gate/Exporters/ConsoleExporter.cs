using System;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;

public sealed class ConsoleExporter : IExporter
{
    public void Generate(GateReport report, string outputDirectory)
    {
        Console.WriteLine("══════════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("                            BENCHMARK GATE REPORT");
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

        if(report.Results.Count == 0)
        {
            Console.WriteLine("   No benchmarks found.");
            Console.WriteLine();
        }
        else
        {
            var tableBuilder = TableBuilder.Create();
            tableBuilder.AddHeader(GateTableBuilder.BuildHeader());

            foreach(var result in report.Results)
            {
                tableBuilder.AddRow(GateTableBuilder.BuildRow(result));
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
