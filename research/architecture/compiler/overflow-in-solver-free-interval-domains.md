---
status: Cited
authored: 2026-07-26
author: research pass commissioned by owner Shane via /research
topic: how solver-free abstract-interpretation analyzers prove the absence of arithmetic overflow, and what fixed-width versus arbitrary-precision representation does to that problem
external-engagement: strong
---

# Proving the absence of arithmetic overflow in solver-free interval domains

> A computation produces a value the representation cannot hold. This is what the sound
> abstract-interpretation analyzers — the ones that use interval domains and no SMT solver — do
> about that, and what changes when the representation has no maximum at all. It proposes no
> Precept design.

## Background

This is the overflow counterpart to
[`underflow-in-solver-free-interval-domains.md`](underflow-in-solver-free-interval-domains.md),
completed the same day. That survey asked how these analyzers model arithmetic that rounds *down* to
zero. This one asks the mirror question: how they establish that a result never goes *out the top*.

The two are related in the source material — Miné's ESOP 2004 paper defines both conditions in the
same sentence, and both are decided by the same interval endpoints — but they are not the same
problem, and the answers diverge on almost every point that matters. Underflow is *tolerated* by
these analyzers and never reported. Overflow is the thing they are built to reject.

The framing is narrow on purpose. `docs/language/precept-language-spec.md:292` (§0.8) excludes
SMT-style and theorem-prover-style proof artifacts, so SPARK, Dafny and Liquid Haskell describe what
needs proving but reach it through machinery Precept has ruled out. The comparators whose method
could transfer are the solver-free ones. Astrée is the strongest candidate: it proves the absence of
integer and floating-point arithmetic overflow on flight-control software.

`research/architecture/compiler/proof-engine-interval-arithmetic-survey.md` already records that
Astrée detects "Integer and floating-point arithmetic overflow" and that Frama-C EVA reports signed
overflow alarms. It records *what they detect*. This file establishes *how*, *what makes it
imprecise*, *what they do when the proof fails*, and what happens to the whole question when the
integer type has no maximum.

**Context this survey does not argue for.** Precept's owner ruled on 2026-07-26 that overflow is
deferred past the first release with no compile-time guarantee in the meantime, and separately that
the runtime guards are removed. The still-open question this feeds is a different one: whether
`integer` should be fixed-width 64-bit or arbitrary precision. That question is recorded as open in
`docs/language/primitive-types.md:697`. This file takes no side on it.

## Methodology

**Research question.** Four parts. (1) Is computing `[lo, hi]` and comparing against the type's
limits the whole technique for proving overflow absent, or is more needed — and what makes it
imprecise in practice? (2) Does the analyzer's job change depending on whether the underlying
operation wraps silently, traps, or saturates, and which is easier to prove absent? (3) How is an
unprovable overflow reported, and does the analyzer continue under an assumption? (4) Does arbitrary
precision remove the problem, and is "cannot overflow" bought at the price of "cannot bound memory
or time"? A fifth, narrower part: is leaving the representable calendar range treated as ordinary
overflow anywhere, or specially?

**Search strategy.** Internal first, per the brief: the sibling underflow survey, the five papers
already mirrored in `research/references/solver-free-static-analysis/`, the interval-arithmetic
survey, `exact-decimal-arithmetic-survey.md` for representation mechanics, and
`research/language/rational-vs-decimal-exact-numeric-representation.md` for the arbitrary-precision
question. Grepping the existing mirrors for "overflow" was the first action and it paid: the Miné
ESOP 2004 paper carries the definition and the worked imprecision example, and the Parma Polyhedra
Library paper turned out to contain the single best piece of evidence on the bounded-versus-unbounded
integer trade, because a numerical abstract-domain library faces that exact choice for its own
coefficients.

Externally: the full Frama-C EVA user manual, fetched as PDF and extracted with `pdftotext -layout`
(the sibling survey used only a partial extract, which did not reach the integer-overflow section);
the AbsInt Astrée product page; NASA's IKOS, added as a shipping open-source solver-free analyzer
that the sibling did not reach; and for question 4, the primary documentation of four
arbitrary-precision or fixed-width implementations — Java `BigInteger`, CPython `int`, GMP, and
Rust's fixed-width integer methods. For question 5, the `java.time.Instant` specification and Noda
Time's own source.

**Inclusion / exclusion.** In scope: analyzers that establish numeric properties without dispatching
to an SMT or first-order solver, and language/library implementations whose documented overflow
behaviour is evidence about the representation trade. Out of scope: SPARK, Dafny, Liquid Haskell,
CBMC and anything else that discharges through a solver — excluded by spec §0.8, and covered by
earlier surveys. Also deliberately excluded: any Precept design proposal, and any argument for or
against fixed-width `integer`.

**Source-grade mix.** Primary external: Miné ESOP 2004; Cousot et al. ESOP 2005; the Frama-C EVA user
manual; the Parma Polyhedra Library paper (Bagnara/Hill/Zaffanella, SCP 2008); Java SE 21 API
specification for `BigInteger` and `Instant`; CPython 3.14 standard-library documentation; the GMP
6.3.0 manual and home page; the Rust Reference and Rust standard library documentation; Noda Time's
own source; IKOS's own documentation. Secondary external: the AbsInt Astrée product page (vendor
marketing documentation). Tertiary: IEEE 754 itself, not fetched, paywalled — flagged in Threats.
Internal sources graded separately.

**Time bounds.** All external fetches and PDF extractions 2026-07-26. Every excerpt below was taken
from the fetched document or mirror, not from a search-result summary. Load-bearing external sources
are mirrored to `research/references/solver-free-static-analysis/frama-c-eva-manual-overflow-excerpts.txt`
and `research/references/arbitrary-precision-numerics/bounded-vs-unbounded-integer-excerpts.txt`.

## Findings

### 1. Yes — comparing the interval against the type's limits is the whole technique. It is not even a separate step; it is inside the abstract arithmetic.

[Primary; Miné, *Relational Abstract Domains for the Detection of Floating-Point Run-Time Errors*,
ESOP 2004; accessed 2026-07-26 via arXiv cs/0703077; mirrored to
`research/references/solver-free-static-analysis/mine-floating-point-relational-domains-esop2004.txt`]

The paper's rounding function is defined to produce the run-time error value `Ω` exactly when the
real result leaves the format:

> "R_{f,+∞}(x) = Ω if x > Mf_f; min{y ∈ F_f | y ≥ x} otherwise
> R_{f,−∞}(x) = Ω if x < −Mf_f; max{y ∈ F_f | y ≤ x} otherwise"
> — § 3.1 / Fig. 1, where `Mf_f = (2 − 2^−p)2^(2^e−bias−2)` is "the largest non-infinity number"

The interval version simply lifts that to the endpoints. Addition, in the paper's first formulation:

> "[a⁻; a⁺] ⊕♯_{f,r} [b⁻; b⁺] = Ω if a⁻ ⊕_{f,r} b⁻ = Ω or a⁺ ⊕_{f,r} b⁺ = Ω;
> [a⁻ ⊕_{f,r} b⁻; a⁺ ⊕_{f,r} b⁺] otherwise"
> — § 4, *Floating-Point Interval Arithmetics*

That is the entire mechanism. There is no separate overflow analysis, no extra obligation raised
alongside the value computation, no second pass. The abstract operation either produces an interval
whose endpoints are representable, in which case overflow is proved absent for that operation, or it
produces the error. Because every endpoint is computed with outward rounding in the concrete format
(lower bounds towards −∞, upper bounds towards +∞), an endpoint that would leave the format is
*forced* to leave it in the abstract too, which is what makes the check sound rather than optimistic.

Miné states plainly which conditions this model treats as errors and which it does not:

> "We focus our analysis on the large class of programs that treat floating-point arithmetics as a
> practical approximation to the mathematical reals R: roundings and underflows are tolerated, but
> not overflows, divisions by zero or invalid operations, which are considered run-time errors and
> halt the program. Our goal is to detect such behaviors."
> — § 3.2, *Custom Floating-Point Computation Model*

**This is the sharpest difference from the sibling survey.** Underflow is in the tolerated list;
overflow is in the error list, in the same sentence. The same interval endpoints decide both, but
only one of them is a property the analysis is trying to establish.

The shipping tools confirm the check is a first-class deliverable rather than a by-product.

[Secondary; AbsInt, *Astrée — Static Analyzer for Runtime Error Detection*; accessed 2026-07-26;
mirrored to `research/references/arbitrary-precision-numerics/bounded-vs-unbounded-integer-excerpts.txt` § 6]

> "Astrée detects the following runtime errors: division by zero, out-of-bounds array indexing,
> erroneous pointer manipulation and dereferencing (NULL, uninitialized and dangling pointers),
> integer and floating-point arithmetic overflow"

> "Astrée is sound — that is, if no errors are signaled, the absence of errors has been formally
> proved."

[Primary; NASA-SW-VnV/IKOS, *Inference Kernel for Open Static Analyzers*; accessed 2026-07-26;
mirrored to the same file § 7]

IKOS — a solver-free abstract-interpretation analyzer that is open source and shipping — carries
overflow as two named, separately selectable checkers:

> "-a=sio — signed integer overflow analysis — checks for signed integer overflows"
> "-a=uio — unsigned integer overflow analysis — checks for unsigned integer overflows"

and its solver-free numerical domains are exactly the family this survey is about: "interval,
congruence, interval-congruence, DBM, var-pack-dbm, var-pack-dbm-congruence, gauge,
gauge-interval-congruence".

### 2. What makes it imprecise is never the check. It is the bound. Three named remedies, all of them ways to get a tighter interval.

The failure mode is not that the analyzer misses an overflow — soundness forbids that. It is that
the interval it computes is wider than the values the program can actually take, so the comparison
against the type limit fails on a program that is in fact safe.

Miné's worked example is a rate limiter — a loop that clamps how fast an output can change — and it
is a textbook case of the plain interval domain losing:

> "Due to the imprecise abstraction of tests, the interval domain will bound Y by
> [−128 − 16n; 128 + 16n] after n loop iterations while in fact it is bounded by [−128; 128]
> independently from n. If N is too big, the interval analysis will conclude that the limiter may
> overflow while it is in fact always perfectly safe."
> — § 4, *Limits of the Interval Domain*

Note what went wrong: nothing about overflow. The domain simply cannot see that `R = X − S` relates
the temporary to the two variables it was computed from, so each loop iteration adds slack. The
overflow verdict is downstream of that.

The remedy is a relational domain, and the paper measures exactly how much it buys:

> "the octagon domain with our approximated transfer functions can prove that the output Y is
> bounded by [−136; 136] independently from n (the optimal bound being [−128; 128]), which is
> sufficient to prove that Y does not overflow."
> — § 6, *Experimental Results*

**That last clause is the structural point of this entire finding.** `[−136; 136]` is not the true
range. It is not even close to optimal by the paper's own admission. It is still a complete proof,
because the only thing the obligation needs is *some* bound that fits inside the type. Precision
work on an overflow obligation is not a search for the exact range; it is a search for any bound
below the limit. That makes overflow a far more forgiving obligation than one that needs the tight
answer.

Astrée reports the same program with a *looser* bound still, and still counts it proved:

[Primary; Cousot, Cousot, Feret, Mauborgne, Miné, Monniaux, Rival, *The ASTRÉE Analyzer*, ESOP 2005;
accessed 2026-07-26; mirrored to
`research/references/solver-free-static-analysis/astree-analyzer-cousot-et-al-esop2005.txt`]

> "The octagon abstract domain will discover a weaker property, R + S ∈ [−128, 128], which is precise
> enough to prove that Y ∈ [−M, M] is stable whenever M ≥ 144. So, by widening, M will be set to the
> least threshold greater than 144 which is loose but precise enough to prove the absence of runtime
> errors (indeed ASTRÉE finds Y ∈ [−261, 261])."
> — § 5, *Examples of Numerical Abstractions in ASTRÉE*

`[−261, 261]` for a variable whose true range is `[−128, 128]`. Twice the width, and it proves the
property. The mechanism is **widening with thresholds** — the analyzer, when a loop's bound will not
converge, jumps to the next value from a precomputed staircase of candidate bounds rather than to
infinity. Astrée's design history records that the thresholds were there from the start:

> "ASTRÉE was designed starting from a simple memory model … and an interval abstraction (a ≤ X ≤ b
> where X is a variable and a, b are constants to be determined by the analysis), which is precise
> enough to express the absence of runtime errors. The widening uses thresholds. This is extremely
> fast (if sufficient care has been taken to use good data structures) but quite imprecise."
> — § 4, *Design of ASTRÉE by Refinement*

Read that carefully: **the interval abstraction is "precise enough to express the absence of runtime
errors"** — the property is expressible in intervals alone. Everything Astrée added afterwards was
about *reaching* an interval tight enough, not about expressing a different property.

The other named remedies, all from the same paper:

- **Variable packing for octagons.** Octagons cost too much to run over every variable, so Astrée
  attaches them to automatically-computed small groups: "the average pack size is usually of order
  of 3 or 4 variables while the number of packs grows linearly with the program size. It follows
  that precise abstractions are performed only when needed, which is necessary to scale up."
- **Trace partitioning** — "a local parametric symbolic abstraction of sets of traces, which is a
  local refinement of reachable states … Examples are loop unrolling, case analysis for tests, etc."
  The paper is explicit that this is a convenience, not a necessity: "By relative completeness of
  Floyd's proof method, it is useless to reason on traces and sets of states should be precise
  enough. However, this greatly simplifies the abstraction which would otherwise require to
  establish more relations among variables."
- **The arithmetic-geometric progression domain**, which bounds a value that drifts under accumulated
  rounding by relating it to a bounded clock — for a computation that is stable in the reals but
  creeps in floating point, it derives a closed-form ceiling (`|B| ≤ … ≤ 30.7191369175` in the
  paper's Fig. 4 example).

And the result of stacking all of it:

> "Nb of lines 70 000 / 226 000 / 400 000 … False alarms 0 / 0 / 0"
> — § 8, Fig. 5, *Performances of ASTRÉE*

Zero false alarms on 400,000 lines. But the same paper is honest that this took domain-specific
engineering, not a general technique: Astrée "is specialized to a safe programming style but is also
domain-aware in that it knows about control/command (e.g., digital filters)", and there are "very few
or no false alarms **when conveniently adapted to an application domain**".

### 3. Wraparound versus trapping changes the analyzer's job completely — and which behaviour counts as an error is a specification choice, not a hardware fact.

This is the finding with the most direct evidence and the least ambiguity.

**Frama-C EVA's defaults are split by whether the C standard leaves the behaviour undefined:**

[Primary; Frama-C EVA User Manual; accessed 2026-07-26; excerpts mirrored to
`research/references/solver-free-static-analysis/frama-c-eva-manual-overflow-excerpts.txt`]

> "By default, Eva emits alarms for — and reduces the sets of possible results of — signed arithmetic
> computations where the possibility of an overflow exists. Indeed, such overflows have an undefined
> behavior according to paragraph 6.5.5 of the C99 standard. If useful, it is also possible to assume
> that signed integers overflow according to a 2's complement representation. The option
> -no-warn-signed-overflow can be used to this end."
> — § 3.2, *Overflow in integer arithmetic*

> "By default, no alarm is emitted for arithmetic operations on unsigned integers for which an
> overflow may happen, since such operations have defined semantics according to the C99 standard.
> If one wishes to signal and prevent such unsigned overflows, option -warn-unsigned-overflow can
> be used."
> — § 3.2, *Overflow in integer arithmetic*

So a wrap that the language *defines* produces no alarm at all: there is nothing to prove absent,
because nothing is wrong. The obligation exists only where a wrap is an error. IKOS's two separate
checkers (`sio`, `uio`) draw the same line, and both are opt-in per run.

**Astrée goes the other way, and its reason is the important one.** It rejects overflow even on a
platform where the wrap is well-defined, because the *specification it is checking* is a set of
programming guidelines, not the C standard:

> "The absence of runtime errors is the implicit specification that there is no violation of the C
> norm (e.g., array index out of bounds), no implementation-specific undefined behaviors (e.g.,
> floating-point division by zero), no violation of the programming guidelines (e.g., arithmetics
> operators on short variables should not overflow the range [−32768, 32767] although, on the
> specific platform, the result can be well-defined through modular arithmetics), and no violation
> of the programmer-supplied assertions."
> — ESOP 2005, § 2, *Domain of Application of ASTRÉE*

**"although, on the specific platform, the result can be well-defined through modular arithmetics."**
That clause settles the question. Whether overflow is a thing to be proved absent is chosen by
whoever writes the specification. The hardware's behaviour is an input to the *concrete semantics*,
not to whether the property is worth proving.

**Which is easier to prove absent? Trapping — decisively, and for a reason that is easy to miss.**

The proof *obligation* is identical in all three cases: the interval endpoints are compared against
the type's limits. What differs is what the analyzer must model **after** an operation it could not
prove safe.

- **Trapping.** If the operation faults, execution halts. There is no successor state to abstract.
  So the analyzer's post-state only ever has to describe the case where the operation succeeded —
  and in that case the value is the mathematically correct one, exactly what the domain already
  computes. Nothing extra is needed. Miné's model is built on precisely this: overflows "are
  considered run-time errors and halt the program".
- **Wrapping.** The operation always succeeds, and produces a value that is not the mathematical
  result. A sound analyzer must model modular arithmetic, and the post-state is genuinely harder:
  the image of an interval under wraparound is not an interval — it is two disjoint pieces, or the
  whole type range once you convexify it. The EVA manual documents the damage this does, in the
  section on its gauges domain:

  > "Beware that the analysis works better when arithmetic overflows are reported as an alarm. With
  > the default options of Frama-C, this means that gauges can be inferred easily for signed
  > variables, but less so for unsigned ones. Indeed, the affine relations inferred by the domain
  > are no longer true once the variable exceeds e.g. INT_MAX and wraps."
  > — § 6.7, *Gauges*

  That is a shipping analyzer stating that its relational reasoning is *better on the type whose
  overflow is an error* than on the type whose overflow is defined. The defined behaviour is the
  harder one to analyse.
- **Saturating.** Also always succeeds, also produces a wrong value, but the operation is monotone
  and its image of an interval *is* an interval (clamped at the bounds). So it composes with the
  interval domain more gracefully than wrapping does. **No surveyed analyzer treats saturating
  arithmetic at all**, which is an absence rather than a demonstrated impossibility — see Threats.

Rust is useful here as a language-side confirmation that these are three distinct, deliberately
named behaviours over the same values rather than three hardware realities:

[Primary; The Rust Reference § Operator expressions — Overflow, and the `i64` standard-library
documentation; accessed 2026-07-26; mirrored to
`research/references/arbitrary-precision-numerics/bounded-vs-unbounded-integer-excerpts.txt` § 4]

> "Integer operators will panic when they overflow when compiled in debug mode. The
> -C debug-assertions and -C overflow-checks compiler flags can be used to control this more
> directly."

> `checked_add`: "Checked integer addition. Computes self + rhs, returning None if overflow occurred."
> `wrapping_add`: "Wrapping (modular) addition. Computes self + rhs, wrapping around at the boundary
> of the type."
> `saturating_add`: "Saturating integer addition. Computes self + rhs, saturating at the numeric
> bounds instead of overflowing."
> `overflowing_add`: "Calculates self + rhs. Returns a tuple of the addition along with a boolean
> indicating whether an arithmetic overflow would occur."

The same value, four documented outcomes, selected per call site — with the *default* operator's
behaviour being a build-flag policy rather than a property of the type. `System.Decimal` sits at the
trapping end unconditionally; see `exact-decimal-arithmetic-survey.md` § Overflow Semantics, which
records that it throws `System.OverflowException` with "no silent wraparound, no saturation, and no
NaN result", and that C#'s `checked`/`unchecked` contexts do not affect it. That survey also carries
a comparison table of overflow strategies across C#, Rust, Ada and Java; it is not repeated here.

### 4. An unprovable overflow is reported as an alarm that is simultaneously an assumption — and the analysis then keeps going, having learned something from the alarm.

The sibling survey found this shape for division. It is the same for overflow, and the manual is
more explicit about the mechanism.

The key clause is in the sentence already quoted: EVA "emits alarms for — **and reduces the sets of
possible results of** — signed arithmetic computations where the possibility of an overflow exists."
The reduction is visible in the manual's own worked example of a floating-point-to-integer
conversion that may overflow:

> ```
>     int main()
>     {
>       float f = Frama_C_float_interval(2e9, 3e9);
>       return (int) f;
>     }
>
>     [eva:alarm] ov_float_int.c:6: Warning: overflow in conversion from floating-point
>      to integer. assert f < 2147483648;
>
>     [eva] Values at end of function main:
>     f ∈ [2000000000. .. 3000000000.]
>     __retres ∈ [2000000000..2147483647]
> ```
> — § 3.2, *Overflow in conversion from floating-point to integer*

`f` can be up to three billion. The returned `int` is reported as at most `2147483647`. The analyzer
did not prove that; it *assumed* it, on the strength of the alarm it just emitted, and carried the
narrowed interval forward. The manual names this directly:

> "each alarm, in general, is also an assumption."
> — § 3.4, *Alarms and their meaning*

and states the resulting contract without hedging:

> "Eva provides sets of possible values under the assumption that the alarms emitted during the
> analysis have been verified by the user (if necessary, using other techniques). If during an actual
> execution of the application, one of the assertions emitted by Eva is violated, values other than
> those predicted by Eva may happen."
> — § 5.7, *What Eva does not provide*

The alarms are counted, not resolved. A representative run summary from the manual:

> ```
>     143 alarms generated by the analysis:
>         71 integer overflows
>         64 accesses to uninitialized left-values
>          8 illegal conversions from floating-point to integer
> ```
> — § 3.3.3, *Analysis summary*

IKOS reports the same way and is unusually honest about what its middle category means:

> "warning may mean three things: (1) the statement results into an error for some executions, or
> (2) the static analyzer did not have enough information to conclude (check dependent on an external
> input, for instance), or (3) the static analyzer was not powerful enough to prove the absence of
> errors."

Astrée's published results are scored the same way — the ESOP 2005 table's bottom row is "False
alarms 0 0 0", meaning the goal is to drive the count to zero, not to make the tool refuse.

**None of them refuses.** Every comparator produces a result, annotated with the assumptions it made
to get there. Precept refuses. Two consequences worth recording, neither of which is an argument:

- The report-and-reduce model is not merely a reporting difference — it is *load-bearing for
  precision downstream*. The `__retres ∈ [2000000000..2147483647]` above is a tighter interval than
  the analyzer could otherwise justify, and every obligation after it benefits. A refusing analyzer
  never sees the downstream obligation at all, because compilation stopped, so it neither gains nor
  needs that precision. The two models are internally consistent; they are not two settings of one
  dial.
- The comparators' output requires the reader to verify the assumptions by other means, which the
  EVA manual says outright. Refusal removes that obligation from the reader.

### 5. Arbitrary precision does not remove overflow. It moves the boundary far out and converts the failure into an unbounded resource question that none of these analyzers proves anything about.

This is question 4, and the evidence is unusually clean because three widely used implementations
draw the line in three different places.

**Java `BigInteger` still has a maximum, and still throws when you cross it.**

[Primary; `java.math.BigInteger`, Java SE 21 API Specification, Oracle; accessed 2026-07-26; mirrored
to `research/references/arbitrary-precision-numerics/bounded-vs-unbounded-integer-excerpts.txt` § 1]

> "Immutable arbitrary-precision integers."

> "BigInteger must support values in the range -2^Integer.MAX_VALUE (exclusive) to
> +2^Integer.MAX_VALUE (exclusive) and may support values outside of that range. An
> ArithmeticException is thrown when a BigInteger constructor or method would generate a value
> outside of the supported range."

> Implementation Note: "In the reference implementation, BigInteger constructors and operations throw
> ArithmeticException when the result is out of the supported range …"

The type is called arbitrary-precision in its own first sentence and has a documented maximum
magnitude in its own third paragraph. The maximum is enormous — roughly 2^(2·10⁹), a number needing
about 256 megabytes just to store — but it is a maximum, and exceeding it is a trapping failure of
exactly the kind fixed-width arithmetic has. **Arbitrary precision did not delete the failure mode;
it relocated the threshold and guaranteed the trap.**

**GMP has no such threshold, and pays for it with a failure it cannot report.**

[Primary; GMP 6.3.0 home page and manual; accessed 2026-07-26; same mirror § 3]

> "There is no practical limit to the precision except the ones implied by the available memory in
> the machine GMP runs on."
> — gmplib.org, *What is GMP?*

> "By default GMP uses malloc, realloc and free for memory allocation, and if they fail GMP prints a
> message to the standard error output and terminates the program."

> "There's currently no defined way for the allocation functions to recover from an error such as out
> of memory, they must terminate program execution. A longjmp or throwing a C++ exception will have
> undefined results."
> — GMP manual § 13, *Custom Allocation*

So the honest statement of GMP's trade is: the arithmetic cannot overflow, and the failure that
replaces overflow is a process termination with no recovery path and no exception. That is a
*strictly worse* failure mode than `BigInteger`'s catchable `ArithmeticException` — it is just much
rarer.

**Python's integers are genuinely unbounded, and the bound came back somewhere else, driven by a
CVE.**

[Primary; CPython 3.14 standard-library documentation, *Integer string conversion length limitation*;
accessed 2026-07-26; same mirror § 2]

> "The int type in CPython is an arbitrary length number stored in binary form (commonly known as a
> "bignum"). There exists no algorithm that can convert a string to a binary integer or a binary
> integer to a string in linear time, *unless* the base is a power of 2. Even the best known
> algorithms for base 10 have sub-quadratic complexity. Converting a large value such as
> int('1' * 500_000) can take over a second on a fast CPU."

> "CPython has a global limit for converting between int and str to mitigate denial of service
> attacks."

> "Limiting conversion size offers a practical way to avoid CVE 2020-10735."

> "The default limit is 4300 digits …"

The manual's own worked example is precise about where the failure lands:

> ```
>     >>> i = int('2' * 4300)
>     >>> i_squared = i*i
>     >>> len(str(i_squared))
>     Traceback (most recent call last):
>     ...
>     ValueError: Exceeds the limit (4300 digits) for integer string conversion …
> ```

The multiplication succeeds. The *display* of the result raises. The value is unbounded; what is
bounded is a superlinear operation over it — and that bound is a policy constant (4300 digits, with a
configurable floor of 640) introduced in Python 3.11 because the unbounded version was a
denial-of-service vulnerability with a CVE number.

**And a numerical abstract-domain library faces the same choice for its own coefficients — the
evidence here is the most transferable, because it is an analyzer making the decision.**

[Primary; Bagnara, Hill, Zaffanella, *The Parma Polyhedra Library: Toward a Complete Set of Numerical
Abstractions for the Analysis and Verification of Hardware and Software Systems*, SCP 2008; already
mirrored at `research/references/solver-free-static-analysis/parma-polyhedra-library-bagnara-hill-zaffanella-scp2008.txt`]

> "For the representation of general convex polyhedra, with the default configuration, the Parma
> Polyhedra Library uses unbounded precision integers. On the other hand, if speed is important and
> the numerical coefficients involved are likely to be small, applications may use native integers
> (8, 16, 32 and 64 bit integers are supported by the PPL). This is a safe strategy since, when using
> native integers, the library also performs systematic (yet efficient) overflow detection. It is
> thus possible to adopt an approach whereby computations are first attempted with native integers.
> If a computation runs to completion, the user can be certain that no overflow occurred. Otherwise
> an exception is raised … so that the client application can be restarted with bigger native
> integers or with unbounded integers."
> — § 3.5, *Unbounded or Native Integer Coefficients*

Three things in that passage matter. The **default is unbounded** — a library whose correctness
requirements are safety-critical chose the representation that cannot overflow as its default.
Fixed-width is offered as an opt-in *speed* choice. And the fixed-width path is only safe because it
carries systematic overflow detection, with a documented fall-back to the unbounded representation.
The paper is scathing about libraries that skip that detection: PolyLib, New Polka and HyTech "use
finite integral types without any mechanism for overflow detection … this means that the effects of
an overflow are completely undefined, i.e., client applications cannot make any assumption about what
can happen should an overflow occur."

And the cost the PPL pays for the unbounded default is exactly the resource question:

> "The library, thanks to its exception-safety characteristics, naturally supports resource-bounded
> computations, that is, computations that are limited in the amount of CPU time or virtual memory or
> both. The client application can take advantage of this feature by attempting a computation that is
> potentially very expensive imposing a maximum limit on the resources to be used. Should this bound
> be exceeded, an exception is raised and the client application can resort to a simplified
> computation …"
> — § 3.4, *Resource-Bounded Computations*

The PPL's answer to "unbounded values might run away" is a *watchdog timer and a memory cap at
runtime*, plus a documented degradation path to a simpler domain. Not a proof.

**What the static-analysis side says about unbounded values.** Two things, pulling in opposite
directions.

First, the domains themselves are already built over mathematically unbounded numbers. Interval
bounds range over `ℤ ∪ {±∞}` (or the reals plus infinities); the top element of the interval domain
*is* the absence of a bound. So an unbounded integer type does not break these domains — it removes
the comparison against a type limit, which was the only content of the overflow obligation. At the
proof level the obligation genuinely disappears. Frama-C's specification language makes the same move
deliberately, and says why:

> "we see why ACSL numbers are mathematical integers by default; otherwise, the expression
> INT_MAX + 1 might overflow, so we would need to use casts, making the expression harder to read and
> more error-prone."
> — EVA User Manual, § 8, *ACSL*

> "numbers (integers and reals) are almost always at the logic level, where there are no overflows and
> no rounding errors."
> — EVA User Manual, § A.8, *Logic versus C*

Second, and this is the cost: **no analyzer surveyed proves a bound on memory or time.** Astrée's
specification is the absence of runtime errors in a fixed-size synchronous program whose memory is
statically allocated — it explicitly "excludes … dynamic memory allocation, unbounded recursive
function calls". EVA's list of what it does not provide includes termination. IKOS's checkers are all
safety checks on values and memory accesses, none of them a resource bound. The property "this
computation's intermediate values stay small enough to compute in reasonable time and space" is a
different class of property — resource or cost analysis — and it is not in any comparator's list.

So the honest answer to question 4 is: **arbitrary precision removes a provable obligation and
creates an unprovable one.** The overflow obligation is decidable by comparing an interval endpoint
against a constant, which is the cheapest thing in the whole analysis. The resource obligation that
replaces it is not something any of these tools attempts, and the three real implementations handle
it at runtime — `BigInteger` by reimposing a limit and throwing, GMP by terminating the process,
Python by capping a superlinear operation with a policy constant after a security incident.

### 6. Leaving the representable calendar range is treated as overflow by name in one library and as a distinct condition in the other — and analytically it is the same shape of obligation either way.

Neither Astrée, Frama-C EVA nor IKOS has anything to say here: they analyse C, which has no calendar
type. The evidence is from library implementations.

**`java.time` splits it into two separate failures on the same call.**

[Primary; `java.time.Instant`, Java SE 21 API Specification, Oracle; accessed 2026-07-26; mirrored to
`research/references/arbitrary-precision-numerics/bounded-vs-unbounded-integer-excerpts.txt` § 5a]

`Instant.plus(TemporalAmount)` documents both:

> "DateTimeException - if the addition cannot be made"
> "ArithmeticException - if numeric overflow occurs"

`ArithmeticException` is Java's ordinary integer-overflow exception, raised from the `Math.*Exact`
family — the backing `long` seconds field overflowing. `DateTimeException` is the declared range being
left; on `plusSeconds`, `plusMillis` and `plusNanos` the javadoc words it as "if the result exceeds
the maximum or minimum instant". So the two conditions are recognised as different: the
representation overflowing, and the *declared* range being left, which can happen while the
representation is still fine.

And the declared range is chosen by the library, not by the width of the backing type:

> Instant.MIN: "The minimum supported Instant, '-1000000000-01-01T00:00Z'. … This is one year earlier
> than the minimum LocalDateTime. This provides sufficient values to handle the range of ZoneOffset
> which affect the instant in addition to the local date-time. The value is also chosen such that the
> value of the year fits in an int."

**Noda Time calls it overflow outright.**

[Primary; Noda Time, `src/NodaTime/Instant.cs`, `main` branch, and the 3.1.x API documentation;
accessed 2026-07-26; same mirror § 5b]

```csharp
public static Instant operator +(Instant left, Duration right)
    => FromUntrustedDuration(left.duration + right);

internal static Instant FromUntrustedDuration(Duration duration)
{
    int days = duration.FloorDays;
    if (days < MinDays || days > MaxDays)
    {
        throw new OverflowException("Operation would overflow range of Instant");
    }
    return new Instant(duration);
}
```

The exception type is `OverflowException` and the message says "would overflow range of Instant",
even though what actually happened is a range check on a computed day count. Noda Time's documented
range is the ISO calendar's, `-9998-01-01T00:00:00Z` to `9999-12-31T23:59:59.999999999Z`. Notably the
public API documentation for `operator +` and `Add` says only "A new Instant representing the sum of
the given values" — the throwing behaviour is visible only in the source.

Noda Time also carries a third treatment on an internal path where throwing would be wrong — explicit
out-of-range sentinel values rather than an exception:

> "Adds the given offset to this instant, either returning a normal LocalInstant, or
> LocalInstant.BeforeMinValue or LocalInstant.AfterMaxValue if the value would overflow."
> — `Instant.SafePlus(Offset)` doc comment

**What this means analytically.** In both libraries the check is `result < declaredMin ||
result > declaredMax` performed eagerly at every operation — structurally identical to comparing an
interval endpoint against a type's limits, which is Finding 1's mechanism. There is nothing special
about the temporal case for an interval domain: the bound is a constant the library declared instead
of a constant the hardware imposed, and the comparison is the same comparison. What *is* different is
that the declared range is not derivable from the representation, so an analyzer would have to be
told it — which is the same premise-source problem `docs/language/primitive-types.md:701` already
names for Precept's own temporal deferral.

## Threats to Validity

- **Astrée was not run.** AbsInt's shipping Astrée is proprietary. Everything attributed to it comes
  from published papers by its authors (ESOP 2005) and from the vendor product page, which is
  Secondary. The papers describe a 2005 research version; the shipping tool has had twenty years to
  change. The `[−261, 261]` figure in particular is a 2005 measurement of a 2005 build.
- **IEEE 754 itself was not fetched** — paywalled. `Mf_f` and the definition of overflow are taken
  from Miné's restatement, which is Primary but a restatement. Grade the standards leg **Tertiary**.
  No claim above rests on it alone.
- **The Frama-C EVA manual has no version in its URL.** The PDF was fetched from
  `https://frama-c.com/download/frama-c-eva-manual.pdf`, which serves the current release. The
  extract is mirrored, so the excerpts are checkable, but a reader comparing against a different
  Frama-C release may find the option names or defaults have moved.
- **No surveyed analyzer treats saturating arithmetic.** This is the weakest claim in Finding 3. It
  rests on the absence of the word from the EVA manual, the Astrée papers and the IKOS checker list,
  plus one targeted web search that returned nothing on point. A negative from searching is not a
  proof; if a domain for saturating arithmetic exists under a name I did not guess, that part of
  Finding 3 is incomplete.
- **The claim that no comparator proves a resource bound is an absence, and resource analysis was not
  surveyed.** There is a real research literature on automatic cost and resource analysis — RAML,
  COSTA, SPEED, and the amortised-resource-analysis line — and none of it was examined. If any of
  those systems bounds the *magnitude* of an unbounded integer computation (as opposed to loop
  iteration counts or heap allocations), the second half of Finding 5 needs softening: the resource
  obligation would be provable in principle, just not by the analyzers surveyed here.
- **Noda Time's source was read from `main`, not from the 3.1.x tag whose API documentation is
  quoted.** The `OverflowException` throw is present on `main` as of the access date; I did not
  verify it is identical in the 3.1.x release.
- **The Rust Reference's fetched "Overflow" section did not include a statement about release-mode
  wrapping.** The wrap / saturate / checked semantics quoted come from the `i64` standard-library
  method documentation instead. Both are Primary, but the claim that release-mode `+` wraps silently
  is supported here by the internal `exact-decimal-arithmetic-survey.md` table rather than by a
  fetched Rust document.
- **Selection bias.** The analyzer comparators were inherited from the sibling underflow survey and
  its predecessor rather than enumerated fresh. IKOS was added; Polyspace, KeY, Fluctuat, CompCert
  and Verasco were not reached, and Polyspace is the most likely to differ, since it is a commercial
  tool with published overflow-checking behaviour that is not in this survey.
- **Python's digit limit is a CPython implementation policy, not a language specification statement.**
  It is documented in the standard library reference, which is Primary for CPython, but another
  Python implementation need not have it.
- **The temporal finding rests on two libraries.** `java.time` and Noda Time share a designer
  lineage (Stephen Colebourne's Joda-Time influenced both), so they are not independent comparators.
  A genuinely independent temporal library — Python's `datetime`, Rust's `chrono`, or a database's
  timestamp type — was not examined.

## Implications for Precept

No design is proposed, and per the brief nothing here argues for either side of the fixed-width
versus arbitrary-precision question.

1. **The overflow obligation is the cheapest thing in this family of analyzers.** It requires no
   machinery beyond the interval the domain already computes for every expression, and no separate
   pass. If Precept's proof engine computes intervals at all, it has already done the work; the check
   is a comparison of two endpoints against two constants.
2. **The difficulty lands entirely on the tightness of the bound, and the field's answers are already
   ranked internally.** Relational domains, widening with thresholds, variable packing and trace
   partitioning are the named remedies, and
   `research/architecture/compiler/solver-free-static-analysis-techniques-survey.md` already weighs
   that family against Precept's per-keystroke language-server constraint. The Astrée rate-limiter
   result is worth carrying forward as a calibration point: a bound twice as wide as the true range
   still discharged the obligation. Overflow tolerates loose bounds in a way that most obligations
   do not.
3. **Whether overflow is an error at all is a specification choice.** Astrée proves it absent on a
   platform where the wrap is well-defined, because its specification says so; EVA does not alarm on
   unsigned wrap, because C99 defines it. This is prior art for the position that a language may
   declare overflow an error independent of what the machine does.
4. **The comparators' report-and-continue model is not available under refusal, and the reason is
   structural, not stylistic.** EVA's reduction after an alarm is what makes downstream obligations
   dischargeable; a refusing compiler never reaches the downstream obligation. The two models are
   each internally consistent. What is worth noting is that the comparators' model shifts a
   verification duty onto the reader, in the EVA manual's own words.
5. **Arbitrary precision removes a decidable obligation and creates one nobody in this family
   proves.** That is the shape of the trade, stated without a preference. The three real
   implementations converge on the same conclusion by different routes: `BigInteger` reimposes a
   limit and traps at it, GMP terminates the process, Python capped a superlinear operation after a
   CVE. And the PPL — an analyzer's own numeric library — defaults to unbounded while offering
   fixed-width as a guarded speed option, which is a data point about what a correctness-critical
   library picks when it has the choice.
6. **The temporal case is not analytically special.** Both libraries check the declared range eagerly
   at every operation, and the check is the same endpoint-versus-constant comparison as a type limit.
   Noda Time calls it overflow by name. This confirms rather than extends what
   `docs/language/primitive-types.md:701` already records — that a proof-or-reject answer for
   temporal arithmetic needs a premise source, since temporal point types carry no bound vocabulary
   today.
7. **New language surface implication — flagging and stopping, per CLAUDE.md's pre-design gate.** If
   Precept ever wanted to bound the *resource* cost of an arbitrary-precision computation (the
   obligation Finding 5 says replaces overflow), the premise would have to be a bound on the
   magnitude of a value, and `min`/`max` on an `integer` is the closest existing vocabulary. Whether
   that generalises, and whether temporal point types should gain bound vocabulary at all, are
   language-surface questions. They are conversations with the owner, not something this research
   should shape further.

## Conclusions

**Conclusion 1: proving overflow absent needs nothing beyond the interval, and the interval
abstraction is expressive enough for the property on its own.**

- *Rationale*: Miné's rounding function returns the run-time error exactly when the value leaves the
  format, and the interval operations lift that to their endpoints, so the check is inside the
  abstract arithmetic rather than alongside it. Astrée's own design history states that the plain
  interval abstraction "is precise enough to express the absence of runtime errors" — every later
  domain was added to reach a tighter interval, not to express a different property.
- *Alternatives considered and rejected*: that a dedicated overflow analysis is needed — rejected,
  because no surveyed tool has one; IKOS's `sio`/`uio` are checkers over the same numerical domains,
  not separate analyses. That relational domains are required for the *property* — rejected on
  Astrée's own statement that intervals suffice to express it.
- *Precedent*: Miné ESOP 2004 § 3.1 Fig. 1 and § 4; Cousot et al. ESOP 2005 § 4; IKOS checker and
  domain lists; AbsInt's detected-error list.
- *Tradeoff accepted*: this makes the *obligation* cheap and says nothing about the *discharge rate*.
  A cheap check on a bad bound is a false alarm, and Finding 2 is entirely about the cost of getting
  bounds good enough.

**Conclusion 2: an overflow obligation is unusually tolerant of imprecision, because any bound inside
the limit is a complete proof.**

- *Rationale*: the same rate-limiter program is proved safe by three different analyses producing
  three different bounds — `[−128, 128]` (true), `[−136, 136]` (octagons), `[−261, 261]` (Astrée with
  threshold widening). All three discharge, because the obligation is a comparison against the type
  limit, not a demand for the exact range.
- *Alternatives considered and rejected*: that precision work on overflow means chasing the tight
  interval — rejected on the direct evidence of Miné's "which is sufficient to prove that Y does not
  overflow" attached to an admittedly non-optimal bound.
- *Precedent*: Miné ESOP 2004 § 4 (interval domain diverging as `[−128 − 16n; 128 + 16n]`) and § 6
  (octagons at `[−136; 136]`); Cousot et al. ESOP 2005 § 5 (Astrée at `[−261, 261]` via widening with
  thresholds); ESOP 2005 Fig. 5 (zero false alarms at 400k lines).
- *Tradeoff accepted*: the tolerance is real but bounded. The interval domain's slack in that example
  grows with the loop count, so on a long enough loop even a forgiving obligation fails. The remedy
  is a relational domain, whose cost is the subject of a separate internal survey.

**Conclusion 3: trapping arithmetic is strictly easier to analyse than wrapping arithmetic, and the
reason is the post-state, not the check.**

- *Rationale*: the obligation is identical in both cases. What differs is that a trap halts, so the
  successor state need only describe the case where the mathematically correct value was produced —
  which is what the domain already computes. A wrap always succeeds and produces a value that is not
  the mathematical result, whose image under an interval abstraction is not an interval. EVA
  documents the precision loss this causes in its own gauges domain, on unsigned variables
  specifically.
- *Alternatives considered and rejected*: that defined behaviour (wrapping) is the easier case
  because there is no error to prove absent — rejected. It is easier in the sense that the obligation
  disappears, and harder in the sense that everything downstream of the operation becomes less
  precise. EVA's manual states the second half explicitly.
- *Precedent*: EVA § 3.2 (signed alarms on, unsigned alarms off, by C99 definedness) and § 6.7
  (gauges "works better when arithmetic overflows are reported as an alarm"); Miné § 3.2 (overflows
  "halt the program"); Astrée ESOP 2005 § 2 (overflow rejected even where modular arithmetic is
  well-defined on the platform); Rust's four named behaviours.
- *Tradeoff accepted*: saturating arithmetic is the untested third case. It should compose well with
  intervals for the same reason wrapping does not — it is monotone and its image of an interval is an
  interval — but no surveyed system does it, so that reasoning is unsupported by evidence.

**Conclusion 4: arbitrary precision converts a decidable compile-time obligation into a runtime
resource failure, and every real implementation handles that failure at runtime rather than proving
it away.**

- *Rationale*: `BigInteger` documents a maximum and throws `ArithmeticException` at it; GMP has no
  maximum and terminates the process when allocation fails, with no defined recovery; CPython's
  values are unbounded but a superlinear operation over them was a denial-of-service CVE, fixed by a
  configurable digit cap. Meanwhile the overflow obligation that arbitrary precision removes is the
  cheapest one in the analysis, and the resource obligation that replaces it is not on any surveyed
  analyzer's checker list.
- *Alternatives considered and rejected*: that "arbitrary precision means overflow cannot occur" —
  rejected on `BigInteger`'s own specification, which names both the range and the exception. That
  the resource question is theoretical — rejected on CVE 2020-10735 and on the PPL's need for
  watchdog timers and memory caps around its own unbounded coefficients.
- *Precedent*: Java SE 21 `BigInteger` class documentation and Implementation Note; GMP 6.3.0 home
  page and manual § 13; CPython 3.14 *Integer string conversion length limitation*; PPL SCP 2008
  § 3.4 and § 3.5.
- *Tradeoff accepted*: the comparison is not symmetric in difficulty. An overflow obligation is
  decided by two comparisons; a resource bound is a research-grade property. Saying "one obligation
  is replaced by another" is technically accurate and understates how differently hard the two are —
  in both directions, since the resource failure is also enormously rarer.

## What would change this conclusion

- **If a shipping analyzer is found that soundly models saturating arithmetic in an interval or
  relational domain**, the third case in Conclusion 3 stops being an untested inference and the
  wrap/trap/saturate comparison needs rewriting with evidence for all three.
- **If a resource- or cost-analysis system is found that bounds the magnitude of arbitrary-precision
  intermediate values** (not just loop counts or allocation sites), Conclusion 4's "not on any
  surveyed analyzer's checker list" is a survey gap rather than a property of the field, and the
  arbitrary-precision trade becomes provable-obligation-for-provable-obligation rather than
  provable-for-unprovable.
- **If Polyspace, Fluctuat or Verasco turns out to prove overflow absent by a method that is not
  interval-endpoints-versus-type-limits**, Conclusion 1 is wrong in its strong form ("nothing beyond
  the interval").
- **If a current Astrée release is found to report substantially different bounds on the published
  rate-limiter example**, the calibration point in Conclusion 2 is stale, though the structural claim
  (any bound inside the limit discharges) would survive.
- **If a temporal library is found that treats leaving the calendar range as something other than a
  range check against declared constants** — a saturating clamp, say, or a widened representation —
  Finding 6's "not analytically special" claim needs qualification.

## Open Questions

- What the widening-with-thresholds technique costs in Precept's prover specifically, and where the
  threshold staircase would come from. Astrée's thresholds are a parameterisation supplied by
  specialists per application domain; Precept has no equivalent input, and the declared `min`/`max`
  constraints in a `.precept` file are the obvious candidate source but were not investigated.
- Whether Precept's proof engine currently computes intervals in a form where the type-limit
  comparison of Finding 1 would be a one-line addition, or whether the intervals it computes are
  scoped to declared constraints only. `research/language/rational-vs-decimal-exact-numeric-representation.md`
  records that the engine represents static numeric magnitudes as .NET `decimal`
  (`NumericRuleFact(… decimal Value)`), which is a different question from whether it carries a
  bound per expression.
- What the resource question would concretely look like for Precept's rule shapes. The
  `rational-vs-decimal` survey already flags denominator blow-up under rational arithmetic and notes
  that Raku's default `Rat` falls back to floating point on denominator overflow. Whether Precept's
  expression forms — no loops, no recursion, bounded expression depth — make unbounded growth
  reachable at all was not investigated, and it is the question that decides how much of Conclusion 4
  actually bites.
- Whether Polyspace's overflow checking differs in method. It is the largest commercial comparator
  not reached by this survey or its predecessors.
- Whether saturating arithmetic has any precedent as a *language-level* choice for a business-rules
  DSL, as opposed to a per-call-site operation in a systems language. Rust offers it per call;
  nothing surveyed makes it the default.

## Sources

**External — Primary**

- **Relational Abstract Domains for the Detection of Floating-Point Run-Time Errors**, Antoine Miné.
  ESOP 2004, LNCS 2986, pp. 3–17. DOI 10.1007/978-3-540-24725-8_2. Accessed 2026-07-26 via arXiv
  preprint cs/0703077 (https://arxiv.org/pdf/cs/0703077). Mirrored to
  `research/references/solver-free-static-analysis/mine-floating-point-relational-domains-esop2004.txt`.
- **The ASTRÉE Analyzer**, Patrick Cousot, Radhia Cousot, Jérôme Feret, Laurent Mauborgne, Antoine
  Miné, David Monniaux, Xavier Rival. ESOP 2005, LNCS 3444, pp. 21–30. Accessed 2026-07-26.
  https://www.di.ens.fr/~cousot/publications.www/CousotEtAl-ESOP05.pdf. Mirrored to
  `research/references/solver-free-static-analysis/astree-analyzer-cousot-et-al-esop2005.txt`.
- **Frama-C EVA User Manual (Evolved Value Analysis)**, CEA List / Inria. The tool's own versioned
  documentation. Accessed 2026-07-26, https://frama-c.com/download/frama-c-eva-manual.pdf, extracted
  with `pdftotext -layout`. Overflow-bearing excerpts mirrored to
  `research/references/solver-free-static-analysis/frama-c-eva-manual-overflow-excerpts.txt`.
- **The Parma Polyhedra Library: Toward a Complete Set of Numerical Abstractions for the Analysis and
  Verification of Hardware and Software Systems**, Roberto Bagnara, Patricia M. Hill, Enea
  Zaffanella. *Science of Computer Programming* 72(1–2), 2008. Accessed via the existing mirror at
  `research/references/solver-free-static-analysis/parma-polyhedra-library-bagnara-hill-zaffanella-scp2008.txt`.
- **java.math.BigInteger**, Java SE 21 API Specification, Oracle. Accessed 2026-07-26.
  https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/math/BigInteger.html. Mirrored to
  `research/references/arbitrary-precision-numerics/bounded-vs-unbounded-integer-excerpts.txt` § 1.
- **java.time.Instant**, Java SE 21 API Specification, Oracle. Accessed 2026-07-26.
  https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/time/Instant.html. Mirrored to
  the same file § 5a.
- **Integer string conversion length limitation**, The Python Standard Library — Built-in Types,
  Python 3.14.6 documentation, Python Software Foundation. Accessed 2026-07-26 (reStructuredText
  source from https://docs.python.org/3/_sources/library/stdtypes.rst.txt). References CVE-2020-10735.
  Mirrored to the same file § 2.
- **GNU MP 6.3.0** — home page (https://gmplib.org/) and manual § 13 *Custom Allocation*
  (https://gmplib.org/manual/Custom-Allocation). Accessed 2026-07-26. Mirrored to the same file § 3.
- **The Rust Reference**, § Operator expressions — Overflow
  (https://doc.rust-lang.org/reference/expressions/operator-expr.html#overflow) and the Rust standard
  library `i64` documentation (https://doc.rust-lang.org/std/primitive.i64.html). Accessed
  2026-07-26. Mirrored to the same file § 4.
- **Noda Time**, `src/NodaTime/Instant.cs` on `main`
  (https://raw.githubusercontent.com/nodatime/nodatime/main/src/NodaTime/Instant.cs) and the 3.1.x
  API documentation (https://nodatime.org/3.1.x/api/NodaTime.Instant.html). Accessed 2026-07-26.
  Mirrored to the same file § 5b.
- **IKOS (Inference Kernel for Open Static Analyzers)**, NASA Software V&V.
  https://github.com/NASA-SW-VnV/ikos and `analyzer/README.md`. Accessed 2026-07-26. Mirrored to the
  same file § 7.

**External — Secondary**

- **Astrée — Static Analyzer for Runtime Error Detection**, AbsInt Angewandte Informatik GmbH.
  Vendor product documentation. https://www.absint.com/astree/index.htm, accessed 2026-07-26.
  Mirrored to `research/references/arbitrary-precision-numerics/bounded-vs-unbounded-integer-excerpts.txt` § 6.

**External — Tertiary**

- **IEEE 754 floating-point standard.** **Not fetched — paywalled.** Used only as restated by Miné.
  See Threats to Validity.

**Internal**

- `research/architecture/compiler/underflow-in-solver-free-interval-domains.md` — the sibling survey.
  Same comparator family, mirror question (arithmetic that rounds to exactly zero). Cited.
- `research/architecture/compiler/proof-engine-interval-arithmetic-survey.md` — covers seven systems
  at the level of what they detect; the starting point for this file's *how*.
- `research/architecture/compiler/divisor-nonzero-under-finite-representation-survey.md` — the survey
  that opened this line of work.
- `research/architecture/compiler/exact-decimal-arithmetic-survey.md` — representation mechanics,
  including `System.Decimal`'s unconditional `OverflowException` and a comparison table of overflow
  strategies across languages. Cited, not repeated.
- `research/architecture/compiler/solver-free-static-analysis-techniques-survey.md` — the
  cost-and-expressiveness ranking that Finding 2's remedies should be weighed against.
- `research/language/rational-vs-decimal-exact-numeric-representation.md` — the arbitrary-precision
  survey on the value-representation side; records the denominator blow-up problem and Raku's
  fallback ceiling, and confirms that Precept's fixed-width-versus-arbitrary-precision question is
  formally deferred rather than settled.
- `docs/language/precept-language-spec.md` §0.8 — the exclusion of solver-style proof artifacts that
  sets this survey's scope.
- `docs/language/primitive-types.md:697` and `:701` — the deferred arithmetic-overflow model and the
  temporal arm that rides with it.
