using System.CommandLine;

namespace PowerUtils.BenchmarkDotnet.Reporter.Options;

public sealed class FailOnWarningsOption : Option<bool>
{    public FailOnWarningsOption()
        : base("--fail-on-warnings", "-fw")
    {
        Description = "Exit with error code when the comparison generates any warnings.";
        Required = false;
        DefaultValueFactory = _ => false;
    }
}
