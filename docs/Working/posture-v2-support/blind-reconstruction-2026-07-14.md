---
title: "George's Blind Reconstruction — Precept Go-Forward Posture on Prove-or-Reject and Runtime Governance"
status: Draft — 2026-07-14 (independent reconstruction, authored before reading frank-go-forward-posture-replay-2026-07-14.md)
author: George (Runtime Dev)
phase: Phase 3 — Independent Blind Reconstruction
purpose: >
  An uncontaminated, independent reconstruction of the settled go-forward posture,
  derived solely from the grounding corpus as assigned. Written before reading
  Frank's v1 posture replay. A "Diff vs. Frank's v1" section is appended after
  the draft was complete and Frank's v1 was read.
grounding-corpus:
  - docs/Working/proof-engine-boundary-ruling-2026-07-06.md (v3, primary; -v1-superseded.md for what changed)
  - docs/Working/proof-engine-decision-ledger-2026-07-12.md (owner-ruled, 2026-07-12)
  - docs/Working/proof-engine-linear-solver-feasibility-2026-07-12.md
  - docs/Working/prove-or-reject-forcing-and-expressiveness-2026-07-12.md
  - docs/Working/frank-prove-or-reject-position-2026-07-11.md
  - docs/Working/fable-analysis-of-frank-response-2026-07-11.md
  - docs/Working/precept-identity-and-guarantees-thesis-2026-07-11.md
  - docs/Working/precept-identity-thesis-supporting-2026-07-11/critique-c2-false-security.md
  - docs/Working/frank-retrospective-proof-engine-arc-2026-07-13.md
  - docs/philosophy.md (canonical anchors, as needed)
  - docs/language/precept-language-spec.md (canonical anchors, as needed)
excluded-until-after-draft:
  - docs/Working/frank-go-forward-posture-replay-2026-07-14.md
---

# George's Blind Reconstruction — Precept's Go-Forward Posture on Prove-or-Reject and Runtime Governance

*Authored 2026-07-14 by George (Runtime Dev). Independent reconstruction from the grounding corpus
only. `frank-go-forward-posture-replay-2026-07-14.md` was deliberately not read until after this
draft was complete. Every claim below carries a citation traceable to the grounding corpus.*

---

## 1. The Governing Model: Hybrid

Precept is governed by the **Hybrid model** — total over the fault surface plus runtime governance of
declared constraints at ingress. This model was re-derived in full by the boundary ruling
(ruling:§3) and ratified via the decision ledger as the package within which prove-or-reject
was ratified (ledger:#1).

The three models and their dispositions:

| Model | Compiler over derived-value faults | Undecidable/unplaceable case | "Compiles clean" means |
|---|---|---|---|
| **A — Bounded + defer** | Partial. Unprovable faults deferred to runtime fault-trap. | Compiles clean; runtime detects. | "Proven what I could; the rest is watched at runtime." |
| **B — Total + limited surface** | Total. | **Inexpressible** — author rewrites into the decidable fragment. | "Totally proven; nothing undecidable admitted." |
| **Hybrid (SETTLED)** | **Total over the live-enforced fault surface.** No fault deferred. | **Admitted as a governed constraint** (not a deferred fault); a fault-prone *operation* over it is still prove-or-reject. | "Every derived-value fault proven; every declared constraint governed at ingress and used as a proof premise." |

**Model A is rejected** on `philosophy.md:57` ("no unproven evaluation faults") and Principles 7/10/11
(`spec:104/110/112`) — independently of how the "no deferrals" spec clause (`:266`) is read. The
clause is corroboration, not a pillar; the pillars are the philosophy and principles (ruling:§2.2,
§3 Leg 2). Three independent corroborations: the owner shipped the opposite at the nearest scope
(count-bound "Reading A, prove-or-reject," commit `c27a382b`); the bug tracker classifies silent
deferral as soundness holes (BUG-017/020/021, `bugs.md:171/188/203`); the adversarial red-team
concluded Model A "fails specifically against Precept's non-negotiable `philosophy.md` [line 57]"
(`verify-1-redteam-modelA.md §2/§4`).

**Pure Model B is rejected** on expressiveness: 21 of 77 sample files use product expressions;
nonlinear invariants such as `rule TotalInventoryCost == AverageCost * QuantityOnHand` are
legitimate governance constraints, not fault-prone operations. Refusing them is a needless
product-quality loss (ruling:§3 Leg 2, Pure-B rejection).

---

## 2. What the Guarantee Covers

The precise guarantee statement: **"A definition that compiles without diagnostics has no unproven
evaluation faults, no unreachable business process states, and no structural dead ends"**
(`philosophy.md:57`).

The absolutist statement: **"Prevention, not detection… these configurations and workflows cannot
exist"** (`philosophy.md:49`).

Both are literally true under Hybrid:
- `philosophy.md:57` holds because every derived-value fault in the live-enforced set is
  proven at compile time — "no unproven evaluation faults" is exact.
- `philosophy.md:49` holds because an invalid external configuration is discarded on the working
  copy before it persists (`spec:268`, `§3A.4:1965`) — prevention, not detection.

### 2.1 The Live-Enforced Fault Surface

The compiler must prove or reject **every fault-prone operation over a definition-derived value**
in the live-enforced set. That set is:

- Division-by-zero (`DivisionByZero`, `Diagnostics.cs:821`)
- `sqrt`/`pow` non-negativity (`SqrtOfNegative`, `Diagnostics.cs:829`)
- Empty-collection access and index-out-of-bounds (`LengthBoundViolation`, `Diagnostics.cs:1281`;
  `CountBoundViolation`, `Diagnostics.cs:1293`)
- Declared-bound containment of a computed result (`OutOfRange` / assignment-range impossibility,
  `spec:208`, `Diagnostics.cs:697`) — a value derived by a Precept expression flowing into a
  field's declared `min`/`max` band

This is the **fault surface**, not "everything decidable." The compiler is total over this surface
and nothing more (ruling:§1, §4.B).

### 2.2 The One Disclosed Carve-Out: D1 Representational Overflow

**Representational overflow is the single exception.** `NumericOverflow` — a computed decimal/money/
temporal value exceeding what the *type itself* can represent (`Diagnostics.cs:690`) — is
**owner-parked (D1)**: `decision-index.md:19`; `compiler-readiness-plan-2026-06-16.md:188`.

D1's precise status:
- A **disclosed, defense-in-depth known-fault backstop with no live compile-time guarantee**
- **Not currently prove-or-reject** — temporary by design
- **No doc may claim it prevented** (D1 text)
- `NumericOverflow` survives as a runtime backstop only, with **no live `[StaticallyPreventable]`
  guarantee** for derived multi-field representability (`compiler-readiness-plan:199`)

D1 is a **narrowing of the guarantee**, honestly disclosed — not a relocated guarantee, not Model A
behavior. The runtime overflow trap is defense-in-depth, not a primary enforcement mechanism
(ruling:§1; ruling appendix, Principles 10/11 row).

**Overflow is the intended end-state target.** Decision 7 of the ledger (owner-ruled): "Overflow
prevention IS the intended end-state — Shane intends to address it post-MVP." The present-tense
overflow claims in `philosophy.md:53`, `spec:110`, `spec:112` correctly state the target; the gap
is build-order, not a text defect. No `philosophy.md` edit; no interim "not-yet" rider. The six
enumerated canonical sentences carrying overflow claims (`philosophy.md:53/:61`; spec P7/P10/P11/
summary at `:104/:110/:112/:266`) are flagged as a disclosed gap (old GATE-F) to reconcile when
overflow un-parks (ledger:#7).

**Critically: `NumericOverflow` and `OutOfRange` are different faults.** `NumericOverflow` is
value-vs-type-range; `OutOfRange` is value-vs-declared-`min`/`max`
(`overflow-prevention-design-analysis.md:516`). Declared-bound containment (`OutOfRange`) **stays
live** in the prove-or-reject set regardless of D1. D1 carves out only representational overflow.

---

## 3. The Routing Axis: Governance vs. Prove-or-Reject

### 3.1 The Obligation-Role Rule

Disposition is a property of an **obligation**, not of a construct. There are exactly two
obligation families, and one oracle (ruling:§4).

**Family A — Every declared constraint `C` (modifier or rule, indistinguishably) generates two
obligations, always:**

1. **Governance (runtime, at ingress):** For each field `C` reads, whenever a raw external
   value enters that field — construction input, event argument bound directly to it, or direct
   field edit — `C` is enforced on the resulting working copy, which is discarded if `C` fails
   (`spec:268`; `§3A.4:1965`). **This is not the deferral of a fault; it is enforcement of a
   declared contract on raw input.**

2. **Premise contribution (compile-time):** `C` contributes its provable fact to the proof
   engine's interval/relational knowledge (`spec:203`, `:1138`), to the extent it is decidable
   in context — a literal bound always; a relational constraint only insofar as its referenced
   fields are themselves bounded. This premise is what discharges Family B obligations.

**Family B — Every fault-prone operation `O` over a definition-derived value carries one
obligation: compile-time prove-or-reject (`spec:266`) — with the single D1 carve-out
(representational overflow).** `O` ranges over the live-enforced fault set (§2.1 above). `O` is
routed to prove-or-reject **by the origin/shape of the value it consumes** — never by how hard
`O` is to prove (ruling:§4.B, the anti-slide device).

**The oracle: decidability.** For each *live-enforced* Family B obligation, decidability selects
the outcome: **discharge** (provably safe), **reject-violated** (provably unsafe, with a concrete
witness), or **reject-unprovable** (neither — the merely-unprovable case, `Diagnostics.cs:1303`).
Decidability is the **discharge oracle**, not the **router**. Routing by effort would make every
newly-hard case a deferral candidate (Model A's slide); routing by origin cannot slide.

### 3.2 The One-Line Boundary (Ruling's Own Formulation)

> *A raw external value at its own ingress slot is **governed**; any value a Precept expression
> has derived from it is **proven-or-rejected** (for the live-enforced fault set) or **carried
> by the disclosed D1 park** (representational overflow); `max 100` and `rule x <= 100` are the
> same construct and get the same treatment; **decidability decides whether a prove-or-reject
> obligation discharges or the definition is rejected — never whether it is proven or governed.***
>
> (ruling:§4, one-line boundary)

The boundary is a **dataflow fact** — origin/derivation, decidable from the typed IR without a
solver. "Before any computation derives from it" (`spec:268`) is the precise line.

### 3.3 Constraint Spelling Is Dispositionally Inert

`precept-language-spec.md:1135`: "Constraint modifiers are shorthand for rules."
`precept-language-spec.md:1138`: "`min 5` and `rule X >= 5` are interchangeable to the proof engine
— proof participation is a function of a constraint's **decidability**, not its syntactic form."

`min Floor ≡ rule Amount >= Floor` (`bugs.md:196`). The v1 ruling's error was routing by
modifier-vs-rule spelling; that error is explicitly corrected in v3 (ruling:§2.1).

### 3.4 Governance Is Not Deferral

This is the load-bearing distinction the corrections jointly produced (ruling:§2.3):

- **Governance** = runtime enforcement of a *declared constraint* on a *raw external value* at
  ingress. The fault is **fully proven at compile time**; governance only makes the carried
  precondition true (`spec:270` Composition: "proven by the compiler, its precondition discharged
  by governance, nothing left to a runtime check"). **Not deferral — nothing about a fault is
  postponed.**

- **Deferral (Model A)** = punting the *proof of a fault-prone operation* to a runtime fault-trap,
  so an unproven fault compiles clean. **This is what `philosophy.md:57` forbids.**

The Composition clause (`spec:270`) is the seam: for a runtime-supplied value, "proven by the
compiler, its precondition discharged by governance." The fault is prevented, not detected.

---

## 4. The Two Distinct Runtime-Governance Mechanisms

The spec defines **two enforcement points**, not one (`spec:1969`):

**4.1 Ingress governance** (`spec:268`): Raw external values — event arguments, construction
inputs, direct field edits — are checked against their declared constraints at the moment they
enter, *before any computation derives from them*. The working copy is discarded if a constraint
fails. This is the mechanism `philosophy.md`'s governance leg refers to when it says
"every value entering the entity from outside the definition."

**4.2 Post-mutation sweep (commit sweep)** (`spec:1967`/`:1969`/`:1354`): After all mutations
in an operation complete, **every declared constraint is re-checked against the completed working
copy**. If any constraint fails, the working copy is discarded atomically. The failure outcome is
`EventOutcome.ConstraintsFailed` — a typed, recoverable refusal (`result-types.md:121`). This
sweep covers "global rules, state ensures (in/to/from), AND event ensures" (`result-types.md:121`).

The spec's own summary at `:1969`: **"Ingress governs *what enters*; the sweep governs *the
result*."**

Constraint modifiers (as rule shorthand per `:1135`/`:1354`) are checked against the complete
working copy **after all mutations** — not per-write, not per-intermediate-assignment. The
obligation is commit-scoped by the spec's own semantics, not per-assignment (ruling:§4, Table row
notes; `fable-analysis:§2 Claim 2`).

**Both mechanisms produce `ConstraintsFailed`** as the typed recoverable outcome
(`result-types.md:121`). Neither is a fault-trap. Neither is deferral.

**The settled routing for derived values**: prove-or-reject was **ratified** for derived-value
containment obligations (ledger:#1). The post-mutation sweep's ability to catch a failed derived
write as `ConstraintsFailed` does not convert the compile posture to GOVERN — the owner explicitly
chose prove-or-reject for this case, on the grounds that an unprovable derived write is an
unanswered domain question the author must answer at authoring time, not defer to runtime.

---

## 5. The Discharge Rule

For each **live-enforced** Family B obligation:

- The obligation is **discharged** (proven safe) if the Family A premises from declared constraints
  decide it safe — using the proof engine's interval/relational knowledge built from the
  definition's constraints, event ensures, row-complement narrowings, and any applicable guard.
- The obligation is **rejected-violated** if the engine exhibits a concrete counterexample
  configuration that breaks the bound. The rejection includes the witness.
- The obligation is **rejected-unresolved** if neither — the engine can neither prove it safe
  nor exhibit a violation. The rejection message includes the computed weakest precondition
  verbatim, naming what the author must supply to discharge it.

Decision 6 (ledger, owner-ratified): `spec:223` is rescoped — "proven violations only" applies to
violation **reports** (dead rows, contradictions, unreachable states), not to containment
obligations. Containment obligations get the **three-way verdict**: proven / proven-violating
(with witness) / unresolved (with WP verbatim). Both proven-violating and unresolved **block**
compilation; the distinguisher is *why* they block (a definite violation vs. an unanswered case).
Dead-row severity = **Error** (ledger:#10): a proven-always-violating write on a reachable row
is the "proven-violating" bucket, which blocks.

The discharge predicate is **conservative** (`spec:221`, Proof philosophy #1 — soundness over
completeness). The engine will sometimes reject a safe-but-unprovable derivation — the author must
add the carrying constraint. This is the accepted tradeoff: a one-time author guard-tax for an
unqualified guarantee.

**The discharge rule applies to the live-enforced set only.** Representational overflow (D1) is
outside this oracle — it is a disclosed gap, not a discharged obligation.

---

## 6. Prove-or-Reject Is Honest Only as a Package

This is explicitly stated in the decision ledger: **"ratifying A over *today's* engine would
reject the corpus's own house style — A is honest only as a package with items 2 and 3"**
(ledger:#1 condition).

The package that makes prove-or-reject honest and buildable:

1. **Certificate criterion (ledger:#2, ratified with extension):** A proof strategy is admissible
   iff it is: (1) legible & re-checkable — emits a certificate from a small spec-enumerated
   vocabulary, replayable by an independent checker and readable by the author; (2) performant —
   stays in the ~1–3 ms class against the ~50 ms recompile budget; (3) right-sized — complexity
   matched to Precept's tiny, few-variable, loop-free problems; (4) earns its place — evidenced
   justification on at least one of: added expressibility, improvement in authoring correctness, or
   genuine fault prevention. This replaces `spec:225`'s tool ban with a *criterion*. The change is
   critical: it legalizes the MVP engine (any linear-arithmetic strategy is spec-clean iff it ships
   a legible certificate).

2. **§6 in the MVP (ledger:#1 folded):** "The author states the consequence as one rule; the engine
   applies it." Pulled into MVP scope. This is the principled escape valve for the author who cannot
   prove a multi-field relationship — state it as one rule, and the engine applies it by congruence.
   Congruence discharge: once a fact is made true at every entry point (by a rule or guard), the
   engine applies it directly to satisfy an obligation (`forcing:§3.1`).

3. **§6 is also the honest fix for multi-fact cases** — the fact that combining linear inequalities
   is itself a linear inequality the author can write as one rule is what makes deferring §1b
   *principled* (ledger:#3 rationale; `retrospective:§2`).

4. **MVP engine scope: §5a → §3 → §2 → §4a → §1a → §6** — the five proof strategies that, taken
   together, prevent prove-or-reject from rejecting the corpus's own idiomatic style.

---

## 7. §1b Status: Deferred, Not Rejected

**Decision 3 (ledger, owner-ruled): DEFER §1b — leave it OPEN, not in the MVP, not scheduled.**
Option C of the three presented (A: in the MVP; B: the phase after §1a; C: don't build now).

§1b = combining two or more linear facts. The argument behind deferral (these are settled, not
merely opinions):

- Combining linear inequalities is never an **expressiveness gain**: the combination of two linear
  inequalities is itself a linear inequality the author can write as one authored rule — so §6
  covers every multi-fact case at zero expressiveness cost to the author (the author writes one
  rule; the engine applies it) (ledger:#3 rationale).
- The only benefit of §1b is **ergonomic**: sparing the author from naming the derived bound as
  an explicit intermediate. That benefit shrinks once prove-or-reject nudges authors to name
  intermediate quantities.
- The strongest §1b candidates (finance waterfalls, simultaneous-constraint LPs) fall outside
  Precept's integrity contract.
- §1b is the highest-cost, spec-override-requiring, soundness-critical build in the roadmap
  (feasibility:§2 — ~1,500–2,200 engine lines, ~2,500–4,000 test lines, 3× §1a).

**`spec:256` override is NOT authorized** (deferred with §1b). The locked spec sentence
("single-pass and depth-bounded, no transitive chasing of a third field") stands.

**Revisit trigger:** a *recurring* real definition where the derived bound genuinely resists being
named as an intermediate field AND authors hand-derive the composite rule wrong often enough to
bite in practice (ledger:#3 ruling text).

§1b is explicitly **"not closed"** — a possibility to consider later. It is not foreclosed; it is
parked with a documented trigger and a principled reason for the parking.

---

## 8. Other Settled Load-Bearing Items

### 8.1 No Escape Hatch

Decision 9 (ledger, owner-ruled): **CLOSE.** No escape hatch; no author-visible "trust me" marker.
The forcing analysis found no exception class that justifies one
(`forcing:§2.2` — the two exception members either dissolve under §4.4 or reduce to ergonomics
chargeable to the one-file identity). An escape hatch would make the guarantee bypassable and
directly corrode the prove-or-reject value proposition Shane ratified prevention for (ledger:#9).

Note: Frank's ruling flagged R4 (an explicit opt-in construct as a *future* language-surface
question) as newly-opened by Shane's "no deferrals" clarification (ruling:§7 R4). That question
was **closed** by Decision 9. The forcing analysis closed it explicitly.

### 8.2 D2 Band-Split: Dissolved

Decision D2 (decision-index, unratified prior proposal) has been superseded and dissolved. Its
premise — that there is a category of "non-carries-proof bands" distinct from rules — does not
exist. A bound **is** a rule (`spec:1135`/`:1138`); `ConstraintKind.cs` has no "band" member
(`ConstraintKind.cs:10`); `min 5` desugars to `rule X >= 5`. A disposition keyed on modifier-vs-rule
spelling is incoherent. D2 is declined as an unratified proposal whose premise does not survive
constraint=rule (ruling:§9 supersedes).

### 8.3 Structural-Severity Flips (Re-affirmed 2026-07-12)

Six diagnostics flipped Warning → Error, re-affirmed by Shane 2026-07-12 (ledger Stage-0b):
- Process-topology (can't leave state): `UnreachableState`, `StructuralSinkState`, `DeadEndState`,
  `RequiredStateDoesNotDominateTerminal` — grounded on `philosophy.md:51` ("proven impossible")
- Uninhabitability (no valid entity can inhabit the definition): `UnsatisfiableRule` (PRE0159),
  `ContradictoryRule` (PRE0155) — grounded on `philosophy.md:49`/`:51`

### 8.4 ProofVerdict DU (Decision A, Stage-0b)

The verdict representation is the `ProofVerdict` discriminated union: `Proven` / `ProvenViolating(witness)` / `Unresolved(condition)`, each case carrying only its own evidence. CLAUDE.md-mandated by the "use discriminated unions for varying shapes" rule (ledger Stage-0b Decision A).

### 8.5 Certificate Format Is the First Coding Slice (§5a)

The certificate step-vocabulary SOURCE = a spec-enumerated `CertificateSteps` catalog
(catalog-before-code, not a reuse of `ProofStrategy` enum) (ledger Stage-0b Decision C). The
catalog's **membership** is not ruled — it owes the §5a certificate-format `/design` pass, which
is the first coding slice, laid down before any new proof strategies.

### 8.6 §1b Independent Re-checker Retracted from MVP

The independent re-checker is §5b, deferred. The MVP ships the §5a certificate *format* only
(re-checkable in principle; no live re-checker, no checker-gated mint) (ledger Stage-0b Decision B).

### 8.7 Active Soundness Holes Are the Completion Target

BUG-017 (`bugs.md:171`: proof engine silently skips result-bound check when operand interval is
unbounded — `OutOfRange` soundness hole), BUG-020 (`bugs.md:188`: field-reference bound inert),
BUG-021 (`bugs.md:203`: min-only lower bound): these are **the completion target** — Model-A-shaped
behaviors to close, not policy. They are `OutOfRange` (declared-bound containment) holes, NOT
`NumericOverflow` (representational overflow) issues. The distinction matters: BUG-017 is a live
fault surface hole; D1's park is a separate disclosed gap.

### 8.8 The Disposition Surface

Per-obligation PROVEN/GOVERNED surface mandated (ruling:§5 Q9): no DEFERRED state. Every accepted
verdict ships its own certificate; GOVERNED tags name the ingress slot. This is the legibility
device that makes "precise rather than magical" actionable at authoring time (ruling:§4 Q9).

### 8.9 Nonlinear Governance Rules Need No New Compiler Capability

A nonlinear invariant such as `rule TotalCost == AvgCost * Qty` is a *governance constraint* —
governed by the post-mutation sweep, never prove-or-reject. Its arithmetic's representational
overflow is parked under D1. The compiler has no obligation to prove nonlinear invariants in the
live fault sense; the sweep enforces them. No new compiler capability is needed for these; they
compile clean under Hybrid and are enforced at runtime (ruling:§4, Table row 10).

---

## 9. The Retrospective's Engineering Lesson (Load-Bearing for Future Work)

Frank's retrospective (`retrospective:§2/§4`) establishes two points that are settled posture
context, not just history:

1. **The June stall (2c-ii / field-reference-bound enforcement) was an engineering boundary
   mistaken for an identity crisis.** The wall was: the engine couldn't prove field-reference
   bounds on unbounded references; the team re-litigated product identity instead of diagnosing
   the engineering gap. The diagnosis was available in the 2c-ii amendment itself.

2. **The ratified package is genuinely new vs. the original prove-or-reject stance** — not a
   circle. Three things are new: the certificate criterion (what proof is allowed to be in
   Precept); §6 as the principled multi-fact escape valve; the proof that §1b is never
   expressibility. These are the governors that make the posture buildable and bounded.

The **principled stopping rule** is the single most important thing the detour produced: §1b
deferred with proof that it is never expressibility; ledger:#2 clause 4 (every strategy must earn
its place); and §6 (author states the fact). Without these three backstops, proof obligation growth
is unbounded (`retrospective:§5`). With them, it is a bounded engineering build.

---

## Summary: The Posture in One Pass

1. **Model: Hybrid.** Total over the live-enforced fault surface; runtime governance of declared
   constraints at ingress. Prove-or-reject ratified by the owner (ledger:#1).

2. **Guarantee:** `philosophy.md:57` unqualified for the live fault set. The guarantee is two-tier:
   live-enforced faults (prove-or-reject) and the disclosed D1 overflow park (defense-in-depth, no
   live prevention, temporary).

3. **Fault surface:** Division, sqrt/pow non-negativity, empty/index access, declared-bound
   containment of computed results. NOT "everything decidable." Routed by origin, not decidability.

4. **One carve-out:** `NumericOverflow` (representational overflow) is owner-parked (D1) — a
   disclosed known gap, no live guarantee, no doc may claim it prevented. `OutOfRange` (declared
   bound) stays live. The two are different faults.

5. **Routing axis:** Raw external value at its ingress slot → governed. Any value a Precept
   expression derives → proven-or-rejected (live set) or D1-parked (overflow).

6. **Discharge rule:** Proven / proven-violating (with witness) / unresolved (with WP verbatim).
   All three outcomes are determined at compile time. Both violating and unresolved block.
   Spec:223 rescoped to scopes violation *reports* (ledger:#6).

7. **Governance is not deferral.** The fault is proven complete at compile time; governance
   discharges only its precondition (that the raw value carries its constraint). Spec:270
   Composition clause.

8. **Two governance mechanisms:** ingress (before computation derives from the value, spec:268)
   and the post-mutation sweep (after all mutations, against the completed working copy, spec:1969).
   Both produce `ConstraintsFailed`. Neither is a fault-trap. Neither is deferral.

9. **§1b deferred, not rejected.** Not in the MVP, not scheduled. Spec:256 override not
   authorized. §6 is the honest fix for multi-fact cases. Revisit trigger documented.

10. **Constraint spelling inert.** `max 100` and `rule x <= 100` are identical. D2 dissolved.

11. **Prove-or-reject is honest only as a package:** certificate criterion + strengthened engine
    (§1a/§2/§3/§6) + three-way verdict surface. Ratifying the posture over today's engine repeats
    June.

12. **No escape hatch** (ledger:#9). The certificate criterion is the power bound; §6 is the
    author's path out; the forcing analysis closed every proposed exception class.

---

## Diff vs. Frank's v1

*This section was written after reading `docs/Working/frank-go-forward-posture-replay-2026-07-14.md`
for the first time. The independent draft above is preserved as written; nothing above this line
was modified after reading Frank's v1.*

---

### Where My Reconstruction and Frank's v1 Match

The following are fully convergent:

- The Hybrid model ruling and its three-model table
- `philosophy.md:57` as the precise guarantee; `philosophy.md:49` as the absolutist voice
- D1 representational overflow: owner-parked, disclosed, no live guarantee, no doc may claim it
  prevented, intended end-state post-MVP (Decision 7 / GATE-O)
- `OutOfRange` vs. `NumericOverflow` as distinct faults — D1 carves out only `NumericOverflow`
- The live-enforced fault surface (division, sqrt/pow, empty/index, declared-bound containment)
- Routing by origin/provenance, not by decidability or surface spelling
- Constraint spelling is dispositionally inert (`max 100` ≡ `rule x <= 100`)
- Decidability is the discharge oracle, not the router
- Governance is not deferral; spec:270 Composition
- §1b deferred (not rejected) per Decision 3; §6 as the honest fix; revisit trigger documented
- No escape hatch per Decision 9
- Model A rejected on philosophy:57/Principles 7/10/11, not on the "no deferrals" clause alone

---

### Where Frank's v1 Covers Something My Reconstruction Underweights

**1. Linear relational invariants are GOVERNED, not proven — by design, not by weakness.**

This is the most significant gap. My reconstruction covered the nonlinear case thoroughly (§8.9),
but Frank's v1 makes a stronger and more explicit point: a **linear** relational invariant such
as `rule Balance == Deposits - Withdrawals` is also **governed**, not proved-or-rejected — because
it *declares a relationship* among independently-set fields rather than *computing a derived value*.

Frank's v1 (§3): "A declared relational invariant over independently-set fields — whether nonlinear
(`rule TotalInventoryCost == AverageCost * QuantityOnHand`) or **linear** (`rule Balance ==
Deposits - Withdrawals`) — is 'Expressible — governed,' with 'Zero expressiveness loss'
(`boundary-ruling:226`). It is governed, not proven, because it *declares a relationship* rather
than *computing a derived value* — there is no derived-value fault obligation to discharge."

My reconstruction's §8.9 addressed this only for nonlinear cases. The stronger claim — that the
routing is by **provenance** (derived computation vs. declared relationship), not by linearity —
is in the ruling's own worked-example table (rows 10/11) but I didn't pull it out as a headline.
Frank's v1 makes it the most prominent correction: "routing is by provenance, not linearity."

The corollary is explicit in Frank's v1: the §1a/§1b linear-solver work concerns discharging
fault obligations using facts as premises — it was **never** the reason a relational invariant is
ungoverned. A relational invariant is governed because it has no derived-value obligation to
discharge, not because it is too hard to prove.

**2. The "two-way routing rule" framing is sharper in Frank's v1.**

Frank's v1 §5 states the routing as a clean **two-way** rule: derived value → prove-or-reject;
not a derived-value obligation → govern (via either ingress or the post-mutation sweep). My
reconstruction describes the same territory but doesn't emphasize the two-way structure quite as
cleanly — I described it as routing by origin with two governance mechanisms, which Frank would
call the "govern route delivered by two mechanisms, but still one route."

**3. Explicit "What must NEVER be said again" — including correction of an earlier v1 draft.**

Frank's §6 explicitly lists banned framings and — crucially — calls out that "the prior version
of this replay committed [the 'govern the undecidable computed case' error] in its §4(b)/§5
middle arm." I could not have known an earlier draft of Frank's replay existed with this error;
the correction is load-bearing context that my reconstruction missed.

Specifically, Frank bans the framing "no deferral is fault-scoped" as a *routing* principle — he
notes the ledger uses "fault-scoped" language but clarifies: "the observation that most of what
the engine proves is fault-shaped… is neither false nor banned — the ratified model IS built
around a bounded fault surface with nonlinear invariants governed. What is banned is only the two
overstatements: (1) routing by spelling; (2) claiming fault-status *causes* the disposition."
My reconstruction didn't make this nuance explicit.

**4. Editable fields as a distinct ingress case.**

Frank's v1 (§4, Mechanism 1b) explicitly calls out `field C editable` as a second ingress case
alongside event arguments and construction inputs, and notes the `FieldNotEditable` rejection for
non-editable fields. My reconstruction mentioned "direct field edits" but didn't dwell on the
`editable` modifier and the distinction between editable and non-editable field access modes.

---

### Where My Reconstruction Covers Something Frank's v1 Omits or Underweights

**1. The three-way verdict and spec:223 rescope (Decision 6).**

My reconstruction explicitly covers the three-way diagnostic verdict (proven/proven-violating with
witness/unresolved with WP verbatim) and Decision 6's rescoping of `spec:223` to violation
*reports* (dead rows, contradictions, unreachable states) rather than containment obligations.
Frank's v1 mentions "decidability is the discharge-oracle" (§3) and briefly notes the "no escape
hatch" ruling (§3), but does not separately surface the three-way verdict structure or the
spec:223 rescope as posture. These are load-bearing for understanding what "prove-or-reject" means
at the diagnostic surface.

**2. Structural-severity flips and BUG-017/020/021 as completion target.**

My §8.3 and §8.7 cover these as settled posture items. Frank's v1 does not — it is focused on
the prove-or-reject / governance boundary, not the broader compiler completion landscape. Not a
gap in Frank's v1 per its scope, but a gap in what an independent engineer reading it would know.

**3. The retrospective's "principled stopping rule" as load-bearing posture.**

My §9 explicitly identifies the three backstops (§1b deferral with proof it is never expressiveness;
certificate criterion clause 4 "earns its place"; §6 as multi-fact escape valve) as the governors
that make the posture *bounded and buildable*, and that their absence was what caused the June
spiral. Frank's v1 §7 touches the retrospective briefly ("the biggest single deposit of the whole
detour") but frames it as historical context rather than as first-class posture.

**4. D2 band-split dissolution.**

My reconstruction explicitly names D2 as dissolved and explains why. Frank's v1 does not discuss
D2 by name, though the constraint=rule point is implicit throughout.

---

### Most Significant Difference

The most significant difference is Frank's explicit, prominent articulation that **linear relational
invariants are governed, not proven — by provenance, not by linearity**. My reconstruction
addressed the nonlinear case but did not make crisp that the routing is the same for a linear
invariant like `rule Balance == Deposits - Withdrawals`: it is governed because it declares a
relationship, not because it is hard to prove. Frank's v1 treats this as the key correction that
resolves what the prior replay draft got wrong, and it is the primary source of the "what must
never be said again" section. My reconstruction needs this sharpened when used as input to the
v2 rewrite.

The second most significant gap is the absence from my draft of the "prior version of this replay
had this error" context — I could not know from the corpus that the replay itself had a prior
draft with a specific mistake. Frank's v1 is in part a self-correction, and the self-correction is
itself load-bearing posture: it names the error that keeps trying to come back ("govern the
undecidable computed case") explicitly as a known recurrence risk.

---

*End of independent reconstruction and diff — George, 2026-07-14.*
