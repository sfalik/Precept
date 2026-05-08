# IsMessagePosition catalog metadata

**Author:** George  
**Date:** 2026-05-08T18:36:50.783-04:00  
**Status:** Recorded for Kramer follow-up  
**Commit SHA:** `105a42a72fedf1a5c7b5cb3cc7a17ea576d5bb95`

## Files changed
- `src/Precept/Language/Token.cs`
- `src/Precept/Language/Function.cs`
- `src/Precept/Language/Tokens.cs`
- `docs/compiler/grammar-generator.md`
- `docs/compiler/tooling-surface.md`
- `docs/language/catalog-system.md`

## Entries populated
- `TokenKind.Because` → `IsMessagePosition: true`
- `TokenKind.Reject` → `IsMessagePosition: true`

## Function review
- Reviewed every built-in in `src/Precept/Language/Functions.cs`.
- No `FunctionMeta` entries were flagged.
- Reason: the current built-in library is numeric, string, and temporal utilities; none has a trailing user-facing message-string parameter.

## Record shape for Kramer
- `TokenMeta(..., bool IsAccessModeAdjective = false, bool IsValidAsMemberName = false, bool IsMessagePosition = false)`
- `FunctionMeta(..., bool HasCIVariant = false, FunctionKind? CIVariantOf = null, bool IsMessagePosition = false)`
- Generator follow-up: derive gold message-string patterns from catalog metadata instead of hardcoding `because` / `reject` in `AddStructuralPatterns()`.
