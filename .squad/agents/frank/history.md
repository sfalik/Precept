## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable guidance for parser, catalog, type-checker, and tooling work.
- Durable active baseline: catalogs remain the language truth; generic consumer flow should dispatch by `SlotValue` shape instead of construct identity; Option F-style generic parse output is acceptable for consumers, but any accessor layer stays deferred until a concrete need exists.

## Learnings

- When all "Open Decisions" in a working doc are locked, collapse the section immediately — replace the full options/rationale body with a single cross-reference line. The locked section is the canonical record; the open section should not persist as a ghost of deliberation once decisions are final. Verdicts + brief rationale belong in the locked list; full options analysis does not.

- Keep catalog metadata as the single language source of truth; parser/tooling sets and per-member consumer switches are mirrored truth unless the catalog cannot express the distinction.
- Grammar `Tag(...)` captures are the slot system in the radical parser design; do not reintroduce a parallel `ConstructMeta.Slots` field once the grammar tree carries named captures.
- The surviving argument for incremental parser change over rebuild is design-risk sequencing, not schedule; AI-assisted throughput weakens time-cost arguments but does not erase unresolved architecture gaps.
- Outcomes require metadata when outcome-level meaning cannot be recovered from token categories alone; `no transition` is the durable example of composition that must live in catalog data.
- The catalog-driven thesis now reaches the upstream pipeline too: lexer tables are already catalog-fed, the radical parser is mostly catalog-driven above the Pratt kernel, and the builder is the cleanest proof-of-concept stage if `ModelContribution` metadata is added.
- `[HandlesCatalogExhaustively]` + `[HandlesCatalogMember]` on consumer stubs are a false promise once Option F's generic dispatch lands. When consumers don't switch per-`ExpressionFormKind`, there is nothing to enforce exhaustiveness on — the annotations lie. Remove them at the source (the three pipeline stubs) and remove the reflection-based xUnit Group 2 tests that enforced them. The attribute type definitions stay because they remain valid on catalog types themselves. Any test that enforces annotation presence must be deleted alongside the annotations; orphan tests asserting dead contracts are worse than no tests.
- Per-consumer implementation recommendations must distinguish "what metadata exists" (§3 analysis) from "what to build and how" (§6 guidance). The former is a snapshot; the latter is a playbook. Mixing them blurs the signal.
- The Pratt expression parser/evaluator is genuinely irreducible and appears in three consumers (parser, type checker, evaluator). Don't pretend it's catalog-drivable — accept the recursive structure and focus on making the operation semantics within it metadata-driven.
- Semantic slot constraints belong ON the `Tag` node, not beside it. The "couples syntax and semantics" objection fails because the radical design already expanded `ConstructMeta` beyond pure parse mechanics (locked decision #4). The `Tag` IS the slot; its semantic expectations are part of the same contract. A parallel array indexed by slot name is a split-brain anti-pattern that invites desynchronization.
- Option (c) — deriving slot type expectations from existing catalogs — fails for any construct-specific fact (guard = Boolean, name = Identifier). These have no derivation source; they ARE the language definition and must be declared explicitly. Derivation-from-existing only works for trivially obvious cases.
- Cross-construct constraints (reference resolution: "target state must exist") are structurally different from per-slot type expectations and require their own `CrossConstructConstraint[]` on `ConstructMeta`. Don't try to shoehorn relationship validation into per-slot metadata — it spans slots across constructs.

- AST node type declarations (`src/Precept/Pipeline/SyntaxNodes/`) are now DELETED — 38 `.cs` record files (main + Expressions subfolder). The old guidance to "preserve them as the AST contract" is superseded; the AST is being rebuilt catalog-driven from scratch.
- `AstNodeTests.cs` deleted alongside the nodes — it tested the structural contract of types that no longer exist.
- Compilation fallout fixed in three files: `SyntaxTree.cs` (stripped `PreceptHeaderNode?` + `ImmutableArray<Declaration>` parameters, now holds only `ImmutableArray<Diagnostic>`), `Parser.cs` (removed `using Precept.Pipeline.SyntaxNodes` + updated constructor call), `GraphAnalyzer.cs` (removed same `using` + dropped `Expression expression` parameter from the stub method).
- Build result after deletions: 0 errors, 0 warnings.

## Recent Updates

### 2026-05-03T05:08:28Z — AST clean-slate deletion recorded
- Deleted the entire src/Precept/Pipeline/SyntaxNodes/ tree (38 files including Expressions/) plus test/Precept.Tests/AstNodeTests.cs.
- SyntaxTree.cs, Parser.cs, and GraphAnalyzer.cs were trimmed to remove the remaining SyntaxNode references; build result is 0 errors, 0 warnings.
- Supersedes the earlier "preserve SyntaxNodes as the AST contract" note: the AST surface is now intentionally absent until the catalog-driven replacement lands.


### 2026-05-03T05:13:00Z — Option F AST stub implemented

**Files created:**
- `src/Precept/Pipeline/SlotValue.cs` — discriminated union with abstract `SlotValue` base + 17 sealed subtypes, one per `ConstructSlotKind` catalog member. Naming adjustments: `Language.Type` → `TypeMeta` (no bare `Type` class exists in `Precept.Language`); used `TypeMeta` for both `TypeExpressionSlot.Type` and `ArgumentListSlot.Args` tuple element. Expression-carrying stubs (`ComputeExpressionSlot`, `GuardClauseSlot`, `OutcomeSlot`, `EnsureClauseSlot`, `RuleExpressionSlot`) hold only `SourceSpan` with `// TODO: add typed Expression tree` comments.
- `src/Precept/Pipeline/ParsedConstruct.cs` — `sealed record ParsedConstruct(ConstructMeta Meta, ImmutableArray<SlotValue> Slots, SourceSpan Span)`. Uses `ConstructMeta` (actual type name) not the task's placeholder `Construct`.

**Files updated:**
- `src/Precept/Pipeline/SyntaxTree.cs` — added `ImmutableArray<ParsedConstruct> Constructs` parameter.
- `src/Precept/Pipeline/Parser.cs` — updated stub constructor call to pass `ImmutableArray<ParsedConstruct>.Empty`.
- `src/Precept/Pipeline/GraphAnalyzer.cs` — `AnalyzeExpression()` now takes `ParsedConstruct construct` parameter.

**Build result:** 0 errors, 0 warnings.


- Deleted Parser.cs, Parser.Declarations.cs, Parser.Expressions.cs implementation (≈28KB of parsing logic).
- Replaced with a 35-line stub matching TypeChecker pattern: returns empty SyntaxTree with no diagnostics.
- Preserved all SyntaxNode type declarations (SyntaxNodes/ folder) — they remain the AST contract.
- Deleted 5 test files testing parser internals/behavior (ExpressionParserTests, ParserInfrastructureTests, SlotParserTests, ParserTests, SampleFileIntegrationTests).
- Trimmed 3 tests referencing deleted Parser fields from ConstructsTests, TokenMetaMemberNameTests, ExpressionFormCoverageTests.
- Final state: build clean, 2603 tests pass (2348 + 255).

### 2026-05-03T02:52:51Z — Catalog-driven pipeline follow-through recorded
- Scribe merged Frank's consumer-architecture note plus Shane's accessor-layer ruling into the canonical ledger: keep consumers generic, keep MCP above raw parse output, and treat any accessor layer as YAGNI until a real caller proves otherwise.
- Scribe also recorded Frank's upstream coverage pass: lexer/parser/builder now sit inside the same catalog-driven pipeline thesis, with the builder identified as the strongest candidate for a first generic proof-of-concept stage.
- Detailed prior active-history entries were compacted into `history-archive.md` during this pass to bring Frank back under the 15 KB gate.

### 2026-05-03T01:34:25Z — Radical AST options note recorded
- The pending-owner-ruling record now keeps Option F (generic `ParsedConstruct` internals + thin typed accessors at boundaries) as the preferred radical AST path, with source generation as the explicit fallback if ergonomics win.

### 2026-05-03T01:07:30Z — Outcomes catalog reversal recorded
- The durable parser/type-checker rule remains: outcomes use the two-level catalog pattern while retaining `OutcomeNode` as the syntax-layer DU because `no transition` is an outcome-level abstraction that token categories cannot enumerate by themselves.

### 2026-05-02T22:22:24Z — Iteration 11 audit session recorded
- Keep the audit baseline in mind: the doc/catalog gap set now centers on declaration-shape metadata lag, queue-by clarification, and the canonical checker implementation gate already locked in `docs/compiler/type-checker.md`.
