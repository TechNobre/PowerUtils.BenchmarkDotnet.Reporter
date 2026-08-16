using System;
using System.Collections.Generic;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare;

public sealed class CompareCommandTests
{
    private readonly Command _command;

    public CompareCommandTests()
    {
        var handler = new CompareHandler(
            Substitute.For<Func<string?, List<BenchmarkReport>>>(),
            Substitute.For<ICompareValidator>(),
            Substitute.For<IKeyedServiceProvider>());
        _command = new CompareCommand(handler).Build();
    }


    [Fact]
    public void CommandName_ShouldBe_Compare()
    {
        // Arrange & Act & Assert
        _command.Name.ShouldBe("compare");
    }

    [Fact]
    public void Command_ShouldHave_8Options()
    {
        // Arrange & Act & Assert
        _command.Options.Count.ShouldBe(8);
    }

    [Fact]
    public void Command_ShouldHave_Description()
    {
        // Arrange & Act & Assert
        _command.Description.ShouldBe("Compare two BenchmarkDotNet reports and produce a diff report.");
    }

    [Fact]
    public void Command_ShouldHave_Action()
    {
        // Arrange & Act & Assert
        _command.Action.ShouldNotBeNull();
    }
}
