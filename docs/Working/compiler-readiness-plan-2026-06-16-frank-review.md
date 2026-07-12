---
status: Draft review — 2026-06-21
reviewer: Frank (Lead/Architect & Language Designer)
target: docs/Working/compiler-readiness-plan-2026-06-16.md
companion: docs/Working/compiler-readiness-plan-2026-06-16-design-revision-notes.md
rigour-bar: docs/Working/compiler-readiness-plan-2026-06-11.md (+ my prior review, B1–B3 / A1–A7)
grounding: research/architecture/compiler/fault-floor-definition-2026-06-11.md; docs/philosophy.md; docs/language/precept-language-spec.md (§0.6/§0.7/Principle 11/§3A.2)
live-source-checked: Evaluator.cs, FaultCode.cs, StaticallyPreventableMap.cs, ProofEngine.Diagnostics.cs, ProofEngine.cs, ProofLedger.cs, RestoreOutcome.cs, ProofEngine.Analysis.cs, Precept0002FaultCodeMustHaveStaticallyPreventable.cs, CatalogFormatters.cs, GrammarGen/Program.cs, evaluator.md, result-types.md, fault-system.md
---

> **SUPERSESSION NOTE (added 2026-07-01).** This review predates the **2026-06-23 matched-pair dissolution**. Its matched-pair endorsements — e.g. the boundary-integrity bullet "The matched-pair rule is sound, including the D8 dynamic-write case … The compiler *continues to reject* the dynamic-write dimension-narrowed conversion until `evaluator.md §Intake-Boundary` lands" and the Phase-4 line "D8 dynamic-write admission BLOCKED matched-pair" — are **superseded** by the plan's admit-now / defer-enforcement treatment: the compiler now **admits** the D8 dynamic-write conversion (and the committed-state tier-2 discharge), with the runtime enforcement deferred and captured in the plan's § 11; no compiler rejection is held pending the runtime. The rest of the review stands as written.

# Architectural Re-Review — Compiler-Readiness Plan (2026-06-16, compiler-only scope)

> Re-reviewed against the live source (not the plan's self-description), `docs/philosophy.md`, the spec (§0.6/§0.7/Principle 11/§3A.2), and the 06-11 plan as the rigour bar. Every load-bearing factual claim in the 06-16 plan that I could check against source, I checked. The verified/took-on-faith ledger is the closing section. The boundary defect the owner caught — runtime *enforcement* pulled into compiler-only work — is the thing this revision exists to fix, and it fixes it.

---

## Verdict

**Revisions required — ONE blocking finding, narrowly scoped. Otherwise sign-off-ready.**

This is a materially stronger artifact than the 06-11 plan it supersedes, and the 06-11 plan was the best strategic document this project had produced. The hard compiler/runtime boundary is real, not cosmetic: I hunted for a single runtime-execution exit criterion in a *retained* slice and did not find one — every in-scope exit is verifiable with `Evaluator.Fire/Update/InspectFire/InspectUpdate/Restore` still throwing `NotImplementedException`, which I confirmed they do (`Evaluator.cs:80/99/121/132/155`). The four owner decisions (D1–D4) are applied consistently and — critically — *honestly*: the plan never once claims overflow prevented, and it draws the relocation-vs-relaxation line (D2 vs D1) exactly where it belongs. My three prior blocking findings (B1/B2/B3) and the advisory set (A1/A2/A3/A6/A7) are all genuinely addressed, not cosmetically waved through; I verified each against the plan text *and* the source it cites.

It does not get a clean "proceed" because of one thing, and it is the kind of thing I do not let slide on the single most philosophically-load-bearing decision in the plan: **the D1 overflow-honesty sweep under-enumerates its own metadata-enforcement surface.** The plan softens every *prose* canon sentence that claims overflow prevented (spec §0.7, Principle 11, `philosophy.md` surfaced owner-gated, `evaluator.md §10` narrowed, `fault-system.md` documents the gap) and it builds Slice 6.2 to make the parked `NumericOverflow` gap explicit in the liveness analyzer — that is real, substantive work and it is 90% of the job. But the Roslyn analyzer `PRECEPT0002` carries a *description string* that asserts, of **every** `FaultCode` member, that its diagnostic "prevents it at compile time" and "makes this fault unreachable at runtime." Post-D1 that is a false canon-in-code sentence for `NumericOverflow` — and DoD-4's own verification method ("a doc-grep confirms no canon sentence claims overflow … *prevented*") will either miss it (incomplete verification) or hit it with no reconciliation instruction in the plan. This is a catalog/metadata-honesty completeness gap, squarely in my non-negotiable lane, and it is the exact shape as my prior B1 (an honesty/completeness sweep that stops one surface short). It is half an hour to clear.

Clear B1 and this plan has my architectural approval to go to Shane for the Phase-0 gate. The boundary is sound, the decisions are honest, the rigour exceeds the bar, and the philosophy survives — the band relocation does not weaken prevention, and the overflow relaxation is disclosed, not hidden.

---

## Blocking findings

### B1 — The D1 overflow-honesty sweep stops one truth-surface short: the `PRECEPT0002` analyzer (and DoD-4's verification scope) still assert overflow is statically prevented

**Defect.** D1 parks overflow as an accepted, documented, *never-claimed-prevented* known fault. The plan reconciles this across the prose canon thoroughly. But the catalog architecture enforces the prevention invariant in **code**, via a Roslyn analyzer I read verbatim:

- `src/Precept.Analyzers/Precept0002FaultCodeMustHaveStaticallyPreventable.cs:19–24` — message: *"every fault must reference the DiagnosticCode that prevents it at compile time"*; description: *"to assert that the corresponding compile-time diagnostic makes this fault unreachable at runtime."*

That analyzer is the machine-readable encoding of `philosophy.md:53` / Principle 11 ("every fault class … is linked to a compiler diagnostic that prevents it"). After D1, `FaultCode.NumericOverflow` (`FaultCode.cs:44-45`) keeps its `[StaticallyPreventable(DiagnosticCode.NumericOverflow)]` attribute — correctly, and in fact *necessarily*, because `PRECEPT0002` structurally requires every member to carry it, and because `NumericOverflow` is still in the bijective core (`StaticallyPreventableMap.cs:34`, which I confirmed) and is still emitted by a live obligation path (`ProofEngine.Analysis.cs:534` computed-field result-vs-bound → `NumericOverflow`; `Diagnostics.cs:692` `PreventsFault: FaultCode.NumericOverflow`). So the *attribute* must stay. But `PRECEPT0002`'s **description string** then asserts of `NumericOverflow` exactly what D1 disclaims: that the diagnostic "makes this fault unreachable at runtime."

The plan's Slice 6.2 builds a *new* liveness meta-test that intersects `StaticallyPreventableMap.InverseAttributeMap` with the emitted set and "fails on a **non-parked** empty intersection" (plan `:1045`), explicitly carving `NumericOverflow` as a parked-by-design gap "not counted as a Principle-11 violation" (plan `:1043`). That is the right move and it does the hard part. What it does **not** do: reconcile `PRECEPT0002`'s own description, nor scope DoD-4's "doc-grep confirms no canon sentence claims overflow prevented" (plan `:1676`) to the analyzer/`src/Precept.Analyzers/*.cs` message strings. The attribute carries two meanings — the *linkage* (fault↔diagnostic, still valid) and the *delivered guarantee* (prevention, now false for one member) — and `PRECEPT0002`'s text conflates them.

**Why it blocks.** This plan's whole D1 thesis is "honest about approximation — never present overflow as prevented," and its DoD-4 verification *claims completeness* ("no canon sentence claims overflow/representability prevented"). A completeness claim that misses the one in-code surface still asserting prevention undercuts itself — the same failure mode as my prior B3 (a completeness claim misstating its source set) and B1 (an honesty sweep under-enumerating its surface). It is also a documentation-sync non-negotiable: a behavior contract changed (overflow no longer prevented), and a documentation-in-code sentence asserting the old contract is owed an update in the same pass. Catalog/metadata honesty is non-negotiable in this codebase; an analyzer that *enforces* "every fault is statically prevented" while the docs admit one is not is precisely the kind of parallel-truth drift the catalog architecture exists to forbid.

**What clears it.** Two cheap edits, both inside work the plan already scopes:
1. In **Slice 6.2** (and its doc-touch), reconcile `PRECEPT0002`'s description/message so it asserts the **linkage** invariant ("every fault must reference its preventing diagnostic") and stops asserting **universal delivered prevention** ("makes this fault unreachable at runtime") — or add an explicit `known-not-prevented` carve-out note keyed to the D1 parked set, mirroring the `StaticallyPreventableMap` docstring's existing "design-time linkage" language for `UnexpectedNull` (`StaticallyPreventableMap.cs:22-27`).
2. Extend **DoD-4**'s verification scope so the "no canon sentence claims overflow prevented" doc-grep covers `src/Precept.Analyzers/*.cs` analyzer strings (and `Diagnostics.cs`/`Faults.cs` *prevention*-claims, not the fault *message* templates), not `docs/` alone.

Nothing else in the plan moves. Slice 6.2 already did the conceptual and mechanized work; this completes its enumeration.

---

## Advisory findings (non-blocking)

### A1 — Name the analyzer surface in the catalog-discipline framing, not just the liveness gate

Slice 6.2 mechanizes the liveness check the right way (Frank-A3 derive-don't-hand-maintain, reusing `Precept0027`'s scanner). Worth one line stating the general principle the B1 fix instances: **when a decision relaxes a guarantee, every surface that *enforces* the guarantee — prose canon AND the analyzer family (`PRECEPT0002`/`0027`/`0029`/`0019`) — is part of the honesty sweep.** This prevents the next guarantee-relaxation from repeating the same one-surface-short miss.

### A2 — The 236-row and 43-bucket reconciliations: totals verified, per-row classifications taken on faith (unchanged from 06-11)

As last time: I verified the arithmetic (67+126+43 = 236; the 43-mined bucket sums; the Point A / Point B / Q5 residue recovery) and spot-read the structure, but I did **not** re-adjudicate all 236 spec-gap classifications or every one of the 43 mined-obligation bucket contents. Confidence MEDIUM-HIGH — the totals reconcile and the sampled rows are accurate. Flagged so the "nothing dropped silently" guarantee is understood as *structurally* verified (every register has a placement and the counts close), not *individually* re-audited row-by-row.

### A3 — Phase 4 `NumericOverflow`/PRE0078 entanglement: name the live obligation site so the executor doesn't read "no built obligation" as true-at-HEAD

D1's registry consequence says "the `NumericOverflow` RETARGET no longer points at a built obligation" — that is the *target* state, correct as a plan statement, but it is **not** true at HEAD: `ProofEngine.Analysis.cs:534` (`CollectComputedFieldBoundObligations`) stamps a live `IntervalContainmentProofRequirement` on a computed field's result-vs-declared-bound that routes to `NumericOverflow` today, and `PRE0078 = DiagnosticCode.NumericOverflow` is the band-containment-vs-representability conflation Slice 1.3's "split/rename PRE0078" targets. The plan handles this correctly at the diagnostic/requirement level (Slice 1.3 reclassifies the band `IntervalContainment` to a warning under D2; the split detaches the "representable range" message), but it names it as "PRE0078 declared-band IntervalContainment," not as the `CollectComputedFieldBoundObligations` site. Add a one-line pointer in Slice 1.3/1.4 so the executor knows the computed-field result-vs-bound obligation *is* the thing being reclassified and that "`NumericOverflow` has no live obligation behind it" becomes true only *after* that reclassification, not before. Pure executor-precision; no strategy change.

---

## Per-dimension assessment (the five charges)

### 1. Boundary integrity — SOUND (this was the highest-stakes check; it passes clean)

The boundary is genuinely runtime-code-free, and I pushed hard to break it.

- **No runtime-execution exit criterion in any retained slice.** I read every phase exit and every runtime-adjacent slice exit. Each is `precept_compile` / `dotnet build` / `dotnet test` / `Compiler.Compile` / doc-capture. The band slices exit at "compiles clean / emits the proven-violation warning," never "governance refuses the write" (Slice 1.3 `:270`, Stress 4 `:1604`). The §10 contract is narrowed honestly (Slice 2.5 / `:403`) rather than asserting a runtime outcome. **Stress 4 (the matched-pair leak hunt, `:1600-1620`) is the right probe and it is genuine** — it walks every runtime-adjacent retained slice (band reclass, D8 dynamic-write, 2c-ii, write-aware tier-2, BUG-030, derived-value, Restore-DU removal) and confirms each holds a compile-time exit or holds the boundary in the reject-when-unprovable direction. I re-ran that probe myself against the stub; it holds.
- **Every `RUNTIME_CODE_DEFER` item carries a real `docs/runtime/*.md` capture pointer.** §11 is exemplary — every row in 11.A/B/C names its section, and I confirmed the principal target exists: `evaluator.md §7.6 "Constraint Evaluation"` is live at `evaluator.md:1326`. The consolidation discipline ("§7.6 is ONE constraint-plan/sweep design, not 8+ edits," `:1191`/`:1287`) is the correct architectural instinct and mirrors the in-plan "merge the three lenses" reuse rule. A defer with no pointer is explicitly disallowed (`:1188`), and I found none.
- **The matched-pair rule is sound, including the D8 dynamic-write case.** The compiler *continues to reject* the dynamic-write dimension-narrowed conversion until `evaluator.md §Intake-Boundary` lands (`:1224`, Stress 4 `:1606`); only the static-fold subset (both operands compile-time constant) is admitted, which needs no runtime capability. This is prevention-engine-correct: unprovable-performable ⇒ reject. The stub I verified (`Evaluator.cs` all-five `NotImplementedException`) is exactly the reason the rule must hold, and the plan grounds it there (`:16`, `:1184`, `:1539`).

The boundary does not leak. This is the defect the owner caught in 06-11, and the revision closes it structurally.

### 2. Decisions correctly applied — SOUND (with B1 attached to D1)

- **D1 (overflow parked).** Verified: `FaultCode.TemporalOverflow` is **ABSENT** from `FaultCode.cs` (15 members, `DivisionByZero=1 … CountBoundViolation=15`, no temporal-overflow member); the plan repeatedly forbids adding it (`:285`, `:1375`, `:1442`). `NumericOverflow`'s `[StaticallyPreventable(DiagnosticCode.NumericOverflow)]` partner is correctly framed as an honest **gap** (`:73`, `:196`, `:281`, `:1369`) — it stays because `PRECEPT0002` requires it and because it is still bijective (`:34`) and still emitted; the registry trim correctly removes only Length/Count, not `NumericOverflow` (`:280-281`). Overflow is **never claimed prevented** anywhere — DoD-4 is explicit and the honesty-about-approximation carve-out is real, not a fig leaf (see Philosophy). **The single residual is B1: the analyzer description-string surface.**
- **D2 (band split).** Clean. Reclassification (fault→flag, proven-violation warning) is in scope (Slice 1.3); enforcement is deferred with capture pointers (Slice 1.3 doc-touch `:271`, §11 `:1203`). The **carries-proof set is preserved** — mincount `count>0` Strategy-2 arm and sign-modifier/rule-fact preservation explicitly survive prove-or-reject (`:265`, `:1666`); nothing that carries a static proof silently loses it. The OutOfRange asymmetry (A2 last time) is correctly carried: never bijective, removal is a no-op (`:267`, `:280`, `:1506`, verified against `StaticallyPreventableMap.cs:28-37`).
- **D3 (atomicity relaxed).** The relaxation introduces no real hole given stub + no users — **Stress 2 (`:1564-1577`) proves it two independent ways** (the runtime enforces nothing regardless; blast radius zero), and the carries-proof floor stays prove-or-reject throughout the transient window, so nothing downstream builds over a new false-clean. The one retained ordering (registry-honest-before-the-Phase-2-checker) is **necessary and unique** — Stress 1 (`:1545-1560`) is a genuine adversarial uniqueness search, and I agree with its verdict: only Phase 2 bakes a structural property over the registry's shape, so only it has a hard data dependency on the registry being honest first. `philosophy.md` is **surfaced, not auto-edited** (D3 / Slice 1.5 `:311` / DoD-9 `:1711-1717`).

### 3. Rigour parity — EXCEEDS the 06-11 bar

Every one of the 8 phases carries per-slice Work / Exit-criteria / Doc-touch, an effort class, dependencies, a phase-exit block, and (Phases 3/4/5/6) a per-phase coverage reconciliation. This is *deeper* than 06-11, which stubbed Phases 2–7 — here all eight are heavyweight. The method-level specificity is real (e.g. `ProofEngine.cs:319-323` walk-stops-at-Object, `SemanticIndex.cs:113` `TypedMemberAccess.Arguments`, BUG-031/032 repros with `precept_compile`-verifiable exit assertions).

- **The consolidated coverage ledger reconciles to the input registers.** I traced it: Floor gaps **16/16** (`:1321-1342`); Scale-backs **8/8** (`:1346-1359`); Registry corrections **10/10** (`:1363-1378`); Spec gaps **236/236** with the arithmetic check 67+126+43 (`:1382-1405`); Mined obligations **43/43** across 7 buckets (`:1409-1421`). The **open-decision residues are explicitly recovered**: Point A → GATE-F (`:1429`), Point B → PARKED with reason (`:1430`), Point C → Slice 0.3 (`:1431`), Q4 → GATE-F B1 (`:1432`), Q5 → Slice 0.3 (`:1433`). "Nothing dropped silently" is *demonstrated* — each previously-leaked item is named with its new home — not merely asserted. The §G PARKED table (`:1437-1452`) gives every parked item a reason, and the DoD §12 re-reconciles every register to a DoD criterion (`:1734-1745`).
- **Calibration is substantive, not decorative** (`:1497-1529`). HIGH/MEDIUM/MEDIUM-LOW with a stated *reason* per item, and the discipline "no load-bearing claim is rated above its evidence" is honored — the stub fact and the registry facts are HIGH (code read verbatim); the owner-gated choices and in-diff disciplines are MEDIUM; the genuinely-unprobed items (temporal-overflow NodaTime behavior, omit-join classification, GATE-D/G/I/J/K picks) are held at MEDIUM-LOW with a named probe/gate that must clear first. That is correct calibration.
- **The sequencing red-team is the real thing** (`:1533-1638`), and stronger than 06-11's. Four adversarial stresses with ground-truth anchors, a hard-vs-soft-vs-decision-coupling taxonomy, and — the part that matters most — Stress 4 is a boundary-integrity probe that I would have written myself. Real failure modes (one-shot structural bake over the registry; wrong-home re-derivation of choice-default membership), real mitigations (the one hard ordering; cross-reference tags; the unconsumed-artifact guard).

### 4. Prior findings — all ACTUALLY addressed (verified against plan text AND source)

- **B1 (GATE-F enumeration completeness, incl. Principle 11's defensive-redundancy clause).** **CLEARED.** The plan enumerates every band-prove-or-reject sentence verbatim — §0.7 L256/L258/L266, §0.6 item-6 L208, Principle 11 "constraint range impossibility" — **and** the L112 "Runtime fault checks exist only as defensive redundancy" clause with an explicit ruling (governance refusal is not a fault check ⇒ the literal clause likely survives, but quoted to the owner) (`:140-148`, `:300-304`, DoD-3 `:1668`). I re-verified all five spec sites against HEAD: L112, L208, L256, L258, L266 are verbatim-accurate. The D1 overflow-honesty addition to the GATE-F set (`:147`) is the right extra leg.
- **B2 (`DesugarsToRule` wording).** **CLEARED.** "Zero constraint-synthesis/enforcement consumers; 2 cosmetic/display" everywhere (`:103`, `:252`, `:1515`, `:1585`); "Never write 'ZERO consumers'" (`:252`). I re-confirmed against source: exactly 2 reads of `.DesugarsToRule` (`CatalogFormatters.cs:253+`, `GrammarGen/Program.cs:173`), zero in `src/Precept/` pipeline.
- **B3 (28-vs-32 provenance).** **CLEARED.** "43 mined obligations recovered from the triage of 32 prior docs (28 superseded + 4 archived)" (`:104`, `:204`), with the archived-docs-obligation confirmation and the FaultCode-count reconcile (verified 15 members) folded in.
- **A1 (status).** **CLEARED.** Front-matter is `Draft plan — 2026-06-16` (`:3`, `:105`).
- **A6 (relocation-not-weakening).** **CLEARED and load-bearing.** Made explicit and kept rigorously *distinct* from D1 (`:87`, `:106`, `:268`, `:1359`, DoD-4 `:1677`). This is the framing that makes the philosophy conversation honest — see Philosophy.
- **A7 (Phase-3 hidden risk).** **CLEARED.** D1 *dissolves* the representability family, the prior highest hidden Phase-3 risk; the plan says so and removes it as a build risk (`:107`, `:418`).
- **A2 / A3** also folded (registry asymmetry noted as a no-op; catalog-routing guardrail "shrink-not-grow," derive-don't-hand-maintain) — `:267`, `:288`, `:345`, `:1378`.

### 5. Philosophy — SOUND. I am the philosophy check; here are the two calls, drawn explicitly.

I read `philosophy.md` against the plan, verbatim. The two hard questions:

**(a) Is parking overflow defensible, or does it erode "invalid configurations are structurally impossible"? Is D1 honest?**

It *does* erode the absolute claim for overflow specifically — and the plan does not pretend otherwise, which is exactly what makes it honest rather than a fig leaf. `philosophy.md:53` says, verbatim: *"Division by zero, arithmetic overflow, empty collection access … are not risks managed at runtime. They are compile-time impossibilities."* D1 makes arithmetic overflow *not* a compile-time impossibility. That is a genuine **relaxation** of a locked philosophy guarantee. The honesty test is whether the system stops *claiming* the guarantee it no longer delivers — and Precept's own "honesty about approximation" principle ("Precept does not present approximation as exactness") is the governing rule. The plan passes it: DoD-4 forbids any canon sentence claiming overflow prevented; DoD-9 surfaces `philosophy.md` L49/L51/L53/L57/L61 (I verified all five verbatim) as owner-gated edits and refuses to auto-resolve them; the floor invariant is restated as "prove-or-reject EXCEPT decimal/temporal representability overflow — a parked known fault we never claim prevented." The gate cannot reach "done" without the owner ratifying the philosophy reconciliation (DoD-9 + DoD-10 are terminal). **That is an honest relaxation under honesty-about-approximation, not erosion-by-stealth.** My one reservation is precisely B1: the honesty is complete in prose and in the liveness gate but stops short of the `PRECEPT0002` analyzer string — which still asserts the impossibility the rest of the sweep disclaims. Close B1 and the honesty is airtight.

**(b) Is relocating band enforcement (D2) "relocation-not-weakening," or weakening dressed up as relocation? The distinction, explicitly:**

- **Weakening** = a configuration that *was* structurally impossible becomes possible (whether detected later or not).
- **Relocation** = the *same* impossibility is enforced, at a different pipeline point; the invalid configuration stays impossible.

For bands on **non-proof-carrying / ingress-sourced fields**, the compiler never had the value to prove on — the current prove-or-reject behavior over-*rejects* (it rejects merely-*unprovable* intervals, which is rejecting valid definitions, not preventing invalid data). Moving enforcement to runtime governance means an out-of-band ingress write is refused *atomically* at the boundary (working copy discarded, no invalid state persists) — and atomic refusal **is** prevention. So for those values D2 is **genuinely relocation**: prevention moves from a compile-time over-rejection (which was not real prevention of the value) to a runtime-governance atomic refusal (which is). Three facts make this honest, and I verified each: (i) the **carries-proof set stays in the compile-time floor** — genuine compile-time prevention is preserved exactly where the compiler has the value (`:265`, `:1666`); (ii) **bands were never a `philosophy.md` compile-time-impossibility claim** — L53's enumerated impossibilities are divisor / overflow / empty-collection, all *retained* floor families; bands are not in that list, so the band relocation needs **no** `philosophy.md` edit at all (that is the A6 argument, and it is correct); (iii) the runtime half is **captured and deferred** (`evaluator.md §7.6` recoverable-refusal lane, distinct from `Faulted`), not silently dropped, and the plan never claims it runs today (DoD-3 "Runtime half deferred"). **Verdict: genuine relocation, honestly drawn, and correctly held distinct from the D1 relaxation.** The plan insists on the D1/D2 distinction relentlessly and never blurs them — which is the single most important philosophical discipline in the document.

**(c) Is `philosophy.md` treated as owner-gated?** Yes — surfaced, never edited by the plan, in every place it appears (D1 `:71`, Slice 1.5 `:311`, DoD-9 `:1711-1717`, §B.10 of the companion notes). Correct per the CLAUDE.md philosophy rule.

---

## Per-phase assessment (rigour-parity call vs the 06-11 bar)

- **Phase 0 — Design gates + read-only probes — SOUND, exceeds.** Correctly CANON_DESIGN + read-only-probes only, zero COMPILER_CODE/RUNTIME_CODE (`:118`). GATE-F carries the complete Frank-B1 enumeration (`:140-148`). Slice 0.3 captures the runtime-canon reconciliations (GATE-I fault-delivery contradiction — verified real at `result-types.md:51` throw vs `:104` `Faulted`; Restore §0.7:272-vs-§3A.2:1946 contradiction — verified real; OD-1; standing declines four-leg; D9; Point C). Slice 0.4 (reconcile-stale-research-before-feeding-agents) is the one Phase-0 exit that gates Phase 1+ — correct discipline. The `RestoreConstraintsFailed` removal correctly classified COMPILER_CODE-not-runtime (verified live DU at `RestoreOutcome.cs`).
- **Phase 1 — Registry honesty + band right-sizing (compiler half) — SOUND, exceeds; highest-risk, correctly identified.** Slice 1.4 is the one HARD-ordering producer; the registry corrections (10/10) are grounded in verified call sites (the `_ => DivisionByZero` backstop at `ProofEngine.Diagnostics.cs:490`; the pow-exponent `≥0 ⇒ SqrtOfNegative` misroute at `:390-392`; the `Literal // vacuously proved` mislabel at `ProofEngine.cs:1181/1194`; `ProofStrategy` at `ProofLedger.cs:100`). **Attach B1** (Slice 1.4/1.5 + Slice 6.2 own the analyzer-string fix) and **A3** (name the `CollectComputedFieldBoundObligations` site).
- **Phase 2 — Position-totality + creation-exhaustiveness checker — SOUND, exceeds.** The `ChildExpressionPositions` catalog projection is textbook catalog-before-code; the 3-layer checker reuses the shipped `Precept0019`/`DiagnosticCoverageScanner` family (no parallel mechanism); the intra-guard short-circuit narrowing bundled in the same slice as the guard-slot closure is a genuinely important coupling, correctly kept atomic. Slice 2.5's InspectFire-no-fault-lane runtime-precondition record is a sophisticated boundary-preservation move (records *why* the compiler checker is a hard runtime precondition without asserting any runtime outcome).
- **Phase 3 — False-Proved repairs + write-aware tier-2 (intra-chain) — SOUND, exceeds.** Hard scope boundary: same-transaction only; committed-state discharge is sound-by-refusal (compiler rejects) with the enforcement deferred + captured. A7 dissolution applied (no representability build risk). Per-phase coverage reconciliation present.
- **Phase 4 — Datatype-surface completeness — SOUND, exceeds.** Type-derivation (compiler) cleanly separated from value-computation (Slice 4.9 captures the runtime tails with per-family capture lines — closing the prior BIZ-9..13 silent-loss gap). D8 dynamic-write admission BLOCKED matched-pair. GATE-J/K canon forks correctly routed. Phase-4 PARKED block disclosed.
- **Phase 5 — Constructs completeness — SOUND, exceeds; largest surface.** 2c-ii (BUG-020, largest unbuilt locked design) ships prove-or-reject at compile time with the unconsumed-artifact guard (the DesugarsToRule↔2c-ii consumer coupling, surfaced at both ends — Stress 3a). Breadth risk, parallelizable, correctly sequenced after the floor holds.
- **Phase 6 — Diagnostic completeness + ownership analyzers — SOUND, exceeds.** Emit-or-retire sweep through `DiagnosticCoverageScanner` Patterns 1/2/3 (count re-derived against HEAD, not memory). *[Correction 2026-07-02: the scanner implements only Pattern 1 (literal) and Patterns 2/2b (catalog-mediated); there is no scanner Pattern 3 — the dynamically-dispatched emission residue is not scanner-detected and must be hand-enumerated, as the plan's Slice 6.1 now states.]* **Slice 6.2 is where B1's fix lands** — it already carves the parked `NumericOverflow` gap; it must also reconcile the `PRECEPT0002` description string. The single-stage-ownership and consumer-`*Kind` analyzers reuse the existing family (Frank-A3 compliant). The three-lenses-into-ONE-register consolidation is the right reuse-not-parallel discipline.
- **Phase 7 — Conformance + API solidity + gate + handoff — SOUND, exceeds.** Conformance runtime facet authored as Skip'd v1 acceptance spec (the cases ARE the spec; green is deferred) — the matched-pair made visible in one place. API solidity as the runtime precursor (typed descriptors single-source — verify-reuse-don't-fork; `RestoreConstraintsFailed` removal). Runtime-ready gate redefined with zero runtime-execution acceptance; §11 handoff enumerated; owner sign-off terminal.

---

## What I verified vs. what I took on faith

**Verified against live source this pass (all accurate unless noted):**

| Claim | Source checked | Result |
|---|---|---|
| Runtime is a pure stub (Fire/Update/InspectFire/InspectUpdate/Restore) | `Evaluator.cs:80/99/121/132/155` | Confirmed — all five `throw new NotImplementedException()` |
| 15 FaultCodes; `TemporalOverflow` ABSENT; `NumericOverflow=12` carries `[StaticallyPreventable]` | `FaultCode.cs:11-55` | Confirmed |
| Bijective core excludes OutOfRange (A2); includes NumericOverflow | `StaticallyPreventableMap.cs:28-37` | Confirmed (`NumericOverflow` at `:34`; no `OutOfRange`) |
| Generic `_ => DivisionByZero` backstop; pow-exponent `≥0 ⇒ Sqrt` misroute | `ProofEngine.Diagnostics.cs:490`, `:386`, `:390-392` | Confirmed |
| `Literal // vacuously proved` mislabel (+ dead-end-state parallel) | `ProofEngine.cs:1181`, `:1194`; `ProofLedger.cs:100-102` | Confirmed |
| `RestoreConstraintsFailed` is a live API-surface DU subtype | `RestoreOutcome.cs` | Confirmed |
| `DesugarsToRule` has exactly 2 cosmetic/display consumers, 0 pipeline (B2) | grep `.DesugarsToRule` across `src/`,`tools/`,`test/` | Confirmed (`CatalogFormatters.cs:253+`, `GrammarGen/Program.cs:173`) |
| GATE-F spec sites verbatim — Principle 11 L112, §0.6 L208, §0.7 L256/L258/L266 | `precept-language-spec.md` | Confirmed verbatim (incl. the "defensive redundancy" clause) |
| Restore contradiction §0.7:272 (trusted hydration) vs §3A.2:1946 (re-validation) | `precept-language-spec.md:272`, `:1946` | Confirmed — real internal-canon contradiction |
| GATE-I fault-delivery contradiction (throw vs structured `Faulted`) | `result-types.md:51` / `:104`; `evaluator.md` Decision 4 `:1754`; §7.6 `:1326` | Confirmed real; capture target exists |
| `philosophy.md` overflow-as-compile-time-impossibility sites | `philosophy.md:49/51/53/57/61` | Confirmed verbatim — L53 lists overflow; D1 gap is real |
| `evaluator.md` "no diagnostics ⇒ no faults" headline | `evaluator.md:19`, `:1679` | Confirmed |
| **`PRECEPT0002` asserts universal prevention in its description (B1)** | `Precept0002FaultCodeMustHaveStaticallyPreventable.cs:19-24` | **Confirmed — over-claims for parked NumericOverflow** |
| Live `NumericOverflow` obligation today (computed-field result-vs-bound) | `ProofEngine.Analysis.cs:534-556`; `Diagnostics.cs:690-692` | Confirmed (A3 — "no built obligation" is target-state, not HEAD) |

**Taken on faith (confidence noted):**

- The **236-row audit per-row classifications** and the **43-mined bucket contents** — totals reconciled and structure spot-read; not re-adjudicated row-by-row (A2). MEDIUM-HIGH.
- The **"28 superseded + 4 archived = 32"** triage count — carried from my prior (verified) review, not re-counted this pass. HIGH.
- The **10 fail-open positions / 6 latent no-default-arm holes** exact counts and the `ProofEngine.cs:319-323` walk-stops-at-`Object` claim — BUG-031/032 pattern verified in my prior review; not re-run via `precept_compile` this pass. MEDIUM.
- The **temporal-overflow NodaTime behavior** — correctly self-marked UNPROBED/MEDIUM-LOW; Phase-0 probe owed.
- **Effort-class day ranges** for all phases — accepted as classes, not validated as counts.

---

## Final call — is the plan READY FOR OWNER SIGN-OFF on the Phase-0 gate set?

**No — but only on a single, narrowly-scoped, half-hour blocking finding.**

The architecture clears: the boundary is runtime-code-free and I could not break it; D1–D4 are applied honestly; my prior B1/B2/B3 and A1/A2/A3/A6/A7 are genuinely closed; the rigour exceeds the bar; the coverage ledger reconciles; the philosophy survives (band relocation is genuine relocation, overflow relaxation is disclosed, `philosophy.md` is owner-gated). That is my approval of the *architecture and the guarantee-contract shape*.

**Blocking list that must clear before this goes to Shane for the Phase-0 gate:**

- **B1** — Complete the D1 overflow-honesty sweep onto its metadata-enforcement surface: reconcile `PRECEPT0002`'s description string (assert the linkage invariant, not universal delivered prevention; or add the known-not-prevented carve-out) in Slice 6.2, and extend DoD-4's verification doc-grep to scope `src/Precept.Analyzers/*.cs`. Until this lands, the catalog architecture *enforces in code* the very overflow-prevention claim D1 disclaims, and DoD-4's completeness claim is unverifiable for that surface.

Fold in A1 (name the analyzer family in the honesty-sweep framing) and A3 (name the `CollectComputedFieldBoundObligations` site) as you go. Clear B1 and the plan has my architectural sign-off; the Phase-0 gate set — GATE-F's owner-gated spec amendments, the `philosophy.md` overflow reconciliation, GATE-I's fault-delivery pick — remains Shane's to ratify, as it must be. Surface GATE-F complete (it is) and the `philosophy.md` overflow gap framed with the A6 distinction (it is), and that conversation will be clean.

The boundary defect the owner caught is fixed. The thesis is right, the homework is real, and the honesty is — with one analyzer string's exception — exemplary. That is not faint praise from me.

— Frank

---

## Amendment verification — 2026-06-21

I re-reviewed only the surfaces my B1/A1/A3 findings touch, and I source-checked the amendment against the live analyzer, the `StaticallyPreventableMap`, and `ProofEngine.Analysis.cs` rather than trusting the doc's self-description. The amendment is **real, not cosmetic**, and it lands the fix in the correct slice with the correct architectural shape.

### Updated verdict

**B1 CLEARED. The plan is sign-off-ready.** The amendment does exactly what I specified — it carries the D1 overflow-honesty sweep onto its metadata-enforcement surface (the `PRECEPT0002` analyzer string) and widens DoD-4's verification scope to the in-code surface — and it does so by rewording to the **linkage invariant** / adding a **`known-not-prevented` carve-out**, NOT by deleting the honesty requirement. No residual blockers. The architecture has my sign-off; the Phase-0 gate set remains Shane's to ratify, as it must.

### Per-finding disposition

- **B1 — CLEARED.** Both legs are in, and both are architecturally sound:
  - **(a) DoD-4 scope widened.** `:1679` now reads "scoped over `docs/` AND `src/Precept.Analyzers/*.cs` analyzer message/description strings AND the `Diagnostics.cs` / `Faults.cs` *prevention*-claim strings (Frank B1 — not `docs/` alone …)." Critically, it scopes to **prevention-claim** strings, not the fault *message* templates — the exact precision I asked for (a fault's own message legitimately names the fault; only a *prevention* claim is the false canon-in-code sentence). The completeness claim is now verifiable on the surface that previously escaped it.
  - **(b) PRECEPT0002 reconciliation enumerated as an in-scope code-as-canon edit in the correct slice, with a real exit criterion.** Slice 6.2 `:1044` enumerates the edit: reword `PRECEPT0002`'s message/description to assert the **linkage invariant** and stop asserting **universal delivered prevention** ("makes this fault unreachable at runtime"), OR add a **`known-not-prevented` carve-out** keyed to the D1 parked set. Exit criterion `:1051` (B1): the description "no longer asserts universal delivered prevention … a string-grep over `src/Precept.Analyzers/*.cs` finds no in-code sentence claiming `NumericOverflow` is prevented / unreachable." Doc-touch `:1053` lists it explicitly as a **"(B1) Code-as-canon edit"** on `Precept0002FaultCodeMustHaveStaticallyPreventable.cs`, not a docs-only touch.
  - **Architecturally sound — source-confirmed.** The live analyzer over-claims exactly as flagged: `Precept0002FaultCodeMustHaveStaticallyPreventable.cs:24-25` description asserts "the corresponding compile-time diagnostic makes this fault unreachable at runtime" of **every** member — false for parked `NumericOverflow`. The carve-out the plan tells the executor to mirror **actually exists**: `StaticallyPreventableMap.cs:18-26` carries the "design-time linkage … the runtime backstop, not the attribute, governs" docstring for `UnexpectedNull`. And the attribute must indeed stay: `NumericOverflow` is still in `BijectiveDiagnosticCodes` (`StaticallyPreventableMap.cs:34`) and still emitted — so the prescribed edit (reword the *delivered-guarantee* sentence, keep the *linkage*) is the correct one. The plan does not weaken the analyzer's structural requirement; it disentangles the two meanings the description-string conflated. That is the LINKAGE-invariant fix I demanded, not a honesty-requirement deletion.

- **A1 — CLEARED.** `:1045` states the general principle where it belongs (in Slice 6.2, the B1 fix site): "when a decision relaxes a guarantee, **every surface that enforces it** is part of the honesty sweep — prose canon **and** the analyzer family (`PRECEPT0002` / `0027` / `0029` / `0019`), not docs alone. This B1 fix is the first instance; the next guarantee-relaxation applies the same rule." That is the durable rule, named with the analyzer family, exactly as A1 asked.

- **A3 — CLEARED.** `:281` (Slice 1.4) names the live obligation site with the target-state-not-true-at-HEAD framing: "'no live obligation behind `NumericOverflow`' is the **target state, not true at HEAD** — today the computed-field result-vs-declared-bound check at `ProofEngine.Analysis.cs:534` (`CollectComputedFieldBoundObligations`) stamps a live `IntervalContainmentProofRequirement` routing to `NumericOverflow`; that is the band `IntervalContainment` Slice 1.3 reclassifies, so the honest gap becomes true only **after** Slice 1.3 lands, not before." Source-confirmed: `ProofEngine.Analysis.cs` `CollectComputedFieldBoundObligations` stamps an `IntervalContainmentProofRequirement` → `NumericOverflow` on a computed field's result-vs-declared-bound (the doc-comment says so verbatim; the body matches). The executor can no longer read "no built obligation" as true-at-HEAD. Slice 1.3 `:262-263` is correctly named as the reclassifying slice. Pure executor precision; no strategy drift.

### Regression check

- **Boundary still clean.** The amendment added exactly two verification surfaces — DoD-4's grep (`:1679`) and Slice 6.2 exit (B1) (`:1051`) — both are **static string-greps over `src/Precept.Analyzers/*.cs`**, not runtime-execution assertions. Slice 6.2 `:1052` explicitly keeps the reciprocal "runtime emits the fault" check **out** (deferred evaluator facet, §11). No new runtime-execution exit criterion was introduced. The compiler/runtime boundary holds.
- **Coverage ledger still reconciles.** The ledger registers (16/8/10/236/43) and the §12 DoD re-reconciliation are untouched; the amendment added clauses inside existing Slice 1.3/1.4/6.2 and DoD-4 bodies without renumbering or dropping a register row. `:1739` Floor-gaps-16 → DoD-4 mapping intact.
- **D1/D2 distinction intact — and the amendment reinforced it.** Slice 1.3 `:268` still keeps the A6 relocation (band → runtime governance, *not* weakened) **distinct** from D1's genuine relaxation. The new B1 text at `:1044` sharpens the same line in code terms — it reconciles only the *overflow* (D1) prevention claim and explicitly leaves the linkage invariant and the band/bijective machinery (D2) standing. The single most important philosophical discipline in the doc — relaxed-vs-relocated — is not blurred; it is carried one surface deeper.

### Final call

**READY FOR OWNER SIGN-OFF on the Phase-0 gate set — Yes.** B1 is cleared on both legs with the correct architectural shape (linkage-invariant / known-not-prevented carve-out, source-confirmed against the live analyzer and the `StaticallyPreventableMap` precedent it mirrors); A1 and A3 are folded; no regression to the boundary, the ledger, or the D1/D2 distinction. No residual blocking list. The Phase-0 gate set itself — GATE-F's owner-gated spec amendments, the `philosophy.md` overflow reconciliation, GATE-I's fault-delivery pick — remains Shane's to ratify, as it must be. The plan has my architectural approval to go to the gate.

— Frank
