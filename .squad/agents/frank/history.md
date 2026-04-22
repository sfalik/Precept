## Core Context

- Owns architecture, language design, and final review gates across runtime, tooling, and documentation.
- Co-owns language research with George and keeps `docs/PreceptLanguageDesign.md` aligned with actual implementation.
- Standing architecture rules: keep MCP as thin wrappers, keep docs factual, surface open decisions instead of inventing behavior, and preserve philosophy boundaries.
- Historical summary (pre-2026-04-13): drove proposal/design work for conditional guards, event hooks, verdict modifiers, modifier ordering, computed fields, and related issue-review passes; also reviewed the Squad `@copilot` lane retirement and other cross-surface contract updates.
- Owns architecture, system boundaries, and review gates across the runtime, tooling, and documentation surfaces.
- **Language Designer:** Owns Precept language design, grammar evolution, and Superpower parser strategy. `docs/PreceptLanguageDesign.md` is the foundational document.
- **Language research co-owner:** Co-owns `docs/research/language/` with George. Knows the comparative studies (xstate, Polly, FluentValidation, Zod/Valibot, LINQ, FluentAssertions, FEEL/DMN, Cedar), expression audit, verbosity analysis, and all PLT references.
- Core architectural discipline: keep MCP tools as thin wrappers, keep docs honest about implemented behavior, and document open decisions instead of inventing values.
- Technical-surface work flows through Elaine (UX), Peterman (brand compliance), Frank (architectural fit), then Shane (sign-off).
- README and brand-spec changes should reflect actual runtime semantics, not speculative future behavior.

## Recent Updates

### 2026-04-19 — Drafted `docs/TypeCheckerDesign.md` (Track B design doc)
- Canonical design document describing the CURRENT implemented architecture of `PreceptTypeChecker`.
- 6-partial decomposition (3,783 LOC): Main, TypeInference, Narrowing, ProofChecks, Helpers, FieldConstraints.
- 7 design decisions with rationale, alternatives rejected, and tradeoffs accepted.
- Research-grounded: references both architecture survey (Frank) and implementation patterns (George).
- Philosophy-traced: 6 principles each linking to `docs/philosophy.md`.
- Integration points table covering proof engine (~7 call sites), language server (3 consumers), MCP (4 tools), and compiler pipeline.
- Re-evaluation triggers from research (2K LOC per file, 5K total, phase formalization, incremental compilation).
- Decision filed at `.squad/decisions/inbox/frank-typechecker-design-doc.md`.

### 2026-04-19 — Design review of issue #118: PreceptTypeChecker decomposition
- **Verdict: APPROVED WITH NOTES.** Proposal is architecturally sound and ready for implementation planning.
- Verified all 67 class-level methods against proposed 6-file grouping. Method inventory is complete — no methods missing, no phantom methods.
- Actual file size: 3695 lines (proposal said ~3200 — likely from a pre-PR#108 baseline).
- Main file size underestimated by ~100-200 lines (computed ~1132 methods-only vs estimated ~950-1050). Not blocking.
- ProofChecks overestimated by ~100-150 lines (computed ~404 vs estimated ~500-550). The vague "C94-C98 helpers" bullet is redundant with individually named methods.
- Copy helpers (CopyRelationalFacts, CopyFieldIntervals, CopyFlags, CopyExprFacts) correctly placed in Helpers — called from both Main and Narrowing, genuinely cross-cutting.
- 7 non-blocking notes filed: size estimates, fully explicit Helpers inventory, front-matter type placement in acceptance criteria, per-file `using` directives.
- Full review at `.squad/decisions/inbox/frank-issue-118-design-review.md`.
- **Key architectural learning:** partial class decomposition of a static class is the correct pattern for large single-concern files in this codebase. The zero-visibility-change property is the key enabler.

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

- Design review gate formalization (2026-04-19): Formalized the two-track design review model across 7 files (CONTRIBUTING.md, ceremonies.md, copilot-instructions.md, squad.agent.md, frank/charter.md, proposal-review/SKILL.md, decisions.md). Track A = issue-comment reviews; Track B = issue + inline PR review comments on design doc markdown. Owner sign-off is the universal completion gate. For Track B, all inline review threads must also be resolved. Key process files: `CONTRIBUTING.md` § 3. Design Review (canonical), `.squad/ceremonies.md` (ceremony definition), `.squad/skills/proposal-review/SKILL.md` (reviewer workflow). The implementation gate in `squad.agent.md` now checks design review status before authorizing implementation planning.

- ProofEngineDesign.md (2026-04-17): Created `docs/ProofEngineDesign.md` as the dedicated design doc for the non-SMT proof engine (Slices 11–15). Key doc structure decisions: (1) organized as a 5-layer architecture (sequential flow → intervals → interval inference → relational → conditional synthesis) rather than by slice number — layers communicate the conceptual stack, slices are implementation ordering; (2) included the full `TryInferInterval` dispatch table because it's the contract that implementers code against; (3) documented design decisions with explicit alternatives-rejected and tradeoffs-accepted per CONTRIBUTING.md requirements; (4) cross-referenced from both PreceptLanguageDesign.md (divisor-safety section) and ConstraintViolationDesign.md (C93 entry). Also updated PreceptLanguageDesign.md to replace the stale "compound expressions assumed satisfiable" statement with a reference to the proof engine.

- Non-SMT proof stack implementation planning (2026-04-17): Authored the 5-slice implementation plan (Slices 11–15) for PR #108, extending divisor safety with sequential assignment flow, interval arithmetic, relational inference, and conditional proof synthesis. Key planning insights: (1) George's "skip sign analysis, go straight to intervals" recommendation is correct — intervals subsume signs, saving ~100 lines of redundant code; (2) the 4 existing `Check_DivisorCompound_*_NoWarning` tests that document Principle #8 conservatism will ALL flip when interval analysis lands — they were not "passing tests" but "known gaps documented as tests"; (3) string-encoded interval markers (`$ival:key:lower:lowerInc:upper:upperInc`) in the existing symbol table avoid threading a second dictionary through the entire narrowing pipeline — minimal API churn; (4) relational markers (`$gt:A:B`) only need strict inequality to prove nonzero — `>=` alone is insufficient because `A >= B` allows `A == B → A-B == 0`; (5) conditional expression proof synthesis is essentially free — it's 5 lines inside `TryInferInterval` (hull of both branches) plus dedicated test coverage. Total estimate: ~340 new + ~60 changed lines, 48 new + 5 updated tests — 30% smaller than George's original ~500+80 estimate because intervals subsume sign analysis.

- Proof technique re-evaluation — Precept execution model grounding (2026-04-17): The team's proof-technique analyses (sign analysis, intervals, relational inference, etc.) were framed using general PL static analysis concepts — join rules, widening, fixpoint computation, branch reconvergence, control-flow graphs — that DO NOT EXIST in Precept. Precept has no loops, no control-flow branches (only `if/then/else` as an expression ternary), no scope nesting, no user-defined functions, and no aliasing. The execution model is: guards select a row → actions execute sequentially left-to-right → done. This means every proof technique is a SINGLE FORWARD PASS, not an iterative fixpoint. Sign analysis: ~100-120 lines (two lookup tables, no iteration). Interval arithmetic: ~130-150 lines (forward propagation only, no widening). Relational inference: ~55-70 lines (declared facts from rules, kill on mutation, no transitive closure). Total for all techniques: ~350-415 lines, not the ~1000+ estimated under general PL framing. Key lesson: ALWAYS derive complexity from the actual language model, not from academic literature about general-purpose languages. The absence of loops and branches eliminates 80%+ of the machinery those techniques normally require.

- Layer 3 sequential symbol-table evaluation (2026-04-17): Full sequential symbol-table update in action chains is NOT recommended — false-positive surface on non-literal assignments (`set Rate = Rate * 2`) is too broad because `ApplyNarrowing` cannot derive proofs from arithmetic output. Recommended: Layer 3a (literal-only proof invalidation) — update proofs only when a `set` assigns a literal. Catches `set Rate = 0 -> set X = Amount / Rate`. Zero false positives. The current snapshot approach violates Principle #8 spirit: checker assumes `$positive:Rate` survives `set Rate = 0` when it has syntactic evidence to the contrary. Runtime still prevents data corruption (evaluator rejects div-by-zero), so this is compile-time quality, not safety. Key insight: Principle #8 is two-sided — "never guess" means don't guess proofs hold AND don't guess proofs are broken. Literal assignments are provable; arithmetic outcomes are not. File as three-tier proposal (3a literal, 3b arithmetic inference, 3c full sequential).

- Stateless events evaluation (2026-04-17): `on EventName` bare form (without `from State`) is the right syntax for stateless transition rows. LL(1) resolvable: `ensure` token = event ensure; `when`/`->`/action keyword = stateless row. Philosophy gap: stateless precepts described as having no "transition surface" — stateless events create a mutation surface, not a routing surface. Flag to Shane before implementation. C49 must become conditional (suppress when event has a stateless row). `no transition` outcome is valid but slightly awkward in stateless context — open question for syntax pass. Key file: `docs/PreceptLanguageDesign.md` § Stateless Precepts (Locked). Computed fields and stateless events are complementary, not alternatives. Verdict: recommended-with-caveats.

- Stateless guardrails design (2026-04-17): Designed the full guidance stack for preventing pseudo-lifecycle misuse of stateless `on` events. Three tiers: (1) structural constraints — `transition` is a hard error in stateless rows (no states to route to); bare `on` rows are a hard error when states are declared (C_STATELESS_MIXING); (2) compile-time pseudo-lifecycle heuristic (C_PSEUDO_LIFECYCLE, warning): same field appearing as discriminator in ≥2 stateless event guards with distinct literal values — especially `choice` fields — indicates a lifecycle hiding in a field; severity calibrated as warning (not error) because guarded mutations are valid when they are genuinely data-conditional, not lifecycle-positional; (3) MCP tool output augmentation: `precept_compile` should include a structured `suggestion` block when the heuristic fires; `precept_language` should document the stateless-vs-stateful decision rule; `precept_inspect` should include pattern detection in the response. Key heuristic: all three features together = pseudo-lifecycle: (a) single field as discriminator, (b) finite named vocabulary (choice/string literals), (c) mutual exclusivity across events. Diagnostic message must teach, not just warn — name the field, list the events, explain the intended use. Philosophy framing: stateless events are the mutation surface; states are the routing surface. One-sentence rule: "If knowing WHERE the entity is affects WHAT it can do next, you need states." The precept-authoring SKILL.md needs a Step 3 decision fork: stateless vs. stateful checklist. Mixing prohibition is symmetrical to C55 (root edit not valid when states declared).

- PR #108 code review (issue #106): APPROVED. Unified narrowing architecture is clean — bespoke `$nonneg:` loop replaced by rule iteration through `ApplyNarrowing`, zero dead code. The C76 dotted key fix (`TryGetIdentifierKey` replacing `idArg.Name`) closed a pre-existing gap for event-arg sqrt() proofs. C42 soundness comment in `TryDecomposeNullOrPattern` is the model for dependency documentation. Three non-blocking warnings: regex allocation in code action handler, missing trailing newline, and "must be nonzero" because-message on a `> 0` ensure (should say "must be positive").
- Principle #8 revision analysis (2026-04-17): Shane challenged "assumes satisfiable" as enabling false sense of security. After full analysis: the principle's posture is correct for genuinely undecidable cases, but the wording has a loophole — it doesn't distinguish "can't prove" (genuinely undecidable) from "didn't prove" (syntactically decidable but unchecked). Recommended Option C: revise to mandate "exercise full proving power against declared syntax" while retaining "assumes satisfiable" for undecidable remainder. Opposed "assumes unsatisfiable" (Option A) — false-positive surface is catastrophic. This makes Layer 3a principle-mandated rather than optional. Key insight: both blanket assumptions (satisfiable and unsatisfiable) destroy author trust when applied to the wrong category. The answer is domain separation: decidable → must prove; undecidable → assume satisfiable.

- Review findings resolution (2026-04-17): Addressed all George + Soup Nazi review findings on `docs/ProofEngineDesign.md` and `temp/proof-stack-implementation-plan.md`. Fixes applied: (1) G7 — `$interval:FieldName` → `$ival:FieldName` inconsistency fixed in plan; (2) S4 — "must flip" table resolved: all three ambiguous tests now mandate option (a): add constraints, keep `BeEmpty()` (Addition → `nonnegative`, Multiplication → `positive` on D and C, AbsFunction → `positive` on D); (3) S9 — AbsFunction clarified: `$nonzero:` alone maps to `Unknown` interval, so `abs(Unknown)=[0,∞)` does NOT exclude zero — must use `positive` not `rule D != 0`; (4) Open Design Decision #2 marked RESOLVED. PR #108 body updated to match: Slice 12 → 19 tests (added 3 ExcludesZero negative-range), Slice 13 → 16 new + 4 updated (added PostMutationInterval, 2 modulo, ProvablyZeroDivisors theory, renamed AbsNonzero → AbsPositive), Slice 15 → 6 tests (added RelationalThenBranch), total → 56 new + 5 updated. Key S9 insight worth internalizing: nonzero ≠ positive for interval proof — `$nonzero:` is a gap marker, not a bound marker, so it maps to `Unknown` in the interval domain.

- "Runtime values" framing correction (2026-04-17): Shane challenged the "runtime values" clause in the proposed Principle #8 revision. He's right — "runtime values" imports a general-purpose PL concept that doesn't apply to Precept's closed-world model. In C#/Rust/TS, "runtime values" means genuinely unknowable external data (user input, network, DB). In Precept, every field is typed, constrained, and mutated only through declared channels. Event args are the closest thing to external data, but even those are typed and ensured. The actual boundary is REASONING DEPTH, not DATA AVAILABILITY: (1) arithmetic composition — checker can't compose proofs through operations like `Rate * 2`; (2) cross-constraint interaction — individual proofs don't prove joint satisfiability; (3) inference rules the checker doesn't have. Replaced "runtime values" with "arithmetic composition, cross-constraint interaction, or reasoning beyond the checker's current inference depth" in the proposed text. This correctly frames the boundary as computational, not informational. Precept's closed-world model closes the information gap that general-purpose languages have — the checker's limitation is about how deep it can reason, not what it can see.

- Bounded proof engine evaluation (2026-04-17): Shane proposed "checker defines the proof boundary, authors write within it" — Rust borrow-checker analogy. After full analysis: SUPPORT. Five pillars: (1) business domain arithmetic is empirically simple — 5 division expressions across 25 samples, all trivially handled by sign analysis; (2) sign analysis (positive × positive = positive) covers 100% of corpus with zero false positives, ~300 lines, no deps; (3) author adaptation is minimal and beneficial — add a guard, add a rule, or split expression — makes implicit assumptions explicit; (4) Rust precedent validates the model — strict, sound, bounded checkers succeed when sound, learnable, and growing; (5) "assumes satisfiable" and "assumes unsatisfiable" both destroy trust — bounded proof with explicit obligations is the only model that preserves trust in both directions. Critical constraint: no escape hatch (unlike Rust's `unsafe`), so the boundary must be wide enough that adaptation is always reasonable. Implementation: constant propagation (shipped) + sign analysis (Phase 2) + dataflow/Layer 3b (Phase 3). Phase 2 must ship with Phase 1 — Layer 3a without sign analysis creates a boundary too narrow for real authors. Revised Principle #8 drafted: replaces POSTURE ("assumes satisfiable") with CONTRACT ("defined proof boundary, documented, versioned, expanded over time"). File: `temp/frank-bounded-proof-opinion.md`.

- Comprehensive enforcement design (2026-04-17): Re-evaluated the non-SMT proof engine against Precept's full constraint surface using MCP tools and source code. **Verdict: optimal architecture, no revision needed.** Precept's execution model (no loops, no branches, no reconverging flow) makes single-pass interval arithmetic not just sufficient but the correct choice. Five alternatives rejected (SMT, octagons, constraint propagation, symbolic execution, pattern-based). Designed 6 new enforcement diagnostics (C94-C99) leveraging the interval infrastructure from PR #108: C94 (assignment constraint enforcement — proven violation only, no possible-violation noise), C95 (contradictory rule), C96 (vacuous rule), C97 (dead guard), C98 (vacuous guard), C99 (cross-event invariant, opt-in fixed-point). Key design decisions: (1) proven-violation-only policy — possible violations → no diagnostic, runtime handles them; (2) simple single-field scope for rule/guard analysis — cross-field and complex expressions left to "unproven"; (3) C99 opt-in — only enforcement requiring fixed-point iteration; (4) error/warning severity split by dead-code vs redundancy distinction; (5) no new DSL constructs required. All new enforcements recommended as follow-up issues A-E after PR #108. Deliverable: `docs/ProofEngineDesign.md` +512 lines (optimality assessment, enforcement summary table, detailed C94-C99 specs, soundness guarantees, design decisions, phasing plan). Commit `6fe8ad4`.

- Bounded proof real-world domain reassessment (2026-04-17): Shane challenged the sample-corpus-based analysis as circular reasoning — and he was RIGHT. Resurveyed arithmetic patterns across finance, insurance, healthcare, supply chain, and HR independent of our 25 samples. Key findings: (1) sign analysis alone covers ~75% of real-world division patterns, not 100%; (2) ~10-15% of division patterns involve subtraction-in-denominator (`TotalCost / (UnitsProduced - DefectiveUnits)`, `Amount / (Total - Discount)`, `Cost / (1 - WasteRate)`) which sign analysis CANNOT handle; (3) relational inference (`A > B → A-B > 0`) is the load-bearing technique that closes this gap, raising coverage to ~95-97%; (4) function-specific proofs (max(0,...), pow(positive,n), abs()) add another ~2-3% — critical for insurance `max(0, Claim - Deductible)` pattern; (5) remaining ~3% is genuinely complex computation (amortization formulas, compound financial instruments) that belongs in application code, not the governance DSL. The "no escape hatch" position holds IF relational inference ships — without it, the boundary is too narrow for the "category error" argument. Previous "100% coverage" claim retracted. Restructuring tax: ~60-70% trivial (add guard), ~20-25% moderate (intermediate field), ~5-10% push to app code. Relational inference PROMOTED from "nice to have" to "load-bearing" — must ship before we publicly claim "handles all business domain arithmetic." File: `temp/frank-design-alternatives.md`.

- Divisor safety docs (Slice 9, #106): The `nonnegative ≠ nonzero` distinction is the most important teaching point — it's the one thing that trips authors. Context-aware C93 messaging makes it explicit. Proof sources for C76 and C93 are now unified: constraints, rules, ensures, and guards all feed the same narrowing markers (`$positive:`, `$nonneg:`, `$nonzero:`). Doc updates touched three files (language design, constraint violation, README) — RuntimeApiDesign.md was correctly scoped to public API and didn't need narrowing internals.
- Proof Engine Design Review — comprehensive, philosophy-rooted (2026-04-18): Full review of `docs/ProofEngineDesign.md` + PR #108 implementation per Shane's directive. **Verdict: APPROVED with 2 blockers.** Architecture (ProofContext + LinearForm + RelationalGraph) is the right design — simplified Zone abstract domain hitting exact cost/power sweet spot. Philosophy alignment is direct and complete: prevention, one-file completeness, inspectability, determinism all served. B1: conditional else-branch doesn't receive negated guard narrowing (`WithGuard(condition, false)`) — design specifies it, implementation doesn't do it. One-line fix in `TryInferInterval`, already in frozen blind-spot list. B2: hover attribution mechanism unspecified — Elaine's "from:" UX requirement needs a design decision for how `IntervalOf` carries provenance. Recommended lazy reconstruction (separate `AttributeInterval` query, no API churn). W1-W6: doc header premature "Implemented"; ConstantOffsetScan GTE/c==0 inclusivity (unreachable, Commit 10 fix); WithRule no GCD-normalize (Commit 10); `BuildEventEnsureNarrowings` doesn't propagate typed `_relationalFacts`; no automated perf benchmark; floor/ceil transfer rule precision loss. 10 strengths documented (G1-G10). Key insight: the 3 execution model assumptions (no loops, no branches, no reconverging flow) are what make interval arithmetic OPTIMAL, not just sufficient — widening is the primary precision loss in general analyzers, and Precept avoids it by construction. Implementation plan (15 commits) is well-ordered, dependency chain correct, engine-closure completion bar is the right mechanism. Filed as `.squad/decisions/inbox/frank-proof-engine-review.md`.

- PR #108 code review (issue #106): APPROVED. Unified narrowing architecture is clean — bespoke `$nonneg:` loop replaced by rule iteration through `ApplyNarrowing`, zero dead code. The C76 dotted key fix (`TryGetIdentifierKey` replacing `idArg.Name`) closed a pre-existing gap for event-arg sqrt() proofs. C42 soundness comment in `TryDecomposeNullOrPattern` is the model for dependency documentation. Three non-blocking warnings: regex allocation in code action handler, missing trailing newline, and "must be nonzero" because-message on a `> 0` ensure (should say "must be positive").
- Divisor safety docs (Slice 9, #106): The `nonnegative ≠ nonzero` distinction is the most important teaching point — it's the one thing that trips authors. Context-aware C93 messaging makes it explicit. Proof sources for C76 and C93 are now unified: constraints, rules, ensures, and guards all feed the same narrowing markers (`$positive:`, `$nonneg:`, `$nonzero:`). Doc updates touched three files (language design, constraint violation, README) — RuntimeApiDesign.md was correctly scoped to public API and didn't need narrowing internals.
- Computed fields: the critical semantic contract is one recomputation pass after all mutations and before constraint evaluation.
- Modifier ordering: parser rigidity lived mostly in parser/completion surface; runtime/model layers were already largely order-independent.
- Guarded declarations: scope-inherited guards are the correct model; implementation gaps should be treated separately from design soundness.
- Access mode verb selection: `view`/`edit` is the strongest verb pairing because (a) universal vocabulary (document editors, file systems, CMS), (b) `view` is a proper verb for the `in <State>` slot (parallel to `assert`/`edit`), and (c) it doesn't carry connotations of prior state (`lock` implies previously unlocked). The verb's connotation matters as much as its denotation.
- Subtractive models (`without`) invert Precept's additive composition convention. Set arithmetic for "what exists here?" is cognitively harder than set union. Negative framing violates Principle #2 (English-ish).
- Reusing existing keywords (`with`) for new purposes saves keyword budget but creates semantic overload. `with` as a preposition doing a verb's job is syntactically weaker than a dedicated verb in the `in <State>` verb slot.
- The `to` preposition carries crossing semantics (Principle #10). Using it for steady-state field presence stretches the metaphor — especially when listing multiple states, which reads as "the field enters at each boundary" rather than "the field exists in these states."
- Cascade/override models (global baseline + per-state subtract/add) are powerful for complex topologies but create order-dependent semantics that violate Precept's additive composition. Three-verb systems (`view`/`edit`/`without`) are over-engineered for a minimal-ceremony language.
- `edit` alone should NOT trigger lifecycle scoping — this preserves backward compatibility. Only a dedicated presence verb (`view`/`lock`) triggers lifecycle scoping. `edit` contributes to the presence set of already-lifecycle-scoped fields.
- Per-state field access mode precedent landscape: structural absence is implemented by Plaid (typestate-oriented, per-typestate field sets) and Rust typestate (per-state-type structs), type-level variants by TypeScript discriminated unions, constraint-on-global by Alloy/TLA+/SCXML, and nothing by XState/Drools/Cedar/Rego. No surveyed system offers Precept's three-mode (absent/readonly/editable) spectrum — the concept is genuinely novel. `readonly` as a per-state declared mode has the weakest precedent support of the three modes.
- Docs work: `PreceptLanguageDesign.md`, editability/runtime docs, and MCP docs must stay synchronized whenever wording around constraints, updates, or inspection/editability changes.
- Unified-verb-plus-mode (`define X <mode>`) vs twin-verb (`view`/`edit`) is a trade-off between explicitness (all modes expressible, single learning point) and economy (fewer keywords, shorter statements, better AI discoverability). The keyword budget cost of the unified approach is 4 new keywords vs 1 for twin verbs. Adjective-category keywords (`readonly`, `editable`, `absent`) are a grammatical novelty in a language that has only used verbs, nouns, prepositions, and adverbs.
- When two syntactic forms can express the same semantics (e.g., `define X editable` and `edit X`), define a clean role partition: one form for unconditional, the other for conditional. Never leave both as equal aliases — it fragments codebases and confuses AI code generation.
- **30-system research synthesis confirms verb-as-mode over verb-plus-qualifier.** Every platform system that successfully handles field access modes encodes the mode in the action word: Salesforce (View/Edit), ServiceNow (Read Only toggles), Dynamics 365 (Show/Hide/Enable/Disable), OpenAPI (readOnly/writeOnly). No successful system uses a generic verb + trailing mode qualifier. The pattern that works in the real world is: the verb IS the mode.
- `view`/`edit` twin verbs remain the strongest synthesis after 5 creative explorations (verbless mode declarations, `field` reuse, `present`/`edit` semantic split, Cedar-inspired `allow` + action). The alternatives either add unnecessary keywords, introduce novel grammatical categories, or communicate less through more words.
- "Bare `field` = readonly" is an elegant implicit-mode pattern (Idea C: `in S field X` defaults to readonly via D3), but `field` doing double duty (declaration + mode) creates a moment of confusion on first encounter that `view` avoids.
- Permission-framing (`allow read`/`allow edit`) is a misfit for Precept's declaration model. `allow` implies request-response permission granting — Precept's modes are structural declarations, not authorization responses.
- Implicit absence is a feature, not a gap. Making silence meaningful (no declaration = absent) is what minimal ceremony means. Explicit `absent` is only needed for documentation and should be optional with a compiler hint.
- Proposal rewrite discipline: when a design decision is locked, rewrite the proposal in-place as though the decided syntax was always the design — no revision notes, no "formerly known as" hedging, no appendices about what changed. The proposal is a living document that reflects current state, not historical process.
- State-level access mode declarations (in `in <State> omit/view/edit <Fields>`) interact cleanly with existing `in <State> edit` and guarded `in <State> when G edit`. The guarded edit counts as access mode coverage for that field in that state. No conflict — just composition.
- `omit` vs `view` distinction is the author's tool for controlling re-entrance behavior: `omit` = data cleared on re-entry, `view` = data preserved. No additional syntax needed for cycle semantics.
- **D3 must be per-(field, state) pair, not per-field toggle.** D3 never "turns off" when other pairs for the same field have explicit declarations. This eliminates the lifecycle-scoping concept entirely and scales declarations proportionally to exceptions, not to field×state product. Hiring pipeline: 1 line vs 11 under the alternative (complete coverage) model.
- **`set` in transitions is NOT governed by `read`/`write` — only `omit` blocks it.** The `read`/`write` axis controls the Update API surface. `set` in the Fire pipeline can write to any present field. Only `omit` (structural absence) is a hard block on `set`. This is a new compile-time diagnostic: `set` targeting a field `omit`ted in the effective target state is an error.
- **Guarded `write` is the only guarded access mode.** `in S when G read F` (conditionally readable) and `in S when G omit F` (conditionally absent) are semantically meaningless/incoherent. Guards belong on the editability axis only — presence/absence is static.
- **`omit` clearing semantics:** On ANY transition into a state where a field is `omit`ted, the value resets to the field's declared default. This is deterministic, applies to initial creation, normal transitions, and cycle re-entry. Does NOT apply to `no transition`.
- **Root-level access modes unneeded for stateful precepts.** D3 IS the root-level baseline. Adding explicit root-level `read`/`omit` creates two ways to express the same semantics. Root-level `write` stays for stateless precepts only.
- **Pressure testing validates design:** All 6 scenarios (helpdesk/cycle, insurance/guarded-write, hiring/deep-linear, subscription/re-entrance, loan/guarded-write-D3, stateless) handled cleanly with `omit`/`read`/`write` + D3-always. No principle violations. One minor Principle #4 tension fully mitigated by tooling (hover shows effective mode).

## Recent Updates

### 2026-04-18 — Rewrote ProofEngineDesign.md for unified architecture

Rewrote `docs/ProofEngineDesign.md` (893 → 1095 lines) to reflect the unified ProofContext + LinearForm + RelationalGraph architecture from `temp/unified-proof-plan.md`.

Key structural decisions:

1. **Architecture framing replaced.** The "five-layer stack" is gone. The doc now describes three composing types (`ProofContext`, `LinearForm`, `RelationalGraph`) and one composing query (`IntervalOf`) as the architecture spine. Layer 1 → `WithAssignment`, Layer 2 → `NumericInterval` (unchanged), Layer 3 → `IntervalOf` dispatch, Layer 4 → LinearForm-keyed `_relationalFacts` + `RelationalGraph`, Layer 5 → `IntervalOf` conditional case.

2. **New sections added:** Research Foundations (zone domain / Cousot & Cousot / CodeContracts Pentagons / Boogie), Rational Type, LinearForm Normalization, Transitive Closure, Proof State Lifecycle and Scope, Coverage Matrix (22 patterns), Unsupported Patterns (19 rows), Risk Register, IntervalOf Query Path.

3. **Design Decisions #1 and #4 marked SUPERSEDED.** #1 (string-encoded interval markers) superseded by typed `ProofContext._fieldIntervals`. #4 (relational markers as a separate layer) superseded by LinearForm-keyed `_relationalFacts`. New decisions noted inline in each.

4. **Phasing updated.** Replaced "PR #108, Slices 11–15" framing with the unified PR six-commit structure. Follow-up A–E sections updated to reference `ProofContext` and `IntervalOf` instead of string markers.

5. **Doc is self-contained.** A reader can understand the full architecture without reading `temp/unified-proof-plan.md`. Implementation artifacts (commit ordering, test counts, file inventory) deliberately omitted — those belong in the PR body. The five gaps are described as problems the architecture solves, with the mechanism for each.

6. **NumericInterval details, IEEE 754 handling, and C94–C99 enforcement specs preserved intact** — these are correct and unchanged; only their framing was updated (e.g., `ctx.IntervalOf(expr)` instead of `TryInferInterval` call sites).

### 2026-04-17 — Applied review feedback to unified proof plan
- Applied all accepted George + Soup Nazi review findings to `temp/unified-proof-plan.md` per Shane's resolved overrides.
- B1: Added `checked` long arithmetic and cross-GCD pre-reduction to Rational spec (§0 constraint #1, §4 Rational.cs entry).
- B2: Added `ProofEngineUnsupportedPatternTests.cs` (4 tests) to §4 test table and §5 commit 3.
- Q2: Added §8 rows 21-22 (negated operand rule, compound-expression rule); updated coverage delta to 12 new-proves. Added §8a rows 18 (scalar-multiple, now "works") and 19 (pow/truncate opacity).
- NB2: Added parens depth-budget note to §4 LinearForm.cs entry.
- NB3 (Shane override): Added scalar-multiple GCD normalization to §2 Gap 1, §4 ProofContext.cs (~350→~360 LOC), §4 CompoundDivisorTests (15→17).
- NB5-NB9: Updated all test counts — LinearFormTests 35→40, RationalTests 20→23, ProofContextTests 30→33, TransitiveClosureTests 14→17, SoundnessInvariantTests 10→15.
- Cascaded all count changes through §5 commit ordering (1427→1437→1493→1511→1528→1555).
- Final totals: 191 new tests, ~1555 total. No architectural spine changes.

### 2026-04-17 — Unified proof plan updated with research findings and Rational decision
- Updated `temp/unified-proof-plan.md` with all post-endorsement research and decisions.
- Added §0 "Research findings" subsection: academic grounding (zone abstract domain / Cousot & Cousot 1977, Miné zone/octagon domains, QF_LRA under-approximation), reference implementations (CodeContracts/Clousot Pentagons domain, Boogie IntervalDomain.cs, Crab/IKOS/Apron), library survey conclusion (no reusable .NET library exists).
- Updated §3 soundness assertion #1: replaced BigInteger-backed Rational spec with `readonly record struct Rational(long Numerator, long Denominator)` implementing `INumber<Rational>`, .NET 10 native, ~100 LOC, `long/long`. Included full decision chain as rationale.
- Added §3 "Grounding audit" subsection: verified all six marker conventions, narrowing method shapes, kill loop, and scope model against codebase.
- Updated §4 manifest entries for Rational.cs (~150→~100 LOC) and RationalTests.cs (expanded test spec).
- Updated §7 Division in LinearForm exclusion to specify `long/long` Rational coefficients.
- Added §9 "Post-endorsement update" note: architectural spine unchanged, endorsements remain valid.

- PreceptTypeChecker.cs decomposition analysis (2026-04-18): Analyzed the full 3197-line file (53 methods) for partial-class decomposition. Key findings: (1) Shane's proposed "Diagnostics" group does not exist as a natural seam — diagnostic emissions are inline, not standalone methods. Replaced with TypeInference (~750 lines) and FieldConstraints (~330 lines). (2) IntervalInference (~157 lines) too small for its own file — merged into ProofChecks (~380 lines combined). (3) Zero visibility changes needed — C# partial classes compile as one class, so `private` members are accessible across all partial files. This eliminates the biggest risk. (4) Commit ordering is bottom-up by dependency: Helpers → FieldConstraints → Narrowing → ProofChecks → TypeInference. (5) Future-proofing validated: #111's C94–C99 land cleanly in ProofChecks, #107/#95 type additions land in TypeInference, #112 stateless events land in main orchestration file. Filed as issue #118, milestone M4. Decision in `.squad/decisions/inbox/frank-typechecker-decomposition.md`.
- No architectural decisions changed. All edits are refinements and external validation.

### 2026-04-18 — PR #108 body updated: replaced stale Slices 11–16 with unified proof plan
- Replaced Slices 11–16 (old per-slice narrowing approach) in PR #108 body with the 6-commit unified ProofContext + LinearForm architecture from `temp/unified-proof-plan.md`.
- Preserved Slices 1–10 content exactly (all checked — shipped). New Commits 1–6 all unchecked.
- Updated Summary: reflects Slices 1–10 as shipped plus the unified proof engine delivering ProofContext + LinearForm + Rational + RelationalGraph, closing all 5 proof gaps.
- Updated Why: added the linear-form decision rationale and explicit SMT/Z3 rejection.
- Updated Implementation Plan: 6 commits with method-level specificity, exact file paths, per-commit test counts (1469 baseline → 1532 → 1542 → 1598 → 1616 → 1633 → 1660), regression anchors, dependency graph, and tests-that-MUST-FLIP table.
- Added Coverage Matrix (22 patterns, 12 new provers, 0 regressions), Unsupported Patterns (§8a) summary, and Risk Register (§6) table per task requirements.
- Updated File Inventory (18 new files), Tooling/MCP sync assessment, and Validation (1469 current → ~1660 target, 191 new tests).
- Key decision: used 1660 as the actual target (1469 + 191) rather than the plan document's 1555 (which used a stale 1364 baseline). The additional 105 tests came from post-Slices-1-10 commits (e.g., span precision work).

### 2026-04-18 — Issue #118 revised: type checker decomposition grounded in ProofEngineDesign.md
- Shane flagged that issue #118's decomposition was built from the current file state, not the post-PR-#108 target state described in `docs/ProofEngineDesign.md`. Revised the entire issue body.
- Key corrections: (1) Added architectural boundary section clarifying what lives on `ProofContext.cs` (IntervalOf, KnowsNonzero, WithAssignment, WithRule, WithGuard, Child, Dump, LookupRelationalInterval, GcdNormalize) vs `PreceptTypeChecker.cs` — the design doc's Integration Points table is the authoritative source. (2) ProofChecks group grew from ~380 to ~500-550 lines to account for the shared assessment model (Commit 11) and C94-C98 enforcement methods (Commit 12) that will exist in the file before this refactoring runs. (3) Updated line estimates from ~3200 to ~3500-3600 for the post-PR-#108 file. (4) Fixed future-proofing: #111 is now scoped to the `nonzero` modifier, not C94-C99 (those land in PR #108). (5) Added C95/C96 integration note for ValidateRules in the main file. (6) Updated Narrowing group for typed RelationalFact rekeying (Commit 9).
- Lesson: decomposition issues for files under active development MUST target the to-be state from the canonical design doc, not the as-is state that will change before the refactoring runs.
### 2026-04-15 — Issue #100 precept name token scope fix (PR #101)
- Fixed `machineDeclaration` capture 4 scope from `entity.name.precept.message.precept` → `entity.name.type.precept.precept` in the TextMate grammar.
- Removed the old scope from the `preceptMessage` semanticTokenScopes mapping in package.json.
- Added explicit Structure Indigo (`#6366F1`) textMateRule for `entity.name.type.precept.precept`.
- No runtime, language server, MCP, or doc changes required — purely a tooling scope correction.
- All 1,476 tests pass. PR #101 marked ready for review.



### 2026-04-15 — Pressure Test Analysis: `omit`/`read`/`write` Field Access Mode Design
- **Task:** Deep analysis of 5 design questions and 6 pressure test scenarios against the `omit`/`read`/`write` syntax (Shane renamed `view`→`read`, `edit`→`write`).
- **Verdict: Design holds.** All 6 scenarios handled cleanly. No principle violations. No better alternative found.
- **Critical D3 refinement:** D3 must operate at the (field, state) pair level, never "turning off." Undeclared pairs always get `read`. This eliminates lifecycle-scoping complexity and the complete-coverage requirement.
- **Q1 (set vs target access mode):** Compiler must check target state. `set` into `omit`ted target is a compile-time error — dead mutation.
- **Q2 (root-level + overrides):** Not needed. D3 IS the root-level baseline. Hiring pipeline: 1 line instead of 11.
- **Q3 (partial D3):** D3 never stops. Per-pair, not per-field. A declaration for (X, Draft) does not affect (X, Screening).
- **Q4 (set into omit):** Compile-time error. Forces author to resolve intent (remove `set` or change mode).
- **Q5 (sticky vs state-determined):** State-determined. `omit` clears on entry. `read` preserves. Author controls re-entrance behavior via mode choice.
- **Composition model formalized:** 7 rules covering D3 baseline, explicit override, guarded write, omit clearing, set validation, contradiction detection, root-level write.
- **Key finding:** `set` in transitions is NOT governed by `read`/`write` — only `omit` blocks it. The `read`/`write` axis is an Update API concern; `set` is a Fire pipeline concern.
- Analysis filed: `.squad/decisions/inbox/frank-pressure-test-analysis.md`

### 2026-04-15 — Null-Reduction Proposal Rewrite: Decided Field Access Mode Syntax
- **Task:** Rewrote `research/language/expressiveness/null-reduction-proposal.md` in-place to replace all Option B (`visible in`/`visible after`) references with the decided `in <StateList> omit/view/edit <FieldList>` syntax.
- **Scope:** Section 1 (preamble item 2), Section 4 (complete rewrite — removed Options A/B/C, presented decided syntax as THE design), Section 5 (computed field absence updated to `omit` semantics), Section 7 (all feature interactions rewritten for `omit`/`view`/`edit`), Section 8 (MCP/language server/diagram references updated), Section 9 (before/after rewritten with decided syntax), Section 10 (decision matrix updated — access mode syntax marked DECIDED), Section 11 (open questions revised for new model), Appendix A (all three sample rewrites use `omit`/`view`/`edit`).
- **Key design choices captured in rewrite:** D3 default (no declarations = globally present + readonly), lifecycle scoping trigger (only `omit`/`view` trigger it, not `edit` alone), complete state coverage for lifecycle-scoped fields, `omit`/`view` distinction as the re-entrance control mechanism, guarded `edit` counts as access mode coverage.
- **`visible` keyword eliminated:** Zero references to `visible in`, `visible after`, or `visible` as a keyword remain in the proposal.
- Sections 2, 3, 6 left unchanged (null vocabulary, `optional`, `is set`/`is not set`, `clear` — all locked decisions).

### 2026-04-15 — Creative Syntax Exploration: Field Access Mode Declarations
- **Task:** Shane requested outside-the-box synthesis of all prior analyses (4 documents), 30-system research survey (3 research files), design principles, parser architecture, and sample files — to produce genuinely creative syntax ideas for per-state field access mode declarations.
- **Explored 5 creative ideas:** (A) `view`/`edit` twin verbs — precedent-backed synthesis winner; (B) Verbless mode declarations — `in S readonly X` pattern, bold but 3 adjective keywords; (C) `field` reuse — Plaid-inspired per-state field re-declaration; (D) `present`/`edit` semantic split — most precise but communicates less; (E) Cedar-inspired `allow read`/`allow edit` — permission model misfit.
- **Ranked recommendation:** A > C > B > D > E. `view`/`edit` twin verbs is #1 — 1 new keyword, LL(1), 9/10 AI discoverability, universal vocabulary, `edit` unchanged, most compact (3 lines for helpdesk). Research from all 30 systems unanimously supports verb-as-mode over verb-plus-qualifier.
- **Key creative discovery:** Idea C (`field` reuse with implicit readonly) is genuinely novel and Plaid-backed, but introduces confusion between field declaration and mode declaration contexts that `view` avoids.
- Decision note filed: `.squad/decisions/inbox/frank-creative-syntax-exploration.md`



### 2026-04-16 — External Research: PLT & DSL Precedents for Per-State Field Access Modes
- **Task:** Survey 10 systems (Alloy, TLA+, SCXML, Plaid, Rust typestate, TypeScript discriminated unions, XState v5, Drools, Cedar, Rego) for how they handle fields having different properties depending on entity state.
- **Key finding:** Systems cluster into 4 patterns — structural per-state fields (Plaid, Rust typestate), type-level variants (TypeScript), constraint-on-global-fields (Alloy, TLA+, SCXML), and unscoped/no-per-state-concept (XState, Drools, Cedar, Rego).
- **Plaid is strongest precedent** for structural per-state field presence — proves the concept is implementable and type-checkable. Rust typestate pattern is production-proven in mainstream ecosystem.
- **TypeScript discriminated unions are closest mainstream analogy** — the `NetworkState` pattern is structurally identical to Precept's `define` use case. Massive adoption validates developer comprehension.
- **No precedent system has all three modes** (absent/readonly/editable). Precept's three-mode spectrum is genuinely novel. `readonly` as a per-state declared mode has essentially no precedent.
- **Adjective-category keywords (`readonly`, `editable`, `absent`) have no DSL precedent** — confirms the grammatical novelty concern from the `define` syntax analysis.
- Research filed: `research/language/expressiveness/field-access-mode-precedents.md`

### 2026-04-15 — Deep Analysis of `define` Syntax for State-Scoped Field Access Modes
- **Task:** Shane locked Site 2 with shape `in <StateList> <verb> <FieldList> <mode>`. Evaluating `define` as the verb, multi-field composition, `edit` coexistence, grammar coherence, AI discoverability, and applied real-sample rewrites.
- **Verb assessment:** `define` is the strongest candidate for the unified-verb-plus-mode shape. Correct part of speech, neutral connotation, scale-compatible across all three modes. Named-predicate collision (issue #8) is the key concern — manageable at LL(2) but creates semantic overload. `mark` is the runner-up verb if collision is unacceptable.
- **Multi-field composition:** Mixed modes on one line (e.g., `define X editable, Y readonly`) creates parser ambiguity. Grammar A (single mode per line, multi-field same-mode shorthand) is correct. Grammar B (per-field mode) kills the shorthand. The complexity of disambiguation doesn't justify the marginal savings.
- **`edit` coexistence — Model I recommended:** `define` handles all unconditional mode declarations. `edit` retains exclusive role for guarded (conditional) editability with `when` guards. Unguarded `edit` soft-deprecated as alias for `define ... editable`. Clean role separation, no overlap.
- **Grammar coherence:** LL(1) disambiguation after `in <StateTarget>` — `define` is a distinct verb token alongside `assert`, `edit`, `when`. Root-level `define` needs LL(2) if named predicates also use `define`. Four new keywords (`define`, `readonly`, `editable`, `absent`) — highest keyword cost of any approach analyzed. Three new adjective-category keywords are unprecedented in the language.
- **Honest trade-off disclosed:** Twin verbs (`view`/`edit`) remain architecturally stronger — 1 keyword vs 4, shorter statements, better AI discoverability, no `absent` over-specification temptation. The `define` approach trades keyword economy for explicitness and a single-verb learning point.
- **Five open questions for Shane:** named-predicate collision policy, explicit `absent` encouragement vs discouragement, `edit` deprecation timeline, adjective keywords acceptability, `define` without mode default.
- Decision note filed: `.squad/decisions/inbox/frank-define-syntax-analysis.md`

### 2026-04-15 — `in/to/from` Access Mode Syntax Creative Exploration
- **Task:** Shane chose Site 2 (state-level declarations) for field access modes and asked for creative exploration of how the existing `in/to/from` preposition system could express absent/readonly/editable — not just "bolt on `include`."
- **Explored 7 distinct syntax ideas:** (1) `view`/`edit` twin verbs, (2) subtractive `without`, (3) `to <State> with` entry introduction, (4) `in <State> with` zero-new-keywords, (5) combined `view X edit Y` single-line, (6) `any` cascade with `without` overrides, (7) `lock`/`edit` antonym pair.
- **Each idea developed with:** full helpdesk ticket rewrite, three-mode expression table, multi-state/`any` support, grammar/parser feasibility, `edit` interaction analysis, static guarantee proof, and interest/flaw assessment.
- **Ranked recommendation:** `view`/`edit` twin verbs is #1 — universal vocabulary, grammatically parallel to existing `edit`, perfect Principle #10 compliance, additive composition, one new keyword that names the readonly mode explicitly. `lock`/`edit` is #2 (tight antonym pair but implies prior unlocked state). `in <State> with` is #3 (zero new keywords but overloads `with`).
- **Key design principle confirmed:** `edit` alone should NOT trigger lifecycle scoping — backward compatibility requires a dedicated presence verb as the scoping trigger.
- **Five open questions surfaced for Shane:** verb preference, `edit`-implies-presence rule, guarded `view` policy, `view all` shorthand, and `view`+`edit` same-state conflict resolution.
- Decision note filed: `.squad/decisions/inbox/frank-in-to-from-access-modes.md`

### 2026-04-13 — Issue #88 docs sync completed for PR #90
- Reconciled the editability/documentation story across `docs/PreceptLanguageDesign.md`, `docs/EditableFieldsDesign.md`, `docs/RuntimeApiDesign.md`, and `docs/McpServerDesign.md`.
- Validation recorded on branch `squad/88-docs-reconcile-editability`: `git diff --check` and `dotnet test --no-restore`.
- Commit `9ea5609` pushed; PR #90 checklist updated and left clean for review.

### 2026-04-12 — Squad `@copilot` lane retirement contract review
- Confirmed the change is narrowly scoped to retiring the Squad-owned `squad:copilot` routing lane while preserving general repo-wide Copilot tooling.
- Required all live workflows, mirrored templates, and squad docs to agree so the lane is retired cleanly.

### 2026-04-18 — Wave 2 code review: PR #108 strangler-fig migration (Steps 7b-i through 8)
- **APPROVED** — 6 commits, 8 files, +507/−386 lines. Strangler-fig migration from string-encoded proof markers to typed stores, followed by scope split.
- Marker elimination total: zero `$ival:`, `$positive:`, `$nonneg:`, `$nonzero:`, `$gt:`, `$gte:` in `src/Precept/Dsl/`. `ToMarkerKey`, `TryParseMarkerKey`, `LookupLegacyRelationalInterval` all deleted.
- All Wave 1 carry-forward findings verified: G1 (GCD normalization) resolved — all storage uses `GcdNormalize`. G2 (flag implications) resolved — all 8 write sites correct. G4 (reference aliasing) partially resolved — `WithRule` still shares `_fieldIntervals`/`_flags`/`_exprFacts` by reference (safe today, fragile by design).
- `BuildEventEnsureNarrowings` rewrite eliminates string-surgery bug class by construction — `LinearForm.Rekey()` replaces `:` split/reassemble.
- Scope split simpler than spec (single class with `Child()`/`ChildMerging()` instead of two-class hierarchy) — correct and sufficient.
- 4 non-blocking warnings: `WithRule` reference sharing (W1), stale `ExtractIntervalFromMarkers` name (W2), 5 stale `ProofContext` comment references (W3), `ValidateStateActions` manual construction instead of `ChildMerging` (W4).
- Key insight: the strangler-fig sequence (dual-write → switch reads → remove old → scope split) is the model for any future migration of shared mutable state in the type checker.

### 2026-04-12 — Issue #9 design review resolutions incorporated
- Folded the resolved design decisions back into the issue body, including null-handling, split diagnostics (C72/C73), and inspect trace expectations.
- Design left implementation-ready after proposal/body synchronization.

### 2026-04-18 — Issue #118 future-proofing: temporal + quantity fit analysis
- Mapped detailed designs for #107 (temporal, 8 types) and #95 (currency/quantity, 7 types) against the proposed 6-file partial class decomposition in issue #118.
- **TypeInference.cs scaling problem:** Combined growth from both proposals balloons the file from ~800 to ~1680-2135 lines (operator tables, typed constants, dot-accessor resolution). Planned split point: extract `TryInferTemporalBinaryKind` and `TryInferDomainBinaryKind` dispatch into a 7th partial `PreceptTypeChecker.DomainTypeInference.cs` when the first proposal lands — not pre-emptively.
- **All other 5 files confirmed clean fit:** FieldConstraints (+90-160 lines from `in`/`of` validation, temporal constraint rejection), ProofChecks (+20-40, neither proposal adds proof engine integration — currency doc explicitly says "proof engine does not need modification"), Narrowing (+70-120, discrete equality narrowing via `TryApplyEqualityNarrowing` fits existing pattern), Helpers (+100-160, mechanical `StaticValueKind`/`MapScalarType` additions), Main (stable).
- Key insight: discrete equality narrowing (`$eq:` markers for unit/currency/dimension compatibility) is a parallel layer to existing numeric/null narrowing — same pipeline, different proof surface.
- Updated issue #118 body with expanded per-file impact table, scaling analysis, planned split documentation, and revised routing table.

## 2026-04-19 — Research: Type Checker Architecture (meta-evaluation)
- Researched 6 production typecheckers (Roslyn, TypeScript, Rust, Swift, Kotlin K2, F#) to evaluate Precept's 6-partial-class split.
- Verdict: KEEP AS-IS. Our split has good external precedent. Strongest match is Kotlin K2's phase model.
- Output: research/architecture/typechecker-architecture-survey-frank.md
- Updated Issue #9 proposal body with all 4 resolved must-resolve items from the design review (Decisions 6–9).
- **Decision 6:** `else` branch null-narrowing — original type retained, no reverse narrowing. #14 precedent.
- **Decision 7:** Separate diagnostic codes C72 (non-boolean condition) and C73 (branch type mismatch), split from C39.
- **Decision 8:** Nullable boolean condition = compile error (C72). Must be non-nullable boolean. Consistent with `when` guard behavior.
- **Decision 9:** MCP Inspect trace shows `conditionResult` and `branchTaken` fields — inspectability means showing the reasoning.
- Updated ACs: AC-3 (C39→C72), AC-5 (else branch behavior), AC-9 (trace field names), AC-12 (per-component test breakdown). Added AC-13 (C72/C73 codes) and AC-14 (nullable boolean rejection).
- Updated teachable error messages table, semantic rules (added rules 7–8), implementation scope, and locked decisions section.
- Posted resolution comment. Design approved — ready for implementation.
- Decision filed: `.squad/decisions/inbox/frank-issue9-resolutions.md`

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

### 2026-04-15 — Field Access Mode Declaration Syntax Analysis
- **Task:** Comprehensive analysis of 7 syntax alternatives for declaring field access modes (absent/readonly/editable) per state, as part of the null-reduction proposal.
- **Ranked recommendation:** Option F (split concerns — `visible in/after` on field line + separate `edit` declarations) is the #1 recommendation. Option B (`include` + `edit` as parallel verb declarations) is #2.
- **Key architectural insight:** Presence and editability are different concerns with different scopes. Presence is per-field metadata (like type and default). Editability is per-state behavior (like asserts). Forcing them into a single mechanism adds ceremony for the common case.
- **Default policy locked:** D3 — fields with no access declaration are globally present + readonly (backward compatible). Fields with any lifecycle declaration are lifecycle-scoped. `edit` alone does NOT trigger lifecycle scoping.
- **Design principles that decided it:** Principle #4 (locality of reference — visibility belongs on the field line), Principle #3 (minimal ceremony — fewer lines than parallel declarations), Principle #9 (parser impact — `visible` as modifier is less disruptive than `after` as a new statement-level keyword).
- **Rejected with rationale:** Option C (scope blocks — violates locked flat grammar), Option A (inline modifiers — line explosion, parser complexity), Option D (access matrix — breaks preposition convention). `absent` keyword deemed unnecessary under D3 policy. `editable` replacing `edit` rejected — `edit` is proven in 17 sample files.
- **`after` scoping insight:** In Option F, `after` appears only inside the `visible` modifier (not as a statement-level preposition). This is LESS grammar disruption than Options B/E which add `after` as a top-level keyword.
- **Cross-cutting rules documented:** `from any` rows need `is set` guard for lifecycle fields; compiler validates `set` targets are visible in transition target state; `edit` implies presence if field is lifecycle-scoped.
- Decision note filed: `.squad/decisions/inbox/frank-field-access-modes.md`

### 2026-04-15 — Three-Axis Field Access Mode Deep Analysis
- **Task:** Shane sees three declaration sites (field-level, state-level, event-level) for field access modes. Deep comparative analysis with full sample rewrites, static guarantee proofs, and philosophy evaluation.
- **Critical discovery: the Cycle Problem.** `visible after <Event>` with transitive reachability degenerates to "globally present" in cyclic graphs (8/25 samples have cycles). Forward reachability from Assign's target in the helpdesk reaches ALL states via the Reopen cycle. This means event-anchored inference is unreliable without a cycle-breaking algorithm.
- **Cycle resolution:** Three strategies: S1 (explicit state list — always works), S2 (cycle-breaking inference — complex but tractable), S3 (state-determined visibility — clean but loses data on re-entrance). Recommended S1 as primary mechanism, S2 as optional sugar.
- **`no transition` tension resolved.** Fields set by `no transition` events (e.g., Triage sets TicketTitle without changing state) cannot be lifecycle-scoped — the `set` targets a field that would be absent in the current state. These fields must be globally present with a concrete default. Only fields introduced by state-CHANGING transitions are true lifecycle-scoping candidates.
- **Ranked recommendation unchanged:** Site 1 (field-level) is #1 — strongest static guarantees, best locality (Principle #4), zero new lines (Principle #3), explicit `visible in` is cycle-proof. Site 2 (state-level `include`) is #2 — also cycle-proof but more lines, field line says nothing about lifecycle. Site 3 (event-level `introduce`) is #3/REJECT — cycle problem is fatal for primary use, no explicit override mechanism, weakest static guarantee.
- **Hybrid recommendation:** Field-level with both forms: `visible in <States>` (primary, always works) and `visible after <Event>` (sugar, simple cases). Compiler resolves `after` to explicit list and displays it.
- **Sticky vs state-determined resolved:** Under explicit `visible in`, the author's list IS the contract. State-determined is the default mental model. If the author wants a field to survive a cycle back, they include the earlier state in the list (effectively making it global).
- **Key insight: one site per concern.** Mixing Sites 1 and 2 (field-level `visible` + state-level `include`) creates two sources of truth. One site for presence (field-level). One site for editability (state-level). `edit` already proves this split works.
- Decision note filed: `.squad/decisions/inbox/frank-three-axis-access-modes.md`

### 2026-04-10 — Structural lifecycle modifiers research
- Produced comprehensive research document at `research/language/expressiveness/structural-lifecycle-modifiers.md`.
- **Taxonomy:** Mapped 5 modifier categories (boundary, path, residency, cardinality, safety) covering 6 candidate modifiers: `terminal`, `required`/`milestone`, `transient`, `final`, `once`, `idempotent`.
- **Survey:** Compared 8 systems (XState, Step Functions, BPMN, Stateless, TLA+/Alloy, Temporal, UML, Cedar/OPA). Terminal/final states are the only modifier with broad cross-system precedent.
- **Tiers:** `terminal` is Tier 1 (strong candidate — compile-time provable, clear `initial` parallel, zero runtime cost). `required`/`transient` are Tier 2 (interesting but guard-dependent false positives or weaker-than-expected guarantees). Event modifiers (`final`, `once`, `idempotent`) are Tier 3 (reject/defer — require runtime state or break row independence).
- **Key insight:** State modifiers constrain graph-structural roles (compile-time provable). Event modifiers constrain behavioral properties (need runtime tracking). The asymmetry is fundamental, not incidental.
- **Domain taxonomy decision:** Created new domain "Structural lifecycle modifiers" (domain #23 in domain-map). Not an extension of existing domains — it adds *new declarations* that create new provable properties, distinct from constraint composition (value constraints), static reasoning (proving from existing declarations), and entity modeling (data shape).
- Updated `research/language/README.md` domain index and horizon domains list. Updated `research/language/domain-map.md` with domain #23 entry and priority assessment.
### 2026-04-11 — Constraint scoping symmetry analysis: full design space exploration
- Shane asked whether there's a mechanism that would make the scoping model symmetrical across all constraint declarations (invariant, assert, edit), without the problems identified in the `when`-on-assert rejection.
- **Explored 10 structural alternatives:** (A) invariant with state prefix — collapses invariant/assert, rejected; (B) universal `when` on everything — event asserts don't need it; (C) rename `when` to `where` — cosmetic; (D) block-scoped `when` (FluentValidation pattern) — breaks flat grammar; (E) invariant gains preposition — same as A; (F) extend `when` to assert — RECOMMENDED; (G) prefix/suffix model — same as F; (H) eliminate invariant as `in any assert` — loses Principle #5; (I) scoped blocks with `end` — overengineered; (J) accept asymmetry as-is — creates teaching gap post-#14.
- **Key structural insight:** The asymmetry isn't between `invariant` and `assert`. It's between **data-truth declarations** and **event-truth declarations**. All data-truth forms (`invariant`, `in/to/from assert`, `in edit`) should accept `when`. Event-truth (`on <Event> assert`) should not — different scope contract (transient input vs. persistent state).
- **Recommendation: Expand issue #14 to include `when` on `in/to/from <State> assert`.** Completes the data-truth conditional surface. Zero new constructs — reuses `WhenOpt` same grammar production. Marginal implementation cost. Improves teachability ("any data-truth declaration accepts `when`"), readability (replaces implication pattern `not G or X`), and inspectability (inspector shows guard-skipped vs. guard-evaluated).
- **Sole justified exception:** `on <Event> assert` excluded — scope category mismatch (event args are transient, no persistent data condition should gate input validation). Zero samples need it. Implication pattern is natural for same-scope operands.
- **Two-axis model:** Scope axis (global / state prefix / event prefix) × Condition axis (unconditional / `when` guard). Every cell is filled or explicitly excluded with principled reason.
- Decision note filed at `.squad/decisions/inbox/frank-constraint-scoping-symmetry.md`. Awaiting Shane sign-off.

### 2026-04-11 — Should `in <State>` become a general-purpose expression predicate? Analysis and verdict
- Shane asked whether `in <State>` should be promoted from a declaration-level prefix to a boolean predicate usable in any expression position (guards, `if...then...else`, `set` RHS, invariants).
- **Verdict: EXPLICITLY REJECT. Do not pursue, not even as a deferred item.**
- **Three independent disqualifying grounds:** (1) Principle #5 violation — collapses the data-truth/movement-truth boundary by mixing state queries into data expressions; (2) No precedent in comparable systems — state-as-predicate only exists in parallel-region statecharts (Harel, SCXML), which Precept does not support; every declarative rule language comparable to Precept keeps state out of expressions (Cedar, DMN, Drools, OPA, FluentValidation); (3) Every use case has a better targeted solution through existing or planned mechanisms.
- **Specific confusion identified:** Homophone ambiguity between `in Draft assert X` (declaration scope) and `when in Draft` (boolean test) — same words, different mechanisms. Expression evaluator scope leak — currently pure (fields/args/literals), would need runtime state. Stateless incoherence — state predicate in a stateless precept is a category error. Redundancy — doubles the AI authoring surface.
- **If state-dependent computed values are needed (issue #17):** The correct answer is `in <State> computed X -> expr` (declaration-scoped derivation, consistent with existing `in <State>` patterns), NOT a general-purpose state predicate.
- This analysis is adjacent to issue #14; issue #14's locked Decision #3 (field-scope only, no state references in `when` guards) is validated and reinforced by this verdict.
- Decision note filed at `.squad/decisions/inbox/frank-in-state-predicate-rejection.md`.

### 2026-04-11 — Should `invariant`/`assert` be renamed to `rule`? Analysis and verdict
- Shane asked whether the current two-keyword system (`invariant` for data constraints, `assert` for state/event constraints) should collapse into a single `rule` keyword.
- **Verdict: KEEP `invariant` and `assert`. Do NOT unify to `rule`.**
- Philosophy analysis: `docs/philosophy.md` uses "rules" as the umbrella concept (~47 occurrences) but explicitly distinguishes "invariants" from "assertions" in its own definitions (line 15, line 30, line 48). The two-keyword system IS the philosophy's design.
- Principle #5 ("Data truth vs movement truth — the keyword tells you the category") is directly encoded in the keyword split. Collapsing to `rule` loses the signal.
- Grammar test: `assert` is a verb and works after prepositions (`in Approved assert X`). `rule` is a noun and reads awkwardly in the same position (`in Approved rule X`). The keywords are grammatically correct for their syntactic positions.
- Domain expert test: Domain experts don't say "invariant," but they DO understand "always true" vs "true here." The `because` clause makes intent transparent on first reading. Jargon learned in one example is acceptable; imprecision that causes ongoing confusion is not.
- Named predicate impact: Keeping the split makes `define` (from the prior recommendation) cleaner — the constraint surface uses `invariant`/`assert`, the naming surface uses `define`. No overlap, no confusion.
- Addressed all 8 analysis dimensions: philosophy alignment, current semantics, RulesDesign.md history, domain expert test, unified case, split-keyword case, named predicate interaction, principle-by-principle scoring.
- Decision note filed at `.squad/decisions/inbox/frank-rule-keyword-unification.md`. Awaiting Shane sign-off.

### 2026-04-11 — Issue #8 keyword analysis: `rule` vs `define` for named predicates
- Shane raised: "Is it confusing to declare a rule that is not really a rule?" — questioning the April 5 convergence on `rule <Name> when <BoolExpr>`.
- **Critical finding:** `rule` is NOT a current DSL keyword. The `PreceptToken` enum has no `Rule` entry; the parser doesn't recognize it; no sample uses it. The RulesDesign.md concepts were implemented as `invariant`/`assert` per the implementation plan terminology change. The premise of a "keyword split" is factually incorrect — but the naming concern is valid on its own merits.
- **Analysis:** Named predicates are fundamentally routing conditions (they start false, become true), not data constraints. They cannot be self-enforcing without breaking initial-state validity. The word "rule" implies enforcement; using it for a non-enforcing declaration breaks the pattern that every constraint-shaped keyword in Precept is operative.
- **Recommendation:** Use `define` instead of `rule`. "define LoanEligible when ..." signals naming, not enforcement. Aligns with Principles #1 (inspectability), #2 (English-ish), #5 (data truth vs movement truth). Compiler should warn on unreferenced definitions (dead-code detection).
- Decision note filed at `.squad/decisions/inbox/frank-named-rule-keyword-confusion.md`. Awaiting Shane sign-off.

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

### 2026-04-11 — Verdict modifiers research: externally grounded rewrite
- Completely rewrote `research/language/expressiveness/verdict-modifiers.md` with all comparable-system claims grounded in actual fetched documentation from 7 systems: FluentValidation (docs.fluentvalidation.net), ESLint (eslint.org), XState v5 (stately.ai), BPMN 2.0/Camunda, Kubernetes (kubernetes.io), Cedar (docs.cedarpolicy.com), Roslyn/.NET (learn.microsoft.com).
- **Major structural additions:** (1) New Cross-System Synthesis section extracting 5 universal patterns from the evidence base. (2) New Architectural Decision section explicitly framing declaration-level vs configuration-level as THE key design choice, recommending Option A (declaration-level) for Precept's one-file-completeness guarantee. (3) External Documentation Sources table with URLs and access dates.
- **Strongest new finding (Pattern 5):** Zero comparable systems put verdict severity on state declarations. XState has tags/meta but no severity. BPMN applies severity to events, not states. Step Functions' `Succeed`/`Fail` are structural types, not annotations. State verdict is genuinely novel territory — significantly riskier than v1 analysis suggested.
- **Key analytical upgrade:** FluentValidation's `IsValid`-is-still-false behavior documented as evidence for Pattern 1 (severity is always metadata). Kubernetes evaluation-enforcement separation documented as the purest configuration-level model. BPMN error/escalation distinction documented as the strongest three-tier precedent.
- **Tier adjustments:** Event verdict remains Tier 1 (now with stronger grounding). State verdict remains Tier 2 but with significantly upgraded caution flag. Rule verdict blocked on enforcement behavior question (does warning block or allow?).
- **Recommendation unchanged but strengthened:** Start with event verdict only (Tier 1). Declaration-level architecture. One-file completeness is non-negotiable.
- Decision inbox updated at `.squad/decisions/inbox/frank-verdict-modifier-research.md`.

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

### 2026-04-11 — Verdict Modifier Design Options (All Tiers)

**Produced:** `research/language/expressiveness/verdict-modifier-design-options.md` + decision note

**Context:** Shane approved moving forward with verdict modifiers across all 3 tiers (events, rules, states) in research mode. Task was to convert the background research into concrete design options (what would implementation look like? what trade-offs?).

**Deliverables:**
- **Events (Option A Recommended):** Outcome-shape declaration (`event Approve success`). Declares intent; compiler verifies rows match (C58–C59). Precedent strong; philosophy aligned.
- **Rules (Path 1 Recommended, Path 2 Open):** Warnings block (Path 1, maintains philosophy) or don't block (Path 2, requires philosophy refresh). THIS IS THE CRITICAL OPEN GATE. Shane must choose before rules design finalizes.
- **States (Option A Recommended):** Endpoint categorization (`state Approved success`). Enables reachability diagnostics (C65–C67); improves visualization. Genuinely novel; no comparable system does this. Value prop requires use-case validation before proposal.

**Architecture decisions locked (declaration-level, metadata-only):**
- All verdicts are author-declared in `.precept` file (one-file completeness)
- No runtime behavioral changes (prevention guarantee intact)
- All impact is styling, diagnostics, and tooling (MCP, diagrams, IDE indicators)
- Verdicts are intent metadata, not enforcement overrides

**Critical findings:**

1. **The non-blocking warning question cuts to philosophy.** Shane said "if we're doing this, non-error rules should be non-blocking." But non-blocking violates "prevention, not detection." Three paths:
   - Path 1: Warnings block anyway (maintain philosophy, simpler, but "warning that blocks" is confusing)
   - Path 2: Warnings don't block (flexible, but requires philosophy refresh downgrading core guarantee)
   - Path 3: New outcome category (unproven complexity)
   
   Recommended Path 1 but flagged as BLOCKING DECISION for Shane. If Path 2 desired, philosophy team must refresh guarantee language first.

2. **State verdicts are novel opportunity.** No comparable DSL annotates states with success/failure verdicts. This is either a differentiator (valuable) or overdesign (not useful). Requires validation: gather 2-3 real `precept` files to demonstrate readability improvement.

3. **Event verdicts are the strong candidate.** Broad precedent (BPMN, FluentValidation, Roslyn); compile-time provable from declared rows; philosophy coherent. Option A has highest confidence.

4. **All options are declaration-level, metadata-only.** Deliberately rejected configuration-level model (ESLint/Kubernetes style) for Precept — violates one-file completeness and is less suitable for non-programmer audience.

**Process learnings:**
- Architectural options document is distinct from background research. Research answers "how do comparable systems work?" Options answer "what would THIS look like?" Both are needed.
- Philosophy-level gates (like non-blocking semantics) must be escalated early; they block design finalization.
- Novel territory (state verdicts) requires post-design evidence collection before proposal stage.

**Next steps:**
- Shane to resolve non-blocking warnings path (1 vs 2)
- Gather state verdict use-case validation (3 sample precepts)
- Route Path 2 (if chosen) to philosophy team for guarantee refresh
- Once open gates resolved, design can move to proposal stage (#Y) per CONTRIBUTING.md

## Learnings

### 2026-04-11 — Event assert `when` exclusion stress-tested: CONFIRMED CORRECT
- Shane requested steel-man counterargument against the exclusion of `when` guards from `on <Event> assert` in issue #14.
- **Theoretical gap is real.** Domain scenarios exist where event arg validation applicability depends on entity field state (tier-dependent amount caps, config-dependent required fields, urgency-dependent minimums). The original rationale was imprecise: "transient input validation doesn't benefit from persistent-data guards" is slightly wrong — the APPLICABILITY of arg-only validation can depend on persistent data, even though the validation body itself remains arg-only.
- **Practical gap is minimal.** The `from any on Event when <Guard> -> reject "..."` pattern covers 100% of these cases. Confirmed via MCP compile + fire: `from any` expands correctly, rejection guards combine fields + args, first-match ordering is preserved, and the runtime produces the correct `Rejected` outcome.
- **Corpus evidence: 0/100+ event asserts across 24 samples need data-conditional applicability.** Every event assert is a universal structural constraint (non-empty, positive, range, modulus) that applies regardless of entity state.
- **Pipeline distinction confirmed via MCP inspect/fire:** Event assert failures produce `source.kind: "event-assertion"` (pre-routing), while `from any` rejection rows produce `source.kind: "transition-rejection"` (routed). The inspector distinguishes "malformed input" from "routed refusal." This semantic distinction is clean and valuable.
- **`from any` workaround cost:** C050/C051 diagnostic warnings when `from any` rejection rows expand into states where the event has no success path. This is a real paper cut but a tooling concern (compiler heuristic improvement), not a language surface issue.
- **Verdict: Exclusion CONFIRMED.** The theoretical gap is too rare, the workaround is complete, and the semantic clarity of "event asserts = context-free well-formedness" outweighs the marginal expressiveness benefit. No change to issue #14.
- **Precision correction for the rationale:** The exclusion rationale should read: "Event arg validation applicability rarely depends on entity field state, and the `from any on Event when ... -> reject` pattern covers those cases without cracking the event assert's clean scope contract." Not the slightly-wrong "transient input validation doesn't benefit from persistent-data guards."

### 2026-04-11 — Conditional asserts (`when` on `assert`) analyzed and rejected → REVERSED by symmetry analysis
- Shane asked whether `in <State> assert <Expr> when <Condition> because "..."` belongs in issue #14, as a separate proposal, or should be rejected.
- **Initial verdict: REJECT.** Six independent arguments against (expressiveness gap absent, double-filter confusion, zero demand, Principle #5 asymmetry correct, reading order degradation, semantic purity).
- Decision note filed at `.squad/decisions/inbox/frank-conditional-assert-rejection.md`.
- **REVERSED** by the constraint scoping symmetry analysis (same date). The deeper analysis reframed the asymmetry: it's not between `invariant` and `assert` — it's between **data-truth** and **event-truth** declarations. All data-truth forms (`invariant`, `in/to/from assert`, `in edit`) should accept `when`. Only event-truth (`on <Event> assert`) is excluded — scope category mismatch (transient input vs persistent state).
- Shane approved the scope expansion. Issue #14 updated to include conditional state asserts as the third declared form.

### 2026-04-11 — Issue #14 scope expansion: conditional state asserts added
- Shane approved expanding issue #14 based on the constraint scoping symmetry analysis at `.squad/decisions/inbox/frank-constraint-scoping-symmetry.md`.
- **Surgical additions to issue #14 body:** (1) Summary expanded with third declared form and two-axis model; (2) New "Conditional state asserts" syntax subsection with `in`, `to`, `from` variants; (3) Semantic rule added: `on <Event> assert` does NOT get `when` — principled exception; (4) New locked decision #5: `when` on all data-truth declarations, NOT on event-truth; (5) Implementation scope expanded with parser/type-checker/runtime/LS/grammar/MCP items for conditional asserts; (6) New acceptance criteria subsection for conditional state asserts; (7) Before/after example showing De Morgan's workaround vs `when` form.
- **Tool limitation:** GitHub MCP tools unavailable in Precept Author mode. Additions composed and presented for manual application.
- Decision note filed at `.squad/decisions/inbox/frank-issue14-scope-expansion.md`.
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

### 2026-04-11 — Event assert `when` exclusion overturned: arg-only guards SHOULD be added
- Shane challenged the exclusion of `when` from `on <Event> assert` in issue #14 on two fronts: (1) whether `from any on Event when Guard -> reject` is a workaround or the preferred form, and (2) whether `when` guards referencing event args (not fields) on event asserts should be supported.
- **Challenge 1 verdict: CONFIRMED.** `from any on Event when Guard -> reject` is the PREFERRED form, not a workaround. When event arg validation depends on entity field state, it IS contextual routing (cross-scope), and transition rows are the right mechanism. Distinguished from the De Morgan's case: De Morgan's encoded conditional invariants through unconditional invariants (wrong mechanism for the intent); `from any` encodes contextual routing through contextual routing (right mechanism). MCP fire verified: `source.kind: "event-assertion"` vs `source.kind: "transition-rejection"` — distinct inspector categories, correct semantic separation.
- **Challenge 2 verdict: REVERSE POSITION.** Arg-only `when` guards on event asserts SHOULD be added to issue #14. Three prior exclusion arguments don't survive re-examination:
  1. "Scope mismatch" → FALSE when guard references args. Both guard and body are arg-only — no cross-boundary mixing.
  2. "No sample evidence" → Shane is right that the corpus is arbitrary. The principled question: does the mechanism serve a real domain pattern? YES — multi-mode events (draft vs final, internal vs external) where validation requirements differ by the event's own arguments.
  3. "Implication pattern is natural for same-scope" → WRONG. Same De Morgan's mental gymnastics as the invariant case. Readability benefit of `when` is scope-independent.
- **Scope rule:** `when` inherits the scope of its parent declaration. Data-truth declarations: field-scoped guards. Event-truth declarations: arg-scoped guards. One rule, no exceptions.
- **Two-axis model completed.** With this addition, every cell in the scope × condition matrix is filled. No exclusion, no principled exception needed.
- **Two-mechanism model for conditional event validation:** (1) Same-scope (arg guard + arg assertion) → `on E assert X when G`. (2) Cross-scope (field guard + arg routing) → `from any on E when G -> reject`. Each concern maps to exactly one mechanism.
- **Self-correction:** I was inconsistent — I accepted "functionally equivalent to implication" as a reason to exclude `when` from event asserts while rejecting that same argument for invariants where `when` was added. The readability, inspectability, and intent-clarity benefits are scope-independent.
- Decision note filed at `.squad/decisions/inbox/frank-event-assert-when-reversal.md`. Awaiting Shane sign-off.

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

### 2026-04-11 — Verdict modifiers research: full design space analysis
- Shane requested comprehensive analysis of baking verdict categories (success/emerald, warning/amber, error/rose) into the Precept DSL as language-level modifiers.
- **Produced comprehensive research** at `research/language/expressiveness/verdict-modifiers.md` covering:
  - 5 application surfaces analyzed (states, events, fields, rules, combinations)
  - 8 comparable systems surveyed (ESLint, FluentValidation, Kubernetes, BPMN, Cedar/OPA, XState, FEEL/DMN, TLA+/Alloy)
  - Compile-time provability assessed per surface (event verdict = strong; state/field = weak)
  - Philosophy alignment checked against all 12 principles (strong fit, soft trade-off on compile-time-first acceptable)
  - Interaction with existing diagnostics and TransitionOutcome enum modeled
  - Tiered recommendations: Tier 1 event verdict (propose), Tier 2 rule/state verdicts (explore), Tier 3 field/entity verdicts (defer)
- **Key insight:** Verdict modifiers are intent declarations + tooling directives, not structural constraints. Similar precedent to `nullable` and `default` (already in DSL).
- **Strongest candidate:** Event verdict — high compile-time provability (verify outcome shapes match declared intent), high tooling impact (diagram colors, preview, MCP, completions), clear semantics.
- **Decision note filed** to `.squad/decisions/inbox/frank-verdict-modifier-research.md` with three-tier recommendation and decision points for Shane.
- **Open question:** Should rule severity be DSL-native (declaration-level) or host-configured (external)? Architectural decision needed before Tier 2 proceeds.
### 2026-04-22 — Null-Reduction + Field Access Modes Proposal Recreated
- **Task:** Recreated `research/language/expressiveness/null-reduction-proposal.md` from scratch as a clean canonical proposal covering both null vocabulary reduction and per-state field access modes.
- **Scope:** Two coupled features in one proposal: (1) `optional` replaces `nullable`, `is set`/`is not set` presence operators, `clear` action keyword, `null` removed from expression language; (2) `omit`/`read`/`write` verb triple, D3 per-pair baseline, 7 composition rules, `write` replaces `edit`.
- **Quality bar:** Matched Issue #17 (computed fields) and Issue #16 (built-in functions) proposal depth — every locked decision has full 4-point rationale (why, alternatives rejected, precedent, tradeoff accepted).
- **Key learning:** When Shane says "just keep rewriting the proposal in the md file," the proposal is a CLEAN document reflecting current design state — no revision history, no appendices, no "updated on" notes. The proposal is a living snapshot, not a changelog.
- **Structural insight:** Coupling null vocabulary and field access modes in one proposal is correct because `optional` needs `is set`/`is not set`, `omit` needs `clear` semantics, and `write` replaces `edit` for verb-triple consistency. Shipping one without the other leaves the language in an inconsistent intermediate state.

### 2026-04-11 — Issue #14 design review filed
- Conducted the full design/architecture review for Issue #14 (`when <guard>` on declarations) on 2026-04-11. Filed at `.squad/decisions/inbox/frank-issue14-design-review.md`; merged into `.squad/decisions.md`.
- **Verdict: APPROVED for Forms 1–3.** Form 4 (`in State when guard edit`) deferred to a follow-on issue — concept is sound, implementation requires per-call guard evaluation rather than static dictionary.
- B3 (research base absent) cleared: both cited research files confirmed present at `research/language/expressiveness/conditional-logic-strategy.md` and `research/language/references/conditional-invariant-survey.md`. Issue body had wrong `docs/` path prefix — corrected in the same session.
- All six locked design decisions upheld as architecturally sound. No philosophy gaps found. Zero grammar changes required.
