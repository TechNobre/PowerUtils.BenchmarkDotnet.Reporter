# `gate` Command Reference

[← Back to README](../../README.md)

Checks a BenchmarkDotNet report against absolute performance thresholds and fails the run when any is exceeded.

**Example:**
```bash
pbreporter gate -i benchmark-report.json -tm 500ms -ta 10kb
```

- [Options](#options)
  - [Scoped Thresholds](#scoped-thresholds)
- [Example of usage](#example-of-usage)
- [Output Metrics](#output-metrics)
- [Error Handling Options](#error-handling-options)



## Options

* (`-i`, `--input`) `<input>`: Path to the folder or file with the report(s) to check. **[Required, unless set via env var or config file]**
* (`-tm`, `--threshold-mean`) `<threshold-mean>`: Fail when a benchmark's mean execution time exceeds this absolute value. Examples: 10ms, 10us, 100ns, 1s. Repeatable. See [Scoped Thresholds](#scoped-thresholds).
* (`-ta`, `--threshold-allocation`) `<threshold-allocation>`: Fail when a benchmark's allocated memory exceeds this absolute value. Examples: 10b, 10kb, 100mb, 1gb. Repeatable. See [Scoped Thresholds](#scoped-thresholds).
* (`-f`, `--format`) `<console|hit-txt|json|markdown>`: Output format for the report. Repeatable. Can also be set via the `PBREPORTER_GATE__FORMATS` environment variable (single value) or the `formats` key in the YAML config file (scalar or list: `formats: [json, markdown]` or block-style `- json`). **[default: console]**
* (`-o`, `--output`) `<output>`: Output directory to export the gate report. Default is current directory. **[default: ./BenchmarkReporter]**
* (`-fw`, `--fail-on-warnings`): Exit with error code when any warnings are generated during the check (e.g., the report wasn't built in RELEASE mode). **[default: disabled]**
* (`-c`, `--config`): Path to a YAML configuration file. Works on any command (not `gate`-specific). Defaults to `pbreporter.yml` or `pbreporter.yaml` in the current directory when present. See [Configuration](../configuration.md).
* (`-?`, `-h`, `--help`): Show help and usage information

> Every option above (including `-i`) can also be set via an environment variable or the YAML config file instead of a CLI argument. See [Configuration](../configuration.md) for the full naming convention and precedence order.

See [Threshold Units](../threshold-units.md#threshold-units) for supported threshold units. `gate` accepts absolute time and memory values only.

### Scoped Thresholds

`-tm`/`--threshold-mean` and `-ta`/`--threshold-allocation` can be repeated to apply different thresholds to different benchmarks, instead of one global value for every benchmark. Each occurrence is a token in one of two forms:
* `<value>`: a bare value (e.g. `500ms`) applies to **every** benchmark. This is exactly the same as writing `*=500ms`.
* `<pattern>=<value>`: applies only to benchmarks whose full name (`Namespace.Type.Method`) matches `<pattern>`.

A pattern is either an exact full name (e.g. `DemoApi.Controllers.CreateController.Create`), or a prefix ending in `*` (e.g. `DemoApi.Controllers.CreateController.*` for every method in that class, or `DemoApi.*` for everything under that namespace). `*` is only allowed as the very last character of a pattern.

When more than one rule matches the same benchmark, the **most specific** one wins: an exact match beats any wildcard, and among wildcards the one with the longer literal prefix wins. This lets you set a loose default and tighten it for specific namespaces, classes, or methods:

```bash
pbreporter gate -i benchmark-report.json \
  -tm 500ms \
  -tm "DemoApi.Controllers.*=100ms" \
  -tm "DemoApi.Controllers.CreateController.Create=20ms"
```

Here, `Create` is checked against `20ms`, every other method on `CreateController` (and any other controller) is checked against `100ms`, and everything else in the report falls back to the global `500ms`. If no rule at all matches a benchmark (no bare value and no matching pattern), that benchmark isn't checked against a threshold.

Scoped thresholds can also be set via environment variables or the YAML config file. See
[Configuration](../configuration.md).



## Example of usage

**Simple usage**
```bash
pbreporter gate -i benchmark-report.json -tm 500ms -ta 10kb
```

**Passing a folder path**
```bash
pbreporter gate -i ./benchmark-reports -tm 500ms -ta 10kb
```
> Note: You can pass a file path or a folder. The tool will automatically find the supported report files in the provided path.

**With output format and directory**
```bash
pbreporter gate -i benchmark-report.json -tm 500ms -f json -f markdown -o ./out
```

**With scoped thresholds**
```bash
pbreporter gate -i benchmark-report.json \
  -tm 500ms -tm "DemoApi.Controllers.CreateController.*=100ms"
```
> Note: See [Scoped Thresholds](#scoped-thresholds) for the `pattern=value` syntax and how the most specific matching rule is chosen.

**With output threshold report**
```bash
pbreporter gate -i benchmark-report.json -tm 500ms -f hit-txt
```
> Note: The `hit-txt` format will only generate when at least one threshold is hit.

**With console output**
```bash
pbreporter gate -i benchmark-report.json -f console
```
> Note: The `console` format displays the report directly in the terminal instead of creating a file.

**With Markdown output**
```bash
pbreporter gate -i benchmark-report.json -f markdown
```
> Note: The `markdown` format is ideal for generating reports to upload to GitHub or other platforms that support Markdown rendering.

**With multiple formats**
```bash
pbreporter gate -i benchmark-report.json -f json -f markdown -f console
```

**With no CLI path, using a config file**. See [Configuration](../configuration.md) for the full example.
```bash
pbreporter gate
```



## Output Metrics

The gate report includes the following metrics for each benchmark:

| Metric | Description |
|--------|-------------|
| **Mean** | Mean execution time per operation, scaled for display |
| **Allocated** | Bytes allocated per operation, scaled for display |

> Note: Unlike `compare`, `gate` has no baseline/target pair to diff, so it reports raw values only - no Gen0/Gen1/Gen2 columns and no percentage-change annotations.



## Error Handling Options

The tool provides options to control exit codes for CI/CD integration and automated quality gates.

**Fail on warnings**
```bash
pbreporter gate -i benchmark-report.json -fw
```
> Note: Exits with code 2 if any warnings are generated during the check (e.g., the report wasn't built in RELEASE mode).

**Threshold hits always fail**
```bash
pbreporter gate -i benchmark-report.json -tm 500ms -ta 10kb
```
> Note: Unlike `compare`'s `--fail-on-threshold-hit`, `gate` has no opt-in/opt-out flag for this - checking thresholds is the command's entire purpose, so exceeding one always exits with code 3.

See [Exit Codes](../exit-codes.md#exit-codes) for all command exit codes.
