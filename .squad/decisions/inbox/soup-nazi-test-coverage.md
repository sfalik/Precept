### 2026-05-14T23:36:31.558-04:00 — Quantity normalization test skeleton status
**By:** Soup Nazi
**What:** Added Slice 17, 21, and 37 quantity-normalization test skeletons across type-checker, proof, numeric-interval, and language-normalizer coverage.
**Status:** `dotnet build test\Precept.Tests\Precept.Tests.csproj` passed. Targeted verification run (`31` tests) finished `19 passed / 12 failed`.

- **Observed red in the targeted run / waiting on implementation:**
  - Affine scalar normalization still behaves linearly for Celsius/Fahrenheit/Reaumur, so the new affine-value expectations fail.
  - The catalog still lacks or does not surface the full affine/logarithmic metadata surface required by the new atom/unit assertions (for example `dB` lookup / `AffineOffset` expectations).
  - Cross-unit safe quantity assignment (`6 [lb_av]` into `max '5 kg'`) still overflows because the compare path stays in raw magnitudes.
  - WholeValue interpolation and interpolated-magnitude-with-static-unit scenarios still resolve to unbounded proof intervals and conservatively emit `NumericOverflow`.
  - Cross-unit price normalization still does not overflow because denominator normalization is not applied at compare time.
  - The cross-dimension literal case currently does not surface the intended qualifier/dimension diagnostic.

- **Observed green in the targeted run:**
  - Build stayed clean after the new test files landed.
  - The targeted run passed 19 checks, including the shift coverage, the cross-temperature proof checks that already align with current behavior, same-unit/cross-unit overflow-side anchors, and the money overflow regression.

- **Expected red / waiting on implementation:**
  - Slice 17 cross-unit quantity and price overflow proofs depend on normalized interval comparison for typed constants.
  - Slice 17 WholeValue interpolation depends on the source-interval extraction path avoiding double normalization.
  - Slice 21 interpolated `'{intField} [lb_av]'` coverage depends on `InterpolatedTypedConstant` interval extraction plus static-unit scaling.
  - Slice 37 scalar normalization tests depend on a concrete `TypedConstantNormalizer` type with `NormalizeQuantity` / `DenormalizeQuantity`.
  - Slice 37 catalog/parsing tests depend on `UcumAtom` and `UcumParsedUnit` exposing `AffineOffset`.
  - Slice 37 interval tests depend on `NumericInterval.Shift(decimal)`.
  - Slice 37 cross-temperature proof tests depend on affine normalization flowing through proof-bound comparison.

- **Expected green once the lane is complete:**
  - Same-unit quantity overflow regression should remain green after normalization work.
  - Plain money overflow should remain green because currencies are not UCUM-normalizable.

- **Why this note exists:** red tests here are honest contract pressure, not noise; they document exactly which missing implementation surface each slice still needs.
