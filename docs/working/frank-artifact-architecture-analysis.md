Agent completed. agent_id: frank-artifact-architecture-an, agent_type: general-purpose, status: completed, description: Frank: Deep analysis of SyntaxTree/SemanticIndex architecture options, elapsed: 321s, total_turns: 0, model: claude-opus-4.6, duration: 318s

> Historical working note: later decisions renamed the current artifacts to `SemanticIndex`, `StateGraph`, `ProofLedger`, `Compilation`, and the runtime-construction stage to **Precept Builder**. Terminology in this note has been normalized where practical, but the recommendation timestamps remain historical.

I now have comprehensive data from the research corpus, design docs, and implementation status. Let me synthesize the analysis.

---

# Architectural Analysis: Compile-Time Artifact Strategy (Parser → Type Checker)

## 1. Executive Conclusion

**Recommendation: Option C — with precision about what "semantic database" means.**

The current doc's direction is correct in architecture but imprecise in vocabulary. `SemanticIndex` is not one thing — it is three things fused under one name. The doc should decompose it into its actual constituents while keeping the two-artifact boundary (SyntaxTree vs. semantic layer) that is architecturally sound.

The specific recommendation is:

- **SyntaxTree** — unchanged. Parser-owned, source-faithful, recovery-aware.
- **SemanticIndex** — retained as the name for the type checker's output, but the doc should explicitly acknowledge its three internal sub-structures:
  1. **Symbol table / declarations** — stable semantic identities, normalized declaration inventories, reference bindings
  2. **Typed expression graph** — a DAG of resolved expressions with operation/function/type identity (not a tree mirroring syntax — a DAG because common subexpressions and field references converge)
  3. **Dependency facts** — computed-field dependencies, arg dependencies, referenced-field sets, constraint-to-field influence edges

These are not three separate artifacts. They are three facets of one artifact. The `SemanticIndex` record should expose them as named sub-structures (e.g. `SemanticIndex.Symbols`, `SemanticIndex.Expressions`, `SemanticIndex.Dependencies`) rather than a flat bag of top-level collections.

This is Option C. But the reason it wins is specific to Precept — not because "flat is always better" or "DSL-scale norms say so."

---

## 2. Option-by-Option Comparison

### Option A — Annotated Syntax (one tree, stamped)

**How it works:** Parser produces `SyntaxTree`. Type checker walks it and attaches semantic info as side-tables keyed by node IDs (CEL pattern) or as mutable properties on nodes (OPA/Rego pattern).

**Research grounding:** This is the dominant DSL-scale pattern. CEL uses `CheckedExpr` = original `Expr` + `type_map` + `reference_map`. OPA mutates `Module` in multiple passes. Dhall parameterizes `Expr s a` through phases. Pkl annotates `PklNode` with `VmType`.

**Strengths:**
- Simplest to implement. One tree, one walk, one set of node types.
- Attribution is trivial — every semantic fact is already keyed to a syntax node.
- LS hover/go-to-def is straightforward: find syntax node at position, look up semantic info.

**Weaknesses for Precept:**
- **Violates ownership clarity.** Recovery shape (parser concern) and semantic identity (type checker concern) are entangled in one artifact. A parser change that adjusts error-recovery shape can break semantic consumers.
- **Graph analysis and proof want normalized inventories, not syntax nesting.** The graph analyzer needs "all transition rows from state X to state Y" as a flat set, not "walk the syntax tree looking for transition blocks nested under state declarations." Same for proof: obligations are instantiated from semantic sites, not syntax positions.
- **The Precept Builder stage wants semantic inventories.** Building dispatch indexes from annotated syntax means traversing tree structure to collect facts that should already be organized by semantic role. This is wasted work at builder time and a coupling risk.
- **The anti-mirroring rules exist for a reason.** The doc's anti-mirroring rules (§6) would be unenforceable under Option A — the artifact IS the syntax tree, so every consumer walks syntax.

**Verdict:** Wrong for Precept. The doc is right to reject this. The research confirms it's the easy path, but Precept's downstream consumers (graph, proof, Precept Builder) all want normalized semantic facts, not tree-structured syntax with annotations. The LS would also blur the syntax/semantic feature boundary.

### Option B — Two True Trees (parallel syntax tree and semantic tree)

**How it works:** Parser produces `SyntaxTree`. Type checker produces a second tree-shaped artifact that is structurally parallel to syntax — a `TypedSyntaxTree` or `SemanticTree` with typed nodes corresponding to syntax nodes, but carrying resolved types, symbols, and bindings instead of tokens.

**Research grounding:** Roslyn's internal bound tree (not public) is the closest precedent — a separate tree of `BoundNode` objects produced by the binder, structurally parallel to the syntax tree. Kotlin K2's FIR is another — a separate tree from PSI, though mutated in phases. Go's `ir.Node` IR after type checking. None of these are DSL-scale systems.

**Strengths:**
- Clear separation of concerns: syntax tree owns source fidelity, semantic tree owns typed structure.
- Proof engine gets tree-shaped attribution for free — every proof obligation maps to a node in the semantic tree, which maps back to a syntax node via structural correspondence.
- Expression evaluation could walk the semantic tree directly (tree-walk interpreter pattern).

**Weaknesses for Precept:**
- **Over-structured for what downstream consumers actually need.** Graph analysis does not want a tree — it wants flat inventories of states, events, transition rows. Proof does not want a tree — it wants a flat inventory of proof obligations keyed to semantic sites. The Precept Builder stage does not want a tree — it wants semantic inventories organized by activation anchor, not by source nesting.
- **Mirrors syntax structure even where semantic consumers don't need it.** If the semantic tree preserves the nesting of `state { event { transition } }`, that nesting is parser layout leaking into the semantic layer — exactly what anti-mirroring rule #1 prohibits.
- **Implementation cost for no consumer benefit.** Building a full parallel tree means defining node types for every syntax construct with typed equivalents. At Precept's grammar scale (flat, ~15 construct kinds), this is manageable but pointless — the tree structure adds no queryable value over flat inventories.
- **The term "two-tree" in the current doc is misleading.** The doc says "two-tree architecture" but then describes `SemanticIndex` as a "semantic database of symbols, bindings, and normalized declarations" — which is NOT a tree. The doc contradicts itself. The `SemanticIndex` the doc actually describes is Option C, not Option B.

**Verdict:** Wrong for Precept. This is what the doc's section title says ("two-tree"), but not what the doc's content describes. If you actually built a second tree structurally parallel to syntax, you'd violate the doc's own anti-mirroring rules. The research shows that parallel semantic trees (Roslyn's bound tree, K2 FIR) appear in large general-purpose compilers where tree structure serves lowering — Precept's Precept Builder stage needs flat inventories, not tree structure.

### Option C — Syntax Tree + Semantic Model / Semantic Database

**How it works:** Parser produces `SyntaxTree`. Type checker produces a semantic artifact organized by symbols, bindings, normalized declarations, typed expressions, dependency facts, and source-origin handles. Not a tree — a queryable database of semantic facts.

**Research grounding:** Roslyn's public `SemanticModel` is the closest precedent — it's not a tree but a queryable facade over the compilation's semantic analysis, keyed by syntax nodes. Roslyn's `Compilation` object holds symbol tables (`GlobalNamespace`, `Assembly`) as flat hierarchies, not syntax-parallel trees. The compilation-result-type survey confirms: Roslyn, TypeScript, Go, Swift all expose semantic information through flat symbol tables and binding lookups, not through second trees.

**Strengths:**
- **Matches what every downstream consumer actually needs.** Graph analysis wants: "give me all transition rows" → flat inventory. Proof wants: "give me all proof obligations at semantic sites" → flat inventory with source-origin handles. The Precept Builder stage wants: "give me all constraints grouped by activation anchor" → flat inventory. LS wants: "give me the symbol at this position" → symbol table lookup with source-origin handle. None of these want tree nesting.
- **Source-origin handles provide attribution without tree coupling.** Every semantic symbol and expression carries a handle back to its source span. Proof attribution, LS hover, go-to-def — all work through handles, not tree correspondence.
- **Normalized declarations are naturally flat.** A `rule` declared inside a `state` block in syntax becomes an entry in the `SemanticIndex.ConstraintInventory` with a `ConstraintActivation.InState` anchor and a source-origin handle. The semantic representation is shaped by semantic role, not source nesting.
- **Testing is straightforward.** Assert against flat inventories: "model should contain 3 field symbols, 2 state symbols, 4 transition rows with these source/target/event triples." No tree-walking assertions.
- **Catalog-driven architecture fit.** Catalog metadata flows into semantic inventories naturally — `OperationKind`, `FunctionKind`, `TypeKind` are properties of flat semantic entries, not tree-node annotations.

**Weaknesses:**
- **Expression representation is ambiguous.** The doc says "typed expressions" but doesn't specify their shape. Expressions have tree/DAG structure (a binary operation has two operands). A purely "flat" model can't represent expression structure. This is where the typed expression DAG sub-structure matters — expressions ARE structured, even in a semantic database.
- **Proof attribution requires care.** Without tree correspondence, the proof engine must work through source-origin handles to map obligations back to authored source. This is fine — the doc already mandates source-origin handles — but it's slightly more work than Option B's structural correspondence.
- **The name `SemanticIndex` doesn't communicate the internal structure.** It sounds like one monolithic thing. In practice it's multiple sub-structures (symbol table, expression graph, dependency facts) that different consumers read independently.

**Verdict:** Correct for Precept. This is what the doc actually describes in its content (not its title). It matches every downstream consumer's access pattern. The weakness around expression representation is real but solvable — expressions carry tree/DAG structure internally while the overall model is organized by semantic role, not by source nesting.

### Option D — Mutable Staged Model

**How it works:** Parser builds an initial object graph. Type checker, graph analyzer, and proof engine mutate or enrich it in place, or it evolves through phases (like Dhall's `Expr s a` parameterized by phase).

**Research grounding:** OPA/Rego is the canonical DSL-scale example — `ast.Compiler` mutates the original `Module` AST through multiple passes (rule indexing, type checking, safety checking, graph analysis). Kotlin K2's FIR is mutated in resolution phases. Swift's `ASTContext` is mutated by the type checker. Go's `types.Info` is caller-allocated mutable maps. The compilation-result-type survey explicitly notes that mutable compilation is NOT the minority pattern — OPA, Kotlin, Swift, Go, Dafny/Boogie all mutate.

**Strengths:**
- **Simplest to implement at the smallest scale.** One data structure, enriched through stages. No separate artifact boundaries. This is genuinely the easiest thing to build when you have one developer and a small DSL.
- **No mapping between artifacts.** Source-origin handles are unnecessary — you're still working with the original syntax nodes, just enriched.
- **Phase-typed parameterization (Dhall pattern) gives some safety.** `Model<Parsed>` vs `Model<Typed>` vs `Model<Analyzed>` could enforce at the type level which enrichments have happened.

**Weaknesses for Precept:**
- **Destroys the LS concurrency model.** The doc's atomic swap via `Interlocked.Exchange` depends on deep immutability. A mutable model means either: (a) locks everywhere (fragile, error-prone), or (b) defensive copying on every swap (wasteful and defeats the purpose). The language-server-integration survey confirms: Roslyn's immutable snapshots are explicitly designed for concurrent LS access. OPA's mutable model works because Regal (its LS) does a full re-parse/re-check per request anyway — but Precept already commits to the same full-recompile model, and immutable snapshots make that swap safe.
- **Testing is harder.** You can't test "what the type checker produced" independently from "what the parser produced" because they're the same mutable object. Regression assertions against mutable state are brittle.
- **Violates catalog-driven architecture.** If stages mutate a shared model, each stage's mutations are invisible at the type level — you can't tell from the artifact's type which enrichments have been applied. Phase parameterization helps but adds type-system complexity.
- **The doc's anti-mirroring rules are unenforceable.** If the model is mutated in place, downstream consumers (graph, proof, lowering) are working with the parser's structure because that's what the model IS — enriched, but still structurally parser-shaped.
- **Every contributor is one mutation away from corrupting shared state.** This is the #1 long-term maintenance risk. A well-meaning contributor adds a field to a syntax node that a later stage reads, creating invisible coupling.

**Verdict:** Wrong for Precept. The LS concurrency model alone kills it. The research shows mutable staged models work in systems that either (a) don't have concurrent LS access requirements, or (b) use locks/copying to compensate. Precept's design explicitly bets on immutability for correctness, and the research validates that bet.

---

## 3. Research Findings That Genuinely Matter

**Finding 1: The DSL-scale consensus is annotated/mutated single tree — but every DSL-scale system with good LS tooling regrets or compensates for it.**
- OPA/Regal: full recompile per request compensates for mutable model.
- CEL: side-table approach (`type_map`, `reference_map`) is the cleanest version of Option A, but CEL has no LS and no downstream analysis stages.
- Dhall: phase parameterization is clever but Dhall's LS is minimal.
- The systems with the best LS tooling (Roslyn, TypeScript, rust-analyzer) all separate syntax from semantics.

**Finding 2: Proof attribution in the surveyed systems (SPARK/GNATprove, Frama-C, Dafny, CBMC) universally uses source-location handles, not tree-structural correspondence.**
- GNATprove attributes to `file:line:col`.
- Frama-C attributes to ACSL annotation locations.
- Dafny attributes to method/assertion locations.
- None of them require a second tree for attribution. Source-origin handles are sufficient.

**Finding 3: Graph analysis in the surveyed systems (SPIN/Promela, Alloy, NuSMV, XState) uniformly operates on normalized state/transition inventories, not syntax trees.**
- SPIN builds a state-space from flat process/channel definitions.
- XState's `@xstate/graph` computes from normalized machine config.
- All want flat adjacency/reachability structures.

**Finding 4: Builder-style stages in the surveyed systems transform semantic inventories into execution-optimized structures.**
- CEL: `Interpretable` tree is not the checked AST — it's a restructured evaluation tree.
- OPA: rule indexes restructure policy for top-down evaluation.
- XState v5: normalized internal model with precomputed transition maps.
- All confirm: builder-style stages read semantic inventories, not syntax nesting.

**Finding 5: Immutability for LS concurrency is validated by Roslyn, contested at DSL scale.**
- The compilation-result-type survey shows OPA, Kotlin, Swift, Go all use mutable results.
- But Roslyn (the gold standard for LS integration) and CEL/Dhall/CUE/Pkl (the immutable DSL-scale minority) are the ones with the cleanest concurrent-access stories.
- Precept's decision to be immutable aligns with the LS-quality end of the spectrum, not the DSL-scale median.

---

## 4. Downstream-Consumer Analysis

### Language Server

| Feature | Needs | Best served by |
|---------|-------|----------------|
| Folding, outline | Syntax structure, declaration spans | `SyntaxTree` — Options A/B/C/D all work |
| Error recovery display | Missing nodes, error nodes | `SyntaxTree` — only Option D entangles this |
| Hover | Symbol at position + type + catalog docs | Semantic symbol table with source-origin handles (Option C) |
| Go-to-definition | Reference binding → declaration-origin | Semantic reference bindings (Option C) |
| Completions | Scope-visible symbols + expected type | Semantic symbol table + `SyntaxTree` for parse context (Option C) |
| Semantic tokens | Symbol classification at source spans | Semantic bindings with source-origin spans (Option C) |
| Atomic snapshot swap | Deep immutability | Option C (immutable record) or Option A (immutable + side tables). Option D fails. |

**LS verdict:** Option C is best. The LS needs both syntax (for structural features) and semantic (for intelligence features) as independent queryable artifacts. Option B adds tree structure the LS doesn't need. Option A entangles them. Option D breaks concurrency.

### Graph Analysis

Graph analysis wants: all states, all events, all transition rows as flat inventories with resolved semantic identity. It does NOT want: tree nesting, syntax positions, recovery shape.

**Graph verdict:** Option C is the only option that naturally provides flat semantic inventories. Options A and B force the graph analyzer to traverse tree structure to extract the same flat facts. Option D would work (the graph analyzer just reads from the mutated model) but with coupling risk.

### Proof Engine

Proof wants: flat inventory of proof obligations, each with a semantic site reference, an originating `ProofRequirement` from catalog metadata, and a source-origin handle for attribution. Proof does NOT want: tree structure for obligation instantiation.

**But:** Expressions within obligations DO have structure (a binary operation has operands). The proof engine needs to walk expression structure to apply strategies like guard-in-path and flow narrowing.

**Proof verdict:** Option C, provided the typed expression sub-structure preserves expression DAG shape. The proof engine reads flat obligation inventories but walks expression DAGs within obligations. Option B would give tree-shaped expressions naturally but adds unnecessary tree structure at the declaration level.

### Precept Builder / Emitter

The Precept Builder stage wants: semantic inventories organized by activation anchor, descriptor tables by identity, execution plans by transition row. It is a restructuring transformation — the output shape (dispatch indexes, constraint plan buckets, slot layouts) bears no structural resemblance to either syntax or semantic input shape.

**Precept Builder verdict:** Option C. The Precept Builder stage reads from flat inventories and restructures. Tree structure (Options A/B) adds nothing — the builder would just extract flat inventories from the tree before restructuring.

### Runtime / Evaluator

The runtime receives only lowered artifacts — it never sees compile-time structures. The question is: which compile-time artifact strategy produces the best lowered outputs?

**Runtime verdict:** Option C produces the cleanest inputs for lowering, which produces the cleanest runtime structures. The runtime is agnostic to compile-time artifact shape — it only sees `Precept`'s lowered descriptors and indexes.

### Catalog-Driven Architecture

The catalog architecture says: domain knowledge is metadata; pipeline stages derive from metadata; no parallel copies.

Option C fits best: semantic inventories are organized by catalog-derived semantic identity (e.g., `OperationKind`, `ConstraintActivation`), not by syntax structure. Catalog metadata flows into semantic entries as resolved kind properties.

Option A risks: consumers switching on syntax node kinds to determine catalog metadata, rather than reading pre-resolved semantic identity.
Option D risks: stages hardcoding knowledge into mutable properties rather than reading from catalog-derived metadata.

---

## 5. Implementation Consequences for Precept

**The "two-tree" label should be corrected.** The doc calls the architecture "two-tree" (§6 title) but describes a syntax tree + semantic database. This is confusing and has led to different conversations arguing different positions. The architecture IS two artifacts — `SyntaxTree` and `SemanticIndex` — but `SemanticIndex` is not a tree. The section title should say "two-artifact architecture" or "syntax/semantic separation."

**`SemanticIndex` should expose named sub-structures.** The current doc treats `SemanticIndex` as a flat bag. In practice it has three natural sub-structures:
- **Symbols** — declaration symbols, reference bindings, scope structure
- **Expressions** — typed expression DAGs with resolved operations/functions/types
- **Facts** — dependency facts, constraint-to-field influence, arg dependencies

Exposing these as named sub-structures (not separate artifacts) helps consumers navigate and helps implementers understand what they're building.

**Typed expressions are DAGs, not trees and not flat.** This is the one place where "flat semantic database" is wrong. `set Amount to RequestedAmount * (1 + TaxRate)` has DAG structure — the binary operations, field references, and literal constants form a directed graph. The `SemanticIndex` must represent this structure. A flat table of "expression entries" can't capture operand relationships. The doc should acknowledge that expressions carry structure (DAG-shaped) even though the overall model is organized by semantic role (flat inventories of symbols, declarations, constraints).

**Source-origin handles are the attribution mechanism.** The proof engine, LS, and MCP tools all need to map semantic facts back to authored source. Tree-structural correspondence (Option B) would provide this automatically. Since we're going with Option C, source-origin handles on every semantic entry must be first-class and well-specified. This is already in the doc but should be elevated from a bullet point to a hard contract.

**Testing is straightforward.** Assert against the three sub-structures independently:
- "SemanticIndex should contain these field symbols with these types"
- "SemanticIndex should contain these typed expressions with these operation kinds"
- "SemanticIndex should contain these dependency edges"

No tree-walking, no structural matching — just flat inventory assertions with FluentAssertions.

**Risk: `SemanticIndex` growing into an unfocused grab-bag.** The doc should set a clear boundary: `SemanticIndex` contains what the type checker produces. Graph facts live in `StateGraph`. Proof facts live in `ProofLedger`. Dependency facts that require graph analysis (reachability-derived) do NOT live in `SemanticIndex` — they live in `StateGraph`. Only dependency facts derivable from semantic analysis alone (computed-field references, constraint-to-field edges) live in `SemanticIndex`.

---

## 6. Recommended Vocabulary

| Current doc term | Problem | Recommended |
|---|---|---|
| "Two-tree architecture" | `SemanticIndex` is not a tree | **"Two-artifact architecture"** or **"Syntax/semantic separation"** |
| `SemanticIndex` | Sounds monolithic; hides sub-structure | Keep `SemanticIndex` as the type name, but document its sub-structures: `Symbols`, `Expressions`, `Dependencies` |
| "semantic database" | Slightly misleading — expressions have structure | **"Semantic model"** — organized by semantic role, with structured expressions internally |
| "parallel trees" | Only one is a tree | **"Parallel artifacts"** — `SyntaxTree` (tree-shaped) and `SemanticIndex` (inventory-shaped with expression DAGs) |
| "typed layer" | Vague | **"Semantic layer"** or just **"SemanticIndex"** |

The term `SemanticIndex` itself is fine as a C# type name. It's concise, unambiguous in code, and communicates the artifact's role more honestly than the older `TypedModel` wording. The vocabulary problem was in the design doc's framing, not in the underlying architecture.

---

## 7. If Changing Course Now Would Help, Say So Plainly

**The architecture is correct. The vocabulary is wrong.**

The current doc describes the right artifact strategy (Option C) but frames it with the wrong vocabulary ("two-tree," "parallel trees") and fails to decompose `SemanticIndex` into its natural sub-structures. This has caused confusion across conversations because the title implies Option B while the content describes Option C.

**Specific changes needed:**

1. **Rename §6 from "The two-tree architecture" to "The two-artifact architecture" or "Syntax and semantic separation."** The architecture is two artifacts, not two trees. One is a tree; one is a semantic model.

2. **Add a sub-section decomposing `SemanticIndex`'s internal structure.** Three named facets: symbols/declarations, typed expression DAGs, and dependency facts. This doesn't change the C# type — it clarifies what's inside it.

3. **Acknowledge that expressions carry DAG structure.** The doc's "flat semantic database" framing is 95% right but wrong about expressions. Expressions are structured. The model is flat at the declaration/inventory level but structured at the expression level.

4. **Elevate source-origin handles to a hard contract.** Not a bullet point — a boxed requirement. Every semantic entry in `SemanticIndex` carries a `SourceOrigin` that maps back to authored source spans. This is what makes proof attribution, LS hover, and go-to-definition work without tree-structural correspondence.

5. **This recommendation was later superseded by the repo-wide rename.** The current C# types now use `SyntaxTree`, `SemanticIndex`, `StateGraph`, `ProofLedger`, and `Compilation`; the core architectural point still stands that the main fix was naming and documentation clarity, not a different artifact strategy.

**The old v1 framing was not better.** If it used "annotated syntax" or "single tree," that was wrong in the other direction. The current architecture is right — it just needs vocabulary that accurately describes what it is.

**No hybrid (Option E) is needed.** The hybrid that would be tempting — syntax tree + normalized semantic inventories + typed expression DAG — is exactly what Option C already is, if you decompose `SemanticIndex` honestly. There is no fifth option hiding here. There's just an Option C that needs to be described with more precision.
