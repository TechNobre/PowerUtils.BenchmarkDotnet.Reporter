using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;

public readonly struct TimeThreshold
{
    public readonly decimal Value;

    private TimeThreshold(decimal value)
    {
        Value = value;
    }


    public static bool TryParse(string? value, out TimeThreshold threshold)
    {
        threshold = default;

        if(!NumericUnitParser.TryExtract(value, out var numericValue, out var unit))
        {
            return false;
        }

        if(!NumericUnitParser.TryConvertTimeToNanoseconds(numericValue, unit, out numericValue))
        {
            return false;
        }

        threshold = new TimeThreshold(numericValue);
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
