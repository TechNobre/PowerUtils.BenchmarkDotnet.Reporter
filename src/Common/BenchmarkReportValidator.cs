using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Common.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class BenchmarkReportValidator
{
    private const string RELEASE = "RELEASE";

    public static void AddIfNotRelease(this List<string> messages, BenchmarkReport? report, string? label = null)
    {
        if(report is null)
        {
            return;
        }

        var configuration = report.Header?.HostEnvironmentInfo?.Configuration;

        if(!RELEASE.EquivalentTo(configuration))
        {
            var subject = string.IsNullOrWhiteSpace(label) ?
                "report" :
                $"{label} report";
            messages.Add($"[{report.FullName}] The {subject} wasn't executed in RELEASE mode: '{configuration}'");
        }
    }
}
