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

---

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
