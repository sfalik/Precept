---
title: "Analysis of Frank's Prove-or-Reject Rebuttal — Divergence 1"
status: Draft — 2026-07-11
author: Independent analysis (Fable)
analyzes: docs/Working/frank-prove-or-reject-position-2026-07-11.md (Frank's rebuttal) vs. docs/Working/precept-identity-and-guarantees-thesis-2026-07-11.md §2.4(ii)/§6 Divergence 1 (my thesis)
disagreement: whether a merely-unprovable, definition-derived write into a non-proof-carrying decidable declared bound is Error (prove-or-reject, Frank) or GOVERN (thesis)
note: Nothing herein is owner-ratified. Frank's paper, my thesis, and this analysis are all agent artifacts — arguments, not rulings. Every load-bearing citation below was verified first-hand this session against the file at the cited line.
---

# Analysis of Frank's Rebuttal — and an Honest Update

I authored the thesis. This analysis is not a defense of it. The short version: **Frank's supporting arguments mostly fail against the source — including one his paper leans on heavily — but his core argument stands, and it changes my recommendation.** The disputed case should default to prove-or-reject, on one condition his own paper names: the explicit opt-in governance construct (the construct Frank labels R4; called "the opt-in construct" below) is decided in the same motion, not deferred. Sections 2–4 do the claim-by-claim work; §5 adjudicates the crux; §6 states the updated recommendation.

---

## 0. Key terms used below

Defined once here; the argument does not change if you read these definitions in place of the terms.

- **The disputed case** — the single situation this document is about: a write whose value is *derived* from other fields (illustrative shape: `set Balance = Deposits - Withdrawals`) landing in a declared numeric bound (e.g. `nonnegative`) that the compiler (a) *cannot prove* the write always satisfies, (b) is *decidable* (the bound is a plain numeric range, not an open-ended relation), and (c) is *non-proof-carrying* (see next term). Frank's paper calls this "the cell"; below it is "the disputed case."
- **Proof-carrying vs. non-proof-carrying bound** — a bound is *proof-carrying* when some other compile-time fault proof leans on it: a downstream expression consumes the field and needs the bound to prove *itself* safe (e.g. a later division needs a divisor's `positive` to rule out divide-by-zero). A bound is *non-proof-carrying* when nothing else depends on it — it stands alone as a declared constraint. Only non-proof-carrying bounds are in dispute; proof-carrying bounds prove-or-reject unconditionally (both sides agree). (This document uses **"band"** and "bound" interchangeably for a declared numeric range such as `nonnegative` or `min 0`.)
- **The fault-floor analysis** — the research synthesis at `research/architecture/compiler/fault-floor-definition-2026-06-11.md` (two independent adjudication passes plus an adversarial "red-team" review, all converging) that classified which runtime failures are *true faults* versus *governance*. Frank's paper and this one both call it "the substrate."
- **The disposition surface** — wherever the tooling shows the author each constraint's classification (REJECTED / CLEAN / GOVERNED) — a hover, an inline decoration, a diagnostic.
- **The proof engine "discharges" an obligation** — it clears/satisfies a proof obligation, either by proving the property or because a stated fact exactly matches the value's expression. **Proof debt** = an obligation the author must discharge at authoring time.
- **The commit sweep** ("the sweep") — the runtime's post-mutation enforcement point: after all of an operation's writes complete, every constraint is re-checked against the finished working copy; if any fails, the whole working copy is discarded and the operation refused. Its typed refusal outcome is `ConstraintsFailed`.
- **The composition seam** (`philosophy.md:55`) — the point where a *carried* constraint is handed to a downstream fault proof so that proof may rely on it. It matters only for proof-carrying bounds.
- **The opt-in construct** (Frank's "R4") — a proposed source-level keyword by which an author *explicitly* declares "govern this at runtime rather than demand a compile-time proof."
- **Disposition labels (REJECTED / CLEAN / GOVERNED)** in the `.precept` samples below are *design labels from this argument* — what the proposed design would do — not current compiler output. `(today: …)` notes flag where HEAD diverges.
- **"Archaeology"** — citing what the code happens to do at HEAD as evidence for what it *should* do; discounted as a form of argument throughout.
- **Divergence 1** — the specific disagreement adjudicated here. **Divergence 2** — a separate owner question (how severe a *proven-always-violating* write should be), left untouched.

---

## 1. The one verified fact neither paper fully engaged

Before the claim-by-claim: one foundational fact turns out to be decisive for several of Frank's steps, and neither paper put it on the table squarely.

The spec defines **two** runtime enforcement points, not one:

> "This post-mutation sweep is one of two enforcement points. Externally-sourced values are first validated against their declared constraints at **ingress** … The sweep then re-checks every constraint against the completed working copy. **Ingress governs *what enters*; the sweep governs *the result*.**" (`docs/language/precept-language-spec.md:1969`)

> "Constraints are evaluated against the working copy **after all mutations complete**. If every constraint passes, the working copy is promoted … If any constraint fails, the working copy is discarded." (`precept-language-spec.md:1967`; same at `:149`)

> "A *rule condition* — **and a *constraint modifier*, which is rule shorthand (§2.4)** — is checked against the **complete working copy *after* all mutations**." (`precept-language-spec.md:1354`)

And the sweep's failure outcome is typed governance, not the fault trap:

> "`EventOutcome.ConstraintsFailed` scope: Covers ALL post-fire constraints: global rules, state ensures (`in`/`to`/`from`), AND event ensures." (`docs/runtime/result-types.md:121`)

So by the spec's own design — reasoning about the committed *end state* rather than intermediate steps, which is the mandated framing — a derived write into a `nonnegative`/`min 0` bound **already has a governed enforcement point with a typed recoverable outcome**: after all writes complete, the commit sweep evaluates the desugared bound (`min 0` rewrites to the equivalent `rule Balance >= 0`) against the finished working copy and refuses with `ConstraintsFailed` on failure. The only thing missing at HEAD is the internal wiring that routes a desugared bound into that runtime constraint check (`DesugarsToRule → constraint-plan`), which has zero consumers today (`research/architecture/compiler/fault-floor-definition-2026-06-11.md:144`) — and that wiring is already a **floor prerequisite under Frank's own position too** (committed-state discharge of *true* faults across a plan's earlier writes is unsound without it, `fault-floor-definition:128,144`). Its absence is a build-sequencing fact, inadmissible for either side.

This is the disputed case in concrete form — an excerpt shaped after `samples/contractor-invoice-settlement.precept` (its `AmountApplied as money … nonnegative` field and `OutstandingBalance … <- LaborCharge - AmountApplied` computed field):

```precept
# Illustrative — mirrors samples/contractor-invoice-settlement.precept conventions.
field Deposits    as money in 'USD' default '0.00 USD' nonnegative
field Withdrawals as money in 'USD' default '0.00 USD' nonnegative
field Balance     as money in 'USD' default '0.00 USD' nonnegative   # the declared bound

from Open on Settle
    -> set Balance = Deposits - Withdrawals    # derived write into a nonnegative bound
    -> no transition
```

Disposition of the `set Balance = …` line:
- **Under the proposed design: REJECTED at compile time** — the compiler cannot prove `Deposits - Withdrawals >= 0` (nothing constrains `Withdrawals <= Deposits`), and the `nonnegative` bound is decidable and non-proof-carrying, so it is a proof debt the author must discharge.
- **Runtime meaning if it were allowed to ship: GOVERNED** — the commit sweep would refuse an out-of-band result with `ConstraintsFailed`, leaving the entity valid.
- **(today:** the compiler checks each write for containment directly rather than routing through the sweep — PRE0078, marked "Current" at `fault-floor-definition:177` — and the sweep-governance path that would yield `ConstraintsFailed` for a bound has no consumers yet, `fault-floor-definition:144`.**)**

This fact governs the verdicts on Frank's Steps 1–2 and his entire §4, below.

---

## 2. Frank's argument, claim by claim

### Claim 1 (his §1): the dispute is exactly one case; external-ingress GOVERN and provable-clean are uncontested

**Strongest form of his claim:** three-quarters of the apparent territory is agreed; fencing it prevents the debate from borrowing force from uncontested ground.
**Verdict: sound.** Accurate fencing; matches thesis §2.4 exactly. No complaint.

### Claim 2 (Steps 1–2): governance is by the philosophy's own text an *ingress* mechanism; a derived value never crosses that boundary, so "the seam has nothing to attach to"

**Strongest form of his claim:** `philosophy.md` §0.7-region and `precept-language-spec.md:268` scope the Governance leg to "every value entering the entity from outside the definition… at the moment it enters." A derived value is manufactured inside the plan; there is no ingress slot at which governance discharges anything.

**Verdict: overstated by textual omission — this is the claim §1 above defeats.** Frank quotes `:268` (the ingress paragraph) accurately, but `:268` itself points at §3A.4, and §3A.4 says in terms that ingress is *one of two* enforcement points and that **"the sweep governs the result"** (`:1969`). The result of `set Balance = Deposits - Withdrawals` is exactly what the sweep's jurisdiction covers, and `:1354` says constraint modifiers — as rule shorthand — are checked against the complete working copy after all mutations. Frank's "there is no ingress slot at which governance could make the bound true" is true and beside the point: the enforcement point for a derived result is not an ingress slot, it is the commit sweep the spec already defines. The composition seam of `philosophy.md:55` (the point where a carried constraint is handed to a downstream fault proof) genuinely does not close for a derived write — Frank is right about that — but nothing needs it to: the seam exists so a *fault proof* can lean on a carried constraint; a non-proof-carrying bound has, by definition, no fault proof leaning on it. Nothing severs.

The contrast between a proof-carrying bound and a stand-alone (non-proof-carrying) one is visible in `samples/invoice-line-item.precept`, whose own comment says the source-field modifiers "are what make the computed totals provably non-negative — no separate runtime rules required":

```precept
# From samples/invoice-line-item.precept (excerpt).
field UnitPrice as money in '{CurrencyCode}' default '0 {CurrencyCode}' nonnegative editable
field Quantity  as integer default 1 positive editable
field Subtotal  as money in '{CurrencyCode}' <- UnitPrice * Quantity   # CLEAN
```

Here `nonnegative` and `positive` on the source fields are **proof-carrying**: the proof that `Subtotal` is itself non-negative leans on them, so the whole line proves clean with no runtime rule. The disputed `Balance` bound is the opposite — nothing downstream consumes it, so it carries no proof; it is a stand-alone declared truth. (`Subtotal` disposition **CLEAN** under the proposed design *and* today — this is a shipped sample.)

One honest residue survives: enforcing at commit means a *transiently* out-of-band intermediate (`set Balance = -50 → set Balance = Balance + 100`) commits clean. That is a real semantic choice the owner should see named — but note it is the semantic the spec *already declares* for constraints ("checked against the complete working copy *after* all mutations," `:1354`; "An invalid configuration never exists, even transiently" is about the *committed* configuration, `:1967`), with SQL deferred-constraint precedent. It also exposes a sharpening that cuts against the *current implementation* of Frank's own position: PRE0078 checks containment per-write (`fault-floor-definition:177` "Current"), which proves a property **stronger than the constraint's defined meaning** — the constraint binds the final copy, not every intermediate assignment. Whichever way the owner rules, the obligation should be scoped to the final per-plan value.

### Claim 3 (Step 3 + his §3): "recoverable" in the consequence taxonomy means *actionable by an external agent*; a derived-write refusal is addressed to no one; hence it is a fault in costume — and the thesis "drops the recoverable half of the sentence"

This is the load-bearing move of the paper and the genuinely new argument. Strongest form: the caller of `Settle` supplied valid args; the refusal "Balance must be at least 0" names nothing the outside world can fix on this call; a refusal no agent can act on is not a governed refusal but an unhandled case surfacing at runtime.

**Verdict: the reinterpretation is not supported by the text, and the "addressed to no one" premise is defeated by Frank's own Fix 3 — but the concern underneath it is real and is the actual crux.** Three parts:

1. **The text.** `fault-floor-definition:122` verbatim: "A fault is a mid-evaluation abort with no recoverable typed outcome (only the Faulted trap); **anything the working-copy discard or ingress refusal handles (ConstraintsFailed/InvalidArgs/Unmatched/Rejected) is governance**." The axis in the sentence is *typed outcome vs. abort* — does the failure have a graceful, typed runtime home, or only the trap? A derived band violation resolves to `ConstraintsFailed` at the sweep (`result-types.md:121`), which is a typed outcome: the entity stays valid, the system continues in a defined state, the refusal carries the rule's reason. Reading "recoverable" as "an external agent can supply a different value" is an *added* criterion the sentence does not contain. Frank charges the thesis with dropping half the sentence; on the text, it is his reading that adds a half.

2. **The addressee (who can act on the refusal).** Frank's own Fix 3 is a `reject` row — the same shape as the wrong-currency row in `samples/contractor-invoice-settlement.precept`:

   ```precept
   # From samples/contractor-invoice-settlement.precept (excerpt) — a reject row.
   from RemittanceHeld on Apply
       -> reject "Remittance is in {Payment.currency} — only USD remittances can be applied …"
   ```

   Fix 3 (`-> reject "Withdrawals exceed deposits — overdraft is not permitted"`) at runtime produces a `Rejected` outcome delivered to *exactly the same caller in exactly the same circumstance*: valid args, refusal caused by the entity's current governed data. `Unmatched` (all guards fail on current data) and a failing state-entry `ensure` (→ `ConstraintsFailed`, `result-types.md:121`) behave identically — valid-args operations refused because of what the entity's data currently is. All three are classified as governance in `:122`, uncontested by Frank. So "a refusal with no value-supplier is not governance" proves far too much: it would indict reject rows, guards, and ensures — the language's core conditional-gating machinery. And the refusal is in fact actionable: "Balance must be at least 0" tells the business the account is overdrawn; the recovery is a different operation sequence (deposit first, then settle), the same recovery shape as any `Unmatched`. The refusal is also **previewable**: `Inspect` shows the would-be refusal before anyone fires (`philosophy.md` full-inspectability commitment) — a first-class product behavior, not a trap.

3. **What survives.** The real content of Step 3 is not taxonomy — it is that under GOVERN the author *never explicitly confronted* the overdraft case, whereas Fix 3's behaviorally-identical refusal is *authored*. That argument does not need the "recoverable means *some external caller can supply a different value*" reinterpretation, and it is Frank's strongest point. I take it up in §5.

Both papers, honestly, over-drew on `:122`: it is a research draft's classification of *runtime consequences*. It settles what a band violation **is** at runtime (a governed refusal — the thesis is right about that, and Frank's §4(2) claim that "the only runtime thing that catches an out-of-band derived value is the Faulted trap" is contradicted by `:1969`/`result-types.md:121` at design level and leans on "today" — discounted). It does **not** settle what the *compile-time authoring posture* for the unproven case should be. That question is genuinely open on the taxonomy, and neither doc should have claimed it closed there.

### Claim 4 (Step 4): ConstraintKind has no band member; min 5 desugars to a rule; FaultCode.OutOfRange carries [StaticallyPreventable]; "the language's own metadata says an out-of-band derived value is a fault"

**Verified facts, wrong weight.** The five `ConstraintKind` members are as he says (`src/Precept/Language/ConstraintKind.cs`); the desugar clause is real (`precept-language-spec.md:1138`); `OutOfRange = 13` does carry `[StaticallyPreventable(DiagnosticCode.OutOfRange)]` (`src/Precept/Language/FaultCode.cs:47-48`). But:

- The registry entry is **the disputed artifact itself**. The fault-floor synthesis — produced by two independent adjudication copies plus a red-team, all converging — classifies OutOfRange/13 as *rule-misclassified-as-fault* and instructs: "REMOVE from the [StaticallyPreventable] registry … OutOfRange (13), LengthBoundViolation (14), CountBoundViolation (15) — no can't-compute referent" (`fault-floor-definition:210`; the convergence record is in its own §Convergence: "3 rule-misclassified (OutOfRange/Length/Count)"). Citing the current registry as evidence for its own correctness is archaeology — "HEAD does this" — and is discounted per the disciplines.
- The first half of the claim actually cuts **against** Frank. If "a derived write that violates `min 5` violates a rule, full stop," then note where rules over derived values are enforced: at the commit sweep, against the completed working copy (`:1354`), producing `ConstraintsFailed`. Frank's own ruling *relies on this* to reject the option of having the proof engine itself carry nonlinear invariants (the ruling's "Pure Model B"): a nonlinear invariant such as `rule Total == Avg * Qty` over computed fields is admitted precisely because "governance already enforces it: the runtime constraint sweep … discarding any operation that violates the identity" (`proof-engine-boundary-ruling-2026-07-06.md`, its Pure-Model-B rejection, quoting `result-types.md:121`, `§3A.4:1965`). And the `Invariant = 1` member of `ConstraintKind` ("Always enforced") *is* the runtime representation of the desugared bound — no sixth member needed.

### Claim 5 (Step 5): a derived-write refusal is "a valid operation trapped," against the process guarantee (`philosophy.md:49`)

**Verdict: weak — proves too much.** `philosophy.md:49`'s "no workflow trapped in a state it can never leave" is the structural dead-end guarantee (a state with no exit path), proven at compile time. A data-conditional refusal of one operation does not trap the workflow: the entity remains in its state with its other operations live, exactly as when a guard fails (`Unmatched`), a reject row fires (`Rejected`), or an entry ensure fails (`ConstraintsFailed`). If a data-dependent refusal were "a valid operation trapped," the same indictment lands on guards, rejects, and ensures — Precept's normal machinery. The good half of Step 5 (surface the unhandled case at authoring time) is Claim 3's survivor again, adjudicated in §5 (the crux).

### Claim 6 (his §3 close): the ruling's reversal of `fault-floor-definition:177/:210` was deliberate, and "a draft's recommendation is an argument, not authority — including my own draft's"

**Verdict: correct in principle; but the reversal's stated ground is Claim 3's reinterpretation, which fails on the text.** So the reversal stands or falls with the crux, not with the taxonomy. Worth noting for calibration (not authority): the fault-floor analysis's classification was not one drafter's whim — two independent adjudication passes and an adversarial reviewer converged on the misclassification verdict, and that reviewer's *own* re-scope ("non-proof-carrying constraints exit" replacing "bands exit," `fault-floor-definition:132`) is precisely the boundary the thesis adopted. One artifact's later reversal against a many-angles convergence needs the stronger argument, and the stronger argument here (§5, the crux) is not the one the reversal cited.

### Claim 7 (his §4): GOVERN honestly built requires (1) a new intra-plan enforcement lane, (2) a rebuilt fault pipeline with a new recoverable outcome type, (3) held-simultaneously discharge machinery — "GOVERN is the expensive one"

**Verdict: substantially overstated; the honest residue is small.**

- **(1) is wrong on the design's own terms.** No intra-plan, post-each-assignment checkpoint is needed, and no sixth `ConstraintKind` position: the constraint's *defined semantics* are commit-time ("checked against the complete working copy *after* all mutations," `:1354`), the sweep is the already-specified enforcement point for results (`:1969`), and the desugared bound is an `Invariant`. What GOVERN needs built is the `DesugarsToRule` wiring — owed under **both** positions as a floor prerequisite (`fault-floor-definition:128,144`), hence not a differential cost.
- **(2) largely dissolves.** The recoverable outcome type exists: `ConstraintsFailed` (`result-types.md:121`). Removing OutOfRange from the registry is the fault-floor analysis's own instruction (`:210`). "Who can act on the refusal" is answered in Claim 3: the same addressee as `Rejected`/`Unmatched`/failed-ensure — the caller, in domain vocabulary, with `Inspect` preview.
- **(3) is real but shared.** The proof-carrying carve-out is the thesis's own stated exception, and discharging a true fault while accounting for a plan's *earlier* writes is owed under Error too (the red-team amendment is about discharging true faults across the writes that precede them, `fault-floor-definition:128`). The genuinely differential costs of GOVERN are: keeping the proof-carrying classification honest as expressions evolve (a bound can *become* proof-carrying when some distant expression starts consuming the field — a change here silently reclassifies a constraint over there, which the disposition surface must track), and naming the commit-time-vs-per-write semantic. Real, and much smaller than Frank's §4 claims.

Frank's closing line here — "slapping GOVERNED on the current behavior without building all of that ships the Faulted trap relabeled" — is true and uncontested; the thesis already makes a sound ledger and real enforcement behind every GOVERNED tag a required property of the completed product (thesis §2.7(b)). Under end-state framing this is a completeness obligation both sides carry, not an argument for either.

### Claim 8 (his §5): the honest cost accounting of Error

**Verdict: honest and creditable — and it under-counts one dynamic while over-crediting nothing.** The bound-invention cost is real (`compile-time-niche-decision-packet-2026-06-10.md:129` "representability masquerading as policy"; 214 unbounded vs 10 bounded money fields, `bounds-only-constraint-enforcement-2026-06-05.md:120-122`). Two things Frank's accounting misses, one on each side:

- **Against Error (new, neither doc stated it): the constraint-deletion dynamic ("band-suppression").** The 214:10 ratio does not just price the ceremony — it predicts author behavior. Under reject-on-unprovable, an author who wants to *document* "Balance shouldn't go negative" but cannot prove it will often not invent bounds — they **delete the band**, losing the proof *and* the governance *and* the documentation. A posture that punishes declaring a constraint suppresses constraint declaration, which for a domain-integrity product is the perverse outcome: fewer declared truths. (The opt-in construct neutralizes this — see §5, the crux.)
- **For Error (new): the fixes relocate governance to where it is actionable.** Fix 1 (`rule Deposits >= Withdrawals`) doesn't just feed the prover — as a declared rule it is *itself governed*, so the offending `Withdraw` is now refused **at its own ingress**, at the moment of overdraft, addressed to the actor who caused it — strictly better-located than a downstream `Settle` refusal phrased in output terms. Prove-or-reject's demand for a carrier is often a demand to move the constraint to the boundary where an agent *can* act. That is the composition seam's logic generalized, and it is a genuine identity argument for Error that Frank gestures at but never lands explicitly.

One correction to his fix menu: Fix 3 as written rejects *every* Settle. The honest authored form is the Fix-2 guard plus a sibling reject row. That pair also exposes Error's real ergonomic tax for non-trivial derivations: the guard must *restate the derivation as a precondition* (`when Base * RiskFactor - Discount >= 0 → set Premium = Base * RiskFactor - Discount`) — duplication the prover keeps sound but the author maintains twice. GOVERN is, in effect, an automatic post-condition; Error demands a hand-inverted precondition.

### Claim 9 (his §6): the decidability concession does not touch the disputed case

**Verdict: logically correct as stated — the obligation exists exactly when the constraint is decidable — but it leaves standing the asymmetry the concession creates, which does bear on the disputed case.** Under Frank's model, the *same* unprovable derived write is: rejected if the target's constraint is a decidable numeric bound (`min 0`); silently governed if it is an undecidable rule (e.g. a relational constraint referencing an unbounded field); and governed if it is the same predicate written as a state-scoped `ensure` (ensures over derived values are runtime gates — nobody proposes rejecting every ensure not provably always-satisfiable, and `ConstraintsFailed` covers them, `result-types.md:121`). Three spellings of one author intent, three postures, with the boundary falling on whether the constraint is in a form the analyzer can decide and how its scope is written — precisely the boundary the compile-time-niche decision packet showed is invisible to the primary author ("the same relation written three ways gets three different guarantee levels," `compile-time-niche-decision-packet-2026-06-10.md`, predictability section) and which `philosophy.md:92`'s domain-expert-author commitment makes disqualifying when illegible. Worse, it is *evadable*: the author who hits the rejection can demote the invariant to a scoped ensure or rewrite the rule into a form the compiler cannot analyze — escaping the error by making the definition strictly *less* analyzable. Frank's paper never engages this consequence. (It is substantially mitigated — not erased — by the opt-in construct, whose rejection message names the honest exit; §5, the crux.)

### Claim 10 (his §7): the opt-in-construct convergence — "the thesis's GOVERN, honestly built, converges on the same language construct I named as my own exit"; a hover tag asks readers to read differently, a construct makes authors write differently

**Verdict: correct, and it is the second-strongest thing in the paper.** The thesis's safeguard is ambient rendering of GOVERNED at the constraint — which the thesis's own adversarial pass already demoted to an *untested hypothesis* routed to `/design` with false-security as an unanswered acceptance criterion (thesis §2.7(b), Recommendation G). Frank's asymmetry is sharp and right: a source-level construct is load-bearing in the artifact; a disposition tag is a promise about tooling. His reframe of the whole fork is fair: *proof debt by default with explicit authored opt-in, versus governance by default with ambient disclosure.* That is the real question, cleanly stated, and it is a better statement of Divergence 1 than either original document's.

---

## 3. What is new versus already-engaged

**Already engaged by thesis §6 (no new work needed):** the taxonomy dispute in its original form (thesis bullet 1); the asymmetric ConstraintKind/no-band-representation leg (thesis bullet 2 — Frank *withdraws* it in his §3, a genuine concession I credit); the ambient-legibility answer to false security (thesis §2.4/§6 — now successfully attacked by Claim 10).

**Genuinely new in Frank's paper:**
1. The recoverability-addressee reinterpretation of `:122` (Claim 3) — new, central, judged unsound on the text, but carrying the real crux inside it.
2. The no-ingress-slot / seam-cannot-close argument (Claim 2) — new; defeated by §3A.4's second enforcement point.
3. The §4 machinery-cost enumeration (Claim 7) — new; substantially overstated; small honest residue.
4. The fully-articulated authoring-time-confrontation argument with the three-fix demonstration (Claims 3/5/8) — the strongest new-in-force point, and the one that moves me.
5. The opt-in-construct-convergence argument and the construct-vs-hover-tag asymmetry (Claim 10) — new and correct.
6. New against the *thesis* (my own error, conceded below): the count precedent. Thesis §6 said the precedent "sits on the proof-carrying side" — half wrong. `c27a382b` (owner-committed, 2026-06-03) enforces "mincount/**maxcount** … Both a provable violation and a merely-unprovable case emit (two-way prove-or-reject)" — and maxcount is *non-proof-carrying* (`fault-floor-definition:132` exits it). A count bound is the cardinality-axis analogue of the disputed numeric bound — `mincount`/`maxcount` on a collection, as in `samples/shopping-cart.precept`:

   ```precept
   # From samples/shopping-cart.precept (excerpt) — a count bound on a collection.
   event Create(
       CatalogItems as set of string notempty mincount 1,   # count bound
       …)
   ```

   The precedent says: a write that makes such a count bound merely *unprovable* (the compiler cannot show the collection will hold at least one / at most N) is **REJECTED** at compile time, exactly as a provable violation is — two-way prove-or-reject — even though `maxcount` carries no downstream proof. So an owner-shipped decision does cover reject-on-merely-unprovable for a non-proof-carrying bound on the cardinality axis. Weighted per the disciplines (an owner-committed choice is more than HEAD archaeology, less than a forward argument; the fault-floor analysis post-dates it and recommends revisiting maxcount), it is real evidence for Frank's default that I mischaracterized. *(Disposition **REJECTED** is the shipped `c27a382b` behavior — owner-committed, not merely current HEAD.)*

---

## 4. Concessions and holds — the ledger

**I concede to Frank:**
- The thesis's count-precedent characterization was half wrong (§3 item 6).
- Ambient legibility is not a sufficient safeguard for a default-GOVERN posture; a hover/inline tag is a tooling promise, not a source-level fact, and my own earlier critique of that ambient-disclosure safeguard already said as much. Claim 10 lands.
- The authoring-time-confrontation argument is a genuine identity argument, not paternalism: an unprovable derived write into a declared band *is* an unanswered domain question ("what happens on overdraft?"), and "One file, complete rules" (`philosophy.md`) favors the answer living in the source.
- His §3 withdrawal of the no-band-representation leg was honest and correct, and his §5 cost accounting is fair as far as it goes.
- His fork-statement (§7 final paragraph) is the right statement of the question.

**I hold against Frank:**
- The recoverability reinterpretation of `fault-floor-definition:122` reads a meaning into the text that is not there; on the text, a derived band violation resolving to `ConstraintsFailed` is governance-classified, and the "addressed to no one" premise is defeated by `Rejected`/`Unmatched`/failed-ensure equivalence and by `Inspect` previewability (Claim 3).
- Governance is not ingress-only; the spec's own §3A.4 gives results a governed enforcement point ("the sweep governs the result," `:1969`; modifiers checked against the complete working copy, `:1354`) (Claim 2).
- §4's machinery bill is mostly non-differential or already-owed; GOVERN is not "the expensive one" (Claim 7).
- Step 4's registry citation is archaeology (citing HEAD as its own justification) against the fault-floor analysis's own many-angles adjudication (Claim 4); Step 5's trapped-workflow framing proves too much (Claim 5).
- His model retains an unengaged spelling/scope asymmetry (decidable band rejects; undecidable rule and scoped ensure govern) that is illegible to the primary author and evadable by de-analyzing the definition (Claim 9).

**Where the thesis was wrong beyond the concession list:** it over-billed the bound-invention corpus cost to this case (the 214:10 figure prices relational-rule *sources*; the disputed case bites only bounded derived-write *targets*, rare today — though the constraint-deletion dynamic in Claim 8 restores much of the force in behavioral form); and it treated `:122` as closer to dispositive for the compile posture than a runtime-consequence taxonomy can be.

---

## 5. The crux, adjudicated

Strip the failed supports from both sides and one question remains — Frank's own final sentence, which I accept as the true form of Divergence 1:

> "Is a merely-unprovable derived write a proof debt the author discharges at authoring time, or a governance choice the author declares explicitly in source?"

Note what this question concedes in both directions. The runtime disposition is settled and it is the thesis's: a violating derived write, in the completed product, is refused by the sweep as `ConstraintsFailed` — a governed refusal, previewable, typed, with the rule's reason (§1 above; `:1967-1969`, `result-types.md:121`). And the compile-time question is settled in neither doc's original form: it is a *default-and-construct* question — reject-by-default with an authored opt-in to governance, or govern-by-default with disclosure.

Two considerations decide it for me, and they both favor Frank's default:

**First, the failure-mode asymmetry.** Under reject-by-default, the failure mode is over-rejection: visible at authoring time, bounded, and exitable (declare the carrier, guard the operation, author the reject row — or, with the opt-in construct, write one honest token). Under govern-by-default, the failure mode is a *silently shipped unanswered case*: the definition compiles clean, the domain question ("what happens on overdraft?") was never confronted, and it surfaces in production as a refusal at the point in the operation graph where it is *least* actionable — downstream of the ingress that caused it, phrased in output terms. A prevention-identity product should choose the failure mode that lands at authoring time. This is the forward, identity-grounded version of Frank's Steps 3/5, and it survives every textual defeat above.

**Second, the safeguard asymmetry (Claim 10).** Error's harshness has a designed exit that is load-bearing in the source (the opt-in construct — flagged by Frank's ruling `proof-engine-boundary-ruling:262-263` and by the thesis's own ambient-disclosure recommendation as the honest shape). GOVERN's dishonesty risk has, as of both documents, only an untested UI hypothesis. When one position's mitigations are source constructs and the other's are hover tags, the first position's risks are better contained.

Against these, the thesis's surviving arguments — the taxonomy's runtime classification, the sweep's existence, the bound-invention / constraint-deletion cost, the spelling-asymmetry legibility problem — establish that GOVERN is *coherent, cheap, and honest as a runtime semantics*, and they demolish the claim that GOVERN is expensive or category-confused. What they do not do is carry the *default*: none of them answers "the definition shipped without the author ever answering the question the band poses." I tried the answer "the band's generated because *is* the answer — refuse, same as ingress" — but ingress refusal has a structural justification (the value's supplier is the addressee) that the derived case genuinely lacks in the *authoring* sense even though it does not lack it in the *runtime* sense (Claim 3). Frank's distinction between those two senses is the real discovery in his paper, even though his taxonomy argument garbled it.

**Stress test — does the update survive without the opt-in construct?** If the owner declines any new construct, the raw choice is Error (corpus-scale friction, guard-restatement duplication, and the constraint-deletion dynamic) versus GOVERN (silent deferral behind an unbuilt disclosure surface). Painfully, still Error: the friction is visible and safe; the deferral is invisible and shipped. But the margin narrows enough that the constraint-deletion dynamic becomes a serious product wound — which is why the recommendation below makes the construct a co-requisite of ratifying Error, not an optional future.

**Verdict on the task's four options:** Frank's rebuttal does not *defeat* GOVERN (most of its attacking arguments fail against the source), and it does not merely *leave it standing* — it **forces the synthesis both papers were circling**: runtime semantics per the thesis (a band violation is governance, enforced by the sweep), compile default per Frank (unproven derived writes into decidable non-proof-carrying bands are a proof debt), joined by the explicit opt-in construct as the single honest bridge. Frank's paper states this synthesis in its last third; the thesis's Recommendation A + G state most of its other half. Neither original position survives unmodified, but Frank's needs the smaller modification — on the crux, he is right.

---

## 6. Updated recommendation to the owner (Divergence 1)

**I change my recommendation: ratify Error — prove-or-reject — as the default for a merely-unprovable, definition-derived write into a non-proof-carrying decidable declared bound, conditional on the opt-in-governance construct being routed to `/design` in the same motion as this ratification, with the rejection diagnostic required to name both exits (the discharging carrier, and — once designed — the opt-in construct).** Proof-carrying bounds prove-or-reject unconditionally (both sides agree). The proven-always-violating severity question (Divergence 2) remains a separate owner sentence.

**Deciding reason, in two sentences:** An unprovable derived write into a declared band is an unanswered domain question, and over-rejection fails visibly at authoring time with a cheap authored exit, while govern-by-default fails invisibly — the unanswered case ships and surfaces in production at its least actionable point, guarded only by a disclosure surface that is still an untested hypothesis. But Error without the opt-in construct suppresses band declaration itself (authors will delete undischargeable bands rather than invent bounds), so the disputed case's disposition and the construct are one design decision, not two.

Secondary riders the owner should see (all argued above): (a) the compile obligation should bind the *final per-plan value*, not every intermediate write — the constraint's own defined semantics are commit-time (`:1354`, `:1967`); (b) the spelling/scope asymmetry (decidable band vs. undecidable rule vs. scoped ensure) is a live legibility cost of Error that the rejection message and disposition surface must carry, since it cannot be removed; (c) the fault-registry corrections (`fault-floor-definition:210`) remain correct and orthogonal — OutOfRange exits the fault registry *even under Error*, because the compile check's identity is containment-obligation, not fault-prevention, and the runtime lane is `ConstraintsFailed`, not a trap; Frank's Step 4 should not be read as defending the registry status quo, which the fault-floor analysis he relies on adjudicated a misclassification.

Nothing above is ratified; this document, Frank's paper, and the thesis are arguments for the owner's decision.

---

*Revised for legibility 2026-07-12 — argument and citations unchanged; coined terms defined/replaced, illustrative samples added; sample dispositions are the proposed design, not current-compiler output.*
