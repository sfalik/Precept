# GAP-029 Fixed: `IsOutcomeAhead()` now catalog-derived

**Date:** 2026-05-02  
**Author:** Frank  
**Branch:** spike/Precept-V2

## What changed

`IsOutcomeAhead()` in `src/Precept/Pipeline/Parser.cs` previously hardcoded the outcome token set:

```csharp
return next.Kind is TokenKind.Transition or TokenKind.No or TokenKind.Reject;
```

This violates the catalog-derivation rule: if a new `TokenCategory.Outcome` token is added to the catalog, `IsOutcomeAhead()` would silently fail to track it.

## Fix applied

Added a catalog-derived `FrozenSet<TokenKind>` static field alongside the other Layer A vocabulary sets:

```csharp
internal static readonly FrozenSet<TokenKind> OutcomeKeywords =
    Tokens.All
        .Where(m => m.Categories.Contains(TokenCategory.Outcome))
        .Select(m => m.Kind)
        .ToFrozenSet();
```

Updated `IsOutcomeAhead()`:

```csharp
private bool IsOutcomeAhead()
{
    var next = Peek(1);
    return OutcomeKeywords.Contains(next.Kind);
}
```

## Behavior

No behavior change. The three catalog outcome tokens (`Transition`, `No`, `Reject`) are identical to what was hardcoded. This is a pure drift-prevention fix.

## Verification

2690 tests green on `spike/Precept-V2`.
