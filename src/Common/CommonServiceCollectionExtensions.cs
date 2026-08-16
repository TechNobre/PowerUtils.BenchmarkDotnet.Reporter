using Microsoft.Extensions.DependencyInjection;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class CommonServiceCollectionExtensions
{
    public static IServiceCollection AddCommon(this IServiceCollection services)
        => services
            .AddTransient<IOHelpers.FileWriter>(sp =>
                (path, content) => IOHelpers.WriteFile(path, content));
}
