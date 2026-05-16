## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; pipeline, runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.
- Proof, qualifier, field-state, and normalization design work must stay grounded in shipped surfaces and verified implementation seams.

## Live Guidance

- Quantity normalization still has two durable lanes: compile-time normalization for declarations/literals and runtime normalization for ingress values; both should stay on shared normalizer logic.
- `TypedField` remains the normalization handshake between analysis and execution: authored bounds stay available for display, normalized bounds feed proof/comparison surfaces.
- Comparison/equality checking must stay as strict about explicit counting-unit identity as assignment is about constrained qualifier axes.
- Completion-provider defects should be treated as context-routing mistakes first; prefer catalog-sourced vocabularies plus explicit slot lanes over local keyword lists.
- Reject-bearing surfaces should be split structurally: success/mutation rows and refusal rows are separate constructs, not a shared slot plus cleanup diagnostics.

## Durable Learnings

- Any claim that work happens "only at compile time" must be stress-tested against Fire/Update/Restore ingress paths.
- Dynamic-unit interpolated forms MUST produce `Unbounded` / not-proved — never fall back to raw `StaticMagnitude` against normalized bounds.
- Counting-unit comparisons need unit-code identity, not just dimension-family compatibility.
- `ResolveSlotSourceQualifierAxis` must distinguish `Unknown` from `Absent`; `IsAssignmentQualifierAxisApplicable` is the discriminator.
- Function-call qualifier preservation belongs in the same enforcement story as operator qualifier compatibility.
- When the grammar can make an invalid form impossible, do that instead of inventing a later semantic ban.
- Hollow-entity validation should be shared across all pre-materialization expression lanes, not re-added slot by slot.

## Historical Summary

- 2026-05-12 through 2026-05-16 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor semantics, reject-surface structure, interval-proof design, quantity normalization, diagnostic-enforcement architecture, and counting-unit comparison gaps.
- The constructor/reject track settled three durable ideas: `on <Event>` is the honest construction surface, fallback `reject` is valid authored refusal rather than misuse, and grammar-level structural exclusion is preferred whenever the language already knows a path is impossible.
- Older slice-by-slice review detail now lives in `history-archive.md` and `.squad/decisions.md`; this file keeps only the guidance and outcomes other agents need immediately.

## Recent Updates

### 2026-05-16T02:12:27Z — Transition-row reject mutual exclusion remains an implementation gap

- OQ1 is already locked: reject must be its own row shape anywhere the language offers a work-or-reject surface.
- `TransitionRow` still combines `SlotActionChain` with a shared `SlotOutcome`, so mutate-then-reject hybrids remain writable until the construct and semantic-model split lands.
- The OQ1-compliant repair is direct: `TransitionRowMutation` keeps action chain plus success-only outcome, `TransitionRowReject` gets a dedicated reject clause, and the parser can disambiguate on the first token after `->`.
- Critical constraint: valid authored rows do not need migration. The split only removes the invalid action-plus-reject hybrid while preserving existing mutation rows and reject-only fallback rows.
- Construction-side guidance remains grammar-first: `EventHandler` already excludes success outcomes structurally, and any construction reject lane should come from a dedicated `SlotRejectClause`.

### 2026-05-16T00:10:00Z — §4.8 gap is broader than construction guards

- Audited the shipped PRE0142/PRE0144 path in `TypeChecker.Validation.FieldState.cs`; the implementation only walks `set` action primary/secondary expressions from initial construction chains.
- Confirmed the documented guard gap is real, but also found two additional construction-row gaps: non-`set` input actions are skipped, and `InterpolatedTypedConstant` holes are not visited by `CollectFieldRefsFromExpression(...)`.
- Confirmed `reject` reasons are not currently typed expressions, so there is no hidden reject-message lane to audit.
- Identified field `default` expressions as a second hollow-entity context: `PriorFieldsOnly` enforces order, not value availability.

### 2026-05-15T23:59:59Z — Deferred test-9 fix spec recorded and shipped

- Frank traced the final deferred quantity-bound failure to two seams: typed-constant validator-specific diagnostic codes were not being promoted into `DiagnosticCode`, and quantity typed constants had a dimension-check bypass in assignment-qualifier validation.
- The repair spec is now durable in `.squad/decisions.md`, and George's follow-up lane reported the branch fully green at `5699/5699`.

### 2026-05-15T23:14:11Z — Deferred qualifier review remains APPROVED after N1–N4 disposition

- Frank reviewed George's three deferred qualifier-fix commits, issued N1–N4 notes, and left the lane APPROVED because the architecture and shipped fixes were sound.
- `N2` was already Shane's work and became moot; the remaining follow-up work was George's N1 slot-hole applicability fix plus Soup Nazi's N3/N4 regression coverage.
- Subsequent commits `f55e283b` and `3468dec0` closed the outstanding notes without changing Frank's approval posture.

### 2026-05-15T22:18:38Z — Completion position spec is now validated end-to-end

- Kramer's two passes consumed the 40-position completion spec and closed all 8 identified gaps without introducing LS-local keyword arrays.
- Durable rule: completion correctness here is a slot-routing problem. Use catalog-sourced vocabularies plus narrow `SlotContext` lanes when a completed target changes the valid next token set.
- Validation closed at 319/319 `Precept.LanguageServer.Tests` passing.

### 2026-05-15T22:12:27-04:00 — Runtime doc sync for constructor semantics

- Shane's directive: runtime docs must be updated as design deliverables, not afterthoughts.
- Audited `docs/runtime/runtime-api.md` against locked constructor semantics design. Found 11 gaps: `Created` variant undocumented, construction outcome space wrong (showed all 7 as reachable), `Create()` without initial event said `Applied`, fire-once not explicit, hollow-context rules absent, `AlwaysRejecting` promotion absent, `TransitionRowMutation`/`TransitionRowReject` dispatch undocumented, no-match to Rejected undocumented, new diagnostics missing, variant count stale, stateless contract still said `Applied`.
- Rewrote Construction section of runtime-api.md. Updated variant count, design decisions, stateless contract, and Fire section.
- Added section 10 to `docs/working/constructor-semantics.md` making runtime doc updates explicit deliverables with per-PR gates and an implementer verification rule.
- Durable learning: runtime API docs are a ship-gate artifact. Design decisions that change outcome semantics MUST land in the runtime doc in the same pass as the design lock, not deferred to implementation time.
