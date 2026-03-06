using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;
using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Validations;

public interface IReportValidation
{
    List<string> HostEnvironmentValidate(BenchmarkResport? baseline, BenchmarkResport? target);
}

public sealed class ReportValidation : IReportValidation
{
    public List<string> HostEnvironmentValidate(BenchmarkResport? baseline, BenchmarkResport? target)
    {
        var messages = new List<string>();

        if(baseline is null)
        {
            return messages;
        }

        if(target is null)
        {
            return messages;
        }

        if(baseline.Header?.HostEnvironmentInfo?.OsVersion.Equivalente(target.Header?.HostEnvironmentInfo?.OsVersion) == false)
        {
            messages.Add($"[{baseline.FullName}] OS Version is different: '{baseline.Header?.HostEnvironmentInfo?.OsVersion}' != '{target.Header?.HostEnvironmentInfo?.OsVersion}'");
        }

        if(baseline.Header?.HostEnvironmentInfo?.ProcessorName.Equivalente(target.Header?.HostEnvironmentInfo?.ProcessorName) == false)
        {
            messages.Add($"[{baseline.FullName}] Processor Name is different: '{baseline.Header?.HostEnvironmentInfo?.ProcessorName}' != '{target.Header?.HostEnvironmentInfo?.ProcessorName}'");
        }

        if(baseline.Header?.HostEnvironmentInfo?.PhysicalProcessorCount != target.Header?.HostEnvironmentInfo?.PhysicalProcessorCount)
        {
            messages.Add($"[{baseline.FullName}] Physical Processor Count is different: '{baseline.Header?.HostEnvironmentInfo?.PhysicalProcessorCount}' != '{target.Header?.HostEnvironmentInfo?.PhysicalProcessorCount}'");
        }

        if(baseline.Header?.HostEnvironmentInfo?.PhysicalCoreCount != target.Header?.HostEnvironmentInfo?.PhysicalCoreCount)
        {
            messages.Add($"[{baseline.FullName}] Physical Core Count is different: '{baseline.Header?.HostEnvironmentInfo?.PhysicalCoreCount}' != '{target.Header?.HostEnvironmentInfo?.PhysicalCoreCount}'");
        }

        if(baseline.Header?.HostEnvironmentInfo?.LogicalCoreCount != target.Header?.HostEnvironmentInfo?.LogicalCoreCount)
        {
            messages.Add($"[{baseline.FullName}] Logical Core Count is different: '{baseline.Header?.HostEnvironmentInfo?.LogicalCoreCount}' != '{target.Header?.HostEnvironmentInfo?.LogicalCoreCount}'");
        }

        if(baseline.Header?.HostEnvironmentInfo?.RuntimeVersion.Equivalente(target.Header?.HostEnvironmentInfo?.RuntimeVersion) == false)
        {
            messages.Add($"[{baseline.FullName}] Runtime Version is different: '{baseline.Header?.HostEnvironmentInfo?.RuntimeVersion}' != '{target.Header?.HostEnvironmentInfo?.RuntimeVersion}'");
        }

        if(baseline.Header?.HostEnvironmentInfo?.Architecture.Equivalente(target.Header?.HostEnvironmentInfo?.Architecture) == false)
        {
            messages.Add($"[{baseline.FullName}] Architecture is different: '{baseline.Header?.HostEnvironmentInfo?.Architecture}' != '{target.Header?.HostEnvironmentInfo?.Architecture}'");
        }

        if(baseline.Header?.HostEnvironmentInfo?.DotNetCliVersion.Equivalente(target.Header?.HostEnvironmentInfo?.DotNetCliVersion) == false)
        {
            messages.Add($"[{baseline.FullName}] DotNet CLI Version is different: '{baseline.Header?.HostEnvironmentInfo?.DotNetCliVersion}' != '{target.Header?.HostEnvironmentInfo?.DotNetCliVersion}'");
        }

        if(baseline.Header?.HostEnvironmentInfo?.ChronometerFrequency?.Hertz != target.Header?.HostEnvironmentInfo?.ChronometerFrequency?.Hertz)
        {
            messages.Add($"[{baseline.FullName}] Chronometer Frequency is different: '{baseline.Header?.HostEnvironmentInfo?.ChronometerFrequency?.Hertz}' != '{target.Header?.HostEnvironmentInfo?.ChronometerFrequency?.Hertz}'");
        }

        if(!"RELEASE".Equivalente(baseline.Header?.HostEnvironmentInfo?.Configuration))
        {
            messages.Add($"[{baseline.FullName}] The baseline report wasn't executed in RELEASE mode: '{baseline.Header?.HostEnvironmentInfo?.Configuration}'");
        }

        if(!"RELEASE".Equivalente(target.Header?.HostEnvironmentInfo?.Configuration))
        {
            messages.Add($"[{target.FullName}] The target report wasn't executed in RELEASE mode: '{target.Header?.HostEnvironmentInfo?.Configuration}'");
        }

        return messages;
    }
}
