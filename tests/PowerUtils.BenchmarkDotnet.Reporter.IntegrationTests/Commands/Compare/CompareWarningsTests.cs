using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Compare;

public sealed class CompareWarningsTests
{
    [Fact]
    public async Task When_Host_Environments_Differ_Should_Print_Warnings_But_Exit_With_Success_By_Default()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-04/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync("compare", "-b", baseline, "-t", target);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().Contain("WARNINGS");
        result.StandardOutput.Should().Contain("OS Version is different");
    }

    [Fact]
    public async Task When_Host_Environments_Differ_And_FailOnWarnings_Is_Set_Should_Exit_With_Warning_Code()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-04/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync("compare", "-b", baseline, "-t", target, "-fw");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.WARNING);
        result.StandardOutput.Should().Contain("WARNINGS");
    }
}
