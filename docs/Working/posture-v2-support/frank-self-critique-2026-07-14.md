---
title: "Frank's Self-Critique of the Go-Forward Posture Replay (v1)"
date: 2026-07-14
author: Frank (Lead/Architect & Language Designer)
status: Working — one of five independent inputs to the posture v2 rewrite; NOT the rewrite
owner: Shane
reviews:
  - docs/Working/frank-go-forward-posture-replay-2026-07-14.md (the doc under critique)
  - docs/Working/frank-canonical-capture-recommendation-2026-07-14.md (drift check)
context: >
  Shane asked for a candid, adversarial-to-myself re-read of the go-forward posture
  replay as one of several independent inputs to a stronger v2. This is NOT the rewrite.
  I re-read the full grounding corpus fresh, then read my own posture doc as I would a
  junior dev's PR. Documentation-only; no source code inspected. Line-level citation
  verification is a separate parallel pass — where I touch a citation here it is because
  the citation carries a *substantive* over/under-statement, not to audit line numbers.
---

# Frank — Self-Critique of the Posture Replay (v1)

Shane — you asked me to be as hard on this as I'd be on someone else's PR. I was. The short version: **the spine is right and faithful to canon — provenance routes, decidability is the discharge-oracle not the router, derived values prove-or-reject, everything else governs, overflow is parked. That is the ratified model and the doc states it.** But it carries two genuine *overstatements* that are conceptually load-bearing (not cosmetic), it leaves **two Fable points from `fable-analysis-of-frank-response-2026-07-11.md` unclosed** — the exact two that will re-open the debate — and it **drifts from my own canonical-capture recommendation on the naming of the boundary axis.** None of it needs structural rework. All of it needs fixing before v2 can claim to "end the re-litigation."

I've ranked the findings. HIGH = will re-open a settled debate if shipped as-is. MEDIUM = weakens the doc or leaves a critique un-answered. LOW = precision debt.

---

## HIGH-severity findings

### H1 — "Runtime governance is not a weaker guarantee — the SAME guarantee" is an overstatement that undercuts our own rejection of Option B

**What's wrong.** §4 closes with: *"Runtime governance is not a weaker guarantee — for both governed cases it is the SAME guarantee, delivered by a different mechanism."* This flattens a distinction the philosophy itself draws and that our whole case against Option B depends on.

**Why (source).** The claim is true at the level of the **outcome** — no invalid configuration persists, whether by proof, ingress, or sweep. It is *false at the level of the epistemic/temporal guarantee* for the relational-invariant case. Spec §0.7 Composition is explicit that for the governed *external-value-feeding-a-fault-proof* case the fault is *"complete at compile time … nothing left to a runtime check"* (`precept-language-spec.md:270`) — i.e. governance there only *discharges a precondition of a proof that already exists*. But a declared relational invariant over independently-set fields (§3) is **not proven at compile time at all** — there is no fault proof; it is enforced *only* by the post-mutation sweep. Critique C1 Finding 1 names exactly this honesty gap: *"Event-B's guarantee is deductive (formal proof); Precept's is operational (runtime enforcement) … Precept **gives up** Event-B's invariant-preservation proof in exchange for runtime enforcement"* (`critique-c1-uniqueness.md` F1). Our own philosophy lists **"Compile-time structural checking"** as a *distinct* commitment from **"Prevention, not detection"** (`philosophy.md:49`/`:59`) — governance delivers the second, not the first. Calling the sweep-governed invariant "the SAME guarantee" as a compile-time proof erases the very axis (compile-time structural impossibility vs. runtime discard) that makes prove-or-reject worth forcing on authors. It is self-undermining: if governing a computed relationship is genuinely the *same* guarantee, a reader is entitled to ask why we ban governing a computed *value* (Option B) — the answer, from my own 07-11 position, is that a computed value governed at runtime is *"a fault in a governed refusal's costume"* because it has no ingress door. The doc can't have it both ways.

**How I'd fix it.** Split the claim in two. (a) **Outcome guarantee** — identical: no invalid configuration ever persists, by any of the three enforcement points. (b) **Epistemic guarantee** — *not* identical: a proven fault is a compile-time structural impossibility (known before any entity exists); a governed relational invariant is a runtime-swept discard (the definition may *attempt* the violation; the sweep refuses the commit). State plainly, per C1, that for declared relational invariants Precept trades deductive invariant-preservation for structural runtime enforcement — and that this trade is *why* the external-value and derived-value cases are routed differently. Honesty here strengthens the anti-Option-B case; the current flattening weakens it.

---

### H2 — The "two distinct mechanisms" framing collides head-on with the canonical §0.7, and borrows a canonical quote to bless a split canon doesn't make

**What's wrong.** §4/§5 build heavily on "governance runs by **two distinct mechanisms** — ingress governance and the post-mutation constraint sweep," and §4 says *"Keeping the two distinct is what makes the guarantee 'precise rather than magical'"* citing `precept-language-spec.md:264`.

**Why (source).** The canonical Guarantee Contract (`precept-language-spec.md:262–270`) uses the phrase *"two distinct mechanisms; keeping them distinct is what makes the guarantee precise rather than magical"* to mean **compile-time fault prevention vs. runtime governance** — *not* ingress-vs-sweep. And §0.7's Governance paragraph defines governance as enforcement *"on every value entering the entity from outside the definition — event arguments, construction inputs, direct field edits — at the moment it enters"* (`:268`) — **ingress only.** The whole-entity post-mutation sweep is real (`result-types.md:121` `ConstraintsFailed` "Covers ALL post-fire constraints: global rules, state ensures, event ensures"; `spec:1969` "the sweep governs the result"), and my boundary ruling's worked row 10 leans on it ("enforced by the constraint sweep," `boundary-ruling:159`) — but it is **not** part of the ratified §0.7 contract, which describes governance as ingress. So the posture doc has (i) minted a *second* "two mechanisms" split that competes with canon's, and (ii) repurposed §0.7's "precise rather than magical" sentence — which blesses the compile/runtime split — to bless my own ingress/sweep split. That's dressing a synthesis in borrowed authority. Note the three-way inconsistency this creates: **spec §0.7 = governance-is-ingress**; **my own canonical-capture doc §1c = "Governance — runtime, on external input only"** (ingress); **this posture doc = ingress + sweep.** All three are mine to reconcile.

**How I'd fix it.** Pick one framing and make it consistent across canon, the capture doc, and the posture. My recommendation: keep canon's primary split (compile-time fault prevention vs. runtime governance) as *the* "two distinct mechanisms," and present ingress-vs-sweep as an *elaboration of how governance is delivered* — explicitly flagged as extending §0.7, with a doc-sync note that §0.7's Governance paragraph currently under-describes governance (it names ingress but the runtime also enforces whole-entity invariants via the sweep, `result-types.md:121`). Do **not** attach the `:264` "precise rather than magical" quote to the ingress/sweep split — that quote belongs to the compile/runtime split. This is also a canon-drift flag: §0.7 may owe a sync to name the sweep. (Surfacing, not resolving — §0.7 is spec surface.)

---

### H3 — The `set`-vs-`rule` disposition asymmetry and its evadability (Fable Claim 9) is never engaged — and it directly attacks the "spelling-invariant" claim the doc rests on

**What's wrong.** §5 asserts, in bold, *"NEVER route by surface spelling"* and calls the model *"spelling-invariant."* But the doc simultaneously routes `set Balance = Deposits - Withdrawals` (derived value → **reject**) and `rule Balance == Deposits - Withdrawals` (relational invariant → **govern** via sweep) to **opposite dispositions**. The doc never confronts that a domain author sees these as the same intent spelled two ways, nor what stops an author from *restructuring* the first into the second to escape a rejection.

**Why (source).** `fable-analysis-of-frank-response-2026-07-11.md` Claim 9 raised precisely this and recorded that I never answered it: *"the same unprovable derived write is: rejected if the target's constraint is a decidable numeric bound; … governed if it is the same predicate written as a state-scoped `ensure` … Three spellings of one author intent, three postures … Worse, it is **evadable**: the author who hits the rejection can demote the invariant to a scoped ensure or rewrite the rule into a form the compiler cannot analyze — escaping the error by making the definition strictly less analyzable. Frank's paper never engages this consequence."* Ledger #9 (no escape hatch) closes only the **explicit** "trust me" marker — it does **not** address the **implicit** evasion of restructuring `set X = expr` into `field X editable` + `rule X == expr` to move from prove-or-reject into sweep-governance. My boundary ruling has the *principled* answer (row 11: *declaring a relationship* is governed, *computing a value* is proven — a genuine provenance/role difference, not spelling), but the posture doc states the "spelling-invariant" slogan **without** the row-11 reasoning that makes the `set`/`rule` split legitimate rather than hypocritical.

**How I'd fix it.** Meet it head-on: (1) explain that `set X = e` makes X a *derived* field (X's value has computed provenance) while `rule X == e` leaves X *independently-set* with an identity constraint (different provenance/role, per `boundary-ruling:160` row 11) — so the differing disposition is provenance, not spelling; (2) name the evasion candidate (restructure-to-govern) explicitly and state the answer — restructuring changes what the definition *means* (X becomes an editable field the author must now govern at its own ingress), it does not launder a computed value into a governed one; and (3) concede the residual legibility cost Fable is right about (the domain author *can* be surprised) and put the load on the rejection message + disposition surface to carry it. This is the single most likely re-litigation vector: the "spelling-invariant" bold claim is a standing invitation to someone to produce the `set`/`rule`/`ensure` triple and reopen the whole routing debate.

---

### H4 — The band-suppression tradeoff is invisible, and the doc treats "no escape hatch" as a clean win when it was an *override* of Fable's co-requisite

**What's wrong.** §6 lists the escape hatch as *"❌ CLOSE — no escape hatch"* and presents it as settled victory. It never records that the adjudicated recommendation feeding that decision made the opt-in construct a **co-requisite** of ratifying Error, nor the accepted downside of overriding it.

**Why (source).** Fable's final recommendation (`fable-analysis-of-frank-response-2026-07-11.md` §6) was: *"ratify Error … **conditional on the opt-in-governance construct being routed to `/design` in the same motion**,"* because Error-without-construct triggers band-suppression: *"an author who wants to document 'Balance shouldn't go negative' but cannot prove it will often not invent bounds — they **delete the band**, losing the proof and the governance and the documentation … for a domain-integrity product the perverse outcome"* (Claim 8), escalated in the stress-test to *"a serious product wound."* Shane then ruled **#9 CLOSE** — overriding the co-requisite. That is a legitimate owner call, but the per-decision-rationale rule (`CLAUDE.md` — every locked decision carries **tradeoff accepted**) means the posture must *record the tradeoff we took on*, not hide it. It also matters that **§6 (author-states-a-rule) does not neutralize band-suppression** — §6 lets the engine honor a fact the author *states*, but the band-suppression author has *no* dischargeable fact to state short of adding a real carrier (`rule Deposits >= Withdrawals`) or bounding the operand; the doc implies §6 is the complete answer to over-rejection, which it is for multi-fact composition but not for band-suppression.

**How I'd fix it.** In §6, under the "no escape hatch" ban, add the accepted tradeoff verbatim: closing #9 accepts the band-suppression / constraint-deletion dynamic (Fable Claim 8) as a known cost, on the ground that a *visible* over-rejection at authoring time beats a *silent* shipped unanswered case (Fable's own crux verdict, §5). And clarify that §6 answers multi-fact over-rejection, **not** band-suppression — the answer to band-suppression is the carrier or the bound, and we accept that some authors will delete the band instead. Naming it is what stops the next person from "rediscovering" it and reopening govern-by-default.

---

## MEDIUM-severity findings

### M1 — The certificate criterion and the "earns its place" stopping rule — the most important thing the whole detour produced — are under-weighted to a single clause

**What's wrong.** The posture doc mentions the certificate criterion only in §7, as one item in a list of "genuinely new material." It never states the four-leg admissibility criterion and never carries clause 4, **"earns its place."**

**Why (source).** My *own* retrospective is emphatic: the certificate criterion is *"the biggest single deposit of the whole detour"* (`frank-retrospective-proof-engine-arc-2026-07-13.md` §2), and clause 4 is *"the single most important thing the detour produced … the principled stopping rule the June effort lacked … what bounds proof-obligation growth and makes deferrals principled rather than cowardly"* (`retrospective` §5; ledger #2 EXTENSION spells out all four legs). A go-forward posture whose *stated purpose* is "so it never has to be re-litigated" that **omits the stopping rule** is missing the exact governor that prevents the *next* spiral — at aggregates (#4), any §1b revisit, or overflow (GATE-O). The doc also under-states the load-bearing **"honest only as the ratified package"** condition (ledger #1: *"A is honest only as a package with items 2 and 3"*) — it appears in §3 but not as the caveat it is: re-assert prove-or-reject over the un-strengthened engine and you get June back.

**How I'd fix it.** Promote the certificate criterion (all four legs) and the "earns its place" stopping rule to a first-class section of v2, framed as *the mechanism that ends re-litigation of "should we build more proof power."* Foreground the "honest only as the package" condition alongside it.

### M2 — The false-security caveat (C2 / R2) is absent; the absolutist headline is exactly the bundled-claim the research warns against

**What's wrong.** §2's headline — *"No invalid configuration. Ever"* — is delivered with no per-site legibility caveat, and the doc treats the guarantee as cleanly communicated.

**Why (source).** `critique-c2-false-security.md` (grounded in research R2) identifies the dominant overtrust mechanism as *"a single global claim standing in for a per-site fact"* — a reader sees "compiles clean" and generalizes it across every constraint. It further records that the ratified mitigation — the per-obligation **PROVEN vs GOVERNED** disposition surface (Q9) — is, as of the corpus, *an untested design hypothesis on the actual (domain-expert, less-technical) audience*, not a solved problem. The posture doc neither carries the caveat nor marks the disposition surface's legibility as an open acceptance criterion.

**How I'd fix it.** Add one honest paragraph: the guarantee is absolute over its live surface, *and* the per-site PROVEN/GOVERNED distinction that keeps a reader from over-generalizing is a ratified-but-unvalidated legibility surface — an open acceptance criterion, not a closed win. (Lower than HIGH because it's a communication concern more than a proof-posture error — but it is an unclosed critique point and v2 is meant to be candid.)

### M3 — DRIFT vs. my own canonical-capture recommendation: "the boundary is proof-carrying vs. not" (capture) vs. "the axis is provenance" (posture)

**What's wrong.** My `frank-canonical-capture-recommendation-2026-07-14.md` §1a states, twice, *"The boundary is proof-carrying vs. not, never band-vs-rule."* The posture doc §5/§7 states the axis is **provenance** ("Where did the operand's value come from?") and — correctly — that *"decidability decides discharge-vs-reject only, never the route."* These are two different axes named as "the boundary," in two docs I wrote the same day.

**Why this is a real finding, not a wording nit (source).** The boundary ruling **explicitly rejected** proof-carrying/decidability as the router and installed provenance: *"decidability is not the router (routing by effort would slide to Model A), but it is the discharge-oracle"* (`boundary-ruling:39`; §4 "routed by the origin/shape of the value … never by how hard O is to prove"). Canon agrees: §0.7 routes by provenance — *"before any computation derives from it"* (`spec:268`). "Proof-carrying vs. not" was the **fence around the *contested subset*** in my 07-11 position (proof-carrying bounds were *uncontested*; only non-proof-carrying derived writes were in dispute) — it was **never the model's router.** The capture doc conflates the dispute-fence with the model-boundary. This matters because the capture doc's **Step 1 recommends promoting that framing into `soundness-and-coverage.md`** — promoting "the boundary is proof-carrying vs not" would inject into canon a router the boundary ruling overruled. The posture doc is the faithful one here; the capture doc needs correction before anything is promoted.

**How I'd fix it (both docs).** Reconcile to a single statement: **the model's routing axis is provenance** (derived-by-expression → prove-or-reject; external value or declared relationship → govern); **decidability is the discharge-oracle within the prove-or-reject arm, never the router**; and **"proof-carrying vs. not" was the fence around the historically-contested subset, now subsumed — all derived-value obligations prove-or-reject regardless of whether the target bound is proof-carrying.** The capture doc's §1a and its Step 1 promotion text must be edited to say provenance, not proof-carrying, or it will canonize the overruled framing.

---

## LOW-severity / precision debt

### L1 — §5's "clean TWO-WAY routing rule, not three" glosses the parked-overflow third bucket
Overflow is neither prove-or-reject nor govern — it is a *disclosed park*, a genuine third disposition sitting **outside** the provenance axis (`boundary-ruling:25`, two-tier fault surface; ledger #7). §2 establishes this; §5's "two-way, not three" doesn't restate it. "Not three" is defensible *in context* (it rebuts the old prove/govern/**defer-the-undecidable-computed-case** middle arm), but a careful reader will catch the parked lane as an un-acknowledged third bucket. **Fix:** footnote §5 — "two-way over the *live* surface; representational overflow is the disclosed park, outside both routes."

### L2 — The final-value-vs-intermediate containment semantic (Fable rider a) is unnamed
Fable flagged that the containment obligation binds the **final per-plan value**, not every intermediate write — a transiently out-of-band intermediate (`set X = -50 → set X = X + 100`) commits clean, which is the spec's own declared semantic (`spec:1354`/`:1967`) and *"a real semantic choice the owner should see named."* The posture doc's §4 never names it. **Fix:** one sentence in §4 stating the obligation is on the committed value, not intermediates.

### L3 — `philosophy.md:55` is stretched to bless the sweep (substantive, not a line-audit point)
§4 quotes *"the runtime enforcement is what makes that carried constraint true, not a second line of defense"* (`philosophy.md:55`) in support of governance-as-not-weaker generally, including the sweep. That sentence is specifically about the **composition seam** — an *external* value carrying a constraint for a *downstream fault proof* (`philosophy.md:55` sits in the "value supplied at runtime" paragraph). It does not speak to whole-entity invariants swept post-mutation. This is the citation-level shadow of H1: the sweep case is *not* "makes a carried constraint true for a proof" — there is no proof. **Fix:** don't cite `:55` for the sweep; cite `result-types.md:121` / `spec:1969` for the sweep and reserve `:55` for the composition case.

---

## Drift summary (posture doc vs. `frank-canonical-capture-recommendation-2026-07-14.md`)

Two drifts, both mine to own:

1. **Governance decomposition.** Capture doc §1c: *"Governance — runtime, on external input only"* (ingress). Posture doc §4/§5: governance = ingress **+ post-mutation sweep**. The posture doc is the more complete/correct one (the sweep is real and enforces §3 relational invariants), but the two docs are inconsistent, and *both* diverge from where they cite canon (§0.7 = ingress). See H2. **The sweep-as-governance needs to be reconciled across all three surfaces, and §0.7 may owe a doc-sync.**

2. **The boundary axis.** Capture doc §1a: *"the boundary is proof-carrying vs. not."* Posture doc §5: the axis is **provenance**. The posture doc matches canon (§0.7) and the boundary ruling's ratified router; the capture doc regresses to the 07-11 dispute-fence framing and — worse — **recommends promoting it to canon (Step 1).** See M3. **The capture doc must be corrected to "provenance" before its Step 1 executes.**

Neither drift is a substantive disagreement about *outcomes* (both docs reject the disputed derived write, govern the relational invariant, park overflow). They are inconsistencies of *framing and axis-naming* — which is exactly the kind of thing that lets a future session re-open "wait, is the router provenance or proof-carrying? is governance one mechanism or two?" and spend a week on it. For a doc whose entire job is to end that, framing consistency *is* the job.

---

## Verdict

**v1 is fundamentally sound — no structural rework.** The spine is correct and faithful to the ratified model and to canon §0.7: provenance routes; derived values are prove-or-reject; decidability is the discharge-oracle, not the router; external values and declared relationships govern; §1b is deferred-not-rejected; overflow is parked-not-solved; no escape hatch. Every one of those is stated and correctly sourced. The doc does *not* commit the sin it was written to end (the "govern the undecidable computed case" middle arm is correctly banned).

**But it is not yet the decisive v2, and "polish" undersells the gap.** Two of the findings are genuine *overstatements* on conceptually load-bearing seams — H1 ("SAME guarantee") and H2 (the "two mechanisms" repurposing) — and left unfixed they hand the reader the material to reopen Option B and to mis-read the canonical contract. Two more are *unclosed Fable points* — H3 (the `set`/`rule` asymmetry + evadability) and H4 (band-suppression) — and they are unclosed precisely because they are the arguments that *lost narrowly*, which is what makes them the ones most likely to come back. And M3 is a drift with my own capture doc that, if not reconciled, will canonize an overruled axis.

So: **sound skeleton, but v2 must (1) fix the two overstatements by separating outcome-guarantee from epistemic-guarantee and aligning "two mechanisms" to canon; (2) explicitly close Fable Claims 8 and 9 rather than leaving them to re-emerge; (3) reconcile the provenance-vs-proof-carrying axis with the capture doc before promotion; and (4) elevate the certificate criterion + "earns its place" stopping rule to first-class, since that is the actual governor against the next spiral.** Do those and the posture is not just accurate — it's finally load-bearing enough to stop the loop. Ship it as-is and the loop has four visible re-entry points.

— Frank

*(Documentation-only self-critique. No source code inspected. No files other than this one created; the posture doc is untouched, per the phase boundary. This is one of five independent inputs to the v2 rewrite.)*

---

## Plain-text summary — top 3 self-identified gaps, ranked by re-litigation likelihood

1. **The `set`-vs-`rule`/`ensure` disposition asymmetry and its evadability is never engaged (H3).** The doc bold-claims "spelling-invariant" while routing `set X = e` (reject) and `rule X == e` (govern) oppositely, and says nothing about an author restructuring one into the other to escape prove-or-reject. Fable raised this (Claim 9) and recorded that I never answered it; I *still* haven't. A single `set`/`rule`/`ensure` triple reopens the entire routing debate — this is the most probable re-entry point.

2. **Band-suppression is invisible and "no escape hatch" is presented as a clean win (H4).** Fable's adjudication made the opt-in construct a *co-requisite* of Error because Error-without-it makes authors delete undischargeable bounds — "a serious product wound." Shane overrode that (ledger #9), but the posture records neither the tradeoff nor that §6 doesn't neutralize it. Someone will rediscover "authors just delete the bound" and reopen govern-by-default.

3. **"Runtime governance is the SAME guarantee" overstates and self-undermines (H1).** For declared relational invariants there is *no* compile-time proof — they are runtime-swept only (C1's Event-B point; §0.7 Composition). Calling that "the SAME guarantee" as a compile-time proof erases the compile-time/runtime axis that justifies rejecting Option B for computed values — inviting exactly "then why not govern everything?" Close behind: the drift with my own capture doc on whether the axis is *provenance* or *proof-carrying* (M3), which will canonize an overruled framing if the capture doc's Step 1 runs unfixed.
