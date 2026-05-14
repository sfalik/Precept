# Proof Engine Remediation Review — Frank's Exhaustive Assessment

**Date:** 2026-05-13  
**Reviewer:** Frank (Lead/Architect & Language Designer)  
**Scope:** George's Slices 1–4 interval proof engine implementation  
**Model:** Claude Opus 4.6 (exhaustive line-by-line review)

---

## VERDICT: **BLOCKED**

### Summary
George's Slice 1–4 groundwork is architecturally unsound and functionally incomplete. The interval proof engine violates Precept's catalog-driven architecture by hardcoding obligation generation logic in the engine rather than declaring it in metadata. Critical path blocker B1 (zero obligations generated) prevents validation of the entire proof strategy. All 13 failing tests depend on this fix.

---

## Critical Issues (blocking re-review until resolved)

### **B1 — Obligation Collection Generates Zero Obligations (Lines 283–298, ProofEngine.cs)**

**Finding:** The code at lines 283–298 attempts to generate `IntervalContainmentProofRequirement` for Set actions on bounded decimal/number fields. **However, tests expecting 2–3 obligations per precept get 0.** The core issue is that `ExtractBoundsFromField()` returns `null` for both `DeclaredMin` and `DeclaredMax` despite fields declaring bounds in syntax.

**Root Cause:** When the TypeChecker processes field declarations with `min`/`max` modifiers (e.g., `field qty as decimal min 1 max 1000`), it extracts modifier values at TypeChecker.cs:378–383 via `TryGetComparableModifierValue()`. This function correctly parses literal expressions but returns `null` if the modifier's parsed expression is malformed or missing. The condition at line 289 `if (min.HasValue || max.HasValue)` never triggers because both are null, so **no obligations are added**.

**Diagnosis:**
- ✅ Code compiles without errors
- ❌ Tests fail with 0 obligations generated (expect 2+)
- ❌ TypedField.DeclaredMin and DeclaredMax are null at runtime
- ❌ No NumericOverflow diagnostics emitted (consequence)

**Specific Remediation:**
1. **Debug TypeChecker field processing:** Add a unit test that compiles a single field with bounds and verifies `TypedField.DeclaredMin` and `TypedField.DeclaredMax` are non-null after type-checking.
2. **Verify modifier extraction:** Instrument `TryGetComparableModifierValue()` to log parsed expressions and return values.
3. **If bounds are correctly extracted:** The bug is in the condition at line 289. Change to ensure the logic correctly handles null coalescing.

**Expected outcome:** At least 2 IntervalContainmentProofRequirement objects per test precept.

---

### **B2 — Catalog Architecture Violation (Lines 283–298, ProofEngine.cs, vs Actions.cs)**

**Finding:** Interval containment obligation generation is hardcoded in the ProofEngine, **not declared in the Actions catalog.** This directly violates Precept's metadata-driven architecture (see `docs/language/catalog-system.md` § Architectural Identity).

**The Violation:**
- Lines 283–298 hardcode **which actions** (only Set) generate obligations
- Lines 284–286 hardcode **which types** (only Decimal, Number) get obligations
- Line 288 hardcodes **bounds extraction logic** (field DeclaredMin/DeclaredMax)
- Line 291–295 hardcodes the **proof requirement construction**

**Why This Violates Architecture:**
Per `docs/language/catalog-system.md`, domain knowledge belongs in **catalog metadata**, not in pipeline stages. The consumer (ProofEngine) must derive behavior from the catalog, not maintain parallel logic.

**The Correct Architecture:**
```csharp
// Actions.cs — ActionMeta for Set should declare:
ActionKind.Set => new(
    ...,
    ProofRequirements: [
        new IntervalContainmentProofRequirement(
            Subject: new SelfSubject(),
            Predicate: "bounded-decimal-or-number",  // ← NEW: type guard in metadata
            Description: "Set actions on bounded decimal/number fields require interval containment")
    ],
    ...),
```

Then ProofEngine dispatches based on `ActionMeta.ProofRequirements`, never hardcoding per-action logic.

**Specific Remediation:**
1. **Move obligation generation to Actions catalog:** Add `ProofRequirements` field to ActionMeta for Set action that declares an interval containment obligation *template*.
2. **Parameterize the template:** The obligation needs to know (a) target field name, (b) field bounds. These come from the action context, not the catalog. Design a catalog entry that can be instantiated with runtime values.
3. **Delegate to ProofEngine:** Refactor ProofEngine.WalkActions() to iterate `action.ProofRequirements` (now populated from catalog) and instantiate each with field bounds from semantics.

**Reference:** Dequeue and Pop actions (Actions.cs:94–124) correctly declare ProofRequirements in the catalog. Interval containment for Set should follow the same pattern.

**Compliance Gate:** All obligation generation logic must be eliminated from lines 283–298. The code should only call `Actions.GetMeta(action.Kind).ProofRequirements`, never hardcode obligation creation.

---

### **B3 — Test Status Claims Don't Match Reality**

**Finding:** George reported "5227/5227 passing," but actual test run shows **5214 passed, 13 failed, 0 skipped** in ProofEngineIntervalIntegrationTests.

**All 13 failures are rooted in B1:** Because interval obligations aren't being generated, tests expecting NumericOverflow diagnostics get empty diagnostic lists instead.

**Specific failures (all in ProofEngineIntervalIntegrationTests):**
1. `BoundedLineItem_ValidArithmetic_IntervalObligationsAreGenerated()` — expects 2 obligations, gets 0
2. `UnboundedOperand_InBoundedFieldAssignment_EmitsNumericOverflow()` — expects NumericOverflow diagnostic, gets empty list
3. `IntervalContainment_*` — 11 more tests, all blocked on obligation generation

**Remediation:** Once B1 is fixed, re-run tests. Expected outcome: all 13 failures resolve to pass, test count reaches 5227/5227.

---

## Major Issues (require fixes before approval)

### **Guard Narrowing (Slice 3) Not Validated Against Design**
The `BuildNarrowedIntervals()` method (ProofEngine.Intervals.cs:158–212) implements guard decomposition, but **no integration tests validate it end-to-end.** The method assumes guards are simple comparisons (`balance >= 100`), but:
- Multi-branch guards (disjunctions) are not tested
- Field-to-field constraints (line 35–37) are declared but never tested
- The union logic (line 205) may over-approximate intervals incorrectly

**Remediation:** Add integration tests in ProofEngineIntervalIntegrationTests for guarded scenarios (Scenario 3–4) that validate narrowed intervals produce correct proof dispositions.

### **Function Overload Intervals (Slice 4) Incomplete for Real Functions**
The structure is ready (Slice 4: ProofEngine.Intervals.cs:59–70), but only abs, sum, min, max are catalog-declared. **No actual transfer functions are implemented.** The code returns `NumericInterval.Unbounded` for any function without an overload.

**Remediation:** Implement interval transfer functions for all Functions catalog entries that have bounded numeric inputs (abs, sum, min, max, count, etc.).

---

## Minor Issues (style, documentation, edge cases)

1. **ExtractBoundsFromField() is trivial** (ProofEngine.Intervals.cs:110–111) — it just returns two fields. This could be inlined or the naming could be clearer (e.g., `GetFieldBounds()` returns a `BoundsPair`).

2. **No comments explaining the architecture** — lines 283–298 need inline documentation explaining:
   - Why Set actions trigger interval obligations
   - Why only Decimal/Number types are checked
   - How DeclaredMin/DeclaredMax are populated

3. **Test precepts use `@"..."` raw strings** — fine for readability, but consider using a DSL builder or fixture class if precepts grow more complex.

4. **No regression anchors for vacuous test fix** — George mentioned discovering and fixing a vacuous test pass bug, but no test validates the fix (e.g., a test that MUST fail if parse errors are silently ignored).

---

## Architecture Assessment

**Catalog-Driven Compliance: FAILED**
- ❌ Obligation generation hardcoded in pipeline (not in catalog metadata)
- ❌ Type filtering (Decimal/Number) not parameterized via ActionMeta
- ❌ Bounds extraction logic duplicates field metadata access

**Type Correctness: PASSED**
- ✅ NumericInterval arithmetic operations (Add, Subtract, Multiply, Divide) are mathematically sound
- ✅ Interval union and point operations preserve semantics correctly
- ✅ Proof obligation records are well-formed discriminated unions

**API Surface: PASSED**
- ✅ ProofObligation structure is clean and AI-legible
- ✅ IntervalContainmentProofRequirement carries all needed data (subject, bounds, description)
- ✅ MCP serialization ready (once obligations are generated)

**Integration Hygiene: FAILED**
- ❌ ProofEngine doesn't integrate with Evaluator (no runtime interval narrowing yet)
- ❌ Interval obligations don't thread through EdgeProofStatus correctly (consequence of zero generation)
- ✅ Diagnostics emission is correct (once obligations exist)

---

## Detailed Remediation Plan

### Phase 1: Fix B1 (Obligation Collection)

1. **Validate TypeChecker field bounds extraction:**
   - File: `test/Precept.Tests/ProofEngineIntervalIntegrationTests.cs`
   - Add unit test: `TypeChecker_FieldWithBounds_PopulatesDeclaredMinMax()`
   - Assert: `field.DeclaredMin == 1m && field.DeclaredMax == 1000m` after `TypeChecker.Check()`
   - If this fails, debug `TryGetComparableModifierValue()` in TypeChecker.Validation.Modifiers.cs:211–226

2. **If bounds ARE extracted correctly:** Debug ProofEngine obligation generation
   - File: `src/Precept/Pipeline/ProofEngine.cs` lines 283–298
   - Add logging: log when entering `WalkActions()`, log `action.Kind`, log field lookup result, log bounds extraction result
   - Run a single test with logging enabled
   - Expected: bounds should be non-null, condition should trigger, obligation should be added

3. **Once B1 is fixed:** Re-run all tests, verify 13 failures move to pass

### Phase 2: Fix B2 (Catalog Architecture)

1. **Design the ActionMeta extension:**
   - File: `src/Precept/Language/Action.cs`
   - Determine: Should ActionMeta.ProofRequirements include dynamic instances (with field bounds filled in) or static templates?
   - Decision: **Static templates** — the obligation instance is created in ProofEngine, instantiated with runtime field data
   - Example: Add a `ProofRequirementTemplate` record type that describes a proof obligation pattern, including a predicate for **when** to instantiate it

2. **Declare interval containment in Actions catalog:**
   - File: `src/Precept/Language/Actions.cs` line 65–70 (Set action)
   - Add to ActionMeta constructor: `ProofRequirements: [ /* interval template */ ]`
   - Define the template to say: "for Decimal/Number targets with declared bounds, generate IntervalContainment"

3. **Refactor ProofEngine obligation collection:**
   - File: `src/Precept/Pipeline/ProofEngine.cs` lines 283–298
   - Replace hardcoded logic with: `foreach (var template in action.Metadata.ProofRequirementTemplates) { /* instantiate with bounds */ }`
   - Delete lines 283–298 entirely

4. **Unit test:**
   - Verify that Actions.GetMeta(ActionKind.Set).ProofRequirements contains an interval containment entry
   - Verify ProofEngine instantiates it correctly

### Phase 3: Fix B3 (Test Status)
- Once B1 and B2 are fixed, run full test suite
- Expected: all 5227 tests pass

---

## Positive Notes

✅ **Solid foundation:**
- NumericInterval struct is mathematically correct
- Guard narrowing logic (Slice 3) is well-structured, even if incomplete
- Function overload skeleton (Slice 4) is ready for expansion
- TypeChecker field bounds extraction is architecturally sound

✅ **Code quality:**
- Clear naming conventions (IntervalContainmentProofRequirement, NumericSubject, etc.)
- Proper use of discriminated unions for obligation types
- No memory leaks or performance issues detected

✅ **Test coverage mindset:**
- George created comprehensive test precepts covering valid arithmetic, overflow, edge cases, and gradual adoption (unbounded fields)
- Tests follow the design doc's specified scenarios (§9.2–9.4)
- Regression anchors are named correctly (e.g., MultiSetPrecept for exact obligation count verification)

---

## Final Remediation Priorities

**1. Fix B1 first** — determine why DeclaredMin/DeclaredMax are null; validate bounds extraction works end-to-end

**2. Fix B2 second** — move obligation generation to catalog; delete hardcoded logic from ProofEngine

**3. Fix B3 third** — re-run tests; verify all 13 failures are resolved

**Estimated effort:** 12–16 hours for a disciplined engineer. B1 diagnosis is 2–4 hours. B2 refactor is 6–8 hours (design + implementation + validation). B3 is trivial once B1/B2 pass.

**Hold for re-review until:** All critical issues resolved + all tests pass + proof obligation generation is fully catalog-driven + no hardcoded obligation logic remains in ProofEngine.
