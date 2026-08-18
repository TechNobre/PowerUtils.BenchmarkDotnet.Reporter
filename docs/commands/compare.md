# `compare` Command Reference

[← Back to README](../../README.md)

Compares two benchmark reports and generates a report with the differences.

**Example:**
```bash
pbreporter compare -b baseline-full.json -t target-full.json
```

- [Options](#options)
  - [Threshold Units](#threshold-units)
  - [Scoped Thresholds](#scoped-thresholds)
- [Example of usage](#example-of-usage)
- [Output Metrics](#output-metrics)
- [Error Handling Options](#error-handling-options)
  - [Exit Codes](#exit-codes)



## Options

* (`-b`, `--baseline`) `<baseline>`: Path to the folder or file with Baseline report. **[Required, unless set via env var or config file]**
* (`-t`, `--target`) `<target>`: Path to the folder or file with target reports. **[Required, unless set via env var or config file]**
* (`-tm`, `--threshold-mean`) `<threshold-mean>`: Throw an error when the mean threshold is met. Examples: 5%, 10ms, 10us, 100ns, 1s. Repeatable. See [Scoped Thresholds](#scoped-thresholds).
* (`-ta`, `--threshold-allocation`) `<threshold-allocation>`: Throw an error when the allocation threshold is met. Examples: 5%, 10b, 10kb, 100mb, 1gb. Repeatable. See [Scoped Thresholds](#scoped-thresholds).
* (`-f`, `--format`) `<console|hit-txt|json|markdown>`: Output format for the report. Repeatable. Can also be set via the `PBREPORTER_COMPARE__FORMATS` environment variable (single value) or the `formats` key in the YAML config file (scalar or list: `formats: [json, markdown]` or block-style `- json`). **[default: console]**
* (`-o`, `--output`) `<output>`: Output directory to export the diff report. Default is current directory. **[default: ./BenchmarkReporter]**
* (`-fw`, `--fail-on-warnings`): Exit with error code when any warnings are generated during comparison (e.g., mismatched host environments). **[default: disabled]**
* (`-ft`, `--fail-on-threshold-hit`): Exit with error code when any threshold is hit during comparison. **[default: disabled]**
* (`-c`, `--config`): Path to a YAML configuration file. Works on any command (not `compare`-specific). Defaults to `pbreporter.yml` or `pbreporter.yaml` in the current directory when present. See [Configuration](../configuration.md).
* (`-?`, `-h`, `--help`): Show help and usage information

> Every option above (including `-b`/`-t`) can also be set via an environment variable or the YAML config file instead of a CLI argument. See [Configuration](../configuration.md) for the full naming convention and precedence order.

### Threshold Units

**Time (`-tm`):**

| Unit | Description | Example |
|------|-------------|---------|
| `ns` | Nanoseconds | `100ns` |
| `us` | Microseconds | `10us` |
| `ms` | Milliseconds | `10ms` |
| `s` | Seconds | `1s` |
| `%` | Percentage relative to baseline | `5%` |

**Memory (`-ta`):**

| Unit | Description | Example |
|------|-------------|---------|
| `b` | Bytes | `10b` |
| `kb` | Kilobytes | `10kb` |
| `mb` | Megabytes | `10mb` |
| `gb` | Gigabytes | `1gb` |
| `%` | Percentage relative to baseline | `5%` |

### Scoped Thresholds

`-tm`/`--threshold-mean` and `-ta`/`--threshold-allocation` can be repeated to apply different thresholds to different benchmarks, instead of one global value for every benchmark. Each occurrence is a token in one of two forms:
* `<value>`: a bare value (e.g. `5%`) applies to **every** benchmark. This is exactly the same as writing `*=5%`.
* `<pattern>=<value>`: applies only to benchmarks whose full name (`Namespace.Type.Method`) matches `<pattern>`.

A pattern is either an exact full name (e.g. `DemoApi.Controllers.CreateController.Create`), or a prefix ending in `*` (e.g. `DemoApi.Controllers.CreateController.*` for every method in that class, or `DemoApi.*` for everything under that namespace). `*` is only allowed as the very last character of a pattern.

When more than one rule matches the same benchmark, the **most specific** one wins: an exact match beats any wildcard, and among wildcards the one with the longer literal prefix wins. This lets you set a loose default and tighten it for specific namespaces, classes, or methods:

```bash
pbreporter compare -b baseline-full.json -t target-full.json \
  -tm 5% \
  -tm "DemoApi.Controllers.*=10ms" \
  -tm "DemoApi.Controllers.CreateController.Create=2ms"
```

Here, `Create` is checked against `2ms`, every other method on `CreateController` (and any other controller) is checked against `10ms`, and everything else in the report falls back to the global `5%`. If no rule at all matches a benchmark (no bare value and no matching pattern), that benchmark isn't checked against a threshold.

Scoped thresholds can also be set via environment variables or the YAML config file. See
[Configuration](../configuration.md).



## Example of usage

**Simple usage**
```bash
pbreporter compare -b baseline-full.json -t target-full.json
```

**Passing folder paths**
```bash
pbreporter compare -b ./baseline-reports -t ./target-reports
```
> Note: You can pass a file path, a folder or mix both. The tool will automatically find the supported report files in the provided paths.

**With output format and directory**
```bash
pbreporter compare -b baseline-full.json -t target-full.json -f json -f markdown -o ./out
```

**With thresholds**
```bash
pbreporter compare -b baseline-full.json -t target-full.json -tm 5% -ta 12b
```

**With thresholds and output threshold report**
```bash
pbreporter compare -b baseline-full.json -t target-full.json -tm 5% -f hit-txt
```
> Note: The `hit-txt` format will only generate when at least one threshold is hit.

**With scoped thresholds**
```bash
pbreporter compare -b baseline-full.json -t target-full.json \
  -tm 5% -tm "DemoApi.Controllers.CreateController.*=1ms" -ft
```
> Note: See [Scoped Thresholds](#scoped-thresholds) for the `pattern=value` syntax and how the most specific matching rule is chosen.

**With console output**
```bash
pbreporter compare -b baseline-full.json -t target-full.json -f console
```
> Note: The `console` format displays the comparison report directly in the terminal instead of creating a file.

**With Markdown output**
```bash
pbreporter compare -b baseline-full.json -t target-full.json -f markdown
```
> Note: The `markdown` format is ideal for generating reports to upload to GitHub or other platforms that support Markdown rendering.

**With multiple formats**
```bash
pbreporter compare -b baseline-full.json -t target-full.json -f json -f markdown -f console
```

**With no CLI paths, using a config file**. See [Configuration](../configuration.md) for the full example.
```bash
pbreporter compare -ft
```



## Output Metrics

The comparison report includes the following metrics for each benchmark:

| Metric | Description |
|--------|-------------|
| **Mean** | Mean execution time per operation (baseline/target), scaled for display; % change shown when available |
| **Gen0** | Gen0 collections per 1,000 operations (baseline/target; % change shown when available) |
| **Gen1** | Gen1 collections per 1,000 operations (baseline/target; % change shown when available) |
| **Gen2** | Gen2 collections per 1,000 operations (baseline/target; % change shown when available) |
| **Allocated** | Bytes allocated per operation (baseline/target), scaled for display; % change shown when available |

> Note: GC collection columns (Gen0, Gen1, Gen2) are only shown when at least one benchmark in the report has non-zero GC data. The `json` format always includes `Gen0Collections`, `Gen1Collections`, and `Gen2Collections` fields in each comparison object.



## Error Handling Options

The tool provides options to control exit codes for CI/CD integration and automated quality gates.

**Fail on warnings**
```bash
pbreporter compare -b baseline-full.json -t target-full.json -fw
```
> Note: Exits with code 2 if any warnings are generated during comparison (e.g., environment differences between baseline and target).

**Fail on threshold hits**
```bash
pbreporter compare -b baseline-full.json -t target-full.json -tm 5% -ta 10% -ft
```
> Note: Exits with code 3 if any performance thresholds are exceeded during comparison.

**Both error handling options**
```bash
pbreporter compare -b baseline-full.json -t target-full.json -tm 5% -fw -ft
```
> Note: If both conditions are met, warnings take priority and the tool exits with code 2.

### Exit Codes

* **0**: Success - No issues detected
* **1**: Generic error (invalid configuration, missing files, invalid threshold values, or other user-triggered failures)
* **2**: Warnings detected (when `--fail-on-warnings` is enabled)
* **3**: Performance thresholds exceeded (when `--fail-on-threshold-hit` is enabled)
