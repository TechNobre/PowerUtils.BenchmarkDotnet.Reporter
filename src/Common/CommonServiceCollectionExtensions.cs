using Microsoft.Extensions.DependencyInjection;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class CommonServiceCollectionExtensions
{
    public static IServiceCollection AddCommon(this IServiceCollection services)
        => services
            .AddTransient<IOUtils.FileWriter>(sp =>
                (path, content) => IOUtils.WriteFile(path, content));
}
