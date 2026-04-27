# docs/ — Design Documentation

Design documents for the Precept compiler pipeline and language surface.

## Structure

| Path | Purpose |
|------|---------|
| [compiler/](compiler/) | Pipeline stage blueprints (lexer → parser → type checker → graph analyzer → proof engine) and cross-cutting compiler infrastructure |
| [language/](language/) | Language specification, vision document, and type system design proposals |
| [runtime/](runtime/) | Runtime API, result types, fault system, Precept Builder, Evaluator, and descriptor type design |
| [tooling/](tooling/) | Language server (LSP), MCP server, and VS Code extension design |

## Top-Level Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [compiler-and-runtime-design.md](compiler-and-runtime-design.md) | Compiler pipeline, runtime surfaces, artifact relationships, LS integration | Draft |
| [catalog-system.md](language/catalog-system.md) | Catalog pattern for closed enum registries (tokens, diagnostics, faults, functions, operators) with `[Meta]` attributes and exhaustive switches | Draft |

## Reading Order

1. **Start here:** [compiler-and-runtime-design.md](compiler-and-runtime-design.md) — pipeline, artifacts, runtime surfaces, LS integration
2. **Stage docs:** [compiler/lexer.md](compiler/lexer.md), [compiler/parser.md](compiler/parser.md), [compiler/type-checker.md](compiler/type-checker.md), [compiler/graph-analyzer.md](compiler/graph-analyzer.md), [compiler/proof-engine.md](compiler/proof-engine.md)
3. **Language surface:** [language/precept-language-spec.md](language/precept-language-spec.md)
4. **Cross-cutting:** [catalog-system.md](language/catalog-system.md), [compiler/diagnostic-system.md](compiler/diagnostic-system.md), [compiler/literal-system.md](compiler/literal-system.md)
5. **Runtime:** [runtime/runtime-api.md](runtime/runtime-api.md), [runtime/precept-builder.md](runtime/precept-builder.md), [runtime/evaluator.md](runtime/evaluator.md), [runtime/descriptor-types.md](runtime/descriptor-types.md)
6. **Tooling:** [tooling/README.md](tooling/README.md) — language server, MCP server, VS Code extension

## Relationship to Other Docs

- `research/architecture/compiler/` — 15 external research surveys that ground the design decisions here.
- `research/language/` — language research, precedent surveys, and proposal rationale.
