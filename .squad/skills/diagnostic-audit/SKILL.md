# Skill: Diagnostic-Emission Gap Analysis

## When to use

When auditing whether declared diagnostics in a catalog-driven compiler are actually emitted by the pipeline.

## Pattern

### 1. Direct emission search is insufficient

In a catalog-driven architecture, diagnostics may be emitted through **indirect dispatch** — the diagnostic code is stored in catalog metadata (e.g., `Operations.GetMeta(op).CIDiagnosticCode`) and referenced via a variable, not a literal `DiagnosticCode.X`. Grepping for `DiagnosticCode.SpecificName` in the pipeline will miss these.

**Search both patterns:**
- Direct: `DiagnosticCode.X` in pipeline files
- Indirect: Check whether the diagnostic code appears in catalog metadata properties that pipeline stages read. Search for the metadata property name (e.g., `CIDiagnosticCode`, `PreventsFault`) in pipeline dispatch code.

### 2. Test types are not emission evidence

Tests in `DiagnosticsTests.cs` (or equivalent) often validate catalog metadata shape — "every enum member has a `GetMeta` entry." These tests pass whether or not the diagnostic is ever emitted. Only tests that compile a source string and assert the diagnostic appears in the result (`CheckExpectingError(source, DiagnosticCode.X)`) prove emission works.

### 3. Categorize by root cause, not by ordinal

Gaps cluster by domain subsystem, not by PRE number. Group by:
- Parser gates never wired (construct rejection paths)
- TypeChecker domain logic not implemented (full cluster of related diagnostics)
- Speculative/latent (feature not yet designed)
- Precision downgrades (generic diagnostic instead of specific)

### 4. Cross-reference against the language spec

For each gap, check whether `docs/language/precept-language-spec.md` promises the validation. If the spec lists the diagnostic in a semantic checks table, it's an integrity gap — the product claims enforcement that doesn't exist. If the spec doesn't mention it, it may be a catalog entry for an unscoped feature.

### 5. Assess impact by the product's core promise

For Precept: "invalid configurations are structurally impossible." Any gap where invalid data silently compiles clean is a Priority 1 integrity violation. Gaps where the error IS caught but with the wrong diagnostic code are Priority 2 (UX, not integrity).
