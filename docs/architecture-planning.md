# Precept Architecture Plan

**Date:** 2026-04-22
**Status:** Pre-design planning artifact — identifies what must be designed, in what order, grounded in what evidence
**Inputs:** Language Vision (`docs/language/precept-language-vision.md`), 15 compiler research surveys (`research/architecture/compiler/`), Runtime Evaluator Architecture Survey (`research/architecture/runtime/runtime-evaluator-architecture-survey.md`)

---

## Framing

This is a planning document, not a design document. Its job is to answer: *what design work needs to happen, in what order, and with what grounding?* The design documents themselves come after this plan is accepted.

The language vision is the authoritative specification of what the system must serve. The compiler research corpus (15 surveys covering 50+ external systems) and the runtime evaluator architecture survey (10 external systems across 8 dimensions) are the authoritative sources of precedent. All claims are grounded in one of these sources.

### What this document covers

The full path from source text to evaluation outcome. The compiler pipeline (lexer → parser → type checker → graph analyzer → proof engine) produces a `CompilationResult`. The runtime constructs an executable model from an error-free compilation result (`Precept.From(CompilationResult)`) and operates against it (evaluator, entity representation, result types, constraint evaluation, working copy atomicity, fault production, and public API surface).

### What this document intentionally excludes

- Language server integration (the LS consumes the compilation result, not the executable model)
- MCP tool shapes (MCP tools wrap runtime APIs but don't constrain their internal design)
- Persistence, serialization, or host-application integration
- The current prototype's implementation decisions

### Committed context treated as given

The two-artifact split is committed:

- **Compilation Result** — the tooling-surface artifact (diagnostics, typed model, proof, graph). Consumed by LS and MCP. Produced by the compiler on every pipeline run, including broken input.
- **Executable Model** — the runtime-surface artifact (dispatch tables, slot-indexed expression trees, scope-indexed constraints). Sealed, immutable, evaluation-ready. Constructed by the runtime (`Precept.From(CompilationResult)`) only from an error-free compilation result.

The fault-correspondence chain is committed: `FaultCode` ↔ `DiagnosticCode` via `[StaticallyPreventable]`, with analyzers PRECEPT0001 and PRECEPT0002 enforcing structural coverage.

The correctness invariant is committed: data-dependent preview queries (`Preview`, `CanFire`) MUST consume the executable model via the same evaluation path as `Fire`, guaranteeing structural semantic agreement between preview and execution.

### Provisional stubs under evaluation

The committed stubs (`Precept`, `Version`, `Fault`, `Evaluator`) represent early shapes. This plan evaluates them against survey evidence and identifies where they may need revision. The stubs are not authoritative — the survey evidence and language vision are.

---

## 1. Pipeline & Runtime Shape

### 1.1 Compiler Pipeline

The language vision establishes the execution model properties that directly determine pipeline shape: no loops, no control-flow branches, no reconverging flow, closed type vocabulary, finite state space, expression purity, no separate compilation. These are not incidental — they are the architectural constraints that make the pipeline tractable.

The compiler pipeline has **five functional stages** in dependency order. All five stages contribute to the `CompilationResult` — the tooling-surface artifact consumed by the LS and MCP. The compiler’s boundary is the `CompilationResult`. Construction of the executable model from an error-free compilation result is the runtime’s responsibility (`Precept.From(CompilationResult)`).

#### Stage 1 — Lexer

**Input:** Raw source text (UTF-8)
**Output:** Token stream with spans

Tokenizes keyword-anchored, line-oriented source. Must handle three literal families: primitive (bare), string (`"..."`), typed constants (`'...'`). Time-unit words (`days`, `hours`) are NOT keywords — they are validated content inside typed constants. The lexer produces the outer `'...'` boundary as a token and hands the interior to a later stage.

#### Stage 2 — Parser

**Input:** Token stream
**Output:** Concrete syntax tree (CST) or abstract syntax tree (AST)

Produces the structural skeleton without resolving names, types, or scopes. Typed constants are parsed as opaque quoted-content nodes. Modifiers are parsed and attached to declarations as opaque attributes. Every expression form is syntactically closed and finitely bounded — the parser never faces unbounded lookahead ambiguity.

#### Stage 3 — Type Checker / Binder

**Input:** Parse tree
**Output:** Typed, name-resolved semantic model

The most complex stage. Must: resolve all declaration identities; resolve all identifier references in expressions; enforce the closed type vocabulary and operator surface; resolve context-sensitive fractional literal types using bidirectional flow; enforce numeric lane integrity and explicit bridge requirements; enforce temporal hierarchy and mediation rules; enforce business-domain operator compatibility and unit commensurability; enforce collection semantics and accessor emptiness guards; enforce choice type membership; resolve computed field dependency graphs and detect cycles; produce typed expression trees where every node carries a resolved type.

#### Stage 4 — Graph Analyzer

**Input:** Typed semantic model
**Output:** State graph analysis results (reachability sets, dominator trees, edge classifications, modifier verdicts)

A Precept-specific stage. The vision describes eight graph reasoning capabilities: BFS/DFS reachability, dominator analysis (Lengauer-Tarjan), reverse-reachability, row-partition analysis, outcome-type analysis. Operates on the state graph as an overapproximation — all declared edges traversable regardless of guards.

#### Stage 5 — Proof Engine

**Input:** Typed semantic model + graph analysis results
**Output:** Proof model with attribution (proven ranges, proof obligations, contradictions, vacuous checks, dead guards)

Interval-based numeric reasoning over typed expression trees. Handles: divisor safety, sqrt non-negativity obligations, assignment range impossibility, contradictory rule detection, vacuous rule detection, dead guard detection, and compile-time constraint checking. Every proof result carries structured attribution. SMT solvers are excluded by language principle.

#### Stage Ordering

1. Lexer → Parser (input dependency)
2. Parser → Type Checker (structural skeleton needed)
3. Type Checker → Graph Analyzer (consumes typed model)
4. Graph Analyzer → Proof Engine (Proof Engine requires both TypedModel and GraphResult)

> **Decision (pipeline-artifacts-and-consumer-contracts.md):** GraphAnalyzer and ProofEngine run in series. ProofEngine's dead-guard attribution requires `GraphResult.ReachableStates`. At DSL scale, GraphAnalyzer runs in microseconds — no performance case for parallelism.

The compiler pipeline terminates here. Its output is the `CompilationResult`. See §3 for executable model design.

### 1.2 Two Pipeline Artifacts

The pipeline produces **two distinct artifacts** for **two distinct consumers**:

**Compilation Result** (tooling surface) — the analysis artifact. Contains: all-stage diagnostics (collected, not short-circuited), typed semantic model queryable by span, proof results, graph analysis results. Produced on every pipeline run, including broken input. Consumed by the language server (hover, completions, go-to-definition, diagnostics) and MCP tools. Follows the Roslyn pattern of an immutable snapshot with partial results on every keystroke.

**Executable Model** (runtime surface) — the execution artifact. Contains: transition dispatch table, slot-indexed expression trees, scope-indexed constraint lists, field descriptor array (including per-state access mode resolution: omit/read/write per field per state, with D3 baseline), topological action chains, graph analysis results (reachability sets, available events per state, edge classifications). Sealed, immutable, evaluation-ready. Only produced when the compilation result has no errors. Follows the CEL `Ast → Program` pattern — a compiled representation that an evaluator runs against entity data to produce outcomes.

The executable model serves two roles: (1) the evaluator consumes it to execute fire/edit operations, and (2) the `Precept` type exposes it as a definition-level query surface — all states, all events, all fields, state graph structure. Entity instances (`Version`) reference their governing `Precept` and combine its precomputed structural knowledge with their current data to provide a single inspectable surface.

The executable model is not a Roslyn-style `Compilation` (a tooling query surface). It is closer to CEL's `Program` (a compiled expression evaluated against an activation) or OPA's `PreparedEvalQuery` (compiled state held internally, safe to share across goroutines). The distinction matters: the LS never touches the executable model; the runtime never queries the compilation result for diagnostics.

**Correctness invariant:** Data-dependent preview queries on `Version` (e.g., `Preview(event)`) MUST consume the executable model via the same evaluation path as `Fire`. Semantic agreement between preview and execution is structural, not conventional.

### 1.3 Runtime Shape

The runtime has five major concerns that operate against the executable model.

**Executable model construction** — `Precept.From(CompilationResult)` lowers the analysis-oriented typed model into a runtime-optimized executable form. This is a runtime responsibility, not a compiler stage. The lowering operations (dispatch table construction, slot resolution, topological sorting, scope indexing, expression tree lowering) are internal construction logic of the `Precept` type.

**Evaluator** — the execution engine. Walks the executable model's expression trees against entity data to produce outcomes. The language vision's execution model properties (no loops, no branches, no reconverging flow, expression purity, finite state space) make tree-walking interpretation the natural strategy — confirmed by CEL's `Interpretable.Eval(Activation)` pattern operating under similar constraints.

**Entity representation** — the data envelope. Current state + current field data + reference to the governing executable model. Operations produce new entity snapshots — the input is never mutated. Survey evidence strongly favors immutable snapshots (XState `MachineSnapshot`, CEL activations, CUE `Value`, Dhall `Val`).

**Public API surface** — what callers touch. Two mutating operations (fire, edit) plus construction (compile → executable model → initial entity). Inspection is not a separate operation — `Version` is the inspectable surface. Structural queries (available events, field access modes per state) are precomputed from graph analysis baked into the executable model. Data-dependent queries (can this event fire? what would happen?) delegate to the evaluator via the same path as fire, differing only in commit behavior.

**Constraint evaluator** — collect-all evaluation of rules and ensures with structured attribution. Distinct from the transition-routing first-match evaluation.

### 1.4 Version as the Inspectable Surface

The language vision is explicit: "Inspection is not a reporting layer — it is a fundamental language operation. It must have the same depth as event execution."

**R5 decision: `Version` is the single inspectable surface.** There is no separate `Inspect()` operation. Instead, `Version` combines three kinds of access:

1. **Structural queries** — `AvailableEvents`, `FieldAccess` (accessible fields in current state with their mode: read or write — omitted fields are structurally absent from the collection), `RequiredArgs(eventName)`. These are precomputed from graph analysis results baked into the executable model during construction. They answer "what could happen from this state?" with zero evaluation cost — the executable model already knows the answer.

2. **Data-dependent queries** — `CanFire(eventName, args)`, `Preview(eventName, args)`, `PreviewEdit(fieldName, value)`. These run the evaluator against a working copy (same path as fire/edit) and discard the result. They answer "what would happen with this data?" and carry the same fidelity as the corresponding operation.

3. **Definition-level queries** — `Version.Precept` exposes the governing executable model: all states, all events, all fields, state graph structure. This is the full definition of the precept, not filtered by current state.

**Survey grounding for this shape:**

- **XState v5's pure `transition()` function.** `transition(machine, state, event)` returns `[nextSnapshot, actions]` without side effects. `getNextSnapshot()` returns just the snapshot for preview. The same evaluation code path serves both — the difference is whether actions are executed.
- **CEL's exhaustive evaluation mode.** `ExhaustiveEval` forces all branches to evaluate regardless of short-circuit, producing a complete trace. The same `Interpretable` tree is walked; only the evaluation mode flag differs.

The unification: `Preview`/`PreviewEdit` share the evaluation path with `Fire`/`Edit` up to and including constraint checking on the working copy. Operations promote the working copy on success; previews always discard it. The outcome record is identical — callers can inspect the full result without committing.

---

## 2. Compiler Components

### 2.1 Lexer

**Design questions:**
- Token type taxonomy — how many distinct token kinds, what fields?
- Typed-constant interiors — split into sub-tokens at lex time, or single opaque span?
- Interpolation boundaries — tracked at lex time?
- Trivia model — whitespace/comment preservation vs. discard (relevant for LS round-trip fidelity)

**Research grounding:** Pipeline architecture survey (Roslyn red-green tree, TypeScript scanner).

**Right-sizing:** Hand-written lexer with a flat token table. The keyword set is bounded and stable. A parser-generator adds complexity for little gain.

### 2.2 Parser

**Design questions:**
- CST (full fidelity for LS formatting/refactoring) vs. AST (semantic only)?
- Error recovery — the parser must never fatal-error on incomplete input (the normal state in the LS)
- Modifier token attachment — positional relationship without ambiguity
- Typed-constant interior representation — opaque node for later type checker interpretation

**Research grounding:** Pipeline architecture survey (Roslyn error recovery, TypeScript parser).

**Right-sizing:** Recursive descent. The grammar is small, line-oriented, keyword-anchored. Error recovery needs explicit design because incomplete input is normal in the LS.

### 2.3 Type Checker / Binder

**Design questions:**
- Symbol table shape — flat declaration namespace per file (no nesting, no modules). How are field, state, event, rule symbols organized?
- Context-sensitive literal typing — bidirectional expected-type propagation for the decimal/number narrowing. Survey evidence: Haskell constraint-based, Kotlin/Swift expected-type, Ada discriminant. For Precept's closed vocabulary (two candidates), a simpler directed expected-type pass is likely proportionate.
- Unit/dimension algebra internal representation — F#-style exponent vectors, admissibility table, or hybrid? This is one of the hardest type system design problems.
- Computed field dependency graph — DAG construction, topological sort, cycle detection
- Temporal hierarchy mediation rules — NodaTime's `Duration` vs. `Period` distinction maps directly
- Typed expression tree representation — every node carries a resolved type; this is what the proof engine consumes
- Semantic query API for LS — hover ("what type is this?"), completions ("what's valid here?"), go-to-definition

**Research grounding:** Pipeline architecture survey, context-sensitive-literal-typing survey, units-of-measure survey, temporal-type-hierarchy survey.

**Right-sizing:** Genuinely complex. The closed type vocabulary helps (no parametric polymorphism, no open hierarchies), but the surface is wide. Don't underestimate.

### 2.4 Graph Analyzer

**Design questions:**
- Internal graph representation — adjacency list? Direct edge objects? The graph is small (typically 5–20 states).
- Algorithm selection — BFS/DFS for reachability (standard), Lengauer-Tarjan for dominators (O(V+E))
- Multi-terminal dominator specification — precise path-obligation semantics must be defined before choosing the algorithm
- Graph analysis result structure for consumption by diagnostics and modifier enforcement
- (State, event) pair outcome-type verdict production

**Research grounding:** State-graph-analysis survey (SPIN, LLVM DominatorTree, XState, SCXML).

**Right-sizing:** The graph is small and static. Standard algorithms on a handful of nodes. The design complexity is in the API surface (how modifier verdicts are structured), not algorithmic.

### 2.5 Proof Engine

**Design questions:**
- Abstract value representation — closed interval `[lo, hi]` for numerics, with "unknown" state, presence handling for optional fields, finite set for choice
- Forward propagation model — single-pass walk over action chains; reassignment invalidates prior facts
- Constraint contribution model — field constraints, rules, and guards enter the proof state with attribution
- Relational reasoning scope — transitive interval narrowing only, or full constraint propagation? This MUST be pinned before implementation.
- Proof attribution data model — structured records carrying interval + contributing constraints + scope. Must be designed BEFORE the proof engine is built.
- Proof-outcome taxonomy — proved dangerous, proved safe, unresolved → diagnostic severity mapping
- Branch-conditional intervals — `if Count > 0 then Total / Count else 0` must model guard as proof fact inside each branch
- Minimum viable scope — Phase 1: single-field intervals, divisor safety, non-negative obligations, contradictory rules. Phase 2: multi-field relational constraints, dead guard detection.

**Research grounding:** Proof-engine-interval-arithmetic survey, proof-attribution-witness-design survey.

**Right-sizing:** No loops means no widening, no fixpoint, no lattice infrastructure. The proof engine is a single-pass interval propagator with attribution. But getting attribution right is genuinely hard design work.

### 2.6 Diagnostic System

**Design questions:**
- Descriptor vs. instance separation (Roslyn pattern)
- Location model — span-based, with line/column derivable
- Severity model — Error, Warning, Info/Hint (the vision's modifier system includes warning-severity rules)
- Secondary locations — for proof diagnostics, the constraints that contributed to the violation
- Constraint violation subject attribution — semantic subjects, scope kind, anchor (richer than standard location lists)
- Serialization format — for MCP, LS, batch output

**Research grounding:** Diagnostic-and-output-design survey, proof-attribution-witness-design survey.

### 2.7 Pipeline Artifacts & Consumer Contracts

This section covers the **tooling-surface artifact** (compilation result) and its consumer contracts. The **runtime-surface artifact** (executable model) is covered in §3.

**Compilation Result (tooling surface):**
- All-stage diagnostics aggregated and queryable
- Typed semantic model queryable by span (hover, completions, go-to-definition)
- Proof results for proof-hover and diagnostic attribution
- Graph analysis results for modifier diagnostics
- Produced on every pipeline run, including broken input
- LS coupling model — same codebase (Roslyn/TypeScript pattern), or published API? Same-codebase is the dominant pattern; LS-to-compiler code ratio is ~1:80 at GP scale, 1:3 to 1:10 at DSL scale.
- Incremental strategy — full recompile on each edit (likely acceptable at DSL file sizes), or incremental CST reuse?
- Cancellation — long-running proof/graph analysis must be cancellable on new keystrokes

**Cross-cutting design questions:**
- What is the relationship between the compilation result and the executable model? The compiler produces the `CompilationResult`. The runtime constructs the executable model from it (`Precept.From(CompilationResult)`). Distinct types with distinct API contracts and distinct consumers.
- Proof model placement — proof results serve both consumers: tooling (hover diagnostics) and potentially runtime (skip redundant checks based on proven ranges). Where does the proof model live, and is it shared or duplicated?
- Do preview queries consume the executable model (guaranteeing semantic agreement with `fire`) or the compilation result? The architectural position is: preview MUST consume the executable model.

**Research grounding:** Language-server-integration survey, compilation-result-type survey, compiler-result-to-runtime survey (CEL Ast→Program, OPA PreparedEvalQuery), outcome-type-taxonomy survey, dry-run-preview-inspect-api survey.

---

## 3. Executable Model

The executable model is the boundary between compiler and runtime. The compiler produces a `CompilationResult`; the runtime constructs the executable model from it via `Precept.From(CompilationResult)`. There is no intermediate stage or component — the lowering is the executable model's construction logic.

### 3.1 Construction (Lowering)

`Precept.From(CompilationResult)` lowers the analysis-oriented typed model into a runtime-optimized executable form. Concrete lowering operations:

- **Transition dispatch table** — builds `(state, event) → TransitionRow[]` index for O(1) lookup on every `fire`
- **Field slot resolution** — resolves field name references in expression trees to working-copy slot indices, eliminating string dictionary lookups at evaluation time
- **Computed field linearization** — topologically sorts the dependency graph into an ordered evaluation chain (computed once, walked on every call)
- **Constraint/rule scope indexing** — pre-buckets constraints and rules by scope: `constraintsFor[state]`, `rulesFor[event]`, `entryActionsFor[state]`, `exitActionsFor[state]`
- **Pre-resolved expression trees** — slot-read nodes with lane-resolved literals; self-contained for evaluation without a symbol table

Construction only succeeds when the compilation result has no errors. On broken input, the LS and MCP consume the compilation result directly. The executable model is never produced from a definition with diagnostics. If construction fails despite an error-free compilation result, that is a compiler bug — not a user-facing condition.

**Design questions:**
- What lowering operations are needed beyond the five identified?
- How is the transition dispatch table keyed — `(state, event)` tuple, or separate nested lookups? How are wildcard/default rows ordered?
- Is expression tree lowering a structural transformation (new node types) or an in-place annotation (slot indices added to existing nodes)?
- Does construction produce a single sealed object or a family of related immutable structures?

**Research grounding:** Compiler-result-to-runtime survey (CEL `env.Program(ast)` — lowering is the `Program` constructor; OPA `rego.PrepareForEval(ctx)` — lowering is inside preparation), dry-run-preview-inspect-api survey (XState `createMachine` — machine creation IS the lowering).

**Right-sizing:** The lowering is real but bounded. At Precept's scale, individual operations (dispatch table construction, slot assignment, topological sort) are straightforward. The design complexity is in the contract — what invariants the executable model guarantees and the evaluator assumes. If that contract is well-specified, the implementation is modest.

### 3.2 Executable Model Contract

The executable model is the sealed, immutable artifact that `Precept.From(CompilationResult)` produces and the evaluator consumes. It is NOT the entity's data — it is the definition's compiled rules. A single executable model serves all instances of the same precept definition. This mirrors CEL's architecture: one `Program` evaluated against many `Activation` bindings.

**Contents:** Transition dispatch table keyed by `(state, event)`, slot-indexed expression trees, scope-indexed constraint lists, field descriptor array, topological action chains for computed fields, entry/exit action chains per state.

**Contract — what the executable model guarantees and the evaluator assumes:**

At minimum:
- Every slot index in an expression tree refers to a valid field slot.
- Every expression tree node has a resolved type tag.
- The transition dispatch table is complete: every `(state, event)` pair present in the definition has an entry.
- Computed field evaluation order is topologically sorted — no forward dependencies.
- Constraint scope indices are complete: every applicable constraint for every state/event combination is indexed.

**Design questions:**
- What is the full type-level contract between the executable model and the evaluator? What can the evaluator assume without checking? (This is D8 and R4 — the same question from two sides.)
- How are stateless precepts represented? They lack states and transition routing but have events, hooks, editability, rules, and fields. Degenerate single-state, or separate model shape?
- How is computed field dependency ordering represented? Ordered list of slot indices, or a structured dependency graph?
- Does the executable model carry proof results? If so, the evaluator could skip proven-safe constraints — but this couples the proof model to the evaluation path.
- What metadata does the executable model carry for result attribution? Constraint violation subject attribution requires knowing which fields a rule references, which state scope an ensure belongs to, and what anchor (in/to/from) applies. This metadata must survive lowering.
- Node identity preservation through lowering — CEL's `Interpretable` tree carries AST node IDs for correlation between compile-time and evaluation-time results. If slot numbers replace expression references and source identity is dropped, LS-to-evaluation correlation breaks.
- Caching and lifetime — safe to cache per file-modification-timestamp, shareable across entity instances and concurrent operations.

**Research grounding:**
- CEL's `Program` is the closest analog — a compiled expression with pre-resolved types that an evaluator walks without re-parsing. CEL preserves AST node IDs through lowering for evaluation-time correlation.
- OPA's `PreparedEvalQuery` compiles and caches the query plan. Evaluation against different inputs reuses the plan.

**Right-sizing:** The executable model is a read-only data structure. Its design complexity is in the contract specification (what invariants it guarantees), not in the data structure implementation. At Precept's scale (5–20 states, dozens of fields, hundreds of expression nodes), the dispatch table is a small map and the expression trees are shallow.

---

## 4. Runtime Components

### 4.1 Evaluator

The evaluator walks the executable model's expression trees against entity data. The language vision's execution model properties make this a straightforward tree-walking interpreter: no loops means no iteration state, no branches means no join-point merging, expression purity means no side-effect tracking.

**Design questions:**
- Static utility vs. instance object? Survey evidence:
  - CEL: `Program.Eval(activation)` — the `Program` is the compiled model, not the evaluator. The evaluator (`Interpretable.Eval`) is stateless tree-walking.
  - XState: `transition(machine, state, event)` is a pure function. No evaluator instance.
  - Dhall: `eval(env, expr)` is a pure function. Total — always succeeds after type checking.
  - Drools: `KieSession.fireAllRules()` is deeply stateful — not applicable to Precept's pure expression model.

  Precept's per-invocation state is the working copy — intrinsically per-operation, not shared. **R1 is RESOLVED: static utility class with pure functions, aligned with the pipeline pattern (`Lexer.Lex`, `Parser.Parse`, `TypeChecker.Check`).**

- What does the evaluator return? The language vision's outcome taxonomy identifies 9 distinct outcomes. The evaluator must produce a result that structurally distinguishes all 9. (See R2.)

- How does the evaluator handle the action chain? Actions are sequenced — each action sees the state produced by all preceding actions. Linear walk, not graph traversal — confirmed by the language vision's "no reconverging flow" property.

- How does the evaluator handle computed fields? After mutations, recompute in topological order using the executable model's linearized list.

- Trust boundary with the type checker. Principle 11 (Static Completeness): "If a precept compiles without diagnostics, it does not produce type errors at runtime." Survey evidence:
  - Dhall: total correspondence — evaluator has NO error paths.
  - SPARK Ada: runtime checks proved unnecessary by static verification can be compiled out.
  - CEL: partial correspondence — type checker catches type faults, evaluator still handles value faults.

  Precept sits between Dhall and CEL: the type checker catches all type errors, but value-dependent faults (division by zero, overflow) remain as defensive paths handled via `FaultCode`.

- Evaluation strategy — tree-walking interpreter over slot-indexed expression trees produced during executable model construction. No JIT compilation.
- Collect-all vs. first-match execution modes — two separate entry points with shared expression machinery, or parameterized by mode?
- Preview as fire variant — `Preview` is `Fire` with a working copy that is always discarded. Both consume the executable model via the same evaluation path. Must share the same code.
- Presence operators — `is set`/`is not set` evaluate against optional field slots. The evaluator must distinguish "slot holds a value" from "slot is unset" without using null as the sentinel (or must define the sentinel convention clearly).
- `clear` execution — `clear` on optional fields resets to unset; on non-optional fields with defaults, resets to the declared default. The evaluator walks the executable model's field descriptor to determine the reset behavior.
- `omit` clearing on state entry — when a transition targets a state where a field is `omit`ted, the evaluator clears that field's slot on the working copy during the transition action chain. Does not apply to `no transition`.

**Research grounding:**
- CEL's tree-walking interpreter with `Interpretable.Eval(Activation)` is the primary structural reference.
- XState's pure `transition()` returning `[snapshot, actions]` is the operational reference.
- Dhall's total evaluator demonstrates what Principle 11 enables.

**Right-sizing:** Tree-walking over slot-indexed expression trees with no loops, no recursion, and finite depth. CEL's evaluator at ~5K lines (within its 30–40K total) is the scale reference.

### 4.2 Entity Representation

The entity is the data envelope: current state + current field data + reference to the governing executable model. Operations produce new entity snapshots — the input is never mutated.

**Design questions:**
- Immutable record vs. mutable-with-copy-on-write? Survey evidence overwhelmingly favors immutable snapshots (XState `MachineSnapshot`, CEL activations, CUE `Value`, Dhall `Val`).

- Slot array vs. dictionary for field storage? The executable model resolves field names to slot indices during construction, enabling array-based storage: `object?[slotCount]` instead of `Dictionary<string, object?>`. Array access is O(1) with no hashing overhead. A hybrid — slot array internally, name-based access methods on the public API — resolves both concerns.

- What does the initial entity look like? Create field array from declared defaults, set initial state, compute computed fields in topological order, validate rules and entry-ensures against the initial configuration. For fields `omit`ted in the initial state, slots are cleared to the unset sentinel.

- Does the entity carry its own state name, or just a state index? Both — internal slot index for evaluation, name for external consumption.

- Field access mode awareness. The entity must know the governing access mode for each field in its current state (precomputed in the executable model). The public API must enforce access modes: `omit` fields are structurally absent — they do not appear in `FieldAccess` and accessing them via the field indexer is a structural error (not a null). `read` fields are read-only via the update API, `write` fields are read-write.

- Stateless entities. Stateless precepts have no state field. A single type with optional state, two sibling types, or common base with specialization. XState's pattern (state machines are one `ActorLogic` implementation among several) suggests a unified interface with stateless as a degenerate case.

**Research grounding:** XState's `MachineSnapshot` is the primary structural reference. CEL's `Activation` interface informs the working-copy-as-overlay pattern.

**Right-sizing:** An immutable record with a slot array and a state discriminator. The design complexity is in the construction path and working copy interaction, not the entity type itself.

### 4.3 Result Types (Outcome Representation) ✅ RESOLVED

The runtime uses three distinct type families: commit outcomes for Fire and Update (flat, single answer), and inspection types for progressive evaluation (annotated landscape).

#### Commit outcome families

Fire and Update each return a sealed hierarchy. Faults throw `FaultException` — they are evaluator-level errors the type checker should have prevented and do not appear in the outcome hierarchy.

```csharp
// Event outcomes — returned by Fire
public abstract record EventOutcome;
public sealed record Transitioned(Version Result) : EventOutcome;
public sealed record Applied(Version Result) : EventOutcome;          // no-transition or stateless success
public sealed record Rejected(string Reason) : EventOutcome;          // authored reject row matched
public sealed record InvalidArgs(string Reason) : EventOutcome;       // arg validation failure (wrong type, unknown key)
public sealed record EventConstraintsFailed(IReadOnlyList<ConstraintViolation> Violations) : EventOutcome;
    // ^ covers all post-fire constraints: global rules, state ensures (in/to/from), and event ensures
public sealed record Unmatched() : EventOutcome;                      // all guards failed (including when precondition)
public sealed record UndefinedEvent() : EventOutcome;                 // no rows for this event in current state

// Update outcomes — returned by Update
public abstract record UpdateOutcome;
public sealed record FieldWriteCommitted(Version Result) : UpdateOutcome;
public sealed record UpdateConstraintsFailed(IReadOnlyList<ConstraintViolation> Violations) : UpdateOutcome;
public sealed record AccessDenied(string FieldName, FieldAccessMode ActualMode) : UpdateOutcome;
public sealed record InvalidInput(string Reason) : UpdateOutcome;
```

**Design decisions:**
- **Two families, not one.** Fire and Update have non-overlapping outcome sets. A unified type would force callers to handle variants that cannot occur. DDD per-command result types is the closest precedent.
- **Sealed hierarchy with exhaustive matching.** C# sealed records with pattern matching. Rust/F# evidence: exhaustive matching is the gold standard for preventing missed cases.
- **Faults throw, not return.** A fault means the type checker has a gap — it is a compiler bug, not business operation. Temporal's `NonDeterminismException` is the precedent. Faults are not in the outcome taxonomy.
- **`Applied` replaces `NoTransitionApplied`.** Covers both stateful no-transition outcomes and stateless event success. Stateless events are reachable — all EventOutcome variants except Transitioned apply.
- **`Rejected` vs `InvalidArgs`.** `Rejected` is an authored business prohibition (a `reject` row matched). `InvalidArgs` is a caller error (wrong type, unknown key). Symmetric with UpdateOutcome's `InvalidInput`. DDD's "business rejection vs. invalid input" distinction applied consistently across both families.

#### Inspection types

`InspectFire` and `InspectUpdate` return distinct inspection types. Both carry progressive certainty annotations — the evaluator runs the full pipeline as far as available data allows.

```csharp
public enum Prospect { Certain, Possible, Impossible }
public enum ConstraintStatus { Satisfied, Violated, Unresolvable }

// UpdateInspection — returned by InspectUpdate
// Shows all non-omitted fields (post-patch, with derived recomputation),
// constraint status, and full row detail for all events defined in the
// current state, evaluated against the hypothetical field state.
public sealed record UpdateInspection(
    IReadOnlyList<FieldSnapshot> Fields,
    IReadOnlyList<ConstraintResult> Constraints,
    IReadOnlyList<EventInspection> Events,            // events defined in current state only
    Version? HypotheticalResult);

// EventInspection — returned by InspectFire, also nested in UpdateInspection.Events
public sealed record EventInspection(
    string EventName,
    Prospect OverallProspect,                          // reduced from Rows: any Certain/Possible → enabled
    IReadOnlyList<ConstraintResult> EventEnsures,
    IReadOnlyList<RowInspection> Rows);                // empty = undefined event

public sealed record RowInspection(
    Prospect Prospect,
    RowEffect Effect,
    IReadOnlyList<FieldSnapshot> ResultingFields,      // ALL non-omitted in target state, post-mutation
    IReadOnlyList<ConstraintResult> Constraints,       // rules + state ensures for this row
    Version? HypotheticalResult);                      // non-null when pipeline fully resolves

// Row effects
public abstract record RowEffect;
public sealed record TransitionTo(string TargetState) : RowEffect;
public sealed record NoTransition() : RowEffect;
public sealed record Rejection(string Reason) : RowEffect;

// Shared inspection primitives
public sealed record FieldSnapshot(
    string FieldName,
    FieldAccessMode Mode,         // Read or Write in the target state
    string FieldType,
    bool IsResolved,              // false when value could not be computed (missing arg dependency)
    object? Value);               // post-mutation value; meaningful only when IsResolved = true

public sealed record ConstraintResult(
    string Description,           // the "because" text
    IReadOnlyList<string> FieldNames,  // attributed fields (empty for entity-level constraints)
    ConstraintStatus Status);     // Satisfied, Violated, or Unresolvable

public sealed record ArgInfo(
    string Name,
    string Type);                 // declared type name (string, number, boolean, etc.)
```

**Design decisions:**
- **Inspect replaces CanFire/CanUpdate.** Inspection is not a boolean predicate — it returns a landscape of annotated possibilities. `EventInspection.OverallProspect` provides the reduced answer: any `Certain` or `Possible` row → event is enabled.
- **InspectFire/InspectUpdate are separate, not a unified Inspect.** Fire and Update are mutually exclusive commit operations — field patches and event args produce a new Version through fundamentally different pipelines (row matching + transition vs. access-mode check + constraint evaluation). A unified `Inspect(fields?, event?, args?)` would force callers to disentangle which combination was evaluated and would conflate two non-overlapping input/output shapes. Two methods, each shaped for its use case.
- **Update replaces Edit.** The commit verb was renamed from `Edit` to `Update` to avoid confusion with `edit` declarations in the DSL (the per-state field access blocks). `Update` also aligns with the verb triple (`omit`/`read`/`write`) — it describes what the caller does to field values, not what the precept author declares.
- **Three-value Prospect, not four.** A fourth value (`ArgDependent`) was considered for guards that are `Possible` specifically because args are missing. Rejected: the three-value model is sufficient — the UI already knows which args are missing via `RequiredArgs` and can infer arg-absence from context. Adding a fourth value would complicate pattern matching at every consumer site for a signal that's redundant with information already on the Version surface.
- **Progressive evaluation.** The evaluator works ahead as far as data allows. With no args, guards with arg-dependent terms are `Possible`; field-only terms resolve immediately. With partial args, the evaluator executes set assignments, recomputes derived fields, and evaluates constraints against the working copy. Each constraint carries `ConstraintStatus` (Satisfied/Violated/Unresolvable) individually.
- **Prospect propagation.** First-match routing means a `Certain` row makes all subsequent rows `Impossible`. An `Impossible` row opens up the next row. The evaluator propagates certainty through the ordered row list.
- **Field patches re-evaluate event prospects.** `InspectUpdate` with a field patch returns event inspections for all events defined in the current state, evaluated against the hypothetical field state — changing a field value shifts which event buttons should be enabled.
- **All non-omitted fields in every result.** Both inspections return ALL non-omitted fields with post-evaluation values. Derived fields are recomputed. Omitted fields are absent (consistent with the omit-as-absence principle).
- **FieldSnapshot carries `IsResolved` flag.** Distinguishes between genuinely-null field values (`IsResolved = true, Value = null`) and unresolvable slots where a required arg dependency is missing (`IsResolved = false`). The evaluator propagates unresolvability transitively through computed field chains.
- **ConstraintResult carries field attribution.** `FieldNames` identifies which fields a constraint relates to, enabling per-field inline validation in the UI. Entity-level constraints (no specific field) have an empty list.
- **ArgInfo replaces string-only arg names.** `RequiredArgs` returns `IReadOnlyList<ArgInfo>` with name and type, enabling the UI to render typed input controls.
- **Always return full landscape.** Both inspection methods return the full annotated result, not a lightweight boolean or summary. At DSL scale (10–50 fields, 5–15 events) the full pipeline is sub-millisecond — premature optimization would add API surface for no measurable gain. If profiling later reveals a hot path, a lightweight mode can be added without changing the existing signature (the full inspection is strictly more information).

**Survey grounding:**
- K8s dry-run returns same type as real operation — Precept's commit operations follow this pattern.
- XState's `transition()` returns same `State` type — preview/commit return type parity. XState v5 has 4 snapshot statuses (`active`/`done`/`error`/`stopped`).
- Terraform plan's progressive resolution model — partial evaluation with annotation.
- No surveyed system has progressive annotation with per-constraint certainty. Purpose-built.

**Right-sizing:** Two commit families (7 + 4 variants = 11 leaf types) plus two inspection types with shared primitives. More outcome types than any surveyed system, but each distinction is semantically meaningful and enables exhaustive matching.

**Open evaluator-design requirements** (deferred to D8/R4 and evaluator design doc):
- Per-expression arg-dependency sets must be baked into the executable model during `Precept.From()` — a lowering-time tree walk, not a runtime check.
- Transitive unresolvability propagation through computed field chains must be specified in the working copy design.
- Guard evaluation under partial args must use Kleene three-value logic (short-circuit semantics for `and`/`or`/`not` with Unknown operands).

### 4.4 Constraint Evaluator

The language vision distinguishes two constraint evaluation modes: collect-all (rules and ensures) and first-match (transition routing). The constraint evaluator handles the collect-all surface.

**Design questions:**
- Collect-all evaluation order. All applicable constraints evaluated regardless of intermediate failures. Survey evidence: OPA's `deny[msg]` collects all violations; CUE's unification accumulates conflicts; XState's guard model is first-match (wrong model for constraints); Eiffel evaluates one assertion at a time (wrong model for collect-all).

- Constraint violation record shape. Four distinct constraint kinds with distinct attribution: event ensures (args + event scope), rules (fields + definition scope), state ensures (fields + state scope with anchor: in/to/from), and rejections (event as a whole). Each violation carries: violated constraint expression, `because` message, semantic subjects, scope kind, anchor.

- Computed field reference expansion. If rule `Total >= 100` references computed field `Total` depending on `Price` and `Quantity`, the violation should attribute to the stored fields the user can actually change. Requires dependency graph traversal or pre-computed transitive expansion in the executable model.

- Constraint scope bucketing. The executable model pre-buckets constraints by scope. The constraint evaluator must compose the correct constraint set for each operation type (e.g., fire from A to B: global rules + `from A` ensures + `to B` ensures + event ensures).

**Research grounding:** OPA's collect-all `deny[msg]`, CUE's unification accumulating `Bottom`, SPARK's per-obligation tracking, Eiffel's blame model. No surveyed system provides semantic-subject attribution with transitive expansion.

**Right-sizing:** A loop over pre-bucketed constraints with working-copy evaluation. Low algorithmic complexity; design complexity is in the violation record shape and transitive attribution.

**Three-tier constraint exposure model (decided):**

Constraints are exposed at three tiers — each backed by `ConstraintDescriptor` as the canonical identity:

| Tier | Surface | Contents | Cost |
|------|---------|----------|------|
| **1. Catalog** | `Precept.Constraints` | Every declared rule, ensure (all anchors), rejection | Precomputed |
| **2. Applicable** | `Version.ApplicableConstraints` | Constraints active for current state (global rules + `in`/`from` ensures + event ensures for available events) | Precomputed |
| **3. Evaluated** | `ConstraintResult` / `ConstraintViolation` | Per-constraint evaluation status in inspection and commit results | Evaluator |

`ConstraintDescriptor` carries: kind (`Rule`, `StateEnsureIn`/`To`/`From`, `EventEnsure`), scope target (state or event name), expression text (source as written), `because` rationale, referenced fields (with transitive expansion of computed field refs), guard presence, and source line number. Transition rejections (`reject`) are NOT a constraint kind — they are author-intentional routing outcomes (`EventOutcome.Rejected`). Created during `Precept.From()`, immutable, reference-equal across all three tiers.

`to <State> ensure` constraints are transitional — they do NOT appear in `Version.ApplicableConstraints` (Tier 2). They only surface in Tier 3 during inspect/fire when the target state is known from row matching. Tier 2 = "what must hold in my current state"; Tier 3 = "what was evaluated for this specific operation".

**Provisional (G1/G9):** `ReferencedFields` and `ConstraintViolation.FieldNames` are flat string lists. The prototype carries a typed target hierarchy (field, event-arg, event, state, definition) for rich attribution. When metadata descriptors (D8/R4) are defined, these become typed target lists.

See `docs/runtime/runtime-api.md` § Constraint Exposure Model for the full type definitions and tier 2 scope rules.

### 4.5 Working Copy / Atomicity Mechanism

The language vision's atomicity guarantee: "All mutations execute on a working copy. Constraints are evaluated against the working copy after all mutations complete. If every constraint passes, the working copy is promoted. If any constraint fails, the working copy is discarded."

**Design questions:**
- Working copy representation. Full clone of the slot array at the start of each operation. At Precept's scale (10–50 fields), `Array.Copy` is essentially free. Copy-on-write adds complexity for a problem that does not exist.

- Working copy lifecycle. Created per fire/edit/preview operation. Populated with current field values. Mutated by action chain. Evaluated against constraints. Promoted (fire/edit on success) or discarded (preview always, fire/edit on failure). Never escapes the operation boundary.

- Computed field recomputation. After action chain completes, recompute in topological order on the working copy before constraint evaluation.

- Working copy for preview. One working copy per event being previewed. Each independent, each discarded after evaluation.

- **Working copy for inspection (R2 requirement).** Inspection working copies must carry an `IsResolved` flag per slot. Unresolvable slots arise when a `set` assignment references a missing arg. Derived fields depending on unresolvable inputs are transitively unresolvable. The recomputation loop must propagate unresolvability through the computed field dependency chain. Constraints evaluated against unresolvable slots produce `ConstraintStatus.Unresolvable` rather than spurious violations.

**Research grounding:** CEL's hierarchical `Activation` (overlay-based variable resolution), OPA's store transactions (snapshot isolation), XState's pure `transition()` (compute next state without modifying current).

**Right-sizing:** Full slot-array clone is the right answer at Precept's scale. The design work is in specifying the lifecycle and interaction with computed field recomputation.

### 4.6 Fault Production

The committed fault-correspondence chain: every `FaultCode` has `[StaticallyPreventable(DiagnosticCode.X)]`. PRECEPT0001 ensures every `Fail()` call takes a `FaultCode`. PRECEPT0002 ensures every `FaultCode` has `[StaticallyPreventable]`.

**Design questions:**
- Concrete `FaultCode` values. Division by zero, overflow, empty-collection accessor, null dereference. Each linked to the `DiagnosticCode` that should have prevented it.

- What happens when a fault IS produced despite a clean compile? Three options:
  - **Throw** (assertion failure — this is a compiler bug). SPARK's pattern; Temporal's `NonDeterminismException`.
  - **Return as fault outcome** (graceful degradation). CEL's error-as-value pattern.
  - **Both** (record the gap + return structured result).
  
  The language vision's Principle 10 (Totality): "Every expression evaluates to a result or a definite error." The word "definite" suggests errors are structured results.

- Fault attribution. Should `Fault` carry expression location, field attribution, and the `[StaticallyPreventable]` link as runtime data?

- Fault vs. constraint violation — distinct categories in the result type.

**Research grounding:** SPARK's proof-elimination, Dhall's totality, CEL's error-as-value, Temporal's `NonDeterminismException`.

**Right-sizing:** Small fault space (~5–10 `FaultCode` members). Design complexity is in the correspondence enforcement (committed) and the fault-vs-violation separation in results.

### 4.7 Public API Surface

The language vision specifies two mutating operations, a construction path, and an inspectable surface.

**Precept carries construction and definition-level queries:**

```csharp
public sealed class Precept
{
    public static Precept From(CompilationResult compilation);

    // Entity creation — mirrors Version's commit/inspect pattern
    public EventOutcome Create(IReadOnlyDictionary<string, object?>? args = null);
    public EventInspection InspectCreate(IReadOnlyDictionary<string, object?>? args = null);

    // Entity restoration — validated reconstruction from persisted data
    public RestoreOutcome Restore(string? state, IReadOnlyDictionary<string, object?> fields);

    // Construction metadata
    public string? InitialEvent { get; }             // null = no initial event

    // Definition-level queries
    public IReadOnlyList<string> States { get; }
    public IReadOnlyList<string> Fields { get; }
    public IReadOnlyList<string> Events { get; }
    public string? InitialState { get; }
    public bool IsStateless { get; }

    // Constraint catalog (Tier 1)
    public IReadOnlyList<ConstraintDescriptor> Constraints { get; }
}
```

`Create` is entity construction. If the precept declares an `initial` event, `Create` fires it atomically: build hollow version (defaults + initial state + omitted fields) → fire initial event with args → return `EventOutcome`. If no initial event, constructs from defaults and returns `Applied`. `InspectCreate` provides the same progressive inspection model as `InspectFire`. The compiler enforces `RequiredFieldsNeedInitialEvent`/`InitialEventMissingAssignments` to guarantee construction coherence.

**Version carries four methods — two commit, two inspect:**

```csharp
public sealed record Version
{
    public Precept Precept { get; }
    public string? State { get; }                                // null for stateless
    public object? this[string fieldName] { get; }               // throws on omitted field
    public IReadOnlyList<FieldAccessInfo> FieldAccess { get; }   // omit = absent
    public IReadOnlyList<string> AvailableEvents { get; }
    public IReadOnlyList<ArgInfo> RequiredArgs(string eventName);

    // Commit — require complete input, return one outcome, mutate state
    EventOutcome    Fire(string eventName, IReadOnlyDictionary<string, object?> args);
    UpdateOutcome   Update(IReadOnlyDictionary<string, object?> fields);

    // Inspect — input optional, return annotated landscape, no mutation
    EventInspection   InspectFire(string eventName, IReadOnlyDictionary<string, object?>? args = null);
    UpdateInspection  InspectUpdate(IReadOnlyDictionary<string, object?>? fields = null);
}
```

1. **Create** — construct the initial entity. If an initial event is declared, fires it with args through the full pipeline (guards, mutations, ensures, constraints). Returns `EventOutcome`. All 7 variants valid — including `Transitioned` (construction-time routing), `Rejected` (business rejection at intake). `UndefinedEvent` cannot occur (compiler guarantee).
2. **InspectCreate** — progressive inspection of construction. Same model as `InspectFire`.
3. **Restore** — validated reconstruction from persisted data. Accepts state + all fields, bypasses access mode checks, recomputes computed fields, evaluates constraints. Returns `RestoreOutcome`: `Restored(Version)` on success, `RestoreConstraintsFailed(Violations)` or `RestoreInvalidInput(Reason)` on failure. Restoring an entity in an invalid state is not allowed — if the definition changed, the correct response is migration (future), not silent acceptance.
4. **Fire** — send an event to an entity, producing a new version or an outcome explaining why not.
5. **Update** — directly mutate `write`-accessible fields, producing a new version or an outcome explaining why not. Input is a dictionary of field names to values.
6. **InspectFire** — progressive inspection of an event. Returns all rows with certainty annotations, field snapshots per row, and constraint results. Optional args refine the landscape from Possible toward Certain.
7. **InspectUpdate** — progressive inspection of a field edit. Returns all non-omitted fields with post-patch values, constraint results, and full event inspection for ALL events evaluated against the hypothetical field state. Optional field patch refines the landscape.
8. **Structural queries** — `AvailableEvents`, `FieldAccess`, `RequiredArgs` are precomputed from graph analysis. Definition-level queries go through `Version.Precept`.

Instance methods on `Version` are the convenience API. The underlying implementation is the shared static evaluation function (per R1/R5). See §4.3 for the full outcome and inspection type definitions.

**Construction-time constraint composition:** When an initial event fires during `Create`, constraints compose in this order: (1) arg ensures on the initial event — pre-assignment validation, (2) field constraints (rules/ensures) — post-assignment, (3) global rules — always, (4) `to <InitialState> ensure` — construction-specific entry truth, (5) `in <InitialState> ensure` — while-in-state truth. No new language surface is needed: `to <InitialState> ensure` naturally serves as construction-time rules.

---

## 5. Key Design Decisions

Fifteen decisions that must be resolved before implementation. D1–D8 cover compiler concerns; R1–R8 cover runtime concerns. D8 and R4 are two halves of the same specification — the executable model contract.

### Compiler Decisions

#### D1 — Pipeline artifact split

**Question:** What are the two pipeline artifacts (compilation result and executable model), what is in each, and how are they produced and related?
**Survey grounding:** Compilation-result-type survey (Roslyn for the tooling surface), compiler-result-to-runtime survey (CEL Ast→Program for the runtime surface).
**Coupling:** Couples to D7 (LS strategy), D4 (diagnostic aggregation), and D8 (executable model contract). Resolve first.

#### D2 — Literal resolution mechanism

**Question:** How do fractional literals acquire their lane from context — expected-type propagation (Kotlin/Swift), constraint solver (Haskell), or default-to-decimal with explicit override?
**Survey grounding:** Context-sensitive-literal-typing survey.
**Coupling:** Couples to D3 (unit algebra) and the typed expression tree design.

#### D3 — Unit/dimension algebra representation

**Question:** F#-style exponent vectors, admissibility table, or hybrid?
**Survey grounding:** Units-of-measure survey.
**Coupling:** One of the hardest design problems. Couples to D2 and temporal type hierarchy.

#### D4 — Diagnostic attribution structure

**Question:** How do diagnostics carry constraint violation subject attribution?
**Survey grounding:** Diagnostic-and-output-design survey, proof-attribution survey.
**Coupling:** Couples to D1 and D5.

#### D5 — Proof attribution schema

**Question:** What is the structure of a proof result — flat list per field, expression-tree-mirroring tree, or separate queryable model?
**Survey grounding:** Proof-attribution survey, proof-engine-interval-arithmetic survey.
**Coupling:** Couples to D4 and D1. Must be designed before the proof engine is built.

#### D6 — LS incremental strategy

**Question:** Full recompile (likely acceptable at DSL file sizes), incremental CST (Roslyn red-green trees), or stage-level caching?
**Survey grounding:** Language-server-integration survey.
**Coupling:** Affects CST/AST design in the parser.

#### D7 — LS coupling model

**Question:** Same codebase (Roslyn/TypeScript), published API, or separate process?
**Survey grounding:** Language-server-integration survey. Same-codebase is the dominant pattern. LS-to-compiler code ratio at DSL scale is 1:3 to 1:10.
**Coupling:** Couples to D1 and D6.

#### D8 — Executable model contract (= R4)

**Question:** What structural invariants does the executable model guarantee that the evaluator can rely on unconditionally? What is the lowering boundary — which transformations happen during construction vs. lazily in the evaluator?
**Survey grounding:** Compiler-result-to-runtime survey (CEL Program internals, OPA PreparedEvalQuery).
**Coupling:** Couples to D1 (the compilation result is the construction input) and blocks evaluator design. This is the same specification as R4 — co-designed from both sides.

**R2-derived requirement (binding):** The executable model must carry per-expression arg-dependency sets — an annotation on each expression node indicating which event arguments appear in its subtree. This is computed as a lowering-time tree walk during `Precept.From()`. Without it, the evaluator cannot distinguish field-only guards from arg-dependent guards for progressive inspection. See `docs/runtime/result-types.md` § Evaluator Design Requirements.

**Metadata-first requirement (binding):** D8/R4 must define typed metadata descriptors (field descriptor, event descriptor, state descriptor, arg descriptor) that replace raw strings throughout the runtime API. The evaluator operates against descriptors, not strings. Descriptors carry the full compiled metadata — name, type, slot index, access modes, constraints, arg-dependency sets — and are the canonical identity for every declared element. All string placeholders in the current `Precept.Next/Runtime/` stubs are provisional gaps pending this decision.

### Runtime Decisions

#### R1 — Evaluator shape ✅ RESOLVED

**Decision:** Static utility class with pure functions. No evaluator instance. Working copy is per-operation, allocated inside the evaluation function. Aligned with the pipeline pattern (`Lexer.Lex`, `Parser.Parse`, `TypeChecker.Check`).

**Survey grounding:** CEL's `Interpretable.Eval(Activation)`, XState's `transition(machine, state, event)`, Dhall's `eval(env, expr)` — all stateless/functional for pure-expression languages.

**Dependencies:** Blocks R3 (entity representation must know who manages the working copy). R5 resolved — Version delegates to Evaluator.

#### R2 — Result type taxonomy ✅ RESOLVED

**Decision:** Two commit outcome families (EventOutcome: 7 variants, UpdateOutcome: 4 variants) plus two inspection types (EventInspection, UpdateInspection) with progressive certainty annotations. Faults throw FaultException — not in the outcome hierarchy. See §4.3 for the full type definitions.
**Survey grounding:** CEL's three-value return, XState's 4 snapshot statuses, K8s dry-run same-type return, Terraform progressive resolution. No surveyed system has 11 categories — purpose-built.
**Coupling:** Couples to R1 ✅ (evaluator's return type) and R5 ✅ (inspect methods on Version). Couples to R6 (violation records as payload).

#### R3 — Entity representation

**Question:** Immutable record, working copy mechanism, slot array vs. dictionary. What is the shape of the entity snapshot?
**Survey grounding:** XState `MachineSnapshot`, CEL `Activation`, CUE `Value` — all immutable. Slot array vs. dictionary is driven by the executable model's slot resolution.
**Coupling:** Couples to R1, R4 (slot layout), and working copy design.

#### R4 — Executable model contract (= D8)

**Question:** What invariants does the executable model guarantee? What can the evaluator assume without checking?
**Survey grounding:** CEL's `Program` (trusted unconditionally), OPA's `PreparedEvalQuery`.
**Coupling:** D8 and R4 are two halves of the same specification. Blocks R1 and R5.

#### R5 — Version as inspectable surface ✅ RESOLVED

**Question:** How do callers inspect the current state of an entity without committing changes?
**Decision:** `Version` is the single inspectable surface with four methods: `Fire`, `Update` (commit), and `InspectFire`, `InspectUpdate` (progressive inspection). Inspection runs the full evaluation pipeline as far as available data allows, annotating each row and constraint with `Prospect` (Certain/Possible/Impossible) and `ConstraintResult` (passes/fails/unresolvable). Three access tiers: (1) structural queries (`AvailableEvents`, `FieldAccess`, `RequiredArgs`) precomputed from graph analysis — free; (2) progressive inspection (`InspectFire`, `InspectUpdate`) delegates to the evaluator via the same pipeline as fire/update — same fidelity, working copy discarded, returns annotated landscape; (3) definition-level queries via `Version.Precept` — all states, events, fields, graph structure.
**Survey grounding:** XState's `transition()`/`getNextSnapshot()` pair. CEL's `ExhaustiveEval`. Terraform plan's progressive resolution model.
**Dependencies:** Depends on R1 ✅ and R2 ✅. Depends on R4.

#### R6 — Constraint evaluation: collect-all attribution model

**Question:** How are constraint violations collected, and what is the shape of a violation record?
**Survey grounding:** OPA's `deny[msg]`, CUE's `Bottom`, SPARK's per-obligation tracking, Eiffel's blame model. Semantic-subject attribution with transitive expansion has zero precedent.
**Coupling:** Couples to R2 (violation records as outcome payload) and R4 (executable model metadata for attribution).

#### R7 — Fault correspondence runtime: defensive redundancy behavior

**Question:** What does the runtime DO when a statically-preventable fault fires despite a clean compile?
**Survey grounding:** SPARK (proved = eliminated), Dhall (total — no faults possible), CEL (errors are values), Temporal (throw `NonDeterminismException`).
**Coupling:** Couples to R2 (faults in result taxonomy) and committed `Fault` type shape.

#### R8 — Entity construction model ✅ RESOLVED

**Question:** How are entities created? `CreateInitialVersion()` was parameterless — how do callers pass initial values when the precept has required fields without defaults?
**Decision:** Construction is modeled as an **initial event** that fires atomically during `Create`:

1. `Precept.Create(args?)` returns `EventOutcome`. Internally: build hollow version (defaults + initial state + omitted fields) → if initial event declared, fire it with args → return outcome. If no initial event, evaluate computed fields + run rules/ensures → return `Applied`.
2. `Precept.InspectCreate(args?)` returns `EventInspection` — same progressive inspection as `InspectFire`.
3. `Precept.InitialEvent` — the initial event name, or `null`.
4. DSL modifier: `event Create initial` (reuses existing `initial` keyword — no new syntax).
5. Compiler enforcement: **`RequiredFieldsNeedInitialEvent`** (required fields without defaults require an initial event) and **`InitialEventMissingAssignments`** (initial event must assign all required fields lacking defaults).
6. Construction-time constraints compose naturally: arg ensures → field constraints → global rules → `to <InitialState> ensure` → `in <InitialState> ensure`. No new language surface.
7. All 7 `EventOutcome` variants are valid at construction — including `Transitioned` (construction-time routing), `Rejected` (business rejection at intake). `UndefinedEvent` cannot occur (compiler guarantee).

**Alternatives rejected:**
- Parameterless `CreateInitialVersion()` — forced authors to either use nonsense defaults or make things optional that shouldn't be optional. No way to enforce business invariants at intake.
- Separate construction API with its own result type — duplicated the event pipeline. Construction goes through the same guards, mutations, ensures, and constraints as any other event.
- Builder pattern (fluent `.WithField().WithField().Build()`) — lacks the atomic guarantee and doesn't compose with the constraint system.

**Survey grounding:** C# constructor parallel (constructor fires first, establishes invariants), OPA's input binding at eval time. The initial-event-as-constructor pattern is purpose-built for Precept's pipeline model.

#### R9 — Entity restoration model ✅ RESOLVED

**Question:** How are entities reconstituted from persisted data? Is restoration trust-based (skip validation) or validated (run constraints)?
**Decision:** Restoration is **validated reconstruction** — restoring an entity in an invalid state is not allowed.

1. `Precept.Restore(state, fields)` returns `RestoreOutcome`: `Restored(Version)`, `RestoreConstraintsFailed(Violations)`, or `RestoreInvalidInput(Reason)`.
2. Restore is a distinct Evaluator entry point — not a mode of Fire or Update. No event matching, no mutations from rows, no access mode checks. State is set directly from caller input. All stored fields are accepted regardless of the restored state's write/read/omit declarations.
3. Pipeline: build working copy with state + all fields → recompute computed fields → evaluate constraints (global rules + `in <State> ensure`) → return outcome.
4. Future migration logic runs before the validation pipeline — transforming persisted data to conform to the current definition before constraints are evaluated.

**Alternatives rejected:**
- Trust-based hydration (skip validation) — if the definition changed, entities could exist in states that violate current constraints. Makes the governance guarantee hollow. Refusing to load is better than silently accepting invalid data.
- Running the full event pipeline (Fire-without-event) — no event to match, no row to select, no mutations to apply. Restore is "here's complete data, validate it" — a distinct entry point.
- Separate `Validate()` on Version (load then check) — splits the trust boundary across two calls. The caller could use the Version before checking. Validated restoration is atomic: you either get a valid Version or a diagnostic.

---

## 6. Design Work Sequencing

### Design Document Map

Fifteen design documents need to be written, organized across three locations by ownership. The executable model contract is the shared boundary document.

| Phase | Document | Path | Decisions | Primary Survey References |
|-------|----------|------|-----------|--------------------------|
| 1 | Pipeline Artifacts & Consumer Contracts | `docs/compiler/pipeline-artifacts.md` | D1, D6, D7 | compilation-result-type-survey, language-server-integration-survey, compiler-result-to-runtime-survey |
| 1 | Diagnostic System | `docs/compiler/diagnostic-system.md` | D4 | diagnostic-and-output-design-survey, proof-attribution-witness-design-survey |
| 1 | Executable Model Contract | `docs/executable-model.md` | D8, R4 | CEL Program internals, OPA PreparedEvalQuery, compiler-result-to-runtime-survey |
| 1 | Result Type Taxonomy | `docs/runtime/result-types.md` | R2 | CEL three-value return, XState MachineSnapshot, CUE Value/Bottom |
| 1* | Literal System | `docs/compiler/literal-system.md` | Lexer decision 3 | PR #114 prototype (`docs/LiteralSystemDesign.md`), language vision § Literal System |
| 2 | Literal Resolution Mechanism | `docs/compiler/literal-resolution.md` | D2 | context-sensitive-literal-typing-survey |
| 2 | Unit/Dimension Algebra | `docs/compiler/unit-algebra.md` | D3 | units-of-measure-dimensional-analysis-survey, temporal-type-hierarchy-survey |
| 2 | Numeric Lane System | `docs/compiler/numeric-lanes.md` | — | language vision § Numeric lane system, issue #115, PR #116 |
| 2 | Temporal Type System | `docs/compiler/temporal-types.md` | — | issue #107 (PR #114 prototype), language vision § Temporal types |
| 2 | Quantity / UOM / Money Types | `docs/compiler/quantity-types.md` | — | issue #95, issue #107 § Forward Design, language vision § Type System |
| 3 | Lexer | `docs/compiler/lexer.md` | — | compiler-pipeline-architecture-survey |
| 3 | Parser | `docs/compiler/parser.md` | D6 applied | compiler-pipeline-architecture-survey |
| 3 | Type Checker | `docs/compiler/type-checker.md` | D2, D3 applied | All Phase 2 documents |
| 3 | Graph Analyzer | `docs/compiler/graph-analyzer.md` | — | state-graph-analysis-survey |
| 4 | Proof Engine | `docs/compiler/proof-engine.md` | D5 | proof-engine-interval-arithmetic-survey, proof-attribution-witness-design-survey |
| 4 | Runtime API | `docs/runtime/runtime-api.md` | R2 applied, R3 | XState MachineSnapshot, CEL Activation |
| 4 | Evaluator | `docs/runtime/evaluator.md` | R1, R5 | compiler-result-to-runtime-survey, exact-decimal-arithmetic-survey, dry-run-preview-inspect-api-survey |
| 4 | Constraint Evaluation | `docs/runtime/constraint-evaluation.md` | R6 | OPA deny[msg], CUE Bottom, SPARK proof output, Eiffel blame model |
| 4 | Fault System | `docs/runtime/fault-system.md` (exists) | R7 | SPARK proof-elimination, Dhall totality, CEL error-as-value |

### Phase 1 — Architecture Foundations (blocks everything)

| Document | Decisions Answered |
|----------|-------------------|
| **Pipeline Artifacts & Consumer Contracts** | D1, D6, D7 |
| **Diagnostic System** | D4 |
| **Executable Model Contract** | D8/R4 |
| **Result Type Taxonomy** | R2 |

Phase 1 documents can be written in parallel with each other. They must be resolved first because every compiler component depends on the artifact shapes (D1) and diagnostic structure (D4), and every runtime component depends on what the executable model provides (D8/R4) and what the evaluator returns (R2).

### Phase 2 — Type System Core (blocks type checker)

| Document | Decisions Answered |
|----------|-------------------|
| **Literal Resolution Mechanism** | D2 |
| **Unit/Dimension Algebra** | D3 |
| **Numeric Lane System** | — (type checker contracts for integer/decimal/number) |
| **Temporal Type System** | — (type checker contracts for 8 NodaTime-backed types) |
| **Quantity / UOM / Money Types** | — (type checker contracts for currency, quantity, price) |

Phase 2 documents can be written in parallel with each other. Can overlap with Phase 1 — literal resolution and unit algebra are independent of result/diagnostic shape decisions. The three type system docs (numeric lanes, temporal, quantity) are clean room designs organized by the contracts the type checker needs — they will be written when the parser is complete and type checker design begins, using prototype designs on PR #114 and issues #95/#107 as reference.

### Phase 3 — Pipeline Component Designs (parallelizable)

| Document | Dependencies |
|----------|-------------|
| **Lexer** | Phase 1 |
| **Parser** | Phase 1, D6 |
| **Type Checker** | Phase 1, Phase 2 |
| **Graph Analyzer** | Phase 1 |

All Phase 3 documents can be written in parallel once Phase 1 and 2 are complete.

### Phase 4 — Proof & Runtime Design

| Document | Dependencies |
|----------|-------------|
| **Proof Engine** | Phase 3 (type checker) + D5 |
| **Runtime API** | Phase 1 (R2, D8/R4) |
| **Evaluator** | Phase 1 (R1 resolved, R2, D8/R4) |
| **Constraint Evaluation** | Phase 1 (R2, D8/R4) |
| **Fault System** | Phase 1 (R2, D8/R4) |

All Phase 4 runtime documents can proceed in parallel once Phase 1 is complete — they don't depend on compiler Phases 2-3. The executable model contract (D8/R4, Phase 1) defines the boundary; the lowering implementation is internal to `Precept.From()` and documented in the Evaluator design alongside the evaluation logic it serves. The Proof Engine depends on Phase 3 (type checker output) + D5 (proof attribution schema).

### Unified Dependency Graph

```
Phase 1: D1+D4+D6+D7          D8/R4                    R2
         (Pipeline             (Executable Model        (Result Type
          foundations)          Contract)                 Taxonomy)
              ↓                     ↓                       ↓
Phase 2: D2+D3                      ↓                       ↓
         (Type system)              ↓                       ↓
              ↓                     ↓                       ↓
Phase 3: Lexer, Parser,            ↓                       ↓
         Type Checker,              ↓                       ↓
         Graph Analyzer             ↓                       ↓
              ↓                     ↓                       ↓
Phase 4: ┌─ Proof Engine            │           ┌─ Runtime API ←───┤
         │                          │           ├─ Evaluator ←─────┤
         │                          │           ├─ Constraints ←───┤
         │                          │           └─ Fault System ←──┘
         │                          │
         └─ Proof depends on        └── Runtime depends on
            Phase 3 + D5               D8/R4 + R2
```

Phases 2 and 3 are compiler-only. Phase 4 runtime documents can start as soon as Phase 1 completes — they don't depend on compiler Phases 2-3. The lowering logic (dispatch table construction, slot resolution, etc.) is internal to `Precept.From()` and covered in the Evaluator design doc alongside the evaluation logic it serves.

---

## 7. Right-Sizing Principles

### Scale Reference Points from the Research

| System | Component | Scale | Relevance |
|--------|-----------|-------|-----------|
| CEL (cel-go) | Full pipeline (parse+check+program+eval) | 30–40K lines Go | Most relevant overall scale reference — DSL with similar pipeline shape |
| OPA/Rego | Full compiler + evaluator | ~200K lines Go | Upper bound — Rego has recursion, set comprehensions, Datalog semantics |
| TypeScript | Type checker alone (checker.ts) | ~50K lines | Scale warning, not a target — parametric generics, structural subtyping, conditional types |
| Roslyn | Full compiler | ~4–5M lines C# | Scale warning — entirely different magnitude |
| NodaTime | Temporal type hierarchy | 15 primary types, ~40 operations | Direct scale reference for Precept's 8 temporal types |
| F# UoM | Unit system extension | Contained type checker extension | Direct reference for unit algebra |
| Dhall LS | Language server for DSL | ~3K lines (1:10 to core) | DSL-scale LS reference |
| Regal/OPA LS | Language server for OPA | ~25K lines (1:8 to core) | DSL-scale LS reference |

The DSL-scale systems (CEL, OPA, Dhall, Jsonnet) consistently show: 30K–200K lines total, full recompile per edit, LS-to-compiler ratio of 1:3 to 1:10. **Precept anchors to the DSL-scale pattern.**

### Where Precept's constraints genuinely simplify

**Proof engine.** No loops → no widening, no fixpoint, no lattice infrastructure, no external solver. Single-pass interval propagator. SPARK's Interval pass and CBMC's "most precise on loop-free, bounded programs" confirm.

**State graph.** Typically 5–20 states. BFS/DFS with a simple visited set.

**Parser.** No operator precedence puzzles, no significant whitespace, no forward references across files. Recursive descent confirmed.

**Separate compilation.** None. One file, full compilation, every time.

**Incremental recompilation.** Files are small. Pipeline is fast. CEL, OPA, Dhall, and Jsonnet all do full recompile per edit. Cancellation token threading is the first defense if analysis grows slow.

**Evaluator.** Tree-walking over slot-indexed expression trees with no loops, no recursion, and finite depth. CEL's evaluator at ~5K lines is the scale reference.

### Where constraints do NOT simplify

**Unit/dimension algebra.** Compound cancellation requires genuine type-level algebra. F# UoM coupling with proof engine has no surveyed precedent. Error message quality for unit mismatches is poor across all surveyed systems.

**Proof attribution.** SPARK ended up with THREE separate output formats. Precept's single schema for hover + MCP + diagnostics with semantic subjects exceeds any surveyed system. Purpose-built.

**Parser error recovery.** Per-construct recovery strategies, not one design decision. If recovery design proves difficult, tree-sitter is a known fallback path.

**Branch-conditional interval narrowing.** The proof engine must split abstract state at branch conditions, propagate independently, and rejoin. This is a real algorithm, not a footnote.

**Diagnostic attribution.** Constraint violation subject attribution is richer than standard systems provide.

**Modifier system.** Novel language feature with no survey precedent for admission logic.

**Node identity preservation through lowering.** Node identity must survive the `Precept.From()` lowering for LS-to-evaluation correlation. No surveyed system formally specifies this invariant.

**Result type taxonomy.** 9 outcomes exceeds all surveyed systems. Purpose-built.

**Constraint attribution with transitive expansion.** Zero precedent in any surveyed system.

### The right-sizing principle

**Solve the problem the language vision actually specifies, grounded in survey precedent, at a scale proportionate to the definition surface.** The language is small, the files are small, the pipeline does not generate code. CEL at 30–40K lines is the scale reference; not Roslyn at 4M.

### Areas with no survey resolution

| Area | Closest analogy | Gap |
|------|----------------|-----|
| Modifier admission logic | Reachability/dominator algorithms | Semantics of "satisfied" are Precept-specific |
| Multi-terminal dominator path obligations | LLVM DominatorTree (single root) | No surveyed system provides multi-terminal path obligation as compiler API |
| Proof engine + unit type coupling | F# UoM (erases before codegen), SPARK (no units) | No system retains unit info through proof-level reasoning |
| Business-domain semantic operators | Dimensional analysis cancellation | Operators beyond standard arithmetic are Precept-specific |
| `inspect` via executable model | CEL EvalState, OPA partial evaluation | No system uses the executable model for per-event preview |
| Unit error diagnostic UX | F#, Boost.Units, Haskell `units` (all poor) | No good model to adopt |

### Where DSL-scale and GP-scale diverge

| Dimension | DSL-scale (CEL, OPA, XState) | GP-compiler (Roslyn, TypeScript, rustc) |
|-----------|------------------------------|----------------------------------------|
| Pipeline | Sequential, explicit stages | Query-driven or demand-driven |
| Artifact | Single compiled object | Complex compilation graph with incremental reuse |
| LS coupling | Full recompile, ratio 1:3 to 1:10 | Incremental, snapshot-based, ratio 1:7 to 1:80 |
| Error recovery | Collect model, limited heuristics | Full recovery, missing tokens, error propagation |
| Proof/analysis | Post-hoc or none | N/A |
| Scale | 30K–200K lines | 300K–5M lines |

---

## 8. Risks and Hard Problems

### R1: Unit cancellation arithmetic in the type checker
Compound unit types require embedding unit dimension vectors in the type representation. F# UoM coupling with proof engine has no surveyed precedent. Error message quality is poor across all surveyed systems. **Hardest single type-checker design problem.**

### R2: Relational reasoning scope ambiguity
The vision mentions relational reasoning but does not define its scope. Full two-variable constraint solving expands into constraint programming. Transitive interval narrowing leaves many proofs "unresolved." **Must be pinned to a precise definition before proof engine implementation.**

### R3: Typed constant narrowing failure mode
The typed constant literal system may produce ambiguous context. Every surveyed system with literal type inference designed an explicit failure diagnostic. **Design the narrowing algorithm and its failure mode explicitly.**

### R4: Proof attribution is a blocking dependency
Every surveyed proof system designed its attribution schema before implementing the proof engine. **Design the attribution data type first — it constrains what the engine must produce.**

### R5: Multi-terminal dominator semantics
All surveyed dominator algorithms assume single-entry/single-exit. **Get a mathematical specification of path-obligation semantics approved before implementing dominator analysis.**

### R6: Decimal division vs. totality
`System.Decimal` silently rounds non-terminating division. Java BigDecimal forces explicit rounding. **Three options: prohibit division in decimal lane, require explicit rounding declaration, or accept silent approximation. Language-level decision required.**

### R7: LS responsiveness under proof load
DSL-scale systems doing full recompile are NOT doing proof-level analysis. **Cancellation token threading is the first defense.**

### R8: Lowering semantic drift
If the LS consumes the typed model while the runtime evaluates the executable model, a bug in `Precept.From()` lowering causes silent semantic disagreement. **The lowering must preserve node identity. `inspect` must consume the executable model. Round-trip tests must verify faithful lowering.**

### R9: Branch-conditional narrowing algorithm
The proof engine must split abstract state at branch conditions, propagate independently, and rejoin. **The branch narrowing algorithm must be fully specified before Phase 4.**

### R10: Result type taxonomy — zero precedent
No surveyed system has 9 distinct outcomes. CEL has 3-4, XState has 3, CUE has 2. The 9-outcome requirement derives from the language vision's semantic distinctions. **This is purpose-built design with no direct precedent to adopt.**

### R11: Constraint attribution with transitive expansion — zero precedent
Semantic-subject attribution with transitive computed-field expansion has no precedent. SPARK's per-obligation tracking is the closest (by check category, not business-rule semantics). **Original design work grounded in OPA/SPARK patterns but extending beyond them.**

---

## 9. Open Questions

Questions this plan intentionally does not answer:

1. **Collection mutation semantics in the working copy.** How are `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear` represented in the slot array? Deep-clone or structural sharing?

2. **Temporal and business-domain type evaluation.** What backing representations do these types use at evaluation time? `NodaTime` for temporal, `decimal` for money, custom structs for quantity/price?

3. **String interpolation evaluation.** Special expression node, or desugared to concatenation during lowering?

4. **Nullable field semantics in expression evaluation.** Trust the type checker, or check defensively? A specific instance of R7.

5. **Concurrency model.** The executable model is safe to share (immutable). Independent entity instances are safe to operate concurrently. Same-instance concurrent operations produce conflicting next-states — a host-application coordination concern.

6. **Error message formatting.** How are `because` messages formatted? Localization? Template system?

7. **Performance characteristics of preview.** Working-copy-per-event cost model should be verified against realistic definitions.

8. **Edit operation scope.** Same evaluation path as fire (with "edit" pseudo-event), or separate path? R5 covers fire/preview but not edit.

---

## 10. Stub Evaluation

The provisional stubs are evaluated against survey evidence.

### `Precept` class — the executable model wrapper

```csharp
public sealed class Precept
{
    private Precept() { }
    public static Precept From(CompilationResult compilation) => ...
    public Version From(string state, ImmutableDictionary<string, object?> data) => ...
}
```

**Assessment:** Conflates two concerns: `From(CompilationResult)` creates the executable model, `From(state, data)` creates an entity instance. CEL separates these: `env.Program(ast)` creates the executable; `program.Eval(activation)` is separate. XState separates: `createMachine(config)` vs. `createActor(machine, options)`. Consider whether instance construction belongs on a separate type.

Additionally, `From(string state, ImmutableDictionary<string, object?> data)` uses string state names and a string-keyed dictionary, but the executable model works with slot and state indices internally. The public API may want name-based access for ergonomics, but internally the executable model works with indices.

### `Version` record — the entity snapshot

```csharp
public sealed record class Version(Precept Precept, string State, ImmutableDictionary<string, object?> Data)
{
    public Version Fire(string eventName, ImmutableDictionary<string, object?>? args = null) => ...
    public Version Edit(string field, object? value) => ...
}
```

**Assessment:** Structurally aligned with XState's `MachineSnapshot`. Tensions:

1. **Return type.** `Fire`/`Edit` return `Version` — implying success. The 9-outcome taxonomy requires that these can fail. Return type should be the result type (R2).
2. **Inspectable surface.** R5 resolved: `Version` IS the inspectable surface. Structural queries (`AvailableEvents`, `FieldAccess` with per-field access mode in current state, `RequiredArgs`) are precomputed from graph analysis. Data-dependent queries (`CanFire`, `Preview`, `PreviewEdit`) delegate to the evaluator via the same path as fire/edit. Definition-level access via `Version.Precept`. No separate `Inspect()` method.
3. **`ImmutableDictionary<string, object?>` for `Data`.** Slot array (`object?[]`) would be more aligned with the executable model. Hybrid (slot array internally, name-based API) resolves both.
4. **Instance methods vs. standalone functions.** Operations as methods on `Version` is a convenience API; underlying implementation should be the shared static evaluation function (R1/R5).
5. **Stateless precepts.** Stub requires `State` but stateless precepts have no state.

### `Fault` record — the runtime fault

```csharp
public readonly record struct Fault(FaultCode Code, string CodeName, string Message);
```

**Assessment:** Minimal and correct. Potential enrichments:
1. **Expression location** — which expression caused the fault.
2. **Field attribution** — which fields contributed.
3. **Defensive-redundancy flag** — surface the `[StaticallyPreventable]` link at runtime.

Adequate for the committed chain but may need enrichment when R2 and R6 are designed.

### `Evaluator` class — the execution engine

```csharp
public static class Evaluator
{
    internal static Fault Fail(FaultCode code, params object?[] args) => Faults.Create(code, args);
}
```

**Assessment:** Static shape aligns with R1 (RESOLVED). Awaits Fire/Edit/Preview signatures pending R2 (result type taxonomy) and D8/R4 (executable model contract).

---

## 11. Survey Grounding Strength

### Decisions with strong survey grounding

- **R1 (Evaluator shape):** CEL, XState, and Dhall converge on stateless/functional evaluation. ✅ RESOLVED.
- **R3 (Entity representation):** XState, CEL, CUE, and Dhall use immutable snapshots. Survey consensus is clear.
- **R5 (Version as inspectable surface):** XState's `transition()`/`getNextSnapshot()` pair demonstrates the shared-path preview pattern. CEL's `ExhaustiveEval` demonstrates full-depth preview. Precept's contribution: structural queries from graph analysis are free on Version, data-dependent queries share the fire path. ✅ RESOLVED.

### Decisions with moderate survey grounding

- **D8/R4 (Executable model contract):** CEL's `Program` and OPA's `PreparedEvalQuery` demonstrate the pattern. But neither formally specifies the trust contract — original specification work needed.
- **R7 (Fault correspondence runtime):** SPARK, Dhall, CEL provide theoretical anchors. But no system has Precept's specific `[StaticallyPreventable]` attribute chain.
- **D1 (Pipeline artifact split):** Roslyn for tooling surface, CEL for runtime surface. Well-grounded.
- **D4 (Diagnostic attribution):** Roslyn pattern is directly adoptable for the envelope; attribution extensions are Precept-specific.

### Decisions with weakest survey grounding (most design work required)

- **R2 (Result type taxonomy):** No surveyed system has 9 distinct outcomes. Purpose-built.
- **R6 (Constraint attribution):** Collect-all has precedent (OPA, CUE). Semantic-subject attribution with transitive expansion has zero precedent.
- **D3 (Unit/dimension algebra):** F# UoM is the structural reference, but coupling with proof engine has no precedent.
- **D5 (Proof attribution schema):** Exceeds what any surveyed system provides natively.

---

## 12. Deferred Work Items

Work items parked during the R2/runtime-API deep dive. Each has enough context to resume without reconstructing the session.

### 12.1 Survey Findings Walk-Through (Findings 4–13)

**Source:** `research/architecture/compiler/dry-run-preview-inspect-api-survey.md`

We walked findings 1–3 from the dry-run/preview/inspect API survey. Finding 2 (result type taxonomy) triggered the R2 rabbit hole that consumed multiple sessions. Findings 4–13 have not been reviewed against the architecture plan.

**Resume point:** Read findings 4–13 from the survey. For each, check whether the architecture-planning doc already addresses it, whether it surfaces a new design question, or whether it confirms an existing decision.

### 12.2 D8/R4 — Executable Model Contract

**Status:** Unresolved. This is the single biggest blocker. Every runtime stub has `TODO D8/R4` annotations because the runtime types are consumers of whatever the compiler produces.

**What must be defined:**
- Field descriptor shape (name, type, slot index, constraints, default, computed expression, dependency chain)
- Event descriptor shape (name, arg contracts, which states it appears in, transition row indices)
- State descriptor shape (name, field access modes per field, applicable ensures, entry/exit action chains)
- Constraint descriptor internals (compiled expression tree, guard expression, slot indices — the internal side of what `ConstraintDescriptor` exposes publicly)
- Transition dispatch table shape (keyed by what, row ordering, wildcard handling)
- Slot-indexed expression tree node types (replacing string-keyed identifier lookups)

**Approach decided:** Work forward from `CompilationResult` → `Precept.From()` → executable model. Define the producer, then let descriptor shapes flow into the runtime API.

**Dependencies:** Blocks R3, R6, and all `TODO D8/R4` annotations across 7+ files.

### 12.3 R3 — Entity Representation

**Status:** Unresolved. Depends on D8/R4 (slot layout defines the working copy shape).

**Key questions:** Immutable record vs. slot array. Working copy mechanism (clone-on-write, copy-on-fire). How `Version` snapshots entity state.

### 12.4 R6 — Constraint Attribution Model

**Status:** Partially resolved. The three-tier exposure model (Precept.Constraints → Version.ApplicableConstraints → ConstraintResult/ConstraintViolation) is decided. The *violation target hierarchy* is deferred — currently flat `FieldNames` strings, needs typed targets (field, event-arg, event, state, definition) once D8/R4 descriptors exist.

**Prototype reference:** `src/Precept/Dsl/ConstraintViolation.cs` — the `ConstraintTarget` hierarchy and `ExpressionSubjects` AST walker are the production-validated design to port forward.

### 12.5 G1/G9 — Typed Violation Target Hierarchy

**Status:** Annotated as provisional. Gated on D8/R4.

**What's needed:** Replace `IReadOnlyList<string> FieldNames` on `ConstraintResult` and `ConstraintViolation` with a typed target list that distinguishes field targets from arg targets from event targets from state targets. Targets should reference metadata descriptors, not strings. The prototype's `ConstraintTarget` hierarchy is the design reference.

### 12.6 Remaining Survey Walk-Throughs

Other surveys in `research/architecture/compiler/` that informed the planning doc but whose findings have not been individually walked:
- `compilation-result-type-survey.md` — feeds D1
- `compiler-result-to-runtime-survey.md` — feeds D8/R4
- `compiler-pipeline-architecture-survey.md` — feeds Phase 3 component designs
- `context-sensitive-literal-typing-survey.md` — feeds D2
- `proof-engine-interval-arithmetic-survey.md` — feeds D5 and proof engine design

These are lower priority — the surveys grounded the planning doc's design questions. Individual findings become relevant when the corresponding design document is being written.
