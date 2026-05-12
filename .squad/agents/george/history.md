## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Diagnostic additions should be semantically precise; do not overload unrelated codes when the compiler contract changes shape.
- Shared-environment build discipline still matters: targeted validation beats noisy full-solution churn when the workspace may be busy.

## Live Guidance

- Mutual-exclusion metadata must be symmetric; PRECEPT0011c makes one-way declarations non-buildable.
- Interpolation remains feasible, but Slice 2 is the dominant cost center and binder plus multiple LS walkers must ship with it to avoid checker/tooling drift.
- Runtime/model changes should preserve current semantic surfaces unless the task explicitly widens them.
- `.squad/decisions.md` is the canonical chronology; `history-archive.md` holds compacted older George detail.

## Historical Summary

- 2026-05-12 summarization passes moved older Track 2, parser-gap, proof-engine, diagnostic-split, Slice 11B, Slice 1, Slices 6/10/11/12, A2B, and earlier inventory fallout detail into `history-archive.md`.
- Current live history keeps only durable guidance plus the newest merged outcomes that other agents are likely to need immediately.

## Recent Updates

### 2026-05-12T04:40:27Z — E1 proof-site operand resolution
- `TryQualifierCompatibilityProof` must read operands from the `TypedBinaryOp` site, not `ParamSubject`, or mismatched qualifiers can bypass PRE0114.
- PRE0114 operand names should come from that same binary-op site; the slice landed in commit `d549b4a5dc478a571ba639ca67ae483ab0ff9fd3` with five new regression tests and seven targeted proof tests passing.

### 2026-05-12T00:40:27.461-04:00 — E4 symbolic qualifier equivalence
- Qualifier compatibility now flows through `QualifiersAreCompatible()`, preserving null handling and the `PeriodDimension.Any` guard while allowing same-source symbolic template comparison across axis subtype boundaries.
- Validation held at the existing single inventory-item baseline failure: build passed, four new symbolic-equivalence tests passed, and PRE0114 now sits at 89 as the expected foundational count.

### 2026-05-12T03:45:15Z — D4 scalar-op qualifier propagation
- Six scalar operations (`*Decimal`, `/Decimal` for money, quantity, price) were missing result-qualifier propagation metadata, causing nested qualifier checks to lose context and emit false PRE0114s.
- The fix adds `InheritFromQualifiedOperand` / `QualifiedOperandInherited` plus transitive proof resolution through nested `TypedBinaryOp` nodes; divide fixtures still need an explicit `nonzero` divisor field.

### 2026-05-12T04:29:05Z — Diagnostic message fixes recorded
- Commit `4535aaa6` closed Elaine's message-template audit; targeted validation stayed green at `818/818`.

### 2026-05-12T03:35:43Z — Bug031 warning expectation landed
- `Parser_Bug031_InterpolatedRejectAndBecause_CompilesClean` now expects the graph-stage `AlwaysRejecting` warning; the DSL fixture stays unchanged because the always-reject row is intentional coverage.

### 2026-05-12T00:01:51Z — Temporal price denominator design locked
- Temporal price denomination stays on the existing `of` qualifier surface (`price of 'time'`, `price of 'date'`); `duration` gains implied temporal-dimension metadata through `TypeMeta.ImpliedQualifiers`.
- Follow-through still needs Dimension→TemporalDimension comparison/proof support and must keep chain validation gated to price fields that explicitly carry the temporal `of` qualifier.

## Learnings

- 2026-05-12 — E2 needed `ResolveQualifierFromExpression()` to synthesize `DeclaredQualifierMeta` directly from `TypedInterpolatedTypedConstant` slots; `DeclaredQualifierMeta.Unit` requires both unit and dimension payloads.
- 2026-05-12 — E3 needed more than currency passthrough. Compound-cancellation dimension proofs must fall back to unit qualifiers and derive numerator dimensions (`kg` → `mass`, `{StockingUnit}` → `{StockingUnit.dimension}`) or nested `Qty * Conv * Price` chains still emit PRE0114.
- 2026-05-12 — On the current inventory-item worktree, E2/E3 cut PRE0114 from 66 to 16; the remaining ReceiveShipment/GrossProfit fallout is tied to deferred `ExchangeRateTimesMoney` propagation plus separate sample edits outside the parenthesization fix.
- 2026-05-12 — P2: `ParseInterpolatedTypedConstant()` always prepends a leading `TextSegment("")` before the first hole. A pure qualifier like `'{CatalogCurrency}'` yields `[TextSegment(""), HoleSegment(...)]`. List-patterns matching `[HoleSegment {...}]` silently miss this; iterate and skip empty TextSegments instead.
- 2026-05-12 — P2: When co-running agents share the filesystem, use `git add <specific-files>` (not `git add -A`) to isolate your commit from other agents' working tree changes.

### 2026-05-12T05:04:03Z — E2 and E3 completed
- Typed interpolated constant qualifier extraction (E2, `8785d753`) and subexpression / compound-unit qualifier propagation (E3, `d3f5aa98`) landed, with `f4db093e` fixing the ReceiveShipment parenthesization follow-up.
- On the current inventory-item.precept worktree, PRE0114 fell from 66 to 16; the remaining 16 are deferred exchange-rate / GrossProfit fallout rather than regressions in the shipped slices.

### 2026-05-12 — P2: Symbolic Qualifier Equality via SourceFieldName
- Added `SourceFieldName string?` to `DeclaredQualifierMeta` (base + 8 subtypes); populated in `MapInterpolatedQualifier` via `ExtractSourceFieldName` helper and in `CreateQualifierFromSlotExpression` directly from field name.
- `QualifiersSymbolicallyEqual` now uses `SourceFieldName` as primary criterion before `ExtractQualifierSourcePath` fallback, enabling cross-subtype equality (ToCurrency == Currency when same source field).
- 8/8 P2 tests pass. PR: https://github.com/sfalik/Precept/pull/141

### 2026-05-12 — P3: price / compound-quantity -> price type algebra
- Added OperationKind.PriceDivideQuantity = 203, ResultQualifierPolicy.CompoundDimensionElevation, and PriceDivideQuantity catalog entry.
- QualifierChainProofRequirement cannot be used for compound-unit fields declared with in 'Y/X' syntax: these fields store only a Unit qualifier, not Dimension. ResolveQualifierOnAxis(..., QualifierAxis.Dimension, ...) returns null for them, causing chain proofs to always fail. The dimension constraint is enforced implicitly via result-qualifier propagation and assignment qualifier checks instead.
- TryDeriveCompoundElevationQualifiers: extracts currency from left (price), splits compound unit from right (quantity), derives numerator dimension, returns [Currency, Unit(numerator, dim, Derived)].
- TryResolveCompoundElevationDimension in ProofEngine: works on RIGHT operand only; extracts compound value, splits at /, derives numerator dimension from unit name.
- 5/5 P3 tests pass; 4910/4910 full suite. PR: https://github.com/sfalik/Precept/pull/142