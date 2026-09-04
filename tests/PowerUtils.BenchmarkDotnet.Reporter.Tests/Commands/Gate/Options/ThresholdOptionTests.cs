using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration.PbReporterConfiguration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate.Options;

public sealed class ThresholdOptionTests
{
    private readonly Command _command;

    public ThresholdOptionTests()
    {
        var handler = new GateHandler(
            Substitute.For<Func<string?, List<BenchmarkReport>>>(),
            Substitute.For<IGateValidator>(),
            Substitute.For<IKeyedServiceProvider>());
        _command = new GateCommand(handler).Build();
    }

    [Theory]
    [InlineData("--threshold-mean")]
    [InlineData("--threshold-allocation")]
    public void When_Threshold_Is_Bare_Value_Shouldnt_Have_Validation_Error(string option)
    {
        // Arrange & Act
        var parseResult = _command.Parse($"{option} 500ms");
        var result = parseResult.GetResult(_command.Options.Single(o => o.Name == option));

        // Assert
        result?.Errors.Count().Should().Be(0);
    }

    [Theory]
    [InlineData("--threshold-mean")]
    [InlineData("--threshold-allocation")]
    public void When_Threshold_Is_Scoped_Value_Shouldnt_Have_Validation_Error(string option)
    {
        // Arrange & Act
        var parseResult = _command.Parse($"{option} \"Demo.Benchmarks.ArrayProcessorBenchmarks.*=500ms\"");
        var result = parseResult.GetResult(_command.Options.Single(o => o.Name == option));

        // Assert
        result?.Errors.Count().Should().Be(0);
    }

    [Theory]
    [InlineData("--threshold-mean")]
    [InlineData("--threshold-allocation")]
    public void When_Threshold_Pattern_Has_Wildcard_Not_At_End_Should_Have_Validation_Error(string option)
    {
        // Arrange & Act
        var parseResult = _command.Parse($"{option} \"Demo.*.ArrayProcessorBenchmarks=500ms\"");
        var result = parseResult.GetResult(_command.Options.Single(o => o.Name == option));

        // Assert
        result?.Errors.Count().Should().Be(1);
        result?.Errors.Should().Contain(e => e.Message == "Invalid threshold pattern 'Demo.*.ArrayProcessorBenchmarks'. A '*' is only allowed as the last character of the pattern.");
    }

    [Theory]
    [InlineData("--threshold-mean")]
    [InlineData("--threshold-allocation")]
    public void When_Threshold_Has_Multiple_Bare_Values_Shouldnt_Have_Validation_Error(string option)
    {
        // Arrange & Act
        // Repeated bare values are allowed at the CLI token-shape level; '%' unit rejection happens
        // later, at evaluation time, via Gate's own TimeThreshold/MemoryThreshold value objects.
        var parseResult = _command.Parse($"{option} 5ms {option} 10ms");
        var result = parseResult.GetResult(_command.Options.Single(o => o.Name == option));

        // Assert
        result?.Errors.Count().Should().Be(0);
    }

    [Fact]
    public void Parse_ShouldBuild_GlobalOnly_ThresholdRule()
    {
        // Arrange
        var parseResult = _command.Parse("-i report.json --threshold-mean 500ms");

        // Act
        var options = GateOptions.Parse(parseResult);

        // Assert
        options.MeanThreshold.Should().ContainSingle(rule => rule.Key == "*" && rule.Value == "500ms");
    }

    [Fact]
    public void Parse_ShouldBuild_ScopedOnly_ThresholdRule()
    {
        // Arrange
        var parseResult = _command.Parse("-i report.json --threshold-mean \"Demo.Benchmarks.ArrayProcessorBenchmarks.*=1ms\"");

        // Act
        var options = GateOptions.Parse(parseResult);

        // Assert
        options.MeanThreshold.Should().ContainSingle(rule =>
            rule.Key == "Demo.Benchmarks.ArrayProcessorBenchmarks.*" && rule.Value == "1ms");
    }

    [Fact]
    public void Parse_ShouldBuild_Mixed_GlobalAndScoped_ThresholdRules()
    {
        // Arrange
        var parseResult = _command.Parse(
            "-i report.json --threshold-mean 500ms --threshold-mean \"Demo.Benchmarks.ArrayProcessorBenchmarks.*=1ms\"");

        // Act
        var options = GateOptions.Parse(parseResult);

        // Assert
        options.MeanThreshold.Should().HaveCount(2);
        options.MeanThreshold.Should().Contain(rule => rule.Key == "*" && rule.Value == "500ms");
        options.MeanThreshold.Should().Contain(rule => rule.Key == "Demo.Benchmarks.ArrayProcessorBenchmarks.*" && rule.Value == "1ms");
    }

    [Fact]
    public void Parse_WhenThresholdNotProvided_ShouldBuild_EmptyThresholdRules()
    {
        // Arrange
        var parseResult = _command.Parse("-i report.json");

        // Act
        var options = GateOptions.Parse(parseResult);

        // Assert
        options.MeanThreshold.Should().BeEmpty();
        options.AllocationThreshold.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhenOnlyConfigurationProvidesThreshold_ShouldUse_ConfigurationValue()
    {
        // Arrange
        var parseResult = _command.Parse("-i report.json");
        var configuration = new GateConfigurationSection { ThresholdMean = "500ms" };

        // Act
        var options = GateOptions.Parse(parseResult, configuration);

        // Assert
        options.MeanThreshold.Should().ContainSingle(rule => rule.Key == "*" && rule.Value == "500ms");
    }

    [Fact]
    public void Parse_WhenCliAndConfigurationSetSamePattern_ShouldPrefer_CliValue()
    {
        // Arrange
        var parseResult = _command.Parse("-i report.json --threshold-mean 500ms");
        var configuration = new GateConfigurationSection { ThresholdMean = "50ms" };

        // Act
        var options = GateOptions.Parse(parseResult, configuration);

        // Assert
        options.MeanThreshold.Should().ContainSingle(rule => rule.Key == "*" && rule.Value == "500ms");
    }

    [Fact]
    public void Parse_ShouldMerge_ConfigurationScopedRules_WithCliRules()
    {
        // Arrange
        var parseResult = _command.Parse("-i report.json --threshold-mean \"Demo.Benchmarks.ArrayProcessorBenchmarks.Method=2ms\"");
        var configuration = new GateConfigurationSection
        {
            ThresholdMean = "500ms",
            Thresholds =
            [
                new ScopedThresholdConfig { Pattern = "Demo.Benchmarks.ArrayProcessorBenchmarks.*", ThresholdMean = "100ms" }
            ]
        };

        // Act
        var options = GateOptions.Parse(parseResult, configuration);

        // Assert
        options.MeanThreshold.Should().HaveCount(3);
        options.MeanThreshold.Should().Contain(rule => rule.Key == "*" && rule.Value == "500ms");
        options.MeanThreshold.Should().Contain(rule => rule.Key == "Demo.Benchmarks.ArrayProcessorBenchmarks.*" && rule.Value == "100ms");
        options.MeanThreshold.Should().Contain(rule => rule.Key == "Demo.Benchmarks.ArrayProcessorBenchmarks.Method" && rule.Value == "2ms");
    }

    [Fact]
    public void Parse_WhenConfigurationScopedEntryHasNoValueForMetric_ShouldBeIgnored()
    {
        // Arrange
        var parseResult = _command.Parse("-i report.json");
        var configuration = new GateConfigurationSection
        {
            Thresholds =
            [
                new ScopedThresholdConfig { Pattern = "Demo.*", ThresholdAllocation = "5kb" }
            ]
        };

        // Act
        var options = GateOptions.Parse(parseResult, configuration);

        // Assert
        options.MeanThreshold.Should().BeEmpty();
        options.AllocationThreshold.Should().ContainSingle(rule => rule.Key == "Demo.*" && rule.Value == "5kb");
    }
}
