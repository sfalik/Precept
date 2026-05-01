# George — Slice 3 Complete (GAP-3: `is set` / `is not set` Postfix Operators)

**Date:** 2026-05-01  
**Branch:** spike/Precept-V2  
**Commit:** 4a041c2

## What Was Created

| File | Action |
|------|--------|
| `src/Precept/Pipeline/SyntaxNodes/Expressions/IsSetExpression.cs` | New — `sealed record IsSetExpression(SourceSpan, Expression Operand)` |
| `src/Precept/Pipeline/SyntaxNodes/Expressions/IsNotSetExpression.cs` | New — `sealed record IsNotSetExpression(SourceSpan, Expression Operand)` |
| `src/Precept/Pipeline/Parser.cs` | Modified — new Pratt led handler for `TokenKind.Is` in `ParseExpression` while loop |
| `test/Precept.Tests/ExpressionParserTests.cs` | Modified — 4 new tests |

## Test Count

- **Baseline (stale binaries, pre-build):** 2111
- **Fresh build before slice (compiled baseline):** 2130 (Slice 7 and others were committed but not yet compiled into the tested binary)
- **After slice (fresh build):** 2134
- **New tests:** 4 (all passing, zero regressions)

## Implementation Decision: No Catalog Entry

`is set` / `is not set` are NOT added to `OperatorKind` or `Operators.All`. `OperatorMeta` holds a single `TokenMeta Token` and `Arity` has no `Postfix` value — multi-token postfix operators don't fit this shape cleanly. The led handler is placed directly in the Pratt loop, before the `OperatorPrecedence.TryGetValue` guard, following the same pattern as the member-access (`.`) handler at binding power 80.

Binding power chosen: **60** — above `and`/`or` (10/20), above `contains` (40), above `+/-` (50), at the arithmetic `*//` level. Allows `x is set and y > 0` to parse as `And(IsSet(x), >(y,0))` correctly.

## Surprises / Notes

- `TokenKind.Is` is NOT in `ExpressionBoundaryTokens` (not a construct leading token), but without the explicit led handler it would silently hit the `OperatorPrecedence.TryGetValue` fallthrough-break and be ignored — exactly the reported bug.
- `dotnet build -q` masks the "Question build" informational output in a way that looks like an actual build failure. The real build succeeded; use `Where-Object` to filter output instead of `-q` when diagnosing build errors.
- Test identifiers use `opt` as the field name to avoid keyword collision (`field` lexes as `TokenKind.Field`, a construct-leading token that would stop atom parsing).
