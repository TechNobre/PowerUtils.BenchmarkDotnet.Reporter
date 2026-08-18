using System;
using System.Collections.Generic;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models.ComparerReport;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.CompareValidatorTests;

public sealed class EvaluateThresholdsTests
{
    private readonly CompareValidator _validator = new();


    private static List<KeyValuePair<string, string>> _global(string value)
        => [new KeyValuePair<string, string>("*", value)];

    private static List<KeyValuePair<string, string>> _rules(params (string Pattern, string Value)[] rules)
        => Array.ConvertAll(rules, rule => new KeyValuePair<string, string>(rule.Pattern, rule.Value)).ToList();

    private static readonly List<KeyValuePair<string, string>> _none = [];


    [Fact]
    public void When_Has_Invalid_Mean_Threshold_Should_Throw_Exception()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        Action act = () => _validator.EvaluateThresholds(report, _global("invalid"), _none);


        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void When_Has_Invalid_Allocation_Threshold_Should_Throw_Exception()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        Action act = () => _validator.EvaluateThresholds(report, _none, _global("invalid"));


        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void When_Has_Invalid_Scoped_Mean_Threshold_Should_Throw_Exception()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        Action act = () => _validator.EvaluateThresholds(report, _rules(("My.Namespace.*", "invalid")), _none);


        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void When_Diffs_Exceed_Percentage_Thresholds_Should_Register_Hits()
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
        _validator.EvaluateThresholds(report, _global("10%"), _global("11%"));


        // Assert
        report.HitThresholds.Should().Contain("Mean threshold hit for 'test hit'");
        report.HitThresholds.Should().Contain("Allocation threshold hit for 'test hit'");
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
        _validator.EvaluateThresholds(report, _global("5ns"), _global("5B"));


        // Assert
        report.HitThresholds.Should().Contain("Mean threshold hit for 'test hit'");
        report.HitThresholds.Should().Contain("Allocation threshold hit for 'test hit'");
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
        _validator.EvaluateThresholds(report, _global("10%"), _none);


        // Assert
        report.HitThresholds.Should().BeEmpty();
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
        _validator.EvaluateThresholds(report, _global("5ns"), _none);


        // Assert
        report.HitThresholds.Should().BeEmpty();
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
        _validator.EvaluateThresholds(report, _global("5ns"), _none);


        // Assert
        report.HitThresholds.Should().BeEmpty();
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
        _validator.EvaluateThresholds(report, _global("5%"), _none);


        // Assert
        report.HitThresholds.Should().BeEmpty();
    }

    [Fact]
    public void When_Scoped_Rule_Matches_Should_Override_Global_Threshold()
    {
        // Arrange
        // Global 50% is loose enough to not hit; the scoped 5% rule for this FullName is tight and should hit instead.
        var report = new ComparerReport();
        report.Add(new Comparison
        {
            Type = "T",
            Name = "Method",
            FullName = "Demo.Benchmarks.ArrayProcessorBenchmarks.Method",
            Mean = MetricComparison.CalculateExecutionTime(100, 110)
        });

        var rules = _rules(
            ("*", "50%"),
            ("Demo.Benchmarks.ArrayProcessorBenchmarks.*", "5%"));


        // Act
        _validator.EvaluateThresholds(report, rules, _none);


        // Assert
        report.HitThresholds.Should()
            .Contain("Mean threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.Method' (rule: Demo.Benchmarks.ArrayProcessorBenchmarks.*)");
    }

    [Fact]
    public void When_No_Scoped_Rule_Matches_Should_Fallback_To_Global_Threshold()
    {
        // Arrange
        var report = new ComparerReport();
        report.Add(new Comparison
        {
            Type = "T",
            Name = "Method",
            FullName = "Demo.Benchmarks.StringProcessorBenchmarks.Method",
            Mean = MetricComparison.CalculateExecutionTime(100, 110)
        });

        var rules = _rules(
            ("*", "5%"),
            ("Demo.Benchmarks.ArrayProcessorBenchmarks.*", "50%"));


        // Act
        _validator.EvaluateThresholds(report, rules, _none);


        // Assert
        report.HitThresholds.Should()
            .Contain("Mean threshold hit for 'Demo.Benchmarks.StringProcessorBenchmarks.Method'");
    }

    [Fact]
    public void When_Multiple_Scoped_Rules_Match_Should_Use_Most_Specific()
    {
        // Arrange
        // The exact method rule (2%) is more specific than the class wildcard (50%) and should win, causing a hit.
        var report = new ComparerReport();
        report.Add(new Comparison
        {
            Type = "T",
            Name = "Method",
            FullName = "Demo.Benchmarks.ArrayProcessorBenchmarks.Method",
            Mean = MetricComparison.CalculateExecutionTime(100, 110)
        });

        var rules = _rules(
            ("Demo.Benchmarks.ArrayProcessorBenchmarks.*", "50%"),
            ("Demo.Benchmarks.ArrayProcessorBenchmarks.Method", "2%"));


        // Act
        _validator.EvaluateThresholds(report, rules, _none);


        // Assert
        report.HitThresholds.Should()
            .Contain("Mean threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.Method' (rule: Demo.Benchmarks.ArrayProcessorBenchmarks.Method)");
    }

    [Fact]
    public void When_No_Threshold_Configured_Should_Not_Register_Hits()
    {
        // Arrange
        var report = new ComparerReport();
        report.Add(new Comparison
        {
            Type = "T",
            Name = "Method",
            FullName = "Demo.Benchmarks.ArrayProcessorBenchmarks.Method",
            Mean = MetricComparison.CalculateExecutionTime(1, 1000)
        });


        // Act
        _validator.EvaluateThresholds(report, _none, _none);


        // Assert
        report.HitThresholds.Should().BeEmpty();
    }

    [Fact]
    public void When_A_Comparison_Matches_No_Configured_Rule_Should_Skip_It_Without_Registering_Hit()
    {
        // Arrange
        // Only a scoped rule for StringProcessorBenchmarks is configured (no catch-all '*' rule), so the
        // ArrayProcessorBenchmarks comparison matches nothing and must be skipped rather than evaluated.
        var report = new ComparerReport();
        report.Add(new Comparison
        {
            Type = "T",
            Name = "Method",
            FullName = "Demo.Benchmarks.ArrayProcessorBenchmarks.Method",
            Mean = MetricComparison.CalculateExecutionTime(1, 1000)
        });

        var rules = _rules(("Demo.Benchmarks.StringProcessorBenchmarks.*", "1ns"));


        // Act
        _validator.EvaluateThresholds(report, rules, _none);


        // Assert
        report.HitThresholds.Should().BeEmpty();
    }
}
