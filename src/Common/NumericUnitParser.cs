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
            if(value[i] == '.') hasDecimalPoint = true;
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

        switch(unit.ToLowerInvariant())
        {
            case "ns":
                return true;
            case "μs":
            case "µs":
            case "us":
                nanoseconds *= 1_000;
                return true;
            case "ms":
                nanoseconds *= 1_000 * 1_000;
                return true;
            case "s":
                nanoseconds *= 1_000 * 1_000 * 1_000;
                return true;
            default:
                return false;
        }
    }

    public static bool TryConvertMemoryToBytes(decimal value, string unit, out decimal bytes)
    {
        bytes = value;

        switch(unit.ToLowerInvariant())
        {
            case "b":
                return true;
            case "kb":
                bytes *= 1_000;
                return true;
            case "mb":
                bytes *= 1_000 * 1_000;
                return true;
            case "gb":
                bytes *= 1_000 * 1_000 * 1_000;
                return true;
            default:
                return false;
        }
    }
}
