---
status: Assessment — 2026-06-01
sources-consulted:
  - docs/philosophy.md
  - docs/language/README.md
  - docs/language/precept-language-spec.md (§0.1, §0.2, §0.3, §0.4, §0.6, §2.4, §3.8, §3A.1–§3A.6, §5)
  - docs/language/business-domain-types.md (`in`/`of` enforcement tiers, maxplaces scope, discrete equality narrowing, D9/D10/D14)
  - docs/compiler/proof-engine.md (Decision 2, "No Runtime Obligation Checking", "No Constraint Evaluation", FaultSiteLink/FaultSiteAnnotation)
  - docs/runtime/fault-system.md (StaticallyPreventable chain, defense-in-depth, Q3)
  - docs/runtime/evaluator.md (§7.1, §7.6, arg-validation gate, FromJson, §9–§10, Constraint Evaluation Matrix)
  - docs/runtime/runtime-api.md (Restoration, arg validation, Fire pipeline)
---

## Question

Does Precept's canonical specification and philosophy state — clearly, consistently, and in an identifiable place — the division of labor between **compile time** and **runtime**? Specifically, for a declared constraint (a rule, a modifier bound such as `min`/`max`, an `ensure`) and for the operations that could violate it: what is guaranteed/required at compile time, and what is enforced/checked at runtime? The question is open; this is a clarity assessment, not a thesis to confirm.

---

## What the canon says — compile time

**Principle 7 (Compile-time-first static checking).** `precept-language-spec.md:103`:
> "The compiler proves what it can, rejects what it can prove invalid, and does not guess. Compile-time structural checking catches unreachable states, type mismatches, constraint contradictions, division by zero, overflow, empty collection access, and more — before runtime, before any entity instance exists. **A precept that compiles without diagnostics has no unproven evaluation faults.**"

**Principle 10 (Totality).** `precept-language-spec.md:109`:
> "For any expression that *could* fault at runtime — division by zero, overflow, empty collection access — the compiler must either prove safety or emit a diagnostic requiring the author to supply constraints that make safety provable. A precept that compiles without diagnostics has no unproven arithmetic or access faults. **Runtime fault traps exist only as defensive redundancy for paths the compiler has already proven unreachable.**"

**Principle 11 (Static completeness).** `precept-language-spec.md:111`:
> "If a precept compiles without diagnostics, it does not fault at runtime. The compiler catches all type errors, proves all arithmetic safety obligations, and verifies all access preconditions at compile time. Every fault class that the evaluator can produce — type mismatch, division by zero, overflow, empty collection access, **constraint range impossibility** — is linked to a compiler diagnostic that prevents it. Runtime fault checks exist only as defensive redundancy, never as the primary enforcement mechanism. This is the bridge between the compiler and the evaluator: the compiler's job is to make every evaluator error path unreachable."

**Proof-engine responsibilities** (compile-time). `precept-language-spec.md:202–207` lists numeric interval reasoning, relational reasoning, divisor safety ("proven-zero divisors are hard errors; divisors with no compile-time nonzero proof are obligation diagnostics"), non-negative obligations, unit-aware interval reasoning, and **assignment range impossibility** ("An assignment expression provably outside the target field's constraint range is a compile-time error", :207).

**Constraint modifiers participate in compile-time proof.** `precept-language-spec.md:1109,1112`:
> "A constraint modifier — `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, ... — desugars to the equivalent `rule` with a generated rationale."
> "A constraint modifier **participates in compile-time proof identically to the equivalent rule** ... `min 5` and `rule X >= 5` are interchangeable to the proof engine. Proof participation is a function of a constraint's **decidability**, not its syntactic form."

**Compile-time checking against defaults** (not against arbitrary runtime data). `precept-language-spec.md:212`:
> "Rules and initial-state ensures are checked against default field values at compile time. A definition where default values violate a declared rule is rejected before any instance exists."

**Proof-engine boundary statement.** `proof-engine.md:92`:
> "If an operation is proven safe, no runtime check is needed. If proof fails, the compiler emits a diagnostic and the author must fix the source before an executable model is produced."

`proof-engine.md:135` (Does NOT own): "Runtime constraint checking is the evaluator's responsibility. The proof engine verifies satisfiability statically."

---

## What the canon says — runtime

**The evaluator enforces constraints dynamically, post-mutation, collect-all.** `philosophy.md:19` (Fire): "evaluates all applicable constraints against the resulting configuration, and commits only if every constraint holds. If any fails, the transition is rejected — the invalid configuration never exists." Spec §3A.4 (`:1937`):
> "All mutations execute on a working copy. Constraints are evaluated against the working copy after all mutations complete. If every constraint passes, the working copy is promoted ... If any constraint fails, the working copy is discarded ... An invalid configuration never exists, even transiently."

`evaluator.md:2129–2148` (Constraint Evaluation Matrix) enumerates which constraint buckets each operation evaluates at runtime (`always`, `from <current>`, `on <event>`, `to <target>`, `in <current>`). `proof-engine.md:2585`: "**Evaluator:** 'Is this constraint satisfied right now given these values?'"

**Runtime faults are defense-in-depth only.** `fault-system.md:280`:
> "When the compiler emits errors, `Precept.From(Compilation)` cannot produce a `Precept` — the evaluator never runs. `Fault` is therefore a defense-in-depth type: it classifies failures that *should never occur* but must be handled if they do (e.g. data loaded from external sources bypassing the compile-time check, or a proof engine gap)."

`fault-system.md:257`:
> "The chain means `FaultCode.DivisionByZero` is defense-in-depth: it classifies a failure that should never occur in a correctly compiled precept, but must be handled if **data arrives from external sources that bypassed compile-time checking.**"

**Runtime structural arg validation gate (type / presence only).** `evaluator.md:1589`: "Arg validation failed (**wrong type, missing required**)". `evaluator.md:1670`: "Arg types must match `ArgDescriptor.Type` | Validated; `InvalidArgs` if mismatch". `runtime-api.md:170`: "`InvalidArgs` | Argument validation failed before row matching. | Wrong type, unknown key, or missing required argument." This gate runs *before* the evaluator/opcode loop (`evaluator.md:762–769`, `runtime-api.md:513`).

**Runtime value/qualifier validation at the fire/update boundary** (business-domain types). `business-domain-types.md:1819` (D14 tier 3):
> "**Runtime boundary validation:** Event args and `precept_fire`/`precept_update` inputs are validated at the API boundary before entering the engine. This is input validation, not mid-evaluation exception — consistent with the temporal proposal's `TryValidateEventArguments` pattern."

`business-domain-types.md:1583` (maxplaces tier 2): "**Event-arg input (runtime boundary):** `precept_fire`/`precept_update` validate event arg values at the input boundary. `{ "Amount": "1.999 USD" }` for a `maxplaces 2` field is rejected before the engine runs."

`business-domain-types.md:465` (`of` tiers): "Runtime event-arg input | **Fire/update boundary** — dimension is validated against the declared category before the engine runs | `DimensionCategoryMismatch`".

`business-domain-types.md:324`: "Currency codes are validated at compile time for literals and **at the fire/update boundary for runtime values.**"

**Arithmetic-result constraint checks at `set` time (runtime).** `business-domain-types.md:1584` (maxplaces tier 3): "**Arithmetic result assignment (runtime):** ... If `Cost` is declared as `money in 'USD' maxplaces 2`, this is a constraint violation at `set` time."

**Restore enforces constraints, bypasses access modes.** `spec §3A.2 (:1916)`: "Restoring an entity in an invalid state is not allowed — the governance guarantee applies from the moment an entity is loaded, not just when it is mutated." `runtime-api.md:266`: "[Restore] recomputes computed fields, and **evaluates constraints** against the restored state." `evaluator.md:2143,2146`: "Restore bypasses access-mode checks but enforces constraint checks."

---

## Where the contract lives

The compile-time/runtime contract is **not stated in a single identifiable section.** It is assembled from at least seven locations, each owning a slice:

| Slice of the contract | Location(s) | Section |
|---|---|---|
| The headline guarantee ("no diagnostics ⟹ no runtime faults"; runtime traps are defensive redundancy) | `precept-language-spec.md:103,109,111` | §0.1 Principles 7, 10, 11 |
| What the proof engine establishes at compile time | `precept-language-spec.md:202–214`; `proof-engine.md:92,122–136` | §0.6; proof-engine Overview / Boundaries |
| Modifier bounds (`min`/`max`) are rules; proved at compile time | `precept-language-spec.md:1109–1112` | §2.4 |
| Constraints are enforced post-mutation on a working copy at runtime | `precept-language-spec.md:1937–1939`; `philosophy.md:19`; `evaluator.md:2129–2148` | §3A.4; Constraint Evaluation Matrix |
| Runtime *structural* arg gate (type/presence) | `evaluator.md:1589,1662–1671`; `runtime-api.md:170` | §9–§10; runtime-api Outcomes |
| Runtime *value/qualifier* validation for externally-sourced values (the fire/update boundary) | `business-domain-types.md:324,465,1583,1819` | `in`/`of` enforcement tiers, maxplaces scope, D14 |
| The compiler→runtime bridge object (defense-in-depth backstops; what crosses the boundary) | `proof-engine.md:107,2378–2395,2572–2585`; `fault-system.md:51,219–257,280` | proof-engine Decision 2 / "No Runtime Obligation Checking"; fault-system StaticallyPreventable chain |

**Finding for Q3:** The *core* claim (compile-time prevention; runtime faults are defense-in-depth) is stated clearly and repeatedly in §0.1 Principles 7/10/11 and reinforced by the fault-system `[StaticallyPreventable]` chain. But the *full division of labor* — and in particular the existence and scope of genuine runtime *value* validation for externally-sourced inputs — is **present-but-scattered**: it lives in the business-domain-types enforcement-tier tables and the evaluator's arg-gate description, not in any §0 principle. A reader of philosophy.md + §0.1 alone would not learn that any runtime value-checking exists at all.

---

## Clear / Murky / Contradictory

| Facet | Classification | Evidence |
|---|---|---|
| Declared constraints (rules, ensures, modifier bounds) are enforced **at runtime, post-mutation, collect-all, atomically** | **Clearly stated** | §3A.4 (`:1937`); §3A.1 collect-all (`:1873`); Constraint Evaluation Matrix (`evaluator.md:2135`) |
| Modifier bounds (`min`/`max`) are rule shorthand and proved at compile time identically to rules | **Clearly stated** | §2.4 (`:1109,1112`) |
| Fault prevention (div-by-zero, overflow, empty access, range impossibility): compile-time proof obligation; runtime trap is defense-in-depth | **Clearly stated** (and cross-enforced by the `[StaticallyPreventable]` chain) | Principles 10/11 (`:109,111`); `fault-system.md:219–257` |
| Compile-time enforcement is **against defaults / statically-known literals**, not arbitrary runtime data | **Present-but-scattered** | §0.6 item 11 (`:212`); business-domain tier-1 rows (`:1817,1582`). Stated where the proof engine is discussed, never abstracted into the headline contract |
| External-input value validation happens at the **fire/update boundary at runtime** | **Present-but-scattered** | Only in `business-domain-types.md` (`:324,465,1583,1819`) and the evaluator arg-gate. Absent from §0.1, §0.3, §3A. The principles' "no runtime faults" framing does not mention it |
| What `ValidateArgs` (the runtime arg gate) actually checks — **type/presence only**, vs. **value/qualifier** | **Murky / possibly contradictory** (see Tensions) | `evaluator.md:1589,1670` (type/presence) vs. `business-domain-types.md:1583,1819` (value/qualifier "before the engine runs") |
| Proof-unresolved → runtime bridge | **Present-but-scattered, and partial** (see Tensions, Q6) | `proof-engine.md:2378–2395,2572–2585`; `fault-system.md:280` |
| Restore: does it evaluate constraints? | **Contradictory between two runtime docs** (see Tensions) | `runtime-api.md:266,887` (evaluates) vs. `evaluator.md:746` (FromJson: "No constraint validation occurs") |

---

## Tensions and silences

### Q4 — Externally-sourced values: where the canon is split

The philosophy and §0.1 frame the entire guarantee as *structural prevention proved at compile time*, with runtime checks as "defensive redundancy" (`:109,111`). But for values that arrive from outside the definition — event args, construction inputs, open/unqualified quantity or currency, restored data — the canon describes **genuine, non-redundant runtime validation** that the compile-time proofs cannot have covered (because the values are not statically known):

- `business-domain-types.md:1819`: "Event args and `precept_fire`/`precept_update` inputs are validated at the API boundary before entering the engine."
- `business-domain-types.md:1583`: a `maxplaces`-violating arg "is rejected before the engine runs."
- `business-domain-types.md:465`: open/event-arg dimension "is validated against the declared category before the engine runs."
- `fault-system.md:280`: faults handle "data loaded from external sources bypassing the compile-time check."

This is **runtime checking that is not merely defensive redundancy** — it is the primary (and only) enforcement point for externally-sourced qualifier/precision violations, since the compiler never saw those values. The canon never reconciles this with the §0.1 principle that "runtime fault checks exist only as defensive redundancy, never as the primary enforcement mechanism" (`:111`). **The two framings are stated in different docs and never set side-by-side.**

A second silence within Q4: the runtime arg gate and the business-domain "fire/update boundary validation" are described in different vocabularies and **may or may not be the same mechanism**. `evaluator.md:1589/1670` says the arg gate checks *type and presence* ("wrong type, missing required"). `business-domain-types.md:1583/1819` says the *same boundary* rejects *value-level* violations (`maxplaces`, qualifier mismatch). Either (a) `ValidateArgs` does more than the evaluator doc lists, or (b) these are two distinct checks and the relationship is unstated. The canon does not say which. (Note also that `on Event ensure` arg constraints — §3A.1 `:1857` — are evaluated *post-mutation* in the `on <event>` constraint bucket per `evaluator.md:2135`, i.e. a *third* place where arg values are checked, distinct from the pre-engine boundary gate.) **Whether qualifier/precision arg validation is the pre-engine gate, a post-mutation ensure, or both, is not stated in one place.**

### Q5 — Principle 1 and Principle 11 vs. runtime validation of external inputs

**Principle 1** (`precept-language-spec.md:91`):
> "Invalid entity configurations ... cannot exist. They are structurally prevented before any change is committed, not caught after the fact. ... A compiler that validates on request is doing detection; a compiler that makes invalid configurations structurally impossible is doing prevention."

**Principle 11** (`precept-language-spec.md:111`):
> "If a precept compiles without diagnostics, it does not fault at runtime. ... Runtime fault checks exist only as defensive redundancy, never as the primary enforcement mechanism."

**The runtime-enforcement text that bears on them** (`business-domain-types.md:1819`, `:1583`, `:465`; `fault-system.md:280`; `evaluator.md:1589`): externally-sourced values are validated *at runtime, at the API boundary, and rejected there* — this is the engine *detecting* an invalid input value at the moment it is presented and refusing it. Read literally:

- Principle 1's prevention guarantee is about **configurations** (state + field values) never persisting — and the working-copy/atomicity model (§3A.4) does deliver that: a rejected arg or failed constraint means no invalid configuration is committed. Under this reading there is no contradiction — prevention is about what can *persist*, and runtime rejection of bad input is the mechanism, not a violation.
- But Principle 11's stronger, more literal claim — "if a precept compiles without diagnostics, it does not fault at runtime" and "runtime checks exist only as defensive redundancy" — sits **in tension** with the fact that externally-sourced value validation is non-redundant primary enforcement. A `maxplaces`-violating or wrong-currency event arg is rejected at runtime by a check that the compiler's "no diagnostics" state did *not* render unreachable, because the offending value was never statically visible. The arg-gate `InvalidArgs` / boundary-rejection path is reachable on a cleanly-compiled precept.

The canon does not resolve which reading of "runtime checks are only defensive redundancy" is intended:
- Is input validation (`InvalidArgs`, qualifier/precision boundary rejection) *outside* the scope of "fault checks" in Principle 11 (which enumerates type mismatch, div-by-zero, overflow, empty access, range impossibility — fault *classes*, via the `FaultCode` set in `fault-system.md:111–151`)? The `FaultCode` registry does include `QualifierMismatch`, `OutOfRange`, and `NumericOverflow` (`fault-system.md:143,146,149`), each `[StaticallyPreventable]` — which suggests these are meant to be compile-time-prevented, not runtime-validated. Yet business-domain-types routes the *same* qualifier/precision violations to a runtime boundary check for external values.

**This is the central unstated assumption:** the canon never states explicitly whether "structural prevention / no runtime faults" (Principles 1, 11) is scoped to *definition-internal* values (defaults, literals, computed results — which the compiler fully sees) and *excludes* externally-sourced inputs (which can only be validated at runtime). The business-domain enforcement-tier tables behave as if that scoping exists; §0.1 does not state it. A reader cannot determine from the canon whether boundary rejection of a bad external value is "the system working as a runtime validator" or "a defensive backstop that should never fire." These point in opposite directions and both are quoted above.

### Q6 — Proof "unresolved" → what the runtime does

§0.6 (`:228`) classifies proof outcomes as *proved dangerous*, *proved safe*, and *unresolved*. The canon states clearly what the **compiler** does with "unresolved": it rejects.

`precept-language-spec.md:2117`: "An unprovable obligation is not treated as 'probably fine.' It remains unresolved, surfaces a compile-time diagnostic, and requires the author to add the missing qualifier, modifier, default, guard, or other proof-bearing fact." `proof-engine.md:92`: "If proof fails, the compiler emits a diagnostic and the author must fix the source before an executable model is produced."

So in the normal path, "unresolved" never reaches the runtime — the `Precept` object is not produced. **The bridge from "compiler could not prove" to "what the evaluator does about it" is therefore stated only for the *off-normal* path** (force-build, catalog evolution, compiler bug, external data bypass):

`proof-engine.md:2380–2388` (Decision 2): only `FaultSiteAnnotation` records cross the compile-runtime boundary; the `ProofLedger` does not. `evaluator.md:960` (CC#6):
> "The first line of defense is the compile gate ... preventing the `Precept` object from being built unless the author fixes them. The second line is the runtime backstop — `FaultSiteAnnotation` on opcodes compiled from unresolved sites (force-builds, catalog evolution, or compiler bugs). For a cleanly-compiled precept, every `opcode.FaultSite` is `null` ..."

**What is clear:** for a cleanly compiled precept there are no unresolved sites, so the bridge is "no bridge needed." For an unresolved site that nonetheless reaches runtime (only via force-build / bug / external bypass), the evaluator hits a `FaultSiteAnnotation` and produces a `Fault` (`EventOutcome.Faulted`, `evaluator.md:1591`).

**What is silent / partial:** The canon documents the bridge **only for the proof-engine's arithmetic/access obligations** (the `FaultSiteLink → FaultCode` family). It does **not** state an analogous runtime story for **`maxplaces` tier-3 arithmetic-result violations** (`business-domain-types.md:1584` calls this "a constraint violation at `set` time" — a *constraint* path, not a `FaultSite` backstop path), nor does it reconcile whether a runtime qualifier/range violation on external input surfaces as `InvalidArgs` (arg gate), `ConstraintsFailed` (post-mutation ensure bucket), or `Faulted` (`QualifierMismatch`/`OutOfRange` FaultCode). All three outcome types exist for what could be the same class of violation, and the canon does not map which external-input failure lands in which.

### Restore: a direct doc-to-doc contradiction

`runtime-api.md:266`: "[Restore] recomputes computed fields, and **evaluates constraints** against the restored state." `runtime-api.md:887`: "Restore evaluates constraints against the restored state — schema drift is detected via `RestoreConstraintsFailed`, not silently accepted." Spec §3A.2 (`:1916`) agrees: "Restoring an entity in an invalid state is not allowed."

But `evaluator.md:746` (FromJson): "It parses the persistence envelope, validates the `$precept` name, resolves the `$state`, and populates the slot array ... **No constraint validation occurs. Access modes are bypassed. The evaluator is not invoked.**"

These may be two different entry points (`Restore` the public API vs. `FromJson` a lower-level hydration path), and `evaluator.md:746` explicitly titles its section "FromJson (Hydration — Not an Evaluator Operation)." But the canon does not reconcile them: the public-API and spec story is "restore enforces constraints"; the evaluator doc's hydration story is "no constraint validation occurs." A reader trying to answer "are externally-restored values constraint-checked at runtime?" gets opposite answers from two runtime docs. **Flagged as a contradiction to verify against code; not resolved here.**

---

*End of assessment. No recommendations offered — this documents clarity, not design direction.*
