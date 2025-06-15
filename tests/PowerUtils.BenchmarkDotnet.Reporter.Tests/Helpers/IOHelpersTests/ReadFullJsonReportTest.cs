using System.IO;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Helpers.IOHelpersTests;

public sealed class ReadFullJsonReportTest
{
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
}
