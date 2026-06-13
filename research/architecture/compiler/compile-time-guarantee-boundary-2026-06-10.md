---
status: Active — feeds the guarantee-statement design (replaces docs/Working/compile-time-guarantee-statement-draft-2026-06-10)
authored: 2026-06-10
author: research-ultra (4 independent derivations + cross-validation)
topic: The right compile-time/runtime boundary for Precept's guarantee — how far the proof engine must go — derived from philosophy + proposed runtime + visual-system UX
external-engagement: partial — internal canon is decisive; external precedent (Dhall, SPARK, Idris, Liquid Haskell, CUE) is an informative bracket only
---

# The Compile-Time Guarantee Boundary

> How far must Precept's proof engine go, and what must be **proven at compile time** versus **governed at runtime**? Answer derived from Precept's own philosophy, proposed runtime, and visual-system UX — not from what other tools do.

## Background

The owner's struggle: "if some amount of guarantee is governed at runtime, why have the compile-time flexibility at all — nail down what's needed so we know how far to take the proof engine." Two framings were explicitly excluded as wrong: (a) "faults must be compile-time because the runtime can't catch them" (false — a runtime check-and-rejects a zero divisor at ingress just fine); (b) "the proof engine's job is to move operations from *possible* into *certain*/*impossible*" (a misread of the tri-state). This research replaces the off-base `compile-time-guarantee-statement-draft-2026-06-10`.

## Methodology

- **Research question:** where the compile-time/runtime boundary sits and how far the proof engine must go, derived from philosophy + proposed runtime + visual-system UX.
- **Search strategy:** four independent derivations — UX-first, philosophy-first, runtime-first, external-informed — over `docs/philosophy.md`, `precept-language-spec.md` §0.6/§0.7/§0.8/§3A, `docs/runtime/*`, `src/Precept/Runtime/Inspection.cs`, `design/system/*`, `design/prototypes/inspector-preview-v1.html`, plus web precedent. Load-bearing citations (the `Prospect` tri-state, §0.7 split) extracted by two agents independently and cross-checked.
- **Exclusions:** the off-base draft and the unlocked guarantee-cluster strawmen were denylisted to prevent inherited framing (verified: no agent read them).
- **Source grade:** internal canon **Primary** and decisive; external precedent **Secondary**, informative only.
- **Time bounds:** 2026-06-10. The evaluator is **stubbed**, so all runtime-mechanism evidence is design intent + a working prototype + §3A.6, not observed behavior.

## Findings

### The boundary is two mechanisms divided by **kind of obligation**, and canon draws it explicitly

> "**Fault prevention — established entirely at compile time.** … It never compiles a fault-prone operation in the hope a runtime check catches it; **there is no deferral.**" — spec §0.7:266
>
> "**Governance — enforced at runtime on external input.** Every declared constraint is enforced on every value entering the entity from outside the definition … at the moment it enters. … **this is prevention, not detection.**" — spec §0.7:268
>
> "The proof is therefore complete at compile time and the fault never occurs — **proven by the compiler, its precondition discharged by governance**, nothing left to a runtime check." — spec §0.7:270

So the split is not a "compile everything" cut; it is two distinct mechanisms that compose. The compiler proves a **structural fact** — "the operand carries a *sufficient* constraint" — **never the concrete value** (philosophy.md:55, verbatim). Governance makes that carried constraint **true** of the concrete value at ingress.

### The decision principle (the single rule)

A check is **compile-time** iff it is decidable **from the definition alone** and **no runtime input could ever repair its failure**; it is **runtime** iff it depends on a concrete external value not present until ingress. Equivalently, in the evaluator's own two failure categories:

- A **FAULT** — an aborted plan with no graceful, recoverable outcome; it fires mid-execution (inside `ExecuteAction`/`EvaluatePlan`) *before any constraint verdict exists*, leaving only `Faulted(fault)` as a defense-in-depth backstop (evaluator.md:648-653) — must be made **impossible at compile time**.
- An **OUTCOME** — a recoverable typed result (`Unmatched`/`ConstraintsFailed`/`Rejected`) produced by working-copy discard — is **governed at runtime**.

### What compile-time must prove-or-reject (no deferral)
1. **Evaluation-fault freedom** at every fault-prone site reachable from contract data (division-by-zero, overflow, empty-collection access, bound/length containment, stack depth) — discharged by a carried-constraint obligation **or the definition is rejected**.
2. **Carried-constraint *sufficiency*** — the structural fact that a runtime value carries enough constraint to discharge every downstream fault obligation. *This is the half governance cannot supply:* ingress makes a constraint true; only the compiler proves it is **sufficient**, so a value cannot pass ingress and still fault three operations later (philosophy.md:55; spec §0.7:270).
3. **Whole-definition structural/process soundness** the runtime never computes because it executes one instance: reachability, dead-ends, required-state dominance, type soundness, no contradictory/unsatisfiable/vacuous rules (philosophy.md:57).
4. **The default-configuration check** — a definition whose own declared defaults violate a declared rule, which no external input can ever repair (bounds-only:140). The one business-rule slice forced to compile-time.

### What runtime governs
- Whether a specific **external value** satisfies the declared constraints — event args, construction inputs, edits — enforced at ingress on a working copy **discarded on failure**, so an invalid configuration never persists even transiently. Still **prevention** via §3A.4 atomicity (spec:1965), not detection.
- Which **transition row** a concrete instance selects (first-match-wins). The graph analyzer deliberately over-approximates (all edges traversable) because guard satisfaction is a runtime-data question (spec:191).
- The honest per-instance **Certain/Possible/Impossible** inspect verdict.

### The line is the contract-input ingress surface
> "The guarantee covers every value entering through the contract — construction, events, edits. **Data that enters outside the contract is not re-proven.** Restored state is trusted as valid … hydration is fast and does not re-validate." — spec §0.7:272

### The tri-state + debounced inspect — the linchpin (and the correction)

`Prospect { Certain, Possible, Impossible }` (Inspection.cs:6, doc-comment **"Row-level certainty in first-match routing"**) is **not** a proof-engine reframing. It is the **runtime's honest, per-instance, partial-information answer**. "**Possible**" is a first-class, designed-in outcome with exactly two constitutional sources:
- a **missing arg** → Kleene `Unknown` → `Possible` (evaluator.md:874-877: `true→Certain, false→Impossible, Unknown→Possible`);
- genuine **first-match ambiguity** (multiple `Certain` rows) → `Possible` (evaluator.md:885).

"Possible" means **"awaiting external input / ambiguous"** — *never* "the engine gave up."

Read backward, the **debounced inspect-on-keystroke** (prototype: 30 ms arg loop / 140 ms draft loop) is the load-bearing evidence for where the boundary sits. The inspector **simulates the full guard/action/computed/constraint machinery on a working copy on every keystroke** (evaluator.md:818-825). For the live verdict to be **honest** (§3A.6:2119, "the inspection result matches what execution would produce"):
- **"Certain" must never fault on Fire** → requires fault-freedom proven at compile time, else the live inspect *itself* crashes and a Certain verdict is a lie.
- **The outcome space must have no phantom/vacuous rows** → requires structural soundness proven at compile time.
- **"Possible" must denote only input-contingency** → requires prove-or-reject **soundness**, so every analysis gap becomes a *compile rejection* (nothing to inspect), never a dishonest "Possible."
- **But the existence of the "Possible" bucket proves the compiler must NOT decide everything.** If it decided every outcome, there would be nothing to inspect. The per-keystroke UX *presupposes* outcomes that stay open until input arrives.

**Therefore the proof engine must reach exactly this far and stop:** far enough that the only thing left "unknown" to live inspection is **genuinely-external input not yet supplied** — never an analysis the engine declined to perform.

### How far is capped — three converging limits
Soundness-over-completeness + proven-violations-only + **no opaque solvers** (spec §0.6:221/223/225 — "SMT/Z3 … excluded even when they could prove more: opaque proof witnesses violate the inspectability commitment") keep proof conservative and **legible**; the decimal-number-model drift hazard couples any proven concrete-value fact to the runtime's number model; the **domain-expert audience** forbids unexplainable verdicts.

## Implications for Precept

- **Resolves "why compile-time if runtime governs":** not because the runtime can't reject input — it can — but because (1) a fault has no graceful runtime outcome (it aborts mid-plan before any verdict), (2) carried-constraint *sufficiency* and whole-definition topology are facts no single-instance run can produce yet are exactly what make every live inspect verdict honest, and (3) prove-or-reject-with-no-deferral is a **commitment** — the guarantee travels *with* the clean-compiled definition.
- **Converges with the obligation-position thread:** the rule "an obligation at every fault-prone site, gated only on *discharge*, never on *creation*" (compiler-and-runtime-design.md:104) is the precondition for the inspector's "Possible" bucket to stay clean. The 10 live fail-open holes found earlier are violations of *this* boundary — the same property from two angles.
- **The proof engine's relational power is driven by the fault floor**, not by proving business rules: it needs enough (interval + two-variable relational, narrowing) to discharge fault obligations and carried-constraint sufficiency — *not* to prove arbitrary rule preservation.

## Conclusions

**Conclusion: the boundary is the contract-ingress surface, with obligations split by kind — faults + structural soundness + carried-constraint sufficiency + the default-config check are prove-or-reject at compile time; concrete-value satisfaction, row selection, and the per-instance inspect verdict are governed at runtime. The proof engine goes exactly far enough that the only thing "unknown" to live inspection is external input not yet supplied.**

- **Rationale:** four unrelated derivations (UX-honesty backward, philosophy's two-mechanism split, the evaluator's two failure categories, external precedent) converge on the same line, and canon draws it verbatim (§0.7:266/268/270/272).
- **Alternatives rejected:** full value-folding/SMT (collapses the "Possible" bucket the inspector needs; opaque, violates inspectability); the Dhall pole (remove fault-prone ops — unavailable to an expressive business-rule language with external input); the Nickel pole (defer faults to runtime — breaks §3A.6 inspect-honesty; §0.7 forbids deferral); gating obligation *creation* on provability (soundness inversion → false "Possible").
- **Precedent:** internal canon decisive (cited above); external bracket — SPARK proves each fault site (Precept matches, keeps the trap as defense-in-depth); Idris evaluates "only total functions during type checking," necessarily conservative (Precept's §0.4 purity/finiteness is the decidable analogue); Liquid Haskell discharges carried preconditions "with no runtime cost," minus the opaque solver.
- **Tradeoff:** a clean compile guarantees fault-freedom + structural soundness, **not** that a given instance's data is valid (that lives at runtime, via Inspect) — public copy must not collapse the two mechanisms into "the compiler proves your data valid" (Principle 8 overclaim). And soundness-over-completeness means the conservative prover will sometimes reject a safe-but-unprovable definition; the author adds a carried constraint rather than getting "probably fine."

## What would change this conclusion
- If the owner removed runtime business-rule governance and required every constraint statically decidable (the Dhall pole) — collapses Axis 2 and the "Possible" bucket.
- If §3A.6 inspection-honesty were relaxed (inspection merely advisory) — the UX-first leg falls; deferring faults to runtime would become tolerable.
- If the owner authorized SMT/Z3 (overriding spec §0.6:225) — pushes the line further at the cost of inspectability (Tier-3 override).
- If the default-config check proved relocatable to construction-time governance — Axis 1 becomes purely fault + structure.

## Open Questions
- Is the proof engine's obligation-position coverage actually complete (the 10 live fail-open holes — the *other* thread)? The boundary-as-designed is sound; this decides whether it's *honored*.
- Do the compile-time value-fold and the runtime `BinaryExecutors` share the exact decimal number model? If not, a "Certain" verdict can diverge from Fire (Principle 11 seam hole).
- What is the canonical `Prospect` → visual-surface vocabulary mapping? The manifest flags it open; the live-UX leg rests on a provisional prototype taxonomy.
- Can the default-config check move to construction-time governance under any construction semantics, or is it irreducibly compile-time?
- Once the evaluator ships, does the live debounced inspect behave as the prototype + §3A.6 specify?

## Threats to Validity
- The "how the tri-state drives UX" leg rests on **design intent + a working HTML prototype + §3A.6**, not shipped behavior (evaluator stubbed; LS inspect handler design-only; preview webview a placeholder). The boundary *conclusion* is locked against committed canon; the inspect UX is not observed.
- The `Prospect` → surface-vocabulary mapping lives in prototype JS, not canonicalized; the manifest calls the data-form field-status model "the most unresolved of the three target surfaces."
- Whether the proof engine *as built* honors obligation-position completeness is **not** verified here (taken as given; prior work found live holes).
- The decimal-number-model drift hazard is correctly identified but the two number models were not diffed against source.

## Sources
- `docs/philosophy.md:43/51/53/55/57/61` — Primary (internal).
- `docs/language/precept-language-spec.md` §0.6:221/223/225, §0.7:266/268/270/272, §0.8, §3A.4:1965, §3A.6:2119 — Primary (internal).
- `src/Precept/Runtime/Inspection.cs:6,9`; `docs/runtime/evaluator.md:648-653/818-825/874-877/885`; `docs/runtime/runtime-api.md`; `docs/runtime/result-types.md` — Primary (internal).
- `docs/compiler-and-runtime-design.md:104` — Primary (internal).
- `design/system/semantic-visual-system-manifest.md:153`; `design/prototypes/inspector-preview-v1.html` — Primary (internal, design intent / prototype).
- `research/architecture/compiler/bounds-only-constraint-enforcement-2026-06-05.md:140` — Primary (internal).
- External bracket (Secondary, informative only): Dhall totality docs; SPARK proof model; Idris totality ("only total functions during type checking"); Liquid Haskell refinement-at-call-site; CUE. Cross-linked from `design/system/` for the UX dimension.
