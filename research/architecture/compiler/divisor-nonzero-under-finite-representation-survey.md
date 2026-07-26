---
status: Cited
authored: 2026-07-26
author: research pass commissioned by owner Shane via /research
topic: how systems that prove division safety handle arithmetic on provably non-zero operands producing exactly zero, and whether the numeric representation removes the problem
external-engagement: partial
---

# Proving a divisor non-zero when the arithmetic can produce zero from non-zero parts

> Two values that are each provably non-zero can multiply to exactly zero, because the true answer is
> too small for the type to hold. This survey collects what other verification systems do about that,
> and what the choice of numeric representation does to the problem. It proposes no Precept decision.

## Background

Precept's proof engine discharges a divide-by-zero obligation by finding something that establishes the
divisor is non-zero. The open question is whether that something must be attached to the divisor itself,
or whether the compiler may work it out from the parts the divisor is computed from.

The question is live rather than theoretical. `docs/language/primitive-types.md:667` already records it,
in canon's own voice:

> "**Open, not covered by either arm — underflow.** Two non-zero finite `number` operands can multiply
> to exactly `0.0`. That is neither `NaN` nor an infinity, so neither case above reaches it, and it
> breaks divisor safety directly: `nonzero` on every operand does not yield a non-zero product. Recorded
> as an open question, not yet routed."

It was raised again at the phase-1 walk of 2026-07-26 (item 14), where the owner confirmed it had never
been put to him. This survey is the preparation for that decision.

## Methodology

**Research question.** How do systems that statically prove division safety handle the case where
arithmetic over provably non-zero operands yields exactly zero — and does the numeric representation
make the case disappear rather than answering it?

**Search strategy.** Internal first, per the skill's triage step: `research/` for prior coverage,
`docs/language/` and `docs/compiler/` for a locked decision. Then external: AdaCore's SPARK User's Guide
(the closest comparator, being a prove-or-reject system with explicit division checks), the SMT-LIB
FloatingPoint theory declaration, the Flocq/Why3 literature on gradual underflow, and Goldberg's survey
as the standard reference on the hazard class.

**Inclusion / exclusion.** In scope: systems that *prove* a divisor non-zero ahead of execution. Out of
scope: systems that trap at runtime, and the general floating-point error-bound literature except where
it bears on reaching exactly zero. Deliberately excluded: Precept-specific recommendations.

**Source-grade mix.** Two Primary external (SPARK User's Guide 27.0w, SMT-LIB FloatingPoint theory),
one Primary external reached only in part (Goldberg, ACM Computing Surveys 1991), one Tertiary
(search-result summary of the Flocq literature, after the primary PDF failed to parse), and internal
sources graded separately below.

**Time bounds.** External fetches 2026-07-26. Internal citations verified against HEAD the same day.

## Findings

### 1. Canon has already recorded the problem and left it open

Internal, Primary. `docs/language/primitive-types.md:667`, quoted in full under Background. The relevant
detail for a decision: canon states the consequence — "`nonzero` on every operand does not yield a
non-zero product" — and explicitly declines to route it.

### 2. Precept reproduces the failure today, and the natural author fix makes it worse

Internal, Primary — reproduced via `precept_compile` at HEAD on 2026-07-26 and recorded on BUG-048.

Dividing by a computed product is **correctly refused**:

```precept
field A as decimal nonzero default 0.0000000000000001 editable
field B as decimal nonzero default 0.0000000000000001 editable
field Product as decimal <- A * B
```

> `PRE0083` — Division is unsafe: 'Product' can be zero in event handler 'Compute'

Adding `nonzero` to the computed field — the response the diagnostic invites — makes the same file
compile with **zero diagnostics** and the divisor obligation `Proved` by `DeclarationAttribute`, while
`1e-16 × 1e-16` is `1e-32`, below `decimal`'s smallest magnitude of about `1e-28`. So the strict rule is
already in force in the implementation, and the escape from it is unsound.

**This is not confined to the approximate lane.** Canon records the case for `number`; the reproduction
above is `decimal`, the exact lane. Any representation with a smallest positive magnitude has it.

### 3. SPARK/GNATprove: division checks are proved, and float reasoning is called out as the hard case

[Primary; SPARK User's Guide 27.0w; accessed 2026-07-26]

GNATprove generates a proof obligation that the divisor differs from zero, and models floating-point
arithmetic as IEEE 754 with a specific rounding mode rather than as real arithmetic:

> "SPARK considers rounding on floating point arithmetic operations to follow Round-Nearest-Even (RNE)
> mode, where a real result is rounded to the nearest floating point value, and ties are resolved to the
> floating-point with a zero in the last place."
> — *Semantics of Floating Point Operations*, SPARK User's Guide 27.0w

The guide is explicit that this is where automatic proof gets hard, and — the part that bears directly
on Precept's question — that the difficulty is precisely about needing more than coarse bounds:

> "Some checks involving floating-point computations are particularly difficult to prove automatically,
> in particular when they don't depend simply on computing coarse static bounds for the result of
> computations. This is related to the weaker support for floating-point arithmetic in provers, compared
> to the support of integer arithmetic."
> — *How to View GNATprove Output*, SPARK User's Guide 27.0w

**What this establishes.** A mature prove-or-reject system in this space does not treat "both operands
are non-zero" as discharging a divisor obligation. It reasons over the rounded result, which means the
only route to proving a computed divisor non-zero is a bound on its magnitude — and the guide names that
class of reasoning as the difficult one rather than the routine one.

### 4. The solver layer distinguishes reals from floats, and Precept's question only exists on the float side

[Primary; SMT-LIB FloatingPoint theory declaration; accessed 2026-07-26]

The theory declares multiplication and division as taking an explicit rounding mode:

```
(fp.mul RoundingMode (_ FloatingPoint eb sb) (_ FloatingPoint eb sb) (_ FloatingPoint eb sb))
(fp.div RoundingMode (_ FloatingPoint eb sb) (_ FloatingPoint eb sb) (_ FloatingPoint eb sb))
```

with semantics referred out to the accompanying technical report rather than stated inline. The
significant part is structural: an operation that takes a rounding mode is an operation whose result is
the *rounded* value, so a solver asked whether `x * y` can be zero given non-zero `x` and `y` answers
**yes** under `FloatingPoint` and **no** under `Real`.

That distinction is not new to this project's research. `research/language/rational-vs-decimal-exact-numeric-representation.md`
§ Area 5 already establishes the algebraic fact and its consequence for provers [internal; Cited]:

> "The pivotal algebraic fact: **rationals are closed under +, −, ×, ÷ (except ÷0); base-10 decimals are
> not closed under ÷.** … **This is why the verification field reasons over exact rationals, not
> decimals:** the SMT-LIB theory of Reals interprets the sort as 'the set of all real numbers' … Z3 and
> other solvers implement `Real` with exact rational arithmetic … precisely because closure under the
> field operations makes the arithmetic sound without rounding."

### 5. The representation answer, stated plainly

Combining 3 and 4: **the question is a property of representations with a smallest positive magnitude,
not a property of division safety.**

| Representation | Can non-zero × non-zero be zero? | Why |
|---|---|---|
| IEEE 754 binary/decimal float | Yes | Gradual underflow; the product rounds into the subnormal range and then to zero |
| Fixed-scale decimal (`System.Decimal`, `BigDecimal` with a scale) | Yes | Smallest representable magnitude; reproduced above at `1e-28` |
| Arbitrary-precision rational (`p/q` with big integers) | **No** | Closed under multiplication: a product of non-zero rationals is a non-zero rational, and there is no smallest positive value to fall below |
| Arbitrary-precision integer | **No** | Same closure argument; no fractional underflow exists |

So a representation change does not *answer* the question — it removes the case. That is why this survey
was commissioned alongside the open arithmetic-model choice for `integer`, and the two are closer to one
decision than two on this specific point.

### 6. The hazard class is long-documented

[Primary; Goldberg, *What Every Computer Scientist Should Know About Floating-Point Arithmetic*, ACM
Computing Surveys 23(1), 1991; accessed 2026-07-26 via Oracle's hosted reprint]

Goldberg's worked case is subtraction rather than multiplication, but it makes the same point about
ordinary-looking operands:

> "The numbers x = 6.87 × 10⁻⁹⁷ and y = 6.81 × 10⁻⁹⁷ appear to be perfectly ordinary floating-point
> numbers, which are more than a factor of 10 larger than the smallest floating-point number
> 1.00 × 10⁻⁹⁸. They have a strange property, however: x ⊖ y = 0 even though x ≠ y!"

The relevance is that "the operands looked fine" is not evidence, and has been known not to be evidence
for thirty-five years.

## Threats to Validity

- **The external evidence is thin, and thinner than the internal evidence.** Six external calls. Two
  comparator systems reached in a useful way (SPARK, SMT-LIB). Frama-C/ACSL, Astrée, Polyspace, KeY and
  CompCert/Verasco were not examined at all, and any of them could hold a materially better answer —
  particularly Astrée, which is built for exactly this class of numerical property.
- **One source failed to parse.** Melquiond's *Some Formal Tools for Computer Arithmetic: Flocq and
  Gappa* returned an unreadable PDF. The claim that verification tools model gradual underflow exactly
  (rather than idealising it away) therefore rests on a search-result summary — **Tertiary** — and is
  not carried as load-bearing anywhere above. It should be upgraded before anyone relies on it.
- **Goldberg's example is subtraction, not multiplication.** It is cited for the hazard class, not as
  direct evidence of the multiplication case. The multiplication case rests on the internal
  reproduction (strong, but internal) and on elementary arithmetic about a smallest representable
  magnitude.
- **SPARK is an adjacent problem, not the same one.** It proves properties of imperative Ada code, with
  a solver behind it. Precept refuses without a solver and reports to a domain expert. The finding that
  transfers is what the *reasoning* requires — a magnitude bound rather than a non-zero fact — not the
  tooling around it.
- **No usability evidence.** Nothing here says what a domain expert would find tolerable to write, which
  is half of the decision.

## Implications for Precept

1. **"Non-zero times non-zero is non-zero" is not available on any lane Precept has today**, exact or
   approximate. Nothing in the surveyed material treats it as sound, and Precept's own compiler already
   refuses it.
2. **The alternative that other systems use is a magnitude lower bound** — knowing the operands cannot
   be smaller than some value, so the product cannot fall below the representable floor. SPARK's guide
   names this class of reasoning as the difficult one, which is a cost signal rather than a prohibition.
3. **The case disappears under an exact rational or arbitrary-precision integer representation**, which
   ties this question to the open arithmetic-model choice. Deciding the representation first would make
   this question moot for the lanes it covers; deciding this question first does not constrain the
   representation.
4. **The unsound escape has to close regardless.** A modifier on a computed field being consumed as a
   premise (BUG-048) is a defect under every option here, and it is currently the path the diagnostics
   steer an author towards.

## Conclusions

**This survey proposes no decision** — the owner asked for prior art, and the two candidate answers turn
on a cost judgement and a usability judgement that the material above does not settle. The one thing it
does establish firmly enough to act on:

- **Conclusion: reasoning from operand non-zeroness to product non-zeroness must not be adopted.**
  - *Rationale*: it is false on every representation Precept currently has, and canon already records
    the counterexample.
  - *Alternatives considered and rejected*: adopting it with a caveat for "large enough" operands —
    rejected because that is a magnitude bound wearing a non-zero fact's clothes, and should be built as
    a magnitude bound if it is built at all.
  - *Precedent*: SPARK models rounded results rather than real ones and proves the divisor check
    against them; SMT-LIB separates `Real` from `FloatingPoint` for exactly this reason.
  - *Tradeoff accepted*: authors who divide by a computed value must state something the compiler can
    use, which is more writing than the intuitive rule would require.

## What would change this conclusion

- If Astrée, Frama-C or Polyspace turn out to discharge divisor obligations from operand non-zeroness
  under a documented soundness argument, the claim that no comparable system does this is wrong.
- If Precept adopts an exact rational or arbitrary-precision representation for the affected lanes, the
  case stops existing and the conclusion becomes vacuous rather than wrong.
- If a magnitude-lower-bound analysis turns out to be cheap to build and easy to explain in a
  diagnostic, the framing of this as a costly alternative is wrong, and the two options collapse into
  one.

## Open Questions

- What a magnitude lower bound costs to build in Precept's prover, and whether it can be explained in a
  diagnostic a domain expert can act on.
- Whether the four unexamined comparators (Astrée, Frama-C/ACSL, Polyspace, KeY) hold a better answer.
- Whether the `money` and `quantity` lanes inherit `decimal`'s floor exactly or have their own.

## Sources

- **Semantics of Floating Point Operations**, SPARK User's Guide 27.0w, AdaCore. Primary. Accessed
  2026-07-26. https://docs.adacore.com/spark2014-docs/html/ug/en/appendix/semantics_of_floating_point_operations.html
- **How to View GNATprove Output**, SPARK User's Guide 27.0w, AdaCore. Primary. Accessed 2026-07-26.
  https://docs.adacore.com/spark2014-docs/html/ug/en/source/how_to_view_gnatprove_output.html
- **SMT-LIB Theory of FloatingPoint**, SMT-LIB initiative. Primary. Accessed 2026-07-26.
  https://smt-lib.org/theories-FloatingPoint.shtml
- **What Every Computer Scientist Should Know About Floating-Point Arithmetic**, David Goldberg, ACM
  Computing Surveys 23(1), 1991. Primary. Accessed 2026-07-26 via Oracle's hosted reprint.
  https://docs.oracle.com/cd/E19957-01/806-3568/ncg_goldberg.html
- **Some Formal Tools for Computer Arithmetic: Flocq and Gappa**, Guillaume Melquiond. **Fetch failed —
  PDF unparseable.** Tertiary as used. https://guillaume.melquiond.fr/doc/21-arith-article.pdf
- `research/language/rational-vs-decimal-exact-numeric-representation.md` § Area 5. Internal, Cited.
- `research/architecture/compiler/exact-decimal-arithmetic-survey.md`. Internal.
- `docs/language/primitive-types.md:667`. Internal canon.
- `docs/Working/bugs.md` BUG-048, second symptom. Internal, reproduced at HEAD 2026-07-26.
