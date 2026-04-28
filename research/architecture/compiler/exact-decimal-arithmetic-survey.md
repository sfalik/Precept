# Exact Decimal Arithmetic Survey

> Raw research collection. No interpretation, no conclusions, no recommendations.
> Research question: How do programming language runtimes implement exact base-10 decimal arithmetic, handle precision propagation, and make overflow a deterministic error rather than silent corruption?

---

## .NET System.Decimal

Source: [Microsoft Docs — System.Decimal](https://learn.microsoft.com/en-us/dotnet/api/system.decimal), [ECMA-335 CLI Specification §IV](https://ecma-international.org/publications-and-standards/standards/ecma-335/), [.NET Runtime source — Decimal.cs](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Decimal.cs)

### Internal Representation

`System.Decimal` is a 128-bit value type. The bit layout is defined as four 32-bit integers (`lo`, `mid`, `hi`, `flags`):

```
Bits 127–96  : flags word
  Bit 31     : sign (0 = positive, 1 = negative)
  Bits 23–16 : scale (0–28) — power of 10 to divide the integer part
  Bits 15–0  : reserved (must be zero)

Bits 95–64   : hi32  — high 32 bits of 96-bit integer mantissa
Bits 63–32   : mid32 — middle 32 bits of 96-bit integer mantissa
Bits 31–0    : lo32  — low 32 bits of 96-bit integer mantissa
```

The value is: `(-1)^sign × (lo32 | mid32<<32 | hi32<<64) × 10^(-scale)`

The 96-bit mantissa holds an unsigned integer in the range [0, 2^96 − 1], approximately 7.92 × 10^28. With the scale exponent ranging 0–28:

- Maximum value: `79,228,162,514,264,337,593,543,950,335` (≈ 7.9 × 10^28)
- Minimum value: `-79,228,162,514,264,337,593,543,950,335`
- Minimum positive nonzero value: `0.0000000000000000000000000001` (1 × 10^-28)
- Significant digits: 28–29 (28 guaranteed; in practice 29 in edge cases)

There is no special representation for NaN, Infinity, or negative zero. The value 0 with scale 2 (representing `0.00`) and 0 with scale 0 (representing `0`) are distinct in bit pattern but compare as equal.

Source: [.NET Runtime — Decimal internals](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Decimal.cs#L40)

### Arithmetic Precision Propagation

**Addition and Subtraction:** The runtime aligns the two operands to the same scale by scaling up the operand with the smaller scale (multiplying its mantissa by a power of 10). The result scale is `max(scale_a, scale_b)`. If alignment overflows the 96-bit mantissa, the runtime rescales both operands to a common lower scale. Result scale may be reduced to fit the result mantissa.

**Multiplication:** The result mantissa is the product of both mantissas; the result scale is `scale_a + scale_b`. Because the product of two 96-bit integers can be up to 192 bits, the runtime must reduce scale by rounding to fit back into 96 bits. The rounding mode used internally is **round half to even** (banker's rounding) during this reduction.

**Division:** The numerator is scaled up by powers of 10 until division produces a remainder-free result or 28 digits are reached. If the quotient cannot be represented exactly in 28–29 significant digits, the result is **rounded** (round half to even) to fit — no exception is raised.

**Arithmetic preservation:** The runtime attempts to preserve trailing zeros (e.g., `1.10 + 2.20 = 3.30` keeps scale 2), but this is not guaranteed — the scale may be reduced when required to fit the 96-bit mantissa.

Source: [ECMA-335 §Partition I, Type System](https://ecma-international.org/publications-and-standards/standards/ecma-335/); [.NET Runtime — DecCalc.cs](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Decimal.DecCalc.cs)

### Overflow Semantics

When an arithmetic result exceeds the representable range (mantissa > 2^96 − 1, after all scaling attempts), `System.Decimal` throws `System.OverflowException`. There is no silent wraparound, no saturation, and no NaN result. This is a synchronous, typed exception.

```csharp
decimal a = decimal.MaxValue;  // 79228162514264337593543950335
decimal b = 1m;
decimal c = a + b;  // throws OverflowException
```

`decimal.Add`, `decimal.Subtract`, `decimal.Multiply`, `decimal.Divide` all throw `OverflowException` on overflow. There are no `checked`/`unchecked` modes — `decimal` arithmetic is always checked.

Source: [System.Decimal.Add — docs](https://learn.microsoft.com/en-us/dotnet/api/system.decimal.add)

### Non-Terminating Division Handling

`System.Decimal` does **not** throw on non-terminating decimal division (e.g., `1m / 3m`). Instead, the result is rounded to 28–29 significant digits using round-half-to-even. The result `1m / 3m = 0.3333333333333333333333333333` (28 threes).

`System.DivideByZeroException` is thrown only on division by zero (not on inexact division). The runtime provides no mechanism to detect or trap inexactness — the programmer cannot know whether the division result was exact or rounded.

Source: [System.Decimal division behavior](https://learn.microsoft.com/en-us/dotnet/api/system.decimal.divide)

### Equality and Comparison Semantics

`decimal` equality (`==`) and `Equals()` are **value-equal**, not representation-equal. Two `decimal` values with different scales but the same mathematical value compare as equal:

```csharp
decimal a = 1.0m;   // scale = 1, mantissa = 10
decimal b = 1.00m;  // scale = 2, mantissa = 100
Console.WriteLine(a == b);       // True
Console.WriteLine(a.Equals(b));  // True
```

`GetHashCode()` returns the same value for mathematically equal decimals regardless of scale. `CompareTo()` also compares by value, not representation. This means `decimal` cannot be used as a dictionary key to distinguish `1.0` from `1.00`.

Source: [System.Decimal.Equals](https://learn.microsoft.com/en-us/dotnet/api/system.decimal.equals)

### Rounding Modes

`Math.Round(decimal, int, MidpointRounding)` supports:
- `MidpointRounding.AwayFromZero` — traditional "round half up" (0.5 → 1, -0.5 → -1)
- `MidpointRounding.ToEven` — banker's rounding (round to nearest even; 0.5 → 0, 1.5 → 2)
- `MidpointRounding.ToZero` (truncation, .NET 6+)
- `MidpointRounding.ToNegativeInfinity` (floor, .NET 6+)
- `MidpointRounding.ToPositiveInfinity` (ceiling, .NET 6+)

Internal arithmetic rounding (during multiplication and division that must reduce scale) always uses **round half to even**. There is no way to change the rounding mode used internally by `+`, `-`, `*`, `/`.

`decimal.Round(decimal, int)` without `MidpointRounding` defaults to `ToEven`.

Source: [MidpointRounding enum](https://learn.microsoft.com/en-us/dotnet/api/system.midpointrounding)

### Financial Calculation Suitability

Considered appropriate for financial calculations in .NET applications. Avoids binary floating-point rounding artifacts (e.g., `0.1 + 0.2 ≠ 0.3` in `double`). Limitations: ~28–29 significant digits (insufficient for some actuarial or scientific calculations). No trap for inexact division. Slower than `double` by roughly 20–100× depending on operation and hardware (no dedicated FPU instructions). Not suitable for calculations requiring more than 28–29 significant digits.

Source: [Microsoft — Choose between decimal and double](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types)

### Notes

- The `M` suffix on literals creates `decimal` literals at compile time: `1.5m`.
- `decimal` is not a primitive in the CLR type system — it is a value type without dedicated IL opcodes; all arithmetic is performed through static method calls compiled to `call` instructions.
- In C# `checked` blocks, `decimal` arithmetic is unaffected — it already throws `OverflowException` unconditionally. The `checked`/`unchecked` contexts only affect `int`, `long`, `short`, `sbyte`, `char`, and their unsigned counterparts.
- Starting with .NET 7, `decimal` implements `INumber<decimal>` from the generic math interfaces.

---

## Java BigDecimal

Source: [OpenJDK — BigDecimal.java](https://github.com/openjdk/jdk/blob/master/src/java.base/share/classes/java/math/BigDecimal.java), [Java SE 21 API Docs](https://docs.oracle.com/en/java/se/21/docs/api/java.base/java/math/BigDecimal.html), [Java Language Specification §4.2.3](https://docs.oracle.com/javase/specs/jls/se21/html/jls-4.html#jls-4.2.3)

### Internal Representation

`BigDecimal` is an arbitrary-precision decimal number represented as:

```
value = unscaledValue × 10^(-scale)
```

Where:
- `unscaledValue` is a `BigInteger` — arbitrary-precision, unlimited size
- `scale` is a 32-bit signed integer (`int`) — the number of digits to the right of the decimal point

Examples:
- `3.14` → unscaledValue = 314, scale = 2
- `314` → unscaledValue = 314, scale = 0
- `3.14e10` → unscaledValue = 31400000000, scale = 0 (or equivalently unscaledValue = 314, scale = -2)

Internally, `BigInteger` stores its magnitude as an `int[]` array, allowing truly arbitrary precision at the cost of heap allocation. There is no fixed upper bound on the number of significant digits.

Source: [BigDecimal.java — OpenJDK](https://github.com/openjdk/jdk/blob/master/src/java.base/share/classes/java/math/BigDecimal.java)

### Arithmetic Precision Propagation

`BigDecimal` arithmetic has two modes governed by `MathContext`:

**UNLIMITED mode (no MathContext / `MathContext.UNLIMITED`):**
- Addition/Subtraction: result scale = `max(scale_a, scale_b)`; result is exact
- Multiplication: result scale = `scale_a + scale_b`; result is exact
- Division: **throws `ArithmeticException`** if the result is a non-terminating decimal

**Explicit MathContext (e.g., `MathContext.DECIMAL128`):**
- Specifies: `precision` (number of significant digits) and `roundingMode`
- All operations round to `precision` significant digits

Standard `MathContext` constants:
```java
MathContext.UNLIMITED    // precision = 0 (unlimited), roundingMode = UNNECESSARY
MathContext.DECIMAL32    // precision = 7,  roundingMode = HALF_UP
MathContext.DECIMAL64    // precision = 16, roundingMode = HALF_UP
MathContext.DECIMAL128   // precision = 34, roundingMode = HALF_UP
```

Scale propagation rules without MathContext:
- `add(BigDecimal)`: `max(this.scale(), augend.scale())`
- `multiply(BigDecimal)`: `this.scale() + multiplicand.scale()`
- `divide(BigDecimal, int scale, RoundingMode)`: caller specifies output scale explicitly

Source: [BigDecimal — arithmetic operations](https://docs.oracle.com/en/java/se/21/docs/api/java.base/java/math/BigDecimal.html#add(java.math.BigDecimal))

### Overflow Semantics

`BigDecimal` does **not** overflow. Because `BigInteger` is arbitrary-precision, any multiplication or addition that produces a larger result simply allocates more heap space. The only bounds are:

- Scale `int` can theoretically exceed `Integer.MAX_VALUE` or `Integer.MIN_VALUE` through repeated operations. If it does, `ArithmeticException("Overflow")` is thrown. In practice, this requires constructing pathological cases.
- `divide()` without a specified scale on a non-terminating decimal throws `ArithmeticException("Non-terminating decimal expansion; no exact representable decimal result.")` — not an overflow, but an exactness failure.

Memory exhaustion (heap OOM) is the practical upper bound, not a language-level overflow.

Source: [BigDecimal.divide — Java docs](https://docs.oracle.com/en/java/se/21/docs/api/java.base/java/math/BigDecimal.html#divide(java.math.BigDecimal))

### Non-Terminating Division Handling

`BigDecimal` has multiple `divide` overloads:

```java
// Throws ArithmeticException if result is non-terminating:
BigDecimal result = new BigDecimal("1").divide(new BigDecimal("3")); // THROWS

// Specifies scale and rounding mode — always produces a result:
BigDecimal result = new BigDecimal("1").divide(new BigDecimal("3"), 10, RoundingMode.HALF_UP);
// result = 0.3333333333

// With MathContext — rounds to precision significant digits:
BigDecimal result = new BigDecimal("1").divide(new BigDecimal("3"), MathContext.DECIMAL128);
// result = 0.3333333333333333333333333333333333 (34 digits)
```

This design makes the programmer **explicitly choose** between exactness (exception on non-terminating results) and rounding (with an explicit rounding mode). This is the fundamental design difference from `System.Decimal`, which silently rounds.

`divideToIntegralValue(BigDecimal)` returns the integer part of the quotient; `remainder(BigDecimal)` returns the mathematical remainder. These are always exact.

Source: [BigDecimal.divide](https://docs.oracle.com/en/java/se/21/docs/api/java.base/java/math/BigDecimal.html#divide(java.math.BigDecimal,int,java.math.RoundingMode))

### Equality and Comparison Semantics

This is a well-known Java pitfall:

```java
BigDecimal a = new BigDecimal("1.0");
BigDecimal b = new BigDecimal("1.00");

a.equals(b);     // FALSE — different scale (1 vs. 2)
a.compareTo(b);  // 0   — same mathematical value
```

`equals()` requires both the mathematical value AND the scale to match. `compareTo()` compares only mathematical value. This inconsistency means `BigDecimal` violates the general contract that `equals` and `compareTo` be consistent.

Consequence: using `BigDecimal` as a key in a `HashMap` or `HashSet` — `new BigDecimal("1.0")` and `new BigDecimal("1.00")` are treated as **different keys**. To normalize: use `stripTrailingZeros()` before comparing or hashing, or always use `compareTo()`.

Source: [BigDecimal.equals — Java docs](https://docs.oracle.com/en/java/se/21/docs/api/java.base/java/math/BigDecimal.html#equals(java.lang.Object)); [Effective Java 3rd Ed., Item 14]

### Rounding Modes

`java.math.RoundingMode` (introduced in Java 5, replacing deprecated `BigDecimal.ROUND_*` constants):

| Mode | Behavior |
|------|----------|
| `UP` | Round away from zero |
| `DOWN` | Truncate toward zero |
| `CEILING` | Round toward positive infinity |
| `FLOOR` | Round toward negative infinity |
| `HALF_UP` | Round to nearest; ties go away from zero (classic "round half up") |
| `HALF_DOWN` | Round to nearest; ties go toward zero |
| `HALF_EVEN` | Round to nearest; ties go to even digit (banker's rounding) |
| `UNNECESSARY` | Assert result is exact; throw `ArithmeticException` if rounding is needed |

`HALF_EVEN` is recommended for financial calculations (reduces cumulative rounding bias). `UNNECESSARY` is used in testing/assertion contexts to verify no rounding occurred.

The `setScale(int, RoundingMode)` method is the `quantize()` equivalent:
```java
BigDecimal value = new BigDecimal("3.14159");
BigDecimal rounded = value.setScale(2, RoundingMode.HALF_UP); // 3.14
```

Source: [java.math.RoundingMode](https://docs.oracle.com/en/java/se/21/docs/api/java.base/java/math/RoundingMode.html)

### Financial Calculation Suitability

Considered the gold standard for financial arithmetic in Java. Used in core banking systems, trading platforms, and financial reporting. Arbitrary precision enables actuarial calculations beyond 28 digits. The `DECIMAL128` MathContext aligns with ISO/IEC standards. The explicit rounding mode requirement ensures no accidental precision loss.

Performance: significantly slower than `double` due to heap allocation, arbitrary-precision arithmetic, and GC pressure. Benchmarks show 10–100× slower for basic arithmetic. For high-frequency trading requiring millions of operations per second, specialized fixed-point or `long`-based approaches are used instead. Each `BigDecimal` instance allocates at least one `BigInteger` internally, plus array storage proportional to the number of digits.

Source: [Java Performance Tuning — BigDecimal cost](https://www.oracle.com/technical-resources/articles/java/javaperf.html)

### Notes

- `BigDecimal(double)` is a trap: `new BigDecimal(0.1)` produces `0.1000000000000000055511151231257827021181583404541015625` — the exact binary float value. Always use `BigDecimal("0.1")` or `BigDecimal.valueOf(0.1)` (which calls `Double.toString()` first).
- `BigDecimal.valueOf(long unscaledVal, int scale)` is the safe factory method for constructing from integer mantissa + scale.
- `stripTrailingZeros()` removes trailing zeros, potentially producing a negative scale: `new BigDecimal("100").stripTrailingZeros()` = `1E+2` (scale = -2).
- `BigDecimal` is immutable and thread-safe.

---

## IEEE 754-2008 Decimal Floating-Point (decimal64, decimal128)

Source: [IEEE Std 754-2008](https://ieeexplore.ieee.org/document/4610935), [IEEE Std 754-2019](https://ieeexplore.ieee.org/document/8766229), [Intel Decimal Floating-Point Math Library](https://www.intel.com/content/www/us/en/developer/articles/tool/intel-decimal-floating-point-math-library.html), [IBM General Decimal Arithmetic](https://speleotrove.com/decimal/)

### Internal Representation

IEEE 754-2008 defines three decimal floating-point formats:

**decimal32** (32-bit): 7 significant decimal digits; exponent range −101 to +90.

**decimal64** (64-bit): 16 significant decimal digits; exponent range −398 to +369. Two encoding formats: Binary Integer Decimal (BID) and Densely Packed Decimal (DPD).

**decimal128** (128-bit): 34 significant decimal digits; exponent range −6176 to +6111. `java.math.MathContext.DECIMAL128` targets this format.

**BID encoding (Binary Integer Decimal — used by Intel, most x86 implementations):**
```
decimal64 BID layout (64 bits):
  Bit 63:     sign
  Bits 62–53: combination field (exponent + leading digit)
  Bits 52–0:  coefficient continuation (binary integer significand)
```

**DPD encoding (Densely Packed Decimal — used by IBM, Power architecture):**
Groups of 10 bits encode 3 decimal digits (1000 values in 1024 possible 10-bit patterns).

Special values supported: `+Infinity`, `-Infinity`, `NaN` (quiet NaN and signaling NaN), `+0`, `-0`, and subnormal numbers.

**Comparison with System.Decimal:** IEEE 754 decimal types support `NaN`, `Infinity`, and subnormal values; `System.Decimal` does not. IEEE 754 decimal is a floating-point type (the exponent floats); `System.Decimal` has a fixed 96-bit mantissa with a scale in [0, 28].

Source: [IEEE 754-2008 §3.5 — decimal formats](https://ieeexplore.ieee.org/document/4610935)

### Arithmetic Precision Propagation

IEEE 754-2008 §5.3.1 defines preferred exponent rules:

- **Addition:** preferred exponent is `min(exponent_a, exponent_b)`; result is the exact mathematical sum rounded to `p` significant digits if necessary
- **Multiplication:** preferred exponent is `exponent_a + exponent_b`; result is the exact product rounded to `p` significant digits
- **Division:** preferred exponent is `exponent_a − exponent_b`; result is rounded to `p` significant digits

The standard mandates that all basic operations (+, −, ×, ÷, sqrt, fused multiply-add) be computed **as if to infinite precision, then rounded once** to the target precision. This is the "correct rounding" guarantee — the only rounding error introduced is the final round step.

Source: [IEEE 754-2008 §4 — rounding direction attributes](https://ieeexplore.ieee.org/document/4610935)

### Overflow Semantics

IEEE 754-2008 defines five exception conditions, each with a flag bit and a default result:

| Exception | Condition | Default result | Flag |
|-----------|-----------|----------------|------|
| `invalidOperation` | 0/0, ∞−∞, NaN input to certain ops | quiet NaN | `IEEE_FLAGS_INVALID` |
| `divisionByZero` | finite ÷ 0 | ±∞ | `IEEE_FLAGS_DIV_BY_ZERO` |
| `overflow` | result exceeds maximum finite | ±∞ (or max finite, depending on rounding mode) | `IEEE_FLAGS_OVERFLOW` |
| `underflow` | result is subnormal and inexact | subnormal or ±0 | `IEEE_FLAGS_UNDERFLOW` |
| `inexact` | result was rounded | rounded result | `IEEE_FLAGS_INEXACT` |

On overflow, the **default behavior** is to return ±Infinity, not to throw an exception. The overflow flag is set in the status register. To convert overflow into an exception, the programmer must enable "traps" for the overflow flag.

This is the fundamental difference from `System.Decimal` and Java's `BigDecimal`: IEEE 754 decimal defaults to silent ±Infinity on overflow, not a thrown exception.

Source: [IEEE 754-2008 §7 — exception handling](https://ieeexplore.ieee.org/document/4610935)

### Non-Terminating Division Handling

IEEE 754-2008 handles non-terminating division by rounding to the target precision (16 significant digits for decimal64, 34 for decimal128). The `inexact` flag is set. There is no exception for non-terminating decimals — the programmer must check the `inexact` flag explicitly or trap on it.

`1 ÷ 3` in decimal64: `3.333333333333333E-1` (16 threes), `inexact` flag set.

Source: [IBM General Decimal Arithmetic — division](https://speleotrove.com/decimal/daops.html#refdivide)

### Equality and Comparison Semantics

IEEE 754-2008 §5.11 defines comparison predicates:

- **`totalOrder(x, y)`:** total ordering that includes negative zero and NaN. `-0 < +0`; NaN is ordered after all other values.
- **`compareQuiet(x, y)`:** value comparison; `NaN` returns unordered (comparison is false); `+0 == -0`.
- **`compareSignaling(x, y)`:** like compareQuiet but raises `invalidOperation` if either operand is NaN.

`1.0` and `1.00` in IEEE 754 decimal have different representations (different exponents) but compare as equal under all comparison predicates except `totalOrder`. Under `totalOrder`: `1.0 < 1.00`.

Source: [IEEE 754-2008 §5.11](https://ieeexplore.ieee.org/document/4610935)

### Rounding Modes

IEEE 754-2008 defines five rounding modes ("rounding-direction attributes"):

| Mode | Description |
|------|-------------|
| `roundTiesToEven` | Round to nearest; ties go to even digit (banker's rounding) — **default** |
| `roundTiesToAway` | Round to nearest; ties go away from zero |
| `roundTowardPositive` | Round toward +∞ (ceiling) |
| `roundTowardNegative` | Round toward −∞ (floor) |
| `roundTowardZero` | Round toward zero (truncation) |

The rounding mode is a thread-local or context attribute set via `fesetround()` in C. This global/thread-local state is a source of bugs in multi-threaded code.

Source: [IEEE 754-2008 §4.3](https://ieeexplore.ieee.org/document/4610935)

### Financial Calculation Suitability

decimal128 meets the precision requirements of virtually all financial calculations (34 significant digits). The standard is designed for financial use. Hardware support (IBM POWER6+, Intel extensions via software library) can make it faster than software-only approaches.

Limitations: the default silent-Infinity-on-error semantics require discipline in financial code to check flags; the global rounding mode is a concurrency hazard; NaN/Infinity as valid result values can propagate silently through calculations.

Source: [IBM — Decimal floating point in the Power architecture](https://ieeexplore.ieee.org/document/4510514)

### Notes

- C23 adds `_Decimal32`, `_Decimal64`, `_Decimal128` as optional standard types. GCC supports them as an extension with `--enable-decimal-float`.
- The Intel Decimal Floating-Point Math Library (libdfp) provides software emulation of IEEE 754-2008 decimal on x86.
- Java's `BigDecimal` with `MathContext.DECIMAL128` approximates decimal128 semantics but does not implement the IEEE 754-2008 exception flag model.
- The General Decimal Arithmetic specification (Cowlishaw, IBM) predates and was incorporated into IEEE 754-2008; Python's `decimal` module implements this spec.

---

## SQL DECIMAL / NUMERIC

Source: [ANSI SQL:2016 Standard §6.27, §8.1](https://www.iso.org/standard/63555.html), [PostgreSQL 15 Docs — Numeric](https://www.postgresql.org/docs/15/datatype-numeric.html), [SQL Server T-SQL Docs — decimal and numeric](https://learn.microsoft.com/en-us/sql/t-sql/data-types/decimal-and-numeric-transact-sql), [Oracle Database SQL Reference — NUMBER](https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Data-Types.html)

### Internal Representation

**ANSI SQL standard:**
`DECIMAL(p, s)` and `NUMERIC(p, s)` are declared types where `p` = total significant decimal digits and `s` = digits to the right of the decimal point. `DECIMAL(10, 2)` can hold values from −99999999.99 to 99999999.99. ANSI SQL requires `NUMERIC` to be exact; `DECIMAL` may have more precision than declared. In practice, most databases treat them identically.

**PostgreSQL internal representation:**
PostgreSQL uses a variable-length format. `NUMERIC` without precision/scale is arbitrary precision. Internally, the value is stored as an array of `int16` "digits" where each "digit" is a value in [0, 9999] representing 4 decimal digits (base-10000). The format includes a sign, weight (power of 10000 of the most significant group), display scale, and the array of base-10000 groups. Maximum precision: up to 131072 digits before the decimal point and up to 16383 digits after.

**SQL Server internal representation:**
Stores `DECIMAL(p, s)` as fixed-width integers scaled by 10^s. Storage width depends on precision:
- 1–9 significant digits: 5 bytes
- 10–19 significant digits: 9 bytes
- 20–28 significant digits: 13 bytes
- 29–38 significant digits: 17 bytes

Maximum: `DECIMAL(38, 38)` — 38 significant digits.

**Oracle NUMBER:**
Oracle `NUMBER(p, s)` uses a variable-length base-100 internal format (each byte stores two decimal digits in "centimal" encoding). Supports up to 38 significant digits. `NUMBER` without precision/scale is essentially unconstrained.

### Arithmetic Precision Propagation

**ANSI SQL standard rules** (SQL:2016 §6.27) for result type of arithmetic on `NUMERIC(p1, s1)` and `NUMERIC(p2, s2)`:

| Operation | Result precision | Result scale |
|-----------|-----------------|--------------|
| e1 + e2 or e1 − e2 | `max(s1, s2) + max(p1−s1, p2−s2) + 1` | `max(s1, s2)` |
| e1 × e2 | `p1 + p2 + 1` | `s1 + s2` |
| e1 ÷ e2 | implementation-defined | implementation-defined |

The precision of addition grows by 1 to accommodate carry. For `DECIMAL(5,2) + DECIMAL(5,2)`, the result precision is `max(2,2) + max(3,3) + 1 = 8`, scale is `max(2,2) = 2`.

Division result type is explicitly left implementation-defined in the standard because the precision/scale of a quotient is theoretically unlimited.

**PostgreSQL behavior:**
- Addition/subtraction: result scale = `max(s1, s2)`, precision grows to accommodate
- Multiplication: result scale = `s1 + s2`
- Division: adds up to `max(4, scale+4)` additional digits; result rounded

**SQL Server behavior:**
- Multiplication: result precision = `p1 + p2 + 1` (capped at 38); result scale = `s1 + s2` (may be reduced if precision cap hit)
- Division: result scale = `max(6, s1 + p2 + 1)`; result precision capped at 38
- When the precision cap (38) is exceeded, the integer part is preserved and the fractional scale is reduced to fit

**Oracle behavior:**
- Addition/subtraction: result precision = `max(p1−s1, p2−s2) + max(s1,s2) + 1`, scale = `max(s1,s2)`
- Multiplication: `NUMBER` (unconstrained, up to 38 digits)

Source: [SQL Server decimal precision rules](https://learn.microsoft.com/en-us/sql/t-sql/data-types/precision-scale-and-length-transact-sql); [PostgreSQL NUMERIC operators](https://www.postgresql.org/docs/current/functions-math.html)

### Overflow Semantics

**PostgreSQL:** Raises `ERROR: numeric field overflow` — rolls back the statement. No silent truncation.

**SQL Server:** Raises arithmetic overflow error (msg 8115, level 16). Rolls back the statement. `SET ARITHABORT ON` (default in modern SQL Server) makes this roll back the batch; `SET ARITHABORT OFF, ANSI_WARNINGS OFF` causes the value to be silently replaced with NULL — a controversial legacy behavior. Default in modern SQL Server is ARITHABORT ON.

**Oracle:** `ORA-01426: numeric overflow` when the integer portion overflows. No silent truncation of the integer part.

**MySQL:** In strict mode (default since 5.7): `ERROR 1264 (22003): Out of range value for column`. In non-strict mode: silently truncates to the maximum representable value (silent corruption).

### Non-Terminating Division Handling

All major databases handle non-terminating division by returning a rounded result to some database-specific precision:

- **PostgreSQL:** Returns a `NUMERIC` result with up to `max(4, s1 + p2 + 1)` decimal places, with the last digit rounded. No error.
- **SQL Server:** Returns `DECIMAL(38, s)` where `s = max(6, s1 + p2 + 1)`, rounded. No error.
- **Oracle:** Returns `NUMBER` (unconstrained), rounded to 38 significant digits. No error.

Example: `1 / 3` in PostgreSQL returns `0.33333333333333333333` (20 decimal places by default for integer division).

Source: [PostgreSQL division behavior](https://www.postgresql.org/docs/current/functions-math.html)

### Equality and Comparison Semantics

In SQL, `DECIMAL(10,2)` and `DECIMAL(10,4)` are different declared types but comparison is by value. `1.50 = 1.5` evaluates to TRUE. All ANSI SQL databases perform value comparison; no scale-mismatch issues for comparison.

SQL NULL semantics apply: any comparison with NULL yields NULL (not true/false), and `NULL = NULL` is NULL, not TRUE. This is three-valued logic (TRUE / FALSE / UNKNOWN).

Trailing zeros: SQL databases generally normalize display but store with declared scale (`1.5` stored as `DECIMAL(10,2)` is stored as `1.50`).

### Rounding Modes

ANSI SQL does not mandate a specific rounding mode. In practice:
- **PostgreSQL:** Uses round-half-away-from-zero for `ROUND()` function; storage truncates (round-toward-zero) rather than rounding to nearest.
- **SQL Server:** `ROUND(numeric, length)` uses round-half-away-from-zero by default. `ROUND(numeric, length, 1)` truncates.
- **Oracle:** `ROUND()` uses round-half-away-from-zero. `TRUNC()` truncates.

Source: [SQL Server ROUND](https://learn.microsoft.com/en-us/sql/t-sql/functions/round-transact-sql)

### Financial Calculation Suitability

The standard approach for financial data storage. The declared precision/scale makes storage requirements predictable. `DECIMAL(19, 4)` is a common choice for currency amounts (19 significant digits, 4 decimal places covers common international requirements).

Limitations: arithmetic on declared types produces results with declared-type precision rules, which differ by database — portable SQL numeric arithmetic is not guaranteed to produce identical results across vendors. Division result type is implementation-defined (the most significant portability gap).

### Notes

- `MONEY` in SQL Server (64-bit integer scaled by 10^-4) and `SMALLMONEY` (32-bit, same scale) exist as specialized types but are considered inferior to `DECIMAL` — they do not participate in standard decimal precision propagation rules.
- PostgreSQL `numeric` without bounds (arbitrary precision) is sometimes used for financial calculations requiring more than 38 digits.
- The `CAST`/`CONVERT` of a `DECIMAL` to a narrower type truncates without rounding in some databases (PostgreSQL, Oracle), raises an error in others (SQL Server with ARITHABORT ON).

---

## Python `decimal` Module (IEEE 754 Decimal)

Source: [Python 3 docs — decimal](https://docs.python.org/3/library/decimal.html), [PEP 327 — Decimal Data Type](https://peps.python.org/pep-0327/), [General Decimal Arithmetic specification](https://speleotrove.com/decimal/), [CPython source — _decimal module](https://github.com/python/cpython/tree/main/Modules/_decimal)

### Internal Representation

Python's `decimal.Decimal` is backed by the C library `mpdecimal` (a fork of IBM's `decNumber` library), which implements the General Decimal Arithmetic specification — effectively a software implementation of IEEE 754-2008 decimal arithmetic.

A `Decimal` value is represented as:
```
sign × coefficient × 10^exponent
```
Where:
- `sign`: 0 (positive) or 1 (negative)
- `coefficient`: a non-negative integer (arbitrary precision within context limits)
- `exponent`: a signed integer

The precision limit is determined by the active `Context`, not hardcoded. Default context precision is 28 significant digits. Maximum context precision: 999999999999999999 (essentially unlimited for practical purposes).

Special values: `Decimal('Infinity')`, `Decimal('-Infinity')`, `Decimal('NaN')`, `Decimal('sNaN')` (signaling NaN).

### Arithmetic Precision Propagation

All arithmetic operations use the **active thread-local `Context`**:

```python
import decimal
ctx = decimal.getcontext()
ctx.prec     # default: 28
ctx.rounding # default: ROUND_HALF_EVEN
```

Operations (`+`, `-`, `*`, `/`) all round to `ctx.prec` significant digits using `ctx.rounding`. There is no automatic scale tracking — the precision is always measured in significant digits (not decimal places), consistent with IEEE 754.

`Decimal.quantize(exp, rounding)` is the explicit scale-setting operation:
```python
d = Decimal('3.14159')
d.quantize(Decimal('0.01'), rounding=ROUND_HALF_UP)  # Decimal('3.14')
```

`quantize` raises `InvalidOperation` if the result would require more digits than the context precision.

Source: [decimal.Decimal.quantize](https://docs.python.org/3/library/decimal.html#decimal.Decimal.quantize)

### Overflow Semantics

Python `decimal` uses a **trap-based error handling model** matching the IEEE 754-2008 flag model:

```python
class Context:
    traps: dict[Signal, bool]  # if True, raises exception; if False, sets flag
    flags: dict[Signal, bool]  # sticky status bits, cleared manually
```

| Signal | Default trap | Default result if no trap |
|--------|-------------|--------------------------|
| `InvalidOperation` | Trap (raises exception) | `NaN` |
| `DivisionByZero` | Trap (raises exception) | `Infinity` |
| `Overflow` | Trap (raises exception) | ±`Infinity` |
| `Underflow` | No trap | 0 |
| `Inexact` | No trap | rounded result |
| `Rounded` | No trap | rounded result |
| `Subnormal` | No trap | subnormal |

With the default context (traps on `InvalidOperation`, `DivisionByZero`, `Overflow`), overflow raises `decimal.Overflow`. With traps disabled, the result is ±`Infinity` and the `Overflow` flag is set.

Source: [decimal — Signals](https://docs.python.org/3/library/decimal.html#signals)

### Non-Terminating Division Handling

```python
Decimal('1') / Decimal('3')
# With default context (prec=28): Decimal('0.3333333333333333333333333333')
# The Inexact and Rounded signals are set (but not trapped by default)
```

With `ROUND_UNNECESSARY` rounding mode and a non-terminating division, `InvalidOperation` is raised. Trapping the `Inexact` signal explicitly is also possible:

```python
with decimal.localcontext() as ctx:
    ctx.traps[decimal.Inexact] = True
    Decimal('1') / Decimal('3')  # raises Inexact
```

Source: [decimal — Arithmetic](https://docs.python.org/3/library/decimal.html#decimal-objects)

### Equality and Comparison Semantics

`Decimal('1.0') == Decimal('1.00')` returns `True` — comparison is by mathematical value. The `compare_total()` method implements IEEE 754 `totalOrder`:

```python
Decimal('1.0') == Decimal('1.00')               # True (value equality)
Decimal('1.0').compare_total(Decimal('1.00'))   # Decimal('-1') — 1.0 < 1.00 in total ordering
Decimal('NaN') == Decimal('NaN')                 # False (IEEE 754 NaN ≠ NaN)
```

`compare_total()` distinguishes all representations including NaN, sNaN, and different exponents.

Source: [decimal.Decimal.compare_total](https://docs.python.org/3/library/decimal.html#decimal.Decimal.compare_total)

### Rounding Modes

The Python `decimal` module supports all 8 General Decimal Arithmetic rounding modes:

| Constant | Description |
|----------|-------------|
| `ROUND_UP` | Round away from zero |
| `ROUND_DOWN` | Truncate toward zero |
| `ROUND_CEILING` | Round toward +∞ |
| `ROUND_FLOOR` | Round toward −∞ |
| `ROUND_HALF_UP` | Round to nearest; ties away from zero |
| `ROUND_HALF_DOWN` | Round to nearest; ties toward zero |
| `ROUND_HALF_EVEN` | Banker's rounding — default |
| `ROUND_05UP` | Round toward zero unless last digit would be 0 or 5, then away from zero |

`ROUND_05UP` is unique to the General Decimal Arithmetic specification; it is not present in IEEE 754-2008 itself.

Context is thread-local and can be temporarily overridden:
```python
with decimal.localcontext() as ctx:
    ctx.prec = 50
    ctx.rounding = decimal.ROUND_HALF_UP
    result = Decimal('1') / Decimal('7')  # 50 digits, half-up rounding
```

Source: [decimal — Rounding modes](https://docs.python.org/3/library/decimal.html#rounding-modes)

### Financial Calculation Suitability

Widely used in Python financial applications. The `localcontext()` context manager makes precision and rounding explicit and scoped. The trap system provides Java-like "must be exact" semantics when needed.

Limitations: significantly slower than native `float` (roughly 20–100× for basic arithmetic). The global/thread-local context is a concurrency and composability hazard — library code that modifies the context can interfere with caller code. Thread-safe usage requires disciplined use of `localcontext()`.

Source: [Python decimal FAQ](https://docs.python.org/3/library/decimal.html#decimal-faq)

### Notes

- The CPython `decimal` module since Python 3.3 uses the C accelerator `_decimal` (wrapping `mpdecimal`), making it ~50× faster than the pure-Python implementation.
- `decimal.HAVE_CONTEXTVAR` (True in Python 3.9+) indicates `ContextVar` support for context-per-coroutine — avoids context pollution in async code.
- `Decimal.from_float(0.1)` produces the exact binary float value, not `0.1`. Always prefer `Decimal('0.1')`.

---

## Rust `rust_decimal` Crate

Source: [crates.io — rust_decimal](https://crates.io/crates/rust_decimal), [GitHub — paupino/rust-decimal](https://github.com/paupino/rust-decimal), [rust_decimal docs.rs](https://docs.rs/rust_decimal/latest/rust_decimal/)

### Internal Representation

`rust_decimal::Decimal` uses a 128-bit fixed-point representation deliberately designed to match .NET's `System.Decimal`:

```rust
pub struct Decimal {
    flags: u32,    // bits 16-23: scale (0-28); bit 31: sign
    hi: u32,       // high 32 bits of 96-bit mantissa
    mid: u32,      // middle 32 bits of 96-bit mantissa
    lo: u32,       // low 32 bits of 96-bit mantissa
}
```

The value is: `(-1)^sign × (lo | mid<<32 | hi<<64) × 10^(-scale)`

- Precision: 28–29 significant decimal digits
- Range: ±79,228,162,514,264,337,593,543,950,335
- Minimum positive: 0.0000000000000000000000000001 (10^-28)
- Scale: 0–28

This is the same representational model as `System.Decimal`, enabling round-trip serialization between .NET and Rust systems. The crate provides no special values (no NaN, no Infinity).

Source: [rust_decimal — Decimal struct](https://docs.rs/rust_decimal/latest/rust_decimal/struct.Decimal.html)

### Arithmetic Precision Propagation

Arithmetic propagation follows the same rules as `System.Decimal`:

- **Addition/Subtraction:** aligns scales by scaling up the smaller-scale operand; result scale = `max(scale_a, scale_b)`
- **Multiplication:** result scale = `scale_a + scale_b`; if the intermediate 192-bit product must be reduced to 96 bits, rounding is applied (round half to even internally)
- **Division:** scales the numerator up to maximize significant digits; the result is rounded to fit 28 significant digits

The crate internally uses 128-bit arithmetic (via `u128` or hardware instructions) for the intermediate computation steps, falling back to multi-precision routines for the 96-bit mantissa manipulation.

Source: [rust_decimal — arithmetic implementation](https://github.com/paupino/rust-decimal/blob/master/src/arithmetic.rs)

### Overflow Semantics

`rust_decimal` provides three API surfaces for arithmetic:

**Panicking operators (`+`, `-`, `*`, `/` via `std::ops`):**
```rust
let a = Decimal::MAX;
let b = Decimal::ONE;
let c = a + b;  // panics with "overflow on addition"
```

**Checked operations (return `Option<Decimal>`):**
```rust
let result: Option<Decimal> = a.checked_add(b);  // None on overflow
let result: Option<Decimal> = a.checked_mul(b);  // None on overflow
let result: Option<Decimal> = a.checked_div(b);  // None on divide-by-zero or overflow
let result: Option<Decimal> = a.checked_sub(b);  // None on overflow
```

**Saturating operations:**
```rust
let result: Decimal = a.saturating_add(b);  // Decimal::MAX on overflow
let result: Decimal = a.saturating_mul(b);  // Decimal::MAX on overflow
```

Division by zero returns `None` for `checked_div` and panics for the `/` operator.

Source: [rust_decimal — checked operations](https://docs.rs/rust_decimal/latest/rust_decimal/struct.Decimal.html#method.checked_add)

### Non-Terminating Division Handling

`rust_decimal` handles non-terminating division the same way as `System.Decimal`: the quotient is computed to the maximum representable precision (28–29 significant digits) with rounding (round half to even). No error is raised.

```rust
let one = Decimal::from_str("1").unwrap();
let three = Decimal::from_str("3").unwrap();
let result = one / three;
// result = 0.3333333333333333333333333333 (28 threes)
```

There is no "inexact" flag or exception for non-terminating results.

Source: [rust_decimal — division behavior](https://github.com/paupino/rust-decimal/blob/master/src/arithmetic.rs)

### Equality and Comparison Semantics

`rust_decimal` implements `PartialEq`, `Eq`, `PartialOrd`, `Ord` all by mathematical value:

```rust
let a = Decimal::from_str("1.0").unwrap();
let b = Decimal::from_str("1.00").unwrap();
assert_eq!(a, b);   // passes — value equality
assert_eq!(a.cmp(&b), std::cmp::Ordering::Equal);
```

`Hash` is implemented consistently with `PartialEq` — mathematically equal values hash identically. `Decimal::is_zero()` checks mathematical value (not bit-level zero).

Source: [rust_decimal — Eq and Ord](https://docs.rs/rust_decimal/latest/rust_decimal/struct.Decimal.html)

### Rounding Modes

`rust_decimal` supports rounding via the `RoundingStrategy` enum:

```rust
pub enum RoundingStrategy {
    MidpointNearestEven,    // banker's rounding (default for internal ops)
    MidpointAwayFromZero,   // traditional "round half up"
    ToZero,                 // truncation
    AwayFromZero,           // always round away from zero
    BankersRounding,        // alias for MidpointNearestEven
    ToNegativeInfinity,     // floor
    ToPositiveInfinity,     // ceiling
}
```

Used with: `Decimal::round_dp_with_strategy(dp, strategy)`.

Internal arithmetic rounding during scale reduction uses `MidpointNearestEven` (banker's rounding), matching `System.Decimal`'s behavior.

Source: [rust_decimal — RoundingStrategy](https://docs.rs/rust_decimal/latest/rust_decimal/enum.RoundingStrategy.html)

### Financial Calculation Suitability

Widely used in Rust financial applications. The 28–29 significant digit precision matches `System.Decimal`. The `checked_*` API surface gives Rust code an explicit and recoverable overflow path without exceptions or panics.

Serde support (`serde` feature flag) enables direct serialization to/from JSON, configured to serialize as string (avoiding JSON number precision loss) or as number. Integration with `sqlx` (PostgreSQL driver) and `diesel` ORM enables direct database round-trips with no precision loss.

Limitations: same 28–29 digit precision ceiling as `System.Decimal`; no trap for inexact division; slower than `f64`.

Source: [rust_decimal — serde integration](https://docs.rs/rust_decimal/latest/rust_decimal/#features)

### Notes

- Feature flags: `serde-str` serializes as string (safe for JSON); `serde-float` serializes as float (lossy); `db-postgres`, `db-diesel-postgres` for ORM integration.
- `Decimal::ZERO`, `Decimal::ONE`, `Decimal::MAX`, `Decimal::MIN`, `Decimal::MIN_POSITIVE` are provided as constants.
- The `rust_decimal_macros` crate provides `dec!("1.5")` macro for compile-time decimal literals.
- As of version 1.33, the crate uses Rust's `u128` natively for intermediate arithmetic on 64-bit platforms, improving performance.

---

## Checked Arithmetic Patterns

Source: [Rust Reference — Integer overflow](https://doc.rust-lang.org/reference/expressions/operator-expr.html#overflow), [Ada 2012 Reference Manual §4.5.3](http://ada-auth.org/standards/12rm/html/RM-4-5-3.html), [.NET checked/unchecked — C# Reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/checked), [LLVM — Overflow Intrinsics](https://llvm.org/docs/LangRef.html#overflow-intrinsics)

### Rust: `checked_*` / `saturating_*` / `wrapping_*` / `overflowing_*`

Rust provides four families of integer arithmetic methods that make overflow semantics explicit at the call site:

**`checked_*` — returns `Option<T>`:**
```rust
let a: u32 = u32::MAX;
let result: Option<u32> = a.checked_add(1);      // None
let result: Option<u32> = a.checked_mul(2);      // None
let result: Option<u32> = a.checked_sub(10);     // Some(u32::MAX - 10)
let result: Option<u32> = 10u32.checked_div(0);  // None (divide by zero)
```
Returns `None` if overflow occurs; `Some(result)` if in range. Forces the caller to handle the overflow case at the type level.

**`saturating_*` — clamps to min/max:**
```rust
let result: u32 = u32::MAX.saturating_add(1);  // u32::MAX
let result: i32 = i32::MIN.saturating_sub(1);  // i32::MIN
```
Never overflows; the result is the closest representable value.

**`wrapping_*` — modular (two's complement) arithmetic:**
```rust
let result: u32 = u32::MAX.wrapping_add(1);   // 0
let result: i32 = i32::MAX.wrapping_add(1);   // i32::MIN
```
Explicit opt-in to two's complement modular arithmetic. Used for hash functions, checksums, and cryptographic primitives where wraparound is intentional.

**`overflowing_*` — returns `(result, did_overflow: bool)`:**
```rust
let (result, overflowed): (u32, bool) = u32::MAX.overflowing_add(1);
// result = 0 (wrapped), overflowed = true
```
Used when code needs to detect overflow but also needs the wrapped value (e.g., for carry-based multi-precision arithmetic).

**Debug vs. release build behavior for `+`, `-`, `*`:**
In debug builds, Rust's `+`, `-`, `*` operators panic on overflow by default (`overflow-checks = true` in `Cargo.toml`). In release builds, they **silently wrap** (two's complement). The explicit `checked_*` / `saturating_*` / `wrapping_*` families produce the same behavior in debug and release mode — they are the Rust idiomatic solution to this debug/release inconsistency.

Source: [Rust Reference — Overflow](https://doc.rust-lang.org/reference/expressions/operator-expr.html#overflow); [Rust std::primitive — checked_add](https://doc.rust-lang.org/std/primitive.u32.html#method.checked_add)

### Ada: `Constraint_Error` and Range Types

Ada makes overflow a language-level semantic rather than a library-level convention. Integer and fixed-point types are defined with explicit ranges:

```ada
type Money is delta 0.01 range 0.00 .. 9_999_999.99;
type Percentage is range 0 .. 100;
```

Any assignment or operation that produces a value outside the declared range raises `Constraint_Error` — a predefined exception that propagates up the call stack. This is checked at runtime for subtype assignments, at elaboration time for static values.

Ada fixed-point types (`delta`) are base-10 decimal approximations with guaranteed precision relative to the `delta`. `type Money is delta 0.01` means the type can represent values that are multiples of 0.01 exactly. The compiler determines storage size to hold values in the declared range at the required precision.

**Ada modular types** (for wrapping arithmetic):
```ada
type Byte is mod 256;  -- arithmetic wraps modulo 256; never raises Constraint_Error
```

This is Ada's equivalent of Rust's `wrapping_*` — an explicit type-level declaration of wrapping semantics.

The `Ada.Numerics.Big_Numbers.Big_Integers` package (Ada 2022) provides arbitrary-precision integers with no overflow.

Source: [Ada 2012 RM — Fixed Point Types §3.5.9](http://ada-auth.org/standards/12rm/html/RM-3-5-9.html); [Ada 2012 RM — Constraint_Error §4.5.3](http://ada-auth.org/standards/12rm/html/RM-4-5-3.html)

### .NET `checked` / `unchecked` Context

C# provides `checked` and `unchecked` keywords as statement and expression operators:

```csharp
// checked context: throws OverflowException on overflow
checked
{
    int a = int.MaxValue;
    int b = a + 1;  // throws OverflowException
}

// unchecked context: wraps silently (default for int/long)
unchecked
{
    int a = int.MaxValue;
    int b = a + 1;  // b = int.MinValue, no exception
}

// Expression form:
int c = checked(int.MaxValue + 1);   // throws
int d = unchecked(int.MaxValue + 1); // wraps
```

The default for `int`, `long`, `short`, `sbyte`, `char`, and unsigned variants is **unchecked** (silent wraparound). The `decimal` type is always-checked regardless of context.

**`/checked` compiler flag:** `<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>` in the project file makes all integer arithmetic checked by default. Rarely used in production C# because it can significantly change the behavior of existing code.

**.NET 7+ — `System.Numerics.INumber<T>` and `IBinaryInteger<T>`:** The generic math interfaces expose:
```csharp
static T INumber<T>.CreateChecked<TOther>(TOther value);
static T INumber<T>.CreateSaturating<TOther>(TOther value);
static T INumber<T>.CreateTruncating<TOther>(TOther value);
```
These mirror Rust's `checked_*` / `saturating_*` / `wrapping_*` pattern at the conversion layer.

Source: [C# checked keyword](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/checked); [System.Numerics.INumber](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.inumber-1)

### LLVM Overflow Intrinsics

LLVM IR provides overflow-checking intrinsics used by compiler backends (Rust, C++, Swift):

```llvm
; Returns { result, i1 overflow_flag }
%result = call { i32, i1 } @llvm.sadd.with.overflow.i32(i32 %a, i32 %b)
%result = call { i32, i1 } @llvm.smul.with.overflow.i32(i32 %a, i32 %b)
%result = call { i32, i1 } @llvm.ssub.with.overflow.i32(i32 %a, i32 %b)
; Unsigned variants:
%result = call { i32, i1 } @llvm.uadd.with.overflow.i32(i32 %a, i32 %b)
```

These compile to a single instruction on x86-64 (`ADD` sets the overflow flag `OF` for signed overflow, `CF` for unsigned overflow). The compiler backend reads the flag register to populate the `i1` overflow bit.

Rust's `checked_add` on integers compiles to these intrinsics and is therefore near-zero cost — the check is a single branch on the carry/overflow flag already computed by the ADD instruction.

Source: [LLVM Language Reference — Overflow Intrinsics](https://llvm.org/docs/LangRef.html#overflow-intrinsics)

### Pattern Comparison Table

| Mechanism | Language | On Overflow | Cost | Returns |
|-----------|----------|-------------|------|---------|
| `checked_add()` | Rust | `None` | O(1) — single branch | `Option<T>` |
| `saturating_add()` | Rust | max/min value | O(1) | `T` |
| `wrapping_add()` | Rust | modular wrap | O(1) — no branch | `T` |
| `overflowing_add()` | Rust | wrapped + flag | O(1) | `(T, bool)` |
| `checked { }` | C# | `OverflowException` | O(1) + exception overhead | `T` or exception |
| Range type | Ada | `Constraint_Error` | O(1) + exception overhead | value or exception |
| Default `+` (C#) | C# | silent wrap | O(1) | `T` |
| Default `+` (Rust, release) | Rust | silent wrap | O(1) | `T` |
| Default `+` (Rust, debug) | Rust | panic | O(1) + panic | `T` or panic |
| Default `+` (decimal, C#) | C# | `OverflowException` | O(1) + exception overhead | `T` or exception |
| IEEE 754 default | C/hardware | ±Infinity, flag set | O(1) | float |
| Java `BigDecimal` UNLIMITED | Java | heap OOM only | O(n) | `BigDecimal` |

### Checked Arithmetic as a Type System Feature

The Rust approach treats overflow as a **type-level property of the arithmetic call**, not a runtime condition or global setting. Consequences:

1. **The signature documents the overflow behavior.** A function returning `Option<u32>` from `checked_add` is self-documenting about overflow possibility. A function returning `u32` from `wrapping_add` declares that wrap-around is intended.

2. **Overflow handling is local, not global.** In C#, `checked` is a lexical scope or compiler flag. In Rust, `wrapping_add` and `checked_add` can coexist in the same function body.

3. **`checked_*` is zero-cost abstraction.** The `Option<T>` is returned as a register pair (value + validity bit) with no heap allocation. The LLVM backend eliminates the overhead of the option in successful cases via branch prediction.

4. **No undefined behavior.** Unlike C/C++ where signed integer overflow is undefined behavior (enabling aggressive optimizer assumptions that can produce incorrect code), Rust's debug-mode panic, release-mode wrap, and explicit variants all have defined semantics.

Source: [Rust Nomicon — Overflow](https://doc.rust-lang.org/nomicon/what-unsafe-does.html); [LLVM — with.overflow intrinsics](https://llvm.org/docs/LangRef.html#overflow-intrinsics)

### Notes

- C++'s `__builtin_add_overflow()`, `__builtin_mul_overflow()` (GCC/Clang) expose overflow intrinsics as a compiler extension; C++26 is adding `std::add_overflow`, `std::mul_overflow` to the standard.
- Swift uses `&+`, `&-`, `&*` operators for explicit wrapping arithmetic; default `+`, `-`, `*` trap on overflow in both debug and release builds (unlike Rust).
- Haskell's `Data.Int.Int32` uses modular arithmetic silently; `Data.SafeInt` (a library package) provides checked arithmetic. GHC has no built-in overflow checking — it is a library concern.
- Java primitive `int`/`long` arithmetic always wraps silently (no checked mode). `Math.addExact(int, int)` (Java 8+) throws `ArithmeticException` on overflow and is the Java equivalent of `checked_add`.

---

The full document was prepared as the markdown block above and should be saved to [research/architecture/compiler/exact-decimal-arithmetic-survey.md](research/architecture/compiler/exact-decimal-arithmetic-survey.md). No file-writing tool was available in this session's toolset, so the content is delivered inline. The survey covers all seven requested systems — .NET `System.Decimal`, Java `BigDecimal`, IEEE 754-2008 decimal64/decimal128, SQL `DECIMAL`/`NUMERIC`, Python's `decimal` module, Rust's `rust_decimal` crate, and checked arithmetic patterns across Rust, Ada, C#, and LLVM — with consistent subsections for representation, precision propagation, overflow, non-terminating division, equality, rounding modes, and financial suitability.
