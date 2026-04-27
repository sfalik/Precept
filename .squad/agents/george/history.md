## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and turns approved decisions into implementable structures.
- Historical summary (pre-2026-04-13): led implementation work and feasibility passes for keyword logical operators, narrowing/proof work, computed fields, and related diagnostic/runtime follow-up.

## Learnings

- Analyzer infrastructure has to follow the actual Roslyn operation tree shapes in the catalog code. Constructor arguments are the happy path; object initializers, spreads, and followed field initializers are the edge cases.
- `OperatorTable`, widening checks, and several parser/checker mapping tables are still parallel copies of catalog knowledge. Replacing them yields the highest implementation payoff.
- Multi-source analyzer tests only need a broader helper signature; real-catalog BCL-heavy stubs should stay out of the default test path.
- Precept.Next delivery quality depends on doc/code contract alignment first. Hollow models, missing diagnostic codes, and stale surface docs block trustworthy slice work faster than raw implementation effort does.
- Proof/fault architecture needs a hard split: `CompilationResult` owns `ProofModel`; `Precept.From(compilation)` owns lowering into executable constraint tables and fault-site projections.
- Constraint behavior is a separate contract from fault behavior: rules/ensures lower into `ConstraintPlan` buckets (`always`, state-anchor, event-anchor), while statically preventable hazards lower only as impossible-path `FaultSite` backstops.
- Shane's current preferences for the synthesized design are now explicit and binding: semantic naming over syntax-shaped names, metadata as early as knowable, and three typed action shapes (`TypedAction`, `TypedInputAction`, `TypedBindingAction`).
- Key paths for this design thread: `docs\compiler-and-runtime-design.md`, `docs\runtime\runtime-api.md`, `docs\working\proposal-george-proof-fault-contract.md`, `.squad\decisions\inbox\george-proof-fault-contract.md`.
- The synthesized design is only coherent when it makes three splits explicit at once: `CompilationResult` vs. `Precept`, constraints vs. faults, and authoring consumers vs. execution consumers. Leaving any one of those implicit invites parallel-model drift.
- The action family needed the exact settled names (`TypedAction`, `TypedInputAction`, `TypedBindingAction`) to stay aligned with Shane's directive while still enforcing semantic naming. Anything looser invites a fresh naming fight.

## Recent Updates

### 2026-04-26 — SyntaxTree vs TypedModel boundary review
- Confirmed the dual-artifact design is correct if the typed layer is allowed to reorganize around semantic identity instead of preserving raw source shape.
- Re-grounded the boundary: `SyntaxTree` owns parser-facing structure, spans, and recovery details; `TypedModel` owns resolved semantic meaning.
- Flagged accidental mirroring as the practical danger during implementation, not the existence of both artifacts.

### 2026-04-26 — Cross-catalog analyzer helper API became the implementation center
- Audited all 10 catalog `GetMeta` shapes and the existing analyzer suite to define a reusable helper surface.
- Confirmed the main infrastructure needs: switch-arm walking, enum-value resolution, named-argument extraction, collection enumeration, flags checks, and selective field-initializer following.
- The later convention change to prefer constructor parameters over `init` metadata properties directly supports this helper strategy by eliminating a second extraction path.

### 2026-04-26 — Catalog audit and analyzer expansion follow-through
- Confirmed there were no missing surfaced types; the real correctness fix was `Period` gaining `EqualityComparable`.
- Identified the remaining highest-value follow-up as consumer drift, especially the language server's hardcoded completion lists.
- Helped shape the analyzer expansion into an infrastructure-first queue rather than a purely simple-patterns-first rollout.

### 2026-04-27 — Combined compiler/runtime v2 draft boundary pass
- Locked the document-level split so compiler and runtime stay co-equal products of one source program rather than letting runtime read authoring artifacts directly.
- Made the anti-mirroring rule explicit: `SyntaxTree` owns parser shape and recovery; `TypedModel` must become a semantic database, not a renamed AST.
- Re-grounded LS/MCP consumption boundaries: authoring features read `CompilationResult`; preview/execution features cross the lowering boundary and read `Precept`.

### 2026-04-27 — Merged v2 review follow-through
- The merged architecture draft is close, but it still needs one more hardening pass before lock: the typed-model minimum inventory, LS feature-to-artifact mapping, and runtime executable-model contract are the remaining soft spots.
- A runtime design stays honest only if constraint planning preserves anchor semantics (`always`, `in`, `to`, `from`, `on`) instead of collapsing them into vague state/event buckets.
- The practical anti-mirroring enforcement point is source-origin semantic identity: if hover/definition/token classification cannot run from typed references plus declaration origins, the typed layer is still too syntax-shaped.

### 2026-04-27 — Final concurrence on combined v2 architecture
- Frank's revision closed the architectural blockers: the typed-model minimum inventory, LS feature matrix, executable lowering contract, and explicit anchor taxonomy are now stated sharply enough to guide implementation.
- The decisive coherence win is the non-symmetric model: diagnostics block executable-model construction, runtime outcomes express normal domain/boundary behavior, and faults remain impossible-path backstops only.
- The remaining work is stage-level alignment, not main-doc architecture repair — especially syncing `docs\runtime\fault-system.md` and later descriptor-backed API detail under D8/R4.

### 2026-04-27 — Post-approval v2 follow-up framing
- Treat `docs\working\combined-design-v2.md` as architecturally settled at the top level; future work on this lane should stay stage-scoped unless Shane explicitly reopens the main split.
- Immediate follow-up belongs in stage-level docs and API detail, not another full top-level rewrite.

### 2026-04-28 — Technical review of combined-design-v2.md (accuracy, implementation readiness, anti-Roslyn-bias)
- Verdict: APPROVED-WITH-CONCERNS. Architecturally sound, catalog-consistent, philosophy-faithful. Constraint evaluation matrix, activation indexes, proof/fault chain, and artifact boundaries are all accurate.
- Critical gap: the doc never enumerates the SyntaxTree node inventory — an implementer has no guidance on what AST nodes to produce. The Constructs catalog (11 ConstructKind values with typed ConstructSlot entries) should drive the node hierarchy, and this mapping must be explicit.
- Critical gap: the parser/TypeChecker contract boundary is implicit. What does the parser guarantee? What does the TypeChecker re-check? This is the #1 source of multi-pass compiler bugs.
- Critical gap: no expression grammar specification. Expressions appear in 6+ slot positions but the doc never lists the expression forms (binary, unary, literal, field-ref, event-arg-ref, function-call, if/then/else, is-set, contains, member-access).
- Roslyn-bias risk: an implementer will default to red/green trees, per-construct check methods, Z3-style solvers, and TypedModel-as-renamed-AST. The doc must explicitly name the correct patterns: flat keyword-dispatched parsing, semantic-table-driven type checking, simple predicate proof strategies, and dispatch-optimized lowered model.
- Right-sizing is good. Proof engine strategies, constraint activation indexes, and the three-tier constraint query are correctly scoped for Precept's actual language.
- Expression evaluation model is never specified (tree-walk vs compiled delegates vs IL). For Precept's scale, tree-walk is correct and should be stated.
- The `set` keyword dual-use (action vs type) requires parser position-context disambiguation — the doc mentions ActionKind stamping at parse time but doesn't call out this specific ambiguity.

### 2026-04-25 — Fully metadata-driven compiler feasibility review
- Confirmed lexer is already close to catalog-driven, parser lookup tables are worth deriving, type checker gets the biggest win, and graph/proof/evaluator work remains partly algorithmic by design.
- Reiterated the clean architectural split: metadata for domain knowledge, hand-written code for execution strategy.

### 2026-04-24 — TypeChecker slices and Precept.Next contract review
- Implemented early slice work for field registration and numeric/operator handling while documenting the surrounding doc/code gaps.
- Logged that TypeChecker, GraphAnalyzer, and ProofEngine scaffolding remained hollow relative to the docs, making contract alignment a prerequisite for deeper implementation.

### 2026-04-17 to 2026-04-19 — Proof/narrowing and issue #118 work
- Advanced numeric narrowing, proof extraction, and the TypeChecker decomposition work while tracing real diagnostic span and regression issues in the samples.
- Kept implementation detail grounded in the actual parser/type-checker structure rather than speculative abstractions.
