using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Exporters;

public interface IExporter
{
    void Generate(ComparerReport report, string outputDirectory);
}
