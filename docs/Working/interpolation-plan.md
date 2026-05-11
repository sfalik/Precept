# Interpolation Implementation Plan

**Status:** Awaiting Shane review before Slice 1 (Parser) implementation begins  
**Architect:** Frank (frank-16, frank-18)  
**Philosophy grounding:** `docs/philosophy.md` — make invalid configurations structurally impossible

---

## Problem Statement

Interpolated typed constants (`'1 {x} kg'`, `'{x} kg'`, `'1 {x}'`) are lexed correctly but completely unimplemented beyond a crash-prevention stub. The parser skips all hole tokens. The type checker maps `TypedConstantStart` to `TypedErrorExpression` with no expression nodes created.

**Lexer (correct):** Tokenizes `{expr}` holes into `TypedConstantStart` / `TypedConstantMiddle` / `TypedConstantEnd` segments.  
**Parser (stub):** `ParseInterpolatedTypedConstant()` at `Parser.Expressions.cs:444` skips all hole tokens — produces flat `LiteralExpression(TypedConstantStart, ...)`. No expression AST nodes.  
**Type checker (crash-safe stub):** `ResolveLiteral` `TypedConstantStart` branch emits `TypeMismatch` diagnostic then returns `TypedErrorExpression` (commit `dd1d8e7f` — D26 crash prevention).

---

## Philosophy Constraint

Precept's core identity: **make invalid configurations structurally impossible, not deferred to runtime.**

A `boolean` in a `quantity` hole that compiles clean is exactly what Precept is built to prevent. There are no "V1 permissive" or "V2 deferred" semantics. All type mismatches must be caught at compile time, with one explicit exception (see `string` below).

### The `string` Exception

`string` is valid in any hole position. A string field could hold a valid unit code or currency code at runtime — the compiler cannot statically know the string's content. This is the one legitimately justified runtime-deferred check, explicitly reasoned against the philosophy.

---

## Key Design Decision: Position-Aware Hole Typing

The valid types for a hole depend on **position within the typed constant**, not just the target type.

### Slot Classification

| Slot | Example | Description |
|------|---------|-------------|
| **Magnitude slot** | `'{x} kg'` | Hole precedes a unit fragment |
| **Unit/qualifier slot** | `'1 {x}'` | Hole follows a numeric fragment |
| **Whole-value slot** | `'{x}'` | Hole is the entire content |

### Why `quantity` Is Invalid in a Unit Slot

`set q = '1 {x}'` where `x` is `quantity in 'kg'`:
- The hole is in the unit slot (after a numeric literal)
- The assembled string would be `'1 1 kg'` — double-magnitude, structurally invalid
- `quantity` must be a compile error in this position
- Only `unitofmeasure` and `string` are valid in the unit slot

### Per-Position Compatibility Tables

**`quantity` target:**
| Slot | Valid hole types |
|------|-----------------|
| Magnitude slot (`'{x} kg'`) | `integer`, `decimal`, `number`, `string` |
| Unit slot (`'1 {x}'`) | `unitofmeasure`, `string` |
| Whole-value slot (`'{x}'`) | `quantity`, `string` |

**`money` target:**
| Slot | Valid hole types |
|------|-----------------|
| Magnitude slot | `integer`, `decimal`, `number`, `string` |
| Currency slot (`'1 {x}'`) | `currency`, `string` |
| Whole-value slot | `money`, `string` |

**`price` target:**
| Slot | Valid hole types |
|------|-----------------|
| Magnitude slot | `integer`, `decimal`, `number`, `string` |
| Unit slot | `unitofmeasure`, `string` |
| Currency slot | `currency`, `string` |
| Whole-value slot | `price`, `string` |

**`currency` target:**
| Slot | Valid hole types |
|------|-----------------|
| Whole-value slot | `currency`, `string` |

**`unitofmeasure` target:**
| Slot | Valid hole types |
|------|-----------------|
| Whole-value slot | `unitofmeasure`, `string` |

### Where Position Detection Lives

Position detection belongs in the **type checker**, not the parser. Slot classification requires:
1. The target type — only available from context-sensitive resolution
2. The position of the hole relative to literal fragments — available from the parsed AST structure

The parser creates the expression nodes; the type checker classifies slots and enforces compatibility.

---

## New Language Surface

### DiagnosticCode
`InterpolatedTypedConstantHoleTypeMismatch = 120`

### AST Node (Parser output)
`InterpolatedTypedConstantExpression` — mirrors `InterpolatedStringExpression`

### ExpressionFormKind
`InterpolatedTypedConstant = 15`

### Typed Node (TypeChecker output)
`TypedInterpolatedTypedConstant` — mirrors `TypedInterpolatedString`

---

## Implementation Slices

### Slice 1 — Parser

**File:** `src/Precept/Pipeline/Parser.Expressions.cs`  
**Method:** `ParseInterpolatedTypedConstant()` at ~line 444  
**Reference:** `ParseInterpolatedString()` at ~lines 399–441

**Work:**
- Rewrite `ParseInterpolatedTypedConstant()` to mirror `ParseInterpolatedString()`
- Consume `TypedConstantStart`, then alternate between `TextSegment` (literal fragments) and `HoleSegment` (expressions), closing on the matching end token
- Add `InterpolatedTypedConstantExpression` record to `src/Precept/Pipeline/ParsedExpression.cs`
- Add `ExpressionFormKind.InterpolatedTypedConstant = 15` to `src/Precept/Language/ExpressionForms.cs`
- Reuse existing `InterpolationSegment`, `HoleSegment`, `TextSegment` types

**Tests:** Parser round-trips — `'{x} kg'`, `'1 {x}'`, `'{x}'`, multi-hole `'{a} {b} kg'`

---

### Slice 2 — Type Checker

**Files:**
- `src/Precept/Pipeline/TypeChecker.Expressions.cs` — new `ResolveInterpolatedTypedConstant()`
- `src/Precept/Pipeline/SemanticIndex.cs` — new `TypedInterpolatedTypedConstant` record
- `src/Precept/Pipeline/TypeChecker.cs` — add `TypedInterpolatedTypedConstant` to `ContainsError()`
- `src/Precept/Language/DiagnosticCode.cs` — add `InterpolatedTypedConstantHoleTypeMismatch = 120`

**Work:**
- Add `InterpolatedTypedConstantExpression` case to `Resolve()` dispatch switch
- Implement `ResolveInterpolatedTypedConstant()`:
  1. Determine target type from context
  2. Walk segments; for each `HoleSegment`, classify its slot position
  3. Resolve the hole expression
  4. Check resolved type against per-slot compatibility table
  5. Emit `InterpolatedTypedConstantHoleTypeMismatch` for violations
  6. Return `TypedInterpolatedTypedConstant` (or `TypedErrorExpression` if unresolvable)
- Remove the stub in `ResolveLiteral` `TypedConstantStart` branch — it's superseded by the new `Resolve()` dispatch
- Update `ContainsError()` to handle `TypedInterpolatedTypedConstant`

**Tests:** Per-slot type mismatch detection, `string` exception, whole-value slot, valid combinations

---

### Slice 3 — Completions

**File:** `tools/Precept.LanguageServer/Handlers/CompletionsHandler.cs`

**Work:**
- Add `IsInsideTypedConstantHole()` helper — checks if cursor is inside `{...}` in a typed constant
- When inside a hole, serve field/arg completions filtered to types valid for that hole's slot position
- Fixes open bug I2 (completions inside `{}` holes)

**Tests:** Completions inside holes for quantity, money, price targets

---

### Slice 4 — Semantic Tokens

**File:** `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs`

**Work:**
- Add `TypedInterpolatedTypedConstant` case to `EnumerateExpressionTree()`
- Walk hole expressions for token classification
- Fixes open bug I3 (semantic highlighting inside `{}` holes)

**Tests:** Token classification inside holes

---

### Slice 5 — Docs/MCP

**Files:**
- `docs/language/precept-language-spec.md` — §2.5, §3.6 already describe target behavior; verify accuracy after implementation
- No grammar changes needed (lexer already handles holes correctly)
- No MCP tool changes needed (no new catalog entries)

---

## Dependency Order

```
Slice 1 (Parser)
    ↓
Slice 2 (TypeChecker)
    ↓
Slice 3 (Completions) ← can parallel with Slice 4
Slice 4 (SemanticTokens) ← can parallel with Slice 3
    ↓
Slice 5 (Docs/MCP)
```

---

## Open Bugs Unblocked by This Plan

| Bug | Description | Unblocked after |
|-----|-------------|-----------------|
| I2 | Completions inside `{}` holes | Slice 2 |
| I3 | Semantic highlighting inside `{}` holes | Slice 2 |

---

## Gates Before Slice 1

- [ ] Shane approves this plan
