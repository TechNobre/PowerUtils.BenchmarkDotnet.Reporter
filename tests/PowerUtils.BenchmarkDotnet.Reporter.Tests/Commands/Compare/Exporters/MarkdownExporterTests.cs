using System;
using System.Collections.Generic;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.IOUtils;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.Exporters;

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
        _output[0].Should().Be("# BENCHMARK COMPARISON REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("    NO COMPARISONS FOUND.");
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
        _output[0].Should().Be("# BENCHMARK COMPARISON REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## ⚠️ WARNINGS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("    * Warning 1");
        _output[5].Should().Be("    * Warning 2");
        _output[6].Should().Be("");
        _output[7].Should().Be("");
        _output[8].Should().Be("## 📊 RESULTS:");
        _output[9].Should().Be("");
        _output[10].Should().Be("    NO COMPARISONS FOUND.");
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
        _output[0].Should().Be("# BENCHMARK COMPARISON REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("     Report   | Type | Method |  Mean | Allocated");
        _output[5].Should().Be("     -------- | ---- | ------ | -----:| ---------:");
        _output[6].Should().Be("     Baseline | Bmk  | Name   | 12 ns |      20 B");
        _output[7].Should().Be("     Target   |      |        | 12 ns |      20 B");
        _output[8].Should().Be("");
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
        _output[0].Should().Be("# BENCHMARK COMPARISON REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("     Report   | Type | Method  |  Mean | Allocated");
        _output[5].Should().Be("     -------- | ---- | ------- | -----:| ---------:");
        _output[6].Should().Be("     Baseline | Bmk  | Method1 | 43 ns |     122 B");
        _output[7].Should().Be("     Target   |      |         | 43 ns |     122 B");
        _output[8].Should().Be("     Baseline | Bmk2 | Method2 | 52 ns |      21 B");
        _output[9].Should().Be("     Target   |      |         | 52 ns |      21 B");
        _output[10].Should().Be("");
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
        _output[0].Should().Be("# BENCHMARK COMPARISON REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("     Report   | Type | Method |  Mean | Allocated");
        _output[5].Should().Be("     -------- | ---- | ------ | -----:| ---------:");
        _output[6].Should().Be("     Baseline | Bmk3 | xpto   |       |          ");
        _output[7].Should().Be("     Target   |      | [NEW]  | 12 ns |      37 B");
        _output[8].Should().Be("");
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
        _output[0].Should().Be("# BENCHMARK COMPARISON REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("     Report   | Type | Method    |  Mean | Allocated");
        _output[5].Should().Be("     -------- | ---- | --------- | -----:| ---------:");
        _output[6].Should().Be("     Baseline | Bmk5 | wdcs      | 12 ns |      20 B");
        _output[7].Should().Be("     Target   |      | [REMOVED] |       |          ");
        _output[8].Should().Be("");
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

        methodColumn.Should().Be(expected);
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
        _output[0].Should().Be("# BENCHMARK COMPARISON REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("    NO COMPARISONS FOUND.");
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
        var report = new ComparerReport();
        var outputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var expectedFileName = Path.Combine(outputDirectory, "benchmark-comparison-report.md");


        // Act
        output.Generate(report, outputDirectory);


        // Assert
        File.Exists(expectedFileName).Should().BeTrue();
    }

    [Fact]
    public void When_Report_Has_Gen0Collections_Should_Print_Gen0Collections_Column()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk1",
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
            Gen0Collections = MetricComparison.CalculateMemoryUsage(2000, 2),
            Allocated = MetricComparison.CalculateMemoryUsage(21, 21)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].Should().Be("# BENCHMARK COMPARISON REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("     Report   | Type | Method  |  Mean |       Gen0 | Allocated");
        _output[5].Should().Be("     -------- | ---- | ------- | -----:| ----------:| ---------:");
        _output[6].Should().Be("     Baseline | Bmk1 | Method1 | 43 ns |            |     122 B");
        _output[7].Should().Be("     Target   |      |         | 43 ns |            |     122 B");
        _output[8].Should().Be("     Baseline | Bmk2 | Method2 | 52 ns |       2000 |      21 B");
        _output[9].Should().Be("     Target   |      |         | 52 ns | 2 (-99.9%) |      21 B");
        _output[10].Should().Be("");
    }

    [Fact]
    public void When_Report_Has_Gen1Collections_Should_Print_Gen1Collections_Column()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk1",
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
            Gen1Collections = MetricComparison.CalculateMemoryUsage(100, 109),
            Allocated = MetricComparison.CalculateMemoryUsage(21, 21)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].Should().Be("# BENCHMARK COMPARISON REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("     Report   | Type | Method  |  Mean |     Gen1 | Allocated");
        _output[5].Should().Be("     -------- | ---- | ------- | -----:| --------:| ---------:");
        _output[6].Should().Be("     Baseline | Bmk1 | Method1 | 43 ns |          |     122 B");
        _output[7].Should().Be("     Target   |      |         | 43 ns |          |     122 B");
        _output[8].Should().Be("     Baseline | Bmk2 | Method2 | 52 ns |      100 |      21 B");
        _output[9].Should().Be("     Target   |      |         | 52 ns | 109 (9%) |      21 B");
        _output[10].Should().Be("");
    }

    [Fact]
    public void When_Report_Has_Gen2Collections_Should_Print_Gen2Collections_Column()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk1",
            Name = "Method1",
            FullName = "FullMethod1",

            Mean = MetricComparison.CalculateExecutionTime(43, 43),
            Gen2Collections = MetricComparison.CalculateMemoryUsage(352, 352),
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
        _output[0].Should().Be("# BENCHMARK COMPARISON REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("     Report   | Type | Method  |  Mean | Gen2 | Allocated");
        _output[5].Should().Be("     -------- | ---- | ------- | -----:| ----:| ---------:");
        _output[6].Should().Be("     Baseline | Bmk1 | Method1 | 43 ns |  352 |     122 B");
        _output[7].Should().Be("     Target   |      |         | 43 ns |  352 |     122 B");
        _output[8].Should().Be("     Baseline | Bmk2 | Method2 | 52 ns |      |      21 B");
        _output[9].Should().Be("     Target   |      |         | 52 ns |      |      21 B");
        _output[10].Should().Be("");
    }

    [Fact]
    public void When_Report_Has_Gen0Collections_Gen1Collections_Gen2Collections_Should_Print_Gen0Collections_Gen1Collections_Gen2Collections_Column()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk1",
            Name = "Method1",
            FullName = "FullMethod1",

            Mean = MetricComparison.CalculateExecutionTime(43, 43),
            Gen0Collections = MetricComparison.CalculateMemoryUsage(122, 132),
            Gen1Collections = MetricComparison.CalculateMemoryUsage(2000, 2000),
            Gen2Collections = MetricComparison.CalculateMemoryUsage(352, 332),
            Allocated = MetricComparison.CalculateMemoryUsage(122, 122)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].Should().Be("# BENCHMARK COMPARISON REPORT");
        _output[1].Should().Be("");
        _output[2].Should().Be("## 📊 RESULTS:");
        _output[3].Should().Be("");
        _output[4].Should().Be("     Report   | Type | Method  |  Mean |       Gen0 | Gen1 |         Gen2 | Allocated");
        _output[5].Should().Be("     -------- | ---- | ------- | -----:| ----------:| ----:| ------------:| ---------:");
        _output[6].Should().Be("     Baseline | Bmk1 | Method1 | 43 ns |        122 | 2000 |          352 |     122 B");
        _output[7].Should().Be("     Target   |      |         | 43 ns | 132 (8.2%) | 2000 | 332 (-5.68%) |     122 B");
        _output[8].Should().Be("");
    }
}
