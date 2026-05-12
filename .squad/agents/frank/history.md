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

### 2026-05-12T14:57:13.598-04:00 — Field-State Guarantee Exhaustive Investigation

- **Critical finding:** The compiler does NOT enforce field-state access mode constraints (omit/readonly) during transition action resolution, guard expression resolution, or state-hook action resolution. `ResolveAction` (TypeChecker.Expressions.Callables.cs:102) and `ResolveActionTarget` (line 314) are completely state-blind — they look up fields in the global `ctx.FieldLookup` dictionary without any state-scoped filtering.
- **Root cause:** `CheckContext` (CheckContext.cs:28) has no `CurrentFromState` or field-accessibility property. The `fromState` string is resolved as a local variable in `NormalizeTransitionRow` (TypeChecker.cs:1097) but never propagated into expression resolution context.
- **Pipeline ordering issue:** `PopulateAccessModes` (TypeChecker.cs:47) runs AFTER `PopulateTransitionRows` (line 41), so even if action resolution wanted to consult access modes, they haven't been populated yet.
- **`TypedAccessMode` records are inert:** Populated and stored in `SemanticIndex.AccessModes` but never consulted by any pipeline stage for enforcement. Neither `GraphAnalyzer` nor `ProofEngine` reference them.
- **Fix architecture:** Post-resolution validation pass (`ValidateFieldStateAccess`) running after both `PopulateTransitionRows` and `PopulateAccessModes` complete. Builds `(stateName, fieldName) → ModifierKind` lookup from unconditional access modes, then walks transition rows and state hooks checking actions/guards against it.
- **6 new diagnostic codes proposed:** D128 `WriteToOmittedField`, D129 `ReadOmittedFieldInGuard`, D130 `ReadOmittedFieldInActionExpression`, D131 `WriteToReadOnlyField`, D132 `WriteToOmittedFieldInStateHook`, D133 `WriteToReadOnlyFieldInStateHook`.
- **Sample audit:** Only 1 violation found across 30 sample files — `insurance-claim.precept:43` (confirmed trigger). No readonly access modes used in any sample file.
- **Test coverage gap:** Zero existing tests for field-state enforcement in transitions. `ConflictingAccessModes` (D42) and `RedundantAccessMode` (D43) are defined but untested. `ComputedFieldNotWritable` (D38) is the closest existing write-blocking diagnostic.
- Proposal written to `docs/working/field-state-guarantees.md`.
- Decision filed: `.squad/decisions/inbox/frank-field-state-guarantees.md`

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

### 2026-05-12T14:59:00-04:00 — Comma-delimited state list syntax spike complete

- Investigated proposal: comma-delimited lists in `StateTarget` slots (anywhere `any`/`all` currently serves as quantifier over states).
- **Key finding: `FieldTarget` already supports comma lists** (`Identifier ("," Identifier)* | all`) — the pattern exists in the grammar. `StateTarget` currently only accepts `Identifier | any`. Extending `StateTarget` to match `FieldTarget`'s pattern is grammatically consistent.
- **Multi-state `from` is pure syntactic sugar** — deterministic expansion to N independent `TypedTransitionRow`s. Zero runtime/evaluator/graph-analyzer/proof-engine changes.
- **Parser entry point:** `ParseStateTarget` (Parser.cs L878) — currently single-identifier + `IsStateWildcard`. Model to follow: `ParseFieldTarget` (Parser.cs L932) comma loop.
- **Type checker expansion point:** `NormalizeTransitionRow` (TypeChecker.cs L1083) and analogous normalization methods for StateEnsure, AccessMode, OmitDeclaration, StateAction.
- **Multi-event `on` lists are a separate domain** with the arg-shape compatibility problem. No adjacent system combines multi-event transitions with typed arguments (see `transition-shorthand.md` §Arg-Shape Compatibility).
- **Corpus impact:** ~11 rows saved out of 196 (~5.6%) — concentrated in `it-helpdesk-ticket`, `utility-outage-report`, `hiring-pipeline`.
- Spike doc filed: `docs/working/comma-list-syntax-spike.md`
- Decision filed: `.squad/decisions/inbox/frank-comma-list-spike.md`

### 2026-05-12T15:15:54-04:00 — Comma-list spike revised: full scope (states + events)

- Shane directed full scope — events are NOT deferred. Comma-delimited lists apply to both `StateTarget` and `EventTarget` in one proposal.
- **Arg-shape compatibility: intersection semantics locked.** Guards and actions on multi-event rows may only reference event args that exist on ALL events in the list with compatible types. Type checker enforces at compile time. This has no precedent in adjacent systems — CSP events are unparameterized, SCXML uses flat data objects, XState separates context from payloads.
- **Alternatives rejected:** Event-gated guard syntax (per-event guards in one row — grammar complexity too high, readability destroyed). No-guard restriction (unnecessarily limiting when intersection semantics are sound).
- **Multi-event desugaring is substitution-based**, not pure copy like multi-state. Event-arg references (`E1.Reason`) are rewritten to the specific event's arg name in each desugared row. No-arg-reference rows degenerate to pure copy.
- **Corpus finding:** The current 20-sample corpus has ZERO clean multi-event candidates. The closest is `refund-request` where `Decline.Note` / `Cancel.Reason` differ in arg name and optionality. Multi-event value is future-facing — terminal-event families in enterprise domains.
- **Combined expansion:** `from A, B on E1, E2` desugars to the Cartesian product (state-major ordering).
- **Implementation cost delta:** Events add ~90 lines of type-checker logic (arg-shape validation + substitution) and 3 new diagnostic codes over the states-only estimate.
- 5 new diagnostics total: `StateListContainsWildcard`, `DuplicateStateInList`, `DuplicateEventInList`, `EventArgShapeIncompatible`, `EventArgTypeMismatch`.
- Type-checker expansion (Path 2) locked. All state-preposition constructs at once. Selective sample corpus update.
- Revised spike doc: `docs/working/comma-list-syntax-spike.md`
- Decision filed: `.squad/decisions/inbox/frank-comma-list-full-scope.md`

### 2026-05-12T17:08:13-04:00 — Exhaustive documentation coverage audit for comma-list spike

- Shane requested exhaustive doc coverage — every file assessed, no hedging.
- **Grammar generator finding (definitive):** `tools/Precept.GrammarGen/Program.cs` L617 `fromOnHeader` regex already supports multi-state commas in capture group 4 but NOT multi-event in capture group 8. The event capture group must be extended. `tmLanguage.json` is a build output regenerated from this generator — never hand-edited.
- **Parser doc finding (critical):** `docs/compiler/parser.md` L193–200 documents the peek-at-2 disambiguation invariant — "the disambiguation token is always at position 2 in the lookahead window." Multi-state comma lists break this invariant (`from Draft, Pending on Submit` has `on` at offset 4+). This is the most critical doc update; an implementer relying on the invariant would produce incorrect disambiguation logic. Same assumption restated in `name-binder.md` L195–196.
- **MCP verdict:** No code changes required in `tools/Precept.Mcp/`. `CatalogFormatters.FormatSyntax()` reads catalog metadata generically — slot descriptions and construct examples update automatically when `ConstructSlot.cs` and `Constructs.cs` update. No `StateTarget`/`EventTarget` hardcoding in any MCP tool.
- **Catalog-system.md verdict:** No change required — describes catalog architecture, not grammar rules. Does not encode single-vs-list semantics for slots.
- **README verdict:** No change required — shows valid single-state/single-event syntax that remains correct.
- **Compiler doc inventory:** `parser.md` (3 updates including disambiguation invariant), `type-checker.md` (slot types), `name-binder.md` (6 locations), `grammar-generator.md` (1 description string).
- **Samples confirmed:** `hiring-pipeline`, `it-helpdesk-ticket`, `utility-outage-report` — all other samples have no clean consolidation candidates.
- Updated spike doc §7 (exhaustive audit) and §8 (complete file inventory) in `docs/working/comma-list-syntax-spike.md`.
- Decision filed: `.squad/decisions/inbox/frank-doc-coverage-audit.md`
