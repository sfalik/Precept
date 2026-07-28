# Recommendation for overflow, underflow, and divisor non-zeroness

**Prepared for Shane, 2026-07-26. This is an architectural recommendation, not a locked design. It
combines the two earlier phase-5 notes with the new finite-representation and solver-free-analysis
research.**

> ## Status: recommendation under active revision — not a locked design, and not a build authorization
>
> **Nothing in this document authorizes implementation, and its build items do not constitute an agreed
> build order.** Before any of it is built it has to go through `/design` and come out the other side as a
> locked design document — with the Philosophy Alignment matrix, the Language Design Grounding and
> Architecture Grounding, stakes-classified decisions, and the four-leg rationale (rationale, alternatives
> rejected, precedent, tradeoff accepted) on every decision that the design process requires and this
> document does not carry. Read the eight build items as a description of the work the model implies, not
> as a plan anyone has signed.
>
> **Several decisions in here are the owner's and are not taken.** They are listed in
> [Work that would require owner consultation](#work-that-would-require-owner-consultation-before-it-is-built)
> and it is the section to read before treating any part of this as settled. The overflow half in
> particular sits behind the item-15 deferral and is described here as a target, not as scheduled work.
>
> **Corrections applied 2026-07-27 after review.** A citation error, a withdrawn claim, a self-contradiction
> and two missing constructs were found and fixed; each correction says in place what it replaced and why.
> The substantive corrections were: the not-`NaN` clause on the `number` lane is **derived and forced, not
> an owner ruling**, and the modifier-semantics half of build item 8 goes through the pre-design
> consultation gate rather than being catalog sync; the claim that the `rule` route does not reach the
> `number` lane is **withdrawn** — it does, and what fails is spelling a fractional threshold there; the
> underflow section is **preparation for a question reserved to the owner**, not an answer to it; `ensure`
> is added as an eighth divisor form; and the `duration` lane is explicitly carved out so canon's
> compound-divisor concession is not overturned by implication.

**What this document is for.** It describes where the divisor and overflow model should end up, what an
author should write to get there, and what has to be built in what order. It is not a report on the
current compiler. Today's behavior appears only where it changes the size or the ordering of the work —
a clause, not a demonstration. The gaps are taken as read.

**Revised 2026-07-26 after Shane asked whether a `rule` could state a threshold `maxplaces` cannot.** It
can. A declared unguarded rule over the divisor field is added as form (g) and rewrites authoring pattern
3, which previously offered only a repeated guard or a declared interval for a non-power-of-ten minimum.
Nothing else in the recommendation changes: the rule carries exactly the same duty to be established and
preserved as a field modifier does, and it needs no new syntax.

**Revised again the same day after Shane observed that the document used the research's conclusions
without naming the techniques they came from.**
[The proof techniques the research surveyed](#the-proof-techniques-the-research-surveyed-and-which-ones-precept-should-use)
now lists every technique the surveys enumerated, what each costs, which ones Precept should adopt, which
are deferred, and which are declined outright — with a table connecting each build item to the technique
underneath it. It changes no recommendation; it makes the choices already implicit in the document
visible as choices.

**Expanded 2026-07-26 after Shane observed that overflow got light coverage while divisor non-zeroness
got a worked example, seven authoring forms, patterns and a technique catalog.** [Overflow
policy](#overflow-policy) is now a section of its own with the same structure: a definition that fails,
the obligations named at each of its four operations, four authoring patterns, its own forms table, and
an explicit account of why the field's precision remedies do not transfer. Three things in it are new
rather than restated. A rule over the *result* of an operation cannot discharge an overflow obligation
even though a rule over a divisor field discharges a divisor obligation, because the computation runs
before rules are evaluated — so the rule construct transfers only to the operands. The overflow
obligation is far more tolerant of a loose bound than the divisor obligation is, which changes where
precision effort is worth spending. And the owner's item-15 ruling defers overflow past the MVP, which
changes when the build items are scheduled but not what they should be.

**Expanded again 2026-07-26 after Shane observed that the document had reasoned almost entirely about
`decimal`, and asked for `Infinity` and `NaN` to be treated as hazards in their own right rather than as
edges of the overflow question.** [The `number` lane](#the-number-lane) works through what the
recommendation means on the approximate lane, and gives each of the two non-finite values its own
treatment. Most of the recommendation carries over unchanged, and the underflow policy routes an open
question canon had recorded against that lane and left unrouted — routes it, rather than settles it; the
cost half is reserved to the owner, see [Underflow policy](#underflow-policy).

**The soundness finding is real, and it is stated plainly in the section rather than softened.**
Precept's `!=` on `number` is IEEE comparison — the operation catalog says so in its own description of
the operation — and under IEEE, `NaN != 0` is true. So a guard of the form `when ComputedRate != 0`,
which this document recommends for `decimal` as sound evidence, admits a `NaN` divisor on the `number`
lane. `decimal` has no `NaN`, so this has no counterpart there and the `decimal` recommendation is
unchanged. The `abs()` magnitude guard is unaffected, because every ordered comparison against a `NaN` is
false and so refuses it for free — which reverses the ordering the rest of the document gives between the
two guard forms. Canon's non-finite position, recorded at `primitive-types.md:660` and dated 2026-07-21,
anticipated the hazard and closed it for field modifiers; it did not close it for guards, and it has not
yet been written into the catalog. That position is **derived and forced rather than owner-ruled** — see
[What canon already fixes about `number`](#what-canon-already-fixes-about-number) for the provenance and
for what follows from it.

Three further things are different on that lane. Overflow produces `+∞` rather than refusing an
operation, so the model has to describe what follows rather than the case that did not happen, and a
single overflow can present four operations later as a zero or a `NaN`. Excluding a `NaN` is a claim no
interval can carry, because a `NaN` satisfies no bound and violates none, so it needs a requirement
family and a diagnostic of its own — today there is one code for the family, `NumericOverflow`, and it
describes magnitude. And two of the eight divisor forms rest on `maxplaces`, which `number` does not
have, so the `rule` and `ensure` forms carry more weight there. (The rule route does reach that lane —
verified at HEAD — and an earlier revision of this document said it did not.)

---

## Recommendation in brief

Precept should reason about the value that the runtime actually stores, not the corresponding
mathematical real.

For arithmetic chains, that means:

1. Every numeric operation must propagate an interval using the representation rules of its result
   lane, including rounding, underflow to zero, and the maximum representable value.
2. Possible overflow must become an operation-level proof obligation and must be refused at compile
   time. What discharges it is upper and lower bounds on the operands, declared by the author because
   the domain has them. There is no equivalent of the divisor's guard-at-the-division-site, because by
   the time an operation could be guarded it has already had to produce its result. The whole of
   [Overflow policy](#overflow-policy) works this through.
3. Underflow should not initially be a separate fault. The analysis should admit zero into the result
   interval and let later obligations, constraints, and guards decide whether zero is allowed there.
4. A field's `nonzero` modifier must never be accepted as an unproved premise. Whatever writes the field
   must establish the constraint, and every other write must preserve it.

For division, Precept should require evidence about the **final stored divisor**, not infer safety from
the non-zeroness of the values used to compute it. The accepted evidence should be:

- a non-zero literal;
- a final-value interval wholly above or below zero;
- a path guard on the final divisor, such as `Divisor != 0`, `Divisor > 0`, or
  `abs(Divisor) >= 0.0001` — the last two being stronger than the first on the `number` lane, where a
  disequality is true for a non-finite value and an ordered comparison is false for one;
- a governed constraint on a stored divisor — including `nonzero maxplaces N` on a field that an
  explicit `set` writes before anything divides by it — once establishment at each write and
  preservation across every other write are implemented;
- a declared unguarded `rule` over the divisor field, such as `rule abs(EffectiveRate) >= 0.05
  because "…"`, under exactly the same establishment-and-preservation duty. A rule states an arbitrary
  threshold directly, which no field modifier can do.

That last form deserves more prominence than the earlier notes gave it. When the same divisor is used in
several places, declaring the constraint once on the field it is stored in is clearer and less
error-prone than repeating a threshold guard at every division. The section
[Authoring patterns](#authoring-patterns-and-when-each-one-fits) sets out when to choose it.

A product raises a further question, and the answer divides into two cases that the earlier notes ran
together.

Where each factor's own declared bounds hold it to one side of zero, ordinary interval multiplication
settles the product with no new proof rule at all: `[0.01 .. 10000] * [0.5 .. 4]` is `[0.005 .. 40000]`,
which excludes zero. That case comes free with representation-aware interval transfers, and it means a
derived field can carry `nonzero` provably — the fix is to constrain the factors, not to abandon the
form.

Where the factors may take either sign and are known only to be some distance away from zero, the needed
step is `|A| >= a and |B| >= b therefore |A * B| >= a * b`, carried through a domain that can represent
a hole around zero. That inequality is true. Precept should still decline to build it in the first
implementation, for reasons of scope and rarity rather than any doubt about its validity — see
[Composing magnitude bounds through a product](#composing-magnitude-bounds-through-a-product).

This keeps the proof rule sound, gives authors an honest way to state a domain minimum, and does not let
`maxplaces` stand in for a proof on its own. A field carrying `nonzero maxplaces N` is a real domain
contract and good evidence — but only once every write into the field has been proved to satisfy it.

## Why this is the right line

### The analyzers model stored arithmetic

The solver-free survey found that Astrée and Frama-C EVA do not idealize finite arithmetic as exact
real arithmetic. Miné's interval model defines the smallest positive representable value for each
format and includes possible underflow in every arithmetic transfer. Plain intervals reach the same
answer by calculating bounds with the concrete format and outward rounding.

The practical consequence is simple: when tiny non-zero operands produce stored zero, zero appears in
the computed interval. A later division is refused because the divisor interval contains zero.
Underflow needs no special inference rule to make that happen
([underflow-in-solver-free-interval-domains.md, Findings 1–3](../../../research/architecture/compiler/underflow-in-solver-free-interval-domains.md)).

Those tools generally do not report underflow itself. They tolerate the rounded value and report the
operation for which zero is forbidden. That approach is a good first implementation for Precept too.
It avoids rejecting harmless tiny calculations while remaining strict at division and at any governed
`nonzero` boundary.

Precept differs from EVA and Astrée in consequence: they emit an alarm and continue, while Precept
refuses a definition it cannot prove safe. The arithmetic question is nevertheless the same: does the
set of possible stored divisor values contain zero?

### Other proof systems require a fact about the complete divisor

The finite-representation survey found the same rule in SPARK/GNATprove. A non-zero declaration on an
operand does not establish that a computed divisor is non-zero. Its worked fixed-point example suggests
a precondition on the complete expression, `(X + Y) /= 0`, and gives the cancelling values as a
counterexample.

SMT-LIB makes the representation distinction explicit: multiplication in the floating-point theory
takes a rounding mode, while multiplication over mathematical reals does not. A sound answer depends on
which one the program executes
([divisor-nonzero-under-finite-representation-survey.md, Findings 3–5](../../../research/architecture/compiler/divisor-nonzero-under-finite-representation-survey.md)).

The recommendation therefore should not be “improve the non-zero algebra.” The algebra is not the
problem. The stored representation invalidates the proposed implication.

### Composing magnitude bounds through a product

**Revised 2026-07-26 after Shane's third reading.** The earlier version of this section was headed
"Magnitude domains help with guards, not with composing products" and treated the absence of prior art
as though it settled the question. It does not, and the correction matters for what an author can write.

The solver-free survey found two non-convex domains that can represent a value being bounded away from
zero: absolute-value octagons and donut domains. The absolute-value octagon form can carry facts such as
`|x| >= 0.1` and use them to clear a divisor check. The surveyed examples obtain that fact from a test in
the program, and no surveyed analyzer performs the step

```text
|A| >= a and |B| >= b
therefore the stored |A * B| >= a * b
```

([underflow-in-solver-free-interval-domains.md, Findings 4–5](../../../research/architecture/compiler/underflow-in-solver-free-interval-domains.md)).

**That finding is about what has been built, not about what is true.** The inequality itself is valid:
`|A * B| = |A| * |B| >= a * b` for non-negative `a` and `b`, with no side conditions, in exact
arithmetic. Nothing in the survey contradicts it, and it should not be described as unsound.

Two things explain why the tools do not carry it. First, when both factors sit on one side of zero,
plain interval multiplication already produces a result interval that excludes zero, so the composition
adds nothing to the case that occurs most. Second, the case where it *would* add something — factors of
either sign, known only to be some distance from zero — needs a domain that can hold a hole around zero,
and most analyzers do not have one. Rarity plus a missing domain, not invalidity.

#### What the two cases mean for Precept

**Sign-definite factors need no new rule.** If `TariffBase` is declared `min 0.01 max 10000` and
`FuelMultiplier` is declared `min 0.5 max 4`, the representation-aware transfer for multiplication
already yields `[0.005 .. 40000]` for the product. That interval excludes zero, so a `nonzero` claim on
a field derived from that product is discharged by machinery already listed as build items 2 and 4. This
is worked through as the second case of
[form (c)](#the-eight-forms-and-what-each-one-contributes) below, and it is the answer to "what would the
author have to change" for the great majority of real definitions: constrain the factors.

**Signed factors with a hole around zero need the composition, and it needs three side conditions.** If
the rule is ever built, a sound version cannot be the bare inequality. The stored product is what
matters, so:

1. **No underflow.** `a * b` must be at least the smallest positive magnitude the result lane can store.
   On `decimal` that is `1e-28`. If `a * b` falls below it, the exact product can round to stored zero
   and the floor is lost.
2. **The floor must itself be representable at the result lane, or be taken down to the nearest
   representable value below it.** Rounding to nearest is monotone, so if `a * b` is exactly
   representable then any exact product at or above it stores at or above it. If `a * b` is not
   representable, the conservative floor is the greatest representable value below `a * b`. For floors
   that come from `maxplaces` this is automatic: `10^-p * 10^-q = 10^-(p+q)` is exact on the `decimal`
   lane for `p + q <= 28`.
3. **The overflow obligation is separate and still applies.** A lower magnitude bound on each factor says
   nothing about the upper end. Two large `decimal` values multiply past `7.9e28` and throw. The
   composition rule discharges the divisor obligation only; the representability obligation of build
   item 5 has to be discharged by upper bounds on the factors independently.

I still recommend implementing guard propagation first and leaving the signed-hole composition unbuilt
for now. That ordering is unchanged. What changes is the reason recorded for it: the rule is deferred
because it is second in line and serves an uncommon shape, not because it would be a false proof.

## The proof techniques the research surveyed, and which ones Precept should use

**Added 2026-07-26 after Shane observed that this document uses conclusions drawn from the research
without ever saying, in one place, which techniques were considered and which are being chosen.** The
sections above argue the policy; this one names the machinery, so a reader can see the choice as a choice.
It does not repeat the arguments — where a technique's case is already made above, it points there.

Four research documents feed this. The technique-space survey enumerated the candidates up front from
Apron's domain lattice and the decision-procedure literature rather than from whatever the surveyor
already knew, and scored every one against two hard requirements the spec sets: no opaque solver, with a
proof a person can read (§0.6 #3), and a worst case predictable enough to run on every keystroke. The
two divisor surveys and the overflow survey then asked which of them bear on this particular problem.

- [solver-free-static-analysis-techniques-survey.md](../../../research/architecture/compiler/solver-free-static-analysis-techniques-survey.md)
  — the enumeration, the cost bands, and the ranking.
- [divisor-nonzero-under-finite-representation-survey.md](../../../research/architecture/compiler/divisor-nonzero-under-finite-representation-survey.md)
  — what a divisor proof needs, from SPARK and the SMT-LIB theory declarations.
- [underflow-in-solver-free-interval-domains.md](../../../research/architecture/compiler/underflow-in-solver-free-interval-domains.md)
  — how the solver-free analyzers model rounding to zero, and the two non-convex domains.
- [overflow-in-solver-free-interval-domains.md](../../../research/architecture/compiler/overflow-in-solver-free-interval-domains.md)
  — the mirror question at the top of the representation, and the precision remedies.

### The requirements that decide it

Two, both from canon, and between them they eliminate most of the candidate list before precision is
even considered.

- **No opaque solver, and the proof has to be readable.** The spec excludes SMT and theorem-prover
  proof artifacts by name (§0.6 #3, §0.8). This is not a cost judgment — it is the inspectability
  commitment. A technique fails here if its answer cannot be shown to a domain expert as a chain of
  the declarations that forced it.
- **A worst case that does not depend on the data.** The whole pipeline reruns after every keystroke —
  about 44 ms for the 77-file corpus, about 3 ms for the worst single file. What matters is not average
  speed but that no input can stall it. A technique whose worst case is exponential is disqualified even
  if it is fast on everything anyone has tried, because the stall arrives on someone's real definition.

There is one thing that makes Precept's position easier than the analyzers it is compared with:
Precept's execution model forms no loops, so there is no fixpoint to converge and no widening operator is
needed. Every domain below is judged on its per-operation cost alone.

### What was surveyed, what each is for, what it costs

Grouped as the research groups them: numeric domains, decision procedures used readably, and structural
techniques that combine or reuse what is already there. The cost column is the per-operation worst case,
with `n` the number of numeric fields in one precept — typically well under fifty, and bounded by the
file rather than by anything at run time.

**Numeric domains.**

| Technique | What it holds | What it is good for | Cost |
|---|---|---|---|
| **Intervals** | `v` lies in `[lo .. hi]`, one field at a time | every bound question this document raises: overflow against the type limit, a divisor interval that excludes zero, a product of sign-definite factors. Already shipped | linear space, constant-time transfer — safe |
| **Zones / difference-bound matrices** | `x − y <= c` | one field bounded against another; also the readable test for whether a set of constraints is contradictory, which is a shortest-path calculation | cubic closure in `n` — safe |
| **Octagons** | `±x ± y <= c` | sums and sign-mixed pairs. Astrée's workhorse; substantially what Precept's depth-1 relational narrowing already approximates | cubic closure in `n` — safe |
| **Absolute-value octagons (AVO)** | adds `±\|x\| ± \|y\| <= c`, so `\|x\| >= k` is expressible | exactly this document's problem: a value held away from zero on both sides, which no interval can say. Built for divisor checks and benchmarked on a division-by-zero suite | same class as octagons — quadratic memory, cubic time, no solver |
| **Donut domains** | a convex set minus a convex hole | the same magnitude property by a different construction: an over-approximation of what is reachable, minus an under-approximation of what is impossible | benchmarked against AVO on the same suite; AVO clears one program it does not |
| **Two-variable-per-inequality (TVPI)** | `a·x + b·y <= c`, arbitrary coefficients, two fields | relations octagons cannot state because they are unit-coefficient only, such as `x <= 3·y` | strongly polynomial over rationals; the integer-exact version is NP-complete and has to be approximated — borderline |
| **Karr affine equalities** | `a₀ + Σaᵢxᵢ = 0`, any number of fields | linear *equalities* — the three-field balance identity `A + B = C` that intervals and octagons structurally cannot represent | linear in program size, polynomial in field count — safe |
| **Linear congruences** | `x ≡ c (mod m)` | divisibility and modulo facts | polynomial — safe, but thin demand |
| **Convex polyhedra** | arbitrary linear relations over any number of fields | everything the others do and more | **exponential worst case.** The reference implementation ships a timeout and falls back to zones when it blows up |

**Decision procedures that can be used readably.**

| Technique | What it decides | Why it is readable | Cost |
|---|---|---|---|
| **Congruence closure** | equality and disequality between terms | it is a union-find computation, and the proof-producing variant returns the actual chain of equalities that forced the answer — not a solver trace | `O(n log n)` — safe |
| **Difference-logic decision** | whether a set of `x − y <= c` constraints can all hold at once | the answer when they cannot is a specific cycle of constraints that contradict each other, which is a readable statement | cubic — safe |
| **String fragments** | length, prefix, automaton membership | length intervals already ship and are readable; automaton products are not, and can blow up | length safe, general string decision risky |

**Structural and combining techniques.**

| Technique | What it does | Cost |
|---|---|---|
| **Reduced product** | run two cheap domains side by side and let each tell the other what it learned, instead of building one big domain. Each keeps its own readable proof | safe if both parts are |
| **Affine arithmetic** | tracks how two computed values are correlated, which plain intervals lose | safe on finite expression trees; helps only where correlation loss is what caused a false refusal |
| **Incremental aggregates** | maintains a running bound on a collection's count or sum across mutations, from per-operation deltas | constant per mutation — safe, and already shipped for counts |
| **Predicate abstraction** | tracks the truth of a fixed set of conditions | **its abstraction step classically calls a theorem prover.** The solver-free version of it is what guard narrowing already does |
| **Dominator-based propagation** | a fact established on every path reaching a point is true at that point; reuses dominators the graph analyzer already computes | near zero, the structure exists |
| **Backward propagation of a requirement** | pushes an obligation back from where it fails to the input that should have carried the bound, which is what makes a diagnostic name the fix | linear — safe |

**Two remedies for imprecision, from the overflow survey, that are about the bound rather than the check.**
The check against a type limit is trivial in every one of these systems; what makes an overflow verdict
wrong is an interval wider than the values the program can really take. Astrée's named answers are
**widening with thresholds** — when a loop will not converge, jump to the next candidate bound from a
precomputed staircase rather than to infinity — and **variable packing**, which runs octagons only over
small automatically-chosen groups of variables, averaging three or four to a group, so the cubic cost is
paid over tiny sets. Neither transfers directly: widening exists to terminate loops and Precept has none,
and packing is a cost control for programs with thousands of variables, not for a definition with forty
fields. They are listed because they are the field's answers to the precision problem, and because
noting that Precept does not need them is part of the case that intervals are enough here
([overflow-in-solver-free-interval-domains.md, Findings 1–2](../../../research/architecture/compiler/overflow-in-solver-free-interval-domains.md)).
The full reasoning, including two further remedies from the same paper that fall to the same argument,
is in
[Why the interval is the whole technique](#why-the-interval-is-the-whole-technique-and-why-the-fields-precision-remedies-do-not-transfer).

### What Precept should adopt

**Adopt now — representation-aware intervals, as the whole base.** Every bound question in this document
is an interval question: whether an operation's result can leave the type, whether a divisor's possible
values include zero, whether a product of bounded factors stays away from zero. The one thing the
intervals must do that Precept's do not yet is model the *stored* result — the smallest magnitude the
lane can hold, what rounding does, what underflow does, what the maximum is. That is build items 4 and 5,
and it is the same construction Astrée and Frama-C EVA use. The argument is in
[The analyzers model stored arithmetic](#the-analyzers-model-stored-arithmetic). Astrée's own design paper
says the interval abstraction is already precise enough to *express* absence of run-time errors;
everything relational they added afterwards was about reaching a tight enough interval, not about saying
something intervals cannot say. For a definition with a few dozen fields and no loops, intervals reach
tight enough on their own.

**Adopt now — a reading of `abs(x) >= k` in a guard or a rule, which is the absolute-value octagon
property without the domain.** This is build item 1, and it is the one place this document asks for
something an interval cannot hold. `x != 0` and `abs(x) >= k` are both statements with a hole in the
middle; a single interval is one unbroken range and cannot have a hole. The absolute-value octagon domain
was built for precisely this and its published results clear divisor checks with facts of the form
`|d| > 0.1`. What Precept needs, though, is much less than the domain. The research is clear that AVO
does not *derive* magnitude bounds from arithmetic — every case in its results table starts from a test
the programmer wrote, and the domain's job is to carry that fact past the branch. Precept's case is the
same: the author writes `when abs(Rate) >= 0.05` or `rule abs(Rate) >= 0.05`, and the compiler has to
read it and use it. Reading a fact the author stated is a narrowing rule on one field, not a relational
domain over pairs. So take the property AVO exists to express, and implement it as guard and rule
narrowing over a single field. Do not build the domain.

**Adopt now — the readable contradiction test, which Precept already has.** The difference-logic
emptiness check is what the existing unsatisfiable-rule and contradictory-rule scans do. It is worth
naming here because the establishment-and-preservation work in build item 3 needs it: deciding whether a
declared rule can be satisfied at all, and whether two of them conflict, is this test.

**Adopt when the case demands it, not before — magnitude composition through a product, in whatever
domain can hold a hole around zero.** This is the AVO-or-donut capability proper, and it is the only
place where Precept would need a non-convex domain rather than a narrowing rule. It is deferred, and the
reasons are set out in
[Composing magnitude bounds through a product](#composing-magnitude-bounds-through-a-product): the
inequality is valid, but sign-definite factors are handled by plain interval multiplication with no new
machinery at all, so the composition only earns its keep for a divisor that may take either sign and is
known only to be some distance from zero. That shape is uncommon. If it turns out to be common in real
definitions, this moves up — it is a scheduling judgment, not a doubt about correctness.

**Not for this problem, but on the list for others — Karr affine equalities, TVPI, congruence closure,
incremental aggregates, dominator propagation.** Each closes a named gap the technique survey ranks, and
Karr in particular is that survey's highest-value recommendation because it is the only surveyed domain
that can hold a three-field equality. None of them bears on divisor non-zeroness or on overflow, which is
what this document is about. They are named so a reader does not conclude they were overlooked.

**Do not pursue at all — convex polyhedra, and predicate abstraction as it is normally built.** Polyhedra
would prove everything the others prove and more, and are disqualified by the second requirement: the
worst case is exponential, and the reference implementation's own answer is a timeout that falls back to a
weaker domain. A timeout makes both the verdict and how long it took depend on the data, which is exactly
what recompiling on every keystroke cannot have — and a proof outcome that varies with a time budget is
not determinism. Predicate abstraction is disqualified by the first requirement: its abstraction step
calls a theorem prover, and the version of it with the prover taken out is a restatement of the guard
narrowing Precept already does.

**Do not pursue — anything that discharges through an SMT solver.** The ground is the second requirement
above — a worst case that does not depend on the data — not a canonical ban. Canon has retired the ban:
§0.6 Proof philosophy item 3 is titled "Legible certificates, not opaque solvers" and says *"The search
procedure is unconstrained — a strategy may use any method to find a proof — but authority rests in the
certificate, not the search."* Under that, an SMT-backed strategy that emitted a legible replayable
certificate would be admissible in principle, and the four-criteria test is what it would have to pass.
What canon does still exclude by name is the *artifact*: §0.8 item 4 excludes "SMT-style or
theorem-prover-style proof artifacts". So the conclusion is unchanged and the reason is different — SMT is
declined here because its cost depends on the data, and a proof outcome that varies with a time budget is
not determinism. This is stated because the divisor research reaches SPARK and SMT-LIB, and a reader could
otherwise take their appearance in the evidence as a recommendation. They are cited for what their
*reasoning* requires — a fact about the complete divisor, reasoned over the rounded result — not for how
they reach it.

### Which technique underlies which build item

| Build item | Technique |
|---|---|
| 1 — read `abs(x) >= k` in a guard and in a rule | the absolute-value-octagon property, implemented as single-field narrowing rather than as the domain |
| 2 — let the divisor rule consult the interval already calculated | plain intervals; a membership test for zero, which is what every surveyed analyzer's divisor check is |
| 3 — establishment and preservation for declared constraints and rules | not a numeric domain. It is a coverage duty over the definition, using the existing contradiction test to decide whether a declared rule is satisfiable |
| 4 — representation-aware interval transfers | intervals computed with the concrete lane's own rounding, the smallest storable magnitude carried explicitly |
| 5a — a declared range per numeric lane, held in the interval | still intervals. The change is that the endpoint type has to distinguish the lane's maximum from the absence of a bound, which it does not today |
| 5b — the representability obligation at every operation | intervals again; the comparison against the lane's maximum, which the overflow research found is the entire technique. No relational domain, no widening — Precept has no loops, so the technique that answers imprecision in the surveyed tools has no work to do here |
| 5c — the missing transfers, `pow` above all | the same interval arithmetic extended to an operation that has none; without it 5b refuses correct compounding definitions |
| 6 — backward reasoning through `round()` (deferred) | would be backward propagation of a requirement through a rounding operation — the one structural technique here that is not recommended, because storing the rounded value and guarding it is clearer |
| 5d — excluding `NaN` on the `number` lane | not an interval technique at all. Every comparison against `NaN` is false, so containment gives no answer and the claim has to be carried separately. This is the one requirement on the list that no numeric domain discharges |
| 8 — stating the divisor requirement per lane | catalog metadata rather than a proof technique. What changes is what the obligation claims, not how it is discharged |
| the deferred signed-hole composition | a non-convex domain — absolute-value octagons or donut domains — and the only build item on this list that would need one |

## Overflow policy

**Expanded 2026-07-26 after Shane asked for the same rigour on overflow that the divisor question
received.** The earlier version of this document gave overflow one subsection and a handful of one-line
mentions, and left the reader to work out for themselves what an author is supposed to write. This
section supplies what the divisor sections already had: a definition that fails, the obligations named
one by one at the operations that carry them, the authoring forms and what each contributes, and an
honest account of the proof technique and of the remedies from the field that this document has been
declining without saying why.

### Overflow should be a hard operation-level obligation

The decimal representation has both a minimum non-zero magnitude and a maximum magnitude.
`System.Decimal` throws on overflow, but multiplication and division may round to fit its 96-bit
coefficient and scale range. Its smallest positive value is `1e-28`, and its maximum is about
`7.9e28`
([exact-decimal-arithmetic-survey.md, System.Decimal](../../../research/architecture/compiler/exact-decimal-arithmetic-survey.md)).

Precept already has `NumericOverflow`, but current interval containment is primarily about a computed
value fitting a field's declared bounds. That is not enough. An intermediate expression can exceed the
representation before assignment to any bounded field.

The compiler should generate a representability obligation at every operation whose result lane has a
finite range. For a chain such as:

```precept
field Exposure as decimal <- Principal * Multiplier * StressFactor
```

the compiler must check both multiplications. It is not sufficient to check only the interval finally
assigned to `Exposure`, because the first stored result is an arithmetic result in its own right.

This should be driven by operation and numeric-lane metadata:

- minimum and maximum finite value;
- smallest positive stored magnitude;
- overflow behavior;
- rounding behavior;
- whether NaN or infinity exists;
- the interval transfer appropriate to the operation and lane.

The proof engine should consume that metadata. It should not carry separate hard-coded cases for
`decimal`, `number`, `money`, and `quantity`.

**One grounding note, because it changes the ordering of the work rather than the target.** The owner
ruled on 2026-07-26, on the second asking and with the consequence stated in plain terms, that the
arithmetic overflow model is deferred past the MVP with no compile-time guarantee in the meantime, and
that the runtime guards come out with it (`docs/working-reset/phase-1/the-walk.md`, item 15;
`docs/language/primitive-types.md` § Open Questions). So overflow is an authorized exemption for the
first release, not a hole phase 5 has to close. Everything in this section describes where the model
should end up and what has to be built to get there; none of it is scheduled ahead of the divisor work,
and the items it names sit behind that ruling until the owner lifts it. Two consequences follow for how
to read the rest. The divisor work does not wait on any of it — a divisor obligation is discharged by
facts about the divisor, and none of those facts is an overflow fact. And the parts of this section that
matter soonest are the ones that shape the interval representation, because building intervals that
cannot express a lane limit now means rebuilding them later.

### Where the boundary actually is, lane by lane

The distances involved differ by more than twenty orders of magnitude between lanes, and that decides
which lane the obligation actually bites on.

| Lane | Backing | Largest magnitude | On exceeding it |
|---|---|---|---|
| `decimal` | .NET `System.Decimal`, 96-bit coefficient | about `7.9e28` | throws `System.OverflowException`, always, with no unchecked mode |
| `money`, `price`, `quantity`, `exchangerate` | `decimal` throughout, by the homogeneous-backing rule in `business-domain-types.md` D12 | the same `7.9e28` | the same throw |
| `integer` | fixed-width 64-bit | `9223372036854775807`, about `9.2e18` | the representation model is the deferred question — fixed-width with proof-or-reject, or arbitrary precision |
| `number` | IEEE 754 binary64 | about `1.8e308` | produces `±∞` rather than throwing, which is the infinity question that rides with the same deferral |

Three things follow, and they are different answers rather than one answer applied three times.

**On `decimal` and the business types, the boundary is far away and is reached by repetition rather than
by size.** Ordinary business magnitudes do not approach `7.9e28`; a national annual budget is about
`1e13`. What reaches it is a chain — repeated multiplication, and above all compounding, where the
exponent multiplies the number of digits rather than adding to them. The worked example below is
deliberately built on compounding for that reason.

**On `integer`, the boundary is close enough to reach with counts.** Three counts each bounded at three
million multiply past `9.2e18`. Cases per pallet times units per case times pallets shipped is an
ordinary thing for an author to write, and it is the lane where a representability obligation would
refuse real definitions rather than pathological ones. It is also the lane whose representation is still
undecided, and the overflow research is direct about what that choice does: arbitrary precision does not
delete the failure, it moves the threshold and changes what happens at it — Java's `BigInteger` documents
a maximum and throws at it, GMP has no maximum and terminates the process when allocation fails with no
recovery path, and CPython's integers are genuinely unbounded but a superlinear conversion over them
became a denial-of-service vulnerability that was fixed with a policy digit cap
([overflow-in-solver-free-interval-domains.md, Finding 5](../../../research/architecture/compiler/overflow-in-solver-free-interval-domains.md)).
What arbitrary precision would do to *this* document is sharper: it removes an obligation that two
comparisons decide, and replaces it with a bound on time and memory that none of the analyzers surveyed
attempts to prove at all. That trade is the owner's to make and this document takes no side in it.

**On `number` the failure is not a throw, and that changes the shape of the obligation rather than its
existence.** A binary64 operation that leaves the format produces infinity and carries on. Precept's
answer there is the deferred infinity question, and it cannot be settled by changing the representation
the way `integer` can — `number` is the approximate lane by construction and the exact lane already
exists as `decimal` (`primitive-types.md` § Open Questions). Nothing in this section proposes an answer.

### A definition that overflows, and where each obligation sits

Parallel to the freight rate card. Same business, one contract further along: a multi-year freight
agreement with an annual escalator, and an exposure figure the finance team reports.

```precept
precept FreightContractExposure

field BaseRate as decimal positive editable
field EscalationFactor as decimal positive editable
field ContractYears as integer positive editable
field AnnualVolume as decimal positive editable

field EscalatedRate as decimal <- BaseRate * pow(EscalationFactor, ContractYears)
field TotalExposure as decimal <- EscalatedRate * AnnualVolume * ContractYears

state Active initial
state Closed terminal
```

Every field is `positive`. Nothing else is declared. Under the target model this definition is refused,
and the interesting part is how many separate refusals it earns and where each one lives.

**Four operations, four obligations.** Reading the two computed expressions left to right:

1. `pow(EscalationFactor, ContractYears)`. The result lane is `decimal`. The base is somewhere in
   `(0 .. +∞)` and the exponent is somewhere in `[1 .. +∞)`, so the result interval is unbounded above.
   Unresolved, so refused. This is the operation that carries the whole risk in this definition, and it
   is worth seeing why: an escalator is a number slightly above one, and raising it to a term makes the
   digit count grow with the term rather than with the escalator. At fifteen percent a year the value
   crosses `7.9e28` somewhere past four hundred and seventy years. Nobody writes a four-hundred-year
   freight contract, which is exactly the point — what saves the definition is the declaration that says
   the term cannot exceed thirty years, and that declaration is a statement about freight contracts, not
   a concession to the compiler.
2. `BaseRate * <the result of 1>`. A separate operation with a separate obligation, over a left operand
   that is itself unbounded above. Refused independently. If the author fixes only the exponent, this
   one is still open, because `BaseRate` still has no ceiling.
3. `EscalatedRate * AnnualVolume`. The first multiplication inside `TotalExposure`, and the one this
   document has been using as its example of an intermediate that nothing assigns. It has no field of
   its own, no declared bounds, and today no obligation at all — the compiler stamps interval
   containment where a *declared bound* exists to contain something, and a nameless intermediate has
   none. That is the gap. The value is stored, in the ordinary sense that the runtime computes it and
   holds it before the next multiplication consumes it, and it can leave the lane there.
4. `<the result of 3> * ContractYears`. The last multiplication, whose result is what `TotalExposure`
   stores. This is the only one of the four that the current compiler would have anything to say about,
   and only if `TotalExposure` carried a declared `max`.

**The fix, and the arithmetic that discharges each obligation.** Every bound below is the domain's own —
a rate card ceiling, the escalation cap the contract itself carries, the term limit, the volume the
network can physically move.

```precept
field BaseRate as decimal min 0.01 max 10000 editable
field EscalationFactor as decimal min 1 max 1.15 editable
field ContractYears as integer min 1 max 30 editable
field AnnualVolume as decimal min 1 max 1000000 editable

field EscalatedRate as decimal <- BaseRate * pow(EscalationFactor, ContractYears)
field TotalExposure as decimal <- EscalatedRate * AnnualVolume * ContractYears
```

- `pow(EscalationFactor, ContractYears)` lies in `[1 .. 66.3]`, because `1.15^30` is about `66.2` and
  the base's own floor of `1` gives the lower end. Well inside the lane.
- `BaseRate * that` lies in `[0.01 .. 663000]`.
- `EscalatedRate * AnnualVolume` lies in `[0.01 .. 6.63e11]`.
- `that * ContractYears` lies in `[0.01 .. 1.99e13]`, which is about fifteen orders of magnitude below
  `7.9e28`.

All four discharge. Note what did the work: four `max` declarations on input fields, no guard anywhere,
and no new syntax. Note also what the lower bounds did — nothing for overflow. `min` matters for the
divisor question and for keeping a magnitude away from zero; the overflow obligation is decided by the
outer ends of the interval, and on a chain of positive factors that means the `max` declarations alone.
Where factors can go negative, both ends matter, because the largest magnitude of a product can come
from two large negatives.

**A grounding note, because it changes the size of the work rather than its direction.** Two of the four
obligations above are already discharged by machinery that exists. Confirmed at HEAD: a computed field
declared `max 100000000000000` over `BaseRate * AnnualVolume * TermFactor`, with the three factors
bounded at `10000`, `1000000` and `30`, compiles with the obligation Proved and the calculated interval
reported as `[0.01 .. 300000000000]`. Interval multiplication over declared bounds works, and it works
through a chained expression, not only a single operation. Two things are missing rather than wrong.
`pow` has no interval transfer, so the compounding version above reports an unbounded interval and
refuses a definition that is in fact safe. And no obligation is created anywhere a bound was not
declared — the intermediate in step 3 gets none, and `EscalatedRate`, carrying no `max` of its own, gets
none either. So the arithmetic is largely built and what is missing is the obligation that makes the
arithmetic answer a question about the lane rather than a question about a declaration.

### What the author writes to stay inside the lane

Four patterns, parallel to the four for divisors. They are not ranked, and none is a fallback from
another; they answer different situations.

**1. Bound every factor at the domain's own limit.** The ordinary answer, and the one the worked
example uses.

```precept
field BaseRate as decimal min 0.01 max 10000 editable
field AnnualVolume as decimal min 1 max 1000000 editable
field ContractYears as integer min 1 max 30 editable
```

Every operation in every chain over those fields then has an interval computed from declared numbers,
and the representability obligation is a comparison. This is fully expressible today — `min` and `max`
are ordinary field modifiers on every numeric lane, they are checked on every write by the same
machinery that checks every other bound, and nothing about writing them is new.

The bounds have to be the domain's, for exactly the reason given for the divisor case. A ceiling
invented to quiet the compiler is enforced on every write, and the first legitimate value above it is
refused at runtime. If the domain genuinely has no ceiling on a quantity, this pattern does not apply to
that quantity and one of the next three does.

**2. Materialize a stage of the chain into a stored field with declared bounds.** The overflow parallel
to divisor pattern 2, and it carries the same condition: it works when something establishes the bound
at the write, and not otherwise.

```precept
field EscalatedRate as decimal min 0.01 max 1000000 default 1

from Active on PublishRate when PublishRate.Rate >= 0.01 and PublishRate.Rate <= 1000000
    -> set EscalatedRate = PublishRate.Rate
    -> no transition
```

Every later chain that consumes `EscalatedRate` reads `[0.01 .. 1000000]` from the field's declaration
rather than recomputing a bound through however many operations produced the rate. The guard tests both
ends because the declaration claims both ends; a guard covering only the ceiling establishes only half of
what the field says about itself. That is the same benefit pattern 2 gives on the divisor side — one
declaration, consulted at every use.

**What it does not do is rescue a derived chain.** Writing `max 1000000` on a field declared
`<- BaseRate * pow(EscalationFactor, ContractYears)` does not cap the product; it asserts that the
product never exceeds a million, and that assertion has to be discharged from the factors, which is the
problem the author started with. On a computed field a declared bound is an invariant to be proved, not
a clamp to be applied — the same rule the divisor section states for `maxplaces` on a derived field. The
pattern converts the problem only when the value arrives through a `set` the compiler can reason about
at the point of the write: from an event argument carrying its own declared bounds, or under a guard
that establishes the ceiling, as above.

**3. Restructure so the large intermediate never exists.** Sometimes the final answer is comfortably
inside the lane and only a partial result leaves it. `(A * B) / C` where `A * B` overflows and the
quotient is small is the standard shape, and the rewrite is `A * (B / C)`.

Three warnings, and they are why this pattern is third rather than first.

- *It is a change of value, not a rearrangement of the same value.* `decimal` division rounds to fit the
  coefficient, so dividing first and multiplying second can produce a different stored answer than
  multiplying first. On a chain that feeds a `maxplaces`-constrained field, or a money amount that gets
  reported, that difference is visible to the business. Reorder deliberately and record why.
- *It can trade an overflow refusal for a divisor refusal.* Dividing early drives the intermediate
  towards zero, which is the other end of the same representation, and if that intermediate is later
  divided by, the interval that now includes small magnitudes is the one the divisor rule reads. The
  underflow policy below is what makes that a refusal rather than a silent wrong answer.
- *Choosing the unit is the same decision in another form.* A `quantity` quoted in grams rather than
  kilograms carries a number a thousand times larger through every operation it participates in. See
  [What money and quantity change](#what-money-and-quantity-change) — the choice of unit is a magnitude
  decision whether or not the author thinks of it as one.

**4. State a ceiling the operands genuinely have, as a `rule` over those operands.** The overflow
parallel to divisor pattern 3, and it earns its place for the same reason: a modifier can only say
things about one field at a time, and a rule can relate two.

```precept
field Pallets as integer positive editable
field UnitsPerPallet as integer positive editable

rule Pallets * UnitsPerPallet <= 5000000 because "the network cannot move more than five million units against one contract"
```

A `max` on each of `Pallets` and `UnitsPerPallet` separately would also bound the product, and where the
domain has both of those numbers that is the simpler thing to write. The rule earns its keep where the
domain's real limit is on the combination — the network's throughput, not the pallet count — and
declaring a per-field ceiling would either be a number nobody can defend or a bound tighter than the
business actually imposes.

The same duties apply as for a rule used in a divisor proof, and they are the same build item 3: something
must establish the rule at creation, and every write that could falsify it must be proved not to. And the
same limitation applies — a rule with a `when` guard holds only where its guard holds, so it contributes
no standing fact and discharges nothing.

### Why a rule over the result cannot carry an overflow claim

This is where the divisor pattern and the overflow pattern genuinely diverge, and it is worth stating
plainly because the shapes look alike.

For a divisor, `rule abs(EffectiveRate) >= 0.05` works because it constrains the *input* to the division.
The compiler reads the rule, learns a fact about the value the operation will consume, and discharges the
obligation before the operation happens.

For overflow, the equivalent-looking `rule TotalExposure <= 1000000000` constrains the *output*. It says
nothing about whether the multiplication that produces `TotalExposure` can be carried out. The ordering
is decisive and it is visible in the runtime: computed fields are recomputed at step 2g of an operation
and constraints are evaluated at step 2h (`evaluator.md`), so a multiplication that leaves the lane throws
during recomputation, before any rule over its result is ever consulted. A claim about a value that only
exists if the operation succeeded cannot be what proves the operation can succeed.

So the rule construct does transfer to overflow, but it has to sit on the operands. Pattern 4 above is
the form that works. A ceiling on the result field is not useless — it is an ordinary bound, checked like
any other, and it will catch a value that is too large for the business even though it fits the lane. It
is simply not evidence about representability, and it should not be written in the belief that it is.

### What money and quantity change

Neither changes the lane limit. All seven business-domain types use `decimal` as their magnitude
backing, by the homogeneous-backing rule in `business-domain-types.md` D12, so `money`, `price`,
`quantity` and `exchangerate` all overflow at the same `7.9e28` that bare `decimal` does. What the
qualifiers change is the number that travels through the analysis, and in one case they change it by
three orders of magnitude without the author writing a digit.

**A quantity's declared bound is normalized before it is compared, and the unit still matters — but not
for the reason an earlier revision of this section gave.** Interval containment converts both sides to
UCUM base units before comparing them, which is what makes `max '5 kg'` correctly accept a value written
as `'6 [lb_av]'` (`business-domain-types.md` § Qualifier compatibility on the bound). The base unit for
mass is the gram, so a bound authored as `5 kg` is `5000` inside the analysis.

That earlier revision concluded from this that a calculation quoted in milligrams "starts six orders of
magnitude closer to the ceiling" than one quoted in kilograms. **Normalization is exactly what defeats
that argument.** It happens on *both* sides of the comparison, so `5 kg` and `5000000 mg` both become
5000 grams and the two spellings are indistinguishable at the point of comparison. The only effect
normalization has on magnitude is a uniform factor of a thousand for every mass, because UCUM's mass base
is the gram, and that factor does not depend on what the author wrote.

The conclusion survives on a different argument, and it is the document's own central one. **The overflow
ceiling applies to the stored value, not to the compared bound.** A field declared `in 'mg'` stores
milligram numbers and carries milligram numbers through every operation it takes part in, and it is those
intermediates that a representability obligation is stamped on. So the unit is a magnitude decision after
all — through what gets stored and multiplied, not through how a bound is checked. This is the concrete
version of the unit warning in pattern 3.

**A price carries a unit in its divisor position, so multiplying a price by a quantity cancels the unit
and leaves the magnitudes multiplied.** `price * quantity → money` is a genuine multiplication of two
magnitudes; the dimensional cancellation happens in the qualifier, not in the number. Nothing about the
qualifier reduces the size of the result, and the representability obligation on that multiplication is
the ordinary one.

**A currency does not imply a bound.** ISO 4217 minor-unit data is available as metadata but is not
enforced, and `maxplaces` on a money field is explicit-only (`business-domain-types.md` § D10). So the
currency tells the analysis nothing about magnitude, and nothing about scale unless the author declared
it. There is no route by which naming a currency contributes to an overflow proof. Where a currency's
practical magnitudes are large — a unit price quoted in a currency whose smallest useful note is in the
thousands — that is a reason to declare `max` explicitly, not something the qualifier supplies.

**Where scale does contribute is at the other end.** `maxplaces` on a factor bounds how many digits the
product needs below the point, which is a scale statement rather than a magnitude statement. It does not
bound the product's magnitude and it does not help discharge a representability obligation. It is worth
saying because scale and magnitude are both "size" in ordinary speech and the two obligations are
genuinely different: `System.Decimal` is 128 bits — a 96-bit integer coefficient plus a separate 32-bit
flags word carrying the sign and a scale byte, so the scale costs none of the 96 bits. What the two
obligations share is the coefficient itself: it must hold *every* significant digit, above and below the
point together, within about 28–29 digits. So a result needing many digits below the point *and* a large
integer part can fail to fit even when neither alone would. The lane metadata in build item 4 is what
lets the analysis see that; a scale declaration on its own does not.

### What each overflow form contributes

Parallel to the divisor forms table, and deliberately its own table — the questions are not the same
questions.

| What the author declares | What it contributes to an overflow proof | What it needs |
|---|---|---|
| nothing — `positive` only | no bound at either end; every operation's interval is unbounded above | correctly refused |
| `max` on every factor in the chain | a computed bound at each operation, from declared numbers | build items 4 and 5; the interval arithmetic itself works at HEAD |
| `min` on every factor, no `max` | nothing for overflow. Lower bounds decide divisor and underflow questions | still refused, for the right reason |
| `max` on the result field only | bounds the last operation's result against a declaration; says nothing about the intermediates that produced it | this is what HEAD already checks, and it is the gap the section opens with |
| `max` on an intermediate **derived** field | an invariant that must itself be proved from the factors; splits one obligation into two, discharges neither | build item 3 for the establishment duty; no relief on its own |
| `max` on an intermediate **stored** field written by a guarded `set` or from a bounded event argument | a standing bound every later operation reads from the declaration instead of recomputing | build item 3, the same establishment-and-preservation duty as the divisor case |
| `rule` over the operands, such as `rule A * B <= K` | a bound on a combination that no per-field modifier can state | build items 2 and 3 |
| `rule` over the result | nothing. The operation runs at step 2g and rules evaluate at 2h, so the claim is about a value that only exists if the operation already succeeded | not a route to a proof |
| restructuring the chain — divide before multiplying, quote in a larger unit | a smaller intermediate, hence a bound that fits | nothing to build; it is an authoring choice with a rounding cost |
| `maxplaces` on a factor | scale, not magnitude. Relevant to whether a result fits the coefficient-and-scale budget together, not to whether its magnitude fits | build item 4's lane metadata is what makes the combined budget visible |

### Why the interval is the whole technique, and why the field's precision remedies do not transfer

The document has been asserting that intervals plus a comparison against the lane's maximum is the whole
technique, and mentioning remedies that "don't transfer" without saying why. Both halves deserve the
explanation. There are four remedies in the research, not the two the earlier text named, and the same
reason disposes of all four.

**Why the interval is enough.** In Miné's model the rounding function is defined to produce the run-time
error value exactly when the mathematical result leaves the format, and the interval operations lift that
to their endpoints — so the overflow check is not a step alongside the value computation, it is inside
the abstract arithmetic. There is no separate overflow analysis in any of the surveyed tools. Astrée's
own design history is more direct still: the plain interval abstraction "is precise enough to express
the absence of runtime errors", and every relational domain the team added afterwards was about reaching
a *tighter* interval, not about expressing a different property
([overflow-in-solver-free-interval-domains.md, Finding 1](../../../research/architecture/compiler/overflow-in-solver-free-interval-domains.md)).

**And the obligation is unusually forgiving.** This is the finding that most changes how the work should
be scheduled, and it had not been carried into this document at all. The same rate-limiter program is
proved safe by three different analyses producing three different bounds: `[−128, 128]` (the true
range), `[−136, 136]` (octagons), and `[−261, 261]` (Astrée with threshold widening). All three discharge
the obligation, because what it asks is not "what is the exact range" but "is there any bound below the
limit" (Finding 2, and Conclusion 2). A bound twice as wide as the truth is a complete proof. That is the
opposite of the divisor obligation, where the interval has to be precise enough to exclude a single
point and a bound one place too wide fails outright. Practically: precision work on overflow is worth
much less than precision work on divisors, and the plain interval domain is very likely to be enough
here for as long as Precept has no loops.

**Why widening with thresholds does not transfer, stated explicitly.** Widening exists for one purpose:
to force a fixpoint computation to terminate. An analyzer walking a loop computes a bound, goes round
again, computes a larger bound, and would keep enlarging forever; the widening operator cuts that off by
jumping — to infinity in the naive version, or to the next candidate from a precomputed staircase in
Astrée's version, which is what makes the jump land somewhere still useful. Every part of that is about
the loop. Precept's execution model has no loops: a handler is a finite list of statements, a computed
field is a finite expression tree over other fields, and the spec's finiteness guarantee (§0.4) is what
rules out the cycles that would create one. So there is no ascending chain, nothing that fails to
converge, and nothing to cut off. The interval for any expression is computed in a single pass from the
declared bounds of the fields it mentions. **Widening is not declined here on cost or on precision
grounds — it has no work to do.** That is worth stating as flatly as possible, because "we are not doing
what Astrée does" reads like a compromise and this one is not: the technique answers a question Precept
does not have.

The same reasoning disposes of two further remedies from the same paper that this document had not
mentioned. *Trace partitioning* — loop unrolling, case analysis on tests — is described by its own
authors as a convenience that simplifies the abstraction rather than something the proof needs. And the
*arithmetic-geometric progression domain* exists to bound a value that drifts under rounding error
accumulated over many iterations, relating the drift to a bounded clock; with no iteration there is no
accumulation to bound.

**Why variable packing does not transfer.** Packing is a cost control. Octagons cost cubic time in the
number of variables they range over, which is unaffordable across a program with thousands of them, so
Astrée runs octagons over automatically-chosen groups averaging three or four variables each and the
cubic cost is paid over tiny sets. Two things make it beside the point here. Precept is not running
octagons — the recommendation is intervals plus a single-field narrowing rule, and intervals are linear.
And a precept has tens of numeric fields, not thousands, bounded by the file rather than by anything
happening at run time; the technique survey's cost bands put octagons themselves inside the affordable
group at that size. Packing solves a scaling problem Precept does not have, using a domain Precept is not
adopting.

What is left after all four are set aside is the plain interval domain, and Astrée's own sentence that it
expresses the property. That is the case for build items 4 and 5 being the whole of the overflow work.

### What trapping buys, and what the comparators do that Precept will not

Two more findings from the overflow research that bear on the design and had not been pulled in.

**Trapping arithmetic is materially easier to analyse than wrapping arithmetic, and Precept's lanes trap.**
`System.Decimal` throws unconditionally, with no unchecked mode. The obligation is identical in either
case — compare the endpoints against the limits — but what differs is what the analysis must describe
*after* an operation it could not prove safe. A trap halts, so the following state only ever has to
describe the case where the operation succeeded, and in that case the value is the mathematically correct
one, which is what the interval already holds. A wrap always succeeds and produces a value that is not
the mathematical result, whose image under an interval abstraction is not an interval but two disjoint
pieces. Frama-C EVA documents the precision this costs in its own relational reasoning, which works
better on the type whose overflow is an error than on the type whose overflow is defined (Finding 3).
Precept inherits the easy case on `decimal` and on every business type. Nothing has to model modular
arithmetic, and nothing should be built as though it might.

**That overflow is an error at all is a specification choice, and there is prior art for making it one.**
Astrée rejects an overflow even on a platform where the wrap is well-defined, because the specification
it checks is a set of programming guidelines rather than the C standard — its own paper says so in the
clause "although, on the specific platform, the result can be well-defined through modular arithmetics".
EVA goes the other way for unsigned C arithmetic, for the equally deliberate reason that C99 defines it.
So a language declaring overflow an error independent of what the machine does is an established
position, not an unusual one (Finding 3).

**The comparators report and continue; Precept refuses, and that is not a dial setting.** EVA emits an
alarm and simultaneously narrows the value it carries forward — its worked example returns
`[2000000000..2147483647]` for a conversion whose input reached three billion, having assumed the
alarm away — and the manual states outright that "each alarm, in general, is also an assumption", with
the verification duty passed to the reader. That narrowing is what makes the analysis precise enough to
discharge later obligations. A refusing compiler never reaches the later obligation, because compilation
stopped, so it neither gains nor needs that precision (Finding 4). This is worth recording because it
removes a temptation: there is no partial-credit mode to reach for when an overflow obligation will not
discharge. The definition is refused and the author declares a bound.

## Underflow policy

**What is settled here and what is not.** `the-walk.md:377–392` (item 14) splits the underflow question
into two halves and this document can only answer one of them. The half that is closed: reasoning from
the parts — non-zero times non-zero is non-zero — is unsound and cannot be the answer, because two
non-zero operands can multiply to a stored zero. Both the walk and canon agree on that and the policy
below is built on it. The half that is open is the **cost**: if a division may only discharge from
something declared on the divisor itself, then an author who divides by a computed product must restate
the constraint on the product, and the walk records that this cost "was never worked out or put to him."
That item is a "Waiting on Shane" row, "to be prepared with worked examples before it is asked."

**This document is that preparation.** The eight divisor forms, the four authoring patterns, the worked
examples and the comparison tables exist to make the cost visible so the question can be put with real
definitions attached rather than in the abstract. Read them as options with their prices marked. Nothing
below decides the open half, and an earlier revision of this document overstated that by saying the
underflow policy "settles" the open question canon recorded. It routes it; the routing is sound; the cost
is the owner's call.

### Underflow should change the interval, not become a general rejection

I do not recommend a general rule that every possible underflow rejects the definition.

A value becoming zero is not always invalid. Zero may be a permitted result of an intermediate,
cancellation, a ratio, or explicit rounding. Rejecting every chain that might reach a tiny magnitude
would force authors to add precision constraints where the domain has none and would reverse the prior
`maxplaces` guidance for the wrong reason.

Instead:

- the operation transfer must include zero whenever the representation may store zero;
- any inherited `nonzero` fact must be dropped;
- assignment to a governed `nonzero` field must be proved safe;
- division or modulo by the result must be proved against the final stored interval;
- diagnostics should explain that finite precision can make the computed value zero.

This matches the solver-free analyzers' treatment while preserving Precept's stricter compile-time
outcome.

There is a larger unresolved question about whether a type advertised as exact should silently round an
inexact division or multiplication at all. The decimal survey shows that `System.Decimal` does. Changing
that contract would affect the numeric model and author-visible behavior, well beyond the divisor rule.
It should be handled in the numeric-lane design work rather than smuggled into phase 5.

## Divisor policy

### Prove the final stored divisor

The rule should be:

> A divisor obligation is discharged only by a fact that excludes zero from the possible stored values
> of the divisor expression at that program point.

This permits ordinary interval reasoning. If a value is known positive or negative, the interval already
excludes zero. It also permits a direct disequality guard and the stronger magnitude guard described
below.

It forbids these unsound shortcuts:

- every factor is `nonzero`, therefore their product is non-zero;
- a field whose value comes from an expression carries `nonzero`, therefore that expression established
  non-zeroness;
- every factor has `maxplaces`, therefore their product remains representable;
- a value was non-zero before `round()`, therefore the rounded value is non-zero.

The last point matters in ordinary authoring. Rounding is exactly capable of turning a small non-zero
value into zero.

Note what the first of those shortcuts does and does not say. Bare `nonzero` on each factor is *not
exactly zero* and composes to nothing, because two values each merely unequal to zero can multiply to a
stored zero. A declared *interval* on each factor is a different fact, and it does compose: interval
multiplication of `[0.01 .. 10000]` by `[0.5 .. 4]` gives `[0.005 .. 40000]`, which excludes zero. The
rejected shortcut is the bare disequality, not every fact an author can declare about a factor. See
[Composing magnitude bounds through a product](#composing-magnitude-bounds-through-a-product).

### Prefer a domain threshold when the domain has one

`Divisor != 0` is sound when it tests the final stored value. It does not, however, express that a rate
below a practical magnitude is unusable.

When the domain has such a minimum, the clearer guard is:

```precept
when abs(ComputedRate) >= 0.0001
```

This works for either sign and states the permitted distance from zero. It is stronger than a bare
disequality and gives the compiler a concrete interval gap.

The threshold must come from the domain contract. It must not be invented merely to satisfy the
compiler. If the rate is definitionally positive, the simpler positive guard remains appropriate:

```precept
when ComputedRate > 0
```

Where the same divisor is used in more than one place, the threshold belongs on the field — as modifiers,
as a declared interval, or as a `rule` beneath the field — rather than repeated at each division. See
[Authoring patterns](#authoring-patterns-and-when-each-one-fits).

### `round()` is not a substitute for a magnitude guard

`round(ComputedRate, 4) != 0` is sound evidence that the rounded result is non-zero. It is not
equivalent to:

```precept
abs(ComputedRate) >= 0.0001
```

With round-half-to-even behavior, `round(x, 4) != 0` implies `abs(x) > 0.00005`, not
`abs(x) >= 0.0001`. The inequality is strict because the exact midpoint `0.00005` rounds to zero:
zero is the even result. The rounded test therefore permits original values whose magnitude is below
the domain threshold in the earlier example.

More importantly, authors should not use `round()` merely as a non-zero-testing trick. When a rounded
value crosses into a `maxplaces`-constrained field and later becomes a divisor, store the rounded value
and guard that stored value:

```precept
from Active on SetRate
    -> set BillingRate = round(RawRate, 4)
    -> no transition

from Active on Calculate when BillingRate != 0
    -> set Cost = Total / BillingRate
    -> no transition
```

The proof is about `BillingRate`. It establishes nothing about whether `RawRate` was non-zero or how
far it was from zero. This is the same rule as for every divisor: test the final stored value after all
representation effects.

### The `duration` lane is carved out, and this recommendation does not reach it

Everything in this section reasons about the numeric lanes. `duration` divisors are governed by canon
already, on terms this document does not propose to change, and saying so explicitly matters because a
strict reading of "prove the final stored divisor" would silently overturn one of them.

`temporal-type-system.md:698–707` sets out the model. `duration / integer|number` and `duration / duration`
both carry divisor safety, backed by `DivideByZeroException` in NodaTime rather than by an IEEE infinity —
so the runtime consequence there is more severe than on `number`, not less. Canon names the proof sources
directly, and the list is broader than this document's: a `positive` or `nonzero` constraint, an explicit
`rule Field > 0` or `rule Field != 0`, a `when Field != 0` guard, `in State ensure Field > 0`, or
`on Event ensure Arg > 0`. Two diagnostics carry it — **C92** for a literal zero divisor, **C93** for an
unproven one.

Three things follow, and none of them is a change this document proposes.

- **Three of this document's eight forms do not exist on that lane.** `min` and `max` are compile errors
  on `duration` — the constraint values are bare numbers rather than duration quantities — and there is no
  `maxplaces`. So forms (c), (e) and (f) have nothing to attach to. What is left is the guard, the rule,
  the `ensure`, and `nonzero` on the field, which canon already lists.
- **Canon's compound-divisor concession stands, and this recommendation does not override it.**
  `temporal-type-system.md:707` reads: *"Compound expressions: `duration / (Rate + 1)` assumes satisfiable
  — no diagnostic (consistent with #106's Principle #8 conservatism)."* That is the opposite of
  prove-or-reject, and a strict reading of this document's central rule would replace it with a refusal.
  **It should not be read that way.** Changing a canonical concession is a decision, not a consequence, and
  this document neither argues for it nor was written with that lane in view. If the numeric model
  eventually makes the temporal concession look inconsistent, that is a reconciliation to run deliberately
  against `temporal-type-system.md` with its own reasoning — not something to inherit by implication from
  a document about `decimal` and `number`.
- **`nonzero` on `duration` needs a type-aware desugar, and canon already says so.** `nonnegative` and
  `nonzero` desugar to `Field >= 0` and `Field != 0` for numeric types, which would be type errors against
  a duration; canon's implementation note requires comparisons against `Duration.Zero` instead. That is a
  detail of the same establishment-and-preservation duty in build item 3, and it is recorded here so that
  an implementer reading build item 3 does not assume the numeric desugar covers every lane.

### Do not require `maxplaces` on intermediates

The earlier recommendation remains correct:

- use `maxplaces` where an input or stored result has a domain scale limit;
- omit it on intentional higher-precision intermediates;
- use `round(value, places)` explicitly when crossing into a scale-constrained value.

The new research does not justify reversing that rule. It shows that unconstrained intermediates make
a composed lower-magnitude proof harder, but the appropriate response is not to attach false domain
contracts to them.

`nonzero maxplaces 4` can imply that a governed stored value has magnitude at least `0.0001`. That is
useful when both constraints are genuine, and it is the basis of the materialize-once pattern in
[Authoring patterns](#authoring-patterns-and-when-each-one-fits). It remains only one fact
about that stored value. If the value then participates in more arithmetic, the compiler must analyze
the stored result of that arithmetic.

## What the author writes, and what each form establishes — a worked example

Shane asked on 2026-07-26 whether writing `when abs(ComputedRate) >= 0.0001` at the division site is
another way of saying `field ComputedRate as decimal nonzero maxplaces 4`. It is not, and working
through why is the clearest way to describe the target model, because it forces each authoring form to
say exactly which fact it contributes and which obligation that fact discharges.

**Revised 2026-07-26 after Shane's second reading.** The first version of this section drew the line in
the wrong place. It answered the narrow question — are the guard and the modifier pair the same thing? —
and then let that answer spread over a pattern it does not apply to. Declaring `nonzero maxplaces N` on a
field whose value arrives directly from an expression the compiler has to analyse is one thing.
Declaring it on a field that an explicit `set` writes, and then dividing by that field, is a different
thing, and it is a good pattern. The reasons below are correct about the first and were wrongly extended
to the second; the text now says which is which, and
[Authoring patterns](#authoring-patterns-and-when-each-one-fits) states the resulting
recommendation.

**Revised again 2026-07-26 after Shane's third reading**, which asked whether constraining the factors
would make the derived form provable. It does, and the answer runs through form (c) case 2, pattern 4,
and [Composing magnitude bounds through a product](#composing-magnitude-bounds-through-a-product). The
second revision was right that *this* product cannot establish the claim and wrong to imply that no
product could. The dividing line is not derived-versus-stored. It is whether anything establishes the
claim at the write — a guard, a `set` under checkable conditions, or the factors' own declared bounds.

### The magnitude floor the pair really does imply

The intuition that `maxplaces` says nothing at all about how close a value can get to zero is right about
`maxplaces` on its own and wrong about `maxplaces` combined with `nonzero`. On the exact `decimal` lane,
a value permitted at most four decimal places and forbidden from being zero cannot be `0.00001`; the
smallest magnitude left to it is exactly `0.0001`. `0.0000` is not a separate small value with four
places — it is zero, and `nonzero` has already excluded it. So the pair really does imply a magnitude
floor, exactly as the earlier note recorded
([maxplaces-and-divisor-non-zeroness](maxplaces-and-divisor-non-zeroness-2026-07-26.md)).

That makes the comparison sharper rather than duller. If the pair implied nothing about magnitude, the
answer would be a one-line no. Instead, for a genuinely governed stored decimal, `nonzero maxplaces 4`
and `abs(x) >= 0.0001` exclude the same set of values. The two are still not interchangeable, for four
reasons that have nothing to do with which values they exclude. The first two apply only where the
field's value arrives from an expression that has to establish the constraint by itself; the last two
apply wherever the modifier pair is used at all.

1. **They are checked at different places about different things.** The guard is a fact about the value
   the division will actually use, established immediately before the division. The modifiers are a
   standing claim about a field's stored value throughout its life. Where the divisor's value comes
   straight from an expression, the modifier claim is something that arithmetic has to establish on its
   own, and a product of two values that may each be arbitrarily small cannot establish it. This
   objection disappears the moment an explicit `set` writes the field under conditions the compiler can
   check — see form (f) — and equally when the factors carry declared bounds that hold them away from
   zero, so that the product's own interval establishes the claim — see form (c) case 2.
2. **A modifier on a derived field is an obligation, not a premise.** Under the target model, writing
   `nonzero` on a field defined as `<- <expression>` asks that expression to prove it. The freight
   example below shows a computation that cannot, so the definition stays refused and the modifier has
   bought nothing. (Today the modifier is accepted unproved — BUG-048, and build item 3 closes it.) Note
   that the obligation itself is the good part; what fails is the particular expression, not the idea of
   proving a field's declared constraint.
3. **The `maxplaces` route can only express thresholds that are powers of ten.** A domain whose minimum
   usable rate is `0.05` or `0.25` cannot state it this way. `abs(x) >= k` takes any `k`.
4. **The floor is implied by a scale declaration rather than stated as a domain minimum.** If the domain
   later permits six decimal places, the implied floor silently drops to `0.000001` and the
   division-site proof changes without anyone editing the division. `maxplaces` is also a `decimal`-lane
   constraint, so this route does not exist at all for a `number` divisor. This is a real cost of the
   form, and an author choosing it should write down the domain minimum in a comment or a `describe`
   text so that a later widening of the scale is recognized as the change to the contract that it is.

### The definition the model must refuse

A freight rate card. The effective rate is the tariff base scaled by a fuel multiplier, and quotes divide
a shipment cost by that rate.

```precept
precept FreightRateCard

field TariffBase as decimal positive editable
field FuelMultiplier as decimal positive editable
field EffectiveRate as decimal <- TariffBase * FuelMultiplier
field UnitCost as decimal default 0

state Active initial
state Closed terminal

event Create(TariffBase as decimal positive, FuelMultiplier as decimal positive) initial
event Quote(ShipmentCost as decimal positive)
event Close

on Create
    -> set TariffBase = Create.TariffBase
    -> set FuelMultiplier = Create.FuelMultiplier

from Active on Quote
    -> set UnitCost = Quote.ShipmentCost / EffectiveRate
    -> no transition

from Active on Close
    -> transition Closed
```

> `PRE0083` — Division is unsafe: 'EffectiveRate' can be zero on event 'Quote' from state 'Active'

This refusal is correct and must stay correct: two positive decimals small enough multiply to a stored
zero. It depends on how `TariffBase` and `FuelMultiplier` are declared, not on the shape of the derived
field — form (c) below shows the same derived field being accepted once those two declarations carry
bounds. Everything below is about what the author writes next, and what the compiler should be able to
make of it.

### The eight forms, and what each one contributes

**(a) Guard the division site on a magnitude.** The right form for a one-off division.

```precept
from Active on Quote when abs(EffectiveRate) >= 0.0001
    -> set UnitCost = Quote.ShipmentCost / EffectiveRate
    -> no transition
```

The fact is: at the moment of division, this value is at least `0.0001` away from zero, for either sign.
It is established where the value is used, after every representation effect has applied, and it holds
whatever the arithmetic upstream did. The syntax parses and type-checks against the existing language —
`abs(decimal) -> decimal` is declared — and what remains is teaching guard narrowing to read it. That is
build item 1.

**(b) Declare `nonzero` on a derived field whose expression cannot establish it.** Not a route to a
proof.

```precept
field EffectiveRate as decimal nonzero <- TariffBase * FuelMultiplier
```

Under the target model this generates an establishment obligation against `TariffBase * FuelMultiplier`,
and with those two fields declared as they are in the example above — `positive`, with no lower bound —
that product cannot discharge it, because small enough operands make it zero. So the definition stays
refused, correctly. Even if some future computation could establish it, the fact would only be *not
exactly zero*, which says nothing about whether the division result is representable. (Today this
modifier is accepted unproved and flips the file from correctly refused to wrongly accepted; that is
BUG-048, build item 3.)

What fails here is this particular expression over these particular factor declarations — not the
practice of declaring a constraint on a field, and not the derived form as such. Form (c) below shows
the same derived field becoming provable once the factors carry bounds. Form (f) declares the same
constraint on a field written by an explicit `set` under a checkable condition, and it works too.

**(c) Declare `nonzero maxplaces 4` together on the same derived field.** Whether this works depends
entirely on what the factors declare. **Revised 2026-07-26 after Shane's third reading** — the earlier
version of this entry said "same answer as (b)" and stopped there, which was wrong.

*Case 1 — the factors are unbounded below.* With `TariffBase` and `FuelMultiplier` declared as in the
refused example above:

```precept
field EffectiveRate as decimal nonzero maxplaces 4 <- TariffBase * FuelMultiplier
```

the establishment obligation from `nonzero` still has nothing to discharge it, and `maxplaces` does not
help discharge it. `maxplaces` alone contributes nothing to a divisor proof under any policy, because a
value constrained to four places may still be zero. On a derived field `maxplaces` is an invariant rather
than a rounding instruction, so a product needing five places violates the field instead of being
trimmed to fit it. Correctly refused.

*Case 2 — the factors carry declared bounds.* The identical derived field becomes provable when the
author constrains the inputs:

```precept
field TariffBase as decimal min 0.01 max 10000 maxplaces 2 editable
field FuelMultiplier as decimal min 0.5 max 4 maxplaces 2 editable
field EffectiveRate as decimal nonzero maxplaces 4 <- TariffBase * FuelMultiplier
```

Both halves of the claim are now discharged, by different reasoning, and both are worth stating
separately because an author who supplies only one of them gets a definition that is still refused.

- **`nonzero` is discharged by the interval.** `[0.01 .. 10000] * [0.5 .. 4]` is `[0.005 .. 40000]`. That
  interval lies wholly above zero, so zero is not among the possible stored values. This is ordinary
  interval multiplication — build items 2 and 4 — and needs no magnitude-composition machinery, because
  both factors are held to one side of zero. The floor `0.005` is far above `decimal`'s smallest
  magnitude of `1e-28`, so underflow cannot pull the product to zero.
- **`maxplaces 4` is discharged by scale addition.** An exact product of a two-place value and a
  two-place value needs at most four places, and on the exact `decimal` lane it is stored exactly
  whenever the coefficient fits. Rounding, when the coefficient does not fit, only discards low-order
  digits, so the stored scale is at most four either way.

Both bullets are needed. Bounds alone leave the scale unconstrained — `min 0.01` permits `0.015`, and
`0.015 * 0.5` is `0.0075`, but nothing stops a three-place factor from producing a five-place product
that violates `maxplaces 4`. Scale alone leaves the magnitude unconstrained in the other direction.

**The upper bounds are not decoration.** Under this recommendation every multiplication carries a
representability obligation (build item 5), and two unbounded `decimal` factors can multiply past
`7.9e28`. `max 10000` and `max 4` give a product bound of `40000`, which discharges it. A rewrite that
supplies only lower bounds fixes the divisor problem and leaves the overflow problem, and the definition
is still refused — for a different and equally correct reason.

The `maxplaces` route to a floor works here as well as an explicit `min`: `nonzero maxplaces 2` on each
factor implies a magnitude of at least `0.01` each, hence at least `0.0001` for the product, which is
exactly the floor `nonzero maxplaces 4` implies for `EffectiveRate`. The arithmetic lands exactly on the
boundary, and `10^-2 * 10^-2 = 10^-4` is exactly representable, so nothing is lost to rounding. It is
still the weaker form of the two, for the reason in the fourth objection above — the floor is implied by
a scale declaration that a later widening can silently move — and it does not supply the upper bounds
the overflow obligation needs. Prefer `min` and `max`.

This case needs no new proof rule beyond what is already scheduled. What it needs is an author who
states the domain's bounds on the inputs.

**(d) Store a rounded rate and guard the stored value.** Sound, available with today's language.

```precept
field BillingRate as decimal maxplaces 4 default 0

from Active on PublishRate
    -> set BillingRate = round(TariffBase * FuelMultiplier, 4)
    -> no transition

from Active on Quote when BillingRate != 0
    -> set UnitCost = Quote.ShipmentCost / BillingRate
    -> no transition
```

The fact is: this stored value is not zero at this division. It is the honest form when the rate has a
four-place domain contract, and it is the pattern described in
[Rounding before division](#rounding-before-division). It gives no floor; for a floor, the guard body
becomes the `abs()` form from (a) and waits on the same build work.

**(e) Let the divisor's declared interval exclude zero.** Sound, and half of it already exists.

```precept
field BillingRate as decimal min 0.01 max 100 default 1
```

The fact is: the stored value lies in `[0.01 .. 100]`, which excludes zero. The compiler already
calculates this interval for the containment obligation; the divisor rule needs to consult it. That is
build item 2, and it is smaller work than the `abs()` narrowing. `PRE0083`'s recovery text already
advises adding `min 1`, so the diagnostic and the proof rule should be brought into agreement in the
same pass.

**(f) Materialize the rate into a `nonzero maxplaces 4` field with an explicit `set`, then divide by
that field.** Sound, and the right form when the same divisor is used in more than one place. This is
the pattern Shane described, and the earlier version of this note wrongly folded it into (c).

```precept
field TariffBase as decimal positive editable
field FuelMultiplier as decimal positive editable
field EffectiveRate as decimal nonzero maxplaces 4 default 1
field UnitCost as decimal default 0
field AnnualCost as decimal default 0

from Active on PublishRate when abs(round(PublishRate.Raw, 4)) >= 0.0001
    -> set EffectiveRate = round(PublishRate.Raw, 4)
    -> no transition

from Active on Quote
    -> set UnitCost = Quote.ShipmentCost / EffectiveRate
    -> no transition

from Active on Project
    -> set AnnualCost = Project.AnnualSpend / EffectiveRate
    -> no transition
```

What makes this different from (c) is not the modifiers, which are identical. It is that the value
arrives through a `set` statement the compiler can reason about at the point of the write. At that point
the guard on the event has already established that the value being stored has magnitude at least
`0.0001` and fits four decimal places, so both halves of `nonzero maxplaces 4` are discharged there. The
freight product in (b) and (c) fails because nothing establishes it; this `set` succeeds because
something does. The source of that establishment can be any fact the compiler already has — a guard on
the handler, an event parameter's own declared constraints, or an interval already proved for the source
expression.

From then on the constraint is a standing property of `EffectiveRate`, and every division by it is
discharged from the field's own declaration. The two division handlers carry no guard, and a third and
fourth would carry none either. That is the whole benefit: the threshold is written once, in the place
that says what the rate is allowed to be, instead of being repeated at each division and kept in step by
hand.

The proof duty this creates has two halves, and both are the same work as build item 3:

- **Establishment.** Every `set` into `EffectiveRate` must be proved to satisfy `nonzero maxplaces 4`
  from the facts holding at that statement. A `set` the compiler cannot discharge is refused, exactly as
  the derived form in (b) is refused.
- **Preservation.** Every other write to the field is subject to the same duty, which is what makes the
  standing claim trustworthy at a division site that is nowhere near the write. A later handler adding
  `set EffectiveRate = 0` is refused rather than quietly invalidating every division in the file.

Two limits carry over from the four reasons above and are not removed by materializing. The implied
floor is only ever a power of ten, so a domain minimum of `0.05` cannot be written this way. And the
floor comes out of the scale declaration rather than being stated, so widening the field to
`maxplaces 6` later drops the floor to `0.000001` without touching any division. Both are acceptable
when the domain minimum genuinely is a power of ten and the author knows they are choosing that
tradeoff; neither is a reason to prefer repeating a guard.

**(g) State the floor as a declared `rule` over the divisor field.** Sound, and the only form that can
write an arbitrary threshold. **Added 2026-07-26 after Shane asked whether a rule could say what
`maxplaces` cannot.**

```precept
field TariffBase as decimal positive editable
field FuelMultiplier as decimal positive editable
field EffectiveRate as decimal <- TariffBase * FuelMultiplier
field UnitCost as decimal default 0
field AnnualCost as decimal default 0

rule abs(EffectiveRate) >= 0.05 because "the rating engine cannot price a shipment below a nickel per unit"

from Active on Quote
    -> set UnitCost = Quote.ShipmentCost / EffectiveRate
    -> no transition

from Active on Project
    -> set AnnualCost = Project.AnnualSpend / EffectiveRate
    -> no transition
```

The fact is: in every configuration this entity can be in, this field is at least `0.05` away from zero,
for either sign. The spec calls a rule "global data truth — constraints that hold after every mutation"
(§3A.1), and a rule with no `when` guard holds in every state, so the fact is available at every division
site with no guard written at any of them. That is the same one-declaration coverage form (f) gives,
without the threshold having to be a power of ten.

Three things distinguish this from the forms above.

- **The threshold is written, not implied.** `0.05` appears literally, in the place that says what the
  rate is allowed to be, and it carries a mandatory `because` that says why. Form (f)'s floor is a
  side-effect of a scale declaration, undocumented unless the author adds a `describe`, and it moves if
  anyone widens the scale later. Nothing about a rule moves except by editing the number.
- **It covers a signed divisor.** A declared interval — form (e) — is one contiguous range and cannot
  exclude a hole around zero. `abs(x) >= k` in a rule can. That is the case form (e) explicitly does not
  reach.
- **It applies to a derived field.** Unlike a modifier on a `<-` field, which today is enforced by
  nothing (BUG-048), a rule is evaluated by the runtime after computed fields are recomputed and before
  the operation commits (`evaluator.md`, step 2g then 2h), so it genuinely governs the recomputed value.

What it does **not** do is escape the proof duty. The same two halves as form (f) apply, and they are the
same build item 3:

- **Establishment.** Something must make the rule true at creation. The compiler already does half of
  this: an unguarded rule is folded against the field defaults and the definition is refused if the
  defaults violate it (`PRE0164`, confirmed at HEAD).
- **Preservation.** Every write that could falsify the rule must be proved not to. This is not built.
  Confirmed at HEAD: a file carrying `rule EffectiveRate >= 0.05` alongside a handler that does
  `set EffectiveRate = 0.0` compiles with no diagnostics at all, while the rule is simultaneously
  consumed to discharge the divisor obligation elsewhere in the same file. This is Shane's item-16 ruling
  applied to the divisor case: the compiler refuses the definition until the author writes something that
  makes the rule provable, rather than accepting it and leaving the runtime to sort it out.

  **What the runtime does in the meantime depends on the shape, and an earlier revision of this document
  got it wrong.** It said the runtime refuses the handler when it fires "so nothing bad is stored," which
  is only true in one of the two shapes:

  - *Divisor written by a row action, divided elsewhere.* Row actions run at step **2e**, before computed
    fields recompute at 2g and before rules evaluate at 2h. The rule does refuse the operation, nothing
    commits, and the reassurance holds.
  - *Divisor is itself a computed field, and the division is in another computed field.* Both recompute
    at step **2g**. The division runs there, before any rule is consulted at 2h. Reproduced at HEAD:
    `field UnitCost as decimal <- ShipmentCost / EffectiveRate` with `rule EffectiveRate >= 0.05`
    compiles with zero errors and the obligation Proved via `CompositionalConstraint`, and at run time the
    division faults before the rule can refuse anything. What the author gets is a divide-by-zero fault
    rather than a clean constraint failure naming the rule they wrote.

  This is the same 2g-then-2h ordering used three bullets above to establish that a rule *does* govern a
  recomputed value, and both readings are correct — a rule checks a value produced at 2g, but it cannot
  prevent the arithmetic that produced it. The consequence is that the missing preservation duty is a live
  breach in the second shape rather than a hygiene gap, which is a further reason to build item 3 rather
  than a reason to distrust form (g).

So `rule abs(EffectiveRate) >= 0.05` over the freight product above is refused for the same reason form
(b) is refused — a product of two possibly-tiny operands establishes nothing — and becomes provable by
the same fix: bound the factors, or write the rate through a guarded `set`. Expressing the claim as a
rule changes where the claim is written and how precisely it can be stated. It does not change what has
to be proved.

**What this form needs before it works.** Two pieces, both already on the build list for other reasons.
Confirmed at HEAD: `rule EffectiveRate >= 0.05` — a bare field-against-constant comparison — already
discharges the divisor obligation through the existing `CompositionalConstraint` strategy, so the
rule-as-premise channel exists and is wired. But `rule abs(EffectiveRate) >= 0.05` discharges nothing,
and neither does the sign-explicit `rule EffectiveRate >= 0.05 or EffectiveRate <= 0.0 - 0.05`. The
missing piece is the same `abs()` narrowing as build item 1 — it has to be reachable from the
rule-sourced fact path and from the default fold, not only from a transition-row guard. The disjunctive
spelling additionally needs the fact extractor to read a two-branch magnitude condition, which is a small
extension of the same work rather than a separate rule.

**(h) State the floor as an `ensure`, scoped to the states or events where it holds.** Sound, named by
canon as a proof source, and the one form that covers a floor holding on some paths and not others.

```precept
in Priced ensure abs(EffectiveRate) >= 0.05 because "a rate below five basis points is not a quotable rate"
```

An earlier revision of this document said there was no eighth route worth listing. That was wrong, and it
was wrong against canon rather than against an opinion. `precept-language-spec.md:994–995` gives the
grammar for all four `ensure` scopes; spec line 206 names "a rule, an ensure, or a guard" as the proof
paths; line 247 names "an explicit rule or ensure" as the author's hand-written substitute; and
`temporal-type-system.md:706` lists `in State ensure Field > 0` and `on Event ensure Arg > 0` by name
among the facts that discharge a divisor obligation. The document searched for rules, modifiers and
guards, found seven forms, and never looked for the fourth construct. The set is eight.

**What it adds that nothing else reaches.** A `rule` is a claim about every configuration the entity can
reach. Where a floor genuinely holds only in part of the lifecycle — a rate that is unconstrained while a
quote is being assembled and bounded once it is priced — a rule cannot state it, because the rule would be
false in `Draft`. This document's own answer at
[when the domain has no defensible minimum](#when-the-domain-has-no-defensible-minimum) is to fall back to
repeating a guard at every division site. `in Priced ensure abs(EffectiveRate) >= 0.05` states it once,
scoped to exactly where the domain says it holds, with the reason attached — and every division reachable
from that state reads it. A guarded rule (`rule ... when`) looks like it should do this and does not: it
contributes no standing fact, for the reason given under form (g).

**What it needs, and the duties it carries.** The same establishment-and-preservation duty as forms (f)
and (g), which is build item 3 — with the scope narrowing what has to be proved rather than removing it.
A state-scoped `ensure` must be established by every transition that *enters* the state and preserved by
every write that happens *while in* it; writes in other states are exempt, which is a smaller obligation
than a rule's and a differently-shaped one. Build item 3 should be read as covering **three** declaration
forms, not two — modifier, rule, and ensure. The owner's item-7 ruling names five constraint forms, so
three of five carry this duty.

Two boundaries worth stating. An event-scoped `ensure` (`on Event ensure ...`) constrains one operation
and so behaves like a guard rather than like a standing fact — useful at a division inside that event's
own action chain, and contributing nothing elsewhere. And the `to`/`from` forms constrain a transition,
not a resting configuration, so they establish a fact at a moment rather than across a region; only the
`in State` form gives the standing, region-scoped fact this route is worth listing for.

There is no ninth route worth listing, and that claim is now made having looked. Guarding the operands for
bare non-zeroness rather than the divisor is the reasoning the whole recommendation rejects. Note that
form (c) case 2 is not an exception to this: it does not guard the operands, it declares intervals on
them, and the interval of the divisor itself is what discharges the obligation. The rule stays "prove the
final stored divisor" throughout — form (g) is not an exception, because a rule over the divisor field is
a claim about the stored divisor and nothing else, and neither is form (h), which is the same claim with a
scope on it.

### The comparison, stated exactly

| Form | What the fact establishes | Sound | What it needs |
|---|---|---|---|
| unchanged | nothing about the stored product | — | correctly refused |
| (a) `when abs(Rate) >= 0.0001` | at this division, magnitude at least `0.0001`, either sign | yes | build item 1 |
| (b) `nonzero` on a derived field, factors unbounded | intended: not exactly zero. This product cannot establish it | no route to a proof | build item 3 makes it an obligation |
| (c) case 1 — `nonzero maxplaces 4` on the same derived field, factors unbounded | intended: not exactly zero, plus a magnitude floor from the two together. Same establishment problem | no route to a proof | same |
| (c) case 2 — the same declaration, with `min`/`max`/`maxplaces` on both factors | the product's interval is `[0.005 .. 40000]`, which excludes zero, and its scale is at most four places | yes | build items 2 and 4; no new composition rule |
| (c) with `maxplaces 4` alone | representable scale only; zero still permitted | n/a | nothing to build |
| (d) stored rounded field, `!= 0` guard on it | this stored value is not zero at this division | yes | available now |
| (e) `min 0.01 max 100` on the stored divisor | the stored value lies in `[0.01 .. 100]`, which excludes zero | yes | build item 2 |
| (f) `nonzero maxplaces 4` on a field written by a guarded `set` | this field's stored value is always at least `0.0001` from zero, at every division by it | yes | build item 3, plus build item 1 if the establishing guard uses `abs()` |
| (g) `rule abs(Rate) >= 0.05 because "…"` over the divisor field | this field is always at least `0.05` from zero, either sign, in every configuration, at every division by it | yes | build item 3 for the rule's own establishment and preservation, plus build item 1 reachable from the rule-sourced fact path |
| (h) `in Priced ensure abs(Rate) >= 0.05 because "…"` | the same claim, scoped to the states where the domain says it holds, at every division reachable in those states | yes | build item 3 extended to the `ensure` form, plus build item 1 reachable from the ensure-sourced fact path |

Rows (b), (c) and (f) declare the identical modifiers. The difference is entirely in where the value
comes from and whether anything establishes the claim there. That is why the table separates them, and
why the earlier one-line answer — "the modifiers are a claim the computation cannot prove" — was too
broad. It is untrue of the guarded `set`, and untrue of a product whose factors are themselves bounded.
What it is true of is a product of two values that may be arbitrarily close to zero, which was the
original freight example and remains correctly refused.

Three different guarantees appear in that table and should not be run together: *not exactly zero*, *at
least `k` away from zero*, and *representable at this scale*. Only the second survives further arithmetic
in any useful way, and even it is not a guarantee that the division result is representable — a divisor
floored at `0.0001` multiplies the numerator by up to ten thousand, which is an overflow question, not a
divisor question.

### The answer to the question as asked

A guard of `abs(x) >= k` and field modifiers of `nonzero maxplaces N` are not the same statement, but for
a stored decimal that is genuinely governed, and where `k` is a power of ten, they exclude the same
values and are equally good evidence. Which one to write depends on how the divisor is used, not on which
is stronger.

Where they genuinely differ is what has to happen for the modifiers to mean anything. They are a claim
about the field's stored value, so something has to establish that claim at every write and preserve it
at every other write. When the value comes straight from a product of two possibly-tiny operands, nothing
establishes it and the definition is refused — that was the original freight example, and that judgment
stands. When the operands carry declared bounds that hold them away from zero, the product's own interval
establishes it and the derived field is accepted. When an explicit `set` writes the field under a
condition the compiler can check, the claim is established there, and every division by the field
afterwards is covered by one declaration instead of a repeated guard.

The remaining limits are real but narrow: `maxplaces` can only imply a power-of-ten floor, it exists only
on the `decimal` lane, and the floor moves if the declared scale is later widened. All three are limits
of the modifier pair, not of the declare-once approach — a `rule` over the same field states an arbitrary
threshold literally, on any numeric type, and does not move. Form (g) above covers it.

## Authoring patterns, and when each one fits

All four are legitimate. None is a fallback from another. Pick by how the divisor is used and what shape
the domain minimum has.

**1. One division, no reuse — guard at the division site.**

```precept
from Active on Recalculate when abs(ComputedRate) >= 0.0001
    -> set NormalizedCost = Recalculate.TotalCost / ComputedRate
    -> no transition
```

Lightweight and local. The fact is established immediately before it is used, and nothing about the rest
of the file has to be understood to see that the division is safe. When the value is divided by exactly
once, there is nothing to be gained by giving it a governed home of its own. If there is no domain
minimum at all, `!= 0` is the honest guard and `abs(x) >= k` should not be invented to replace it.

**2. A divisor reused in several places, with a power-of-ten domain minimum — materialize it into a
`nonzero maxplaces N` field.**

```precept
field EffectiveRate as decimal nonzero maxplaces 4 default 1

from Active on PublishRate when abs(round(PublishRate.Raw, 4)) >= 0.0001
    -> set EffectiveRate = round(PublishRate.Raw, 4)
    -> no transition
```

Then divide by `EffectiveRate` in as many handlers and rules as the domain calls for, with no guard at
any of them. This is the recommended form for the reuse case, and Shane's reasoning for it is right on
both counts:

- *It is self-documenting.* A reader of the field declaration learns the rate's contract in one line, at
  the place that defines what the rate is. Under the guard-at-each-site form, the same reader has to find
  every division and read a threshold out of a guard expression to reconstruct the same contract, and has
  no way to know they have found all of them.
- *It is stated once.* With N division sites, the guard form writes the threshold N times and depends on
  a person keeping N copies in step. Change the domain minimum and every one of them has to be found and
  edited; miss one and the file still compiles, because a stale `abs(x) >= 0.0001` is still a valid proof
  of non-zeroness — just no longer the domain's rule. The field declaration has one place to be right and
  one place to change.

The cost is honest and small: the floor is implied by the scale rather than written as a minimum, so
record the domain minimum in a `describe` text next to the field and treat a later change of
`maxplaces` as a change to the contract. If that implicitness bothers you, pattern 3's rule form works
just as well for a power-of-ten floor as for any other number, and states it outright.

**3. A divisor reused in several places, with a domain minimum that is not a power of ten — declare the
threshold as a `rule`.**

**Rewritten 2026-07-26.** Shane asked whether a rule could say what `maxplaces` cannot. It can, and it is
the right answer for this case — better than either of the two forms this section previously offered.

`maxplaces` cannot say `0.05` or `0.25`. Pattern 2 does not fit, and stretching the declared scale to the
nearest power of ten in order to use it would state a weaker rule than the domain's while looking like a
stronger one. But the threshold does not have to be carried by a modifier at all:

```precept
field EffectiveRate as decimal default 1 editable

rule abs(EffectiveRate) >= 0.05 because "the rating engine cannot price a shipment below a nickel per unit"
```

Then divide by `EffectiveRate` wherever the domain calls for it, with no guard at any division. This
keeps both of the properties that make pattern 2 worth choosing — the contract is stated once, and it is
stated where the reader will look for it — and it drops both of pattern 2's costs. The number `0.05` is
written literally rather than implied by a scale, so it does not move when someone later widens the
field, and the mandatory `because` carries the reason, so there is nothing to record in a separate
`describe`.

It also reaches a case nothing else here does. A rate that may be negative, held some distance from zero
in both directions, cannot be described by a declared interval, because one contiguous range cannot
exclude a hole in the middle. `abs(x) >= k` in a rule describes it exactly.

This is ordinary Precept, not a trick. Rules stating arbitrary domain thresholds are all over the
samples — `rule TotalCreditHours <= 21`, `rule RequiredDeposit >= QuotedTotal * 0.25`,
`rule ScheduledMinute % 15 == 0`. A magnitude floor on a rate is the same kind of statement.

**What the author is taking on.** A rule is a claim about every configuration the entity can reach, so
the same two duties as pattern 2 apply, and for the same reason — a claim consumed as a premise at a
division site is worth nothing unless something makes it true and everything keeps it true. Something
must establish the rule at creation, and every write that could falsify it must be proved not to. The
first half exists today: an unguarded rule is folded against the field defaults, and a definition whose
defaults violate the rule is refused (`PRE0164`). The second half is build item 3, the same work that
makes pattern 2 sound. Until it lands, a handler that drives the rate below the floor compiles clean
while the rule is being used to discharge divisions elsewhere in the file. Where the divisor is written by
a row action the runtime does refuse that handler when it fires, because actions run at step 2e and rules
at 2h, so nothing is stored and what is missing is only the compiler telling the author their handler can
never succeed. Where the divisor is itself a computed field and the division sits in another computed
field, both recompute at step 2g and the division faults before any rule is consulted — so there the
runtime is not a backstop at all, and the author meets a divide-by-zero fault instead of the constraint
failure they wrote the rule to get. See form (g)'s preservation bullet for the reproduction.

**Three things to know before choosing it.**

- *Guard the rule and it stops proving anything.* A `rule ... when SomeFlag` holds only where its guard
  holds, so it contributes no standing fact and discharges no division. If the floor genuinely applies
  only in some configurations, the guard belongs at the division site instead.
- *A rule cannot mention event arguments.* The spec confines rules to field scope so they stay valid
  across every event. A threshold on a value that arrives as an event argument goes on the argument's own
  declaration or in the handler's guard.
- *The floor sits on its own line, not on the field.* A reader scanning field declarations alone will not
  see it. Put the rule directly beneath the field it governs.

**The two older forms, and when they still apply.** Neither is wrong; both are now second choices.

- *Materialize and guard.* Store the value into an ordinary field under a guarded `set`, then write
  `abs(x) >= 0.05` at each division site. This still repeats the threshold once per division, which is
  the maintenance cost the rule form avoids. It remains the right form when the floor is conditional —
  when it applies on some paths and not others — because that is exactly what a guarded rule cannot do.
- *Materialize and declare an interval*, once the divisor rule reads declared intervals — build item 2:

  ```precept
  field EffectiveRate as decimal min 0.05 max 100 default 1
  ```

  An interval bound is an arbitrary number, so it states `0.05` directly, and for a divisor the domain
  holds to one sign this is the most direct thing to write — the bound is part of the field's type
  contract and is checked on every write by the same machinery that checks every other bound. Prefer it
  over the rule form whenever the rate is sign-definite. It cannot cover a signed divisor bounded away
  from zero in both directions; that is where the rule form is the only one that fits.

The pressure for a declarative magnitude constraint in field syntax is correspondingly lower than the
earlier version of this section implied. Between an interval for the sign-definite case and a rule for
the signed case, the language already covers both, and neither needs new syntax.

**4. The divisor is computed from other governed fields — constrain the factors and let the derived
field carry the claim.**

```precept
field TariffBase as decimal min 0.01 max 10000 maxplaces 2 editable
field FuelMultiplier as decimal min 0.5 max 4 maxplaces 2 editable
field EffectiveRate as decimal nonzero maxplaces 4 <- TariffBase * FuelMultiplier
```

This is the pattern for a rate that is genuinely a function of its inputs and should be recomputed
whenever they change, rather than published through an event. It keeps the derived form — no `set`, no
staleness between the inputs and the rate — and it earns the `nonzero` claim rather than asserting it,
because `[0.01 .. 10000] * [0.5 .. 4]` is `[0.005 .. 40000]` and that interval has no zero in it. Every
division by `EffectiveRate` is then discharged from the field's declaration, with no guard at any
division site, exactly as in pattern 2.

The bounds have to be the domain's. A tariff base that the business genuinely allows to be one cent, and
a fuel multiplier the business genuinely holds between a half and four, are contract statements worth
writing down for their own sake; the divisor proof is a consequence, not the motive. Inventing
`min 0.01` on a value the domain permits to be arbitrarily small is the same mistake as inventing a
threshold guard, and it is worse here because the bound is enforced on the input as well.

Give both ends. The upper bounds are what discharge the overflow obligation on the multiplication; lower
bounds alone leave the definition refused for overflow instead of for division. And keep the factor
scales summing to no more than the derived field's — two-place factors for a four-place rate — or the
product violates `maxplaces 4` even though it is safely away from zero.

Choose pattern 2 over this one when the rate is published by a person or an upstream system rather than
computed, when the inputs have no defensible bounds, or when the rate has to hold steady while its
inputs move.

## The `number` lane

Everything above this point reasons about `decimal`. The worked examples are rate cards and freight
charges, the magnitude floor is `System.Decimal`'s `1e-28`, the ceiling is its `7.9e28`, and the eight
divisor forms lean twice on `maxplaces`, which only `decimal` has. That is a fair reflection of where
Precept's business definitions live, and it is also a gap, because the question that started this whole
line of work was asked about `number` and not about `decimal`. Canon records it at
[`primitive-types.md:667`](../../language/primitive-types.md):

> **Open, not covered by either arm — underflow.** Two non-zero finite `number` operands can multiply to
> exactly `0.0`. That is neither `NaN` nor an infinity, so neither case above reaches it, and it breaks
> divisor safety directly: `nonzero` on every operand does not yield a non-zero product. Recorded as an
> open question, not yet routed.

Most of the recommendation carries over unchanged. Two parts of it do not exist on this lane at all, one
part is weaker there in a way that matters, and one hazard has no counterpart on `decimal` whatsoever.

### What canon already fixes about `number`

`number` is the approximate lane, backed by IEEE 754 binary64. It is terminal — nothing widens out of it,
and `decimal` and `number` never mix in arithmetic, assignment, comparison, function arguments or
defaults. Crossing is explicit, through `approximate(decimal) -> number` one way and `round(number, n)
-> decimal` or `floor(number) -> integer` the other. Business-domain types never reach it: `money`,
`quantity`, `price` and `exchangerate` are all `decimal`-backed, so
[what money and quantity change](#what-money-and-quantity-change) is a `decimal`-only discussion by
construction.

Its constraints are `nonnegative`, `positive`, `nonzero`, `min`, `max`, `optional` and `default`. There
is no `maxplaces` — a base-10 scale is not a property a binary float has, and declaring one is a type
error.

Two commitments in canon then decide the shape of everything below. The first is the spec's Totality
principle, Design Principle 10 in §0.1 (`precept-language-spec.md:110`): *every expression evaluates to a
result — never silent `NaN`, `Infinity`, or `null`.* The second is canon's non-finite position, recorded
at `primitive-types.md:660` and dated 2026-07-21, which separates the two non-finite cases because their
entry paths differ:

- **`NaN` is prevented.** Its entry paths are enumerated and each is closed. `approximate()` cannot
  manufacture one, because `decimal` has no `NaN` and its whole range sits far inside binary64's. `sqrt`
  of a negative already carries a stamped precondition. `0.0 / 0.0` is the divisor obligation itself. The
  one path left is a non-finite value arriving from outside the definition, in an event argument or a
  write to an editable field, and ingress governance is what covers that.
- **`+∞` and `-∞` are deferred**, riding with the arithmetic overflow model. Their remaining path is
  arithmetic — finite operands manufacturing an infinity mid-expression, which no check at the boundary
  can reach. A `NaN` that arises *from* an infinity, through `∞ - ∞` or `∞ / ∞`, follows the infinity
  case rather than the `NaN` one, and is deferred with it.
- **Underflow is open**, quoted above, and unrouted.

Canon then states the consequence that this document has to answer for:

> On the `number` lane the qualifier modifiers `nonzero`, `positive`, and `nonnegative` therefore
> establish the predicate *and* not-`NaN`, checked where the value enters. Without the added clause the
> modifier would establish nothing usable: `NaN != 0` evaluates true, so a `nonzero` divisor could be
> `NaN` and pass.

Canon saw the hazard and closed it for field modifiers. It did not close it for guards, and a guard is
the form this document recommends first.

#### Where the not-`NaN` clause comes from, stated precisely

**It is derived and forced, not an owner ruling, and this document earlier said otherwise.** An earlier
revision cited the clause six times as "the owner's non-finite ruling of 2026-07-21." That attribution
was wrong. `the-walk.md:377–392` put this exact item to the owner as item 14; he marked it **not mine**,
with the word "derived", and on a plain re-ask said the question had never been put to him. The walk
records the clause as standing "as derived and forced."

Nothing about the clause weakens under the correction, because what holds it up was never an
attribution. The argument is one line and it is the walk's own: **`NaN != 0` evaluates true under IEEE
comparison, so an unqualified `nonzero` on this lane establishes nothing a divisor proof can use — a
`NaN` divisor would satisfy it and pass.** A modifier that admits the one value the obligation exists to
exclude is not a weaker fact than intended; it is no fact at all. So the clause is forced by what the
lane makes true, and it would be forced identically if nobody had ever written it down.

What *does* change under the correction is scheduling, and it changes in one specific place. Requiring
the not-`NaN` claim is forced and belongs with the proof work — that is item 5d, and it stays there.
Changing what the modifiers `nonzero`, `positive` and `nonnegative` *mean* on this lane is a
modifier-semantics change, and `primitive-types.md` says so in its own voice twelve lines below the
heading quoted above: *"The change to what a qualifier modifier establishes on the `number` lane is a
modifier-semantics change and is designed before it ships; this section states the target."* An earlier
revision of this document scheduled it as mechanical catalog sync, which routed a modifier-semantics
decision past the pre-design consultation gate on the strength of an attribution that did not hold. It is
now carried as a design-queue item — see build item 8 and
[Work that would require owner consultation](#work-that-would-require-owner-consultation-before-it-is-built).

**The same item 14 governs the underflow half, and this document is its preparation rather than its
answer.** See [Underflow policy](#underflow-policy).

### Can a `number` field hold one of these values today?

This has to be settled before anything else, because it decides whether `Infinity` and `NaN` are hazards
Precept has to reason about or merely representations that never occur. The answer is that nothing stops
them.

Canon is explicit that the representations exist. `primitive-types.md:650`: *IEEE 754 `double` can
represent `+∞`, `-∞`, and `NaN`. Precept's `number` lane inherits these representations.* Not "excludes"
— inherits. The lane is binary64 and it comes with binary64's full value set.

Canon is equally explicit that they must not occur, through the spec's Totality principle, Design
Principle 10 in §0.1 (`precept-language-spec.md:110`):
*every expression evaluates to a result — never silent `NaN`, `Infinity`, or `null`.* So the position is
that these values are representable and forbidden, and something has to do the forbidding.

Nothing does, yet. Four checks:

- **No diagnostic and no fault names them.** `DiagnosticCode` has exactly one code for this whole family,
  `NumericOverflow` (PRE0078), and `FaultCode` has its counterpart. There is no code for a value being
  `NaN` and none for a value being infinite. The one code that exists describes magnitude, which is the
  question `NaN` does not answer.
- **No code anywhere handles them.** Searching the compiler and the runtime for any test of finiteness —
  `IsNaN`, `IsInfinity`, `IsFinite`, or any spelling of non-finiteness — returns nothing at all.
- **The modifier metadata does not carry the clause the ruling assigned it.** The `Nonzero` entry in
  `Modifiers.cs` declares a single satisfaction, `SelfValue ≠ 0` against a `decimal` constant, with no
  lane distinction and no finiteness component.
- **The value boundary is not built yet, so there is nothing there to check either.** `PreceptValue`, the
  type every field and argument crosses the runtime boundary as, is a declared hierarchy whose
  construction and conversion members currently throw. Ingress governance is documented as enforcing a
  field's *declared* constraints on externally-sourced values, which is the right design and also the
  limit of it: a bare `number` field with no modifier declares nothing, so there is nothing for ingress
  to enforce.

So the honest statement is that `number` can hold `+∞`, `-∞` and `NaN` today, that canon forbids all
three, and that the forbidding is a ruling rather than a mechanism. That is why the two values need
treating as hazards in their own right rather than as edges of the overflow question, which the next two
subsections do.

They are also hazards of visibly different shapes, and the difference decides how each should be handled:

| | `+∞` / `-∞` | `NaN` |
|---|---|---|
| How it arises | one operation exceeding the lane's magnitude | `∞ - ∞`, `∞ / ∞`, `0 * ∞`, `0.0 / 0.0`, `sqrt` of a negative |
| Where it sits in the order | at the top and bottom, ordered correctly against every finite value | nowhere. It is unordered with everything, including itself |
| Against a declared bound | violates an upper bound, as any too-large value does | satisfies no bound and violates none |
| Under a `!= 0` test | passes, and it should not | **passes, and it must not** |
| Under `abs(x) >= k` | passes | fails, which is the safe direction |
| What it does downstream | poisons arithmetic in a recoverable way — `1 / ∞` is `0`, which is finite | poisons everything it touches, permanently, and every comparison that could detect it is false |
| Can an interval carry the claim | yes; it is the endpoint | no |

### `+∞` and `-∞`, as a hazard in their own right

On `decimal` the overflow argument has a convenient shape. The operation throws, the runtime has nowhere
to go, so refusing at compile time is the only alternative to a fault the author cannot handle. `number`
removes that convenience entirely. Nothing throws. `1e300 * 1e300` is `+∞`, and `+∞` is a perfectly
ordinary value that arithmetic keeps operating on.

**Overflow on this lane should be modelled as producing a specific value, not as refusing an
operation.** That is the difference from `decimal` and it is not a detail of wording. On `decimal` the
model is that the operation does not complete; the analysis after it only has to describe the case where
the result fit, which is what makes trapping arithmetic the easy case for interval analysis. On `number`
the operation always completes, and it completes with `+∞` or `-∞` — so the analysis after it has to
describe a state in which the value is infinite. Modelling `number` overflow as a refusal describes
something that does not happen.

Analytically this is the good news. Saturation to a distinguished endpoint is the friendliest of the
three arithmetic behaviors. A trap removes the case; a wrap scatters it to the far end of the range,
which is why a wrapped result's image under an interval abstraction is two disconnected pieces that no
single interval covers; a saturation puts it at the endpoint, where an interval already reaches. Infinity
orders correctly against every finite value, so `[a .. +∞]` is a well-formed interval and an obligation
of the form *this result is finite* is an ordinary comparison against the lane's maximum. The overflow
research separated trapping from wrapping and concluded trapping was materially easier; it then noted —
flagging it as the weakest claim in that finding — that no surveyed analyzer treats saturating arithmetic
at all. That absence is an absence of precedent, not a technical obstacle, and the note matters mainly so
that nobody later reads the trapping argument and assumes this lane was covered by it. It was not.

**The reason to refuse is different, and it is the reason the case cannot simply be tolerated.** On
`decimal` a compile-time refusal replaces a runtime fault. On `number` there is no runtime fault to
replace, so the refusal is the only thing there is, and what demands it is Totality rather than
fault-avoidance. A `decimal` overflow stops the computation where it happened. A `number` overflow stops
nothing: it substitutes a value and lets the rest of the definition run on it, and the substituted value
then transforms into other things. `1 / ∞` is `0` — finite, ordinary-looking, and now a zero that no
guard upstream expected, which can walk straight into a division. `∞ - ∞` and `∞ / ∞` are `NaN`, which is
the harder hazard below. So a single overflow four operations upstream can present as a divide-by-zero or
as a `NaN`, with nothing at the point of failure pointing back at the operation that caused it.

**It should carry its own obligation, distinct from fitting a declared bound.** The claim wanted is *this
result is finite* — that the operation did not reach the lane's magnitude limit. That is genuinely the
same shape as the representability obligation recommended for `decimal`, and it can ride on the same
machinery, with the lane's maximum as the comparison point. What it must not do is get folded into the
containment check against a field's *declared* interval, because a field with no declared bounds gets no
containment obligation at all and is exactly where an infinity would arrive unnoticed. That distinction
is item 5b, and on this lane it is the difference between catching the case and not.

The one thing blocking it is representational rather than conceptual: the interval type cannot express
this lane's maximum, which is the subject of
[The interval cannot hold this lane](#the-interval-cannot-hold-this-lane).

### `NaN`, as a hazard in its own right

`NaN` is not a large value, a small value, or an edge of the range. It is a value that is unordered with
every other value including itself, and that property — not its origin — is what makes it the more
serious of the two.

**Where it comes from.** Five producing forms, and they divide into two groups by whether Precept already
has something to say about them:

| Form | Status in Precept |
|---|---|
| `0.0 / 0.0` | already the divisor obligation — this is the case the obligation exists for |
| `sqrt` of a negative | already a stamped precondition, refused at HEAD with its own diagnostic |
| `∞ - ∞` | reachable only through an infinity, so it rides the deferred case |
| `∞ / ∞` | the same |
| `0 * ∞` | the same |
| a non-finite value arriving from outside the definition | ingress governance, and only where a modifier was declared |

Canon's non-finite position makes exactly this split, and it is why canon calls `NaN` prevented while
calling
infinity deferred: three of the paths were already closed by obligations Precept has, the fourth is a
boundary question, and every remaining path runs through an infinity and therefore belongs to the other
case. The split is sound. What it means in practice is that **the `NaN` case is only as closed as the
infinity case is**, and the infinity case is deferred.

**Why it is worse than a too-large finite value.** Three properties compound:

- **It propagates through nearly everything and never recovers.** Almost any arithmetic operation with a
  `NaN` operand produces a `NaN`. There are documented IEEE exceptions — `pow(NaN, 0)` is `1.0`, and
  `Functions.cs` declares a `pow` overload with a `number` base and an `integer` exponent that would
  reach it — so the propagation is not literally universal. It is close enough to universal to be
  treated as such, and the exceptions make the hazard smaller rather than larger. There is no operation
  that brings a computation back the way an infinity can return to finite territory through `1 / ∞`. One
  `NaN` early in a definition makes essentially every value derived from it a `NaN`, and the property the
  argument actually rests on is the next bullet, which has no exceptions.
- **No bound detects it.** `x <= max` is false for a `NaN` and `x > max` is also false. It satisfies no
  upper bound and violates none, so it is neither inside a declared interval nor outside one. Every
  mechanism Precept has for constraining a numeric value works by comparison, and comparison is precisely
  the mechanism that cannot see a `NaN`.
- **It makes guards behave as though nothing were wrong.** A guard is a comparison. Every ordered
  comparison against a `NaN` is false, so a guard that admits on a comparison refuses the event — which
  is at least safe — but a guard that admits on a *disequality* passes, because disequality is the one
  comparison that is true for a `NaN`. That is
  [A disequality guard is weaker than a magnitude guard](#a-disequality-guard-is-weaker-than-a-magnitude-guard-and-only-on-this-lane)
  below, and it is where the soundness question actually lives.

**It needs its own obligation, and no interval can carry it.** *This result is finite* and *this result
is not `NaN`* are different claims, discharged by different evidence, and only the first is a question
about magnitude. An interval containment obligation asks whether a value lies within a range; for a
`NaN` that question has no answer, so a containment obligation cannot absorb the case as a special value
at the end of the range no matter how the range is written. This is build item 5d, and it is the one
requirement in the whole recommendation that no numeric domain discharges.

**It also deserves its own diagnostic.** Today there is one code for this family, `NumericOverflow`,
which describes magnitude. Reporting a possible `NaN` under a code that says a value was too large tells
the author the wrong thing about what to fix — a magnitude problem is fixed with a bound, and a `NaN`
problem is not fixable with a bound at all, because bounds cannot see it. The two should be separately
named so the recovery advice can differ.

### The obligation is stated in a shape IEEE makes weak

The divisor requirement is declared once in the operation catalog and reused across lanes. For the
`number` lane it reads, in `Operations.cs`, exactly as it does for `decimal`:

```
new NumericProofRequirement(new ParamSubject(PNumber), OperatorKind.NotEquals, 0m,
    "Divisor must be non-zero"),
```

The comparison is `≠`, and the constant is a .NET `decimal` zero. On `decimal` that is precisely the
right property: the only value that makes division fail is zero, and `≠ 0` excludes it exactly. On
`number` it is not. Division by zero on that lane does not fail at all — `1.0 / 0.0` is `+∞`, `-1.0 /
0.0` is `-∞`, and `0.0 / 0.0` is `NaN`. So the obligation is not standing between the definition and a
trap. It is standing between the definition and a value Totality forbids. And the property that keeps a
`number` division inside Totality is not "the divisor is not zero." It is "the divisor is finite, is not
zero, and is not `NaN`."

Stated the way the recommendation states things elsewhere: **on the `number` lane the divisor obligation
should be a finiteness-and-non-zeroness obligation, not a disequality against zero.** The name it carries
today is `≠ 0` because the requirement was written once for a lane where those two things coincide.

### A disequality guard is weaker than a magnitude guard, and only on this lane

**First, the question this rests on, answered directly: Precept's `!=` on `number` is IEEE comparison,
not something stricter.** The operation catalog says so in its own description — `NumberEqualsNumber` is
"Number equality (IEEE 754)" and `NumberNotEqualsNumber` is "Number inequality (IEEE 754)" — and
`primitive-types.md` describes the lane's comparison the same way, adding that the proof engine cannot
prove exact equality on it. There is no Precept-specific comparison layer that normalizes a `NaN` before
the test, and nothing anywhere in the compiler or runtime tests for one. So IEEE's comparison results are
Precept's comparison results, and the rest of this subsection follows from that.

Three IEEE facts settle the whole question, and they are not symmetric:

| Test | Result when the value is `NaN` | What that does to a guard |
|---|---|---|
| `x != 0` | **true** | The guard passes. `NaN` reaches the division. |
| `x == 0` | false | — |
| `x > 0`, `x >= k`, `x < k`, `abs(x) >= k` | **false** | The guard fails. `NaN` is refused entry. |
| `x == x` | false | The classic self-inequality test for `NaN`. |

Every ordered comparison against `NaN` is false, including both directions, which is why `NaN` is neither
inside a range nor outside it. Disequality is the one comparison that is *true* for `NaN`, and it is the
one the divisor obligation is written in.

**Stated plainly, because it is the most important thing in this section.** A guard of the form
`when ComputedRate != 0`, which this document recommends for `decimal` as sound and workable evidence,
does not exclude a `NaN` on the `number` lane. It admits one. A `NaN` divisor then produces a `NaN`
result, which no declared bound rejects and no later comparison detects, and which Totality forbids. This
is a `number`-specific soundness gap with no `decimal` equivalent — `decimal` has no `NaN`, so on that
lane `≠ 0` excludes exactly the values that make division fail, and the recommendation stands unchanged
there. On `number` it does not, and a `!= 0`-shaped divisor guard cannot be trusted on that lane until
something separately establishes that the divisor is not a `NaN`. That is build item 8 for the modifier
and requirement metadata, and item 5d for the obligation itself.

The consequence for the eight forms is direct, and it is worth naming the shapes rather than the letters,
because the bare `!= 0` guard is not one of the eight at all. **The direct `!= 0` guard at the division
site** — the form
[when the domain has no defensible minimum](#when-the-domain-has-no-defensible-minimum) recommends as the
honest one — does not exclude `NaN` on the `number` lane. **Form (a), the `abs(x) >= k` magnitude guard**,
does, and it does so for free, without any test being added, because the comparison it is already written
in returns false for `NaN`. The same holds for a plain `> 0` guard. **Form (b)**, `nonzero` declared on a
derived field, is unaffected on this point, because it is not a route to a proof on either lane.

That reverses the ordering this document gives elsewhere. On `decimal` the choice between `!= 0` and
`abs(x) >= k` is a question of whether the domain has a defensible minimum, and if it does not, the bare
disequality is the honest form. **On `number` the magnitude guard is the sounder form regardless**,
because it discharges more of the obligation than it was written for. Where the domain genuinely has no
minimum, the honest form on this lane is a disequality *plus* something that establishes finiteness —
either a `nonzero` declaration on the field, which under canon's derived not-`NaN` clause carries it,
or an explicit `x == x` test, which is the standard idiom and which Precept can already express.

Two grounding notes, because they change scope rather than direction. First, both forms are recognized at
HEAD: `when Baseline != 0` on a `number` field discharges the obligation through `GuardInPath`, and
`nonzero` on a `number` field discharges it through `DeclarationAttribute`. Neither carries a finiteness
component. Second, the not-`NaN` clause the ruling assigns to the modifiers is a decision and not yet a
mechanism — the `Nonzero` entry in `Modifiers.cs` declares a single satisfaction, `SelfValue ≠ 0` against
a `decimal` constant, with no lane distinction and no finiteness clause, and searching the compiler and
the runtime for any handling of `NaN` or infinity returns nothing at all. The ruling has been made. The
catalog has not been taught it. That is exactly the kind of gap `CLAUDE.md`'s catalog notice describes,
and it sits on the divisor path.

### How wide the `NaN` hole actually is

Honesty about size matters here, because "a guard form is unsound" reads worse than the situation
warrants and better than it warrants at the same time.

It is narrower than it sounds because canon closed three of the four `NaN` entry paths, and the third is
live at HEAD — `sqrt` of a possibly-negative argument is refused today, with its own diagnostic. A `NaN`
cannot be written as a literal and cannot arrive through the `decimal` bridge.

It is wider than it sounds for two reasons. The remaining entry path is a value from outside the
definition, and ingress governance only closes it *where the author declared a modifier that carries the
clause*. A bare `number` field with no modifier, or a bare `number` event argument, has nothing to carry
it. The author who writes `when Baseline != 0` is precisely the author who did not declare `nonzero`,
because the guard is what they wrote instead. And the second reason is the deferred infinity case: while
overflow is unproven on this lane, finite operands can still manufacture an infinity mid-expression, and
`∞ - ∞` or `∞ / ∞` produces a `NaN` inside the definition, downstream of every check at the boundary.

So the recommendation is not "the guard is broken." It is that on the `number` lane the disequality guard
carries less than it appears to, the magnitude guard carries more, and the difference is not stylistic.
Both should be recognized, and the compiler should be able to tell the author which one it is looking at.

### Negative zero is not the problem it looks like

IEEE has two zeros, and a product that underflows keeps the sign of its operands, so `-0.0` is reachable
on this lane in a way it is not on `decimal`. It turns out not to complicate the divisor question.
`+0.0 == -0.0` is true under IEEE comparison, which is what `number` comparison is documented as using, so
`x != 0` rejects both zeros and `abs(x) >= k` rejects both. A guard written the obvious way does the right
thing without knowing the second zero exists.

Where the sign does show is downstream. `1.0 / 0.0` and `1.0 / -0.0` produce infinities of opposite sign,
so a division that slips past an inadequate guard poisons the computation in a direction that depends on
a sign the author never sees. That is an argument for the divisor obligation, not a separate hazard, and
it needs no separate treatment.

### Underflow: the lane the model was designed for

`number`'s underflow is gradual where `decimal`'s is abrupt. Below roughly `2.2e-308` a binary64 value
becomes subnormal, keeping its magnitude but shedding precision digit by digit, until below about
`4.9e-324` it flushes to a signed zero. `decimal` has no subnormals — it has a fixed maximum scale and a
smallest representable magnitude of `1e-28`, and it reaches zero in one step.

The policy does not change. [Underflow should change the interval, not become a general
rejection](#underflow-should-change-the-interval-not-become-a-general-rejection) applies to this lane
exactly as written: let the zero into the interval where the arithmetic can produce it, do not reject the
multiplication that produced it, and refuse at the division where zero is actually forbidden. If
anything the argument is stronger here, because this is the lane the surveyed analyzers were built for.
Miné's smallest-positive constant, the one that makes the interval multiplication admit zero for two
non-zero operands, is a binary floating-point constant; Astrée and Frama-C EVA both model binary doubles;
the transfer of that model to a fixed-scale base-10 type was the inferential step the underflow research
flagged as its own weakest point. On `number` there is no inferential step. The model applies directly.

That routes the open question quoted at the top of this section, and routing is a smaller claim than
settling — see the note below. Canon recorded that `nonzero` on every operand does not yield a non-zero
product and left it unrouted. The routing is the one this document already recommends for `decimal`: the
claim belongs to the stored divisor, not to the operands, and the arithmetic that produced it is allowed
to admit zero.

The subnormal range does introduce one thing `decimal` does not have. A value can be non-zero, finite,
and carry only a handful of significant digits, so a magnitude guard at `1e-320` is technically satisfied
by a value whose arithmetic is nearly meaningless. That is a precision concern rather than a safety one —
the division is well defined and the result is finite — and it is not something the divisor obligation
should be asked to police. It is worth saying plainly in author-facing documentation for the lane, and
nothing more.

### The rule route reaches this lane; spelling a fractional threshold does not

The revision that added form (g) — a declared unguarded `rule` over the divisor field, standing in for a
threshold `maxplaces` cannot express — matters more on `number` than on `decimal`, because `maxplaces`
does not exist here at all. Forms (c) and (f) and the whole of authoring pattern 2 are `decimal`-only by
construction. The rule is what is left for an arbitrary threshold.

**An earlier revision of this section claimed the rule route does not reach this lane, and that the build
item carrying rule-sourced facts therefore had an uncosted lane dimension. Both claims were wrong and are
withdrawn.** They rested on a comparison in which the two cells differed by more than the lane. Six cells
run against HEAD:

| Lane | Threshold as written | Result |
|---|---|---|
| `decimal` | `0.05` | Proved, `CompositionalConstraint` |
| `decimal` | `5e-2` | Unresolved, PRE0083 |
| `decimal` | `5.0e-2` | Unresolved, PRE0083 |
| `number` | `0.05` | PRE0018 type error; obligation Unresolved |
| `number` | `5e-2` | no diagnostic on the literal; obligation Unresolved |
| `number` | `1` | **Proved, `CompositionalConstraint`** |

The last row is decisive. A `rule` over a bare `number` field discharges the divisor obligation through
the same `CompositionalConstraint` strategy the `decimal` lane uses. **The rule route works on `number`
today.** There is no lane gap in the proof engine here and nothing extra for that build item to carry.

What is real, and narrower, is that **there is currently no way to spell a fractional threshold on the
`number` lane at all.** Both spellings fail, for unrelated reasons:

- `rule Baseline >= 0.05` on a `number` field is PRE0018, *"Expected a number value here, but got
  'decimal'"*. The fractional literal resolves to `decimal` and the two lanes do not mix. That is a
  literal-typing question in rule position, it is genuine, and it is separate from anything about proofs.
- `rule Baseline >= 5e-2` type-checks and is **silently wrong**. Exponent notation is not parsed anywhere
  in the language: in `TypeChecker.Expressions.cs`, a literal without a `.` goes to `long.TryParse` and a
  literal with one goes to `decimal.TryParse` under `NumberStyles.AllowDecimalPoint`, which excludes
  `AllowExponent`. Both discard the boolean result, so every exponent literal becomes the value **0**
  everywhere downstream. `5e-2` and `5.0e-2` both do this. The author has written `rule Baseline >= 0`
  and nothing tells them so — the only diagnostic is PRE0083 pointing at the division, which says nothing
  about the rule. On a field declared `positive` the degenerate rule does trip PRE0154 ("governs
  nothing"), which is a partial rescue and only where the vacuity happens to be provable.

The second of those is a defect in the language, not in this recommendation, and it is not confined to
rules — any `1e6` written anywhere in a guard, rule, ensure or expression is currently the value zero. It
is filed in `docs/Working/bugs.md`. **Until it is fixed, no document should recommend exponent notation as
a workaround for anything**, and an earlier revision of this one did exactly that.

This is a scope note rather than a change of direction. The rule route is the right route on this lane
too, it works, and it needs no new syntax. What it needs is a spelling, and supplying one is the
fractional-literal typing question above rather than proof-engine work.

### The interval cannot hold this lane

The representation problem named in build item 4 is worse here than the `decimal` discussion makes it
sound. `NumericInterval` holds both endpoints as .NET `decimal`, and uses `decimal.MinValue` and
`decimal.MaxValue` as the markers for unbounded. A `decimal`-lane interval at least covers its own lane,
with the one confusion that the largest representable value and "no bound" are the same marker. A
`number`-lane interval covers a fraction of its lane at both ends:

| | Smallest positive magnitude | Largest magnitude |
|---|---|---|
| `number` (binary64) | `4.9e-324` subnormal, `2.2e-308` normal | `1.8e308` |
| `decimal`, and so the interval | `1e-28` | `7.9e28` |

Everything above `7.9e28` and everything below `1e-28` — which is to say the entire subnormal range, the
entire range where underflow actually happens, and about 279 orders of magnitude of ordinary scientific
values above the top (`7.9e28` to `1.8e308`), with roughly 295 below the floor — has no representation in
the domain that is supposed to be reasoning about it.
The lane's own maximum, the number an overflow obligation has to compare against, cannot be written down.

The catalog does already treat this lane distinctly in one place, which is worth crediting: the `number`
multiply and divide operations carry their own interval transfers, which widen the result by a relative
term to allow for rounding. That is an honest attempt at a rounding-aware transfer. It is relative only,
with no absolute allowance at the underflow floor, and it is computed in `decimal` arithmetic, so it
inherits `decimal`'s rounding rather than binary64's. Getting it right is the same build item as before —
it is just that on this lane the endpoint type has to change before the transfer can be correct at all.

### What transfers, what needs a variant, what is new

| | On `number` |
|---|---|
| Prove the final stored divisor, not its operands | Transfers unchanged. |
| Underflow admits zero into the interval; refuse at the division | Transfers unchanged, and routes the open question canon recorded at `primitive-types.md:667`. The cost half stays open. |
| A `rule` over the divisor field discharges the obligation | Transfers unchanged. Verified at HEAD: `rule Baseline >= 1` on a bare `number` field proves through `CompositionalConstraint`. Carries more weight here because `maxplaces` does not exist on this lane. |
| Establishment and preservation duty on any declared fact | Transfers unchanged. |
| `abs(x) >= k` magnitude guard | Transfers, and is *stronger* here — it excludes `NaN` for free. |
| Declared interval on the divisor field | Transfers unchanged; unbuilt on both lanes. |
| Overflow discharged by bounds on the operands, never by a rule over the result | Transfers unchanged. |
| Direct `!= 0` guard at the division site | **Needs a variant.** Sound on `decimal`; admits `NaN` here. Needs an accompanying finiteness fact. |
| The obligation spelled `≠ 0` | **Needs a variant.** Should be finiteness-and-non-zeroness on this lane. |
| The underflow floor | **Needs a variant.** `4.9e-324` with a subnormal range above it, not `1e-28` with nothing. |
| The overflow ceiling | **Needs a variant.** `1.8e308`, and the interval type cannot represent it. |
| `nonzero maxplaces N` implying a magnitude floor | **Does not exist.** No `maxplaces` on this lane. Forms (c) and (f), and pattern 2, are `decimal`-only. |
| Everything about `money` and `quantity` | **Does not exist.** Business-domain types are `decimal`-backed. |
| Excluding `NaN` | **New.** No `decimal` counterpart. Not expressible as a range obligation, and not reportable under the one code that exists. |
| Infinity as a storable value that arithmetic continues on | **New.** No `decimal` counterpart. `decimal` traps instead, so overflow there ends a computation while here it substitutes a value and the computation goes on. |
| Overflow modelled as refusing an operation | **Needs a variant.** Nothing is refused on this lane. Overflow produces `+∞` or `-∞`, and the model has to describe the state that follows rather than the case that did not happen. |
| Saturating rather than trapping arithmetic | **New**, and unprecedented in the surveyed analyzers. |
| Two zeros | New, and harmless — IEEE comparison treats them as equal. |

### What this changes in the build

Most of it rides on machinery already scoped. The recommendation that operation metadata should describe
each lane's rounding, range and behavior at the boundary was written with exactly this in mind, and a
finiteness flag and a lane maximum are two more fields on the same metadata. Three things do not ride on
it and should be named separately, and they appear as the lane note on item 1, item 5d and item 8 in the
build list below.

Nothing here argues for pulling the `number` lane forward. The infinity case is deferred by the owner's
item-15 ruling and this section does not reopen it. What it does change is the content of the deferral:
what is deferred on this lane is not "a fault the author will hit in testing" but "a value the definition
keeps computing on," and the item-15 exemption should be understood in those terms when it comes back up.

## Authoring examples

These examples show the authoring form the model is aimed at. Each is labelled with what it needs.

### Available with the language and proof rules as they stand

For a positive computed rate, guard the value that will actually be used:

```precept
field BaseRate as decimal
field AdjustmentFactor as decimal
field ComputedRate as decimal <- BaseRate * AdjustmentFactor
field NormalizedCost as decimal

from Active on Recalculate when ComputedRate > 0
    -> set NormalizedCost = Recalculate.TotalCost / ComputedRate
    -> no transition
```

The important fact is not that `BaseRate` and `AdjustmentFactor` are non-zero. The handler tests
`ComputedRate`, after its computation and storage semantics have applied.

For a signed divisor with no domain minimum other than non-zero:

```precept
from Active on Recalculate when ComputedRate != 0
    -> set NormalizedCost = Recalculate.TotalCost / ComputedRate
    -> no transition
```

Direct `!= 0` and positive guards on the divisor are already recognized as discharging evidence. On a
`decimal` divisor the two are equally sound. On a `number` divisor they are not — a `!= 0` test is true
for `NaN` and a `> 0` test is false for it, so the positive guard excludes more than the disequality
does. See [The `number` lane](#the-number-lane).

### Existing author syntax that needs proof-engine work

Where the domain forbids rates whose magnitude is below `0.0001`, the author should be able to write:

```precept
from Active on Recalculate when abs(ComputedRate) >= 0.0001
    -> set NormalizedCost = Recalculate.TotalCost / ComputedRate
    -> no transition
```

`abs()` already exists. The function catalog declares type-preserving overloads:

- `abs(integer) -> integer`
- `abs(decimal) -> decimal`
- `abs(number) -> number`
- `abs(money) -> money`
- `abs(quantity) -> quantity`

The last two preserve their qualifier. What is missing is the narrowing rule that turns
`abs(ComputedRate) >= 0.0001` into the fact `ComputedRate != 0`. That is build work against the existing
language, not a new author-facing construct.

### Rounding before division

If a domain boundary requires a six-place stored rate, round explicitly and then test the rounded,
stored value, following the rule in [`round()` is not a substitute for a magnitude guard](#round-is-not-a-substitute-for-a-magnitude-guard):

```precept
field RawRate as decimal
field BillingRate as decimal maxplaces 6
field Charge as decimal

from Active on Settle
    -> set BillingRate = round(RawRate, 6)
    -> no transition

from Active on Calculate when abs(BillingRate) >= 0.000001
    -> set Charge = Calculate.Amount / BillingRate
    -> no transition
```

The guard is on `BillingRate`, not `RawRate`, because `round(RawRate, 6)` may produce zero. `maxplaces 6`
is present because this example gives `BillingRate` a six-place domain contract, not because every rate
should have one. The guard proves a fact about `BillingRate` only; it does not prove that `RawRate`
was non-zero.

### When the domain has no defensible minimum

If the domain permits arbitrarily small non-zero rates within the representation, the author should not
invent a threshold or add `maxplaces` to make the proof easier. The honest form is a direct final-value
guard:

```precept
from Active on Calculate when ComputedRate != 0
    -> set Charge = Calculate.Amount / ComputedRate
    -> no transition
```

If the operation must also reject impractically small rates, then that minimum is part of the domain
contract and should be written as the `abs()` guard.

This is the one authoring recommendation in the document that is lane-specific. On `decimal` the bare
disequality is the honest form and excludes every value that makes the division fail. On `number` it does
not, because `NaN != 0` is true, and the honest form there is either the `abs()` guard, which refuses
`NaN` by construction, or the disequality alongside a `nonzero` declaration on the field, which under
canon's derived not-`NaN` clause carries a finiteness clause the guard does not.

## What is convention, what is build work, and what would need design

### Already expressible and usable as an authoring convention

- Put `maxplaces` only on values whose domain contract limits scale.
- Leave deliberate intermediate and ratio fields unconstrained when they need additional precision.
- Use `round()` explicitly at a domain boundary.
- Guard the final stored divisor with `Divisor != 0` or, for a positive value, `Divisor > 0`. On a
  `number` divisor prefer the magnitude or positivity form over the disequality — `abs(x) >= k` and
  `x > 0` are false for a non-finite value and so refuse it, while `x != 0` is true for one and admits
  it. See [The `number` lane](#the-number-lane).
- Guard the rounded value rather than the value before rounding.
- Where a threshold is a standing property of a field rather than a condition of one operation, write it
  as a `rule` beneath the field. The construct already exists, already takes an arbitrary number, and
  already carries a mandatory reason. A bare comparison of a field against a constant already discharges
  a divisor obligation today; a magnitude form using `abs()` waits on build item 1.
- Declare `max` — and `min` where a factor can go negative — on every field that takes part in a
  multiplication chain, at the domain's own limits. This is the whole of what an author has to do to keep
  a computation inside its lane, and it needs nothing that does not exist: `min` and `max` are ordinary
  modifiers on every numeric lane and are checked on every write today. What is missing is the compiler
  reading them for representability, not the ability to write them.
- Where the domain's real ceiling is on a combination rather than on either field alone, write it as a
  `rule` over the operands — `rule Pallets * UnitsPerPallet <= 5000000 because "…"`. Put it on the
  operands, never on the result: a rule over the result of a multiplication is a claim about a value that
  only exists if the multiplication already succeeded. See
  [Why a rule over the result cannot carry an overflow claim](#why-a-rule-over-the-result-cannot-carry-an-overflow-claim).
- Treat the unit a `quantity` is quoted in as a magnitude decision. A field declared `in 'mg'` *stores*
  milligram numbers and carries them through every operation, so the same physical mass quoted in
  milligrams travels through the analysis six orders of magnitude larger than in kilograms — and it is
  the stored intermediate that the representability obligation is stamped on. (Bound normalization is not
  the mechanism: it converts both sides of a comparison to UCUM base units, so it makes the spellings
  equivalent rather than different. See
  [What money and quantity change](#what-money-and-quantity-change).)

No new syntax is needed for these practices.

### Existing language that needs compiler and catalog work

Each item names the proof technique it rests on; the catalog of techniques considered, and why the rest
were declined, is in
[The proof techniques the research surveyed](#the-proof-techniques-the-research-surveyed-and-which-ones-precept-should-use).

1. **Teach guard narrowing about `abs()`.** The function catalog already describes `abs()` as
   type-preserving and non-negative. It needs metadata describing the inverse guard fact:
   `abs(x) >= k`, for `k > 0`, excludes `(-k, k)` for `x`. The proof engine can then discharge the
   existing numeric `x != 0` divisor obligation from that fact.

   This requires a new proof strategy and proof explanation, but not a new proof-obligation kind. The
   obligation remains “the divisor is non-zero.”

   **Reach it from three places, not one.** A transition-row guard is only the first. The same fact has
   to be available when `abs(x) >= k` appears in a declared `rule` — confirmed at HEAD, a bare
   `rule Rate >= 0.05` already discharges a divisor obligation through the existing
   `CompositionalConstraint` strategy, but the same rule written with `abs()` discharges nothing — and
   when it appears in the default fold that checks a rule against the field defaults (`PRE0164`, which
   likewise sees through a bare comparison and not through `abs()`). Building this against the guard path
   alone leaves the authoring form recommended in
   [Authoring patterns](#authoring-patterns-and-when-each-one-fits), pattern 3, still unprovable.
   Extend the fact extractor to read the sign-explicit spelling
   `rule Rate >= 0.05 or Rate <= 0.0 - 0.05` in the same pass; it is the same magnitude claim written
   without the function call, it discharges nothing today, and reading it is a small addition to the same
   code rather than separate work. One duty rides with it: recognizing the disjunction as the magnitude
   claim is an equivalence between two differently-written expressions, and the owner's item-13 ruling
   requires such an equivalence to carry a written argument. This one is short — the disjunction is a
   direct case split on the sign of the author's own expression rather than an algebraic rewrite of it —
   but it is owed and should be written rather than assumed.

   **What this item does *not* have to carry, corrected.** An earlier revision claimed an uncosted lane
   dimension here — that rule-sourced facts do not reach the `number` lane. That is wrong: `rule Baseline
   >= 1` on a bare `number` field discharges the divisor obligation through `CompositionalConstraint`,
   verified at HEAD. The rule route works on both lanes and this item carries no extra lane weight. What
   is true, and separate, is that a *fractional* threshold cannot currently be spelled on `number` at all
   — `0.05` is a type error and `5e-2` silently parses to zero through a language-wide exponent-literal
   defect filed in `docs/Working/bugs.md`. Neither is proof-engine work. See
   [The rule route reaches this lane](#the-rule-route-reaches-this-lane-spelling-a-fractional-threshold-does-not).

   **What this item does have to carry on that lane** is which comparison shapes exclude a non-finite
   value and which do not: `abs(x) >= k` and `x > 0` are false for `NaN` and so refuse it, while `x != 0`
   is true for `NaN` and admits it. See [The `number` lane](#the-number-lane).

2. **Let the divisor rule read the interval that is already calculated.** A field declared
   `min 0.01 max 100` already gets an interval containment obligation carrying `[0.01 .. 100]`; the
   divisor rule should consult it, since an interval wholly above or below zero settles the question
   outright. `PRE0083`'s recovery text already advises adding `min 1`, so the diagnostic and the proof
   rule are brought into agreement in the same pass. This is smaller work than the `abs()` narrowing
   and belongs beside it — see
   [What the author writes, and what each form establishes](#what-the-author-writes-and-what-each-form-establishes--a-worked-example),
   form (e).

   Together with item 4 this also delivers the derived-field case. When a field is defined as a product
   of factors whose own declared intervals hold them to one side of zero, the interval calculated for
   the product excludes zero, and the `nonzero` establishment obligation of item 3 is discharged from
   it with no further machinery. That is form (c) case 2 and pattern 4, and it is worth calling out
   during implementation because it is easy to build item 2 as a lookup on the divisor's own declared
   modifiers and miss the computed interval, which is where most of the value is.

3. **Make a field's declared constraints something the writes must prove.** A `nonzero` modifier must
   generate an establishment obligation at every write that produces the field's value — the expression
   of a derived field, and each `set` statement that assigns to a stored one — and a preservation duty
   on every other write. Until proved, the modifier cannot satisfy a later divisor obligation. This is
   the BUG-048 correction required under every policy. The same family covers a plain `set Divisor = 0`
   into a `nonzero` stored field, which currently draws no diagnostic and needs one line in the bug
   record beside BUG-048 and BUG-050.

   This is also what makes the materialize-once pattern work, and that is the larger part of its value.
   Once a `set` into a `nonzero maxplaces N` field is proved from the facts holding at the write, every
   division by that field is discharged from the declaration, so a reused divisor states its threshold
   once instead of once per division site. See
   [Authoring patterns](#authoring-patterns-and-when-each-one-fits), pattern 2. No extra
   build work is implied beyond this item — the establishment and preservation machinery is the same
   machinery either way. What changes is that this item should be scheduled as the way reuse is meant to
   be written, not only as a correction to an unsound acceptance.

   **Declared rules and `ensure`s are in scope for this item, not separate ones.** A `rule` consumed as a
   premise at a division site owes exactly what a modifier owes, and so does an `in State ensure`. The
   creation half already exists for rules — an unguarded rule is folded against the field defaults and the
   definition refused if they violate it (`PRE0164`). The preservation half does not: confirmed at HEAD, a
   file carrying `rule Rate >= 0.05` and a handler doing `set Rate = 0.0` compiles with no diagnostics
   while that same rule discharges a division elsewhere in the file. Whether the runtime catches it
   depends on the shape — a row action is refused at step 2h, but a computed field dividing by another
   computed field faults at 2g before any rule is consulted, so in that shape there is no backstop at all.
   This is Shane's item-16 ruling in the phase-1 walk applied to the divisor case, and it is what makes
   pattern 3 sound.

   It should be built as **one duty covering three declaration forms** — modifier, `rule`, and
   `in State ensure` — rather than as three mechanisms that drift. The owner's item-7 ruling names five
   constraint forms, so this is three of five. A state-scoped `ensure` narrows what has to be proved
   rather than removing it: established by every transition entering the state, preserved by every write
   occurring within it, and exempt elsewhere. See
   [form (h)](#the-eight-forms-and-what-each-one-contributes).

4. **Make interval transfers representation-aware.** Operation metadata must state how each numeric lane
   rounds, underflows, overflows, and admits special values. The proof engine then calculates intervals
   for stored results rather than mathematical-real results.

   **One structural note that decides how this is built.** The interval type today holds both endpoints
   as .NET `decimal` and uses `decimal.MinValue` and `decimal.MaxValue` as the markers for "no bound".
   So the `decimal` lane's maximum and the absence of any bound at all are currently the same number in
   the representation, and the comparison item 5 needs cannot be written until they are distinguishable.
   The same choice has a second consequence: a `number`-lane interval is held in `decimal` too, so the
   analysis cannot presently represent a `number` value between `7.9e28` and binary64's `1.8e308` at all.
   Neither is a bug in what exists — the interval type was built to compare computed values against
   declared bounds, and for that purpose the sentinel is fine. Both mean this item is a change to the
   interval representation and not only an addition of metadata beside it. There is also no lane-range
   metadata anywhere in the catalogs today — no maximum, no smallest storable magnitude, no overflow
   behavior — so this item creates catalog entries as well as consuming them, and it should be built
   catalog-first for the reason the whole architecture is catalog-first.

5. **Generate representation-range obligations at arithmetic operations.** The current
   `IntervalContainment` requirement covers fitting values into declared intervals. It does not fully
   describe an intermediate operation fitting the numeric representation. I recommend a distinct
   catalogued requirement such as `RepresentableResult`, attached by operation metadata and mapped to
   `NumericOverflow`.

   This is a new proof-obligation kind and build work, but it does not require new author syntax. It
   splits into three pieces that are worth naming separately, because they can be scheduled apart and
   because two of them are much smaller than they look.

   **5a. Give every numeric lane a declared range, and teach the interval to hold it.** The catalog
   entries and the interval-representation change described in item 4. Nothing else in item 5 can be
   built first.

   **5b. Stamp the obligation at every operation, not only where a declared bound exists.** This is the
   real gap and it is the whole of the difference between what exists and what is wanted. Confirmed at
   HEAD: interval containment is stamped where a field carries declared bounds — on a `set` target, on a
   computed field's own declaration — and a nameless intermediate inside an expression carries no
   obligation at all. The multiplication in the middle of `A * B * C` is an operation whose result the
   runtime computes and holds, and it needs an obligation of its own against the lane, independent of
   what any field declared. The discharge is the comparison, which is the cheap part; the collection is
   the work.

   **5c. Fill the transfer gaps that make safe definitions refuse.** Confirmed at HEAD: a chain of
   multiplications over fields with declared `min`/`max` propagates correctly and proves — the worked
   example's four-factor chain reports `[0.01 .. 300000000000]` and discharges. But `pow` has no interval
   transfer, so any compounding chain reports an unbounded interval and would be refused by 5b even when
   the author has declared every bound the domain has. `pow` with an integer exponent is the one that
   matters, because compounding is the shape that actually reaches the `decimal` lane's ceiling. Do this
   in the same pass as 5b or the first thing 5b refuses will be a correct definition.

   Two scheduling notes. All three sit behind the owner's item-15 deferral and none of them is a
   prerequisite for the divisor work — see
   [Overflow policy](#overflow-policy). And per Finding 2 of the overflow research, the discharge rate
   here will be high for a comparatively crude implementation, because any bound below the lane's limit
   is a complete proof; precision effort belongs on the divisor side, where it is a single point that has
   to be excluded.

   **5d. Exclude non-finite values, as a separate claim from fitting the range.** This one is
   `number`-only and it is not a variant of the others. Every comparison against `NaN` is false, so a
   `NaN` neither satisfies an upper bound nor violates one; asking whether it lies inside an interval has
   no answer. A containment obligation therefore cannot absorb it as a special value at the end of the
   range, and the claim "this result is not `NaN`" has to be carried as its own requirement, discharged
   by its own evidence. Infinity is the easier half and does fit the containment shape, because it orders
   correctly against every finite value and genuinely is the top of the lane — so the right split is two
   claims, *this result is finite* against the lane's maximum, and *this result is not `NaN`* on its own.

   **Report them under separate codes.** There is one code for this family today, `NumericOverflow`
   (PRE0078), and it describes magnitude; there is no code for a value being `NaN` and none for a value
   being infinite. A possible `NaN` reported as an overflow tells the author to add a bound, and a bound
   cannot fix it, because bounds are comparisons and comparisons cannot see a `NaN`. The recovery advice
   differs, so the codes should too.

   The finiteness half sits behind the item-15 deferral with the rest of item 5. The `NaN` half is only
   as deferred as the infinity case is, since every remaining path to a `NaN` runs through an infinity.
   See [The `number` lane](#the-number-lane).

6. **Defer backward reasoning through `round()`.** It is logically sound that
   `round(x, places) != 0` implies `x != 0`, because `round(0, places)` is always zero. Supporting that
   implication would nevertheless require a separate proof strategy from `abs()` guard narrowing. The
   compiler would need to reason backward through the rounding unit, midpoint mode, and a constant
   `places` argument.

   I do not recommend this for the first pass. Authors can get a clearer proof by storing the rounded
   value and guarding it directly, which also proves the value actually used as the divisor. Backward
   reasoning adds machinery mainly to support an indirect authoring form and must not be treated as
   part of the `abs()` work.

7. **Keep the implementation bounded.** Intervals and the `abs()` guard refinement are solver-free and
   have predictable cost. The broader solver-free survey places intervals, DBMs, and octagons in the
   bounded polynomial group suitable for per-keystroke compilation
   ([solver-free-static-analysis-techniques-survey.md](../../../research/architecture/compiler/solver-free-static-analysis-techniques-survey.md)).
   Convex polyhedra are the one technique that crosses out of that group, and they are declined for that
   reason — see
   [What Precept should adopt](#what-precept-should-adopt).

8. **State the divisor requirement per lane.** The divisor requirement is declared once in the operation
   catalog and reused across lanes; for the `number` lane it reads `NotEquals, 0m`, the same comparison
   against the same `decimal` constant that the `decimal` lane uses. On `decimal` that is exactly the
   property that makes division safe. On `number` it is not, because `NaN != 0` is true, and because
   division by zero on that lane produces an infinity rather than failing. The property that keeps a
   `number` division inside the spec's Totality principle is that the divisor is finite, non-zero and not
   `NaN`, and the requirement should say so.

   This is smaller than items 4 and 5 and it is not behind the item-15 deferral, because it is about what
   the obligation claims rather than about proving a range. It is also what makes the guard forms honest
   on that lane, so it belongs beside item 1 rather than after it. See
   [The `number` lane](#the-number-lane).

   **What this item no longer contains, and why.** An earlier revision folded a second half into this
   item: teaching `Modifiers.cs` that `nonzero`, `positive` and `nonnegative` establish not-`NaN` on the
   `number` lane, presented as mechanical catalog sync closing the distance between a ruling and the
   metadata. Two things were wrong with that. The attribution was wrong — the clause is derived and
   forced, not owner-ruled (see
   [Where the not-`NaN` clause comes from](#where-the-not-nan-clause-comes-from-stated-precisely)). And
   the work is not mechanical: `primitive-types.md` states twelve lines below the passage in question that
   *"The change to what a qualifier modifier establishes on the `number` lane is a modifier-semantics
   change and is designed before it ships; this section states the target."* Scheduling it here routed a
   modifier-semantics decision past the pre-design consultation gate.

   The observation that motivated it stands and is worth keeping as evidence for whoever designs it: the
   `Nonzero` entry in `Modifiers.cs` declares one satisfaction, `SelfValue ≠ 0` against a `decimal`
   constant, with no lane distinction and no finiteness clause, and there is no handling of `NaN` or
   infinity anywhere in the compiler or the runtime. So canon's target and the catalog are apart. Closing
   that distance is now listed under
   [Work that would require owner consultation](#work-that-would-require-owner-consultation-before-it-is-built)
   rather than here.

### Work I do not recommend now

- Do not add a `minabs`, `awayfromzero`, or similar modifier. `abs(x) >= k` already expresses the
  author intent, at a division site as a guard and over a field as a `rule`. The rule form covers the
  case the modifier was being considered for — an arbitrary magnitude floor that holds everywhere and is
  written once — so the gap that would have justified new field syntax is closed by a construct the
  language already has.
- Do not add a general underflow proof-obligation kind in the first implementation. Correct stored-value
  propagation plus existing downstream obligations is enough to prevent unsafe division.
- Do not infer product non-zeroness merely from factor non-zeroness. This is about the bare `nonzero`
  modifier, which carries no magnitude. A factor's declared *interval* is a different fact and does
  compose through ordinary interval multiplication — that is not this item, and it should be built.
- Do not build magnitude-floor composition for signed factors — `|A| >= a` and `|B| >= b` therefore
  `|A * B| >= a * b`, carried through a domain with a hole around zero — before guard-based magnitude
  refinement is working and measured. The inequality is valid and the rule is soundable under the three
  side conditions in
  [Composing magnitude bounds through a product](#composing-magnitude-bounds-through-a-product); it is
  deferred because it needs a non-convex domain that nothing else in the plan needs — the absolute-value
  octagon or the donut domain, the only two the research found, and the only build item on the whole list
  that would require one — and because the
  case it serves — a divisor that may take either sign and is known only to be some distance from zero —
  is uncommon next to the sign-definite case that plain intervals already cover. If the case turns out
  to be common in real definitions, this moves up; it is the one deferral in this list that is a
  scheduling judgment rather than a soundness one.
- Do not use `maxplaces` as a compiler hint where the domain does not constrain scale.
- Do not build widening with thresholds, variable packing, trace partitioning, or the
  arithmetic-geometric progression domain for the overflow work. These are the field's four named
  answers to a loose overflow bound, and all four exist to handle accumulation around a loop or to make
  a relational domain affordable across thousands of variables. Precept has no loops and a few dozen
  fields per definition. This is not a deferral to reconsider later — the techniques answer a question
  the language does not pose. The reasoning is set out in
  [Why the interval is the whole technique](#why-the-interval-is-the-whole-technique-and-why-the-fields-precision-remedies-do-not-transfer).
- Do not spend precision effort on overflow ahead of the divisor work. Any bound below the lane's limit
  is a complete proof, so a crude interval discharges an overflow obligation that a precise one is needed
  for on a divisor. The overflow research measured this directly: the same program is proved safe by
  bounds of `[−128, 128]`, `[−136, 136]` and `[−261, 261]`.
- Do not model modular or saturating arithmetic anywhere in the proof engine. Every lane that traps —
  `decimal` and all the business types — gives the easy case, where the state after an operation only has
  to describe the successful result. Nothing in Precept wraps today, and building for a wrap that does
  not exist buys imprecision for nothing.

### Work that would require owner consultation before it is built

No new syntax is required by the recommendation above. That holds for the overflow half as well as the
divisor half: bounding every field that takes part in a computation is `min` and `max`, which exist on
every numeric lane and are enforced today, and bounding a combination is a `rule`, which exists and takes
an arbitrary expression. Nothing an author has to write to keep a chain inside its lane is missing from
the language.

Three overflow-specific decisions are the owner's rather than this document's, and none of them is a
syntax question:

- **Lifting the item-15 deferral.** Overflow is an authorized exemption from prove-or-reject for the MVP,
  ruled 2026-07-26 with the consequence stated. Build item 5 describes where the model should end up;
  scheduling any of it is a decision to close the exemption, and that is the owner's to make. **Build
  item 4 is not in this bullet**, and an earlier revision wrongly put it here. Item 4 is the
  interval-representation change, and the divisor half depends on it directly: form (c) case 2 discharges
  `nonzero` through items 2 and 4, its soundness argument is representation-aware (the floor `0.005` sits
  far above `decimal`'s smallest magnitude of `1e-28`, so underflow cannot pull the product to zero), the
  underflow policy's first requirement is item 4's transfer, and final-judgment item 1 is stated in terms
  of representation-aware intervals. Routing item 4 through the overflow exemption would have blocked the
  divisor work behind a decision the divisor work does not depend on.
- **The `integer` representation.** Fixed-width 64-bit under prove-or-reject, or arbitrary precision,
  which removes the obligation and replaces it with a bound on time and memory that nothing in the
  surveyed field proves. Recorded as open in `primitive-types.md` § Open Questions and unchanged by this
  document.
- **The `number` lane's infinity behavior**, which rides with the same deferral and cannot be settled by
  changing the representation, since `number` is the approximate lane by construction. One thing about
  this deferral is worth restating before it comes back up. On `decimal` a deferred overflow means a
  runtime fault the author will meet in testing. On `number` nothing faults: the operation substitutes an
  infinity and the definition keeps computing on it, so the failure can present four operations later as
  a division by zero or as a `NaN` that no declared bound rejects. The exemption is the owner's to keep
  or lift either way; it is a larger exemption on that lane than on this one. See
  [The `number` lane](#the-number-lane).

Two further decisions belong here, and both were previously presented in this document as settled when
canon and the phase-1 walk record them as open:

- **What `nonzero`, `positive` and `nonnegative` establish on the `number` lane.** The *requirement* that
  a `number` divisor be finite and not `NaN` is forced and stays as build items 5d and 8. Changing what
  the three modifiers establish is a different thing, and `primitive-types.md` says of it in its own
  voice: *"The change to what a qualifier modifier establishes on the `number` lane is a
  modifier-semantics change and is designed before it ships; this section states the target."* It
  therefore goes through the pre-design consultation gate and then `/design`, not through catalog sync.
  The evidence is assembled in build item 8 and in
  [The `number` lane](#the-number-lane); the decision is not this document's to take.
- **What the strict divisor rule costs authors, on the underflow question.** `the-walk.md:377–392`
  (item 14) records the underflow half as unsettled, and records precisely which half: reasoning from the
  parts — non-zero times non-zero is non-zero — is unsound and cannot be the answer, but *what it costs an
  author to restate the constraint on the divisor* "was never worked out or put to him," and the item is
  a "Waiting on Shane" row "to be prepared with worked examples before it is asked." This document is that
  preparation. The eight forms, the four authoring patterns and the worked examples are the author-cost
  material the walk asked for. They are options with their costs attached, not a ruling. See
  [Underflow policy](#underflow-policy).

And one that *is* a language question, flagged rather than answered: **temporal arithmetic has no premise
to reason from.** `instant ± duration` can leave the representable calendar range, and the overflow
research found that both `java.time` and Noda Time check exactly that, eagerly, at every operation, with
Noda Time calling it overflow by name — analytically the same endpoint-against-constant comparison as a
type limit. The difference is that the range is declared by the library rather than imposed by the
representation, so an analyzer has to be told it. Precept's temporal point types reject `min`/`max`
today, so there is nothing for an obligation to read. Giving them bound vocabulary would be new language
surface and goes through the pre-design consultation, exactly as `primitive-types.md` already records.

If the team later wants any of the following, it changes the author-facing language or numeric contract
and must go through the pre-design consultation with Shane before `/design`:

- a declarative magnitude constraint in field syntax — noting that the `rule` form in pattern 3 already
  covers the case this would serve, so the reason to want it would have to be something other than
  expressiveness;
- a new exact-rational numeric type or replacement of the decimal lane;
- a rule that inexact decimal arithmetic must be explicitly rounded rather than using
  `System.Decimal`'s implicit arithmetic rounding;
- an author-selectable precision or rounding context;
- an underflow-observation expression or built-in that instruments an operation rather than inspecting
  only its stored result.

Those may be worthwhile, but none is necessary to make divisor proof sound under the current language.

## Final judgment

Precept should adopt the strict final-divisor rule and make it useful rather than weaken it.

The useful form is not “declare every input non-zero” and not “put `maxplaces` everywhere.” It is:

1. calculate possible **stored** values with representation-aware intervals;
2. reject any arithmetic operation that may exceed the representation — at every operation in a chain,
   not only where a field's declared bound gives the compiler something to compare against, and
   discharged by `max` (and, for factors that can go negative, `min`) declared on the operands because
   the domain has those limits;
3. let underflow introduce zero into the interval;
4. require a fact about the final divisor that excludes zero;
5. recognize `abs(Divisor) >= k` as that fact when the domain supplies a meaningful `k`;
6. recognize a field's own `nonzero maxplaces N` declaration as that fact once every write into the
   field has been proved to establish it, so that a divisor used in several places states its threshold
   once;
7. discharge that establishment from the divisor's own calculated interval where the divisor is derived
   from factors whose declared bounds hold them to one side of zero, so that constraining the inputs is
   an available answer and not only guarding the output;
8. recognize an unguarded `rule` over the divisor field as that fact, under the same establishment and
   preservation duty, so an arbitrary domain threshold — `0.05`, `0.25`, any number the domain names —
   can be stated once, literally, with its reason attached, without new field syntax;
9. require declared constraints to be established at each write and preserved at every other, rather
   than trusted;
10. state the divisor requirement in terms the lane makes true — on `number` that is finiteness and
    non-zeroness, not a disequality against zero, because `NaN != 0` is true and because division by
    zero on that lane produces an infinity rather than failing.

This resolves the tension with the earlier `maxplaces` recommendation. Intermediate precision remains
available. Approximation at domain boundaries remains explicit through `round()`. Divisor safety is
obtained from the value that will actually be divided by, after finite-representation effects, rather
than from declarations on values that existed earlier in the chain.

Nine of those ten rest on one technique, and the choice is deliberate: representation-aware interval
arithmetic, plus a narrowing rule that reads a magnitude claim the author wrote. No relational or
non-convex domain is needed for any of it. The tenth rests on no numeric domain at all — excluding a
`NaN` is not a question an interval can answer, since every comparison against one is false — and it is
the one requirement on the list that needs its own family. The techniques that were considered and
declined, and the requirements that decided each, are in
[The proof techniques the research surveyed](#the-proof-techniques-the-research-surveyed-and-which-ones-precept-should-use).

The recommendation is ready to inform phase-5 design, but it is not a locked decision and it is not a
build authorization. The immediate compiler work uses existing author syntax. Any later proposal that
changes field syntax or the numeric contract must first be discussed with Shane under the pre-design
consultation rule.

**What has to happen before any of this is built.** This document goes through `/design` and comes out as
a locked design document. That process supplies what a recommendation does not carry: the Philosophy
Alignment matrix, the Language Design Grounding and Architecture Grounding, decisions classified by
stakes, and the four legs — rationale, alternatives rejected, precedent, tradeoff accepted — on every
decision that ends up locked. Until that has happened, the eight build items describe implied work rather
than agreed work, and their stated orderings are untested claims.

**Two things the 2026-07-27 review named that this document cannot close, recorded so they are not lost.**
First, **nobody has estimated what this recommendation would cost authors** — how many real definitions it
would refuse — and no honest method exists, since counting sample files establishes nothing and a refused
definition is the only evidence such a rule generates. That is the half of the underflow question reserved
to the owner (see [Underflow policy](#underflow-policy)), and it is the largest open question against the
document. Second, **the central rule has survived attack but has never been independently re-derived.** A
reader briefed to reach the answer from the underlying evidence without following this document's
reasoning produced only disagreements; there is no record of it arriving at "prove the final stored
divisor" on its own and confirming it. Surviving attack is a weaker thing than being reproduced, and the
strongest claim here rests on the weaker of the two.
