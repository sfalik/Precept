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
| [soundness-and-coverage.md](soundness-and-coverage.md) | How the prove-or-reject guarantee is made sound and its measurable boundary — the decision rationale (why prove-or-reject, not a fault-floor), the coverage matrix (`FaultCode` × obligation-creation site × disposition) and the bucket test for new failing cases, the legible certificate format, the counterexample, the independent checker, and coverage of the known fail-open holes (cross-cuts proof engine, graph analyzer, type checker, fault correspondence) | Design (skeleton) |
| [diagnostic-system.md](diagnostic-system.md) | Diagnostic codes, severity, message templates, audience model, stage attribution | Full |
| [literal-system.md](literal-system.md) | How literals flow through every pipeline stage — lexer segmentation, parser assembly, type-checker resolution, evaluator materialization | Draft |
| [tooling-surface.md](tooling-surface.md) | TextMate grammar generation, semantic token two-pass design, completion filtering | Full |
| [grammar-generator.md](grammar-generator.md) | Grammar generator algorithm, pattern templates, structural composition, catalog gap | Full |

## Source Code

All pipeline stages live under `src/Precept/Pipeline/`. Tests are in `test/Precept.Tests/`.

## Open questions

Each stage document carries an `Open Questions / Implementation Notes` section — section 13 of the sixteen-section template — and the cross-cutting documents carry the equivalent. The discussion of each question stays in the section that raised it; this table only makes them findable. Ordered by the document that owns the question, in the order the documents are listed above.

[`../compiler-and-runtime-design.md`](../compiler-and-runtime-design.md) is deliberately left out of this table. It is the architecture overview covering the pipeline and the runtime together, and the runtime's own index leaves it out for the same reason: indexing it under one area's README would make that README the home for questions the other area half-owns. The two things it leaves unsettled belong to the runtime anyway — whether anything ever constructs a `Fault` (`../runtime/fault-system.md` Q3, already indexed in [`../runtime/README.md`](../runtime/README.md) § Open questions), and definition migration, which § Definition versioning records as deliberately out of scope for v1 rather than undecided.

| Question — what somebody has to decide | Where it is discussed |
|---|---|
| How finely the parser recovers after a syntax error. It synchronizes at construct boundaries today; finer per-slot recovery may improve diagnostics and adds complexity, and the document says the choice needs implementation experience. | [parser.md](parser.md) § Open Questions / Implementation Notes — Error Recovery Granularity |
| Whether the parser keeps trivia. Whitespace and comments are discarded today; formatting tools would need them attached. Recorded as not blocking the runtime. | [parser.md](parser.md) § Open Questions / Implementation Notes — Trivia Preservation |
| Whether `TypedErrorExpression` gains an optional list of candidate types, so a failed resolution can offer a "did you mean?" suggestion. Marked low priority and post-v1. | [type-checker.md](type-checker.md) § 13 Open Questions, item 4 |
| What should happen when a price and a quantity share a dimension but not a unit — `'USD/kg' × quantity in 'g'`. The document names three candidates (reject on exact unit, convert automatically, split by unit kind), calls the policy undecided, and records that until it is settled the cancellation silently drops the conversion factor. | [proof-engine.md](proof-engine.md) § 7.2 Axis-Fallback Chains — "Known gap — cross-unit cancellation" |
| Whether routing an entering value to the constraints declared on the slot it enters is served by the builder's `ConstraintInfluenceMap` or by the evaluator's own bucket index. The proof engine document says neither document answers it. | [proof-engine.md](proof-engine.md) § 8 — Builder Proof-Consumption Contract; the same question from the runtime side is [`../runtime/precept-builder.md`](../runtime/precept-builder.md) § 12 |
| Whether decimal precision behavior in division and multiplication — rounding, scale accumulation, mantissa overflow — needs a proof strategy of its own or fits inside the existing set, should a precision warning be added to the binary-operation catalog entry. | [proof-engine.md](proof-engine.md) § 13 Future Considerations, item 8 |
| Where the certificate checker's compiler-side call site sits in the pipeline. § 5b commits to the checker running at two places and names one of them as a compiler stage that audits the proof engine at authoring time; where that stage is invoked is written nowhere, and the § Pipeline Order table above lists six stages with no checker among them. No stage document owns this question, because the stage does not exist yet — § 5b is where the requirement is stated, not where the placement is argued. | [soundness-and-coverage.md](soundness-and-coverage.md) § 5b — The independent checker |
| `FunctionArgConstraintViolation` — whether its scaffolding comes out or per-argument constraint machinery gets built. The coverage map records it as a fork for the owner with no locked specification behind either side, and notes that choosing "build" routes it to `/design` as new language surface. | [soundness-and-coverage.md](soundness-and-coverage.md) § 3.1 Coverage by fault mode |
| Whether `FaultCode` is closed at the members it has, or PRECEPT0002 is relaxed so a category for a defect in the engine itself can be added. Such a defect has no diagnostic an author could act on, which is what the chain requires of every member. | [diagnostic-system.md](diagnostic-system.md) § FaultCode → DiagnosticCode Chain — "What the chain rules out"; the runtime half of the same question is `../runtime/fault-system.md` Q3 |
| Whether diagnostics need a way to link to proof-model entries, if proof results come to carry richer structured output — expression trees, interval ranges, counterexample values. Recorded as deferred until the proof engine design settles. | [diagnostic-system.md](diagnostic-system.md) § Open Questions / Implementation Notes — D5 coupling |
| What the percentage domain (`'10 percent'`) is: its type name, its validator shape, and its arithmetic semantics. | [literal-system.md](literal-system.md) § Implementation Notes, item 4 |

Several entries that sit in one of those sections are **not** open, and are deliberately absent from the table above:

- `lexer.md` § 13 holds implementation notes and two contingent considerations — Unicode identifiers and multi-line strings — both of which § 14 Deliberate Exclusions decides against.
- `name-binder.md` § 13 states that all its design questions are locked.
- `graph-analyzer.md` § 12 is five resolved questions plus implementation notes. Its note that future work may introduce incremental analysis is decided the other way by [`../compiler-and-runtime-design.md`](../compiler-and-runtime-design.md) § Incremental compilation model, which makes re-running everything on change the intended model.
- `tooling-surface.md` § 13 records its open questions as resolved and points at [grammar-generator.md](grammar-generator.md). Two older inline callouts in its body still read "Open Question (unresolved)"; both are answered there — the token-scope grouping question by § Scope grouping, and the question of which catalogs feed the grammar by § Deliberate Exclusions. The callouts are stale text, not live decisions.
- `type-checker.md` § 13 names W2 and G1–G3 as remaining open items, but nothing in the document says what they are, so there is nothing for a row to point at.
- `proof-engine.md` § 13 also carries two things to verify (strategy coverage, Roslyn analyzer scope), a deliberate deferral of multi-fact combination with its own re-open trigger, and a justification mechanism recorded as not planned. Those are work and settled positions rather than choices anybody has to make.
- `diagnostic-system.md` § Open Questions also carries two pieces of work: a drift test confirming every statically-preventable fault's diagnostic is actually emitted, and a wording change to PRE0116's recovery steps held until message holes are enrolled.

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
