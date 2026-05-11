# Frank-16: Interpolation Plan (Philosophy-Grounded, Full Compile-Time Enforcement)

**Date:** 2026-05-11  
**Agent:** frank-16 (claude-opus-4.6)  
**Status:** Superseded by frank-18 (position-aware revision)

## Summary

Delivered a complete, philosophy-grounded implementation plan for string interpolation in typed constants.
Key change from frank-15: **full compile-time per-component hole type enforcement — no V2 deferrals.**

## Why frank-15 Was Rejected

1. Frank did not read `docs/philosophy.md` first (charter requirement).
2. Permissive V1 hole typing defers type mismatches to runtime — this directly violates Precept's core identity of making invalid configurations structurally impossible.

## Design Decisions

### String Exception
`string` is valid in any hole position. A string field could hold a valid unit code or currency code at runtime — this is the one legitimately justified runtime-deferred check. All other type mismatches must be caught at compile time.

### New DiagnosticCode
`InterpolatedTypedConstantHoleTypeMismatch = 120`

### 5-Slice Plan
1. **Parser** — Rewrite `ParseInterpolatedTypedConstant()` to mirror `ParseInterpolatedString()`. New `InterpolatedTypedConstantExpression` AST node + `ExpressionFormKind.InterpolatedTypedConstant = 15`.
2. **Type Checker** — New `TypedInterpolatedTypedConstant` typed node. New `ResolveInterpolatedTypedConstant()`. Per-component compatibility enforced at compile time.
3. **Completions** — `IsInsideTypedConstantHole()` helper; when in a hole, serve field/arg completions.
4. **Semantic Tokens** — Add `TypedInterpolatedTypedConstant` to `EnumerateExpressionTree()`.
5. **Docs/MCP** — Spec already describes target behavior; no grammar/MCP changes needed.

## Per-Component Compatibility Table (must be enforced at compile time)

| Target type    | Valid hole expression types                                          |
|----------------|----------------------------------------------------------------------|
| `quantity`     | `integer`, `decimal`, `number`, `unitofmeasure`, `string`, `quantity` |
| `money`        | `integer`, `decimal`, `number`, `currency`, `string`, `money`        |
| `price`        | `integer`, `decimal`, `number`, `currency`, `unitofmeasure`, `string`, `price` |
| `currency`     | `string`, `currency`                                                  |
| `unitofmeasure`| `string`, `unitofmeasure`                                             |

**Note:** This table was revised by frank-18 to be position-aware. See `frank-interpolation-position-aware.md`.
