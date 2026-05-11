# Decision: Parser Precedence Fix + Ensure Type Context Propagation

**Author:** Frank  
**Date:** 2026-05-10  
**Status:** Implemented  

## Bug 1 — Parser Precedence Inversion (NonAssociative rightBp)

### Root Cause

In `Parser.Expressions.cs`, `GetLedBindingPower()` computed the right binding power for `NonAssociative` operators as `int.MaxValue`:

```csharp
Associativity.NonAssociative => int.MaxValue,
```

In a Pratt parser, `rightBp` controls the minimum precedence required for operators to bind within the right-hand operand. `int.MaxValue` meant **no operator could ever bind on the right side of a comparison** — not even higher-precedence ones like `*` (60) or `/` (60). This caused `a <= b * c` to parse as `(a <= b) * c` instead of `a <= (b * c)`.

The catalog values were correct (comparison = 30, additive = 50, multiplicative = 60). The inversion was entirely in the parser's consumption of those values.

### Fix

Changed `NonAssociative` rightBp to `meta.Precedence + 1`:

```csharp
Associativity.NonAssociative => meta.Precedence + 1,
```

This allows higher-precedence operators (arithmetic at 50/60) to bind within comparison right-operands, while still preventing same-precedence operators from right-associating. The behavior matches `Left` associativity for the purpose of right-operand parsing — the only difference should be a separate non-associativity enforcement check (which was never implemented; `NonAssociativeComparison` diagnostic code exists but is never emitted).

### Impact

- `rule ExistingDebt <= AnnualIncome * 3.0` now parses correctly without parentheses
- `when AnnualIncome >= ExistingDebt * 2.0` now parses correctly without parentheses
- No existing tests broke — the 5,073-test suite passes clean

## Bug 2 — Missing Type Context in Binary Expression Operands (PRE0052)

### Root Cause

In `TypeChecker.Expressions.cs`, `ResolveBinaryOp()` resolved both operands without type context:

```csharp
var left = Resolve(bin.Left, ctx);
var right = Resolve(bin.Right, ctx);  // ← no expectedType
```

Typed constants (single-quoted literals like `'0.00 USD'`) require `expectedType` to resolve — without it, `ResolveTypedConstant` emits PRE0052 (UnresolvedTypedConstant) and returns `TypedErrorExpression`. The D13 error-propagation check then short-circuited before the existing context-retry logic at line 613 could run.

The context-retry mechanism (`TryContextRetryBinaryOp`) already existed for numeric literals but was unreachable for typed constants because the D13 gate (`if left/right is TypedErrorExpression → bail`) fired first.

### Fix

Added proactive type-context propagation for typed constant operands, inserted between the initial resolution and the D13 check:

1. **Right operand (proactive):** When `bin.Right` is a `TypedConstant` literal and left resolved successfully, resolve right with `left.ResultType` as `expectedType` on the first attempt. No stale diagnostic issue.

2. **Left operand (retry):** When `bin.Left` is a `TypedConstant` literal that failed initial resolution and right resolved successfully, remove the stale PRE0052 diagnostic and re-resolve left with `right.ResultType` as `expectedType`.

### Scope

This is a **general type propagation approach for typed constants in binary expressions**, not specific to `ensure`. It applies everywhere binary expressions appear: rules, guards, ensures, computed fields, transition guards, etc. The fix is architecturally correct because typed constants inherently need type context — in a binary expression, the peer operand is the natural source of that context.

## Files Changed

| File | Change |
|------|--------|
| `src/Precept/Pipeline/Parser.Expressions.cs` | `NonAssociative` rightBp: `int.MaxValue` → `meta.Precedence + 1` |
| `src/Precept/Pipeline/TypeChecker.Expressions.cs` | Added typed constant context propagation in `ResolveBinaryOp` before D13 check |
| `samples/apartment-rental-application.precept` | Removed workaround parentheses from `(RequestedRent * 3)` (2 occurrences) |

## Test Results

All 5,073 tests pass (0 failures, 0 skipped). No test was written against the wrong behavior.
