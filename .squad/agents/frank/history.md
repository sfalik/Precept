## Core Context

- Owns architecture, system boundaries, and review gates across the runtime, tooling, and documentation surfaces.
- **Language Designer:** Owns Precept language design, grammar evolution, and Superpower parser strategy. `docs/PreceptLanguageDesign.md` is the foundational document.
- **Language research co-owner:** Co-owns `docs/research/language/` with George. Knows the comparative studies (xstate, Polly, FluentValidation, Zod/Valibot, LINQ, FluentAssertions, FEEL/DMN, Cedar), expression audit, verbosity analysis, and all PLT references.
- Core architectural discipline: keep MCP tools as thin wrappers, keep docs honest about implemented behavior, and document open decisions instead of inventing values.
- Technical-surface work flows through Elaine (UX), Peterman (brand compliance), Frank (architectural fit), then Shane (sign-off).
- README and brand-spec changes should reflect actual runtime semantics, not speculative future behavior.

## Recent Updates

### 2026-04-12 — Event hooks gap investigation + external FSM precedent survey
- Researched event-level action hooks triggered by Shane's `on Advance -> set Count = Count + 1` parse error.
- Filed full external precedent survey across 5 systems (XState v5, SCXML, Akka Classic FSM, Spring SM, Redux) in `research/language/expressiveness/event-hooks.md`.
- **Stateless case (Issue A):** CONFIRMED VIABLE. Zero Principle 7 tension — stateless precepts have no transition rows, so the shared-context concern that motivated Principle 7 has no application surface.
- **Stateful case (Issue B):** Deferred as Issue B. Three unresolved questions: execution order position (Frank recommends Option 3 — after row mutations, before exit actions — citing SCXML §3.13 as normative precedent), outcome-scoping (fires on Unmatched?), and explicit Principle 7 exception rationale.
- C49 revision required for Issue A: events with hooks suppress C49; events with asserts but no hooks get lower-severity warning.
- Confirmed `ActionChain` reuse: `EventActionDecl` shares the existing parser combinator, no changes needed.



### 2026-04-10 — Structural lifecycle modifiers: second pass (expanded modifier space)
- Produced comprehensive second pass at `research/language/expressiveness/structural-lifecycle-modifiers.md`.
- **Scope expansion:** Added 10 event modifier candidates (`entry`, `advancing`, `settling`, `irreversible`, `universal`, `isolated`, `guarded`, `total`, `completing`, `symmetric`), 8 field modifier candidates (`writeonce`, `identity`, `monotonic`, `sensitive`, `derived`, `immutable`, `sealed after <State>`, `audit`), and 6 additional state modifier candidates (`error`, `resting`, `decision`, `guarded` on incoming transitions, `absorbing`, `convergent`).
- **Key insight reversal:** First pass concluded "event modifiers constrain behavioral properties requiring runtime state." Second pass disproves this — events have rich graph-structural properties (source scope, outcome shape, target scope) that are fully compile-time provable from declared transition rows. The error was conflating "event modifier" with "event firing-history modifier."
- **Strongest new candidates (Tier 1 additions):** `entry` (intake event pattern), `advancing`/`settling` (routing vs mutation dichotomy), `isolated` (single-state scope), `writeonce` (intake-data immutability), `sealed after <State>` (lifecycle-phase freeze point), `guarded` on states (entry safety).
- **Most impactful discovery:** The `advancing`/`settling` split is the single most valuable modifier concept — it captures a fundamental domain distinction (routing events vs data-collection events) visible in every sample.
- **Cross-cutting findings:** Four modifier roles identified (structural constraint, intent declaration, tooling directive, feature gate). Recommended Option C: modifiers must be either compile-time verifiable OR tooling-actionable.
- **Philosophical question surfaced:** Does the project accept tooling-directive modifiers (`sensitive`, `audit`) or restrict to compile-time-provable properties? Recommended Option C as position; flagged for Shane's decision.
- Updated recommendation tiers: 8 Tier 1 candidates, 14 Tier 2 candidates, 12 Tier 3 (reject/defer).

### 2026-04-10 — Structural lifecycle modifiers research
- Produced comprehensive research document at `research/language/expressiveness/structural-lifecycle-modifiers.md`.
- **Taxonomy:** Mapped 5 modifier categories (boundary, path, residency, cardinality, safety) covering 6 candidate modifiers: `terminal`, `required`/`milestone`, `transient`, `final`, `once`, `idempotent`.
- **Survey:** Compared 8 systems (XState, Step Functions, BPMN, Stateless, TLA+/Alloy, Temporal, UML, Cedar/OPA). Terminal/final states are the only modifier with broad cross-system precedent.
- **Tiers:** `terminal` is Tier 1 (strong candidate — compile-time provable, clear `initial` parallel, zero runtime cost). `required`/`transient` are Tier 2 (interesting but guard-dependent false positives or weaker-than-expected guarantees). Event modifiers (`final`, `once`, `idempotent`) are Tier 3 (reject/defer — require runtime state or break row independence).
- **Key insight:** State modifiers constrain graph-structural roles (compile-time provable). Event modifiers constrain behavioral properties (need runtime tracking). The asymmetry is fundamental, not incidental.
- **Domain taxonomy decision:** Created new domain "Structural lifecycle modifiers" (domain #23 in domain-map). Not an extension of existing domains — it adds *new declarations* that create new provable properties, distinct from constraint composition (value constraints), static reasoning (proving from existing declarations), and entity modeling (data shape).
- Updated `research/language/README.md` domain index and horizon domains list. Updated `research/language/domain-map.md` with domain #23 entry and priority assessment.

### 2026-04-10 — Issue #31 shipped
- PR #50 merged to main (squash SHA `305ec03`). Issue #31 closed. 775 tests passing.

### 2026-04-10 - PR #50 final review — Issue #31 keyword logical operators

**Verdict: APPROVED.** All 8 slices verified. 774/774 tests passing. Full review filed at `.squad/decisions/inbox/frank-31-review.md`.

**Key architectural confirmation:** `BuildKeywordDictionary()` uses `symbol.All(char.IsLetter)` to auto-include `and`/`or`/`not` via reflection. Registered with `requireDelimiters: true` — whole-word safety against prefixes like `android` is architectural, not ad-hoc. Consistent with `contains` precedent.

**ApplyNarrowing() confirmed updated** — all three arms (`"not"`, `"and"`, `"or"`) correctly updated at their respective pattern-match sites.

**MCP is catalog-driven** — zero hardcoded operator strings. `TokenSymbol` attribute update is the only change needed; LanguageTool picks it up automatically.

**Minor non-blocking gap:** `LanguageToolTests.LogicalOperatorsAreKeywordForms` asserts `&&` and `||` are absent, but does not assert `!` is absent. Not blocking because `!` was removed from the tokenizer entirely and has no path to the inventory.

**Learnings:**
- The attribute-driven `BuildKeywordDictionary()` + `requireDelimiters` is the correct mechanism for any future operator-to-keyword migrations. No tokenizer code changes are needed — only the `[TokenSymbol]` attribute value.
- When reviewing logical operator migrations, `ApplyNarrowing()` is the easy-to-miss site — it uses pattern-match syntax (`{ Operator: "..." }` and string comparison) separately from the main type-checker switch. Both were correctly updated here.

### 2026-04-10 - Issue #31 Slice 8 docs — PreceptLanguageDesign.md sync
- Updated `docs/PreceptLanguageDesign.md` on branch `squad/31-keyword-logical-operators` to reflect #31 as fully implemented.
- **Sections updated:** (1) "Keyword vs Symbol Design Framework" — migration table heading changed from "Implementation Pending" to "Implemented", table columns renamed from "Current/Target/Pending" to "Symbolic/Keyword/Implemented", removed "Until implementation" paragraph; (2) Reserved keywords list — removed "Pending additions (#31)" note, added `and`, `or`, `not` to the main keyword list; (3) Nullability and narrowing — updated Pattern 1 and Pattern 2 code examples from `&&`/`||` to `and`/`or`, updated prose labels and inline descriptions to match; (4) Expressions section — removed `pending migration` annotations, updated `!` → `not` and `&&`/`||` → `and`/`or` in bullet list; (5) Event asserts section and Minimal Example — updated `&&` → `and` in both code examples.
- Committed as `03d50b7`, pushed to origin. PR #50 Slice 8 docs checkbox now checked.

### 2026-04-10 - Philosophy Refresh Assessment — full research corpus review
- Systematically reviewed all 32 files in `docs/research/language/` against the rewritten `docs/philosophy.md` (commits `dd14a4c` entity-first rewrite, `185e20b` governance strengthening).
- Assessment written to `docs/research/language/philosophy-refresh-assessment.md`.
- **Findings:** 16 files aligned, 14 need framing updates, 2 need significant refresh (`xstate.md`, one framing gap). No file contradicts the philosophy. Two cross-cutting problems dominate: (1) zero files use "governed integrity" — the philosophy's unifying principle; (2) 14 files don't consider data-only/stateless entities.
- Identified 4 new research gaps: constraint patterns for data-only entities, Inspect/Fire semantics for stateless instances, governance-vs-validation positioning evidence, MDM/industry-standard comparison depth.
- Recommended 16-file refresh order prioritized by impact × dependency.
- Quality bar (`computed-fields.md`) confirmed as excellent research but needs minor framing update for governed integrity language and stateless entity implications.
- Gold standard files: `entity-first-positioning-evidence.md`, `entity-modeling-surface.md`, `static-reasoning-expansion.md`.

### 2026-04-08 - PR #48 code review — data-only precepts (Slices 1–7)
- Reviewed `feature/issue-22-data-only-precepts` (commits e0eac05–833422e) against all 12 Q&A design decisions from `.squad/decisions/decisions.md`.
- **Verdict: CHANGES REQUESTED.** All 12 decisions faithfully implemented (11 PASS, 1 ISSUE — Decision 11 sample files absent). Architecture is sound; no silent drift from design.
- **Blocking issue:** `docs/PreceptLanguageDesign.md`, `docs/RuntimeApiDesign.md`, and `docs/McpServerDesign.md` are entirely missing stateless-precept documentation. This violates the project's non-negotiable doc-sync mandate.
- **Required changes:** (1) Update three design docs before merge. (2) Add multi-event C49 test (Decision 10 contract untested: 3 events → 3 separate warnings).
- **Code quality nits:** `DiagnosticCatalog.C55.MessageTemplate` used instead of `FormatMessage()` at PreceptRuntime.cs:1824; `currentState!` null-forgiving in MCP tool stateful branches; null-forgiving on `InitialState!` in `CollectCompileTimeDiagnostics` relies on implicit guard.
- **Recommendations (non-blocking):** Add explicit `IsStateless` guard before "2. Validate initial state asserts" block; add at least one sample `.precept` file (Decision 11 placeholder names are fine); add `in State edit all` engine test; replace `MessageTemplate` with `FormatMessage()` on C55.
- Full review filed at `.squad/decisions/inbox/frank-pr48-review.md`.

### 2026-04-08 - Issue #22 semantic rules review (4 spawns)
- Reviewed and rewrote #22 semantic rules for stateless precepts across 4 spawns: (1) decomposed the "states, events, and transitions forbidden" rule — states tautological, transitions structurally impossible, events the only real boundary; (2) deep analysis of stateless event boundary — confirmed binary taxonomy (data vs. behavioral), no middle tier, single-state escape hatch is correct not ceremonial; (3) warning model research — audited C48–C53 diagnostic infrastructure, external precedent survey; (4) full #22 issue body rewrite.
- **Owner decision (Shane):** events-without-states = warning (not error). C50 upgraded from hint to warning for consistency. Binary taxonomy confirmed.
- Wrote `frank-stateless-event-boundary.md` and `frank-warning-model-research.md` to decisions inbox; both merged into decisions.md by Scribe.

### 2026-04-08 - Language research corpus completed
- Closed the domain-first corpus with the final sweep in `3cc5343`, keeping `docs/research/language/domain-map.md` as the canonical map and leaving proposal bodies untouched.
- The finished corpus now spans Batch 1 `54a77da`, Batch 2 `48860ae`, and the final corpus/index pass `3cc5343`, while preserving `computed-fields.md` as the quality bar and keeping horizon domains visible.

### 2026-04-08 - Language research corpus map locked
- Preserved `docs/research/language/domain-map.md` as the canonical research-corpus map and kept the corpus organized by domain rather than by issue.
- Held the session guardrails: no proposal-body edits during corpus curation, `computed-fields.md` remains the quality bar, and horizon domains stay in scope even before proposals exist.
- The alternate map-file experiment was folded back out; Batch 1 research landed in `54a77da`, Batch 2 landed in `48860ae`, and only Batch 3 plus the final README/index sweep remain.

### 2026-04-07 - PR #35 merge: finalize README cleanup and record Squad decision
- Merged PR #35 (chore: finalize README cleanup and record Squad decision) to `main`.
- Branch `chore/upgrade-squad-latest` carried 2 commits: (1) Scribe post-task recording for PR #34 merge, and (2) README Quick Example refactoring (removed explanatory hedge, copyable DSL block, replaced markdown image syntax with fixed-width HTML img tag).
- Cleaned up merged inbox entries from `.squad/decisions/inbox/` and updated `.squad/agents/j-peterman/history.md` with team update.
- Workflow: Clean working tree before push (separated uncommitted Squad sync artifacts from committed README cleanup work), explicit PR creation with `gh pr create --base main --head sfalik:chore/upgrade-squad-latest`, merged with merge-commit strategy.
- User directive honored: branch retained locally and remotely per user request (NOT deleted post-merge).
- Verified zero scope creep: 81 additions, 3 files changed (README.md, .squad/decisions.md, .squad/agents/j-peterman/history.md), no unrelated code changes.
- Co-authored-by trailer included in original commits.

### 2026-04-07 - PR #34 merge with Squad config and README image fixes
- Merged PR #34 (chore: upgrade Squad configuration and fix README image links) to `main`.
- Branch `chore/upgrade-squad-latest` carried both architectural Squad updates and related README.md image path corrections.
- Image references corrected: `brand/readme-hero.svg` → `design/brand/readme-hero.svg` (added `design/` prefix per canonical asset layout).
- Workflow: Committed image fix with Co-authored-by trailer, pushed with `-u` for upstream tracking, created PR via `gh pr create` with explicit `--base` and `--head` flags, merged with merge-commit strategy, deleted remote branch post-merge.
- Remote branch cleaned up post-merge; no uncommitted changes remaining.
- User directive captured: keep branch open for future work (logged in decisions).
- Verified zero scope creep: only Squad config + image link fixes, no unrelated code changes.

### 2026-04-05 - Named rule proposal converged
- Reached the final proposal framing for issue #8: rule <Name> when <BoolExpr>, with reuse allowed in when, invariant, and state assert, but not on <Event> assert.
- The standing architecture filter now treats philosophy, non-programmer readability, and configuration-like legibility as explicit review criteria instead of secondary polish.

### 2026-05-01 - Expression feature research & proposals
- Produced comprehensive expression-surface research at `docs/research/dsl-expressiveness/expression-feature-proposals.md`.
- Confirmed current expression limitations via MCP compile: no ternary (`?` parse error), no `.length` on strings (PRECEPT038), no function calls, no named guards.
- Proposed 7 features across 3 waves: Wave 1 (ternary + `.length`), Wave 2 (named guards + conditional invariants), Wave 3 (`.contains()` + numeric functions), Future (computed fields).
- Extended research base beyond existing 6-library set to include FEEL/DMN, Drools DRL, and Cedar (AWS). FEEL is the strongest comparator for business-rule DSL expression design — it has ternary, string functions, numeric functions, and range membership, all of which Precept currently lacks.
- Updated `docs/research/dsl-expressiveness/README.md` with the new file entry.
- Decision note filed at `.squad/decisions/inbox/frank-expression-research.md`. No implementation authorized — Shane sign-off required per wave.

### 2026-04-05 - Proposal bodies expanded for issues #11-#13
- Expanded GitHub issues #11, #12, and #13 into fuller proposal narratives with before/after Precept examples, reference-language snippets, and explicit architectural cautions.
- Logged the wave placement and guardrails in .squad/decisions.md so the issue bodies stay aligned with keyword-anchored flat statements and first-match routing.

### 2026-04-05 - Trunk consolidation dissent logged
- Audited the repo topology and argued for force-promoting 'feature/language-redesign' to 'main' because 'main' still carries only placeholder history.
- The team did not adopt that path: Uncle Leo's review blocked direct trunk replacement, so Frank's recommendation now stands as a documented dissent pending Shane sign-off.

---

2026-04-05T03:20:00Z: Steinbrenner applied branch protection to main (pull requests required, force pushes/admin only, no branch deletion).

### 2026-04-08 - Issue #17 computed fields proposal revamp
- Revamped `temp/issue-body-rewrites/17.md` to incorporate all findings from the research document and all 7 team re-reviews.
- Resolved all 6 semantic contracts from the research: scope boundary, recomputation timing (Fire+Update+Inspect), nullability/accessor safety, dependency ordering, writeability/external input, tooling surface.
- Used Steinbrenner's default-if-skipped options: (a) conservative nullable rejection, (a) `.count` only for collection accessors, (a) Terraform-model external input rejection, (a) single pass after all mutations.
- Added 3 new locked decisions (#9 nullable conservative rejection, #10 external input rejection, #11 `.count`-only collection accessor restriction).
- Expanded locked decision #8 from Fire-only to all three pipelines citing George's `CommitCollections` analysis.
- Added `->` dual-use acknowledgment note to locked decision #1.
- Replaced "Open Questions: None" with a "Resolved decisions" table mapping all 6 research contracts to locked decisions.
- Changed wave from "Wave 4: Compactness + Composition" to "Composition" framing per Steinbrenner's recommendation.
- Added new sections: Tooling surface (hover, MCP compile, inspect/fire, completions), Teachable error messages (9 scenarios), Before/after example using travel-reimbursement, Interleaved fields examples, `->` reading guide callout.
- Surfaced the research's cross-category positioning claim in Motivation section.
- Added spreadsheet mental model sentence to Summary.
- Linked research document in Research and rationale links.
- Expanded Explicit exclusions with: silent default fallbacks, writable computed fields, null-coalescing operator, unsafe collection accessors. Merged fixed-point evaluation with cycle detection.
- Expanded acceptance criteria from 19 to 37 items, organized by category (parser, type checker, runtime, LS, MCP, general). Added all items from Soup Nazi's must-add and should-add lists plus Uncle Leo's blockers.
- Expanded implementation scope from 10 to 16 items covering nullable validation, accessor safety, Update pipeline, external input rejection, LS filtering, MCP specifics.

### 2026-04-08 - Issue #22 semantic rule review for Shane
- Reviewed the "states, events, and transitions forbidden in stateless precepts" semantic rule at Shane's request before implementation.
- Confirmed Shane's critique that the "states forbidden" and "transitions forbidden" parts are tautological/redundant — the real design decision is the events boundary.
- The issue body already acknowledges the tautology in the exclusions section ("this exclusion is tautological") but contradicts itself in the semantic rules section by framing it as a prohibition. Recommended rewrite to eliminate the contradiction.
- Key architectural insight: the only non-trivial semantic boundary is events. Events are forbidden because they dispatch transitions between states (a semantic dependency), not because of a tautology or structural impossibility. This is a real design choice with a potential future evolution path (stateless event-triggered mutations), deliberately closed for now.

### 2026-04-08 - Stateless event boundary deep analysis
- Shane flagged the revised #22 semantic rules as "a slippery slope." Conducted deep analysis of whether events should be forbidden in stateless precepts.
- **Recommendation: events require states. The binary taxonomy (data vs. behavioral) is correct.** The "middle tier" (data + events, no states) is a category error, not a missing feature.
- Key insight: events in Precept are not generic triggers — they are routed through states via `from State on Event -> Outcome`. The `from` is the routing context, not decoration. Events without routing are a different concept ("commands") that would need parallel syntax, semantics, and tooling.
- The single-state escape hatch (`state Active initial`) is correct, not ceremonial: it communicates "this entity has exactly one behavioral mode" — a true structural statement, not dummy ceremony. Cost: 1 line.
- Precedent survey confirms no surveyed system (Terraform, SQL, DDD, GraphQL, Protobuf) provides "data entity with named commands but no lifecycle." Every system with named commands routes them through a behavioral layer.
- The slippery slope concern is acknowledged but self-correcting: the escape hatch is cheap enough that demand for stateless events won't reach critical mass.
- Decision filed at `.squad/decisions/inbox/frank-stateless-event-boundary.md`.

## Learnings

### 2026-04-11 - Issue #14 final approval — all 4 forms as unified wave

- Produced definitive unified design sign-off for all four `when <guard>` forms (invariants, state asserts, event asserts, conditional edit eligibility) as a single implementation wave. Filed at `.squad/decisions/inbox/frank-issue14-final-approval.md`.
- **All 4 forms APPROVED.**
- **Semantic tension between Forms 1–3 (constraint-skip) and Form 4 (permission-grant) is not an inconsistency.** The directionality difference follows from the nature of the two declaration types: constraints default to applying everywhere and are scoped downward; edit permissions default to denied everywhere and are granted upward. `when <guard>` means "conditional on this boolean" in all cases — what "conditional" implies operationally follows the declaration kind. Teaching model holds uniformly.
- **Fail-closed is non-negotiable for Form 4 and must be explicitly contracted in the issue.** Guards on edit blocks are permission grants. Granting access on evaluation error = fail-open permission system = never acceptable. Must specify: guard evaluation error → treat as false → field not granted.
- **Scope-inherited conditioning holds cleanly for all 4 forms.** Form 4 edit guards are definitionally field-scoped — there is no current event during `Update`, so event args cannot be referenced. The distinction does not blur anywhere. Type checker must emit diagnostic for any edit guard that references event-arg identifiers.
- **New prerequisite identified for Form 4: Elaine coordination on dynamic editability.** With guarded edit blocks, the editable field set can change as data values change (guard truth changes). The preview inspector already handles dynamic field lists, but UX treatment of fields appearing/disappearing mid-edit must be explicitly designed before Form 4 ships.
- **Hydration reorder (moving `HydrateInstanceData` to start of `Update` and `Inspect(patch)`) is the primary regression risk** when including Form 4. George's analysis confirms it's a structural rearrangement with no semantic change (pure read). Risk is bounded and acceptable.
- **The deferral was scope-splitting, not design rejection.** Form 4 is the same design principle as Forms 1–3 — the same `when <guard>` conditional semantics applied to a different declaration kind. The additive approach (static dict unchanged, guarded blocks in separate structure evaluated at call time) dissolves the original structural obstacle. No new conceptual risks.

### 2026-04-11 - Form 4 additive approach design review

- Analyzed Shane's proposed additive approach for `in State when <guard> edit <fields>` (Form 4, Issue #14).
- **Verdict: additive approach is semantically sound. No design-level concerns justify continued deferral.**
- Conjunctive semantics (in-state AND guard-true) are preserved by the two-pass union: unconditional fields from static dict + guarded fields from per-call guard evaluation. Union doesn't undermine conjunct because state filter is applied before guard filter.
- **Guard failure → fail-closed** is the correct default. Guards on edit blocks are permission grants, not constraint gates. Fail-open on error would be a broken permission system. Must be explicitly specified in the follow-on issue.
- **`in any when <guard> edit`** — use the same construction-time expansion that unconditional `any` already uses. No sentinel handling needed at call sites. Consistent with existing pattern.
- UneditableField message disambiguation (why the field is not editable: not-declared vs. wrong-state vs. guard-false) is an implementation quality decision, not a design requirement. Not a deferral criterion.
- **The deferral was always implementation-scoping, not design.** Form 4 is semantically identical to Forms 1–3 — the same `when <guard>` conditional semantics applied to a different declaration kind. The static dict was the only structural obstacle; the additive approach removes it without touching the existing path.
- Three spec requirements must appear in the follow-on issue: (1) fail-closed guard evaluation contract, (2) `any` pre-expansion strategy, (3) Elaine coordination on dynamic editability (fields appear/disappear with guard truth value — intended behavior, UX contract needs to handle it).
- Filed: `.squad/decisions/inbox/frank-issue14-form4-design.md`.

### 2026-04-08 - Warning model research for structurally degenerate precepts
- Audited the full diagnostic infrastructure: `ConstraintSeverity` enum (Error/Warning/Hint), `DiagnosticCatalog.cs` constraints C48–C53, `PreceptAnalysis.cs` graph analysis, and the language server's `MapValidationDiagnostic` severity mapping. All three tiers are fully wired and operational.
- The type checker (`PreceptTypeChecker.cs`) does zero reachability analysis — all graph-level structural analysis lives in `PreceptAnalysis.cs` as a separate analysis phase.
- Single-state precepts with events and no state-changing transitions already trigger **C50 (hint)**: "State 'Active' has outgoing transitions but all reject or no-transition — no path forward." This is the correct severity — hint, not warning — because the pattern is legitimate (counters, rate tables, config entities with audit ops).
- External precedent strongly supports Precept's existing model: C#/Rust/TypeScript treat unreachable code as warnings; state machine tools either don't detect unreachable states or flag as warnings; SQL is silent on degenerate constraints. No surveyed system errors on structurally degenerate but type-correct constructs.
- **Key conclusion:** The existing diagnostic infrastructure already handles Shane's scenario correctly. No changes needed to severity assignments. The "events require states" hard error is correct (category boundary), and the single-state escape hatch produces a hint (C50) rather than silence. The system is honest without being punitive.
- Decision filed at `.squad/decisions/inbox/frank-warning-model-research.md`.

### 2026-04-08 - Proposal revamp process
- When research identifies semantic contracts and multiple reviewers converge on the same gaps, the work is bounded editing rather than rethinking. The 7 reviews all pointed to the same 3-4 core gaps — the volume of reviews was higher than the volume of actual decisions needed.
- Steinbrenner's "default-if-skipped" pattern is valuable: each gate decision has a clearly stated default, so the editing pass can proceed without blocking on explicit Shane sign-off for each one.

### 2026-04-08 - Rationale pass on computed fields proposal
- Added "Why" rationale to all 11 locked design decisions in `temp/issue-body-rewrites/17.md`. Each decision now includes alternatives considered, precedent citations, tradeoff accepted, and the philosophy principle it serves.
- Incorporated Shane's 3 specific design points:
  1. **Non-nullable inputs (#9):** Documented that Precept is stricter than all 24 surveyed systems. Those systems all have null-handling operators (COALESCE, BLANKVALUE, etc.); we don't — so conservative rejection is the only sound choice. Practical impact near zero (all numeric fields in samples are non-nullable with defaults).
  2. **No constraints on computed values (#4):** Clarified that invariants already CAN reference computed fields, which covers output validation. Field-level constraints are the wrong abstraction because they validate externally-supplied data — computed fields have no external supply path. Determinism isn't threatened either way; the choice is about mechanism clarity.
  3. **Inspect recomputation (#8 + semantic rules):** Reworded to clarify that Inspect operates on a clone — recomputation is simulated on a working copy, consistent with how Inspect already simulates `set` mutations. Skipping recomputation would break Inspect's preview contract (disagreement with Fire on constraint results).
- Also added rationale to semantic rules section and expanded exclusions with brief "because" statements.
- Key learning: proposals that state WHAT without WHY are incomplete. The research doc had all the evidence — the gap was in surfacing the key "because" statement at each decision point in the proposal itself.
- Error message tables are a force multiplier: each row simultaneously specifies a compile error, documents the user-facing UX, and implicitly defines an acceptance criterion. Elaine was right that these are the primary learning surface.

### 2026-05-18 - Philosophy draft terminology correction
- Corrected `design/brand/research/philosophy-draft-v2.md` to use actual DSL construct names in concept lists: fields, states, events, invariants, and assertions. `guard` remains acceptable only as informal prose or as the inline `when` condition concept, not as a top-level declared construct.
- Adjusted the draft's opening construct list and Peterman note so fields and states are framed as peer authoring tools rather than a required-vs-optional hierarchy. This keeps the philosophy aligned with the intended stateless-precept direction from Issue #22 without overstating current parser constraints.

### 2026-04-08 - Philosophy location canonicalized
- Philosophy canonical location: `docs/philosophy.md` (not `design/brand/`)
- `design/brand/philosophy.md` is now a redirect pointer
- The old icon-specific sections were stripped in earlier edits

### 2026-05-18 - README hero sizing PR closeout
- Merged PR #36 (`chore: finalize README hero DSL sizing contract`) from `chore/upgrade-squad-latest` into `main` with a merge commit, preserving the branch per Shane's standing directive to keep it open for follow-on work.
- Final README hero DSL tuning is now anchored to GitHub's 830px repo-view image ceiling; the durable regeneration contract lives in `design/brand/readme-hero-dsl.html` and `design/brand/capture-hero-dsl.mjs`, with the rendered artifact at `design/brand/readme-hero-dsl.png`.
- When a README asset PR is already visually approved, keep the PR/body scoped to the final user-visible change plus the reproducibility contract; do not reopen aesthetic debate once the principal has said the result "looks perfect."

### 2026-04-08 - Already-merged PR verification on retained branches
- When a long-lived branch is supposed to stay open after merge, branch existence proves nothing about PR state. Verify with `gh pr status` and `gh pr view` before attempting any PR creation or merge action.
- Fetch first, then compare `origin/main`, `HEAD`, and `origin/<branch>`. A stale local `main` can make a completed PR look unmerged when GitHub has already landed the merge commit.
- Correct closeout in this situation is factual reporting: PR number, title, merge commit, merged timestamp, and surviving branch state locally/remotely — not a second round of theatrics.

### 2026-04-05 - Beyond-v1 type system roadmap reasoning
- Completed forward-looking type system growth analysis appended to `docs/research/language/references/type-system-survey.md`. Evaluated 9 type candidates against the same 6-system survey base (FEEL, Cedar, Drools, NRules, BPMN, SQL).
- **Phase 2 (post-v1) top 3:** (1) ordinal `choice` comparison with explicit `ordered` keyword — lowest cost, highest convenience, extends v1; (2) named choice sets (`choiceset`) — reduces repetition, low parser cost; (3) `integer` — first genuinely new type, justified when fractional-rejection invariants appear as workaround patterns.
- **Phase 3 (enterprise):** `decimal`/`money` (financial precision), `duration` (scheduling/SLA), `time` (business hours — hardest timezone design problem).
- **Long-term:** `range`/intervals (tier-based decisioning), string pattern constraints, `list<T>` with index access.
- **Never-add list is architectural identity, not technical limitation:** record/structured types, map/dynamic keys, datetime/timestamp, function types, domain-specific string types, `any`/dynamic typing, inheritance. Each would erode Precept's core contract (statically analyzed, deterministically executable, self-contained).
- **Key design principle confirmed:** each phase transition gated by evidence from real usage (workaround cost > complexity cost), not speculation. Requires 50+ real-world precepts before Phase 2 triggers.
- **Hardest open question for Phase 2:** integer division semantics — does `5 / 2` yield `2` or `2.5`? Must be resolved before `integer` ships. Recommendation: truncate, no implicit promotion.
- **`map<K,V>` is the strongest "never" candidate** — not because it's technically impossible but because a precept with dynamic keys is a precept that hasn't been modeled. The right answer is always "model your fields explicitly."

### 2026-05-16 - Focused PR handling for squad metadata
- When Shane asks for a PR on an existing `.squad/team.md` edit, keep the branch and PR surgically scoped to that file even if local bookkeeping updates happen afterward.
- Reliable path set for this workflow: `.squad/team.md` for the roster/source metadata, `.squad/agents/frank/history.md` for post-task learnings, and `main` as the PR base unless explicitly redirected.

### 2026-05-15 - Standard issue workflow normalization
- Standardize GitHub issue management by separating concerns cleanly: routing labels tell Squad who owns the work, taxonomy labels tell humans what kind of work it is, GitHub Project `Status` carries lifecycle, and issue open/closed carries terminal semantics.
- Proposal-specific status labels (`needs-decision`, `decided`, `deferred`) are architectural clutter. A proposal is just an issue type; decision-waiting belongs in board status (`In Review`), and the final outcome belongs in the closing comment plus issue closure.
- Recommended minimal routing surface for this repo: `squad` as the team inbox and exactly one `squad:{member}` label for direct ownership. Keep priority/blocker/security-style labels optional and special; do not let them become shadow workflow states.
- Key references for this recommendation: `.squad/templates/issue-lifecycle.md`, `.squad/routing.md`, `docs/research/language/README.md`, and `.copilot/skills/architectural-proposals/SKILL.md`.

### 2026-05-01 - Expression research methodology
- MCP compile tool is the authoritative way to confirm expression limitations — faster and more reliable than reading parser source. Use `precept_compile` with minimal test precepts to verify each proposed construct before documenting it.
- FEEL (DMN) is the strongest external comparator for Precept's expression surface. It's a business-oriented DSL with ternary (`if-then-else`), string functions (`string length`, `contains`), numeric functions (`abs`, `min`, `max`, `round`), and range membership (`x in [1..10]`). Any expression feature Precept considers should be benchmarked against FEEL's design.

### 2026-05-17 - README DSL contract text sizing analysis
- Evaluated four approaches for keeping DSL contract text size consistent with surrounding README text on GitHub: (a) redesigned PNG, (b) SVG with `<text>`, (c) styled HTML/CSS, (d) fenced code block.
- **Key architectural insight:** Any image-based approach (PNG or SVG) is fundamentally brittle for text sizing. Images render in their own scaling context via `<img>` — text inside scales with the image viewport, not the page's font cascade. No `width` attribute or viewBox tuning can synchronize image-internal text with page text across viewports and zoom levels.
- **SVG is better quality, same sizing problem.** GitHub renders SVGs as `<img>` tags, not inline. Vector quality eliminates pixelation but the viewport-mismatch remains identical to PNG.
- **HTML/CSS is impossible on GitHub.** The sanitizer strips `<style>`, `style=""`, `class`, and all custom CSS. No path to styled code in a README.
- **Fenced code blocks are the only stable approach.** Native `<pre><code>` participates in the page's CSS font cascade. Text scales identically to all other page text at every viewport and zoom level.
- **The DSL's readability without decoration proves the language design thesis.** If the DSL needs color to be readable, the keyword-anchored English-ish design has failed. It hasn't.
- **Medium-term unlock: GitHub Linguist registration.** A Tree-sitter grammar + Linguist inclusion gives real syntax highlighting in fenced code blocks that tracks GitHub themes and scales natively.
- Sizing analysis and the resulting README image-width tradeoff are now preserved in `.squad/decisions.md`.

### 2026-04-07 - PR workflow with uncommitted changes
- When branch carries uncommitted changes related to the stated PR task (e.g., README image fix), create a commit before pushing and creating the PR. Include the Co-authored-by trailer per repo policy.
- Push with `-u` flag to set upstream tracking, then use `gh pr create` with explicit `--base` and `--head` to ensure correct target branch.
- For merge decisions, prefer merge commit strategy when PR spans multiple decision areas (Squad config + image fix). Use auto-merge when available, but be prepared for merge method prompt and branch deletion confirmation.
- Verified successful PR #34: merged to main with both Squad configuration upgrades and README image link fixes (design/ prefix added) in a single clean commit history.
- Cedar (AWS) is the strongest *counter-precedent*. It deliberately omits division and most math functions to maintain formal analyzability. Precept should note which features Cedar excludes and why — not everything FEEL does is automatically right for a constrained DSL.
- The ternary gap is the single highest-impact expression limitation. It causes row duplication across 14+ samples. String `.length` is the most *embarrassing* gap — it's a table-stakes feature that every comparison target provides.

### 2026-04-05 - Language proposal review sequencing
- Reviewed language proposal issues #8-#13 against the DSL expressiveness research and the current language-design constraints.
- Recommended first-wave candidates: `#10` string `.length` and `#8` named guards; second wave: `#9` ternary-in-`set`, then `#11` absorb shorthand; last wave: `#12` inline `else reject` and `#13` field-level constraints.
- Reaffirmed that keyword-anchored flat statements and first-match routing are architectural guardrails; proposals that pressure either surface need explicit containment or they will sprawl.

### 2026-04-05 - Language proposal body expansion (#11, #12, #13)
- Expanded issues #11, #12, #13 from acceptance-criteria stubs into full proposal writeups with real Precept examples drawn from existing sample files and reference-language code (xstate, Polly, Zod, FluentValidation).
- **#11 (`absorb`):** `absorb` must be event-scoped (not bare); explicit `set` takes precedence; language server must warn on zero-match absorb. Last wave.
- **#12 (`else reject`):** Scope locked to `reject` only — never `else transition` or `else set`. Only one `else reject` per event+state pair; multi-guard scenarios must use standalone fallback rows. The multi-else-reject interaction must be resolved in a design doc before any code. Second-to-last wave.
- **#13 (field-level constraints):** Shape A (inline `min`/`max`) violates the keyword-anchor principle — research README already rejected it. Shape B (`constrain` keyword) preserves the principle but creates two constraint pathways. Neither shape is implementation-ready without a Shane sign-off on which to adopt. Last wave.
- Decisions inbox entry written at `.squad/decisions/inbox/frank-expand-language-proposals.md`.

### 2026-05-14 - Guard reuse in invariants and state asserts
- Researched whether named guard declarations (#8) should be referenceable in `invariant` and state `assert` contexts, not just `when` clauses.
- **Recommendation: YES.** Guard bodies are field-scoped; invariants and state asserts are field-scoped. Exact scope match — no widening needed. Allowing guard names in all field-scoped boolean-expression positions is the natural name-resolution rule.
- **Event asserts: NO.** Confirmed via PRECEPT016 that `on <Event> assert` is arg-only scoped — fundamentally incompatible with field-scoped guards. This is correct scoping, not a limitation.
- **`set` RHS: NO (v1).** Even though type-compatible for boolean fields, allowing guards in value positions blurs the boundary with computed fields (Proposal 7). Keep concepts separate.
- Guard reuse should be part of v1 guard implementation, not a follow-up — the implementation cost is near-zero if guards are resolved as named boolean expression symbols.
- Key composition point with Proposal 4 (conditional invariants): `invariant X > 0 when GuardName because "..."` — guards as conditions on conditional invariants.
- Updated `docs/research/dsl-expressiveness/expression-feature-proposals.md` Proposal 3 with full reuse analysis, scope compatibility table, examples, and Proposal 4 interaction.
- Decision filed at `.squad/decisions/inbox/frank-guard-reuse.md`.

### 2026-04-05 - Type system expansion proposal
- Filed comprehensive GitHub issue proposal for expanding the Precept type system. Triggered by Shane asking "do we support dates yet?" — we don't.
- **Corpus evidence is strong:** 4 samples use `number` as day-counter workaround for dates; 1 sample uses `number` + paired invariants as ersatz enum; 3+ samples use `set of string` for named-item collections that are really constrained value sets; 1 sample concatenates strings to simulate structured data.
- **Recommended `choice` type** over `enum`: `choice` reads as configuration ("pick one of these"), `enum` reads as programming. `choice("Low", "Medium", "High")` — the value set is the type. This is the highest-confidence addition because it has no operator design, no temporal semantics, and no philosophy tension.
- **Recommended `date` type** at day-level granularity only: no time-of-day, no timezone. Deterministic day arithmetic (`+` / `-` with `number` of days), comparison operators, `.day` / `.month` / `.year` accessors. This matches corpus usage (all 4 date-workaround samples track relative day offsets, not wall-clock times).
- **Rejected `datetime`/`timestamp`:** Timezone semantics violate deterministic inspectability. If you can't fire the same event with the same inputs and get the same result regardless of where the server is, you've broken Precept's core contract. Time-of-day is an event input, not a DSL type.
- **Rejected structured/record types:** Precept is flat by design. The trafficlight `EmergencyReason = AuthorizedBy + ": " + Reason` concatenation is a deliberate flattening, not a workaround. Adding record types would create nested field paths, complicate the type checker's per-field interval analysis, and pressure the keyword-anchored flat statement model.
- **`choice` interacts with collections:** `set of choice(...)` is the natural replacement for `set of string` when the string values are a known domain. This should be supported from v1 — the collection inner-type constraint just widens from `{string, number, boolean}` to `{string, number, boolean, choice}`.
- Decision filed at `.squad/decisions/inbox/frank-type-system-expansion.md`.

### 2026-05-15 - Named construct renamed from `guard`/`predicate` to `rule`
- Shane challenged the `guard` naming: if the construct is reused in `invariant` and state `assert`, it's no longer a guard. He was right.
- Evaluated 6 options: keep `guard` (rejected — routing connotation), `rule` (recommended), `check` (rejected — imperative), `condition` (rejected — verbose), `predicate` (rejected — academic, violates "English-ish" principle), split constructs (rejected — artificial complexity for identical scope/semantics).
- **Recommendation: `rule`.** Rationale: (1) scope-neutral — serves routing, data truth, and movement truth equally; (2) Precept's README already says "One file, all rules"; (3) C46 already calls these "rule positions"; (4) 4 chars, natural English, declarative syntax; (5) `guard` stays in its proper home — the inline `when` expression in transition rows.
- Updated `expression-feature-proposals.md` Proposal 3, `expression-language-audit.md` L3, and `docs/research/dsl-expressiveness/README.md` — all renamed from `predicate` to `rule`.
- Superseded `frank-guard-reuse.md` (reuse analysis still valid; keyword changes).
- Decision filed at `.squad/decisions/inbox/frank-guard-naming.md`.

### 2026-04-05 - Closeout lanes must follow operational cohesion
- When auditing a dirty worktree for trunk closeout, group files by **one deployable behavior change**, not by author, folder, or “they were touched at the same time.”
- For workflow normalization, the safe trunk lane is the live GitHub automation plus the first-order docs/templates that describe the same lifecycle. Agent histories, research-tree reshuffles, PRDs, mockups, and stray lockfiles are not part of that lane and must not hitchhike.
- A good closeout sequence is: land the enforcement mechanics first, then the operator-facing guidance that matches those mechanics, then bring UX exploration separately after a fresh product/design review.
### 2026-04-05 - Type system survey findings
- Surveyed 6 systems (DMN/FEEL, Cedar, Drools, NRules, BPMN, SQL) for type system evidence. Key findings:
- **`date` is universally present** — all 6 systems have a date type. A business rule DSL without date is incomplete. Day-granularity date-only (no time, no timezone) is the safe v1 choice, validated by Cedar's date-only constructor and SQL's timezone cautionary tale.
- **`enum`/`choice` is table stakes** — Drools has `declare enum`, SQL has `ENUM`, NRules uses C# `enum`. FEEL and Cedar lack it and users work around the absence. Highest-confidence addition.
- **Constructor function pattern is the standard** — FEEL uses `date("2024-03-15")`, Cedar uses `datetime("2024-10-15")`. Precept should follow this pattern if literal syntax is too complex.
- **Duration splits in two** in FEEL and SQL (calendar vs clock), but Cedar uses one (millisecond-based). For Precept's day-granularity model, number-of-days arithmetic is simpler and sufficient — no dedicated duration type needed in v1.
- **Decimal/money appears universally** but is a growth-phase addition: `BigDecimal` (FEEL), `decimal` (Cedar extension), `DECIMAL(p,s)` (SQL). Not a v1 essential — Precept's `number` already handles decimal values.
- **Record/structured types conflict with Precept's flat model** — FEEL has `context`, Cedar has `Record`, but both serve different architectural goals. Precept's rejection of structured types is validated.
- **Proposal #25 is well-calibrated** — `choice` and `date` are the two highest-value additions. The rejections (datetime, structured types, overengineered duration) are validated by the survey.
- Research written to `docs/research/language/references/type-system-survey.md`.

### 2026-04-08 - Severity consistency challenge on stateless event boundary
- Shane challenged the severity inconsistency: zero-states+events=error vs single-state+events+no-transitions=hint. "Same structural degeneration, different severities."
- Confronted the argument honestly. The premise is factually wrong: the two scenarios are NOT structurally identical. MCP Fire confirmed single-state events produce **NoTransition** outcomes (real firing, real mutations, Count 3→4). Zero-state events would produce **Undefined** (no firing, no mutations, nothing).
- The scenarios share only 1 of 5 behavioral dimensions (no state change). They diverge on event firing, action execution, field mutation, and outcome production. "No state movement" ≠ "no useful dispatch surface."
- The API-level argument is even stronger: `Fire(currentState, event)` and `Inspect(currentState)` require a `currentState` parameter. No states → no valid API input. Events become not just useless but **unaddressable** — the runtime pipeline has no entry point for them.
- The orphaned-event analogy (C49=warning) doesn't apply: orphaned events have *accidentally missing* routing (add a `from` row to fix). Events-without-states have *structurally impossible* routing (must change the precept's category to fix).
- **Position confirmed: error is correct.** Revised the justification from "category boundary" (hand-wavy) to "unfulfillable contract — events are unaddressable without states, and the Fire/Inspect APIs are structurally incompatible with statelessness."
- Updated `.squad/decisions/inbox/frank-stateless-event-boundary.md` with addendum.

## Language Design Expertise — Deep Study (2026-04-05)

### A. PreceptLanguageDesign.md — Complete Internalization

**5 Goals:**
1. No indentation-based structure — blocks must be explicit and line-oriented. Maps cleanly to Superpower without offside-rule handling.
2. Tooling-friendly — keyword-anchored statements, deterministic parse, predictable IntelliSense. Parser knows statement kind at first token.
3. Keyword-anchored flat statements — every statement begins with a recognizable keyword. No section headers, no indentation.
4. Explicit nullability — `nullable` keyword, not punctuation-based null markers. First-class in type checking.
5. Compile-time-first semantics — catch authoring mistakes early. The compiler proves contradictions; the inspector handles data-dependent impossibility.

**12 Design Principles (with practical implications):**
1. **Deterministic, inspectable model.** Fire/inspect always produces the same result for the same inputs. All validation evaluates against "proposed world" (post-mutation, pre-commit). No hidden state, no side effects.
2. **English-ish but not English.** Keywords like `with`, `because`, `from`, `on` read naturally but don't attempt full sentences. Samples are the tutorial.
3. **Minimal ceremony.** No colons, curly braces, semicolons. `because` is the sentinel. Keyword anchoring replaces punctuation.
4. **Locality of reference.** Rules live near what they describe — invariants near fields, state asserts near states.
5. **Data truth vs movement truth.** `invariant` = static data constraints (always hold). `assert` = movement constraints (checked when something happens).
6. **Collect-all for validation, first-match for routing.** Validation reports every failure. Transition rows evaluate top-to-bottom, first match wins.
7. **Self-contained rows.** Each transition row is independently readable. No shared context with sibling rows.
8. **Sound, compile-time-first static analysis.** Rejects real semantic mistakes early but never guesses. If the checker can't prove a contradiction, it assumes satisfiable.
9. **Tooling drives syntax.** IntelliSense, diagnostics, preview are first-class design constraints. Grammar is friendly to Superpower token/combinator parser.
10. **Consistent prepositions.** `from` = leaving, `to` = entering, `in` = while in, `on` = when an event fires. Same meaning everywhere.
11. **`->` means "do something."** Arrow introduces an action. Separates context from action. Sequential execution, read-your-writes.
12. **AI is a first-class consumer.** Deterministic semantics, keyword-anchored flat statements, structured tool APIs (MCP). The intended workflow: domain expert describes intent, AI authors the precept, toolchain closes the correctness loop.

**12 Deliberate Exclusions:**
1. No indentation-sensitive syntax
2. No punctuation-heavy delimiters (`{...}`, `[...]` for blocks)
3. No lookahead-heavy constructs
4. No section headers (`fields:`, `rules:`)
5. No implicit state machine semantics (auto-advance, timeout)
6. No cross-precept composition (imports, mixins, inheritance)
7. No implicit null handling (`?.`, `??`)
8. No function definitions
9. No ternary expressions (currently — high-impact future feature)
10. No string methods (currently — `.length` is table-stakes gap)
11. No type annotations on literals
12. No computed/virtual fields

Each exclusion removes complexity that Superpower would struggle with (indentation, lookahead) or that requires context-dependent parsing.

### B. Core Pipeline Architecture

**Tokenize → Parse → Type-Check → Assemble → Execute:**
- Phase 1 (Tokenize): `PreceptTokenizerBuilder` → keywords registered via reflection from `[TokenSymbol]` attributes (zero drift). Output: `TokenList<PreceptToken>`.
- Phase 2 (Parse): `PreceptParser` → 8 statement combinators in priority order (EditDecl, EventAssertDecl, StateAssertDecl, TransitionRowParser, StateActionDecl, FieldDecl, InvariantDecl, StateDecl). `AssembleModel` validates structure and calls type checker.
- Phase 3 (Type-Check): `PreceptTypeChecker` → builds symbol tables, traverses expressions, checks types and null-flow. C38–C43.
- Phase 4 (Compile): `PreceptCompiler` → validates graph (reachability, coverage). C48–C53 warnings.
- Phase 5 (Execute): `PreceptEngine.Fire()` → find transition rows, evaluate guards, execute mutations, validate asserts/invariants.

**Phase boundaries:** Each phase owns a specific concern and assumes the prior phase passed. Parser = syntax. Type checker = types/null-flow. Compiler = graph. Runtime = behavior.

**Key constraint codes:** Parse C1–C10, Type-check C38–C43, Compile C25/C46–C53, Runtime C14–C16/C54. Every diagnostic maps to exactly one code.

### C. Superpower Parser — Enables and Constrains

**What it does naturally:**
- Flat token stream parsing — no indentation tracking
- Keyword-led combinators — `Token.EqualTo().Then()` identifies statement kind
- Try-or chains — `.Try().Or()` for disambiguation without committing
- First-match semantics — mirrors transition row evaluation
- Sequential action pipes — LINQ-style `from ... select`
- Expression precedence via mutually recursive combinators

**What it doesn't do:**
- Indentation-aware parsing → no block nesting in Precept
- Unbounded lookahead → grammar stays LL-friendly (LL(2) max)
- Context-sensitive tokens → resolved by try-or branches, not tokenizer feedback
- Semantic actions during parse → validation decoupled into AssembleModel/TypeChecker

**March 2026 redesign:** Replaced regex parser + indentation blocks with flat keyword-anchored Superpower combinators. The language surface was shaped *around* Superpower's strengths.

### D. Keyword-Anchored Principle — Definitive Understanding

**What it means:** Every statement begins with a keyword. Statement kind resolvable by first token or LL(2) lookahead. No indentation tracking. No block balancing.

**What it does NOT mean (per issue #13 reassessment):** Inline keyword suffixes like `min`/`max` on `field` lines are NOT a violation. Shape A (`field Amount as number default 0 min 0 max 10000`) stays on one line, starts with `field`, uses keyword-led suffixes (like existing `as`, `default`, `nullable`), requires no indentation or block balancing.

**The real test:** Does a proposed construct require indentation tracking, block balancing, or context-sensitive disambiguation? If no, it does not violate keyword-anchoring.

### E. Research Library Integration

**Expressiveness studies (6+ libraries):**
- xstate: Closest overlap on `from/on/when`. Precept's flat rows are simpler but lack hierarchical states (intentionally excluded — too costly for domain model).
- Polly: Overlap on `->` action pipeline. Polly's retry/circuit-breaker patterns map to Precept's action chain but with different semantics.
- FluentValidation: Overlap on `invariant`, `in <State> assert`. FluentValidation's `.Must()` chains are more expressive but lose inspectability.
- Zod/Valibot: Overlap on field declarations + `invariant`. Schema-first validation; strongest comparator for field-level constraints (issue #13).
- LINQ: Overlap on mutation/value-selection. Ternary gap is most visible here.
- FluentAssertions: Overlap on `invariant`, `on ... assert`. Chain-style assertions are more readable for complex predicates.
- FEEL/DMN: Strongest external comparator for business-rule DSL expression design. Has ternary, string functions, numeric functions, range membership.
- Cedar (AWS): Strongest counter-precedent. Deliberately omits division and most math for formal analyzability.

### 2026-04-11 — Issue #14 design verdict: `when <guard>` on declarations — APPROVED (Forms 1–3)

- **Philosophy fit is pre-confirmed, not just defensible.** `docs/philosophy.md` explicitly states "guarded invariants do not weaken the guarantee; they make it precise." When a proposal implements what the philosophy already describes rather than extending it, the philosophy-fit finding is trivially GREEN. Note this explicitly in review to prevent any confusion about scope.
- **Scope-inherited conditioning is industry-standard.** Zero surveyed systems allow cross-scope guard conditions (FluentValidation guards reference entity properties; Drools guards reference fact properties; Cedar guards reference policy attributes; Alloy facts reference signature fields). The scope-inheritance principle is not a Precept invention — it is the established practice across all production constraint systems.
- **Implementation constraints ≠ design flaws.** All three reviewers' "blockers" are implementation scope questions: narrowing unsoundness (`WhenGuard is null` filter) is a type-checker implementation invariant; compile DTO gap is prerequisite parallel work; Form 4 static-dictionary mismatch is an architectural scope split best filed as a follow-on issue. None require proposal revision. Classify correctly.
- **B3 (research files absent) was a false blocker caused by wrong path prefixes in the issue body.** The proposal cited `docs/research/language/...` paths; the actual paths are `research/language/...`. Always verify research file existence directly via `file_search` rather than trusting issue body citations. Corrected the issue body in the same session.
- **The planned analysis docs (`frank-constraint-scoping-symmetry.md`, `frank-event-assert-when-reversal.md`) were cited in the issue as if existing but had not been produced.** Their substance was covered inline in the review itself (G5 and G6). If a review covers the substance of a planned document, the review IS the document — do not block on materializing the file separately.
- **Event assert `when` reversal was the most important finding.** The prior position (exclude `on <Event> assert` from `when` coverage) was an overcorrection. The correct boundary is scope-scoped, not declaration-type-scoped. Arg-only guards on event asserts are within-scope → permitted. The teaching model becomes uniform: "every constraint declaration accepts `when`; the guard lives in the same scope as the constraint."
- **Form 4 conjunctive semantics are architecturally sound but require a different runtime pattern.** `in State when guard edit` — state AND guard must hold. Both are independent filters. The concept is philosophically grounded; the implementation gap is that `_editableFieldsByState` is static. Per-call guard evaluation is the correct strategy for Form 4; a separate issue preserves scope discipline.

**Expression audit highlights:** No ternary (row duplication across 14+ samples), no string `.length` (table-stakes gap), no function calls, no named guards. Current expression surface covers: arithmetic, logical, comparison, contains, collection accessors.

**Verbosity analysis:** Three top smells: event-argument ingestion boilerplate, guard-pair duplication, non-negative constraint boilerplate. Map to proposals #11 (absorb), #12 (else reject), #13 (field constraints).

**Open proposals #8–#18:**
- Wave 1 (highest impact): #10 string `.length`, #8 named rules, #9 ternary
- Wave 2: #11 absorb, #12 else reject, #14 conditional invariants
- Wave 3: #13 field constraints, #15 string `.contains()`, #16 numeric functions
- Future: #17 computed fields
- Rejected: #18 conditional outcome in `->` chain

All proposals are additive and Superpower-compatible. No structural redesign required.

### 2026-04-08 - Issue #22 full rewrite incorporating warning model decisions
- Rewrote the complete #22 issue body at Shane's request, incorporating all session decisions: events-without-states = warning (Shane's override of Frank's error recommendation), C50 severity upgrade from hint to warning, tautological/structurally-impossible rules dropped from semantic rules.
- Folded the "Design philosophy thread" section into Summary and Motivation per Shane's conciseness directive. Philosophy argument survives as the Summary's second paragraph; domain fragmentation and progressive disclosure stay in Motivation but tighter.
- Added locked decision #7 (warning model) with full 4-part rationale: consistency with C49/C50, why C50-as-hint was too lenient, precedent from C#/Rust unreachable-code warnings.
- Restructured Exclusions into 4 categories: states (definitional boundary, no rule needed), transitions (structurally impossible via C54), events (active design decision — warning per locked decision #7), preview (deferred).
- Updated implementation scope: removed "reject events/transitions in stateless context" parser items, added analysis-phase items for new warning diagnostic and C50 severity upgrade.
- Updated acceptance criteria: events-without-states produces warning (not error), C50 severity = warning, transitions in stateless fail existing C54.
- Key writing lesson: conciseness requires structural discipline — say each argument once in the section where it belongs, then reference it elsewhere. The old issue repeated the philosophy argument in Summary, Motivation, Design Philosophy Thread, and Locked Decisions. The rewrite eliminates that redundancy without losing the argument.

### 2026-04-11 — Issue #14 design review filed
- Conducted the full design/architecture review for Issue #14 (`when <guard>` on declarations) on 2026-04-11. Filed at `.squad/decisions/inbox/frank-issue14-design-review.md`; merged into `.squad/decisions.md`.
- **Verdict: APPROVED for Forms 1–3.** Form 4 (`in State when guard edit`) deferred to a follow-on issue — concept is sound, implementation requires per-call guard evaluation rather than static dictionary.
- B3 (research base absent) cleared: both cited research files confirmed present at `research/language/expressiveness/conditional-logic-strategy.md` and `research/language/references/conditional-invariant-survey.md`. Issue body had wrong `docs/` path prefix — corrected in the same session.
- All six locked design decisions upheld as architecturally sound. No philosophy gaps found. Zero grammar changes required.
