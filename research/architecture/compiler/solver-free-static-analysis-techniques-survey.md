---
status: Active
authored: 2026-06-06
author: research (lifecycle-1)
topic: Which solver-free static-analysis techniques (abstract domains, legible decision procedures, structural/combination methods) could extend Precept's compile-time proof coverage subject to (R1) no opaque solver / legible witness and (R2) per-keystroke LS worst-case predictability
external-engagement: strong
---

# Solver-free static-analysis techniques for Precept's proof engine

> Precept's proof engine proves arithmetic/structural safety obligations at compile time with no SMT solver and a legible witness, recompiling end-to-end after every keystroke. This survey evaluates the candidate static-analysis techniques (numeric abstract domains, legible decision procedures, structural/combination methods) against two hard requirements — **R1: solver-free + legible witness** (spec §0.6 #3) and **R2: per-keystroke worst-case predictability** (~44 ms corpus / ~3 ms worst file baseline) — and ranks them by value-per-cost within both constraints. Intended consumer: a future `/design` on extending the proof engine.

## Background

This is **authorized horizon-groundwork.** The proof engine today runs interval arithmetic plus a single-pass depth-1 difference-bound/octagon-style relational narrowing (`docs/compiler/proof-engine.md` Strategy 4; `ProofEngine.Intervals.cs` `RelationalHalfLine`/`BuildNarrowedIntervals`). The known proof gaps — enumerated by the demand-side corpus audit (`fragment-boundary-corpus-validation-2026-06-05.md`) and the capability-side siblings — are: **3-field linear identities** (esp. equality `A+B==C`), **non-unit-coefficient two-variable** (`X ≤ c·Y`), collection aggregates, products/ratios, equality/disequality, interval-overlap, distributional stats, semantic completability/liveness, and rule-preservation across operations.

This survey does **not** re-survey the interval/SPARK/EVA/Astrée/LH/Dafny/CBMC/Infer comparator corpus (already in `proof-engine-interval-arithmetic-survey.md`) nor re-derive the octagon-closure-minus-widening result (already locked in `relational-constraint-representation-survey.md`, C1). It **builds forward**: classifying the *full* technique space by abstract domain and decision procedure, attaching worst-case complexity + witness form + per-gap coverage to each, and ranking value-per-cost within R1+R2.

The two hard requirements, verbatim from the spec:

**R1 — no opaque solver / legible** [`docs/language/precept-language-spec.md` §0.6 #3]:

> "Opaque solvers are rejected on principle. The language's proof reasoning must be legible … This is why SMT/Z3 solvers are excluded even when they could prove more: opaque proof witnesses violate the inspectability commitment."

**R2 — per-keystroke worst-case predictability.** The full pipeline (lex → parse → type-check → proof → graph) recompiles in the language server after every keystroke; baseline ~44 ms for the whole 77-file corpus, ~3 ms worst single file (MEMORY: *compiler is fast, no incremental*). The binding property is **bounded, data-independent worst-case** — a per-keystroke recompile cannot tolerate a pathological stall, so a technique whose worst-case is exponential or data-dependent is risky even if fast on average.

The §0.4 execution model is the enabling backdrop: **no loops → no fixpoint → no widening needed** (§0.4 items 1/3). Every domain below is evaluated *without its widening operator*, because Precept forms no loops; the cost question is therefore purely the per-operation (closure/join/transfer) cost, not fixpoint convergence.

## Methodology

- **Research question.** Which solver-free static-analysis techniques could improve Precept's compile-time proof coverage subject to R1 (legible witness, no opaque solver) and R2 (per-keystroke worst-case predictability), and how do they rank by value-per-cost within both?
- **Comparator enumeration (not convenience-sampled).** The full technique space was enumerated up front from the abstract-interpretation domain lattice (Apron's expressiveness lattice, Fig. 2 of the CAV09 paper, was used as the spanning set) and the decision-procedure literature, not from techniques the author already knew. Three families: **(A) numeric abstract domains** — intervals, zones/DBM, octagons, TVPI/subpolyhedra, Karr affine-equality, linear congruences, convex polyhedra; **(B) legible decision procedures** — congruence closure/EUF, difference-logic/UTVPI decision, decidable string fragments; **(C) structural/combination** — reduced products, affine arithmetic, predicate/boolean abstraction, incremental aggregates + monotonicity, dominator-based fact propagation, guard-feasibility via DBM emptiness, backward requirement propagation.
- **Per-technique sub-questions.** For each: (1) Is it solver-free, and what is the witness form? (R1 pass/fail) (2) Worst-case complexity per operation (closure/join/transfer) and is it bounded/data-independent? (R2 pass/borderline/risk) (3) Which named Precept gap(s) does it close? (4) Prior art with perf evidence.
- **Search strategy.** Primary-source fetch (PDF → `pdftotext`) of: Müller-Olm & Seidl POPL'04 (Karr complexity), Apron CAV'09 (domain lattice/cost), Bagnara/Hill/Zaffanella SCP'08 (PPL polyhedra cost + watchdog mitigation), Nieuwenhuis & Oliveras IC'07 (congruence closure complexity + proof-producing union-find), Simon/King/Howe HOSC'10 (TVPI strongly-polynomial). Octagon closure complexity reused from the in-tree mirror (`octagon-domain-mine-hosc2006-arxiv-cs0703084.txt`, Miné). Web-search confirmation for affine arithmetic (Stolfi/de Figueiredo), predicate abstraction (Graf-Saïdi/Ball-Rajamani SLAM), linear congruences (Granger). Precept-side state read directly from `docs/compiler/proof-engine.md` and `src/Precept/Pipeline/ProofEngine.*.cs`.
- **Inclusion / exclusion.** Included: any technique that could discharge a *Precept* obligation class with a legible witness and bounded worst-case. Excluded: SMT/SAT-as-decision-engine (R1 hard fail by spec); widening operators (no loops); runtime-validation libraries (no static proof). Predicate abstraction is included but flagged — its *abstraction step* classically uses a solver.
- **Source-grade mix.** Primary: Miné octagon (HOSC'06), Müller-Olm/Seidl (POPL'04), Nieuwenhuis/Oliveras (IC'07), Simon/King/Howe (HOSC'10), Bagnara et al. (SCP'08), Miné/Jeannet Apron (CAV'09). Secondary: affine-arithmetic project pages, predicate-abstraction papers reached via search summary. Tertiary: a small number of complexity restatements reached only via web-search summary (flagged in Threats).
- **Time bounds.** External fetches 2026-06-06; Precept-side reads against working tree HEAD on `spike/Precept-V2-Radical` 2026-06-06.

## Findings

### The cost/precision lattice (R2's spine)

The numeric domains form a total order on precision and a (roughly) total order on cost. Apron's CAV'09 paper presents them as an expressiveness lattice [Primary; Miné & Jeannet, *Apron: A Library of Numerical Abstract Domains for Static Analysis*, CAV 2009, LNCS 5643; access 2026-06-06; mirrored to `research/references/solver-free-static-analysis/apron-domains-mine-jeannet-cav2009.txt`]:

> "They vary in expressiveness and in the cost/precision trade-off." (Introduction)

> "the boxed domains are those currently available: intervals, octagons [20] … polyhedra [10] (using GMP integers and the double description method), and linear equalities [17] (implemented as an abstraction of polyhedra). … Linear congruences [13] … are optionally available through the PPL [5]." (§ Domains)

The key R2 fact: the domains split cleanly into a **safely-polynomial, data-independent** band (intervals, zones/DBM, octagons, TVPI-rational, Karr affine, linear congruences) and an **exponential-worst-case** band (convex polyhedra via double-description). The boundary is exactly where Precept's R2 risk line falls.

### Family A — numeric abstract domains

| Domain | Represents | Witness form (R1) | Per-op worst-case (R2) | Closes which Precept gap | Prior-art perf evidence |
|---|---|---|---|---|---|
| **Intervals** (shipped) | `v ∈ [lo,hi]`, one var | per-field interval + contributing constraints | O(n) state, O(1) transfer — **safe** | (baseline) | Astrée core; non-relational |
| **Zones / DBM** (≈shipped) | `x − y ≤ c` | constraint graph / shortest path | O(n²) mem, **O(n³) closure** — **safe (bounded, predictable)** | unit-coeff 2-var differences (partly shipped via depth-1 half-line) | difference logic; PPL "bounded difference shapes" fallback |
| **Octagons** (≈shipped) | `±x ± y ≤ c` | DBM of `±x±y≤c` (a rule list) | O(n²) mem, **O(n³) strong closure** — **safe** | sums `x+y≤c`, sign-mixed 2-var | Miné HOSC'06; Astrée at Airbus scale |
| **TVPI / subpolyhedra** | `a·x + b·y ≤ c`, **arbitrary coeffs**, ≤2 vars | planar polyhedra per var-pair (inequality list) | **strongly polynomial (rational)**; integral tightening NP-complete (approximated) — **borderline** | **non-unit-coeff 2-var `X ≤ c·Y`** (a named gap) | Simon/King/Howe HOSC'10 |
| **Karr affine equalities** | `a₀ + Σaᵢxᵢ = 0` (linear **equalities**, any arity) | a basis/system of equalities (Gaussian-eliminated) | **O(program · k³)**, k=#vars — **safe (polynomial, deterministic)** | **3-field linear identity `A+B==C`** (the #1 named gap) | Müller-Olm/Seidl POPL'04 |
| **Linear congruences** | `x ≡ c (mod m)`, `Σaᵢxᵢ ≡ c (mod m)` | congruence system | polynomial (Granger) — **safe** | modulo/divisibility defaults (a minor gap) | Granger 1989/1991 |
| **Convex polyhedra** | `Σaᵢxᵢ ≤ c`, **arbitrary** linear | constraint **and** generator system | **EXPONENTIAL worst-case (double description)** — **RISK** | general N-var linear relations | PPL SCP'08 (with watchdog mitigation) |

**Karr's affine-equality domain — closes the #1 gap, and is solver-free + polynomial.** Müller-Olm & Seidl's reformulation of Karr [Primary; Müller-Olm & Seidl, *Precise Interprocedural Analysis through Linear Algebra*, POPL 2004; access 2026-06-06; mirrored to `…/karr-affine-relations-muller-olm-seidl-popl2004.txt`]:

> "Karr's algorithm … determines all intraprocedurally valid affine relations in an affine program." (§1)

> "An affine relation is a condition [of the form a₀ + a₁x₁ + … + aₙxₙ = 0] … where x₁, …, xₙ are program variables." (§1)

> "The running time of our algorithms is linear in the program size and polynomial in the number of occurring variables." (Abstract)

> "Our base algorithm as well as the extended algorithms run in time linear in the program size and polynomial in the number of program variables." (§1)

The witness is *exactly* a legible artifact: the domain element **is** a finite system of linear equalities (a vector-space basis), and the analysis works by Gaussian elimination over that basis:

> "the set of all affine relations forms an F-vector space." (§3)

> "the affine relation a is valid at a program point u, iff Wₐ a = 0 for all r ∈ R(u)." (Lemma 1)

Crucially the domain captures **equalities** (`A + B = C`), which intervals/octagons/DBM structurally cannot — the octagon ceiling is `±x ± y ≤ c` (two-variable, unit-coefficient *inequalities*), so a three-field balance identity is provably out of the octagon fragment but provably *in* Karr's. This is the single domain that closes the corpus's one recurring out-of-fragment numeric invariant (the `A + B ≤/== C` balance identity in saas-license / equipment-lease / production-order / event-venue, per `fragment-boundary-corpus-validation-2026-06-05.md`).

**TVPI — closes the non-unit-coefficient gap, strongly polynomial but with an integral caveat.** Simon/King/Howe [Primary; Simon, King & Howe, *The Two Variable Per Inequality Abstract Domain*, HOSC 23(1), 2010; access 2026-06-06; mirrored to `…/tvpi-two-variable-per-inequality-simon-king-howe-hosc2010.txt`]:

> "the Two Variable Per Inequality abstract domain … is able to express systems of linear inequalities where each inequality has at most two variables. The domain represents a sweet-point in the performance-cost tradeoff between the faster Octagon domain and the more expressive domain of general convex polyhedra." (Abstract)

> "the only relational domain that combines linear relations with arbitrary coefficients and strongly polynomial performance." (Abstract)

The cost/precision positioning matches Precept's exact gap — octagons cannot express `2·x ≤ y`:

> "The Octagon domain [47] only allows linear inequalities of the form [±xᵢ ± xⱼ ≤ c] … the precision loss that occurs when using only Octagons. For example, [a non-unit-coefficient relation] needs to be expressed as an adjunct to the Octagon domain." (§1)

The R2 caveat (flagged honestly): the **integral** tightening is NP-complete, approximated to stay polynomial —

> "since calculating a Z-polyhedron from a given TVPI polyhedron is NP-complete [42], we present an approximation that is precise in practice but retains the strongly polynomial performance." (§1)

So TVPI over **rationals/decimals** is strongly polynomial (R2 safe); the integer-exactness layer is the part that would need the documented approximation to avoid an NP-hard worst-case. Precept's numeric fields are decimal-backed, so the rational path is the relevant one — but the integral caveat is why TVPI rates **borderline** rather than **safe**: a future integer-exactness obligation would require adopting the approximation, not the exact algorithm.

**Convex polyhedra — closes everything, but is R2-disqualifying.** Polyhedra (Cousot–Halbwachs) express arbitrary linear relations, but the double-description method is exponential in the worst case [Primary; Bagnara, Hill & Zaffanella, *The Parma Polyhedra Library*, SCP 72(1–2), 2008; access 2026-06-06; mirrored to `…/parma-polyhedra-library-bagnara-hill-zaffanella-scp2008.txt`]:

> "Since the worst case space complexity of the methods employed is exponential, in general the client application cannot make a safe and practical choice." (§3.2)

The decisive R2 evidence is that PPL itself ships a **watchdog/timeout** mitigation and falls back to a *polynomial* domain (zones/"bounded difference shapes") when the exponential blows up:

> "the user can set a timeout for the operations … When a resource limit is reached, the class temporarily switches to a simpler family of polyhedra: bounded difference shapes … computed using a polynomial complexity method." (§3.3, Up_Appr_Polyhedron example)

A timeout-and-fall-back-to-zones strategy is exactly what a per-keystroke recompile *cannot* tolerate: it makes the verdict (and its latency) data-dependent. Polyhedra are the canonical "LS-risky" technique — flagged RISK, recommended avoid for the interactive path.

### Family B — legible decision procedures (used legibly, not as SMT)

**Congruence closure / EUF (union-find) — solver-free, O(n log n), proof-producing witness.** This is the strongest R1 result in the survey: congruence closure is a *decision procedure* (it decides satisfiability of conjunctions of ground equalities/disequalities) yet is **not** an opaque solver — it is a union-find computation whose witness is an explicit equality chain [Primary; Nieuwenhuis & Oliveras, *Fast congruence closure and extensions*, Information and Computation 205(4), 2007; access 2026-06-06; mirrored to `…/fast-congruence-closure-nieuwenhuis-oliveras-ic2007.txt`]:

> "we present a very simple and clean incremental congruence closure algorithm and show that it runs in the best known time O(n log n)." (Abstract)

> "we introduce a proof-producing union-find data structure that is then used for extending our congruence closure algorithm, without increasing the overall O(n log n) time, in order to produce a k-step explanation for a given equation." (Abstract)

The **k-step explanation** is precisely the legible witness §0.6 #3 demands — a chain of the equalities that forced the conclusion, the union-find path, not a solver trace. The historical lineage (Downey-Sethi-Tarjan JACM'80, Nelson-Oppen JACM'80) established the O(n log n) bound; the proof-producing variant is what makes it R1-compliant. Precept use: equality/disequality reasoning over field references (the **equality/disequality gap**), and structural-equality facts (`X == Y` rules feeding sign/divisor decisions). R2: **safe** (quasi-linear, deterministic, incremental — well-suited to per-keystroke).

**Difference-logic / UTVPI decision.** Already substantially the engine's relational core (depth-1 half-line narrowing ≈ a single-pass DBM). The full decision procedure (conjunction of `x − y ≤ c` satisfiable iff no negative cycle, decided by Floyd–Warshall/Bellman–Ford) is the *guard-feasibility* lever (see Family C). R1: witness = the constraint graph / negative cycle (legible). R2: **safe** (O(n³) closure, the same bound as octagons; bounded). Already cited in `relational-constraint-representation-survey.md` Gap 2; not re-derived here.

**Decidable string fragments (length / prefix / automaton).** Precept already runs a sound **string-length interval** domain (`StringLengthIntervalOf`, Strategy 9). A richer decidable fragment (prefix/suffix relations, or a bounded-automaton membership test) would close some string-shape obligations, but the corpus audit found string-length the dominant string need and it is already covered. R1: length intervals are legible; full string-automaton witnesses are larger but still structured. R2: length intervals **safe**; general string-constraint decision procedures are **borderline-to-risk** (automaton products can blow up). Low priority — the demand is mostly already met.

### Family C — structural / combination techniques

**Reduced products (combine cheap domains).** A reduced product runs two cheap domains and exchanges facts between them (e.g. intervals × congruences, or octagons × Karr). Apron implements exactly this [Primary; Apron CAV'09]:

> "Apron supports a generic reduced product which is instantiated to combine polyhedra and linear congruences." (§ Domains)

For Precept this is the *composition mechanism* for adopting Karr alongside the existing interval/octagon core without a monolithic redesign — each domain keeps its own legible witness, and the reduction is a bounded fact-exchange pass. R1: **pass** (each component witness is legible; the reduction is a finite narrowing). R2: **safe** if both components are polynomial (interval × octagon × Karx all are). This is the recommended *architecture* for layering, not a standalone capability.

**Affine arithmetic (correlation tracking).** AA represents a quantity as `x₀ + x₁ε₁ + … + xₖεₖ` with `εᵢ ∈ [−1,1]`, tracking first-order correlations interval arithmetic loses [Secondary; Stolfi & de Figueiredo, *Self-Validated Numerical Methods and Applications*, 1997 / *Affine Arithmetic: Concepts and Applications*, Numerical Algorithms 37, 2004; access 2026-06-06]:

> "Unlike interval arithmetic, it keeps track of correlations between computed and input quantities, and is therefore resistant to the catastrophic loss of precision often observed in long interval computations."

AA would sharpen Precept's *computed-field* interval inference (e.g. `(A + B)/2` where A and B are correlated, or repeated subexpressions) — the interval-dependency problem the engine's `IntervalOfNarrowed` propagation hits. R1: the affine form (the εᵢ coefficient vector) is a structured, legible witness. R2: **safe** (per-op cost grows with the number of noise symbols, but bounded and deterministic for finite acyclic expression trees — §0.4 guarantees finiteness). Niche: only helps where correlation loss is the cause of a false NumericOverflow; the corpus audit found computed fields are mostly *unconstrained* derived `<-` fields (no obligation), so demand is modest.

**Predicate / boolean abstraction — FLAG R1.** Predicate abstraction (Graf-Saïdi'97; Ball-Rajamani SLAM) tracks the truth of a finite predicate set [Secondary; Ball & Rajamani, SLAM/Bebop; access 2026-06-06]. The capability (reason over a finite set of boolean facts) is attractive, **but the classical abstraction step uses a theorem prover/solver** to compute the abstract transformer (SLAM's C2bp calls a prover; Cartesian abstraction is the solver-light approximation). Adopting predicate abstraction *with* its solver-based abstraction violates R1. A **solver-free** variant — enumerate a fixed predicate set and propagate truth syntactically (no prover to compute the abstraction) — is possible but is essentially what the engine's guard-narrowing already does in miniature. Verdict: **R1 fail as classically formulated**; the solver-free degenerate case adds little over existing guard tracking. Avoid unless a specific finite-predicate obligation appears.

**Incremental/maintained aggregates (view maintenance) + monotonicity.** For collection-aggregate gaps (sum/count/min/max invariants maintained across operations), incremental view-maintenance reasoning + monotonicity (an `add` to a non-negative collection can only *increase* a sum) is a sound, solver-free direction. This is exactly the model the engine already uses for the **count interval** (Strategy 10: per-kind/per-action sound deltas seeded from the band). Extending the same delta-machinery to numeric aggregates (sum/min/max bounds) is a bounded, legible, polynomial extension. R1: **pass** (the witness is the delta chain / monotonicity argument). R2: **safe** (per-mutation O(1) delta, same as count). High value for the **collection-aggregate gap**, and it reuses shipped machinery.

**Dominator-based fact propagation (reuse graph-analyzer dominators).** The graph analyzer already computes dominators (Lengauer–Tarjan O(V+E), §0.5 #4). A fact established on every path that dominates a use-site is true at the use-site — so dominator information could license propagating a guard/ensure fact forward to dominated rows without re-proving. R1: **pass** (witness = the dominating row + dominance path). R2: **safe** (dominators already computed; the propagation is a graph walk). Value: sharpens cross-row proof reuse; modest gap coverage but near-zero marginal cost (reuses an existing structure). Note §0.5's overapproximation rule (edges treated as traversable) means dominance is *structural* — sound for "fact holds on all structural paths," which is the conservative direction.

**Guard-feasibility via DBM emptiness.** The existential "is this guard ∧ post-state satisfiable" test is **DBM emptiness** (negative-cycle detection) — polynomial, solver-free, the same machinery as the existing PRE0154/0155 contradiction scan. This is already established in `liveness-completability-verification-survey.md` as the solver-free route to the completability/liveness gap. R1: **pass** (witness = the negative cycle = the contradicting constraint set). R2: **safe** (O(n³)). Value: closes the **semantic completability/liveness gap** *under-approximated with a witness* (the existential mirror of proof-philosophy #1/#2). Already surveyed; cited forward, not re-derived.

**Backward requirement propagation.** Propagate an obligation backward from a fault site to the inputs that must satisfy it (weakest-precondition-style, but bounded by §0.4's no-loops/no-branches → a single straight-line WP, not a fixpoint). R1: **pass** (the propagated requirement is a legible constraint on the source). R2: **safe** (linear walk; §0.4 guarantees no join points). Value: improves *attribution* (names the source field that should carry the bound) more than it closes new gaps — complements the §0.7 "name what would make it safe" posture.

### Worst-case predictability summary (R2 spine)

| Band | Techniques | R2 |
|---|---|---|
| **Safely polynomial, data-independent** | intervals, zones/DBM, octagons, **Karr affine**, linear congruences, congruence closure (O(n log n)), reduced products, affine arithmetic, incremental aggregates, dominator propagation, DBM-emptiness guard-feasibility, backward propagation | **fit per-keystroke** |
| **Polynomial-with-a-caveat** | **TVPI** (rational strongly-polynomial; integral tightening NP-complete → must use the approximation) | **borderline — adopt the rational/approximate path only** |
| **Exponential worst-case** | **convex polyhedra** (double description) | **RISK — avoid on the interactive path; needs timeout+fallback, which is itself R2-incompatible** |

The ~44 ms / ~3 ms baseline means a single O(n³) closure over the per-file variable set (octagon/DBM/Karr scale, where n = fields in a precept, typically <50) is comfortably within budget — these are O(n³) in a *small, statically-bounded* n, not in program size. Polyhedra's exponential is in the *number of generators*, which is data-dependent and unbounded — the disqualifying difference.

## Threats to Validity

- **Some complexity restatements are Tertiary.** The Downey-Sethi-Tarjan / Nelson-Oppen O(n log n) congruence-closure *historical attribution* and the Granger linear-congruence complexity were reached via web-search summary; the **load-bearing** O(n log n) + proof-producing-witness claim is excerpt-grounded from the fetched Nieuwenhuis-Oliveras IC'07 primary. The Granger "polynomial" claim is the weakest-grounded numeric-domain row (Secondary/Tertiary) — flagged; linear congruences is a low-priority recommendation regardless, so this does not move a load-bearing conclusion.
- **Affine-arithmetic and predicate-abstraction rows are Secondary.** AA's correlation-tracking quote is from the project/survey pages, not the fetched primary; the predicate-abstraction "uses a solver" claim is from search summaries of SLAM/Graf-Saïdi. The R1-fail verdict on predicate abstraction rests on the well-documented SLAM-uses-a-prover fact; if a fully solver-free predicate-abstraction formulation is more capable than characterized, the "avoid" verdict could soften — but the engine's existing guard-narrowing already covers the solver-free degenerate case, so the practical recommendation is robust.
- **TVPI integral-vs-rational nuance.** The NP-completeness is specifically of computing the *integer hull* (Z-polyhedron) of a TVPI system; the rational TVPI domain is strongly polynomial. The survey treats Precept's decimal fields as the rational path. If a future obligation genuinely needs integer-exact TVPI reasoning, the borderline rating becomes load-bearing and the documented approximation must be adopted — re-verify before locking.
- **PDF text-extraction fidelity.** All primary excerpts were extracted via `pdftotext`; mathematical notation (subscripts, `±`, matrices) may render imperfectly in the mirrors. The prose excerpts quoted are robust to this; the formula reconstructions (`a₀ + Σaᵢxᵢ = 0`, `±x ± y ≤ c`) are the surveyor's clean restatement of the extracted forms, cross-checked against the in-tree octagon mirror.
- **Precept-side gap inventory inherited.** The "named gaps" come from `fragment-boundary-corpus-validation-2026-06-05.md` and siblings; if that corpus audit drifts (e.g. a new sample introduces a heavy product/ratio invariant), the per-gap *demand* weighting shifts even though the per-technique *capability* facts hold.
- **n is assumed small and bounded.** The R2-safe verdict for O(n³) domains assumes per-precept variable counts stay small (<~50 fields). A pathological precept with hundreds of numeric fields would make even O(n³) closure noticeable per keystroke. The corpus baseline supports the assumption, but a field-count cap (reject-or-degrade above N) would harden it — flagged as an open question.

## Implications for Precept

1. **The #1 named gap (3-field linear identity `A+B==C`) has a solver-free, polynomial, legible-witness domain that closes it exactly: Karr affine equalities.** This is the highest-value finding — it is the one domain that adds *equality* reasoning (which intervals/octagons structurally cannot represent) at polynomial cost with a Gaussian-eliminated equality basis as the witness.
2. **The cost line that R2 draws is precisely the polyhedra boundary.** Everything at or below octagon/Karr/TVPI-rational/congruence-closure expressiveness is safely polynomial; convex polyhedra (and only polyhedra) cross into exponential. R2 therefore *defines* the proof engine's ceiling: weakly-relational + affine-equality, never full polyhedra.
3. **Adoption should be by reduced product, not replacement.** The existing interval+octagon core stays; Karr (and optionally TVPI/congruences) layer in as reduced-product components, each keeping its own legible witness — matching Apron's architecture and preserving §0.6 #6 attribution.
4. **Several high-value extensions reuse machinery already in the engine.** Incremental aggregates (extend the Strategy-10 count-delta walk to sum/min/max), dominator-based propagation (reuse §0.5 #4 dominators), and DBM-emptiness guard-feasibility (reuse the PRE0154/0155 contradiction scan) are near-zero-marginal-cost wins.
5. **Predicate abstraction and full string-constraint solving are the two "looks useful, fails a requirement" traps** — predicate abstraction fails R1 (solver in the abstraction step) and string-automaton solving risks R2; both should be explicitly declined.

## Conclusions

Ranked by **value-per-cost within R1+R2** (which to adopt, in what order, which to avoid).

**C1 — Adopt Karr's affine-equality domain (as a reduced-product component) — highest value-per-cost.**
- *Rationale.* It is the only surveyed domain that adds linear-*equality* reasoning (`A + B = C`), closing the corpus's single recurring out-of-fragment numeric invariant, and it does so solver-free, in time *linear in program size and polynomial in variable count*, with a witness (a Gaussian-eliminated equality basis) that is structured legible data — satisfying both R1 and R2.
- *Alternatives considered and rejected.* (a) **Polyhedra** — would also close it (and more) but is exponential worst-case (R2 disqualifying; PPL itself mitigates with timeout+fallback, which a per-keystroke recompile cannot tolerate). (b) **Stay octagon-only** — rejected: octagons structurally cannot represent equalities (`±x±y≤c` only), so the 3-field identity stays unprovable. (c) **TVPI** — closes the *non-unit-coefficient inequality* gap but not the *equality* gap, and carries the integral NP-completeness caveat; lower priority than Karr.
- *Precedent.* Müller-Olm/Seidl POPL'04 (affine relations, linear-in-program/polynomial-in-vars, vector-space witness); Apron CAV'09 (Karr = "linear equalities … implemented as an abstraction of polyhedra," composable via reduced product).
- *Tradeoff accepted.* Karr captures affine *equalities* only — not inequalities (octagons/TVPI own those) and not nonlinear relations (products/ratios stay out). Precept inherits that ceiling; combined with the existing octagon/interval core via reduced product, the union covers equalities + weak inequalities but never general linear arithmetic.

**C2 — Ship the cheap, machinery-reusing structural wins next: incremental aggregates, dominator-propagation, DBM-emptiness guard-feasibility.**
- *Rationale.* Each closes a named gap (collection aggregates; cross-row reuse; completability/liveness) at near-zero marginal cost by reusing shipped machinery (Strategy-10 delta walk; §0.5 dominators; PRE0154/0155 contradiction scan), all solver-free with legible witnesses (delta chain / dominance path / negative cycle), all polynomial.
- *Alternatives considered and rejected.* (a) **Defer until after a domain rewrite** — rejected: these are additive and independent of the Karr layering; sequencing them after a heavier domain change wastes their low cost. (b) **Predicate abstraction for the cross-row/aggregate reasoning** — rejected: R1 fail (solver in abstraction step) and the solver-free degenerate case is no better than existing guard tracking.
- *Precedent.* The engine's existing count-band tracking (Strategy 10) is incremental-aggregate reasoning already shipped; `liveness-completability-verification-survey.md` establishes DBM-emptiness as the solver-free liveness route; §0.5 already computes dominators.
- *Tradeoff accepted.* These are sound-but-incomplete sharpenings, not new expressiveness frontiers — they extend reach within the existing fragment rather than enlarging it, and under-approximate where soundness demands (liveness must carry a witness, never certify a path it can't exhibit).

**C3 — Treat TVPI (rational path) as an optional third tier for the non-unit-coefficient gap; congruence-closure for equality/disequality.**
- *Rationale.* TVPI closes `X ≤ c·Y` with strongly-polynomial rational performance and a planar-polyhedra witness; congruence closure closes equality/disequality with O(n log n) and a proof-producing union-find witness (both R1+R2 pass on their relevant paths). They are lower priority than C1/C2 because the corpus demand for each is thinner (7 non-unit-coeff rules; scattered equality uses).
- *Alternatives considered and rejected.* (a) **Octagons for `X ≤ c·Y`** — rejected: octagons are unit-coefficient only. (b) **SMT/EUF for equality** — rejected: opaque (R1). (c) **Adopt TVPI's integral-exact algorithm** — rejected on the interactive path: integer hull is NP-complete; the rational/approximate path is the R2-safe choice.
- *Precedent.* Simon/King/Howe HOSC'10 (TVPI strongly-polynomial, sweet-spot between octagon and polyhedra); Nieuwenhuis/Oliveras IC'07 (congruence closure O(n log n) + proof-producing union-find).
- *Tradeoff accepted.* TVPI's integral-exactness is sacrificed (rational approximation) to keep R2; congruence closure adds a second relational mechanism (maintenance cost) for a thin demand. Adopt only if the design pass confirms the demand justifies the surface.

**C4 — Avoid convex polyhedra and (as classically formulated) predicate abstraction on the interactive path.**
- *Rationale.* Polyhedra are exponential worst-case (R2 disqualifying — PPL itself uses timeout+fallback, which makes verdict latency data-dependent, the one thing a per-keystroke recompile forbids). Predicate abstraction's abstraction step uses a theorem prover (R1 disqualifying), and its solver-free degenerate case adds nothing over existing guard narrowing.
- *Alternatives considered and rejected.* (a) **Polyhedra with a watchdog/timeout** — rejected: a timeout makes the proof outcome and latency non-deterministic, violating the determinism commitment (§0.6 #1 soundness + the "no solver timeouts" right-sizing rationale in `proof-engine.md` §4). (b) **Cartesian (solver-light) predicate abstraction** — rejected: still needs a transformer computation, and the truth-propagation it reduces to is already the engine's guard model.
- *Precedent.* PPL SCP'08 (exponential worst-case + watchdog mitigation); SLAM/Graf-Saïdi (prover-based abstraction); `proof-engine.md` §4 ("no solver timeouts or resource limits … Determinism 100%").
- *Tradeoff accepted.* Precept permanently forgoes general N-variable linear-relation proving and arbitrary-predicate reasoning. This is the deliberate expressiveness ceiling §0.4/§0.6 already commits to — the survey confirms it is the *right* ceiling under R1+R2, not a regrettable limitation.

## What would change these conclusions

- **C1 falsifier.** If a probe of `ProofEngine.*.cs` shows Karr-style equality facts cannot be threaded through the existing reduced-product / `narrowed`-dictionary architecture without a fixpoint (e.g. equality propagation requiring transitive chasing that §0.4's single-pass envelope forbids), Karr's "polynomial + fits the architecture" claim weakens and the 3-field gap may need a bespoke fixed-arity strategy instead of the general domain.
- **C1/C3 falsifier (demand).** If a future corpus introduces heavy *nonlinear* invariants (products/ratios as *constrained* rules, not just derived `<-` fields), neither Karr nor TVPI closes them, and the gap inventory's "extend toward equalities, not general arithmetic" verdict (from the fragment-boundary audit) would need revisiting — possibly toward a fixed-degree polynomial domain (still solver-free per Müller-Olm/Seidl's degree-d extension, O(n·k^{3d}), but the exponent in d is a new R2 risk).
- **C2 falsifier.** If extending the count-delta walk to numeric aggregates turns out to require a join over divergent mutation orders (a fixpoint), the "near-zero marginal cost, reuses Strategy-10" claim fails and incremental aggregates drop in priority.
- **C4 falsifier.** If a bounded-dimension polyhedra variant (cap the number of variables/generators to a small fixed N, reject above it) is shown to give *data-independent* worst-case within budget, polyhedra could move from RISK to a capped tier — but this requires the cap to be a hard reject (not a timeout), preserving determinism. Absent such a cap, C4 holds.
- **R2-n falsifier.** If profiling shows real precepts with field counts large enough that a single O(n³) closure (octagon/Karr scale) exceeds the per-keystroke budget, the "safely polynomial" band needs a field-count cap and the whole budget analysis re-grounds against the new worst-file number.

## Open Questions

- **Does the existing `narrowed`-dictionary / reduced-product architecture admit a Karr equality component without a fixpoint?** Requires a `ProofEngine.Intervals.cs` probe before a `/design` lock — the single-pass depth-1 envelope must accommodate equality propagation.
- **What is the right field-count cap (if any) for R2 hardening?** The O(n³) domains are safe only for small n; a hard reject-or-degrade threshold would make the worst-case data-independent. Needs a profiling pass against the corpus's largest precept.
- **Fixed-degree polynomial domain (Müller-Olm/Seidl degree-d extension) for products/ratios** — solver-free and polynomial in program size but O(k^{3d}) in variable count; whether any corpus demand justifies the d-exponent R2 cost is unresolved (no current constrained nonlinear invariant in the corpus).
- **Reduced-product reduction ordering and witness composition** — when intervals × octagons × Karr disagree on a field's bound, the reduction order and the combined witness shape (which domain's fact "wins" in the attribution) need a design decision to preserve §0.6 #6 legibility.
- **Integer-exact TVPI** — if an integer-exactness obligation appears, whether the documented strongly-polynomial approximation is precise enough for Precept's count/index reasoning, or whether the NP-complete integer hull is genuinely needed (in which case TVPI exits the R2-safe band).

## Sources

- **The Octagon Abstract Domain** — Antoine Miné — *Higher-Order and Symbolic Computation* 19(1):31–100, 2006, DOI 10.1007/s10990-006-8609-1 (author copy arXiv:cs/0703084). **Primary.** Octagon `±x±y≤c` form, O(n²) memory / O(n³) closure, widening-as-separate. Mirrored (in-tree, reused) to `research/references/octagon-domain/octagon-domain-mine-hosc2006-arxiv-cs0703084.txt`.
- **Precise Interprocedural Analysis through Linear Algebra** — Markus Müller-Olm, Helmut Seidl — POPL 2004, DOI 10.1145/964001.964029. **Primary.** Karr affine-equality domain: affine relation form `a₀+Σaᵢxᵢ=0`, linear-in-program/polynomial-in-vars complexity, vector-space/Gaussian witness. Mirrored to `…/karr-affine-relations-muller-olm-seidl-popl2004.txt`. Access 2026-06-06.
- **The Two Variable Per Inequality Abstract Domain** — Axel Simon, Andy King, Jacob M. Howe — *Higher-Order and Symbolic Computation* 23(1):87–143, 2010, DOI 10.1007/s10990-010-9062-8 (open-access copy kar.kent.ac.uk/30678). **Primary.** TVPI `a·x+b·y≤c` arbitrary-coeff ≤2-var, strongly-polynomial (rational), integral hull NP-complete, sweet-spot between octagon and polyhedra. Mirrored to `…/tvpi-two-variable-per-inequality-simon-king-howe-hosc2010.txt`. Access 2026-06-06.
- **Fast congruence closure and extensions** — Robert Nieuwenhuis, Albert Oliveras — *Information and Computation* 205(4):557–580, 2007, DOI 10.1016/j.ic.2006.08.009. **Primary.** Congruence closure O(n log n), proof-producing union-find / k-step explanation witness, EUF decision procedure. Mirrored to `…/fast-congruence-closure-nieuwenhuis-oliveras-ic2007.txt`. Access 2026-06-06.
- **The Parma Polyhedra Library: Toward a Complete Set of Numerical Abstractions** — Roberto Bagnara, Patricia M. Hill, Enea Zaffanella — *Science of Computer Programming* 72(1–2):3–21, 2008, DOI 10.1016/j.scico.2007.08.001. **Primary.** Convex polyhedra exponential worst-case (double description); watchdog/timeout mitigation falling back to bounded-difference (zones). Mirrored to `…/parma-polyhedra-library-bagnara-hill-zaffanella-scp2008.txt`. Access 2026-06-06.
- **Apron: A Library of Numerical Abstract Domains for Static Analysis** — Antoine Miné, Bertrand Jeannet — CAV 2009, LNCS 5643, DOI 10.1007/978-3-642-02658-4_52. **Primary.** Domain expressiveness lattice (intervals < octagons < linear-equalities/congruences < polyhedra); reduced product; Karr = "linear equalities … abstraction of polyhedra." Mirrored to `…/apron-domains-mine-jeannet-cav2009.txt`. Access 2026-06-06.
- **Variations on the Common Subexpression Problem** — Peter J. Downey, Ravi Sethi, Robert E. Tarjan — *J. ACM* 27(4):758–771, 1980, DOI 10.1145/322217.322228. **Secondary** (historical O(n log n) congruence-closure attribution; reached via search summary, load-bearing claim re-grounded from Nieuwenhuis-Oliveras). Access 2026-06-06.
- **Affine Arithmetic: Concepts and Applications** — Luiz Henrique de Figueiredo, Jorge Stolfi — *Numerical Algorithms* 37:147–158, 2004, DOI 10.1023/B:NUMA.0000049462.70970.b6 (and the Stolfi Affine Arithmetic Project pages, ic.unicamp.br). **Secondary.** Correlation-tracking `x₀+Σxᵢεᵢ` form; improves on interval dependency. Access 2026-06-06.
- **Construction of Abstract State Graphs with PVS** (predicate abstraction) — Susanne Graf, Hassan Saïdi — CAV 1997, LNCS 1254; and **Boolean and Cartesian Abstraction for Model Checking C Programs** — Thomas Ball, Andreas Podelski, Sriram K. Rajamani — TACAS 2001, LNCS 2031 (SLAM). **Secondary** (reached via search summaries). Predicate-abstraction abstraction step uses a prover → R1 caveat. Access 2026-06-06.
- **Static Analysis of Linear Congruence Equalities among Variables of a Program** — Philippe Granger — TAPSOFT 1991, LNCS 493, DOI 10.1007/3-540-53982-4_10; *Static analysis of arithmetical congruences*, Int. J. Computer Mathematics 30, 1989. **Secondary/Tertiary** (polynomial-complexity claim via search summary; low-priority recommendation). Access 2026-06-06.
- **In-tree, cited not re-surveyed:** `proof-engine-interval-arithmetic-survey.md` (SPARK/EVA/Astrée/LH/Dafny/CBMC/Infer); `relational-constraint-representation-survey.md` (octagon-closure-minus-widening C1, difference-logic Gap 2); `liveness-completability-verification-survey.md` (DBM-emptiness guard-feasibility); `fragment-boundary-corpus-validation-2026-06-05.md` (demand-side gap inventory); `bounds-only-constraint-enforcement-2026-06-05.md` + `interval-vs-value-evaluation-prior-art-2026-06-05.md` (interval/non-relational ceiling).
- **Precept canon:** `docs/language/precept-language-spec.md` §0.4 (no loops → no widening), §0.6 (#1 soundness, #2 proven-violations, #3 no-solver/legible, #6 attribution), §0.7; `docs/compiler/proof-engine.md` §4 (right-sizing: 0 external deps, determinism 100%), Strategies 4/8/9/10; `src/Precept/Pipeline/ProofEngine.Intervals.cs` (`RelationalHalfLine`, `BuildNarrowedIntervals`, single-pass depth-1). **Primary** (in-repo).
