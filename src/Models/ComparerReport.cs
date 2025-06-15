using System.Collections.Generic;

namespace PowerUtils.BenchmarkDotnet.Reporter.Models;

public sealed class ComparerReport
{
    public List<string> Warnings { get; init; } = [];
    public List<Comparison> Comparisons { get; private set; } = [];
    public List<string> HitThresholds { get; init; } = [];

    public void Add(Comparison comparison)
    {
        if(comparison.Mean is null && comparison.Allocated is null)
        {
            return;
        }

        Comparisons.Add(comparison);
    }


    public sealed class Comparison
    {
        public required string? Type { get; init; }
        public required string? Name { get; init; }
        public required string? FullName { get; init; }


        public MetricComparison? Mean { get; set; }
        public MetricComparison? Allocated { get; set; }
    }
}
