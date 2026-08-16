using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Compare;

public sealed class CompareSuccessTests
{
    [Fact]
    public async Task When_Comparing_Reports_With_Minor_Differences_Should_Exit_With_Success()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-02/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync("compare", "-b", baseline, "-t", target);


        // Assert
        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("BENCHMARK COMPARISON REPORT");
        result.StandardOutput.ShouldContain("StringConcat");
        result.StandardOutput.ShouldContain("StringJoin");
        result.StandardOutput.ShouldContain("-2.77%");
        result.StandardOutput.ShouldContain("-4.98%");
    }

    [Fact]
    public async Task When_Comparing_Folder_Inputs_With_Multiple_Benchmark_Classes_Should_Merge_Results()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-10");
        var target = TestDataPath.Resolve("report-11");


        // Act
        var result = await ProcessRunner.RunAsync("compare", "-b", baseline, "-t", target);


        // Assert
        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("GenerateArray");
        result.StandardOutput.ShouldContain("GenerateString");
    }

    [Fact]
    public async Task When_Comparing_Equivalent_Reports_With_Different_File_Names_Should_Exit_With_Success()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-05/Baseline-report-full.json");
        var target = TestDataPath.Resolve("report-05/Target-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync("compare", "-b", baseline, "-t", target);


        // Assert
        result.ExitCode.ShouldBe(0);
    }
}
