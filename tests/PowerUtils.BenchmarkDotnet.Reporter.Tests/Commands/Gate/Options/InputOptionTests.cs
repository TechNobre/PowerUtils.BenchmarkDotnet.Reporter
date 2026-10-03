using System;
using System.Collections.Generic;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration.PbReporterConfiguration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate.Options;

public sealed class InputOptionTests
{
    private readonly Command _command;

    public InputOptionTests()
    {
        var handler = new GateHandler(
            Substitute.For<Func<string?, List<BenchmarkReport>>>(),
            Substitute.For<IGateValidator>(),
            Substitute.For<IKeyedServiceProvider>());
        _command = new GateCommand(handler).Build();
    }

    [Fact]
    public void GateCommand_ShouldHave_InputOption_NotRequired() =>
        // Assert
        // Required=false at the System.CommandLine level: input can come from env vars or the YAML config file instead of the CLI.
        GateOptions.InputOption.Required.Should().BeFalse();

    [Fact]
    public void Parse_WithCliValueOnly_ShouldUse_CliValue()
    {
        // Arrange
        var parseResult = _command.Parse("-i cli-report.json");

        // Act
        var options = GateOptions.Parse(parseResult);

        // Assert
        options.Input.Should().Be("cli-report.json");
    }

    [Fact]
    public void Parse_WithConfigurationValueOnly_ShouldUse_ConfigurationValue()
    {
        // Arrange
        var parseResult = _command.Parse(string.Empty);
        var configuration = new GateConfigurationSection { Input = "config-report.json" };

        // Act
        var options = GateOptions.Parse(parseResult, configuration);

        // Assert
        options.Input.Should().Be("config-report.json");
    }

    [Fact]
    public void Parse_WithCliAndConfigurationValue_ShouldPrefer_CliValue()
    {
        // Arrange
        var parseResult = _command.Parse("-i cli-report.json");
        var configuration = new GateConfigurationSection { Input = "config-report.json" };

        // Act
        var options = GateOptions.Parse(parseResult, configuration);

        // Assert
        options.Input.Should().Be("cli-report.json");
    }

    [Fact]
    public void Parse_WithNoCliOrConfigurationValue_ShouldLeave_ItNull()
    {
        // Arrange
        var parseResult = _command.Parse(string.Empty);

        // Act
        var options = GateOptions.Parse(parseResult);

        // Assert
        options.Input.Should().BeNull();
    }
}
