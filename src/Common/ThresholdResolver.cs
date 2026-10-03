using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class ThresholdResolver
{
    public readonly record struct ResolvedThreshold(decimal Value, bool IsPercentage, string Pattern);

    // Parse eagerly so malformed threshold syntax fails fast, even when no comparison matches it.
    // Pre-sort by specificity once — pattern specificity is constant across comparisons.
    public static List<ResolvedThreshold> Resolve(
        IReadOnlyList<KeyValuePair<string, string>> rules,
        Func<string, (decimal Value, bool IsPercentage)> parse)
        => rules
            .Select(rule =>
            {
                var (value, isPercentage) = parse(rule.Value);
                return new ResolvedThreshold(value, isPercentage, rule.Key);
            })
            .OrderByDescending(rule => NamespacesUtils.GetSpecificity(rule.Pattern))
            .ToList();

    public static ResolvedThreshold? FindMatch(IReadOnlyList<ResolvedThreshold> sortedResolved, string? fullName)
        => sortedResolved
            .Where(rule => NamespacesUtils.IsMatch(rule.Pattern, fullName))
            .Select(rule => (ResolvedThreshold?)rule)
            .FirstOrDefault();
}
