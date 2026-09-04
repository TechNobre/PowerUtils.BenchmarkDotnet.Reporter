using System.IO;
using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Gate;

public sealed class GateWarningsTests
{
    // None of the checked-in test-data fixtures were built in DEBUG, so this class writes its own
    // minimal report inline to exercise the RELEASE-mode warning.
    private static async Task<string> _writeDebugReportAsync(TempOutputDirectory scratch)
    {
        var path = scratch.CombinePath("debug-report.json");
        await File.WriteAllTextAsync(path,
            """
            {
                "Title": "Debug-report",
                "HostEnvironmentInfo": {
                    "Configuration": "DEBUG"
                },
                "Benchmarks": [
                    {
                        "Type": "Benchmark",
                        "Method": "SomeMethod",
                        "FullName": "Benchmark.SomeMethod",
                        "Statistics": { "Mean": 15.5 },
                        "Memory": { "BytesAllocatedPerOperation": 48 }
                    }
                ]
            }
            """,
            TestContext.Current.CancellationToken);
        return path;
    }


    [Fact]
    public async Task When_Report_Was_Not_Built_In_Release_Should_Print_Warning_But_Exit_With_Success_By_Default()
    {
        // Arrange
        using var scratch = new TempOutputDirectory();
        var input = await _writeDebugReportAsync(scratch);


        // Act
        var result = await ProcessRunner.RunAsync("gate", "-i", input);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().Contain("WARNINGS");
        result.StandardOutput.Should().Contain("wasn't executed in RELEASE mode");
    }

    [Fact]
    public async Task When_Report_Was_Not_Built_In_Release_And_FailOnWarnings_Is_Set_Should_Exit_With_Warning_Code()
    {
        // Arrange
        using var scratch = new TempOutputDirectory();
        var input = await _writeDebugReportAsync(scratch);


        // Act
        var result = await ProcessRunner.RunAsync("gate", "-i", input, "-fw");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.WARNING);
        result.StandardOutput.Should().Contain("WARNINGS");
    }

    [Fact]
    public async Task When_Report_Was_Built_In_Release_Shouldnt_Print_Warning()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync("gate", "-i", input, "-fw");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().NotContain("WARNINGS");
    }
}
