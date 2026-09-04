using System.Collections.Generic;
using System.Linq;
using MarkdownLog;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class MarkdownLogExtensions
{
    public static TableRow ToTableRow(this IEnumerable<string?> cells)
        => new()
        {
            Cells = cells.Select(text => new TableCell { Text = text }).ToList()
        };
}