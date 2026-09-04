using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate;

public sealed class GateHandlerTests
{
    private readonly Func<string?, List<BenchmarkReport>> _readBenchmarks;
    private List<BenchmarkReport> _benchmarks;

    private readonly IKeyedServiceProvider _serviceProvider;
    private readonly IGateValidator _validator;
    private readonly IExporter _exporter;

    private readonly GateHandler _handler;


    public GateHandlerTests()
    {
        _benchmarks = [];

        _readBenchmarks = (path)
            => path switch
            {
                "input" => _benchmarks,
                _ => throw new ArgumentException()
            };

        _validator = Substitute.For<IGateValidator>();
        _exporter = Substitute.For<IExporter>();

        _serviceProvider = Substitute.For<IKeyedServiceProvider>();
        _serviceProvider
            .GetRequiredKeyedService(Arg.Any<Type>(), Arg.Any<object?>())
            .Returns(_exporter);

        _handler = new(
            _readBenchmarks,
            _validator,
            _serviceProvider);
    }


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void When_Input_Is_Missing_Should_Throw_DomainException(string? input)
    {
        // Arrange & Act
        Action act = () => _handler.Execute(new()
        {
            Input = input,
            Formats = ["xpto"]
        });


        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*input*required*");
    }

    [Fact]
    public void When_Report_Has_Two_Benchmarks_Should_Generate_Report_With_Two_Results()
    {
        // Arrange
        _benchmarks = [
            new() { FullName = "Benchmark1", Type = "Type1", Method = "Method1", Statistics = new() { Mean = 12 } },
            new() { FullName = "Benchmark2", Type = "Type2", Method = "Method2", Statistics = new() { Mean = 24 } }
        ];


        // Act
        _handler.Execute(new()
        {
            Input = "input",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<GateReport>(i => i.Results.Count == 2),
                Arg.Any<string>());
    }

    [Fact]
    public void Should_Map_Mean_And_Allocated_From_Benchmark_Statistics_And_Memory()
    {
        // Arrange
        _benchmarks = [
            new()
            {
                FullName = "Benchmark1",
                Type = "Type1",
                Method = "Method1",
                Statistics = new() { Mean = 42 },
                Memory = new() { BytesAllocatedPerOperation = 123 }
            }
        ];


        // Act
        _handler.Execute(new()
        {
            Input = "input",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<GateReport>(i =>
                    i.Results.Single().Type == "Type1" &&
                    i.Results.Single().Name == "Method1" &&
                    i.Results.Single().FullName == "Benchmark1" &&
                    i.Results.Single().Mean == 42 &&
                    i.Results.Single().Allocated == 123),
                Arg.Any<string>());
    }

    [Fact]
    public void When_No_Warnings_Generated_And_FailOnWarnings_Is_True_Should_Return_Success_ExitCode()
    {
        // Arrange
        _validator
            .ValidateHostEnvironment(Arg.Any<List<BenchmarkReport>>())
            .Returns([]);


        // Act
        var act = _handler.Execute(new()
        {
            Input = "input",
            Formats = ["xpto"],
            FailOnWarnings = true
        });


        // Assert
        act.Should().Be(Constants.ExitCodes.SUCCESS);
    }

    [Fact]
    public void When_Warnings_Generated_And_FailOnWarnings_Is_False_Should_Return_Success_ExitCode()
    {
        // Arrange
        var expectedMessage = Guid.NewGuid().ToString();
        _validator
            .ValidateHostEnvironment(Arg.Any<List<BenchmarkReport>>())
            .Returns([expectedMessage]);


        // Act
        var act = _handler.Execute(new()
        {
            Input = "input",
            Formats = ["xpto"],
            FailOnWarnings = false
        });


        // Assert
        _exporter
            .Received()
            .Generate(Arg.Is<GateReport>(i => i.Warnings.Count == 1 && i.Warnings.Contains(expectedMessage)), Arg.Any<string>());
        act.Should().Be(Constants.ExitCodes.SUCCESS);
    }

    [Fact]
    public void When_Warnings_Generated_And_FailOnWarnings_Is_True_Should_Return_Warning_ExitCode()
    {
        // Arrange
        _validator
            .ValidateHostEnvironment(Arg.Any<List<BenchmarkReport>>())
            .Returns([Guid.NewGuid().ToString()]);


        // Act
        var act = _handler.Execute(new()
        {
            Input = "input",
            Formats = ["xpto"],
            FailOnWarnings = true
        });


        // Assert
        act.Should().Be(Constants.ExitCodes.WARNING);
    }

    [Fact]
    public void When_Threshold_Hit_Should_Return_ThresholdHit_ExitCode_Regardless_Of_FailOnWarnings()
    {
        // Arrange
        _validator
            .When(v => v.EvaluateThresholds(Arg.Any<GateReport>(), Arg.Any<IReadOnlyList<KeyValuePair<string, string>>>(), Arg.Any<IReadOnlyList<KeyValuePair<string, string>>>()))
            .Do(ci => ci.ArgAt<GateReport>(0).HitThresholds.Add("Mean threshold hit for 'test hit'"));


        // Act
        var act = _handler.Execute(new()
        {
            Input = "input",
            Formats = ["xpto"],
            FailOnWarnings = false
        });


        // Assert
        _exporter
            .Received()
            .Generate(Arg.Is<GateReport>(i => i.HitThresholds.Count > 0), Arg.Any<string>());
        act.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
    }

    [Fact]
    public void When_Threshold_Hit_And_Warnings_Generated_Should_Prioritize_ThresholdHit_ExitCode()
    {
        // Arrange
        _validator
            .ValidateHostEnvironment(Arg.Any<List<BenchmarkReport>>())
            .Returns([Guid.NewGuid().ToString()]);
        _validator
            .When(v => v.EvaluateThresholds(Arg.Any<GateReport>(), Arg.Any<IReadOnlyList<KeyValuePair<string, string>>>(), Arg.Any<IReadOnlyList<KeyValuePair<string, string>>>()))
            .Do(ci => ci.ArgAt<GateReport>(0).HitThresholds.Add("Mean threshold hit for 'test hit'"));


        // Act
        var act = _handler.Execute(new()
        {
            Input = "input",
            Formats = ["xpto"],
            FailOnWarnings = true
        });


        // Assert
        act.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
    }

    [Fact]
    public void Should_Generate_Report_For_Every_Requested_Format()
    {
        // Arrange & Act
        _handler.Execute(new()
        {
            Input = "input",
            Formats = ["one", "two"]
        });


        // Assert
        _serviceProvider.Received(2).GetRequiredKeyedService(typeof(IExporter), Arg.Any<object?>());
    }
}
