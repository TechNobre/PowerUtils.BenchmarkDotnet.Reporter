using System;
using System.Collections.Generic;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration;

// Parses a narrow subset of YAML: nested mappings, sequences of scalars or mappings, quoted/unquoted
// scalars, and flow-style scalar sequences ([a, b, c]). No anchors, tags, flow-style mappings ({}),
// flow sequences of non-scalars, multi-line scalars, or multi-document files. Sufficient for this
// tool's flat/one-level-nested configuration schema.
public static class YamlDocumentParser
{
    // Splitting on '\n' alone is sufficient for CRLF too: a stray trailing '\r' is whitespace and
    // gets removed by _tokenize's TrimEnd() below.
    private static readonly string[] _lineBreaks = ["\n"];

    public static Dictionary<string, object?> Parse(string text)
    {
        var lines = _tokenize(text);

        if(lines.Count == 0)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        var index = 0;
        var node = _parseBlock(lines, ref index, lines[0].Indent);

        if(node is not Dictionary<string, object?> mapping)
        {
            throw new DomainException("The YAML document root must be a mapping.");
        }

        return mapping;
    }


    // Callers only invoke this once they've confirmed index < lines.Count and lines[index].Indent == indent
    // (that's exactly how the value to parse next is identified), so no bounds/indent guard is needed here.
    private static object? _parseBlock(List<_Line> lines, ref int index, int indent)
        => _isSequenceItem(lines[index].Content)
            ? _parseSequence(lines, ref index, indent)
            : _parseMapping(lines, ref index, indent);

    private static Dictionary<string, object?> _parseMapping(List<_Line> lines, ref int index, int indent)
    {
        var mapping = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        while(index < lines.Count && lines[index].Indent == indent && !_isSequenceItem(lines[index].Content))
        {
            var line = lines[index];
            var separatorIndex = line.Content.IndexOf(':');
            if(separatorIndex == -1)
            {
                throw new DomainException($"Expected 'key: value' at line {line.LineNumber}: '{line.Content}'.");
            }

            var key = line.Content[..separatorIndex].Trim();
            var rawValue = line.Content[(separatorIndex + 1)..].Trim();
            index++;

            mapping[key] = rawValue.Length > 0
                ? (rawValue.StartsWith('[') && rawValue.EndsWith(']')
                    ? _parseFlowSequence(rawValue)
                    : _unquote(rawValue))
                : (index < lines.Count && lines[index].Indent > indent
                    ? _parseBlock(lines, ref index, lines[index].Indent)
                    : null);
        }

        return mapping;
    }

    private static List<object?> _parseSequence(List<_Line> lines, ref int index, int indent)
    {
        var sequence = new List<object?>();
        var itemIndent = indent + 2;

        while(index < lines.Count && lines[index].Indent == indent && _isSequenceItem(lines[index].Content))
        {
            var line = lines[index];
            var itemContent = line.Content.Length > 1 ? line.Content[2..].Trim() : string.Empty;

            if(itemContent.Length == 0)
            {
                index++;
                sequence.Add(index < lines.Count && lines[index].Indent >= itemIndent
                    ? _parseBlock(lines, ref index, lines[index].Indent)
                    : null);
                continue;
            }

            var separatorIndex = itemContent.IndexOf(':');
            if(separatorIndex == -1)
            {
                sequence.Add(_unquote(itemContent));
                index++;
                continue;
            }

            var key = itemContent[..separatorIndex].Trim();
            var rawValue = itemContent[(separatorIndex + 1)..].Trim();
            index++;

            var mapping = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                [key] = rawValue.Length > 0
                    ? _unquote(rawValue)
                    : (index < lines.Count && lines[index].Indent > itemIndent
                        ? _parseBlock(lines, ref index, lines[index].Indent)
                        : null)
            };

            if(index < lines.Count && lines[index].Indent == itemIndent && !_isSequenceItem(lines[index].Content))
            {
                foreach(var pair in _parseMapping(lines, ref index, itemIndent))
                {
                    mapping[pair.Key] = pair.Value;
                }
            }

            sequence.Add(mapping);
        }

        return sequence;
    }

    private static List<object?> _parseFlowSequence(string value)
    {
        var content = value[1..^1];
        var result = new List<object?>();

        foreach(var item in content.Split(','))
        {
            var trimmed = item.Trim();
            if(trimmed.Length > 0)
            {
                result.Add(_unquote(trimmed));
            }
        }

        return result;
    }

    private static bool _isSequenceItem(string content)
        => content == "-" || content.StartsWith("- ", StringComparison.Ordinal);

    private static string _unquote(string value)
        => value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''))
            ? value[1..^1]
            : value;

    private static List<_Line> _tokenize(string text)
    {
        var result = new List<_Line>();
        var rawLines = text.Split(_lineBreaks, StringSplitOptions.None);

        for(var i = 0; i < rawLines.Length; i++)
        {
            var raw = rawLines[i];

            if(raw.Contains('\t'))
            {
                throw new DomainException($"Tabs are not allowed for indentation at line {i + 1}: '{raw}'.");
            }

            var trimmed = raw.TrimStart(' ');
            var indent = raw.Length - trimmed.Length;
            trimmed = trimmed.TrimEnd();

            if(trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            result.Add(new _Line(indent, trimmed, i + 1));
        }

        return result;
    }


    private sealed record _Line(int Indent, string Content, int LineNumber);
}
