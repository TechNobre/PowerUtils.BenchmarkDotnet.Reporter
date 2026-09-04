using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration.PbReporterConfiguration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate.Options;

public sealed class FormatOptionTests
{
    private readonly Command _command;

    public FormatOptionTests()
    {
        var handler = new GateHandler(
            Substitute.For<Func<string?, List<BenchmarkReport>>>(),
            Substitute.For<IGateValidator>(),
            Substitute.For<IKeyedServiceProvider>());
        _command = new GateCommand(handler).Build();
    }

    [Fact]
    public void GateCommand_ShouldHave_FormatsOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--format");

        // Assert
        option.ValueType.Should().Be(typeof(string[]));
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-f");
        option.Description.Should().Be("Output format for the report. Can also be set via the PBREPORTER_GATE__FORMATS environment variable or the 'formats' key in the YAML config file (scalar or list).");
        (option.GetDefaultValue() as string[]).Should().Equal("console");
    }

    [Theory]
    [InlineData("markdown")]
    [InlineData("jSOn")]
    [InlineData("HIT-TXT")]
    [InlineData("console")]
    public void When_Format_Is_Valid_Shouldnt_Have_Validation_Error(string format)
    {
        // Arrange
        var option = "--format";

        var formatsOption = _command.Options.Single(o => o.Name == option);


        // Act
        var parseResult = _command.Parse($"{option} {format}");
        var firstOptionResult = parseResult.GetResult(formatsOption);

        // Assert
        firstOptionResult?.Errors.Count().Should().Be(0);
    }

    [Theory]
    [InlineData("invalid-format")]
    [InlineData("csv")]
    [InlineData("html")]
    public void When_Format_Is_Invalid_Should_Have_Validation_Error(string format)
    {
        // Arrange
        var option = "--format";

        var formatsOption = _command.Options.Single(o => o.Name == option);


        // Act
        var parseResult = _command.Parse($"{option} {format}");
        var firstOptionResult = parseResult.GetResult(formatsOption);

        // Assert
        firstOptionResult?.Errors.Count().Should().Be(1);
        firstOptionResult?.Errors.Should().Contain(e => e.Message == $"Invalid format '{format}'. Allowed values: console, markdown, json, hit-txt");
    }

    [Fact]
    public void Parse_WithConfigurationFormatOnly_ShouldUse_ConfigurationFormat()
    {
        // Arrange
        var parseResult = _command.Parse(string.Empty);
        var configuration = new GateConfigurationSection { Formats = ["markdown"] };

        // Act
        var options = GateOptions.Parse(parseResult, configuration);

        // Assert
        options.Formats.Should().Equal("markdown");
    }

    [Fact]
    public void Parse_WithCliAndConfigurationFormat_ShouldPrefer_CliFormat()
    {
        // Arrange
        var parseResult = _command.Parse("--format json");
        var configuration = new GateConfigurationSection { Formats = ["markdown"] };

        // Act
        var options = GateOptions.Parse(parseResult, configuration);

        // Assert
        options.Formats.Should().Equal("json");
    }

    [Fact]
    public void Parse_WithNoCliOrConfigurationFormat_ShouldUseDefault_Console()
    {
        // Arrange
        var parseResult = _command.Parse(string.Empty);

        // Act
        var options = GateOptions.Parse(parseResult);

        // Assert
        options.Formats.Should().Equal("console");
    }

    [Fact]
    public void Parse_WithConfigurationMultipleFormats_ShouldUse_AllConfigurationFormats()
    {
        // Arrange
        var parseResult = _command.Parse(string.Empty);
        var configuration = new GateConfigurationSection { Formats = ["json", "markdown"] };

        // Act
        var options = GateOptions.Parse(parseResult, configuration);

        // Assert
        options.Formats.Should().Equal("json", "markdown");
    }
}
