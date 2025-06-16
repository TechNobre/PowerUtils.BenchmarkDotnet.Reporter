using System;
using System.IO;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;
using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers.IOHelpersTests;

public sealed class ReadFullJsonReportTest : IDisposable
{
    private readonly string _tempDirectory;


    public ReadFullJsonReportTest()
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

    [Fact]
    public void When_Pass_Valid_File_Should_Return_Report()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("test-data", "report-01", "Benchmark-report-full.json"));


        // Act
        var act = IOHelpers.ReadFullJsonReport(path);


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
        var act = IOHelpers.ReadFullJsonReport(path).Single();


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
        BenchmarkFullJsonResport[] act() => IOHelpers.ReadFullJsonReport(filePath);


        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldStartWith($"Failed to deserialize the file '{filePath}'. ");
    }
}
