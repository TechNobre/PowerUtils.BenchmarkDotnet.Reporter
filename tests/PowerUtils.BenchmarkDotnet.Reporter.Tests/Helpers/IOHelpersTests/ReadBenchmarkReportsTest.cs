using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers.IOHelpersTests;

public sealed class ReadBenchmarkReportsTest
{
    [Fact]
    public void When_Json_Only_Contains_Two_Benchmark_Should_Return_Two_Record()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("test-data", "report-01", "Benchmark-report-full.json"));


        // Act
        var act = IOHelpers.ReadBenchmarkReports(path);


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
        var act = IOHelpers.ReadBenchmarkReports(path);


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
        var act = IOHelpers.ReadBenchmarkReports(path);


        // Assert
        act.Count.ShouldBe(2);
        act.ShouldContain(b => b.FullName == "Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray");
        act.ShouldContain(b => b.FullName == "Demo.Benchmarks.StringProcessorBenchmarks.GenerateString");
    }
}
