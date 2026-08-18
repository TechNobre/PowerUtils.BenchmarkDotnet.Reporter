using System.CommandLine;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class GlobalOptions
{
    public static readonly Option<string> ConfigOption = new("--config", "-c")
    {
        Description = "Path to a YAML configuration file. Defaults to 'pbreporter.yml' or 'pbreporter.yaml' in the current directory when present.",
        Recursive = true
    };
}
