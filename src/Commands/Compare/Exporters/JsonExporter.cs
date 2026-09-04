using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.IOUtils;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;

public sealed class JsonExporter(FileWriter writer) : IExporter
{
    private readonly FileWriter _writer = writer;

    public void Generate(ComparerReport report, string outputDirectory)
        => _writer(
            Path.Combine(outputDirectory, "benchmark-comparison-report.json"),
            report.ToJson());
}
