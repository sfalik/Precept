# docs/ — Design Documentation

Design documents for the Precept compiler pipeline and language surface.

## Structure

| Path | Purpose |
|------|---------|
| [compiler/](compiler/) | Pipeline stage blueprints (lexer → parser → type checker → graph analyzer → proof engine) and cross-cutting compiler infrastructure |
| [language/](language/) | Language specification, vision document, and type system design proposals |
| [runtime/](runtime/) | Runtime API, result types, and fault system design |

## Top-Level Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [compiler-and-runtime-design.md](compiler-and-runtime-design.md) | Compiler pipeline, runtime surfaces, artifact relationships, LS integration | Draft |
| [catalog-system.md](language/catalog-system.md) | Catalog pattern for closed enum registries (tokens, diagnostics, faults, functions, operators) with `[Meta]` attributes and exhaustive switches | Draft |

## Reading Order

1. **Start here:** [compiler-and-runtime-design.md](compiler-and-runtime-design.md) — pipeline, artifacts, runtime surfaces, LS integration
2. **Stage docs:** [compiler/lexer.md](compiler/lexer.md) (parser and type checker docs pending clean-room redesign)
3. **Language surface:** [language/precept-language-spec.md](language/precept-language-spec.md)
4. **Cross-cutting:** [catalog-system.md](language/catalog-system.md), [compiler/diagnostic-system.md](compiler/diagnostic-system.md), [compiler/literal-system.md](compiler/literal-system.md)
5. **Runtime:** [runtime/runtime-api.md](runtime/runtime-api.md) — result types, fault system, fire/update API

## Relationship to Other Docs

- `research/architecture/compiler/` — 15 external research surveys that ground the design decisions here.
- `research/language/` — language research, precedent surveys, and proposal rationale.
