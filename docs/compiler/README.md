# compiler/ — Pipeline Stage Blueprints

> [!IMPORTANT]
> **Before implementing any pipeline stage, read [`compiler-and-runtime-design.md` — § Non-Negotiable Rules](../compiler-and-runtime-design.md#non-negotiable-rules).** Key rules:
>
> - New tokens, operators, and constructs go in the catalog — not inline sets or hardcoded conditions
> - Parser lookahead uses `Constructs.ByLeadingToken` and `ConstructMeta.Entries` — derived from catalog, not re-encoded
> - Operator binding power comes from the Operators catalog `BindingPower` property — not hardcoded in `ParseExpression`
> - SemanticIndex is a flat semantic inventory — NOT a structural mirror of the parse tree
>
> See also: **[catalog-system.md — § Architectural Identity](../language/catalog-system.md#architectural-identity-metadata-driven)** for the full catalog pattern and enforcement model; **[catalog-driven-checklist.md](../contributing/catalog-driven-checklist.md)** for the pre-implementation verification gate.

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
| 4. Type Checker | [type-checker.md](type-checker.md) | Full | Implemented |
| 5. Graph Analyzer | [graph-analyzer.md](graph-analyzer.md) | Full | Implemented |
| 6. Proof Engine | [proof-engine.md](proof-engine.md) | Full | Implemented (base); prove-or-reject MVP designed |

## Cross-Cutting Infrastructure

| Document | Purpose | Doc maturity |
|----------|---------|--------------|
| [compiler-and-runtime-design.md](../compiler-and-runtime-design.md) | How pipeline stages connect — artifact types, consumer contracts, LS integration strategy | Canonical design |
| [soundness-and-coverage.md](soundness-and-coverage.md) | How the prove-or-reject guarantee is made sound and its measurable boundary — the decision rationale (why prove-or-reject, not a fault-floor), the coverage matrix (`FaultCode` × obligation-creation site × disposition) and the bucket test for new failing cases, the legible certificate format, the witness, the deferred independent re-checker, and coverage of the known fail-open holes (cross-cuts proof engine, graph analyzer, type checker, fault correspondence) | Design (skeleton) |
| [diagnostic-system.md](diagnostic-system.md) | Diagnostic codes, severity, message templates, audience model, stage attribution | Full |
| [literal-system.md](literal-system.md) | How literals flow through every pipeline stage — lexer segmentation, parser assembly, type-checker resolution, evaluator materialization | Draft |
| [tooling-surface.md](tooling-surface.md) | TextMate grammar generation, semantic token two-pass design, completion filtering | Full |
| [grammar-generator.md](grammar-generator.md) | Grammar generator algorithm, pattern templates, structural composition, catalog gap | Full |

## Source Code

All pipeline stages live under `src/Precept/Pipeline/`. Tests are in `test/Precept.Tests/`.

## Relationship to Other Docs

- [`../compiler-and-runtime-design.md`](../compiler-and-runtime-design.md) — the architectural spine; covers stage boundaries, artifact types, and the non-negotiable rules every stage doc respects. Always read before the per-stage docs.
- [`../language/README.md`](../language/README.md) — the language surface this pipeline implements. Catalog definitions in `docs/language/catalog-system.md` ground every parser and type-checker decision.
- [`../runtime/README.md`](../runtime/README.md) — what consumes the pipeline's `Compilation` output. The `Precept` runtime type wraps a `Compilation`.
- [`../tooling/README.md`](../tooling/README.md) — what consumes the pipeline's diagnostics and structural data. LS diagnostics, MCP `precept_compile`, and the TextMate grammar all derive from the pipeline.
- `research/architecture/compiler/` — the comparator surveys that ground architectural decisions in `compiler-and-runtime-design.md`.

## Cross-cutting concerns

- **Catalog discipline.** Per the [!IMPORTANT] callout above. Every stage derives from catalog metadata; no parallel keyword lists, no per-member kind switches.
- **Diagnostic system.** Every stage emits diagnostics through [`diagnostic-system.md`](diagnostic-system.md)'s codes and templates. Adding a new diagnostic touches the `Diagnostics` catalog first, then the emitting stage, then the doc.
- **Literal system.** Literals flow through every stage — lexer segmentation, parser assembly, type-checker resolution. [`literal-system.md`](literal-system.md) is the cross-stage reference.
- **Anti-mirroring.** `SemanticIndex` (TypeChecker output) is a flat semantic inventory, **not** a structural mirror of the parse tree. Downstream stages consume semantic data, not parser shape. See [`type-checker.md § SemanticIndex`](type-checker.md).
