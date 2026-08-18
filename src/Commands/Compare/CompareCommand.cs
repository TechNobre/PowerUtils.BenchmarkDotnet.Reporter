using System.CommandLine;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;

public sealed class CompareCommand(CompareHandler handler) : ICommandModule
{
    public Command Build()
    {
        var compareCommand = new Command(
            "compare",
            "Compare two BenchmarkDotNet reports and produce a diff report.")
        {
            GlobalOptions.ConfigOption,
            CompareOptions.BaselineOption,
            CompareOptions.TargetOption,
            CompareOptions.MeanThresholdOption,
            CompareOptions.AllocationThresholdOption,
            CompareOptions.FormatsOption,
            CompareOptions.OutputOption,
            CompareOptions.FailOnWarningsOption,
            CompareOptions.FailOnThresholdHitOption
        };

        compareCommand.SetAction(GlobalExceptionHandler.Wrap(parser =>
        {
            var configFilePath = parser.GetValue(GlobalOptions.ConfigOption);
            var configuration = ConfigurationLoader.Load(configFilePath);
            return handler.Execute(CompareOptions.Parse(parser, configuration.Compare));
        }));

        return compareCommand;
    }
}
