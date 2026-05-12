# Complete Sample Error Inventory

**Date:** 2026-05-12T01:17:00-04:00
**Author:** Frank (Lead/Architect)
**Scope:** All 30 `.precept` files in `samples/`

---

## Summary

- **30 sample files** total
- **20 clean** — zero diagnostics
- **10 with errors** — 46 diagnostics total across 5 distinct diagnostic codes

---

## Clean Files (20)

building-access-badge-request, clinic-appointment-scheduling, computed-tax-net, crosswalk-signal, customer-profile, event-registration, fee-schedule, invoice-line-item, library-hold-request, parcel-locker-pickup, payment-method, restaurant-waitlist, subscription-cancellation-retention, sum-on-rhs-rule, Test, trafficlight, transitive-ordering, utility-outage-report, vehicle-service-appointment, warranty-repair-request

---

## Error Inventory by File

### 1. apartment-rental-application.precept — 2 errors

| Line | Code | Diagnostic | Root Cause |
|------|------|-----------|-----------|
| 30 | PRE0114 | `MonthlyIncome > '0.00 USD'` — incompatible Currency qualifiers | F3: Static TC qualifier extraction |
| 31 | PRE0114 | `RequestedRent > '0.00 USD'` — incompatible Currency qualifiers | F3: Static TC qualifier extraction |

**Context:** Fields declared `money in 'USD'`, compared against static typed constant `'0.00 USD'`. Proof engine cannot extract the `USD` qualifier from the `TypedLiteral` node.

### 2. hiring-pipeline.precept — 1 error

| Line | Code | Diagnostic | Root Cause |
|------|------|-----------|-----------|
| 26 | PRE0114 | `OfferAmount > '0.00 USD'` — incompatible Currency qualifiers | F3: Static TC qualifier extraction |

**Context:** `OfferAmount as money in 'USD'` vs `'0.00 USD'`. Same root cause as apartment-rental.

### 3. insurance-claim.precept — 1 error

| Line | Code | Diagnostic | Root Cause |
|------|------|-----------|-----------|
| 29 | PRE0114 | `ApprovedAmount > '0.00 USD'` — incompatible Currency qualifiers | F3: Static TC qualifier extraction |

**Context:** `ApprovedAmount as money in 'USD'` vs `'0.00 USD'`. Same root cause.

### 4. inventory-item.precept — 27 errors

| Line | Code | Diagnostic | Root Cause |
|------|------|-----------|-----------|
| 108 | PRE0114 | GrossProfit computed field — `<expression>` vs `TotalShrinkage` incompatible Currency | F4+: Exchange rate / interpolated qualifier chain |
| 108 | PRE0114 | GrossProfit computed field — `<expression>` vs `TotalCostOfGoods` incompatible Currency | F4+: Exchange rate / interpolated qualifier chain |
| 127 | PRE0114 | `StockingUnitsPerPurchaseUnit > '0 {StockingUnit}/{PurchaseUnit}'` — incompatible Unit | F4+: Compound interpolated TC qualifier |
| 128 | PRE0114 | `StockingUnitsPerSaleUnit > '0 {StockingUnit}/{SaleUnit}'` — incompatible Unit | F4+: Compound interpolated TC qualifier |
| 144 | PRE0018 | `ListPrice / StockingUnitsPerSaleUnit >= AverageCost` — expected price, got quantity | F4+: Dimension cancellation with interpolated qualifiers |
| 151 | PRE0018 | Same ensure in LowStock state | F4+: Same |
| 225 | PRE0068 | `TotalInventoryCost = ...Rate * (...)` — qualifier mismatch on `'{CatalogCurrency}'` | F4: ExchangeRateTimesMoney result qualifier |
| 225 | PRE0114 | TotalInventoryCost vs `<expression>` — incompatible Currency | F4: ExchangeRate result qualifier |
| 225 | PRE0114 | Rate vs `<unknown>` — incompatible FromCurrency↔Currency | F4: ExchangeRate chain proof |
| 227 | PRE0068 | `AverageCost = (...) / (...)` — qualifier mismatch on `'{CatalogCurrency}'` | F4: Cascading from exchange rate |
| 227 | PRE0083 | Division by zero — denominator can be zero | F4+: Cascading (proof engine can't verify qty > 0) |
| 227 | PRE0114 | TotalInventoryCost vs `<expression>` — incompatible Currency | F4: Cascading |
| 227 | PRE0114 | Rate vs `<unknown>` — incompatible FromCurrency↔Currency | F4: Chain proof |
| 231 | PRE0068 | Same as L225, LowStock→Listed transition row | F4: Same |
| 231 | PRE0114 | Same as L225 | F4: Same |
| 231 | PRE0114 | Same as L225 | F4: Same |
| 233 | PRE0068 | Same as L227, LowStock→Listed transition row | F4: Same |
| 233 | PRE0083 | Division by zero — same pattern | F4+: Same |
| 233 | PRE0114 | Same as L227 | F4: Same |
| 233 | PRE0114 | Same as L227 | F4: Same |
| 236 | PRE0068 | Same as L225, LowStock catch-all row | F4: Same |
| 236 | PRE0114 | Same as L225 | F4: Same |
| 236 | PRE0114 | Same as L225 | F4: Same |
| 238 | PRE0068 | Same as L227, LowStock catch-all row | F4: Same |
| 238 | PRE0083 | Division by zero — same pattern | F4+: Same |
| 238 | PRE0114 | Same as L227 | F4: Same |
| 238 | PRE0114 | Same as L227 | F4: Same |

**Context:** The exchange rate multiplication path (`Rate * (SupplierUnitCost * (PurchaseQty * StockingUnitsPerPurchaseUnit))`) cascades through 3 ReceiveShipment transition rows (Listed, LowStock→Listed, LowStock catch-all). The 27 errors reduce to ~5 root causes with 3× duplication per transition row.

### 5. it-helpdesk-ticket.precept — 1 error

| Line | Code | Diagnostic | Root Cause |
|------|------|-----------|-----------|
| 32 | PRE0120 | `optional notempty` on event arg `Note` | F1: Sample bug |

### 6. library-book-checkout.precept — 2 errors

| Line | Code | Diagnostic | Root Cause |
|------|------|-----------|-----------|
| 46 | PRE0120 | `optional notempty` on event arg `Condition` | F1: Sample bug |
| 47 | PRE0120 | `optional notempty` on another event arg | F1: Sample bug |

### 7. loan-application.precept — 6 errors

| Line | Code | Diagnostic | Root Cause |
|------|------|-----------|-----------|
| 28 | PRE0114 | `ApprovedAmount > '0.00 USD'` — incompatible Currency | F3: Static TC qualifier extraction |
| 38 | PRE0114 | `Submit.Amount > '0.00 USD'` — incompatible Currency | F3: Static TC qualifier extraction |
| 40 | PRE0114 | `Submit.Income >= '0.00 USD'` — incompatible Currency | F3: Static TC qualifier extraction |
| 41 | PRE0114 | `Submit.Debt >= '0.00 USD'` — incompatible Currency | F3: Static TC qualifier extraction |
| 44 | PRE0120 | `optional notempty` on event arg `Note` | F1: Sample bug |
| 45 | PRE0114 | `Approve.Amount > '0.00 USD'` — incompatible Currency | F3: Static TC qualifier extraction |

### 8. maintenance-work-order.precept — 1 error

| Line | Code | Diagnostic | Root Cause |
|------|------|-----------|-----------|
| 53 | PRE0120 | `optional notempty` on event arg `Reason` | F1: Sample bug |

### 9. refund-request.precept — 2 errors

| Line | Code | Diagnostic | Root Cause |
|------|------|-----------|-----------|
| 32 | PRE0120 | `optional notempty` on event arg `Note` | F1: Sample bug |
| 37 | PRE0120 | `optional notempty` on event arg `Note` | F1: Sample bug |

### 10. travel-reimbursement.precept — 3 errors

| Line | Code | Diagnostic | Root Cause |
|------|------|-----------|-----------|
| 40 | PRE0120 | `optional notempty` on event arg `Note` | F1: Sample bug |
| 50 | PRE0018 | `set LodgingTotal = Submit.Lodging` — expected decimal, got number | F2: Sample type mismatch |
| 51 | PRE0018 | `set MealsTotal = Submit.Meals` — expected decimal, got number | F2: Sample type mismatch |

---

## Error Summary by Diagnostic Code

| Code | Name | Count | Files | Root Cause Category |
|------|------|-------|-------|-------------------|
| PRE0120 | ConflictingModifiers | 8 | 6 files | Sample bug: `optional notempty` |
| PRE0114 | UnprovedQualifierCompatibility | 25 | 5 files | Compiler: 9 static TC + 16 inventory-item |
| PRE0068 | QualifierMismatch | 6 | 1 file | Compiler: exchange rate result qualifier |
| PRE0018 | TypeMismatch | 4 | 2 files | Mixed: 2 sample (decimal/number) + 2 compiler |
| PRE0083 | DivisionByZero | 3 | 1 file | Compiler: cascading from qualifier resolution |
| **Total** | | **46** | **10 files** | |

---

## Error Summary by Root Cause

| Root Cause | Fix Type | Diagnostic Count | Sample Count | Effort |
|-----------|---------|------------------|--------------|--------|
| F1: `optional notempty` on event args | Sample fix | 8 | 6 | Small |
| F2: `number` → `decimal` type mismatch | Sample fix | 2 | 1 | Small |
| F3: Static typed constant qualifier extraction | Compiler fix | 9 | 4 | Medium |
| F4: ExchangeRateTimesMoney result qualifier + chain | Compiler fix | 27 (inventory-item) | 1 | Large |
| **Total** | | **46** | **10** | |
