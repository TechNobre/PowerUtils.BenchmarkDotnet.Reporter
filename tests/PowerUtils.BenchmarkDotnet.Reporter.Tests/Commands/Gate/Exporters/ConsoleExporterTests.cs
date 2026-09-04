using System;
using System.IO;
using System.Text;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate.Exporters;

[CollectionDefinition(nameof(ConsoleExporterTests), DisableParallelization = true)]
public class ConsoleTestCollection;

// Tests in this class manipulate Console.Out (shared global state) and must run sequentially
// to prevent race conditions when running in parallel with other tests
[Collection(nameof(ConsoleExporterTests))]
public sealed class ConsoleExporterTests : IDisposable
{
    private readonly TextWriter _originalOutput;
    private readonly StringBuilder _stringBuilder;
    private readonly StringWriter _stringWriter;

    private readonly ConsoleExporter _exporter = new();

    public ConsoleExporterTests()
    {
        _originalOutput = Console.Out;
        _stringBuilder = new();
        _stringWriter = new(_stringBuilder);
        Console.SetOut(_stringWriter);
    }

    public void Dispose()
    {
        _stringWriter.Dispose();
        Console.SetOut(_originalOutput);
    }


    [Fact]
    public void When_Doesnt_Have_Warnings_And_Results_Should_Print_Only_Message_NoBenchmarksFound()
    {
        // Arrange
        var report = new GateReport();


        // Act
        _exporter.Generate(report, "");


        // Assert
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                            BENCHMARK GATE REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "   No benchmarks found.",
            "",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
    }

    [Fact]
    public void When_Has_Only_Warnings_Should_Print_Only_Warnings()
    {
        // Arrange
        var report = new GateReport
        {
            Warnings = [
                "Warning 1",
                "Warning 2"
            ]
        };


        // Act
        _exporter.Generate(report, "");


        // Assert
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                            BENCHMARK GATE REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "⚠️ WARNINGS:",
            "",
            "   • Warning 1",
            "   • Warning 2",
            "",
            ".................................................................................",
            "",
            "📊 RESULTS:",
            "",
            "   No benchmarks found.",
            "",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
    }

    [Fact]
    public void When_Has_One_Result_Should_Print_OneRow_In_Table()
    {
        // Arrange
        var report = new GateReport();
        report.Results.Add(new()
        {
            Type = "Bmk",
            Name = "Name",
            FullName = "Full",
            Mean = 12,
            Allocated = 20
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                            BENCHMARK GATE REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "Type     Method     Mean      Allocated",
            "───────────────────────────────────────",
            "Bmk      Name       12 ns     20 B     ",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
    }

    [Fact]
    public void When_Has_HitThresholds_Should_Print_Them()
    {
        // Arrange
        var report = new GateReport();
        report.HitThresholds.Add("Hit Threshold 1");
        report.HitThresholds.Add("Hit Threshold 2");


        // Act
        _exporter.Generate(report, "");


        // Assert
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                            BENCHMARK GATE REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "   No benchmarks found.",
            "",
            "",
            ".................................................................................",
            "",
            "🚨 THRESHOLD VIOLATIONS:",
            "",
            "   • Hit Threshold 1",
            "   • Hit Threshold 2",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
    }

    private void _outputShouldBe(params string[] expectedLines)
    {
        var lines = _stringBuilder.ToString().Split(Environment.NewLine);

        for(var i = 0; i < expectedLines.Length; i++)
        {
            lines[i].Should().Be(expectedLines[i]);
        }
    }
}
