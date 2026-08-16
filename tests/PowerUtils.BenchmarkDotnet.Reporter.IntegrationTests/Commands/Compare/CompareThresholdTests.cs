using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Compare;

public sealed class CompareThresholdTests
{
    [Fact]
    public async Task When_Mean_Threshold_Is_Hit_And_FailOnThresholdHit_Is_Set_Should_Exit_With_ThresholdHit_Code()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-04/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target, "-tm", "5%", "-ft");


        // Assert
        result.ExitCode.ShouldBe(3);
        result.StandardOutput.ShouldContain("THRESHOLD VIOLATIONS");
        result.StandardOutput.ShouldContain("Mean threshold hit for 'Benchmark.StringConcat'");
        result.StandardOutput.ShouldContain("Mean threshold hit for 'Benchmark.StringJoin'");
    }

    [Fact]
    public async Task When_Mean_Threshold_Is_Hit_But_FailOnThresholdHit_Is_Not_Set_Should_Exit_With_Success()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-04/Benchmark-report-full.json");

        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target, "-tm", "5%");

        // Assert
        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("THRESHOLD VIOLATIONS");
    }

    [Fact]
    public async Task When_Allocation_Threshold_Is_Hit_And_FailOnThresholdHit_Is_Set_Should_Exit_With_ThresholdHit_Code()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-04/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target, "-ta", "1kb", "-ft");


        // Assert
        result.ExitCode.ShouldBe(3);
        result.StandardOutput.ShouldContain("THRESHOLD VIOLATIONS");
    }

    [Fact]
    public async Task When_Mean_And_Allocation_Diffs_Do_Not_Exceed_Threshold_Should_Exit_With_Success()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-02/Benchmark-report-full.json");

        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target, "-tm", "10%", "-ta", "1kb", "-ft");


        // Assert
        result.ExitCode.ShouldBe(0);
    }
}
