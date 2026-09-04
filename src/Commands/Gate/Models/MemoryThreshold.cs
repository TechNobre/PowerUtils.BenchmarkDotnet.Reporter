using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;

public readonly struct MemoryThreshold
{
    public readonly decimal Value;

    private MemoryThreshold(decimal value)
    {
        Value = value;
    }


    public static bool TryParse(string? value, out MemoryThreshold threshold)
    {
        threshold = default;

        if(!NumericUnitParser.TryExtract(value, out var numericValue, out var unit))
        {
            return false;
        }

        if(!NumericUnitParser.TryConvertMemoryToBytes(numericValue, unit, out numericValue))
        {
            return false;
        }

        threshold = new MemoryThreshold(numericValue);
        return true;
    }

    public static MemoryThreshold Parse(string? value)
    {
        if(TryParse(value, out var threshold))
        {
            return threshold;
        }
        throw new DomainException($"The value '{value}' is not a valid threshold.");
    }

    public static implicit operator decimal(MemoryThreshold threshold) => threshold.Value;
}
