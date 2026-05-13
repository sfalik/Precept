# Diagnostic Gap Analysis

> Working document — Frank, 2026-05-13

## Executive Summary

Of the 132 diagnostics defined in `DiagnosticCode.cs`, 54 have no emission site in the pipeline — they are declared with full catalog metadata (and often with tests), but the compiler never produces them. This means Precept's language spec promises validation it does not deliver. For a product whose identity is "invalid configurations are structurally impossible," unenforced diagnostics are an integrity debt: the spec says the compiler catches cross-currency arithmetic, invalid dates, and choice-set violations — and it doesn't.

**Correction to input analysis:** The CI enforcement cluster (PRE0066, PRE0095, PRE0097, PRE0098) was incorrectly reported as "never emitted." These ARE emitted by `ValidateCIEnforcement` in `TypeChecker.Validation.cs` via catalog-driven dispatch through `Operations.GetMeta().CIDiagnosticCode` and `Functions.GetMeta().CIDiagnosticCode`. The original grep missed them because emission uses indirect catalog references, not literal `DiagnosticCode.CaseInsensitive*` identifiers in the pipeline. **These 4 diagnostics are not gaps — they are working correctly.** The true gap count is **50**, not 54.

---

## Gap Inventory by Root Cause

### Root Cause A: Parser Gates Never Wired (PRE0013, PRE0014, PRE0015)

**What these were supposed to do:**
- **PRE0013 `OmitDoesNotSupportGuard`**: Reject `omit Field in State when Guard` — `omit` is unconditional structural exclusion.
- **PRE0014 `EventHandlerDoesNotSupportGuard`**: Reject `on Event when Guard -> actions` — event handlers don't support guards, only transition rows do.
- **PRE0015 `PreEventGuardNotAllowed`**: Reject `from State when Guard on Event -> ...` — guard must come after `on Event`, not before.

**Why they're missing:** The parser grew construct dispatch and slot resolution for guards, transitions, and event handlers, but the specific rejection paths for these three invalid forms were never wired. The parser silently accepts these constructs (or misparses them) rather than emitting the diagnostic.

**Current user experience:** A user who writes `omit Notes in Draft when Notes is set` will either get a confusing parse error about unexpected tokens, or the guard will be silently ignored. The spec (§2.7) explicitly promises these diagnostics.

**Spec reference:** All three appear in §2.7 Parser Diagnostics table.

### Root Cause B: TypeChecker Domain Logic Not Implemented

Five complete domain clusters have full catalog metadata and (in most cases) tests that assert the diagnostic should fire, but the TypeChecker has no code to detect the condition or emit the diagnostic.

#### B1: Temporal Constant Validation (PRE0055–PRE0062)

| PRE | Name | Intended enforcement |
|-----|------|---------------------|
| PRE0055 | `InvalidDateValue` | `'2024-02-30'` — calendar date doesn't exist |
| PRE0056 | `InvalidDateFormat` | `'30-02-2024'` — wrong format, should be YYYY-MM-DD |
| PRE0057 | `InvalidTimeValue` | `'25:61:00'` — hours/minutes/seconds out of range |
| PRE0058 | `InvalidInstantFormat` | `'2024-01-01T00:00:00'` — missing trailing Z |
| PRE0059 | `InvalidTimezoneId` | `'US/Eastern'` — not canonical IANA form |
| PRE0060 | `UnqualifiedPeriodArithmetic` | `date + unconstrained_period` — period may have mixed components |
| PRE0061 | `MissingTemporalUnit` | Bare number in temporal arithmetic context |
| PRE0062 | `FractionalUnitValue` | `'1.5 months'` — temporal units must be whole |

**What the TypeChecker currently does:** The typed constant pipeline resolves temporal strings to NodaTime types via `TypedConstantValidation`. However, it uses `UnresolvedTypedConstant` (PRE0052) and `InvalidTypedConstantContent` (PRE0053) as catch-all errors. The domain-specific temporal diagnostics (PRE0055–0058) that would give precise, actionable feedback ("this date doesn't exist" vs. "this format is wrong") are never selected.

For PRE0060–0062 (period arithmetic safety), the type checker resolves temporal binary operations but does not inspect the qualifier context to detect unconstrained period composition or unit mismatches.

**Spec reference:** §3.6 Expression Typing Rules explicitly lists `UnqualifiedPeriodArithmetic` as the diagnostic for `date ± period` when the period is unconstrained. The temporal type system doc references the full diagnostic set.

**Current user experience:** `field D as date default '2024-02-30'` produces either a generic `InvalidTypedConstantContent` error or, worse, silently parses as an error type and cascades. The user never sees "February 30 doesn't exist."

#### B2: Currency/Unit Arithmetic Safety (PRE0070–PRE0074)

| PRE | Name | Intended enforcement |
|-----|------|---------------------|
| PRE0070 | `CrossCurrencyArithmetic` | `USD_amount + EUR_amount` — different currencies |
| PRE0071 | `CrossDimensionArithmetic` | `'5 kg' + '3 mi'` — mass ≠ length |
| PRE0072 | `DenominatorUnitMismatch` | `price in 'USD/kg' * quantity in 'mi'` |
| PRE0073 | `DurationDenominatorMismatch` | `price in 'USD/days' * duration` — variable-length |
| PRE0074 | `CompoundPeriodDenominator` | `period in 'hours&minutes' * price in 'USD/hours'` |

**What the TypeChecker currently does:** The type checker resolves business-domain binary operations through the Operations catalog, producing correct result types for valid combinations. But it does not check qualifier compatibility — when the Operations catalog says "same currency required," the checker doesn't verify the currencies actually match. The catalog metadata exists (`CrossDimensionArithmetic` related codes are wired), but no emission call runs in the pipeline.

**Spec reference:** §3.6 Business-domain operators explicitly says "Same currency required" for money±money, and the business-domain-types doc lists all five diagnostics in its enforcement table.

**Current user experience:** `field Total as money in 'USD' <- CostUSD + CostEUR` compiles clean. This is **silent wrong behavior** — the product's core promise is that this cannot happen.

#### B3: Choice Type Runtime Semantics (PRE0086–PRE0090)

| PRE | Name | Intended enforcement |
|-----|------|---------------------|
| PRE0086 | `ChoiceLiteralNotInSet` | `Status == "Pending"` where "Pending" isn't a declared value |
| PRE0087 | `ChoiceArgOutsideFieldSet` | Arg choice includes values outside field's declared set |
| PRE0088 | `ChoiceElementTypeMismatch` | `choice of integer` arg to `choice of string` field |
| PRE0089 | `ChoiceRankConflict` | Arg order conflicts with field's declared order |
| PRE0090 | `ChoiceMissingElementType` | `choice(...)` without `of T` |

**What the TypeChecker currently does:** Parse-stage validation catches PRE0088 and PRE0090 during `ParseTypeRef()` — these are correctly emitted at parse time (confirmed in Parser.cs). However, PRE0086, PRE0087, and PRE0089 are type-checker-stage checks that require comparing expression literals against resolved choice field declarations, and that comparison logic doesn't exist.

**Spec reference:** §3.8 Choice type validation table explicitly lists all five diagnostics and their firing conditions.

**Current user experience:** `rule Status == "Pending"` where Status is `choice of string("Active", "Done")` compiles clean. The user made a typo; the compiler doesn't catch it.

#### B4: Collection Safety Guards (PRE0099–PRE0101, PRE0104)

| PRE | Name | Intended enforcement |
|-----|------|---------------------|
| PRE0099 | `KeyPresenceSafety` | Key-based access without `contains` guard |
| PRE0100 | `IndexBoundsGuard` | Index-based access without bounds guard |
| PRE0101 | `KeyUniquenessGuard` | `put` without uniqueness guard |
| PRE0104 | `MissingOrderingKey` | `min`/`max` on unordered collection |

**What the TypeChecker currently does:** The existing collection safety system emits PRE0063 `UnguardedCollectionAccess` and PRE0064 `UnguardedCollectionMutation` for `.peek`/`.min`/`.max` and `pop`/`dequeue` operations. But the newer collection types (list, lookup, queue-by-priority) have additional safety requirements — key-based access needs `contains` guards, index-based access needs bounds guards — and these emission sites were never added.

**Spec reference:** The collection-types doc defines these safety requirements. The catalog entries have `PreventsFault` links to `CollectionEmptyOnAccess` and related fault codes.

**Current user experience:** `Lookup for "missing_key"` compiles without requiring `when Lookup contains "missing_key"`. At runtime, this would fault.

### Root Cause C: Completely Unused — Never Specced or Stale (7 diagnostics)

| PRE | Name | Assessment |
|-----|------|-----------|
| PRE0013 | `OmitDoesNotSupportGuard` | Specced (§2.7) — **not stale, reclassify as Root Cause A** |
| PRE0014 | `EventHandlerDoesNotSupportGuard` | Specced (§2.7) — **not stale, reclassify as Root Cause A** |
| PRE0015 | `PreEventGuardNotAllowed` | Specced (§2.7) — **not stale, reclassify as Root Cause A** |
| PRE0043 | `RedundantAccessMode` | Specced (§3.8) — **implementation gap, not stale** |
| PRE0079 | `OutOfRange` | Specced as type-stage, but semantically requires runtime or proof. **Misclassified stage.** |
| PRE0091 | `AmbiguousTypedConstant` | Designed for multi-candidate typed constant resolution. Currently single-candidate resolution makes this unreachable. **Latent — keep for when resolution gains multiple candidates.** |
| PRE0092 | `EventHandlerInStatefulPrecept` | Specced (§3.8 stateless/stateful cross-validation). **Implementation gap.** |

**Revised assessment:** Only PRE0091 is truly latent/speculative. The others are specced but unenforced.

### Root Cause D: Scattered Parse/Type Errors (17 diagnostics)

Grouped by sub-theme:

#### D1: Parser expression validation (PRE0010, PRE0011, PRE0012)

| PRE | Name | Status |
|-----|------|--------|
| PRE0010 | `NonAssociativeComparison` | Specced §2.7. Parser currently doesn't detect chained comparisons. |
| PRE0011 | `UnexpectedKeyword` | Specced §2.7. Parser currently emits `ExpectedToken` instead. |
| PRE0012 | `InvalidCallTarget` | Specced §2.7. Parser currently emits generic error. |

These are precision downgrades — the parser catches the error condition but emits a generic `ExpectedToken` instead of the domain-specific diagnostic.

#### D2: Type-checker validation gaps (14 diagnostics)

| PRE | Name | Sub-theme | Spec status |
|-----|------|-----------|-------------|
| PRE0019 | `NullInNonNullableContext` | Null safety | Implicit in type rules |
| PRE0022 | `FunctionArgConstraintViolation` | Function args | Specced in §3.7 |
| PRE0027 | `DuplicateArgName` | Duplicate detection | Specced §3.8 |
| PRE0035 | `InvalidModifierValue` | Modifier validation | Specced §3.8 |
| PRE0039 | `ComputedFieldWithDefault` | Computed field | Specced §3.8 |
| PRE0042 | `ConflictingAccessModes` | Access modes | Specced §3.8 |
| PRE0044 | `ListLiteralOutsideDefault` | List literals | Specced §3.8 |
| PRE0050 | `EventArgOutOfScope` | Scope rules | Specced §3.5 |
| PRE0051 | `InvalidInterpolationCoercion` | Interpolation | Specced §3.6 |
| PRE0067 | `MaxPlacesExceeded` | Business domain | Specced |
| PRE0078 | `NumericOverflow` | Value safety | Proof-level concern |
| PRE0085 | `NonChoiceAssignedToChoice` | Choice types | Specced §3.8 |
| PRE0094 | `InitialEventMissingAssignments` | Lifecycle | Specced §3A.5 |
| PRE0105 | `CollectionInnerTypeError` | Collections | Specced |

**Note on PRE0094:** This was already identified as a blocking gap in the v3 field-state-guarantees work (see history.md). D93 IS emitted; D94 is not. They are a matched pair — D93 says "you need an initial event," D94 says "your initial event doesn't assign all required fields."

---

## Impact Assessment

### Priority by integrity risk

| Root Cause | User Impact | Spec Integrity | AI Legibility |
|------------|-------------|----------------|---------------|
| **B2: Currency/unit arithmetic** | **Silent wrong behavior.** Cross-currency arithmetic compiles clean. This directly contradicts Precept's core promise. | Spec explicitly promises this validation. | MCP `precept_diagnostic` returns PRE0070 with full trigger/recovery info — AI consumers believe the enforcement exists. |
| **B3: Choice validation** | **Silent wrong behavior.** Non-existent choice values pass type checking. | Spec §3.8 lists these checks explicitly. | Same MCP gap — diagnostics appear real to AI. |
| **B1: Temporal validation** | **Precision loss.** Bad dates get a generic error instead of a precise one. Less severe than B2 because the error IS caught, just with wrong code. | Spec describes the specific diagnostics. | AI gets wrong diagnostic for temporal errors. |
| **B4: Collection safety** | **Runtime faults instead of compile errors.** Missing key/index/bounds guards slip through. Proof engine may catch some via existing `UnguardedCollectionAccess`. | Spec and collection-types doc describe the requirements. | AI consumers don't know which specific safety check is needed. |
| **A: Parser gates** | **Confusing errors.** Invalid syntax gets generic parse error. | Spec §2.7 lists the specific diagnostics. | Minor — syntax errors are syntax errors. |
| **D: Scattered** | **Mixed.** Some are precision downgrades (D1), some are genuine gaps (D94, D42). | Most specced. | Varies. |

### Core philosophy violation

The currency/unit gap (B2) is the most serious because it directly violates `docs/philosophy.md`'s foundational claim: "No errors. No bugs. Business logic cannot produce a wrong answer." Cross-currency arithmetic producing a wrong answer is exactly the class of bug Precept promises to eliminate. The spec says the compiler catches it. The compiler doesn't.

---

## Remediation Options per Gap Group

### B2: Currency/Unit Arithmetic (PRE0070–PRE0074)

**Option 1: Wire qualifier comparison in TypeChecker expression resolution**
- What: When the Operations catalog resolves a business-domain binary operation, add a post-resolution check that compares qualifier values (currency, dimension, unit) of the two operands. Emit the appropriate diagnostic when they mismatch.
- Tradeoff accepted: Must integrate with the existing qualifier resolution system in the ProofEngine, or implement a lighter version in the TypeChecker.
- Catalog impact: None — diagnostics already defined with correct metadata.
- TypeChecker impact: New emission sites in `TypeChecker.Expressions.cs` after binary operation resolution, or a new validation pass.
- Test impact: Tests need to be written in TypeChecker test suite. DiagnosticsTests.cs tests only validate catalog metadata, not emission.

**Option 2: Extend ProofEngine qualifier compatibility checks**
- What: The ProofEngine already checks qualifier compatibility for proof obligations (PRE0114 `UnprovedQualifierCompatibility`). Extend this to also emit the domain-specific diagnostics (PRE0070–0074).
- Tradeoff accepted: Proof engine is a later pipeline stage; the error would surface as a proof failure rather than a type error. This changes the error's position in the diagnostic output but the behavior is correct.

**Frank's recommendation:** Option 1. The Operations catalog already knows these are type-level checks (stage = `Type`). The qualifier comparison belongs in the TypeChecker, close to where binary operations are resolved. The ProofEngine handles provability obligations — "can we prove this division won't be zero?" — not type-level domain rules like "these currencies must match." Keep the boundary clean.

### B3: Choice Type Validation (PRE0086–PRE0089)

**Option 1: Add choice literal/arg comparison in TypeChecker**
- What: When the TypeChecker resolves an expression involving a choice field (comparison, assignment, arg passing), compare the literal or arg's values against the resolved choice field's declared value set. Emit PRE0086 for unknown literals, PRE0087 for arg superset violations, PRE0089 for rank conflicts.
- Tradeoff accepted: Requires the TypeChecker to carry resolved choice value sets through expression typing. This is new plumbing.
- Catalog impact: None.
- TypeChecker impact: New logic in `TypeChecker.Expressions.cs` for choice-aware comparison/assignment.
- Test impact: Tests need full emission coverage. Existing DiagnosticsTests.cs only validates catalog shape.

**Frank's recommendation:** Option 1 is the only viable path. Choice validation is definitionally a type-checker responsibility — it compares a literal against a declared type's value domain. PRE0088 and PRE0090 are already handled at parse time; PRE0086/0087/0089 complete the set.

### B1: Temporal Constant Validation (PRE0055–PRE0062)

**Option 1: Specialize TypedConstantValidation for temporal types**
- What: The existing typed constant resolution path uses `UnresolvedTypedConstant` and `InvalidTypedConstantContent` as catch-alls. Intercept temporal constant resolution and emit the specific diagnostic (PRE0055 for invalid date, PRE0056 for wrong format, etc.) before falling back to the generic.
- Tradeoff accepted: Temporal constant parsing already uses NodaTime; the specialization is about choosing the right diagnostic code rather than changing the validation logic.

**Option 2: Keep generic diagnostics, deprecate PRE0055–0062**
- What: Accept that `InvalidTypedConstantContent` is sufficient, remove the specialized temporal diagnostics.
- Tradeoff accepted: Worse user experience. "Invalid typed constant" is less helpful than "February 30 doesn't exist."

**Frank's recommendation:** Option 1. The whole point of domain-specific diagnostics is precision. "February 30 doesn't exist" is better than "invalid typed constant content." The validation logic already knows the specific failure mode (NodaTime returns it) — we're just not selecting the right diagnostic code.

For PRE0060–0062 (period arithmetic rules), this is more substantive — it requires qualifier-aware type checking in the temporal binary operation path, parallel to the B2 currency/unit work.

### B4: Collection Safety (PRE0099–PRE0101, PRE0104)

**Option 1: Extend ProofEngine collection safety**
- What: The ProofEngine already handles `UnguardedCollectionAccess` (PRE0063) and `UnguardedCollectionMutation` (PRE0064). Extend it to also check key presence (PRE0099), index bounds (PRE0100), key uniqueness (PRE0101), and ordering key requirements (PRE0104).

**Option 2: Add type-checker-level checks for the newer collection types**
- What: Wire emission sites in `TypeChecker.Expressions.Callables.cs` where member access and collection operations are resolved.

**Frank's recommendation:** Depends on the enforcement model. PRE0099 and PRE0100 are proof-level obligations — "prove the key exists before accessing by key" parallels "prove the collection is non-empty before `.peek`." These belong in the ProofEngine. PRE0104 `MissingOrderingKey` is a type-checker structural check — the collection type must declare an ordering key for `.min`/`.max` to be valid. Wire PRE0104 in TypeChecker, PRE0099/0100/0101 in ProofEngine.

### Root Cause A: Parser Gates (PRE0013–PRE0015)

**Option 1: Wire rejection paths in Parser**
- What: Add explicit detection and diagnostic emission in the parser for the three guarded forms that are invalid.

**Frank's recommendation:** Option 1 — straightforward parser work. These are simple pattern detection: check for a `when` token in positions where it's invalid, emit the diagnostic. The spec defines exactly what to reject.

### Category 1 Stragglers

| PRE | Recommendation |
|-----|---------------|
| PRE0043 `RedundantAccessMode` | Wire in `TypeChecker.Validation.cs` alongside existing access mode validation. Specced. |
| PRE0079 `OutOfRange` | **Reclassify as proof-level obligation.** Static bounds checking on constant assignments is feasible; runtime value bounds checking is not. Wire for constant defaults and constant `set` actions only. Flag remaining cases to ProofEngine. |
| PRE0091 `AmbiguousTypedConstant` | **Keep latent.** Current resolution is single-candidate. When multi-candidate resolution ships, this fires. No action needed. |
| PRE0092 `EventHandlerInStatefulPrecept` | Wire in `TypeChecker.Validation.cs` structural validation. Specced §3.8. |
| PRE0094 `InitialEventMissingAssignments` | **Blocking gap, already identified.** v3 field-state-guarantees Slices 10-11 depend on this. Wire alongside D93. |

---

## Proposed Remediation Plan

### Priority 1 — Integrity violations (spec promises validation that doesn't exist, user gets silent wrong behavior)

1. **PRE0070–0074: Currency/unit arithmetic safety.** Cross-currency/cross-dimension arithmetic silently producing wrong answers is a direct violation of Precept's core promise. Must wire qualifier comparison in TypeChecker expression resolution.

2. **PRE0086–0089: Choice value validation.** Non-existent choice values passing type checking undermines closed-set governance. Must wire choice literal/arg comparison in TypeChecker.

3. **PRE0094: InitialEventMissingAssignments.** Required fields not validated on initial event — entity can be created with missing required fields. Already identified as blocking gap.

4. **PRE0092: EventHandlerInStatefulPrecept.** Mixing event handlers and transition rows creates ambiguous execution order. Structural check.

### Priority 2 — User experience gaps (errors caught but with wrong diagnostic, or runtime faults instead of compile errors)

5. **PRE0055–0058: Temporal constant precision.** Dates, times, instants getting generic error instead of precise diagnostic. Better DX, no behavior change.

6. **PRE0099–0101, PRE0104: Collection safety extensions.** Key/index/bounds/ordering safety for newer collection types. Some may already be caught by existing `UnguardedCollectionAccess` — audit before implementing.

7. **PRE0013–0015: Parser guard gates.** Invalid guard positions get confusing errors instead of clear rejections.

8. **PRE0010–0012: Parser expression precision.** Chained comparisons, keywords-as-values, invalid call targets get generic `ExpectedToken` instead of specific diagnostic.

9. **PRE0043: RedundantAccessMode.** Specced, simple structural check.

10. **PRE0060–0062: Period arithmetic safety.** Unconstrained period composition, bare numbers in temporal context, fractional units.

### Priority 3 — Scattered type-checker gaps (individual checks not wired)

11. **PRE0035 `InvalidModifierValue`**, **PRE0039 `ComputedFieldWithDefault`**, **PRE0042 `ConflictingAccessModes`**, **PRE0044 `ListLiteralOutsideDefault`**, **PRE0050 `EventArgOutOfScope`**, **PRE0085 `NonChoiceAssignedToChoice`**, **PRE0027 `DuplicateArgName`**, **PRE0067 `MaxPlacesExceeded`**: These are individual emission sites that need to be added to existing TypeChecker validation methods.

### Priority 4 — Deferred

12. **PRE0091 `AmbiguousTypedConstant`**: Latent. Single-candidate resolution makes it unreachable. Wire when multi-candidate resolution ships.

13. **PRE0079 `OutOfRange`**: Reclassify. Constant-assignment bounds checking is feasible but low priority relative to the integrity violations above. Runtime value bounds checking is a ProofEngine concern.

14. **PRE0019 `NullInNonNullableContext`**, **PRE0022 `FunctionArgConstraintViolation`**, **PRE0051 `InvalidInterpolationCoercion`**, **PRE0078 `NumericOverflow`**: These require deeper analysis of whether the type system's current handling (via `TypeMismatch` or proof obligations) already covers the case under a different diagnostic code. May be precision upgrades rather than true gaps.

---

## Implementation Shape

### Priority 1 items — what the implementation looks like

**PRE0070–0074 (currency/unit arithmetic):**
- **Where:** `TypeChecker.Expressions.cs`, in the binary operation resolution path. After `Operations.GetMeta()` returns the resolved operation, check qualifier compatibility.
- **Logic shape:** The resolved `TypedBinaryOp` has operand expressions with resolved types. For money/quantity/price operands, extract the qualifier (currency code, unit, dimension) from the field declaration. Compare. If mismatched, emit the appropriate diagnostic.
- **Prerequisite:** The TypeChecker must have access to qualifier information from field declarations. This is currently available through `CheckContext.FieldLookup`.
- **Complexity:** Medium. The qualifier resolution is non-trivial — fields may have dynamic qualifiers (e.g., `money in '{CatalogCurrency}'`). The ProofEngine already handles dynamic qualifier resolution (Strategy 5). The TypeChecker may need to handle only static qualifiers and defer dynamic cases to the ProofEngine.
- **Tests:** Write new test class `TypeCheckerCurrencyTests.cs` with positive and negative cases for each diagnostic. Existing DiagnosticsTests.cs only validates catalog metadata.

**PRE0086–0089 (choice validation):**
- **Where:** `TypeChecker.Expressions.cs` or `TypeChecker.Expressions.Callables.cs`, in the literal/comparison resolution path.
- **Logic shape:** When a `TypedLiteral` (string or integer) is compared to or assigned to a choice field, check whether the literal value exists in the field's declared choice set. Requires carrying `ChoiceValues` through the typed field metadata.
- **Complexity:** Low-medium. The choice value set is already parsed and available in the field's type reference. The comparison is string/integer equality against a known set.
- **Tests:** Write `TypeCheckerChoiceTests.cs`.

**PRE0094 (InitialEventMissingAssignments):**
- **Where:** `TypeChecker.Validation.cs`, adjacent to the existing D93 emission site (line 325).
- **Logic shape:** For each initial event, check that every required field (no default, non-optional) has a `set` action in at least one transition row for that event from the initial state. Already designed in v3 field-state-guarantees Slice 11.
- **Complexity:** Medium. Requires traversing transition rows for initial events and checking action targets against required field lists.

**PRE0092 (EventHandlerInStatefulPrecept):**
- **Where:** `TypeChecker.Validation.cs`, in `ValidateStructural`.
- **Logic shape:** If `ctx.States.Count > 0 && ctx.EventHandlers.Count > 0`, emit PRE0092 for each event handler.
- **Complexity:** Trivial.

### Test expectations

The existing tests in `DiagnosticsTests.cs` are catalog metadata tests — they validate that every `DiagnosticCode` has a `GetMeta` entry with correct shape. These tests **will pass regardless** of whether emission is wired. New tests must assert that `Compiler.Compile(source)` produces the expected diagnostic for specific input patterns.

For the CI enforcement cluster (already working), `TypeCheckerCITests.cs` provides the pattern: `TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.X)` and `TypeCheckerTestHelpers.CheckExpectingClean(precept)`.

### Catalog change surface

**None.** All 50 gap diagnostics already have complete catalog entries in `Diagnostics.cs` with correct messages, severities, stages, fix hints, related codes, and example snippets. The gap is emission, not definition.

---

## Open Questions for Shane

1. **PRE0079 `OutOfRange` — compile-time or runtime?** The catalog declares it as `DiagnosticStage.Type` (compile-time), but bounds checking against runtime values (e.g., `set X = Y` where `Y` could be anything) can't be done statically. Do we (a) wire it only for constant assignments where the value is known at compile time, (b) reclassify it as a proof obligation, or (c) make it a runtime fault only?

2. **PRE0070–0074: Dynamic qualifier handling.** When a field has a dynamic qualifier like `money in '{CatalogCurrency}'`, should the TypeChecker (a) skip cross-currency checks for dynamic qualifiers and let the ProofEngine handle them, or (b) emit the diagnostic at compile time with a message noting the dynamic qualifier? The ProofEngine already has Strategy 5 for qualifier resolution.

3. **Priority sequencing.** The v3 field-state-guarantees work (Slices 10-11) already depends on PRE0094. Should PRE0094 be fast-tracked ahead of the currency/unit arithmetic cluster, even though B2 is a higher integrity risk?

4. **PRE0091 `AmbiguousTypedConstant` — remove or keep?** It's the only truly speculative diagnostic with no emission path and no planned feature. Keeping it is harmless but adds catalog surface that AI consumers may find misleading. Remove, or annotate as "reserved for future use"?

5. **Scattered Priority 3 items.** Several of the 14 scattered diagnostics (PRE0019, PRE0022, PRE0051, PRE0078) may already be covered by different diagnostic codes (e.g., `TypeMismatch` catches many cases that a more specific diagnostic would handle better). Should we invest in precision upgrades for these, or accept the current generic handling?

---

## CI Enforcement Correction

The original analysis listed 4 CI diagnostics (PRE0066, PRE0095, PRE0097, PRE0098) as "Category 2: Defined + Tested, Never Emitted." **This is incorrect.** These diagnostics are emitted by `ValidateCIEnforcement` in `TypeChecker.Validation.cs` (lines 700-860) via indirect catalog-driven dispatch:

- `Operations.GetMeta(op).CIDiagnosticCode` emits PRE0066 and PRE0095 for `==` and `!=` on `~string` fields.
- `Functions.GetMeta(func).CIDiagnosticCode` emits PRE0097 and PRE0098 for `startsWith` and `endsWith` on `~string` fields.

The original grep searched for literal `DiagnosticCode.CaseInsensitive*` in the pipeline. The emission uses indirect catalog references (`binaryDiagCode` and `functionDiagCode` variables), which the grep missed. `TypeCheckerCITests.cs` contains 15+ tests that assert correct emission — these tests pass.

**Lesson:** Catalog-driven emission means the diagnostic code name won't appear literally at the emission site. Gap analysis must search for both direct references and catalog-mediated dispatch patterns.

---

## Test Strategy for Gap Remediation

Every gap closure must ship with tests that prove the diagnostic fires on invalid input and does not fire on valid input. The existing test infrastructure provides everything needed — no new framework or base class required.

### Test Pattern: `CheckExpectingError`

The dominant integration test pattern runs the full pipeline (Lexer → Parser → NameBinder → TypeChecker) and asserts a specific `DiagnosticCode` appears at Error severity. This is the pattern to use for all gap tests.

**Template:**

```csharp
[Fact]
public void DescriptiveCondition_EmitsExpectedDiagnostic()
{
    var precept = """
        precept Widget
        // ... minimal DSL that triggers the diagnostic ...
        """;
    TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ExpectedCode);
}
```

**How it works:** `TypeCheckerTestHelpers.Check(preceptText)` runs the full pipeline and collects diagnostics from all stages (manifest, symbols, index). `CheckExpectingError` filters to Error severity and asserts the code is present. `CheckExpectingClean` asserts no Error-severity diagnostics exist (excluding D93 `RequiredFieldsNeedInitialEvent` construction noise).

### Concrete Examples per Gap Cluster

#### B1: Temporal Constant Validation (PRE0055–PRE0062)

```csharp
// PRE0055 — InvalidDateValue: calendar date doesn't exist
[Fact]
public void DateField_February30_EmitsInvalidDateValue()
{
    var precept = """
        precept Widget
        field D as date default '2024-02-30'
        state Open initial
        """;
    TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidDateValue);
}

// PRE0056 — InvalidDateFormat: wrong date format
[Fact]
public void DateField_WrongFormat_EmitsInvalidDateFormat()
{
    var precept = """
        precept Widget
        field D as date default '30-02-2024'
        state Open initial
        """;
    TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.InvalidDateFormat);
}

// PRE0061 — MissingTemporalUnit: bare number in temporal arithmetic
[Fact]
public void DateField_AddBareNumber_EmitsMissingTemporalUnit()
{
    var precept = """
        precept Widget
        field D as date default '2024-01-01'
        field Result as date <- D + 5
        state Open initial
        """;
    TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.MissingTemporalUnit);
}
```

#### B2: Currency/Unit Arithmetic Safety (PRE0070–PRE0074)

```csharp
// PRE0070 — CrossCurrencyArithmetic: adding USD to EUR
[Fact]
public void MoneyFields_DifferentCurrencies_EmitsCrossCurrencyArithmetic()
{
    var precept = """
        precept Widget
        field CostUSD as money in 'USD' default '0.00 USD'
        field CostEUR as money in 'EUR' default '0.00 EUR'
        field Total as money in 'USD' <- CostUSD + CostEUR
        state Open initial
        """;
    TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.CrossCurrencyArithmetic);
}

// PRE0071 — CrossDimensionArithmetic: adding mass to length
[Fact]
public void QuantityFields_DifferentDimensions_EmitsCrossDimensionArithmetic()
{
    var precept = """
        precept Widget
        field Weight as quantity in 'kg' default '0 kg'
        field Length as quantity in 'm' default '0 m'
        field Bad as quantity in 'kg' <- Weight + Length
        state Open initial
        """;
    TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.CrossDimensionArithmetic);
}

// Negative case — same currency is valid
[Fact]
public void MoneyFields_SameCurrency_NoDiagnostic()
{
    var precept = """
        precept Widget
        field Cost1 as money in 'USD' default '0.00 USD'
        field Cost2 as money in 'USD' default '0.00 USD'
        field Total as money in 'USD' <- Cost1 + Cost2
        state Open initial
        """;
    TypeCheckerTestHelpers.CheckExpectingClean(precept);
}
```

#### B3: Choice Type Validation (PRE0086–PRE0089)

```csharp
// PRE0086 — ChoiceLiteralNotInSet: literal not in declared choice set
[Fact]
public void ChoiceField_LiteralNotInSet_EmitsChoiceLiteralNotInSet()
{
    var precept = """
        precept Widget
        field Status as choice of string("Active", "Done")
        state Open initial
        state Closed
        event Close
        from Open on Close when Status == "Pending" -> transition Closed
        """;
    TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ChoiceLiteralNotInSet);
}

// Negative case — valid choice value
[Fact]
public void ChoiceField_ValidLiteral_NoDiagnostic()
{
    var precept = """
        precept Widget
        field Status as choice of string("Active", "Done")
        state Open initial
        state Closed
        event Close
        from Open on Close when Status == "Active" -> transition Closed
        """;
    TypeCheckerTestHelpers.CheckExpectingClean(precept);
}
```

#### B4: Collection Safety Guards (PRE0099–PRE0101, PRE0104)

```csharp
// PRE0099 — KeyPresenceSafety: key access without contains guard
[Fact]
public void LookupField_KeyAccess_WithoutContainsGuard_EmitsKeyPresenceSafety()
{
    var precept = """
        precept Widget
        field Items as lookup of string to number
        field Result as number <- Items for "key1"
        state Open initial
        """;
    TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.KeyPresenceSafety);
}
```

#### Parser Gates (PRE0013–PRE0015)

Parser-level tests use the parser directly, not `TypeCheckerTestHelpers`:

```csharp
// PRE0015 — PreEventGuardNotAllowed: guard before on-event
[Fact]
public void GuardBeforeOnEvent_EmitsPreEventGuardNotAllowed()
{
    var manifest = Precept.Pipeline.Parser.Parse(
        Lexer.Lex("precept W\nstate S initial\nevent E\nfrom S when true on E -> no transition"));
    manifest.Diagnostics.Should().Contain(
        d => d.Code == nameof(DiagnosticCode.PreEventGuardNotAllowed));
}
```

### Positive vs. Negative Test Requirements

**Current practice:** Existing tests overwhelmingly check positive cases only — "this bad input fires the diagnostic." `TypeCheckerCITests.cs` is the notable exception: every violation test has a companion `*_NoDiagnostic()` test with the corrected form.

**Requirement for gap tests:**

| Assertion Type | Requirement | Rationale |
|----------------|-------------|-----------|
| Positive case (fires) | **Required — at least 1 per code.** Use `CheckExpectingError`. | Without this, Gate 2 is meaningless — the code is "referenced" but never proved to fire. |
| Negative case (doesn't fire) | **Required — at least 1 per code.** Use `CheckExpectingClean`. | A diagnostic that fires on valid input is a false positive — the worst kind of bug in a domain-integrity product. The B2 cluster (currency arithmetic) is especially critical: `Cost1 + Cost2` with same currency must NOT fire. |
| Message text | **Not required.** | Current tests don't verify message text. The `DiagnosticsTests` catalog tests already verify `MessageTemplate` correctness via `Create_ProducesCorrectCodeString_ForEveryDiagnosticCode`. Adding message assertions to integration tests adds brittleness without proportionate value. |
| Source span accuracy | **Not required for gap closure, recommended for new parser diagnostics.** | Span accuracy is important for IDE tooling (language server underlines, code actions), but the TypeChecker helper doesn't expose span assertions. Parser diagnostics (PRE0013–0015) should spot-check span correctness since they're the primary consumer of parser span precision. |

### Test Placement Guidance

| Gap Cluster | File | Rationale |
|-------------|------|-----------|
| B1: Temporal (PRE0055–0062) | `TypeCheckerTypedConstantTests.cs` | Temporal constants are typed constant resolution — the existing typed constant test file already covers `UnresolvedTypedConstant` and `InvalidTypedConstantContent`. Add a section for the temporal-specific codes. |
| B2: Currency/Unit (PRE0070–0074) | **New file: `TypeCheckerCurrencyUnitTests.cs`** | No existing file covers cross-qualifier arithmetic validation. The CI tests (`TypeCheckerCITests.cs`) are the closest structural parallel — domain-specific expression validation. A dedicated file keeps the cluster cohesive. |
| B3: Choice (PRE0086–0089) | `TypeCheckerStructuralTests.cs` | Structural tests already contain `EmptyChoice` and `DuplicateChoiceValue`. Choice literal/arg validation is the same domain. Add a section after the existing choice category. |
| B4: Collection Safety (PRE0099–0101, PRE0104) | **New file: `TypeCheckerCollectionSafetyTests.cs`** | Collection safety is a distinct domain from existing collection tests. The existing `TypeCheckerQuantifierTests.cs` covers quantifier predicates; key/index/bounds safety is a separate concern. |
| A: Parser Gates (PRE0013–0015) | `ParserExpressionTests.cs` or **new file: `ParserGuardValidationTests.cs`** | These are parser-level construct validation, not expression parsing. A new file is cleaner, but the existing parser test file is also acceptable since it already covers `AssignmentInExpressionContext`. |
| D: Scattered (PRE0035, PRE0039, etc.) | Existing slice files per-category | Each scattered diagnostic belongs in whichever existing file covers its domain. `InvalidModifierValue` → `TypeCheckerModifierTests.cs`. `ComputedFieldWithDefault` → `TypeCheckerStructuralTests.cs`. Match the existing test organization. |

### Test Naming Convention

Observed pattern from existing tests (consistent across all TypeChecker test files):

```
{Condition}_{InputDescription}_{ExpectedOutcome}
```

Examples from the codebase:
- `TildeEquals_OnCIField_NoDiagnostic`
- `Equals_OnCIField_EmitsTildeEqualsRequired`
- `OptionalField_IsSet_InGuard_ResolvesToBoolean_NoDiagnostic`
- `NonOptionalField_IsSet_InGuard_EmitsIsSetOnNonOptional`

**Rules:**
- PascalCase throughout
- Condition first (what's being tested), input variation second, expected outcome last
- Positive results end with `_NoDiagnostic` or `_ResolvesCleanly`
- Negative results end with `_Emits{DiagnosticName}` or describe the outcome
- No `Test_` prefix, no `Should_` prefix — the method name IS the assertion description
