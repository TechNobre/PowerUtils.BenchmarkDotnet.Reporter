# Test Data Documentation

This document provides detailed information about the test data samples available in the `tests/test-data/` directory of this repository.



## Overview

The test data directory contains various benchmark report samples that can be used to test and demonstrate the functionality of PowerUtils.BenchmarkDotNet.Reporter. These samples are organized in numbered folders and represent different benchmark scenarios and configurations.

> **💡 NOTE**: For best results with `PowerUtils.BenchmarkDotNet.Reporter`, always use **Full Benchmark JSON** files, typically named `*-report-full.json` or `*-report-full-compressed.json`. If files were renamed, the extension may differ, but what matters is that the content is the full JSON format exported by BenchmarkDotNet.



## File Structure

Each test data folder typically contains the following files:

| File Type | Description | Required for Tool |
|-----------|-------------|-------------------|
| `*-report-full.json` | Full JSON report with complete benchmark data | ✅ **Supported** |
| `*-report-full-compressed.json` | Compressed full JSON report | ✅ **Supported** |
| `*-report-brief.json` | Brief JSON report with summary data | ✅ **Supported** |
| `*-report-brief-compressed.json` | Compressed brief JSON report | ✅ **Supported** |
| `*-report-github.md` | GitHub-formatted markdown report | ❌ Not supported |
| `*-report.csv` | CSV format report | ❌ Not supported |
| `*-report.html` | HTML format report | ❌ Not supported |



## Available Test Data Sets

### 📊 `report-01` - Basic String Operations
- **File**: `Benchmark-report-full.json`
- **Environment**: Windows 11 (10.0.26100.3323)
- **Purpose**: Basic benchmark with string concatenation methods

| Method       | Mean      | Allocated |
|--------------|-----------|-----------|
| StringConcat | 15.555 ns | 48 B      |
| StringJoin   | 16.705 ns | 48 B      |

**Best for**: Getting started, basic comparisons, testing tool functionality

---

### 📊 `report-02` - Similar Operations (Minor Differences)
- **File**: `Benchmark-report-full.json`
- **Environment**: Windows 11 (10.0.26100.3323)
- **Purpose**: Similar benchmark with slightly different performance results

| Method       | Mean      | Allocated |
|--------------|-----------|-----------|
| StringConcat | 15.125 ns | 48 B      |
| StringJoin   | 15.873 ns | 48 B      |

**Best for**: Testing small performance differences, threshold testing

---

### 📊 `report-03` - Method Changes
- **File**: `Benchmark-report-full.json`
- **Environment**: Windows 11 (10.0.26100.3323)
- **Purpose**: Benchmark with added/removed methods

| Method       | Mean      | Allocated |
|--------------|-----------|-----------|
| StringJoin   | 15.873 ns | 48 B      |
| MethodTest   | 15.125 ns | 48 B      |

**Best for**: Testing scenarios with new/removed benchmark methods

---

### 📊 `report-04` - Significant Performance Differences
- **File**: `Benchmark-report-full.json`
- **Environment**: Windows 10 (10.0.26100.3323) ⚠️ *Different OS*
- **Purpose**: Dramatically different performance results

| Method       | Mean      | Allocated  |
|--------------|-----------|------------|
| StringConcat | 26.089 ms | 361.859 MB |
| StringJoin   | 13.691 ms | 361.859 MB |

**Best for**: Testing large performance regressions, threshold violations, different environments

---

### 📊 `report-05` - Equivalent reports with different file names
- **Files**:
  - `Baseline-report-full.json`
  - `Target-report-full.json`
- **Purpose**: Identical benchmark results with different file names

---

### 📊 `report-10` - Multi-Benchmark Classes (Baseline)
- **Files**:
  - `Demo.Benchmarks.ArrayProcessorBenchmarks-report-full.json`
  - `Demo.Benchmarks.StringProcessorBenchmarks-report-full.json`
- **Environment**: Windows 11 (10.0.26100.3323)
- **Purpose**: Multi-class benchmark with array and string processing operations

| Method         | Mean      | Allocated |
|----------------|-----------|-----------|
| GenerateArray  | 17.952 μs | 21.547 KB |
| GenerateString | 1.457 ms  | 33.313 MB |

**Best for**: Testing multi-benchmark scenarios, complex project structures, baseline for report-11

---

### 📊 `report-11` - Multi-Benchmark Classes (Comparison)
- **Files**:
  - `Demo.Benchmarks.ArrayProcessorBenchmarks-report-full.json`
  - `Demo.Benchmarks.StringProcessorBenchmarks-report-full.json`
- **Environment**: Windows 11 (10.0.26100.3323)
- **Purpose**: Similar multi-class benchmark with slight performance variations

| Method         | Mean      | Allocated |
|----------------|-----------|-----------|
| GenerateArray  | 17.946 μs | 21.563 KB |
| GenerateString | 1.45 ms   | 36.606 MB |

**Best for**: Testing complex comparisons, multi-file scenarios, memory allocation changes

---

### 📊 `report-12` - Multi-Benchmark Classes (Comparison)
- **Files**:
  - `Demo.Benchmarks.ArrayProcessorBenchmarks-report-full.json`
  - `Demo.Benchmarks.StringProcessorBenchmarks-report-full.json`
- **Environment**: Linux Ubuntu 24.04.2 LTS (Noble Numbat)

| Method         | Mean      | Allocated |
|----------------|-----------|-----------|
| GenerateArray  | 11.266 μs | 21.516 KB |
| GenerateString | 1.915 ms  | 35.19 MB  |

**Best for**: Testing decimal precision for $.Benchmarks[].Measurements[].Nanoseconds

---

### 📊 `report-20`, `report-21` - All Json formats
- **Files**:
  - `Demo.Benchmarks.ArrayProcessorBenchmarks-report-full.json`
  - `Demo.Benchmarks.ArrayProcessorBenchmarks-report-full-compressed.json`
  - `Demo.Benchmarks.ArrayProcessorBenchmarks-report-brief.json`
  - `Demo.Benchmarks.ArrayProcessorBenchmarks-report-brief-compressed.json`
  - `Demo.Benchmarks.StringProcessorBenchmarks-report-full.json`
  - `Demo.Benchmarks.StringProcessorBenchmarks-report-full-compressed.json`
  - `Demo.Benchmarks.StringProcessorBenchmarks-report-brief.json`
  - `Demo.Benchmarks.StringProcessorBenchmarks-report-brief-compressed.json`
- **Purpose**: Test handling of all JSON report formats (full, brief, compressed)


## Sample Comparisons

### 🔄 `report-01` vs `report-02` - Minor Performance Variations
- **Baseline**: `report-01`
- **Target**: `report-02`

**Expected Results**:
- **StringConcat**: Mean improvement of `-2.77%` (faster)
- **StringJoin**: Mean improvement of `-4.98%` (faster)

**Use Case**: Test detection of small performance improvements

---

### 🔄 `report-01` vs `report-03` - Method Changes
- **Baseline**: `report-01`
- **Target**: `report-03`

**Expected Results**:
- **StringConcat**: `REMOVED` ❌
- **StringJoin**: Mean improvement of `-4.98%` (faster)
- **MethodTest**: `NEW` ✅

**Use Case**: Test handling of added/removed benchmark methods

---

### 🔄 `report-01` vs `report-04` - Major Performance Regression
- **Baseline**: `report-01`
- **Target**: `report-04`

**Expected Results**:
- **StringConcat**:
  - Mean: `+167,721,298.63%` (major regression) 🚨
  - Allocated: `+790,492,641.67%` (major regression) 🚨
- **StringJoin**:
  - Mean: `+81,955,304.02%` (major regression) 🚨
  - Allocated: `+790,492,629.17%` (major regression) 🚨
- **Environment**: Different OS (Windows 11 → Windows 10)

**Use Case**: Test threshold violations, major performance regressions

---

### 🔄 `report-10` vs `report-11` - Multi-Benchmark Minor Changes
- **Baseline**: `report-10`
- **Target**: `report-11`

**Expected Results**:
- **GenerateArray**:
  - Mean: `-0.03%`
  - Allocated: `+0.08%`
- **GenerateString**:
  - Mean: `-0.48%`
  - Allocated: `+9.89%`
