## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Diagnostic additions should be semantically precise; do not overload unrelated codes when the compiler contract changes shape.
- Shared-environment build discipline still matters: targeted validation beats noisy full-solution churn when the workspace may be busy.

## Learnings

### 2026-05-11 — Slice 11B: Temporal Price Denominator

- Pre-existing spike branch failures (26 tests in `TypeCheckerAssemblyTests` + `TypeCheckerAssignmentQualifierTests`) produce `ExpectedToken` parser errors unrelated to type checker work. Always baseline before claiming new failures.
- `TypeMeta` is a positional record — adding a new optional parameter at the end is safe because all call sites use named parameters. The computed-property-shadowing-parameter pattern (`public T[] Foo { get; } = Foo ?? []`) is the established idiom here.
- `typeRef.ResolvedKind` on a `QualifiedTypeReference` delegates to `InnerType.ResolvedKind` — no need to unbox manually; the property dispatch covers it.
- Dimension→TemporalDimension fallback is intentionally broad: any field with a `TemporalDimension` declared qualifier satisfies a `Dimension`-axis chain requirement. Cross-domain mismatches (physical vs temporal) are caught by `ExtractComparableValue` string comparison, not axis gating.
- Adding `internal static` test entry points to private ProofEngine helpers (behind `InternalsVisibleTo`) is the established pattern and keeps tests exercising real code paths without overexposing the API.

## Live Guidance

- Mutual-exclusion metadata must be symmetric; PRECEPT0011c makes one-way declarations non-buildable.
- Interpolation remains feasible, but Slice 2 is the dominant cost center and binder plus multiple LS walkers must ship with it to avoid checker/tooling drift.
- Runtime/model changes should preserve current semantic surfaces unless the task explicitly widens them.

## Historical Summary

- Earlier Track 2, parser-gap, proof-engine, and diagnostic-split detail was archived into `history-archive.md` during the 15 KB summarization pass.
- `.squad/decisions.md` remains the canonical chronology; this file now keeps only current implementation guidance and the newest merged outcomes.

## Recent Updates

### 2026-05-11T21:23:24.768-04:00 — Slice 12 temporal chain validation complete
- `src/Precept/Language/Operations.cs`: `Operations.Create()` switch arms at lines 615-633 now add `QualifierChainProofRequirement` entries for `PriceTimesPeriod` and `PriceTimesDuration`.
- Constructor shape confirmed from `src/Precept/Language/ProofRequirement.cs` lines 106-112: `QualifierChainProofRequirement(ProofSubject LeftSubject, QualifierAxis LeftAxis, ProofSubject RightSubject, QualifierAxis RightAxis, string Description)`.
- Added `test/Precept.Tests/ProofEngineTemporalChainTests.cs`: `Prove()` at lines 12-16, `AssertSingleChainObligation()` at lines 19-25, and 12 scenario tests at lines 30-169 covering proved matches, mismatches, bare-operand obligations, and regression anchors.
- Validation: targeted class passed all 12 tests; full `dotnet test test/Precept.Tests/` remains at 26 pre-existing spike-branch failures, unchanged.

### 2026-05-12T01:05:25Z — Inventory coverage audit tightened remaining interpolation follow-up
- `inventory-item.precept` still exposes a compound-unit interpolation gap, so the checker follow-up needs `unitofmeasure` U2 plus quantity compound patterns for forms like `'{StockingUnit}/{PurchaseUnit}'` and `'0 {StockingUnit}/{PurchaseUnit}'`, with dimensional validation of the resulting compound unit.
- No new proof-engine bug was confirmed: BUG-B remains covered through interpolation plus Slice 9 fallback, while BUG-A now looks like an explicit regression-gap risk once event args parse cleanly.

### 2026-05-11 — Slice 11B: Temporal Price Denominator Type System Extension
- Added `ImpliedQualifiers` to `TypeMeta` record; Duration entry carries `TemporalDimension(Time, Baseline)`.
- Extended `ExtractQualifiers` in TypeChecker: `price of 'time'`/`'date'` routes to `MapTemporalDimensionQualifier`; `quantity of 'time'` still emits `InvalidDimensionString` (type-gated guard).
- Added `TemporalUnit` and `TemporalDimension` arms to `ExtractComparableValue`; `PeriodDimension.Any` → null (locked).
- Extended `ResolveQualifierOnAxis` with `Dimension → TemporalDimension` fallback and implied-qualifier loop.
- Added `ExtractComparableValueForTest` and `GetImpliedQualifierOnAxis` internal test entry points.
- MCP DTO: added `string[]? ImpliedQualifiers` to `TypeCatalogEntryDto`; rendered as `"Axis:Value"` strings.
- 13 new tests, all pass. 26 pre-existing spike branch failures unchanged.
- Slice 12 (PriceTimesPeriod/PriceTimesDuration chain requirements) is now unblocked.

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

### 2026-05-11 — Slice 12: Temporal Chain Validation — BLOCKED
- Investigated G8 (PriceTimesPeriod) and G13 (PriceTimesDuration) for QualifierChainProofRequirement additions.
- Finding: price type uses `QS_CurrencyAndDimension` with `of` → `QualifierAxis.Dimension` (physical). No temporal qualifier axis exists on price. No `per` preposition exists in the token catalog. Duration is unqualified.
- Adding chain requirements would break existing valid `price × period` arithmetic (ResolveQualifierOnAxis returns null → proof always fails).
- `ExtractComparableValue` also lacks TemporalDimension/TemporalUnit arms.
- Deferred: requires price type extension with temporal denominator support before catalog entries can be added.
- Decision filed: `.squad/decisions/inbox/george-slice12-blocked.md`.

### 2026-05-11T22:41:49Z — Slice 6: ProofEngine compositional constraint propagation (S6)
- Added ProofStrategy.CompositionalConstraint = 6 and TryCompositionalConstraintProof strategy.
- Strategy discharges numeric obligations on fields whose ALL assignments are TypedInterpolatedTypedConstant nodes where magnitude/whole-value slot source carries a satisfying modifier (nonzero, positive, etc.).
- Conservative intersection semantics: ALL assignment paths must satisfy; any non-interpolated assignment causes decline.
- Helpers: FindInterpolatedAssignments (scans transition rows + event handlers), GetMagnitudeSlotSource (magnitude → whole-value fallback), ResolveSourceModifiers (field + arg ref resolution).
- Reuses existing SatisfactionCovers() for subsumption — no new proof logic.
- 10 new tests, all 193 ProofEngine tests pass.

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

### 2026-05-11T23:43:07Z — Slice 12 handoff and sample fallout recorded
- Frank completed the unblock design for Slice 12: temporal price denominators stay on `of`, duration gains implied temporal-dimension qualifiers, and Strategy 5 needs Dimension→TemporalDimension comparison support.
- Cross-agent inventory analysis says `samples/inventory-item.precept` will need follow-up on binary-chain qualifier propagation, ensure-expression coverage, and bare `unitofmeasure` semantics.

### 2026-05-12T00:01:51Z — Temporal price denominator design locked for Slice 12
- Frank validated the additive path: temporal price denomination stays on the existing `of` qualifier (`price of 'time'`, `price of 'date'`), so no new `per` keyword or separate grammar branch is needed.
- `quantity of 'time'` remains invalid; temporal semantics still route through `duration` / `period`, and `duration` should surface implied temporal-dimension metadata through `TypeMeta.ImpliedQualifiers` rather than checker hardcoding.
- Slice 12 follow-through needs comparison/proof support for Dimension→TemporalDimension fallback and must keep chain validation gated to price fields that explicitly carry the new temporal `of` qualifier.
- Shane follow-ups remain open on `quantity in 's'` hinting, `money ÷ duration -> price` inference, and `period ×/÷ integer` operator support.

### 2026-05-11T21:26:23.861-04:00 — Slice A2B compound-unit interpolation landed
- The type-grammar table in `src/Precept/Pipeline/TypeChecker.Expressions.cs` is static-array driven: each target type maps through `GetFormsForType()` to `SegmentForm(TextMatch[] TextChecks, InterpolationSlotKind[] Slots)` entries, and `TryMatchForm()` walks the parser's `2N+1` alternating segment shape to assign slot identities by hole index.
- Slot identity lives in `src/Precept/Pipeline/SemanticIndex.cs` as `InterpolationSlotKind` (the plan still calls it `SlotIdentity`). Slice A2B added `NumeratorUnit` and `DenominatorUnit` there.
- Exact text-segment matching is delegate-based, not token-based: separator checks happen through `TextMatch` functions over `TextSegment.Text`, and the slash separator is the literal `MatchSlash(string text) => text == "/"` check. Literal spaces in mixed forms still use exact inline checks like `(string s) => s == " "`.
- A2B added quantity Q5 (`'{n} {A}/{B}'`) plus a dedicated `UnitOfMeasureForms` compound-unit pattern for `'{A}/{B}'`; quantity Q6 was intentionally not added because the existing quantity whole-value lane is `H[whole-value]`, not a unit-only compound form.

