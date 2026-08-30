using System.Globalization;
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
            case "ns":
                break;
            case "μs":
            case "µs":
            case "us":
                numericValue *= 1_000;
                break;
            case "ms":
                numericValue *= 1_000 * 1_000;
                break;
            case "s":
                numericValue *= 1_000 * 1_000 * 1_000;
                break;
            case "%":
                isPercentage = true;
                break;
            default:
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
