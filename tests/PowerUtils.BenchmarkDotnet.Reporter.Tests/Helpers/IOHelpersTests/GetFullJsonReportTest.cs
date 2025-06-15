using System;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers.IOHelpersTests;

public sealed class GetFullJsonReportTest : IDisposable
{
    private readonly string _tempDirectory;


    public GetFullJsonReportTest()
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
        var act = IOHelpers.GetFullJsonReport(path);


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
        string[] act() => IOHelpers.GetFullJsonReport(path);


        // Assert
        Should.Throw<FileNotFoundException>(act)
            .Message.ShouldContain("The provided path is null or empty.");
    }

    [Fact]
    public void When_File_Doesnt_Exist_Should_Throw_FileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}{IOHelpers.REPORT_FILE_ENDS}");


        // Act
        string[] act() => IOHelpers.GetFullJsonReport(filePath);


        // Assert
        Should.Throw<FileNotFoundException>(act)
            .Message.ShouldContain($"The provided path '{filePath}' doesn't exist or is not a {IOHelpers.REPORT_FILE_ENDS} file");
    }

    [Fact]
    public void When_Directory_Doesnt_Exist_Should_Throw_FileNotFoundException()
    {
        // Arrange & Act
        string[] act() => IOHelpers.GetFullJsonReport(_tempDirectory);


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
        string[] act() => IOHelpers.GetFullJsonReport(path);


        // Assert
        Should.Throw<FileNotFoundException>(act)
            .Message.ShouldContain($"The provided path '{path}' doesn't exist or is not a {IOHelpers.REPORT_FILE_ENDS} file");
    }

    [Fact]
    public void When_There_Are_Multiple_FullJsonReport_Files_Should_Return_All_Of_FullPaths_For_Them()
    {
        // Arrange
        var filePath1 = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}{IOHelpers.REPORT_FILE_ENDS}");
        var filePath2 = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}{IOHelpers.REPORT_FILE_ENDS}");
        File.WriteAllText(filePath1, "{}");
        File.WriteAllText(filePath2, "{}");


        // Act
        var act = IOHelpers.GetFullJsonReport(_tempDirectory);


        // Assert
        string[] expected = [filePath1, filePath2];
        Array.Sort(expected);
        act.ShouldBe(expected);
    }
}
