# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

---

### 2026-06-23T23:24:14Z: Arg→Field Constraint-Propagation Amendment Assessment

**By:** Frank
**To:** Shane
**Status:** Advisory design opinion — not pending Shane sign-off; merged from `.squad/decisions/inbox/frank-arg-constraint-doc-reframe.md`.
**Target:** `docs/Working/compiler-readiness-plan-2026-06-16.md`
**Persisted assessment:** `docs/Working/arg-constraint-propagation-opinion-2026-06-23.md`

- Reframed per owner direction (2026-06-23) from “plan amendment to fold into GATE-F” to an advisory design opinion; verdict = oppose-as-posed; recommend the inverse (infer-band / reject-provable / govern-rest).
- The earlier “pending Shane sign-off” framing applied to the amendment; this advisory opinion is on the merits, not a ratification item.

---

### 2026-06-16T13:06:00Z: Architectural Review: Compiler Readiness Plan (2026-06-11)

**By:** Frank
**To:** Shane
**Status:** Merged from `.squad/decisions/inbox/frank-compiler-readiness-plan-review.md`.
**Target:** `docs/Working/compiler-readiness-plan-2026-06-11.md`
**Review:** `docs/Working/compiler-readiness-plan-2026-06-11-frank-review.md`
**Companion research:** research/architecture/compiler/fault-floor-definition-2026-06-11.md; docs/Working/compile-time-niche-decision-packet-2026-06-10.md

- Verdict: Sound with required revisions.
- Blocking findings: B1 GATE-F under-enumerates the owner-gated canon-amendment set; B2 `DesugarsToRule` is not literally zero-consumer today but has zero `src/Precept/` consumers; B3 28-vs-32 prior-docs provenance mismatch.
- Advisory findings: A1–A7 folded into the review.
- Verified all 13 load-bearing claims against source.
- Coordinator note: `.squad/config.json` model overrides for Frank and Elaine were bumped to `claude-opus-4.8`.

---

### Compiler-Readiness Plan 06-16 — Amendment Verification (B1 Clearance)

**By:** Frank
**Date:** 2026-06-21T10:38:00-04:00
**Status:** ✅ B1 CLEARED — plan is sign-off-ready. Phase-0 gate ratification remains Shane's.
**Target doc:** `docs/Working/compiler-readiness-plan-2026-06-16.md` (amended after my 06-16/06-21 re-review)
**Review record:** `docs/Working/compiler-readiness-plan-2026-06-16-frank-review.md` § Amendment verification — 2026-06-21
**Requested by:** Shane (owner)

---

## Decision

The amendment **clears B1** and addresses A1 and A3. The plan is **READY FOR OWNER SIGN-OFF on the Phase-0 gate set.** Verified by source-check, not by the doc's self-description.

## Per-finding disposition

### B1 — CLEARED (blocking → resolved), both legs, correct architectural shape

- **(a) DoD-4 verification scope widened.** Plan `:1679` now scopes the "no canon sentence claims overflow prevented" grep over `docs/` **AND** `src/Precept.Analyzers/*.cs` analyzer message/description strings **AND** the `Diagnostics.cs`/`Faults.cs` *prevention*-claim strings. Correctly scoped to **prevention-claims**, not fault *message* templates (a fault's own message legitimately names the fault; only a prevention claim is the false canon-in-code sentence).
- **(b) PRECEPT0002 reconciliation enumerated as an in-scope code-as-canon edit in Slice 6.2.** Work `:1044`; exit criterion `:1051` (the description no longer asserts universal delivered prevention; a `src/Precept.Analyzers/*.cs` grep finds no in-code sentence claiming `NumericOverflow` prevented/unreachable); doc-touch `:1053` lists it explicitly as a **"(B1) Code-as-canon edit"** on `Precept0002FaultCodeMustHaveStaticallyPreventable.cs`.
- **Architecturally sound.** The fix rewords to the **linkage invariant** ("every fault must reference its preventing diagnostic") / adds a **`known-not-prevented` carve-out** — it does **not** delete the honesty requirement, and it keeps the attribute (NumericOverflow is still bijective and emitted). Source-confirmed: the live analyzer over-claims at `Precept0002FaultCodeMustHaveStaticallyPreventable.cs:24-25` ("makes this fault unreachable at runtime"); the carve-out precedent it mirrors exists at `StaticallyPreventableMap.cs:18-26` (the design-time-linkage docstring for `UnexpectedNull`); `NumericOverflow` is in `BijectiveDiagnosticCodes` (`StaticallyPreventableMap.cs:34`).

### A1 — CLEARED

General honesty-sweep principle named at plan `:1045`: when a decision relaxes a guarantee, **every enforcing surface** — prose canon **and** the analyzer family (`PRECEPT0002`/`0027`/`0029`/`0019`) — is part of the sweep, not docs alone. This B1 fix is the first instance.

### A3 — CLEARED

`CollectComputedFieldBoundObligations` (`ProofEngine.Analysis.cs:534`) named at plan `:281` with the **target-state-not-true-at-HEAD** framing: today it stamps a live `IntervalContainmentProofRequirement` → `NumericOverflow`; the "no live obligation" gap becomes true only **after** Slice 1.3 reclassifies the band IntervalContainment. Source-confirmed against `ProofEngine.Analysis.cs`.

## Regression check

- **Boundary clean.** Both added verification surfaces (DoD-4 grep `:1679`; Slice 6.2 exit `:1051`) are static string-greps over analyzer source — not runtime-execution assertions. Slice 6.2 `:1052` keeps "runtime emits the fault" out (deferred evaluator facet, §11). No new boundary leak.
- **Coverage ledger reconciles.** Registers (16/8/10/236/43) and §12 re-reconciliation untouched; clauses added inside existing slice bodies without renumbering or dropping a row.
- **D1/D2 distinction intact and reinforced.** Slice 1.3 `:268` keeps A6 relocation distinct from D1 relaxation; the new B1 text reconciles only the overflow (D1) prevention claim, leaving the linkage invariant and band/bijective machinery (D2) standing.

## Final call

**READY FOR OWNER SIGN-OFF on the Phase-0 gate set — Yes.** No residual blockers. The Phase-0 gate ratification itself — GATE-F's owner-gated spec amendments, the `philosophy.md` overflow reconciliation, GATE-I's fault-delivery pick — remains Shane's, as it must be.

— Frank
## Runtime Guarantee Model

---

# Decision note — Band-model ruling: Phase-0 corpus map & frozen question set

**Author:** Frank (Lead/Architect) · **Owner:** Shane · **Date:** 2026-07-06 · **Phase:** 0 (setup — no ruling)
**Branch:** `spike/Precept-V2-Radical`

## What this note records
Phase-0 of the Shane-approved bounded research operation that will rule on **which guarantee model governs Precept**. Phase 0 only *frames* the evidence hunt; the ruling is Phase 3, Shane ratifies Phase 4. Nothing here decides the model.

## Primary axis (Q1) — the frozen fork
The ruling's primary axis is a three-way meta-fork; every other question resolves as a consequence:

- **Model A — Bounded compiler + runtime governance.** Compiler proves a subset; non-discharged bounds are *governed at runtime*. Partial compiler + stopping rule. Precedent: gradual verification, SPARK/Ada. Risk: false promise + runtime-enforcement limits.
- **Model B — Total compiler + deliberately limited surface.** Not-statically-decidable ⇒ **inexpressible**; author rewrites into the provable subset. "Compiles clean" = "totally proven." Cost: expressive power. Philosophy-native.
- **Principled hybrid — total over the definition-internal surface, with an honest, typed, VISIBLE governance boundary only at genuinely-external runtime input** (§0.7:268 surface). No silent partial-proof.

Q2–Q11 (stopping/admissibility rule; expressiveness cost of B; runtime-enforceability of the deferred set; proven-violation severity; default-vs-set-action consistency; merely-unprovable case; carries-proof bounds; false-promise resolution; philosophy over-read + GATE-F spec amendments; taxonomy/adjacent-calls) are each framed "under A / under B / under hybrid" so readers hunt for *discriminating* evidence, not history summaries. Added sub-questions: Q1a (provenance of the debate's own authority), Q2a (is D2 even shipped — finding: proposal-only), Q3a (does §0.6:249 already show Model B live for the string-length axis), Q4a (does `ConstraintsFailed` distinguish external-input governance from deferred internal-band enforcement).

## Two ground-truth discriminators (verified first-hand at HEAD)
1. **Spec §0.7:266 says "there is no deferral"** and §0.6:258 says count bounds are "never deferred to a runtime check." Canonical spec is presently written B/hybrid-shaped; D2 (`decision-index.md:20`) would amend it toward A (that amendment IS GATE-F).
2. **D2 band-split is proposal-only, not shipped.** At HEAD `CountBoundViolation`(:1295) / `LengthBoundViolation`(:1283) are `Severity.Error`; PRE0136 fires even on the merely-unprovable case. Readers must not describe D2 as current behavior.

## Deliverables & locations
- **Reader-brief template + full corpus map + frozen question set:** `~/.copilot/session-state/21c4af47-5141-4752-ac79-6938d5c2e269/files/phase0-corpus-map-and-brief-spec.md` (Sections 1–4).
- **Fleet:** 6 readers (A niche packet · B fault-floor+precedent · C 06-16 plan/D1–D4/GATEs · **D reserved-crux+runtime-enforceability, owned by George** · E 2026-07-06 convergence [verify-don't-inherit] · F expressiveness ledger) + **anchor G** (primary-source/provenance/git-blame loop-breaker).
- Corpus gaps I fixed vs the plan: added the expressiveness-cost corpus (F), the prior-art precedent surveys (B), and an end-to-end runtime-enforceability owner (D/George); corrected the plan's "§0.7:258" → §0.6:258.

## Guardrails carried
Owner-gated surfaces (`philosophy.md`, spec §0.6/§0.7, GATE-F/GATE-H amendments) are **recommend-only** until Shane's one-pass ratification (Phase 4). No spec/philosophy edit in this operation. Fresh re-derivation (D-A): the 2026-07-03 and 2026-07-06 docs are claims to verify, not a starting position.

---

---
title: Runtime CANNOT enforce a deferred internal declared band — Model A escape hatch is a false promise at the enforcement layer
author: George (Runtime Dev)
date: 2026-07-06
operation: Band Enforcement — Compiler Guarantee-Model Ruling (Model A vs B vs hybrid)
slice: D (reserved crux + runtime-enforceability reality)
status: Evidence finding for Frank's Phase-3 ruling — NOT a ruling. Stated as a hard yes/no per mandate.
brief: session-state/.../files/briefs/brief-D-george-runtime.md
---

# Hard finding (Q4)

**The runtime CANNOT reliably enforce a deferred internal declared band** (`min`/`max`/
`minlength`/`maxlength`/`mincount`/`maxcount`) — because a declared band has **no runtime
governance representation at all**, and even the rule/ensure governance path it would
share is unbuilt at HEAD.

## Why (primary-source spine)

1. **Bands are not a runtime constraint kind.** `ConstraintKind` (`src/Precept/Language/ConstraintKind.cs:9-24`)
   = `Invariant` (rule) + `StateResident`/`StateEntry`/`StateExit` (ensures) + `EventPrecondition`.
   No band kind. A band cannot become a `ConstraintDescriptor` — that type requires an
   `ExpressionText` + `Because` a band declaration lacks (`src/Precept/Runtime/SharedTypes.cs:42`).
2. **`ConstraintsFailed` covers rules + ensures only.** `docs/runtime/result-types.md:114,121`
   ("Covers ALL post-fire constraints: global rules, state ensures, AND event ensures"). One
   undifferentiated governance path; bands are on **none** of it (answers Q4a: the disposition
   surface has nothing to render for a deferred band).
3. **The §7.6 sweep evaluates only rule/ensure buckets.** `docs/runtime/evaluator.md:1326-1360`,
   `:1695-1698` ("Every `ConstraintDescriptor` appears in exactly one bucket").
4. **Band proof is compile-time-only.** `docs/compiler/proof-engine.md:107` — "Proof ledger does
   NOT cross the compile-runtime boundary — only `FaultSiteDescriptor` residue (defense-in-depth
   backstops) crosses into runtime." An internal-computed band violation is a **compile-time**
   `NumericOverflow` Error (`docs/compiler/diagnostic-system.md:163`; `Diagnostics.cs:697/1283/1295`
   all Error at HEAD).
5. **The only runtime residue is a defense-in-depth fault, framed as a compiler defect.**
   `FaultCode.OutOfRange` (`src/Precept/Language/FaultCode.cs:47-48`); `docs/runtime/fault-system.md:280,316`
   — "reachable only for out-of-contract data … a fault on contract data would indicate a
   proof-engine gap — a defect to fix, not a condition to design around."
6. **No band-deferral seam exists.** The only "deferred" seam in the evaluator is lazy collection
   materialization (`docs/runtime/evaluator.md:1280`, "DEFERRED — do NOT implement"). Unrelated.
7. **The commit pipeline is stubbed.** `Version.cs:81` (`Fire` → `UndefinedEvent()`), `Version.cs:85`
   (`Update` → `NotImplementedException`), `Precept.cs:158` (`Constraints` → `NotImplementedException`),
   `Evaluator.cs:46` ("TODO: implement Fire/Update once the executable model is designed").

## Bearing on the ruling

- **Undermines Model A.** Its "defer to runtime" escape hatch requires a runtime band-enforcement
  surface that is neither designed nor shipped, and that §0.7:266 ("there is no deferral") forbids.
  The governance-checkpoint view (`band-guarantee-boundary-analysis-2026-07-03.md:114`) *assumes*
  "Fire refuses the write"; the runtime canon **refutes** the premise.
- **Consistent with B / already-hybrid.** Shipped reality = compile-time prove-or-reject for bands;
  runtime governance for rules/ensures + §0.7:268 external-input ingress. That *is* the hybrid line.
- **Cost of choosing A anyway:** must first commission a runtime band-governance surface (lower
  bands → `Invariant` constraints, or add an ingress band-validation pass) AND amend §0.7:266.
  Owner-gated; out of scope for a design pass.

*Neutral on the final model choice. Frank rules in Phase 3; Shane ratifies. This is evidence, stated hard per the Slice-D mandate.*

---

---

### 2026-07-06: Proof-Engine Boundary Ruling — Revision 2 (re-derivation under two owner corrections)

**By:** Frank (Lead/Architect & Language Designer)
**To:** Shane (owner)
**Status:** Draft — pending Shane ratification. RECOMMENDATION for owner-gated surfaces (`docs/philosophy.md`, spec §0.6/§0.7); nothing edited.
**Branch:** spike/Precept-V2-Radical
**Verified-at-HEAD:** a6922fd829ad5c8a227fadfa2504a584b9e630a0
**Ruling:** `docs/Working/proof-engine-boundary-ruling-2026-07-06.md` (Revision 2; overwrites the canonical path)
**Supersedes:** `docs/Working/proof-engine-boundary-ruling-2026-07-06-v1-superseded.md` (v1 preserved; this is a full re-derivation, not an amendment)

---

## Revised headline

**The verdict did not move — Hybrid governs Precept, Model A and pure-B are both rejected — but the two corrections replaced two wrong pillars of the derivation, and the boundary rule is materially sharper.**

The compiler is **total over the fault surface**: every fault-prone operation over a *definition-derived* value (division, overflow-arithmetic, `sqrt`/`pow`, empty/index access, and containment of a *computed result* in a declared bound) is compile-time **prove-or-reject**, never deferred to a runtime fault-trap. The language is **not** limited to the decidable fragment (pure-B rejected): a declared constraint — **modifier or rule, one construct** (`spec:1135`/`:1138`) — is at once a runtime-**governed** obligation on the raw external values entering its fields (`:268`) and a compile-time **proof premise** (`:203`). **Governance is not fault-deferral** — the fault is proven complete at compile time and only its precondition is enforced at ingress (Composition, `:270`).

**One-line boundary (re-derived, syntax-blind):** *a raw external value at its own ingress slot is **governed**; any value a Precept expression has derived from it is **proven-or-rejected**; `max 100` and `rule x <= 100` get identical dispositions; **decidability decides whether a prove-or-reject obligation discharges or the definition is rejected — never whether it is proven or governed.***

## How each challenge landed

- **Challenge 1 (constraint = rule):** disposition is a property of the **obligation**, not the construct. Every constraint does both — governed-at-ingress (A.1) and proof-premise (A.2); prove-or-reject attaches to fault-prone **operations over derived values** (B), routed by origin (`:268` "before any computation derives"), never by spelling. Decidability is the **discharge oracle**, not the router — reconciling the anti-slide requirement with `:1138` ("proof participation is a function of decidability"). v1's "decidability is never a routing criterion" was the error; it's never the router, always the oracle.
- **Challenge 2 (no-deferrals was a process safeguard):** the `§0.7:266` "no deferral" clause was **redundant** — it restates Principles 7/10/11. Model A is rejected on `philosophy.md:57` + Principles 7/10/11 + the owner-**shipped** count-bound prove-or-reject (`c27a382b`) + the bug tracker classifying silent deferral as soundness holes (BUG-017/020/021) — all independent of the clause. `:266` demoted from pillar to corroboration; its **meaning flagged owner-gated** (R1, recommend "Reading N": no-deferral scopes to fault-proof only; governance is not deferral). One new door opened: an explicit author-visible trust construct is not Model A and is flagged as a future `/design` question (R4).

## Owner action items (ratification checklist — full detail in §10 of the ruling)

- Ratify **Hybrid** (total-over-faults + governed-carriers) and the **Obligation-Role Rule** (§4).
- **R1:** confirm the meaning of `§0.7:266` "no deferral" (recommend Reading N; do not edit).
- **R2 (optional):** adopt the syntax-explicit precision clause on `§0.7:268`.
- **R4 (new):** decide whether to open a `/design` question for an explicit trust construct.
- **R5:** confirm no edit to `docs/philosophy.md`.
- Confirm Q5/Q7 stay **Error**; approve the per-obligation PROVEN/GOVERNED surface (no DEFERRED state); confirm BUG-017/020/021 are the Phase-5 completion target.

## Provenance (updated)

`§0.7:266` is owner-committed (`5af46537`, sfalik) but its intended *meaning* is now owner-flagged → R1. The nearest owner **language** decision (count Reading A, `c27a382b`, "yes, unproven divisor") points the same way as the ruling. D2's premise ("non-carries-proof bands" distinct from rules) **does not survive** constraint = rule (`ConstraintKind.cs` has no band member). This ruling does not overturn an owner decision.

---

## 2026-07-09 — Revision 3 addendum (overflow-scope reconciliation)

**By:** Frank · **To:** Shane · **Status:** Draft — pending ratification. Recommend-only for owner-gated surfaces; nothing edited in philosophy/spec.
**Ruling:** `docs/Working/proof-engine-boundary-ruling-2026-07-06.md` (now Revision 3; edited **in place**, not a new file). Rev-2 snapshot preserved at session-state `files/ruling-v2-pre-overflow-revision.md`.
**Trigger:** owner directive — "we explicitly pushed overflow out of scope for now; research that, then revise the ruling" — plus the `TotalCostInvariant` stress-test that exposed the over-rejection.

### What changed (model verdict UNCHANGED — Hybrid + Obligation-Role Rule stand)

Only overflow's treatment *within* the model moved. **Representational** overflow (`NumericOverflow` — a computed value exceeding what the decimal/money/temporal **type itself** can hold) is carved OUT of the live family-B prove-or-reject set and reclassified as an **owner-parked, disclosed known gap** (D1, `decision-index.md:19`; plan §2 D1, `compiler-readiness-plan-2026-06-16.md:188`–`:199`): a defense-in-depth backstop with no live `[StaticallyPreventable]` guarantee; temporary; no doc may claim it prevented. The rest of the fault floor stays LIVE prove-or-reject: division-by-zero, `sqrt`/`pow` domain, empty/index access, and **declared-bound containment** (`OutOfRange`/assignment-range, `:697`/`spec:208`).

**The load-bearing distinction:** `NumericOverflow` (value-vs-**type-range**) ≠ `OutOfRange` (value-vs-**declared-min/max**) — two different faults (`overflow-prevention-design-analysis.md:516`). The fault surface is now explicitly **two-tier**: live-enforced faults vs the parked overflow lane.

### Edits made in place

- **§1 executive ruling & §4.B Obligation-Role Rule** — fault lists made two-tier; representational overflow carved out; `NumericOverflow`≠`OutOfRange` stated explicitly.
- **Worked examples** — row 3 & Q6 de-conflated (`set X=150` on `max 100` is `OutOfRange`, not `NumericOverflow`); rows 6/7/10/12 updated (representational-overflow reject removed; declared-bound containment stays live). **Row 10 (`TotalCost`)**: under overflow-parked it **compiles clean** once fields are initialized (invariant governed; product overflow parked; result-`max` doesn't back-propagate). Added a table scope-note: examples assume field-initialization satisfied (the row-10 `RequiredFieldsNeedInitialEvent` structural-first finding).
- **Q5/Q7/Q11 + D1 adjacency** — reconciled: representational overflow out of the live set; Q11 taxonomy gains a "parked overflow lane" row and `RequiredFieldsNeedInitialEvent` in definition-incoherence; the Rev-2 "unresolved D1 clash / BUG-017 debt" flag RESOLVED (BUG-017 is `OutOfRange`, live — not the overflow question).
- **§0.1 principle appendix** — Principles 7/10/11 rows updated to the disclosed-gap position.

### New owner action item

- **R6 (new):** reconcile `philosophy.md:53` + spec Principles 10/11 (`:110`/`:112`) — which still list overflow as prevented — with D1's park. This is a **disclosed, surfaced-only (DoD-9)** gap; owner-gated; enumeration via GATE-F (`decision-index.md:46`). **Recommend-only — did NOT edit philosophy or spec.** GATE-O (∞/NaN, integer-conversion overflow) remains pending owner disposition.

**Net:** the ruling now honestly matches the owner's overflow-out-of-scope decision without weakening the Hybrid model. Status stays **Draft — pending Shane ratification.**

---

## Coordinator Policy

---

### 2026-07-10T03-52-46: Policy — Reviewer agents must be spawned as full subagents with inlined charter, never as lean Explore agents
**By:** squad-coordinator
**What:** Policy — Reviewer agents must be spawned as full subagents with inlined charter, never as lean Explore agents
**References:** CONTRIBUTING.md, .github/agents/squad.agent.md, docs/scenarios/upgrading.md (bradygaster/squad)
**Why:** **Context:** Discovered during the v0.9.4 → v0.11.0 squad upgrade audit that this repo's `.github/agents/squad.agent.md` contains a "Review Spawning — Full Subagents with Charter Context" section with no backing decision record. Per official Squad guidance (docs/scenarios/upgrading.md): "Don't customize squad.agent.md... if you need custom behavior, use directives in decisions.md instead." This decision closes that gap so the policy survives future upgrades even if the corresponding squad.agent.md section is silently overwritten.

**Policy:**
When the user asks the team to review a PR, reviewer agents (e.g. Frank, Soup Nazi) MUST be spawned as full `runSubagent`/`task` calls with the reviewer's charter.md inlined into the spawn prompt — never as lean Explore agents. Explore agents lack domain expertise, charter identity, and gate-enforcement authority needed for a thorough, gate-aware verdict.

**Required spawn pattern per reviewer:**
1. Read the reviewer's charter (`{team_root}/.squad/agents/{name}/charter.md`) and inline it into the spawn prompt.
2. Include the reviewer's identity block (name, role, expertise, style).
3. Include the linked issue's acceptance criteria for the tester/AC-gate reviewer — instruct them to read the issue directly via GitHub tools.
4. Specify review criteria by role: Lead/Architect (Frank) — doc accuracy vs. implementation, diagnostic message correctness, grammar sync, completions/hover, dead code scan. Tester (Soup Nazi) — AC-to-test matrix, spot-check test quality, disabled test scan.
5. Require structured output: `APPROVED` or `BLOCKED` with numbered findings (`B{N}:` / `G{N}:`).
6. Spawn reviewers in parallel — no data dependency between them.

**Rule:** Do NOT use a generic Explore agent for reviews under any circumstance.

**Remediation:** `.github/agents/squad.agent.md` § "Review Spawning — Full Subagents with Charter Context" is the applied instantiation of this decision. If a future upgrade overwrites that section, re-derive it from this record.

---

### 2026-07-10T09-19-58-04:00: Policy — Squad customization is directive-first, minimum-patch; only hard coordinator invariants stay as direct `squad.agent.md` patches
**By:** Frank
**What:** Policy — Squad customization is directive-first, minimum-patch; only hard coordinator invariants stay as direct `squad.agent.md` patches
**References:** `.github/agents/squad.agent.md`, `.squad/decisions.md`, `.github/skills/coordinator-source-of-truth/SKILL.md`, `docs/scenarios/upgrading.md` (bradygaster/squad)
**Why:** **Context:** The v0.9.4 → v0.11.0 upgrade audit found repo-local `squad.agent.md` customizations with no durable classification. Upstream guidance says to prefer directives/skills over direct `squad.agent.md` edits, but this repo also has a small set of repo-specific coordinator rules that must stay live in the governance file itself or the coordinator will immediately route/gate incorrectly after an overwrite.

**Policy:**
- Default to **directive-first, minimum-patch** customization. If a local behavior can live canonically in `.squad/decisions.md` or a dedicated skill, put it there and treat any `squad.agent.md` copy as a convenience restatement.
- Reserve direct `squad.agent.md` patches for **HARD-INVARIANT** sections only: repo-specific coordinator text that must remain in the live governance file because a post-hoc decision record cannot correct the coordinator after the file is overwritten. The current hard-invariant set is:
  1. **`@copilot` retired/disabled lane** — routing, roster, and casting text must all say the lane is unavailable in this repo.
  2. **Implementation Gate — Draft PR Required** — the coordinator must read and enforce the draft-PR gate directly from its live governance file.
- Treat these as **DIRECTIVE-BACKED** local customizations instead of hard invariants:
  1. **Review Spawning — Full Subagents with Charter Context**
  2. **Coordinator Restraint Rules**
- Marker convention in `.github/agents/squad.agent.md`:
  - `<!-- PRECEPT-SQUAD: HARD-INVARIANT START ... -->` / `END`
  - `<!-- PRECEPT-SQUAD: DIRECTIVE-BACKED START ... -->` / `END`

**Rationale:** This keeps the live coordinator patch surface as small as possible while preserving the few repo-specific rules that cannot safely rely on a later recovery pass.

**Alternatives considered and rejected:**
1. **Keep every customization as a direct `squad.agent.md` patch:** rejected — maximizes upgrade drift and recreates the overwrite problem on every `squad upgrade`.
2. **Move every customization out of `squad.agent.md` immediately:** rejected — live routing/gating/roster semantics would still be wrong the moment an upgrade overwrote the coordinator file.
3. **Leave customizations unmarked:** rejected — future audits would have no durable way to distinguish must-reapply invariants from convenience restatements.

**Precedent:** Matches official Squad upgrade guidance to prefer directives/skills for local behavior, while preserving the repo's existing pattern of backing surviving `squad.agent.md` customizations with a durable decision record.

**Tradeoff accepted:** A small direct-patch surface remains and must still be re-verified on upgrades, but the required reapply set is now explicit, minimal, and auditable.

**Remediation:** If a future upgrade overwrites `squad.agent.md`, reapply HARD-INVARIANT blocks first; regenerate or omit DIRECTIVE-BACKED blocks from the canonical decisions/skills as needed.

---

### 2026-07-10T04-07-22: Backport Coordinator Restraint Rules as a `squad.agent.md` customization (upstream never shipped the code)
**By:** squad-coordinator
**What:** Backport Coordinator Restraint Rules as a `squad.agent.md` customization (upstream never shipped the code)
**References:** upstream issue #587, PR #683, PR #859, PR #953, `.github/agents/squad.agent.md`
**Why:** **Decision:** Add a "Coordinator Restraint Rules" section to `.github/agents/squad.agent.md` as a deliberate local customization, since this feature never shipped upstream in `squad-cli`.

**Why (research trail):**
- Upstream issue #587 (bradygaster/squad, filed 2026-03-24 by the repo owner) proposed 6 restraint rules to stop the coordinator from over-narrating agent output, re-explaining context, or spawning unsolicited follow-ups.
- Code PR #683 implemented it; got bundled into batch PR #859 (2026-04-05) alongside unrelated fixes (compaction recovery, result persistence, and an unrelated `squad watch`/`triage` CLI flag fix).
- PR #859 was closed unmerged on 2026-04-10 — blocked by a missing-test-coverage flag on the unrelated bundled CLI flag fix, not on the restraint-rules content itself.
- PR #953 (docs-only) merged the next day, 2026-04-11, publishing `docs/reference/coordinator-restraint.md` describing the 6 rules as if shipped.
- Issue #587 was closed "completed" on 2026-04-18 with no linked commit — the feature was never actually merged into `squad.agent.md.template`. Confirmed absent from `squad-cli` as of the current upgrade audit.
- No open upstream issue currently reports this specific docs/code desync, though other issues report the same class of bug (docs/CHANGELOG claiming shipped features that were never wired into install manifests or runtime).

**Reconciliation with existing behavior:** Rule 5 ("no follow-up agents unless mandated") must not override this repo's existing Parallel Fan-Out / Eager Execution Philosophy. The local backport therefore forbids only additional, undeclared follow-up spawns layered on after the fact; it does not block already-declared proactive chaining.

**Alternatives considered and rejected:**
1. **Do nothing / wait for upstream to ship it:** rejected — no upstream work is in flight and the feature was already incorrectly treated as shipped.
2. **Copy the upstream docs wording verbatim:** rejected — that would silently contradict this repo's declared eager fan-out behavior.

**Precedent:** Follows the same repo pattern as other `squad.agent.md` customizations: apply the local section, then back it with a durable decision record so the behavior survives future upgrades.

**Tradeoff accepted:** This remains a local customization until upstream actually ships the code, so it must be re-verified during future Squad upgrades.
