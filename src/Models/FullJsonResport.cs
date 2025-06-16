// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace PowerUtils.BenchmarkDotnet.Reporter.Models;
public sealed class BenchmarkFullJsonResport
{ // Generated with https://json2csharp.com/
    public string? FilePath { get; set; }
    public string? FileName { get; set; }

    public string? Title { get; set; }

    public HostEnvironmentInfoRecord? HostEnvironmentInfo { get; set; }
    public List<BenchmarkRecord>? Benchmarks { get; set; }



    public sealed class HostEnvironmentInfoRecord
    {
        public string? BenchmarkDotNetCaption { get; set; }
        public string? BenchmarkDotNetVersion { get; set; }
        public string? OsVersion { get; set; }
        public string? ProcessorName { get; set; }
        public int? PhysicalProcessorCount { get; set; }
        public int? PhysicalCoreCount { get; set; }
        public int? LogicalCoreCount { get; set; }
        public string? RuntimeVersion { get; set; }
        public string? Architecture { get; set; }
        public bool? HasAttachedDebugger { get; set; }
        public bool? HasRyuJit { get; set; }
        public string? Configuration { get; set; }
        public string? JitModules { get; set; }
        public string? DotNetCliVersion { get; set; }
        public ChronometerFrequencyRecord? ChronometerFrequency { get; set; }
        public string? HardwareTimerKind { get; set; }


        public sealed class ChronometerFrequencyRecord
        {
            public int Hertz { get; set; }
        }
    }

    public sealed class BenchmarkRecord
    {
        public string? DisplayInfo { get; set; }
        public string? Namespace { get; set; }
        public string? Type { get; set; }
        public string? Method { get; set; }
        public string? MethodTitle { get; set; }
        public string? Parameters { get; set; }
        public string? FullName { get; set; }
        public string? HardwareIntrinsics { get; set; }
        public StatisticsRecord? Statistics { get; set; }
        public MemoryRecord? Memory { get; set; }
        public List<MeasurementRecord>? Measurements { get; set; }
        public List<MetricRecord>? Metrics { get; set; }



        public sealed class StatisticsRecord
        {
            public List<decimal>? OriginalValues { get; set; }
            public int N { get; set; }
            public decimal Min { get; set; }
            public decimal LowerFence { get; set; }
            public decimal Q1 { get; set; }
            public decimal Median { get; set; }
            public decimal Mean { get; set; }
            public decimal Q3 { get; set; }
            public decimal UpperFence { get; set; }
            public decimal Max { get; set; }
            public decimal InterquartileRange { get; set; }
            public List<decimal>? LowerOutliers { get; set; }
            public List<decimal>? UpperOutliers { get; set; }
            public List<decimal>? AllOutliers { get; set; }
            public decimal StandardError { get; set; }
            public decimal Variance { get; set; }
            public decimal StandardDeviation { get; set; }
            public decimal Skewness { get; set; }
            public decimal Kurtosis { get; set; }
            public ConfidenceIntervalRecord? ConfidenceInterval { get; set; }
            public PercentilesRecord? Percentiles { get; set; }



            public sealed class ConfidenceIntervalRecord
            {
                public int N { get; set; }
                public decimal Mean { get; set; }
                public decimal StandardError { get; set; }
                public int Level { get; set; }
                public decimal Margin { get; set; }
                public decimal Lower { get; set; }
                public decimal Upper { get; set; }
            }

            public sealed class PercentilesRecord
            {
                public decimal P0 { get; set; }
                public decimal P25 { get; set; }
                public decimal P50 { get; set; }
                public decimal P67 { get; set; }
                public decimal P80 { get; set; }
                public decimal P85 { get; set; }
                public decimal P90 { get; set; }
                public decimal P95 { get; set; }
                public decimal P100 { get; set; }
            }
        }

        public sealed class MemoryRecord
        {
            public int Gen0Collections { get; set; }
            public int Gen1Collections { get; set; }
            public int Gen2Collections { get; set; }
            public int TotalOperations { get; set; }
            public int BytesAllocatedPerOperation { get; set; }
        }

        public sealed class MeasurementRecord
        {
            public string? IterationMode { get; set; }
            public string? IterationStage { get; set; }
            public int LaunchIndex { get; set; }
            public int IterationIndex { get; set; }
            public int Operations { get; set; }
            public decimal Nanoseconds { get; set; }
        }

        public sealed class MetricRecord
        {
            public decimal Value { get; set; }
            public DescriptorRecord? Descriptor { get; set; }



            public sealed class DescriptorRecord
            {
                public string? Id { get; set; }
                public string? DisplayName { get; set; }
                public string? Legend { get; set; }
                public string? NumberFormat { get; set; }
                public int UnitType { get; set; }
                public string? Unit { get; set; }
                public bool TheGreaterTheBetter { get; set; }
                public int PriorityInCategory { get; set; }
            }
        }
    }
}
