## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Diagnostic additions should be semantically precise; do not overload unrelated codes when the compiler contract changes shape.
- Shared-environment build discipline still matters: targeted validation beats noisy full-solution churn when the workspace may be busy.

## Live Guidance

- Mutual-exclusion metadata must be symmetric; PRECEPT0011c makes one-way declarations non-buildable.
- Interpolation remains feasible, but Slice 2 is the dominant cost center and binder plus multiple LS walkers must ship with it to avoid checker/tooling drift.
- Runtime/model changes should preserve current semantic surfaces unless the task explicitly widens them.

## Historical Summary

- Earlier Track 2, parser-gap, proof-engine, and diagnostic-split detail was archived into `history-archive.md` during the 15 KB summarization pass.
- `.squad/decisions.md` remains the canonical chronology; this file now keeps only current implementation guidance and the newest merged outcomes.

## Recent Updates

### 2026-05-11T22:41:00Z — Slice 1 Parser for InterpolatedTypedConstantExpression complete
- Rewrote `ParseInterpolatedTypedConstant()` to produce `InterpolatedTypedConstantExpression` with full segment AST (mirrors `InterpolatedStringExpression`).
- Added `ExpressionFormKind.InterpolatedTypedConstant = 15` with catalog metadata; moved `TypedConstantStart` from Literal LeadTokens to the new form.
- Added NameBinder arms for both `CollectFieldDependencies` and `WalkExpression`.
- TC stub (`ResolveInterpolatedTypedConstantExpressionStub`) routes the new node to crash-prevention diagnostics; the old `ResolveInterpolatedTypedConstantStub` for `LiteralExpression(TypedConstantStart)` is now dead code.
- 10 parser round-trip tests added covering 1/2/3-hole patterns, expressions in holes, and form kind verification.
- Segment shape: always starts and ends with `TextSegment` (may be empty); for N holes → 2N+1 segments.

### 2026-05-11T20:03:33Z — ConflictingModifiers implementation recorded
- George's canonical closeout is `DiagnosticCode.ConflictingModifiers = 120`, dedicated validator routing, and symmetric `MutuallyExclusiveWith` declarations on `Optional` and `Notempty`.
- The PRECEPT0011c symmetry requirement is now a durable implementation note for any future mutual-exclusion work.

### 2026-05-11T20:03:33Z — Interpolation LOE warning retained
- The interpolation plan is still feasible, but Slice 2 owns most of the complexity and the binder / language-server walker follow-through must be treated as in-scope work, not cleanup.

### 2026-05-11T22:41:00Z — Slices 10+11: Assignment qualifier propagation
- Slice 11 (G9): Added `FromCurrency` and `ToCurrency` cases to `ValidateAssignmentQualifiers` switch. Exchange rate field-to-field assignment now validates from/to currency match.
- Slice 10 (G7): Added binary/unary expression handling to `ValidateAssignmentQualifiers` via recursive leaf operand extraction (`ExtractLeafOperands`). `set usdField = eurField + eurField` now correctly produces `QualifierMismatch`.
- Architecture decision: leaf-extraction approach over proof obligations — keeps consistency with existing direct-diagnostic pattern in `ValidateAssignmentQualifiers`.
- Known limitation: bare-operand-to-qualified-target (`set usdField = bareField + bareField`) deferred to proof engine scope.

### 2025-07-11 — Part B Slices 7+8+9: Proof engine qualifier coverage
- Slice 7: Added QualifierCompatibilityProofRequirement on QualifierAxis.Currency to all 8 money operations (2 arithmetic + 6 comparison). Closes critical gap where `money in 'USD' + money in 'EUR'` had no proof enforcement.
- Slice 8: Introduced QualifierChainProofRequirement DU subtype with dual-subject, dual-axis design for cross-type qualifier chains. Added to ExchangeRateTimesMoney (FromCurrency↔Currency) and PriceTimesQuantity (Dimension↔Dimension). Extended ProofEngine Strategy 5 with chain comparison via ExtractComparableValue.
- Slice 9: Added Unit→Dimension fallback in ResolveQualifierOnAxis so dimension-only fields satisfy Unit-axis proofs.
- Updated MCP tools (LanguageTool, ProofsTool) for chain rendering and dual-subject classification.
- Updated ProofRequirementCatalogTests for 6th kind. 19 new ProofEngine tests, all 193 pass.

### 2025-07-24 — Slice 2: Full type-grammar matching for interpolated typed constants
- Replaced both interpolated typed constant stubs with complete ResolveInterpolatedTypedConstant() implementation.
- Redesigned form matching from element-per-element to segment-aware model using SegmentForm with TextMatch delegates, correctly handling parser's 2N+1 segment structure.
- Per-type form tables: Money (4), Quantity (4), Price (8), ExchangeRate (8), Currency/UoM/Dimension (1 each), Duration/Period (4 single + compound).
- Added 4 diagnostics: InvalidInterpolatedTypedConstantForm (121), InterpolationNotSupportedForType (122), InterpolatedTypedConstantHoleTypeMismatch (123), DimensionMismatchInUnitSlot (124).
- Added TypedInterpolatedTypedConstant, TypedInterpolationSlot, InterpolationSlotKind to SemanticIndex.
- 39 new tests, 4 existing tests updated. All 129 typed constant tests pass.

### 2026-05-11T22:41:49Z — Squad batch closeout
- Slice 1 parser work landed with `InterpolatedTypedConstantExpression` and 10 parser tests.
- Part B slices 7+8+9 landed proof-engine qualifier enforcement, chain requirements, and dimension fallback.
- Slices 10+11 landed assignment qualifier propagation plus `FromCurrency`/`ToCurrency` handling.
- Slice 2 and Slice 12 remain in progress under the current batch.
