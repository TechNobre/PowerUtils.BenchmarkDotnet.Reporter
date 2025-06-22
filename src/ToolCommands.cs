using System;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands;
using PowerUtils.BenchmarkDotnet.Reporter.Options;

namespace PowerUtils.BenchmarkDotnet.Reporter;

public sealed class ToolCommands : RootCommand
{
    public ToolCommands(IServiceProvider provider)
    {
        var baseline = new BaselineOption();
        var target = new TargetOption();

        var meanThreshold = new MeanThresholdOption();
        var allocationThreshold = new AllocationThresholdOption();

        var formats = new FormatsOption();
        var output = new OutputOption();

        var failOnWarnings = new FailOnWarningsOption();
        var failOnThresholdHit = new FailOnThresholdHitOption();

        var compareCommand = new Command(
            "compare",
            "Compare two BenchmarkDotNet reports and produce a diff report.")
        {
            baseline,
            target,
            meanThreshold,
            allocationThreshold,
            formats,
            output,
            failOnWarnings,
            failOnThresholdHit
        };

        compareCommand.SetAction(parser => provider
            .GetRequiredService<IComparerCommand>()
            .Execute(
                parser.GetValue(baseline)!,
                parser.GetValue(target)!,
                parser.GetValue(meanThreshold),
                parser.GetValue(allocationThreshold),
                parser.GetValue(formats)!,
                parser.GetValue(output)!,
                parser.GetValue(failOnWarnings),
                parser.GetValue(failOnThresholdHit)));

        Subcommands.Add(compareCommand);
    }
}
