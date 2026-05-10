## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Shared-environment build discipline matters: targeted build/test commands are safer than full-solution runs when the workspace may have external file locks.

## Learnings

- `CurrencyCatalog` is the durable ISO 4217 runtime/catalog surface: transactional currencies only, with case-insensitive `All` lookups and XML-only exclusions living in sync tests rather than the catalog itself.
- ProofEngine requirement discharge stays fact-driven: forwarding facts must be consumed before discharge, already-proved obligations must be skipped, and divisor/nonzero subject resolution must bind the correct operand.
- Error propagation in the TypeChecker should preserve the original diagnostic source and return `TypedErrorExpression` without duplicating parent diagnostics.
- `ParsedConstruct.LeadingTokenKind` is the minimal durable downstream recovery surface when parser-consumed anchor keywords still matter later in normalization or tooling.
- `count` is a collection member/accessor, not a synthetic built-in function; proof and guard consumers should match typed member access.
- Message-position support is catalog metadata on `TokenMeta`/`FunctionMeta`; generator and tooling consumers should derive from the flag instead of maintaining parallel lists.
- Typed-literal validation is catalog-driven end to end: `TypeMeta.ContentValidation` selects the validator, `TypedConstantValidation.Validate(...)` is the only dispatcher, and runtime JSON ingress still goes through `TypeRuntime<T>` / `TypeRuntimeMeta`.
- Temporal and UCUM parsing now live under shared language/runtime parser stacks, while ISO 4217 and UCUM source data remain embedded external reference datasets rather than Precept catalogs.
- `UcumAtomCatalog` is the sole UCUM source of truth; Tier 1 browse surfaces may synthesize normalized/prefixed forms, while full lookup stays XML-backed.
- Business-domain qualifier derivation must be unit-code-aware: not every dimensionless UCUM code is a `count`, and angle/solid-angle forms must bypass the count fallback.
- `TypedField` now carries `SourceSpan NameSpan`; LS consumers should use it rather than whole-construct spans or parser-slot rediscovery.
- AI authoring completeness depends on preserving catalog-carried guidance (examples, diagnostics, proof metadata, operation compatibility), not just exposing more top-level sections.
- `SyntaxReference` examples and anti-patterns must be compile-verified before becoming canonical tooling or MCP guidance.
- The local Precept MCP shell path uses newline-delimited JSON-RPC over stdio; LSP `Content-Length` framing is not the right probe format there.

## Recent Updates

### 2026-05-10T00:47:45Z — Slice 3 core ArgReference recording committed
- Commit `cba898b7` added `ArgReference(TypedArg Arg, SourceSpan Site)` plus `SemanticIndex.ArgReferences`, extended `SemanticIndex.Empty` and `CheckContext`, and kept arg provenance symmetric with the existing field/state/event reference surfaces.
- `TypeChecker.Expressions.cs` now records `ArgReference` at both `TypedArgRef` resolution sites (identifier scope lookup and member-access resolution), and `TypeChecker.cs` persists `ctx.ArgReferences.ToImmutableArray()` into the final semantic index.
- Added `test/Precept.Tests/ArgReferenceTests.cs` with 3 tests; George validated the slice at 3740/3740 passing tests.

### 2026-05-10T00:11:05Z — Slice 0a outline metadata committed
- Commit `d85449ea` extended `ConstructMeta` with `bool IsOutlineNode = false` and `string? OutlineSymbolTag = null`, then marked `PreceptHeader`, `FieldDeclaration`, `StateDeclaration`, `EventDeclaration`, and `RuleDeclaration` as outline nodes with `Module`, `Property`, `Enum`, `Function`, and `Boolean` tags in `src/Precept/Language/Constructs.cs`.
- George added four catalog tests under `test/Precept.Tests/` and validated the branch at 3737 passing tests.

### 2026-05-09T23:46:43Z — TypedField NameSpan landed for LS prerequisites
- George added `TypedField.NameSpan` in `src/Precept/Pipeline/SemanticIndex.cs`, populated it from `DeclaredField.NameSpan` in `TypeChecker`, updated runtime tests, and validated at 3733 passing tests.
- This closes the field-name span blocker raised by the language-server doc reviews; only the preview restore-failure contract remains open.

### 2026-05-09T23:02:39Z — Catalog authoring pass complete
- George-15 finished the catalog authoring pass: `DiagnosticMeta` now carries `TriggerCondition`, `RecoverySteps`, `ExampleBefore`, and `ExampleAfter` across all 116 diagnostics; `SyntaxReference` gained `AntiPattern` plus 8 common patterns / 3 anti-patterns; and `src/Precept/Language/Quickstart.cs` added `QuickstartCatalog`.
- Validation closed green at 3733/3733 `Precept.Tests`, 26/26 `Precept.Mcp.Tests`, and zero build warnings.

### 2026-05-09T17:41:32Z — Typed-literal system plan completed
- George completed all 12 slices of `typed-literal-system-plan`: embedded ISO/UCUM sources, XML-backed loaders, temporal and UCUM parsers, validator framework wiring, TypeChecker migration, and canonical doc sync.
- Durable boundary: runtime measure types remain explicit stubs; compile-time parsing/validation is complete while runtime arithmetic integration is future work.

### 2026-05-09T15:33:49Z — Six-slice commit batch recorded
- Scribe recorded George-6's six logical commits spanning catalog metadata, pipeline consumers, analyzers, docs, tooling, and squad-state follow-up.
- Commit sequence preserved for traceability: `b1c95512`, `d27fae6b`, `a1956961`, `9c608dc2`, `b9009a2a`, `dbfac08e`.

## Historical Summary

- Earlier 2026-05-09 work locked the transactional `CurrencyCatalog` boundary, the XML drift-test posture, and the targeted tooling workflow for ISO refreshes.
- 2026-05-08 work restored GraphAnalyzer/ProofEngine behavior, structural diagnostics, and the broader semantic-index assembly pipeline.
- 2026-05-07 and earlier groundwork established the catalog-driven parser/checker trajectory and the rule that durable design decisions belong in catalogs, canonical docs, and the squad ledger rather than in scattered switches.

## Latest Slice

- Slice 3 core complete: `ArgReference(TypedArg Arg, SourceSpan Site)` and `SemanticIndex.ArgReferences` landed as the semantic-index arg provenance surface.
- `CheckContext`, `SemanticIndex.Empty`, and both `TypedArgRef` resolution paths now record arg references before `TypeChecker` seals them into the final semantic index.
- Tests: 3 facts added in `test/Precept.Tests/ArgReferenceTests.cs`; George validated at 3740/3740 passing tests.
