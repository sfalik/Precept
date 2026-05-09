# Gap Audit Tracker — Typed Literal System (Business Domain Types)

> **READ THIS FIRST at every session start before responding to any gap-related questions.**
> Update this file immediately whenever a status changes — do not rely on SQL or session context alone.
>
> **Branch:** `Precept-V2-Radical`
> **Scope:** money, currency, quantity, unitofmeasure, price, exchangerate, period
> **Last updated:** 2026-05-09

---

## Standing Rules (Shane directives — never violate)

- **No deferrals.** Q1–Q8 + Q6b are all real work items in priority order. Only Q9 and Q10 are sequenced (after Q1–Q8 complete).
- **Q9 (Language Server)** starts after Q1–Q8 are all resolved.
- **Q10 (Runtime evaluator)** starts after Q9 is complete.
- MCP tools policy: only `precept_language` and `precept_compile` are sanctioned until runtime is implemented. Do NOT use `precept_inspect`, `precept_fire`, or `precept_update`.

---

## Gap Status Board

| ID | Gap | Status | Blocked on |
|----|-----|--------|-----------|
| Q1 | UCUM Tier 1 ~150 atoms — stub has only 10 | **✅ done** | george-11: ~150 codes in `UcumCatalog.cs` + browse synthesis/fallback. Commits 03613099, 7c237f1b |
| Q2 | DimensionCatalog API cleanup (`AllAliases`→`All: FrozenDict`, drop `AllNames`) | ✅ **done** | — |
| Q3 | Parser qualifier extraction P0 — `TypeChecker.cs` hardcodes `DeclaredQualifiers: Empty`; parse inside `ParseTypeReference()` | ✅ **done** | — |
| Q4 | All domain catalogs → registry pattern + MCP surfacing | **✅ done** | george-10: TemporalUnits aligned; `precept_language` surfaces domains. Commits 17ee4b29, 26a96873, 16687b7a |
| Q5 | CLR value type shapes (Money/Quantity/Price/ExchangeRate) | ✅ **done** | — |
| Q6 | count/dimensionless proof engine investigation | ✅ **decided** | — (see Q6b for action item) |
| Q6b | TypeChecker uses `TryGetAlias(DimensionVector.None)` → always "count" for all dimensionless units (rad misclassified). Frank Option D: unit-aware lookup in qualifier derivation path | **✅ done** | george-13: unit-aware derivation in TypeChecker; rad/deg/sr no longer collapse to count. Commit 6de90732 |
| Q7 | Zero sample `.precept` files for money/quantity/price/exchangerate/period/currency | **pending** | george-9 complete |
| Q8 | No UCUM drift test (reference: `CurrencyCatalogSyncTests.cs`) | **✅ done** | soup-nazi-8: `UcumCatalogDriftTests.cs` written + 3732/3732 pass. Commit 01498826 |
| Q9 | Language Server entirely stubbed — `GetCompletions()` throws `NotImplementedException` | **sequenced** | Q1–Q8 + Q6b all done |
| Q10 | Runtime evaluator entirely stubbed — no execution path | **sequenced** | Q9 done |

---

## Decisions Log

### Q1 — UCUM Tier 1
- Curate proper ~150 list. ucum-research agent completed (2026-05-09).
- Results in temp file; George to write to `research/language/ucum-tier1-curation.md` and implement `Tier1Codes` in `UcumAtomCatalog.cs`.
- Key finding: remove `s` (time → NodaTime) and `mol` (scientific, not business). Keep remaining 8 from stub + ~142 new.

### Q2 — DimensionCatalog
- Keep `speed`, `force`, `count` — all intentional extras ratified by Shane.
- API cleanup: `AllAliases` → `All: FrozenDictionary`, drop `AllNames`.
- george-9 implementing.

### Q3 — Parser qualifier extraction (P0)
- Approach 1: parse inside `ParseTypeReference()`.
- New node: `QualifiedTypeReference` + `TryParseQualifiers()` driven by `QualifierShape.Slots`.
- TypeChecker populates `DeclaredQualifiers` from parsed qualifiers (removes hardcoded `Empty`).
- george-9 implementing.

### Q4 — Domain catalog registry pattern
- Align all domain catalogs (Currency, UCUM, Dimension, temporal) to standard pattern (static class + `All: FrozenDictionary`).
- Surface through `precept_language` MCP tool.
- Pending george-9.

### Q5 — CLR value type shapes
- Done. george-8 produced 7 pure data records.

### Q6 — count/dimensionless investigation
- Arithmetic opaqueness: false alarm. Proof engine compares `DeclaredQualifierMeta.Unit` by record equality (includes `UnitCode` string), NOT `DimensionVector`. `each != case` already enforced.
- Real gap captured in Q6b.

### Q6b — Option D (Frank's recommendation)
- In TypeChecker qualifier derivation: when unit resolves to `DimensionVector.None`, do NOT use `TryGetAlias` (always returns "count").
- Instead: check if unit code is a known counting unit → assign "count"; otherwise (e.g., `rad`, `sr`) → no category.
- Keep `count` in `DimensionCatalog` for forward lookup.
- File as separate GitHub issue after P0 ships. Can bundle as follow-on slice within P0 PR.

### Q7 — Sample files
- Write `.precept` samples demonstrating all 6 business domain types.
- Assign to George or dedicated agent after george-9 completes.

### Q8 — UCUM drift test
- Mirror `CurrencyCatalogSyncTests.cs` pattern.
- Natural dependency on Q1 (Tier1Codes populated).
- Assign to Soup Nazi after Q1 is implemented.

### Q9 — Language Server (sequenced)
- Real gap, not deferred. Starts after Q1–Q8 + Q6b all complete.

### Q10 — Runtime evaluator (sequenced)
- Real gap, not deferred. Phase 3. Starts after Q9 complete.

---

## Active Agents

| Agent | Task | Status |
|-------|------|--------|
| george-9 | P0 qualifier pipeline fix (Q3) + DimensionCatalog cleanup (Q2) | ✅ done — 3721/3721 tests pass |
| ucum-research | Curate UCUM Tier 1 ~150 list (Q1) | ✅ complete — results written to temp |
| george-10 | Q4: domain catalog registry pattern + MCP surfacing | ✅ done (commits 17ee4b29, 26a96873, 16687b7a) |
| george-11 | Q1 impl: write Tier1Codes to UcumCatalog.cs | ✅ done (commits 03613099, 7c237f1b) |

---

## Unblocking Chain

```
george-9 completes
  → Q4: domain catalog registry + MCP (spawn George)
  → Q7: sample .precept files (spawn George or writer)
  → Q6b: file GitHub issue, implement in TypeChecker

ucum-research complete (✅ done)
  → Q1: George writes Tier1Codes to UcumAtomCatalog.cs + research file
  → Q8: Soup Nazi writes UCUM drift test (after Q1 implemented)

All Q1–Q8 + Q6b done
  → Q9: Language Server implementation begins
  → Q9 done → Q10: Runtime evaluator begins
```
