# Source mirror — cross-unit conversion arithmetic survey

Verbatim excerpts captured 2026-05-31 for `research/language/expressiveness/cross-unit-conversion-arithmetic-survey.md`.
Each block records the source, stable identifier, access date, and grade. These defend the survey's
load-bearing claims against URL rot.

---

## Pint — non-multiplicative / offset units

**Source:** Pint documentation, "Temperature conversion" / non-multiplicative units page
(https://pint.readthedocs.io/en/stable/user/nonmult.html), accessed 2026-05-31. Grade: **Secondary**
(authoritative library docs, public versioning).

> "multiplication, division and exponentiation of quantities with offset units is problematic just
> like addition."

> "the addition of quantities with offset units is ambiguous, e.g. for *10 degC + 100 degC* two
> different result are reasonable depending on the context."

> "Pint (since version 0.6) will by default raise an error when a quantity with offset unit is used in
> these operations."

> "OffsetUnitCalculusError: Ambiguous operation with offset unit (degC)."

> "quantities with offset units cannot be created like other quantities by multiplication of magnitude
> and unit but have to be explicitly created."

> "As an alternative to raising an error, pint can be configured to work more relaxed via setting the
> UnitRegistry parameter *autoconvert_offset_to_baseunit* to true." — "pint will convert the quantities
> with offset units automatically to the corresponding base unit before performing the operation."

> "The parser knows about *delta* units and uses them when a temperature unit is found in a
> multiplicative context."

---

## Pint — float-based conversion factors lose precision (inexactness)

**Source:** hgrecco/pint GitHub issue #201, "Conversions for quantities with Decimal units yield
imprecise results" (https://github.com/hgrecco/pint/issues/201), accessed 2026-05-31. Grade:
**Tertiary** (issue thread; corroborated by docs `non_int_type=Decimal` option, which is Secondary).

> Issue title: "Conversions for quantities with Decimal units yield imprecise results #201"

Reported behavior (verbatim values from the issue):

> Input: `Decimal(1835008) * ureg.bytes` — Converting to mebibytes yields
> `Quantity(1.749999999999541248, 'mebibyte')` — Expected precise result: `Decimal('1.75')`.

Root cause as described: Pint computes the conversion factor as a `float` internally and only converts
to `Decimal` afterward, so float rounding is baked in before the decimal multiply. Pint exposes a
`non_int_type` registry parameter (settable to `Decimal`/`Fraction`) to mitigate, but the factor
computation path is the precision-loss source.

---

## Indriya (JSR-385 reference implementation) — exact rational conversions

**Source:** `RationalConverter` class Javadoc, unitsofmeasurement/indriya, master branch
(https://github.com/unitsofmeasurement/indriya/blob/master/src/main/java/tech/units/indriya/function/RationalConverter.java),
accessed 2026-05-31. Grade: **Primary** (authoritative reference-implementation source, public
versioning).

> "This class represents a converter multiplying numeric values by an exact scaling factor (represented
> as the quotient of two `BigInteger` numbers)."

Corroborating (Secondary, search-surfaced): Indriya stores dividend/divisor after cancelling common
factors and performs the final conversion via `BigDecimal` division with a `MathContext` — exact
rational arithmetic rather than `double`. (RationalConverter Javadoc / source, indriya 2.1.x.)

---

## GNU units — nonlinear/temperature units require functional notation (affine)

**Source:** GNU `units` manual, "Temperature Conversions"
(https://www.gnu.org/software/units/manual/html_node/Temperature-Conversions.html), accessed
2026-05-31. Grade: **Primary** (official GNU manual, versioned).

> "Conversions between temperatures are different from linear conversions between temperature
> *increments*."

> "The absolute temperature conversions are handled by units starting with 'temp', and you must use
> functional notation."

> "The temperature-increment conversions are done using units starting with 'deg' and they do not
> require functional notation."

> "Think of 'tempF(x)' not as a function but as a notation that indicates that x should have units of
> 'tempF' attached to it."

> "The 'tempC()' and 'tempF()' definitions are limited to positive absolute temperatures, and giving a
> value that would result in a negative absolute temperature generates an error message."

Example commands (verbatim): `You have: tempF(45)` / `You want: tempC` → `7.2222222`; increment form
`45 degF` → `degC` is a plain linear conversion.

**Nonlinear unit definition shape** (GNU `units` manual, "Defining Nonlinear Units", same access date,
Primary): a nonlinear unit is "a unit name, a formal parameter name, two functions" (forward + inverse)
plus optional `units=[domain;range]` and domain/range specs — i.e. a *function pair*, not a
multiplicative factor. `units=[1;K]` means tempF takes a dimensionless input and the inverse yields a
unit conformable with K.

---

## Boost.Units — absolute (affine) temperature wrapper

**Source:** Boost.Units 1.84 documentation, "Absolute and Relative Temperature Example"
(https://www.boost.org/doc/libs/1_84_0/doc/html/boost_units/Examples.html), accessed 2026-05-31.
Grade: **Secondary** (authoritative library docs).

> "it is important to be able to differentiate between an absolute temperature measurement and a
> measurement of temperature difference."

> "This issue touches on some surprisingly deep mathematical concepts (see Wikipedia for a basic
> review)" — i.e. affine-space mathematics.

Boost.Units wraps absolute temperatures in `quantity<absolute<fahrenheit::temperature>>` (displayed
`{ 32 } F`) distinct from the relative `quantity<fahrenheit::temperature>` (displayed `[ 32 ] F`). The
`absolute<>` wrapper defines `+`/`-` against relative quantities (point ± vector) but does not admit a
point being multiplied.

---

## mp-units (C++) — affine restriction stated explicitly

**Source:** mp-units documentation, "The Affine Space"
(https://mpusz.github.io/mp-units/latest/users_guide/framework_basics/the_affine_space/), accessed
2026-05-31. Grade: **Secondary** (authoritative library docs; mp-units is a C++ standardization
proposal reference, P3045).

> "The multiply syntax support is disabled for units that provide a point origin in their definition
> (i.e., units of temperature like `K`, `deg_C`, and `deg_F`)."

Allowed affine operations (verbatim):

> "*point* - *point* -> *vector*" ; "*point* + *vector* -> *point*" ; "*vector* + *point* -> *point*"
> ; "*point* - *vector* -> *point*"

Prohibited (verbatim):

> "It is not possible to: add two *points*, subtract a *point* from a *vector*, multiply nor divide
> *points* with anything else."

---

## F# units of measure — compile-time-only, units erased at runtime

**Source:** Microsoft Learn, "Units of Measure"
(https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/units-of-measure); captured in
`research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md` (F# section). Grade:
**Primary** (official language reference). Re-cited, not re-fetched.

> "any attempt to implement functionality that depends on checking the units at run time is not
> possible. For example, implementing a `ToString` function to print out the units is not possible."

And (same survey, F# cross-cancellation / conversion-constant section): converting `g → kg` requires the
programmer to supply an explicit conversion constant `gramsPerKilogram : float<g/kg> = 1000.0<g/kg>`;
F# "will not validate exchange rates against live data" and provides "no implicit currency conversion."
The unit tags are erased before IL generation — `float<kg>` compiles to the same IL as `float`.

---

## Rust uom — autoconvert feature flag, base-unit normalization

**Source:** docs.rs/uom (crate 0.38.x); captured in
`research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md` (uom section). Grade:
**Secondary**. Re-cited, not re-fetched.

> "operations on quantities (+, -, *, /, …) have zero runtime cost over using the raw storage type
> (e.g. `f32`)."

uom normalizes to base units at construction (`Length::new::<kilometer>(5.0)` stores `5000.0` m). The
`autoconvert` feature "exists to account for compiler limitations where zero-cost code is not generated
for non-floating point underlying storage types" — i.e. cross-scale auto-conversion between same-dimension
units IS performed, gated by the `autoconvert` flag (on by default).
