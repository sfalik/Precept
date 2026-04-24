# compiler/ — Pipeline Stage Blueprints

Implementation blueprints for each stage of the Precept v2 compiler pipeline. Each doc follows a consistent structure: Overview → Design Principles → Architecture → domain-specific sections → Error Recovery → Consumer Contracts → Deliberate Exclusions → Cross-References → Source Files.

## Pipeline Order

The compiler is a linear five-stage pipeline. Read the stage docs in this order:

```
Source string → Lexer.Lex → TokenStream → Parser.Parse → SyntaxTree → TypeChecker.Check → TypedModel → GraphAnalyzer.Analyze → GraphModel → ProofEngine.Prove → ProofModel
```

| Stage | Document | Key decisions | Status |
|-------|----------|---------------|--------|
| 1. Lexer | [lexer.md](lexer.md) | L1–L10, R1–R5, D1 | Draft |
| 2. Parser | [parser.md](parser.md) | P1–P7 | Draft |
| 3. Type Checker | [type-checker.md](type-checker.md) | T1–T9 | Draft |
| 4. Graph Analyzer | _(not yet written)_ | — | Planned |
| 5. Proof Engine | _(not yet written)_ | — | Planned |

## Cross-Cutting Infrastructure

| Document | Purpose | Status |
|----------|---------|--------|
| [pipeline-artifacts-and-consumer-contracts.md](pipeline-artifacts-and-consumer-contracts.md) | How pipeline stages connect — artifact types, consumer contracts, LS integration strategy | Draft |
| [diagnostic-system.md](diagnostic-system.md) | Diagnostic codes, severity, message templates, audience model, stage attribution | Draft |
| [literal-system.md](literal-system.md) | How literals flow through every pipeline stage — lexer segmentation, parser assembly, type-checker resolution, evaluator materialization | Draft |

## Source Code

All pipeline stages live under `src/Precept.Next/Pipeline/`. Tests are in `test/Precept.Next.Tests/`.
