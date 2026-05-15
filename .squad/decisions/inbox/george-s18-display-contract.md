# George — Slice 18 Display Contract Decision

**By:** George  
**Date:** 2026-05-15T15:35:00Z  
**Status:** Implementation complete, tests green

---

## What Was Found

### IntervalContainmentProofRequirement — carried only normalized bounds

`IntervalContainmentProofRequirement.DeclaredMin/Max` were populated from `GetFieldBounds()`, which returns `NormalizedDeclaredMin ?? DeclaredMin` — the proof-math value. For quantity fields with cross-unit bounds (e.g., `field weight as quantity in 'kg' min '5 g'`), this meant `DeclaredMin = 0.005` (normalized to kg), not `5` (the authored value in grams).

The `NumericOverflow` diagnostic message in `ProofEngine.Diagnostics.cs` used these normalized values directly, producing confusing output like `[0.005 .. 2]` instead of the user-authored `[5 g .. 2000 g]`.

### CompileProofObligationDto — single set of bounds

`CompileProofObligationDto.DeclaredMin/Max` mirrored the requirement's normalized bounds. MCP consumers had no way to distinguish authored from normalized values.

---

## What Was Changed

### 1. `ProofRequirement.cs` — Added `AuthoredMin`/`AuthoredMax` to `IntervalContainmentProofRequirement`

Added two new fields alongside the existing normalized `DeclaredMin`/`DeclaredMax`:
- `DeclaredMin`/`DeclaredMax` — normalized (UCUM base-unit) values, used for interval math in `TryIntervalContainmentProof`. Semantics unchanged.
- `AuthoredMin`/`AuthoredMax` — raw authored values from `TypedField.DeclaredMin/Max`, used exclusively for diagnostic display.

For non-quantity fields (no unit normalization), both pairs are identical.

### 2. `Actions.cs` — `GenerateIntervalContainmentObligations` populates both pairs

`(min, max)` from `GetFieldBounds()` continue to be the normalized proof bounds. Added:
```csharp
var authoredMin = targetField.DeclaredMin;
var authoredMax = targetField.DeclaredMax;
```
These are passed as `AuthoredMin`/`AuthoredMax` on the requirement.

The Description string was also updated to show authored bounds for human readability.

### 3. `ProofEngine.Diagnostics.cs` — NumericOverflow uses authored bounds

```csharp
var displayMin = intervalReq.AuthoredMin ?? intervalReq.DeclaredMin;
var displayMax = intervalReq.AuthoredMax ?? intervalReq.DeclaredMax;
```

Fallback to normalized if authored is null (covers programmatically-constructed obligations in tests).

### 4. `CompileToolDtos.cs` — Added `NormalizedDeclaredMin`/`NormalizedDeclaredMax`

The existing `DeclaredMin`/`DeclaredMax` on the DTO now carry the authored values (for display). The new `NormalizedDeclaredMin`/`NormalizedDeclaredMax` expose the normalized proof-math values. MCP consumers who need precise interval math can use the normalized pair.

### 5. `CompileTool.cs` — `MapProofObligation` projects both pairs

- `DeclaredMin/Max` ← `AuthoredMin/Max ?? DeclaredMin/Max` (authored, for display)
- `NormalizedDeclaredMin/Max` ← `DeclaredMin/Max` from the requirement (normalized)

---

## Test Results

- `Precept.Tests`: 5524 passed, 9 failed (all 9 pre-existing branch failures, none introduced by Slice 18)
- `Precept.Mcp.Tests`: 44 passed, 0 failed
- `ProofRequirementCatalogTests` updated: added `AuthoredMin`/`AuthoredMax` to the direct constructor call

---

## Acceptance Criteria Status

- ✅ `NumericOverflow` diagnostic uses authored values for human-readable output
- ✅ `IntervalContainmentProofRequirement` carries both authored and normalized bounds
- ✅ `CompileProofObligationDto` exposes both sets to MCP consumers
- ✅ All existing tests still pass (no new regressions)
