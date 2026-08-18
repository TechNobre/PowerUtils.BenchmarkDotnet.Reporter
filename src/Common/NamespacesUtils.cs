using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class NamespacesUtils
{
    public const char WILDCARD = '*';

    public static IReadOnlyList<KeyValuePair<string, string>> Merge(params IReadOnlyList<KeyValuePair<string, string>>[] layers)
    {
        var merged = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        foreach(var layer in layers)
        {
            foreach(var rule in layer)
            {
                // Later layers take precedence, overriding any earlier rule with the same key.
                // The key is removed first to ensure the newly added entry appears last in the dictionary,
                // maintaining case-insensitive lookups while preserving the casing of the most recent key.
                merged.Remove(rule.Key);
                merged[rule.Key] = rule.Value;
            }
        }

        return merged
            .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value))
            .ToList();
    }

    public static bool IsValidPattern(string? pattern)
    {
        if(string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        var wildcardIndex = pattern.IndexOf(WILDCARD);
        return wildcardIndex == -1 || wildcardIndex == pattern.Length - 1;
    }

    public static bool IsMatch(string pattern, string? fullName)
    {
        if(fullName is null)
        {
            return false;
        }

        if(pattern[^1] != WILDCARD)
        {
            return pattern.EquivalentTo(fullName);
        }

        var prefix = pattern[..^1];

        return fullName.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);
    }

    public static int GetSpecificity(string pattern)
        => pattern[^1] == WILDCARD
            ? pattern.Length - 1
            : int.MaxValue;
}
