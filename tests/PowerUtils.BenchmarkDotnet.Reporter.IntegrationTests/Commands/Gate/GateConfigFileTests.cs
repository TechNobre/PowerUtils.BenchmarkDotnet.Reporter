using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Gate;

public sealed class GateConfigFileTests
{
    [Fact]
    public async Task When_ConfigOption_PointsTo_ValidYamlFile_Should_ApplyScopedThreshold()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-10");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            """
            gate:
              thresholds:
                - pattern: "Demo.Benchmarks.ArrayProcessorBenchmarks.*"
                  thresholdMean: 10us
            """,
            TestContext.Current.CancellationToken);

        // Act
        var result = await ProcessRunner.RunAsync([
            "gate",
            "-i", input,
            "--config", configPath]);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Mean threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray' (rule: Demo.Benchmarks.ArrayProcessorBenchmarks.*)");
        result.StandardOutput.Should().NotContain("Mean threshold hit for 'Demo.Benchmarks.StringProcessorBenchmarks.GenerateString'");
    }

    [Fact]
    public async Task When_ConfigOption_PointsTo_MissingFile_Should_ExitWithFailure()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var missingPath = Path.Combine(Path.GetTempPath(), "pbreporter-does-not-exist.yml");


        // Act
        var result = await ProcessRunner.RunAsync([
            "gate",
            "-i", input,
            "--config", missingPath]);


        // Assert
        result.ExitCode.Should().NotBe(Constants.ExitCodes.SUCCESS);
        result.StandardError.Should().Contain(missingPath);
    }

    [Fact]
    public async Task When_DefaultConfigFile_ExistsInWorkingDirectory_Should_BeUsed_WithoutConfigOption()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-10");
        using var scratch = new TempOutputDirectory();
        await File.WriteAllTextAsync(
            scratch.CombinePath("pbreporter.yml"),
            """
            gate:
              thresholds:
                - thresholdMean: 10us
            """,
            TestContext.Current.CancellationToken);


        // Act
        var result = await ProcessRunner.RunAsync(
            ["gate", "-i", input],
            workingDirectory: scratch.Path);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Mean threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray'");
    }

    [Fact]
    public async Task When_EnvironmentVariable_OverridesConfigFile_Should_UseEnvironmentValue()
    {
        // Arrange
        // File sets a loose 50ms global mean threshold; the env var tightens it to 10us.
        var input = TestDataPath.Resolve("report-10");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            """
            gate:
              thresholds:
                - thresholdMean: 50ms
            """,
            TestContext.Current.CancellationToken);

        var environmentVariables = new Dictionary<string, string?>
        {
            ["PBREPORTER_GATE__THRESHOLD_MEAN"] = "10us"
        };


        // Act
        var result = await ProcessRunner.RunAsync([
            "gate",
            "-i", input,
            "--config", configPath],
            environmentVariables: environmentVariables);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Mean threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray'");
    }

    [Fact]
    public async Task When_CliArgument_OverridesConfigFileAndEnvironmentVariable_Should_UseCliValue()
    {
        // Arrange
        // File sets 10us (would hit), env var loosens it to 50ms (would not hit), CLI tightens it back to 10us (would hit).
        var input = TestDataPath.Resolve("report-10");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            """
            gate:
              thresholds:
                - thresholdMean: 10us
            """,
            TestContext.Current.CancellationToken);

        var environmentVariables = new Dictionary<string, string?>
        {
            ["PBREPORTER_GATE__THRESHOLD_MEAN"] = "50ms"
        };


        // Act
        var result = await ProcessRunner.RunAsync([
            "gate",
            "-i", input,
            "--config", configPath,
            "-tm", "10us"],
            environmentVariables: environmentVariables);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);
        result.StandardOutput.Should().Contain("Mean threshold hit for 'Demo.Benchmarks.ArrayProcessorBenchmarks.GenerateArray'");
    }

    [Fact]
    public async Task When_ConfigOption_Supplies_Input_Should_RunSuccessfully_WithoutCliPath()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            $"""
            gate:
              input: "{input.Replace("\\", "/")}"
            """,
            TestContext.Current.CancellationToken);


        // Act
        var result = await ProcessRunner.RunAsync(["gate", "--config", configPath]);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().Contain("StringConcat");
    }

    [Fact]
    public async Task When_EnvironmentVariable_Supplies_Input_Should_RunSuccessfully_WithoutCliPath()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var environmentVariables = new Dictionary<string, string?>
        {
            ["PBREPORTER_GATE__INPUT"] = input
        };


        // Act
        var result = await ProcessRunner.RunAsync(["gate"], environmentVariables: environmentVariables);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().Contain("StringConcat");
    }

    [Fact]
    public async Task When_CliInput_OverridesConfigInput_Should_UseCliValue()
    {
        // Arrange
        // The config file points at a folder with no matching benchmarks (report-10); the CLI
        // input points at report-01 instead, and must be the one actually used.
        var wrongPath = TestDataPath.Resolve("report-10");
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            $"""
            gate:
              input: "{wrongPath.Replace("\\", "/")}"
            """,
            TestContext.Current.CancellationToken);


        // Act
        var result = await ProcessRunner.RunAsync(
            ["gate", "-i", input, "--config", configPath]);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        result.StandardOutput.Should().Contain("StringConcat");
    }

    [Fact]
    public async Task When_ConfigFile_Has_InvalidFormat_Should_ExitWithError()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            """
            gate:
              formats: [csv]
            """,
            TestContext.Current.CancellationToken);


        // Act
        var result = await ProcessRunner.RunAsync(["gate", "-i", input, "--config", configPath]);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.ERROR);
        result.StandardError.Should().Contain("Invalid format 'csv'. Allowed values: console, markdown, json, hit-txt");
    }

    [Fact]
    public async Task When_EnvironmentVariable_Has_InvalidFormat_Should_ExitWithError()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var environmentVariables = new Dictionary<string, string?>
        {
            ["PBREPORTER_GATE__FORMATS"] = "csv"
        };


        // Act
        var result = await ProcessRunner.RunAsync(["gate", "-i", input], environmentVariables: environmentVariables);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.ERROR);
        result.StandardError.Should().Contain("Invalid format 'csv'. Allowed values: console, markdown, json, hit-txt");
    }

    [Fact]
    public async Task When_ConfigFile_Has_InvalidScopedThresholdPattern_Should_ExitWithError()
    {
        // Arrange
        var input = TestDataPath.Resolve("report-10");
        using var scratch = new TempOutputDirectory();
        var configPath = scratch.CombinePath("pbreporter.yml");
        await File.WriteAllTextAsync(
            configPath,
            """
            gate:
              thresholds:
                - pattern: "Demo.*.ArrayProcessorBenchmarks"
                  thresholdMean: 10us
            """,
            TestContext.Current.CancellationToken);


        // Act
        var result = await ProcessRunner.RunAsync(["gate", "-i", input, "--config", configPath]);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.ERROR);
        result.StandardError.Should().Contain("Invalid threshold pattern 'Demo.*.ArrayProcessorBenchmarks'. A '*' is only allowed as the last character of the pattern.");
    }
}
