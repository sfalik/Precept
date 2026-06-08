---
status: Active — horizon groundwork
authored: 2026-06-05
author: research (lifecycle-1)
topic: How comparable verification/constraint systems draw the line between reasoning over bounds/abstractions vs evaluating concrete values, and how they handle "a constraint's source must be bounded/decidable" plus default/initial-state validity
external-engagement: strong
---

# Interval-vs-Value Evaluation: Prior Art on Bounds-Reasoning vs Value-Folding for Constraint Discharge

> Where do verification and constraint systems put the line between reasoning over a sound **abstraction** of a field (`X ∈ [a,b]`) and **evaluating a concrete value** (`5 >= 10`), and do they adopt a "the source of a constraint must be bounded/decidable, else reject and ask for an annotation" discipline?

## Background

Precept's compiler is deciding between two postures for proving that field constraints hold (the design decision tracked in the sibling internal doc
[`bounds-only-constraint-enforcement-2026-06-05.md`](bounds-only-constraint-enforcement-2026-06-05.md), the intended consumer of this survey):

- **(a) Bounds-only.** Reason *only* over field bounds/intervals — a sound abstraction `X ∈ [a,b]` — narrowing from declared bounds plus relational bounds, **reject** a constraint whose *source* field is unbounded ("supply a bound"), and check defaults against the narrowed interval. Never evaluate a concrete value.
- **(b) Bounds + value-folding.** Additionally fold concrete default values (`5 >= 10 → false`) and compute derived/computed values.

The trade is **comprehensiveness** (does bounds-only catch every structurally-broken definition?) vs **author ergonomics** (does reject-unbounded-sources cascade onerously?) vs Precept's stated **"soundness over completeness / no opaque solver"** posture.

This survey grounds that decision in prior art. It builds forward from — and deliberately does not duplicate — the internal
[`proof-engine-interval-arithmetic-survey.md`](proof-engine-interval-arithmetic-survey.md) (mechanics of GNATprove, Frama-C EVA, Astrée, Liquid Haskell, Dafny),
[`relational-constraint-representation-survey.md`](relational-constraint-representation-survey.md), and the octagon mirror at `research/references/octagon-domain/`. Those establish *how* each engine propagates facts; this survey isolates the single **bounds-vs-values axis** and the **supply-a-bound / supply-a-precondition** discipline.

## Methodology

- **Research question.** For each comparable system: does it discharge a constraint by reasoning over a **bound/abstraction** or by **evaluating a concrete value**? And does it adopt a "constraint source must be bounded/decidable, else reject and demand an annotation" discipline? What does that reveal about bounds-only's comprehensiveness ceiling and its ergonomic (annotation-burden) cost?
- **Comparators** (chosen to span the abstraction↔evaluation spectrum, per the prompt): abstract interpretation / interval + octagon domain (Cousot & Cousot; Miné; Astrée); refinement types + SMT (Liquid Haskell, F\*, Dafny); CUE's value/constraint lattice; SPARK / Frama-C proof obligations; config-language defaults (Nickel, Dhall).
- **Search strategy.** Primary sources first: the Cousot POPL'77 abstract page, the Miné octagon paper (HOSC 2006 / arXiv:cs/0703084, mirrored locally), the CUE language specification + "Logic of CUE" concept docs + bounds tour, the Liquid Haskell course lecture, the SPARK User's Guide subprogram-contracts page, the Nickel user manual. WebSearch/WebFetch over di.ens.fr, cuelang.org, nikivazou.github.io, docs.adacore.com, nickel-lang.org. Internal triage first (`proof-engine-interval-arithmetic-survey.md`, octagon mirror).
- **Inclusion / exclusion.** Included: systems that decide a value-membership-in-a-set property statically or at eval time. Excluded: full deductive provers used interactively (Coq/Isabelle) — out of Precept's "no opaque solver, no interactive proof" scope; mentioned only where Dafny/F\* touch the SMT line.
- **Source-grade mix.** Primary for all load-bearing claims (official specs, the Cousot/Miné papers, AdaCore/Nickel/CUE official docs, the Vazou course). Two PDFs (the ESOP'21 refinement tutorial arXiv:2010.07763 and `refinement_types_for_haskell.pdf`) were **not text-extractable** at fetch time; their claims are carried by the HTML course lecture and the internal survey instead, and the gap is declared in Threats to Validity.
- **Time bounds.** All web fetches 2026-06-05. CUE / Nickel / SPARK docs are living documents; dates recorded per citation. Academic sources are stable by venue+year.

## Findings

### The spectrum: abstraction at one pole, evaluation at the other

The comparators sort cleanly onto a single axis — *what does the engine manipulate when it discharges a "value satisfies constraint" obligation?*

| System | Reasons over… | Constraint-source discipline | Default/initial validity | Closest to Precept option |
|---|---|---|---|---|
| **Abstract interpretation — interval domain** (Cousot; Miné; EVA; Astrée) | **Bound/abstraction** (`v ∈ [c1,c2]`); never the concrete value | Unbounded source ⇒ `⊤`/`[-∞,+∞]` ⇒ can't prove ⇒ **alarm** (the AI analogue of "reject") | Abstract state at entry over-approximates; alarm if any concrete value in the abstract set violates | **(a) bounds-only** |
| **Refinement types + SMT** (Liquid Haskell; F\*; Dafny) | **Logical constraint** over a *decidable* fragment; SMT decides the implication | Predicate must be in the decidable fragment; precondition must be **supplied** by author, caller must establish it | Subtyping check: the value/term's refinement must imply the target refinement | **(a)**, logic-flavored |
| **CUE** | **Lattice meet** — value *and* constraint are the same kind of object; bound = "infinite disjunction of concrete values" | A bound is just a less-specific point; values and bounds **blur** by construction | Default `d ⊑ v`: default must be an *instance of* (≤) the constraint — checked by **subsumption**, not evaluation | **(a)/(b) blurred** |
| **SPARK / GNATprove** | Both: lightweight **Interval** bound-propagation pass + SMT VC pass | Author **supplies** Pre/Post; precondition = "constraints on callers"; absent precondition defaults to `True` (imprecise) | Subtype range constraint (`range 0..100`) gives the **bound**; range check proved against it | **(a)** with optional value-level VCs |
| **Nickel** | **Concrete value** — contracts "check that a value satisfies some property at run-time" | None — a contract is an evaluated predicate; laziness defers but still evaluates | Default merged in, then the **evaluated** value is checked against the contract | **(b) value-evaluation** |

### 1. Abstract interpretation — the interval domain is the canonical bounds-only model

Cousot & Cousot's foundational move is exactly "reason over an abstraction, accept incompleteness for soundness" [Primary; POPL 1977 pp. 238–252; access date 2026-06-05; mirrored `research/references/interval-vs-value-evaluation/cousot-ai-spark-nickel.md`]:

> "Abstract interpretation of programs consists in using that denotation to describe computations in another universe of abstract objects ... so that the resulta of abstract execution give some informations on the actual computations"
> — Cousot & Cousot, POPL'77 abstract (di.ens.fr/~cousot/COUSOTpapers/POPL77.shtml)

The interval domain represents exactly the bound shape Precept's option (a) proposes, and the literature is explicit about its **comprehensiveness ceiling** — it is *non-relational* [Primary; Miné, HOSC 2006 / arXiv:cs/0703084; mirrored `research/references/octagon-domain/octagon-domain-mine-hosc2006-arxiv-cs0703084.txt` lines 95–110]:

> "the lattice of intervals ... and the lattice of polyhedra ... They represent, respectively, invariants of the form (v ∈ [c1 ; c2 ]) and (α1 v1 + · · · + αn vn ≤ c) ... Whereas the interval analysis is very efficient—linear memory and time cost—but not very precise, the polyhedron analysis is much more precise ... but has a huge memory cost"
> — Miné, *The Octagon Abstract Domain*, § II

> "the correctness of the program in Figure 1 depends on the discovery of invariants of the form (a ∈ [−m, m]) where m must not be treated as a constant, but as a variable ... Thus, this example is beyond the scope of interval analysis."
> — *ibid.*

This is the precise statement of what bounds-only **provably cannot catch**: any constraint whose validity depends on a *relationship between two fields* (`A + B == C`, `A ≤ B` where both vary). The interval/box domain abstracts each field independently; relational facts collapse to the product of the per-field intervals (the "best approximation in the interval (b) ... octagon (d)" figure caption, line 243). The octagon domain — already Precept's chosen relational representation per the LOCKED relational-rules design — sits precisely "between, in term of expressiveness and cost, the interval and the polyhedron domains" (line 154), recovering `±x ± y ≤ c` but **not** general linear relations like `A + B == C` with three or more variables or non-unit coefficients.

**The transfer the prompt asks about is exact.** "Reject-unbounded-source" *is* the AI analogue of "the abstract value is `⊤` / `[-∞,+∞]` → can't prove → alarm." In AI the engine silently emits a false alarm; Precept's proposal makes the same event a hard rejection with a directive ("supply a bound"). Astrée operationalizes the inverse — *supplying* external bound knowledge removes alarms: `proof-engine-interval-arithmetic-survey.md` § Astrée records that the user can "fine-tune precision ... by supplying external knowledge (e.g., loop bounds)" to reach "exactly zero false alarms." That is the ergonomic shape of "supply-a-bound" already in production use.

Soundness-over-completeness is the explicit posture: an alarm is a *possible* error (Frama-C EVA: "an alarm indicates a possible (not necessarily actual) runtime error", per the internal survey § EVA), and integers are even soundly over-approximated as reals — "Integer variables can be assumed, in the analysis, to be real in order to find approximate but safe invariants" (octagon mirror lines 38–40).

### 2. Refinement types + SMT — decidable-fragment discipline, supply-the-precondition

Liquid Haskell does **not** evaluate values and does **not** use interval abstraction; it reasons over a *logical constraint* and dispatches the implication to an SMT solver — but only because the predicate language is deliberately restricted to a **decidable fragment** [Primary; Vazou, lh-course Lecture 01; access date 2026-06-05; mirrored `…/liquid-haskell-decidable-fragment.md`]:

> "Liquid Haskell takes great care to ensure that type checking is decidable and efficient. To achieve this, it has to be careful to generate verification conditions that are decidable and efficiently checkable by the SMT solver."
> — Vazou, *Refinement Types* (lh-course Lecture 01)

The predicate grammar is "variables, booleans, numbers, boolean operators, equality, linear arithmetic, [and] uninterp. functions" — i.e. the quantifier-free logic of linear arithmetic + uninterpreted functions (QF-UFLIA). Subtyping reduces to an SMT *validity* check, not value evaluation:

> "The subtyping rule for base types ... is the following: {v:b | p₁} ⊆ {v:b | p₂}, if p₁ 'implies' p₂ for all values v of type b. To check implications, we use the below two implication rules that are based on the SMT solver."
> — *ibid.*

The **supply-a-precondition** discipline is built in: division's divisor is typed `{v:Int | v /= 0}`, so "the caller must establish the non-zero precondition or type-checking fails" (*ibid.*). This is the same shape as "reject the constraint unless its source carries the fact that makes it provable." The internal survey records the parallel for Dafny — "the accuracy of verification depends entirely on the quality of the annotations" and absent annotations Dafny "cannot verify properties that depend on what the loop computes" (`proof-engine-interval-arithmetic-survey.md` § Dafny).

**Bearing on Precept.** Refinement types confirm two things: (i) the "supply the constraint that makes it provable" posture is a mainstream, well-precedented design, not an idiosyncrasy; (ii) the price of *predictable automatic* checking is a **decidable-fragment restriction** — Precept's bounds-only is a *much smaller* decidable fragment than QF-UFLIA (bounds + octagon relations vs full linear arithmetic + UF), so it trades expressiveness for the "no opaque solver" guarantee, where Liquid Haskell accepts an SMT solver in the loop.

### 3. CUE — the system where values *are* bounds (the owner's observation, confirmed)

The owner's observation that "values become bounds" is **literally CUE's design**. CUE unifies values, types, and constraints into one lattice [Primary; CUE spec + "Logic of CUE" concept doc; access date 2026-06-05; mirrored `…/cue-spec-lattice-bounds-defaults.md`]:

> "CUE's values also include what we normally think of as types ... It does not distinguish between types and values: only the relationship of values in the lattice is important."
> — CUE Language Specification § Values

> "constraints, a category of values that falls between the traditional concepts of value and type"
> — *The Logic of CUE* (lattice diagram description)

A **bound** is explicitly an infinite disjunction of concrete values — i.e. a value at a less-specific lattice point:

> "A _bound_ ... defines a logically infinite disjunction of concrete values represented as a single comparison. For example, `>= 2` represents the infinite disjunction `2|3|4|5|6|7|…`."
> — CUE Language Specification § Bounds

Crucially for the **default-validity** question, CUE checks a default by **subsumption (lattice ≤), not evaluation**:

> "Any value `v` _may_ be associated with a default value `d`, where `d` must be in instance of `v` (`d ⊑ v`)."
> — CUE Language Specification § Default values

> "Conceptually, CUE keeps two parallel values, one for all possible values and one for the default, which must be an instance of the former."
> — *The Logic of CUE*

A concrete value unified with a bound it violates yields **bottom** (`0 & >10 // failure`, from the bounds tour) — and "Any evaluation error in CUE results in a bottom value ... Bottom is an instance of every other value" (spec § Bottom). So CUE *does* blur value and bound: checking "default 5 satisfies `>= 10`" is the unification `5 & >=10`, a **meet in the lattice** that lands on bottom — formally a bound-subsumption check, not arithmetic value-folding, even though the operand `5` is concrete.

**Bearing on Precept.** CUE is the strongest precedent that option (a) and option (b) **need not be distinct**: when a concrete default is just the most-specific lattice point and a bound is a less-specific one, "check default 5 against `>= 10`" and "check `X ∈ [10,∞)` admits 5" are *the same operation*. This is the key insight for Precept's decision — value-folding a default against an interval need not be a second mechanism; it can be the interval-membership test specialized to a singleton interval `[5,5]`. CUE shows a shipping language built entirely on that identification.

### 4. SPARK / Frama-C — author supplies the contract; bounds come from the type

SPARK is the clearest "the tool asks for the constraint that makes it provable" precedent. A precondition is the caller's obligation, and GNATprove's modular proof *demands* the author supply it [Primary; SPARK User's Guide § Subprogram Contracts; access date 2026-06-05; mirrored `…/cousot-ai-spark-nickel.md`]:

> "The precondition of a subprogram specifies constraints on callers of the subprogram."
> "When a caller is analyzed with GNATprove, it checks that the precondition of the called subprogram holds at the point of call."
> "It may be necessary to add a precondition to the subprogram for analyzing its callers."
> "The default precondition of `True` used by GNATprove when no explicit one is given may not be precise enough"
> — SPARK User's Guide, Subprogram Contracts

For **default/initial-value validity and bounds**, SPARK is *bound-based*: the internal survey records that "Ada's type system carries explicit range constraints (e.g., `type Score is range 0 .. 100`), which GNATprove uses as initial interval bounds," and the dedicated **Interval** outcome bucket proves "range checks ... by a simple static analysis of bounds ... based on type bounds of sub-expressions" *before* invoking SMT (`proof-engine-interval-arithmetic-survey.md` § SPARK). This is option (a) shipping in safety-critical production: a cheap bound-propagation pass first, SMT only for what bounds can't settle — and the bound's *source* is the declared subtype range. This maps directly onto Precept "declared field bounds → narrowed interval → check." Frama-C EVA is the pure-AI analogue: it reasons over interval+congruence abstract values and emits an alarm (= the ACSL assertion that "was not provable") when the abstract divisor set includes 0 (internal survey § EVA).

This precedent matters for Precept's §0.7 framing ("names what would make the operation provably safe"): GNATprove's unproved-check messages and the supplied-precondition workflow are exactly the "name the missing fact" posture.

### 5. Config-language defaults — Nickel evaluates the value (the option-(b) pole)

Nickel is the clean counter-pole: contracts are *evaluated against concrete values*, not abstract bounds [Primary; Nickel user manual § Contracts; access date 2026-06-05; mirrored `…/cousot-ai-spark-nickel.md`]:

> "To a first approximation, contracts are assertions. They check that a value satisfies some property at run-time."
> "When evaluating `x`, the following steps are performed: 1. Evaluate `1 + 1`. 2. Check that the result is a number."
> — Nickel user manual, Contracts

A default is merged in (via merge priority) and then the **evaluated** value is checked against the contract — pure value-evaluation, laziness deferring *when* but not *whether* evaluation happens. The tradeoff Nickel accepts is the inverse of Precept's: it catches a broken default only on the path that evaluates it (no whole-space guarantee), and an unbounded *source* is simply never checked until a concrete value flows through. This is exactly the comprehensiveness gap option (a) is designed to close.

(Dhall: its defaults are typed records merged with `//`; validity is by **typing**, not arithmetic evaluation — same bound/typing-side as CUE. Not separately excerpt-grounded here; flagged as a minor gap in Threats to Validity.)

## Threats to Validity

- **Two PDF primary sources unfetchable.** The ESOP'21 refinement tutorial (arXiv:2010.07763) and `refinement_types_for_haskell.pdf` returned binary/non-extractable content at fetch time. The decidable-fragment and supply-precondition claims are instead carried by the **Vazou lh-course HTML lecture** (Primary, authoritative author) plus the internal `proof-engine-interval-arithmetic-survey.md` (which cites the same tutorial). The load-bearing claims are corroborated across two independent surfaces, but the exact ESOP'21 phrasing ("QF-UFLIA") is from the course lecture's grammar enumeration, not the tutorial PDF.
- **F\* not directly fetched.** F\* is grouped with Liquid Haskell/Dafny on the SMT-decidable-fragment claim by analogy and the internal survey; no fresh F\*-specific excerpt was captured. Low risk (F\*'s SMT-fragment discipline is well established) but flagged.
- **Dhall default-validity claim under-grounded.** The Dhall "validity by typing not evaluation" claim rests on training-data knowledge (Tertiary), not a fresh excerpt; treated as a minor supporting point, not load-bearing. CUE + Nickel carry the defaults-axis conclusion.
- **HAL/arXiv access gating.** The Miné HAL mirror was access-gated at fetch time; the load-bearing octagon/interval quotes come from the **pre-existing local arXiv mirror** (`research/references/octagon-domain/...`), which is the same paper (HOSC 2006 / arXiv:cs/0703084). No substitution risk.
- **Living-doc recency.** CUE, Nickel, SPARK docs evolve; all fetched 2026-06-05. The lattice/bounds/defaults and precondition semantics quoted are core, stable features unlikely to churn, but the access dates bound the claim.
- **Domain mismatch.** All comparators analyze *programs/configs* (sequential code, config trees); Precept governs *entity field constraints over a lifecycle*. The interval/bound mechanics transfer cleanly; the *control-flow* aspects (loops, widening) are largely irrelevant to Precept's flat-field setting — which, if anything, makes bounds-only **more** complete for Precept than for a general program analyzer (no loop-induced precision loss).

## Implications for Precept

1. **Option (a) is the interval-domain / SPARK-Interval-pass model, and it is mainstream and sound.** Reasoning over `X ∈ [a,b]` and rejecting an unbounded source is *exactly* abstract interpretation's interval domain plus SPARK's "declared range = initial bound" plus the alarm-on-⊤ behavior reframed as a hard rejection. Precept would be in the company of Astrée, Frama-C, and GNATprove — the NIST-recognized sound-analysis tools.
2. **The comprehensiveness ceiling is precisely characterized and already addressed.** Bounds-only (non-relational) provably cannot catch constraints whose validity is *relational* (`A + B == C`, cross-field `A ≤ B`). Miné names this exactly. Precept's already-LOCKED octagon relational layer recovers the `±x ± y ≤ c` slice — but **not** general 3+-variable / non-unit-coefficient linear relations (the polyhedron tier Precept has deliberately not adopted). So the honest comprehensiveness statement is: *bounds + octagon catches per-field and two-variable-unit-coefficient relational breakage; it cannot catch general linear-relational breakage* — and that residue is a known, named, bounded gap, not an open-ended one.
3. **"Supply a bound" is well-precedented ergonomically — and the burden is real but bounded.** SPARK's "add a precondition," Astrée's "supply external bound knowledge," Liquid Haskell's "type the divisor non-zero" are the same discipline in production. The known ergonomic cost (the annotation cascade) is exactly what SPARK users report as the cost of modular proof. Precept's mitigation, unavailable to general program analyzers, is that **most fields already carry a declared type whose range is the bound** (the Ada-subtype precedent) — so "unbounded source" should be the exception (genuinely range-free fields), not the rule.
4. **Value-folding need not be a separate mechanism (CUE).** The strongest design signal: CUE proves a concrete default `5` against a bound `>= 10` as the *lattice meet* `5 & >=10 → ⊥` — i.e. interval-membership specialized to the singleton `[5,5]`. Precept can fold a concrete default by treating it as a degenerate interval and reusing the *same* narrowing-and-check machinery, rather than adding a parallel value-evaluation path. This collapses the (a)-vs-(b) dichotomy for the *default-validity* sub-question: option (b)'s "fold `5 >= 10`" is option (a)'s interval check at a point.
5. **The "no opaque solver" posture is what separates Precept from Liquid Haskell/Dafny, and bounds-only is consistent with it.** Refinement types buy expressiveness by accepting an SMT solver; Precept's bounds+octagon is a *decidable, inspectable, solver-free* fragment. The prior art says this is a deliberate, defensible point on the expressiveness/inspectability curve — strictly weaker than QF-UFLIA, strictly stronger and more predictable than "run a solver and hope it terminates."

## Conclusions

**Precept's bounds-only hypothesis most closely matches the interval abstract domain (Cousot/Miné) as operationalized by SPARK's lightweight Interval pass, with the octagon layer for two-variable relations** — and CUE supplies the precedent that concrete-default checking is the *same* operation as interval-membership, not a separate value-folding path.

- **Rationale.** Across the abstraction↔evaluation spectrum, every *sound, comprehensive, whole-space* checker (AI tools, SPARK, refinement types) reasons over **bounds/constraints**, not evaluated values; only the *value-evaluation* config tools (Nickel) check concrete values, and they explicitly give up the whole-space guarantee. Precept's prevention-not-detection commitment forces the bounds side. Reject-unbounded-source is the principled hard-rejection form of AI's alarm-on-⊤.
- **Alternatives considered and rejected.** (i) Pure value-folding (Nickel model) — rejected: catches a broken default only on evaluated paths, no whole-space guarantee, leaves unbounded sources unchecked, contradicts prevention-not-detection. (ii) Full SMT/refinement (Liquid Haskell/Dafny) — rejected: buys expressiveness at the cost of an opaque solver in the loop, against Precept's "no opaque solver / predictable" posture. (iii) Polyhedron domain (general linear relations) — rejected upstream by the LOCKED relational design on cost/inspectability grounds; octagon is the chosen midpoint.
- **Precedent.** Interval domain `v ∈ [c1,c2]` and its non-relational limit (Miné); Cousot soundness-via-approximation (POPL'77); SPARK Interval bound-pass + subtype-range-as-bound + supply-the-precondition; Astrée supply-external-bounds-to-kill-alarms; Liquid Haskell decidable-fragment + supply-the-precondition; CUE default-as-instance subsumption check (`d ⊑ v`); Nickel as the rejected value-evaluation pole.
- **Tradeoff accepted.** Bounds(+octagon)-only is *incomplete*: it provably cannot reject definitions broken by general linear-relational facts (3+ vars / non-unit coefficients). Precept accepts a known, named, bounded comprehensiveness gap (and the supply-a-bound annotation burden) in exchange for soundness, inspectability, predictable termination, and no opaque solver — the same trade SPARK's Interval pass and Astrée make in production safety-critical use.

## What would change this conclusion

- If a comparable **sound, whole-space, solver-free** checker were found that discharges constraints by *value-evaluation* rather than bound-reasoning, the claim "comprehensive checkers reason over bounds, not values" would be wrong.
- If the **supply-a-bound cascade** in Precept's flat-field setting turned out to demand annotations on a *majority* of fields (rather than the genuinely range-free minority), the "burden is bounded because most fields carry a typed range" mitigation (Implication 3) would fail, and the ergonomic case for option (b) or a hybrid would strengthen.
- If Precept's real-world constraint corpus turns out to be **dominated by general linear-relational constraints** (3+ variables, non-unit coefficients) rather than per-field and two-variable-unit-coefficient ones, the "named bounded gap" (Conclusion tradeoff) would be a *central* gap, not a residue — pushing toward the polyhedron tier the relational design rejected.

## Open Questions

- **F\* and Dhall** were not directly excerpt-grounded; a follow-up could confirm F\*'s SMT-fragment discipline and Dhall's by-typing default validity with primary excerpts.
- **Quantifying the annotation cascade.** Prior art establishes the *shape* of the supply-a-bound burden but not its *magnitude* in a flat-field, declared-type setting. An empirical pass over Precept's sample corpus (how many fields are genuinely range-free?) would measure the real ergonomic cost — the decisive input for the sibling design doc.
- **Singleton-interval folding as the unifying mechanism.** CUE shows default-check = interval-membership-at-a-point. Whether Precept's narrowing engine can cleanly represent a concrete default as a degenerate `[d,d]` interval and reuse the same check is an implementation-feasibility question for the sibling doc.

## Sources

- **Cousot, P. & Cousot, R.**, "Abstract Interpretation: A Unified Lattice Model for Static Analysis of Programs by Construction or Approximation of Fixpoints." *POPL 1977*, pp. 238–252. Primary. Accessed 2026-06-05 via https://www.di.ens.fr/~cousot/COUSOTpapers/POPL77.shtml. Mirrored: `research/references/interval-vs-value-evaluation/cousot-ai-spark-nickel.md`.
- **Miné, A.**, "The Octagon Abstract Domain." *Higher-Order and Symbolic Computation*, 2006; arXiv:cs/0703084. Primary. Mirrored (full text): `research/references/octagon-domain/octagon-domain-mine-hosc2006-arxiv-cs0703084.txt`; quote index in `…/interval-vs-value-evaluation/cousot-ai-spark-nickel.md`.
- **CUE Language Specification** — § Values, § Unification, § Bounds, § Default values, § Bottom, § Top. Primary. https://cuelang.org/docs/reference/spec/ (accessed 2026-06-05). Mirrored: `…/interval-vs-value-evaluation/cue-spec-lattice-bounds-defaults.md`.
- **"The Logic of CUE"** (CUE concept doc) + **CUE bounds tour**. Primary. https://cuelang.org/docs/concept/the-logic-of-cue/ , https://cuelang.org/docs/tour/types/bounds/ (accessed 2026-06-05). Mirrored as above.
- **Vazou, N.**, "Refinement Types" (lh-course Lecture 01). Primary (authoritative author course). https://nikivazou.github.io/lh-course/Lecture_01_RefinementTypes.html (accessed 2026-06-05). Mirrored: `…/interval-vs-value-evaluation/liquid-haskell-decidable-fragment.md`.
- **Jhala, R. & Vazou, N.**, "Refinement Types: A Tutorial." ESOP 2021; arXiv:2010.07763. Primary by identifier; PDF body **not text-extractable at fetch** (see Threats to Validity) — claims carried by the course lecture + internal survey.
- **SPARK User's Guide — Subprogram Contracts.** AdaCore. Primary. https://docs.adacore.com/spark2014-docs/html/ug/en/source/subprogram_contracts.html (accessed 2026-06-05). Mirrored: `…/interval-vs-value-evaluation/cousot-ai-spark-nickel.md`.
- **Nickel user manual — Contracts.** Primary. https://nickel-lang.org/user-manual/contracts (accessed 2026-06-05). Mirrored as above.
- **Internal (Cited):** `research/architecture/compiler/proof-engine-interval-arithmetic-survey.md` (GNATprove/EVA/Astrée/Liquid Haskell/Dafny mechanics, SPARK Interval bucket, subtype-range-as-bound); `research/architecture/compiler/relational-constraint-representation-survey.md`; sibling design `research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md` (intended consumer).
