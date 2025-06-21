using System.CommandLine;

namespace PowerUtils.BenchmarkDotnet.Reporter.Options;

public sealed class OutputOption : Option<string>
{
    public OutputOption()
        : base("--output", "-o")
    {
        Description = "Output directory to export the diff report. Default is current directory.";
        DefaultValueFactory = _ => "./BenchmarkReporter";
    }
}
