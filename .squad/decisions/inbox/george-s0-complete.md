# Decision: Slice 0 Complete — Diagnostic Coverage Enforcement Infrastructure

**Date:** 2025-07-25  
**Author:** George (Runtime Dev)  
**Status:** Complete

## Context

Slice 0 installs the diagnostic enforcement infrastructure: two Roslyn analyzers that prevent diagnostic coverage regressions, plus shared scanner and allow-list machinery.

## Decisions

### 1. Diagnostic IDs use distinct numeric codes (not suffixed)

- PRECEPT0027 — Gate 1 missing emission (Error)
- PRECEPT0028 — Gate 2 missing test (Error)
- PRECEPT0029 — Gate 1 stale allow-list (Warning)
- PRECEPT0030 — Gate 2 stale allow-list (Warning)

**Rationale:** Roslyn 5.3.0 silently drops diagnostics with lowercase-letter-suffixed IDs (e.g. `"PRECEPT0027b"`). Distinct numeric IDs are universally reliable.

### 2. Gate 1 allow-list count: 30 (not 49)

The original spec estimated 49 unemitted codes. Actual count is 30 because:
- Slice 7 (parser guard), Slice 9B (temporal constants), and Slice 1 (B2 currency/unit) have already shipped, closing multiple gaps.
- Some originally-estimated codes were already emitted at baseline.

### 3. Gate 2 allow-list has 5 entries (Slice 1 B2 codes)

Cross-project analyzer cannot detect test references in `Precept.Tests` for codes emitted in the main `src/Precept/` compilation unit. The 5 B2 codes (PRE0070–0074) are allow-listed with justification.

### 4. Scanner detection covers three emission patterns

`DiagnosticCoverageScanner.Scan()` detects: `Diagnostics.Create()` first argument, `CIDiagnosticCode` property assignment, and `CIDiagnosticCode` named argument. `GetMeta()` calls and enum definitions are excluded.

## Files

- `src/Precept.Analyzers/Precept0027DiagnosticEmissionCoverage.cs`
- `src/Precept.Analyzers/Precept0028DiagnosticTestCoverage.cs`
- `src/Precept.Analyzers/DiagnosticCoverageAllowLists.cs`
- `src/Precept.Analyzers/DiagnosticCoverageScanner.cs`
- `test/Precept.Analyzers.Tests/Precept0027Tests.cs`
- `test/Precept.Analyzers.Tests/Precept0028Tests.cs`
- `docs/Working/diagnostic-enforcement.md` (tracker updated)
