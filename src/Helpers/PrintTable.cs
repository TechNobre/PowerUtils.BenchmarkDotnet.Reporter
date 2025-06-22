using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerUtils.BenchmarkDotnet.Reporter.Helpers;

public sealed class TableBuilder
{
    public const int SPACE_BETWEEN_COLUMNS = 5;

    private string[]? _header;
    private readonly List<string[]> _rows = [];
    private readonly List<int> _columnWidths = [];

    private TableBuilder() { }
    public static TableBuilder Create() => new();



    public TableBuilder AddHeader(params IEnumerable<string> columns)
    {
        var totalColumns = columns.Count();
        if(totalColumns == 0)
        {
            return this;
        }

        if(_header is not null)
        {
            throw new InvalidOperationException("Header has already been added");
        }

        if(_rows.Count != 0)
        {
            throw new InvalidOperationException("Rows have already been added, cannot add header now");
        }

        _header = new string[totalColumns];

        var index = 0;
        foreach(var column in columns)
        {
            _header[index] = column;
            _setColumnWith(index, column.Length);

            index++;
        }

        return this;
    }

    public TableBuilder AddRow(params IEnumerable<string> columns)
    {
        var totalColumns = columns.Count();
        if(totalColumns == 0)
        {
            return this;
        }

        if(_columnWidths.Count > 0 && _columnWidths.Count != totalColumns)
        {
            throw new InvalidOperationException("Cannot add row with a different number of columns than already defined before");
        }

        var row = new string[totalColumns];

        var index = 0;
        foreach(var column in columns)
        {
            row[index] = column;
            _setColumnWith(index, column.Length);

            index++;
        }

        _rows.Add(row);

        return this;
    }


    public string[][] Build()
    {
        if(_rows.Count == 0)
        {
            return [];
        }

        var numberOfHeaderRows = 0;
        if(_header is not null)
        {
            numberOfHeaderRows = 2; // One for the header and one for the separator
        }

        var rowsCount = _rows.Count + numberOfHeaderRows; // Add one for the header if it exists
        var columnsCount = _columnWidths.Count;

        var rows = new string[rowsCount][];

        if(numberOfHeaderRows > 0)
        {
            rows[0] = new string[columnsCount]; // First row is the header
            rows[1] = new string[columnsCount]; // Second row is the separator
            for(var i = 0; i < columnsCount; i++)
            {
                var maxWidth
                    = _columnWidths[i]
                    + (i + 1 < columnsCount ? SPACE_BETWEEN_COLUMNS : 0); // Add space between columns except for the last one

                rows[0][i] = _header![i].PadRight(maxWidth);
                rows[1][i] = new string('─', maxWidth);
            }
        }

        for(var i = 0; i < _rows.Count; i++)
        {
            var columns = new string[columnsCount];
            for(var j = 0; j < columnsCount; j++)
            {
                var maxWidth
                    = _columnWidths[j]
                    + (j + 1 < columnsCount ? SPACE_BETWEEN_COLUMNS : 0); // Add space between columns except for the last one

                columns[j] = $"{_rows[i][j]?.PadRight(maxWidth)}";
            }

            rows[i + numberOfHeaderRows] = columns;
        }

        return rows;
    }


    private void _setColumnWith(int columnIndex, int width)
    {
        if(_columnWidths.Count <= columnIndex)
        {
            _columnWidths.Add(width);
        }
        else
        {
            _columnWidths[columnIndex] = Math.Max(_columnWidths[columnIndex], width);
        }
    }
}
