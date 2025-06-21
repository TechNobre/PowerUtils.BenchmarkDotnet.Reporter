using System;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Commands;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests;

public sealed class ToolCommandsTest
{
    private readonly IServiceProvider _provider;

    public ToolCommandsTest()
    {
        _provider = Substitute.For<IServiceProvider>();
        var command = Substitute.For<IComparerCommand>();

        _provider
            .GetService(typeof(IComparerCommand))
            .Returns(command);
    }

    [Fact]
    public void Constructor_ShouldAddCommands()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        toolCommands.Subcommands.Count.ShouldBe(1);
    }

    [Fact]
    public void RootCommands_ShouldContain_CompareCommand()
    {
        // Arrange
        var toolCommands = new ToolCommands(_provider);


        // Act
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == "compare");


        // Assert
        toolCommands.Subcommands.ShouldContain(compareCommand);
    }

    [Fact]
    public void CompareCommand_ShouldHave_6Options()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == "compare");

        compareCommand.Options.Count.ShouldBe(6);
    }
}
