# Project Context

- **Owner:** shane
- **Project:** Precept — domain integrity engine for .NET. DSL that makes invalid states structurally impossible.
- **Stack:** C# / .NET 10.0 (language server), TypeScript (VS Code extension), xUnit
- **My domain:** `tools/Precept.LanguageServer/` (C# LSP server) and `tools/Precept.VsCode/` (TypeScript extension)
- **Key files:** `PreceptAnalyzer.cs` (completions/hover), `PreceptSemanticTokensHandler.cs`, `syntaxes/precept.tmLanguage.json` (grammar)
- **Critical sync rule:** When George changes the DSL parser, I must update both `tmLanguage.json` AND `PreceptAnalyzer.cs` in the same pass
- **Build:** `dotnet build tools/Precept.LanguageServer/...` for LS; `npm run compile` in `tools/Precept.VsCode/` for extension
- **Tests:** `test/Precept.LanguageServer.Tests/`
- **Created:** 2026-04-04

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
