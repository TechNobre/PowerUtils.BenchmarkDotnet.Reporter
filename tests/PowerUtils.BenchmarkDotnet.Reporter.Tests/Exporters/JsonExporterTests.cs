using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Helpers.IOHelpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Exporters;

public sealed class JsonExporterTests
{
    private readonly FileWriter _writer;
    private string? _output;

    public JsonExporterTests()
        => _writer = (path, content) => _output = content;


    [Fact]
    public void Validate_Json_Structure()
    {
        // Arrange
        var report = new ComparerReport();
        var output = new JsonExporter(_writer);


        // Act
        output.Generate(report, "");


        // Assert
        _output.ShouldBe("""
            {
              "Warnings": [],
              "Comparisons": [],
              "HitThresholds": []
            }
            """);
    }

    [Fact]
    public void Validate_If_FileOutputJson_Is_Created()
    {
        // Arrange
        FileWriter writer = WriteFile;
        var output = new JsonExporter(writer);
        var report = new ComparerReport();
        var outputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var expectedFileName = Path.Combine(outputDirectory, "benchmark-comparison-report.json");


        // Act
        output.Generate(report, outputDirectory);


        // Assert
        File.Exists(expectedFileName).ShouldBeTrue();
    }
}
