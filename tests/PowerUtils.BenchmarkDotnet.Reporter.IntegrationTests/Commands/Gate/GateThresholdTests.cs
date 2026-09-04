using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Gate;

public sealed class GateThresholdTests
{
    [Fact]
    public async Task When_Mean_Threshold_Is_Hit_Should_Exit_With_ThresholdHit_Code()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync("gate", "-i", input, "-tm", "1ns");


        // Assert
        // No -ft flag exists for gate - a threshold hit always fails the run.
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("THRESHOLD VIOLATIONS");
        result.StandardOutput.Should().Contain("Mean threshold hit for 'Benchmark.StringConcat'");
        result.StandardOutput.Should().Contain("Mean threshold hit for 'Benchmark.StringJoin'");
    }

    [Fact]
    public async Task When_Allocation_Threshold_Is_Hit_Should_Exit_With_ThresholdHit_Code()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync("gate", "-i", input, "-ta", "1b");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("THRESHOLD VIOLATIONS");
        result.StandardOutput.Should().Contain("Allocation threshold hit for 'Benchmark.StringConcat'");
        result.StandardOutput.Should().Contain("Allocation threshold hit for 'Benchmark.StringJoin'");
    }

    [Fact]
    public async Task When_Mean_And_Allocation_Are_Under_Threshold_Should_Exit_With_Success()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "gate", "-i", input, "-tm", "500ms", "-ta", "10kb");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
    }

    [Fact]
    public async Task When_Threshold_Value_Uses_Percentage_Unit_Should_Exit_With_Error_Code()
    {
        // Arrange
        // Gate checks absolute values only - '%' isn't a recognized unit since there's no baseline to diff against.
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync("gate", "-i", input, "-tm", "5%");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.ERROR);
        result.StandardError.Should().Contain("not a valid threshold");
    }
}
