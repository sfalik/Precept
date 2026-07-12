---
title: "Frank — Review of the Proof-Engine MVP & Proof-Phases Roadmap"
status: Draft — 2026-07-12 (review artifact; not owner-ratified)
author: Frank (Lead/Architect & Language Designer)
owner: Shane
reviews: docs/Working/proof-engine-mvp-and-proof-phases-2026-07-12.md
note: Review artifact intended to drive a revision pass on the reviewed document. Findings, verdicts, and citations are locked as delivered — a reviser sharpens the reviewed doc's text against them; a reviser does NOT resolve the owner-gated items named here.
---

# Frank — Review of `proof-engine-mvp-and-proof-phases-2026-07-12.md`

## 0. Orientation (for a fresh reader)

This document is my formal review of `docs/Working/proof-engine-mvp-and-proof-phases-2026-07-12.md` — the "MVP and Roadmap" costing document for the proof engine's numeric-limit proof-type capabilities. That document sits inside the broader **prove-or-reject** debate chain: the design question of whether a computed value written into a field's declared bound that the compiler *cannot prove* stays in-bounds should be **Error (prove-or-reject)** or **GOVERN**. My position paper on the core disagreement is `docs/Working/frank-prove-or-reject-position-2026-07-11.md`; the reviewed document does not re-litigate that disposition — it *conditionally costs and stages* the engine work assuming prove-or-reject is ratified. This review verifies the reviewed document's engine claims against shipped source, assesses its MVP/phase staging, and flags where its largest build silently pre-decides locked-spec questions that remain owner-gated. Everything below is stated against the *current* engine; every "compiles/rejects today" is an observation of shipped code, not a design authority.

---

## 1. Summary verdict

**Accept as a credible, honest costing document — with one architectural correction and one flagged spec pre-decision that the doc does not surface.** This is the most implementation-grounded document in the chain, and it's the first one whose engine claims I could verify line-by-line against shipped source and find *accurate*. The baseline-already-works claim is true. The missing-proof-type diagnoses are technically sound. The corpus arithmetic is trustworthy (exact on file-level counts, minor variance on per-site counts). The adversary pass caught a real false-verification error and it propagated into the main doc. This is a marked methodological improvement over the forcing/expressiveness doc, whose trichotomy rested on an engine we don't have — here every "compiles/rejects today" is explicitly labeled an observation of the *current* engine, and they hold up.

**But**: the MVP's largest item (§1 linear relationships) quietly requires relaxing the single-pass depth-bound that spec:256 locks — and the doc frames this as pure engineering ("the solver core is new") rather than as a locked-spec override. That is the same decision I already flagged, now wearing an implementation hat, and it must go back to you. Separately, I believe the doc **over-scopes §1** by conflating two separable capabilities, and the corpus-justified core does *not* need the spec-violating half.

## 2. Technical credibility (source + corpus spot-check)

**Baseline "already works today" — ACCURATE.** Verified every cited anchor:
- Currency/unit matching: `ProofEngine.Qualifiers.cs` is 954 lines (doc says ~950); PRE0114 path real. ✓
- Length limits: `ProofEngine.Lengths.cs` real, containment proof present. ✓
- Count tracking: `ProofEngine.cs:579–671` is exactly `CountContainmentObligation`/`AdvanceCount`. ✓
- Division safety: `samples/travel-reimbursement.precept:13` (`TripDays … positive`) and `:58` (`LodgingTotal / TripDays <= '350.00 USD'`) verbatim. ✓
- Rounding rules: `Functions.cs:44–155` carry hand-written `IntervalTransfer` for min/max/clamp/abs/floor/ceil/truncate/round. ✓
- Bare field-vs-field rules: `TryFlowNarrowingProof` at `Strategies.cs:1078`, `ExtractFieldToFieldLeaf` at `:1240`. ✓

**Missing-proof-type diagnoses — SOUND, and the "no forbidden technique" bar is respected.** The three load-bearing structural claims are all true against source:
- **No linear solver exists.** Grep confirms: the only "octagon/DBM" token is a comment analogy at `Composition.cs:554`, exactly as the inventory states. The solver core genuinely is greenfield. ✓
- **sqrt/pow/modulo carry no `IntervalTransfer`** → result treated as unbounded. Confirmed at `Functions.cs:183–213` (proof requirements present, no interval transfer). The proposed fix — catalog-shaped per-function range rules following the `abs` sign-split template — is sound and needs no new machinery. ✓
- **Money product wiring gap is exactly as narrow as claimed.** `Operations.cs` declares multiply/divide `IntervalTransfer` for Integer/Decimal/Number only; Money/Quantity binary ops carry none. `NumericInterval.Multiply`/`Divide` already do sentinel-safe 4-corner evaluation. So §3 really is "one wiring gap," Small, and reuses proven substrate. The doc's claim that a bare money passthrough already proves (only ×/÷ is unwired) matches the code. ✓

Critically, none of the proposed strategies smuggle in an external SMT solver, non-termination, or unsound approximation. §1's solver is the only one that edges near the line, and I address it in §4 below. **§4a (surface the already-computed range + one witness)** is credible and cheap: `ProofLedger.ProofObligation.ComputedInterval` is real and populated (`ProofEngine.cs:141`), so the numbers genuinely exist and are thrown away today.

**Corpus arithmetic — TRUSTWORTHY.** Exact matches: 77 files; 37 money files; 72 length files; 42 accumulator-write files; 149 total rules; 22 arithmetic-in-comparison rules; 0 sqrt/pow/round call sites (confirms §8's zero-demand claim); `Test.precept:1–10` verbatim; `insurance-claim.precept:127` verbatim. Minor variance where expected: the "179 accumulator writes" reproduces as 188 with a cruder regex — same order, same 42-file spread; not sloppy, just a stricter pattern than mine. **The corpus evidence is sound enough to stage on.**

## 3. Assessment of the MVP/later-phase staging

**Broadly right, with one over-scope.** The tiering discipline is good: §4 is split (cheap witness half in MVP, expensive derived-condition half deferred); §5 is split (justification format in, independent re-checker deferred); §9 correctly SKIP on zero demand + trivial exact hand-fix; §7 correctly deferred behind a language decision it flags rather than resolves. The honesty grading is fair — I checked the load-bearing ones: §3 money "CAN'T-BE-DONE-BY-HAND" is real (every hand-fix fails; only escape is deleting the `max`); §1's "DUPLICATE-A-FORMULA" clamp genuinely weakens "impossible" into "silently clamped"; §2's "TRIVIAL when it's just a cap, DUPLICATE when each case carries a formula" split is accurate. The deck is not stacked.

**Over-scope finding (the substantive one): §1 bundles two separable capabilities, and only one is corpus-justified.** The doc lists §1's genuinely-new work as *both* (a) multi-term facts (`field + field ≤ const`, constants + coefficients) *and* (b) "combining two or more facts (deliberately single-hop today)." But the flagship idiom — `when Total + Charge.Amount <= 10000 → set Total = Total + Charge.Amount` — needs only **(a)**: recognize that the assigned expression is syntactically the guarded sum, so the guard's bound transfers. That is one multi-term fact applied once — still single-hop. The 24 guard-then-apply sites, the 6 sum-vs-field rules, and the 16 add/subtract computed fields are all single-fact shapes. **I found no corpus example that requires (b) fact-combination / transitive closure.** The doc does not produce one either. So the spec:256-violating half (multi-fact combination, a Farkas/elimination core) appears to be built for a need the corpus doesn't demonstrate. **Recommendation: split §1 into §1a (multi-term single-fact representation + guard-sum matching — the corpus-justified MVP core, and it stays inside the one-hop discipline) and §1b (multi-fact combination — defer until a corpus case forces it).** This is not pedantry: §1a can likely ship without a spec override, while §1b cannot — see §4.

**Under-scope: none found.** §6 (rules-with-arithmetic) as first post-MVP upgrade is defensible (mirror-field hand-fix compiles today; 22/149 rules). Nothing left out of MVP will cause immediate pain that isn't already Phase-5-gated.

**One scoping nuance the doc understates:** §1's headline "179 sites" overstates *current* bite. The doc itself concedes the running-total-into-`nonnegative`-field case "compiles clean today… because no obligation is generated (Phase 5 work)." So most of the 179 only start rejecting once Phase 5 lands obligation generation. §1's true *today* exposure is the narrower set of bounded min/max fields with guard-then-apply. The MVP necessity is real but its urgency is coupled to Phase 5 sequencing — worth making explicit so §1 isn't over-prioritized against items that bite now.

## 4. Consistency with my six previously-flagged owner-gated items

This is where the document needs to go back to you.

- **spec:256 (single-pass, depth-bounded, "no transitive chasing") — IMPLICITLY PRE-DECIDED. Flag.** The spec (line ~256 region) states verbatim: *"The mechanism is single-pass and depth-bounded (no transitive chasing of a third field), per §0.4."* §1's item-(b) — "combining two or more facts (today is deliberately single-hop)" — is precisely a relaxation of that locked decision. The doc cites `Intervals.cs:629` and `Composition.cs:512` (the one-hop enforcement points) as things the new solver must change, but frames it as "the solver core is new" engineering rather than "this requires overriding a locked spec." **A Farkas-style linear solver inherently combines constraints; putting it in the MVP silently answers the spec:256 question I flagged.** This is the same decision wearing an implementation hat. Per my §3 finding, the good news is that the *corpus-justified* §1a core does **not** need this — so you may be able to ship the MVP without touching spec:256 at all. Either way, the decision is yours, not the document's.

- **spec:225 (SMT/Z3 ban; opaque-solvers-rejected; witnesses must be structured data) — ENGAGED but linkage UNDERSTATED. Flag.** Principle 3 in the spec bans opaque solvers *"even when they could prove more."* If §1b's linear core ever ships, its spec:225-legality depends entirely on it emitting a **legible, independently-checkable certificate** (a Farkas certificate *is* checkable — a vector of non-negative multipliers — unlike an SMT trace, so this is achievable). The doc's §5(a) justification format is exactly that story — but the doc treats §5(a) as build-ordering ergonomics, not as **the load-bearing precondition for §1's solver being spec-legal at all.** The linkage should be explicit: any linear-arithmetic strategy must ship *with* a Farkas-style structured witness, or it violates spec:225. Right now §5 reads as "nice to have first"; against spec:225 it is "constitutionally required for §1."

- **collection-types.md `.sum`/`.average` admission — CORRECTLY SURFACED, not pre-decided.** §7 explicitly flags this as an owner language decision that "must go through the language-design process," defers the whole item to a later gated phase, and does not sketch a syntax. This is exactly the discipline I want. No action beyond noting the doc handled it correctly.

- **The disposition sentence itself — assumed settled (disclosed, but the opener overstates).** The doc's premise ("prove-or-reject… is settled (`ProofEngine.cs:432`); this document does not re-litigate it") cites a *code comment* as evidence the *design intent* is settled, while the front-matter admits "Nothing owner-ratified." The costing exercise is a reasonable thing to do conditionally, but be aware: **this entire document is contingent on ratifying the disposition.** If the disposition softens, the MVP evaporates. The word "settled" in the opener should read "assumed-settled for scoping."

- **spec:223 rescope / the two competing bridge constructs — NOT pre-decided.** The document stays clear of both. §6 is proof-reading of existing syntax (no new construct); §7 defers the `.sum` construct. Good — it does not pick a bridge.

## 5. New items I want you (Shane) to explicitly decide

1. **spec:256 override, scoped.** Does the MVP get the *multi-fact* linear solver (§1b, relaxes the locked single-hop depth-bound), or only the *multi-term single-fact* core (§1a, stays inside spec:256)? My architectural read: the corpus only justifies §1a. Confirm whether you want §1b built now at all — it cannot be built without authorizing a locked-spec override.
2. **spec:225 binding on §1.** Ratify that any linear-arithmetic strategy ships *with* a legible Farkas-style certificate (structured, independently checkable) — making §5(a) a hard precondition of §1, not a later ergonomic. Confirm the certificate criterion satisfies the opaque-solver ban.
3. **§1a/§1b split.** Do you accept splitting §1 as I recommend (ship the guard-sum-matching core; defer fact-combination until a corpus case forces it)? This is the cleanest way to get the MVP's benefit without the spec override.
4. **MVP-premise contingency.** Acknowledge that this whole roadmap is downstream of ratifying the prove-or-reject disposition (still open item #1 from my prior six). If that moves, the staging is void.
5. **§7's `.sum`/`.average` language decision** remains yours and un-pre-decided — the doc correctly defers it, but it's the gating dependency for Phase 6, so it needs to enter the design process on its own timeline if aggregates are ever wanted.

*(Per my standing Pre-Design Owner Consultation discipline, I am naming these, not resolving them.)*

## 6. Closing assessment

This is a genuinely good document and the strongest in the chain on the axis that matters here — it is grounded in the engine we actually shipped, not an aspirational one, and it says so at every step. I verified a representative slice of its file:line citations and its corpus statistics and found them accurate to the point of being reproducible; the adversary pass caught a real false "compiles-clean" claim (§7's aggregate hand-fix) and the correction propagated into the main text, which is exactly the self-correction rigor I complained was missing before. The MVP set is close to right and the honesty grading is fair. My two reservations are architectural, not credibility: (1) §1 is over-scoped — it bundles a spec:256-violating multi-fact solver with a corpus-justified multi-term core that the flagship idiom doesn't actually need, and the two should be split; and (2) the document's largest build silently answers two of the locked-spec questions I already flagged to you (spec:256's depth-bound, spec:225's certificate criterion) under the cover of "just staging," and those need to come back to you as the same decisions they always were. Fix the §1 split and surface the two spec linkages explicitly, and I'm comfortable with this as the engineering roadmap for the disposition — conditional, as always, on your ratifying the disposition itself.

---

## 7. Revision Instructions (for whoever revises the phases document)

**Target file:** `docs/Working/proof-engine-mvp-and-proof-phases-2026-07-12.md`. These convert the findings above into concrete edits. Apply them to the reviewed document's *own text* — do not restate this review inside it.

**Hard boundary — do NOT resolve owner-gated items.** The following remain Shane's calls and must NOT be decided in the revision: (a) whether the MVP gets the §1b multi-fact solver at all (the spec:256 override scope); (b) whether the spec:225 Farkas-certificate criterion is ratified as satisfying the opaque-solver ban; (c) whether the prove-or-reject disposition itself is ratified. The revision's job is to **surface and sharpen these distinctions in the document's prose** — make the choices legible and correctly scoped — **not to make the calls.** Where a phrasing currently implies a decision has been made, replace it with language that flags the decision as open and owner-gated.

Concrete revision tasks:

1. **Split §1 into §1a and §1b.**
   - **§1a — multi-term single-fact core (corpus-justified MVP).** Multi-term fact representation (`field + field ≤ const`, constants + coefficients) plus guard-sum matching — recognizing that an assigned expression is syntactically the guarded sum so the guard's bound transfers. This covers the flagship idiom (`when Total + Charge.Amount <= 10000 → set Total = Total + Charge.Amount`), the 24 guard-then-apply sites, the 6 sum-vs-field rules, and the 16 add/subtract computed fields. It stays **single-hop**, inside the spec:256 depth-bound discipline.
   - **§1b — multi-fact combination (deferred).** Combining two or more facts / transitive closure — a Farkas / elimination core. State plainly that **no corpus example in the supporting analysis requires §1b**, and defer it until a corpus case forces it.

2. **State the spec:256 relationship explicitly per sub-item.** Add text making clear that **§1a can likely ship without a spec:256 override** (it stays single-pass, depth-bounded — "no transitive chasing"), while **§1b cannot** — multi-fact combination is precisely a relaxation of the locked spec:256 decision (*"The mechanism is single-pass and depth-bounded (no transitive chasing of a third field), per §0.4."*). Where §1 currently frames the new solver core as pure engineering ("the solver core is new"), reframe the §1b portion as an engineering task **contingent on an owner-authorized spec:256 override** — not as staging that quietly answers the question. Preserve the `Intervals.cs:629` and `Composition.cs:512` one-hop-enforcement citations, but recast them as the enforcement points a §1b override would have to change, not as incidental engineering.

3. **Bind §5(a) to spec:225 as a hard precondition of §1.** Add explicit language connecting §5(a)'s justification/witness format to spec:225's opaque-solver ban. Any linear-arithmetic strategy (§1a or §1b) must ship **with** a legible, independently-checkable Farkas-style structured certificate (a vector of non-negative multipliers), or it violates spec:225 (*Principle 3: opaque solvers rejected "even when they could prove more"*). Reframe §5(a) from "build-ordering ergonomics / nice to have first" to **"constitutionally required precondition for §1 being spec-legal at all."** Note that a Farkas certificate is independently checkable (unlike an SMT trace), so the criterion is achievable — but flag that whether this criterion *satisfies* the opaque-solver ban is an owner ratification, not a settled fact.

4. **Soften the opener's "settled" framing.** Where the "What this document decides" opener says the prove-or-reject target "is settled (`ProofEngine.cs:432`); this document does not re-litigate it," change "settled" to **"assumed-settled for scoping purposes."** Keep the `ProofEngine.cs:432` citation but note it is a *code comment* evidencing implementation intent, not owner ratification (the front-matter's "Nothing owner-ratified" already concedes this). Add a one-line contingency: **the entire roadmap is downstream of ratifying the disposition — if the disposition softens, the MVP staging is void.**

5. **Add the Phase-5-sequencing caveat to §1's "179 sites" headline.** Note that the 179-site figure overstates §1's *current* bite: most of those sites (the running-total-into-`nonnegative`-field cases) "compile clean today… because no obligation is generated (Phase 5 work)." §1's true *today* exposure is the narrower set of bounded min/max fields with guard-then-apply; the rest only begin rejecting once Phase 5 lands obligation generation. Make the coupling explicit so §1 isn't over-prioritized against items that bite immediately.

6. **Preserve what the document already handled correctly — do not weaken it.**
   - §7 (`.sum`/`.average`) correctly defers to the language-design process and sketches no syntax — keep it exactly as deferred; do not let a revision harden it into a proposal. Add only a forward note that §7 is the gating dependency for Phase 6 and must enter the design process on its own timeline if aggregates are ever wanted.
   - §6 (rules-with-arithmetic, proof-reading existing syntax, no new construct) and the document's avoidance of the spec:223 bridge-construct question should stay untouched — do not introduce a bridge construct.
   - The honesty grading and adversary-corrected §7 aggregate hand-fix claim are accurate — do not re-open or re-grade them.

7. **General tone correction.** Everywhere the document's staging language implies a locked-spec question has been resolved "as part of staging," rewrite to surface the question as open and owner-gated. The revision improves the document's *self-awareness about what it is pre-deciding* — it does not change any verdict, corpus figure, or citation.
