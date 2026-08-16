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
        option.ValueType.Should().Be(typeof(string));
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-b");
        option.Required.Should().BeTrue();
        option.Description.Should().Be("Path to the folder or file with Baseline report.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_TargetOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--target");


        // Assert
        option.ValueType.Should().Be(typeof(string));
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-t");
        option.Required.Should().BeTrue();
        option.Description.Should().Be("Path to the folder or file with target reports.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_ThresholdMeanOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--threshold-mean");


        // Assert
        option.ValueType.Should().Be(typeof(string));
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-tm");
        option.Description.Should().Be("Throw an error when the mean threshold is met. Examples: 5%, 10ms, 10us, 100ns, 1s.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_ThresholdAllocationOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--threshold-allocation");


        // Assert
        option.ValueType.Should().Be(typeof(string));
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-ta");
        option.Description.Should().Be("Throw an error when the allocation threshold is met. Examples: 5%, 10b, 10kb, 100mb, 1gb.");
    }

    [Fact]
    public void CompareCommand_ShouldHave_OutputOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--output");


        // Assert
        option.ValueType.Should().Be(typeof(string));
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-o");
        option.Description.Should().Be("Output directory to export the diff report. Default is current directory.");
        (option.GetDefaultValue() as string).Should().Be("./BenchmarkReporter");
    }

    [Fact]
    public void CompareCommand_ShouldHave_FailOnThresholdHitOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--fail-on-threshold-hit");


        // Assert
        option.ValueType.Should().Be(typeof(bool));
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-ft");
        option.Required.Should().BeFalse();
        option.Description.Should().Be("Exit with error code when any threshold is hit during comparison.");
        Convert.ToBoolean(option.GetDefaultValue()).Should().Be(false);
    }

    [Fact]
    public void CompareCommand_ShouldHave_FailOnWarningsOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--fail-on-warnings");


        // Assert
        option.ValueType.Should().Be(typeof(bool));
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-fw");
        option.Required.Should().BeFalse();
        option.Description.Should().Be("Exit with error code when the comparison generates any warnings.");
        Convert.ToBoolean(option.GetDefaultValue()).Should().Be(false);
    }
}
