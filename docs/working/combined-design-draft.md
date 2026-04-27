# Combined Design Draft

**Status:** Synthesis baseline for George to amend  
**Authors synthesized:** Frank + George  
**Audience:** Shane, runtime/compiler/tooling implementers  
**Scope:** Unified architectural draft for metadata-everywhere, typed pipeline boundaries, constraint execution, proof/fault, lowering, and language-server consumption

---

## Purpose

This document is the combined design. It is not a note dump and it is not a fresh brainstorm. It is the baseline synthesis after the proof/fault contract work, with the fixed inputs treated as settled:

1. **Kinds appear as soon as knowable.**
2. **The lowering boundary is governed by `docs\compiler-and-runtime-design.md`.**
3. **Metadata-everywhere applies across all surfaces.**
4. **The action model is locked to three typed semantic shapes.**
5. **Semantic naming beats syntax-shaped naming.**

The result is a two-surface architecture:

- **`CompilationResult`** is the full analysis snapshot for authoring, diagnostics, proof, and tooling.
- **`Precept`** is the lowered executable model for runtime and preview execution.

That split is final for this synthesis.

---

## 1. Synthesis in one page

Precept remains a metadata-driven system end to end. The catalogs declare language knowledge; the pipeline, runtime, language server, MCP tools, and tests consume that metadata rather than maintaining parallel logic.

The compiler still runs five unconditional analysis stages:

`Lexer -> Parser -> TypeChecker -> GraphAnalyzer -> ProofEngine`

Those stages stop at **analysis artifacts**. They do **not** produce the runtime artifact. The runtime artifact is produced only at the existing lowering boundary:

`Precept.From(CompilationResult)`

That lowering step reads the complete successful compilation snapshot and projects only execution-relevant residue into a descriptor-backed executable model:

- executable expressions
- executable actions
- executable constraint buckets
- executable fault sites
- descriptor tables
- slot layout / runtime indices

The runtime never consumes the typed model directly. The language server normally never consumes the lowered runtime directly. That is the contract line.

---

## 2. Typed pipeline stage boundaries

## 2.1 Stage contract table

| Stage | Output artifact | Kinds/metadata that must exist here | What this stage must not do |
|---|---|---|---|
| Lexer | `TokenStream` | `TokenKind` on every token | No structural parsing, no semantic resolution |
| Parser | `SyntaxTree` | Parser-knowable kinds: `ConstructKind`, `ActionKind`, `OperatorKind`, `TypeKind` on `TypeRef`, `ModifierKind` | No operation/function resolution, no proof reasoning |
| TypeChecker | `TypedModel` | Typed-semantic kinds: `OperationKind`, `FunctionKind`, resolved `TypeAccessor`, resolved result `TypeKind`, slot-aware refs, typed declarations retaining `ConstructKind` | No graph reachability, no proof disposition, no lowering |
| GraphAnalyzer | `GraphResult` | Structural graph facts derived from typed declarations + modifier metadata | No runtime plan construction, no proof emission |
| ProofEngine | `ProofModel` | Proof obligations, proof evidence/disposition, diagnostic/fault linkage | No executable model construction |
| Lowering (`Precept.From`) | `Precept` executable model | Descriptor-backed runtime plans projected from `CompilationResult` | No new language analysis; no re-typing; no re-proving |
| Evaluator | outcomes/inspections | Runtime descriptors, executable expressions/actions, constraint plan, fault plan | No direct dependence on `SyntaxTree`/`TypedModel`/proof certificates |

## 2.2 Earliest-knowable kind assignment

The slogan is not "parse time for everything." That would be sloppy. The real rule is:

> **A kind appears at the earliest stage that can know it, then is carried forward rather than re-derived.**

That means:

- **Parser-assigned**
  - `ConstructKind`
  - `ActionKind`
  - `OperatorKind`
  - `TypeKind` on `TypeRef`
  - `ModifierKind`
- **Type-checker-assigned**
  - `OperationKind`
  - `FunctionKind`
  - resolved `TypeAccessor`
  - resolved result `TypeKind` on typed expressions

Any design that pushes a knowable kind later is architectural drift. Any design that re-derives an already-known kind in a downstream consumer is an outrage.

## 2.3 Required typed-model shape

The typed model is a **parallel tree**, not in-place annotation of syntax nodes. Its job is to preserve authoring-oriented semantic structure while making kind- and metadata-based consumption direct.

Minimum synthesis requirements:

- `TypedDeclaration` retains **`ConstructKind`**
- typed expressions retain resolved semantic identity (`OperationKind`, `FunctionKind`, `TypeAccessor`, result `TypeKind`)
- field/event/state/arg references are resolved and slot-aware
- event-argument references exist as first-class typed expressions
- typed constraints are explicit declarations, not buried ad hoc in field records

The typed model is the last authoring-oriented semantic artifact. It is not the runtime executable representation.

---

## 3. Action-shape model and naming

## 3.1 Locked action family

The synthesis locks the action family to **three semantic shapes** across both typed and lowered models:

1. **`TypedAction` / `ExecutableAction`**  
   Base shape for target-only actions.
2. **`TypedOperandAction` / `ExecutableOperandAction`**  
   Adds an **`OperandExpression`** / lowered operand plan for actions that consume an authored input expression.
3. **`TypedBindingAction` / `ExecutableBindingAction`**  
   Adds **`Binding`** metadata for actions whose semantics may capture the removed value (`pop`, `dequeue`).

## 3.2 Naming rule

The model uses **semantic names**, not syntax-leaking names.

- Say **`OperandExpression`**, not `Value`
- Say **`Binding`**, not `IntoTarget`
- Say **`FaultSite`**, not `RuntimeCheckLocation`
- Say **`ConstraintActivation`**, not `EnsureBucketType`

The design describes what the model means, not how the surface happened to spell it.

## 3.3 Why three shapes instead of one or eight

- **Not eight verb-specific classes:** that is Roslyn bias, not Precept architecture.
- **Not one nullable bag:** that collapses meaningful shape differences and pushes null-forgiving noise into consumers.
- **Three shapes are enough:** base, operand-bearing, binding.

Verb identity comes from **`ActionKind`**. Shape identity comes from the typed/lowered action family. Consumers dispatch on kind and consume shape without syntax-shaped duplication.

## 3.4 Action mapping

| Surface action | Typed/lowered shape |
|---|---|
| `clear` | `TypedAction` / `ExecutableAction` |
| `set`, `add`, `remove`, `enqueue`, `push` | `TypedOperandAction` / `ExecutableOperandAction` |
| `dequeue`, `pop` | `TypedBindingAction` / `ExecutableBindingAction` |

If `pop` or `dequeue` omits a capture, the action is still the binding shape; the `Binding` payload is simply absent. The semantic family remains correct because the action's meaning is still "remove and optionally capture result," not "generic no-value action."

---

## 4. Constraint-evaluation contract

Constraint evaluation is a first-class runtime contract, not a paragraph stub and not a proof-engine side effect. This is the core of Precept's prevention guarantee.

## 4.1 Compile-time identity

Author-declared constraints stay explicit through analysis:

- `rule`
- `in ... ensure`
- `from ... ensure`
- `to ... ensure`
- `on ... ensure`

The typed model preserves them as typed declarations carrying `ConstructKind` plus resolved semantic references. They are not flattened into anonymous boolean expressions.

## 4.2 Lowered runtime contract

Lowering produces:

- **`ConstraintDescriptor`** — stable runtime/public identity for the authored constraint
- **`ExecutableConstraintPlan`** — lowered executable condition + activation metadata + referenced descriptor set

The descriptor is what public APIs and inspection surfaces talk about. The plan is what the evaluator runs.

## 4.3 Constraint activation buckets

Lowering precomputes four activation buckets:

1. **Always bucket**  
   Global `rule` declarations
2. **State bucket by `(StateDescriptor, EnsureAnchor)`**  
   `EnsureAnchor.In`, `EnsureAnchor.From`, `EnsureAnchor.To`
3. **Event bucket by `EventDescriptor`**  
   Event ensures
4. **Availability index**  
   Structural state/event applicability derived from graph/runtime tables so event ensures only activate where the event is actually available

This keeps scope evaluation deterministic without duplicating language rules inside the evaluator.

## 4.4 Evaluation matrix

| Operation | Constraint buckets evaluated |
|---|---|
| `Fire` | Always + `from` old state + event bucket + `to` target state + `in` target state |
| `Update` | Always + `in` current state |
| `InspectFire` | Same as `Fire`, but non-committing |
| `InspectUpdate` | Same as `Update`, but non-committing |
| `Create` / `InspectCreate` | Same event-based matrix when an initial event exists; otherwise the stateless/default construction path evaluates the applicable non-event buckets |
| `Restore` | Always + `in` restored state, with restore-specific pipeline bypass rules already defined in the runtime API |

Inspection and commit share the same plans. Only disposition differs.

## 4.5 Constraints are not faults

Constraint outcomes stay ordinary domain/runtime outcomes:

- satisfied
- violated
- unresolvable during inspection

Commit-time violations surface as `ConstraintViolation`-style outcomes. They do **not** route through the fault system. A constraint failure is an authored business truth, not an engine breach.

---

## 5. Proof/fault runtime contract

## 5.1 Compile-time proof contract

`CompilationResult.Proof` is the **analysis-rich proof artifact**. It keeps the full authoring/tooling story:

- **`ProofObligation`** — the catalog-declared requirement plus semantic subject and origin
- **`ProofDisposition`** — `Proven`, `Unproven`, or `Contradicted`
- **`ProofEvidence`** — literal, modifier, guard-in-path, or other approved proof strategy
- **proof origin/site identity** — operation, function overload, accessor, or action site
- **diagnostic link** — which `DiagnosticCode` is emitted if the obligation is not discharged
- **fault link** — which `FaultCode` would represent the impossible runtime breach

That is the complete static contract. It stays in `CompilationResult`.

## 5.2 Lowered runtime projection

Lowering does **not** carry the full proof model into execution. It projects only runtime-relevant proof residue:

- **`ExecutableFaultPlan`**
- **`FaultSiteDescriptor`** per runtime-reachable preventable-fault site
- linked **`FaultCode`**
- linked **`DiagnosticCode`**
- linked semantic owner metadata (`ActionKind`, `OperationKind`, `FunctionKind`, accessor identity, relevant descriptors)

The runtime gets the fault bridge, not the proof tree.

## 5.3 Proof/fault chain

The chain is:

`catalog metadata -> ProofRequirement -> ProofObligation -> DiagnosticCode -> FaultCode -> FaultSiteDescriptor`

That chain must be explicit and machine-readable. No hidden lookup tables in random consumers.

## 5.4 What counts as a runtime fault

Runtime faults are **defense-in-depth for impossible-by-design engine hazards**, such as:

- division or modulo by zero that proof should have blocked
- `pop` / `dequeue` on an empty collection that proof should have blocked
- presence/accessor hazards that proof should have blocked
- impossible overload/accessor execution that should have been rejected earlier

These are not normal domain outcomes. They indicate the compiler/runtime contract has been breached.

## 5.5 What does not count as a runtime fault

These remain normal outcomes:

- `Rejected`
- `Unmatched`
- `InvalidArgs` / `InvalidInput`
- `AccessDenied`
- `ConstraintViolation`

Those are part of ordinary domain execution. Faults are not.

## 5.6 Proof strategies in scope

The synthesis baseline assumes the initial proof engine discharges obligations through a bounded set of explicit strategies:

- literal proof
- modifier proof
- guard-in-path proof
- straightforward flow narrowing already represented in the typed/graph model

Anything outside that bounded set remains unproven and produces diagnostics. Precept is prevention-first, not speculative theorem proving.

---

## 6. Language-server and MCP consumption boundary

## 6.1 `CompilationResult` consumers

The language server is a **`CompilationResult` consumer first**.

These surfaces read the analysis snapshot directly:

- diagnostics
- semantic tokens
- completions
- hover
- go-to-definition
- proof explanations / future proof-aware authoring UX
- MCP `precept_compile`

They read:

- `Tokens`
- `SyntaxTree` when needed for spans/shape
- `TypedModel`
- `GraphResult`
- `ProofModel`
- merged diagnostics

They do **not** require lowering for ordinary authoring.

## 6.2 `Precept` consumers

Only execution-oriented surfaces cross the lowering boundary:

- LS preview / inspector experience
- MCP `precept_inspect`
- MCP `precept_fire`
- MCP `precept_update`
- host application runtime

The rule is:

- if `HasErrors`, stay on `CompilationResult`
- if `!HasErrors`, construct `Precept` and run preview/runtime against the lowered model

That boundary is already established in `docs\compiler-and-runtime-design.md` and remains intact here.

## 6.3 Metadata everywhere means tooling too

Metadata-everywhere does not stop at runtime internals. It applies across all surfaces:

- LS hover/completions/tokens derive from catalog + typed metadata
- MCP compile-style DTOs expose kind-rich structure and proof/constraint metadata
- runtime inspection surfaces expose descriptor-backed identities, not raw strings long-term
- tests derive coverage obligations from catalogs and executable metadata inventories

If a surface needs knowledge and that knowledge exists in metadata, the surface reads the metadata. It does not invent a parallel copy.

---

## 7. Compile-time only vs lowered runtime artifacts

| Artifact | Compile-time only | Lowered runtime artifact | Notes |
|---|---|---|---|
| `TokenStream` | Yes | No | Authoring/diagnostic surface only |
| `SyntaxTree` | Yes | No | Parser artifact; not a runtime dependency |
| `TypedModel` | Yes | No | Analysis-oriented semantic tree |
| `GraphResult` | Yes | No | Structural analysis snapshot |
| Full `ProofModel` | Yes | No | Kept for diagnostics/tooling only |
| Detailed proof evidence/certificates | Yes | No | Never required for execution |
| Source spans / author-facing attribution detail | Yes | No | Lowering may retain coarse descriptor provenance only |
| Descriptor tables | No | Yes | Runtime/public identity surface |
| Executable expressions | No | Yes | Lowered, slot-aware, runtime-oriented |
| Executable actions (three shapes) | No | Yes | Mirrors typed action family semantically |
| `ExecutableConstraintPlan` | No | Yes | Scope-indexed runtime evaluation |
| `ConstraintDescriptor` | No | Yes | Public/runtime identity for authored constraints |
| `ExecutableFaultPlan` | No | Yes | Runtime backstop for preventable hazards |
| `FaultSiteDescriptor` | No | Yes | Links runtime impossible-path breach to semantic origin |

The important rule is simple: **analysis artifacts do not leak directly into execution; execution artifacts are lowered projections, not reused analysis nodes.**

---

## 8. Integrated synthesis decisions

1. **The five compiler stages end at `ProofModel`.** Lowering happens only in `Precept.From(CompilationResult)`.
2. **Kinds appear at the earliest knowable stage and are then carried forward.**
3. **The typed model is a parallel semantic tree and retains `ConstructKind` on typed declarations.**
4. **The action family is locked to three semantic shapes:** base, operand-bearing, binding.
5. **Semantic naming is mandatory** for model contracts and runtime plans.
6. **Constraint evaluation is a sibling runtime contract beside proof/fault, not part of the proof system.**
7. **Proof stays analysis-rich and compile-time facing; lowering projects only runtime-relevant fault residue.**
8. **Runtime faults are defense-in-depth for impossible paths only.**
9. **Language intelligence consumes `CompilationResult`; execution preview/runtime consumes `Precept`.**
10. **Metadata-everywhere applies across compiler, runtime, LS, MCP, diagnostics, proof, faults, and tests.**

---

## 9. Baseline for amendment

George should amend **this** baseline, not reopen the settled boundaries above.

The remaining useful amendment work is implementation depth:

- concrete descriptor types
- executable-plan internal shapes
- fault-context threading mechanics inside the evaluator
- proof-evidence DTO detail
- MCP/LS DTO consequences of the richer metadata

Those are refinement tasks. The architectural split and the contracts above are the synthesis baseline.
