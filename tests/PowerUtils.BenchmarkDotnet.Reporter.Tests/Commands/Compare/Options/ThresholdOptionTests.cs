using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration.PbReporterConfiguration;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration.PbReporterConfiguration.CompareConfigurationSection;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.Options;

public sealed class ThresholdOptionTests
{
    private readonly Command _command;

    public ThresholdOptionTests()
    {
        var handler = new CompareHandler(
            Substitute.For<Func<string?, List<BenchmarkReport>>>(),
            Substitute.For<ICompareValidator>(),
            Substitute.For<IKeyedServiceProvider>());
        _command = new CompareCommand(handler).Build();
    }

    [Theory]
    [InlineData("--threshold-mean")]
    [InlineData("--threshold-allocation")]
    public void When_Threshold_Is_Bare_Value_Shouldnt_Have_Validation_Error(string option)
    {
        // Arrange & Act
        var parseResult = _command.Parse($"{option} 5%");
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
        var parseResult = _command.Parse($"{option} \"Demo.Benchmarks.ArrayProcessorBenchmarks.*=5%\"");
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
        var parseResult = _command.Parse($"{option} \"Demo.*.ArrayProcessorBenchmarks=5%\"");
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
        // Repeated bare values are allowed; each becomes its own '*' rule and the last one wins at evaluation time.
        var parseResult = _command.Parse($"{option} 5% {option} 10ms");
        var result = parseResult.GetResult(_command.Options.Single(o => o.Name == option));

        // Assert
        result?.Errors.Count().Should().Be(0);
    }

    [Fact]
    public void Parse_ShouldBuild_GlobalOnly_ThresholdRule()
    {
        // Arrange
        var parseResult = _command.Parse("-b base.json -t target.json --threshold-mean 5%");

        // Act
        var options = CompareOptions.Parse(parseResult);

        // Assert
        options.MeanThreshold.Should().ContainSingle(rule => rule.Key == "*" && rule.Value == "5%");
    }

    [Fact]
    public void Parse_ShouldBuild_ScopedOnly_ThresholdRule()
    {
        // Arrange
        var parseResult = _command.Parse("-b base.json -t target.json --threshold-mean \"Demo.Benchmarks.ArrayProcessorBenchmarks.*=1ms\"");

        // Act
        var options = CompareOptions.Parse(parseResult);

        // Assert
        options.MeanThreshold.Should().ContainSingle(rule =>
            rule.Key == "Demo.Benchmarks.ArrayProcessorBenchmarks.*" && rule.Value == "1ms");
    }

    [Fact]
    public void Parse_ShouldBuild_Mixed_GlobalAndScoped_ThresholdRules()
    {
        // Arrange
        var parseResult = _command.Parse(
            "-b base.json -t target.json --threshold-mean 5% --threshold-mean \"Demo.Benchmarks.ArrayProcessorBenchmarks.*=1ms\"");

        // Act
        var options = CompareOptions.Parse(parseResult);

        // Assert
        options.MeanThreshold.Should().HaveCount(2);
        options.MeanThreshold.Should().Contain(rule => rule.Key == "*" && rule.Value == "5%");
        options.MeanThreshold.Should().Contain(rule => rule.Key == "Demo.Benchmarks.ArrayProcessorBenchmarks.*" && rule.Value == "1ms");
    }

    [Fact]
    public void Parse_WhenThresholdNotProvided_ShouldBuild_EmptyThresholdRules()
    {
        // Arrange
        var parseResult = _command.Parse("-b base.json -t target.json");

        // Act
        var options = CompareOptions.Parse(parseResult);

        // Assert
        options.MeanThreshold.Should().BeEmpty();
        options.AllocationThreshold.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhenOnlyConfigurationProvidesThreshold_ShouldUse_ConfigurationValue()
    {
        // Arrange
        var parseResult = _command.Parse("-b base.json -t target.json");
        var configuration = new CompareConfigurationSection { ThresholdMean = "5%" };

        // Act
        var options = CompareOptions.Parse(parseResult, configuration);

        // Assert
        options.MeanThreshold.Should().ContainSingle(rule => rule.Key == "*" && rule.Value == "5%");
    }

    [Fact]
    public void Parse_WhenCliAndConfigurationSetSamePattern_ShouldPrefer_CliValue()
    {
        // Arrange
        var parseResult = _command.Parse("-b base.json -t target.json --threshold-mean 5%");
        var configuration = new CompareConfigurationSection { ThresholdMean = "50%" };

        // Act
        var options = CompareOptions.Parse(parseResult, configuration);

        // Assert
        options.MeanThreshold.Should().ContainSingle(rule => rule.Key == "*" && rule.Value == "5%");
    }

    [Fact]
    public void Parse_ShouldMerge_ConfigurationScopedRules_WithCliRules()
    {
        // Arrange
        var parseResult = _command.Parse("-b base.json -t target.json --threshold-mean \"Demo.Benchmarks.ArrayProcessorBenchmarks.Method=2%\"");
        var configuration = new CompareConfigurationSection
        {
            ThresholdMean = "50%",
            Thresholds =
            [
                new ScopedThresholdConfig { Pattern = "Demo.Benchmarks.ArrayProcessorBenchmarks.*", ThresholdMean = "10%" }
            ]
        };

        // Act
        var options = CompareOptions.Parse(parseResult, configuration);

        // Assert
        options.MeanThreshold.Should().HaveCount(3);
        options.MeanThreshold.Should().Contain(rule => rule.Key == "*" && rule.Value == "50%");
        options.MeanThreshold.Should().Contain(rule => rule.Key == "Demo.Benchmarks.ArrayProcessorBenchmarks.*" && rule.Value == "10%");
        options.MeanThreshold.Should().Contain(rule => rule.Key == "Demo.Benchmarks.ArrayProcessorBenchmarks.Method" && rule.Value == "2%");
    }

    [Fact]
    public void Parse_WhenConfigurationScopedEntryHasNoValueForMetric_ShouldBeIgnored()
    {
        // Arrange
        var parseResult = _command.Parse("-b base.json -t target.json");
        var configuration = new CompareConfigurationSection
        {
            Thresholds =
            [
                new ScopedThresholdConfig { Pattern = "Demo.*", ThresholdAllocation = "5kb" }
            ]
        };

        // Act
        var options = CompareOptions.Parse(parseResult, configuration);

        // Assert
        options.MeanThreshold.Should().BeEmpty();
        options.AllocationThreshold.Should().ContainSingle(rule => rule.Key == "Demo.*" && rule.Value == "5kb");
    }
}
