using System;
using System.Collections.Generic;
using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Helpers.IOHelpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Tests.Exporters;

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
        _output.ShouldBeEmpty();
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
        _output[0].ShouldBe("Warning 1");
        _output[1].ShouldBe("Warning 2");
        _output[2].ShouldBe("");
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
        _output[0].ShouldBe("hit1");
        _output[1].ShouldBe("hit2");
        _output[2].ShouldBe("hit3");
        _output[3].ShouldBe("");
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
        _output[0].ShouldBe("Warning 2");
        _output[1].ShouldBe("hit1");
        _output[2].ShouldBe("hit3");
        _output[3].ShouldBe("");
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
        File.Exists(expectedFileName).ShouldBeTrue();
    }
}
