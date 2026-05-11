# Frank-18: Position-Aware Interpolation Plan Revision

**Date:** 2026-05-11  
**Agent:** frank-18 (claude-opus-4.6)  
**Status:** Approved — awaiting Shane review of interpolation-plan.md before Slice 1 begins

## Problem With frank-16 Compatibility Table

Shane identified a design flaw: the flat compatibility table applied valid types uniformly per target type, regardless of hole position. This is wrong.

Example: `set q = '1 {x}'` where q is `quantity in 'kg'`  
- The hole is in the **unit slot** (after a numeric literal)
- `quantity` in this position would produce `'1 1 kg'` — structurally invalid
- Only `unitofmeasure` and `string` are valid in the unit slot

## Key Insight: Slot Position Determines Valid Types

Hole position within the typed constant determines valid types, not just the target type.

### Slot Classification

| Slot | Example | Valid types |
|------|---------|-------------|
| **Magnitude slot** | `'{x} kg'` | `integer`, `decimal`, `number`, `string` |
| **Unit/qualifier slot** | `'1 {x}'` | `unitofmeasure`, `string` (for quantity target) |
| **Whole-value slot** | `'{x}'` alone | target type itself + string |

### Why `quantity` Is Invalid in a Unit Slot
`'1 {x}'` where x is `quantity in 'kg'` would produce `'1 1 kg'` — the magnitude of the quantity gets appended, producing a double-magnitude string. This is structurally invalid and must be a compile error.

## Where Position Detection Lives

Position detection belongs in the **type checker**, not the parser. Slot classification requires:
1. The target type (from context-sensitive resolution)
2. The position of the hole relative to literal fragments

The parser cannot know the target type — this is a semantic analysis concern.

## Implementation Impact on Slice 2

`ResolveInterpolatedTypedConstant()` must:
1. Identify each hole's slot position (magnitude, unit/qualifier, whole-value)
2. Apply the per-slot compatibility rules
3. Emit `InterpolatedTypedConstantHoleTypeMismatch` (code 120) for violations

## New DiagnosticCode
`InterpolatedTypedConstantHoleTypeMismatch = 120`
