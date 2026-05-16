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
- Formal grammar production rules must reflect structural exclusion decisions immediately — the grammar doc is a design deliverable, not an afterthought that waits for implementation.

- Completed the planned runtime-tool section in `docs/tooling/mcp.md` so all four planned tools now have purpose, inputs, outputs, and explicit runtime-implementation dependencies: `precept_create`, `precept_update`, `precept_inspect`, and `precept_fire`.
- Locked OQ7 in `docs/working/constructor-semantics.md`: construction inspection will be implemented as `InspectCreate()` in core and exposed through `precept_inspect`.
- Wrote the confirmation note to `.squad/decisions/inbox/frank-planned-runtime-tools.md` so the canonical MCP doc reflects Shane's confirmed surface before runtime work begins.

### 2026-05-15T23:05:36.097-04:00 — OQ6 locked: `precept_create` is the planned construction MCP tool

- Shane chose Option A: add `precept_create` as a dedicated planned MCP tool instead of overloading `precept_fire`.
- Updated `docs/tooling/mcp.md` so `precept_create` is documented as planned with purpose, inputs, outputs, and the `Create()` runtime dependency.
- Updated `docs/working/constructor-semantics.md` to replace the open-question callout with a locked decision block; `precept_fire` remains existing-entity-only.

### 2026-05-15T22:59:42.817-04:00 — OQ5 locked: `docs/tooling/mcp.md` is canonical

- Consolidated `docs/McpServerDesign.md` into `docs/tooling/mcp.md` and archived the old file as a redirect stub.
- Verified the live discoverable MCP surface from `tools/Precept.Mcp/Tools/`: `precept_ping`, `precept_quickstart`, `precept_syntax`, `precept_types`, `precept_operations`, `precept_proofs`, `precept_patterns`, `precept_diagnostic`, `precept_domains`, and `precept_compile`.
- Durable rule: `docs/tooling/mcp.md` is the single MCP contract doc; `tools/Precept.Plugin/README.md` remains a separate shipped-payload/distribution note, not the live contract.

### 2026-05-15T22:29:35-04:00 — Naming decision applied: Resolution/Reject

- Shane chose Option A (`Resolution/Reject`) as the naming axis for the grammar-level split constructs.
- Applied renames across `docs/language/precept-grammar.md` (21 occurrences) and `docs/working/constructor-semantics.md` (23 occurrences).
- `TransitionRowMutation` → `TransitionRowResolution`, `EventHandlerMutation` → `EventHandlerResolution`, `MutationOutcome` → `ResolutionOutcome`.
- Typed model records also renamed: `TypedEventHandlerMutation` → `TypedEventHandlerResolution`, `TypedTransitionMutationRow` → `TypedTransitionResolutionRow`.
- Prose uses of "mutation" (describing field writes) left untouched.
- Decision record written to `.squad/decisions/inbox/frank-resolution-naming.md`.

### 2026-05-15T22:20:33-04:00 — Grammar doc updated: TransitionRow/EventHandler → mutation/reject split

- Updated `docs/language/precept-grammar.md` to reflect the OQ1-locked structural split.
- `TransitionRow` → `TransitionRowMutation` + `TransitionRowReject`; `EventHandler` → `EventHandlerMutation` + `EventHandlerReject`.
- Introduced `MutationOutcome` (narrowed: `transition StateName` | `no transition`) and `RejectClause` (`reject StringLiteral`) as distinct slot kinds replacing the old combined `Outcome`.
- Parser disambiguation: after reaching `->`, if the next token is `reject`, the construct is the Reject variant; otherwise it's the Mutation variant. Same logic applies to both `from` and `on` families.
- The slot-kind count increased from 17 to 18 (split `Outcome` → `MutationOutcome` + `RejectClause`); construct count increased from 12 to 14.


### 2026-05-15T22:47:48.336-04:00 — OQ4 unified as `UnreachableRow`; `EventRow` naming adopted

- Locked OQ4 to a single `UnreachableRow` diagnostic for both construction rows and transition rows shadowed by an earlier always-matching row.
- Updated the constructor-semantics and grammar docs to rename the construction-row family to `EventRow` / `EventRowReject`, preserving the asymmetric base-name + `Reject` rule alongside `TransitionRow` / `TransitionRowReject`.
- Durable rule: when two diagnostics describe the same structural defect at different row scopes, unify the code and vary only the message text by context.
### 2026-05-16T02:47:48Z — Frank-24 decision merged and inbox cleared

- Merged the `frank-oq4-unreachable-row.md` inbox note into `.squad/decisions.md`.
- The durable row-shadowing diagnostic is `UnreachableRow`; the construction-row family now uses `EventRow` / `EventRowReject`.
- The inbox note was deleted after merge.
## Historical Summary

- 2026-05-12 through 2026-05-16 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor semantics, reject-surface structure, interval-proof design, quantity normalization, diagnostic-enforcement architecture, and counting-unit comparison gaps.
- The constructor/reject track settled three durable ideas: `on <Event>` is the honest construction surface, fallback `reject` is valid authored refusal rather than misuse, and grammar-level structural exclusion is preferred whenever the language already knows a path is impossible.
- Older slice-by-slice review detail now lives in `history-archive.md` and `.squad/decisions.md`; this file keeps only the guidance and outcomes other agents need immediately.

## Recent Updates

### 2026-05-16T03:20:00Z — Scribe batch closed

- The planned runtime-tool note was already present in `.squad/decisions.md`, so the inbox copy was cleared without duplication.
- No decision entries were older than 30 days, so the archive gate made no changes.
- Frank history was condensed to stay below the 15 KB threshold.

### 2026-05-16T03:08:40Z — Precept-create OQ6 batch recorded

- Pre-check found `.squad/decisions.md` at 20999 bytes and 0 inbox notes; the archive gate found no entries older than 30 days.
- No decision merge was needed, no history file crossed the 15 KB summarization gate, and the batch ended with orchestration/session logs only.

### 2026-05-16T03:07:39Z — MCP consolidation batch recorded

- Merged the `frank-precept-create-tool.md` inbox note into `.squad/decisions.md`; the earlier OQ5 canonical note was already present.
- No decision entries older than 30 days were archived; no agent history crossed the 15 KB gate on this pass.

### Batch summary

- 2026-05-16 02:40 through 02:12: TransitionRow/EventHandler naming was finalized, the reject-only split was confirmed, and constructor/runtime doc sync closed.
- 2026-05-15 23:59 through 22:12: qualifier follow-ups, completion-position validation, and runtime API sync were completed.