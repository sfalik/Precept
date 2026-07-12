# Reconstruction: what "over-proving" meant and whether pulling back from it was sound

**status:** Cited
**external-engagement:** purely-internal — decision-archaeology of the project's own history (no outside sources engaged; all evidence is this repo's docs, spec, code, and git history)
**topic:** Reconstruct and adjudicate the "over-proving / over-rejection / over-stepping the fault floor" pullback — the D2 "band split" that moved the band-containment lane (OutOfRange / LengthBound / CountBound, PRE0078) from prove-or-reject toward flag/warning, and the "fault floor (prove-or-reject) vs everything-beyond (flag-sound, revisited after runtime)" boundary. Were the documented reasons sound and do they still hold?
**authored:** 2026-07-06
**method:** synthesis of three independent investigators (`compile-time-niche`, `fault-floor-SB`, `plan-and-session`) and their three adversarial verifiers, plus first-party re-verification of the load-bearing spec/code/git citations.

---

## Background

The owner suspected the proof engine had *over-stepped its fault floor* — rejecting definitions at compile time that it should not. The suspicion crystallised into a decision on the compiler-readiness plan, "D2 band split," which reclassified the band-containment lane (the declared min/max, length, and count checks, carried by fault codes `OutOfRange` / `LengthBound` / `CountBound` and the `PRE0078` set-action check) from *fault / prove-or-reject* down toward a *compile-time flag/warning*, and drew a boundary: the **fault floor stays prove-or-reject**; **everything beyond it ships "flag-sound," to be revisited after the runtime exists.**

Two years of design context sit behind this: a 20-agent compile-time-niche decision packet, a 10-agent fault-floor research pass, the readiness plan itself, and — most recently — two owner-commissioned 2026-07-03 analyses (`band-guarantee-boundary-analysis`, `d2-proven-violation-philosophy-analysis`) plus an in-flight design pass (`prove-govern-classification-and-legibility`, dated 2026-07-06). That last session preserves the pullback's core but **reverses one sub-component**: it routes a *provably-always-violating* band assignment back to a compile-time **error** (as "definition incoherence") rather than the warning the pullback assigned.

This document reconstructs what "over-proving" actually meant, why the pullback happened, whether its documented reasons were sound and still hold, and whether the current session has drifted from — or faithfully completed — the grounded original.

---

## Methodology

Three investigators reconstructed the history independently from primary sources, each anchored on a different corpus (the niche packet; the fault-floor research; the readiness plan + current session). Each was paired with an adversarial verifier tasked to (a) confirm the citations hold verbatim and (b) find the strongest counter-case that would flip the verdict. This synthesis folds all six, then re-verifies the linchpin citations against committed `HEAD` first-hand rather than trusting the chain.

**First-party re-verification performed (all confirmed at `HEAD`):**

- `docs/language/precept-language-spec.md:208` (§0.6 item 6): "*An assignment expression provably outside the target field's constraint range is a compile-time error.*" — **unamended.**
- `precept-language-spec.md:266` (§0.7): fault prevention "*established entirely at compile time … no result outside a declared bound … never deferred to a runtime check … there is no deferral.*"
- `precept-language-spec.md:258`: the *unprovable* cardinality case is in that same no-deferral floor — "*an unprovable grow/shrink emits `CountBoundViolation` (PRE0136) … discharged by an author guard, never deferred to a runtime check.*"
- `src/Precept/Language/Diagnostics.cs`: `OutOfRange` = **Error** (:697); `UnsatisfiableGuard` / `VacuousRule` / `ContradictoryRule` = **Warning** (:770/:792/:802).
- Commit `2df5b37d`: author **and** committer `sfalik` (the owner), Claude co-authored; subject "*docs(readiness): compiler-only boundary revision*"; body "*Four owner decisions applied: overflow parked, band split, atomicity relaxed, Frank B1-B3 folded.*" The band split is **one line among four**, inside a docs-revision commit — no standalone decision record isolates the proven-case severity.
- `band-guarantee-boundary-analysis-2026-07-03.md` header: "*commissioned by the owner … D2 band split is NOT owner-ratified — this analysis is free to reframe or reject it.*"

All three verifiers agreed the investigators' quotes are accurate; the one verifier who marked `citations-hold=false` did so on **interpretation**, not quote-accuracy ("individual quotes are accurate … but the crux-bearing interpretive claim does not survive"). So the dispute in this record is never *what the sources say* — it is *what follows from them.*

---

## Findings

### 1. "Over-proving" was two distinct things the sources keep bundled

All three investigators independently converged on this, and it is the master key to the whole question:

- **Meaning 1 — genuine over-rejection of VALID precepts (a real soundness/friction defect).** The shipped band lane errored not only on *provable* violations but on *merely-unprovable* intervals: `PRE0078` rejected `[-inf..+inf]` (`fault-floor-definition-2026-06-11.md:176`), count errored "*even on the FIRST unguarded add into a maxcount-1 collection*" (:184), length errored on "*unbounded source into capped field, forcing bound-invention*" (:180). These are true false-rejections: soundness-over-completeness forbids rejecting what you cannot prove unsafe. The plan's own D2 rationale names exactly this: "*the band lane was over-reach — the compiler was rejecting merely-unprovable intervals, not just proven violations*" (`compiler-readiness-plan-2026-06-16.md:210`).

- **Meaning 2 — a CLASSIFICATION claim ("bands are not faults"), attaching to the PROVABLE case.** Under the fault-floor "can-it-compute?" test — "*a fault is a mid-evaluation abort with no recoverable typed outcome … anything the working-copy discard or ingress refusal handles (ConstraintsFailed) is governance*" (`fault-floor-definition:122`) — an out-of-band value "*computes, stores, compares; every runtime lane refuses recoverably … No can't-compute referent*" (:76-78). The `PRE0078` "exceeded the representable range" message is "*factually false in the band case*" (:74). This is a category/honesty argument. It does **not** say any valid precept is falsely rejected — a *proven* band violation is a **true** rejection of a genuinely-broken definition.

**These are different problems.** Meaning 1 attaches to the *unprovable* interval and is a soundness defect. Meaning 2 is a *label* dispute on the *provable* case. Neither, by itself, argues that a **provably-always-violating** band assignment must be a *warning* rather than an *error*. That distinction decides the entire dispute (§ "The decisive distinction," below).

### 2. The pullback bundled two logically independent moves

The D2 band split does two separable things (`compiler-readiness-plan-2026-06-16.md:207`):

- **Move 1 — merely-unprovable intervals compile clean.** This *is* the Meaning-1 over-rejection fix. **Uncontested.** Every investigator and verifier agrees it is correct and the current session keeps it.
- **Move 2 — proven violations emit a WARNING instead of an ERROR.** **Contested.** This is where the entire disagreement lives.

The whole question reduces to Move 2.

### 3. The evidence legs behind the pullback

**Strategic grounding (well-argued, not a hedge).** The compile-time-niche decision packet (20-agent, sealed/contamination-verified, with full advocate and red-team positions) resolved a *demonstrated* predictability failure — a reproduced `PRE0155` false positive where "*this correct precept would be rejected*" — by inverting the guarantee direction: "*The fault floor stays prove-or-reject (total, stable). Everything else in the niche ships under a FLAG-SOUNDNESS contract … an edit crossing the fragment boundary can only lose a warning (never gain a false rejection); engine growth only adds flags … Error promotion is then a per-diagnostic earned status (soundness verified), NOT a posture*" (`compile-time-niche-decision-packet-2026-06-10.md:117`). The oracle that *earns* error-promotion is the runtime, which is still a stub. This is a precise architectural answer to a live unsoundness, not a retreat.

**Fault-declassification (double-derived, red-teamed, HIGH confidence).** The band violation is a recoverable governance refusal (`ConstraintsFailed`), not an aborting fault. This is correct under Precept's own fault definition and is not disputed by anyone, including the current session.

**The scale-back recommendation.** The fault-floor research recommended "*Reclassify-to-flag (proven-violation warning) + defer-to-governance FOR NON-PROOF-CARRYING FIELDS ONLY*" (:177) — but **kept Error** for the provable *default* case, reclassified as a third category, "**definition incoherence** … neither fault floor nor flag layer … every Create fails" (:132, :189).

**Owner application.** Commit `2df5b37d` (owner-authored, owner-committed, Claude co-authored) applied "band split" as one of four owner decisions; the plan declares D2 "*settled … nothing may reopen them*" (:186). Frank review A6 supplied the "*prevention relocated, not weakened*" framing.

**The pullback KNEW it contradicted locked spec.** Both plan generations flagged that proven→warn required amending §0.6 item 6 and §0.7 ("*§0.6 item 6 reinterprets to proven-violation-only*," `fault-floor-definition:200-201`; the earlier plan's Frank B1 enumerates the sentences). That amendment was **owner-gated (GATE-F) and never executed.** Re-verified: §0.6 item 6 and §0.7 still mandate error / no-deferral at `HEAD`.

---

## The decisive distinction — and which side the evidence supports

The prompt asks whether the band lane was pulled back because it **genuinely over-rejected valid precepts**, or because of a **faults-vs-governance classification preference**. Resolved per-move, the evidence is clean:

**On the UNPROVABLE case → over-rejection. GROUNDED. STILL HOLDS.** Prove-or-reject on `[-inf..+inf]`, on the first unguarded add, on an unbounded source into a capped field, all falsely reject valid definitions. Fixing that is Move 1, and it is correct. The friction is quantified by a sibling proposal: reject-unbounded-source "*forces new max bounds on ~the majority of relational-rule sources, including open-ended money fields where a ceiling is unnatural*" — 214 unbounded money fields vs 10 (`bounds-only-constraint-enforcement-2026-06-05.md:120`).

**On the PROVABLE case → NOT over-rejection.** The session's own commissioned analysis states it verbatim: "*over-rejection is only reached by a checker that tries to prove runtime-value facts. By construction this checker refuses that; past the decidability edge there is nothing left to prove*" (`band-guarantee-boundary-analysis-2026-07-03.md:42`). A *proven* violation is fully decidable — rejecting it does not reintroduce the Meaning-1 defect. So Move 2 is a **severity/classification choice**, not a soundness fix. On that axis the evidence cuts **both ways** and does not resolve:

- **For Error (reject the proven case):** locked §0.6 item 6 (:208) still says "compile-time error," unamended; §0.6 item 11 already establishes the reject-proven/clean-unprovable shape; `OutOfRange`-on-default is `Error` at HEAD (`Diagnostics.cs:697`); the always-violation is "*fully decidable from the definition … the predicate is the decidable set*" (`band-guarantee:99`), placing it on the decidable-reject side of the boundary the analysis itself derived.
- **For Warning (the pullback's disposition):** the fault-floor research *explicitly recommended* proven→warning for non-proof-carrying fields (:177, :181); the governance-checkpoint view grounds it — a set-action flows through `Fire`, which *has* a runtime checkpoint a default lacks (`band-guarantee:108-114`); and Precept **already ships proven structural certainties as warnings** — `UnsatisfiableGuard`/`VacuousRule`/`ContradictoryRule` are `Warning` at HEAD (`Diagnostics.cs:770/792/802`), so a proven-band warning is consistent with shipped canon, not anomalous; and the flag-soundness contract stages error-promotion as *earned after the runtime oracle exists* — with the runtime a stub, Error-now is premature by that discipline.

**Both owner-commissioned 2026-07-03 analyses deliberately refused to resolve Move 2**, reserving it as an owner call: "*coherence-parity says reject, governance-checkpoint says warn-and-govern. This is the crux inside the crux, and the owner should decide it explicitly*" (`band-guarantee:114`); "*a genuine disagreement between two consistent readings*" the analyses "*deliberately did not resolve.*" That deliberate two-reading reservation **is** the finding: the proven-case severity is evidence-underdetermined and owner-reserved.

### The internal inconsistency in the pullback (real, and it cuts against Move 2)

The pullback kept **Error** for the provably-violating *default* ("definition incoherence," `fault-floor-definition:189`) while routing the provably-violating *set-action* — the same shape, differing only in position — to **Warning**. `d2-proven-violation-philosophy-analysis-2026-07-03.md` names this "Internal inconsistency #1." The governance-checkpoint view answers it (default has no downstream checkpoint; set-action does), but that answer is *one side of the unresolved crux*, not a settlement. Separately, the plan's own boundary note routes a provably-unsatisfiable ensure ("*every fire is statically guaranteed to fail*") to `/design` as "*genuine prove-or-reject territory: the definition is incoherent regardless of runtime*" (`compiler-readiness-plan-2026-06-16.md:1097`) — the **same shape** as the always-violating set-action, given the **opposite** disposition. The pullback is not internally coherent on the proven case.

---

## Threats to validity

- **Investigator corpus overlap.** All three anchored on the same ~8 documents; a source outside that set (see completeness critic) could shift the picture. Mitigated by first-party HEAD re-verification of every linchpin.
- **Self-referential authority.** Several load-bearing claims are drawn from the *current session's own* documents (`band-guarantee`, `d2-proven`, `prove-govern`). Where those documents concede against the session's own position (e.g. `band-guarantee:114`, `:199`; `band-guarantee:147` conceding the carries-proof cut "cannot" settle the severity), the concession is strong evidence *because* it is against interest. Where they assert *for* the session (e.g. the "NOT owner-ratified" header), they are treated as contestable framing, not fact.
- **The spec is mid-amendment.** Reading `HEAD` as "locked canon" is complicated by the fact that the pullback *intended* to amend the very sentences now cited as binding. The amendment was never run — so HEAD is authoritative *as text*, but its authority *as settled intent* is genuinely contested (see verdict).
- **Provenance is contested between two same-session documents.** `band-guarantee` header says the split is "NOT owner-ratified"; `d2-proven:99` calls it "owner-locked." They cannot both be simply true; the reconciling read is grain (see below).
- **The runtime does not exist.** "Relocation, not weakening" and "earned after the runtime oracle" both depend on a runtime governance sweep that is a stub (`Evaluator.cs` NotImplementedException). No one verified whether a *locked runtime doc* specifies `ConstraintsFailed`-on-band-violation; if it does not, the warning defers to nothing today.

---

## Verdict: the pullback was MIXED — and so is the session's handling of it

**The pullback's CORE was well-grounded and correct, and still holds. It is NOT drift.** The fault-declassification (HIGH confidence, undisputed), the Move-1 over-rejection fix (a real soundness defect, correctly fixed), and the fault-floor/flag-layer boundary (a precise answer to a demonstrated `PRE0155` false positive) are rigorous, double-derived, red-teamed, and owner-applied. **Any framing of the whole pullback as "drift" or "AI-slop" is wrong** and is refuted by commit `2df5b37d` (owner-authored and committed), the plan lock, and the 20- and 10-agent derivations.

**The pullback's ONE contested leg — proven case → warning — was under-deliberated and internally inconsistent, but it is NOT a settled overreach the session merely "corrects."** Its four-leg rationale justifies only the friction fix; the specific severity rode in on the scale-back; it contradicts unamended §0.6 item 6; and it is inconsistent with the default-case Error it kept. **Yet it has a live, principled defense** (research recommendation, governance-checkpoint, the shipped dead-row-warning parity, earned-promotion-after-runtime) that the owner's *own* commissioned analyses **explicitly declined to resolve, reserving it for the owner.** It is a genuine two-reading crux, not drift.

**Therefore neither pole the orchestrator has entertained is right:**

- ✗ "The pullback was drift; keep the proof logic wholesale." **Too strong** — it discards the well-grounded core and the live warning case, and mischaracterises an owner-authored decision.
- ✗ "The pullback was a fully-deliberated decision the session is now eroding." **Too strong** — the specific proven-case severity was never independently deliberated, contradicts unamended locked spec, and was self-inconsistent with the default case.
- ✓ **Mixed:** grounded core (kept), plus one under-deliberated, spec-contradicting, still-open owner-reserved severity leg.

**Where the owner and orchestrator are WRONG:** the working framing that the band split was "never owner-ratified / agent-authored, so it can be reversed as drift" **overstates the record**. The owner authored *and committed* the split; the reconciling read the evidence supports is **grain**: the owner ratified "band split" at a **coarse grain** (reclassify bands, defer enforcement — motivated by the real Meaning-1 over-rejection), but the **specific proven-definition-internal → warn severity** that contradicts §0.6 item 6 was never *separately* deliberated (no decision record, GATE-F never run, self-inconsistent with the default case, and later re-commissioned for review). "Coarse-grain ratified, fine-grain never settled" is the honest characterisation — not "never ratified."

---

## Has this session drifted?

**On the core: no. The session faithfully preserves the grounded pullback.** It keeps bands non-fault; keeps carries-proof (mincount discharge) in the prove-or-reject floor; keeps the flag layer's warning-default posture untouched; and keeps the Move-1 over-rejection fix — routing the runtime-dependent / merely-unprovable case to *govern*, "*no reject, no proven-violation warning*" (`prove-govern-classification-and-legibility-2026-07-06.md:139*`), with a no-false-reject falsifier and an over-approximating-reachability soundness requirement that "*only ever downgrades reject→advisory, never fabricates a rejection*" (:150). It does not touch the "value-governance ratchet" the owner feared; it stops that ratchet cold. On the core, the session *completes* an obligation the ratified plan itself created (:1097 routed the always-fails case to `/design`; this design is that pass).

**On the contested leg: the session reaches a defensible disposition, but its self-justification does not hold — and that is the real problem.** The session reverses proven→warning back to **Error** ("definition incoherence") and grounds it on guard-17: "*the provable case rejecting is implementation against locked spec, not a fresh choice*" (`prove-govern:216`). **That grounding is overstated, for two independent reasons the evidence establishes:**

1. **The "I'm just implementing locked spec" claim invokes a spec that is mid-amendment.** The pullback *itself* slated §0.6 item 6 for owner-gated softening to proven-violation-only; §0.6 item 6 reads "error" at HEAD **only because GATE-F never ran.** Wielding the unamended sentence against the very decision that meant to amend it is the **"decided-but-not-applied" anti-pattern** — re-deriving from the un-softened text because the softening's *application* hasn't landed. (This is precisely the pattern the owner's own standing feedback warns against: never re-open a made decision because its application hasn't landed.)

2. **The HEAD-authority yardstick is applied SELECTIVELY.** If unamended `HEAD` binds, it binds against **both** of the session's dispositions — not just the one it cites. §0.7 (:266) and §0.6 item 6 (:208) put the *provable* case in the no-deferral floor (→ the session's Error), **but line 258 puts the *unprovable* cardinality case in the *same* no-deferral floor** — "*never deferred to a runtime check*" — which the session's *govern-runtime-dependent* disposition **contradicts**. The session invokes HEAD to justify rejecting the provable case while its own treatment of the unprovable case departs from HEAD. You cannot have it both ways: either the spec is binding-as-written (then unprovable→govern is also a departure) or it is mid-amendment and underdetermined (then provable→reject is a *choice*, not mechanical implementation).

**Conclusion on drift.** The session has **not** drifted on the core and is **not** eroding a fully-settled decision. But it is **making an owner-reserved classification choice while presenting it as mechanical spec-compliance.** Two of the three adversarial verifiers independently reached this: the session-favoring "mixed" (session merely *corrects* an overreach) does **not** survive; the more defensible reading is that the proven-case severity is a genuine owner-domain fork the session is resolving *while claiming it isn't choosing*. The intellectually honest posture — which the session's *own neutral analyses* modeled and the session's *design rationale* abandoned — is: "*This is a two-reading crux the analyses reserved for you; here is why I lean Error; you own it, and the clean path is to run the §0.6-item-6 amendment (GATE-F) explicitly.*" The disposition (Error) is defensible; the **grounding** ("not a fresh choice") is not.

**Net, stated plainly:** the pullback's core was right and the session keeps it; the pullback's one contested leg is a still-open owner crux, not settled drift; the session's chosen resolution (reject the proven case) is reasonable but is dressed as spec-implementation when it is in fact an owner-domain call the spec — self-contradictory and mid-amendment at HEAD — cannot settle for it. Both the "pullback was slop, keep the proof logic" framing and the "session is eroding a locked decision" framing are wrong; the honest move is to **run the amendment decision explicitly** rather than let either the pullback or the session settle the proven-case severity under cover of authority neither cleanly holds.

---

## Completeness critic — what this reconstruction did NOT cover that could flip the verdict

1. **The commissioning transcript behind the July `band-guarantee` analysis (highest leverage).** The header asserts "*D2 band split is NOT owner-ratified … free to reject it*" and attributes the commission to the owner. Nobody read the actual owner directive. **If** a transcript shows the owner literally said the split isn't ratified and to feel free to reject it, the provenance sub-dispute collapses in the session's favor and the reversal becomes *executing explicit owner direction* — not "overstepping to settle a crux," and not the anti-pattern flagged above. **If** the header is agent self-authorization paraphrasing a narrower owner ask, the verdict stands. This single unread source could flip the "has the session drifted" section.

2. **Git-blame / authorship of §0.6 item 6 (:208) and §0.7 (:266) themselves.** The session's linchpin is that these are *locked owner canon* it merely implements. The owner's own AI-slop heuristic says: when impl and spec disagree, check git history; an AI-co-authored spec departure is *cleanup*, not "decision required." Nobody traced line 208's provenance. If "compile-time error" was itself a late Claude-authored insertion rather than original owner intent, its authority as the sentence-the-session-implements weakens sharply, and the session's guard-17 grounding loses its foundation entirely.

3. **The runtime governance spec.** "Relocation, not weakening" and "earned after the runtime oracle" both depend on a runtime sweep that refuses band violations recoverably (`ConstraintsFailed`). The `Evaluator` is a stub, and no one checked whether a *locked runtime doc* specifies that refusal. If it does, the warning posture (relocation is real-in-design) strengthens; if it does not, the warning defers to nothing and the reject posture strengthens. This directly weights the crux and was left unexamined.

4. **Adjacent D1 (overflow parked) and D3 (atomicity relaxed).** The reconstruction isolated bands. Overflow is a genuine *can't-compute* fault; if it was *also* pulled out of the floor, that would indicate over-correction beyond the fault-declassification rationale (and support the "pullback over-reached" reading more broadly); if it was kept in the floor, that corroborates a principled, consistently-applied boundary. Whether "pullback" was a principled boundary or ad hoc per-lane is testable here and was not tested.

---

## Sources

Primary (first-party re-verified at `HEAD` where marked ✓):
- `docs/language/precept-language-spec.md` — §0.6 item 6 (:208 ✓ "compile-time error"), item 11 (:213), §0.7 (:266 ✓ "no result outside a declared bound … no deferral"), :258 ✓ (unprovable cardinality "never deferred to a runtime check").
- `src/Precept/Language/Diagnostics.cs` ✓ — `OutOfRange`=Error (:697); `UnsatisfiableGuard`/`VacuousRule`/`ContradictoryRule`=Warning (:770/:792/:802).
- git `2df5b37d` ✓ — owner-authored+committed, Claude co-authored, "band split" one of "Four owner decisions applied."
- `research/architecture/compiler/fault-floor-definition-2026-06-11.md` — :15, :74, :76-86, :122, :132, :176-178, :180, :184, :189, :193, :200-201, :227.
- `docs/Working/compile-time-niche-decision-packet-2026-06-10.md` — :11, :117, :125, :171.
- `docs/Working/compiler-readiness-plan-2026-06-16.md` — :186, :207-213, :278 (GATE-H), :1097.
- `docs/Working/compiler-readiness-plan-2026-06-11.md` — :35, :98 (Frank B1 amendment enumeration).
- `docs/Working/band-guarantee-boundary-analysis-2026-07-03.md` ✓ (header) — :3, :23, :33, :42, :56, :99, :108-114, :136-140, :147, :199.
- `docs/Working/d2-proven-violation-philosophy-analysis-2026-07-03.md` — :47, :49, :67, :77, :79, :99, :103, §3b, §4.
- `docs/Working/prove-govern-classification-and-legibility-2026-07-06.md` — :19, :60, :139, :150, :208, :216, :250.
- `research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md` — :120.
