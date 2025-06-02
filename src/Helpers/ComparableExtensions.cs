using System;

namespace PowerUtils.BenchmarkDotnet.Reporter.Helpers;

public static class ComparableExtensions
{
    public static bool Equivalente(this string? left, string? right)
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
