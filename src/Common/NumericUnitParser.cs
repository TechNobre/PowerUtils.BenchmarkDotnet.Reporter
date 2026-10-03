using System.Globalization;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class NumericUnitParser
{
    // Scans a leading numeric part (digits + at most one '.') and returns it plus whatever
    // string remains as the unit suffix (e.g. "500ms" -> (500, "ms"), "10kb" -> (10, "kb")).
    public static bool TryExtract(string? value, out decimal numericValue, out string unit)
    {
        numericValue = 0;
        unit = string.Empty;

        if(string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var i = 0;
        var hasDecimalPoint = false;
        while(i < value.Length && (char.IsDigit(value[i]) || (value[i] == '.' && !hasDecimalPoint)))
        {
            if(value[i] == '.')
            {
                hasDecimalPoint = true;
            }

            i++;
        }

        // Always use InvariantCulture so '.' is the decimal separator
        if(!decimal.TryParse(value[..i], NumberStyles.Number, CultureInfo.InvariantCulture, out numericValue))
        {
            return false;
        }

        if(numericValue <= 0)
        {
            return false;
        }

        unit = value[i..];
        return true;
    }

    public static bool TryConvertTimeToNanoseconds(decimal value, string unit, out decimal nanoseconds)
    {
        nanoseconds = value;

        return unit.ToLowerInvariant() switch
        {
            "ns" => true,
            "μs" or "µs" or "us" => _tryMultiply(value, 1_000, out nanoseconds),
            "ms" => _tryMultiply(value, 1_000_000, out nanoseconds),
            "s" => _tryMultiply(value, 1_000_000_000, out nanoseconds),
            _ => false,
        };
    }

    public static bool TryConvertMemoryToBytes(decimal value, string unit, out decimal bytes)
    {
        bytes = value;

        return unit.ToLowerInvariant() switch
        {
            "b" => true,
            "kb" => _tryMultiply(value, 1_000, out bytes),
            "mb" => _tryMultiply(value, 1_000_000, out bytes),
            "gb" => _tryMultiply(value, 1_000_000_000, out bytes),
            _ => false,
        };
    }

    // Leaves `result` as the original value when the product would overflow decimal.
    private static bool _tryMultiply(decimal value, int factor, out decimal result)
    {
        result = value;

        if(value > decimal.MaxValue / factor)
        {
            return false;
        }

        result = value * factor;
        return true;
    }
}
