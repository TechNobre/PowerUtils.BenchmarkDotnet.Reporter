using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare;

public static class CompareServiceCollectionExtensions
{
    public static IServiceCollection AddCompareCommand(this IServiceCollection services)
        => services
            .AddTransient<ICommandModule, CompareCommand>()
            .AddTransient<CompareHandler>()
            .AddKeyedTransient<IExporter, MarkdownExporter>(ExporterFormats.MARKDOWN)
            .AddKeyedTransient<IExporter, JsonExporter>(ExporterFormats.JSON)
            .AddKeyedTransient<IExporter, HitTxtExporter>(ExporterFormats.HIT_TXT)
            .AddKeyedTransient<IExporter, ConsoleExporter>(ExporterFormats.CONSOLE)
            .AddTransient<ICompareValidator, CompareValidator>()
            .AddTransient<Func<string?, List<BenchmarkReport>>>(sp =>
                (path) => CompareHelpers.ReadBenchmarkReports(path));
}
