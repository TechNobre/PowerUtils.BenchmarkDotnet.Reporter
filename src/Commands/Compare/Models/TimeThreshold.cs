using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;

public readonly struct TimeThreshold
{
    public readonly decimal Value;
    public readonly bool IsPercentage;

    private TimeThreshold(decimal value, bool isPercentage)
    {
        Value = value;
        IsPercentage = isPercentage;
    }


    public static bool TryParse(string? value, out TimeThreshold threshold)
    {
        threshold = default;

        if(!NumericUnitParser.TryExtract(value, out var numericValue, out var unit))
        {
            return false;
        }

        var isPercentage = unit == "%";
        if(!isPercentage && !NumericUnitParser.TryConvertTimeToNanoseconds(numericValue, unit, out numericValue))
        {
            return false;
        }

        threshold = new TimeThreshold(numericValue, isPercentage);
        return true;
    }

    public static TimeThreshold Parse(string? value)
    {
        if(TryParse(value, out var threshold))
        {
            return threshold;
        }
        throw new DomainException($"The value '{value}' is not a valid threshold.");
    }

    public static implicit operator decimal(TimeThreshold threshold) => threshold.Value;
}
