# Combined Compiler + Runtime Design v2

> **Status:** Approved working architecture  
> **Audience:** compiler, runtime, language-server, MCP, and documentation authors  
> **Role:** advanced successor to `docs\compiler-and-runtime-design.md`

## 1. Architectural frame

### The problem

Precept compiles a `.precept` source file into an executable contract that governs how a business entity's data evolves under declared rules. This creates a structural tension: authoring surfaces need the full analysis picture — including broken programs with syntax errors, unresolved references, and unproven safety obligations — while runtime surfaces need a validated, lowered model that executes deterministically without consulting source structure or proof internals.

Without an explicit split between these two worlds, two failure modes emerge. Runtime code acquires compile-time dependencies (syntax trees, parser recovery shape, proof graphs), making the evaluator fragile and untestable in isolation. Or authoring tools bypass analysis artifacts and reason directly over the runtime model, losing the ability to report errors, provide completions, or navigate incomplete programs.

This document defines the architecture that prevents both: two top-level products from one source (`CompilationResult` and `Precept`), with an explicit lowering boundary between them, and per-stage contracts that specify what each pipeline stage owns, produces, and who consumes it.

### Architectural commitments

These are the invariants this architecture enforces. A reader who reads only this list understands what the design commits to.

1. `CompilationResult` and `Precept` are distinct artifacts with distinct consumers.
2. `SyntaxTree` and `TypedModel` are distinct artifacts with distinct jobs.
3. The `TypedModel` anti-mirroring boundary is enforceable: symbols, bindings, normalized declarations, typed execution forms, dependency facts, and source-origin handles are mandatory inventory.
4. Graph and proof remain analysis artifacts; lowering alone builds the executable model.
5. Lowering must preserve distinct executable plan families for `always`, `in`, `to`, `from`, and `on` anchors.
6. Valid executable models report structured runtime outcomes; `Fault` is reserved for impossible-path invariant breaches.
7. The LS is a `CompilationResult` consumer first and a runtime consumer second, with exact feature-to-artifact boundaries.
8. MCP follows the same split.
9. Descriptor-backed runtime identity is the stable contract; current string placeholders are provisional only.
10. `TypedModel` is the compiler's resolved semantic contract. The runtime does not depend on `TypedModel` as a type — lowering transforms selected semantic knowledge into runtime-native descriptor and plan shapes. The runtime depends on lowered artifacts, not on compiler artifact types.
11. Consumers — LS, MCP, evaluator, and tests — must not maintain independent copies of catalog-defined vocabulary. Catalog metadata is the single source. Consumer-local kind tables or parallel keyword lists are an invariant breach and must be caught by drift tests.

### How to read this document

§1–4 describe the design decisions, principles, and artifact inventory. §5 maps each pipeline stage with its contract, inputs, outputs, and design rationale. §6–7 define which artifacts each consumer (language server, MCP, host applications) should read. §8 covers the three runtime result families — diagnostics, outcomes, and faults — and how they relate. Appendix A tracks current implementation status, which changes on a different cadence than the architecture itself.

### Design principles

**Metadata-driven architecture.** Catalogs in `src\Precept\Language\` define the language; pipeline stages consume metadata and add analysis. Stages are generic machinery — they do not encode language knowledge in their own logic.

**Earliest knowable metadata.** A stage stamps catalog identity onto its output as soon as that identity is knowable. Later stages carry it forward; no stage defers an assignment it could make.

**Distinct artifacts.** `SyntaxTree` is not `TypedModel`; `CompilationResult` is not `Precept`; proof is not runtime. Each artifact has exactly one owner and a defined set of consumers.

**Honest contracts.** This document distinguishes stable architectural contract from current implementation reality wherever they differ.

## 2. Canonical artifacts and the lowering boundary

Precept produces two top-level products from one `.precept` source:

1. **`CompilationResult`** — the immutable authoring and tooling snapshot. Always produced, even from broken input — authoring surfaces need the full analysis snapshot regardless of whether the program is valid.
2. **`Precept`** — the executable runtime model. Produced only from error-free compilations — it carries the lowered executable model that runtime surfaces need for execution and inspection, without syntax trees, proof internals, or parser recovery.

### What crosses the lowering boundary

Analysis artifacts and runtime artifacts serve different lifecycles. `Precept.From()` selectively lowers analysis knowledge — descriptors, constraint metadata, expression text, source references, execution plans — into runtime-native shapes. What does not cross: syntax trees, token streams, proof graphs, parser recovery, and graph topology as artifacts. The criterion for crossing is whether the runtime needs the information for execution or inspection, and whether it can be expressed in a runtime-native shape independent of compile-time artifact lifetimes.

The rule is **type dependency direction**: runtime types do not hold references to `CompilationResult`, `TypedModel`, or `SyntaxTree`. But runtime shapes do carry analysis-derived knowledge — `ConstraintDescriptor` carries expression text, source lines, scope targets, and guard metadata. That is analysis knowledge in lowered form, not a violation of the boundary. Artifacts don't cross; selected knowledge from artifacts crosses in runtime-native shapes.

### Artifact inventory

| Artifact | Layer | Classification | Current shape |
|---|---|---|---|
| `TokenStream` | lexical | compile-time | `TokenStream(ImmutableArray<Token> Tokens, ImmutableArray<Diagnostic> Diagnostics)` |
| `SyntaxTree` | syntactic | compile-time | `SyntaxTree(ImmutableArray<Diagnostic> Diagnostics)` |
| `TypedModel` | semantic | compile-time | `TypedModel(ImmutableArray<Diagnostic> Diagnostics)` |
| `GraphResult` | structural analysis | compile-time | `GraphResult(ImmutableArray<Diagnostic> Diagnostics)` |
| `ProofModel` | safety proof | compile-time | `ProofModel(ImmutableArray<Diagnostic> Diagnostics)` |
| `CompilationResult` | whole snapshot | compile-time | `CompilationResult(Tokens, SyntaxTree, Model, Graph, Proof, Diagnostics, HasErrors)` |
| descriptor tables | lowered runtime | runtime | `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `ConstraintDescriptor` in `Precept` sealed model |
| slot layout, dispatch indexes, action/expression plans | lowered runtime | runtime | lowered execution structures in `Precept` sealed model |
| constraint-plan indexes | lowered runtime | runtime | `always`, `in`, `to`, `from`, `on` plan families in `Precept` sealed model |
| fault-site backstops | lowered runtime | runtime | `FaultSiteDescriptor` + `FaultCode` in `Precept` sealed model |
| `Precept` | executable model | runtime | sealed class; public API stubbed |
| `Version` | entity snapshot | runtime | sealed record; public API stubbed |
| `EventOutcome`, `UpdateOutcome`, `RestoreOutcome` | runtime results | runtime | public DUs/records present |
| `ConstraintResult`, `ConstraintViolation` | runtime results | runtime | embedded in outcome and inspection records |
| `Fault` | runtime backstop | runtime | evaluator impossible-path defense only |

## 3. End-to-end chain

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

## 4. Source-of-truth inputs

Every stage begins from two roots:

**`.precept` source text** — the author-owned program.

**Catalogs** — `Tokens`, `Types`, `Functions`, `Operators`, `Operations`, `Modifiers`, `Actions`, `Constructs`, `Diagnostics`, `Faults`. These are the language specification in machine-readable form.

Catalogs enter as early as they are knowable. Later stages may add resolved meaning, but they must not recreate catalog truth by hardcoded switches when that truth already exists in metadata.

---

## 5. Stage-by-stage contract

### 5.1 Lexer — `Lexer.Lex(string source) -> TokenStream`

The lexer is the only stage with no semantic opinion — it converts raw text into classified tokens with exact spans. The key decision is that `TokenKind` comes directly from catalog metadata (`Tokens.GetMeta`), not from a parallel enum maintained by the lexer. This makes the lexer a vocabulary consumer, not a vocabulary owner.

**Purpose:** Convert raw text into a flat token stream with exact spans and lex diagnostics.
**Inputs:** `string source`; catalogs: `Tokens`, `Diagnostics`; supporting type: `SourceSpan`.
**Output:** `TokenStream(ImmutableArray<Token> Tokens, ImmutableArray<Diagnostic> Diagnostics)` where `Token` is `Token(TokenKind Kind, string Text, SourceSpan Span)`.
**Metadata entry:** `TokenKind` comes directly from `Tokens.GetMeta(...)` / `Tokens.Keywords`; token categories, TextMate scope, semantic token type, and completion hints remain derivable from `TokenMeta`.
**Consumers:** `Parser`; `CompilationResult`; LS lexical tokenization and grammar tooling.
**Current reality:** This is the one materially implemented compiler stage.

### 5.2 Parser — `Parser.Parse(TokenStream tokens) -> SyntaxTree`

The parser's job is structural fidelity, not semantic meaning. The key decision is that `SyntaxTree` preserves the author's source structure — including recovery shape for broken programs — without resolving names, types, or overloads. This separation exists because tooling needs source-faithful structure (folding, outline, recovery context) independently of semantic resolution.

**Purpose:** Convert the flat token stream into the parser-owned structural model of the authored program, including recovery shape.
**Inputs:** `TokenStream`; catalogs: `Constructs`, `Tokens`, `Operators`, `Diagnostics`.
**Output:** **Current:** `SyntaxTree(ImmutableArray<Diagnostic> Diagnostics)`. **Proposed:** `SyntaxTree(PreceptSyntax Root, ImmutableArray<Diagnostic> Diagnostics)` with source-faithful declaration and expression nodes, missing-node representation, and span ownership.
**Metadata entry:** The parser stamps syntax-level identities as soon as syntax alone can know them: construct kind, anchor keyword, action keyword, operator token, literal segment form.
**Consumers:** `TypeChecker`; LS syntax-facing features such as outline, folding, and recovery-aware local context.
**Current reality:** `Parser.Parse` is a stub; the parser contract is designed but not implemented.

### 5.3 `SyntaxTree` vs `TypedModel` — non-negotiable split

| Question | `SyntaxTree` owns | `TypedModel` owns |
|---|---|---|
| primary job | what the author wrote | what the program means |
| shape bias | source-faithful, recovery-aware | semantic, normalized |
| spans on malformed input | yes | only through semantic site handles / diagnostics |
| token adjacency / delimiters | yes | no |
| name/type resolution | no | yes |
| operation / overload identity | no | yes |
| best consumers | parser diagnostics, folding, source tools | LS intelligence, graph, proof, lowering |

Example:

| Source | `SyntaxTree` concern | `TypedModel` concern |
|---|---|---|
| `Approve.Amount <= RequestedAmount` | member-access node over exact tokens and spans | resolved event-arg symbol + field symbol + resolved `OperationKind` + result type `boolean` |

Architectural rule: the typed layer must feel like a semantic database, not an AST with annotations.

**Minimum required `TypedModel` inventory**

| Semantic inventory | Required contract |
|---|---|
| declaration symbols | Stable semantic identities for fields, states, events, args, and constraint-bearing declarations, each with declaration-origin handles for diagnostics and navigation. |
| reference bindings | Every semantic identifier/expression site binds directly to a symbol, overload, accessor, operator, or action identity. |
| normalized declarations | Rules, `in` ensures, `to` ensures, `from` ensures, `on` ensures, transition rows, access declarations, state hooks, and stateless hooks live in semantic inventories shaped for analysis and lowering rather than parser nesting. |
| typed expressions | Expression nodes carry resolved result type plus resolved operation/function/accessor identity and semantic subjects. |
| typed actions | Semantic action families resolve to one of three named shapes — `TypedAction`, `TypedInputAction`, or `TypedBindingAction` — with catalog-defined operand and binding contracts. Parser-shaped action nodes are not part of this inventory. |
| dependency facts | Computed-field dependencies, arg dependencies, referenced-field sets, and semantic edge data required by graph/proof/lowering are materialized here. |
| source-origin handles | Semantic sites keep stable links back to authored source spans/lines for diagnostics, hover, and go-to-definition without inheriting token adjacency. |

**Anti-mirroring enforcement rules**

1. `TypedModel` may reference authored source sites, but it must not preserve parser child layout, missing-node shape, delimiter ownership, or recovery nullability as its primary contract.
2. Hover, go-to-definition, semantic tokens, and semantic completions must be satisfiable from `TypedModel` bindings plus source-origin handles. If those features need to walk parser structure after binding, the typed boundary is underspecified.
3. `GraphResult`, `ProofModel`, and lowering consume normalized semantic inventories and bindings, not syntax nodes.
4. `SyntaxTree` remains the sole owner of recovery, token grouping, exact authored ordering, and malformed-construct shape.

### 5.4 Type checker — `TypeChecker.Check(SyntaxTree tree) -> TypedModel`

The type checker is the first stage that reasons about semantics rather than structure. The key decision is that type resolution is a separate pass from parsing — `TypedModel` is a projection of `SyntaxTree`, not an in-place annotation — because tooling and downstream stages need to reason about source structure and semantic meaning independently.

**Purpose:** Resolve names, scopes, types, overloads, modifiers, legal operations, typed actions, semantic subjects, and normalized declarations.
**Inputs:** `SyntaxTree`; catalogs: `Types`, `Functions`, `Operators`, `Operations`, `Modifiers`, `Actions`, `Constructs`, `Diagnostics`.
**Output:** **Current:** `TypedModel(ImmutableArray<Diagnostic> Diagnostics)`. **Proposed:** semantic symbol tables, binding indexes, normalized declaration inventories, typed expressions/actions, dependency facts, source-origin handles, and diagnostics.
**Metadata entry:** This is the first stage that resolves `TypeKind`, `FunctionKind`, `OperatorKind`, `OperationKind`, `ModifierMeta`, `ActionMeta`, `FunctionOverload`, `TypeAccessor`, and attached `ProofRequirement` records into semantic identity.
**Consumers:** `GraphAnalyzer`, `ProofEngine`, LS semantic tooling, MCP compile output, lowering.
**Current reality:** `TypeChecker.Check` is stubbed; the semantic model contract is ahead of implementation.

**Typed action family — three shapes only**

Actions in the typed model resolve to exactly one of three semantic shapes. No flat shape with optional fields, no additional shapes.

| Shape | Type name | Verb examples | Operand contract |
|---|---|---|---|
| base | `TypedAction` | `clear`, `reset` | none — value ownership is internal |
| operand-bearing | `TypedInputAction` | `set`, `add`, `remove`, `enqueue`, `push` | `InputExpression: TypedExpression` |
| binding | `TypedBindingAction` | `dequeue`, `pop` | `Binding: TypedBinding` |

The partition reflects verb-surface ownership: the base shape owns its value internally, the operand-bearing shape accepts a caller-supplied value, and the binding shape derives its value from a named source. A flat shape would require nullable fields on the majority of members.

Field naming discipline — use these names, not synonyms:

| Correct | Do not use |
|---|---|
| `InputExpression` | `Value`, `Input` |
| `Binding` | `IntoTarget` |
| `ConstraintActivation` | `EnsureBucketType` |
| `FaultSite` | `RuntimeCheckLocation` |

Lowering produces the matching executable family: `ExecutableAction`, `ExecutableInputAction`, and `ExecutableBindingAction`. The same naming discipline applies to the lowered shapes.

**Earliest-knowable kind assignment**

Each pipeline stage stamps catalog identity onto its output as early as that identity is determinable. No stage should defer an assignment it could make, and no stage should reach for a kind that requires a later stage's inputs.

| Stage | Kinds assigned |
|---|---|
| Parser | `ConstructKind`, `ActionKind`, `OperatorKind`, `TypeKind` on `TypeRef` nodes, `ModifierKind` |
| Type checker | `OperationKind`, `FunctionKind`, resolved `TypeAccessor`, resolved result `TypeKind` on typed expressions |

The parser stamps everything that syntax alone can determine. The type checker stamps everything that requires name, type, or overload resolution. The boundary is enforced: a kind that requires name resolution does not appear in the `SyntaxTree`, and a kind that syntax alone determines does not wait for the type checker.

### 5.5 Graph analyzer — `GraphAnalyzer.Analyze(TypedModel model) -> GraphResult`

The graph analyzer derives lifecycle structure from semantic declarations. The key decision is that graph analysis consumes the resolved `TypedModel` — not syntax — because reachability, dominance, and topology require resolved state/event/transition identity, not source-structural nesting.

**Purpose:** Derive lifecycle structure from the semantic definition: reachability, event availability, dominance-style facts, and runtime-reusable topology.
**Inputs:** `TypedModel`; catalogs: `Modifiers`, `Actions`, `Diagnostics`.
**Output:** **Current:** `GraphResult(ImmutableArray<Diagnostic> Diagnostics)`. **Proposed:** graph facts keyed by semantic identities plus diagnostics.
**Metadata entry:** State semantics such as `initial`, `terminal`, `required`, `irreversible`, `success`, `warning`, and `error` come from modifier metadata already resolved by the type checker; the analyzer must not reinterpret raw syntax.
**Consumers:** `ProofEngine`, `Precept.From`, LS structural diagnostics, runtime structural precomputation.
**Current reality:** `GraphAnalyzer.Analyze` is stubbed.

**Proposed `GraphResult` facts**

| Fact group | Example contents |
|---|---|
| reachability | initial state, reachable states, unreachable states |
| topology | edges, predecessors, successors, event coverage per state |
| structural validity | terminal outgoing-edge violations, required-state dominance, irreversible back-edge violations |
| runtime indexes | available events by state, state-scoped routing buckets, target-state facts lowering can reuse |

### 5.6 Proof engine — `ProofEngine.Prove(TypedModel model, GraphResult graph) -> ProofModel`

The proof engine is the last analysis stage before lowering. The key decision is that proof is bounded — four strategies only, no general SMT solver — and that proof stops at analysis. The runtime receives only lowered fault-site residue for defense-in-depth, not the proof graph itself.

**Purpose:** Discharge statically preventable runtime hazards by bounded abstract reasoning over the semantic and graph-resolved program.
**Inputs:** `TypedModel`, `GraphResult`; catalogs: `Operations`, `Functions`, `Types`, `Diagnostics`, `Faults`.
**Output:** **Current:** `ProofModel(ImmutableArray<Diagnostic> Diagnostics)`. **Proposed:** obligations, evidence, dispositions, preventable-fault links, diagnostics, and semantic site attribution.
**Metadata entry:** Proof obligations originate in metadata: `BinaryOperationMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`, `TypeAccessor.ProofRequirements`, and action metadata. `FaultCode` ↔ `DiagnosticCode` linkage remains catalog-owned as a prevention/backstop relationship.
**Consumers:** `CompilationResult`; LS/MCP proof reporting; lowering of fault residue into runtime backstops.
**Current reality:** `ProofEngine.Prove` is stubbed; only the catalog-side proof vocabulary exists today.

Boundary rule: proof stops at analysis. It does not build the executable runtime model, and runtime does not receive the whole proof graph — only lowered residue needed for defense-in-depth backstops.

**Proof strategy set**

The proof engine operates over a bounded, non-extensible strategy set. An obligation is discharged only when one of these four strategies applies:

| Strategy | When applicable |
|---|---|
| Literal proof | the value is a known compile-time literal; outcome is directly knowable |
| Modifier proof | the value flows through a catalog-defined modifier chain whose output bounds are statically determined |
| Guard-in-path proof | a guard expression in the control flow statically establishes a range or type constraint sufficient to discharge the obligation |
| Straightforward flow narrowing | type narrowing through the control-flow graph alone is sufficient to bound the value |

Any obligation outside this set is unresolvable by the compiler and emits a `Diagnostic`. New strategies are language changes, not tooling extensions.

**Proof/fault chain**

The end-to-end prevention/backstop chain is:

```
catalog metadata → ProofRequirement → ProofObligation → DiagnosticCode → FaultCode → FaultSiteDescriptor
```

| Link | Owner | Meaning |
|---|---|---|
| catalog metadata → `ProofRequirement` | catalog entries (`BinaryOperationMeta`, `FunctionOverload`, `TypeAccessor`, action metadata) | declares what must be provable at the call site |
| `ProofRequirement` → `ProofObligation` | proof engine | instantiates the requirement against a specific semantic site |
| `ProofObligation` → `DiagnosticCode` | proof engine + diagnostics catalog | records an unresolved obligation as an authoring-time finding |
| `DiagnosticCode` → `FaultCode` | diagnostics catalog | prevention counterpart: the diagnostic that should have blocked this site from compiling |
| `FaultCode` → `FaultSiteDescriptor` | lowering + evaluator | if the site survives to runtime (defense-in-depth only), a `FaultSiteDescriptor` identifies it for the backstop |

`FaultSiteDescriptor` is not an ordinary runtime result; it is the runtime face of an impossible path that a correct program never reaches.

### 5.7 Whole-pipeline snapshot — `Compiler.Compile(string source) -> CompilationResult`

`CompilationResult` is an aggregation boundary, not a reasoning stage. The key decision is that it captures the complete analysis pipeline as one immutable snapshot, so consumers (LS, MCP, lowering) can access any stage's output without re-running the pipeline or managing individual stage artifacts.

**Purpose:** Produce one immutable snapshot of the full compiler pipeline.
**Inputs:** Raw source + the five pipeline stages.
**Output:** `CompilationResult(TokenStream Tokens, SyntaxTree SyntaxTree, TypedModel Model, GraphResult Graph, ProofModel Proof, ImmutableArray<Diagnostic> Diagnostics, bool HasErrors)`.
**Metadata entry:** None added here; this is an aggregation boundary, not a reasoning stage.
**Consumers:** LS, MCP `precept_compile`, `Precept.From`.
**Current reality:** This wiring exists and merges diagnostics correctly, but four of the five stages are still hollow.

### 5.8 Lowering boundary — `Precept.From(CompilationResult compilation) -> Precept`

Lowering is the one-way gate between analysis and runtime. The key decision is that `Precept.From()` is the sole owner of this transformation — no other code path builds the runtime model — and that it selectively transforms analysis knowledge into runtime-native shapes rather than copying or referencing compile-time artifacts.

**Purpose:** Lower the analysis snapshot into the executable runtime model.
**Inputs:** Error-free `CompilationResult`; semantic inputs come from `TypedModel`, `GraphResult`, and proof residue from `ProofModel`; catalogs are not re-read here.
**Output:** `Precept` as a sealed executable model owning descriptor tables, slot layout, dispatch indexes, lowered execution plans, explicit constraint-plan indexes, inspection metadata, and fault-site backstops.
**Metadata entry:** Catalog metadata reaches runtime only in lowered semantic form: descriptor identity, resolved operation/function/action identity, constraint descriptors, and proof-owned fault-site residue.
**Consumers:** `Precept.Create`, `Precept.Restore`, `Version` operations, MCP runtime tools, host applications.
**Current reality:** `Precept.From` currently checks `HasErrors` and then throws `NotImplementedException`.

**Lowered executable-model contract**

| Runtime concern | Lowered structure | Consumed by |
|---|---|---|
| identity | descriptor tables for `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `ConstraintDescriptor` | every runtime API surface |
| storage | slot layout, field-to-slot map, default-value plan, omission metadata | create, restore, fire, update |
| routing | per-state and stateless event-row dispatch indexes, target-state routing metadata | fire and inspect fire |
| execution | lowered expression nodes and action plans keyed to descriptors and resolved semantic identities | evaluator |
| recomputation | dependency graph and evaluation order for computed fields | fire, update, restore, inspect |
| access | per-state field access-mode index and query surface | update and inspect update |
| constraints | explicit executable plan indexes for `always`, `in`, `to`, `from`, and `on` anchors | create, restore, fire, update, inspect |
| inspection | row/source/result-shaping metadata needed to produce `EventInspection`, `RowInspection`, `UpdateInspection`, `ConstraintResult`, and `FieldSnapshot` | inspection surfaces |
| fault backstops | `FaultSite`/fault-site descriptors linked to `FaultCode` and prevention `DiagnosticCode` | impossible-path defense only |

**Operation-facing plan selection**

| Operation | Required lowered contract |
|---|---|
| `Create` | default-value plan, initial-state seed, optional initial-event descriptor/arg contract, then shared fire-path execution |
| `Restore` | slot population, descriptor validation, recomputation, `always` + `in <current>` constraint plans; no access checks and no row dispatch |
| `Fire` | row dispatch, action plans, recomputation, `always` + `from <current>` + `on <event>` + `to <target>` constraint plans |
| `Update` | access-mode index, patch validation, recomputation, `always` + `in <current>` constraint plans; `InspectUpdate` additionally runs event-prospect evaluation over the hypothetical state |

The anchor taxonomy is explicit and non-flattened: runtime lowering must preserve `always`, `in`, `to`, `from`, and `on` as distinct executable-plan families.

**Constraint activation indexes**

The five constraint-plan families are accessed through four precomputed activation indexes. These are built once during lowering and keyed to descriptor identity — not recalculated at runtime.

| Index | Key type | Covers |
|---|---|---|
| always index | global | rules and ensures with no state or event anchor; active on every operation |
| state activation index | `(StateDescriptor, ConstraintActivation)` | `InState`, `FromState`, and `ToState` anchors |
| event activation index | `EventDescriptor` | `on Event ensure` anchors |
| event availability index | `(StateDescriptor?, EventDescriptor)` | available-event scope; null state key applies to stateless precepts |

The `ConstraintActivation` discriminant in the state activation index distinguishes whether a constraint binds to the current state, the source state of a transition, or the target state of a transition. Callers do not compute this at dispatch time; they look up a prebuilt bucket.

**Stable contract vs provisional current surface**

The stable runtime contract is descriptor-backed. Current public stubs still expose string placeholders and string-selected entry points because D8/R4 is unresolved. Those strings are provisional implementation placeholders, not the architectural end state.

### 5.9 Evaluator — shared runtime executor

The evaluator consumes only lowered artifacts — no syntax, no catalogs, no compile-time types. The key decision is that it is a plan executor, not a reasoning engine: execution semantics are fully determined at lowering time, and the evaluator simply runs the prebuilt plans. This keeps the runtime testable in isolation.

**Purpose:** Execute or inspect runtime plans without consulting syntax or raw catalogs.
**Inputs:** `Precept`, `Version`, descriptor-keyed arguments or patches, lowered execution plans, explicit constraint-plan indexes, and fault-site backstops.
**Output:** Operation-specific results: `EventOutcome`, `UpdateOutcome`, `RestoreOutcome`, `EventInspection`, `UpdateInspection`, `RowInspection`; impossible-path invariant breaches classify as `Fault`.
**Metadata entry:** None newly derived; evaluator consumes lowered metadata only.
**Consumers:** `Precept` and `Version` façades.
**Current reality:** `Evaluator` exists, but every operation body is a stub. `Fail(FaultCode, ...)` already routes through `Faults.Create(...)`.

Runtime rule: valid executable models do not produce in-domain runtime errors. Expected runtime behavior is expressed as structured outcomes and inspections; `Fault` is reserved for defense-in-depth classification of impossible-path engine invariant breaches.

**Constraint evaluation matrix**

Every operation evaluates constraints through the same lowered plan indexes. Access-mode checks and row dispatch are independent of constraint evaluation.

| Operation | Access-mode checks | Row dispatch | Constraint plans evaluated |
|---|---|---|---|
| `Fire` | no | yes | `always`, `from <current>`, `on <event>`, `to <target>` |
| `InspectFire` | no | yes | same as `Fire` |
| `Update` | yes | no | `always`, `in <current>` |
| `InspectUpdate` | yes | no | same as `Update`, plus event-prospect evaluation over the hypothetical state |
| `Create` with initial event | no | yes (initial event) | `always`, plus initial-event fire-path plans |
| `Create` without initial event | no | no | `always`, `in <initial>` |
| `Restore` | no | no | `always`, `in <current>` |

Two rules that must hold across this matrix:

1. `Restore` bypasses access-mode checks and row dispatch, but does **not** bypass constraint evaluation.
2. `to` ensures are transitional; they are not part of the standing state surface and do not participate in `in`-anchor evaluation.

Inspection and commit paths execute the same lowered plans. Disposition alone differs — report vs. enforce.

### 5.10 Create / InspectCreate — `Precept.Create(...)`, `Precept.InspectCreate(...)`

Create is the entity's entry point. The key decision is that creation with an initial event reuses the full fire-path execution — it is not a separate code path — ensuring that initial-event constraints, actions, and transitions apply identically to the creation flow.

**Purpose:** Construct the first valid `Version`, optionally by atomically firing the declared initial event.
**Inputs:** `Precept`; lowered defaults, `InitialState`, `InitialEvent`, arg descriptors, and the same fire-path runtime plans used by event execution.
**Output:** `Create` returns `EventOutcome`; `InspectCreate` returns `EventInspection`. Success yields `Applied(Version)` or `Transitioned(Version)`.
**Metadata reflection:** Default values, initial-event arg contract, field descriptors, and applicable constraint descriptors are consumed directly from the lowered model.
**Consumers:** Host applications, onboarding UI, MCP runtime preview patterns.
**Current reality:** Public methods exist and are documented, but both bodies are stubbed.

### 5.11 Restore — `Precept.Restore(...)`

Restore reconstitutes persisted data under the current definition. The key decision is that Restore validates rather than trusts — it runs constraint evaluation but intentionally bypasses access-mode restrictions, because persisted data represents a prior valid state, not an active field edit.

**Purpose:** Reconstitute persisted entity data under the current definition, validating rather than trusting storage.
**Inputs:** `Precept`; caller-supplied persisted state and fields; lowered descriptors, slot validation, recomputation, and applicable restore constraint plans.
**Output:** `RestoreOutcome` with `Restored(Version)`, `RestoreConstraintsFailed(IReadOnlyList<ConstraintViolation>)`, or `RestoreInvalidInput(string Reason)`.
**Metadata reflection:** Uses the same lowered field/state/constraint descriptors as commit paths, but intentionally bypasses access-mode restrictions.
**Consumers:** Persistence boundaries, migration tooling later.
**Current reality:** Public DU exists; method body is a stub.

### 5.12 Fire / InspectFire — `Version.Fire(...)`, `Version.InspectFire(...)`

Fire is the core state-machine operation. The key decision is that routing, action execution, transition, recomputation, and constraint evaluation are a single atomic pipeline — not composable steps that callers assemble — because partial execution would violate the determinism guarantee.

**Purpose:** Execute or preview event routing, action application, transition, recomputation, and constraint evaluation.
**Inputs:** `Version`; event descriptors, arg descriptors, row dispatch tables, lowered action plans, recomputation index, explicit anchor-plan indexes, and fault sites.
**Output:** `EventOutcome` (`Transitioned`, `Applied`, `Rejected`, `InvalidArgs`, `EventConstraintsFailed`, `Unmatched`, current provisional `UndefinedEvent`) and `EventInspection` / `RowInspection`.
**Metadata reflection:** Constraint identity survives into `ConstraintResult` and `ConstraintViolation` through `ConstraintDescriptor`. Routing uses descriptor-backed row identity; any remaining event-name string lookup is provisional.
**Consumers:** Host applications, MCP `precept_fire`, runtime preview, future LS preview surfaces.
**Current reality:** Public outcome types are implemented; `Version` methods and evaluator bodies are stubbed. String parameters are explicit TODO placeholders pending descriptor-based D8/R4 work.

### 5.13 Update / InspectUpdate — `Version.Update(...)`, `Version.InspectUpdate(...)`

Update is the direct-edit operation. The key decision is that field edits are governed by access-mode declarations and constraint evaluation — they are not raw writes — and that `InspectUpdate` additionally evaluates the event landscape over the hypothetical post-patch state.

**Purpose:** Execute or preview direct field edits under access-mode and constraint governance.
**Inputs:** `Version`; field descriptors, per-state access facts, recomputation dependencies, `always` / `in` constraint plans, and event-prospect evaluation over a hypothetical state.
**Output:** `UpdateOutcome` (`FieldWriteCommitted`, `UpdateConstraintsFailed`, `AccessDenied`, `InvalidInput`) and `UpdateInspection`.
**Metadata reflection:** Access modes and constraint identity come from lowered descriptors; `UpdateInspection.Events` reuses event inspection against the hypothetical post-patch state.
**Consumers:** Application edit flows, MCP `precept_update`, runtime preview.
**Current reality:** Public shapes exist; method bodies are stubbed; string-based field identity remains provisional.

---

## 6. Language-server consumption contract

The language server must consume artifacts by responsibility, not convenience.

| LS feature | Exact artifact contract | Not the default dependency |
|---|---|---|
| keyword / operator / punctuation / literal / comment classification | `TokenStream` + token metadata (`TokenMeta` / token categories / TextMate scope data) | `SyntaxTree`, `TypedModel`, `Precept` |
| syntax-aware outline / folding / recovery | `SyntaxTree` | `TypedModel` |
| diagnostics list | merged `CompilationResult.Diagnostics` | direct stage-specific polling |
| semantic tokens for identifiers | `TypedModel` symbol/reference bindings + semantic source-origin spans | token categories alone or parser heuristics |
| completions | catalogs for candidate inventory; `SyntaxTree` for local parse context; `TypedModel` for scope, binding, expected type, and legal semantic choices | `GraphResult` / `ProofModel` as baseline engines |
| hover | `TypedModel` semantic site + catalog documentation / signatures / descriptions | raw syntax or text search |
| go-to-definition | `TypedModel` reference binding + declaration-origin handles | syntax-tree guessing or name search |
| preview / inspect | lowered `Precept` + runtime inspection, only when `!HasErrors` | `CompilationResult` after lowering succeeds |
| graph / proof explanation surfaces | `GraphResult` and `ProofModel` when explicitly surfacing unreachable-state or proof information | everyday completion / hover / tokenization paths |

Two hard rules:

1. Do not make semantic LS features consume `SyntaxTree` because the typed layer is underspecified.
2. Do not make preview/runtime LS features consume `CompilationResult` after lowering succeeds.

**Current implementation reality:** `tools\Precept.LanguageServer\Program.cs` only boots the server and waits for exit. The matrix above is a contract for later implementation, not a description of current behavior.

## 7. Runtime and tooling consumer split

| Consumer | Correct artifact |
|---|---|
| LS diagnostics / semantic tokens / completions / hover / definition | `CompilationResult` |
| MCP `precept_language` | catalogs directly |
| MCP `precept_compile` | `CompilationResult` |
| MCP `precept_inspect` | `Precept` + inspection runtime |
| MCP `precept_fire` | `Precept` / `Version.Fire` |
| MCP `precept_update` | `Precept` / `Version.Update` |
| host application authoring-time validation | `CompilationResult` |
| host application execution | `Precept` + `Version` |

## 8. Diagnostics, outcomes, and faults

Precept's promise is not that runtime faults are merely classified well. It is that valid executable models do not surface in-domain runtime errors. The three result families have different jobs, and collapsing them would undermine the guarantee.

### 8.1 The three result families

| Surface | Produced by | Meaning |
|---|---|---|
| `Diagnostic` | compiler pipeline | authoring-time finding against source |
| runtime outcome / inspection | runtime API | expected success, domain rejection, or boundary-validation result |
| `Fault` | evaluator backstop | impossible-path engine invariant breach |

`Precept.From(compilation)` is the gate. Error diagnostics stop executable-model construction. Once a valid `Precept` exists, normal runtime behavior is described by structured outcomes, not by faults.

**Non-symmetry rule:** every `FaultCode` has a compiler-owned diagnostic counterpart in the prevention sense, but many diagnostics have no runtime fault counterpart, and many runtime outcomes are intentionally modeled as normal results rather than faults.

| Category | Compile-time surface | Runtime surface | Meaning in a valid executable model |
|---|---|---|---|
| authoring defect (lex / parse / type / graph / proof) | `Diagnostic` only | no runtime surface; `Precept` is not constructed | authoring-time problem |
| proof obligation not discharged | `Diagnostic` only | no runtime surface; `Precept` is not constructed | compiler blocks executable model until the hazard is prevented |
| business prohibition or rule failure | may have no compile-time issue | `Rejected`, `EventConstraintsFailed`, `UpdateConstraintsFailed`, `RestoreConstraintsFailed` | normal domain-governed outcome |
| routing / availability result | may have no compile-time issue | `Unmatched` and current provisional `UndefinedEvent` | normal runtime selection / boundary result |
| caller input / persisted-data mismatch | descriptor/type contracts exist, but not as source diagnostics for that invocation | `InvalidArgs`, `InvalidInput`, `RestoreInvalidInput`, `AccessDenied` | normal boundary-validation outcome |
| impossible-path invariant breach | compiler-owned prevention rule exists in catalog metadata | `Fault` only if runtime somehow reaches the site | defense-in-depth backstop; should be unreachable |

### 8.2 Commit and inspection surfaces

**Commit outcomes by operation:**

| Operation | Success | Domain outcome | Boundary-validation outcome | Engine invariant breach |
|---|---|---|---|---|
| `Create` / `Fire` | `Applied`, `Transitioned` | `Rejected`, `EventConstraintsFailed`, `Unmatched` | `InvalidArgs`, current provisional `UndefinedEvent` | `Fault` |
| `Update` | `FieldWriteCommitted` | `UpdateConstraintsFailed`, `AccessDenied` | `InvalidInput` | `Fault` |
| `Restore` | `Restored` | `RestoreConstraintsFailed` | `RestoreInvalidInput` | `Fault` |

`UndefinedEvent` and other string-selected invalid-input cases are part of the current provisional surface, not a permanent endorsement of string-lookup-era runtime identity. Descriptor-backed APIs may narrow or eliminate some of these branches.

**Inspection types:** `EventInspection` provides the reduced event-level landscape. `RowInspection` provides per-row prospect, effect, snapshots, and constraints. `UpdateInspection` provides hypothetical field state plus the resulting event landscape. `ConstraintResult` carries evaluation status referencing `ConstraintDescriptor`. `FieldSnapshot` captures resolved or unresolved field value in hypothetical state.

Inspection must share the same lowered plans as commit. It is not a second evaluator.

### 8.3 Constraint query contract

Three tiers form the constraint query and inspection contract. Each tier is additive in specificity.

| Tier | Surface | Contents | When available |
|---|---|---|---|
| definition | `Precept.Constraints` | every declared `ConstraintDescriptor` in the definition | always, from the lowered model |
| applicable | `Version.ApplicableConstraints` | the zero-cost subset active for current state and context | from any live `Version` |
| evaluated | `ConstraintResult` / `ConstraintViolation` | what was actually checked during a specific operation | embedded in outcome and inspection results only |

The definition tier enumerates the full catalog. The applicable tier narrows to the live context without evaluation. The evaluated tier records actual decisions made during a specific operation execution.

---

## Appendix A: Implementation status

*Implementation status changes on a different cadence than architectural decisions. This appendix tracks current reality; the main document tracks the stable contract.*

| Area | Current state | Required state |
|---|---|---|
| lexer | implemented | keep as lexical truth source |
| parser | stub | build real `SyntaxTree.Root` and recovery shape |
| typed model | diagnostics-only stub | become the semantic model with anti-mirroring contract |
| graph | diagnostics-only stub | carry real topology and runtime indexes |
| proof | diagnostics-only stub | carry obligations, evidence, and preventable-fault links |
| lowering | error guard + stub | build descriptors, plans, and executable indexes |
| runtime operations | public shapes, no bodies | full evaluator-driven behavior |
| language server | bootstrap only | consume tokens/tree/model/runtime by responsibility |
| runtime API identity | string placeholders for fields/events/args | descriptor-based public API |

**Implementation action items**

Concrete deliverables that follow from this document's contracts:

1. **Define concrete descriptor types.** `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, and `ConstraintDescriptor` must be defined as first-class sealed types — not string aliases or late-bound lookup keys. Every runtime API surface routes through descriptor identity.

2. **Define lowered constraint anchor shapes.** Each anchor family (`always`, `in`, `to`, `from`, `on`) requires a distinct internal lowered shape. A single flat constraint record spanning all families is prohibited; the evaluation matrix depends on the families being separately addressable.

3. **Thread `FaultSiteDescriptor` through evaluator dispatch.** Every backstop site in the evaluator must carry a resolved `FaultSiteDescriptor` linked to its `FaultCode`. Unadorned `Fault` calls with no site context are incomplete.

4. **Update LS and MCP DTOs.** When descriptor types are defined, language-server and MCP data-transfer objects must be updated to match the new descriptor contracts. Parallel string-keyed shapes in those surfaces are provisional scaffolding, not permanent architecture.

5. **Add drift tests.** Write tests that fail if any consumer — LS, MCP, evaluator, or test helpers — begins maintaining a parallel copy of catalog-defined vocabulary. Catalogs are the single source; consumer-local kind tables are a maintenance invariant breach, and the test suite should catch it.
