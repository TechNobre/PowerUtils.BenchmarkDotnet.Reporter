using System;
using System.Collections.Generic;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Compare.Exporters;

internal static class ExporterFormats
{
    public const string CONSOLE = "console";
    public const string MARKDOWN = "markdown";
    public const string JSON = "json";
    public const string HIT_TXT = "hit-txt";

    public static readonly HashSet<string> All = new([CONSOLE, MARKDOWN, JSON, HIT_TXT], StringComparer.OrdinalIgnoreCase);
}
