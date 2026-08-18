# Configuration

[← Back to README](../README.md)

Every option on every command can be set three ways: a **CLI argument**, an **environment variable**, or a **YAML config file**. This page explains the naming convention and precedence that all three follow, it applies to `compare` today and to any command added in the future.

- [Precedence](#precedence)
- [Environment Variables](#environment-variables)
- [Configuration File](#configuration-file)



## Precedence

Highest wins, lowest to highest:

```
config file  →  environment variables  →  CLI arguments
```

The priority is environment variables → command-line convention. A value set in the config file can be overridden per-run by an environment variable, which can in turn be overridden by a CLI argument on that specific invocation, useful for setting an org-wide or repo-wide default that individual runs can still override.



## Environment Variables

`compare`'s options

| Environment variable | Equivalent to |
|-----------------------|---------------|
| `PBREPORTER_COMPARE__BASELINE` | `-b <path>` |
| `PBREPORTER_COMPARE__TARGET` | `-t <path>` |
| `PBREPORTER_COMPARE__FORMATS` | `-f <format>` (single value, e.g. `json`; for multiple formats use CLI `-f` flags) |
| `PBREPORTER_COMPARE__THRESHOLD_MEAN` | `-tm <value>` (global/`*` rule) |
| `PBREPORTER_COMPARE__THRESHOLD_ALLOCATION` | `-ta <value>` (global/`*` rule) |
| `PBREPORTER_COMPARE__THRESHOLDS__<n>__PATTERN` | the `<pattern>` half of a scoped `-tm`/`-ta` entry, index `<n>` starting at `0` |
| `PBREPORTER_COMPARE__THRESHOLDS__<n>__THRESHOLD_MEAN` | the `<value>` half of a scoped `-tm "<pattern>=<value>"` entry, same index `<n>` |
| `PBREPORTER_COMPARE__THRESHOLDS__<n>__THRESHOLD_ALLOCATION` | the `<value>` half of a scoped `-ta "<pattern>=<value>"` entry, same index `<n>` |

The naming convention is `PBREPORTER_<SECTION>__<KEY>`, where `<SECTION>` is the command name (`COMPARE` today) and `<KEY>` is the option name, both matched **case-insensitively**. `__` (double underscore) separates each part of the name, including the numeric index for a scoped entry.

```bash
export PBREPORTER_COMPARE__THRESHOLD_MEAN=5%
export PBREPORTER_COMPARE__THRESHOLDS__0__PATTERN="DemoApi.Controllers.CreateController.*"
export PBREPORTER_COMPARE__THRESHOLDS__0__THRESHOLD_MEAN=10ms

pbreporter compare -b baseline-full.json -t target-full.json -ft
```
> Note: This is equivalent to running with `-tm 5% -tm "DemoApi.Controllers.CreateController.*=10ms"`. A `-tm`/`-ta` value passed on the command line for the same pattern still overrides the corresponding environment variable.

See [`compare` → Scoped Thresholds](commands/compare.md#scoped-thresholds) for the `pattern=value` matching and specificity rules that apply regardless of which source supplied the rule.



## Configuration File

Options can also live in a YAML file, for a durable, repo-committed baseline that both CLI arguments and environment variables can still override.

By default `pbreporter` looks for `pbreporter.yml` or `pbreporter.yaml` in the current directory. Pass `-c`/`--config <path>` (after the subcommand name, e.g. `pbreporter compare ... --config path.yml`) to use a different file explicitly. The option itself is a shared, reusable option definition (not `compare`-specific) - future commands add it to their own options the same way and read their own section from the same file.

```yaml
# pbreporter.yml
compare:
  baseline: baseline-full.json
  target: target-full.json
  formats: [json, markdown, console]
  thresholds:
    - thresholdMean: 5%
    - pattern: "DemoApi.*"
      thresholdMean: 10ms
    - thresholdAllocation: 10kb
    - pattern: "DemoApi.Controllers.CreateController.Create"
      thresholdAllocation: 5kb
```

`baseline`/`target` are plain scalars, equivalent to `-b`/`-t`. With the file above, `pbreporter compare` (no `-b`/`-t` needed) reads `baseline-full.json`/`target-full.json`; either can still be overridden per-run with `-b`/`-t` on the command line.

Every threshold, global or scoped, is an entry under `thresholds`:
* An entry **without** `pattern` is the global (`*`) threshold for whichever metric(s) it sets, equivalent to a bare `-tm`/`-ta` value or a `PBREPORTER_COMPARE__THRESHOLD_MEAN`/ `PBREPORTER_COMPARE__THRESHOLD_ALLOCATION` environment variable. If more than one entry sets the same metric without a pattern, the **last one in the file wins**.
* An entry **with** `pattern` is a scoped rule: `thresholdMean` and/or `thresholdAllocation` set that pattern's threshold for the corresponding metric, same syntax and specificity rules as [`compare` → Scoped Thresholds](commands/compare.md#scoped-thresholds).
* A single entry can set `thresholdMean`, `thresholdAllocation`, or both, global and scoped entries for the two metrics don't need to be paired up.

> **Note:** this is intentionally *not* a literal mirror of the CLI/env var shape, there's no top-level `thresholdMean:`/`thresholdAllocation:` scalar. Every threshold lives in the `thresholds:` list; whether it's global or scoped is determined only by the presence of `pattern`.

```bash
pbreporter compare -ft
```
> Note: With the file above and no other options, `-b`/`-t` are read from the file (`baseline-full.json`/`target-full.json`), and every benchmark is checked against `5%`/`5kb`, except `DemoApi.*` methods (`10ms`) and `DemoApi.Controllers.CreateController.Create` specifically (`5kb` allocation, `10ms` mean inherited from the looser `DemoApi.*` rule).

```bash
pbreporter compare -b baseline-full.json -t target-full.json --config ./ci/pbreporter.yml -ft
```
> Note: Explicitly pointing `--config` at a missing file is an error (the tool exits non-zero); the default lookup (no `--config` given) simply skips the file layer when neither `pbreporter.yml` nor `pbreporter.yaml` exists.

Only a narrow subset of YAML is supported: nested mappings, block sequences (items prefixed with `- `), flow-style sequences (`[a, b, c]`), and quoted/unquoted scalar values. Anchors, tags, flow-style mappings (`{a: b}`), multi-line scalars, and multi-document files are not supported.
