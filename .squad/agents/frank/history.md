## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; pipeline, runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.
- Proof, qualifier, field-state, and normalization design work must stay grounded in shipped surfaces and verified implementation seams.

## Live Guidance

- Quantity normalization now has two durable lanes: compile-time normalization for declarations and literals, runtime normalization for ingress values (`TypeRuntimeMeta.ReadJson` / `TypeRuntime<Quantity>.FromClr`). Both call the same `TypedConstantNormalizer` logic.
- `TypedField` is the normalization handshake between analysis and execution: authored bounds stay available for display, normalized bounds feed proof/comparison surfaces, and the Builder remains the conversion boundary into `PreceptValue`.
- `IntervalOf` scaling is expression-form scoped, not universal: scale static typed constants and interpolated magnitude + static-unit forms; do **not** scale field refs, arg refs, or interpolated whole-value holes.
- `GetFieldBounds` and trusted-fact extraction must read normalized quantity data, and event-arg bound normalization still needs explicit parity design.
- Compiler/runtime duplication questions should be framed through the three-layer enforcement model: compile-time diagnostics, ingress validation, and defense-in-depth runtime faults.
- Comparison and equality operator checking must mirror assignment strictness for explicit counting units: shared `count` dimension is not enough when static unit codes differ.

## Durable Learnings

- Any claim that work happens "only at compile time" must be stress-tested against Fire/Update/Restore ingress paths.
- Storage conventions for business-domain values are architectural decisions; they shape evaluator invariants and cannot be deferred casually.
- ProofEngine intervals and evaluator opcodes share source data, not a common intermediate representation.
- `PreceptValue` bytes 8-23 are a three-way union lane (`decimal`, `long`, or reference region); quantity unit identity is not blocked by the 32-byte layout.
- Prefer catalog-mediated dispatch and metadata-backed mappings over per-code hardcoded routing in both compiler and runtime consumers.
- Dynamic-unit interpolated forms MUST produce `Unbounded` / not-proved — never fall back to raw `StaticMagnitude` against normalized bounds. This is the false-proof prevention invariant.
- When a design says "universal post-step," verify it actually means expression-type-dispatched post-step — the dispatch table is the contract, not the word "universal."
- Slices that depend on unimplemented runtime infrastructure (stubs) should be explicitly numbered out of the implementation sequence and marked not implementation-ready to prevent ordering confusion.
- Counting-unit comparisons need unit-code identity, not just dimension-family compatibility; only physically convertible UCUM units should pass same-dimension cross-unit checks.
- Catalog `QualifierMatch` declarations are only as strong as their enforcement wiring: the Functions catalog declared `QualifierMatch.Same` on min/max/clamp/abs overloads from inception, but `SelectOverload` never read it. Audit principle: every catalog constraint must trace to an enforcement point.
- Function call resolution and binary operator resolution are architecturally parallel but historically asymmetric: operators have `ValidateQualifierCompatibility`, functions have nothing. Any future qualifier-sensitive surface must wire enforcement at the same time the catalog entry is declared.

## Historical Summary

- 2026-05-12 through 2026-05-15 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor diagnostics, interval-proof design, quantity normalization, diagnostic-enforcement architecture, and the counting-unit comparison gap.
- The durable enforcement baseline is: PRE0078 stays in ProofEngine Strategy 7, PRE0079 is the TypeChecker literal-bounds wire, PRE0019 is retired unless real presence-obligation generation is added, and PRE0094 is already emitted in the checker.
- Older batch-by-batch detail now lives in `.squad/decisions.md` and `history-archive.md`; this live file keeps only the guidance and latest outcomes other agents need immediately.

## Recent Updates

### 2026-05-15T14:55:25Z — Tracker sync and the Wave 2 / Slice N/M review loop are durably closed

- §5.0 tracker rows 15, 15b, 16, 19, 20, 31, 33, 35, 36, and 37 are recorded as ✅ against commit `f1215192`, and the bounds-validation documentation lane is now numbered as Slices 44 and 45.
- Wave 2 stayed APPROVED after George's `01f255ab` follow-up, which preserved authored-vs-normalized bounds and added the affine-price guard plus regression coverage.
- Slice N/M closed after two blocker passes: B1/B2 were fixed in `0837ad6f`, B3 was fixed in `70ee2406`, and the final verdict is APPROVED.

## Learnings

- Suppression fixes must be reviewed against every downstream diagnostic that depends on the suppression, especially when one path intentionally hands off to a more specific diagnostic.
- Tracker and documentation passes should assign stable slice IDs as soon as standalone fixes appear so later reviews and closeout logs can cite one durable name.
- WholeValue interval tests using same-unit (scale=1.0) for both source and target cannot detect double-normalization bugs. Cross-unit WholeValue tests are mandatory when Slice 19 lands — track as Slice 19 obligation.
- Intentionally-red tests that assert the *correct* expected behavior (not `Skip`) are superior contract pressure — they fail loudly on regression AND on fix, ensuring the fix is noticed and the test transitions to green deliberately.
- Display contract pattern: when a record carries both "math values" and "display values," the construction site must source them from genuinely distinct paths (e.g., `GetFieldBounds()` for normalized, `field.DeclaredMin` for authored). A fallback operator (`AuthoredMin ?? DeclaredMin`) at rendering sites ensures graceful degradation for non-quantity cases.
- `HasSingleMagnitudeSlot`-style positive guards are more robust than negative exclusion lists — new slot kinds automatically fail the check rather than needing maintenance.

### 2026-05-15T15:37:42Z — Slice 17 and Slice 18 reviews recorded approved

- Slice 17 review approved the 9-test normalization matrix and preserved the intentionally-red cross-dimension case as honest contract pressure.
- Slice 18 review approved the authored/normalized display contract split across proof requirements, diagnostics, and MCP projection.
- Durable warnings to keep live: Slice 19 still needs a cross-unit WholeValue regression plus MCP normalized-bound coverage, and the Test 6 cross-dimension root causes should stay tracked as debt until implementation closes them.

### 2026-05-15T15:37:42Z — Slice 21 review recorded approved

- Slice 21 APPROVED: 10 interpolated quantity integration tests covering all §5.3/G21 required behavioral cases. Conservative-case tests (3, 6, 7) correctly assert `NumericOverflow` fires for unbounded/dynamic-unit inputs.
- W1: conservative tests verify overflow fires but don't explicitly assert `ProofDisposition != Proved` — adding this would make the false-proof prevention invariant explicit in the suite. Not blocking.
- W2: both-holes form (`'{intField} {unitField}'`) not tested — single-hole dynamic-unit test (Test 6) covers the architectural invariant. Not blocking.
- Test 9 cross-unit WholeValue anchor is acceptable for Slice 21 scope; the tighter `max '1 kg'` variant that would actually detect double-normalization is correctly deferred as future obligation.
- Durable learning: happy-path anchors and regression detectors are distinct test categories — a test that passes regardless of bug presence is an anchor, not a guard. Both are valuable but must not be confused.

### 2026-05-15T12:15:38Z — Slice 24 review recorded approved

- Slice 24 APPROVED: Money/price interpolated interval extraction. Four code paths (money magnitude, money WholeValue, price+static-denominator magnitude, price-dynamic → Unbounded) correctly implemented in `IntervalOfNarrowed` with `ApplyStaticUnitScaling` handling the price inverse-factor case.
- The `HasSingleMagnitudeSlot` guard in `ApplyStaticUnitScaling` is correctly scoped: WholeValue expressions skip scaling because they're already normalized. The Unbounded guard for Price only fires on Magnitude (correct — WholeValue doesn't need denominator inversion).
- W1: No WholeValue-specific tests for money/price. The generic Slice 19 WholeValue path covers the behavior, but dedicated anchors should land in follow-up.
- W2: No same-unit (factor=1.0) price test to detect accidental scaling. Low risk but good regression anchor debt.
- Doc tracker in §5.0 needs Slice 24 marked complete.

### 2026-05-15T12:15:38Z — Slice 23 review recorded approved

- Slice 23 APPROVED: StaticQualifier routing through proof-engine and type-checker qualifier consumers. Early-return pattern in `ResolveQualifierFromInterpolatedConstant` covers all four subtypes with correct axis fall-through. `BuildQualifiersFromStaticInterpolated` maps subtypes to `DeclaredQualifierMeta` arrays for PRE0134 emission.
- WholeValue constraint (B23) is structurally enforced: `ResolveStaticQualifier` returns null on WholeValue slots — no double-copy possible.
- PRE0134 mismatch tests exist for unit (kg/g) and currency (USD/EUR). No price/exchangerate mismatch test — low risk warning.
- One failing test (`CompoundUnitPositivityProof_ClearsDivisionByZero`) is pre-existing, not a regression.
- Slice 25 (field-default proof coverage) is now unblocked. Doc tracker in §5.0 needs Slice 23 marked complete.

### 2026-05-15T14:01:02Z — Modifier comma separator assessment

- Assessed mandatory comma between field modifiers. Verdict: **Do not introduce.**
- Key findings: (1) No sample field in the corpus exceeds 4 modifiers. The densest pattern is `default V nonnegative maxplaces N writable` — 4 modifiers, already keyword-anchored and unambiguous. (2) The parser (`ParseModifierList`) loops on `ValueModifierTokens.Contains(Peek().Kind)` — each modifier is keyword-led, making the sequence self-delimiting. Valued modifiers consume their expression via a boundary predicate that terminates at the next modifier keyword or construct boundary. There is zero parsing ambiguity today. (3) Comma (`TokenKind.Comma`) is already used exclusively as a list-item separator (state entries, event args, field names, choice values). Adding it as a modifier separator would overload its semantic role. (4) Philosophy principle #5 (keyword-anchored readability) explicitly states that keywords are the structural boundaries — not punctuation. Commas would contradict this grounding principle.
- If density grows in the future (temporal types, new constraint modifiers), the right lever is formatter-enforced line breaks per modifier — a tooling concern, not a grammar concern.

### 2026-05-15T18:32:00Z — QS-1 review: model additions APPROVED

- QS-1 (commit `f391d197`) APPROVED: `QualifierAxis.PriceIn`, `QualifierShape.OfRequiresCurrencyIn`, and `DeclaredQualifierMeta.CompoundPrice` all match the spec in `docs/Working/frank-price-qualifier-shape-analysis.md` §3 exactly.
- Model-only slice is architecturally clean: no behavioral wiring, no half-states. All downstream `QualifierAxis` switches (14 total across TypeChecker, ProofEngine, RichHoverFactory, CompletionHandler) have `_ =>` defaults that handle `PriceIn` gracefully.
- `Types.cs` correctly NOT changed — `QS_CurrencyAndDimension` stays on `QualifierAxis.Currency` until QS-2 wires the handler.
- W1–W4: RichHoverFactory has 5 axis-name/label/text switches and MCP has string interpolation that will show generic/opaque labels for `PriceIn`. All non-blocking — tracked as QS-2 obligations.
- Durable learning: model-only slices that add enum values and DU subtypes are safe to land independently when all consumers use default/wildcard fallbacks. The exhaustion risk is only real for switches without `_ =>` arms.
