# Decision Register

Tracks all locked design decisions. Decisions move from `inbox/` → `accepted/` when Shane confirms. Open questions within accepted decisions are tracked here until resolved.

| ID | Decision | Status | Date | File |
|----|----------|--------|------|------|
| OQ-Currency | `sealed class Currency`, frank-114, ISO 4217 catalog model | Locked | 2026-05-04 | [accepted/frank-currency-type-design.md](accepted/frank-currency-type-design.md) |

## Open Questions Within Accepted Decisions

| OQ ID | Parent Decision | Question | Frank's Recommendation | Status |
|-------|----------------|----------|----------------------|--------|
| OQ-CUR-1 | OQ-Currency | Include curated `symbol` supplement (not in ISO 4217 spec)? | Option A — include | ⏳ Open — Shane has not responded |
| OQ-CUR-2 | OQ-Currency | Upgrade `Money.Currency`, `Price.Currency`, `ExchangeRate.From`/`.To` from `string` to `Currency`? | Option A — upgrade | ✅ Presumed agreed (no breaking change; structural consistency) |
| OQ-CUR-3 | OQ-Currency | Support both `Get<Currency>()` and `Get<string>()` on `currency`-typed fields? | Option A — both | ⏳ Open |
| OQ-CUR-4 | OQ-Currency | Embedded resource vs. separate `Precept.Currencies` data package? | Option A — embedded (v1) | ⏳ Open — Shane has not responded |
