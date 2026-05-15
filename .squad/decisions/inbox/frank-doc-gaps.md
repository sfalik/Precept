## 2026-05-14T23:36:31.558-04:00

- `docs/language/business-domain-types.md:373` now contradicts the normalized counting-unit design. It still says counting units such as `each`, `case`, `pack`, and `dozen` are "opaque" with "no shared dimension," but the current architecture intentionally gives business counting units the shared count dimension (`DimensionVector.None`) plus factor-one representation and relies on PRE0137 to enforce unit-code identity inside that family. That doc needs a follow-up correction outside Slices 38–42.
