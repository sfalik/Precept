# Matched-pair decomposition changelog — `compiler-readiness-plan-2026-06-16.md`

**status:** Surgical pass — 2026-06-23
**scope:** Decouple the agent-introduced "matched-pair rule" from the plan. Apply the owner-established discriminator ("would the compiler STILL reject this once the runtime is FULLY built?") to every matched-pair site. Everything outside matched-pair handling (the boundary, D1–D4, the rigour, Frank B1/A1/A3, the coverage ledger, calibration except the matched-pair line, the red-team except Stress 4) is preserved.

## The discriminator applied

- **GENUINE_FAULT_A** — the operation is inherently unprovable-fault-safe; a fully-built runtime cannot rescue it → KEEP the rejection, REFRAME as permanent prove-or-reject, STRIKE the "until the runtime lands" / "matched-pair" scaffold framing.
- **RUNTIME_ABSENCE_B** — a fully-built runtime performs the operation safely, so the compiler would ADMIT it; it only rejects now because the runtime is absent → ADMIT now, DEFER the enforcement, rely on the § 11 capture (the same admit-now / defer-enforcement treatment D2/D3 bands get). REMOVE the over-rejection.

**Anti-smuggle held:** runtime-absence is not fault-unprovability. The "prevention posture: unprovable ⇒ reject" banner is preserved ONLY where the rejection is genuine fault-safety (2c-ii compiler half, element write-site proof, un-walked faulting guard, intra-chain stale discharge, provably-unsatisfiable cross-transition ensure).

## Counts

- **GENUINE_FAULT_A (reframed as permanent prove-or-reject; rejection kept):** 2 distinct cases — 2c-ii field-reference bound (Slice 5.1); un-walked faulting guard (Slice 2.5). Carve-outs held as (a) inside (b) cases: element write-site proof (Slice 5.3), intra-chain stale discharge (Slice 3.1), provably-unsatisfiable cross-transition ensure (Slice 5.5).
- **RUNTIME_ABSENCE_B (admit-now / defer-enforcement; over-rejection removed or mislabel struck):** 8 substantive cases — D8 dynamic-write conversion; BUG-030 value tail; BIZ-9..13 derived-value tails; reclassified-band governance (already (b); relabel only); committed-state tier-2 discharge (the one genuine over-rejection at source); element-ingress governance; omit-clears/readonly patch; cross-transition ensure enforcement. Plus 2 non-cases de-labeled away from matched-pair: collect-all sweep (plain RUNTIME_CODE_DEFER); Restore DU removal (confirmed already clean — no edit).
- **§0 discipline 3:** rewritten (the central change). **§11 discipline 2:** rewritten in lockstep.
- Downstream consistency: phase exits (0,1,3,4,6,7), §11.A/D/E rows, coverage ledger recaps, calibration MEDIUM bullet, red-team Stress 4 + summary, DoD-1/7/8.

## The §0 discipline-3 rewrite (line 22)

**Before:** "**The matched-pair rule.** The compiler must not *admit* … a write the stubbed runtime cannot perform … Where admission depends on a deferred runtime capability (e.g. D8 dynamic-write …), the compiler **continues to reject** until the runtime conversion's capture lands. Prevention-engine posture: unprovable-sound ⇒ reject, never skip."

**After:** "**Prove-or-reject (permanent) and admit-now / defer-enforcement (transient).** The compiler rejects what it cannot prove **fault-safe** — period … runtime-absence is **not** fault-unprovability … Separately, where an operation **is** fault-safe once the runtime performs it (reclassified-band governance — the canonical model; D8 …; committed-state tier-2 discharge), the compiler **admits** it now and the runtime **enforcement** is deferred and captured in § 11 … The compiler does **not** keep rejecting merely because the runtime is unbuilt …" — strikes the D8-continues-to-reject clause; strikes the unprovable-sound branding from the runtime-absence half; names bands as the canonical (b) model; dissolves the "matched-pair rule" name.

## Site-by-site

| Line(s) | Case / id | a/b | Before (gist) | After (gist) |
|---|---|---|---|---|
| 22 | §0 discipline 3 (C2) | b (blanket) | "matched-pair rule … continues to reject until the runtime lands; unprovable-sound ⇒ reject" | split into permanent prove-or-reject + admit-now/defer-enforcement; D8/committed/bands named as (b); name dissolved |
| 218 | Phase 0 exit | b | "matched-pair rule preserved … admissions gated on runtime-canon capture landing first" | admit-now/defer-enforcement; no admission gated on the runtime being built; no rejection held |
| 317 | Phase 1 exit | b | "matched-pair rule holds: compiler admits nothing the stubbed runtime cannot perform" | admit-now/defer-enforcement; band lane compiles clean, enforcement deferred with pointer |
| 404 | Slice 2.5 guard (A1-flagged) | a | "Matched-pair preservation … sound-by-refusal" | "Prove-or-reject (permanent)" — un-walked faulting guard rejects forever; genuine fault-unprovability |
| 424 | Phase 3 boundary discipline (C4) | mixed | "The matched-pair rule … keeps rejecting / stays sound-by-refusal until the capture lands" | genuine prove-or-reject (intra-chain) vs admit-now/defer-enforcement (committed-state, BUG-030 value) |
| 437 | Slice 3.1 committed-state half (C4 / Q1-blocking) | b | "HARD scope boundary (matched-pair) … conservative-refusal … it rejects (sound-by-refusal)" | committed-state discharge ADMITTED relying on captured prior-commit enforcement; enforcement defers; no over-rejection; intra-chain stays genuine prove-or-reject |
| 446 | Slice 3.1 doc-touch | b | "in-scope compiler counterpart is this slice's conservative-refusal logic" | "admitted compiler-side now; the prior-commit enforcement is the deferred half" |
| 526 | Phase 3 exit | b | "no committed-state discharge claimed (sound-by-refusal); matched-pair rule holds: no admission outruns a deferred runtime capability" | committed-state ADMITTED, enforcement deferred (band treatment); slices reject only what they can't prove fault-safe |
| 532 | Phase 4 goal | b | "deferred under the matched-pair rule" | deferred as admit-now/defer-enforcement |
| 534 | Phase 4 boundary | b | "derive the type and either admit-when-statically-provable or keep rejecting (matched-pair rule, §0)" | derive the type and admit the fault-safe operation, deferring enforcement; genuine unprovable-safe ops rejected (permanent) |
| 684 | Slice 4.8 derivation rows (BUG-030/BIZ-9..13) | b | (pre-applied) "admit-now-defer-capture, not a held rejection" | unchanged — already correct admit-now framing; divisor obligations preserved via "the obligation discharges" |
| 707/716/718/762 | Slice 4.9 + Phase 4 exit (D8) | b | (pre-applied) admit-now-defer; "no over-rejection held" | unchanged — already reframed in the prior DONE edits |
| 797 | Slice 5.1 2c-ii (A1-flagged) | a | "Matched-pair posture … Where the compiler cannot prove the bound holds, it rejects (unprovable ⇒ reject)" | "Compiler prove-or-reject (permanent); runtime ingress is a separate deferred feature" — unprovable bound rejects forever; ingress is a deferred runtime feature, not a hold |
| 815 | Slice 5.1 doc-touch | a | "This is the matched-pair runtime tail" | "the deferred runtime-ingress feature … compiler's prove-or-reject is permanent and independent of it" |
| 853 | Slice 5.3 boundary note (EI-1) | b (+a carve-out) | "Boundary note (matched-pair) … the runtime matched pair with the shipped Rule-4 read-site reach" | element write-site proof = permanent prove-or-reject (ships here); element-ingress governance = deferred feature; compiler ADMITS the Rule-4 read-site reach, enforcement deferred |
| 869 | Slice 5.3 doc-touch (EI-1, EI-2) | b | "deferred element-ingress + collect-all … Rule-4/Rule-2 matched-pair soundness note" | split: element-ingress = read-site-discharge / ingress-enforcement note (admit-now); collect-all = plain RUNTIME_CODE_DEFER (no admission gated on it) |
| 908 | Slice 5.5 cross-transition (least contaminated) | b (+a carve-out) | CANON_DESIGN fork for provably-unsatisfiable; runtime enforcement "RUNTIME_CODE_DEFER regardless" | added: provably-unsatisfiable = genuine prove-or-reject; satisfiable-ensure runtime enforcement = admit-now/defer-enforcement (band treatment), not a compiler over-rejection |
| 939 | Slice 5.6 omit-clears/readonly (EI) | b | "Boundary note (matched-pair) … enforcement needs the evaluator" | "Boundary note (admit-now / defer-enforcement — § 11 capture)" — compiler admits, carries tables; enforcement deferred (band treatment); no rejection held |
| 999 | Slice 5 placement ledger | b | "The runtime matched-pair tails (… element-ingress governance, collect-all sweep, …)" | deferred runtime features (admit-now/defer-enforcement) + collect-all as plain RUNTIME_CODE_DEFER; compiler holds no over-rejection |
| 1105 | Phase 6 exit | b | "the matched-pair rule untouched" | "no admit-now / defer-enforcement obligation introduced" |
| 1127 | Slice 7.2 conformance authoring | b | "Matched-pair discipline … compiler facet asserts the continued rejection" | "Admit-now / defer-enforcement discipline … compiler facet asserts the admission; Skip'd runtime case asserts the deferred enforcement" |
| 1128 | Slice 7.2 exit | b | "compiler-rejection case paired with a Skip'd runtime-admission case" | "compiler-admission case paired with a Skip'd runtime-enforcement case" |
| 1179 | Phase 7 exit | b | "matched-pair rule holds (every compiler over-rejection has its Skip'd runtime-admission partner …)" | admit-now/defer-enforcement holds (Skip'd runtime-enforcement case = the deferred enforcement, not a flipped rejection) |
| 1192 | §11 discipline 2 (Q1/Disc-2 blocking) | b (multi-case) | "The matched-pair rule … the compiler keeps rejecting until the capture lands" (D8, element-ingress reach, committed-state) | rewritten once: admit-now/defer-enforcement for all (b); genuine prove-or-reject named separately and not a §11 matter |
| 1206 | §11.A reclassified-band row (C5) | b | "Matched pair / in-scope counterpart" | "In-scope compiler counterpart (admit-now / defer-enforcement — the canonical model) … not withholding a rejection pending the runtime" |
| 1207 | §11.A committed-state row (C4) | b | "In-scope compiler counterpart (matched pair): … keeps the compiler sound-by-refusal … rejects, never discharges" | committed-state ADMITTED relying on captured prior-commit enforcement; no over-rejection held; pre-release no live fault (D3) |
| 1209 | §11.A element-ingress row (C6) | b | "Rule-4/Rule-2 matched-pair soundness note" | "Rule-4/Rule-2 admit-now soundness-pairing note … not a held compiler over-rejection" |
| 1275 | §11.D proof-engine bullet | b | "the in-scope compiler counterpart is Phase 3 Slice 3.1's sound-by-refusal logic" | admits relying on captured enforcement; enforcement is the deferred half |
| 1297 | §11.E item 4 | b | "the compiler stays sound-by-refusal until this holds" | admits now relying on captured enforcement; enforcement is the deferred half |
| 1299 | §11.E item 6 | b | "the matched pair of the shipped Rule-4 read-site reach" | "the deferred-enforcement partner of the shipped (admitted) Rule-4 read-site reach" |
| 1318 | Coverage de-dup note | b | "that is the matched-pair split" | "compiler-half-now / enforcement-deferred split" |
| 1393 | Coverage table (business-domain) | b | "D8 dynamic-write admission BLOCKED matched-pair → §11" | "D8 dynamic-write admitted compile-side, conversion enforcement → §11" |
| 1467 | Register-H committed-state recap | b | "the in-scope counterpart is the Phase-3 Slice-3.1 conservative refusal" | "admitted compiler-side … admit-now / defer-enforcement" |
| 1470 | Register-H element-ingress recap | b | "Rule-4/Rule-2 matched-pair note" | "admit-now soundness-pairing note (compiler admits the read-site reach; ingress enforcement deferred)" |
| 1488 | Reconciliation totals | b | "the rest phased or matched-pair split to §11" | "compiler-half-placed with enforcement deferred to §11" |
| 1508 | Calibration band-claim | b | "the deferred matched-pair partner" | "the deferred enforcement partner" |
| 1517 | Calibration MEDIUM bullet (C8) | b | "The matched-pair rule keeps the compiler sound … continues to reject until the capture lands — unprovable-sound ⇒ reject" | "Admit-now / defer-enforcement keeps the compiler honest … admits now, enforcement captured in § 11; does not keep rejecting because the runtime is unbuilt" |
| 1532 | Calibration summary | b | "the matched-pair refusals … are MEDIUM" | "the admit-now / defer-enforcement captures … are MEDIUM" |
| 1538 | Red-team question 4 framing | b | "a matched-pair leak that would smuggle a runtime-execution exit criterion" | "does any retained slice assert a runtime-EXECUTION exit criterion, and is every admitted op's enforcement captured (no silent loss)" |
| 1603/1605 | Stress 4 heading + intro (C7) | b | "matched-pair leak hunt … admits a write the stub cannot perform (matched-pair violation → guaranteed fault)" | "no-silent-loss hunt … admitting a fault-safe op whose enforcement defers is the correct pre-release behavior, not a leak" |
| 1607 | Stress 4 band bullet | b | "matched-pair satisfied in the safe direction" | "admit-now model: compiler admits the fault-safe operation" |
| 1609→1610 | Stress 4 D8 bullet (C7) | b | "The plan does NOT admit the dynamic-write case … continues over-rejecting it until the capture lands … reject-when-unprovable direction" | D8 dynamic-write is fault-safe; compiler ADMITS, enforcement defers (band treatment); verify the § 11 pointer (no silent loss) |
| 1613 | Stress 4 tier-2 bullet | b | "in-scope compiler counterpart is conservative refusal … the compiler rejects" | intra-chain = permanent prove-or-reject; committed-state ADMITTED, enforcement deferred |
| 1623 | Stress 4 verdict | b | "no matched-pair leak … holds the boundary in the reject-when-unprovable direction" | "no runtime-execution exit criterion, no silent loss … admits a fault-safe op with enforcement deferred under a pointer" |
| 1634 | Red-team summary item 4 | b | "No matched-pair leak … reject-when-unprovable direction" | "No runtime-execution exit criterion, no silent loss … admit-now / defer-enforcement" |
| 1658 | DoD-1 boundary carve-out | b | "The compiler stays sound-by-refusal … (the matched-pair rule: it keeps rejecting what the stubbed runtime cannot make sound)" | committed-state ADMITTED relying on captured prior-commit invariant; enforcement deferred (band treatment); intra-chain prove-or-reject ships |
| 1704/1705 | DoD-7 | b | "the matched pair for the BLOCKED compile-side dynamic-write admission" | "the deferred enforcement of the admitted compile-side dynamic-write case; admit-now / defer-enforcement" |
| 1711/1712 | DoD-8 | b | "the matched-pair rule holds … every compiler over-rejection has its Skip'd runtime-admission partner authored, not flipped" | admit-now / defer-enforcement split; Skip'd runtime-ENFORCEMENT case asserts the deferred enforcement; genuine prove-or-reject needs no runtime partner |
| §11.B 1227 / 1256 / 1476 (D8 recap) | C3 | b | (pre-applied) "admitted compile-side; enforcement deferred" | unchanged — already reframed in prior DONE edits |
| §11.B 1224 / DoD recap (Restore, C1) | non-case | b | already clean — pure API-surface DU removal, no matched-pair framing | confirmed clean; no edit |

## Anti-smuggle / adversarial corrections honored

- **Committed-state (Q1-blocking):** the source over-rejection at Slice 3.1 (lines 424/437) was the one genuine lazy-retention site; it is now admit-now/defer-enforcement at SOURCE, matching the D8 treatment — both (b) cases now in the identical state.
- **2c-ii + element write-site (do-not-over-correct):** kept as GENUINE_FAULT_A; only the mislabel and the §11-discipline-2 "keeps rejecting" listing were struck.
- **Divisor obligations (BIZ-9..13 / BUG-030):** the plan text retains "the obligation discharges" — the compile-time DivisionByZero/sqrt obligations on division-bearing derivations stay permanent prove-or-reject; only the value-application defers. No "no fault-safety reason to reject" assertion was introduced.
- **D1 conditioning (D8):** the fault-safety claim stays conditioned on the PARKED representability genus (lines 707, 1227, 1610 cite "the only overflow genus is the PARKED representability family, D1, never a rejection ground").
- **Discipline 2 (line 1192) single coordinated rewrite:** rewritten once, not piecemeal — no half-edited "keeps rejecting" list survives.

## Not touched (preserved)

The boundary (compiler code + canon design IN, runtime coding OUT); D1–D4; the rigour; Frank B1/A1/A3 fixes; the coverage ledger structure and totals; the BUG-030 Slice 3.3 interval-math cancellation-factor fix (permanent soundness repair); calibration except the matched-pair line; the red-team except Stress 4 (and the question-4 framing it feeds).
