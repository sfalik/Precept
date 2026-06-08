# Soundness Case — Precept's compile-time guarantee

**Status:** Draft — Soundness Case (living artifact) — 2026-06-07. **NOT a locked design.** This is the *central deliverable* of the soundness-and-coverage architecture effort: an explicit, auditable argument for **why the compile-time guarantee holds**. It is maintained across `/design` → `/plan` → `/execute`; the owner signs off on *this argument*, not on "tests pass." It is the forcing function — a complete Case cannot be authored if a gap exists, so an un-fillable section *is* a found gap.

> **The claim under argument.** *If a definition compiles, the entity is correct:* every rule is structurally enforced (violation unreachable), no runtime fault can occur (totality), no dead-end/unreachable configuration exists (structural completeness), and the entity is constructable. (Principles 1/6/7/10/11; spec §0.1, §0.5, §0.6, §0.7.)

> **Why the Case is needed.** The guarantee is only as sound as the engine's verdicts, and today those verdicts are **trusted, not checked**. `[StaticallyPreventable]` is a *correspondence assertion* (a reflection map, `FaultCode.cs`), not a proof that the diagnostic actually fires at every fault site; the BUG-017/021 family demonstrated that a `Proved` disposition can be hollow. The architecture this Case argues for closes that gap with **witness-checking** (every "proved" carries a legible certificate a *small trusted checker* re-verifies) **+ a catalog-driven coverage analyzer** (every obligation that should emit, does).

---

## 0. How to read this Case

The Case is structured as **two theorems, a trusted-base audit, and an empirical net**, over a **scope object** (the complete soundness-verdict family). The logic:

1. **Scope (§1)** — enumerate *every* soundness-critical verdict. If a verdict class is missing here, the whole Case is silently incomplete — so this enumeration is independently replicated and convergence-checked.
2. **Soundness theorem (§2)** — *for every verdict in scope, if the checker accepts, the verdict genuinely holds.* This kills **false positives** (hollow `Proved`).
3. **Coverage theorem (§3)** — *every obligation that should be raised, is raised, and reaches a checker.* This kills **missing obligations** (the BUG-017 shape — a fault site with no obligation stamped at all).
4. **Trusted-base audit (§4)** — the soundness/coverage theorems reduce trust to a *small* base (the checkers + the coverage analyzer + the catalogs). This section enumerates that base and argues each component correct, and is designed so the base *can* later be formally verified.
5. **Empirical net (§5)** — independent runtime evidence that the theorems hold in practice: differential testing, bounded-model-checking over the finite state space, and mutation testing of the checker itself.
6. **Falsifier register (§6)** — the standing "what would prove this Case wrong" list, actively hunted.

**Soundness vs completeness asymmetry (load-bearing, spec §0.6 philosophy #1).** A false `Unresolved` (rejecting a safe definition) is *friction*; a false `Proved` (accepting an unsafe one) is a *broken guarantee*. The Case is therefore asymmetric: §2 (soundness) is the hard guarantee; over-rejection is a usability concern handled elsewhere. Every "Proved/clean/well-typed/reachable" is soundness-critical; the corresponding "Unresolved/diagnostic" is not.

---

## 1. Scope — the complete soundness-verdict family

> *The set of every verdict whose false-positive breaks the guarantee. The design covers this set; the plan builds a checker for every cell; this Case argues each is sound and covered.*

**Populated from the independently-replicated Step-0 enumeration (two cold agents "A" and "B" + reconciliation).** Convergence on the cell-set is the completeness evidence; every divergence is investigated, not averaged. The two enumerations **converged strongly**: identical phase structure, identical 13-cell proof-obligation core, and — most importantly — *both independently* surfaced the obligation-emission-completeness family (§1.5) as the highest-leverage false-clean class and *both independently* rediscovered the same live unsound hole (cross-unit qualifier-product cancellation, F-5/§1.1). Divergences occurred only at edges both agents self-flagged as uncertain (intermediate-subexpression overflow coverage; the event-arg constraint-carrying link) — these are tracked as open coverage probes, not resolved by averaging.

### §1.1 Proof engine — obligation kind × discharge (`Proved` could be false)

The 13 enum-derived `ProofRequirementKind` cells (each `Proved` is soundness-critical; each maps to a `FaultCode` via the routing in `ProofEngine.Diagnostics.cs`):

| Obligation kind | `Proved` asserts | Discharge strategies | Linked fault / diagnostic |
|---|---|---|---|
| **Numeric** | divisor ≠ 0 / sqrt operand ≥ 0 / pow exponent ≥ 0 / value in band | `Literal`, `DeclarationAttribute`, `GuardInPath`, `FlowNarrowing`, `CompositionalConstraint` | `DivisionByZero`(83), `SqrtOfNegative`(84), `OutOfRange`(79) |
| **Presence** | optional field set before access | `GuardInPath`, `FlowNarrowing` | `UnprovedPresenceRequirement`(116) → `UnexpectedNull`(5) |
| **Dimension** | period operand has required time dimension | `DeclarationAttribute`, `GuardInPath` | `UnprovedDimensionRequirement`(113) |
| **Modifier** | field declares required modifier (`ordered` …) | `DeclarationAttribute` | `UnprovedModifierRequirement`(112), `NonOrderableCollectionExtreme`(65) |
| **QualifierCompatibility** | two operands share a qualifier-axis value | `QualifierCompatibility` | `UnprovedQualifierCompatibility`(114) → `QualifierMismatch`(11) |
| **QualifierChain** | multi-hop qualifier chain resolves compatibly | (chain narrowing) | PRE0114 family |
| **IntervalContainment** | computed/assigned value-interval ⊆ field `[min,max]` | `IntervalContainment` | `NumericOverflow`(78) |
| **LengthContainment** | string length-interval ⊆ `[minlength,maxlength]` | `LengthContainment` | `LengthBoundViolation`(135) |
| **CountContainment** | post-mutation count-interval ⊆ `[mincount,maxcount]` | `CountContainment`, `CollectionGrowth` | `CountBoundViolation`(136) |
| **KeyPresence** | collection provably contains / lacks key | `GuardInPath` | `KeyPresenceSafety`(99) / `KeyUniquenessGuard`(101) |
| **IndexBounds** | index N satisfies `0≤N<count` (or `≤count` insert) | `GuardInPath` | `IndexBoundsGuard`(100) |
| **DimensionalProduct** | `quantity×quantity` lands in curated business-domain dimension set | `DimensionalProduct` | `IncompatibleDimensionalProduct`(157) |
| **AssignmentQualifier** | open field narrows under guard to target's qualifier | `QualifierCompatibility` | `UnprovedAssignmentQualifierCompatibility`(141) |

**Cross-cutting proof-engine machinery verdicts** (a bug here corrupts many cells at once):
- **Sequential proof-flow / staleness** (`ReassignedBefore`, `CountEstablishedBefore`, `CountInvalidatedBefore`, `ProofLedger.cs:34–82`) — a guard fact must not discharge an obligation downstream of a reassignment that invalidated it. **This is the exact previously-shipped-and-corrected over-prove** (spec §0.6 item 7: "a guard fact survived a reassignment, proving a divide-by-zero safe"). Highest-leverage proof-flow verdict — multiplies across Numeric/Presence/Key/Index.
- **Relational-fact soundness** (the three discharge-side invariants, `proof-engine.md` Decision 6) — guarded-rule drop filter, consumer isolation, contradiction guard. A break over-proves via `FlowNarrowing`.
- **Unreachable/dead-end obligation suppression** (`ProofEngine.cs:1166–1193`) — obligations on transitions from unreachable states are force-`Proved`. **Sound only if the graph analyzer's reachability verdict (§1.3) is sound** — cross-phase dependency.
- **Composition** (`ProofEngine.Composition.cs`) — a fact whose own obligation is still `Unresolved` must not discharge a dependent obligation.
- **Unit normalization** + **interval over-approximation core** (`ProofEngine.Intervals.cs`) — a wrong UCUM scale factor or an *under*-approximating interval produces a false containment `Proved`.
- **Default-satisfiability** (`ProofEngine.Satisfiability.cs`) — defaults provably satisfy every unguarded global `rule`; a false "clean" makes an entity constructible into a rule-violating configuration (Principle-1 breach). Conservative: only a *provably-false* fold rejects.

**Known live unsound hole (both agents, independently):** cross-unit cancellation in qualifier products (`proof-engine.md:1790`) — `'USD/kg' × quantity in 'g'` silently drops the UCUM factor; the doc itself labels it "unsound for cross-unit operands." Tracked as falsifier **F-5**.

### §1.2 Type checker — "well-typed / no-diagnostic" assertions

Type soundness (`TypeMismatch`, `UndeclaredField`, `InvalidMemberAccess`, arity/arg-constraint, null-in-non-nullable, choice membership, typed-constant validity, collection/scalar shape); qualifier/unit/dimension soundness (`QualifierMismatch`, cross-currency/dimension/counting-unit, bounds-require-qualifier — the latter is *upstream-critical* because it defines the bound every §1.1 containment proves against); access-mode/omit soundness (`OmittedFieldRead/Set`, `ConflictingAccessModes`, `ComputedFieldNotWritable` — these are where the future `WriteToOmittedField`/`WriteToReadOnlyField` faults will map); construction-time totality (`RequiredFieldUnassignedOnEntry`, the uninitialized-read family, `CircularComputedField` — acyclicity also underpins the §0.4 no-fixpoint property).

### §1.3 Graph analyzer — structural verdicts (under §0.5 over-approximation)

Reachability, no structural sink, no dead-end, terminal/irreversible contracts, required-state dominance (Lengauer-Tarjan), reject-classification (`AlwaysRejecting` / `StateAlwaysRejects`), single well-formed initial/construction. **Over-approximation is sound only one-way** (treat every edge as traversable): the risk inversion to guard is any place that *prunes* a guarded edge — reject-classification is the highest-risk site (it reasons over outcomes, not pure edge structure). **Cross-phase note:** the spec-only dead-guard/unsatisfiable-guard items must never be allowed to prune graph edges when they ship, or reachability under-approximates and §1.1's suppression discharges real obligations falsely.

### §1.4 Fault↔diagnostic correspondence

Every `FaultCode` (15, `FaultCode.cs`) must have a preventing diagnostic that *actually fires at every site that could produce it* — not merely a registered `[StaticallyPreventable]` link. The existing drift test ("the code has ≥1 `Diagnostics.Create` call") is *necessary, not sufficient*: "wired somewhere" ≠ "fires on every fault-reachable path" — that sufficiency is exactly §1.5. Higher-risk rows: the non-bijective `UnexpectedNull` (shared conservative backstop) and the many-to-one collection-safety collapses (hand-coded policy, not attribute-derived). **Known map gaps:** the future `WriteToOmittedField`/`WriteToReadOnlyField` faults (referenced in `Evaluator.cs`, not yet enum members) currently have no fault-side backstop — prevention rests entirely on the type-side verdicts in §1.2.

### §1.5 Obligation-stamping completeness — the invisible verdict family

The pipeline's silent decision *not* to stamp an obligation at a fault-prone site. This is an **absence** — no catalog enumerates it, no discharge test exercises it, and the per-verdict checker of §2 cannot see it (a checker can only reject a *witness that exists*). It is exactly the BUG-017/021 shape, and **both cold enumerations independently elevated it to the top of the leverage list**. `proof-engine.md:507–513` already calls emission-completeness a *contract*, not a guideline. The **coverage theorem (§3) exists primarily to make this family checkable** — it is the reason the architecture is witness-checking *plus* a coverage analyzer, not witness-checking alone.

**Open coverage probes (divergence edges, both agents):** (a) intermediate-subexpression overflow — whether every interior arithmetic node (not just the assignment boundary) gets a `NumericOverflow` obligation; (b) the event-arg constraint-carrying link — whether a missing constraint-carrying check on an event arg could let an unconstrained arg falsely discharge a downstream obligation (the §0.7 Composition seam); (c) newer construct families (materialized/composite-basis fields) whose obligation completeness is unverified. These are the cells most likely missed by an enumeration derived from discharge strategies rather than emission sites — carried forward as design-pass coverage targets.

---

## 2. Soundness theorem — accepted ⟹ true

> **Theorem (per-verdict soundness).** For every soundness-critical verdict V in scope (§1): if the trusted checker accepts the witness W that the (untrusted) engine emitted for V, then V genuinely holds.
>
> Equivalently: a hollow `Proved`/`clean`/`well-typed`/`reachable` cannot survive the checker. The engine may be *arbitrarily buggy* — soundness rests only on the checker, not on the engine.

**Proof obligation of the Case (to be discharged per verdict class in the design + plan):** for each cell in §1, exhibit (a) the **witness format** — the certificate data the engine emits, (b) the **checker** — the small trusted procedure that re-verifies W against the actual obligation, and (c) the **local soundness argument** — why "checker accepts W" implies the verdict. The verify-simple/validate-hard pattern (Verasco-style): the engine does the hard search; the checker does the easy *validation*, and only the checker is trusted.

**Witness format note.** This completes an *existing spec mandate* — §0.6 item 13 (structured proof attribution) and Proof philosophy #6 already require the engine to carry *why* a verdict holds; today `ProofLedger.cs` carries `Strategy` + `ComputedInterval` but not the full attribution chain. The witness format is the realization of that mandate, structured as a re-checkable model (not free text).

*(Per-verdict-class soundness arguments populated in the design pass.)*

---

## 3. Coverage theorem — every obligation that should emit, does

> **Theorem (coverage).** For every site in a definition that *could* produce a soundness-critical fault, the pipeline stamps the obligation whose discharge §2 governs; no fault-prone site is silently un-obligated.
>
> Soundness (§2) guarantees *accepted verdicts are true*. Coverage guarantees *there are no missing verdicts* — it closes the gap where the engine never raises an obligation at all (so there is nothing for the checker to reject). §2 + §3 together are the whole guarantee: every fault site raises an obligation (§3), and every accepted obligation is genuinely discharged (§2).

**Mechanism (research-validated, has a direct model).** A **catalog-driven coverage analyzer** on the `PRECEPT0019` set-difference pattern (`Precept0019PipelineCoverageExhaustiveness.cs`, the `[HandlesCatalogExhaustively]`/`[HandlesCatalogMember]` model): the catalogs declare the *complete* set of fault-prone construct shapes; the analyzer proves, by catalog set-difference, that the obligation-raising code handles every member — a missing handler is a build error, not a latent hole. Runs in CI so coverage cannot regress.

**This is the §1.5 verdict family's only defense.** Obligation-stamping completeness is an *absence* invisible to per-verdict checking (a checker can only reject a witness that exists); the coverage analyzer is what makes the absence detectable.

*(Coverage-analyzer scope + the catalog completeness arguments populated in the design pass.)*

---

## 4. Trusted-base audit

> §2 and §3 reduce the trust surface from the full ~7.8 KLOC prover (+ type-checker + graph-analyzer) to a *small* trusted base: the **checkers**, the **coverage analyzer**, and the **catalogs** they read. Everything else (the entire obligation-discharge search) is **untrusted** — a bug there can only cause over-rejection (friction), never a false accept.

**Sizing (firmed by the Step-0 per-strategy witness/checker-complexity probe, grounded in `ProofEngine.*.cs`):** the trusted base for the **current 11 strategies** lands at **~750–900 LOC** — ≈500–600 LOC of checker kernel + ≈250–300 LOC of witness data-model (a DU of certificate shapes). The ~1,500-line working estimate has real headroom, reserved for the relational-domain certificate verifier (Farkas combination / affine-equality basis) that the future Karr/octagon work will need — *not* required by the current 11. Two structural facts drive the small size: (i) **5 of 11 strategies are trivial re-execution** (Literal, Length/Count/Interval-containment band-checks, CollectionGrowth) sharing one arithmetic core — the verify-simple half; (ii) **CountContainment and CollectionGrowth already carry their full certificate** on the requirement/obligation record today. The "validate-hard" half is the 6–7 relational/qualifier/compositional strategies whose witness must *name the contributing facts* so the checker verifies rather than re-searches.

**Per-component correctness argument + independent-replication record** (each trusted component built by ≥2 independent attempts that must converge) is populated in the design + plan. **Known wildcard (probe-flagged):** `CompositionalConstraint`'s witness must capture the blocked-constraint discipline (self-unsatisfiable rules excluded) — whether that is witness-carried (cheap) or checker-recomputed (pulls satisfiability machinery into the trusted base) is a real design fork worth ±80 LOC. **Witness-DU note:** the 11-member `ProofStrategy` enum *under-counts* the distinct witness shapes (≈14–15 subtypes needed — `GuardInPath` and `QualifierCompatibility` each cover multiple sub-strategies); the witness model is keyed by witness shape, not by enum member.

**Generalization sizing (Step-0 graph/type-checker witness-inventory probe).** The architecture generalizes cleanly but the *checkers are phase-specific* — three idioms, near-zero shared verification logic: (1) **tree re-derivation** (type checker), (2) **set-closure / emptiness-certificate verification** (graph analyzer negative verdicts), (3) **interval/relational containment** (proof engine). What *is* reusable across phases — and is the genuine shared kernel — is the **witness-plumbing convention** (the existing `ProofForwardingFact` DU in `StateGraph.cs:91` already demonstrates structured-witness forwarding; extend it, don't fork) and the **catalog-as-the-checker's-oracle** principle (every checker re-derives from `Operators`/`Types`/`Modifiers`/`Functions`, never a parallel rule list — the CLAUDE.md catalog non-negotiable *is* the witness-checking trusted base). The witness *data* already largely exists: graph reachability paths / dominator facts / reachable sets are surfaced today; the type checker's `TypedExpression` tree (`SemanticIndex.cs:17-130`) is a fully-annotated typing derivation — but it is **discarded at the proof-engine boundary**, so the main net-new plumbing is *retaining* it. Phase-specific work for the generalization phase (P6+): surface the reverse-reachable set + **pick the cut-set dominance witness** (the one Alethe-pole design fork — re-checking the fixpoint dominator-set ≈ recomputing it; the cut-set reformulation restores cheap checking); retain the typing derivation; build the two-to-three small idiom-specific checkers. **The expensive certificate machinery (Farkas/DBM) is NOT required by either phase's currently-shipped verdicts** — it only re-enters with the deferred semantic-completability (liveness G5b) roadmap verdict, whose witness (an exhibited satisfying input) is the one place that genuinely reuses the proof engine's relational machinery. **Positive/negative asymmetry** (load-bearing for checker cost): positive existential verdicts (reachable / well-typed) have cheap re-executable path/derivation witnesses; negative/universal verdicts (unreachable / dead-end / dominates) need closed-set emptiness certificates (moderate). The type checker is almost entirely positive (lightest phase); the graph analyzer is mixed; only dominance carries a real witness-design decision.

**Design-for-formal-verifiability:** the trusted base is to be **small, pure, side-effect-free** so it *can* be formally verified later (F*/Coq/a verifiable subset). Full formal verification is the eventual ceiling; the base is designed toward it now. The audit records, per component, whether it meets the purity discipline.

| Trusted component | What it certifies | Why it is correct | LOC | Replication | Verifiable-shape |
|---|---|---|---|---|---|
| *(per-strategy checkers — populated from Step-0 probe)* | | | | | |
| *(coverage analyzer)* | | | | | |
| *(catalogs as ground truth)* | | | | | |

---

## 5. Empirical net

> The theorems are the argument; the empirical net is the *independent evidence* that the argument holds in the running system. Standing harnesses, not one-time checks. Precept's **finiteness** (closed, no loops, small state spaces) makes near-exhaustive empirical verification possible — a property general program verification cannot use.

- **§5.1 Differential testing** — every `Proved` verdict cross-checked against the runtime evaluator over generated inputs: if the engine proved a divisor non-zero, no generated input may make it zero. (Blocked today: the evaluator is a stub — `Evaluator.cs` throws `NotImplementedtException`. This harness lands when the evaluator does; flagged as a §6 falsifier-dependency.)
- **§5.2 Bounded-model-checking over the finite state space** — exhaustively enumerate reachable configurations (finite, by construction) and confirm no rule-violating / faulting / dead-end configuration is reachable. This is *near-formal* and is **only possible because Precept is finite** — the closest thing to a machine-checked guarantee short of formal verification.
- **§5.3 Mutation testing of the checker** — deliberately plant bugs in the *trusted* checker and confirm the test suite catches them. This is what *justifies trusting the checker*: a checker whose bugs the tests don't catch is not yet trustworthy. Exit criterion: 100% mutation kill on the trusted base.
- **§5.4 Adversarial false-witness suite** — a standing corpus of hand-crafted *false* witnesses (a plausible-but-wrong Karr basis, a Farkas combination that doesn't sum, an interval that under-approximates) that the checker MUST reject. Grows every time a new strategy or failure mode is found.

---

## 6. Falsifier register

> The standing "what would prove this Case wrong" list — actively hunted, not passively maintained. (Hillel Wayne's "what would change my mind.") Each entry names an observation that, if found, falsifies a Case claim.

- **F-1 — a strategy with no checkable witness.** If any `ProofStrategy` discharges a verdict whose correctness cannot be captured in a re-checkable certificate (the checker would have to re-run the full engine), the verify-simple/validate-hard premise fails for that cell. *Probed in Step-0 (per-strategy witness/checker complexity).*
- **F-2 — a fault-prone construct shape absent from the catalogs.** Coverage (§3) rests on the catalogs being the *complete* enumeration of fault-prone shapes. A fault site whose shape no catalog declares is invisible to the set-difference. *Hunted continuously; the coverage analyzer's own completeness is itself a §1.5 verdict.*
- **F-3 — a checker the tests don't constrain.** If mutation testing (§5.3) cannot reach 100% kill on a trusted component, that component is trusted without justification. *Exit criterion in the plan's trust-harness phase.*
- **F-4 — Karr/relational technique needs a fixpoint.** If closing the affine-equality gap requires iteration-to-fixpoint, it violates §0.4 (no widening) and is not an additive technique. **Resolved (Step-0, two independent probes converged): feasible-single-pass.** Karr's classical fixpoint requirement is purely a consequence of loops; §0.4.1/0.4.2/0.4.3 structurally eliminate loops, branches, and reconverging flow ("there is no join point where two different states must be merged"). The affine-subspace join at the two merge sites (conditional expressions `Intervals.cs:135-140`; OR-guard arms) is a terminating one-shot Gaussian-elimination step — categorically distinct from iteration-to-fixpoint — and Karr slots additively into the existing reduced-product reduction in `BuildNarrowedIntervals`, within the per-keystroke budget (O(k³), k = field count < 50). **Two design-time conditions (owner sign-off):** (1) it relaxes the current strictly-depth-1/two-field-inequality posture to "one bounded global linear solve" (still fixpoint-free; the §0.6:256 depth-1 wording needs updating); (2) the global affine basis must honor the §0.6-item-7 sequential-reassignment invalidation discipline (drop/recompute equalities mentioning a reassigned field). The residual falsifier: if design enumeration finds a *merge site beyond conditional-expressions and OR-guards* that is iterative, or a relational technique whose witness needs a non-checkable solver trace (violating R1-legibility / §0.6 #3), the additive claim weakens.
- **F-5 — graph over-approximation pruned in the unsafe direction.** If any graph verdict prunes a guarded edge (treating it as non-traversable), reachability under-approximates and a real dead-end could be declared clean. *Flagged by the verdict-family enumeration (reject-classification is the highest-risk site).*
- **F-6 — the `[StaticallyPreventable]` map has a gap.** A `FaultCode` whose preventing diagnostic does not fire at some site (or a fault with no map entry — e.g. the future `WriteToOmittedField`/`WriteToReadOnlyField` placeholders). *The map's completeness is a §1.4 verdict; the coverage analyzer should subsume it.*

---

## Provenance

Grounds: the 5 research surveys in `research/architecture/compiler/` (`solver-free-static-analysis-techniques-survey`, `witness-checking-soundness-architecture-survey`, `fragment-boundary-corpus-validation-2026-06-05`, `liveness-completability-verification-survey`, `proof-attribution-witness-design-survey`); the architecture brief `proven-core-governed-boundary-2026-06-05.md`; spec §0.1/§0.4/§0.5/§0.6/§0.7; `ProofLedger.cs`, `Precept0019PipelineCoverageExhaustiveness.cs`. Feeds: `/design` (the locked architecture) and `/plan` (the phased build). Per the meta-plan `streamed-swinging-sprout.md`.
