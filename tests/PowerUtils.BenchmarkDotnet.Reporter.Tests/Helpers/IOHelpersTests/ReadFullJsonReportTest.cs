using System.IO;
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
}
