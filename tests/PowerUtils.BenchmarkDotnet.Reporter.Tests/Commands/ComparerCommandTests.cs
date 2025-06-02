using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands;
using PowerUtils.BenchmarkDotnet.Reporter.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Validations;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands;

public sealed class ComparerCommandTests
{
    private readonly Func<string?, BenchmarkFullJsonResport> _readFullJsonReport;
    private readonly BenchmarkFullJsonResport _baselineReport;
    private readonly BenchmarkFullJsonResport _targetReport;

    private readonly IReportValidation _validation;
    private readonly IExporter _exporter;

    private readonly ComparerCommand _command;


    public ComparerCommandTests()
    {
        _baselineReport = new();
        _targetReport = new();

        _readFullJsonReport = (path)
            => path switch
            {
                "baseline" => _baselineReport,
                "target" => _targetReport,
                _ => throw new ArgumentException()
            };

        _validation = Substitute.For<IReportValidation>();
        _exporter = Substitute.For<IExporter>();


        var provider = Substitute.For<IKeyedServiceProvider>();
        provider
            .GetRequiredKeyedService(Arg.Any<Type>(), Arg.Any<object?>())
            .Returns(_exporter);

        _command = new(
            _readFullJsonReport,
            _validation,
            provider);
    }


    [Fact]
    public void When_Report_Has_Warnings_And_WarningThrow_Equals_False_Should_Not_Throw_Exception()
    {
        // Arrange
        var expectedMessage = Guid.NewGuid().ToString();

        _validation
            .HostEnvironmentValidate(Arg.Any<BenchmarkFullJsonResport>(), Arg.Any<BenchmarkFullJsonResport>())
            .Returns([expectedMessage]);


        // Act
        void act() => _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "");


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
            "");


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
        (_baselineReport.Benchmarks = []).Add(new()
        {
            Statistics = new()
            {
                Mean = 12
            }
        });


        // Act
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "");


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
        (_targetReport.Benchmarks = []).Add(new()
        {
            Memory = new()
            {
                BytesAllocatedPerOperation = 120
            }
        });


        // Act
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "");


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
            "");


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
            "");


        // Assert
        Should.Throw<FormatException>(act);
    }

    [Fact]
    public void Should_Register_Thrashold_Values_Above_Setted_PercentsThrashold()
    {
        // Arrange
        _baselineReport.Benchmarks = [];
        _targetReport.Benchmarks = [];

        _baselineReport.Benchmarks.Add(new()
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
        });

        _targetReport.Benchmarks.Add(new()
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
        });

        _baselineReport.Benchmarks.Add(new()
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
        });

        _targetReport.Benchmarks.Add(new()
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
        });


        // Act
        _command.Execute(
            "baseline",
            "target",
            "10%",
            "11%",
            ["xpto"],
            "");


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
        _baselineReport.Benchmarks = [];
        _targetReport.Benchmarks = [];

        _baselineReport.Benchmarks.Add(new()
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
        });

        _targetReport.Benchmarks.Add(new()
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
        });

        _baselineReport.Benchmarks.Add(new()
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
        });

        _targetReport.Benchmarks.Add(new()
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
        });


        // Act
        _command.Execute(
            "baseline",
            "target",
            "5ns",
            "5B",
            ["xpto"],
            "");


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
            .HostEnvironmentValidate(Arg.Any<BenchmarkFullJsonResport>(), Arg.Any<BenchmarkFullJsonResport>())
            .Returns([EXPECTED_MESSAGE]);


        // Act
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "");


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
        (_baselineReport.Benchmarks = []).Add(new()
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
        });
        _targetReport.Benchmarks = [];


        // Act
        _command.Execute(
            "baseline",
            "target",
            null,
            null,
            ["xpto"],
            "");


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
        _baselineReport.Benchmarks = [];
        _targetReport.Benchmarks = [];

        _baselineReport.Benchmarks.Add(new()
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
        });
        _baselineReport.Benchmarks.Add(new()
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
        });

        _targetReport.Benchmarks.Add(new()
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
        });
        _targetReport.Benchmarks.Add(new()
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
        });


        // Act
        _command.Execute(
            "baseline",
            "target",
            "10%",
            "11%",
            ["xpto"],
            "");


        // Assert
        _exporter
            .Received()
            .Generate(
                Arg.Is<ComparerReport>(i => i.Comparisons.Count == 2),
                Arg.Any<string>());
    }
}
