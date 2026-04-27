# Combined Compiler + Runtime Design v2

> **Status:** Shared v2 architecture draft  
> **Audience:** compiler, runtime, language-server, MCP, and documentation authors  
> **Role:** advanced successor to `docs\compiler-and-runtime-design.md`

## 1. Architectural frame

Precept has two top-level products from one `.precept` source:

1. **`CompilationResult`** — the immutable authoring and tooling snapshot. Always produced, even on broken input.
2. **`Precept`** — the executable runtime model. Produced only from an error-free `CompilationResult`.

That split is structural, not cosmetic. `CompilationResult` exists so authoring surfaces can reason over broken programs without pretending they are executable. `Precept` exists so runtime surfaces can execute a lowered, descriptor-backed model without dragging syntax, proof internals, or parser recovery shape into the evaluator.

| Principle | Meaning here |
|---|---|
| Metadata-driven architecture | Catalogs in `src\Precept\Language\` define the language; stages consume metadata and add analysis. |
| Earliest knowable metadata | A stage stamps catalog identity onto its output as soon as that identity is knowable, then later stages carry it forward. |
| Distinct artifacts | `SyntaxTree` is not `TypedModel`; `CompilationResult` is not `Precept`; proof is not runtime. |
| Honest contracts | This document distinguishes stable architectural contract from current implementation reality wherever they differ. |

## 2. Canonical artifacts

| Layer | Artifact | Current shape | Intended role |
|---|---|---|---|
| lexical | `TokenStream` | `TokenStream(ImmutableArray<Token> Tokens, ImmutableArray<Diagnostic> Diagnostics)` | tokenized source + lex diagnostics |
| syntactic | `SyntaxTree` | `SyntaxTree(ImmutableArray<Diagnostic> Diagnostics)` | parser-owned source structure and recovery shape |
| semantic | `TypedModel` | `TypedModel(ImmutableArray<Diagnostic> Diagnostics)` | resolved domain meaning |
| structural analysis | `GraphResult` | `GraphResult(ImmutableArray<Diagnostic> Diagnostics)` | reachability, topology, lifecycle facts |
| safety proof | `ProofModel` | `ProofModel(ImmutableArray<Diagnostic> Diagnostics)` | proof obligations, evidence, preventable-fault links |
| whole snapshot | `CompilationResult` | `CompilationResult(Tokens, SyntaxTree, Model, Graph, Proof, Diagnostics, HasErrors)` | immutable whole-pipeline view |
| lowered executable | `Precept` | sealed class; public API stubbed | runtime-native definition |
| entity snapshot | `Version` | sealed record; public API stubbed | immutable per-instance state |
| runtime surfaces | `EventOutcome`, `UpdateOutcome`, `RestoreOutcome`, inspection records, `Fault` | public DUs/records present | runtime result contracts |

**Compile-time vs lowered artifact classification**

The boundary between analysis artifacts and runtime artifacts is a hard line. Nothing from the compile-time half is present in a live `Precept` or `Version`; nothing from the lowered half is present in a `CompilationResult`.

| Artifact | Classification | Where it lives |
|---|---|---|
| `TokenStream` | compile-time only | `CompilationResult.Tokens` |
| `SyntaxTree` | compile-time only | `CompilationResult.SyntaxTree` |
| `TypedModel` | compile-time only | `CompilationResult.Model` |
| `GraphResult` | compile-time only | `CompilationResult.Graph` |
| `ProofModel` | compile-time only | `CompilationResult.Proof` |
| `CompilationResult` | compile-time only | returned by `Compiler.Compile` |
| descriptor tables (`FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `ConstraintDescriptor`) | lowered runtime | `Precept` sealed model |
| slot layout + default-value plan | lowered runtime | `Precept` sealed model |
| dispatch indexes | lowered runtime | `Precept` sealed model |
| executable action and expression plans | lowered runtime | `Precept` sealed model |
| constraint-plan indexes (`always`, `in`, `to`, `from`, `on`) | lowered runtime | `Precept` sealed model |
| fault-site backstops (`FaultSiteDescriptor`, `FaultCode`) | lowered runtime | `Precept` sealed model |
| `Version` | runtime instance state | produced by `Precept` operations |
| `EventOutcome`, `UpdateOutcome`, `RestoreOutcome` | runtime results | returned by `Version` and `Precept` |
| `ConstraintResult`, `ConstraintViolation` | runtime results | embedded in outcome and inspection records |
| `Fault` | runtime backstop | evaluator impossible-path defense only |

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

| Root input | Meaning |
|---|---|
| `.precept` source text | author-owned program |
| catalogs | `Tokens`, `Types`, `Functions`, `Operators`, `Operations`, `Modifiers`, `Actions`, `Constructs`, `Diagnostics`, `Faults` |

Catalogs enter as early as they are knowable. Later stages may add resolved meaning, but they must not recreate catalog truth by hardcoded switches when that truth already exists in metadata.

---

## 5. Stage-by-stage contract

### 5.1 Lexer — `Lexer.Lex(string source) -> TokenStream`

| Item | Contract |
|---|---|
| purpose | Convert raw text into a flat token stream with exact spans and lex diagnostics. |
| inputs | previous artifact: none; direct input: `string source`; catalogs: `Tokens`, `Diagnostics`; supporting type: `SourceSpan`. |
| output shape | **Current + intended:** `TokenStream(ImmutableArray<Token> Tokens, ImmutableArray<Diagnostic> Diagnostics)` where `Token` is `Token(TokenKind Kind, string Text, SourceSpan Span)`. |
| metadata entry | `TokenKind` comes directly from `Tokens.GetMeta(...)` / `Tokens.Keywords`; token categories, TextMate scope, semantic token type, and completion hints remain derivable from `TokenMeta`. |
| downstream consumers | `Parser`; `CompilationResult`; LS lexical tokenization and grammar tooling. |
| current reality | This is the one materially implemented compiler stage. The stage boundary is real, not aspirational. |

### 5.2 Parser — `Parser.Parse(TokenStream tokens) -> SyntaxTree`

| Item | Contract |
|---|---|
| purpose | Convert the flat token stream into the parser-owned structural model of the authored program, including recovery shape. |
| inputs | previous artifact: `TokenStream`; catalogs: `Constructs`, `Tokens`, `Operators`, `Diagnostics`. |
| output shape | **Current:** `SyntaxTree(ImmutableArray<Diagnostic> Diagnostics)`. **Proposed:** `SyntaxTree(PreceptSyntax Root, ImmutableArray<Diagnostic> Diagnostics)` with source-faithful declaration and expression nodes, missing-node representation, and span ownership. |
| metadata entry | The parser stamps syntax-level identities as soon as syntax alone can know them: construct kind, anchor keyword, action keyword, operator token, literal segment form. |
| downstream consumers | `TypeChecker`; LS syntax-facing features such as outline, folding, and recovery-aware local context. |
| current reality | `Parser.Parse` is a stub throwing `NotImplementedException`; the parser contract is designed but not implemented. |

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

That is the enforceable anti-mirroring boundary.

### 5.4 Type checker — `TypeChecker.Check(SyntaxTree tree) -> TypedModel`

| Item | Contract |
|---|---|
| purpose | Resolve names, scopes, types, overloads, modifiers, legal operations, typed actions, semantic subjects, and normalized declarations. |
| inputs | previous artifact: `SyntaxTree`; catalogs: `Types`, `Functions`, `Operators`, `Operations`, `Modifiers`, `Actions`, `Constructs`, `Diagnostics`. |
| output shape | **Current:** `TypedModel(ImmutableArray<Diagnostic> Diagnostics)`. **Proposed:** semantic symbol tables, binding indexes, normalized declaration inventories, typed expressions/actions, dependency facts, source-origin handles, and diagnostics. |
| metadata entry | This is the first stage that resolves `TypeKind`, `FunctionKind`, `OperatorKind`, `OperationKind`, `ModifierMeta`, `ActionMeta`, `FunctionOverload`, `TypeAccessor`, and attached `ProofRequirement` records into semantic identity. |
| downstream consumers | `GraphAnalyzer`, `ProofEngine`, LS semantic tooling, MCP compile output, lowering. |
| current reality | `TypeChecker.Check` is stubbed; the semantic model contract is ahead of implementation. |

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

| Item | Contract |
|---|---|
| purpose | Derive lifecycle structure from the semantic definition: reachability, event availability, dominance-style facts, and runtime-reusable topology. |
| inputs | previous artifact: `TypedModel`; catalogs: `Modifiers`, `Actions`, `Diagnostics`. |
| output shape | **Current:** `GraphResult(ImmutableArray<Diagnostic> Diagnostics)`. **Proposed:** graph facts keyed by semantic identities plus diagnostics. |
| metadata entry | State semantics such as `initial`, `terminal`, `required`, `irreversible`, `success`, `warning`, and `error` come from modifier metadata already resolved by the type checker; the analyzer must not reinterpret raw syntax. |
| downstream consumers | `ProofEngine`, `Precept.From`, LS structural diagnostics, runtime structural precomputation. |
| current reality | `GraphAnalyzer.Analyze` is stubbed. |

**Proposed `GraphResult` facts**

| Fact group | Example contents |
|---|---|
| reachability | initial state, reachable states, unreachable states |
| topology | edges, predecessors, successors, event coverage per state |
| structural validity | terminal outgoing-edge violations, required-state dominance, irreversible back-edge violations |
| runtime indexes | available events by state, state-scoped routing buckets, target-state facts lowering can reuse |

### 5.6 Proof engine — `ProofEngine.Prove(TypedModel model, GraphResult graph) -> ProofModel`

| Item | Contract |
|---|---|
| purpose | Discharge statically preventable runtime hazards by bounded abstract reasoning over the semantic and graph-resolved program. |
| inputs | previous artifacts: `TypedModel`, `GraphResult`; catalogs: `Operations`, `Functions`, `Types`, `Diagnostics`, `Faults`. |
| output shape | **Current:** `ProofModel(ImmutableArray<Diagnostic> Diagnostics)`. **Proposed:** obligations, evidence, dispositions, preventable-fault links, diagnostics, and semantic site attribution. |
| metadata entry | Proof obligations originate in metadata: `BinaryOperationMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`, `TypeAccessor.ProofRequirements`, and action metadata. `FaultCode` ↔ `DiagnosticCode` linkage remains catalog-owned as a prevention/backstop relationship. |
| downstream consumers | `CompilationResult`; LS/MCP proof reporting; lowering of fault residue into runtime backstops. |
| current reality | `ProofEngine.Prove` is stubbed; only the catalog-side proof vocabulary exists today. |

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

| Item | Contract |
|---|---|
| purpose | Produce one immutable snapshot of the full compiler pipeline. |
| inputs | raw source + the five pipeline stages. |
| output shape | `CompilationResult(TokenStream Tokens, SyntaxTree SyntaxTree, TypedModel Model, GraphResult Graph, ProofModel Proof, ImmutableArray<Diagnostic> Diagnostics, bool HasErrors)`. |
| metadata entry | none added here; this is an aggregation boundary, not a reasoning stage. |
| downstream consumers | LS, MCP `precept_compile`, `Precept.From`. |
| current reality | This wiring exists and merges diagnostics correctly, but four of the five stages are still hollow. |

### 5.8 Lowering boundary — `Precept.From(CompilationResult compilation) -> Precept`

| Item | Contract |
|---|---|
| purpose | Lower the analysis snapshot into the executable runtime model. |
| inputs | previous artifact: error-free `CompilationResult`; semantic inputs come from `TypedModel`, `GraphResult`, and proof residue from `ProofModel`; catalogs are not re-read ad hoc here. |
| output shape | `Precept` as a sealed executable model owning descriptor tables, slot layout, dispatch indexes, lowered execution plans, explicit constraint-plan indexes, inspection metadata, and fault-site backstops. |
| metadata entry | Catalog metadata reaches runtime only in lowered semantic form: descriptor identity, resolved operation/function/action identity, constraint descriptors, and proof-owned fault-site residue. |
| downstream consumers | `Precept.Create`, `Precept.Restore`, `Version` operations, MCP runtime tools, host applications. |
| current reality | `Precept.From` currently checks `HasErrors` and then throws `NotImplementedException`. |

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

The anchor taxonomy is therefore explicit and non-flattened: runtime lowering must preserve `always`, `in`, `to`, `from`, and `on` as distinct executable-plan families.

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

The stable runtime contract is descriptor-backed. Current public stubs still expose string placeholders and string-selected entry points because D8/R4 is unresolved. Those strings are provisional implementation placeholders, not the architectural end state. Detailed runtime docs that follow from this document must treat descriptor-backed identity as permanent contract and string lookup behavior as current-era compatibility scaffolding only.

### 5.9 Evaluator — shared runtime executor

| Item | Contract |
|---|---|
| purpose | Execute or inspect runtime plans without consulting syntax or raw catalogs. |
| inputs | `Precept`, `Version`, descriptor-keyed arguments or patches, lowered execution plans, explicit constraint-plan indexes, and fault-site backstops. |
| output shape | operation-specific results: `EventOutcome`, `UpdateOutcome`, `RestoreOutcome`, `EventInspection`, `UpdateInspection`, `RowInspection`; impossible-path invariant breaches classify as `Fault`. |
| metadata entry | none newly derived; evaluator consumes lowered metadata only. |
| downstream consumers | `Precept` and `Version` façades. |
| current reality | `Evaluator` exists, but every operation body is a stub. `Fail(FaultCode, ...)` already routes through `Faults.Create(...)`. |

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

| Item | Contract |
|---|---|
| purpose | Construct the first valid `Version`, optionally by atomically firing the declared initial event. |
| inputs | previous artifact: `Precept`; lowered defaults, `InitialState`, `InitialEvent`, arg descriptors, and the same fire-path runtime plans used by event execution. |
| output shape | `Create` returns `EventOutcome`; `InspectCreate` returns `EventInspection`. Success yields `Applied(Version)` or `Transitioned(Version)`. |
| metadata reflection | Default values, initial-event arg contract, field descriptors, and applicable constraint descriptors are consumed directly from the lowered model. |
| downstream consumers | host applications, onboarding UI, MCP runtime preview patterns. |
| current reality | Public methods exist and are documented, but both bodies are stubbed. |

### 5.11 Restore — `Precept.Restore(...)`

| Item | Contract |
|---|---|
| purpose | Reconstitute persisted entity data under the current definition, validating rather than trusting storage. |
| inputs | previous artifact: `Precept`; caller-supplied persisted state and fields; lowered descriptors, slot validation, recomputation, and applicable restore constraint plans. |
| output shape | `RestoreOutcome` with `Restored(Version)`, `RestoreConstraintsFailed(IReadOnlyList<ConstraintViolation>)`, or `RestoreInvalidInput(string Reason)`. |
| metadata reflection | Uses the same lowered field/state/constraint descriptors as commit paths, but intentionally bypasses access-mode restrictions. |
| downstream consumers | persistence boundaries, migration tooling later. |
| current reality | Public DU exists; method body is a stub. |

### 5.12 Fire / InspectFire — `Version.Fire(...)`, `Version.InspectFire(...)`

| Item | Contract |
|---|---|
| purpose | Execute or preview event routing, action application, transition, recomputation, and constraint evaluation. |
| inputs | previous artifact: `Version`; event descriptors, arg descriptors, row dispatch tables, lowered action plans, recomputation index, explicit anchor-plan indexes, and fault sites. |
| output shape | `EventOutcome` (`Transitioned`, `Applied`, `Rejected`, `InvalidArgs`, `EventConstraintsFailed`, `Unmatched`, current provisional `UndefinedEvent`) and `EventInspection` / `RowInspection`. |
| metadata reflection | Constraint identity survives into `ConstraintResult` and `ConstraintViolation` through `ConstraintDescriptor`. Routing uses descriptor-backed row identity; any remaining event-name string lookup is provisional. |
| downstream consumers | host applications, MCP `precept_fire`, runtime preview, future LS preview surfaces. |
| current reality | Public outcome types are implemented; `Version` methods and evaluator bodies are stubbed. String parameters are explicit TODO placeholders pending descriptor-based D8/R4 work. |

### 5.13 Update / InspectUpdate — `Version.Update(...)`, `Version.InspectUpdate(...)`

| Item | Contract |
|---|---|
| purpose | Execute or preview direct field edits under access-mode and constraint governance. |
| inputs | previous artifact: `Version`; field descriptors, per-state access facts, recomputation dependencies, `always` / `in` constraint plans, and event-prospect evaluation over a hypothetical state. |
| output shape | `UpdateOutcome` (`FieldWriteCommitted`, `UpdateConstraintsFailed`, `AccessDenied`, `InvalidInput`) and `UpdateInspection`. |
| metadata reflection | Access modes and constraint identity come from lowered descriptors; `UpdateInspection.Events` reuses event inspection against the hypothetical post-patch state. |
| downstream consumers | application edit flows, MCP `precept_update`, runtime preview. |
| current reality | Public shapes exist; method bodies are stubbed; string-based field identity remains provisional. |

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

**Current implementation reality:** `tools\Precept.LanguageServer\Program.cs` only boots the server and waits for exit. The matrix above is therefore a contract for later implementation, not a description of current behavior.

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

## 8. Diagnostics, runtime outcomes, and faults

Precept's promise is not that runtime faults are merely classified well. It is that valid executable models do not surface in-domain runtime errors. The three result families therefore have different jobs:

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

### Commit surfaces

| Operation | Success | Domain outcome | Boundary-validation outcome | engine invariant breach (`Fault`) |
|---|---|---|---|---|
| `Create` / `Fire` | `Applied`, `Transitioned` | `Rejected`, `EventConstraintsFailed`, `Unmatched` | `InvalidArgs`, current provisional `UndefinedEvent` | `Fault` |
| `Update` | `FieldWriteCommitted` | `UpdateConstraintsFailed`, `AccessDenied` | `InvalidInput` | `Fault` |
| `Restore` | `Restored` | `RestoreConstraintsFailed` | `RestoreInvalidInput` | `Fault` |

`UndefinedEvent` and other string-selected invalid-input cases are part of the current provisional surface, not a permanent endorsement of string-lookup-era runtime identity. Descriptor-backed APIs may narrow or eliminate some of these branches.

### Inspection surfaces

| Type | Role |
|---|---|
| `EventInspection` | reduced event-level landscape |
| `RowInspection` | per-row prospect, effect, snapshots, constraints |
| `UpdateInspection` | hypothetical field state + resulting event landscape |
| `ConstraintResult` | evaluation status referencing `ConstraintDescriptor` |
| `FieldSnapshot` | resolved or unresolved field value in hypothetical state |

Inspection must share the same lowered plans as commit. It is not a second evaluator.

**Constraint exposure tiers**

Three tiers form the constraint query and inspection contract. Each tier is additive in specificity.

| Tier | Surface | Contents | When available |
|---|---|---|---|
| definition | `Precept.Constraints` | every declared `ConstraintDescriptor` in the definition | always, from the lowered model |
| applicable | `Version.ApplicableConstraints` | the zero-cost subset active for current state and context | from any live `Version` |
| evaluated | `ConstraintResult` / `ConstraintViolation` | what was actually checked during a specific operation | embedded in outcome and inspection results only |

The definition tier enumerates the full catalog. The applicable tier narrows to the live context without evaluation. The evaluated tier records actual decisions made during a specific operation execution.

## 9. Current implementation drift snapshot

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

## 10. Design assertions locked by this document

1. `CompilationResult` and `Precept` are distinct artifacts with distinct consumers.
2. `SyntaxTree` and `TypedModel` are distinct artifacts with distinct jobs.
3. The `TypedModel` anti-mirroring boundary is enforceable: symbols, bindings, normalized declarations, typed execution forms, dependency facts, and source-origin handles are mandatory inventory.
4. Graph and proof remain analysis artifacts; lowering alone builds the executable model.
5. Lowering must preserve distinct executable plan families for `always`, `in`, `to`, `from`, and `on` anchors.
6. Valid executable models report structured runtime outcomes; `Fault` is reserved for impossible-path invariant breaches.
7. The LS is a `CompilationResult` consumer first and a runtime consumer second, with exact feature-to-artifact boundaries.
8. MCP follows the same split.
9. Descriptor-backed runtime identity is the stable contract; current string placeholders are provisional only.
10. `TypedModel` is the compiler's resolved semantic contract, not the executable runtime contract. Collapsing the two makes the compiler a runtime dependency and violates the lowering boundary.
11. Consumers — LS, MCP, evaluator, and tests — must not maintain independent copies of catalog-defined vocabulary. Catalog metadata is the single source. Consumer-local kind tables or parallel keyword lists are an invariant breach and must be caught by drift tests.