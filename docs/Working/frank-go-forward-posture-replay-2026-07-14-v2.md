---
title: Frank's Go-Forward Posture Replay — v2
date: 2026-07-14
author: Frank (Lead/Architect & Language Designer)
status: Working — posture confirmation checkpoint, v2 (supersedes v1 for posture purposes; not yet promoted to canon)
owner: Shane
supersedes: docs/Working/frank-go-forward-posture-replay-2026-07-14.md (v1)
synthesizes:
  - docs/Working/posture-v2-support/frank-self-critique-2026-07-14.md (Frank — findings H1–H4, M1–M3)
  - docs/Working/posture-v2-support/citation-audit-2026-07-14.md (Fact Checker — 29 ✅, 2 ⚠️)
  - docs/Working/posture-v2-support/coverage-matrix-2026-07-14.md (Soup Nazi — 107 items; 68 dropped, 7 hedged, 5 contradicted)
  - docs/Working/posture-v2-support/blind-reconstruction-2026-07-14.md (George — independent cross-check)
  - docs/Working/posture-v2-support/devils-advocate-2026-07-14.md (Fact Checker DA — 5 unclosed openings)
note: >
  Documentation-only. No source code inspected. The changelog appendix maps every change to its
  source, so the rewrite itself is auditable. Where a source item is deliberately out of the
  posture's scope, §12 says so explicitly rather than dropping it silently.
---

# Frank's Go-Forward Posture Replay — v2

Requested by Shane, verbatim: *"ask frank to replay what he believes the go-forward posture is."* No hedging. This is the record — corrected, completed, and closed against every rival reading the source corpus still permits.

**How to read this.** §1–§6 are the posture proper (what Precept is, what it guarantees, how obligations route, how runtime governance works). §7–§8 are the machinery that makes the posture *legible and bounded* (the diagnostic surface; the certificate criterion and the stopping rule). §9 is the deferred/parked surface, stated openly. §10 is the banned-framings list. §11 is why this was hard-won and how we do not spiral again. §12 is what I am deliberately holding out of scope. §13 is what I want **you** to rule rather than have me decide silently. The appendix is the changelog.

---

## 1. What Precept is

Precept is a **domain integrity engine**. You declare — in one file — what a business entity's data is, what it is allowed to become, and the rules that must always hold over it. Precept then makes every state of that data that violates those rules **structurally impossible to reach**: at compile time where it can prove impossibility, and by structural runtime enforcement everywhere else. Governance, not validation — there is **no unchecked path**, no boundary where enforcement can be bypassed (`compiler-and-runtime-design.md`; `philosophy.md:49`). States are the coordinate system, not the point; **stateless precepts are first-class** (`philosophy.md`).

## 2. What Precept guarantees

**No invalid configuration. Ever — across the surface the guarantee enforces today.** Not "caught." Not "detected." Not "usually." A configuration that violates a declared rule cannot come into existence — no operation, input, code path, or bypass produces one. *"No operation can produce a result that violates a declared rule — the invalid configuration is not reachable"* (`philosophy.md:51`). If Precept accepts a definition, every value that definition can ever hold satisfies every rule declared over it, enforced structurally on every operation — unconditionally or under precise guard conditions.

**One disclosed carve-out, stated plainly because honesty is not hedging: representational overflow.** A computed value exceeding what the *type itself* can represent (`NumericOverflow`) is **owner-parked** — *"a disclosed, defense-in-depth known-fault backstop with no live compile-time guarantee — not currently prove-or-reject, temporary, and no doc may claim it prevented"* (`proof-engine-boundary-ruling-2026-07-06.md:25`; re-confirmed `:275`). This is a *different fault* from declared-bound containment: representational overflow is value-vs-**type-range** (`NumericOverflow`); containment is value-vs-**declared-`min`/`max`** (`OutOfRange`), which stays **enforced** (`boundary-ruling:25`).

**Name the tension plainly, because it is deliberate.** Two source-level facts are simultaneously true and must be stated together, or a reader re-imports overflow into the guarantee as enforced today:

1. The boundary ruling says overflow is parked and *"no doc may claim it prevented"* (`boundary-ruling:25`, `:275` R6).
2. Shane **ruled the present-tense overflow claims in canon stay as-is** — they state the intended end-state; *"the gap is build-order, not a text defect"* (`proof-engine-decision-ledger-2026-07-12.md` #7). Overflow-prevention **is** the ratified target, sequenced post-MVP as **GATE-O**.

So `philosophy.md:53` and spec Principles 10/11 (`:110`/`:112`) legitimately carry present-tense overflow language as **target**, while this posture excludes it from what is enforced **today**. This is not drift and not an accident: it is an **owner policy choice** to preserve target-language, made *over* the boundary ruling's own recommendation to surface/reconcile the text (`boundary-ruling:275` R6) and *over* the identity/philosophy-reconciliation critique that flagged `:53` as an unresolved present-tense overclaim (`critique-c3-philosophy-reconciliation.md`). I record the disagreement as decided, not as absent: **the posture excludes representational overflow from what is enforced today; canon keeps target-language for it by owner ruling.** Anyone citing canon to say "overflow is inside the enforced guarantee" is reading target-language as if it were in force today, which #7 forbids.

*(The number-model prerequisite under overflow — the shared numeric core the eventual proof leans on — is a real build blocker, not just a text question; noted so it is not mistaken for pure documentation, `critique-c3-philosophy-reconciliation.md`.)*

*(Minor: the overflow strategy is itself unchosen — fixed-width handling (trap/saturate/wrap) vs. arbitrary precision (grow the representation so overflow can't occur) — which is a further reason the question is deferred past the MVP.)*

## 3. The governing model, and what must be proved — of what

**The model is Hybrid** (`boundary-ruling:§3`, ratified as the package within which prove-or-reject was ratified, `decision-ledger` #1). The compiler is **"total over the fault surface"** — *not* "every declared constraint, full stop." Inverting that — treating the compiler as total over *every* declared constraint — is the error to guard against.

**Keep two things separate: what the ratified design targets, and what is built today** (retrospective §2 lesson 4).

**What the design targets (ratified).** Every fault-prone operation over a *definition-derived* value in the **set of fault kinds enforced at compile time today** is compile-time **prove-or-reject** — discharged from the constraints its inputs carry, or the definition is rejected naming the carrier. That set is:

- **division** (division-by-zero),
- **`sqrt`/`pow` non-negativity**,
- **empty-collection / index access**,
- **declared-bound containment** of a *computed result* within a declared `min`/`max` (`OutOfRange`), and
- **cardinality / count-bound containment** — a derived count against `mincount`/`maxcount`/`minlength`/`maxlength` (`CountBoundViolation`/`LengthBoundViolation`, `boundary-ruling:25`, worked row 14; spec:258).

The last one matters: the **count family is inside that enforced set, not adjacent to it** — and it is the source of the *nearest owner language decision* we have, count "Reading A" (owner-signed, shipped `c27a382b`), which chose reject-the-merely-unprovable at this exact scope (`boundary-ruling:§8`).

**What is built today.** The engine *"understands a relational rule only in its simplest shape: one field compared to one field (`rule X >= Y`)"* (`proof-engine-linear-solver-feasibility-2026-07-12.md:22`) plus the faults it already enforces. **§1a** (multi-term linear facts, one at a time), **§2** (case-by-case reasoning), **§3** (money through × and ÷), and **§6** (the author states the fact as one rule; the engine applies it) are **MVP scope, designed-not-built** (`proof-engine.md:2609`). A claim that "the compiler proves everything decidable" describes neither today's build nor the target — it overclaims both.

**A declared relational invariant is GOVERNED, not proven — and the routing axis is provenance, not linearity.** A declared relational invariant over independently-set fields — whether **nonlinear** (`rule TotalInventoryCost == AverageCost * QuantityOnHand`) or **linear** (`rule Balance == Deposits - Withdrawals`) — is *"Expressible — governed,"* with *"Zero expressiveness loss"* (`boundary-ruling:226`). It is governed, not proven, because it **declares a relationship** rather than **computing a derived value** — there is no derived-value fault obligation to discharge, only a relationship to maintain. The linear identity is governed for the identical reason (`boundary-ruling:160`, worked row 11). Routing is by **provenance, not linearity**: the linear-solver work (§1a/§1b) concerns *discharging fault obligations using facts as premises*, not *proving relational invariants* — so linearity was never the reason a relational invariant is unproven; the reason is that a relationship among independent fields has no derived-value obligation to discharge. The invariant's fault-prone *sub-operations* (a division inside the formula) remain prove-or-reject over their derived operands (`boundary-ruling:130`). *"21 of 77 sample files use products"* (`boundary-ruling:25`) and are honored this way.

**The load-bearing condition — do not drop it.** Prove-or-reject is honest **only as the ratified package**: over the *strengthened* engine (§1a/§2/§3/§6) plus the certificate criterion (§8). Re-assert it over June's engine and you get June back — rejecting the corpus's own house style (`decision-ledger` #1: *"A is honest only as a package with items 2 and 3"*; `frank-canonical-capture-recommendation-2026-07-14.md:56`). The posture is locked; its *honesty is contingent on the engine landing.*

## 4. The discriminating axis — obligation-role, operationalized as provenance

This is where re-litigation keeps restarting, so I state it as the boundary ruling states it, then operationalize it, then **name and reject every rival reading the sources still permit.**

**The formal frame is the Obligation-Role Rule** (`boundary-ruling:§4`): disposition is a property of an **obligation**, never of a construct. There are exactly two obligation families and one oracle:

- **Family A — every declared constraint (modifier or rule, indistinguishably) generates two obligations, always:** (A.1) **governance** — enforced on raw external values at their ingress slot, and on the resulting whole-entity state at commit; and (A.2) **premise** — it contributes its provable fact to the proof engine to the extent it is decidable. The carries-proof/non-carries-proof split **dissolves**: a constraint is never "routed," it always does both (`boundary-ruling:§4`, Q8).
- **Family B — every fault-prone operation over a *definition-derived* value carries one obligation: compile-time prove-or-reject** (the enforced fault set per §3), *"routed by the origin/shape of the value it consumes … never by how hard `O` is to prove"* (`boundary-ruling:§4`) — with representational overflow the one parked exception.
- **The oracle is decidability**, and it selects **discharge vs. reject** for a family-B obligation — it is *"the discharge-oracle … not the router"* (`boundary-ruling:39`).

**The operational question — "provenance" — is the practical shorthand for "does this operand carry a family-B obligation?":**

> **Where did the operand's value come from?**
> - **Derived by a Precept expression → its FAULT OBLIGATIONS are PROVE-or-REJECT.** If the constraints its inputs carry decide the obligation, it discharges; otherwise the definition is **REJECTED** (the author adds the carrier via §6, or bounds the source). Decidability decides **discharge-vs-reject only** — never route-to-governance.
> - **Not a derived-value fault obligation → GOVERN it, structurally.** Two mechanisms, both governance (§5): a value *supplied externally* is enforced at **ingress**; a *declared relational invariant over independently-set fields* is enforced by the **post-mutation sweep**.

**Rival readings the sources still permit — named and rejected:**

- ❌ **"Route by proof-carrying vs. non-proof-carrying."** This was the *fence around the historically-contested subset* in my 07-11 position (proof-carrying bounds were uncontested; only non-proof-carrying derived writes were in dispute) — it was **never the model's router.** The boundary ruling **explicitly rejected** decidability/proof-carrying as the router (`boundary-ruling:39`), and all derived-value obligations prove-or-reject **regardless** of whether the target bound is proof-carrying. *Self-correction on the record:* my own `frank-canonical-capture-recommendation-2026-07-14.md` §1a still calls the boundary "proof-carrying vs. not" and its Step-1 plan would **promote that overruled framing to canon** — that is a **drift I am flagging against my own doc**; the ratified router is **provenance**, and the capture doc must be corrected to say so before anything is promoted. (I do not edit the capture doc here; §13 carries the action.)
- ❌ **"There are three routes (prove / ingress-govern / sweep-govern)."** No — there are **two routes on the provenance axis** (prove-or-reject vs. govern); *govern* is one route **delivered by two mechanisms.** Separately, the parked-overflow lane is a **disclosed fourth disposition** that sits **outside the routing axis entirely** — neither proven nor governed, a disclosed gap (`boundary-ruling:25`). So: two active routes, one of them two-mechanism, plus one out-of-axis disclosed park. That is the exact count; "two-way, not three" means *no third **govern-the-undecidable-computed-value** middle arm* — it does not deny the overflow park.
- ⚠️ **"Route by surface spelling."** This is the real, narrow ban. *"`max 100` and `rule x <= 100` are the same construct and get the same treatment"* (`boundary-ruling:134`); a disposition keyed on modifier-vs-rule *"is incoherent"* (`boundary-ruling:37`, quoting `spec:1138`). Position is inert too: `default 150` on `max 100` and `set X = 150` get the **same** Error (`boundary-ruling` Q6). Same provenance ⇒ same disposition, however spelled or positioned.

**The `set` vs. `rule` question, met head-on.** A careful author will notice that `set Balance = Deposits - Withdrawals` (rejected if unprovable) and `rule Balance == Deposits - Withdrawals` (governed) get **opposite** dispositions, and ask whether the "spelling-invariant" claim is a lie or whether the reject is *evadable* by rewriting one into the other. Answer, precisely:

- These differ by **provenance/role, not spelling.** In `set X = e`, X's value is **computed** — X is a derived field, and the computed result carries a family-B obligation. In `rule X == e`, X is **independently-set** and the rule merely constrains a relationship among independent fields — no derived value is computed into X, so no family-B obligation exists (`boundary-ruling:160`, worked row 11). The role of X genuinely differs; this is provenance, not syntax.
- The "evasion" **does not launder a computed value into a governed one** — it *redesigns the entity.* Rewriting `set X = e` into `field X editable` + `rule X == e` makes X an externally-settable field that is now **governed at its own ingress** *and* swept for the identity — the author has changed what X *means* and taken on the obligation to supply/edit it. Enforcement is not escaped; the definition is different. There is **no restructuring that carries a computed value to runtime without proof** — that is the escape hatch, and it is closed (#9, §9).
- **Conceded cost, placed where it belongs.** The domain author *can* be surprised that two spellings of a similar-looking intent route differently — the adversarial critique of the earlier position raised exactly this (`fable-analysis-of-frank-response-2026-07-11.md`). That legibility cost is real and is **carried by the diagnostic surface** (§7) — the rejection names the carrier and the disposition surface shows PROVEN vs GOVERNED per obligation. It is a surface obligation, not a hole in the model.

## 5. Runtime governance — two mechanisms, scoped to the contract path

**Scope this claim explicitly:** the two mechanisms below govern the **contract path — mutations entering or produced by declared operations.** They are **not** the whole runtime behavior surface. **Restored/hydrated state** is trusted as valid at persistence time and re-governed only on the next operation; **host-injected / out-of-contract data** is outside the contract envelope and can still reach the `[StaticallyPreventable]` **evaluator traps** — defense-in-depth for out-of-contract entry, *"unreachable for contract data — a trap firing on contract data is a proof-engine defect to fix, not an accommodated path"* (`frank-canonical-capture-recommendation-2026-07-14.md`; spec §0.7; `runtime-api.md` three-layer model). Those traps are a **third runtime zone, not a hidden third governance mechanism.**

**Reconcile with canonical §0.7.** Canon's Guarantee Contract names *"two distinct mechanisms"* meaning **compile-time fault prevention vs. runtime governance**, and describes governance as enforcement *"on every value entering the entity from outside the definition … at the moment it enters"* — i.e. **ingress** (`precept-language-spec.md:262`–`:270`). The ingress-vs-sweep split *within* governance is an **elaboration of how the govern route is delivered**, grounded in `precept-language-spec.md:1969` (*"Ingress governs what enters; the sweep governs the result"*) and `result-types.md:121`. I do **not** attach §0.7's *"precise rather than magical"* line to the ingress/sweep split — that line blesses the compile/runtime split. **Canon §0.7's Governance paragraph currently under-describes governance** (it names ingress; the runtime also enforces whole-entity invariants via the sweep). Whether §0.7 should be synced to name the sweep is an owner-gated doc question (§13, open); I flag it, I do not silently rewrite the framing.

**Mechanism 1 — Ingress governance: an external value, checked against its own carried constraint, at entry, before any computation.** The spec names exactly three ingress points: *"event arguments, construction inputs, direct field edits"* (`precept-language-spec.md:268`). Both author-facing surfaces below are ingress-governed for the same reason — their value is supplied, not computed:

- **(a) Operation/event arguments** — values a caller supplies firing an event or constructing the entity. Enforced the moment they enter, before any action derives from them (Fire pipeline: `runtime-api.md:361`, `:656`; construction inputs specifically: `precept-language-spec.md:268`, `runtime-api.md:162`/`:180`).
- **(b) Editable fields** — a field declared directly settable from outside (`field C editable`, `boundary-ruling:166`; access mode `Write`, `runtime-api.md:487`), mutated via a direct field edit (`Update`). Its edited value is externally-supplied, so the same ingress governance enforces the field's declared constraint (`runtime-api.md:386`, `:656`; `boundary-ruling:127`). A field *not* editable in the current state rejects the edit outright — `FieldNotEditable`, before any value is considered (`result-types.md:138`/`:147`).

Ingress checks **one incoming value** against **its own carried constraint**, at the instant it enters, **operation-blind** (`precept-language-spec.md:268`). *"The runtime enforcement is what makes that carried constraint true, not a second line of defense"* (`philosophy.md:55`) — this is the **Composition** seam, and it applies to the ingress case: an external operand carries a declared constraint, the compiler's fault proof rests on that carried fact, and ingress makes it true (`precept-language-spec.md:270`).

**Mechanism 2 — The post-mutation constraint sweep: all whole-entity invariants, checked against the resulting state, at commit.** After the mutation applies and computed fields recompute, the sweep re-evaluates **all whole-entity invariants** — global rules, state ensures (`in`/`to`/`from`), event ensures — against the *resulting* working copy, discarding the operation (`ConstraintsFailed`) if any invariant is violated (`result-types.md:114`/`:121`; `precept-language-spec.md:1969`). This is the mechanism that enforces the declared relational invariants of §3, because no single incoming value carries such a relationship.

**Failure paths.** Malformed argument → `InvalidArgs` (Stage 2, `result-types.md:113`); non-editable field edit → `FieldNotEditable` (`result-types.md:138`/`:147`); constraint violation → the mutation runs on a working copy that is discarded, `ConstraintsFailed`, nothing persists — *"an invalid configuration never persists — this is prevention, not detection"* (`precept-language-spec.md:268`; `result-types.md:114`/`:146`).

**The two mechanisms, crisply distinct.** Ingress = *one* incoming external value, against *its own* carried constraint, at *entry*, *before* computation. Sweep = *all* whole-entity invariants, against the *resulting* state, at *commit*, *after* computation. A relationship among independently-set fields is reachable **only** by the sweep, never by ingress.

## 6. The guarantee, precisely — outcome identical, epistemics honestly different

The outcome and the epistemics must be stated separately; collapsing them into "runtime governance is the same guarantee" hides a distinction the case against Option B depends on — the uniqueness critique's point that a deductive prover out-proves Precept on shared invariants (`critique-c1-uniqueness.md`). State it in two layers:

- **Outcome guarantee — identical.** No invalid configuration ever *persists*, whether a constraint was discharged by proof, enforced at ingress, or maintained by the sweep. The entity cannot enter a governed-invalid state any more than a proof-invalid one. This is the absolute of §2, and it holds across all three enforcement points.
- **Epistemic guarantee — honestly different, by design.** A proven fault is a **compile-time structural impossibility** — known before any entity exists (`philosophy.md:59`, "Compile-time structural checking," a *distinct* commitment from "Prevention, not detection"). A governed **relational invariant is not proven at compile time at all** — it is enforced *only* by the runtime sweep; the definition may *attempt* the violation, and the sweep refuses the commit. On the shared surface of business invariants, a deductive prover (Event-B) out-proves Precept **by design**: Precept **trades** deductive invariant-preservation for structural runtime enforcement (`critique-c1-uniqueness.md`). We do not paper this over — we *chose* it, and it is *why* external values and derived values route differently.

This honesty **strengthens** the anti-Option-B case rather than weakening it: precisely because a computed value governed at runtime would be *"a fault in a governed refusal's costume"* (it has no ingress door), we **reject** Option B for derived values and **prove-or-reject** them instead (`frank-canonical-capture-recommendation-2026-07-14.md`; `decision-ledger` #1).

**No GOVERNED claim without real enforcement behind it** (`critique-c3-philosophy-reconciliation.md`). A disposition of "governed" is honest only where the sweep/ingress **actually enforces** the constraint — the runtime constraint-plan wiring must exist for a bound to be genuinely swept. Where the enforcement is not yet built, the honest label is "designed," not "governed." We do not stamp GOVERNED over unbuilt enforcement.

## 7. The diagnostic surface — three-way verdict, per-obligation disposition, and the false-security constraint

The reject-vs-govern routing is only half the story: the **legibility machinery** that makes prove-or-reject honest at the author's surface is first-class posture, not an afterthought (`decision-ledger` #6; `boundary-ruling` Q9).

**The three-way verdict — the `ProofVerdict` DU** (Stage 0b Decision A; ledger #6; Forcing §4.2). Every prove-or-reject obligation resolves to exactly one of:

- **`Proven`** — discharged.
- **`ProvenViolating(witness)`** — rejected, carrying **one concrete violating configuration**.
- **`Unresolved(condition)`** — rejected, carrying the **weakest precondition printed verbatim**.

Both non-`Proven` verdicts **block**. `Unresolved` blocks not because the write "might be broken" but because the definition left a **reachable case with no authored disposition** — and prevention does not admit that. This is a DU because the three cases carry different evidence; it is not a flat record with nullable fields (CLAUDE.md DU rule; Stage 0b Decision A).

**The `spec:223` rescope** (ledger #6). *"Proven violations only"* is scoped to violation **reports** (dead rows, contradictions, unreachable states); bounded-write checks are **containment obligations** with the three-way verdict above. This is the spec text catching up to the ratified prove-or-reject, not a new rule.

**Severities, closed on the Error side** (ledger #10; Stage 0b; boundary Q5). A **provably-always-violating** derived write on a **reachable** row is `ProvenViolating` and is a hard **Error** — a definition defect, not dead-code advice (ledger #10; boundary Q5). The **six structural-severity flips Warning→Error** stand — process-topology (`UnreachableState`, `StructuralSinkState`, `DeadEndState`, `RequiredStateDoesNotDominateTerminal`) and uninhabitability (`UnsatisfiableRule`/PRE0159, `ContradictoryRule`/PRE0155) — and **dead-end→dead-end disposition = Error**. Distinguish **merely-unprovable** (`Unresolved`, rejected, WP printed) from **proven-always-violating** (`ProvenViolating`, rejected, witness printed): different evidence, same block (`critique-c3-philosophy-reconciliation.md`).

**The per-obligation disposition surface — PROVEN vs GOVERNED, no DEFERRED state** (boundary Q9). Each obligation carries a queryable, source-attributed disposition (PROVEN with its source, or GOVERNED with its ingress slot / swept invariant named), projected into `precept_proofs` and hover. **There is no DEFERRED state** — that state is the exact thing the posture forbids.

**The false-security constraint on that surface — an open acceptance criterion, not a solved problem** (`critique-c2-false-security.md`; the identity thesis's recommendation to treat the disposition surface as a design question). The dominant overtrust vector is *a single global "compiles clean" signal standing in for a per-site fact* (`critique-c2-false-security.md`). Three standing cautions ride with the surface, and none is yet validated on the actual (domain-expert, less-technical) audience:

- The disposition must be **legible at the point of authorship**, rendered **inline/ambient at the constraint**, not hidden behind a query the author must know to ask.
- Do **not** bundle PROVEN / GOVERNED into one flowing absolute sentence; the strong clause survives and the qualifier is dropped.
- **Disclosure first**: the disposition tags must not out-run their own soundness.

I mark this a **ratified-but-unvalidated legibility hypothesis routed to `/design`** (per the identity thesis's recommendation to treat the disposition surface as a design question) — **not** a closed win. Overclaiming here is itself a false-security failure.

**Two build-facts of the surface** (Stage 0b Decisions B, C): the MVP ships the **certificate *format* only — no running re-checker** (the independent re-checker is §5b, deferred); and the certificate step vocabulary comes from a **spec-enumerated `CertificateSteps` catalog** (catalog-before-code), not the `ProofStrategy` enum — the catalog's **membership** is unruled and owes the §5a certificate-format design pass.

## 8. The certificate criterion, and the principled stopping rule

This is the **most important thing the whole detour produced**, and it is first-class posture (retrospective §2 lesson 1, `:72`).

The spec used to **ban** solver-style engines outright (`spec:225`). That is replaced by a **criterion**: a proof strategy is admissible **iff all four** hold (`precept-language-spec.md:225`; ledger #2 EXTENSION):

1. **Legible & independently re-checkable** — emits a certificate from the spec-enumerated `CertificateSteps` vocabulary, replayable by an independent checker, readable by the author.
2. **Performant** — stays in the single-file recompile budget (the ~1–3 ms class against ~50 ms).
3. **Right-sized** — matched to Precept's tiny, few-variable, loop-free problems; a hard cap and an honest *"couldn't prove — here's what would"* fallback, never unbounded search. **Authority is the certificate, not the search** (Forcing §4.7).
4. **Earns its place** — each strategy carries evidenced value (expressibility for domains that matter, authoring-quality improvement, or genuine fault prevention). Nothing speculative; technical admissibility (1–3) is necessary but not sufficient.

**Clause 4 is the principled stopping rule the June effort lacked** (retrospective §5, `:72`). Together with **§1b deferred on a proof that combining linear facts is never an expressibility gain** (`:74`) and **§6 (the author states the fact the engine can't derive)**, it is the **governor** that bounds proof-obligation growth and makes deferrals principled rather than cowardly. Without these three, this is a research project whose compiler grows forever; with them, it is a bounded build.

## 9. The deferred and parked surface — stated openly, not dropped

Everything the posture holds *out* of the active build, said explicitly so no one re-imports it as either shipped or rejected.

- **§1b (multi-fact linear composition) — open in principle, closed for the MVP** (`decision-ledger` #3). Two dimensions at once: it is **open in the design space** (option C = *"leave it OPEN … A possibility to consider later,"* with a revisit trigger), **and closed for the current authorization set** — *"not in the MVP, not scheduled,"* the `spec:256` override **not authorized**, *"build nothing that requires it."* It is **deferred, not rejected** — do not conflate the two — **and** it is inactive in the plan until the revisit trigger (a *recurring* real definition where the derived bound resists being named as an intermediate field and authors hand-derive the composite wrong often enough to bite). The honest fix for multi-fact cases today is **§6**.
- **The escape hatch — CLOSED, no exceptions** (#9). An unresolved derived obligation is a **reject**, never a runtime shrug: *"✅ CLOSE — no escape hatch."* No author-visible "trust me" marker; a trust marker is directly corrosive to the guarantee.
  - **The accepted tradeoff, recorded.** Closing #9 **accepts the band-suppression / constraint-deletion dynamic** as a known cost — the adversarial critique of the earlier position flagged exactly this (`fable-analysis-of-frank-response-2026-07-11.md`): an author who cannot discharge a bound may **delete the bound** rather than invent a carrier, losing the proof, the governance, and the documentation. We take this on deliberately, on the ground that a **visible over-rejection at authoring time beats a silently-shipped unanswered case** (`fable-analysis-of-frank-response-2026-07-11.md` §5). **§6 does not neutralize band-suppression** — §6 lets the engine honor a fact the author *states*; the band-suppression author has no dischargeable fact short of a real carrier or a bounded operand. The answer to band-suppression is the carrier or the bound, and we accept that some authors will delete the band instead. Naming it is what stops the next person from "rediscovering" it and reopening govern-by-default.
- **Representational overflow — parked, target post-MVP (GATE-O)** (§2). Disclosed, not solved; no doc claims it prevented; the ∞/NaN and integer-conversion sub-lanes remain open inside the parked lane. The strategy itself — fixed-width vs. arbitrary precision — is unchosen by design; see §2.
- **Aggregates (`sum`/`min`/`max`/`average`) — routed to `/design`, arbitrary folds stay out** (ledger #4). New language surface; owes a Tier-2 pre-design conversation; sequences **after** the MVP core (wants §3 division + interval machinery). Not part of the MVP; not rejected.
- **Compute-then-check row construct — deferred, parked-not-closed** (ledger #5). Computed fields are **not** a full substitute (their derivation scope excludes event args and recomputes only post-mutation), so the residual event-arg-derived guard-routing gap is real but **ergonomic only** — no expressiveness loss, the duplication workaround always exists. Revisit as a `/design` item post-MVP if duplicated guard formulas prove a real authoring-drift pain.

## 10. What must NEVER be said again (banned framings)

Each with its ratified ground. The first four are the routing/posture errors; the last five are the **process** errors the retrospective flags as the shape of the next spiral.

- ❌ **"Govern, don't prove" (Option B) for derived values.** Shane ratified **A — prove-or-reject** and explicitly rejected Option B, *"the thesis's counter-position"* (`decision-ledger` #1). *(This is the error that keeps trying to come back — a "govern the undecidable computed case" middle arm — banned precisely because it recurs.)* Includes the thesis's own govern-don't-prove recommendation and the philosophy-reconciliation critique's "govern non-proof-carrying bands" and "Error-is-only-interim" readings — **contradicted by the ratified posture**, named here so they are not silently revived.
- ❌ **"Unprovable ⇒ defer to runtime"** for a *derived* value. That is a **reject** — *"no escape hatch"* (`decision-ledger` #9). Runtime is not the amnesty for a failed proof of an internal value.
- ❌ **"§1b is rejected."** It was **DEFERRED** and left OPEN (`decision-ledger` #3). Do not conflate "deferred" with "not part of the design." (Nor the reverse — do not present it as scheduled; see §9.)
- ⚠️ **"Route by surface spelling / by proof-carrying vs. not / by decidability."** The one real narrow ban plus the two rival axes killed in §4. Same construct, same provenance ⇒ same disposition; decidability is the discharge-oracle, not the router; proof-carrying was the dispute-fence, now subsumed.
- ❌ **"Prove-or-reject is already honest on June's / today's engine"** (retrospective §2). It is honest **only as the ratified package** (§3, §8). Re-assert the slogan over the un-strengthened engine and you reproduce the June stall.
- ❌ **"This hard slice means Precept's identity is wrong — reopen the philosophy first"** (retrospective §4, `:109`). The first move at a wall is a **disciplined engineering diagnosis** (what is undecidable, what is merely-unimplemented, what one authored rule would discharge), **not** identity re-litigation. The June 2c-ii amendment already held the diagnosis; the team set it down and picked up the philosophy question — that reflex cost a month.
- ❌ **"Revive the D2 band-split under new vocabulary"** — e.g. "non-proof-carrying *policy* bands are a different category." A bound **is** a rule; there is no band category (`ConstraintKind` has no band member; `boundary-ruling:§8`). D2 is dissolved, not renamed.
- ❌ **"Slice 0 can start dirty / it's just plumbing / it can absorb adjacent scope"** (retrospective §3, §5). Slice 0 is the highest-risk reshape and the 2c-ii analog; the two P0s are **preconditions**, not cleanup (§11).
- ❌ **"Just handle one more matcher form while we're here"** (retrospective §3, §5). The first near-miss is the spiral trying to restart; §1a's OQ2 and all of §1b stay parked until a *recurring* real need forces a re-gate.

**What is NOT banned:** the observation that *most of what the engine proves is fault-shaped and much of what governance enforces is nonlinear business-rule-shaped* is **neither false nor banned** — the ratified model IS built around a bounded fault surface (`boundary-ruling:25`) with nonlinear invariants governed (`:226`), and the ledger uses "fault-scoped" language (#1 folded-in note). Banned is only: (1) routing by **spelling**; and (2) claiming fault-status **causes** the disposition — a derived value is prove-or-reject because it is **derived**, an external value is governed because it is **external**; fault-vs-non-fault is not the router.

## 11. Why this was hard-won, and how we do not spiral again

We spent a month circling this, and the honest record says it was a **spiral, not a straight line** (`frank-retrospective-proof-engine-arc-2026-07-13.md:81`). The ratified answer was **not** always the position; it was hard-won (the retrospective records the swing at `:81`/`:109`).

**The stall was an engineering wall, not an identity crisis.** The June 2c-ii slice was scoped as "mostly reuse" but needed **(a)** a new fold capability (BUG-027, `ScanRulesAgainstDefaults`), **(b)** a soundness redesign because the reuse **over-proved** on unbounded references, and **(c)** genuinely-new length/count band-relational machinery (retrospective §1.1). That is the shape of a slice that looks tractable and explodes on contact. The diagnosis was already in the June amendment; the team set it down and re-litigated product identity instead.

**The landing carried genuinely-new material** not present at the start: the **certificate criterion** (*"the biggest single deposit,"* `:72`) and the proof that **§1b is never an expressibility gain** (`:74`). Not a circle — a spiral that deposited the governor (§8) the June effort lacked.

**The build-sequencing conditions — unmet, because nothing is built** (retrospective §3/§5; these are go-forward posture, not just history):

1. **Fix the two P0s before Slice 0 starts.** Fold the `DeadEndState` Warning→Error flip **into** Slice 0 so its outcome-neutrality is true by construction; **rule OQ1** (the certificate step-kind for the multi-term match) before Slice 0's DoD depends on it — no `Proven` mints through the §1a path until OQ1 is ruled.
2. **Treat Slice 0 like 2c-ii should have been treated** — full input-space enumeration, adversarial soundness review before commit, and refuse "while we're here" scope.
3. **Hold the parking lines** — §1a's matcher OQ2 and all of §1b stay parked until a *recurring, real* definition forces a re-gate.

**The countermeasure at the next wall:** the first artifact is a one-page engineering diagnosis (what is undecidable, what is merely-unimplemented, what one authored rule would discharge). Philosophy re-litigation is allowed only if that diagnosis fails to close — which, on the evidence of 2c-ii, it usually won't. I own enforcing this.

## 12. Deliberately out of scope (named, not dropped)

Some source items are genuinely not posture-statement material. I hold these out **on purpose**, with a pointer, rather than omit them silently:

- **Compiler-completion gate tracking** — old gates D3/D4, GATE-F/G/H/I/J/M, GATE-B/C/D/E/K/L/N, and the still-open GATE-I (fault delivery) / GATE-J (two spec self-contradictions). These are **plan/ledger tracking**, not the prove/govern posture; they live in the decision ledger and readiness plan and resolve in their own slices.
- **Slice-0 build mechanics** — Emission (c) interleaving, retiring `DiagnosticStage`, BUG-017/020/021 as the containment-completion target. Build detail, tracked in the readiness plan.
- **Canon-edit recommendations** — boundary R3 (keep `spec:258`), R5 (no edits to `philosophy.md:49/55/57`), R6 (surface overflow text). R6 is folded into §2; R3/R5 are owner-gated canon acts, noted not restated. Ledger #8 (tighten the one `philosophy.md:49` "wrong answer" clause) is **already applied** in canon (`frank-canonical-capture-recommendation-2026-07-14.md`); I do not re-open it.
- **Positioning / uniqueness copy** — the uniqueness critique's positioning points (SPARK narrowing, empty-cell-as-cost, "bet not moat," language-austerity attribution, Eiffel evidence). These shape **public/marketing copy**, not the proof posture. *(That same critique's Event-B honesty point is **in** scope and closed in §6.)*
- **Forcing-analysis proposals superseded by the ledger** — Forcing §4.5 (replace `spec:256` with full linear entailment) is **contradicted** by the ratified §1b deferral (§9); named as superseded, not carried.
- **Thesis recommendations (philosophy-copy, comparator rows, §0.7 discharge-tier vacuity, definition-evolution deploy-lane).** Owner-gated canon or post-MVP `/design` items; the ones bearing on posture — the three-way-verdict machinery and the disposition surface — are closed in §7.

If any of these is *not* where you'd draw the line, say so and I'll pull it into scope.

## 13. Open questions I want you to rule — not decide silently

1. **§0.7 doc-sync.** Canon's Governance paragraph describes governance as **ingress only**; the runtime also enforces whole-entity invariants via the **post-mutation sweep** (`result-types.md:121`, `spec:1969`). Should §0.7 be synced to name the sweep as a governance mechanism, or is the sweep to stay described only in §3A.4 while §0.7 keeps the compile/runtime framing? Owner-gated spec surface — I flag, I do not rewrite.
2. **Capture-doc drift.** `frank-canonical-capture-recommendation-2026-07-14.md` §1a and its Step-1 promotion plan currently say the boundary is *"proof-carrying vs. not."* The ratified router is **provenance** (§4). The capture doc must be corrected before its Step-1 rationale is promoted to `soundness-and-coverage.md`, or it will canonize an overruled framing. Confirm you want that correction, and I'll make it in the appropriate phase.
3. **The disposition-surface legibility hypothesis (§7).** The inline/ambient rendering and the surface's own soundness are an **unvalidated** design hypothesis with false-security as an unanswered acceptance criterion. Confirm it routes to `/design` with that criterion named, rather than being treated as solved.
4. **Certificate-format `/design` scope (§7).** The `CertificateSteps` catalog **membership** and **OQ1** (the certificate step-kind for the multi-term match) are unruled and block the `Proven` mint through §1a. Confirm these are the reserved first coding slice (§5a) and that OQ1 is ruled before Slice 0's DoD depends on it.

---

## Appendix — Changelog: v1 → v2 (auditable)

Each change names the finding/phase that drove it.

| # | Change | Driven by |
|---|---|---|
| C1 | **Restored the count/cardinality family** (`CountBoundViolation`/`LengthBoundViolation`) into the live-enforced fault set (§3); noted count "Reading A" as the nearest owner language decision. | Coverage: boundary worked row 14 / Q11 (dropped). |
| C2 | **Added the diagnostic surface section (§7)**: three-way `ProofVerdict` DU (Proven/ProvenViolating-witness/Unresolved-WP), `spec:223` rescope, per-obligation PROVEN/GOVERNED disposition with **no DEFERRED state**. | Coverage **critical**: ledger #6 + boundary Q9 (dropped); Stage 0b Decision A; Forcing §4.2. |
| C3 | **Severities closed on the Error side** (§7): proven-always-violating = Error; six structural-severity Warning→Error flips; dead-end→dead-end = Error; merely-unprovable vs proven-violating distinguished. | Coverage: ledger #10, Stage 0b F5, boundary Q5; critique C3-F3. |
| C4 | **Certificate criterion + stopping rule promoted to first-class (§8)**: four legs, "earns its place," authority-is-the-certificate; the three governors (§1b-proof, clause 4, §6). | Self-critique M1; coverage: ledger #2, Forcing §4.7, retrospective §2/§5; George cross-check. |
| C5 | **Rewrote the routing section (§4)** as Obligation-Role Rule → provenance shorthand; **named and killed** the proof-carrying, three-route, and spelling rival readings; **engaged the `set`-vs-`rule` evadability** head-on. | DA #2 (biggest opening); self-critique H3; George's "provenance not linearity" cross-check. |
| C6 | **Split the guarantee into outcome vs. epistemic (§6)**; corrected v1's "SAME guarantee"; stated the Event-B trade honestly; added "no GOVERNED without real enforcement." | Self-critique H1; critique C1-F1, C3-F9. |
| C7 | **Scoped the two-mechanism runtime claim to the contract path (§5)**; named restored/out-of-contract state + evaluator traps as a **third zone, not a governance mechanism**; reconciled with canonical §0.7 and stopped mis-citing `:264` for the ingress/sweep split. | DA #5; self-critique H2. |
| C8 | **Named the overflow tension as deliberate (§2)**: parked-and-"no doc may claim it prevented" **and** owner-ruled target-language-stays; distinguished owner policy from source consensus. | DA #1, #4; self-critique (de-hedge ledger #7); critique C3-F4/F5. |
| C9 | **§1b stated in two dimensions (§9)**: open in principle, closed for the MVP/authorization set absent the revisit trigger. | DA #3; coverage ledger #3. |
| C10 | **Recorded the band-suppression tradeoff (§9)** as the accepted cost of closing the escape hatch; noted §6 does not neutralize it. | Self-critique H4; `fable-analysis` Claim 8. |
| C11 | **Expanded §10 banned framings** with five **process** errors (honest-on-old-engine; engineering-wall-as-identity-crisis; D2-under-new-vocabulary; dirty-Slice-0; one-more-matcher-form). | DA banned-framings gaps 1–5; coverage retrospective §4/§5. |
| C12 | **Added the anti-spiral / build-sequencing section (§11)**: 2c-ii diagnosis, the two P0s, hold-the-parking-lines, engineering-diagnosis-first. | Coverage retrospective §1.1/§3/§4/§5 (dropped); self-critique M1. |
| C13 | **Added the false-security constraint on the disposition surface (§7)** as an unvalidated `/design` hypothesis (inline not query-only; no bundling; disclosure-first). | Coverage C2-F1..F5, Thesis Rec G; self-critique M2. |
| C14 | **Added deferred aggregates (#4) and compute-then-check (#5) to §9** so they are not silently dropped. | Coverage ledger #4/#5; Forcing §4.4/§4.6. |
| C15 | **Added §12 "deliberately out of scope"** naming the gate-tracking, build mechanics, positioning copy, superseded proposals, and canon-edit recs I am holding out — instead of dropping them silently. | Task instruction; coverage old-gates / thesis-recs / C1 positioning items. |
| C16 | **Citation fixes.** Construction inputs re-anchored to `spec:268` + `runtime-api.md:162`/`:180` (not Fire-only); "through genuine debate and reversal" de-quoted to an inference with `:81`/`:109`; ingress-vs-sweep anchored to `spec:1969`; canonical anchors added where the claim is philosophical (`spec:225`, `compiler-and-runtime-design.md`, `proof-engine.md:2609`, capture-doc `:56`). | Citation audit ⚠️ #1, #2 + strengthening list. |
| C17 | **Added §13 open questions** (§0.7 sync; capture-doc correction; disposition-surface hypothesis; certificate-format/OQ1) so owner-gated items are surfaced, not silently decided. | Self-critique H2/M3; coverage Stage 0b B/C; retrospective P0s. |
| C18 | **Minor:** named **arbitrary precision** as a live overflow-strategy alternative to fixed-width handling (§2 note + §9 pointer), reinforcing why the strategy choice is deferred past MVP. | Shane-directed addition, 2026-07-14 (post-v2 review). |

**What stayed from v1 (correct and decisive, unchanged in substance):** the identity/guarantee statement (§1–§2); Hybrid + total-over-the-fault-surface (§3); design-target-vs-built-today; relational-invariants-governed-by-provenance-not-linearity (§3); the provenance routing spine and decidability-as-discharge-oracle (§4); ingress governance mechanics and the composition seam (§5); Option-B ban, unprovable-derived-is-reject, §1b-deferred-not-rejected, spelling-invariance (§10); the spiral-not-a-circle framing (§11). Every one of the 29 audit-verified citations is retained.

---

### Readability pass — no content changes (2026-07-14, after the C1–C17 rewrite)

A prose-only pass followed the substance rewrite above, at Shane's request that v2 read less like agent-speak and more like v1's voice. It changed **tone and construction only** — no claim, decision, source citation, changelog entry (C1–C17), or open question (§13) was altered in meaning. Specifically, it removed the review's own process-machinery labels from the body prose — the `(DA #N)`, `(self-critique H1–H4/M1–M3)`, `(coverage …)`, `(George …)`, and `(audit ⚠️ #N)` tags that named *how* v2 was produced rather than *what* the posture says — and smoothed a few stiff transitions.

A follow-up label-simplification then made every remaining citation self-explanatory to a cold reader: bare finding-IDs (`C2-F1`, `C3-F3`, `F5`, `C1-F2..F6`), thesis-recommendation labels (`Rec F`, `Rec G`, `Rec A`), and author-tied claim numbers (`Fable Claim 8/9`) were replaced with plain-language descriptions of what each source says, keeping the source-document filenames (`critique-c1-uniqueness.md`, `critique-c2-false-security.md`, `critique-c3-philosophy-reconciliation.md`, `fable-analysis-of-frank-response-2026-07-11.md`). The finding-ID keys still live in the C1–C17 table below for anyone auditing provenance.

A later touch-up also de-jargoned the coined term "live set" / "live-enforced fault set" / "live fault floor" (and adjacent "live surface/guarantee/axis" phrasings) throughout the body prose, replacing them with plain language such as "the set of fault kinds enforced at compile time today," "the faults it already enforces," and "in force today." The one remaining "live" is the verbatim `"no live compile-time guarantee"` quote from `boundary-ruling:25`. No meaning changed.

Every real source citation (`boundary-ruling`, `decision-ledger`, `philosophy.md`, `spec`, `retrospective`, the `critique-*` docs, the forcing/fable analyses) was preserved. The full process audit trail those tags carried still lives in the C1–C17 table above, so nothing in the reasoning chain was lost — it simply left the reading flow. Neither pass reopened any C1–C17 decision.

— Frank
