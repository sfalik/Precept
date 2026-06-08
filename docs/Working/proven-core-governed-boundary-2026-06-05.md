# Proven Core, Governed Boundary — architecture exploration

**Status:** Design-exploration brief — **NOT a locked design, NOT a syntax proposal.** Philosophy-adjacent (it concerns the core guarantee architecture) and owner-driven. It captures a direction developed in conversation, to feed a `/design` pass (with the pre-design owner consultation). **No syntax is proposed here** — construct *shapes* are sketched only to convey the concept; forms are deferred to the design pass. Authored 2026-06-05.

> One idea: confine all *governed* validation to the ingress boundary (args / edits), where passing the check **establishes a proven refinement** that the compiler propagates to the entire interior. Governance lives at the door; everything inside the door is proof. The widening constructs (maintained aggregates, conserved pools, etc.) are all *instances* of this one principle.

---

## 1. The organizing principle

§0.7 already designates **ingress** ("what enters" — construction, events, edits) as the governance boundary. This direction promotes that boundary from *"rejects bad input"* to *"establishes the facts the proof engine consumes."* An arg or edit that passes its ingress check is stamped with a **validated refinement**, and the compiler then treats that refinement as a proven precondition for every downstream operation.

The result is a **proven core with a governed boundary**: the entity's interior is a closed, always-valid world; the only place validation happens is the boundary where the outside world touches it. That is the cleanest possible shape for a *domain integrity engine*, and it resolves the "governance is hollow" critique structurally — governance is not scattered runtime checks on every operation; it is a single boundary that *feeds the proof*.

## 2. Why it escapes the proof fragment

The fragment limit (intervals + two-variable octagon; see `precept-guarantee-specification-2026-06-05.md`) is a limit on **static proof**. An ingress check is **runtime evaluation of a concrete predicate** — the value and current state are *known* — so it can evaluate *any computable* constraint, however nonlinear (a concentration ratio, an overlap test, a checksum, a regex, a capability index). There is **no fragment limit on a concrete evaluation.**

So the hard check moves to the one place it is free, and its *result* becomes a static fact the in-fragment engine carries forward:

> **dynamic check at the boundary → static fact in the core.**

We are not *proving* the hard thing statically; we are *checking* it once where checking is unrestricted, then *proving with the result*.

## 3. Two regimes (honest about where it's eternal vs maintained)

- **Intrinsic value refinements — eternal, free.** A constraint *about the value itself* (range, set membership, pattern, any predicate) is checked once and is true *forever*, because the value is immutable. The compiler propagates it with zero further obligation. Recovers **every value-intrinsic out-of-fragment case** — the bulk of "validate this field" rules.
- **Relational / aggregate refinements — maintained by universal re-gating.** A fact relating a value to *mutable* state (`Position ≤ 20% of Total`) could be invalidated by a later write. The structural payoff: because Precept sees **every** mutation site (finite, no hidden writes, no aliasing), the compiler can *require* that every write which could invalidate the refinement **re-passes the same check**. "Checked once" becomes "maintained because there is no way into the state without passing the gate." Overlap, conservation, concentration, `sum ≤ budget` all recover this way.

**The genuine residual (an open design question, §6):** a relational refinement whose related state is mutated *outside* a gate would go stale. The language needs a **refinement-scope rule** — the compiler enforces that all fields of a refinement are gate-only (it can, since it sees every write), or the refinement is re-checked at use, or it is scoped to the admitting operation.

## 4. The widening constructs, as instances of the one principle

Each "wall" from the 16-domain probe is a place where authors are forced down to raw arithmetic with no construct to carry the proof. Each construct below is the *same boundary/core move* specialized to a wall — the hard part is established at a gate, and the interior relies on it.

| Wall (probe) | Construct (instance of the principle) | How the boundary establishes the fact |
|---|---|---|
| **Aggregate over collection** (`sum(lines) ≤ budget`) — dominant | **Maintained aggregate** field (compiler-owned incremental `sum`/`count`) | The compiler owns the increment/decrement at each mutation gate, so it *proves* the aggregate tracks the collection; the bound is then an ordinary in-fragment field rule. (Incremental view maintenance — *not* general `reduce`.) |
| **Conservation / partition identity** (`A+B+C == Total`) | **Conserved pool** with **transfer-only** mutation (`move N from X to Y`) | The only operation is a balanced transfer (−N here, +N there), so the identity holds *by construction*; a non-conserving mutation is unexpressible. (Double-entry as a primitive — also recovers `debits == credits`.) |
| **Interval overlap** (`no double-booking`) | **Exclusive-interval** collection type | The type's admit operation gates on non-overlap; every entry re-passes, so the set is non-overlapping by the admission discipline. (Verified primitive carries the invariant.) |
| **Quantified element rule** (`all x ≥ floor`) | **Refinement-typed element** (`collection of {x: x ≥ floor}`) | The refinement is gated *at insertion*; the quantified invariant holds by construction. |
| **Bounded products** (`X·Y ≤ c`, both bounded) | **Interval multiplication** in the engine | Not a boundary case — standard interval arithmetic over bounded operands; sound, recovers much of the product wall directly. |
| **Residual nonlinear / distributional** (`Cpk ≥ 1.33`) | *(stays governed at ingress)* + **lifecycle gate** for containment | Checked at the boundary (concrete eval, unrestricted); a gate transition can establish a *proven downstream state-fact* ("in `Verified`, Cpk held"), confining the approximation to one checkpoint. |

The first two — **maintained aggregates** and **conserved pools** — are the highest-leverage (they recover the two dominant walls) and the best-grounded.

## 5. The payoff

Combined, the *irreducibly-governed-and-uncontained* zone shrinks to almost nothing:
- Anything checkable (i.e. anything) is validated at the boundary — no fragment limit.
- Intrinsic facts propagate free; relational facts stay live via compiler-enforced universal re-gating.
- The proven interior never sees an unvalidated value, so it is structurally safe.

This makes *"OK with governed validation at input time"* a **sufficient** stance — input time is the only time governance is needed. The 16-domain probe answered "is the provable space large enough" as **yes** (~⅔ even in adversarially-hostile domains, ~89% in the natural corpus); this architecture widens it further by converting the dominant governed shapes into proven-because-gated ones.

## 6. Open questions for the design pass (do not resolve here)

1. **Refinement-scope / staleness rule** (the load-bearing one, §3) — how a relational refinement's validity is bounded and re-established; whether the compiler enforces gate-only writes on refined fields.
2. **Which aggregates maintain cleanly** — `sum`/`count` are exact incrementally; `min`/`max` are clean for append-only collections, costly otherwise; conditional/filtered aggregates need care.
3. **Conserved-pool surface** — bucket declaration, the transfer operation, interaction with the existing field/event model.
4. **Refinement syntax + ingress semantics** — how a validated predicate attaches to an arg/edit, and how `InvalidArgs`/`ConstraintsFailed` at ingress relate to it.
5. **DBM → octagon** — the implemented relational record is one-field-per-side (a strict DBM); two-field sums are in-principle octagon but not native today. Extending to octagon widens the in-fragment base under all of the above. (Also: fix `guarantee-spec §7`, which overstates two-field sums as native.)
6. **Distributional statistics** — confirm these stay governed (runtime metrics, not definitional invariants); the lifecycle-gate is their containment, not a proof.

## 7. Grounding

Well-founded PL, not novel risk: **refinement types discharged at a trusted boundary**; **"parse, don't validate"** (illegal states unrepresentable at the edge, valid everywhere after — Alexis King); **rely/guarantee** reasoning; **incremental view maintenance** (materialized aggregate views); **double-entry** (conservation by construction). The Precept-specific elegance is that §0.7 *already* nominates ingress as the governance boundary — this completes the existing model rather than fighting it.

## 8. Evidence base

- 16-domain adversarial probe (the walls + the verdict "large enough, gaps namable"): summarized in conversation; candidate for a durable decision doc.
- `research/architecture/compiler/fragment-boundary-corpus-validation-2026-06-05.md` (~89% in-fragment; the 3-field balance identity as the recurring out-of-fragment invariant).
- `research/architecture/compiler/liveness-completability-verification-survey.md` (DBM feasibility solver-free; the data-fragment boundary).
- `docs/Working/precept-guarantee-specification-2026-06-05.md` (the guarantee taxonomy this architecture would widen; §4 prevented/rejected/governed; §7 boundary).
- Spec: §0.4 (single-pass), §0.6 #3 (no SMT), §0.7 (ingress vs sweep — the boundary this builds on); `docs/philosophy.md` (controlled mutation, no functions, determinism — what makes universal re-gating enforceable).

## 9. What this feeds

A `/design` pass (with the pre-design owner consultation, since this is language surface and philosophy-adjacent), scoped in order:
1. **The keystone** — the boundary/core principle: refinement-establishment at ingress + compiler propagation + the §6.1 scope rule. Everything else depends on it.
2. **Maintained aggregates** and **conserved pools** — the two highest-leverage instances.
3. The remaining instances (exclusive-interval, refinement elements, interval multiplication) as additive extensions.
