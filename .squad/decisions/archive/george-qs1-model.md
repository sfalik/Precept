# George — QS-1 model additions

Date: 2026-05-15

## Decision

Implement QS-1 as pure model metadata only:

- Add `QualifierAxis.PriceIn` as the polymorphic `price in ...` axis.
- Add `QualifierShape.OfRequiresCurrencyIn` so the shape can declare the `price in ... of ...` constraint without consumer hardcoding.
- Add `DeclaredQualifierMeta.CompoundPrice` to carry `currency/unit` compound `in` values as a first-class qualifier payload.

## Guardrails

- Do **not** change `QS_CurrencyAndDimension` in this slice. It stays on `QualifierAxis.Currency` until QS-2 lands `MapPriceInQualifier` atomically.
- Do **not** wire type-checker behavior in QS-1.
- Keep existing consumer behavior unchanged; rely on existing `_` / `default` fallbacks where qualifier-axis switches are already non-exhaustive.

## Validation

- `dotnet build src\Precept\ -v:minimal -nologo`
- `dotnet test test\Precept.Tests\ --no-build --logger "console;verbosity=minimal"`
- Result after QS-1: same known baseline of 9 failing tests (5561 passed / 9 failed / 5570 total).
