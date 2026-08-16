using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Compare;

public sealed class CompareErrorTests
{
    [Fact]
    public async Task When_Baseline_Path_Does_Not_Exist_Should_Exit_With_Non_Zero_Code()
    {
        // Arrange
        var target = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", "non-existent-baseline.json", "-t", target);


        // Assert
        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain("doesn't exist or is not a .json file");
    }

    [Fact]
    public async Task When_Target_Path_Does_Not_Exist_Should_Exit_With_Non_Zero_Code()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", "non-existent-target.json");

        // Assert
        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain("doesn't exist or is not a .json file");
    }

    [Fact]
    public async Task When_Format_Is_Invalid_Should_Exit_With_Non_Zero_Code()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-02/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target, "-f", "not-a-real-format");


        // Assert
        result.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public async Task When_Required_Options_Are_Missing_Should_Exit_With_Non_Zero_Code()
    {
        // Arrange & Act
        var result = await ProcessRunner.RunAsync("compare");

        // Assert
        result.ExitCode.Should().NotBe(0);
    }
}
