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
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == "compare");
        var baselineOption = compareCommand.Options.Single(o => o.Name == "--baseline");

        baselineOption.ValueType.ShouldBe(typeof(string));
        baselineOption.Aliases.Count.ShouldBe(1);
        baselineOption.Aliases.ShouldContain("-b");
        baselineOption.Required.ShouldBeTrue();
        baselineOption.Description.ShouldBe("Path to the folder or file with Baseline report.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_TargetOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == "compare");
        var targetOption = compareCommand.Options.Single(o => o.Name == "--target");

        targetOption.ValueType.ShouldBe(typeof(string));
        targetOption.Aliases.Count.ShouldBe(1);
        targetOption.Aliases.ShouldContain("-t");
        targetOption.Required.ShouldBeTrue();
        targetOption.Description.ShouldBe("Path to the folder or file with target reports.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_ThresholdMeanOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == "compare");
        var meanThresholdOption = compareCommand.Options.Single(o => o.Name == "--threshold-mean");

        meanThresholdOption.ValueType.ShouldBe(typeof(string));
        meanThresholdOption.Aliases.Count.ShouldBe(1);
        meanThresholdOption.Aliases.ShouldContain("-tm");
        meanThresholdOption.Description.ShouldBe("Throw an error when the mean threshold is met. Examples: 5%, 10ms, 10μs, 100ns, 1s.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_ThresholdAllocationOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == "compare");
        var allocationThresholdOption = compareCommand.Options.Single(o => o.Name == "--threshold-allocation");

        allocationThresholdOption.ValueType.ShouldBe(typeof(string));
        allocationThresholdOption.Aliases.Count.ShouldBe(1);
        allocationThresholdOption.Aliases.ShouldContain("-ta");
        allocationThresholdOption.Description.ShouldBe("Throw an error when the allocation threshold is met. Examples: 5%, 10b, 10kb, 100mb, 1gb.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_OutputOption()
    {
        // Arrange & Act
        var toolCommands = new ToolCommands(_provider);


        // Assert
        var compareCommand = toolCommands.Subcommands.Single(c => c.Name == "compare");
        var outputOption = compareCommand.Options.Single(o => o.Name == "--output");

        outputOption.ValueType.ShouldBe(typeof(string));
        outputOption.Aliases.Count.ShouldBe(1);
        outputOption.Aliases.ShouldContain("-o");
        outputOption.Description.ShouldBe("Output directory to export the diff report. Default is current directory.");
        (outputOption.GetDefaultValue() as string).ShouldBe("./BenchmarkReporter");
    }
}
