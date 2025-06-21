using System.CommandLine;

namespace PowerUtils.BenchmarkDotnet.Reporter.Options;

public sealed class BaselineOption : Option<string>
{
    public BaselineOption()
        : base("--baseline", "-b")
    {
        Description = "Path to the folder or file with Baseline report.";
        Required = true;
    }
}
