using System;

namespace PowerUtils.BenchmarkDotnet.Reporter.Helpers;

public static class BeautifyExtentions
{
    public static string BeautifyTime(this decimal? time)
        => time is null
            ? ""
            : time.Value.BeautifyTime();

    public static string BeautifyTime(this decimal time)
    {
        var unit = "ns";
        if(time >= 1000)
        {
            time /= 1000;
            unit = "μs";

            if(time >= 1000)
            {
                time /= 1000;
                unit = "ms";

                if(time >= 1000)
                {
                    time /= 1000;
                    unit = "s";


                    if(time >= 60)
                    {
                        var minutes = Math.Floor(time / 60);
                        var seconds = time % 60;

                        return seconds == 0
                            ? $"{minutes}m"
                            : $"{minutes}m {seconds:N0}s";
                    }
                }
            }
        }

        // Format with max 3 decimal places by removing trailing zeros
        var formattedTime = time.ToString("N3").TrimEnd('0').TrimEnd('.');
        return $"{formattedTime} {unit}";
    }

    public static string BeautifyMemory(this decimal? memory)
        => memory is null
            ? ""
            : memory.Value.BeautifyMemory();

    public static string BeautifyMemory(this decimal memory)
    {
        var unit = "B";

        if(memory >= 1024)
        {
            memory /= 1024;
            unit = "KB";

            if(memory >= 1024)
            {
                memory /= 1024;
                unit = "MB";

                if(memory >= 1024)
                {
                    memory /= 1024;
                    unit = "GB";

                    if(memory >= 1024)
                    {
                        memory /= 1024;
                        unit = "TB";
                    }
                }
            }
        }

        // Format with max 3 decimal places by removing trailing zeros
        var formattedMemory = memory.ToString("N3").TrimEnd('0').TrimEnd('.');
        return $"{formattedMemory} {unit}";
    }

    public static string BeautifyPercentage(this decimal? percentage)
        => percentage is null
            ? ""
            : percentage.Value.BeautifyPercentage();

    public static string BeautifyPercentage(this decimal percentage)
    {
        // Format with max 2 decimal places by removing trailing zeros
        var formattedPercentage = percentage.ToString("N2").TrimEnd('0').TrimEnd('.');
        return $"{formattedPercentage}%";
    }
}
