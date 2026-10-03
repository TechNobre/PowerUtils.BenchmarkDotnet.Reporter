using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate.Options;

public sealed class OptionsTests
{
    private readonly Command _command;

    public OptionsTests()
    {
        var handler = new GateHandler(
            Substitute.For<Func<string?, List<BenchmarkReport>>>(),
            Substitute.For<IGateValidator>(),
            Substitute.For<IKeyedServiceProvider>());
        _command = new GateCommand(handler).Build();
    }

    [Fact]
    public void GateCommand_ShouldHave_InputOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--input");


        // Assert
        option.ValueType.Should().Be<string>();
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-i");
        option.Required.Should().BeFalse();
        option.Description.Should().Be("Path to the folder or file with the report(s) to check. Can also be set via the PBREPORTER_GATE__INPUT environment variable or the 'input' key in the YAML config file; one of these sources must supply a value.");
    }

    [Fact]
    public void GateCommand_ShouldHave_ThresholdMeanOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--threshold-mean");


        // Assert
        option.ValueType.Should().Be<string[]>();
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-tm");
        option.Description.Should().Be("Fail when a benchmark's mean execution time exceeds this absolute value. Examples: 10ms, 10us, 100ns, 1s. Repeat with 'pattern=value' (e.g. 'MyNamespace.MyClass.*=10ms') to scope a threshold to matching benchmarks; a bare value (no 'pattern=') sets the global threshold.");
    }

    [Fact]
    public void GateCommand_ShouldHave_ThresholdAllocationOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--threshold-allocation");


        // Assert
        option.ValueType.Should().Be<string[]>();
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-ta");
        option.Description.Should().Be("Fail when a benchmark's allocated memory exceeds this absolute value. Examples: 10b, 10kb, 100mb, 1gb. Repeat with 'pattern=value' (e.g. 'MyNamespace.MyClass.*=10kb') to scope a threshold to matching benchmarks; a bare value (no 'pattern=') sets the global threshold.");
    }

    [Fact]
    public void GateCommand_ShouldHave_OutputOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--output");


        // Assert
        option.ValueType.Should().Be<string>();
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-o");
        option.Description.Should().Be("Output directory to export the gate report.");
        (option.GetDefaultValue() as string).Should().Be("./BenchmarkReporter");
    }

    [Fact]
    public void GateCommand_ShouldHave_FailOnWarningsOption()
    {
        // Arrange & Act
        var option = _command.Options.Single(o => o.Name == "--fail-on-warnings");


        // Assert
        option.ValueType.Should().Be<bool>();
        option.Aliases.Count.Should().Be(1);
        option.Aliases.Should().Contain("-fw");
        option.Required.Should().BeFalse();
        option.Description.Should().Be("Exit with error code when the gate check generates any warnings.");
        Convert.ToBoolean(option.GetDefaultValue()).Should().Be(false);
    }

    [Fact]
    public void GateCommand_ShouldNotHave_FailOnThresholdHitOption() =>
        // A threshold hit always fails the run - gate has no opt-in/opt-out flag for it, unlike compare.
        // Arrange & Act & Assert
        _command.Options.Should().NotContain(o => o.Name == "--fail-on-threshold-hit");
}
