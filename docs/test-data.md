# Test Data Documentation

This document provides detailed information about the test data samples available in the `tests/test-data/` directory of this repository.



## Overview

The test data directory contains various benchmark report samples that can be used to test and demonstrate the functionality of PowerUtils.BenchmarkDotNet.Reporter. These samples are organized in numbered folders and represent different benchmark scenarios and configurations.

> **💡 Important**: Always use the `*-full.json` files when working with PowerUtils.BenchmarkDotNet.Reporter, as this tool requires the full JSON format exported by BenchmarkDotNet.



## File Structure

Each test data folder typically contains the following files:

| File Type | Description | Required for Tool |
|-----------|-------------|-------------------|
| `*-report-full.json` | Full JSON report with complete benchmark data | ✅ **Required** |
| `*-report-github.md` | GitHub-formatted markdown report | ❌ Reference only |
| `*-report.csv` | CSV format report | ❌ Reference only |
| `*-report.html` | HTML format report | ❌ Reference only |

> **⚠️ Note**: PowerUtils.BenchmarkDotNet.Reporter only works with the full JSON format (`*-full.json` files).



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
