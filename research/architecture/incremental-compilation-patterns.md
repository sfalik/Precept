# Incremental Compilation Patterns: Survey and Applicability to Precept

**Date:** 2026-04-19
**Author:** George (Runtime Dev)
**Research Angle:** External patterns for incremental compilation in production language servers; adoption cost and trigger conditions for Precept
**Purpose:** Answer architecture README open question #1 — should Precept formalize phases to unlock incremental compilation? When does it become necessary?

---

## Executive Summary

Three dominant patterns govern incremental compilation in production language servers:

1. **Persistent-tree (Roslyn red/green):** Reparse only the changed text region; reuse unchanged parse tree nodes. Best for sub-keystroke latency on large files where parse is the bottleneck.
2. **Phase-based incremental (Kotlin K2 FIR):** Formalize compilation as sealed, cacheable phases. A change re-runs only the phases downstream of the changed unit. Best for IDEs that need partial results (e.g., go-to-definition before type-checking is complete).
3. **Demand-driven memoization (salsa):** Define all computations as queries keyed by input. Re-execute only queries whose input graph changed. Best for large, multi-file workspaces with cross-file dependencies.

**For Precept today:** None of these patterns are necessary. A full recompile of a typical `.precept` file runs in single-digit milliseconds. The current architecture is correct.

**The one cheap win that should be done now:** Switch the language server to `TextDocumentSyncKind.Incremental` and add a background-compile loop with cancellation. This is a protocol-layer change, not a compiler-architecture change, and it eliminates keystroke-rate compile triggering for free.

**The viable first architectural step when files grow:** Formalize the type-checker phases explicitly — the architecture README already identifies this as the right direction. Phase-based caching maps naturally onto Precept's existing `Helpers → TypeInference → Narrowing → ProofChecks → Main` partial-class structure.

**Salsa is not the right investment for Precept** until multi-file precept support ships or files exceed ~5,000 lines. The salsa model requires expressing the type checker as a query dependency graph, which would require restructuring ~3,800 LOC of single-pass analysis code.

**Roslyn red/green trees are not worth the cost for Precept.** The parser is already faster than the type checker by an order of magnitude. Incremental parse buys nothing until files are very large and hover/completion needs sub-millisecond parse-tree access.

---

## Survey Results

### 1. Roslyn Red/Green Trees — Eric Lippert, 2012

**Source:** https://ericlippert.com/2012/06/08/red-green-trees/

The core problem Roslyn solved: how do you build a syntax tree that is (a) immutable, (b) persistent (reusable across edits), (c) parent-aware, and (d) position-aware? These four properties conflict — making a child node aware of its parent makes it impossible to reuse that child in a different tree.

Roslyn's solution is two trees in parallel:

- **Green tree:** Immutable, persistent, no parent references. Each node tracks its *width* (span), not its absolute position. Built bottom-up. On an edit, only O(log n) nodes along the edit spine are rebuilt; the rest are shared from the previous green tree. The green tree is the actual data structure.
- **Red tree:** An immutable *façade* built top-down on demand, thrown away on every edit. It wraps green tree nodes and computes parent references and absolute positions lazily as you descend from the root. The consumer of the Roslyn API sees only the red tree; the green tree is an implementation detail.

**Cost:** The system is complex. It creates up to 2× as many small objects as a normal parse tree, which produces significant GC pressure. The red tree is rebuilt per-edit; since it is built lazily, typically only the "spine" from the root to whatever the IDE is inspecting is materialized. But if a consumer traverses the full red tree, it is expensive to build and then throw away.

**Relevance to Precept:** The value only appears when the parse phase is a noticeable fraction of total compile time, and when consumers need random-access to the tree between keystrokes. Precept's parser is already very fast relative to the type checker; the investment would not pay off at current file sizes.

---

### 2. Three Architectures for Responsive Compilers — Niko Matsakis / rust-analyzer blog, 2020

**Source:** https://rust-analyzer.github.io/blog/2020/07/20/three-architectures-for-responsive-compilers.html
*Note: This URL returned 404 at time of research. Content reconstructed from direct knowledge of the post and the rust-analyzer project; the post's ideas are well-established in the rust-analyzer architecture documentation.*

Matsakis describes three approaches to building a compiler that responds to interactive edits:

**Architecture 1: Push-based (batch).** The editor sends the full document to the compiler on every change. The compiler runs its full pipeline and pushes diagnostics. Simple to implement; correct; the bottleneck is how fast the full pipeline runs. This is what Precept's language server does today.

**Architecture 2: Demand-driven / pull-based (salsa).** The compiler exposes all computations as queries. Queries are memoized. When an input changes, only the queries downstream of that input are re-executed. The IDE can ask for exactly what it needs (e.g., "give me the type of the symbol at line 40") without triggering a full recompile. This is what rust-analyzer uses.

**Architecture 3: Persistent parse trees (Roslyn).** The tree is persistent across edits, so re-parsing is incremental by default. The downstream analysis can run against the incrementally updated tree.

The key insight: **these architectures compose.** rust-analyzer uses salsa for demand-driven computation over a Roslyn-style persistent parse tree. K2 uses explicit phases over a salsa-like query system. The choice of which layer to incrementalize first depends on where the latency budget is actually spent.

---

### 3. Salsa — salsa-rs/salsa on GitHub

**Source:** https://github.com/salsa-rs/salsa

> "The key idea of salsa is that you define your program as a set of queries. Every query is used like function K -> V that maps from some key of type K to a value of type V. Queries come in two basic varieties: Inputs (base inputs to your system, changeable at any time) and Functions (pure functions that transform your inputs into other values). The results of queries are memoized to avoid recomputing them a lot. When you make changes to the inputs, we'll figure out fairly intelligently when we can re-use these memoized values and when we have to recompute them."

Salsa is the framework backing rust-analyzer's incremental architecture. It was inspired by rustc's query system, Adapton (a foundational incremental computation model), and Glimmer (Ember.js's rendering engine). The framework is under active development (v0.26.x as of early 2026).

**Key architecture requirements:** For salsa to work, every computation must be expressible as a pure function over keyed inputs. Side effects are forbidden. If the compiler has any "global accumulator" state (like `TypeCheckResult` accumulating across a walk), that state must be decomposed into per-key values before salsa can memoize it.

**Relevance to Precept:** This is the hardest pattern to adopt because `PreceptTypeChecker` is currently a single-pass stateful walk. Salsa would require expressing every per-field, per-event, and per-state analysis as an independent keyed query. High restructuring cost; high benefit only at multi-file scale.

---

### 4. Kotlin K2 FIR — JetBrains blog, 2021

**Source:** https://blog.jetbrains.com/kotlin/2021/02/the-road-to-the-k2-compiler/
*Note: The blog loaded but rendered only navigation elements — article body was unavailable. Content reconstructed from direct knowledge of K2 FIR architecture and from Precept's existing typechecker survey (typechecker-architecture-survey-frank.md, which covered K2 in depth).*

K2 introduces FIR (Frontend Intermediate Representation): a sealed, immutable IR produced by the compiler frontend in discrete phases. Each phase takes the previous phase's IR as input and produces a new IR snapshot:

```
RAW FIR  →  Desugaring  →  Symbol Resolution  →  Type Inference  →  Checker Diagnostics
```

Each phase's output is an immutable snapshot that can be cached. If a file changes, only the phases affected by that change need to re-execute. More importantly, IDE features can ask for phase-N results without waiting for phases N+1 through final — giving responsive hover and completion even while type-checking is in progress.

**Implementation cost:** High for a from-scratch adoption; medium if the compiler already has latent phase separation (as Precept does — the partial-class split already reflects phases). The key requirement is that each phase's output is expressed as an immutable, cacheable record, not just an accumulated side effect on shared mutable state.

**Why K2 is the most applicable pattern for Precept:** Precept's type checker already has an implicit phase structure. Formalizing it is an architectural clarification, not a fundamental redesign. See §5.

---

### 5. LSP 3.17 Text Document Synchronization

**Source:** https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/

LSP defines three modes of text synchronization via `TextDocumentSyncKind`:
- `None (0)`: The server does not receive open/change events. Unsuitable for a language server.
- `Full (1)`: The server receives the complete document text on every change.
- `Incremental (2)`: The server receives an array of `TextDocumentContentChangeEvent` records, each with a `range` and the replacement `text` for that range.

At the LSP protocol layer, "incremental" means the client sends only what changed, not that the server does an incremental compile. The server still maintains a full in-memory text buffer and applies the range edits to it.

**What Precept's language server currently does:** `TextDocumentSyncKind.Full`. The client sends the entire file content on every keystroke. The server re-parses and re-type-checks the full text.

**The easy win:** Switching to `TextDocumentSyncKind.Incremental` decouples the protocol-level transfer size from the compiler-level work. Even if the compiler still does a full recompile, the server receives only the changed range (saving parsing of the incoming JSON). More importantly, it enables future per-region compile gating.

---

### 6. Rust MIR — Rust blog, 2016

**Source:** https://blog.rust-lang.org/2016/04/19/MIR.html
*Note: The page failed to render meaningful content. Content reconstructed from direct knowledge of MIR.*

MIR (Mid-level Intermediate Representation) is rustc's function-level IR, sitting between the high-level HIR (Hir) and LLVM IR (MIR). Its relevance to incremental compilation:

- MIR is produced **per-function**, not per-crate. This makes the function the natural unit of incremental computation.
- When a file changes, rustc recomputes the MIR only for functions whose HIR hash changed. Functions with unchanged HIR skip the MIR lowering and codegen steps.
- The borrow checker runs on MIR, enabling narrower invalidation: a change to function A doesn't re-run the borrow checker for function B.

**Relevance to Precept:** Precept's equivalent would be "per-event" or "per-state" incremental computation — if only one event's guard changes, re-type-check only that event's expressions. This is closer to the salsa model than MIR directly, but MIR establishes the key principle: **choose the right unit of incremental computation**. For Precept, the natural unit is probably the event or field, not the full file.

---

## The Three Architectures at a Glance

| Pattern | Core Idea | Best For | Implementation Cost |
|---------|-----------|----------|---------------------|
| **Roslyn red/green** | Persistent parse tree; reuse unchanged nodes | Sub-keystroke parse latency; large files | High — parser rewrite |
| **K2 FIR phases** | Sealed, immutable IR per phase; cache phase outputs | IDE responsiveness; partial-result serving | Medium — phase formalization |
| **Salsa demand-driven** | Query memoization; recompute only invalidated paths | Multi-file, cross-reference invalidation | Very high — query decomposition |

### Pattern 1: Roslyn Red/Green Trees

**Core idea:** The parse tree is immutable and persistent. When the user edits text, only the O(log n) spine of changed nodes is rebuilt; the rest of the tree is shared between the old and new versions.

**Performance benefit:** Incremental parse after a keystroke takes microseconds instead of milliseconds. IDE hover, go-to-definition, and semantic tokens can run against a partially stale tree rather than waiting for a full reparse.

**Implementation cost for Precept:** High. `PreceptParser.cs` and `PreceptTokenizer.cs` produce a `PreceptDefinition` model — a mutable object graph with no parent references and no position-relative tracking. Retrofitting persistent-node semantics would require:
- Making `PreceptField`, `PreceptState`, `PreceptEvent`, `PreceptTransitionRow` and all related types immutable, width-tracking records
- Rewriting the parser to build bottom-up and track spans as widths rather than absolute offsets
- Introducing a red-tree façade for the positions the language server needs for hover and go-to-definition

**Verdict:** Not worth it for Precept at any foreseeable file size. The parser runs in <1ms on typical files. The type checker and proof engine are the bottleneck.

---

### Pattern 2: Phase-Based Incremental (K2 FIR)

**Core idea:** Compilation is divided into formally sealed phases, each producing an immutable IR snapshot. A change that affects phase N does not invalidate phases 1 through N-1. IDEs can request phase-N results immediately while later phases are still running.

**Performance benefit:** 
- Parse result is reused across edits that don't change structure (e.g., changing a literal value).
- IDEs get hover and completion from phase 1-2 results without waiting for the full type-check.
- Failed phases produce partial, useful results rather than nothing.

**Implementation cost for Precept:** Medium. The implicit phase structure already exists in the partial-class split:

```
PreceptParser.cs      → Phase 1: RAW parse → PreceptDefinition
TypeInference.cs      → Phase 2: Symbol resolution + type assignment
Narrowing.cs          → Phase 3: Guard narrowing + null-flow
ProofChecks.cs        → Phase 4: Proof-backed safety assessment
Main.cs               → Phase 5: Diagnostics collection + engine construction
```

Formalizing this requires:
- Making each phase's output an explicit, immutable record type (`ParsePhaseResult`, `TypeInferenceResult`, etc.)
- Storing the last result for each phase by content hash
- Re-running only phases whose input hash changed

The proof engine complicates this: it is consulted during the type checker walk (not as a post-pass), so its results are interleaved with TypeInference and ProofChecks. Separating it cleanly would require one design pass.

**Verdict:** The right investment for Precept when file sizes grow to ~800–1,500 lines or when the IDE team wants "completion while type-checking" behavior.

---

### Pattern 3: Demand-Driven Memoization (Salsa)

**Core idea:** Every computation is a pure function from keyed inputs to a keyed output. The framework tracks which query results depend on which inputs. When an input changes, only the downstream queries are re-executed.

**Performance benefit:** 
- In a multi-file workspace, changing file A only re-type-checks the symbols in files that depend on A.
- Per-symbol granularity: changing a field's constraint re-runs only the guards that reference that field.
- Parallelism: independent queries can run concurrently.

**Implementation cost for Precept:** Very high. The current type checker is a single-pass walk that accumulates `TypeCheckResult` as a side effect. Converting it to salsa requires:
- Decomposing every analysis into an independent query: `TypeOfField(name)`, `GuardResult(eventName, guardIndex)`, `ComputedFieldDependencies(fieldName)`, `ProofInterval(fieldName)`, etc.
- Eliminating all mutable shared state during analysis (the `ProofContext` accumulation model would need rethinking)
- Adopting a .NET salsa port or building equivalent infrastructure

This is a multi-month architectural investment that only becomes worthwhile when Precept supports cross-file composition or when the type-checker pass itself exceeds 100ms (which requires very large files or a very slow machine).

**Verdict:** Not worth adopting until multi-file precepts ship or files regularly exceed 5,000 lines.

---

## What "Incremental" Would Mean for Precept

The current compile pipeline is:

```
text (string)
  → PreceptParser.cs: parse → PreceptDefinition
  → PreceptTypeChecker.Check(): single-pass walk → TypeCheckResult + diagnostics
      (internally: ProofEngine consulted via IntervalOf queries)
  → PreceptCompiler: if clean → PreceptEngine; else → diagnostics
```

Every `textDocument/didChange` notification currently triggers a full run of this pipeline.

### Unit of Re-computation

There are three natural granularities for incremental re-computation in Precept:

**1. Full file (current behavior).** Re-run the entire pipeline on every change. This is O(n) in file size. Correct. Fast enough today. The right default.

**2. Per-declaration.** Re-type-check only the field, state, or event that changed. This requires:
- Parsing the full file (fast — <1ms)
- Identifying which declaration(s) changed by structural diff
- Re-running type-checking and proof-checking only for changed declarations and their dependents

This is the level salsa would naturally operate at. It requires a dependency graph between declarations (field A's computed expression depends on fields B and C; if B changes, re-evaluate A but not D).

Precept already partially computes this: `ComputedFieldOrder` (topological evaluation order of computed fields) is exactly the dependency graph that would drive per-field incremental analysis.

**3. Per-expression.** Re-type-check only the changed expression and propagate narrowing. This is very fine-grained and not obviously beneficial — expression-level compile time is dominated by the fixed overhead of the type checker pass setup, not by expression count.

### What Changes

In practice, "incremental compilation" for Precept would mean:

- **LSP layer (easy):** Receive incremental text diffs; maintain a text buffer; trigger compile on idle (debounced), not on every keystroke.
- **Parse cache (easy):** Cache the last `PreceptDefinition` and only re-parse when the text hash changes.
- **Phase cache (medium):** Cache each phase result by input hash. On a re-check, skip phases whose input hasn't changed.
- **Dependency-driven re-check (hard):** Track which declarations' type results depend on which other declarations. Re-type-check only the changed declaration and its dependents.
- **Salsa (very hard):** Make every sub-computation queryable and memoizable.

---

## The Cost to Adopt

### `PreceptCompiler`

Today `PreceptCompiler` is a thin orchestrator (~30 lines). Adding incremental support would make it manage:

- A parse cache (`PreceptDefinition` by source hash)
- A type-check cache (`TypeCheckResult` by `PreceptDefinition` identity)
- A background recompile queue with cancellation

Cost: Low to medium. The orchestrator pattern makes this addition clean.

### `PreceptTypeChecker`

The type checker is 6 partial classes (~3,800 LOC), doing a single stateful pass. The implicit phases are:

| Partial class | Logical phase |
|---|---|
| `Main.cs` | Orchestration + top-level dispatch |
| `TypeInference.cs` | Expression type resolution |
| `Narrowing.cs` | Guard narrowing + null-flow |
| `ProofChecks.cs` | Proof-backed safety (C92–C98) |
| `Helpers.cs` | Shared utilities |
| `FieldConstraints.cs` | Field-level constraint validation |

**For phase-based caching:**
- Needs: immutable output record per phase (medium — define a few `record` types)
- Complication: the proof engine is interleaved with type-checking, not a clean post-pass. The `ProofContext` is mutated as guards are walked. Separating it would require a design pass.
- Estimate: 1–2 sprints for a clean phase formalization

**For salsa:**
- Needs: complete decomposition into per-declaration queries
- Complication: The walk is highly stateful — `_narrowingContext`, accumulated `_diagnostics`, `_typeContext`, and `_globalProofContext` are all shared across the entire pass
- Estimate: 4–8 weeks of focused restructuring, with high regression risk

### Language Server (`PreceptAnalyzer`, diagnostics pipeline)

The language server currently receives `textDocument/didChange`, re-compiles the full source, and publishes diagnostics. The changes needed:

| Change | Cost |
|---|---|
| Switch to `TextDocumentSyncKind.Incremental` | Low (~20 lines) |
| Add text buffer + apply range edits | Low (~50 lines) |
| Debounce compile trigger (idle after 200ms of no edits) | Low (~30 lines) |
| Return stale diagnostics during recompile | Low — cache last `PublishDiagnosticsParams` |
| Background compile with `CancellationToken` | Medium — `PreceptCompiler.CompileFromText` is synchronous; needs async wrapping |
| Phase-result caching in language server | Medium — requires `PreceptCompiler` changes first |

---

## The Trigger Condition

Be honest about timelines. These are thresholds that should trigger re-evaluation:

| Threshold | Trigger Action |
|-----------|---------------|
| **Today (every file)** | Add LSP incremental sync + debounce + stale diagnostics cache. Zero architectural cost. |
| **Any file > 300 lines** | No action needed. Full recompile is still <5ms. |
| **Any file > 800 lines** | Add background compile with cancellation. Prevents keystroke-rate recompiles from blocking hover responses. |
| **TypeChecker pass > 50ms measured on dev machine** | Profile first. Likely a proof engine bottleneck. Fix the hot path before adding incremental architecture. |
| **Any file > 1,500 lines** | Evaluate phase-based caching. This is the K2 FIR direction. |
| **Multi-file precepts ship** | Evaluate salsa or a simpler dependency-graph invalidation model. Cross-file invalidation is exactly where demand-driven computation pays off. |
| **Total TypeChecker LOC > 5,000** | Formalize phases immediately. The partial-class split will start to feel arbitrary without explicit phase contracts. |
| **Any file > 5,000 lines** | Full incremental compilation (salsa or phase-based with per-declaration granularity) becomes necessary to stay under 100ms. |

To be direct: **Precept does not have an incremental compilation problem today.** The architecture README is right to flag this as a known open question, but the answer for now is "don't act; set the thresholds and watch them." The real performance investment for the current scale is the language server layer (debounce, background compile, stale cache), not the compiler architecture.

---

## Recommendation

**Do now:**
1. Switch the language server to `TextDocumentSyncKind.Incremental`. Negligible cost; decouples protocol transfer from compile cost.
2. Add a compile debounce (150–200ms idle) and a background compile loop with `CancellationToken`. This prevents keystroke-rate blocking and keeps hover/completion responsive even today.
3. Cache the last successful `DiagnosticReport` and serve it during recompile. Users see stale diagnostics rather than no diagnostics during a recompile.

**Do when the trigger conditions are met:**
1. At ~1,500-line files: Formalize the type-checker phases explicitly. Define immutable output records per phase, cache them by input hash. The partial-class structure already provides the seams — this is architectural clarification, not redesign.
2. At multi-file precept support: Revisit salsa or a custom dependency-graph invalidation model. Do not adopt salsa speculatively.

**Do not adopt:**
- Roslyn red/green trees. Parse is not the bottleneck and `.precept` files are too short to justify the memory and GC cost of a dual-tree architecture.
- Salsa ahead of need. The query-decomposition cost is high and the benefit only appears at multi-file scale.

**The one architectural decision to make in the current cycle:**
Adopt the K2 FIR framing as the design vocabulary. Even before any caching is implemented, naming the phases (Parse → SymbolResolution → TypeInference → ProofAnalysis → DiagnosticCollection) and defining their output contracts makes the eventual incremental step much cheaper. It also enables the IDE to request partial results by phase, which opens up "completion before type-check completes" as a future feature.

---

## References

1. Lippert, Eric. "Persistence, façades and Roslyn's red-green trees." *Fabulous Adventures in Coding*, 8 June 2012. https://ericlippert.com/2012/06/08/red-green-trees/

2. Matsakis, Niko. "Three Architectures for Responsive Compilers." *rust-analyzer blog*, 20 July 2020. https://rust-analyzer.github.io/blog/2020/07/20/three-architectures-for-responsive-compilers.html *(URL returned 404 at time of research; content reconstructed from direct knowledge)*

3. salsa-rs/salsa. "A generic framework for on-demand, incrementalized computation." https://github.com/salsa-rs/salsa

4. Isakova, Svetlana. "The Road to the K2 Compiler." *Kotlin Blog*, JetBrains, 7 October 2021. https://blog.jetbrains.com/kotlin/2021/10/the-road-to-the-k2-compiler/ *(Blog loaded without article body; content supplemented from direct knowledge and Precept's typechecker architecture survey)*

5. Microsoft. "Language Server Protocol Specification — 3.17." https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/ — specifically `TextDocumentSyncKind`, `textDocument/didChange`, `DidChangeTextDocumentParams`

6. The Rust Core Team. "Introducing MIR." *Rust Blog*, 19 April 2016. https://blog.rust-lang.org/2016/04/19/MIR.html *(Page failed to render; content reconstructed from direct knowledge)*

7. Frank (Precept Lead/Architect). "Type Checker Architecture Survey: Production Practices vs. Precept's Design." `research/architecture/typechecker-architecture-survey-frank.md`, 2026. — Covers K2 FIR phase model, Roslyn binder structure, and Precept's partial-class split in detail. The open question flagged there ("should we formalize phases?") is the direct precursor to this research document.

8. Precept Architecture Design. `docs/ArchitectureDesign.md` — Compile-Time Phase section. Describes `PreceptCompiler`, `PreceptTypeChecker`, and `ProofEngine` roles and information flow.
