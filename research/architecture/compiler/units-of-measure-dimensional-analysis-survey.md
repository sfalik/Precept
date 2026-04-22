# Units-of-Measure and Dimensional Analysis Type Systems Survey

> Raw research collection. No interpretation, no conclusions, no recommendations.
> Research question: How do real type systems represent compound units, check unit compatibility in arithmetic, compute result units from operator application, handle unit cancellation, and report unit mismatch diagnostics?

---

## F# Units of Measure

Source: Microsoft Learn, "Units of Measure" — https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/units-of-measure; Andrew Kennedy (designer), Microsoft Research Cambridge.

### Unit / Dimension Representation

Units in F# are declared as types annotated with the `[<Measure>]` attribute. Each unit name is a phantom type — it exists solely at the type level and is erased before any code is emitted.

```fsharp
[<Measure>] type kg
[<Measure>] type m
[<Measure>] type s
```

A dimensioned quantity is expressed as a generic primitive type annotated in angle brackets:

```fsharp
let mass : float<kg> = 5.0<kg>
let velocity : float<m/s> = 12.0<m/s>
```

The unit annotation is a *unit formula* — a product of named measures raised to integer powers, written using the operators `*`, `/`, and `^`. Spaces between identifiers inside angle brackets also mean multiplication. The compiler normalizes all unit formulas into a canonical form: exponents sorted alphabetically, negative powers converted to reciprocals, numerator and denominator grouped.

Supported carrier types include `float`, `float32`, `decimal`, all signed and unsigned integer types (`int`, `int64`, `sbyte`, `uint`, etc.), and `nativeint`/`unativeint`. Non-numeric types (e.g., custom classes) can carry unit annotations but do not participate in dimensioned arithmetic.

The SI unit library is available at `FSharp.Data.UnitSystems.SI.UnitSymbols` and `FSharp.Data.UnitSystems.SI.UnitNames`.

### Compound Unit Composition

Compound units are written directly inside the angle bracket annotation:

```fsharp
[<Measure>] type N = kg m / s^2    // derived unit: Newton = kg·m·s⁻²
[<Measure>] type Pa = N / m^2      // Pascal = N·m⁻² = kg·m⁻¹·s⁻²
```

Literal annotations follow the same syntax:

```fsharp
let force : float<kg m / s^2> = 9.81<kg m / s^2>
let pressure : float<N/m^2> = 101325.0<N/m^2>
```

The F# compiler treats both `kg m s^-2` and `m /s s * kg` as the same canonical type `float<kg m/s^2>`. This normalization is purely syntactic/type-level — it allows the compiler to determine type equality for unit expressions written in different equivalent forms.

Generic units are supported through type-level unit variables:

```fsharp
let genericSum (x : float<'u>) (y : float<'u>) = x + y
let velocity : float<m/s> = 3.0<m/s>
let area : float<m^2> = 4.0<m^2>
// genericSum velocity area  // type error: 'm/s ≠ 'm^2
```

### Unit Cancellation Rules

When operands are multiplied or divided, the unit exponents are added or subtracted respectively. If all exponents reach zero, the result is dimensionless (`float<1>`). The compiler computes cancellation entirely at the type level during type-checking.

Example:
```fsharp
let distance : float<m> = 100.0<m>
let time : float<s> = 9.58<s>
let speed : float<m/s> = distance / time   // m / s = m¹ · s⁻¹
```

Cross-cancellation:
```fsharp
let price : float<USD/kg> = 3.50<USD/kg>
let weight : float<kg> = 2.0<kg>
let total : float<USD> = price * weight    // (USD/kg) * kg → USD
```

A conversion constant has units that cancel the source and introduce the target:
```fsharp
let gramsPerKilogram : float<g kg^-1> = 1000.0<g/kg>
let convertGramsToKilograms (x : float<g>) = x / gramsPerKilogram
// float<g> / float<g kg^-1> = float<kg>
```

### Permitted vs. Rejected Operations

**Permitted:**
- Addition and subtraction: only between quantities with the **same** unit type. `float<m> + float<m>` → `float<m>`.
- Multiplication: always permitted; unit exponents add. `float<m> * float<kg>` → `float<m kg>`.
- Division: always permitted; unit exponents subtract. `float<m> / float<s>` → `float<m/s>`.
- Integer and negative-integer powers via `^`.
- Comparison operators (`<`, `>`, `=`, etc.) between same-unit values.
- Passing a `float<'u>` to functions expecting `float<'u>`.

**Rejected (compile error):**
- Addition or subtraction between values with different units: `float<m> + float<s>` is a type error.
- Passing `float<m>` where `float<s>` is expected.
- Assigning `float<m>` to a variable typed as `float<kg>`.

Stripping units requires explicit conversion via `float length` (converts `float<cm>` → `float`) or via `LanguagePrimitives.FloatWithMeasure`. This is an intentional friction point to prevent accidental unit stripping.

### Runtime Erasure vs. Retention

Units are **completely erased at runtime**. The F# compiler removes all measure annotations before IL code generation. A `float<kg>` compiles to the same IL as a `float`. No boxing, no overhead, no runtime representation of the unit. F# documentation explicitly states: "any attempt to implement functionality that depends on checking the units at run time is not possible. For example, implementing a `ToString` function to print out the units is not possible."

### Diagnostic Messages and Error Format

Errors are reported by the F# compiler at the call site where the type mismatch is detected. The error message names the conflicting types using their full unit annotation:

```
error FS0001: Type mismatch. Expecting a
    float<m>
but given a
    float<s>
The unit of measure 'm' does not match the unit of measure 's'
```

Errors include a source location (file, line, column) and display both the expected and actual unit-annotated types. The error is a standard type unification failure, formatted as the normal F# type-mismatch diagnostic with unit-formula strings substituted in place of type arguments.

### Dimensionless Quantity Handling

Dimensionless quantities are represented as `float<1>`. The literal annotation `<1>` explicitly marks a value as dimensionless. A `float<1>` is type-equivalent to a plain `float` from a numeric perspective but is kept distinct by the type system to prevent accidental mixing. Conversion between `float<1>` and `float` is explicit via `float` conversion operator.

### Currency / Domain-Specific Notes

There is no built-in currency support. Currency units can be defined as user-defined measures:
```fsharp
[<Measure>] type USD
[<Measure>] type EUR
[<Measure>] type EUR_per_USD = EUR/USD

let exchangeRate : float<EUR/USD> = 1.08<EUR/USD>
let amount : float<USD> = 100.0<USD>
let euroAmount : float<EUR> = amount * exchangeRate   // USD * (EUR/USD) → EUR
```

No implicit currency conversion is provided; conversion constants must be explicitly provided by the programmer. The type system will track the algebra correctly but will not validate exchange rates against live data or enforce that only economically valid currency combinations are used.

### Notes

- The `[<Measure>]` attribute was introduced in F# 2.0 (2010), designed by Andrew Kennedy based on his earlier theoretical work.
- The `FSharp.UMX` library (community project) uses the same measure type system to tag plain values with semantic labels (e.g., user IDs, timestamps) without physical dimension semantics.
- F# RFC FS-1091 extended unit-of-measure support to all unsigned integer types.
- The canonical form normalization (alphabetical ordering of base units) means that `m/s` and `s^-1 m` are the same type.

---

## Boost.Units (C++)

Source: Boost.Units 1.1.0 documentation — https://www.boost.org/doc/libs/1_84_0/doc/html/boost_units.html; Matthias C. Schabel and Steven Watanabe, Boost Software License.

### Unit / Dimension Representation

Boost.Units represents dimensions as MPL (Boost Metaprogramming Library) typelists where each element is a pair of a **base dimension tag** and a **rational exponent**. Base dimensions are declared using the curiously recurring template pattern (CRTP):

```cpp
struct length_base_dimension : base_dimension<length_base_dimension, 1> {};
struct mass_base_dimension   : base_dimension<mass_base_dimension,   2> {};
struct time_base_dimension   : base_dimension<time_base_dimension,   3> {};
```

Each base dimension is assigned a unique integer ordinal. The library uses these ordinals to sort dimensions within typelists, enabling canonical reduction of compound dimension types.

A **dimension** is a sorted typelist of `dim<BaseDimension, StaticRational>` pairs:

```cpp
typedef make_dimension_list<
    boost::mpl::list<
        dim<mass_base_dimension,   static_rational<1>>,
        dim<length_base_dimension, static_rational<2>>,
        dim<time_base_dimension,   static_rational<-2>>
    >
>::type energy_dimension;
```

A **unit** associates a dimension with a concrete measurement system (e.g., SI). A **quantity** pairs a unit with a value:

```cpp
template<class Unit, class Y = double> class quantity;
```

### Compound Unit Composition

Compound units are formed by multiplying and dividing unit types using template operators. Boost.Units provides operator overloads so that expressions like `si::meter / si::second` produce a new unit type at compile time whose dimension is `length^1 * time^-1`.

The `derived_dimension` convenience template allows declaration without explicit MPL lists:

```cpp
typedef derived_dimension<length_base_dimension, 2>::type area_dimension;
typedef derived_dimension<mass_base_dimension, 1,
                          length_base_dimension, 2,
                          time_base_dimension, -2>::type energy_dimension;
```

Quantities of compound types are created from the product of simpler quantities:

```cpp
quantity<si::length>  d = 3.0 * si::meters;
quantity<si::time>    t = 2.0 * si::seconds;
quantity<si::velocity> v = d / t;  // length/time → velocity dimension
```

The result type of `*` and `/` between quantities is computed at compile time by adding or subtracting the exponent vectors in the dimension typelists.

### Unit Cancellation Rules

When two quantities are multiplied or divided, the library computes the resulting dimension by element-wise addition or subtraction of the rational exponents in the dimension typelists. Elements whose resulting exponent is zero are removed from the reduced dimension. This reduction happens entirely at compile time during template instantiation.

Example: `quantity<si::area>` (L²) divided by `quantity<si::length>` (L¹) produces `quantity<si::length>` (L¹). A product of `quantity<si::force>` (M·L·T⁻²) and `quantity<si::length>` (L) produces `quantity<si::energy>` (M·L²·T⁻²).

When all base-dimension exponents cancel to zero, the result type is `quantity<dimensionless>`, which implicitly converts to the underlying scalar type.

### Permitted vs. Rejected Operations

**Permitted:**
- `+` and `-` between quantities of **identical** dimension and unit system.
- `*` between any two quantities (dimension exponents add).
- `/` between any two quantities (dimension exponents subtract).
- `pow<R>` and `root<R>` for rational powers and roots.
- Boolean comparisons (`==`, `<`, etc.) between same-dimension, same-unit quantities.
- Implicit conversion between quantities of the same dimension if their reduced units are identical.
- Explicit conversion between dimensionally compatible quantities across unit systems.

**Rejected (compile-time error):**
- `+` or `-` between quantities of different dimension — a static assertion fires or a template substitution failure occurs.
- Direct construction of a `quantity<Unit, Y>` from a bare scalar `Y` (safety design; `from_value` static method is provided but bypasses checking).
- Implicit conversion between quantities in different unit systems (e.g., SI meters and CGS centimeters both have `length_dimension` but differ in scale; this requires an explicit conversion).

### Runtime Erasure vs. Retention

Units are erased at runtime with proper compiler optimization. The `quantity<Unit, double>` struct typically reduces to a single `double` in the generated machine code after inlining and optimization. Boost.Units documentation states: "With appropriate compiler optimization, no runtime execution cost is introduced." The dimension and unit information exist only as template parameters during compilation and do not appear in the compiled binary.

### Diagnostic Messages and Error Format

Because Boost.Units is implemented entirely through C++ template metaprogramming, diagnostic messages appear as C++ template instantiation errors. These errors are notoriously verbose. A typical error for adding two incompatible quantities shows deeply nested template instantiation chains involving `mpl::list`, `dim`, `static_rational`, and `make_dimension_list`. The actual semantic content (e.g., "cannot add length and time") is buried within the template parameter dump. Error messages vary significantly across compilers; GCC and Clang both include the full template parameter chain in the error output.

### Dimensionless Quantity Handling

Dimensionless quantities have the type `quantity<dimensionless, Y>`. The `dimensionless` unit corresponds to an empty dimension list (all exponents zero). Boost.Units allows implicit conversion from a `quantity<dimensionless, Y>` to the underlying value type `Y`, enabling dimensionless quantities to interoperate with raw scalars. This implicit conversion is a special-cased template specialization of the `quantity` class.

### Currency / Domain-Specific Notes

Boost.Units provides no built-in currency support. Users can define custom base dimensions for currency and register conversion factors using `BOOST_UNITS_DEFINE_CONVERSION_FACTOR`. The unit algebra would correctly track compound currency types (e.g., USD per kilogram), but no runtime exchange rate lookup or validation is provided.

### Notes

- Boost.Units requires Boost.MPL and is demanding of C++ standard compliance; some older compilers (MSVC 6.0, GCC 3.3.x) are not supported.
- Two unit systems are distinguished: *homogeneous systems* (all quantities stored in one system) and *heterogeneous systems* (quantities may mix systems; conversions tracked automatically).
- `quantity_cast` can access the raw underlying value, bypassing type safety — analogous to `reinterpret_cast`.

---

## Frink Programming Language

Source: Frink language documentation — https://frinklang.org/; Frink data file — https://frinklang.org/frinkdata/units.txt; developed by Alan Eliasen.

### Unit / Dimension Representation

Frink represents all numeric quantities as a value paired with a dimension vector expressed in terms of nine **fundamental dimensions**:

| Quantity | Fundamental Unit | Symbol |
|---|---|---|
| length | meter | m |
| mass | kilogram | kg |
| time | second | s |
| current | ampere | A |
| luminous intensity | candela | cd |
| amount of substance | mole | mol |
| temperature | kelvin | K |
| information | bit | bit |
| currency | U.S. dollar | USD |

Every unit in Frink's data file is recursively defined in terms of these fundamental dimensions. The dimension of any expression is represented as a vector of rational exponents over these nine base dimensions. For example, a velocity is `m s⁻¹` (length¹ · time⁻¹) and power is `kg m² s⁻³` (mass¹ · length² · time⁻³).

This representation is **runtime-based**, not type-level. Dimensions are carried as data alongside each value throughout computation. Frink is a dynamically-typed language and does not perform static type checking.

### Compound Unit Composition

Compound unit expressions are written using whitespace (multiplication), `/` (division), and `^` (exponentiation). Whitespace between any two identifiers or values implies multiplication:

```
// velocity: m/s
12 meters / second

// acceleration: m/s²
9.81 m/s^2

// energy: kg·m²/s²
5 kg m^2/s^2
```

Frink's data file defines thousands of named units as multiples or combinations of fundamental units. Units resolve to dimension vectors at parse/evaluation time. Compound expressions are evaluated by composing dimension vectors algebraically.

### Unit Cancellation Rules

Frink computes dimension cancellation at runtime by adding the exponent vectors of operands:

- Multiplication: exponent vectors are added element-wise.
- Division: exponents of the divisor are subtracted from the dividend's exponent vector.

When an exponent component reaches zero, that dimension is absent from the result. If all components are zero, the result is dimensionless.

Example from documentation:
```
week/day      // both are time; result = 7 (dimensionless)
foot meter    // both length; result = foot·meter = area
```

Frink automatically displays the result's dimension classification alongside the value:
```
1 volt
→  1 m^2 s^-3 kg A^-1 (electric_potential)
```

### Permitted vs. Rejected Operations

**Permitted:**
- Addition and subtraction between quantities of **the same dimension** (e.g., feet + meters = length, handled transparently by converting to common base units).
- Multiplication and division between any quantities.
- Comparison (`<`, `>`, `==`) between quantities of the same dimension.
- The `conforms` operator tests dimensional compatibility: `foot conforms meters` → `true`.
- Conversion with `->` operator: `55 mph -> yards/second` produces a value in the target units if dimensions are compatible.

**Rejected (runtime error):**
- Addition or subtraction between quantities of different dimensions (e.g., `3 meters + 5 seconds`) produces a runtime conformance error.
- Conversion with `->` where dimensions are incompatible produces a "conformance error" with suggestions.

Variable dimension constraints: Variables can be declared with dimension constraints using `is`:
```
var speed is velocity = 60 mph
var mass  is mass     = 5 kg
```
Assigning a value of the wrong dimension to such a variable produces a runtime error.

### Runtime Erasure vs. Retention

Frink **retains units at runtime**. This is the opposite of compile-time systems. Every Frink value carries its full dimension vector during execution. This enables:
- Runtime unit conversion and checking.
- Display of units in output (e.g., `5 kg`).
- The `dimensionsToArray[unit]` function to inspect exponents programmatically.
- The `getExponent[unit, baseUnit]` function to query a specific base-dimension exponent.
- Live currency exchange rate fetching and integration into calculations.

The cost is that all computations carry the metadata overhead of the dimension vector.

### Diagnostic Messages and Error Format

Conformance errors are reported as runtime messages in the form:

```
Conformance error:
  Left side is: 24.5872 m s^-1 (velocity)
  Right side is: 0.9144 m (length)
  Suggestion: multiply left side by time
              or divide left side by frequency
```

The error shows both operand values in base SI dimensions, identifies the named dimension where possible, and offers a textual suggestion for how to reconcile the mismatch. No source location is provided in the message body (the interpreter reports the error at the point of evaluation).

### Dimensionless Quantity Handling

Dimensionless quantities have an empty dimension vector (all exponents zero). Frink displays them as plain numbers. The built-in function `isUnit[expr]` returns `true` for any quantity including dimensionless numbers. Results like `week/day` (both time dimensions) automatically cancel to `7` (a dimensionless integer).

### Currency / Domain-Specific Notes

Currency is a **first-class fundamental dimension** in Frink, with USD as the base unit. The data file and runtime support:

- All major world currencies via ISO 4217 codes (e.g., `EUR`, `JPY`, `GBP`) as named units convertible to USD.
- Live exchange rate fetching from the internet.
- Historical U.S. dollar buying power using CPI data from FRED (St. Louis Federal Reserve), allowing expressions like `1250 dollar_1867 -> dollar` (adjusted for inflation).
- Historical British price data using pre-decimal units (pounds, shillings, pence).
- Compound currency expressions: `(13.99 dollars) / (1750 ml 80 proof) -> "dollars/floz"` naturally produces a unit in `USD · volume⁻¹`.

Currency conversion: `100 USD -> EUR` converts using a live fetched exchange rate. The result's dimension is `EUR` (same dimension as `USD`, just a different scale factor). Cross-currency arithmetic composes naturally within the dimensional algebra.

### Notes

- Frink is implemented in Java and runs on the JVM.
- The `getScale[unit]` function returns the numeric scale of a unit relative to base units, enabling inspection of conversion factors.
- Frink's approach represents dimensional analysis as a runtime system rather than a compile-time type system — the tradeoff between safety and flexibility vs. zero-cost static enforcement.
- The data file (`units.txt`) is the single authoritative source for all unit definitions and is large (~several thousand entries).

---

## Haskell `dimensional` and `units` Packages

Source: Hackage — https://hackage.haskell.org/package/dimensional; https://hackage.haskell.org/package/units; Bjorn Buckwalter (dimensional); Richard Eisenberg, Takayuki Muranushi (units).

### Unit / Dimension Representation

#### `dimensional` (Bjorn Buckwalter)

The `dimensional` package (version 1.x+) uses **Data Kinds** and **Closed Type Families** to represent dimensions at the type level. Each of the 7 SI base dimensions is represented as a type-level integer exponent. A dimension is a 7-tuple of type-level integers `(L, M, T, I, Θ, N, J)` for length, mass, time, current, temperature, amount, and luminous intensity.

The core types are:
- `Quantity d a` — a dimensioned quantity where `d` is the dimension (type-level vector) and `a` is the numeric value type.
- `Unit d a` — a unit with dimension `d` and conversion factor of type `a`.
- Type aliases: `type Length a = Quantity DLength a`, `type Velocity a = Quantity DVelocity a`, etc.

The `*~` operator wraps a raw number with a unit to form a quantity. The `/~` operator extracts a raw number by specifying the target unit.

```haskell
leg :: Length Double
leg = 1 *~ mile

timeOfJourney :: Time Double
timeOfJourney = sum $ fmap (leg /) speeds
```

#### `units` (Richard Eisenberg)

The `units` package takes a more general approach separating **dimensions** from **units**. Dimensions are declared as empty Haskell data types with a `Dimension` typeclass instance. Units are declared similarly with a `Unit` typeclass instance specifying the `BaseUnit` and `conversionRatio`. Quantities are `Qu dim lcsu numtype` where `lcsu` (local coherent system of units) maps dimensions to their storage units.

```haskell
data LengthDim = LengthDim
instance Dimension LengthDim

data Meter = Meter
instance Unit Meter where
  type BaseUnit Meter = Canonical
  type DimOfUnit Meter = LengthDim
```

### Compound Unit Composition

#### `dimensional`

Compound dimensions are constructed via type-level arithmetic on the exponent tuples. The `Numeric.Units.Dimensional` module exports `*~`, `/~`, `(*)`, `(/)` operators that operate on `Quantity` values and produce quantities with appropriately combined dimensions.

```haskell
averageSpeed :: Velocity Double
averageSpeed = _4 * leg / timeOfJourney
-- length / time → velocity
```

#### `units`

The `units` package provides type operators for combining units:
- `:*` — multiply units (sum exponents at type level)
- `:/` — divide units (subtract exponents at type level)
- `:^` — raise to a power (multiply exponents by integer at type level)
- `%*`, `%/`, `%^` — analogous operators for `Qu` quantity types

```haskell
type MetersPerSecond = Meter :/ Second
speed :: MkQu_ULN MetersPerSecond LCSU Double
speed = 20 % (Meter :/ Second)

type Velocity = Length %/ Time  -- same type as above
```

The type family machinery in GHC evaluates these operators at compile time to produce reduced dimension types.

### Unit Cancellation Rules

#### `dimensional`

Cancellation is handled by GHC's type-checker reducing the type-level integer arithmetic on dimension exponents. When numerator and denominator exponents match, they reduce to zero and are removed. The `dimensional` package uses `numtype-dk` to provide type-level integer arithmetic. Closed type families compute `Add`, `Sub`, `Mul` on these integers at compile time.

#### `units`

The `units` package uses a type-level multiset (sorted typelist) to represent dimension vectors. GHC reduces `:*` and `:/` type applications to canonical sorted form during type-checking. The `redim` function performs a compile-time safe cast when two types represent the same underlying dimension but may differ in the order of type-level operations (since `A * B` and `B * A` may produce structurally different but semantically equivalent types).

### Permitted vs. Rejected Operations

**Permitted:**
- Addition and subtraction (`|+|` / `|-|` in `dimensional`; `|+|` in `units`) between quantities of **the same dimension**.
- Multiplication and division producing quantities with the algebraically combined dimension.
- Powers and roots at both type and term level.
- Extraction of raw numeric values by specifying a unit (`/~` in `dimensional`; `#` operator in `units`).

**Rejected (GHC compile-time type error):**
- Adding or subtracting quantities of different dimensions (e.g., `Length + Time`).
- Using a length where a time is expected.
- In `units`: mixing quantities from incompatible LCSUs without explicit conversion.

The `dimensional` documentation notes: "Note that addition and subtraction on units does not make physical sense, so those operations are not provided" (in the context of `Unit` types; operations are provided for `Quantity` types of matching dimension).

### Runtime Erasure vs. Retention

In both packages, dimensions are **erased at runtime**. The `Quantity d a` type is a `newtype` over `a` (or structurally equivalent), so at runtime a `Length Double` is just a `Double`. No dimension vector is stored at runtime. The type-level encoding is a phantom type parameter.

The `Data.Metrology.Unsafe` module in `units` exports the internal constructor, enabling explicit unit coercions that bypass type safety.

### Diagnostic Messages and Error Format

Because both packages use GHC's type system, errors appear as GHC type errors. These can be complex and difficult to read because GHC exposes the internal type-level representation in the error message. For `dimensional`, errors show the concrete `Quantity DLength Double` vs. `Quantity DTime Double` type names. For `units`, errors show deeply nested type family applications.

The `units` documentation explicitly acknowledges this: "The haddock documentation is insufficient for using the units package... `Data.Metrology.Internal`: This module contains mostly-internal definitions that may appear in GHC's error messages." The package exports internal definitions into a top-level module specifically to reduce module-prefix clutter in error messages.

Typical GHC error format (paraphrased):
```
Could not match type 'Qu '[F LengthDim One] MkLCSU ... Double'
                 with 'Qu '[F TimeDim One] MkLCSU ... Double'
```

### Dimensionless Quantity Handling

Both packages define a `Dimensionless` type (all exponents zero). In `dimensional`, `Dimensionless a` is `Quantity DOne a` where `DOne` is the zero-exponent vector. In `units`, `Number` and `Dimensionless` are exported from `Data.Metrology`.

Dimensionless quantities are returned by operations like dividing two same-dimension quantities. They can interoperate with raw numbers in some contexts.

### Currency / Domain-Specific Notes

Neither `dimensional` nor `units` provides built-in currency support. The `dimensional` package is focused on the SI base dimensions (7 physical dimensions). The `units` package is extensible to any domain: users can define non-physical dimensions (e.g., currency, pixels) by declaring custom dimension types and unit types. No currency conversion logic is provided.

### Notes

- `dimensional` version 1.0+ requires GHC 8.0 or later and uses Closed Type Families; earlier versions used functional dependencies.
- `units` requires `singletons` and `template-haskell` for its machinery; it generates code via Template Haskell to reduce boilerplate.
- Both packages have seen limited adoption relative to the F# built-in due to the complexity of use and verbosity of GHC error messages.
- The `units-defs` companion package to `units` provides pre-built SI units and sets up `DefaultUnitOfDim` instances for monomorphic use.

---

## Rust `uom` Crate

Source: docs.rs/uom — https://docs.rs/uom/latest/uom/; crate version 0.38.0.

### Unit / Dimension Representation

`uom` (Units of Measurement) implements compile-time dimensional analysis in Rust through a combination of generics, associated types, and macros. The central design principle is to work with **quantities** (length, mass, velocity) rather than measurement units (meter, foot) as the primary abstraction.

Quantities are represented as a generic struct `Quantity<D, U, V>` where:
- `D` is a dimension type (encoding the exponents of base dimensions)
- `U` is a unit system (e.g., SI)
- `V` is the value storage type (e.g., `f64`, `f32`, `i32`)

The SI system is defined using three macros: `system!` (declares the system of quantities), `quantity!` (declares each quantity and its units), and `unit!` (declares individual measurement units). These macros generate the type infrastructure.

Convenience type aliases are generated, e.g.:
```rust
use uom::si::f64::*;  // imports Length, Mass, Velocity, etc. as f64-based types
```

Internally, values are **normalized to base units** (e.g., all lengths stored as meters regardless of input unit). Creation specifies the input unit:
```rust
let length = Length::new::<kilometer>(5.0);   // stored as 5000.0 meters
let time   = Time::new::<second>(15.0);
```

### Compound Unit Composition

Compound quantities are produced by arithmetic operations on `Quantity` values. The Rust type system computes the result type at compile time by combining the dimension parameters:

```rust
let velocity: Velocity = length / time;        // Length / Time → Velocity
let acceleration: Acceleration = velocity / time;  // Velocity / Time → Acceleration
```

The `ISQ!` macro implements type aliases for a specific system and storage type. Dimensional exponents are encoded as `typenum` integers (a Rust library for type-level integers) in the struct's type parameters.

Custom systems can be defined by executing `system!` with a list of base quantities:
```
system! {
    quantities: ISQ {
        length: meter, L;
        mass:   kilogram, M;
        time:   second, T;
    }
    units: SI {
        mod length::Length;
        mod mass::Mass;
        mod time::Time;
    }
}
```

### Unit Cancellation Rules

Cancellation is handled by Rust's type checker through `typenum` arithmetic. The dimension parameters are type-level integers (from the `typenum` crate) that the compiler evaluates statically. When exponents sum or subtract to zero, the corresponding dimension is eliminated from the result type. If all dimensions cancel, the result is a dimensionless `Quantity`.

The `autoconvert` feature flag controls whether base-unit conversion is applied automatically in binary operations between quantities with the same dimension but different base units.

### Permitted vs. Rejected Operations

**Permitted:**
- Addition and subtraction between `Quantity` values of the same dimension type.
- Multiplication and division between any `Quantity` values (result type computed from combined dimensions).
- Powers and roots via `typenum` arithmetic at type level.
- `get::<unit>()` to extract the numeric value in a specific unit.

**Rejected (compile-time error):**
- Adding or subtracting quantities of different dimensions:
  ```rust
  let error = length + time; // error[E0308]: mismatched types
  ```
  The compiler emits `error[E0308]: mismatched types` indicating the concrete incompatible `Quantity` types.

### Runtime Erasure vs. Retention

Units are erased at runtime. `uom` documentation: "operations on quantities (+, -, *, /, …) have zero runtime cost over using the raw storage type (e.g. `f32`)." The `Quantity<D, U, V>` struct reduces at runtime to just its stored numeric value. No dimension metadata is carried at runtime.

The design normalizes to base units at construction time. The conversion factor is applied once when the value is created (e.g., `Length::new::<kilometer>(5.0)` stores `5000.0`). Subsequent operations are purely numeric.

### Diagnostic Messages and Error Format

Rust's type errors for `uom` are somewhat more readable than Boost.Units or Haskell `units` errors because Rust error messages tend to be well-formatted. However, the concrete type names are verbose. A mismatch between `Quantity<dyn Dimension<...>, SI, f64>` and another dimension produces output showing the full generic parameter chain. The error code `E0308` (mismatched types) is the standard Rust type mismatch error; there is no `uom`-specific error code.

### Dimensionless Quantity Handling

Dimensionless quantities are handled as quantities where all dimension exponents are zero. Specific handling depends on the system definition. The ISQ system includes a `Ratio` (dimensionless) quantity. Extraction of a raw value from a dimensionless quantity is straightforward.

### Currency / Domain-Specific Notes

`uom` has no built-in currency support. Custom quantity systems can be defined to include currency dimensions, but no conversion or exchange rate infrastructure is provided.

### Notes

- `uom` requires Rust 1.68.0 or later.
- The `autoconvert` feature is critical for correct behavior with non-floating-point integer storage types; the documentation notes it exists "to account for compiler limitations where zero-cost code is not generated for non-floating point underlying storage types."
- The `no_std` mode is supported for embedded targets.
- Values of integer types may not be able to represent all sub-base-unit quantities (e.g., `i32` length in meters cannot represent 1 centimeter = 0.01 meters).

---

## JSR 354 (Java Money API)

Source: JavaMoney project — https://javamoney.github.io/; JSR 354 specification — https://jcp.org/en/jsr/detail?id=354; Reference implementation: Moneta.

### Unit / Dimension Representation

JSR 354 defines a Java API for monetary amounts and currencies. It does not implement a general-purpose dimensional analysis type system. The core types are:

- `CurrencyUnit` (interface): represents a currency identified by its ISO 4217 alphabetic code (e.g., `"USD"`, `"EUR"`), numeric code, and default fraction digits.
- `MonetaryAmount` (interface): represents a monetary amount pairing a `CurrencyUnit` with a numeric value. The interface exposes `getCurrency()`, `getNumber()`, and arithmetic methods.
- `NumberValue` (class): wraps a numeric value with metadata about precision and number class.

The Moneta reference implementation provides concrete classes `Money` (backed by `BigDecimal`) and `FastMoney` (backed by `long` scaled to fixed precision).

### Compound Unit Composition

JSR 354 is **not** a general dimensional analysis system. There is no facility for compound unit types like `USD/kg` or `USD·item⁻¹` as first-class types in the API. All `MonetaryAmount` values hold a single `CurrencyUnit`. Ratios and compound monetary quantities must be managed manually at the application level.

The API focuses on single-currency arithmetic: adding two dollar amounts, rounding to a target fraction digit count, applying a percentage, etc.

### Unit Cancellation Rules

Not applicable in the general-dimensional-analysis sense. The API defines two semantic categories of arithmetic:

1. **Same-currency operations**: `add`, `subtract` — require both operands to have the same `CurrencyUnit`.
2. **Scalar operations**: `multiply(Number)`, `divide(Number)` — multiply or divide a `MonetaryAmount` by a dimensionless number, producing a `MonetaryAmount` in the same currency.

There is no unit cancellation in the dimensional-analysis sense. `multiply(MonetaryAmount)` is not defined in the core API (multiplying two monetary amounts would produce a currency-squared, which has no monetary meaning in the API's model).

### Permitted vs. Rejected Operations

**Permitted:**
- `amount.add(other)` — requires `amount.getCurrency().equals(other.getCurrency())`.
- `amount.subtract(other)` — same currency required.
- `amount.multiply(long)`, `amount.multiply(double)`, `amount.multiply(Number)` — scales the amount by a dimensionless number.
- `amount.divide(long)`, `amount.divide(double)`, `amount.divide(Number)` — divides amount by dimensionless number.
- `amount.remainder(long/double)` — remainder after division.
- `amount.negate()` — negation.
- `amount.abs()` — absolute value.
- `amount.stripTrailingZeros()` — precision adjustment.
- Comparison operations: `isGreaterThan(other)`, `isLessThan(other)`, `isEqualTo(other)` — require same currency.

**Rejected (runtime exception):**
- `add` or `subtract` between amounts of different currencies: throws `MonetaryException` (or implementation-specific exception). There is **no compile-time checking** — mismatches are detected at runtime.
- Converting between currencies directly through arithmetic is not supported; the `javax.money.convert` package provides `CurrencyConversion` as a `MonetaryOperator` applied through `amount.with(conversion)`.

### Runtime Erasure vs. Retention

Currency information is **retained at runtime**. `MonetaryAmount.getCurrency()` returns the `CurrencyUnit` at any time. The JSR 354 model is entirely runtime-based — there is no type-level currency parameterization in Java's type system. `MonetaryAmount` is the same Java type regardless of what currency it holds. Type-level currency checking (as in F# `float<USD>`) is not part of the specification.

### Diagnostic Messages and Error Format

Errors are Java exceptions thrown at runtime:
- `MonetaryException` is the general exception class for API violations.
- Adding or subtracting mismatched currencies typically throws `MonetaryException` with a message like `"Currency mismatch: EUR and USD"` (exact message is implementation-specific).
- No source location beyond the standard Java stack trace.

### Dimensionless Quantity Handling

JSR 354 has no concept of dimensionless quantities. All amounts must have a currency. Dimensionless ratios (e.g., an exchange rate) are represented as plain `Number` values (used in `multiply`/`divide` operations) rather than as `MonetaryAmount`.

### Currency / Domain-Specific Notes

JSR 354 is entirely currency-specific. Key design decisions:

- **Currency identity**: currencies are compared by their `CurrencyUnit`. ISO 4217 codes are the primary identifier.
- **No cross-currency arithmetic**: the API deliberately does not define `add(MonetaryAmount, MonetaryAmount)` across currencies. Currency conversion must be applied explicitly via `CurrencyConversion`.
- **Exchange rate model**: the `javax.money.convert` package defines `ExchangeRate`, `ExchangeRateProvider`, and `CurrencyConversion`. An `ExchangeRate` from EUR to USD has a `factor` (a `NumberValue`) representing the rate. Applying `ExchangeRate` to a `MonetaryAmount` produces a new `MonetaryAmount` in the target currency.
- **Precision handling**: `MonetaryRounding` provides rounding operations appropriate to the currency's fraction digits.
- **Immutability**: `MonetaryAmount` implementations are required to be immutable.

### Notes

- JSR 354 was finalized as Java Specification Request 354 and is not part of the Java SE standard library; it is available as an external dependency (`javax.money:money-api`).
- The Moneta reference implementation is the most widely used. Other implementations exist for specific domains.
- JavaMoney libraries (above the API) provide currency conversion via ECB (European Central Bank) rates and historical rate sources.
- Unlike F#, Haskell, or Rust approaches, JSR 354 performs **no static dimensional analysis**; it is a well-typed API for currency arithmetic with runtime enforcement only.

---

## Andrew Kennedy — "Types for Units-of-Measure: Theory and Practice" (1997/2009)

Source: Kennedy, Andrew J. "Programming Languages and Dimensions." PhD thesis, University of Cambridge, 1997; Kennedy, Andrew. "Types for Units-of-Measure: Theory and Practice." LNCS 6299 (Central European Functional Programming School 2009). doi:10.1007/978-3-642-17685-2_8.

### Unit / Dimension Representation

Kennedy's theory models units of measure as elements of a **free abelian group**. The group is generated by the set of base unit names, with the group operation being product and the identity element being the dimensionless unit `1`. Every compound unit is a product of base units raised to integer powers:

$$u = u_1^{n_1} \cdot u_2^{n_2} \cdots u_k^{n_k}, \quad n_i \in \mathbb{Z}$$

The key algebraic facts:
- **Commutativity**: $u \cdot v = v \cdot u$ (meter·second = second·meter)
- **Associativity**: $(u \cdot v) \cdot w = u \cdot (v \cdot w)$
- **Inverses**: every unit $u$ has an inverse $u^{-1}$ such that $u \cdot u^{-1} = 1$
- **Identity**: $1 \cdot u = u$

In Kennedy's original ML-family language, units are phantom type parameters on a numeric base type. The type `float<u>` means a floating-point number with unit `u`. The representation in the compiler is a finite map from base unit names to their integer exponents.

**Extension to rational exponents**: The 2009 paper extends the algebra from integer exponents $\mathbb{Z}$ to rational exponents $\mathbb{Q}$, enabling expressions like square roots of units (`sqrt(m²/s²) : float<m/s>`).

### Compound Unit Composition

Compound units are represented as the formal product of base units with exponents. In the type system:

- The product type `float<u·v>` is the type of a value with unit $u \cdot v$.
- The quotient type `float<u/v>` is `float<u·v⁻¹>`.
- The power type `float<u^n>` represents unit $u^n$.

In Kennedy's formalism, a **unit type** is a ground unit expression (no free unit variables) or a **unit scheme** (universally quantified over unit variables):

$$\forall u_1, u_2 \cdots \text{ unit}. \;\tau$$

This allows unit-polymorphic functions like `fun scale (x : float<'u>) (k : float<1>) = k * x` which works for any unit `'u`.

### Unit Cancellation Rules

Cancellation follows directly from the abelian group axioms. Formally:

- `float<u> * float<v>` has type `float<u·v>` (exponent addition)
- `float<u> / float<v>` has type `float<u·v⁻¹>` (exponent subtraction)
- When $u = v$, then `float<u> / float<v>` has type `float<1>` (dimensionless)

Kennedy's type inference algorithm for unit unification is based on **unification in free abelian groups** (also called **exponential unification** in the literature). This is decidable and has efficient algorithms. The principal type theorem holds: every well-typed term has a unique most-general type.

A critical property: **unit inference is not generally decidable for Hindley-Milner type inference extended with units** unless restricted. Kennedy's system restricts inference to avoid undecidability by imposing constraints on where unit variables may appear. The 2009 paper discusses the boundary carefully.

### Permitted vs. Rejected Operations

The theory defines:

- **Permitted**: `+` and `-` between `float<u>` operands of the **same unit** `u`. Multiplication and division between operands of any units (result type computed by unit algebra). All monomorphic numeric operations on `float<1>` (dimensionless).
- **Rejected**: `+` or `-` between `float<u>` and `float<v>` when `u ≠ v` (in reduced form). The type checker generates a unification failure.

The 1997 thesis proves:
- **Soundness**: well-typed programs never produce unit mismatches at runtime.
- **Completeness**: the type inference algorithm finds a principal type for every well-typed program.
- **Principal type theorem**: every well-typed expression has a most general type.

### Runtime Erasure vs. Retention

Kennedy's design mandates **erasure**. Unit annotations exist solely in the type system. The compiled representation of `float<kg>` is identical to `float`. No runtime metadata, no boxing overhead. The motivation is that unit checking should have zero performance cost — units are a compile-time verification tool, not a runtime value.

### Diagnostic Messages and Error Format

Kennedy's theory-level description focuses on the nature of type errors rather than their user-facing format. A unit mismatch during type unification produces a type error reporting the conflicting unit types. In the implemented F# compiler (which directly instantiates Kennedy's theory), the error messages show the two conflicting unit expressions at the location of the mismatch.

The 2009 paper discusses that the principal type of an expression may involve unit variables, and that the diagnostic must show the constraint that cannot be satisfied (e.g., attempting to unify $u$ with $u^{-1}$ is impossible except when both are `1`).

### Dimensionless Quantity Handling

The identity element of the free abelian group is the dimensionless unit `1`. A dimensionless quantity has type `float<1>`. Kennedy's system distinguishes `float<1>` from `float` (the unparameterized type) to preserve type-level tracking. Conversion between `float<1>` and `float` requires an explicit operation.

The theory proves that the dimensionless type is the unique identity: for all units $u$, $u \cdot 1 = u$.

### Currency / Domain-Specific Notes

Kennedy's original papers do not specifically address currency. However, the theory is fully general: currency names (USD, EUR) can be added as additional base generators of the free abelian group alongside physical dimensions. Exchange rates would then be conversion constants with compound units (e.g., `EUR/USD : float<EUR/USD>`).

The 2009 paper discusses that base units are **incommensurable** by default — the type system does not know that EUR and USD are related, only that they are distinct generators. A conversion constant `rate : float<EUR/USD>` must be provided explicitly, and the type system verifies that multiplying `float<USD>` by `float<EUR/USD>` yields `float<EUR>`.

### Notes

- Kennedy's original 1997 work was titled "Programming Languages and Dimensions" and was his Cambridge PhD thesis. The formalization of the free abelian group model appeared in the 1997 ESOP paper "Relational Parametricity and Units of Measure."
- The unit type algebra in Kennedy's theory is equivalent to the theory of **Z-modules** (modules over the integers), which is the algebraic structure underlying the group of characters of a free group.
- The 2009 LNCS paper (Central European Functional Programming School) is the most accessible reference, covering both the formal theory and the practical F# implementation.
- Kennedy's system supports **unit-polymorphic generics** — functions quantified over unit variables, enabling code like `map : (float<'u> → float<'v>) → float<'u> list → float<'v> list` that works for any pair of units.
- The key decidability result: unit inference is decidable because the unification problem for free abelian groups is decidable (unlike the unification problem for general term algebras, which may not be). This is the theoretical basis for the F# implementation's tractability.
- Kennedy notes in the 2009 paper that extending the system to rational exponents (for square roots, cube roots) changes the algebraic structure to **Q-modules** (modules over the rationals), which remains decidable.
