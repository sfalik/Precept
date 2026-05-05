### 2026-05-05T00:32Z: LOCKED — Evaluator-only computation (Option B); CLR types are pure data records

**By:** Frank

**Status:** Accepted. Supersedes frank-computation-locality and frank-operator-overloads.

**Merged source:** `frank-evaluator-vs-clr-computation.md`.

- **CLR types (`Money`, `Price`, `ExchangeRate`, `Quantity`) are pure data records.** No operators, no arithmetic methods, no validation logic. `ToString()`, `Parse()`, construction only.
- **Computation lives in named executor modules** (`MoneyOperations`, `QuantityOperations`, `PriceOperations`, `ExchangeRateOperations`) as `internal static` classes in `src/Precept/Runtime/Operations/`. Each method corresponds to one `OperationKind`.
- **Why not Option A:** creates structural duplication of domain rules — same-currency, same-unit, D15 boundary, D16 exception table — enforced in both CLR type operators AND the evaluator pipeline. Seven rules with two enforcement points; must stay manually in sync.
- **Under Option B:** type checker reads `ProofRequirements` from `OperationMeta` (compile time); executor module method is the single runtime enforcement point. No third copy. Catalog-driven.
- **D8 auto-conversion:** lives in `QuantityOperations.Add()` — has full access to `UnitCatalog`. One path. Under Option A it required two paths (same-unit operator + named method with source).
- **Evaluator:** three-line indexer — resolve operation, index into `TypeRuntimeMeta.BinaryExecutors`, call. Zero domain logic.
- **The NodaTime analogy was wrong for this decision:** NodaTime carries computation because it has no runtime. Precept has a runtime. The analogy justifies the separate assembly; it does not require computation on the types.
- Open items: OQ-DISP-1 (Runtime-layer aggregation class name), OQ-DISP-2 (update evaluator.md dispatch code to use qualified name), OQ-DISP-3 (update catalog-system.md open question).
