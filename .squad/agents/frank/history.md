## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs derive from durable catalog shape rather than enum-identity switches or parallel lists.
- Interpolation work must preserve compile-time guarantees first; plans that trade structural certainty for runtime validation remain philosophically out of bounds.
- Proof and qualifier fixes should stay bounded to catalog metadata and conservative symbolic reasoning rather than speculative provenance systems.

## Live Guidance

- String holes remain out of scope for interpolated typed constants; typed-hole composition is the only acceptable path.
- Slice 6 stays numeric-only, including the single-hole whole-value fallback; qualifier, dimension, modifier, and presence obligations remain declaration-driven.
- Temporal semantics stay with `duration` / `period`; `quantity of 'time'` remains invalid, while temporal-denominated prices stay on `price of ...`.
- MCP/public tooling contracts should continue to expose curated projections rather than raw core catalog records.

## Historical Summary

- 2026-05-11 work established the current research baseline: inventory-item root-cause triage, Part D pre-existing test-failure design, Slice 2B completion audit, and the durable MCP projection boundary. The decision ledger and any existing history archives remain the source of full chronology.
- Early 2026-05-12 work locked the temporal qualifier guidance, reconciled the proof-plan status, and appended the Newman-ready DTO-free MCP execution plan plus context-window mitigation notes.

## Recent Updates

### 2026-05-12T03:07:25.498-04:00 — Post-recovery inventory-item verdict corrected
- inventory-item is not gated on missing language features; the parser and compound-unit support already shipped.
- Remaining state is **21 diagnostics**: **16 compiler** (exchange-rate / symbolic qualifier equality) and **5 sample** (division-by-zero guards and margin-expression design).
- Recommended next sequence: fix the stale inventory-item header, design symbolic equality for interpolated qualifiers, then revisit the sample-only diagnostics.
- Validation snapshot: **5471/5471 tests passing** with a clean working tree.

### 2026-05-12T03:33:33Z — Scalar-op qualifier propagation design locked as D4
- Kept the work in Part D, not Part C: scalar `money|quantity|price ×|÷ decimal` propagation fixes syntax-reference/test fallout but does not move inventory-item.
- Locked the metadata-driven approach: `ResultQualifierPolicy.InheritFromQualifiedOperand` → `QualifiedOperandInherited` binding → transitive `ResolveQualifierOnAxis` handling for `TypedBinaryOp` subjects.
- Durable side effect: nested same-qualifier binary expressions can now resolve qualifiers transitively instead of dying at the inner expression boundary.

### 2026-05-12T08:40:00-04:00 — P2/P3/P3b code review complete

- P2 (SourceFieldName symbolic equality): Approved. Clean two-tier design — SourceFieldName at type-check time, ExtractQualifierSourcePath fallback for legacy. ExtractSourceFieldName correctly handles leading empty TextSegment from parser. Cross-subtype comparison (ToCurrency ↔ Currency) is the F4 critical path and is tested.
- P3 (PriceDivideQuantity): Approved. New OperationKind=203, ResultQualifierPolicy.CompoundDimensionElevation, CompoundDimensionElevationRequired binding all follow catalog-driven architecture. TryDeriveCompoundElevationQualifiers in TypedConstants and TryResolveCompoundElevationDimension in ProofEngine are correctly asymmetric (right operand only — compound-quantity is always the divisor).
- P3b (CompoundUnitCancellation Dimension form): Approved. The Dimension fallback in TryGetCompoundUnit was shipped inside the P3 commit, not separately. P3b is test-only — 3 tests validating the `of '{dim}'` form. ExtractCompoundValue in ProofEngine already handles Dimension. Symmetric in both operand orders.
- Full suite: 5496/5496 tests pass (4913 core + 264 LS + 39 MCP + 280 analyzers). No regressions.

### 2026-05-12T13:02:45Z — inventory-item qualifier edit queued for sign-off
- George removed the stale RC1 / RC2 inventory-item header comments after the compiler support landed.
- The remaining BUG-A work is the sample-side `Rate as exchangerate in '{SupplierCurrency}' to '{CatalogCurrency}'` edit, which is waiting on Frank's approval before it is applied.

### 2026-05-12T10:52:21.633-04:00 — Exhaustive inventory-item root-cause review + plan expansion

- Performed exhaustive root-cause analysis of all remaining diagnostics on `samples/inventory-item.precept` (16–19 total across 3 root causes: RC1 compound-unit qualifier bug, RC2 unqualified exchange rate arg, RC3 algebraic DivisionByZero).
- **Key finding: F4 is already implemented.** The `CurrencyConversion` `ResultQualifierPolicy` and its ProofEngine handler already exist. The remaining ReceiveShipment diagnostics are blocked on BUG-C (event arg interpolated qualifiers), not on missing policy. Design decision Q1 is closed.
- RC1 traces to a defect in E2's shipped `ResolveQualifierFromInterpolatedConstant` — it returns only DenominatorUnit for compound-unit constants instead of constructing `{numerator}/{denominator}`.
- Added Part G to plan: G1 (compound-unit fix, 4 diagnostics, immediately actionable) and G2 (algebraic DivisionByZero proof, 3 diagnostics, blocked on G1+BUG-C).
- Rewrote F4 with full reframing documentation.
- Decision filed: `.squad/decisions/inbox/frank-proof-coverage-expansion.md`

## Learnings

- `ResolveQualifierFromInterpolatedConstant` (ProofEngine.cs L1338): handles simple slots correctly but the compound-unit NumeratorUnit+DenominatorUnit pair path is missing. The DenominatorUnit-only fallback (L1369) was designed for price/exchangerate denominator slots, not for compound-unit typed constants that need both slots composed.
- `CurrencyConversionRequired` handler (ProofEngine.cs L1194–1202, L1315–1321): resolves result Currency from exchangerate operand's ToCurrency. Works correctly when the exchangerate has qualifier metadata.
- `ExchangeRateTimesMoney` (Operations.cs L675–684): has `CurrencyConversion` policy + `QualifierChainProofRequirement(FromCurrency, Currency)`. Both already correct.
- `PriceTimesQuantity` (Operations.cs L616–625): has `CompoundUnitCancellation` policy, which correctly propagates currency through the CompoundUnitCancellationRequired handler's currency fallback (L1183–1189).
- The proof engine's DivisionByZero strategy resolves subjects to field names via `GetFieldName()`. Compound expressions (binary ops) return null, causing all proof strategies to decline. G2's algebraic reasoning is a genuine gap.

### 2026-05-12 — BUG-C Exhaustive Design (Part H)

- **Critical finding: BUG-C syntax is already implemented.** `ParseArgumentList()` → `ParseTypeReference()` → `TryParseQualifiers()` handles interpolated qualifiers on event args. `PopulateEvents()` → `ExtractQualifiers()` → `MapInterpolatedQualifier()` produces correct `DeclaredQualifierMeta` subtypes. Direct assignment `Cost = Rate * Amt` compiles clean with 0 diagnostics.
- **Residual bug is pre-existing, not BUG-C-specific:** The accumulation pattern `Total + (Rate * Amt)` fails because `ResolveQualifierFromExpression`'s `CurrencyConversionRequired` handler (ProofEngine.cs:1327) returns `ToCurrency` meta when `Currency` axis was requested. The type checker handles this correctly in `ValidateAssignmentQualifiers` (TypedConstants.cs:72–93) by creating `new DeclaredQualifierMeta.Currency(toCurr)`, but the proof engine lacks this translation.
- **Secondary gap:** `ExtractQualifierSourcePath` (ProofEngine.cs:1080) doesn't handle `FromCurrency`/`ToCurrency` subtypes — falls to `_ => null`.
- **Workaround confirmed:** Splitting compound expressions across multiple `set` actions compiles clean (each assignment is direct, avoiding nested binary ops).
- Key parser entry points for event arg qualifiers: `ParseArgumentList()` L772, `TryParseQualifiers()` L622, `ParseTypeReference()` L440. Key type checker path: `PopulateEvents()` L462 → `ExtractQualifiers()` L103 → `MapInterpolatedQualifier()` L154.
- Decision filed: `.squad/decisions/inbox/frank-bugc-design.md`

### 2026-05-12T13:06:50.365-04:00 — Proof Gap Consolidated Assessment

- **All 9 gaps (G1–G9) are DONE.** Every proof requirement identified in `proof-gaps-issues.md` has been implemented in the Operations.cs catalog and the ProofEngine handles them correctly.
- **ConstraintRefs is DONE.** `SemanticSubjects` removed from `TypedRule`/`TypedEnsure`. `CollectFieldRefs`/`CollectArgRefs` walkers implemented in TypeChecker.cs L1463–1512. `ctx.ConstraintRefs.Add()` calls at L752, L819, L876. Tests assert non-empty influence.
- **ExchangeRateTimesMoney nested addition is FIXED.** Commit `ba576b08` added `TranslateCurrencyAxis` (ProofEngine.cs L1356–1368) which converts `ToCurrency` → `Currency` when resolving through `CurrencyConversionRequired`. Test `ExchangeRateTimesMoney_InNestedAddition_UsesCurrencyAxisResult` (L4910–4928) passes.
- **`ExtractQualifierSourcePath` now handles `FromCurrency`/`ToCurrency`** (ProofEngine.cs L1085–1086). The secondary gap from BUG-C analysis is closed.
- **3 DiagnosticsTests failures remain** — `UnprovedQualifierCompatibility` format string uses 6 args (`{0}`–`{5}`) but test fixtures pass only 4. Trivial fix: add 2 more placeholder args.
- **MCP server running stale code** due to Analyzers `MSB3492` cache issue. Does not affect xUnit test suite (4933/4936 passing, 3 failures are the format string issue).
- **G7 (expression-result qualifier provenance)** is architecturally PARTIAL but has no practical gap for current operations. The recursive binary-operand fallback in `ValidateAssignmentQualifiers` is conservative-correct. Only becomes urgent if a new `ResultQualifierPolicy` produces qualifiers different from both operands.
- Decision filed: `.squad/decisions/inbox/frank-proof-gaps-plan.md`
