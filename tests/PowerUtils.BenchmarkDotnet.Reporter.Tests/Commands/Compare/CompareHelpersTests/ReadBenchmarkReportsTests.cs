using System;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.CompareHelpersTests;

public sealed class ReadBenchmarkReportsTests : IDisposable
{
    private readonly string _tempDirectory;


    public ReadBenchmarkReportsTests()
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
    public void When_Json_Only_Contains_Two_Benchmark_Should_Return_Two_Record()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("test-data", "report-01", "Benchmark-report-full.json"));


        // Act
        var act = CompareHelpers.ReadBenchmarkReports(path);


        // Assert
        act.Count.ShouldBe(2);
        act.ShouldContain(b => b.FullName == "Benchmark.StringConcat");
        act.ShouldContain(b => b.FullName == "Benchmark.StringJoin");
    }

    [Fact]
    public void When_Folder_Contains_More_Than_One_Report_Should_Return_All_Benchmarks_From_All_Jsons()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("test-data", "report-11"));


        // Act
        var act = CompareHelpers.ReadBenchmarkReports(path);


        // Assert
        act.Count.ShouldBe(2);
        act.ShouldContain(b => b.FullName == "Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray");
        act.ShouldContain(b => b.FullName == "Demo.Benchmarks.StringProcessorBenchmarks.GenerateString");
    }

    [Fact]
    public void When_Folder_Contains_Brief_And_Full_Report_Should_Deduplicate_And_Return_One_Of_Each_Benchmark()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("test-data", "report-21"));


        // Act
        var act = CompareHelpers.ReadBenchmarkReports(path);


        // Assert
        act.Count.ShouldBe(2);
        act.ShouldContain(b => b.FullName == "Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray");
        act.ShouldContain(b => b.FullName == "Demo.Benchmarks.StringProcessorBenchmarks.GenerateString");
    }

    [Fact]
    public void When_Json_Doesnt_Have_Benchmarks_Property_Should_Return_Empty_List()
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
        var act = CompareHelpers.ReadBenchmarkReports(filePath);


        // Assert
        act.ShouldBeEmpty();
    }
}
