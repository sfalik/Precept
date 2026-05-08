# TypeChecker B1/B2/B3 Blockers — Fixed

**By:** George (Runtime Dev)
**Date:** 2026-05-08T07:00:00-04:00
**Status:** Complete — all three R3 blockers resolved, tests green
**Context:** Frank's R3 final gate review (`.squad/decisions/inbox/frank-r3-final-review.md`) identified three blockers preventing GraphAnalyzer from proceeding.

---

## Changes

### B3: MissingExpression D26 gap (5 LOC)

`ResolveMissing()` now emits a lightweight `DiagnosticCode.TypeMismatch` diagnostic with args `("expression", "missing")` before returning `TypedErrorExpression`. This closes the D26 self-containment invariant — every error path through Resolve() now records a TC-level diagnostic.

No new DiagnosticCode was added (per Frank's approval gate). TypeMismatch is the closest existing Error-severity TC code.

### B1: Field expression resolution (~100 LOC)

`ResolveFieldExpressions()` resolves default and computed expressions on `TypedField` entries:
- Default expressions from `ParsedModifier` with `Kind == ModifierKind.Default`
- Computed expressions from `ComputeExpressionSlot` on the field's `Syntax`
- `ComputedFieldDep` extraction via recursive `CollectFieldRefs()` tree walker
- `FieldScopeMode.PriorFieldsOnly` enforces forward-reference prohibition
- Qualifier binding left as null (no parser-level qualifier slot on field constructs yet)
- Event arg defaults left as null (DeclaredArg carries only ModifierKind, not values)

### B2: Construct normalization (~200 LOC)

Four new normalization methods following the established `manifest.ByKind` + Resolve + accumulate pattern:
- `PopulateEnsures()` — StateEnsure (in/to/from → ConstraintKind) and EventEnsure (on → EventPrecondition)
- `PopulateAccessModes()` — state/field reference resolution, Editable→Write / Readonly→Read mapping, optional guard
- `PopulateStateHooks()` — state reference, leading token → AnchorScope, action chain via ResolveAction()
- `PopulateEditDeclarations()` — D24 placeholder using ConstructKind.OmitDeclaration, field targets recorded

### Supporting changes

- `ParsedConstruct.LeadingTokenKind` — added `TokenKind?` to the positional record (2 parser sites updated) for anchor scope determination
- Doc updates W3 (§1 status), W4 (§4 LOC estimate → ~2700), W5 (§13 preamble → COMPLETED)
- 17 tests updated to match new diagnostic emission and populated accumulators

---

## Validation

- Build: 0 errors, 0 warnings
- Tests: 3342 Precept.Tests + 263 Precept.Analyzers.Tests — all passing
- D26 assert: no fires on any test or sample file

## Open Items

- **Qualifier binding** on TypedField — needs parser-level qualifier slot on field constructs (future work)
- **Event arg default expressions** — DeclaredArg only carries ModifierKind array, not values (future work)
- **DiagnosticCode.TypeMismatch reuse** for MissingExpression — Frank may want a dedicated code in the future
