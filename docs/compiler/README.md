# compiler/ — Pipeline Stage Blueprints

Implementation blueprints for each stage of the Precept compiler pipeline. Each doc follows the 16-section canonical template: Status → Overview → Responsibilities and Boundaries → Right-Sizing → Inputs and Outputs → Architecture → Component Mechanics → Dependencies and Integration Points → Failure Modes and Recovery → Contracts and Guarantees → Design Rationale and Decisions → Innovation → Open Questions / Implementation Notes → Deliberate Exclusions → Cross-References → Source Files.

## Pipeline Order

The compiler is a linear six-stage pipeline. Read the stage docs in this order:

```
Source string → Lexer.Lex → TokenStream → Parser.Parse → ConstructManifest → NameBinder.Bind → SymbolTable → TypeChecker.Check → SemanticIndex → GraphAnalyzer.Analyze → StateGraph → ProofEngine.Prove → ProofLedger → Compiler.Compile → Compilation
```

| Stage | Document | Doc maturity | Impl state |
|-------|----------|--------------|------------|
| 1. Lexer | [lexer.md](lexer.md) | Full | Implemented |
| 2. Parser | [parser.md](parser.md) | Full | Implemented |
| 3. Name Binder | [name-binder.md](name-binder.md) | Full | Implemented |
| 4. Type Checker | [type-checker.md](type-checker.md) | Full | Stub |
| 5. Graph Analyzer | [graph-analyzer.md](graph-analyzer.md) | Full | Stub |
| 6. Proof Engine | [proof-engine.md](proof-engine.md) | Full | Stub |

## Cross-Cutting Infrastructure

| Document | Purpose | Doc maturity |
|----------|---------|--------------|
| [compiler-and-runtime-design.md](../compiler-and-runtime-design.md) | How pipeline stages connect — artifact types, consumer contracts, LS integration strategy | Draft |
| [diagnostic-system.md](diagnostic-system.md) | Diagnostic codes, severity, message templates, audience model, stage attribution | Draft |
| [literal-system.md](literal-system.md) | How literals flow through every pipeline stage — lexer segmentation, parser assembly, type-checker resolution, evaluator materialization | Draft |
| [tooling-surface.md](tooling-surface.md) | TextMate grammar generation, semantic token two-pass design, completion filtering | Full |

## Source Code

All pipeline stages live under `src/Precept/Pipeline/`. Tests are in `test/Precept.Tests/`.
