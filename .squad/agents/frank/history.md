## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; pipeline, runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.
- Proof, qualifier, field-state, and normalization design work must stay grounded in shipped surfaces and verified implementation seams.

## Live Guidance

- Quantity normalization still has two durable lanes: compile-time normalization for declarations/literals and runtime normalization for ingress values; both should stay on shared normalizer logic.
- `TypedField` remains the normalization handshake between analysis and execution: authored bounds stay available for display, normalized bounds feed proof/comparison surfaces.
- Comparison/equality checking must stay as strict about explicit counting-unit identity as assignment is about constrained qualifier axes.
- Completion-provider defects should be treated as context-routing mistakes first; prefer catalog-sourced vocabularies plus explicit slot lanes over local keyword lists.

## Durable Learnings

- Any claim that work happens "only at compile time" must be stress-tested against Fire/Update/Restore ingress paths.
- Dynamic-unit interpolated forms MUST produce `Unbounded` / not-proved — never fall back to raw `StaticMagnitude` against normalized bounds.
- Counting-unit comparisons need unit-code identity, not just dimension-family compatibility.
- `ResolveSlotSourceQualifierAxis` must distinguish `Unknown` from `Absent`; `IsAssignmentQualifierAxisApplicable` is the discriminator.
- Function-call qualifier preservation belongs in the same enforcement story as operator qualifier compatibility.

## Historical Summary

- 2026-05-12 through 2026-05-15 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor diagnostics, interval-proof design, quantity normalization, diagnostic-enforcement architecture, and counting-unit comparison gaps.
- Older slice-by-slice approval detail now lives in `history-archive.md` and `.squad/decisions.md`; this file keeps only the guidance and outcomes other agents need immediately.

## Learnings

### 2026-05-15T20:16:01-04:00 — Reversed "uniformity" recommendation: `on <Event>` is correct construction syntax for stateful precepts

- Shane's pushback on the "keep `from <InitialState> on <InitialEvent>`" recommendation was correct. Full analysis in `.squad/decisions/inbox/frank-constructor-syntax-on-event.md`.
- **Core insight:** False uniformity (dressing unconditional genesis as state-dispatched transition) is worse than honest divergence. If terminal construction established that construction is a distinct semantic category, the syntax must follow.
- Grammar unification is clean: `EventHandlerDeclaration` is the shared production. Disambiguation between construction rows and state-agnostic handlers happens at type-checking via the `initial` modifier on the event declaration. No parser ambiguity.
- Guard restriction (`EventHandlerDoesNotSupportGuard`) should be removed: `when` guards on `on Event ->` forms are coherent for both construction (guarded paths) and state-agnostic handlers (field-value conditions).
- Stateless/stateful constructor syntax unification reflects semantic reality: construction IS the same operation in both contexts.
- Durable learning: "syntactic uniformity" is only valuable when it reflects semantic uniformity. When semantics diverge, forcing shared syntax teaches a lie.

### 2026-05-15T19:57:05-04:00 — Terminal constructor design analysis reverses prior position on construction-time transitions

- Evaluated Shane's proposal: initial event as true constructor (always terminal in initial state + fire-once enforcement).
- **Reversed prior verdict** that construction-time transitions are "sound." They are technically sound but semantically confusing — `initial` stops meaning "where entities begin" when construction can route elsewhere.
- The proposal resolves keyword overload more elegantly than a rename: `initial` genuinely means "origin" in both state and event contexts when construction is terminal.
- Construction-time routing pattern is empirically unused (0/20+ samples) and replaceable with two-step construct + route.
- D94 simplifies to single-target analysis (always initial state). Proof engine wins.
- Fire-once enforcement via excluding initial event from post-construction event space (returns `UndefinedEvent`). No new mechanism.
- Stateless precepts: fully compatible (constraints vacuously satisfied).
- **Recommendation: Adopt.** Superior to keyword rename approach. Decision saved to `.squad/decisions/inbox/frank-constructor-terminal-design.md`.

### 2026-05-15T19:48:03-04:00 — Critical design review of initial event/state semantics identified 3 pre-ship fixes

- `initial` keyword overload is a P5 violation: same word means "graph position" (state) and "construction mechanism" (event). Recommendation: split to `initial` (state) + `constructor` (event).
- Stateless/stateful mutual exclusion is too rigid: `on Event -> actions` should be allowed in stateful precepts when no `from ... on <same event>` row exists. Current restriction forces boilerplate `from any on X -> ... -> no transition`.
- `no transition` in stateless handlers: grammar says EventHandlerDeclaration has no Outcome production, but semantics doc implied it was valid. Need parser verification and enforcement.
- Hollow-then-hydrate pattern is sound (working copy atomicity resolves it).
- Construction-time transition away from initial state is sound (initial state is dispatch context + omit shape + entry ensures), but needs better documentation.
- Construction guarantee is composite D94 + D132, not D94 alone — docs should make this explicit.

### 2026-05-15T19:37:38-04:00 — Initial event/state semantics audit confirmed spec-implementation alignment

- `initial` on state (lifecycle position) and `initial` on event (construction mechanism) are orthogonal concepts sharing a keyword.
- Construction chain: hollow version (state set, defaults applied) → initial event fires through standard pipeline → outcome returned.
- Initial event CAN transition away from initial state at construction time — `Transitioned` is valid construction outcome.
- D94 checks per-row completeness, not aggregate — every guarded construction path must assign all required fields.
- Wildcard `from any` rows count as construction paths; omitted fields in initial state exempt from assignment requirement.
- No inconsistency found between spec, runtime API doc, and diagnostic implementations.

### 2026-05-15T18:36:25-04:00 — Broader construction guarantee audit found 4 additional bugs

- Wildcard FromState rows invisible to stateful construction check (false positive + false negative).
- D132 materialization check lacks self-referential RHS validation (proposed D143).
- D142 ignores SecondaryExpression (inconsistent with D130 sibling).
- D142 ignores cross-field uninitialized reads (proposed D144).
- Durable learning: construction guarantee completeness requires checking which chains are considered, what constitutes a valid assignment, and what scope of field references is checked.

### 2026-05-15T18:09:43-04:00 — Uninitialized field reads in initial events need dedicated checking

- `ValidateConstructionGuarantees` currently misses stateless precepts and cannot detect self-referential reads on the RHS of an initial `set` action.
- Treat the repair as two TypeChecker fixes: keep `PRE0094` focused on missing required-field assignments, and add a separate D142-style diagnostic for `set X = f(X)` when `X` has no default or prior assignment.

### 2026-05-15T18:04:26.860-04:00 — Completion position specs should drive context routing, not local suppression lists

- Top-level constructs belong only to `SlotContext.TopLevel` and only at whitespace-only line starts.
- Missing completion cases should first become explicit slot lanes (`AfterStateTarget`, `AfterEventTarget`, `AfterNo`, `InSetAssignment`) before anyone adds ad-hoc item filters.

## Recent Updates

### 2026-05-15T23:59:59Z — Deferred test-9 fix spec recorded and shipped

- Frank traced the final deferred quantity-bound failure to two seams: typed-constant validator-specific diagnostic codes were not being promoted into `DiagnosticCode`, and quantity typed constants had a dimension-check bypass in assignment-qualifier validation.
- The repair spec is now durable in `.squad/decisions.md`, and George's follow-up lane reported the branch fully green at `5699/5699`.


### 2026-05-15T23:14:11Z — Deferred qualifier review remains APPROVED after N1–N4 disposition

- Frank reviewed George's three deferred qualifier-fix commits, issued N1–N4 notes, and left the lane APPROVED because the architecture and shipped fixes were sound.
- `N2` was already Shane's work and became moot; the remaining follow-up work was George's N1 slot-hole applicability fix plus Soup Nazi's N3/N4 regression coverage.
- Subsequent commits `f55e283b` and `3468dec0` closed the outstanding notes without changing Frank's approval posture.

### 2026-05-15T22:27:03Z — Deferred qualifier follow-up landed

- George closed the three deferred PRE0141 items: `TypedFunctionCall.ResultQualifiers`, implied-qualifier parity in `ResolveDirectQualifierAxis`, shared compound-unit helpers, and explicit `Unknown` fallback for qualifier-capable slot sources.
- Targeted qualifier suites stayed green and the full core suite returned to the unchanged 9-failure branch baseline.

### 2026-05-15T22:18:38Z — Completion position spec is now validated end-to-end

- Kramer's two passes consumed the 40-position completion spec and closed all 8 identified gaps without introducing LS-local keyword arrays.
- Durable rule: completion correctness here is a slot-routing problem. Use catalog-sourced vocabularies plus narrow `SlotContext` lanes when a completed target changes the valid next token set.
- Validation closed at 319/319 `Precept.LanguageServer.Tests` passing.

### 2026-05-15T18:34:40-04:00 — Deferred qualifier fixes APPROVED (commit 85974302)

- Reviewed George's implementation of three deferred items from PRE0141 seam replacement. All three core fixes are correct and match spec intent.
- ImpliedQualifiers parity, UCUM helper extraction, function-call qualifier preservation, and slot Absent→Unknown fix all land cleanly.
- Non-blocking: early-return in `ResolveSlotSourceQualifierAxis` still returns Absent (spec prescribed Unknown for applicable types), but the path is unreachable in practice. LS changes (~400 lines) bundled out of scope — approved but flagged for commit hygiene.
- Decision recorded in `.squad/decisions/inbox/frank-deferred-fixes-review.md`.

### 2026-05-15T20:40:13Z — Price qualifier enforcement architecture is durably closed

- The shipped assignment qualifier model is now the per-axis `Resolved / Unknown / Absent` contract behind `PRE0141`; unknown constrained axes are never silent success.
- The separate `PriceIn` / `CompoundPrice` qualifier-shape work remains design-state only and is not a prerequisite for the enforcement repair.

### 2026-05-15T23:26:25Z — Test-failure triage is durably closed to one deferred red test

- Frank's fix spec on the 10 pre-existing failures is now canonical: the lane had three root causes — pairwise qualifier false positives, a missing `time` dimension alias, and the interpolated-qualifier positivity proof seam.
- George implemented the repair set in commit `a03fcf4e`, and the branch moved from 10 failing tests to 1 remaining intentionally deferred quantity-bound dimension-check failure.
- Scribe merged Frank's fix spec plus George's pairwise-fix closeout into the canonical decision ledger and cleared the inbox copy.
