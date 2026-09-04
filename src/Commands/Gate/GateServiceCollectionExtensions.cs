using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate;

public static class GateServiceCollectionExtensions
{
    public static IServiceCollection AddGateCommand(this IServiceCollection services)
        => services
            .AddTransient<ICommandModule, GateCommand>()
            .AddTransient<GateHandler>()
            .AddKeyedTransient<IExporter, MarkdownExporter>(ExporterFormats.MARKDOWN)
            .AddKeyedTransient<IExporter, JsonExporter>(ExporterFormats.JSON)
            .AddKeyedTransient<IExporter, HitTxtExporter>(ExporterFormats.HIT_TXT)
            .AddKeyedTransient<IExporter, ConsoleExporter>(ExporterFormats.CONSOLE)
            .AddTransient<IGateValidator, GateValidator>();
}
