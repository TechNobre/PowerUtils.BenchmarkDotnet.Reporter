using System.Globalization;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Models;

public readonly struct MemoryThreshold
{
    public readonly decimal Value;
    public readonly bool IsPercentage;

    private MemoryThreshold(decimal value, bool isPercentage)
    {
        Value = value;
        IsPercentage = isPercentage;
    }


    public static bool TryParse(string? value, out MemoryThreshold threshold)
    {
        threshold = default;

        if(string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Find the position where the numeric part ends (digits and at most one '.')
        var i = 0;
        var hasDecimalPoint = false;
        while(i < value.Length && (char.IsDigit(value[i]) || (value[i] == '.' && !hasDecimalPoint)))
        {
            if(value[i] == '.') hasDecimalPoint = true;
            i++;
        }

        // Extract the value part; always use InvariantCulture so '.' is the decimal separator
        if(!decimal.TryParse(value[..i], NumberStyles.Number, CultureInfo.InvariantCulture, out var numericValue))
        {
            return false;
        }

        if(numericValue <= 0)
        {
            return false;
        }

        var isPercentage = false;

        // Extract the unit part
        var unit = value[i..];

        // Match unit string to enum
        switch(unit.ToLowerInvariant())
        {
            case "b":
                break;
            case "kb":
                numericValue *= 1_000;
                break;
            case "mb":
                numericValue *= 1_000 * 1_000;
                break;
            case "gb":
                numericValue *= 1_000 * 1_000 * 1_000;
                break;
            case "%":
                isPercentage = true;
                break;
            default:
                return false;
        }

        threshold = new MemoryThreshold(numericValue, isPercentage);
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
