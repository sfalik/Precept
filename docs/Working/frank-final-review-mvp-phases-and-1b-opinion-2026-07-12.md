---
title: "Frank — Final Review: MVP-Phases Incorporation Check, §1b-for-MVP Opinion, and Product Direction"
status: Draft — 2026-07-12 (Frank's opinion; not owner-ratified)
author: Frank (Lead/Architect & Language Designer)
owner: Shane
reviews: docs/Working/proof-engine-mvp-and-proof-phases-2026-07-12.md (post-incorporation version, commit 5459d66d)
context: Final step in the prove-or-reject design debate chain. Shane asked Frank to (1) confirm his prior 7 revision instructions (docs/Working/frank-review-proof-engine-mvp-phases-2026-07-12.md §7) were incorporated correctly, (2) give a first-principles (not corpus-bound) opinion on whether §1b (multi-fact linear combination) is needed for the MVP, and (3) give any final candid input on product direction given Shane's intent to ratify the prove-or-reject disposition.
note: This is Frank's opinion, not a ratification. The disposition, the §1b build decision, and any spec:256/spec:225 overrides remain Shane's to decide.
---

# Frank — Final Review: Incorporation, §1b for MVP, and Direction

Shane — I've re-read my own review and §7 instructions, the current phases doc in full, my position paper §5, and re-verified the one-hop enforcement code. Here's where I land.

## 1. Incorporation check — landed, and one deliberate meaning-shift you should see

All seven revision instructions were incorporated, and incorporated *well* — not mechanically. Verified item by item:

1. **§1/§1a/§1b split — done correctly.** §1a is "Multi-term facts + guard-sum matching (the single-fact core)"; §1b is "Combining two or more facts." The doc states plainly "The 77-sample corpus shows **no case that requires §1b**." ✓
2. **spec:256 per-sub-item — done.** §1a carries "Spec relationship (§1a): likely no override needed" (correctly scoped as a fact-shape *wording widening*, not a depth-bound override). §1b quotes spec:256 verbatim, recasts `Intervals.cs:629`/`Composition.cs:512` as "the locked decision's enforcement in code… exactly what a §1b build would change," and states "Building §1b therefore requires an owner-authorized override… a staging document cannot grant that." That's exactly the reframe I asked for. ✓
3. **§5(a) ↔ spec:225 binding — done.** "§5(a)'s justification format is a hard precondition for §1 being spec-legal at all — not a build-ordering nicety," with the Farkas-certificate criterion flagged as itself an owner ratification. ✓
4. **Opener softened — done, verbatim.** "assumed-settled here for scoping purposes," the code-comment caveat, and the "if the disposition softens, the staging is void" contingency are all present. ✓
5. **Phase-5 sequencing caveat on "179 sites" — done** ("A sequencing caveat on the 179-site headline"). ✓
6. **Preserved-correctly items — intact.** §7 still defers `.sum`/`.average` with no syntax sketched; §6 introduces no bridge construct; the adversary-corrected honesty grades are untouched. ✓
7. **General tone correction — done throughout**; owner-gated language is consistent.

**The one meaning-shift, and why it's correct, not an error:** my original recommendation was "defer §1b *until a corpus case forces it*." The revision deliberately does **not** say that — it reframes §1b as **"OPEN — owner decision, weighed on domain grounds, feasibility pending,"** and explicitly says "it is *not* deferred by default." That is a genuine change from my text — but it's the *right* change, because it's your steer landing: the corpus isn't comprehensive, so "corpus forces it" is the wrong trigger. The doc correctly elevated §1b from "defer-by-default" to "open owner call on domain grounds." I endorse the change. It's also precisely why you're now asking me the §1b question — so let me answer it as a first-principles question, not a corpus-counting one.

**Verdict on incorporation:** faithful, complete, no meaning distorted. Nothing to fix.

## 2. Is §1b needed for the MVP? — No. Defer it, and don't commit to building it yet.

**My position: do not build §1b for the MVP, and do not put it on a committed phase. Keep it exactly where the doc now has it — OPEN — but my architect's recommendation is a firm *not now*, with a concrete trigger for revisiting.** This is a real position, not a hedge. Here's the reasoning, deliberately not leaning on the corpus.

### Your ergonomics-vs-expressiveness read is correct — and I can make it airtight without the corpus

You said omitting §1b costs *authoring ergonomics* (write an extra rule), not *expressiveness* (nothing becomes unsayable). That's right, and here's the first-principles proof, which needs no corpus at all:

> **§1b only ever combines linear facts, and the combination of a finite set of linear inequalities is itself a linear inequality.** Therefore the derived bound §1b would compute is *always expressible as a single authored rule or guard* — `rule <linear expr> <op> <linear expr>`. There is no linear consequence that §1b can prove and the author cannot state as one fact.

So §1b is, by construction, never an expressiveness gap. It is pure automation of a derivation the author can always perform by hand and write down as one rule. That's a mathematical guarantee, not a corpus artifact — which is the kind of ground you asked me to reason from.

### But I'll refine your read in two directions — one that helps §1b, one that hurts it

**Helps §1b (be honest about this):** the friction §1b removes is a *different, less noble* friction than the one I defended in my position paper §5. There, I argued prove-or-reject friction is worth paying because it forces the author to *confront an unhandled case* — a genuine domain question ("what happens on overdraft?"). §1b's friction is **not** that. With §1b's shape, the author *has* handled everything — both source rules exist, the case is complete — they're just being made to *re-derive and restate a consequence the compiler could compute*. That's redundant-derivation friction, not confront-the-case friction, and it has *less* philosophical justification than the friction I defended. So I won't pretend §1b's cost is zero or that it's the "good" kind of friction. It's the annoying kind.

**Hurts §1b (and this is decisive):** the magnitude of that friction is much smaller than it first looks, for a reason the doc doesn't fully draw out — **named intermediate fields collapse almost every multi-fact chain into single-hop.** In the doc's own flagship §1b example (`Planned − Allocated` divisor), a domain author who introduces `field OpenCapacity <- PlannedUnits - AllocatedUnits` and rules against *that* turns the two-rule transitive chain into facts §1a already reads. Real domain authors name their quantities — "open capacity," "available headroom," "uncommitted balance" are *how the business already thinks*. Precept's whole premise is that the author reasons in domain terms; domain terms *are* the intermediate names. So the population of cases where a value depends on a genuinely un-nameable, deep, multi-party linear chain — that a competent domain author couldn't or wouldn't decompose into named fields — is much thinner than "any domain with multiple interacting constraints" suggests.

### Where I went looking for a real §1b need (beyond the corpus) — and what I found

I constructed the hardest cases I could across domains you named:

- **Finance — covenant/waterfall structures** (senior/mezz/equity tranches, simultaneous leverage + coverage ratios). This is the strongest candidate: multiple simultaneous linear constraints, and the "safe" bound on a residual genuinely emerges from combining several. **But** — authors of these already name every tier (`SeniorBalance`, `AvailableCash`, `CoverageHeadroom`) because the business does. Named intermediates ⇒ single-hop. And where the reasoning is a *true* simultaneous-constraint LP, that's not Precept computing a field bound — that's an optimization problem living *outside* the entity's integrity contract. Precept governs "can this data become this," not "solve this LP."
- **Inventory/allocation** (committed ≤ planned, allocated ≤ committed, divide by open remainder — the doc's example). Real, but the textbook fix is one named `OpenCapacity` field. Single-hop.
- **Healthcare (bed/resource allocation), logistics (multi-leg capacity):** same pattern — the intermediate quantity is a named, business-meaningful field, and once named the chain is single-hop.

I could not construct a case where (a) the combined bound is genuinely un-nameable as an intermediate field, **and** (b) hand-deriving the composite rule is *hard* (not merely tedious), **and** (c) the reasoning properly belongs inside Precept's integrity contract rather than an external solver. All three have to hold for §1b to earn a general Farkas solver, and I couldn't make them co-occur.

### The build cost is *larger* than it looks, which flips the ROI hard

§1b is not just "one more strategy." It is:

- **The single greenfield build in the whole roadmap** (no linear-solver code exists — confirmed).
- **The only item requiring a locked-spec override** (spec:256) *and* riding on the certificate ratification (spec:225).
- **A soundness-critical build in a *prevention* engine.** A general fact-combining solver must reproduce, *in general form*, the two protections the one-hop discipline currently enforces structurally: circularity prevention and fact-staleness (the `ReassignedBefore` stamps). Get either wrong and the solver *proves false things* — which in a prove-or-reject engine means shipping "structurally impossible" guarantees that aren't. That is the worst failure mode Precept can have, and a Farkas/simplex core is exactly where it hides.

So the ROI is: **small, avoidable ergonomic benefit** vs. **the highest-complexity, highest-soundness-risk, spec-override-requiring build in the plan.** That's a clear defer.

### My confidence, and what would change my mind

**Confidence: high** that §1b is not needed for the MVP and not an expressiveness gap (the linear-conclusion argument is a theorem, not an observation). **Medium-high** that its ergonomic benefit stays small in practice (this rests on the "authors name intermediates" claim — strong, but empirical).

**What would flip me to "build it":** a *real* definition (not corpus, not synthetic) exhibiting **all three** of: (1) a computed bound whose derivation genuinely resists being named as an intermediate field without contortion; (2) evidence that authors hand-derive the composite rule *wrong* often enough that the drift hazard bites in practice, not just in theory; (3) the pattern *recurring* across enough definitions to amortize a soundness-critical solver build. One anecdote isn't enough — I'd want the recurrence, because the cost here is a general solver, not a point fix. Bring me that and I'll reverse.

## 3. Final input on direction — ratify prove-or-reject; hold the line at §1a; watch three Large items

**Prove-or-reject is the right disposition and *is* your unique value — ratify it.** I'm fully consistent with my position paper here. The reason to pay its authoring friction is that structural prevention is the differentiator. GOVERN quietly degrades Precept into "validation that can defer to runtime" — which is the commodity every framework already ships. The moment an invalid configuration can transiently exist and get refused at runtime, you've lost the one sentence that makes Precept worth adopting. So prove-or-reject isn't a tax on the product; it *is* the product. Ratify it.

**On buildability — the MVP is genuinely tractable for solo-plus-agents, *if you hold the line at §1a.*** Phases 1–2 are well-scoped: §2, §3, §4a, §5a are all Small/Medium and ride on substrate that already ships (I re-verified the load-bearing anchors — `NumericInterval` four-corner math, guard-splitting, `ProofLedger.ComputedInterval`, the `Functions.cs` range-rule template). §1a is the biggest MVP build but stays *inside* spec:256 and reuses the existing fact machinery. That's a real, finishable plan.

**Three scope-creep red flags, in priority order:**

1. **§1b (the Farkas solver) — the #1 risk, covered above.** It's the one item that could quietly consume a disproportionate share of a small team's capacity *and* introduce soundness bugs. Keep it OPEN and unscheduled. Don't let "we'll need it eventually" pull it into a committed phase.
2. **§5b (independent re-checker) — a *second* full proof implementation** covering all ~11 strategy shapes. Its value is builder-side drift assurance, not author unblocking. For a solo-plus-agents team this is a large, low-author-visibility build. The doc already marks it deferrable — I'd go further and say **don't build it until the strategy set stops changing**, or you'll be re-checking a moving target twice.
3. **§4b (derived safe-condition printing) — Large, with a real readability failure mode** on multi-input formulas. The Phase-1 half (§4a: show the computed range + one breaking example) delivers most of the author value at Small–Medium cost. Treat §4b as genuinely optional.

**The 90%-value path is Phases 0–3** (baseline + money wiring + case-by-case + rejection numbers + §5a justification + §1a + §6 rules-with-arithmetic). That's it. Everything past Phase 3 is deferrable without hurting the core promise. If you want the simplest thing that makes prove-or-reject shippable against the product's own house style, that's the line.

**One sequencing caution the doc gestures at but should be a hard gate:** prove-or-reject + Phase-5 obligation-generation will start rejecting a *lot* of definitions that compile clean today (the §1a sequencing caveat and BUG-020 both flag this). If you flip obligation-generation on *before* the Phase 1–2 proof types land, authors hit an over-rejection wall — the exact "too complex to use" failure you're worried about. **Gate Phase 5 obligation-generation behind Phases 1–2.** Get the proof strength in before you turn up the rejection volume, or the first real user experience of prove-or-reject is a brick wall.

## 4. Bottom line

Ratify prove-or-reject — it's the product's unique value, and I hold that position without reservation. Adopt Phases 0–3 as the plan; that's a tractable solo-plus-agents build resting on substrate that already ships. **Do not build §1b, and don't schedule it** — it's not an expressiveness gap (linear conclusions are always sayable as one rule), its ergonomic benefit is small once authors name intermediate fields, and its cost is the single highest-risk, spec-override-requiring, soundness-critical build in the roadmap. Leave it OPEN with the trigger I named (a recurring, genuinely un-nameable, hand-derivation-error-prone real case). Watch §5b and §4b as the other two Large items that can quietly balloon. And gate Phase-5 obligation-generation behind the Phase 1–2 proof types so prove-or-reject's debut isn't an over-rejection wall. Hold the line at §1a and the juice is well worth the squeeze.

*Per my standing consultation discipline: this is my opinion, not a ratification. The disposition, the §1b build call, and the spec:256/spec:225 overrides remain yours.*
