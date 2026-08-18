using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.IOUtils;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;

public sealed class JsonExporter(FileWriter writer) : IExporter
{
    private readonly FileWriter _writer = writer;

    private static readonly JsonSerializerOptions _options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public void Generate(ComparerReport report, string outputDirectory)
        => _writer(
            Path.Combine(outputDirectory, "benchmark-comparison-report.json"),
            JsonSerializer.Serialize(
                report,
                _options));
}
