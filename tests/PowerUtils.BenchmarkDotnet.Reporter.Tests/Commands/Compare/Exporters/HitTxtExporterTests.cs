using System;
using System.Collections.Generic;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.IOHelpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Commands.Compare.Exporters;

public sealed class HitTxtExporterTests
{
    private readonly HitTxtExporter _exporter;
    private List<string> _output = [];

    public HitTxtExporterTests()
    {
        void writer(string path, string content)
            => _output = [.. content.Split(Environment.NewLine)];
        _exporter = new HitTxtExporter(writer);
    }


    [Fact]
    public void When_Doesnt_Have_Hits_Shouldnt_Generate_Report()
    {
        // Arrange
        var report = new ComparerReport();


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output.Should().BeEmpty();
    }


    [Fact]
    public void Should_Generate_Report_Only_With_Warnings()
    {
        // Arrange
        var report = new ComparerReport
        {
            Warnings = [
                "Warning 1",
                "Warning 2"
            ]
        };


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].Should().Be("Warning 1");
        _output[1].Should().Be("Warning 2");
        _output[2].Should().Be("");
    }

    [Fact]
    public void Should_Generate_Report_Only_With_Thrasholds()
    {
        // Arrange
        var report = new ComparerReport
        {
            HitThresholds = [
                "hit1",
                "hit2",
                "hit3"
            ]
        };


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].Should().Be("hit1");
        _output[1].Should().Be("hit2");
        _output[2].Should().Be("hit3");
        _output[3].Should().Be("");
    }

    [Fact]
    public void Should_Generate_Report_With_All()
    {
        // Arrange
        var report = new ComparerReport()
        {
            Warnings = [
                "Warning 2"
            ],
            HitThresholds = [
                "hit1",
                "hit3"
            ]
        };


        // Act
        _exporter.Generate(report, "");


        // Assert
        _output[0].Should().Be("Warning 2");
        _output[1].Should().Be("hit1");
        _output[2].Should().Be("hit3");
        _output[3].Should().Be("");
    }

    [Fact]
    public void Validate_If_FileOutputHitTxt_Is_Created()
    {
        // Arrange
        FileWriter writer = WriteFile;
        var output = new HitTxtExporter(writer);
        var report = new ComparerReport()
        {
            HitThresholds = [ "hit1" ]
        };
        var outputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var expectedFileName = Path.Combine(outputDirectory, "benchmark-comparison-hits.txt");


        // Act
        output.Generate(report, outputDirectory);


        // Assert
        File.Exists(expectedFileName).Should().BeTrue();
    }
}
