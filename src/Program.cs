using System;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter;
using PowerUtils.BenchmarkDotnet.Reporter.Commands;
using PowerUtils.BenchmarkDotnet.Reporter.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;
using PowerUtils.BenchmarkDotnet.Reporter.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Validations;

// We print a lot of numbers here and we want to make it always in invariant way
Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

var serviceCollection = new ServiceCollection();
serviceCollection
    .AddTransient<ToolCommands>()
    .AddTransient<IOHelpers.Printer>(sp =>
        (message) => IOHelpers.Print(message))
    .AddTransient<IOHelpers.FileWriter>(sp =>
        (path, content) => IOHelpers.WriteFile(path, content))
    .AddTransient<Func<string?, BenchmarkFullJsonResport[]>>(sp =>
        (path) => IOHelpers.ReadFullJsonReport(path))
    .AddTransient<IReportValidation, ReportValidation>()
    .AddKeyedTransient<IExporter, MarkdownExporter>("markdown")
    .AddKeyedTransient<IExporter, JsonExporter>("json")
    .AddKeyedTransient<IExporter, HitTxtExporter>("hit-txt")
    .AddKeyedTransient<IExporter, ConsoleExporter>("console")
    .AddTransient<IComparerCommand, ComparerCommand>();

return serviceCollection.BuildServiceProvider()
    .GetRequiredService<ToolCommands>()
    .Parse(args)
    .Invoke();
