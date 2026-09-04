using System.IO;
using System.Text;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.IOUtils;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;

public sealed class HitTxtExporter(FileWriter writer) : IExporter
{
    private readonly FileWriter _writer = writer;

    public void Generate(GateReport report, string outputDirectory)
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
                Path.Combine(outputDirectory, "benchmark-gate-hits.txt"),
                sb.ToString());
        }
    }
}
