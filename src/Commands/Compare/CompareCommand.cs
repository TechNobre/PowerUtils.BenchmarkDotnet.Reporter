using System.CommandLine;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;

public sealed class CompareCommand(CompareHandler handler) : ICommandModule
{
    public Command Build()
    {
        var compareCommand = new Command(
            "compare",
            "Compare two BenchmarkDotNet reports and produce a diff report.")
        {
            CompareOptions.BaselineOption,
            CompareOptions.TargetOption,
            CompareOptions.MeanThresholdOption,
            CompareOptions.AllocationThresholdOption,
            CompareOptions.FormatsOption,
            CompareOptions.OutputOption,
            CompareOptions.FailOnWarningsOption,
            CompareOptions.FailOnThresholdHitOption
        };

        compareCommand.SetAction(parser =>
            handler.Execute(CompareOptions.Parse(parser)));

        return compareCommand;
    }
}
