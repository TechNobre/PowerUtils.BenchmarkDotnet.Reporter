using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Gate;

public sealed class GateSuccessTests
{
    [Fact]
    public async Task When_Report_Stays_Under_Thresholds_Should_Exit_With_Success()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "gate", "-i", input, "-tm", "500ms", "-ta", "10kb");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().Contain("BENCHMARK GATE REPORT");
        result.StandardOutput.Should().Contain("StringConcat");
        result.StandardOutput.Should().Contain("StringJoin");
    }

    [Fact]
    public async Task When_No_Thresholds_Are_Configured_Should_Exit_With_Success()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync("gate", "-i", input);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
    }

    [Fact]
    public async Task When_Input_Is_A_Folder_With_Multiple_Benchmark_Classes_Should_Check_All_Of_Them()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-10");


        // Act
        var result = await ProcessRunner.RunAsync(
            "gate", "-i", input, "-tm", "500ms", "-ta", "50mb");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().Contain("GenerateArray");
        result.StandardOutput.Should().Contain("GenerateString");
    }
}
