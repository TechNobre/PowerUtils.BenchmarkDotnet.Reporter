using System.CommandLine;

namespace PowerUtils.BenchmarkDotnet.Reporter.Options;

public sealed class AllocationThresholdOption : Option<string>
{
    public AllocationThresholdOption()
        : base("--threshold-allocation", "-ta")
    {
        Description = "Throw an error when the allocation threshold is met. Examples: 5%, 10b, 10kb, 100mb, 1gb.";
    }
}
