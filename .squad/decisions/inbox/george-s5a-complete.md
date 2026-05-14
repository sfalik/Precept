# Slice 5A Complete — PRE0091 AmbiguousTypedConstant

**Date:** 2025-07-14
**By:** George

## What Was Wired

PRE0091 `AmbiguousTypedConstant` is now emitted from the TypeChecker typed-constant resolution path when `expectedType is null` and a literal validates against multiple temporal quantity candidate types (Duration and Period).

## File Boundary Decision

**`TypeChecker.Expressions.cs` owns typed-constant resolution.** The `ResolveTypedConstant` method and the new `ResolveTypedConstantCandidates` helper both live here. `TypeChecker.Expressions.TypedConstants.cs` owns post-resolution concerns: qualifier validation, interpolated typed-constant resolution, and interpolated string handling — it does not participate in the initial resolution dispatch.

## Ambiguity Detection Logic

- A static `TemporalQuantityCandidates` array holds `[TypeKind.Duration, TypeKind.Period]`.
- When `expectedType is null`, the new `ResolveTypedConstantCandidates` helper iterates candidates, calls `TypedConstantValidation.Validate` for each, and collects survivors.
- 0 survivors → returns null (caller falls through to existing `UnresolvedTypedConstant` emission).
- 1 survivor → resolves to that type directly (e.g., `'P30D'` resolves only as Period since Duration validation rejects ISO period notation).
- 2+ survivors → emits PRE0091 with the display names of the first two competing types.

## Key Finding: Temporal Quantity Overlap

Both Duration and Period share the `TemporalQuantityParser` and both accept human-readable temporal quantities (`'30 days'`, `'2 hours'`). The ambiguity is structural: `'30 days'` is genuinely valid as both types. Only ISO-format period notation (`'P30D'`) disambiguates to Period alone, because the Duration validator's NodaTimePattern is `"quantity"` (no NormalizingIso fallback).

## Anomalies

None. The existing single-expected-type fast path is fully preserved — PRE0091 is structurally unreachable on well-typed precepts, consistent with the Q4 resolution in §10.

## Allow-List Changes

- Removed `AmbiguousTypedConstant` from Gate 1 allow-list.
- Added `AmbiguousTypedConstant` to Gate 2 allow-list (cross-project analyzer cannot detect test references in separate Precept.Tests assembly).
