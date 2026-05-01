# Decision Note — Phase 2c Complete (Slices 23–26)

**Date:** 2026-05-01  
**Author:** George  
**Branch:** spike/Precept-V2

---

## What Shipped

Phase 2c closes the PRECEPT0019 promotion work. All four slices landed in a single pass with no deferred items.

### Slice 23 — TypeChecker [HandlesForm] coverage
- `TypeChecker.cs` received `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` on the class.
- `private static void CheckExpression(Expression expression)` stub added with all 11 `[HandlesForm]` annotations.
- Both `TypeChecker` and `GraphAnalyzer` are `public static class` — stubs are `private static`, not instance methods.

### Slice 24 — GraphAnalyzer [HandlesForm] coverage
- `GraphAnalyzer.cs` received the same class attribute.
- `private static void AnalyzeExpression(Expression expression)` stub with all 11 `[HandlesForm]` annotations.

### Slice 25 — ExpressionFormCoverageTests Layer 2
- New file: `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` (namespace `Precept.Tests.Language`).
- 26 tests: count assertion, per-kind GetMeta + HoverDocs theories (11 each), IsLeftDenotation correctness for led/nud forms, LeadTokens contract.
- Existing `test/Precept.Tests/ExpressionFormCoverageTests.cs` (Layer 3 reflection+round-trip) updated: `ContainSingle` → `HaveCount(3)`, added `BindingFlags.Static` to method search, changed from `First()` to iterating all annotated types.

### Slice 26 — PRECEPT0019 promoted to Error
- `defaultSeverity` flipped from `DiagnosticSeverity.Warning` to `DiagnosticSeverity.Error`.
- `<WarningsNotAsErrors>PRECEPT0019</WarningsNotAsErrors>` and its comment removed from `Precept.csproj`.
- `Precept0019Tests.cs` TP1+TP2 severity assertions updated to `DiagnosticSeverity.Error`.
- Pre-condition (zero PRECEPT0019 warnings before flip) verified explicitly.

---

## Key Design Decisions

**Static class stubs require `BindingFlags.Static`** — The PRECEPT0019 analyzer uses Roslyn's symbol model (not reflection) and finds static methods fine. The xUnit reflection test in `ExpressionFormCoverageTests` however used `BindingFlags.Instance` only, which would silently miss static-class annotations. Fixed.

**Three-type annotation contract** — `ParseSession`, `TypeChecker`, and `GraphAnalyzer` each carry `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]`. The reflection test now asserts `HaveCount(3)`. When Phase 3 adds more pipeline stages they must update this count.

**Layer split preserved** — `test/Precept.Tests/ExpressionFormCoverageTests.cs` = Layer 3 (reflection bridge + parse round-trips). `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` = Layer 2 (catalog shape, per-kind metadata). `test/Precept.Tests/ExpressionFormCatalogTests.cs` = Layer 1 (enum + GetMeta). All three coexist in different namespaces.

---

## Exit State

- Build: 0 errors, 0 warnings (pre-existing RS1030 in `Precept.Analyzers.csproj` is unrelated)
- Tests: 2300 passing, 0 failing (+26 vs Phase 2b baseline of 2274)
- PRECEPT0019 severity: `DiagnosticSeverity.Error`
- `<WarningsNotAsErrors>`: removed
