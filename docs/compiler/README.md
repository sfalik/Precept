# compiler/ — Pipeline Stage Blueprints

Implementation blueprints for each stage of the Precept v2 compiler pipeline. Each doc follows a consistent structure: Overview → Design Principles → Architecture → domain-specific sections → Error Recovery → Consumer Contracts → Deliberate Exclusions → Cross-References → Source Files.

**Design decisions placement:** Each stage doc has a dedicated `## Design Decisions` section that catalogs all decisions as an auditable index. The type checker's section (T1–T9) contains full standalone rationales. The lexer (L1–L10, R1–R5, D1) and parser (P1–P7) sections are catalog tables with cross-references to the inline discussions where the rationale lives. Both forms serve the same purpose: a single place to verify that every decision is documented.

## Pipeline Order

The compiler is a linear five-stage pipeline. Read the stage docs in this order:

```
Source string → Lexer.Lex → TokenStream → Parser.Parse → SyntaxTree → TypeChecker.Check → TypedModel → GraphAnalyzer.Analyze → GraphResult → ProofEngine.Prove → ProofModel
```

| Stage | Document | Status |
|-------|----------|--------|
| 1. Lexer | [lexer.md](lexer.md) | Draft |
| 2. Parser | _(pending clean-room redesign)_ | Planned |
| 3. Type Checker | _(pending clean-room redesign)_ | Planned |
| 4. Graph Analyzer | _(not yet written)_ | Planned |
| 5. Proof Engine | _(not yet written)_ | Planned |

## Cross-Cutting Infrastructure

| Document | Purpose | Status |
|----------|---------|--------|
| [compiler-and-runtime-design.md](../compiler-and-runtime-design.md) | How pipeline stages connect — artifact types, consumer contracts, LS integration strategy | Draft |
| [diagnostic-system.md](diagnostic-system.md) | Diagnostic codes, severity, message templates, audience model, stage attribution | Draft |
| [literal-system.md](literal-system.md) | How literals flow through every pipeline stage — lexer segmentation, parser assembly, type-checker resolution, evaluator materialization | Draft |
| _(tooling-surface.md — not yet written)_ | Syntax highlighting category system, TextMate scope policy for v2 tokens (`~`, `~=`, `!~`, single-quoted typed constants), semantic token two-pass design | Planned |

## Source Code

All pipeline stages live under `src/Precept.Next/Pipeline/`. Tests are in `test/Precept.Next.Tests/`.
