using System;
using System.IO;
using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Compare;

public sealed class CompareErrorTests
{
    [Fact]
    public async Task When_Baseline_Path_Does_Not_Exist_Should_Exit_With_Error_Code()
    {
        // Arrange
        var target = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", "non-existent-baseline.json", "-t", target);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.ERROR);
        result.StandardError.Should().Contain("doesn't exist or is not a .json file");
    }

    [Fact]
    public async Task When_Target_Path_Does_Not_Exist_Should_Exit_With_Error_Code()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", "non-existent-target.json");

        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.ERROR);
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
        result.ExitCode.Should().NotBe(Constants.ExitCodes.SUCCESS);
    }

    [Fact]
    public async Task When_Required_Options_Are_Missing_Should_Exit_With_Error_Code()
    {
        // Arrange & Act
        var result = await ProcessRunner.RunAsync("compare");

        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.ERROR);
        result.StandardError.Should().Contain("baseline");
        result.StandardError.Should().Contain("required");
    }

    [Fact]
    public async Task When_Config_File_Has_Unknown_Key_Should_Exit_With_Error_Code_And_Show_Message()
    {
        // Arrange — write a config file with 'format' (old key, now 'formats')
        var configPath = Path.Combine(Path.GetTempPath(), $"pbreporter-test-{Guid.NewGuid():N}.yml");
        await File.WriteAllTextAsync(configPath,
            """
            compare:
              format: json
            """,
            TestContext.Current.CancellationToken);

        try
        {
            // Act
            var result = await ProcessRunner.RunAsync("compare", "--config", configPath);

            // Assert
            result.ExitCode.Should().Be(Constants.ExitCodes.ERROR);
            result.StandardError.Should().Contain("Error:");
            result.StandardError.Should().Contain("Unknown key 'format'");
        }
        finally
        {
            File.Delete(configPath);
        }
    }
}
