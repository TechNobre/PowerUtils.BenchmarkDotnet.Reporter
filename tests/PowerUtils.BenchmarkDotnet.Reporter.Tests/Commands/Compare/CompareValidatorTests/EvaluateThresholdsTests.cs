using System;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models.ComparerReport;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.CompareValidatorTests;

public sealed class EvaluateThresholdsTests
{
    private readonly CompareValidator _validator = new();


    [Fact]
    public void When_Has_Invalid_Mean_Threshold_Should_Throw_Exception()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        void act() => _validator.EvaluateThresholds(report, "invalid", null);


        // Assert
        Should.Throw<FormatException>(act);
    }

    [Fact]
    public void When_Has_Invalid_Allocation_Threshold_Should_Throw_Exception()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        void act() => _validator.EvaluateThresholds(report, null, "invalid");


        // Assert
        Should.Throw<FormatException>(act);
    }

    [Fact]
    public void Should_Register_Thrashold_Values_Above_Setted_PercentsThrashold()
    {
        // Arrange
        var report = new ComparerReport();
        report.Add(new Comparison
        {
            Type = "T",
            Name = "test hit",
            FullName = "test hit",
            Mean = MetricComparison.CalculateExecutionTime(12, 1200),
            Allocated = MetricComparison.CalculateMemoryUsage(120, 120000)
        });
        report.Add(new Comparison
        {
            Type = "T",
            Name = "test equals",
            FullName = "test equals",
            Mean = MetricComparison.CalculateExecutionTime(45, 45),
            Allocated = MetricComparison.CalculateMemoryUsage(1234, 1234)
        });


        // Act
        _validator.EvaluateThresholds(report, "10%", "11%");


        // Assert
        report.HitThresholds.ShouldContain("Mean threshold hit for 'test hit'");
        report.HitThresholds.ShouldContain("Allocation threshold hit for 'test hit'");
    }

    [Fact]
    public void Should_Register_Thrashold_Values_Above_Setted_UnitThrashold()
    {
        // Arrange
        var report = new ComparerReport();
        report.Add(new Comparison
        {
            Type = "T",
            Name = "test hit",
            FullName = "test hit",
            Mean = MetricComparison.CalculateExecutionTime(12, 1200),
            Allocated = MetricComparison.CalculateMemoryUsage(120, 120000)
        });


        // Act
        _validator.EvaluateThresholds(report, "5ns", "5B");


        // Assert
        report.HitThresholds.ShouldContain("Mean threshold hit for 'test hit'");
        report.HitThresholds.ShouldContain("Allocation threshold hit for 'test hit'");
    }

    [Fact]
    public void When_Diff_Is_Exactly_Equal_To_Percentage_Threshold_Should_Not_Register_Hit()
    {
        // Arrange
        var report = new ComparerReport();
        report.Add(new Comparison
        {
            Type = "T",
            Name = "test equal to threshold",
            FullName = "test equal to threshold",
            Mean = MetricComparison.CalculateExecutionTime(100, 110)
        });


        // Act
        _validator.EvaluateThresholds(report, "10%", null);


        // Assert
        report.HitThresholds.ShouldBeEmpty();
    }

    [Fact]
    public void When_Diff_Is_Exactly_Equal_To_Unit_Threshold_Should_Not_Register_Hit()
    {
        // Arrange
        var report = new ComparerReport();
        report.Add(new Comparison
        {
            Type = "T",
            Name = "test equal to threshold",
            FullName = "test equal to threshold",
            Mean = MetricComparison.CalculateExecutionTime(10, 15)
        });


        // Act
        _validator.EvaluateThresholds(report, "5ns", null);


        // Assert
        report.HitThresholds.ShouldBeEmpty();
    }

    [Fact]
    public void When_Threshold_Is_Unit_Based_Should_Not_Evaluate_Percentage_Diff()
    {
        // Arrange
        // Absolute diff (1ns) is below the 5ns threshold, but the percentage diff (10%) would exceed
        // a value of 5 if it were (incorrectly) evaluated as a percentage.
        var report = new ComparerReport();
        report.Add(new Comparison
        {
            Type = "T",
            Name = "test unit only",
            FullName = "test unit only",
            Mean = MetricComparison.CalculateExecutionTime(10, 11)
        });


        // Act
        _validator.EvaluateThresholds(report, "5ns", null);


        // Assert
        report.HitThresholds.ShouldBeEmpty();
    }

    [Fact]
    public void When_Threshold_Is_Percentage_Based_Should_Not_Evaluate_Absolute_Diff()
    {
        // Arrange
        // Percentage diff (1%) is below the 5% threshold, but the absolute diff (10ns) would exceed
        // a value of 5 if it were (incorrectly) evaluated as an absolute difference.
        var report = new ComparerReport();
        report.Add(new Comparison
        {
            Type = "T",
            Name = "test percentage only",
            FullName = "test percentage only",
            Mean = MetricComparison.CalculateExecutionTime(1000, 1010)
        });


        // Act
        _validator.EvaluateThresholds(report, "5%", null);


        // Assert
        report.HitThresholds.ShouldBeEmpty();
    }
}
