using System.CommandLine;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

// We print a lot of numbers here and we want to make it always in invariant way
Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

var serviceCollection = new ServiceCollection();
serviceCollection
    .AddCompareCommand()
    .AddCommon();

var rootCommand = new RootCommand(".NET BenchmarkDotNet Reporter Tool");
foreach (var module in serviceCollection.BuildServiceProvider().GetServices<ICommandModule>())
{
    rootCommand.Subcommands.Add(module.Build());
}
return await rootCommand.Parse(args).InvokeAsync();
