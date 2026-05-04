# Cross-Cutting Decisions Register Review

**From:** Frank (Lead Architect)
**Date:** 2025-01-21
**Type:** Analysis Complete
**Artifact:** `docs/working/cross-cutting-decisions.md`

---

## Summary

Completed comprehensive gap coverage review across all 11 canonical docs.
Created `docs/working/cross-cutting-decisions.md` documenting:

- **5 Priority 1 decisions** (blocking multiple stages, must resolve first)
- **7 Priority 2 decisions** (significant cross-stage impact)
- **8 Priority 3 decisions** (minor coordination needed)
- **Coverage assessment** with 13 newly-found gaps not in existing registers

## Key Findings

**Expression tree design** is the single most blocking cross-cutting decision — it affects Parser, Type Checker, Proof Engine, Evaluator, and Precept Builder. No expression-related implementation can proceed until this is resolved.

**Gap register coverage: ~90%** — the two existing registers capture most significant gaps. The 13 newly-found gaps are primarily:
- Literal system implementation questions
- Evaluator opcode execution details
- Graph analyzer edge-case semantics

## Decisions Needed

1. **Expression tree structure** — Roslyn-style vs S-expression vs span+lazy-parse
2. **SlotValue shape authority** — parser.md vs type-checker.md as canonical
3. **SemanticIndex reference collections** — add to type checker or reconstruct in LS?

## Recommendation

Schedule a design session to resolve Priority 1 decisions before implementation sprints begin. These cannot be resolved stage-by-stage — they require coordinated decision-making.

# Technical Review: Elaine's `lookup`/`queue` Surface Proposals

**By:** Frank
**Date:** 2025-07-17
**Status:** Recommendations delivered — pending owner sign-off

---

## Proposal 1 — Replace `containskey` with `contains`

**Verdict: APPROVED.**

No grammar ambiguity. `contains` is an infix expression operator at precedence 40 (spec §2.1). It parses as `ContainsExpression(left, ParseExpression(40))`. The left operand is resolved to a field type by the type checker, not the parser. Extending the type checker's `contains` validation table from `{set, queue, stack}` to `{set, queue, stack, lookup}` is a pure type-checker change. The parser sees `Expr contains Expr` regardless of whether the left side is a set or a lookup.

If someone passes a `V`-typed expression to `F contains Expr` on a `lookup of K to V`, the type checker fires `TypeMismatch` — the expected type is `K`, the actual type is `V`. The diagnostic message should say "contains on lookup tests key membership; expected type K, got V." This is clean — no new diagnostic code needed, just a message template specialization.

The `-key` suffix is purely cosmetic disambiguation. No parser production, no proof obligation, no evaluator branch depends on the distinction between `contains` and `containskey`. The type checker already knows the collection kind from the field's declared type. The suffix duplicates information the type system already has.

---

## Proposal 2 — Replace `removekey` with `remove`

**Verdict: APPROVED.**

Parser: no changes required. The `ActionStatement` grammar is already `remove Identifier Expr`. The parser emits the same AST node regardless of whether the field is `set of T` or `lookup of K to V`. Type checker resolves the field type and validates that the expression matches `T` (for set) or `K` (for lookup). This is a type-checker-only extension.

Proof obligation: confirmed identical to `set`. `remove` on `set` is no-op-if-absent — no guard required, no emptiness proof needed. `removekey` on `lookup` has the same semantics (spec: "removekey requires no guard — no-op if absent, like remove on set"). Unifying the keyword preserves this guarantee. No new proof obligation category.

The `-key` suffix is not load-bearing anywhere. No pipeline stage, no evaluator branch, no proof rule depends on it. It exists only because the original `collection-types.md` design mirrored .NET's `Dictionary.ContainsKey`/`Dictionary.Remove` API naming. That's API naming leaking into a DSL surface — exactly what Precept's language design is supposed to prevent.

---

## Proposal 3 — Use `by` at the dequeue-capture site

**Verdict: APPROVED WITH MODIFICATION.**
### Analysis of filter-condition ambiguity

The concern I raised previously: `dequeue ClaimQueue into CurrentClaim by CurrentSeverity` could be misread as "dequeue the item BY this severity" (a filter/selection condition) rather than "dequeue and capture the severity INTO this field."

Is this a real parsing ambiguity? **No.** The parser grammar for dequeue is:

```
dequeue Identifier (into Identifier (by Identifier)?)?
```

There is no conditional-dequeue production. The parser has no `by` + expression continuation that would create a grammatical fork. The `by` keyword in this position is unambiguously a capture binding — the parser cannot misparse it.

Is it a reader-misparse risk? **Mildly.** A business author encountering `dequeue F into X by Y` for the first time might momentarily wonder whether `by Y` means "select by Y" or "capture Y." But this is a first-encounter learning cost, not an ongoing ambiguity. Once learned, the pattern is stable.
### Weighing the arguments

**Elaine's consistency argument** (spec Principle 5 — keyword-anchored readability): The `by` keyword appears at declaration (`queue of T by P`), at enqueue (`enqueue F Expr by Priority`), and now at dequeue (`dequeue F into X by Y`). The same keyword, the same role (introducing the priority axis), in all three action contexts. An author who writes `enqueue F X by P` one line above will instinctively reach for `by` at dequeue. Encountering `priority` there is a vocabulary seam — two words for one concept within the same type.

**My filter-reading concern**: Theoretical. No grammar production creates ambiguity. No current or planned Precept feature introduces conditional dequeue. If conditional dequeue were ever needed, it would use `when` (the language's universal guard keyword), not `by`. The `by` keyword is already claimed for priority-axis role connection — overloading it for a future filter condition would itself be the design error.

**Verdict:** Elaine's consistency argument is stronger. Principle 5 says "statement kind is identified by its opening keyword sequence" — and within that, vocabulary consistency across the lifecycle of a single type is the natural corollary. `by` at declaration, `by` at enqueue, `by` at dequeue. The fork was unjustified.
### The modification

The accessor (`.priority`) and quantifier binding (`.priority`) remain as nouns. This is correct and Elaine explicitly preserves it. `by` is a preposition introducing a role at action sites. `.priority` is a noun naming a property at access sites. Different grammatical roles, same underlying concept. No seam.

---

## Summary Table

| Proposal | Verdict | Conditions |
|---|---|---|
| `contains` replaces `containskey` | **Approved** | Type checker emits `TypeMismatch` if `V`-typed arg supplied; diagnostic message should name the key/value distinction |
| `remove` replaces `removekey` | **Approved** | No-op-if-absent semantics preserved; no new proof obligation |
| `by` replaces `priority` at dequeue-capture | **Approved** | Accessor (`.priority`) and quantifier binding (`.priority`) retain noun form |

---

## Implementation Notes

All three changes are type-checker-only and catalog-metadata updates. No parser grammar changes. No new AST node types. The `Actions` catalog entry for `remove` gains `lookup` in its applicable-types metadata. The `Operations` catalog entry for `contains` gains `lookup` in its valid-lhs-types list. The dequeue action grammar already supports an optional trailing identifier — the keyword text changes from `priority` to `by`.

The `containskey` and `removekey` tokens can be removed from the lexer's keyword table entirely (they are not yet implemented — this is pre-implementation design). The `priority` keyword at action sites is similarly pre-implementation.

---

---

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

---

---

# One-shot: install puppeteer, screenshot, remove
npm install --no-save puppeteer
node -e "<screenshot script>"  # see commit for full script
npm uninstall puppeteer && rm package.json package-lock.json && rm -rf node_modules
```

Future improvement: automate this as a build script or CI step.

---

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

---

# Decision: Issue #22 Design Fidelity Directive

**Date:** 2026-04-08
**By:** Shane (user directive)

When implementing issue #22, if anything the team is going to implement strays from the design docs or seems ambiguous, they must stop and ask rather than guess. Design understanding is a prerequisite before coding starts.

---

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

---

# Design Evaluation: Per-Field `readonly` Modifier as Access Default Inversion

**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2025-07-14
**Requested by:** Shane
**Verdict:** **Reject**

---

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

---

# Doc Audit: `writable` Field Modifier — Findings & Decisions

**Date:** 2025-04-27
**Author:** Frank (Lead/Architect)
**Scope:** All 32 files in `docs/` audited for `writable` modifier language change

---

---

# Soup Nazi: writable Test Coverage Review

**Date:** 2025-07-06
**Reviewer:** Soup Nazi (Tester)
**Scope:** `writable` field modifier — full catalog + regression + MCP audit
**Test run:** 1793 tests (includes 10 new `WritableSurfaceTests` added during investigation), 0 failed, 0 skipped ✅

---

---

# Decision: README hero PNG fallback

- **Context:** GitHub does not render the styled inline HTML contract block in `README.md` as intended.
- **Decision:** Use `brand/readme-hero-dsl.png` as the GitHub-facing contract sample and keep a collapsed plain-text version immediately below for copyability.
- **Why:** The PNG preserves the intended branded syntax presentation on GitHub, while the collapsed source keeps the sample useful to humans and AI agents without turning the section back into a long raw block.
- **Files:** `README.md`, `brand/readme-hero-dsl.png`, `brand/readme-hero-dsl.precept`

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

---

# Precept V2 — Exhaustive Parser & Lexer Test Coverage Audit

**Branch:** `spike/Precept-V2`
**Requested by:** @soup-nazi
**Date:** 2025-07
**Baseline:** 2107 passing, 0 failing
**Constraint:** `Compiler.Compile()` unusable (TypeChecker throws `NotImplementedException`). All parser tests use `Lexer.Lex()` + `Parser.Parse()` directly. TypeChecker-level diagnostics are blocked.

---

## Executive Summary

Exhaustive cross-reference of every construct in `docs/language/precept-language-spec.md` against all 23 test files and all 28 sample files in `samples/`.

| Severity | Count | Description |
|----------|------:|-------------|
| **Critical** | 2 | Known parser bugs producing wrong diagnostics on spec-valid input |
| **High** | 15 | Spec-defined constructs with zero parser tests; all have sample-file usage |
| **Medium** | 14 | Constructs with test gaps: missing variants, edge cases, or partial coverage |
| **Low / Blocked** | 7 | Edge cases, TypeChecker-stage validations, or trivial diagnostic variants |
| **Total Gaps** | **38** | Out of ~75 distinct parse-level constructs inventoried |

Sample file parse coverage: **3 clean / 2 partial / 23 untested** (of 28 total).

---

## Coverage Matrix

Columns: **Construct** · **Positive Test?** · **Negative Test?** · **File(s)** · **Severity** · **Notes**
### 1 · Precept Header

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| `precept Name` header | ✅ | ✅ (missing name) | `ParserTests.cs` | — | Well-covered |

---
### 2 · Top-Level Declarations

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| `field Name as TypeRef Modifiers?` | ✅ | ✅ | `ParserTests`, `SlotParserTests` | — | Well-covered |
| `field Name as TypeRef -> Expr` (computed) | ✅ | — | `ParserTests` | — | Computed field happy path covered |
| `field N1, N2 as TypeRef` (multi-name) | ✅ | — | `ParserTests` | — | Multi-name shorthand covered |
| `state Name (modifiers)?` | ✅ | — | `ParserTests`, `SlotParserTests` | — | Well-covered |
| `state N1, N2` (multi-name) | — | — | — | **Medium** | No test for multiple states in single decl |
| `event Name (Args)? initial?` | ✅ | — | `ParserTests`, `SlotParserTests` | — | Single-name form covered |
| `event N1, N2` (multi-name) | — | — | — | **Medium** | No test for `event Submit, Cancel` shorthand |
| `rule BoolExpr because "msg"` | ✅ | ✅ (missing `because`) | `ParserTests` | — | Well-covered |
| `rule BoolExpr when Guard because "msg"` | ✅ | — | `ParserTests` | — | Guard form covered |

---
### 3 · In-State Declarations (`in State ...`)

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| `in State ensure Condition because "msg"` | ✅ | — | `ParserTests` (Slice 4.2) | — | Simple form covered |
| `in State ensure Condition when Guard because "msg"` | ❌ BUG | ❌ BUG | `ParserTests` (known failure) | **Critical** | **GAP-2**: Parser terminates condition at `when`, then `Expect(Because)` sees `when` and emits bogus diagnostic. Used in `insurance-claim.precept` line 28, `loan-application.precept` line 25. |
| `in State modify FieldTarget readonly/editable` | ✅ | ✅ | `ParserTests` (Slice 4.1, 4.2) | — | Well-covered |
| `in State modify ... when Guard` (pre/post guard) | ✅ | — | `ParserTests` | — | Guard on modify covered |
| `in State omit FieldTarget` | ✅ | ✅ | `ParserTests` | — | Well-covered |

---
### 4 · To-State Declarations (`to State ...`)

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| `to State ensure Condition because "msg"` | ✅ | — | `ParserTests` (Slice 4.3) | — | Simple form covered |
| `to State ensure Condition when Guard because "msg"` | — | — | — | **Medium** | No test for guard-bearing to-ensure. Spec §2.2 defines this form. |
| `to State -> ActionList` | ✅ | — | `ParserTests` (Slice 4.3) | — | Well-covered |
| `to State -> ActionList when Guard` | ✅ | — | `ParserTests` | — | Guard form covered |

---
### 5 · From-State Declarations (`from State ...`)

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| `from State ensure Condition because "msg"` | ✅ | — | `ParserTests` (Slice 5.1) | — | Simple form covered |
| `from State ensure Condition when Guard because "msg"` | — | — | — | **Medium** | No test for guard-bearing from-ensure. Same spec form as GAP-2. |
| `from State -> ActionList` | ✅ | — | `ParserTests` (Slice 5.1) | — | Covered |
| `from any on Event -> Outcome` | ✅ (no-transition) | — | `ParserTests` (Slice 5.1) | **Medium** | `from any` tested only with `no transition`. No test for `from any -> transition X` or `from any -> reject "msg"`. |

---
### 6 · On-Event Declarations (`on Event ...`)

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| `on Event ensure Condition because "msg"` | ✅ | — | `ParserTests` (Slice 4.4) | — | Simple form covered |
| `on Event ensure Condition when Guard because "msg"` | ✅ | — | `ParserTests` | — | Guard form covered |
| `on Event -> ActionList` | ✅ | — | `ParserTests` (Slice 4.4) | — | Well-covered |

---
### 7 · Transition Rows (`from State on Event ...`)

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| `from State on Event -> Outcome` | ✅ | — | `ParserTests` (Slice 5.1) | — | Well-covered |
| `from State on Event when Guard -> Outcome` | ✅ | — | `ParserTests` | — | Guard form covered |
| `from State on Event -> Actions -> Outcome` | ✅ | — | `ParserTests` | — | Actions before outcome covered |
| `-> transition StateName` outcome | ✅ | — | `ParserTests` | — | Covered |
| `-> no transition` outcome | ✅ | — | `ParserTests` | — | Covered |
| `-> reject "msg"` outcome | ✅ | — | `ParserTests` | — | Covered |
| `from any on Event -> ...` — all outcomes | Partial | — | `ParserTests` | **Medium** | `no transition` tested; `transition X` and `reject "msg"` with `any` not tested |

---
### 8 · Action Statements

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| `set F = Expr` | ✅ | — | `ParserTests` (Slice 4.3) | — | Covered |
| `add F Expr` | ✅ | — | `ParserTests` | — | Covered |
| `remove F Expr` | — | — | — | **High** | **GAP-6**: No parser test. Used in `hiring-pipeline.precept` (line 54), `insurance-claim.precept` (line 49). |
| `enqueue F Expr` | — | — | — | **High** | **GAP-7**: No parser test. `ActionsTests` covers catalog entry only. |
| `dequeue F` | — | — | — | **High** | **GAP-5 (partial)**: No parser test for dequeue without `into`. |
| `dequeue F into G` | — | — | — | **High** | **GAP-5**: No parser test for `into` clause. `IntoSupported` flag verified in catalog only. |
| `push F Expr` | — | — | — | **High** | **GAP-7 (push variant)**: No parser test. |
| `pop F` | — | — | — | **High** | **GAP-8**: No parser test for pop without `into`. |
| `pop F into G` | — | — | — | **High** | No parser test for `into` clause on pop. |
| `clear F` | ✅ | — | `ParserTests` (Slice 4.3) | — | Covered |

---
### 9 · Expression Atoms

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| Identifier | ✅ | — | `ExpressionParserTests` | — | Covered |
| Integer literal | ✅ | — | `ExpressionParserTests` | — | Covered |
| Decimal literal | ✅ | — | `ExpressionParserTests` | — | Covered |
| Exponent literal (`1.5e2`) | — | — | — | **Low** | Lexer covers it; no expression-parse test for exponent form |
| Boolean literal (`true`/`false`) | ✅ | — | `ExpressionParserTests` | — | Covered |
| String literal (plain) | ✅ | — | `ExpressionParserTests` | — | Covered |
| Interpolated string (`"Hello {Name}"`) | — | — | — | **High** | **GAP-10**: No expression-parser test for `StringStart`/`StringMiddle`/`StringEnd` reassembly into `InterpolatedStringExpression`. Lexer tests only. Used in multiple sample files. |
| Typed constant (`'2026-04-23'`) | — | — | — | **Critical** | **GAP-1**: `ParseAtom()` has no case for `TypedConstant` token. Expression parser will emit error or fall through. Used in `fee-schedule.precept` (implicit), any file with temporal/domain typed constants in expressions. |
| Interpolated typed constant (`'amount {N}'`) | — | — | — | **High** | **GAP-11**: No expression-parser test for `TypedConstantStart`/`Middle`/`End` reassembly. Depends on GAP-1 fix. |
| List literal (`[1, 2, 3]`) | — | — | — | **High** | **GAP-12**: No expression-parser test for `LeftBracket` → `ListLiteralExpression`. Spec §2.1 null-denotation table includes it. Used in `default` clauses for collection fields. |
| Parenthesized expression | ✅ | — | `ExpressionParserTests` | — | Covered |
| Negative literal folding (`-1`, `-3.14`) | ✅ | — | `ExpressionParserTests` | — | Covered |

---
### 10 · Expression Operators

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| `+` addition | ✅ | — | `ExpressionParserTests` | — | Covered |
| `-` subtraction | ✅ | — | `ExpressionParserTests` | — | Covered |
| `*` multiplication | ✅ | — | `ExpressionParserTests` | — | Covered |
| `/` division | — | — | — | **Medium** | `OperatorsTests` covers catalog; no `ParseExpr("a / b")` test |
| `%` modulo | — | — | — | **High** | **GAP-16**: No `ParseExpr("a % b")` test. Catalog and lexer tested only. |
| `>` greater-than | ✅ | — | `ExpressionParserTests` | — | Covered |
| `<` less-than | — | — | — | **High** | **GAP-17 (partial)**: Only `>` is tested. No `ParseExpr("a < b")` test. |
| `>=` greater-than-or-equal | — | — | — | **High** | **GAP-17**: No test. Used in `loan-application.precept` (`CreditScore >= 300`). |
| `<=` less-than-or-equal | — | — | — | **High** | **GAP-17**: No test. |
| `==` equals | — | — | — | **High** | **GAP-17**: No test. Used in `customer-profile.precept` (`MarketingOptIn == false`). |
| `!=` not-equals | — | — | — | **High** | **GAP-17**: No test. |
| `~=` case-insensitive equals | — | — | — | **High** | **GAP-15**: No `ParseExpr("name ~= 'john'")` test. Catalog and lexer tested only. |
| `!~` case-insensitive not-equals | — | — | — | **High** | **GAP-15**: No test. |
| `and` | ✅ | — | `ExpressionParserTests` | — | Covered |
| `or` | ✅ | — | `ExpressionParserTests` | — | Covered |
| `not` (prefix) | ✅ | — | `ExpressionParserTests` | — | Covered |
| `is set` (postfix) | — | — | — | **Critical** | **GAP-3 (known)**: No expression-parser test. Used in `insurance-claim.precept` (line 28), `loan-application.precept` (line 62), `customer-profile.precept` (line 17). |
| `is not set` (postfix) | — | — | — | **Critical** | **GAP-3 (known)**: No expression-parser test. |
| `contains` (infix) | — | — | — | **High** | **GAP-4**: No expression-parser test for `set contains value`. Used in `hiring-pipeline.precept` (line 53), `insurance-claim.precept` (line 62). |
| Non-associative comparison diagnostic | — | ❌ | — | **High** | **GAP-13**: No test for `a == b == c` producing `NonAssociativeComparison` diagnostic. Listed as parse-stage code in `DiagnosticsTests`. |

---
### 11 · Expression Forms (Structural)

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| Member access (`obj.field`) | ✅ | — | `ExpressionParserTests` | — | Covered |
| Function call (`f(a, b)`) | ✅ | — | `ExpressionParserTests` | — | Covered |
| Method call (`obj.method(args)`) | — | — | — | **Medium** | **GAP-29**: No test for left-denotation `MemberAccessExpression → (` → `MethodCallExpression`. Used by collection accessors `.count`, `.peek`, `.min`, `.max`. |
| `InvalidCallTarget` diagnostic | — | ❌ | — | **Medium** | **GAP-14**: No test for `(a + b)(x)` producing `InvalidCallTarget` diagnostic. |
| Conditional (`if E then E else E`) | ✅ | — | `ExpressionParserTests` | — | Covered |
| Precedence (arithmetic before logical) | ✅ | — | `ExpressionParserTests` | — | Covered |
| Boundary at `when` | ✅ | — | `ExpressionParserTests` | — | Covered |
| Boundary at `because` | ✅ | — | `ExpressionParserTests` | — | Covered |

---
### 12 · Type References

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| Scalar: `string`, `boolean`, `integer`, `decimal`, `number` | ✅ | — | `SlotParserTests` | — | Covered |
| Temporal: `date`, `time`, `instant`, `duration`, `period`, `timezone`, `zoneddatetime`, `datetime` | — | — | — | **Medium** | No parse test for `field D as date`. `SlotParserTests` covers `ParseTypeExpression` but only for selected scalar types. Catalog tests cover these as `TypeKind` entries. |
| Domain: `money`, `currency`, `quantity`, `unitofmeasure`, `dimension`, `price`, `exchangerate` | Partial | — | `ParserTests` (WSI qualifier tests) | **Medium** | `money in 'USD'` and `exchangerate from 'USD' to 'EUR'` tested via WSI tests. `currency`, `quantity`, `unitofmeasure`, `dimension`, `price` have no PARSE tests. |
| `set of T` collection type | ✅ | — | `SlotParserTests` | — | Covered |
| `queue of T` collection type | — | — | — | **Medium** | No parse test for `field Q as queue of string`. |
| `stack of T` collection type | — | — | — | **Medium** | No parse test for `field S as stack of string`. |
| `choice of T(v1, v2, ...)` type | ✅ | ✅ | `SlotParserTests` | — | Well-covered including diagnostic cases |
| Type qualifier `in 'unit'` | ✅ | — | `ParserTests` (WSI tests) | — | Covered for money/exchangerate |
| Type qualifier `of 'family'` | ✅ | — | `ParserTests` (WSI tests) | — | Covered for exchangerate/price |
| Type qualifier `to 'unit'` (exchange) | ✅ | — | `ParserTests` (WSI tests) | — | Covered |
| Case-insensitive collection `set of ~string` | — | — | — | **Medium** | **GAP-26**: No parser test for `Tilde` before `string` in collection inner-type position. |

---
### 13 · Field Modifiers (in `ModifierList`)

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| `optional` flag | Partial | — | `SlotParserTests` | **Medium** | **GAP-24**: `ModifiersTests` covers catalog; `SlotParserTests` parses modifier lists but no test asserting `optional` produces a `FlagModifierNode`. |
| `writable` flag | Partial | — | `WritableSurfaceTests` | **Medium** | **GAP-25**: `WritableSurfaceTests` tests lexing and compiler throw. No PARSE test asserting `FieldDeclarationNode.Modifiers` contains `FlagModifierNode(writable)`. |
| `nonnegative` flag | ✅ | — | `SlotParserTests` | — | Covered |
| `positive` flag | ✅ | — | `SlotParserTests` | — | Covered |
| `nonzero` flag | — | — | — | **Medium** | No parse test. Catalog covered in `ModifiersTests`. |
| `notempty` flag | — | — | — | **Medium** | No parse test. Catalog covered. Used in `DescriptorsTests`. |
| `ordered` flag | — | — | — | **Medium** | **GAP-23**: No parse test. Used in choice-field context. |
| `default Expr` value-bearing | Partial | — | `ParserTests` (WSI) | **Medium** | **GAP-18**: WSI test checks modifier count but not the expression node. No unit test for `default` producing `DefaultModifierNode` with correct expression. |
| `min Expr` value-bearing | — | — | — | **Medium** | **GAP-19**: No parse test. Used in `payment-method.precept`, `fee-schedule.precept`. |
| `max Expr` value-bearing | — | — | — | **Medium** | **GAP-19**: No parse test. Used in `payment-method.precept`, `fee-schedule.precept`. |
| `minlength Expr` value-bearing | — | — | — | **Medium** | **GAP-20**: No parse test. |
| `maxlength Expr` value-bearing | — | — | — | **Medium** | **GAP-20**: No parse test. |
| `mincount Expr` value-bearing | — | — | — | **Medium** | **GAP-21**: No parse test. |
| `maxcount Expr` value-bearing | — | — | — | **Medium** | **GAP-21**: No parse test. |
| `maxplaces Expr` value-bearing | — | — | — | **Medium** | **GAP-22**: No parse test. Used in `fee-schedule.precept`, `invoice-line-item.precept`. Integration test via `insurance-claim.precept` (partial). |

---
### 14 · State Modifiers

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| `initial` state modifier | ✅ | — | `SlotParserTests`, `ParserTests` | — | Covered |
| `terminal` state modifier | ✅ | — | `SlotParserTests`, `ParserTests` | — | Covered |
| `required` state modifier | — | — | — | **Medium** | **GAP-31**: No parse test. Catalog covered in `ModifiersTests`. |
| `irreversible` state modifier | — | — | — | **Medium** | **GAP-31**: No parse test. |
| `success` state modifier | — | — | — | **Medium** | **GAP-31**: No parse test. |
| `warning` state modifier | — | — | — | **Medium** | **GAP-31**: No parse test. |
| `error` state modifier | — | — | — | **Medium** | **GAP-31**: No parse test. Used in `trafficlight.precept` (integration-tested but no modifier assertion). |

---
### 15 · Lexer-Level Constructs

| Construct | ✅ Positive | ❌ Negative | Test File | Sev | Notes |
|-----------|------------|------------|-----------|-----|-------|
| String literal | ✅ | ✅ | `LexerTests` | — | Well-covered |
| Interpolated string (`"Hello {Name}"`) | ✅ | ✅ | `LexerTests` | — | Well-covered at lexer level |
| Typed constant (`'value'`) | ✅ | ✅ | `LexerTests` | — | Well-covered at lexer level |
| Interpolated typed constant | ✅ | ✅ | `LexerTests` | — | Well-covered at lexer level |
| Number literals (int, decimal, exponent) | ✅ | — | `LexerTests` | — | Well-covered |
| All operators and punctuation | ✅ | — | `LexerTests` | — | Well-covered |
| Comments (`# ...`) | ✅ | — | `LexerTests` | — | Covered |
| Newlines / whitespace | ✅ | — | `LexerTests` | — | Covered |
| Identifier rules | ✅ | ✅ | `LexerTests` | — | Covered including reserved words |
| Error recovery | ✅ | — | `LexerTests` | — | Covered |
| Nesting depth limit (8 levels) | — | ❌ | — | **Low** | **GAP-36**: Spec §1.7 defines max 8 interpolation nesting depth. No test enforcing `UnterminatedInterpolation` at depth 9. |
| `UnescapedBraceInLiteral` diagnostic | — | ❌ | — | **Low** | **GAP-37**: `DiagnosticsTests` lists this as a parse code. No lexer test for bare `}` inside a literal. |

---
### 16 · Parser Diagnostic Coverage

| Diagnostic Code | ✅ Produced by Test? | Test File | Sev | Notes |
|-----------------|---------------------|-----------|-----|-------|
| `UnexpectedToken` | ✅ | `ParserTests` | — | Error recovery tests produce this |
| `MissingBecause` | ✅ | `ParserTests` | — | `Parse_RuleDeclaration_MissingBecause` |
| `MissingOutcome` | ✅ | `ParserTests` | — | Error recovery covered |
| `PreEventGuard` | ✅ | `ParserTests` | — | Covered |
| `StashedGuard` | ✅ | `ParserTests` (Slice 4.4) | — | EventHandler stashed-guard diagnostic |
| `ChoiceMissingElementType` | ✅ | `SlotParserTests` | — | Covered |
| `ChoiceElementTypeMismatch` | ✅ | `SlotParserTests` | — | Covered |
| `EmptyChoice` | — | ❌ | — | **Low** | **GAP-34**: `choice of string()` form. Distinct from `ChoiceMissingElementType`. |
| `NonAssociativeComparison` | — | ❌ | — | **High** | **GAP-13**: Listed as parse-stage in `DiagnosticsTests`. No test producing it. |
| `InvalidCallTarget` | — | ❌ | — | **Medium** | **GAP-14**: No test producing `(expr)(args)` call-target error. |
| `UnexpectedKeyword` | — | ❌ | — | **Low** | **GAP-35**: Listed as parse-stage in `DiagnosticsTests`. No test producing it. |

---

## Top 10 Highest-Priority Gaps

Ordered by: parser correctness > spec contract > sample-file blast radius > implementation cost.
### Priority 1 — GAP-2: `in/to/from State ensure Condition when Guard` (Parser BUG)
**Severity:** Critical — parser bug producing false diagnostic on spec-valid syntax
**Spec:** §2.2 — `ensure BoolExpr ("when" BoolExpr)? ("because" StringExpr)?`
**Root cause:** `ParseExpr()` is called first; `when` is a `StructuralBoundaryToken`, so it terminates the condition early. Then `Expect(Because)` sees `when` and emits `MissingBecause`. The `when` guard clause after `ensure` is never parsed.
**Blast radius:** `insurance-claim.precept` line 28, `loan-application.precept` line 25. Both integration tests explicitly work around this failure with reduced assertion scope.
**Fix:** After `ParseExpr()` completes the condition, check if current token is `When`; if so, parse guard into a `GuardNode`; then optionally parse `because`.
**Tests needed:** `Parse_StateEnsure_In_WithConditionAndGuard`, `Parse_StateEnsure_To_WithConditionAndGuard`, `Parse_StateEnsure_From_WithConditionAndGuard`

---
### Priority 2 — GAP-1: `TypedConstant` atom in expression parser (Parser BUG)
**Severity:** Critical — typed constant literals produce parser error/fallthrough in expression context
**Spec:** §2.1 null-denotation table — `TypedConstant` token → `TypedConstantExpression`
**Root cause:** `ParseAtom()` has no case for `TokenKind.TypedConstant` or `TokenKind.TypedConstantStart`. The lexer produces these tokens correctly (confirmed by `LexerTests`), but the parser doesn't consume them.
**Blast radius:** Any precept using typed constant literals in expressions (`'2026-04-23'`, `'USD'`, etc.). Blocks `fee-schedule.precept`, `computed-tax-net.precept`, and any sample file with temporal/domain typed constant expressions.
**Fix:** Add `TypedConstant` case in `ParseAtom()` producing `TypedConstantExpression`; add `TypedConstantStart` case that reassembles interpolated typed constant using the same loop as interpolated strings.
**Tests needed:** `ParseExpr_TypedConstantLiteral_ProducesTypedConstantExpression`, `ParseExpr_InterpolatedTypedConstant_ProducesInterpolatedTypedConstantExpression`

---
### Priority 3 — GAP-3: `is set` / `is not set` postfix expressions
**Severity:** Critical (known gap)
**Spec:** §2.1 — postfix at precedence 40, alongside `contains`
**Root cause:** `ParseExpr()` left-denotation likely doesn't handle `Is` token followed by `Set` / `Not Set`. No `IsSetExpression` AST node produced.
**Blast radius:** `insurance-claim.precept` line 28 (`DecisionNote is set`), `loan-application.precept` line 62 (`Approve.Note is set`), `customer-profile.precept` line 17 (`Email is set`). `SyntaxReference.NullNarrowing` test references the string but doesn't parse it.
**Tests needed:** `ParseExpr_IsSet_ProducesIsSetExpression`, `ParseExpr_IsNotSet_ProducesNegatedIsSetExpression`

---
### Priority 4 — GAP-4: `contains` infix expression
**Severity:** High
**Spec:** §2.1 — infix at precedence 40
**Blast radius:** `hiring-pipeline.precept` line 53 (`PendingInterviewers contains RecordInterviewFeedback.Interviewer`), `insurance-claim.precept` line 62.
**Tests needed:** `ParseExpr_Contains_ProducesContainsExpression`, precedence test vs `and`/`or`

---
### Priority 5 — GAP-17: `<`, `<=`, `>=`, `==`, `!=` comparison operators
**Severity:** High — multiple operators completely untested in expression parser
**Spec:** §2.1 — all standard comparisons at precedence 30
**Blast radius:** `loan-application.precept` (`CreditScore >= 300`), `customer-profile.precept` (`MarketingOptIn == false`), all sample files using non-`>` comparisons in rules or guards.
**Tests needed:** `[Theory][InlineData("<")][InlineData("<=")][InlineData(">=")][InlineData("==")][InlineData("!=")]` — one theory covering all five missing operators

---
### Priority 6 — GAP-10: Interpolated string expression
**Severity:** High
**Spec:** §2.5 — `StringStart`/`StringMiddle`/`StringEnd` reassembly loop
**Blast radius:** Any `"string with {Field}"` expression in action statements. Multiple sample files use interpolated strings in `reject` messages and `set` expressions.
**Tests needed:** `ParseExpr_InterpolatedString_ProducesInterpolatedStringExpression`, test with multiple interpolation segments

---
### Priority 7 — GAP-5/6/7/8: `remove`, `enqueue`, `dequeue`, `push`, `pop` action statements
**Severity:** High (5 related gaps)
**Spec:** §2.2 action statement grammar
**Blast radius:** `hiring-pipeline.precept` (`remove`, `enqueue`), `insurance-claim.precept` (`remove`), any sample using queue/stack collections.
**Tests needed (per action):**
- `Parse_Action_Remove_ProducesRemoveActionNode`
- `Parse_Action_Enqueue_ProducesEnqueueActionNode`
- `Parse_Action_Dequeue_WithoutInto`, `Parse_Action_Dequeue_WithInto`
- `Parse_Action_Push_ProducesPushActionNode`
- `Parse_Action_Pop_WithoutInto`, `Parse_Action_Pop_WithInto`

---
### Priority 8 — GAP-12: List literal expression `[a, b, c]`
**Severity:** High
**Spec:** §2.1 — `LeftBracket` null-denotation → `ListLiteralExpression`
**Blast radius:** `default []` on collection fields; any expression initializing or comparing a set/queue/stack.
**Tests needed:** `ParseExpr_EmptyList`, `ParseExpr_NonEmptyList`, `ParseExpr_NestedList` (if supported)

---
### Priority 9 — GAP-15: `~=` and `!~` case-insensitive operators
**Severity:** High
**Spec:** §2.1 — comparison operators at precedence 30
**Blast radius:** Any DSL doing case-insensitive string matching. Catalog confirmed in `OperatorsTests`, tokens confirmed in `LexerTests`. Missing parser layer.
**Tests needed:** `ParseExpr_CaseInsensitiveEquals_ProducesCorrectNode`, `ParseExpr_CaseInsensitiveNotEquals`

---
### Priority 10 — GAP-13: `NonAssociativeComparison` diagnostic
**Severity:** High
**Spec:** §2.7 — parse-stage error when a second comparison is chained: `a > b > c`
**Tests needed:** `ParseExpr_ChainedComparison_EmitsNonAssociativeComparison` — verifies both that a diagnostic is emitted AND that parsing recovers cleanly

---

## Sample File Parse Coverage
### Well-Tested (clean parse, multiple assertions)

| File | Tests | What's Verified |
|------|------:|----------------|
| `crosswalk-signal.precept` | 4 | No diagnostics, header, declaration count (5), AccessModeNodes, TransitionRows |
| `trafficlight.precept` | 4 | No diagnostics, header, declaration count (5), AccessModeNodes, TransitionRows |
| `hiring-pipeline.precept` | 4 | No diagnostics (WSI), header, declaration count (5), TransitionRows |
### Partial Coverage (known parse failures, counts only)

| File | Tests | What's Verified | Known Failures |
|------|------:|----------------|----------------|
| `insurance-claim.precept` | 1 | Declaration counts only (8 fields verified) | GAP-2 (`in Approved ensure ... when`), GAP-3 (`is set`) |
| `loan-application.precept` | 1 | Declaration counts only | GAP-2 (`in UnderReview ensure ... when`) |
### No Parse Coverage (23 files)

The following sample files are never loaded by a test. Each has a note on which features it would exercise:

| File | Key Constructs to Exercise |
|------|---------------------------|
| `apartment-rental-application.precept` | Complex lifecycle, `from any`, multiple ensure |
| `building-access-badge-request.precept` | Multi-step workflow, guards, computed fields |
| `clinic-appointment-scheduling.precept` | Temporal types, `date`/`time` fields, `duration` |
| `computed-tax-net.precept` | Computed fields (`->`), `positive`, `nonnegative`, `min`/`max`, `writable` |
| `customer-profile.precept` | Stateless precept, `is set` (GAP-3), `==` (GAP-17), `choice of string(...)` with `writable` |
| `event-registration.precept` | Multi-state, `enqueue`/`dequeue` (GAP-5/7), `from any` |
| `fee-schedule.precept` | `maxplaces` (GAP-22), `nonnegative`, `max`, stateless precept, `writable` |
| `invoice-line-item.precept` | Typed constants in expressions (GAP-1), `maxplaces`, computed fields |
| `it-helpdesk-ticket.precept` | Complex workflow, `remove` (GAP-6), ensures with guards |
| `library-book-checkout.precept` | Queue operations (GAP-7), `dequeue into` (GAP-5), `push`/`pop` |
| `library-hold-request.precept` | Queue operations, `enqueue` |
| `maintenance-work-order.precept` | Complex lifecycle, choice fields, multiple actions |
| `parcel-locker-pickup.precept` | Set operations, `contains` (GAP-4) |
| `payment-method.precept` | `min`/`max` modifiers (GAP-19), `optional`, `writable`, stateless precept |
| `refund-request.precept` | Complex rules, `is set` (GAP-3), guards |
| `restaurant-waitlist.precept` | Queue operations (GAP-7), `push`/`pop` (GAP-8), `from any` |
| `subscription-cancellation-retention.precept` | `~=` (GAP-15), complex rules |
| `sum-on-rhs-rule.precept` | Sum-on-RHS rules, `positive`, computed fields |
| `transitive-ordering.precept` | Rule transitivity, `positive`, computed fields, no states |
| `travel-reimbursement.precept` | Money type qualifiers, `in 'USD'`, typed constants |
| `utility-outage-report.precept` | Complex workflow, multiple modifiers |
| `vehicle-service-appointment.precept` | Date/time fields, temporal types |
| `warranty-repair-request.precept` | Ensures with guards, `is set`, choice fields |

---

## Recommendations
### R1 — Fix parser bugs before writing new tests (Priority 1 and 2 first)
GAP-2 (ensure+guard) and GAP-1 (TypedConstant atom) are parser bugs, not test gaps. Fix the production code first; then the tests become regression anchors. Fixing GAP-2 will immediately unblock the `insurance-claim.precept` and `loan-application.precept` integration tests from partial to full coverage.
### R2 — Add a sample-file integration theory to `ParserTests.cs`
Add a `[Theory][InlineData("filename.precept")]` test that loads each sample file, parses it, and asserts `diagnostics.Should().BeEmpty()`. This catches regressions without per-construct knowledge. Once GAP-1 and GAP-2 are fixed, all 28 sample files should pass this test. Use the existing pattern from Slice 5.3 (`Parse_SampleFile_HasNoParseErrors`).
### R3 — Add `ExpressionParserTests` batch for missing operators
All five missing comparison operators (GAP-17) and both case-insensitive operators (GAP-15) can be covered with a single `[Theory][InlineData(...)]` test. Similarly for `%` (GAP-16). Combine into `ParseExpr_BinaryOperator_Coverage_Theory` to avoid test sprawl.
### R4 — Add action statement tests for collection-mutating actions
`remove`, `enqueue`, `dequeue (into)`, `push`, `pop (into)` are five related gaps (GAP-5/6/7/8). One file — `ActionStatementParserTests.cs` — can cover all six action keywords with positive + negative tests (missing target, missing value, malformed `into` clause).
### R5 — Add value-bearing modifier tests to `SlotParserTests.cs`
`default`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces` (GAP-18–22) are all value-bearing modifiers with no dedicated parse tests. A `[Theory]` over the modifier keywords with representative expression forms would cover them efficiently.
### R6 — Add tests for remaining state modifier keywords
`required`, `irreversible`, `success`, `warning`, `error` (GAP-31) each need a `ParseStateModifierList_*` test in `SlotParserTests.cs`. These are low-effort: copy the `ParseStateModifierList_Terminal` pattern.
### R7 — Test `is set`, `contains`, list literals, and interpolated strings together
GAP-3, GAP-4, GAP-10, GAP-12 are all expression-layer features that can be added to `ExpressionParserTests.cs` without any production code fixes (except GAP-3 and GAP-10 may require parser support). Audit the Pratt parser's `ParseAtom()` and left-denotation table before writing tests — confirm which of these are already wired and just untested vs. which need production changes.
### R8 — Scope TypeChecker tests as a separate milestone
All TypeChecker-level validations (§3 of the spec) are blocked by `NotImplementedException`. Do not attempt to write TypeChecker tests until the implementation is underway. Track them separately — they are not a test-writing problem yet.

---

## Appendix: Test File Inventory

| File | Domain | Count |
|------|--------|------:|
| `ParserTests.cs` | Parser integration, declarations, slices 4–6 | ~200 tests |
| `ExpressionParserTests.cs` | Pratt parser, atoms, operators, precedence | ~35 tests |
| `SlotParserTests.cs` | Individual slot parsers: types, modifiers, guards | ~60 tests |
| `LexerTests.cs` | Token production, interpolation, typed constants | ~45 tests |
| `DiagnosticsTests.cs` | Catalog exhaustiveness, stage assignments, codes | ~20 tests |
| `ActionsTests.cs` | Action catalog metadata | ~15 tests |
| `OperatorsTests.cs` | Operator catalog, qualifier dispatch | ~40+ tests |
| `OperationsTests.cs` | Operation catalog, binary/unary DU | ~40+ tests |
| `TokensTests.cs` | Token catalog, categories, TextMate scopes | ~30 tests |
| `ModifiersTests.cs` | Modifier catalog exhaustiveness | ~20 tests |
| `ConstraintsTests.cs` | Constraint catalog | ~15 tests |
| `FunctionsTests.cs` | Function catalog | ~20 tests |
| `ConstructsTests.cs` | Construct catalog (declarations) | ~15 tests |
| `TypesTests.cs` | Type catalog | ~15 tests |
| `FaultsTests.cs` | Fault catalog | ~15 tests |
| `SyntaxReferenceTests.cs` | DSL reference string completeness | ~10 tests |
| `WritableSurfaceTests.cs` | `writable` modifier surface | ~5 tests |
| `ProofRequirementTests.cs` | ProofRequirement DU instances | ~15 tests |
| `ProofRequirementCatalogTests.cs` | ProofRequirements catalog | ~12 tests |
| `DescriptorsTests.cs` | Runtime descriptor records | ~15 tests |
| `GraphAnalyzerTests.cs` | Dependency graph analysis | ~? tests |
| `ProofEngineTests.cs` | Proof engine dispatch | ~? tests |
| `RuntimeTests.cs` | Runtime evaluation | ~? tests |

**Total baseline:** 2107 tests, 0 failing.

---

# Soup Nazi — Full Test Coverage Review: spike/Precept-V2

**Date:** 2025-07-14
**Reviewer:** Soup Nazi (Tester)
**Branch:** `spike/Precept-V2`
**Test runs:** `dotnet test test/Precept.Tests/ --no-build` + `dotnet test test/Precept.Analyzers.Tests/ --no-build`

---

## Test Run Results

| Suite | Passed | Failed | Skipped |
|-------|--------|--------|---------|
| `Precept.Tests` | 2424 | 0 | 0 |
| `Precept.Analyzers.Tests` | 254 | 0 | 0 |
| `Precept.LanguageServer.Tests` | 0 | 0 | 0 (empty — pre-existing) |
| `Precept.Mcp.Tests` | 0 | 0 | 0 (empty — pre-existing) |

No test failures. No skipped tests introduced by this branch.

---

## Skipped Tests

None. No `[Fact(Skip = ...)]` or `[Theory(Skip = ...)]` entries added in this branch.

---

## Missing Tests

**M1: [OperatorsTests] IsSet/IsNotSet Arity not asserted as Postfix**

`Arity.Postfix = 3` is a NEW enum value added in this branch. `GetMeta_UnaryOperators_HaveUnaryArity` and `GetMeta_BinaryOperators_HaveBinaryArity` pin those enum values in tests — but there's no `GetMeta_PostfixOperators_HavePostfixArity` equivalent for `OperatorKind.IsSet` and `OperatorKind.IsNotSet`. The parser dispatch logic depends on arity to tell prefix from postfix from binary. If `Postfix` is ever changed or a new IsSet-family operator gets the wrong arity, nothing catches it. Required:

```csharp
[Theory]
[InlineData(OperatorKind.IsSet)]
[InlineData(OperatorKind.IsNotSet)]
public void GetMeta_PostfixOperators_HavePostfixArity(OperatorKind kind)
    => Operators.GetMeta(kind).Arity.Should().Be(Arity.Postfix);
```

---

**M2: [OperatorsTests] IsSet/IsNotSet Tokens sequence not directly asserted**

The DU's core new data — the `Tokens` property — is never directly read in tests. `ByTokenSequence_IsSet_Resolves` and `ByTokenSequence_IsNotSet_Resolves` prove the lookup works, but they exercise the FrozenDictionary index, not the source `Tokens` list. If `IsSet.Tokens` were `[Is, Set, Set]` the lookup might still work but the shape would be wrong. Required:

```csharp
[Fact]
public void IsSet_Tokens_IsIsSet()
{
    var op = (MultiTokenOp)Operators.GetMeta(OperatorKind.IsSet);
    op.Tokens.Should().HaveCount(2);
    op.Tokens[0].Kind.Should().Be(TokenKind.Is);
    op.Tokens[1].Kind.Should().Be(TokenKind.Set);
}

[Fact]
public void IsNotSet_Tokens_IsIsNotSet()
{
    var op = (MultiTokenOp)Operators.GetMeta(OperatorKind.IsNotSet);
    op.Tokens.Should().HaveCount(3);
    op.Tokens[0].Kind.Should().Be(TokenKind.Is);
    op.Tokens[1].Kind.Should().Be(TokenKind.Not);
    op.Tokens[2].Kind.Should().Be(TokenKind.Set);
}
```

---

**M3: [ExpressionFormCoverageTests (root)] PostfixOperation missing from parse round-trip Theory**

`ParseExpression_ReturnsCorrectNodeTypeForForm` covers all 10 other `ExpressionFormKind` members (Literal×2, Identifier, Grouped, BinaryOperation, UnaryOperation, MemberAccess, ListLiteral via Theory; Conditional, FunctionCall, MethodCall as separate Facts), but `PostfixOperation` is absent. The mapping from form enum member → concrete AST node type is the whole point of that test — an incomplete Theory is incomplete coverage. `ExpressionParserTests.ParseExpression_IsSet` tests the parser behavior in isolation, but that's not the same as coverage in `ExpressionFormCoverageTests`, which is the exhaustiveness test. Required:

```csharp
[Fact]
public void ParseExpression_PostfixOperation_IsSet_ReturnsCorrectNodeType()
{
    var expr = ParseExpr("opt is set");
    expr.Should().BeOfType<IsSetExpression>(
        because: "parsing 'opt is set' must produce an IsSetExpression (PostfixOperation form)");
}
```

---

**M4: [Precept0020Tests] No PRECEPT0020 non-fire test for switch with MultiTokenOp arms**

The PRECEPT0020 analyzer comment says "MultiTokenOp arms are skipped." That invariant is unverified in tests. `GivenOperatorWithInlineToken_DoesNotCrash` (which is a SingleTokenOp with an inline token, not a MultiTokenOp) is often cited as the robustness test, but it does not exercise the MultiTokenOp skip path at all. If the `if (creation.Type?.Name != "SingleTokenOp") continue;` guard were removed, `GivenOperatorWithInlineToken_DoesNotCrash` would still pass (it would just try to extract a `Token` named arg from the MultiTokenOp constructor and return null). Required: a switch containing a `MultiTokenOp` arm that WOULD collide with a `SingleTokenOp` if Multi were not skipped — and assert zero PRECEPT0020 diagnostics.

```csharp
[Fact]
public async Task GivenSingleAndMultiTokenOpsWithSameLeadToken_NoPRECEPT0020()
{
    // MultiTokenOp [Or, Set] shares lead token Or with SingleTokenOp Or.
    // PRECEPT0020 must not fire — it skips MultiTokenOp arms (PRECEPT0023b handles that).
    var source = OperatorStubs + @"
    public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
    {
        OperatorKind.Or     => new SingleTokenOp(kind, Tokens.GetMeta(TokenKind.Or),   ""Or"",  Arity.Binary, Associativity.Left, 10, OperatorFamily.Logical),
        OperatorKind.Extra1 => new MultiTokenOp(kind, [Tokens.GetMeta(TokenKind.Or), Tokens.GetMeta(TokenKind.And)], ""OrAnd"", Arity.Binary, Associativity.Left, 20, OperatorFamily.Logical),
        _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
    };
" + CloseBrace;
    var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0020OperatorsTokenCollision>(source);
    diagnostics.Where(d => d.Id.StartsWith("PRECEPT0020")).Should().BeEmpty();
}
```

---

**M5: [ExpressionParserTests] No test for chained postfix presence operator behavior**

`IsSet` and `IsNotSet` are `NonAssociative`. The contains-chaining non-associativity test exists (Slice 18 in ExpressionParserTests). But there's no test for `x is set is set` or `x is set is not set`. What does the parser do? It should either emit a diagnostic (consistent with the binary non-associativity rule) or cleanly stop. Either behavior is valid but must be pinned. This is especially important because `is set` has a different parsing path (led via `PostfixOperation`) than binary operators, so the non-associativity guard may not fire the same way. Required: one test clarifying the behavior:

```csharp
[Fact]
public void ParseExpression_IsSet_Chained_BehaviorIsDefined()
{
    // "x is set is set" — behavior must be defined: either diagnostic or clean stop.
    // Pin whichever behavior the parser implements so regressions are caught.
    var tree = Parser.Parse(Lexer.Lex("rule x is set is set because \"test\""));
    // Assert the expected outcome here (error or no-error + truncated parse)
}
```

---

**M6: [OperatorsTests] IsSet/IsNotSet not pinned in Precedence_MatchesSpec**

`Precedence_MatchesSpec` is a Theory that pins every SingleTokenOp's precedence value. It covers all 18 SingleTokenOp entries. But `IsSet` (precedence 60) and `IsNotSet` (precedence 60) are `MultiTokenOp` entries and are absent from it. The precedence of postfix presence operators relative to arithmetic (60 > 50) and comparison (60 > 30) is a spec-level decision that should be regression-tested. Required:

```csharp
[Theory]
[InlineData(OperatorKind.IsSet,    60)]
[InlineData(OperatorKind.IsNotSet, 60)]
public void GetMeta_PostfixOperators_Precedence_MatchesSpec(OperatorKind kind, int expected)
    => Operators.GetMeta(kind).Precedence.Should().Be(expected);
```

---

## Passing Observations

**P1:** All 2424 `Precept.Tests` pass. All 254 `Precept.Analyzers.Tests` pass. Zero failures.

**P2:** No `[Fact(Skip)]` or `[Theory(Skip)]` entries added in this branch. Zero skips. Clean.

**P3:** PRECEPT0019 — 5 tests: 2 true-positives (class + struct with missing handlers), 3 true-negatives (all handled, multi-annotation, no class marker). Solid diagnostic boundary coverage.

**P4:** PRECEPT0020 — 5 tests: both 0020a (by-token collision) and 0020b (precedence collision) have TP and TN cases, plus a combined case. The inline-token crash guard is also covered.

**P5:** PRECEPT0021 — 4 tests: distinct texts, null text handled, two-arm duplicate, three-arm duplicate (two diagnostics). The null-text skip behavior is verified.

**P6:** PRECEPT0022 — 3 tests: all-catalog-reference baseline, single inline offender, multiple inline offenders. Good.

**P7:** PRECEPT0023 — 6 tests: valid multi-token baseline, 0023a with 1 token, 0023a with 0 tokens, 0023b single/multi lead collision, 0023c duplicate full sequence, and the critical real-catalog pattern (IsSet + IsNotSet share lead but different sequences → no 0023c). The last test proves the invariant doesn't fire on the actual catalog shape.

**P8:** GAP-A (when-guard on ensure): parse tests for `StateEnsure` and `EventEnsure` with and without guard. Regression anchors ensure the no-guard case still works. Sample file integration (insurance-claim, loan-application) confirms the fix holds in realistic precepts.

**P9:** GAP-B (modifiers after computed expressions): 5 tests covering single trailing modifier, multiple trailing, pre+post, pre-only regression. Pre-modifier-only regression confirms the fix didn't break the pre-modifier path.

**P10:** GAP-C (keywords as member/function names): `.min`, `.max` member access and `min()`, `max()` function call tests exist. Sample file integration confirms the fix in context. `TokenMetaMemberNameTests` pins the catalog-derived membership.

**P11:** Expression form additions: `is set`, `is not set`, list literals, method calls, typed constants, interpolated typed constants — all have happy-path parse tests with AST node type assertions.

**P12:** ExpressionFormKind catalog (11 members): count, GetMeta exhaustiveness, HoverDocs, IsLeftDenotation for all forms (Theory), Category for all forms (Theory), PostfixOperation special shape (3 standalone Facts). Comprehensive.

**P13:** Annotation bridge: `ExpressionFormCoverageTests` (root) uses reflection to verify `[HandlesCatalogExhaustively]` exists on 3 types and that all `[HandlesCatalogMember]` annotations collectively cover every `ExpressionFormKind`. This is the xUnit-level mirror of PRECEPT0019 for the ExpressionFormKind catalog specifically.

**P14:** `[HandlesForm] → [HandlesCatalogMember]` rename: the reflection-based `ExpressionFormCoverageTests` directly accesses `HandlesCatalogMemberAttribute` — if the rename were incomplete or the attribute class were missing, these tests would fail. Rename is de facto tested.

**P15:** `Precept.LanguageServer.Tests` and `Precept.Mcp.Tests` are empty (no test files). This is pre-existing — not a regression from this branch. Nothing new was added to them that needs testing.

---

## Verdict

```
VERDICT: BLOCKED — 6 missing tests, 0 skipped
```

M1 and M6 are required to close the OperatorsTests gap on the new `Arity.Postfix` enum value and the new MultiTokenOp precedence data. M3 makes `ExpressionFormCoverageTests` live up to its name. M4 protects the MultiTokenOp skip invariant that PRECEPT0020 relies on. M5 defines the behavior of chained postfix presence operators — right now it's undefined in tests. M2 directly pins the DU shape that the PRECEPT0023 analyzer is designed to protect.

All six are straightforward to add. No implementation changes needed. Write the tests, get them to green, and resubmit.

No soup for you.

# Calculated Field Arrow Direction: `<-` vs `->` Analysis







**Author:** Frank (Lead/Architect & Language Designer)



**Date:** 2026-04-27



**Status:** VERDICT — REJECT



**Requested by:** Shane







---







## 1. Current State
### How `->` is used today







The `->` token (`TokenKind.Arrow`) serves **two distinct grammatical roles** in the Precept DSL:







**Role A — Computed field expression introducer:**



```



field Tax as number nonnegative -> Subtotal * TaxRate



field Net as number positive -> Subtotal - Tax



field LineTotal as number -> TaxableAmount + TaxAmount nonnegative



```







Grammar production (spec §2.2, line 576):



```



field Identifier ("," Identifier)* as TypeRef FieldModifier* ("->" Expr)?



```







The `->` appears at the end of a field declaration, after modifiers, to introduce a computed expression. The field is read-only by contract: no `set`, no `edit`, no `writable`. The arrow says "this field's value is derived from this expression."







**Role B — Action chain / outcome separator:**



```



from Draft on Submit



    -> set ApplicantName = Submit.Applicant



    -> set RequestedAmount = Submit.Amount



    -> transition Approved



```







Grammar productions (spec §2.2, lines 626–628, 656–658, 665–667):



```



from StateTarget on Identifier ("when" BoolExpr)?



    ("->" ActionStatement)*



    "->" Outcome



```







The `->` here introduces each step in a transition pipeline — actions and the final outcome. The parser loops consuming `-> ActionKeyword` pairs, breaking when the token after `->` is an outcome keyword.







**Role C — State action (entry/exit hook):**



```



to Active -> set LastLogin = now()



from Expired -> clear Cache



```







The `->` introduces the action chain after a state target in `to`/`from` scoped constructs.
### How the lexer handles it







The lexer is catalog-driven. `->` lives in the `Tokens` catalog as:







- **TokenKind:** `Arrow` (line 145 of `TokenKind.cs`)



- **Text:** `"->"`



- **Category:** `TokenCategory.Structural` (not `Operator` — this is a deliberate classification; `Cat_Str`, line 328 of `Tokens.cs`)



- **TextMateScope:** `keyword.operator.arrow.precept`



- **SemanticTokenType:** `operator`







The lexer resolves `->` through the `TwoCharOperators` frozen dictionary (line 411 of `Tokens.cs`). The scan order is: try two-char operators first (`->` before `-`), then fall back to single-char operators (`-` as `Minus`). This is the maximal-munch guarantee documented in spec §1.5, line 209:







> `->` before `-`







The `TwoCharOperators` table is built generically from `Tokens.All` entries with length-2 text and `Operator` or `Structural` categories. The lexer's `TryScanOperator()` method (Lexer.cs lines 733–757) does a single `TwoCharOperatorStarters.Contains(c)` guard, then a `TwoCharOperators.TryGetValue((c, PeekNext), ...)` lookup. No special-case code for `->`.
### What complexity exists







**Disambiguation cost: zero at the lexer level.** The lexer emits `Arrow` tokens identically regardless of context. All disambiguation is in the parser:







- In a `field` declaration: `->` after modifiers means "computed expression follows."



- In a transition row: `->` at line start means "action or outcome follows."



- In a state action: `->` after state target means "action chain follows."







The parser knows which role `->` plays purely from its position in the grammar production — not from any lookahead past the arrow itself. The token after `->` tells the parser whether it's an action keyword (continue loop), outcome keyword (break loop), or expression start (computed field). This is straightforward recursive descent: the parser is already inside the right production when it encounters `->`.







**There is no ambiguity.** The two roles occupy different grammar productions that are entered through different leading tokens (`field` vs `from`/`to`/`on`). The parser never faces a choice between "is this `->` a computed expression arrow or an action chain arrow?" because by the time it sees `->`, it already knows which production it's in.







---







## 2. The `<-` Proposal







The proposal: use `<-` (left-pointing arrow) for computed field expressions instead of `->` (right-pointing arrow). The action chain would retain `->`.
### What would change







#### Lexer impact







A new token kind `LeftArrow` (or `ComputedArrow`) would be needed:







- **TokenKind.cs:** Add `LeftArrow` to the `Operators` section (line ~145).



- **Tokens.cs GetMeta:** Add a new entry: `TokenKind.LeftArrow => new(kind, "<-", Cat_Str, "Computed field expression", ...)`.



- **TwoCharOperators table:** Automatically picks up `('<', '-')` from the new catalog entry.







**Critical conflict: `<-` collides with `<` followed by `-` (unary negation).** The `TwoCharOperatorStarters` set already contains `<` (because of `<=`). Adding `<-` means the lexer would try to match `('<', '-')` as a `LeftArrow` token. But consider this existing legal expression:







```



rule Score < -5 because "..."



```







The lexer would see `<` followed by `-` and greedily consume `<-` as a `LeftArrow` token. This **breaks all existing programs** that use `< -` (less-than, followed by negative number or negation). The maximal-munch rule that currently works cleanly would now create a genuine ambiguity that requires context-sensitive lexing.







Possible mitigations:



1. **Require whitespace between `<` and `-` when they're separate tokens.** This changes the lexer from context-free to whitespace-sensitive in operator scanning. The current lexer has zero whitespace sensitivity in operator scanning — this would be a first.



2. **Don't use the maximal-munch table; add special-case code.** This breaks the catalog-driven scan model. Every other two-char operator goes through the generic `TwoCharOperators` lookup. `<-` would need a carve-out.



3. **Abandon `<-` as a two-char operator and parse it as two tokens (`<` + `-`).** This means the parser must compose two separate tokens into a computed-arrow concept. That's a regression in token-level clarity.







None of these are clean. The current system has **zero special-case code** in operator scanning.







#### Parser impact







If the lexer successfully emits `LeftArrow` tokens:







- Field declaration production changes from `("->" Expr)?` to `("<-" Expr)?`.



- The parser checks for `TokenKind.LeftArrow` instead of `TokenKind.Arrow` when parsing field tails.



- Action chains continue checking for `TokenKind.Arrow`.







The parse grammar change is trivial — a single token kind swap. No disambiguation change, because `->` and `<-` never competed in the same parse context anyway.







#### Type checker impact







None. The type checker operates on the AST, not tokens. Whether the tree says "computed expression" regardless of which arrow introduced it, the semantic analysis is identical.







#### Catalog impact







- `TokenKind.cs`: Add `LeftArrow` (1 enum member, update count).



- `Tokens.cs`: Add `GetMeta` entry for `LeftArrow`.



- The `TwoCharOperators` table auto-derives from `All`.



- The `Operators` catalog is **not affected** — `->` is not an expression-level operator (it's `Structural` category), and `<-` wouldn't be either.



- Spec §1.1 operator table: add `LeftArrow` / `<-` row.



- Spec §1.5 scan priority: add `<-` scan rule and document the `< -` conflict.







#### Grammar generation impact







The TextMate grammar generator reads catalog metadata and emits `tmLanguage.json`. A new `LeftArrow` token with `keyword.operator.arrow.precept` scope would be auto-picked up. Completions and hover would derive from the catalog entry. Impact is minimal — one new derived entry — **assuming the lexer conflict is solved.**







---







## 3. Compiler Simplification Assessment
### Does `<-` reduce parser lookahead requirements?







**No.** The current `->` requires zero lookahead to disambiguate between its two roles. The parser is already inside the correct production (field declaration vs. transition row) before encountering `->`. There is no lookahead cost to reduce.
### Does it eliminate any ambiguity that `->` currently creates?







**No.** There is no ambiguity to eliminate. `->` is unambiguous in both parse contexts because the contexts are entered through different leading tokens (`field` vs `from`/`to`/`on`). The parser never needs to decide "which kind of arrow is this?"
### Does it make the parse grammar more regular or less regular?







**Less regular.** Currently `->` is the universal "pipeline step" glyph — it means "what follows derives from / is produced by what precedes." Using `<-` for one role and `->` for the other introduces a directional split that the grammar must track. The parser now has two arrow token kinds where one sufficed.







More concretely: the scan-priority rule becomes harder. The current rule is clean — `->` before `-`. Adding `<-` requires `<-` before `<` and a resolution for the `<` + `-` collision. The scan priority table gains a new conflict pair.
### Does it affect operator overloading or multi-use disambiguation?







**Yes — negatively.** The `<` character is currently a starter for three tokens: `<=`, `<`, and (hypothetically) `<-`. The `-` character is currently a starter for two tokens: `->` and `-`. With `<-`, both characters become starters for three tokens each, and the `<-` vs `< -` collision is a genuine new ambiguity. The current system has **zero** collisions between its two-char operators and their single-char prefixes in isolation.
### Net assessment







**`<-` adds complexity.** It introduces a lexer conflict that doesn't exist today, requires either whitespace sensitivity or special-case scanning to resolve, and provides zero disambiguation benefit at the parser level because the parser never had an ambiguity to begin with.







---







## 4. Language Surface Assessment
### Does `<-` read better or worse than `->`?







Compare the two:







```







---







---







---

# Design Session Round 1: Catalog-Driven Parser Full Vision







**By:** Frank



**Date:** 2026-04-27



**Status:** Round 1 complete — awaiting George's challenge (Round 2)







## What This Is







Round 1 of a 3-round design session requested by Shane. The prior analysis walked back Layer D (slot-driven productions) and rejected Layer C (disambiguation metadata). Shane explicitly rejected those walkbacks and asked for a full-vision design with no compromise.







## Key Decisions in This Round







1. **`DisambiguationEntry` replaces `LeadingToken` on `ConstructMeta`.** The single `LeadingToken` field cannot express constructs with multiple leading tokens (`StateEnsure` has 3, `AccessMode` has 2 with different disambiguation per entry). The new `Entries: ImmutableArray<DisambiguationEntry>` field carries per-leading-token disambiguation metadata.







2. **Generic disambiguation replaces 4 hand-written methods.** `ParseDisambiguated` handles the `when` guard uniformly, then matches disambiguation tokens from catalog metadata. Zero per-construct disambiguation code.







3. **Generic slot iteration drives all 11 constructs.** `ParseConstructSlots` reads `meta.Slots` and dispatches to slot parsers via a `FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>>`. No per-construct parse methods.







4. **Node factory dictionary instead of exhaustive switch.** Trades CS8509 compile-time enforcement for runtime testability via factory completeness tests. Flagged for George's challenge.







5. **Source generation rejected at current scale.** Design is generator-ready but 11 constructs don't justify the infrastructure investment.







## Design Artifact







`docs/working/catalog-parser-design-v1.md` — full design with C# sketches, catalog changes, migration path, and questions for George.







## What Round 2 Should Challenge







See `## For George` section in the design doc. Key areas: `Entries` replacing `LeadingToken` (breaking catalog change), `when` guard uniformity assumption, slot parser `SyntaxNode?` return type fragility, factory dictionary vs. switch, anchor/guard injection coupling, and clean-slate re-estimate.







---







---







---

# Decision: Catalog-Driven Parser Design — Round 3 Resolutions







**By:** Frank



**Date:** 2026-04-27



**Status:** Design decisions locked pending Shane review







## Context







Round 3 of the catalog-driven parser design collaboration. George (Round 2) found two bugs in the v1 design and flagged six decisions for Frank's disposition. Shane added a new question about language extensibility and generic AST options.







## Decisions Made
### George's Six Flagged Items







1. **F1 (LeadingTokenSlot): ACCEPTED.** `LeadingTokenSlot: ConstructSlotKind?` on `DisambiguationEntry` correctly handles the `write all` bug where the leading token doubles as slot content.







2. **F2 (BuildNode shape): GEORGE WINS — exhaustive switch.** CS8509 compile-time enforcement is the correct invariant shape for BuildNode. `_slotParsers` stays as dictionary (registry pattern). Split by purpose, not unified.







3. **F3 (ActionChain peek-before-consume): ACCEPTED.** Verified against all three outcome forms and the no-action case. Fix is correct and complete.







4. **F4 (Two-position when guard): ACCEPTED.** Both pre-disambiguation and post-EventTarget guard positions are valid Precept syntax. The generic disambiguator handles both uniformly. Spec must document both.







5. **F5 (DisambiguationTokens derivation): REJECTED.** Routing and grammar are separable concerns. Declare disambiguation tokens explicitly. No `IntroductionTokens` field on ConstructSlot.







6. **F6 (Migration PR sequence): ACCEPTED with bridge property.** Catalog shape change in PR 1 with `PrimaryLeadingToken` backward-compatible bridge. Parser work in subsequent PRs. Bridge removed when last consumer migrates.
### Extensibility Analysis Outcomes







- Generic AST (ConstructNode with Slots[]): **REJECTED.** Catastrophic type-safety loss for TypeChecker and Evaluator.



- AST-as-catalog-tree: **REJECTED.** Confuses syntax with semantics.



- Source-generated AST nodes: **DEFERRED.** Break-even at ~25-30 constructs. Design is generator-ready.



- Irreducible per-construct code identified: ConstructKind, GetMeta, AST node, BuildNode arm, TypeChecker rules, Evaluator semantics.







## Artifacts







- `docs/working/catalog-parser-design-v3.md` — supersedes v1 and v2 as the living design document.







---







## Summary







Audited all 32 files in `docs/` for references to field access modes, field modifiers, token vocabulary, grammar, diagnostics, and runtime enforcement. 7 files required updates; 25 required no change.







---







## Design Confirmed (Locked)







1. **Two-layer access mode model:**



   - Layer 1: field-level `writable` modifier sets baseline (`write` default across states)



   - Layer 2: `in <State> write|read|omit` overrides per-(field, state) pair



   - State-level ALWAYS wins over field-level for a specific pair



   - Fields without `writable` default to `read` in all states (D3 baseline preserved)







2. **Root-level `write <Field>` eliminated.** Use `writable` modifier on the field declaration instead.







3. **Root-level `write all` preserved** as sugar for stateless precepts — marks all non-computed fields writable.







4. **`writable` on computed field** → existing `ComputedFieldNotWritable` diagnostic fires.







5. **`writable` on event arg** → new `WritableOnEventArg` diagnostic (compile-time only; no runtime backstop path).







6. **`TokenKind.Writable`**: `Text = "writable"`, category `Declaration`, `ValidAfter = VA_FieldModifier`.







7. **`ModifierKind.Writable`**: `ModifierShape.Flag`, `FieldModifierMeta` subtype. Count: 14 → 15.







---







## Files Updated







| File | Changes |



|------|---------|



| `docs/language/precept-language-spec.md` | §1.1 token vocabulary, §1.2 keywords, §2.2 grammar/composition rules, §2.4 modifiers, §3.8 validation, §3.10 diagnostics |



| `docs/archive/language-design/precept-language-vision.md` | Editability form table, declaration keywords, Field Access Modes section, composition rules, parser/typechecker responsibilities (archived) |



| `docs/compiler/parser.md` | Flag modifiers list (added `writable`), dispatch note (write all only), AccessMode grammar node |



| `docs/compiler/type-checker.md` | Processing model — `writable` modifier validation and `WritableOnEventArg` |



| `docs/compiler/diagnostic-system.md` | `WritableOnEventArg` added to `DiagnosticCode` enum and exhaustive switch |



| `docs/language/catalog-system.md` | `Writable` in TokenKind enum, token count 90+ → 91+, `FieldModifierMeta` 14 → 15 members |



| `docs/runtime/evaluator.md` | Access-Mode Enforcement note updated — resolved mode from two-layer composition |







## Files Confirmed No Change







All `docs/working/` files (historical records — must not be updated per audit policy), `docs/philosophy.md`, lexer, graph-analyzer, proof-engine, literal-system, tooling-surface, precept-builder, fault-system, result-types, primitive-types, temporal-type-system, business-domain-types, extension, mcp (stub), language-server (stub), and all READMEs.







**Key rationale for result-types.md:** The runtime `FieldAccessMode { Read, Write }` enum represents the *resolved* per-(field, state) mode after both layers are applied. Correct as-is — `writable` is a compile-time declaration modifier; its resolution into runtime access mode happens in the Precept Builder.







---







## Open Questions / Escalations







None. All decisions locked and documented above.







---







---







---

# Frank — `writable` Field Modifier Review







**Date:** 2026-04-27



**Branch:** `precept-architecture`



**Commits reviewed:** 28535e4 (catalog + docs), 54672c8 (samples + tests)



**Verdict:** BLOCKED







---







## Verdict: BLOCKED







One blocker. Three minor doc defects. Everything else is well-executed. Fix B1, M1, and M2 and this clears.







---







## B1 — `Constructs.AccessMode.LeadingToken` incorrectly changed to `TokenKind.In`



**Severity:** Blocker



**File:** `src/Precept/Language/Constructs.cs`, line 107







`AccessMode.LeadingToken` was changed from `TokenKind.Write` to `TokenKind.In`. This is wrong and the impact is non-trivial: `Parser.cs` is a stub throwing `NotImplementedException`. The real parser has not been written yet. `parser.md` line 138 states: "The dispatch table is a direct map from `ConstructMeta.LeadingToken` to parse methods." A Parser.cs implementer following the catalog will build:







- `In → ParseAccessMode()` — **wrong**



- `In` at the top level must route to `ParseInScoped()` for preposition disambiguation (StateEnsure, AccessMode state-scoped form, StateAction all share `In` as leading token). Direct `In → ParseAccessMode()` would skip that disambiguation entirely.



- The `Write → ParseAccessMode()` path for `write all` (parser.md line 130) has no catalog entry to back it — it would be a dangling dispatch table entry with no corresponding `LeadingToken`.







The correct `LeadingToken` for `AccessMode` is `TokenKind.Write`. This is the one token that maps DIRECTLY to `ParseAccessMode()` at the top level. The state-scoped form (`in State write|read|omit`) enters `AccessMode` indirectly through `ParseInScoped()` — it is a secondary production routed by the preposition disambiguation method, not a first-class `LeadingToken` dispatch.







The `UsageExample = "in Draft write Amount"` and the description are accurate and should be kept. The `LeadingToken` field alone is wrong.







**Required fix:** `TokenKind.In` → `TokenKind.Write` in `Constructs.cs` `AccessMode` entry.







---







## M1 — Stale `edit` terminology in spec §1.1 token table



**Severity:** Minor



**File:** `docs/language/precept-language-spec.md`, lines 47 and 111







Two entries in the §1.1 token vocabulary table carry v1 `edit` terminology:







- Line 47: `| In | in | State-scoped ensure/edit (in State ensure ...) |` — "edit" is a v1 keyword removed in v2 (§1.2 explicitly states this). Should reference write/read/omit.



- Line 111: `| All | all | Universal quantifier / edit all |` — "edit all" is the v1 form. The v2 form is `write all`.







§1.2 says "`edit` is not reserved in v2. `write` replaces `edit`." Having the token table say "edit all" is a direct contradiction in the same document.







**Required fix:**



- Line 47: `State-scoped ensure/edit (in State ensure ...)` → `State-scoped ensure/write/read/omit scope preposition`



- Line 111: `Universal quantifier / edit all` → `Universal quantifier / write all (stateless precepts), read all / omit all (state-scoped)`







---







## M2 — catalog-system.md field modifier count comment stale



**Severity:** Minor



**File:** `docs/language/catalog-system.md`, line 740







The code sample comment reads `// ── Field modifiers (14) ─────────────────────────────────`. There are now 15 field modifiers. The summary table at line 716 correctly says 15. The `ModifierKind.cs` group comment correctly says `field (15)`. The inline doc comment is the only laggard.







**Required fix:** `(14)` → `(15)` on line 740.







---







## Good Findings







**G1 — Token group placement correct.** `TokenKind.Writable` lands in the Declaration group between `Optional` and `Because`. Consistent with `Optional` as a field-declaration modifier; Declaration group is the right category.







**G2 — `VA_FieldModifier` bidirectional setup correct.** `TokenKind.Writable` appears in the `VA_FieldModifier` array (other modifiers can precede `writable`) and `writable`'s own `ValidAfter: VA_FieldModifier` (other modifiers can follow `writable`). Position-agnostic within the modifier list. Samples confirm both orderings work: `positive writable`, `optional writable`.







**G3 — `FieldModifierMeta(AnyType)` split is architecturally correct.** `ApplicableTo = AnyType` (empty) is right because computed-field exclusion is a semantic rule (field has a `->` expression), not a type-compatibility rule. `ApplicableTo` encodes type-based restrictions; the computed-field restriction belongs to the type checker. The test comment explicitly documents this: "computed-field restriction is enforced by the type checker, not the modifier catalog." Clean separation.







**G4 — `WritableOnEventArg` diagnostic is correctly placed and specified.** Stage=Type, Severity=Error, Category=Structure, positioned after `CircularComputedField` and before `ConflictingAccessModes` in the enum. The message "The 'writable' modifier cannot appear on event argument '{0}'" is accurate. The fix hint "Remove 'writable' — event arguments are always read-only within the transition body" is precise and actionable.







**G5 — Sample migration is clean.** All 6 migrated samples (`computed-tax-net`, `fee-schedule`, `invoice-line-item`, `payment-method`, `sum-on-rhs-rule`, `transitive-ordering`) are stateless precepts. `writable` appears only on non-computed fields in every case. All 22 other samples are untouched. State-scoped `in State write` forms are preserved across the full sample set. `customer-profile.precept:write all` is correctly untouched. No stale `write <FieldName>` patterns remain.







**G6 — D3 guarantee correctly preserved.** Fields without `writable` default to `read`. The language spec §2.2 "D3 default" rule (composition rule #2) is accurate. The evaluator.md correctly describes the two-layer composition model being pre-resolved at Precept Builder time, with the evaluator reading descriptors, not re-computing access modes.







**G7 — Two-layer composition model documentation is a significant improvement.** The Layer 1 / Layer 2 model in §2.2 replaces the previous flat "read-by-default" description with a structured account of how field baselines and state overrides compose. The nine composition rules are complete, ordered correctly (baseline → D3 default → state override → guarded write → omit clear → set restriction → conflicts → computed → event arg), and accurate.







**G8 — All 1783 tests pass.** No regressions.







**G9 — Language spec completeness is thorough.** `writable` appears in §1.1 (Declaration keyword table), §1.2 (reserved keyword set), §2.2 (access mode grammar with full two-layer model), §2.4 (field modifiers table), §3.8 (modifier validation table), §3.10 (diagnostic table with `WritableOnEventArg`). No section missing.







**G10 — MCP gap is pre-existing, not introduced by this change.** `LanguageTool.cs` does not exist — MCP only has `PingTool.cs`. `writable` will flow through automatically when `LanguageTool` is implemented from the catalog. No gap introduced here.







---







## Minor Observation (Not a Blocker)







**`ComputedFieldNotWritable.RelatedCodes` does not include `WritableOnEventArg`.** Both diagnose misuse of `writable`. This is a weak cross-navigation gap in the LS diagnostic experience. Not a blocker — the two codes diagnose distinct surfaces (computed fields vs. event args) — but worth considering when the LS diagnostic UI ships.







---







## Fixes Required Before Re-Review







1. `src/Precept/Language/Constructs.cs` — `TokenKind.In` → `TokenKind.Write` on the `AccessMode` `LeadingToken` argument. Description and example may stay as-is.



2. `docs/language/precept-language-spec.md` line 47 — remove "edit", add write/read/omit.



3. `docs/language/precept-language-spec.md` line 111 — `edit all` → `write all`.



4. `docs/language/catalog-system.md` line 740 — `(14)` → `(15)`.







Items 2–4 are doc-only and may land in the same commit as item 1.







---







---







---

# Decision Note: Catalog-Driven Parser Design Round 2







**By:** George



**Date:** 2026-04-27



**Related doc:** `docs/working/catalog-parser-design-v2.md`



**Status:** Pending Frank's Round 3 response







---







## Context







Shane asked for the full vision of a catalog-driven parser with no compromise. Frank wrote Round 1



(`docs/working/catalog-parser-design-v1.md`). This note records George's Round 2 findings for



the decisions log.







---







## Findings Requiring Team Decision
### Finding G1 — Two Implementation Bugs in Frank's v1 (Both Are Real, Both Need Fixes)







**Bug 1 (ActionChain/Outcome boundary):** Frank's `ParseActionChain()` consumes `->` then breaks



when an outcome keyword follows, leaving the outcome keyword as the current token. `ParseOutcome()`



then expects `->` first and returns null. This fires on every `TransitionRow` with actions — the



`Outcome` required slot gets `ExpectedSlot` diagnostic on valid input.







Fix: `ParseActionChain()` must peek at `Peek(1)` before consuming `->` when an outcome keyword



follows. Leave `->` for `ParseOutcome()` to consume as its introduction token.







**Bug 2 (AccessMode `Write`-leading path):** `write all` dispatches with `Write` consumed as the



leading token. The generic slot iterator then calls `_slotParsers[AccessModeKeyword]()` but current



token is now the field target (`all`). The `Write` token is gone.







Fix: Add `LeadingTokenSlot: ConstructSlotKind?` to `DisambiguationEntry`. When set, the parser



injects a synthetic node from the already-consumed leading token into that slot rather than parsing



fresh.







**Disposition needed:** Frank should confirm both fixes are accepted before any implementation work



begins on the parser.







---
### Finding G2 — `DisambiguationEntry` Is a Breaking Catalog Shape Change







`ConstructMeta.LeadingToken` must become `Entries: ImmutableArray<DisambiguationEntry>`. This



breaks:



- LS completions that read `ConstructMeta.LeadingToken`



- MCP vocabulary output that uses it



- Any tests referencing it







Mitigation: add `PrimaryLeadingToken` computed property = `Entries[0].LeadingToken` as



backward-compat bridge. Consumers migrate to `Entries` incrementally.







**The "no migration" claim in Frank's v1 is correct for the parser stub, not for catalog consumers.**



The catalog PR must be sequenced before parser implementation begins.







---
### Finding G3 — Implicit Contract Must Become Explicit







The generic disambiguator leaves disambiguation tokens unconsumed. This only works if each



slot parser consumes its own introduction token as its first action. This is an invariant that



holds for all current slot parsers but is undocumented. If a future slot parser omits this,



the stream is corrupted silently.







Mitigation: document the contract in the `_slotParsers` dictionary code comment and in the



parser design doc. This is not a blocker, but it must be done before implementation.







---
### Finding G4 — Pre-Disambiguation `when` Guard Position in TransitionRow







The `when` guard can appear at two syntactic positions in TransitionRow:



1. Before the `on` disambiguation token (`from X when expr on Y → ...`)



2. After the event target (`from X on Y when expr → ...`, the standard form)







Frank's generic disambiguator handles both correctly: pre-disambiguation guards are injected



into slot[2] after disambiguation; post-EventTarget guards are parsed naturally during slot



iteration. Both produce the same `TransitionRowNode.Guard` field.







**Decision needed from Frank:** Is `from X when expr on Y → ...` (pre-disambiguation guard)



actually valid Precept syntax? The parser.md tables list `When` as a re-check option for all



preposition methods, but no samples demonstrate this form. If it is NOT valid, the disambiguator



should either skip the guard-consumption step or constrain it to constructs that declare



a `GuardClause` slot at the construct level.







---







## Revised Estimate







| Option | Scope | Estimate |



|--------|-------|---------|



| Option 1 (A+B+C+E only) | Catalog + vocabulary tables + disambiguation metadata + sync set | 1 week |



| Option 3 (A+B+D+E, Frank's full vision) | Full catalog-driven parser | **3–3.5 weeks** (was 2.5–3; +18h correctness hardening) |







The corrections do not change the architectural recommendation — they harden it. Option 3 is



still viable on a clean-slate build. The bugs are correctness issues that exist regardless of



which option is chosen once parser implementation begins.







---







## No Code Before These Are Resolved







Per George's charter: no implementation work until Frank's sign-off on the design. These bugs



are discovered in design review — the right time. The parser is still a stub. These are zero-cost



fixes at design time.







---







---







---

# Soup Nazi — Writable Coverage Review



**Date:** 2026-04-27



**Reviewer:** Soup Nazi (Tester)



**Scope:** `writable` field modifier — test coverage audit



**Test run:** 1783 tests, 0 failed, 0 skipped ✅







---







## Verdict: BLOCKED







Two blockers. No soup until they are fixed.







---







## Blockers
### B1: `AccessMode.LeadingToken` change is untested







`Constructs.AccessMode` had its `LeadingToken` changed from `TokenKind.Write` to `TokenKind.In`. No test anywhere in `ConstructsTests.cs` asserts `LeadingToken` on any construct — the field is completely invisible to the test suite.







If someone reverts this to `Write` (or any other value), no test will catch it.







**Required fix:** Add a test to `ConstructsTests.cs`:







```csharp



[Theory]



[InlineData(ConstructKind.AccessMode,       TokenKind.In)]



[InlineData(ConstructKind.StateEnsure,      TokenKind.In)]



[InlineData(ConstructKind.StateAction,      TokenKind.To)]



[InlineData(ConstructKind.EventEnsure,      TokenKind.Ensure)]



[InlineData(ConstructKind.FieldDeclaration, TokenKind.Field)]



[InlineData(ConstructKind.StateDeclaration, TokenKind.State)]



[InlineData(ConstructKind.EventDeclaration, TokenKind.Event)]



public void LeadingToken_IsCorrect(ConstructKind kind, TokenKind expectedToken)



{



    Constructs.GetMeta(kind).LeadingToken.Should().Be(expectedToken,



        $"{kind} must begin with {expectedToken}");



}



```







`AccessMode → In` is the regression anchor for this change. The rest are bonus coverage.







---
### B2: `WritableOnEventArg` missing from `DiagnosticsTests.TypeCodes` static list







`TypeCodes` in `DiagnosticsTests.cs` (line 153) is a hardcoded TheoryData used by `TypeStageCodes_AllHaveTypeStage`. It includes `ComputedFieldNotWritable` but does **not** include `WritableOnEventArg`. The two diagnostic codes were added in the same implementation — one is present, one is absent.







The three dynamic `Create_*` theories (using `AllDiagnosticCodes()`) DO exercise `WritableOnEventArg` and would catch a missing `GetMeta` entry or factory crash. But `TypeStageCodes_AllHaveTypeStage` will not catch a future stage miscategorization for `WritableOnEventArg`.







**Required fix:** Add `DiagnosticCode.WritableOnEventArg` to the `TypeCodes` TheoryData in `DiagnosticsTests.cs`:







```csharp



// near ComputedFieldNotWritable



DiagnosticCode.WritableOnEventArg,



```







---







## Good Observations







**G1: ModifiersTests — 7 new/updated tests are correct.**



Count invariants updated (28→29 total, 14→15 field, 25→26 structural), `Writable_AppliesToAnyType`, `Writable_IsStructuralFlag`, `Writable_TokenTextIsWritable`, and `FlagModifiers_HasValueIsFalse` theory updated — all well-formed. The "empty = any type" semantics are correctly documented in the assertion message.







**G2: Dynamic exhaustiveness net is solid.**



`GetMeta_ReturnsForEveryModifierKind`, `All_ContainsEveryKindExactlyOnce`, and the three `Create_*` theories all use `Enum.GetValues<>()` — new entries are covered without code changes. `WritableOnEventArg` is not orphaned; it passes the Create factory tests today.







**G3: TokensTests uses dynamic count — no hardcoded token count to update.**



`All_ContainsExactlyAsManyEntries_AsEnumValues` derives its expected count from the enum. `AllKeywords_HaveTextMateScope` and `AllKeywords_HaveSemanticTokenType` cover `TokenKind.Writable` automatically (it is `Cat_Decl`, which is included in both token-property checks). No action needed.







**G4: Sample files are clean.**



All 6 migrated samples place `writable` only on non-computed fields:



- `computed-tax-net.precept`: Subtotal, TaxRate writable; Tax, Net (computed) — no `writable`. ✅



- `fee-schedule.precept`: BaseFee, DiscountPercent, MinimumCharge writable; TaxRate, CurrencyCode locked. ✅



- `invoice-line-item.precept`: Description, UnitPrice, Quantity, DiscountPercent writable; Subtotal through LineTotal (all computed) — no `writable`. ✅



- `sum-on-rhs-rule.precept`: Total, Tax, Fee writable; Net (computed) — no `writable`. ✅



- `transitive-ordering.precept`: High, Mid, Low writable; Spread (computed) — no `writable`. ✅



- `payment-method.precept`: IsDefault, Nickname writable; no computed fields. ✅







**G5: Hover description coverage is implicit.**



`GetMeta_ReturnsForEveryModifierKind` asserts `Description.NotBeNullOrEmpty` for every `ModifierKind` — `Writable` is covered without a dedicated test.







---







## Deferred Tests (Parser/TypeChecker not yet implemented)







These tests MUST exist before the implementation is marked complete. They are not optional.







| ID | Test | Trigger |



|----|------|---------|



| D1 | `field X as money writable` → zero diagnostics | Parser + TypeChecker |



| D2 | `field X as money` (no modifier) → field is read-only baseline | TypeChecker semantic model |



| D3 | Computed field + `writable` → `ComputedFieldNotWritable` | TypeChecker |



| D4 | Event arg + `writable` → `WritableOnEventArg` | TypeChecker |



| D5 | Field `writable` baseline + `in State write|read|omit` → correct composed mode | TypeChecker + evaluator |



| D6 | State-scoped `in State write Field` still works (regression) | TypeChecker |



| D7 | Stateless `write all` still works (regression) | TypeChecker |



| D8 | `writable` on computed field that also has `default` → both `ComputedFieldNotWritable` and `ComputedFieldWithDefault` fire | TypeChecker |







---







## Summary







Fix B1 and B2, then resubmit. The catalog-level work is solid. The sample migration is correct. The blocking gaps are small and surgical: one new `[Theory]` in `ConstructsTests.cs` and one line added to `DiagnosticsTests.TypeCodes`.







No soup until then.







---







## Verdict







**NEEDS MORE TESTS**







Two blockers. The catalog work is solid. The lexer surface is now covered. The gaps are surgical and documented below with exact fixes.







---







## Test Coverage Assessment
### What's covered







| Area | Test | Status |



|------|------|--------|



| `Writable_AppliesToAnyType` — `ApplicableTo.Should().BeEmpty()` | `ModifiersTests.cs` | ✅ |



| `Writable_IsStructuralFlag` — `Category == Structural`, `HasValue == false` | `ModifiersTests.cs` | ✅ |



| `Writable_TokenTextIsWritable` — `Token.Text == "writable"`, `Token.Kind == Writable` | `ModifiersTests.cs` | ✅ |



| `FlagModifiers_HasValueIsFalse` includes `Writable` | `ModifiersTests.cs` | ✅ |



| Count invariants updated: 29 total, 15 field, 26 structural | `ModifiersTests.cs` | ✅ |



| `GetMeta_ReturnsForEveryModifierKind` covers `Writable` via enum exhaustion | `ModifiersTests.cs` | ✅ |



| `AllFieldModifiers_AreStructural` covers `Writable` structurally | `ModifiersTests.cs` | ✅ |



| `TokenKind.Writable` in `Keywords`, TextMateScope, SemanticTokenType (via exhaustiveness) | `TokensTests.cs` | ✅ |



| `Create_*` factory theories cover `WritableOnEventArg` via `Enum.GetValues<DiagnosticCode>()` | `DiagnosticsTests.cs` | ✅ |



| `WritableOnEventArg` meta not-null, severity + stage returned by `Create` | `DiagnosticsTests.cs` | ✅ (via AllDiagnosticCodes dynamic) |



| Lexer emits `Writable` token after type keywords (all 5 surface cases) | `WritableSurfaceTests.cs` | ✅ (added during investigation) |



| `in Draft write Amount` emits `Write` not `Writable` (correct distinction) | `WritableSurfaceTests.cs` | ✅ |



| `write all` preserved — lexes as `Write + All` | `WritableSurfaceTests.cs` | ✅ |



| Root-level `write Amount` — lexer doesn't reject (Parser's job) | `WritableSurfaceTests.cs` | ✅ |
### What is NOT covered — blockers







See Findings section.







---







## Findings
### [GAP] WritableOnEventArg Missing from TypeCodes Stage Group







**Severity:** Major







**File:** `test/Precept.Tests/DiagnosticsTests.cs`, `TypeCodes` member data (~line 153)







**Finding:** `DiagnosticsTests.TypeCodes` is the hardcoded list used by `TypeStageCodes_AllHaveTypeStage`. It includes `DiagnosticCode.ComputedFieldNotWritable` (added in the same PR) but does **not** include `DiagnosticCode.WritableOnEventArg` (also added in the same PR). The two codes were introduced together; one made it into the stage-group list, one did not.







The three dynamic `Create_*` theories iterate `Enum.GetValues<DiagnosticCode>()` and DO exercise `WritableOnEventArg` — the `GetMeta` entry exists, the factory doesn't crash, and the severity/stage round-trips correctly via the generic path. But `TypeStageCodes_AllHaveTypeStage` will not catch a future miscategorization (e.g., accidentally setting `DiagnosticStage.Parse` instead of `DiagnosticStage.Type`).







Additionally, no severity spot-check exists for `WritableOnEventArg` the way one exists for `DivisionByZero_HasErrorSeverity`. This is a minor but real gap — it's `Severity.Error` and that contract should be pinned.







**Required action:**







1. Add `DiagnosticCode.WritableOnEventArg` to `TypeCodes` in `DiagnosticsTests.cs` between `CircularComputedField` and `ConflictingAccessModes`:







```csharp



// existing entries



DiagnosticCode.ComputedFieldNotWritable,



DiagnosticCode.ComputedFieldWithDefault,



DiagnosticCode.CircularComputedField,



DiagnosticCode.WritableOnEventArg,   // ← ADD THIS



DiagnosticCode.ConflictingAccessModes,



```







2. Add a severity spot-check:







```csharp



[Fact]



public void WritableOnEventArg_HasErrorSeverity()



{



    Diagnostics.GetMeta(DiagnosticCode.WritableOnEventArg).Severity.Should().Be(Severity.Error);



}



```







---
### [GAP] AccessMode.LeadingToken Change Has No Regression Test







**Severity:** Major







**File:** `test/Precept.Tests/ConstructsTests.cs`







**Finding:** `ConstructKind.AccessMode.LeadingToken` was changed from `TokenKind.Write` to `TokenKind.In` as part of this PR. This is a behavioral change to the catalog's public contract — `LeadingToken` drives LS completions, MCP vocabulary output, and semantic token classification. No test in `ConstructsTests.cs` asserts `LeadingToken` on any construct — the property is completely invisible to the test suite. A regression back to `TokenKind.Write` would not be caught by any test.







The `GetMeta_ReturnsForEveryConstructKind` exhaustiveness test checks `Kind`, `Name`, `Description`, and `UsageExample`. It does not check `LeadingToken`.







**Required action:** Add a `[Theory]` to `ConstructsTests.cs` pinning `LeadingToken` for key constructs. `AccessMode → In` is the regression anchor for this PR change; the others are bonus coverage that should also have been tested:







```csharp



[Theory]



[InlineData(ConstructKind.AccessMode,       TokenKind.In)]



[InlineData(ConstructKind.StateEnsure,      TokenKind.In)]



[InlineData(ConstructKind.StateAction,      TokenKind.To)]



[InlineData(ConstructKind.EventEnsure,      TokenKind.On)]



[InlineData(ConstructKind.FieldDeclaration, TokenKind.Field)]



[InlineData(ConstructKind.StateDeclaration, TokenKind.State)]



[InlineData(ConstructKind.EventDeclaration, TokenKind.Event)]



[InlineData(ConstructKind.TransitionRow,    TokenKind.From)]



[InlineData(ConstructKind.PreceptHeader,    TokenKind.Precept)]



public void LeadingToken_IsCorrect(ConstructKind kind, TokenKind expectedToken)



{



    Constructs.GetMeta(kind).LeadingToken.Should().Be(expectedToken,



        $"{kind} must begin with {expectedToken}");



}



```







---
### [CONFIRMED] ModifiersTests — 4 New Writable Tests Are Correct







**Severity:** N/A (confirmed)







**File:** `test/Precept.Tests/ModifiersTests.cs`







**Finding:** All 4 new Writable tests are well-formed and assert the right catalog properties:



- `Writable_AppliesToAnyType` — asserts empty `ApplicableTo` with the correct semantic comment ("empty = applies to all types; computed-field restriction is enforced by the type checker")



- `Writable_IsStructuralFlag` — asserts `Category == Structural` and `HasValue == false`



- `Writable_TokenTextIsWritable` — asserts `Token.Text == "writable"` and `Token.Kind == TokenKind.Writable`



- `FlagModifiers_HasValueIsFalse` updated to include `ModifierKind.Writable`







Count invariants (29 total / 15 field / 26 structural) are correct.







**Required action:** None.







---
### [CONFIRMED] TokensTests — TokenKind.Writable Covered by Exhaustiveness







**Severity:** N/A (confirmed)







**File:** `test/Precept.Tests/TokensTests.cs`







**Finding:** No direct spot-check test for `TokenKind.Writable` exists in `TokensTests.cs`. However, the existing exhaustiveness tests adequately cover it:



- `GetMeta_ReturnsWithoutThrowing_ForEveryTokenKind` — runs over every `TokenKind` including `Writable`



- `All_ContainsExactlyAsManyEntries_AsEnumValues` — count-invariant catches missing entries



- `AllKeywords_HaveTextMateScope` — `Writable` has `Cat_Decl` and non-null text, so it's included



- `AllKeywords_HaveSemanticTokenType` — same reason



- `Keywords_ContainsAllKeywordCategoryTokensWithNonNullText` — `Writable` will be in both `expectedKeys` and `Keywords.Keys`







The indirect coverage via `Writable_TokenTextIsWritable` in `ModifiersTests.cs` also pins the token text. No spot-check gap that needs to be filled.







**Required action:** None. Pre-existing pattern; `ValidAfter` membership is not tested for any token — that's a broader gap outside this PR's scope.







---
### [CONFIRMED] No Old `write Field` Syntax in Test Data







**Severity:** N/A (confirmed)







**File:** All `test/Precept.Tests/*.cs`







**Finding:** Grep for `write\s+\w` across all test files found zero matches in test data strings. The only occurrence is in a comment string in `ConstructsTests.cs` ("root-level write"). No regression from eliminated `write Field` syntax exists in the catalog test suite.







**Required action:** None. Note: the eliminated syntax is not rejected at lex time (lexer is context-free; `write Amount` emits `Write + Identifier` without error). Parser-level rejection must be tested once Parser is implemented.







---
### [CONFIRMED] MCP Regression — Lexer Correctly Handles writable







**Severity:** N/A (confirmed)







**Finding:** MCP server is live (`precept_ping` = ok). All lexer-surface probes pass:







| Probe | Result |



|-------|--------|



| `field Amount as money writable` | `Writable` token emitted after `MoneyType` ✅ |



| `field Amount as money` (no modifier) | No `Writable` token emitted ✅ |



| `write all` on stateless precept | `Write + All` tokens; no `Writable` ✅ |



| `in Draft write Amount` | `In + Write` tokens; `Writable` token absent (correct: `write` is the access-mode keyword, `writable` is the field modifier) ✅ |



| `write Amount` (eliminated syntax) | Lexes as `Write + Identifier`; no lex diagnostic. Rejection is Parser/TypeChecker work ✅ |







All compile paths uniformly throw `NotImplementedException` at `Parser.Parse()` — consistent with the known stub state.







**Required action:** None for current state.







---
### [CONFIRMED] WritableSurfaceTests.cs Created During Investigation







**Severity:** N/A (informational)







**File:** `test/Precept.Tests/WritableSurfaceTests.cs` (new, created during investigation)







**Finding:** 10 new tests were created during the MCP regression phase. They cover:



- 5 `*_LexesCorrectly` tests — verify token stream shapes for each writable surface case



- 5 `*_CompileThrowsNotImplemented` tests — anchor the current stub state







**Caution:** The `CompileThrowsNotImplemented` tests are asserting stub behavior. When Parser is implemented, they will turn red. That is correct and honest — they will be visible failures requiring update. They should NOT be deleted or skipped before the implementation lands; they should be converted to positive-case assertions at that time.







All 10 new tests pass. Total count is now 1793.







**Required action:** None. Keep the file. Update `*_CompileThrowsNotImplemented` tests when Parser is implemented.







---







## Deferred Tests (Parser/TypeChecker stubs — required before implementation is complete)







These tests MUST exist before the `writable` implementation is marked done. Red is acceptable. Skip is not.







| ID | Test | Gate |



|----|------|------|



| D1 | `field X as money writable` → compiles clean, zero diagnostics | Parser + TypeChecker |



| D2 | `field X as money` (no modifier) → field is read-only baseline | TypeChecker semantic model |



| D3 | Computed field + `writable` → `ComputedFieldNotWritable` diagnostic | TypeChecker |



| D4 | Event arg + `writable` → `WritableOnEventArg` diagnostic | TypeChecker |



| D5 | `writable` baseline + `in State write Field` override → correct composed access mode | TypeChecker + evaluator |



| D6 | `in State write Field` still works on non-writable field (regression) | TypeChecker |



| D7 | Stateless `write all` still works (regression) | TypeChecker |



| D8 | `writable` on computed field with `default` → both `ComputedFieldNotWritable` + `ComputedFieldWithDefault` fire | TypeChecker |







---







## Summary







The catalog-level work is solid. The `Writable` modifier entry in `Modifiers.cs` is correct and well-tested. The 1793 tests pass cleanly.







**Fix these two gaps, then resubmit:**







1. **`WritableOnEventArg` → add to `TypeCodes`** in `DiagnosticsTests.cs` + add `WritableOnEventArg_HasErrorSeverity` spot-check.



2. **`AccessMode.LeadingToken → In`** → add `LeadingToken_IsCorrect` theory to `ConstructsTests.cs`.







Both fixes are one-liners or near-one-liners. No soup until then.







---







---







---
