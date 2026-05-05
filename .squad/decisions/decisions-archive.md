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

---

### 2026-04-27: User directive — greenfield parser, no consumer concerns

**By:** Shane (via Copilot)

**What:** The parser is being written from scratch. The stub throws NotImplementedException. There are zero existing consumers of the parser output. Concerns about "breaking existing consumers" or "sequenced PR plans to protect consumers" are invalid in this context. Read the actual code before raising compatibility concerns.

**Why:** User correction — George raised a backward-compatibility concern that does not apply to a not-yet-implemented component.

### 2026-04-27: Design question — calculated field operator

**By:** Shane (via Copilot)

**What:** Would switching calculated fields from -> to <- help the catalog-driven parser design? Specifically: does having -> serve dual duty (transition arrow + computed field assignment) create token ambiguity that complicates slot catalog design? Would <- as a dedicated "computed from" operator simplify things?

**Why:** Shane raised this mid-design-iteration — fold into the parser design loop.

---
### Read the docs — language vision and language design

**By:** Shane (via Copilot)

**What:** Before producing any design output, ALL agents MUST read the relevant documentation — especially language vision and language design docs. Do not design in a vacuum. Do not assume. Read first.

**Why:** Agents have been designing without grounding in the existing language vision and language design documentation. This produces proposals that may contradict or ignore already-decided direction.

**Specific docs to read (minimum):**

- docs/philosophy.md

- docs/archive/language-design/precept-language-vision.md (archived)

- docs/language/precept-language-spec.md

- docs/language/catalog-system.md

- Any other docs/language/* files

- Sample files in samples/ to see actual syntax in use

**Rule:** If a design decision contradicts an existing doc, call that tension out explicitly rather than silently overriding it.

---
### Roslyn source generators for test generation

**By:** Shane (via Copilot)

**What:** Agents working on the catalog-driven parser design MUST consider Roslyn source generators as part of the solution — specifically for test generation. If the catalog describes constructs, slots, and grammar, a source generator could emit test scaffolding (or even test cases themselves) directly from catalog metadata, keeping tests in sync with language surface changes automatically.

**Why:** A truly catalog-driven design means the catalog is the single source of truth — test coverage should derive from it, not be hand-maintained alongside it. Source generators close that loop.

**Questions to address in design:**

- Could a Roslyn generator read ConstructCatalog/ConstructMeta and emit parser test stubs?

- Could it emit round-trip parse tests (text -> AST -> text) per construct automatically?

- How does this interact with the generic AST proposal — if AST nodes are generic/generated, do tests follow?

- What is the boundary between generated test scaffolding and hand-authored test logic?

---

---

---

# Current (->)

field Tax as number nonnegative -> Subtotal * TaxRate

field Net as number positive -> Subtotal - Tax

---

---

---

# Proposed (<-)

field Tax as number nonnegative <- Subtotal * TaxRate

field Net as number positive <- Subtotal - Tax

```

Reading left to right: `field Tax as number nonnegative -> Subtotal * TaxRate` reads as "field Tax, a nonnegative number, **derived as** Subtotal times TaxRate." The arrow flows in the reading direction. The declaration on the left, the derivation on the right, and the arrow connects them in reading order.

With `<-`: "field Tax, a nonnegative number, **receives from** Subtotal times TaxRate." The arrow points backwards — from right to left, against reading direction. The semantic is "the expression on the right flows into the name on the left," which is assignment semantics (like Erlang's `X = 5` or R's `x <- 5`).

This is a philosophical misfit. Precept's `->` isn't assignment — it's derivation declaration. The field doesn't *receive* a value; the field *is defined as* the expression. The right-pointing arrow says "this is what you get" — a definition, not an imperative assignment. The left-pointing arrow imports imperative assignment semantics from general-purpose programming languages, which is precisely the direction Precept's philosophy says not to go.
### Philosophy filter

| Criterion | `->` | `<-` |

|-----------|------|------|

| Domain integrity vs. enforcement-later | Neutral | Neutral |

| Deterministic and inspectable | ✓ Both | ✓ Both |

| Keyword-anchored, flat statements | ✓ Preserved | ✓ Preserved |

| First-match routing / collect-all validation | N/A | N/A |

| AI legibility | ✓ Consistent arrow glyph across all pipeline contexts | ✗ Two arrows, need context to know which is which |

| Configuration vs. scripting | ✓ Declarative derivation feel | ✗ Assignment feel (imperative) |

| Power without hidden behavior | ✓ Same glyph for "this step produces" everywhere | ✗ Different glyph for same "produces" concept in field vs. transition |

**AI legibility specifically:** LLMs reading Precept files benefit from having one universal "produces" operator. A single arrow glyph in two grammatical contexts (field derivation and action pipeline) is less cognitive surface than two glyphs that must be mapped to their respective contexts. Models that have seen `->` in action chains will correctly predict it in computed fields. Introducing `<-` creates a second pattern to learn with no semantic payoff.
### Convention alignment

| Language/System | Arrow | Meaning |

|----------------|-------|---------|

| Haskell | `->` | Function type / pattern match result |

| Erlang | `->` | Clause body |

| R | `<-` | Assignment (imperative) |

| Elm | `->` | Function type / case branch result |

| OCaml | `->` | Pattern match / function type |

| PostgreSQL | `GENERATED ALWAYS AS (expr)` | Keyword-based, no arrow |

| Kotlin | `get() =` | Property getter |

| C# | `=>` | Expression body |

The `<-` convention is associated almost exclusively with **imperative assignment** (R, some reactive frameworks). The `->` convention is associated with **derivation, pattern matching, and "produces"** semantics. Precept's computed fields are derivations, not assignments. `->` is the correct convention.

---

## 5. Verdict: REJECT
### What's wrong with the proposal

1. **The compiler simplification claim is false.** There is no ambiguity in the current `->` usage that `<-` would resolve. The parser never needs to disambiguate between computed-field `->` and action-chain `->` because these are in different grammar productions entered through different leading tokens. The disambiguation cost is zero today.

2. **`<-` introduces a real lexer conflict.** The character pair `< -` currently and correctly scans as two separate tokens (`LessThan`, `Minus`). Adding `<-` as a two-char token breaks maximal-munch for expressions like `Score < -5`. Resolving this requires whitespace sensitivity or special-case scanning — both of which are regressions from the current zero-special-case operator scanner.

3. **`<-` carries wrong semantics.** The left-pointing arrow evokes assignment ("value flows into variable"), which is an imperative concept. Precept's computed fields are declarative derivations ("field is defined as expression"). The right-pointing arrow correctly conveys "this produces" / "this derives as." This is not a style preference — it's a semantic alignment question that affects how readers (human and AI) interpret the construct.

4. **It breaks the universal pipeline glyph.** `->` currently serves as a consistent "what follows is produced" glyph in both field derivations and action chains. Splitting this into two directional arrows doubles the visual vocabulary without adding expressiveness. The consistency of a single arrow glyph is a feature, not a coincidence — it was a deliberate design choice (spec §2.2, line 644: "The `->` arrow is deliberately overloaded to create a visual pipeline that reads top-to-bottom").
### Tradeoff accepted

We accept that `->` is a multi-use token (field derivation + action chain), which means the parser must know its context to interpret it. This is a trivially cheap disambiguation — the parser is already inside the correct production — and the readability benefit of a single universal "produces" glyph outweighs the theoretical elegance of one-token-one-meaning.

---

## 6. Alternatives Considered
### `=>` (fat arrow)

Used in C#, JavaScript, TypeScript for expression bodies. Would avoid the `< -` collision. However:

- Introduces a third arrow-like glyph alongside `->` and `=`.

- `=>` is strongly associated with lambda/function bodies in mainstream languages. Precept doesn't have lambdas. Importing the glyph without the semantics creates false familiarity.

- Doesn't improve over `->` in any dimension that matters.

**Verdict:** No advantage.
### `=` (plain equals)

```

field Tax as number nonnegative = Subtotal * TaxRate

```

This is how spreadsheets and some config languages work. Problems:

- `=` is already `TokenKind.Assign` — used in `set Balance = 0` action statements. Adding a second role creates a real ambiguity: in `field X as number = 5`, is `5` a computed expression or a default value? Defaults use `default 5`, so there's a syntactic distinction, but the visual similarity is a readability trap.

- `=` in an expression context evokes imperative assignment even more strongly than `<-`.

**Verdict:** Worse than the status quo.
### Keyword-based (`computed`, `derives`, `defined as`)

```

field Tax as number nonnegative computed Subtotal * TaxRate

field Tax as number nonnegative derives Subtotal * TaxRate

```

This follows the PostgreSQL `GENERATED ALWAYS AS` model. However:

- Precept's field declaration line is already dense: `field Name as Type Modifier* -> Expr`. Adding a multi-word keyword increases line width without adding clarity over `->`.

- A keyword would need to be reserved, increasing the keyword table and reducing the identifier namespace.

- The `->` glyph is compact and visually distinct — it immediately signals "derivation follows" without consuming identifier-like tokens.

**Verdict:** Inferior to `->` for Precept's compact line-oriented grammar.
### Status quo (`->`)

The right-pointing arrow:

- Has zero lexer conflicts.

- Requires zero parser disambiguation (context is always unambiguous from the enclosing production).

- Reads as "derives as" / "produces" — correct semantics for both computed fields and action chains.

- Is consistent with functional/DSL precedent for "this produces that."

- Is already implemented, documented, tested, and understood.

**Verdict:** The correct choice. No change needed.

---

## Source References

| Artifact | Location | Relevance |

|----------|----------|-----------|

| `TokenKind.Arrow` | `src/Precept/Language/TokenKind.cs:145` | Token enum definition |

| Arrow catalog entry | `src/Precept/Language/Tokens.cs:328–329` | `Cat_Str` category, `keyword.operator.arrow.precept` scope |

| `TwoCharOperators` table | `src/Precept/Language/Tokens.cs:411–415` | Generic two-char scan table derivation |

| `TryScanOperator()` | `src/Precept/Pipeline/Lexer.cs:733–757` | Maximal-munch scan — zero special cases |

| `LessThan` token | `src/Precept/Language/Tokens.cs:314` | `<` is single-char operator, `<` is also in `TwoCharOperatorStarters` via `<=` |

| `Minus` token | `src/Precept/Language/Tokens.cs:320–321` | `-` is single-char operator, `-` is also in `TwoCharOperatorStarters` via `->` |

| Field declaration grammar | `docs/language/precept-language-spec.md:576` | `("->" Expr)?` at end of field production |

| Transition row grammar | `docs/language/precept-language-spec.md:626–628` | `("->" ActionStatement)* "->" Outcome` |

| Deliberate overload rationale | `docs/language/precept-language-spec.md:644` | "deliberately overloaded to create a visual pipeline" |

| Scan order spec | `docs/language/precept-language-spec.md:209` | `->` before `-` in maximal-munch priority |

| Computed field research | `research/language/expressiveness/computed-fields.md` | Read-only derivation contract, precedent survey |

| `computed-tax-net.precept` | `samples/computed-tax-net.precept:10–11` | Canonical computed field usage |

| `invoice-line-item.precept` | `samples/invoice-line-item.precept:16–20` | Multi-step computed field chains |

| `travel-reimbursement.precept` | `samples/travel-reimbursement.precept:14` | Computed field with modifier interleaving |

---

---

---

# Decision: Access-mode shorthand grammar and AST split

**Date:** 2026-04-28

**By:** Shane (owner) with Frank and George follow-through

**Status:** Locked

## Decision

- Access declarations use `modify` plus the adjectives `readonly` or `editable`; structural exclusion uses `omit`.

- `modify` and `omit` share the same `FieldTarget` shapes: a single field, a comma-separated field list, or `all`.

- The locked grammar is:

```precept

in State modify Field readonly [when Guard]

in State modify Field editable [when Guard]

in State modify F1, F2, ... readonly|editable [when Guard]

in State modify all readonly|editable [when Guard]

in State omit Field

in State omit F1, F2, ...

in State omit all

```

- Guards are permitted only on `modify`; `omit` is never guardable.

- Syntax and catalog shapes stay split: `AccessModeDeclaration` and `OmitDeclaration` are separate AST node kinds, not a unified access-declaration node.

## Why

- Shane explicitly directed the team to preserve comma-separated field shorthand and the `all` shorthand in the new `modify` surface.

- He also extended that same shorthand to `omit`, because both verbs operate over the same `FieldTarget` domain.

- Separate AST nodes preserve the real semantic difference: `omit` changes structural presence, while `modify` declares an access level and optionally carries a guard.

## Follow-through

- Frank completed the live-doc sweep across the language spec, language vision, parser design, parser reference, catalog-system doc, runtime API doc, evaluator doc, and the design-round record so the published grammar is consistent everywhere.

- George's vocabulary-migration implementation remains the code baseline for `modify` / `readonly` / `editable` / `omit`; any earlier sample simplifications that split comma-separated targets are superseded by this shorthand-preservation directive.

---

---
### 2026-04-28 — v8 parser design document authored

**By:** Frank

**Status:** Complete

**Decision type:** Design document

**Decisions captured in v8:**

- OmitDeclaration is a separate construct from AccessMode with its own DisambiguationEntry `[new(TokenKind.In, [TokenKind.Omit])]`, separate AST node (2 slots only), and separate disambiguation routing.

- AccessMode DisambiguationEntry narrowed from `[Modify, Omit]` (v7) to `[Modify]` only (v8).

- FieldTargetNode is a discriminated union: abstract base + SingularFieldTarget, ListFieldTarget, AllFieldTarget sealed subtypes.

- ByLeadingToken[In] dispatches to 3 constructs (was 2 in v7): StateEnsure, AccessMode, OmitDeclaration.

- Total ConstructKinds is 12 (was 11 in v7). BuildNode switch has 12 arms.

- v7's `InScoped_RoutesToAccessMode_WhenOmitFollowsState` test was incorrect and is replaced by `InScoped_RoutesToOmitDeclaration_WhenOmitFollowsState`.

- Proposal C (when as StateAction disambiguation token) is DEFERRED — not incorporated without Shane's approval.

**Artifacts produced:**

- `docs/working/catalog-parser-design-v8.md` — primary design document, supersedes v7

- `docs/working/v8-design-session-notes.md` — change summary and decision verification matrix

---

---

v8 fixes applied per George's review: 4 targeted edits (omit guard diagnostic, stashed guard behavior, sync clarification, 2.1 split formalized). Verdict expected: APPROVED.

---

---

v8 approved after fix verification — proceed to Phase 2.

---

---

---

# George's Review of catalog-parser-design-v8.md

**By:** George (Runtime Dev)

**Date:** 2026-04-28

**Reviewing:** `docs/working/catalog-parser-design-v8.md`

---

## George's Review of catalog-parser-design-v8.md

**Verdict:** BLOCKED

---
### Check A — OmitDeclaration Split

**PASS**: All four items confirmed.

- `AccessMode` entry: `[new(TokenKind.In, [TokenKind.Modify])]` ✓ (§4 critical entries table, line 322)

- `OmitDeclaration` entry: `[new(TokenKind.In, [TokenKind.Omit])]` ✓ (§4 critical entries table, line 323)

- `ByLeadingToken[In]` theory uses `InlineData(TokenKind.In, 3)` ✓ (Slice 1.5 test spec)

- v7's wrong test `InScoped_RoutesToAccessMode_WhenOmitFollowsState` explicitly replaced with `InScoped_RoutesToOmitDeclaration_WhenOmitFollowsState` ✓ (§ Slice 4.4, with full corrected test body shown)

---
### Check B — omit guard prohibition

**FAIL**: Structural slot is correct but diagnostic test coverage is incomplete.

- `OmitDeclaration` has NO `GuardClause` slot: ✓ confirmed (`[SlotStateTarget, SlotFieldTarget]` — 2 slots, no guard; §1 Slot Sequences, §3 `OmitDeclarationNode`)

- Test for `in State omit Field when Guard` → diagnostic: **MISSING**

`ParseOmit_NeverHasGuard` (Slice 4.4) checks only that "result node has NO Guard property at all (structural impossibility)." This verifies the happy-path AST shape, but does NOT assert that parsing `in State omit Field when Guard` emits a diagnostic. Those are two different behaviors — one tests correct output, the other tests incorrect input detection.

**Additionally**, the stashed-guard + OmitDeclaration case is unaddressed: `in State when Guard omit Field` — the generic disambiguator (Slice 4.1 step 3) pre-consumes an optional `when` guard before peeking the disambiguation token. If it stashes a guard and routes to `OmitDeclaration`, Slice 4.2 step 4 says "Inject stashed guard into GuardClause slot index (if present)." There is no GuardClause slot in `OmitDeclaration`. The spec says "if present" which implies silent discard — but this is a permanently-locked language invariant. A stashed guard being silently discarded when routed to `OmitDeclaration` is a diagnostic gap that needs to be explicitly specified and tested.

**Two concrete fixes needed:**

1. Add `ParseOmit_WithGuard_PostField_EmitsDiagnostic` test spec: `in Closed omit Amount when Active` → diagnostic.

2. Specify behavior in Slice 4.2 when stashed guard cannot be injected because routed construct has no GuardClause slot. Either: (a) emit a named diagnostic, or (b) explicitly document "silent discard" as acceptable. Pick one and add a test.

---
### Check C — FieldTargetNode DU

**PASS**: Correct abstract base + 3 sealed subtypes, specified with full C# signatures in §3.

- `abstract record FieldTargetNode(SourceSpan Span) : SyntaxNode(Span)` ✓

- `sealed record SingularFieldTarget(SourceSpan Span, Token Name)` ✓

- `sealed record ListFieldTarget(SourceSpan Span, ImmutableArray<Token> Names)` ✓

- `sealed record AllFieldTarget(SourceSpan Span, Token AllToken)` ✓

- `ParseFieldTarget()` spec returns correct DU subtype based on token shape ✓ (Slice 4.3)

---
### Check D — comma-separated shorthand

**PASS**: All 9 grammar forms explicitly enumerated in §2. Test coverage:

- 6 `modify` forms tested individually (Singular×2, List×2, All×2 for readonly/editable) ✓

- 3 `omit` forms tested individually (Singular, List, All) ✓

- Comma-separated list tests: `ParseAccessMode_ListReadonly`, `ParseAccessMode_ListEditable`, `ParseOmit_List` ✓

- `all` tests: `ParseAccessMode_AllReadonly`, `ParseAccessMode_AllEditable`, `ParseOmit_All` ✓

---
### Check E — sync tokens

**PASS** (with implementation clarity concern): §1 correctly documents `modify` and `omit` as recovery anchors within `in`-scoped parse failures, and `ErrorSync_SyncSetIncludesModifyAndOmit` test is present.

However, the `SyncToNextDeclaration()` implementation shown in Slice 5.4 only loops on `Constructs.LeadingTokens`:

```csharp

if (Constructs.LeadingTokens.Contains(Current().Kind))

    return;

```

`modify` and `omit` are NOT in `LeadingTokens` — they are disambiguation tokens that appear AFTER `in`. The spec says they "serve as recovery anchors within `in`-scoped parse failures" but the shown sync loop has no path to check them. The implementation mechanism is not shown: is it a secondary check in the disambiguator itself? A supplementary token set passed to sync? This is implementable but under-specified. Frank should document the concrete mechanism or accept that this test verifies disambiguator-level recovery, not `SyncToNextDeclaration()`.

This doesn't block the design (it's a real limitation of the shown code snippet) but the test name `ErrorSync_SyncSetIncludesModifyAndOmit` will be misleading if modify/omit are handled by the disambiguator rather than by the sync function. **Recommend clarifying the mechanism in Slice 5.4.**

---
### Check F — Proposal C

**PASS**: §8 explicitly marks Proposal C (`when` as `StateAction` disambiguation token) as DEFERRED/OPEN, "explicitly NOT incorporated in v8." Shane has not approved it. ✓

---
### Slice Sizing

**Slice 1.4** (~140 lines): FITS. Full `GetMeta()` rewrite for 12 constructs is a big switch body but structurally uniform. No split needed — each case is roughly the same pattern. As long as the implementer keeps local slot-index constants per arm, this is manageable in one context window.

**Slice 2.1** (~220 lines): **SPLIT NEEDED.** 220 lines across 15+ files in a new directory with base types + DU + 12 concrete nodes is over the comfort threshold. Frank already proposed the right split:

- **2.1a** (~80 lines): Base types (`SyntaxNode`, `Declaration`, `Expression`, shared nodes), plus `FieldTargetNode` DU (abstract + 3 sealed subtypes). The DU needs to be visible before anything references it.

- **2.1b** (~140 lines): All 12 concrete `Declaration` subtypes.

This split respects the dependency: `AccessModeNode` and `OmitDeclarationNode` reference `FieldTargetNode`, so 2.1a must complete before 2.1b.

**Slice 3.1** (~100–150 lines): FITS. Pratt parser is the most intellectually dense piece, but 100–150 lines for the full implementation is realistic for a well-scoped method. The vocabulary dictionary is already wired in Slice 2.2. No split needed.

**No other slices are oversized** — Slice 3.2 is ~120 lines but spread across 9 simple parsing methods with uniform patterns (peek, consume, return). That's fine.

---
### Feasibility Issues

1. **Slice 4.2: stashed guard + OmitDeclaration (BLOCKING)**: The generic disambiguator pre-consumes a `when` guard before routing. For `OmitDeclaration`, there is no GuardClause slot to inject into. The spec says "if present" (silent discard) but this is a permanently-locked invariant violation that should produce a diagnostic. The behavior must be explicitly specified before implementation — a developer cannot make this decision unilaterally. This ties directly to Check B item 2.

2. **Slice 5.4: modify/omit recovery mechanism (implementation clarity)**: `SyncToNextDeclaration()` as written loops only on `Constructs.LeadingTokens`. The spec claims `modify` and `omit` serve as recovery anchors within `in`-scoped failures, but the implementation path is not shown. Is this a secondary check in `ParseInScopedConstruct()` itself? Or does `SyncToNextDeclaration()` accept supplementary tokens? Needs one more sentence of spec. Not blocking on its own, but the test `ErrorSync_SyncSetIncludesModifyAndOmit` will be hard to implement without it.

3. **Slice 2.1 split**: Frank's suggested 2.1a/2.1b split is correct but not formally part of the plan — it's described as a "consider" option. Given 220 lines, I'm recommending it be formalized. Not blocking but should be resolved before sprint starts.

---
### Test Spec Gaps

1. **(BLOCKING) `in State omit Field when Guard` → diagnostic not tested**: No Soup Nazi spec anywhere tests that a post-field `when` on an omit declaration emits a diagnostic. `ParseOmit_NeverHasGuard` only tests structural shape. Need: `ParseOmit_WithPostFieldGuard_EmitsDiagnostic`.

2. **(BLOCKING) `in State when Guard omit Field` → stashed guard + no injection slot**: The pre-field guard + omit routing path has no specified behavior and no test. Need: `ParseOmit_WithPreFieldGuard_EmitsDiagnosticAndDiscards` (or equivalent — but the behavior must be decided first per Feasibility Issue #1).

3. **(Minor) Slice 4.2 tests don't cover the OmitDeclaration injection path**: `GuardInjection_StashedGuard_LandsInCorrectSlot` only tests `StateEnsure`. A companion test covering OmitDeclaration (no injection slot path) would prevent silent discard from being accidentally removed.

4. **(Minor) Disambiguation routing tests are type-assertion based** ✓ (e.g., `BeOfType<OmitDeclarationNode>()`). No gap here — this is correct.

---
### BuildNode Completeness

**PASS**: 12 arms confirmed, with explicit `OmitDeclaration` arm shown:

```csharp

ConstructKind.OmitDeclaration => new OmitDeclarationNode(span,

    (StateTargetNode)slots[0]!, (FieldTargetNode)slots[1]!),

```

Total arm count is 12, matching total `ConstructKind` count. Wildcard `_` arm with `ArgumentOutOfRangeException` present ✓.

---
### Summary

v8 is substantially correct — the OmitDeclaration split is clean, the FieldTargetNode DU is architecturally sound, disambiguation entries are properly separated, and test coverage for all 9 grammar forms is solid. Two blocking gaps: (1) there is no diagnostic test for guard-attempted-on-omit (the invariant is stated but not verified at the parse-input level), and (2) the behavior when a pre-consumed `when` guard is stashed and then routed to OmitDeclaration (which has no GuardClause slot) is completely unspecified — this is an implementation corner case that a developer cannot resolve independently.

---

## Items Frank Must Fix Before Phase 2

1. **Add `ParseOmit_WithPostFieldGuard_EmitsDiagnostic` to Slice 4.4 Soup Nazi spec**: Parse `"in Closed omit Amount when Active"` → assert diagnostic emitted (specify the diagnostic code — either a new `OmitDoesNotSupportGuard` or reuse an existing code).

2. **Specify Slice 4.2 behavior when stashed guard + OmitDeclaration (no GuardClause slot)**: Document whether the stashed guard is (a) silently discarded with a diagnostic, or (b) silently discarded without a diagnostic. Then add a corresponding Soup Nazi test: `ParseOmit_WithPreFieldGuard_EmitsDiagnosticAndParses` or `ParseOmit_WithPreFieldGuard_DiscardsGuardSilently` (but option (a) is strongly preferred — this is an invariant violation).

3. **(Recommended, not strictly blocking) Clarify Slice 5.4 sync mechanism**: Add one sentence explaining HOW `modify` and `omit` serve as within-`in`-block recovery anchors. The shown `SyncToNextDeclaration()` loop doesn't include them; the actual mechanism (secondary check in the disambiguator? supplementary token set?) needs to be named.

4. **(Recommended) Formalize Slice 2.1a/2.1b split**: Change the "consider" language to a firm split. The 220-line single-slice is above threshold.

---

---
### 2026-04-28 — Phase 2 decisions audit complete

**By:** Frank

**Status:** Complete

**Decision type:** Audit

**Category of fixes:** Documentation dispatch tables and AST sections in `docs/compiler/parser.md` and `docs/language/precept-language-spec.md` still treated `OmitDeclaration` as part of the `AccessMode` construct. 9 targeted edits applied to align both files with the locked decisions: `OmitDeclaration` is a separate `ConstructKind` with its own disambiguation entry, AST node, and 2-slot sequence (no guard). Source catalog files were already correct.

**Audit artifact:** `docs/working/audit-decisions-notes.md`

---

---
### 2026-04-28T23:04:41Z: User directive — spike mode constraints

**By:** Shane (via Copilot)

**What:** While on the `precept-architecture` spike branch, no new branches and no PRs are to be created. All commits go directly to `precept-architecture`. Agents must never run `git checkout -b` or `gh pr create` during a spike session.

**Why:** User request — spike branches are exploratory; PRs and sub-branches add noise and process overhead that doesn't belong in a spike.

---

---

---

# Deep Re-Review: Catalog Extensibility CS8509 Enforcement

**Reviewer:** Frank (Lead/Architect)

**Date:** 2026-04-28

**Branch:** `feature/catalog-extensibility`

**Prior verdict:** APPROVED (frank-george-deviation-review.md)

**Revised verdict:** **BLOCKED** — 5 wildcard gaps defeat CS8509 enforcement

---

## Scope of re-review

The plan's central goal:

> When a developer adds a new `ConstructKind`, `ActionKind`, `ActionSyntaxShape`, or `RoutingFamily` to the catalog, the compiler must produce **CS8509 errors** at every location that needs updating. No silent gaps. No runtime-only throws.

I re-examined every switch expression in the implementation, not just the two originally reported deviations.

---

## What IS correct (confirmed)

| Switch | File:Line | CS8509 intact? | Notes |

|--------|-----------|----------------|-------|

| `BuildNode()` | Parser.cs:1315–1378 | ✅ | All 12 `ConstructKind` arms, no wildcard, `#pragma CS8524` only |

| `DisambiguateAndParse()` EventScoped | Parser.cs:239–252 | ✅ | All `ConstructKind` arms listed explicitly, no wildcard |

| `DisambiguateAndParse()` StateScoped | Parser.cs:272–286 | ✅ | All `ConstructKind` arms listed explicitly, no wildcard |

| `ParseActionStatement()` outer | Parser.cs:612–620 | ✅ | All 4 `ActionSyntaxShape` arms + explicit `None` throw, no wildcard |

These four switches achieve the plan's goal. Adding a new named enum member triggers CS8509.

---

## What is BROKEN — 5 gaps
### Gap 1–4: Inner `ActionKind` switches use `_ => throw`

**Plan requirement (Slice 5, explicitly):**

> Inner switch on `ActionKind` inside each shape handler (**no default**): fires when a new ActionKind is added with an existing shape but no node constructor.

The plan's code examples showed `// No default — CS8509 fires when new [shape] ActionKind added` on every inner switch.

**Actual implementation:**

| # | Method | File:Line | Pattern | CS8509? |

|---|--------|-----------|---------|---------|

| 1 | `ParseAssignValueStatement` | Parser.cs:631–635 | `_ => throw new InvalidOperationException(...)` | ❌ suppressed |

| 2 | `ParseCollectionValueStatement` | Parser.cs:644–651 | `_ => throw new InvalidOperationException(...)` | ❌ suppressed |

| 3 | `ParseCollectionIntoStatement` | Parser.cs:666–671 | `_ => throw new InvalidOperationException(...)` | ❌ suppressed |

| 4 | `ParseFieldOnlyStatement` | Parser.cs:679–683 | `_ => throw new InvalidOperationException(...)` | ❌ suppressed |

**Failure scenario:** Add `ActionKind.Increment` with `SyntaxShape = AssignValue` to the catalog. The outer `ActionSyntaxShape` switch routes it correctly to `ParseAssignValueStatement`. But the inner `ActionKind` switch's `_ =>` wildcard catches it — producing a runtime `InvalidOperationException` instead of a CS8509 compile error. The developer gets no compiler guidance.

**This directly defeats Gap 7 from the plan** ("ActionKind↔Statement node enforcement — Covered by gap 4 fix: ParseActionStatement CS8509 forces one arm per ActionKind").

**Fix:** Remove all four `_ => throw` arms. Add `#pragma warning disable CS8524` / `restore` around each inner switch (same pattern as BuildNode and the outer switch). Each inner switch already lists every `ActionKind` that belongs to its shape — the wildcard is purely redundant defensive code.
### Gap 5: `InvokeSlotParser` uses `_ => throw`

| # | Method | File:Line | Pattern | CS8509? |

|---|--------|-----------|---------|---------|

| 5 | `InvokeSlotParser` | Parser.cs:845–868 | `_ => throw new ArgumentOutOfRangeException(...)` | ❌ suppressed |

**The code comments and test are misleading:**

- Line 864: `// CS8509 enforcement: a new ConstructSlotKind member without an arm is a build error.`

- Line 866: `_ => throw` — **this wildcard makes the comment false.** `_ =>` covers all remaining patterns including named members. CS8509 does NOT fire.

- Test `InvokeSlotParser_SwitchIsExhaustive` (ParserInfrastructureTests.cs:90–97) checks the member **count** (17) — this is a fragile fallback, not CS8509 enforcement.

**Failure scenario:** Add `ConstructSlotKind.ConditionalBlock = 17`. The `_ =>` wildcard catches it at runtime. The count-based test catches it only if someone remembers to update the magic number. Neither is a CS8509 build error.

**Scope consideration:** `ConstructSlotKind` is not one of the four enums named in the plan's goal statement. However, the implementation itself claims CS8509 enforcement via code comments and a test named `InvokeSlotParser_SwitchIsExhaustive`. If the team chose to use test enforcement instead of CS8509 here, the comment and test name are misleading. Either way, this needs resolution.

**Fix:** Remove the `_ => throw` arm. Add `#pragma warning disable CS8524` / `restore` around the switch. All 17 `ConstructSlotKind` members already have arms. Update or remove the count-based test (it becomes redundant once CS8509 is the actual enforcement mechanism).

---

## How my prior review went wrong

In my original review, I caught myself mid-analysis:

> Wait — correction: `_ =>` in a switch expression **does** suppress CS8509 because it covers all remaining patterns including named members.

I then dismissed this because `InvokeSlotParser` operates on `ConstructSlotKind`, not the two deviating enums I was reviewing. This was correct but insufficient — I should have then asked: "Are there OTHER wildcards on enums that ARE in scope?" I never examined the four inner `ActionKind` switches. They are **squarely within Slice 5's scope** and the plan explicitly required no defaults. I failed to check whether the implementation matched the plan on those switches.

---

## Verdict: BLOCKED

**5 switches must be fixed before merge:**

1. `ParseAssignValueStatement` (Parser.cs:634) — remove `_ => throw`, add `#pragma CS8524`

2. `ParseCollectionValueStatement` (Parser.cs:650) — remove `_ => throw`, add `#pragma CS8524`

3. `ParseCollectionIntoStatement` (Parser.cs:670) — remove `_ => throw`, add `#pragma CS8524`

4. `ParseFieldOnlyStatement` (Parser.cs:682) — remove `_ => throw`, add `#pragma CS8524`

5. `InvokeSlotParser` (Parser.cs:866) — remove `_ => throw`, add `#pragma CS8524`, fix misleading comment

**Gaps 1–4 are plan violations** — Slice 5 explicitly required inner `ActionKind` switches with no default arm. The implementation has wildcards on all four.

**Gap 5 is a correctness/honesty issue** — the code claims CS8509 enforcement but the wildcard defeats it. Whether it's in formal plan scope or not, the misleading comment must be resolved.

After these fixes, every catalog enum switch in the parser will use the same pattern: explicit arms for all named members, `#pragma CS8524` to suppress unnamed-integer noise, no wildcard. CS8509 will fire on every new enum member. The plan's central goal will be achieved.

---

---

---

# Enum Deviation Review — `feature/catalog-extensibility`

**From:** Frank (Lead/Architect)

**Date:** 2026-04-28

**Re:** Two deviations reported by George post-implementation

---

## 1. `ActionSyntaxShape.None` — **UNDERMINES CS8509**
### What George did

Added `None = 0` to `ActionSyntaxShape` and added a corresponding `ActionSyntaxShape.None => throw new InvalidOperationException(...)` arm to the outer switch in `ParseActionStatement`.
### Why it matters

The plan required the outer `ActionSyntaxShape` switch and the inner `ActionKind` switches to have **no** default or wildcard arms — pure CS8509 enforcement. The `None` arm creates an escape hatch that defeats this.

Here is the exact failure mode:

1. Developer adds `ActionKind.Foo = 9`.

2. They forget to set a real `SyntaxShape` — either because they copy an incomplete stub or because `None = 0` is now a valid-looking named value that default-initializes silently.

3. The outer `ActionSyntaxShape` switch in `ParseActionStatement` hits `ActionSyntaxShape.None => throw`. That arm is satisfied. Execution terminates at runtime.

4. The inner per-shape `ActionKind` switches (`ParseAssignValueStatement`, `ParseCollectionValueStatement`, etc.) are **never reached**.

5. CS8509 fires at **none** of them — the new `ActionKind.Foo` slips the net entirely until runtime.

The plan's purpose was to make that slip **impossible at compile time**. George's `None` sentinel converts a compile-time hard error into a runtime exception. That is the wrong direction.
### Unreported companion deviation (B3)

While inspecting the inner switches, I found that George also used `_ => throw` wildcards in `ParseAssignValueStatement` and `ParseCollectionValueStatement`:

```csharp

// ParseAssignValueStatement — line ~632

ActionKind.Set => new SetStatement(span, field, value),

_ => throw new InvalidOperationException(...),

// ParseCollectionValueStatement — line ~643

ActionKind.Add => ..., ActionKind.Remove => ..., ActionKind.Enqueue => ..., ActionKind.Push => ...,

_ => throw new InvalidOperationException(...),

```

The `_` wildcard suppresses CS8509. A developer adding `ActionKind.Foo` with `SyntaxShape = AssignValue` would NOT get a compile-time error at `ParseAssignValueStatement` — it falls through to `_ => throw` silently at runtime. George did **not** report this deviation. It is the structural companion to the `None` problem and must be fixed at the same time.

(Note: George correctly used explicit wrong-family throw arms in the `DisambiguateAndParse` switches — lines 243–249 and 279–283 list every non-matching `ConstructKind` explicitly. The same discipline must be applied to the inner `ActionKind` switches.)
### Required fixes

- **B1:** Remove `None = 0` from `ActionSyntaxShape`. The enum must have exactly four members: `AssignValue`, `CollectionValue`, `CollectionInto`, `FieldOnly` — values 1 through 4, or leave C# auto-assign them starting from 1 if no explicit numbering is needed.

- **B2:** Remove the `ActionSyntaxShape.None => throw` arm from the `ParseActionStatement` outer switch. The switch must cover only the four real shapes with no wildcard.

- **B3:** Replace `_ => throw` wildcards in `ParseAssignValueStatement` and `ParseCollectionValueStatement` (and any other inner `ActionKind` switches that use a wildcard) with exhaustive explicit-arm patterns. For each shape-dispatch method, list every `ActionKind` that **does not** belong to that shape as an explicit `=> throw new InvalidOperationException(...)` arm. This is exactly how `DisambiguateAndParse` was implemented — the same pattern must apply here.

---

## 2. `RoutingFamily.None` — **SAFE**
### What George did

Added `None = 0` to `RoutingFamily`.
### Analysis

`RoutingFamily` is **never switched on** in `Parser.cs`. The parser routes constructs via `FindDisambiguatedConstruct` using token kinds from `DisambiguationEntry` — `RoutingFamily` is read from catalog metadata to guide routing logic but is not itself the discriminant of any switch expression in production code.

The test `Constructs_RoutingFamily_AllMembersHaveValue` correctly validates:

```csharp

meta.RoutingFamily.Should().NotBe((RoutingFamily)0,

    $"{kind} must have a non-default RoutingFamily");

```

This catches any `ConstructKind` that ships without a valid `RoutingFamily` — including any developer who accidentally leaves the field default-initialized. The enforcement is at test time, not compile time, but that is acceptable because `RoutingFamily` carries no CS8509-enforced switch in the parser.

`RoutingFamily.None` is a design smell — a sentinel for a non-value — and I would prefer it not exist. But it does not undermine CS8509 enforcement in production code. **No required fix before merge.** George may clean it up in a follow-on if desired.

---

## 3. `#pragma warning disable CS8524` — **SAFE**
### What George did

Added `#pragma warning disable CS8524` / `#pragma warning restore CS8524` pairs around four exhaustive switch expressions.
### Analysis

CS8524 and CS8509 are **distinct diagnostics**:

- **CS8509** fires when a **named** enum member is absent from a switch expression. This is the enforcement diagnostic for the catalog extensibility plan.

- **CS8524** fires when an **unnamed** raw integer value (e.g., `(ConstructKind)99`) could potentially reach a switch expression that does not have a catch-all arm.

A `#pragma disable CS8524` does **not** suppress CS8509. Both can be active simultaneously and independently.

All four pragmas are **tightly scoped** — each `disable` is immediately followed by a `restore` after the switch expression closes:

| Block | Lines | Switch |

|-------|-------|--------|

| 1 | 237–251 | EventScoped `FindDisambiguatedConstruct` switch |

| 2 | 270–285 | StateScoped `FindDisambiguatedConstruct` switch |

| 3 | 610–620 | `ActionSyntaxShape` switch in `ParseActionStatement` |

| 4 | 1313–1378 | `BuildNode` 12-arm `ConstructKind` switch |

No pragma spans a file boundary or suppresses anything beyond the intended switch. CS8509 is active at all four sites. George's explanation is technically correct.

---

## 4. Overall Verdict — **BLOCKED**

The branch does not merge until George resolves the following numbered items. **No exceptions.**

**B1:** Remove `None = 0` from `ActionSyntaxShape`. Enum must have exactly: `AssignValue`, `CollectionValue`, `CollectionInto`, `FieldOnly`.

**B2:** Remove the `ActionSyntaxShape.None => throw` arm from the outer switch in `ParseActionStatement`. Outer switch must be a clean 4-arm exhaustive switch, no sentinel handling.

**B3:** Replace `_ => throw` wildcard arms in the inner `ActionKind` switches (`ParseAssignValueStatement`, `ParseCollectionValueStatement`, and any other inner dispatch methods that switch on `ActionKind`) with exhaustive explicit-arm patterns — listing every `ActionKind` value that does not belong to that shape as a named throw arm. CS8509 must fire at these switches when a new `ActionKind` is added.

---

**`RoutingFamily.None` does not block merge.

`#pragma disable CS8524` does not block merge.**

Fix B1–B3, push, and request re-review.

---

---

## Frank — Final Re-Review Verdict

**Date:** 2026-04-28

**Branch:** feature/catalog-extensibility

**Commit reviewed:** 5e5b2f958b041f199c7360c79feb49f6c7e02ba4

---
### Verdict: APPROVED

---
### Findings

Every blocking item from both prior review documents is closed.

**B1 — `ActionSyntaxShape.None = 0` removed:** ✅

`ActionSyntaxShape` now has exactly 4 members with no explicit integer assignments:

```csharp

public enum ActionSyntaxShape { AssignValue, CollectionValue, CollectionInto, FieldOnly }

```

C# auto-assigns 0–3. No sentinel. Note: `AssignValue = 0` now occupies the zero slot formerly held by `None`. This means a default-initialized `ActionMeta.SyntaxShape` silently resolves to `AssignValue` rather than an obvious error sentinel. This is a minor structural note — it doesn't undermine CS8509 enforcement (all 8 catalog entries explicitly declare their `SyntaxShape`, and no route through the outer switch bypasses the inner switches), but it's worth recording: if a future `ActionMeta` entry is added with `SyntaxShape` accidentally omitted from the constructor, the `Enum.IsDefined` test will pass silently because `AssignValue = 0` is defined. **Not a blocker**, but the team should remain aware that `None = 0`'s safety-net role is not fully replicated by the current arrangement.

**B2 — `ActionSyntaxShape.None` arm removed from outer switch:** ✅

`ParseActionStatement` outer switch is a clean 4-arm exhaustive switch:

```csharp

return meta.SyntaxShape switch

{

    ActionSyntaxShape.AssignValue     => ParseAssignValueStatement(meta),

    ActionSyntaxShape.CollectionValue => ParseCollectionValueStatement(meta),

    ActionSyntaxShape.CollectionInto  => ParseCollectionIntoStatement(meta),

    ActionSyntaxShape.FieldOnly       => ParseFieldOnlyStatement(meta),

};

```

`#pragma CS8524` tightly scoped. No wildcard. No sentinel arm.

**B3 — `ParseAssignValueStatement` inner switch:** ✅

All 8 `ActionKind` members covered with explicit named arms. `Set` is the valid arm; `Add`, `Remove`, `Enqueue`, `Dequeue`, `Push`, `Pop`, `Clear` each throw with identity-specific messages. No wildcard. `#pragma CS8524` tightly scoped.

**B4 — `ParseCollectionValueStatement` inner switch:** ✅

All 8 `ActionKind` members covered. `Add`, `Remove`, `Enqueue`, `Push` are valid; `Set`, `Dequeue`, `Pop`, `Clear` throw. No wildcard. `#pragma CS8524` tightly scoped.

**B5 — `ParseCollectionIntoStatement` inner switch:** ✅

All 8 `ActionKind` members covered. `Dequeue`, `Pop` are valid; `Set`, `Add`, `Remove`, `Enqueue`, `Push`, `Clear` throw. No wildcard. `#pragma CS8524` tightly scoped.

**B6 — `ParseFieldOnlyStatement` inner switch:** ✅

All 8 `ActionKind` members covered. `Clear` is the valid arm; `Set`, `Add`, `Remove`, `Enqueue`, `Dequeue`, `Push`, `Pop` throw. No wildcard. `#pragma CS8524` tightly scoped.

**B7 — `InvokeSlotParser`:** ✅

`_ => throw` removed. All 17 `ConstructSlotKind` members have explicit named arms. `#pragma CS8524` tightly scoped. Comment updated to accurately state "CS8509 enforces named-value coverage here; #pragma CS8524 suppresses unnamed-integer noise" — no longer a false claim.

**Test fix — `Actions_ActionSyntaxShape_AllMembersHaveValue`:** ✅

`NotBe((ActionSyntaxShape)0)` replaced with `Enum.IsDefined(meta.SyntaxShape).Should().BeTrue(...)`. This is the correct assertion now that no `None = 0` sentinel exists to distinguish "unset" from the first real member. The test guards against raw integer values outside the defined set.

**Actions.cs — 8 ActionMeta entries:** ✅

All 8 `ActionKind` entries carry a non-zero, explicitly-set `SyntaxShape`: `Set → AssignValue`, `Add/Remove/Enqueue/Push → CollectionValue`, `Dequeue/Pop → CollectionInto`, `Clear → FieldOnly`. No entry is missing a shape declaration.

---
### CS8509 Enforcement Status

**Confirmed. The pattern achieves the stated goal.**

The enforcement chain is structurally sound:

1. A developer adds `ActionKind.Increment = 8` to the enum and declares `SyntaxShape = AssignValue` on the new `ActionMeta`.

2. The outer `ActionSyntaxShape` switch routes to `ParseAssignValueStatement`.

3. The inner `ActionKind` switch in `ParseAssignValueStatement` has no arm for `ActionKind.Increment`. **CS8509 fires. Build fails.**

4. The developer must add the arm before the branch compiles.

The same chain holds for all 4 inner switches depending on which `SyntaxShape` the new kind declares. CS8509 is active at all four sites because no wildcard suppresses it. `#pragma CS8524` suppresses only CS8524 (unnamed-integer noise) and has no effect on CS8509 — the two diagnostics are independent.

The only remaining caveat is the observation under B1: if a developer adds a new `ActionMeta` and omits `SyntaxShape` entirely (relying on default initialization), it silently routes to `AssignValue` (value 0) rather than producing an obvious test failure. This is a test-time gap, not a compile-time gap, and does not affect CS8509 enforcement. The `Enum.IsDefined` test will pass in that scenario. A future hardening option is to add explicit `= 1, 2, 3, 4` numbering so 0 becomes undefined, but that is not required for this merge.

**All 7 blocking items closed. No open findings. Branch is approved for merge.**

---

---

---

# Deviation Review: George's Catalog Extensibility Implementation

**Reviewer:** Frank (Lead/Architect)

**Date:** 2026-04-28

**Branch:** `feature/catalog-extensibility`

**Verdict:** APPROVED — both deviations are safe; CS8509 enforcement is intact.

---

## Deviation 1: `None = 0` on `RoutingFamily` and `ActionSyntaxShape`
### Finding: Safe — no CS8509 gap

**`RoutingFamily`** (Construct.cs:37–53):

- `None = 0` exists as a sentinel for default-initialization detection.

- **No switch expression in the codebase dispatches on `RoutingFamily` at all.** The parser routes by `ConstructKind` (via `DisambiguateAndParse` and `BuildNode`), not by `RoutingFamily`. `RoutingFamily` is a metadata property on `ConstructMeta` — it classifies constructs for documentation and routing-table grouping, but the actual dispatch switches are on `ConstructKind`.

- Since there is no `RoutingFamily` switch, `None` cannot act as a catch-all or mask missing arms. CS8509 enforcement operates entirely through the `ConstructKind` switches, which have no `None` member and no wildcard arms (except the `_ => throw` in `GetMeta` and `InvokeSlotParser`, which are defensive guards against unnamed integer casts, not semantic catch-alls).

- Every `ConstructMeta` in `Constructs.cs` assigns a non-`None` routing family (Header, Direct, StateScoped, or EventScoped). `None` is only reachable via `default(RoutingFamily)`.

**`ActionSyntaxShape`** (Action.cs:29–41):

- `None = 0` exists as the same sentinel pattern.

- The `ParseActionStatement` switch (Parser.cs:611–621) dispatches on `ActionSyntaxShape` and includes an **explicit `ActionSyntaxShape.None => throw` arm** — it does not fall through or act as a default. It hard-throws with a diagnostic message identifying the offending `ActionKind`.

- Every `ActionMeta` in `Actions.cs` assigns a non-`None` shape. `None` is only reachable via `default(ActionSyntaxShape)`.

- Adding a new `ActionSyntaxShape` member (e.g., `ConditionalValue`) would trigger CS8509 on the `ParseActionStatement` switch because the new member would have no arm. The `None` arm does not catch it.

**Conclusion:** `None = 0` sentinels are inert. They serve test/initialization detection and do not participate in any switch dispatch path that would mask a missing arm. CS8509 enforcement is fully intact for both enum families.

---

## Deviation 2: `#pragma warning disable CS8524`
### Finding: Safe — CS8524 suppression does not affect CS8509

**The two warnings are independent:**

- **CS8509** fires when a *named* enum member has no matching arm. This is the enforcement we depend on.

- **CS8524** fires when an *unnamed* integer value (e.g., `(ConstructKind)999`) has no matching arm. This is noise for our use case.

**Evidence from Parser.cs:**

- 4 pragma-scoped regions suppress CS8524 only: lines 238–252, 271–286, 611–621, 1314–1379.

- Each pragma is tightly scoped (`disable` immediately before the switch, `restore` immediately after).

- `TreatWarningsAsErrors=true` is set in `Precept.csproj` (line 7), meaning CS8509 fires as an **error**, not a warning. Suppressing CS8524 has zero effect on CS8509 — they are separate diagnostic IDs with separate suppression state.

**Verification:** The `BuildNode` switch (Parser.cs:1315–1378) has exactly 11 arms for exactly 11 `ConstructKind` members, with no wildcard. If a 12th `ConstructKind` is added, CS8509 fires as a build error. The CS8524 pragma does not intercept this.

**The `InvokeSlotParser` switch** (Parser.cs:845–868) uses the older `_ => throw` pattern for `ConstructSlotKind`, which is also fine — the wildcard catches only unnamed integer casts, and CS8509 still fires for missing named members because the switch is an expression (not a statement).

Wait — correction: `_ =>` in a switch expression **does** suppress CS8509 because it covers all remaining patterns including named members. However, `InvokeSlotParser` switches on `ConstructSlotKind`, not one of the two deviating enums. The four `#pragma` regions all cover switches that have **no wildcard arm** — they list every named member explicitly and suppress only the unnamed-integer CS8524 noise. This is exactly the correct pattern.

---

## Summary

| Deviation | Safe? | Reason |

|-----------|-------|--------|

| `None = 0` on `RoutingFamily` | ✅ | No switch dispatches on `RoutingFamily`; sentinel is metadata-only |

| `None = 0` on `ActionSyntaxShape` | ✅ | Explicit `None => throw` arm; does not mask new members |

| `#pragma disable CS8524` | ✅ | Independent from CS8509; tightly scoped; `TreatWarningsAsErrors` makes CS8509 a build error |

**The catalog extensibility contract is intact:** adding a new `ConstructKind` or `ActionKind` (or `ActionSyntaxShape` / `RoutingFamily`) member produces CS8509 build errors at every incomplete switch. George's deviations are structurally sound.

---

---

---

# PRECEPT0018 — Semantic Enum Zero-Slot Analyzer

**Author:** Frank (Code Reviewer)

**Date:** 2026-04-28

**Status:** Ready for implementation

**Implementer:** George

---

## 1. Feasibility

**Straightforward.** This is a textbook `SymbolAction` analyzer on `SymbolKind.NamedType` filtered to enums. The Roslyn `IFieldSymbol.ConstantValue` API gives direct access to the underlying integer value of each member. No control-flow analysis, no cross-compilation lookups, no semantic model gymnastics.

The existing `src/Precept.Analyzers/` project already targets `netstandard2.0` with `Microsoft.CodeAnalysis.CSharp 5.3.0` and is wired into `src/Precept/Precept.csproj` via `<ProjectReference OutputItemType="Analyzer">`. Infrastructure cost: zero.

**Diagnostic ID:** `PRECEPT0018`

---

## 2. The Rule
### What triggers a violation

An enum member resolves to integer value `0` AND does not meet any of the exemption criteria below.

Precisely: for every `enum` type declaration where the containing namespace starts with `Precept`, iterate its `IFieldSymbol` members with `HasConstantValue == true`. If any member's `ConstantValue` converts to `0L` (after widening to `long`), and no exemption applies, report `PRECEPT0018` on that member.
### Exemption criteria (in evaluation order)

| # | Condition | Rationale |

|---|-----------|-----------|

| E1 | The enum has `[System.Flags]` | Flags enums require `None = 0` by design. Standard C# pattern. |

| E2 | The member is named exactly `None` | Universal .NET sentinel convention. `None = 0` is structural — it means "no value assigned." |

| E3 | The member has `[AllowZeroDefault]` | Explicit opt-out for intentional semantic defaults where zero-init is correct by design. |

**That's it.** Three exemptions. No name allowlists for `Any`, `Normal`, `Default`, `Unknown`, etc. Those must use `[AllowZeroDefault]` — see § 3 for justification.
### Scope

- **Checked:** All enums in any namespace starting with `Precept` (covers `Precept.Language`, `Precept.Runtime`, `Precept.Pipeline`, etc.).

- **Not checked:** Test assemblies, third-party code, namespaces outside `Precept.*`.

- **Visibility:** All access levels — `public`, `internal`, `private`. The `LexerMode` enum is `private` and still needs protection. The silent-default risk is the same regardless of visibility.
### `[Flags]` enums

Auto-exempted entirely (E1). The analyzer skips them — it does not inspect individual members. Currently only `TypeTrait` is `[Flags]` in the codebase.

---

## 3. Opt-Out Mechanism

**Recommended:** `[AllowZeroDefault]` attribute on the member, with `[Flags]` and `None`-named auto-exemptions.
### Why not the alternatives

| Option | Verdict | Reason |

|--------|---------|--------|

| **Name allowlist** (`None`, `Unknown`, `Any`, `Normal`, `Default`, …) | ❌ Rejected | Brittle. Every new sentinel-like name requires a code change to the analyzer. `InState` and `Ensure` are not sentinel-sounding but sit at zero intentionally in some contexts. The allowlist either grows unbounded or misses cases. |

| **`[SemanticEnum]` on the enum** (opt-in) | ❌ Rejected | Inverts the safety default. Unannotated enums are unchecked — which means every new enum is unprotected until someone remembers to add the attribute. The whole point of this analyzer is to prevent *forgetting*. |

| **`[SuppressZeroDefault]` on the member** | ❌ Rejected | Semantically identical to `[AllowZeroDefault]` but with a confusing double-negative name. "Suppress the zero-default diagnostic" vs. "Allow zero as the default" — the latter reads as intent, the former reads as workaround. |

| **`[AllowZeroDefault]` on the member** | ✅ Selected | See below. |
### Why `[AllowZeroDefault]`

1. **Safe by default.** Every enum is checked. You must explicitly opt out — the dangerous path requires a conscious decision.

2. **Self-documenting.** The attribute at the declaration site says "yes, zero-init is intentional here" — future readers don't have to reconstruct why.

3. **No magic lists.** The only auto-exempted name is `None`, which is the universal .NET convention. Everything else requires explicit annotation.

4. **Minimal noise.** Only 3 existing enums need the attribute: `LexerMode.Normal`, `QualifierMatch.Any`, `PeriodDimension.Any`. That's 3 one-line annotations across the entire codebase.

5. **Consistent with project philosophy.** Precept's design principle is "make invalid states structurally impossible." An opt-out attribute is the structural version of that principle applied to the analyzer itself.
### Why `None` gets auto-exempted (not just the attribute)

`None = 0` is a .NET ecosystem convention with decades of usage. Requiring `[AllowZeroDefault]` on every `None` member would be pure ceremony — nobody has ever accidentally named a member `None` when they meant it to be semantically meaningful. The auto-exemption eliminates ~3 annotations (`RoutingFamily.None`, `GraphAnalysisKind.None`, `QualifierAxis.None`) with zero false-negative risk.

---

## 4. Placement
### Analyzer class

**File:** `src/Precept.Analyzers/Precept0018SemanticEnumZeroSlot.cs`

Lives alongside the existing 17 analyzers. No new project needed.
### Attribute class

**File:** `src/Precept/AllowZeroDefaultAttribute.cs`

```csharp

namespace Precept;

/// <summary>

/// Suppresses PRECEPT0018 for an enum member at value 0.

/// Apply this when zero-initialization is intentional — e.g., a "don't-care" default

/// or a documented initial state. The attribute signals that default(T) routing to

/// this member was a deliberate design choice, not an accident.

/// </summary>

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]

public sealed class AllowZeroDefaultAttribute : Attribute { }

```

**Why `AttributeTargets.Field`:** Enum members are fields in the CLR model. `AttributeTargets.Field` is the correct target. (There is no `AttributeTargets.EnumMember`.)
### No csproj changes needed

- `src/Precept.Analyzers/Precept.Analyzers.csproj` — no changes. The project already has the Roslyn reference and build configuration.

- `src/Precept/Precept.csproj` — no changes. The `<ProjectReference>` to Analyzers is already in place.

- `test/Precept.Analyzers.Tests/` — no changes to the test project file (it already references the analyzer project).

---

## 5. Diagnostic Output
### Message format

```

PRECEPT0018: Enum member '{0}' in '{1}' has value 0. Semantic enums must use explicit

1-based values so default(T) throws instead of silently routing. Assign '{0} = 1'

(and renumber subsequent members) or mark with [AllowZeroDefault] if zero-init is intentional.

```

**Substitutions:**

- `{0}` = member name (e.g., `AssignValue`)

- `{1}` = enum name (e.g., `ActionSyntaxShape`)
### Descriptor

```csharp

private static readonly DiagnosticDescriptor Rule = new(

    DiagnosticId,

    title: "Enum member at value 0 in semantic enum",

    messageFormat: "Enum member '{0}' in '{1}' has value 0 — semantic enums must use explicit " +

                   "1-based values so default(T) throws instead of silently routing. " +

                   "Assign '{0} = 1' (and renumber subsequent members) or mark with [AllowZeroDefault] " +

                   "if zero-init is intentional.",

    category: "Precept.Language",

    defaultSeverity: DiagnosticSeverity.Error,

    isEnabledByDefault: true,

    description: "Every enum where all members are semantically meaningful must leave the zero " +

                 "slot unnamed. default(T) = (T)0 = unnamed = SwitchExpressionException rather " +

                 "than silent routing to an arbitrary first member. Enums with None = 0, " +

                 "[Flags] enums, and members marked [AllowZeroDefault] are exempt.");

```

**Severity: Error.** The existing project uses `TreatWarningsAsErrors`, so Warning would also block the build. But this is a correctness invariant — it deserves Error severity to match the other PRECEPT analyzers.

---

## 6. Implementation Guide
### File inventory

| File | Action | Description |

|------|--------|-------------|

| `src/Precept/AllowZeroDefaultAttribute.cs` | **Create** | The opt-out attribute |

| `src/Precept.Analyzers/Precept0018SemanticEnumZeroSlot.cs` | **Create** | The analyzer |

| `test/Precept.Analyzers.Tests/Precept0018Tests.cs` | **Create** | Test cases |
### Analyzer skeleton

```csharp

using System.Collections.Immutable;

using System.Linq;

using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.Diagnostics;

namespace Precept.Analyzers;

/// <summary>

/// PRECEPT0018 — Enum members at value 0 in Precept.* namespaces must be either:

/// (a) named "None" (structural sentinel), (b) in a [Flags] enum, or

/// (c) marked with [AllowZeroDefault]. All other zero-valued members are flagged

/// because default(T) silently routes to them.

/// </summary>

[DiagnosticAnalyzer(LanguageNames.CSharp)]

public sealed class PRECEPT0018SemanticEnumZeroSlot : DiagnosticAnalyzer

{

    public const string DiagnosticId = "PRECEPT0018";

    private static readonly DiagnosticDescriptor Rule = new( /* see § 5 */ );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>

        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)

    {

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeEnum, SymbolKind.NamedType);

    }

    private static void AnalyzeEnum(SymbolAnalysisContext ctx)

    {

        var type = (INamedTypeSymbol)ctx.Symbol;

        // Only enums.

        if (type.TypeKind != TypeKind.Enum)

            return;

        // Scope: Precept.* namespaces only.

        if (!IsInPreceptNamespace(type))

            return;

        // E1: [Flags] enums are exempt.

        if (type.GetAttributes().Any(a =>

            a.AttributeClass?.Name == "FlagsAttribute" &&

            a.AttributeClass.ContainingNamespace?.ToDisplayString() == "System"))

            return;

        // Check each member for value 0.

        foreach (var member in type.GetMembers().OfType<IFieldSymbol>())

        {

            if (!member.HasConstantValue)

                continue;

            if (System.Convert.ToInt64(member.ConstantValue) != 0L)

                continue;

            // E2: Named "None" — structural sentinel.

            if (member.Name == "None")

                continue;

            // E3: [AllowZeroDefault] attribute.

            if (member.GetAttributes().Any(a =>

                a.AttributeClass?.Name == "AllowZeroDefaultAttribute"))

                continue;

            // Violation.

            ctx.ReportDiagnostic(Diagnostic.Create(

                Rule,

                member.Locations.FirstOrDefault(),

                member.Name,

                type.Name));

        }

    }

    private static bool IsInPreceptNamespace(INamedTypeSymbol type)

    {

        var ns = type.ContainingNamespace;

        while (ns != null && !ns.IsGlobalNamespace)

        {

            if (ns.Name == "Precept")

                return true;

            ns = ns.ContainingNamespace;

        }

        return false;

    }

}

```
### Key implementation notes

1. **`Convert.ToInt64`** handles all underlying enum types (`byte`, `short`, `int`, `long`, `uint`, etc.) safely.

2. **Namespace check walks up** the containment chain. `Precept.Language.TokenKind` → finds `Precept` at depth 2. `System.DayOfWeek` → never hits `Precept`.

3. **Attribute matching is by name**, not by type identity. The analyzer runs in `netstandard2.0` and doesn't reference `src/Precept/` — it cannot resolve the attribute's `INamedTypeSymbol` by reference. Name matching (`"AllowZeroDefaultAttribute"`) is the standard Roslyn analyzer pattern for this.

4. **`RegisterSymbolAction` on `SymbolKind.NamedType`** fires once per type declaration. This is more efficient than syntax-node analysis for enum-level checks.

---

## 7. Test Plan

All tests go in `test/Precept.Analyzers.Tests/Precept0018Tests.cs`. Use the existing `AnalyzerTestHelper.AnalyzeAsync<T>` pattern.
### True positives (MUST flag)

| # | Test name | Source | Expected |

|---|-----------|--------|----------|

| TP1 | `Implicit_zero_first_member_flags` | `namespace Precept.Language { public enum Foo { Bar, Baz } }` | 1 diagnostic on `Bar` |

| TP2 | `Explicit_zero_first_member_flags` | `namespace Precept.Language { public enum Foo { Bar = 0, Baz = 1 } }` | 1 diagnostic on `Bar` |

| TP3 | `Zero_not_first_member` | `namespace Precept.Language { public enum Foo { A = 1, B = 0, C = 2 } }` | 1 diagnostic on `B` |

| TP4 | `Private_enum_still_flagged` | `namespace Precept.Pipeline { class Outer { enum Inner { X, Y } } }` | 1 diagnostic on `X` |

| TP5 | `Internal_enum_still_flagged` | `namespace Precept.Runtime { internal enum Foo { Bar, Baz } }` | 1 diagnostic on `Bar` |

| TP6 | `Nested_precept_namespace` | `namespace Precept.Language.Nested { public enum Foo { Bar } }` | 1 diagnostic on `Bar` |
### True negatives (MUST NOT flag)

| # | Test name | Source | Expected |

|---|-----------|--------|----------|

| TN1 | `None_at_zero_exempt` | `namespace Precept.Language { public enum Foo { None = 0, Bar = 1 } }` | 0 diagnostics |

| TN2 | `Flags_enum_exempt` | `namespace Precept.Language { [System.Flags] public enum Foo { None = 0, A = 1, B = 2 } }` | 0 diagnostics |

| TN3 | `Flags_enum_with_non_None_zero` | `namespace Precept.Language { [System.Flags] public enum Foo { All = 0, A = 1 } }` | 0 diagnostics (entire `[Flags]` enum exempt) |

| TN4 | `AllowZeroDefault_attribute` | Source with `[AllowZeroDefault] Any = 0` | 0 diagnostics |

| TN5 | `One_based_enum_clean` | `namespace Precept.Language { public enum Foo { A = 1, B = 2, C = 3 } }` | 0 diagnostics |

| TN6 | `Non_precept_namespace_ignored` | `namespace SomeOtherLib { public enum Foo { Bar } }` | 0 diagnostics |

| TN7 | `No_zero_member_at_all` | `namespace Precept.Language { public enum Foo { A = 1, B = 2 } }` | 0 diagnostics |
### Edge cases

| # | Test name | Source | Expected |

|---|-----------|--------|----------|

| EC1 | `Multiple_zero_members` | `namespace Precept.Language { public enum Foo { A = 0, B = 0, C = 1 } }` | 2 diagnostics (both `A` and `B`) |

| EC2 | `None_plus_semantic_zero` | `namespace Precept.Language { public enum Foo { None = 0, Bad = 0, Good = 1 } }` | 1 diagnostic on `Bad` (only `None` exempted by name) |

| EC3 | `Empty_enum` | `namespace Precept.Language { public enum Foo { } }` | 0 diagnostics |

| EC4 | `Byte_underlying_type` | `namespace Precept.Language { public enum Foo : byte { A, B } }` | 1 diagnostic on `A` |

| EC5 | `Long_underlying_type` | `namespace Precept.Language { public enum Foo : long { A = 0L, B = 1L } }` | 1 diagnostic on `A` |
### Regression anchors (real Precept enums)

These don't use synthetic source — they document the expected analyzer behavior against the actual codebase once the fix wave completes. George should add these as comments in the test file or as a companion checklist, not as compilable tests (since they'd require the full Precept assembly):

| Enum | Expected | After fix wave |

|------|----------|----------------|

| `Severity` (1-based) | Clean | ✅ Already fixed |

| `DiagnosticStage` (1-based) | Clean | ✅ Already fixed |

| `TypeKind` (1-based) | Clean | ✅ Already fixed |

| `RoutingFamily.None = 0` | Clean (E2: name `None`) | ✅ Already correct |

| `TypeTrait` (`[Flags]`) | Clean (E1) | ✅ Already correct |

| `QualifierMatch.Any = 0` | Clean after `[AllowZeroDefault]` | Needs attribute |

| `PeriodDimension.Any = 0` | Clean after `[AllowZeroDefault]` | Needs attribute |

| `LexerMode.Normal = 0` | Clean after `[AllowZeroDefault]` | Needs attribute |

| `TokenKind.Precept = 0` | **Flagged** | Needs 1-based fix |

| `OperatorKind.Or = 0` | **Flagged** | Needs 1-based fix |

---

## 8. Rollout Sequence

The analyzer enforces at Error severity in a `TreatWarningsAsErrors` build. Enabling it before fixing the remaining ~23 at-risk enums will break the build. George should follow this sequence:

1. **Create the attribute** (`AllowZeroDefaultAttribute.cs`) — zero risk, additive.

2. **Create the analyzer** (`Precept0018SemanticEnumZeroSlot.cs`) — does not fire until build.

3. **Create the tests** (`Precept0018Tests.cs`) — validates analyzer logic in isolation.

4. **Add `[AllowZeroDefault]` to the 3 intentional-zero enums:**

   - `LexerMode.Normal`

   - `QualifierMatch.Any`

   - `PeriodDimension.Any`

5. **Fix the remaining ~20 catalog enums** to 1-based values (same pattern as the 6 already fixed). This is a separate PR or a later batch in the same PR — George's call based on scope.

6. **Build.** If clean, the analyzer is live.

**Step 5 is the big one.** The 6 already-fixed enums (`Severity`, `DiagnosticStage`, `ConstraintStatus`, `Prospect`, `FieldAccessMode`, `ActionSyntaxShape`, `TypeKind`) prove the pattern. The remaining ~20 follow the same mechanical transformation: assign `= 1` to the first member, explicit sequential values to the rest, update any hardcoded integer references in tests.

---

## 9. What This Does NOT Cover

- **Enum members with no explicit value that aren't at zero.** The analyzer only checks value `0`. It does not enforce "all members must have explicit values" — that's a style rule, not a correctness invariant.

- **Switch exhaustiveness.** PRECEPT0007 already covers `GetMeta` switch exhaustiveness. This analyzer is complementary — it protects the *enum declaration*, not the *switch consumption*.

- **Struct default-initialization.** If a struct has a semantic enum field, `default(Struct)` produces `(Enum)0`. This analyzer catches the enum-side problem; struct-level protection would be a separate analyzer.

---

## Appendix A: Enums That Will Need `[AllowZeroDefault]`

| Enum | Member | File | Justification |

|------|--------|------|---------------|

| `LexerMode` | `Normal` | `Pipeline/Lexer.cs` | Correct initial state — zero-init of the lexer starts in `Normal` mode by design. |

| `QualifierMatch` | `Any` | `Language/Operation.cs` | Documented default for ~95% of catalog entries. Zero-init means "matches any qualifier" — correct. |

| `PeriodDimension` | `Any` | `Language/ProofRequirement.cs` | Don't-care default. Zero-init means "any time dimension acceptable" — correct. |

## Appendix B: Enums That Will Need 1-Based Fix

These are the catalog enums where the first semantic member currently sits at implicit or explicit `0`. Each needs the same mechanical fix applied to the first 6:

`ActionKind`, `AnchorScope`, `AnchorTarget`, `Arity`, `Associativity`, `ConstructKind`, `ConstructSlotKind`, `ConstraintKind`, `DiagnosticCategory`, `DiagnosticCode`, `FaultCode`, `FaultSeverity`, `FunctionCategory`, `FunctionKind`, `ModifierCategory`, `ModifierKind`, `OperationKind`, `OperatorFamily`, `OperatorKind`, `ProofRequirementKind`, `TokenCategory`, `TokenKind`

**Count: ~22 enums.** All follow the same pattern — assign `= 1` to the first member and explicit sequential values to the rest.

---

*Frank — 2026-04-28. This document is implementation-ready. George: follow § 6 for file paths, § 7 for tests, § 8 for rollout order. No ambiguity should remain.*

---

---

---

# Zero-Slot Enum Audit — `src/Precept/`

**Reviewer:** Frank

**Date:** 2026-04-28

**Scope:** All `enum` declarations in `src/Precept/` audited for named semantic values at position 0

**Trigger:** `ActionSyntaxShape` fix (explicit 1-based values); Shane asked "are there others?"

---

## Executive Summary

Shane's fix was correct and necessary. There are **four confirmed risks** sharing the same class of failure, one of which (`Severity.Info=0`) has the most dangerous silent-failure profile in the codebase today. Two more (`ConstraintStatus.Satisfied=0`, `Prospect.Certain=0`) are dormant time bombs that will matter the moment the evaluator and inspection engine are no longer stubs. The remaining enums are either intentionally sentinel-at-zero, protected by wildcard-throw arms, or unused in live switch dispatch.

---

## 1. Zero-Slot Risks Found
### RISK 1 — `Severity` · `Severity.cs` (inside `Diagnostic.cs`) · **HIGH**

```csharp

public enum Severity

{

    Info,     // = 0  ← RISK

    Warning,

    Error

}

```

**Where switched on:**

`Compiler.cs:34` — `diagnostics.Any(d => d.Severity == Severity.Error)` sets `HasErrors`.

Every diagnostic consumer in the language server and MCP tools compares against `Severity.Error` or `Severity.Warning`.

**Zero-default consequence:**

`Diagnostic` is a `readonly record struct`. `default(Diagnostic)` gives `Severity = Info`. A bug in any new diagnostic factory path that passes `default` for severity (e.g., `new Diagnostic(default, stage, code, msg, span)`) silently downgrades all errors to informational. `Compiler.HasErrors` returns `false`. The pipeline passes. Invalid precepts compile clean. This is the most dangerous silent failure mode in the codebase — it bypasses the correctness gate entirely.

**Current protection:**

`Diagnostics.Create(code, span)` is the sole factory and always derives severity from `DiagnosticMeta`. But the struct is publicly constructible without it.

**Recommended fix:**

```csharp

public enum Severity

{

    Info    = 1,

    Warning = 2,

    Error   = 3,

}

```

`default(Severity) = (Severity)0` is unnamed. Any uninitialized diagnostic severity will throw `SwitchExpressionException` on first inspection — loud, not silent.

---
### RISK 2 — `DiagnosticStage` · `Diagnostic.cs` · **MEDIUM**

```csharp

public enum DiagnosticStage

{

    Lex,    // = 0  ← RISK

    Parse,

    Type,

    Graph,

    Proof

}

```

**Where switched on:**

Stage is carried in every `Diagnostic` struct and in `DiagnosticMeta`. The language server and MCP vocabulary filter diagnostics by stage for display ordering and category attribution. A zero-default stage misattributes the diagnostic to `Lex` regardless of what pipeline stage produced it.

**Zero-default consequence:**

Same struct-constructibility exposure as `Severity`. Less catastrophic (wrong attribution, not wrong severity), but still wrong: a type-check error silently appears as a lex error in tooling output.

**Recommended fix:**

```csharp

public enum DiagnosticStage

{

    Lex   = 1,

    Parse = 2,

    Type  = 3,

    Graph = 4,

    Proof = 5,

}

```

**Note:** Fix `Severity` and `DiagnosticStage` together — they share the same struct and the same factory.

---
### RISK 3 — `ConstraintStatus` · `Inspection.cs` · **HIGH (dormant)**

```csharp

public enum ConstraintStatus { Satisfied, Violated, Unresolvable }

//                             ^= 0  ← RISK

```

**Where switched on:**

`ConstraintResult.Status` is the per-constraint evaluation output returned by `InspectFire` and `InspectUpdate`. MCP tool `precept_inspect` surfaces this directly to callers. UI and agent consumers branch on `Satisfied` vs. `Violated` to decide whether constraints are blocking.

**Zero-default consequence:**

`ConstraintResult` is a reference-type record, so accidental zero-construction is less likely than with a struct. The risk is in the evaluator implementation (currently `throw new NotImplementedException()`). When the evaluator is implemented, any code path that constructs a `ConstraintResult` without explicitly setting `Status` (possible in collection initializers, factory patterns, or test scaffolding) silently marks a **violated constraint as satisfied**. This is a direct correctness failure in the constraint enforcement model — the entire point of Precept.

**Recommended fix:**

```csharp

public enum ConstraintStatus { Satisfied = 1, Violated = 2, Unresolvable = 3 }

```

Fix this **before** the evaluator is implemented, not after. Retrofitting is harder once there are construction sites.

---
### RISK 4 — `Prospect` · `Inspection.cs` · **HIGH (dormant)**

```csharp

public enum Prospect { Certain, Possible, Impossible }

//                     ^= 0  ← RISK

```

**Where switched on:**

`RowInspection.Prospect` and `EventInspection.OverallProspect` are the first-match routing certainty signals returned from inspection. MCP `precept_inspect` uses these to tell callers which rows will fire, which might fire, and which cannot. A wrong `Certain` on an impossible row leads agents and UIs to present incorrect state-transition forecasts.

**Zero-default consequence:**

Same analysis as `ConstraintStatus`. The evaluator is a stub. When implemented, an uninitialized `Prospect` field silently presents an impossible row as **certain to fire** — the most misleading possible output from inspection.

**Recommended fix:**

```csharp

public enum Prospect { Certain = 1, Possible = 2, Impossible = 3 }

```

---
### RISK 5 — `FieldAccessMode` · `SharedTypes.cs` · **MEDIUM (dormant)**

```csharp

public enum FieldAccessMode { Read, Write }

//                            ^= 0  ← RISK

```

**Where switched on:**

`FieldSnapshot.Mode` and `FieldAccessInfo.Mode` in runtime inspection. Callers use this to decide whether to render a field as editable in UIs or allow write operations in the runtime API.

**Zero-default consequence:**

Zero-initialized mode = `Read`. A field that should be writable in the current state silently appears read-only. Write attempts are blocked without error. This is a subtle behavioral correctness failure — not a crash, not a thrown exception, just wrong behavior in the access model.

**Recommended fix:**

```csharp

public enum FieldAccessMode { Read = 1, Write = 2 }

```

---
### RISK 6 — `TypeKind` · `TypeKind.cs` · **MEDIUM (dormant)**

```csharp

public enum TypeKind

{

    String,  // = 0  ← RISK

    Boolean,

    Integer,

    // ... 24 more

}

```

**Where switched on:**

`Types.GetMeta(TypeKind kind)` has a `_ => throw ArgumentOutOfRangeException` wildcard — an **unnamed** zero would throw. But `String` is a **named** member at 0. An uninitialized `TypeKind` in the type checker (currently a stub) would silently treat any untyped expression node as `String`. Type compatibility checks, operation lookup, and accessor validation would all pass for `String` when the actual type is unknown.

**Current protection:**

The TypeChecker is `throw new NotImplementedException()`. No live dispatch today.

**Recommended fix:**

```csharp

public enum TypeKind

{

    String = 1,

    Boolean = 2,

    Integer = 3,

    // ...

}

```

Given the size of `TypeKind` (26 members) and the number of construction sites in catalog metadata, this is a larger change. Flag for implementation before TypeChecker is filled in.

---

## 2. Enums Assessed as Safe

| Enum | Reason |

|------|--------|

| `RoutingFamily` | `None=0` is an **explicit sentinel** — XML doc says "Not set — sentinel value for default-initialization detection." This is the right pattern. |

| `GraphAnalysisKind` | `None=0` is a structural sentinel. Used as the default for `EventModifierMeta.RequiredAnalysis` — "no analysis required." Correct usage. |

| `QualifierAxis` | `None=0` is a structural sentinel. Used as the default for `FixedReturnAccessor.ReturnsQualifier` — "no qualifier returned." Correct usage. |

| `TypeTrait` | `[Flags]` enum. `None=0` is the standard flags-enum sentinel pattern. Correct. |

| `PeriodDimension` | `Any=0` acts as a "don't care" default. `DimensionProofRequirement` uses it when any dimension is acceptable. Functionally sentinel. |

| `QualifierMatch` | `Any=0` documented explicitly as "Default for ~95% of entries." Used as a default parameter value in `BinaryOperationMeta`. Correct usage — this is the intentional safe default. |

| `LexerMode` (private) | `Normal=0` is the correct initial lexer state. The scanner struct is zero-initialized with `Normal` as default, which is intentional. Even without the explicit assignment in `Scanner()`, zero-init would be correct. |

| `FaultSeverity` | Single member `Fatal=0`. No dispatch ambiguity possible. |

| `ActionKind` | `Set=0`. All 4 parser shape-specific switches enumerate every `ActionKind` with explicit arms (either correct or `throw InvalidOperationException`). An unnamed 0 would throw; the named `Set=0` correctly routes to `SetStatement` in `ParseAssignValueStatement`. `Actions.GetMeta` has `_ => throw`. Mitigated by exhaustive per-shape arm coverage. |

| `ConstraintKind` | `Invariant=0`. `Constraints.GetMeta` has `_ => throw`. `ConstraintMeta` uses a DU subtype pattern — consumers pattern-match on `ConstraintMeta.Invariant` (the subtype), not on `ConstraintKind` directly. The DU acts as a second guard layer. |

| `ConstructKind` | `PreceptHeader=0`. `ParseDirectConstruct` has a `var k => throw` wildcard. `BuildNode` has CS8524 pragma (named exhaustive) — an unnamed 0 would throw `SwitchExpressionException`; `PreceptHeader` is never reached via the direct-parse path for a legitimate precept header. |

| `ConstructSlotKind` | `IdentifierList=0`. `InvokeSlotParser` has CS8524 pragma, no wildcard — unnamed 0 throws. But named `IdentifierList=0` would route to `ParseIdentifierList` if zero-initialized. Low risk: slots are only constructed in the catalog with explicit `ConstructSlotKind` values. |

| `DiagnosticCategory`, `DiagnosticCode`, `FaultCode` | Catalog enums used in `GetMeta` switches that all have `_ => throw`. Zero-default names would dispatch to first member, but all construction is factory-controlled. |

| `FunctionKind`, `FunctionCategory` | `GetMeta` has `_ => throw`. Factory-controlled. |

| `ModifierKind` | `Optional=0`. `Modifiers.GetMeta` has `_ => throw` (confirmed by pattern). Modifier metadata always built via catalog. |

| `OperationKind`, `OperatorKind` | `GetMeta` has `_ => throw`. Factory-controlled. |

| `ProofRequirementKind` | `Numeric=0`. `GetMeta` has `_ => throw`. |

| `TokenKind` | `Precept=0`. `Tokens.GetMeta` has (inferred) `_ => throw`. Tokens come from the lexer, never from zero-init. |

| `Arity`, `Associativity`, `OperatorFamily` | Used exclusively in catalog `OperatorMeta` records. Always explicitly set. Associativity: `Left=0` is the overwhelmingly common operator direction — a zero-default here would be correct for most operators. |

| `AnchorScope`, `AnchorTarget` | `InState=0`, `Ensure=0`. Used only in `AnchorModifierMeta` construction in the Modifiers catalog. All construction is explicit. |

| `ModifierCategory`, `TypeCategory` | Classification fields on metadata records. Always explicitly set in catalog construction. |

| `TokenCategory` | `Declaration=0`. Used in `TokenMeta.Categories` lists, always explicitly populated. |

---

## 3. Root Cause Analysis

**Why wasn't 1-based explicit values used from the start?**

Honest assessment: it was an oversight, not a considered tradeoff.

The evidence is the inconsistency. `RoutingFamily.None=0`, `GraphAnalysisKind.None=0`, and `QualifierAxis.None=0` all show that **someone was aware of the zero-default trap** and applied the sentinel pattern in those cases. But `Severity.Info=0`, `Prospect.Certain=0`, and `ConstraintStatus.Satisfied=0` show the protection wasn't applied consistently.

The pattern was:

1. **Applied correctly** when the developer had a structural "nothing here" concept that wanted to live at zero — the sentinel pattern was deliberate.

2. **Not applied** for semantic enums where every value is meaningful and there is no structural "nothing." Those defaulted to 0-based because C# defaults to 0-based.

`ActionSyntaxShape` was the acute case because:

- The enum was likely refactored from an earlier form that had a `None` sentinel

- When `None` was removed, the first real value inherited slot 0 without explicit reassignment

- The parser switch was exhaustive-named with CS8524 pragma (no wildcard throw), so a zero-default silently routed

The broader problem is that the 1-based-for-semantic-enums policy was never written down as a design rule. The `ActionSyntaxShape` fix established the pattern after the fact. The other risks above are the same class of bug waiting to manifest — most blocked by stubs today, all active risks once those stubs are filled in.

**Recommended policy going forward:**

> Every enum where ALL members are semantically meaningful (no structural "nothing" at 0) should use explicit 1-based integer values. The zero slot is unnamed. `default(Kind) = (Kind)0` throws or is structurally detectable.

>

> Enums with an explicit `None = 0` sentinel are correct as-is — that IS the intended zero behavior.

This policy, if encoded in a Roslyn analyzer or code review checklist, would have caught every risk identified in this audit at definition time.

---

*Report written by Frank. Fix `Severity` + `DiagnosticStage` first — they are in a live struct with a correctness gate. Fix `ConstraintStatus`, `Prospect`, and `FieldAccessMode` before the evaluator is implemented. Flag `TypeKind` as a pre-TypeChecker task.*

---

---

---

# Decision: ActionSyntaxShape — Explicit 1-Based Integer Values

**Date:** 2026-04-28

**Author:** George (Runtime Dev)

**Branch:** `precept-architecture`

**Commit:** `de2005a`

## Decision

`ActionSyntaxShape` members now carry explicit integer values starting at 1:

```csharp

public enum ActionSyntaxShape

{

    AssignValue     = 1,

    CollectionValue = 2,

    CollectionInto  = 3,

    FieldOnly       = 4,

}

```

`default(ActionSyntaxShape)` == `(ActionSyntaxShape)0` — an unnamed integer value with no named arm in any exhaustive switch.

## Why

The prior layout had `AssignValue = 0` (implicit). Any code that constructs an `ActionMeta` and leaves `SyntaxShape` as `default` silently routes through `ParseAssignValueStatement`. With `TreatWarningsAsErrors=true` and the existing `#pragma CS8524` pairs on each switch, the unnamed zero slot now causes a `SwitchExpressionException` at runtime instead of silent wrong-parser dispatch. The 1-based layout is self-enforcing structural protection at zero runtime cost.

## Alternatives Considered

- **Keep `None = 0` sentinel** — rejected because B1 removed it precisely to eliminate the dead "None" arm from the parser switch. Reinstating it defeats that cleanup.

- **Add a runtime guard in `ActionMeta` constructor** — redundant; the switch throws automatically without any extra code once zero is unnamed.

- **Leave values implicit** — leaves the silent zero-default trap in place; easy to trigger with a careless `ActionMeta(...)` call that omits the positional `SyntaxShape` argument in a future slice.

## Tradeoff Accepted

Serialized integer values for `ActionSyntaxShape` (if any external system ever persists them) shift by +1 relative to the old implicit layout. No current consumers serialize this enum, so the tradeoff is zero-cost in practice.

## Test Change

`Actions_ActionSyntaxShape_AllMembersHaveValue` (used `Enum.IsDefined`) was replaced by `Actions_ActionSyntaxShape_AllMembersAreNonZero` (asserts `(int)s > 0`). The new assertion directly enforces the 1-based invariant; the old assertion was weakened the moment `None=0` was removed, because `Enum.IsDefined(AssignValue)` is trivially true regardless of its integer value.

---

---

---

# George → Frank: B1–B7 Fixed — Re-review Requested

**From:** George (Runtime Dev)

**Date:** 2026-04-28

**Branch:** `feature/catalog-extensibility`

**Commit:** `5e5b2f9`

**Re:** Frank's BLOCKED verdicts in `frank-enum-deviation-review.md` and `frank-deep-deviation-review.md`

---

All 7 blocking issues are resolved. Build is clean (0 errors, 0 warnings, TreatWarningsAsErrors=true). All 2044 tests pass.

## What was changed
### B1 — `ActionSyntaxShape.None` removed (`src/Precept/Language/Action.cs`)

Removed `None = 0` and its XML doc comment. The enum now has exactly four members:

```csharp

public enum ActionSyntaxShape

{

    AssignValue,

    CollectionValue,

    CollectionInto,

    FieldOnly,

}

```
### B2 — Outer switch cleaned (`src/Precept/Pipeline/Parser.cs` — `ParseActionStatement`)

Removed the `ActionSyntaxShape.None => throw` arm. The outer switch now has exactly 4 arms with the `#pragma CS8524` pair intact.
### B3 — `ParseAssignValueStatement` inner switch

Replaced `_ => throw` with explicit named throw arms for every `ActionKind` not in the `AssignValue` shape: `Add`, `Remove`, `Enqueue`, `Dequeue`, `Push`, `Pop`, `Clear`. Added `#pragma CS8524` pair.
### B4 — `ParseCollectionValueStatement` inner switch

Replaced `_ => throw` with explicit named throw arms for `Set`, `Dequeue`, `Pop`, `Clear`. Added `#pragma CS8524` pair.
### B5 — `ParseCollectionIntoStatement` inner switch

Replaced `_ => throw` with explicit named throw arms for `Set`, `Add`, `Remove`, `Enqueue`, `Push`, `Clear`. Added `#pragma CS8524` pair.
### B6 — `ParseFieldOnlyStatement` inner switch

Replaced `_ => throw` with explicit named throw arms for `Set`, `Add`, `Remove`, `Enqueue`, `Dequeue`, `Push`, `Pop`. Added `#pragma CS8524` pair.
### B7 — `InvokeSlotParser` wildcard and comment (`Parser.cs`)

- Removed `_ => throw new ArgumentOutOfRangeException(...)` arm

- Added `#pragma warning disable CS8524` before the switch and `restore` after

- Replaced the misleading comment with: "CS8509 enforces named-value coverage here; #pragma CS8524 suppresses unnamed-integer noise."
### Companion test fix (`test/Precept.Tests/ActionsTests.cs`)

`Actions_ActionSyntaxShape_AllMembersHaveValue` was checking `NotBe((ActionSyntaxShape)0)`. With `None = 0` removed, `AssignValue` is now value 0, making that assertion a false positive. Updated to `Enum.IsDefined(meta.SyntaxShape)` which catches undefined integer values without falsely flagging `AssignValue`.

---

## Verification

```

dotnet build src/Precept/Precept.csproj  → succeeded, 0 errors, 0 warnings

dotnet test test/Precept.Tests/          → Passed! 2044/2044

```

Every catalog enum switch in the parser now follows the `BuildNode` gold standard:

explicit arms for all named members · `#pragma CS8524` to suppress unnamed-integer noise · no wildcard · CS8509 active.

---

---

---

# Decision: Catalog Extensibility Implementation Complete (PR #138)

**By:** George (Runtime Dev)

**Date:** 2026-04-28

**Branch:** feature/catalog-extensibility

**PR:** #138

## Status

All 7 slices implemented and passing (2044 tests).

## What Was Done
### Slice 1 — ExpressionBoundaryTokens derived from catalog

`ExpressionBoundaryTokens` is now composed of `StructuralBoundaryTokens` (6 fixed tokens: When, Because, Arrow, Ensure, EndOfSource, NewLine) plus `Constructs.LeadingTokens` — no more hardcoded construct-leading tokens in the parser. Adding a new construct kind with a new leading token automatically extends the expression boundary.
### Slice 2 — BuildNode wildcard removed (CS8509 enforced)

The `_ => throw` wildcard arm was removed from `BuildNode`. The switch now has one arm per `ConstructKind` with no default. CS8509 fires when a new `ConstructKind` is added without a corresponding assembly arm. `#pragma warning disable CS8524` suppresses the unnamed-integer variant (correct: we only guard against new named members, not arbitrary integer casts).
### Slice 3 — RoutingFamily enum + ConstructMeta field

`RoutingFamily` enum added: `None=0` (sentinel), `Header`, `Direct`, `StateScoped`, `EventScoped`. Added as required positional parameter to `ConstructMeta`. All 12 catalog entries populated. Tests assert no entry has `None` routing family.
### Slice 3b — DisambiguateAndParse exhaustive switches

Both EventScoped and StateScoped switches in `DisambiguateAndParse` now have:

- Named arms for each matching construct

- `null =>` arm calling `EmitAmbiguityAndSync` (handles "not found" from lookup)

- Explicit wrong-family throw arms (all non-matching ConstructKind values listed explicitly — no wildcard)

This gives CS8509-style protection: adding a new ConstructKind forces updating both switches.
### Slice 4 — ActionSyntaxShape enum + ActionMeta field + Actions.ByTokenKind

`ActionSyntaxShape` enum added: `None=0` (sentinel), `AssignValue`, `CollectionValue`, `CollectionInto`, `FieldOnly`. Added as required positional parameter to `ActionMeta`. All 8 catalog entries populated. `Actions.ByTokenKind` (FrozenDictionary) added for O(1) token→meta lookup.
### Slice 5 — ParseActionStatement two-level CS8509 refactor

`ParseActionStatement` replaced with a shape-dispatch entry + 4 private helpers:

- Outer switch on `meta.SyntaxShape` (no default — CS8509 on new shapes)

- Inner switch per helper on `meta.Kind` (no default — CS8509 on new actions within each shape)

- Unknown token path uses `Actions.ByTokenKind.TryGetValue` instead of a `default:` case
### Slice 6 — AccessModeKeywords derived from catalog

`TokenMeta.IsAccessModeAdjective` bool flag added (optional, default false). `Readonly` and `Editable` tokens tagged. `Tokens.AccessModeKeywords` FrozenSet derived from tagged entries. `ParseAccessModeKeywordDirect` uses `Tokens.AccessModeKeywords.Contains(...)` instead of `is TokenKind.Readonly or TokenKind.Editable`.

## Implementation Deviations from Plan

1. **`None=0` sentinels** — Both `RoutingFamily` and `ActionSyntaxShape` got a `None=0` member not in the original plan spec. This is required for the "all members have non-default value" tests to work correctly (testing `.NotBe((RoutingFamily)0)` requires `0` to be the sentinel, not a valid value).

2. **`#pragma warning disable CS8524`** — The plan described "CS8509 fires here." The actual compiler warning for exhaustive named-enum switches without wildcard is CS8524 (unnamed values) when the switch handles all named values explicitly. CS8509 fires when named values are missed. Both concerns are addressed: CS8524 is suppressed (unnamed integer casts aren't our concern), and CS8509 will fire if a new named ConstructKind is added without an arm.

## Cross-Surface Impact

Parser internal only. No grammar, language server, MCP, or sample changes needed.

---

---

---

# Decision: All Semantic Zero-Slot Enums Use Explicit 1-Based Values

**Date:** 2026-04-28

**Author:** George (Runtime Dev)

**Status:** Implemented — commit `d300b26` on `precept-architecture`

---

## Decision

Every enum in `src/Precept/` where ALL members are semantically meaningful (no structural "nothing" at 0) must use explicit 1-based integer values. The zero slot stays unnamed. `default(Kind)` = `(Kind)0` = unnamed = throws `SwitchExpressionException` rather than silently routing to a wrong member.

This is a blanket structural invariant, not per-enum policy. Once established, new enums start at 1 by default.

---

## Enums Fixed

| Enum | File | Member count | Zero-slot risk |

|------|------|:---:|------|

| `Severity` | `Language/Diagnostic.cs` | 3 | `default(Diagnostic)` gives `Severity.Info`; `HasErrors` returns false on zero-constructed diagnostics |

| `DiagnosticStage` | `Language/Diagnostic.cs` | 5 | Zero-constructed diagnostics attributed to `Lex` stage |

| `ConstraintStatus` | `Runtime/Inspection.cs` | 3 | Zero-initialized result marks violated constraint as `Satisfied` — inverts Precept's core correctness guarantee |

| `Prospect` | `Runtime/Inspection.cs` | 3 | Zero-initialized prospect presents an impossible transition row as `Certain` to fire |

| `FieldAccessMode` | `Runtime/SharedTypes.cs` | 2 | Zero-initialized mode silently locks writable fields as `Read` |

| `TypeKind` | `Language/TypeKind.cs` | 26 | Zero-initialized kind silently treats unknown types as `String` |

---

## Pattern Applied

```csharp

// Before (unsafe — String = 0 is a valid member)

public enum TypeKind { String, Boolean, Integer, ... }

// After (safe — (TypeKind)0 is unnamed, throws SwitchExpressionException)

public enum TypeKind

{

    String  = 1,

    Boolean = 2,

    Integer = 3,

    ...

}

```

---

## Rationale

- **Silent wrong-branch routing is the failure mode.** When an enum member sits at 0 and a struct gets zero-initialized (field omitted, `default(T)`, array element, unset out-parameter), the switch routes silently to that member. The bug is invisible — no exception, no compiler warning, semantically wrong behavior.

- **The 1-based layout is free.** No behavioral change when all constructors set the field explicitly. The only observable difference is that `(T)0` now has no named arm — which is exactly the trip-wire we want.

- **`SwitchExpressionException` is the right signal.** It fires immediately at the misuse site with a clear message, not downstream after data has propagated. This is strictly better than a silent wrong-value path.

- **Consistency with `ActionSyntaxShape`.** That enum was made 1-based in the prior session (commit `de2005a`). Applying the same rule to all other enums closes the audit uniformly.

---

## Alternatives Rejected

- **Leave at 0, add runtime guards:** Adds noise at every consumer, doesn't eliminate the silent path.

- **Add `None = 0` sentinel:** The sentinel itself becomes a valid arm; switch exhaustiveness can mask it. The unnamed-slot approach is strictly stronger.

- **Per-enum risk assessment:** Inconsistent. The rule is simple enough to apply universally. Dormant enums (`ConstraintStatus`, `Prospect`, `FieldAccessMode`, `TypeKind`) are safer to fix now than when the evaluator ships and they're live.

---

## Impact on Tests

No tests used `(EnumName)0` or `default(EnumName)` — no test changes required. All 2044 tests passed without modification.

---

## Going Forward

New enums in `src/Precept/` where all members are semantically meaningful must start at 1. The canonical check: *"Is there a valid program state represented by integer 0 for this type?"* If no, start at 1.

---

---

---
