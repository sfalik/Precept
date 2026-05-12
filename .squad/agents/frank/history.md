## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs derive from durable catalog shape rather than enum-identity switches or parallel lists.
- Interpolation work must preserve compile-time guarantees first; plans that trade structural certainty for runtime validation remain philosophically out of bounds.
- Proof and qualifier fixes should stay bounded to catalog metadata and conservative symbolic reasoning rather than speculative provenance systems.

## Live Guidance

- String holes remain out of scope for interpolated typed constants; typed-hole composition is the only acceptable path.
- Slice 6 stays numeric-only, including the single-hole whole-value fallback; qualifier, dimension, modifier, and presence obligations remain declaration-driven.
- Temporal semantics stay with `duration` / `period`; `quantity of 'time'` remains invalid, while temporal-denominated prices stay on `price of ...`.
- MCP/public tooling contracts should continue to expose curated projections rather than raw core catalog records.

## Historical Summary

- 2026-05-11 through early 2026-05-12 established the current durable research baseline: inventory-item root-cause triage, temporal guidance, proof-gap closure assessment, qualifier-path fixes, DTO-free MCP execution planning, and the field-state-guarantee investigation.
- The comma-list spike sequence is now locked at the documentation/research layer: parser-disambiguation docs need correction, MCP surfaces stay catalog-driven, and the spike record captures both the full-scope exploration and the later event-list deferral decision.
- Older detailed batch-by-batch chronology lives in `.squad/decisions.md`; this history keeps only durable architectural guidance plus the latest high-value rulings.

## Recent Updates

### 2026-05-12T22:25:28Z — B4 review approved the projection and blocked the orchestration placement

- Reviewed Kramer-3’s B4 runtime path and approved the `EdgeProofStatus` data shape plus its regression coverage.
- Locked the remaining objection as the next action: proof-status enrichment/domain logic should not stay in `Compiler.cs` orchestration when the relevant edge-expansion knowledge already lives with `GraphAnalyzer`.

### 2026-05-12T14:57:13.598-04:00 — Field-state guarantee investigation

- Confirmed the compiler does not currently enforce state-scoped omit/readonly access during transition or state-hook action resolution.
- Locked the recommended fix shape as a post-resolution validation pass after access modes populate, with dedicated diagnostics instead of speculative state-blind enforcement changes.

### 2026-05-12T15:15:54-04:00 — Comma-list spike reframed and audited

- Captured both the full-scope design exploration and the later state-only/event-deferral revision in the decision ledger so the research trail preserves Shane’s direction changes.
- Exhaustive doc audit locked the critical parser-doc invariant update and confirmed no MCP code change is required because syntax output stays catalog-driven.

### 2026-05-12T17:56:47-04:00 — Hover B2/B3 are V1; B4 deferred

- B2 routing mismatch is a V1 blocker: `HoverHandler.cs` must try construct-span dispatch for rule/ensure/transition/reject/access/omit before generic operator/function/accessor fallbacks.
- B3 mutability honesty is a V1 scope correction, not guarded-access implementation: only unconditional `AccessModes` may feed V1 writable counts/state lists.
- B4 state proof missing-path narrative is deferred until `StateGraph` exposes a stable predecessor-edge explanation; V1 ships only the grounded two-line unreachable-state summary.
- B2/B3 still merge only against a green hover-test baseline.

## Learnings

### 2026-05-12T18:26:00-04:00 — Comprehensive spec accuracy audit

- Audited all 1865 lines of `docs/language/precept-language-spec.md` against source code.
- §0 Preamble line 26 still says "pipeline stages not yet built" — all stages are built; stale.
- §0.5 design contract claims analysis for 8+ modifiers (`guarded`, `entry`, `isolated`, `universal`, `milestone`, `sealed after`, `writeonce`, outcome-type modifiers) that do NOT exist in `ModifierKind` or `GraphAnalyzer.cs`. Only `terminal`, `required`, `irreversible`, and `initial` are implemented.
- §0.6 design contract has 3 unimplemented requirements (contradictory rule detection, vacuous rule detection, tautological guard detection) and 2 partial ones (assignment range impossibility, reachability sharpening).
- ProofEngine implements 6 discharge strategies but only 5 are documented; Strategy 6 (compositional constraint propagation) is undocumented.
- C48/C49/C50/C51/C52 notation in §0.5 does NOT match actual `DiagnosticCode` enum names or ordinals — misleading shorthand.
- §2.2 uses "Declaration" suffix on 6 ConstructKind names that the code doesn't have (e.g., `TransitionRowDeclaration` vs `TransitionRow`).
- §3.10 diagnostic groups table accounts for ~90 of 129 codes; 70+ undocumented in spec narrative.
- Grounding reference `docs/PreceptLanguageDesign.md` is a dead link — file doesn't exist.
- Token vocabulary (§1.1: 139 tokens), reserved keywords (§1.2: 102 keywords), and Set/SetType handling all verified accurate.
- Full findings written to `.squad/decisions/inbox/frank-spec-audit.md`.

### 2026-05-12T18:04:32.430-04:00 — Comma-list sample refresh

- The highest-value sample updates were pure-copy state subsets only: `HiringPipeline.RejectCandidate`, `ItHelpdeskTicket.RegisterAgent`, `ItHelpdeskTicket.Assign`, and `UtilityOutageReport.RegisterCrew`.
- I left rows expanded whenever either the guard or the outcome diverged (for example `InterviewLoop on RecordInterviewFeedback`), preserving the spike's "syntactic sugar only" contract.
- `precept_compile` was unavailable through MCP during this pass, so I validated the edited sample files through VS Code diagnostics instead; all three changed samples reported zero diagnostics.

### 2026-05-12T18:18:18.326-04:00 — Comma-list canonical doc sync

- The canonical documentation needed the semantics stated in two layers: the language docs must define `StateTarget := Identifier ("," Identifier)* | any`, and the compiler docs must separately spell out parser scanning plus pure-copy normalization.
- `docs/language/catalog-system.md` stayed correct without edits because it describes slot architecture, not `StateTarget` grammar; `docs/compiler/diagnostic-system.md` was already accurate and complete for S4.
- README now carries a minimal comma-list transition example so the public syntax surface shows the implemented subset-state shorthand rather than leaving it buried only in the spec.
