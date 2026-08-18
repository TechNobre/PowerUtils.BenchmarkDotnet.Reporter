using System;
using System.Collections.Generic;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration.PbReporterConfiguration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.Options;

public sealed class BaselineTargetOptionTests
{
    private readonly Command _command;

    public BaselineTargetOptionTests()
    {
        var handler = new CompareHandler(
            Substitute.For<Func<string?, List<BenchmarkReport>>>(),
            Substitute.For<ICompareValidator>(),
            Substitute.For<IKeyedServiceProvider>());
        _command = new CompareCommand(handler).Build();
    }

    [Fact]
    public void CompareCommand_ShouldHave_BaselineAndTargetOptions_NotRequired()
    {
        // Assert
        // Required=false at the System.CommandLine level: baseline/target can now come from
        // env vars or the YAML config file instead of the CLI.
        CompareOptions.BaselineOption.Required.Should().BeFalse();
        CompareOptions.TargetOption.Required.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithCliValuesOnly_ShouldUse_CliValues()
    {
        // Arrange
        var parseResult = _command.Parse("-b cli-baseline.json -t cli-target.json");

        // Act
        var options = CompareOptions.Parse(parseResult);

        // Assert
        options.Baseline.Should().Be("cli-baseline.json");
        options.Target.Should().Be("cli-target.json");
    }

    [Fact]
    public void Parse_WithConfigurationValuesOnly_ShouldUse_ConfigurationValues()
    {
        // Arrange
        var parseResult = _command.Parse(string.Empty);
        var configuration = new CompareConfigurationSection
        {
            Baseline = "config-baseline.json",
            Target = "config-target.json"
        };

        // Act
        var options = CompareOptions.Parse(parseResult, configuration);

        // Assert
        options.Baseline.Should().Be("config-baseline.json");
        options.Target.Should().Be("config-target.json");
    }

    [Fact]
    public void Parse_WithCliAndConfigurationValues_ShouldPrefer_CliValues()
    {
        // Arrange
        var parseResult = _command.Parse("-b cli-baseline.json -t cli-target.json");
        var configuration = new CompareConfigurationSection
        {
            Baseline = "config-baseline.json",
            Target = "config-target.json"
        };

        // Act
        var options = CompareOptions.Parse(parseResult, configuration);

        // Assert
        options.Baseline.Should().Be("cli-baseline.json");
        options.Target.Should().Be("cli-target.json");
    }

    [Fact]
    public void Parse_WithNoCliOrConfigurationValues_ShouldLeave_ThemNull()
    {
        // Arrange
        var parseResult = _command.Parse(string.Empty);

        // Act
        var options = CompareOptions.Parse(parseResult);

        // Assert
        options.Baseline.Should().BeNull();
        options.Target.Should().BeNull();
    }
}
