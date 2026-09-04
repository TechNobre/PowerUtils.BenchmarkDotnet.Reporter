using System.CommandLine;
using PowerUtils.BenchmarkDotnet.Reporter.Common;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;

public sealed class GateCommand(GateHandler handler) : ICommandModule
{
    public Command Build()
    {
        var gateCommand = new Command(
            "gate",
            "Check a BenchmarkDotNet report against absolute performance thresholds.")
        {
            GlobalOptions.ConfigOption,
            GateOptions.InputOption,
            GateOptions.MeanThresholdOption,
            GateOptions.AllocationThresholdOption,
            GateOptions.FormatsOption,
            GateOptions.OutputOption,
            GateOptions.FailOnWarningsOption
        };

        gateCommand.SetAction(GlobalExceptionHandler.Wrap(parser =>
        {
            var configFilePath = parser.GetValue(GlobalOptions.ConfigOption);
            var configuration = ConfigurationLoader.Load(configFilePath);
            return handler.Execute(GateOptions.Parse(parser, configuration.Gate));
        }));

        return gateCommand;
    }
}
