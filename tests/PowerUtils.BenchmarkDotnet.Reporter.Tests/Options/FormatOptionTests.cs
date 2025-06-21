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
    }

    [Theory]
    [InlineData("markdown", true)]
    [InlineData("jSOn", true)]
    [InlineData("HIT-TXT", true)]
    [InlineData("console", true)]
    [InlineData("invalid-format", false)]
    public void Test_Validation_Format_Option(string format, bool isValid)
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
        firstOptionResult?.Errors.Count().ShouldBe(isValid ? 0 : 1);
    }
}
