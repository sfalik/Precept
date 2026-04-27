# Combined Design Final

**Status:** Final synthesized design  
**Authors synthesized:** Frank + George  
**Amended by:** George  
**Audience:** Shane, runtime/compiler/tooling/MCP implementers  
**Scope:** Final architectural contract for metadata-everywhere, typed pipeline boundaries, action shapes, constraint execution, proof/fault lowering, and language-server/runtime consumption

---

## Purpose

This document amends `docs\working\combined-design-draft.md` into the final synthesized design.

The following decisions are already settled and are preserved without reopening them:

1. **Kinds appear as soon as knowable.**
2. **The lowering boundary is governed by `docs\compiler-and-runtime-design.md`.**
3. **Metadata-everywhere applies across all surfaces.**
4. **The action model is locked to three typed semantic shapes.**
5. **Semantic naming beats syntax-shaped naming.**

If this document and `docs\compiler-and-runtime-design.md` ever disagree about where analysis ends and runtime lowering begins, `docs\compiler-and-runtime-design.md` wins.

---

## 1. Final synthesis in one page

Precept has **two architectural surfaces** produced from one source text:

- **`CompilationResult`** — the full immutable analysis snapshot for diagnostics, authoring tools, graph/proof reasoning, and compile-style MCP output.
- **`Precept`** — the lowered executable model for `Create`, `Restore`, `Fire`, `Update`, and their inspection counterparts.

The compiler pipeline remains exactly five unconditional analysis stages:

`Lexer -> Parser -> TypeChecker -> GraphAnalyzer -> ProofEngine`

Those stages stop at analysis artifacts. They do **not** construct the runtime artifact. Runtime construction begins only at:

`Precept.From(CompilationResult)`

That lowering step reads the complete successful compilation snapshot and projects only runtime-relevant residue into a descriptor-backed executable model:

- lowered descriptor tables
- lowered expression plans
- lowered action plans
- lowered constraint plans and activation indexes
- lowered fault-site plans
- slot layout and runtime indexes

The split is intentional and hard:

- **Language intelligence** consumes `CompilationResult`.
- **Execution and execution preview** consume `Precept`.
- **No consumer is allowed to maintain a parallel semantic model when the metadata already exists.**

That is the final synthesis boundary.

---

## 2. Governing architectural rules

### 2.1 Metadata-everywhere means every surface

Metadata-everywhere is not limited to catalogs or runtime internals. It governs:

- parse-tree node identity
- typed-model semantic identity
- graph/proof reasoning
- lowering
- runtime descriptors and dispatch
- language-server features
- MCP DTOs
- tests and drift checks

If a consumer needs language knowledge and that knowledge already exists in metadata, the consumer must read the metadata rather than rebuild its own list, switch, or string table.

### 2.2 Earliest-knowable identity rule

The rule is precise:

> **A kind is assigned at the earliest stage that can know it and is then carried forward rather than re-derived.**

That yields this required split:

- **Parser-assigned kinds**
  - `ConstructKind`
  - `ActionKind`
  - `OperatorKind`
  - `TypeKind` on `TypeRef`
  - `ModifierKind`
- **Type-checker-assigned kinds**
  - `OperationKind`
  - `FunctionKind`
  - resolved `TypeAccessor`
  - resolved result `TypeKind` on typed expressions

Any downstream consumer that re-derives one of these identities from C# type structure, token text, or ad hoc string matching is architectural drift.

### 2.3 No Roslyn-style parallel models

The synthesized design explicitly rejects three failure modes:

1. **Analysis/runtime collapse** — `Precept` is not allowed to expose the `TypedModel` as its executable contract.
2. **Consumer-local copies of language knowledge** — LS, MCP, evaluator, and tests must not maintain independent hardcoded vocabularies when catalogs or typed metadata already cover them.
3. **Syntax-shaped object explosion** — the model does not create one permanent semantic shape per surface spelling when the real semantic family is smaller.

---

## 3. Typed pipeline stage boundaries

### 3.1 Stage contract table

| Stage | Output artifact | Identity/kinds that must exist here | Consumes | Must not do |
|---|---|---|---|---|
| Lexer | `TokenStream` | `TokenKind` on every token | source text + token catalog metadata | No parsing, no semantic resolution |
| Parser | `SyntaxTree` | `ConstructKind`, `ActionKind`, `OperatorKind`, `TypeKind` on `TypeRef`, `ModifierKind` | `TokenStream` + catalog lookup indexes | No operation/function resolution, no graph/proof reasoning, no lowering |
| TypeChecker | `TypedModel` | `ConstructKind` on typed declarations; resolved `OperationKind`, `FunctionKind`, `TypeAccessor`, result `TypeKind`, resolved descriptor references | `SyntaxTree` + catalog metadata | No reachability/proof conclusions, no runtime plan construction |
| GraphAnalyzer | `GraphResult` | Graph facts derived from typed declarations and modifier metadata | `TypedModel` | No proof emission, no lowering |
| ProofEngine | `ProofModel` | proof obligations, dispositions, evidence, diagnostic/fault linkage | `TypedModel` + `GraphResult` + catalog metadata | No executable-model construction |
| Lowering (`Precept.From`) | `Precept` executable model | descriptor-backed executable identities, lowered plans, runtime indexes | full successful `CompilationResult` | No re-parsing, no re-typing, no re-proving |
| Evaluator | runtime outcomes and inspections | runtime descriptors + lowered executable plans | `Precept` + `Version` + call inputs | No direct dependency on `SyntaxTree`, `TypedModel`, or full `ProofModel` |

### 3.2 Required parse-tree shape

The parse tree must carry the kinds that are parser-knowable:

- declarations retain `ConstructKind`
- action statements retain `ActionKind`
- modifiers retain `ModifierKind`
- `TypeRef` retains resolved `TypeKind`
- expressions retain lexical operator identity via `OperatorKind`

This is what “as soon as knowable” means on the parse side.

### 3.3 Required typed-model shape

The typed model is a **parallel semantic tree**, not annotated syntax in place. It preserves authoring-oriented structure while making downstream metadata consumption direct.

Minimum requirements:

- `TypedDeclaration` retains **`ConstructKind`**
- typed expressions retain resolved semantic identity (`OperationKind`, `FunctionKind`, `TypeAccessor`, result `TypeKind`)
- field/state/event/arg references are resolved into descriptor-aware or slot-aware semantic references
- event-argument references exist as first-class typed expressions
- rules and ensures remain explicit typed declarations, not anonymous booleans hidden inside field declarations
- modifier metadata remains explicit; graph/proof consumers do not translate raw tokens back into modifier identity

The typed model is the final **authoring-semantic** artifact. It is not the runtime representation.

---

## 4. Action-shape model and naming

### 4.1 Locked typed action family

The final synthesis locks the action family to **three semantic shapes** across both typed and lowered representations:

1. **`TypedAction` / `ExecutableAction`**  
   Base shape for target-only actions.
2. **`TypedInputAction` / `ExecutableInputAction`**  
   Adds an **`InputExpression`** / lowered input plan for actions that consume an authored input.
3. **`TypedBindingAction` / `ExecutableBindingAction`**  
   Adds a **`Binding`** / lowered binding plan for actions whose semantics may capture the removed value.

### 4.2 Mapping table

| Surface action | Typed shape | Lowered shape |
|---|---|---|
| `clear` | `TypedAction` | `ExecutableAction` |
| `set`, `add`, `remove`, `enqueue`, `push` | `TypedInputAction` | `ExecutableInputAction` |
| `dequeue`, `pop` | `TypedBindingAction` | `ExecutableBindingAction` |

If `pop` or `dequeue` omits capture syntax, the action still uses the binding shape; the `Binding` payload is simply absent. The semantic family is still “remove and optionally capture,” not “generic no-input action.”

### 4.3 Naming rule

This design uses **semantic names**, not syntax-leaking names:

- `InputExpression`, not `Value`
- `Binding`, not `IntoTarget`
- `ConstraintActivation`, not `EnsureBucketType`
- `FaultSite`, not `RuntimeCheckLocation`

The contract names what the model **means**, not what the surface happened to spell.

### 4.4 Why three shapes and not one or eight

- **Not eight verb-specific semantic classes** — that duplicates `ActionKind` with type hierarchy noise.
- **Not one nullable bag** — that hides real shape distinctions and forces null-forgiving clutter into consumers.
- **Three shapes are enough** — base, input-bearing, binding.

Verb identity comes from `ActionKind`. Shape identity comes from the action family.

---

## 5. Constraint-evaluation contract

Constraint evaluation is a first-class runtime contract. It is not a proof-engine side effect, and it is not allowed to remain an underspecified helper call.

### 5.1 Compile-time identity

Author-declared constraints remain explicit through analysis:

- `rule`
- `in ... ensure`
- `from ... ensure`
- `to ... ensure`
- `on ... ensure`

The typed model preserves them as typed constraint declarations carrying at least:

- their authored kind/scope
- semantic references to the states/events they target
- the typed condition expression
- optional guard expression
- provenance needed to create runtime/public descriptors

They are not flattened into anonymous boolean expressions or hidden as field metadata.

### 5.2 Lowered runtime contract

Lowering produces two distinct runtime artifacts per authored constraint:

- **`ConstraintDescriptor`** — stable runtime/public identity for the authored constraint
- **`ExecutableConstraintPlan`** — lowered executable condition plus activation metadata for evaluator use

The descriptor is what public APIs, inspection results, and violations reference. The plan is what the evaluator runs.

### 5.3 Constraint activation indexes

Lowering precomputes four activation structures:

1. **Always index**  
   Global `rule` declarations.
2. **State activation index** keyed by `(StateDescriptor, ConstraintActivation)`  
   For `ConstraintActivation.InState`, `ConstraintActivation.FromState`, and `ConstraintActivation.ToState`.
3. **Event activation index** keyed by `EventDescriptor`  
   For `on Event ensure` declarations.
4. **Event availability index** keyed by `(StateDescriptor?, EventDescriptor)`  
   Derived from graph/runtime dispatch tables so event ensures only participate where the event is structurally available. `null` state covers stateless precepts.

The evaluator is never allowed to rediscover these scope rules from authored syntax at runtime.

### 5.4 Evaluation matrix

| Operation | Constraint plans evaluated |
|---|---|
| `Fire` | Always + `from` old state + event bucket + `to` target state + `in` target state |
| `Update` | Always + `in` current state |
| `InspectFire` | Same plans as `Fire`, non-committing |
| `InspectUpdate` | Same plans as `Update`, non-committing |
| `Create` with initial event | Same plan set as `Fire` |
| `Create` without initial event | Always + `in` initial state when stateful; Always only when stateless |
| `Restore` | Always + `in` restored state |

Additional rules:

- `Restore` bypasses row matching and access-mode checks per the runtime API, but it does **not** bypass constraints.
- `to` ensures are transitional only. They are evaluated when a target state is known during event/create execution, not as part of the standing “what must be true here?” state surface.
- inspection and commit run the **same lowered constraint plans**; only disposition differs.

### 5.5 Constraint exposure tiers

The runtime keeps the three-tier exposure model already implied by the runtime API:

1. **Definition tier** — `Precept.Constraints`: every declared constraint descriptor
2. **Applicable tier** — `Version.ApplicableConstraints`: the zero-cost subset active for the current state/context
3. **Evaluated tier** — `ConstraintResult` / `ConstraintViolation`: what was actually checked for a specific operation

All three tiers reference the same `ConstraintDescriptor` identity.

### 5.6 Constraints are not faults

Constraint outcomes are normal domain/runtime outcomes:

- satisfied
- violated
- unresolvable during inspection

Commit-time constraint failures surface as ordinary constraint-failure outcomes. They do **not** enter the proof/fault runtime contract. A violated rule or ensure is authored business truth, not an engine breach.

---

## 6. Proof/fault runtime contract

### 6.1 Compile-time proof contract

`CompilationResult.Proof` owns the full analysis-rich proof artifact. It contains at least:

- **`ProofObligation`** — the catalog-declared requirement plus semantic subject and origin
- **`ProofDisposition`** — `Proven`, `Unproven`, or `Contradicted`
- **`ProofEvidence`** — the approved discharge strategy and supporting semantic facts
- **site identity** — action, operation, function overload, accessor, or other semantic owner
- **diagnostic link** — which `DiagnosticCode` is emitted if the obligation is not discharged
- **fault link** — which `FaultCode` represents the impossible runtime breach

This is the complete static proof contract. It remains in `CompilationResult`.

### 6.2 Proof strategies in scope

The initial proof engine is intentionally bounded. The synthesized design assumes explicit, inspectable strategies:

- literal proof
- modifier proof
- guard-in-path proof
- straightforward flow narrowing already represented in the typed/graph model

Anything outside that bounded set remains unproven and produces diagnostics. Precept is prevention-first, not speculative theorem proving.

### 6.3 Lowered runtime projection

Lowering does **not** carry the full proof model into execution. It projects only runtime-relevant residue:

- **`ExecutableFaultPlan`**
- **`FaultSiteDescriptor`** per runtime-reachable statically preventable hazard
- linked **`FaultCode`**
- linked **`DiagnosticCode`**
- linked semantic owner metadata (`ActionKind`, `OperationKind`, `FunctionKind`, accessor identity, relevant descriptors)

The runtime gets the fault bridge, not the proof tree.

### 6.4 Proof/fault chain

The chain is explicit and machine-readable:

`catalog metadata -> ProofRequirement -> ProofObligation -> DiagnosticCode -> FaultCode -> FaultSiteDescriptor`

The `FaultCode -> DiagnosticCode` prevention chain required by the diagnostic/fault design remains intact. No hidden lookup tables are allowed in arbitrary consumers.

### 6.5 What counts as a runtime fault

Runtime faults are **defense-in-depth for impossible-by-design hazards**, such as:

- division or modulo by zero that proof should have blocked
- `pop` / `dequeue` on an empty collection that proof should have blocked
- accessor/presence hazards that proof should have blocked
- impossible overload or accessor execution that typing/proof should have made unreachable

These are not business outcomes. They indicate a breached compiler/runtime contract.

### 6.6 What does not count as a runtime fault

These remain ordinary runtime/domain outcomes:

- `Rejected`
- `Unmatched`
- `InvalidArgs` / `InvalidInput`
- `AccessDenied`
- constraint failures

Faults must not become a second validation system.

---

## 7. Language-server and MCP consumption boundary

### 7.1 `CompilationResult` is the authoring boundary

The language server consumes the analysis snapshot **after the five analysis stages complete and before lowering begins**.

Normal authoring features read `CompilationResult` directly:

- diagnostics
- semantic tokens
- completions
- hover
- go-to-definition
- proof explanations / proof-aware authoring UX
- MCP `precept_compile`

Those consumers read analysis artifacts such as:

- `Tokens`
- `SyntaxTree` for spans/shape when needed
- `TypedModel`
- `GraphResult`
- `ProofModel`
- merged diagnostics

They do **not** require `Precept` and must not recreate parallel semantic information when the compilation snapshot already contains it.

### 7.2 `Precept` is the execution boundary

Only execution-oriented consumers cross lowering:

- runtime API consumers
- language-server preview/inspection that executes hypothetical behavior
- MCP `precept_inspect`
- MCP `precept_fire`
- MCP `precept_update`

The rule is hard:

- if `HasErrors`, stay entirely on `CompilationResult`
- if `!HasErrors`, construct `Precept` via `Precept.From(compilation)` and execute against the lowered model

### 7.3 Consequences for LS and MCP

Metadata-everywhere applies to tooling too:

- LS hover, completions, and semantic tokens derive from catalog metadata and typed semantic identity
- compile-style MCP DTOs expose kind-rich typed/proof/constraint structure from `CompilationResult`
- runtime/inspection DTOs expose descriptor-backed identities from `Precept`
- neither LS nor MCP maintains a shadow vocabulary or shadow semantic contract

This is how the design avoids tool/runtime drift.

---

## 8. Compile-time-only artifacts vs lowered runtime artifacts

| Artifact | Compile-time only | Lowered runtime artifact | Notes |
|---|---|---|---|
| `TokenStream` | Yes | No | Authoring, lex diagnostics, token-driven tooling |
| `SyntaxTree` | Yes | No | Parse artifact only |
| `TypedModel` | Yes | No | Final authoring-semantic tree |
| `GraphResult` | Yes | No | Structural analysis snapshot |
| full `ProofModel` | Yes | No | Diagnostics/tooling only |
| proof evidence and reasoning traces | Yes | No | Never required for execution |
| source spans and author-facing attribution detail | Yes | No | Lowering may keep coarse provenance only through descriptors |
| descriptor tables | No | Yes | Runtime/public identity surface |
| lowered expression plans | No | Yes | Slot-aware runtime form |
| lowered action plans (three shapes) | No | Yes | Runtime mirror of the locked action family |
| `ExecutableConstraintPlan` | No | Yes | Scope-indexed runtime evaluation |
| `ConstraintDescriptor` | No | Yes | Public/runtime identity for authored constraints |
| `ExecutableFaultPlan` | No | Yes | Runtime backstop for statically preventable hazards |
| `FaultSiteDescriptor` | No | Yes | Runtime identity for impossible-path breaches |

The governing rule is simple:

> **Analysis artifacts do not execute directly, and execution artifacts are lowered projections rather than reused analysis nodes.**

---

## 9. Final synthesized decisions

1. **The five compiler stages end at `ProofModel`.** Lowering starts only in `Precept.From(CompilationResult)`.
2. **Kinds appear at the earliest knowable stage and are then carried forward.**
3. **The typed model is a parallel semantic tree and retains `ConstructKind` on typed declarations.**
4. **The action family is locked to three semantic shapes:** `TypedAction`, `TypedInputAction`, `TypedBindingAction` and their lowered counterparts.
5. **Semantic naming is mandatory** for model contracts and runtime plans.
6. **Constraint evaluation is a sibling runtime contract beside proof/fault, not part of the proof system.**
7. **Proof stays analysis-rich and compile-time facing; lowering projects only runtime-relevant fault residue.**
8. **Runtime faults are defense-in-depth for impossible paths only.**
9. **Language intelligence consumes `CompilationResult`; execution preview/runtime consumes `Precept`.**
10. **Metadata-everywhere applies across compiler, runtime, LS, MCP, diagnostics, proof, faults, and tests.**
11. **No surface may keep a Roslyn-style parallel semantic model when the catalog/typed metadata already provides the answer.**

---

## 10. Implementation guidance implied by this synthesis

The architectural work remaining after this document is refinement, not boundary-setting. The main follow-through items are:

- define the concrete descriptor types required by the runtime API contract
- define the exact lowered executable-model internal shapes (`ExecutableExpression`, `ExecutableConstraintPlan`, `ExecutableFaultPlan`)
- thread runtime fault context through evaluator dispatch without reintroducing hidden parallel models
- update LS/MCP DTOs so compile-style and runtime-style outputs reflect the richer metadata cleanly
- add drift tests that prove all major consumers are reading metadata rather than maintaining independent lists

Those are implementation tasks inside the boundaries above. The boundaries themselves are now final.
