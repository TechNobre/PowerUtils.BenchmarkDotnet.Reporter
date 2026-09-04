using System;
using System.Collections.Generic;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.IOUtils;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate.Exporters;

public sealed class MarkdownExporterTests
{
    private readonly MarkdownExporter _exporter;
    private List<string> _output = [];

    public MarkdownExporterTests()
    {
        void writer(string path, string content)
            => _output = [.. content.Split(Environment.NewLine)];
        _exporter = new MarkdownExporter(writer);
    }


    [Fact]
    public void When_Doesnt_Have_Warnings_And_Results_Should_Print_Only_Message_NoBenchmarksFound()
    {
        // Arrange
        var report = new GateReport();


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].Should().Be("# BENCHMARK GATE REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("    NO BENCHMARKS FOUND.");
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
        _output[0].Should().Be("# BENCHMARK GATE REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## ⚠️ WARNINGS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("    * Warning 1");
        _output[5].Should().Be("    * Warning 2");
        _output[6].Should().Be("");
        _output[7].Should().Be("");
        _output[8].Should().Be("## 📊 RESULTS:");
        _output[9].Should().Be("");
        _output[10].Should().Be("    NO BENCHMARKS FOUND.");
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
        _output[0].Should().Be("# BENCHMARK GATE REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("     Type | Method |  Mean | Allocated");
        _output[5].Should().Be("     ---- | ------ | -----:| ---------:");
        _output[6].Should().Be("     Bmk  | Name   | 12 ns |      20 B");
        _output[7].Should().Be("");
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
        _output[0].Should().Be("# BENCHMARK GATE REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("    NO BENCHMARKS FOUND.");
        _output[5].Should().Be("");
        _output[6].Should().Be("## 🚨 THRESHOLD VIOLATIONS:");
        _output[7].Should().Be("");
        _output[8].Should().Be("    * Hit Threshold 1;");
        _output[9].Should().Be("    * Hit Threshold 2;");
        _output[10].Should().Be("");
    }

    [Fact]
    public void Validate_If_FileOutputMarkdown_Is_Created()
    {
        // Arrange
        FileWriter writer = WriteFile;
        var output = new MarkdownExporter(writer);
        var report = new GateReport();
        var outputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var expectedFileName = Path.Combine(outputDirectory, "benchmark-gate-report.md");


        // Act
        output.Generate(report, outputDirectory);


        // Assert
        File.Exists(expectedFileName).Should().BeTrue();
    }
}
