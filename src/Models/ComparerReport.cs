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
        public string? Name { get; init; }
        public string? FullName { get; init; }


        public MetricComparison? Mean { get; set; }
        public MetricComparison? Allocated { get; set; }
    }
}
