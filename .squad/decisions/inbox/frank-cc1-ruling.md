### 2026-05-03: CC#1 — Expression Tree Design — RESOLVED

**By:** Shane Falik (owner ruling)
**Decision:** Option A — Roslyn-style typed expression nodes

**Shape:**
- `ParsedExpression` — sealed abstract record + sealed subtypes per expression form (~10). Parser output.
- `TypedExpression` — sealed abstract record + sealed subtypes with resolved type info. Type checker output.
- Expression tree is the specifically typed layer; rest of parser AST is generic.
- Closed set by design — new expression form requires C# code change.

**Exhaustiveness enforcement:**
- Sealed class hierarchy = compiler-level exhaustiveness checking
- Roslyn analyzer test in test suite = build-time verification that all expression-DU switch arms are exhaustive. Adding a subtype without updating all switches = test fails.

**Why:**
- ~10 expression forms is a bounded, catalogable set. Strongly typed DU eliminates entire class of runtime errors.
- Closed set is a feature, not a limitation — expression additions are rare, intentional language changes that SHOULD require global updates.
- Exhaustiveness enforcement makes the C# compiler a partner in correctness, not just the test suite.
- Consistent with catalog-first architecture: expression forms are declared in the catalog; the DU reflects catalog entries in C#.

**Blocks resolved:** Parser expression slots, TC §7.2 Expression Resolution Engine, Proof Engine strategies 3 & 4, Builder compilation.

**Next wave item:** CC#25 (Execution Dispatch Design) and CC#2 (SlotValue Subtype Shapes) — present briefs when Shane is ready.
