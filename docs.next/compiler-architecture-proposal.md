# Precept Compiler Design Plan

**Date:** 2026-04-21
**Status:** Pre-design planning artifact — identifies what must be designed, in what order, grounded in what evidence
**Authors:** Frank (Lead/Architect), George (Runtime Developer)
**Inputs:** Language Vision (`docs.next/precept-language-vision.md`), 15 external research surveys (`research/architecture/compiler/`)

---

## Framing

This is a planning document, not a design document. Its job is to answer: *what design work needs to happen, in what order, and with what grounding?* The design documents themselves come after this plan is accepted.

The language vision is the authoritative specification of what the compiler must serve. The research corpus (15 surveys covering 50+ external systems) is the authoritative source of precedent. This plan draws on both. All claims are grounded in one of those two sources.

---

## 1. Pipeline Shape

The language vision establishes the execution model properties that directly determine pipeline shape: no loops, no control-flow branches, no reconverging flow, closed type vocabulary, finite state space, expression purity, no separate compilation. These are not incidental — they are the architectural constraints that make the pipeline tractable.

The required pipeline has **six functional stages** in dependency order. Stages 1–5 produce the analysis artifacts; Stage 6 lowers those into the runtime-consumable executable model.

### Stage 1 — Lexer

**Input:** Raw source text (UTF-8)
**Output:** Token stream with spans

Tokenizes keyword-anchored, line-oriented source. Must handle three literal families: primitive (bare), string (`"..."`), typed constants (`'...'`). Time-unit words (`days`, `hours`) are NOT keywords — they are validated content inside typed constants. The lexer produces the outer `'...'` boundary as a token and hands the interior to a later stage.

### Stage 2 — Parser

**Input:** Token stream
**Output:** Concrete syntax tree (CST) or abstract syntax tree (AST)

Produces the structural skeleton without resolving names, types, or scopes. Typed constants are parsed as opaque quoted-content nodes. Modifiers are parsed and attached to declarations as opaque attributes. Every expression form is syntactically closed and finitely bounded — the parser never faces unbounded lookahead ambiguity.

### Stage 3 — Type Checker / Binder

**Input:** Parse tree
**Output:** Typed, name-resolved semantic model

The most complex stage. Must: resolve all declaration identities; resolve all identifier references in expressions; enforce the closed type vocabulary and operator surface; resolve context-sensitive fractional literal types using bidirectional flow; enforce numeric lane integrity and explicit bridge requirements; enforce temporal hierarchy and mediation rules; enforce business-domain operator compatibility and unit commensurability; enforce collection semantics and accessor emptiness guards; enforce choice type membership; resolve computed field dependency graphs and detect cycles; produce typed expression trees where every node carries a resolved type.

### Stage 4 — Graph Analyzer

**Input:** Typed semantic model
**Output:** State graph analysis results (reachability sets, dominator trees, edge classifications, modifier verdicts)

A Precept-specific stage. The vision describes eight graph reasoning capabilities: BFS/DFS reachability, dominator analysis (Lengauer-Tarjan), reverse-reachability, row-partition analysis, outcome-type analysis. Operates on the state graph as an overapproximation — all declared edges traversable regardless of guards.

### Stage 5 — Proof Engine

**Input:** Typed semantic model + graph analysis results
**Output:** Proof model with attribution (proven ranges, proof obligations, contradictions, vacuous checks, dead guards)

Interval-based numeric reasoning over typed expression trees. Handles: divisor safety, sqrt non-negativity obligations, assignment range impossibility, contradictory rule detection, vacuous rule detection, dead guard detection, and compile-time constraint checking. Every proof result carries structured attribution. SMT solvers are excluded by language principle.

### Stage 6 — Emitter

**Input:** Typed semantic model + graph analysis results + proof model
**Output:** Executable model (sealed, immutable, evaluation-ready artifact)

The emitter lowers the analysis-oriented typed model into a runtime-optimized executable form. This is not code generation — it is structural transformation of a correct semantic model into a representation the evaluator can walk without symbol-table lookups, declaration scanning, or dependency re-sorting. Concrete lowering operations:

- **Transition dispatch table** — builds `(state, event) → TransitionRow[]` index for O(1) lookup on every `fire`
- **Field slot resolution** — resolves field name references in expression trees to working-copy slot indices, eliminating string dictionary lookups at evaluation time
- **Computed field linearization** — topologically sorts the dependency graph into an ordered evaluation chain (computed once, walked on every call)
- **Constraint/rule scope indexing** — pre-buckets constraints and rules by scope: `constraintsFor[state]`, `rulesFor[event]`, `entryActionsFor[state]`, `exitActionsFor[state]`
- **Pre-resolved expression trees** — slot-read nodes with lane-resolved literals; self-contained for evaluation without a symbol table

The emitter only runs when Stages 1–5 produce no errors. On broken input, the pipeline stops at the analysis artifacts — the LS and MCP consume those directly. The executable model is never produced from a definition with diagnostics.

### Stage Ordering

1. Lexer → Parser (input dependency)
2. Parser → Type Checker (structural skeleton needed)
3. Type Checker → Graph Analyzer and Proof Engine (both consume the typed model)
4. Graph Analyzer and Proof Engine can run in parallel after Type Checker
5. Optional synchronization: Proof Engine can use graph results to sharpen reachability reasoning
6. Emitter runs after Stages 3–5, only on error-free input

### Two Pipeline Artifacts

The pipeline produces **two distinct artifacts** for **two distinct consumers**:

**Compilation Result** (tooling surface) — the analysis artifact. Contains: all-stage diagnostics (collected, not short-circuited), typed semantic model queryable by span, proof results, graph analysis results. Produced on every pipeline run, including broken input. Consumed by the language server (hover, completions, go-to-definition, diagnostics) and MCP tools. Follows the Roslyn pattern of an immutable snapshot with partial results on every keystroke.

**Executable Model** (runtime surface) — the execution artifact. Contains: transition dispatch table, slot-indexed expression trees, scope-indexed constraint lists, field descriptor array, topological action chains. Sealed, immutable, evaluation-ready. Only produced when the compilation result has no errors. Consumed by the runtime operations (`fire`, `inspect`, `update`). Follows the CEL `Ast → Program` pattern — a compiled representation that an evaluator runs against entity data to produce outcomes.

The executable model is not a Roslyn-style `Compilation` (a tooling query surface). It is closer to CEL\u2019s `Program` (a compiled expression evaluated against an activation) or OPA\u2019s `PreparedEvalQuery` (compiled state held internally, safe to share across goroutines). The distinction matters: the LS never touches the executable model; the runtime never queries the compilation result for diagnostics.

**Correctness invariant:** `inspect` MUST go through the full pipeline including the emitter. If inspect and fire both consume the executable model via the same evaluation path, semantic agreement between tooling preview and runtime execution is structural, not conventional.

---

## 2. Component Inventory

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
- Abstract value representation — closed interval `[lo, hi]` for numerics, with "unknown" state, null handling for nullable fields, finite set for choice
- Forward propagation model — single-pass walk over action chains; reassignment invalidates prior facts
- Constraint contribution model — field constraints, rules, and guards enter the proof state with attribution
- Relational reasoning scope — transitive interval narrowing only, or full constraint propagation? This MUST be pinned before implementation.
- Proof attribution data model — structured records carrying interval + contributing constraints + scope. Must be designed BEFORE the proof engine is built.
- Proof-outcome taxonomy — proved dangerous, proved safe, unresolved → diagnostic severity mapping
- Branch-conditional intervals — `if Count > 0 then Total / Count else 0` must model guard as proof fact inside each branch
- Minimum viable scope — Phase 1: single-field intervals, divisor safety, non-negative obligations, contradictory rules. Phase 2: multi-field relational constraints, dead guard detection.

**Research grounding:** Proof-engine-interval-arithmetic survey, proof-attribution-witness-design survey.

**Right-sizing:** No loops means no widening, no fixpoint, no lattice infrastructure. The proof engine is a single-pass interval propagator with attribution. But getting attribution right is genuinely hard design work.

### 2.6 Emitter

**Design questions:**
- What lowering operations are needed beyond the five identified (dispatch table, slot resolution, linearization, scope indexing, expression tree pre-resolution)?
- How is the transition dispatch table keyed — `(state, event)` tuple, or separate nested lookups? How are wildcard/default rows ordered?
- Is expression tree lowering a structural transformation (new node types) or an in-place annotation (slot indices added to existing nodes)?
- Does the emitter produce a single sealed object or a family of related immutable structures?
- What is the emitter's contract with the evaluator — does it guarantee specific structural invariants the evaluator can rely on unconditionally?

**Research grounding:** Compiler-result-to-runtime survey (CEL Ast→Program lowering, OPA PreparedEvalQuery), dry-run-preview-inspect-api survey (XState compiled machine).

**Right-sizing:** The lowering is real but bounded. At Precept's scale, individual operations (dispatch table construction, slot assignment, topological sort) are straightforward. The design complexity is in the contract — what invariants the emitter guarantees and the evaluator assumes. If that contract is well-specified, the implementation is modest.

### 2.7 Evaluator

**Design questions:**
- Evaluation strategy — tree-walking interpreter over emitter-produced, slot-indexed expression trees. No JIT compilation. Expression surface is pure, tree is finite, no hot-loop pressure.
- Totality and error representation — discriminated union `EvalResult<T> = Value(T) | EvalError(...)`, or throw/catch? The evaluator must have a principled answer for every failure case.
- Collect-all vs. first-match execution modes — two separate entry points with shared expression machinery, or parameterized by mode?
- Working copy atomicity — flat value array indexed by slot number (not a string-keyed map). The emitter's slot assignment makes this possible.
- Inspect as fire variant — `inspect` is `fire` with a working copy that is always discarded. Both consume the executable model via the same evaluation path. Must share the same code.
- Lane integrity at eval level — trust the type checker (static completeness principle). Each slot has a known type tag from the emitter, making lane confusion physically impossible at the slot level without runtime checks.

**Research grounding:** Compiler-result-to-runtime survey (CEL evaluator), dry-run-preview-inspect-api survey (XState transition), exact-decimal-arithmetic survey, outcome-type-taxonomy survey.

### 2.8 Diagnostic System

**Design questions:**
- Descriptor vs. instance separation (Roslyn pattern)
- Location model — span-based, with line/column derivable
- Severity model — Error, Warning, Info/Hint (the vision's modifier system includes warning-severity rules)
- Secondary locations — for proof diagnostics, the constraints that contributed to the violation
- Constraint violation subject attribution — semantic subjects, scope kind, anchor (richer than standard location lists)
- Serialization format — for MCP, LS, batch output

**Research grounding:** Diagnostic-and-output-design survey, proof-attribution-witness-design survey.

### 2.9 Pipeline Artifacts & Consumer Contracts

This is the single most important design section — it defines the boundary between the compiler and all consumers.

The pipeline produces two artifacts with distinct consumer APIs. This section covers both as a single design responsibility.

**Compilation Result (tooling surface):**
- All-stage diagnostics aggregated and queryable
- Typed semantic model queryable by span (hover, completions, go-to-definition)
- Proof results for proof-hover and diagnostic attribution
- Graph analysis results for modifier diagnostics
- Produced on every pipeline run, including broken input
- LS coupling model — same codebase (Roslyn/TypeScript pattern), or published API? Same-codebase is the dominant pattern; LS-to-compiler code ratio is ~1:80.
- Incremental strategy — full recompile on each edit (likely acceptable at DSL file sizes), or incremental CST reuse?
- Cancellation — long-running proof/graph analysis must be cancellable on new keystrokes

**Executable Model (runtime surface):**
- Transition dispatch table, slot-indexed expression trees, scope-indexed constraint lists, field descriptor array, topological action chains
- Sealed, immutable, evaluation-ready
- Only produced when compilation result has no errors
- Outcome type hierarchy — 9 outcomes must be structurally distinguishable at the C# type level (sealed hierarchy, not string codes)
- Constraint violation attribution record — semantic subjects + scope + anchor as structured data, consumable by diagnostics, hover, and MCP
- Caching and lifetime — safe to cache per file-modification-timestamp, shareable across entity instances and concurrent operations

**Cross-cutting design questions:**
- What is the relationship between the two artifacts? Produced from the same pipeline session but distinct types with distinct API contracts.
- Proof model placement — proof results serve both consumers: tooling (hover diagnostics) and potentially runtime (skip redundant checks based on proven ranges). Where does the proof model live, and is it shared or duplicated?
- Does `inspect` consume the executable model (guaranteeing semantic agreement with `fire`) or the compilation result? The architectural position is: inspect MUST consume the executable model.

**Research grounding:** Language-server-integration survey, compilation-result-type survey, compiler-result-to-runtime survey (CEL Ast→Program, OPA PreparedEvalQuery), outcome-type-taxonomy survey, dry-run-preview-inspect-api survey.

---

## 3. Key Design Decisions

Seven architectural choices plus one new emitter contract decision that must be resolved before implementation.

### D1 — Pipeline artifact split

**Question:** What are the two pipeline artifacts (compilation result and executable model), what is in each, and how are they produced and related?
**Framing:** The pipeline produces a tooling surface (compilation result: diagnostics, typed model, proof, graph — for LS/MCP) and a runtime surface (executable model: dispatch tables, slot-indexed trees, scope-indexed constraints — for fire/inspect/update). These are distinct types with distinct API contracts, produced from the same pipeline session. The executable model is only produced when the compilation result has no errors.
**Survey grounding:** Compilation-result-type survey (Roslyn for the tooling surface pattern), compiler-result-to-runtime survey (CEL Ast→Program for the runtime surface pattern).
**Coupling:** Couples to D7 (LS strategy), D4 (diagnostic aggregation), and D8 (emitter contract). Resolve first.

### D2 — Literal resolution mechanism

**Question:** How do fractional literals acquire their lane from context — expected-type propagation (Kotlin/Swift), constraint solver (Haskell), or default-to-decimal with explicit override?
**Survey grounding:** Context-sensitive-literal-typing survey.
**Coupling:** Couples to D3 (unit algebra) and the typed expression tree design.

### D3 — Unit/dimension algebra representation

**Question:** F#-style exponent vectors, admissibility table, or hybrid?
**Survey grounding:** Units-of-measure survey.
**Coupling:** One of the hardest design problems. Couples to D2 and temporal type hierarchy. Resolve before finalizing the type checker.

### D4 — Diagnostic attribution structure

**Question:** How do diagnostics carry constraint violation subject attribution?
**Survey grounding:** Diagnostic-and-output-design survey, proof-attribution survey.
**Coupling:** Couples to D1 and D5.

### D5 — Proof attribution schema

**Question:** What is the structure of a proof result — flat list per field, expression-tree-mirroring tree, or separate queryable model?
**Survey grounding:** Proof-attribution survey, proof-engine-interval-arithmetic survey.
**Coupling:** Couples to D4 and D1. Must be designed before the proof engine is built.

### D6 — LS incremental strategy

**Question:** Full recompile (likely acceptable at DSL file sizes), incremental CST (Roslyn red-green trees), or stage-level caching?
**Survey grounding:** Language-server-integration survey.
**Coupling:** Affects CST/AST design in the parser.

### D7 — LS coupling model

**Question:** Same codebase (Roslyn/TypeScript), published API, or separate process?
**Survey grounding:** Language-server-integration survey. Same-codebase is the dominant pattern. LS-to-compiler code ratio at DSL scale is 1:3 to 1:10 (Dhall 1:10, Regal/OPA 1:8, Jsonnet 1:3). The Roslyn/OmniSharp ratio of 1:80 is GP-scale, not representative for Precept.
**Coupling:** Couples to D1 and D6. Resolve early.

### D8 — Emitter contract

**Question:** What structural invariants does the emitter guarantee that the evaluator can rely on unconditionally? What is the lowering boundary — which transformations happen in the emitter vs. lazily in the evaluator?
**Survey grounding:** Compiler-result-to-runtime survey (CEL Program internals, OPA PreparedEvalQuery).
**Coupling:** Couples to D1 (defines the executable model's content) and blocks evaluator design. Must be resolved before Phase 4.

---

## 4. Design Work Sequencing

### Phase 1 — Architecture Foundations (blocks everything)

| Document | Decisions Answered | Primary Survey References |
|----------|-------------------|--------------------------|
| **Pipeline Artifacts & Consumer Contracts** | D1, D6, D7 | compilation-result-type-survey, language-server-integration-survey, compiler-result-to-runtime-survey |
| **Diagnostic System** | D4 | diagnostic-and-output-design-survey, proof-attribution-witness-design-survey |

Phase 1 documents can be written in parallel with each other. The Pipeline Artifacts document replaces the earlier "Compilation Result & LS Integration" scope — it must cover both the tooling surface (compilation result) and the runtime surface (executable model) as a single design.

### Phase 2 — Type System Core (blocks type checker)

| Document | Decisions Answered | Primary Survey References |
|----------|-------------------|--------------------------|
| **Literal Resolution Mechanism** | D2 | context-sensitive-literal-typing-survey |
| **Unit/Dimension Algebra** | D3 | units-of-measure-dimensional-analysis-survey, temporal-type-hierarchy-survey |

Phase 2 documents can be written in parallel with each other. Can overlap with Phase 1 — literal resolution and unit algebra are independent of result/diagnostic shape decisions.

### Phase 3 — Pipeline Component Designs (parallelizable)

| Document | Dependencies | Primary Survey References |
|----------|-------------|--------------------------|
| **Lexer** | Phase 1 | compiler-pipeline-architecture-survey |
| **Parser** | Phase 1, D6 | compiler-pipeline-architecture-survey |
| **Type Checker** | Phase 1, Phase 2 | All Phase 2 documents |
| **Graph Analyzer** | Phase 1 | state-graph-analysis-survey |

All Phase 3 documents can be written in parallel once Phase 1 and 2 are complete.

### Phase 4 — Emitter, Proof & Evaluator Design (depends on type checker)

| Document | Dependencies | Primary Survey References |
|----------|-------------|---------------------------|
| **Emitter & Executable Model** | Phase 1 (D1), Phase 3 (type checker design) | compiler-result-to-runtime-survey, dry-run-preview-inspect-api-survey |
| **Proof Engine** | Phase 3 (type checker design) + D5 | proof-engine-interval-arithmetic-survey, proof-attribution-witness-design-survey |
| **Evaluator Architecture** | Emitter design (D8) + Phase 3 (type checker) | compiler-result-to-runtime-survey, exact-decimal-arithmetic-survey, dry-run-preview-inspect-api-survey |

The Emitter design must precede or accompany the Evaluator design — the evaluator's hot path depends on the data structures the emitter produces. Proof Engine can proceed in parallel with Emitter since they share no output dependency.

---

## 5. Right-Sizing Principles

### Scale Reference Points from the Research

The surveys provide concrete scale data for comparable systems:

| System | Component | Scale | Relevance |
|--------|-----------|-------|-----------|
| CEL (cel-go) | Full pipeline (parse+check+program+eval) | 30–40K lines Go | Most relevant overall scale reference — DSL with similar pipeline shape |
| OPA/Rego | Full compiler + evaluator | ~200K lines Go | Upper bound — Rego has recursion, set comprehensions, Datalog semantics |
| TypeScript | Type checker alone (checker.ts) | ~50K lines | Scale warning, not a target — parametric generics, structural subtyping, conditional types |
| Roslyn | Full compiler | ~4–5M lines C# | Scale warning — entirely different magnitude |
| NodaTime | Temporal type hierarchy | 15 primary types, ~40 operations | Direct scale reference for Precept's 8 temporal types |
| F# UoM | Unit system extension | Contained type checker extension | Direct reference for unit algebra (exponent vector core is compact; complexity is in normalization and error messages) |
| Dhall LS | Language server for DSL | ~3K lines (1:10 to core) | DSL-scale LS reference |
| Regal/OPA LS | Language server for OPA | ~25K lines (1:8 to core) | DSL-scale LS reference |

The DSL-scale systems (CEL, OPA, Dhall, Jsonnet) consistently show: 30K–200K lines total, full recompile per edit, LS-to-compiler ratio of 1:3 to 1:10. The GP-scale systems (Roslyn, TypeScript, rustc) show: 300K–5M lines, incremental infrastructure, LS-to-compiler ratio of 1:7 to 1:80. **Precept anchors to the DSL-scale pattern.**

### Where Precept's constraints genuinely simplify

**Proof engine.** General-purpose analyzers (SPARK/GNATprove, Frama-C EVA, Astrée) require widening operators, abstract lattices, and SMT solvers because programs have loops and unbounded state. Precept has none. SPARK's Interval pass — a lightweight pre-filter before SMT invocation — is the structural template: forward propagation using type-declared bounds. CBMC is explicitly "most precise on loop-free, bounded programs — the encoding is exact." No loops → no widening, no fixpoint, no lattice infrastructure, no external solver. Single-pass interval propagator.

**State graph.** SPIN and NuSMV handle millions of states. Alloy targets "typically < 20 atoms per signature." LLVM's DominatorTree computes in sub-milliseconds for <1000 nodes. Precept graphs are typically 5–20 states. BFS/DFS with a simple visited set is the entire reachability algorithm. At this scale, Cooper-Harvey-Kennedy's simpler iterative O(V·E) dominator algorithm may outperform Lengauer-Tarjan due to cache effects — both run in microseconds.

**Parser.** No operator precedence puzzles, no significant whitespace, no forward references across files, no ambiguous overloading. Roslyn, TypeScript, and rust-analyzer all use hand-written recursive descent — confirmed as the right technique for this grammar complexity.

**Separate compilation.** None. No modules, no imports, no cross-file linkage. One file, full compilation, every time.

**Incremental recompilation.** Files are small. Pipeline is fast (no code gen, no linking, no SMT). CEL, OPA, Dhall, and Jsonnet all do full recompile per edit and report acceptable latency. Full recompile on every edit is very likely acceptable — validate before investing in incremental infrastructure. **Cancellation token threading (Roslyn/TypeScript pattern) is the first defense** if proof or graph analysis grows slow, not incremental infrastructure.

**Literal resolution.** The plan frames this as "bidirectional flow," but for Precept's closed two-candidate set (decimal vs. number), the Kotlin ILT precedent shows this reduces to a simple conditional: check expected type → if it fits, use it; else use the default. This is genuinely simpler than the bidirectional framing suggests.

**Diagnostic base structure.** Roslyn's descriptor/instance separation (`DiagnosticDescriptor` + `Diagnostic`) is directly adoptable without significant design work. The design effort is in the attribution extensions (proof, constraint violation), not the diagnostic envelope itself.

### Where constraints do NOT simplify

**Unit/dimension algebra.** Compound cancellation (`price × quantity → money`, `exchangerate × money → money`) requires genuine type-level algebra. F# UoM uses normalized exponent vectors with integer powers — compact core, but: (a) F# erases units before IL emission, and Precept's proof engine needs unit info during interval reasoning — a coupling no surveyed system documents; (b) F# only supports integer exponents; if Precept ever needs fractional powers, integer-only isn't forward-compatible; (c) every surveyed unit system (F#, Boost.Units, Haskell `units`) reports poor error message quality for unit mismatches. The plan must address diagnostic UX for unit errors explicitly.

**Proof attribution.** SPARK ended up with THREE separate output formats (prose text, tabular `gnatprove.out`, SARIF JSON) each serving different consumers. Dafny uses the LSP `Diagnostic` schema (constrained by the standard's `relatedInformation` field to location arrays without structured semantic labels). Precept's requirement — a single schema for hover + MCP + diagnostics with semantic subjects, scope kind, and anchor — exceeds what any surveyed system provides. This is purpose-built design work.

**Parser error recovery.** The plan correctly identifies this as requiring explicit design, but the surveys show it's a set of per-construct recovery strategies, not one design decision. Roslyn uses two mechanisms (missing tokens with `IsMissing == true` inserted at expected positions, and skipped tokens attached as trivia). Pkl's team switched from their hand-rolled parser to tree-sitter for the LS specifically because recovery was too hard. If recovery design proves difficult, tree-sitter is a known fallback path.

**Branch-conditional interval narrowing.** The plan describes the proof engine as a "single-pass interval propagator" — technically accurate but understated. For expressions like `if Count > 0 then Total / Count else 0`, the proof engine must split abstract state at the branch condition (narrowing `Count` to `[1, ∞)` in the then-branch), propagate each branch independently, and rejoin at the expression result. Frama-C EVA implements this via abstract state splitting at every branch point. This is the non-trivial component of interval propagation — it's a real algorithm, not a footnote. Must be fully specified before Phase 4.

**Diagnostic attribution.** Constraint violation subject attribution (semantic subjects, scope kind, anchor) is richer than standard systems provide. Roslyn's `AdditionalLocations` provides location arrays but no structured semantic labels. The richer attribution schema is Precept-specific.

**Modifier system.** Novel language feature with no survey precedent for the admission logic. No surveyed system declares expected lifecycle properties and validates them structurally against graph analysis. The algorithms (reachability, dominators) are standard; the semantic specification of what makes a modifier satisfied is original work.

**Emitter node identity preservation.** CEL's `Interpretable` tree carries AST node IDs for correlation between compile-time and evaluation-time results (`EvalState` maps IDs to runtime-observed values). The plan's emitter must preserve expression identity through lowering so the LS can correlate evaluation results back to source spans for hover and inspect output. If slot numbers replace expression references and source identity is dropped, LS-to-evaluation correlation breaks. No surveyed system formally specifies this invariant.

### Where DSL-scale and GP-scale diverge

| Dimension | DSL-scale (CEL, OPA, XState) | GP-compiler (Roslyn, TypeScript, rustc) |
|-----------|------------------------------|----------------------------------------|
| Pipeline | Sequential, explicit stages | Query-driven or demand-driven |
| Artifact | Single compiled object | Complex compilation graph with incremental reuse |
| LS coupling | Full recompile, ratio 1:3 to 1:10 | Incremental, snapshot-based, ratio 1:7 to 1:80 |
| Error recovery | Collect model, limited heuristics | Full recovery, missing tokens, error propagation |
| Proof/analysis | Post-hoc (separate tools) or none | N/A |
| Scale | 30K–200K lines | 300K–5M lines |

The plan anchors to the DSL-scale column. **The discipline test is on features where DSL-scale systems have no precedent** — unit algebra (F# is GP-scale), proof engine (all surveyed proof engines are GP-scale tools), multi-terminal dominators (formal verification tools). For those features, the plan draws patterns from GP-scale systems while holding to DSL-scale implementation discipline.

### The right-sizing principle

**Solve the problem the language vision actually specifies, grounded in survey precedent, at a scale proportionate to the definition surface.** The language is small, the files are small, the pipeline does not generate code. CEL at 30–40K lines is the scale reference for the full pipeline; not Roslyn at 4M.

### Areas with no survey resolution

The following areas require original design work — the 15 surveys found no precedent, only partial analogies. The plan must not underestimate these or assume they'll be resolved by adopting an existing system's approach.

| Area | Closest analogy | Gap |
|------|----------------|-----|
| Modifier admission logic | Reachability/dominator algorithms | The semantics of what makes a modifier "satisfied" are Precept-specific — no surveyed system declares expected lifecycle properties |
| Multi-terminal dominator path obligations | LLVM DominatorTree (single root), UPPAAL specification properties | No surveyed system provides multi-terminal "every path passes through D" as a compiler-stage API (R5) |
| Proof engine + unit type coupling | F# UoM (erases units before codegen), SPARK (no unit types) | No surveyed system retains unit info through proof-level interval reasoning |
| Business-domain semantic operators | Dimensional analysis cancellation rules | Operators beyond standard arithmetic cancellation (e.g., temporal arithmetic semantics) are Precept-specific |
| `inspect` as fire-via-emitter | CEL EvalState (records values per node), OPA partial evaluation | No surveyed system uses the emitter's executable model to produce a per-event preview without side effects |
| Unit error diagnostic UX | F#, Boost.Units, Haskell `units` (all poor quality) | Every surveyed unit system reports poor unit mismatch messages — there is no good model to adopt |

---

## 6. Risks and Hard Problems

### R1: Unit cancellation arithmetic in the type checker
The `price × quantity → money` rule is a type algebra, not a lookup table. Compound unit types (`price in 'USD/kg'`) require embedding unit dimension vectors in the type representation. F# UoM uses normalized exponent vectors with integer powers — compact core, but: (a) F# erases units before IL emission, and Precept's proof engine needs unit info during interval reasoning — a coupling no surveyed system documents; (b) F# only supports integer exponents; if Precept ever needs fractional powers, integer-only isn't forward-compatible; (c) every surveyed unit system (F#, Boost.Units, Haskell `units`) reports poor error message quality for unit mismatches. **Hardest single type-checker design problem. Unit error diagnostic UX must be designed explicitly — survey precedent offers no good model.**

### R2: Relational reasoning scope ambiguity
The vision mentions relational reasoning but does not define its scope precisely. If the team builds toward full two-variable constraint solving, the proof engine expands into constraint programming. If pinned to transitive interval narrowing, many proofs come back "unresolved." **Must be pinned to a precise definition before proof engine implementation.**

### R3: Typed constant narrowing failure mode
The two-door literal system may produce ambiguous context in some expression positions. Every surveyed system with literal type inference had to design an explicit failure diagnostic: Kotlin (`INTEGER_LITERAL_OUT_OF_RANGE`), Rust (`E0282: type annotations needed`), Swift (`expression type 'T' is ambiguous without more context`). Precept's diagnostic must name the two candidates and why neither was selected. **Design the narrowing algorithm and its failure mode explicitly — not as a generic "emit error" step, but as a specified diagnostic with concrete message format.**

### R4: Proof attribution is a blocking dependency
Every surveyed proof system designed its attribution schema before implementing the proof engine — not after. SPARK has three output formats (prose, tabular `gnatprove.out`, SARIF JSON). Dafny uses the LSP `Diagnostic` schema, constrained by the standard's `relatedInformation` field to location arrays without structured semantic labels. Precept's requirement (hover + MCP + diagnostics with semantic subjects) exceeds what any surveyed system provides natively. **Design the attribution data type first — it constrains what the engine must produce.**

### R5: Multi-terminal dominator semantics
All surveyed dominator algorithms assume single-entry/single-exit. LLVM's `DominatorTree` assumes a single root. UPPAAL and SCXML can express path properties via specification languages, but those are model-checking tools, not compiler pipeline stages. No surveyed system provides a clean API for multi-terminal path obligation: "every path from initial to any terminal passes through state D." **Get a mathematical specification of path-obligation semantics approved before implementing dominator analysis — this is original formal work with no survey resolution.**

### R6: Decimal division vs. totality
`System.Decimal` silently rounds non-terminating division (`1m / 3m` produces 28 threes, no exception, no inexactness flag). Java BigDecimal forces the choice: `divide(BigDecimal)` without rounding parameters throws `ArithmeticException` on non-terminating results; `divide(BigDecimal, int, RoundingMode)` requires explicit rounding acknowledgment. The language promises approximation honesty. **Three options: (a) prohibit division in the decimal lane, (b) require explicit rounding declaration in the DSL (Java BigDecimal model), (c) accept silent approximation. This is a language-level decision that must be resolved explicitly before evaluator design.**

### R7: LS responsiveness under proof load
All DSL-scale systems that do full recompile (CEL, OPA, Dhall, Jsonnet) are doing so on pipelines WITHOUT proof-level analysis. SPARK's Interval pass is a pre-filter precisely because the SMT layer is slow. If Precept's proof engine grows to include relational reasoning, the "full recompile is fine" assumption may break. **Cancellation token threading (Roslyn/TypeScript pattern) is the first defense. Incremental infrastructure is a later investment if profiling demands it.**

### R8: Emitter semantic drift
If the LS consumes the typed semantic model while the runtime evaluates the emitted executable model, a bug in the emitter's lowering could cause silent semantic disagreement. CEL's `Interpretable` tree preserves AST node IDs for correlation between compile-time and evaluation-time results (`EvalState` maps IDs to runtime-observed values). The mitigation is architectural: `inspect` must go through the emitter, and round-trip tests must verify faithful lowering. **The emitter must preserve node identity through lowering so the LS can correlate evaluation results back to source spans. If slot numbers replace expression references and source identity is dropped, LS-to-evaluation correlation breaks.**

### R9: Branch-conditional narrowing algorithm
The plan describes the proof engine as a "single-pass interval propagator" — technically accurate but understated. For expressions like `if Count > 0 then Total / Count else 0`, the proof engine must split abstract state at the branch condition, narrow `Count` to `[1, ∞)` in the then-branch, propagate each branch independently, and rejoin at the expression result. Frama-C EVA implements this via abstract state splitting at every branch point. **This is the non-trivial component of interval propagation. The branch narrowing algorithm must be fully specified before Phase 4 — it is not a footnote to the forward-propagation pass.**

---

## 7. Design Document Inventory

Ten design documents need to be written, in the sequencing above.

| Phase | Document | Decisions | Primary Survey References |
|-------|----------|-----------|--------------------------|
| 1 | Pipeline Artifacts & Consumer Contracts | D1, D6, D7 | compilation-result-type-survey, language-server-integration-survey, compiler-result-to-runtime-survey |
| 1 | Diagnostic System | D4 | diagnostic-and-output-design-survey, proof-attribution-witness-design-survey |
| 2 | Literal Resolution Mechanism | D2 | context-sensitive-literal-typing-survey |
| 2 | Unit/Dimension Algebra | D3 | units-of-measure-dimensional-analysis-survey, temporal-type-hierarchy-survey |
| 3 | Lexer | — | compiler-pipeline-architecture-survey |
| 3 | Parser | D6 applied | compiler-pipeline-architecture-survey |
| 3 | Type Checker | D2, D3 applied | All Phase 2 documents |
| 3 | Graph Analyzer | — | state-graph-analysis-survey |
| 4 | Emitter & Executable Model | D8 | compiler-result-to-runtime-survey, dry-run-preview-inspect-api-survey |
| 4 | Proof Engine & Evaluator | D5 | proof-engine-interval-arithmetic-survey, proof-attribution-witness-design-survey, exact-decimal-arithmetic-survey |
