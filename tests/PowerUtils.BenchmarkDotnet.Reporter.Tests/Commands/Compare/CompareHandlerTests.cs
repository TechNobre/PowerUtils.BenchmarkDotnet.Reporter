using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using System.Linq;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare;

public sealed class CompareHandlerTests
{
    private readonly Func<string?, List<BenchmarkReport>> _readBenchmarks;
    private List<BenchmarkReport> _baselineBenchmarks;
    private List<BenchmarkReport> _targetBenchmarks;

    private readonly IKeyedServiceProvider _serviceProvider;
    private readonly ICompareValidator _validator;
    private readonly IExporter _exporter;
    private readonly ComparerReport _comparerReport;

    private readonly CompareHandler _handler;


    public CompareHandlerTests()
    {
        _baselineBenchmarks = [];
        _targetBenchmarks = [];

        _readBenchmarks = (path)
            => path switch
            {
                "baseline" => _baselineBenchmarks,
                "target" => _targetBenchmarks,
                _ => throw new ArgumentException()
            };

        _comparerReport = new();

        _validator = Substitute.For<ICompareValidator>();
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


    [Fact]
    public void When_Have_Baseline_And_Target_Equivalent_Shouldnt_Generate_Warning()
    {
        // Arrange
        _baselineBenchmarks = [new() { Header = new() { HostEnvironmentInfo = new() { Configuration = "RELEASE" } } }];
        _targetBenchmarks = [new() { Header = new() { HostEnvironmentInfo = new() { Configuration = "RELEASE" } } }];

        var validator = new CompareValidator();


        var handler = new CompareHandler(
            _readBenchmarks,
            validator,
            _serviceProvider);


        // Act
        handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(Arg.Is<ComparerReport>(i => i.Warnings.Count == 0), Arg.Any<string>());
    }

    [Fact]
    public void When_Comparison_Generate_Warning_And_FailOnWarnings_Is_False_Should_Return_Success_ExitCode()
    {
        // Arrange
        _baselineBenchmarks = [new() { Header = new() { HostEnvironmentInfo = new() { Configuration = "RELEASE" } } }];
        var expectedMessage = Guid.NewGuid().ToString();

        _validator
            .ValidateHostEnvironment(Arg.Any<BenchmarkReport>(), Arg.Any<BenchmarkReport>())
            .Returns([expectedMessage]);


        // Act
        var act = _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(Arg.Is<ComparerReport>(i => i.Warnings.Count == 1), Arg.Any<string>());
        act.Should().Be(Constants.ExitCodes.SUCCESS);
    }

    [Fact]
    public void When_Comparison_Generate_Warning_And_FailOnWarnings_Is_True_Should_Return_Failure_Warning_ExitCode()
    {
        // Arrange
        _baselineBenchmarks = [new() { Header = new() { HostEnvironmentInfo = new() { Configuration = "RELEASE" } } }];
        var expectedMessage = Guid.NewGuid().ToString();

        _validator
            .ValidateHostEnvironment(Arg.Any<BenchmarkReport>(), Arg.Any<BenchmarkReport>())
            .Returns([expectedMessage]);


        // Act
        var act = _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"],
            FailOnWarnings = true
        });


        // Assert
        _exporter
            .Received()
            .Generate(Arg.Is<ComparerReport>(
                i => i.Warnings.Count == 1 &&
                i.Warnings.Contains(expectedMessage)),
            Arg.Any<string>());
        act.Should().Be(Constants.ExitCodes.WARNING);
    }

    [Fact]
    public void When_No_Warnings_Generated_And_FailOnWarnings_Is_True_Should_Return_Success_ExitCode()
    {
        // Arrange
        _validator
            .ValidateHostEnvironment(Arg.Any<BenchmarkReport>(), Arg.Any<BenchmarkReport>())
            .Returns([]);


        // Act
        var act = _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"],
            FailOnWarnings = true
        });


        // Assert
        act.Should().Be(Constants.ExitCodes.SUCCESS);
    }

    [Fact]
    public void When_No_Thresholds_Hit_And_FailOnThresholdHit_Is_True_Should_Return_Success_ExitCode()
    {
        // Arrange & Act
        var act = _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"],
            FailOnThresholdHit = true
        });


        // Assert
        act.Should().Be(Constants.ExitCodes.SUCCESS);
    }

    [Fact]
    public void When_Baseline_And_Target_Have_Different_Type_Method_FullName_Should_Use_Baseline_Values()
    {
        // Arrange
        // FullName is matched case-insensitively, so baseline and target here refer to the same
        // benchmark despite differing in casing - letting us assert that the baseline's exact
        // Type/Method/FullName values are the ones kept in the resulting comparison.
        _baselineBenchmarks = [
            new()
            {
                Type = "BaselineType",
                Method = "BaselineMethod",
                FullName = "sharedfullname",
                Statistics = new() { Mean = 12 }
            }];
        _targetBenchmarks = [
            new()
            {
                Type = "TargetType",
                Method = "TargetMethod",
                FullName = "SHAREDFULLNAME",
                Statistics = new() { Mean = 12 }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Count == 1 &&
                    i.Comparisons.First().Type == "BaselineType" &&
                    i.Comparisons.First().Name == "BaselineMethod" &&
                    i.Comparisons.First().FullName == "sharedfullname"),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Baseline_Doesnt_Have_Benchmarks_Should_Generate_Zero_Comparisons()
    {
        // Arrange & Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i => i.Comparisons.Count == 0),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Only_Have_Method_On_Baseline_Should_Have_OneComparation_With_Status_Removed()
    {
        // Arrange
        _baselineBenchmarks = [
            new()
            {
                Statistics = new()
                {
                    Mean = 12
                }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Count(s => s.Mean!.Status == ComparisonStatus.Removed) == 1),
                    Arg.Any<string>());
    }

    [Fact]
    public void When_Only_Have_Method_On_Target_Should_Have_OneComparation_With_Status_New()
    {
        // Arrange
        _targetBenchmarks = [
            new()
            {
                Memory = new()
                {
                    BytesAllocatedPerOperation = 120
                }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Count(s => s.Allocated!.Status == ComparisonStatus.New) == 1),
                    Arg.Any<string>());
    }

    [Fact]
    public void When_Only_Have_Method_On_Target_Should_Use_Target_Type_Method_And_FullName()
    {
        // Arrange
        _targetBenchmarks = [
            new()
            {
                Type = "TargetType",
                Method = "TargetMethod",
                FullName = "TargetFullName",
                Memory = new()
                {
                    BytesAllocatedPerOperation = 120
                }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Count == 1 &&
                    i.Comparisons.First().Type == "TargetType" &&
                    i.Comparisons.First().Name == "TargetMethod" &&
                    i.Comparisons.First().FullName == "TargetFullName"),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Target_Doesnt_Have_Benchmarks_Should_Generate_Comparisons_With_Status_Removed()
    {
        // Arrange
        _baselineBenchmarks = [
            new()
            {
                FullName = "TestMethod",
                Method = "TestMethod",
                Statistics = new()
                {
                    Mean = 12
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 120
                }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Count == 1 &&
                    i.Comparisons.First().Allocated!.Status == ComparisonStatus.Removed &&
                    i.Comparisons.First().Mean!.Status == ComparisonStatus.Removed),
                Arg.Any<string>());
    }

    [Fact]
    public void Each_Method_In_Benchmarks_Should_Appear_Once_In_ComparerReport()
    {
        // Arrange
        _baselineBenchmarks = [
            new()
            {
                FullName = "method one",
                Method = "method 1",
                Statistics = new()
                {
                    Mean = 12
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 120
                }
            },
            new()
            {
                FullName = "method two",
                Method = "method 2",
                Statistics = new()
                {
                    Mean = 13
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 130
                }
            }];
        _targetBenchmarks = [
            new()
            {
                FullName = "method one",
                Method = "method 1",
                Statistics = new()
                {
                    Mean = 12
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 120
                }
            },
            new()
            {
                FullName = "method two",
                Method = "method 2",
                Statistics = new()
                {
                    Mean = 13
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 130
                }
            }];
        var baselineCount = _baselineBenchmarks.Count;
        var targetCount = _targetBenchmarks.Count;


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Count == baselineCount &&
                    i.Comparisons.Count == targetCount),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Have_Two_Baseline_And_TargetShould_Register_Two_Comparisons()
    {
        // Arrange
        _baselineBenchmarks = [
            new()
            {
                FullName = "Benchmark1",
                Method = "Benchmark1",
                Statistics = new()
                {
                    Mean = 45
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 1234
                }
            },
            new()
            {
                FullName = "Benchmark2",
                Method = "Benchmark2",
                Statistics = new()
                {
                    Mean = 124
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 12334
                }
            }];

        _targetBenchmarks = [
            new()
            {
                FullName = "Benchmark1",
                Method = "Benchmark1",
                Statistics = new()
                {
                    Mean = 45
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 1234
                }
            },
            new()
            {
                FullName = "Benchmark2",
                Method = "Benchmark2",
                Statistics = new()
                {
                    Mean = 124
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 12334
                }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Count == 2 &&
                    i.Comparisons.Count(c => c.FullName == "Benchmark1") == 1 &&
                    i.Comparisons.Count(c => c.FullName == "Benchmark2") == 1),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Baseline_Have_Has_Value_For_Gen0Collections_Should_Have_Comparison_Baseline_With_For_Gen0Collections()
    {
        // Arrange
        _baselineBenchmarks = [
            new()
            {
                Memory = new()
                {
                    Gen0Collections = 20000,
                    TotalOperations = 1000
                }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen0Collections!.Baseline).First() != null),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Target_Has_Value_For_Gen0Collections_Should_Have_Comparison_With_Target_For_Gen0Collections()
    {
        // Arrange
        _targetBenchmarks = [
            new()
            {
                Memory = new()
                {
                    Gen0Collections = 2000,
                    TotalOperations = 100
                }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen0Collections!.Target).First() != null),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Baseline_Has_Value_For_Gen1Collections_Should_Have_Comparison_Baseline_With_For_Gen1Collections()
    {
        // Arrange
        _baselineBenchmarks = [
            new()
            {
                Memory = new()
                {
                    Gen1Collections = 20000,
                    TotalOperations = 1000
                }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen1Collections!.Baseline).First() != null),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Target_Has_Value_For_Gen1Collections_Should_Have_Comparison_With_Target_For_Gen1Collections()
    {
        // Arrange
        _targetBenchmarks = [
            new()
            {
                Memory = new()
                {
                    Gen1Collections = 2000,
                    TotalOperations = 100
                }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen1Collections!.Target).First() != null),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Baseline_Has_Value_For_Gen2Collections_Should_Have_Comparison_Baseline_With_For_Gen2Collections()
    {
        // Arrange
        _baselineBenchmarks = [
            new()
            {
                Memory = new()
                {
                    Gen2Collections = 20000,
                    TotalOperations = 1000
                }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen2Collections!.Baseline).First() != null),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Target_Has_Value_For_Gen2Collections_Should_Have_Comparison_With_Target_For_Gen2Collections()
    {
        // Arrange
        _targetBenchmarks = [
            new()
            {
                Memory = new()
                {
                    Gen2Collections = 2000,
                    TotalOperations = 100
                }
            }];


        // Act
        _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"]
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen2Collections!.Target).First() != null),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Comparison_Generate_Threshold_Hit_And_FailOnThresholdHit_Is_False_Should_Return_Success_ExitCode()
    {
        // Arrange
        _validator
            .When(v => v.EvaluateThresholds(Arg.Any<ComparerReport>(), Arg.Any<string?>(), Arg.Any<string?>()))
            .Do(ci => ci.ArgAt<ComparerReport>(0).HitThresholds.Add("Mean threshold hit for 'test hit'"));


        // Act
        var act = _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"],
            FailOnThresholdHit = false
        });


        // Assert
        _exporter
            .Received()
            .Generate(Arg.Is<ComparerReport>(i => i.HitThresholds.Count > 0), Arg.Any<string>());
        act.Should().Be(Constants.ExitCodes.SUCCESS);
    }

    [Fact]
    public void When_Comparison_Generate_Threshold_Hit_And_FailOnThresholdHit_Is_True_Should_Return_ThresholdHit_ExitCode()
    {
        // Arrange
        _validator
            .When(v => v.EvaluateThresholds(Arg.Any<ComparerReport>(), Arg.Any<string?>(), Arg.Any<string?>()))
            .Do(ci => ci.ArgAt<ComparerReport>(0).HitThresholds.Add("Mean threshold hit for 'test hit'"));


        // Act
        var act = _handler.Execute(new()
        {
            Baseline = "baseline",
            Target = "target",
            Formats = ["xpto"],
            FailOnThresholdHit = true
        });


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i => i.HitThresholds.Count > 0),
                Arg.Any<string>());
        act.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
    }
}
