---
status: Cited
authored: 2026-06-02
author: research (lifecycle-1)
topic: How constraint/verification systems represent and discharge a relational constraint between two fields (x >= y, x <= ceiling), and whether a bounded, non-fixpoint, legible approach exists that fits Precept's §0.4 / §0.6-item-3
external-engagement: strong
---

# Relational constraint representation and discharge: A-vs-B and the bounded-closure question

> When a constraint relates two fields (`x >= y`, `x <= ceiling`), how do real verification systems *represent* that relation and *discharge* it — and is there a **bounded, single-pass, legible** discharge that fits Precept's §0.4 ("bounded relational closure, no widening") and §0.6 item 3 (no opaque solver; witness = legible rule list)?

## Background

Grounds **Decision 2** in [`docs/Working/relational-rules-and-bounds-design-2026-06-02.md`](../../../docs/Working/relational-rules-and-bounds-design-2026-06-02.md): how a relational/field-reference bound is represented as a proof obligation — a *data-model variant* (the relation attached to one field's bound) vs a *relational-path discharge* (the relation kept as a fact, discharged by a relational strategy). The design frames these as two candidate shapes (A vs B) and defers the pick to the build slice's enumerate/probe step. This survey supplies the external grounding for that choice.

This is a **build-forward synthesis, not a from-scratch survey.** The comparator corpus for *interval/range analysis and proof discharge* is already covered in [`proof-engine-interval-arithmetic-survey.md`](proof-engine-interval-arithmetic-survey.md) (SPARK/GNATprove, Frama-C EVA, Astrée, Liquid Haskell, Dafny, CBMC, Infer). That survey is cited here for those systems' representation/discharge behavior rather than re-run. The new work is (a) the A-vs-B classification across the corpus, (b) three supplemental gaps the prior survey did not reach — octagon closure as a *bounded* operation, difference logic as relation-as-fact, and CUE's lattice fixpoint — and (c) the synthesis for Decision 2.

## Methodology

- **Research question.** Stated above. Two sub-axes: (i) *representation* — is the relation attached to **one variable's bound/refinement** (shape A, e.g. Liquid Haskell `{v:Int | v >= y}`) or kept as a **fact in a shared relational store/domain** (shape B, e.g. Astrée's octagon, Dafny's VC assumption context)? (ii) *discharge* — bounded single-pass closure, or iterate-to-fixpoint?
- **Per-system sub-questions** (cited verbatim where load-bearing): (1) representation A-ish or B-ish? (2) bounded closure vs fixpoint? (3) where on the non-relational-intervals → weakly-relational-octagons → fully-relational-polyhedra precision/cost spectrum? (4) witness legibility — can it explain *why* a value narrowed in human/rule terms (§0.6 item 3)?
- **Search strategy.** (1) Re-read the in-tree `proof-engine-interval-arithmetic-survey.md` for SPARK / EVA / Astrée / Liquid Haskell / Dafny / CBMC / Infer; cite, do not re-survey. (2) Primary-source fetch of Miné, *The Octagon Abstract Domain* (arXiv cs/0703084, the HOSC 2006 author copy; PDF extracted to text locally) for the closure-complexity question. (3) Difference logic via a primary DBM reference (Wikipedia *Difference bound matrix*, which quotes the Floyd–Warshall canonical-form result) plus a web-search confirmation of the Cotton–Maler DPLL(T) line. (4) CUE lattice/unification/cycle behavior from the CUE language spec and the CUE tour (Secondary, as the task allows).
- **Inclusion / exclusion.** Included: systems that either narrow a field's domain from a relation, or keep relations as discharged facts. Excluded: the broad runtime-validation comparators (Zod/Pydantic/FluentValidation) except as the "no propagation" reference point — those are already excerpted in `research/language/references/constraint-composition.md` and are cited, not re-surveyed.
- **Source-grade mix.** Primary: Miné octagon paper, CUE language spec, the in-tree survey's Primary citations (SPARK UG, Frama-C EVA manual, Liquid Haskell ESOP tutorial). Secondary: CUE tour pages, the difference-logic DBM reference, Astrée/AbsInt vendor docs (via the in-tree survey). Tertiary: the Cotton–Maler DPLL(T) detail reached only via web-search summary (flagged in Threats).
- **Time bounds.** External fetches 2026-06-02.

## Findings

### A-vs-B classification across the corpus

The axis: does the system represent `x >= y` by **changing `x`'s own type/bound/interval** (A — relation absorbed into one variable's domain), or by **keeping `x - y >= 0` as a standalone fact in a relational store** that a relational strategy later consults (B — relation lives between the variables)?

| System | Representation | Bounded vs fixpoint | Precision tier | Witness legibility | Source |
|---|---|---|---|---|---|
| **Non-relational intervals** (Frama-C EVA core, InferBO, GNATprove Interval pass) | **A** — each variable carries an interval `[lo,hi]`; a relation `x>=y` can only be *used* to narrow `x` via the branch/guard, not stored *as* a relation | Fixpoint with **widening** for loops; single-pass for straight-line code | non-relational (weakest) | per-variable value set at each program point; relations between variables are **lost** | proof-engine-interval-arithmetic-survey.md (EVA §, InferBO §, GNATprove §) |
| **Octagons** (Astrée's weakly-relational domain; Miné) | **B** — the relation `±x ± y ≤ c` is a first-class element of a shared DBM (difference-bound matrix) | **Bounded closure** (O(n³) Floyd–Warshall strong closure); widening is a *separate* operator used only for loops | weakly-relational (middle) | DBM = a set of `±x ± y ≤ c` constraints; the closed form *is* a rule list | Miné (octagon paper); Astrée § |
| **Difference logic / DBM** (Pratt; Cotton–Maler) | **B** — `x − y ≤ c` is the stored fact; conjunction satisfiable iff **no negative cycle** | **Bounded** — shortest-path (Floyd–Warshall / Bellman–Ford), no search loop | weakly-relational | the constraint graph; the canonical DBM is the tightest derivable constraint set | DBM reference; difference-logic search |
| **Polyhedra** (Astrée's relational domain) | **B** — arbitrary linear `Σ aᵢxᵢ ≤ c` | Fixpoint + widening; exponential-class operators | fully-relational (strongest, costliest) | a system of linear inequalities | Miné (cost comparison); Astrée § |
| **Liquid Haskell** (refinement types) | **A** — the relation is *in the type*: `{v:Int | v >= y}` | constraint-based (Horn/Fixpoint over SMT), **no interval fixpoint** for straight-line code | exact (SMT linear arithmetic) | "inferred type vs required type" diff — the refinement predicate is human-readable | proof-engine-interval-arithmetic-survey.md (Liquid Haskell §) |
| **Dafny / SPARK-SMT / CBMC** (VC + solver) | **B** — the relation is an *assumption in the VC context*; `requires x >= y` becomes a hypothesis the solver carries | no fixpoint for loop-free code (WP/SSA → single formula); loops need user invariants | exact (SMT/SAT, bit-precise for CBMC) | counterexample trace / failing-clause location; **not** a "this narrowed because of rule R" story | proof-engine-interval-arithmetic-survey.md (Dafny §, SPARK §, CBMC §) |
| **CUE** (lattice unification) | **A** — a field's value *is* the unification (meet) of all constraints on it; `x: >=0 & <=10` makes `x`'s domain `[0,10]` | **fixpoint** over the constraint graph (with cycle-breaking heuristics) | exact over the value lattice | a unification trace, not a rule list | CUE spec; constraint-composition.md |

The field is **split on representation but converges on a key sub-result**: the *weakly-relational* systems (octagons, difference logic) are exactly the ones that are **both relation-as-fact (B) and bounded (no fixpoint for the relational step itself)**. The non-relational interval systems are A-ish but lose the relation; the fully-relational polyhedra and the SMT/lattice systems pay a fixpoint or solver cost.

### Gap 1 — Octagon closure IS a bounded operation, not an iterate-to-fixpoint

This is the crux for §0.4. Miné's octagon domain represents exactly the two-field relations Precept's Decision 2 is about, and its core operation — closure — is a **bounded O(n³) graph algorithm over a fixed variable set**, not a fixpoint iteration.

The constraint form [Primary; Miné, *The Octagon Abstract Domain*, arXiv cs/0703084, access 2026-06-02]:

> "allows us to represent invariants of the form (±x ± y ≤ c), where x and y are program variables and c is a real constant." (Abstract)

The closure is a Floyd–Warshall shortest-path computation with cubic cost — and crucially, the **widening operator is named separately** from closure, used only for loop fixpoints:

> "We focus on giving an efficient representation based on Difference-Bound Matrices—O(n²) memory cost, where n is the number of variables—and graph-based algorithms for all common abstract operators—O(n³) time cost. This includes a normal form algorithm to test equivalence of representation and **a widening operator to compute least fixpoint approximations**." (Abstract — note closure and widening are distinct; widening is the fixpoint piece, used for loops)

The closure algorithm itself:

> "Theorem 3.1 leads to a closure algorithm inspired by the Floyd–Warshall shortest-path algorithm. This algorithm is described in Figure 7 and runs in O(N³) time." (§ III, "Closure")

The *strong* closure (the octagon-specific normal form that also propagates the `±x ± y` combinations, not just differences) is likewise bounded:

> "From this definition, we derive the strong closure algorithm m⁺ ↦ (m⁺)• described in Figure 8. The algorithm looks a bit like the closure algorithm of Figure 7 and **also runs in O(N³) time**." (§ IV.C, "Strong Closure")

And closure is a *normal form* — there is a unique closed DBM per non-empty octagon:

> "m⁺ = (m⁺)• ⟺ m⁺ is strongly closed. … (m⁺)• = inf_P {n⁺ | D⁺(n⁺) = D⁺(m⁺)} (Normal Form)." (Theorem 4)

The precision/cost positioning — octagons sit **strictly between** non-relational intervals and fully-relational polyhedra, which is exactly the three-tier spectrum the task asked about:

> "The invariants computed are always more precise than the ones computed in [1], which gives itself always better results than the widespread intervals domain [5]; but they are less precise than the costly polyhedron analysis [6]." (§ VI.C, "Precision and Cost")

> "This domain allows us to manipulate invariants of the form (±x ± y ≤ c) with a O(n²) worst case memory cost per abstract state and a O(n³) worst case time cost per abstract operation … beyond the scope of interval analysis, for a much smaller cost than polyhedron analysis." (§ VII, Conclusion)

**Reading for Precept.** "Bounded relational closure" in §0.4 is not a Precept neologism — it maps directly onto an established, named domain operation: **octagon strong closure**, a O(n³) Floyd–Warshall pass over a fixed variable set that derives the tightest implied two-field bounds. The piece §0.4 forbids (widening) is the *separate* operator Miné uses only for loops — and Precept has no loops (§0.4 items 1/3). So the §0.4 envelope is "octagon closure without the widening step," which is coherent: closure is total and terminating on its own; widening is only needed to force convergence of a loop fixpoint that Precept never forms.

### Gap 2 — Difference logic: relation-as-fact discharged by shortest-path, no solver loop

Difference logic is the pure relation-as-fact (B) representation: the relation `x − y ≤ c` is the stored object, and satisfiability is decided by a graph algorithm, not a search loop.

[Secondary; *Difference bound matrix*, Wikipedia, access 2026-06-02]:

> "It suffices to apply the Floyd–Warshall algorithm to the graph and associates to each entry (a, b) the shortest path from a to b in the graph. If this algorithm detects a cycle of negative length, this means that the constraints are not satisfiable, and thus that the zone is empty."

The canonical (closed) DBM is the tightest derivable constraint set — the same normal-form idea as octagons:

> "the shortest path from any edge a to any edge b is the arrow (a, b). This graph is called the potential graph of the DBM."

The decision-procedure framing (a conjunction of `x − y ≤ c` is satisfiable iff the constraint graph has no negative cycle; propagation is incremental shortest-path / Bellman–Ford) is the basis of difference-logic theory solvers inside DPLL(T) [Tertiary — reached via web-search summary of Cotton & Maler, *Fast and Flexible Difference Constraint Propagation for DPLL(T)*, SAT 2006; flagged in Threats].

**Reading for Precept.** Difference logic confirms that a relation-as-fact representation (B) can be discharged by a *bounded* graph operation. It is the degenerate case of octagons (differences only, `x − y ≤ c`, no `+`). It establishes the precedent that "keep the relation as a fact, discharge by shortest-path closure" is a legible, terminating, solver-free strategy — which is what Decision 2's option B is, restricted to the depth Precept allows.

### Gap 3 — CUE: lattice unification IS a fixpoint (the thing §0.4 rejects)

CUE is the comparator the design already cites as the "narrow values by propagating constraints" model. Confirmed: CUE narrows by **unification = greatest-lower-bound on a complete lattice**, and evaluation is a **fixpoint over the constraint graph** with explicit cycle-breaking — precisely the strategy §0.4 forbids.

[Primary; *The CUE Language Specification*, cuelang.org/docs/references/spec/, access 2026-06-02]:

> "All possible values are ordered in a lattice, a partial order where every two elements have a single greatest lower bound."

> "The unification of values a and b is defined as the greatest lower bound of a and b." (binary operator `a & b`, commutative/associative/idempotent)

CUE's narrowing result-shape is exactly the `⊓` (meet) in the design's R-NARROW rules: `x: >=0 & <=10` unifies to the interval `[0,10]`. But the *evaluation strategy* is a fixpoint, surfaced in how CUE handles reference cycles [Secondary; CUE tour, *Reference Cycles*, cuelang.org/docs/tour/references/cycle/, access 2026-06-02]:

> "Because all values are final in CUE, a field with a concrete value (e.g. `200`) can only be valid if it is that value. If CUE sees this concrete value being unified with some other expression then the evaluation of that expression is postponed, which often allows cycles to be broken."

That CUE needs *special cycle-breaking machinery at all* is the tell: the underlying model follows references transitively to a fixed point, and only the concrete-value-finality heuristic stops it from looping forever on `a: b+100, b: a-100`. The witness for a narrowed CUE value is a unification/evaluation trace, not a "this rule narrowed this field" list.

**Reading for Precept.** CUE supplies the *result shape* Precept wants (relation tightens a field's domain via meet) but the *wrong evaluation strategy* (fixpoint). This confirms the design's stated divergence: take CUE's `⊓` result, reject CUE's fixpoint. Critically, CUE is shape **A** (the relation is absorbed into the field's value) AND a fixpoint — so "A" alone does not buy boundedness; the boundedness comes from the *discharge strategy*, not the representation.

### The orthogonality the corpus reveals

The A-vs-B representation axis and the bounded-vs-fixpoint discharge axis are **orthogonal**:

| | Bounded discharge | Fixpoint discharge |
|---|---|---|
| **A (relation in the field's bound)** | *(rare — needs a one-pass narrowing rule, no transitive chase)* | CUE (lattice unification); interval domains *with* widening |
| **B (relation as a shared fact)** | **Octagons (strong closure), difference logic (shortest-path)** | Polyhedra; SMT VC-context (Dafny/SPARK) for loops |

The established, named home of "bounded + relational" is the **B / bounded** cell: octagons and difference logic. There is no widely-named domain in the "A / bounded" cell — A-representations that stay bounded do so by *not chasing transitivity* (a deliberate single-hop restriction), which is a design choice rather than a textbook domain.

## Threats to Validity

- **Octagon-closure excerpts from the arXiv author copy, not the published HOSC 2006 DOI.** The arXiv cs/0703084 version (extracted locally via `pdftotext`) is Miné's own copy of the same paper; the Abstract, the O(n³) closure/strong-closure statements, and the precision-vs-intervals/polyhedra positioning are verbatim from it. The published *Higher-Order and Symbolic Computation* 19(1):31–100, 2006 (DOI 10.1007/s10990-006-8609-1) is the citable venue; section numbering may differ slightly between the arXiv and journal layouts. Load-bearing claims (closure is O(n³) Floyd–Warshall; widening is separate; octagons between intervals and polyhedra) are excerpt-grounded and robust to that difference.
- **Difference-logic DPLL(T) detail is Tertiary.** The negative-cycle / Bellman–Ford incremental-propagation framing of Cotton–Maler was reached via a web-search summary, not a fetched primary PDF. The *canonical-form-via-Floyd–Warshall* and *negative-cycle = unsatisfiable* claims are excerpt-grounded from the DBM reference (Secondary); the solver-integration detail is the only Tertiary piece and is not load-bearing for Decision 2 (it only confirms B-discharge is bounded, which octagons already establish).
- **CUE tour pages are Secondary.** The lattice/unification definitions are from the CUE *spec* (Primary); the cycle-breaking mechanism quote is from the CUE *tour* (Secondary). The "CUE is a fixpoint" conclusion rests on the spec's lattice/GLB definition (Primary) plus the existence of cycle-breaking machinery (Secondary) — the inference is sound but the "fixpoint" word is the surveyor's characterization of the lattice-evaluation model, not a verbatim CUE term. Flagged accordingly.
- **The in-tree interval survey is the source for SPARK/EVA/Astrée/LH/Dafny/CBMC/Infer rows.** Those rows inherit that survey's source grades; this file did not re-fetch them. If that survey drifts, the A-vs-B table rows for those systems drift with it.
- **Selection of the A/B axis is the surveyor's framing.** The literature does not universally use "A vs B"; it is a synthesis lens chosen to match Decision 2's two candidate shapes. A reader who rejects the lens would still find the underlying per-system facts (representation, boundedness, precision tier) intact.

## Implications for Precept

1. **§0.4's "bounded relational closure" maps to an established domain operation.** It is octagon **strong closure** — a O(n³) Floyd–Warshall pass over a fixed variable set, which is total and terminating *without* widening. §0.4's "absence of widening" is not a loss; widening is the *separate* operator octagons use only for loop fixpoints, and Precept forms no loops. This is the strongest result for the design: §0.4 is not inventing a bespoke notion — it is naming "octagons minus the loop-widening step."

2. **For Decision 2, the representation axis (A vs B) does NOT determine boundedness — the discharge strategy does.** CUE is shape A *and* a fixpoint; octagons are shape B *and* bounded. So the design cannot pick A vs B *in order to* get boundedness; boundedness is a separate, independently-securable property (cap the closure depth; do one pass). Both of Decision 2's options can be made bounded.

3. **The evidence gently favors the conceptual model behind option B (relation-as-fact), but does not mandate it for the implementation.** The only *named, bounded, relational* domains in the field (octagons, difference logic) are B-representations discharged by shortest-path closure — that is the established precedent for "two-field relation, no solver, legible." Option A (relation absorbed into one field's `IntervalContainmentProofRequirement` bound) has its in-field precedent only in CUE/refinement-types, both of which reach exactness via a fixpoint/SMT the design rejects — so A-as-implemented in Precept would be a *novel* point (bounded single-hop narrowing into a field bound) with no clean textbook home. This is not disqualifying — Precept's single-pass-no-fixpoint posture is itself deliberately off the textbook map — but it means **option A is the less-precedented of the two**, and the design's own note that A "is no longer a `decimal?`… touches more surface" compounds that.

4. **Witness legibility (§0.6 item 3) favors the relation-as-fact view regardless of A/B implementation.** Octagons and difference logic keep the witness as a *rule list* (`±x ± y ≤ c` constraints / the constraint graph) — directly satisfying "the witness is a legible rule list." CUE's unification trace and Dafny's counterexample are *not* rule-list witnesses. Whichever representation Decision 2 picks, the **witness should carry the contributing relation as a named fact** ("`X ≥ Y` narrowed `X`'s lower bound to `Y`'s lower bound"), which is the octagon/difference-logic shape, not the CUE shape.

5. **The depth-bound (Decision 4) is the right lever and has precedent.** Octagon closure is bounded because it operates over a *fixed* variable set and terminates by graph structure, not by chasing references to convergence. Precept's "single-pass, depth 0–1" is the discrete analogue: do the closure once, do not iterate. If samples need more reach, raising to a fixed small N (still not a fixpoint) is exactly "run a bounded number of closure passes" — still terminating, still legible.

## Conclusions

**C1 — §0.4's "bounded relational closure" is the octagon strong-closure operation minus loop-widening.**

- *Rationale.* Octagon strong closure represents the same `±x ± y ≤ c` two-field relations Decision 2 targets, derives the tightest implied bounds in a single O(n³) Floyd–Warshall pass over a fixed variable set, and is total/terminating without widening; widening is a *separate* operator Miné uses only for loop fixpoints, which Precept never forms.
- *Alternatives considered and rejected.* (a) "Bounded relational closure is a Precept-specific term with no precedent" — rejected: it maps onto a named, published domain operation. (b) "Octagons require widening, so they violate §0.4" — rejected: the paper names widening as distinct from closure (Abstract: "a normal form algorithm … **and** a widening operator"); closure alone is the bounded piece.
- *Precedent.* Miné, octagon paper (Abstract; Theorem 3.1/Fig. 7 closure O(N³); § IV.C strong closure O(N³); § VI.C precision between intervals and polyhedra) — all excerpted above.
- *Tradeoff accepted.* Octagons are weakly-relational: they capture `±x ± y ≤ c` but not arbitrary linear relations (that is polyhedra). Precept inherits that ceiling — relations beyond two-field `±`-combinations will not be provable by a closure of this class. Accepted: the design's scope is exactly two-field relational bounds.

**C2 — A-vs-B does not decide boundedness; both Decision-2 options can be bounded, and the evidence leaves the implementation choice genuinely open while gently favoring the relation-as-fact *witness shape*.**

- *Rationale.* The representation axis (relation-in-field-bound vs relation-as-shared-fact) is orthogonal to the discharge axis (bounded vs fixpoint): CUE is A+fixpoint, octagons are B+bounded. Boundedness is secured by capping closure depth, not by the representation. So Decision 2 should be made on the design's stated grounds (blast radius, witness uniformity, consumer surface) — *not* on "which one is bounded," because both can be.
- *Alternatives considered and rejected.* (a) "Pick B because only B is bounded" — rejected: A can be bounded too (CUE is just A done as a fixpoint; a single-hop A is bounded). (b) "Pick A because CUE/refinement-types prove A narrows fields" — rejected: both reach that via the fixpoint/SMT Precept forbids, so they are not precedent for a *bounded* A. (c) "The field gives a clean precedent for one option" — rejected: the field is split on representation; the only clean precedent is for *bounded-relational-as-fact* (octagons/difference logic), which is the conceptual home of B but does not bind the C# data-model decision.
- *Precedent.* The orthogonality table above (CUE A+fixpoint; octagons/difference-logic B+bounded; polyhedra B+fixpoint; Liquid Haskell A+constraint-solve). Witness-legibility: octagons/difference-logic keep a rule-list witness (§0.6 item 3); CUE/Dafny do not.
- *Tradeoff accepted.* This conclusion declines to pick A or B for Precept — it deliberately leaves the data-model choice to the build slice's enumerate/probe step (as the design already specifies), and only constrains the *witness* to be a named-relation rule list. The cost is that this survey does not collapse Decision 2 to one answer; the benefit is honesty — the evidence genuinely does not mandate one representation, and forcing a pick from the literature would be over-reading it.

## What would change these conclusions

- **C1 falsifier.** If a careful reading of the published HOSC 2006 text shows octagon *closure* (not widening) itself requires iteration to a fixpoint on a fixed variable set — i.e. the O(N³) is per-iteration and the number of iterations is unbounded — then "bounded relational closure" would *not* map to octagons and §0.4 would be claiming a stronger property than the established domain provides. (Current excerpts say closure is a single Floyd–Warshall pass; this would require them to be wrong.)
- **C2 falsifier (toward B).** If, during the build, option A's bound-variant turns out to touch *only* the enumerated consumers (Strategy 8, `TypedFieldRef`, MCP DTO) and produces a *uniform* witness, the "A is less-precedented / more surface" caution weakens and A becomes the cleaner pick — flipping the gentle lean.
- **C2 falsifier (toward neither / split confirmed).** If three or more additional weakly-relational systems are found that implement the relation as an *in-field refinement* (shape A) with a *bounded* (non-fixpoint) discharge, the "A/bounded cell is empty in the literature" claim is wrong and A gains the textbook precedent it currently lacks.

## Open Questions

- The exact published-venue section numbers for the octagon closure/strong-closure theorems (arXiv layout used here; HOSC 2006 DOI is the citable venue).
- Whether Precept's eventual depth-bound (Decision 4) should be expressed as "N closure passes" (octagon-analogue) or "N transitive hops" (reference-chase analogue) — the two coincide at N=1 but may diverge for N>1; out of scope for Decision 2, relevant to Decision 4's eventual lock.
- Whether any production system implements *bounded* (capped-depth) octagon closure deliberately as a precision/cost knob — the literature treats closure as run-to-normal-form, not capped. (Precept's cap is novel in *intent* even though the per-pass operation is standard.)

## Sources

- **The Octagon Abstract Domain** — Antoine Miné — *Higher-Order and Symbolic Computation* 19(1):31–100, 2006, DOI 10.1007/s10990-006-8609-1; author copy arXiv:cs/0703084 (https://arxiv.org/abs/cs/0703084, https://arxiv.org/pdf/cs/0703084, accessed 2026-06-02; PDF text-extracted locally via pdftotext). **Primary.** Load-bearing for C1; excerpts on constraint form, closure/strong-closure O(N³) Floyd–Warshall, widening-as-separate, and precision-between-intervals-and-polyhedra. Mirrored to `research/references/octagon-domain/octagon-domain-mine-hosc2006-arxiv-cs0703084.txt`.
- **The CUE Language Specification** — CUE Authors — cuelang.org/docs/references/spec/ (accessed 2026-06-02). **Primary.** Lattice / unification / GLB definitions.
- **Reference Cycles** (CUE tour) — CUE Authors — cuelang.org/docs/tour/references/cycle/ (accessed 2026-06-02). **Secondary.** Cycle-breaking / postponed-evaluation mechanism.
- **Difference bound matrix** — Wikipedia — en.wikipedia.org/wiki/Difference_bound_matrix (accessed 2026-06-02). **Secondary.** Floyd–Warshall canonical form; negative-cycle = unsatisfiable.
- **Fast and Flexible Difference Constraint Propagation for DPLL(T)** — S. Cotton, O. Maler — SAT 2006, LNCS 4121, DOI 10.1007/11814948_19. **Tertiary** (reached via web-search summary; not fetched). Incremental Bellman–Ford theory propagation; not load-bearing.
- **proof-engine-interval-arithmetic-survey.md** (in-tree) — covers SPARK/GNATprove, Frama-C EVA, Astrée (octagons + polyhedra, line 223), Liquid Haskell (lines 276–370), Dafny, CBMC, Infer. Source grades per that file. Cited for those systems' representation/discharge; not re-surveyed.
- **research/language/references/constraint-composition.md** (in-tree) — CUE lattice, Zod/Pydantic/FluentValidation no-propagation reference; value-narrowing as "the absent capability."
- **docs/language/precept-language-spec.md §0.4** (line 174) — "Standard interval arithmetic, bounded relational closure, and single-pass validation … The absence of widening is a feature." The Precept-side claim this survey grounds.
