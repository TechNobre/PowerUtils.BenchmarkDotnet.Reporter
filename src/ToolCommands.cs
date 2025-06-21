using System;
using System.CommandLine;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands;

namespace PowerUtils.BenchmarkDotnet.Reporter;

public sealed class ToolCommands : RootCommand
{
    public ToolCommands(IServiceProvider provider)
    {
        var baseline = _createBaselineOption();
        var target = _createTargetOption();

        var meanThreshold = _createMeanThresholdOption();
        var allocationThreshold = _createAllocationThresholdOption();

        var formats = _createFormatsOption();
        var output = _createOutputOption();

        var compareCommand = new Command(
            "compare",
            "Compare two BenchmarkDotNet reports and produce a diff report.")
        {
            baseline,
            target,
            meanThreshold,
            allocationThreshold,
            formats,
            output
        };

        compareCommand.SetAction(parser => provider
            .GetRequiredService<IComparerCommand>()
            .Execute(
                parser.GetValue(baseline)!,
                parser.GetValue(target)!,
                parser.GetValue(meanThreshold),
                parser.GetValue(allocationThreshold),
                parser.GetValue(formats)!,
                parser.GetValue(output)!));

        Subcommands.Add(compareCommand);
    }


    private static Option<string> _createBaselineOption()
        => new("--baseline", "-b")
        {
            Description = "Path to the folder or file with Baseline report.",
            Required = true
        };

    private static Option<string> _createTargetOption()
        => new("--target", "-t")
        {
            Description = "Path to the folder or file with target reports.",
            Required = true
        };

    private static Option<string> _createMeanThresholdOption()
        => new("--threshold-mean", "-tm")
        {
            Description = "Throw an error when the mean threshold is met. Examples: 5%, 10ms, 10μs, 100ns, 1s."
        };

    private static Option<string> _createAllocationThresholdOption()
        => new("--threshold-allocation", "-ta")
        {
            Description = "Throw an error when the allocation threshold is met. Examples: 5%, 10b, 10kb, 100mb, 1gb."
        };

    private static Option<string[]> _createFormatsOption()
    {
        var option = new Option<string[]>("--format", "-f")
        {
            Description = "Output format for the report.",
            DefaultValueFactory = _ => ["console"]
        };

        option.Validators.Add(result =>
        {
            var allowedValues = new[] { "console", "markdown", "json", "hit-txt" };
            var values = result.GetValue(option);
            if(values != null)
            {
                foreach(var value in values)
                {
                    if(!allowedValues.Contains(value))
                    {
                        result.AddError($"Invalid format '{value}'. Allowed values: {string.Join(", ", allowedValues)}");
                    }
                }
            }
        });

        return option;
    }

    private static Option<string> _createOutputOption()
        => new("--output", "-o")
        {
            Description = "Output directory to export the diff report. Default is current directory.",
            DefaultValueFactory = _ => "./BenchmarkReporter"
        };
}
