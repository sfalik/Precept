# Deep review — compiler-readiness-plan-2026-06-16.md

**Status:** Review report — 2026-07-01
**Reviewed:** `docs/Working/compiler-readiness-plan-2026-06-16.md` (plus companions: design-revision-notes, frank-review, matched-pair-changelog)
**Method:** 51-agent workflow — six review lenses (exit criteria/gates, settled-decision application, boundary sweep, coverage-ledger reconciliation, ground-truth citation checks against HEAD source, internal cross-references) plus a precept-reviewer process pass. Every finding was adversarially verified by a skeptic whose refutation grounds included "citation doesn't match" and "this reopens a settled decision." 43 raw findings → 32 survived → deduplicated below to 1 blocker, 11 concerns, 13 nits. 11 findings refuted.
**Verdict:** Remediation required — but nothing found challenges the plan's decisions, architecture, or sequencing. D1–D4, the boundary ruling, and the Slice 3.1 scope boundary are consistently applied in the plan body. Everything below is bookkeeping, fixable in one editing pass.

---

## Blocker

**B1. The companion capture list still says "reject" where the plan says "accept."** (`compiler-readiness-plan-2026-06-16-design-revision-notes.md:63`)
The design-revision-notes companion holds the master to-do list for writing the runtime documentation, and the plan requires that list to be applied before any Phase-1+ work starts (plan:1195). One entry still carries the deleted matched-pair framing: it says the compiler must keep rejecting the dynamic-write unit-conversion case (D8) *"until this lands."* The owner decided the opposite on 2026-06-23: the compiler accepts the case now and the runtime's enforcement is recorded as a deferred obligation (plan:1228 says exactly this). The word "admit-now" never appears in the companion. An agent executing from the companion as written would record the outdated contract into `evaluator.md`. Fix: rewrite the entry to the accept-now/defer-enforcement contract.

## Concerns

**Conversations promised but never scheduled:**

**C1. The Phase-3 discharge-shape conversation (GATE-B) has no Phase-0 slot.** (plan:444)
Slice 3.1 says its owner conversation about which of two implementation shapes to use is "settled in Phase 0," but GATE-B appears only inside Phase 3 (lines 431/444/446/520) — it is missing from the Phase-0 gate roster (plan:34), Slice 0.2's gate definitions, the Phase-0 exit checklist (plan:214), and the final sign-off list (plan:1727). Phase 0 can be declared complete without the conversation happening; the slice then silently uses a default the owner never picked. (The superseded 06-11 plan did include GATE-B in its Phase-0 walk — it was dropped in revision.)

**C2. The three Phase-0 gate rosters disagree.** (plan:34 vs plan:214 vs plan:1727)
The summary table lists gates I and M but not L; the Phase-0 exit lists L but not I or M; the final sign-off list ("Every Phase-0 gate") lists neither L nor M. An auditor working from the sign-off list would never verify GATE-L's disposition or GATE-M's placement. Fix: one roster, referenced from all three sites.

**C3. Phases 4–5 contain ~6 owner decisions with no scheduled time or venue.** (plan:732, also plan:726, plan:741, plan:986)
The D16 function-table fork (add overloads vs strike the row), unit-of-measure atomic-only, angle-unit, bare-number bounds, identity notempty, and the Slice 5.7 choice element-type fork are all written as "owner picks" mid-slice, with no gate name. Exit criteria do require a recorded ruling before anything lands, so nothing ships silently — but the D16 answer even changes the amount of compiler code, landing in an already-in-flight slice (4.8). Fix: give these forks named gate slots (or a batch conversation at phase start).

**A counting error:**

**C4. "Floor count 11 → 10" is wrong; it should be 9.** (plan:194; repeated at plan:206 and plan:1729)
D1 parks two of the eleven prevent-or-reject fault families — decimal representability overflow (family 9) AND temporal representability overflow (family 10); the research doc numbers them separately. Removing two of eleven leaves nine. The wrong count would be propagated into the fault-floor research doc that grounds every later slice.

**Undispositioned and unaccounted items:**

**C5. Slice 4.9's new fault codes contradict the sign-off contract, with no stated disposition.** (plan:708)
The slice captures new "integer-conversion overflow" and non-finite (∞/NaN) fault codes into `fault-system.md`. DoD-9's contract (plan:1720) says the evaluator can fault after a clean compile ONLY on the parked decimal/temporal representability family. The new codes are neither inside that park, nor covered by any compile-time obligation, nor explicitly parked. The plan must pick one of the three.

**C6. The sign-set element-band gap is parked outside the accounting ledger.** (plan:1000)
It is parked in Phase 5's placement note but has no row in the ledger's parked register (Register G, plan:1441), no §11 pointer, and is absent from the DoD parked list (plan:1749) — a parked item living outside the one table whose purpose is proving nothing was dropped silently.

**C7. The mincount Create-time refusal deferral is missing from the §11 handoff.** (plan:1336)
The ledger row claims "+ §11 (Create-time refusal, pointer `evaluator.md §7.1 Create`)," but no §11 row (11.A–11.E), no companion §B row, and no Slice 7.7 handoff entry names it. The runtime initiative would inherit a handoff list missing this obligation.

**C8. The sample-corpus work routes to a Phase-7 slice that doesn't exist.** (plan:1423)
The ledger sends "4 unauthored samples + zero-use re-verification" to Phase 7, but no Phase-7 slice mentions samples; DoD-5 (plan:1688) cites "a Phase 7 corpus slice, delegated to precept-author" that was never written.

**C9. "Q4" labels two different open questions.** (plan:1421)
The open-bug bucket's Q4 (vacuous/mutual bound warning policy, from dynamic-modifier-bounds research) and the open-decision bucket's Q4 (Principle-11 defensive-redundancy scoping, from contract-clarity) are different items; the 06-16 plan dropped the source citations that disambiguated them, so the "43/43 placed" claim can't be audited.

**Citations that don't match the code (verified against HEAD):**

**C10. The third diagnostic-detection pattern doesn't exist in the scanner.** (plan:1023; `DiagnosticCoverageScanner.cs`; `diagnostic-system.md:531`)
Slice 6.1 tells the executor to count diagnostic coverage using three detection patterns "per DiagnosticCoverageScanner.cs" — literal creations, catalog-mediated fields, and diagnostics chosen dynamically at runtime. The scanner (all 226 lines) implements only the first two (as Patterns 1, 2, 2b); it has no capability to detect dynamically-chosen diagnostics. The canonical doc `diagnostic-system.md:531` makes the same false claim about the scanner — canonical drift, fix in the same pass. This matters: a count taken with the two real patterns silently misses exactly the dynamic-dispatch emission sites.

**C11. Two capture pointers name a runtime doc that doesn't exist.** (plan:815, plan:924)
Both name `docs/runtime/proof-engine.md`; the proof-engine doc lives at `docs/compiler/proof-engine.md` (docs/runtime/ contains only descriptor-types, evaluator, fault-system, precept-builder, README, result-types, runtime-api). The plan's honesty discipline hangs on capture pointers naming real sections; the plan's own §11 table uses the correct unprefixed form for the same links.

## Nits

- N1. Phase 5 is the only phase with no phase-level exit block; it ends at the coverage note (plan:998). Template consistency only — the boundary re-assertion exists in-phase and Phase 6's dependencies already gate on Phase 5 outputs.
- N2. Slice 7.4 claims the descriptor builder-population deferral is "already enumerated in §11"; no §11 row names builder population (plan:1145).
- N3. The frank-review companion (2026-06-21) still endorses the deleted matched-pair rule with no supersession banner (frank-review:75, :128). Low stakes — nothing directs execution from it — but banner it.
- N4. "Slice 0.5" at plan:114 should be Slice 0.4 (the exit criterion at plan:217 has it right).
- N5. "Phase 6 Slice 3c" at plan:778/793/1011 is a leftover label from the superseded Phase-8 emission plan; the analyzer is Slice 6.4.
- N6. Slice 0.1's probe (b) is said to feed GATE-D, but GATE-D (plan:138) is the PRE0101 duplicate-key question; probe (b)'s own text routes elsewhere (plan:126/130).
- N7. Citation drift set: `ProofEngine.Analysis.cs:467` is the element-LENGTH collector, not the count collector (plan:556); the "stale regardless of source" sentence is paraphrase presented as quotation (plan:435); `result-types.md:63` wording differs from the quote (plan:167); the "~25 teachable messages" count isn't supported by the cited range `temporal-type-system.md:1352-1360` (plan:650); spec lines 256/258 are §0.6 tail paragraphs, not §0.7 (plan:313); `StaticallyPreventableMap.cs:28-37` vs `:28-36` — one is off by a line (plan:1510 vs plan:267/280/1544).

## Refuted findings (11 — the anti-loop mechanism working)

Three would-be blockers died under adversarial checking: a claimed silent drop of the Point-A residue and a claimed mis-route of the PRE0164/PRE0115 identity reframe both turned out to be resolved by plan text the finder hadn't read; a claimed sign-modifier contradiction misread D2's carries-proof carve-out. Also refuted: "D4 lacks four-leg rationale" (D4 is a fix-fold, not a decision), a claimed Phase-4 gate mislabel, a claimed §11 discipline violation in Slice 7.5, and five others. Full refutation text in the raw output (path below).

## What this review did NOT cover

1. **Buildability of the designs themselves.** All lenses reviewed the plan as a document. Nobody checked the flagship mechanisms (creation-exhaustiveness checker 2.1–2.3, write-aware discharge 3.1, band reclassification + registry repartition 1.3–1.4, field-reference bound enforcement 5.1, ownership analyzer 6.4) against the real proof-engine internals. Suggested check: one proof-engine-literate pass over those slices, per-slice verdict "buildable as specified" vs "needs a design pass first."
2. **Upstream completeness of the ledger's inputs.** The ledger was verified downstream only (~20 of 313 rows traced to destination slices). Nobody re-opened the 619-row spec audit, the 32-doc triage, bugs.md, or the fault-floor research doc to confirm every upstream item became a register row. An item dropped before the ledger was built survives every check that ran.
3. **Red-team stresses 1–3 were taken on the plan's word.** Stress 4 (no exit criterion needs the unbuilt runtime to execute) was independently re-verified and held, including the `Evaluator.cs` stubs. The single-hard-ordering claim and the coordinated-move-safety argument were not independently re-derived.

Process caveat: the ground-truth citation lens and four of its verifiers ran without the harness safety-classifier review (unavailable at the time). Their findings (C10, C11, N7) are direct quote-vs-file mismatches — cheap to spot-check by opening the cited lines.

Raw machine-readable results (every finding, citation, and verdict): `/tmp/claude-1000/-home-sfalik-source-repos-Precept/22c2b8cb-4360-49a0-86ae-7cd6fae87f0c/tasks/wz7dl6q1f.output` (workflow `wf_64ebe7af-319`; per-agent journal in the session's `subagents/workflows/wf_64ebe7af-319/journal.jsonl`).

## Recommended disposition

One remediation pass over the plan + companions fixing B1 and C1–C11 (N1–N7 ride along), combined with the terminology/readability rewrite so the plan is edited exactly once. The one open pre-sign-off question is not-covered item 1 (buildability of the flagship slices).
