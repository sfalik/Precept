---
title: "Frank's Charter-Gate Review — Overnight Design Run + v3 Plan"
status: Draft — 2026-07-12 (Frank's architect review, applying his own charter gates; nothing ratified)
author: Frank (Lead/Architect & Language Designer)
owner: Shane
reviews:
  - docs/Working/overnight-design-run-2026-07-12-SUMMARY.md
  - docs/Working/certificate-steps-membership-2026-07-12.md
  - docs/Working/slice-design-money-2026-07-12.md
  - docs/Working/slice-design-constant-rule-2026-07-12.md
  - docs/Working/slice-design-case-by-case-2026-07-12.md
  - docs/Working/slice-design-witness-2026-07-12.md
  - docs/Working/slice-design-single-fact-2026-07-12.md
  - docs/Working/slice-design-structural-severity-2026-07-12.md
  - docs/Working/compiler-readiness-plan-2026-07-12.md
context: >
  Follow-on to Frank's prior citation/process-verification review
  (frank-review-readiness-plan-v3-2026-07-12.md, gates cleared). This pass is
  explicitly NOT a mechanical audit — Shane asked Frank to apply his own
  standing charter gates (Design Gate, Implementation Plan Gate vs the
  CONTRIBUTING.md quality bar / his PR #108 exemplar, Catalog-Driven
  Enforcement, Philosophy Filter, AI-First Design, per-decision rationale
  audit) to the 9-doc "overnight design run" plus the modified v3 plan, and
  to give his own first-principles opinion on all 15 ranked owner-gated
  open questions and a bottom-line, gate-ranked greenlight verdict.
note: >
  This is Frank's raw, verbatim architect assessment — read-only analysis,
  no files edited by Frank. Per his own standing discipline, this is his
  read, not a ratification; every flagged gate and ruling-lean remains
  Shane's to confirm or redirect.
---

# Frank — architect review of the overnight design run (2026-07-12)

Shane — this is my own opinion pass, my gates, my voice. I'm not re-checking citations; I spot-checked only where a verdict hung on it. Where the work is right I'll say so without hedging, because most of it is right.

## Top line first

**This is the best design work this project has produced.** The four-leg rationale is *above* my bar — half these decisions carry Reversibility, Blast-radius, and Strongest-counter-evidence legs I never asked for. The ⊥-rail ownership analysis is exactly the reasoning I'd have demanded and would have rejected anything less than. The fail-open inventory growing from one hole to four-plus-Hole-5 by live adversarial probe is the intellectual honesty I want and rarely get. Nobody papered over a shape with nullable fields; nobody smuggled an operator into the parser; nobody manufactured a catalog violation. **Say it plainly: this cleared my Design Gate and my Implementation-Plan Gate on rigor.**

But it is **not** greenlightable to code exactly as the plan-of-record reads, because the *plan* (`compiler-readiness-plan-2026-07-12.md`) carries one backwards sequencing sentence that poisons the very first slice, and the biggest slice (§1a) has an owner-gate sitting on the Definition-of-Done critical path that the ruling log doesn't resolve. Fix those two and rule a short list of one-word defaults, and coding starts.

Gate-ranked verdict is at the bottom. First, my gates.

---

## Gate 1 — Design Gate (Runtime / Tooling / MCP) across the six slices

**PASS on all six — and this is the part I want to praise, because it's the trap I block PRs for.** A design that fixes runtime and forgets tooling/MCP is blocked on my desk even when tests pass. None of these six does that.

The reason it works is *architecturally* correct, not accidental: the one breaking tooling/DTO reshape — `ProofVerdict` DU + certificate projection into `CompileToolDtos`, `RichHoverFactory`, `precept_proofs`, `mcp.md`/`language-server.md` — is concentrated **once** in Slice 0. Every later slice then correctly reasons "no structural tooling change, my verdicts/intervals ride the Slice-0 projection" and *says so explicitly* rather than going silent:

- **§3 money** — computedInterval projection to MCP/hover called out; the PRE0078-message-stays-range-less honesty is stated (the message re-render rides Slice 0, not this slice). Correct and honest.
- **§2 case-by-case** — an actual "Cross-component propagation" block: Runtime None, LS None-structural (richer certs through Slice-0 hover), MCP None-structural (CaseSplit arms through the Slice-0 projection), doc-update names mcp.md/language-server.md for *verification*. Correct.
- **§1a** — MCP: no new DTO, conditional on OQ1; doc-sync enumerated. Correct.
- **§4a witness** — tooling propagation (hover, precept_proofs, MCP DTOs) is *in scope*. Correct.
- **§6 constant-rule** — Decision 7 pins the certificate mapping; doc-update enumerated. Correct.
- **structural-severity** — nails it: severity maps per-*value* not per-*code* (`DiagnosticProjector`/`DiagnosticEnricher`), so the flip propagates with **zero** tooling edits. That's the right observation and it's cited.

I will not manufacture a concern here for balance. The gate is satisfied. Good.

## Gate 2 — Implementation-Plan Gate vs my PR-#108 bar

The plan has the file inventory tables (per-slice "Inventory of what will be built"), the ordering constraints, the per-slice failing-test matrices, the ⊥/single-hop/staleness invariant cells as mandatory rows, the doc-update obligations enumerated per slice, and a tooling/MCP sync assessment per slice. On structure it **meets the bar.**

Two things it does *differently* from #108, and I approve both: (a) it deliberately writes line-pinned edit-plans at *slice start*, not now, because Slice 0 reshapes `BuildNarrowedIntervals`/the verdict model that §2/§6 edit — pinning line numbers today would be stale by execution. That's correct discipline, not a gap. (b) It expands Slice-0 and §3 heavyweight and stubs the rest with the "expand N+1" rule. Fine.

**Where it falls short of my bar — one real defect, one omission:**

- **DEFECT (P0): the sequencing sentence is backwards, and it's load-bearing on the *first* slice.** §7 says structural-severity "can land any time after Slice 0's dead-end=Error dependency is available" and "slots where convenient." But **dead-end=Error is *delivered by* the structural-severity slice.** And Slice 0 step 3 / Hole 4 relabels dead-end→dead-end rows from false-`Proved` to `Unresolved`, claiming outcome-neutrality "now that `DeadEndState` = Error … already rejects the compile." That claim is **false until structural-severity has landed.** As written, an agent builds Slice 0 first (it's the hard prerequisite), flips those rows to `Unresolved` while `DeadEndState` is still `Warning`, and *changes corpus compile outcomes the plan swears are neutral*. The structural-severity slice doc itself states the constraint in the *correct* direction and proposes the de-garble — but the plan-of-record still contains the backwards sentence, and agents build from the plan. **This must be fixed before any coding.** Cleanest fix: fold the `DeadEndState` Warning→Error flip (at minimum; ideally the whole process-topology family) **into Slice 0**, so the outcome-neutrality is true by construction. Second choice: make structural-severity a hard predecessor of Slice 0's Hole-4 action. "Slots where convenient" is simply wrong for this one code.

- **OMISSION (P0 for §1a only): the Definition of Done is not self-executing.** DoD #1 requires "all six capabilities green via full-compile," including §1a. But §1a's Decision 8 correctly states **no `Proven` mints through the new path until the certificate step-kind (OQ1) is ruled** — and OQ1 is *not* in the plan's ruling log; it's an open owner gate on new public vocabulary. So the plan's own DoD depends on a ruling the plan doesn't carry. That's an embedded owner-gate on the critical path to "MVP done." It's honestly surfaced in the slice doc, but the plan should state it at the DoD: **§1a's accepting half is owner-gated on OQ1; its engine work proceeds in parallel; DoD #1 cannot close for §1a until OQ1 is ruled.**

## Gate 3 — MVP completeness (does executing this produce a working prove-or-reject compiler?)

I traced the chain against the ratified ledger scope (§1a/§2/§3/§4a/§5a/§6 + certificate + structural-severity, §1b deferred, aggregates/compute-then-check out).

**It composes into a working MVP — with two named seams:**

1. **§1a's accepting half is blocked on OQ1** (above). Everything else composes to green independently.
2. **§4a value-witnesses degrade to `Unresolved` on arg/element leaves until §2 lands** (F8). This is handled correctly as a *graceful* dependency — the gate keeps it sound (an un-point-evaluable corner fails validation → Unresolved, never false ProvenViolating). Not a hard ordering. Good.

Everything else — Presence/KeyPresence config-witness deferred, the Modifier 13th grid cell conservative, §6's hook-context decline — composes because each defaults to the conservative common denominator (Proven/Unresolved, no over-claim). No ratified-scope capability is left un-built by any slice. **The exit criteria compose into "MVP done" — except that DoD #1 has the §1a/OQ1 gate on it.** Surface that and the composition is honest.

## Gate 4 — Catalog-driven enforcement

**PASS, and I looked hard because this is where I reject work.**

- **CertificateSteps catalog mechanism is catalog-first.** Decision C1 rules the source is a spec-enumerated `CertificateSteps` catalog, membership owner-gated — that's *exactly* catalog-before-code: rule the member first, then emit. Correct.
- **Decision 5 is the one I most wanted to see and they got it right:** one `IntervalArithmetic` step kind that *cites the owning Operations/Functions catalog member's transfer* — **not** per-operator step kinds. Per-operator kinds would be a parallel copy of the Operations catalog inside CertificateSteps — the exact "derive, don't duplicate" violation I reject. They rejected it for that reason. That's §3 money getting **zero** new vocabulary for free. Right.
- **§1a's LinearTermFact + matcher are pipeline-internal inference records, not catalog metadata** — correctly argued: they encode no per-member domain knowledge (operator semantics stay in Operations; transfers stay on `BinaryOperationMeta`). The *one* catalog item — the step kind for the match — is correctly parked as owner-gated. No violation.
- **§2 reuses `CaseSplit`, adds no kind.** The site-condition premise question is parked, not hardcoded. Correct.
- **The one honest flag:** the string-function length caps still dispatch on a `FunctionKind` switch in `Lengths.cs:250-291` instead of a `LengthTransfer` on `FunctionOverload` — a real catalog-placement gap. It is **surfaced, not perpetuated**, and correctly deferred (it's pre-existing, doesn't block Slice 0, and the certificate meta states the transfer normatively so the certificate stays honest even while dispatch hasn't moved). That's the right call — flag it, don't let this work step in it, close it in a future slice.

No catalog offense. Good.

## Gate 5 — Philosophy Filter

Ran it explicitly against the set:

- **Domain integrity, not deferred enforcement** — this is the whole point; prevention strength never *decreases* in any slice (every slice widens only what is *provably* safe, and each ⊥ rail exists to stop the new power from opening a false-Proven). PASS.
- **Deterministic / inspectable** — the certificate *is* inspectability's sharpest form; every step is a bounded recompute, no search, no fixpoint (spec §0.4 honored per slice). The witness executes concretely before it's shown. PASS.
- **Keyword-anchored flat statements preserved** — no slice touches authored syntax; all changes are compiler-*output* vocabulary. PASS.
- **First-match routing / collect-all** — untouched; §2's per-arm-then-union is exact case analysis, not a widened join. PASS.
- **AI legibility of the new certificate/witness/verdict shapes** — see Gate 6; strong.
- **Reads like configuration, increases power without hiding behavior** — the certificate/witness make *more* behavior visible, not less. PASS.

No philosophy gap that I resolve myself. One thing I'll flag *to you* per my charter, not resolve: the certificate/verdict work makes the engine's approximation (interval over-approximation, sign-set holes) **visible in public output** for the first time. That's philosophy-*aligned* (approximation honesty, Principle 8) — but it's a new surface where the product now *shows its seams* to authors and AI agents. I don't think it needs a philosophy edit; I think it's the philosophy working as designed. Noting it because "the core guarantee's inspectability surface changed shape" is on my flag-list, and I'd rather you hear it from me than discover it.

## Gate 6 — AI-first

**PASS, and it's designed AI-first, not retrofitted.** `ProofVerdict` DU, span-cited premises, recomputable steps, `precept_proofs` rendering the closed vocabulary from `CertificateSteps.All` (**never** a parallel list — Decision 5 again), the witness DU rendering structured bindings in authored units. The MCP DTO reshape is a *budgeted breaking change* owned in Slice 0, which is the correct treatment of a public-API break. The certificate renders one author-readable line per step *and* stays machine-replayable — that's structured-output-first with human-readability as the co-equal, exactly the balance I want. Nothing here is designed human-first with the AI surface bolted on.

## Gate 7 — Scope, priorities, my own engineering judgment

**Sequencing is sound engineering except the one backwards sentence.** Slice 0 first (biggest breaking reshape + soundness fixes — risk front-loaded). §3 next (mechanical, high value — the *only* case an author can't hand-fix). §2 before §4a (§4a's witness needs §2's arg narrowing — correct dependency order). §1a last (biggest greenfield, owner-gated — don't front-load the greenfield build, resolve its gate in parallel). That's how I'd order it. The **only** ordering error is structural-severity's placement (P0, above).

**Scope creep check: clean.** §1b is deferred and the spec:256 override is *not* authorized — and I verified no slice sneaks it back. §1a explicitly keeps single-hop, refuses per-field splitting, refuses algebraic rearrangement (Extension A parked). The "moved-left internal form" is argued to be within the ruled ceiling, and **I agree with that argument on the merits** — it's the single fixed identity `L⊕R ⟺ L−R⊕0`, already public in the certificate vocab (`RelationImplication`) and in spec:256's own justification; no constant crosses the comparison. That is not "the engine does algebra sometimes." No creep.

**Corners that will bite — minor, name them:** the decimal corner-overflow `OverflowException` in `NumericInterval` (money OQ2) is a live crash in a method §3 *touches*. Leaving a known crash in code you're editing is sloppy. Fold the saturate-to-Unbounded guard into the §3 kernel pass — the slice is already in that file. Don't route it to bugs.md and walk past it.

## Gate 8 — Per-decision rationale audit

**I went looking for a WHAT-without-WHY to send back. I didn't find one.** Every Decision I read carries all four legs, most carry three more. This is the standard I nag the team about, met without me asking. The witness Decision 4 (disposition map on `ProofRequirementMeta`, never a kind-switch), §6 Decision 8 (fold-order pin with the suppression-asymmetry argument), §1a Decision 5 (moved-left within-ceiling) — these are model rationale blocks. Nothing to send back for rationale.

---

## Gate 9 — my own ruling-lean on the 15 ranked open questions

You asked for my opinion, first-principles, the way I gave §1b. Here it is. **B = real blocker; D = ship-with-default, confirm at boundary; T = genuine Tier-2/3 conversation before you can rule.**

1. **§1a certificate step (both seams)** — **B + T.** Real blocker for §1a's accepting half; genuine new public vocabulary → Tier-3. My lean: **option (b), one new step kind serving both seams** (21→22, the single budgeted growth). Two payload widenings (a) soften two currently-crisp recompute contracts and smuggle the multiset-equality inference into each — I don't like blunting `RelationImplication`'s two-field contract. One clean new kind is more honest. But this is yours; have the conversation.
2. **§1a spec:256 reading** — **D, not T.** My firm opinion: this is a **wording widening, not a shape-restricting lock.** Multi-term single-fact chases no third field — it's single-hop, depth-bound untouched, and spec item 2 (:207) *already mandates* multi-field relational reasoning; today's impl only covers the single-pair subset. Widening fulfills the mandate. **Not** a Tier-3 override. One-word confirm and it lands at the correction step. The design read it right and correctly didn't self-authorize.
3. **§1a Extensions A/B + moved-left confirmation** — **D.** Ship the conservative floor; **defer both extensions** pending Falsifier-1 corpus evidence. Confirm the in-ceiling reading of the internal moved-left form — I already agree with it (#2 reasoning). No reopening.
4. **§2 Strategy-8 / Hole-5** — **B-shaped but already caught.** The canonical soundness claim at `proof-engine.md:1707` is *wrong* and there's a live false-`Proven` — that's the one genuine outrage in the pile, but it's a *shipped-code* outrage the adversarial pass **found and correctly routed to Slice 0's fail-open sweep** as the same shape as Holes 1–4. Approve the doc correction. Ownership is not ambiguous: **Slice 0 fixes it** (it's a fail-open, Slice 0 owns the sweep), **§2 inherits the corrected losslessness precondition.** Not a greenlight blocker — it's a Slice-0 work item already on the board.
5. **§2 arm-assumption evidence form** — **D, mild-T.** My lean: **(a) site-context payload, no premise** — the conditional's condition is a subexpression of the obligation's site, which is already shared context; a one-sentence Decision-7 revision. Owner-gated because it touches settled text, but low-stakes. §2's engine work isn't blocked; the accepting certificate needs the ruling.
6. **§4a two extensions** — **D.** (i) **Accept** the ProvenViolating linkage amendment for qualifier/dimension (zero vocab growth, more honest); Unresolved fallback if you decline — sound either way. (ii) Two-candidate corner is fine (still gated by mandatory validation); single corner if you'd rather — sound. Confirm at boundary.
7. **structural-severity sequencing** — **B (the P0).** Apply the de-garble; fold `DeadEndState`=Error into Slice 0 (my preference) or hard-pin structural-severity before Slice 0's dead-end relabel. Approve the companion doc corrections (graph-analyzer OQ1 override, spec:186/189, diagnostic-system:191 caveat retirement) as proposed.
8. **Slice-0 type-implied modifiers** — **D → rule "widen."** Widen `CatalogFact`'s recompute rule with the Types-catalog third arm. The engine *already* folds `field.ImpliedModifiers` (`Composition.cs:674`); a certificate that can't cite that evidence is incomplete and falsifies its own "no premise outside the eleven" claim. The design already wrote the third arm. Widen, don't park.
9. **§6 hook-context ordering canon** — **D + T.** Ship the **conservative decline** (sound) and route the shipped sign path's hook behavior to the §2.2 sweep to confirm it's not a live fail-open — option (a). Whether hook reads see committed pre-state is a real runtime-*semantics-in-canon* decision (Tier-2/3), but it's **post-MVP** — don't block on establishing the ordering guarantee now.
10. **§2 ⊥-arm sentence** — **D.** Confirm **(A)** (outer environment, tightest unconditionally-sound). One word.
11. **§3 money wire-set boundary** — **D.** My call: **×/÷ now** (the plan title, the unfixable-by-hand case); **+/− and same-space Negate as a fast-follow**; **Duration/Period negation + ExchangeRate wait on magnitude-space verification.** All parked cells are precision-only (unwired ⇒ conservative reject), so any answer is sound. Don't let this stall §3.
12. **§4a Presence/KeyPresence config-witness** — **D.** Confirm **deferred.**
13. **§4a Modifier 13th grid cell** — **D → rule (i) NotApplicable**, mirroring the qualifier row. Built behavior is conservative until you rule, so no rework risk.
14. **§6 qualified-rule residual posture** — **D + T.** For the MVP, **(a) semantics-only** — document the failing-compile-only residual and the sign/interval-fold divergence. Option (c) (sequence obligations first, thread the blocked set) is the strongest and sets the pattern for every future magnitude consumer of trusted facts — but it rides Slice-0's reshape and is bigger; take it post-MVP. The residual only exists *inside an already-rejecting compile*, so (a) is honest enough now.
15. **Remaining parks** — **D, batch it.** One I care about: fold the decimal-overflow saturate-guard into §3's kernel pass (don't leave a live crash in a touched method). `samples/Test.precept` disposition is yours. Everything else is safe under any answer.

---

## Gate 10 — Bottom line, gate-ranked

**Verdict: greenlight coding on Slice 0 → §3 → §2 → §4a → §6 → structural-severity, after two P0 plan corrections and a short batch of one-word/default rulings. §1a's engine work can start in parallel but §1a cannot be finished — cannot go green for DoD — until you rule OQ1.** This does **not** need another full design pass. The designs are sound; the gaps are in the *plan's sequencing and a handful of owner rulings*, not in the engineering.

Ranked by how load-bearing:

- **P0-1 — Fix the structural-severity/dead-end=Error ordering** (Gate 2 defect, OQ7). Fold `DeadEndState`=Error into Slice 0, or hard-pin structural-severity ahead of Slice 0's Hole-4 relabel. Without this, the *first slice built* silently changes corpus outcomes it calls neutral. **Non-negotiable, do it before any code.**
- **P0-2 — Rule OQ1 (§1a certificate step) before §1a completes** and state the §1a-accepting/OQ1 gate at the plan's DoD. My lean: one new step kind (b). New public vocabulary → have the Tier-3 conversation. Everything else can start while this is pending.
- **P1 — Rule the four low-stakes unblockers now, my defaults given:** OQ2 spec:256 (confirm the widening read), OQ8 type-implied modifiers (widen CatalogFact), OQ5 arm-assumption evidence (site-context), OQ4 (approve the Strategy-8 doc correction; Hole-5 → Slice 0 sweep). These clear the accepting-half certificate paths cleanly.
- **P2 — Ship-with-default, confirm at slice boundary:** OQ3, 6, 10, 11, 12, 13, 15. All sound under any answer.
- **P2 — Real but non-MVP-blocking canon items:** OQ9 (hook-ordering) and OQ14 (magnitude-consumption posture) — set the conservative default now, take the canon decision post-MVP.

And the part I don't often get to write: **on rigor, this design run is right, and I said so at the top for a reason.** The four-leg discipline, the ⊥-rail ownership reasoning, the honest fail-open inventory, the catalog discipline in Decision 5, the concentration of the tooling break into one slice — that's the standard. Hold the next passes to it.

— Frank
