# Diagnostic Enforcement — Implementation Notes

> **Status:** Complete (mission closed 2026-05-14)
> **Plan doc:** `docs/Working/diagnostic-enforcement.md`
> **Branch:** `spike/Precept-V2-Radical`

---

## 1. Executive Summary

The diagnostic enforcement mission closed 49 confirmed diagnostic gaps across 11 implementation slices. The outcome: 2 Roslyn analyzer gates installed (PRECEPT0027–0030), 30+ diagnostic codes newly wired with emission sites and tests, 2 codes staged with full infrastructure but blocked on Lookup type expansion (PRE0099, PRE0101), 1 slice closed by audit as not viable (9A), and a small set of deferred items documented with explicit unblock conditions. The catalog's promises are now enforced by standing automation.

---

## 2. Implementation Wave Log

| Wave | Slices | Notes |
|------|--------|-------|
| **Wave 1** (parallel) | 0, 1, 4, 6, 7, 9B, 9C | Foundation + all independent slices launched simultaneously |
| **Wave 2** (after Slice 1) | 2 | Choice validation depends on qualifier comparison patterns established in Slice 1 |
| **Wave 3** (after Slices 1+2+4) | 8, 5A | Scattered TypeChecker gaps + typed-constant ambiguity resolution |
| **Wave 4** (after Slice 8) | 9A | Audit-only — verified prerequisites from Slice 8 |

---

## 3. Slice-by-Slice Record

### Slice 0 — Enforcement Infrastructure (PRECEPT0027–0030)

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Commit** | `4e9f729a` |
| **Diagnostics** | PRECEPT0027 (Gate 1: no emission site), PRECEPT0028 (Gate 2: no test), PRECEPT0029 (allow-list hygiene warning), PRECEPT0030 (allow-list stale entry) |
| **Files modified** | `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` (new), `src/Precept.Analyzers/DiagnosticCoverageScanner.cs` (new), analyzer registration + tests |
| **Tests added** | Gate 1 + Gate 2 analyzer unit tests |
| **Notes** | Originally planned as PRECEPT0027–0028 only; expanded to include PRECEPT0029–0030 during implementation for allow-list hygiene |

### Slice 1 — B2: Qualifier Enforcement (PRE0070–0074)

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Commit** | `8b69d10a` |
| **Diagnostics** | PRE0070 `CrossCurrencyArithmetic`, PRE0071 `CrossDimensionArithmetic`, PRE0072 `DenominatorUnitMismatch`, PRE0073 `DimensionCategoryMismatch`, PRE0074 `CompoundPeriodDenominator` |
| **Files modified** | `src/Precept/Pipeline/TypeChecker.Expressions.cs` |
| **Tests added** | Positive + negative cases for each qualifier axis |
| **Notes** | Dynamic qualifiers (`money in '{CatalogCurrency}'`) silently skipped per Q2 decision (ProofEngine Strategy 5 handles at runtime) |

### Slice 2 — B3: Choice Value Validation (PRE0086, PRE0087, PRE0089)

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Commit** | `dc2d1f0e` |
| **Diagnostics** | PRE0086 `ChoiceLiteralNotInSet`, PRE0087 `ChoiceArgOutsideFieldSet`, PRE0089 `ChoiceRankConflict` |
| **Files modified** | `src/Precept/Pipeline/TypeChecker.Expressions.cs` |
| **Tests added** | Choice literal validation, arg validation, rank conflict detection |

### Slice 3 — PRE0094 InitialEventMissingAssignments

| Field | Value |
|-------|-------|
| **Status** | ✅ No work needed |
| **Diagnostics** | PRE0094 — already wired in `TypeChecker.Validation.FieldState.cs` |
| **Notes** | Confirmed wired during Q3 sequencing review. Gap inventory was stale. |

### Slice 4 — PRE0092 EventHandlerInStatefulPrecept

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Commit** | `12a3f8e6` |
| **Diagnostics** | PRE0092 `EventHandlerInStatefulPrecept` |
| **Files modified** | `src/Precept/Pipeline/TypeChecker.Validation.cs` (structural check in `ValidateStructural`) |
| **Tests added** | Positive (stateful precept with event handler) + negative (stateless precept with event handler) |

### Slice 5 — Temporal Constant Precision (PRE0055–0058)

| Field | Value |
|-------|-------|
| **Status** | ✅ Subsumed by Slice 9B |
| **Notes** | Slice 9B (catalog-mediated typed-constant family diagnostics) shipped first and covered PRE0055–0058 automatically via `TypedConstantFamilyMeta.FormatErrorCode`/`SemanticErrorCode`. No separate Slice 5 implementation needed. |

### Slice 5A — PRE0091 AmbiguousTypedConstant

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Commit** | `9292aa1b` |
| **Diagnostics** | PRE0091 `AmbiguousTypedConstant` |
| **Files modified** | TypeChecker typed-constant candidate arbitration path |
| **Tests added** | Ambiguity, unique-candidate, zero-candidate, explicit-context cases |
| **Notes** | Sequenced after Slices 1+2+4 to let typed-constant split stabilize |

### Slice 6 — B4: Collection Safety Extensions (PRE0100, PRE0104)

| Field | Value |
|-------|-------|
| **Status** | ✅ Partial — 2 wired, 2 staged |
| **Commit** | `196ad9f1` |
| **Diagnostics wired** | PRE0100 `IndexBoundsGuard`, PRE0104 `MissingOrderingKey` |
| **Diagnostics staged** | PRE0099 `KeyPresenceSafety`, PRE0101 `KeyUniquenessGuard` |
| **Files modified** | `ProofEngine.Strategies.cs`, `ProofEngine.Diagnostics.cs`, `ProofEngine.cs`, `ProofRequirementKind.cs`, `ProofRequirement.cs`, `ProofRequirements.cs`, `TypeChecker.Expressions.Callables.cs`, `DiagnosticCoverageAllowLists.cs` |
| **Tests added** | 10 tests in `TypeCheckerCollectionSafetyTests` |
| **Allow-list changes** | Removed: `IndexBoundsGuard`, `MissingOrderingKey`. Retained: `KeyPresenceSafety`, `KeyUniquenessGuard` |
| **Notes** | See §4 for staged infrastructure details |

### Slice 7 — Parser Guard Gates (PRE0013–0015)

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Commit** | `84def08a` |
| **Diagnostics** | PRE0013 `OmitDoesNotSupportGuard`, PRE0014 `EventHandlerDoesNotSupportGuard`, PRE0015 `TransitionGuardMustFollowEvent` |
| **Files modified** | `src/Precept/Pipeline/Parser.cs` |
| **Tests added** | 6 tests (positive + negative per code) in `ParserGuardValidationTests.cs` |
| **Notes** | Parser recovery after each rejection — continues parsing remaining constructs |

### Slice 8 — Scattered TypeChecker Gaps (10 codes)

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Commit** | `7061744c` |
| **Diagnostics** | PRE0027 `DuplicateArgName`, PRE0035 `InvalidModifierValue`, PRE0039 `ComputedFieldWithDefault`, PRE0042 `ConflictingAccessModes`, PRE0043 `RedundantAccessMode`, PRE0044 `ListLiteralOutsideDefault`, PRE0050 `EventArgOutOfScope`, PRE0067 `MaxPlacesExceeded`, PRE0085 `ValueNotInChoiceSet`, PRE0105 `CollectionElementTypeMismatch` |
| **Files modified** | `TypeChecker.Validation.cs`, `TypeChecker.Expressions.cs`, multiple test files |
| **Tests added** | Positive + negative pair per code |
| **Deferred from this slice** | PRE0022, PRE0048, PRE0051 (need deeper analysis — may be covered by `TypeMismatch`), PRE0019 (retired, subsumed by PRE0116) |

### Slice 9A — Modifier Constraint Catalog Mediation

| Field | Value |
|-------|-------|
| **Status** | ❌ Closed — not viable |
| **Commit** | `e6ce1a95` |
| **Audit finding** | `ValidateModifiers` contains only 2 identity-specific branches (WritableOnEventArg, ComputedFieldNotWritable) — both context-dependent on the same modifier (Writable). `ValidateModifierValues` branches all emit the same code (InvalidModifierValue). Threshold ≥3 not met. |

### Slice 9B — Catalog-Mediated Typed-Constant Family Diagnostics (PRE0055–0058)

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Commit** | `ff58d19d` |
| **Diagnostics** | PRE0055 `InvalidTimezoneId`, PRE0056 `UnqualifiedPeriodArithmetic`, PRE0057 `MissingTemporalUnit`, PRE0058 `FractionalUnitValue` |
| **Mechanism** | `TypedConstantFamilyMeta.FormatErrorCode` + `SemanticErrorCode` — validation reads family metadata for diagnostic selection |
| **Files modified** | Typed-constant family catalog, `TypedConstantValidation.cs`, `DiagnosticCoverageAllowLists.cs` |
| **Tests added** | Positive + negative per domain-specific temporal code |
| **Notes** | Subsumes Slice 5 — catalog-mediated approach covers temporal diagnostics automatically |

### Slice 9C — Catalog-Mediated ProofRequirement DiagnosticCode

| Field | Value |
|-------|-------|
| **Status** | ✅ Complete |
| **Commit** | `51c7c03c` |
| **What was built** | Added `DiagnosticCode` property to `ProofRequirementMeta` base. Refactored `CreateFaultSiteLink` to use `ProofRequirements.GetMeta(kind).DiagnosticCode` for all non-Numeric kinds. |
| **Documented exceptions** | (1) `Numeric` kind retained as direct emission — 1:many context-dependent mapping (DivisionByZero, SqrtOfNegative, etc.). (2) `KeyPresence` kind dispatches based on `RequireAbsence` flag (PRE0099 vs PRE0101). (3) `CreateDiagnostic` retained as legitimate per-obligation formatting. |
| **Strategy 7 verification** | Confirmed: `ProofRequirements.GetMeta(ProofRequirementKind.IntervalContainment).DiagnosticCode == DiagnosticCode.NumericOverflow` |
| **Files modified** | `ProofRequirement.cs` (meta records), `ProofRequirements.cs`, `ProofEngine.Diagnostics.cs` |

---

## 4. Staged Infrastructure: PRE0099 and PRE0101

### What is staged

Full proof-engine infrastructure for key-presence safety is committed and functional — it lacks only the **obligation generation trigger** because the Lookup type does not yet expose the required accessors/actions.

| Component | File | What exists |
|-----------|------|-------------|
| Proof requirement kind | `src/Precept/Language/ProofRequirementKind.cs` | `KeyPresence = 10` |
| Proof requirement record | `src/Precept/Language/ProofRequirement.cs` | `KeyPresenceProofRequirement(Subject, RequireAbsence, Description)` — `RequireAbsence=false` → PRE0099, `RequireAbsence=true` → PRE0101 |
| Catalog metadata | `src/Precept/Language/ProofRequirements.cs` | `ProofRequirementMeta.KeyPresence` entry in `GetMeta` switch |
| Strategy implementation | `src/Precept/Pipeline/ProofEngine.Strategies.cs` | `GuardHasContainsCheck(guard, fieldName, requireNegated)` + `WalkForContains` recursive walker — matches `Field contains X` (positive) and `not (Field contains X)` (negative) guard patterns |
| Diagnostic dispatch | `src/Precept/Pipeline/ProofEngine.Diagnostics.cs` | `CreateDiagnostic` handles `KeyPresenceProofRequirement` — selects PRE0099 or PRE0101 based on `RequireAbsence` flag |
| Fault-site link routing | `src/Precept/Pipeline/ProofEngine.Diagnostics.cs` | `CreateFaultSiteLink` dispatches KeyPresence to `FaultCode.CollectionEmptyOnAccess` (PRE0099) or `FaultCode.CollectionEmptyOnMutation` (PRE0101) |
| Constraint type | `src/Precept/Pipeline/ProofEngine.cs` | `ContainsGuardConstraint` record for strategy internals |
| Allow-list entries | `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` | `"KeyPresenceSafety"` and `"KeyUniquenessGuard"` remain in Gate 1 allow-list |

### What is missing

| Blocked code | Missing prerequisite | What to add |
|--------------|---------------------|-------------|
| **PRE0099** `KeyPresenceSafety` | Lookup type has no key-access accessor (only `.count` exists today) | Add a key-access accessor to `TypeKind.Lookup` in the Types catalog (e.g. `[key]` subscript or `.get(key)` callable). The obligation generator must create a `KeyPresenceProofRequirement(Subject, RequireAbsence: false, ...)` when this accessor is resolved on a Lookup field. |
| **PRE0101** `KeyUniquenessGuard` | Lookup type has no `put` action (only `Add` exists, targeting Set/Bag) | Add a `put` action to `TypeKind.Lookup` in the Actions catalog. The obligation generator must create a `KeyPresenceProofRequirement(Subject, RequireAbsence: true, ...)` when `put` is resolved on a Lookup field — the key must NOT already be present. |

### How to complete the wiring

1. **Add the Lookup accessor** — extend the Types catalog entry for `TypeKind.Lookup` to expose a key-access accessor (subscript or method). This gives the type checker and proof engine a resolution target.
2. **Add the Lookup `put` action** — extend the Actions catalog to include a `put` action for Lookup. This parallels `Add` for Set/Bag but with key-uniqueness semantics.
3. **Wire obligation generation** — in the proof-obligation collection pass (likely `ProofEngine.CollectObligations` or the TypeChecker callable resolution), emit `KeyPresenceProofRequirement` when the new accessor/action is resolved on a Lookup field.
4. **Remove from allow-list** — delete `"KeyPresenceSafety"` and `"KeyUniquenessGuard"` from `DiagnosticCoverageAllowLists.Gate1AllowList`.
5. **Add tests** — positive (access without contains guard → diagnostic fires) and negative (access with contains guard → no diagnostic).

The strategy (`GuardHasContainsCheck`) and diagnostic dispatch are already committed and do not need modification.

---

## 5. Audit Findings

### Slice 9A — Not Viable

**Conclusion:** Closed. Catalog mediation for modifier constraint violations does not meet the ≥3 branch threshold.

**Audit details:**
- `ValidateModifiers` has only 2 identity-specific branches: `WritableOnEventArg` and `ComputedFieldNotWritable`
- Both are context-dependent checks on the same modifier (Writable) — not a pure 1:1 identity→code mapping
- `ValidateModifierValues` branches all emit the same code (`InvalidModifierValue`) — no per-identity diagnostic differentiation
- The governing policy requires ≥3 branches with identical "check constraint → emit unique code" shape

### Slice 9C — Completed with Documented Exceptions

**Conclusion:** Complete. All ProofEngine emission paths use catalog dispatch except three documented exceptions.

**Documented exceptions:**
1. **Numeric kind** — 1:many context-dependent mapping. A single `NumericProofRequirement` can fail as `DivisionByZero`, `SqrtOfNegative`, or other codes depending on the operation context. Direct emission via `GetNumericRequirementDiagnosticCode` is correct.
2. **KeyPresence kind** — 1:2 mapping based on `RequireAbsence` flag. Dispatches to PRE0099 or PRE0101. Direct field-based dispatch is correct (not the catalog's 1:1 `DiagnosticCode` property).
3. **`CreateDiagnostic` method** — per-obligation formatting. Each obligation kind requires specific message arguments (field names, bounds, etc.). This method legitimately uses per-kind formatting branches.

**Strategy 7 verification:** `ProofRequirements.GetMeta(ProofRequirementKind.IntervalContainment).DiagnosticCode == DiagnosticCode.NumericOverflow` — confirmed.

---

## 6. Allow-List Status

### Gate 1 — Codes removed during this mission

| Slice | Codes removed |
|-------|---------------|
| 1 | `CrossCurrencyArithmetic`, `CrossDimensionArithmetic`, `DenominatorUnitMismatch`, `DimensionCategoryMismatch`, `CompoundPeriodDenominator` |
| 2 | `ChoiceLiteralNotInSet`, `ChoiceArgOutsideFieldSet`, `ChoiceRankConflict` |
| 4 | `EventHandlerInStatefulPrecept` |
| 5A | `AmbiguousTypedConstant` |
| 6 | `IndexBoundsGuard`, `MissingOrderingKey` |
| 7 | `OmitDoesNotSupportGuard`, `EventHandlerDoesNotSupportGuard`, `TransitionGuardMustFollowEvent` |
| 8 | `DuplicateArgName`, `InvalidModifierValue`, `ComputedFieldWithDefault`, `ConflictingAccessModes`, `RedundantAccessMode`, `ListLiteralOutsideDefault`, `EventArgOutOfScope`, `MaxPlacesExceeded`, `ValueNotInChoiceSet`, `CollectionElementTypeMismatch` |
| 9B | `InvalidTimezoneId`, `UnqualifiedPeriodArithmetic`, `MissingTemporalUnit`, `FractionalUnitValue` |

### Gate 1 — Codes still on allow-list

| Code | Reason |
|------|--------|
| `KeyPresenceSafety` | Staged — blocked on Lookup key-access accessor |
| `KeyUniquenessGuard` | Staged — blocked on Lookup put action |
| `NonAssociativeComparison` | D1: parser expression precision (deferred) |
| `UnexpectedKeyword` | D1: parser expression precision (deferred) |
| `InvalidCallTarget` | D1: parser expression precision (deferred) |
| `NullInNonNullableContext` | Retired — subsumed by PRE0116 (pending removal from enum) |
| `FunctionArgConstraintViolation` | Precision upgrade — TypeMismatch fires instead |
| `ScalarOperationOnCollection` | Precision upgrade — TypeMismatch fires instead |
| `InvalidInterpolationCoercion` | Precision upgrade — TypeMismatch fires instead |
| `OutOfRange` | Deferred — constant-literal bounds check (decision: option a confirmed) |
| `CollectionOperationOnScalar` | Pre-existing — no emission site wired |
| `InvalidTypedConstantContent` | Pre-existing — no emission site wired |
| `InvalidDateValue` | Pre-existing — no emission site wired |
| `InvalidDateFormat` | Pre-existing — no emission site wired |
| `InvalidTimeValue` | Pre-existing — no emission site wired |
| `InvalidInstantFormat` | Pre-existing — no emission site wired |
| `NonOrderableCollectionExtreme` | Pre-existing — no emission site wired |
| `UnsatisfiableGuard` | Pre-existing — no emission site wired |
| `DivisionByZero` | Pre-existing — no emission site wired |
| `SqrtOfNegative` | Pre-existing — no emission site wired |
| `ChoiceElementTypeMismatch` | Pre-existing — no emission site wired |
| `ChoiceMissingElementType` | Pre-existing — no emission site wired |

### Gate 2 status

Gate 2 allow-list is populated with cross-project detection entries — the analyzer cannot detect test references in `Precept.Tests` (separate project). All codes listed in Gate 2 have confirmed tests; the allow-list exists to suppress false positives from the cross-project boundary.

---

## 7. Pre-Existing Test Failures

7 ProofEngine test failures exist from prior work (pre-dating this enforcement mission). These are **unrelated** to any enforcement slice and were not introduced or affected by this implementation. They need a separate cleanup pass — likely related to proof-engine refactoring for interval containment or presence obligations.

---

## 8. Remaining Work

| Item | Unblock condition | Priority |
|------|-------------------|----------|
| **PRE0099** `KeyPresenceSafety` | Add key-access accessor to `TypeKind.Lookup` in Types catalog | Medium — blocked on Lookup type expansion |
| **PRE0101** `KeyUniquenessGuard` | Add `put` action for Lookup in Actions catalog | Medium — blocked on Lookup type expansion |
| **PRE0079** `OutOfRange` | None — ready to implement. Wire constant-literal bounds check in TypeChecker (option a confirmed). | Medium |
| **PRE0060–0062** Period arithmetic safety | B2 pattern establishment (Slice 1 shipped — pattern is now available) | Medium |
| **PRE0022, PRE0051** Precision upgrades | Deeper analysis — may already be covered under `TypeMismatch` | Low |
| **PRE0010–0012** Parser expression precision | None — lower priority | Low |
| **PRE0019** Retirement | Remove from `DiagnosticCode` enum (architecturally subsumed by PRE0116) | Low |
| **7 ProofEngine test failures** | Separate cleanup pass | Medium |

---

## 9. Cross-Plan Interaction: Interval Proof Engine

The interval proof engine plan (`docs/Working/interval-proof-engine-design.md`) interacts with diagnostic enforcement at two points:

1. **PRE0078 `NumericOverflow`** — emitted by interval proof engine Strategy 7 (`IntervalContainment`). This code was never in enforcement plan scope. Gate 1 allow-list removal for PRE0078 is coordinated with interval engine Slice 2 shipping — already verified by Slice 9C that `ProofRequirements.GetMeta(ProofRequirementKind.IntervalContainment).DiagnosticCode == DiagnosticCode.NumericOverflow`.

2. **PRE0079 `OutOfRange`** boundary — interval engine owns expression-level bounds (computed intervals), enforcement plan owns literal-assignment bounds (TypeChecker). These are complementary: PRE0079 fires on `set field to 42` where `42 > max 10`; PRE0078 fires on `set field to X + Y` where the computed interval exceeds bounds. No overlap because TypeChecker runs before ProofEngine and the Q10 literal dedup gate prevents double collection.

3. **Slice 9C verification** — confirmed Strategy 7 uses catalog-mediated dispatch (`ProofRequirementMeta.IntervalContainment.DiagnosticCode`), not a hardcoded literal. The interval engine is aligned with catalog-driven architecture.

---

## 10. Implementer Notes (George)

These notes were recorded by George during and after the enforcement mission and capture the implementation-floor perspective not visible from architecture review alone.

### File Conflict Groups — Wave Serialization Rationale

Three files had multi-slice ownership, forcing wave serialization within each group:

| File | Slices | Resolution |
|------|--------|-----------|
| `TypeChecker.Expressions.cs` | 1, 2, 8 | Serialized: Wave 1 (Slice 1) → Wave 2 (Slice 2) → Wave 3 (Slice 8) |
| `TypeChecker.Validation.cs` | 4, 8, 9A | Wave 1 (Slice 4) → Wave 3 (Slice 8) → Wave 4 (Slice 9A audit) |
| `Parser.cs` | 7 only | No conflict — Wave 1 parallel |

All other slices were independent and ran in Wave 1 in parallel. File conflict analysis — not domain dependency — was the primary wave-ordering constraint.

### Slice 9B Subsuming Slice 5

Slice 9B (catalog-mediated dispatch in ProofEngine) was scoped to subsume Slice 5 (temporal constant ambiguity / PRE0055–0058) via a catalog-mediated approach. The `TypedConstantFamilyMeta.FormatErrorCode` / `SemanticErrorCode` pattern is architecturally superior to direct temporal emission because it keeps the dispatch table in metadata rather than control flow. Slice 5 as originally spec'd would have been an independent TypeChecker wire; the merged approach is cleaner.

### Staged Infrastructure (PRE0099/0101) — Implementation Posture

The PRE0099/0101 staged work (`KeyPresenceProofRequirement`, `GuardHasContainsCheck`, `ContainsGuardConstraint`) is fully functional code — not a scaffold or placeholder. The strategy was written against the Lookup type as it will exist after catalog expansion. When the key-access accessor and `put` action land in the catalog, the proof engine strategy requires no changes; only the TypeChecker obligation collection site and the allow-list entry need updating.

### Distilled Lesson

> Keep enforcement work catalog-driven; preserve explicit carve-outs for legitimate direct emissions (1:many mappings, context-dependent dispatch); treat stage boundaries and durable logs as the primary review surface — not inline code comments.
