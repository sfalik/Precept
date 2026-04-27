# compiler/ — Pipeline Stage Blueprints

Implementation blueprints for each stage of the Precept compiler pipeline. Each doc follows the 16-section canonical template: Status → Overview → Responsibilities and Boundaries → Right-Sizing → Inputs and Outputs → Architecture → Component Mechanics → Dependencies and Integration Points → Failure Modes and Recovery → Contracts and Guarantees → Design Rationale and Decisions → Innovation → Open Questions / Implementation Notes → Deliberate Exclusions → Cross-References → Source Files.

## Pipeline Order

The compiler is a linear five-stage pipeline. Read the stage docs in this order:

```
Source string → Lexer.Lex → TokenStream → Parser.Parse → SyntaxTree → TypeChecker.Check → SemanticIndex → GraphAnalyzer.Analyze → StateGraph → ProofEngine.Prove → ProofLedger → Compiler.Compile → Compilation
```

| Stage | Document | Status |
|-------|----------|--------|
| 1. Lexer | [lexer.md](lexer.md) | Draft |
| 2. Parser | [parser.md](parser.md) | Draft |
| 3. Type Checker | [type-checker.md](type-checker.md) | Stub |
| 4. Graph Analyzer | [graph-analyzer.md](graph-analyzer.md) | Stub |
| 5. Proof Engine | [proof-engine.md](proof-engine.md) | Stub |

## Cross-Cutting Infrastructure

| Document | Purpose | Status |
|----------|---------|--------|
| [compiler-and-runtime-design.md](../compiler-and-runtime-design.md) | How pipeline stages connect — artifact types, consumer contracts, LS integration strategy | Draft |
| [diagnostic-system.md](diagnostic-system.md) | Diagnostic codes, severity, message templates, audience model, stage attribution | Draft |
| [literal-system.md](literal-system.md) | How literals flow through every pipeline stage — lexer segmentation, parser assembly, type-checker resolution, evaluator materialization | Draft |
| [tooling-surface.md](tooling-surface.md) | TextMate grammar generation, semantic token two-pass design, completion filtering | Stub |

## Source Code

All pipeline stages live under `src/Precept/Pipeline/`. Tests are in `test/Precept.Tests/`.
