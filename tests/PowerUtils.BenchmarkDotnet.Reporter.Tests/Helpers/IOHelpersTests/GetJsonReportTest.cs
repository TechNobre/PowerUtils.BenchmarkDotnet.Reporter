using System;
using System.IO;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers.IOHelpersTests;

public sealed class GetJsonReportTest : IDisposable
{
    private readonly string _tempDirectory;


    public GetJsonReportTest()
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
    public void When_File_Exits_Should_Return_Array_With_One_Full_Path()
    {
        // Arrange
        var path = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}{IOHelpers.REPORT_FILE_ENDS}");
        File.WriteAllText(path, "{}");


        // Act
        var act = IOHelpers.GetJsonReport(path);


        // Assert
        act.ShouldBe([path]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void When_Path_Isnt_Defined_Should_Throw_NotFoundException(string? path)
    {
        // Arrange & Act
        string[] act() => IOHelpers.GetJsonReport(path);


        // Assert
        Should.Throw<FileNotFoundException>(act)
            .Message.ShouldContain("The provided path is null or empty");
    }

    [Fact]
    public void When_File_Doesnt_Exist_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}{IOHelpers.REPORT_FILE_ENDS}");


        // Act
        string[] act() => IOHelpers.GetJsonReport(filePath);


        // Assert
        Should.Throw<FileNotFoundException>(act)
            .Message.ShouldContain($"The provided path '{filePath}' doesn't exist or is not a {IOHelpers.REPORT_FILE_ENDS} file");
    }

    [Fact]
    public void When_Directory_Doesnt_Exist_Should_Throw_FileNotFoundException()
    {
        // Arrange & Act
        string[] act() => IOHelpers.GetJsonReport(_tempDirectory);


        // Assert
        Should.Throw<FileNotFoundException>(act)
            .Message.ShouldContain($"No {IOHelpers.REPORT_FILE_ENDS} files found in the provided directory");
    }

    [Fact]
    public void When_File_Is_Invalid_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var path = Path.Combine(_tempDirectory, "nonexistent.json");


        // Act
        string[] act() => IOHelpers.GetJsonReport(path);


        // Assert
        Should.Throw<FileNotFoundException>(act)
            .Message.ShouldContain($"The provided path '{path}' doesn't exist or is not a {IOHelpers.REPORT_FILE_ENDS} file");
    }

    [Fact]
    public void When_There_Are_Multiple_JsonReport_Files_Should_Return_All_Of_FullPaths_For_Them()
    {
        // Arrange
        var filePath1 = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}{IOHelpers.REPORT_FILE_ENDS}");
        var filePath2 = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}{IOHelpers.REPORT_FILE_ENDS}");
        File.WriteAllText(filePath1, "{}");
        File.WriteAllText(filePath2, "{}");


        // Act
        var act = IOHelpers.GetJsonReport(_tempDirectory)
            .OrderBy(f => f)
            .ToArray();


        // Assert
        var expectedPaths = new[] { filePath1, filePath2 }
            .OrderBy(f => f)
            .ToArray();
        act.ShouldBe(expectedPaths);
    }

    [Fact]
    public void When_Folder_Contains_Brief_Full_Compressed_And_Uncompressed_Should_Return_All_Of_Them()
    {
        // Arrange
        var path = Path.GetFullPath(Path.Combine("test-data", "report-21"));


        // Act
        var act = IOHelpers.GetJsonReport(path);


        // Assert
        act.Length.ShouldBe(8);

        act.ShouldContain(s => Path.GetFileName(s) == "Demo.Benchmarks.ArrayProcessorBenchmarks-report-brief.json");
        act.ShouldContain(s => Path.GetFileName(s) == "Demo.Benchmarks.ArrayProcessorBenchmarks-report-brief-compressed.json");
        act.ShouldContain(s => Path.GetFileName(s) == "Demo.Benchmarks.ArrayProcessorBenchmarks-report-full.json");
        act.ShouldContain(s => Path.GetFileName(s) == "Demo.Benchmarks.ArrayProcessorBenchmarks-report-full-compressed.json");

        act.ShouldContain(s => Path.GetFileName(s) == "Demo.Benchmarks.StringProcessorBenchmarks-report-brief.json");
        act.ShouldContain(s => Path.GetFileName(s) == "Demo.Benchmarks.StringProcessorBenchmarks-report-brief-compressed.json");
        act.ShouldContain(s => Path.GetFileName(s) == "Demo.Benchmarks.StringProcessorBenchmarks-report-full.json");
        act.ShouldContain(s => Path.GetFileName(s) == "Demo.Benchmarks.StringProcessorBenchmarks-report-full-compressed.json");
    }
}
