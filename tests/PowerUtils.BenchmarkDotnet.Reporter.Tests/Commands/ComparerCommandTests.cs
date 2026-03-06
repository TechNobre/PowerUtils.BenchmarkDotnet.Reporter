using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands;
using PowerUtils.BenchmarkDotnet.Reporter.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Validations;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands;

public sealed class ComparerCommandTests
{
    private readonly Func<string?, List<BenchmarkResport>> _readBenchmarks;
    private List<BenchmarkResport> _baselineBenchmarks;
    private List<BenchmarkResport> _targetBenchmarks;

    private readonly IKeyedServiceProvider _serviceProvider;
    private readonly IReportValidation _validation;
    private readonly IExporter _exporter;

    private readonly ComparerCommand _command;


    public ComparerCommandTests()
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

        _validation = Substitute.For<IReportValidation>();
        _exporter = Substitute.For<IExporter>();


        _serviceProvider = Substitute.For<IKeyedServiceProvider>();
        _serviceProvider
            .GetRequiredKeyedService(Arg.Any<Type>(), Arg.Any<object?>())
            .Returns(_exporter);

        _command = new(
            _readBenchmarks,
            _validation,
            _serviceProvider);
    }


    [Fact]
    public void When_Have_Baseline_And_Target_Shouldnt_Generate_Warning()
    {
        // Arrange
        _baselineBenchmarks = [new() { Header = new() { HostEnvironmentInfo = new() { Configuration = "RELEASE" } } }];
        _targetBenchmarks = [new() { Header = new() { HostEnvironmentInfo = new() { Configuration = "RELEASE" } } }];

        var validation = new ReportValidation();


        var command = new ComparerCommand(
            _readBenchmarks,
            validation,
            _serviceProvider);


        // Act
        command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(Arg.Is<ComparerReport>(i => i.Warnings.Count == 0), Arg.Any<string>());
    }

    [Fact]
    public void When_Report_Has_Warnings_And_WarningThrow_Equals_False_Should_Not_Throw_Exception()
    {
        // Arrange
        var expectedMessage = Guid.NewGuid().ToString();

        _validation
            .HostEnvironmentValidate(Arg.Any<BenchmarkResport>(), Arg.Any<BenchmarkResport>())
            .Returns([expectedMessage]);


        // Act
        void act() => _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void When_Baseline_Doesnt_Have_Benchmarks_Should_Generate_Zero_Comparisons()
    {
        // Arrange & Act
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


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
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Mean!.Status).First() == ComparisonStatus.Removed &&
                    i.Comparisons.Select(s => s.Mean!.Unit).First() == "ns"),
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
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Allocated!.Status).First() == ComparisonStatus.New &&
                    i.Comparisons.Select(s => s.Allocated!.Unit).First() == "B"),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Has_Invalid_Mean_Threshold_Should_Throw_Exception()
    {
        // Arrange & Act
        void act() => _command.Execute(
            "baseline",
            "target",
            "invalid",
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        Should.Throw<FormatException>(act);
    }

    [Fact]
    public void When_Has_Invalid_Allocation_Threshold_Should_Throw_Exception()
    {
        // Arrange & Act
        void act() => _command.Execute(
            "baseline",
            "target",
            null,
            "invalid",
            ["xpto"],
            "",
            false,
            false);


        // Assert
        Should.Throw<FormatException>(act);
    }

    [Fact]
    public void Should_Register_Thrashold_Values_Above_Setted_PercentsThrashold()
    {
        // Arrange
        _baselineBenchmarks = [
            new()
            {
                FullName = "test hit",
                Method = "test hit",
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
                FullName = "test equals",
                Method = "test equals",
                Statistics = new()
                {
                    Mean = 45
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 1234
                }
            }];

        _targetBenchmarks = [
            new()
            {
                FullName = "test hit",
                Method = "test hit",
                Statistics = new()
                {
                    Mean = 1200
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 120000
                }
            },
            new()
            {
                FullName = "test equals",
                Method = "test equals",
                Statistics = new()
                {
                    Mean = 45
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 1234
                }
            }];


        // Act
        _command.Execute(
            "baseline",
            "target",
            "10%",
            "11%",
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.HitThresholds.Contains("Mean threshold hit for 'test hit'") &&
                    i.HitThresholds.Contains("Allocation threshold hit for 'test hit'")),
                Arg.Any<string>());
    }

    [Fact]
    public void Should_Register_Thrashold_Values_Above_Setted_UnitThrashold()
    {
        // Arrange
        _baselineBenchmarks = [
            new()
            {
                FullName = "test hit",
                Method = "test hit",
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
                FullName = "test equals",
                Method = "test equals",
                Statistics = new()
                {
                    Mean = 45
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 1234
                }
            }];

        _targetBenchmarks = [
            new()
            {
                FullName = "test hit",
                Method = "tetest hitst",
                Statistics = new()
                {
                    Mean = 1200
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 120000
                }
            },
            new()
            {
                FullName = "test equals",
                Method = "test equals",
                Statistics = new()
                {
                    Mean = 45
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 1234
                }
            }];


        // Act
        _command.Execute(
            "baseline",
            "target",
            "5ns",
            "5B",
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.HitThresholds.Contains("Mean threshold hit for 'test hit'") &&
                    i.HitThresholds.Contains("Allocation threshold hit for 'test hit'")),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Worning_Is_Generated_Should_Be_Stored_In_ComparerReport()
    {
        // Arrange
        const string EXPECTED_MESSAGE = "Processor Name is different";

        _validation
            .HostEnvironmentValidate(Arg.Any<BenchmarkResport>(), Arg.Any<BenchmarkResport>())
            .Returns([EXPECTED_MESSAGE]);

        _baselineBenchmarks = [
            new()
            {
                Statistics = new()
                {
                    Mean = 12
                }
            }];


        // Act
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Warnings.Contains(EXPECTED_MESSAGE)),
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
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


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


        // Act
        _command.Execute(
            "baseline",
            "target",
            "10%",
            "11%",
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i => i.Comparisons.Count == 2),
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
        _command.Execute(
            "baseline",
            "target",
            "10%",
            "11%",
            ["xpto"],
            "",
            false,
            false);


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
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen0Collections!.Baseline).First() != null),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Target_Have_Has_Value_For_Gen0Collections_Should_Have_Comparison_With_Target_For_Gen0Collections()
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
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen0Collections!.Target).First() != null),
                Arg.Any<string>());
    }


    [Fact]
    public void When_Baseline_Have_Has_Value_For_Gen1Collections_Should_Have_Comparison_Baseline_With_For_Gen1Collections()
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
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen1Collections!.Baseline).First() != null),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Target_Have_Has_Value_For_Gen1Collections_Should_Have_Comparison_With_Target_For_Gen1Collections()
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
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen1Collections!.Target).First() != null),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Baseline_Have_Has_Value_For_Gen2Collections_Should_Have_Comparison_Baseline_With_For_Gen2Collections()
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
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen2Collections!.Baseline).First() != null),
                Arg.Any<string>());
    }

    [Fact]
    public void When_Target_Have_Has_Value_For_Gen2Collections_Should_Have_Comparison_With_Target_For_Gen2Collections()
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
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            false,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i =>
                    i.Comparisons.Select(s => s.Gen2Collections!.Target).First() != null),
                Arg.Any<string>());
    }


    [Fact]
    public void When_Comparison_Generate_Warning_And_FailOnWarnings_Is_False_Should_Return_Success_ExitCode()
    {
        // Arrange
        var failOnWarnings = false;

        _validation
            .HostEnvironmentValidate(Arg.Any<BenchmarkResport>(), Arg.Any<BenchmarkResport>())
            .Returns(["Warning message"]);

        _baselineBenchmarks = [
            new()
            {
                Statistics = new()
                {
                    Mean = 12
                }
            }];


        // Act
        var act = _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            failOnWarnings,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i => i.Warnings.Count > 0),
                Arg.Any<string>());

        act.ShouldBe(Constants.ExitCodes.SUCCESS);
    }

    [Fact]
    public void When_Comparison_Generate_Warning_And_FailOnWarnings_Is_True_Should_Return_Failure_Warning_ExitCode()
    {
        // Arrange
        var failOnWarnings = true;

        _validation
            .HostEnvironmentValidate(Arg.Any<BenchmarkResport>(), Arg.Any<BenchmarkResport>())
            .Returns(["Warning message"]);

        _baselineBenchmarks = [
            new()
            {
                Statistics = new()
                {
                    Mean = 12
                }
            }];


        // Act
        var act = _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "",
            failOnWarnings,
            false);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i => i.Warnings.Count > 0),
                Arg.Any<string>());

        act.ShouldBe(Constants.ExitCodes.WARNING);
    }

    [Fact]
    public void When_Comparison_Generate_Threshold_Hit_And_FailOnThresholdHit_Is_False_Should_Return_Success_ExitCode()
    {
        // Arrange
        var failOnThresholdHit = false;

        _baselineBenchmarks = [
            new()
            {
                FullName = "test hit",
                Method = "test hit",
                Statistics = new()
                {
                    Mean = 12
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 120
                }
            }];

        _targetBenchmarks = [
            new()
            {
                FullName = "test hit",
                Method = "tetest hitst",
                Statistics = new()
                {
                    Mean = 1200
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 120000
                }
            }];


        // Act
        var act = _command.Execute(
            "baseline",
            "target",
            "5ns",
            "5B",
            ["xpto"],
            "",
            false,
            failOnThresholdHit);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i => i.HitThresholds.Count > 0),
                Arg.Any<string>());

        act.ShouldBe(Constants.ExitCodes.SUCCESS);
    }

    [Fact]
    public void When_Comparison_Generate_Threshold_Hit_And_FailOnThresholdHit_Is_True_Should_Return_ThresholdHit_ExitCode()
    {
        // Arrange
        var failOnThresholdHit = true;

        _baselineBenchmarks = [
            new()
            {
                FullName = "test hit",
                Method = "test hit",
                Statistics = new()
                {
                    Mean = 12
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 120
                }
            }];
        _targetBenchmarks = [
            new()
            {
                FullName = "test hit",
                Method = "tetest hitst",
                Statistics = new()
                {
                    Mean = 1200
                },
                Memory = new()
                {
                    BytesAllocatedPerOperation = 120000
                }
            }];


        // Act
        var act = _command.Execute(
            "baseline",
            "target",
            "5ns",
            "5B",
            ["xpto"],
            "",
            false,
            failOnThresholdHit);


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i => i.HitThresholds.Count > 0),
                Arg.Any<string>());

        act.ShouldBe(Constants.ExitCodes.THRESHOLD_HIT);
    }
}
