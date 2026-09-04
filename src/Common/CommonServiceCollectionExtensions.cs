using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class CommonServiceCollectionExtensions
{
    public static IServiceCollection AddCommon(this IServiceCollection services)
        => services
            .AddTransient<IOUtils.FileWriter>(sp =>
                (path, content) => IOUtils.WriteFile(path, content))
            .AddTransient<Func<string?, List<BenchmarkReport>>>(sp =>
                (path) => BenchmarkReportLoader.ReadBenchmarkReports(path));
}
