# George H1/H2 Done

Date: 2026-05-12

## Summary
- H1 fixed the proof-engine currency-axis bug in `src/Precept/Pipeline/ProofEngine.cs` by translating `CurrencyConversionRequired` results from `ToCurrency` to `Currency` when nested expressions ask for the Currency axis.
- H1 also closed the `ExtractQualifierSourcePath()` fallback gap by adding `FromCurrency` and `ToCurrency` handling.
- H2 updated `samples/inventory-item.precept` so `ReceiveShipment.Rate` is declared as `exchangerate in '{SupplierCurrency}' to '{CatalogCurrency}'` and removed the redundant runtime `Rate.from` / `Rate.to` ensures.

## Diagnostics cleared
- ReceiveShipment PRE0114 sites at lines 212, 214, 218, 220, 223, and 225 are cleared.
- Current sample compile now leaves only PRE0083 at lines 214, 220, and 225.

## Remaining open items
- G2 still needs the denominator proof for `(QuantityOnHand + PurchaseQty * StockingUnitsPerPurchaseUnit)`.
- The MCP `precept_compile` surface did not reflect the updated runtime during this session, so final verification used the built `Precept.dll` directly.