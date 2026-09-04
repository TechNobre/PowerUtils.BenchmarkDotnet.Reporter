using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Gate;

public sealed class GateScopedThresholdTests
{
    // report-10: ArrayProcessorBenchmarks.GenerateArray Mean ~17951.8ns; StringProcessorBenchmarks.GenerateString Mean ~1457234.6ns.

    [Fact]
    public async Task When_Only_A_Scoped_Rule_Is_Configured_Should_Only_Check_The_Matching_Class()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-10");


        // Act
        var result = await ProcessRunner.RunAsync(
            "gate", "-i", input,
            "-tm", "Demo.Benchmarks.ArrayProcessorBenchmarks.*=10us");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Mean threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray' (rule: Demo.Benchmarks.ArrayProcessorBenchmarks.*)");
        result.StandardOutput.Should().NotContain("Mean threshold hit for 'Demo.Benchmarks.StringProcessorBenchmarks.GenerateString'");
    }

    [Fact]
    public async Task When_Scoped_Rule_Overrides_Global_Threshold_With_Bigger_Value_Should_Not_Hit_That_Class()
    {
        // Arrange
        // Global 10us would hit both classes; scoping ArrayProcessorBenchmarks to a loose 50us keeps it from hitting.
        var input = TestDataPath.Resolve("report-10");


        // Act
        var result = await ProcessRunner.RunAsync(
            "gate", "-i", input,
            "-tm", "10us", "-tm", "Demo.Benchmarks.ArrayProcessorBenchmarks.*=50us");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Mean threshold hit for 'Demo.Benchmarks.StringProcessorBenchmarks.GenerateString'");
        result.StandardOutput.Should().NotContain("Mean threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray'");
    }

    [Fact]
    public async Task When_Scoped_Rule_Overrides_Global_Threshold_With_Smaller_Value_Should_Hit_That_Class()
    {
        // Arrange
        // Global 50ms is loose enough that neither class would hit; scoping ArrayProcessorBenchmarks to a
        // tight 1us causes it to hit, while StringProcessorBenchmarks stays under the loose global.
        var input = TestDataPath.Resolve("report-10");


        // Act
        var result = await ProcessRunner.RunAsync(
            "gate", "-i", input,
            "-tm", "50ms", "-tm", "Demo.Benchmarks.ArrayProcessorBenchmarks.*=1us");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Mean threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray' (rule: Demo.Benchmarks.ArrayProcessorBenchmarks.*)");
        result.StandardOutput.Should().NotContain("Mean threshold hit for 'Demo.Benchmarks.StringProcessorBenchmarks.GenerateString'");
    }
}
