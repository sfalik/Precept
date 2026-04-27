# Proposal: Proof/Fault Runtime Contract for the Synthesized Design

**Author:** Frank  
**Audience:** Shane, George, runtime/tooling/MCP implementers  
**Status:** Proposed synthesis  
**Scope:** Boundary between `CompilationResult.Proof`, `Precept.From(compilation)` lowering, runtime faults, and consumer surfaces

---

## Non-negotiables carried forward

1. **Metadata appears as early as knowable.** Parse-time identities (`ConstructKind`, `ActionKind`, `OperatorKind`, `TypeKind`) appear in syntax/typed nodes as soon as the stage can know them. Type-resolved identities (`OperationKind`, `FunctionKind`, overload choice) appear in the typed model, not later.
2. **Metadata everywhere means all surfaces.** Compiler, proof, lowering, runtime, language server, MCP, and tests all read the same declared metadata.
3. **Semantic naming wins.** The contract names semantic roles (`Binding`, `FaultSite`, `ConstraintBucket`, `ProofObligation`) rather than syntax-shaped placeholders.
4. **Three typed action shapes stay locked.** The typed/lowered action family is: `TypedAction` base, `TypedOperandAction`, and `TypedBindingAction` for pop/dequeue result capture.
5. **Lowering boundary stays where the docs already put it.** `CompilationResult` remains the full analysis snapshot. `Precept.From(compilation)` lowers that snapshot into the runtime-optimized executable model. The proof engine does **not** become the executable-model constructor.

---

## Proposal in one sentence

`CompilationResult` keeps the full proof story for diagnostics and tooling; `Precept.From(compilation)` lowers only the **runtime-relevant fault contract** and the **scope-indexed constraint plan** into the executable model; runtime faults are defense-in-depth signals for impossible paths, not a second validation system.

---

## 1. What proof artifacts exist after compilation and after lowering

### After compilation (`CompilationResult`)

`CompilationResult.Proof` should be the **analysis-rich proof artifact**:

- **Obligations** — every proof requirement collected from metadata-bearing operations, accessors, and actions
- **Disposition** per obligation — `Proven` / `Unproven`
- **Evidence** for proven obligations — literal proof, modifier proof, guard/path proof, etc.
- **Diagnostic link** — the `DiagnosticCode` emitted when an obligation is unproven
- **Fault link** — the `FaultCode` that the obligation statically prevents
- **Site identity** — the semantic execution site that introduced the obligation

That makes the proof artifact explicit enough for diagnostics, future tooling, and MCP export without forcing the runtime to carry the whole analysis tree.

### After lowering (`Precept.From(compilation)`)

Lowering keeps **only runtime-relevant proof residue**:

- **`ExecutableConstraintPlan`** — precomputed rule/ensure buckets indexed by scope
- **`ExecutableFaultPlan`** — one lowered `FaultSiteDescriptor` per runtime-reachable preventable-fault site
- **Lowered expressions/actions** — slot-indexed, descriptor-backed, runtime-optimized forms

The executable model should **not** embed the full `ProofModel`, certificates, or proof search traces. Those belong to analysis, not execution.

---

## 2. What runtime consumes directly

The runtime consumes the lowered executable model, not the typed model directly.

That executable model should include:

- **Descriptors** for fields, events, states, args, and constraints
- **Lowered action plan** using the locked three action shapes:
  - `ExecutableAction`
  - `ExecutableOperandAction`
  - `ExecutableBindingAction`
- **Lowered expression plan** with already-resolved `OperationKind`, `FunctionKind`, overload identity, slot indices, and accessor metadata
- **`ExecutableConstraintPlan`**
  - always-on rule bucket
  - state-ensure buckets keyed by `(state, anchor)`
  - event-ensure buckets keyed by event/state context
- **`ExecutableFaultPlan`**
  - `FaultSiteDescriptor`
  - linked `FaultCode`
  - linked `DiagnosticCode`
  - linked semantic owner (`ActionKind`, `OperationKind`, `FunctionKind`, accessor, field/event/state descriptor)

The runtime reads that data directly during inspect/fire/update. It does **not** consult proof certificates or rerun proof.

---

## 3. What remains compile-time diagnostic only

These stay on the compilation side only:

- full obligation inventory for authoring/debugging
- proof certificates and evidence records
- abstract fact state and guard-derived reasoning
- unproven-obligation diagnostics
- source-oriented attribution details used by LS/MCP (`Span`, proof explanations, author-facing messages)

If an obligation is unproven, the result is a **compile-time diagnostic**, not a deferred runtime warning. Precept's philosophy is prevention, not late advisory behavior.

---

## 4. What becomes runtime fault behavior

Only **impossible-by-design evaluator breaches** become runtime faults.

That includes paths like:

- division/modulo by zero that should have been blocked by proof
- empty collection access/mutation that should have been guarded
- invalid member access or impossible overload execution that should have been rejected during typing/proof

It does **not** include normal domain outcomes:

- `Rejected`
- `Unmatched`
- `InvalidArgs`
- access denial
- ordinary constraint violations from `rule` / `ensure`

Those are first-class runtime/business outcomes, not faults.

So the contract is:

1. **Proof requirement declared in catalog metadata**
2. **Proof obligation collected in `ProofModel`**
3. **Unproven obligation emits `DiagnosticCode`**
4. **Same semantic site lowers to `FaultSiteDescriptor` with linked `FaultCode` + `DiagnosticCode`**
5. **If runtime still reaches that path, evaluator emits a fault as defense-in-depth**

This preserves the `FaultCode -> DiagnosticCode` chain already required by the diagnostic design without making faults part of ordinary author experience.

---

## 5. Constraint evaluation relationship

The synthesized design must treat constraint evaluation as a sibling contract beside proof/fault, because that is what inspect/fire/update actually execute.

Lowering should build:

- **Global rule bucket** — always evaluated after fire/update and during inspection
- **State ensure buckets**
  - `in`
  - `from`
  - `to`
- **Event ensure buckets** keyed by event identity and applicable state context

`ConstraintDescriptor` remains the public identity. Runtime evaluation produces `ConstraintResult` / `ConstraintViolation` by referencing those descriptors.

Proof/fault metadata does **not** replace constraint metadata. They serve different jobs:

- **Constraint plan:** what must be evaluated for domain correctness
- **Fault plan:** what should never be reachable if the compiler/runtime contract is sound

---

## 6. How inspect / fire / update relate to the contract

All three operations run against the same lowered executable model.

### Inspect

- Uses the same lowered action, expression, constraint, and fault site descriptors
- Returns constraint results and row prospects
- Discards the working copy instead of committing
- Does **not** surface proof certificates by default
- If an impossible evaluator path is still reached, it reports the same runtime fault contract as commit mode

### Fire / Update

- Use the same execution path as inspection
- Commit only after the lowered constraint plan passes
- Surface business/runtime outcomes normally
- Surface faults only on invariant breach / defense-in-depth paths

This keeps the runtime API's existing invariant intact: **inspection and commit share the same evaluation path**.

---

## 7. How language-server-facing consumers relate to the contract

The language server should stay on the **analysis side** unless it explicitly needs runtime preview.

### LS consumers that read `CompilationResult`

- diagnostics
- semantic tokens
- completions
- hover
- go-to-definition
- proof-stage diagnostics/future proof explanations

These consumers read `Tokens`, `Model`, `Graph`, `Proof`, and merged `Diagnostics`. They do **not** need the lowered executable model.

### LS consumer that may read `Precept`

- preview / inspector-style evaluation

That consumer only constructs `Precept` when `!HasErrors`, then uses the lowered executable model for inspect/create/fire-style preview.

So the boundary is clean:

- **Language intelligence** reads `CompilationResult`
- **Execution preview** reads `Precept`

That is exactly the split already described in `docs/compiler-and-runtime-design.md` and `docs/runtime/runtime-api.md`.

---

## 8. Synthesis decisions

1. **Proof stays an analysis artifact.** It is not the executable model.
2. **Lowering produces a runtime fault plan, not a runtime proof engine.**
3. **Runtime consumes lowered descriptors, buckets, and slot-indexed plans only.**
4. **Unproven obligations remain compile-time diagnostics only.**
5. **Runtime faults are defense-in-depth for statically preventable breaches, not ordinary business outcomes.**
6. **Constraint evaluation is explicitly precomputed at lowering via scope-indexed buckets.**
7. **LS structural features consume `CompilationResult`; preview consumes `Precept`.**

This is the honest synthesized contract. Anything looser collapses the analysis/runtime boundary; anything stricter hides the proof/fault bridge the design still owes Shane.
