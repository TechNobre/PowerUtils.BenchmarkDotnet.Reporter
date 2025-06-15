using System;
using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Helpers.IOHelpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Exporters;

public sealed class ConsoleExporterTests
{
    private readonly ConsoleExporter _exporter;
    private readonly List<string> _output = [];

    public ConsoleExporterTests()
    {
        Printer printer = _output.Add;
        _exporter = new ConsoleExporter(printer);
    }


    [Fact]
    public void When_Doest_Have_Warnings_And_Results_Should_Print_Only_Message_NoComparisonsFound()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe(Environment.NewLine);
        _output[1].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[2].ShouldBe(Environment.NewLine);
        _output[3].ShouldBe("                        BENCHMARK COMPARISON REPORT");
        _output[4].ShouldBe(Environment.NewLine);
        _output[5].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[6].ShouldBe(Environment.NewLine);
        _output[7].ShouldBe(Environment.NewLine);
        _output[8].ShouldBe("📊 RESULTS:");
        _output[9].ShouldBe(Environment.NewLine);
        _output[10].ShouldBe(Environment.NewLine);
        _output[11].ShouldBe("   No comparisons found.");
        _output[12].ShouldBe(Environment.NewLine);
        _output[13].ShouldBe(Environment.NewLine);
        _output[14].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[15].ShouldBe(Environment.NewLine);
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

        _output[0].ShouldBe(Environment.NewLine);
        _output[1].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[2].ShouldBe(Environment.NewLine);
        _output[3].ShouldBe("                        BENCHMARK COMPARISON REPORT");
        _output[4].ShouldBe(Environment.NewLine);
        _output[5].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[6].ShouldBe(Environment.NewLine);
        _output[7].ShouldBe(Environment.NewLine);
        _output[8].ShouldBe("⚠️ WARNINGS:");
        _output[9].ShouldBe(Environment.NewLine);
        _output[10].ShouldBe(Environment.NewLine);
        _output[11].ShouldBe("   • Warning 1");
        _output[12].ShouldBe(Environment.NewLine);
        _output[13].ShouldBe("   • Warning 2");
        _output[14].ShouldBe(Environment.NewLine);
        _output[15].ShouldBe(Environment.NewLine);
        _output[16].ShouldBe(".................................................................................");
        _output[17].ShouldBe(Environment.NewLine);
        _output[18].ShouldBe(Environment.NewLine);
        _output[19].ShouldBe("📊 RESULTS:");
        _output[20].ShouldBe(Environment.NewLine);
        _output[21].ShouldBe(Environment.NewLine);
        _output[22].ShouldBe("   No comparisons found.");
        _output[23].ShouldBe(Environment.NewLine);
        _output[24].ShouldBe(Environment.NewLine);
        _output[25].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[26].ShouldBe(Environment.NewLine);
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
        _output[0].ShouldBe(Environment.NewLine);
        _output[1].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[2].ShouldBe(Environment.NewLine);
        _output[3].ShouldBe("                        BENCHMARK COMPARISON REPORT");
        _output[4].ShouldBe(Environment.NewLine);
        _output[5].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[6].ShouldBe(Environment.NewLine);
        _output[7].ShouldBe(Environment.NewLine);
        _output[8].ShouldBe("📊 RESULTS:");
        _output[9].ShouldBe(Environment.NewLine);
        _output[10].ShouldBe(Environment.NewLine);
        _output[11].ShouldBe("Report       Type     Method     Mean      Allocated");
        _output[12].ShouldBe(Environment.NewLine);
        _output[13].ShouldBe("────────────────────────────────────────────────────");
        _output[14].ShouldBe(Environment.NewLine);
        _output[15].ShouldBe("Baseline     Bmk      Name       12 ns     20 B");
        _output[16].ShouldBe(Environment.NewLine);
        _output[17].ShouldBe("Target                           12 ns     20 B");
        _output[18].ShouldBe(Environment.NewLine);
        _output[19].ShouldBe(Environment.NewLine);
        _output[20].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[21].ShouldBe(Environment.NewLine);
    }

    [Fact]
    public void When_Have_TwoResults_Should_Print_FourRows_In_Table()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk7",
            Name = "Method1",
            FullName = "FullMethod1",

            Mean = MetricComparison.CalculateExecutionTime(43, 43),
            Allocated = MetricComparison.CalculateMemoryUsage(122, 122)
        });
        report.Comparisons.Add(new()
        {
            Type = "Bmk8",
            Name = "Method2",
            FullName = "FullMethod2",

            Mean = MetricComparison.CalculateExecutionTime(52, 52),
            Allocated = MetricComparison.CalculateMemoryUsage(21, 21)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe(Environment.NewLine);
        _output[1].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[2].ShouldBe(Environment.NewLine);
        _output[3].ShouldBe("                        BENCHMARK COMPARISON REPORT");
        _output[4].ShouldBe(Environment.NewLine);
        _output[5].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[6].ShouldBe(Environment.NewLine);
        _output[7].ShouldBe(Environment.NewLine);
        _output[8].ShouldBe("📊 RESULTS:");
        _output[9].ShouldBe(Environment.NewLine);
        _output[10].ShouldBe(Environment.NewLine);
        _output[11].ShouldBe("Report       Type     Method      Mean      Allocated");
        _output[12].ShouldBe(Environment.NewLine);
        _output[13].ShouldBe("─────────────────────────────────────────────────────");
        _output[14].ShouldBe(Environment.NewLine);
        _output[15].ShouldBe("Baseline     Bmk7     Method1     43 ns     122 B");
        _output[16].ShouldBe(Environment.NewLine);
        _output[17].ShouldBe("Target                            43 ns     122 B");
        _output[18].ShouldBe(Environment.NewLine);
        _output[19].ShouldBe("Baseline     Bmk8     Method2     52 ns     21 B");
        _output[20].ShouldBe(Environment.NewLine);
        _output[21].ShouldBe("Target                            52 ns     21 B");
        _output[22].ShouldBe(Environment.NewLine);
        _output[23].ShouldBe(Environment.NewLine);
        _output[24].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[25].ShouldBe(Environment.NewLine);
    }

    [Fact]
    public void When_Benchmarks_Have_Different_Values_Should_Print_Differences_In_ResultRows()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk3",
            Name = "Method1",
            FullName = "FullMethod1",

            Mean = MetricComparison.CalculateExecutionTime(50, 25),
            Allocated = MetricComparison.CalculateMemoryUsage(100, 75)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe(Environment.NewLine);
        _output[1].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[2].ShouldBe(Environment.NewLine);
        _output[3].ShouldBe("                        BENCHMARK COMPARISON REPORT");
        _output[4].ShouldBe(Environment.NewLine);
        _output[5].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[6].ShouldBe(Environment.NewLine);
        _output[7].ShouldBe(Environment.NewLine);
        _output[8].ShouldBe("📊 RESULTS:");
        _output[9].ShouldBe(Environment.NewLine);
        _output[10].ShouldBe(Environment.NewLine);
        _output[11].ShouldBe("Report       Type     Method      Mean             Allocated");
        _output[12].ShouldBe(Environment.NewLine);
        _output[13].ShouldBe("──────────────────────────────────────────────────────────────");
        _output[14].ShouldBe(Environment.NewLine);
        _output[15].ShouldBe("Baseline     Bmk3     Method1     50 ns            100 B");
        _output[16].ShouldBe(Environment.NewLine);
        _output[17].ShouldBe("Target                            25 ns (-50%)     75 B (-25%)");
        _output[18].ShouldBe(Environment.NewLine);
        _output[19].ShouldBe(Environment.NewLine);
        _output[20].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[21].ShouldBe(Environment.NewLine);
    }

    [Fact]
    public void When_Baseline_Doesnt_Have_Values_Shouldnt_Print_Value_Only_TargetRow()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk1",
            Name = "xpto",
            FullName = "Full",

            Mean = MetricComparison.CalculateExecutionTime(null, 12),
            Allocated = MetricComparison.CalculateMemoryUsage(null, 37)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe(Environment.NewLine);
        _output[1].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[2].ShouldBe(Environment.NewLine);
        _output[3].ShouldBe("                        BENCHMARK COMPARISON REPORT");
        _output[4].ShouldBe(Environment.NewLine);
        _output[5].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[6].ShouldBe(Environment.NewLine);
        _output[7].ShouldBe(Environment.NewLine);
        _output[8].ShouldBe("📊 RESULTS:");
        _output[9].ShouldBe(Environment.NewLine);
        _output[10].ShouldBe(Environment.NewLine);
        _output[11].ShouldBe("Report       Type     Method     Mean      Allocated");
        _output[12].ShouldBe(Environment.NewLine);
        _output[13].ShouldBe("────────────────────────────────────────────────────");
        _output[14].ShouldBe(Environment.NewLine);
        _output[15].ShouldBe("Baseline     Bmk1     xpto                 ");
        _output[16].ShouldBe(Environment.NewLine);
        _output[17].ShouldBe("Target                [NEW]      12 ns     37 B");
        _output[18].ShouldBe(Environment.NewLine);
        _output[19].ShouldBe(Environment.NewLine);
        _output[20].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[21].ShouldBe(Environment.NewLine);
    }

    [Fact]
    public void When_Target_Doesnt_Have_Target_Values_Shouldnt_Print_Only_BaselineRow()
    {
        // Arrange
        var report = new ComparerReport();
        report.Comparisons.Add(new()
        {
            Type = "Bmk9",
            Name = "wdcs",
            FullName = "Full",

            Mean = MetricComparison.CalculateExecutionTime(12, null),
            Allocated = MetricComparison.CalculateMemoryUsage(20, null)
        });


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].ShouldBe(Environment.NewLine);
        _output[1].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[2].ShouldBe(Environment.NewLine);
        _output[3].ShouldBe("                        BENCHMARK COMPARISON REPORT");
        _output[4].ShouldBe(Environment.NewLine);
        _output[5].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[6].ShouldBe(Environment.NewLine);
        _output[7].ShouldBe(Environment.NewLine);
        _output[8].ShouldBe("📊 RESULTS:");
        _output[9].ShouldBe(Environment.NewLine);
        _output[10].ShouldBe(Environment.NewLine);
        _output[11].ShouldBe("Report       Type     Method        Mean      Allocated");
        _output[12].ShouldBe(Environment.NewLine);
        _output[13].ShouldBe("───────────────────────────────────────────────────────");
        _output[14].ShouldBe(Environment.NewLine);
        _output[15].ShouldBe("Baseline     Bmk9     wdcs          12 ns     20 B");
        _output[16].ShouldBe(Environment.NewLine);
        _output[17].ShouldBe("Target                [REMOVED]               ");
        _output[18].ShouldBe(Environment.NewLine);
        _output[19].ShouldBe(Environment.NewLine);
        _output[20].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[21].ShouldBe(Environment.NewLine);
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
        var targetLine = _output?[^5];
        var methodColumn = targetLine?
            .Split([' '], StringSplitOptions.RemoveEmptyEntries)[1]
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
        _output[0].ShouldBe(Environment.NewLine);
        _output[1].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[2].ShouldBe(Environment.NewLine);
        _output[3].ShouldBe("                        BENCHMARK COMPARISON REPORT");
        _output[4].ShouldBe(Environment.NewLine);
        _output[5].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[6].ShouldBe(Environment.NewLine);
        _output[7].ShouldBe(Environment.NewLine);
        _output[8].ShouldBe("📊 RESULTS:");
        _output[9].ShouldBe(Environment.NewLine);
        _output[10].ShouldBe(Environment.NewLine);
        _output[11].ShouldBe("   No comparisons found.");
        _output[12].ShouldBe(Environment.NewLine);
        _output[13].ShouldBe(Environment.NewLine);
        _output[14].ShouldBe(".................................................................................");
        _output[15].ShouldBe(Environment.NewLine);
        _output[16].ShouldBe(Environment.NewLine);
        _output[17].ShouldBe("🚨 THRESHOLD VIOLATIONS:");
        _output[18].ShouldBe(Environment.NewLine);
        _output[19].ShouldBe(Environment.NewLine);
        _output[20].ShouldBe("   • Hit Threshold 1");
        _output[21].ShouldBe(Environment.NewLine);
        _output[22].ShouldBe("   • Hit Threshold 2");
        _output[23].ShouldBe(Environment.NewLine);
        _output[24].ShouldBe(Environment.NewLine);
        _output[25].ShouldBe("══════════════════════════════════════════════════════════════════════════════════");
        _output[26].ShouldBe(Environment.NewLine);
    }
}
