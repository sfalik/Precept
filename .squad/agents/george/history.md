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
- Snippet templates are syntactically valid DSL strings: templates for constructs match top-level declaration forms; templates for actions match their respective `ActionSyntaxShape` grammars. Derived from sample files, not invented.

- Typed-literal validation is catalog-driven end to end: `TypeMeta.ContentValidation` selects the validator, `TypedConstantValidation.Validate(...)` is the only dispatcher, and runtime JSON ingress still goes through `TypeRuntime<T>` / `TypeRuntimeMeta`.
- Temporal and UCUM parsing now live under shared language/runtime parser stacks, while ISO 4217 and UCUM source data remain embedded external reference datasets rather than Precept catalogs.
- `UcumAtomCatalog` is the sole UCUM source of truth; Tier 1 browse surfaces may synthesize normalized/prefixed forms, while full lookup stays XML-backed.
- Business-domain qualifier derivation must be unit-code-aware: not every dimensionless UCUM code is a `count`, and angle/solid-angle forms must bypass the count fallback.
- `TypedField` now carries `SourceSpan NameSpan`; LS consumers should use it rather than whole-construct spans or parser-slot rediscovery.
- AI authoring completeness depends on preserving catalog-carried guidance (examples, diagnostics, proof metadata, operation compatibility), not just exposing more top-level sections.
- `SyntaxReference` examples and anti-patterns must be compile-verified before becoming canonical tooling or MCP guidance.
- The local Precept MCP shell path uses newline-delimited JSON-RPC over stdio; LSP `Content-Length` framing is not the right probe format there.
- Track 2 Phase A can land safely as a mixed catalog-first batch only when the consumer changes are tiny derivations from newly added metadata (`Parser.KeywordsValidAsMemberName`, modifier-bound validation, `abs()` proof discharge). Deeper plan items like valued event-arg modifiers and construct-slot rewiring still need later slices.
- `OperatorMeta` can carry `StaticResultType` / `ResultTypePolicy` as catalog coverage without changing current typing behavior because live expression typing already flows through `Operations`; the metadata addition is still valuable for Phase A completeness and tests.
- Token metadata only closes wildcard/broadcast regressions when binder/type-checker consumers also read the metadata; parser acceptance alone still lets `any` / `all` collapse back into undeclared-name diagnostics.

## Recent Updates

### 2026-05-10T04:20:44Z — Track 2 value modifier rename landed
- George completed the Track 2 modifier-family rename and applicability-shape cleanup: the core subtype is now `ValueModifierMeta`, `ApplicableToEventArgs` is gone, and `ValueModifierDeclarationSite` carries the declaration-site truth with `writable` restricted to field declarations.
- Parser, type checker, proof, and MCP-facing core surfaces now read the canonical value-modifier metadata directly, and George synced the core docs plus the durable decision handoff that Scribe merged into `.squad/decisions.md`.
- Validation closed green on the reported runtime lane: Precept build passed, `Precept.Tests` passed, and `Precept.Mcp.Tests` passed.

### 2026-05-10T03:27:00Z — Track 2 Phase A safe batch landed
- George landed the immediately safe Phase A batch across tokens, modifiers, operators, constructs, outcomes, and functions, plus minimal consumer derivations in parser, type-checker validation, and proof discharge.
- New coverage includes keyword-member-name parsing, modifier-bound diagnostics, outcome/function/operator catalog assertions, and a proof regression showing `sqrt(abs(X)) >= 0` now discharges cleanly.
- Validation closed green at 3830/3830 `Precept.Tests` plus a targeted `src\Precept\Precept.csproj` build using isolated artifact paths.

### 2026-05-10T02:50:04Z — Team update: visual taxonomy now has a canonical catalog surface
- Frank's token-surface decision is now durable: `SemanticTokenTypes` is the approved 14th catalog, `TokenMeta` collapses to a single `VisualCategory`, and token-surface projections (custom type, TextMate scope, base styles, constrained-modifier support) derive from that catalog rather than parallel token fields.
- Shane's event-italic ruling is also locked: constrained events keep `SupportsConstrainedModifier = true`, with italic as the universal constraint-pressure signal and blocked events stacking italic plus dim.

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

- Track 2 Phase A safe batch complete: `TokenMeta` gained wildcard/broadcast/function/member-name flags; `FieldModifierMeta` gained event-arg/bound metadata; `OperatorMeta` gained `StaticResultType` and `ResultTypePolicy`; `ConstructMeta`, `OutcomeMeta`, and `FunctionOverload` gained their Phase A fields.
- Minimal consumer reads landed only where they were low-risk and already aligned with live code: parser derives keyword member names and `min`/`max` function-call eligibility from token metadata, type-checker validates explicit min/max-style bound pairs, and proof discharge recognizes nonnegative-return function overloads such as `abs`.
- Deliberately deferred: valued modifier payloads on event args and any construct-slot grammar rewiring beyond metadata flags.

- Slice 16 complete: `SnippetTemplate` populated on 5 top-level completion constructs (`PreceptHeader`, `FieldDeclaration`, `StateDeclaration`, `EventDeclaration`, `RuleDeclaration`) in `Constructs.cs`, and on 11 primary action verbs (`set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `append`, `insert`, `put`) in `Actions.cs`.
- Templates use VS Code snippet format with `${N:placeholder}` tab stops, derived from sample `.precept` files. Each template leads with the keyword and includes one tab stop per required authoring slot.
- 6 new tests in `test/Precept.Tests/Language/ConstructCatalogTests.cs` and `ActionCatalogTests.cs`; 3750 total tests passing.
- Build validated with isolated `--artifacts-path temp/george-slice16-tests` to avoid shared-environment file lock collisions; language server build also green.

### 2026-05-10T04:33:18Z — Track 2 plan merged, but execution is paused
- Scribe merged the Track 2 master-plan, Phase A guardrail, and catalog-test gate notes into the canonical decision ledger: Track 2 stays metadata-first, Slice 2 is audit-only, and later consumer slices own parser/checker/proof/MCP rewires.
- Shane then switched active execution back to Track 1 only. Do not start new Track 2 implementation work until he explicitly reopens the lane.
