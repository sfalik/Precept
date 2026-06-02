# Proof-Engine Guarantee Contract — Doc-Clarity Plan — 2026-06-02

**Status**: Draft
**Companion docs**:
- `docs/Working/compile-time-vs-runtime-contract-clarity-2026-06-01.md` (clarity assessment — neutral)
- `docs/Working/guarantee-claims-doc-audit-2026-06-02.md` (Frank's guarantee-claims audit)
- `docs/Working/dynamic-modifier-bounds-research-2026-06-01.md` (the work this unblocks)
- `docs/Working/bugs.md` → BUG-017 (the one concrete contract breach found so far)
**Scope gate**: makes the compile-time/runtime guarantee contract canonical and single-sourced, so it is not re-litigated; unblocks the dynamic-modifier-bounds design (a bound is a governed constraint; downstream guarantees prove-or-reject).

## The contract (grounding — to be locked in Phase 1)

Four-part statement, converged in conversation 2026-06-01/02:

1. **Fault-freedom is established entirely at compile time** (Principles 7/10/11). For every fault-prone op the compiler proves the operand *carries* a sufficient discharging constraint (declared modifier/rule, author guard, or statically-known value) — a **structural** fact, not the concrete value. If it cannot, it **rejects**. It never defers a fault obligation to runtime.
2. **Governance is enforced at runtime on external input** (Principle 6). Declared constraints are enforced on every externally-sourced value at ingress, before any dependent computation reads it; operation-blind; atomicity (§3A.4) discards on failure so no invalid configuration persists — **prevention, not detection**.
3. **Composition**: the compile-time proof for an external operand rests on the structural "carries a declared constraint" fact; governance makes that constraint true on the value. Proof complete at compile time; nothing deferred.
4. **Runtime fault traps are redundant** — unreachable for contract data. They fire only for **out-of-contract data** (cross-version Restore, external injection). A fault from a **proof-engine gap is a bug**, not an accommodated condition.

## Phase summary

| Phase | Goal | Items | Decisions required | Effort | Status |
|---|---|---|---|---|---|
| 1 | Lock the canonical contract statement + decide where it lives | contract §1–4; placement | D1 placement; D2 philosophy wording sign-off | S–M (1–2d) | Draft |
| 2 | Empirical grounding: verify code prove-or-reject vs drift per fault class | divisor, overflow/bounds, sqrt, empty-access, count, maxplaces; ingress; Restore | none (read-only) | M (2–3d) | Stub |
| 3 | Propagate to canonical compiler/runtime docs | spec §0/§3A, proof-engine.md, fault-system.md, runtime-api.md, evaluator.md | D3 Restore truth (from Phase 2) | M–L (3–5d) | Stub |
| 4 | Consumer-facing docs (owner-authored) | philosophy.md, README.md | D2 (philosophy) | S (1d) | Stub |

## Decisions captured (converged 2026-06-01/02)

- **Governance ↔ fault-freedom are distinct axes.** Runtime constraint enforcement is governance (P6), not a deferred fault check. Settles the apparent Principle 11 tension *without* a P11 reword.
- **Prove-or-reject, never defer.** The compiler proves the operand *carries* its constraint or rejects; it never punts a fault obligation to runtime.
- **"Carries proof" is the right framing.** The compiler proves a structural fact about external operands (they carry declared constraints), not their values — so "the compiler can't prove anything about external values" is *false* and must not appear in any doc.
- **Prevention, not detection — holds.** Atomicity (§3A.4) means an invalid configuration never persists; rejecting before commit is prevention. (Rejects Frank A3.)
- **A proof-engine gap is a bug, not defense-in-depth.** Out-of-contract data (cross-version Restore, external injection) is the *only* legitimate reason a `[StaticallyPreventable]` trap fires. (Rejects Frank MQ2 / the "within current proof coverage" softening.)
- **The three-layer model is consistent with the contract** (Layer 1 = fault-freedom, Layer 2 = governance, Layer 3 = redundant traps); it needs *sharpening*, not reversal.

## Consolidated audit disposition

Both audits, each finding mapped to a contract-aligned action. `TIGHTEN` = fix the runtime/impl doc to the contract; `QUALIFY` = add the out-of-contract carve-out (not a softening); `REJECT` = contradicts the contract, do not apply; `RESOLVE` = needs code ground-truth.

| Source | Finding | Disposition | Action / Phase |
|---|---|---|---|
| Clarity | Contract not stated in one place | — | Write it. P1 |
| Clarity | Arg-gate: type/presence (evaluator) vs value-level (biz-types) | RESOLVE→TIGHTEN | Ingress = governance enforcing declared value-level constraints; reconcile evaluator.md. P2→P3 |
| Clarity | Restore constraint-check contradiction | RESOLVE | Ground in code; Restore = out-of-contract → governance re-validates. P2→P3 |
| Clarity | unresolved→runtime bridge silent | — | Covered: unresolved = reject; out-of-contract = governance/fault. P1/P3 |
| Clarity | P11 "primary vs redundant" tension | — | No reword; add composition statement. P1/P3 |
| Frank A1 | "no diagnostics = no faults" ambiguous | QUALIFY | Out-of-contract carve-out (not proof-coverage). P3 |
| Frank A2 | "structurally impossible" — which layer? | TIGHTEN | The P4 composition insertion names both mechanisms (compile-proof + governance + atomicity). P4 |
| Frank A3 | "prevention is really detection" | REJECT | Atomicity = prevention. P1 records the rejection; philosophy unchanged. |
| Frank A4 | "compile-time impossibilities" overstated | REJECT (softening) | Keep absolute for in-contract data; **do not** add "within proof coverage." Out-of-contract handled in runtime docs (P3), not a philosophy qualifier. |
| Frank C1 | Restore constraint contradiction | RESOLVE | = Clarity Restore. P2→P3 |
| Frank C2 | "no errors/bugs" vs structured outcomes | REJECT (for philosophy) | Keep identity copy; structured-outcomes nuance lives in runtime docs. P3 if anywhere. |
| Frank MQ1 | clean-compile missing Restore exception | QUALIFY (runtime docs) | Out-of-contract carve-out in runtime docs (P3); philosophy line-35 only if Phase 2 finds Restore bypasses. |
| Frank MQ2 | missing "proof-engine gap" qualifier | REJECT | A gap is a bug, not a standing qualifier. P1 records the rejection. |
| Frank MQ3 | "structurally impossible" missing ingress layer | TIGHTEN (runtime docs) | The P4 insertion explains governance properly; do **not** concede "some impossibilities are runtime." Runtime-doc tighten in P3. |
| Frank MQ4 | Restore bypasses contract | RESOLVE | = Restore; out-of-contract framing. P2→P3 |
| Frank recs | direct philosophy.md edits | owner-gated | Several rejected (A3, A4-soften, MQ2); P4 owner-authored. |
| runtime-api:654 | "ingress catches cases the proof engine cannot prove statically" | TIGHTEN | Re-scope to: governance enforcing *declared* constraints on external input. P3 |
| fault-system:278/316 | "...or a proof engine gap" | TIGHTEN | Split out-of-contract-boundary (legit) from proof-gap (bug). P3 |

## Open decisions (gate the phases)

- **D1 — Where the canonical contract lives.** Options: (a) new spec §0 subsection (principles-adjacent) for the §1–4 statement + detailed mechanics in `compiler-and-runtime-design.md`; (b) all of it in `compiler-and-runtime-design.md` with a one-paragraph pointer from spec §0 and philosophy; (c) a dedicated `docs/guarantee-contract.md`. Recommended: (a) — keep the principle-level composition statement beside the principles, mechanics in the compiler/runtime design doc, everything else references it. *Land in P1.*
- **D2 — Philosophy wording sign-off (owner-gated).** ✅ **Resolved 2026-06-02** — owner approved the single Phase-4 insertion verbatim (carries-proof + composition + "not a second line of defense"); keep lines 49/51/53-rest/57; reject the A3/A4-soften/MQ2/MQ3/C2 changes. Landing still sequenced after Phases 1–3. Line-35 caveat remains contingent on the Phase 2 Restore finding.
- **D3 — Restore truth.** Does `Restore` re-validate constraints (per runtime-api + `RestoreConstraintsFailed`) or bypass (per evaluator `FromJson`)? Resolved by Phase 2 code-grounding before any doc edit. *Land in P2.*

## Phase 1 — Lock the canonical contract statement (heavyweight)

**Goal**: a single canonical articulation of the §1–4 contract, placed once, that every other doc references.

**Steps**:
1. Decide D1 (placement) with the owner.
2. Draft the canonical statement at the chosen home: the four parts above, plus the "carries proof" framing and the explicit prove-or-reject / never-defer rule.
3. Add the **composition statement** to spec §0 (the bridge between P6 governance and P10/11 fault-freedom) — the one genuinely-missing principle-level sentence.
4. Record the two rejections (Frank A3, MQ2) and the no-P11-reword decision inline, so future readers see them settled.
5. Cross-reference targets enumerated for Phase 3 (every doc that currently re-states a guarantee points here instead — pointer philosophy).

**Exit criteria**:
- Canonical statement exists at one location; D1 closed.
- spec §0 carries the composition sentence; owner has approved any philosophy-adjacent wording.
- The §1–4 statement explicitly contains: prove-or-reject; carries-proof; governance-is-not-deferral; prevention-via-atomicity; proof-gap-is-a-bug; out-of-contract is the only legitimate trap trigger.

**Doc-update obligations**: spec §0 (composition sentence + pointer); the chosen canonical home (D1).

## Phase 2 — Empirical grounding (stub — TBD pending Phase 1)

**Goal**: verify, per fault class, whether the code matches the contract (prove-or-reject) or drifts (skip/defer) — the contract is the *target*; this surfaces the honest gap + bug backlog and resolves D3.
**Scope**: divisor (expect ✓ per spec:204), overflow/bounds (BUG-017 ✗), sqrt, empty-access, count, maxplaces; whether ingress enforces declared *value-level* constraints; whether Restore re-validates.
**Decisions required**: none (read-only; produces D3 answer + bug entries).
**Effort**: M (2–3d).

## Phase 3 — Propagate to canonical compiler/runtime docs (stub)

**Goal**: every canonical compiler/runtime doc states the contract or references it; the drifted framings are tightened.
**Scope**: spec §3A (ingress-ordering: constraints on external input enforced before dependent computation); `proof-engine.md` (prove-or-reject intro); `fault-system.md` (split out-of-contract from proof-gap); `runtime-api.md` (re-scope three-layer / ingress sentence; resolve Restore per D3); `evaluator.md` (reconcile arg-gate + FromJson/Restore).
**Decisions required**: D3.
**Effort**: M–L (3–5d).

## Phase 4 — Consumer-facing docs (owner-authored)

**Goal**: philosophy.md states the composition (the one genuinely-missing connection); README routes to the canonical contract. Philosophy is already ~90% contract-aligned (line 53 already states prove-or-reject; line 51 already states governance + atomicity) — so this is **one surgical insertion, not a rewrite**, and the audit softenings are explicitly rejected.

**philosophy.md — the single approved insertion** (owner-approved wording 2026-06-02). Insert into line 53 immediately after *"…what would make safety provable."*:

> When a calculation depends on a value supplied at runtime — an edited field or an event input — the compiler does not guess at that value. It requires the value to *carry* a constraint sufficient to prove the calculation safe, and the engine enforces that constraint the moment the value enters. The compiler proves a structural fact — that the value carries its constraint — never the value itself; the runtime enforcement is what makes that carried constraint true, not a second line of defense.

This states carries-proof + composition + forecloses the defense-in-depth misreading, in one paragraph.

**Keep unchanged**: lines 49 ("no errors. no bugs." — defensible identity copy), 51, the rest of 53, 57.

**Explicitly rejected** (would damage correct text): Frank A3 (prevention-is-detection — atomicity makes rejection prevention), A4 softening ("within current proof coverage" — legitimizes proof gaps), MQ2 (proof-gap qualifier — a gap is a bug), MQ3 ("some impossibilities are runtime" — conflates governance with deferral), C2 rewrite (structured-outcomes nuance belongs in runtime docs).

**Contingent — line 35 ("no code path that bypasses the contract")**: leave as-is unless Phase 2 finds Restore bypasses constraints. If it does, fix Restore (preferred) or add one honest caveat — finding-driven, not pre-written.

**README.md**: add a pointer from the guarantee language to the canonical contract (D1 home). No claim changes.

**Decisions required**: D2 (philosophy wording — now approved; landing sequenced after Phases 1–3).
**Effort**: S (1d).

## Definition of done

- The §1–4 contract is canonical and single-sourced; all guarantee-claim docs reference it rather than re-stating it.
- No doc contains the deferral framing ("runtime catches what the proof engine can't prove" as fault-safety), the "can't prove anything about external values" phrasing, or "proof-engine gap" as accommodated defense-in-depth.
- The Restore contradiction is resolved against code.
- Empirical gaps (BUG-017 + any siblings) are filed; docs state target honestly without softening the guarantee.
- A future "is this compile-time or runtime?" question resolves by pointing at one section.

## Plan update protocol

Spike-branch mode — this plan doc is the hub. Pause and report at each phase boundary. Philosophy/README edits (P4) require explicit owner sign-off before landing. Update on phase completion with commit hashes.
