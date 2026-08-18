# Advisory Sources Reference

Use these sources to enrich a vulnerability finding with advisory details (severity, affected versions, patched version, CVE/GHSA IDs, description) before deciding on a remediation target version.

**Priority order**: Project-local scanner output (`dotnet list --vulnerable`) first. Use external sources to fill in missing advisory context - patched version, full CVE/GHSA ID, severity, and description.

---

## 1. GitHub Dependabot Alerts

When the user provides a GitHub repository URL or Dependabot alert, use the following patterns to retrieve alert details.

If the user does not provide a Dependabot URL, derive it from git remotes first:

```powershell
git remote -v
```

Derivation rules:
- Prefer remote `origin` when it points to GitHub.
- If `origin` is not GitHub, select another GitHub remote if available.
- Support both remote formats:
  - `https://github.com/<owner>/<repo>.git`
  - `git@github.com:<owner>/<repo>.git`
- Extract `<owner>/<repo>` and construct:
  - `https://github.com/<owner>/<repo>/security/dependabot`

If no GitHub remote exists, git is unavailable, or parsing fails, skip Dependabot lookup and continue with GitHub Advisory Database and NuGet advisory sources.

### Dependabot alerts page

```
https://github.com/<owner>/<repo>/security/dependabot
```

Each alert card shows:
- Package name and ecosystem (look for `nuget`).
- Current version vs. patched version.
- GHSA advisory ID and link.
- Auto-dismiss/close status.

### GitHub REST API - list Dependabot alerts

```powershell
# Requires a GitHub token with security_events scope
$headers = @{ Authorization = "Bearer $env:GITHUB_TOKEN" }
Invoke-RestMethod `
  -Uri "https://api.github.com/repos/<owner>/<repo>/dependabot/alerts?state=open&ecosystem=nuget&per_page=100" `
  -Headers $headers | ConvertTo-Json -Depth 10
```

Key response fields:
- `security_advisory.ghsa_id` - GHSA identifier.
- `security_advisory.cve_id` - CVE identifier (may be null for some advisories).
- `security_advisory.severity` - `low`, `medium`, `high`, or `critical`.
- `security_advisory.description` - Full advisory description.
- `security_vulnerability.vulnerable_version_range` - Affected version range (e.g., `< 13.0.3`).
- `security_vulnerability.first_patched_version.identifier` - The earliest safe version.
- `dependency.package.name` - Package name.

### GitHub REST API - single alert

```powershell
Invoke-RestMethod `
  -Uri "https://api.github.com/repos/<owner>/<repo>/dependabot/alerts/<alert-number>" `
  -Headers $headers
```

---

## 2. GitHub Advisory Database

Use when you have a GHSA ID or CVE and need the full advisory record independently of a specific repo.

### GHSA lookup URL

```
https://github.com/advisories/<GHSA-xxxx-xxxx-xxxx>
```

The page shows:
- Published / updated dates.
- Affected package, ecosystem, version range, patched version.
- CVSS score and vector.
- References (including CVE).

### GitHub Advisory Database API

```powershell
$query = @"
{
  securityAdvisory(ghsaId: "GHSA-xxxx-xxxx-xxxx") {
    summary
    severity
    publishedAt
    vulnerabilities(first: 10) {
      nodes {
        package { name ecosystem }
        vulnerableVersionRange
        firstPatchedVersion { identifier }
      }
    }
  }
}
"@

$body = @{ query = $query } | ConvertTo-Json
Invoke-RestMethod `
  -Uri "https://api.github.com/graphql" `
  -Method Post `
  -Headers @{ Authorization = "Bearer $env:GITHUB_TOKEN"; "Content-Type" = "application/json" } `
  -Body $body
```

Filter `vulnerabilities.nodes` for `package.ecosystem == "NUGET"` (case-insensitive).

---

## 3. NuGet Package Advisories

NuGet.org surfaces security advisory data directly for each package version.

### Package advisory page URL

```
https://www.nuget.org/packages/<PackageName>/<Version>
```

Navigate to the "Vulnerabilities" tab to see all advisories affecting a specific version and which version resolves each.

### NuGet Registration API - advisory metadata

```powershell
# Retrieve the registration index for a package
Invoke-RestMethod "https://api.nuget.org/v3/registration5-gz-semver2/<packagename>/index.json"
```

Each leaf entry (`catalogEntry`) may include a `vulnerabilities` array:

```json
{
  "vulnerabilities": [
    {
      "advisoryUrl": "https://github.com/advisories/GHSA-xxxx-xxxx-xxxx",
      "severity": "2"
    }
  ]
}
```

Severity mapping: `0` = low, `1` = moderate, `2` = high, `3` = critical.

Use the advisory URL to cross-reference with the GitHub Advisory Database.

### dotnet package search (CLI)

```powershell
# Find the latest available version (may be the patched version)
dotnet package search <PackageName> --exact-match --format json
```

Cross-reference the candidate version against the advisory's `firstPatchedVersion` to confirm it is safe.

---

## 4. Cross-Reference and ID Normalization

| Source provides | Cross-reference to get |
|----------------|------------------------|
| CVE only | Search `https://github.com/advisories?query=<CVE-ID>` to get GHSA |
| GHSA only | Advisory page shows CVE reference if assigned |
| NuGet advisory URL | Points to GHSA; parse GHSA ID from URL path |

When both CVE and GHSA are available, record both in the `advisory` field of the security-fix comment (comma-separated), e.g., `CVE-2024-21907, GHSA-5crp-q978-2vqf`.

---

## Confidence ranking

When sources provide conflicting patched-version information, use this priority:

1. `dotnet list package --vulnerable` output (most authoritative for the local project's resolved dependency graph).
2. GitHub Advisory Database `firstPatchedVersion.identifier` for the affected NuGet package.
3. NuGet Registration API `vulnerabilities[].advisoryUrl` + related advisory page.
4. Dependabot alert `security_vulnerability.first_patched_version.identifier` (from user-provided or git-remote-derived URL).

If all agree, proceed with confidence. If they disagree, use the highest patched version to be safe, and note the discrepancy in the `reason` field of the security-fix comment.
