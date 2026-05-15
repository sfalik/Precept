# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

---

---

### 2026-05-15T22:09:58Z: Deferred qualifier follow-up items are fully closed and the PRE0141 architecture stays approved

**By:** Scribe

**Status:** Merged from Frank's scoped follow-up plan, George's implementation closeout, Soup Nazi's quantity regression pass, and Frank's earlier review approval.

**Merged sources:** rank-qualifier-deferred-scoping.md; george-qualifier-deferred-fixes.md; soup-nazi-quantity-gap-tests.md; rank-qualifier-enforcement-review.md.

- Frank's three deferred items are now the shipped rule set: preserve QualifierMatch.Same function-call qualifiers, restore TypeChecker implied-qualifier parity, and make quantity-slot Unknown vs Absent explicit instead of accidental.
- George implemented the full closure: TypedFunctionCall now carries nullable ResultQualifiers consumed by both assignment validation and the proof engine, ResolveDirectQualifierAxis(...) consults implied qualifiers, and shared QualifierUnitHelpers now own compound-unit splitting across checker and proof paths.
- Soup Nazi's eight quantity tests confirmed the quantity expression lane was already green across bare refs, whole-value interpolation, unit slots, binary results, and conditionals, so the quantity-slot change is a contract-hardening clarification rather than a newly-opened lane.
- Frank's review still stands: the axis-aware Resolved / Unknown / Absent assignment model, PRE0141 split, and regression matrix are architecturally approved with no blocking findings remaining.
- Validation closed at 55/55 focused assignment tests and the unchanged 5642 passed / 9 failed / 5651 total branch baseline.

---

### 2026-05-15T22:09:58Z: Initial events must not hide uninitialized required-field reads behind assignment presence checks

**By:** Scribe

**Status:** Merged from Frank's diagnostic-gap ruling.

**Merged source:** rank-count-uninitialized-read.md.

- Frank confirmed a genuine lifecycle-validation gap: set count = count + 1 inside an initial event for a required field with no default currently compiles with zero diagnostics even though the right-hand side reads an undefined value.
- The gap has two distinct parts: ValidateConstructionGuarantees currently returns early for stateless precepts before checking required-field assignment coverage, and the existing D94 lane only proves that a target field is assigned somewhere, not that its RHS avoids self-reading an undefined value.
- The recommended fix is split, not overloaded: extend stateless initial-event construction validation inside TypeChecker.Validation.FieldState.cs, and add a new dedicated diagnostic (UninitializedFieldReadInInitialAssignment, next slot 142) for self-referential initial assignments that read their own target before any prior value exists.
- Durable rule: construction guarantees must validate both assignment presence and use-before-definition safety on the initial event path.

---

### 2026-05-15T22:09:58Z: Completion routing is now gated by real cursor position instead of fallback top-level leakage

**By:** Scribe

**Status:** Merged from Frank's completion-position specification and Kramer's implementation audit.

**Merged sources:** rank-completion-position-spec.md; kramer-completion-audit.md.

- Frank locked the governing completion rule: top-level construct keywords only belong in SlotContext.TopLevel at the start of a line (after optional whitespace) and must be explicitly suppressed in expression, modifier, target, and post-keyword positions.
- Kramer closed the shipped leak by threading raw document text into completion handling, gating top-level construct completions behind a whitespace-only line-prefix check, and correcting the 	ransition routing path so state targets no longer fall back to construct keywords.
- The audit also broadened the accurate downstream surfaces: post-type declaration completions now include qualifier prepositions and computed-field <-, event arg declarations now receive value modifiers instead of event-only modifiers, valued modifiers route into expression completions, and operator vocabulary is now offered from the catalog-driven expression lane.
- Durable rule: completion item sets stay catalog-derived, but fallback context routing must fail closed instead of defaulting mid-construct positions to TopLevel.

---

### 2026-05-15T20:40:13Z: Assignment qualifier enforcement now rejects unproved constrained axes with PRE0141

**By:** Scribe

**Status:** Merged from Frank's architectural ruling, George's replacement implementation, and Soup Nazi's regression matrix.

**Merged sources:** `frank-price-qualifier-full-analysis.md`; `george-qualifier-enforcement-arch.md`; `soup-nazi-qualifier-enforcement-tests.md`.

- Frank locked the governing rule: every qualifier axis constrained by the target must be provably compatible at compile time; unknown is only acceptable on unconstrained axes.
- George replaced the old `TryGetAssignmentSourceQualifiers(...)` seam with axis-aware assignment resolution (`Resolved` / `Unknown` / `Absent`), introduced `PRE0141 UnprovedAssignmentQualifierCompatibility`, and kept definite mismatches on `PRE0068` / `PRE0069`.
- The new resolver covers direct refs, typed constants, whole-value interpolation, qualifier slots, conditionals, and binary-derived results across `price`, `money`, `quantity`, and `exchangerate`.
- Soup Nazi's 19-test matrix is now the durable assignment-enforcement surface across `set`, field default, and event-arg default lanes; George validated the change against the unchanged 9-failure branch baseline plus analyzer and language-server suites.

---

---

### 2026-05-15T20:40:13Z: Plain `price` typed constants now flow through the existing qualifier-mismatch enforcement path

**By:** Scribe

**Status:** Merged from Frank's gap analysis, George's set-assignment fix, and Soup Nazi's price-literal regression tests.

**Merged sources:** `frank-price-qualifier-enforcement-gap.md`; `george-price-qualifier-enforcement.md`; `soup-nazi-price-qualifier-tests.md`.

- Frank's diagnosis stands: the bug was an assignment-source extraction defect, not a language-shape defect, because the resolved `TypedTypedConstant` already carried the needed qualifier metadata.
- George fixed the seam by trusting non-empty `TypedTypedConstant.DeclaredQualifiers` instead of re-deriving a money-only special case, so plain `price` literals now participate in `ValidateResolvedQualifiers(...)`.
- The fix reuses `PRE0068 QualifierMismatch`; no new price-only diagnostic was introduced.
- Soup Nazi's set-action and field-default anchors lock count-unit mismatch, matching control, and currency mismatch behavior for the shared assignment validation path.

---

---

### 2026-05-15T20:40:13Z: Interpolated `.unit` price sources now surface slot-backed qualifiers during assignment validation

**By:** Scribe

**Status:** Merged from Frank's interpolated-gap analysis and George's interpolated-slot fix.

**Merged sources:** `frank-interpolated-unit-qualifier-gap.md`; `george-interpolated-unit-qualifier-fix.md`.

- Frank established that `'4.17 USD/{field.unit}'` resolves as an `InterpolatedTypedConstant` with a unit slot under `TypedMemberAccess`, so the qualifier fact already existed at compile time but never reached assignment validation.
- George added the slot-backed interpolated extraction path, preserved static currency text for mixed static/dynamic price literals, and reused the same reach-through helper shape as `ValidateUnitSlotDimensionConsistency(...)`.
- The resulting behavior stays on the existing `PRE0068 QualifierMismatch` lane: interpolated count-unit mismatch and interpolated currency mismatch now fail, while the matching control still passes.

---

---

### 2026-05-15T20:40:13Z: `price` qualifier-shape evolution is recorded as a metadata-first proposal

**By:** Scribe

**Status:** Merged from Frank's qualifier-shape proposal; still awaiting owner review.

**Merged source:** `frank-price-qualifier-shape-analysis.md`.

- Frank recorded three model gaps in the current `price` qualifier shape: `in` cannot resolve as unit-or-currency, `in 'USD/each'` cannot fill currency and unit together, and `in`+`of` coexistence cannot be made conditional on currency-only `in`.
- The proposal keeps the fix in metadata: add `QualifierAxis.PriceIn`, add `DeclaredQualifierMeta.CompoundPrice`, and add `QualifierShape.OfRequiresCurrencyIn` so downstream consumers derive the behavior instead of hardcoding price-specific branches.
- The proposal is preserved as design-state only. The shipped assignment-enforcement fixes above do not depend on owner approval of this separate qualifier-shape evolution.

---

---

### 2026-05-15T19:30:00Z: Slice 26 warnings are closed and the slice is fully approved

**By:** Scribe

**Status:** Merged from Frank's warning-closure verification.

**Merged source:** `frank-slice-26-warnings-check.md`.

- Frank verified the arg-default diagnostic context now formats as `arg 'Event.Arg'` through `ArgDefaultContext`, closing the user-facing `"here"` fallback on overflow diagnostics.
- The new `EventArgDefault_QualifierMismatch_EmitsDiagnostic` test exercises the live `ValidateAssignmentQualifiers` arg-default path with `money in 'USD' default '50 EUR'`.
- Focused `TypeCheckerEventArgDefaultTests` now pass 8/8, Slice 26 is fully closed, and Slice 27 is unblocked.

---

---

### 2026-05-15T18:30:00Z: Slice 26 review approved the design but required two pre-doc-sync fixes

**By:** Scribe

**Status:** Merged from Frank's Slice 26 review.

**Merged source:** `frank-slice-26-review.md`.

- Frank approved the core Slice 26 shape: `ResolveEventArgExpressions`, arg-default obligation collection, `ValidateMaxPlaces` delegation, and the cross-unit scaling path were all structurally correct with 7 new tests green.
- W1 required an `ArgDefaultContext` formatter branch so arg-default overflow diagnostics stop falling back to `"here"` instead of naming the owning event arg.
- W2 required direct qualifier-mismatch coverage on the arg-default `ValidateAssignmentQualifiers` path; the standing 9-suite failures remained explicitly pre-existing and non-blocking once the warnings were closed.

---

---

### 2026-05-15T18:01:02Z: Mandatory commas between field modifiers are rejected

**By:** Scribe

**Status:** Merged from Frank's feasibility assessment.

**Merged source:** `frank-modifier-comma-assessment.md`.

- No parser ambiguity or current readability cliff justifies punctuation here: modifier keywords are self-delimiting and no sample in the corpus exceeds four modifiers on one field.
- Commas already carry list semantics elsewhere in the DSL, so reusing them between modifiers would blur an existing structural cue instead of clarifying the grammar.
- If modifier readability ever degrades, the preferred intervention is formatter-enforced line breaking rather than grammar-level comma requirements.

---

---

### 2026-05-15T11:37:42Z: Slice 21 interpolated quantity proof coverage is fully green

**By:** Scribe

**Status:** Merged from Soup Nazi's coverage report.

**Merged source:** `soup-nazi-s21-coverage.md`.

- All 10 tests in `TypeCheckerInterpolatedQuantityTests` pass against the unchanged 9-failure branch baseline, covering magnitude-slot scaling, WholeValue recursion, conservative dynamic-unit and dynamic-price fallback, and money magnitude handling.
- The report explicitly confirms `HasSingleMagnitudeSlot` prevents double-normalization on WholeValue paths, while noting the current happy-path bound is not itself a dedicated false-safety detector.
- Recommended future hardening is a tighter-bound regression if that guard ever changes; no new regressions were introduced in the current slice coverage pass.

---

---

### 2026-05-15T18:00:00Z: Slice 25 warning closure is verified and the slice is fully closed

**By:** Scribe

**Status:** Merged from Frank's warning-closure verification.

**Merged source:** `frank-slice-25-warnings-check.md`.

- Frank verified W1, W2, and W3 are all actually closed on `spike/Precept-V2-Radical`, not just papered over.
- The new coverage now directly exercises `FoldValue`'s static-magnitude interpolated-default path, documents and tests the forward-reference ordering limitation in `CheckInitialStateSatisfiability`, and restores the formatting break in `CollectDefaultObligations`.
- All 8 `ProofEngineFieldDefaultTests` pass, the full suite stays at the same 9 pre-existing failures, and Slice 26 is unblocked with Slice 25 marked fully closed.

---

---

### 2026-05-15T17:30:00Z: Slice 25 approval locks the field-default proof path for interpolated typed constants

**By:** Scribe

**Status:** Merged from Frank's Slice 25 review.

**Merged source:** `frank-slice-25-review.md`.

- Frank approved the Slice 25 design split: `CollectDefaultObligations` owns interval-containment obligation generation, while `FoldValue` Part A handles ensures-time folding of interpolated defaults.
- Cross-unit and affine behavior are locked to the canonical normalization seams (`IntervalOf`, `ApplyStaticUnitScaling`, and the same affine formula as `TypedConstantNormalizer`) rather than duplicating scaling logic inline.
- Frank called out three non-blocking follow-ups at review time: add direct coverage for the `FoldValue` static-magnitude path, document/test forward-reference ordering sensitivity in `CheckInitialStateSatisfiability`, and clean up a small formatting artifact near `FormatViolationReason`.

---

---

### 2026-05-15T17:00:00Z: Slice 23 and Slice 24 warning closures are approved and do not block forward progress

**By:** Scribe

**Status:** Merged from Frank's S23/S24 warning-closure review.

**Merged source:** `frank-s23s24-warnings-review.md`.

- Frank approved the warning-closure tests for the Slice 23 qualifier-routing path and the Slice 24 money/price interpolated-interval paths, with the suite still holding at 5545 passing and 9 pre-existing failures.
- The review locks the two-axis price mismatch regression through `BuildQualifiersFromStaticInterpolated → ValidateResolvedQualifiers`, confirms WholeValue money/price interpolations stay on the correct non-scaling path, and confirms the same-unit price magnitude regression stays proved without accidental inverse scaling.
- One remaining note is explicitly non-blocking: a same-unit test comment should describe the interval as `[decimal.MinValue..3]`, not `[0..3]`.

---

---

### 2026-05-15T16:25:03Z: Frank review warnings now gate forward progress the same way blockers do

**By:** Scribe

**Status:** Merged from Shane's follow-up user directive.

**Merged source:** `copilot-directive-2026-05-15T12-25-03.md`.

- After Frank reviews a slice, every finding must be closed before the team moves on: warnings (`W{N}`) are proceed/no-proceed gates, not optional cleanup.
- This tightens the standing review loop without changing runtime behavior: approval-closeout now means doc sync plus full review-findings closure, not just blocker resolution.

---

---

### 2026-05-15T16:15:38Z: Slice 24 approval closes the money/price interpolated-interval lane

**By:** Scribe

**Status:** Merged from Frank's approval and George's implementation note.

**Merged sources:** `frank-s24-review.md`; `george-s24-money-price-intervals.md`.

- Frank approved Slice 24: the money magnitude, money WholeValue, price+static-denominator magnitude, and price-dynamic → `Unbounded` paths all match the §5.6 design intent, with `ApplyStaticUnitScaling` correctly handling the inverse-factor price case.
- George confirmed the Slice 19 single-slot path was already generic enough for money and most price inputs; the true Slice 24 delta was making the Price+Magnitude+no-static-qualifier fallback explicitly return `Unbounded`, documenting the per-type responsibilities, and adding the static-denominator price regression tests.
- Remaining debt is non-blocking: dedicated money/price WholeValue anchors and a same-unit price regression would strengthen coverage, but Frank cleared the slice and the doc tracker should now show Slice 24 complete.

---

---

### 2026-05-15T16:15:38Z: Slice 23 locked the static-qualifier routing contract for interpolated typed constants

**By:** Scribe

**Status:** Merged from George's qualifier-routing implementation note and Frank's approval.

**Merged sources:** `george-s23-qualifier-routing.md`; `frank-s23-review.md`.

- `ResolveQualifierFromInterpolatedConstant` now consults `StaticQualifier` per axis before falling back to slot-based logic, eliminating false-positive `PRE0114` cases when an interpolated qualifier is already statically known.
- `TryGetAssignmentSourceQualifiers` now projects interpolated `StaticQualifier` metadata into assignment checking so `set`/`default` can emit `PRE0134` for definite mismatches instead of silently accepting them.
- George locked the guardrails too: `WholeValue` forms never populate `StaticQualifier`, axis-specific early returns never hide dynamic holes, and the fallback stays conservative.
- Frank approved the slice on `c643bc04`: the subtype-to-qualifier mapping is complete, PRE0134 mismatch emission is correct, and the only targeted-suite failure remained the pre-existing `CompoundUnitPositivityProof_ClearsDivisionByZero` baseline.
- Slice 25 is now unblocked by dependency, while one non-blocking warning remains: add a price/exchangerate mismatch test that exercises simultaneous currency+unit disagreement through the shared `ValidateResolvedQualifiers` path.

---

---

### 2026-05-15T16:13:52Z: Doc tracker maintenance is now part of the approval-closeout loop

**By:** Scribe

**Status:** Merged from Shane's user directive.

**Merged source:** `copilot-directive-2026-05-15T12-13-52.md`.

- After Frank approves a slice, the Scribe closeout pass must update `docs/working/quantity-normalization-design.md` so the tracker reflects the actual implementation state in the same batch.
- This is a process-memory decision, not a language/runtime change: the canonical doc tracker now moves in lockstep with review approval instead of waiting for a later cleanup pass.

---

---

### 2026-05-15T15:37:42Z: Slice 21 coverage is approved and locked as the interpolated-quantity proof baseline

**By:** Scribe

**Status:** Merged from Frank's review and Soup Nazi's coverage report.

**Merged sources:** `frank-s21-review.md`; `soup-nazi-s21-coverage.md`.

- Frank approved Slice 21: all 10 interpolated quantity overflow tests cover the required §5.3/G21 shapes, and the conservative lanes correctly keep unbounded or dynamic-unit inputs on the `NumericOverflow` path instead of producing false proofs.
- Soup Nazi confirmed the full test slate is green on the branch baseline: the suite locks magnitude-slot scaling, WholeValue recursion, dynamic-unit conservatism, dynamic price-denominator conservatism, money magnitude handling, and the cross-unit WholeValue happy path.
- Two follow-up warnings stay as explicit debt, not blockers: add a direct `ProofDisposition != Proved` assertion for conservative cases, and add the both-holes dynamic-unit form (`'{intField} {unitField}'`) as a sharper regression anchor.

---

---

### 2026-05-15T14:55:25Z: Slice N and M review loop closed with B3 fix and final approval

**By:** Scribe

**Status:** Merged from Frank's review loop and George's repair notes.

**Merged sources:** `frank-nm-review.md`; `george-nm-fix.md`; `frank-nm-rereview.md`; `george-nm-b3-fix.md`; `frank-nm-final.md`.

- Frank's first review blocked Slice N on B1/B2: `AllowsBareNumericQuantityBound` was suppressing PRE0018 too broadly, and the regression suite lacked bare-numeric negative tests for dimension-only and unqualified quantity bounds.
- George's `0837ad6f` fix narrowed the suppression to the intended qualifier lane, added both missing negative tests, and removed the duplicate bare-numeric helper implementation.
- Frank's re-review found the B3 follow-on: count-dimension fields (`quantity of 'count'`) also need PRE0018 suppression so the dedicated PRE0138 path remains the only diagnostic.
- George's `70ee2406` repair reused the existing `IsCountDimension` helper instead of duplicating count-dimension detection, restored the Slice M contract, and left all 14 qualifier-compatibility tests green.
- Frank's final verdict is APPROVED: Slices N and M are fully closed with no new regressions beyond the branch baseline.

---

---

### 2026-05-15T14:55:25Z: Wave 2 warnings W1 and W2 are closed without new regressions

**By:** Scribe

**Status:** Merged from Frank's Wave 2 review and George's follow-up fix note.

**Merged sources:** `frank-wave2-review.md`; `george-w1w2-fix.md`.

- Frank's Wave 2 review approved Slices 15, 15b, 16, 19, 20, 31, 33, 35, 36, and 37 with two follow-up warnings: preserve authored `DeclaredMin/Max` values alongside normalized storage, and guard `NormalizePrice` against affine-offset denominator units.
- George confirmed the authored-vs-normalized storage split was already present on the current branch for both `TypedField` and `TypedArg`, then added regression coverage so that invariant stays locked.
- `01f255ab` also hardened `NormalizePrice` by routing through static affine metadata and throwing for affine-offset denominators instead of silently applying scale-only math.
- Focused tests passed, and George reported that the remaining full-suite failures are unrelated pre-existing branch issues.

---

---

### 2026-05-15T14:35:23Z: Bounds-validation documentation lane is locked as Slices 44 and 45

**By:** Scribe

**Status:** Merged from Frank's slice-numbering note.

**Merged source:** `frank-slices-nm-doc.md`.

- The standalone bounds-validation follow-ups are now numbered as Slice 44 (bare-integer bound promotion) and Slice 45 (PRE0138 CountDimensionBoundsAmbiguous).
- Frank placed both in a dedicated **Bounds** lane in the §5.7 summary table so they stay distinct from the cross-unit operation-enforcement slices.
- This is a documentation-only decision: no runtime, checker, or tooling behavior changed in the numbering pass itself.

---

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

---

### 2026-05-15T03:43:11Z: Business counting-unit docs still need a post-annotation correction

**By:** Scribe

**Status:** Merged from Frank's docs-gap note.

**Merged source:** `frank-doc-gaps.md`.

- Frank's Slices 38–42 documentation annotations are committed, but `docs/language/business-domain-types.md:373` still describes business counting units such as `each`, `case`, `pack`, and `dozen` as opaque with no shared dimension.
- The durable architecture remains the opposite: business counting units intentionally share `DimensionVector.None` with factor-one representation, and PRE0137 enforces explicit unit-code identity inside that family.
- Treat the `business-domain-types.md` wording as a follow-up docs correction outside the completed annotation batch.

---

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

---

### 2026-05-15: Count/unit bound gap investigation
**By:** Frank (investigation)
**Requested by:** Shane
**Summary:** The type checker emits spurious PRE0018+PRE0133 on `quantity in 'unit' max <integer>` even though the proof engine already correctly infers the field's unit for that bound (test4 proves clean at `[3..3] ⊆ [−∞..4]`). Separately, `quantity of 'count' max <integer>` deserves a count-specific diagnostic because the dimension-only qualifier is ambiguous for bounds—counting units are not mutually convertible.

---

**Finding 1 — `quantity in 'unit' max <integer>` should be CLEAN:**

Root cause is two independent check sites that don't account for unit inheritance from the field declaration:

1. **PRE0018 (TypeMismatch)** — `TypeChecker.cs:649-659`. The max modifier value `4` resolves as `TypeKind.Integer` via `Resolve()`. `IsAssignable(Integer, Quantity)` returns false → emits "Expected a quantity value here, but got 'integer'". The checker has no promotion path that says "bare integer on a quantity field with explicit `in 'unit'` qualifier → treat as quantity in that unit."

2. **PRE0133 (BoundsRequireQualifier)** — `TypeChecker.Validation.Modifiers.cs:209-225`. `TryGetComparableModifierValue` returns `ExtractedBoundValue(4, Empty)` for a NumberLiteral (line 397-398 unconditionally returns empty qualifiers). `ValidateBoundQualifierCompatibility` then sees the bound has no qualifiers on a field that requires them → fires PRE0133.

**The proof engine already handles this correctly.** The compile output shows `test4` (quantity in 'box' max 4) with `declaredMax: 4` and `computedInterval: [3..3]` → disposition: **Proved**. The proof engine's `ExtractFieldInterval` path reads the declared max as a bare numeric magnitude and the field's unit qualifier provides dimensional context. The type checker is behind the proof engine here.

**Verdict:** This is a BUG. `quantity in 'unit' max <integer>` is semantically valid — the integer bound inherits the unit from the field's `in` qualifier. Both PRE0018 and PRE0133 are false positives in this case.

---

**Finding 2 — `quantity of 'count'` diagnostic:**

There is NO existing count-specific diagnostic for "dimension-only qualifier on a count field with bounds." The relevant codes are:
- PRE0133 (`BoundsRequireQualifier`) — generic, fires for any missing qualifier; doesn't explain the count-dimension ambiguity
- PRE0137 (`CrossCountingUnitOperation`) — for binary/function operations combining different counting units; NOT for declaration-level qualification gaps

**Why `of 'count' max 4` is genuinely ambiguous:** The `of 'dimension'` qualifier constrains the dimension family but does NOT pin a specific unit. For physically convertible dimensions (mass, length), this is fine because any unit in the dimension can be normalized via UCUM factors. For the **count dimension**, units (each, box, case, pallet) share `DimensionVector.None` but have factor-1 atoms with NO conversion relationship. A bare integer bound on `of 'count'` is ambiguous: 4 of WHAT? The comparison is meaningless without a unit code.

**Needed:** A new diagnostic or a specialization of PRE0133's message path that fires ONLY when:
- Field type is `quantity`
- Qualifier is dimension-only (`of 'count'` → `DeclaredQualifierMeta.Dimension` with `DimensionName == "count"`)
- Bound modifier (min/max) is present

Suggested code: **PRE0138** `CountDimensionBoundsAmbiguous`
Message: "Bounds on count-dimension fields require an explicit unit qualifier ('in box', 'in each') because counting units are not mutually convertible."
Severity: Error

---

**Existing coverage:**

- **Slice 15** (TypeChecker bounds extraction / `NormalizedDeclaredMin/Max`): Addresses how normalized bounds are stored on `TypedField` for proof consumption. Does NOT address suppression of PRE0018/PRE0133 for bare-integer bounds with `in 'unit'` fields.
- **Slices 30-34** (qualifier gap enforcement for operators/functions): Cover PRE0137 at expression evaluation time, not at declaration time.
- **No existing slice** covers either of these two cases.

---

**Proposed fix — NEW slice (pre-normalization, can land independently):**

**Slice N: Bare-integer bound promotion for unit-qualified quantity fields**

Scope:
1. `TypeChecker.cs` ~line 620/649 (min/max bound resolution): When `resolvedType == TypeKind.Quantity` and `typedField.DeclaredQualifiers` contains a `DeclaredQualifierMeta.Unit`, suppress the `IsAssignable` check for integer→quantity. The bare integer is valid because the unit is inherited.
2. `TypeChecker.Validation.Modifiers.cs` `TryGetComparableModifierValue` (line 396-398): When the field carries a `Unit` qualifier and the expression is a `NumberLiteral`, synthesize the field's unit qualifier onto the `ExtractedBoundValue` instead of returning `Empty`. This prevents PRE0133 from firing.
3. Alternatively, `ValidateBoundQualifierCompatibility` (line 214-225): Skip the `BoundsRequireQualifier` emission when the field has an explicit `Unit` qualifier and the bound is a bare numeric — the unit is unambiguous by inheritance.

**Slice M: Count-dimension bounds ambiguity diagnostic (PRE0138)**

Scope:
1. `DiagnosticCode.cs`: Add `CountDimensionBoundsAmbiguous = 138`
2. `Diagnostics.cs`: Template: "Bounds on count-dimension fields require an explicit unit qualifier ('in box', 'in each') because counting units are not mutually convertible."
3. `TypeChecker.Validation.Modifiers.cs` `ValidateBoundQualifierRequirements` (or a new validation pass): When field has `DeclaredQualifierMeta.Dimension` with dimension name resolving to the count family (`DimensionVector.None`), AND has min/max bounds, emit PRE0138 instead of (or in addition to) PRE0133. PRE0138 gives the author actionable guidance: change `of 'count'` to `in 'box'` (or whatever specific unit).

---

**New diagnostic needed:** Yes.
- **Code:** PRE0138 `CountDimensionBoundsAmbiguous`
- **Fires when:** A quantity field is declared with a count-dimension-only qualifier (`of 'count'`) and has min/max bounds. The dimension qualifier doesn't pin a unit, and counting units are not convertible, making the bound comparison ambiguous.
- **Message:** "Bounds on count-dimension fields require an explicit unit qualifier ('in box', 'in each') because counting units are not mutually convertible. Use 'in <unit>' instead of 'of count'."
- **Recovery:** Change `quantity of 'count' max 4` → `quantity in 'box' max 4` (or whatever the intended unit is).


---

# George Wave 2A Gaps / Follow-ups

Timestamp: 2026-05-15T07:59:53.548-04:00
Slug: normalization-and-qualifiers

## Open gaps observed while implementing slices

- Affine temperature normalization remains incomplete in TypedConstantNormalizer.NormalizeQuantity; existing red tests in TypedConstantNormalizerTests and ProofEngineIntervalIntegrationTests still show Celsius/Fahrenheit comparison drift.
- NumericInterval.Shift(decimal) slice tests remain red in baseline and are outside this wave's scope.
- Membership qualifier enforcement currently focuses on static qualifiers; dynamic qualifier forms are intentionally deferred.


---

# George Wave 2A Notes

Timestamp: 2026-05-15T07:59:53.548-04:00
Branch: spike/Precept-V2-Radical

## Decisions captured

1. Store normalized numeric bounds at type-check extraction time for both fields and args; keep raw DeclaredMin/DeclaredMax populated in parallel to avoid breaking existing consumers while enabling normalized-first reads via NormalizedDeclaredMin/NormalizedDeclaredMax.
2. In proof interval computation, apply static-unit scaling only for raw-magnitude expression forms (TypedTypedConstant, InterpolatedTypedConstant with a single Magnitude slot and static unit qualifier).
3. Dynamic-unit interpolated forms ('{n} {u}', '10 USD/{u}') do not produce trusted static numeric facts; TryGetStaticNumericValue now declines these to avoid false proofs.
4. contains synthetic operations now run through the binary qualifier compatibility seam so PRE0137/PRE0071 behavior matches arithmetic/comparison qualifier checks where static qualifiers are known.

## Validation snapshot

- dotnet build src/Precept/Precept.csproj succeeded.
- dotnet test test/Precept.Tests/Precept.Tests.csproj --no-build remains red at baseline count (24 failing tests), with pre-existing affine/shift-related failures still present.


---

