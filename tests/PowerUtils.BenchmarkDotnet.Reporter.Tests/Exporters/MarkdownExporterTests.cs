using System;
using System.Collections.Generic;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Helpers.IOHelpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Exporters;

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
    public void When_Doesnt_Have_Warnings_And_Results_Should_Print_Only_Message_NoComparisonsFound()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe("# BENCHMARK COMPARISON REPORT");
        _output[1].ShouldBe("");
        _output[2].ShouldBe("## 📊 RESULTS:");
        _output[3].ShouldBe("");
        _output[4].ShouldBe("    NO COMPARISONS FOUND.");
    }

    [Fact]
    public void When_Has_Only_Warnings_Should_Print_Only_Warnings()
    {
        // Arrange
        var report = new ComparerReport
        {
            Warnings = [
                "Warning 1",
                "Warning 2"
            ]
        };


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe("# BENCHMARK COMPARISON REPORT");
        _output[1].ShouldBe("");
        _output[2].ShouldBe("## ⚠️ WARNINGS:");
        _output[3].ShouldBe("");
        _output[4].ShouldBe("    * Warning 1");
        _output[5].ShouldBe("    * Warning 2");
        _output[6].ShouldBe("");
        _output[7].ShouldBe("");
        _output[8].ShouldBe("## 📊 RESULTS:");
        _output[9].ShouldBe("");
        _output[10].ShouldBe("    NO COMPARISONS FOUND.");
    }

    [Fact]
    public void When_All_Results_Are_Equals_Should_Print_Without_Labels()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk",
            Name = "Name",
            FullName = "Full",

            Mean = MetricComparison.CalculateExecutionTime(12, 12),
            Allocated = MetricComparison.CalculateMemoryUsage(20, 20)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe("# BENCHMARK COMPARISON REPORT");
        _output[1].ShouldBe("");
        _output[2].ShouldBe("## 📊 RESULTS:");
        _output[3].ShouldBe("");
        _output[4].ShouldBe("     Report   | Type | Method |  Mean | Allocated");
        _output[5].ShouldBe("     -------- | ---- | ------ | -----:| ---------:");
        _output[6].ShouldBe("     Baseline | Bmk  | Name   | 12 ns |      20 B");
        _output[7].ShouldBe("     Target   |      |        | 12 ns |      20 B");
        _output[8].ShouldBe("");
    }

    [Fact]
    public void When_Have_TwoResults_Should_Print_FourRows_In_Table()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk",
            Name = "Method1",
            FullName = "FullMethod1",

            Mean = MetricComparison.CalculateExecutionTime(43, 43),
            Allocated = MetricComparison.CalculateMemoryUsage(122, 122)
        });
        report.Comparisons.Add(new()
        {
            Type = "Bmk2",
            Name = "Method2",
            FullName = "FullMethod2",

            Mean = MetricComparison.CalculateExecutionTime(52, 52),
            Allocated = MetricComparison.CalculateMemoryUsage(21, 21)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe("# BENCHMARK COMPARISON REPORT");
        _output[1].ShouldBe("");
        _output[2].ShouldBe("## 📊 RESULTS:");
        _output[3].ShouldBe("");
        _output[4].ShouldBe("     Report   | Type | Method  |  Mean | Allocated");
        _output[5].ShouldBe("     -------- | ---- | ------- | -----:| ---------:");
        _output[6].ShouldBe("     Baseline | Bmk  | Method1 | 43 ns |     122 B");
        _output[7].ShouldBe("     Target   |      |         | 43 ns |     122 B");
        _output[8].ShouldBe("     Baseline | Bmk2 | Method2 | 52 ns |      21 B");
        _output[9].ShouldBe("     Target   |      |         | 52 ns |      21 B");
        _output[10].ShouldBe("");
    }

    [Fact]
    public void When_Baseline_Doesnt_Have_Values_Shouldnt_Print_Value_Only_TargetRow()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk3",
            Name = "xpto",
            FullName = "Full",

            Mean = MetricComparison.CalculateExecutionTime(null, 12),
            Allocated = MetricComparison.CalculateMemoryUsage(null, 37)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe("# BENCHMARK COMPARISON REPORT");
        _output[1].ShouldBe("");
        _output[2].ShouldBe("## 📊 RESULTS:");
        _output[3].ShouldBe("");
        _output[4].ShouldBe("     Report   | Type | Method |  Mean | Allocated");
        _output[5].ShouldBe("     -------- | ---- | ------ | -----:| ---------:");
        _output[6].ShouldBe("     Baseline | Bmk3 | xpto   |       |          ");
        _output[7].ShouldBe("     Target   |      | [NEW]  | 12 ns |      37 B");
        _output[8].ShouldBe("");
    }

    [Fact]
    public void When_Target_Doesnt_Have_Target_Values_Shouldnt_Print_Only_BaselineRow()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk5",
            Name = "wdcs",
            FullName = "Full",

            Mean = MetricComparison.CalculateExecutionTime(12, null),
            Allocated = MetricComparison.CalculateMemoryUsage(20, null)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe("# BENCHMARK COMPARISON REPORT");
        _output[1].ShouldBe("");
        _output[2].ShouldBe("## 📊 RESULTS:");
        _output[3].ShouldBe("");
        _output[4].ShouldBe("     Report   | Type | Method    |  Mean | Allocated");
        _output[5].ShouldBe("     -------- | ---- | --------- | -----:| ---------:");
        _output[6].ShouldBe("     Baseline | Bmk5 | wdcs      | 12 ns |      20 B");
        _output[7].ShouldBe("     Target   |      | [REMOVED] |       |          ");
        _output[8].ShouldBe("");
    }

    [Theory]
    [InlineData(ComparisonStatus.Removed, "REMOVED")]
    [InlineData(ComparisonStatus.New, "NEW")]
    public void When_Has_Status_To_Show_Lable_Should_Show_Correspondent_Name(ComparisonStatus status, string expected)
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk",
            Name = "Name",
            FullName = "Full",

            Mean = status == ComparisonStatus.Removed
                ? MetricComparison.CalculateExecutionTime(12, null)
                : MetricComparison.CalculateMemoryUsage(null, 120)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        var targetLine = _output?[^2];
        var methodColumn = targetLine?
            .Split('|')[2]
            .Trim(' ', '[', ']');

        methodColumn.ShouldBe(expected);
    }

    [Fact]
    public void When_Has_HitThresholds_Should_Print_Them()
    {
        // Arrange
        var report = new ComparerReport();
        report.HitThresholds.Add("Hit Threshold 1");
        report.HitThresholds.Add("Hit Threshold 2");


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe("# BENCHMARK COMPARISON REPORT");
        _output[1].ShouldBe("");
        _output[2].ShouldBe("## 📊 RESULTS:");
        _output[3].ShouldBe("");
        _output[4].ShouldBe("    NO COMPARISONS FOUND.");
        _output[5].ShouldBe("");
        _output[6].ShouldBe("## 🚨 THRESHOLD VIOLATIONS:");
        _output[7].ShouldBe("");
        _output[8].ShouldBe("    * Hit Threshold 1;");
        _output[9].ShouldBe("    * Hit Threshold 2;");
        _output[10].ShouldBe("");
    }

    [Fact]
    public void Validate_If_FileOutputMarkdown_Is_Created()
    {
        // Arrange
        FileWriter writer = WriteFile;
        var output = new MarkdownExporter(writer);
        var report = new ComparerReport();
        var outputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var expectedFileName = Path.Combine(outputDirectory, "benchmark-comparison-report.md");


        // Act
        output.Generate(report, outputDirectory);


        // Assert
        File.Exists(expectedFileName).ShouldBeTrue();
    }
}
