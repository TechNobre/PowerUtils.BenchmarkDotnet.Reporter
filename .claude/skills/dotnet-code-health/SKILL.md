---
name: dotnet-code-health
description: 'Find and fix .NET build errors, warnings, analyzer findings (CA*), code-style and code-smell suggestions (IDE*, editorconfig) that `dotnet build` alone hides, with a deterministic scan, severity-ordered fixes, and a final report. Works on-demand (only code changed vs the default branch) or as a command with arguments: /dotnet-code-health [all|error|warning|info|<code>]. Use when the user asks to build, validate the build, fix warnings, fix analyzer or style suggestions, clean code smells, improve reliability, or before declaring a change done or ready to commit.'
argument-hint: '[all|error|warning|info|<diagnostic code, e.g. CA2263>]'
license: MIT
allowed-tools: Bash
---

# .NET Code Health

Scan the solution with a fixed sequence of commands, fix the findings in a fixed order (severity, then criticality), re-scan until clean, and finish with a written report that is printed to the CLI. Never report "build succeeded" without having inspected warnings and info items.

## When to Use

- The user asks to build, compile or validate a .NET project, or to fix warnings, analyzer findings, style suggestions or code smells.
- After finishing a code change, before reporting it as done.
- The user runs `/dotnet-code-health` (with or without an argument).

## When Not to Use

- Running unit or mutation tests: do that after this skill reports a clean result.

## Prerequisites

- `git` repository, .NET SDK on `PATH` (honour `global.json`), PowerShell for the bundled script: `pwsh` 7+ (Windows, Linux, macOS) or `powershell` 5.1 (built into Windows). Install `pwsh` with `winget install Microsoft.PowerShell` (Windows), `brew install powershell` (macOS) or the [Microsoft package repository](https://learn.microsoft.com/powershell/scripting/install/installing-powershell-on-linux) (Linux). With neither shell, use the [fallback sequence](#fallback-without-powershell).
- Run every command from the repository root.
- `<skill-dir>` below is the directory containing this `SKILL.md`.
- `<ps>` below is `pwsh -NoProfile` on any OS, or `powershell -NoProfile -ExecutionPolicy Bypass` on Windows when `pwsh` is missing.

## Arguments and Scope

The scope is chosen once, at the start, and is passed to the script as `-Scope`.

| Invocation | Scope | Meaning |
|------------|-------|---------|
| `/dotnet-code-health` or on-demand (no slash command) | `changed` | Only findings on lines that differ from the default branch (committed, staged, unstaged and untracked changes) |
| `/dotnet-code-health all` | `all` | Every finding in the solution |
| `/dotnet-code-health error` | `error` | Only error-severity findings (whole solution) |
| `/dotnet-code-health warning` | `warning` | Only warning-severity findings (whole solution) |
| `/dotnet-code-health info` | `info` | Only info-severity findings (whole solution) |
| `/dotnet-code-health CA2263` | `CA2263` | Only that diagnostic code (whole solution) |

- Argument matching is case-insensitive. Anything else is a usage error: print the table above and stop with a non-zero exit code.
- The default branch is auto-detected (`origin/HEAD`, then `main`, then `master`). Override it with `-Base <branch>` only if the invocation names another branch.
- If the current branch is the default branch, `changed` covers only uncommitted work.

## Core Rules

1. **Never hide diagnostics.** The script already builds with `--no-incremental` and full output; do not replace it with quieter commands.
2. **Fix the cause.** Make the code satisfy the rule instead of silencing it.
3. **Respect the repo.** Follow `.editorconfig` and, when present, the coding and testing conventions in `AGENTS.md`, `CLAUDE.md` or `CONTRIBUTING.md`.
4. **Stay in scope.** Fix only the findings in `findings.tsv`. Do not refactor unrelated code.
5. **Preserve file encoding and line endings** (BOM, CRLF/LF) when editing or running bulk rewrites.
6. **Never commit or push.** Leave the changes in the working tree.

### NEVER

- **NEVER** edit `.editorconfig`.
- **NEVER** use `#pragma warning disable` (or `#pragma warning restore` to scope one).
- **NEVER** use `[SuppressMessage]` (including `GlobalSuppressions.cs` and `[assembly: SuppressMessage]`).
- **NEVER** use `<NoWarn>` (or `-nowarn`, `/nowarn`).
- **NEVER** lower or disable a diagnostic by any other route (`.globalconfig`, `.ruleset`, `dotnet_diagnostic.*.severity`, `<WarningsNotAsErrors>`, `<TreatWarningsAsErrors>` changes).

If the only way to clear a finding is one of the above, it is unresolved: report it.

## Workflow

Follow these steps in order. Do not skip or reorder them.

### 1. Preflight

```bash
git rev-parse --show-toplevel
dotnet --version
pwsh --version
```

If `pwsh` is missing, use `powershell` on Windows (see `<ps>`); if no PowerShell is available, switch to the [fallback sequence](#fallback-without-powershell). If `artifacts/` is not git-ignored (the script warns), do not edit `.gitignore`; add the warning to "Decisions needed" in the report.

### 2. Analyze (first run)

```bash
<ps> -File <skill-dir>/scripts/analyze.ps1 -Scope <scope> -Reset
```

`-Reset` starts a new baseline; use it only on this first run of an invocation.

| Output (under `artifacts/code-health/`) | Content |
|------------------------------------------|---------|
| `findings.tsv` | Findings in scope, already de-duplicated and sorted: severity (error, warning, info), then criticality, then code, file and line |
| `findings-all.tsv` | Every finding regardless of scope |
| `baseline.tsv` | Snapshot of the first run, used by the report |
| `build.log`, `format.log` | Raw tool output |

Exit codes: `0` nothing in scope, `1` findings remain, `2` tooling or usage error (read the message; fix the environment, not the code). Format-only findings are labelled `info`; everything the build reports keeps its own severity. The criticality order is in [references/severity-order.md](references/severity-order.md).

If the exit code is `0`, go straight to step 6.

### 3. Fix, in the order of `findings.tsv`

Work top to bottom: all errors, then all warnings, then all infos; inside each severity, the most critical category first. Do not jump ahead.

- **Mechanical findings** (`WHITESPACE`, `FINALNEWLINE`, `ENDOFLINE`, `CHARSET`, `IMPORTS`, and style rules that have a code fixer): batch them with the formatter, limited to the affected files when the scope is not `all`:

  ```bash
  dotnet format whitespace <solution> --no-restore --include <files>
  dotnet format style <solution> --severity info --no-restore --include <files>
  dotnet format analyzers <solution> --severity info --no-restore --include <files>
  ```

  Review the diff afterwards. If a fixer touches lines outside the scope, revert that hunk and edit by hand.
- **Everything else:** edit the code by hand. Read the diagnostic message and the rule documentation before changing behaviour.
- `IDE1006` (naming) has no automatic fix: rename by hand following `.editorconfig`.
- Auto-fixers can create new findings (for example `IDE0042` producing names that violate `IDE1006`, or `IDE0305` leaving an unused `using`); the re-scan catches them.

### 4. Tests first, only when needed

Before a fix that can change behaviour (nullability, disposal, async, exception handling, public API, logic simplification), check whether tests already cover the code:

1. Find the test projects (references to `Microsoft.NET.Test.Sdk`, `xunit`, `NUnit`, `MSTest` or `Microsoft.Testing.Platform`) and search them for the affected type or method.
2. If covered, apply the fix and run the covering tests.
3. If not covered, write a small characterization test that passes on the current code, then apply the fix and re-run it. Follow the repo's test conventions.
4. Skip this step for mechanical findings and pure style/naming changes.

Record every test file you added so the report can list it.

### 5. Re-scan until clean

```bash
<ps> -File <skill-dir>/scripts/analyze.ps1 -Scope <scope>
```

Do not pass `-Reset`. Repeat steps 3 to 5 until exit `0`, or until every remaining finding can only be cleared by violating the **NEVER** rules and is listed in the report's "Decisions needed" section.

### 6. Report (always the last step)

```bash
<ps> -File <skill-dir>/scripts/analyze.ps1 -Report -Decisions "<item 1>" "<item 2>"
```

The script writes `artifacts/code-health/report.md` and prints its content to the CLI: summary by severity (initial, fixed, remaining, new), fixed issues, remaining issues, new issues, files changed, test files touched, and decisions needed. `-Decisions` is optional; use it for abandoned findings and the git-ignore warning.

Finish by stating the outcome (the report path and the counts of fixed and remaining findings). Generic follow-ups, such as building the project and running the tests, are the caller's next steps; do not run or require any other skill for them.

## Fallback without PowerShell

Run the same sequence by hand, from the repository root (shown in POSIX shell syntax; use the equivalent commands in `cmd` on Windows, and write the logs as UTF-8):

```bash
mkdir -p artifacts/code-health
dotnet build <solution> --no-incremental -nologo -v:minimal > artifacts/code-health/build.log 2>&1
dotnet format <solution> --verify-no-changes --severity info --no-restore --verbosity normal > artifacts/code-health/format.log 2>&1
git merge-base HEAD <base-branch>
git diff --unified=0 <merge-base>
git ls-files --others --exclude-standard
```

- Diagnostics are the lines matching `<file>(<line>,<col>): error|warning <CODE>: <message> [<project>]`. Format-only findings (not present in the build log) are `info`.
- De-duplicate by file, line, column and code. Sort by severity, then by the criticality order in [references/severity-order.md](references/severity-order.md).
- For the `changed` scope keep only findings whose line is inside a `+` hunk of the diff, or in an untracked file.
- Save the first sorted list as the baseline; at the end, write the report with the layout in [references/report-template.md](references/report-template.md) and print it with `cat artifacts/code-health/report.md`.
