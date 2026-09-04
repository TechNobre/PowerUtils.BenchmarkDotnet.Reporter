using System.Collections.Generic;

namespace PowerUtils.BenchmarkDotnet.Reporter.Commands.Gate.Models;

public sealed class GateReport
{
    public List<string> Warnings { get; init; } = [];
    public List<Result> Results { get; init; } = [];
    public List<string> HitThresholds { get; init; } = [];


    public sealed class Result
    {
        public required string? Type { get; init; }
        public required string? Name { get; init; }
        public required string? FullName { get; init; }

        public decimal? Mean { get; init; }
        public decimal? Allocated { get; init; }
    }
}
