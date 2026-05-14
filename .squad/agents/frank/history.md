## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.
- Proof, qualifier, and field-state design work must stay grounded in shipped spec and evaluator/runtime surfaces, not inferred intent.

## Live Guidance

- `readonly` / `editable` / guarded access modes govern the Update (patch) surface; they do not restrict event-driven `set` actions unless the spec changes.
- Business-domain magnitude modifier legality is a catalog contract: fix drift in metadata and docs, not with checker-only special cases.
- Required-field constructor enforcement is now an active implementation surface: D93 and D94 are real checker obligations, while stateless construction stays outside the state-entry slice.
- Interval containment is the authoritative overflow-prevention mechanism for bounded arithmetic; obsolete `@bounds`, separate validator-phase, and runtime-fallback proposals are historical only.

## Historical Summary

- 2026-05-12 through 2026-05-13 concentrated Frank's work around hover contract reviews, comma-list `StateTarget` closure, field-state guarantees, modifier applicability drift, constructor diagnostics, and interval-proof design.
- Durable batch-by-batch detail now lives in `.squad/decisions.md` and `history-archive.md`; this live file keeps the newest conclusions and immediate guidance other agents need.
- Diagnostic coverage enforcement now uses two gates, negative tests are mandatory for gap closures, and catalog-mediated emission expansion stays selective.

## Recent Updates

### 2026-05-14T04:43:00Z — Final enforcement review corrected PRE0094 inventory drift and restored PRE0019 retirement annotation

- `docs/Working/diagnostic-enforcement.md` now reflects that PRE0094 already has two live emitters in `TypeChecker.Validation.FieldState.cs`, so the gap count is 49 rather than 50 and the emission inventory is 84 rather than 83.
- Slice 3 is now recorded as already wired instead of pending implementation work.
- The §3.7 D2 table again marks PRE0019 as retired, and the Elaine naming references plus Q1, Q10, and Q2 were rechecked with no new open questions.

### 2026-05-14T04:00:00Z — PRE0079 dead-code investigation resolved

- `docs/Working/diagnostic-enforcement.md` now carries the resolved PRE0079 verdict from Frank's investigation.
- PRE0079 stays a TypeChecker wire with zero live emitters today, distinct from PRE0078 because it covers the constant-literal-assignment case (for example, `set field to 42` where `max` is `10`).
- No new proof infrastructure is needed: the checker can compare the literal directly against declared `min`/`max` bounds, and the decision was filed to `.squad/decisions/inbox/frank-pre0079-investigation.md`.

### 2026-05-14T00:56:58Z — Q2 resolved for dynamic qualifier enforcement

- `docs/Working/diagnostic-enforcement.md` now marks Q2 as decided.
- The TypeChecker silently skips PRE0070–0074 cross-currency diagnostics when the qualifier is dynamic.
- Strategy 5 in the ProofEngine remains the enforcement point for dynamic qualifier validation.

### 2026-05-14T00:52:34Z — Q1/Q10 resolved for diagnostic enforcement

- `docs/Working/diagnostic-enforcement.md` now marks Q1 and Q10 as decided.
- PRE0079 is finalized as a TypeChecker-only literal-bounds diagnostic; PRE0078 remains the ProofEngine / Strategy 7 interval diagnostic.
- The plan now records the PRE0078/PRE0079 message text updates and the Q10 obligation-generation gate as separate from the Gate 1 allow-list timing.

### 2026-05-14T00:05:43Z — Diagnostic enforcement revised for interval-proof dependency

- `docs/Working/diagnostic-enforcement.md` now treats PRE0078 `NumericOverflow` as a Strategy 7 / ProofEngine obligation failure instead of a Slice 8 TypeChecker wire.
- The plan now records Slice 9C's dependency on interval-engine Slice 2, narrows PRE0079 to the constant-literal overflow case, and adds the cross-plan dependency table plus Q9/Q10 coordination risks.

### 2026-05-13T18:17:15Z–2026-05-13T20:05:43Z — Interval-proof design and enforcement alignment consolidated

- Frank locked Strategy 7 (`IntervalContainment`) as the bounded-expression design surface, corrected the interval test-strategy narrative, and revised the overflow-prevention analysis to point at the approved proof-engine design rather than obsolete validator/runtime proposals.
- The paired enforcement-plan update kept PRE0078 on the ProofEngine path, narrowed PRE0079 to constant-literal assignments, and recorded the cross-plan dependency between diagnostic enforcement Slice 9C and interval-engine Slice 2.


## Learnings

- Q4 PRE0091 AmbiguousTypedConstant decision recorded: narrow first tranche (temporal quantity ambiguity only) validated by architecture — `ResolveTypedConstant` receives `expectedType`, making multi-candidate enumeration speculative infrastructure for error-recovery paths only.
- If a design depends on an existing diagnostic, confirm the emitting pipeline stage instead of trusting `DiagnosticMeta` declarations or spec prose.
- Gate 2 is a tripwire, not proof of behavioral correctness; every gap closure still needs both positive and negative tests.
- Stale exploratory docs must be revised promptly once an implementation-spec path is approved, or they become misleading guidance for downstream agents.
- `set` into an `omit` target-state field is the decisive field-state rule; Update access modes do not constrain Fire semantics.
- Catalog static initialization may not reach downstream catalog statics during cctor execution; reverse references should defer through `Lazy<T>`.
- Before using a diagnostic as an ordering dependency, verify whether the checker wiring already shipped; stale sequencing premises should not override integrity-first prioritization.
- Gate 1 allow-list entries only need root-cause cluster comments; per-issue citations duplicate slice-level tracking when codes cannot slip independently from their cluster.
- Gate 2's emission-site analyzer should stay on an explicit pipeline-centered scan set; broad all-source scans create more false-positive risk than value while the known emission paths remain stable and well-defined.
- Strip `// ...`, `/** */`, and `/// ...` comment content before Gate 2 matches `DiagnosticCode.*`; the implementation cost is small and removes the doc-comment false-positive class up front.
- When Gate 1 stale-entry enforcement can prove an allow-list entry is obsolete, the feature PR that introduces the new emission site should own the allow-list removal; dependent plans should also record an explicit review checkpoint so the cross-plan cleanup is verified.
- Q10 PRE0078/PRE0079 deduplication is subsumed by Question 1: once PRE0079 owns constant-literal bounds checks in the TypeChecker and PRE0078 owns expression-result interval failures in ProofEngine Strategy 7, the pipeline boundary itself prevents double-firing without a separate gate.
- Diagnostic name & message UX review (Elaine's audit): approved with conditions. Six of seven proposed conventions adopted as standards. `CannotVerify*` prefix rejected in favor of condition-first names (`FieldMayBeAbsent`, `ModifierNotGuaranteed`, etc.). CI enforcement family renamed to `CaseMismatch*` convention. PRE0019 rename rejected as too narrow — must cover full emission surface. `StateWithNoWayOut` rejected as too colloquial — `NonTerminalDeadEnd` adopted.
- Proof diagnostic naming convention established: names describe the *state of the author's definition* (condition-first), not the outcome of the compiler's proof attempt. No `CannotVerify*`, `Unproved*`, or `FailedToProve*` prefixes.
- AI-parseable message structure convention (Convention 8): Subject → Condition → Repair (em-dash separated). Constrains new and rewritten messages.
- Convention 9: Rename PRs must update FixHint, RecoverySteps, TriggerCondition, and examples in the same commit when old vocabulary appeared in those fields.
- `DiagnosticCode` renames are structurally safe: `nameof()` in `GetMeta()` auto-propagates to catalog; compiler catches stale test references. Manual update needed for Gate 1 allow-list and language server switches.
- 2026-05-13T22:41:11.432-04:00 — PRE0019 emission-site audit found no live emitters in `src/Precept/`; the code is declaration/metadata-only today. Current maybe-absent failures route through `TypeMismatch` or PRE0116, so the correct future-facing rename is `ValueMayBeAbsent`, not an optional-field-specific `when` diagnostic.
- 2026-05-14T02:53:55-04:00 — PRE0019 wiring investigation: **do not wire now.** The enforcement doc's "TypeMismatch fires today; upgrade" premise is stale — no TypeMismatch fires for presence cases. PRE0116 `UnprovedPresenceRequirement` (ProofEngine) already covers presence checking via proof obligations with guard-aware, strategy-based discharge. Wiring PRE0019 in the TypeChecker would either duplicate PRE0116 or require duplicating the ProofEngine's guard-context infrastructure (~50-100 lines). The correct prerequisite before wiring is: audit whether the operation catalog's `PresenceProofRequirement` generation has any coverage gaps. If no gap exists, PRE0019 is architecturally subsumed and should be retired, not wired. Enforcement doc corrected in place; decision filed.
- 2026-05-13T23:05:12-04:00 — PRE0019 wire-vs-retire audit: **RETIRE.** Full audit confirmed PRE0019 is architecturally subsumed by PRE0116. Key finding: `new PresenceProofRequirement(...)` is never constructed in production code — no catalog entry (operations, functions, accessors, actions) declares one. The entire presence-proof discharge pipeline (type, satisfactions, Strategies 2/3/5, PRE0116 emission) is fully plumbed but receives zero obligations. The gap is obligation *generation*, not diagnostic codes. The fix: enhance ProofEngine's expression walker to inject `PresenceProofRequirement` when `TypedFieldRef` references an optional field, routing through existing PRE0116. PRE0019 is `DiagnosticStage.Type` — wrong stage for guard-aware checks. Decision filed to `.squad/decisions/inbox/frank-pre0019-wire-vs-retire.md`; enforcement doc updated (removed PRE0019 from deferred table, Slice 8 checklist, non-catalog-mediation table; updated Q5 with addendum).
- 2026-05-13T23:22:05-04:00 — Comprehensive ProofEngine gap audit completed. Audited all 7 ProofRequirement subtypes, all ProofSatisfaction variants, all 7 strategies, all 13 FaultCode `[StaticallyPreventable]` attributes, and all proof-family diagnostic codes. **One critical gap:** `PresenceProofRequirement` never constructed in production — discharge pipeline fully plumbed, zero obligations feeding it. Added as Slice 12 to `docs/Working/interval-proof-engine-design.md`. **One moderate metadata gap:** `FaultCode.UnexpectedNull` → PRE0019 should be → PRE0116; fixed in Slice 12. **Two pre-tracked dead codes:** PRE0019 (retirement per frank-16), PRE0079 (diagnostic enforcement plan Slice 9C). Old Slice 12 renumbered to Slice 13. Coverage matrix updated with presence row. Test recommendation: write tests WITH Slice 12, not before — project convention writes tests in-slice, and the gap is structural not ambiguous. Decision filed to `.squad/decisions/inbox/frank-proofengine-gap-audit.md`.
- 2026-05-13T23:29:59-04:00 — PRE0079 `OutOfRange` dead-code investigation: **WIRE via TypeChecker.** Emission-site audit confirmed zero live emitters — `DiagnosticCode.OutOfRange` exists only in enum, catalog metadata, `FaultCode` attribute, and fault metadata. Unlike PRE0019, PRE0079 is NOT architecturally subsumed: PRE0078 `NumericOverflow` (ProofEngine Strategy 7) covers expression-level bounds, but PRE0079 covers the distinct constant-literal-assignment case where the literal value is known at compile time. These are complementary diagnostics at different pipeline stages (TypeChecker vs. ProofEngine), with different recovery actions and AI dispatch keys. Pipeline ordering prevents double-firing (Q10 resolved). Implementation is straightforward: check numeric literal assignments against field `min`/`max` modifiers in the TypeChecker's action validation. Remains in enforcement doc Deferred list with confirmed annotation. Decision filed to `.squad/decisions/inbox/frank-pre0079-investigation.md`.
- 2026-05-13T23:05:12-04:00 — Full Elaine sync: applied all approved diagnostic name and message changes from `docs/Working/diagnostic-name-message-review.md` to `docs/Working/diagnostic-enforcement.md`. Updated ~40 references across 8 sections (gap inventory tables §3.4–3.7, alternatives §5.4, Slices 4/5/6/7/8 details, deferred list, Q5 decision block). Name families updated: `*WithoutWhen` collection safety (5 codes), `CaseMismatch*` CI enforcement (5 codes), condition-first proof names (5 codes: `ModifierNotGuaranteed`, `DimensionQualifierMissing`, `QualifiersMayBeIncompatible`, `InitialStateConstraintUnsatisfied`, `FieldMayBeAbsent`), plus individual renames (`ChoiceValueOrderMismatch`, `CollectionElementTypeMismatch`, `ValueNotInChoiceSet`, `TransitionGuardMustFollowEvent`, `FunctionArgumentInvalid`, `NonTextTypeInStringInterpolation`, `UnrecognizedTypedConstant`, `NonTerminalDeadEnd`, etc.). Test method names in slice specs updated to match. PRE0019 left untouched per prior retirement decision. No surprises — mechanical sync, all references found via grep.
- 2026-05-13T23:38:20-04:00 — Final holistic review of `diagnostic-enforcement.md`. **One material finding:** PRE0094 `InitialEventMissingAssignments` has two live emission sites in `TypeChecker.Validation.FieldState.cs` but the gap inventory, Slice 3, emission counts (50→49, 83→84), and allow-list references all treated it as unemitted. Q3's decision noted the implementation but the doc was never updated. Corrected gap count (50→49), annotated §3.6, struck Slice 3, updated tracker. Also annotated §3.7 D2 table entry for PRE0019 as RETIRED (was listed as an active gap without annotation). All Elaine-approved names cross-checked — no missed renames. PRE0019/PRE0079 sections accurate. Q1/Q10 decisions correctly reflected. Decision filed.

