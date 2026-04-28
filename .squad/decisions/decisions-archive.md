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

# Doc Audit: `writable` Field Modifier — Findings & Decisions

**Date:** 2025-04-27  
**Author:** Frank (Lead/Architect)  
**Scope:** All 32 files in `docs/` audited for `writable` modifier language change

---

# Soup Nazi: writable Test Coverage Review

**Date:** 2025-07-06
**Reviewer:** Soup Nazi (Tester)
**Scope:** `writable` field modifier — full catalog + regression + MCP audit
**Test run:** 1793 tests (includes 10 new `WritableSurfaceTests` added during investigation), 0 failed, 0 skipped ✅

