### 2026-05-07: Slice 4 Complete
**By:** George (for Soup Nazi)
**Commit:** `ac95de2`

**TypedTypedConstant triggers:** A string literal becomes `TypedTypedConstant` (instead of `TypedLiteral`) when:
1. The literal's `LiteralKind` is `TokenKind.TypedConstant` (single-quoted string in DSL), AND
2. An `expectedType` context is provided (non-null, non-Error), AND
3. The target type's `TypeMeta.ContentValidation` is non-null (Date, Time, DateTime, Period, Currency, UnitOfMeasure, Dimension).

Without `expectedType` context, a typed constant emits `UnresolvedTypedConstant` and returns `TypedErrorExpression`.

**ContentValidation dispatch:**
- `NodaTimeValidation` → Date (`LocalDatePattern.Iso`), Time (`LocalTimePattern.ExtendedIso`), DateTime (`LocalDateTimePattern.ExtendedIso`), Period (`PeriodPattern.NormalizingIso`)
- `ClosedSetValidation` → Currency (ISO 4217 codes, case-insensitive), UnitOfMeasure (recognized units, case-insensitive), Dimension (recognized families, case-insensitive)
- `RegexValidation` → general pattern match via `System.Text.RegularExpressions.Regex.IsMatch`

On validation failure → `InvalidTypedConstantContent` diagnostic + `TypedErrorExpression`.

**DiagnosticCodes used:**
- `UnresolvedTypedConstant` (52) — typed constant with no type context
- `InvalidTypedConstantContent` (53) — typed constant content fails validation

**Context threading:** `expectedType` is passed as an optional `TypeKind?` parameter to `Resolve(expr, ctx, expectedType)`. Callers set it:
- Field defaults: caller passes `field.ResolvedType` (wiring deferred to when default resolution is implemented)
- Binary op context retry: when bottom-up fails and one operand is a literal, re-resolve with the other side's type
- Function call context retry: when overload resolution fails, re-resolve literal args with each candidate parameter type

For Soup Nazi test setup: call `TypeChecker.ResolveExpression(expr, ctx, expectedType: TypeKind.Date)` to test typed constant resolution with context. Without the expectedType, typed constants will emit `UnresolvedTypedConstant`.

**Valid typed constant examples:**
- `'2026-01-15'` with expectedType=Date → `TypedTypedConstant(Date, "2026-01-15", LocalDate(2026,1,15))`
- `'USD'` with expectedType=Currency → `TypedTypedConstant(Currency, "USD", "USD")`
- `'09:30:00'` with expectedType=Time → `TypedTypedConstant(Time, "09:30:00", LocalTime(9,30,0))`

**Invalid typed constant examples:**
- `'2026-13-01'` with expectedType=Date → `InvalidTypedConstantContent` (invalid month)
- `'XYZ'` with expectedType=Currency → `InvalidTypedConstantContent` (not in ISO 4217)
- `'not-a-time'` with expectedType=Time → `InvalidTypedConstantContent` (NodaTime parse failure)

**NodaTime parsers per type:**
- Date → `LocalDatePattern.Iso.Parse()` (pattern: `uuuu'-'MM'-'dd`)
- Time → `LocalTimePattern.ExtendedIso.Parse()` (pattern: `HH':'mm':'ss`)
- DateTime → `LocalDateTimePattern.ExtendedIso.Parse()` (pattern: `uuuu'-'MM'-'dd'T'HH':'mm':'ss`)
- Period → `PeriodPattern.NormalizingIso.Parse()` (normalizing ISO 8601)
