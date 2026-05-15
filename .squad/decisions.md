# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

---

### 2026-05-15T02:32:44Z: Affine quantity normalization now carries UCUM pre-offsets for temperature units

**By:** Scribe

**Status:** Merged from Frank's affine conversion design note.

**Merged source:** `frank-affine-conversion-design.md`.

- Quantity normalization now has an affine design path for temperature units: `Cel`, `[degF]`, and `[degRe]` convert with `base = (value + offset) × scale`, while linear units keep `AffineOffset = null` and preserve current behavior.
- Root cause is explicit: `StripFunctionWrapper` preserves UCUM multiplicative factors but discards the function-name offset encoding, so `UcumAtom` / `UcumParsedUnit` must carry `AffineOffset` metadata into normalization.
- The implementation plan upgrades `TryGetStaticScalingFactor` into affine conversion metadata, adds `NumericInterval.Shift(decimal)`, updates typed-constant normalization and proof intervals, and records 24 focused tests in `docs/working/quantity-normalization-design.md` §6.8.
- Logarithmic units (dB, pH, Np) stay out of scope by design, and the temperature work remains orthogonal to PRE0137's counting-unit comparison rule.

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

---

### 2026-05-15T01:52:56Z: Counting-unit wording now separates dimension-family compatibility from value conversion, and binary-op proof fallback is a real gap

**By:** Scribe

**Status:** Merged from Frank's counting-unit compatibility note.

**Merged source:** `frank-counting-unit-compatibility.md`.

- The research note is corrected: `count` / `DimensionVector.None` only identifies the shared dimension family for business counting units; it does not imply semantic convertibility between `each`, `box`, `case`, or other explicit units.
- Two inaccurate statements in `business-units-quantity-normalization-survey.md` must be read as wording errors, not as approved semantics: dimension-alias membership is real, but cross-unit value conversion is not.
- Assignment validation already rejects explicit counting-unit mismatches (`quantity in 'each'` cannot accept `quantity in 'box'`), so documentation must not describe the looser proof path as intended compatibility.
- Frank surfaced the architectural gap: `ProofEngine.QualifiersAreCompatible` currently lets same-dimension counting units satisfy binary-op qualifier proofs via the `count` fallback, so static comparisons like `each` vs `box` can prove when they should degrade to `Unbounded` or be rejected until a real conversion model exists.

---

---

### 2026-05-15T02:26:33Z: Cross-counting-unit binary operations now have a concrete enforcement plan

**By:** Scribe

**Status:** Merged from Frank's solution design; duplicate gap note folded into the existing 2026-05-15T01:52:56Z gap record.

**Merged sources:** `frank-cross-unit-comparison-solution.md`; `frank-cross-unit-comparison-gap.md` (deduplicated).

- `ValidateQualifierCompatibility` must enforce qualifier checks for `OperatorFamily.Comparison` as well as arithmetic, closing the current PRE0070/PRE0071 hole for cross-currency and cross-dimension comparisons.
- Add **PRE0137 `CrossCountingUnitOperation`** for static quantity binary operations where both qualifiers sit in the `"count"` dimension but their explicit unit codes differ (`each` vs `box`, `case`, `pallet`, etc.).
- Preserve the current same-dimension allowance for physically convertible UCUM units (`kg`, `g`, `[lb_av]`); the stricter rule is specific to business counting units where no universal factor exists.
- `docs/working/quantity-normalization-design.md` §6.7 is now the durable design surface for the gap and its implementation-ready fix.

---

### 2026-05-15T02:37:53Z: Function-call qualifier enforcement is missing for same-match counting-unit operations

**By:** Scribe

**Status:** Recorded from Frank's comprehensive cross-counting-unit analysis.

**Merged source:** `frank-14` spawn manifest (no inbox file present at merge time).

- Frank's exhaustive 16-category audit confirmed the current §6.7 fix plan only closes binary operators; function calls were an uncovered enforcement surface.
- **Critical Gap C:** the Functions catalog already declares `QualifierMatch.Same` on quantity/money overloads for `min`, `max`, `clamp`, and `abs`, but `SelectOverload` never reads `FunctionOverload.Match`, so calls like `max(qty_each, qty_box)` currently resolve without qualifier diagnostics.
- The implementation-ready fix is `ValidateFunctionQualifierCompatibility` immediately after overload selection, reusing existing catalog metadata instead of adding parallel rules; PRE0137 should cover both binary operators and these same-match function calls.
- Gap D remains explicitly deferred: `in` / `not in` membership still routes through `CreateSyntheticBinaryOp` and skips `ValidateQualifierCompatibility`.
- `docs/working/quantity-normalization-design.md` §6.7.9–§6.7.11 is now the durable design surface for the full operator/function taxonomy, the critical function-call hole, and the deferred membership follow-up.

---

### 2026-05-15T03:43:11Z: First-wave quantity-normalization implementation landed on `spike/Precept-V2-Radical`

**By:** Scribe

**Status:** Merged from George's first-wave implementation note.

**Merged source:** `george-slice-14-43-22-30-32-34.md`.

- Slices 43, 14, 34, 22, 30, and 32 are committed on `spike/Precept-V2-Radical`; per-slice `dotnet build src/Precept/Precept.csproj` passed, while full `dotnet test` still carries pre-existing branch failures outside this slice set.
- The landed surface is broader than the original slice labels: `TypedInterpolatedTypedConstant` was renamed to `InterpolatedTypedConstant` across runtime, tests, and language-server consumers, and the branch now carries `TypedConstantNormalizer`, `NumericInterval.Scale(decimal)`, `AffineOffset` on UCUM atom/unit metadata, and static qualifier metadata on interpolated typed constants.
- Qualifier enforcement now covers comparison operators and same-match function calls; because `min`/`max` parse as constraint keywords in this branch's grammar context, regression coverage used `clamp` and `abs` to exercise the same `SelectOverload` enforcement path.
- PRE0137 / `CrossCountingUnitOperation` is now wired for explicit counting-unit mismatches in same-match quantity function calls as part of Slice 32.

---

### 2026-05-15T03:43:11Z: Business counting-unit docs still need a post-annotation correction

**By:** Scribe

**Status:** Merged from Frank's docs-gap note.

**Merged source:** `frank-doc-gaps.md`.

- Frank's Slices 38–42 documentation annotations are committed, but `docs/language/business-domain-types.md:373` still describes business counting units such as `each`, `case`, `pack`, and `dozen` as opaque with no shared dimension.
- The durable architecture remains the opposite: business counting units intentionally share `DimensionVector.None` with factor-one representation, and PRE0137 enforces explicit unit-code identity inside that family.
- Treat the `business-domain-types.md` wording as a follow-up docs correction outside the completed annotation batch.

---

### 2026-05-15T03:43:11Z: Quantity-normalization skeleton tests landed with honest red pressure

**By:** Scribe

**Status:** Merged from Soup Nazi's test-status note.

**Merged source:** `soup-nazi-test-coverage.md`.

- Commit `58e498fa` added quantity-normalization skeleton coverage for Slices 17, 21, and 37 across type-checker, proof, interval, and normalizer surfaces.
- `dotnet build test\\Precept.Tests\\Precept.Tests.csproj` passed, and the targeted 31-test verification run finished `19 passed / 12 failed`.
- The remaining reds are explicit contract pressure tied to unimplemented affine metadata/offset behavior, raw-magnitude compare paths, interpolated interval extraction plus static-unit scaling, denominator normalization for cross-unit price comparisons, and the intended cross-dimension diagnostic surface.
- The green portion of the batch already locks shift coverage, same-unit anchors, cross-temperature proof anchors that match current behavior, and the money-overflow regression.

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

---
