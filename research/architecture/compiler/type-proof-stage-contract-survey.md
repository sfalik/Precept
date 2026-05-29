---
status: Cited
authored: 2026-05-29
author: research (lifecycle-1)
topic: How separate-proof-stage systems structure the type-checker↔verifier contract — who generates proof obligations, how they cross the stage boundary, who emits diagnostics, the patterns beyond VC-generation, and the IDE/incremental consequences
external-engagement: strong
---

# Type-Checker↔Verifier Stage-Contract Survey

> In systems with a separate proof/verification stage after type checking, how is the type-checker↔verifier *contract* structured? Who generates proof obligations (front-end VC-generation vs back-end self-derivation), how are obligations communicated and attributed across the boundary, who emits diagnostics and when, what patterns exist beyond VC-generation, and what are the IDE/incremental consequences? This survey supplies precedent and trade-offs for Precept's assignment-qualifier-discharge-placement design and the broader type↔proof-contract direction. It does **not** pick Precept's direction — that is deferred to the owner-facing design pass.

## Background

Precept has a separate proof-engine stage after type checking (`docs/compiler/proof-engine.md`). The documented contract is specific and worth stating precisely, because the commissioning framing ("the type checker hands the proof engine nothing; the proof engine self-derives") is only half right:

- **Static channel — type checker stamps obligations.** `docs/compiler/proof-engine.md` Decision 3 ("Obligations Stamped by Type Checker, Not Identified by Proof Engine"): *"The proof engine reads `ProofRequirement` arrays stamped onto typed expressions by the type checker. It does NOT identify what needs to be proved."* The requirements originate in catalog metadata (`BinaryOperationMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`, `TypeAccessor.ProofRequirements`, `ActionMeta.ProofRequirements`); the type checker stamps them onto `TypedExpression`/`TypedAction` nodes during resolution.
- **Dynamic channel — proof engine walks actions and calls a catalog-supplied generator.** `ProofEngine.cs` `CollectObligations` → `WalkActions` invokes `ActionMeta.DynamicObligationGenerator` (a `Func<TypedAction, SemanticIndex, ImmutableArray<ProofObligation>>` field on the action's *catalog* entry — `Actions.cs:76`, `GenerateIntervalContainmentObligations`). This is the interval/length-containment-on-`set` obligation the framing calls "self-derived."

So Precept's true contract is **catalog-as-the-obligation-source with two delivery channels**: (1) static requirements stamped by the type checker onto typed nodes, (2) dynamic obligations produced by a catalog-supplied function the proof engine calls while walking the action chain. Neither channel hardcodes obligation knowledge in engine logic; both terminate in catalog metadata. The proof engine "self-derives" only in the weak sense of *invoking a catalog-provided generator during a tree walk* — not in the strong sense of *encoding domain knowledge in engine-side dispatch*.

The immediate decision this feeds: where open-field assignment-qualifier compatibility is checked. **B1 = self-derive** the assignment obligation in `WalkActions` (a sibling of the existing interval-containment-on-`set` dynamic obligation); **B2 = VC-handoff** (type checker generates/stamps the obligation explicitly, Dafny/Frama-C style). The broader question: should Precept stay with catalog-driven obligation sourcing, or move toward front-end VC-generation / an intermediate verification language (IVL) as a general type↔proof contract?

This file **extends** three in-tree surveys rather than restating them. It reuses their per-system mechanics and adds the *contract/handoff* axis they do not centrally answer, plus four systems they do not cover (Whiley, Viper/IVL, F*, OutsideIn(X)):

- `proof-attribution-witness-design-survey.md` — already documents SPARK/GNATprove, Dafny→Boogie, Frama-C/WP→Why3, Liquid Haskell, CBMC, and Rust borrowck *result/witness/attribution* shapes. This survey reuses those rows for the *who-generates / how-communicated / who-emits* axis.
- `flow-sensitive-check-placement-survey.md` — already documents Roslyn `NullableWalker`, Kotlin K2 CHECKERS, Rust MIR borrowck, TypeScript CFA *staging*. This survey reuses its diagnostic-staging finding directly.
- `compiler-pipeline-architecture-survey.md` — already documents rustc's demand-driven query system, Roslyn's four stages, K2's FIR phases. This survey reuses the query-system row for the demand-driven pattern.
- `research/architecture/README.md` open questions #1/#2 (formalize phases? defer diagnostics to a dedicated phase?) — this survey is additional evidence for both.

## Methodology

- **Research question** — see title. Per comparator, five sub-questions: (a) *who* generates proof obligations — front-end (type checker / VC-gen) or back-end (a walk over the typed tree); (b) *how* obligations cross the stage boundary (stamped annotations, a separate assertion/IVL artifact, in-memory constraint set); (c) *who* emits diagnostics and at *what* stage; (d) what *pattern* it exemplifies; (e) IDE/incremental story.
- **Search strategy** — triaged the three in-tree surveys + the architecture README first (they cover the *mechanics* of SPARK/Dafny/Frama-C/LH/CBMC/Rust/Roslyn/K2/TS and rustc queries; this survey does not re-fetch those). Then fetched primary/secondary sources for the four gap systems: Whiley (Pearce's whileydave.com architecture posts + WhileyTheoremProver README), Viper (ETH PM group project page), Why3 (why3.org), F* (fstar-lang.org), OutsideIn(X) (Peyton Jones JFP-2011 landing page + abstract). Grounded Precept's actual contract by reading `docs/compiler/proof-engine.md` (Decisions 3–5, obligation-instantiation §, `ProofRequirement` DU) and `src/Precept/Pipeline/ProofEngine.cs` (`CollectObligations`/`WalkActions`) + `src/Precept/Language/Actions.cs`/`Action.cs` (`DynamicObligationGenerator`).
- **Inclusion criteria** — systems with (a) a distinct type/binding stage AND (b) a downstream stage that decides a *correctness obligation* (verification condition, refinement subtyping, borrow safety, runtime-safety check) the base type stage did not itself decide. VC-based verifiers, refinement-type checkers, the borrow checker, nullable flow analysis, and constraint-based HM inference all qualify.
- **Exclusion criteria** — the witness-serialization angle (covered in `proof-attribution-witness-design-survey.md`); the *narrowing-mechanism* angle (covered in `context-sensitive-literal-typing-survey.md`); pure single-stage checkers with no downstream obligation stage (Go `go/types`, CEL, Dhall — they decide everything in the type checker).
- **Source-grade mix** — Primary for Viper, Why3, F*, OutsideIn(X) (authoritative project pages / peer-reviewed paper), the in-tree primaries reused (rustc-dev-guide, Roslyn/K2 docs, GNATprove/Dafny/Frama-C docs), and the WhileyTheoremProver README. Secondary for the two Whiley architecture blog posts (named author = the language designer, but blog-grade not peer-reviewed). One Tertiary: the F* "VC-gen is part of the type system, not a separate stage" reading is inferred from the Dijkstra-monad paper *titles* on the F* home page rather than a verbatim architecture statement — flagged in Threats.
- **Time bounds** — fetched 2026-05-29. External docs track moving project pages; load-bearing excerpts mirrored to `research/references/type-proof-stage-contract/source-excerpts.md`.

## Findings

### Comparator table

| System | (a) Who generates obligations | (b) Cross-stage communication mechanism | (c) Who emits diagnostics + at what stage | (d) Pattern | (e) IDE / incremental story |
|---|---|---|---|---|---|
| **Whiley** | **Front-end VC-generator** — a path-sensitive traversal of the typed WyIL produces VCs. | **Separate artifact**: WyIL (typed IR) → WyAL (assertion language) `.wyal`/`.wycs` file. VCs are *implications* (assumptions ⟹ assertion). | **Verifier (WyTP / Boogie)** emits, at the verification stage, after type checking already passed. | **Front-end VC-generation** (closest structural analog to Precept). | Whole-function VC generation; per-function granularity supports modular re-verification. |
| **Dafny → Boogie** | **Front-end** — Dafny lowers the resolved/typed program to Boogie IR; Boogie generates per-procedure VCs. | **IVL**: Dafny → Boogie program → SMT-LIB2 to Z3. | **Dafny** maps SMT failures back to source ("postcondition might not hold on path…") at the verification stage; LSP `publishDiagnostics`. | **IVL (Boogie)**. | LSP server; per-method verification; caching of verification results. |
| **Frama-C / WP → Why3** | **Front-end** — WP computes weakest preconditions for each ACSL annotation → "goals". | **IVL**: WP goals → Why3 session → SMT/Coq. | **Frama-C/WP** reports goal status (Valid/Unknown/Invalid) per ACSL annotation at the WP stage. | **IVL (Why3)** + weakest-precondition VC-gen. | Ivette GUI per-goal status; `why3session.xml` replay = incremental proof reuse. |
| **SPARK → Why3** | **Front-end** (GNAT2why) — translates SPARK to Why3; Why3 VC-gen. | **IVL**: SPARK → Why3 → SMT (CVC5/Z3/Alt-Ergo). | **GNATprove** emits per-check messages + SARIF at the proof stage; flow analysis and proof are distinct sub-stages. | **IVL (Why3)**. | `why3session.xml` + memcached proof caching across runs/team = strong incremental story. |
| **F\*** | **Front-end, fused into the type checker** — the type-and-effect system (Dijkstra monads) computes WP-style VCs *as part of typing*. | **In-process**: VCs handed to Z3 during type checking; no separate user-facing IR artifact. | **The type checker** emits, during typing — VC discharge *is* type checking. | **VC-generation fused with typing** (degenerate "separate stage"). | Interactive; SMT query caching; `--admit_smt_queries` for fast iteration. |
| **Liquid Haskell** | **Back-end plugin** — intercepts the GHC-typed AST and *generates Horn-clause constraints* per refinement check. | **In-memory constraint set** → `liquid-fixpoint` → SMT. | **LH plugin** emits GHC-style "Liquid Type Mismatch" diagnostics, after GHC type-checks. | **Constraint-collection-then-solve** (refinement flavor). | Runs inside GHC/HLS type-check; per-module. |
| **Rust borrowck** | **Back-end pass** — a walk over MIR self-derives init/borrow obligations from the IR ("we see `*a+1`, so we check `a` is initialized and not mutably borrowed"). | **Shared IR (MIR)** in-memory; no separate obligation artifact. | **Borrow checker** emits in a dedicated final MIR reporting walk, after HIR typeck. | **Self-derivation by walking a flow-aware IR**. | rustc salsa-style query system: `mir_borrowck(def_id)` is a cached query → strong incremental. |
| **Roslyn nullable** | **Back-end walk** — `NullableWalker` self-derives null-state warnings from bound nodes. | **In-memory** walk over the bound tree; no separate artifact. | **The flow walk** emits W-warnings, after binding (not the binder). | **Self-derivation by walking the typed/bound tree**. | Needs whole-method binding; per-method flow analysis; IDE recomputes per edited method. |
| **Kotlin K2 / FIR** | **Resolution phase** detects, **CHECKERS phase** reports — obligations stashed in the FIR tree mid-resolution. | **In-tree annotations** on FIR nodes (errors stashed until CHECKERS). | **CHECKERS phase only** — "it's allowed to report diagnostics only in this phase." | **Staged diagnostics** (orthogonal to who generates). | Per-file FIR; lazy resolution phases. |
| **GHC OutsideIn(X)** | **Constraint generator** walks the term producing constraints (incl. implications). | **In-memory constraint set** handed to a domain-parametric solver X. | **The solver** surfaces unsolved/contradictory constraints as type errors, after generation. | **Constraint-collection-then-solve** (HM(X) lineage). | Whole-module; constraint solving per binding group. |
| **Precept (current)** | **Hybrid, both catalog-sourced**: (1) type checker *stamps* catalog `ProofRequirement`s onto typed nodes; (2) proof engine `WalkActions` calls a *catalog-supplied* `DynamicObligationGenerator`. | **(1) stamped annotations** on `TypedExpression`/`TypedAction` carried in `SemanticIndex`; **(2) in-memory** obligations built during the action walk. | **Proof engine** emits, at the proof stage (with the documented PRE0141 type-stage / PRE0114 proof-stage split for the specific qualifier case). | **Catalog-driven obligation sourcing** — a stamping-style front-end channel *plus* a walk-the-tree back-end channel, both grounded in catalog metadata. | Batch today; no query/incremental layer (README open Q#1). |

### Detail per gap system (the four not in prior surveys)

**Whiley — the closest structural analog: front-end VC-generation into a separate assertion artifact** [Secondary (named designer); accessed 2026-05-29; mirrored]:

Whiley deliberately separates type checking from verification and generates VCs in the front-end, handing a *separate artifact* to an interchangeable prover. The pipeline is Whiley source → WyC (type-checks, emits typed WyIL) → WyAL (verification conditions) → WyCS/WyTP/Boogie (discharge):

> "To verify a Whiley source file is correct, it is first translated into a WyIL file, then into a WyAL file which, finally, is verified as correct (or not) by the WyCS theorem prover."
> — Pearce, "The Architecture of Verification in Whiley" (2013), accessed 2026-05-29.

VC generation is a path-sensitive traversal that builds implications:

> "the VC generator performs a path-sensitive traversal accumulating assumptions" … "to construct the VC, we combine the assumptions and assertion together using `==>` (i.e. an implication) to give our final verification condition."
> — Pearce, "Generating Verification Conditions for Whiley" (2012), accessed 2026-05-29.

> "A verification condition is a logical expression which, if proved to be satisfiable, indicates an error in the program."
> — ibid.

The prover is a *separate program* discharging *generated* VCs:

> "The Whiley Theorem Prover (WyTP) is an automatic and interactive theorem prover designed to discharge verification conditions generated by the Whiley Compiler."
> — WhileyTheoremProver README (accessed 2026-05-29).

This is the canonical **front-end VC-generation** shape (option B1's opposite, B2's exemplar): a dedicated VC-generator walks the typed IR and emits a separate assertion artifact that a swappable back-end discharges. The "nice separation of concerns" Whiley cites is exactly the B2 argument.

**Viper / Silver — the IVL pattern as a reusable substrate** [Primary; accessed 2026-05-29; mirrored]:

Viper is an *intermediate* verification language: front-end languages translate *into* Viper, and Viper's back-ends discharge.

> "The Viper toolset can be used to implement verification techniques for front-end programming languages via translations into the Viper language."
> — ETH PM group, Viper project page (accessed 2026-05-29).

Two back-ends — a Boogie-translation verifier and a symbolic-execution verifier that "uses the Z3 SMT solver to discharge logical queries." Multiple independent front-ends target it: Gobra (Go), Nagini (Python), Prusti (Rust), Chalice. This is the **IVL** pattern in its purest reusable form — the front-end's only job is to *translate to the IVL*; obligation generation and discharge are the IVL toolchain's job.

**Why3 — the other dominant IVL, shared by SPARK and Frama-C** [Primary; accessed 2026-05-29; mirrored]:

> "It provides a rich language for specification and programming, called WhyML, and relies on external theorem provers, both automated and interactive, to discharge verification conditions."
> — why3.org (accessed 2026-05-29).

WhyML is used "as an intermediate language for the verification of C, Java, or Ada programs." Both SPARK (via GNAT2why) and Frama-C/WP target Why3 — so the *same IVL* backs two independent front-ends, the same multiplexing benefit Viper shows. This is why the in-tree `proof-attribution` survey saw `why3session.xml` in *both* the SPARK and Frama-C rows: it's the shared Why3 substrate.

**F\* — VC-generation fused into the type-and-effect system** [Primary for the SMT framing; Tertiary for the "not-a-separate-stage" reading — see Threats; accessed 2026-05-29; mirrored]:

> F* "combines the expressive power of dependent types with proof automation based on SMT solving and tactic-based interactive theorem proving."
> — fstar-lang.org (accessed 2026-05-29).

F* computes weakest-precondition VCs via Dijkstra monads *as part of typing* (the "Dijkstra Monads for Free/All" line). This is the degenerate boundary case: there *is* VC-generation, but it is *fused with* type checking rather than a separate downstream stage — the type checker itself calls Z3. It marks one end of the design axis (maximal fusion), opposite Whiley's maximal separation.

**GHC OutsideIn(X) — constraint-collection-then-solve** [Primary; accessed 2026-05-29; mirrored]:

> "OutsideIn(X) is parameterised over the particular underlying constraint domain X, in the same way as HM(X)."
> — Vytiniotis, Peyton Jones, Schrijvers, Sulzmann, JFP 21 (2011), <https://simon.peytonjones.org/outsideinx/>.

A constraint *generator* walks the term producing constraints (including implication constraints for local assumptions / GADTs); a separate *solver* for the domain X canonicalises and simplifies them. "This constraint solver has been implemented and distributed as part of GHC 7." This is the classic **constraint-collection-then-solve** pattern (Hindley-Milner / HM(X) lineage): the front-end's output to the back-end is a *constraint set*, not annotations or an IVL program. Liquid Haskell is the refinement-typed instance of the same shape (Horn clauses → liquid-fixpoint).

## Patterns enumerated and weighed

The six patterns the deliverable asks for, each with mechanism, ≥1 exemplar, and trade-offs across separation-of-concerns / diagnostic attribution / IDE incrementality / implementation cost.

**1. Front-end VC-generation + discharge.** *Mechanism*: a VC-generator walks the typed program and produces logical obligations handed to a prover. *Exemplars*: Whiley, Dafny, Frama-C/WP, SPARK. *Trade-offs*: strongest separation-of-concerns (swappable provers); the front-end owns source attribution because *it* generated the VC (Dafny's "postcondition might not hold on path…" maps the SMT failure back). Cost: a real VC-generator + a logical IR + an obligation-to-source map. IDE: per-procedure VCs cache well.

**2. Intermediate verification language (IVL).** *Mechanism*: lower to a shared IR (Boogie / Why3 / Viper) that a back-end discharges; a deeper commitment than ad-hoc VCs because the IVL is a stable, *reusable, multi-front-end* substrate. *Exemplars*: Viper (Gobra/Nagini/Prusti), Why3 (SPARK + Frama-C), Boogie (Dafny). *Trade-offs*: maximum reuse and prover-ecosystem leverage; but two translation hops (front-end → IVL → SMT) and the attribution problem is *harder* (failures surface in IVL terms and must be mapped back two levels). Highest implementation cost; only pays off with multiple front-ends or to inherit a mature prover stack.

**3. Catalog-driven self-derivation by walking the typed tree.** *Mechanism*: a back-end pass walks the typed/flow IR and derives obligations from per-node metadata, with no front-end hand-off of a logical artifact. *Exemplars*: **Rust borrowck** (derives init/borrow checks from MIR), **Roslyn nullable** (`NullableWalker` derives null-state from bound nodes), abstract interpreters generally. **Precept's dynamic channel is this shape** — `WalkActions` deriving interval-containment-on-`set`. *Trade-offs*: lowest hand-off cost (no IVL, no VC artifact); the deriving pass owns attribution naturally (it knows the node it's standing on); but obligation knowledge must live *somewhere* — Rust/Roslyn put it in pass logic, **Precept puts it in catalog metadata + a catalog-supplied generator function**, which is the cleaner variant. IDE: composes well with query systems (Rust's `mir_borrowck` query).

**4. Demand-driven / query-based.** *Mechanism*: checks are cached queries recomputed only when inputs change; orthogonal to *who* generates — it's about *when/how often*. *Exemplar*: rustc salsa (`mir_borrowck(def_id)` query), Swift request-evaluator. *Trade-offs*: the strongest IDE/incremental story by far; cost is the query-graph infrastructure and disciplined input-tracking. Composes with patterns 1–3 (Rust layers it over self-derivation).

**5. Constraint-collection-then-solve.** *Mechanism*: front-end emits a *constraint set*; a domain solver decides satisfiability. *Exemplars*: HM / OutsideIn(X) (GHC), Liquid Haskell (Horn clauses → liquid-fixpoint), CUE (lattice unification, per `compiler-pipeline-architecture-survey.md`). *Trade-offs*: clean separation between generation and solving; attribution is the classic pain (a contradiction is a *set* property, so blaming one source location is heuristic — the well-known "constraint solver error message" problem). Solver is reusable across constraint shapes.

**6. Staged diagnostics.** *Mechanism*: defer *emission* to a dedicated terminal phase regardless of where the obligation was decided. *Exemplar*: Kotlin K2 CHECKERS ("allowed to report diagnostics only in this phase"); Rust's final MIR reporting walk; Roslyn nullable W-warnings. *Trade-offs*: clean reporting against fully-resolved state; bookkeeping cost (stash errors in the tree until the phase). Orthogonal to patterns 1–5 — any of them can stage emission.

## Threats to Validity

- **F\* "fused-not-separate-stage" reading is Tertiary.** The claim that F* generates VCs *as part of typing* rather than in a separate downstream stage is inferred from the Dijkstra-monad paper *titles* and the SMT-automation framing on the F* home page, not a verbatim architecture statement. If the implementation in fact has a distinct post-typeck VC-generation stage, the F* row and the "VC-gen fused with typing" boundary-case claim need revising. Load-bearing only for the *edges* of Conclusion 1's spectrum, not its core split.
- **Whiley sources are Secondary (blog), not peer-reviewed.** The two architecture posts are by the language designer (high credibility) but are blog-grade; the WhileyTheoremProver README (Primary) corroborates the central "VCs generated by the compiler, discharged by a separate prover" claim, which is the only load-bearing Whiley fact.
- **Selection bias toward known verifiers.** The comparator set was seeded from the commissioning brief + the two prior in-tree surveys, not an exhaustive enumeration of separate-proof-stage systems. Systems like Viper-front-ends beyond the four named, KeY (Java), or VeriFast were not separately surveyed; if several of them self-derive rather than VC-generate, Conclusion 1's "dedicated verifiers favor front-end VC-gen" leg weakens.
- **Reused-survey recency.** The SPARK/Dafny/Frama-C/Rust/Roslyn/K2 mechanics are reused from `proof-attribution-witness-design-survey.md` and `flow-sensitive-check-placement-survey.md` (both fetched 2026-05-29 / on file); this survey did not re-fetch them, so any drift in those upstream docs since their survey dates is inherited.
- **Precept-internal grounding is code+doc, not behavioral.** The "where Precept sits" characterization rests on reading `proof-engine.md` Decision 3 + `ProofEngine.cs`/`Actions.cs`, not on running the pipeline. The doc is status-Implemented for these decisions, but a doc/code drift would shift the static-vs-dynamic-channel framing.

## Implications for Precept

**Where Precept's model sits.** Precept's contract is a **hybrid catalog-driven model** that does not map cleanly onto a single pattern. Its *static channel* (type checker stamps catalog `ProofRequirement`s onto typed nodes, proof engine reads them) is structurally a *lightweight front-end annotation hand-off* — the same idea as pattern 1's "front-end attaches the obligation," but the obligation is a declarative catalog record rather than a generated logical formula, and the discharge is a fixed strategy set rather than an SMT call. Its *dynamic channel* (`WalkActions` invoking a catalog-supplied `DynamicObligationGenerator`) is pattern 3 (self-derivation by walking the typed tree) — but the cleaner *catalog-metadata* variant rather than the Rust/Roslyn *pass-logic* variant. So:

- **Is catalog-driven self-derivation precedented for a separate-proof-stage language?** *Yes, the walk-the-typed-tree shape is well-precedented* — Rust borrowck and Roslyn nullable both derive correctness obligations by walking a typed/flow IR in a post-typeck pass, exactly Precept's dynamic channel. What is *less* precedented (and is Precept's distinctive contribution) is grounding the derived obligations in **catalog metadata + a catalog-supplied generator** rather than in pass-internal logic — that variant is closest to nothing in the survey, because the surveyed self-derivers (Rust, Roslyn) hardcode the obligation knowledge in the pass, exactly the smell Precept's catalog system exists to avoid.
- **Do comparable systems overwhelmingly favor front-end VC-generation / IVL?** *The dedicated-verifier systems do; the type-system-integrated checkers do not.* This is the key neutrality point: the comparator space splits cleanly. Systems whose *primary purpose is verification* (Whiley, Dafny, Frama-C, SPARK, F*, Viper consumers) overwhelmingly use front-end VC-gen and frequently an IVL. Systems where the check is *one analysis among many in a general compiler* (Rust borrowck, Roslyn nullable, K2) overwhelmingly self-derive by walking a typed/flow IR. Precept is architecturally the *latter kind of system* (a general DSL pipeline with proof as one stage), not a *dedicated verifier* — which is precedent-consistent with its current self-derive-ish model. But its *ambition* (prevention-as-structural-guarantee) is the *former kind*. That tension is the real design question, and it is the owner's to resolve.

**What VC-generation / IVL would buy or cost over the current model, as a GENERAL contract:**
- *Buys*: clean separation-of-concerns and prover-swappability (Whiley's stated reason); a path to richer obligations than the fixed six-strategy set; an attribution discipline forced by generating an explicit obligation. An IVL specifically buys a mature external prover ecosystem.
- *Costs*: an IVL contradicts `proof-engine.md`'s explicit rejection of general SMT solving (*"General SMT solving (Z3, CVC4/5) would add non-deterministic verification times, external dependencies, and implementation complexity"*) — adopting Why3/Viper/Boogie reintroduces exactly the non-determinism and external dependency Precept rejected for determinism. Front-end VC-gen without an IVL is cheaper but still adds a logical-IR layer and a generation pass the catalog model currently folds into stamping + strategies. The two-level attribution problem (pattern 2) is a real diagnostic-quality cost the catalog model avoids today (Precept's obligations attribute directly to the `TypedExpression` site).

**The B1-vs-B2 sub-question (assignment-qualifier check).** The survey gives precedent for *both*, and the choice is consistent within Precept's existing model either way:
- **B1 (self-derive in `WalkActions`)** is precedented by Rust/Roslyn (pattern 3) *and* is the natural sibling of the existing `DynamicObligationGenerator` for interval-containment-on-`set`. It keeps the assignment obligation in the same channel as the closest existing analog (containment-on-`set`), and attributes directly to the action site. It is the lower-disruption choice and the one most consistent with "catalog-supplied generator on `ActionMeta`."
- **B2 (type-checker stamps the obligation)** is precedented by Whiley/Dafny (pattern 1) *and* is the natural sibling of the existing static-stamping channel (Decision 3). It is the right choice *if* the compatibility decision needs resolution context the type checker has and the proof engine would have to recompute.

The deciding factor is *which channel already holds the information the check needs* — a reassignment-invalidation/qualifier-narrowing question about Precept's substrate, not a precedent question. The survey supplies precedent for each; it does **not** pick B1 vs B2. That, and the broader stay-self-derive-vs-adopt-VC-gen/IVL direction, are **deferred to the owner-facing design pass**.

**Connection to README open questions #1/#2.** Open Q#2 ("defer diagnostics to a dedicated phase?") is answered "yes, dominantly" by pattern 6: K2, Rust, and Roslyn all decouple emission from decision, and Precept's existing PRE0141/PRE0114 split already does this for the qualifier case — the survey supports generalizing it. Open Q#1 ("formalize phases / incremental?") connects to pattern 4: the systems with the best IDE story (Rust, Swift) layer a demand-driven query system over *whatever* obligation pattern they use — so the incremental question is *orthogonal* to the B1/B2 and self-derive/VC-gen questions and can be decided independently.

## Conclusions

**Conclusion 1 — The contract structure splits by system *purpose*: dedicated verifiers favor front-end VC-generation (often via an IVL); general compilers with a proof/flow stage favor back-end self-derivation by walking a typed IR.**

- *Rationale*: Every surveyed dedicated-verification system (Whiley, Dafny, Frama-C/WP, SPARK, F*) generates obligations in/near the front-end and hands a logical artifact (VCs, IVL program) to a discharge back-end; every surveyed general-compiler analysis (Rust borrowck, Roslyn nullable, K2) self-derives obligations by walking a typed/flow IR with no logical hand-off. The split is clean and tracks *whether verification is the product or one analysis among many*.
- *Alternatives considered and rejected*: "one dominant pattern across the field" — rejected; the evidence shows two stable clusters, not one winner. Pre-favoring either (the neutrality risk) would misrepresent the bimodal evidence.
- *Precedent*: Whiley/Dafny/Frama-C/SPARK/F* (front-end VC-gen / IVL); Rust/Roslyn/K2 (self-derivation) — all quoted above and in the two reused in-tree surveys.
- *Tradeoff accepted*: the conclusion describes the field's split, not Precept's choice; it deliberately stops short of saying which cluster Precept *should* join.

**Conclusion 2 — Catalog-driven self-derivation by walking the typed tree is well-precedented as a shape, and Precept's catalog-metadata variant is a cleaner-than-precedent instance of it.**

- *Rationale*: Rust borrowck and Roslyn nullable establish "walk the typed/flow IR and derive obligations in a post-typeck pass" as a mainstream, production pattern for separate-stage correctness checks. Precept does the same walk but sources the obligation knowledge from catalog metadata + a catalog-supplied generator (`ActionMeta.DynamicObligationGenerator`) rather than pass-internal logic — strictly less domain knowledge in engine code than the precedents carry.
- *Alternatives considered and rejected*: "self-derivation is unusual / unprecedented for a separate-proof-stage language" — rejected; it is precisely how two of the most-used production compilers implement their separate flow/borrow stage. The *catalog-metadata* refinement is the only part with thin precedent.
- *Precedent*: rustc-dev-guide MIR borrowck ("we see `*a+1`, so we check `a` is initialized and not mutably borrowed"); Roslyn `NullableWalker` (both quoted in `flow-sensitive-check-placement-survey.md` / `proof-attribution-witness-design-survey.md`).
- *Tradeoff accepted*: self-derivation forgoes prover-swappability and the richer obligation language an IVL would bring; the survey notes this cost without weighing it for Precept.

**Conclusion 3 — Diagnostic staging (defer emission to a dedicated phase) and incrementality (demand-driven queries) are orthogonal to who generates obligations, and each is independently well-precedented.**

- *Rationale*: K2/Rust/Roslyn defer *emission* regardless of how obligations arise (pattern 6); Rust/Swift layer a query system over their obligation pattern for incrementality (pattern 4). Neither is coupled to the front-end-vs-back-end-generation choice — so Precept can decide README Q#1/Q#2 independently of the B1/B2 and self-derive/VC-gen questions.
- *Alternatives considered and rejected*: treating "where the check computes," "when diagnostics emit," and "how incremental it is" as one bundled decision — rejected; the survey shows they vary independently across the comparators (K2 computes-in-resolution but emits-in-CHECKERS; Rust self-derives but queries; Whiley front-end-generates but is batch).
- *Precedent*: Kotlin K2 CHECKERS phase; rustc salsa queries + final MIR reporting walk; Swift request evaluator (all in the reused in-tree surveys).
- *Tradeoff accepted*: staging adds bookkeeping (stash-until-emit); queries add graph infrastructure — both real costs the survey flags but does not weigh for Precept.

## What would change these conclusions

- If two or more *dedicated verification* systems are found that **self-derive obligations by walking a typed tree with no front-end VC artifact** (e.g. a verifier that runs purely as a post-typeck abstract interpreter without generating VCs), Conclusion 1's clean purpose-based split weakens.
- If two or more *general compilers* are found that **lower a non-verification flow check to a full IVL** (Boogie/Why3/Viper) rather than self-deriving, Conclusion 1's "general compilers self-derive" leg weakens.
- If reading the F* implementation (not the paper titles) shows VC-generation is in fact a *separate post-typeck stage* rather than fused with typing, the F* row and the "VC-gen fused with typing" boundary-case claim need revising (this rests on a Tertiary inference — see Threats).
- If a surveyed self-deriving compiler is found to ground its derived obligations in *declarative metadata* the way Precept's catalog does, Conclusion 2's "cleaner-than-precedent" leg loses its novelty claim (it would then be precedented, not distinctive).

## Open Questions

- The survey establishes the *contract shape* per pattern but does not measure the *attribution-quality delta* between self-derivation (direct site attribution) and IVL (two-level mapping) on realistic programs — only notes the IVL mapping is harder in principle.
- Whether Precept's proof stage already holds the reassignment-invalidation / qualifier-narrowing substrate the assignment-compat check needs (the B1/B2 deciding factor) is an implementation-state question for the design pass, not a comparator question.
- Whether a *bounded* front-end VC-generation (generating obligations but discharging with Precept's existing finite strategy set, no SMT) is a coherent middle point between B1 and full B2/IVL — unexplored here; would need a feasibility pass.

## Sources

- **David J. Pearce. "The Architecture of Verification in Whiley" (2013-06-26).** <https://whileydave.com/2013/06/26/the-architecture-of-verification-in-whiley/>. Secondary (named author = Whiley designer; blog). Accessed 2026-05-29. Mirrored to `research/references/type-proof-stage-contract/source-excerpts.md`.
- **David J. Pearce. "Generating Verification Conditions for Whiley" (2012-12-04).** <https://whileydave.com/2012/12/04/generating-verification-conditions-for-whiley/>. Secondary. Accessed 2026-05-29. Mirrored.
- **WhileyTheoremProver README.** Whiley project. <https://github.com/Whiley/WhileyTheoremProver>. Primary (project README). Accessed 2026-05-29. Mirrored.
- **Viper project page.** ETH Zürich Programming Methodology Group. <https://www.pm.inf.ethz.ch/research/viper.html>. Primary. Accessed 2026-05-29. Mirrored.
- **Why3 project home.** <https://www.why3.org/>. Primary. Accessed 2026-05-29. Mirrored.
- **F\* language home.** <https://www.fstar-lang.org/>. Primary (for SMT/Dijkstra-monad framing); the "fused-not-separate-stage" reading is Tertiary inference from paper titles (see Threats). Accessed 2026-05-29. Mirrored.
- **Vytiniotis, Peyton Jones, Schrijvers, Sulzmann. "OutsideIn(X): Modular type inference with local assumptions."** Journal of Functional Programming, Vol 21, pp. 333–412 (2011). <https://simon.peytonjones.org/outsideinx/>. Primary (peer-reviewed). Accessed 2026-05-29. Mirrored.

### Precept canonical sources grounding the "where Precept sits" claim

- `docs/compiler/proof-engine.md` — Decision 3 (obligations stamped by type checker), Decision 5 (modifier-proof via catalog), `ProofRequirement` DU, the SMT-rejection rationale. Canonical.
- `src/Precept/Pipeline/ProofEngine.cs` — `CollectObligations` / `WalkActions` (dynamic-obligation channel). Source.
- `src/Precept/Language/Actions.cs`, `Action.cs` — `DynamicObligationGenerator` catalog field. Source.

### Prior in-tree surveys extended (reused, not duplicated)

- `proof-attribution-witness-design-survey.md` — SPARK/GNATprove, Dafny→Boogie, Frama-C/WP→Why3, Liquid Haskell, CBMC, Rust borrowck mechanics (reused for the who-generates / how-communicated / who-emits columns).
- `flow-sensitive-check-placement-survey.md` — Roslyn nullable, K2 CHECKERS, Rust MIR borrowck, TypeScript CFA staging (reused for the diagnostic-staging column + pattern 6).
- `compiler-pipeline-architecture-survey.md` — rustc query system, Roslyn stages, K2 FIR phases, CUE unification (reused for the demand-driven column + pattern 4/5).
- `research/architecture/README.md` § Open questions #1–#2 — the phase-formalization and deferred-diagnostics questions this survey adds evidence to.

## Inbound citation

This survey is load-bearing for the assignment-qualifier-discharge-placement design pass (the B1 self-derive vs B2 VC-handoff question) and for the broader type↔proof-contract direction (stay catalog-driven self-derivation vs adopt front-end VC-generation / IVL). That design pass will cite this file for the pattern enumeration, the purpose-based split, and the precedent for each option. The Precept direction and the B1/B2 pick are **deferred to that owner-facing pass** and are not decided here.
