---
status: Cited
authored: 2026-07-26
author: research pass commissioned by owner Shane via /research
topic: how solver-free abstract-interpretation analyzers model arithmetic that rounds to exactly zero, and whether any of them can prove a computed divisor non-zero
external-engagement: strong
---

# Underflow to exactly zero in solver-free interval domains

> Two values are each known non-zero, their product is too small for the representation to hold, and
> the stored result is zero. This is what the sound abstract-interpretation analyzers — the ones that
> use interval domains and no SMT solver — actually do about that. It proposes no Precept design.

## Background

`research/architecture/compiler/divisor-nonzero-under-finite-representation-survey.md` established that a
declared non-zero fact on each operand does not discharge a divisor obligation about an expression over
those operands, and that Precept's own compiler already refuses the case. That survey reached SPARK and
the SMT-LIB theory declarations. It left a hole it named in its own Threats to Validity: the systems
built for exactly this class of numerical property — Astrée and Frama-C — were not examined, and the
survey guessed they might hold a better answer.

They were in fact examined earlier, in
`research/architecture/compiler/proof-engine-interval-arithmetic-survey.md`, which covers Frama-C EVA and
Astrée. But that survey records *what they detect*, not *how they model rounding*, so the specific
question was still open. This file closes it.

**Companion file.** [`overflow-in-solver-free-interval-domains.md`](overflow-in-solver-free-interval-domains.md)
asks the mirror question of the same comparators — how they prove a result never leaves the *top* of
the representation, and what changes when the type has no maximum at all. The two are decided by the
same interval endpoints, and Miné defines both conditions in one sentence, but the answers diverge on
almost every point: underflow is in these analyzers' *tolerated* list and is never reported, while
overflow is in their error list and is the thing they are built to reject. Read that file for
widening with thresholds, the wrap-versus-trap-versus-saturate comparison, the alarm-as-assumption
reporting model, and what arbitrary-precision integers do to the obligation.

The framing is narrow on purpose. `docs/language/precept-language-spec.md:292` (§0.8) excludes SMT-style and
theorem-prover-style proof artifacts, so SPARK, Dafny and Liquid Haskell describe what needs proving but
reach it through machinery Precept has ruled out. The comparators whose method could transfer are the
solver-free ones: abstract interpretation over numeric domains.

## Methodology

**Research question.** Does a standard interval domain model rounding, or does it reason over exact reals
and therefore give unsound answers near the representation floor? Is there a named technique for tracking
a lower bound on *magnitude*, as distinct from a lower bound on *value*? How is an uncleared divisor
reported? And does the exactness of the numeric type change any of it?

**Search strategy.** Internal first: the two surveys named above, the five mirrored papers already in
`research/references/solver-free-static-analysis/`, and
`research/architecture/compiler/exact-decimal-arithmetic-survey.md` for representation mechanics. The
mirrored Apron paper pointed at its own reference [19] — Miné's ESOP 2004 floating-point domain paper —
which turned out to be the primary source for the whole question. From there: the Astrée papers by its
authors (ESOP 2005, PLDI 2003), the Frama-C EVA user manual, and targeted searches for a magnitude-bound
domain under several phrasings ("range excluding zero", "non-convex divisor", "disequality domain",
"absolute value domain", "away from zero").

**Inclusion / exclusion.** In scope: analyzers that establish numeric properties without dispatching to
an SMT or first-order solver. Out of scope: SPARK, Dafny, Liquid Haskell, CBMC and anything else that
discharges through a solver — already covered by the prior survey, and excluded by spec §0.8 anyway.
Deliberately excluded: any Precept design proposal.

**Source-grade mix.** Five Primary external (Miné ESOP 2004; Cousot et al. ESOP 2005; Blanchet et al.
PLDI 2003; Chen/Liu/Miné/Kapur/Wang SAS 2014; the Frama-C EVA user manual, which is the tool's own
versioned documentation). One Primary external used lightly (Ghorbal et al. VMCAI 2012). One Tertiary
substitution flagged in Threats: IEEE 754 itself was not fetched. Internal sources graded separately.

**Time bounds.** External fetches and PDF extractions 2026-07-26. All excerpts re-extracted with
`pdftotext -layout` from the downloaded PDFs and mirrored, rather than taken from search-result summaries.

## Findings

### 1. The interval domain does not reason over exact reals. It carries the representation floor as an explicit constant.

[Primary; Miné, *Relational Abstract Domains for the Detection of Floating-Point Run-Time Errors*, ESOP
2004; accessed 2026-07-26 via arXiv cs/0703077; mirrored to
`research/references/solver-free-static-analysis/mine-floating-point-relational-domains-esop2004.txt`]

The paper defines, per floating-point format `f`, exactly the constant the question turns on:

> "For each format f ∈ F we define in particular:
> • mf_f = 2^(1−bias−p) the smallest non-zero positive number;
> • Mf_f = (2 − 2^−p)2^(2^e−bias−2), the largest non-infinity number."
> — § 3.1 *IEEE 754 Floating-Point Numbers*

And it names the case by name:

> "Rounding can build infinities from non-infinities operands (this is called overflow), and it may
> return zero when the absolute value of the result is too small (this is called underflow)."
> — § 3.1

The domain then carries `mf_f` as an additive error term on *every* arithmetic operation. In the
linearization that lets relational domains stay sound over floating-point code, each of addition,
subtraction, multiplication, division and cast contributes a term `⊞♯ mf_f [−1; 1]` — see § 5 of the
mirrored text, where all five rules end in that term. The paper says plainly what it is for:

> "To account for rounding errors, an upper bound of |R_{f,r}(x·y) − (x·y)| (where · ∈ {+,−,×,/}) is
> included in L expr_f Mρ♯. It is the sum an error relative to the arguments x and y, expressed using
> ε_f, and an absolute error mf_f due to a possible underflow."
> — § 5, *About ε_f*

Astrée's own paper states the same structure in ordinary notation:

[Primary; Cousot, Cousot, Feret, Mauborgne, Miné, Monniaux, Rival, *The ASTRÉE Analyzer*, ESOP 2005;
accessed 2026-07-26; mirrored to
`research/references/solver-free-static-analysis/astree-analyzer-cousot-et-al-esop2005.txt`]

> "The general approach has been to define the concrete semantics of floating point computations in the
> reals (taking the worst possible rounding errors explicitly into account), to abstract with real
> numbers but to implement, thanks to a further sound over-approximation, using floats. For example the
> float expression (x + y) + z is evaluated as in the reals as x + y + z + ε₁ + ε₂ where
> |ε₁| ≤ ε_rel.|x + y| + ε_abs and |ε₂| ≤ ε_rel.|x + y + ε₁ + z| + ε_abs."
> — § *Floating-Point Interval Linear Form Abstraction*

The `ε_abs` term is the same thing under a different name: an absolute error allowance that does not
shrink as the operands shrink. That is precisely what makes the bound correct near zero.

**So the answer to the first question is: it models rounding, and it does so by admitting an error of at
least one smallest-representable-magnitude in either direction on every operation.** It is not an exact-real
analysis with a soundness hole near the floor. The floor is in the arithmetic.

There is a second, simpler mechanism worth separating out. The plain non-relational interval domain does
not need an error term at all, because it computes bounds using the concrete format's own operations with
outward rounding — lower bounds rounded towards −∞, upper bounds towards +∞:

> "[a⁻; a⁺] ⊗♯_f [b⁻; b⁺] = [min((a⁺ ⊗_{f,−∞} b⁺), (a⁻ ⊗_{f,−∞} b⁺), (a⁺ ⊗_{f,−∞} b⁻), (a⁻ ⊗_{f,−∞} b⁻));
> max((a⁺ ⊗_{f,+∞} b⁺), (a⁻ ⊗_{f,+∞} b⁺), (a⁺ ⊗_{f,+∞} b⁻), (a⁻ ⊗_{f,+∞} b⁻))]"
> — § 4, *Floating-Point Interval Arithmetics*

Multiply `[1e-16, 1e-16]` by itself under those rules and the bounds are the *rounded* product, which is
zero. The interval is `[0, 0]` or `[0, mf_f]`. Zero is in it, and it got there by the domain doing the
right thing rather than by a gap in the domain. The error term in the paragraphs above exists for the
relational domains (octagon and friends), which manipulate expressions symbolically and would otherwise
apply real-field algebra that floating-point arithmetic does not obey.

### 2. Underflow itself is not reported. It is tolerated, and the report arrives later, at the division.

Miné states the computation model these analyzers work in:

> "We focus our analysis on the large class of programs that treat floating-point arithmetics as a
> practical approximation to the mathematical reals R: roundings and underflows are tolerated, but not
> overflows, divisions by zero or invalid operations, which are considered run-time errors and halt the
> program."
> — § 3.2, *Custom Floating-Point Computation Model*

Frama-C EVA is built the same way. Its floating-point alarms cover two things, and underflow is not among
them:

[Primary; Frama-C EVA User Manual; accessed 2026-07-26; excerpts mirrored to
`research/references/solver-free-static-analysis/frama-c-eva-manual-excerpts.txt`]

> "By default, Eva emits an alarm whenever a floating-point operation can result in a NaN or infinite
> value, and continues the analysis with a finite interval representing the result obtained if excluding
> these possibilities."
> — § *Floating-point alarms*

The three settings of the option that controls those alarms — `-warn-special-float` with values
`non-finite`, `nan`, `none` — are all about NaN and infinity. There is no underflow setting because there
is no underflow alarm. A product that rounds to zero is simply a value of zero, propagated like any other.

**This is the shape of the whole thing.** These analyzers do not treat underflow as an error at the point
it happens. They let the zero into the value and catch it at the point where a zero is actually
forbidden — the division. The safety property is enforced where it is stated, not where it was caused.

### 3. The divisor check is a membership test on the interval, and zero being in it is a refusal.

Miné's interval division is defined to fail outright when the divisor interval straddles zero:

> "[a⁻; a⁺] ⊘♯_f [b⁻; b⁺] = ◦ Ω if b⁻ ≤ 0 ≤ b⁺"
> — § 4, *Floating-Point Interval Arithmetics* (Ω is the run-time error)

EVA says the same thing in prose, and states the soundness direction explicitly:

> "a particular division in the analyzed program is the object of a warning as soon as the set of
> possible values for the divisor contains zero. Only if the set of possible values computed by Eva does
> not contain zero is the warning omitted, and that means that the divisor really cannot be null at
> run-time."
> — § 1.2

EVA's own worked example of the check is structurally the case this survey is about — a divisor that is a
computed product of two variables:

> ```
>     int A, B;
>     void main(int x, int y)
>     {
>       A = 100 / (x * y);
>       B = 333 % x;
>     }
>
>     div.c:4: ... division by zero. assert (int)(x*y) ≢ 0;
> ```
> "In the above example, there is no way for the analyzer to guarantee that x*y is not null, so it emits
> an alarm at line 4."
> — § 3.2, *Division by zero*

Note what the emitted obligation is a statement *about*: `(x*y) ≢ 0`, the whole divisor expression, not
`x ≢ 0 ∧ y ≢ 0`. This matches the shape the prior survey found in GNATprove's suggested fix.

**Putting 1, 2 and 3 together: the interval domain handles the underflow case correctly, and it handles
it by refusing.** It does not need to detect underflow specially. The product of two tiny intervals is an
interval containing zero — because the stored product genuinely is zero — and the divisor test then fails
on that interval. There is no unsoundness to fix here and no cleverness involved. The domain simply never
proves the thing that is false.

**How the reporting differs from Precept's.** EVA emits the alarm and *continues under the assumption
that it does not happen*:

> "If the divisor seems to be able to take non-null values, the analyzer is allowed to take into account
> the property that the divisor is different from zero when it continues the analysis after this point."
> — § 3.2, *Division by zero*

So the model is: a maybe, reported at the site, with the analysis proceeding as if the maybe resolved
favourably. Astrée is the same shape — an alarm is a possible violation, and the published results are
scored by how many alarms remain, with zero being the goal (ESOP 2005 Fig. 5 reports "False alarms 0 0
0"). Neither system refuses to produce a result. Precept refuses. That is a genuine difference in the
product, not in the analysis: the analysis question "can this be zero" is answered identically; what is
done with a *yes* differs.

### 4. There is a named technique for a lower bound on magnitude, and it was invented for this exact problem.

[Primary; Chen, Liu, Miné, Kapur, Wang, *An Abstract Domain to Infer Octagonal Constraints with Absolute
Value*, SAS 2014; accessed 2026-07-26; mirrored to
`research/references/solver-free-static-analysis/absolute-value-octagons-chen-liu-mine-kapur-wang-sas2014.txt`]

The paper opens by naming the divisor problem as the motivation, and by naming why ordinary domains
cannot do it:

> "E.g., to remove a division-by-zero false alarm, the analysis needs to find a range excluding 0 for the
> divisor, which is in general a non-convex property and may be out of the reasoning power of convex
> abstract domains."
> — § 1

That is the crux. An interval `[lo, hi]` is a convex set. "Not zero" is a set with a hole in it, which no
single interval can be. The domain the paper proposes fixes that by admitting the absolute-value function
into the constraint language:

> "we propose a new abstract domain, namely the abstract domain of octagonal constraints with absolute
> value (AVO), to infer relations of the form {±X ± Y ≤ c, ±X ± |Y| ≤ d, ±|X| ± |Y| ≤ e} over each pair
> of variables X, Y in the program where constants c, d, e ∈ R are automatically inferred by the analysis."
> — § 1

> "x ≤ −1 ∨ x ≥ 1 can be encoded as |x| ≥ 1"
> — § 1

`|x| ≥ 1` is a lower bound on magnitude expressed as an upper bound on `−|x|`, which is one of the
constraint forms the domain supports. **The technique has a name — the absolute-value octagon domain, AVO
— and its stated purpose is discharging divisor obligations.** It keeps the octagon domain's complexity
class: quadratic memory, cubic time, no solver.

The paper's results table shows the domain inferring literal magnitude lower bounds and clearing the
divisor with them:

| program | weakest condition needed | octagons infer | AV octagons infer | AVO false alarms |
|---|---|---|---|---|
| motiv(if) | `dy ≠ 0` | `dy ∈ [−∞,+∞]` | `\|dy\| > 0` | 0 |
| gpc | `den ≠ 0` | `den ∈ [−∞,+∞]` | `\|den\| > 0.1` | 0 |
| goc | `d ≠ 0` | `d ∈ [−∞,+∞]` | `\|d\| ≥ 0.1` | 0 |
| xcor | `usemax ≠ 0` | `usemax ≥ 0` | `usemax > 0` | 0 |

— Fig. 10, *Experimental results on NECLA division-by-zero benchmarks*

There is also a whole benchmark suite for the problem, the **NECLA Division-by-zero False Alarms** set,
described as "extracted from source code of several open-source projects" and illustrating "commonly used
techniques that programmers use to protect a division-by-zero".

A second named domain does the same job by a different construction:

[Primary; Ghorbal, Ivančić, Balakrishnan, Maeda, Gupta, *Donut Domains: Efficient Non-convex Domains for
Abstract Interpretation*, VMCAI 2012; accessed 2026-07-26 via HAL hal-01660908; mirrored to
`research/references/solver-free-static-analysis/donut-domains-ghorbal-et-al-vmcai2012.txt`]

> "The abstract objects are encoded as a pair of two convex abstract objects: the first defines an
> over-approximation of reachable values, while the second under-approximates the set of impossible
> values. The geometrical concretization is defined by a convex set minus another convex set (or hole).
> We thus call these domains donut domains."
> — § 1

A convex set with a hole punched out of it around the origin is a magnitude lower bound by another route.
The two papers benchmark against each other on the same NECLA set, with AVO clearing one program (`xcor`)
that the donut domain does not.

### 5. Neither of those domains gets the magnitude bound from arithmetic. They get it from tests in the code.

This is the limit, and it matters more than the existence of the domains.

Every case in the AVO results table is a program where the author *wrote a guard* — a zero test or an
`fabs` comparison — and the domain's job is to carry the fact past the branch. The motivating example is
explicit about this: at the point after `if (dx == 0.0 && dy == 0.0) return;` the fact `dx ≠ 0 ∨ dy ≠ 0`
holds, and AVO's contribution is being able to *represent* it as `−|dx| − |dy| < 0` and then combine it
with the later `fabs(dy) > fabs(dx)` test to reach `−|dy| < 0`.

Nothing in AVO's constraint language relates `|X × Y|` to `|X|` and `|Y|`. Its constraints are linear in
the variables and their absolute values, over pairs; the closure algorithms and transfer functions the
paper defines are for that shape. A multiplication of two variables is outside it. The general treatment
of such operations in this family of analyzers is Miné's: replace one factor by an interval and multiply,
which throws away exactly the relationship you would need.

**And even a correct real-arithmetic magnitude bound would not finish the job.** Suppose an analysis
established `|A| ≥ 1e-16` and `|B| ≥ 1e-16` and were permitted to conclude `|A × B| ≥ 1e-32` over the
reals. The value that gets stored is the *rounded* product, and Finding 1 says the domain's own model
already allows that to differ from the real product by up to `mf_f`. So the conclusion "the stored product
is non-zero" requires `1e-32 > mf_f` — a comparison of the derived bound against a constant of the
representation. It is not an algebraic step. **A magnitude lower bound only proves a computed divisor
non-zero when the bound clears the representation floor**, and how far the bound has to clear it by is a
property of the format, not of the program.

No surveyed source performs that composition. That is a finding about absence, and Threats to Validity
grades it accordingly.

### 6. Exactness changes the mechanism, not the existence of the case.

The analyzers draw the line between exact and rounded arithmetic sharply, and they draw it because
blurring it is unsound in a stated direction. Astrée:

> "A general problem with relational numerical domains is that of floating point numbers. Considering
> them as reals (as usually done with theorem provers) or fixed point numbers (as in CBMC) would not
> conform to the norm whence would be unsound."
> — ESOP 2005, § *Floating-Point Interval Linear Form Abstraction*

Frama-C draws it as a language boundary. Its specification language ACSL has a `real` type that lives in
a different world from the C `float` and `double`:

> "numbers (integers and reals) are almost always at the logic level, where there are no overflows and no
> rounding errors. Conversely, there are neither floating-point infinities nor NaNs."
> — EVA User Manual, § A.8 *Logic versus C*

So on a genuinely exact lane, the underflow case does not arise: there is no smallest positive magnitude
to fall below. That is the same conclusion the prior divisor survey reached from the SMT-LIB
`Real`/`FloatingPoint` split, arrived at from a different direction.

**But three things stop that from being a clean answer.**

First, exactness in the sense of "base-10 with a fixed scale" is not exactness in this sense. Per
`research/architecture/compiler/exact-decimal-arithmetic-survey.md`, IEEE 754 decimal formats carry
subnormals and an underflow condition of their own — the survey's own table lists `underflow` with the
condition "result is subnormal and inexact" and outcome "subnormal or ±0". And a fixed-scale type like
`System.Decimal`, which that survey records as having no subnormals and no infinities, still has a
smallest representable magnitude set by its maximum scale. The prior divisor survey reproduced the failure
on Precept's `decimal` lane for that reason.

Second, an exact *integer* lane does not remove the case either, it just changes how zero arrives. EVA's
worked divisor example in Finding 3 is `int`, not floating-point: `100 / (x * y)`. In a wrapping
two's-complement integer type a product of two non-zero values can be zero by overflow. EVA emits alarms
for signed overflow separately —

> "By default, Eva emits alarms for — and reduces the sets of possible results of — signed arithmetic
> computations where the possibility of an overflow exists."
> — EVA User Manual, § 3.2

— but the divisor alarm is emitted on its own terms regardless, from the divisor interval containing zero.
Integer division is a second route: truncation toward zero turns a non-zero quotient into a stored zero
whenever the magnitude of the numerator is below the divisor's.

Third — and this is the point that carries — the underflow case is one instance of a more general one.
What actually creates it is *any* operation whose stored result can be zero when its inputs are not:
underflow, truncating division, overflow wraparound, and the cancellation case the prior survey found in
GNATprove's fixed-point example. The analyzers do not have four mechanisms for these. They have one: model
the stored result, propagate the interval, test the divisor interval for membership of zero. That single
mechanism covers all four without knowing which one it is looking at.

**The honest answer to the fourth question, then, is that the exactness of the lane is not the axis.** The
axis is whether the representation has a smallest positive magnitude and whether its operations can wrap
or truncate. Arbitrary-precision rationals and arbitrary-precision integers have neither, so they remove
the case; every fixed-width or fixed-scale representation has at least one, exact or approximate.

## Threats to Validity

- **Astrée was not run.** AbsInt's shipping Astrée is proprietary and was not exercised. Everything
  attributed to it here comes from published papers by the people who built it (ESOP 2005, PLDI 2003) and
  from Miné's ESOP 2004 domain paper, which describes the domains Astrée uses. The shipping tool may have
  moved on in twenty years, and the papers describe research versions.
- **IEEE 754 itself was not fetched.** The standard is behind a paywall. The normative definition of
  underflow, and the claim that decimal formats have subnormals, rest on Miné's restatement (Primary, but
  a restatement) and on the internal `exact-decimal-arithmetic-survey.md`. Grade the standards leg
  **Tertiary**. No claim above rests on it alone — Finding 1's `mf_f` is quoted from Miné's
  own definition, not from the standard.
- **Miné's model is stated for binary formats.** His `F` is "the set of all available formats" and the
  construction does not depend on the radix, but the paper's worked format is 32-bit binary. Extending
  `mf_f` to IEEE 754 decimal formats is inference on my part, corroborated by the internal decimal survey
  rather than by the source.
- **The magnitude-bound domains are research artifacts, not shipping features.** AVO and the donut domain
  were evaluated with the Interproc research analyzer. Neither is documented as being in Astrée, Frama-C
  or any production analyzer. Their cost in a real tool is not established by this survey.
- **Finding 5 is an absence, not a demonstrated impossibility.** I found no source composing a magnitude
  lower bound through a multiplication. I searched under several phrasings and read the AVO paper's
  constraint language closely enough to see that it does not have the shape, but a negative result from
  searching is weaker than a proof. If such a technique exists under a name I did not guess, Finding 5 is
  wrong in its strong form.
- **Four comparators still unexamined.** Polyspace, KeY, Fluctuat and CompCert/Verasco were not reached.
  Fluctuat is the one most likely to matter: it tracks the exact value and the accumulated rounding error
  as separate quantities (the Goubault–Putot line of work, which Miné explicitly contrasts with his own in
  § 5), and that separation is a plausible place for a different answer to Finding 5 to live.
- **Two-column PDF extraction.** The Astrée PLDI 2003 text extracted with columns interleaved and was used
  only for orientation; no excerpt above is taken from it. The other four extracted cleanly and were read
  in place.

## Implications for Precept

No design is proposed. What the findings bear on:

1. **Refusing a computed divisor that can underflow is what the field does, and it costs these analyzers
   nothing special to do it.** The interval domain reaches the correct answer without any dedicated
   underflow machinery: the product's interval contains zero because the product is zero, and the divisor
   test fails on that. Precept's current behaviour on this case matches the comparators exactly.
2. **The unsound escape the prior survey documented is the whole problem.** Nothing in the abstract
   interpretation literature has an equivalent of accepting a `nonzero` declaration on a computed field as
   a premise. In these systems there is nowhere to write such a claim about a computed value — the value's
   abstraction is derived, and a derived interval is not something the author can assert over.
3. **If a magnitude lower bound is ever wanted, the prior art exists, is named, and is solver-free** — but
   it delivers less than it sounds like it does. It carries `|x| ≥ k` past a branch that established it.
   It does not manufacture the bound from arithmetic, and the step from `|A| ≥ a ∧ |B| ≥ b` to a non-zero
   stored product needs a comparison against the format's smallest magnitude that no surveyed system
   performs.
4. **~~New language surface implication.~~ Withdrawn 2026-07-26 — this was wrong, and the correction
   changes where the work goes.** The claim was that Precept "has no way today to state a lower bound on
   magnitude", so expressing `|x| ≥ k` would need a new modifier and would therefore be language surface
   requiring an owner conversation. **`abs()` already exists** — `precept-language-spec.md:1571` and
   `primitive-types.md:611` both declare it over the primitive numeric lane, type-preserving. So an author
   can already write `when abs(Divisor) >= 0.01`, and it parses and type-checks.

   What is actually missing is narrower. Checked at HEAD 2026-07-26: a handler guarded
   `when abs(Product) >= 0.01` dividing by `Product` is **refused** — the divisor obligation comes back
   `Unresolved` and the file fails with `PRE0083`, while the same handler guarded `when Product != 0` or
   `when Product > 0` discharges by `GuardInPath`. So the guard is expressible and the proof engine does
   not read it: `abs(...)` in a guard establishes nothing about its argument.

   **That makes this build work against an existing language, not new surface**, and it does not need the
   pre-design conversation. It is still a statement about what the compiler does rather than what it
   should do, so what the guard *ought* to establish belongs to phase 5's discharge procedure — but the
   author-facing form the domain literature asks for is already in the grammar.

   The observation the original point rested on survives and is worth keeping: `nonzero` is a
   disequality, and `min`/`max` bound the value rather than its distance from zero, so a two-sided bound
   like `min -100 max 100` says nothing about how close to zero the value may be. The mistake was
   concluding from that that the language cannot express the property at all.

## Conclusions

**Conclusion 1: a standard interval domain models rounding, including underflow, and is sound at the
representation floor.**

- *Rationale*: Miné defines the smallest representable magnitude `mf_f` per format and adds it as an
  additive error term to every operation in the linearization; the plain interval domain gets the same
  result more directly by computing bounds with outward rounding in the concrete format. Neither reasons
  over exact reals.
- *Alternatives considered and rejected*: that these tools idealise floating-point as real arithmetic and
  are therefore unsound near zero — rejected, because Astrée's own paper names that idealisation as the
  unsound approach it declined to take, in those words.
- *Precedent*: Miné ESOP 2004 § 3.1 and § 5; Cousot et al. ESOP 2005 § *Floating-Point Interval Linear
  Form Abstraction*.
- *Tradeoff accepted*: the price of that soundness is imprecision. The `mf_f` term is added
  unconditionally, so even computations that never come near the floor carry the allowance.

**Conclusion 2: no surveyed solver-free analyzer proves a product of non-zero operands to be non-zero, and
none tries.**

- *Rationale*: the property is false, so a sound analysis cannot establish it. The interval containing zero
  is the correct answer, and the tools report it as an alarm at the division rather than as an underflow at
  the multiplication.
- *Alternatives considered and rejected*: that Astrée or Frama-C hold a better answer than SPARK did —
  which is what the prior survey guessed. Rejected on the evidence: they hold the *same* answer, reached
  without a solver, which makes it more transferable rather than better.
- *Precedent*: Miné's interval division returning the run-time error when `b⁻ ≤ 0 ≤ b⁺`; EVA's worked
  `100 / (x * y)` example emitting `assert (int)(x*y) ≢ 0`.
- *Tradeoff accepted*: this leaves genuinely safe programs unproved. That is the false-alarm cost the
  entire Astrée research programme is about reducing, and it is not reducible to zero in general.

**Conclusion 3: a magnitude-lower-bound domain exists under a name, but it propagates guards rather than
composing arithmetic.**

- *Rationale*: AVO infers and carries constraints of the form `|X| ≥ k`, and the SAS 2014 results table
  shows it discharging divisor obligations with them. But every case in that table originates in a test the
  programmer wrote, and the domain's constraint language has no rule relating `|X × Y|` to `|X|` and `|Y|`.
- *Alternatives considered and rejected*: treating the plain interval domain's `lo > 0` as a magnitude lower
  bound — rejected, because it only works for values known to be positive; for a signed value the property
  is non-convex, which is the paper's stated reason for building a new domain at all.
- *Precedent*: Chen et al. SAS 2014 § 1 and Fig. 10; Ghorbal et al. VMCAI 2012 § 1; the NECLA
  division-by-zero benchmark set.
- *Tradeoff accepted*: reporting this as "the technique exists" risks overstating it. It exists for the
  guard-propagation case, which is not the case this survey was commissioned about.

## What would change this conclusion

- **If Fluctuat, or any error-term domain in the Goubault–Putot line, turns out to derive a magnitude lower
  bound on a product from bounds on its factors**, Finding 5 and Conclusion 3's limitation are wrong, and
  the composed proof has prior art after all.
- **If a shipping analyzer is found that alarms on underflow itself** rather than tolerating it, Finding 2
  is wrong about the reporting model, and the "catch it at the division" design is a choice rather than the
  convention.
- **If AVO or the donut domain is found in a production analyzer with published cost figures**, the
  characterisation of magnitude-bound domains as research artifacts is wrong, and the cost question the
  prior survey left open becomes answerable from the literature rather than by building it.
- **If IEEE 754's normative text turns out to define decimal underflow differently from the binary case**
  in a way that matters, Finding 6's first point needs rework. The internal decimal survey says it does not,
  but the standard was not read.

## Open Questions

- What a magnitude lower bound would cost in Precept's prover specifically. The complexity class is
  established (AVO keeps the octagon domain's quadratic memory and cubic time), but Precept's per-keystroke
  language-server constraint — the one
  `research/architecture/compiler/solver-free-static-analysis-techniques-survey.md` weighs everything
  against — was not evaluated for AVO here.
- Whether Precept even wants the guard-propagation case that AVO actually solves. The surveyed programs
  protect a division with an explicit test; whether Precept authors write that shape, and whether the
  compiler currently carries such a test past the branch, was not investigated.
- Whether Fluctuat's separation of exact value from accumulated error is expressible without a solver, and
  whether it composes through multiplication. This is the single highest-value unexamined comparator.
- What the corresponding smallest representable magnitude is for Precept's `money` and `quantity` lanes —
  inherited from `decimal` or their own. Carried forward unchanged from the prior survey's open questions.

## Sources

- **Relational Abstract Domains for the Detection of Floating-Point Run-Time Errors**, Antoine Miné. ESOP
  2004, LNCS 2986, pp. 3–17. DOI 10.1007/978-3-540-24725-8_2. Primary. Accessed 2026-07-26 via arXiv
  preprint cs/0703077 (https://arxiv.org/pdf/cs/0703077). Mirrored to
  `research/references/solver-free-static-analysis/mine-floating-point-relational-domains-esop2004.txt`.
- **The ASTRÉE Analyzer**, Patrick Cousot, Radhia Cousot, Jérôme Feret, Laurent Mauborgne, Antoine Miné,
  David Monniaux, Xavier Rival. ESOP 2005, LNCS 3444, pp. 21–30. Primary. Accessed 2026-07-26.
  https://www.di.ens.fr/~cousot/publications.www/CousotEtAl-ESOP05.pdf. Mirrored to
  `research/references/solver-free-static-analysis/astree-analyzer-cousot-et-al-esop2005.txt`.
- **A Static Analyzer for Large Safety-Critical Software**, Bruno Blanchet, Patrick Cousot, Radhia Cousot,
  Jérôme Feret, Laurent Mauborgne, Antoine Miné, David Monniaux, Xavier Rival. PLDI 2003. Primary.
  Accessed 2026-07-26 via arXiv cs/0701193. Used for orientation only; no excerpt above is taken from it
  (two-column extraction interleaved).
- **An Abstract Domain to Infer Octagonal Constraints with Absolute Value**, Liqian Chen, Jiangchao Liu,
  Antoine Miné, Deepak Kapur, Ji Wang. SAS 2014, LNCS 8723, pp. 101–117. DOI
  10.1007/978-3-319-10936-7_7. Primary. Accessed 2026-07-26. https://www.di.ens.fr/~liu/papers/sas14.pdf.
  Mirrored to
  `research/references/solver-free-static-analysis/absolute-value-octagons-chen-liu-mine-kapur-wang-sas2014.txt`.
- **Donut Domains: Efficient Non-convex Domains for Abstract Interpretation**, Khalil Ghorbal, Franjo
  Ivančić, Gogul Balakrishnan, Naoto Maeda, Aarti Gupta. VMCAI 2012, LNCS 7148, pp. 235–250. DOI
  10.1007/978-3-642-27940-9_16. Primary. Accessed 2026-07-26 via HAL hal-01660908
  (https://hal.science/hal-01660908/document). Mirrored to
  `research/references/solver-free-static-analysis/donut-domains-ghorbal-et-al-vmcai2012.txt`.
- **Frama-C EVA User Manual (Evolved Value Analysis)**, CEA List / Inria. Primary (the tool's own
  versioned documentation). Accessed 2026-07-26.
  https://frama-c.com/download/frama-c-eva-manual.pdf. The excerpts quoted above are mirrored to
  `research/references/solver-free-static-analysis/frama-c-eva-manual-excerpts.txt`.
- **Apron: A Library of Numerical Abstract Domains for Static Analysis**, Bertrand Jeannet, Antoine Miné.
  CAV 2009. Primary. Already mirrored at
  `research/references/solver-free-static-analysis/apron-domains-mine-jeannet-cav2009.txt`. Cited here as
  the pointer that located the ESOP 2004 paper.
- **IEEE 754 floating-point standard.** **Not fetched — paywalled.** Its content is used only as restated
  by Miné (Primary) and by the internal decimal survey. Graded **Tertiary** as used here; see Threats to
  Validity.
- `research/architecture/compiler/overflow-in-solver-free-interval-domains.md` — the companion survey,
  same comparators and same interval endpoints, asking the mirror question about the top of the
  representation. Internal, Cited.
- `research/architecture/compiler/divisor-nonzero-under-finite-representation-survey.md` — the survey this
  extends. Internal, Cited.
- `research/architecture/compiler/proof-engine-interval-arithmetic-survey.md` — internal, covers Frama-C
  EVA and Astrée at the level of what they detect.
- `research/architecture/compiler/exact-decimal-arithmetic-survey.md` — internal; representation mechanics,
  including IEEE 754 decimal subnormals and `System.Decimal`'s fixed scale. Cited, not repeated.
- `research/architecture/compiler/solver-free-static-analysis-techniques-survey.md` — internal; the
  cost-and-expressiveness ranking this survey's Open Questions defer to.
- `docs/language/precept-language-spec.md` §0.8 — internal canon; the exclusion of solver-style proof
  artifacts that sets this survey's scope.
