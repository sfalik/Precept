# Design-Revision Notes — companion to `compiler-readiness-plan-2026-06-16.md`

**status:** Draft notes — 2026-06-16
**purpose:** two things the main plan references but does not inline —
1. **§ A** — the concrete edits the grounding research doc `research/architecture/compiler/fault-floor-definition-2026-06-11.md` needs so it stops carrying the parked/atomic framing the plan superseded (reconcile-before-feeding-agents discipline).
2. **§ B** — the consolidated runtime-canon capture-obligation list: every `docs/runtime/*.md` revision the compiler plan forces, so the deferred runtime build inherits a complete spec. Nothing is lost.

These notes are owner-reviewable and must land **before** any Phase-1+ slice begins (the research doc grounds the slices; stale framing would re-inject parked work).

---

## A. Fault-floor research-doc edits (`research/architecture/compiler/fault-floor-definition-2026-06-11.md`)

Apply D1 (overflow parked), D2 (band split), D3 (atomicity relaxed), and Frank's review fixes to the research doc itself. Each row below is a concrete edit.

1. **Status field (Frank A1).** Update the doc's status to note the three applied owner decisions (overflow parked, band split compiler/runtime, atomicity relaxed) and that the readiness plan now derives from the corrected framing. Reference the 2026-06-16 plan.

2. **Floor count 11 → 10.** Representability overflow (family 9) moves OUT of the prove-or-reject floor into a new **"Known faults — accepted, parked"** sub-section. Propagate the count to the Conclusions line and every place the doc says "11 floor families."

3. **Floor invariant carve-out (the single most load-bearing reframe).** Amend "prove-or-reject, never defer" to **"prove-or-reject EXCEPT decimal/temporal representability overflow — a parked known fault we never claim prevented."** Mark this as an **owner-approved honesty carve-out**, not auto-resolved (philosophy gate). Keep it **distinct** from the band relocation (bands are relocated to runtime governance, not relaxed).

4. **NumericOverflow inversion reframe.** Drop "THE one missing obligation family / build at every sink." Keep the diagnostic of *why* the band lane was vacuous. Add a **PARKED** disposition row replacing the `[high]` gap row. Note `FaultCode.NumericOverflow` survives as a documented defense-in-depth known-fault trap with no live `[StaticallyPreventable]` guarantee behind it.

5. **GATE-A.** Move from "owner decisions the plan gates on" to a **"Parked — revisit post-runtime"** register; note decimal-without-the-obligation is the de-facto resolution.

6. **Temporal overflow.** Fold into the same PARKED known-fault register; "verify-then-build / ADD `FaultCode.TemporalOverflow`" → "document as parked known fault (same representability genus)." Keep **only** the checked-constant-fold sliver as COMPILER_CODE self-protection (so a `2e28 * 2e28` literal can't crash the pipeline). Reconcile P4-TEMP-11 to PARKED.

7. **HARD COORDINATION CONSTRAINT / "ONE coordinated move."** **Delete** (D3). Replace with simple incremental sequencing; the only retained ordering is **registry-honest-before-position-checker**. Note the wiring leg is itself deferred runtime code, so the "coordinated move" was doubly void.

8. **DesugarsToRule "zero consumers" (Frank B2).** Rewrite all 3–4 occurrences to **"zero constraint-synthesis/enforcement consumers; 2 cosmetic/display consumers exist (GrammarGen `Program.cs:173`, MCP `CatalogFormatters.cs:253+`)."** No pipeline stage synthesizes it into a TypedRule/TypedEnsure.

9. **"floor PREREQUISITE" promotion of the sweep wiring.** Demote to "deferred runtime-initiative item whose design is captured in runtime canon (§ B)."

10. **Write-aware tier-2.** Split structurally — same-transaction = compiler (in scope, Phase 3 Slice 3.1); committed-state = deferred runtime leg (§ B). No research-doc sentence may claim committed-state discharge as a compile-time guarantee.

11. **Implications refinements (1)(2)(3).**
    - (1) Drop "representability obligations enter" + "ONE coordinated move"; demote the wiring leg to deferred runtime.
    - (2) `evaluator.md §10` "no diagnostics means no evaluator faults" is now **false under parked overflow** — amend to **except the representability family** (flag the philosophy gap; the position-totality work still closes every other lane).
    - (3) Keep the flag-layer-needs-runtime point; drop the coordinated-move ordering sentence.

12. **Conclusions four-leg rationale.** Tradeoff leg changes from "representability adds bounds-demand…" to **"band scale-backs remove invented-bound friction; representability overflow accepted as a parked, documented known fault (honesty-over-completeness)."** Attach the missing rejected-alternative + tradeoff legs to the captured standing-declines (Frank §6: convex-polyhedra + predicate-abstraction off the interactive path).

13. **Provenance (Frank B3).** "43 mined obligations recovered from the triage of 32 prior docs (28 superseded + 4 archived)." Confirm the 4 archived docs' transferred obligations are present in the 43-register; reconcile any FaultCode member-count against the live `FaultCode.cs`.

14. **Double-register dedup.** The doc's Findings/Floor/Scale-backs/Registry/Conclusions re-pass (the "FF-*" lens) is **doc-revision-of-the-research-doc**, not a second set of work items — the work items live in the Gaps/Scale-backs/Registry-corrections register (FG/SB/RC). Label the re-pass explicitly so a reader cannot mistake `FF-position-totality-checker` for a duplicate of `FG-01`. Friction-reduction machinery (flow narrowing, sign-set algebra, event-ensure folds, PE-G13 taint suppression) is a **keep/verify** item with a primary home in the Phase 2/3 verification slices — not solely a doc-revision row.

---

## B. Runtime-canon capture obligations (consolidated)

Every runtime decision the compiler plan forces, grouped by target doc. **Consolidation discipline:** the `evaluator.md §7.6` obligations MUST be written as **ONE coherent constraint-plan/sweep design pass**, not many uncoordinated edits. Where two docs disagree today, the obligation is to **reconcile to one shape** and record the ruling.

### B.1 — `evaluator.md`

- **§7.6 governance / ingress sweep (ONE consolidated pass)** — constraint-plan synthesis from DesugarsToRule + per-commit/ingress enforcement; the recoverable governance-refusal lane for reclassified bands (distinct from `Faulted`); committed-state tier-2 discharge invariant (every prior commit enforces the discharging constraint); collect-all (no short-circuit) sweep; element-ingress governance (Rule 2 / AC 7); 2c-ii field-reference-bound ingress; omit-clears-on-entry + readonly/editable patch enforcement; the umbrella ingress value-constraint sweep (guarantee-contract Part 2, spec §0.7).
- **§7.1 / §7.2** — the executable model: exact EventOutcome / UpdateOutcome / EventInspection / UpdateInspection / RestoreOutcome shapes per scenario; Create verdict space (Created/Rejected); FromJson/Restore (Restore substantially dissolves into next-operation re-governance + out-of-contract trap backstop).
- **§7.5** — omit-clears + readonly/editable patch semantics.
- **§9 / §7.6 fault delivery (GATE-I)** — reconcile the in-canon contradiction: `result-types.md:51/63/72` (throw `FaultException`) vs `result-types.md:104/117/127` + `evaluator.md §7.6` (`EventOutcome.Faulted(Fault)` structured variant) vs `fault-system.md` Q1 (open). Settle to ONE shape (Decision 4 "Structured Outcomes, Never Exceptions" leans Faulted). Owner-facing — surface, don't unilaterally pick.
- **§10** — the "no diagnostics ⇒ evaluator never faults" contract, amended to **except the parked decimal/temporal representability family** (D1 honesty carve-out); plus the committed-state invariant; plus the position-totality / creation-exhaustiveness checker as the HARD runtime precondition that keeps the contract alive as the language grows.
- **Cross-unit reduction-rule section (MO-DSO-05, highest-priority extraction — never written)** — `k = ScaleToBase(u_q)/ScaleToBase(u_p)` in decimal, target-directed to the price denominator unit, rounding at maxplaces, `k=1` same-unit; covers BUG-030 value-application and the BIZ-9/10/11/12/13 derived-value computations (money/money exchangerate value, quantity/quantity compound value, inverse-division value, time-denominator cancellation value). *(Type derivation is Phase-4 compiler work; value computation defers here.)*
- **BRANCH_FALSE/BRANCH_TRUE/if-lowering opcode contract** — the conditional-totality spec half; fixes `evaluator.md:993` + the &&/||/?: notation drift at 6 sites.
- **Edge-value semantics** — banker's rounding, clamp directions, ∞/NaN, integer-conversion overflow.
- **§Intake-Boundary (extend `:297-311`)** — D8 dynamic-write dimension-narrowed target-directed conversion. **Matched pair** with the Phase-4 compile-side admission, which stays BLOCKED until this lands.
- **D9 injectable per-operation clock** — so `now()` is deterministic/testable.
- **D18 executor-module dispatch + D13a serialization; temporal execution contract** (incl. NodaTime STJ).
- **OD-1 shared expression-evaluation core** — the runtime evaluator and compile-time ConstantFold must share ONE core (also recorded in `compiler-and-runtime-design.md`).

### B.2 — `result-types.md`

- Reconcile `:51` + `:104/:117/:127` to the GATE-I single fault-delivery shape.
- Recoverable-refusal outcome shape for governance band-refusal — distinct from `Faulted`.
- `Prospect` has no `Faulted` member; the Kleene evaluator must not throw on an unresolvable subexpression (InspectFire no-fault-lane precondition).
- ConstraintsFailed shape for the collect-all sweep.

### B.3 — `fault-system.md`

- Q1 reconciled to the GATE-I shape.
- OutOfRange / LengthBound / CountBound are **no longer fault codes for the reclassified fields** — omit them from the impossible-path table for those fields.
- NumericOverflow documented as a **known-not-prevented** fault.
- Member-listing drift (RC-09): 13-vs-15 + stale `NullInNonNullableContext` partner vs `FaultCode.cs:23 UnprovedPresenceRequirement` — reconcile against the live `FaultCode.cs`.
- Modifier-desugar because-rationale in fault messages.
- Edge-value fault surfaces (integer-conversion overflow, etc.).

### B.4 — `proof-engine.md`

- Write-awareness discharge rule + GATE-B shape (intra-chain in scope; record the committed-state runtime precondition as the carries-proof contract's runtime dependency).
- Qualifier-source mutation obligation.
- BUG-030 cancellation factor.
- DesugarsToRule consumer/synthesis seam (the runtime sweep is the consumer).
- The standing declines (convex-polyhedra + predicate-abstraction off the interactive path) with full four-leg rationale.
- BUG-030 policy reconcile vs `proof-engine.md:1790`; restate the stale deleted-witness-checker line.
- Cross-transition ensure is NOT a compile-time obligation under current §0.6 — record, and flag the provably-unsatisfiable case as a candidate §0.6 definition-incoherence detection (CANON_DESIGN decision, not a clean runtime punt).

### B.5 — `business-domain-types.md`

- Qualifier-source mutation record; BUG-030 D8 record; Point C value/qualifier validation reconcile.
- D6.2 composite-period lowering; F-LANG-BIZ-11 money minor-unit boundary precision; maxplaces runtime points 2-3 (`:1581-1590`).
- maxplaces over-precision known-limitation note (compile-time precision proof parked + runtime points deferred ⇒ entirely unenforced).

### B.6 — `runtime-api.md`

- Restore / restoration surface; general ingress value-constraint governance umbrella; D9 clock seam.

### B.7 — `descriptor-types.md`

- F-API-01 typed field descriptors — solidify the descriptor surface the evaluator builds against; verify reuse of existing FieldDescriptor, not a fork.

### B.8 — `compiler-and-runtime-design.md`

- OD-1 shared core; the DesugarsToRule pipeline handoff seam.

### B.9 — `diagnostic-system.md` (compiler canon, but downstream of the band/registry reclass)

- PRE0101 routing (GATE-D); qualifier taxonomy label (GATE-C); new/retargeted fault codes; the §0.6/§0.7 three-category taxonomy (GATE-H).

### B.10 — Owner-gated, NEVER auto-edited

- **`philosophy.md`** absolute-claims reconciliation — bands relocated to runtime governance (not weakened, Frank A6); overflow genuinely a disclosed limitation (D1). Surface as a guarantee-statement promotion; the owner authorizes. The core-guarantee category changed (overflow now a disclosed limitation), so this is a genuine philosophy-gap surface, not an incidental sync.
- **GATE-F** spec amendments (§0.7 L256/L258/L266, §0.6 item-6 L208, Principle 11 "constraint range impossibility" + L112 "defensive redundancy") — Tier-3 owner-approval, enumerated complete before the conversation (Frank B1).
