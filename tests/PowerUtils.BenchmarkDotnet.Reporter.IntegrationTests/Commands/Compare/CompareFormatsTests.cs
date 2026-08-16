using System.IO;
using System.Threading.Tasks;
using PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Helpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.IntegrationTests.Commands.Compare;

public sealed class CompareFormatsTests
{
    [Fact]
    public async Task When_Markdown_Format_Is_Requested_Should_Write_Markdown_Report_File()
    {
        // Arrange
        using var output = new TempOutputDirectory();
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-02/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target, "-f", "markdown", "-o", output.Path);


        // Assert
        result.ExitCode.Should().Be(0);

        var reportPath = output.CombinePath("benchmark-comparison-report.md");
        File.Exists(reportPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(reportPath, TestContext.Current.CancellationToken);
        content.Should().Contain("# BENCHMARK COMPARISON REPORT");
        content.Should().Contain("StringConcat");
    }

    [Fact]
    public async Task When_Json_Format_Is_Requested_Should_Write_Json_Report_File()
    {
        // Arrange
        using var output = new TempOutputDirectory();
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-02/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target, "-f", "json", "-o", output.Path);


        // Assert
        result.ExitCode.Should().Be(0);

        var reportPath = output.CombinePath("benchmark-comparison-report.json");
        File.Exists(reportPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(reportPath, TestContext.Current.CancellationToken);
        content.Should().Contain("\"Comparisons\"");
        content.Should().Contain("StringConcat");
    }

    [Fact]
    public async Task When_HitTxt_Format_Is_Requested_And_Threshold_Is_Hit_Should_Write_Hits_File()
    {
        // Arrange
        using var output = new TempOutputDirectory();
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-04/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target, "-tm", "5%", "-f", "hit-txt", "-o", output.Path);


        // Assert
        result.ExitCode.Should().Be(0);

        var hitsPath = output.CombinePath("benchmark-comparison-hits.txt");
        File.Exists(hitsPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(hitsPath, TestContext.Current.CancellationToken);
        content.Should().Contain("Mean threshold hit for 'Benchmark.StringConcat'");
    }

    [Fact]
    public async Task When_HitTxt_Format_Is_Requested_And_Nothing_Is_Hit_Should_Not_Write_File()
    {
        // Arrange
        using var output = new TempOutputDirectory();
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-02/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target, "-f", "hit-txt", "-o", output.Path);


        // Assert
        result.ExitCode.Should().Be(0);

        var hitsPath = output.CombinePath("benchmark-comparison-hits.txt");
        File.Exists(hitsPath).Should().BeFalse();
    }

    [Fact]
    public async Task When_Multiple_Formats_Are_Requested_Should_Write_All_Corresponding_Files()
    {
        // Arrange
        using var output = new TempOutputDirectory();
        var baseline = TestDataPath.Resolve("report-01/Benchmark-report-full.json");
        var target = TestDataPath.Resolve("report-02/Benchmark-report-full.json");


        // Act
        var result = await ProcessRunner.RunAsync(
            "compare", "-b", baseline, "-t", target,
            "-f", "markdown", "-f", "json", "-o", output.Path);


        // Assert
        result.ExitCode.Should().Be(0);
        File.Exists(output.CombinePath("benchmark-comparison-report.md")).Should().BeTrue();
        File.Exists(output.CombinePath("benchmark-comparison-report.json")).Should().BeTrue();
    }
}
