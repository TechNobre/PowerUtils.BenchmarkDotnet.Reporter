using System.Collections.Generic;
using PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;
using PowerUtils.BenchmarkDotnet.Reporter.Common;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Exporters;

internal static class GateTableBuilder
{
    public static List<string> BuildHeader()
        => ["Type", "Method", "Mean", "Allocated"];

    public static List<string?> BuildRow(GateReport.Result result)
        =>
        [
            result.Type,
            result.Name,
            result.Mean.BeautifyTime(),
            result.Allocated.BeautifyMemory()
        ];
}
