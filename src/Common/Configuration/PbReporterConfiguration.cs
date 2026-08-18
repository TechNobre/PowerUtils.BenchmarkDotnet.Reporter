using System.Collections.Generic;

namespace PowerUtils.BenchmarkDotnet.Reporter.Common.Configuration;

public sealed class PbReporterConfiguration
{
    public CompareConfigurationSection? Compare { get; set; }


    public sealed class CompareConfigurationSection
    {
        public string? Baseline { get; set; }
        public string? Target { get; set; }
        public List<string>? Formats { get; set; }
        public string? ThresholdMean { get; set; }
        public string? ThresholdAllocation { get; set; }
        public List<ScopedThresholdConfig>? Thresholds { get; set; }


        public sealed class ScopedThresholdConfig
        {
            public string? Pattern { get; set; }
            public string? ThresholdMean { get; set; }
            public string? ThresholdAllocation { get; set; }
        }
    }
}
