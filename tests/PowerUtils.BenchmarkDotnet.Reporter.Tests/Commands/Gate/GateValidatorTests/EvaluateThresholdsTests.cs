using System;
using System.Collections.Generic;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate.GateValidatorTests;

public sealed class EvaluateThresholdsTests
{
    private readonly GateValidator _validator = new();


    private static List<KeyValuePair<string, string>> _global(string value)
        => [new KeyValuePair<string, string>("*", value)];

    private static List<KeyValuePair<string, string>> _rules(params (string Pattern, string Value)[] rules)
        => Array.ConvertAll(rules, rule => new KeyValuePair<string, string>(rule.Pattern, rule.Value)).ToList();

    private static readonly List<KeyValuePair<string, string>> _none = [];

    private static GateReport.Result _result(string fullName, decimal? mean = null, decimal? allocated = null)
        => new()
        {
            Type = "T",
            Name = fullName,
            FullName = fullName,
            Mean = mean,
            Allocated = allocated
        };


    [Fact]
    public void When_Has_Invalid_Mean_Threshold_Should_Throw_Exception()
    {
        // Arrange
        var report = new GateReport();


        // Act
        Action act = () => _validator.EvaluateThresholds(report, _global("invalid"), _none);


        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void When_Has_Invalid_Allocation_Threshold_Should_Throw_Exception()
    {
        // Arrange
        var report = new GateReport();


        // Act
        Action act = () => _validator.EvaluateThresholds(report, _none, _global("invalid"));


        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void When_Has_Percentage_Mean_Threshold_Should_Throw_Exception()
    {
        // Arrange
        // Gate checks absolute values only - there's no baseline to compute a percentage against.
        var report = new GateReport();


        // Act
        Action act = () => _validator.EvaluateThresholds(report, _global("5%"), _none);


        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void When_Has_Percentage_Allocation_Threshold_Should_Throw_Exception()
    {
        // Arrange
        var report = new GateReport();


        // Act
        Action act = () => _validator.EvaluateThresholds(report, _none, _global("5%"));


        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void When_Has_Invalid_Scoped_Mean_Threshold_Should_Throw_Exception()
    {
        // Arrange
        var report = new GateReport();


        // Act
        Action act = () => _validator.EvaluateThresholds(report, _rules(("My.Namespace.*", "invalid")), _none);


        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void When_Values_Exceed_Thresholds_Should_Register_Hits()
    {
        // Arrange
        var report = new GateReport();
        report.Results.Add(_result("test hit", mean: 1200, allocated: 120000));
        report.Results.Add(_result("test ok", mean: 45, allocated: 1234));


        // Act
        _validator.EvaluateThresholds(report, _global("500ns"), _global("1kb"));


        // Assert
        report.HitThresholds.Should().Contain("Mean threshold hit for 'test hit'");
        report.HitThresholds.Should().Contain("Allocation threshold hit for 'test hit'");
    }

    [Fact]
    public void When_Value_Is_Exactly_Equal_To_Threshold_Should_Not_Register_Hit()
    {
        // Arrange
        var report = new GateReport();
        report.Results.Add(_result("test equal to threshold", mean: 15));


        // Act
        _validator.EvaluateThresholds(report, _global("15ns"), _none);


        // Assert
        report.HitThresholds.Should().BeEmpty();
    }

    [Fact]
    public void When_Value_Is_Null_Should_Not_Register_Hit()
    {
        // Arrange
        // A benchmark with no Mean/Allocated data at all (e.g. missing statistics) shouldn't be checked.
        var report = new GateReport();
        report.Results.Add(_result("test no data"));


        // Act
        _validator.EvaluateThresholds(report, _global("1ns"), _global("1b"));


        // Assert
        report.HitThresholds.Should().BeEmpty();
    }

    [Fact]
    public void When_Scoped_Rule_Matches_Should_Override_Global_Threshold()
    {
        // Arrange
        // Global 500ns is loose enough to not hit; the scoped 50ns rule for this FullName is tight and should hit instead.
        var report = new GateReport();
        report.Results.Add(_result("Demo.Benchmarks.ArrayProcessorBenchmarks.Method", mean: 100));

        var rules = _rules(
            ("*", "500ns"),
            ("Demo.Benchmarks.ArrayProcessorBenchmarks.*", "50ns"));


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
        var report = new GateReport();
        report.Results.Add(_result("Demo.Benchmarks.StringProcessorBenchmarks.Method", mean: 100));

        var rules = _rules(
            ("*", "50ns"),
            ("Demo.Benchmarks.ArrayProcessorBenchmarks.*", "500ns"));


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
        // The exact method rule (50ns) is more specific than the class wildcard (500ns) and should win, causing a hit.
        var report = new GateReport();
        report.Results.Add(_result("Demo.Benchmarks.ArrayProcessorBenchmarks.Method", mean: 100));

        var rules = _rules(
            ("Demo.Benchmarks.ArrayProcessorBenchmarks.*", "500ns"),
            ("Demo.Benchmarks.ArrayProcessorBenchmarks.Method", "50ns"));


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
        var report = new GateReport();
        report.Results.Add(_result("Demo.Benchmarks.ArrayProcessorBenchmarks.Method", mean: 1000));


        // Act
        _validator.EvaluateThresholds(report, _none, _none);


        // Assert
        report.HitThresholds.Should().BeEmpty();
    }

    [Fact]
    public void When_A_Result_Matches_No_Configured_Rule_Should_Skip_It_Without_Registering_Hit()
    {
        // Arrange
        // Only a scoped rule for StringProcessorBenchmarks is configured (no catch-all '*' rule), so the
        // ArrayProcessorBenchmarks result matches nothing and must be skipped rather than evaluated.
        var report = new GateReport();
        report.Results.Add(_result("Demo.Benchmarks.ArrayProcessorBenchmarks.Method", mean: 1000));

        var rules = _rules(("Demo.Benchmarks.StringProcessorBenchmarks.*", "1ns"));


        // Act
        _validator.EvaluateThresholds(report, rules, _none);


        // Assert
        report.HitThresholds.Should().BeEmpty();
    }
}
