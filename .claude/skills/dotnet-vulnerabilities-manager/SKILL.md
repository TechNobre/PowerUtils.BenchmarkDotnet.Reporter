---
name: dotnet-vulnerabilities-manager
description: Detect and fix NuGet security vulnerabilities in .NET projects. Always use this skill when the user mentions: CVE in .NET, vulnerable NuGet packages, dotnet list package --vulnerable, dependabot alerts on a .NET repo, GHSA advisory for a NuGet package, transitive vulnerability in csproj, NuGet security patch, audit NuGet packages for security, fix CVE in project, security scan results, or when a GitHub Dependabot alert or security advisory points at a NuGet dependency. Covers the full remediation workflow: CLI-based detection, direct and transitive package remediation with dotnet commands, traceability comments for transitive override entries in project files, enrichment via external advisories (GitHub Dependabot, GitHub Advisory Database, NuGet advisories), and automatic cleanup of stale transitive overrides that are no longer necessary.
license: MIT
---

# .NET Vulnerabilities Manager

## Overview

Scan .NET projects and solutions for known NuGet package vulnerabilities, remediate them using dotnet CLI commands, annotate transitive override entries with a standardized security comment so future developers understand why a package was added only for remediation, and clean up past overrides that are no longer needed.

## Scope

- Detect vulnerable packages (direct and transitive) via `dotnet list package --vulnerable --include-transitive`.
- Remediate direct package vulnerabilities by upgrading the affected package.
- Remediate transitive vulnerabilities by adding an explicit direct reference that forces a safe version.
- Annotate transitive override entries with a multi-line verbose comment in the project file.
- Remove stale transitive override packages that are no longer required.
- Enrich findings with external advisory data from GitHub Dependabot, GitHub Advisory Database, and NuGet advisories.

## Out of Scope

- Upgrading packages that have no security advisory (use `dotnet-nuget-manager` for that).
- Non-.NET package ecosystems (npm, pip, etc.).
- Direct XML file edits when an equivalent dotnet CLI command exists.

## Prerequisites

- .NET SDK installed; `dotnet` available in `PATH`.
- Internet access or an accessible NuGet feed for package resolution.

## Core Rules

1. **CLI-first**: Always use dotnet CLI to add/remove/update packages. Only edit XML directly when no CLI alternative exists (e.g., adding security comments or editing package metadata in `Directory.Packages.props` for transitive version overrides in CPM projects).
2. **Annotate transitive overrides only**: Every `PackageReference` or `PackageVersion` added specifically to force a safe transitive version must have a `security-fix` comment immediately above it. Direct package upgrades do not require security comments. See the [comment specification](references/comment-spec.md).
3. **No duplicate transitive comments**: If a security-fix comment for the same advisory+package already exists, update it rather than adding a new one.
4. **Verify after every change**: Re-run `dotnet list package --vulnerable --include-transitive` after each remediation to confirm the advisory no longer appears.
5. **Auto-cleanup**: When a stale transitive override is detected and confirmed no longer needed after validation, remove it automatically with `dotnet remove package`.
6. **One change set at a time**: Remediate and verify one vulnerability before moving to the next. This prevents cascading restore failures.

---

## Phase 1: Detection

Run the primary scan on every targeted project or solution file:

```powershell
# Scan all projects in a solution
dotnet list <solution.slnx|solution.sln> package --vulnerable --include-transitive

# Scan a single project
dotnet list <project.csproj> package --vulnerable --include-transitive
```

Parse the output and build a triage table:

| Package | Version | Advisory | Severity | Type |
|---------|---------|---------|----------|------|
| Newtonsoft.Json | 12.0.1 | CVE-2024-21907 | High | direct |
| System.Net.Http | 4.3.0 | CVE-2017-0248 | Critical | transitive |

**Type classification rules**:
- `direct` — appears under the project's own `<PackageReference>` entries.
- `transitive` — flagged in the `--include-transitive` output but NOT present in the project's own references; it is pulled in by a dependency.

If the user provides a GitHub Dependabot URL (e.g., `https://github.com/owner/repo/security/dependabot`) or mentions a GHSA/CVE ID, also read [advisory-sources.md](references/advisory-sources.md) to enrich the finding with the full advisory before remediating.

If the user does NOT provide a Dependabot URL, attempt to derive it from git remotes:

```powershell
git remote -v
```

Use these rules:
- Prefer `origin` when it points to GitHub.
- If `origin` is not GitHub, use another GitHub remote if one exists.
- Accept both HTTPS and SSH GitHub remote formats:
   - `https://github.com/<owner>/<repo>.git`
   - `git@github.com:<owner>/<repo>.git`
- Normalize to `owner/repo` and construct:
   - `https://github.com/<owner>/<repo>/security/dependabot`

If git is unavailable, the folder is not a git repo, or no GitHub remote can be parsed, skip Dependabot enrichment and continue remediation using local scan output plus GitHub Advisory Database/NuGet advisory sources.

---

## Phase 2: Triage

For each vulnerability, determine the remediation path before acting:

1. **Check if a fixed version exists**:
   ```powershell
   dotnet package search <PackageName> --exact-match --format json
   ```
   Find the lowest version that is ≥ the current version AND does not carry the advisory. Cross-reference with the advisory's "patched version" field from NuGet or GitHub Advisory Database.

2. **Detect project style** (affects how versions are applied):
   - `Directory.Packages.props` present → Central Package Management (CPM). Read the guidance in the CPM note below.
   - No `Directory.Packages.props` → per-project version management.

3. **Check for prior overrides**: Look for existing `PackageReference` entries that already have a `security-fix` comment. If one exists for the same package but with an outdated version, update the version and the comment rather than adding a new entry.

---

## Phase 3: Remediation

### 3a — Direct vulnerability

The vulnerable package is a direct reference in the project:

```powershell
# Update to the patched version
dotnet add <project.csproj> package <PackageName> --version <safe-version>

# Restore and verify
dotnet restore <project.csproj>
dotnet list <project.csproj> package --vulnerable --include-transitive
```

For direct vulnerabilities, no security-fix comment is required. This is a standard direct dependency upgrade.

### 3b — Transitive vulnerability

The vulnerable package is NOT a direct reference; it is pulled in by another package:

```powershell
# Add an explicit direct reference to force the safe transitive version
dotnet add <project.csproj> package <TransitivePackageName> --version <safe-version>

# Restore and verify
dotnet restore <project.csproj>
dotnet list <project.csproj> package --vulnerable --include-transitive
```

Add the security-fix comment above the new `PackageReference` to explain why this otherwise-indirect package is listed directly:

```xml
<!--
  security-fix: transitive
  advisory: CVE-2017-0248, GHSA-xxxx-xxxx-xxxx
  transitive-package: System.Net.Http
  pulled-by: Microsoft.AspNet.WebApi.Client >= 5.2.6
  fixed-version: 4.3.4
  reason: TLS validation bypass allowing man-in-the-middle interception; explicit reference forces safe transitive version.
-->
<PackageReference Include="System.Net.Http" Version="4.3.4" />
```

#### CPM note (Central Package Management with Directory.Packages.props)

When `Directory.Packages.props` is present:
- Direct package upgrades: edit the version in `Directory.Packages.props` (the CLI `dotnet add` won't manage CPM versions directly). No security-fix comment is required for direct upgrades.
- Transitive overrides: add `<PackageVersion Include="<Name>" Version="<safe>" />` to `Directory.Packages.props`. Place the security-fix comment immediately above this entry.
- Do NOT add a `Version` attribute to the `<PackageReference>` in `.csproj` when CPM is active.

### 3c — Stale override cleanup

After upstream dependencies have been updated, a transitive override added for a past CVE may no longer be needed.

**Detection**:
1. Identify `PackageReference` entries that have a `security-fix: transitive` comment.
2. For each, check whether the current parent package(s) named in `pulled-by` now bundles a version ≥ the `fixed-version` in the comment.
3. Run a test removal:
   ```powershell
   dotnet remove <project.csproj> package <OverridePackageName>
   dotnet restore <project.csproj>
   dotnet list <project.csproj> package --vulnerable --include-transitive
   ```
4. If vulnerability no longer appears after removal → the override is stale. Keep it removed.
5. If the vulnerability reappears → restore the reference (`dotnet add package` again) and keep it.

### 3d — Unresolvable vulnerabilities

When no safe version is available yet (the advisory is open with no patch):

- Do not force an insecure or unreleased version.
- Report the advisory ID, affected package, current version, and the fact that no patched release exists.
- Suggest watching the advisory URL for a fix and pinning a known-good version if a partial mitigation exists.

---

## Phase 4: Verification

After all remediations are applied, run a final verification pass:

```powershell
# Full solution re-scan
dotnet list <solution> package --vulnerable --include-transitive

# Confirm the build is still healthy
dotnet restore <solution>
dotnet build <solution> --no-incremental
```

If `dotnet build` fails after a package update, investigate compatibility before proceeding — do not leave the project in a broken state.

---

## External Advisory Enrichment

Before remediating, or when a user provides a GHSA/CVE ID without specifying a safe version, read [advisory-sources.md](references/advisory-sources.md) for lookup instructions for:
- **GitHub Dependabot** alerts page (project-specific affected versions and patched version per alert). If no URL is provided, derive it from `git remote -v` when the repository is hosted on GitHub.
- **GitHub Advisory Database** (GHSA canonical records with patched-version ranges).
- **NuGet advisory pages** (per-package advisory metadata and recommended version).

---

## Comment Specification

Read [comment-spec.md](references/comment-spec.md) for:
- Full field definitions and allowed values.
- Templates for transitive and CPM transitive placement.
- Idempotency rules (when to create vs. update an existing comment).

---

## Worked Examples

### Example 1 — Direct vulnerability fix

**User**: "Newtonsoft.Json is flagged for CVE-2024-21907 in my project."

**Steps**:
1. `dotnet list src/Api/Api.csproj package --vulnerable --include-transitive` → confirms `Newtonsoft.Json 12.0.1` is direct, `High`.
2. `dotnet package search Newtonsoft.Json --exact-match --format json` → identifies `13.0.3` as the patched release.
3. `dotnet add src/Api/Api.csproj package Newtonsoft.Json --version 13.0.3`
4. `dotnet restore src/Api/Api.csproj`
5. `dotnet list src/Api/Api.csproj package --vulnerable --include-transitive` → clean.
6. No security-fix comment is added because this is a direct package upgrade.

### Example 2 — Transitive vulnerability fix

**User**: "My build flags System.Net.Http for CVE-2017-0248 as a transitive dependency."

**Steps**:
1. Confirm `System.Net.Http` is transitive (not in direct refs).
2. Identify parent package pulling it in from scan output.
3. Determine patched version from NuGet advisory page.
4. `dotnet add src/Api/Api.csproj package System.Net.Http --version 4.3.4`
5. `dotnet restore && dotnet list ... --vulnerable` → clean.
6. Add `security-fix: transitive` comment explaining the forced reference.

### Example 3 — Stale override removal

**User**: "Check if the System.Formats.Asn1 override we added six months ago is still needed."

**Steps**:
1. Find the entry in `.csproj` with `security-fix: transitive` comment.
2. `dotnet remove src/Api/Api.csproj package System.Formats.Asn1`
3. `dotnet restore && dotnet list ... --vulnerable` → clean (parent now bundles safe version).
4. Override removed permanently.
