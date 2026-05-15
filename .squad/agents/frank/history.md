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
