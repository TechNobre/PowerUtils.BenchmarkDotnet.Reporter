using System;
using System.Collections.Generic;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate;

public sealed class GateCommandTests
{
    private readonly Command _command;

    public GateCommandTests()
    {
        var handler = new GateHandler(
            Substitute.For<Func<string?, List<BenchmarkReport>>>(),
            Substitute.For<IGateValidator>(),
            Substitute.For<IKeyedServiceProvider>());
        _command = new GateCommand(handler).Build();
    }


    [Fact]
    public void CommandName_ShouldBe_Gate()
    {
        // Arrange & Act & Assert
        _command.Name.Should().Be("gate");
    }

    [Fact]
    public void Command_ShouldHave_7Options()
    {
        // Arrange & Act & Assert
        _command.Options.Count.Should().Be(7);
    }

    [Fact]
    public void Command_ShouldHave_Description()
    {
        // Arrange & Act & Assert
        _command.Description.Should().Be("Check a BenchmarkDotNet report against absolute performance thresholds.");
    }

    [Fact]
    public void Command_ShouldHave_Action()
    {
        // Arrange & Act & Assert
        _command.Action.Should().NotBeNull();
    }
}
