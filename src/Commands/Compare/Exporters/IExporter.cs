using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;

public interface IExporter
{
    void Generate(ComparerReport report, string outputDirectory);
}
