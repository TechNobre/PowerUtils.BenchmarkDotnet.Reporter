using System.CommandLine;

namespace PowerUtils.BenchmarkDotnet.Reporter.Options;

public sealed class MeanThresholdOption : Option<string>
{
    public MeanThresholdOption()
        : base("--threshold-mean", "-tm")
    {
        Description = "Throw an error when the mean threshold is met. Examples: 5%, 10ms, 10μs, 100ns, 1s.";
    }
}
