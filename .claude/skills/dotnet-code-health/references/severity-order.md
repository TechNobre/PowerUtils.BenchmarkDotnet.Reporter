# Fix Order

Findings are fixed by severity first (`error`, `warning`, `info`), then by category rank (lower first), then by code, file and line. `scripts/analyze.ps1` (`$CategoryRules`) is the source of truth; keep this table in sync with it.

| Rank | Category | Codes | Why this position |
|------|----------|-------|-------------------|
| 0 | build | `CS*`, `MSB*`, `NU*` | Compiler, build and restore problems block everything else |
| 1 | security | `CA3xxx`, `CA5xxx`, `CA2100`, `CA2109`, `CA2119`, `CA2153`, `CA23xx`, `CA24xx` | Injection, crypto and deserialization risks |
| 2 | reliability | remaining `CA2xxx` | Usage and reliability bugs (disposal, argument validation, async misuse) |
| 3 | performance | `CA18xx` | Behaviour-preserving, but real cost |
| 4 | maintainability | remaining `CA*` | Design, naming and maintainability rules |
| 5 | style | `IDE*` | `.editorconfig` code-style suggestions |
| 6 | other | any unrecognised code | Review manually |
| 7 | format | `WHITESPACE`, `FINALNEWLINE`, `ENDOFLINE`, `CHARSET`, `IMPORTS` | Purely mechanical; fixed in a batch by `dotnet format` |

## Severity labels

| Label | Source |
|-------|--------|
| `error`, `warning` | As reported by `dotnet build` |
| `info` | Reported only by `dotnet format --verify-no-changes --severity info` (the build hides these) |

## Diagnostic prefixes

| Prefix | Meaning | Surfaced by |
|--------|---------|-------------|
| `CS` | C# compiler | `dotnet build` |
| `CA` | .NET code analyzers | `dotnet build` (warning or error), `dotnet format` (info) |
| `IDE` | Code style from `.editorconfig` | `dotnet build` (warning or error), `dotnet format` (info) |
| `NU` | NuGet restore and audit | `dotnet build` |
| `MSB` | MSBuild infrastructure | `dotnet build` |
| `WHITESPACE`, `FINALNEWLINE`, `ENDOFLINE`, `CHARSET`, `IMPORTS` | Formatting rules from `.editorconfig` | `dotnet format` |
