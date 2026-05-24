# Appendix — Type-System Docs Audit (Sub-Agent Report)

**Date**: 2026-05-24
**Scope**: `docs/language/primitive-types.md` + `temporal-type-system.md` + `business-domain-types.md` vs implementation
**Author**: Sub-agent of the 2026-05-24 compiler-readiness review
**Findings**: 16 — 2 P0, 8 P1, 6 P2
**Parent doc**: [../compiler-readiness-review-2026-05-24.md](../compiler-readiness-review-2026-05-24.md)

## § A. Methodology

Three docs read in full. Cross-checked against:
- Catalog: `Types.cs`, `TypeKind.cs`, `Operations.cs`, `OperationKind.cs`, `Functions.cs`, `FunctionKind.cs`, `Modifiers.cs`, `ModifierKind.cs`, `OperatorKind.cs`, `Operators.cs`, `CurrencyCatalog.cs`, `Ucum/`, `Time/`
- Pipeline: `TypeChecker.cs` + partials (.Expressions, .Expressions.Callables, .Expressions.AssignmentQualifiers, .Expressions.TypedConstants, .Validation.CI, .Validation.Modifiers)
- Diagnostics: `DiagnosticCode.cs`, `Diagnostics.cs`
- MCP `precept_types(scope:types)` live catalog dump
- Test artifacts: `Slice11B_TemporalPriceDenominatorTests.cs`, `ProofEngineTemporalChainTests.cs`, validator tests, `OperationsTests`, `FunctionsTests`, `ModifiersTests`, `CatalogTests/TypeCatalogTests.cs`

## § C. Drift Findings (Full)

### F-LANG-01 — `primitive-types.md` Status field stale [P1]
**Evidence**: `:10` "Designed — type checker implementation pending" — but 6 primitives ship in `Types.cs:297-362` with full operator coverage and sample use.
**Fix**: Update Status to `Implementation state: Implemented`.
**Effort**: S

### F-LANG-02 — `business-domain-types.md` Status field stale [P1]
**Evidence**: `:10` "Proposal — not yet implemented" — but `TypeKind.cs:28-34` (7 new kinds), `Types.cs:502-615` full metadata, `Operations.cs:422-697` full operators, `CurrencyCatalog.cs`, `Ucum/`, `Runtime/BusinessValues/MoneyValue.cs`, active in `samples/11-insurance-claim-adjudication.precept:32-36`.
**Fix**: Update Status to `Implemented (with documented gaps — see F-LANG-BIZ-*)`.
**Effort**: S

### F-LANG-03 — `temporal-type-system.md` doc maturity vs implementation state inconsistent [P2]
**Evidence**: `:7-10` Doc maturity = "Draft" but Implementation state = "Implemented in `src/Precept/Language/Time/`".
**Fix**: Promote Doc maturity to "Full"/"Locked", or add explicit § Open Questions section.
**Effort**: S

### F-LANG-PRIM-01 — Doc claims `string` is orderable; catalog says it isn't [P2]
**Evidence**: `primitive-types.md:87-90,447` claim ordinal `<`/`>`/`<=`/`>=` on `string`. `Types.cs:300-302` declares `string` with `EqualityComparable | ChoiceElement` — no `Orderable`. `Operations.cs` has no `StringLessThanString` etc.
**Fix**: Either remove ordering rows from doc, OR add `StringLessThan`/etc. + `TypeTrait.Orderable`.
**Effort**: S (doc) or M (catalog + tests)

### F-LANG-PRIM-02 — `maxplaces` "decimal-only" narrowing in doc is incorrect [P2]
**Evidence**: `primitive-types.md:231,520` "Only applicable to `decimal`". `Modifiers.cs:236-241` `BusinessMagnitudeTypes = [Decimal, Money, Quantity, Price, ExchangeRate]`.
**Fix**: Update doc to say "applies to decimal AND business-domain magnitude types."
**Effort**: S

### F-LANG-PRIM-03 — `min`/`max` cross-lane signature implied but not catalog-realized [P2]
**Evidence**: `primitive-types.md:549` row "(numeric, numeric) → numeric"; `Functions.cs:41-55` declares same-type overloads only.
**Fix**: Update doc to spell out homogeneous overloads + integer→decimal widening rescue.
**Effort**: S

### F-LANG-PRIM-04 — RedundantModifier (nonneg+positive) catalog mutex (error) vs doc warning [P2]
**Evidence**: `primitive-types.md:535` says warning; `Modifiers.cs:89,105` declares both directions `MutuallyExclusiveWith` (error semantics).
**Fix**: Audit `TypeChecker.Validation.Modifiers.cs` to confirm actual severity. Reconcile with doc.
**Effort**: S audit + S-M fix

### F-LANG-TEMP-01 — `'3 days'` cannot resolve to `Duration` in `instant` context [P0]
**Evidence**: `temporal-type-system.md:570-585,1138-1139` claim `instant ± '3 days'` → `Duration.FromDays(3)`. `TemporalUnits.cs:25` declares `day` with `PeriodFactory: Period.FromDays`, `DurationFactory: null`. `TemporalQuantityParser.cs:41-50` strictly dispatches by which factory non-null. `Operations.cs:322-332` instant supports only `instant ± duration`.
**Fix**: Either (a) give `day`/`week` both factories + context-aware parser, OR (b) revise doc — `instant + 'N days'` is type error, `instant + 'N hours'` is only timeline path.
**Effort**: M (a) or S (b)

### F-LANG-TEMP-02 — `'2 weeks'` cannot resolve to `Duration` in `instant` context [P0]
**Evidence**: Same as F-LANG-TEMP-01 for `weeks`. `TemporalUnits.cs:24` — `week` has `Period.FromWeeks`, null duration factory.
**Fix**: Bundled with F-LANG-TEMP-01.

### F-LANG-TEMP-03 — `nonzero`/`nonnegative` on `duration` field not in catalog applicability [P1]
**Evidence**: `temporal-type-system.md:699-703` declares ships + canonical proof source for `duration / duration`. `Modifiers.cs:16-21` `ZeroBoundNumericTypes = [Integer, Decimal, Number, Money, Quantity, Price, ExchangeRate]` — Duration absent.
**Fix**: Add `TypeKind.Duration` (+ Period) to `ZeroBoundNumericTypes`. Add type-aware desugar `Field != Duration.Zero`. Add tests.
**Effort**: M

### F-LANG-TEMP-04 — "Always-false literal period comparison" warning not implemented [P2]
**Evidence**: `temporal-type-system.md:760` promises warning on `1 month == 30 days`. No `AlwaysFalsePeriod`/similar code in `DiagnosticCode.cs` (188 codes inspected).
**Fix**: Add analyzer or drop promise from doc.
**Effort**: S (drop) or M (implement)

### F-LANG-TEMP-05 — Mixed-unit combined quantity rejected (`'1 day + 12 hours'`) [P1]
**Evidence**: `temporal-type-system.md:744-749` table maps `P1DT12H` to `'1 day + 12 hours'` as valid. `TemporalQuantityParser.cs:53-54` explicitly rejects: `TEMP005: "Temporal quantities cannot mix calendar units and time units."`
**Fix**: Either (a) relax parser when context expects `period`, OR (b) update doc to remove mixed example.
**Effort**: M (a) or S (b)

### F-LANG-TEMP-06 — Legacy timezone abbreviations (`EST`/etc.) not rejected with specific message [P2]
**Evidence**: `temporal-type-system.md:862-864` specific messages claimed. `TemporalParser.cs:113-119` delegates to `DateTimeZoneProviders.Tzdb.GetZoneOrNull` (generic message).
**Fix**: Add allowlist of common legacy abbreviations with targeted messages, OR drop specific abbreviation messages from doc.
**Effort**: S

### F-LANG-TEMP-07 — Windows timezone name (`'Pacific Standard Time'`) gets generic error [P2]
**Evidence**: Same as F-LANG-TEMP-06 for Windows names per `:865`.
**Fix**: Bundle with F-LANG-TEMP-06.

### F-LANG-TEMP-08 — `zoneddatetime ± period` in catalog despite doc saying compile error [P1]
**Evidence**: `temporal-type-system.md:912-915` says not supported. `OperationKind.cs:91-92` declares `ZonedDateTimePlusPeriod = 59`, `ZonedDateTimeMinusPeriod = 60`. `Operations.cs:396-402` declares valid.
**Fix**: Choose authoritative. NodaTime supports `ZonedDateTime + Duration` not Period directly — verify runtime path. Remove from catalog OR remove from doc.
**Effort**: S (doc) or M (catalog removal + diagnostic + tests)

### F-LANG-BIZ-01 — `money / price → quantity` cancellation not in catalog [P2]
**Evidence**: `business-domain-types.md` § Compound Types (L1049-1055) discusses inverse division. `Operations.cs` has `MoneyDivideMoney/Quantity/Period/Duration` but NO `MoneyDividePrice`.
**Fix**: Add `MoneyDividePrice → Quantity` with qualifier-chain proof. Update doc tables.
**Effort**: M

### F-LANG-BIZ-02 — ISO 4217 implicit `maxplaces` (D10) not implemented [P1]
**Evidence**: `business-domain-types.md:481-482,86` (D10) declares implicit precision from `MinorUnit`. `MoneyValue.cs:7` comment acknowledges. No `GetImpliedMaxplaces` / dispatch code exists.
**Fix**: Implement maxplaces injection at typed-constant validator. Look up `CurrencyCatalog.All[code].MinorUnit`. Apply as bound when no explicit `maxplaces`. Add tests for JPY (0), USD (2), BHD (3).
**Effort**: M

### F-LANG-BIZ-03 — `currency` accessors `.name`, `.minorUnit`, `.numericCode`, `.symbol` not in catalog [P1]
**Evidence**: `business-domain-types.md:524-530` lists 4 accessors. `Types.cs:520-530` declares Currency meta with **no `Accessors` array**.
**Fix**: Add 4 accessors to Currency meta. Wire to runtime values from `CurrencyEntry`. Add `TypeCatalogTests` coverage.
**Effort**: M

### F-LANG-BIZ-04 — `CurrencyCatalog` public API surface differs entirely from doc spec [P1]
**Evidence**: `business-domain-types.md:546-569` specifies `Default` singleton, `Get`, `TryGet`, `GetByNumericCode`, `TryGetByNumericCode`, `IsValid`, `All` as `IReadOnlyList<Currency>`, `DataVersion`. Real `CurrencyCatalog.cs:82-84` exposes only `static FrozenDictionary<string, CurrencyEntry> All`.
**Fix**: Build documented API on top of existing `All` dictionary (small wrapper methods).
**Effort**: S

### F-LANG-BIZ-05 — Quantity × quantity cancellation depends on unverified ResultQualifierPolicy [P2]
**Evidence**: `Operations.cs:578-581` declares `QuantityTimesQuantity` unconditionally with `CompoundUnitCancellation` policy. Doc `:634` says simple×simple rejected.
**Fix**: Add test asserting `kg * m` (no cancellation) produces compile diagnostic. Fix if needed.
**Effort**: S (test) + M (fix if needed)

### F-LANG-BIZ-06 — `exchangerate` implicit `positive` constraint not in catalog [P2]
**Evidence**: `business-domain-types.md:973` "Implicit constraint: positive". `Types.cs:599-615` no `ImpliedModifiers`. Compare to Currency/Timezone/UnitOfMeasure/Dimension at `:454,525,556,572` which have `ImpliedModifiers: [Notempty]`.
**Fix**: Add `ImpliedModifiers: [Positive]` to ExchangeRate in `Types.cs`.
**Effort**: S

### F-LANG-BIZ-07 — Composite period basis `'years&months'` (`&` separator, D4) not implemented [P1]
**Evidence**: `business-domain-types.md:1262-1269` table maps `'years&months'` → `Years | Months`. `TemporalQuantityParser.cs:16` splits on `+` only, no `&` handling.
**Fix**: Implement `&` separator in temporal-unit qualifier value parsing. Map to bitfield of NodaTime `PeriodUnits` flags. Update `Period.Between` lowering. Add tests.
**Effort**: M-L

### F-LANG-BIZ-08 — Discrete equality narrowing (D9) infrastructure unverified [P1]
**Evidence**: `business-domain-types.md:1145-1238` claims `$eq:X.currency:USD` markers injected via same pipeline as null-flow narrowing (#106). `TypeChecker.Expressions.AssignmentQualifiers.cs` exists; specific marker injection/consumption not directly inspected.
**Fix**: Audit AssignmentQualifiers.cs against doc § Discrete Equality Narrowing examples (L1190-1225). Add integration tests for all 3 patterns.
**Effort**: M

### F-LANG-BIZ-09 — Entity-scoped units (`units` block) doesn't exist as construct [P2]
**Evidence**: `business-domain-types.md:242-282,1832-1842` implies `units` block. Searching `Parser.cs`, `Constructs.cs`, construct catalog: no `units` block construct. UCUM covers `each`/`case` etc.
**Fix**: Either (a) drop `units` block aspirational claim ("Future — not in v1 scope"), OR (b) build construct.
**Effort**: S (doc) or L (build)

## § D. Implementation-Only Features

1. `abs(money) → money` and `abs(quantity) → quantity` — `Functions.cs:82-83`. Doc says abs is numeric-only.
2. `min(money, money)`, `max(money, money)`, `min(quantity, quantity)`, `max(quantity, quantity)` — `Functions.cs:49-50,65-66`.
3. `clamp(money, money, money)` and `clamp(quantity, quantity, quantity)` — `Functions.cs:99-100`.
4. `round(money, places) → money` and `round(quantity, places) → quantity` — `Functions.cs:163-164`.
5. `pow(decimal, integer)` and `pow(number, integer)` — `Functions.cs:192-193`.
6. `min`/`max` interval transfer lambdas — `Functions.cs:44-48`.
7. `Maxplaces` applies to Decimal/Money/Quantity/Price/ExchangeRate — `Modifiers.cs:30-34,236-241`.
8. `Min`/`Max` modifier applies to Money/Quantity/Price (not ExchangeRate).
9. `Mincount`/`Maxcount` modifiers for all collection types.
10. `Writable` modifier — `Modifiers.cs:243-247`.
11. `Now` is the only freestanding temporal function.
12. `ChoiceElementType` trait — declared on string/boolean/integer/decimal/number.
13. `UnitDimensionHelper.cs` + `Ucum/` namespace — full UCUM grammar parser ships.
14. `PeriodDimension` enum — `ProofRequirement.cs:66`.
15. `InvalidTimezoneId` diagnostic — `Diagnostics.cs:534`.

## § E. Confidence Statement

- **`primitive-types.md` — High confidence (8/10).** Maps cleanly to catalog. Drift in Status field + ordering claims + `maxplaces` undersold + `min`/`max` cross-lane wording.
- **`temporal-type-system.md` — Medium confidence (6/10).** Eight types ship; timezone-mediation correct; period/duration split enforced. Three load-bearing claims unimplemented (F-LANG-TEMP-01/02/08 + F-LANG-TEMP-05 mixed quantities). Doc 1900 lines comprehensive but needs F-LANG-TEMP findings flagged.
- **`business-domain-types.md` — Lower confidence (5/10).** Seven types ship; full operator surface; qualifier shapes correct; Slice11B test coverage exists. But: Status grossly wrong, `CurrencyCatalog` API absent, currency accessors missing, D10 implicit maxplaces unimplemented, D4 composite basis unimplemented, exchangerate implicit positive missing, compound-algebra inverse missing, `units` block doesn't exist.

**Cross-cutting recommendation**: Status corrections (F-LANG-01/02/03) ship in single doc-only pass. Catalog gaps (F-LANG-BIZ-03/04/06, F-LANG-TEMP-03) are small wins. F-LANG-TEMP-01/02 + F-LANG-BIZ-07 are largest design-vs-implementation decisions — warrant owner sign-off.
