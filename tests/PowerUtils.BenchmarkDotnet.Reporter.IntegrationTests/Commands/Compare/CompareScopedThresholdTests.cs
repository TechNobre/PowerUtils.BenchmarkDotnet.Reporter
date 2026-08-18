using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Compare;

public sealed class CompareScopedThresholdTests
{
    [Fact]
    public async Task When_Scoped_Allocation_Threshold_Matches_One_Class_Should_Only_Hit_That_Class()
    {
        // Arrange
        // report-10 -> report-11: StringProcessorBenchmarks.GenerateString allocation diff is +9.89%,
        // ArrayProcessorBenchmarks.GenerateArray allocation diff is only +0.08%.
        var baseline = TestDataPath.Resolve("report-10");
        var target = TestDataPath.Resolve("report-11");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target,
            "-ta", "Demo.Benchmarks.StringProcessorBenchmarks.*=5%", "-ft");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Allocation threshold hit for 'Demo.Benchmarks.StringProcessorBenchmarks.GenerateString' (rule: Demo.Benchmarks.StringProcessorBenchmarks.*)");
        result.StandardOutput.Should().NotContain("Allocation threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray'");
    }

    [Fact]
    public async Task When_Scoped_Rule_Overrides_Global_Threshold_With_Bigger_Value_Should_Not_Hit_That_Class()
    {
        // Arrange
        // Global 5% allocation threshold would hit both classes; scoping ArrayProcessorBenchmarks to a loose
        // 50% threshold should keep it from hitting, while StringProcessorBenchmarks still hits via the global.
        var baseline = TestDataPath.Resolve("report-10");
        var target = TestDataPath.Resolve("report-11");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target,
            "-ta", "5%", "-ta", "Demo.Benchmarks.ArrayProcessorBenchmarks.*=50%", "-ft");


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Allocation threshold hit for 'Demo.Benchmarks.StringProcessorBenchmarks.GenerateString'");
        result.StandardOutput.Should().NotContain("Allocation threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray'");
    }

    [Fact]
    public async Task When_Scoped_Rule_Overrides_Global_Threshold_With_Smaller_Value_Should_Not_Hit_That_Class()
    {
        // Arrange
        // Global 5% allocation threshold would hit both classes; scoping ArrayProcessorBenchmarks to a tight
        // 1% threshold should cause it to hit, while StringProcessorBenchmarks still hits via the global.
        var baseline = TestDataPath.Resolve("report-10");
        var target = TestDataPath.Resolve("report-11");

        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target,
            "-ta", "50%", "-ta", "Demo.Benchmarks.ArrayProcessorBenchmarks.*=1%", "-ft");

        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().NotContain("Allocation threshold hit for");
    }
}
