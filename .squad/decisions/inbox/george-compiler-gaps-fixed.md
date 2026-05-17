# Compiler Gaps Audit Fixes

**Author:** George  
**Date:** 2026-05-17  
**Requested by:** Shane  
**Status:** Decision Record

---

## What was fixed

1. **PRE0092 / PRE0094 on Pattern A construction rows**
   - Stateful `on <InitialEvent> -> ...` rows now count as construction rows during structural and construction-guarantee validation.
   - This restores the spec-correct Pattern A lane: an initial event plus `on Create` construction rows in a stateful precept is valid.

2. **PRE0038 on `set` to computed fields**
   - `ResolveAction` now emits `ComputedFieldNotWritable` when an assignment action targets a computed field.
   - Read access to computed fields remains valid.

3. **PRE0010 on chained comparisons**
   - The Pratt parser now detects comparison chaining (`0 <= Amount <= 1000`) and emits `NonAssociativeComparison` at parse time.
   - Recovery consumes the chained tail and returns the left comparison so the user does not fall through to the misleading PRE0018 type-error path.

---

## Where the gaps were

- `src/Precept/Pipeline/TypeChecker.Validation.Structural.cs`
  - PRE0092 still trusted `TypedEventRow.IsConstruction` alone when deciding whether an event handler was illegal in a stateful precept.
- `src/Precept/Pipeline/TypeChecker.Validation.FieldState.cs`
  - Construction guarantees and construction-guard checks also trusted `IsConstruction` alone, which let the Pattern A snippet still trip PRE0094 when that flag was not set on the row.
- `src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs`
  - The existing computed-field write protection only covered declaration modifiers, not `set` actions in transition/event rows.
- `src/Precept/Pipeline/Parser.Expressions.cs`
  - The Pratt loop honored non-associative binding power but still built a second comparison node, which deferred the user-facing failure to type checking as PRE0018.
- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs`
  - Gate allow-lists needed sync once PRE0010 gained a real emission site and test coverage remained cross-project.

---

## Root-cause observations

- The Slice 8b design is correct: construction status is semantic (`event ... initial`), not parser syntax. The gap was downstream validators still depending too hard on the cached `IsConstruction` flag instead of re-deriving from event metadata when needed.
- PRE0038 was never a catalog gap; the compiler already knew computed fields are not writable, but action assignment normalization was missing the validation hook.
- Non-associative operator metadata alone is not enough for good diagnostics in a Pratt parser. You still need an explicit recovery branch for the forbidden chained shape if you want a precise parser error instead of a later type mismatch.

---

## Verification

- Added regression tests for:
  - multi-state Pattern A construction rows compiling clean,
  - non-initial event handlers in stateful precepts still emitting PRE0092,
  - the `SyntaxReference.CommonPatterns` constructor snippet compiling clean,
  - transition-row writes to computed fields emitting PRE0038,
  - regular-field writes / computed-field reads not emitting PRE0038,
  - chained comparisons emitting PRE0010 and not PRE0018.
- `dotnet test test/Precept.Tests/ --filter "FullyQualifiedName~TypeCheckerConstructionStructuralTests|FullyQualifiedName~TypeCheckerTransitionTests|FullyQualifiedName~Track2PhaseAParserTests|FullyQualifiedName~SyntaxReferenceTests" --no-restore` ✅
- `dotnet test test/Precept.Tests/` ❌ blocked by pre-existing `F5TempVerify` failures on `samples\parcel-locker-pickup.precept` and `samples\clinic-appointment-scheduling.precept` (`UnsatisfiableInitialState`).
- `dotnet test test/Precept.Tests/ --no-build -q` ❌ same pre-existing `F5TempVerify` failures.
