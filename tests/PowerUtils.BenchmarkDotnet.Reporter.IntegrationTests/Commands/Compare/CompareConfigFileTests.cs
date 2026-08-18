using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Compare;

public sealed class CompareConfigFileTests
{
    [Fact]
    public async Task When_ConfigOption_PointsTo_ValidYamlFile_Should_ApplyScopedThreshold()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-10");
        var target = TestDataPath.Resolve("report-11");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            """
            compare:
              thresholds:
                - thresholdAllocation: 50%
                - pattern: "Demo.Benchmarks.StringProcessorBenchmarks.*"
                  thresholdAllocation: 5%
            """,
            TestContext.Current.CancellationToken);

        // Act
        var result = await ProcessRunner.RunAsync([
            "compare",
            "-b", baseline,
            "-t", target,
            "--config", configPath,
            "-ft"]);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Allocation threshold hit for 'Demo.Benchmarks.StringProcessorBenchmarks.GenerateString' (rule: Demo.Benchmarks.StringProcessorBenchmarks.*)");
        result.StandardOutput.Should().NotContain("Allocation threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray'");
    }

    [Fact]
    public async Task When_ConfigOption_PointsTo_MissingFile_Should_ExitWithFailure()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-02/Benchmark-report-full.json");
        var missingPath = Path.Combine(Path.GetTempPath(), "pbreporter-does-not-exist.yml");


        // Act
        var result = await ProcessRunner.RunAsync([
            "compare",
            "-b", baseline,
            "-t", target,
            "--config", missingPath]);


        // Assert
        result.ExitCode.Should().NotBe(Constants.ExitCodes.SUCCESS);
        result.StandardError.Should().Contain(missingPath);
    }

    [Fact]
    public async Task When_DefaultConfigFile_ExistsInWorkingDirectory_Should_BeUsed_WithoutConfigOption()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-10");
        var target = TestDataPath.Resolve("report-11");
        using var scratch = new TempOutputDirectory();
        await File.WriteAllTextAsync(
            scratch.CombinePath("pbreporter.yml"),
            """
            compare:
              thresholds:
                - thresholdAllocation: 5%
            """,
            TestContext.Current.CancellationToken);


        // Act
        var result = await ProcessRunner.RunAsync([
            "compare",
            "-b", baseline,
            "-t", target,
            "-ft"],
            workingDirectory: scratch.Path);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Allocation threshold hit for 'Demo.Benchmarks.StringProcessorBenchmarks.GenerateString'");
    }

    [Fact]
    public async Task When_EnvironmentVariable_OverridesConfigFile_Should_UseEnvironmentValue()
    {
        // Arrange
        // File sets a loose 50% global allocation threshold; the env var tightens it to 5%.
        var baseline = TestDataPath.Resolve("report-10");
        var target = TestDataPath.Resolve("report-11");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            """
            compare:
              thresholds:
                - thresholdAllocation: 50%
            """,
            TestContext.Current.CancellationToken);

        var environmentVariables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__THRESHOLD_ALLOCATION"] = "5%"
        };


        // Act
        var result = await ProcessRunner.RunAsync([
            "compare",
            "-b", baseline,
            "-t", target,
            "--config", configPath,
            "-ft"],
            environmentVariables: environmentVariables);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Allocation threshold hit for 'Demo.Benchmarks.StringProcessorBenchmarks.GenerateString'");
    }

    [Fact]
    public async Task When_CliArgument_OverridesConfigFileAndEnvironmentVariable_Should_UseCliValue()
    {
        // Arrange
        // File sets 5% (would hit), env var loosens it to 50% (would not hit), CLI tightens it back to 5% (would hit).
        var baseline = TestDataPath.Resolve("report-10");
        var target = TestDataPath.Resolve("report-11");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            """
            compare:
              thresholds:
                - thresholdAllocation: 5%
            """,
            TestContext.Current.CancellationToken);

        var environmentVariables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__THRESHOLD_ALLOCATION"] = "50%"
        };


        // Act
        var result = await ProcessRunner.RunAsync([
            "compare",
            "-b", baseline,
            "-t", target,
            "--config", configPath,
            "-ta", "5%",
            "-ft"],
            environmentVariables: environmentVariables);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Allocation threshold hit for 'Demo.Benchmarks.StringProcessorBenchmarks.GenerateString'");
    }

    [Fact]
    public async Task When_ConfigOption_Supplies_BaselineAndTarget_Should_RunSuccessfully_WithoutCliPaths()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-02/Benchmark-report-full.json");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            $"""
            compare:
              baseline: "{baseline.Replace("\\", "/")}"
              target: "{target.Replace("\\", "/")}"
            """,
            TestContext.Current.CancellationToken);


        // Act
        var result = await ProcessRunner.RunAsync(["compare", "--config", configPath]);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().Contain("StringConcat");
    }

    [Fact]
    public async Task When_EnvironmentVariables_Supply_BaselineAndTarget_Should_RunSuccessfully_WithoutCliPaths()
    {
        // Arrange
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-02/Benchmark-report-full.json");
        var environmentVariables = new Dictionary<string, string?>
        {
            ["PBREPORTER_COMPARE__BASELINE"] = baseline,
            ["PBREPORTER_COMPARE__TARGET"] = target
        };


        // Act
        var result = await ProcessRunner.RunAsync(["compare"], environmentVariables: environmentVariables);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().Contain("StringConcat");
    }

    [Fact]
    public async Task When_CliBaseline_OverridesConfigBaseline_Should_UseCliValue()
    {
        // Arrange
        // The config file points at a folder with no matching benchmarks (report-10); the CLI
        // baseline/target point at report-01/report-02 instead, and must be the ones actually used.
        var wrongPath = TestDataPath.Resolve("report-10");
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-02/Benchmark-report-full.json");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            $"""
            compare:
              baseline: "{wrongPath.Replace("\\", "/")}"
              target: "{wrongPath.Replace("\\", "/")}"
            """,
            TestContext.Current.CancellationToken);


        // Act
        var result = await ProcessRunner.RunAsync(
            ["compare", "-b", baseline, "-t", target, "--config", configPath]);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().Contain("StringConcat");
    }
}
