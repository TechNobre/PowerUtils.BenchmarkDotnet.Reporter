using System.IO;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.IOUtils;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;

public sealed class JsonExporter(FileWriter writer) : IExporter
{
    private readonly FileWriter _writer = writer;

    public void Generate(GateReport report, string outputDirectory)
        => _writer(
            Path.Combine(outputDirectory, "benchmark-gate-report.json"),
            report.ToJson());
}
