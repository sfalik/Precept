### 2026-05-05T01:00Z: User directive — `DimensionCode` property named `MeasureDimension`

**By:** Shane (via Copilot)

**Status:** Directive recorded. Locks naming for the `dimension` field CLR type.

**Merged source:** `copilot-directive-2026-05-04T20-59-49.md`.

- The `DimensionCode` property on the `Quantity` CLR type (and the type itself) shall be named `MeasureDimension`, not `DimensionCode` or `Dimension`.
- Rationale: `Dimension` is already a first-class Precept language type (`TypeKind.Dimension`, keyword `dimension`). Using it as a property name creates a scope collision. `MeasureDimension` is distinct, unambiguous, and future-proof against a planned algebraic `Dimension` type for dimensional analysis.
