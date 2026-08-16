using System;
using System.IO;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.CompareHelpersTests;

public sealed class ReadJsonBenchmarkReportsTests : IDisposable
{
    private readonly string _tempDirectory;


    public ReadJsonBenchmarkReportsTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if(Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Theory]
    [InlineData("report-20", "Demo.Benchmarks.ArrayProcessorBenchmarks-report-full.json")]
    [InlineData("report-20", "Demo.Benchmarks.ArrayProcessorBenchmarks-report-full-compressed.json")]
    [InlineData("report-20", "Demo.Benchmarks.ArrayProcessorBenchmarks-report-brief.json")]
    [InlineData("report-20", "Demo.Benchmarks.ArrayProcessorBenchmarks-report-brief-compressed.json")]
    public void When_Pass_Valid_File_Should_Return_Report(string folder, string file)
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("test-data", folder, file));


        // Act
        var act = CompareHelpers.ReadJsonBenchmarkReports(path);


        // Assert
        act.Should().NotBeNull();
    }

    [Fact]
    public void When_Read_Report_Should_Contain_FilePath_And_FileName()
    {
        // Arrange
        var fileName = "Benchmark-report-full.json";
        var path = Path.GetFullPath(Path.Combine("test-data", "report-01", fileName));


        // Act
        var act = CompareHelpers.ReadJsonBenchmarkReports(path).Single();


        // Assert
        act.FilePath.Should().Be(path);
        act.FileName.Should().Be(fileName);
    }

    [Fact]
    public void When_File_With_Invalid_PropertyType_Should_Throw_InvalidOperationException_With_InnerException_JsonException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}{CompareHelpers.REPORT_FILE_ENDS}");
        File.WriteAllText(
            filePath,
            """
            {
                "HostEnvironmentInfo":{
                    "ChronometerFrequency":{
                        "Hertz":"1000000000"
                    }
                }
            }
            """);


        // Act
        Action act = () => CompareHelpers.ReadJsonBenchmarkReports(filePath);


        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().StartWith($"Failed to deserialize the file '{filePath}'. ");
    }

    [Fact]
    public void When_File_Doesnt_Have_Benchmarks_Property_Should_Not_Throw()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}{CompareHelpers.REPORT_FILE_ENDS}");
        File.WriteAllText(
            filePath,
            """
            {
                "Title": "Benchmark-without-benchmarks"
            }
            """);


        // Act
        var act = CompareHelpers.ReadJsonBenchmarkReports(filePath).Single();


        // Assert
        act.Benchmarks.Should().BeNull();
    }

    [Fact]
    public void Each_BenchmarkRecord_Should_Have_Header_Populated()
    {
        // Arrange
        var fileName = "Benchmark-report-full.json";
        var path = Path.GetFullPath(Path.Combine("test-data", "report-01", fileName));


        // Act
        var act = CompareHelpers.ReadJsonBenchmarkReports(path).Single();


        // Assert
        foreach(var benchmark in act.Benchmarks ?? [])
        {
            benchmark.Header.Should().NotBeNull();
            benchmark.Header!.FilePath.Should().Be(act.FilePath);
            benchmark.Header.FileName.Should().Be(act.FileName);
            benchmark.Header.Title.Should().Be(act.Title);

            benchmark.Header.HostEnvironmentInfo.Should().NotBeNull();
            benchmark.Header.HostEnvironmentInfo!.BenchmarkDotNetCaption.Should().Be(act.HostEnvironmentInfo?.BenchmarkDotNetCaption);
            benchmark.Header.HostEnvironmentInfo.BenchmarkDotNetVersion.Should().Be(act.HostEnvironmentInfo?.BenchmarkDotNetVersion);
            benchmark.Header.HostEnvironmentInfo.OsVersion.Should().Be(act.HostEnvironmentInfo?.OsVersion);
            benchmark.Header.HostEnvironmentInfo.ProcessorName.Should().Be(act.HostEnvironmentInfo?.ProcessorName);
            benchmark.Header.HostEnvironmentInfo.PhysicalProcessorCount.Should().Be(act.HostEnvironmentInfo?.PhysicalProcessorCount);
            benchmark.Header.HostEnvironmentInfo.PhysicalCoreCount.Should().Be(act.HostEnvironmentInfo?.PhysicalCoreCount);
            benchmark.Header.HostEnvironmentInfo.LogicalCoreCount.Should().Be(act.HostEnvironmentInfo?.LogicalCoreCount);
            benchmark.Header.HostEnvironmentInfo.RuntimeVersion.Should().Be(act.HostEnvironmentInfo?.RuntimeVersion);
            benchmark.Header.HostEnvironmentInfo.Architecture.Should().Be(act.HostEnvironmentInfo?.Architecture);
            benchmark.Header.HostEnvironmentInfo.HasAttachedDebugger.Should().Be(act.HostEnvironmentInfo?.HasAttachedDebugger);
            benchmark.Header.HostEnvironmentInfo.HasRyuJit.Should().Be(act.HostEnvironmentInfo?.HasRyuJit);
            benchmark.Header.HostEnvironmentInfo.Configuration.Should().Be(act.HostEnvironmentInfo?.Configuration);
            benchmark.Header.HostEnvironmentInfo.JitModules.Should().Be(act.HostEnvironmentInfo?.JitModules);
            benchmark.Header.HostEnvironmentInfo.DotNetCliVersion.Should().Be(act.HostEnvironmentInfo?.DotNetCliVersion);

            benchmark.Header.HostEnvironmentInfo.ChronometerFrequency.Should().NotBeNull();
            ((int?)benchmark.Header.HostEnvironmentInfo.ChronometerFrequency!.Hertz).Should().Be(act.HostEnvironmentInfo?.ChronometerFrequency?.Hertz);

            benchmark.Header.HostEnvironmentInfo.HardwareTimerKind.Should().Be(act.HostEnvironmentInfo?.HardwareTimerKind);
        }
    }
}
