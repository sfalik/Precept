# Frank-19: Debug vs. Release Build Policy

**Date:** 2026-05-11  
**Agent:** frank-19 (claude-opus-4.6)  
**Status:** Option B rejected by Shane — Release-only mandate in effect

## Analysis

Examined `Debug.Assert` usage across pipeline files and LS build configuration.

### Option A — Release-only with symbols
Single configuration: Release + PDB. No Debug config.
All `Debug.Assert`/`#if DEBUG` → unconditional `throw new InvalidOperationException(...)`.

### Option B — Keep Debug builds, replace asserts (recommended by frank-19)
Retain Debug config for ergonomics; replace `Debug.Assert` with unconditional `throw`.
This was Frank's recommendation.

## Shane's Decision

**Option A. Release-only. No exceptions.**

> "No debug builds. Production builds can have symbols. I'm a one man show I don't have bandwidth to test two different builds."

## Implemented As

- `Directory.Build.props` (repo root) sets `Release` as default + `DebugSymbols=true`, `DebugType=portable`
- All 7 `Debug.Assert` sites in the pipeline converted to unconditional `throw new InvalidOperationException(...)`
- No `#if DEBUG` blocks remain in `src/Precept/Pipeline/` or `src/Precept/Language/`
