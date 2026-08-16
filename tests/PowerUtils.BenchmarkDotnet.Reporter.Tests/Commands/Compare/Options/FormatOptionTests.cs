using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.Options;

public sealed class FormatOptionTests
{
    private readonly Command _command;

    public FormatOptionTests()
    {
        var handler = new CompareHandler(
            Substitute.For<Func<string?, List<BenchmarkReport>>>(),
            Substitute.For<ICompareValidator>(),
            Substitute.For<IKeyedServiceProvider>());
        _command = new CompareCommand(handler).Build();
    }

    [Fact]
    public void CompareCommand_ShouldHave_FormatsOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--format");

        // Assert
        option.ValueType.ShouldBe(typeof(string[]));
        option.Aliases.Count.ShouldBe(1);
        option.Aliases.ShouldContain("-f");
        option.Description.ShouldBe("Output format for the report.");
        (option.GetDefaultValue() as string[]).ShouldBe(["console"]);
    }

    [Theory]
    [InlineData("markdown")]
    [InlineData("jSOn")]
    [InlineData("HIT-TXT")]
    [InlineData("console")]
    public void When_Format_Is_Valid_Shouldnt_Have_Validation_Error(string format)
    {
        // Arrange
        var command = "compare";
        var option = "--format";

        //var toolCommands = new ToolCommands(_provider);
        var formatsOption = _command.Options.Single(o => o.Name == option);
        var validation = formatsOption.Validators.Single();


        // Act
        var parseResult = _command.Parse($"{command} {option} {format}");
        var firstOptionResult = parseResult.GetResult(formatsOption);

        // Assert
        firstOptionResult?.Errors.Count().ShouldBe(0);
    }

    [Theory]
    [InlineData("invalid-format")]
    [InlineData("csv")]
    [InlineData("html")]
    public void When_Format_Is_Invalid_Should_Have_Validation_Error(string format)
    {
        // Arrange
        var command = "compare";
        var option = "--format";

        var formatsOption = _command.Options.Single(o => o.Name == option);
        var validation = formatsOption.Validators.Single();


        // Act
        var parseResult = _command.Parse($"{command} {option} {format}");
        var firstOptionResult = parseResult.GetResult(formatsOption);

        // Assert
        firstOptionResult?.Errors.Count().ShouldBe(1);
        firstOptionResult?.Errors.ShouldContain(e => e.Message == $"Invalid format '{format}'. Allowed values: console, markdown, json, hit-txt");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void When_Format_Isnt_Defined_Should_Have_Validation_Error(string? format)
    {
        // Arrange
        var command = "compare";
        var option = "--format";

        var formatsOption = _command.Options.Single(o => o.Name == option);
        var validation = formatsOption.Validators.Single();


        // Act
        var parseResult = _command.Parse($"{command} {option} {format}");
        var firstOptionResult = parseResult.GetResult(formatsOption);

        // Assert
        firstOptionResult?.Errors.Count().ShouldBe(1);
        firstOptionResult?.Errors.ShouldContain(e => e.Message == "Required argument missing for option: '--format'.");
    }
}
