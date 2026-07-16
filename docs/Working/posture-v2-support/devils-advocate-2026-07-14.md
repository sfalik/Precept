---
title: "Devil's Advocate Review — Frank posture replay v2 support"
date: 2026-07-14
author: Fact Checker (Devil's Advocate mode)
status: Working — input to posture rewrite only
scope: Documentation-only review; no source-code inspection
primary_artifact: docs/Working/frank-go-forward-posture-replay-2026-07-14.md
---

# Devil's Advocate Review — openings left by the current posture replay

This pass does **not** argue that the posture is wrong. It argues that, in several places, the current wording still permits a smart reader to assemble a *different* reading from the same source set. Wherever that alternate read is still plausible, the v2 rewrite should close it explicitly.

## 1. Guarantee scope vs. the overflow carve-out

### Claim under test
The posture says the absolute guarantee holds across the whole **live-enforced** surface, with representational overflow as the one disclosed carve-out (`frank-go-forward-posture-replay-2026-07-14.md` §2).

### Plausible alternative reading from the same sources
A reader can still argue that the corpus does **not** cleanly support a "live guarantee except overflow" formulation, because the source set itself preserves two simultaneously true statements:
1. the boundary ruling says representational overflow is **parked** and "no doc may claim it prevented" (`proof-engine-boundary-ruling-2026-07-06.md` §1, §7 R6), **and**
2. the owner later ruled to **leave the present-tense overflow claims in canon as-is** because they state the intended target, not a text defect (`proof-engine-decision-ledger-2026-07-12.md` item #7).

That supports an adversarial reading that the guarantee language is still intentionally target-level, not purely live-surface descriptive.

### Evidence supporting the posture's reading
- Boundary ruling Rev. 3 explicitly carves `NumericOverflow` out of the live fault surface and calls it a disclosed known gap, not currently prove-or-reject.
- The same ruling keeps declared-bound containment (`OutOfRange`) live while separating it from representational overflow.
- The ledger leaves `GATE-O` open, which supports "not built yet" rather than "already covered."

### Evidence supporting the alternative reading
- `docs/philosophy.md` still says arithmetic overflow is a compile-time impossibility.
- `docs/language/precept-language-spec.md` Principle 10 / 11 still say overflow is prevented/proven.
- Ledger #7 says those sentences stay because they describe the intended end-state; the ruling is explicitly **not** "the text is wrong today, fix it now."
- `critique-c3-philosophy-reconciliation.md` flags the omission directly: the draft tempering program fixed `philosophy.md:49`, but left `:53`'s overflow-prevention overclaim unresolved.

### Does the current posture foreclose the alternative?
**No.** It discloses the carve-out, but it does **not** explicitly name the source-level tension between:
- "no doc may claim it prevented," and
- the later owner ruling to preserve present-tense canonical overflow claims.

So a reader can still cite canon and say, "the official texts still count overflow inside the guarantee; Frank is narrowing them in this posture doc on his own."

### Gap to close in v2
Say plainly that the source corpus contains a **deliberate owner-held tension**: live posture excludes representational overflow today, while canon keeps target-language for it. If v2 does not name that tension as deliberate, it stays available as re-litigation fuel.

## 2. The "TWO-WAY provenance-only routing axis"

### Claim under test
The posture says there is one discriminating question — provenance — and that routing is cleanly **two-way**, not three-way (`frank-go-forward-posture-replay-2026-07-14.md` §5).

### Plausible alternative reading from the same sources
A reader can instead say the corpus supports an **obligation-role** reading, not a pure provenance reading:
- the boundary ruling's core device is the **Obligation-Role Rule** (`proof-engine-boundary-ruling-2026-07-06.md` §4), where disposition belongs to the *obligation family*, not the construct;
- the Fable adversarial analysis repeatedly frames the seam as **proof-carrying vs non-proof-carrying** and leans on the sweep-governs-result distinction for derived-result constraints (`fable-analysis-of-frank-response-2026-07-11.md` Claims 2–4);
- the runtime/spec corpus itself distinguishes **ingress**, **sweep**, and **out-of-contract/trap** semantics (`precept-language-spec.md` §0.7, §3A.4; `runtime-api.md` Three-Layer Enforcement Model).

Under that reading, "provenance" is at best a compression of a more exact rule, not the rule itself.

### Evidence supporting the posture's reading
- Boundary ruling executive summary: raw external value at ingress is governed; derived value is proven-or-rejected.
- Ledger #1 fold-in: "raw input is governed at the door; anything computed from it is proven."
- The posture is right to reject **surface spelling** and **decidability** as routers.

### Evidence supporting the alternative reading
- Boundary ruling §4 explicitly says disposition is a property of an **obligation**, not a construct.
- The relational-invariant bucket is not routed by operand provenance alone; it is routed by *being a declared relationship with no derived-value fault obligation to discharge*.
- `fable-analysis-of-frank-response-2026-07-11.md` gives a documented alternate question set: proof-carrying vs non-proof-carrying, ingress vs sweep, typed outcome vs fault trap.
- `frank-canonical-capture-recommendation-2026-07-14.md` itself still uses the phrase "proof-carrying vs. not" as a boundary explanation, even while rejecting band-vs-rule.

### Does the current posture foreclose the alternative?
**Only partially.** It bans spelling-based and decidability-based routing, but it does **not** explicitly name and kill these other rival framings:
- proof-carrying vs non-proof-carrying,
- obligation-role vs provenance,
- "two mechanisms under one govern side" vs "three operational routes."

Because those rivals are not named, a reader can still claim the posture's one-question rule is too compressed to be source-faithful.

### Gap to close in v2
Either:
1. restate the axis as **obligation-role, with provenance as the practical shorthand**, or
2. keep the provenance slogan but explicitly reject the proof-carrying/non-proof-carrying and three-route restatements by name.

This is the single biggest re-litigation opening in the current doc.

## 3. §1b as DEFERRED, not REJECTED

### Claim under test
The posture says §1b is deferred/open, not rejected (`frank-go-forward-posture-replay-2026-07-14.md` §3, §6).

### Plausible alternative reading from the same sources
A reader can plausibly say the ledger treats §1b as **functionally closed for the current roadmap**, even if not metaphysically rejected:
- option C is chosen,
- it is "not in the MVP, not scheduled,"
- the `spec:256` override is **not authorized**, and
- a fairly strict revisit trigger is imposed (`proof-engine-decision-ledger-2026-07-12.md` item #3).

That creates room for the adversarial gloss: "open in theory, rejected in practice."

### Evidence supporting the posture's reading
- The ledger says **OPEN** and provides a revisit trigger rather than a permanent no.
- The feasibility doc calls §1b a legitimate candidate second phase on first principles, not a design mistake.
- The retrospective treats the "never an expressibility gain" result as a *governor on sequencing*, not a proof that §1b is conceptually invalid.

### Evidence supporting the alternative reading
- The same ledger item says "build nothing that requires" the `spec:256` override.
- The honest fix is said to be **§6**, now inside the MVP.
- The revisit trigger is narrow enough that a hostile reader can paraphrase it as: "don't expect this unless repeated real pain appears later."

### Does the current posture foreclose the alternative?
**Not fully.** It bans the word **rejected**, but it does not sharply distinguish:
- **open in the design space**, from
- **inactive in the authorized build plan**.

Because that distinction is not spelled out, someone can still translate the plan state into a practical rejection and claim the posture is softening that reality.

### Gap to close in v2
State both halves at once:
- §1b is **open in principle**, and
- §1b is **closed for the MVP/current authorization set** unless the revisit trigger fires.

That removes the easy "you're relabeling rejection as defer" attack.

## 4. The D1 / GATE-O framing as "build-order, not text defect"

### Claim under test
The posture treats the ledger's "build-order, not a text defect" line as the settled frame for overflow (`frank-go-forward-posture-replay-2026-07-14.md` §2).

### Plausible alternative reading from the same sources
A reader can say: **that is an owner policy choice, not a corpus-consensus interpretation**. The source set contains substantial material arguing that the overflow sentences really are false *as present-tense implementation claims*:
- boundary ruling §7 R6 recommends surfacing/reconciling the philosophy/spec overflow statements,
- the thesis's supporting critique flags the same overclaim,
- the ruling digest carries the philosophy-alignment tension as unresolved.

So the adversarial reading is: "Frank is presenting a disputed textual interpretation as if the whole corpus converged on it, when what actually converged was only the owner's leave-text-as-target ruling."

### Evidence supporting the posture's reading
- Ledger #7 is explicit and owner-ratified.
- Canonical-capture analysis says the overflow present-tense stance is ruled tension, not a new drift item.

### Evidence supporting the alternative reading
- Boundary ruling Rev. 3 does **not** say the text is clean; it says the gap must be surfaced to the owner.
- `critique-c3-philosophy-reconciliation.md` treats the omission as a real documentation inconsistency.
- The issue is repeatedly described in supporting docs as a **tension**, not as a resolved semantic reading.

### Does the current posture foreclose the alternative?
**No.** It quotes the owner's ruling, but it does not admit that several source documents reached the opposite textual conclusion before the owner overrode the edit recommendation.

### Gap to close in v2
Say explicitly: "the sources do **not** unanimously agree that the text is semantically clean; the owner chose to preserve target-language anyway." Without that sentence, the posture overstates how settled this framing is.

## 5. The two runtime mechanisms (ingress governance + post-mutation sweep)

### Claim under test
The posture says runtime governance is delivered by **two distinct mechanisms**: ingress governance and the post-mutation sweep (`frank-go-forward-posture-replay-2026-07-14.md` §4).

### Plausible alternative reading from the same sources
A reader can argue that the docs describe a third runtime-relevant zone that the posture omits: **restored / out-of-contract state**.

The spec is explicit that restored state is neither ingress nor sweep at load time; it is trusted on hydration and only re-governed on the next operation, with evaluator fault traps as backstop for out-of-contract data (`precept-language-spec.md` §0.7; §3A.4). `runtime-api.md` likewise exposes a third layer: **defense-in-depth evaluator faults**.

So an adversary can say the posture's two-mechanism story is only complete for **contract-path mutation governance**, not for the full runtime behavior surface.

### Evidence supporting the posture's reading
- For in-contract mutation surfaces, the spec really does define two enforcement points: ingress and sweep.
- `Fire` and `Update` pipelines both funnel through those surfaces.
- `ConstraintsFailed` at Stage 9–10 is the documented whole-entity enforcement point.

### Evidence supporting the alternative reading
- `precept-language-spec.md` §0.7: restored state is "trusted as valid at persistence time" and is **neither** ingress nor sweep at load.
- The same section says host-injected/bypassed data is outside the contract envelope and can still hit runtime traps.
- `runtime-api.md` Three-Layer Enforcement Model explicitly includes evaluator faults as a third runtime layer.
- `result-types.md` also contains typed runtime outcomes (`Rejected`, `Unmatched`, `InvalidArgs`) that are runtime behavior, but not either of the two governance mechanisms.

### Does the current posture foreclose the alternative?
**No.** It does not scope the claim narrowly enough. It says runtime governance exists for what proof cannot reach, but it never adds the crucial limiting phrase: **"within contract-path mutation semantics."**

### Gap to close in v2
Scope the two-mechanism claim explicitly to **contract-path governance of mutations**. Then separately name restored/out-of-contract data and evaluator traps as outside the governed contract path, not as a hidden third governance mechanism.

## Banned-framings gaps: retrospective error-shapes that §6 does not currently ban

The retrospective names several recurring failure shapes that the posture's §6 list does **not** explicitly lock out:

1. **Turning an engineering wall into a product-identity crisis.**
   - Retrospective §4 says the June 2c-ii amendment already contained the engineering diagnosis, but the team set it down and reopened "what should Precept be" instead.
   - §6 bans some wrong conclusions, but not this meta-error: *when a slice gets hard, do not re-litigate product identity before doing the engineering diagnosis.*

2. **Reasserting prove-or-reject as a slogan over the wrong engine.**
   - Retrospective §2 is explicit: prove-or-reject is honest only **as the ratified package**, not over June's/today's weaker engine.
   - §6 bans "prove everything decidable, full stop," but does **not** ban the subtler framing: "the posture was always right, the engine details are incidental."

3. **Reviving the D2 band-split through a new vocabulary.**
   - Retrospective §1.2 identifies the 06-16 band split as the nadir.
   - §6 bans routing by **surface spelling**, but it does **not** explicitly ban reintroducing the same split as "non-proof-carrying policy bands are different" or similar language.

4. **Flattening the history into "a circle" or "we always knew this."**
   - Retrospective §2 / §4 insists this was a spiral that deposited genuinely new material (certificate criterion, §1b non-expressibility result, §6-as-MVP escape valve).
   - Posture §7 gestures at this, but §6 does **not** ban the rhetorical move that erases those deposits and reopens the debate by pretending nothing materially changed.

5. **Skipping the engineering-diagnosis artifact when the next hard wall appears.**
   - Retrospective §4 gives a concrete countermeasure: the first move at the next wall should be a one-page engineering diagnosis of what is undecidable, what is merely unimplemented, and what one authored rule would discharge.
   - §6 does not currently ban the old reflex that produced the detour.

## Prioritized openings to close in the v2 rewrite

1. **Highest — tighten the routing rule.**
   The current "one provenance question" slogan leaves too much room for a rival obligation-role / proof-carrying reading built from the same sources.

2. **High — make the overflow tension explicit as deliberate, not invisible.**
   V2 should say the corpus preserves a live carve-out **and** target-language canon by owner choice. If that duality stays implicit, overflow will keep getting re-imported into the live guarantee.

3. **High — distinguish owner policy from source consensus on the text-defect question.**
   "Build-order, not text defect" is ruled, but not unanimously derived. V2 should say so.

4. **Medium — state §1b's status in two dimensions at once.**
   Open in principle; closed for MVP/current authorization absent revisit trigger.

5. **Medium — scope the two-mechanism runtime claim to contract-path mutations.**
   Otherwise restored/out-of-contract runtime behavior remains an easy hole-punch.

## Bottom line

- **Major claims tested:** 5
- **Claims with a genuine unclosed opening:** 5
- **Single most important opening:** the §5 routing formulation — "one provenance question, two-way only" is still compressing a source set that can plausibly be reassembled into an obligation-role / proof-carrying alternative unless v2 names and closes that rival reading directly.
