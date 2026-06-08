# Source mirror — CUE language spec & concept docs (value/constraint lattice, bounds, defaults)

Mirrored for the interval-vs-value-evaluation survey. Access date 2026-06-05.
Source grade: Primary (official CUE language specification + official concept docs, cuelang.org).

## CUE Language Specification — https://cuelang.org/docs/reference/spec/ (accessed 2026-06-05)

§ Values:
> "All possible values are ordered in a lattice, a partial order where every two elements have a single greatest lower bound."

§ Values (types are values):
> "CUE's values also include what we normally think of as types, like `string` and `float`. It does not distinguish between types and values: only the relationship of values in the lattice is important."

§ Unification:
> "The _unification_ of values `a` and `b` is defined as the greatest lower bound of `a` and `b`."

§ Bounds:
> "A _bound_, syntactically a [unary expression], defines a logically infinite disjunction of concrete values represented as a single comparison. For example, `>= 2` represents the infinite disjunction `2|3|4|5|6|7|…`."
> "For any [comparison operator] `op`, `op a` is the disjunction of every `x` such that `x op a`."

§ Default values:
> "Any value `v` _may_ be associated with a default value `d`, where `d` must be in instance of `v` (`d ⊑ v`)."
> "Intuitively, when an expression needs to be resolved for an operation other than unification or disjunction, non-starred elements are dropped in favor of starred ones if the starred ones do not resolve to bottom."

§ Bottom and errors:
> "Any evaluation error in CUE results in a bottom value, represented by the token `_|_`. Bottom is an instance of every other value."

§ Top:
> "Top is represented by the underscore character `_`, lexically an identifier. Unifying any value `v` with top results in `v` itself."

## CUE — "The Logic of CUE" concept doc — https://cuelang.org/docs/concept/the-logic-of-cue/ (accessed 2026-06-05)

> "Types _are_ values" and "Values (and thus types) are ordered into a lattice"

> "Every value in CUE, including what would in most programming languages be considered types, is partially ordered in a single hierarchy (a lattice, to be precise)."

> "if an element B is a subset of element A, there is a path from A to B. In more general terms, we then say that A _subsumes_ B, or that B is an _instance of_ A."

> "for every two elements, there is a _unique_ instance of both elements that subsumes all other elements that are an instance of both elements. This is called the greatest lower bound, or meet."

On constraints as an intermediate category between value and type (from the lattice diagram description):
> "constraints, a category of values that falls between the traditional concepts of value and type"

On defaults:
> "Conceptually, CUE keeps two parallel values, one for all possible values and one for the default, which must be an instance of the former."

## CUE tour — Bounds — https://cuelang.org/docs/tour/types/bounds/ (accessed 2026-06-05)

> "A bound is expressed using comparison operators such as `>`, `<=`, and `!=`. It permits values where the comparison would return `true`, and we say that _the bound is defined_ for these values."
> "They work on numbers, strings, bytes, and `null`."
> "Bounds define a lower bound, an upper bound, or inequality for a certain value, all of which can be combined."
(Example shown in-page: `zero: 0 & >10 // failure` — unifying a concrete value with a bound yields bottom when the value falls outside the bound.)
