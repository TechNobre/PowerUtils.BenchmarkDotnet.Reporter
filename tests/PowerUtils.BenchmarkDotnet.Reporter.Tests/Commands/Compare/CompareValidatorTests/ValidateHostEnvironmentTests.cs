using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.CompareValidatorTests;

public sealed class ValidateHostEnvironmentTests
{
    [Fact]
    public void When_All_Properties_Equal_Returns_EmptyList()
    {
        // Arrange
        var baseline = _createBenchmarkReport();
        var target = _createBenchmarkReport();
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void When_BaselineReport_Is_Null_Should_Return_EmptyList()
    {
        // Arrange
        BenchmarkReport? baseline = null;
        var target = _createBenchmarkReport();
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void When_TargetReport_Is_Null_Should_Return_EmptyList()
    {
        // Arrange
        var baseline = _createBenchmarkReport();
        BenchmarkReport? target = null;
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void When_OsVersion_Only_Differs_In_Case_Should_Return_EmptyList()
    {
        // Arrange
        var baseline = _createBenchmarkReport(osVersion: "Windows 10");
        var target = _createBenchmarkReport(osVersion: "WINDOWS 10");
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void When_OsVersion_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(osVersion: "Windows 10");
        var target = _createBenchmarkReport(osVersion: "Windows 11");
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Contain("OS Version is different");
    }

    [Fact]
    public void When_ProcessorName_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(processorName: "AMD Ryzen 6 3600X");
        var target = _createBenchmarkReport(processorName: "AMD Ryzen 5 3600X");
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Contain("Processor Name is different");
    }

    [Fact]
    public void When_PhysicalProcessorCount_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(physicalProcessorCount: 1);
        var target = _createBenchmarkReport(physicalProcessorCount: 3);
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Contain("Physical Processor Count is different");
    }

    [Fact]
    public void When_PhysicalCoreCount_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(physicalCoreCount: 1);
        var target = _createBenchmarkReport(physicalCoreCount: 3);
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Contain("Physical Core Count is different");
    }

    [Fact]
    public void When_LogicalCoreCount_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(logicalCoreCount: 1);
        var target = _createBenchmarkReport(logicalCoreCount: 3);
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Contain("Logical Core Count is different");
    }

    [Fact]
    public void When_RuntimeVersion_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(runtimeVersion: ".NET 9.0.2 (9.0.225.6610)");
        var target = _createBenchmarkReport(runtimeVersion: ".NET 19.0.2 (9.0.225.6610)");
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Contain("Runtime Version is different");
    }

    [Fact]
    public void When_Architecture_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(architecture: "X64");
        var target = _createBenchmarkReport(architecture: "X32");
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Contain("Architecture is different");
    }

    [Fact]
    public void When_DotNetCliVersion_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(dotNetCliVersion: "10.0.100-preview.1.25120.13");
        var target = _createBenchmarkReport(dotNetCliVersion: "11.0.100-preview.1.25120.13");
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Contain("DotNet CLI Version is different");
    }

    [Fact]
    public void When_Hertz_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(hertz: 122);
        var target = _createBenchmarkReport(hertz: 23423);
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Contain("Chronometer Frequency is different");
    }

    [Fact]
    public void When_BaselineConfiguration_Is_Not_Release_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(configuration: "ddd");
        var target = _createBenchmarkReport();
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Contain("The baseline report wasn't executed in RELEASE mode");
    }

    [Fact]
    public void When_TargetConfiguration_Is_Not_Release_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport();
        var target = _createBenchmarkReport(configuration: "ddd");
        var validator = new CompareValidator();


        // Act
        var result = validator.ValidateHostEnvironment(baseline, target);


        // Assert
        result.Count.Should().Be(1);
        result[0].Should().Contain("The target report wasn't executed in RELEASE mode");
    }

    private static BenchmarkReport _createBenchmarkReport(
        string osVersion = "Windows 11 (10.0.26100.3323)",
        string processorName = "AMD Ryzen 5 3600X",
        int? physicalProcessorCount = 1,
        int? physicalCoreCount = 6,
        int? logicalCoreCount = 12,
        string runtimeVersion = ".NET 9.0.2 (9.0.225.6610)",
        string architecture = "X64",
        string dotNetCliVersion = "10.0.100-preview.1.25120.13",
        int hertz = 10000000,
        string configuration = "RELEASE") => new()
        {
            Header = new()
            {
                HostEnvironmentInfo = new()
                {
                    OsVersion = osVersion,
                    ProcessorName = processorName,
                    PhysicalProcessorCount = physicalProcessorCount,
                    PhysicalCoreCount = physicalCoreCount,
                    LogicalCoreCount = logicalCoreCount,
                    RuntimeVersion = runtimeVersion,
                    Architecture = architecture,
                    DotNetCliVersion = dotNetCliVersion,
                    ChronometerFrequency = new() { Hertz = hertz },
                    Configuration = configuration,
                    BenchmarkDotNetCaption = "BenchmarkDotNet",
                    BenchmarkDotNetVersion = "0.14.0",
                    HardwareTimerKind = "Unknown"
                }
            }
        };
}
