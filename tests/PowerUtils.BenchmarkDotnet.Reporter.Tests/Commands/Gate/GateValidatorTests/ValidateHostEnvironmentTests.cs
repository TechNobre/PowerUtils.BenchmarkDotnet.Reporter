using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate.GateValidatorTests;

public sealed class ValidateHostEnvironmentTests
{
    private readonly GateValidator _validator = new();


    [Fact]
    public void When_Benchmark_Was_Executed_In_Release_Should_Not_Generate_Warning()
    {
        // Arrange
        var benchmarks = new List<BenchmarkReport>
        {
            new()
            {
                FullName = "Benchmark1",
                Header = new() { HostEnvironmentInfo = new() { Configuration = "RELEASE" } }
            }
        };


        // Act
        var messages = _validator.ValidateHostEnvironment(benchmarks);


        // Assert
        messages.Should().BeEmpty();
    }

    [Fact]
    public void When_Benchmark_Was_Executed_In_Debug_Should_Generate_Warning()
    {
        // Arrange
        var benchmarks = new List<BenchmarkReport>
        {
            new()
            {
                FullName = "Benchmark1",
                Header = new() { HostEnvironmentInfo = new() { Configuration = "DEBUG" } }
            }
        };


        // Act
        var messages = _validator.ValidateHostEnvironment(benchmarks);


        // Assert
        messages.Should().ContainSingle()
            .Which.Should().Contain("Benchmark1").And.Contain("RELEASE").And.Contain("DEBUG");
    }

    [Fact]
    public void When_Header_Is_Missing_Should_Generate_Warning()
    {
        // Arrange
        var benchmarks = new List<BenchmarkReport>
        {
            new() { FullName = "Benchmark1" }
        };


        // Act
        var messages = _validator.ValidateHostEnvironment(benchmarks);


        // Assert
        messages.Should().ContainSingle();
    }

    [Fact]
    public void When_No_Benchmarks_Should_Return_EmptyList()
    {
        // Arrange
        var benchmarks = new List<BenchmarkReport>();


        // Act
        var messages = _validator.ValidateHostEnvironment(benchmarks);


        // Assert
        messages.Should().BeEmpty();
    }

    [Fact]
    public void When_Multiple_Benchmarks_Not_In_Release_Should_Generate_One_Warning_Per_Benchmark()
    {
        // Arrange
        var benchmarks = new List<BenchmarkReport>
        {
            new() { FullName = "Benchmark1", Header = new() { HostEnvironmentInfo = new() { Configuration = "DEBUG" } } },
            new() { FullName = "Benchmark2", Header = new() { HostEnvironmentInfo = new() { Configuration = "DEBUG" } } }
        };


        // Act
        var messages = _validator.ValidateHostEnvironment(benchmarks);


        // Assert
        messages.Should().HaveCount(2);
    }
}
