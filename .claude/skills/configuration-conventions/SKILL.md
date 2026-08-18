---
name: configuration-conventions
description: 'Explains how pbreporter options correlate across CLI arguments, environment variables, and the YAML config file - naming conventions, precedence, and the scoped/global rule shape per source. Use this skill before adding a new CLI option or a new command, so the shapes you create stay consistent with the existing ones instead of being re-invented per-option.'
---

# Configuration Conventions

## Overview

Every `pbreporter` option can be set three ways: a **CLI argument**, an **environment variable**, or a **YAML config file**. Precedence, highest wins:

```
CLI arguments  >  environment variables  >  YAML config file
```

The four files that implement it:

| File | Role |
|------|------|
| `src/Common/Configuration/PbReporterConfiguration.cs` | Root config schema - one nested `XxxConfigurationSection` class per command |
| `src/Common/Configuration/ConfigurationLoader.cs` | Reads the YAML file + environment variables, merges them (env overrides file), exposes `Load(...)` |
| `src/Common/Configuration/YamlDocumentParser.cs` | Generic, schema-agnostic YAML-subset parser (mappings/sequences/scalars only) |
| `src/Common/GlobalOptions.cs` | Shared, reusable `Option<T>` definitions any command can add to its own options (currently `--config`/`-c`) |
| Each command's own `XxxOptions.cs` (e.g. `src/Commands/Compare/CompareOptions.cs`) | Declares the CLI options and merges the loaded config section on top of/under the parsed CLI values |

**Read this before touching any of them.** The goal is that a new option or command looks like it was written by the same person who wrote `compare`'s thresholds - not a one-off pattern.

## Naming convention for a plain (non-scoped) option

Most options are a single value with no "apply to a subset" behavior - e.g. `compare`'s `-b`/`--baseline`:

| Source | Shape |
|--------|-------|
| CLI | `--baseline <value>` (kebab-case long name) |
| Env var | `PBREPORTER_COMPARE__BASELINE=value` - `PBREPORTER_<SECTION>__<KEY>`, `<SECTION>` = command name upper-snake, `<KEY>` = option name upper-snake, `__` (double underscore) separates segments, matched **case-insensitively**. See `ConfigurationLoader._apply`. |
| YAML | `compare:\n  baseline: value` - command name as a lowercase top-level key, option name as a **camelCase** nested key. See `ConfigurationLoader._parseCompareSection`. |

This is the same shape for any future plain option on any command, swap `compare`/`baseline` for the new command/option name.

Not every option needs all three sources, some genuinely are CLI-only (a one-off flag that only ever makes sense typed at invocation time, never as a durable setting). But don't assume a required-looking value is CLI-only by default: `compare`'s `-b`/`-t` looked like obvious CLI-only candidates (required, "just" file paths) but turned out to be exactly the kind of thing users want to set once via `PBREPORTER_COMPARE__BASELINE`/`TARGET` or a YAML file (e.g. a CI pipeline that always diffs against the same baseline artifact) and override per-run only when needed. When an option is `Required = true` at the CLI level *and* you're giving it config/env support too, drop `Required` there and let the value being `null`/empty after all three sources are merged surface as a normal runtime error instead (see `CompareHelpers.GetJsonReport`'s null-path check for the pattern this codebase already uses instead of hand-rolling new "still missing" validation).

## Scoped (pattern-based, repeatable) options

Thresholds are the one option shape today that supports "apply this value only to benchmarks matching a pattern" (`compare -tm "DemoApi.*=10ms"`), and this is the shape to copy for any future option with the same "global default + per-item overrides" need. Each source expresses it differently - this is the one place the three sources are **not** structurally symmetric:

| Source | Shape |
|--------|-------|
| CLI | Repeatable option; a **bare value** (`-tm 5%`) is global (pattern `*`); `pattern=value` (`-tm "DemoApi.*=10ms"`) is scoped. See `CompareOptions.MeanThresholdOption`'s validator + `_parseThresholdTokens`. |
| Env var | Global is a **plain scalar key** (`PBREPORTER_COMPARE__THRESHOLD_MEAN`); scoped entries use a 0-based **indexed array**: `PBREPORTER_COMPARE__THRESHOLDS__<n>__PATTERN`, `__<n>__THRESHOLD_MEAN`, `__<n>__THRESHOLD_ALLOCATION`. See `ConfigurationLoader._apply`. |
| YAML | **No top-level scalar for global at all.** Every threshold - global or scoped - is an entry in a `thresholds:` list. An entry **without** a `pattern` key is global for whichever metric(s) it sets; an entry **with** `pattern` is scoped. If more than one pattern-less entry sets the same metric, **the last one in the file wins**. See `ConfigurationLoader._parseCompareSection`. |

```yaml
# pbreporter.yml
compare:
  thresholds:
    - thresholdMean: 5%                             # global mean
    - pattern: "DemoApi.*"
      thresholdMean: 10ms                           # scoped mean
    - thresholdAllocation: 10kb                     # global allocation (separate entry - a
                                                    # single entry can set one or both metrics)
    - pattern: "DemoApi.Controllers.CreateController.Create"
      thresholdAllocation: 5kb                      # scoped allocation
```

**Don't "fix" this asymmetry by adding top-level YAML scalars for global values** - it was deliberately removed (a `thresholds:` entry without `pattern` already means global, so a separate top-level `thresholdMean:`/`thresholdAllocation:` key would just be a second, redundant way to spell the same thing). The env var and CLI shapes are unaffected by this - they keep their own natural "global" spelling (a bare value / a plain scalar key).

### Merge mechanics

`src/Common/NamespacesUtils.cs` is the shared pattern-matching/merge utility, reuse it, don't write new merge logic for a new scoped option:
- `IsValidPattern(pattern)`: a pattern is valid if it has no `*`, or exactly one `*` as its last character.
- `IsMatch(pattern, fullName)` / `GetSpecificity(pattern)`: exact match beats any wildcard; among wildcards, the longer literal prefix wins. Used at evaluation time (`CompareValidator`) to pick the single best-matching rule for a given item out of all the rules that matched it.
- `Merge(params IReadOnlyList<KeyValuePair<string, string>>[] layers)`: merges rule lists keyed by pattern; a later layer's rule for a pattern replaces an earlier layer's rule for that same pattern. Called **twice**, at two different layers:
  1. `ConfigurationLoader`'s field-level merge (`_mergeCompareSections`/`_mergeThresholds`) combines the YAML file's rules with the environment variables' rules - this one is field-level (a pattern's `ThresholdMean` and `ThresholdAllocation` can come from different layers for the same pattern), because YAML/env each carry both metrics on one config object per pattern.
  2. `XxxOptions.Parse` (e.g. `CompareOptions.Parse`) combines the config-derived rules (already merged file+env) with the CLI-derived rules via `NamespacesUtils.Merge(fromConfig, fromCli)`, CLI is always the last argument, so it's the highest-precedence layer. This one is whole-rule-level (`KeyValuePair<string, string>`, one metric's ruleset at a time), because that's the flat shape both CLI parsing and config-to-rules conversion already produce.

## Recipe: adding a new option to an existing command

1. **CLI**: add the `Option<T>` (or, for a scoped option, follow `CompareOptions._createThresholdOption`'s pattern) to the command's `XxxOptions.cs`.
2. **Schema**: add the matching property (or, for scoped, extend the section's `Thresholds`-style list entry class) to the section class in `PbReporterConfiguration.cs`.
3. **Env var**: add a branch to `ConfigurationLoader._apply` for the new key under the command's section name.
4. **YAML**: add parsing for the new key in the section's parse method (e.g. `_parseCompareSection`), plain scalar for a simple option, or fold into the `thresholds`-entry pattern-null-means-global handling if it's scoped.
5. **Merge**: wire the config→CLI merge in the command's `Options.Parse`, unless the option is intentionally CLI-only (see "not every option needs all three sources" above).
6. **Docs**: update the README's `Environment Variables` and `Configuration File` subsections and its option bullet list, and `AGENTS.md`'s options table.
7. **Tests**: mirror the existing test files, `tests/.../Common/Configuration/ConfigurationLoaderTests.cs` (env var parsing, YAML parsing, field-level file/env merge, including duplicate-global-entry-keeps-last if scoped), `tests/.../Common/Configuration/YamlDocumentParserTests.cs` (only if the new option needs YAML shapes the generic parser doesn't support yet - usually it doesn't), and the command's own `Options/ThresholdOptionTests.cs`-equivalent (CLI parsing + config/CLI merge). Run `dotnet stryker -tp tests/PowerUtils.BenchmarkDotnet.Reporter.Tests/PowerUtils.BenchmarkDotnet.Reporter.Tests.csproj -p PowerUtils.BenchmarkDotnet.Reporter.csproj --test-runner mtp --mutate "<changed file>"` on every file you touch and drive it to 100% before considering the option done (per `AGENTS.md`'s boundaries).

## Recipe: adding a new command

1. Add a new `XxxConfigurationSection` class and a corresponding property on `PbReporterConfiguration` (follow `CompareConfigurationSection`'s shape).
2. Extend `ConfigurationLoader._apply`'s section-name check (currently only recognizes `"COMPARE"`) to also recognize the new command's section name, and add its own scalar/indexed- array parsing branches.
3. Extend `ConfigurationLoader.ParseYamlDocument` to look up the new command's top-level YAML key and parse it into the new section (mirror `_parseCompareSection`).
4. In the new command's `Build()`, add `GlobalOptions.ConfigOption` to its own `Options` (it's not registered on `RootCommand`, each command adds it directly, the same way `CompareCommand` does), call `ConfigurationLoader.Load(parser.GetValue(GlobalOptions.ConfigOption))`, and pass the new section into the command's own `Options.Parse(...)` - copy `CompareCommand.Build()`'s `SetAction` verbatim as the template.
5. Documentation and tests as in the option recipe above, plus a new `docs/test-data.md`/`AGENTS.md` entry for the command itself if one doesn't already exist.

## Worked example: `compare`'s mean/allocation thresholds

The concrete reference implementation to pattern-match against for any new scoped option, the global rule and one scoped rule, expressed identically across all three sources:

**CLI**
```bash
-tm 5% -tm "DemoApi.Controllers.CreateController.Create=2ms"
```

**Environment variables**
```bash
PBREPORTER_COMPARE__THRESHOLD_MEAN=5%
PBREPORTER_COMPARE__THRESHOLDS__0__PATTERN=DemoApi.Controllers.CreateController.Create
PBREPORTER_COMPARE__THRESHOLDS__0__THRESHOLD_MEAN=2ms
```

**YAML**
```yaml
compare:
  thresholds:
    - thresholdMean: 5%
    - pattern: "DemoApi.Controllers.CreateController.Create"
      thresholdMean: 2ms
```

All three resolve to the same rule set: `Create` is checked against `2ms`, every other benchmark against `5%`.

See [Scoped Thresholds](../../../docs/commands/compare.md) in the compare command docs, and [Environment Variables](../../../docs/configuration.md) and [Configuration File](../../../docs/configuration.md) in the configuration docs for full user-facing syntax, and `AGENTS.md`'s "Configuration Conventions" note for the short version.
