# Diagnostic Gap Closure and Enforcement

> **Status:** Design — Pending Shane Sign-off
> **Author:** Frank (Lead/Architect)
> **Date:** 2026-05-13

---

## 1. Executive Summary

Precept's identity is founded on a single non-negotiable guarantee: invalid configurations are structurally impossible. The compiler is the product's primary proof surface — if the compiler says a definition is valid, it is valid. Every diagnostic code declared in the catalog is an implicit promise to the user: "the compiler will catch this class of error."

Of the 132 diagnostics defined in `DiagnosticCode.cs`, **49 have no emission site** in any pipeline stage. (Originally counted as 50; PRE0094 `InitialEventMissingAssignments` was confirmed wired in `TypeChecker.Validation.FieldState.cs` during the Q3 sequencing review.) They are declared with catalog metadata, documented in the spec, and in most cases already have anticipatory tests — but the compiler never produces them. This is an integrity debt with two layers of consequence:

1. **Silent wrong behavior.** Cross-currency arithmetic compiles clean. A choice field accepts string literals that aren't in its declared value set. A lookup field can be accessed by key without a `contains` guard. The compiler's silence is false validation.

2. **AI legibility failure.** The MCP `precept_diagnostic` tool returns full trigger/recovery metadata for these codes. AI consumers (including this agent) present them to users as working enforcement. They aren't.

This design does two things: it closes the most critical gaps and it installs two Roslyn analyzer gates that prevent future regressions. The analyzers are the enforcement mechanism; the gap closure work is the remediation. Together they form a complete answer to the question "how do we know the catalog's promises are kept?"

**Cross-plan dependency (interval proof engine):** One of the 49 unemitted codes — PRE0078 `NumericOverflow` — is **not addressed by this plan's gap-closure slices.** It is addressed by the interval proof engine (`docs/Working/interval-proof-engine-design.md`), which introduces Strategy 7 (`IntervalContainment`) as the emission mechanism for arithmetic bounds violations. This plan's Gate 1 allow-list carries PRE0078 until interval engine Slice 2 ships, at which point it is removed. See § 8 "Cross-Plan Dependency" for the full coordination table.

**Important correction from the initial gap analysis:** Four diagnostics (PRE0066, PRE0095, PRE0097, PRE0098) were initially reported as "never emitted." These ARE emitted — by `ValidateCIEnforcement` in `TypeChecker.Validation.cs` via catalog-driven dispatch through `Operations.GetMeta().CIDiagnosticCode` and `Functions.GetMeta().CIDiagnosticCode`. The original grep missed them because indirect catalog references don't produce a literal `DiagnosticCode.X` token at the emission call site. **The true gap count is 49, not 54.** (Originally 50; PRE0094 confirmed wired during Q3 review.) This catalog-driven emission pattern is the reason the analyzer emission-site pass must check for indirect dispatch, not just literal references.

---

## 2. Current State

### What the pipeline currently does

The pipeline produces diagnostics through three patterns:

1. **Direct emission:** `Diagnostics.Create(DiagnosticCode.X, span, ...)` at an explicit call site in a pipeline stage. The vast majority of working diagnostics use this pattern.

2. **Catalog-mediated emission:** `CIDiagnosticCode: DiagnosticCode.X` in `Operations.cs` or `Functions.cs`. The CI enforcement cluster (PRE0066, PRE0095, PRE0097, PRE0098) uses this pattern — `ValidateCIEnforcement` reads the catalog property and dispatches without ever mentioning the code by name in the pipeline source.

3. **ProofEngine dispatch:** `DiagnosticCode.X` in ProofEngine switch branches for proof obligation failures. This includes the new **Strategy 7 (IntervalContainment)** path: unresolved `IntervalContainmentProofRequirement` obligations emit `NumericOverflow` (PRE0078). See `docs/Working/interval-proof-engine-design.md` §3.3. Once the interval proof engine Slice 2 ships, PRE0078 transitions from "unemitted" to "emitted via ProofEngine dispatch" — this is the mechanism that closes the PRE0078 gap, not a TypeChecker wire.

**Current emission inventory (as of 2026-05-13):**
- 132 diagnostic codes defined in `DiagnosticCode.cs`
- 84 have emission sites in the pipeline or catalog-emission files (PRE0094 confirmed wired; was previously miscounted as unemitted)
- 49 have no emission site (the gap) — **48 after interval proof engine Slice 2 ships** (PRE0078 `NumericOverflow` transitions to "emitted via ProofEngine Strategy 7"). PRE0094 `InitialEventMissingAssignments` was originally counted but is confirmed wired in `TypeChecker.Validation.FieldState.cs`.
- 7 have neither emission nor test (see § 4.5 below)
- 0 emitted codes are untested — all 84 emitted codes have at least one test reference

**⚠️ Hard dependency on interval proof engine:** PRE0078 (`NumericOverflow`) is NOT addressed by this enforcement plan's gap-closure slices. It is addressed by the interval proof engine (`docs/Working/interval-proof-engine-design.md`), which introduces Strategy 7 (`IntervalContainment`) as the emission mechanism. The interval engine's Slice 2 is the completion gate for PRE0078's transition from "unemitted" to "emitted." This plan's Gate 1 allow-list entry for PRE0078 is removed when interval engine Slice 2 ships — not when this plan's Slice 8 ships.

### What the pipeline does NOT do (and must)

- No diagnostic fires when money fields with different currencies are combined in an arithmetic expression (`CostUSD + CostEUR` compiles clean).
- No diagnostic fires when a choice field is compared to a string literal that isn't in its declared value set (`Status == "Pending"` where `"Pending"` isn't declared compiles clean).
- No diagnostic fires when a lookup field is accessed by key without a `contains` guard.
- No diagnostic fires when a field declared `omit` in a state is used in an expression anchored to that state (covered separately in field-state-guarantees-v3.md).
- ~~No diagnostic fires when an initial event fails to assign all required fields (PRE0094 — this design closes that gap).~~ **Corrected:** PRE0094 IS wired in `TypeChecker.Validation.FieldState.cs`. This was a stale gap-inventory entry.
- Temporal constant errors surface as generic `InvalidTypedConstantContent` (PRE0053) rather than domain-specific diagnostics.
- No diagnostic fires when a typed constant would validate against multiple candidate typed-constant families after inference is introduced; the current resolver has no ambiguity state and therefore cannot emit PRE0091.
- No mechanism exists to detect when a new diagnostic code is added without an emission site, so gaps silently accumulate.

### No enforcement mechanism exists

There is currently no gate — automated or manual — that catches a new diagnostic code added to `DiagnosticCode.cs` without a corresponding emission site in the pipeline. This means the gap can and will grow over time through normal development. The Roslyn analyzer pair in Slice 0 closes this structural hole.

---

## 3. Gap Inventory by Root Cause

### 3.1 Root Cause A — Parser Gates Never Wired (PRE0013–0015)

These three diagnostics are specified in §2.7 (Parser Diagnostics) but the parser never reaches the emission call sites.

| PRE | Name | Invalid construct |
|-----|------|-------------------|
| PRE0013 | `OmitDoesNotSupportGuard` | `omit Field in State when Guard` — `omit` is unconditional structural exclusion |
| PRE0014 | `EventHandlerDoesNotSupportGuard` | `on Event when Guard -> actions` — event handlers don't support guards |
| PRE0015 | `TransitionGuardMustFollowEvent` | `from State when Guard on Event -> ...` — guard must follow `on Event`, not precede it |

**Why missing:** Parser construct dispatch and slot resolution for guards, transitions, and event handlers were built incrementally. The three specific rejection paths for these invalid forms were never added. The parser silently accepts or misparscs these constructs.

**Current user experience:** `omit Notes in Draft when Notes is set` produces a confusing generic parse error about unexpected tokens, or the guard is silently ignored. The spec promises a precise, actionable diagnostic.

### 3.2 Root Cause B1 — Temporal Constant Precision (PRE0055–0062)

The typed constant validation pipeline reaches `TypedConstantValidation` but uses `UnrecognizedTypedConstant` (PRE0052) and `InvalidTypedConstantContent` (PRE0053) as catch-alls instead of selecting the domain-specific diagnostic.

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

#### Scoped companion — PRE0091 `AmbiguousTypedConstant`

PRE0091 is not a temporal-precision variant of PRE0055–0059. It is a typed-constant resolution-path diagnostic that only becomes reachable once the TypeChecker can evaluate **multiple viable target types** for the same single-quoted literal.

**Why missing today:** `ResolveTypedConstant(...)` is currently single-candidate. It is driven by a concrete `expectedType`, delegates to `TypedConstantValidation.Validate(...)`, and either accepts that one target type or emits PRE0052/PRE0053. There is no candidate set, no survivor filtering, and therefore no ambiguity state to report.

**Scoped enforcement target for this plan:** when the typed-constant resolution path is broadened to try multiple candidate target types, retain every candidate whose content validation succeeds. If the survivor count is:

- **0** → preserve the current PRE0052 `UnrecognizedTypedConstant` / PRE0053 `InvalidTypedConstantContent` behavior
- **1** → resolve normally to that typed constant
- **>1** → emit PRE0091 `AmbiguousTypedConstant` naming the conflicting candidate types and return an error expression

**First-tranche boundary:** this plan scopes PRE0091 to the **TypeChecker typed-constant resolution path only** (`ResolveTypedConstant` + validation helpers). It does **not** broaden parser syntax, evaluator/runtime behavior, or proof-engine responsibilities.

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
| PRE0089 | `ChoiceValueOrderMismatch` | Arg order conflicts with field's declared order |

PRE0088 and a related companion (PRE0090 `ChoiceMissingElementType`) are correctly emitted at parse time via `ParseTypeRef()`. PRE0086, PRE0087, and PRE0089 are type-checker-stage checks that require comparing expression literals against resolved choice field declarations — that comparison logic doesn't exist.

**Current user experience:** `rule Status == "Pending"` where Status is `choice of string("Active", "Done")` compiles clean. A typo in a governance rule is invisible to the compiler.

### 3.5 Root Cause B4 — Collection Safety Extensions (PRE0099–0101, PRE0104)

| PRE | Name | Intended trigger |
|-----|------|-----------------|
| PRE0099 | `KeyAccessWithoutWhen` | Key-based lookup access without `contains` guard |
| PRE0100 | `IndexAccessWithoutWhen` | Index-based list access without bounds guard |
| PRE0101 | `DuplicateKeyAddWithoutWhen` | `put` without uniqueness guard |
| PRE0104 | `MissingOrderingKey` | `min`/`max` on collection without declared ordering key |

The existing collection safety system emits PRE0063 `CollectionAccessWithoutWhen` and PRE0064 `CollectionMutationWithoutWhen` for `.peek`/`.min`/`.max` and `pop`/`dequeue`. The newer collection types (list, lookup, queue-by-priority) have additional safety requirements that were never wired.

### 3.6 Root Cause C — Structural Single-Check Gaps (PRE0092, PRE0094)

| PRE | Name | Status |
|-----|------|--------|
| PRE0092 | `EventHandlerInStatefulPrecept` | Specced (§3.8). Trivial structural check — if `ctx.States.Count > 0 && ctx.EventHandlers.Count > 0`, emit. |
| PRE0094 | `InitialEventMissingAssignments` | ~~Specced (§3A.5). Blocking gap.~~ **ALREADY WIRED (confirmed 2026-05-14).** Two emission sites exist in `TypeChecker.Validation.FieldState.cs`. Q3 decision confirmed this; the original gap-inventory claim that "D94 does not fire" is stale. PRE0094 is not part of the active gap — remove from Slice 3 scope and Gate 1 allow-list. |

### 3.7 Root Cause D — Scattered TypeChecker Gaps (17 diagnostics)

Individual emission sites missing across multiple TypeChecker validation methods. Most are straightforward wires.

**D1: Parser expression precision (PRE0010–0012)** — chained comparisons, keywords-as-values, invalid call targets. Parser catches the error condition but emits generic `ExpectedToken` instead of the domain-specific code.

**D2: TypeChecker validation gaps (13 codes):**

| PRE | Name | Theme |
|-----|------|-------|
| PRE0019 | ~~`NullInNonNullableContext`~~ | ~~Null safety~~ **RETIRED (2026-05-14).** Subsumed by PRE0116 `FieldMayBeAbsent`. See Q5 addendum. |
| PRE0022 | `FunctionArgumentInvalid` | Function args |
| PRE0027 | `DuplicateArgName` | Duplicate detection |
| PRE0035 | `InvalidModifierValue` | Modifier validation |
| PRE0039 | `ComputedFieldWithDefault` | Computed field |
| PRE0042 | `ConflictingAccessModes` | Access modes |
| PRE0044 | `ListLiteralOutsideDefault` | List literals |
| PRE0050 | `EventArgOutOfScope` | Scope rules |
| PRE0051 | `NonTextTypeInStringInterpolation` | Interpolation |
| PRE0067 | `MaxPlacesExceeded` | Business domain |
| PRE0085 | `ValueNotInChoiceSet` | Choice types |
| PRE0105 | `CollectionElementTypeMismatch` | Collections |
| PRE0043 | `RedundantAccessMode` | Access modes |

**D3: ProofEngine-owned gap — now addressed by interval proof engine (1 code):**

| PRE | Name | Resolution |
|-----|------|------------|
| PRE0078 | `NumericOverflow` | **Owned by Strategy 7 (IntervalContainment) in the interval proof engine.** Not a TypeChecker gap — it is a ProofEngine obligation failure diagnostic. See `docs/Working/interval-proof-engine-design.md` §3.2–§3.3. The interval engine emits `NumericOverflow` when an `IntervalContainmentProofRequirement` is unresolved (computed interval exceeds target field's declared bounds). Gate 1 allow-list removal coordinated with interval engine Slice 2 completion. |

### 3.8 Codes With Neither Emission Nor Test (7 codes)

These 7 codes have no emission site AND no test file reference anywhere in the test suite. They are the floor of the debt:

| Code | Notes |
|------|-------|
| `AmbiguousTypedConstant` (PRE0091) | Scoped in Slice 5A. Blocked on multi-candidate typed-constant resolution landing in the TypeChecker path; not a parser/runtime change. |
| `EventHandlerDoesNotSupportGuard` (PRE0014) | Root Cause A — parser gate never wired |
| `EventHandlerInStatefulPrecept` (PRE0092) | Root Cause C — trivial structural check not wired |
| `OmitDoesNotSupportGuard` (PRE0013) | Root Cause A — parser gate never wired |
| `OutOfRange` (PRE0079) | **CONFIRMED (2026-05-14).** Emission-site audit found zero live emitters — `DiagnosticCode.OutOfRange` exists only in the enum, `GetMeta()` catalog, `FaultCode.OutOfRange` `[StaticallyPreventable]` link, and `Faults.GetMeta()`. No pipeline stage (TypeChecker, GraphAnalyzer, ProofEngine, Evaluator) emits it; no test references it. The interval proof engine (`docs/Working/interval-proof-engine-design.md`) now owns *expression-level* bounds checking via `IntervalContainmentProofRequirement` → `NumericOverflow` (PRE0078). PRE0079 retains a narrower scope: constant assignments where a literal exceeds `min`/`max` bounds can be caught trivially at type-check time without interval arithmetic. **Verdict: wire in TypeChecker** as a constant-literal-assignment bounds check. Not subsumed — PRE0078 and PRE0079 are complementary diagnostics at different pipeline stages for different scenarios (Q1/Q10 resolved). |
| `TransitionGuardMustFollowEvent` (PRE0015) | Root Cause A — parser gate never wired |
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

### 5.1 Enforcement mechanism: Roslyn analyzers for Gate 1 + Gate 2

**Decision: Roslyn analyzers** in `src/Precept.Analyzers` are the enforcement path for both gates. The prior convention-test approach is retired.

**Planned analyzer IDs:**
- `PRECEPT0027` — Gate 1: every `DiagnosticCode` must have an emission site or be allow-listed
- `PRECEPT0028` — Gate 2: every emitted `DiagnosticCode` must be referenced by at least one test or be allow-listed

**Rationale:** We want enforcement at authoring and build time (IDE + CI), not only test-run time. The emission-site distinction still requires semantic analysis; analyzers make that cost worthwhile by turning it into always-on governance.

A `CompilationEndAction` implementation must still distinguish:
- `Diagnostics.GetMeta(DiagnosticCode.X)` references in `Diagnostics.cs` — catalog reads, NOT emissions
- `Diagnostics.Create(DiagnosticCode.X, ...)` in pipeline stages — real emissions
- `CIDiagnosticCode: DiagnosticCode.X` in `Operations.cs`/`Functions.cs` — catalog-mediated emissions

**Accepted tradeoff:** Higher implementation complexity than simple convention scans, in exchange for immediate feedback and stronger drift resistance. The catalog-declared `IsImplemented` flag (see § 5.3) remains the architecturally correct truth source for a future simplification pass.

### 5.2 Two-gate design

The analyzer pair enforces two distinct properties:

**Gate 1 — Emission site existence:** Every `DiagnosticCode` member must have at least one reference (direct or catalog-mediated) in a pipeline emission-site source file, OR be on the allow-list with a tracking comment.

**Gate 2 — Test coverage:** Every emitted diagnostic code must be referenced in at least one test file. Gate 2's scope is codes that pass Gate 1 — codes on the Gate 1 allow-list have no test obligation because there's nothing to test.

**Current state that makes Gate 2 start clean:** All 83 currently-emitted codes already have at least one `DiagnosticCode.{MemberName}` reference in the test suite. Gate 2's allow-list starts empty. This is notable — 42 of the 49 unemitted codes also have anticipatory test references (tests written expecting future implementation). Only the 7 codes in § 3.8 above have neither emission nor test.

### 5.3 Gap closure approach

**Post-resolution validation for TypeChecker gaps.** New checks follow the `ValidateCIEnforcement` pattern: a validation method that walks already-resolved `TypedExpression` trees and `TypedAction` chains after Pass 2 resolution is complete. This avoids threading state context into resolution and keeps validation cohesive.

**Parser rejection paths for Root Cause A.** Simple pattern detection in the parser: when a `when` token appears in a position where it's invalid (`omit` context, event handler context, pre-event context), emit the specific diagnostic and continue parsing.

**TypeChecker qualifier comparison for B2.** After `Operations.GetMeta()` resolves a business-domain binary operation, add a post-resolution check that compares qualifier values (currency code, unit, dimension) from both operands. The TypeChecker handles static qualifiers; dynamic qualifiers (`money in '{CatalogCurrency}'`) are deferred to the ProofEngine.

**Choice literal comparison for B3.** When a `TypedLiteral` is compared to or assigned to a choice field, check whether the literal's value exists in the field's declared choice set. The choice value set is already parsed and available in the field's type reference.

**Typed-constant candidate arbitration for PRE0091.** Keep typed-constant parsing in the TypeChecker. Once candidate enumeration exists, `ResolveTypedConstant` should validate the raw literal against each eligible candidate type, keep the successful survivors, resolve on a single survivor, emit PRE0091 for multiple survivors, and preserve PRE0052/PRE0053 for zero survivors. This keeps ambiguity handling in the same stage that owns typed-constant context propagation.

### 5.4 Alternatives considered and rejected

**Option: Wire currency/unit checks in the ProofEngine rather than TypeChecker.** The ProofEngine already handles qualifier compatibility for proof obligations (`PRE0114 QualifiersMayBeIncompatible`). Extending it would work but places a type-level check in the proof stage, blurring the stage boundary. The Operations catalog declares `DiagnosticStage.Type` for these codes — they belong in the TypeChecker. Proof obligations are about provability ("can we prove this division won't be zero?"), not type-level domain rules ("these currencies must match"). The boundary must stay clean.

**Option: Accept generic temporal diagnostics, deprecate PRE0055–0062.** `InvalidTypedConstantContent` is sufficient in that it catches the error. But domain-specific diagnostics exist precisely to give precision. "February 30 doesn't exist" is better than "invalid typed constant content." The NodaTime exception already contains the specific failure reason; we're just not selecting the right code.

**Option: xUnit convention tests as enforcement gates.** Rejected. They run at test time only, duplicate analyzer semantics, and weaken IDE/build-time drift resistance.

---

## 6. Spec Gaps

### Gap 1: Catalog-driven emission not documented as a pattern

The spec and contributing guide describe direct emission (`Diagnostics.Create(...)`) but not catalog-mediated emission via `CIDiagnosticCode`. This caused the initial audit to miscount the gap (54 instead of 49). Any gap audit tool — including the Gate 1 analyzer — must search for both patterns.

**Annotation needed:** `DiagnosticMeta` documentation should note that `CIDiagnosticCode` on operation/function metadata constitutes an emission site equivalent to `Diagnostics.Create(...)`.

### Gap 2: §3.6 qualifier compatibility scope unclear

The spec says "Same currency required" for money±money but doesn't specify whether this applies to fields with dynamic qualifiers (`money in '{CatalogCurrency}'`). The design here treats dynamic qualifiers as TypeChecker-exempt and ProofEngine-governed. This should be documented in §3.6.

### Gap 3: Choice type enforcement split across stages

§3.8 documents PRE0086–0090 as a unit but they actually span two stages: PRE0088 and PRE0090 fire at parse time; PRE0086, PRE0087, PRE0089 fire at type-check time. The spec should clarify this split.

---

## 7. Implementation Plan

> **Quality bar:** Method-level specificity for Slices 0–4 and 5A. Cluster-level specificity for Slices 5–8. Every slice has file paths, test method names, regression anchors, and a checklist.

### Architectural Approach

**Analyzer enforcement first.** Slice 0 installs the enforcement mechanism before any gap is closed. Once the analyzer pair exists with all 49 unemitted codes in the Gate 1 allow-list, every subsequent gap-closure slice automatically validates its own work: the act of removing a code from the allow-list is the slice's completion gate.

**Gap closure follows priority, not technical dependency.** The ordering within each priority tier is largely independent.

### Ordering Constraints

```
Slice 0 (Roslyn analyzers) ───────────────────────────────→ scaffolding for all slices

Slice 1 (B2 currency/unit) ──→ independent of all others
Slice 2 (B3 choice)        ──→ independent of all others
Slice 3 (PRE0094)          ──→ ✅ ALREADY WIRED — no implementation needed
Slice 4 (PRE0092)          ──→ independent (trivial)

Slice 5 (B1 temporal)      ──→ independent
Slice 5A (PRE0091)         ──→ after typed-constant resolution extraction in the parser/typechecker split; no parser gate dependency
Slice 6 (B4 collection)    ──→ independent
Slice 7 (A parser gates)   ──→ independent

Slice 8 (scattered D)      ──→ independent (individual wires); PRE0078 REMOVED — owned by interval engine

Slice 9A (modifier catalog)──→ after Slice 8 wires PRE0035/PRE0042 (mechanism migration, not gap closure)
Slice 9B (typed-const cat.) ──→ after Slice 5 OR independent (subsumes Slice 5 if ordered first)
Slice 9C (proof catalog)   ──→ AFTER interval engine Slice 2 (must include Strategy 7 in audit)

PRE0078 Gate 1 removal     ──→ AFTER interval engine Slice 2 completes (external dependency)
```

All gap-closure slices (1–8, plus 5A) depend on Slice 0 only in the soft sense: Slice 0 is the mechanism that validates completion. The code changes in Slices 1–8 and 5A do not have a code dependency on Slice 0.

Slices 9A–9C are **mechanism-migration slices**, not gap-closure slices. They do not add new diagnostic coverage — they refactor how existing emission is dispatched. Their completion gate is behavioral equivalence (same diagnostics fire on same inputs) plus analyzer recognition of the new indirect emission paths. They are lower priority than gap closure and should be sequenced after their respective prerequisite gap-closure slices ship.

PRE0091 is the one deliberate exception to the otherwise-independent ordering rule: it should follow the **type-checker side** of the ongoing parser/typechecker split if that split is actively moving `ResolveTypedConstant` and typed-constant helpers into their own file. It does **not** depend on the parser guard-gate work in Slice 7 and should not be blocked on unrelated parser cleanup.

---

### Slice 0 — Roslyn Analyzer Foundation: Gate 1 + Gate 2

**Purpose:** Install the two-gate enforcement mechanism in `Precept.Analyzers`. Once this slice ships, all future gap-closure work is automatically validated at build/IDE time. The Gate 1 allow-list starts with all 49 currently-unemitted codes (including PRE0078, which will be removed by the interval proof engine's Slice 2 rather than by this plan's gap-closure slices); each subsequent slice removes entries as gaps are closed.

**Analyzer architecture (authoritative for Gate 1 + Gate 2):**
- `PRECEPT0027` and `PRECEPT0028` are separate analyzers that share one semantic scanner and one allow-list source.
- A shared scanner computes three sets from the compilation: all catalog codes, emitted codes, and test-referenced codes.
- Gate 1 reports against `all - emitted - Gate1AllowList`; Gate 2 reports against `emitted - tested - Gate2AllowList`.
- Stale-entry checks run in both gates so allow-lists only represent active exceptions.

**Expected files (Slice 0 deliverables):**
- `src/Precept.Analyzers/Precept0027DiagnosticEmissionCoverage.cs` (Gate 1 analyzer)
- `src/Precept.Analyzers/Precept0028DiagnosticTestCoverage.cs` (Gate 2 analyzer)
- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` (shared Gate 1 / Gate 2 allow-lists)
- `src/Precept.Analyzers/DiagnosticCoverageScanner.cs` (shared emission/test coverage discovery)
- `test/Precept.Analyzers.Tests/Precept0027Tests.cs`
- `test/Precept.Analyzers.Tests/Precept0028Tests.cs`

**Gate 1 (`PRECEPT0027`) behavior:**
- Build the emitted-code set from pipeline emission contexts:
  - `Diagnostics.Create(DiagnosticCode.X, ...)`
  - `CIDiagnosticCode: DiagnosticCode.X` assignments in operations/functions metadata
  - ProofEngine dispatch branches that emit `DiagnosticCode.X`
- Exclude catalog reads (`Diagnostics.GetMeta(DiagnosticCode.X)`) and enum-definition references.
- Report diagnostics for any `DiagnosticCode` member not emitted and not in `Gate1AllowList`.
- Report stale allow-list entries when a Gate 1 allow-listed code is now emitted.

**Gate 2 (`PRECEPT0028`) behavior:**
- Use Gate 1 emitted set as the source of truth for "must be tested."
- Scan test projects for `DiagnosticCode.{MemberName}` references.
- Report diagnostics for emitted codes missing test references and not in `Gate2AllowList`.
- Report stale Gate 2 allow-list entries when test coverage now exists.

**Scope of "covered":**
- **Emission coverage:** semantic detection of real emission contexts (not raw string grep).
- **Test coverage:** reference presence in test source (`DiagnosticCode.{MemberName}`), with allow-list exceptions.

**What it does NOT catch:** Dead code paths that are syntactically present but unreachable at runtime, behavioral correctness of test assertions, or spec documentation completeness.

**Allow-list ownership and location:**
- **Location:** `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs`
- **Ownership:** Runtime/diagnostics maintainers who touch `src/Precept/Language/DiagnosticCode.cs` or diagnostic emission wiring must update this file in the same PR.
- **Policy:** Gate 1 allow-list entries require root-cause comments; Gate 2 allow-list is exception-only and should remain empty unless explicitly justified.

**Regression anchors:**
- Existing analyzer patterns in `src/Precept.Analyzers` (`PRECEPT0001`–`PRECEPT0023`)
- Existing analyzer test harness in `test/Precept.Analyzers.Tests/AnalyzerTestHelper.cs`

**Files:** `src/Precept.Analyzers/*.cs`, `test/Precept.Analyzers.Tests/*.cs` (create)

- [x] Create shared coverage scanner + allow-list infrastructure in `src/Precept.Analyzers`
- [x] Populate `Gate1AllowList` with all unemitted codes (30 entries, with cluster comments)
- [x] Keep `Gate2AllowList` empty at initialization (all currently emitted diagnostics are test-referenced)
- [x] Implement `PRECEPT0027` Gate 1 emission-coverage analyzer + stale-entry check (`PRECEPT0029`)
- [x] Implement `PRECEPT0028` Gate 2 test-coverage analyzer + stale-entry check (`PRECEPT0030`)
- [x] Add analyzer tests for positive, negative, and stale allow-list cases
- [x] Verify analyzers fire in analyzer test harness and in solution build

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

- [x] Add `ValidateQualifierCompatibility` method in `TypeChecker.Expressions.cs`
- [x] Add `TryGetStaticQualifiers` helper to extract qualifier from resolved types
- [x] Wire into binary operation resolution path
- [x] Tests: 8 tests (positive + negative for each diagnostic code)
- [x] Remove 5 codes from Gate 1 allow-list in `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs`
- [x] Verify Gate 1 staleness check now flags removed entries (proves the round-trip works)

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

PRE0087 (`ChoiceArgOutsideFieldSet`) fires when an event arg's `choice` type includes values outside the target field's declared set. PRE0089 (`ChoiceValueOrderMismatch`) fires when the arg's rank ordering contradicts the field's declared order.

**Test file:** `test/Precept.Tests/TypeChecker/TypeCheckerStructuralTests.cs` (add section after existing choice tests)

Test method names:
- `ChoiceField_LiteralNotInSet_EmitsChoiceLiteralNotInSet` — `[Fact]`
- `ChoiceField_ValidLiteral_NoDiagnostic` — `[Fact]`
- `ChoiceField_LiteralCaseMismatch_EmitsChoiceLiteralNotInSet` — `[Fact]`
- `ChoiceArg_ValueOutsideFieldSet_EmitsChoiceArgOutsideFieldSet` — `[Fact]`
- `ChoiceArg_ValuesSubsetOfFieldSet_NoDiagnostic` — `[Fact]`
- `ChoiceArg_RankConflictsWithField_EmitsChoiceValueOrderMismatch` — `[Fact]`
- `ChoiceArg_RankMatchesField_NoDiagnostic` — `[Fact]`

**Remove from Gate 1 allow-list:** `ChoiceLiteralNotInSet`, `ChoiceArgOutsideFieldSet`, `ChoiceValueOrderMismatch`

**Regression anchors:**
- `TypeCheckerStructuralTests` — existing choice tests (`EmptyChoice`, `DuplicateChoiceValue`) must pass unchanged

**Files:** `src/Precept/Pipeline/TypeChecker.Expressions.cs` (modify), `test/Precept.Tests/TypeChecker/TypeCheckerStructuralTests.cs` (modify)

- [x] Add `ValidateChoiceLiteral` in `TypeChecker.Expressions.cs`
- [x] Wire into comparison resolution path (literal vs. choice field)
- [x] Wire into assignment resolution path (arg vs. choice field target)
- [x] Add PRE0087 arg superset check
- [x] Add PRE0089 rank conflict check
- [x] Tests: 7 tests
- [x] Remove 3 codes from Gate 1 allow-list

---

### Slice 3 — PRE0094: InitialEventMissingAssignments ✅ ALREADY WIRED

> **Status: No implementation needed.** PRE0094 is confirmed wired in `TypeChecker.Validation.FieldState.cs` (two emission sites at lines 342 and 363, inside `ValidateConstructionGuarantees`). The original gap-inventory claim that "D94 does not fire" was stale — D94 fires today. Q3's sequencing decision documented this finding. PRE0094 does not belong in the Gate 1 allow-list and does not need a gap-closure slice.

~~**Purpose:** Close the blocking lifecycle integrity gap. A precept with an initial event can be created without providing values for required fields. D93 (`RequiredFieldsNeedInitialEvent`) fires but its pair D94 does not — the matched enforcement is incomplete.~~

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

- [x] Add PRE0092 check in `ValidateStructural`
- [x] Tests: 3 tests
- [ ] Remove `EventHandlerInStatefulPrecept` from Gate 1 allow-list (allow-list file does not exist yet; TODO comment added at emission site)

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
- `TypeCheckerTypedConstantTests` — existing tests for `UnrecognizedTypedConstant` (PRE0052) and `InvalidTypedConstantContent` (PRE0053) must pass; those codes remain for non-temporal constant failures

**Files:** `src/Precept/Pipeline/TypeChecker.Expressions.cs` or `TypeChecker.TypedConstants.cs` (modify), `test/Precept.Tests/TypeChecker/TypeCheckerTypedConstantTests.cs` (modify)

- [x] Specialize temporal constant validation to select domain-specific diagnostic over generic
- [x] PRE0055 (`InvalidDateValue`) — calendar date doesn't exist
- [x] PRE0056 (`InvalidDateFormat`) — wrong format
- [x] PRE0057 (`InvalidTimeValue`) — out-of-range time components
- [x] PRE0058 (`InvalidInstantFormat`) — missing trailing Z
- [ ] PRE0059 (`InvalidTimezoneId`) — non-canonical IANA form (deferred: requires ZonedDateTime/Timezone validation split)
- [x] Tests: 6+ tests (positive + negative for each code)
- [x] Remove 4 codes from Gate 1 allow-list (PRE0055–0058; PRE0059 remains pending)

---

### Slice 5A — PRE0091: Ambiguous Typed Constant Resolution

**Purpose:** Activate PRE0091 in a narrowly-scoped way once the TypeChecker supports **multi-candidate typed-constant resolution**. This slice adds an ambiguity outcome to the typed-constant resolver; it does not redesign typed constants as a whole.

**Behavior added in this tranche:**
- `ResolveTypedConstant` may evaluate more than one candidate target type for the same raw literal when context is inference-based rather than a single fixed `expectedType`
- If exactly one candidate validates, resolution succeeds unchanged
- If multiple candidates validate, emit PRE0091 `AmbiguousTypedConstant` and surface the competing type names in the diagnostic payload
- If no candidates validate, preserve current PRE0052/PRE0053 behavior

**Pipeline landing zone:** `src/Precept/Pipeline/TypeChecker.Expressions.cs` in the typed-constant resolution path (`ResolveTypedConstant`), with validation continuing to flow through `src/Precept/Language/TypedConstantValidation.cs`

**Explicitly out of scope for this tranche:**
- Parser grammar or tokenization changes
- Runtime evaluator behavior changes
- ProofEngine ambiguity handling
- Broad temporal-precision work already tracked in Slice 5
- Heuristic inference beyond the candidate set already available to the TypeChecker
- Reclassifying PRE0091 in the catalog or removing the diagnostic

**Design notes:**
- Keep the current expected-type fast path. PRE0091 should only appear when the resolver is genuinely operating over a candidate set.
- The first concrete ambiguity case should come from existing temporal families that already share a literal surface (for example, `'30 days'` validating as both `duration` and `period`).
- Prefer a small helper (`ResolveTypedConstantCandidates` or equivalent) that returns 0/1/many survivors without moving validation logic out of `TypedConstantValidation`.
- If richer survivor metadata is needed for the diagnostic message, extend `TypedConstantParseResult` surgically rather than forking validator-specific result shapes.

**Code surfaces/files likely impacted:**
- `src/Precept/Pipeline/TypeChecker.Expressions.cs` — `ResolveTypedConstant` control flow and ambiguity emission
- `src/Precept/Language/TypedConstantValidation.cs` — candidate-by-candidate validation entry point reuse
- `src/Precept/Language/TypedConstantParseResult.cs` — only if survivor metadata is needed for PRE0091 payload shaping
- `test/Precept.Tests/TypeChecker/TypeCheckerTypedConstantTests.cs` — new ambiguity coverage

**Tests to add:**
- `TypedConstant_MultipleTemporalCandidates_EmitsAmbiguousTypedConstant` — `[Fact]`
- `TypedConstant_SingleCandidate_ResolvesWithoutDiagnostic` — `[Fact]`
- `TypedConstant_WithExplicitExpectedType_SkipsAmbiguityPath` — `[Fact]`
- `TypedConstant_NoCandidateMatch_PreservesUnresolvedOrInvalidBehavior` — `[Fact]`
- `TypedConstant_QualifierFilteredToSingleCandidate_NoAmbiguityDiagnostic` — `[Fact]` (only if qualifier filtering participates in the first tranche)

**Acceptance criteria:**
- PRE0091 is emitted only from the TypeChecker typed-constant resolution path, never from the parser
- Existing single-expected-type typed-constant tests continue to pass unchanged
- At least one real multi-candidate literal (expected first: temporal quantity form) now produces PRE0091
- Zero-candidate paths still emit PRE0052/PRE0053 rather than PRE0091
- `AmbiguousTypedConstant` is removed from the Gate 1 allow-list once emission + tests exist

- [x] Confirm the parser/typechecker split has stabilized the owning typed-constant file boundary (`TypeChecker.Expressions.cs` vs. `TypeChecker.Expressions.TypedConstants.cs`)
- [x] Add a candidate-enumeration helper in the TypeChecker typed-constant path
- [x] Preserve the single-expected-type fast path
- [x] Emit PRE0091 when multiple validated survivors remain
- [x] Tests: ambiguity + unique-candidate + zero-candidate + explicit-context cases
- [x] Remove `AmbiguousTypedConstant` from Gate 1 allow-list

---

### Slice 6 — B4: Collection Safety Extensions (PRE0099–0101, PRE0104)

**Purpose:** Extend collection safety enforcement to the newer collection types (list, lookup, queue-by-priority). Existing enforcement covers `CollectionAccessWithoutWhen` (PRE0063) and `CollectionMutationWithoutWhen` (PRE0064) for `.peek`/`.min`/`.max`/`pop`/`dequeue`.

**Design split (per remediation analysis):**
- PRE0099 `KeyAccessWithoutWhen` and PRE0100 `IndexAccessWithoutWhen` → ProofEngine (proof obligations parallel to existing `CollectionAccessWithoutWhen`)
- PRE0101 `DuplicateKeyAddWithoutWhen` → ProofEngine (uniqueness is a proof obligation)
- PRE0104 `MissingOrderingKey` → TypeChecker structural check (collection type must declare an ordering key for `.min`/`.max` to be structurally valid)

**Modify (TypeChecker):** `src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs`

For PRE0104 — when `.min` or `.max` is called on a collection, check whether the collection type declares an ordering key. If not, emit PRE0104.

**Modify (ProofEngine):** `src/Precept/Pipeline/ProofEngine.Diagnostics.cs` (or appropriate ProofEngine file)

For PRE0099 and PRE0100 — add proof obligation checks following the pattern of existing `CollectionAccessWithoutWhen` checks. For lookup field access by key, require a `contains` guard. For list access by index, require a bounds guard.

**Test files:**
- `test/Precept.Tests/TypeChecker/TypeCheckerCollectionSafetyTests.cs` (create)
- `test/Precept.Tests/ProofEngine/ProofEngineSafetyTests.cs` (modify or create)

Test method names (representative):
- `LookupField_KeyAccess_WithoutContainsGuard_EmitsKeyAccessWithoutWhen` — `[Fact]`
- `LookupField_KeyAccess_WithContainsGuard_NoDiagnostic` — `[Fact]`
- `ListField_IndexAccess_WithoutBoundsGuard_EmitsIndexAccessWithoutWhen` — `[Fact]`
- `LookupField_PutWithoutUniquenessGuard_EmitsDuplicateKeyAddWithoutWhen` — `[Fact]`
- `OrderedCollection_MinWithoutOrderingKey_EmitsMissingOrderingKey` — `[Fact]`
- `OrderedCollection_MinWithOrderingKey_NoDiagnostic` — `[Fact]`

**Remove from Gate 1 allow-list:** `KeyAccessWithoutWhen`, `IndexAccessWithoutWhen`, `DuplicateKeyAddWithoutWhen`, `MissingOrderingKey`

**Regression anchors:**
- `TypeCheckerCollectionTests` — existing collection access tests must pass
- `ProofEngineTests` — existing `CollectionAccessWithoutWhen` and `CollectionMutationWithoutWhen` tests must pass

**Files:** `src/Precept/Pipeline/TypeChecker.Expressions.Callables.cs` (modify), `src/Precept/Pipeline/ProofEngine.Diagnostics.cs` (modify), test files (create/modify)

- [x] Audit: verify whether existing PRE0063/0064 already catches some B4 cases
- [x] PRE0104 `MissingOrderingKey` — TypeChecker structural check on `.min`/`.max` calls
- [ ] PRE0099 `KeyAccessWithoutWhen` — ProofEngine proof obligation for lookup key access (blocked: lookup accessor not yet in catalog)
- [x] PRE0100 `IndexAccessWithoutWhen` — ProofEngine proof obligation for list index access
- [ ] PRE0101 `DuplicateKeyAddWithoutWhen` — ProofEngine proof obligation for `put` without uniqueness guard (blocked: lookup put action not yet in catalog)
- [x] Tests: 10 tests in TypeCheckerCollectionSafetyTests
- [x] Remove 2 codes from Gate 1 allow-list (IndexBoundsGuard, MissingOrderingKey)

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
    Emit(Diagnostics.Create(DiagnosticCode.TransitionGuardMustFollowEvent, span));
}
```

**Test file:** `test/Precept.Tests/Parser/ParserGuardValidationTests.cs` (create)

Test method names:
- `OmitDeclaration_WithGuard_EmitsOmitDoesNotSupportGuard` — `[Fact]`
- `OmitDeclaration_WithoutGuard_NoDiagnostic` — `[Fact]`
- `EventHandler_WithGuard_EmitsEventHandlerDoesNotSupportGuard` — `[Fact]`
- `EventHandler_WithoutGuard_NoDiagnostic` — `[Fact]`
- `TransitionRow_GuardBeforeOnEvent_EmitsTransitionGuardMustFollowEvent` — `[Fact]`
- `TransitionRow_GuardAfterOnEvent_NoDiagnostic` — `[Fact]`

**Remove from Gate 1 allow-list:** `OmitDoesNotSupportGuard`, `EventHandlerDoesNotSupportGuard`, `TransitionGuardMustFollowEvent`

**Regression anchors:**
- `ParserScopedConstructTests` — existing omit/event/transition parsing tests must pass
- `ParserCoverageGapTests` — existing coverage gap tests must pass

**Files:** `src/Precept/Pipeline/Parser.cs` (modify), `test/Precept.Tests/Parser/ParserGuardValidationTests.cs` (create)

- [x] Add `OmitDoesNotSupportGuard` rejection in `ParseOmitDeclaration` (or equivalent)
- [x] Add `EventHandlerDoesNotSupportGuard` rejection in `ParseEventHandler`
- [x] Add `TransitionGuardMustFollowEvent` rejection in `ParseTransitionRow` pre-event guard detection
- [x] Parser recovery after each rejection (continue parsing remaining constructs)
- [x] Tests: 6 tests
- [x] Remove 3 codes from Gate 1 allow-list

---

### Slice 8 — Scattered TypeChecker Gaps

**Purpose:** Wire individual emission sites for the lower-priority scattered codes. These are straightforward additions to existing TypeChecker validation methods, not new validation passes.

**Priority ordering within this slice:**

| Priority | Code | Where | What |
|----------|------|-------|------|
| High | PRE0039 `ComputedFieldWithDefault` | `TypeChecker.Validation.cs` | Computed field (derives via `<-`) cannot also have a `default` expression |
| High | PRE0042 `ConflictingAccessModes` | `TypeChecker.Validation.cs` | Two `modify when` or `readonly`/`editable` declarations conflict on the same field+state |
| High | PRE0043 `RedundantAccessMode` | `TypeChecker.Validation.cs` | Access mode declaration is redundant (field is already unconditionally in that mode) |
| High | PRE0085 `ValueNotInChoiceSet` | `TypeChecker.Expressions.cs` | Non-choice value assigned to choice-typed field |
| High | PRE0044 `ListLiteralOutsideDefault` | `TypeChecker.Expressions.cs` | List literal `[a, b, c]` used outside a `default` expression |
| Medium | PRE0027 `DuplicateArgName` | `TypeChecker.Validation.cs` | Duplicate parameter name in event arg list |
| Medium | PRE0035 `InvalidModifierValue` | `TypeChecker.Validation.cs` | Modifier applied with invalid value |
| Medium | PRE0050 `EventArgOutOfScope` | `TypeChecker.Expressions.cs` | Event arg referenced outside a transition row for that event |
| Medium | PRE0067 `MaxPlacesExceeded` | `TypeChecker.Expressions.cs` | Money value exceeds declared max decimal places |
| Medium | PRE0105 `CollectionElementTypeMismatch` | `TypeChecker.Expressions.cs` | Collection inner type mismatch |
| Deferred | PRE0022, PRE0048, PRE0051 | Various | Require deeper analysis — may already be handled by `TypeMismatch` under a different code |

> **Note:** PRE0019 `NullInNonNullableContext` was previously listed in this table. It has been removed — PRE0019 is **architecturally subsumed by PRE0116** `FieldMayBeAbsent` (ProofEngine). The audit (2026-05-14) confirmed: (a) zero live emitters exist, (b) the entire presence-proof discharge pipeline (Strategies 2/3/5) is plumbed but no catalog entry or TypeChecker path generates `PresenceProofRequirement` obligations, (c) the gap is in obligation *generation*, not diagnostic codes. PRE0019 is staged for retirement; the real fix is adding presence obligation generation in the ProofEngine, which routes through PRE0116.

> **Note:** PRE0078 `NumericOverflow` was previously listed in this table. It has been removed — PRE0078 is **not a TypeChecker gap**. It is a ProofEngine obligation failure diagnostic owned by Strategy 7 (`IntervalContainment`) in the interval proof engine. See §3.7 D3 and `docs/Working/interval-proof-engine-design.md` §3.2–§3.3. Gate 1 allow-list removal for PRE0078 is coordinated with interval engine Slice 2, not with this plan's Slice 8.

**Test placement:**

| Code | Test file |
|------|-----------|
| `ComputedFieldWithDefault` | `TypeCheckerStructuralTests.cs` |
| `ConflictingAccessModes` | `TypeCheckerModifierTests.cs` |
| `RedundantAccessMode` | `TypeCheckerModifierTests.cs` |
| `ValueNotInChoiceSet` | `TypeCheckerStructuralTests.cs` (choice section) |
| `ListLiteralOutsideDefault` | `TypeCheckerExpressionTests.cs` |
| `DuplicateArgName` | `TypeCheckerStructuralTests.cs` |
| `InvalidModifierValue` | `TypeCheckerModifierTests.cs` |
| `EventArgOutOfScope` | `TypeCheckerExpressionTests.cs` |
| `MaxPlacesExceeded` | `TypeCheckerCurrencyUnitTests.cs` |
| `CollectionElementTypeMismatch` | `TypeCheckerCollectionSafetyTests.cs` |

**Test naming convention** (consistent with existing test files):
- `{Condition}_{InputDescription}_{ExpectedOutcome}`
- Examples: `ComputedField_WithDefaultExpression_EmitsComputedFieldWithDefault`, `ComputedField_WithoutDefault_NoDiagnostic`

**Remove from Gate 1 allow-list:** Each code as it is wired

**Regression anchors:**
- All existing TypeChecker test files — existing tests in modifier/structural/expression files must pass unchanged

**Files:** `src/Precept/Pipeline/TypeChecker.Validation.cs` (modify), `src/Precept/Pipeline/TypeChecker.Expressions.cs` (modify), multiple test files (modify)

- [x] Wire `ComputedFieldWithDefault` (PRE0039) + tests (positive + negative)
- [x] Wire `ConflictingAccessModes` (PRE0042) + tests
- [x] Wire `RedundantAccessMode` (PRE0043) + tests
- [x] Wire `ValueNotInChoiceSet` (PRE0085) + tests
- [x] Wire `ListLiteralOutsideDefault` (PRE0044) + tests
- [x] Wire `DuplicateArgName` (PRE0027) + tests
- [x] Wire `InvalidModifierValue` (PRE0035) + tests
- [x] Wire `EventArgOutOfScope` (PRE0050) + tests
- [x] Wire `MaxPlacesExceeded` (PRE0067) + tests
- [x] Wire `CollectionElementTypeMismatch` (PRE0105) + tests
- [x] Analyze PRE0022, PRE0048, PRE0051 before wiring (may be duplicates under different codes)
- [ ] ~~PRE0019~~ — retired; architecturally subsumed by PRE0116 (see Q5 addendum)
- [ ] ~~PRE0078~~ — removed from Slice 8 scope; owned by interval proof engine Strategy 7
- [x] Remove wired codes from Gate 1 allow-list as each is completed

---

### Slice 9A — Catalog-Mediated Emission: Modifier Constraint Violations

> **Numbering note:** Slices 9A–9C use the "9" prefix to indicate they are catalog-mediation expansion work (§ 9 architectural evolution). The letter suffix distinguishes them from the gap-closure slices (0–8) without disrupting existing numbering. They are active implementation slices in the current execution plan with the same quality bar and enforcement posture as Slices 0–8.

**Objective:** Migrate modifier constraint violations from per-identity switches to a single generic validation loop driven by `ModifierMeta.ConstraintDiagnosticCode`. If `ValidateModifiers` has ≥3 branches with the "check constraint → emit code" shape, add the metadata property and replace those branches with catalog dispatch.

**Governing policy:** Direct emission remains the default. This slice applies catalog mediation only because modifier constraints satisfy all three criteria: (1) stable 1:1 mapping between modifier identity and its constraint diagnostic, (2) the emission logic is uniform (check applicability, emit), and (3) validation is a membership/property check on a resolved modifier, not a structural judgment.

**Target files:**
- `src/Precept/Language/Catalogs/Modifiers.cs` — add `ConstraintDiagnosticCode` property to `ModifierMeta`
- `src/Precept/Pipeline/TypeChecker.Validation.cs` — refactor `ValidateModifiers` to dispatch through catalog
- `test/Precept.Tests/TypeChecker/TypeCheckerModifierTests.cs` — regression + new positive/negative cases
- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` — no allow-list change expected (PRE0035, PRE0042 are already emitted via direct paths; this migrates emission mechanism, not net-new coverage)

**Prerequisite:** Audit pass. Before implementation, confirm that `ValidateModifiers` has ≥3 branches with identical shape. If <3, this slice is deferred as unnecessary — annotate tracker as "audit: not viable."

**Completion gate:**
- `ValidateModifiers` no longer contains per-modifier-identity switch branches for constraint diagnostics — all such emission reads from `ModifierMeta.ConstraintDiagnosticCode`.
- Gate 1 (`PRECEPT0027`) continues to detect PRE0035 and PRE0042 as emitted (the emission site moves from a literal to a catalog property dereference — the analyzer must handle indirect catalog paths, per Slice 0 design).
- No behavioral change: the same diagnostics fire on the same inputs as before.

**Test/regression anchors:**
- All existing tests in `TypeCheckerModifierTests.cs` must pass unchanged (behavioral equivalence).
- Add ≥1 new positive/negative pair per migrated diagnostic code to prove catalog dispatch works end-to-end.
- `TypeCheckerCITests.cs` — unrelated, must pass (proves no interference with existing catalog-mediated emission).

- [x] Audit `ValidateModifiers` branch count and shape — **audit: not viable (2 identity-specific branches, threshold is ≥3)**
- [ ] ~~Add `ConstraintDiagnosticCode` to `ModifierMeta`~~ — N/A (audit: not viable)
- [ ] ~~Populate metadata for each modifier whose constraint violation has a dedicated `DiagnosticCode`~~ — N/A (audit: not viable)
- [ ] ~~Refactor `ValidateModifiers` to generic loop reading catalog property~~ — N/A (audit: not viable)
- [ ] ~~Verify existing modifier tests pass unchanged (behavioral equivalence)~~ — N/A (audit: not viable)
- [ ] ~~Add new positive + negative tests for catalog-dispatch path~~ — N/A (audit: not viable)
- [ ] ~~Verify Gate 1 still recognizes the emission site (indirect catalog reference)~~ — N/A (audit: not viable)

---

### Slice 9B — Catalog-Mediated Emission: Typed-Constant Family Diagnostics

**Objective:** Promote the typed-constant family registry from in-method dispatch to a catalog surface with per-family `FormatErrorCode` and `SemanticErrorCode` properties. Replace the generic PRE0052/PRE0053 fall-through with catalog-declared domain-specific diagnostics (PRE0055–0058 for temporal; others as families stabilize).

**Governing policy:** Catalog mediation applies because typed-constant families satisfy all three criteria: (1) stable 1:1 mapping from family to its diagnostic pair, (2) the validation logic is uniform ("parse against format → format error; validate against domain rules → semantic error"), (3) the check is on already-resolved typed-constant content, not syntactic structure.

**Target files:**
- `src/Precept/Language/Catalogs/TypedConstants.cs` (new or extended) — typed-constant family metadata with `FormatErrorCode` + `SemanticErrorCode`
- `src/Precept/Pipeline/TypedConstantValidation.cs` — refactor `Validate()` to read family metadata for diagnostic selection
- `test/Precept.Tests/TypeChecker/TypeCheckerTypedConstantTests.cs` — positive/negative cases for domain-specific codes
- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` — remove PRE0055–0058 from Gate 1 allow-list as they become emitted

**Dependency:** Sequenced after Slice 5 (B1 temporal precision) if Slice 5 ships first with direct emission. If Slice 9B ships first, it subsumes Slice 5's coverage for temporal diagnostics. Either ordering is valid — the dependency is on typed-constant resolution being stable, not on Slice 5 specifically.

**Completion gate:**
- `TypedConstantValidation.Validate()` selects diagnostic codes from family metadata, not from per-family `if`/`switch` branches.
- Every typed-constant family with a declared diagnostic pair emits its domain-specific code instead of generic PRE0052/PRE0053.
- Gate 1 recognizes the new emission paths (indirect catalog property dereference).
- PRE0055–0058 (at minimum) are removed from Gate 1 allow-list.

**Test/regression anchors:**
- Existing typed-constant tests in `TypeCheckerTypedConstantTests.cs` must pass unchanged.
- Add ≥1 positive + negative pair per newly-emitted domain-specific diagnostic code.
- `TypeCheckerCITests.cs` — existing CI enforcement tests unaffected.

- [x] Confirm typed-constant family registry is promotable to catalog surface (audit current dispatch structure)
- [x] Design `TypedConstantFamilyMeta` record with `FormatErrorCode` + `SemanticErrorCode` (nullable for families without domain-specific codes)
- [x] Populate metadata for temporal families (date, time, instant, duration) — PRE0055–0058
- [x] Refactor `TypedConstantValidation.Validate()` to read catalog for diagnostic selection
- [x] Add positive + negative tests for domain-specific temporal codes
- [x] Remove PRE0055–0058 from Gate 1 allow-list
- [x] Verify Gate 1 recognizes indirect catalog emission

---

### Slice 9C — Catalog-Mediated Emission: Proof Obligation Consistency

**Objective:** Verify and complete catalog-mediated emission for proof obligations. `ProofRequirements.GetMeta(kind).DiagnosticCode` is partially used today. This slice audits all ProofEngine emission paths — including the new **Strategy 7 (IntervalContainment)** path added by the interval proof engine — and migrates any remaining hardcoded `DiagnosticCode.X` literals to consistent catalog dispatch.

**Governing policy:** Proof obligations satisfy all three criteria: (1) stable 1:1 mapping from proof obligation kind to its diagnostic, (2) the emission logic is uniform ("obligation not met → emit diagnostic"), (3) the check is on resolved proof results, not structural parsing. Where a ProofEngine branch applies per-obligation branching logic beyond simple "met/not-met" (e.g., conditional message formatting), direct emission remains correct for that specific branch.

**Interval proof engine integration (hard dependency):** Strategy 7 (`IntervalContainment`) emits `NumericOverflow` for unresolved `IntervalContainmentProofRequirement` obligations. This slice must verify that `ProofRequirements.GetMeta(ProofRequirementKind.IntervalContainment).DiagnosticCode == DiagnosticCode.NumericOverflow` and that Strategy 7's emission uses catalog dispatch rather than a hardcoded literal. If the interval engine ships with a hardcoded emission path, this slice migrates it. **Slice 9C should be sequenced after interval engine Slice 2 completes** — otherwise there is no Strategy 7 emission path to audit.

**Target files:**
- `src/Precept/Pipeline/ProofEngine.cs` — migrate hardcoded emission to `ProofRequirements.GetMeta(kind).DiagnosticCode`
- `src/Precept/Language/Catalogs/ProofRequirements.cs` — verify all obligation kinds have `DiagnosticCode` populated
- `test/Precept.Tests/ProofEngine/ProofEngineTests.cs` — regression coverage for migrated paths
- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs` — no allow-list change expected (proof codes are already emitted; this is mechanism migration)

**Prerequisite:** Audit pass. Grep ProofEngine for literal `DiagnosticCode.X` emission sites. Count catalog-dispatched vs. hardcoded. If all emission is already catalog-mediated, this slice closes as "audit: already complete."

**Completion gate:**
- All ProofEngine emission paths use `ProofRequirements.GetMeta(kind).DiagnosticCode` unless the branch has legitimate per-obligation formatting logic that requires direct emission (documented exception per the do-not-apply criteria).
- Gate 1 continues to recognize all proof-obligation diagnostics as emitted.
- No behavioral change: same diagnostics fire on same inputs.

**Test/regression anchors:**
- All existing ProofEngine tests must pass unchanged (behavioral equivalence).
- Add ≥1 positive + negative pair per migrated emission path where existing test coverage is thin.
- `TypeCheckerCITests.cs` — unaffected, must pass.

- [ ] Audit ProofEngine: count `ProofRequirements.GetMeta(...).DiagnosticCode` vs. hardcoded `DiagnosticCode.X`
- [ ] **Include Strategy 7 (IntervalContainment) in the audit** — verify `ProofRequirements.GetMeta(ProofRequirementKind.IntervalContainment).DiagnosticCode == DiagnosticCode.NumericOverflow`; migrate if hardcoded
- [ ] Document any branches that legitimately require direct emission (per-obligation formatting)
- [ ] Migrate remaining hardcoded paths to catalog dispatch
- [ ] Verify existing ProofEngine tests pass unchanged
- [ ] Add positive + negative tests for migrated paths with thin coverage
- [ ] Verify Gate 1 recognition of migrated emission sites

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
Priority 0 is a blocking prerequisite: no gap-closure slice is complete until `PRECEPT0027` and `PRECEPT0028` are active in build + IDE.
- [ ] **Slice 0:** Roslyn analyzers (Gate 1 + Gate 2) — `PRECEPT0027` + `PRECEPT0028`

### Priority 1 — Integrity Violations (Silent Wrong Behavior)
- [x] **Slice 1:** Currency/unit arithmetic safety (PRE0070–0074) — `TypeChecker.Expressions.cs` qualifier comparison
- [x] **Slice 2:** Choice value validation (PRE0086–0089) — `TypeChecker.Expressions.cs` choice literal check
- [x] **Slice 3:** ~~InitialEventMissingAssignments (PRE0094)~~ — **already wired** in `TypeChecker.Validation.FieldState.cs`; no gap-closure work needed
- [ ] **Slice 4:** EventHandlerInStatefulPrecept (PRE0092) — trivial structural check in `ValidateStructural`

### Priority 2 — User Experience Gaps
- [x] **Slice 5:** ~~Temporal constant precision (PRE0055–0058)~~ — **subsumed by Slice 9B** catalog-mediated emission
- [x] **Slice 5A:** Ambiguous typed constant resolution (PRE0091) — TypeChecker typed-constant candidate arbitration, sequenced after typed-constant split stabilization
- [ ] **Slice 6:** Collection safety extensions (PRE0099–0101, PRE0104) — ProofEngine + TypeChecker
- [x] **Slice 7:** Parser guard gates (PRE0013–0015) — explicit rejection paths in `Parser.cs`

### Priority 3 — Scattered TypeChecker Gaps
- [ ] **Slice 8:** Scattered emission sites (PRE0027, PRE0035, PRE0039, PRE0042, PRE0043, PRE0044, PRE0050, PRE0067, PRE0085, PRE0105) — individual TypeChecker wires

### Priority 4 — Catalog-Mediated Emission Expansion (Mechanism Migration) ★ Active
These slices are in-scope for the current execution plan. They refactor emission dispatch from per-identity switches to catalog-mediated loops. They do not add net-new diagnostic coverage — their gate is behavioral equivalence plus analyzer recognition. Each requires a prerequisite audit pass; if the audit shows insufficient branch count, the slice closes as "not viable."

- [x] **Slice 9A:** ~~Modifier constraint violations~~ → audit: not viable — closed 2025-07-14. Only 2 identity-specific branches found (WritableOnEventArg, ComputedFieldNotWritable); both are context-dependent checks on the same modifier (Writable), not a pure 1:1 identity→code mapping. `ValidateModifierValues` branches all emit the same code (InvalidModifierValue). Threshold ≥3 not met.
- [x] **Slice 9B:** ~~Typed-constant family diagnostics~~ → `ContentValidation.FormatErrorCode`/`SemanticErrorCode` — catalog dispatch in `TypedConstantValidation` (subsumes Slice 5)
- [x] **Slice 9C:** Proof obligation consistency → `ProofRequirements.GetMeta(kind).DiagnosticCode` everywhere — audit and migrate remaining hardcoded ProofEngine emission **(sequence after interval engine Slice 2; must include Strategy 7)** — ✅ Completed 2025-07-14: Added `DiagnosticCode` property to `ProofRequirementMeta`; refactored `CreateFaultSiteLink` to use catalog dispatch for all non-Numeric kinds; `Numeric` retained as documented exception (1:many context-dependent mapping); `CreateDiagnostic` retained as legitimate per-obligation formatting; Strategy 7 verified: `IntervalContainment.DiagnosticCode == NumericOverflow`.

### Deferred
- [ ] **Period arithmetic safety** (PRE0060–0062) — qualifier-aware temporal type checking, parallel to B2; deferred pending B2 pattern establishment
- [ ] **Precision upgrades** (PRE0022, PRE0051) — deeper analysis needed; may already be covered by `TypeMismatch` or proof obligations under different codes
- [ ] ~~PRE0019~~ — **RETIRED (2026-05-14).** Architecturally subsumed by PRE0116 `FieldMayBeAbsent`. Zero live emitters; the entire presence-proof discharge pipeline is plumbed but no obligation source exists. The gap is in obligation generation (ProofEngine), not a diagnostic code. See Q5 addendum.
- [ ] **OutOfRange** (PRE0079) — **CONFIRMED dead-code, wire in TypeChecker (2026-05-14).** Zero live emitters today. Wire the constant-literal-assignment bounds check in the TypeChecker: when a `set` action assigns a numeric literal to a field with declared `min`/`max` modifiers, compare the literal value against bounds and emit PRE0079 if violated. The general expression-level bounds case is owned by the interval proof engine's Strategy 7 (`IntervalContainmentProofRequirement` → `NumericOverflow`). Pipeline ordering prevents double-firing (TypeChecker runs before ProofEngine). See `docs/Working/interval-proof-engine-design.md` §4.4 and Q1/Q10 decisions.
- [ ] **OutOfRange message update** (PRE0079) — update the diagnostic text to `Field '{name}' literal value {value} is outside declared bounds [min .. max].`
- [ ] **Parser expression precision** (PRE0010–0012) — chained comparisons, keywords-as-values, invalid call targets; lower priority than guard gates

### Cross-Plan Dependency: Interval Proof Engine → Diagnostic Enforcement

The interval proof engine (`docs/Working/interval-proof-engine-design.md`) has direct implications for this enforcement plan:

| Interval Engine Slice | Effect on Diagnostic Enforcement |
|---|---|
| **Slice 1** (Catalog + NumericInterval) | `ProofRequirementKind.IntervalContainment = 7` added to catalog. Slice 9C audit must include it. `ProofRequirements.All.Length` increases from 6 to 7. |
| **Slice 2** (Obligation Collection + Strategy 7) | `NumericOverflow` (PRE0078) becomes actively emitted via ProofEngine dispatch. **Remove PRE0078 from Gate 1 allow-list at this point.** Gate 1 analyzer must recognize the Strategy 7 emission path (ProofEngine dispatch pattern). Update the PRE0078 message to `Field '{name}' may produce values in [lo .. hi], outside declared bounds [min .. max].` |
| **Slice 3** (Guard Narrowing) | No direct enforcement impact. Reduces false negatives on interval proofs — fewer unresolved obligations means fewer `NumericOverflow` emissions on correct definitions. |
| **Slice 5** (Hover Extension) | No enforcement impact. Hover is display-only. |
| **Slice 6** (MCP Sync) | `precept_language` vocabulary output gains `IntervalContainment` member. No enforcement plan change needed — MCP derives from catalog automatically. |
| **Slices 7–12** (Catalog-driven obligation generator, qualified-type bounds, typed-constant extraction, qualifier compatibility, string/collection constraints, type-family regression) | Expands interval proof engine scope beyond `decimal`/`number` to all constrained type families. Introduces new diagnostics (`BoundsRequireQualifier`, `BoundsQualifierMismatch`) — these will need Gate 1 allow-list entries until their respective slices ship. May introduce `LengthContainment`/`CountContainment` obligation kinds for string/collection (Slice 11). See `interval-proof-engine-design.md` §§10–12. |

**Gate 1 analyzer note:** Once interval engine Slice 2 ships, the Gate 1 analyzer must detect ProofEngine Strategy 7's emission of `NumericOverflow` as a valid emission site. The emission pattern is: proof obligation `Disposition != Proved` → emit `Diagnostics.Create(DiagnosticCode.NumericOverflow, ...)`. This falls under existing "ProofEngine dispatch branch" recognition in the analyzer scanner (§5.1). No additional scanner logic is needed beyond what Slice 0 already specifies. The Q10 literal dedup gate is separate from this allow-list handling: it controls whether an interval obligation is collected for a literal assignment, not whether PRE0078 remains on the Gate 1 allow-list before Slice 2 ships.

**Slice 9C interaction:** The interval engine adds `IntervalContainment = 7` to `ProofRequirementKind`. Slice 9C's audit of ProofEngine emission paths must include Strategy 7. Specifically: verify that `ProofRequirements.GetMeta(ProofRequirementKind.IntervalContainment).DiagnosticCode == DiagnosticCode.NumericOverflow`. If Strategy 7 uses a hardcoded `DiagnosticCode.NumericOverflow` literal rather than reading from catalog metadata, Slice 9C should migrate it to the catalog-mediated pattern.

**PRE0079 `OutOfRange` boundary with interval engine:** The interval engine handles *expression-level* bounds checking: "does this arithmetic result interval fit within the target field's declared `min`/`max`?" This is the general case of bounds checking. PRE0079 `OutOfRange` retains only the narrow *constant-assignment* case: "does this literal value `42` exceed the field's declared `max 10`?" — a trivial TypeChecker check that doesn't need interval arithmetic because the value is known at compile time as a point interval `[42, 42]`. These two surfaces are complementary, not overlapping.

---

## 9. Architectural Evolution

Roslyn analyzers are the standing enforcement baseline for this plan. The items below are post-baseline refinements that complement the active implementation slices — they are not alternatives to analyzer gates.

### Catalog-declared `IsImplemented` flag

`DiagnosticMeta` already declares `DiagnosticStage` (Lex/Parse/Type/Graph/Proof). Adding an `IsImplemented` (or `EmissionSite`) property would let the catalog itself declare whether a diagnostic is live or aspirational. A catalog-backed analyzer check could then verify that every `IsImplemented = true` entry has an actual emission site, and every `IsImplemented = false` entry is tracked in an issue.

This is the architecturally correct evolution — it follows the metadata-driven principle: the catalog declares truth, enforcement derives from it. It still requires touching all 132 catalog entries and migration discipline across the catalog. `PRECEPT0027`/`PRECEPT0028` remain mandatory baseline enforcement; the `IsImplemented` path is a later simplification of analyzer inputs, not a replacement of analyzer gates.

### Analyzer hardening after initial rollout

After `PRECEPT0027`/`PRECEPT0028` land, hardening work should focus on:
- comment/string-literal stripping where needed to avoid false positives
- tighter semantic discrimination of emission vs. metadata references
- optional fixers or quick actions for allow-list hygiene

### Catalog-mediated emission: expansion scope

The `CIDiagnosticCode` pattern on `Operations.GetMeta()` and `Functions.GetMeta()` is the only shipped instance of catalog-mediated diagnostic emission. `ValidateCIEnforcement` is its sole consumer — a generic loop that walks resolved expressions and emits the diagnostic declared by catalog metadata without naming any specific `DiagnosticCode` member at the call site.

This pattern is powerful and correct for its use case, but **it is selective, not universal.** The governing policy is:

> **Direct emission remains the default pattern.** Catalog-mediated emission is appropriate only where:
> 1. There is a stable 1:1 mapping from a catalog member to a diagnostic code.
> 2. The emission logic is uniform across all members of that catalog (no per-member branching at the call site).
> 3. The validation is a membership/property check on already-resolved artifacts, not a context-sensitive structural judgment.
>
> Where any of these criteria is absent, direct emission with explicit `DiagnosticCode.X` at the call site is correct, readable, and audit-friendly.

The three expansion candidates are active implementation slices (Slice 9A–9C in § 7/§ 8), sequenced within the current execution plan. The remainder of this subsection retains the do-not-apply list as standing policy.

#### Do-not-apply list (direct emission must remain)

The following areas must retain direct `Diagnostics.Create(DiagnosticCode.X, ...)` emission. Catalog mediation would obscure intent, introduce false genericity, or violate the three criteria above:

| Area | Reason |
|------|--------|
| **Parser rejection paths** (PRE0013–0015, PRE0010–0012) | Context-sensitive structural judgments during parsing. No catalog member maps 1:1 to these — they fire on syntactic position, not catalog-member identity. |
| **Structural checks** (PRE0092 `EventHandlerInStatefulPrecept`, PRE0094 `InitialEventMissingAssignments`) | One-off structural invariants. No catalog axis exists to generalize across. |
| **TypeChecker expression-level precision** (PRE0051) | Condition-specific checks that depend on expression subtree shape, not catalog-member classification. Adding a catalog property would be pretend-genericity for a single consumer. |
| **Cross-entity qualifier comparison** (PRE0070–0074 currency/unit arithmetic) | The emission logic is uniform for B2, but the qualifier-matching rules are structurally diverse across arithmetic vs. denomination vs. compound-period cases — per-member branching is inherent to the domain. |
| **Gate analyzers themselves** (`PRECEPT0027`/`PRECEPT0028`) | Meta-enforcement. These analyze emission patterns; they are not themselves governed by catalog dispatch. |

---

## 10. Open Questions for Shane

1. **PRE0079 `OutOfRange` — scope narrowed by interval engine.** The general case of "expression result exceeds field bounds" is now owned by the interval proof engine (Strategy 7 → `NumericOverflow`). PRE0079's remaining scope is the narrow *constant-assignment* case: `set field to 42` where `42` exceeds the field's declared `max 10`. This is a trivial TypeChecker check (compare literal value against declared bounds). Options: (a) wire PRE0079 for constant assignments only at type-check time (recommended — the literal value is known, no interval arithmetic needed), (b) let Strategy 7 subsume this case too (it would, since `IntervalOf(literal 42) = [42, 42]` exceeds `max 10`, but that emits `NumericOverflow` not `OutOfRange` — two codes for the same user-visible problem), or (c) deprecate PRE0079 entirely and let `NumericOverflow` cover both cases. **Recommendation:** (a) for the constant-literal case as a precision diagnostic ("this literal is out of range" is more helpful than "arithmetic may overflow"), plus document the boundary: PRE0079 fires on literal assignments, PRE0078 fires on expression results. Direction needed.

   **Decision:** RESOLVED — option **(a)**. Wire PRE0079 for **constant-literal assignments only**.
   - **Scope:** PRE0079 fires in the **TypeChecker** when a literal assigned to a field violates that field's declared `min`/`max` bounds.
   - **Boundary:** PRE0078 remains the **ProofEngine / Strategy 7** diagnostic for non-literal expression intervals that may exceed declared bounds.
   - **Rationale:** These are distinct error categories with different pipeline stages, recovery actions, and AI dispatch keys: PRE0079 is a known-fact literal constraint violation; PRE0078 is a proof failure on a computed range.

2. **PRE0070–0074: Dynamic qualifier handling.** When a field has a dynamic qualifier like `money in '{CatalogCurrency}'`, the TypeChecker cannot statically compare currencies. Should the TypeChecker: (a) silently skip cross-currency checks for dynamic qualifiers and let the ProofEngine handle them via Strategy 5, or (b) emit the diagnostic at compile time with a message noting the dynamic qualifier and that the check is partial? The design assumes (a) — confirm before Slice 1 implementation.

   **Decision:** RESOLVED — option **(a)**. The TypeChecker silently skips cross-currency checks for dynamic qualifiers.
   - **Scope:** When a field's qualifier is dynamic (e.g., `money in '{CatalogCurrency}'`), no PRE0070–0074 diagnostic is emitted at type-check time.
   - **Enforcement:** The ProofEngine handles dynamic qualifier validation via Strategy 5 at proof time.
   - **Rationale:** Static currency comparison is not possible when the qualifier is a runtime value. Deferring to the ProofEngine is the correct architectural boundary.

3. **PRE0094 priority sequencing.** The field-state-guarantees-v3 work (Slices 10–11 in that doc) depends on PRE0094. Should PRE0094 be fast-tracked ahead of the B2 currency/unit cluster, even though B2 is a higher integrity risk by the philosophy test? Or is the B2 dependency more urgent to address first?

   **Decision:** RESOLVED — sequence **B2 (PRE0070–0074)** first.
   - **Priority:** B2 remains **Slice 1** because currency/unit arithmetic is the higher-integrity-severity gap.
   - **Premise correction:** PRE0094 (`InitialEventMissingAssignments`) is already implemented via `ValidateConstructionGuarantees` in `TypeChecker.Validation.FieldState.cs`, so the dependency argument for fast-tracking it ahead of B2 is moot.
   - **Impact:** `field-state-guarantees-v3` Slices 10–11 are already unblocked.
   - **Planning consequence:** PRE0094 / D94 should be removed from the ordering rationale; B2 is sequenced first on integrity severity alone.

4. **PRE0091 `AmbiguousTypedConstant` — first-tranche candidate breadth.** The scoped plan assumes the first emitting implementation is limited to candidate sets already available inside the TypeChecker typed-constant path (expected first: temporal quantity ambiguity such as `duration` vs. `period`). Should the first tranche stay that narrow, or should it immediately enumerate every content-validated typed-constant family? Recommendation is the narrow tranche — it is enough to activate PRE0091 without entangling broader inference work.

   **Decision:** RESOLVED — **narrow first tranche** (temporal quantity ambiguity only).
   - **Scope:** The first emitting implementation of PRE0091 covers only temporal quantity ambiguity (e.g., `duration` vs. `period`). No other typed-constant families are included in Slice 5A.
   - **Rationale:** Code review confirmed that `ResolveTypedConstant` already receives `expectedType` (the declared field type) as a parameter. When `expectedType` is known — the normal case for any field assignment — the resolver validates against exactly one type with no candidate enumeration. PRE0091 is structurally unreachable on a well-typed precept under the current architecture; it only becomes reachable when `expectedType is null`, which occurs in error-recovery paths (e.g., the field type itself failed to resolve). Building multi-candidate enumeration broadly would be pure speculative infrastructure for paths that currently only exist in error states. The narrow tranche is enough to activate PRE0091 without entangling broader inference work or restructuring the resolver unnecessarily.
   - **Future path:** If Precept adds untyped expressions or type inference — contexts where field type context is genuinely unavailable at resolution time — the candidate breadth question reopens. Until then, narrow is correct.
   - **Implementation consequence:** Slice 5A's expected-type fast path note ("Keep the current expected-type fast path. PRE0091 should only appear when the resolver is genuinely operating over a candidate set.") is validated by architecture, not just preference.

5.**Scattered Priority 3 precision upgrades.** PRE0019 `NullInNonNullableContext`, PRE0022 `FunctionArgumentInvalid`, PRE0051 `NonTextTypeInStringInterpolation`, PRE0078 `NumericOverflow` may already be covered by `TypeMismatch` or other codes. Should we invest in precision upgrades for these (more specific diagnostic on the same condition), or accept the current generic handling and let the allow-list entries remain indefinitely?

   **Decision:** RESOLVED — invest in precision upgrades for all four codes, with PRE0048 added as a fifth case.
   - **Scope:**
     - **PRE0019 `NullInNonNullableContext`** — ~~`TypeMismatch` fires today; upgrade to the specific code.~~ **CORRECTED (2026-05-14).** Emission-site audit found no live emitters — neither `TypeMismatch` nor any other code fires for null-in-non-nullable today. **RESOLVED (2026-05-14 audit).** PRE0019 is **architecturally subsumed by PRE0116** and staged for retirement. The audit found: (a) `new PresenceProofRequirement(...)` is never constructed in production code — no catalog entry declares one; (b) the entire discharge pipeline (Strategies 2/3/5, `DeclaredPresenceMeta`, `ProofSatisfaction.Presence`) is fully plumbed but receives zero obligations; (c) the gap is obligation *generation* (not diagnostic codes) — when an optional field appears in a value position, no `PresenceProofRequirement` is injected; (d) PRE0019 is `DiagnosticStage.Type` which is the wrong pipeline stage for a guard-aware check — the ProofEngine (PRE0116's stage) is the correct enforcement point. The fix: add presence obligation generation in the ProofEngine's expression walker (inject `PresenceProofRequirement` when `TypedFieldRef` references an optional field), routing through existing PRE0116. PRE0019 remains on Gate 1 allow-list as retirement-pending. `FaultCode.UnexpectedNull`'s `[StaticallyPreventable]` link should be redirected to PRE0116 when obligation generation ships.
     - **PRE0022 `FunctionArgumentInvalid`** — `TypeMismatch` fires today; upgrade to the specific code.
     - **PRE0048 `ScalarOperationOnCollection`** — `TypeMismatch` fires today when `set` is used on a collection field (for example, `set Tags = "hello"` where `Tags` is `set of string`). The `TypeMismatch` message ("Expected a Set value here, but got 'String'") implies the wrong thing — the action keyword is wrong, not the value type. PRE0048's message ("'set' cannot be used with collection field 'Tags'") plus fix hint ("use a collection action — add, remove, enqueue, push") is meaningfully more useful. Frank's precision scan identified this as matching the same pattern as the other three.
     - **PRE0051 `NonTextTypeInStringInterpolation`** — `TypeMismatch` fires today; upgrade to the specific code.
     - **PRE0078 `NumericOverflow`** — remove from this list. PRE0078 is **not** a precision gap; it is a ProofEngine obligation diagnostic owned by Strategy 7. It was never a TypeChecker precision-upgrade candidate. The original Q5 text included it speculatively; the precision scan confirmed it does not belong here.
   - **Rationale:** Each of these codes exists in the catalog with a specific trigger condition, message, and fix hint that is more actionable than `TypeMismatch`. Keeping `TypeMismatch` as the emission site leaves those catalog investments unreachable and degrades AI diagnostic legibility. The cost of each upgrade is low — a targeted code swap plus a field-type guard at one identifiable site.
   - **Implementation:** These are Priority 3 precision upgrades. They are deferred from Slice 8 pending deeper per-code analysis but are now confirmed in scope. PRE0048 is added to the Slice 8 deferred table alongside PRE0019, PRE0022, and PRE0051.
   - **Diagnostic Message Updates (accepted):**
      - **PRE0019 `NullInNonNullableContext`:**
        - **Message:** `"'{0}' requires a value and cannot be empty here"` — `"'{0}' may be unset — this position requires a value"` (approved by Frank's audit; future-safe rename target: `ValueMayBeAbsent`)
        - **FixHint:** `"Add the 'optional' modifier to the field declaration to allow null values"` — `"Add 'when {0} is set' before this expression, or add a 'default' value to the field declaration"`
        - **RecoverySteps item 3:** `"—so the field is never null"` — `"—so the field is always set"`
     - **PRE0022 `FunctionArgumentInvalid`:**
       - **Message:** Accepted as-is; the current form reads naturally in English.
       - **RecoverySteps:** Replace tautological steps ("Check the constraint…" / "Ensure the value satisfies the constraint") with `"Review the constraint shown in the error message and adjust the argument value"` / `"See the built-in function reference for allowed values at each argument position"`.
     - **PRE0051 `NonTextTypeInStringInterpolation`:**
       - **Message:** `"A {0} value cannot appear inside a text interpolation"` → `"'{1}' is a {0} and cannot be embedded in a text interpolation"` (adding field name as second parameter `{1}`).
       - **Fallback message if `{1}` is unavailable at the emit site:** `"A {0} cannot be embedded in a text interpolation — remove or replace this expression"`.
       - **FixHint:** `"Convert this value to text before interpolating, or remove it from the text literal"` → `"Remove this expression from the interpolation, or use a text-typed field instead"`.
       - **RecoverySteps item 2:** `"Or convert it to a text representation before interpolating"` → `"Or reference a text-typed field or a field with a text accessor instead"`.
     - **PRE0048 `ScalarOperationOnCollection`:** No message review yet — Elaine reviewed the three originally listed codes. Message review for PRE0048 is deferred until Slice 8 analysis.
   - **Rationale for message updates:** Use field-first ordering and Precept vocabulary consistency (`is set` / `is unset` rather than "null" / "empty"); PRE0019's `FixHint` was actively misleading because the field is already optional; PRE0051's `FixHint` implied a coercion path that does not exist for collections.

6. **Gate 1 allow-list granularity.** The initial allow-list has all 49 unemitted codes. Should each entry cite a specific tracking issue (e.g., `// tracked in #245`), or is a cluster comment (`// Root Cause B2 — not yet implemented`) sufficient? The issue-level citation is richer but requires creating 49 tracking issues. Direction on granularity before Slice 0 implementation.

   **Decision:** RESOLVED — cluster comment annotation. Each allow-list entry carries a comment identifying its root cause cluster (e.g., `// Root Cause B2 — not yet implemented`). No per-issue citations. Rationale: implementation progress is already tracked at the slice/cluster level; individual codes within a cluster will not slip independently, so issue-level traceability adds maintenance overhead with no traceability benefit.

7. **Analyzer scan scope: Pipeline-only vs. all source.** The recommended initial scope covers `src/Precept/Pipeline/*.cs` + `Operations.cs` + `Functions.cs` (+ ProofEngine emission paths). If future emission patterns emerge outside these files (e.g., a new `RuntimeValidator` in `src/Precept/Runtime/`), the scan set needs manual updating. Should the analyzer scan all of `src/Precept/**/*.cs` minus known non-emission files instead, making it automatically inclusive? Broader scope means fewer false negatives but potentially more false positives from non-emission references. Direction before Slice 0 is finalized.

   **Decision:** RESOLVED — pipeline-only explicit scan set.
   - **Scan set:** `src/Precept/Pipeline/*.cs` + `Operations.cs` + `Functions.cs` + ProofEngine emission paths. This is the complete set of known emission sites today.
   - **Rationale:** False positives from a broad all-source scan outweigh the benefit of automatic inclusion. The known emission patterns are stable and well-defined, and the explicit list keeps the analyzer contract clear and auditable.
   - **Maintenance rule:** If a new emission path opens outside Pipeline (for example, a future `RuntimeValidator`), that is a conscious architectural decision and the scan set is updated manually at that time.

8. **Doc-comment false positives.** The emission-site scan picks up `DiagnosticCode.X` in XML doc comments (`/// <see cref="DiagnosticCode.X"/>`). Currently rare — only one instance exists and `X` is not a real member name. If it becomes a pattern, the scan should strip `// ...` and `/** */` comments before matching. For now, does Shane want comment stripping from the start, or accept the current minimal risk?

   **Decision:** RESOLVED — strip comments from the start.
   - **Implementation rule:** The Gate 2 emission-site analyzer strips `// ...`, `/** */`, and `/// ...` comment content before matching `DiagnosticCode.*` references.
   - **Rationale:** This is the defensive default. Comment stripping is straightforward to implement, and it eliminates the false-positive class up front instead of accepting the risk now and patching later.

9. **Interval engine Slice 2 → Gate 1 allow-list coordination.** When interval engine Slice 2 ships and `NumericOverflow` becomes actively emitted, who removes PRE0078 from the Gate 1 allow-list — the interval engine PR itself, or a follow-up enforcement-plan PR? **Recommendation:** The interval engine PR removes it. The Gate 1 stale-entry check will flag it automatically once the emission site exists, so the interval engine PR will fail the analyzer gate unless it removes the entry. This is the correct forcing function — no separate coordination needed. Confirm this matches Shane's expectation for cross-plan allow-list management.

   **Decision:** RESOLVED — interval engine PR owns the allow-list cleanup.
   - **Rationale:** Gate 1's stale-entry analyzer is the forcing function. Once PRE0078 has a real emission site, the interval engine PR will fail if the allow-list entry is still present, so no separate coordination PR is needed.
   - **Verification checkpoint:** Slice 0 / the allow-list implementation slice should call this out explicitly as a cross-plan dependency: when interval engine Slice 2 ships, review must confirm that the same PR removed PRE0078 from the Gate 1 allow-list.

10. **PRE0078 vs. PRE0079 deduplication risk.** With the interval engine emitting `NumericOverflow` (PRE0078) for expression-level bounds violations, and PRE0079 `OutOfRange` potentially covering constant-literal bounds violations (see Question 1), there is a narrow overlap: `set field to 42` where `42` exceeds `max 10` could theoretically trigger both PRE0079 (literal out of range) and PRE0078 (interval `[42, 42]` exceeds `[min, 10]`). Should the TypeChecker's PRE0079 check suppress the ProofEngine's PRE0078 on the same span (avoid double-diagnostic), or should PRE0079 be deprecated entirely in favor of letting Strategy 7 handle all bounds cases? Direction needed before PRE0079 is wired.

   **Decision:** RESOLVED — subsumed by Q1.
   - **Question 1 boundary:** Question 1 already established the ownership line: PRE0079 `OutOfRange` fires on constant-literal assignments in the TypeChecker, while PRE0078 `NumericOverflow` fires on expression results via the ProofEngine / Strategy 7.
   - **Deduplication effect:** That boundary prevents double-firing. The TypeChecker runs before the ProofEngine, so a literal assignment that exceeds field bounds triggers PRE0079 first, and Strategy 7 does not re-emit PRE0078 for the same span.
   - **Conclusion:** No separate Q10 resolution is needed beyond the Q1 boundary.
