using System;
using System.IO;
using System.Text;
using PowerUtils.BenchmarkDotnet.Reporter.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Exporters;

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
        _stringBuilder.Clear();
        _stringWriter.Dispose();
        Console.SetOut(_originalOutput);
    }


    [Fact]
    public void When_Doest_Have_Warnings_And_Results_Should_Print_Only_Message_NoComparisonsFound()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        _exporter.Generate(report, "");


        // Assert
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "   No comparisons found.",
            "",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
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
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
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
            "   No comparisons found.",
            "",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
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
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "Report       Type     Method     Mean      Allocated",
            "────────────────────────────────────────────────────",
            "Baseline     Bmk      Name       12 ns     20 B     ",
            "Target                           12 ns     20 B     ",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
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
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "Report       Type     Method      Mean      Allocated",
            "─────────────────────────────────────────────────────",
            "Baseline     Bmk7     Method1     43 ns     122 B    ",
            "Target                            43 ns     122 B    ",
            "Baseline     Bmk8     Method2     52 ns     21 B     ",
            "Target                            52 ns     21 B     ",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
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
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "Report       Type     Method      Mean             Allocated  ",
            "──────────────────────────────────────────────────────────────",
            "Baseline     Bmk3     Method1     50 ns            100 B      ",
            "Target                            25 ns (-50%)     75 B (-25%)",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
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
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "Report       Type     Method     Mean      Allocated",
            "────────────────────────────────────────────────────",
            "Baseline     Bmk1     xpto                          ",
            "Target                [NEW]      12 ns     37 B     ",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
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
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "Report       Type     Method        Mean      Allocated",
            "───────────────────────────────────────────────────────",
            "Baseline     Bmk9     wdcs          12 ns     20 B     ",
            "Target                [REMOVED]                        ",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
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
        var lines = _stringBuilder.ToString().Split(Environment.NewLine);
        var targetLine = lines?[^4];
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
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "   No comparisons found.",
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
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "Report       Type     Method      Mean      Gen0           Allocated",
            "────────────────────────────────────────────────────────────────────",
            "Baseline     Bmk1     Method1     43 ns                    122 B    ",
            "Target                            43 ns                    122 B    ",
            "Baseline     Bmk2     Method2     52 ns     2000           21 B     ",
            "Target                            52 ns     2 (-99.9%)     21 B     ",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
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
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "Report       Type     Method      Mean      Gen1         Allocated",
            "──────────────────────────────────────────────────────────────────",
            "Baseline     Bmk1     Method1     43 ns                  122 B    ",
            "Target                            43 ns                  122 B    ",
            "Baseline     Bmk2     Method2     52 ns     100          21 B     ",
            "Target                            52 ns     109 (9%)     21 B     ",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
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
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "Report       Type     Method      Mean      Gen2     Allocated",
            "──────────────────────────────────────────────────────────────",
            "Baseline     Bmk1     Method1     43 ns     352      122 B    ",
            "Target                            43 ns     352      122 B    ",
            "Baseline     Bmk2     Method2     52 ns              21 B     ",
            "Target                            52 ns              21 B     ",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
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
        _outputShouldBe(
            "══════════════════════════════════════════════════════════════════════════════════",
            "                        BENCHMARK COMPARISON REPORT",
            "══════════════════════════════════════════════════════════════════════════════════",
            "",
            "📊 RESULTS:",
            "",
            "Report       Type     Method      Mean      Gen0           Gen1     Gen2             Allocated",
            "──────────────────────────────────────────────────────────────────────────────────────────────",
            "Baseline     Bmk1     Method1     43 ns     122            2000     352              122 B    ",
            "Target                            43 ns     132 (8.2%)     2000     332 (-5.68%)     122 B    ",
            "",
            "══════════════════════════════════════════════════════════════════════════════════",
            "");
    }

    private void _outputShouldBe(params string[] expectedLines)
    {
        var lines = _stringBuilder.ToString().Split(Environment.NewLine);

        for(var i = 0; i < expectedLines.Length; i++)
        {
            lines[i].ShouldBe(expectedLines[i]);
        }
    }
}
