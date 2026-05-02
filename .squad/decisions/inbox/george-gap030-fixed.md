# GAP-030 Fixed: KeywordsUsableAsFunctionNames catalog-derived

**Date:** 2026-05-02  
**Author:** George  
**Branch:** spike/Precept-V2  
**Commit:** 9759fc2

## Summary

GAP-030 (`ParseAtom` hardcoded `case TokenKind.Min:` / `case TokenKind.Max:`) is now fixed.
`ParseAtom` in `Parser.Expressions.cs` no longer contains hardcoded switch arms for specific
keyword tokens that are also function names.

## What changed

### `src/Precept/Pipeline/Parser.cs` (commit 9759fc2)

Added catalog-derived `KeywordsUsableAsFunctionNames`:

```csharp
internal static readonly FrozenSet<TokenKind> KeywordsUsableAsFunctionNames =
    Functions.All
        .Where(f => Tokens.Keywords.ContainsKey(f.Name))
        .Select(f => Tokens.Keywords[f.Name])
        .ToFrozenSet();
```

This is the intersection of `Functions.All` names and `Tokens.Keywords`. Today it yields
`{ TokenKind.Min, TokenKind.Max }`. Any future function whose name collides with a keyword
token will be included automatically with no parser code change required.

### `src/Precept/Pipeline/Parser.Expressions.cs` (commit ea18430)

Replaced the hardcoded switch arm block:

```csharp
case TokenKind.Identifier:
// GAP-C fix: min/max are keywords but can also appear as function names
case TokenKind.Min:
case TokenKind.Max:
{ ... }
```

With a pre-switch catalog-driven check:

```csharp
if (current.Kind == TokenKind.Identifier || KeywordsUsableAsFunctionNames.Contains(current.Kind))
{
    // shared identifier/call parsing (advance, check for '(', return CallExpression or IdentifierExpression)
}
switch (current.Kind) { /* all other cases, no Identifier/Min/Max */ }
```

## Design rationale

The pattern matches how all other vocabulary sets work in the parser:
- `ActionKeywords`, `TypeKeywords`, `ModifierKeywords`, `OutcomeKeywords` — all `FrozenSet<TokenKind>` derived from catalog
- The intersection approach (`Functions.All` ∩ `Tokens.Keywords`) is the minimal, obvious derivation with no new metadata fields required

## Verification

- 2690 tests passing, 0 failures
- `min(a, b)` parses as `CallExpression` ✅  
- `max(a, b)` parses as `CallExpression` ✅  
- `min` / `max` used as identifiers produce `IdentifierExpression` ✅  
- GAP-030 marked Fixed in `docs/working/language-consistency-gaps.md`
