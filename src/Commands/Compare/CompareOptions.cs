using System.CommandLine;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;
using System.Linq;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;

public sealed record CompareOptions
{
    public string? Baseline { get; init; }
    public string? Target { get; init; }
    public string? MeanThreshold { get; init; }
    public string? AllocationThreshold { get; init; }
    public string[] Formats { get; init; } = default!;
    public string Output { get; init; } = default!;
    public bool FailOnWarnings { get; init; }
    public bool FailOnThresholdHit { get; init; }


    public static readonly Option<string> BaselineOption
        = new("--baseline", "-b")
        {
            Description = "Path to the folder or file with Baseline report.",
            Required = true
        };

    public static readonly Option<string> TargetOption
        = new("--target", "-t")
        {
            Description = "Path to the folder or file with target reports.",
            Required = true
        };

    public static readonly Option<string> MeanThresholdOption
        = new("--threshold-mean", "-tm")
        {
            Description = "Throw an error when the mean threshold is met. Examples: 5%, 10ms, 10us, 100ns, 1s."
        };

    public static readonly Option<string> AllocationThresholdOption =
        new("--threshold-allocation", "-ta")
        {
            Description = "Throw an error when the allocation threshold is met. Examples: 5%, 10b, 10kb, 100mb, 1gb."
        };

    public static readonly Option<string[]> FormatsOption = _createFormats();
    private static Option<string[]> _createFormats()
    {
        var option = new Option<string[]>("--format", "-f")
        {
            Description = "Output format for the report.",
            DefaultValueFactory = _ => [ExporterFormats.CONSOLE]
        };

        option.Validators.Add(static result =>
        {
            var values = result.Tokens.Select(token => token.Value);
            foreach(var value in values)
            {
                if(!ExporterFormats.All.Contains(value))
                {
                    result.AddError($"Invalid format '{value}'. Allowed values: {string.Join(", ", ExporterFormats.All)}");
                }
            }
        });

        return option;
    }

    public static readonly Option<string> OutputOption =
        new("--output", "-o")
        {
            Description = "Output directory to export the diff report. Default is current directory.",
            DefaultValueFactory = _ => "./BenchmarkReporter"
        };

    public static readonly Option<bool> FailOnWarningsOption =
        new("--fail-on-warnings", "-fw")
        {
            Description = "Exit with error code when the comparison generates any warnings.",
            Required = false,
            DefaultValueFactory = _ => false
        };

    public static readonly Option<bool> FailOnThresholdHitOption =
        new("--fail-on-threshold-hit", "-ft")
        {
            Description = "Exit with error code when any threshold is hit during comparison.",
            Required = false,
            DefaultValueFactory = _ => false
        };

    public static CompareOptions Parse(ParseResult parser)
        => new()
        {
            Baseline = parser.GetValue(BaselineOption),
            Target = parser.GetValue(TargetOption),
            MeanThreshold = parser.GetValue(MeanThresholdOption),
            AllocationThreshold = parser.GetValue(AllocationThresholdOption),
            Formats = parser.GetValue(FormatsOption)!,
            Output = parser.GetValue(OutputOption)!,
            FailOnWarnings = parser.GetValue(FailOnWarningsOption),
            FailOnThresholdHit = parser.GetValue(FailOnThresholdHitOption)
        };
}
