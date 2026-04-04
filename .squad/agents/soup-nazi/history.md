# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. Makes invalid states structurally impossible.
- **Stack:** C# / .NET 10.0 (runtime, tests), xUnit + FluentAssertions
- **My domain:** All three test suites: `test/Precept.Tests/` (666 tests), `test/Precept.LanguageServer.Tests/`, `test/Precept.Mcp.Tests/`
- **Test conventions:** xUnit `[Fact]` and `[Theory]`, FluentAssertions, `PascalCase` + `Tests` suffix
- **Key docs for test strategy:** `docs/RulesDesign.md` (what constraints must do), `docs/ConstraintViolationDesign.md` (what violations look like)
- **Ground truth:** `samples/` — 20 `.precept` files showing valid and invalid usage
- **Build/test:** `dotnet test` (all), `dotnet test test/Precept.Tests/` (single suite)
- **Created:** 2026-04-04

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
