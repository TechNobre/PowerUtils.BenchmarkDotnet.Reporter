using System.IO;
using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Gate;

public sealed class GateFormatsTests
{
    [Fact]
    public async Task When_Markdown_Format_Is_Requested_Should_Write_Markdown_Report_File()
    {
        // Arrange
        using var output = new TempOutputDirectory();
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "gate", "-i", input, "-f", "markdown", "-o", output.Path);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);

        var reportPath = output.CombinePath("benchmark-gate-report.md");
        File.Exists(reportPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(reportPath, TestContext.Current.CancellationToken);
        content.Should().Contain("# BENCHMARK GATE REPORT");
        content.Should().Contain("StringConcat");
    }

    [Fact]
    public async Task When_Json_Format_Is_Requested_Should_Write_Json_Report_File()
    {
        // Arrange
        using var output = new TempOutputDirectory();
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "gate", "-i", input, "-f", "json", "-o", output.Path);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);

        var reportPath = output.CombinePath("benchmark-gate-report.json");
        File.Exists(reportPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(reportPath, TestContext.Current.CancellationToken);
        content.Should().Contain("\"Results\"");
        content.Should().Contain("StringConcat");
    }

    [Fact]
    public async Task When_HitTxt_Format_Is_Requested_And_Threshold_Is_Hit_Should_Write_Hits_File()
    {
        // Arrange
        using var output = new TempOutputDirectory();
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "gate", "-i", input, "-tm", "1ns", "-f", "hit-txt", "-o", output.Path);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.THRESHOLD_HIT);

        var hitsPath = output.CombinePath("benchmark-gate-hits.txt");
        File.Exists(hitsPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(hitsPath, TestContext.Current.CancellationToken);
        content.Should().Contain("Mean threshold hit for 'Benchmark.StringConcat'");
    }

    [Fact]
    public async Task When_HitTxt_Format_Is_Requested_And_Nothing_Is_Hit_Should_Not_Write_File()
    {
        // Arrange
        using var output = new TempOutputDirectory();
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "gate", "-i", input, "-tm", "500ms", "-f", "hit-txt", "-o", output.Path);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);

        var hitsPath = output.CombinePath("benchmark-gate-hits.txt");
        File.Exists(hitsPath).Should().BeFalse();
    }

    [Fact]
    public async Task When_Multiple_Formats_Are_Requested_Should_Write_All_Corresponding_Files()
    {
        // Arrange
        using var output = new TempOutputDirectory();
        var input = TestDataPath.Resolve("report-01/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "gate", "-i", input,
            "-f", "markdown", "-f", "json", "-o", output.Path);


        // Assert
        result.ExitCode.Should().Be(Constants.ExitCodes.SUCCESS);
        File.Exists(output.CombinePath("benchmark-gate-report.md")).Should().BeTrue();
        File.Exists(output.CombinePath("benchmark-gate-report.json")).Should().BeTrue();
    }
}
