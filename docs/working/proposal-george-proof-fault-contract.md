# Proposal: Proof/Fault Runtime Contract for the Synthesized Design

**Author:** George  
**Date:** 2026-04-26  
**Status:** Working synthesis proposal for Frank + George review

## Hard constraints this proposal keeps

| # | Constraint | Source |
|---|---|---|
| 1 | Metadata appears at the earliest stage where it is knowable, then is carried forward rather than re-derived. | Shane directive; `docs\working\review-george-on-frank.md` §2.1 |
| 2 | Metadata-everywhere applies across compiler, runtime, LS, MCP, diagnostics, proof, faults, and tests. | Shane directive; `docs\language\catalog-system.md` § Architectural Identity |
| 3 | Semantic model naming wins over syntax-shaped naming. | `.squad\decisions\inbox\copilot-directive-20260426-185930.md` |
| 4 | Typed actions use three semantic shapes: base, input-bearing, and binding. | `.squad\decisions\inbox\copilot-directive-20260426-185330.md` |
| 5 | Lowering does **not** happen in the proof engine. The five compiler stages stop at `ProofModel`; `Precept.From(compilation)` is the lowering boundary. | `docs\compiler-and-runtime-design.md` § Compiler Pipeline, § Precept; `docs\runtime\runtime-api.md` § Construction Path |

## Proposal in one sentence

Compilation owns the **proof contract**; lowering owns the **runtime contract**; runtime faults are only the fail-fast mirror of statically preventable proof obligations, while author-declared constraints remain normal business outcomes.

## 1. What proof artifacts exist after compilation / lowering

### After compilation (`CompilationResult`)

`CompilationResult.Proof` should be the full analysis artifact. It should contain one entry per statically preventable hazard:

- **`ProofObligation`** — the catalog-declared requirement (`ProofRequirement`) plus its semantic subject and origin
- **`ProofOrigin`** — where the obligation came from: operation, function overload, accessor, action, or lowered constraint expression
- **`ProofDisposition`** — `Proven`, `Unproven`, or `Contradicted`
- **`ProofEvidence`** — why it was proven (literal, modifier, guard-in-path, flow narrowing, etc.)
- **`PreventableFaultLink`** — the metadata bridge to the failure surfaces:
  - `DiagnosticCode` to emit when the obligation is not proven
  - `FaultCode` that would fire if execution still reaches the impossible path

This makes the proof/fault contract closed and inspectable in one place: **requirement → evidence/absence → diagnostic → corresponding fault**.

### After lowering (`Precept.From(compilation)`)

Lowering should **not** carry the whole `ProofModel` into execution. It should project only what runtime needs:

- lowered executable expressions/actions
- precomputed `ConstraintPlan` tables
- descriptor tables (field/event/state/arg)
- **`FaultSite` metadata** attached to executable steps that can only fail if the proof contract was violated

`ProofCertificate`-style detail stays compile-time/tooling-facing. Runtime keeps only the lightweight projection needed to report an impossible-path failure accurately.

## 2. What runtime consumes directly

The runtime should consume an executable model lowered from the successful `CompilationResult`, not the analysis tree directly.

### Required runtime inputs

1. **Descriptors**  
   Field, state, event, and arg descriptors remain the runtime identity surface.

2. **Executable action shapes**  
   Keep the three typed semantic shapes:
   - `TypedAction` — base shape, used directly for zero-input actions such as `clear`
   - `TypedInputAction` — carries `InputExpression`
   - `TypedBindingAction` — carries `Binding` for `pop` / `dequeue`

3. **Executable expressions**  
   Lowered, slot-aware expressions keyed by `OperationKind`, `FunctionKind`, `TypeAccessor`, etc.

4. **Constraint plans**  
   Runtime should evaluate lowered `ConstraintPlan` objects, not re-walk declarations.

5. **Fault sites**  
   Each statically preventable hazard that survives lowering becomes a `FaultSite` with:
   - `FaultCode`
   - linked descriptor/origin metadata
   - optional source span / declaration identity for reporting

## 3. Constraint evaluation contract

Constraint evaluation is runtime behavior, but its identity is compile-time metadata.

### Compile-time identity

Each authored rule / ensure lowers from a typed declaration into a stable `ConstraintDescriptor` plus a lowered `ConstraintPlan`.

`ConstraintPlan` should use semantic, not syntax-shaped, fields:

- `Descriptor`
- `Activation`
- `Condition`
- `ReferencedDescriptors`
- `Scope`

### Runtime indexing

Lowering should precompute three buckets:

1. **Always constraints** — global rules
2. **State-anchored constraints** — keyed by `(StateDescriptor, EnsureAnchor)` for `in` / `to` / `from`
3. **Event-anchored constraints** — keyed by `EventDescriptor`

That gives the evaluator a deterministic lookup story:

- `Fire` evaluates: always + `from` old state + event + `to` new state + `in` new state
- `Update` evaluates: always + `in` current state
- `InspectFire` / `InspectUpdate` evaluate the **same plans** in preview mode and return statuses instead of committing

This keeps constraint identity shared across compile/runtime, while runtime behavior stays a normal business path rather than a fault path.

## 4. What remains compile-time diagnostic only

These should stop in `CompilationResult` and never become normal runtime behavior:

- unresolved proof obligations (`Unproven` / `Contradicted`)
- graph and reachability problems
- invalid construct placement / type incompatibility / illegal action-target pairings
- impossible descriptor wiring or missing lowering metadata
- any proof/fault mismatch detectable before lowering

If any of these exist, `CompilationResult.HasErrors` is true and `Precept.From(compilation)` must not produce a runtime artifact.

## 5. What becomes runtime fault behavior

Only **statically preventable engine hazards** become runtime faults.

Examples:

- divide / modulo by zero
- pop / dequeue from an empty collection
- accessor use that required a proven presence or count fact

Those are **not** author-level business violations. They are impossible-path failures. The contract is:

1. catalog metadata declares the proof requirement
2. proof produces a diagnostic if it cannot be discharged
3. successful compilation means the path is considered safe
4. runtime still keeps a `FaultSite` as a fail-fast backstop

If such a fault fires at runtime, treat it as compiler/runtime contract failure, not as a normal rejected operation.

By contrast:

- `reject` stays `Rejected`
- failed rule / ensure evaluation stays `ConstraintViolation` / `ConstraintResult`
- invalid args stay `InvalidArgs` / `InvalidInput`
- access-mode denial stays `AccessDenied`

Those are business/runtime outcomes, not faults.

## 6. How inspect / fire / update relate to the contract

All three runtime surfaces should share the same lowered executable model and the same constraint/fault metadata.

### `Fire`
- executes lowered action/expression plans
- evaluates applicable `ConstraintPlan`s after recomputation
- returns business outcomes on authored invalidity
- only trips `FaultSite` on impossible-path engine failure

### `Update`
- uses the same constraint tables with the update-specific scope set
- follows the same business-outcome vs. fault split

### `InspectFire` / `InspectUpdate`
- run the same plans in non-committing mode
- surface `ConstraintResult` and row/event prospects
- must not invent a separate proof model
- should never present faults as user-level “possible” outcomes; a fault during inspection is an engine failure path

## 7. How language-server-facing consumers relate to the contract

The language server is a **`CompilationResult` consumer first**, a runtime consumer second.

### LS / `precept_compile`

Read directly from:

- `TypedModel` for symbol/type-aware tooling
- `GraphResult` for structural diagnostics
- `ProofModel` for proof diagnostics and proof/fault explanation
- merged `Diagnostics` for publishing

They should not depend on runtime lowering for normal authoring features.

### LS preview / runtime-backed inspection

Only the preview path crosses the lowering boundary:

- if `HasErrors`, stay entirely on `CompilationResult`
- if error-free, create `Precept` via `Precept.From(compilation)` and use runtime inspection

That preserves the boundary already defined in `docs\compiler-and-runtime-design.md`: tooling always gets a compilation snapshot; runtime exists only for sound compilations.

## 8. Synthesis decisions

1. **Proof is analysis-only.** It lives in `CompilationResult`, not in `Precept`.
2. **Lowering owns execution shape.** `Precept.From(compilation)` projects proof/fault metadata into executable `FaultSite`s and `ConstraintPlan`s.
3. **Constraints are not faults.** They remain authored business enforcement outcomes.
4. **Faults are impossible-path backstops.** They are metadata-linked to proof obligations and diagnostics, but are not part of the normal domain outcome model.
5. **Tooling reads proof directly; runtime reads lowered projections.** That is the clean contract split.
