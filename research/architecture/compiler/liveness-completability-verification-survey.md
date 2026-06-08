---
status: Cited
authored: 2026-06-05
author: research (architecture/compiler)
topic: Compile-time liveness / completability verification — workflow soundness, statechart liveness checks, model-checking liveness, decidability of guarded-transition completability without an opaque solver, and the honest direction of approximation for an existential "never-stranded" claim
external-engagement: strong
---

# Liveness & Completability Verification at Compile Time — Survey

> Can Precept guarantee, at compile time, that an entity is never *stranded* — every operation it offers in a reachable state is completable by some valid input, no reachable non-terminal state is *semantically* frozen, construction always succeeds — and can this be decided **without an opaque solver** (spec §0.6 #3 excludes SMT/Z3 on principle)? This survey grounds the **liveness row (G5b)** of `docs/Working/precept-guarantee-specification-strawman-2026-06-05.md` and the `/design` lock that will resolve its open decisions D2/D4/D5.

## Background

`docs/philosophy.md` line 51 already claims liveness in absolute terms — *"every non-terminal state has a path forward… no dead ends where an entity gets stuck with no way to advance"* (also lines 57, 61). The graph analyzer enforces the **structural** version (G5a): reachability (PRE0080), dead-end sinks, `AlwaysRejecting`/`StateAlwaysRejects`. But spec §0.5 explicitly **over-approximates** — *"Structural graph analysis treats all edges as traversable regardless of `when` guards… This is sound"* for the structural claim. So **semantic completability** — an entity stuck because *every* exit's guard ∧ post-mutation constraints are jointly unsatisfiable — is **not** checked, and philosophy line 51's absolute phrasing reaches past §0.5's honest structural-only scope. The strawman names this the frontier and asks (D4) whether the gap can be closed *without* an opaque solver, and (D2) at what severity.

This is a distinct *shape* of property from Precept's existing proofs. The shipped fault obligations are **universal** ("∀ inputs, no fault"); completability is **existential / reachability** ("∃ an input satisfying a transition's guard *and* the post-state constraints, in each reachable state"). The existing `state-graph-analysis-survey.md` covers the *structural* algorithms (BFS reachability, dead-end sinks, dominators); this survey covers the *semantic* layer those structural checks cannot see, and the decidability/cost question for the existential feasibility test underneath it.

## Methodology

- **Research question.** What do comparable systems check for liveness/completability, at what severity; is Precept's completability decidable in its fragment (intervals / difference-bounds / equality / strings / enums) *without* an opaque solver, and if only approximately, which direction of approximation stays *honest* for an existential "never-stranded" claim?
- **Search strategy.** Web (WebSearch/WebFetch June 2026) across: van der Aalst workflow-net soundness literature (Formal Aspects of Computing 2011 classification; LICS 2022 complexity; reset-arcs undecidability p464); Alpern & Schneider "Defining Liveness" (IPL 1985, Cornell PDF); SPARK/GNATprove dead-code & `--proof-warnings` docs (docs.adacore.com, learn.adacore.com); difference-bound-matrix emptiness (Wikipedia/HandWiki, arXiv SDBM); Liquid Haskell vacuity (ucsd-progsys, arXiv); itemis CREATE / Yakindu statechart validation (Eclipse Marketplace, itemis docs); UML/SCXML frozen-state and deadlock detection; abstract-interpretation over- vs under-approximation. Precept-internal: `docs/philosophy.md`, `precept-language-spec.md` §0.1/§0.4/§0.5/§0.6/§0.7, `GraphAnalyzer.cs`, `result-types.md`, the strawman.
- **Inclusion criteria.** Sources that (a) formally define a liveness/completability/soundness property over a state graph or guarded-transition system, (b) state its decidability/complexity, or (c) document where a real tool draws the solver-vs-cheap-analysis line for an unreachable/dead/frozen check. **Excluded:** general model-checking tutorials with no completability angle; runtime-only liveness monitors.
- **Source-grade mix.** Primary (peer-reviewed papers: LICS 2022, IPL 1985, vdAalst p464; authoritative vendor docs: AdaCore SPARK UG, itemis Marketplace listing). Secondary (Wikipedia DBM article, search-engine summaries of UML frozen-state work). One Tertiary fallback (ScienceDirect frozen-states paper — 403 at fetch, see Threats).
- **Time bounds.** All fetches 2026-06-05. Vendor-doc behavior (SPARK switches, itemis validations) is version-sensitive; versions cited where shown.

## Findings

### RQ1 — Workflow soundness (the closest analogue)

van der Aalst's **soundness** of workflow (WF-) nets is the precise formal analogue of "every offered operation completable / no frozen state / construction succeeds." The definition has three legs, mapping nearly 1:1 onto Precept's liveness claim:

**Classical soundness — the three requirements** [Primary; access 2026-06-05; mirrored to `research/references/liveness-completability/vdaalst-soundness-reset-arcs-undecidable-p464.pdf`]:

> "(1) option to complete: for [every reachable marking] … [the marking which just marks sink place o] ∈ R(N, M), (2) proper completion: if sink place o is marked all other places are empty for a given case, and (3) no dead transitions: it should [be possible to fire every transition in some reachable marking]."
> — van der Aalst et al., *Soundness of Workflow Nets with Reset Arcs is Undecidable!*, Def. 4 (ATPN 2009), p464.pdf

The mapping to Precept:
- **option to complete** ⇔ "no reachable non-terminal state is semantically frozen" + "every reachable state retains a path to a terminal/completion."
- **proper completion** ⇔ a clean terminal configuration (no stranded obligations) — partly subsumed by "option to complete" (the paper notes requirement 2 is implied by 1).
- **no dead transitions** ⇔ "every operation the system offers is completable by some valid input" (a transition that can fire in *no* reachable marking is exactly Precept's `AlwaysRejecting`/semantically-dead event).

Crucially, the LICS 2022 paper states soundness in **temporal-logic** form, which is *exactly* G5b's existential-inside-universal shape [Primary; access 2026-06-05; mirrored to `research/references/liveness-completability/blondin-mazowiecki-complexity-soundness-workflow-nets-LICS2022.pdf`]:

> "1-soundness can be loosely rephrased as i ⊨ ∀G ∃F f."
> — Blondin & Mazowiecki, *The complexity of soundness in workflow nets*, LICS 2022, §1 (arXiv:2201.05588)

`∀G ∃F f` = "in **all** reachable configurations (∀G), there **exists** a path to the final marking (∃F f)." That is precisely G5b: a *universal over reachable states* of an *existential over completions*. This is the canonical statement that completability is a reachability/existential property distinct from the safety (∀-no-fault) proofs Precept already does.

**Decidability and complexity.** For classical (bounded-token) WF-nets, soundness is **decidable** but **EXPSPACE-complete** [Primary; LICS 2022 abstract & §1]:

> "We … focus on three of its variants: classical, structural and generalised soundness. The first two are EXPSPACE-complete, and, surprisingly, the latter is PSPACE-complete, thus computationally simpler."
> — Blondin & Mazowiecki, LICS 2022, Abstract

> "we show that classical soundness and 𝑘-soundness are in fact both EXPSPACE-hard and in EXPSPACE, and hence EXPSPACE-complete."
> — ibid., §1

Decidability rests on the reduction to boundedness + liveness of a modified net [Primary; LICS 2022 §1]:

> "deciding classical soundness amounts to checking boundedness and liveness of a slightly modified net … classical soundness is decidable since boundedness and liveness are decidable problems."

**Which extensions break decidability.** Cancellation (reset arcs) over unbounded places makes soundness **undecidable** [Primary; p464.pdf]:

> "Theorem 1 (Undecidability of soundness). Soundness is undecidable for [reset WF-nets]."
> — vdAalst et al., p464.pdf, Thm. 1

> "Theorem 1 shows that the ability of cancellation combined with unbounded places makes soundness undecidable."
> — ibid., §6

**The lesson for Precept.** Soundness — Precept's exact liveness claim — is decidable *in general only at EXPSPACE cost*, and tips to *undecidable* the moment the model gains unbounded counters + cancellation. The EXPSPACE source is the **unbounded token count / concurrency** of Petri nets. Precept's lifecycle graph has **no token-marking state explosion**: one entity, one current state, a finite declared state set, no concurrent markings, no unbounded places. That is what moves Precept's *structural* completability question off the WF-net complexity curve entirely. The hard part for Precept is not the graph (finite, single-marking) — it is the **per-edge guard ∧ post-state feasibility test** (RQ4), which the WF-net abstraction folds into transition enabledness and does not model in data terms.

### RQ2 — Statechart / state-machine liveness checks

Statechart tooling **does** check no-dead/unreachable-states and frozen-state/deadlock, but splits sharply by *method*: cheap **structural** checks ship as live editor diagnostics; **semantic** "no state where all guards fail" requires model-checking or simulation.

**itemis CREATE / Yakindu — structural, live, warning+error** [Primary (vendor listing); access 2026-06-05]:

> "The validation of statecharts includes syntax and semantic checks of the complete state chart. Examples of validations are the detection of unreachable states, dead ends, and references to unknown events. These validation constraints are live checked during editing. In case a constraint is violated, this is visualized by warning and error markers, which are attached to the faulty model elements."
> — YAKINDU Statechart Tools, Eclipse Marketplace listing (marketplace.eclipse.org/content/yakindu-statechart-tools, accessed 2026-06-05)

This is **structurally** the same layer Precept already ships (G5a): "unreachable states, dead ends" detected over the declared graph, live, as warning/error markers — *not* a guard-satisfiability check. itemis adds a **deadlock/endless-loop** check only in the SCXML domain [Secondary; itemis release notes 3.5.0, accessed 2026-06-05]: *"added a detection for endless loops for invalid models that contain dead locks."*

**UML state machines — the semantic "frozen state" check needs model checking.** The literature defines a frozen/trap state precisely as "all outgoing guards mutually exclusive and unsatisfiable," and reaches it via formal verification or simulation, **not** a single-pass static rule [Secondary; web summaries, access 2026-06-05]:

> "A deadlock occurs when the system reaches a state where no outgoing transitions are possible, yet the state is not a final state… outgoing transitions can all be disabled simultaneously if their guards do not cover all possibilities."
> — go-uml.com, *Avoiding Deadlocks in UML State Diagrams* (accessed 2026-06-05)

> "If the conditions for all outgoing transitions from a state are mutually exclusive and never met, the state becomes a trap."
> — ibid.

> "formal verification techniques can mathematically prove that deadlocks are impossible by modeling the system as a finite automaton… Static analysis is complemented by simulation with various input sequences."
> — ibid.

**Takeaway for RQ2.** The *structural* dead-end / unreachable check is universally a **warning/error editor diagnostic** (itemis, SCXML validators) — Precept already matches this. The *semantic* "no frozen state where every guard is unsatisfiable" check is, in the statechart world, delegated to model checking (NuSMV/SPIN-class) or simulation — it is **not** offered as a cheap live diagnostic, because guard-satisfiability in general needs a solver. No surveyed statechart tool offers semantic completability as a free, solver-free, compile-time guarantee.

### RQ3 — Model checking liveness (the formal frame)

Alpern & Schneider's canonical definition pins exactly the property G5b states, and pins why it's the *dual* of safety [Primary; access 2026-06-05; mirrored to `research/references/liveness-completability/alpern-schneider-defining-liveness-IPL1985.pdf`]:

> "We now formalize liveness. A partial execution α is live for a property P if and only if there is a sequence of states β such that αβ ⊨ P. A liveness property is one for which every partial execution is live."
> — Alpern & Schneider, *Defining Liveness*, IPL 21(4) 1985, §3

> "The thing to observe about a liveness property is that no partial execution is irremediable: it always remains possible for the required 'good thing' to occur in the future."
> — ibid.

"Every partial execution extends to a satisfying one" is *verbatim* Precept's "no reachable state is stranded — from here there always exists a path forward." Note the directional asymmetry Alpern & Schneider make explicit, and which is load-bearing for RQ5:

> "Informally, a safety property stipulates that some 'bad thing' does not happen… a liveness property stipulates that a 'good thing' happens during execution."
> — ibid., §2–3

Safety = ∀-no-bad (Precept's fault proofs); liveness = ∃-good-extension (G5b). They are formally different property classes, decided by different machinery: safety by reachable-set over-approximation; liveness by cycle/extension analysis (e.g. SPIN's nested-DFS over a Büchi automaton — see `state-graph-analysis-survey.md`). The well-known cost of the liveness route is **state explosion**, which is why practical *languages* do not check liveness inline — it lives in dedicated verifiers (SPIN, NuSMV, UPPAAL), not in a single-pass compiler front-end.

### RQ4 — Decidability of guarded-transition completability WITHOUT a solver (the load-bearing one)

The existential feasibility test under G5b is: *does there exist an input vector satisfying (transition guard) ∧ (post-mutation field constraints/rules), within the reachable state's known field intervals?* The decisive question is which **fragment** that conjunction lives in.

**Difference-bound / interval fragment → polynomial, solver-free.** Precept's static facts are intervals (`min`/`max`) plus single-pass relational bounds (field-to-field `>=`/`>`/`<=`/`<`, octagon-style per the locked relational-rules design). That is *exactly* the difference-bound-matrix (DBM) fragment, whose **emptiness/satisfiability is decidable in polynomial time** by negative-cycle detection [Primary; access 2026-06-05; mirrored to `research/references/liveness-completability/dbm-wikipedia.md`]:

> "equations of the form x ≤ c, x ≥ c, x₁ ≤ x₂ + c and x₁ ≥ x₂ + c."
> — *Difference bound matrix*, Wikipedia (constraint class) (accessed 2026-06-05)

> "It suffices to apply the Floyd–Warshall algorithm to the graph… If this algorithm detects a cycle of negative length, this means that the constraints are not satisfiable."
> — ibid. (emptiness test)

The complexity is standard for Floyd-Warshall: **O(n³)** in the number of variables [Secondary; HandWiki/SDBM summaries, access 2026-06-05]: *"the satisfiability of DBM constraints can be determined in O(n³) time."* This is the same shape as Precept's **existing** empty-intersection contradiction check (`PRE0154`/`PRE0155` rule-consistency scans). **Completability over Precept's interval+difference-bound fragment is therefore feasible solver-free, in polynomial time, by the machinery the proof engine already owns** — single-pass, no fixpoint, no widening, fully compatible with §0.4.

**Where the solver line actually is.** Emptiness stays polynomial *only* while constraints remain difference-bounds + intervals. The line is crossed by: arbitrary linear arithmetic with ≥3-variable coupling (full Presburger — decidable but super-exponential), or DBMs extended with congruence/strided constraints, which tip to **NP-hard** [Secondary; SDBM, access 2026-06-05]: *"the SDBM satisfiability problem is NP-hard, so no polynomial-time algorithm is likely to exist."* General string/regex feasibility and mixed theories are the SMT province. Precept's fragment — intervals, single-pass difference bounds, equality, finite enums, fixed string literals — sits on the **cheap (polynomial) side** for the *guard∧constraint feasibility* test, provided the relational reasoning stays at the locked single-pass octagon depth and does not chase transitive third-field chains (§0.4).

**How refinement-type / contract tools detect the dual ("vacuous precondition / unreachable / dead code").** They split into a **flow-analysis** layer (cheap) and a **prover** layer (expensive), and — critically — those that detect it *semantically* **use the SMT solver** for it:

- **SPARK/GNATprove, flow-analysis layer** [Secondary; learn.adacore.com, access 2026-06-05]: *"SPARK can detect some cases of both unreachable and dead code through its precise construction of a dependency graph linking a subprogram's statements to all its inputs and outputs… This analysis might not be able to detect complex cases."* — cheap, structural, **warning** severity, **incomplete**.
- **SPARK/GNATprove, prover layer** [Primary; docs.adacore.com SPARK UG, access 2026-06-05]: *"GNATprove offers a switch, `--proof-warnings=on`, that uses proof to help identify unreachable branches and unreachable code and also to help identify subprogram contracts or assumptions that are always false."* This is the *semantic* version — and it **invokes the prover** (the UG notes it requires "calling a prover for each potential warning, which incurs a small cost"). It is **warning**, opt-in, and explicitly **incomplete**: *"The warnings issued by `--proof-warnings=on` are not guaranteed to be complete: an absence of warnings does not guarantee… an absence of dead branches or code."*
- **Liquid Haskell, vacuity** [Secondary; ucsd-progsys / arXiv, access 2026-06-05]: detecting that an `error` call is dead requires proving *"it happens under an inconsistent environment"* — i.e. the refinement environment is **UNSAT**, discharged by the **SMT solver**. *"if this check fails, Liquid Haskell generates a totality error."*

The pattern is consistent and decisive for Precept: **everyone who does *semantic* completability/vacuity at full generality uses the SMT solver** (SPARK `--proof-warnings`, Liquid Haskell), at **warning** severity, **incomplete**. The only solver-*free* dead/unreachable detection in the surveyed tools is the **structural flow-analysis** layer (SPARK's dependency graph; itemis's graph validation) — exactly the layer Precept already ships (G5a). The opening for Precept is that its data fragment is *narrower than these tools' fragments*: SPARK and Liquid Haskell must handle full arithmetic + arrays + user predicates (hence SMT), whereas Precept's guard∧constraint conjunction is interval+difference-bound, where emptiness is the polynomial DBM check. **Precept can do solver-free what SPARK/LH need SMT for — because its theory is the cheap fragment, not in spite of refusing SMT.**

### RQ5 — Severity & the honest direction of approximation

For a **safety** (∀) property, the sound abstraction **over-approximates** reachable states [Secondary; abstract-interpretation survey, access 2026-06-05]: *"Abstract interpretation computes an over-approximation of reachable states… it will at least include every state that is reachable… This over-approximation approach is sound for safety properties… the alarm may be spurious."* Precept's §0.5 is exactly this: over-approximate reachability, sound for the *structural* claim, may over-claim "reachable."

But G5b is an **existential / liveness** claim ("∃ a path forward"), and over-approximation flips from honest to **dishonest** there. The directions:

- **Over-approximating reachability/feasibility** → may falsely report a state as *completable* (claim a path forward that the real guards forbid). For a "**never stranded**" guarantee this is the **dangerous** error: it tells the author "no dead end here" when the entity *is* stranded. This is precisely the BUG-017 failure mode the strawman warns of — a detection-tool "skip to avoid false positives" leaking into a prevention contract.
- **Under-approximating completability** → may falsely report a completable state as *possibly frozen* (demand a witness the author must supply, or decline to certify liveness). This is the **honest** error for a prevention claim: it errs toward "I cannot prove you're safe," never toward a false "you're safe." It is the exact analogue of Precept's locked Proof-philosophy #1 (*"soundness over completeness… The language always chooses the safe direction"*) and #2 (*"proven violations only"*) — applied to the *existential* direction.

The literature names the existential-under-approximation idea directly: **danger invariants / must-analysis** under-approximate to *exhibit a real witness* before asserting an existential bug/path [Primary; arXiv:1503.05445 "Danger Invariants", access 2026-06-05 — title/abstract only]. The principle: for an existential claim, you certify it **only when you can produce a witness**; absent a witness, you decline — you never assume one exists.

**Severity, per surveyed precedent.** Every tool that checks completability/vacuity beyond the structural layer reports it as a **warning**, opt-in, explicitly incomplete (SPARK `--proof-warnings`, Liquid Haskell totality, itemis semantic markers). None promotes semantic completability to a *hard, complete, must-prove* gate — because at full generality it's either undecidable (RQ1 reset extension), EXPSPACE (RQ1 classical), or needs SMT (RQ4). The honest framings available to Precept are therefore three, ranked by what its fragment actually supports:

1. **Hard guarantee** — only defensible for the slice where the guard∧constraint conjunction is **decidable solver-free** (the interval+difference-bound DBM fragment) AND the result is read in the **under-approximating** direction (certify "completable" only with a witness; otherwise reject/decline). Within that slice, "prove-or-reject" is honest and matches the fault-obligation contract.
2. **Warning / best-effort lint** — the SPARK/LH/itemis precedent: emit "this state may be frozen / this offered operation may never be completable" when the cheap check is *inconclusive*, never blocking. This is what Precept's existing `AlwaysRejecting` warning already is for the structural case.
3. **Explicitly approximate** — state in the spec/philosophy that the liveness claim is **structural** (per §0.5) and that semantic completability is *checked where decidable, surfaced as a lint where not* — matching §0.5's existing honesty discipline ("approximation must be visible").

## Implications for Precept

1. **G5b's property shape is settled prior art.** It is workflow-net classical soundness (`∀G ∃F f`) and Alpern–Schneider liveness ("every partial execution extends"). Precept is not inventing a property; it is choosing how far to verify a well-studied one. The strawman's framing (existential-inside-universal, distinct from the ∀-safety proofs) is exactly right.

2. **The structural layer Precept already ships (G5a) is the universal practice.** itemis, SCXML validators, and SPARK's flow layer all stop at structural dead-end/unreachable detection as live warning/error diagnostics. Precept matches the field here; G5a is on solid, well-precedented ground.

3. **The semantic layer (G5b) is decidable solver-free in Precept's fragment — and only in Precept's fragment.** The guard∧post-constraint feasibility test is DBM emptiness (polynomial, negative-cycle), the same machinery as the existing `PRE0154`/`PRE0155` contradiction scans. This is the genuinely novel, defensible position: **Precept can do solver-free what SPARK/Liquid Haskell need SMT for, because its data theory is the cheap (interval+difference-bound) fragment.** The constraint is that this holds *only* while relational reasoning stays at the locked single-pass octagon depth (§0.4); transitive third-field chains, full linear arithmetic, congruences, or regex feasibility cross into NP-hard/SMT territory and must stay out of the G5b check.

4. **The honest direction is under-approximation.** For a "never stranded" promise, over-approximating feasibility (the §0.5 direction that's *sound for safety*) is *unsound* — it would falsely certify "path forward." G5b must certify "completable" **only with a witness** (an exhibited input satisfying guard∧constraints) and decline otherwise. This is the existential mirror of locked Proof-philosophy #1/#2 and the danger-invariant/must-analysis pattern.

5. **`Prospect {Certain, Possible, Impossible}` is the natural surface.** A solver-free DBM-feasible witness ⟹ `Possible` (a completion provably exists). An empty DBM for *every* offered exit ⟹ the state is provably frozen (`Impossible` for all events) — a real dead-end. Inconclusive (fragment exceeded) ⟹ the lint/`Possible`-without-witness case. This three-valued surface is exactly what lets G5b stay honest: it distinguishes "proven completable," "proven frozen," and "couldn't decide."

## Conclusions

### C1 — G5b is workflow-net soundness; verify it as such, scoped to Precept's non-Petri graph

- **Rationale.** The property is `∀G ∃F f` (LICS 2022) / "every partial execution extends" (Alpern–Schneider 1985) — a settled, named property, not novel surface. Precept's lifecycle graph lacks the unbounded-token concurrency that makes WF-net soundness EXPSPACE; it is a single-marking, finite-state graph, so the *structural* soundness question is cheap and the *only* hard part is per-edge data feasibility (RQ4).
- **Alternatives rejected.** *Treat G5b as a fresh Precept invention* — rejected: re-derives a 40-year-old result and risks getting the directional honesty wrong (the WF-net literature already pins it). *Model the lifecycle as a full Petri net* — rejected: imports EXPSPACE/undecidability (reset arcs) that Precept's single-entity model doesn't incur.
- **Precedent.** Blondin & Mazowiecki LICS 2022 (`∀G ∃F f`, EXPSPACE-complete classical); Alpern & Schneider IPL 1985 (liveness = every prefix extends); vdAalst p464 (reset extension undecidable).
- **Tradeoff accepted.** Committing to "soundness" framing means owning *proper completion* and *no dead transitions* as named obligations, not just "no dead-end sink" — a slightly larger surface than the current structural check.

### C2 — Semantic completability is decidable WITHOUT a solver in Precept's interval+difference-bound fragment

- **Rationale.** The existential feasibility test (guard ∧ post-constraints satisfiable within known intervals) is DBM emptiness — polynomial via negative-cycle detection — for the interval + single-pass difference-bound fragment Precept already reasons in. The proof engine already runs an empty-intersection contradiction scan; G5b's feasibility test is the same machinery turned existential. **No SMT needed**, §0.6 #3 respected, §0.4 single-pass respected.
- **Alternatives rejected.** *Use SMT* — rejected on principle (§0.6 #3) and unnecessary given the fragment. *Declare it undecidable / structural-only forever* — rejected: that's true for WF-nets with unbounded tokens and for SPARK/LH's full theories, but **false for Precept's narrower fragment**, where DBM emptiness suffices. Conflating "undecidable in general" with "undecidable for us" would forfeit a guarantee Precept can actually make.
- **Precedent.** DBM emptiness O(n³) negative-cycle (Wikipedia/HandWiki); SPARK flow-analysis dead-code detection without the prover; the contrast that SPARK `--proof-warnings` and Liquid Haskell vacuity need SMT *because their theories are richer than Precept's*.
- **Tradeoff accepted.** The guarantee holds *only* inside the fragment — guards/constraints that escape it (transitive 3-field chains, full linear arithmetic, congruence, regex feasibility) cannot be certified solver-free and must fall to the lint/decline path. The decidability is real but **bounded by the fragment**, and that boundary must be made visible (§0.5 honesty discipline).

### C3 — Honest severity: a HARD guarantee only on the under-approximating, witness-carrying, in-fragment slice; a WARNING (or explicit-approximate) elsewhere

- **Rationale.** For an existential "never stranded" claim, the *honest* error direction is under-approximation: certify "completable" only with an exhibited witness, decline otherwise — never over-claim a path forward (that's the BUG-017 failure mode). Inside the decidable fragment, "prove-or-reject" is honest and can be a **hard** guarantee. Outside it, no surveyed tool offers more than a **warning** (SPARK `--proof-warnings`, Liquid Haskell, itemis), and a hard error there would be either unsound (over-approx) or impossible (undecidable). The third option — state the claim **explicitly structural** with a semantic *lint* — matches §0.5's existing honesty.
- **Alternatives rejected.** *Hard, complete, must-prove liveness across the whole language* — rejected: undecidable/EXPSPACE/SMT-requiring at full generality; no precedent does it; would force either SMT (§0.6 #3 breach) or unsound over-approximation. *Over-approximate and call it sound (the §0.5 move)* — rejected for G5b: sound for *safety*, **unsound for an existential liveness claim** (would falsely certify "path forward"). *Pure warning everywhere, no hard guarantee even in-fragment* — rejected: leaves real, decidable, witness-backed dead-ends as ignorable lint, weaker than Precept's own structural construction-path `AlwaysRejecting`→Error promotion already is.
- **Precedent.** SPARK `--proof-warnings` (warning, opt-in, incomplete); Liquid Haskell totality (SMT-backed, warning-ish); itemis (warning+error markers, structural); abstract-interpretation over-approx = sound-for-safety-only; danger-invariants/must-analysis = under-approximate-with-witness for existential claims; Precept locked Proof-philosophy #1/#2.
- **Tradeoff accepted.** A *tiered* severity (hard in-fragment, warning out-of-fragment) is more nuanced than a single switch and must be derived from the guarantee class (strawman D3), not set case-by-case. And it forces a **philosophy reconciliation** (strawman D5): line 51's absolute "an entity gets stuck" must either scope to the structural-plus-in-fragment-semantic claim, or be acknowledged as aspirational where the fragment is exceeded. That is an owner decision, flagged not resolved here.

## What would change these conclusions

- **C2 falsifier.** If Precept's guard/constraint surface routinely needs feasibility checks *outside* intervals+difference-bounds (e.g. authors commonly write 3-way transitive numeric couplings, modular/congruence constraints, or regex-membership guards in transition `when` clauses), then the "polynomial DBM emptiness suffices" claim fails for the common case and G5b collapses toward the warning/SMT tier. A survey of real `.precept` guard shapes in `samples/` would test this.
- **C2/C3 falsifier.** If the locked single-pass octagon relational design turns out to *already require* transitive closure to be useful (i.e. depth-1 difference bounds are too weak to express real guards), the cheap-fragment boundary moves and the polynomial claim weakens.
- **C1 falsifier.** If Precept later admits a construct with WF-net-reset-arc character (unbounded counters + cancellation/"clear all" semantics — e.g. an unbounded collection field cleared by a transition), the structural completability question itself could tip toward undecidability, not just the data layer.
- **C3 falsifier.** If three or more comparable prevention-oriented systems are found that make semantic completability a *hard, complete, solver-free* guarantee across a full language (not just a fragment), the "warning is the universal practice beyond the fragment" claim is wrong and a harder G5b becomes defensible.

## Open Questions

- **Witness construction & surfacing.** A `Possible`-with-witness verdict needs the DBM feasibility check to *emit* the satisfying input (the corner of the zone), not just a boolean — what proof-attribution shape (§0.6 #6) carries it to hover/diagnostics? (Cross-ref `proof-attribution-witness-design-survey.md`.)
- **"No frozen state" as a per-state existential over events.** G5b's state-level leg is ∃ over the offered events *per reachable state* — does the analyzer iterate (state × event) feasibility, and how does that compose with the over-approximate structural reachability that feeds it (the reachable set is over-approx; the per-state feasibility is under-approx — the *composition's* honesty needs its own check)?
- **Construction-always-succeeds.** G2 (default validity) already gives "Create-from-defaults succeeds"; is "construction always succeeds" fully subsumed by G2, or does G5b add anything (e.g. construction under *non-default* host-supplied inputs)?
- **Exact fragment boundary for `when` guards.** A precise enumeration of which guard/constraint forms stay in-DBM vs escape — needed before C2's "in-fragment hard guarantee" can be turned into a diagnostic rule.

## Sources

- **Blondin, M. & Mazowiecki, F. — *The complexity of soundness in workflow nets*.** LICS 2022 / arXiv:2201.05588. **Primary.** Accessed 2026-06-05. Mirrored: `research/references/liveness-completability/blondin-mazowiecki-complexity-soundness-workflow-nets-LICS2022.pdf`. (Soundness = `∀G ∃F f`; classical/structural EXPSPACE-complete, generalised PSPACE-complete; decidability via boundedness+liveness.)
- **van der Aalst, W.M.P. et al. — *Soundness of Workflow Nets with Reset Arcs is Undecidable!*** ATPN 2009 / p464.pdf. **Primary.** Accessed 2026-06-05. Mirrored: `research/references/liveness-completability/vdaalst-soundness-reset-arcs-undecidable-p464.pdf`. (Def. 4 three legs: option to complete / proper completion / no dead transitions; Thm. 1 reset-arc undecidability.)
- **Alpern, B. & Schneider, F.B. — *Defining Liveness*.** Information Processing Letters 21(4), 1985. **Primary.** Accessed 2026-06-05. Mirrored: `research/references/liveness-completability/alpern-schneider-defining-liveness-IPL1985.pdf`. (Liveness = every partial execution extends to a satisfying one; safety/liveness dichotomy.)
- **AdaCore — SPARK User's Guide, §7.1 How to Run GNATprove (`--proof-warnings`).** docs.adacore.com/spark2014-docs (UG 27.0w). **Primary (vendor).** Accessed 2026-06-05. (`--proof-warnings=on` uses proof to find unreachable branches/code + always-false contracts; not guaranteed complete.)
- **AdaCore — *Detecting Unreachable Code and Dead Code* (SPARK for the MISRA C Developer, ch. 8).** learn.adacore.com. **Secondary (vendor tutorial).** Accessed 2026-06-05. (Flow-analysis dependency-graph detection; warnings; incomplete — "might not be able to detect complex cases.")
- **Difference bound matrix.** Wikipedia (+ HandWiki mirror). **Secondary.** Accessed 2026-06-05. Mirrored: `research/references/liveness-completability/dbm-wikipedia.md`. (Constraint class x≤c, x₁≤x₂+c; emptiness via Floyd-Warshall negative-cycle; O(n³).)
- **Strided Difference Bound Matrices.** arXiv:2405.11244. **Secondary.** Accessed 2026-06-05. (SDBM satisfiability NP-hard — the boundary where the cheap fragment ends.)
- **Liquid Haskell — vacuity / totality / dead-code via inconsistent (UNSAT) environments.** ucsd-progsys.github.io + arXiv:1711.03842 (Refinement Reflection). **Secondary.** Accessed 2026-06-05. (Dead `error` proven via inconsistent refinement environment, discharged by SMT.)
- **YAKINDU Statechart Tools / itemis CREATE — validation.** Eclipse Marketplace listing + itemis user-guide release notes 3.5.0. **Primary (vendor listing) / Secondary (release notes).** Accessed 2026-06-05. (Detection of unreachable states, dead ends; live during editing; warning+error markers; SCXML deadlock/endless-loop detection.)
- **UML state-machine deadlock/frozen-state characterization.** go-uml.com *Avoiding Deadlocks in UML State Diagrams* (+ search summaries of UML model-checking literature). **Secondary/Tertiary.** Accessed 2026-06-05. (Frozen/trap state = all outgoing guards mutually exclusive & unsatisfiable; detected via model checking or simulation, not single-pass static rule.)
- **Abstract-interpretation over- vs under-approximation; danger invariants.** *A Survey of Automated Techniques for Formal Software Verification* (USF) + arXiv:1503.05445 *Danger Invariants*. **Secondary/Primary (title-abstract).** Accessed 2026-06-05. (Over-approx reachable set sound for safety, spurious alarms; under-approx with witness for existential claims.)
- **Precept-internal.** `docs/philosophy.md` (lines 49–61); `docs/language/precept-language-spec.md` §0.1/§0.4/§0.5/§0.6/§0.7; `src/Precept/Pipeline/GraphAnalyzer.cs`; `docs/runtime/result-types.md`; `docs/Working/precept-guarantee-specification-strawman-2026-06-05.md`; sibling surveys `state-graph-analysis-survey.md`, `static-vs-runtime-expression-evaluation-survey.md`, `interval-vs-value-evaluation-prior-art-2026-06-05.md`. **Primary (internal canon).**

## Threats to Validity

- **Cornell PDF was scanned (CCITT fax); extracted via OCR-grade pdftotext.** The Alpern–Schneider excerpts are from the local text extraction of the mirrored PDF; OCR artifacts were visible in surrounding prose (e.g. "'good thing'" rendered with spacing noise). The two load-bearing quoted definitions ("A partial execution α is live…", "no partial execution is irremediable…") were legible and cross-checked against the abstract; risk is low but non-zero. The mirror is retained for re-verification.
- **ScienceDirect "Detection of Possible Frozen States in Communicating UML State Machines" returned 403** at fetch. The RQ2 frozen-state characterization rests instead on go-uml.com (Secondary) and search-engine summaries (Tertiary). The *conceptual* claim (frozen = all guards unsatisfiable; needs model-checking/simulation) is corroborated across multiple sources and matches the workflow-net "no dead transitions" leg, so it is robust even with the weaker citation — but the specific paper's method is not verbatim-confirmed.
- **itemis CREATE validation excerpt is from the Eclipse Marketplace listing**, not the live in-product user guide (the user-guide validation page 404'd). Marketplace text is vendor-authored and stable, but the *current* product may have evolved the validation set; the release-notes corroboration (SCXML deadlock detection) is from a specific dated release (3.5.0, 2019).
- **SDBM NP-hardness and the O(n³) DBM figure are Secondary** (HandWiki/summary), not lifted from the primary SDBM paper body (the arXiv HTML was not fetched in full). The polynomial-emptiness claim is independently standard (Floyd-Warshall) and the Wikipedia primary-constraint-class + negative-cycle excerpt is verbatim; the NP-hard *boundary* claim is the weaker-sourced of the two and is used only to mark where the cheap fragment ends, not as a load-bearing decidability result.
- **"Danger invariants" cited from title/abstract only** — used to name the under-approximation-with-witness pattern, not for a specific theorem. The directional-honesty conclusion (C3) rests primarily on Alpern–Schneider (Primary) + the abstract-interpretation over-approx-sound-for-safety result + Precept's own locked Proof-philosophy, so it does not hinge on this source.
- **Precept-fragment claim depends on the locked relational design holding at single-pass depth.** C2's "polynomial in Precept's fragment" assumes guards/constraints stay interval+difference-bound; this was reasoned from the spec (§0.4, relational-rules design) and not validated against a corpus of real `.precept` guard shapes. The C2 falsifier names this gap.
- **No primary-source benchmark** confirms the DBM feasibility check runs within Precept's stated compile-time budget (~44ms corpus / ~3ms worst file). The complexity is polynomial and the proof engine already runs the analogous contradiction scan, so the risk is low, but it is an inference, not a measurement.
