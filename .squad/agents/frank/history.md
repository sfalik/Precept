## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable guidance for parser, catalog, type-checker, runtime, and tooling work.
- Durable active baseline: catalogs remain the language truth; generic consumer flow dispatches by metadata/shape instead of construct identity; CC#1 keeps a sealed typed-expression DU while the broader parser/runtime surface stays metadata-driven.
- CC#25 runtime baseline is now fixed for current planning: production Fire uses Option A + G (`PreceptValue` tagged value storage plus catalog-owned delegate arrays), while LS/MCP interactive tooling keeps traced tree-walk evaluation.
- TypeBuilder/source-generation paths remain recorded as analyzed alternatives, not the active SaaS architecture, unless deployment or inspectability constraints change.

- **Q2 resolved (2026-05-03):** Opcodes-in-CompilationResult was evaluated and rejected. The current architecture remains correct: opcodes stay in Precept.From() / the Precept executable model, CompilationResult stays analysis-only, and TypedExpression remains available to LS/MCP consumers. Shane accepted the decision on 2026-05-03.

- **Q1 resolved (2026-05-03):** Option B (`IEvaluatorTrace` hook) chosen for guard trace granularity. Shane confirmed 2026-05-03.

- **Opcodes-in-CompilationResult evaluation (2026-05-03):** Recommended NO — opcodes belong in the `Precept` executable model (built by `Precept.From()`), not in `Compilation`/`CompilationResult`. The canonical design already places opcode lowering in the Precept Builder stage. `Compilation` is the analysis snapshot for authoring surfaces; putting execution plans there conflates analysis with execution, wastes work on LS recompiles, and blurs the severance boundary. No architecture change needed — the current design is correct.

- **ReadJson/WriteJson API design (2026-05-03):** Closed the JSON ingress/egress seam from the Fire-call lifecycle walkthrough. Phase 8 egress: `FormatValue` is replaced by `WriteJson(Utf8JsonWriter, PreceptValue)` — zero boxing for scalars, ref-region types are already-heap references cast and written directly. Phase 1 ingress: `StoreValue`/`ParseValue` replaced by `ReadJson(ref Utf8JsonReader, ref PreceptValue)` — `ref Utf8JsonReader` required because it's a ref struct; `ref PreceptValue` for consistent write-back. Null handling is call-site-only (check `TokenType == Null` before dispatching). Collection runtimes own their structural loop; element-type runtime handles individual elements. Token-advance ownership: call site advances to value token, `ReadJson` calls `GetXxx()`, call site advances at next iteration. `TypeRuntimeMeta` final surface: `ReadJson`, `WriteJson`, `ParseString`, `FormatString`, `BinaryExecutors`, `UnaryExecutors`. `ExtractValue`/`StoreValue`/`ParseValue` eliminated from all hot paths.

- **CC#25 Fire data lifecycle walkthrough (2026-05-03):** Peak live footprint for one Fire under A+G is ~44-48 `PreceptValue` slots, total stack traffic is ~4,480 bytes, the working copy is the donated next-version slot array, and pooled arrays cut GC-visible allocation to the boundary objects. The remaining implementation questions are slot-array ownership transfer, eval-stack allocation strategy, JSON ingress/egress ownership, event-args representation, trace-path data structures, and multi-row working-copy pooling.
- **CC#25 final runtime recommendation (2026-05-03):** The real performance lever is representation, not dispatch. Replace boxed `object?` hot-path values with a 32-byte `PreceptValue` tagged struct and keep execution semantics on catalog-owned delegate arrays. `System.Linq.Expressions` stays an upgrade seam, not a v1 dual-path commitment.
- **CC#25 SaaS constraint resolution (2026-05-03):** TypeBuilder/source-generated CLR types only win under a different product shape. In the current SaaS, per-definition cold-start and loss of fine-grained inspectability outweigh warm-path throughput gains.
- Catalog schema diagram work (2026-05-03) produced a three-level visual section in `docs/language/catalog-system.md` with 13 catalogs in scope, `ConstructSlotKind` treated as support schema rather than a catalog, and Elaine owning the rendering while Frank owns the architectural message.
- LS enrichment features (did you mean? / code actions) require three catalog structure changes before LS implementation: `Diagnostic.Args`, `DiagnosticMeta.SuggestionSources`, and `ConstructMeta.ModifierDomain`; classification axes like `SuggestionSource` and `ModifierDomain` stay bare enums.
- The `tree` variable/type-name sweep confirmed the durable naming boundary: use `ConstructManifest` / `manifest` for the flat Precept parser artifact, while legitimate Roslyn `SyntaxTree`, parse-tree prose, and graph-theory tree language remain untouched.
- `docs/compiler-and-runtime-design.md` is the narrative overview layer over the canonical stage docs; it inherits open questions rather than silently resolving them, and `SemanticIndex` must stay framed as a flat semantic inventory rather than an annotated syntax tree.
- Gap-register deprecation (2026-05-03) is final: discovery registers were archived, unresolved gaps moved into canonical docs as Open Questions, and `docs/working/cross-cutting-decisions.md` is now the sequencing/ownership driver.
- **CC#1 resolved (2026-05-03):** `ParsedExpression` and `TypedExpression` are sealed DUs, the expression tree is the only strongly typed parser output layer, and exhaustiveness relies on sealed-hierarchy switches plus the annotation-bridge pattern for distributed dispatch.

### 2026-05-04T00:43:26Z — TypeRuntime-as-catalog analysis delivered
- Answered Shane's challenge: "Why not make TypeRuntime a full catalog?" — Answer: TypeRuntime is NOT a 14th catalog (it's not language surface), but it IS catalog-owned metadata.
- The correct shape: `TypeMeta` gains a `Runtime` property of type `TypeRuntime` (the abstract class with sealed subclasses). The abstract class hierarchy stays as the implementation shape, but it's owned by the catalog entry rather than maintained as a parallel array.
- Key distinction established: catalog DUs (like `ModifierMeta`) are metadata shapes consumers pattern-match on; implementation class hierarchies (like `TypeRuntime`) are behavioral implementations consumers call via virtual dispatch. TypeRuntime is the latter.
- Consumer access: `Types.GetMeta(kind).Runtime.WriteJson(...)` or via a derived `TypeRuntime[]` index for hot paths — derived from catalog, never a parallel copy.
- This aligns with the existing decision: "persistence behavior belongs on catalog metadata."

### 2026-05-04T00:27:39Z — Collections BCL-vs-Custom analysis delivered
- Revised position: BCL `System.Collections.Immutable` for all nine collection types. Seven direct, two via thin composition wrappers. Zero from-scratch persistent data structures at v1.
- Key finding: All four motivations for custom types (immutability, JSON round-trip, DSL accessors, persistent semantics for discard) are satisfied equally by BCL immutable types.
- Sortability solved via per-field `IComparer<PreceptValue>` built during `Precept.From()`, capturing TypeTag and direction modifier. Feed directly into `ImmutableSortedDictionary.Create()`.
- `PreceptValue` needs `IEquatable<PreceptValue>` + `GetHashCode()` for hash-based collections, but NOT `IComparable<PreceptValue>` (use per-field comparers instead).
- Risk reduction: from multi-month high-complexity custom data structures to days of thin wrapper work.
- Surfaced 3 new open questions: `ImmutableQueue` lacks O(1) `.Count` (need cached count wrapper), `~string` case-insensitive equality requires per-field `IEqualityComparer<PreceptValue>`, Bag zero-count cleanup is trivial wrapper logic.
- The existing `collection-types.md` already documents BCL backing for List (`ImmutableList<T>`) and QueueBy (`SortedDictionary<TPriority, Queue<TElement>>`), confirming the project's established BCL-first approach.

### 2026-05-04T00:15:36Z — CC#25 Collections + TypeRuntimeMeta Q&A delivered
- Answered Shane's two questions on collection backing types and TypeRuntimeMeta justification in `frank-collections-and-typemeta.md`.
- Collections: Precept-owned persistent immutable types (e.g., `ImmutableLog<PreceptValue>`) stored as heap refs in `slot.Ref`. Persistent semantics are non-negotiable for working-copy discard.
- TypeRuntimeMeta: design is correct; recommended rename to `TypeRuntime`, shape as abstract class + sealed subclasses. Defended against switch/delegate-struct/interface alternatives. It is behavioral catalog data, not scattered machinery.
- Surfaced 3 new open questions: composite element representation, builder pattern for ReadJson, element-type parameterization ordering.

### 2026-05-04T00:12:46Z — CC#25 Q2 boundary locked
- Frank-51's evaluation is now durable context: do not move opcodes into `CompilationResult`; keep lowering inside `Precept.From()` on the executable-model side of the boundary.
- Shane accepted the recommendation, and the public-analysis boundary stays unchanged: `Compilation` / `CompilationResult` remains an authoring snapshot while `TypedExpression` continues serving LS/MCP consumers.

### 2026-05-03T23:00:32Z — ReadJson / WriteJson API lock recorded
- Frank-48 closed the CC#25 JSON ingress/egress seam: ReadJson now owns typed value extraction, WriteJson owns symmetric egress, null handling stays at the call site, and collection runtimes own structural JSON loops.
- The locked TypeRuntimeMeta surface is ReadJson, WriteJson, ParseString, FormatString, BinaryExecutors, and UnaryExecutors, with ExtractValue and StoreValue / ParseValue kept out of Fire, Inspect, and Update hot paths.
### 2026-05-03T22:22:27Z — CC#25 corpus canonicalized
- Scribe merged 19 CC#25 inbox files into 7 durable ledger entries, deleted the processed inbox notes, and recorded the active runtime baseline as `PreceptValue` + catalog-owned delegate dispatch with TypeBuilder and lane-split alternatives explicitly closed.
- The Fire-call lifecycle walkthrough is now part of Frank's active context as the quantitative implementation baseline for A+G memory/ownership work.

### 2026-05-03T16:44:09Z — Gap-register deprecation and wave driver recorded
- Frank-38 restructured `docs/working/cross-cutting-decisions.md` into the wave-ordered execution driver (Waves 0-5, 26 decisions, ownership labels), archived the two working gap registers, and migrated their unresolved content into canonical docs as inline Open Questions.
- Durable baseline: separate gap registers are retired; new gaps belong directly in the relevant canonical doc, while sequencing and ownership routing now live in `docs/working/cross-cutting-decisions.md`.
- Specific closeout: missing gap #55 (`GraphEvent.IsInitial` derivation) was added to `docs/compiler/graph-analyzer.md`, and the deprecation rationale is now captured in the decision ledger.

### 2026-05-03T15:18:05Z — Catalog diagram baseline and ownership routing recorded
- Frank-34's research memo is now the durable baseline for schema-diagram work: the live catalog system is 13 catalogs because `ExpressionForms` is in scope, and `ConstructSlotKind` is supporting schema rather than a catalog.
- User routing directive updated: Elaine owns both Mermaid and ASCII diagram rendering. Frank remains the architectural analyst/decision source for what the diagrams should communicate.
- The because-clause ledger closeout is also recorded: grammar docs already match the separate `EnsureClause` + `BecauseClause` slot anatomy, and George's optional-slot follow-up closed the last catalog-red defect.


### 2026-05-04T00:52:48Z — TypeRuntime design locked
- CC#25 TypeRuntime architecture is now locked: TypeRuntime is catalog-owned metadata on TypeMeta, exposed as a Runtime property typed as the abstract TypeRuntime class with sealed subclasses.
- The separate TypeRuntimeMeta DU-through-Types variant is rejected. Keep one type catalog lookup, not parallel GetMeta / GetRuntime switches.
- Durable guidance: consumers call Types.GetMeta(kind).Runtime...; any indexed runtime table is derived from Types.All, never maintained as an independent source of truth.

### 2026-05-04T00:56:54Z — CC#25 slot vocabulary boundary locked
- Answered CC#25 Q1: parser-time construct slots and runtime field slots are different vocabularies with different owners and lifecycles.
- Durable boundary: `ParsedConstruct.Slots` / `SlotValue` stay compile-time only; runtime execution uses field slot indices in the `PreceptValue[]` working copy, with `SlotLayout` as the field-name-to-slot-index map built in `Precept.From()`.
- Shane accepted the answer; when discussion crosses parser and runtime layers, say **construct slots** vs **field slots** explicitly.

### 2026-05-03T21:12:30-04:00 — CC#25 Q2 locked; JSON-first API accepted
- Q2 is now fully locked: event args become `PreceptValue` inside the evaluator, and `LOAD_ARG` loads them into the evaluator's `PreceptValue[]` register file. The args-vs-fields asymmetry is lifecycle/ownership only.
- Public API primary ingress is now `JsonElement` for commit/update/create/restore data and args.
- `IReadOnlyDictionary<string, object?>` overloads are demoted to convenience extension methods for tests and in-process callers.
- `docs/runtime/runtime-api.md` stays unchanged until the implementation PR ships the runtime surface.

- **Q3 resolved (2026-05-03):** "Where do execution plans come from?" — confirmed existing architecture answers all 6 sub-questions. `ExecutionPlan` is a named type (opcode array + ResultType), compiled eagerly in Pass 5 of `Precept.From()`, embedded in owning structures (ExecutionRow.Guard, ActionPlan.Value, ConstraintDescriptor.Expression, FieldDescriptor.ComputedPlan), shared across Versions via the Precept reference, accessed by structural traversal not name-based lookup. No design change required.

### 2026-05-04T01:18:00Z — Q3 ledger merge recorded
- Frank-53's CC#25 Q3 recommendation is now merged into `decisions.md`; the durable runtime baseline remains unchanged because the existing architecture already answered the execution-plan origin questions.
- Durable statement: `ExecutionPlan` stays the eager Pass 5 compiled form owned by the immutable `Precept` model and reached structurally from guards, action values, constraints, and computed fields.

### 2026-05-04T01:18:00Z — Q4 ledger merge recorded
- Frank's working-copy recommendation is now durably recorded in `decisions.md`: each candidate row forks from the original `Version.Slots`, guards read only immutable source slots, and only the winning row can donate its working copy into the next `Version`.
- Durable constraint: no shared mutable working copy crosses row boundaries; pooling remains an optimization seam, not a semantic change.

### 2026-05-04T02:14:47Z — Q6 revised stack-depth decision accepted
- Shane accepted the revised CC#25 Q6 answer: stack-depth enforcement moves into the Type Checker as an LS diagnostic, with the builder reduced to a debug-assert trust boundary.
