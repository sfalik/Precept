## Core Context







- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.



- Catalog metadata remains the language truth; runtime, tooling, and docs derive from durable catalog shape rather than enum-identity switches or parallel lists.



- Interpolation work must preserve compile-time guarantees first; plans that trade structural certainty for runtime validation remain philosophically out of bounds.



- Proof and qualifier fixes should stay bounded to catalog metadata and one-hop semantic traces rather than speculative provenance systems.







## Live Guidance







- String holes remain out of scope for interpolated typed constants; typed-hole composition is the only acceptable path.



- Slice 6 stays numeric-only, including the single-hole whole-value fallback; qualifier, dimension, modifier, and presence obligations remain declaration-driven.



- Temporal semantics stay with `duration` / `period`; `quantity of 'time'` remains invalid, while temporal-denominated prices stay on `price of ...`.



- MCP/public tooling contracts should continue to expose curated projections rather than raw core catalog records.







## Learnings



### 2026-05-11T23:01:00-04:00 — Slice 2B audit: DONE

- Shane suspected Slice 2B ("compound-unit interpolation") was already implemented despite the plan marking it "🔲 Not Started." Verified: **fully implemented.**

- All P1–P8 (price), X1–X8 (exchangerate), Q5–Q8 (quantity compound), U2–U4 (unitofmeasure compound), and D5–D7/Pe5–Pe7 (temporal compound) forms are present in `TypeChecker.Expressions.TypedConstants.cs`.

- `InterpolationSlotKind.NumeratorUnit`/`DenominatorUnit` exist in `SemanticIndex.cs`. Diagnostic codes 121–123 are all wired.

- 17 dedicated compound-unit tests exist; all 107 typed constant tests pass.

- The plan's status line is stale — this work shipped during the RC-2 fix cycle. Decision written to `.squad/decisions/inbox/frank-2b-audit.md`.







### 2026-05-11T22:53:58-04:00 — Part D test failure fix design (B1–B4) written

- Designed 4 slices (D1–D4) for 30 pre-existing test failures diagnosed by Soup Nazi.

- B1 (24 failures): `optional notempty` on event args in `FullPrecept` and `LoanApplication` fixtures. Modifier validation is correct — fixtures updated to drop `notempty`. Validation NOT weakened.

- B2 (3 failures): Exchange rate tests used `from 'USD' to 'EUR'` but canonical syntax is `in 'USD' to 'EUR'` per `QS_ExchangeRate` shape. Tests fixed to use `in`. No parser change. No interaction with Rec 2 (`.from`/`.to` member access).

- B3 (1 failure): VS Code `package.json` missing `"onLanguage:precept"` in `activationEvents`. One-line JSON addition. Owned by Kramer.

- B4 (1 failure): MCP syntax reference "Money and quantity typed fields" example uses `default '0.00 USD/kg'` — compound-unit form now rejected by RC-2. Fix: remove default from `UnitPrice`. Restorable after Slice 2B. Owned by Newman.

- All 4 slices are independent, no dependencies on Parts A/B/C.

- Decision doc written to `.squad/decisions/inbox/frank-b-slices-design.md`.



### 2026-05-11T21:54:11-04:00 — Deep dive: inventory-item.precept root cause analysis



- Identified 3 root causes for 161 errors (2 compiler, 1 sample design); rest is cascade.



- **RC-1 (Parser):** `Parser.TryParseQualifiers()` only accepts `TokenKind.TypedConstant`, rejects `TypedConstantStart`. This gates ALL interpolated qualifiers in field/arg declarations. ~100 cascade errors trace here.



- **RC-2 (TypeChecker):** Missing compound-unit patterns Q6/Q7/Q8 (`'0 {A}/{B}'` forms) in `QuantityForms[]`. Q5 requires 3 holes but sample uses 2-hole patterns.



- **Sample bugs:** `is set` on non-optional Sku (PRE0049), money/price type mismatch in cost comparison (PRE0018), unguarded division by zero (PRE0083).



- **A2B visibility in this file:** ZERO — every benefiting line is blocked by RC-1.



- Updated sample file header with new bug classification; wrote full analysis to `.squad/decisions/inbox/frank-inventory-deep-dive.md`.







### 2026-05-11T21:05:25-04:00 — inventory-item.precept coverage analysis



- Compiled `samples/inventory-item.precept` via `precept_compile`: 125 diagnostics, ~73% are BUG-C or direct cascades from BUG-C (failed interpolation → failed arg parsing → cascade "not declared" errors).



- **Plan gap found:** compound-unit interpolation (`'{StockingUnit}/{PurchaseUnit}'`) is not covered by any per-type grammar in the plan. The `unitofmeasure` type only defines `U1: H[whole-value]`; quantity Q1–Q4 have no compound-unit patterns. This blocks 4 field declarations and 2 rules.



- BUG-B is indirectly covered by Part A + Slice 9 axis fallback (Unit→Dimension). No separate slice needed.



- BUG-A cannot be distinguished from BUG-C cascades in this file — all event args use interpolated qualifiers. Once BUG-C ships, Slice 10 (assignment expression qualifier propagation) should handle the remaining scenarios, but explicit test coverage for args-in-expressions is recommended.



- Sample file has its own design issues: `SupplierUnitCost` is declared `money` but used as `price` (no `MoneyTimesQuantity` operation exists); `Sku is set` on non-optional field; division by zero on `TotalInventoryCost / QuantityOnHand`.



- Compound-unit interpolation needs a new slice (Slice 2B or Slice 2 extension) with patterns for `unitofmeasure` and `quantity` compound forms.







## Historical Summary



- Detailed 2026-05-11 research chronology, proof-plan audits, interpolation follow-ons, and temporal-design analysis were compacted into `history-archive.md` during the 2026-05-12T00:50:06Z summarization pass.



## Recent Updates



### 2026-05-12T03:33:33Z — D4 (reframed) scalar-op qualifier propagation design complete
- Designed the fix for 6 scalar operations (`MoneyTimesDecimal`, `MoneyDivideDecimal`, `QuantityTimesDecimal`, `QuantityDivideDecimal`, `PriceTimesDecimal`, `PriceDivideDecimal`) that silently drop qualifiers.
- **Naming ruling:** Kept as D4, not C5. Part C is "inventory-item fixes" — this fix does NOT affect inventory-item (zero scalar-decimal ops in that file; all 66 remaining PRE0114 are BUG-A/C4). Part D is "test failure fixes" — this fixes the `SyntaxReferenceMirrorsSourceAndExamplesCompile` test.
- **Design:** New `ResultQualifierPolicy.InheritFromQualifiedOperand` → new `QualifiedOperandInherited` QualifierBinding subtype → `MapQualifierBinding` branch → `ResolveQualifierOnAxis` transitive resolution through `TypedBinaryOp` subjects.
- **Bonus fix:** Transitive resolution also handles `SameQualifierRequired` on nested binary ops (e.g., `(MoneyA + MoneyB) - MoneyC`), which was previously broken for the same reason.
- **No SyntaxReference modification.** The example stays as-is; the compiler fix makes it compile correctly.
- Decision doc: `.squad/decisions/inbox/frank-scalar-op-qualifier-design.md`

### 2026-05-12T03:35:43Z — D4 reframed to scalar-op qualifier propagation

- The `default '0.00 USD/kg'` premise is closed as a false attribution: the runtime already accepts compound-unit price defaults, so the real failure is qualifier preservation in scalar operations like `money * decimal` and `money / decimal`.

- `frank-5` remains in progress designing the metadata-aligned propagation fix; keep the syntax-reference example and failing MCP test pointed at the real runtime bug rather than simplifying the example away.





### 2026-05-12T02:53:58Z — Part C inventory-item compile fixes designed (C1–C4)

- Designed 4 slices for Part C of the typed constants plan: C1 (dimension cancellation, George in-flight), C2 (keyword-as-member-name `.from`/`.to`), C3 (compound boolean `=` sample fix + diagnostic), C4 (BUG-A proof engine arg qualifier resolution).

- **C2 root cause:** Circular dependency between `TokenMeta.IsValidAsMemberName` (computed property → `Tokens.KeywordsValidAsMemberName`) and `Parser.KeywordsValidAsMemberName` (filters by `TokenMeta.IsValidAsMemberName`). Fix: derive parser set directly from `Types.All` accessor names, breaking the cycle.

- **C3 root cause:** Sample file bug — `=` is assignment-only, `==` is comparison. Parser correctly rejects `=` in expression context. Added `AssignmentInExpressionContext` diagnostic for usability.

- **C4 root cause (BUG-A):** `ResolveQualifierOnAxis()` only resolves field qualifiers via semantic index. `TypedArgRef` carries qualifiers directly but `GetFieldName()` returns null for it. Fix: direct qualifier extraction from `TypedArgRef` node, mirroring existing `ResolveSourceModifiers()` pattern. Symbolic equality uses template string identity — structural, not semantic.

- Decision doc: `.squad/decisions/inbox/frank-inventory-c-slices.md`



### 2026-05-12T00:50:06Z — Q2 derivation inference ruling recorded

- D19 is now locked in `docs/language/business-domain-types.md`: derivation operations do not infer qualifiers onto resulting `price` values.

- Authors must declare `of 'time'` / `of 'date'` explicitly on target price fields when temporal denomination matters.



### 2026-05-12T00:50:06Z — Temporal proof-plan audit reconciled

- Canonical-doc review confirmed Slice 11B/12 stays additive to locked docs and that G15 is a false gap.

- Durable status correction: Slices 7–11 are already implemented; only Slice 11B and Slice 12 remain open.



### 2026-05-11T20:25:57Z — MCP projection contract direction preserved

- Raw catalog serialization stays off the public MCP contract path; the curated projection layer remains the durable direction.



### 2026-05-11T20:03:33Z — Slice 6 boundary held

- Slice 6 remains numeric-only, with the single-hole whole-value fallback included and compile-time guarantees preserved.



### 2026-05-12T01:54:11Z — inventory-item deep dive follow-up carried forward

- RC-1: `Parser.TryParseQualifiers()` is the gating blocker for interpolated qualifier positions because it rejects `TypedConstantStart` in field/arg qualifier slots.

- RC-2: `QuantityForms[]` still lacks Q6/Q7/Q8 coverage for `'0 {A}/{B}'`-style compound-unit bounds, so A2B remains incomplete for this file's rule shapes.

- A2B visibility in `samples/inventory-item.precept` is effectively zero until RC-1 lands, because the relevant declarations fail before type-check pattern matching can help.



### 2026-05-12T02:12:11Z — TypeChecker expression split analysis recorded

- The 104 KB `TypeChecker.Expressions.cs` file was analyzed end-to-end and the recommended execution path is a 3-way partial split: Core, Callables, and TypedConstants.

- Recommended relocations are intentionally surgical: move `IsAssignable` into the Core tail and move `TryContextRetryOverload` beside `SelectOverload` in the Callables partial.

- The analysis explicitly treats George RC-1 / RC-2 as already committed work and calls the split safe to execute once any live background edits are confirmed idle.

## Recent Updates

### 2026-05-12T04:29:05Z — Inventory-item PRE0114 plan recorded
- The inventory-item PRE0114 analysis and Part E plan were folded into the canonical decision ledger.
- RC-1 through RC-4 remain the durable root-cause map, with the sample grouping bug called out as separate source fallout.
