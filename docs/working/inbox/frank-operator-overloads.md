### 2026-05-05T00:14Z: Operator overload surface analysis for business-domain types

**By:** Frank

**Status:** Analysis recorded — informs Option B design (evaluator-only computation).

**Merged source:** `frank-operator-overloads.md`.

- Three structural problems with naïve C# operator overloads: (1) operators cannot take extra parameters (`IUnitConversionSource` needed for D8); (2) same operand types, different return types (`Money / Money` → `decimal` or `ExchangeRate`); (3) commutative cross-type operators require declaration on both types.
- Per-type operator surfaces analyzed: `Money` (additive, scaling, ratio, comparisons, named `DeriveRate`/`DivideBy`); `ExchangeRate` (currency conversion `operator*`, scaling, named `Apply`/`Invert`); `Price` (dimensional cancellation `operator*`, scaling, additive); `Quantity` (same-unit operators, D8 as named method with source injection, compound division).
- `Quantity.operator+(Quantity, Quantity)` requires same unit; D8 auto-conversion is a named method (`AddSameDimension(other, source)`) — cannot be an `operator+` because of the required injection parameter.
- `Money / Money` → operator returns `decimal` (ratio, same currency); `ExchangeRate` derivation is a named method (`DeriveRate`) because C# cannot return two types from one operator signature.
- **This analysis was superseded by Option B.** Under the evaluator-only model, CLR types carry no operators; all arithmetic is in named executor modules. Operator overloads on `Precept.Types` types were explicitly not adopted.
