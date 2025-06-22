namespace PowerUtils.BenchmarkDotnet.Reporter.Models;

public sealed record MetricComparison
{
    public const string DEFAULT_UNIT_EXECUTION_TIME = "ns";
    public const string DEFAULT_UNIT_MEMORY_USAGE = "B";


    public string? Unit { get; init; }
    public decimal? Baseline { get; private set; }
    public decimal? Target { get; private set; }
    public decimal? Diff { get; private set; }
    public decimal? DiffPercentage { get; private set; }
    public ComparisonStatus Status { get; private set; }

    private MetricComparison() { }


    public static MetricComparison? CalculateExecutionTime(decimal? baseline, decimal? target)
        => _calculate(baseline, target, DEFAULT_UNIT_EXECUTION_TIME);

    public static MetricComparison? CalculateMemoryUsage(decimal? baseline, decimal? target)
        => _calculate(baseline, target, DEFAULT_UNIT_MEMORY_USAGE);

    public static MetricComparison? CalculateGarbageCollectionOperations(decimal? baselineIteration, decimal? baselineOperations, decimal? targetIteration, decimal? targetOperations)
    {
        if(
            baselineIteration is not null &&
            baselineIteration > 0 &&
            baselineOperations is not null &&
            baselineOperations > 0)
        {
            baselineIteration = baselineIteration * 1_000 / baselineOperations;
        }
        else
        {
            baselineIteration = null;
        }

        if(
            targetIteration is not null &&
            targetIteration > 0 &&
            targetOperations is not null &&
            targetOperations > 0)
        {
            targetIteration = targetIteration * 1_000 / targetOperations;
        }
        else
        {
            targetIteration = null;
        }

        return _calculate(baselineIteration, targetIteration, null);
    }

    private static MetricComparison? _calculate(decimal? baseline, decimal? target, string? unit)
    {
        if(baseline is null && target is null)
        {
            return null;
        }

        var comparison = new MetricComparison
        {
            Unit = unit,
            Baseline = baseline,
            Target = target
        };


        if(baseline is not null && target is not null)
        {
            // When there is a baseline and target, we calculate the difference and percentage
            comparison.Diff = target - baseline;
            if(baseline != 0)
            {
                var percentage = comparison.Diff / baseline;
                comparison.DiffPercentage = (decimal?)((double)percentage * 100);
            }

            if(comparison.Diff == 0)
            {
                // The baseline and target are equal
                comparison.Status = ComparisonStatus.Equal;
            }
            else if(comparison.Diff > 0)
            {
                // The target is worse than the baseline
                comparison.Status = ComparisonStatus.Worse;
            }
            else
            {
                // The target is better than the baseline
                comparison.Status = ComparisonStatus.Better;
            }
        }
        else if(baseline is null)
        {
            // When there is no baseline, we consider the target as new
            comparison.Status = ComparisonStatus.New;
        }
        else
        {
            // When there is no target, we consider the target as removed
            comparison.Status = ComparisonStatus.Removed;
        }


        return comparison;
    }
}
