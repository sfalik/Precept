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
- `ResolveSlotSourceQualifierAxis` must distinguish `Unknown` (type supports axis but source doesn't declare it) from `Absent` (type cannot carry qualifier on this axis). The `IsAssignmentQualifierAxisApplicable` check is the discriminator — returning `Absent` for a bare quantity source feeding a unit slot into a dimension-constrained target is a silent acceptance bug.
- Catalog `QualifierMatch` declarations are only as strong as their enforcement wiring: the Functions catalog declared `QualifierMatch.Same` on min/max/clamp/abs overloads from inception, but `SelectOverload` never read it. Audit principle: every catalog constraint must trace to an enforcement point.
- Function call resolution and binary operator resolution are architecturally parallel but historically asymmetric: operators have `ValidateQualifierCompatibility`, functions have nothing. Any future qualifier-sensitive surface must wire enforcement at the same time the catalog entry is declared.

## Historical Summary

- 2026-05-12 through 2026-05-15 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor diagnostics, interval-proof design, quantity normalization, diagnostic-enforcement architecture, and the counting-unit comparison gap.
- The durable enforcement baseline is: PRE0078 stays in ProofEngine Strategy 7, PRE0079 is the TypeChecker literal-bounds wire, PRE0019 is retired unless real presence-obligation generation is added, and PRE0094 is already emitted in the checker.
- Older batch-by-batch detail now lives in `.squad/decisions.md` and `history-archive.md`; this live file keeps only the guidance and latest outcomes other agents need immediately.

## Learnings

### 2026-05-15T18:09:43-04:00 — Uninitialized Field Read in Initial Event (diagnostic gap)

- **Confirmed genuine diagnostic gap:** `set count = count + 1` in an initial event where `count` has no `default` produces zero diagnostics. The compiler silently accepts a provable uninitialized read.
- **Two distinct gaps found:**
  1. **D94 stateless blind spot:** `ValidateConstructionGuarantees` returns early at line 323 when `ctx.States.Count == 0`, giving stateless precepts zero per-field construction validation even when an initial event exists and required fields have no defaults.
  2. **Missing self-referential initial assignment check:** Even if D94 ran, it only checks whether the field IS assigned (`IsSetAction` + `FieldName` match). It never inspects the RHS expression. `set count = count + 1` passes D94's check because `count` IS being set — but the RHS reads an undefined value.
- **Recommendation:** New diagnostic D142 (`UninitializedFieldReadInInitialAssignment`) in `TypeChecker.Validation.FieldState.cs`. Separate fix for D94's stateless early return. Both are TypeChecker-stage, not ProofEngine.
- **Durable learning:** Construction guarantee checks that verify "field IS assigned" are necessary but insufficient. The completeness guarantee requires also checking that assignment expressions don't read the field's own undefined value. This is analogous to the "use before def" analysis in traditional compilers — a dimension the field-state validation framework currently lacks entirely.

### 2026-05-15T18:04:26.860-04:00 — Completion Position Specification Review

- **Top-level keyword leak:** `SlotContextResolver.GetCursorContext` falls through to `SlotContext.TopLevel` as a default when it cannot classify the position. This is the root cause of Shane's complaint — top-level construct keywords (`field`, `state`, `event`, `rule`, `from`, `in`, `on`, `to`) appear in expression contexts, type positions, and modifier positions because an unclassified cursor defaults to `TopLevel`, which maps to `GetTopLevelItems()`. The fix is: unclassified mid-construct cursors should return `AfterKeyword` (empty completions), not `TopLevel`.
- **SlotContext coverage is incomplete for some grammar positions.** There is no distinct `SlotContext` for: after `->` (where action verbs OR outcome keywords are expected), inside `because` string expressions, after `transition` keyword (where a state name is expected), after `no` keyword (where `transition` is expected), or inside event argument lists (where parameter names then `as` then types are expected). These are all routed through heuristic fallbacks.
- **Modifier completions are already type-filtered** via `GetModifiers(compilation, position, domain)` — this reads the resolved type and already-applied modifiers. This is correct. State modifiers (`terminal`, `required`, `irreversible`, `success`, `warning`, `error`) and event modifiers (`initial`) are served through `ModifierDomain.State` and `ModifierDomain.Event` respectively. Value modifiers are filtered by `TypeTarget` applicability.
- **Expression completions correctly include event args** scoped to the current event via `GetCurrentEventArgItems`. They include field names, functions, and boolean literals. Boolean literal suppression exists for known non-boolean target types.
- **Typed-constant completions are sophisticated** — they include slot-phase detection (number → space → unit), qualifier-aware filtering, currency/unit/dimension catalogs, reuse of existing typed constants in the file, and temporal format examples.
- **The `any` and `all` keywords** need special handling: `any` is valid in state-target positions (as a wildcard), `all` is valid in field-target positions (for `modify all` / `omit all`). Neither should appear in expression completions.
- **Outcome keywords (`transition`, `no transition`, `reject`)** need their own completion context after `->` when no action verb is recognized. Currently, `InActionVerb` serves action keywords but doesn't include outcome keywords — they share the same `->` trigger.

## Recent Updates

- Older Recent Updates entries were compacted into history-archive.md on 2026-05-15T22:09:58Z.

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

### 2026-05-15T22:09:58Z — Deferred qualifier follow-up closure merged and architectural approval preserved

- George shipped all three items from Frank's scoped follow-up plan: qualifier-preserving TypedFunctionCall.ResultQualifiers, TypeChecker implied-qualifier parity, and the explicit quantity-slot Unknown fallback.
- Soup Nazi's quantity sweep stayed green, confirming the quantity expression lane was already closed before the hardening pass.
- Scribe merged the deferred-fix closure and Frank's uninitialized-field-read diagnostic-gap ruling into .squad/decisions.md; Frank's earlier PRE0141 review approval remains the standing architectural verdict.

