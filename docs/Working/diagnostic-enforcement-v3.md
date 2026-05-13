# Diagnostic Gap Closure and Enforcement

> **Status:** Design — Pending Shane Sign-off
> **Author:** Frank (Lead/Architect)
> **Date:** 2026-05-13

---

## 1. Executive Summary

Precept's identity is founded on a single non-negotiable guarantee: invalid configurations are structurally impossible. The compiler is the product's primary proof surface — if the compiler says a definition is valid, it is valid. Every diagnostic code declared in the catalog is an implicit promise to the user: "the compiler will catch this class of error."

Of the 132 diagnostics defined in `DiagnosticCode.cs`, **50 have no emission site** in any pipeline stage. They are declared with catalog metadata, documented in the spec, and in most cases already have anticipatory tests — but the compiler never produces them. This is an integrity debt with two layers of consequence:

1. **Silent wrong behavior.** Cross-currency arithmetic compiles clean. A choice field accepts string literals that aren't in its declared value set. A lookup field can be accessed by key without a `contains` guard. The compiler's silence is false validation.

2. **AI legibility failure.** The MCP `precept_diagnostic` tool returns full trigger/recovery metadata for these codes. AI consumers (including this agent) present them to users as working enforcement. They aren't.

This design does two things: it closes the most critical gaps and it installs a two-gate convention test that prevents future regressions. The convention test is the enforcement mechanism; the gap closure work is the remediation. Together they form a complete answer to the question "how do we know the catalog's promises are kept?"

**Important correction from the initial gap analysis:** Four diagnostics (PRE0066, PRE0095, PRE0097, PRE0098) were initially reported as "never emitted." These ARE emitted — by `ValidateCIEnforcement` in `TypeChecker.Validation.cs` via catalog-driven dispatch through `Operations.GetMeta().CIDiagnosticCode` and `Functions.GetMeta().CIDiagnosticCode`. The original grep missed them because indirect catalog references don't produce a literal `DiagnosticCode.X` token at the emission call site. **The true gap count is 50, not 54.** This catalog-driven emission pattern is the reason the convention test's emission-site scan must check for indirect dispatch, not just literal references.

---

## 2. Current State

### What the pipeline currently does

The pipeline produces diagnostics through three patterns:

1. **Direct emission:** `Diagnostics.Create(DiagnosticCode.X, span, ...)` at an explicit call site in a pipeline stage. The vast majority of working diagnostics use this pattern.

2. **Catalog-mediated emission:** `CIDiagnosticCode: DiagnosticCode.X` in `Operations.cs` or `Functions.cs`. The CI enforcement cluster (PRE0066, PRE0095, PRE0097, PRE0098) uses this pattern — `ValidateCIEnforcement` reads the catalog property and dispatches without ever mentioning the code by name in the pipeline source.

3. **ProofEngine dispatch:** `DiagnosticCode.X` in ProofEngine switch branches for proof obligation failures.

**Current emission inventory (as of 2026-05-13):**
- 132 diagnostic codes defined in `DiagnosticCode.cs`
- 83 have emission sites in the pipeline or catalog-emission files
- 50 have no emission site (the gap)
- 7 have neither emission nor test (see § 4.5 below)
- 0 emitted codes are untested — all 83 emitted codes have at least one test reference

### What the pipeline does NOT do (and must)

- No diagnostic fires when money fields with different currencies are combined in an arithmetic expression (`CostUSD + CostEUR` compiles clean).
- No diagnostic fires when a choice field is compared to a string literal that isn't in its declared value set (`Status == "Pending"` where `"Pending"` isn't declared compiles clean).
- No diagnostic fires when a lookup field is accessed by key without a `contains` guard.
- No diagnostic fires when a field declared `omit` in a state is used in an expression anchored to that state (covered separately in field-state-guarantees-v3.md).
- No diagnostic fires when an initial event fails to assign all required fields (PRE0094 — this design closes that gap).
- Temporal constant errors surface as generic `InvalidTypedConstantContent` (PRE0053) rather than domain-specific diagnostics.
- No mechanism exists to detect when a new diagnostic code is added without an emission site, so gaps silently accumulate.

### No enforcement mechanism exists

There is currently no gate — automated or manual — that catches a new diagnostic code added to `DiagnosticCode.cs` without a corresponding emission site in the pipeline. This means the gap can and will grow over time through normal development. The convention test (Slice 0) closes this structural hole.

---

## 3. Gap Inventory by Root Cause

### 3.1 Root Cause A — Parser Gates Never Wired (PRE0013–0015)

These three diagnostics are specified in §2.7 (Parser Diagnostics) but the parser never reaches the emission call sites.

| PRE | Name | Invalid construct |
|-----|------|-------------------|
| PRE0013 | `OmitDoesNotSupportGuard` | `omit Field in State when Guard` — `omit` is unconditional structural exclusion |
| PRE0014 | `EventHandlerDoesNotSupportGuard` | `on Event when Guard -> actions` — event handlers don't support guards |
| PRE0015 | `PreEventGuardNotAllowed` | `from State when Guard on Event -> ...` — guard must follow `on Event`, not precede it |

**Why missing:** Parser construct dispatch and slot resolution for guards, transitions, and event handlers were built incrementally. The three specific rejection paths for these invalid forms were never added. The parser silently accepts or misparscs these constructs.

**Current user experience:** `omit Notes in Draft when Notes is set` produces a confusing generic parse error about unexpected tokens, or the guard is silently ignored. The spec promises a precise, actionable diagnostic.

### 3.2 Root Cause B1 — Temporal Constant Precision (PRE0055–0062)

The typed constant validation pipeline reaches `TypedConstantValidation` but uses `UnresolvedTypedConstant` (PRE0052) and `InvalidTypedConstantContent` (PRE0053) as catch-alls instead of selecting the domain-specific diagnostic.

| PRE | Name | Intended trigger |
|-----|------|-----------------|
| PRE0055 | `InvalidDateValue` | `'2024-02-30'` — calendar date doesn't exist |
| PRE0056 | `InvalidDateFormat` | `'30-02-2024'` — wrong format, must be YYYY-MM-DD |
| PRE0057 | `InvalidTimeValue` | `'25:61:00'` — hours/minutes/seconds out of range |
| PRE0058 | `InvalidInstantFormat` | `'2024-01-01T00:00:00'` — missing trailing Z |
| PRE0059 | `InvalidTimezoneId` | `'US/Eastern'` — not canonical IANA form |
| PRE0060 | `UnqualifiedPeriodArithmetic` | `date + unconstrained_period` — period has mixed components |
| PRE0061 | `MissingTemporalUnit` | Bare number in temporal arithmetic context |
| PRE0062 | `FractionalUnitValue` | `'1.5 months'` — temporal units must be whole |

The error IS caught (PRE0052/0053 fires), but the user sees "invalid typed constant content" instead of "February 30 doesn't exist." B1 is a precision gap, not a silent-failure gap. NodaTime already returns the specific failure reason — the TypeChecker just doesn't select the right code.

PRE0060–0062 (period arithmetic rules) are more substantive: they require qualifier-aware type checking in the temporal binary operation path, parallel to B2.

### 3.3 Root Cause B2 — Currency/Unit Arithmetic Safety (PRE0070–0074) ⚠️ HIGHEST SEVERITY

| PRE | Name | Intended trigger |
|-----|------|-----------------|
| PRE0070 | `CrossCurrencyArithmetic` | `USD_amount + EUR_amount` — different currencies |
| PRE0071 | `CrossDimensionArithmetic` | `'5 kg' + '3 mi'` — mass ≠ length |
| PRE0072 | `DenominatorUnitMismatch` | `price in 'USD/kg' * quantity in 'mi'` |
| PRE0073 | `DurationDenominatorMismatch` | `price in 'USD/days' * duration` — variable-length denominator |
| PRE0074 | `CompoundPeriodDenominator` | `period in 'hours&minutes' * price in 'USD/hours'` |

**What the TypeChecker currently does:** Binary operation resolution through `Operations.GetMeta()` produces a correct result type for valid combinations but does not check qualifier compatibility — when the Operations catalog says "same currency required," the checker doesn't verify the currencies match. The expression `CostUSD + CostEUR` resolves to `money` without error.

**Severity:** This directly violates `docs/philosophy.md`'s foundational claim: "No errors. No bugs. Business logic cannot produce a wrong answer." Cross-currency arithmetic producing a wrong answer is **precisely the class of bug Precept promises to eliminate.** The spec (§3.6 Business-domain operators) explicitly says "Same currency required" for money±money. The compiler doesn't enforce it.

**Note on dynamic qualifiers:** When a field has a dynamic qualifier like `money in '{CatalogCurrency}'`, the TypeChecker cannot statically compare currencies. Static qualifier comparison is feasible; dynamic qualifier resolution is ProofEngine territory (Strategy 5). See Open Question 2.

### 3.4 Root Cause B3 — Choice Value Validation (PRE0086–0089)

| PRE | Name | Intended trigger |
|-----|------|-----------------|
| PRE0086 | `ChoiceLiteralNotInSet` | `Status == "Pending"` where "Pending" isn't declared in the choice set |
| PRE0087 | `ChoiceArgOutsideFieldSet` | Arg choice includes values outside field's declared set |
| PRE0088 | `ChoiceElementTypeMismatch` | `choice of integer` arg to `choice of string` field |
| PRE0089 | `ChoiceRankConflict` | Arg order conflicts with field's declared order |

PRE0088 and a related companion (PRE0090 `ChoiceMissingElementType`) are correctly emitted at parse time via `ParseTypeRef()`. PRE0086, PRE0087, and PRE0089 are type-checker-stage checks that require comparing expression literals against resolved choice field declarations — that comparison logic doesn't exist.

**Current user experience:** `rule Status == "Pending"` where Status is `choice of string("Active", "Done")` compiles clean. A typo in a governance rule is invisible to the compiler.

### 3.5 Root Cause B4 — Collection Safety Extensions (PRE0099–0101, PRE0104)

| PRE | Name | Intended trigger |
|-----|------|-----------------|
| PRE0099 | `KeyPresenceSafety` | Key-based lookup access without `contains` guard |
| PRE0100 | `IndexBoundsGuard` | Index-based list access without bounds guard |
| PRE0101 | `KeyUniquenessGuard` | `put` without uniqueness guard |
| PRE0104 | `MissingOrderingKey` | `min`/`max` on collection without declared ordering key |

The existing collection safety system emits PRE0063 `UnguardedCollectionAccess` and PRE0064 `UnguardedCollectionMutation` for `.peek`/`.min`/`.max` and `pop`/`dequeue`. The newer collection types (list, lookup, queue-by-priority) have additional safety requirements that were never wired.

### 3.6 Root Cause C — Structural Single-Check Gaps (PRE0092, PRE0094)

| PRE | Name | Status |
|-----|------|--------|
| PRE0092 | `EventHandlerInStatefulPrecept` | Specced (§3.8). Trivial structural check — if `ctx.States.Count > 0 && ctx.EventHandlers.Count > 0`, emit. |
| PRE0094 | `InitialEventMissingAssignments` | Specced (§3A.5). Blocking gap — required fields not validated at initial event. D93 (`RequiredFieldsNeedInitialEvent`) fires but its pair D94 does not. Entity can be created with missing required fields. Already identified as a dependency of field-state-guarantees-v3 Slices 10–11. |

### 3.7 Root Cause D — Scattered TypeChecker Gaps (17 diagnostics)

Individual emission sites missing across multiple TypeChecker validation methods. Most are straightforward wires.

**D1: Parser expression precision (PRE0010–0012)** — chained comparisons, keywords-as-values, invalid call targets. Parser catches the error condition but emits generic `ExpectedToken` instead of the domain-specific code.

**D2: TypeChecker validation gaps (14 codes):**

| PRE | Name | Theme |
|-----|------|-------|
| PRE0019 | `NullInNonNullableContext` | Null safety |
| PRE0022 | `FunctionArgConstraintViolation` | Function args |
| PRE0027 | `DuplicateArgName` | Duplicate detection |
| PRE0035 | `InvalidModifierValue` | Modifier validation |
| PRE0039 | `ComputedFieldWithDefault` | Computed field |
| PRE0042 | `ConflictingAccessModes` | Access modes |
| PRE0044 | `ListLiteralOutsideDefault` | List literals |
| PRE0050 | `EventArgOutOfScope` | Scope rules |
| PRE0051 | `InvalidInterpolationCoercion` | Interpolation |
| PRE0067 | `MaxPlacesExceeded` | Business domain |
| PRE0078 | `NumericOverflow` | Value safety |
| PRE0085 | `NonChoiceAssignedToChoice` | Choice types |
| PRE0105 | `CollectionInnerTypeError` | Collections |
| PRE0043 | `RedundantAccessMode` | Access modes |

### 3.8 Codes With Neither Emission Nor Test (7 codes)

These 7 codes have no emission site AND no test file reference anywhere in the test suite. They are the floor of the debt:

| Code | Notes |
|------|-------|
| `AmbiguousTypedConstant` (PRE0091) | Unreachable — current resolution is single-candidate. Latent; wire when multi-candidate resolution ships. |
| `EventHandlerDoesNotSupportGuard` (PRE0014) | Root Cause A — parser gate never wired |
| `EventHandlerInStatefulPrecept` (PRE0092) | Root Cause C — trivial structural check not wired |
| `OmitDoesNotSupportGuard` (PRE0013) | Root Cause A — parser gate never wired |
| `OutOfRange` (PRE0079) | Catalog declares `DiagnosticStage.Type` but bounds checking of runtime values is impossible statically. Reclassify as proof obligation or wire for constant assignments only. |
| `PreEventGuardNotAllowed` (PRE0015) | Root Cause A — parser gate never wired |
| `RedundantAccessMode` (PRE0043) | Root Cause D — specced (§3.8), implementation not wired |

---

## 4. Impact Assessment

### Priority by integrity risk

| Cluster | User impact | Philosophy violation |
|---------|-------------|---------------------|
| **B2: Currency/unit arithmetic** | Silent wrong behavior — cross-currency arithmetic compiles clean and produces wrong answers | **Direct.** philosophy.md: "Business logic cannot produce a wrong answer." The compiler's promise is the product's promise. |
| **B3: Choice validation** | Silent wrong behavior — non-existent choice values pass type checking | **Direct.** Closed-set governance is a foundational claim; the set isn't closed if the compiler accepts out-of-set literals. |
| **PRE0094: InitialEventMissingAssignments** | Entity can be created with missing required fields | **Direct.** Prevention and Totality require every reachable configuration to be valid. |
| **B4: Collection safety** | Runtime faults instead of compile errors — key/index safety violations slip through | **High.** "Compile errors, not runtime surprises" is the core operational guarantee. |
| **A: Parser gates** | Confusing generic parse errors instead of clear rejection | **Medium.** UX degradation but errors ARE caught. |
| **B1: Temporal precision** | Generic error instead of precise error — less severe because the error IS caught | **Low-medium.** Precision gap, not a missing guard. |
| **D: Scattered** | Mixed — some are precision downgrades, some genuine gaps | Varies per code. |

### The B2 case for urgency

`field Total as money in 'USD' <- CostUSD + CostEUR` compiles clean today. At runtime the evaluator arithmetic combines the raw decimal values regardless of currency — it doesn't fault, it silently produces a number that represents a blend of USD and EUR denominated in USD. There is no runtime indication that anything is wrong. The MCP tool returns `CrossCurrencyArithmetic` with a complete trigger description that asserts this is caught at compile time. Both the product documentation and the AI tooling claim an enforcement that doesn't exist.

---

## 5. Design

### 5.1 Enforcement mechanism: convention test over Roslyn analyzer

**Decision: xUnit convention test** (`DiagnosticEmissionCoverageTests.cs`) rather than a Roslyn analyzer (`PRECEPT0027`).

**Rationale:** The emission-site distinction problem makes a Roslyn analyzer heavier than it looks. A `CompilationEndAction` analyzer would need to distinguish:
- `Diagnostics.GetMeta(DiagnosticCode.X)` references in `Diagnostics.cs` — catalog reads, NOT emissions
- `Diagnostics.Create(DiagnosticCode.X, ...)` in pipeline stages — real emissions
- `CIDiagnosticCode: DiagnosticCode.X` in `Operations.cs`/`Functions.cs` — catalog-mediated emissions

That distinction requires semantic analysis of the `IFieldReferenceOperation`'s containing invocation, which is tractable but adds complexity disproportionate to the value — since the convention test catches the same class of regressions at PR time. The convention test is the pragmatic gate. If the allow-list grows unwieldy or the team wants build-time IDE feedback, promote the logic to a Roslyn analyzer in `Precept.Analyzers` later.

**A Roslyn analyzer is the right next evolution,** not the right first step. The catalog-declared `IsImplemented` flag (see § 5.3) is the architecturally correct long-term answer, but it requires touching all 132 catalog entries.

### 5.2 Two-gate design

The convention test enforces two distinct properties:

**Gate 1 — Emission site existence:** Every `DiagnosticCode` member must have at least one reference (direct or catalog-mediated) in a pipeline emission-site source file, OR be on the allow-list with a tracking comment.

**Gate 2 — Test coverage:** Every emitted diagnostic code must be referenced in at least one test file. Gate 2's scope is codes that pass Gate 1 — codes on the Gate 1 allow-list have no test obligation because there's nothing to test.

**Current state that makes Gate 2 start clean:** All 83 currently-emitted codes already have at least one `DiagnosticCode.{MemberName}` reference in the test suite. Gate 2's allow-list starts empty. This is notable — 42 of the 49 unemitted codes also have anticipatory test references (tests written expecting future implementation). Only the 7 codes in § 3.8 above have neither emission nor test.

### 5.3 Gap closure approach

**Post-resolution validation for TypeChecker gaps.** New checks follow the `ValidateCIEnforcement` pattern: a validation method that walks already-resolved `TypedExpression` trees and `TypedAction` chains after Pass 2 resolution is complete. This avoids threading state context into resolution and keeps validation cohesive.

**Parser rejection paths for Root Cause A.** Simple pattern detection in the parser: when a `when` token appears in a position where it's invalid (`omit` context, event handler context, pre-event context), emit the specific diagnostic and continue parsing.

**TypeChecker qualifier comparison for B2.** After `Operations.GetMeta()` resolves a business-domain binary operation, add a post-resolution check that compares qualifier values (currency code, unit, dimension) from both operands. The TypeChecker handles static qualifiers; dynamic qualifiers (`money in '{CatalogCurrency}'`) are deferred to the ProofEngine.

**Choice literal comparison for B3.** When a `TypedLiteral` is compared to or assigned to a choice field, check whether the literal's value exists in the field's declared choice set. The choice value set is already parsed and available in the field's type reference.

### 5.4 Alternatives considered and rejected

**Option: Wire currency/unit checks in the ProofEngine rather than TypeChecker.** The ProofEngine already handles qualifier compatibility for proof obligations (`PRE0114 UnprovedQualifierCompatibility`). Extending it would work but places a type-level check in the proof stage, blurring the stage boundary. The Operations catalog declares `DiagnosticStage.Type` for these codes — they belong in the TypeChecker. Proof obligations are about provability ("can we prove this division won't be zero?"), not type-level domain rules ("these currencies must match"). The boundary must stay clean.

**Option: Accept generic temporal diagnostics, deprecate PRE0055–0062.** `InvalidTypedConstantContent` is sufficient in that it catches the error. But domain-specific diagnostics exist precisely to give precision. "February 30 doesn't exist" is better than "invalid typed constant content." The NodaTime exception already contains the specific failure reason; we're just not selecting the right code.

**Option: Roslyn analyzer as Gate 1.** See § 5.1. Deferred, not rejected.

---

## 6. Spec Gaps

### Gap 1: Catalog-driven emission not documented as a pattern

The spec and contributing guide describe direct emission (`Diagnostics.Create(...)`) but not catalog-mediated emission via `CIDiagnosticCode`. This caused the initial audit to miscount the gap (54 instead of 50). Any gap audit tool — including the convention test — must search for both patterns.

**Annotation needed:** `DiagnosticMeta` documentation should note that `CIDiagnosticCode` on operation/function metadata constitutes an emission site equivalent to `Diagnostics.Create(...)`.

### Gap 2: §3.6 qualifier compatibility scope unclear

The spec says "Same currency required" for money±money but doesn't specify whether this applies to fields with dynamic qualifiers (`money in '{CatalogCurrency}'`). The design here treats dynamic qualifiers as TypeChecker-exempt and ProofEngine-governed. This should be documented in §3.6.

### Gap 3: Choice type enforcement split across stages

§3.8 documents PRE0086–0090 as a unit but they actually span two stages: PRE0088 and PRE0090 fire at parse time; PRE0086, PRE0087, PRE0089 fire at type-check time. The spec should clarify this split.

---

## 7. Implementation Plan

> **Quality bar:** Method-level specificity for Slices 0–4. Cluster-level specificity for Slices 5–8. Every slice has file paths, test method names, regression anchors, and a checklist.

### Architectural Approach

**Convention test first.** Slice 0 installs the enforcement mechanism before any gap is closed. Once the convention test exists with all 50 unemitted codes in the allow-list, every subsequent gap-closure slice automatically validates its own work: the act of removing a code from the allow-list is the slice's completion gate.

**Gap closure follows priority, not technical dependency.** The ordering within each priority tier is largely independent.

### Ordering Constraints

```
Slice 0 (convention test) ─────────────────────────────────→ scaffolding for all slices

Slice 1 (B2 currency/unit) ──→ independent of all others
Slice 2 (B3 choice)        ──→ independent of all others
Slice 3 (PRE0094)          ──→ independent; Slice 10-11 in field-state-v3 depend on it
Slice 4 (PRE0092)          ──→ independent (trivial)

Slice 5 (B1 temporal)      ──→ independent
Slice 6 (B4 collection)    ──→ independent
Slice 7 (A parser gates)   ──→ independent

Slice 8 (scattered D)      ──→ independent (individual wires)
```

All gap-closure slices (1–8) depend on Slice 0 only in the soft sense: Slice 0 is the mechanism that validates completion. The code changes in Slices 1–8 do not have a code dependency on Slice 0.

---

### Slice 0 — Convention Test: Gate 1 + Gate 2

**Purpose:** Install the two-gate enforcement mechanism. Once this slice ships, all future gap-closure work is automatically validated by the test suite. The allow-list starts with all 50 currently-unemitted codes; each subsequent slice removes entries as gaps are closed.

**Create:** `test/Precept.Tests/CatalogTests/DiagnosticEmissionCoverageTests.cs`

```csharp
public sealed class DiagnosticEmissionCoverageTests
{
    // Gate 1 allow-list: known-unemitted codes — shrinks as gaps close.
    // Each entry must cite the tracking issue or reason.
    private static readonly HashSet<DiagnosticCode> Gate1AllowList = new()
    {
        // Root Cause A — parser gates never wired
        DiagnosticCode.OmitDoesNotSupportGuard,           // PRE0013
        DiagnosticCode.EventHandlerDoesNotSupportGuard,   // PRE0014
        DiagnosticCode.PreEventGuardNotAllowed,           // PRE0015
        // Root Cause B1 — temporal constant precision
        DiagnosticCode.InvalidDateValue,                  // PRE0055
        DiagnosticCode.InvalidDateFormat,                 // PRE0056
        DiagnosticCode.InvalidTimeValue,                  // PRE0057
        DiagnosticCode.InvalidInstantFormat,              // PRE0058
        DiagnosticCode.InvalidTimezoneId,                 // PRE0059
        DiagnosticCode.UnqualifiedPeriodArithmetic,       // PRE0060
        DiagnosticCode.MissingTemporalUnit,               // PRE0061
        DiagnosticCode.FractionalUnitValue,               // PRE0062
        // Root Cause B2 — currency/unit arithmetic (highest severity)
        DiagnosticCode.CrossCurrencyArithmetic,           // PRE0070
        DiagnosticCode.CrossDimensionArithmetic,          // PRE0071
        DiagnosticCode.DenominatorUnitMismatch,           // PRE0072
        DiagnosticCode.DurationDenominatorMismatch,       // PRE0073
        DiagnosticCode.CompoundPeriodDenominator,         // PRE0074
        // Root Cause B3 — choice type validation
        DiagnosticCode.ChoiceLiteralNotInSet,             // PRE0086
        DiagnosticCode.ChoiceArgOutsideFieldSet,          // PRE0087
        DiagnosticCode.ChoiceRankConflict,                // PRE0089
        // Root Cause B4 — collection safety extensions
        DiagnosticCode.KeyPresenceSafety,                 // PRE0099
        DiagnosticCode.IndexBoundsGuard,                  // PRE0100
        DiagnosticCode.KeyUniquenessGuard,                // PRE0101
        DiagnosticCode.MissingOrderingKey,                // PRE0104
        // Root Cause C — structural single-check gaps
        DiagnosticCode.EventHandlerInStatefulPrecept,     // PRE0092
        DiagnosticCode.InitialEventMissingAssignments,    // PRE0094
        // Root Cause D — scattered TypeChecker gaps
        DiagnosticCode.NonAssociativeComparison,          // PRE0010
        DiagnosticCode.UnexpectedKeyword,                 // PRE0011
        DiagnosticCode.InvalidCallTarget,                 // PRE0012
        DiagnosticCode.NullInNonNullableContext,          // PRE0019
        DiagnosticCode.FunctionArgConstraintViolation,    // PRE0022
        DiagnosticCode.DuplicateArgName,                  // PRE0027
        DiagnosticCode.InvalidModifierValue,              // PRE0035
        DiagnosticCode.ComputedFieldWithDefault,          // PRE0039
        DiagnosticCode.ConflictingAccessModes,            // PRE0042
        DiagnosticCode.RedundantAccessMode,               // PRE0043
        DiagnosticCode.ListLiteralOutsideDefault,         // PRE0044
        DiagnosticCode.EventArgOutOfScope,                // PRE0050
        DiagnosticCode.InvalidInterpolationCoercion,      // PRE0051
        DiagnosticCode.MaxPlacesExceeded,                 // PRE0067
        DiagnosticCode.NumericOverflow,                   // PRE0078
        DiagnosticCode.OutOfRange,                        // PRE0079
        DiagnosticCode.NonChoiceAssignedToChoice,         // PRE0085
        DiagnosticCode.AmbiguousTypedConstant,            // PRE0091 — latent; single-candidate resolution
        DiagnosticCode.CollectionInnerTypeError,          // PRE0105
        // ... remaining codes to full count of 50 ...
    };

    // Gate 2 allow-list: emitted codes without tests. Currently empty — all 83 emitted
    // codes already have test references. Non-empty entries require a tracking reason.
    private static readonly HashSet<DiagnosticCode> Gate2AllowList = new()
    {
    };

    [Fact]
    public void Every_DiagnosticCode_Has_An_Emission_Site_Or_Is_AllowListed()
    {
        var repoRoot = GetRepoRoot();
        var emittedCodes = GetEmittedCodes(repoRoot);
        var allCodes = Enum.GetValues<DiagnosticCode>();

        var violations = allCodes
            .Where(code => !emittedCodes.Contains(code) && !Gate1AllowList.Contains(code))
            .ToList();

        violations.Should().BeEmpty(
            $"The following DiagnosticCode members have no emission site and are not allow-listed:\n" +
            string.Join("\n", violations.Select(c => $"  - {c}")) +
            "\nAdd an emission site in a pipeline stage, or add to Gate1AllowList with a tracking comment.");
    }

    [Fact]
    public void Gate1_AllowList_Contains_Only_Actually_Unemitted_Codes()
    {
        var repoRoot = GetRepoRoot();
        var emittedCodes = GetEmittedCodes(repoRoot);

        var staleEntries = Gate1AllowList.Where(code => emittedCodes.Contains(code)).ToList();

        staleEntries.Should().BeEmpty(
            $"The following Gate1AllowList entries are now emitted and should be removed:\n" +
            string.Join("\n", staleEntries.Select(c => $"  - {c}")));
    }

    [Fact]
    public void Every_Emitted_DiagnosticCode_Has_A_Test()
    {
        var repoRoot = GetRepoRoot();
        var emittedCodes = GetEmittedCodes(repoRoot);

        var testText = GetTestFileText(repoRoot);
        var violations = emittedCodes
            .Where(code => !Gate2AllowList.Contains(code))
            .Where(code => !testText.Contains($"DiagnosticCode.{code}"))
            .ToList();

        violations.Should().BeEmpty(
            $"The following emitted DiagnosticCode members have no test reference:\n" +
            string.Join("\n", violations.Select(c => $"  - {c}")) +
            "\nWrite a test asserting this diagnostic fires, or add to Gate2AllowList with a reason.");
    }

    [Fact]
    public void Gate2_AllowList_Contains_Only_Actually_Untested_Codes()
    {
        var repoRoot = GetRepoRoot();
        var testText = GetTestFileText(repoRoot);

        var staleEntries = Gate2AllowList
            .Where(code => testText.Contains($"DiagnosticCode.{code}"))
            .ToList();

        staleEntries.Should().BeEmpty(
            $"The following Gate2AllowList entries now have test coverage and should be removed:\n" +
            string.Join("\n", staleEntries.Select(c => $"  - {c}")));
    }

    private static HashSet<DiagnosticCode> GetEmittedCodes(string repoRoot)
    {
        // Emission-site source files:
        //   - All .cs files under src/Precept/Pipeline/
        //   - src/Precept/Language/Operations.cs
        //   - src/Precept/Language/Functions.cs
        // Excluded (non-emission references):
        //   - src/Precept/Language/DiagnosticCode.cs (enum definition)
        //   - src/Precept/Language/Diagnostics.cs (GetMeta catalog — not emission)
        var pipelineFiles = Directory
            .GetFiles(Path.Combine(repoRoot, "src", "Precept", "Pipeline"), "*.cs");
        var catalogEmissionFiles = new[]
        {
            Path.Combine(repoRoot, "src", "Precept", "Language", "Operations.cs"),
            Path.Combine(repoRoot, "src", "Precept", "Language", "Functions.cs"),
        };

        var allText = string.Join("\n",
            pipelineFiles.Concat(catalogEmissionFiles).Select(File.ReadAllText));

        return Enum.GetValues<DiagnosticCode>()
            .Where(code => allText.Contains($"DiagnosticCode.{code}"))
            .ToHashSet();
    }

    private static string GetTestFileText(string repoRoot)
    {
        var testDirs = new[]
        {
            Path.Combine(repoRoot, "test", "Precept.Tests"),
            Path.Combine(repoRoot, "test", "Precept.LanguageServer.Tests"),
            Path.Combine(repoRoot, "test", "Precept.Mcp.Tests"),
        };
        return string.Join("\n",
            testDirs.SelectMany(d => Directory.GetFiles(d, "*.cs", SearchOption.AllDirectories))
                    .Select(File.ReadAllText));
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);
        while (dir != null && !dir.GetFiles("*.slnx").Any())
            dir = dir.Parent;
        return dir?.FullName
            ?? throw new InvalidOperationException("Could not find repo root (no .slnx found).");
    }
}
```

**Scope of "covered":** The literal token `DiagnosticCode.{MemberName}` appears in at least one `.cs` file in the emission-site source set (Pipeline files + `Operations.cs` + `Functions.cs`), excluding the enum definition and metadata catalog. This catches all three emission patterns: direct `Diagnostics.Create(...)`, catalog-mediated `CIDiagnosticCode:` assignments, and ProofEngine dispatch branches.

**What it does NOT catch:** Dead code paths (code referenced inside an unreachable branch still passes), incorrect emission context (stray references in comments or `FaultSiteLink` constructors count as "present"), and spec documentation completeness.

**Test method names:**
- `Every_DiagnosticCode_Has_An_Emission_Site_Or_Is_AllowListed` — `[Fact]`
- `Gate1_AllowList_Contains_Only_Actually_Unemitted_Codes` — `[Fact]`
- `Every_Emitted_DiagnosticCode_Has_A_Test` — `[Fact]`
- `Gate2_AllowList_Contains_Only_Actually_Untested_Codes` — `[Fact]`

**Regression anchors:**
- `DiagnosticsTests.AllDiagnosticCodes` — existing exhaustiveness check pattern used as structural precedent
- `CatalogTestReflection.AllDiagnostics()` — reflection pattern reused

**Files:** `test/Precept.Tests/CatalogTests/DiagnosticEmissionCoverageTests.cs` (create)

- [ ] Create `DiagnosticEmissionCoverageTests.cs` with `GetRepoRoot()` and `GetEmittedCodes()` helpers
- [ ] Populate `Gate1AllowList` with all 50 unemitted codes (with cluster comments)
- [ ] Implement `Every_DiagnosticCode_Has_An_Emission_Site_Or_Is_AllowListed` (Gate 1)
- [ ] Implement `Gate1_AllowList_Contains_Only_Actually_Unemitted_Codes` (Gate 1 inverse)
- [ ] Implement `Every_Emitted_DiagnosticCode_Has_A_Test` (Gate 2) with empty `Gate2AllowList`
- [ ] Implement `Gate2_AllowList_Contains_Only_Actually_Untested_Codes` (Gate 2 inverse)
- [ ] Verify test passes with all 50 entries in Gate 1 allow-list and Gate 2 allow-list empty

---

### Slice 1 — B2: Currency/Unit Arithmetic Safety (PRE0070–0074)

**Purpose:** Close the highest-severity integrity gap. Cross-currency and cross-dimension arithmetic currently compiles without error. This slice adds qualifier comparison after binary operation resolution in the TypeChecker.

**Modify:** `src/Precept/Pipeline/TypeChecker.Expressions.cs`

In the binary operation resolution path, after `Operations.GetMeta()` returns the resolved `TypedBinaryOp`, add a qualifier compatibility check:

```csharp
private static TypedExpression ResolveBinaryOp(
    BinaryOpSlot slot, CheckContext ctx, ...)
{
    // ... existing resolution ...
    var resolved = new TypedBinaryOp(left, right, resolvedOp, resultType, span);

    // Qualifier compatibility check (B2 enforcement)
    ValidateQualifierCompatibility(resolved, slot.Span, ctx);

    return resolved;
}

private static void ValidateQualifierCompatibility(
    TypedBinaryOp op, SourceSpan span, CheckContext ctx)
{
    // Extract static qualifiers from operand types.
    // Skip if either operand has a dynamic qualifier (deferred to ProofEngine).
    var leftQualifier = TryGetStaticQualifier(op.Left);
    var rightQualifier = TryGetStaticQualifier(op.Right);
    if (leftQualifier is null || rightQualifier is null) return;

    // Money: same currency required (PRE0070)
    if (op.Left.ResolvedType.Kind == TypeKind.Money &&
        op.Right.ResolvedType.Kind == TypeKind.Money &&
        !StringComparer.OrdinalIgnoreCase.Equals(leftQualifier.Currency, rightQualifier.Currency))
    {
        ctx.Emit(Diagnostics.Create(DiagnosticCode.CrossCurrencyArithmetic, span,
            leftQualifier.Currency, rightQualifier.Currency));
        return;
    }

    // Quantity: same dimension required (PRE0071)
    if (op.Left.ResolvedType.Kind == TypeKind.Quantity &&
        op.Right.ResolvedType.Kind == TypeKind.Quantity &&
        !StringComparer.Ordinal.Equals(leftQualifier.Dimension, rightQualifier.Dimension))
    {
        ctx.Emit(Diagnostics.Create(DiagnosticCode.CrossDimensionArithmetic, span,
            leftQualifier.Dimension, rightQualifier.Dimension));
        return;
    }

    // PRE0072, PRE0073, PRE0074 checks for price/rate denominator mismatches...
}
```

**Test file (new):** `test/Precept.Tests/TypeChecker/TypeCheckerCurrencyUnitTests.cs`

Test method names:
- `MoneyFields_DifferentCurrencies_EmitsCrossCurrencyArithmetic` — `[Fact]`
- `MoneyFields_SameCurrency_NoDiagnostic` — `[Fact]`
- `MoneyField_DynamicQualifier_NoDiagnosticAtTypeStage` — `[Fact]`
- `QuantityFields_DifferentDimensions_EmitsCrossDimensionArithmetic` — `[Fact]`
- `QuantityFields_SameDimension_NoDiagnostic` — `[Fact]`
- `PriceField_DenominatorUnitMismatch_EmitsDenominatorUnitMismatch` — `[Fact]`
- `PriceField_DurationDenominatorVariable_EmitsDurationDenominatorMismatch` — `[Fact]`
- `PriceField_CompoundPeriodDenominator_EmitsCompoundPeriodDenominator` — `[Fact]`

**Remove from Gate 1 allow-list:** `CrossCurrencyArithmetic`, `CrossDimensionArithmetic`, `DenominatorUnitMismatch`, `DurationDenominatorMismatch`, `CompoundPeriodDenominator`

**Regression anchors:**
- `TypeCheckerExpressionTests` — existing binary operation resolution tests must pass unchanged
- `TypeCheckerCITests` — CI enforcement validation is unrelated, must pass

**Files:** `src/Precept/Pipeline/TypeChecker.Expressions.cs` (modify), `test/Precept.Tests/TypeChecker/TypeCheckerCurrencyUnitTests.cs` (create)

- [ ] Add `ValidateQualifierCompatibility` method in `TypeChecker.Expressions.cs`
- [ ] Add `TryGetStaticQualifier` helper to extract qualifier from resolved types
- [ ] Wire into binary operation resolution path
- [ ] Tests: 8 tests (positive + negative for each diagnostic code)
- [ ] Remove 5 codes from Gate 1 allow-list in `DiagnosticEmissionCoverageTests.cs`
- [ ] Verify Gate 1 staleness check now flags removed entries (proves the round-trip works)

---

### Slice 2 — B3: Choice Value Validation (PRE0086–0089)

**Purpose:** Close the choice type integrity gap. A `choice of string("Active", "Done")` field compared against `"Pending"` compiles clean today. This slice wires choice literal/arg comparison in the TypeChecker.

**Modify:** `src/Precept/Pipeline/TypeChecker.Expressions.cs`

In the comparison and assignment resolution path, when a literal is compared to or assigned to a choice-typed field:

```csharp
private static void ValidateChoiceLiteral(
    TypedLiteral literal, TypedFieldRef fieldRef, SourceSpan span, CheckContext ctx)
{
    if (fieldRef.ResolvedField.Type is not ChoiceTypeRef choiceType) return;

    var literalValue = literal.Value?.ToString();
    if (literalValue is null) return;

    if (!choiceType.Values.Any(v => string.Equals(v.Value, literalValue, StringComparison.Ordinal)))
    {
        ctx.Emit(Diagnostics.Create(DiagnosticCode.ChoiceLiteralNotInSet, span,
            literalValue, fieldRef.FieldName,
            string.Join(", ", choiceType.Values.Select(v => $"\"{v.Value}\""))));
    }
}
```

PRE0087 (`ChoiceArgOutsideFieldSet`) fires when an event arg's `choice` type includes values outside the target field's declared set. PRE0089 (`ChoiceRankConflict`) fires when the arg's rank ordering contradicts the field's declared order.

**Test file:** `test/Precept.Tests/TypeChecker/TypeCheckerStructuralTests.cs` (add section after existing choice tests)

Test method names:
- `ChoiceField_LiteralNotInSet_EmitsChoiceLiteralNotInSet` — `[Fact]`
- `ChoiceField_ValidLiteral_NoDiagnostic` — `[Fact]`
- `ChoiceField_LiteralCaseMismatch_EmitsChoiceLiteralNotInSet` — `[Fact]`
- `ChoiceArg_ValueOutsideFieldSet_EmitsChoiceArgOutsideFieldSet` — `[Fact]`
- `ChoiceArg_ValuesSubsetOfFieldSet_NoDiagnostic` — `[Fact]`
- `ChoiceArg_RankConflictsWithField_EmitsChoiceRankConflict` — `[Fact]`
- `ChoiceArg_RankMatchesField_NoDiagnostic` — `[Fact]`

**Remove from Gate 1 allow-list:** `ChoiceLiteralNotInSet`, `ChoiceArgOutsideFieldSet`, `ChoiceRankConflict`

**Regression anchors:**
- `TypeCheckerStructuralTests` — existing choice tests (`EmptyChoice`, `DuplicateChoiceValue`) must pass unchanged

**Files:** `src/Precept/Pipeline/TypeChecker.Expressions.cs` (modify), `test/Precept.Tests/TypeChecker/TypeCheckerStructuralTests.cs` (modify)

- [ ] Add `ValidateChoiceLiteral` in `TypeChecker.Expressions.cs`
- [ ] Wire into comparison resolution path (literal vs. choice field)
- [ ] Wire into assignment resolution path (arg vs. choice field target)
- [ ] Add PRE0087 arg superset check
- [ ] Add PRE0089 rank conflict check
- [ ] Tests: 7 tests
- [ ] Remove 3 codes from Gate 1 allow-list

---

### Slice 3 — PRE0094: InitialEventMissingAssignments

**Purpose:** Close the blocking lifecycle integrity gap. A precept with an initial event can be created without providing values for required fields. D93 (`RequiredFieldsNeedInitialEvent`) fires but its pair D94 does not — the matched enforcement is incomplete.

This slice is already designed in detail in `docs/Working/field-state-guarantees-v3.md` Slice 11. The implementation belongs there; this slice here exists to track it in the diagnostic enforcement context and ensure Gate 1 allow-list removal is coordinated.

**Modify:** `src/Precept/Pipeline/TypeChecker.Validation.cs`, adjacent to the existing D93 emission site (line 325)

**Logic shape:** For each initial event, collect all required fields (non-optional, no default expression, not computed, not a collection type). Walk all transition rows where the event is the initial event and `FromState` is the initial state. If no transition row for the event has a `set` action targeting the required field, emit D94.

**Test file (new):** `test/Precept.Tests/TypeChecker/TypeCheckerLifecycleTests.cs`

Test method names:
- `InitialEvent_RequiredField_NoSetAction_EmitsInitialEventMissingAssignments` — `[Fact]`
- `InitialEvent_RequiredField_SetActionPresent_NoDiagnostic` — `[Fact]`
- `InitialEvent_OptionalField_NoSetAction_NoDiagnostic` — `[Fact]`
- `InitialEvent_FieldWithDefault_NoSetAction_NoDiagnostic` — `[Fact]`
- `InitialEvent_ComputedField_NoSetAction_NoDiagnostic` — `[Fact]`
- `InitialEvent_CollectionField_NoSetAction_NoDiagnostic` — `[Fact]`
- `InitialEvent_MultipleRequiredFields_PartialSet_EmitsForUnset` — `[Fact]`

**Remove from Gate 1 allow-list:** `InitialEventMissingAssignments`

**Regression anchors:**
- `TypeCheckerLifecycleTests` — D93 existing tests must pass (D94 is a companion, not a replacement)
- `TypeCheckerTransitionTests` — transition resolution unaffected

**Files:** `src/Precept/Pipeline/TypeChecker.Validation.cs` (modify), `test/Precept.Tests/TypeChecker/TypeCheckerLifecycleTests.cs` (create or modify)

- [ ] Add D94 check in `ValidateConstructionGuarantees` (or new dedicated method)
- [ ] Wire: collect required fields, walk initial event rows, check action chains
- [ ] Tests: 7 tests (positive + negative for each exemption)
- [ ] Remove `InitialEventMissingAssignments` from Gate 1 allow-list

---

### Slice 4 — PRE0092: EventHandlerInStatefulPrecept

**Purpose:** Close the trivial structural check gap. Event handlers (`on Event -> actions`) are the stateless-precept equivalent of transition rows. Using them in a stateful precept (one with `state` declarations) creates ambiguous execution order semantics.

**Modify:** `src/Precept/Pipeline/TypeChecker.Validation.cs`, in `ValidateStructural`

```csharp
// PRE0092 — EventHandlerInStatefulPrecept
if (ctx.States.Count > 0 && ctx.EventHandlers.Count > 0)
{
    foreach (var handler in ctx.EventHandlers)
    {
        ctx.Emit(Diagnostics.Create(DiagnosticCode.EventHandlerInStatefulPrecept,
            handler.Span, handler.EventName));
    }
}
```

**Test file:** `test/Precept.Tests/TypeChecker/TypeCheckerStructuralTests.cs` (add section)

Test method names:
- `EventHandler_InStatefulPrecept_EmitsEventHandlerInStatefulPrecept` — `[Fact]`
- `EventHandler_InStatelessPrecept_NoDiagnostic` — `[Fact]`
- `EventHandler_MultipleHandlers_InStatefulPrecept_EmitsForEach` — `[Fact]`

**Remove from Gate 1 allow-list:** `EventHandlerInStatefulPrecept`

**Regression anchors:**
- `TypeCheckerStructuralTests` — existing structural validation tests must pass
- `TypeCheckerStatelessTests` — event handler tests in stateless context must pass

**Files:** `src/Precept/Pipeline/TypeChecker.Validation.cs` (modify), `test/Precept.Tests/TypeChecker/TypeCheckerStructuralTests.cs` (modify)

- [ ] Add PRE0092 check in `ValidateStructural`
- [ ] Tests: 3 tests
- [ ] Remove `EventHandlerInStatefulPrecept` from Gate 1 allow-list

---

### Slice 5 — B1: Temporal Constant Precision (PRE0055–0058)

**Purpose:** Replace generic `InvalidTypedConstantContent` (PRE0053) with domain-specific temporal diagnostics. The error is already caught — this slice makes it actionable.

**Modify:** `src/Precept/Pipeline/TypeChecker.Expressions.cs` or `TypeChecker.TypedConstants.cs`

In `TypedConstantValidation` (or wherever temporal constant resolution occurs), intercept the NodaTime parsing failure and select the specific diagnostic based on the failure type:

- `LocalDate` parse fails with "day out of range" → PRE0055 `InvalidDateValue`
- `LocalDate` parse fails with format error → PRE0056 `InvalidDateFormat`
- `LocalTime` parse fails with value out of range → PRE0057 `InvalidTimeValue`
- `Instant` parse fails with missing Z or wrong format → PRE0058 `InvalidInstantFormat`
- IANA timezone lookup fails → PRE0059 `InvalidTimezoneId`

**Test file:** `test/Precept.Tests/TypeChecker/TypeCheckerTypedConstantTests.cs` (add section)

Test method names (representative):
- `DateField_February30_EmitsInvalidDateValue` — `[Fact]`
- `DateField_ValidDate_NoDiagnostic` — `[Fact]`
- `DateField_WrongFormat_EmitsInvalidDateFormat` — `[Fact]`
- `TimeField_HoursOutOfRange_EmitsInvalidTimeValue` — `[Fact]`
- `InstantField_MissingZ_EmitsInvalidInstantFormat` — `[Fact]`
- `TimezoneField_NonCanonicalId_EmitsInvalidTimezoneId` — `[Fact]`

**Remove from Gate 1 allow-list:** `InvalidDateValue`, `InvalidDateFormat`, `InvalidTimeValue`, `InvalidInstantFormat`, `InvalidTimezoneId`

**Regression anchors:**
- `TypeCheckerTypedConstantTests` — existing tests for `UnresolvedTypedConstant` (PRE0052) and `InvalidTypedConstantContent` (PRE0053) must pass; those codes remain for non-temporal constant failures

**Files:** `src/Precept/Pipeline/TypeChecker.Expressions.cs` or `TypeChecker.TypedConstants.cs` (modify), `test/Precept.Tests/TypeChecker/TypeCheckerTypedConstantTests.cs` (modify)

- [ ] Specialize temporal constant validation to select domain-specific diagnostic over generic
- [ ] PRE0055 (`InvalidDateValue`) — calendar date doesn't exist
- [ ] PRE0056 (`InvalidDateFormat`) — wrong format
- [ ] PRE0057 (`InvalidTimeValue`) — out-of-range time components
- [ ] PRE0058 (`InvalidInstantFormat`) — missing trailing Z
- [ ] PRE0059 (`InvalidTimezoneId`) — non-canonical IANA form
- [ ] Tests: 6+ tests (positive + negative for each code)
- [ ] Remove 5 codes from Gate 1 allow-list

---

### Slice 6 — B4: Collection Safety Extensions (PRE0099–0101, PRE0104)

**Purpose:** Extend collection safety enforcement to the newer collection types (list, lookup, queue-by-priority). Existing enforcement covers `UnguardedCollectionAccess` (PRE0063) and `UnguardedCollectionMutation` (PRE0064) for `.peek`/`.min`/`.max`/`pop`/`dequeue`.

**Design split (per remediation analysis):**
- PRE0099 `KeyPresenceSafety` and PRE0100 `IndexBoundsGuard` → ProofEngine (proof obligations parallel to existing `UnguardedCollectionAccess`)
- PRE0101 `KeyUniquenessGuard` → ProofEngine (uniqueness is a proof obligation)
- PRE0104 `MissingOrderingKey` → TypeChecker structural check (collection type must declare an ordering key for `.min`/`.max` to be structurally valid)

**Modify (TypeChecker):** `src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs`

For PRE0104 — when `.min` or `.max` is called on a collection, check whether the collection type declares an ordering key. If not, emit PRE0104.

**Modify (ProofEngine):** `src/Precept/Pipeline/ProofEngine.Diagnostics.cs` (or appropriate ProofEngine file)

For PRE0099 and PRE0100 — add proof obligation checks following the pattern of existing `UnguardedCollectionAccess` checks. For lookup field access by key, require a `contains` guard. For list access by index, require a bounds guard.

**Test files:**
- `test/Precept.Tests/TypeChecker/TypeCheckerCollectionSafetyTests.cs` (create)
- `test/Precept.Tests/ProofEngine/ProofEngineSafetyTests.cs` (modify or create)

Test method names (representative):
- `LookupField_KeyAccess_WithoutContainsGuard_EmitsKeyPresenceSafety` — `[Fact]`
- `LookupField_KeyAccess_WithContainsGuard_NoDiagnostic` — `[Fact]`
- `ListField_IndexAccess_WithoutBoundsGuard_EmitsIndexBoundsGuard` — `[Fact]`
- `LookupField_PutWithoutUniquenessGuard_EmitsKeyUniquenessGuard` — `[Fact]`
- `OrderedCollection_MinWithoutOrderingKey_EmitsMissingOrderingKey` — `[Fact]`
- `OrderedCollection_MinWithOrderingKey_NoDiagnostic` — `[Fact]`

**Remove from Gate 1 allow-list:** `KeyPresenceSafety`, `IndexBoundsGuard`, `KeyUniquenessGuard`, `MissingOrderingKey`

**Regression anchors:**
- `TypeCheckerCollectionTests` — existing collection access tests must pass
- `ProofEngineTests` — existing `UnguardedCollectionAccess` and `UnguardedCollectionMutation` tests must pass

**Files:** `src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs` (modify), `src/Precept/Pipeline/ProofEngine.Diagnostics.cs` (modify), test files (create/modify)

- [ ] Audit: verify whether existing PRE0063/0064 already catches some B4 cases
- [ ] PRE0104 `MissingOrderingKey` — TypeChecker structural check on `.min`/`.max` calls
- [ ] PRE0099 `KeyPresenceSafety` — ProofEngine proof obligation for lookup key access
- [ ] PRE0100 `IndexBoundsGuard` — ProofEngine proof obligation for list index access
- [ ] PRE0101 `KeyUniquenessGuard` — ProofEngine proof obligation for `put` without uniqueness guard
- [ ] Tests: 6+ tests
- [ ] Remove 4 codes from Gate 1 allow-list

---

### Slice 7 — Root Cause A: Parser Guard Gates (PRE0013–0015)

**Purpose:** Replace confusing generic parse errors with precise, actionable diagnostics for three invalid guard-position forms.

**Modify:** `src/Precept/Pipeline/Parser.cs`

In the construct parsing paths for `omit` declarations, event handlers, and transition rows, detect when a `when` token appears in an invalid position:

```csharp
// In ParseOmitDeclaration (or wherever omit constructs are parsed):
// After consuming the field target slot, if next token is 'when':
if (Peek().Kind == TokenKind.When)
{
    var span = Advance().Span; // consume 'when'
    // Consume or skip the guard expression for recovery
    SkipToNextConstruct();
    Emit(Diagnostics.Create(DiagnosticCode.OmitDoesNotSupportGuard, span));
}

// In ParseEventHandler:
// After consuming event name, if next token is 'when':
if (Peek().Kind == TokenKind.When)
{
    var span = Advance().Span;
    SkipGuardExpression();
    Emit(Diagnostics.Create(DiagnosticCode.EventHandlerDoesNotSupportGuard, span));
}

// In ParseTransitionRow:
// After consuming from-state, if next token is 'when' (before 'on Event'):
if (Peek().Kind == TokenKind.When && !SeenOnKeyword)
{
    var span = Advance().Span;
    SkipGuardExpression();
    Emit(Diagnostics.Create(DiagnosticCode.PreEventGuardNotAllowed, span));
}
```

**Test file:** `test/Precept.Tests/Parser/ParserGuardValidationTests.cs` (create)

Test method names:
- `OmitDeclaration_WithGuard_EmitsOmitDoesNotSupportGuard` — `[Fact]`
- `OmitDeclaration_WithoutGuard_NoDiagnostic` — `[Fact]`
- `EventHandler_WithGuard_EmitsEventHandlerDoesNotSupportGuard` — `[Fact]`
- `EventHandler_WithoutGuard_NoDiagnostic` — `[Fact]`
- `TransitionRow_GuardBeforeOnEvent_EmitsPreEventGuardNotAllowed` — `[Fact]`
- `TransitionRow_GuardAfterOnEvent_NoDiagnostic` — `[Fact]`

**Remove from Gate 1 allow-list:** `OmitDoesNotSupportGuard`, `EventHandlerDoesNotSupportGuard`, `PreEventGuardNotAllowed`

**Regression anchors:**
- `ParserScopedConstructTests` — existing omit/event/transition parsing tests must pass
- `ParserCoverageGapTests` — existing coverage gap tests must pass

**Files:** `src/Precept/Pipeline/Parser.cs` (modify), `test/Precept.Tests/Parser/ParserGuardValidationTests.cs` (create)

- [ ] Add `OmitDoesNotSupportGuard` rejection in `ParseOmitDeclaration` (or equivalent)
- [ ] Add `EventHandlerDoesNotSupportGuard` rejection in `ParseEventHandler`
- [ ] Add `PreEventGuardNotAllowed` rejection in `ParseTransitionRow` pre-event guard detection
- [ ] Parser recovery after each rejection (continue parsing remaining constructs)
- [ ] Tests: 6 tests
- [ ] Remove 3 codes from Gate 1 allow-list

---

### Slice 8 — Scattered TypeChecker Gaps

**Purpose:** Wire individual emission sites for the lower-priority scattered codes. These are straightforward additions to existing TypeChecker validation methods, not new validation passes.

**Priority ordering within this slice:**

| Priority | Code | Where | What |
|----------|------|-------|------|
| High | PRE0039 `ComputedFieldWithDefault` | `TypeChecker.Validation.cs` | Computed field (derives via `<-`) cannot also have a `default` expression |
| High | PRE0042 `ConflictingAccessModes` | `TypeChecker.Validation.cs` | Two `modify when` or `readonly`/`editable` declarations conflict on the same field+state |
| High | PRE0043 `RedundantAccessMode` | `TypeChecker.Validation.cs` | Access mode declaration is redundant (field is already unconditionally in that mode) |
| High | PRE0085 `NonChoiceAssignedToChoice` | `TypeChecker.Expressions.cs` | Non-choice value assigned to choice-typed field |
| High | PRE0044 `ListLiteralOutsideDefault` | `TypeChecker.Expressions.cs` | List literal `[a, b, c]` used outside a `default` expression |
| Medium | PRE0027 `DuplicateArgName` | `TypeChecker.Validation.cs` | Duplicate parameter name in event arg list |
| Medium | PRE0035 `InvalidModifierValue` | `TypeChecker.Validation.cs` | Modifier applied with invalid value |
| Medium | PRE0050 `EventArgOutOfScope` | `TypeChecker.Expressions.cs` | Event arg referenced outside a transition row for that event |
| Medium | PRE0067 `MaxPlacesExceeded` | `TypeChecker.Expressions.cs` | Money value exceeds declared max decimal places |
| Medium | PRE0105 `CollectionInnerTypeError` | `TypeChecker.Expressions.cs` | Collection inner type mismatch |
| Deferred | PRE0019, PRE0022, PRE0051, PRE0078 | Various | Require deeper analysis — may already be handled by `TypeMismatch` under a different code |

**Test placement:**

| Code | Test file |
|------|-----------|
| `ComputedFieldWithDefault` | `TypeCheckerStructuralTests.cs` |
| `ConflictingAccessModes` | `TypeCheckerModifierTests.cs` |
| `RedundantAccessMode` | `TypeCheckerModifierTests.cs` |
| `NonChoiceAssignedToChoice` | `TypeCheckerStructuralTests.cs` (choice section) |
| `ListLiteralOutsideDefault` | `TypeCheckerExpressionTests.cs` |
| `DuplicateArgName` | `TypeCheckerStructuralTests.cs` |
| `InvalidModifierValue` | `TypeCheckerModifierTests.cs` |
| `EventArgOutOfScope` | `TypeCheckerExpressionTests.cs` |
| `MaxPlacesExceeded` | `TypeCheckerCurrencyUnitTests.cs` |
| `CollectionInnerTypeError` | `TypeCheckerCollectionSafetyTests.cs` |

**Test naming convention** (consistent with existing test files):
- `{Condition}_{InputDescription}_{ExpectedOutcome}`
- Examples: `ComputedField_WithDefaultExpression_EmitsComputedFieldWithDefault`, `ComputedField_WithoutDefault_NoDiagnostic`

**Remove from Gate 1 allow-list:** Each code as it is wired

**Regression anchors:**
- All existing TypeChecker test files — existing tests in modifier/structural/expression files must pass unchanged

**Files:** `src/Precept/Pipeline/TypeChecker.Validation.cs` (modify), `src/Precept/Pipeline/TypeChecker.Expressions.cs` (modify), multiple test files (modify)

- [ ] Wire `ComputedFieldWithDefault` (PRE0039) + tests (positive + negative)
- [ ] Wire `ConflictingAccessModes` (PRE0042) + tests
- [ ] Wire `RedundantAccessMode` (PRE0043) + tests
- [ ] Wire `NonChoiceAssignedToChoice` (PRE0085) + tests
- [ ] Wire `ListLiteralOutsideDefault` (PRE0044) + tests
- [ ] Wire `DuplicateArgName` (PRE0027) + tests
- [ ] Wire `InvalidModifierValue` (PRE0035) + tests
- [ ] Wire `EventArgOutOfScope` (PRE0050) + tests
- [ ] Wire `MaxPlacesExceeded` (PRE0067) + tests
- [ ] Wire `CollectionInnerTypeError` (PRE0105) + tests
- [ ] Analyze PRE0019, PRE0022, PRE0051, PRE0078 before wiring (may be duplicates under different codes)
- [ ] Remove wired codes from Gate 1 allow-list as each is completed

---

### Test Requirements for All Gap Closure

**Required for every diagnostic code:**

| Assertion type | Requirement | Rationale |
|----------------|-------------|-----------|
| Positive case (fires) | Required — at least 1 per code | Without this, Gate 2's "has a test reference" check is satisfied but the diagnostic is never proved to actually fire. |
| Negative case (doesn't fire on valid input) | Required — at least 1 per code | A diagnostic that fires on valid input is a false positive — the worst outcome for a domain-integrity product. The B2 cluster (`Cost1 + Cost2` with the same currency) is critical. |
| Message text accuracy | Not required | Current tests don't verify message text. Catalog tests in `DiagnosticsTests.cs` already verify `MessageTemplate` shape. |
| Source span accuracy | Not required for type-checker diagnostics; recommended for new parser diagnostics | Parser diagnostics (PRE0013–0015) should spot-check span correctness since the language server uses them for underlines. |

**Pattern:**

```csharp
// Positive case — diagnostic fires on invalid input
[Fact]
public void DescriptiveCondition_EmitsExpectedDiagnostic()
{
    var precept = """
        precept Widget
        // ... minimal DSL that triggers the diagnostic ...
        """;
    TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.ExpectedCode);
}

// Negative case — no diagnostic on valid input
[Fact]
public void DescriptiveCondition_ValidForm_NoDiagnostic()
{
    var precept = """
        precept Widget
        // ... valid form of the same construct ...
        """;
    TypeCheckerTestHelpers.CheckExpectingClean(precept);
}
```

---

## 8. Tracker

### Priority 0 — Enforcement Foundation
- [ ] **Slice 0:** Convention test (Gate 1 + Gate 2) — `DiagnosticEmissionCoverageTests.cs`

### Priority 1 — Integrity Violations (Silent Wrong Behavior)
- [ ] **Slice 1:** Currency/unit arithmetic safety (PRE0070–0074) — `TypeChecker.Expressions.cs` qualifier comparison
- [ ] **Slice 2:** Choice value validation (PRE0086–0089) — `TypeChecker.Expressions.cs` choice literal check
- [ ] **Slice 3:** InitialEventMissingAssignments (PRE0094) — `TypeChecker.Validation.cs`, already designed in field-state-guarantees-v3 Slice 11
- [ ] **Slice 4:** EventHandlerInStatefulPrecept (PRE0092) — trivial structural check in `ValidateStructural`

### Priority 2 — User Experience Gaps
- [ ] **Slice 5:** Temporal constant precision (PRE0055–0058) — specialize `TypedConstantValidation`
- [ ] **Slice 6:** Collection safety extensions (PRE0099–0101, PRE0104) — ProofEngine + TypeChecker
- [ ] **Slice 7:** Parser guard gates (PRE0013–0015) — explicit rejection paths in `Parser.cs`

### Priority 3 — Scattered TypeChecker Gaps
- [ ] **Slice 8:** Scattered emission sites (PRE0027, PRE0035, PRE0039, PRE0042, PRE0043, PRE0044, PRE0050, PRE0067, PRE0085, PRE0105) — individual TypeChecker wires

### Deferred
- [ ] **Period arithmetic safety** (PRE0060–0062) — qualifier-aware temporal type checking, parallel to B2; deferred pending B2 pattern establishment
- [ ] **Precision upgrades** (PRE0019, PRE0022, PRE0051, PRE0078) — deeper analysis needed; may already be covered by `TypeMismatch` or proof obligations under different codes
- [ ] **AmbiguousTypedConstant** (PRE0091) — latent; wire when multi-candidate typed constant resolution ships
- [ ] **OutOfRange** (PRE0079) — reclassify; constant-assignment bounds checking only; runtime value bounds checking is proof-level
- [ ] **Parser expression precision** (PRE0010–0012) — chained comparisons, keywords-as-values, invalid call targets; lower priority than guard gates

---

## 9. Long-Term Evolution

### Catalog-declared `IsImplemented` flag

`DiagnosticMeta` already declares `DiagnosticStage` (Lex/Parse/Type/Graph/Proof). Adding an `IsImplemented` (or `EmissionSite`) property would let the catalog itself declare whether a diagnostic is live or aspirational. A catalog validation test could then verify that every `IsImplemented = true` entry has an actual emission site, and every `IsImplemented = false` entry is tracked in an issue.

This is the architecturally correct long-term answer — it follows the metadata-driven principle: the catalog declares truth, enforcement derives from it. But it requires touching all 132 catalog entries and doesn't prevent the gap from growing if developers forget to set the flag. The convention test is the right first step; the catalog approach is the right second step if the allow-list grows unwieldy.

### Roslyn analyzer as next evolution

If the convention test proves its value and the team wants build-time (IDE) enforcement rather than CI enforcement, promote the logic to a `CompilationEndAction` analyzer in `Precept.Analyzers` as `PRECEPT0027`. The emission-site detection pattern — distinguish `IFieldReferenceOperation` on `DiagnosticCode` members inside `Diagnostics.Create()` invocations or `CIDiagnosticCode` assignments from references in `Diagnostics.GetMeta()` — is well-understood from this analysis.

---

## 10. Open Questions for Shane

1. **PRE0079 `OutOfRange` — compile-time or reclassify?** The catalog declares `DiagnosticStage.Type` (compile-time), but bounds checking against runtime values (`set X = Y` where `Y` could be anything) can't be done statically. Options: (a) wire only for constant assignments where the value is known at compile time, (b) reclassify as a proof obligation and move to ProofEngine, or (c) treat as runtime-only fault. Recommendation is (a) or (b); (c) means removing from the type-checker catalog entry. Direction needed before Slice 8 attempts to wire it.

2. **PRE0070–0074: Dynamic qualifier handling.** When a field has a dynamic qualifier like `money in '{CatalogCurrency}'`, the TypeChecker cannot statically compare currencies. Should the TypeChecker: (a) silently skip cross-currency checks for dynamic qualifiers and let the ProofEngine handle them via Strategy 5, or (b) emit the diagnostic at compile time with a message noting the dynamic qualifier and that the check is partial? The design assumes (a) — confirm before Slice 1 implementation.

3. **PRE0094 priority sequencing.** The field-state-guarantees-v3 work (Slices 10–11 in that doc) depends on PRE0094. Should PRE0094 be fast-tracked ahead of the B2 currency/unit cluster, even though B2 is a higher integrity risk by the philosophy test? Or is the B2 dependency more urgent to address first?

4. **PRE0091 `AmbiguousTypedConstant` — keep or remove?** It's the only truly speculative diagnostic with no emission path and no anticipated feature that would create it. Keeping it on the allow-list is harmless (it blocks Gate 1 without causing failures). Removing it from `DiagnosticCode.cs` is clean but irreversible if multi-candidate resolution ships later. Options: (a) keep on allow-list with a "reserved" comment, (b) remove from the enum and re-add when needed, (c) add an `IsReserved` flag to `DiagnosticMeta`. Recommendation is (a) — confirm.

5. **Scattered Priority 3 precision upgrades.** PRE0019 `NullInNonNullableContext`, PRE0022 `FunctionArgConstraintViolation`, PRE0051 `InvalidInterpolationCoercion`, PRE0078 `NumericOverflow` may already be covered by `TypeMismatch` or other codes. Should we invest in precision upgrades for these (more specific diagnostic on the same condition), or accept the current generic handling and let the allow-list entries remain indefinitely?

6. **Gate 1 allow-list granularity.** The initial allow-list has all 50 unemitted codes. Should each entry cite a specific tracking issue (e.g., `// tracked in #245`), or is a cluster comment (`// Root Cause B2 — not yet implemented`) sufficient? The issue-level citation is richer but requires creating 50 tracking issues. Direction on granularity before Slice 0 implementation.

7. **Convention test scan scope: Pipeline-only vs. all source.** The recommended scan covers `src/Precept/Pipeline/*.cs` + `Operations.cs` + `Functions.cs`. If future emission patterns emerge outside these files (e.g., a new `RuntimeValidator` in `src/Precept/Runtime/`), the scan set needs manual updating. Should the test scan all of `src/Precept/**/*.cs` minus known non-emission files instead, making it automatically inclusive? Broader scope means fewer false negatives but potentially more false positives from non-emission references. Direction before Slice 0 is finalized.

8. **Doc-comment false positives.** The emission-site scan picks up `DiagnosticCode.X` in XML doc comments (`/// <see cref="DiagnosticCode.X"/>`). Currently rare — only one instance exists and `X` is not a real member name. If it becomes a pattern, the scan should strip `// ...` and `/** */` comments before matching. For now, does Shane want comment stripping from the start, or accept the current minimal risk?
