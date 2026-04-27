# Precept Compiler and Runtime Design

> **Status:** Approved working architecture
> **Audience:** compiler, runtime, language-server, MCP, and documentation authors

**How to read this document.** Sections 1–3 establish what Precept promises, its architectural approach (catalog-driven, purpose-built, unified pipeline), and the end-to-end pipeline overview — read these first for the design's spine. Sections 4–5 cover Lexer and Parser. Section 6 defines the artifact boundaries between `SyntaxTree` and `TypedModel` — what each owns, how the semantic inventory links back to syntax nodes, and the constraints that keep downstream consumers independent of source structure. Sections 7–11 are the remaining per-stage contracts (Type Checker through Lowering), each opening with how that stage serves the structural guarantee; read them in order for the compilation story, or jump to a specific stage when doing component work. Sections 12–16 cover the runtime surface, tooling integration (TextMate grammar generation, MCP, language server), and Appendix A tracks implementation status — these are the consumer-facing contracts that tie compilation output to real product surfaces.

## 1. What Precept promises

Precept is a domain integrity engine for .NET. A single declarative contract governs how a business entity's data evolves under business rules across its lifecycle, making invalid configurations structurally impossible. You declare fields, constraints, lifecycle states, and transitions in one `.precept` file. The runtime compiles that declaration into an immutable engine that enforces every rule on every operation. No invalid combination of lifecycle position and field data can persist.

This is not validation. Validation checks data at a moment in time, when called. Precept declares what the data is allowed to become and enforces that declaration structurally, on every operation, with no code path that bypasses the contract.

The guarantee is **prevention, not detection.** Invalid entity configurations cannot exist — they are structurally prevented before any change is committed. The engine is deterministic: same definition, same data, same outcome. At any point, you can preview every possible action and its outcome without executing anything. Nothing is hidden.

Everything in this document — every pipeline stage, every artifact, every runtime operation — exists to deliver that guarantee.

## 2. Architectural approach

### Catalog-driven design

Precept keeps its language purposely simple. Rather than embedding domain knowledge in pipeline stage implementations (as traditional compilers do), Precept externalizes the entire language definition as structured metadata in ten catalogs. Pipeline stages are generic machinery that reads this metadata.

This inverts the traditional compiler model. In general-purpose compilers (Roslyn, GCC, TypeScript), domain knowledge is scattered across pipeline stage implementations — adding a language feature means touching dozens of files. The surveyed DSL-scale systems take a different approach: CEL centralizes its language definition in `Env` declarations, OPA/Rego externalizes rule indexing and type environments in `ast.Compiler`, and CUE's lattice-based evaluation derives behavior from schema declarations. Precept takes this pattern further — adding a language feature means adding an enum member and filling an exhaustive switch. The C# compiler refuses to build if any member lacks metadata, and propagation to every consumer (grammar, completions, hover, MCP, semantic tokens) is automatic.

The ten catalogs fall into two groups:

**Language definition** — what the language IS: `Tokens` (lexical vocabulary), `Types` (type system families), `Functions` (built-in function library), `Operators` (operator symbols with precedence/associativity/arity), `Operations` (typed operator combinations — which `(op, lhs, rhs)` triples are legal), `Modifiers` (declaration-attached modifiers as a discriminated union with five subtypes), `Actions` (state-machine action verbs), `Constructs` (grammar forms and declaration shapes).

**Failure modes** — how it reports problems: `Diagnostics` (compile-time rules), `Faults` (runtime failure modes). The diagnostic-and-output-design survey confirms that Precept's catalog-based separation of diagnostic rule definition (`DiagnosticCode` in the `Diagnostics` catalog) from diagnostic instance (`Diagnostic` with source location, severity, and message arguments) follows the Roslyn pattern (`DiagnosticDescriptor` / `Diagnostic`) — the most explicit rule-vs-instance separation in the surveyed systems. TypeScript uses a similar `DiagnosticMessage` / `Diagnostic` split. The survey also reveals a severity-level divide: all surveyed DSL-scale systems (CEL, OPA/Rego, CUE, Dhall, Jsonnet, Pkl, Starlark) have error-only diagnostics — no warnings, no hints. Only general-purpose compilers (Roslyn, TypeScript, Rust, Swift) define 4+ severity levels. Precept's `Diagnostics` catalog defines severity levels beyond error, which is an intentional choice above DSL-scale norms, driven by the authoring-surface ambition of the language server and MCP tools.

Their union IS the language specification in machine-readable form. No consumer maintains a parallel copy. Every downstream artifact — the TextMate grammar, LS completions, LS hover, MCP vocabulary, semantic tokens, type-checker behavior — derives from catalog metadata.

The architectural principle: **if something is domain knowledge, it is metadata; if it is metadata, it has a declared shape; if shapes vary by kind, the shape is a discriminated union.** Pipeline stages, tooling, and consumers derive from the metadata — they never encode language knowledge in their own logic. See `docs/language/catalog-system.md` for the full catalog system design.

> **Precept Innovations**
> - **Catalog-as-spec inversion.** Traditional compilers scatter language knowledge across pipeline implementations. Precept externalizes the entire language specification as ten machine-readable catalogs — their union IS the spec, and every consumer derives from them. No other DSL tooling in this category has this property.
> - **Single-act feature propagation.** Adding a language feature is one enum member with an exhaustive metadata switch. The C# compiler refuses to build if metadata is missing, and propagation to grammar, completions, hover, MCP vocabulary, and semantic tokens is automatic.
> - **Grammar generation from catalogs.** The TextMate grammar, LS completions, and MCP vocabulary are generated artifacts, not hand-edited — they cannot drift from the language specification because they ARE the specification, projected to different surfaces.

### Purpose-built

Precept's pipeline is purpose-built for Precept's specific shape: a declarative DSL with fields, states, events, transitions, constraints, and guards. Every stage knows what it is building toward — an executable model that structurally prevents invalid configurations. This is the norm at DSL scale. The surveyed systems — CEL (single-expression safety language), OPA/Rego (policy evaluation), Dhall (configuration with guaranteed termination), Pkl (structured configuration), CUE (constraint-based configuration) — all build purpose-specific pipelines tuned to their domain, not extensible compiler frameworks. Precept follows the same principle: the pipeline does not need to be extensible to other languages. It needs to be correct for this one.

### Unified pipeline

The system has compilation phases (lexing, parsing, type checking, graph analysis, proof) and runtime phases (lowering, evaluation, constraint enforcement, structured outcomes). These are sequential stages of one system. They share the same catalog metadata. They produce artifacts that flow forward. Compilation builds understanding; runtime acts on it.

Two top-level products emerge from this pipeline:

**`CompilationResult`** — the immutable analysis snapshot. Always produced, even from broken input. Authoring surfaces (language server, MCP compile) need the full picture — including syntax errors, unresolved references, and unproven safety obligations — to provide diagnostics, completions, and navigation. This follows the error-tolerant compilation model established by Roslyn (full syntax trees from broken input) and adopted across the surveyed systems: OPA's compiler collects errors without short-circuiting; CEL's `Compile()` returns `Issues` alongside partial ASTs; Dhall's LSP runs the full pipeline and pushes all diagnostics.

**`Precept`** — the executable runtime model. Produced only from error-free compilations via `Precept.From(compilation)`. This is the sealed model that runtime operations execute against. It carries lowered descriptor tables, execution plans, and constraint indexes — not syntax trees or proof graphs.

The relationship is straightforward: analysis builds `CompilationResult`; lowering transforms it into `Precept`; runtime operations execute against `Precept`. Authoring tools read `CompilationResult`. Execution tools read `Precept`.

## 3. The pipeline

```mermaid
flowchart TD
    classDef input fill:#dbeafe,stroke:#60a5fa,color:#1e3a5f
    classDef stage fill:#ede9fe,stroke:#a78bfa,color:#3b1f7e
    classDef artifact fill:#fef3c7,stroke:#f59e0b,color:#78350f
    classDef runtime fill:#d1fae5,stroke:#34d399,color:#064e3b
    classDef leaf fill:#a7f3d0,stroke:#10b981,color:#064e3b

    src([Source text]):::input
    cat([Catalogs]):::input

    subgraph compile [Compile-time pipeline]
        direction LR
        L(Lexer):::stage --> P(Parser):::stage --> TC(TypeChecker):::stage --> GA(GraphAnalyzer):::stage --> PE(ProofEngine):::stage
    end

    src --> compile
    cat -. feeds all stages .-> compile

    compile --> CR[CompilationResult]:::artifact
    CR -- "only when !HasErrors" --> PF(Precept.From):::artifact

    subgraph runtime [Runtime operations]
        C(Create):::runtime
        R(Restore):::runtime
        E(Evaluator):::runtime
        SQ(Structural queries):::runtime
    end

    PF --> C
    PF --> R
    PF --> E
    PF --> SQ

    E --> FI([Fire / InspectFire]):::leaf
    E --> UI([Update / InspectUpdate]):::leaf
    E --> FB([Fault backstops]):::leaf
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

`Precept.From()` lowers analysis knowledge into runtime-native shapes. Runtime types hold no references to compile-stage artifacts — but the knowledge those artifacts contain crosses in lowered form.

- **Transition dispatch index** — state × event → target state. This is graph topology, lowered into a routing table the evaluator and inspection surfaces consume directly.
- **State descriptor table** — all named states with metadata (display name, terminal flag, initial/required/irreversible modifiers, available events). Enables structural queries ("what states exist?", "what modifiers apply?").
- **Event availability index** — valid events per state. Enables "what can I do from here?" queries for MCP, AI agents, and UI consumers.
- **Reachability index** — states reachable from a given state. Enables structural navigation without re-running the compiler.
- **Pathfinding residue** — enough topology for shortest-path navigation from current state to a target. The graph analog of `ConstraintInfluenceMap` — causal reasoning over lifecycle structure.
- **`ConstraintDescriptor`** — expression text, source lines, scope targets, guard metadata, `ConstraintActivation` anchor.
- **`ConstraintInfluenceMap`** — constraint → contributing fields with expression-text excerpts. Enables "which field change would fix this?" without reverse-engineering.
- **Structured violation shapes** — `ConstraintViolation` carries failing constraint descriptor, evaluated field values, guard context, and failing sub-expression.
- **Fault-site backstops** — `FaultSite` descriptors linked to `FaultCode` and the compiler-owned prevention `DiagnosticCode`.

`SyntaxTree`, `TokenStream`, parser recovery, and the `ProofModel` graph structure don't cross — nothing at runtime needs them. The compiler-result-to-runtime survey confirms a spectrum of severance approaches: CEL's `Program` retains AST node IDs (`Interpretable.ID()`) for cost tracking and `EvalState` observation, maintaining a back-reference to the checked AST; Dhall discards all compile artifacts after decoding — the normalized `Expr Src Void` is consumed by the `Decoder` and the host-language value carries no back-reference; Pkl's `Evaluator` merges parse, type-check, and evaluation into a single call with no separate compile artifact at all. Precept's lowering sits in the middle — it carries analysis knowledge forward in runtime-native shapes (descriptors, plans, indexes) while severing all structural references to compile-time artifacts.

> **Precept Innovations**
> - **Unified pipeline.** Compilation and runtime are sequential stages of one system sharing the same catalog metadata — not two separate systems bolted together. There is no "compile step" followed by a "runtime step" from the user's perspective; the pipeline flows from source text to executable enforcement in one continuous transformation.
> - **CompilationResult as an always-available intelligence snapshot.** Even broken programs produce a full `CompilationResult` with partial analysis — language server and MCP tools always have something to work with. Traditional compilers stop at the first error boundary; Precept provides progressive intelligence across all stages.
> - **Lowering as selective transformation.** The boundary between analysis and execution is not a wall — it is a selective transformation that carries exactly the analysis knowledge the runtime needs in runtime-native shapes, while preventing runtime types from depending on compile-time artifacts.
> - **Graph topology as a first-class runtime artifact.** The `Precept` model carries a full lowered topology — transition dispatch index, state descriptor table, reachability index, pathfinding residue — not just an opaque executor. Runtime consumers (MCP, AI agents, UI) can ask "what states exist?", "what can I do from here?", "how do I reach state X?" These are structural guarantee questions, answerable without re-running the compiler. No other state machine library in this category exposes lifecycle topology as a queryable runtime surface.

## 4. Lexer

The lexer converts raw text into classified tokens with exact spans. It has no semantic opinion. The key design choice: `TokenKind` comes directly from catalog metadata (`Tokens.GetMeta`), not from a parallel enum maintained by the lexer. The lexer is a vocabulary consumer, not a vocabulary owner.

```mermaid
flowchart LR
    classDef input fill:#dbeafe,stroke:#60a5fa,color:#1e3a5f
    classDef stage fill:#ede9fe,stroke:#a78bfa,color:#3b1f7e
    classDef artifact fill:#fef3c7,stroke:#f59e0b,color:#78350f

    SRC([source text]):::input
    CAT([Tokens · Diagnostics\ncatalogs]):::input
    LEX(Lexer):::stage
    OUT[TokenStream]:::artifact

    SRC --> LEX
    CAT --> LEX
    LEX --> OUT
```

| | |
|---|---|
| **Output** | `TokenStream` — `ImmutableArray<Token>` (each: `TokenKind`, `Text`, `SourceSpan`) plus lex-phase diagnostics |
| **Catalog role** | `TokenKind` comes from `Tokens.GetMeta(...)` / `Tokens.Keywords`. Token categories, TextMate scope, semantic token type, and completion hints derive from `TokenMeta`. |
| **Consumers** | Parser, `CompilationResult`, LS lexical tokenization and grammar tooling |

**How it serves the guarantee:** The lexer ensures that every character of source text is accounted for and classified according to catalog-defined vocabulary. No ambiguity in token identity propagates downstream.

**Implementation status:** This is the one materially implemented compiler stage.

> **Precept Innovations**
> - **Catalog-driven token recognition.** `TokenKind` derives from catalog metadata, not a parallel enum. The lexer is a vocabulary consumer — adding a keyword to the `Tokens` catalog automatically makes it lexable, highlightable, and completable.
> - **No vocabulary ownership at the lexer level.** Traditional lexers own a hardcoded keyword table. Precept's lexer reads its vocabulary from the same metadata that drives every other consumer.

## 5. Parser

The parser builds the source-structural model of the authored program. Its key design choice: `SyntaxTree` preserves the author's source structure — including recovery shape for broken programs — without resolving names, types, or overloads. Tooling needs source-faithful structure (folding, outline, recovery context) independently of semantic resolution.

```mermaid
flowchart LR
    classDef input fill:#dbeafe,stroke:#60a5fa,color:#1e3a5f
    classDef stage fill:#ede9fe,stroke:#a78bfa,color:#3b1f7e
    classDef artifact fill:#fef3c7,stroke:#f59e0b,color:#78350f

    TS[TokenStream]:::artifact
    CAT([Constructs · Tokens\nOperators · Diagnostics\ncatalogs]):::input
    PAR(Parser):::stage
    OUT[SyntaxTree]:::artifact

    TS -->|"token stream"| PAR
    CAT --> PAR
    PAR --> OUT
```

| | |
|---|---|
| **Output** | `SyntaxTree` — `PreceptSyntax Root` with source-faithful declaration/expression nodes, missing-node representation, and span ownership; plus parse-phase diagnostics. (Current shape: diagnostics-only stub.) |
| **Catalog role** | The parser stamps syntax-level identities as soon as syntax alone can know them: construct kind, anchor keyword, action keyword, operator token, literal segment form. |
| **Consumers** | TypeChecker, LS syntax-facing features (outline, folding, recovery-aware local context) |

**How it serves the guarantee:** Structural fidelity means the type checker and downstream stages work from a faithful representation of the author's intent, including malformed programs — authoring tools can diagnose problems precisely because the structure is preserved, not discarded on error.

**Implementation status:** `Parser.Parse` is a stub; the contract is designed but not implemented.

### Parser/TypeChecker contract boundary

The parser guarantees to the type checker:

- Every declaration is structurally well-formed — required slots are filled, or represented as `MissingNode` (never silently absent). The type checker does not re-validate structural completeness.
- All identifiers in keyword positions have been resolved to catalog-defined keywords. The type checker does not re-resolve keyword identity.
- `ConstructKind`, `ActionKind`, `OperatorKind`, `TypeKind` (on `TypeRef`), and `ModifierKind` are stamped on every applicable node. The type checker can assume these are present and correct.

What the parser does NOT guarantee: name resolution, type compatibility, overload selection, or semantic legality. The type checker owns all semantic resolution.

### Error recovery

Error recovery is construct-level, not token-level. When the parser encounters a malformed construct, it emits a diagnostic and skips to the next newline-anchored declaration keyword (`field`, `state`, `event`, `rule`, `from`, `in`, `to`, `on`). This is panic-mode recovery with synchronization at declaration boundaries.

Malformed input is represented as `MissingNode` for required slots that could not be parsed and `SkippedTokens` trivia attached to the nearest valid node for tokens that could not be incorporated into any construct. A `MissingNode` carries the expected `ConstructSlot` identity and the span where the parser expected content. The tree always accounts for every character of source text — no input is silently discarded. This adapts the error-tolerant parsing approach pioneered by Roslyn (missing tokens + skipped tokens trivia) to Precept's simpler grammar shape, where recovery granularity is at the declaration level rather than the statement level.

### Node inventory

The parser produces one syntax node type per `ConstructKind`, with child nodes corresponding to `ConstructSlot` entries from the `Constructs` catalog. The root is `PreceptSyntax`, containing declaration nodes:

- `FieldDeclarationSyntax` — field name, type reference, modifiers, default/computed expression
- `StateBlockSyntax` — state name, modifiers, nested state-scoped declarations
- `EventDeclarationSyntax` — event name, modifiers, arg declarations
- `TransitionRowSyntax` — anchor (`from`/`to`/`in`/`on`), state/event references, guard, action chain
- `RuleDeclarationSyntax` — guard (optional), ensure expression, because clause
- `EnsureDeclarationSyntax` — state/event-scoped constraint with because clause
- `AccessDeclarationSyntax` — edit/readonly declarations per field per state

Expression nodes: `BinaryExpressionSyntax`, `UnaryExpressionSyntax`, `LiteralExpressionSyntax`, `FieldReferenceSyntax`, `EventArgReferenceSyntax`, `FunctionCallSyntax`, `IfThenElseSyntax`, `MemberAccessSyntax`, `IsSetExpressionSyntax`, `ContainsExpressionSyntax`.

### Catalog-to-grammar mapping

Catalog metadata factors into parsing decisions at specific points. The parser uses `Constructs.GetMeta()` to determine legal declaration forms — each `ConstructKind` defines the expected slot sequence, and the parser validates that slots appear in the declared order with declared optionality. The parser uses `Operators.GetMeta()` for expression parsing — operator precedence and associativity come from catalog metadata, not a hardcoded table. Keyword recognition is inherited from the lexer's catalog-driven `TokenKind` assignments; the parser dispatches on `TokenKind`, not on string comparison.

### Right-sized parser patterns

Precept's grammar calls for parser patterns scaled to a flat, keyword-anchored, line-oriented DSL — not patterns designed for deeply nested general-purpose languages. The surveyed DSL-scale systems confirm what works at this scale:

- **Flat parse trees.** Precept's grammar has no deep nesting, no brace-delimited scopes, no expression statements. Red/green tree architectures (Roslyn, rust-analyzer) solve incremental reparsing of deeply nested, brace-delimited structures — a problem that does not exist in flat, line-oriented grammars. CEL produces a flat protobuf AST; OPA/Rego produces module-level `Rule` lists; Dhall and Jsonnet both produce single-expression trees with no incremental infrastructure.
- **Declaration-boundary error recovery.** When the parser encounters a malformed construct, it skips to the next newline-anchored declaration keyword (`field`, `state`, `event`, `rule`, `from`, `in`, `to`, `on`). This is panic-mode recovery with synchronization at declaration boundaries. Token-level insertion/deletion with cost models (as in Roslyn or GCC) is designed for statement-level grammars where recovery points are ambiguous. Precept's keyword-anchored lines provide unambiguous synchronization. OPA's parser similarly synchronizes at rule boundaries; Pkl's tree-sitter grammar provides node-level error recovery.
- **Expressions only in specific slots** — guards, action RHS, ensure clauses, computed fields, if/then/else, and because clauses. CEL is a single-expression language; OPA confines expressions to rule bodies and comprehensions. Precept follows the same containment pattern — the parser does not need a general-purpose expression parser for the full language.
- **Operator precedence from metadata.** Operator precedence comes from `Operators.GetMeta()`, not a hardcoded table. The correct pattern is precedence-climbing — a standard technique for expression parsing at this scale (CEL uses a similar approach in its ANTLR-generated parser with explicit precedence levels; OPA's parser embeds precedence in its recursive descent structure).
- **LL(1) with single-token lookahead** in most positions, given the keyword-anchored, line-oriented design. This is simpler than the LL(k) or GLR techniques general-purpose languages require.

### `ActionKind` dual-use note

`set` appears as both an action keyword (`TokenCategory.Action` — e.g., `set Amount to 100`) and a type keyword (`TokenCategory.Type` — e.g., `field Tags as set of string`). The parser disambiguates by position context: after `->` or in action position = action; after `as`/`of` or in type position = type. This disambiguation is a parser responsibility, not a catalog lookup — the catalog correctly classifies `set` under both categories.

**Implementation status:** `Parser.Parse` is a stub; the contract is designed but not implemented.

> **Precept Innovations**
> - **Flat, declaration-oriented grammar.** No nesting beyond expression-within-declaration. This makes the grammar trivially parseable, the error recovery model simple and predictable, and the SyntaxTree shape directly useful for tooling without the complexity budget of a general-purpose language parser.
> - **Precedence from catalog metadata.** Operator precedence and associativity are not hardcoded — they derive from `Operators.GetMeta()`. Changing precedence is a catalog edit, not a parser rewrite.
> - **One node type per `ConstructKind`.** The node inventory is catalog-derived — each construct in the `Constructs` catalog maps to exactly one syntax node with slots matching `ConstructSlot` entries. The parser shape IS the grammar shape.

## 6. SyntaxTree and TypedModel

Precept's compilation pipeline produces two artifacts with distinct roles: `SyntaxTree` (source-faithful parse tree) and `TypedModel` (flat semantic inventory). They are not two independent trees — entries in the semantic inventory hold direct back-pointers to their originating syntax nodes. This section defines what each artifact owns, how the back-pointer link works, and the constraints that keep downstream consumers from depending on source structure.

### What each artifact owns

**`SyntaxTree` owns source structure:**

- **Source fidelity** — exact authored ordering, token adjacency, span offsets. The tree preserves what the author wrote, including whitespace-adjacent constructs and declaration order as authored.
- **Recovery shape** — missing nodes, error nodes, skipped-tokens trivia, malformed constructs. The tree accounts for every character of source text, even in broken programs.
- **Structural LS consumers** — parser diagnostics, folding, outline, progressive LS intelligence that operates before semantic resolution is available.

**`TypedModel` owns semantic meaning:**

- **Resolved identities** — resolved names, types, overloads, operation identity. Every identifier and expression has a resolved semantic identity backed by catalog metadata.
- **Normalized declarations** — semantic inventories shaped for analysis and lowering, not parser nesting. Rules, `in`/`to`/`from`/`on` ensures, transition rows, access declarations, state hooks, and stateless hooks are organized by semantic role, not by source position.
- **Semantic consumers** — LS semantic intelligence (hover, go-to-definition, semantic tokens, semantic completions), graph analysis, proof, and lowering.

If a consumer needs source structure, it reads `SyntaxTree`. If it needs meaning, it reads `TypedModel`. The ownership boundaries are strict — the semantic inventory is shaped by what its consumers need, not by what the parser produces.

**Example:** For `Approve.Amount <= RequestedAmount`, the syntax tree holds a member-access node over exact tokens and spans — it knows the dot position, the identifier spans, the operator token. The typed model holds a resolved event-arg symbol, a resolved field symbol, a resolved `OperationKind`, and a result type of `boolean` — it knows what the expression means, not where the tokens are. The typed entry also holds a direct reference to the originating syntax node, so an LS hover handler can resolve the semantic identity from `TypedModel` and read the source span from the linked syntax node without searching the tree.

### Syntax-node back-pointers

Semantic entries in `TypedModel` hold direct references to their originating `SyntaxTree` nodes. A `TypedField` points to its `FieldDeclarationSyntax`; a `TypedExpression` points to the expression syntax node it was resolved from; a `TypedTransitionRow` points to its `TransitionRowSyntax`. The pointer is a direct object reference — not a span lookup, not an index-based correlation.

**Why this is the right trade for Precept:**

- **Same-process language server.** The LS runs in the same process as the compiler. There is no serialization boundary between compilation output and LS consumption — both artifacts live in the same heap. A direct object reference is the simplest, fastest, and most debuggable linking strategy.
- **Immutable full-snapshot model.** Precept recompiles the entire file on every change (§10). Each compilation produces a fresh, immutable `CompilationResult` containing both `SyntaxTree` and `TypedModel`. The back-pointers never dangle — they always reference nodes within the same compilation snapshot.
- **Small DSL, bounded cost.** At Precept's scale (flat grammar, shallow trees, 64KB ceiling), the memory cost of holding both artifacts plus cross-references is negligible. There is no reason to introduce indirection layers that would only pay for themselves in large-codebase compilers.
- **Easier LS evolution.** When an LS feature needs a new piece of source-structural context (e.g., the exact span of a guard clause for a diagnostic underline), the back-pointer makes it immediately available without extending the `TypedModel` contract. This keeps the semantic inventory stable while the LS surface grows.
- **Malformed-code handling.** In broken programs, recovery nodes in `SyntaxTree` may have no corresponding semantic entry. The back-pointer design handles this naturally: only successfully resolved semantic entries hold pointers. The LS can fall back to `SyntaxTree`-only intelligence for unresolved regions without any correlation logic.

**What the back-pointer is NOT.** It is a navigation convenience for LS features and diagnostic rendering — not a license for semantic consumers to depend on syntax structure. The presence of the pointer does not change the ownership boundary: `TypedModel` owns meaning; `SyntaxTree` owns structure.

### Downstream consumers and the syntax boundary

Graph analysis, proof, and lowering consume the semantic inventories in `TypedModel`. They must not traverse syntax nodes, even though the back-pointers make them reachable. This is a structural constraint, not a suggestion.

The reasoning is consumer independence: if graph analysis walked syntax nodes to extract transition topology, a change to parser recovery shape could break graph analysis. If proof walked syntax to read constraint expressions, adding a new expression node kind would require proof changes. These stages consume normalized semantic declarations — transition rows with resolved state/event identity, typed constraint expressions, dependency-fact sets — and they should continue to work correctly regardless of how the parser evolves.

The LS is the primary beneficiary of the back-pointers. Hover reads the semantic identity from `TypedModel` and the source span from the linked syntax node. Go-to-definition reads the declaration symbol from `TypedModel` and the declaration span from the linked syntax node. Semantic tokens read the resolved kind from `TypedModel` and the token range from the linked syntax node. These are precisely the LS patterns where the back-pointer eliminates boilerplate span correlation.

### Required TypedModel inventory

The `TypedModel` is a flat semantic inventory — not a tree, not a mirror of parser structure. Its shape is driven by what semantic consumers need:

- **Declaration symbols** — stable semantic identities for fields, states, events, args, and constraint-bearing declarations, each holding a back-pointer to the originating syntax node for diagnostics and navigation.
- **Reference bindings** — every semantic identifier/expression site binds directly to a symbol, overload, accessor, operator, or action identity.
- **Normalized declarations** — rules, `in`/`to`/`from`/`on` ensures, transition rows, access declarations, state hooks, and stateless hooks live in semantic inventories shaped for analysis and lowering, not parser nesting.
- **Typed expressions** — expression entries carry resolved result type plus resolved operation/function/accessor identity and semantic subjects, with a back-pointer to the originating expression syntax node.
- **Typed actions** — semantic action families resolve to one of three named shapes (see §7 Type Checker) with catalog-defined operand and binding contracts.
- **Dependency facts** — computed-field dependencies, arg dependencies, referenced-field sets, and semantic edge data required by graph/proof/lowering.

### Anti-mirroring rules

These rules constrain the `TypedModel` shape. They belong here — with the artifact-boundary decision — not with the type checker implementation, because they are architectural constraints on the artifact boundary, not algorithmic guidance for the type checker.

1. **No parser layout inheritance.** `TypedModel` must not preserve parser child layout, missing-node shape, or recovery nullability as its primary contract. The semantic inventory is organized by semantic role, not by source structure.
2. **Semantic LS features must not walk syntax.** Hover, go-to-definition, semantic tokens, and semantic completions must be satisfiable from `TypedModel` bindings plus back-pointers to originating syntax nodes. If an LS feature must walk parser structure to answer a semantic question, the `TypedModel` is underspecified — fix the inventory, not the LS feature.
3. **Downstream stages consume semantic inventories.** Graph analysis, proof, and lowering consume normalized semantic inventories. They must not traverse syntax nodes via back-pointers. If a downstream stage needs source-structural information, the `TypedModel` is missing a semantic fact.
4. **`SyntaxTree` retains sole ownership of source shape.** Recovery, token grouping, exact authored ordering, and malformed-construct shape belong to `SyntaxTree` exclusively. `TypedModel` entries hold back-pointers for navigation, not syntax fragments for reconstruction.

### Research context

At DSL scale, most systems blur the syntax/semantics boundary — they annotate or mutate a single tree because the grammar is simple enough that the cost of entanglement is low. CEL attaches a `type_map` and `reference_map` via node IDs; OPA/Rego mutates the original AST in multiple passes; Dhall parameterizes a single `Expr` type through phases. Among general-purpose compilers, Roslyn produces a separate `SemanticModel` that provides symbol/type resolution on demand without modifying the syntax tree — the closest precedent for Precept's approach.

Precept makes the separation explicit and enforced, accepting a small implementation cost for clean ownership boundaries between source-structural and semantic consumers. The back-pointer strategy is a pragmatic addition: it gives the LS cheap navigation without compromising the separation that protects downstream stages. This is a deliberate over-investment relative to DSL-scale norms, motivated by the same reasoning that drives the catalog-as-spec inversion — structural guarantees are worth more than implementation economy.

> **Precept Innovations**
> - **Flat semantic inventory, not a parallel tree.** `TypedModel` is a flat inventory of symbols, bindings, and normalized declarations — not a second tree that mirrors parser structure. The shape is driven by what graph analysis, proof, lowering, and the LS need, not by what the parser produces. The anti-mirroring rules enforce this structurally.
> - **Syntax-node back-pointers with consumer discipline.** Semantic entries hold direct references to originating syntax nodes — cheap LS navigation without span correlation. But downstream stages (graph, proof, lowering) consume only the semantic inventories, never the syntax structure behind the pointers. The back-pointer is a navigation convenience, not a structural dependency.

## 7. Type Checker

The type checker is the first stage that reasons about semantics. Its key design choice: type resolution produces a flat semantic inventory (`TypedModel`) rather than annotating syntax nodes in-place — because downstream consumers (graph analysis, proof, lowering) and LS semantic features need normalized declarations and resolved identities, not decorated source structure. Semantic entries hold back-pointers to originating syntax nodes (see §6), but the inventory's shape is driven by consumer needs, not by parser layout.

```mermaid
flowchart LR
    classDef input fill:#dbeafe,stroke:#60a5fa,color:#1e3a5f
    classDef stage fill:#ede9fe,stroke:#a78bfa,color:#3b1f7e
    classDef artifact fill:#fef3c7,stroke:#f59e0b,color:#78350f

    ST[SyntaxTree]:::artifact
    CAT([Types · Functions · Operators\nOperations · Modifiers · Actions\nConstructs · Diagnostics\ncatalogs]):::input
    TC(Type Checker):::stage
    OUT[TypedModel]:::artifact

    ST -->|"syntax tree"| TC
    CAT --> TC
    TC --> OUT
```

| | |
|---|---|
| **Output** | `TypedModel` — semantic symbol tables and binding indexes, normalized declaration inventories, typed expressions and actions, dependency facts, and syntax-node back-pointers, plus diagnostics. (Current shape: diagnostics-only stub.) |
| **Catalog role** | First stage to resolve `TypeKind`, `FunctionKind`, `OperatorKind`, `OperationKind`, `ModifierMeta`, `ActionMeta`, `FunctionOverload`, `TypeAccessor`, and attached `ProofRequirement` records into semantic identity. |
| **Consumers** | GraphAnalyzer, ProofEngine, LS semantic tooling, MCP compile output, lowering |

**How it serves the guarantee:** The type checker catches semantic defects — type mismatches, illegal operations, invalid modifier combinations, unresolved references — before the program reaches graph analysis or runtime. Every expression and declaration that passes type checking has a resolved, catalog-backed semantic identity. This is where the structural guarantee begins to take shape: if it type-checks, its operations are legal.

**Implementation status:** `TypeChecker.Check` is stubbed; the semantic model contract is ahead of implementation.

### Right-sized type checking: generic resolution passes

The type checker should NOT have a `CheckFieldDeclaration()`, `CheckTransitionRow()`, `CheckRuleDeclaration()` method per construct kind. The surveyed DSL-scale type checkers confirm the right pattern for this scale: CEL's checker walks the AST once, resolving types against its `Env` environment with overload dispatch from a centralized function registry; OPA's type checker (`ast/check.go`) makes a single pass over rules against a `TypeEnv`. The correct model for Precept is the same — generic resolution passes that read construct metadata from catalogs. Catalog-resolvable checks are generic passes; only construct-specific structural validation that genuinely differs by kind (field declarations vs. transition rows have different type-checking needs) warrants per-kind methods. The type checker builds semantic symbol tables and binding indexes — a symbol-table-driven approach — not a parallel tree that mirrors `SyntaxTree` with type annotations added.

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

> **Precept Innovations**
> - **Catalog-driven resolution passes.** Type checking resolves against catalog metadata (`Operations`, `Functions`, `Types`, `Modifiers`, `Actions`) rather than encoding per-construct behavior in checker logic. Adding a new operation or function to the catalog automatically makes it resolvable — no checker code changes required.
> - **Flat semantic inventory, not annotated syntax.** The `TypedModel` is a flat inventory of symbols, bindings, and normalized declarations — not an AST with types bolted on. Semantic entries hold back-pointers to originating syntax nodes for LS navigation, but downstream consumers (graph analysis, proof, lowering) consume only the semantic inventories and remain independent of source structure.
> - **Three-shape typed action family.** Actions resolve to exactly one of three semantic shapes (`TypedAction`, `TypedInputAction`, `TypedBindingAction`), enforced by the DU pattern. A flat shape with optional nullable fields is prohibited — the type system prevents invalid action representations.

## 8. Graph Analyzer

The graph analyzer derives lifecycle structure from semantic declarations. Its key design choice: graph analysis consumes the resolved `TypedModel` — not syntax — because reachability, dominance, and topology require resolved state/event/transition identity, not source-structural nesting.

```mermaid
flowchart LR
    classDef input fill:#dbeafe,stroke:#60a5fa,color:#1e3a5f
    classDef stage fill:#ede9fe,stroke:#a78bfa,color:#3b1f7e
    classDef artifact fill:#fef3c7,stroke:#f59e0b,color:#78350f

    TM[TypedModel]:::artifact
    CAT([Modifiers · Actions\nDiagnostics\ncatalogs]):::input
    GA(Graph Analyzer):::stage
    OUT[GraphResult]:::artifact

    TM -->|"semantic declarations"| GA
    CAT --> GA
    GA --> OUT
```

| | |
|---|---|
| **Output** | `GraphResult` — graph facts keyed by semantic identities, plus diagnostics. (Current shape: diagnostics-only stub.) |
| **Catalog role** | State semantics (`initial`, `terminal`, `required`, `irreversible`, `success`, `warning`, `error`) come from modifier metadata already resolved by the type checker; the analyzer must not reinterpret raw syntax. |
| **Consumers** | ProofEngine, `Precept.From`, LS structural diagnostics, runtime structural precomputation |

**How it serves the guarantee:** The graph analyzer detects lifecycle defects — unreachable states, terminal states with outgoing edges, required-state dominance violations, irreversible back-edges — that would make the state machine unsound. These are structural problems in the contract itself, caught before any instance exists. The surveyed state-graph analysis systems confirm the value of compile-time structural verification: SPIN/Promela performs reachability and deadlock detection on state models; Alloy Analyzer checks structural properties of relational models; NuSMV/nuXmv performs CTL/LTL model checking for reachability and liveness; XState's `@xstate/graph` computes reachable states and transition paths. Precept's graph analyzer applies these same structural analysis patterns — reachability, dead-state detection, topological validation — at compile time rather than as a separate verification step.

### GraphResult inventory

- **`ReachabilitySet`** — partitions all states into reachable, unreachable, and terminal sets relative to the initial state.
- **`TransitionAdjacency`** — state → events → target states topology map; the directed edge set of the lifecycle graph.
- **`PredecessorIndex`** — state → set of states that have a direct transition into it.
- **`SuccessorIndex`** — state → set of states reachable via a single outgoing transition.
- **`DominanceFact`** — records that a required-state modifier mandates all paths to a terminal state pass through the required state.
- **`TerminalOutgoingViolation`** — flags a terminal state that has outgoing transitions, violating structural soundness.
- **`IrreversibleBackEdgeViolation`** — flags a transition that re-enters an irreversible state from a downstream state.
- **`EventCoverageEntry`** — per-state inventory of which events have declared transition rows and which do not.
- **`ProofForwardingFact`** — graph-derived facts (reachability gaps, dominance violations, structural defects) forwarded to the proof engine as obligation inputs.

**Implementation status:** `GraphAnalyzer.Analyze` is stubbed.

> **Precept Innovations**
> - **Reachability as a first-class design artifact.** Graph analysis produces reachable/unreachable state sets, structural validity facts, and runtime indexes — not just a pass/fail check. These facts flow into proof obligations and runtime precomputation.
> - **Lifecycle soundness as a compile-time guarantee.** Unreachable states, terminal outgoing-edge violations, required-state dominance violations, and irreversible back-edges are all caught before any instance exists. No state machine library in this category provides this level of static lifecycle verification.
> - **Structural cycle and dominance detection.** The graph analyzer reasons about structural properties (dominance, predecessor/successor relationships, event coverage per state) that would otherwise require runtime observation to discover.

## 9. Proof Engine

The proof engine is the last analysis stage before lowering — and the compile-time half of the structural guarantee. It discharges statically preventable runtime hazards: if it can prove an operation is safe at compile time, no runtime check is needed; if it cannot, the compiler emits a diagnostic and the author must fix the source before an executable model is produced. Its key design choice: proof is bounded — four strategies only, no general SMT solver — and proof stops at analysis. The runtime receives only lowered fault-site residue for defense-in-depth, not the proof graph itself.

```mermaid
flowchart LR
    classDef input fill:#dbeafe,stroke:#60a5fa,color:#1e3a5f
    classDef stage fill:#ede9fe,stroke:#a78bfa,color:#3b1f7e
    classDef artifact fill:#fef3c7,stroke:#f59e0b,color:#78350f

    TM[TypedModel +\nGraphResult]:::artifact
    CAT([Operations · Functions\nTypes · Diagnostics · Faults\ncatalogs]):::input
    PE(Proof Engine):::stage
    OUT[ProofModel]:::artifact

    TM -->|"semantic model + graph facts"| PE
    CAT --> PE
    PE --> OUT
```

| | |
|---|---|
| **Output** | `ProofModel` — obligations and evidence, dispositions and preventable-fault links, diagnostics with semantic site attribution. (Current shape: diagnostics-only stub.) |
| **Catalog role** | Proof obligations originate in metadata: `BinaryOperationMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`, `TypeAccessor.ProofRequirements`, and action metadata. `FaultCode` ↔ `DiagnosticCode` linkage is catalog-owned. |
| **Consumers** | `CompilationResult`, LS/MCP proof reporting, lowering of fault residue into runtime backstops |

### ProofModel inventory

- **`ProofObligation`** — a single provable claim: carries semantic site reference, originating `ProofRequirement` (from catalog metadata), strategy used for discharge, disposition (`proved` or `unresolvable`), and `DiagnosticCode` reference if unresolvable.
- **`FaultSiteLink`** — links an unresolvable `ProofObligation` to its corresponding `FaultSiteDescriptor`, threading the proof/fault chain so lowering can plant runtime backstops.
- **`ConstraintInfluenceEntry`** — maps a constraint to its contributing fields and expression-text excerpts; lowering reads these to build the runtime `ConstraintInfluenceMap`.
- **`InitialStateSatisfiabilityResult`** — per-field/constraint-pair verdict on whether default values satisfy initial-state constraints, with diagnostic reference if unsatisfiable.
- **`ObligationCoverageRecord`** — tracks which proof strategy discharged each obligation, providing an auditable coverage map across the strategy set.

### Proof strategy set

The proof engine operates over a bounded, non-extensible strategy set:

- **Literal proof** — the value is a known compile-time literal; outcome is directly knowable.
- **Modifier proof** — the value flows through a catalog-defined modifier chain whose output bounds are statically determined.
- **Guard-in-path proof** — a guard expression in the control flow statically establishes a sufficient range or type constraint.
- **Straightforward flow narrowing** — if a guard clause in the same transition row establishes a constraint on a field, that constraint is available as evidence for proof obligations on expressions within that row's action chain. This is type-state narrowing through the immediately enclosing control path, not general dataflow analysis.

Any obligation outside this set is unresolvable by the compiler and emits a `Diagnostic`. New strategies are language changes, not tooling extensions. Each strategy is a simple predicate function, not a solver — literal proof checks a compile-time constant, modifier proof checks a modifier chain, guard-in-path proof checks enclosing guard subsumption, flow narrowing checks immediate control-path type state. This bounded approach is a deliberate design decision: the surveyed verification systems (SPARK Ada/GNATprove, Dafny, Liquid Haskell, CBMC) all depend on external SMT solvers (Z3, CVC4/5) or SAT solvers for general proof discharge, introducing significant implementation complexity and non-deterministic verification times. Precept's four-strategy set avoids external solver dependencies entirely — at the cost of proof coverage breadth — which is appropriate for a DSL where the expression language is intentionally constrained and the obligation space is bounded.

### Per-obligation disposition model

Each proof obligation resolves to an explicit disposition — not a binary pass/fail. The surveyed verification systems confirm the value of per-obligation disposition granularity: CBMC reports `SUCCESS`, `FAILURE`, or `UNKNOWN` per property; Frama-C/WP reports `Valid`, `Unknown`, `Invalid`, or `Timeout` per ACSL annotation; Dafny tracks per-method `PipelineStatistics` with `ErrorCount`, `InconclusiveCount`, `TimeoutCount`, and `OutOfResourceCount`. Precept's proof model follows this pattern — each `ProofObligation` carries a disposition (proved, unresolvable) and the strategy that discharged it (or the diagnostic emitted). The disposition is the proof engine's primary output; the proof/fault chain (below) threads it into the rest of the system.

SPARK GNATprove additionally provides a `Justified` disposition for checks that cannot be proved but have been manually annotated as acceptable (`pragma Annotate(GNATprove, False_Positive|Intentional, Pattern, Reason)`). Precept does not need this mechanism today — the bounded strategy set and constrained expression language should cover the obligation space — but if the proof coverage boundary (below) reveals uncoverable obligations, a justification mechanism would be the precedented response.

**Proof coverage boundary:** The four strategies must be validated against the sample corpus (20 files in `samples/`). If cross-field comparison obligations (e.g., `ApprovedAmount <= RequestedAmount`) cannot be discharged by any of the four strategies, a fifth strategy (e.g., relational pair narrowing) is needed before v1. This is the highest-risk unknown in the proof engine — the value proposition depends on coverage being sufficient for real-world programs.

### Initial-state satisfiability

If default field values and initial-state constraints are both statically known, the proof engine verifies satisfiability at compile time and emits a diagnostic if no valid initial configuration exists. An author who writes `field X as number default 0` and `in Draft ensure X > 5` gets a compile-time error, not a runtime `EventConstraintsFailed` on create. This is threaded through the proof/fault chain: `ProofRequirement` (initial-state satisfiability) → `ProofObligation` (specific field/constraint pair) → `DiagnosticCode` (unsatisfiable initial configuration). This check applies to `Create` without initial event; `Create` with initial event evaluates satisfiability through the normal fire-path proof chain.

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

> **Precept Innovations**
> - **Compile-time satisfiability checking.** The proof engine guarantees that initial-state configurations are satisfiable at compile time — no validator, state machine library, or rules engine in this category provides this. It is the proof engine's signature contribution.
> - **`ConstraintInfluenceMap`.** Lowering can produce a precomputed map from constraints to contributing fields (with expression-text excerpts). This makes AI inspection structurally superior — an agent can answer "which field change would satisfy this constraint?" without reverse-engineering expressions. This is a structural differentiator for the MCP surface.
> - **Structured "why not" explanations.** Constraint violations carry structured explanation depth — the failing expression, evaluated field values, guard context, and failing sub-expression — not just a boolean status. This transforms MCP tools from status reporters to causal reasoning engines.
> - **Bounded, non-extensible strategy set.** Four strategies only, each a simple predicate function — not a general solver framework. This makes the proof engine predictable, auditable, and implementable without external dependencies.

## 10. Compilation Snapshot

`CompilationResult` is an aggregation boundary, not a reasoning stage — but it is the artifact that makes the guarantee inspectable. It captures the complete analysis pipeline as one immutable snapshot so consumers can access any stage's output without re-running the pipeline. Even broken programs produce a `CompilationResult` with partial analysis.

```mermaid
flowchart LR
    classDef input fill:#dbeafe,stroke:#60a5fa,color:#1e3a5f
    classDef stage fill:#ede9fe,stroke:#a78bfa,color:#3b1f7e
    classDef artifact fill:#fef3c7,stroke:#f59e0b,color:#78350f

    SRC([source text]):::input
    TS[TokenStream]:::artifact
    ST[SyntaxTree]:::artifact
    TM[TypedModel]:::artifact
    GR[GraphResult]:::artifact
    PM[ProofModel]:::artifact
    SNAP(Compilation\nSnapshot):::stage
    OUT[CompilationResult]:::artifact

    SRC --> SNAP
    TS --> SNAP
    ST --> SNAP
    TM --> SNAP
    GR --> SNAP
    PM --> SNAP
    SNAP --> OUT
```

| | |
|---|---|
| **Output** | `CompilationResult` — `TokenStream Tokens`, `SyntaxTree SyntaxTree`, `TypedModel Model`, `GraphResult Graph`, `ProofModel Proof`, `ImmutableArray<Diagnostic> Diagnostics`, `bool HasErrors` |
| **Consumers** | LS, MCP `precept_compile`, `Precept.From` |

### Incremental compilation model

Given the 64KB ceiling on `.precept` definition size, **re-run everything on change** is the intended compilation model. A keystroke re-lexes, re-parses, re-typechecks, re-analyzes, and re-proves the entire file. There is no incremental invalidation boundary. The size ceiling is the performance argument: at 64KB, full recompilation is fast enough for keystroke-level responsiveness without the complexity of incremental pipelines.

### Contract digest hash

`CompilationResult` should emit a deterministic hash of the compiled definition's semantic content — fields, types, constraints, states, transitions — excluding whitespace and comments. This **contract digest** lets host applications detect definition changes without diffing source text, and grounds the definition versioning story (see below). Paired with a structural diff API (`ContractDiff(old, new)` → added/removed/changed fields, states, constraints), it provides a production deployment safety net.

### Definition versioning

When a `.precept` file changes (field added, state renamed, constraint tightened), persisted `Version` instances compiled against the old definition may fail `Restore` under the new definition's constraints. **This is a known gap — definition migration is out of scope for v1.** The contract digest hash provides change detection; a structural diff API provides change enumeration; but automated migration is deferred. Host applications that need to handle definition evolution must manage the migration externally. The gap is acknowledged so downstream design does not assume migration exists.

**Implementation status:** The wiring exists and merges diagnostics correctly, but four of the five stages are still hollow.

> **Precept Innovations**
> - **Contract digest hash.** A deterministic semantic hash enables definition-change detection without source diffing — no other DSL runtime provides this. It grounds deployment safety and the future migration story.
> - **Always-available analysis snapshot.** `CompilationResult` is produced even from broken input — authoring surfaces always have diagnostics, partial structure, and whatever analysis succeeded. This is not error tolerance; it is progressive intelligence.
> - **Full-pipeline re-run as the correct model.** The 64KB ceiling makes incremental compilation unnecessary, eliminating an entire class of invalidation bugs that plague larger language tooling.

## 11. Lowering

Lowering is the transformation from analysis to execution — and the stage that makes the structural guarantee executable. The evaluator becomes a plan executor that does not reason about semantics at runtime because lowering has already resolved all semantic questions into executable plans. `Precept.From(CompilationResult)` is the sole owner of this transformation — no other code path builds the runtime model. It selectively transforms analysis knowledge into runtime-native shapes rather than copying or referencing compile-time artifacts.

```mermaid
flowchart LR
    classDef stage fill:#ede9fe,stroke:#a78bfa,color:#3b1f7e
    classDef artifact fill:#fef3c7,stroke:#f59e0b,color:#78350f

    CR[CompilationResult]:::artifact
    LOW(Lowering\n— Precept.From —):::stage
    OUT[Precept]:::artifact

    CR -->|"only when !HasErrors"| LOW
    LOW --> OUT
```

| | |
|---|---|
| **Output** | `Precept` — sealed executable model: descriptor tables and slot layout, dispatch indexes, lowered execution plans, constraint-plan indexes, reachability/topology indexes, inspection metadata, fault-site backstops |
| **Catalog role** | Catalog metadata reaches runtime only in lowered semantic form: descriptor identity, resolved operation/function/action identity, constraint descriptors, and proof-owned fault-site residue. Lowering reads catalog metadata transitively through already-resolved model identities — it does not perform fresh catalog lookups for classification. |
| **Consumers** | `Precept.Create`, `Precept.Restore`, `Version` operations, MCP runtime tools, host applications |

### Lowered model inventory

- **`FieldDescriptor`** — field name, `TypeKind`, slot index, modifiers, default-value expression, source origin.
- **`StateDescriptor`** — state name, terminal flag, modifier set (initial, required, irreversible, success, warning, error), available events.
- **`EventDescriptor`** — event name, modifier set, arg descriptors, source origin.
- **`ArgDescriptor`** — arg name, `TypeKind`, optionality, default expression, source origin.
- **`TransitionDispatchIndex`** — state × event → target state + lowered action plan; the routing table the evaluator and inspection surfaces consume directly.
- **`ConstraintPlanIndex`** — constraint plans organized by activation anchor (`always`, `in state`, `to state`, `from state`, `on event`) into precomputed dispatch buckets.
- **`ConstraintDescriptor`** — expression text, source line, guard metadata, `ConstraintActivation` anchor, because text, scope targets.
- **`ConstraintInfluenceMap`** — constraint → contributing fields with expression-text excerpts; enables "which field change would fix this?" without reverse-engineering.
- **`FaultSiteDescriptor`** — defense-in-depth backstop keyed by site identity, linked to `FaultCode` and the prevention `DiagnosticCode`.
- **`SlotLayout`** — field → slot index mapping that addresses the flat evaluation plan's register file.
- **`ReachabilityIndex`** — state → set of states reachable from it; enables structural navigation without re-running the compiler.
- **`ExecutionPlan`** — lowered flat action sequences: slot-addressed opcodes with field-slot references, literal constants, operation codes, and result slots.

### Lowered executable-model contract

| Runtime concern | Lowered structure | Consumed by |
|---|---|---|
| identity | descriptor tables: `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `ConstraintDescriptor` | every runtime API surface |
| storage | slot layout, field-to-slot map, default-value plan, omission metadata | create, restore, fire, update |
| routing | per-state and stateless event-row dispatch indexes, target-state routing metadata | fire and inspect fire |
| topology | reachability index (state → reachable states), pathfinding residue (goal-directed navigation) | structural queries, MCP, AI navigation, inspect |
| execution | lowered flat evaluation plans: slot-addressed opcodes with field-slot references, literal constants, operation codes, and result slots — keyed to descriptors and resolved semantic identities | evaluator |
| recomputation | dependency graph and evaluation order for computed fields | fire, update, restore, inspect |
| access | per-state field access-mode index and query surface | update and inspect update |
| constraints | explicit executable plan indexes for `always`, `in`, `to`, `from`, and `on` anchors | create, restore, fire, update, inspect |
| inspection | row/source/result-shaping metadata for `EventInspection`, `RowInspection`, `UpdateInspection`, `ConstraintResult`, `FieldSnapshot` | inspection surfaces |
| fault backstops | `FaultSite`/fault-site descriptors linked to `FaultCode` and prevention `DiagnosticCode` | impossible-path defense only |

### Descriptor type shapes

The descriptor types referenced throughout this document are first-class sealed types, not string aliases:

- **`FieldDescriptor`** — field name, `TypeKind`, slot index, modifiers (optional, required, computed, readonly, etc.), default-value expression, source origin.
- **`StateDescriptor`** — state name, modifier set (initial, terminal, required, irreversible, success, warning, error), source origin.
- **`EventDescriptor`** — event name, modifier set (initial, forbidden, etc.), arg descriptors, source origin.
- **`ArgDescriptor`** — arg name, `TypeKind`, optionality, default expression, source origin.
- **`ConstraintDescriptor`** — constraint kind (rule/ensure), anchor family, expression text, because text, guard context, source lines, scope targets, `ConstraintActivation`.

These are the runtime face of declarations. Every runtime API surface routes through descriptor identity.

### Expression evaluation model

The executable model is a **flat evaluation plan** — precomputed slot references, operation opcodes, literal constants, and result slots — not a recursive tree interpreter. Think of it as register-based bytecode where "registers" are field slots. This makes evaluation predictable-time, cache-friendly, and trivially serializable for inspection. Tree-walk interpretation is the dominant pattern in the surveyed DSL-scale systems — CEL uses tree-walking via `Interpretable.Eval()`, OPA/Rego uses top-down tree evaluation, Dhall normalizes via recursive substitution, Pkl evaluates lazily through its AST — and it would be correct for Precept. However, Precept's evaluation is tighter than expression evaluation: it executes a fixed action/constraint plan against a known slot layout. The flat plan trades the simplicity of tree-walking for predictable-time execution, inspectability (MCP tools can display plan structure without tracing recursive calls), and determinism properties that make Precept's runtime distinctive. This is a design decision, not a researched consensus — the surveyed systems succeed with tree-walking at their scale.

### Lowering is restructuring, not renaming

The runtime model is organized for execution, not for semantic analysis. Constraint plans are grouped by activation anchor, not by source declaration order. Action plans are grouped by transition row, not by field. The runtime model is a dispatch-optimized index, not a renamed analysis model. The surveyed systems confirm this pattern: CEL's `Program` is a lowered `Interpretable` tree optimized for evaluation, not a copy of the checked AST; OPA's `Compiler` builds internal rule indexes that restructure policy for efficient top-down evaluation; XState v5 transforms machine configuration into a normalized internal model with precomputed transition maps. An implementer must NOT map `TypedModel` types 1:1 to runtime types — lowering is a selective, restructuring transformation.

### Constraint activation indexes

The five constraint-plan families (`always`, `in`, `to`, `from`, `on`) are accessed through four precomputed activation indexes, built once during lowering and keyed to descriptor identity:

- **Always index** (global) — rules and ensures with no state or event anchor; active on every operation.
- **State activation index** (`StateDescriptor`, `ConstraintActivation`) — `InState`, `FromState`, and `ToState` anchors.
- **Event activation index** (`EventDescriptor`) — `on Event ensure` anchors.
- **Event availability index** (`StateDescriptor?`, `EventDescriptor`) — available-event scope; null state key for stateless precepts.

The `ConstraintActivation` discriminant distinguishes whether a constraint binds to the current state, the source state, or the target state of a transition. Callers look up a prebuilt bucket, not compute activation at dispatch time. **`ConstraintActivation` should be cataloged** — it is language-surface knowledge that consumers (type checker, lowering, evaluator, MCP, LS) need as structured metadata, not an internal implementation enum.

### `Version` serialization contract

Host applications must persist and hand back to `Restore` the following: the current state name (or stateless marker), and field values keyed by field name. The serialization shape is `(string StateName, IDictionary<string, object?> FieldValues)` — or equivalently, `(StateDescriptor?, SlotArray)` at the descriptor level. Hosts own the serialization format (JSON, binary, database columns); Precept owns the contract for what data is required. `Restore` validates the supplied data against the current definition's constraints — it does not trust the persisted shape.

### Current surface

The stable runtime contract is descriptor-backed. Current public stubs still expose string placeholders and string-selected entry points. Those strings are provisional implementation placeholders, not the architectural end state.

**Implementation status:** `Precept.From` currently checks `HasErrors` and then throws `NotImplementedException`.

> **Precept Innovations**
> - **Flat evaluation plans with slot-addressed opcodes.** Expressions are not tree-walked — they are precomputed into flat, cache-friendly execution plans with field-slot references and operation codes. This makes evaluation predictable-time and trivially inspectable. No other DSL runtime in this category commits to flat evaluation.
> - **Dispatch-optimized constraint indexes.** Constraints are grouped by activation anchor into precomputed buckets — the evaluator never scans or filters at dispatch time. Five anchor families, four activation indexes, built once during lowering.
> - **`ConstraintInfluenceMap` as a lowered artifact.** The dependency from constraints to contributing fields, with expression-text excerpts, becomes a first-class runtime artifact — enabling AI agents to reason causally about constraint satisfaction.

## 12. Runtime surface and operations

Once a valid `Precept` exists, four operations govern entity lifecycle. The evaluator is a shared plan executor — it consumes only lowered artifacts and executes prebuilt plans. Execution semantics are fully determined at lowering time.

```mermaid
flowchart TD
    classDef artifact fill:#fef3c7,stroke:#f59e0b,color:#78350f
    classDef runtime fill:#d1fae5,stroke:#34d399,color:#064e3b
    classDef leaf fill:#a7f3d0,stroke:#10b981,color:#064e3b

    P[Precept]:::artifact

    CREATE(Create):::runtime
    RESTORE(Restore):::runtime
    EVAL(Evaluator):::runtime
    STRUCT(Structural\nqueries):::runtime

    P --> CREATE
    P --> RESTORE
    P --> EVAL
    P --> STRUCT

    FIRE([Fire · InspectFire]):::leaf
    UPD([Update · InspectUpdate]):::leaf
    FAULT([Fault backstops]):::leaf

    EVAL --> FIRE
    EVAL --> UPD
    EVAL --> FAULT
```

### Evaluator

| | |
|---|---|
| **Input** | `Precept`, `Version`, descriptor-keyed arguments or patches, lowered execution plans, constraint-plan indexes, fault-site backstops |
| **Output** | `EventOutcome`, `UpdateOutcome`, `RestoreOutcome` (commit); `EventInspection`, `UpdateInspection`, `RowInspection` (inspect); `Fault` (impossible-path only) |

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

| | |
|---|---|
| **Input** | `Precept`; lowered defaults, `InitialState`, `InitialEvent`, arg descriptors, fire-path runtime plans |
| **Output** | `EventOutcome` (commit) or `EventInspection` (inspect). Success yields `Applied(Version)` or `Transitioned(Version)`. |

### Restore

Restore reconstitutes persisted data under the current definition. It validates rather than trusts — it runs constraint evaluation but intentionally bypasses access-mode restrictions, because persisted data represents a prior valid state, not an active field edit. **Restore recomputes computed fields BEFORE constraint evaluation, not after** — persisted data may include stale computed-field values, and constraints must evaluate against recomputed results. The compiler-result-to-runtime survey shows that XState v5 provides the closest precedent for state reconstitution: `createActor(machine, { snapshot: JSON.parse(persistedSnapshot) })` restores a previously serialized snapshot. However, XState performs no constraint re-evaluation on restore — it trusts the persisted snapshot shape. Precept's `Restore` deliberately does not trust: it re-validates against the current definition's constraints, catching both stale computed values and definition-evolution mismatches.

| | |
|---|---|
| **Input** | `Precept`; caller-supplied persisted state and fields; lowered descriptors, slot validation, recomputation, restore constraint plans |
| **Output** | `RestoreOutcome` — `Restored(Version)`, `RestoreConstraintsFailed(IReadOnlyList<ConstraintViolation>)`, or `RestoreInvalidInput(string Reason)` |

### Fire

Fire is the core state-machine operation. Routing, action execution, transition, recomputation, and constraint evaluation are a single atomic pipeline — not composable steps callers assemble — because partial execution would violate the determinism guarantee.

| | |
|---|---|
| **Input** | `Version`; event/arg descriptors, row dispatch tables; lowered action plans, recomputation index; anchor-plan indexes, fault sites |
| **Output** | `EventOutcome` — `Transitioned`, `Applied`, `Rejected`, `InvalidArgs`, `EventConstraintsFailed`, `Unmatched`, provisional `UndefinedEvent`. `EventInspection` / `RowInspection` for inspect. |

Constraint identity survives into `ConstraintResult` and `ConstraintViolation` through `ConstraintDescriptor`. Routing uses descriptor-backed row identity. The runtime API survey highlights a gap in every surveyed state machine runtime: XState's `snapshot.can(event)` returns a boolean with no distinction between "no transition defined for this event" and "a transition is defined but its guard returned false" — both result in `false`. After `actor.send(event)`, if nothing changed, the snapshot is simply identical to the pre-send snapshot with no error or rejection signal. Erlang gen_statem's `keep_state_and_data` signals "handled, no change" but provides no guard/routing discrimination to the caller. Precept structurally distinguishes `Unmatched` (no row matched the state × event combination) from `Rejected` or `EventConstraintsFailed` (rows matched but guard or constraint evaluation prevented the transition) — giving callers and AI agents precise causal information about why an event did not produce a transition.

### Update

Update governs direct field edits under access-mode declarations and constraint evaluation. `InspectUpdate` additionally evaluates the event landscape over the hypothetical post-patch state. `Update` exists because Precept is not a state machine runtime — it is a domain integrity engine that owns the data layer alongside the lifecycle layer. Fields have access-mode declarations per state, always-constraints, recomputed dependencies, and structured outcomes for denied or constrained writes. A pure event/transition mechanism would leave direct field edits ungoverned; `Update` closes that gap without routing every data change through an event.

| | |
|---|---|
| **Input** | `Version`; field descriptors, per-state access facts; recomputation dependencies; `always`/`in` constraint plans, event-prospect evaluation |
| **Output** | `UpdateOutcome` — `FieldWriteCommitted`, `UpdateConstraintsFailed`, `AccessDenied`, `InvalidInput`. `UpdateInspection` for inspect. |

### Structured outcomes

The structural guarantee means that a valid executable model communicates entirely through structured outcomes. There are three result families, and collapsing them would undermine the guarantee. The surveyed systems confirm the value of structured result types: CEL returns a three-value result `(ref.Val, *EvalDetails, error)` distinguishing evaluation results, error values within the type system, and infrastructure failures; OPA returns `ResultSet` with per-expression values and bindings; Eiffel's Design-by-Contract model distinguishes precondition violations from postcondition violations from class invariant violations. The outcome-type taxonomy survey broadens this evidence: gRPC's status code model distinguishes `FAILED_PRECONDITION` (business rule violation) from `INVALID_ARGUMENT` (caller input error) from `INTERNAL` (infrastructure failure) — the closest surveyed precedent for Precept's business-outcome / boundary-validation / fault taxonomy. Kubernetes `Status` carries ~18 machine-readable `Reason` values for the same purpose. F# Result and Rust `Result<T, E>` with typed error enums provide the pattern-matching model Precept's outcome DUs follow. Yet the outcome-type taxonomy survey also reveals that most state machine runtimes cannot distinguish these categories at the type level: Temporal collapses validator rejection and handler failure into the same `ApplicationError` type (disambiguated only by a string `Type()` field the application must set); XState's `send()` returns `void` with no acknowledgment; Erlang gen_statem relies on convention-based reply tuples. Precept's three-family taxonomy (diagnostics, runtime outcomes, faults) extends the gRPC/Kubernetes pattern to the full lifecycle operation surface:

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

Inspection shares the same lowered plans as commit. It is not a second evaluator. The surveyed systems confirm the value of preview/inspect patterns but differ in depth: Terraform `plan` previews infrastructure changes before apply; XState v5's `machine.transition()` computes the next state without side effects; OPA's partial evaluation pre-computes policy results with unknown inputs; Temporal's update validators run validation logic before committing workflow state. The runtime API survey adds further detail: XState v5 exposes pure transition functions — `transition(machine, snapshot, event)` returns `[nextSnapshot, actionsToExecute]` without executing actions or mutating actor state, and `getNextTransitions(snapshot)` enumerates enabled transitions without firing any event. These are the closest surveyed precedent for Precept's event-availability index and `InspectFire`. However, XState's inspection depth stops at "would this event cause a change?" (boolean) — it does not preview constraint evaluation, per-row prospects, or structured outcomes. Precept's inspection goes further — it previews every possible transition from any state with full constraint evaluation and per-row structured outcomes, using the same lowered execution plans as the commit path.

### Constraint query contract

Three tiers, additive in specificity:

- **Definition** — `Precept.Constraints`: every declared `ConstraintDescriptor` in the definition. Always available from the lowered model.
- **Applicable** — `Version.ApplicableConstraints`: the zero-cost subset active for the current state and context. Available from any live `Version`. (This is a runtime convenience for API consumers, not an evaluation necessity — the evaluator always uses activation indexes directly.)
- **Evaluated** — `ConstraintResult` / `ConstraintViolation`: what was actually checked during a specific operation. Embedded in outcome and inspection results only.

### Structured "why not" violation explanations

When `Fire` returns `Rejected` or `EventConstraintsFailed`, or `Update` returns `UpdateConstraintsFailed`, the outcome carries `ConstraintViolation` objects with **structured explanation depth**: the failing constraint descriptor, the expression text, the evaluated field values at the point of failure (`{ field: value }` pairs), the guard context that scoped the constraint (if guarded), and the specific sub-expression that failed. This is not a formatting concern — it is cheap to compute during evaluation and transforms MCP and inspection from "it failed" to "it failed because X was 3 and the constraint requires X > 5."

The multi-span attribution pattern from the Rust borrow checker provides relevant precedent for this design: a single borrow-checker diagnostic carries multiple labeled source spans (primary span for the conflict, secondary spans for the causal chain — "first mutable borrow occurs here," "second mutable borrow occurs here," "first borrow later used here"). Precept's `ConstraintViolation` follows the same structural principle — the failing expression, the contributing field values, and the guard context form a labeled causal chain, not a single error site. Infer (Meta) takes a similar approach with `bug_trace` — an ordered array of inter-procedural trace steps, each attributed to a source location with a description of what the analysis observed.

### Operation-facing plan selection

| Operation | Required lowered contract |
|---|---|
| `Create` | default-value plan, initial-state seed, optional initial-event descriptor/arg contract, then shared fire-path execution |
| `Restore` | slot population, descriptor validation, recomputation, `always` + `in <current>` constraint plans; no access checks, no row dispatch |
| `Fire` | row dispatch, action plans, recomputation, `always` + `from <current>` + `on <event>` + `to <target>` constraint plans |
| `Update` | access-mode index, patch validation, recomputation, `always` + `in <current>` constraint plans; inspect additionally runs event-prospect evaluation |

> **Precept Innovations**
> - **Structured outcomes taxonomy.** Every runtime operation communicates through a structured outcome — success, domain rejection, boundary validation, or impossible-path fault. There are no exceptions, no error codes, no untyped failures. An AI agent or host application can pattern-match on outcome type and always know what happened and why.
> - **Inspection API.** `InspectFire` and `InspectUpdate` preview every possible action and its outcome without executing anything — using the same lowered plans as commit. No other state machine library or rules engine provides read-only preview of transitions with full constraint evaluation before committing.
> - **Causal violation explanations.** Constraint violations carry structured explanation depth — evaluated field values, guard context, failing sub-expression — not just a boolean. This makes MCP tools causal reasoning engines, not status reporters.
> - **Restore with recomputation-first constraint evaluation.** Persisted data is never trusted — Restore recomputes computed fields before evaluating constraints, catching stale computed values that would otherwise pass through silently.

## 13. Type and immutability strategy

All compile-time and runtime types in Precept are deeply immutable. This is not a style preference — it is a correctness requirement imposed by the language server's concurrency model. On every document edit, the LS runs the full pipeline and atomically swaps the held `CompilationResult` reference via `Interlocked.Exchange`. A handler thread that read the old reference before the swap must see a fully consistent snapshot, with no possibility of torn state. Deep immutability — `ImmutableArray<T>` and `ImmutableDictionary<TK,TV>` for all collections, `init`-only properties on all record types, no mutable types exposed — is what makes this guarantee structural rather than convention-dependent. The compilation-result-type survey reveals that immutability is not the DSL-scale consensus: OPA's `ast.Compiler` is mutated during compilation, Kotlin K2's FIR tree is mutated in phases, Swift's `ASTContext` is mutated by the type checker, Go's `types.Info` is caller-allocated mutable maps, and Dafny/Boogie mutate their program representations in place. Only CEL (`Ast`), Dhall, CUE (`cue.Value`), and Pkl (`PObject`) produce immutable compilation results. Precept's immutable `CompilationResult` is a deliberate, LS-driven choice — not inherited consensus.

The choice of C# type kind for each artifact follows from its role. Stage artifacts (`TokenStream`, `SyntaxTree`, `TypedModel`, `GraphResult`, `ProofModel`) and `CompilationResult` are `sealed record class` — immutable snapshots with value equality, making test assertions direct structural comparisons rather than field-by-field checks. `Diagnostic` is `readonly record struct` — small, value-typed, and zero-allocation when stored in collections, reflecting its high-volume, short-lived role. `Precept` is `sealed class`, not a record — it has factory methods (`From`) and carries behavior, making it a behavior-bearing object rather than a data bag. `Version` is `sealed record class` — an immutable entity snapshot with value equality, consistent with its role as the atomic unit of state that operations return. There are no interfaces and no abstract classes: each type has exactly one implementation. Interfaces are added only when a second implementation appears or a consumer requires substitution — never speculatively.

On every document edit, the language server runs the full pipeline (`Compiler.Compile(source)`) and atomically replaces its held `CompilationResult` reference. Incremental compilation infrastructure — Roslyn's red-green trees, rust-analyzer's salsa database — solves a problem that does not exist at Precept's DSL scale, where the full pipeline runs in microseconds. The surveyed DSL-scale systems uniformly confirm this: OPA/Regal recompiles the full module set on single-file edits; Dhall's LSP runs the full pipeline (parse → resolve → type check) on each save; Jsonnet's language server re-parses and re-evaluates on each change; CEL compiles single expressions in one call with no incremental infrastructure. None of these systems has found incremental recompilation necessary at their scale. The swap is safe for concurrent LSP requests because `CompilationResult` is fully immutable — no locks are needed beyond `Interlocked.Exchange` on the reference itself.

The language server calls `Compiler.Compile(source)` directly — same process, no published NuGet package, no serialization boundary. This is the dominant pattern at DSL scale: Regal imports OPA's parser and compiler as Go libraries in-process; Dhall's LSP lives in the same monorepo and directly calls `Dhall.Parser`, `Dhall.TypeCheck`, and `Dhall.Core`; Jsonnet's language server imports `go-jsonnet` as a library; CUE's LSP is built into the CLI binary itself. The LS-to-compiler code ratio at this scale is 1:3 to 1:10. Single-process integration eliminates serialization overhead, IPC latency, and version-mismatch risk. A separate compiler process or package is warranted only when the compiler is shared across multiple host tools with independent release cycles — a threshold Precept has not reached and may never reach.

> **Precept Innovations**
> - **Immutability as a correctness property, not just a style preference.** The LS atomic swap pattern depends on deep immutability — it is not optional. This propagates through every artifact type in the system.
> - **Full recompile as a deliberate, researched choice.** Not a simplification or a TODO — a surveyed, right-sized decision. DSL-scale systems universally use full recompile; incremental infrastructure would add complexity with no user-visible benefit at this scale.

## 14. TextMate grammar generation

The TextMate grammar (`tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`) is a **generated artifact**, not a hand-edited file. The grammar generator reads catalog metadata and emits the complete grammar — keyword patterns, operator patterns, type name patterns, declaration-level patterns, and block delimiters. This means the grammar is always in sync with the language specification: no drift between syntax highlighting and actual grammar is possible.

### Catalog contributions to the grammar

| Catalog | What it contributes |
|---|---|
| `Tokens` | Keyword patterns (alternation of all `TokenCategory.Keyword` members), operator patterns (symbol sequences from `TokenMeta`), punctuation patterns |
| `Types` | Built-in type name patterns (alternation of all surfaced `TypeKind` display names) |
| `Constructs` | Declaration-level patterns (anchor keywords for each `ConstructKind`), block delimiters, slot-level structure hints |
| `Operators` | Operator precedence groups (used for scope nesting in the grammar to support bracket matching and indentation) |

The same catalog metadata drives LS completions, LS hover content, LS semantic tokens, and MCP `precept_language` vocabulary. Adding a keyword, type, or operator to the appropriate catalog automatically updates every surface — grammar, completions, hover, semantic tokens, and MCP output.

### Anti-pattern

Do NOT add patterns directly to `tmLanguage.json`. Add the language element to the appropriate catalog, and let the grammar generator pick it up. Hand-editing the grammar file creates drift between the grammar and the language specification — the exact problem the catalog-driven architecture is designed to prevent.

> **Precept Innovations**
> - **Single source of truth for language surface.** Grammar, completions, hover, semantic tokens, and MCP vocabulary are all derived from the same catalog definitions. No other DSL tooling in this category has this level of surface coherence — most maintain separate grammar files, completion lists, and documentation that drift independently.
> - **Grammar generation, not grammar authoring.** The TextMate grammar is a build output. Syntax highlighting correctness is a property of catalog completeness, not of grammar maintenance. A new keyword highlights correctly the moment its catalog entry is added.
> - **Zero-drift guarantee.** Because the grammar is generated from the same metadata the parser and type checker consume, it is structurally impossible for syntax highlighting to disagree with actual parse behavior.

## 15. MCP integration

Precept ships five MCP tools as **primary distribution surfaces** — not integrations bolted on afterward. The MCP server is an AI-first design concern: every architectural decision accounts for AI agent consumers alongside human developers.

### Tool inventory

| Tool | Purpose | Core API surface |
|---|---|---|
| `precept_language` | Complete DSL vocabulary — keywords, operators, scopes, constraints, pipeline stages | Catalogs directly |
| `precept_compile(text)` | Parse, type-check, analyze; returns typed structure + diagnostics | `CompilationResult` |
| `precept_inspect(text, currentState, data, eventArgs?)` | Read-only preview of what each event would do | `Precept` + inspection runtime |
| `precept_fire(text, currentState, event, data?, args?)` | Single-event execution for step-by-step tracing | `Precept` / `Version.Fire` |
| `precept_update(text, currentState, data, fields)` | Direct field editing to test `edit` declarations and constraints | `Precept` / `Version.Update` |

### Architectural principles

**Thin wrappers.** MCP tools are thin wrappers around core APIs — domain logic lives in `src/Precept/`, not in the MCP layer. If a tool method exceeds ~30 lines of non-serialization code, the logic belongs in the core.

**Catalog-derived vocabulary.** The `precept_language` tool derives its vocabulary directly from catalog metadata. When a new keyword, type, or construct is added to the catalogs, it appears in `precept_language` output automatically — no MCP code change required.

**Structured outcomes for AI consumption.** Fire, inspect, and update return structured outcomes designed for AI agent consumption — causal reasoning, not just status codes. Constraint violations carry expression text, evaluated values, and guard context. Inspection results carry per-row prospects, effects, and constraint results.

**MCP as the primary research instrument.** The intended workflow for both AI agents and human developers: use `precept_compile` and `precept_language` BEFORE reading source code. The MCP tools provide the authoritative view of what the language is and what a definition means.

### AI-first design principle

Public API contracts, diagnostic structures, and DSL constructs must be understandable by AI agents without contextual human knowledge. This means: structured types over string messages, deterministic output shapes, causal explanations in violation results, and complete vocabulary exposure through `precept_language`.

The `ConstraintInfluenceMap` (§9 innovation) would make MCP tools causal reasoning engines: given a constraint failure, an AI agent could determine "which field change would satisfy this constraint?" without reverse-engineering expression semantics — the influence map provides the dependency graph directly.

> **Precept Innovations**
> - **MCP vocabulary from catalogs.** The `precept_language` vocabulary is generated from the same catalogs that drive grammar and completions. A developer (human or AI) who knows the MCP vocabulary already knows the language surface — no redundancy, no drift.
> - **Inspection as a first-class MCP operation.** `precept_inspect` provides read-only preview of every possible transition from any state — with full constraint evaluation, per-row prospects, and structured outcomes. No other MCP tool in any category provides this depth of preview.
> - **Causal reasoning in tool output.** Structured "why not" explanations in fire/update results transform MCP from status reporting to causal reasoning — an AI agent can explain failures without access to source code.
> - **AI-first, not AI-adapted.** The MCP surface was designed alongside the core API, not retrofitted. Structured outcomes, deterministic shapes, and complete vocabulary exposure are architectural requirements, not afterthoughts.

## 16. Language-server integration

The language server consumes pipeline artifacts by responsibility. Each LS feature reads from exactly the artifact that owns the information it needs.

**Lexical classification** (keyword, operator, punctuation, literal, comment) — reads `TokenStream` + `TokenMeta`. Not `SyntaxTree`, not `TypedModel`.

**Syntax-aware features** (outline, folding, recovery) — reads `SyntaxTree`. Not `TypedModel`.

**Diagnostics** — reads merged `CompilationResult.Diagnostics`. Not per-stage polling.

**Semantic tokens for identifiers** — reads `TypedModel` symbol/reference bindings; source spans come from back-pointers to originating syntax nodes (see §6). Not token categories alone.

**Completions** — reads catalogs for candidate inventory, `SyntaxTree` for local parse context, `TypedModel` for scope/binding/expected type. Not `GraphResult` or `ProofModel`.

**Hover** — reads `TypedModel` semantic identity + catalog documentation/signatures; source location from the back-pointer to the originating syntax node. Not raw syntax.

**Go-to-definition** — reads `TypedModel` reference binding + declaration-origin back-pointer. Not syntax-tree guessing.

**Preview/inspect** — reads lowered `Precept` + runtime inspection, only when `!HasErrors`. Not `CompilationResult` after lowering.

**Graph/proof explanation** — reads `GraphResult` and `ProofModel` when explicitly surfacing unreachable-state or proof information. Not for everyday completion/hover/tokenization.

Two hard rules: (1) Do not make semantic LS features walk `SyntaxTree` to answer semantic questions — if the `TypedModel` plus its back-pointers cannot answer the question, the inventory is underspecified (see §6 anti-mirroring rules). (2) Do not make preview/runtime LS features consume `CompilationResult` after lowering succeeds.

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
| Typed model | diagnostics-only stub | flat semantic inventory with back-pointers and anti-mirroring contract |
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
