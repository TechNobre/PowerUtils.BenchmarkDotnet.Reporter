using System;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Commands;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Options;

public sealed class OptionsTests
{
    private readonly IServiceProvider _provider;

    public OptionsTests()
    {
        _provider = Substitute.For<IServiceProvider>();
        var command = Substitute.For<IComparerCommand>();

        _provider
            .GetService(typeof(IComparerCommand))
            .Returns(command);
    }

    [Fact]
    public void CompareCommand_ShouldHave_BaselineOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var command = toolCommands.Subcommands.Single(c => c.Name == "compare");
        var option = command.Options.Single(o => o.Name == "--baseline");

        option.ValueType.ShouldBe(typeof(string));
        option.Aliases.Count.ShouldBe(1);
        option.Aliases.ShouldContain("-b");
        option.Required.ShouldBeTrue();
        option.Description.ShouldBe("Path to the folder or file with Baseline report.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_TargetOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var command = toolCommands.Subcommands.Single(c => c.Name == "compare");
        var option = command.Options.Single(o => o.Name == "--target");

        option.ValueType.ShouldBe(typeof(string));
        option.Aliases.Count.ShouldBe(1);
        option.Aliases.ShouldContain("-t");
        option.Required.ShouldBeTrue();
        option.Description.ShouldBe("Path to the folder or file with target reports.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_ThresholdMeanOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var command = toolCommands.Subcommands.Single(c => c.Name == "compare");
        var option = command.Options.Single(o => o.Name == "--threshold-mean");

        option.ValueType.ShouldBe(typeof(string));
        option.Aliases.Count.ShouldBe(1);
        option.Aliases.ShouldContain("-tm");
        option.Description.ShouldBe("Throw an error when the mean threshold is met. Examples: 5%, 10ms, 10μs, 100ns, 1s.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_ThresholdAllocationOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var command = toolCommands.Subcommands.Single(c => c.Name == "compare");
        var option = command.Options.Single(o => o.Name == "--threshold-allocation");

        option.ValueType.ShouldBe(typeof(string));
        option.Aliases.Count.ShouldBe(1);
        option.Aliases.ShouldContain("-ta");
        option.Description.ShouldBe("Throw an error when the allocation threshold is met. Examples: 5%, 10b, 10kb, 100mb, 1gb.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_OutputOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var command = toolCommands.Subcommands.Single(c => c.Name == "compare");
        var option = command.Options.Single(o => o.Name == "--output");

        option.ValueType.ShouldBe(typeof(string));
        option.Aliases.Count.ShouldBe(1);
        option.Aliases.ShouldContain("-o");
        option.Description.ShouldBe("Output directory to export the diff report. Default is current directory.");
        (option.GetDefaultValue() as string).ShouldBe("./BenchmarkReporter");
    }

    [Fact]
    public void CompareCommand_ShouldHave_FailOnThresholdHitOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == "compare");        var option = compareCommand.Options.Single(o => o.Name == "--fail-on-threshold-hit");

        option.ValueType.ShouldBe(typeof(bool));
        option.Aliases.Count.ShouldBe(1);
        option.Aliases.ShouldContain("-ft");
        option.Required.ShouldBeFalse();
        option.Description.ShouldBe("Exit with error code when any threshold is hit during comparison.");
        Convert.ToBoolean(option.GetDefaultValue()).ShouldBe(false);
    }

    [Fact]
    public void CompareCommand_ShouldHave_FailOnWarningsOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == "compare");        var option = compareCommand.Options.Single(o => o.Name == "--fail-on-warnings");

        option.ValueType.ShouldBe(typeof(bool));
        option.Aliases.Count.ShouldBe(1);
        option.Aliases.ShouldContain("-fw");
        option.Required.ShouldBeFalse();
        option.Description.ShouldBe("Exit with error code when the comparison generates any warnings.");
        Convert.ToBoolean(option.GetDefaultValue()).ShouldBe(false);
    }
}
