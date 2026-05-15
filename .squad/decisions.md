# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

---

### 2026-05-15T01:05:58Z: Quantity normalization review findings accepted, ordered, and narrowed to implementation-safe scope

**By:** Scribe

**Status:** Merged from George's technical review and Frank's disposition pass.

**Merged sources:** `george-normalization-design-review.md`; `frank-normalization-george-review.md`.

- George's design review approved the normalization plan with conditions: Slice 16's blast radius extends through obligation generation, proof diagnostics, MCP projection, and proof tests; duplicate Slice 22 numbering and the missing display contract were the main planning hazards.
- Frank accepted every technical finding, corrected the concrete code targets (`TypedArg`, `PopulateEvents`), moved runtime-ingress work out of the active implementation track, and locked the implementation ordering around the review's safety constraints.
- B16's dynamic-unit rule is canonical: `TryGetStaticNumericValue` must refuse raw `StaticMagnitude` facts when no static scaling factor exists, and interval-containment proof success never suppresses independent PRE0116 presence obligations.
- Slice 26's track membership and B18's full display contract were left to explicit follow-up decisions; both are now resolved by separate canonical entries below.
---

### 2026-05-15T01:05:58Z: Q1 locked the quantity-overflow display contract and fully resolved B18

**By:** Scribe

**Status:** Merged from Frank's Q1 lock note.

**Merged source:** `frank-q1-display-contract-locked.md`.

- `NumericOverflow` and related interval-containment displays must show the computed interval de-normalized into the field's declared unit, e.g. `[−∞ .. 5 kg] (computed: [6 [lb_av] .. 6 [lb_av]])`.
- `IntervalContainmentProofRequirement` must carry the field's declared qualifier so the diagnostic renderer can de-normalize before presenting overflow evidence.
- This closes the last ambiguity behind B18: the design now specifies the display contract completely enough for Slice 18 implementation, diagnostic rendering, and downstream hover/MCP alignment.
- Frank wrote the lock directly into `docs/Working/quantity-normalization-design.md`; that document update is staged with this Scribe pass.
---

### 2026-05-15T01:05:58Z: Slice 26 stays inside the quantity-normalization track

**By:** Scribe

**Status:** Merged from Frank's reinclusion decision.

**Merged source:** `frank-slice26-reinclusion.md`.

- Shane's direction keeps event-arg default resolution in-track instead of deferring it to a separate issue, closing the asymmetry where field defaults would normalize and prove correctly but arg defaults would stay ignored.
- The active implementation shape is tight: add `ResolveEventArgExpressions`, populate `TypedArg.DefaultExpression`, route arg defaults through the same assignability and interval-containment machinery, and reuse Slice 15b's normalized arg bounds.
- The one notable adapter is `ValidateMaxPlaces`, which needs a minor `TypedArg`-compatible path; this is mechanical scope, not a design blocker.
- General/dynamic default expressions remain conservative: typed-constant defaults are the primary supported proof shape, while interpolated defaults can degrade to unbounded when static scaling facts are unavailable.

---

### 2026-05-15T00:08:25Z: Typed-constant interpolation holes require PRE0116 proof-stage presence checks

**By:** Scribe

**Status:** Merged from Frank's null-guard decision.

**Merged source:** `frank-test-precept-null-guard.md`.

- Frank locked the `samples/Test.precept` null-guard gap to **PRE0116 `UnprovedPresenceRequirement`** in the ProofEngine presence-proof path, not a new TypeChecker special case and not PRE0019.
- The missing traversal case is `TypedInterpolatedTypedConstant`: typed-constant interpolation holes must behave like other value-position optional reads and generate presence obligations unless guards prove presence.
- Recommended repository action: revert `samples/Test.precept` to the clean literal form and keep any interpolation coverage in a dedicated sample file instead of overloading the fixture.
- Implementation scope is adjacent to, but separate from, quantity-normalization Slices 14–21.

---

### 2026-05-15T00:08:25Z: Slice P1 landed typed-constant hole presence-proof traversal in `ProofEngine.WalkExpression`

**By:** Scribe

**Status:** Merged from George's Slice P1 implementation.

**Merged source:** `george-p1-presence-proof.md`.

- George added the `TypedInterpolatedTypedConstant` traversal case in `src/Precept/Pipeline/ProofEngine.cs`, iterating slot expressions so optional field reads inside typed-constant holes now participate in presence-proof generation.
- `WalkExpression` now carries `includeOptionalArgRefs` for typed-constant holes, and guard extraction understands `TypedArgRef` presence checks so guarded optional arg reads discharge correctly without widening unrelated surfaces.
- Added four focused proof-engine presence tests covering unguarded integer/quantity holes, guarded optional holes, and non-optional holes.
- Validation summary from George: focused `ProofEnginePresenceTests` passed, `samples/Test.precept` now reports `UnprovedPresenceRequirement` on line 14, and `dotnet build src/Precept/Precept.csproj` passed cleanly.
- Implementation landed in commit `ae19510f`.

---

### 2026-05-14T22:00:00Z: Quantity normalization §5.5.6 conditions fully resolved — implementation gate cleared

**By:** Scribe

**Status:** Merged from Frank's design-resolution pass.

**Merged source:** `frank-normalization-conditions-resolved.md`.

- Frank resolved all six §5.5.6 conditions in a single edit pass on `docs/Working/quantity-normalization-design.md`. Implementation of Slices 14–21 may proceed after Shane's sign-off on the design document.
- **Condition 1 (bounds-storage contradiction):** SUPERSEDED markers placed on §3.6, §3.7, and §7 Q2. §0's "store both original and normalized on `TypedField`" is the single authoritative design.
- **Condition 2 (`IntervalOf` post-step scope):** Replaced "universal post-step" language with expression-type-dispatched `TryGetStaticScalingFactor` pseudocode. Scales `TypedTypedConstant` with static unit and `TypedInterpolatedTypedConstant` with Magnitude slot + static unit only. Excludes `TypedFieldRef`, `TypedArgRef`, and WholeValue-slot interpolated constants.
- **Condition 3 (`GetFieldBounds` raw reads):** Slice 16 now specifies `NormalizedDeclaredMin ?? DeclaredMin` fallback.
- **Condition 4 (`TryGetStaticNumericValue` raw facts):** Slice 16 now normalizes `StaticMagnitude` via `TryGetStaticScalingFactor` before returning trusted facts.
- **Condition 5 (`TypedEventArg` normalization):** Decided Option (a) — add `NormalizedDeclaredMin/Max` to `TypedEventArg`, architecturally parallel to `TypedField`. Slice 15b added.
- **Condition 6 (`NumericInterval.Scale` parameter type):** `Scale(decimal factor)` confirmed. Factor conversion in `TryGetStaticScalingFactor`, not inside interval algebra.
- Added §5.6 Extended Slice Details (Slices 22–26) from George's gap audit with full objective/files/approach/tests/dependencies for each.
- Added §0.6 Design Resolution Summary; replaced George's §0.6 header. Key architectural invariant: `TryGetStaticScalingFactor(TypedExpression) → decimal?` is the single dispatch point for all expression-type → scaling-factor decisions.

---

### 2026-05-14T22:00:00Z: PRE0027 diagnosis — no DuplicateArgName errors exist anywhere in the repository

**By:** Scribe

**Status:** Merged from Frank's PRE0027 investigation.

**Merged source:** `frank-pre0027-diagnosis.md`.

- Frank scanned all 30 sample files for PRE0027 (`DuplicateArgName`) errors. **Result: none exist.** The suspicion was unfounded.
- The only active error in `samples/Test.precept` is **PRE0078** (`NumericOverflow`), which is pre-existing. The original literal `'6 [lb_av]'` already violated the `max '5 kg'` bound before George's edit.
- George's modification (`'6 [lb_av]'` → `'{test2} [lb_av]'`) changed the proof shape from concrete-interval to unbounded-interval but did not introduce a new error category; PRE0078 fires in both versions.
- `test/Precept.Analyzers.Tests/AnalyzerTestHelper.cs` contains George's new `AnalyzeWithFilePathsAsync<TAnalyzer>()` helper — legitimate C# test infrastructure, not a DSL artifact.
- **Recommended action:** Revert `samples/Test.precept` to the original (`git checkout samples/Test.precept`). If interpolated-quantity test coverage is needed for normalization work, create a new sample file with satisfiable bounds rather than mutating the existing Test.precept fixture.

---

### 2026-05-14T22:00:00Z: Slice 27 (Doc Sync) added — 6 surfaces need updates after quantity normalization Slices 14–21

**By:** Scribe

**Status:** Merged from Frank's doc-sync audit.

**Merged source:** `frank-doc-sync-slice.md`.

- Frank audited all canonical documentation surfaces for staleness relative to quantity normalization (Slices 14–21) and added Slice 27 (Doc Sync) to `docs/Working/quantity-normalization-design.md`.
- **Surfaces requiring updates:**
  - `docs/language/precept-language-spec.md` — §0.6 Proof Engine Contract: add unit-aware normalization bullet; §5 Proof Engine: add cross-unit interval containment paragraph.
  - `docs/compiler/proof-engine.md` — `IntervalContainmentProofRequirement` record: annotate DeclaredMin/Max as normalized for quantity/price; interval source table: note normalization; Strategy 6: note StaticMagnitude normalization.
  - `docs/Working/interval-proof-engine-design.md` — tracker cross-reference to normalization design; §2.2/§2.3/§3.2: annotate that bounds are now UCUM-normalized for quantity/price types.
  - `docs/runtime/runtime-api.md` — add three-layer enforcement model section (compile-time / ingress / defense-in-depth); this named architectural concept from §0.5 has no canonical home in published docs.
  - `tools/Precept.Mcp/Dtos/CompileToolDtos.cs` — add `NormalizedDeclaredMin/Max` to `CompileProofObligationDto`.
  - `tools/Precept.Mcp/Tools/CompileTool.cs` — project normalized bounds in DTO.
- **Confirmed clean (no changes needed):** `docs/language/catalog-system.md`, `README.md`, `docs/philosophy.md`, `samples/`, `docs/mcp/` (directory doesn't exist).
- Most significant gap: the three-layer enforcement model (compile-time / ingress / defense-in-depth) is a named architectural concept from §0.5 with no canonical published home. Slice 27 assigns it to `docs/runtime/runtime-api.md`.

---

# Decision Record: Exhaustive Architectural Review— Interpolated Forms & Normalization Design

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14T17:08:27-04:00
**Status:** Proposed
**Scope:** Architectural review findings, conditions, and decisions for the quantity normalization design (§0–§0.5, Slices 14–22) with exhaustive interpolated-form coverage.

---

## D1: §0 supersedes §3.6, §3.7, and §7 Q2

**Decision:** §0's "store both original and normalized on `TypedField`" is the approved design for bounds storage. §3.6 (overwrite DeclaredMax), §3.7 (normalize inside TryGetTypedConstantMagnitude), and §7 Q2 Option B (normalize at proof time) are superseded and must be marked as such in the design doc.

**Rationale:** Three competing descriptions of the same feature in one design doc is an implementation hazard. §0 was written as the architectural reassessment — it takes precedence by design intent.

---

## D2: `IntervalOf` post-step must be expression-type-scoped, not universal

**Decision:** The `IntervalOf` post-step scales intervals by a UCUM factor only for:
- `TypedTypedConstant` with a static unit
- `TypedInterpolatedTypedConstant` with `Magnitude` slot kind + static unit

It does NOT scale:
- `TypedFieldRef` or `TypedArgRef` (their intervals come from declared bounds which are already in the field/arg's unit system)
- `TypedInterpolatedTypedConstant` with `WholeValue` slot kind (the source value already carries quantity semantics)

**Rationale:** Blind universal scaling causes double normalization when the source is already a quantity-typed entity. The magnitude-to-quantity conversion (magnitude × unit) is only meaningful when the expression is constructing a NEW quantity from a raw numeric magnitude and a static unit — not when it's referencing an existing quantity value.

---

## D3: `GetFieldBounds` must read normalized bounds

**Decision:** `GetFieldBounds` (`ProofEngine.Intervals.cs:131`) must read `TypedField.NormalizedDeclaredMin/Max` when populated, falling back to `DeclaredMin/Max` when null (for non-quantity types).

**Rationale:** After Slice 15, `DeclaredMin/Max` are raw authored magnitudes. Using them for field-ref interval computation produces intervals in the author's declared unit, which would then be incorrectly scaled by the `IntervalOf` post-step if the expression is inside a typed constant. Normalized bounds ensure field-ref intervals are in base units, making all interval comparisons unit-homogeneous.

---

## D4: `TryGetStaticNumericValue` must normalize `StaticMagnitude`

**Decision:** `ProofEngine.Composition.cs:221-223` must normalize `StaticMagnitude` by the static UCUM unit factor when extracting a concrete numeric value from `TypedInterpolatedTypedConstant`.

**Rationale:** This method feeds trusted-rule facts into Strategy 6. If bounds are normalized but `StaticMagnitude` is raw, fact-comparison produces wrong results. The normalization is a single `ApplyFactor` call using `TryGetStaticUnit`.

---

## D5: `NumericInterval.Scale` takes `decimal`, not `UcumExactFactor`

**Decision:** The `Scale` method on `NumericInterval` takes a `decimal` factor parameter.

**Rationale:** Interval bounds are `decimal`. The `UcumExactFactor → decimal` conversion is done once by `TypedConstantNormalizer.ApplyFactor`. Passing `UcumExactFactor` through to `NumericInterval` couples the interval algebra to UCUM types unnecessarily. The conversion is lossless in practice (decimal has 28-29 significant digits; UCUM factors for real-world units are well within range).

---

## D6: Event arg bound normalization needs decision

**Decision:** OPEN — requires Shane's input.

If event args can carry quantity bounds (e.g., `newWeight: quantity of 'mass' max '10 [lb_av]'`), those bounds need the same normalization treatment as `TypedField` bounds. Options:
- (A) Add `NormalizedDeclaredMin/Max` to `TypedEventArg` — parallel to `TypedField`
- (B) Have `ExtractArgInterval` normalize on-the-fly from arg qualifier metadata

Recommendation: Option (A) for consistency with the `TypedField` approach.

---

## D7: Overall design verdict — APPROVED WITH CONDITIONS

**Decision:** The quantity normalization design is architecturally sound and may proceed to implementation once the 6 conditions in §5.5.6 are resolved. No condition is design-blocking — all are specification gaps that would otherwise require ad-hoc resolution during implementation.

---

# George — Interpolated Typed-Constant Gap Audit

**Date:** 2026-05-14T17:06:00-04:00
**Scope:** Exhaustive follow-up on `TypedInterpolatedTypedConstant` coverage relative to quantity-normalization Slices 19–21.

## Core finding

Slices 19–21 are correct for the exact quantity bug (`'{test2} [lb_av]'`) and quantity whole-value interpolation, but they are **not** exhaustive for interpolated typed constants as a whole.

`TypedInterpolatedTypedConstant` currently stores only:
- `Slots`
- `ResultType`
- `Span`
- `StaticMagnitude`

It does **not** retain static qualifier-bearing text (`USD`, `kg`, `USD/kg`, `USD/EUR`) and it does **not** expose qualifier identity for `WholeValue` holes. That omission creates multiple gaps beyond the interval-only quantity track.

## High-priority confirmed behaviors

1. **False-positive PRE0078 on bounded interpolated `set` actions**
   - quantity: `'{src} [lb_av]'`, `'{q}'`
   - money: `'{src} USD'`, `'{m}'`
   - price: `'{src} USD/kg'`, `'{p}'`
   - cause: `IntervalOfNarrowed` has no `TypedInterpolatedTypedConstant` path, so these fall to `Unbounded`.

2. **False-positive PRE0114 in rules/ensures**
   - quantity: `q > '{n} kg'`, `q > '{other}'`
   - money: `m > '{n} USD'`, `m > '{other}'`
   - price: `p > '{n} USD/kg'`, `p > '{other}'`
   - cause: `ResolveQualifierFromInterpolatedConstant` only inspects slots; static text and whole-value source qualifiers resolve as `unresolved`.

3. **Silent acceptance of definite qualifier mismatches in `set` / `default`**
   - quantity in `'m'` accepts `'{n} kg'`
   - money in `'EUR'` accepts `'{n} USD'`
   - price in `'EUR'` of `'length'` accepts `'{n} USD/kg'`
   - cause: `ValidateAssignmentQualifiers` has no interpolated-static-text / whole-value path.

4. **Interpolated field defaults are not proof-checked**
   - example: `field q as quantity in 'kg' max '5 kg' default '{n} kg'` with `n default 10`
   - cause: no interval-containment obligation for defaults, and initial-state constant folding treats interpolated typed constants as unknown.

5. **Interpolated event-arg defaults are entirely unwired**
   - `TypedArg.DefaultExpression` is never resolved today.
   - even clearly invalid defaults like `default '{"oops"} kg'` compile clean.

## Medium-priority conservative cases

These should remain explicit `Unbounded` / not-proved paths unless static qualifier identity becomes available:
- `'{n} {u}'`
- `'{n} {a}/{b}'`
- `'{n} {c}/{u}'`

Current engine still falls through to `Unbounded`; the audit recommends documenting this as intentional conservative behavior rather than leaving it as an accidental default-case fallthrough.

## Proposed slices for Shane review

- **22 — Capture static interpolated qualifier metadata**
  - extend `TypedInterpolatedTypedConstant` so form matching preserves static currency/unit/from/to metadata.
- **23 — Route interpolated qualifier metadata through qualifier consumers**
  - update `ResolveQualifierFromInterpolatedConstant` and `ValidateAssignmentQualifiers`.
- **24 — Extend interpolated interval extraction beyond quantity**
  - money whole-value/magnitude paths; price whole-value/magnitude paths with denominator-aware normalization.
- **25 — Add field-default proof coverage for interpolated typed constants**
  - either generate default-time interval containment obligations or teach initial-state/default analysis to fold these forms.
- **26 — Event arg default resolution (companion prerequisite)**
  - broader than quantity normalization, but required for exhaustive interpolated-default coverage.

## Recommendation

Treat Slices 19–21 as **necessary but not sufficient**. If Shane wants an actually exhaustive interpolated typed-constant story, approve at least Slices 22–25, and decide whether Slice 26 stays in this track or becomes a separate arg-default design item.

---

# Decision Record: Diagnostic Enforcement — Compiler/Runtime Alignment Assessment

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Status:** Proposed
**Trigger:** Shane's concern that the diagnostic enforcement implementation may have compounded compiler/runtime duplication

---

## D1: Diagnostic enforcement did NOT compound duplication

The 11-slice enforcement mission wired ~30 diagnostic codes. Classification:
- **Category 1 (compile-time-only):** ~24 codes — structural definition checks with zero runtime counterpart. No duplication possible.
- **Category 2 (compiler + defense-in-depth fault):** ~4 codes — ProofEngine proof obligations linked to existing `FaultCode` via `[StaticallyPreventable]`. Different operations (abstract proof discharge vs. concrete fault detection). Not duplication.
- **Category 3 (behavioral overlap with runtime):** ~2 codes — qualifier enforcement deliberately bifurcates: static qualifiers in TypeChecker, dynamic qualifiers deferred to ProofEngine/runtime. Correct architecture.

**Decision:** No remediation needed. The enforcement implementation is clean.

---

## D2: Three-layer enforcement model is the canonical architectural frame

The project has three distinct enforcement layers:

| Layer | Owner | Scope | Codes |
|-------|-------|-------|-------|
| **1. Compile-time** | Compiler pipeline (TypeChecker, ProofEngine) | Validates authored definition | 132 DiagnosticCodes |
| **2. Ingress** | `TypeRuntimeMeta.ReadJson` / `TypeRuntime<T>.FromClr` | Validates submitted values at API boundary | Per-type delegates (normalize, domain check) |
| **3. Defense-in-depth** | Evaluator `FaultCode` | Catches impossible-path bugs | 15 FaultCodes, all `[StaticallyPreventable]` |

**Decision:** This three-layer model should be named, documented, and referenced in `docs/runtime/runtime-api.md` or `docs/compiler-and-runtime-design.md`. It eliminates the recurring "are we duplicating?" question by making layer boundaries explicit.

---

## D3: Ingress validation (Layer 2) must be designed as a coherent surface

§0.4 of `quantity-normalization-design.md` designed quantity normalization for the ingress layer. Two additional ingress concerns need the same treatment:

1. **Choice domain validation** — submitted choice values must be in the field's declared domain
2. **Dynamic qualifier checking** — for fields with dynamic qualifiers, submitted args must have compatible qualifiers

**Decision:** Design these as `TypeRuntimeMeta.ReadJson` / `TypeRuntime<T>.FromClr` concerns, following the quantity normalization pattern. Do NOT implement them as evaluator-side checks or as TypeChecker logic clones.

---

## D4: The `[StaticallyPreventable]` chain is the structural duplication guard

Every `FaultCode` maps to exactly one `DiagnosticCode`. Enforced by PRECEPT0001 + PRECEPT0002 (Roslyn analyzers). This chain was in place BEFORE the enforcement mission and was not modified by it.

The chain's invariant: "If the compiler emits no errors, the evaluator should never fault." Runtime faults are defense-in-depth, not re-implementations of compile-time checks.

**Decision:** Preserve and rely on this chain. Any new runtime enforcement obligation should first check whether it belongs in Layer 1 (compiler), Layer 2 (ingress), or Layer 3 (fault). Do not add enforcement logic to the evaluator's opcode loop that could instead be an ingress-time validation.

---

## D5: Catalog-mediated dispatch (Slices 9B/9C) proactively improved alignment

- Slice 9B's `TypedConstantFamilyMeta.FormatErrorCode/SemanticErrorCode` enables runtime's `ParseString` delegate to share the same catalog-driven validation dispatch.
- Slice 9C's `ProofRequirementMeta.DiagnosticCode` unified the proof-obligation → diagnostic mapping through catalog metadata instead of hardcoded branches.

**Decision:** These patterns are correct and should be the model for future enforcement wiring. Prefer catalog-mediated dispatch over direct code-identity switches in both compiler and runtime consumers.

---

### 2026-05-14: Implementation notes created
**By:** Frank
**What:** Created docs/Working/diagnostic-enforcement-implementation-notes.md — technical record of enforcement implementation outcomes
**Why:** Companion to the plan doc; captures implementation reality, staged infrastructure details, and remaining work for future sessions

---

# Decision: Runtime Arg Normalization — Normalize-on-Intake

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Status:** Decided
**Trigger:** Shane's challenge to §0.3 — runtime args with quantity values cannot be pre-normalized by the Builder because their values are unknown at compile time.

---

## Context

§0.3 of the quantity normalization design concluded that "the evaluator never normalizes" and "drift risk is structurally impossible." This was correct for compile-time literals but incomplete — it omitted runtime event args whose values arrive at Fire time in non-base units.

A caller firing `SetWeight` with `newWeight = '6 [lb_av]'` would trigger a false constraint violation against a pre-normalized bound of `5.0 kg` unless the arg's magnitude is normalized before the constraint comparison executes.

---

## Decisions

### D1: Storage Convention — Normalized Magnitudes in PreceptValue

`PreceptValue` for quantity types stores the magnitude in UCUM base units (normalized). The original unit is stored alongside for egress display. This is no longer a "Phase 3 deferred decision" — it is locked now.

### D2: Normalize-on-Intake at the API Boundary

Normalization of runtime quantity values happens in `TypeRuntimeMeta.ReadJson` (JSON lane) and `TypeRuntime<Quantity>.FromClr` (typed lane) — at the Fire/Update/Restore boundary, before values enter the evaluator's arg slot array.

### D3: Same Normalizer, No New Overload

The ingress path calls `TypedConstantNormalizer.Normalize(decimal, UcumParsedUnit?)` — the identical method used by the TypeChecker. No `PreceptValue`-accepting overload is needed because ingress operates on raw extracted values before `PreceptValue` construction.

### D4: Denormalize Method Required

`TypedConstantNormalizer.Denormalize(decimal, UcumParsedUnit?)` must be added for egress (WriteJson, ToClr). This is the arithmetic inverse — divide by factor instead of multiply. Ships in Slice 14 alongside `Normalize`.

### D5: Evaluator Identity Preserved

The evaluator remains a pure plan executor. It never normalizes. The "evaluator never normalizes" invariant holds — normalization happens upstream at the ingress boundary.

### D6: Slice 22 Added (Phase 3)

A new Slice 22 covers runtime ingress normalization wiring: `TypeRuntimeMeta.ReadJson`/`TypeRuntime<Quantity>.FromClr` call `Normalize`; `WriteJson`/`ToClr` call `Denormalize`. Depends on Slice 14 and Phase 3 Builder/Evaluator implementation.

---

## Rationale

- **Normalize-at-compare rejected:** Anti-pattern (§0 explicitly rejected "normalize at every comparison"). Would require normalization logic in every constraint plan touching quantity fields.
- **Normalize-at-write rejected:** Would require a new `NORMALIZE_QUANTITY` opcode (catalog change), making the evaluator participate in normalization (violates plan-executor identity).
- **Normalize-on-intake selected:** Establishes a universal invariant — "all quantity magnitudes in PreceptValue are in base units" — making every downstream consumer (constraint plans, computed fields, inspections) correct without additional normalization.

---

## Consequences

- §0.3's consumer table gains a fourth entry: the ingress boundary.
- The single-implementation claim holds — `TypedConstantNormalizer` is the sole normalization logic.
- PreceptValue internal layout for quantities must accommodate both normalized magnitude and original unit reference.
- Slice 14 gains `Denormalize` as a companion method.

---

# Decision Record: Catalog Integration for Quantity Normalization

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Status:** Proposed
**Scope:** Whether UCUM quantity normalization requires catalog entries, and how it integrates with the runtime opcode/function infrastructure

---

### 2026-05-14: Catalog-driven checklist audit
**By:** Frank (Shane's request)
**What:** Audited catalog-driven-checklist.md against live codebase and catalog-system.md
**Findings:** The checklist still reflected a 12-catalog model, omitted `ExpressionForms` and `Outcomes`, described stale parser derivation points (`OperatorPrecedence`, token-level member-name flags), and understated the current enforcement/tooling surfaces. Live code now derives parser behavior from `Constructs`, `Outcomes`, `Operators`, `ExpressionForms`, and `Types` accessors, with Roslyn enforcement for distributed dispatch and `[CatalogDU]` exhaustiveness.
**Action:** Updated the checklist to reflect the 14-catalog system, current parser derivation rules, current Roslyn enforcement patterns, and current MCP / grammar / language-server sync surfaces.

---

# Decision: Code Sharing Between Compiler and Runtime for Quantity Normalization

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14
**Trigger:** Shane's pushback — "How will the runtime share the same code as the compiler if compiler uses decimals and runtime uses PreceptValue?"

---

### 2026-05-14: Corrected stale precept_language reference
**By:** Frank
**What:** Removed reference to deprecated `precept_language` MCP tool from catalog-driven-checklist.md. Replaced with guidance pointing to current scoped tools.
**Note:** Found `docs/McpServerDesign.md` still referencing `precept_language` as removed, and `.github/copilot-instructions.md` still has a stale `LanguageTool.cs` / `precept_language` sync note. Not edited here.

---

# Decision: Two-Layer Value Architecture for Normalized Bounds

**Author:** Frank (Lead Architect)
**Date:** 2026-05-14T02:08:59-04:00
**Trigger:** Shane's representational consistency pushback on §0/§0.1 of quantity-normalization-design.md

---

### 2026-05-14T05:34:18Z: PreceptValue quantity storage uses the union reference lane, so unit identity is not ruled out by the 32-byte layout

**By:** Scribe

**Status:** Merged from Frank's correction note.

**Merged source:** `frank-preceptvalue-quantity-storage.md`.

- Frank corrected `docs/Working/quantity-normalization-design.md` §2.1: `PreceptValue` bytes 8–23 are a three-way union (`decimal`, `long`, or reference region), not a decimal-only payload.
- The existing 32-byte `PreceptValue` budget therefore does not rule out quantity unit storage through the reference lane; collection backing arrays already prove that lane is viable for composite storage.
- George should treat quantity runtime storage as an open implementation choice between reference-lane composite storage and descriptor metadata, not as a settled "unit must live outside `PreceptValue`" constraint.

---

### 2026-05-14T05:30:09Z: Interpolated quantity normalization splits static-literal and interpolated paths, and `precept_compile` still runs proof diagnostics

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).

**Merged sources:** `frank-interpolated-quantity-analysis.md`, `frank-mcp-compile-correction.md`.

- Frank confirmed the live `NumericOverflow` sample in `samples/Test.precept` is the interpolated expression `'{test2} [lb_av]'`, which resolves to `TypedInterpolatedTypedConstant`, not the static-literal `TypedTypedConstant` path targeted by the existing normalization slices.
- The false positive comes from two independent proof gaps: Strategy 7 treats `TypedInterpolatedTypedConstant` as unbounded because `IntervalOfNarrowed` has no interpolated case, and Strategy 6 cannot discharge `DeclarationValue`-sourced bounds in `SatisfactionCovers`.
- The static-literal normalization fix remains valid but separate; interpolated quantities now carry their own Slices 19–21 track, including static-unit capture on `TypedInterpolatedTypedConstant` and UCUM-based scaling of slot-derived intervals.
- Frank also corrected the design note's tooling claim: MCP `precept_compile` runs the full compiler pipeline including the ProofEngine, so the earlier 0-diagnostic observation was a case-specific behavior note rather than evidence that proof diagnostics were absent from the tool.

---

### 2026-05-14T04:43:00Z: Final review corrected PRE0094 inventory drift and annotated PRE0019 retirement in diagnostic enforcement

**By:** Scribe

**Status:** Merged from Frank's final diagnostic-enforcement review.

**Merged source:** `frank-enforcement-final-review.md`.

- Frank's final audit found PRE0094 was already live in two `TypeChecker.Validation.FieldState.cs` emitters; the enforcement doc's gap count was corrected 50→49 across 9 references, the emission inventory 83→84, and Slice 3 was struck as already wired rather than pending.
- The §3.7 D2 table now explicitly marks PRE0019 as retired so the plan no longer implies active implementation work for a dead diagnostic.
- Elaine naming references remain correct, the PRE0019/PRE0079 sections stay accurate, and Q1, Q10, and Q2 are resolved with no new open questions.

---

### 2026-05-14T04:00:00Z: PRE0079 dead-code audit confirms a TypeChecker wire instead of retirement

**By:** Scribe

**Status:** Merged from Frank's PRE0079 investigation.

**Merged source:** `frank-pre0079-investigation.md`.

- Frank confirmed `DiagnosticCode.OutOfRange` has zero live emitters today; the code currently exists only in the enum, catalog metadata, and fault metadata surfaces.
- PRE0079 remains distinct from PRE0078: PRE0078 `NumericOverflow` is the ProofEngine / Strategy 7 diagnostic for computed-expression interval failures, while PRE0079 is the TypeChecker-only constant-literal-assignment case such as `set Field to 42` against `max 10`.
- The implementation is intentionally trivial and local: compare numeric literal assignments against declared `min`/`max` modifiers during TypeChecker action validation; no new proof infrastructure or extra deduplication pass is needed.

---

### 2026-05-14T06:53:00Z: PRE0019 is retired in favor of proof-stage presence obligations routed through PRE0116

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (3 files -> 1 canonical entry).

**Merged sources:** `frank-pre0019-audit.md`, `frank-pre0019-wiring.md`, `frank-pre0019-wire-vs-retire.md`.

- Frank's audit confirmed PRE0019 `NullInNonNullableContext` has no live emitters in `src\Precept\`; the catalog entry, metadata, and `FaultCode.UnexpectedNull` link exist, but nothing in the TypeChecker, ProofEngine, or runtime produces the code today.
- The intermediate wiring analysis concluded the enforcement plan's old "upgrade TypeMismatch" premise was stale because presence checking already belongs to the guard-aware ProofEngine surface, not to a new TypeChecker-side duplicate.
- The final verdict is RETIRE: presence-proof infrastructure is already plumbed through PRE0116 `UnprovedPresenceRequirement`, but no production code generates `PresenceProofRequirement`; the real fix is to inject those obligations for optional field references and eventually re-point `FaultCode.UnexpectedNull` to PRE0116.

---

### 2026-05-14T03:22:05Z: ProofEngine gap audit closes on Slice 12 presence obligations and keeps PRE0019 cleanup separate

**By:** Scribe

**Status:** Merged from Frank's comprehensive ProofEngine gap audit.

**Merged source:** `frank-proofengine-gap-audit.md`.

- Frank confirmed the critical gap is structural, not discharge logic: `PresenceProofRequirement` is fully plumbed through satisfactions, strategies, diagnostics, and fault-site links, but production code never constructs the obligation.
- The interval proof engine plan now carries **Slice 12: Presence Obligation Generation**, including the `FaultCode.UnexpectedNull` metadata correction from PRE0019 to PRE0116 and the old Slice 12 renumbering to Slice 13.
- PRE0019 remains dead-code retirement work outside the interval slice, PRE0079 remains tracked by diagnostic-enforcement Slice 9C, and the recommended tests should ship with Slice 12 rather than as a pre-slice red test.

---

### 2026-05-14T02:33:16Z: Diagnostic naming canon now uses `when` vocabulary, `CaseMismatch*`, and condition-first proof names

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (3 files -> 1 canonical entry).

**Merged sources:** `elaine-diagnostic-review.md`, `frank-diagnostic-review.md`, `elaine-guard-term-revision.md`.

- Graph-theory and proof-engine jargon are below bar for author-facing diagnostics; the durable naming standard is now plain DSL vocabulary that describes the author's condition rather than compiler internals.
- The adopted family updates are: collection-safety names move to `*WithoutWhen`, CI enforcement names move to `CaseMismatchOn*`, proof diagnostics move to `ModifierNotGuaranteed` / `DimensionQualifierMissing` / `QualifiersMayBeIncompatible` / `InitialStateConstraintUnsatisfied` / `FieldMayBeAbsent`, and PRE0119 settles on `NonTerminalDeadEnd`.
- Rename passes now carry two standing conventions: messages should stay `Subject — Condition — Repair` parseable, and every rename pass must update `FixHint`, `RecoverySteps`, `TriggerCondition`, and examples in the same change.

---

### 2026-05-14T01:04:03Z: B2 currency and unit integrity gaps stay ahead of PRE0094 because D94 already shipped

**By:** Scribe

**Status:** Merged from Frank's sequencing decision.

**Merged source:** `frank-q3-sequencing.md`.

- Shane locked B2 (`PRE0070`–`PRE0074`) ahead of any PRE0094 sequencing change on integrity severity alone.
- Frank confirmed the old rationale for fast-tracking PRE0094 is gone because `ValidateConstructionGuarantees` already wires `InitialEventMissingAssignments` in the type-checking pipeline.
- The field-state-guarantees slices should no longer cite PRE0094 as a blocking dependency in diagnostic-enforcement ordering arguments.

---

### 2026-05-14T00:56:58Z: Dynamic qualifier mismatches stay silent at type-check time and resolve in proof

**By:** Scribe

**Status:** Merged from Frank's Q2 resolution.

**Merged source:** `frank-q2-dynamic-qualifier.md`.

- The TypeChecker silently skips PRE0070–PRE0074 cross-currency and related qualifier checks when the qualifier value is dynamic.
- No partial compile-time diagnostic is emitted for forms like `money in '{CatalogCurrency}'`; Strategy 5 in the ProofEngine remains the enforcement surface.
- The architectural boundary is now durable team memory: dynamic qualifier validation is a proof-time concern, not a static comparison pass.

---

### 2026-05-14T00:52:34Z: PRE0079 stays literal-only, PRE0078 stays interval-proof, and Q10 deduplication is subsumed by that boundary

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).

**Merged sources:** `frank-q1-q10-resolved.md`, `frank-q10-deduplication-subsumed.md`.

- PRE0079 `OutOfRange` is locked to constant-literal assignments in the TypeChecker, while PRE0078 `NumericOverflow` remains the ProofEngine / Strategy 7 diagnostic for non-literal interval failures.
- Frank's initial Q10 note proposed obligation-generation gating to avoid double-reporting, but the final ruling tightened further: no separate dedup mechanism is needed because the pipeline boundary itself keeps literal PRE0079 cases out of the PRE0078 interval path.
- The durable rule is stage separation, not post-hoc suppression.

---

### 2026-05-14T00:05:43Z: Diagnostic enforcement revision now treats PRE0078 as an interval-proof obligation and narrows PRE0079

**By:** Scribe

**Status:** Merged from Frank's diagnostic-enforcement revision.

**Merged source:** `frank-diagnostic-enforcement-revision.md`.

- Frank corrected the enforcement plan's ownership model: PRE0078 `NumericOverflow` is a ProofEngine obligation failure emitted by Strategy 7 (`IntervalContainment`), not a Slice 8 TypeChecker gap.
- Slice 9C now depends on interval-engine Slice 2 so the proof-obligation consistency audit covers `IntervalContainment`, while Gate 1 allow-list removal for PRE0078 waits on that external emission path.
- PRE0079 `OutOfRange` is now narrowed to constant-literal assignments that trivially exceed declared bounds; general expression-level bounds checking belongs to the interval proof engine.

---

### 2026-05-14T00:00:00Z: Interval proof engine resume locked five implementation truths and validated slices 1–4 green

**By:** Scribe

**Status:** Merged from George's interval resume record.

**Merged source:** `george-interval-resume.md`.

- `quantity` is a reserved DSL keyword, so interval tests must use names like `qty`; `nonnegative` is a proof attribute only and does not populate `DeclaredMin`, so interval-bound tests must use explicit `min` modifiers instead.
- Guard narrowing only handles field-to-constant comparisons, and Strategy 2 division-by-zero proof only succeeds when the divisor is a field with a proving modifier like `positive`.
- Sample bounds are an all-path contract: every event arg assigned into a bounded field must carry compatible bounds, and George validated the slice with all 5227 `Precept.Tests` passing.

---

### 2026-05-13T23:55:10Z: Overflow-prevention analysis now defers arithmetic bounds enforcement to Strategy 7

**By:** Scribe

**Status:** Merged from Frank's overflow-prevention revision.

**Merged source:** `frank-overflow-prevention-revision.md`.

- Frank revised `overflow-prevention-design-analysis.md` in place so obsolete `@bounds`, bounded-integer, validator-phase, runtime-fallback, extra-diagnostic, and three-wave rollout claims are clearly superseded by the interval proof engine design.
- Strategy 7 (`IntervalContainment`) now owns bounded arithmetic range checking, `NumericOverflow` emission, modifier-derived bound extraction, guard narrowing, catalog-driven interval transfer, and hover interval display.
- The overflow-prevention analysis keeps only durable value: problem framing, strategy comparison, and historical context, with explicit cross-references to `interval-proof-engine-design.md`.

---

### 2026-05-13T04:52:18Z: Diagnostic gap closures now require paired positive and negative integration tests

**By:** Scribe

**Status:** Merged from Frank's test-quality standards note.

**Merged source:** `frank-test-recommendations.md`.

- Every diagnostic gap closure must ship both a positive `CheckExpectingError` case and a negative `CheckExpectingClean` case; positive-only coverage is below the team quality bar.
- Gate 2 remains a tripwire rather than proof of behavioral coverage because literal `DiagnosticCode.X` references can come from metadata or catalog tests without exercising emission behavior.
- Message-text checks stay optional, TypeChecker span checks stay recommended-not-required, and domain-specific test files remain the preferred placement for new gap tests.

---

### 2026-05-13T04:52:18Z: D94 follow-up coverage now locks omit-boundary, multi-initial-state, and stateless-bailout regressions

**By:** Scribe

**Status:** Merged from George's Slice 13 closeout note.

**Merged source:** `george-d94-slice13-done.md`.

- George added `D94_FieldOmittedInInitialState_NotRequired` to document the D94/D132 boundary where fields omitted in every initial state stay outside D94's construction set.
- `D94_MultipleInitialStates_AllRowsChecked` now proves every initial-state construction row is checked, and `D94_StatelessPrecept_WithInitialEvent_SkipsCheck` durably documents the pre-existing stateless bailout gap.
- `Diagnostics.cs` now describes D94's trigger in terms of construction paths from initial-state rows, and commit `d9edee97` closed both targeted and full `Precept.Tests` validation green.

---

### 2026-05-13T04:52:18Z: D94 false-positive review confirms initial-state row scoping and flags a stateless follow-up gap

**By:** Scribe

**Status:** Merged from Frank's D94 analysis.

**Merged source:** `frank-d94-analysis.md`.

- Frank confirmed commit `4c567cdc` fixed the real D94 bug: construction analysis must inspect only initial-event rows whose `FromState` is an initial state, while non-initial-state rows remain lifecycle paths.
- The D94/D132 boundary is now durable team guidance: `NeedsInitialEvent` excludes fields omitted in every initial state from D94, and D132 owns omit→non-omit lifecycle crossings.
- Follow-up guidance is recorded for targeted tests around omitted initial-state fields and multi-initial-state coverage, plus a low-priority stateless-initial-event gap where D93 and D94 currently both skip enforcement.

---

### 2026-05-13T04:32:04Z: User directive keeps all work on `spike/Precept-V2-Radical` with no new branches

**By:** Scribe

**Status:** Merged from user directive inbox note.

**Merged source:** `copilot-directive-no-new-branches.md`.

- Shane directed that no new branches be created; all work stays on `spike/Precept-V2-Radical` while the team remains in spike mode.
- This is durable team-memory guidance and should be treated as a branch-discipline constraint for follow-on work in the current spike.

---

### 2026-05-13T04:32:04Z: D94 construction analysis now ignores non-initial-state rows on the initial event

**By:** Scribe

**Status:** Merged from George's D94 bugfix note.

**Merged source:** `george-d94-bugfix.md`.

- `ValidateConstructionGuarantees` now analyzes only initial-event rows whose `FromState` is an initial state, so lifecycle transitions on the same initial event no longer participate in construction guarantees.
- `samples\Test.precept` compiles clean again, `D94_NonInitialStateRow_NotChecked` covers the regression, and `dotnet test test\Precept.Tests\Precept.Tests.csproj` closed green at `5138/5138` with commit `4c567cdc`.

---

### 2026-05-13T04:28:00Z: D94 now requires every Form 1 initial-event construction row to assign newly present required fields

**By:** Scribe

**Status:** Merged from George's Slice 11 closeout note.

**Merged source:** `george-slice11-done.md`.

- `ValidateConstructionGuarantees` now keeps the D93 no-initial-event path and adds D94 per initial-event transition row, plus the no-row event-level diagnostic, when required no-default fields remain unset.
- The new enforcement stays scoped to Form 1/stateful precepts so stateless initial-event handler baselines continue using the existing construction semantics.
- `TypeCheckerConstructionTests` gained 10 Slice 11 regressions, and `dotnet build src/Precept/Precept.csproj --nologo` plus `dotnet test test/Precept.Tests/Precept.Tests.csproj --nologo` closed green at `5137/5137` with commit `0b42fd1a`.

---

### 2026-05-13T04:22:01Z: D93 RequiredFieldsNeedInitialEvent now blocks construction when required present fields lack any initial event

**By:** Scribe

**Status:** Merged from George's Slice 10 closeout note.

**Merged source:** `george-slice10-done.md`.

- `ValidateConstructionGuarantees` now emits D93 when a precept has no initial event and still exposes required non-collection, non-computed fields at construction time.
- The construction check reuses the D132 required-field filter, but excludes fields omitted in every initial state so omit-driven draft workflows stay valid until a field becomes present.
- `TypeChecker.Check` now runs the construction validation immediately after field-state validation; `TypeCheckerConstructionTests` plus coupled fixtures and `samples\Test.precept` closed green at `5127/5127` tests, and the sample now fails with D93.

---

### 2026-05-13T00:46:00Z: Sentinel defaults are now a recorded omit anti-pattern in SyntaxReference guidance

**By:** Scribe

**Status:** Merged from Elaine's prose refinement.

**Merged source:** `elaine-omit-antipattern-text.md`.

- Using `default 0`, `default false`, or `default ""` to stand in for "not meaningful yet" is now durable anti-pattern guidance: sentinel defaults hide the transition where the field must first become meaningful.
- The preferred authoring guidance is to declare the field `omit` in every state where it has no business meaning, then initialize it on the transition into a non-omitted state with `set Field = ...`.
- `default` remains reserved for real business defaults rather than lifecycle placeholders.

---

### 2026-05-13T00:45:00Z: Elaine's adopted diagnostic naming v2 is now the canonical field-state family, and Frank applied it to the v3 doc

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).

**Merged sources:** `elaine-naming-v2.md`, `frank-adopted-names.md`.

- Elaine's adopted compact v2 naming replaces the earlier sentence-like normalization pass: D130 = `OmittedFieldReadInState`, D131 = `OmittedFieldSetInTargetState`, and D132 = `RequiredFieldUnassignedOnEntry`.
- The adopted set keeps the catalog house style aligned with compact condition phrases like `InitialEventMissingAssignments` and `RequiredFieldsNeedInitialEvent` while keeping state or entry context explicit instead of compiler shorthand.
- Frank applied those names throughout `docs\Working\field-state-guarantees-v3.md`, including the summary, tables, section headers, inline prose, and design-motivation references, and intentionally left `src\Precept\Language\SyntaxReference.cs` untouched because Elaine had concurrent prose work there.

---

### 2026-05-13T00:32:50Z: Elaine's naming-normalization proposal pushes the field-state diagnostic family toward subject-first catalog names

**By:** Scribe

**Status:** Merged from Elaine's diagnostic naming normalization proposal.

**Merged source:** `elaine-diagnostic-naming-normalization.md`.

- Elaine judged the catalog house style to be subject-first, plain-English condition naming, which makes `MustSetOmitToNonOmit` the clearest outlier and leaves `ReadOfOmittedField` / `WriteToTargetOmittedField` sounding more robotic than the rest of `DiagnosticCode`.
- Her proposed normalized family is `FieldOmittedInStateCannotBeRead`, `FieldOmittedInTargetStateCannotBeSet`, and `RequiredFieldNeedsAssignmentWhenBecomingPresent`, plus a tightened `InitialEventMissingRequiredFieldAssignments`, while `RequiredFieldsNeedInitialEvent`, `ConflictingAccessModes`, and `RedundantAccessMode` stay as-is.
- Even with better identifiers, the Problems panel still needs field-first, state-aware, repair-oriented message text; the stable diagnostic name alone should not carry that UX burden.

---

### 2026-05-13T00:32:50Z: Elaine's UX review freezes the canonical v3 field-state codes at D130, D131, and D132 and flags D132 naming as unshipped

**By:** Scribe

**Status:** Merged from Elaine's diagnostic UX review.

**Merged source:** `elaine-diagnostics-ux-review.md`.

- The v3 field-state design now has one canonical numbering surface: `ReadOfOmittedField` = D130, `WriteToTargetOmittedField` = D131, and `MustSetOmitToNonOmit` = D132. Earlier ledger references to provisional D131/D133/D135 should be read as D130/D131/D132.
- Elaine judged D130 and D131 conceptually sound, but flagged D132's current name as compiler shorthand that should be rewritten into author language before ship.
- Her proposed Problems-panel copy is now durable team memory: `Field '{0}' is omitted in state '{1}' and cannot be read in this expression.`, `Field '{0}' is omitted in target state '{1}'; this transition cannot set it.`, and `Required field '{0}' is omitted in '{1}' but present in '{2}'; add \`set {0} = ...\` to this transition.`

---

### 2026-05-13T00:26:25Z: Required-field initialization semantics stay compile-time complete across all three precept forms

**By:** Scribe

**Status:** Merged from Frank's field-initialization analysis.

**Merged source:** `frank-field-init-semantics.md`.

- Stateful precepts with an initial event remain compile-time safe because `InitialEventMissingAssignments` must force every required no-default field to be assigned during construction.
- Stateful precepts without an initial event cannot carry required no-default fields at all because `RequiredFieldsNeedInitialEvent` rejects that shape, while stateless precepts follow the same constructor guarantees without state-entry semantics.
- The open spec gap remains omit→non-omit reentry: non-optional, no-default fields need a must-set rule on the transition, while defaulted and optional fields can re-materialize from their valid baseline values.

---

### 2026-05-13T00:26:25Z: Field-state guarantees v3 resets the implementation boundary to D130, D131, and D132 only

**By:** Scribe

**Status:** Merged from Frank's v3 design closeout.

**Merged source:** `frank-v3-design-complete.md`.

- Frank replaced the blocked v2 plan with a spec-grounded v3 design: keep `ReadOfOmittedField` (D130), keep `WriteToTargetOmittedField` (D131), and add `MustSetOmitToNonOmit` (D132).
- This record was canonicalized after the v3 renumber pass; older references to provisional D131/D133/D135 now map to D130/D131/D132.
- The redesign explicitly drops the old v2-only from-state target-write, readonly/access-condition, and ProofEngine enforcement surfaces because the shipped spec separates Update access modes from Fire/`set` semantics.
- The design note also records the remaining spec edits needed when this ships: annotate the §3.5 scope gap, define omit→non-omit required-field reentry, and state state-hook access-mode direction explicitly.

---

### 2026-05-13T00:26:25Z: Circular static-init crash in Tokens/Types is closed with lazy keyword-set initialization

**By:** Scribe

**Status:** Merged from George's runtime fix note.

**Merged source:** `george-circular-static-init-fix.md`.

- `Tokens.KeywordsValidAsMemberName` now uses a `Lazy<FrozenSet<TokenKind>>` backing field, so `Types.All` is not touched during `Tokens..cctor()`.
- The crash path was a CLR static-constructor cycle: `Types.GetMeta()` re-entered `Tokens..cctor()`, which previously read `Types.All` before assignment and faulted in `SelectMany`.
- George reported the public API shape unchanged and the repository suite green at `4996/4996` tests passing.

---

### 2026-05-13T00:00:39Z: Kramer's B4 fix pass closed the no-obligations copy bug and proof-summary de-dup coverage

**By:** Scribe

**Status:** Merged from Kramer's B4 fix note.

**Merged source:** `kramer-b4-fixes.md`.

- Kramer extended `StateGraph.EdgeProofStatus` with `HasObligations`, populated it during compiler enrichment, and switched rich state hover to render the locked no-obligations wording when connected edges exist but none carry proof obligations.
- The fix pass also added the missing regression anchors: language-server coverage for the connected-edge/no-proof-obligation path and core projection coverage that duplicate `Requirement.Description` values collapse to one unresolved summary.
- `docs/Working/hover-design.md` was updated to match the shipped projection/rendering rule, and targeted language-server tests, compiler-edge-proof tests, and the language-server build all passed.


### 2026-05-13T00:00:38Z: Hover B4 review approved the edge-proof projection shape but blocked a no-obligation copy bug and missing de-dup coverage

**By:** Scribe

**Status:** Merged from Frank's B4 review.

**Merged source:** `frank-b4-review.md`.

- Frank approved the overall B4 architecture: `StateGraph.EdgeProofStatus`, compiler enrichment, incident-edge filtering, and rich state-card rendering were all wired in the expected places.
- Two blockers remained before closeout: the no-obligations branch used the wrong emptiness check and the projection suite did not yet lock `Requirement.Description` de-duplication.
- Targeted B4 tests passed even though an unrelated pre-existing modifier-catalog failure still kept the broader core suite red.

---

### 2026-05-13T00:00:00Z: PRE0091 `AmbiguousTypedConstant` ships narrow first and enforcement infrastructure stays explicit and comment-stripped

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (5 files -> 1 canonical entry).

**Merged sources:** `frank-q4-narrow.md`, `frank-q6-allowlist-granularity.md`, `frank-q7-analyzer-scan-scope.md`, `frank-q8-doc-comment-stripping.md`, `frank-q9-allowlist-coordination.md`.

- PRE0091's first implementation stays limited to temporal quantity ambiguity because `ResolveTypedConstant` normally receives `expectedType`; broad candidate enumeration would be speculative infrastructure for error-recovery-only paths.
- Gate 1 allow-list entries need only root-cause cluster comments, while Gate 2 scans an explicit pipeline-centered file set and strips `//`, `/** */`, and `///` comments before matching `DiagnosticCode.*` references.
- PRE0078 allow-list cleanup is owned by the interval-engine PR itself: once a real emission site exists, the stale-entry analyzer forces the same PR to remove the allow-list entry.

---

### 2026-05-12T23:56:04Z: B4 edge-proof status projection landed across the compiler, graph model, and rich state hover

**By:** Scribe

**Status:** Merged from George's B4 completion note.

**Merged source:** `george-b4-complete.md`.

- George added `StateGraph.EdgeProofStatuses` plus the `EdgeProofStatus` record so graph-level proof projections survive into tooling without rejoining proof data inside the language server.
- Compiler enrichment now maps unresolved transition-row obligations onto concrete graph edges, de-duplicates repeated descriptions, and marks edges proven when no unresolved summaries remain.
- The rich state card now renders per-edge proof gaps or the matching proven/no-obligations copy, and targeted test counts rose to `278` language-server tests and `4973` core tests.

---

### 2026-05-12T23:50:08Z: Modifier-gap regression coverage landed across price, exchangerate, business magnitudes, and identity types

**By:** Scribe

**Status:** Merged from Soup Nazi's modifier-gap regression suite note.

**Merged source:** `soup-nazi-modifier-gap-tests.md`.

- Soup Nazi added 22 tests across three files: a 17-test `PriceExchangeRateModifierTests.cs` suite, a 3-test `IdentityTypeModifierTests.cs` suite, and two `maxplaces` additions in `MoneyQuantityModifierRegressionTests.cs`.
- The suite locked the expected split between newly-fixed price paths, already-green exchangerate and identity-type behavior, and the deliberate negative coverage that `exchangerate min/max` must stay invalid.
- The tester note also preserved two useful cautions: the price regression guards were not asserting the actual pre-fix diagnostic code, and five `ModifiersTests` theory failures were pre-existing drift rather than fallout from this batch.

---

### 2026-05-12T23:50:08Z: Modifier catalog gaps were closed for `price`, `exchangerate`, business-magnitude `maxplaces`, and identity-type redundancy handling

**By:** Scribe

**Status:** Merged from George's modifier-gap implementation note.

**Merged source:** `george-modifier-catalog-gaps-fixed.md`.

- Commit `a727dddb` widened `ZeroBoundNumericTypes` to include `Price` and `ExchangeRate`, widened ranged modifiers to include `Price`, and introduced `BusinessMagnitudeTypes` so `maxplaces` now applies across decimal, money, quantity, price, and exchangerate.
- The TypeChecker applicability guard now skips implied modifiers, collapsing identity-type `notempty` handling to `RedundantModifier` instead of the previous double-diagnostic shape.
- George validated the implementation at `4969/4969` core tests green and handed Soup Nazi the missing regression scenarios to lock.

---

### 2026-05-12T23:48:11Z: Hover B2/B3 final re-review approved after `omit all` hover coverage landed

**By:** Scribe

**Status:** Merged from Frank's final B2/B3 re-review.

**Merged source:** `frank-b2-b3-review-v3.md`.

- Frank approved the final B2/B3 pass once construct-card routing, omit-aware mutability summaries, and `omit all` behavior all matched `docs/Working/hover-design.md`.
- The missing regression coverage was closed with two new hover tests that lock the `omit all` path on both affected surfaces.
- Targeted validation closed green at `275/275` language-server tests.

---

### 2026-05-12T23:45:45Z: Required-field constructor enforcement is specified but entirely unimplemented

**By:** Scribe

**Status:** Merged from Frank's required-field initialization gap analysis.

**Merged source:** `frank-required-fields-analysis.md`.

- Frank confirmed PRE0093 and PRE0094 already exist in the diagnostic catalog and message templates, but no pipeline stage emits them and no tests or samples exercise the constructor contract.
- The gap is broader than one check: `Precept.Create()` is still a stub, samples currently rely on editable Draft-state initialization, and even event-argument syntax appears incomplete for the spec's constructor examples.
- The durable owner-direction question is whether to implement the spec as written now, refine the spec around the shipped Draft-edit path, or split compile-time advisory from runtime enforcement.

---

### 2026-05-12T23:42:25Z: Hover B2/B3 re-review stayed blocked until `omit all` gained explicit regression coverage

**By:** Scribe

**Status:** Merged from Frank's B2/B3 re-review.

**Merged source:** `frank-b2-b3-review-v2.md`.

- Frank confirmed the routing-order fix and omit-driven mutability summaries were structurally correct and that targeted language-server tests were green.
- The remaining blocker was narrow and explicit: no hover regression yet locked the `omit all` path on field and state cards.
- Durable review guidance is that B2/B3 were functionally close, but not mergeable until the `omit all` surface was test-covered.

---

### 2026-05-12T23:38:00Z: Field-state guarantees v2 consistency review blocked readonly/access-condition enforcement on event-driven `set`

**By:** Scribe

**Status:** Merged from Frank's v2 consistency review.

**Merged source:** `frank-v2-consistency-review.md`.

- Frank confirmed D133, the multi-field parser fix, omit/access-mode unification, and D42/D43 emission are architecturally sound and grounded in the spec.
- He blocked the wider design where it crossed the spec boundary: `readonly` and guarded `editable` restrict Update-path patches, not event-driven `set` actions, so D132, D134, and the proposed Phase 2 proof surface were not approved.
- The review also recorded that general from-state D130 and guard-read D131 need either narrower justification or explicit spec extension rather than being presented as already-shipped language law.
- Numbering note: this review uses the provisional field-state numbering; canonical v3 renumbering later mapped old D131 -> D130 and old D133 -> D131, with D135 becoming D132.

---

### 2026-05-12T23:36:02Z: Field-state guarantees v2 doc was finalized pending owner sign-off after resolving its tracked open questions

**By:** Scribe

**Status:** Merged from Frank's v2 doc update.

**Merged source:** `frank-v2-doc-update.md`.

- Frank advanced `docs/Working/field-state-guarantees-v2.md` to "Design Finalized — Pending Implementation" after documenting resolutions for Q1-Q5, B1-B3, and the W5 false-positive limitation.
- The finalized draft records `IsBroadcast`, spec-truth baselines, DNF handling for OR conditions, wildcard multiplicity, and the self-loop dual-diagnostic decision as the working implementation contract.
- This record stays explicitly pending Shane's sign-off rather than claiming the design was owner-approved.

---

### 2026-05-12T23:33:48Z: Self-loop omit validation intentionally fires both D130 and D133 when `from` and `to` are the same state

**By:** Scribe

**Status:** Merged from `copilot-self-loop-omit-both.md`.

- For a self-loop transition `S -> S`, omitting a field in `S` triggers both D130 and D133 because `ValidateFieldStateAccess` checks the omitted field against both the from-state and the to-state, and both roles resolve to the same state.
- Self-loops remain a general-case path, not a special case: the engine should keep the uniform from-state D130 check and to-state D133 check rather than suppressing one side when the state identities match.
- Diagnostic expectations and future regression tests should treat dual reporting on self-loops as intentional behavior, not as a deduplication bug.
- Numbering note: this record uses the pre-canonical field-state numbering; the target-state write diagnostic `D133` was later renumbered to canonical v3 `D131`.

---

### 2026-05-12T23:25:25Z: Final comma-list `StateTarget` spike redesign is approved with proposal-wording follow-up

**By:** Scribe

**Status:** Merged from Frank's final spike review.

**Merged source:** `frank-final-spike-review.md`.

- Frank approved commits `53d68d51` and `cf3c6a81`: `ResolvedStateTarget` now carries explicit wildcard truth inside the TypeChecker, while `NormalizeTransitionRow` remains the intentional compatibility boundary that projects wildcard rows back to `TypedTransitionRow.FromState = null`.
- The review confirmed the spike stayed metadata-disciplined where it matters: wildcard detection remains token-metadata-driven, runtime/graph/proof behavior stays unchanged, and comma-list expansion remains pure syntactic sugar with first-match preservation plus strengthened regression coverage.
- Follow-up is proposal hardening, not implementation repair: the formal issue should defend the wildcard compatibility boundary, stay honest about the localized hand-shaped parser grammar scan, and strengthen locked decisions `D3`/`D4` with rejected alternatives, accepted tradeoffs, and explicit research/corpus grounding.

---

### 2026-05-12T23:20:42Z: Soup Nazi re-review approves George's blocker closeout and the explicit-wildcard `ResolvedStateTarget` redesign

**By:** Scribe

**Status:** Merged from Soup Nazi's re-review verdict.

**Merged source:** `soup-nazi-re-review-verdict.md`.

- George's commit `53d68d51` closed blocker set B1-B5 plus nit N1 with parser AST anchors, stronger expansion and guard-clone assertions, multi-unknown-state diagnostic fan-out coverage, and the corrected `4969` core-test count in the spike doc.
- George's commit `cf3c6a81` replaced the `ResolvedStateTarget` null-sentinel with an explicit `IsWildcard` contract while preserving the nullable wildcard bridge only at `NormalizeTransitionRow`.
- Soup Nazi re-reviewed both commits, reported `0 blockers / 2 good findings`, and approved the spike for merge readiness.

---

### 2026-05-12T23:20:42Z: Hover B2/B3 follow-up shipped rich state routing and mutability-honest summaries

**By:** Scribe

**Status:** Merged from Kramer's completion note.

**Merged source:** `kramer-b2-b3-complete.md`.

- `HoverHandler.cs` now routes state identifiers and construct cards through the rich-hover path before generic token help can mask them.
- `RichHoverFactory.cs` now reuses the rich field/state/event/arg symbol cards and reports only unconditional writable truth in V1 mutability summaries, excluding guarded access while keeping omit-state locks.
- Validation closed green at `271/271` language-server tests plus a successful language-server build.

---

### 2026-05-12T23:20:42Z: Frank's B2/B3 hover review keeps declaration-span routing and omit-aware mutability blocked

**By:** Scribe

**Status:** Merged from Frank's blocked review.

**Merged source:** `frank-b2-b3-review.md`.

- Frank confirmed the good news first: state identifier routing is fixed, `reject` and qualifier precedence are correct, and `271/271` language-server tests passed.
- Blocker B1 remains: `TryCreateTypeHover(...)` and `TryCreateActionHover(...)` still return before rich construct routing, so declaration-span `money` and action keywords can bypass the construct card.
- Blockers B2 and B3 remain: the field and state mutability summaries still ignore state-local `omit` declarations in the global/unconditional writable fallbacks, so omitted surfaces can still be reported as `✏️` instead of `🔒`.

---

### 2026-05-12T23:02:04Z: Hover B1 review locked the compact-card proof-hover contract before the fix pass

**By:** Scribe

**Status:** Merged from Frank's pre-fix blocked review.

**Merged source:** `frank-b1-review.md`.

- Frank's blocker list made the contract explicit: qualifier proof diagnostics, qualifier proof expressions, and qualifier declarations all had to use the compact 3-line card shapes from `docs/Working/hover-design.md` instead of the older forensic sections.
- The review also locked the exact wording surface: `✅ Proven`, `⚡ Enforced`, `⚠️ Gap`, plus `proven` instead of `proved`, with transition hover using `Gap:` rather than `Proof gap:`.
- Routing itself was not the problem: proof-first dispatch in `HoverHandler` and the rich proof/qualifier precedence inside `RichHoverFactory` were already judged structurally correct; the rendered copy and stale red tests were the blocked surface.

---

### 2026-05-12T23:02:04Z: George's B1 hover fix pass landed the compact-card contract and cleared the targeted language-server suites

**By:** Scribe

**Status:** Merged from George's hover-fix closeout note.

**Merged source:** `george-b1-fixes.md`.

- Commit `c2a38a56` replaced verbose proof-gap hover blocks with the compact qualifier/proof cards, locked the shared badge vocabulary to `✅ Proven`, `⚡ Enforced`, and `⚠️ Gap`, and normalized shipped copy from `proved` to `proven`.
- `HoverHandler.cs` required no routing change; the fix stayed inside `RichHoverFactory.cs` and matching `HoverHandlerTests.cs` expectations.
- Validation closed green at `41/41` `HoverHandlerTests` and `269/269` full language-server tests, while repo-wide `dotnet test` still hits the unrelated multi-unknown-state baseline in `TypeCheckerTransitionTests`.

---

### 2026-05-12T23:02:04Z: Field-state guarantees v2 stays approved only after omit-state resolution, broadcast compatibility, and the real test matrix are locked

**By:** Scribe

**Status:** Merged and reconciled from Frank's design review and Soup Nazi's test-plan audit.

**Merged sources:** `frank-v2-design-review.md`, `soup-nazi-v2-test-review.md`.

- Implementation cannot start until omit declarations resolve `StateTarget` before unifying into `AccessModes`, `NameBinder` iterates every field in a multi-field target, and broadcast compatibility keeps a stable `all` identity instead of collapsing `.FieldName` to null.
- The structural/conditional split, pipeline ordering, and D130-D134 envelope remain approved, but the plan's real test surface is much larger than advertised: event handlers, wildcard/self-loop multiplicity, diagnostics stage lists, and broadcast regressions all need explicit anchors.
- Open questions that still block exact tests are now durable team memory: D132 baseline semantics for unmentioned fields, OR-disjunct handling in access conditions, wildcard diagnostic multiplicity, and self-loop D130/D133 double-report behavior.
- Numbering note: this v2 record preserves the provisional field-state numbering; when the surviving v3 diagnostics were canonicalized, old D131/D133/D135 became D130/D131/D132.

---

### 2026-05-12T23:02:04Z: Comma-list `StateTarget` spike is architecturally approved, but parser/test closure and count reconciliation remain open

**By:** Scribe

**Status:** Merged and reconciled from Frank's spike review and Soup Nazi's test-gap audit.

**Merged sources:** `frank-spike-review.md`, `soup-nazi-spike-review-gaps.md`.

- Frank approved commit `a63d88b4` on architecture: parser disambiguation stays catalog-derived, `ResolveStateTargets` remains the single normalization path, expansion is pure-copy, and the grammar already accepts comma lists.
- The follow-up gate is test completeness, not design shape: parser AST coverage still needs 2-name, 3+-name, whitespace, and trailing-comma anchors, and expansion coverage must assert cloned guard/action/outcome semantics plus multi-unknown-state diagnostic fan-out.
- Published validation counts must match real output; the durable tester note is that current `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-build --nologo` reports `4962`, so any `4966` claim needs reconciliation before handoff.

---

### 2026-05-12T22:39:45Z: Field-state guarantees v2 will enforce structural access violations in the TypeChecker and conditional access through proof obligations

**By:** Scribe

**Status:** Merged from Frank's late inbox note.

**Merged source:** `frank-field-state-v2.md`.

- Enforcement is now explicitly split by certainty: omit/unconditional-readonly violations stay in a new TypeChecker validation pass, while guarded-editability checks become a new `ProofRequirementKind.AccessCondition` path in the ProofEngine.
- `FieldTargetSlot` must become multi-name so access-mode and omit declarations stop dropping fields 2..N from comma-separated targets, and omit declarations should feed `ctx.AccessModes` as `ModifierKind.Omit` rather than living on a disconnected enforcement surface.
- The design also locks the diagnostic envelope for this work: activate existing D42/D43 declaration checks and add D130-D134 for structural write/read failures plus unproved access conditions.
- Numbering note: later v3 canonicalization renamed the surviving field-state diagnostics from provisional D131/D133/D135 to D130/D131/D132.

---

### 2026-05-12T22:25:28Z: Language-spec audit locks a targeted cleanup for stale references, overclaimed contracts, and incomplete diagnostic coverage

**By:** Scribe

**Status:** Merged from Frank's audit note.

**Merged source:** `frank-spec-audit.md`.

- Frank confirmed several high-confidence spec defects in `docs/language/precept-language-spec.md`: the dead `docs/PreceptLanguageDesign.md` grounding reference, stale "not yet built" pipeline wording, a vestigial open-questions placeholder, misleading `C48`-style labels, six `ConstructKind` names that do not match code, and an incomplete diagnostic-groups table.
- The audit also preserves the owner-decision boundary: §0.5 and §0.6 currently overclaim shipped behavior if they are meant to describe only implemented guarantees, and ProofEngine Strategy 6 remains undocumented unless the owner wants it promoted to public documentation.
- Frank ranked the immediate cleanup around preamble/status accuracy, replacing `C48`-`C52` notation with real diagnostic names, and explicitly qualifying which design-contract items are future or partial.

---

### 2026-05-12T22:25:28Z: Language docs and README now match shipped comma-list `StateTarget` behavior

**By:** Scribe

**Status:** Merged from Frank's docs-closeout note.

**Merged source:** `frank-s6-docs-done.md`.

- `docs/language/precept-language-spec.md`, `docs/language/precept-grammar.md`, `docs/compiler/parser.md`, `docs/compiler/type-checker.md`, `docs/compiler/name-binder.md`, and `README.md` now describe `StateTarget := Identifier ("," Identifier)* | any` while keeping `EventTarget` single-name.
- The synced docs now record pure-copy expansion semantics plus per-name span/resolution behavior for comma-list state targets, and the parser docs now describe the shipped variable-offset state-scoped disambiguation scan.
- `docs/language/catalog-system.md` and `docs/compiler/diagnostic-system.md` were explicitly verified as already accurate for the shipped comma-list subset and needed no further change.

---

### 2026-05-12T22:18:18Z: User model directive locks claude-opus-4.7 behind explicit permission while keeping claude-opus-4.6 available under normal rules

**By:** Scribe

**Status:** Merged from Shane's directive notes.

**Merged sources:** `copilot-directive-opus47.md`, `copilot-directive-opus46-ok.md`.

- The hard rule is now durable team memory: no one uses `claude-opus-4.7` unless Shane explicitly authorizes it for the session or task.
- The clarification also locks the non-ban boundary: `claude-opus-4.6` remains available for complex work under the existing model-selection policy and is not part of the prohibition.
- Model-selection guidance should therefore treat the directive as a surgical ban on `claude-opus-4.7`, not a general no-opus policy.

---

### 2026-05-12T22:18:18Z: Hover V1 now routes construct cards first, reports only unconditional mutability truth, and exposes graph-edge proof gaps through StateGraph metadata

**By:** Scribe

**Status:** Merged and deduplicated from Kramer's hover-closeout notes.

**Merged sources:** `kramer-b2-b3-routing-and-mutability.md`, `kramer-b4-edge-proof-status.md`, `elaine-b4-design-doc-update.md`.

- `HoverHandler.cs` now lets construct-span cards beat generic operator/function/accessor fallbacks while preserving the existing proof-first behavior and identifier-driven symbol hovers where they are still the honest trigger surface.
- `RichHoverFactory.cs` now limits writable counts and state mutability summaries to unconditional `AccessModes`, omitting guarded access from V1 mutability claims instead of synthesizing misleading read-only complements.
- `StateGraph` now carries edge proof-status metadata projected from unresolved transition proof obligations, allowing state hover to explain unproven graph edges without re-joining proof data inside the language server.
- `docs/Working/hover-design.md` is now synced to the shipped B4 state-proof narrative, including the `📍` / `✅ Proven` / `⚠️ Gap` badge vocabulary and the fact that B4 appends to the rich state hover instead of shipping as a standalone hover kind.
- Kramer's B2/B3/B4 validation stayed green on the full build plus core and language-server test suites (`4966` core tests, `281` LS tests).

---

### 2026-05-12T22:18:18Z: Construct metadata now describes `StateTarget` as a single state, `any`, or a comma-delimited state list

**By:** Scribe

**Status:** Merged from George's manifest closeout; inbox artifact was absent, so the ledger was updated directly from the spawn manifest.

- `src/Precept/Language/ConstructSlot.cs:18` now documents `ConstructSlotKind.StateTarget` as accepting a state name, `any`, or a comma-delimited list of state names.
- `src/Precept/Language/Constructs.cs:29` now gives the shared `SlotStateTarget` description the same list-capable wording, keeping catalog-facing construct metadata aligned with the shipped parser and type-checker behavior.
- George reported the S5 catalog wording pass complete and confirmed the validation build passed.

---

### 2026-05-12T22:18:18Z: Comma-delimited state-target lists now ship end-to-end across parser, type checker, normalization, and diagnostics

**By:** Scribe

**Status:** Merged, deduplicated, and reconciled across George's S1/S2/S3/S4 notes.

**Merged sources:** `george-s1-parser-done.md`, `george-s2-tc-done.md`, `george-s3s4-done.md`.

- `ParseStateTarget()` now accepts `any` or comma-delimited state-name lists, preserves per-name spans, and parser disambiguation scans past those lists before resolving state-scoped constructs.
- Transition-row normalization expands one typed row per named source state while preserving undeclared names as explicit per-name diagnostics instead of collapsing them into wildcard semantics.
- The remaining state-target normalization passes are now aligned: ensures, access modes, and state hooks each expand per state-list entry, while omit declarations stay unchanged because they do not consume `StateTargetSlot`.
- Two dedicated diagnostics close the authoring contract: `StateListContainsWildcard` rejects mixed named-state plus `any` lists, and `DuplicateStateInList` warns on duplicate names before deduplicated expansion proceeds.

---

### 2026-05-12T22:18:18Z: Canonical samples now demonstrate comma-list state syntax only where semantics stay identical

**By:** Scribe

**Status:** Merged from Frank's sample-closeout note.

**Merged source:** `frank-s7-samples-done.md`.

- `samples/hiring-pipeline.precept`, `samples/it-helpdesk-ticket.precept`, and `samples/utility-outage-report.precept` now use comma-list source states for the pure-copy transitions George's runtime/parser work actually supports.
- Frank deliberately left rows expanded anywhere guards, actions, or outcomes diverged, preserving the spike ruling that comma lists are syntactic sugar for identical rows rather than a semantic broadening of transition behavior.
- Validation for the three edited samples stayed clean through VS Code diagnostics even though MCP `precept_compile` was unavailable during the pass.

---

### 2026-05-12T15:15:10Z: Remaining `inventory-item.precept` proof fallout splits into shipped G1, BUG-C event-arg qualifiers, and deferred algebraic G2

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-proof-coverage-expansion.md`.

- Frank closed the root-cause triage: RC1 was the compound-unit qualifier bug in `ResolveQualifierFromInterpolatedConstant`, RC2 is blocked on BUG-C because unqualified `exchangerate` event args cannot carry `in ... to ...` metadata yet, and RC3 is a later algebraic proof-composition problem.
- The decision explicitly reframed F4 as already implemented in runtime semantics; the remaining ReceiveShipment currency fallout is a data-shape gap, not a missing `CurrencyConversion` policy.
- G1 stayed the immediate slice because it clears four diagnostics surgically; G2 remains deferred until BUG-C exposes qualifier data on event args.

---

### 2026-05-12T15:15:10Z: Proof hover spec is now consolidated into `docs/Working/hover-design.md`

**By:** Scribe

**Status:** Merged from Elaine's inbox note.

**Merged source:** `elaine-hover-merge.md`.

- Elaine merged the proof-hover design into `docs/Working/hover-design.md`, replacing the old standalone proof-hover draft with the canonical hover spec.
- Qualifier hover now uses the proof-aware Scenario 4 card, and the working doc carries explicit scenarios for qualified fields, proof-bearing binary expressions, and proof diagnostic squiggles.
- Hover routing, precedence, proof data-shape requirements, and proof-specific open questions now live in one maintained design surface instead of split working docs.

---

### 2026-05-12T15:15:10Z: Proof hover ships honest fallback reasons until compile-time proof exposes a stable failure-reason payload

**By:** Scribe

**Status:** Merged from Kramer's inbox note.

**Merged source:** `kramer-hover-gap-proof-reason.md`.

- Kramer recorded that `Compilation.Proof` already gives hover enough truth for verdict, requirement, operands, qualifiers, context, and fix hints, but not a stable unresolved-reason payload for the `Reason:` line.
- The shipped hover design therefore prefers explicit heuristics over invented precision when proof failure reasons are not surfaced directly from the compile-time ledger.
- Elaine v4 hover implementation landed in commits `5ab6030e`, `516aa6ba`, and `7829e9c6`, and `264/264` `Precept.LanguageServer.Tests` passed with the current honest-fallback approach.

---

### 2026-05-12T15:15:10Z: Compound-unit interpolated constants now resolve full `{A}/{B}` qualifiers before denominator fallback

**By:** Scribe

**Status:** Merged from George's inbox note.

**Merged source:** `george-g1-compound-unit-fix.md`.

- George fixed `ResolveQualifierFromInterpolatedConstant` so typed constants carrying both numerator and denominator unit slots build the full compound qualifier string instead of collapsing to the denominator.
- The G1 pass shipped in commit `cb4fbf57`, kept `StaticMagnitude` on the typed interpolated constant node, and reused trusted positive-rule proofs so downstream nonzero obligations can discharge from the cleaned qualifier evidence.
- RC1 fallout is cleared in `samples/inventory-item.precept`: the PRE0114s at plan lines 122/123 and the cascading DivisionByZero diagnostics at lines 137/142 are gone, with docs/history synced in `1ee54bdb`.

---

### 2026-05-12T13:52:04Z: Same-qualifier arithmetic metadata and PRE0114 operand labeling are now fixed together

**By:** Scribe

**Status:** Merged from George's inbox note.

**Merged source:** `george-proof-diagnostic-fix.md`.

- George added `Match: QualifierMatch.Same` to the six same-qualifier money/quantity/price `+/-` operations so nested arithmetic results now retain the proved qualifier contract the checker and proof engine already know how to recurse through.
- `ProofEngine.CreateDiagnostic(...)` now describes full expressions recursively and attaches resolved qualifier values to PRE0114 operand labels, replacing placeholder fallbacks on both the computed-expression and collection-access paths.
- The fix shipped in commit `d187230c` with new proof-engine and operation-catalog regressions, and George reported the full suite green at `5507/5507`.

---

### 2026-05-12T13:52:04Z: Proof-stage diagnostics and hover need operand truth, repair guidance, and dedicated routing

**By:** Scribe

**Status:** Merged from Elaine's inbox note.

**Merged source:** `elaine-proof-ux-audit.md`.

- Elaine's proof UX audit locked the core teachable-moment failures: `<unknown>`-style placeholders are never acceptable, qualifier diagnostics must show the actual conflicting values, and human repair guidance must be paired with structured args rather than baked into sentence fragments.
- The audit also established that proof hover is a routing problem as much as a content problem: generic operator and transition hover frequently wins before authors ever see proof context.
- The durable design split is now explicit: declaration-contract hover, expression-proof hover, and diagnostic-squiggle hover are separate UX jobs and should not be collapsed into one generic card.

---

### 2026-05-12T13:52:04Z: Proof hover working spec is filed for Shane sign-off before implementation

**By:** Scribe

**Status:** Merged from Elaine's inbox note.

**Merged source:** `elaine-hover-design-filed.md`.

- Elaine wrote `docs/working/proof-hover-design.md` as the canonical working spec for proof-hover UX before Kramer starts implementation work.
- The doc covers precedence failures in the current hover stack, scenario-specific card requirements, routing rules, and the proof-evidence data shape the implementation must have available.
- Status is now durable: the hover design is ready for Shane review and annotation, not for silent implementation drift.

---

### 2026-05-12T13:52:04Z: Proof diagnostics now use explicit 'Cannot prove…' wording with structured qualifier payloads

**By:** Scribe

**Status:** Merged from George's inbox note.

**Merged source:** `george-proof-message-rewrites.md`.

- George implemented Elaine Section A's proof-message rewrites in commit `1d8962f7`, including the six-argument PRE0114 shape that carries operand labels, axis, context clause, and left/right qualifier values separately.
- PRE0112, PRE0113, PRE0115, PRE0116, PRE0082, PRE0083, and PRE0084 now use clearer author-facing wording while keeping the structured diagnostic contract intact for tooling and AI consumers.
- The batch updated proof-engine and exact-message regression coverage, synced the proof/diagnostic runtime docs, and stayed green on the targeted core path at `4914/4914`.

---

### 2026-05-12T13:52:04Z: Proof diagnostic root cause is missing same-qualifier operation metadata, not a new proof strategy gap

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-expression-qualifier-diagnostic.md`.

- Frank isolated the nested `(A - B) - C` PRE0114 failures to a catalog input gap: same-qualifier `+/-` operations lacked `Match: QualifierMatch.Same`, so intermediate `TypedBinaryOp` results did not advertise inherited qualifiers and the existing recursive proof path never activated.
- The approved fix stays metadata-driven and surgical: add `Match: Same` to the six same-qualifier money/quantity/price arithmetic operations and cover nested regressions; no provenance redesign or new proof algorithm is required.
- The message UX follow-up remains independently valuable: `ProofEngine` should describe subexpressions recursively and show resolved qualifier values instead of `<expression>` placeholders so legitimate qualifier failures explain the real mismatch.

---

### 2026-05-12T05:04:03Z: MCP hybrid rollout requires scoped reference tools

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-mcp-context-window.md`.

- Frank measured the current MCP payloads and found the legacy aggregate `precept_language` surface is about `401 KB`, far beyond a safe routine tool-result budget.
- The approved hybrid direction still stands, but Newman must add `scope` filtering to `precept_types` and `precept_domains` and keep `precept_operations` filter-first.
- The pre-ship rule is explicit: do not preserve `precept_language`, even as a hidden compatibility fallback.

---

### 2026-05-12T05:04:03Z: E2 and E3 qualifier fixes cut inventory-item PRE0114 to 16

**By:** Scribe

**Status:** Merged from George's inbox note.

**Merged source:** `george-e2-e3-complete.md`.

- George landed E2 in `8785d753` and E3 in `d3f5aa98`, then followed with `f4db093e` for the ReceiveShipment parenthesization fix.
- `samples/inventory-item.precept` PRE0114 count dropped from `66` to `16` after typed interpolated constant qualifier extraction, subexpression qualifier propagation, and compound-unit cancellation improvements.
- The remaining 16 PRE0114 diagnostics are deferred exchange-rate / GrossProfit fallout; two separate `TypeMismatch` sample edits remain outside the committed parenthesization fix.

---

### 2026-05-12T05:04:03Z: DTO-free MCP working doc is the canonical design surface

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-mcp-working-doc.md`.

- Frank wrote `docs/Working/mcp-dto-free-design.md` as the durable design record for the approved hybrid DTO-free MCP direction.
- The document explicitly records that Approach 4 (Hybrid) is approved, there are no known programmatic consumers of the current catalog JSON, and implementation may proceed.
- The architecture boundary stays intact: raw core serialization remains rejected and the public MCP surface stays curated.

---

### 2026-05-12T05:04:03Z: DTO-free MCP architecture is implementation-ready for Newman

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-mcp-impl-plan.md`.

- `docs/Working/mcp-dto-free-design.md` now carries an execution-grade implementation plan covering catalog-tool string returns, formatter extraction, `precept_compile` contract reduction, cleanup scope, and test rewrites.
- The dependency ruling is locked: no `src/Precept`, language-server, or VS Code extension work is required for this implementation pass.
- Newman can start coding from the working doc without further architecture elaboration.

---

### 2026-05-12T05:04:03Z: DTO-free MCP architecture analysis confirms hybrid curated projection

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-mcp-dto-free-design.md`.

- Frank evaluated multiple DTO-free MCP approaches under the no-codegen constraint and rejected raw core serialization plus attribute-driven converter sprawl.
- The accepted direction keeps the contract curated while removing DTO type maintenance: catalog/reference tools move toward compact rendered output and only genuinely programmatic surfaces keep minimal structured JSON.
- The core ruling remains that transport shape is a deliberate MCP contract concern, not something to leak back into `src/Precept` domain types.

---

### 2026-05-12T04:29:05Z: Inventory-Item PRE0114 Root Cause Analysis and Resolution Plan



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-inventory-item-pre0114-plan.md`.



- Frank traced the remaining `inventory-item.precept` PRE0114 failures to four root causes: shared `ParameterMeta` ambiguity, missing typed-interpolated-constant qualifier resolution, missing compound/cross-type propagation, and symbolic comparison gaps between `Dimension("{X.dimension}")` and `Unit("{X}")`.

- The plan adds Part E to `docs/Working/typed-constants-and-proof-coverage-plan.md` with slices E1–E4; the recommended execution order is E1 → E4 → E2 → E3.

- One sample bug is separate from the compiler work: `ReceiveShipment` needs inner parenthesization so the intended scalar-chain grouping survives parsing.

---

### 2026-05-12T04:29:05Z: Diagnostic Message Teachable Moment Audit



**By:** Scribe



**Status:** Merged from Elaine's inbox note.



**Merged source:** `elaine-diagnostic-audit.md`.



- Elaine ranked PRE0114 (`UnprovedDimensionRequirement`) as the worst diagnostic because the emitted `"unknown"` dimension payload and double-`in` suffix hide the real fix.

- The audit recommends author-facing rewrites for PRE0113/PRE0115 and several lower-priority proof/type messages so the Problems panel names the field, the context, and the fix direction in plain DSL terms.

- The systemic context bug is the same one called out in the proof engine: `FormatContextDescription` should stop producing `in event ... in state ...` chains.

---

### 2026-05-12T04:29:05Z: Diagnostic Message Location Tag Revision



**By:** Scribe



**Status:** Merged from Elaine's inbox note.



**Merged source:** `elaine-diagnostic-message-review.md`.



- Elaine rejected dot-path location tags like `ReceiveShipment.ensure` because authors wrote `on ReceiveShipment ensure` / `in Approved ensure`, not field-access syntax.

- The recommended tag format preserves the DSL preposition and scope name (`[on ... ensure]` / `[in ... ensure]`) so event-vs-state ensures stay distinguishable.

- Structured `Args` remain part of the contract so AI agents can reconstruct the location tag without regex-parsing the rendered message.

---

### 2026-05-12T04:29:05Z: Diagnostic Message Fixes Implemented and Validated



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-diagnostic-message-fixes.md`.



- George implemented all 10 approved diagnostic-message fixes from Elaine's audit in `commit 4535aaa6`.

- Validation stayed green on the targeted path: `818/818` tests passed.

- The batch also synced proof-context formatting, removed the hardcoded `"unknown"` payload from PRE0114, and updated the proof-engine diagnostic documentation.

- The RuleIdentity follow-up remained intentionally skipped because the runtime model still lacks an author-facing label beyond `RuleIndex`.

---

### 2026-05-12T04:00:00Z: Modifier applicability drift was confirmed as catalog gaps for `price`, `maxplaces`, and identity-type redundancy handling

**By:** Scribe

**Status:** Merged from Frank's modifier applicability audit.

**Merged source:** `frank-modifier-price-analysis.md`.

- Frank grounded the issue in catalog metadata, not checker logic: `price` bound modifiers and business-magnitude `maxplaces` were missing from `Modifiers.cs`, while the checker was faithfully enforcing incomplete metadata.
- The audit locked the design line that `price` magnitude modifiers are semantically valid, `exchangerate min/max` remain deliberately invalid, and `notempty` must stay off scalar business magnitudes.
- It also recorded the validator-shape bug on identity types: explicit `currency`/`unitofmeasure`/`dimension notempty` should degrade to redundancy-only handling.

---

### 2026-05-12T01:05:25Z: inventory-item coverage audit confirms compound-unit interpolation follow-up



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-inventory-item-coverage.md`.



- `samples/inventory-item.precept` still needs a typed-constant follow-up for compound-unit interpolation: forms like `'{StockingUnit}/{PurchaseUnit}'` and `'0 {StockingUnit}/{PurchaseUnit}'` are outside the current `unitofmeasure` and `quantity` interpolation grammars, so Slice 2 needs a Slice 2B-style extension for compound-unit patterns plus dimensional validation.

- BUG-B remains covered once interpolated typed constants and Slice 9's Unit→Dimension fallback land; the Rate × money path is already covered by existing operation/catalog work.

- BUG-A still looks like an interpolation-driven cascade rather than a separate proof defect, but explicit regressions for event-arg qualifier use in `ensure` comparisons and arithmetic expressions should ship before calling that path closed.

- Remaining sample fallout after the plan lands is design-level, not compiler-level: `SupplierUnitCost` is modeled as `money` where `price` semantics are needed, `Sku is set` still targets a non-optional field, and the average-cost calculation still needs a division-by-zero guard.

---

### 2026-05-12T01:05:25Z: Slice 11B shipped and unblocked temporal price-chain validation



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-slice11b-complete.md`.



- `price of 'time'` and `price of 'date'` now stay on the existing `of` qualifier surface: `ExtractQualifiers` routes those price qualifiers to temporal dimensions while `quantity of 'time'` remains invalid.

- The proof pipeline now carries the required temporal comparison infrastructure: `ExtractComparableValue` understands `TemporalUnit` / `TemporalDimension`, `ResolveQualifierOnAxis` falls back `Dimension → TemporalDimension`, and `TypeMeta.ImpliedQualifiers` lets `duration` contribute implied temporal-denominator metadata.

- MCP type-catalog output now serializes implied qualifiers, and George locked the implementation with 13 new Slice 11B tests. Slice 12 can proceed on top of the completed temporal denominator substrate.

- Tooling follow-up remains open for Kramer: `price of ...` completions should offer `'time'` and `'date'` alongside physical dimensions.

---

### 2026-05-12T00:50:06Z: Temporal proof-plan audit confirms Slice 11B/12 direction and closes G15 as a false gap



**By:** Scribe



**Status:** Merged from Frank's inbox notes.



**Merged sources:** `frank-plan-extension-g15.md`, `frank-spec-coverage-audit.md`, `frank-temporal-canonical-analysis.md`.



- Canonical-doc review confirmed the Slice 11B direction is additive to locked docs: temporal denominators stay on `price of ...`, `price of 'time'` / `price of 'date'` is the right extension, and `quantity of 'time'` remains invalid.

- `ImpliedQualifiers` on `TypeMeta`, temporal routing in `ExtractQualifiers`, comparable temporal values, and Dimension→TemporalDimension fallback remain the accepted infrastructure for Slice 11B.

- G15 is closed as a false gap: derivation-direction operations do not need qualifier-chain proofs because the operands share no qualifier axis, and assignment validation already enforces declared-target compatibility in the practical cases.

- The plan-status correction is durable: Slices 7–11 were confirmed already implemented; only Slice 11B and Slice 12 remain open work.

---

### 2026-05-12T00:50:06Z: Derivation operations do not infer qualifiers on resulting `price` values



**By:** Scribe



**Status:** Merged from Frank's inbox notes.



**Merged source:** `frank-q2-derivation-no-inference.md`.



- `money ÷ quantity`, `money ÷ period`, and `money ÷ duration` produce bare `price` results; the compiler does not infer denominator qualifiers from the divisor.

- Authors who need temporal-denominated derived prices must assign into fields explicitly declared with `of 'time'` or `of 'date'`.

- The rationale is now locked as D19 in `docs/language/business-domain-types.md`: qualifier inference on derivation would violate Precept's explicit, deterministic, inspectable domain-contract model.

---

### 2026-05-12T00:01:51Z: Temporal price denominators stay on `price of ...`; `quantity of 'time'` remains invalid



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-temporal-type-system-design.md`.



- Frank validated the Slice 11B direction without widening the grammar surface: temporal price denomination stays on the existing `of` preposition, so `price of 'time'` and `price of 'date'` are the additive path and no `per` keyword is introduced.

- The earlier UCUM rule remains intact: `quantity of 'time'` stays a type error even though UCUM temporal atoms remain available for compound units; authors should use `duration` or `period` for temporal semantics instead of adding `time` to `DimensionCatalog`.

- `duration` should advertise its temporal meaning through `TypeMeta.ImpliedQualifiers` so the proof/comparison pipeline can consume implied temporal-dimension metadata rather than hardcoded duration cases.

- Existing `quantity × duration` and `quantity × period` arithmetic stays valid without new proof obligations; Slice 12 chain validation should activate only for price fields that actually declare a temporal `of` qualifier.

- Open questions parked for Shane: whether `quantity in 's'` should emit a hint toward `duration`, whether `money ÷ duration -> price` should infer a temporal denominator automatically, and whether `period ×/÷ integer` should ship as a separate follow-up.

---

### 2026-05-11T22:41:49Z: string Excluded from Typed Constant Interpolation Holes



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-string-excluded-from-interpolation.md`.



- `string` stays invalid in every typed-constant interpolation hole position; compile-time rejection is the canonical behavior.

- The decision restores the prior compile-time guarantee and rejects runtime-deferral as a structural escape hatch.

- No new diagnostic code is needed because `InterpolatedTypedConstantHoleTypeMismatch` already covers the failure.

---

### 2026-05-11T22:41:49Z: TypeChecker Slices 10 + 11 Complete



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-typechecker-slices-done.md`.



- Extended `ValidateAssignmentQualifiers` to recursively extract leaf operands from binary/unary expression trees before applying the existing qualifier checks.

- Added `FromCurrency` and `ToCurrency` switch arms so exchange-rate assignments now validate both currency sides.

- Preserved the existing proof-engine boundary for bare-expression assignment gaps that still need structural provenance.

- Shipped with 10 new assignment-qualifier tests.

---

### 2026-05-11T22:41:49Z: Slice 2 Complete: Full Type-Grammar Matching for Interpolated Typed Constants



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-slice2-done.md`.



- Replaced the stubs with segment-aware matching against per-type interpolated typed-constant forms.

- Added temporal compound matching plus slot compatibility checks for magnitude, currency, unit, whole-value, and compound slots.

- Introduced diagnostics for invalid forms, unsupported types, hole-type mismatches, and dimension/unit mismatches.

- Closed with 39 new tests and 129 typed-constant tests passing.

---

### 2026-05-11T22:41:49Z: Slice 1 (Parser) Complete



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-slice1-done.md`.



- Added the `InterpolatedTypedConstantExpression` AST and rewrote parsing to emit the full segment structure.

- Updated NameBinder and TypeChecker routing so holes still bind correctly while the type checker owns slot classification.

- Added the interpolated typed-constant expression form to the catalog and moved `TypedConstantStart` to that form.

- Parser coverage landed with 10 round-trip tests.

---

### 2026-05-11T22:41:49Z: Proof Engine Qualifier Coverage — Part B (Slices 7+8+9)



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-proof-qualifier-coverage-partb.md`.



- Added `QualifierCompatibilityProofRequirement` on `QualifierAxis.Currency` to all 8 money operations so same-currency enforcement is no longer implicit-only.

- Introduced `QualifierChainProofRequirement` for cross-type qualifier validation on `ExchangeRateTimesMoney` and `PriceTimesQuantity`, with dual-axis comparison support.

- Added Unit→Dimension fallback in `ResolveQualifierOnAxis()` so dimension-only fields can satisfy unit-axis proof obligations.

- Validation landed with 19 new ProofEngine tests and 193/193 proof tests passing.

---

## Decisions

### D1: Normalization Is NOT a Catalog Concern

UCUM quantity normalization does not belong in any catalog (Functions, Operations, or otherwise). It fails the catalog-system.md § Completeness Principle test: "Is this part of a complete description of Precept?" Normalization is never author-invocable — it is implicit correctness infrastructure for cross-unit bound comparison.

### D2: No New Opcodes Required

The Builder's existing opcode inventory (`LOAD_SLOT`, `MEMBER_ACCESS`, `LOAD_LIT`, `BINARY_OP`) is sufficient for runtime bound enforcement. The Builder reads `TypedField.NormalizedDeclaredMin/Max` and embeds pre-normalized decimal values as `LOAD_LIT` operands. No `NORMALIZE_UNIT` or `SCALE_BY_FACTOR` opcode is needed.

### D3: Proof Engine and Evaluator Share Through Semantic Model, Not Catalog

The sharing seam between compile-time proof obligations and runtime constraint enforcement is `TypedField.NormalizedDeclaredMin/Max` — a semantic model property. Both the ProofEngine (which reads it for interval construction) and the Builder (which reads it for constraint plan compilation) consume the same TypeChecker-produced value. Catalog metadata is not the correct sharing mechanism for computed analysis artifacts.

### D4: No Revision to Slices 14–21

The current implementation plan requires zero changes based on this analysis. No catalog entries are added, no opcodes are added, no existing catalog entries are modified.

### D5: Future Phase 3 Runtime Convention Is Independent

Whether `PreceptValue` stores quantity magnitudes in base UCUM units (pre-normalized at write) or authored units (normalized at compare) is a Phase 3 decision that affects `FieldDescriptor` and the Builder's expression compiler — not the catalog layer.

---

## Rationale

The catalog-system.md § Architectural Identity principle ("pipeline stages read catalog metadata") governs *language knowledge* — the closed set of operations, functions, types, and constructs that define what `.precept` files can contain. UCUM normalization is not language knowledge; it is a correctness implementation detail of how existing catalog-defined operations (comparison, interval containment) handle values with attached unit metadata.

The anti-pattern the catalog principle prevents is: "pipeline stage X hardcodes language knowledge Y that should be in a catalog." Normalization has no equivalent — no pipeline stage hardcodes "which UCUM factors exist" in a way that should be metadata. The UCUM factor comes from the parsed unit string (external reference data), not from any Precept language definition.

---

## References

- `docs/Working/quantity-normalization-design.md` §0.1
- `docs/language/catalog-system.md` § Architectural Identity, § Completeness Principle
- `docs/runtime/evaluator.md` §3 Out of Scope ("The evaluator never calls Operations.GetMeta()...")
- `docs/runtime/precept-builder.md` §7 Pass 4–5

---

## Decisions

**D1:** The UCUM normalization logic (scale factor application) lives in ONE place: `Language/Numeric/TypedConstantNormalizer`. Both `Pipeline/` and `Runtime/` can call it because both depend on `Language/`.

**D2:** The "code sharing" concern is resolved by architecture, not by making the normalizer operate on `PreceptValue`. Normalization (domain math) and type-wrapping (`decimal` → `PreceptValue`) are separate concerns that compose at the Builder boundary.

**D3:** The evaluator NEVER normalizes at runtime. The Builder pre-applies normalization at deploy time by reading `TypedField.NormalizedDeclaredMin/Max` and wrapping into `LoadLit(PreceptValue)`. There is no second normalization implementation to drift.

**D4:** Option C (shared decimal-based utility in Language/) is the chosen architecture. Options A (PreceptValue-based utility), B (catalog function), and D (utility in Runtime/) are rejected for dependency direction, catalog principle, and visibility reasons respectively.

**D5:** No changes to Slices 14–21 are required. The current plan already implements Option C correctly — the missing piece was the explicit documentation of WHY it avoids duplication.

**D6:** Future Phase 3 storage convention decision (authored units vs. base units at write time) does not change this architecture. Even in the authored-units scenario, the core `ApplyFactor` math stays singular; only a thin extraction wrapper would be added.

---

## Rationale

Shane's concern presupposes two separate normalization implementations: one in the compiler (decimal-based) and one in the runtime (PreceptValue-based). This duplication is structurally impossible in the current architecture because:
1. Normalization logic exists in exactly one place.
2. The Builder reads the SAME pre-computed normalized values — it doesn't re-derive.
3. The evaluator never normalizes — it executes prebuilt comparisons on pre-normalized literals.

The `PreceptValue.FromClr(decimal)` call in the Builder is TYPE WRAPPING, not normalization logic. The domain math (UCUM scale factors) remains singular.

---

## Context

Shane argued that compile-time bound values (`max '5 kg'`) should use the same representation (`PreceptValue`) as runtime values, because:
1. `LOAD_LIT` carries `PreceptValue` (confirmed)
2. Opcode delegates are `Func<PreceptValue, PreceptValue, PreceptValue>` (confirmed)
3. The operations library works exclusively with `PreceptValue` (confirmed)

## Decisions

### D1: `TypedField.NormalizedDeclaredMin/Max` stays `decimal?`

**Rationale:** `TypedField` is a compile-time semantic model artifact. Its primary consumer is the ProofEngine (per-keystroke), which does abstract interval arithmetic (`NumericInterval` — union, intersection, containment, scaling) on `decimal` ranges. PreceptValue has no interval algebra. Making it `PreceptValue?` adds extraction cost for zero benefit.

### D2: `IntervalContainmentProofRequirement.NormalizedMin/Max` stays `decimal?`

**Rationale:** Feeds directly into `NumericInterval.Contains(min, max)` — pure decimal arithmetic. No consumer benefits from PreceptValue wrapping at this level.

### D3: `LOAD_LIT` carries `PreceptValue` (already correct, no change)

**Rationale:** `public sealed record LoadLit(PreceptValue Value) : Opcode; // literals pre-wrapped at build time`. This was already specified correctly. Shane's consistency requirement IS satisfied at the evaluator level.

### D4: The Builder is the explicit `decimal → PreceptValue` conversion boundary

**Rationale:** Builder Pass 5 reads `TypedField.NormalizedDeclaredMax` (decimal), calls `PreceptValue.FromClr(normalizedMax)`, and embeds it in `LoadLit`. After this point, bounds and runtime values are indistinguishable `PreceptValue` operands. The conversion happens once per deploy, not per-keystroke.

### D5: Design doc language must be precise about what LOAD_LIT carries

**Rationale:** The previous draft said "pre-bakes normalized `decimal` bounds into `LOAD_LIT` opcodes" — which reads as if LOAD_LIT carries raw decimal. It doesn't; it carries PreceptValue. This imprecision caused Shane's valid concern. Fixed in §0.1 Q2 and §0.2.

## Summary

Shane identified a real clarity gap, not a real type gap. The design's TYPES were correct but the EXPLANATION was misleading. Representational consistency IS satisfied at the evaluator level (both bounds and runtime values are PreceptValue in LoadLit opcodes). The semantic model correctly uses `decimal?` because the ProofEngine's abstract interval arithmetic has no PreceptValue analogue.

## Affected Artifacts

- `docs/Working/quantity-normalization-design.md` — §0 Revised Key Types (clarification added), §0.1 Q2 (rewritten), §0.2 (new section)
- No type changes to Slices 14–21
- No changes to `TypedField`, `IntervalContainmentProofRequirement`, or Builder design
