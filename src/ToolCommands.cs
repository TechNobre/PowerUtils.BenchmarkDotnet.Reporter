using System;
using System.CommandLine;
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

        compareCommand
            .SetHandler(
                provider.GetRequiredService<IComparerCommand>().Execute,
                baseline,
                target,
                meanThreshold,
                allocationThreshold,
                formats,
                output);

        Add(compareCommand);
    }


    private static Option<string> _createBaselineOption()
        => new(
            ["-b", "--baseline"],
            "Path to the folder or file with Baseline report.")
        {
            IsRequired = true
        };

    private static Option<string> _createTargetOption()
        => new(
            ["-t", "--target"],
            "Path to the folder or file with target reports.")
        {
            IsRequired = true
        };

    private static Option<string> _createMeanThresholdOption()
        => new(
            ["-tm", "--threshold-mean"],
            "Throw an error when the mean threshold is met. Examples: 5%, 10ms, 10μs, 100ns, 1s.");

    private static Option<string> _createAllocationThresholdOption()
        => new(
            ["-ta", "--threshold-allocation"],
            "Throw an error when the allocation threshold is met. Examples: 5%, 10b, 10kb, 100mb, 1gb.");

    private static Option<string[]> _createFormatsOption()
        => new Option<string[]>(
            ["-f", "--format"],
            () => ["console"],
            "Output format for the report.")
        .FromAmong(
            "console",
            "markdown",
            "json",
            "hit-txt");

    private static Option<string> _createOutputOption()
        => new(
            ["-o", "--output"],
            () => "./BenchmarkReporter",
            "Output directory to export the diff report. Default is current directory.");
}
