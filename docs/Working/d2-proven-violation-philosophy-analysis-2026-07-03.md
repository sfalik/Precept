---
status: Draft analysis — 2026-07-03
author: research/analysis pass (owner-requested philosophy-alignment review of owner decision D2)
topic: Does D2's "proven-violation warning" for non-safety declared bounds align with Precept's "prevention, not detection" philosophy, or is it a genuine gap?
scope: Provenance + philosophy-alignment ONLY. Does NOT change D2 (owner-locked). Surfaces a crux for owner deliberation.
grounding: docs/philosophy.md; docs/Working/compiler-readiness-plan-2026-06-16.md §2 (D2); research/architecture/compiler/fault-floor-definition-2026-06-11.md; docs/Working/compiler-readiness-plan-2026-06-16-frank-review.md (A6); docs/compiler/diagnostic-system.md; live source (Diagnostics.cs)
---

# D2 "proven-violation warning" vs. "prevention, not detection" — provenance and philosophy-alignment analysis

> The owner asked: D2 reclassifies non-safety declared bounds (min/max, length, count — `OutOfRange`/`LengthBound`/`CountBound` + the PRE0078 band check) from prove-or-reject down to a **compile-time warning that fires only on a proven violation**. When the compiler can actually prove the definition breaks its own declared bound, it emits a warning and compiles clean. Does that align with "prevention, not detection" and "invalid configurations are structurally impossible," or is it a gap? This analysis reads the sources and reports; it does not defend or attack the decision, and it does not change D2.

---

## 1. The provenance chain — how we arrived at "proven-violation warning"

Plain language, step by step, each step cited:

1. **The compile-time-niche decision** (referenced in `fault-floor-definition-2026-06-11.md:15`) set the strategy: a **fault floor** that is prove-or-reject, and a separate **universal/flag layer** that is flag-sound and post-runtime. The owner "suspected the engine had over-stepped the floor (band-containment rejections that are really rule-preservation)" and commissioned an exhaustive floor definition to ground a new readiness plan.

2. **The fault-floor research** (`fault-floor-definition-2026-06-11.md`) ran two independent fault-taxonomy adjudications plus a red-team, applying a single test to each fault code: *can it compute?* A "fault" is defined as "a mid-evaluation abort with no recoverable typed outcome (only the `Faulted` trap); anything the working-copy discard or ingress refusal handles (`ConstraintsFailed`/`InvalidArgs`/...) is governance" (`:122`).

3. **The band findings.** Against that test, the research adjudicated the band codes as **rule-misclassified-as-fault** (its § Findings and § Scale-backs):
   - `OutOfRange` (`:76-78`): "an out-of-band value computes, stores, compares; every runtime lane refuses recoverably ... No can't-compute referent." It further noted the prover "already has flag posture (fail-open on undecidable magnitude ...), internal evidence the band family was never floor."
   - `LengthBoundViolation` (`:80-82`): "no string operation is undefined by length ... Length bounds discharge NO fault obligation, so they exit the floor with no carries-proof residue."
   - `CountBoundViolation` (`:84-86`): "add-to-full computes; sweep refuses recoverably; the code's own comment calls the runtime trap 'a defense-in-depth backstop, not the boundary enforcer.'"
   - `PRE0078` band containment (`:74`, `:175-178`): the current check "Error on provable violation AND on merely-unprovable intervals" while the message falsely claims "exceeded the representable range"; the *representability* half is a true fault with no obligation anywhere, but the *declared-band* half is rule sugar.

4. **The recommended scale-back** (`:173-207`, § Scale-backs) was therefore: for **non-proof-carrying fields only**, reclassify these checks to **"proven-violation warning" + defer-to-governance** — "proven violations → flag-sound warning (provably-dead row); unprovable → no compile diagnostic; enforcement → governance sweep" (`:181`). The research introduced the **carries-proof boundary** as the real dividing line — "NON-PROOF-CARRYING standing constraints exit; proof-carrying ones do not" (`:132`) — so `mincount`'s `count>0` discharge and sign-modifier/rule-fact preservation stay in the floor.

5. **Owner decision D2** (`compiler-readiness-plan-2026-06-16.md:203-213`) adopted this as a split: the **compiler half** (reclassify to a proven-violation warning; trim the registry; amend the spec) is in scope; the **runtime half** (the governance sweep that actually enforces the bounds at each commit/ingress) is deferred and captured in §11. D2 cites the scale-back findings as precedent (`:212`).

6. **The Frank adversarial review — finding A6** (`compiler-readiness-plan-2026-06-16-frank-review.md:113-118`) supplied the load-bearing argument that makes D2 philosophically presentable: **"prevention relocated, not weakened."** A6 draws the line explicitly (`:115-116`): "**Weakening** = a configuration that *was* structurally impossible becomes possible ... **Relocation** = the *same* impossibility is enforced, at a different pipeline point; the invalid configuration stays impossible." Frank verified three facts and concluded (`:118`): "genuine relocation, honestly drawn, and correctly held distinct from the D1 relaxation."

7. **The readiness plan** applies all of this: Slice 1.3 (`:397-406`) reclassifies the three band checks to a proven-violation warning for non-proof-carrying fields, with the explicit exit criterion that "an out-of-band write on a non-proof-carrying field **compiles clean**" and "a **provable** band violation emits the proven-violation **warning**." The runtime enforcement is captured in §11 as `RUNTIME_CODE_DEFER` with an `evaluator.md §7.6` pointer.

**Net:** the chain is *can-it-compute adjudication → "bands are recoverable rule sugar, not can't-compute faults" → reclassify non-proof-carrying bands to proven-violation warning + defer enforcement to runtime governance → A6 "relocation, not weakening" justifies it → plan Slice 1.3 builds the compiler half.*

---

## 2. The stated rationale — the strongest version of WHY

Three arguments, quoted, that together form the case for D2:

**(a) The over-rejection argument (D2's own rationale, `:210`).** "the band lane was over-reach — the compiler was rejecting merely-unprovable intervals, not just proven violations. Downgrading to a proven-violation flag removes the false rejections without losing the genuine prevention signal." The point: prove-or-reject on bands was rejecting *valid* definitions (ones whose bounds it merely could not prove), which is itself a philosophy problem — Precept should not reject a definition it cannot prove *unsafe*.

**(b) The relocation argument (Frank A6, `:118`; D2 tradeoff, `:213`).** From D2: "**prevention is relocated, not weakened** ... for non-proof-carrying bands and for values arriving at ingress, the future runtime's governance refusal is still atomic prevention; the write is refused whole." From A6 (`:118`): "atomic refusal **is** prevention. So for those values D2 is **genuinely relocation**: prevention moves from a compile-time over-rejection (which was not real prevention of the value) to a runtime-governance atomic refusal (which is)." This rests on `philosophy.md:35` — governance "enforces that declaration structurally, on every operation, with no code path that bypasses the contract" — and `philosophy.md:55` — "the runtime enforcement is what makes that carried constraint true, not a second line of defense."

**(c) The enumeration argument (A6, `:118`; D2, `:213`).** "bands were never a `philosophy.md` compile-time-impossibility claim — L53's enumerated impossibilities are divisor / overflow / empty-collection, all *retained* floor families; bands are not in that list, so the band relocation needs **no** `philosophy.md` edit at all." Verified: `philosophy.md:53` enumerates exactly "Division by zero, arithmetic overflow, empty collection access" as compile-time impossibilities; declared-bound containment is not named there.

**(d) The honesty/can't-compute argument (research, `:122`, `:76-86`).** A band violation is not a "fault" under Precept's own fault definition — an out-of-band value *computes, stores, compares*; the runtime refuses it *recoverably* (`ConstraintsFailed`), not by aborting mid-evaluation. Keeping it in the prove-or-reject fault floor was a category error, and the current PRE0078 message ("exceeded the representable range") is "factually false in the band case" (`:74`).

---

## 3. The philosophy tension — both cases, cited verbatim

### 3a. The case that D2 ALIGNS

- **Bands are outside the enumerated impossibilities.** `philosophy.md:53` (verbatim): *"Division by zero, arithmetic overflow, empty collection access — these are not risks managed at runtime. They are compile-time impossibilities. If the compiler cannot prove an expression safe given the declared constraints, it rejects the definition."* Declared min/max/length/count bounds are not in this list. The enumerated set is exactly the retained fault floor.

- **Prevention can legitimately live at the enforcement boundary, not only at compile time.** `philosophy.md:55` (verbatim): *"the runtime enforcement is what makes that carried constraint true, not a second line of defense."* Precept's own model already locates prevention *partly* at runtime for carried constraints — so a band prevented by runtime governance is not, by itself, a departure.

- **Governance is atomic and unbypassable — atomic refusal is prevention, not detection.** `philosophy.md:35` (verbatim): *"Governance declares what the data is allowed to become and enforces that declaration structurally, on every operation, with no code path that bypasses the contract."* And `philosophy.md:59`: *"with no window in between where an invalid combination can exist."* If the runtime sweep refuses the out-of-band write atomically (working copy discarded), the invalid configuration never persists — which is the relocation, not weakening, claim.

- **A band violation is a recoverable governance outcome, not a can't-compute fault.** The research (`:122`) grounds the fault/governance split in the runtime outcome shape; a band violation yields `ConstraintsFailed`, a first-class recoverable outcome, exactly the kind of enforcement `philosophy.md:19` describes ("commits only if every constraint holds. If any fails, the transition is rejected — the invalid configuration never exists").

- **Corroborating internal-consistency point (see §3b for the counter-weight):** a **provably-dead** structural case is *already* a warning in shipped canon. `UnsatisfiableGuard` (PRE0082) — a guard the compiler proves can never be true, i.e. a transition row that can never fire — is `Severity.Warning`, not Error, verified at `src/Precept/Language/Diagnostics.cs:770` ("this row can never fire"). So Precept *already* treats a proven structural certainty (a dead row) as a *report*, not a rejection. On this axis, a proven-violation band as a warning is **consistent** with existing treatment, not an anomaly. (This directly corrects the premise in the owner's question that "Precept rejects a provably-unsatisfiable guard" — at HEAD it does not; it warns.)

### 3b. The case that D2 does NOT align

- **A provably out-of-band value IS an "invalid configuration."** `philosophy.md:43` (verbatim): *"Invalid configurations are structurally impossible. A valid entity is simply one where every constraint holds for its current configuration."* A declared `min`/`max`/`length`/`count` bound is a declared constraint on the configuration. A value the compiler *proves* violates it is, by this definition, an invalid configuration — and D2 ships the definition that produces it with a warning.

- **"No operation can produce a result that violates a declared rule."** `philosophy.md:49` (verbatim): *"No operation can produce a result that violates a declared rule — the invalid configuration is not reachable."* A proven band violation is precisely a case where the compiler has demonstrated an operation *does* produce a rule-violating result. Emitting a warning and compiling clean is the compiler certifying-with-a-caveat exactly the thing this sentence says is not reachable.

- **The definition of prevention-not-detection reads against a warning.** `philosophy.md:49` (verbatim): *"not what bugs you try to catch, but what bugs cannot exist."* A compile-time warning on a proven violation is a caught bug that still exists in the shipped artifact — the shape of detection, not prevention. The compiler here can prove the definition *un*safe (not merely fail to prove it safe), yet does not reject — the inverse of `philosophy.md:53`'s "If the compiler cannot prove an expression safe ... it rejects."

- **Internal inconsistency #1 — the definition-incoherence Error family.** The plan RETAINS Error severity for the *default-value* band violation. `OutOfRange` on a default (PRE0079) is `Severity.Error`, verified at `Diagnostics.cs:697` ("Default value {1} for field '{0}' violates declared '{2}'"), and the plan keeps it Error under a "definition incoherence" identity (`:416`, GATE-H `:278`). So: **a default that provably violates its own bound = Error (rejected); a set-action that provably violates the same bound = Warning (compiled).** Both are provable, both guarantee an invalid configuration is produced; the only difference is *when* (creation vs. transition). Whether that "when" is a principled line is a real question — the philosophy's guarantee (`:59`) is explicitly "not just at creation, but through every transition, every direct edit, and every operation."

- **Internal inconsistency #2 — the plan's own cross-transition-ensure reasoning.** The plan flags a "provably-unsatisfiable cross-transition ensure ... where every fire is statically guaranteed to fail" as **"genuine prove-or-reject territory: the definition is incoherent regardless of runtime, and a complete runtime cannot rescue a never-satisfiable ensure"** (`:1097`, `:1423`), routing it to candidate compile-time definition-incoherence Error. A set-action band that the compiler proves is violated *for all inputs* to that action (e.g. a literal `set F = 200` on a `max 100` field) is the **same shape** — an operation that can never succeed, which no runtime can rescue — yet D2 routes it to a warning. The plan applies opposite dispositions to the same structural shape.

- **The all-inputs vs. some-path distinction is not drawn.** D2 says only "the compiler can actually demonstrate the bound is broken" (`:207`); it does not distinguish "proven violated for **all** inputs to this operation" (a dead/always-failing operation, ≈ definition incoherence by the plan's own §3b-#2 logic) from "proven violated on **some** reachable path" (a governance concern the runtime refuses per-instance). Both collapse into one "proven-violation warning." The owner's third sub-question — does the plan distinguish these? — resolves to **no**, and that is where the sharpest inconsistency lives.

---

## 4. The crux question(s) the owner must decide (stated neutrally)

1. **Is a provably-violated non-safety declared bound an "invalid configuration" in the `philosophy.md:43` sense (which says such configurations are structurally impossible), or is it a governance concern that the runtime sweep prevents atomically at enforcement time (`philosophy.md:35`/`:55`)?** The whole disagreement reduces to which of these two philosophy passages governs a declared band. A6 answers "governance/relocation"; the owner's concern answers "invalid configuration."

2. **Should the disposition split on all-inputs vs. some-path rather than on proven-vs-unprovable?** Concretely: should a band the compiler proves violated *for every input to an operation* (a dead/always-failing operation) be an **Error** — as the plan's own cross-transition-ensure reasoning (`:1097`) already argues for the ensure case and as definition-incoherence already does for the default case (`:416`) — while only the *some-reachable-path* case is left to runtime governance? D2 currently does not draw this line.

3. **Is "proven-violation → warn" the right shape at all, or is the real fix "proven-violation → reject, merely-unprovable → compile clean"?** This variant would keep D2's genuine correction (stop over-rejecting *unprovable* intervals — the `:210` rationale) while preserving prevention where the compiler has *positive proof of violation* (never ship a definition the compiler has proven breaks a declared constraint). It differs from D2 only in the disposition of the *proven* case: reject instead of warn. Note the tension with §3a's corroborating point — `UnsatisfiableGuard` (a proven-dead row) is a warning today, so "proven structural certainty → reject" is not currently how the whole hygiene family behaves; adopting option 3 for bands would make bands stricter than dead rows.

4. **Does the runtime half being deferred change the answer?** A6's "relocation, not weakening" depends on the runtime governance sweep actually existing to make the refusal atomic. Today the runtime is a stub and the enforcement is deferred to §11. During the deferral window the band is enforced *nowhere* (D3 accepts this as harmless pre-release). Is the relocation argument sound as a *design commitment* even while the enforcement point does not yet exist — i.e. is prevention "relocated" or merely "promised"?

---

## 5. What this analysis does NOT settle

- **D2 is an owner-locked decision** (`compiler-readiness-plan-2026-06-16.md:186`: "They are settled ... nothing may reopen them"). This analysis does not change it, and per CLAUDE.md a locked decision cannot be overridden inside an analysis or design pass — only the owner authorizes that.
- **It does not edit `philosophy.md` or the plan.** The philosophy reconciliation D2 touches is already owner-gated in the plan (GATE-F / DoD-9); this document only surfaces a possible gap for that conversation.
- **It does not adjudicate the crux.** §4 states the questions neutrally; it does not recommend option 1, 2, 3, or 4.
- **It does not re-audit the fault-floor research's per-code adjudications.** It takes the research's "bands are recoverable, not can't-compute" finding as given and reasons about its philosophy consequences; a reader who disputes that adjudication would reopen §1 step 3.
- **The runtime-outcome claims are about designed, not observed, behavior.** The runtime is a stub (`Evaluator.cs` all-five `NotImplementedException`); "the governance sweep refuses recoverably" is a design contract in `evaluator.md §7.6`, not observed execution.
