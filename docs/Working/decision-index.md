# Compiler-Readiness Decision Index

**status:** Working index — companion to `compiler-readiness-plan-2026-06-16.md`

This is the one-page lookup for "is this already decided?" When a settled question resurfaces from a new angle, find the row, read the plain-language decision, and follow the citation to the full text — do **not** re-derive it. Each row is a pointer, never a copy: the "Where decided" column names the exact section that owns the decision, and the "Applied / not-yet-applied" column tells you whether the compiler code has landed or is still scheduled — a decision being *made* and its implementation being *built* are two different things, and this index keeps them visibly distinct.

**How to read the third column.** `LIVE` = already in the compiler. `→ Phase N Slice X` = decided, code lands in that named slice. `captured-only` = the decision is written into runtime canon (`docs/runtime/*.md`) as a spec for the deferred runtime build; no code is built for it in this plan. `surfaced-only` = an owner-gated philosophy/guarantee surface that is flagged for the owner and never auto-edited.

**Resolution state.** The four owner decisions (D1–D4) are **settled** this session — cite them, never re-litigate. The Phase-0 lettered gates are mostly **pending** owner conversations (the plan is still `Draft`; Phase 0 has not run). GATE-A is the one gate already resolved (it *is* D1). GATE-M is deliberately not a Phase-0 gate — it resolves later, at Phase-5 start.

---

## The boundary and the four owner decisions

| Decision (plain language) | Where decided | Applied / not-yet-applied |
|---|---|---|
| **Prove-or-reject is permanent.** The compiler rejects any definition it cannot prove can never fault. The runtime being an unbuilt stub is *never* itself a reason to reject — runtime-absence is not the same as a fault the compiler can't rule out. | plan §0 discipline 3; grounded in `philosophy.md` L53 (division-by-zero / overflow / empty-collection are compile-time impossibilities) and L57 (a clean compile has no unproven evaluation faults); `docs/compiler/proof-engine.md` | LIVE — this is the compiler's standing behavior. Remaining false-"proved" holes close in Phase 3 (Slices 3.1–3.3); made structurally total by the Phase-2 exhaustiveness checker (Slice 2.3). |
| **Admit-now / defer-enforcement.** An operation that is safe *only once the runtime enforces something* is admitted by the compiler today; the runtime's enforcement job is written into the handoff (§11), not built. The compiler never keeps rejecting merely because the runtime is unbuilt. | plan §0 discipline 3 | Pattern is LIVE in shape for reclassified bands (D2) and committed-state discharge (Slice 3.1). Enforcement itself is **captured-only** — `docs/runtime/evaluator.md §7.6` + plan §11. |
| **D1 — Overflow parked.** Decimal- and temporal-representability overflow (a computed value exceeding what the number/date types can hold) is an accepted, documented, temporary known fault. No prevention machinery is built for it; no doc may claim it prevented. | plan §2 D1 (resolves GATE-A as "park") | Registry keeps `NumericOverflow` as a disclosed backstop with no live guarantee → Phase 1 Slice 1.4. Honesty disclosure = DoD-4. Canon softened → Phase 1 Slice 1.5. `philosophy.md` gap is **surfaced-only** (DoD-9). |
| **D2 — Band split.** Min/max, length, and count bounds that feed no safety proof drop from rejection to a warning that fires only on a *proven* violation (merely-unprovable cases compile clean). Runtime enforcement of those bounds is deferred. Bounds that do feed a proof ("carries-proof") keep rejecting. | plan §2 D2 | Compiler reclassification → Phase 1 Slice 1.3; registry trim → Slice 1.4; spec amendment → Slice 1.5 (DoD-3). Runtime governance enforcement = **captured-only** (`evaluator.md §7.6`, §11). |
| **D3 — Atomicity relaxed.** Pre-release with a stubbed runtime, so changes land incrementally with no coordinated all-at-once merges. Exactly **one** hard ordering survives: the fault-code registry must be made honest (Phase 1 Slice 1.4) *before* the Phase-2 creation-exhaustiveness checker (Slice 2.3) first reads it. | plan §2 D3 | Governs Phase 1 → Phase 2 sequencing (DoD-3). The prior plan's "coordinated move" machinery is deleted. |
| **D4 — Frank review fixes folded.** The accepted findings of the adversarial review of the prior plan are applied throughout (e.g. B1 complete GATE-F enumeration, B2 DesugarsToRule wording, B3 provenance count, A6 "relocated, not weakened"). | plan §2 D4 | Applied throughout the plan text; individual findings land in the slices they name. |

---

## The Slice 3.1 scope boundary (the intra-chain / committed-state split)

| Decision (plain language) | Where decided | Applied / not-yet-applied |
|---|---|---|
| **Intra-chain staleness → the compiler rejects.** If an earlier write in the *same* event's action chain invalidates a standing fact (a modifier or rule fact), that fact may no longer discharge the obligation. The compiler can see the write, so this is real fault-unprovability — permanent prove-or-reject, no runtime rescue. | plan Slice 3.1 (Scope boundary) | → Phase 3 Slice 3.1 — extends the existing `ProofLedger` staleness machinery (`ReassignedBefore` etc.); not built yet. |
| **Committed-state discharge → admit now, enforcement deferred.** Proofs discharged against values persisted by *earlier, separate* operations are admitted by the compiler today. Soundness rests on the runtime enforcing each discharging constraint at every prior commit — deferred, not a compiler exit criterion. | plan Slice 3.1 (Scope boundary); plan §12 DoD-1 boundary carve-out | Admitted compiler-side now; enforcement is **captured-only** at `evaluator.md §7.6 + §10`. |

---

## Phase-0 owner-decision gates

All raised in Slice 0.2 unless noted. Status is **pending** unless stated otherwise (Phase 0 has not run).

| Gate — what it decides (plain language) | Where it resolves | Status / where applied |
|---|---|---|
| **GATE-A** — whether to build overflow prevention. | plan §2 D1 / Slice 0.2 exit | **Resolved: park** (it *is* D1). |
| **GATE-B** — the shape of write-aware tier-2 discharge: attach preservation obligations to writes, vs. invalidate the stale fact and re-establish it forward. | Slice 0.2 → applied Phase 3 Slice 3.1 | Pending; conservative default = the reuse (staleness-invalidation) shape. |
| **GATE-C** — the *label* only for qualifier-assignment-lane rejections (`QualifierMismatch` vs. a named third category). The reject behavior is already settled. | Slice 0.2 → applied Phase 3 Slice 3.2 | Pending (label only — the build never blocks on it). |
| **GATE-D** — PRE0101 duplicate-key semantics: a keyed add *requires the key absent* (stays a fault) vs. *put/overwrite* (uniqueness becomes a governance rule, enforcement deferred). | Slice 0.2 → `diagnostic-system.md`, `collection-types.md`, spec | Pending. The separate false-emptiness *fix* lands regardless (Phase 1 Slice 1.4). |
| **GATE-E** — what a declared minimum count means at Create: birth exemption / deferred check at first mutation / defined refusal + flag. | Slice 0.2 → applied Phase 3 Slice 3.3 (compiler half); runtime refusal deferred | Pending. |
| **GATE-F** — the *complete* enumeration of spec + `philosophy.md` sentences the band and overflow changes amend. Tier-3: amends locked spec text, owner approval required, quoted verbatim. | Slice 0.2 → applied Phase 1 Slice 1.5 | Pending (enumeration listed in the plan; owner must approve before edit). |
| **GATE-G** — whether declared minimum bounds should discharge in the proof engine, or the §0.7 discharge-tier sentence narrows instead. | Slice 0.2 → Phase 3 Slice 3.4 (option i) or Phase 1 Slice 1.5 (option ii) | Pending; default = the spec-narrowing path (overflow parked removes option i's motivation). |
| **GATE-H** — confirm the three-category taxonomy: every diagnostic is exactly one of fault floor / definition incoherence / flag layer. | Slice 0.2 → applied Phase 1 Slice 1.4 | Pending (a confirmation; both independent floor derivations converged on it). |
| **GATE-I** — the fault-delivery contract: is a fault a thrown exception or a structured outcome value? Runtime canon currently contradicts itself. | Slice 0.3 (owner-facing) → `result-types.md`, `evaluator.md`, `fault-system.md` | Pending; **surface the contradiction, do not pick unilaterally**. |
| **GATE-J** — two internal spec self-contradictions: sqrt of an integer (§3.2 vs §3.7) and choice-typed field-vs-field comparison (a false-clean soundness bug). | Slice 0.2 → applied Phase 4 Slices 4.6 / 4.10 | Pending (owner picks the canonical reading; the fix follows). |
| **GATE-K** — cross-unit policy for logarithmic units (dB, Np, B): allow (with what semantics) vs. restrict. Tier-2 new language surface. | Slice 0.2 → applied Phase 4 | Pending; an interim floor (`ScaleIsRational=false`) already blocks the unsound cases. |
| **GATE-L** — whether to build absolute-measurement positions (an "instant"-analog for units) or keep deferring. New language surface. | Slice 0.2 | Pending; recommended disposition = **defer** with a recorded reason. |
| **GATE-M** — which single pipeline stage owns PRE0141 (`UnprovedAssignmentQualifierCompatibility`) emission for bound expressions. | **Not a Phase-0 gate** — resolves at Phase-5 start as a side effect of Slice 5.1; fallback carries it to Phase 6 | Pending (deliberately deferred to Phase 5). |
| **GATE-N** — how evolved definitions deploy against previously persisted entities. | Slice 0.2 → re-checked at Phase-7 gate | Pending; disposition = **confirm out** of fault-floor scope (its own future initiative). |
| **GATE-O** — fault-code disposition for integer-conversion overflow and non-finite (∞ / NaN) values: fold into the park / new fault lane owed prevention / own explicit park. | Slice 0.2 → applied Phase 4 Slice 4.9; DoD-9 §10 contract follows | Pending. |

---

## Canonical Locked Design Decisions the plan implements (pointer rows)

These live in the canonical docs and are **already locked** — the plan's job is to make the compiler surface implement them (Phase 4/5, DoD-5). They are cited here so a resurfacing question routes to the owning doc, not back into the plan. Do not confuse these numbers with the plan's own D1–D4.

> **Numbering caution:** "D9" is used by two different documents. `business-domain-types.md` D9 = open-field discrete-equality narrowing; `evaluator.md` D9 (captured in plan Slice 0.3) = injectable per-operation clock. Always pair the number with its owning doc.

| Decision (plain language) | Where decided | Applied / not-yet-applied |
|---|---|---|
| **D6** — entity-scoped conversion factors are typed compound quantities, not bare integers (and no dedicated `units { }` block). | `business-domain-types.md` § Locked Design Decisions D6 (line 1717) | Canon-locked; compiler surface completed in Phase 4/5 (DoD-5). |
| **D8** — commensurable same-dimension arithmetic with deterministic unit resolution (auto-conversion to the target unit). Cited by the boundary as a canonical admit-now / defer-enforcement case. | `business-domain-types.md` § Locked Design Decisions D8 (line 1733) | Compiler admits the static case; the dynamic-write enforcement is **captured-only** (`evaluator.md §Intake-Boundary`, DoD-7). |
| **D9 (business)** — open fields require discrete equality narrowing before arithmetic. | `business-domain-types.md` § Locked Design Decisions D9 (line 1741) | Canon-locked; reused by the Slice 3.2 qualifier-source obligation. |
| **D15** — time-unit denominators use NodaTime vocabulary and cancel against `period` or `duration` (fixed-length boundary). | `business-domain-types.md` § Locked Design Decisions D15 (line 1821) | Canon-locked; compiler surface completed in Phase 4/5 (DoD-5). |
| **D16** — business-domain magnitude types inherit Precept `decimal` semantics by default; domain identity justifies specific table exceptions. | `business-domain-types.md` § Locked Design Decisions D16 (line 1847) | Canon-locked; compiler surface completed in Phase 4/5 (DoD-5). |
| **Fault-delivery / registry homes** — the `[StaticallyPreventable]`-derived fault map (the honest registry D1/D2/D3 reshape) and the proof-engine rationale. | `docs/compiler/diagnostic-system.md` § The `[StaticallyPreventable]`-derived fault map (line 533) and § Design Rationale (line 734); `docs/compiler/proof-engine.md` § 11 Design Rationale (line 2420) | Registry made honest → Phase 1 Slice 1.4; write-aware discharge recorded → Phase 3 Slice 3.1. |
