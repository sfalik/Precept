# Decision: README Image Link Fixes

**Date:** 2026-04-07  
**Owner:** J. Peterman (Brand/DevRel)  
**Status:** Completed

## Problem
README.md contained two broken image references that used incorrect relative paths:
- `brand/readme-hero.svg` 
- `brand/readme-hero-dsl.png`

These links pointed to `./brand/` but the actual assets are in `./design/brand/`.

## Solution
Updated both image references in README.md to use the correct path prefix:
- `brand/readme-hero.svg` → `design/brand/readme-hero.svg`
- `brand/readme-hero-dsl.png` → `design/brand/readme-hero-dsl.png`

## Verification
- ✅ Files verified to exist at corrected paths
- ✅ No other image references found in README
- ✅ Documentation context (hero example, contract display) remains accurate

## Impact
- Fixes broken hero example and contract diagram display in the README Quick Example section
- No behavioral changes—purely corrects link resolution for public-facing documentation

## Notes
The README's narrative around the hero example remains valid: it correctly notes that GitHub cannot render the styled DSL treatment, so the README displays the rendered contract (`readme-hero-dsl.png`) alongside copyable DSL source code. The path fix enables both assets to load correctly in GitHub's markdown renderer.

---

# Decision: README hero PNG fallback

- **Context:** GitHub does not render the styled inline HTML contract block in `README.md` as intended.
- **Decision:** Use `brand/readme-hero-dsl.png` as the GitHub-facing contract sample and keep a collapsed plain-text version immediately below for copyability.
- **Why:** The PNG preserves the intended branded syntax presentation on GitHub, while the collapsed source keeps the sample useful to humans and AI agents without turning the section back into a long raw block.
- **Files:** `README.md`, `brand/readme-hero-dsl.png`, `brand/readme-hero-dsl.precept`

---

# Decision: README Hero DSL PNG Rendering

**Author:** Elaine (UX)  
**Date:** 2025-07-21  
**Status:** Proposed  
**Scope:** brand/readme-hero-dsl.png

## Context

The README hero DSL snippet exists as an HTML file (`brand/readme-hero-dsl.html`) with syntax highlighting, and as an SVG state diagram (`brand/readme-hero.svg`). GitHub renders SVG but does not render arbitrary HTML. A PNG rendition of the syntax-highlighted code block is needed for contexts where the HTML source cannot be embedded directly — GitHub README `<img>` tags, social previews, and external documentation.

## Decision

- Render `brand/readme-hero-dsl.html` to `brand/readme-hero-dsl.png` using a headless Chromium screenshot at **2× device pixel ratio** for retina clarity.
- Output: **1268×942 px** (displays at 634×471 effective size) — tight crop of the `<pre>` code block, transparent background.
- The HTML source file remains the **editable source of truth**; the PNG is a derived asset that should be regenerated whenever the HTML changes.
- No fonts are embedded — the PNG captures the rendered output from Cascadia Code / Consolas fallback chain as available on the build machine. For cross-platform consistency, regenerate on a machine with Cascadia Code installed.

## Rationale

- PNG over SVG-from-HTML: GitHub `<img>` tags render PNGs reliably; converting syntax-highlighted HTML to SVG would require manual glyph work. The existing SVG is the state diagram — a different asset.
- 2× scale: GitHub displays images on retina screens. 1× screenshots appear blurry. 2× provides crisp text at reasonable file size (~137 KB).
- Transparent background: allows the PNG to sit on any surface background without a visible matte, matching the body `transparent` in the source HTML.

## Regeneration

If the hero snippet changes, re-render with:

```bash
# One-shot: install puppeteer, screenshot, remove
npm install --no-save puppeteer
node -e "<screenshot script>"  # see commit for full script
npm uninstall puppeteer && rm package.json package-lock.json && rm -rf node_modules
```

Future improvement: automate this as a build script or CI step.

---

## Decision

Treat `docs/HowWeGotHere.md` as a retrospective historical narrative, not as a live trunk-consolidation memo.

## Why

- Shane asked to remove the branch-history section as irrelevant.
- The "worth preserving" material read like an active recommendation set instead of a record of what endured.
- The unresolved/recommendation sections kept pulling the document back into pending-decision framing.

## Applied To

- `docs/HowWeGotHere.md`

---

# Steinbrenner Final Point Decision

- Date: 2026-04-05
- Branch: `feature/language-redesign`
- Decision: Treat the outstanding `docs\HowWeGotHere.md` addition as a coherent single final-point commit on the current branch.

## Why

- The working tree had one substantive product-facing change: a historical/consolidation document that explains how the branch got here and what remains unresolved before trunk return.
- That artifact is self-contained and does not need to be split from adjacent implementation work because there is no adjacent implementation work left unstaged.
- Freezing it in one commit gives the team an auditable reference point for any later trunk-curation exercise.

## Outcome

- Commit the document together with PM bookkeeping updates.
- Use the resulting SHA as the current branch's final planning reference until new work is intentionally started.

---

# Decision: Issue #22 Design Fidelity Directive

**Date:** 2026-04-08
**By:** Shane (user directive)

When implementing issue #22, if anything the team is going to implement strays from the design docs or seems ambiguous, they must stop and ask rather than guess. Design understanding is a prerequisite before coding starts.

---

# Decision: Issue #22 — Data-Only Precepts Design Q&A (12 Decisions)

**Date:** 2026-04-08
**By:** Shane (owner) via Squad Q&A
**Issue:** #22 — Data-only precepts

#### Decision 1: `all` keyword — field name collision
No special handling needed. Adding `all` to `PreceptToken` with `[TokenSymbol("all")]` and `requireDelimiters: true` automatically reserves it. Using `all` as a field/state/precept name is a hard parse error by architecture.

#### Decision 2: Root `edit` model representation
Option A — make `State` nullable on the existing `PreceptEditBlock` record. Root-level edits have `State = null`. No new model type needed.

#### Decision 3: Root `edit` parsing strategy
Parser accepts both root `edit` and `in State edit` forms as valid syntax. The type checker enforces the constraint: root `edit` + states declared = compile error (C55) with migration guidance. Avoids backtracking in the Superpower parser.

#### Decision 4: Events-in-stateless diagnostic code
Reuse C49 (orphaned event). Events in stateless precepts trigger C49 — structurally they are orphaned (no state routing surface). No new diagnostic code needed.

#### Decision 5: Root `edit` + states = compile error diagnostic
New code C55, severity Error. Message: "Root-level `edit` is not valid when states are declared. Use `in any edit all` or `in <State> edit <Fields>` instead."

#### Decision 6: Inspect for stateless — include events
Include events in the Inspect result, each with outcome `Undefined`. Uses existing `TransitionOutcome.Undefined` — no new outcome needed.

#### Decision 7: CreateInstance overloads for stateless
Only the 1-arg `CreateInstance(data)` overload works for stateless precepts. The 2-arg `CreateInstance(state, data)` overload throws `ArgumentException` for any call on a stateless precept, even with null state.

#### Decision 8: C50 severity upgrade — sample impact
Confirmed safe. All 21 existing samples compile clean with zero C50 diagnostics. Upgrading from hint to warning surfaces no new warnings in the sample corpus.

#### Decision 9: C29 invariant pre-flight for stateless
C29 fires at compile time for stateless precepts, same as stateful. Invariants on default values are checked regardless of whether the precept has states.

#### Decision 10: Event warnings — one per event
One C49 warning per event. A stateless precept with 3 events produces 3 separate warnings, consistent with existing C49 behavior.

#### Decision 11: Sample file names
Use `customer-profile.precept`, `fee-schedule.precept`, `payment-method.precept` as placeholder samples. Shane plans a major sample overhaul later.

#### Decision 12: Future root-level pattern
`edit` is the only root-level declaration planned for stateless. No need to design a general extensible root-level pattern. Keep it as a single special case.

---

# Decision: Slice 7 Test Coverage — Known Gaps (Deferred)

**Date:** 2026-04-08
**By:** Soup Nazi (Tester)

Three coverage gaps identified during Slice 7 test writing and explicitly deferred as non-blocking:

1. No direct unit test for `GetEditableFieldNames(null)` internal API — covered indirectly via Inspect/Update paths.
2. No multi-event stateless precept test — only single-event C49 path covered. Multiple C49 warnings (one per event) path is untested.
3. `PreceptInstance.WorkflowName` mismatch on stateless Inspect not covered.

These are known gaps, recorded for future test pass. Not blocking Slice 7 merge.

---

# Readability Review: combined-design-v2.md (2026-07-17)

**Reviewer:** Elaine (UX Designer)
**Doc:** `docs/working/combined-design-v2.md`
**Verdict:** APPROVED-WITH-CONCERNS

## Top 3 Findings

1. **Parser section needs shape specificity.** The section explains the parser's *philosophy* (source-faithful, recovery-aware) but doesn't give an implementer enough to know what SyntaxTree nodes to define. Missing: error recovery node shape, concrete node inventory or shape sketch, and explicit contract for how malformed input is represented. A parser design doc author would need to invent these from scratch.

2. **Missing navigation guide.** A 486-line doc serving two audiences (human implementers and AI agents) needs a "How to read this document" paragraph after the status block. Three sentences: what §1–§3 cover (commitments and pipeline overview), what §4–§8 cover (per-stage contracts), what §9–§12 cover (runtime and integration). This is the single highest-ROI addition for both audiences.

3. **"How it serves the guarantee" paragraphs become formulaic.** Useful for Lexer through Graph Analyzer. By Proof Engine and Lowering, the pattern is predictable and the content is restating the opening sentence. Recommendation: fold the guarantee connection into the stage's opening paragraph for §8–§10 and drop the separate labeled paragraph.

## Genre Assessment

The rewrite succeeds. §1 opens with a problem statement and architectural commitment, not an inventory. Per-stage sections lead with design decisions. The philosophy-first framing is consistent throughout. This is a design document, not a reference manual.

## Decision

This doc is ready to serve as the architectural foundation for per-stage design docs (starting with the parser). The concerns above are improvements, not blockers — the parser concern is the most urgent because that's the immediate next use case.

---

# Design Review: combined-design-v2.md — Soundness, Completeness, Innovation

**Reviewer:** Frank (Lead Architect)
**Date:** 2026-06-03
**Document:** `docs/working/combined-design-v2.md`
**Context:** Only the Lexer is implemented. All other pipeline stages are stubs.

---

## VERDICT: APPROVED-WITH-CONCERNS

The document is architecturally sound and well-structured. It reads as a unified design explanation rooted in philosophy, not a defense of two separate systems. The pipeline is coherent, the artifact boundaries are clean, and the lowered executable model is concrete enough to implement against. The concerns below are real design gaps that will cost us if we hit them mid-implementation rather than addressing them now.

---

## Soundness Issues

1. **The proof strategy set is closed but its coverage boundary is unstated.** The doc lists four strategies (literal, modifier, guard-in-path, flow narrowing) and says "any obligation outside this set is unresolvable." But it never states what percentage of real-world proof obligations these four strategies can discharge. If most `ProofRequirement` instances in practice require cross-field reasoning (e.g., `ApprovedAmount <= RequestedAmount`), then the four strategies are a beautiful design that rejects most real programs. The doc should include a coverage analysis against the sample corpus — even an informal one — so implementers know whether the strategy set is right-sized or whether a fifth strategy (e.g., relational pair narrowing) is needed before v1.

2. **`Restore` bypasses access-mode but evaluates constraints — the interaction with computed fields is unspecified.** If persisted data includes stale computed-field values, does Restore recompute before constraint evaluation? The recomputation index is listed as a Restore input, but the evaluation order (recompute → validate vs. validate → recompute) is not specified. Getting this wrong means Restore either rejects valid persisted data or accepts invalid computed values.

3. **The `Create` without initial event path evaluates `always` + `in <initial>` — but default values may not satisfy `in <initial>` constraints.** The doc doesn't specify whether this is a compile-time guarantee (the proof engine should catch it) or a runtime domain outcome. The static-reasoning research (C3) says this is a known check, but the combined design doesn't thread it through the proof/fault chain. An author who writes `field X as number default 0` and `in Draft ensure X > 5` gets no compile-time warning in the current design — only a runtime `EventConstraintsFailed` on create. That violates the prevention promise.

4. **`ConstraintActivation` discriminant is described but not typed.** The doc says it "distinguishes whether a constraint binds to the current state, the source state, or the target state." But it doesn't specify whether this is an enum, a DU, or a tag on the descriptor. Given the catalog-driven architecture, this should be cataloged — it's language-surface knowledge that consumers need, not an implementation detail.

---

## Completeness Gaps

1. **No error recovery strategy for the parser.** The doc says `SyntaxTree` preserves "recovery shape for broken programs" and mentions "missing-node representation," but there is no recovery algorithm specified. Panic-mode? Synchronization tokens? The parser recovery strategy directly affects LS quality — a bad recovery model means completions and diagnostics degrade on every keystroke. This is a design decision, not an implementation detail, and it should be locked before the parser is built.

2. **No incremental compilation model.** The doc treats the pipeline as a single-shot transformation: source → tokens → tree → model → graph → proof → CompilationResult. But the language server needs incremental re-analysis on every keystroke. The doc should specify the invalidation boundary — does a keystroke re-lex the whole file? Re-parse? Re-typecheck? For a single-file DSL this may be "just re-run everything" and that's fine — but say so explicitly, with a size-ceiling argument for why that's acceptable (the 64KB source limit helps here).

3. **No serialization contract for `Version`.** The doc specifies `Restore` as the reconstitution path, but never specifies what the caller provides. What is the serialization shape of a `Version`? Is it `(stateName, fieldValues)`? `(stateDescriptor, slotArray)`? The host application needs a defined contract for what to persist and what to hand back to `Restore`. Without it, every host will invent its own serialization and we'll get impedance mismatches.

4. **No definition versioning or migration story.** When a `.precept` file changes (field added, state renamed, constraint tightened), what happens to persisted `Version` instances compiled against the old definition? `Restore` will reject them if they don't satisfy the new constraints. The doc should at least name this as a known gap and specify whether migration is in-scope or explicitly deferred.

5. **No observability hooks.** The doc specifies structured outcomes and inspections, but no tracing, logging, or metric emission points. For a production runtime, host applications need to observe: which events fired, which constraints failed, how long evaluation took, which proof strategies were used. These hooks shape the evaluator's internal architecture — bolting them on later means refactoring the evaluator.

---

## Innovation Opportunities

1. **The proof engine should guarantee initial-state satisfiability at compile time.** The research base (static-reasoning-expansion.md, C3/C4/C5) already describes per-field interval analysis. The combined design should commit to a concrete compile-time guarantee: *if default field values and initial-state constraints are both statically known, the proof engine verifies satisfiability and emits a diagnostic if no valid initial configuration exists.* This is unique among DSL runtimes — no validator, state machine library, or rules engine provides this. It's the proof engine's signature contribution and it's achievable with the bounded strategy set already designed.

2. **Precompute a "constraint influence map" during lowering for AI-native inspection.** Currently the inspection API tells you *what* constraints are active and *whether* they passed. It doesn't tell you *which fields drive which constraints* — the dependency graph exists in the `TypedModel` but is not lowered into an inspectable form. If lowering also produces a `ConstraintInfluenceMap` (constraint → contributing fields, with expression-text excerpts), then an AI agent can answer "why did this constraint fail?" and "which field change would fix it?" without reverse-engineering expressions. This is a structural differentiator for the MCP surface.

3. **The executable model should be a compiled decision table, not a tree walk.** The doc says lowering produces "lowered expression nodes and action plans" but doesn't specify the execution model. For Precept's small, closed expression language, the optimal model is a flat evaluation plan — precomputed slot references, operation opcodes, and result slots — not a recursive tree interpreter. Think of it as a register-based bytecode where "registers" are field slots. This makes evaluation predictable-time, cache-friendly, and trivially serializable for inspection. The doc should commit to "flat evaluation plan" as the executable model shape and explicitly reject tree-walking.

4. **Emit a machine-readable "contract digest" alongside `CompilationResult`.** A deterministic hash of the compiled definition's semantic content (fields, types, constraints, states, transitions — excluding whitespace and comments) would let host applications detect definition changes without diffing source text. Pair it with a structural diff API (`ContractDiff(old, new)` → added/removed/changed fields, states, constraints) and you have the foundation for the migration story (gap #4 above) and a production deployment safety net.

5. **The constraint evaluation matrix should surface "why not" explanations as structured data.** When `Fire` returns `Rejected` or `EventConstraintsFailed`, the outcome carries `ConstraintViolation` objects. But the doc doesn't specify whether violations carry *explanation depth* — just the failing expression text, or also the evaluated field values, the guard that scoped the constraint, and the specific sub-expression that failed. For AI legibility, violations should carry structured explanation: `{ constraint, expression, evaluatedValues: { field: value }, guardContext?, failingSubExpression? }`. This is cheap to compute during evaluation and transforms MCP from "it failed" to "it failed because X was 3 and the constraint requires X > 5."

---

## Right-Sizing Issues

1. **The `SyntaxTree` vs `TypedModel` anti-mirroring rules are over-specified for a doc at this level.** Four numbered rules about what `TypedModel` must not do, plus a seven-item "required inventory" — this is component-level design specification embedded in an architecture document. The architectural decision (they are separate artifacts with separate jobs) is correct and should stay. The implementation contract should move to a parser/type-checker-specific design doc that the implementer reads when building those stages.

2. **The five constraint-plan families and four activation indexes are correctly designed but could be simplified in the implementation.** The `from` and `to` families only activate during `Fire`. The `on` family only activates during `Fire`. The `in` family activates during `Update`, `Create`-without-event, and `Restore`. The `always` family activates everywhere. This means the evaluator really has two modes: "fire mode" (all five families) and "edit mode" (always + in). The doc could name these modes to simplify the mental model without losing the family distinction.

---

## Top 3 Recommended Changes Before This Doc Drives Per-Component Design

### 1. Add a proof coverage analysis against the sample corpus.

Run the four proof strategies against every `ProofRequirement` that would arise from the 20 sample files. Report how many obligations each strategy discharges and how many remain unresolvable. If coverage is below ~90%, design a fifth strategy before implementation begins. This is the highest-risk unknown in the document — the proof engine's value proposition depends on it.

### 2. Specify the parser error recovery strategy.

Lock one of: (a) panic-mode with synchronization at declaration keywords, (b) token-deletion/insertion with cost model, (c) "re-lex everything, re-parse everything" with the 64KB ceiling as the performance argument. The LS team cannot build completion/diagnostic features without knowing what the tree looks like on broken input.

### 3. Commit to a flat evaluation plan as the executable model.

Replace "lowered expression nodes and action plans" with a concrete specification: slot-addressed evaluation plans with operation opcodes, field-slot references, literal constants, and result slots. This prevents the implementation from defaulting to a recursive AST interpreter — which would be correct but would sacrifice the performance and inspectability properties that make Precept's runtime distinctive.

---

*This review is direct because the timing demands it. Addressing these three items now — before the parser, type checker, and evaluator are built — is nearly free. Addressing them after implementation begins is expensive. The architecture is sound. These are the gaps that would bite us.*

---

# Decision: combined-design-v2 promoted to canonical location

**Author:** Frank (Lead/Architect)
**Date:** 2025-07-23

## Decision

`docs/working/combined-design-v2.md` promoted to `docs/compiler-and-runtime-design.md`, replacing the prior short-form doc (203 lines).

## What changed

- **Type strategy rationale absorbed as prose.** The short doc's Type Strategy table contained both C# signatures and design reasoning. The reasoning — why each type kind was chosen, the immutability contract, the LS full-recompile model, the same-process integration pattern — was extracted and written as four paragraphs of design rationale in a new §12 "Type and immutability strategy." Two Innovations callout bullets added.
- **C# field-level signatures removed.** Shane decided these do not belong in a design doc — they drift from code. Code is the source of truth for signatures; the design doc captures the *reasoning* behind the type kind choices.
- **Short doc retired.** The promoted v2 (now ~710 lines, 15 sections + appendix) supersedes all content from the short doc. No design decisions were lost.
- **Working copy deleted.** `docs/working/combined-design-v2.md` removed after promotion.

## Rationale

The combined-design-v2 doc had accumulated all review feedback, innovations callouts, and section expansions across multiple review rounds. The short doc's only unique content was the type strategy reasoning, which v2 now carries. Keeping both docs would create a drift risk with no benefit.

---

# Decision: Combined Design v2 Comprehensive Revision Pass

**By:** Frank
**Date:** 2026-07-17
**Status:** Applied

## Summary

Applied all team review feedback (Frank design review, George technical accuracy, Elaine readability) to `docs/working/combined-design-v2.md` in a single revision pass. Added Precept Innovations callouts to every major section. Added two new sections: §12 TextMate Grammar Generation and §13 MCP Integration.

## What Changed

### Review feedback applied (all three reviewers)
- Navigation guide ("How to read this document") after status block
- Parser: error recovery shape, node inventory, catalog-to-grammar mapping, anti-Roslyn guidance, ActionKind dual-use, parser/TypeChecker contract boundary
- TypeChecker: anti-pattern for per-construct check methods
- Proof engine: coverage boundary, flow narrowing clarification, initial-state satisfiability
- Compilation snapshot: no-incremental-compilation model, contract digest hash, definition versioning gap
- Lowering: fixed "catalogs not re-read" claim, descriptor shapes, flat evaluation plan, anti-pattern warnings, ConstraintActivation cataloging, Version serialization contract
- Runtime: Restore recomputation order, structured "why not" violations

### New content
- **Precept Innovations callouts** in every major section (§2–§14), 2–4 bullets each
- **§12 TextMate grammar generation** — catalog contributions table, anti-pattern, zero-drift guarantee
- **§13 MCP integration** — tool inventory, thin-wrapper principle, AI-first design, catalog-derived vocabulary

### Structural changes
- Former §12 (LS integration) renumbered to §14
- Doc grew from 486 to 694 lines
- Formulaic guarantee paragraphs folded into stage openings for §8–§10

## Decisions Locked
- Parser error recovery: construct-level panic mode with `MissingNode` + `SkippedTokens`
- Expression evaluation: flat slot-addressed evaluation plans, tree-walk explicitly rejected
- Incremental compilation: "re-run everything" is the intended model (64KB ceiling)
- Definition versioning: known gap, deferred beyond v1
- `ConstraintActivation`: should be cataloged (language-surface knowledge)

---

# Decision: Graph topology crosses the lowering boundary as runtime-native shapes

**Author:** Frank (Lead/Architect)
**Date:** 2025-07-17
**Status:** Decided
**Scope:** `docs/compiler-and-runtime-design.md` §3, §10

## Decision

Graph-derived knowledge crosses the lowering boundary into the `Precept` runtime model as runtime-native shapes. The prior categorical claim — "graph topology does not cross" — is replaced with a precise principle: *artifact types* don't cross; *analysis-derived knowledge* crosses in lowered form.

## What was wrong

The passage in §3 listed "graph topology as artifacts" among things that do not cross the lowering boundary. This conflated two distinct concepts:

1. **Artifact type references** — runtime types must not depend on `GraphResult`, `SyntaxTree`, `TypedModel`, etc. This is correct and unchanged.
2. **Analysis-derived knowledge** — graph topology knowledge (transition routing, state inventory, reachability, event availability) is essential for the structural queries surface, MCP tools, and AI navigation. This must cross.

The prior framing implied an architectural prohibition on graph knowledge at runtime, when the actual prohibition is only on artifact type coupling.

## New principle

> Artifacts don't cross; analysis-derived knowledge crosses in runtime-native shapes.

The `GraphResult` artifact (with its compiler-stage types) does not cross — runtime types hold no references to it. But the knowledge it contains is lowered into runtime-native shapes:

- **Transition dispatch index** — state × event → target state
- **State descriptor table** — name, metadata, terminal flag, available events
- **Event availability index** — valid events per state
- **Reachability index** — reachable states from a given state
- **Pathfinding residue** — goal-directed navigation topology (the graph analog of `ConstraintInfluenceMap`)

## Rationale

1. **Structural queries surface requires it.** The runtime pipeline diagram already includes "Structural queries" as a runtime surface. That surface cannot answer "what states exist?" or "what events are available from here?" without lowered graph knowledge.

2. **MCP and AI navigation require it.** `precept_inspect` and `precept_fire` already imply structural awareness. AI agents navigating a state machine need reachability and pathfinding — not just event dispatch.

3. **The guarantee is inspectable only if structure is queryable.** Precept's promise includes "you can preview every possible action." That requires knowing the state machine's topology at runtime, not just executing transitions.

4. **Consistency with existing lowered artifacts.** `ConstraintInfluenceMap` already crosses as a lowered artifact for causal reasoning. Reachability and pathfinding are the structural analog — causal reasoning over lifecycle topology.

## What genuinely does not cross (unchanged)

- `SyntaxTree` — no runtime consumer
- `TokenStream` — no runtime consumer
- Parser recovery shape — authoring artifact only
- `ProofModel` graph structure — runtime needs proof outcomes, not the proof obligation graph

These don't cross because no runtime operation needs them — the prohibition is consumer-driven, not categorical.

---

# Design Evaluation: Per-Field `readonly` Modifier as Access Default Inversion

**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2025-07-14
**Requested by:** Shane
**Verdict:** **Reject**

---

## Proposal Summary

Invert D3: make `write` the universal default for (field, state) pairs. Add a `readonly` modifier on field declarations to permanently lock fields from ever being written in any state. Eliminate root-level `write` declarations entirely.

---

## Question 1: Does inverting D3 weaken the conservative guarantee?

**Yes. Fundamentally.**

D3 as specified (§2.2 Access Mode, composition rule 1) states: "D3 is the universal per-pair baseline — undeclared (field, state) pairs default to `read`." The design principle behind this is explicit: "Authors declare only exceptions to readonly — `write` opens a field for editing in that state."

This is a **closed-world access model**. Nothing is writable unless explicitly opened. The omission failure mode is safe: if an author forgets to declare a `write`, the field is locked in that state. The author must take a deliberate action — writing the `write` keyword — to open the attack surface.

The proposal inverts this to an **open-world access model**. Everything is writable unless explicitly restricted. The omission failure mode is unsafe: if an author forgets to mark a field `readonly`, it is exposed in every state to direct mutation via `Update`.

This is the firewall-rule principle. Good security defaults to DENY; you add ALLOW exceptions. D3 defaults to DENY (read-only) and authors add ALLOW exceptions (`write`). The proposal defaults to ALLOW (writable) and authors add DENY exceptions (`readonly`). In a **governance** language — one whose entire identity is built on "invalid configurations are structurally impossible" (Principle 1: Prevention, not detection) — the conservative default is non-negotiable.

### Corpus evidence

The sample set confirms that the conservative default reflects real domain proportions:

- **Stateful precepts with zero write declarations:** `hiring-pipeline`, `loan-application` (except one guarded write), `apartment-rental-application`, `restaurant-waitlist`, `library-hold-request`, `travel-reimbursement`, `warranty-repair-request`. These precepts rely entirely on event-driven mutation. The D3 default silently protects all fields from direct editing. Under the proposal, all those fields would be writable by default — an enormous, invisible expansion of the attack surface.

- **Stateful precepts with 1–2 write declarations:** `crosswalk-signal` (1), `clinic-appointment-scheduling` (1), `building-access-badge-request` (1), `insurance-claim` (2), `maintenance-work-order` (1), `refund-request` (1), `subscription-cancellation-retention` (1), `event-registration` (2), `it-helpdesk-ticket` (1), `utility-outage-report` (1), `vehicle-service-appointment` (1). The typical pattern is opening 1–3 fields in 1–2 states. The remaining (field, state) pairs — the overwhelming majority — stay protected by D3.

- **Stateless precepts:** `fee-schedule` (3 of 5 writable), `payment-method` (2 of 6 writable), `computed-tax-net` (2 of 4 writable), `invoice-line-item` (4 of N writable). Even in stateless precepts, the typical pattern is that some fields are intentionally locked. `customer-profile` is the only sample using `write all`.

The verbosity cost of the current model is 1–2 lines per precept. The safety cost of the proposed model is an invisible, unbounded expansion of the mutation surface whenever an author omits a `readonly` marker.

### Principle citations

- **Principle 1 (Prevention, not detection):** The proposal turns field-level access control from structurally prevented to structurally permitted. An author who omits `readonly` on a field that should be locked has created a governance gap. Under D3, the same omission creates no gap.
- **Principle 4 (Full inspectability):** Auditability is stronger when the declared surface is the exception set (small, explicit) rather than the restriction set (requiring mental subtraction from a universal default). "What can a user directly edit here?" is answered by scanning for `write` keywords under D3. Under the proposal, the answer is "everything, minus what's marked `readonly`" — which requires reading every field declaration to check for the absence of a modifier.

---

## Question 2: Does `readonly` on a field cleanly complement or conflict with computed fields?

**It creates a semantic inconsistency.**

Computed fields (`field Tax as number -> Subtotal * TaxRate`) are already implicitly readonly. The spec enforces this structurally — `ComputedFieldNotWritable` is a type-checker diagnostic (§3.8). A computed field's readonly nature arises from its derivation: it has an expression, so it cannot be directly assigned. This is not a modifier; it is a structural consequence of the field's kind.

Under the proposal, the access defaults would be:

| Field kind | Proposed default | Actual access |
|---|---|---|
| Stored field (no `readonly`) | write | write |
| Stored field (with `readonly`) | write → overridden to read | read |
| Computed field | write (in theory) | read (structurally) |

The computed field's access mode would be inconsistent with the declared default. A stored field and a computed field would have different effective defaults despite the language claiming "write is the default." The author would need to understand that computed fields are a hidden exception to the stated default — undermining Principle 4 (inspectability) and Principle 5 (keyword-anchored readability).

Under D3, the picture is consistent:

| Field kind | D3 default | Actual access |
|---|---|---|
| Stored field (no `write`) | read | read |
| Stored field (with `write`) | read → overridden to write | write |
| Computed field | read | read |

All fields default to read. Computed fields are naturally aligned with the default. Stored fields that need to be writable are explicitly opened. There is no inconsistency to explain.

Adding `readonly` as a modifier also creates a redundancy question: should `readonly` on a computed field be a warning (redundant modifier), an error (modifier conflicts with structural readonly), or silently accepted? Each answer has downsides. Under D3, the question never arises — there is no `readonly` keyword, and computed fields simply match the default.

---

## Question 3: Does "write default, restrict per state" change the auditability story?

**Yes. It weakens it materially.**

In a stateful precept under D3, the audit question "which fields can a user directly edit in state S?" is answered by reading the `in S write` declarations. If there are none, the answer is "nothing — all mutation happens through events." This is a **closed-world audit**: the write declarations ARE the complete answer.

Under the proposal, the same question is answered by: "every field, minus those marked `readonly` on the field declaration, minus those restricted by `in S read` or `in S omit` declarations." This is an **open-world audit** requiring cross-referencing the field declarations (for `readonly` markers), the state-scoped access declarations (for per-state restrictions), and computing the difference. The mental model is subtraction from a universal set rather than enumeration of an explicit set.

For a governance language — one where the point is to make the access contract **explicit and visible** — the open-world model is the wrong posture. The current model's strength is that the write declarations positively assert what is open. The proposed model requires the reader to infer what is open from what is not restricted.

This matters especially for AI consumers. Precept's Principle 3 (deterministic semantics) and Principle 5 (keyword-anchored readability) are designed partly for AI legibility. A closed-world access model is easier for AI agents to reason about: "find all `write` declarations" is a simple, complete query. "Find all fields, subtract `readonly` fields, subtract per-state restrictions" is a compositional query with a higher error surface.

---

## Additional Concerns

### The `readonly` keyword itself is misaligned

`readonly` is a **programming-language concept** from C#, Java, Rust, TypeScript. It carries connotations of compile-time immutability, final binding, memory-model guarantees. Precept's access model is about **editability** — which fields can the host application directly mutate via the `Update` operation. These are different concepts. A field that is `read` in a given state is not immutable — events can still `set` it during transitions. It is merely not directly editable by the external caller. Introducing `readonly` would import programming-language semantics into a domain-configuration language, violating the philosophy's positioning of Precept as a language for domain experts and business analysts, not software developers (§ Who authors a precept in philosophy.md).

### Root-level `write` elimination is a false economy

The proposal motivates itself partly by eliminating root-level `write` declarations. But the current model already makes these declarations do useful work:

- `write BaseFee, DiscountPercent, MinimumCharge` in `fee-schedule` — the `write` keyword positively documents the author's intent. Reading it, you know immediately which fields are editable. The comment above it ("Only pricing levers are editable; TaxRate and CurrencyCode are locked") is restating what the `write` declaration already says.
- `write all` in `customer-profile` — a deliberate, visible assertion that everything is open. Under the proposal, this becomes the invisible default, and the author's deliberate intent vanishes from the surface.

The `write` keyword carries semantic weight as a positive assertion. Replacing it with the absence of `readonly` loses that signal.

---

## Verdict: **Reject**

The proposal inverts Precept's conservative access posture from closed-world (safe by default, explicitly opened) to open-world (exposed by default, explicitly restricted). This:

1. **Weakens the omission failure mode** from safe (field locked) to unsafe (field exposed).
2. **Creates an access-default inconsistency** between stored and computed fields.
3. **Degrades auditability** from positive enumeration to negative subtraction.
4. **Imports programming-language semantics** (`readonly`) into a domain-configuration language.
5. **Eliminates the positive-assertion value** of `write` declarations for marginal verbosity savings (1–2 lines per precept).

D3 is philosophically correct, empirically well-calibrated to real domain proportions, and consistent with the governance identity. It should not be inverted.

### What would need to change for reconsideration

If the underlying concern is verbosity in stateless precepts that happen to have mostly-writable fields, there are narrower solutions that preserve D3:

- A `write all` shorthand already exists and handles the fully-open case.
- If a `write all except F1, F2` syntax were needed, it could be evaluated without inverting the default. The exception list would still be a positive declaration against a positively-declared baseline.

Neither of these requires abandoning the conservative default. The proposal conflates "reduce boilerplate" with "invert the safety model." Only the former is a real problem; the latter is the wrong solution.

---

# Decision Record: Research Grounding for compiler-and-runtime-design.md

**Author:** Frank (Lead/Architect)
**Date:** 2025-07-25

## What was reframed

Three section headings and their framing were changed from GP-compiler-negative to DSL-scale-positive:

| Old heading | New heading | Change |
|---|---|---|
| "Anti-Roslyn guidance" | "Right-sized parser patterns" | Replaced "don't do what Roslyn does" framing with "here's what works at DSL scale and why" — grounded in CEL, OPA, Dhall, Jsonnet, Pkl evidence |
| "Anti-pattern: per-construct check methods" | "Right-sized type checking: generic resolution passes" | Added CEL checker and OPA `ast/check.go` as surveyed precedent for single-pass catalog-driven type resolution |
| "Anti-pattern: serialized TypedModel" | "Lowering is restructuring, not renaming" | Added CEL `Program`, OPA rule indexes, and XState v5 as surveyed precedent for restructuring transformations in lowering |

The catalog-driven section (§2) still mentions Roslyn/GCC/TypeScript as a contrast point, but now frames them as "general-purpose compilers" and immediately pivots to what DSL-scale systems do instead — with CEL, OPA, and CUE as named examples.

## What research was used

All grounding draws from the 15-survey compiler corpus (`research/architecture/compiler/`) and the runtime evaluator survey (`research/architecture/runtime/`):

| Doc section | Surveys referenced | Systems cited |
|---|---|---|
| Catalog-driven design (§2) | compiler-pipeline-architecture-survey | CEL, OPA/Rego, CUE |
| Purpose-built (§2) | compiler-pipeline-architecture-survey, language-server-integration-survey | CEL, OPA/Rego, Dhall, Pkl, CUE |
| Parser patterns (§5) | compiler-pipeline-architecture-survey, language-server-integration-survey | CEL, OPA/Rego, Dhall, Jsonnet, Pkl |
| Error recovery (§5) | compiler-pipeline-architecture-survey | Roslyn (adapted pattern), OPA, Pkl |
| Type checking (§6) | compiler-pipeline-architecture-survey | CEL, OPA/Rego |
| Graph analysis (§7) | state-graph-analysis-survey | SPIN/Promela, Alloy, NuSMV/nuXmv, XState `@xstate/graph` |
| Proof engine (§8) | proof-engine-interval-arithmetic-survey | SPARK Ada/GNATprove, Dafny, Liquid Haskell, CBMC |
| CompilationResult (§9) | compiler-pipeline-architecture-survey | Roslyn, OPA, CEL, Dhall |
| Lowering / flat eval (§10) | runtime-evaluator-architecture-survey | CEL, OPA/Rego, Dhall, Pkl, XState v5 |
| Structured outcomes (§11) | runtime-evaluator-architecture-survey | CEL, OPA, Eiffel/DbC |
| Inspection (§11) | dry-run-preview-inspect-api-survey | Terraform, XState v5, OPA, Temporal |
| Incremental compilation (§12) | language-server-integration-survey | OPA/Regal, Dhall, Jsonnet, CEL |
| LS single-process (§12) | language-server-integration-survey | Regal/OPA, Dhall, Jsonnet, CUE |

## Gaps found

1. **Flat evaluation plans vs tree-walking.** The surveyed DSL-scale systems (CEL, OPA, Dhall, Pkl) all use tree-walk evaluation and succeed at their scale. Precept's choice of flat slot-addressed evaluation plans is a design decision for inspectability and determinism, not a pattern validated by DSL-scale precedent. The doc now explicitly flags this as a design decision rather than a researched conclusion.

2. **Proof engine bounded strategy set.** No surveyed DSL-scale system has a comparable bounded proof engine — the verified systems (SPARK, Dafny, Liquid Haskell) all use SMT solvers for general proof. Precept's four-strategy bounded approach is novel in this space. The doc now flags the tradeoff (no solver dependency, reduced coverage breadth) and anchors it in the verification survey evidence.

3. **Grammar generation from catalogs.** No surveyed system generates its TextMate grammar from the same metadata that drives parsing and type checking. This remains an ungrounded innovation claim — it is Precept-specific and has no external precedent to anchor.

## Gap fill pass

Six surveys were not consulted in the initial grounding. Each was read against the relevant doc sections. Changes:

1. **`state-machine-runtime-api-survey.md` → §11 runtime surface.** Three additions. (a) Fire section: XState's `can()` and `send()` void return cannot distinguish guard failure from undefined transition — Precept's `Unmatched` vs `Rejected`/`EventConstraintsFailed` is a structural differentiator, now explicitly anchored. (b) Update section: no surveyed state machine runtime provides direct field mutation outside the event/transition mechanism — Precept's `Update` operation is architecturally unique, now documented with evidence from XState, Temporal, SCXML, gen_statem, Akka, and Step Functions. (c) Inspection section: XState v5's pure transition functions (`transition()`, `getNextSnapshot()`, `getNextTransitions()`) are the closest precedent for Precept's inspection API, now cited alongside the existing Terraform/OPA/Temporal references.

2. **`compiler-result-to-runtime-survey.md` → §10 lowering.** Two additions. (a) Lowering boundary: CEL retains AST node IDs via `Interpretable.ID()`, Dhall discards all compile artifacts after decoding, Pkl merges compilation and evaluation into a single call — this spectrum now frames Precept's "selective transformation" design. (b) Restore section: XState v5's `createActor(machine, { snapshot })` is the closest precedent for state reconstitution from persistence, but trusts the persisted shape without constraint re-evaluation — Precept's validation-on-restore is now anchored as a deliberate divergence.

3. **`compilation-result-type-survey.md` → §12 immutability.** One addition. The summary table reveals immutability is not the DSL-scale consensus: OPA, Kotlin K2, Swift, Go, Dafny, and Boogie all mutate compilation state in place. Only CEL, Dhall, CUE, and Pkl produce immutable results. Precept's immutable `CompilationResult` is now framed as an LS-driven choice, not inherited consensus.

4. **`proof-attribution-witness-design-survey.md` → §8 proof engine.** Two additions. (a) Per-obligation disposition model: CBMC's `SUCCESS`/`FAILURE`/`UNKNOWN`, Frama-C/WP's `Valid`/`Unknown`/`Invalid`/`Timeout`, and Dafny's per-method statistics now ground Precept's per-obligation disposition granularity. SPARK's `Justified` disposition is noted as a precedented response if the proof coverage boundary reveals uncoverable obligations. (b) Structured violation shapes: Rust borrow checker's multi-span labeled diagnostic model and Infer's `bug_trace` now ground `ConstraintViolation`'s causal chain structure.

5. **`outcome-type-taxonomy-survey.md` → §11 runtime outcomes.** One addition. The structured outcomes paragraph now cites gRPC's `FAILED_PRECONDITION`/`INVALID_ARGUMENT`/`INTERNAL` tri-category distinction and Kubernetes `Status.Reason` as the closest surveyed precedent for Precept's business-outcome / boundary-validation / fault taxonomy. F#/Rust typed result unions ground the pattern-matching model. The survey's cross-cutting finding — that most state machine runtimes (Temporal, XState, Erlang) cannot distinguish these categories at the type level — is now cited to strengthen the innovation claim.

6. **`diagnostic-and-output-design-survey.md` → §2 diagnostics throughout.** One addition. The failure-modes catalog paragraph now grounds Precept's `DiagnosticCode`/`Diagnostic` rule-vs-instance separation in the Roslyn `DiagnosticDescriptor`/`Diagnostic` pattern. The severity-level divide (DSL-scale tools are error-only; GP compilers define 4+ levels) is documented, framing Precept's multi-severity diagnostics as an intentional choice above DSL-scale norms.

---

# George — Technical Review: combined-design-v2.md

**Date:** 2026-04-28
**Verdict:** APPROVED-WITH-CONCERNS

## Summary

The doc is architecturally sound, internally consistent with the catalog system, and faithful to the philosophy. The pipeline stages, artifact boundaries, lowering contract, and runtime operation surfaces are accurately described. The constraint evaluation matrix and activation indexes are precise and correct. The proof/fault chain is well-specified.

However, the doc has **critical implementation-readiness gaps for the Parser** (our immediate next step) and **Roslyn-bias risks** that would cause an implementer to default to conventional compiler patterns in at least four places. These must be patched before the parser design doc is written.

## Top 3 Recommended Changes Before Parser Design

1. **Add a concrete SyntaxTree node inventory.** The doc says what `SyntaxTree` is for but never enumerates its node types. An implementer has no guidance on whether to produce `PreceptSyntax` as a single flat root with child declarations, a Roslyn-style red/green tree, or something else. The Constructs catalog defines 11 `ConstructKind` values with typed slots — the doc should state explicitly that the parser produces one syntax node type per `ConstructKind`, with child nodes corresponding to `ConstructSlot` entries. This is the single most critical gap.

2. **Specify the parser/TypeChecker contract boundary.** The doc says the parser stamps `ConstructKind`, `ActionKind`, `OperatorKind`, `TypeKind` on `TypeRef` nodes, and `ModifierKind` — but doesn't say what the parser guarantees to the TypeChecker and what it doesn't. Does the parser guarantee structurally well-formed declarations (every required slot filled, or represented as a missing-node)? Does the parser guarantee that all identifiers in keyword positions actually resolved to catalog keywords? What can the TypeChecker assume without re-checking? This contract is the #1 source of bugs in multi-pass compilers when it's implicit.

3. **Add an explicit anti-Roslyn section for the parser.** The doc should state: (a) Precept's grammar is line-oriented and flat — there is no block nesting, no brace-delimited scopes, no expression statements; (b) error recovery is construct-level — if a line doesn't parse, emit a diagnostic and skip to the next newline-anchored declaration keyword; (c) the parser does NOT need a general-purpose recursive-descent expression parser for the full language — expressions only appear in specific slots (guards, action RHS, ensure clauses, computed fields, because clauses); (d) operator precedence comes from `Operators.GetMeta()`, not a hardcoded precedence table.

## Technical Accuracy Issues

1. **§5 Parser: "Catalog entry" claim is underspecified.** The doc says "The parser stamps syntax-level identities as soon as syntax alone can know them: construct kind, anchor keyword, action keyword, operator token, literal segment form." This is accurate in principle but dangerous in practice. The parser CAN stamp `ConstructKind` because construct dispatch is keyword-anchored (`field`, `state`, `event`, `rule`, `from`, `in`, `to`, `on`). The parser CAN stamp `OperatorKind` because operators are token-level. But `ActionKind` is ambiguous in syntax — `set` is both an action keyword (TokenCategory.Action) and a type keyword (TokenCategory.Type). The doc should note that `ActionKind` stamping requires the parser to use position context (after `->` = action; after `as`/`of` = type), and this disambiguation is a parser responsibility, not a catalog lookup.

2. **§6 TypeChecker: "projection, not in-place annotation" is correct but needs the mechanism.** The doc says `TypedModel` is a projection of `SyntaxTree`, which is architecturally right. But it doesn't say whether the TypeChecker walks the `SyntaxTree` nodes and produces parallel `TypedModel` nodes (Roslyn's approach), or whether it reads the `SyntaxTree` and populates semantic tables/inventories (more like a symbol-table-driven approach). For Precept's flat, declaration-oriented grammar, the latter is correct — the TypeChecker should build declaration registries and expression bindings, not a parallel tree. The doc should say this explicitly to prevent a Roslyn-pattern default.

3. **§8 ProofEngine: Strategy descriptions are accurate but "straightforward flow narrowing" is vague.** Literal proof, modifier proof, and guard-in-path proof are well-scoped. "Straightforward flow narrowing" could mean anything from SSA-based type narrowing to full dataflow analysis. In Precept's context, this should be scoped to: "if a guard clause in the same transition row establishes a constraint on a field, that constraint is available as evidence for proof obligations on expressions within that row's action chain." The doc should narrow this.

4. **§10 Lowering: "Catalogs are not re-read here" is a design assertion, not an implementation constraint.** Lowering receives `TypedModel` and `GraphResult` which already carry catalog-resolved identities. But the lowering step may still need to read catalog metadata for things like default-value computation, constraint text extraction, or fault-site descriptor construction. The doc should clarify: lowering reads catalog metadata transitively through already-resolved model identities, but does not perform fresh catalog lookups for classification purposes.

5. **Descriptor types are described by name but not by shape.** The doc references `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `ConstraintDescriptor` — and `ConstraintDescriptor` actually exists in code with a concrete shape. But the other four are named without any shape specification. The implementation action items (§Appendix A) correctly flag this, but the main doc body treats them as if they're defined. An implementer reading §10–§11 would assume these types exist.

## Implementation Readiness Gaps

### Parser (immediate next step)
- **No node inventory.** The doc never says what `PreceptSyntax Root` contains. Is it a list of declaration nodes? What are the node types? How do expression nodes nest? This is the gap that will cause the most re-derivation.
- **No error recovery strategy.** The doc says "recovery shape" and "missing-node representation" but doesn't specify the pattern. Does the parser use marker nodes for missing required slots? Does it use `Option<T>` for optional slots? Does it skip-to-newline on error?
- **No expression grammar specification.** Expressions appear in guards, action RHS values, ensure clauses, computed fields, if/then/else, and because clauses. The doc doesn't specify the expression grammar or how it relates to the Operators/Operations catalogs. The parser needs to know: what are the expression forms? (binary, unary, literal, field-ref, event-arg-ref, function-call, if-then-else, is-set/is-not-set, contains, member-access). This is a significant omission.
- **No statement about whether Precept's grammar is LL(1), LL(k), or requires backtracking.** Given the line-oriented, keyword-anchored design, it should be LL(1) with single-token lookahead in most positions, but this should be stated.
- **Catalog lookup timing for the parser is unclear.** The doc says the parser consults `Constructs`, `Tokens`, `Operators`, `Diagnostics`. It should specify: the parser uses `Tokens.Keywords` (already built into the lexer) for keyword recognition; it uses `Constructs.GetMeta()` for construct shape validation; it uses `Operators.GetMeta()` for precedence during expression parsing. When does each lookup happen?

### TypeChecker
- Adequate for a per-component design doc, but needs the "semantic tables, not parallel tree" clarification (issue #2 above).
- The typed action family (§6) is well-specified — this is a strong point.
- Missing: how does the TypeChecker handle multi-name declarations (`field a, b, c as number`)? Does it expand them into separate symbols? This matters for the TypedModel shape.

### GraphAnalyzer
- Adequate. The `GraphResult` facts list is concrete enough. The key risk is an implementer building a general graph library instead of the four specific fact categories listed.
- Missing: does the graph operate over `StateDescriptor` and `EventDescriptor` identities from the TypedModel, or does it build its own node identities? The doc says it consumes `TypedModel` — this should be explicit.

### ProofEngine
- The bounded strategy set is the right design. The proof/fault chain is well-specified.
- Missing: what does "unresolvable by the compiler" mean for the author? Is it always a hard error? Or can the author annotate intent? (I suspect hard error is correct for Precept's philosophy — but the doc should state it.)

### Evaluator/Runtime
- The constraint evaluation matrix is precise and correct. The operation-facing plan selection table is accurate.
- Missing: the doc never describes the expression evaluation model. Are lowered expressions interpreted (tree-walk)? Compiled to IL? Compiled to delegates? For Precept's scale, tree-walk interpretation is the right answer — but the doc should say so to prevent an implementer from building a JIT compiler.

## Roslyn-Bias Risks

1. **§5 Parser — An implementer will build a Roslyn-style recursive-descent parser with red/green trees.** The doc says nothing about Precept's grammar being flat and line-oriented. Roslyn's design is for a language with deep nesting (classes → methods → statements → expressions). Precept has NO nesting beyond expression-within-declaration. An implementer needs to know: one pass, keyword-dispatched, line-scoped declarations, with expression sub-parsing only in specific slots. The red/green tree pattern is massive overkill.

2. **§6 TypeChecker — An implementer will write per-construct-kind check methods.** The doc says the TypeChecker resolves `OperationKind`, `FunctionKind`, etc. from catalogs, but doesn't explicitly say: "the type checker should NOT have a `CheckFieldDeclaration()`, `CheckTransitionRow()`, etc. method per construct kind — it should have generic resolution passes that read construct metadata." In practice, some per-construct logic is unavoidable (field declarations and transition rows have genuinely different type-checking needs), but the doc should draw the line: catalog-resolvable checks are generic; construct-specific structural validation is the only place for per-kind methods.

3. **§8 ProofEngine — An implementer will reach for Z3 or an SMT solver.** The doc's "four strategies only, no general SMT solver" is the right call and IS stated. But the strategies are described abstractly enough that an implementer might still build a general obligation-discharge framework. The doc should add: "Each strategy is a simple predicate function, not a solver. Literal proof checks whether the operand is a compile-time constant. Modifier proof checks whether the field carries a relevant modifier. Guard-in-path proof checks whether the enclosing guard expression subsumes the obligation. Flow narrowing checks type state through the immediately enclosing control path."

4. **§10 Lowering — An implementer will serialize the TypedModel.** The doc says lowering "selectively transforms" — but an implementer's default is to map TypedModel types 1:1 to runtime types. The doc should state: the runtime model's shape is organized for execution, not for semantic analysis. Constraint plans are grouped by activation anchor, not by source declaration order. Action plans are grouped by transition row, not by field. The runtime model is a dispatch-optimized index, not a renamed analysis model.

5. **Expression parsing — An implementer will build a Pratt parser or ANTLR grammar.** Neither is stated as required or prohibited. For Precept's expression grammar (binary ops with catalog-defined precedence, unary negation/not, field refs, function calls, if/then/else, member access, is-set, contains), a simple precedence-climbing parser reading precedence from `Operators.GetMeta()` is the right tool. The doc should name this.

## Right-Sizing Issues

1. **The `TypedModel` inventory (§5) is comprehensive but may be over-specified for initial implementation.** "Dependency facts — computed-field dependencies, arg dependencies, referenced-field sets, and semantic edge data" is real and needed, but it's a second-pass concern. The initial TypedModel needs declaration symbols, reference bindings, and typed expressions. Dependency extraction can be a sub-pass within the TypeChecker or a separate lightweight analysis. The doc should distinguish "required for correctness" from "required for optimization."

2. **The proof engine's four strategies are right-sized for the current language.** No over-generalization detected. This is a strong point.

3. **The constraint activation indexes (§10) are correctly scoped.** Four indexes for five anchor families, with the activation discriminant — this is exactly what the evaluator needs, no more.

4. **The three-tier constraint query contract is well-designed but the doc should note that Tier 2 (ApplicableConstraints) is a runtime convenience, not an evaluation necessity.** The evaluator always uses the activation indexes directly. Tier 2 is for API consumers who want to know what constraints are relevant before firing an event.

