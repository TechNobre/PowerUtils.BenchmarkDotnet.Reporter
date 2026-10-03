using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using static PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration.PbReporterConfiguration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;

public sealed record GateOptions
{
    public string? Input { get; init; }
    public IReadOnlyList<KeyValuePair<string, string>> MeanThreshold { get; init; } = [];
    public IReadOnlyList<KeyValuePair<string, string>> AllocationThreshold { get; init; } = [];
    public IReadOnlyList<string> Formats { get; init; } = [];
    public string Output { get; init; } = default!;
    public bool FailOnWarnings { get; init; }


    public static readonly Option<string> InputOption
        = new("--input", "-i")
        {
            Description = "Path to the folder or file with the report(s) to check. Can also be set via the PBREPORTER_GATE__INPUT environment variable or the 'input' key in the YAML config file; one of these sources must supply a value."
        };

    public static readonly Option<string[]> MeanThresholdOption
        = _createThresholdOption(
            "--threshold-mean",
            "-tm",
            "Fail when a benchmark's mean execution time exceeds this absolute value. Examples: 10ms, 10us, 100ns, 1s. Repeat with 'pattern=value' (e.g. 'MyNamespace.MyClass.*=10ms') to scope a threshold to matching benchmarks; a bare value (no 'pattern=') sets the global threshold.");

    public static readonly Option<string[]> AllocationThresholdOption
        = _createThresholdOption(
            "--threshold-allocation",
            "-ta",
            "Fail when a benchmark's allocated memory exceeds this absolute value. Examples: 10b, 10kb, 100mb, 1gb. Repeat with 'pattern=value' (e.g. 'MyNamespace.MyClass.*=10kb') to scope a threshold to matching benchmarks; a bare value (no 'pattern=') sets the global threshold.");

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
                    result.AddError(_invalidPatternMessage(pattern));
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
            Description = "Output format for the report. Can also be set via the PBREPORTER_GATE__FORMATS environment variable or the 'formats' key in the YAML config file (scalar or list).",
            DefaultValueFactory = _ => [ExporterFormats.CONSOLE]
        };

        option.Validators.Add(static result =>
        {
            var values = result.Tokens
                .Select(token => token.Value)
                .Where(value => !ExporterFormats.All.Contains(value));

            foreach(var value in values)
            {
                result.AddError(_invalidFormatMessage(value));
            }
        });

        return option;
    }

    public static readonly Option<string> OutputOption =
        new("--output", "-o")
        {
            Description = "Output directory to export the gate report.",
            DefaultValueFactory = _ => "./BenchmarkReporter"
        };

    public static readonly Option<bool> FailOnWarningsOption =
        new("--fail-on-warnings", "-fw")
        {
            Description = "Exit with error code when the gate check generates any warnings.",
            Required = false,
            DefaultValueFactory = _ => false
        };

    // Precedence, lowest to highest: config-file/env-var layer (Gate configuration) < CLI arguments.
    public static GateOptions Parse(ParseResult parser, GateConfigurationSection? configuration = null)
        => new()
        {
            Input = parser.GetValue(InputOption) ?? configuration?.Input,
            MeanThreshold = NamespacesUtils.Merge(
                _rulesFromConfig(configuration, configuration?.ThresholdMean, entry => entry.ThresholdMean),
                _parseThresholdTokens(parser.GetValue(MeanThresholdOption)!)),
            AllocationThreshold = NamespacesUtils.Merge(
                _rulesFromConfig(configuration, configuration?.ThresholdAllocation, entry => entry.ThresholdAllocation),
                _parseThresholdTokens(parser.GetValue(AllocationThresholdOption)!)),
            Formats = parser.GetResult(FormatsOption)?.Tokens.Count > 0
                ? parser.GetValue(FormatsOption)!
                : configuration?.Formats is { Count: > 0 } configFormats
                    ? _validateConfigFormats(configFormats)
                    : [ExporterFormats.CONSOLE],
            Output = parser.GetValue(OutputOption)!,
            FailOnWarnings = parser.GetValue(FailOnWarningsOption)
        };

    private static string _invalidFormatMessage(string value)
        => $"Invalid format '{value}'. Allowed values: {string.Join(", ", ExporterFormats.All)}";

    private static string _invalidPatternMessage(string pattern)
        => $"Invalid threshold pattern '{pattern}'. A '*' is only allowed as the last character of the pattern.";

    // Config/env values bypass the CLI option validators, so they are checked here.
    private static string[] _validateConfigFormats(IEnumerable<string> formats)
    {
        var result = formats.ToArray();

        var invalid = result.FirstOrDefault(format => !ExporterFormats.All.Contains(format));
        if(invalid is not null)
        {
            throw new DomainException(_invalidFormatMessage(invalid));
        }

        return result;
    }

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
        GateConfigurationSection? configuration,
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
                if(!NamespacesUtils.IsValidPattern(entry.Pattern))
                {
                    throw new DomainException(_invalidPatternMessage(entry.Pattern));
                }

                rules.Add(new(entry.Pattern, value));
            }
        }

        return rules;
    }
}
