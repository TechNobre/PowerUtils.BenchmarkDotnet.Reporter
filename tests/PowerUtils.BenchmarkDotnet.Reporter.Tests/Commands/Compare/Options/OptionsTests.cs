using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.Options;

public sealed class OptionsTests
{
    private readonly Command _command;

    public OptionsTests()
    {
        var handler = new CompareHandler(
            Substitute.For<Func<string?, List<BenchmarkReport>>>(),
            Substitute.For<ICompareValidator>(),
            Substitute.For<IKeyedServiceProvider>());
        _command = new CompareCommand(handler).Build();
    }

    [Fact]
    public void CompareCommand_ShouldHave_BaselineOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--baseline");


        // Assert
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
        var option = _command.Options.Single(o => o.Name == "--target");


        // Assert
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
        var option = _command.Options.Single(o => o.Name == "--threshold-mean");


        // Assert
        option.ValueType.ShouldBe(typeof(string));
        option.Aliases.Count.ShouldBe(1);
        option.Aliases.ShouldContain("-tm");
        option.Description.ShouldBe("Throw an error when the mean threshold is met. Examples: 5%, 10ms, 10us, 100ns, 1s.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_ThresholdAllocationOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--threshold-allocation");


        // Assert
        option.ValueType.ShouldBe(typeof(string));
        option.Aliases.Count.ShouldBe(1);
        option.Aliases.ShouldContain("-ta");
        option.Description.ShouldBe("Throw an error when the allocation threshold is met. Examples: 5%, 10b, 10kb, 100mb, 1gb.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_OutputOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--output");


        // Assert
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
        var option = _command.Options.Single(o => o.Name == "--fail-on-threshold-hit");


        // Assert
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
        var option = _command.Options.Single(o => o.Name == "--fail-on-warnings");


        // Assert
        option.ValueType.ShouldBe(typeof(bool));
        option.Aliases.Count.ShouldBe(1);
        option.Aliases.ShouldContain("-fw");
        option.Required.ShouldBeFalse();
        option.Description.ShouldBe("Exit with error code when the comparison generates any warnings.");
        Convert.ToBoolean(option.GetDefaultValue()).ShouldBe(false);
    }
}
