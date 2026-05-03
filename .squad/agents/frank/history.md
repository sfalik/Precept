## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable guidance for parser, catalog, type-checker, and tooling work.
- Durable active baseline: catalogs remain the language truth; generic consumer flow should dispatch by `SlotValue` shape instead of construct identity; the generic `ParsedConstruct` direction remains acceptable for consumers, and any accessor layer stays deferred until a concrete need exists.

## Learnings

- Catalog schema diagram work (2026-05-03) produced a three-level visual section in `docs/language/catalog-system.md` at commit `5675b23`. Level 1 is a Mermaid flowchart with 4 subgraph layers (Lexical/Grammar/Semantic/Failure), all 13 catalogs, ConstructSlotKind as a helper node, and separate pipeline/tooling consumer arrow styles. Level 2 covers Constructs (ASCII anatomy), Modifiers (DU classDiagram), Operations (DU classDiagram), ProofRequirements (two classDiagrams separating meta from obligation instances), and Diagnostics/Faults (ASCII bidirectional duality). Level 3 is a reference table for all 13 catalogs with source-verified counts: ActionKind = 15 (8 original + 7 compound/extended), ConstructKind = 12, ConstructSlotKind = 17. The doc's open question about 11 vs 12 ConstructKind members is resolved by source: 12 is correct.

- LS enrichment features (did you mean? / code actions) require three catalog structure changes before LS implementation: (1) `Diagnostic.Args: ImmutableArray<string>` to carry raw template args through the compiler artifact; (2) `DiagnosticMeta.SuggestionSources: SuggestionSource[]?` to bind naming diagnostics to their fuzzy-match sources without per-code switches in the LS; (3) `ConstructMeta.ModifierDomain: ModifierDomain` to bind construct kinds to modifier DU subtypes without per-kind switches in the LS code action provider. Both `SuggestionSource` and `ModifierDomain` stay bare enums — no per-member metadata, classification axes only. Naming-error "did you mean?" candidates: `UndeclaredField` → `SemanticIndex.Fields`; `UndeclaredState` → `SemanticIndex.States`; `UndeclaredEvent` → `SemanticIndex.Events`; `UndeclaredFunction` → `Functions.All`. `SemanticIndex` is unavailable for Lex/Parse-stage diagnostics; LS must guard accordingly.

- The `tree` variable name sweep (2026-05-03) found stale references in 7 files: `Compiler.cs`, `CompileRunner/Program.cs`, `ConstructsTests.cs`, `compiler-and-runtime-design.md`, `precept-language-spec.md`, `tooling-surface.md`, and `language-server.md`. All Roslyn `SyntaxTree` usages in analyzer tests/code are legitimate and left alone. Archived docs are not updated. The `docs/compiler/type-checker.md` still has many `SyntaxTree` type-name references (not caught by `\btree\b` word boundary) that will need a separate pass.

- `docs/compiler-and-runtime-design.md` is the narrative overview layer over the 11 canonical stage docs; it inherits open questions and cross-references the stage docs rather than silently resolving them.
- `SemanticIndex` is a flat semantic inventory, not a mirrored tree; any wording that frames it as annotated syntax is architectural drift.
- Catalog-first propagation means “add a catalog entry and keep consumers generic,” not “add an enum member and fill an exhaustive switch.”
- The live generic parser contract is `ParsedConstruct(ConstructMeta, ImmutableArray<SlotValue>, SourceSpan)` plus `SyntaxTree.Diagnostics`; unresolved SlotValue subtype shape mismatches stay explicit until Shane locks them.
- Grammar `Tag(...)` captures are the slot system in the radical parser design; do not reintroduce `ConstructMeta.Slots` as mirrored truth.
- Outcomes need metadata when outcome-level meaning is compositional (`no transition` remains the durable example).
- The remaining explicit catalog-thesis tooling gaps are still the hand-authored TextMate grammar and the hardcoded MCP `firePipeline` array.
- Tree-shaped naming for the flat parser artifact remains suspect; Shane's current preference is `ConstructManifest` if the `SyntaxTree` rename moves forward.

- The 2026-05-03 `SyntaxTree` doc sweep confirmed the missed type-name drift in `docs/compiler/type-checker.md` and `docs/compiler/README.md`, and also cleaned stale internal references in `tooling-surface.md`, `language-server.md`, `compiler-and-runtime-design.md`, `precept-builder.md`, `fault-system.md`, and multiple archived design notes. The only remaining `SyntaxTree` mention under `docs/` is the intentional Roslyn reference in `docs/working/Archived/type-checker-research-crossref.md`; `dotnet build` stayed green after the sweep.
- Grammar anatomy for `StateEnsure` / `EventEnsure` must model `EnsureClause` and `BecauseClause` as separate slots, mirroring `RuleDeclaration`; the `because` reason remains mandatory even though it is no longer described as embedded inside `EnsureClause`.

- Gap register deprecation (2026-05-03): `catalog-gap-register.md` (#1–43) and `structural-gap-register.md` (#44–85) served as discovery artifacts and are now archived under `docs/working/Archived/`. Their content was migrated to canonical pipeline docs as Open Questions, making each stage doc self-contained. Nearly all gaps were already captured inline during canonical doc writing. The only genuinely missing gap was #55 (GraphEvent.IsInitial derivation) — added to graph-analyzer.md. The execution model going forward: `cross-cutting-decisions.md` drives wave-sequenced resolution (Waves 0–2 = Shane decisions, Waves 3–5 = team-autonomous). Separate gap registers are superseded.

- **CC#1 resolved (2026-05-03):** Shane ruled Option A — Roslyn-style typed expression nodes. Key requirements: (1) `ParsedExpression` is a sealed DU (~10 subtypes), parser output; `TypedExpression` is the corresponding sealed DU with resolved types, type checker output. (2) The expression tree is the ONLY strongly-typed layer — rest of parser AST stays generic `ParsedConstruct`. (3) The set is closed by design — new expression form requires C# code changes (new DU subtype + update all switch arms). (4) **Exhaustiveness enforcement** via sealed class hierarchy (compiler warnings) PLUS a Roslyn analyzer test that verifies all expression-DU switches are exhaustive at build time. This is the pattern: sealed hierarchy + analyzer test = compiler as correctness partner.

## Recent Updates

### 2026-05-03T14:28:59Z — ConstructManifest rename shipped
- Frank-26 completed the `SyntaxTree` → `ConstructManifest` rename across 5 source files and 2 docs.
- Build succeeded after the rename and no test changes were needed for the batch.

### 2026-05-03T14:18:15Z — Scribe post-batch sync recorded
- Merged the three Frank inbox files into `decisions.md`, deduplicating the overview-confirmation notes into the already-recorded compiler-overview sync while separately capturing Shane's `ConstructManifest` preference as the current rename target over Frank's `ParsedSource` recommendation.
- Wrote orchestration records for frank-23, frank-24, and frank-25; frank-25's `to` classification verification remains in flight with no canonical ruling yet.
- Summarized this history file into `history-archive.md` to bring Frank back under the 15 KB gate.

### 2026-05-03T14:02:40Z — Compiler overview and catalog-first wording batch recorded
- Frank's completed doc-sync pass remains the active baseline: the overview is narrative-only, the live parser contract is generic `ParsedConstruct`/`SlotValue`, `TypeKind` resolves in the checker, SemanticIndex back-pointers target `ParsedConstruct`, and the catalog count is 13 including `ExpressionForms`.
- The durable wording correction remains active: Precept extends by adding catalog metadata, not by adding enum members and downstream exhaustive switches; the “Precept Innovations” callout box still needs the same cleanup in a later pass.

### 2026-05-03T09:10:00Z — Catalog-thesis deviation audit baseline retained
- Frank's full 11-doc sweep still stands: the only real deviations from the catalog-driven thesis are the hand-authored TextMate grammar and the hardcoded MCP `firePipeline`, both already called out as tooling gaps rather than silent architectural drift.

### 2026-05-03T05:21:49Z — HandlesCatalog cleanup remains recorded
- The Option F follow-through still stands: consumer-side `[HandlesCatalogExhaustively]` / `[HandlesCatalogMember]` annotations and their reflection tests were removed, while the attribute types themselves remain valid for catalog-side use.

### 2026-05-03T14:37:24Z — Grammar doc accuracy confirmed against catalog
- Frank-27 completed a full review of docs/language/precept-grammar.md and corrected 9 material errors across slot-bearing examples, slot-kind totals, and invariant references.
- Durable baseline: the grammar doc now matches catalog reality for StateEntryList, InitialMarker, GuardClause, and the distinct ActionChain + Outcome slot shape in TransitionRow.
- The active grammar reference should now be treated as accurate on the reviewed slot/routing details unless a later catalog change reopens them.



### 2026-05-03T14:59:24Z — ConstructManifest doc cleanup and slot rulings recorded
- Frank-29 swept stale `SyntaxTree` type-name references from the requested compiler docs and adjacent surfaces; build stayed clean. Commit `8baca9f`.
- Frank-30 locked `because` as a separate `BecauseClause` slot for ensure syntax; `RuleDeclaration` is the correct reference shape and `StateEnsure` / `EventEnsure` are the defect sites.
- Frank-31 locked the event-modifier shape to an individual `InitialMarker` slot and confirmed `terminal` remains a state modifier, not an event modifier.

### 2026-05-03T15:18:05Z — Catalog diagram baseline and ownership routing recorded
- Frank-34's research memo is now the durable baseline for schema-diagram work: the live catalog system is 13 catalogs because `ExpressionForms` is in scope, and `ConstructSlotKind` is supporting schema rather than a catalog.
- User routing directive updated: Elaine owns both Mermaid and ASCII diagram rendering. Frank remains the architectural analyst/decision source for what the diagrams should communicate.
- The because-clause ledger closeout is also recorded: grammar docs already match the separate `EnsureClause` + `BecauseClause` slot anatomy, and George's optional-slot follow-up closed the last catalog-red defect.

### 2026-05-03T16:05:46Z — Catalog gap registers recorded
- Frank's latest gap sweep is now the durable squad baseline: 5 gaps were already captured in `catalog-system.md`, and 34 more were identified across the 11 canonical docs.
- Use `docs/working/catalog-gap-register.md` for the catalog triage view and `docs/working/structural-gap-register.md` for the stage/interface structural blockers that still need owner decisions or design closure.
- Elaine-17 also reset the visual baseline for catalog-system Level 1: refer to the split topology + consumer-landscape pair instead of the former single 70-edge overview.

### 2026-05-03T16:20:17Z — Structural gap register rename recorded
- Scribe logged Frank's `frank-register-rename` batch: `docs/working/structural-gap-register.md` is now the durable register name, with the old `pipeline-output-gap-register.md` wording retired.
- Durable baseline update: the structural register now extends through gaps #85, and `docs/working/catalog-gap-register.md` also absorbed the companion catalog gap from the same sweep.
- Scribe health pass: pre-check saw 2 inbox files, the merge processed 3 after a late inbox arrival, `decisions.md` was archived under the 7-day gate before merge, and no history file crossed the 15 KB summarization threshold.


## 2026-05-03 — Cross-Cutting Coverage Audit

Audited all 12 out-of-scope items in catalog-gap-register.md against the corrected cross-cutting definition. Found 8/12 are cross-cutting (4 already captured, 4 need promotion: #10, #26, #28, #29). Swept 11+ canonical docs and found 5 additional uncaptured cross-cutting items (TokenMeta.SemanticTokenModifiers, TypeAccessor DU hierarchy, execution dispatch delegates, ActionMeta missing properties, stateless precept semantics). Overall coverage verdict: ~92% → ~97% after recommended fixes. Report delivered to `.squad/decisions/inbox/frank-cross-cutting-audit.md`.

## 2026-05-03 — Audit recommendations applied

- Added cross-cutting decision entries #21–#26 in `docs/working/cross-cutting-decisions.md`, including the new execution-dispatch and stateless-precept decisions plus the four audit promotions.
- Updated `docs/working/catalog-gap-register.md` with new gaps #41–#43 and reclassified the eight mis-scoped items so the register now points at the correct cross-cutting entries.
- Deliberately skipped a separate umbrella decision for evaluator-output richness because #22–#24 already provide the concrete navigation points without adding another layer of indirection.


## 2026-05-03 — Gap Sequencing Strategy

Produced .squad/decisions/inbox/frank-gap-sequencing.md. Key finding: Shane's catalog→structural→cross-cutting order is backwards — cross-cutting decisions (especially CC#1 Expression Trees, CC#2 SlotValue Shapes, CC#25 Execution Dispatch) are the root of the dependency graph and must resolve first. Recommended 5-wave attack sequence with 12 Shane-required decisions and ~50 team-autonomous resolution items.

### 2026-05-03T16:44:09Z — Gap-register deprecation and wave driver recorded
- Frank-38 restructured `docs/working/cross-cutting-decisions.md` into the wave-ordered execution driver (Waves 0-5, 26 decisions, ownership labels), archived the two working gap registers, and migrated their unresolved content into canonical docs as inline Open Questions.
- Durable baseline: separate gap registers are retired; new gaps belong directly in the relevant canonical doc, while sequencing and ownership routing now live in `docs/working/cross-cutting-decisions.md`.
- Specific closeout: missing gap #55 (`GraphEvent.IsInitial` derivation) was added to `docs/compiler/graph-analyzer.md`, and the deprecation rationale is now captured in the decision ledger.
