using System.CommandLine;

namespace PowerUtils.BenchmarkDotnet.Reporter.Options;

public sealed class FailOnThresholdHitOption : Option<bool>
{    public FailOnThresholdHitOption()
        : base("--fail-on-threshold-hit", "-ft")
    {
        Description = "Exit with error code when any threshold is hit during comparison.";
        Required = false;
        DefaultValueFactory = _ => false;
    }
}
