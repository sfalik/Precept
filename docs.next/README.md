# docs.next/ — v2 Clean-Room Design Documentation

Design documents for the Precept v2 compiler pipeline and language surface. These are **clean-room** artifacts — they do not reference v1 implementation details.

## Structure

| Path | Purpose |
|------|---------|
| [compiler/](compiler/) | Pipeline stage blueprints (lexer → parser → type checker → graph analyzer → proof engine) and cross-cutting compiler infrastructure |
| [language/](language/) | Language specification, vision document, and type system design proposals |
| [runtime/](runtime/) | Runtime API, result types, and fault system design |

## Top-Level Documents

| Document | Purpose | Status |
|----------|---------|--------|
| [architecture-planning.md](architecture-planning.md) | Master architecture plan — identifies what must be designed, in what order, grounded in research evidence | Planning |
| [catalog-system.md](catalog-system.md) | Catalog pattern for closed enum registries (tokens, diagnostics, faults, functions, operators) with `[Meta]` attributes and exhaustive switches | Draft |
| [compiler-architecture-proposal.md](compiler-architecture-proposal.md) | Initial compiler design proposal — predates the architecture plan | Superseded by `architecture-planning.md` |

## Reading Order

1. **Start here:** [architecture-planning.md](architecture-planning.md) — the master plan that sequences all design work
2. **Pipeline overview:** [compiler/pipeline-artifacts-and-consumer-contracts.md](compiler/pipeline-artifacts-and-consumer-contracts.md) — how stages connect
3. **Stage docs** (in pipeline order): [compiler/lexer.md](compiler/lexer.md) → [compiler/parser.md](compiler/parser.md) → [compiler/type-checker.md](compiler/type-checker.md)
4. **Language surface:** [language/precept-language-spec.md](language/precept-language-spec.md) — grows per stage as decisions lock
5. **Cross-cutting:** [catalog-system.md](catalog-system.md), [compiler/diagnostic-system.md](compiler/diagnostic-system.md), [compiler/literal-system.md](compiler/literal-system.md)
6. **Runtime:** [runtime/runtime-api.md](runtime/runtime-api.md) — how the compiler's output is consumed at runtime (result types, fault system, fire/update API)

## Relationship to Other Docs

- `docs/` — v1 design docs (implemented behavior). `docs.next/` designs the replacement.
- `research/architecture/compiler/` — 15 external research surveys that ground the design decisions here.
- `research/language/` — language research, precedent surveys, and proposal rationale.
