# George — UCUM catalog collapse

**Date:** 2026-05-09  
**Author:** George (Runtime Dev)

---

## Decision

Collapse the UCUM catalog split so `UcumAtomCatalog` is the single source of truth for UCUM metadata.

## Locked Rules

- `UcumAtomCatalog.All` always means the full XML-backed UCUM universe used for validation.
- `UcumAtomCatalog.BrowseTier1()` is the curated Tier 1 completion/browsing surface and preserves the declared category order.
- Tier 1 entries resolve from `UcumAtomCatalog.All` when possible; parse-only forms such as prefixed, compound, and compact-exponent codes are synthesized through `UcumParser` and cached.
- Internal callers should use `UcumParser.Parse(...)` for parsing and `UcumAtomCatalog` for catalog data.
- `UcumCatalog` remains only as a thin compatibility shim with no duplicated UCUM state.

## Files

- `src/Precept/Language/Ucum/UcumAtomCatalog.cs`
- `src/Precept/Language/Ucum/UcumCatalog.cs`
- `src/Precept/Pipeline/TypeChecker.cs`
- `src/Precept/Language/PriceValidator.cs`
- `src/Precept/Language/QuantityValidator.cs`
- `src/Precept/Language/Ucum/UcumValidator.cs`
- `tools/Precept.Mcp/Tools/LanguageTool.cs`
- `test/Precept.Tests/Language/Ucum/UcumCatalogTests.cs`
- `test/Precept.Mcp.Tests/LanguageToolTests.cs`
- `docs/tooling/mcp.md`

## Validation

- `dotnet build src\Precept\Precept.csproj`
- `dotnet test test\Precept.Tests\Precept.Tests.csproj`
- `dotnet test test\Precept.Mcp.Tests\Precept.Mcp.Tests.csproj`

## Outcome

`All` no longer means two different things, Tier 1 browsing moved onto `UcumAtomCatalog`, and runtime/tooling callers now read from one authoritative UCUM catalog surface.
