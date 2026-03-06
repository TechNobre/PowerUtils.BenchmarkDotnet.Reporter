using System;
using System.IO;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers.IOHelpersTests;

public sealed class ReadJsonBenchmarkResportsTest : IDisposable
{
    private readonly string _tempDirectory;


    public ReadJsonBenchmarkResportsTest()
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
        var act = IOHelpers.ReadJsonBenchmarkResports(path);


        // Assert
        act.ShouldNotBeNull();
    }

    [Fact]
    public void When_Read_Report_Should_Contain_FilePath_And_FileName()
    {
        // Arrange
        var fileName = "Benchmark-report-full.json";
        var path = Path.GetFullPath(Path.Combine("test-data", "report-01", fileName));


        // Act
        var act = IOHelpers.ReadJsonBenchmarkResports(path).Single();


        // Assert
        act.FilePath.ShouldBe(path);
        act.FileName.ShouldBe(fileName);
    }

    [Fact]
    public void When_File_With_Invalid_PropertyType_Should_Throw_InvalidOperationException_With_InnerException_JsonException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}{IOHelpers.REPORT_FILE_ENDS}");
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
        Reporter.Models.JsonBenchmarkResports[] act() => IOHelpers.ReadJsonBenchmarkResports(filePath);


        // Assert
        Should.Throw<InvalidOperationException>((Func<Reporter.Models.JsonBenchmarkResports[]>)act)
            .Message.ShouldStartWith($"Failed to deserialize the file '{filePath}'. ");
    }

    [Fact]
    public void Each_BenchmarkRecord_Should_Have_Header_Populated()
    {
        // Arrange
        var fileName = "Benchmark-report-full.json";
        var path = Path.GetFullPath(Path.Combine("test-data", "report-01", fileName));


        // Act
        var act = IOHelpers.ReadJsonBenchmarkResports(path).Single();


        // Assert
        foreach(var benchmark in act.Benchmarks ?? [])
        {
            benchmark.Header.ShouldNotBeNull();
            benchmark.Header.FilePath.ShouldBe(act.FilePath);
            benchmark.Header.FileName.ShouldBe(act.FileName);
            benchmark.Header.Title.ShouldBe(act.Title);

            benchmark.Header.HostEnvironmentInfo.ShouldNotBeNull();
            benchmark.Header.HostEnvironmentInfo.BenchmarkDotNetCaption.ShouldBe(act.HostEnvironmentInfo?.BenchmarkDotNetCaption);
            benchmark.Header.HostEnvironmentInfo.BenchmarkDotNetVersion.ShouldBe(act.HostEnvironmentInfo?.BenchmarkDotNetVersion);
            benchmark.Header.HostEnvironmentInfo.OsVersion.ShouldBe(act.HostEnvironmentInfo?.OsVersion);
            benchmark.Header.HostEnvironmentInfo.ProcessorName.ShouldBe(act.HostEnvironmentInfo?.ProcessorName);
            benchmark.Header.HostEnvironmentInfo.PhysicalProcessorCount.ShouldBe(act.HostEnvironmentInfo?.PhysicalProcessorCount);
            benchmark.Header.HostEnvironmentInfo.PhysicalCoreCount.ShouldBe(act.HostEnvironmentInfo?.PhysicalCoreCount);
            benchmark.Header.HostEnvironmentInfo.LogicalCoreCount.ShouldBe(act.HostEnvironmentInfo?.LogicalCoreCount);
            benchmark.Header.HostEnvironmentInfo.RuntimeVersion.ShouldBe(act.HostEnvironmentInfo?.RuntimeVersion);
            benchmark.Header.HostEnvironmentInfo.Architecture.ShouldBe(act.HostEnvironmentInfo?.Architecture);
            benchmark.Header.HostEnvironmentInfo.HasAttachedDebugger.ShouldBe(act.HostEnvironmentInfo?.HasAttachedDebugger);
            benchmark.Header.HostEnvironmentInfo.HasRyuJit.ShouldBe(act.HostEnvironmentInfo?.HasRyuJit);
            benchmark.Header.HostEnvironmentInfo.Configuration.ShouldBe(act.HostEnvironmentInfo?.Configuration);
            benchmark.Header.HostEnvironmentInfo.JitModules.ShouldBe(act.HostEnvironmentInfo?.JitModules);
            benchmark.Header.HostEnvironmentInfo.DotNetCliVersion.ShouldBe(act.HostEnvironmentInfo?.DotNetCliVersion);

            benchmark.Header.HostEnvironmentInfo.ChronometerFrequency.ShouldNotBeNull();
            ((int?)benchmark.Header.HostEnvironmentInfo.ChronometerFrequency.Hertz).ShouldBe(act.HostEnvironmentInfo?.ChronometerFrequency?.Hertz);

            benchmark.Header.HostEnvironmentInfo.HardwareTimerKind.ShouldBe(act.HostEnvironmentInfo?.HardwareTimerKind);
        }
    }
}
