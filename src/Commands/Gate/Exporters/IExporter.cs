using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;

public interface IExporter
{
    void Generate(GateReport report, string outputDirectory);
}
