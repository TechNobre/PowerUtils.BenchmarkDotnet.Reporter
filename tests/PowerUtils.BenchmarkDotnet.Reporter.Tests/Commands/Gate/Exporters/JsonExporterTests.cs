using System;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.IOUtils;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Gate.Exporters;

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
        var report = new GateReport();
        var output = new JsonExporter(_writer);


        // Act
        output.Generate(report, "");


        // Assert
        _output.Should().Be(string.Join(Environment.NewLine,
            "{",
            "  \"Warnings\": [],",
            "  \"Results\": [],",
            "  \"HitThresholds\": []",
            "}"));
    }

    [Fact]
    public void Validate_If_FileOutputJson_Is_Created()
    {
        // Arrange
        FileWriter writer = WriteFile;
        var output = new JsonExporter(writer);
        var report = new GateReport();
        var outputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var expectedFileName = Path.Combine(outputDirectory, "benchmark-gate-report.json");


        // Act
        output.Generate(report, outputDirectory);


        // Assert
        File.Exists(expectedFileName).Should().BeTrue();
    }
}
