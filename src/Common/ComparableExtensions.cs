using System;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common;

public static class ComparableExtensions
{
    public static bool EquivalentTo(this string? left, string? right)
    {
        if(left is null && right is null)
        {
            return true;
        }

        if(left is null || right is null)
        {
            return false;
        }

        return left.Equals(right, StringComparison.InvariantCultureIgnoreCase);
    }
}
