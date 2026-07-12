---
title: "Analysis of Frank's Prove-or-Reject Rebuttal — Divergence 1"
status: Draft — 2026-07-11
author: Independent analysis (Fable)
analyzes: docs/Working/frank-prove-or-reject-position-2026-07-11.md (Frank's rebuttal) vs. docs/Working/precept-identity-and-guarantees-thesis-2026-07-11.md §2.4(ii)/§6 Divergence 1 (my thesis)
disagreement: whether a merely-unprovable, definition-derived write into a non-proof-carrying decidable declared bound is Error (prove-or-reject, Frank) or GOVERN (thesis)
note: Nothing herein is owner-ratified. Frank's paper, my thesis, and this analysis are all agent artifacts — arguments, not rulings. Every load-bearing citation below was verified first-hand this session against the file at the cited line.
---

# Analysis of Frank's Rebuttal — and an Honest Update

I authored the thesis. This analysis is not a defense of it. The short version: **Frank's supporting arguments mostly fail against the source — including one his paper leans on heavily — but his core argument stands, and it changes my recommendation.** The cell should default to prove-or-reject, on one condition his own paper names: the explicit opt-in governance construct (R4) is decided in the same motion, not deferred. Sections 2–4 do the claim-by-claim work; §6 adjudicates; §7 states the updated recommendation.

---

## 1. The one verified fact neither paper fully engaged

Before the claim-by-claim: one piece of substrate turns out to be decisive for several of Frank's steps, and neither paper put it on the table squarely.

The spec defines **two** runtime enforcement points, not one:

> "This post-mutation sweep is one of two enforcement points. Externally-sourced values are first validated against their declared constraints at **ingress** … The sweep then re-checks every constraint against the completed working copy. **Ingress governs *what enters*; the sweep governs *the result*.**" (`docs/language/precept-language-spec.md:1969`)

> "Constraints are evaluated against the working copy **after all mutations complete**. If every constraint passes, the working copy is promoted … If any constraint fails, the working copy is discarded." (`precept-language-spec.md:1967`; same at `:149`)

> "A *rule condition* — **and a *constraint modifier*, which is rule shorthand (§2.4)** — is checked against the **complete working copy *after* all mutations**." (`precept-language-spec.md:1354`)

And the sweep's failure outcome is typed governance, not the fault trap:

> "`EventOutcome.ConstraintsFailed` scope: Covers ALL post-fire constraints: global rules, state ensures (`in`/`to`/`from`), AND event ensures." (`docs/runtime/result-types.md:121`)

So by the spec's own design — in end-state terms, which is the mandated framing — a derived write into `min 0` **already has a governed enforcement point with a typed recoverable outcome**: the post-mutation sweep evaluates the desugared `rule Balance >= 0` against the completed working copy and refuses with `ConstraintsFailed` on failure. The only thing missing at HEAD is the `DesugarsToRule → constraint-plan` wiring, which has zero consumers today (`research/architecture/compiler/fault-floor-definition-2026-06-11.md:144`) — and that wiring is already a **floor prerequisite under Frank's own position too** (committed-state tier-2 discharge of true faults is unsound without it, `fault-floor-definition:128,144`). Its absence is a build-sequencing fact, inadmissible for either side.

This fact governs the verdicts on Frank's Steps 1–2 and his entire §4, below.

---

## 2. Frank's argument, claim by claim

### Claim 1 (his §1): the dispute is exactly one cell; external-ingress GOVERN and provable-clean are uncontested

**Steelman:** three-quarters of the apparent territory is agreed; fencing it prevents the debate from borrowing force from uncontested ground.
**Verdict: sound.** Accurate fencing; matches thesis §2.4 exactly. No complaint.

### Claim 2 (Steps 1–2): governance is by the philosophy's own text an *ingress* mechanism; a derived value never crosses that boundary, so "the seam has nothing to attach to"

**Steelman:** `philosophy.md` §0.7-region and `precept-language-spec.md:268` scope the Governance leg to "every value entering the entity from outside the definition… at the moment it enters." A derived value is manufactured inside the plan; there is no ingress slot at which governance discharges anything.

**Verdict: overstated by textual omission — this is the claim §1 above defeats.** Frank quotes `:268` (the ingress paragraph) accurately, but `:268` itself points at §3A.4, and §3A.4 says in terms that ingress is *one of two* enforcement points and that **"the sweep governs the result"** (`:1969`). The result of `set Balance = Deposits - Withdrawals` is exactly what the sweep's jurisdiction covers, and `:1354` says constraint modifiers — as rule shorthand — are checked against the complete working copy after all mutations. Frank's "there is no ingress slot at which governance could make the bound true" is true and beside the point: the enforcement point for a derived result is not an ingress slot, it is the commit sweep the spec already defines. The composition seam of `philosophy.md:55` genuinely does not close for a derived write — Frank is right about that — but nothing needs it to: the seam exists so a *fault proof* can lean on a carried constraint; a non-proof-carrying band has, by definition, no fault proof leaning on it. Nothing severs.

One honest residue survives: enforcing at commit means a *transiently* out-of-band intermediate (`set Balance = -50 → set Balance = Balance + 100`) commits clean. That is a real semantic choice the owner should see named — but note it is the semantic the spec *already declares* for constraints ("checked against the complete working copy *after* all mutations," `:1354`; "An invalid configuration never exists, even transiently" is about the *committed* configuration, `:1967`), with SQL deferred-constraint precedent. It also exposes a sharpening that cuts against the *current implementation* of Frank's own position: PRE0078 checks containment per-write (`fault-floor-definition:177` "Current"), which proves a property **stronger than the constraint's defined meaning** — the constraint binds the final copy, not every intermediate assignment. Whichever way the owner rules, the obligation should be scoped to the final per-plan value.

### Claim 3 (Step 3 + his §3): "recoverable" in the consequence taxonomy means *actionable by an external agent*; a derived-write refusal is addressed to no one; hence it is a fault in costume — and the thesis "drops the recoverable half of the sentence"

This is the load-bearing move of the paper and the genuinely new argument. Steelman: the caller of `Settle` supplied valid args; the refusal "Balance must be at least 0" names nothing the outside world can fix on this call; a refusal no agent can act on is not a governed refusal but an unhandled case surfacing at runtime.

**Verdict: the reinterpretation is not supported by the text, and the "addressed to no one" premise is defeated by Frank's own Fix 3 — but the concern underneath it is real and is the actual crux.** Three parts:

1. **The text.** `fault-floor-definition:122` verbatim: "A fault is a mid-evaluation abort with no recoverable typed outcome (only the Faulted trap); **anything the working-copy discard or ingress refusal handles (ConstraintsFailed/InvalidArgs/Unmatched/Rejected) is governance**." The axis in the sentence is *typed outcome vs. abort* — does the failure have a graceful, typed runtime home, or only the trap? A derived band violation resolves to `ConstraintsFailed` at the sweep (`result-types.md:121`), which is a typed outcome: the entity stays valid, the system continues in a defined state, the refusal carries the rule's reason. Reading "recoverable" as "an external agent can supply a different value" is an *added* criterion the sentence does not contain. Frank charges the thesis with dropping half the sentence; on the text, it is his reading that adds a half.

2. **The addressee.** Frank's own Fix 3 is `-> reject "Withdrawals exceed deposits — overdraft is not permitted"` — which at runtime produces a `Rejected` outcome delivered to *exactly the same caller in exactly the same circumstance*: valid args, refusal caused by the entity's current governed data. `Unmatched` (all guards fail on current data) and a failing state-entry `ensure` (→ `ConstraintsFailed`, `result-types.md:121`) behave identically — valid-args operations refused because of what the entity's data currently is. All three are classified as governance in `:122`, uncontested by Frank. So "a refusal with no value-supplier is not governance" proves far too much: it would indict reject rows, guards, and ensures — the language's core conditional-gating machinery. And the refusal is in fact actionable: "Balance must be at least 0" tells the business the account is overdrawn; the recovery is a different operation sequence (deposit first, then settle), the same recovery shape as any `Unmatched`. The refusal is also **previewable**: `Inspect` shows the would-be refusal before anyone fires (`philosophy.md` full-inspectability commitment) — a first-class product behavior, not a trap.

3. **What survives.** The real content of Step 3 is not taxonomy — it is that under GOVERN the author *never explicitly confronted* the overdraft case, whereas Fix 3's behaviorally-identical refusal is *authored*. That argument does not need the "recoverable-by-whom" reinterpretation, and it is Frank's strongest point. I take it up in §6.

Both papers, honestly, over-drew on `:122`: it is a research draft's classification of *runtime consequences*. It settles what a band violation **is** at runtime (a governed refusal — the thesis is right about that, and Frank's §4(2) claim that "the only runtime thing that catches an out-of-band derived value is the Faulted trap" is contradicted by `:1969`/`result-types.md:121` at design level and leans on "today" — discounted). It does **not** settle what the *compile-time authoring posture* for the unproven case should be. That question is genuinely open on the taxonomy, and neither doc should have claimed it closed there.

### Claim 4 (Step 4): ConstraintKind has no band member; min 5 desugars to a rule; FaultCode.OutOfRange carries [StaticallyPreventable]; "the language's own metadata says an out-of-band derived value is a fault"

**Verified facts, wrong weight.** The five `ConstraintKind` members are as he says (`src/Precept/Language/ConstraintKind.cs`); the desugar clause is real (`precept-language-spec.md:1138`); `OutOfRange = 13` does carry `[StaticallyPreventable(DiagnosticCode.OutOfRange)]` (`src/Precept/Language/FaultCode.cs:47-48`). But:

- The registry entry is **the disputed artifact itself**. The fault-floor synthesis — produced by two independent adjudication copies plus a red-team, all converging — classifies OutOfRange/13 as *rule-misclassified-as-fault* and instructs: "REMOVE from the [StaticallyPreventable] registry … OutOfRange (13), LengthBoundViolation (14), CountBoundViolation (15) — no can't-compute referent" (`fault-floor-definition:210`; the convergence record is in its own §Convergence: "3 rule-misclassified (OutOfRange/Length/Count)"). Citing the current registry as evidence for its own correctness is archaeology — "HEAD does this" — and is discounted per the disciplines.
- The first half of the claim actually cuts **against** Frank. If "a derived write that violates `min 5` violates a rule, full stop," then note where rules over derived values are enforced: at the sweep, against the completed working copy (`:1354`), producing `ConstraintsFailed`. Frank's own ruling *relies on this* to reject Pure Model B — the nonlinear invariant `rule Total == Avg * Qty` over computed fields is admitted precisely because "governance already enforces it: the runtime constraint sweep … discarding any operation that violates the identity" (`proof-engine-boundary-ruling-2026-07-06.md`, Pure-B rejection, quoting `result-types.md:121`, `§3A.4:1965`). And the `Invariant = 1` member ("Always enforced") *is* the runtime representation of the desugared bound — no sixth member needed.

### Claim 5 (Step 5): a derived-write refusal is "a valid operation trapped," against the process guarantee (`philosophy.md:49`)

**Verdict: weak — proves too much.** `philosophy.md:49`'s "no workflow trapped in a state it can never leave" is the structural dead-end guarantee (a state with no exit path), proven at compile time. A data-conditional refusal of one operation does not trap the workflow: the entity remains in its state with its other operations live, exactly as when a guard fails (`Unmatched`), a reject row fires (`Rejected`), or an entry ensure fails (`ConstraintsFailed`). If a data-dependent refusal were "a valid operation trapped," the same indictment lands on guards, rejects, and ensures — Precept's normal machinery. The good half of Step 5 (surface the unhandled case at authoring time) is Claim 3's survivor again, adjudicated in §6.

### Claim 6 (his §3 close): the ruling's reversal of `fault-floor-definition:177/:210` was deliberate, and "a draft's recommendation is an argument, not authority — including my own draft's"

**Verdict: correct in principle; but the reversal's stated ground is Claim 3's reinterpretation, which fails on the text.** So the reversal stands or falls with the crux, not with the taxonomy. Worth noting for calibration (not authority): the substrate's classification was not one drafter's whim — two independent copies and a red-team converged on the misclassification verdict, and the red-team's *own* re-scope ("non-proof-carrying constraints exit" replacing "bands exit," `fault-floor-definition:132`) is precisely the boundary the thesis adopted. One artifact's later reversal against a multi-vantage convergence needs the stronger argument, and the stronger argument here (§6) is not the one the reversal cited.

### Claim 7 (his §4): GOVERN honestly built requires (1) a new intra-plan enforcement lane, (2) a rebuilt fault pipeline with a new recoverable outcome type, (3) held-simultaneously discharge machinery — "GOVERN is the expensive one"

**Verdict: substantially overstated; the honest residue is small.**

- **(1) is wrong on the design's own terms.** No intra-plan, post-each-assignment checkpoint is needed, and no sixth `ConstraintKind` position: the constraint's *defined semantics* are commit-time ("checked against the complete working copy *after* all mutations," `:1354`), the sweep is the already-specified enforcement point for results (`:1969`), and the desugared bound is an `Invariant`. What GOVERN needs built is the `DesugarsToRule` wiring — owed under **both** positions as a floor prerequisite (`fault-floor-definition:128,144`), hence not a differential cost.
- **(2) largely dissolves.** The recoverable outcome type exists: `ConstraintsFailed` (`result-types.md:121`). Removing OutOfRange from the registry is the substrate's own instruction (`:210`). "Recoverable by whom" is answered in Claim 3: by the same addressee as `Rejected`/`Unmatched`/failed-ensure — the caller, in domain vocabulary, with `Inspect` preview.
- **(3) is real but shared.** The proof-carrying carve-out is the thesis's own stated exception, and write-aware tier-2 discharge is owed under Error too (the red-team amendment is about true-fault discharge across prefix writes, `fault-floor-definition:128`). The genuinely differential costs of GOVERN are: the surgical proof-carrying classification kept honest as expressions evolve (a bound can *become* proof-carrying when a distant expression starts consuming the field — action at a distance the disposition surface must track), and the commit-vs-per-write semantic being named. Real, and much smaller than §4 claims.

Frank's closing line here — "slapping GOVERNED on the current behavior without building all of that ships the Faulted trap relabeled" — is true and uncontested; the thesis already makes a sound ledger and real enforcement behind every GOVERNED tag a required property of the completed product (thesis §2.7(b)). Under end-state framing this is a completeness obligation both sides carry, not an argument for either.

### Claim 8 (his §5): the honest cost accounting of Error

**Verdict: honest and creditable — and it under-counts one dynamic while over-crediting nothing.** The bound-invention cost is real (`compile-time-niche-decision-packet-2026-06-10.md:129` "representability masquerading as policy"; 214 unbounded vs 10 bounded money fields, `bounds-only-constraint-enforcement-2026-06-05.md:120-122`). Two things Frank's accounting misses, one on each side:

- **Against Error (new, neither doc stated it): band-suppression.** The 214:10 ratio does not just price the ceremony — it predicts author behavior. Under reject-on-unprovable, an author who wants to *document* "Balance shouldn't go negative" but cannot prove it doesn't invent bounds — they **delete the band**, losing both the proof *and* the governance *and* the documentation. A posture that punishes declaring a constraint suppresses constraint declaration, which for a domain-integrity product is the perverse outcome: fewer declared truths. (An opt-in construct neutralizes this — see §6.)
- **For Error (new): the fixes relocate governance to where it is actionable.** Fix 1 (`rule Deposits >= Withdrawals`) doesn't just feed the prover — as a declared rule it is *itself governed*, so the offending `Withdraw` is now refused **at its own ingress**, at the moment of overdraft, addressed to the actor who caused it — strictly better-located than a downstream `Settle` refusal phrased in output terms. Prove-or-reject's demand for a carrier is often a demand to move the constraint to the boundary where an agent *can* act. That is the composition seam's logic generalized, and it is a genuine identity argument for Error that Frank gestures at but never lands explicitly.

One correction to his fix menu: Fix 3 as written rejects *every* Settle. The honest authored form is the Fix-2 guard plus a sibling reject row. That pair also exposes Error's real ergonomic tax for non-trivial derivations: the guard must *restate the derivation as a precondition* (`when Base * RiskFactor - Discount >= 0 → set Premium = Base * RiskFactor - Discount`) — duplication the prover keeps sound but the author maintains twice. GOVERN is, in effect, an automatic post-condition; Error demands a hand-inverted precondition.

### Claim 9 (his §6): the decidability concession does not touch the disputed cell

**Verdict: logically correct as stated — the obligation exists exactly when the constraint is decidable — but it leaves standing the asymmetry the concession creates, which does bear on the cell.** Under Frank's model, the *same* unprovable derived write is: rejected if the target's constraint is a decidable band (`min 0`); silently governed if it is an undecidable rule (e.g., relational with an unbounded reference); and governed if it is the same predicate written as a state-scoped `ensure` (ensures over derived values are runtime gates — nobody proposes rejecting every ensure not provably always-satisfiable, and `ConstraintsFailed` covers them, `result-types.md:121`). Three spellings of one author intent, three postures, with the boundary being the analytic fragment / constraint-scope line — precisely the boundary the niche packet showed is invisible to the primary author ("the same relation written three ways gets three different guarantee levels," `niche-packet` predictability section) and which `philosophy.md:92`'s domain-expert-author commitment makes disqualifying when illegible. Worse, it is *evadable*: the author who hits the rejection can demote the invariant to a scoped ensure or de-analyze the rule — escaping the error by making the definition strictly less analyzable. Frank's paper never engages this consequence. (It is substantially mitigated — not erased — by an opt-in construct whose rejection message names the honest exit; §6.)

### Claim 10 (his §7): the R4 convergence — "the thesis's GOVERN, honestly built, converges on the same language construct I named as my own exit"; a hover tag asks readers to read differently, a construct makes authors write differently

**Verdict: correct, and it is the second-strongest thing in the paper.** The thesis's safeguard is ambient rendering of GOVERNED at the constraint — which the thesis's own adversarial pass already demoted to an *untested hypothesis* routed to `/design` with false-security as an unanswered acceptance criterion (thesis §2.7(b), Recommendation G). Frank's asymmetry is sharp and right: a source-level construct is load-bearing in the artifact; a disposition tag is a promise about tooling. His reframe of the whole fork is fair: *proof debt by default with explicit authored opt-in, versus governance by default with ambient disclosure.* That is the real question, cleanly stated, and it is a better statement of Divergence 1 than either original document's.

---

## 3. What is new versus already-engaged

**Already engaged by thesis §6 (no new work needed):** the taxonomy dispute in its original form (thesis bullet 1); the asymmetric ConstraintKind/no-band-representation leg (thesis bullet 2 — Frank *withdraws* it in his §3, a genuine concession I credit); the ambient-legibility answer to false security (thesis §2.4/§6 — now successfully attacked by Claim 10).

**Genuinely new in Frank's paper:**
1. The recoverability-addressee reinterpretation of `:122` (Claim 3) — new, central, judged unsound on the text, but carrying the real crux inside it.
2. The no-ingress-slot / seam-cannot-close argument (Claim 2) — new; defeated by §3A.4's second enforcement point.
3. The §4 machinery-cost enumeration (Claim 7) — new; substantially overstated; small honest residue.
4. The fully-articulated authoring-time-confrontation argument with the three-fix demonstration (Claims 3/5/8) — the strongest new-in-force point, and the one that moves me.
5. The R4-convergence argument and the construct-vs-hover-tag asymmetry (Claim 10) — new and correct.
6. New against the *thesis* (my own error, conceded below): the count precedent. Thesis §6 said the precedent "sits on the proof-carrying side" — half wrong. `c27a382b` (owner-committed, 2026-06-03) enforces "mincount/**maxcount** … Both a provable violation and a merely-unprovable case emit (two-way prove-or-reject)" — and maxcount is *non-proof-carrying* (`fault-floor-definition:132` exits it). So an owner-shipped decision does cover reject-on-merely-unprovable for a non-proof-carrying bound on the cardinality axis. Weighted per the disciplines (an owner-committed choice is more than HEAD archaeology, less than a forward argument; the fault-floor synthesis post-dates it and recommends revisiting maxcount), it is real evidence for Frank's default I mischaracterized.

---

## 4. Concessions and holds — the ledger

**I concede to Frank:**
- The thesis's count-precedent characterization was half wrong (§3 item 6).
- Ambient legibility is not a sufficient safeguard for a default-GOVERN posture; a hover/inline tag is a tooling promise, not a source-level fact, and my own C2 critique already said as much. Claim 10 lands.
- The authoring-time-confrontation argument is a genuine identity argument, not paternalism: an unprovable derived write into a declared band *is* an unanswered domain question ("what happens on overdraft?"), and "One file, complete rules" (`philosophy.md`) favors the answer living in the source.
- His §3 withdrawal of the no-band-representation leg was honest and correct, and his §5 cost accounting is fair as far as it goes.
- His fork-statement (§7 final paragraph) is the right statement of the question.

**I hold against Frank:**
- The recoverability reinterpretation of `fault-floor-definition:122` is eisegesis; on the text, a derived band violation resolving to `ConstraintsFailed` is governance-classified, and the "addressed to no one" premise is defeated by `Rejected`/`Unmatched`/failed-ensure equivalence and by `Inspect` previewability (Claim 3).
- Governance is not ingress-only; the spec's own §3A.4 gives results a governed enforcement point ("the sweep governs the result," `:1969`; modifiers checked against the complete working copy, `:1354`) (Claim 2).
- §4's machinery bill is mostly non-differential or already-owed; GOVERN is not "the expensive one" (Claim 7).
- Step 4's registry citation is archaeology against the substrate's own multi-vantage adjudication (Claim 4); Step 5's trapped-workflow framing proves too much (Claim 5).
- His model retains an unengaged spelling/scope asymmetry (decidable band rejects; undecidable rule and scoped ensure govern) that is illegible to the primary author and evadable by de-analyzing the definition (Claim 9).

**Where the thesis was wrong beyond the concession list:** it over-billed the bound-invention corpus cost to this cell (the 214:10 figure prices relational-rule *sources*; the disputed cell bites only bounded derived-write *targets*, rare today — though the band-suppression dynamic in Claim 8 restores much of the force in behavioral form); and it treated `:122` as closer to dispositive for the compile posture than a runtime-consequence taxonomy can be.

---

## 5. The crux, adjudicated

Strip the failed supports from both sides and one question remains — Frank's own final sentence, which I accept as the true form of Divergence 1:

> "Is a merely-unprovable derived write a proof debt the author discharges at authoring time, or a governance choice the author declares explicitly in source?"

Note what this question concedes in both directions. The runtime disposition is settled and it is the thesis's: a violating derived write, in the completed product, is refused by the sweep as `ConstraintsFailed` — a governed refusal, previewable, typed, with the rule's reason (§1 above; `:1967-1969`, `result-types.md:121`). And the compile-time question is settled in neither doc's original form: it is a *default-and-construct* question — reject-by-default with an authored opt-in to governance, or govern-by-default with disclosure.

Two considerations decide it for me, and they both favor Frank's default:

**First, the failure-mode asymmetry.** Under reject-by-default, the failure mode is over-rejection: visible at authoring time, bounded, and exitable (declare the carrier, guard the operation, author the reject row — or, with R4, write one honest token). Under govern-by-default, the failure mode is a *silently shipped unanswered case*: the definition compiles clean, the domain question ("what happens on overdraft?") was never confronted, and it surfaces in production as a refusal at the point in the operation graph where it is *least* actionable — downstream of the ingress that caused it, phrased in output terms. A prevention-identity product should choose the failure mode that lands at authoring time. This is the forward, identity-grounded version of Frank's Steps 3/5, and it survives every textual defeat above.

**Second, the safeguard asymmetry (Claim 10).** Error's harshness has a designed exit that is load-bearing in the source (the R4 construct — flagged by Frank's ruling `proof-engine-boundary-ruling:262-263` and by the thesis's own Recommendation G as the honest shape). GOVERN's dishonesty risk has, as of both documents, only an untested UI hypothesis. When one position's mitigations are source constructs and the other's are hover tags, the first position's risks are better contained.

Against these, the thesis's surviving arguments — the taxonomy's runtime classification, the sweep's existence, the bound-invention/band-suppression cost, the spelling-asymmetry legibility problem — establish that GOVERN is *coherent, cheap, and honest as a runtime semantics*, and they demolish the claim that GOVERN is expensive or category-confused. What they do not do is carry the *default*: none of them answers "the definition shipped without the author ever answering the question the band poses." I tried the answer "the band's generated because *is* the answer — refuse, same as ingress" — but ingress refusal has a structural justification (the value's supplier is the addressee) that the derived case genuinely lacks in the *authoring* sense even though it does not lack it in the *runtime* sense (Claim 3). Frank's distinction between those two senses is the real discovery in his paper, even though his taxonomy argument garbled it.

**Stress test — does the update survive without R4?** If the owner declines any new construct, the raw choice is Error (corpus-scale friction, guard-restatement duplication, band-suppression) versus GOVERN (silent deferral behind an unbuilt disclosure surface). Painfully, still Error: the friction is visible and safe; the deferral is invisible and shipped. But the margin narrows enough that the band-suppression dynamic becomes a serious product wound — which is why the recommendation below makes the construct a co-requisite of ratifying Error, not an optional future.

**Verdict on the task's four options:** Frank's rebuttal does not *defeat* GOVERN (most of its attacking arguments fail against the source), and it does not merely *leave it standing* — it **forces the synthesis both papers were circling**: runtime semantics per the thesis (a band violation is governance, enforced by the sweep), compile default per Frank (unproven derived writes into decidable non-proof-carrying bands are a proof debt), joined by the explicit opt-in construct as the single honest bridge. Frank's paper states this synthesis in its last third; the thesis's Recommendation A + G state most of its other half. Neither original position survives unmodified, but Frank's needs the smaller modification — on the crux, he is right.

---

## 6. Updated recommendation to the owner (Divergence 1)

**I change my recommendation: ratify Error — prove-or-reject — as the default for a merely-unprovable, definition-derived write into a non-proof-carrying decidable declared bound, conditional on the R4 opt-in-governance construct being routed to `/design` in the same motion as this ratification, with the rejection diagnostic required to name both exits (the discharging carrier, and — once designed — the opt-in construct).** Proof-carrying bounds prove-or-reject unconditionally (both sides agree). The proven-always-violating severity fork (Divergence 2 / Recommendation H) remains a separate owner sentence.

**Deciding reason, in two sentences:** An unprovable derived write into a declared band is an unanswered domain question, and over-rejection fails visibly at authoring time with a cheap authored exit, while govern-by-default fails invisibly — the unanswered case ships and surfaces in production at its least actionable point, guarded only by a disclosure surface that is still an untested hypothesis. But Error without the opt-in construct suppresses band declaration itself (authors will delete undischargeable bands rather than invent bounds), so the cell's disposition and the construct are one design decision, not two.

Secondary riders the owner should see (all argued above): (a) the compile obligation should bind the *final per-plan value*, not every intermediate write — the constraint's own defined semantics are commit-time (`:1354`, `:1967`); (b) the spelling/scope asymmetry (decidable band vs. undecidable rule vs. scoped ensure) is a live legibility cost of Error that the rejection message and disposition surface must carry, since it cannot be removed; (c) the fault-registry corrections (`fault-floor-definition:210`) remain correct and orthogonal — OutOfRange exits the fault registry *even under Error*, because the compile check's identity is containment-obligation, not fault-prevention, and the runtime lane is `ConstraintsFailed`, not a trap; Frank's Step 4 should not be read as defending the registry status quo, which his own substrate adjudicated a misclassification.

Nothing above is ratified; this document, Frank's paper, and the thesis are arguments for the owner's decision.
