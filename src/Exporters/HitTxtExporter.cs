using System.IO;
using System.Text;
using PowerUtils.BenchmarkDotnet.Reporter.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Helpers.IOHelpers;

namespace PowerUtils.BenchmarkDotnet.Reporter.Exporters;

public sealed class HitTxtExporter(FileWriter writer) : IExporter
{
    private readonly FileWriter _writer = writer;

    public void Generate(ComparerReport report, string outputDirectory)
    {
        var sb = new StringBuilder();

        foreach(var warning in report.Warnings)
        {
            sb.AppendLine(warning);
        }

        foreach(var hit in report.HitThresholds)
        {
            sb.AppendLine(hit);
        }

        if(sb.Length > 0)
        {
            _writer(
                Path.Combine(outputDirectory, "benchmark-comparison-hits.txt"),
                sb.ToString());
        }
    }
}
