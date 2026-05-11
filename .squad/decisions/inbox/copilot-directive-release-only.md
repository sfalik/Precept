# Directive: Release-Only Build Policy

**Date:** 2026-05-11  
**Source:** Shane (explicit directive)

## Directive

> "No debug builds. Production builds can have symbols. I'm a one man show I don't have bandwidth to test two different builds."

## Policy

- Single build configuration: **Release**
- PDB/symbols enabled: `<DebugSymbols>true</DebugSymbols>` + `<DebugType>portable</DebugType>` in `Directory.Build.props`
- All `Debug.Assert`, `Debug.Fail`, `#if DEBUG` must be eliminated from the pipeline
- Replacement: `throw new InvalidOperationException(...)` for invariant violations; proper diagnostics for user-visible errors
- No Debug configuration in any `.csproj`

## Implemented

- `Directory.Build.props` created at repo root
- 7 `Debug.Assert` sites in pipeline converted (see pipeline-audit-fix-plan.md)
