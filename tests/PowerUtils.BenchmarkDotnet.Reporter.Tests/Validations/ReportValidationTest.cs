using PowerUtils.BenchmarkDotnet.Reporter.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Validations;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Validations;

public class ReportValidationTest
{
    [Fact]
    public void When_All_Properties_Equal_Returns_EmptyList()
    {
        // Arrange
        var baseline = _createBenchmarkReport();
        var target = _createBenchmarkReport();
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void When_OsVersion_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(osVersion: "Windows 10");
        var target = _createBenchmarkReport(osVersion: "Windows 11");
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldContain("OS Version is different");
    }

    [Fact]
    public void When_ProcessorName_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(processorName: "AMD Ryzen 6 3600X");
        var target = _createBenchmarkReport(processorName: "AMD Ryzen 5 3600X");
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldContain("Processor Name is different");
    }

    [Fact]
    public void When_PhysicalProcessorCount_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(physicalProcessorCount: 1);
        var target = _createBenchmarkReport(physicalProcessorCount: 3);
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldContain("Physical Processor Count is different");
    }

    [Fact]
    public void When_PhysicalCoreCount_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(physicalCoreCount: 1);
        var target = _createBenchmarkReport(physicalCoreCount: 3);
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldContain("Physical Core Count is different");
    }

    [Fact]
    public void When_LogicalCoreCount_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(logicalCoreCount: 1);
        var target = _createBenchmarkReport(logicalCoreCount: 3);
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldContain("Logical Core Count is different");
    }

    [Fact]
    public void When_RuntimeVersion_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(runtimeVersion: ".NET 9.0.2 (9.0.225.6610)");
        var target = _createBenchmarkReport(runtimeVersion: ".NET 19.0.2 (9.0.225.6610)");
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldContain("Runtime Version is different");
    }

    [Fact]
    public void When_Architecture_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(architecture: "X64");
        var target = _createBenchmarkReport(architecture: "X32");
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldContain("Architecture is different");
    }

    [Fact]
    public void When_DotNetCliVersion_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(dotNetCliVersion: "10.0.100-preview.1.25120.13");
        var target = _createBenchmarkReport(dotNetCliVersion: "11.0.100-preview.1.25120.13");
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldContain("DotNet CLI Version is different");
    }

    [Fact]
    public void When_Hertz_Is_Different_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(hertz: 122);
        var target = _createBenchmarkReport(hertz: 23423);
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldContain("Chronometer Frequency is different");
    }

    [Fact]
    public void When_BaselineConfiguration_Is_Not_Release_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport(configuration: "ddd");
        var target = _createBenchmarkReport();
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldContain("The baseline report wasn't executed in RELEASE mode");
    }

    [Fact]
    public void When_TargetConfiguration_Is_Not_Release_Returns_Message()
    {
        // Arrange
        var baseline = _createBenchmarkReport();
        var target = _createBenchmarkReport(configuration: "ddd");
        var validation = new ReportValidation();


        // Act
        var result = validation.HostEnvironmentValidate(baseline, target);


        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldContain("The target report wasn't executed in RELEASE mode");
    }

    private static BenchmarkFullJsonResport _createBenchmarkReport(
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
            },

            Benchmarks = []
        };
}
