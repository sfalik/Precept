# Combined Compiler + Runtime Design v2 — George source draft

> **Status:** Source draft  
> **Audience:** compiler, runtime, language-server, MCP, and design-doc authors  
> **Scope:** end-to-end contract from source text through executable runtime operations  
> **Boundary rule:** compiler and runtime are equal citizens; neither is a footnote to the other

## 1. Architectural frame

Precept has two top-level products from one `.precept` source:

1. **`CompilationResult`** — the authoring/tooling snapshot. Always produced, even on broken input.
2. **`Precept`** — the executable runtime model. Produced only from an error-free `CompilationResult`.

That split is not cosmetic. It is the contract that keeps authoring truth, executable truth, and consumer truth from collapsing into one accidental mega-model.

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
                       Precept.From(...)
                             │
               ┌─────────────┼─────────────┐
               ▼             ▼             ▼
           Create        Restore      Version operations
                                         ├─ Fire / InspectFire
                                         └─ Update / InspectUpdate
```

## 2. Canonical artifacts

| Layer | Artifact | Role |
|---|---|---|
| lexical | `TokenStream` | tokenized source + lex diagnostics |
| syntactic | `SyntaxTree` | parser-owned source structure and recovery shape |
| semantic | `TypedModel` | resolved domain meaning |
| structural analysis | `GraphResult` | state/event reachability and graph facts |
| safety proof | `ProofModel` | proof obligations, evidence, and preventable-fault links |
| authoring snapshot | `CompilationResult` | immutable whole-pipeline snapshot |
| executable model | `Precept` | lowered runtime definition |
| entity snapshot | `Version` | immutable per-instance state |
| runtime results | `EventOutcome`, `UpdateOutcome`, `RestoreOutcome`, inspection records | operation contracts |

## 3. Source-of-truth inputs

Every stage starts from two roots:

| Root input | Meaning |
|---|---|
| source text | author-owned `.precept` program |
| catalogs | machine-readable language metadata from `src\Precept\Language\` |

Catalogs must enter as early as knowable, then survive or resolve forward. Later stages may add resolved meaning; they must not re-infer catalog facts that were already known.

---

## 4. Stage-by-stage contract

### 4.1 Lexer — `Lexer.Lex(string source) -> TokenStream`

| Item | Contract |
|---|---|
| purpose | Convert source text into a flat token stream with precise spans and lex diagnostics. |
| exact inputs | raw `string source`; token vocabulary from `Tokens.All`; diagnostic metadata from `Diagnostics`; `SourceSpan` rules. |
| exact output artifact shape | **Current implemented shape:** `TokenStream(ImmutableArray<Token> Tokens, ImmutableArray<Diagnostic> Diagnostics)` where each `Token` is `Token(TokenKind Kind, string Text, SourceSpan Span)`. |
| metadata flow | Token catalog metadata enters directly through keyword/operator/punctuation lookup. `TokenKind` survives into the output. No semantic resolution happens here. |
| downstream consumers | `Parser` consumes `TokenStream.Tokens`; `Compiler` merges `TokenStream.Diagnostics`; LS lexical coloring can consume tokens directly. |
| tooling / runtime / MCP consumers | LS semantic-token seed layer; MCP language vocabulary derives from the same token catalog, not from ad hoc lexer tables. Runtime does **not** consume raw tokens. |
| current implementation vs contract | This is the only materially implemented stage today. `Lexer.cs` is already largely catalog-driven and emits real `TokenStream` content. |

### 4.2 Parser — `Parser.Parse(TokenStream tokens) -> SyntaxTree`

| Item | Contract |
|---|---|
| purpose | Build the parser-owned structural representation of the source, including recovery shape and exact authored ordering. |
| exact inputs | previous artifact: `TokenStream`; relevant catalogs: `Constructs`, `Tokens`, operator precedence/associativity metadata, diagnostics catalog. |
| exact output artifact shape | `SyntaxTree` must expand to at least `SyntaxTree(PreceptNode Root, ImmutableArray<Diagnostic> Diagnostics)`, where `PreceptNode` is never null and missing syntax is represented structurally, not by null. Child nodes carry `SourceSpan` and parser-recovery facts. |
| metadata flow | Parser resolves construct identity as far as syntax alone can know it: declaration forms, action keywords, anchors, operator forms, literal segment forms. That metadata survives as syntax-node kind identity. |
| downstream consumers | `TypeChecker` consumes `SyntaxTree.Root` plus spans/recovery shape. LS syntax-oriented features may consume the tree for outline/folding/recovery-aware completions. |
| tooling / runtime / MCP consumers | LS may use `SyntaxTree` for syntax-only UX, but semantic UX must not stop here. MCP compile/fire/update/inspect do not consume syntax directly. |
| current implementation vs contract | **Major gap:** current `SyntaxTree` is only `ImmutableArray<Diagnostic> Diagnostics`; `Parser.Parse` is a stub throwing `NotImplementedException`. |

### 4.3 Type checker — `TypeChecker.Check(SyntaxTree tree) -> TypedModel`

| Item | Contract |
|---|---|
| purpose | Resolve authored syntax into semantic meaning: symbols, resolved types, scope, typed expressions, typed actions, and normalized declarations. |
| exact inputs | previous artifact: `SyntaxTree`; relevant catalogs: `Types`, `Modifiers`, `Functions`, `Operators`, `Operations`, `Actions`, `Constructs`, `Diagnostics`, token/type metadata bridges. |
| exact output artifact shape | `TypedModel` must stop being a mirror-shaped stub and become the semantic model. At minimum it needs definition/symbol tables (`FieldSymbol`, `StateSymbol`, `EventSymbol`, `ArgSymbol`), normalized declaration arrays (`Rules`, `Ensures`, `TransitionRows`, `AccessModes`, `StateActions`, `StatelessHooks`), `InitialState`, typed expressions/actions, resolved modifiers, resolved types, and `ImmutableArray<Diagnostic> Diagnostics`. |
| metadata flow | Catalog metadata is resolved here into semantic identities: `TypeKind`, `ModifierKind`, `OperationKind`, `Function` overload choice, action kind, proof requirements attached to typed operations, etc. The output keeps semantic identity, not syntax spelling. |
| downstream consumers | `GraphAnalyzer`, `ProofEngine`, LS hover/completion/definition/semantic classification, MCP `precept_compile`, lowering. |
| tooling / runtime / MCP consumers | LS semantic features consume `TypedModel` first. MCP `precept_compile` should serialize typed structure from here, not from syntax nodes. Lowering reads the typed model, not the raw syntax tree. |
| current implementation vs contract | **Major gap:** current `TypedModel` is only `ImmutableArray<Diagnostic> Diagnostics`; `TypeChecker.Check` is a stub. |

### 4.4 Graph analyzer — `GraphAnalyzer.Analyze(TypedModel model) -> GraphResult`

| Item | Contract |
|---|---|
| purpose | Derive lifecycle graph truth from the semantic model: reachability, event coverage, dominators, required/terminal/irreversible validations, and state-edge indexing. |
| exact inputs | previous artifact: `TypedModel`; relevant catalogs: state modifier metadata, construct/action metadata that affects graph shape, diagnostics catalog. |
| exact output artifact shape | `GraphResult` must carry more than diagnostics: reachable-state set, edge inventory, successor/predecessor indexes, event coverage per state, dominator-style facts for required-state reasoning, and `ImmutableArray<Diagnostic> Diagnostics`. |
| metadata flow | Graph stage consumes semantic state/event/action metadata already resolved in `TypedModel`; it should not re-parse syntax. Its output adds structural facts keyed by semantic symbols/descriptors. |
| downstream consumers | `ProofEngine`; lowering; LS warnings for unreachable states/unhandled events; MCP compile output if graph facts are exposed. |
| tooling / runtime / MCP consumers | LS preview and runtime structural queries should eventually rely on precomputed graph facts baked into `Precept`; authoring diagnostics come from `GraphResult`. |
| current implementation vs contract | **Major gap:** current `GraphResult` is only `ImmutableArray<Diagnostic> Diagnostics`; `GraphAnalyzer.Analyze` is a stub. |

### 4.5 Proof engine — `ProofEngine.Prove(TypedModel model, GraphResult graph) -> ProofModel`

| Item | Contract |
|---|---|
| purpose | Prove or fail proof obligations for statically preventable runtime hazards, with explicit evidence and diagnostic/fault linkage. |
| exact inputs | previous artifacts: `TypedModel` and `GraphResult`; relevant catalogs: `Operations`, `Functions`, proof-requirement metadata, `Diagnostics`, `Faults`. |
| exact output artifact shape | `ProofModel` should contain at least `ImmutableArray<ProofObligation> Obligations`, proof disposition/evidence records, preventable-fault links (`DiagnosticCode` + `FaultCode`), and `ImmutableArray<Diagnostic> Diagnostics`. |
| metadata flow | This is where proof metadata declared in catalogs becomes concrete obligation records tied to semantic subjects and graph context. The proof output should preserve that link directly; runtime fault linkage is projected from here, not reinvented later. |
| downstream consumers | `CompilationResult`; lowering for `FaultSite` projection; LS proof diagnostics/explanations; MCP compile output. |
| tooling / runtime / MCP consumers | LS and MCP authoring surfaces consume proof **directly** from `ProofModel`. Runtime consumes only the lowered residue, not the full proof tree. |
| current implementation vs contract | **Major gap:** current `ProofModel` is only `ImmutableArray<Diagnostic> Diagnostics`; `ProofEngine.Prove` is a stub. |

### 4.6 Whole-pipeline snapshot — `Compiler.Compile(string source) -> CompilationResult`

| Item | Contract |
|---|---|
| purpose | Produce one immutable snapshot of the entire compiler pipeline. |
| exact inputs | raw source; all five stages above. |
| exact output artifact shape | **Implemented shape:** `CompilationResult(TokenStream Tokens, SyntaxTree SyntaxTree, TypedModel Model, GraphResult Graph, ProofModel Proof, ImmutableArray<Diagnostic> Diagnostics, bool HasErrors)`. |
| metadata flow | No new metadata is invented here; `CompilationResult` is a snapshot/aggregation boundary. |
| downstream consumers | language server, MCP `precept_compile`, runtime lowering gate. |
| tooling / runtime / MCP consumers | LS should atomically swap a single `CompilationResult`; MCP `precept_compile` should serialize from this snapshot; runtime lowering must reject `HasErrors == true`. |
| current implementation vs contract | `Compiler.Compile` is correctly wired and merges stage diagnostics, but four of the five stages are currently hollow. |

### 4.7 Lowering boundary — `Precept.From(CompilationResult compilation) -> Precept`

| Item | Contract |
|---|---|
| purpose | Lower the analysis snapshot into the executable runtime model. This is the boundary where authoring-oriented data becomes execution-oriented data. |
| exact inputs | previous artifact: error-free `CompilationResult`; relevant catalogs: none re-derived ad hoc — lowering consumes already-resolved semantic metadata from `TypedModel`, `GraphResult`, and proof residue from `ProofModel`. |
| exact output artifact shape | `Precept` is a sealed executable model owning: `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `ConstraintDescriptor`; slot layout; row dispatch tables; lowered expression/action plans; constraint buckets (`always`, state-anchor, event-anchor); and fault-site projections. |
| metadata flow | Catalog metadata should arrive here already resolved. Lowering projects semantic identity into runtime descriptors and plan tables. It must not copy syntax structure or rerun type/proof logic. |
| downstream consumers | `Precept.Create`, `Precept.InspectCreate`, `Precept.Restore`, `Version` operations, MCP runtime tools, host applications. |
| tooling / runtime / MCP consumers | Runtime-backed preview consumes `Precept`; normal authoring tooling should stay on `CompilationResult`. |
| current implementation vs contract | `Precept.From` currently only guards on `HasErrors` and then throws `NotImplementedException`. The public doc comments already point at dispatch tables, slot arrays, and constraint buckets, but none exist yet. |

### 4.8 Runtime construction path — `Precept.Create(...)` / `Precept.InspectCreate(...)`

| Item | Contract |
|---|---|
| purpose | Construct the first valid entity snapshot, optionally by atomically firing the initial event. |
| exact inputs | previous artifact: `Precept`; relevant executable metadata: field descriptors/defaults, `InitialState`, `InitialEvent`, arg descriptors, constraint plans, graph facts. |
| exact output artifact shape | `Create` returns `EventOutcome`; `InspectCreate` returns `EventInspection`. Successful commit yields a `Version` inside `Applied` or `Transitioned`. |
| metadata flow | Lowered defaults, access semantics, initial-event contract, and constraint descriptors are consumed directly through descriptors and plans. |
| downstream consumers | host applications; MCP create-adjacent preview semantics; later `Version` operations. |
| tooling / runtime / MCP consumers | Runtime preview and UI onboarding paths. |
| current implementation vs contract | Both methods are stubs. The desired public contract is already documented in `runtime-api.md`, but there is no executable implementation yet. |

### 4.9 Runtime restoration path — `Precept.Restore(...) -> RestoreOutcome`

| Item | Contract |
|---|---|
| purpose | Reconstitute persisted entity data under the **current** definition, validating rather than trusting storage. |
| exact inputs | previous artifact: `Precept`; caller-supplied state and field values; executable descriptors; constraint plans; recomputation rules. |
| exact output artifact shape | Existing sealed outcome family: `Restored(Version)`, `RestoreConstraintsFailed(IReadOnlyList<ConstraintViolation>)`, `RestoreInvalidInput(string Reason)`. |
| metadata flow | Uses descriptor-keyed validation plus lowered constraint plans; bypasses access-mode restrictions intentionally; still uses the same constraint descriptor identities. |
| downstream consumers | application persistence boundary; migration tooling later. |
| tooling / runtime / MCP consumers | runtime/API layer; MCP may eventually expose restore-like validation separately, but not as an authoring compile surface. |
| current implementation vs contract | Public shape exists; method body is a stub. |

### 4.10 Event execution path — `Version.Fire(...)` / `Version.InspectFire(...)`

| Item | Contract |
|---|---|
| purpose | Execute or preview event routing, action application, state transition, recomputation, constraint evaluation, and impossible-path fault defense. |
| exact inputs | previous artifact: `Version`; relevant executable metadata: event descriptor, arg descriptors, row tables, action plans, graph facts, constraint buckets, fault sites. |
| exact output artifact shape | Existing contracts: `EventOutcome` variants (`Transitioned`, `Applied`, `Rejected`, `InvalidArgs`, `EventConstraintsFailed`, `Unmatched`, `UndefinedEvent`) and `EventInspection`/`RowInspection` with `ConstraintResult`, `FieldSnapshot`, `Prospect`, `RowEffect`. |
| metadata flow | Fire must consume descriptor-backed runtime plans. Constraint descriptors survive into `ConstraintViolation` / `ConstraintResult`. Fault-site metadata survives only as backstop classification, not as a user-level business outcome. |
| downstream consumers | host application command path; runtime preview; MCP `precept_fire` and `precept_inspect`. |
| tooling / runtime / MCP consumers | preview webviews, agent inspection, application business flows. |
| current implementation vs contract | Public outcome types exist; `Version` methods and `Evaluator.Fire` / `Evaluator.InspectFire` are stubs. |

### 4.11 Direct update path — `Version.Update(...)` / `Version.InspectUpdate(...)`

| Item | Contract |
|---|---|
| purpose | Execute or preview direct field edits under access-mode and constraint governance. |
| exact inputs | previous artifact: `Version`; relevant executable metadata: field descriptors, per-state access facts, recomputation dependencies, applicable constraint buckets, event prospect evaluation tables. |
| exact output artifact shape | Existing contracts: `UpdateOutcome` variants (`FieldWriteCommitted`, `UpdateConstraintsFailed`, `AccessDenied`, `InvalidInput`) and `UpdateInspection`. |
| metadata flow | Access metadata and constraint metadata come from descriptors and lowered plans, not strings. `UpdateInspection.Events` reuses event inspection against a hypothetical post-patch state. |
| downstream consumers | application edit flows; MCP `precept_update` and `precept_inspect`. |
| tooling / runtime / MCP consumers | edit forms, preview surfaces, agent workflows. |
| current implementation vs contract | Public shapes exist; bodies are stubs. |

### 4.12 Shared runtime executor — `Evaluator`

| Item | Contract |
|---|---|
| purpose | Be the shared execution engine behind commit and inspect paths. |
| exact inputs | `Precept`, `Version`, descriptor-keyed args/patches, lowered action/expression/constraint/fault plans. |
| exact output artifact shape | No independent artifact; returns the operation outcome/inspection types above and uses `Fault` only for impossible-path engine failures. |
| metadata flow | Reads lowered descriptors/plans only. It must not consult syntax or raw catalogs during execution. |
| downstream consumers | `Precept` and `Version` façades. |
| tooling / runtime / MCP consumers | indirect only through runtime APIs. |
| current implementation vs contract | `Evaluator` exists only as a stub plus `Fail(FaultCode, ...)`. |

---

## 5. `SyntaxTree` vs `TypedModel`: the non-negotiable split

### `SyntaxTree` owns

- authored order
- parser recovery shape
- exact syntactic spelling
- token-to-node grouping
- span fidelity for malformed constructs

### `TypedModel` owns

- resolved symbols
- resolved types
- normalized declaration meaning
- action/operation/function identity
- scope and binding truth
- semantic dependency information

### What accidental shape mirroring looks like

Bad signs:

1. A typed node is just a syntax node with renamed properties.
2. The typed layer preserves parser-only nullability/recovery shape instead of normalizing meaning.
3. The language server reads `SyntaxTree` for hover, go-to-definition, or semantic completions because the typed model is too syntax-shaped to help.

The typed layer must feel like a semantic database, not a prettier AST.

---

## 6. Language-server consumption contract

The language server should consume artifacts by responsibility, not convenience.

| LS feature | Correct source |
|---|---|
| lexical highlighting seed | `TokenStream` + token catalog metadata |
| parse-aware outline/folding/recovery | `SyntaxTree` |
| diagnostics | merged `CompilationResult.Diagnostics` |
| semantic tokens for identifiers | `TypedModel` |
| completions | `TypedModel` first; `SyntaxTree` only for local parse context |
| hover | `TypedModel` |
| go-to-definition | `TypedModel` |
| preview / inspect | `Precept` + runtime inspection, only when `!HasErrors` |

Two hard rules:

1. **Do not force semantic features to read syntax because the typed model is underspecified.**
2. **Do not force preview/runtime features to read `CompilationResult` once lowering succeeds.**

Current implementation difference: `tools\Precept.LanguageServer\Program.cs` only boots the server; there are no handlers yet. That makes this boundary especially important now, before the wrong consumers get locked in.

---

## 7. MCP consumption contract

| MCP tool | Correct source |
|---|---|
| `precept_language` | catalogs directly |
| `precept_compile` | `CompilationResult` |
| `precept_inspect` | `Precept` + runtime inspection surface |
| `precept_fire` | `Precept` / `Version.Fire` |
| `precept_update` | `Precept` / `Version.Update` |

The MCP split must mirror the LS split: authoring tools consume compiler artifacts; execution tools consume lowered runtime artifacts.

---

## 8. Runtime lowering detail

`Precept.From(compilation)` is not a trivial wrapper. It must produce runtime-native structures:

| Runtime concern | Lowered form |
|---|---|
| identity | descriptor tables: `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `ConstraintDescriptor` |
| data storage | slot layout / field index map |
| event routing | per-state event dispatch tables |
| mutation execution | lowered action plan chain |
| expression evaluation | lowered expression tree/plan with resolved operation/function identity |
| constraints | `ConstraintPlan` buckets: always, state-anchor, event-anchor |
| proof residue | `FaultSite` / `FaultSiteDescriptor` projection only |
| structural queries | precomputed available events, field access, applicable constraints |

Compiler proof detail stays on the compilation side. Runtime gets only the executable residue it actually needs.

---

## 9. Outcome-shape contract

### Commit outcomes

| Operation | Success | Domain-invalid | Structure-invalid | impossible-path engine failure |
|---|---|---|---|---|
| `Create` / `Fire` | `Applied` / `Transitioned` | `Rejected`, `EventConstraintsFailed`, `Unmatched` | `InvalidArgs`, `UndefinedEvent` | `Fault` path |
| `Update` | `FieldWriteCommitted` | `UpdateConstraintsFailed`, `AccessDenied` | `InvalidInput` | `Fault` path |
| `Restore` | `Restored` | `RestoreConstraintsFailed` | `RestoreInvalidInput` | `Fault` path |

### Inspection outcomes

Inspection shares the same lowered plans but returns:

- `EventInspection`
- `UpdateInspection`
- `RowInspection`
- `ConstraintResult`
- `FieldSnapshot`

Inspection must never invent a parallel evaluator.

---

## 10. Current implementation drift snapshot

The proposed contract is materially ahead of the codebase today:

| Area | Current state | Required state |
|---|---|---|
| lexer | implemented | keep as lexical truth source |
| parser | stub | build real `SyntaxTree.Root` and recovery shape |
| typed model | diagnostics-only stub | become the semantic model |
| graph | diagnostics-only stub | carry real graph facts |
| proof | diagnostics-only stub | carry obligations/evidence/fault links |
| lowering | guard + stub | build descriptors and executable plans |
| runtime operations | public shapes, no bodies | full evaluator-driven behavior |
| language server | bootstrap only | consume tokens/tree/model/runtime by responsibility |

Also: the repository currently does **not** have a green build baseline. `dotnet build` presently fails with existing `PRECEPT0014b` construct-catalog ambiguity errors unrelated to this draft. That is pre-existing drift, not created by this document.

---

## 11. Design assertions this draft locks

1. `CompilationResult` and `Precept` are separate artifacts with different consumers.
2. `SyntaxTree` and `TypedModel` are separate artifacts with different jobs.
3. The typed layer must reorganize around semantic identity, not preserve syntax shape.
4. Lowering is the execution boundary; proof does not become runtime.
5. Constraint behavior and fault behavior are separate contracts.
6. The language server is a `CompilationResult` consumer first and a runtime consumer second.
7. MCP follows the same split.

If later detailed docs violate any of those seven, the architecture will drift back into accidental mirroring and parallel-model confusion.
