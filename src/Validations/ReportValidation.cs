using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Helpers;
using PowerUtils.BenchmarkDotnet.Reporter.Models;

namespace PowerUtils.BenchmarkDotnet.Reporter.Validations;

public interface IReportValidation
{
    List<string> HostEnvironmentValidate(BenchmarkFullJsonResport? baseline, BenchmarkFullJsonResport? target);
}

public sealed class ReportValidation : IReportValidation
{
    public List<string> HostEnvironmentValidate(BenchmarkFullJsonResport? baseline, BenchmarkFullJsonResport? target)
    {
        var messages = new List<string>();

        if(baseline is null)
        {
            messages.Add("The baseline report isn't defined");
            return messages;
        }

        if(target is null)
        {
            messages.Add("The target report isn't defined");
            return messages;
        }

        if(baseline.HostEnvironmentInfo?.OsVersion.Equivalente(target.HostEnvironmentInfo?.OsVersion) == false)
        {
            messages.Add($"OS Version is different: '{baseline.HostEnvironmentInfo?.OsVersion}' != '{target.HostEnvironmentInfo?.OsVersion}'");
        }

        if(baseline.HostEnvironmentInfo?.ProcessorName.Equivalente(target.HostEnvironmentInfo?.ProcessorName) == false)
        {
            messages.Add($"Processor Name is different: '{baseline.HostEnvironmentInfo?.ProcessorName}' != '{target.HostEnvironmentInfo?.ProcessorName}'");
        }

        if(baseline.HostEnvironmentInfo?.PhysicalProcessorCount != target.HostEnvironmentInfo?.PhysicalProcessorCount)
        {
            messages.Add($"Physical Processor Count is different: '{baseline.HostEnvironmentInfo?.PhysicalProcessorCount}' != '{target.HostEnvironmentInfo?.PhysicalProcessorCount}'");
        }

        if(baseline.HostEnvironmentInfo?.PhysicalCoreCount != target.HostEnvironmentInfo?.PhysicalCoreCount)
        {
            messages.Add($"Physical Core Count is different: '{baseline.HostEnvironmentInfo?.PhysicalCoreCount}' != '{target.HostEnvironmentInfo?.PhysicalCoreCount}'");
        }

        if(baseline.HostEnvironmentInfo?.LogicalCoreCount != target.HostEnvironmentInfo?.LogicalCoreCount)
        {
            messages.Add($"Logical Core Count is different: '{baseline.HostEnvironmentInfo?.LogicalCoreCount}' != '{target.HostEnvironmentInfo?.LogicalCoreCount}'");
        }

        if(baseline.HostEnvironmentInfo?.RuntimeVersion.Equivalente(target.HostEnvironmentInfo?.RuntimeVersion) == false)
        {
            messages.Add($"Runtime Version is different: '{baseline.HostEnvironmentInfo?.RuntimeVersion}' != '{target.HostEnvironmentInfo?.RuntimeVersion}'");
        }

        if(baseline.HostEnvironmentInfo?.Architecture.Equivalente(target.HostEnvironmentInfo?.Architecture) == false)
        {
            messages.Add($"Architecture is different: '{baseline.HostEnvironmentInfo?.Architecture}' != '{target.HostEnvironmentInfo?.Architecture}'");
        }

        if(baseline.HostEnvironmentInfo?.DotNetCliVersion.Equivalente(target.HostEnvironmentInfo?.DotNetCliVersion) == false)
        {
            messages.Add($"DotNet CLI Version is different: '{baseline.HostEnvironmentInfo?.DotNetCliVersion}' != '{target.HostEnvironmentInfo?.DotNetCliVersion}'");
        }

        if(baseline.HostEnvironmentInfo?.ChronometerFrequency?.Hertz != target.HostEnvironmentInfo?.ChronometerFrequency?.Hertz)
        {
            messages.Add($"Chronometer Frequency is different: '{baseline.HostEnvironmentInfo?.ChronometerFrequency?.Hertz}' != '{target.HostEnvironmentInfo?.ChronometerFrequency?.Hertz}'");
        }

        if("RELEASE".Equivalente(baseline.HostEnvironmentInfo?.Configuration) == false)
        {
            messages.Add($"The baseline report wasn't executed in RELEASE mode: '{baseline.HostEnvironmentInfo?.Configuration}'");
        }

        if("RELEASE".Equivalente(target.HostEnvironmentInfo?.Configuration) == false)
        {
            messages.Add($"The target report wasn't executed in RELEASE mode: '{target.HostEnvironmentInfo?.Configuration}'");
        }

        return messages;
    }
}
