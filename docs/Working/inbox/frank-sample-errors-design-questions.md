# Design Questions — Sample Completeness Fixes

**Date:** 2026-05-12T01:17:00-04:00
**Author:** Frank (Lead/Architect)
**Context:** All-samples error analysis, F-series design

---

## Q1: ExchangeRateTimesMoney Result Qualifier Policy

### The error(s)

All 27 remaining `inventory-item.precept` errors (6 QualifierMismatch, 16 UnprovedQualifierCompatibility, 3 DivisionByZero, 2 TypeMismatch) trace to the exchange rate multiplication path:

```
TotalInventoryCost = TotalInventoryCost + ReceiveShipment.Rate * (...)
```

The `ExchangeRateTimesMoney` operation is already registered in the Operations catalog (`Operations.cs:659`), but it has **no `ResultQualifierPolicy`**. The proof engine cannot determine what currency the result money carries after multiplying by an exchange rate.

### What the spec says

The Operations catalog declares:
- `ExchangeRateTimesMoney`: `exchangerate × money → money`
- ProofRequirement: `exchangerate.FromCurrency == money.Currency` (the source money must be denominated in the exchange rate's "from" currency)

The catalog does NOT declare what qualifier the result money inherits. Logically, `exchangerate(from, to) × money(from) → money(to)` — the result is denominated in the exchange rate's "to" currency.

### Why existing policies don't work

- `InheritFromQualifiedOperand` — picks qualifiers from the operand with the same result type. Both operands could contribute (money carries currency, exchangerate carries from/to). And the result currency is the exchangerate's *to* currency, NOT the money operand's currency. This policy would give the wrong answer.
- `CompoundUnitCancellation` — designed for dimension/unit cancellation in multiplication/division. Not applicable to cross-axis currency conversion.

### Options

**Option A: New `ResultQualifierPolicy.CurrencyConversion`**

Add a dedicated policy that declares: "Result currency = exchangerate's ToCurrency axis." This is the most explicit encoding.

- **Pro:** Semantically precise. The catalog entry for `ExchangeRateTimesMoney` becomes self-documenting.
- **Pro:** The proof engine gets a clear dispatch path — `case CurrencyConversion` → resolve `ToCurrency` from the exchangerate operand.
- **Con:** A new `QualifierBinding` subtype and proof engine branch for a single operation.

**Option B: Metadata on the `BinaryOperationMeta` — `ResultQualifierSource` record**

Instead of a new policy enum, add a `ResultQualifierSource` to the operation metadata: `new ResultQualifierSource(ParamSubject.ExchangeRate, QualifierAxis.ToCurrency, QualifierAxis.Currency)` — meaning "the result's Currency qualifier is inherited from the ExchangeRate operand's ToCurrency axis."

- **Pro:** General-purpose — works for any future operation where the result qualifier comes from a cross-axis source.
- **Pro:** Metadata-driven — no per-operation switch in the proof engine.
- **Con:** More infrastructure. Need to define the `ResultQualifierSource` record, teach `MapQualifierBinding` to use it, and wire it into `ResolveQualifierFromExpression`.

**Option C: Hardcoded proof engine branch for `ExchangeRateTimesMoney`**

Just switch on `OperationKind.ExchangeRateTimesMoney` in the proof engine and resolve `ToCurrency` directly.

- **Pro:** Fastest to implement. No new types.
- **Con:** Violates catalog-driven architecture. Domain knowledge in pipeline stage. Exactly the anti-pattern we don't allow.

### Recommendation

**Option A.** It's the smallest change that preserves catalog authority. `CurrencyConversion` is a genuine third qualifier-propagation semantic (after `SameQualifierRequired` and `CompoundUnitCancellation`) — it deserves its own discriminator. The single-operation argument is not a concern: the catalog describes what exists, not what might exist.

Option B is more general but over-engineered for the current surface. If we need cross-axis qualifier sources for other operations later, we can generalize at that time. Option C is not an option — period.

### What Shane needs to decide

1. **Approve Option A** (new `CurrencyConversion` policy), **or Option B** (general `ResultQualifierSource`)?
2. Is there a third semantic I'm missing that would make Option B's generality pay for itself today?

---

## Q2: `TypedLiteral` Qualifier Propagation — Node Shape Decision

### The error(s)

9 PRE0114 errors across 4 non-inventory samples (apartment-rental-application, hiring-pipeline, insurance-claim, loan-application) — all from `money_field > '0.00 USD'` comparisons where the proof engine cannot extract the `USD` qualifier from the static typed constant.

### Why this is a question

The fix requires `TypedLiteral` (the semantic-index node for static typed constants) to carry qualifier information. Currently `TypedLiteral` has only `ResultType`, `Value`, and `Span` — no qualifier data. Two options for the node shape:

**Option A: Add `DeclaredQualifiers` property to `TypedLiteral`**

```csharp
public sealed record TypedLiteral(
    TypeKind ResultType,
    object? Value,
    ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers,
    SourceSpan Span
) : TypedExpression(ResultType, Span);
```

- **Pro:** Consistent with `TypedArgRef` and `TypedFieldRef` which already carry `DeclaredQualifiers`.
- **Pro:** The type checker already knows the qualifiers when creating `TypedLiteral` for typed constants — just needs to pass them through.
- **Con:** `TypedLiteral` is used for ALL literals (numbers, booleans, strings), not just typed constants. Adding a nullable qualifier property that's only populated for typed constants is a shape smell.

**Option B: New `TypedTypedConstant` node type**

```csharp
public sealed record TypedTypedConstant(
    TypeKind ResultType,
    object? Value,
    ImmutableArray<DeclaredQualifierMeta> DeclaredQualifiers,
    SourceSpan Span
) : TypedExpression(ResultType, Span);
```

- **Pro:** Clean separation — only typed constants carry qualifiers.
- **Pro:** Non-nullable `DeclaredQualifiers` — construction requires providing them.
- **Con:** New node type means updating every consumer that pattern-matches on `TypedLiteral` for typed constants.

### Recommendation

**Option A** — add the nullable property. The type checker already distinguishes typed constants from plain literals. The nullable makes the "only populated for typed constants" contract explicit, and the proof engine can pattern-match `TypedLiteral { DeclaredQualifiers: { } quals }` exactly like it does for `TypedArgRef`. Option B is cleaner in theory but creates unnecessary churn for the same semantic.

### What Shane needs to decide

**Option A or Option B?** This is a node-shape decision that affects the semantic index contract.
