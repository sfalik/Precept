# Squad Decisions

---

### 2026-05-16T03:25:00Z: OQ8 locks diagram entry arrow for construction

**By:** Scribe

**Status:** Merged from frank-28's OQ8 diagram note.

**Merged source:** `frank-oq8-diagram-entry-arrow.md`.

- Construction is shown as a ● pseudo-node entry arrow in the diagram.
- Construction rows stay in the inspector section above transition rows.
- Durable rule: prefer the UML initial-pseudostate convention when it keeps construction legible and uncluttered.

### 2026-05-16T02:47:48Z: OQ4 locks `UnreachableRow`; construction rows now use `EventRow`

**By:** Scribe

**Status:** Merged from frank-24's OQ4 lock + EventRow rename batch.

**Merged source:** `frank-oq4-unreachable-row.md`.

- `UnreachableRow` is the single diagnostic code for both construction rows and transition rows that appear after an always-matching row.
- The construction-row family now uses `EventRow` / `EventRowReject`, preserving the asymmetric base-name + `Reject` rule alongside `TransitionRow` / `TransitionRowReject`.
- Durable rule: when two diagnostics describe the same structural defect at different row scopes, unify the code and vary only the message text by context.

---

## ACTIVE DECISIONS — Current Sprint

---

---

### 2026-05-16T02:40:40Z: Transition-row naming finalization locks the asymmetric family model

**By:** Scribe

**Status:** Merged from frank-23's naming-finalization pass.

**Merged source:** `frank-23` spawn manifest.

- `TransitionRowResolution` is now finalized as `TransitionRow` across `docs/language/precept-grammar.md` (8 renames) and `docs/working/constructor-semantics.md` (11 renames).
- Both construct families now use the same asymmetric naming rule on the documentation surface: `TransitionRow` / `TransitionRowReject` and `EventHandler` / `EventHandlerReject`.
- Durable naming rule: the base construct name denotes the success path; only the reject path carries an explicit `Reject` suffix.

---

### 2026-05-16T02:12:27Z: Constructor-semantics runtime documentation is a ship gate, not follow-up cleanup

**By:** Scribe

**Status:** Merged from Frank's constructor runtime-doc sync note.

**Merged source:** `frank-runtime-doc-sync.md`.

- `docs/runtime/runtime-api.md` needed an explicit constructor-semantics sync: `Created` must be documented as the construction success outcome, invalid construction-only outcome variants must be called out, fire-once enforcement and hollow-context rules must be described, and stateless construction text must stop talking about `Applied`.
- The runtime/API docs also need the constructor-specific row-dispatch and diagnostic story kept honest: `AlwaysRejecting` severity promotion for initial events, the mutation-vs-reject row model, and the current set of constructor diagnostics all belong in the implementation-facing narrative.
- Durable rule: constructor-semantics work is not complete until the runtime docs, result-type docs, evaluator docs, and MCP-facing contract notes are verified against the shipped wire/runtime surface.

---
### 2026-05-16T02:12:27Z: Constructor semantics are locked around `on <Event>` intake rows and explicit refusal semantics

**By:** Scribe

**Status:** Merged from Frank's constructor-semantics and reject-surface notes.

**Merged sources:** `frank-constructor-semantics-design.md`; `frank-constructor-design-updated.md`; `frank-conditional-construction-research.md`; `frank-reject-construction-semantics.md`; `frank-decision5-rationale.md`.

- The durable constructor surface is `on <Event>`: construction is authored as intake handling, guards remain allowed, and the stateful `on Event` restriction stays deliberate rather than accidental.
- Frank's precedent sweep confirmed conditional construction is normal language design; Precept's differentiator is declarative, guarded, inspectable construction rather than imperative constructor code.
- A fallback `reject` on construction rows is valid authored refusal, not misuse: omission means no construction path exists, `Unmatched` is the implicit no-guard-hit verdict, and `reject` is the explicit reason-bearing version of that refusal surface.

---
### 2026-05-16T02:12:27Z: Construction enforcement must stay grammar-first, and hollow-entity validation is a shared problem

**By:** Scribe

**Status:** Merged from Frank's constructor-semantics and reject-surface notes.

**Merged sources:** `frank-structural-exclusion.md`; `frank-construction-transition-enforcement.md`; `frank-construction-diagnostics.md`; `frank-4.8-gap-analysis.md`.

- `transition` and `no transition` are already structurally impossible on `EventHandler` because the construct omits `SlotOutcome`; construction should gain refusal through a dedicated `SlotRejectClause`, not by reviving a shared outcome slot or a type-checker-only ban.
- `AlwaysRejecting` already detects all-reject event surfaces; the remaining diagnostics work is to treat initial events as an error lane and keep the construction documentation aligned with the shipped analyzer/type-checker behavior.
- The PRE0142/PRE0144 follow-up is broader than construction guards: hollow-entity availability checks should cover construction guards, all expression-carrying construction actions, interpolated typed-constant holes, and field `default` expressions from one shared validation story.

---
### 2026-05-16T02:12:27Z: Transition-row reject mutual exclusion is still an implementation gap, not a new design question

**By:** Scribe

**Status:** Merged from Frank's constructor-semantics and reject-surface notes.

**Merged sources:** `frank-reject-mutual-exclusion.md`; `frank-transition-row-slot-consistency.md`; `frank-transition-grammar-surface.md`.

- OQ1 is already decided: any work-or-reject surface must split success/mutation and reject into separate grammar constructs so hybrid rows are unwritable by construction.
- The shipped `TransitionRow` still carries optional `SlotActionChain` plus combined `SlotOutcome`, so `-> set ... -> reject ...` remains syntactically writable today even though no sample relies on it.
- The repair is to split `TransitionRow` into mutation and reject forms, narrow success outcomes away from `reject`, mirror the split in the typed semantic-model DU, and update the language spec to describe the separated row shapes.
- The authored DSL surface stays effectively unchanged for valid rows: existing mutation rows and reject-only fallback rows remain writable as-is, while only the invalid action-plus-reject hybrid becomes structurally impossible.

---
### 2026-05-15T23:14:11Z: Qualifier enforcement closeout is now durable across N1 and N3/N4

**By:** Scribe

**Status:** Merged from George's N1 fix note and Soup Nazi's N3/N4 regression closeout.

**Merged sources:** `george-n1-fix.md`; `soup-nazi-n3n4-tests.md`.

- George closed Frank's N1 note in commit `f55e283b` by making `ResolveSlotSourceQualifierAxis(...)` return `Unknown` instead of `Absent` when an unresolved slot hole's type can carry the requested qualifier axis.
- Soup Nazi closed N3/N4 in commit `3468dec0` with 26 regression tests across `TypeCheckerAssignmentQualifierTests` and `ProofEngineTypedArgQualifierTests`, covering the implied `duration` qualifier path, compound-cancellation helper parity, and qualifier preservation for `min`, `max`, and `round`.
- Final closeout validation finished `dotnet test test/Precept.Tests/` at 5689 / 5699 passing with 10 unrelated pre-existing failures, and all 26 new tests passed.
- A transient detached HEAD artifact that surfaced as a duplicate variable in `FieldState.cs` was resolved before the final verification run and is not part of the shipped baseline.

---

### 2026-05-15T23:14:11Z: Construction guarantee audit repairs are now the shipped D94/D142/D143/D144 baseline

**By:** Scribe

**Status:** Merged from George's implementation notes for the initial-event and construction follow-up fixes.

**Merged sources:** `george-d142-implementation.md`; `george-construction-audit-fixes.md`.

- George closed the stateless construction blind spot by making validation inspect stateless initial handlers (and null-from-state rows when present) instead of skipping the lane outright.
- The shipped construction rules now include `PRE0142 UninitializedFieldReadInInitialAssignment`, `PRE0143 MaterializedFieldSelfReference`, and `PRE0144 UninitializedCrossFieldReadInInitialAssignment`, plus wildcard `from any` coverage accounting for required-field guarantees.
- Focused construction and diagnostic suites passed green, while full `Precept.Tests` remained on the unrelated proof/qualifier branch baseline noted in George's reports.

---

### 2026-05-15T23:14:11Z: Action-chain continuation arrows now stay on the action completion lane

**By:** Scribe

**Status:** Merged from Kramer's completion follow-up note.

**Merged source:** `kramer-action-chain-continuation.md`.

- Completion routing now handles continuation `->` tokens even before the parser has extended the enclosing action-chain span onto the next line.
- The fallback repair is intentionally narrow: a bounded backward scan over recent significant tokens classifies the site as `InActionVerb` when it finds the prior action verb and same-or-deeper-indented chain arrow.
- Validation closed green at `320 / 320` `Precept.LanguageServer.Tests` passing after the new `Completions_ActionChainContinuationArrow_UsesActionItems` regression landed.

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


