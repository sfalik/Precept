# Precept Compiler and Runtime Design

> **Status:** Approved working architecture
> **Audience:** compiler, runtime, language-server, MCP, and documentation authors

## 1. What Precept promises

Precept is a domain integrity engine for .NET. A single declarative contract governs how a business entity's data evolves under business rules across its lifecycle, making invalid configurations structurally impossible. You declare fields, constraints, lifecycle states, and transitions in one `.precept` file. The runtime compiles that declaration into an immutable engine that enforces every rule on every operation. No invalid combination of lifecycle position and field data can persist.

This is not validation. Validation checks data at a moment in time, when called. Precept declares what the data is allowed to become and enforces that declaration structurally, on every operation, with no code path that bypasses the contract.

The guarantee is **prevention, not detection.** Invalid entity configurations cannot exist — they are structurally prevented before any change is committed. The engine is deterministic: same definition, same data, same outcome. At any point, you can preview every possible action and its outcome without executing anything. Nothing is hidden.

Everything in this document — every pipeline stage, every artifact, every runtime operation — exists to deliver that guarantee.

## 2. Architectural approach

### Catalog-driven design

Precept keeps its language purposely simple. Rather than embedding domain knowledge in pipeline stage implementations (as traditional compilers do), Precept externalizes the entire language definition as structured metadata in ten catalogs. Pipeline stages are generic machinery that reads this metadata.

This inverts the traditional compiler model. In Roslyn, GCC, or TypeScript, domain knowledge is scattered across pipeline stage implementations. Adding a language feature means touching dozens of files. In Precept, adding a language feature means adding an enum member and filling an exhaustive switch — the C# compiler refuses to build if any member lacks metadata, and propagation to every consumer (grammar, completions, hover, MCP, semantic tokens) is automatic.

The ten catalogs fall into two groups:

**Language definition** — what the language IS: `Tokens` (lexical vocabulary), `Types` (type system families), `Functions` (built-in function library), `Operators` (operator symbols with precedence/associativity/arity), `Operations` (typed operator combinations — which `(op, lhs, rhs)` triples are legal), `Modifiers` (declaration-attached modifiers as a discriminated union with five subtypes), `Actions` (state-machine action verbs), `Constructs` (grammar forms and declaration shapes).

**Failure modes** — how it reports problems: `Diagnostics` (compile-time rules), `Faults` (runtime failure modes).

Their union IS the language specification in machine-readable form. No consumer maintains a parallel copy. Every downstream artifact — the TextMate grammar, LS completions, LS hover, MCP vocabulary, semantic tokens, type-checker behavior — derives from catalog metadata.

The architectural principle: **if something is domain knowledge, it is metadata; if it is metadata, it has a declared shape; if shapes vary by kind, the shape is a discriminated union.** Pipeline stages, tooling, and consumers derive from the metadata — they never encode language knowledge in their own logic. See `docs/language/catalog-system.md` for the full catalog system design.

### Purpose-built

Precept's pipeline is not a general-purpose compiler framework. It is designed for Precept's specific shape: a declarative DSL with fields, states, events, transitions, constraints, and guards. Every stage knows what it is building toward — an executable model that structurally prevents invalid configurations. The pipeline does not need to be extensible to other languages. It needs to be correct for this one.

### Unified pipeline

The system has compilation phases (lexing, parsing, type checking, graph analysis, proof) and runtime phases (lowering, evaluation, constraint enforcement, structured outcomes). These are sequential stages of one system. They share the same catalog metadata. They produce artifacts that flow forward. Compilation builds understanding; runtime acts on it.

Two top-level products emerge from this pipeline:

**`CompilationResult`** — the immutable analysis snapshot. Always produced, even from broken input. Authoring surfaces (language server, MCP compile) need the full picture — including syntax errors, unresolved references, and unproven safety obligations — to provide diagnostics, completions, and navigation.

**`Precept`** — the executable runtime model. Produced only from error-free compilations via `Precept.From(compilation)`. This is the sealed model that runtime operations execute against. It carries lowered descriptor tables, execution plans, and constraint indexes — not syntax trees or proof graphs.

The relationship is straightforward: analysis builds `CompilationResult`; lowering transforms it into `Precept`; runtime operations execute against `Precept`. Authoring tools read `CompilationResult`. Execution tools read `Precept`.

## 3. The pipeline

```text
source text + catalogs
        │
        ▼
Lexer ─► Parser ─► TypeChecker ─► GraphAnalyzer ─► ProofEngine
        │            │                │                │
        └────────────┴────────────────┴────────────────┘
                             ▼
                    CompilationResult
                             │
                             ▼   (only when !HasErrors)
                    Precept.From(compilation)
                             │
                ┌────────────┼────────────┬───────────────┐
                ▼            ▼            ▼               ▼
             Create       Restore      Evaluator      Structural queries
                                            │
                           ┌───────────────┼────────────────┐
                           ▼               ▼                ▼
                    Fire / InspectFire  Update / InspectUpdate  Fault backstops
```

Every stage begins from two roots. The `.precept` source text is the author-owned program. The catalogs are the language specification. Catalogs enter as early as they are knowable — later stages carry catalog-stamped identity forward, never recreating it from hardcoded switches.

### Artifact inventory

| Artifact | Owner | Classification |
|---|---|---|
| `TokenStream` | Lexer | compile-time |
| `SyntaxTree` | Parser | compile-time |
| `TypedModel` | TypeChecker | compile-time |
| `GraphResult` | GraphAnalyzer | compile-time |
| `ProofModel` | ProofEngine | compile-time |
| `CompilationResult` | Compiler | compile-time aggregate |
| Descriptor tables, slot layout, dispatch indexes, constraint-plan indexes, fault-site backstops | `Precept.From` (lowering) | runtime |
| `Precept` | `Precept.From` | runtime executable model |
| `Version` | runtime operations | runtime entity snapshot |
| `EventOutcome`, `UpdateOutcome`, `RestoreOutcome` | Evaluator | runtime results |
| `ConstraintResult`, `ConstraintViolation` | Evaluator | runtime results |
| `Fault` | Evaluator | runtime backstop (impossible-path only) |

### What crosses the lowering boundary

`Precept.From()` selectively lowers analysis knowledge — descriptors, constraint metadata, expression text, source references, execution plans — into runtime-native shapes. What does not cross: syntax trees, token streams, proof graphs, parser recovery, and graph topology as artifacts.

The rule is **type dependency direction**: runtime types do not hold references to `CompilationResult`, `TypedModel`, or `SyntaxTree`. But runtime shapes carry analysis-derived knowledge — `ConstraintDescriptor` carries expression text, source lines, scope targets, and guard metadata. That is analysis knowledge in lowered form. Artifacts don't cross; selected knowledge from artifacts crosses in runtime-native shapes.

## 4. Lexer

The lexer converts raw text into classified tokens with exact spans. It has no semantic opinion. The key design choice: `TokenKind` comes directly from catalog metadata (`Tokens.GetMeta`), not from a parallel enum maintained by the lexer. The lexer is a vocabulary consumer, not a vocabulary owner.

**Takes in:** `string source`; catalogs: `Tokens`, `Diagnostics`.
**Produces:** `TokenStream(ImmutableArray<Token> Tokens, ImmutableArray<Diagnostic> Diagnostics)` where `Token` is `Token(TokenKind Kind, string Text, SourceSpan Span)`.
**Catalog entry:** `TokenKind` comes directly from `Tokens.GetMeta(...)` / `Tokens.Keywords`; token categories, TextMate scope, semantic token type, and completion hints remain derivable from `TokenMeta`.
**Consumed by:** Parser, `CompilationResult`, LS lexical tokenization and grammar tooling.

**How it serves the guarantee:** The lexer ensures that every character of source text is accounted for and classified according to catalog-defined vocabulary. No ambiguity in token identity propagates downstream.

**Implementation status:** This is the one materially implemented compiler stage.

## 5. Parser

The parser builds the source-structural model of the authored program. Its key design choice: `SyntaxTree` preserves the author's source structure — including recovery shape for broken programs — without resolving names, types, or overloads. Tooling needs source-faithful structure (folding, outline, recovery context) independently of semantic resolution.

**Takes in:** `TokenStream`; catalogs: `Constructs`, `Tokens`, `Operators`, `Diagnostics`.
**Produces:** `SyntaxTree(PreceptSyntax Root, ImmutableArray<Diagnostic> Diagnostics)` with source-faithful declaration and expression nodes, missing-node representation, and span ownership. (Current shape: diagnostics-only stub.)
**Catalog entry:** The parser stamps syntax-level identities as soon as syntax alone can know them: construct kind, anchor keyword, action keyword, operator token, literal segment form.
**Consumed by:** TypeChecker, LS syntax-facing features (outline, folding, recovery-aware local context).

**How it serves the guarantee:** Structural fidelity means the type checker and downstream stages work from a faithful representation of the author's intent, including malformed programs — authoring tools can diagnose problems precisely because the structure is preserved, not discarded on error.

**Implementation status:** `Parser.Parse` is a stub; the contract is designed but not implemented.

### `SyntaxTree` vs `TypedModel`

These are distinct artifacts with distinct jobs.

`SyntaxTree` owns what the author wrote — source-faithful, recovery-aware, with exact token adjacency and spans on malformed input. Its consumers are parser diagnostics, folding, and source tools.

`TypedModel` owns what the program means — semantic, normalized, with resolved names/types/overloads and operation identity. Its consumers are LS intelligence, graph analysis, proof, and lowering.

Example: for `Approve.Amount <= RequestedAmount`, the syntax tree holds a member-access node over exact tokens and spans. The typed model holds a resolved event-arg symbol, a resolved field symbol, a resolved `OperationKind`, and a result type of `boolean`.

The typed layer must feel like a semantic database, not an AST with annotations.

**Required `TypedModel` inventory:**

- **Declaration symbols** — stable semantic identities for fields, states, events, args, and constraint-bearing declarations, each with declaration-origin handles for diagnostics and navigation.
- **Reference bindings** — every semantic identifier/expression site binds directly to a symbol, overload, accessor, operator, or action identity.
- **Normalized declarations** — rules, `in`/`to`/`from`/`on` ensures, transition rows, access declarations, state hooks, and stateless hooks live in semantic inventories shaped for analysis and lowering, not parser nesting.
- **Typed expressions** — expression nodes carry resolved result type plus resolved operation/function/accessor identity and semantic subjects.
- **Typed actions** — semantic action families resolve to one of three named shapes (see Type Checker below) with catalog-defined operand and binding contracts.
- **Dependency facts** — computed-field dependencies, arg dependencies, referenced-field sets, and semantic edge data required by graph/proof/lowering.
- **Source-origin handles** — semantic sites keep stable links back to authored source spans/lines for diagnostics, hover, and go-to-definition without inheriting token adjacency.

**Anti-mirroring rules:** (1) `TypedModel` must not preserve parser child layout, missing-node shape, or recovery nullability as its primary contract. (2) Hover, go-to-definition, semantic tokens, and semantic completions must be satisfiable from `TypedModel` bindings plus source-origin handles — if they need to walk parser structure, the typed boundary is underspecified. (3) Graph, proof, and lowering consume normalized semantic inventories, not syntax nodes. (4) `SyntaxTree` remains the sole owner of recovery, token grouping, exact authored ordering, and malformed-construct shape.

## 6. Type Checker

The type checker is the first stage that reasons about semantics. Its key design choice: type resolution is a separate pass from parsing — `TypedModel` is a projection of `SyntaxTree`, not an in-place annotation — because tooling and downstream stages need to reason about source structure and semantic meaning independently.

**Takes in:** `SyntaxTree`; catalogs: `Types`, `Functions`, `Operators`, `Operations`, `Modifiers`, `Actions`, `Constructs`, `Diagnostics`.
**Produces:** Semantic symbol tables, binding indexes, normalized declaration inventories, typed expressions/actions, dependency facts, source-origin handles, and diagnostics. (Current shape: diagnostics-only stub.)
**Catalog entry:** This is the first stage that resolves `TypeKind`, `FunctionKind`, `OperatorKind`, `OperationKind`, `ModifierMeta`, `ActionMeta`, `FunctionOverload`, `TypeAccessor`, and attached `ProofRequirement` records into semantic identity.
**Consumed by:** GraphAnalyzer, ProofEngine, LS semantic tooling, MCP compile output, lowering.

**How it serves the guarantee:** The type checker catches semantic defects — type mismatches, illegal operations, invalid modifier combinations, unresolved references — before the program reaches graph analysis or runtime. Every expression and declaration that passes type checking has a resolved, catalog-backed semantic identity. This is where the structural guarantee begins to take shape: if it type-checks, its operations are legal.

**Implementation status:** `TypeChecker.Check` is stubbed; the semantic model contract is ahead of implementation.

### Typed action family — three shapes only

Actions in the typed model resolve to exactly one of three semantic shapes:

- **`TypedAction`** (base) — verbs like `clear`, `reset`. No operand; value ownership is internal.
- **`TypedInputAction`** (operand-bearing) — verbs like `set`, `add`, `remove`, `enqueue`, `push`. Carries `InputExpression: TypedExpression`.
- **`TypedBindingAction`** (binding) — verbs like `dequeue`, `pop`. Carries `Binding: TypedBinding`.

The partition reflects verb-surface ownership. A flat shape with optional fields would require nullable fields on the majority of members.

Field naming discipline:

| Correct | Do not use |
|---|---|
| `InputExpression` | `Value`, `Input` |
| `Binding` | `IntoTarget` |
| `ConstraintActivation` | `EnsureBucketType` |
| `FaultSite` | `RuntimeCheckLocation` |

Lowering produces the matching executable family: `ExecutableAction`, `ExecutableInputAction`, `ExecutableBindingAction`. Same naming discipline.

### Earliest-knowable kind assignment

| Stage | Kinds assigned |
|---|---|
| Parser | `ConstructKind`, `ActionKind`, `OperatorKind`, `TypeKind` on `TypeRef` nodes, `ModifierKind` |
| Type checker | `OperationKind`, `FunctionKind`, resolved `TypeAccessor`, resolved result `TypeKind` on typed expressions |

The parser stamps everything that syntax alone can determine. The type checker stamps everything that requires name, type, or overload resolution. A kind that requires name resolution does not appear in `SyntaxTree`; a kind that syntax alone determines does not wait for the type checker.

## 7. Graph Analyzer

The graph analyzer derives lifecycle structure from semantic declarations. Its key design choice: graph analysis consumes the resolved `TypedModel` — not syntax — because reachability, dominance, and topology require resolved state/event/transition identity, not source-structural nesting.

**Takes in:** `TypedModel`; catalogs: `Modifiers`, `Actions`, `Diagnostics`.
**Produces:** Graph facts keyed by semantic identities plus diagnostics. (Current shape: diagnostics-only stub.)
**Catalog entry:** State semantics (`initial`, `terminal`, `required`, `irreversible`, `success`, `warning`, `error`) come from modifier metadata already resolved by the type checker; the analyzer must not reinterpret raw syntax.
**Consumed by:** ProofEngine, `Precept.From`, LS structural diagnostics, runtime structural precomputation.

**How it serves the guarantee:** The graph analyzer detects lifecycle defects — unreachable states, terminal states with outgoing edges, required-state dominance violations, irreversible back-edges — that would make the state machine unsound. These are structural problems in the contract itself, caught before any instance exists.

**Proposed `GraphResult` facts:**

- **Reachability** — initial state, reachable states, unreachable states.
- **Topology** — edges, predecessors, successors, event coverage per state.
- **Structural validity** — terminal outgoing-edge violations, required-state dominance, irreversible back-edge violations.
- **Runtime indexes** — available events by state, state-scoped routing buckets, target-state facts lowering can reuse.

**Implementation status:** `GraphAnalyzer.Analyze` is stubbed.

## 8. Proof Engine

The proof engine is the last analysis stage before lowering. Its key design choice: proof is bounded — four strategies only, no general SMT solver — and proof stops at analysis. The runtime receives only lowered fault-site residue for defense-in-depth, not the proof graph itself.

**Takes in:** `TypedModel`, `GraphResult`; catalogs: `Operations`, `Functions`, `Types`, `Diagnostics`, `Faults`.
**Produces:** Obligations, evidence, dispositions, preventable-fault links, diagnostics, and semantic site attribution. (Current shape: diagnostics-only stub.)
**Catalog entry:** Proof obligations originate in metadata: `BinaryOperationMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`, `TypeAccessor.ProofRequirements`, and action metadata. `FaultCode` ↔ `DiagnosticCode` linkage remains catalog-owned as a prevention/backstop relationship.
**Consumed by:** `CompilationResult`, LS/MCP proof reporting, lowering of fault residue into runtime backstops.

**How it serves the guarantee:** The proof engine discharges statically preventable runtime hazards. If it can prove an operation is safe at compile time, no runtime check is needed. If it cannot, the compiler emits a diagnostic — the author must fix the source before an executable model is produced. This is the compile-time half of prevention.

### Proof strategy set

The proof engine operates over a bounded, non-extensible strategy set:

- **Literal proof** — the value is a known compile-time literal; outcome is directly knowable.
- **Modifier proof** — the value flows through a catalog-defined modifier chain whose output bounds are statically determined.
- **Guard-in-path proof** — a guard expression in the control flow statically establishes a sufficient range or type constraint.
- **Straightforward flow narrowing** — type narrowing through the control-flow graph alone bounds the value.

Any obligation outside this set is unresolvable by the compiler and emits a `Diagnostic`. New strategies are language changes, not tooling extensions.

### Proof/fault chain

The end-to-end prevention/backstop chain:

```
catalog metadata → ProofRequirement → ProofObligation → DiagnosticCode → FaultCode → FaultSiteDescriptor
```

- **Catalog metadata → `ProofRequirement`** — catalog entries declare what must be provable at each call site.
- **`ProofRequirement` → `ProofObligation`** — the proof engine instantiates the requirement against a specific semantic site.
- **`ProofObligation` → `DiagnosticCode`** — an unresolved obligation becomes an authoring-time diagnostic.
- **`DiagnosticCode` → `FaultCode`** — each diagnostic has a prevention counterpart: the fault that would occur if this site somehow reached runtime.
- **`FaultCode` → `FaultSiteDescriptor`** — if the site survives to runtime (defense-in-depth only), lowering plants a backstop.

`FaultSiteDescriptor` is the runtime face of an impossible path that a correct program never reaches.

**Implementation status:** `ProofEngine.Prove` is stubbed; only the catalog-side proof vocabulary exists today.

## 9. Compilation Snapshot

`CompilationResult` is an aggregation boundary, not a reasoning stage. It captures the complete analysis pipeline as one immutable snapshot so consumers can access any stage's output without re-running the pipeline.

**Takes in:** Raw source + the five pipeline stages.
**Produces:** `CompilationResult(TokenStream Tokens, SyntaxTree SyntaxTree, TypedModel Model, GraphResult Graph, ProofModel Proof, ImmutableArray<Diagnostic> Diagnostics, bool HasErrors)`.
**Consumed by:** LS, MCP `precept_compile`, `Precept.From`.

**Implementation status:** The wiring exists and merges diagnostics correctly, but four of the five stages are still hollow.

## 10. Lowering

Lowering is the transformation from analysis to execution. `Precept.From(CompilationResult)` is the sole owner of this transformation — no other code path builds the runtime model. It selectively transforms analysis knowledge into runtime-native shapes rather than copying or referencing compile-time artifacts.

**Takes in:** Error-free `CompilationResult`; semantic inputs come from `TypedModel`, `GraphResult`, and proof residue from `ProofModel`. Catalogs are not re-read here.
**Produces:** `Precept` as a sealed executable model owning descriptor tables, slot layout, dispatch indexes, lowered execution plans, explicit constraint-plan indexes, inspection metadata, and fault-site backstops.
**Catalog entry:** Catalog metadata reaches runtime only in lowered semantic form: descriptor identity, resolved operation/function/action identity, constraint descriptors, and proof-owned fault-site residue.
**Consumed by:** `Precept.Create`, `Precept.Restore`, `Version` operations, MCP runtime tools, host applications.

**How it serves the guarantee:** Lowering precomputes everything the runtime needs to enforce every constraint on every operation. The evaluator becomes a plan executor — it does not reason about semantics at runtime because lowering has already resolved all semantic questions into executable plans. This is what makes the runtime deterministic and testable in isolation.

### Lowered executable-model contract

| Runtime concern | Lowered structure | Consumed by |
|---|---|---|
| identity | descriptor tables: `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `ConstraintDescriptor` | every runtime API surface |
| storage | slot layout, field-to-slot map, default-value plan, omission metadata | create, restore, fire, update |
| routing | per-state and stateless event-row dispatch indexes, target-state routing metadata | fire and inspect fire |
| execution | lowered expression nodes and action plans keyed to descriptors and resolved semantic identities | evaluator |
| recomputation | dependency graph and evaluation order for computed fields | fire, update, restore, inspect |
| access | per-state field access-mode index and query surface | update and inspect update |
| constraints | explicit executable plan indexes for `always`, `in`, `to`, `from`, and `on` anchors | create, restore, fire, update, inspect |
| inspection | row/source/result-shaping metadata for `EventInspection`, `RowInspection`, `UpdateInspection`, `ConstraintResult`, `FieldSnapshot` | inspection surfaces |
| fault backstops | `FaultSite`/fault-site descriptors linked to `FaultCode` and prevention `DiagnosticCode` | impossible-path defense only |

### Constraint activation indexes

The five constraint-plan families (`always`, `in`, `to`, `from`, `on`) are accessed through four precomputed activation indexes, built once during lowering and keyed to descriptor identity:

- **Always index** (global) — rules and ensures with no state or event anchor; active on every operation.
- **State activation index** (`StateDescriptor`, `ConstraintActivation`) — `InState`, `FromState`, and `ToState` anchors.
- **Event activation index** (`EventDescriptor`) — `on Event ensure` anchors.
- **Event availability index** (`StateDescriptor?`, `EventDescriptor`) — available-event scope; null state key for stateless precepts.

The `ConstraintActivation` discriminant distinguishes whether a constraint binds to the current state, the source state, or the target state of a transition. Callers look up a prebuilt bucket, not compute activation at dispatch time.

### Current surface

The stable runtime contract is descriptor-backed. Current public stubs still expose string placeholders and string-selected entry points. Those strings are provisional implementation placeholders, not the architectural end state.

**Implementation status:** `Precept.From` currently checks `HasErrors` and then throws `NotImplementedException`.

## 11. Runtime surface and operations

Once a valid `Precept` exists, four operations govern entity lifecycle. The evaluator is a shared plan executor — it consumes only lowered artifacts and executes prebuilt plans. Execution semantics are fully determined at lowering time.

### Evaluator

**Takes in:** `Precept`, `Version`, descriptor-keyed arguments or patches, lowered execution plans, explicit constraint-plan indexes, fault-site backstops.
**Produces:** `EventOutcome`, `UpdateOutcome`, `RestoreOutcome`, `EventInspection`, `UpdateInspection`, `RowInspection`; impossible-path breaches classify as `Fault`.

Valid executable models do not produce in-domain runtime errors. Expected runtime behavior is expressed as structured outcomes and inspections. `Fault` is reserved for defense-in-depth classification of impossible-path engine invariant breaches.

**Implementation status:** `Evaluator` exists, but every operation body is a stub. `Fail(FaultCode, ...)` already routes through `Faults.Create(...)`.

### Constraint evaluation matrix

Every operation evaluates constraints through the same lowered plan indexes. Access-mode checks and row dispatch are independent of constraint evaluation.

| Operation | Access-mode checks | Row dispatch | Constraint plans evaluated |
|---|---|---|---|
| `Fire` | no | yes | `always`, `from <current>`, `on <event>`, `to <target>` |
| `InspectFire` | no | yes | same as `Fire` |
| `Update` | yes | no | `always`, `in <current>` |
| `InspectUpdate` | yes | no | same as `Update`, plus event-prospect evaluation over hypothetical state |
| `Create` with initial event | no | yes (initial event) | `always`, plus initial-event fire-path plans |
| `Create` without initial event | no | no | `always`, `in <initial>` |
| `Restore` | no | no | `always`, `in <current>` |

Two rules: (1) `Restore` bypasses access-mode checks and row dispatch but does **not** bypass constraint evaluation. (2) `to` ensures are transitional — they do not participate in `in`-anchor evaluation.

Inspection and commit paths execute the same lowered plans. Disposition alone differs — report vs. enforce.

### Create

Create constructs the first valid `Version`, optionally by atomically firing the declared initial event. Creation with an initial event reuses the full fire-path execution — not a separate code path — so initial-event constraints, actions, and transitions apply identically.

**Takes in:** `Precept`; lowered defaults, `InitialState`, `InitialEvent`, arg descriptors, fire-path runtime plans.
**Produces:** `EventOutcome` (commit) or `EventInspection` (inspect). Success yields `Applied(Version)` or `Transitioned(Version)`.

### Restore

Restore reconstitutes persisted data under the current definition. It validates rather than trusts — it runs constraint evaluation but intentionally bypasses access-mode restrictions, because persisted data represents a prior valid state, not an active field edit.

**Takes in:** `Precept`; caller-supplied persisted state and fields; lowered descriptors, slot validation, recomputation, restore constraint plans.
**Produces:** `RestoreOutcome` — `Restored(Version)`, `RestoreConstraintsFailed(IReadOnlyList<ConstraintViolation>)`, or `RestoreInvalidInput(string Reason)`.

### Fire

Fire is the core state-machine operation. Routing, action execution, transition, recomputation, and constraint evaluation are a single atomic pipeline — not composable steps callers assemble — because partial execution would violate the determinism guarantee.

**Takes in:** `Version`; event descriptors, arg descriptors, row dispatch tables, lowered action plans, recomputation index, anchor-plan indexes, fault sites.
**Produces:** `EventOutcome` — `Transitioned`, `Applied`, `Rejected`, `InvalidArgs`, `EventConstraintsFailed`, `Unmatched`, current provisional `UndefinedEvent`. `EventInspection` / `RowInspection` for inspect.

Constraint identity survives into `ConstraintResult` and `ConstraintViolation` through `ConstraintDescriptor`. Routing uses descriptor-backed row identity.

### Update

Update governs direct field edits under access-mode declarations and constraint evaluation. `InspectUpdate` additionally evaluates the event landscape over the hypothetical post-patch state.

**Takes in:** `Version`; field descriptors, per-state access facts, recomputation dependencies, `always`/`in` constraint plans, event-prospect evaluation.
**Produces:** `UpdateOutcome` — `FieldWriteCommitted`, `UpdateConstraintsFailed`, `AccessDenied`, `InvalidInput`. `UpdateInspection` for inspect.

### Structured outcomes

The structural guarantee means that a valid executable model communicates entirely through structured outcomes. There are three result families, and collapsing them would undermine the guarantee:

**Diagnostics** — produced by the compiler pipeline. Authoring-time findings against source. Error diagnostics block `Precept` construction.

**Runtime outcomes** — produced by runtime operations. Expected success, domain rejection, or boundary-validation results. These are normal, in-domain behavior:
- Business outcomes: `Rejected`, `EventConstraintsFailed`, `UpdateConstraintsFailed`, `RestoreConstraintsFailed`
- Routing/availability: `Unmatched`, current provisional `UndefinedEvent`
- Boundary validation: `InvalidArgs`, `InvalidInput`, `RestoreInvalidInput`, `AccessDenied`

**Faults** — produced only by the evaluator backstop. Impossible-path engine invariant breaches. Every `FaultCode` has a compiler-owned diagnostic counterpart (the prevention rule that should have blocked the site). But many diagnostics have no fault counterpart, and many runtime outcomes are intentionally modeled as normal results, not faults.

| Category | Compile-time surface | Runtime surface |
|---|---|---|
| Authoring defect | `Diagnostic` only | no runtime surface; `Precept` not constructed |
| Unresolved proof obligation | `Diagnostic` only | no runtime surface; `Precept` not constructed |
| Business prohibition or rule failure | may have no compile-time issue | structured domain outcome |
| Routing/availability result | may have no compile-time issue | structured boundary outcome |
| Caller input/data mismatch | descriptor/type contracts exist | structured boundary-validation outcome |
| Impossible-path invariant breach | compiler-owned prevention rule | `Fault` (defense-in-depth; should be unreachable) |

### Commit outcomes by operation

| Operation | Success | Domain outcome | Boundary-validation | Invariant breach |
|---|---|---|---|---|
| `Create` / `Fire` | `Applied`, `Transitioned` | `Rejected`, `EventConstraintsFailed`, `Unmatched` | `InvalidArgs`, provisional `UndefinedEvent` | `Fault` |
| `Update` | `FieldWriteCommitted` | `UpdateConstraintsFailed`, `AccessDenied` | `InvalidInput` | `Fault` |
| `Restore` | `Restored` | `RestoreConstraintsFailed` | `RestoreInvalidInput` | `Fault` |

### Inspection

`EventInspection` provides the reduced event-level landscape. `RowInspection` provides per-row prospect, effect, snapshots, and constraints. `UpdateInspection` provides hypothetical field state plus the resulting event landscape. `ConstraintResult` carries evaluation status referencing `ConstraintDescriptor`. `FieldSnapshot` captures resolved or unresolved field value in hypothetical state.

Inspection shares the same lowered plans as commit. It is not a second evaluator.

### Constraint query contract

Three tiers, additive in specificity:

- **Definition** — `Precept.Constraints`: every declared `ConstraintDescriptor` in the definition. Always available from the lowered model.
- **Applicable** — `Version.ApplicableConstraints`: the zero-cost subset active for the current state and context. Available from any live `Version`.
- **Evaluated** — `ConstraintResult` / `ConstraintViolation`: what was actually checked during a specific operation. Embedded in outcome and inspection results only.

### Operation-facing plan selection

| Operation | Required lowered contract |
|---|---|
| `Create` | default-value plan, initial-state seed, optional initial-event descriptor/arg contract, then shared fire-path execution |
| `Restore` | slot population, descriptor validation, recomputation, `always` + `in <current>` constraint plans; no access checks, no row dispatch |
| `Fire` | row dispatch, action plans, recomputation, `always` + `from <current>` + `on <event>` + `to <target>` constraint plans |
| `Update` | access-mode index, patch validation, recomputation, `always` + `in <current>` constraint plans; inspect additionally runs event-prospect evaluation |

## 12. Language-server integration

The language server consumes pipeline artifacts by responsibility. Each LS feature reads from exactly the artifact that owns the information it needs.

**Lexical classification** (keyword, operator, punctuation, literal, comment) — reads `TokenStream` + `TokenMeta`. Not `SyntaxTree`, not `TypedModel`.

**Syntax-aware features** (outline, folding, recovery) — reads `SyntaxTree`. Not `TypedModel`.

**Diagnostics** — reads merged `CompilationResult.Diagnostics`. Not per-stage polling.

**Semantic tokens for identifiers** — reads `TypedModel` symbol/reference bindings + semantic source-origin spans. Not token categories alone.

**Completions** — reads catalogs for candidate inventory, `SyntaxTree` for local parse context, `TypedModel` for scope/binding/expected type. Not `GraphResult` or `ProofModel`.

**Hover** — reads `TypedModel` semantic site + catalog documentation/signatures. Not raw syntax.

**Go-to-definition** — reads `TypedModel` reference binding + declaration-origin handles. Not syntax-tree guessing.

**Preview/inspect** — reads lowered `Precept` + runtime inspection, only when `!HasErrors`. Not `CompilationResult` after lowering.

**Graph/proof explanation** — reads `GraphResult` and `ProofModel` when explicitly surfacing unreachable-state or proof information. Not for everyday completion/hover/tokenization.

Two hard rules: (1) Do not make semantic LS features consume `SyntaxTree` because the typed layer is underspecified. (2) Do not make preview/runtime LS features consume `CompilationResult` after lowering succeeds.

### Consumer artifact map

| Consumer | Correct artifact |
|---|---|
| LS diagnostics / semantic tokens / completions / hover / definition | `CompilationResult` |
| MCP `precept_language` | catalogs directly |
| MCP `precept_compile` | `CompilationResult` |
| MCP `precept_inspect` | `Precept` + inspection runtime |
| MCP `precept_fire` | `Precept` / `Version.Fire` |
| MCP `precept_update` | `Precept` / `Version.Update` |
| Host application authoring-time validation | `CompilationResult` |
| Host application execution | `Precept` + `Version` |

**Current LS reality:** `tools\Precept.LanguageServer\Program.cs` only boots the server and waits for exit. The matrix above is a contract for later implementation.

---

## Appendix A: Implementation status

*Implementation status changes on a different cadence than architectural decisions. This appendix tracks current reality; the main document tracks the stable contract.*

| Area | Current state | Required state |
|---|---|---|
| Lexer | implemented | keep as lexical truth source |
| Parser | stub | build real `SyntaxTree.Root` and recovery shape |
| Typed model | diagnostics-only stub | semantic model with anti-mirroring contract |
| Graph | diagnostics-only stub | real topology and runtime indexes |
| Proof | diagnostics-only stub | obligations, evidence, and preventable-fault links |
| Lowering | error guard + stub | descriptors, plans, and executable indexes |
| Runtime operations | public shapes, no bodies | full evaluator-driven behavior |
| Language server | bootstrap only | consume tokens/tree/model/runtime by responsibility |
| Runtime API identity | string placeholders | descriptor-based public API |

### Implementation action items

1. **Define concrete descriptor types.** `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, and `ConstraintDescriptor` as first-class sealed types — not string aliases. Every runtime API surface routes through descriptor identity.

2. **Define lowered constraint anchor shapes.** Each anchor family (`always`, `in`, `to`, `from`, `on`) requires a distinct internal lowered shape. A single flat constraint record is prohibited; the evaluation matrix depends on families being separately addressable.

3. **Thread `FaultSiteDescriptor` through evaluator dispatch.** Every backstop site must carry a resolved `FaultSiteDescriptor` linked to its `FaultCode`. Unadorned `Fault` calls with no site context are incomplete.

4. **Update LS and MCP DTOs.** When descriptor types are defined, LS and MCP data-transfer objects must match. Parallel string-keyed shapes are provisional scaffolding.

5. **Add drift tests.** Tests that fail if any consumer maintains a parallel copy of catalog-defined vocabulary. Catalogs are the single source; consumer-local kind tables are an invariant breach.
