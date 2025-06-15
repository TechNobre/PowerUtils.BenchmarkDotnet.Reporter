using PowerUtils.BenchmarkDotnet.Reporter.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Models.ComparerReport;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Models;

public sealed class ComparerReportTests
{
    [Fact]
    public void When_Comparison_Hasnt_Mean_And_Allocated_Shouldnt_Add()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        report.Add(new Comparison
        {
            Type = null,
            Name = null,
            FullName = null,

            Mean = null,
            Allocated = null
        });


        // Assert
        report.Comparisons.ShouldBeEmpty();
    }

    [Fact]
    public void When_Comparison_Has_Mean_Should_Add()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        report.Add(new Comparison
        {
            Type = null,
            Name = null,
            FullName = null,

            Mean = MetricComparison.CalculateExecutionTime(1, 1),
            Allocated = null
        });


        // Assert
        report.Comparisons.Count.ShouldBe(1);
    }

    [Fact]
    public void When_Comparison_Has_Allocated_Should_Add()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        report.Add(new Comparison
        {
            Type = null,
            Name = null,
            FullName = null,

            Mean = null,
            Allocated = MetricComparison.CalculateMemoryUsage(1, 1)
        });


        // Assert
        report.Comparisons.Count.ShouldBe(1);
    }
}
