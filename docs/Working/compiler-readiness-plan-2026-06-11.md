---
status: Active
authored: 2026-06-11
companion-research: research/architecture/compiler/fault-floor-definition-2026-06-11.md; docs/Working/compile-time-niche-decision-packet-2026-06-10.md
appendices: docs/Working/compiler-readiness-plan-2026-06-11-appendices/ (fault-floor-research-full, spec-coverage-audit [619 rows], working-docs-triage [43-obligation register])
scope-gate: blocks runtime implementation — when this plan completes, the compiler is at 100% of the canonical spec with a sound, right-sized fault floor, and runtime work can begin
---

# Compiler Readiness Plan — 2026-06-11

> **A fresh plan, replacing `compiler-readiness-plan-2026-05-24.md`.** Goal: take the Precept compiler to **100% of the canonical language spec** — all datatypes, all grammar, all constructs, all diagnostics, per the canonical designs — on top of a **sound, right-sized fault floor**. The floor is made total *and* trimmed of over-reach (band-containment prove-or-reject removed); then the language is widened against a floor that holds; then we are **ready to begin runtime work**. The flag-sound universal "niche" analysis layer is explicitly **post-runtime** and is not planned here.

## Context

Two strategic decisions reset the prior plan. (1) The **compile-time niche decision** (`compile-time-niche-decision-packet-2026-06-10.md`) established: the fault floor is **prove-or-reject** (total, stable); everything beyond it is **flag-sound, and revisited after the runtime**. (2) The **fault-floor research** (`fault-floor-definition-2026-06-11.md`) adjudicated the registry and found the engine both **over-stepped** the floor (the band-containment lane — OutOfRange/LengthBound/CountBound + the PRE0078 declared-band check — is rule-preservation misclassified as faults) and **under-built** it (representability-overflow has no obligation anywhere; 10 obligation-position fail-open holes; several false-"Proved" sites). This plan closes the gaps, removes the over-reach as one coordinated move, completes the spec, and ends runtime-ready. Inputs feeding it: 16 floor gaps, 8 scale-backs, 10 registry corrections, **236 spec-coverage gaps** (619-row audit), and **43 mined obligations** recovered from the 28 superseded docs so nothing is lost.

## Phase summary

| Phase | Goal | Covers | Gates | Effort | Detail |
|---|---|---|---|---|---|
| **Phase 0 — Decision & Probe Gate** | Clear every owner decision and probe-adjudicate the 3 open floor items so no downstream phase builds on an unsettled premise or stalls mid-build waiting on an o | All 9 open owner-decisions surfaced as gates; the 3 probe-first floor items (temporal-over | This phase IS the gate-resolution phase; self-gates only on owner availability. Tier-3 spe | S (3-6 days, mostly owner conversations + probes) | heavyweight |
| **Phase 1 — The Coordinated Floor Move** | Land the DesugarsToRule->governance/ingress wiring (floor prerequisite), the band scale-back (non-proof-carrying fields only), the WHOLE registry trim+repartiti | All 8 scale-backs; all 10 registry corrections (whole, not split); DesugarsToRule wiring;  | GATE-A numeric representation; GATE-D PRE0101; GATE-E mincount-at-birth; GATE-F spec/philo | L (8-13 days) | heavyweight |
| **Phase 2 — Position-Totality + Creation-Exhaustiveness Checker** | Make floor completeness STRUCTURAL: catalog ChildExpressionPositions projection over the 5 expression-bearing DUs + a 3-layer build-time creation-exhaustiveness | Floor gap: obligation-position incompleteness (BUG-031/032); intra-guard conjunct narrowin | None blocking — depends on Phase 1's honest registry so the checker asserts against real c | L (8-12 days) | stub |
| **Phase 3 — False-Proved Repairs + Missing Floor Obligations** | Close every green-ledger-over-a-guaranteed-fault hole and add the floor's missing obligation families so the floor is COMPLETE, not just position-total. | Write-aware tier-2 discharge; representability-overflow family at every arithmetic sink (i | GATE-A (representability shape); GATE-B (write-awareness shape); GATE-C (qualifier identit | L (10-15 days) | stub |
| **Phase 4 — Spec Completeness: Lexer + Primitives + Choice + Business + Temporal Datatypes** | Reach 100% canon for the no-floor-entanglement datatype/literal surface: lexer/literals, primitives/choice, business-domain (money/currency/quantity/unit/dimens | Spec gaps: Lexer (6) + Primitives/choice (22) + Business-domain (28) + Temporal (11) = 67; | GATE-J (sqrt §3.2-vs-§3.7 + choice field-vs-field canon contradictions); GATE-K (log-unit  | L (12-18 days, parallelizable across sub-areas) | stub |
| **Phase 5 — Spec Completeness: Collections + Modifiers + Expression Language + Rules/States/Binder/Graph** | Reach 100% canon for collections, the modifier matrix (incl. field-reference bound enforcement), the expression language, rule/ensure scoping forms, states/tran | Spec gaps: Collections (22) + Modifiers (11) + Expression (23) + Rules/ensures (18) + Stat | GATE-M (PRE0141 bound-expression ownership — decide via the 2c-ii interaction; feeds Phase | L (14-20 days) | stub |
| **Phase 6 — Diagnostic + Pipeline-Tail Completeness + Emission Ownership** | Close proof-engine/diagnostic + pipeline-tail/header/parser-recovery gaps; emit-or-retire every declared DiagnosticCode; verify every [StaticallyPreventable] co | Spec gaps: Proof-engine+diagnostic (28) + gap-fill pipeline-tail/header/parser-recovery/Co | GATE-M (from Phase 5) feeds Slice 3c; BUG-022 exemption-model /design; merge-the-three-len | L (12-18 days) | stub |
| **Phase 7 — Conformance, API Solidity + Polish + Runtime-Ready Gate** | Stand up the conformance program (ONE register), solidify the API surface as the runtime-build precursor, burn down polish, settle the floor->runtime reconcilia | Conformance-suite program (Precept.Conformance.Tests, three-edge coverage, fence annotatio | GATE-I (fault-delivery, settle if not already); GATE-N (definition-evolution lane confirm- | M (8-12 days) | stub |

## Decisions captured

- The fault floor is the 11-obligation-family, position-total, prove-or-reject core (divisor!=0; sqrt/threshold>=0; presence; collection-nonempty-on-access; collection-nonempty-on-mutation; index-in-bounds; key-presence; operation-site qualifier+dimensional compatibility incl. correct UCUM factors; decimal-representability overflow; temporal representability; computed-arg function computability) PLUS the static edge owed by binder/type-checker totality (TypeMismatch/UndeclaredField/InvalidMemberAccess/FunctionArityMismatch as defense-in-depth traps, no proof obligation owed).
- Three soundness disciplines are part of the minimum discharge power, not optional: sequential staleness (ReassignedBefore/CountInvalidatedBefore), write-aware tier-2 discharge, and intra-guard short-circuit conjunct narrowing for presence.
- DesugarsToRule->runtime constraint-plan + ingress wiring is PROMOTED from scale-back ordering constraint to a floor PREREQUISITE: committed-state tier-2 discharge of true faults is unsound without runtime enforcement of the discharging constraints; it has ZERO consumers today, so retiring compile band gates before it leaves bands enforced nowhere.
- The band-containment lane (OutOfRange/Length/Count + PRE0078-band) EXITS the floor to governance + flag-sound warnings — but only for NON-PROOF-CARRYING fields. The carries-proof boundary (not band-vs-not) is the real line: mincount's count>0 discharge, sign-modifier/rule-fact preservation, and bounds consumed by representability proofs do NOT exit.
- Position-totality is itself a floor property AND a HARD runtime precondition: the per-keystroke InspectFire path runs guards with no fault lane in Prospect (Certain/Possible/Impossible), so a fail-open obligation position crashes the inspector. Durability is guaranteed structurally by the catalog ChildExpressionPositions projection + a 3-layer creation-exhaustiveness checker — not by spot fixes.
- Three categories, not two: fault floor (prove-or-reject) / definition incoherence (PRE0079-on-default, PRE0164, PRE0115 — always-Error, every Create fails, neither fault nor flag) / flag layer (post-runtime, warning-default with earned per-diagnostic error promotion).
- Registry repartition lands WHOLE in one atomic move: REMOVE OutOfRange/Length/Count FaultCodes; RETARGET NumericOverflow to the new representability obligation; ADD IndexOutOfBounds (route PRE0100) / KeyNotFound (route PRE0099) / TemporalOverflow + the UnexpectedNull bijective row (route PRE0116); PRE0101->DuplicateKey only under require-absence; RETIRE the generic _ => DivisionByZero backstop for per-family honest routing; add ProofStrategy.Vacuous.
- The flag-sound universal niche layer (Layer 2 contradiction/vacuity/dead-guard/clock-decay/strand + Layer 3 contract projection/counterfactuals) is explicitly POST-RUNTIME and OUT OF SCOPE; the runtime needs nothing from it while it needs the shipped runtime as its error-promotion oracle (the live PRE0155 false positive proves premature promotion actively rejects correct precepts).
- The definition-evolution deploy lane (fleet stranding, restore-time skew) is the most severe uncovered family and is explicitly its own separate decision — confirmed OUT of floor scope; folding it in would blur the teachable line.
- Convergent evidence: two independent fault adjudications + red-team + critic agree on 5 true faults, 4 type-checker-owned, 3 rule-misclassified, 2 mixed; the evaluator's own impossible-path table independently lists exactly the true-fault/TC-owned codes and omits the contested ones.
- Standing declines recorded: convex polyhedra and predicate abstraction are permanently off the interactive path (data-dependent latency breaks determinism R2; theorem-prover step breaks R1); solver-free scope lock per spec §0.6 #3.
- Forwarding-fact vacuous suppression is verified SOUND this session (GraphAnalyzer.BuildEdges ignores row.Guard, so reachability over-approximates and unreachable=structural) — ProofStrategy.Vacuous records it honestly.

## Open decisions — surfaced as upstream gates (resolved in Phase 0)

Every gate is listed here and re-cited at the phase it blocks. Recommendations are from the fault-floor research; the owner decides.

### GATE-A — Numeric representation (gates the representability-overflow obligation family)

- **Options:** (i) fixed decimal + obligation-driven bounds demand + prove-or-reject representability at every arithmetic sink (reuses IntervalOf/IntervalTransfer); honest friction: unbounded A+B rejects, not just mul/pow chains. (ii) arbitrary-precision numerics, which DELETES the NumericOverflow fault class and most reinstated write-time checking.
- **Gates:** Phase 1 (registry RETARGET of NumericOverflow) and Phase 3 (the representability obligation shape). Surfaced in Phase 0.
- **Recommendation:** Owner call — a genuinely-open runtime-value-representation decision (runtime is stubbed/choosable). The fault-floor research treats it as load-bearing enough that Phase 3 cannot start without it; surface the (i) friction statement honestly (unbounded A+B rejects). No recommendation carried per neutral-framing discipline.

### GATE-B — Write-awareness repair shape for tier-2 discharge

- **Options:** (i) preservation obligations on writes into proof-carrying constraints (the PRE0078 mechanism re-aimed at constraints that actually discharge); (ii) staleness-invalidation of declaration/rule facts + forward re-establishment from the written expression's interval (the discipline the collection lane already implements). Per-lane mixes possible.
- **Gates:** Phase 3 (write-aware tier-2 build). Surfaced in Phase 0. Real authoring-ergonomics consequences either way.
- **Recommendation:** Owner call. Note the collection lane ALREADY implements (ii), so (ii) reuses existing machinery and argues for consistency; (i) reuses the PRE0078 mechanism being re-aimed in Phase 1. Frame neutrally.

### GATE-C — Qualifier assignment-lane taxonomy

- **Options:** (i) PRE0141 + the qualifier-source obligation as faults (QualifierMismatch identity); (ii) a named type-identity-integrity third category. Behavior consensus across all passes: KEEP both rejections and BUILD the qualifier-source obligation; only the LABEL is open.
- **Gates:** Phase 1 (registry routing of PRE0114/0141) and Phase 3 (qualifier-source obligation identity). Build proceeds under either label; only the label defers.
- **Recommendation:** Behavior is settled (keep + build); the label is the only open item. Copy A's keep was conditional on the rename — present accurately; build the obligation regardless of the label so it does not block.

### GATE-D — PRE0101 duplicate-key semantics

- **Options:** (i) require-absence add => can't-compute => DuplicateKey fault code (stays in fault lane); (ii) put/overwrite or recoverable-refusal => uniqueness RULE => governance (exits the fault lane). The current collapse to CollectionEmptyOnMutation is wrong under EITHER reading.
- **Gates:** Phase 1 (registry: PRE0101 gets DuplicateKey only under (i)). Probe-adjudicate in Phase 0.
- **Recommendation:** Owner call. Keep the rejection conservatively until adjudicated; fix the false-emptiness collapse regardless of outcome.

### GATE-E — Mincount-at-birth Create semantics (forced by the count reclassification)

- **Options:** (i) birth exemption; (ii) deferred first-mutation check; (iii) defined every-Create-fails refusal + compile flag. The reclassification forces this; nobody has made it.
- **Gates:** Phase 1 (count scale-back can't land cleanly without it) and Phase 3 (mincount-at-birth false-Proved seed). No interim state may leave the ledger lying.
- **Recommendation:** Owner call. Note mincount RETAINS a proof-carrying count>0 discharge role, so its Strategy-2 arm must survive the move regardless of the birth-semantics choice.

### GATE-F — Spec/philosophy canon amendments (owner approval required; never bundled silently)

- **Options:** §0.7's 'no result outside a declared bound' clause; Principle 11's 'constraint range impossibility' fault class; §0.6 item 6 -> proven-violation-only; the §0.7 discharge-tier sentence (per GATE-G); plus the OWNER-GATED philosophy.md absolute-claims reconciliation ('No errors. No bugs.' / 'compile-time impossibilities').
- **Gates:** Phase 1 (the coordinated move CANNOT be canon while the spec asserts the opposite). Tier-3 consultation in Phase 0; philosophy/spec edit gate per CLAUDE.md.
- **Recommendation:** Owner deliberation required — these are guarantee-contract changes. Surface each amendment explicitly with verbatim prior-decision quotes; do NOT auto-edit philosophy.md (rides the guarantee-statement promotion separately).

### GATE-G — Min-bound discharge vacuity (§0.7 promises a declared modifier/rule as a discharge tier, but min 1.0 discharges nothing)

- **Options:** (i) make DeclarationValue satisfactions resolvable — which makes those bounds proof-carrying with write-awareness consequences; (ii) amend the §0.7 discharge-tier sentence (folds into GATE-F).
- **Gates:** Phase 3 (discharge-power completeness); Phase 1 if resolved by spec amendment.
- **Recommendation:** Owner call. (i) widens proof power but pulls bounds into the carries-proof set; (ii) is a spec narrowing. Frame neutrally.

### GATE-H — Taxonomy adoption (the three-category model)

- **Options:** (i) fault floor / definition incoherence / flag layer (both derivations converged on this); (ii) fold definition-incoherence Errors into the flag layer as pre-earned promotions (recon's frame).
- **Gates:** Phase 1 (the identity reframe of PRE0079-on-default/PRE0164/PRE0115). Confirm in Phase 0.
- **Recommendation:** Both derivations converged on (i); (ii) contradicts the earned-promotion oracle rule (an always-on Error 'earned' before the runtime oracle exists violates the flag contract). Surface for confirmation; recommend (i).

### GATE-I — Fault-delivery contract (Q1)

- **Options:** result-types.md:51 'faults throw FaultException' vs evaluator.md §7.6 'faults are never thrown — EventOutcome.Faulted'. Must be settled before the evaluator lands either shape.
- **Gates:** Phase 1 (registry routing benefits from the delivery shape); confirmed at the Phase 7 runtime-ready gate at the latest. A design item between floor and runtime — specify in Phase 0.
- **Recommendation:** Owner call — settle before the runtime build. Note Restore mostly dissolves per spec :272 (next-operation re-governance + traps as out-of-contract backstop).

### GATE-J — sqrt §3.2-vs-§3.7 internal canon contradiction (+ choice field-vs-field contradiction)

- **Options:** sqrt: §3.2 says integer widens to number in any context (impl follows); §3.7 says integer input is a type error. Likely drop 'integer' from §3.7 vs add explicit integer-arg rejection. Choice: primitive-types says field-vs-field is always error; §3.6 allows it under same-element-type + order-preserving subsequence; impl satisfies neither (accepts disjoint sets and mismatched element types — a soundness-grade false-clean).
- **Gates:** Phase 4 (primitives+choice completeness). Doc-vs-doc contradictions, not new surface.
- **Recommendation:** Owner picks the canonical reading; the impl follows. Tier-1/light Tier-2.

### GATE-K — Log-unit (dB/Np/B) cross-unit policy ruling

- **Options:** allow-with-what vs restrict. UCUM log function stripped from catalog, no multiplicative factor exists, the field's libraries forbid multiplying log units. ScaleIsRational=false guard shipped as interim floor.
- **Gates:** Phase 4 (business-domain completeness). Needs a small /design ruling BEFORE that slice executes.
- **Recommendation:** Owner call via small /design. Interim soundness floor already holds, so this is unblocking, not urgent.

### GATE-L — Phase 7 Slice 4 absolute-measurement positions (absolute temperature/dBm/pH point type)

- **Options:** Build the 'instant' analog for units (a point type distinct from quantity amounts) vs continue deferral. NEW language surface.
- **Gates:** Phase 4 (business-domain), but recommended DEFERRED. Full Tier-2 owner-consultation lifecycle when taken up.
- **Recommendation:** Defer — edge-case business-wise, low priority, parked at the collection design's lock. Carry as an explicit deferral, not a Phase-4 work item.

### GATE-M — PRE0141 bound-expression ownership (gates Phase 6 Slice 3c ownership analyzer)

- **Options:** walk bounds / split the code / accept carve-out / static-only bounds. 2c-ii's bounds-desugar-to-rules (Phase 5) will likely resolve it as a side effect.
- **Gates:** Resolved in Phase 5 (2c-ii), consumed in Phase 6 (Slice 3c). Decide the 2c-ii interaction rather than independently.
- **Recommendation:** Decide the 2c-ii interaction rather than independently; 2c-ii's desugar likely makes PRE0141 a side-effect resolution. Confirm at Phase 5 start.

### GATE-N — Definition-evolution deploy lane

- **Options:** Confirm it stays OUT of this plan's floor scope (its own separate decision — fleet stranding, restore-time skew, the most severe uncovered family) vs fold in (rejected by both derivations).
- **Gates:** Phase 7 (runtime-ready gate) — a confirm-not-build gate.
- **Recommendation:** Confirm OUT of floor scope per the niche packet; schedule its own decision before first external users. Folding it in would blur the teachable line.

## Heavyweight phase blocks (current + next)

### Phase 0 — Decision & Probe Gate

**Goal:** Clear every owner decision and probe-adjudicate the 3 open floor items so no downstream phase is blocked or building on an unsettled premise. Deliberately front-loaded as its own short phase because the decisions gate everything: numeric representation gates the representability family; taxonomy gates the registry reframe; mincount semantics gates the count scale-back; spec amendments gate the coordinated move; fault-delivery gates the runtime. Almost entirely conversation + probes + design reconciliation.

**Dependencies:** None upstream; this phase exists to remove downstream blockers. Owner availability is the only constraint.

**Effort:** S (3-6 days)

#### Slice 1 — Probe-adjudicate the 3 open floor items (read-before-asserting)

- **Work:** Run verify-then-build probes via precept_compile: (a) temporal-arithmetic overflow on date/datetime +/- out-of-range period (NodaTime behavior) -> confirm the gap before building any obligation; (b) omit x global-rule join -> adjudicate whether it is a presence-lane obligation-creation join (floor-shaped, folds into Phase 2's position walk) or flag-shaped (moves PRE0082 hook-guard coverage to the flag layer — surface explicitly, no silent reclassification); (c) PRE0101 -> confirm the current CollectionEmptyOnMutation collapse and frame the require-absence-vs-put semantics question. Record each as an explicit disposition row with verbatim probe input+output.
- **Exit criteria:** Three disposition rows written with verbatim probe evidence; temporal-overflow gap confirmed-or-refuted; omit-join classified floor/flag; PRE0101 semantics question framed for the owner. No obligation built yet.
- **Doc-touch:** None canonical yet — dispositions recorded in PR body / working scratch. If temporal overflow confirmed, note the future docs/compiler/proof-engine.md + docs/runtime/fault-system.md obligation.

#### Slice 2 — Surface the owner decisions as gates (conversational, one at a time, neutrally framed)

- **Work:** Walk the owner through each gate in plain text, one at a time, no carried recommendation where the research declines to carry one: GATE-A numeric representation; GATE-B write-awareness shape; GATE-C qualifier taxonomy label; GATE-D PRE0101; GATE-E mincount-at-birth; GATE-H taxonomy adoption; GATE-G min-bound vacuity; and pre-flag the GATE-F spec-amendment set. For each: name the gap, cite the canonical-doc area checked, capture the owner's direction. GATE-A and GATE-H touch the guarantee contract — surface the philosophy-gap honestly per CLAUDE.md. For the GATE-F Tier-3 spec conflicts, quote the prior locked decisions verbatim (§0.7 'no result outside a declared bound', Principle 11 'constraint range impossibility', §0.6 item 6).
- **Exit criteria:** Each gate has a recorded owner direction OR an explicit 'defer to Phase N start' with reason; the spec-amendment set is acknowledged as owner-gated and queued for Phase 1 approval; consultation evidence (checked / found / authorized) is visible in the conversation record; philosophy.md reconciliation recorded as owner-gated, never auto-edited.
- **Doc-touch:** No canon edits in this slice (spec amendments only APPROVED here, applied in Phase 1).

#### Slice 3 — Reconcile the between-floor-and-runtime design items + record runtime-build constraints

- **Work:** Specify GATE-I fault-delivery contract (FaultException vs EventOutcome.Faulted) as a settled design note so the runtime can build either shape; confirm Restore substantially dissolves per spec :272 (next-operation re-governance + traps as out-of-contract backstop) with the definition-evolution residue routed to the separate deploy-lane decision (GATE-N preview); record OD-1 (runtime evaluator + compile-time ConstantFold share ONE expression-evaluation core — cheap now, expensive to retrofit) as a runtime-build constraint; record the standing declines (convex polyhedra + predicate abstraction permanently off-path).
- **Exit criteria:** Fault-delivery contract settled in a design note; Restore residue confirmed routed out; OD-1 recorded as a Phase-7/runtime precondition; standing-declines paragraph drafted for the plan; the canonical home for the compile/runtime division of labor identified (today assembled from 7+ locations).
- **Doc-touch:** docs/runtime/fault-system.md + evaluator.md §7.6 + result-types.md:51 reconciliation QUEUED (applied when the contract lands); spec §3A.2:1946 Restore-contradiction fix QUEUED; D9 injectable-clock requirement recorded for evaluator.md (queued to Phase 4).

### Phase 1 — The Coordinated Floor Move

**Goal:** Land the DesugarsToRule->governance/ingress wiring, the band scale-back, the WHOLE registry trim+repartition, and the owner-approved spec amendments as ONE atomic, internally-consistent change so there is never a window where declared bounds are enforced nowhere and never a ledger that lies. This both right-sizes the floor (removes over-reach) and supplies the prerequisite (runtime enforcement of discharging constraints) that makes committed-state tier-2 discharge sound. The registry lands WHOLE here (not split with Phase 2) so the Phase-2 checker asserts against honest codes.

**Dependencies:** Phase 0 cleared GATE-A/D/E/F/G/H/I and the 3 probes. WITHIN this phase: Slice 2 (wiring) MUST precede Slice 3 (band retirement) — no window where bounds are enforced nowhere. Slice 4 (registry) depends on Slice 1's GATE answers (PRE0101 routing, taxonomy) and Slice 3's reclassification. Slice 5 (spec) depends on GATE-F approval and must land in the same coordinated move so canon never asserts the opposite of the shipped behavior. The whole phase is one logical atomic change delivered as ordered slices behind a single coordinated merge.

**Effort:** L (8-13 days). Heavy because it is a soundness-and-canon coordinated move touching the proof engine, runtime governance wiring, the fault registry, and the spec simultaneously.

#### Slice 1 — Stage the gated answers + adjudicate the omit-join disposition into the floor enumeration

- **Work:** Translate the Phase-0 owner answers into concrete routing decisions for this phase (PRE0101->DuplicateKey or exit; taxonomy label for qualifier; mincount-at-birth posture; which §0.7/Principle-11/§0.6 amendments are authorized). Fold the omit x global-rule join disposition (if Phase-0 adjudicated it floor-shaped, queue it for Phase 2's position walk; if flag-shaped, record the PRE0082 reclassification explicitly). No code or spec edit yet — this slice resolves the routing table the rest of the phase executes against.
- **Exit criteria:** A single routing table records, for each affected code, its post-move identity (fault / definition-incoherence / flag / governance) and its FaultCode (if any); omit-join has an explicit floor/non-floor row; no spec edit made yet.
- **Doc-touch:** None yet (routing table in PR body).

#### Slice 2 — DesugarsToRule -> runtime constraint-plan + ingress wiring (the floor prerequisite)

- **Work:** Wire modifier/bound desugar into the constraint plan and ingress so the designed TypedRule/TypedEnsure sweep actually enforces sign modifiers and (retained, proof-carrying) bounds on every commit/ingress lane. This has ZERO consumers today — bounds/modifiers are never synthesized into TypedRules. Land this BEFORE any compile-band gate is retired so enforcement never disappears (still double-enforced this slice).
- **Exit criteria:** A declared sign modifier / proof-carrying bound is enforced at the runtime governance/ingress lane (test-proved); the prerequisite that makes committed-state tier-2 discharge sound is in place; no compile-band gate retired yet.
- **Doc-touch:** docs/runtime/evaluator.md (constraint-plan + ingress enforcement); docs/compiler/proof-engine.md (DesugarsToRule consumer); docs/compiler-and-runtime-design.md (pipeline).

#### Slice 3 — Band scale-back for NON-PROOF-CARRYING fields (PRE0078-band / Length / Count)

- **Work:** Reclassify PRE0078 declared-band IntervalContainment, PRE0135 LengthContainment, PRE0136 CountContainment to flag-sound proven-violation warning + governance sweep — FOR NON-PROOF-CARRYING FIELDS ONLY. Preserve: mincount's count>0 discharge (Strategy-2 mincount arm survives explicitly), sign-modifier/rule-fact preservation, and bounds consumed by future representability proofs. Split/rename PRE0078 so the 'representable range' message attaches only to the new representability obligation (built Phase 3). Dissolve BUG-017 band-half / BUG-018 / BUG-021-as-floor (BUG-021 min-only containment re-files into Phase 3). Fix the PRE0135 '?' placeholder.
- **Exit criteria:** Add-to-full / out-of-band-write on a non-proof-carrying field compiles clean (governance refuses recoverably, now actually enforced via Slice 2); proof-carrying fields still prove-or-reject; mincount count>0 discharge intact; no message claims 'exceeded the representable range' for a band check.
- **Doc-touch:** docs/compiler/proof-engine.md (rewrite the no-deferral contracts; Strategy rows); docs/compiler/diagnostic-system.md (PRE0078/0135/0136 reclassification, severity, message).

#### Slice 4 — Registry trim + repartition (WHOLE, one pass) + ledger-honesty rider

- **Work:** Execute the entire registryCorrections list as one pass so the registry is honest before Phase 2's checker reads it: REMOVE OutOfRange/Length/Count from [StaticallyPreventable] + bijective core (compile checks survive under definition-incoherence / flag identities; PRE0079-on-default keeps Error, no author-visible change). RETARGET NumericOverflow (bijective partner becomes the new representability obligation, built Phase 3). ADD FaultCode.IndexOutOfBounds (route PRE0100), FaultCode.KeyNotFound (route PRE0099), the UnexpectedNull bijective row (route PRE0116 — StaticallyPreventableMap currently excludes it, so a presence trap reports a divisor message). PRE0101->DuplicateKey only under GATE-D require-absence. RETIRE the generic _ => FaultCode.DivisionByZero backstop -> per-family honest routing (qualifier->QualifierMismatch; PRE0157 dimensional product->TypeMismatch-genus/new code). ADD FaultCode.TemporalOverflow (gated on Slice-1 probe). Fix the pow-exponent >=0 misroute to SqrtOfNegative (shape-keying -> requirement-identity routing). KEEP TypeMismatch/UndeclaredField/InvalidMemberAccess/FunctionArityMismatch as defense-in-depth traps + one-line stack-depth disposition. Add ProofStrategy.Vacuous for forwarding-fact suppression honesty.
- **Exit criteria:** No rule code registered as a fault; no family traps as a literal _ => DivisionByZero; PRE0099/0100/0101 no longer produce false-emptiness messages; a presence/currency/pow fault reports an honest message; ProofStrategy.Vacuous recorded instead of Proved/Literal for vacuous forwarding facts; fault-system.md 13-vs-15 drift + stale NullInNonNullableContext partner fixed.
- **Doc-touch:** docs/runtime/fault-system.md (13-vs-15 drift + partner -> UnprovedPresenceRequirement); docs/compiler/diagnostic-system.md (code routing); docs/runtime/result-types.md (FaultCode inventory).

#### Slice 5 — Owner-approved spec amendments + canon-drift reconciliation (the atomic capstone)

- **Work:** Apply ONLY the GATE-F-approved amendments: §0.7's 'no result outside a declared bound' clause; Principle 11's 'constraint range impossibility' fault class; §0.6 item 6 -> proven-violation-only; the §0.7 discharge-tier sentence per GATE-G. Reconcile the canon-drift mined obligations belonging to this move: BUG-030 policy record vs proof-engine.md:1790 (promote the allow-all-with-surfacing lock OR re-open under §0.7 per owner — single-value it, do not let two contradictory records stand; restate the stale 'witness-checker' validating-consumer line); §0.6 items 7-10 'Specification-only' mislabel (codes fire); Restore spec §3A.2:1946 vs trusted-hydration. Philosophy.md absolute-claims reconciliation is OWNER-GATED and rides the guarantee-statement promotion — do NOT auto-edit. Verify the whole move is internally consistent (no spec sentence now contradicts the shipped reclassification).
- **Exit criteria:** Spec no longer asserts band prove-or-reject; canon describes the three-category taxonomy; no spec sentence contradicts the shipped registry; BUG-030 record is single-valued; philosophy.md untouched pending owner deliberation; the coordinated move is atomic and green end-to-end.
- **Doc-touch:** docs/language/precept-language-spec.md (§0.7, §0.6 item 6, Principle 11, §3A.2); docs/compiler/proof-engine.md:1790; docs/Working/bugs.md (BUG-030 narrow; move BUG-017/018/021-band dissolutions); README.md headline if it rides; philosophy.md ONLY with explicit owner approval.

## Later phase stubs

### Phase 2 — Position-Totality + Creation-Exhaustiveness Checker

- **Goal:** Make floor completeness a STRUCTURAL guarantee, not a set of spot fixes: project the catalog ChildExpressionPositions over the 5 expression-bearing DUs and build a 3-layer build-time creation-exhaustiveness checker that fails the build if any obligation family is missing at any catalogued position. HARD runtime precondition (the per-keystroke InspectFire path runs every position with no fault lane in Prospect).
- **Scope:** Slice 1: catalog ChildExpressionPositions projection (TypedExpression[15], TypedAction[3], TypedInterpolationSegment[2], TypedTransitionRow[2], TypedEventRow[2]; incl. the never-iterated AccessModes collection + TypedMemberAccess.Arguments/BUG-032) — catalog-before-code, single source of truth. Slice 2: total obligation walk closing the 10 fail-open positions, BUNDLING intra-guard short-circuit presence narrowing ('when X is set and X > 5') so the guard-slot closure does not mass-false-reject the canonical optional idiom; close BUG-031/032; characterize the corpus compat wave. Slice 3: the 3-layer creation-exhaustiveness checker (the missing guarantee property) + the 6 latent no-default-arm holes + fold the collection plan's element-entry-path family coverage into ONE mechanism; fold omit-join if Phase-0 adjudicated it floor-shaped.
- **Gates:** None blocking — depends on Phase 1's honest registry (the checker must assert against post-repartition codes).
- **Effort:** L (8-12 days)
- **Status:** Stub — TBD pending earlier-phase completion + the listed gates.

### Phase 3 — False-Proved Repairs + Missing Floor Obligations

- **Goal:** Close every green-ledger-over-a-guaranteed-fault hole and add the floor's missing obligation families so the floor is COMPLETE.
- **Scope:** Write-aware tier-2 discharge (GATE-B shape); representability-overflow obligation family at every arithmetic sink incl. UCUM factor application + checked constant-fold (GATE-A shape); temporal-overflow obligation + FaultCode (post-probe); qualifier-source mutation obligation (closes the silently-wrong-money hole live in fee-schedule.precept); BUG-030 UCUM cancellation factor (compile half; runtime value-application deferred to Phase 7 backlog); presence-wrapping-contradiction; mincount-at-birth seed (GATE-E); FunctionArg computed-arg per-FunctionMeta totality sweep (intersects BUG-032); choice-default membership (route to TYPE CHECKER, not a proof obligation — breaks C59/C86 today); min-bound discharge vacuity (GATE-G if resolved in-engine); BUG-021 (re-filed from Phase 1); BUG-028/029.
- **Gates:** GATE-A (representability shape), GATE-B (write-awareness shape), GATE-C (qualifier identity label), GATE-E (mincount), GATE-G (min-bound vacuity).
- **Effort:** L (10-15 days)
- **Status:** Stub — TBD pending earlier-phase completion + the listed gates.

### Phase 4 — Spec Completeness: Lexer + Primitives + Choice + Business + Temporal Datatypes

- **Goal:** 100% canon for the no-floor-entanglement datatype/literal surface.
- **Scope:** Lexer (6: exponent-lane restriction + silent-zero value bug, empty-interpolation rejection, lexer diagnostic de-dup, ~ on non-string, ~ident diagnostic); Primitives+choice (22: no-context numeric diagnostic, CI ~string enforcement family, ~startsWith first-arg, sqrt integer-lane reconciliation, choice PRE0088/0090/default-membership/cross-field-comparison, pow/sqrt/round/mid arg-constraint wiring + correct message routing, collection-typed-hole rejection); Business-domain (28: exchangerate slash syntax [owner-ruled, retire 'to' form], kg/hour denominator, angle-unit dimension identity, UCUM denominators, maxplaces compile-time precision proof [owner-authorized spec evolution], money/quantity qualifier rows, price-cancellation Stage-5 promotion + the never-written evaluator.md cross-unit reduction-rule doc); Temporal (11: date+/-typed-constant-quantity, DurationDenominatorMismatch identity, ~15 teachable-message drifts, PRE0073/0075/0076 routing, temporal-overflow coordinating with Phase 3); D9 clock-seam doc; 4 catalog-showcase samples (re-verify zero-use first).
- **Gates:** GATE-J (sqrt + choice field-vs-field canon contradictions — owner picks reading); GATE-K (log-unit policy, small /design); GATE-L (absolute-measurement positions — DEFER, new surface); maxplaces-proof spec evolution (owner-authorized).
- **Effort:** L (12-18 days, parallelizable)
- **Status:** Stub — TBD pending earlier-phase completion + the listed gates.

### Phase 5 — Spec Completeness: Collections + Modifiers + Expression Language + Rules/States/Binder/Graph

- **Goal:** 100% canon for collections, the modifier matrix, the expression language, rule/ensure scoping, states/transitions/events/actions, binder+type-checker, and graph-analyzer.
- **Scope:** Collections (22: list-literal defaults across all 6 kinds + empty [], lookup-access, cardinality, accessors, ascending/descending); Modifiers (11: compatibility matrix + field-reference bound enforcement = 2c-ii BUG-020, the largest unbuilt locked design, now unblocked by BUG-027; computed-field default fold); Expression (23: operators, functions, quantifiers, presence, member access, collection-in-interpolation rejection); Rules/ensures (18: all scoping forms + because interpolation binding); States/actions (25: action verbs + by/into/at variants, access modes, event args/initial); Binder/TC (17: shadowing, inference, qualifier compatibility, computed-field ordering+cycles, binder-shadowing lint); Graph (10: reachability/dead-end/dominators/terminal/coverage). Mined: conditional-totality spec+evaluator-contract half (owner-approved) + proof-engine half (re-grounded against the fault-floor outcome — do NOT resurrect the deleted ProofChecker), two-axis element bounds (deferred), sign-set element-band gap, BUG-025/026/Q3/Q4 diagnostic-policy bundle.
- **Gates:** GATE-M (PRE0141 bound-expression ownership — decide via the 2c-ii interaction; feeds Phase 6). Caveat: re-derive conditional-totality build-order/soundness-backstop against the fault-floor outcome.
- **Effort:** L (14-20 days)
- **Status:** Stub — TBD pending earlier-phase completion + the listed gates.

### Phase 6 — Diagnostic + Pipeline-Tail Completeness + Emission Ownership

- **Goal:** Close proof-engine/diagnostic + pipeline-tail gaps; emit-or-retire every declared code; verify every [StaticallyPreventable] code is live; build the single-stage-ownership analyzer.
- **Scope:** Proof-engine+diagnostic (28) + the gap-fill pipeline-tail/header/parser-recovery/Compilation-orchestration/grammar-gen/catalog-meta remainder of the 236; Phase-9 diagnostic completeness (148-vs-162 reconcile via DiagnosticCoverageScanner Patterns 1/2/3 — literal grep incomplete; NullInNonNullableContext wire-or-retire; NonAssociativeComparison label); Slice-3c catalog-declared single-stage diagnostic-ownership Roslyn analyzer (gated GATE-M, one carve-out for PRE0141, zero allow-list); BUG-022 consumer-side *Kind exhaustiveness analyzer + Precept0019 exemption /design; Slice-4 witness richness (re-grounded as diagnostic-inspectability per Principle 4, NOT the deleted witness-checking); Compilation digest hash/ContractDiff stub disposition; SlotValue/ParsedConstruct contract; catalog-system.md drift; F-LANG-SPEC-08 ten graph modifiers (explicit post-runtime Compiler-1.1 deferral).
- **Gates:** GATE-M feeds Slice 3c; BUG-022 exemption-model /design; merge-the-three-lenses (this phase + conformance [Covers] + Stage-2 audit = ONE register).
- **Effort:** L (12-18 days)
- **Status:** Stub — TBD pending earlier-phase completion + the listed gates.

### Phase 7 — Conformance, API Solidity + Polish + Runtime-Ready Gate

- **Goal:** Stand up the conformance program, solidify the API surface, burn down polish, settle floor->runtime reconciliations, enumerate (not build) the runtime backlog, owner sign-off -> READY TO BEGIN RUNTIME WORK.
- **Scope:** Conformance-suite program (Precept.Conformance.Tests own-lane allowed-red project, three-edge coverage catalog<->impl/spec<->impl/spec<->catalog, [SpecRef]/[Covers] + coverage meta-test, fence-annotation convention, runtime facet as Skip'd v1 spec, Phase-0 infra, area sequencing, 5 carried open decisions); API solidity (F-API-01 typed field descriptors = explicit runtime precursor, F-PAR-04, F-TC-02/03); polish (~20 P2s, F-NB-01/PRF-01/LS-03, F-CAT-01 27-violation re-verify, BUILD the /audit lifecycle skill); record OD-1 shared-eval-core; re-derive the 'compiler production-ready' exit checklist under the niche framing (full-suite stability, zero warnings, doc/code reconciliation, catalog sweep, diagnostic-scenario matrix); settle GATE-I if not earlier; enumerate the runtime-phase parked bundle (Rule-2 element governance at ingress, general ingress value-constraint governance §0.7, D6.2 composite-period lowering, F-LANG-BIZ-11 money boundary precision, BUG-030 runtime factor application, maxplaces runtime points 2-3, cross-unit reduction-rule evaluator.md requirement); low-priority triage carriers; confirm GATE-N (definition-evolution lane stays out) and niche Layer 2/3 deferred post-runtime; owner sign-off.
- **Gates:** GATE-I (fault-delivery), GATE-N (definition-evolution confirm-out); owner sign-off; all prior phases complete.
- **Effort:** M (8-12 days)
- **Status:** Stub — TBD pending earlier-phase completion + the listed gates.

## Coverage ledger (every item placed or deferred-with-reason)

EVERY required item is placed or deferred-with-reason.

FLOOR GAPS (16, from the research Gaps section): (1) obligation-position incompleteness -> Phase 2 Slices 2-3; (2) write-unaware tier-2 -> Phase 3; (3) DesugarsToRule->governance wiring -> Phase 1 Slice 2; (4) representability-overflow -> Phase 3 (registry slot Phase 1 Slice 4); (5) qualifier-source mutation -> Phase 3; (6) BUG-030 UCUM cancellation -> Phase 3 (compile) + Phase 1 Slice 5 (canon record) + Phase 7 (runtime value-application); (7) presence-wrapping-contradiction -> Phase 3; (8) mincount-at-birth -> Phase 3 (+ scale-back coupling Phase 1 Slice 3); (9) fault registry/link repartition -> Phase 1 Slice 4 (WHOLE); (10) FunctionArg computed-arg totality -> Phase 3; (11) temporal-arithmetic overflow -> probe Phase 0 Slice 1, build Phase 3 (+ datatype surface Phase 4); (12) choice-default membership -> Phase 3 (TC-owned; also surfaces in Phase 4 choice work); (13) omit x global-rule join -> adjudicated Phase 0 Slice 1, built Phase 2 Slice 3 if floor-shaped; (14) min-bound discharge vacuity -> Phase 3 (GATE-G; Phase 1 Slice 5 if by amendment); (15) pre-runtime design reconciliations (fault-delivery GATE-I, Restore) -> Phase 0 Slice 3 + confirm Phase 7; (16) binder shadowing/unused-binder -> Phase 5 (TC-owned lint).

SCALE-BACKS (8): PRE0078-band, PRE0135, PRE0136, OutOfRange+PRE0079, PRE0164+PRE0115 identity reframe, BUG-017/018/021 dissolution, spec/canon edits, the HARD COORDINATION CONSTRAINT — ALL in Phase 1 (Slices 3-5), landing as the one coordinated move.

REGISTRY CORRECTIONS (all 10 bullets) -> Phase 1 Slice 4 WHOLE (REMOVE OutOfRange/Length/Count; RETARGET NumericOverflow; ADD IndexOutOfBounds/KeyNotFound/UnexpectedNull/TemporalOverflow; PRE0101->DuplicateKey conditional; RETIRE the _ => DivisionByZero backstop; KEEP the 4 TC traps + stack-depth disposition; FIX fault-system.md drift; ADD ProofStrategy.Vacuous). The pow-exponent misroute fix rides here too.

236 SPEC GAPS (13 areas, 619 rows): Lexer(6)+Primitives/choice(22)+Business(28)+Temporal(11)=67 -> Phase 4; Collections(22)+Modifiers(11)+Expression(23)+Rules/ensures(18)+States/actions(25)+Binder/TC(17)+Graph(10)=126 -> Phase 5; Proof/diagnostic(28)+pipeline-tail/header/parser-recovery/Compilation/grammar-gen/catalog-meta gap-fill remainder=43 -> Phase 6. 67+126+43=236.

43 MINED OBLIGATIONS: canon-drift(4) -> Phase 1 Slice 5 (BUG-030 reconcile, §0.6 7-10 mislabel, Restore §3A.2) + philosophy.md OWNER-GATED (rides guarantee-statement promotion, never auto-edited). doc-sync-owed(6) -> 2c-ii package Phase 5, conditional-totality spec half Phase 5, D9 clock-seam Phase 4, price-cancellation Stage-5 promotion Phase 4, cross-unit reduction-rule evaluator.md Phase 4, standing-declines record Phase 0 Slice 3. future-candidate(2) -> Karr-first proof-power list + expressiveness aggregates recorded as OWNER-GATED Tier-2 STUBS (NOT planned — see deferred). locked-design-unbuilt(5) -> 2c-ii Phase 5, computed-field default fold Phase 5, Slice-3c ownership analyzer Phase 6, conditional-totality proof-engine half Phase 5, conformance suite Phase 7. open-bug(6) -> BUG-020 Phase 5, BUG-021 Phase 3, BUG-022 Phase 6, position-incompleteness(BUG-031/032) Phase 2, BUG-025/026 Phase 5, BUG-028/029 Phase 3. open-decision(5) -> GATE-M Phase 5/6, GATE-K log-units Phase 4, OD-1 Phase 0 Slice 3 + Phase 7, Points A/C/Q4-Q5 settled across GATE-F/I + Phase 0, Point B (certain/possible/impossible vocab) deferred low-priority. plan-phase-incomplete(15) -> Phase-7-residue datatype rows Phase 4, Phase-8 Slices 3c/4/5 Phase 6, Phase-9 diagnostic completeness Phase 6, Phase-10 API solidity Phase 7, Phase-11 polish Phase 7, Phase-12 gate redefinition Phase 7, runtime parked bundle Phase 7 backlog, F-LANG-SPEC-08 ten graph modifiers Phase 6 post-runtime deferral, low-priority triage carriers Phase 7.

EXPLICITLY DEFERRED WITH REASON: (a) the flag-sound niche Layer 2/3 — POST-RUNTIME per the niche packet; the runtime needs nothing from it while it needs the shipped runtime as its error-promotion oracle (the live PRE0155 false positive proves premature promotion actively rejects correct precepts). (b) definition-evolution deploy lane (GATE-N) — its own decision, most severe uncovered family, folding it in blurs the teachable line. (c) Phase 7 Slice 4 absolute-measurement positions (GATE-L) — new surface, edge-case, Tier-2-gated when taken up. (d) future-candidate proof-power (Karr-first) + expressiveness (maintained aggregates) lists — owner-gated Tier-2, carried as stubs not work, post-runtime/separate. (e) F-LANG-SPEC-08 ten graph-analyzer modifiers — owner-deferred Compiler-1.1, post-runtime. The dropped/overtaken items (witness-checking half, prevented/governed annotation framing, ingress-refinement keystone) are correctly NOT carried per the triage's reasons. Nothing dropped silently.

## Sequencing red-team

I red-teamed every phase boundary against the four constraints. WHERE THE DRAFTS DISAGREED I PREFERRED THE DEPENDENCY-CORRECT ORDERING, and the merge took the best of both: Draft B's dedicated decision-gate phase (the decisions genuinely gate the coordinated move and the representability family, and a mid-build owner-call stall is the failure Draft A risks by folding decisions into Slice 1 of a heavyweight phase) became Phase 0; Draft A's WHOLE-registry-in-the-coordinated-move (atomicity) was preferred over Draft B's SPLIT registry (Draft B's Phase-2 Slice-3 did a partial registry repartition, then minted the rest in Phase-3's coordinated move — that splits the registry across two phases and makes the position-totality checker in Draft B's Phase 2 assert against a HALF-honest registry, a real soundness-of-the-checker hole). 

(a) COORDINATED-MOVE ATOMICITY — INTACT. Phase 1 keeps the four pieces (wiring / band scale-back / WHOLE registry / spec amendments) in one phase behind a single coordinated merge, with Slice 2 (wiring) strictly before Slice 3 (band retirement) so there is never a window where declared bounds are enforced nowhere (the verified zero-consumer DesugarsToRule grep). Both drafts honored atomicity; the merge tightened it by refusing the registry split.

(b) FLOOR PREREQUISITE BEFORE ITS DEPENDENT — INTACT. The DesugarsToRule->governance wiring (Phase 1 Slice 2) precedes the write-aware tier-2 repair (Phase 3) that relies on committed-state discharge being soundly enforced. Phase 1 before Phase 3.

(c) FLOOR-COMPLETENESS BEFORE RUNTIME-READY GATE — INTACT. Position-totality + the 3-layer checker (Phase 2) and the false-Proved repairs + missing obligations (Phase 3) both land before the Phase-7 runtime-ready gate. The checker (Phase 2) is correctly placed AFTER the WHOLE registry (Phase 1) so it asserts against honest codes — this is the specific reordering that fixes Draft B's split-registry hole.

(d) OWNER GATES UPSTREAM OF THE PHASES THEY BLOCK — INTACT. Every gate is surfaced in Phase 0 and re-cited at the phase it blocks: GATE-A/D/E/F/G/H/I -> Phase 1; GATE-A/B/C/E/G -> Phase 3; GATE-J/K/L -> Phase 4; GATE-M -> Phase 5/6; GATE-I/N -> Phase 7. No gate is buried inside the phase it governs.

(e) FALSE-PROVED BEFORE RUNTIME — INTACT (Phase 3 before Phase 7); a green ledger over a guaranteed fault is exactly what the runtime would surface as an unattributable backstop fault long after the causal write.

(f) BREADTH AFTER THE FLOOR HOLDS — INTACT. Spec-completeness (Phases 4-6) follows the floor being sound (Phase 1), position-total (Phase 2), and complete (Phase 3), so new constructs land against a floor that holds rather than widening the fail-open surface. The diagnostic/ownership phase (6) is last in breadth because it consumes GATE-M (PRE0141 ownership) resolved by the 2c-ii work in Phase 5.

WHAT WAS REORDERED AND WHY: I split Draft A's overloaded Phase-1-Slice-1 (which carried both decision-settling AND probe work) into a standalone Phase 0, because a heavyweight coordinated-move phase cannot also be the place owner decisions are first surfaced (Tier-3 spec conflicts need owner deliberation that could stall the merge). I collapsed Draft A's Phase 3 (false-Proved) and Draft B's Phase 2 (substrate)+Phase-3(coordinated-move) tension by sequencing: Phase 0 gate -> Phase 1 whole coordinated move (registry whole) -> Phase 2 position-totality (asserts against honest registry) -> Phase 3 false-Proved + missing obligations. I merged the two drafts' 5-and-6 breadth phases into datatype (4) and grammar/rules/states (5) tracks per Draft B's parallelizability insight while keeping Draft A's floor-first spine. No phase ships before its dependency.

## Definition of done

READY TO BEGIN RUNTIME WORK, defined as ALL of: (1) The fault floor is SOUND — every false-Proved hole closed (write-aware tier-2, qualifier-source, presence-wrapping, mincount-at-birth, BUG-030 cancellation factor), no family traps as the generic DivisionByZero backstop, the ledger never claims more than the floor (ProofStrategy.Vacuous in place). (2) The fault floor is COMPLETE — all 11 obligation families present, position-total (the catalog ChildExpressionPositions projection + 3-layer creation-exhaustiveness checker gate the build so a fail-open obligation position is structurally impossible), the representability + temporal-overflow families built under the GATE-A representation decision. (3) The fault floor is RIGHT-SIZED — the band-containment lane (OutOfRange/Length/Count + PRE0078-band) has exited to governance + flag-sound warnings for non-proof-carrying fields, the DesugarsToRule->governance/ingress wiring enforces the discharging constraints, the registry is trimmed/repartitioned WHOLE, and the owner-approved spec amendments make canon consistent with the shipped behavior — all delivered as one coordinated move with no window where bounds were enforced nowhere. (4) The canonical language spec is 100% IMPLEMENTED — all 236 coverage gaps closed across datatypes/grammar/constructs/diagnostics, every declared DiagnosticCode emits-or-retires, every [StaticallyPreventable] code is live-not-dead, the single-stage-ownership analyzer gates emission, and doc/code/catalog are reconciled into ONE register. (5) The floor->runtime design reconciliations are settled — the fault-delivery contract (GATE-I) is decided, OD-1 (shared expression-eval core) is recorded as a runtime-build constraint, the API surface is final (the runtime builds against it), and the runtime-phase backlog is ENUMERATED (not built) so nothing is dropped. (6) Owner sign-off on the gate. The flag-sound universal niche layer is explicitly revisited AFTER runtime; the definition-evolution deploy lane remains its own separate decision (GATE-N confirmed out).

## Risks and caveats

CALIBRATION (from the research's own Threats to Validity): HIGH confidence — the registry adjudication (double-derived, red-teamed, critic-checked, 10+ live probes), the NumericOverflow inversion, the OutOfRange/Length/Count misclassification, the position-incompleteness breach, the DesugarsToRule zero-consumers constraint, the sequencing verdict, forwarding-fact suppression soundness. MEDIUM — the write-unaware discharge hole's RUNTIME consequence is inferred from the DESIGNED (stubbed) evaluator's working-copy semantics, not observed execution; the cross-event sign-modifier case assumes the sweep ships without modifier desugar (which Phase 1 Slice 2 fixes). MEDIUM-LOW — temporal overflow (asserted from NodaTime, UNPROBED — Phase 0 Slice 1 verifies-then-builds; if the probe refutes the gap, the temporal-overflow obligation + FaultCode drop out of Phases 3/4), and the omit x global-rule join's floor-shape (adjudication owed in Phase 0, not done).

LOAD-BEARING DECISION RISK: GATE-A (numeric representation) is the highest-leverage open gate — choosing arbitrary-precision DELETES the entire representability obligation family and most of the reinstated write-time checking, materially shrinking Phase 3. The plan is structured so this collapses cleanly rather than requiring rework: the registry RETARGET (Phase 1 Slice 4) stages the slot, the family BUILDS in Phase 3, and under arbitrary-precision both simply do less.

COORDINATED-MOVE RISK: Phase 1 is the single riskiest phase — a soundness-and-canon atomic move across the proof engine, runtime wiring, the registry, and the spec simultaneously, gated on six owner decisions all of which must clear in Phase 0. If any GATE-F amendment is declined, the shipped reclassification and canon diverge; the plan refuses to ship the band scale-back without the matching spec edit (Slice 5 is the atomic capstone). Philosophy.md is NEVER auto-edited — it rides the separate owner-gated guarantee-statement promotion.

COMPAT-WAVE RISK: Phase 2's position-totality walk WILL produce a wave of new (correct) errors on existing corpus files; the intra-guard conjunct narrowing is BUNDLED (not deferred) precisely so the guard-slot closure does not mass-false-reject the canonical 'when X is set and X > Y' idiom. If narrowing is under-built, the wave becomes false-rejections rather than true catches.

DO-NOT-RESURRECT DIRECTIVE: two later items (conditional-totality proof-engine half in Phase 5, witness richness Slice 4 in Phase 6) cited the now-DELETED ProofChecker / verify-don't-trust architecture; both MUST be re-grounded against the fault-floor outcome, not executed from stale soundness-arch citations. Witness work is diagnostic-inspectability (Principle 4), categorically distinct from the deleted witness-CHECKING.

SCOPE BOUNDARY: this plan ends at 'ready to begin runtime'. The flag-sound niche layer (Layer 2/3) is genuinely out of scope and must NOT be planned here; the definition-evolution deploy lane is its own decision and should be scheduled before first external users independent of this plan.

## The exhaustive backlog lives in the appendices

This plan organizes the work into phases; the **complete itemized backlog** is in `compiler-readiness-plan-2026-06-11-appendices/`:
- **`fault-floor-research-full.md`** — every fault adjudication, the engine inventory, both floor derivations, the reconciliation, red-team, and critic (the full record behind `research/architecture/compiler/fault-floor-definition-2026-06-11.md`).
- **`spec-coverage-audit.md`** — all 619 rows (383 fully / 85 partially / 20 stub / 53 missing / 78 diverges) with per-feature canon cites and probe evidence, plus 98 canon-drift items to fix.
- **`working-docs-triage.md`** — the disposition of all 32 prior docs and the 43-item obligation register with provenance.

## Plan update protocol

Update this file when a phase completes (mark slices done with commit refs + test-suite outcome), when a gate is resolved (move the decision from "open" to "captured"), or when execution surfaces a new obligation (add it to the relevant phase + note it in the coverage ledger). Phases beyond the next stay stubs until their predecessor lands.
