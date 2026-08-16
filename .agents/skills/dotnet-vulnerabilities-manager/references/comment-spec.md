# Security-Fix Comment Specification

Security-fix comments are required only for transitive override entries added to force a safe version of a vulnerable transitive dependency. Direct package upgrades do not require security-fix comments. For transitive overrides, place the `security-fix` XML comment **immediately above** the element so the reason for the explicit override remains diff-visible.

---

## Field Definitions

| Field | Required | Values | Notes |
|-------|----------|--------|-------|
| `security-fix` | yes | `transitive` | Marks this entry as a transitive vulnerability override |
| `advisory` | yes | Comma-separated CVE/GHSA IDs | Use both CVE and GHSA when available; use whichever identifier is known if only one exists |
| `transitive-package` | yes | NuGet package name | The vulnerable package that is a transitive dependency |
| `pulled-by` | yes | `<ParentPackage> >= <version>` | The direct dependency that pulls in the vulnerable transitive package |
| `fixed-version` | yes | SemVer string | The version that resolves the advisory |
| `reason` | yes | Free text | Human-readable explanation; include the vulnerability class and impact |

---

## Templates

### Transitive vulnerability (per-project .csproj)

Place immediately before the **new** `<PackageReference>` added to override the transitive version:

```xml
<!--
  security-fix: transitive
  advisory: CVE-YYYY-NNNNN, GHSA-xxxx-xxxx-xxxx
  transitive-package: PackageName
  pulled-by: ParentPackage >= X.Y
  fixed-version: X.Y.Z
  reason: <vulnerability class and brief impact description>; explicit reference forces safe transitive version.
-->
<PackageReference Include="PackageName" Version="X.Y.Z" />
```

**Example**:

```xml
<!--
  security-fix: transitive
  advisory: CVE-2017-0248, GHSA-7jgv-x5wq-7vx7
  transitive-package: System.Net.Http
  pulled-by: Microsoft.AspNet.WebApi.Client >= 5.2.6
  fixed-version: 4.3.4
  reason: TLS certificate validation bypass allowing man-in-the-middle interception; explicit reference forces safe transitive version.
-->
<PackageReference Include="System.Net.Http" Version="4.3.4" />
```

---

### Central Package Management (Directory.Packages.props)

For CPM projects, comments go in `Directory.Packages.props`, not in individual `.csproj` files.

**Transitive override (add new PackageVersion entry)**:

```xml
<!--
  security-fix: transitive
  advisory: CVE-YYYY-NNNNN, GHSA-xxxx-xxxx-xxxx
  transitive-package: PackageName
  pulled-by: ParentPackage >= X.Y
  fixed-version: X.Y.Z
  reason: <vulnerability class and brief impact description>; explicit version override forces safe transitive version across all projects.
-->
<PackageVersion Include="PackageName" Version="X.Y.Z" />
```

---

## Placement Rules

1. **Immediately above**: The comment block must appear on the line(s) directly preceding the `<PackageReference>` or `<PackageVersion>` element, with no blank lines between.
2. **Not inside the element**: Do not use inline XML comments or attribute annotations; always use a preceding block comment.
3. **Multiple advisories**: If a single transitive override addresses more than one CVE/GHSA, list all IDs in the `advisory` field, comma-separated.
4. **Ordering within ItemGroup**: Place the security-annotated entries at the top of their `<ItemGroup>` block to make them visually prominent.

---

## Idempotency Rules

These rules prevent duplicate comments accumulating over time:

1. **Same transitive package + same advisory already annotated**: If a `security-fix` comment for the exact same `advisory` ID(s) and `transitive-package` already exists, do not add a new comment. Only update the `fixed-version` and `reason` fields if the version changed.
2. **Same transitive package + different advisory**: Add a new comment for the additional advisory. Each distinct advisory gets its own comment block.
3. **Version downgrade never allowed**: Do not replace a higher version with a lower one, even if the advisory only specifies a minimum safe version that is lower.

---

## Advisory ID normalization

When only one identifier is known:
- If only a CVE is available: use `CVE-YYYY-NNNNN` and omit the GHSA field rather than leaving it blank.
- If only a GHSA is available: use `GHSA-xxxx-xxxx-xxxx` and omit the CVE field.
- Do not use placeholder text like `GHSA-unknown` or `CVE-unknown`.
