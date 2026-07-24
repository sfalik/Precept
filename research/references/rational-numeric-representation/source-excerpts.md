# Source excerpts — rational vs decimal exact numeric representation

Snapshots of the load-bearing verbatim excerpts used in
`research/language/rational-vs-decimal-exact-numeric-representation.md`.
All fetched 2026-07-19 via WebFetch. Excerpts are reproduced as returned; where the
fetch tool summarized rather than quoted, that is noted. See the parent doc's
Threats to Validity for the WebFetch-summarization caveat.

---

## Python — `fractions.Fraction` (Primary)
Source: <https://docs.python.org/3/library/fractions.html> (Python 3 stdlib docs)

- "A Fraction instance can be constructed from a pair of rational numbers, from a single number, or from a string."
- "The first version requires that _numerator_ and _denominator_ are instances of numbers.Rational and returns a new Fraction instance with a value equal to numerator/denominator."
- "Fraction instances are hashable, and should be treated as immutable."
- "Numerator of the Fraction in lowest term." / "Denominator of the Fraction in lowest terms. Guaranteed to be positive."
- Example: `>>> Fraction(16, -10)` → `Fraction(-8, 5)`

## Ruby — `Rational` (Primary)
Source: <https://ruby-doc.org/core/Rational.html>

- "A rational number can be represented as a pair of integer numbers: a/b (b>0), where a is the numerator and b is the denominator."
- "A rational object is an exact number, which helps you to write programs without any rounding errors."
- Literal suffix examples: `2/3r  #=> (2/3)`, `(1/2r).abs #=> (1/2)`
- Reduction: `Rational(4, -6)  #=> (-2/3) # Reduced.`
- Exactness demo: `10.times.inject(0) {|t| t + Rational('0.1') } #=> (1/1)` vs float `#=> 0.9999999999999999`

## Julia — `Rational` (Primary)
Source: <https://docs.julialang.org/en/v1/manual/complex-and-rational-numbers/>

- "Julia has a rational number type to represent exact ratios of integers."
- "Rationals are constructed using the `//` operator" (e.g. `2//3`).
- "If the numerator and denominator of a rational have common factors, they are reduced to lowest terms such that the denominator is non-negative."
- "The promotion system makes interactions with other numeric types effortless" (e.g. `3//5 + 1`).
- (Doc does not discuss numerator/denominator overflow.)

## Haskell — `Data.Ratio` (Primary)
Source: <https://hackage-content.haskell.org/package/base-4.22.0.0/docs/Data-Ratio.html>

- "type Rational = Ratio Integer"
- `%`: "Forms the ratio of two integral numbers."
- numerator: "Extract the numerator of the ratio in reduced form: the numerator and denominator have no common factor and the denominator is positive."
- denominator: analogous, "the denominator is positive."

## Raku — `Rat` / `FatRat` (Primary) — VERIFIES the denominator-ceiling fallback
Source: <https://docs.raku.org/type/Rat>

- "Number literals with a dot but without exponent produce `Rat`s."
- "On overflow of the denominator during an arithmetic operation a `Num` (floating-point number) is returned instead."
- "If you want arbitrary precision arithmetic with rational numbers, use the `FatRat` type instead."

## Racket — numeric tower (Primary)
Source: <https://docs.racket-lang.org/reference/numbers.html>

- "The precision and size of exact numbers is limited only by available memory (and the precision of operations that can produce irrational numbers). In particular, adding, multiplying, subtracting, and dividing exact numbers always produces an exact result."
- "Unless otherwise specified, computations that involve an inexact number produce inexact results."
- "Inexact numbers can be coerced to exact form, except for the inexact numbers +inf.0 … +nan.f, which have no exact form."

## Scheme R5RS — §6.2 Numbers, exact/inexact (Primary)
Source: <https://conservatory.scheme.org/schemers/Documents/Standards/R5RS/HTML/r5rs-Z-H-9.html>
(R7RS §6.2 inherits materially identical exactness language.)

- §6.2.2: "A number is exact if it was written as an exact constant or was derived from exact numbers using only exact operations."
- §6.2.2: "A number is inexact if it was written as an inexact constant, if it was derived using inexact ingredients, or if it was derived using inexact operations."
- §6.2.2: "Rational operations such as + should always produce exact results when given exact arguments."
- §6.2.5: "Exact->inexact returns an inexact representation of z. The value returned is the inexact number that is numerically closest to the argument."
- §6.2.5: "Inexact->exact returns an exact representation of z."
- sqrt: "It is desirable (but not required) for potentially inexact operations such as sqrt, when applied to exact arguments, to produce exact answers whenever possible (for example the square root of an exact 4 ought to be an exact 2)."

## Common Lisp — HyperSpec `ratio` type (Primary)
Source: <http://www.lispworks.com/documentation/HyperSpec/Body/t_ratio.htm>

- "A ratio is a number representing the mathematical ratio of two non-zero integers, the numerator and denominator, whose greatest common divisor is one, and of which the denominator is positive and greater than one."

## Clojure — `Ratio` (Primary)
Source: <https://clojure.org/reference/data_structures>

- "Represents a ratio between integers. Division of integers that can't be reduced to an integer yields a ratio, i.e. 22/7 = 22/7, rather than a floating point or truncated value."
- "Clojure also supports the Java boxed number types … including BigInteger and BigDecimal, plus its own Ratio type."

## Wolfram Language — exact & approximate results (Secondary — vendor docs)
Source: <https://reference.wolfram.com/language/tutorial/ExactAndApproximateResults.html>

- "This is taken to be an exact rational number, and reduced to its lowest terms: 452 / 62"
- "You can tell the Wolfram Language to give you an approximate numerical result … by ending your input with //N."
- "Whenever you give a number with an explicit decimal point, the Wolfram Language produces an approximate numerical result."
- Principle (fetch-summarized): Wolfram preserves exact symbolic forms (integers, rationals, and unevaluated expressions like `Sqrt[2]`) unless N[] or a decimal point requests approximation.

## SMT-LIB — theory of Reals (Primary) — solvers reason over exact rationals
Source: <https://smt-lib.org/theories-Reals.shtml>

- Real sort interpreted as "the set of all real numbers".
- Division: "/ as a total function that coincides with the real division function for all inputs x and y where y is non-zero"; division by zero: "terms of the form (/ t 0) are meaningful in every instance of Reals. However, the declaration imposes no constraints on their value."
- Values (fetch-summarized): Real values include "all terms of the form (/ m n)" where m and n are numerals meeting coprimality conditions — i.e. the model values are exact rationals p/q in coprime form.

## IBM / Cowlishaw — General Decimal Arithmetic FAQ (Primary — the decimal-for-finance rationale)
Source: <https://speleotrove.com/decimal/decifaq1.html>

- "Binary floating-point numbers can only approximate common decimal numbers. The value 0.1, for example, would need an infinitely recurring binary fraction."
- "There are legal and other requirements (for example, in Euro regulations) which dictate the working precision (in decimal digits) and rounding method (to decimal digits) to be used for calculations." Such compliance "can only be met by working in base 10, using an arithmetic which preserves precision."
- "almost all (98.6%) of the numbers in commercial databases have a decimal or integer representation, and the majority are decimal (scaled by a power of ten)."

## Internal — exact-decimal-arithmetic-survey (Primary; Precept research)
Source: `research/architecture/compiler/exact-decimal-arithmetic-survey.md`

- System.Decimal: "does not throw on non-terminating decimal division (e.g., 1m / 3m). Instead, the result is rounded to 28–29 significant digits using round-half-to-even."
- Java BigDecimal `divide()` on a non-terminating result: throws `ArithmeticException("Non-terminating decimal expansion; no exact representable decimal result.")`.
  → Direct evidence that base-10 decimal is NOT closed under division.
</content>
