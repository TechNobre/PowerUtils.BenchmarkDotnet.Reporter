# Dead Code

How the skill finds dead code, what it removes on its own, and what it only reports. Rule: **remove only what is provably unused; report everything else.**

## Tiers

| Tier | Source | Finds | Action |
|------|--------|-------|--------|
| 1 | Roslyn rules `IDE0051` (unused private member), `IDE0052` (private member assigned, never read), `IDE0060` (unused parameter), `IDE0005` (unnecessary using), compiler `CS0168`, `CS0169`, `CS0219`, `CS0414` (unused local/field) | Private and local code, unused usings | Remove when the checklist below passes; otherwise report |
| 2 | Roslynator CLI `find-symbol --unused --visibility public internal` (`all` and `deadcode` scopes) | Unused internal and public symbols across the whole solution | **Report only**, never delete |
| 3 | Code coverage (not run by the script) | Code never executed by tests | Corroborating evidence only, never a trigger to delete |

Tier 1 needs no installation. The script enables the IDE rules for one extra build through generated files under `artifacts/code-health/` (`deadcode.globalconfig`, `deadcode.targets`), so the repo's `.editorconfig` is never edited. A generated global config has lower precedence than `.editorconfig`, so a rule the repo sets to `none` or `silent` stays off: the script lists it as "Not scanned" and a clean result never covers it. Tier 2 installs `roslynator.dotnet.cli` into `artifacts/code-health/tools`, outside the repo's tracked files, and is skipped with a note when it cannot be installed.

## Safe-removal checklist (Tier 1)

Delete the code only when **all** of these hold. If one fails, keep the code and add it to "Decisions needed".

1. The member is `private`, a local, a parameter of a `private` method or local function, or an unused `using`. Parameters of `protected`, `protected internal`, `internal` (when the assembly has `InternalsVisibleTo`) or public methods are report-only, because callers and derived types may live outside the solution.
2. The member has **no attributes** (attributes usually mean reflection, serialization or a framework hook, for example `[JsonInclude]`, `[Inject]`, `[Obsolete]`).
3. Its name is not used in `nameof(...)`, a string literal, an expression tree or a reflection call (search the repo for the name).
4. `IDE0060`: the method is not an override, interface implementation, delegate target, event handler or a method bound by name (`[MemberData]`, `[ModelBinder]`...). Update every call site when removing the parameter, and check each removed argument expression has no side effects (calls, `++`, `ref`/`out`, `await`, allocations with observable effects) or order dependence, for example `M(Interlocked.Increment(ref counter))`. If it does, keep the expression as a separate statement before the call (preserving evaluation order) or keep the parameter and report it.
5. `IDE0052`/`CS0414`: removing the field also removes its writes; check the assigned expression has no side effects, otherwise keep the expression as a statement.
6. A test-class fixture field (xunit `IClassFixture`, `[Fact]` classes) is checked against rule 2 and 3 like any other member.

Removing one member can make another unused. The re-scan loop (step 5 of the workflow) catches the cascade.

## Never delete from candidate lists

`deadcode-candidates.tsv` (Tier 2) and coverage gaps (Tier 3) are for visibility. The following look unused but are not:

- ASP.NET controllers, minimal-API endpoints, hosted services, middleware, `Program`/`Main`
- Handlers, validators and interface implementations registered through DI or assembly scanning
- Serialized or mapped models (JSON, EF, YAML), including properties only set by a deserializer
- Test classes, test constructors and fixtures discovered by the test runner
- Public API of a library, analyzer or tool consumed outside the solution
