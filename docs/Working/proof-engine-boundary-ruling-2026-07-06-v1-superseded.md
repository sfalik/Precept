---
title: Proof-Engine Boundary Ruling — Which Model Governs Precept
status: Draft — 2026-07-06 (pending Shane ratification)
author: Frank (Lead/Architect & Language Designer)
owner: Shane
branch: spike/Precept-V2-Radical
verified-at-HEAD: e09f1af44febcaa8c3768c4a28f94cb45a91d820
supersedes:
  - docs/Working/band-guarantee-boundary-analysis-2026-07-03.md
  - docs/Working/d2-proven-violation-philosophy-analysis-2026-07-03.md
  - docs/Working/prove-govern-classification-and-legibility-2026-07-06.md (as the model ruling; its legibility mechanism is adopted)
  - docs/Working/decision-index.md D2 (band-split), as an unratified proposal
gate: This is a RECOMMENDATION for the owner-gated surfaces (docs/philosophy.md, spec §0.6/§0.7). Frank does not edit them; Shane ratifies.
---

# Proof-Engine Boundary Ruling

> **Neutrality is over. This document rules.** Phases 0–2 gathered and adversarially verified the evidence; the fleet converged and the red-team refined it. Every load-bearing claim below carries a `path:line` citation I re-confirmed first-hand at HEAD `e09f1af4` (the anchor verified at `0127340b`; the three primary-source files — `precept-language-spec.md`, `Diagnostics.cs`, `philosophy.md` — are clean and unchanged between those commits, so line numbers hold).

---

## 1. Executive ruling

**Precept is governed by the Hybrid model: the compiler is *total* over the entire definition-internal surface — every value produced by evaluating a Precept expression is compile-time prove-or-reject for any fault or bound obligation it participates in, with no deferral — and runtime *governance* is real but confined to raw host-supplied values at their ingress slot, checked against that slot's own declared contract before any expression derives from them. The one-line boundary: a checked value is governed at runtime *only if it is host-origin, at the ingress edge, against its own same-slot contract, with no Precept expression node between the raw input and the check; everything else is proven at compile time or the definition is rejected.*** This is not a new invention — it is spec §0.7 (`precept-language-spec.md:266`/`:268`) as already written, made precise with a decidable *ingress-origin* predicate and defended against the drift (D2) that would have slid it toward runtime deferral. Model A (bounded compiler + defer-the-rest) is rejected because it contradicts Precept's identity guarantee (`philosophy.md:57`); pure Model B (refuse everything not statically decidable) is rejected because it would make legitimate nonlinear governance rules — balance identities, ratios, product rules — inexpressible, which the governance mechanism (`:268`) already handles honestly without any deferral of a fault.

---

## 2. Q1 — The model choice (the ruling; four legs)

### The three models, stated crisply

| Model | Compiler | Not-statically-decidable case | "Compiles clean" means |
|---|---|---|---|
| **A — Bounded + govern** | Partial. A stopping rule routes each construct compile-vs-runtime. | Deferred to runtime governance. | "Proven what I could; the rest is watched at runtime." |
| **B — Total + limited** | Total over the language. | **Inexpressible** — author rewrites into the provable subset. | "Totally proven." |
| **Hybrid (RULED)** | Total over the definition-internal surface. | Internal ⇒ **reject** (author supplies the carrier); genuinely external input ⇒ **governed at a named, visible ingress boundary**. | "Every internal computation proven; every external input carries its constraint, enforced at ingress." |

### The ruling: **Hybrid.**

### Leg 1 — Rationale (why, not what)

Precept's guarantee is stated in two voices that must both be true of a clean compile. The precise voice: *"A definition that compiles without diagnostics has no unproven evaluation faults, no unreachable business process states, and no structural dead ends"* (`philosophy.md:57`). The absolutist voice: *"Prevention, not detection. No errors. No bugs… these configurations and workflows cannot exist"* (`philosophy.md:49`). The spec operationalizes this as **two distinct mechanisms** and says so explicitly: *"Precept's guarantees are delivered by two distinct mechanisms; keeping them distinct is what makes the guarantee precise rather than magical"* (`precept-language-spec.md:264`). Mechanism one — **fault prevention** — is *"established entirely at compile time… It never compiles a fault-prone operation in the hope a runtime check catches it; there is no deferral"* (`:266`). Mechanism two — **governance** — is *"enforced at runtime on external input… every value entering the entity from outside the definition… before any computation derives from it"* (`:268`).

Hybrid is the *only* model that makes both voices literally true simultaneously. The compiler is total over everything the definition computes internally (so `:57`'s "no unproven evaluation faults" holds without qualification), while governance discards any invalid external input on a working copy before it can persist (`:268`; §3A.4 discard), so `:49`'s "these configurations cannot exist" holds by prevention, not detection. The boundary between the two is not a decidability threshold the compiler slides along — it is the *origin* of the value, a syntactic/dataflow fact. That is what makes it "precise rather than magical" and what stops the weeks of circling: a construct is routed by **where its value came from**, never by **how hard it is to prove**.

### Leg 2 — Alternatives considered and rejected

**Model A — rejected on identity, not on runtime feasibility.**
The philosophy defeater is decisive and I rest the rejection on it: a clean compile guarantees *"no unproven evaluation faults"* (`philosophy.md:57`) and *"if a precept compiles without diagnostics, it does not fault at runtime… Runtime fault checks exist only as defensive redundancy, never as the primary enforcement mechanism"* (Principle 11, `precept-language-spec.md:112`). Model A makes a runtime check the *primary* enforcement mechanism for the deferred set — that is precisely the identity Precept defines itself against (Principle 1 "Prevention, not detection," `:92`). Shane's own words name the harm: *"Why check some cases and not others at compile time? That gives a false sense of security"* (quoted in `band-guarantee-boundary-analysis-2026-07-03.md:11`).

**I explicitly decline the fleet's runtime-can't-enforce argument as the *defeater*.** Verifier-1 (gemini) is correct that "the runtime has no representation for a deferred band, therefore A is impossible" is **circular** — the runtime lacks band-governance plumbing *because HEAD implements no-deferral*; if we chose A we would build `ConstraintKind.FieldBand` and an ingress band-validation pass. The absence of that code (confirmed first-hand: `ConstraintKind` has exactly five members `Invariant/StateResident/StateEntry/StateExit/EventPrecondition`, no band member; `ConstraintsFailed` scope covers *"rules, state ensures, event ensures"* only, `result-types.md:114`/`:121`; `result-types.md` maturity is Design/*"Pending — not yet implemented"*) is an **engineering gap, not an architectural limit.** So it does not *defeat* A. What it *does* legitimately establish is A's **price**: choosing A means committing to design and build an entire runtime band-governance surface that does not exist and is not specified — a cost input, not the reason A is wrong. A is wrong because it trades away the product's identity; it is merely *also* expensive.

**Pure Model B — rejected on expressiveness, and because it over-reads fault-prevention onto governance.**
Pure-B refuses every construct whose safety is not statically decidable, including genuinely nonlinear rules. That is coherent but wrong for two reasons. First, the cost is not an edge case: 21 of 77 sample `.precept` files contain product/multiplication expressions (verifier-2, re-confirmed by HEAD grep), spanning loan debt ratios, rent-to-income, inventory cost, invoice subtotal/tax, lease overage, and OEE; the corpus audit puts ~89% of real invariants in-fragment (`precept-guarantee-specification-2026-06-05.md` §7), leaving a *small but load-bearing* residual of legitimate business invariants (balance identities `A + B == C` across four precepts, budgets, deposits, ratios). Forcing all of these into paired-action bookkeeping rewrites would, in verifier-2's words, "narrow Precept into a much less credible business-rule language." Second — and more fundamental — **a nonlinear rule is a governance constraint, not a fault-prone operation.** `rule TotalInventoryCost == AverageCost * QuantityOnHand` (`compile-time-prevention-features-proposal-2026-06-06.md` §1.3) is enforced at runtime as a *rule* — the existing, designed governance mechanism (`:268`; the constraint sweep over rules/ensures, `result-types.md:114`) — with a violating operation discarded on the working copy (§3A.4). Pure-B would either refuse the rule (needless expressiveness loss) or pretend to prove it (impossible for a genuine product). Hybrid does neither: the rule is *governed* (as all rules always have been), while its arithmetic sub-expressions remain prove-or-reject for overflow. Pure-B mistakes "governed at runtime as a rule" for "a deferred fault" — they are different mechanisms, and conflating them is what makes pure-B over-strict.

### Leg 3 — Precedent / prior art

- **Precept's own shipped code is already hybrid-shaped.** Fault-class band violations reject at compile time — `NumericOverflow` (`Diagnostics.cs:690`), `OutOfRange` (`:697`), `LengthBoundViolation` (`:1281`), `CountBoundViolation` (`:1293`) are all `Severity.Error`, and `CountBoundViolation` fires on *"a provable violation, or merely unprovable"* (`:1293`–`:1301` TriggerCondition). Governance runs on external input at ingress (`:268`). The running compiler is not Model A anywhere; it is the hybrid boundary already.
- **Owner-ratified narrow precedent.** The count-bound "Reading A" (prove-or-reject, no deferral for cardinality) carries an explicit owner sign-off — *"Owner sign-off 2026-06-03, explicitly framing it as 'yes, unproven divisor'"* (`count-bound-discharge-semantics-2026-06-03.md:297`), shipped as commit `c27a382b`. This is the structurally identical decision at cardinality scope, decided the hybrid way, by the owner.
- **§0.6:249 is live Model-B-discipline shipping today for one axis.** *"An unbounded source flowing into a capped field is a genuine gap that the author closes by declaring the matching bound on the source"* (`precept-language-spec.md:249`), enforced as `Severity.Error` (`Diagnostics.cs:1281`). The forced-rewrite tax is not hypothetical — it is production behavior on the string-length axis, and it has been tolerable.
- **External field (via `interval-vs-value-evaluation-prior-art-2026-06-05.md`).** SPARK/GNATprove use a lightweight interval bound-propagation pass and require *"the author supplies the missing precondition or the proof doesn't discharge"* (`:47`) — total sound bounds-checking, placing Precept *"in the company of Astrée, Frama-C, and GNATprove — the NIST-recognized sound-analysis tools"* (`:151`). Nickel — the field's clean "evaluate contracts against concrete values / defer to runtime" pole — is cited *only to be rejected* (`:130`). SPARK's graduated assurance-level ladder and the gradual-verification literature (Lehmann & Tanter POPL 2017) support the **legibility of a boundary that exists for other reasons** (the disposition surface, Q9) — Reader B's key nuance — *not* "prove-some/defer-the-rest is the right model." I adopt them for Q9 and decline to stretch them into a Q1 endorsement of A.
- **Dhall/CUE/Nickel spread.** The three occupy the poles Precept sits between: Dhall removes fault-prone operations entirely (pure-B pole), Nickel defers to runtime (A pole), CUE unifies constraints without a totality guarantee. Hybrid's "total-internal + governed-external" is the principled middle none of them occupies, and it is the middle the spec already chose.

### Leg 4 — Tradeoff accepted

**Soundness over completeness on the internal surface.** The conservative prover will sometimes reject a safe-but-unprovable internal computation — the author must add the carrying constraint (a source bound, a guard, or a rewrite). This is a real, named ergonomic cost (`count-bound-discharge-semantics-2026-06-03.md:196`: *"a one-time author guard-tax… authors lose the 'unguarded add silently compiles' ergonomic; they gain a structurally-prevented overflow"*). We take it deliberately: it is the price of `:57`'s guarantee being *unqualified*. The corpus evidence says the tax is small (~89% in-fragment; count-bound in-transition corpus cost "zero" today) and the escape valve for the genuinely-nonlinear residual is governance, not rejection, so the austerity of pure-B is avoided.

---

## 3. The admissibility / boundary rule (the anti-slippery-slope device)

The ruling's centerpiece. It must let a reader route **any** future construct to prove-or-reject vs govern-at-ingress **without reopening the debate**. It does so because it routes by **origin**, never by decidability.

### The ingress-origin predicate (decidable from the typed IR, no solver)

> For a checked value `v` and a declared contract `c`, classify `v` as **runtime-governed** if and only if **all four** hold:
>
> 1. **Host-origin.** `v` is a raw value supplied by the host, not produced by evaluating a Precept expression. Its source node is exactly one of: a **construction input slot**, an **event-argument slot**, or a **direct field-edit value**.
> 2. **Ingress edge.** The check occurs at the contract boundary where that raw value enters, *before any Precept action / computed-field / rule / guard expression derives from it* (`precept-language-spec.md:268`, "before any computation derives from it").
> 3. **Same-slot contract.** `c` is the declared contract *of that ingress slot itself* — the event arg's / constructed field's / edited field's own declared type, bound, or qualifier. A stricter downstream target's band is **not** governed merely because the source was external.
> 4. **No expression node.** No AST operator / function / member / conditional / interpolation / action-result / computed-field node sits between the raw ingress source node and the check.
>
> **If any leg fails, `v` is definition-internal and is compile-time prove-or-reject** for every fault or bound obligation it participates in. If unresolved, the definition is **rejected**, naming the bound / guard / source declaration that would make the proof carry.

**Why it is stable and not a spectrum.** The routing criterion is a property of *where the value came from* (a source-node kind plus an "is there an expression node above it" check) — both decidable by inspecting the typed IR / dataflow graph, with no SMT solver and no "how hard is this to prove" judgment. Crucially: **"not statically decidable" is never itself a routing criterion.** Model A slides because every newly-hard-to-prove case is a candidate for deferral; Hybrid cannot slide because difficulty is irrelevant to routing — a hard-to-prove *internal* value rejects, a trivial-to-check *external* value is still governed at ingress. The line does not move when the prover gets weaker or stronger.

This predicate also **dissolves the D2 "carries-proof vs non-carries-proof" split** (see Q8): under Hybrid every internal bound is uniformly prove-or-reject, so there is no fragile sub-classification of bounds that a future obligation change could silently demote (the instability Reader B flagged at `fault-floor-definition-2026-06-11.md:132`, where the carries-proof set "can grow… become proof-carrying").

### Worked examples (route each without reopening the debate)

| Construct | Legs | Route | Disposition |
|---|---|---|---|
| **`default 150` on `max 100`** | A literal default is host-authored but it is a *definition-internal* declared value checked against the field's own band; it is not an ingress value flowing through a contract slot at runtime. Leg 1 fails (it is a compile-time constant of the definition, not a runtime host input). | **Prove-or-reject.** | Provable violation ⇒ **Error** (`OutOfRange`, `Diagnostics.cs:697`). |
| **`set x = <computed>` where `x` has `max 100`** | The RHS is an expression node (Leg 4 fails); the target-band containment is of an internally-computed result. | **Prove-or-reject.** | Provable violation or merely-unprovable ⇒ **Error** (`NumericOverflow`, `Diagnostics.cs:690`; `diagnostic-system.md:163`). |
| **`set Total = Current + Add.Amount`, `Total: max 100`** | `Add.Amount` is an event arg — but the *checked value* against `Total`'s band is `Current + Add.Amount`, which has a `+` node (Leg 4 fails). The arg's **own** contract (e.g., `Amount: money min 0`) is separately governed at ingress. | **Two obligations, cleanly split.** `Add.Amount` vs its own contract = governed at ingress; `Current + Add.Amount` vs `Total.max` = prove-or-reject. | If `Add.Amount` is unbounded above, the target-band proof cannot carry ⇒ **reject**; author bounds `Amount` (`in 0..100`) or guards (`when Current + Add.Amount <= 100`). |
| **Event-arg-derived bound: `field Cap max Limit` where `Limit` is an event arg / field reference** | The *bound declaration* references a runtime value. The declaration's legality is an ingress/field-contract fact (admitted, governed); a fault-prone operation that *depends* on `Cap`'s bound is internal (Leg 4 fails). | **Split:** bound admitted + governed; dependent operation prove-or-reject. | Dependent operation rejects when `Limit` is not itself sufficiently bounded (`dynamic-modifier-bounds-research-2026-06-01.md` Q1 verdict). Rewrite: bound `Limit`. |
| **Unbounded `money` field with no ceiling** | If it feeds *no* fault/bound obligation, no obligation exists — it is simply stored. If it feeds a bounded computation, that computation is internal (Leg 4 fails). | **Stored freely; prove-or-reject only where it feeds an obligation.** | No diagnostic unless it feeds an obligation it cannot discharge ⇒ then reject; author declares a ceiling on the source (`precept-language-spec.md:249` pattern, live today). |
| **Nonlinear governance rule: `rule TotalInventoryCost == AverageCost * QuantityOnHand`** | A `rule` is a governance constraint, enforced post-mutation (`:268`; `result-types.md:114`). Its operands' arithmetic (`*`) is internal (Leg 4 fails) — prove-or-reject for **overflow only**. | **Governed as a rule** (runtime, on every operation, working-copy discard); **arithmetic prove-or-reject for faults.** | Rule expressible, never refused; a violating operation is discarded (prevention via governance, not a deferred fault). Operands must be bounded so the product can't overflow. |

The nonlinear-rule row is the decisive one: it is expressible under Hybrid (governed) and inexpressible under pure-B, and it involves **no fault deferral** (the governance of a rule is not the deferral of a fault) — which is why Hybrid is neither A nor pure-B.

---

## 4. Downstream dispositions Q5–Q11 (resolved under Hybrid)

### Q5 — Proven-violation severity: **reject as Error (definition incoherence).**

A provably-always-violating, reachable internal bound assignment is a definition that can never produce a valid configuration on that path. Under Hybrid it is always internal (a computed or declared assignment is never an ingress value), so it is prove-or-reject and rejects as **Error**. D2's "warn-and-govern for non-carries-proof bands" (`decision-index.md:20`) is **rejected**.

- **Rationale:** A certain, reachable violation is incoherence — Principle 1 (`:92`) makes prevention structural, so a definition that provably produces an invalid configuration must not compile. Demoting it to a warning-plus-runtime-governance both (a) defers a fault-class obligation to a governance surface that does not represent it (`ConstraintKind` has no band member; `ConstraintsFailed` scope excludes bands, `result-types.md:114`/`:121`) and (b) converts a compile-time *certainty* into runtime *advice* — the "false sense of security" Shane named.
- **Alternatives rejected:** (i) **Warn-and-govern (D2)** — rejected: violates `:266` "no deferral," defers to a nonexistent runtime surface, demotes a certainty. (ii) **Silent accept** — rejected: the prevention-vs-detection failure Precept exists to prevent.
- **Precedent:** HEAD already rejects — `OutOfRange`/`NumericOverflow`/`Length`/`Count` all `Error` (`Diagnostics.cs:697`/`:690`/`:1281`/`:1293`); owner-ratified count Reading A (`count-bound-discharge-semantics-2026-06-03.md:297`); SPARK rejects an unprovable range check rather than deferring it (`interval-vs-value-evaluation-prior-art-2026-06-05.md:47`).
- **Tradeoff:** None material — a *provable always-violation* is never a safe program; rejecting it costs no legitimate expressiveness. (The completeness tradeoff lives in Q7, the merely-unprovable case.)

### Q6 — Default-vs-set-action consistency: **consistent — both internal, both prove-or-reject, both Error.**

Position does not change disposition. `default 150` on `max 100` → `OutOfRange` Error (`Diagnostics.cs:697`); `set x = 150` on `max 100` → `NumericOverflow` Error (`Diagnostics.cs:690`; the partition is documented at `diagnostic-system.md:163` — *"a declared default value that violates a numeric bound is `OutOfRange`; a computation result… that exceeds a field's bounds is `NumericOverflow`"*). Both are internal values, both prove-or-reject, both `Severity.Error`. The differing **code** is a subject-category partition (legibility), not a disposition inconsistency — `diagnostic-system.md:180` confirms the intended rule: *"Fault-prevention obligations block; structural-soundness diagnostics report."* Under Hybrid this is locked: any value that is not ingress-origin gets the same disposition (reject) regardless of whether it arrived via `default` or `set`; the two codes remain, distinguished only by subject for author legibility.

### Q7 — The merely-unprovable case: **reject (Error), name the carrier — same as HEAD, same as cardinality.**

An internal computation whose bound obligation is *merely unprovable* (not provably-violating, just not provably-safe) is rejected, naming the bound/guard/source that would discharge it. This reconciles exactly with cardinality, which is this case: `CountBoundViolation` fires on *"a provable violation, or merely unprovable"* (`Diagnostics.cs:1293`–`:1301`) and `:258` says count is *"never deferred to a runtime check."*

- **Rationale:** Soundness. A merely-unprovable internal bound is *"not statically guaranteed"* (the diagnostic's own words) — admitting it is exactly the unproven evaluation fault `philosophy.md:57` says a clean compile does not contain. Principle 7 (`:104`): the compiler *"does not guess."*
- **Alternatives rejected:** (i) **Defer to runtime (A)** — no runtime surface + violates `:266`. (ii) **Infer a weaker satisfiable bound** — unsound (invents a constraint the author did not declare).
- **Precedent:** `CountBoundViolation`/`LengthBoundViolation` both fire on merely-unprovable at HEAD; owner-ratified count Reading A; SPARK "the proof doesn't discharge" ⇒ reject.
- **Tradeoff:** Over-rejection of safe-but-unprovable internal computations — the author supplies the carrying constraint. Accepted (soundness > completeness; Principles 10/11, `:110`/`:112`).

### Q8 — Carries-proof bounds: **stay prove-or-reject under every model; and Hybrid dissolves the fragile split.**

A bound that also feeds a fault obligation (a `max` that also bounds an index; a divisor relation from a rule — `:258`: *"a divisor over a subtraction of two related fields… is provably non-zero… and discharges"*) is prove-or-reject. Affirmed. **The exact test:** any bound whose value is consumed by a fault-prevention obligation discharge is prove-or-reject — but under Hybrid this is *subsumed*, because **all** internal bounds are uniformly prove-or-reject. There is no "carries-proof vs non-carries-proof" partition to keep honest.

- **Rationale:** A bound consumed by a fault obligation is load-bearing for safety; demoting it reintroduces the fault. Hybrid's uniform internal prove-or-reject makes accidental demotion structurally impossible.
- **Alternatives rejected:** **D2's carries-proof/non-carries-proof split** — rejected because the carries-proof set is *not author-stable*: it grows and shrinks as the obligation set changes (`fault-floor-definition-2026-06-11.md:132`; and the plan's own note that parking overflow *"no longer pulls any bounds proof-carrying"*). Any split keyed on it is a snapshot that must be re-derived whenever the floor's obligations change — a permanent slippery-slope generator. Hybrid removes the generator.
- **Precedent:** `:258` relational reasoning already treats rule-sourced relations as proof contributors; the HEAD prover discharges divisor safety from `rule X > Y`.
- **Tradeoff:** Uniform prove-or-reject means even a band that feeds no *current* obligation is prove-or-reject (slightly stricter than D2's attempt to relax "pure documentation" bands). Accepted: a declared band is a claim the author wants enforced, and the stability gain (no fragile split) dominates.

### Q9 — False-promise resolution: **ship the per-obligation disposition surface — as the realization of inspectability, not a patch.**

The boundary must be legible. Under Hybrid the false promise is already **removed by construction** (like B: nothing internal is deferred, so "compiles clean" = "totally proven internally"). What remains is to make the *two-mechanism* boundary visible per obligation. This is **required** by existing principles, not a new concession:

- Principle 4 (`:98`): *"Inspectability extends to proof reasoning — proven ranges, source attribution, and what the engine could not prove must all be surfaceable through diagnostics, hover, and tooling."*
- §0.8 item 4 (`:292`): *"No opaque proof. The proof engine's obligations and discharges are inspectable — `precept_proofs` MCP tool surfaces them; LS hover surfaces them."*
- Principle 8 (`:106`): the exact/approximate — here, proven/governed — line *"must be visible in the type system and the language surface."*

**Ruling:** ship a per-obligation disposition surface that renders, for each obligation, **PROVEN** (compile-time, with source attribution) or, for each genuine ingress constraint, **GOVERNED** (runtime, at the named ingress slot). It has **no "DEFERRED" state** — because Hybrid has no deferred-internal bucket, a "deferred" entry would be a false category. Expose via `precept_proofs` + LS hover (the surfaces `:292` already names).

- **Rationale:** The two-mechanism split is real; Principle 4 and §0.8.4 *require* it to be surfaceable; Principle 8 requires the proven/governed line to be visible.
- **Alternatives rejected:** (i) **No surface** — violates Principle 4 / §0.8.4. (ii) **A tri-state proven/deferred/governed** — rejected: "deferred" is a category Hybrid does not contain; rendering it would re-introduce the false promise the model eliminates.
- **Precedent:** HEAD already surfaces proofs via `precept_proofs` + hover (§0.8.4); the SPARK-assurance / gradual-verification legibility literature supports *legibility of an existing boundary* (Reader B's nuance) — adopted here for exactly that.
- **Tradeoff:** New structured-data surface to build and keep in sync (must be structured data, not parsed prose — §0.6 item 6, `:231`). Accepted.

### Q11 — Taxonomy (GATE-H) + adjacent D1/D3: **three diagnostic categories confirmed; add the governance disposition as a non-diagnostic surface; D1/D3 stay scoped-settled.**

The three-category taxonomy holds under Hybrid, grounded in HEAD severities I re-confirmed:

| Category | Severity | Members (HEAD-confirmed) |
|---|---|---|
| **Fault floor** (a fault-prone op over an internal value) | **Error**, prove-or-reject | `DivisionByZero`, `NumericOverflow` (`:690`), `OutOfRange` (`:697`), `SqrtOfNegative`, `LengthBoundViolation` (`:1281`), `CountBoundViolation` (`:1293`), empty-collection/index access |
| **Definition incoherence** (structural contradiction) | **Error** | `UnsatisfiableInitialState` (`:867`), `DefaultViolatesRule` (`:877`), provably-always-violating internal assignment, unreachable state, dead-end |
| **Flag layer** (proven structural certainty, no fault/incoherence) | **Warning** | `UnsatisfiableGuard` (`:770`), `VacuousRule` (`:792`), `ContradictoryRule` (`:802`), `TautologicalGuard` |

Hybrid adds a **fourth, non-diagnostic** row: the **ingress-governance disposition** (a runtime obligation, surfaced in the Q9 disposition view, *not* a diagnostic). This keeps the three-way *diagnostic* taxonomy intact while naming the governance boundary explicitly.

**D1 (overflow parked) and D3 (atomicity relaxed)** stay in their current scoped-settled disposition (`decision-index.md:19`/`:21`; committed via `2df5b37d`) and are **not reopened** by this ruling. One honest flag for Shane, not a reopening: overflow of an *internal* computation is a fault (`NumericOverflow` Error at HEAD, Principle 10 lists it) and is therefore prove-or-reject under Hybrid; if any reading of D1's "park" implies deferring an internal overflow fault, that reading conflicts with Hybrid and should be reconciled when D1 is next touched — but the ruling does not disturb D1's current disposition.

### D1/D3 adjacency and GATE-H taxonomy are **confirmed, not reopened.** The band/deferral axis is this ruling's scope; overflow-parking and atomicity-sequencing are settled elsewhere and inherited as-is.

---

## 5. Q3 — Expressiveness cost (the accepted tradeoff, honestly enumerated)

What becomes inexpressible under Hybrid, and the author's rewrite path:

| Pattern | Under Hybrid | Author's rewrite | Cost |
|---|---|---|---|
| **Open-ended numeric feeding a bounded computation** | The *field* is fine (stored freely); the *computation* that must fit a bound rejects if the source is unbounded. | Declare a ceiling on the source, or guard the operation. | Small — only where the value actually feeds an obligation. |
| **Runtime-value-dependent bound** (`min Floor`, `max Limit`) | Bound *declaration* admitted + governed; dependent fault-prone operation prove-or-reject. | Bound the reference field (`Floor`/`Limit`) sufficiently. | The `dynamic-modifier-bounds` pattern — already reconciled with §0.7. |
| **Unbounded string source into a capped field** | Reject (already live). | Declare the matching bound on the source (`:249`). | **Already shipping** as `Severity.Error` (`Diagnostics.cs:1281`) — proven tolerable. |
| **Genuinely nonlinear governance rule** (product/ratio/balance identity) | **Expressible — governed as a rule.** | None beyond making operands fault-safe (bounded so no overflow). | **Zero expressiveness loss** — this is where Hybrid beats pure-B decisively. |
| **Aggregate functions `sum`/`reduce`** | Do not exist today (PRE0030, `compile-time-prevention-features-proposal-2026-06-06.md` §1.3); orthogonal to the model choice. | Paired-action bookkeeping (as real samples already do), governed by the sweep. | Pre-existing; not introduced by this ruling. If added later: computed-and-feeding-an-obligation ⇒ prove-or-reject; maintained-by-paired-actions ⇒ governed. |

**Is the loss acceptable for real precepts?** Yes. The corpus audit puts ~89% of real invariants in-fragment (`precept-guarantee-specification-2026-06-05.md` §7); the count-bound in-transition corpus cost is "zero" today; the string-length forced-rewrite already ships without complaint. The one genuinely-common residual — nonlinear governance rules (21/77 sample files) — is **not lost**, because Hybrid governs it rather than refusing it. The accepted tradeoff is therefore the *guard-tax on internal fault-prone computations over unbounded sources* — real, small, and the direct price of `:57`'s unqualified guarantee.

---

## 6. Q10 — Owner-gated recommendations (verbatim; recommend, do not edit)

**Headline: the ruling requires NO weakening of the spec or philosophy.** HEAD `§0.7:266`/`:268` and `§0.6:258` are already Hybrid-shaped; the load-bearing owner-gated act is to **decline** the D2/GATE-F amendment that would have introduced deferral, and to *optionally* adopt one precision clause that hard-codes the anti-slide predicate into the spec. I recommend; Shane ratifies.

### R1 — DECLINE the GATE-F / D2 amendment to §0.7 (keep the "no deferral" clause verbatim)

**Proposed (by D2/GATE-F) — to be DECLINED:** demote non-carries-proof band violations from rejection to a proven-violation warning with runtime enforcement deferred (`decision-index.md:20`), which requires amending `:266`'s *"there is no deferral"* clause.

**Recommendation — keep `:266` exactly as written:**

> **§0.7:266 (KEEP, unchanged):** "It never compiles a fault-prone operation in the hope a runtime check catches it; **there is no deferral**. (Principles 7, 10, 11.)"

*Rationale for Shane:* this is your own settling commit (`5af46537c`, author `sfalik`, 2026-06-02, message *"Settles the long-standing ambiguity… fault prevention = compile-time prove-or-reject, never defer"*). The ruling ratifies it; it does not overturn it.

### R2 — OPTIONAL precision clause on §0.7:268 (owner-gated addition; the anti-slide device in the spec)

**Before (`:268`, current):**

> "**Governance — enforced at runtime on external input.** Every declared constraint is enforced on every value entering the entity from outside the definition — event arguments, construction inputs, direct field edits — at the moment it enters, before any computation derives from it."

**After (recommended — one clause added; nothing removed):**

> "**Governance — enforced at runtime on external input.** Every declared constraint is enforced on every value entering the entity from outside the definition — event arguments, construction inputs, direct field edits — at the moment it enters, before any computation derives from it. *A value is external input only where it enters raw at its ingress slot and is checked against that slot's own declared contract with no Precept expression between the raw input and the check; a value produced by evaluating a Precept expression — an assignment RHS, computed field, or aggregate — is definition-internal and is proven at compile time (Fault prevention, above), never governed as external input, even when one of its operands was external.*"

*Rationale for Shane:* this makes the ingress-origin predicate canonical, so "external input" can never be mis-read to include an internally-computed-but-runtime-derived value — the exact slide (`set Total = Current + Add.Amount` deferring `Total`'s band because an operand was external) that would collapse Hybrid into A. It is a **clarification of existing intent**, not a change of behavior.

### R3 — §0.6:258 (KEEP, unchanged)

> **§0.6:258 (KEEP):** "…count is 'no result outside a declared bound', discharged by an author guard, **never deferred to a runtime check**."

No change; the cardinality axis already embodies the ruling.

### R4 — philosophy.md:49 — recommend NO edit (the absolutist copy is honest under Hybrid)

`philosophy.md:49` (*"Prevention, not detection. No errors. No bugs…"*) is **not** an over-read under Hybrid and needs **no narrowing**. Under Hybrid: internal faults are prove-or-reject (they cannot occur), and invalid external configurations are discarded on the working copy before they persist (`:268`; §3A.4) — so *"these configurations and workflows cannot exist"* is literally true by **prevention**, not detection. The precise enumeration at `:57` scopes the compile-time half correctly and is likewise unchanged.

> **Recommendation:** leave `docs/philosophy.md` untouched. The `:49`/`:57` pair is already consistent with the ruled model. (Per CLAUDE.md, any philosophy edit is owner-gated regardless; here I recommend none.)

*One place a reader might think narrowing is needed, and why it isn't:* a nonlinear rule "governed at runtime" might look like detection. It is not — a violating operation is discarded on the working copy so no invalid configuration ever persists (`:268`). That is prevention via governance, exactly what `:49` describes.

---

## 7. Provenance note (this ruling settles an unratified proposal — it does not overturn an owner decision)

Two facts, both re-confirmed first-hand, are load-bearing for the legitimacy of ruling as I have:

1. **"No deferral" is original owner intent, not a late AI insertion.** `git blame` puts `§0.7:266` at commit `5af46537c`, author/committer `sfalik` (the owner's identity), 2026-06-02, in a deliberate *"Settles the long-standing ambiguity"* commit; `§0.6:258` at `c27a382b0`, same author, reinforcing it with shipped feature work. Both are AI-co-authored (Claude Opus 4.8 trailer) but committed under the owner's identity as explicit settling acts. The spec is already B/hybrid-shaped **by the owner's own hand.**
2. **D2 (the "band split" toward A) was never authoritatively ratified.** The record Phase-0 cited (`decision-index.md:20`) is **git-untracked** (`?? docs/Working/decision-index.md`; `git ls-files --error-unmatch` fails; no commit history) — zero git provenance. The only committed assertion is the readiness plan's own prose (via `2df5b37d`, a broad AI-co-authored docs revision, not a discrete ratification artifact), and the two owner-commissioned 2026-07-03 analyses **openly contradict each other** on D2's status (`band-guarantee-boundary-analysis-2026-07-03.md:3` "NOT owner-ratified" vs `d2-proven-violation-philosophy-analysis-2026-07-03.md:99` "owner-locked"). And it is **unshipped**: `CountBoundViolation`/`LengthBoundViolation` remain `Severity.Error` at HEAD, `CountBoundViolation` firing on the merely-unprovable case.

**Therefore this ruling is not overturning a prior owner decision.** It ratifies the owner's committed "no deferral" intent and *settles* the unratified, uncommitted, contradicted, unshipped D2 proposal by declining it. The one genuinely-owner-decided adjacent items (D1 overflow-park, D3 atomicity) are left untouched.

---

## 8. Supersedes

This ruling replaces the scattered analyses that circled the question:

- `docs/Working/band-guarantee-boundary-analysis-2026-07-03.md` — its reject-vs-govern crux is resolved here (reject; internal) and its SPARK/gradual-verification precedent is adopted for Q9 legibility only.
- `docs/Working/d2-proven-violation-philosophy-analysis-2026-07-03.md` — its "D2 is owner-locked" premise is corrected (D2 is unratified) and its philosophy analysis is superseded by §2 and §6/R4 here.
- `docs/Working/prove-govern-classification-and-legibility-2026-07-06.md` — **as the model ruling** (its "govern the runtime-dependent case" classifier is replaced by the ingress-origin predicate, which is decidable and non-sliding); **its disposition/legibility surface is adopted** (Q9). Its "was a shipped warning" premise was factually wrong at HEAD (Reader E, §6.1) and is not relied upon.
- `docs/Working/decision-index.md` **D2 (band-split)** — declined as an unratified proposal (R1). D1/D3 rows are undisturbed.

The 2026-07-03 and 2026-07-06 docs may be moved to `docs/Working/Superseded/` on ratification, with a cross-link back to this ruling.

---

## 9. Ratification checklist for Shane (one pass)

Sign off, redirect, or request revision on each owner-gated item:

- [ ] **The model.** Ratify **Hybrid** (total-internal prove-or-reject + governance only at the ingress-origin boundary) as the model that governs Precept. (§2)
- [ ] **The boundary predicate.** Ratify the four-leg **ingress-origin predicate** (§3) as the canonical, non-sliding admissibility rule.
- [ ] **R1 — Decline D2/GATE-F.** Confirm `§0.7:266` "no deferral" stays verbatim; the band-split is declined. (§6 R1)
- [ ] **R2 — Precision clause (optional).** Adopt / decline the recommended one-clause addition to `§0.7:268` that makes the ingress-origin predicate canonical. (§6 R2)
- [ ] **R4 — Philosophy.** Confirm **no** edit to `docs/philosophy.md:49`/`:57` (recommended: leave untouched). (§6 R4)
- [ ] **Q5/Q7 severities.** Confirm proven-violation and merely-unprovable internal bounds stay **Error** (reject), matching HEAD. (§4 Q5, Q7)
- [ ] **Q9 disposition surface.** Approve building the per-obligation PROVEN/GOVERNED disposition surface (no DEFERRED state) on `precept_proofs` + hover. (§4 Q9)
- [ ] **Q11 taxonomy.** Confirm the three diagnostic categories + the non-diagnostic governance disposition; confirm D1/D3 stay settled/not-reopened. (§4 Q11)
- [ ] **Phase 5 authorization.** On ratification, authorize the compiler-completion re-scope under Hybrid (route through `/plan`; runtime-governance obligations captured-only). (§10)

---

## 10. Bridge to compiler-completion (Phase 5) — implications, not the plan

Captured for the Phase-5 `/plan` pass; not the plan itself.

- **The compiler's target is totality over the definition-internal surface.** "Finishing the compiler" means: every fault-prone operation and every bound obligation over an internally-computed value is discharged prove-or-reject. The fault floor (div-by-zero, overflow, empty-collection, sqrt<0, out-of-declared-bound results, index/key) is the completion target; bands are *in* it for internal values, not split out.
- **Kill the carries-proof/non-carries-proof machinery.** The ingress-origin predicate makes it unnecessary; do not build the fragile split D2 implied. This *simplifies* the checker (one rule: internal ⇒ prove-or-reject).
- **Build the ingress-origin classifier in the typed IR.** A single dataflow pass tags each checked value's source-node kind and whether an expression node intervenes — the decidable routing device. This is the concrete new work the predicate implies, and it needs no solver.
- **Build the Q9 disposition surface as structured data.** Per-obligation PROVEN (with source attribution) / GOVERNED (ingress slot named); wire to `precept_proofs` (MCP) and LS hover (`docs/tooling/mcp.md`, `docs/tooling/language-server.md` doc-sync). No DEFERRED state exists.
- **Runtime governance is captured-only for the future runtime build.** The runtime obligation is *narrow and buildable*: enforce each declared constraint on raw external input at its ingress slot (event args, construction inputs, direct edits), on the working copy with discard (`:268`, §3A.4). No internal-band deferral surface is ever built — that is the Model-A machinery Hybrid does not need. Record this in `docs/runtime/evaluator.md` (intake boundary) and `result-types.md` as a captured obligation, not active work.
- **Nonlinear governance rules need no new compiler capability.** They are rules — already governed. The only compiler obligation is fault-safety of their arithmetic sub-expressions (overflow prove-or-reject), which the fault floor already covers.
- **Doc-sync obligations on ratification:** `precept-language-spec.md` §0.7 (R2 clause, if adopted); `docs/compiler/proof-engine.md` (ingress-origin predicate + uniform internal prove-or-reject; retire the carries-proof split); `docs/compiler/diagnostic-system.md` (three-category taxonomy + governance disposition); `docs/tooling/mcp.md` + `docs/tooling/language-server.md` (disposition surface). All are the same edit pass as the code, per the documentation-sync non-negotiable.
- **What Phase 5 must decide (not decided here):** the exact structured-data schema of the disposition model; whether `sum`/`reduce` are ever admitted (and if so, prove-or-reject vs governed per the predicate); D1's overflow-park reconciliation with uniform internal prove-or-reject (flagged in Q11, not reopened here).

---

## Appendix — Ruling checked against every §0.1 principle

| # | Principle (`precept-language-spec.md`) | Served / Tension | Note |
|---|---|---|---|
| 1 | Prevention, not detection (`:92`) | **Served** | Internal faults can't occur (prove-or-reject); invalid external configs discarded pre-persist (`:268`). |
| 2 | One file, complete rules (`:94`) | **Served** | The boundary lives inside the definition's own ingress contract — no external validators. |
| 3 | Deterministic semantics (`:96`) | **Served** | Routing is a syntactic/dataflow fact; no solver, no non-determinism. |
| 4 | Full inspectability (`:98`) | **Served — and requires Q9** | *"what the engine could not prove must be surfaceable"* → rejections name the carrier; disposition surface mandated. |
| 5 | Keyword-anchored readability (`:100`) | Neutral | Unaffected. |
| 6 | Explicit domain meaning (`:102`) | Served | Bounds are domain contracts, uniformly enforced. |
| 7 | Compile-time-first (`:104`) | **Served — tradeoff** | *"proves what it can, rejects what it can prove invalid, does not guess"*; over-rejects safe-but-unprovable internals (accepted). |
| 8 | Approximation honesty (`:106`) | **Served** | The proven/governed line is visible in the surface (Q9 disposition). |
| 9 | Mandatory rationale (`:108`) | Neutral | Bounds carry generated rationale; unchanged. |
| 10 | Totality (`:110`) | **Served — tradeoff** | Every internal expression prove-or-reject; conservative rejection is the accepted cost. |
| 11 | Static completeness (`:112`) | **Served** | Clean compile ⇒ no internal fault; governance makes ingress-carried constraints true — *"without deferral: §0.7."* |

No principle is violated. Model A would violate 1/7/10/11 (deferral makes runtime the primary enforcement). Pure-B violates none but pays an expressiveness cost that is a product-quality loss, not a principle gain. Hybrid serves all eleven and takes the soundness-over-completeness tradeoff on 7/10 knowingly.

---

*End of ruling. Draft — 2026-07-06, pending Shane ratification. Not Locked.*
