using System;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class BeautifyExtensions
{
    public static string BeautifyTime(this decimal? time)
        => time is null
            ? ""
            : time.Value.BeautifyTime();

    public static string BeautifyTime(this decimal time)
    {
        var (value, unit) = _scale(time, 1000, ["ns", "μs", "ms", "s"]);

        if(unit == "s" && value >= 60)
        {
            var minutes = Math.Floor(value / 60);
            var seconds = value % 60;

            return seconds == 0
                ? $"{minutes}m"
                : $"{minutes}m {seconds:N0}s";
        }

        return $"{_trimTrailingZeros(value, "N3")} {unit}";
    }

    public static string BeautifyMemory(this decimal? memory)
        => memory is null
            ? ""
            : memory.Value.BeautifyMemory();

    public static string BeautifyMemory(this decimal memory)
    {
        var (value, unit) = _scale(memory, 1024, ["B", "KB", "MB", "GB", "TB"]);

        return $"{_trimTrailingZeros(value, "N3")} {unit}";
    }

    public static string BeautifyPercentage(this decimal? percentage)
        => percentage is null
            ? ""
            : percentage.Value.BeautifyPercentage();

    public static string BeautifyPercentage(this decimal percentage)
        => $"{_trimTrailingZeros(percentage, "N2")}%";


    // Steps the value up through `units` (in order) by dividing by `divisor` while it stays above it
    private static (decimal Value, string Unit) _scale(decimal value, decimal divisor, string[] units)
    {
        var unitIndex = 0;
        while(value >= divisor && unitIndex < units.Length - 1)
        {
            value /= divisor;
            unitIndex++;
        }

        return (value, units[unitIndex]);
    }

    private static string _trimTrailingZeros(decimal value, string format)
        => value.ToString(format).TrimEnd('0').TrimEnd('.');
}
