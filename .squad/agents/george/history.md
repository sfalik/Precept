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

### 2026-05-12T13:52:04Z — Same-qualifier arithmetic metadata and PRE0114 label routing closed
- Commit `d187230c` added `Match: QualifierMatch.Same` to the six same-qualifier money/quantity/price `+/-` operations, so nested arithmetic subexpressions now carry inherited qualifier bindings instead of dropping proof context.
- `ProofEngine` now labels PRE0114 operands with recursive expression descriptions plus resolved qualifier values; George validated the combined fix against the full suite at `5507/5507`.

### 2026-05-12T13:52:04Z — Elaine Section A proof-message rewrites landed
- Commit `1d8962f7` moved PRE0114 to the six-argument "Cannot prove..." format and refreshed PRE0112/PRE0113/PRE0115/PRE0116/PRE0082/PRE0083/PRE0084 wording without widening the requirement metadata surface.
- Targeted core validation stayed green at `4914/4914`, and the proof / diagnostic docs were synced in the same pass.

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

- 2026-05-12T13:35:11.425-04:00 — Slice 12 (`PriceTimesPeriod` + `PriceTimesDuration` temporal chain validation) was already present on `spike/Precept-V2-Radical` in commit `302d53e1`; `Operations.cs` carries both `QualifierChainProofRequirement` entries and `ProofEngineTemporalChainTests` already covers the required temporal-match, mismatch, bare, and regression matrix.
- 2026-05-12T13:35:11.425-04:00 — For spike follow-through on named slices, check `git log -- <files>` before editing: this branch can already contain the catalog/tests, so the remaining work is verification plus squad bookkeeping rather than duplicate runtime changes.
- 2026-05-12T13:31:43.877-04:00 — `samples/inventory-item.precept` still carries stale inline `BUG-A`/`BUG-C` commentary even after the top-of-file compile-blocker header is removed; keep header cleanups surgical and record any compile-status mismatch separately instead of broad comment churn.
- 2026-05-12T13:31:43.877-04:00 — In this workspace, `precept_compile` still reports 15 existing `InventoryItem` diagnostics after the header-only edit, so the brief's "0 diagnostics" claim should be treated as environment/runtime drift until F5 or a follow-up runtime pass confirms otherwise.
- 2026-05-12 — `ProofEngine.ResolveQualifierFromExpression()` must translate `CurrencyConversionRequired` results from `ToCurrency` back onto the caller's `Currency` axis; nested `Total + Rate * Amt` proofs otherwise compare across axes and emit false PRE0114.
- 2026-05-12 — `ProofEngine.ExtractQualifierSourcePath()` also needs `FromCurrency` and `ToCurrency` coverage so literal fallback comparison still works when `SourceFieldName` is absent.
- 2026-05-12 — The inventory-item ReceiveShipment qualifier fallout is cleared at lines 212, 214, 218, 220, 223, and 225; only the G2 denominator PRE0083s remain at 214, 220, and 225.
- 2026-05-12 — `ResolveQualifierFromInterpolatedConstant` now lives at `src/Precept/Pipeline/ProofEngine.cs:1351`; the G1 fix must run before the denominator-only fallback or compound-unit constants collapse to the denominator slot.
- 2026-05-12T11:08:13.750-04:00 — The typed constant AST shape is `TypedInterpolatedTypedConstant(Slots, ResultType, Span, StaticMagnitude)` where each `TypedInterpolationSlot` carries an `Expression` plus `SlotKind`; compound unit literals arrive as `NumeratorUnit` + `DenominatorUnit` slots, not a single `Unit` slot.
- 2026-05-12T11:08:13.750-04:00 — Compound-unit proof coverage now sits in `ProofEngineTypedArgQualifierTests`: `CompoundUnitInterpolatedConstant_ResolvesCompoundUnitQualifier`, `SingleUnitInterpolatedConstant_StillResolvesSingleUnitQualifier`, `CompoundUnitRule_DoesNotEmit_PRE0114`, `CompoundUnitPositivityProof_ClearsDivisionByZero`, and `InventoryItem_Sample_Clears_G1_Diagnostics`.
- 2026-05-12 — E2 needed `ResolveQualifierFromExpression()` to synthesize `DeclaredQualifierMeta` directly from `TypedInterpolatedTypedConstant` slots; `DeclaredQualifierMeta.Unit` requires both unit and dimension payloads.
- 2026-05-12 — E3 needed more than currency passthrough. Compound-cancellation dimension proofs must fall back to unit qualifiers and derive numerator dimensions (`kg` → `mass`, `{StockingUnit}` → `{StockingUnit.dimension}`) or nested `Qty * Conv * Price` chains still emit PRE0114.
- 2026-05-12 — On the current inventory-item worktree, E2/E3 cut PRE0114 from 66 to 16; the remaining ReceiveShipment/GrossProfit fallout is tied to deferred `ExchangeRateTimesMoney` propagation plus separate sample edits outside the parenthesization fix.
- 2026-05-12 — P2: `ParseInterpolatedTypedConstant()` always prepends a leading `TextSegment("")` before the first hole. A pure qualifier like `'{CatalogCurrency}'` yields `[TextSegment(""), HoleSegment(...)]`. List-patterns matching `[HoleSegment {...}]` silently miss this; iterate and skip empty TextSegments instead.
- 2026-05-12 — P2: When co-running agents share the filesystem, use `git add <specific-files>` (not `git add -A`) to isolate your commit from other agents' working tree changes.
- 2026-05-12 — Sample headers can go stale after runtime/parser fixes land; when a blocker moves from compiler support to sample authoring, update the sample comment to name the remaining sample-side edit instead of leaving obsolete root-cause notes.
- 2026-05-12 — Same-qualifier arithmetic must declare `Match: QualifierMatch.Same`, not just `QualifierCompatibilityProofRequirement`; otherwise intermediate `TypedBinaryOp.ResultQualifier` stays null and nested PRE0114 proofs never recurse through the subexpression.
- 2026-05-12 — PRE0114's six-argument rewrite is cleaner when qualifier values are resolved at diagnostic emission time; reusing preformatted operand strings hides the structured left/right qualifier payloads and breaks the shared template for chain proofs.
- 2026-05-12T13:10:03.666-04:00 — G2 landed by intersecting trusted zero-bound facts from rules/event ensures with recursive sign propagation (`+`, `-`, `*`, `/`); `QuantityOnHand >= 0` plus `ReceiveShipment.PurchaseQty > 0` plus `StockingUnitsPerPurchaseUnit > 0` is enough to prove the ReceiveShipment denominator strictly positive.
- 2026-05-12T18:06:53.8406657-04:00 — `StateTargetSlot` can widen to `ImmutableArray<string>` + `ImmutableArray<SourceSpan>` without breaking the pre-S2 pipeline if it keeps compatibility getters for `StateName` / `NameSpan`; parser disambiguation for `RoutingFamily.StateScoped` must scan past `Identifier ("," Identifier)* | any` before applying the existing `when <expr>` search for `on` / `ensure` / `modify` / `omit` / `->`.
- 2026-05-12T18:04:32.430-04:00 — S2 transition-row expansion should resolve the shared event / guard / action / outcome payload once, then loop `StateTargetSlot.StateNames` to emit one `TypedTransitionRow` per source-state name; keep per-name `StateReference` spans and preserve undeclared names as non-null `FromState` values so bad list entries do not silently collapse into wildcard semantics.
- 2026-05-12T18:25:12.9082641-04:00 — Once state-target normalization can drop duplicates, `ImmutableArray<T>.Builder.MoveToImmutable()` becomes a trap: it requires `Count == Capacity`. Use `ToImmutable()` for deduplicating expansions or the checker will throw before diagnostics surface.
- 2026-05-12T18:25:12.9082641-04:00 — In the current runtime, the remaining S3 state-target work lives in `PopulateEnsures`, `PopulateAccessModes`, and `PopulateStateHooks`; `PopulateEditDeclarations` (the current stand-in for omit normalization) never reads `StateTargetSlot`, so omit declarations need documentation only, not list expansion.
- 2026-05-12T18:18:18.326-04:00 — `StateTarget` syntax/help text now needs to describe all three accepted shapes everywhere it is cataloged: a single state name, `any`, and a comma-delimited state-name list. The MCP syntax surface updates automatically once the shared slot metadata/comment text is kept in sync.

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

### 2026-05-12T13:02:45Z — inventory-item header cleanup recorded
- Removed the stale RC1 / RC2 compiler-blocker comments from `samples/inventory-item.precept`; those blockers are already shipped.
- BUG-A is now explicitly a sample-side edit: `Rate as exchangerate in '{SupplierCurrency}' to '{CatalogCurrency}'` still needs Frank's sign-off before George changes the sample.
- Commit: `b389e74e`.

### 2026-05-12T15:15:10Z — Kramer proof hover implementation shipped
- Kramer landed Elaine v4 proof hover across three commits: `5ab6030e` (qualifier hover), `516aa6ba` (proof status hover), and `7829e9c6` (proof chain detail).
- Language-server validation stayed green at `264/264` in `Precept.LanguageServer.Tests`, so George can treat proof-hover routing and cards as shipped editor behavior rather than pending design work.

### 2026-05-12T19:02:04-04:00 — B1 proof-hover blocker fixes landed
- Reworked `RichHoverFactory` proof-gap qualifier/expression/diagnostic rendering onto compact card output and locked the shared badge vocabulary to `✅ Proven`, `⚡ Enforced`, and `⚠️ Gap`; qualifier declaration hovers now emit the compact `⚖️` card instead of the old forensic dump.
- Normalized shipped copy from `proved` to `proven` in user-facing hover text, refreshed `HoverHandlerTests` to the compact-card contract, and validated green at `41/41` targeted hover tests plus `269/269` full language-server tests; repo-wide `dotnet test` is still blocked by the unrelated `TypeCheckerTransitionTests.TransitionRow_MultiStateFromList_MultipleUnknownStates_EmitsPerStateDiagnostic` baseline failure (3 `UndeclaredState` diagnostics vs expected 2).
