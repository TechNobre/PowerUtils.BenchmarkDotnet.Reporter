# Report Template

Use this layout when `scripts/analyze.ps1 -Report` is unavailable. Write it to `artifacts/code-health/report.md` and print it with `cat` (or `Get-Content`). Keep the section order and titles unchanged.

````markdown
# Code health report

- Date (UTC): <yyyy-MM-dd HH:mm>
- Branch: <current branch>
- Scope: <changed|all|error|warning|info|CODE>
- Base: <base branch> (merge-base <8-char sha>)
- Solution: <solution or project file>

## Summary

| Severity | Initial | Fixed | Remaining | New |
|---|---|---|---|---|
| error | 0 | 0 | 0 | 0 |
| warning | 0 | 0 | 0 | 0 |
| info | 0 | 0 | 0 | 0 |

Open findings outside the scope: <n>

## Fixed issues

| Severity | Code | Category | Location | Message |
|---|---|---|---|---|

## Remaining issues

None.

## New issues

None.

## Files changed

```text
<git diff --stat <merge-base> output, then untracked files>
```

## Test files touched

None.

## Decisions needed

None.
````

Matching rules for the tables:

- A finding counts as the same issue when file, code and message match (line numbers shift while fixing).
- Fixed = in the baseline but no longer present. New = present now but not in the baseline. Remaining = present now, in scope.
- Rows are sorted by severity, then by the criticality order in `severity-order.md`.
