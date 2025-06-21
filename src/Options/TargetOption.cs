using System.CommandLine;

namespace PowerUtils.BenchmarkDotnet.Reporter.Options;

public sealed class TargetOption : Option<string>
{
    public TargetOption()
        : base("--target", "-t")
    {
        Description = "Path to the folder or file with target reports.";
        Required = true;
    }
}
