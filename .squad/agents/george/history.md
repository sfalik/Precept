# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. DSL that makes invalid states structurally impossible. Declarative `.precept` files compile to executable runtime contracts.
- **Stack:** C# / .NET 10.0 (core runtime, language server, MCP server), TypeScript (VS Code extension), xUnit + FluentAssertions
- **My domain:** `src/Precept/Dsl/` — full DSL pipeline: tokenizer, parser, type checker, expression evaluator, runtime engine, constraint evaluation
- **Key docs:** `docs/PreceptLanguageDesign.md` (DSL spec — law), `docs/RulesDesign.md` (constraints), `docs/ConstraintViolationDesign.md`, `docs/CatalogInfrastructureDesign.md`
- **Tests:** `test/Precept.Tests/` — 666 tests, xUnit + FluentAssertions
- **Samples:** `samples/` — 20 `.precept` files, canonical usage examples
- **Created:** 2026-04-04

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
