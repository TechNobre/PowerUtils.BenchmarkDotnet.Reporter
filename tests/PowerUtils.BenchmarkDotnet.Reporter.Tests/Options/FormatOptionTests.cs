using System;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Commands;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Options;

public sealed class FormatOptionTests
{
    private readonly IServiceProvider _provider;

    public FormatOptionTests()
    {
        _provider = Substitute.For<IServiceProvider>();
        var command = Substitute.For<IComparerCommand>();

        _provider
            .GetService(typeof(IComparerCommand))
            .Returns(command);
    }

    [Fact]
    public void CompareCommand_ShouldHave_FormatsOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == "compare");
        var formatsOption = compareCommand.Options.Single(o => o.Name == "--format");

        formatsOption.ValueType.ShouldBe(typeof(string[]));
        formatsOption.Aliases.Count.ShouldBe(1);
        formatsOption.Aliases.ShouldContain("-f");
        formatsOption.Description.ShouldBe("Output format for the report.");
        (formatsOption.GetDefaultValue() as string[]).ShouldBe(["console"]);
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

        var toolCommands = new ToolCommands(_provider);
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == command);
        var formatsOption = compareCommand.Options.Single(o => o.Name == option);
        var validation = formatsOption.Validators.Single();


        // Act
        var parseResult = toolCommands.Parse($"{command} {option} {format}");
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

        var toolCommands = new ToolCommands(_provider);
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == command);
        var formatsOption = compareCommand.Options.Single(o => o.Name == option);
        var validation = formatsOption.Validators.Single();


        // Act
        var parseResult = toolCommands.Parse($"{command} {option} {format}");
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

        var toolCommands = new ToolCommands(_provider);
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == command);
        var formatsOption = compareCommand.Options.Single(o => o.Name == option);
        var validation = formatsOption.Validators.Single();


        // Act
        var parseResult = toolCommands.Parse($"{command} {option} {format}");
        var firstOptionResult = parseResult.GetResult(formatsOption);

        // Assert
        firstOptionResult?.Errors.Count().ShouldBe(1);
        firstOptionResult?.Errors.ShouldContain(e => e.Message == "Required argument missing for option: '--format'.");
    }
}
