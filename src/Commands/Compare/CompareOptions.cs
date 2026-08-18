using System;
using System.CommandLine;
using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration.PbReporterConfiguration;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration.PbReporterConfiguration.CompareConfigurationSection;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;

public sealed record CompareOptions
{
    public string? Baseline { get; init; }
    public string? Target { get; init; }
    public IReadOnlyList<KeyValuePair<string, string>> MeanThreshold { get; init; } = [];
    public IReadOnlyList<KeyValuePair<string, string>> AllocationThreshold { get; init; } = [];
    public IReadOnlyList<string> Formats { get; init; } = [];
    public string Output { get; init; } = default!;
    public bool FailOnWarnings { get; init; }
    public bool FailOnThresholdHit { get; init; }


    public static readonly Option<string> BaselineOption
        = new("--baseline", "-b")
        {
            Description = "Path to the folder or file with Baseline report. Can also be set via the PBREPORTER_COMPARE__BASELINE environment variable or the 'baseline' key in the YAML config file; one of these sources must supply a value."
        };

    public static readonly Option<string> TargetOption
        = new("--target", "-t")
        {
            Description = "Path to the folder or file with target reports. Can also be set via the PBREPORTER_COMPARE__TARGET environment variable or the 'target' key in the YAML config file; one of these sources must supply a value."
        };

    public static readonly Option<string[]> MeanThresholdOption
        = _createThresholdOption(
            "--threshold-mean",
            "-tm",
            "Throw an error when the mean threshold is met. Examples: 5%, 10ms, 10us, 100ns, 1s. Repeat with 'pattern=value' (e.g. 'MyNamespace.MyClass.*=10ms') to scope a threshold to matching benchmarks; a bare value (no 'pattern=') sets the global threshold.");

    public static readonly Option<string[]> AllocationThresholdOption
        = _createThresholdOption(
            "--threshold-allocation",
            "-ta",
            "Throw an error when the allocation threshold is met. Examples: 5%, 10b, 10kb, 100mb, 1gb. Repeat with 'pattern=value' (e.g. 'MyNamespace.MyClass.*=10kb') to scope a threshold to matching benchmarks; a bare value (no 'pattern=') sets the global threshold.");

    private static Option<string[]> _createThresholdOption(string name, string alias, string description)
    {
        var option = new Option<string[]>(name, alias)
        {
            Description = description,
            DefaultValueFactory = _ => []
        };

        option.Validators.Add(static result =>
        {
            foreach(var token in result.Tokens.Select(token => token.Value))
            {
                var separatorIndex = token.IndexOf('=');
                if(separatorIndex == -1)
                {
                    // A bare value (no 'pattern=') applies to every benchmark, equivalent to '*=value'.
                    continue;
                }

                var pattern = token[..separatorIndex];
                if(!NamespacesUtils.IsValidPattern(pattern))
                {
                    result.AddError($"Invalid threshold pattern '{pattern}'. A '*' is only allowed as the last character of the pattern.");
                }
            }
        });

        return option;
    }

    public static readonly Option<string[]> FormatsOption = _createFormats();
    private static Option<string[]> _createFormats()
    {
        var option = new Option<string[]>("--format", "-f")
        {
            Description = "Output format for the report. Can also be set via the PBREPORTER_COMPARE__FORMATS environment variable or the 'formats' key in the YAML config file (scalar or list).",
            DefaultValueFactory = _ => [ExporterFormats.CONSOLE]
        };

        option.Validators.Add(static result =>
        {
            var values = result.Tokens
                .Select(token => token.Value)
                .Where(value => !ExporterFormats.All.Contains(value));

            foreach (var value in values)
            {
                result.AddError($"Invalid format '{value}'. Allowed values: {string.Join(", ", ExporterFormats.All)}");
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

    // Precedence, lowest to highest: config-file/env-var layer (Compare configuration) < CLI arguments.
    public static CompareOptions Parse(ParseResult parser, CompareConfigurationSection? configuration = null)
        => new()
        {
            Baseline = parser.GetValue(BaselineOption) ?? configuration?.Baseline,
            Target = parser.GetValue(TargetOption) ?? configuration?.Target,
            MeanThreshold = NamespacesUtils.Merge(
                _rulesFromConfig(configuration, configuration?.ThresholdMean, entry => entry.ThresholdMean),
                _parseThresholdTokens(parser.GetValue(MeanThresholdOption)!)),
            AllocationThreshold = NamespacesUtils.Merge(
                _rulesFromConfig(configuration, configuration?.ThresholdAllocation, entry => entry.ThresholdAllocation),
                _parseThresholdTokens(parser.GetValue(AllocationThresholdOption)!)),
            Formats = parser.GetResult(FormatsOption)?.Tokens.Count > 0
                ? parser.GetValue(FormatsOption)!
                : configuration?.Formats is { Count: > 0 } configFormats
                    ? configFormats.ToArray()
                    : [ExporterFormats.CONSOLE],
            Output = parser.GetValue(OutputOption)!,
            FailOnWarnings = parser.GetValue(FailOnWarningsOption),
            FailOnThresholdHit = parser.GetValue(FailOnThresholdHitOption)
        };

    private static List<KeyValuePair<string, string>> _parseThresholdTokens(string[] tokens)
    {
        var rules = new List<KeyValuePair<string, string>>();

        foreach(var token in tokens)
        {
            var separatorIndex = token.IndexOf('=');

            rules.Add(separatorIndex == -1
                ? new(NamespacesUtils.WILDCARD.ToString(), token)
                : new(token[..separatorIndex], token[(separatorIndex + 1)..]));
        }

        return rules;
    }

    private static List<KeyValuePair<string, string>> _rulesFromConfig(
        CompareConfigurationSection? configuration,
        string? globalValue,
        Func<ScopedThresholdConfig, string?> scopedValueSelector)
    {
        var rules = new List<KeyValuePair<string, string>>();

        if(!string.IsNullOrWhiteSpace(globalValue))
        {
            rules.Add(new(NamespacesUtils.WILDCARD.ToString(), globalValue));
        }

        foreach(var entry in configuration?.Thresholds ?? [])
        {
            var value = scopedValueSelector(entry);
            if(!string.IsNullOrWhiteSpace(entry.Pattern) && !string.IsNullOrWhiteSpace(value))
            {
                rules.Add(new(entry.Pattern, value));
            }
        }

        return rules;
    }
}
