# Language Server & Compiler Integration Survey

> **Purpose**: Raw external research on how language servers and tooling integrate with compiler internals across systems of different scales. NO interpretation for Precept, NO recommendations — document only.
>
> **Date**: 2025-01
>
> **Scope**: 14 systems across three tiers (big compilers, mid-scale typed languages, small/DSL-scale)

---

## Roslyn / OmniSharp (C#)

### Shared Code vs Separate Implementation

Roslyn IS the compiler — the language server and the compiler are the same codebase. Microsoft redesigned the C# and VB compilers as a "compiler-as-a-platform" with layered APIs explicitly designed for IDE consumption. OmniSharp (the cross-platform LS) sits on top of Roslyn's Workspaces API layer.

### Compiler APIs Called by the Language Server

Roslyn exposes four API layers, from low to high:

1. **Compiler APIs** — the core pipeline:
   - `SyntaxTree` — immutable, full-fidelity parse trees (every character preserved, including trivia/whitespace)
   - `Compilation` — represents a single invocation of the compiler; binds syntax trees to references
   - `SemanticModel` — the queryable semantic layer; per-syntax-tree facade over the `Compilation`
   - `ISymbol` hierarchy — `INamedTypeSymbol`, `IMethodSymbol`, `IPropertySymbol`, etc.

2. **Diagnostic APIs** — `Diagnostic`, `DiagnosticAnalyzer`, `CodeFixProvider`

3. **Workspaces APIs**:
   - `Workspace` — abstract host container
   - `Solution` → `Project` → `Document` — immutable snapshot chain
   - `MSBuildWorkspace` — loads `.csproj`/`.sln` files
   - `AdhocWorkspace` — for testing

4. **Code Actions / Refactoring** — `CodeRefactoringProvider`

```
┌──────────────────────────────────────────────────┐
│                  OmniSharp / VS                  │
│              (LSP / VS Extension)                │
├──────────────────────────────────────────────────┤
│         Roslyn Workspaces API Layer              │
│  Workspace → Solution → Project → Document       │
├──────────────────────────────────────────────────┤
│          Roslyn Compiler API Layer               │
│  SyntaxTree  Compilation  SemanticModel  ISymbol │
├──────────────────────────────────────────────────┤
│           Roslyn Compiler Pipeline               │
│  Lexer → Parser → Binder → Lowerer → Emitter    │
└──────────────────────────────────────────────────┘
```

### How Type Information Drives Completions

`SemanticModel.LookupSymbols(position)` returns all symbols accessible at a given source location. The completion provider calls this, filters by context (e.g., member access vs. statement level), and maps `ISymbol` instances to completion items. Type inference results are available directly from the `SemanticModel` — no separate analysis pass needed.

### Diagnostics: Push vs Pull

Roslyn supports both:
- `GetDiagnostics()` / `GetSyntacticDiagnostics()` / `GetSemanticDiagnostics()` — pull model, called on demand per `Document`
- `DiagnosticAnalyzer` — the analyzers are invoked by the host (VS or OmniSharp) and results are pushed to the IDE diagnostics panel
- The Workspaces layer fires `WorkspaceChanged` events when `Solution` snapshots change, which triggers diagnostic re-analysis

### Incremental Recompilation Strategy

**Immutable snapshots with structural sharing.** Every edit creates a new `Solution` snapshot. `SyntaxTree` uses red-green trees — two layers where the green (inner) tree is immutable and shared, and the red (outer) tree is rebuilt cheaply. `Compilation` is incrementally derived from the previous one — only re-analyzing what changed. `SemanticModel` is computed lazily per-tree.

### State Persistence Between Requests

The `Workspace` object holds the current `Solution` snapshot. It persists across all requests. Each LSP request reads the latest snapshot — there's no per-request re-parsing. The snapshot model means reads and writes don't conflict.

### Cancellation Handling

`CancellationToken` is threaded through essentially every API call. When a new keystroke arrives, the previous request's token is cancelled. Roslyn's internal APIs (binding, flow analysis) all check cancellation cooperatively, throwing `OperationCanceledException`.

### Public API vs LS-Internal Boundary

Roslyn's public APIs (`Microsoft.CodeAnalysis`, `Microsoft.CodeAnalysis.CSharp`) are NuGet packages usable by anyone. OmniSharp uses these same public APIs — there's no hidden LS-only surface. Some analyzers use `internal` APIs via `InternalsVisibleTo`, but the stated goal is for the public API to be sufficient.

### LS Codebase Size Relative to Compiler

OmniSharp itself is relatively thin (~50K lines). Roslyn is ~4M+ lines. The ratio is roughly 1:80. OmniSharp is mostly glue between LSP transport and Roslyn Workspaces APIs.

**Key repos**: `dotnet/roslyn`, `OmniSharp/omnisharp-roslyn`

---

## TypeScript / tsserver

### Shared Code vs Separate Implementation

Same codebase. The TypeScript compiler (`tsc`) and the language service (`tsserver`) share all core code. The language service IS the compiler, plus additional API surface for IDE features. The `LanguageService` interface wraps the compiler pipeline.

### Compiler APIs Called by the Language Server

The `LanguageService` interface exposes:

```typescript
interface LanguageService {
  getSyntacticDiagnostics(fileName: string): Diagnostic[];
  getSemanticDiagnostics(fileName: string): Diagnostic[];
  getCompletionsAtPosition(fileName: string, position: number, ...): CompletionInfo;
  getCompletionEntryDetails(fileName: string, position: number, name: string, ...): CompletionEntryDetails;
  getQuickInfoAtPosition(fileName: string, position: number): QuickInfo;
  getDefinitionAtPosition(fileName: string, position: number): DefinitionInfo[];
  getReferencesAtPosition(fileName: string, position: number): ReferenceEntry[];
  getSignatureHelpItems(fileName: string, position: number, ...): SignatureHelpItems;
  getEmitOutput(fileName: string): EmitOutput;
  getProgram(): Program;
  // ... ~60 more methods
}
```

The host implements `LanguageServiceHost`:

```typescript
interface LanguageServiceHost {
  getScriptFileNames(): string[];
  getScriptVersion(fileName: string): string;
  getScriptSnapshot(fileName: string): IScriptSnapshot | undefined;
  getCompilationSettings(): CompilerOptions;
  // ...
}
```

The `DocumentRegistry` is a shared cache that allows multiple `LanguageService` instances to share `SourceFile` ASTs when the file content is the same.

```
┌───────────────────────────────────────────────┐
│              tsserver (LSP host)              │
├───────────────────────────────────────────────┤
│            LanguageService API                │
│  getSyntacticDiagnostics()                    │
│  getCompletionsAtPosition()                   │
│  getDefinitionAtPosition()                    │
│  getQuickInfoAtPosition()                     │
├───────────────────────────────────────────────┤
│   LanguageServiceHost   DocumentRegistry      │
│   (file snapshots)      (shared SourceFiles)  │
├───────────────────────────────────────────────┤
│           TypeScript Compiler Core            │
│  Scanner → Parser → Binder → Checker → Emitter│
└───────────────────────────────────────────────┘
```

### How Type Information Drives Completions

`getCompletionsAtPosition()` internally calls `TypeChecker.getTypeAtLocation()` and `TypeChecker.getSymbolAtLocation()`. The checker resolves the expression type, then enumerates members/properties of that type. For dot-completions, it gets the type of the expression left of the dot and lists all accessible members. The type checker's internal symbol tables are queried directly.

### Diagnostics: Push vs Pull

**Pull model.** `tsserver` calls `getSyntacticDiagnostics()` and `getSemanticDiagnostics()` when the editor requests them (typically after a debounced idle period). The compiler pipeline has decoupled phases — syntactic diagnostics come from the parser (cheap), semantic diagnostics require the type checker (expensive). This separation is a design goal: "Operations that depend on only a single file can be accomplished by the language service without any other knowledge. ... Operations that require knowledge of the project as a whole require more investment."

### Incremental Recompilation Strategy

**On-demand, per-file snapshots.** `ScriptSnapshot` provides the host's view of a file. When content changes, the host provides a new snapshot. TypeScript uses `updateLanguageServiceSourceFile()` to incrementally re-parse only the changed spans using the syntax tree's change range. The `Program` object is recreated but shares unchanged `SourceFile` instances. Type checking is lazy — the `TypeChecker` only processes files/symbols as they're queried.

### State Persistence Between Requests

`tsserver` keeps the `LanguageService` alive. The `Program` (collection of `SourceFile` ASTs) persists and is updated incrementally. The `DocumentRegistry` caches `SourceFile` instances keyed by `(fileName, compilationSettings, scriptSnapshot key)`. Symbol resolution results are cached within the `TypeChecker` for the current `Program`.

### Cancellation Handling

`tsserver` uses a cancellation token mechanism. When a new request arrives, previous pending requests can be cancelled. The compiler pipeline checks cancellation at statement granularity during type checking. The design explicitly calls out: "Apply edits, request a new baseline of diagnostics, cancel the request, change."

### Public API vs LS-Internal Boundary

The `LanguageService` interface is the public API — it's what `tsserver`, VS Code's built-in TS extension, and third-party tools all use. Many internal compiler types (`TypeChecker`, `Binder`, `Symbol` internal interfaces) are not public API, though they're accessible in practice since TypeScript ships as JS modules. The `ts.createLanguageService()` factory is the official entry point.

### LS Codebase Size Relative to Compiler

The language service is part of the compiler codebase. `checker.ts` alone is ~50K lines. The LS-specific code (completions, quick info, go-to-definition, etc.) in `src/services/` is ~30K lines. The total compiler is ~200K lines. So LS-specific code is roughly 15% of the total.

**Key repo**: `microsoft/TypeScript`; design doc at `wiki/Using-the-Language-Service-API`

---

## rust-analyzer

### Shared Code vs Separate Implementation

**Completely separate from `rustc`.** rust-analyzer is a ground-up reimplementation of Rust's frontend (parsing, name resolution, type inference, macro expansion) designed specifically for IDE use. It does NOT call into `rustc` at all. It uses its own parser (`rust-analyzer/crates/parser`), its own name resolver, its own type checker.

### Compiler APIs Called by the Language Server

None from `rustc`. rust-analyzer has its own internal API organized around the `salsa` incremental computation framework. Key internal "queries" (salsa functions):

- `parse(FileId)` → `Parse<SourceFile>` — file-level syntax tree
- `def_map(CrateId)` → `DefMap` — per-crate name resolution
- `infer(DefId)` → `InferenceResult` — per-function type inference
- `ty(DefId)` → `Ty` — type of a definition
- `resolve_path(...)` — path resolution in scope

```
┌──────────────────────────────────────────────────┐
│              rust-analyzer LSP layer             │
│        (handles, completions, diagnostics)       │
├──────────────────────────────────────────────────┤
│             IDE Layer (hir API)                   │
│   Semantics  Function  Type  Module  Struct       │
├──────────────────────────────────────────────────┤
│         salsa Incremental DB                      │
│  parse()  def_map()  infer()  ty()  macro_expand()│
├──────────────────────────────────────────────────┤
│        Crate-level Analysis                       │
│  Name Resolution  Type Inference  Macro Expansion │
├──────────────────────────────────────────────────┤
│              Syntax (rowan)                       │
│      Lossless CST  Green/Red Tree                 │
└──────────────────────────────────────────────────┘
```

### How Type Information Drives Completions

The `hir` (high-level intermediate representation) layer provides `Semantics` — the main entry point for IDE queries. For completions, it resolves the expression type via `Semantics::type_of_expr()`, then enumerates methods, fields, and associated items available on that type. Method resolution requires trait solving (which traits are in scope, which impls apply).

### Diagnostics: Push vs Pull

**Pull model with background computation.** The LSP handler requests diagnostics, which triggers salsa queries. However, rust-analyzer also runs background "prime caches" tasks on idle to pre-compute common queries, so that when diagnostics are requested, the results may already be cached.

### Incremental Recompilation Strategy

**salsa-based fine-grained incremental computation.** This is the "query-based compiler" architecture from Matklad's "Three Architectures" blog post:

1. All function calls in the compiler are instrumented as salsa queries
2. salsa records which queries were called during each query's execution (dependency tracking)
3. On a file change, only the `parse()` query for that file is invalidated
4. Dependents are re-validated lazily: if the result of a dependency hasn't actually changed (despite its input changing), propagation stops — this is called "early cutoff"

This is necessary for Rust because:
- Macros can introduce new top-level declarations (breaks map-reduce approach)
- No header files (breaks snapshot-after-headers approach)  
- The compilation unit (crate) is too large for simple laziness
- Intra-crate name resolution requires fixed-point iteration intertwined with macro expansion

The key insight from the blog post: "We compensate for the deficit of laziness with incrementality."

### State Persistence Between Requests

The salsa database persists across requests. It contains all cached query results and their dependency edges. The `AnalysisHost` owns the database and provides `Analysis` snapshots (read-only views) for concurrent request handling. VFS (virtual file system) changes are applied to the host, which creates a new revision in salsa.

### Cancellation Handling

rust-analyzer uses salsa's built-in cancellation. When a new edit arrives:
1. The main thread applies the VFS change to `AnalysisHost`
2. This bumps the salsa revision, which poisons any in-progress reads
3. Background threads detect the cancellation (salsa throws `Cancelled`) and abort
4. New `Analysis` snapshots are issued for the updated state

This is cooperative — salsa checks for cancellation at query boundaries.

### Public API vs LS-Internal Boundary

The `ide` crate provides the public-ish API (`Analysis`, `AnalysisHost`). The `hir` crate provides semantic types (`Function`, `Module`, `Type`, `Struct`). The `salsa` database and low-level crates are internal. The architecture is layered: `LSP handlers` → `ide` → `hir` → `hir-def` + `hir-ty` → `salsa DB`. External consumers would use the `ide` crate API.

### LS Codebase Size Relative to Compiler

rust-analyzer is ~300K lines of Rust across ~70 crates. There is no separate "compiler" — the entire thing IS the IDE engine. The LSP transport layer (`rust-analyzer/crates/rust-analyzer`) is ~15K lines; the IDE feature layer (`ide`, `ide-assists`, `ide-completion`, `ide-diagnostics`) is ~50K lines; the core analysis (`hir`, `hir-def`, `hir-ty`) is ~100K lines; infrastructure (parser, syntax, salsa integration, VFS) is ~50K lines.

**Key repo**: `rust-lang/rust-analyzer`; key blog: matklad's "Three Architectures for a Responsive IDE" (2020-07-20)

---

## gopls (Go)

### Shared Code vs Separate Implementation

gopls is a **separate codebase** from the Go compiler (`cmd/compile`), but it reuses the Go standard library's analysis packages: `go/types` (type checker), `go/ast` (AST), `go/parser`, `go/token`, `go/packages`, and `go/analysis` (static analysis framework). These packages were designed to be consumed by tools — they're part of Go's standard library.

### Compiler APIs Called by the Language Server

```go
// Type checking
cfg := &types.Config{
    Importer: importer,
    Error:    errorHandler,
}
info := &types.Info{
    Types:      map[ast.Expr]types.TypeAndValue{},
    Defs:       map[*ast.Ident]types.Object{},
    Uses:       map[*ast.Ident]types.Object{},
    Implicits:  map[ast.Node]types.Object{},
    Selections: map[*ast.SelectorExpr]*types.Selection{},
    Scopes:     map[ast.Node]*types.Scope{},
}
pkg, err := cfg.Check(pkgPath, fset, files, info)

// Static analysis framework
pass := &analysis.Pass{
    Analyzer:  analyzer,
    Fset:      fset,
    Pkg:       pkg,
    TypesInfo: info,
    // ...
}
```

gopls also uses `golang.org/x/tools/go/packages` for loading, and `golang.org/x/tools/go/analysis` for running analyzers.

```
┌──────────────────────────────────────────────────┐
│                 gopls LSP layer                  │
│           (handles, session, server)             │
├──────────────────────────────────────────────────┤
│         Snapshot (immutable world-view)           │
│  persistent.Map  metadata.Graph  filecache       │
├──────────────────────────────────────────────────┤
│          Type Checking Batch                      │
│  typeCheckBatch()  memoize.Promise                │
├──────────────────────────────────────────────────┤
│       go/types    go/ast    go/analysis          │
│     (stdlib type checker, parser, analyzers)     │
└──────────────────────────────────────────────────┘
```

### How Type Information Drives Completions

gopls uses `types.Info` — a struct populated by `types.Config.Check()` that maps every `ast.Expr` to its `TypeAndValue`, every `*ast.Ident` to its defining or using `types.Object`, and every `*ast.SelectorExpr` to its `types.Selection`. For completions after a dot, gopls:
1. Gets the `types.Type` of the expression before the dot from `types.Info.Types`
2. Enumerates the type's method set via `types.NewMethodSet(T)`
3. Lists struct fields via `types.Struct.Field(i)`
4. Filters by exported/unexported based on package context

### Diagnostics: Push vs Pull

**Both.** gopls maintains a diagnostics "store" and pushes diagnostics to the client via `textDocument/publishDiagnostics`. Diagnostics are recomputed when the Snapshot changes. The client can also pull diagnostics. gopls runs `go/analysis` analyzers in the background after type checking completes, then pushes those results too.

### Incremental Recompilation Strategy

**Clone-based immutable snapshots with persistent data structures.**

gopls uses an immutable `Snapshot` model (from `internal/cache/snapshot.go`, ~2220 lines):
- Each `Snapshot` is a frozen view of the workspace at a point in time
- On a file change, `clone()` creates a new `Snapshot` that shares most data with the previous one via `persistent.Map` (a persistent/immutable sorted map)
- `metadata.Graph` tracks package-level dependency information
- `filecache` provides cross-process caching of type-checking results (on-disk cache keyed by content hash)
- Type checking uses `typeCheckBatch()` which memoizes results via `memoize.Promise`
- Each Snapshot's type-checking results are cached and shared; when a file changes, only affected packages are re-type-checked

The session model is: `Session` → `View` (per workspace folder) → `Snapshot` (immutable, cloned on change)

### State Persistence Between Requests

The `Session` object and its `View`s persist. Each `View` holds the current `Snapshot`. Snapshots contain the cached parse trees, type-checking results, analysis results, and metadata graph. The `filecache` persists across process restarts (on-disk).

### Cancellation Handling

Each `Snapshot` has a context that is cancelled when the snapshot becomes stale (replaced by a newer one). Long-running operations check `ctx.Done()`. When a new file change arrives, the old snapshot's context is cancelled, causing in-flight operations to abort.

### Public API vs LS-Internal Boundary

`go/types`, `go/ast`, `go/parser`, and `go/analysis` are all public Go standard library packages — anyone can use them. gopls's internal packages (`internal/cache`, `internal/lsp`, `internal/analysis`) are unexported (Go's `internal/` convention). The boundary is clear: public stdlib for language analysis, internal packages for IDE-specific orchestration.

### LS Codebase Size Relative to Compiler

gopls is ~150K lines in `golang.org/x/tools/gopls/`. The Go standard library packages it depends on (`go/types` ~40K, `go/ast` ~5K, `go/parser` ~5K, `go/analysis` ~3K) total ~55K. The Go compiler (`cmd/compile`) is ~100K+ lines but shares very little code with gopls. gopls is thus larger than the analysis libraries it uses.

**Key repo**: `golang/tools` (contains gopls); design doc at `gopls/doc/design/implementation.md`

---

## SourceKit-LSP (Swift)

### Shared Code vs Separate Implementation

SourceKit-LSP is a **thin LSP wrapper** that delegates to two underlying services:
- `sourcekitd` — the Swift compiler daemon (runs the Swift compiler's frontend in a long-lived process)
- `clangd` — for C/C++/Objective-C files in mixed-language projects

`sourcekitd` IS the Swift compiler's frontend running as a service. So for Swift files, the language server shares compiler code completely (via the daemon). For C-family files, it delegates to the entirely separate `clangd`.

### Compiler APIs Called by the Language Server

SourceKit-LSP communicates with `sourcekitd` via an XPC (or JSON) request/response protocol. Key requests:

- `source.request.codecomplete` — completions at a position
- `source.request.cursorinfo` — type/symbol info at cursor
- `source.request.editor.open` / `.replacetext` — open/edit a document
- `source.request.diagnostics` — request diagnostics
- `source.request.semantic_tokens` — semantic highlighting
- `source.request.indexer.findUSRs` — find symbol occurrences

```
┌──────────────────────────────────────────────────┐
│             SourceKit-LSP (Swift)                │
│          (LSP protocol adapter)                  │
├─────────────────────┬────────────────────────────┤
│    sourcekitd       │        clangd              │
│  (Swift compiler    │  (LLVM C-family            │
│   as a daemon)      │   language server)         │
├─────────────────────┴────────────────────────────┤
│       Swift Compiler Frontend / libclang         │
└──────────────────────────────────────────────────┘
```

### How Type Information Drives Completions

`sourcekitd` runs the Swift compiler's type checker internally when handling a `codecomplete` request. It parses and type-checks up to the cursor position, then queries the compiler's `Sema` (semantic analysis) module for available completions. The completions include type information because the full compiler frontend is running.

### Diagnostics: Push vs Pull

`sourcekitd` pushes diagnostics when a document is opened or edited — it compiles the file and sends back diagnostics as part of the response. SourceKit-LSP receives these and publishes them to the client.

### Incremental Recompilation Strategy

`sourcekitd` uses the Swift compiler's incremental compilation infrastructure. For IDE mode, it performs "syntax-only" or "partial" type checking depending on the request. Background indexing (when enabled) performs full compilation of changed files. The daemon caches compiler state between requests.

### State Persistence Between Requests

`sourcekitd` is a long-running daemon — it keeps the compiler's ASTs and semantic information in memory. SourceKit-LSP itself maintains the workspace model (build system, file tracking). No background indexing by default (build-system integration required).

### Cancellation Handling

SourceKit-LSP supports LSP request cancellation. Requests to `sourcekitd` can also be cancelled.

### Public API vs LS-Internal Boundary

`sourcekitd` exposes a C API (`sourcekitd.h`) that is the public interface. SourceKit-LSP uses this public API. The Swift compiler internals are not directly accessible — everything goes through `sourcekitd`'s request/response protocol.

### LS Codebase Size Relative to Compiler

SourceKit-LSP itself is ~30K lines of Swift. `sourcekitd` is part of the Swift compiler repo (~1M+ lines). The LS is extremely thin relative to the compiler.

**Key repos**: `apple/sourcekit-lsp`, `apple/swift` (contains sourcekitd)

---

## Kotlin Language Server (deprecated)

### Shared Code vs Separate Implementation

The community Kotlin Language Server (`fwcd/kotlin-language-server`, now deprecated in favor of JetBrains' official `kotlin-lsp`) used **Kotlin compiler internal APIs** directly. It embedded the Kotlin compiler frontend as a library.

### Compiler APIs Called by the Language Server

```kotlin
// Create a compiler environment for a set of source files
val environment = KotlinCoreEnvironment.createForProduction(
    disposable,
    compilerConfiguration,
    EnvironmentConfigFiles.JVM_CONFIG_FILES
)

// Parse a file
val ktFile: KtFile = environment.createKtFile(source, path)

// Type-check: get the binding context
val analysisResult = TopDownAnalyzerFacadeForJVM.analyzeFilesWithJavaIntegration(
    project, files, trace, configuration, packagePartProvider
)
val bindingContext: BindingContext = analysisResult.bindingContext

// Query type at a position
val expression: KtExpression = // find in AST
val type: KotlinType = bindingContext.getType(expression)
```

Key types: `KotlinCoreEnvironment`, `KtFile`, `BindingContext`, `KotlinType`, `DeclarationDescriptor`, `ResolveSession`

### How Type Information Drives Completions

The LS created a `CompiledFile` containing the `KtFile` (parsed AST) and `BindingContext` (type-checked). For completions, it:
1. Found the expression at cursor position in the `KtFile` AST
2. Queried `BindingContext` for the type of the expression
3. Used `DeclarationDescriptor` hierarchy to enumerate members
4. For expression-level incremental work, it used a "fake tiny file" technique — inserting the cursor context into a minimal synthetic file and re-type-checking just that fragment

### Diagnostics: Push vs Pull

Pull — diagnostics were computed by running the Kotlin compiler's analysis phase on demand and extracting `BindingContext.getDiagnostics()`.

### Incremental Recompilation Strategy

File-level incremental compilation. `KotlinCoreEnvironment` was created once and files were updated individually. The "fake tiny file" mechanism provided expression-level incrementality for completions — instead of re-analyzing the whole file, a minimal file containing just the relevant scope was created and analyzed.

### State Persistence Between Requests

The `KotlinCoreEnvironment` and compiled file cache persisted across requests.

### Cancellation Handling

Not documented in detail. The deprecated LS had basic threading.

### Public API vs LS-Internal Boundary

The Kotlin compiler's APIs used (`KotlinCoreEnvironment`, `BindingContext`, etc.) are compiler-internal. They are not stable public APIs. This was a major fragility point and one reason JetBrains eventually created the official `kotlin-lsp`.

### LS Codebase Size Relative to Compiler

The community Kotlin LS was ~20K lines. The Kotlin compiler is ~500K+ lines. The LS was small but tightly coupled to compiler internals.

**Key repos**: `fwcd/kotlin-language-server` (deprecated), `JetBrains/kotlin` (compiler), `JetBrains/kotlin-lsp` (new official)

---

## CEL (Common Expression Language)

### Shared Code vs Separate Implementation

CEL has **no dedicated language server**. The `cel-go` library provides a self-contained evaluation toolkit. Any IDE tooling would need to be built on top of CEL's programmatic API.

### Compiler APIs Available

```go
// Declare an environment with type information
env, err := cel.NewEnv(
    cel.Variable("name", cel.StringType),
    cel.Variable("age",  cel.IntType),
    cel.Types(&myproto.Request{}),     // register protobuf types
    cel.Function("greet",              // custom function
        cel.Overload("greet_string",
            []*cel.Type{cel.StringType},
            cel.StringType,
            cel.UnaryBinding(greetImpl),
        ),
    ),
)

// Compile (parse + type-check)
ast, issues := env.Compile(`name.startsWith("test") && age > 18`)
// issues contains parse/type errors

// Type of the expression
ast.OutputType() // cel.BoolType

// Evaluate
prg, err := env.Program(ast)
out, det, err := prg.Eval(map[string]interface{}{
    "name": "test-user",
    "age":  25,
})
```

Key types: `cel.Env`, `cel.Ast`, `cel.Program`, `cel.Type`, `cel.Variable`, `cel.Function`

```
┌───────────────────────────────────────────────────┐
│            No dedicated LS exists                 │
├───────────────────────────────────────────────────┤
│              cel.Env API                          │
│  NewEnv()  Compile()  Program()  Eval()           │
├───────────────────────────────────────────────────┤
│           CEL Core (cel-go)                       │
│  Parser → Checker (type-check) → Interpreter      │
└───────────────────────────────────────────────────┘
```

### How Type Information Could Drive Completions

`cel.Env` knows all declared variables, their types, all registered functions and their overloads, and all protobuf type descriptors. An LS could:
1. Call `env.Compile()` on the partial expression to get partial AST
2. Query `env` for variable names and types
3. For dot-access, resolve the type of the prefix expression and enumerate its fields (protobuf message fields, map keys)

CEL supports gradual type checking — expressions can mix typed and `dyn` (dynamic) values.

### Diagnostics

`env.Compile()` returns `*cel.Issues` containing parse and type-check errors with source positions.

### Incremental Recompilation

Not applicable — CEL expressions are typically single expressions, not files. Each `Compile()` call is fast.

### State Persistence

The `cel.Env` is reusable and cheap to create.

### LS Codebase Size

N/A — no LS exists. `cel-go` is ~15K lines. The cel-spec (language definition) is ~5K lines.

**Key repos**: `google/cel-go`, `google/cel-spec`

---

## Rego / OPA (Open Policy Agent)

### Shared Code vs Separate Implementation

**Separate.** Regal (`styrainc/regal`) is the linter and language server for Rego. It's a separate project from OPA (`open-policy-agent/opa`), though it imports OPA's parser and compiler as Go libraries. The VS Code extension (`tsandall/vscode-opa`) delegates LSP features to Regal.

### Compiler APIs Called by the Language Server

Regal imports OPA's Go packages:
- `github.com/open-policy-agent/opa/ast` — parser, type checker, compiler
- `github.com/open-policy-agent/opa/rego` — evaluation engine

```go
// Parse
module, err := ast.ParseModule("policy.rego", source)

// Compile (type-check + resolve)
compiler := ast.NewCompiler()
compiler.Compile(map[string]*ast.Module{"policy": module})
compiler.Errors // type errors

// Type of a rule
typeEnv := compiler.TypeEnv
typ := typeEnv.Get(ref)
```

### LS Features Provided

Regal provides: diagnostics (parse errors + lint rules), code completions (built-in functions, rule names, package references), hover (documentation for built-in functions), go-to-definition, code actions (quick fixes for lint violations), formatting.

### Diagnostics

Push model — Regal watches for file changes and pushes diagnostics. It runs OPA's parser for syntax errors and its own lint rules for style/correctness issues.

### Incremental Recompilation

OPA's `ast.Compiler` recompiles the full module set. For a single-file edit, Regal re-parses that file and recompiles. OPA modules are relatively small, so this is fast.

### LS Codebase Size

Regal is ~25K lines of Go. OPA is ~200K lines. Regal is lightweight relative to OPA.

**Key repos**: `styrainc/regal`, `open-policy-agent/opa`, `tsandall/vscode-opa`

---

## CUE

### Shared Code vs Separate Implementation

CUE has a built-in LSP command: `cue lsp`. It's part of the CUE CLI binary itself. Internally, the CUE LSP implementation **forks the gopls architecture** — it imports and reuses infrastructure from `internal/golangorgx/gopls/` (a vendored/forked copy of gopls internals). The language-specific parts (CUE parsing, type checking, evaluation) use CUE's own packages.

### Compiler APIs Called by the Language Server

CUE's evaluation model is based on lattice-based unification rather than traditional type checking:

```go
// Load CUE files
instances := load.Instances(args, config)

// Build a CUE value (parsed + evaluated)
ctx := cuecontext.New()
value := ctx.BuildInstance(instance)

// Query type/constraints
value.LookupPath(cue.ParsePath("field"))
value.Kind()         // StringKind, IntKind, StructKind, etc.
value.Validate()     // check constraints
```

### Architecture

```
┌──────────────────────────────────────────────────┐
│           cue lsp (built into CLI)               │
├──────────────────────────────────────────────────┤
│    Forked gopls infrastructure                    │
│  (session, view, snapshot model)                 │
├──────────────────────────────────────────────────┤
│          CUE Evaluation Engine                    │
│  Load → Parse → Evaluate (lattice unification)   │
└──────────────────────────────────────────────────┘
```

### Diagnostics

CUE's evaluation produces errors when values don't unify (conflicting constraints). The LSP pushes these as diagnostics.

### Incremental Recompilation

Inherits gopls's snapshot/clone model for workspace management. CUE evaluation itself is re-run on changes.

### LS Codebase Size

CUE is ~200K lines total. The LSP-specific code reuses the forked gopls infrastructure (~50K lines of vendored code). CUE-specific LSP glue is relatively small.

**Key repo**: `cue-lang/cue`; LSP code at `internal/golangorgx/gopls/`

---

## Pkl (Apple)

### Shared Code vs Separate Implementation

**Separate LSP, partially sharing code.** The Pkl LSP (`apple/pkl-lsp`) is a separate repository from the Pkl language (`apple/pkl`). The LSP is written in Kotlin (98.9% Kotlin). Notably, the LSP switched from the Pkl compiler's parser to **tree-sitter** for parsing (PR #17: "Switch parser to tree-sitter"). The main Pkl compiler uses a hand-rolled parser (replaced ANTLR in PR #917).

### Compiler APIs

The Pkl compiler (`pkl-core`, written in Java 65.5% / Kotlin 28.7%) provides:
- `pkl-parser` — the hand-rolled parser module
- `pkl-core` — evaluation engine, type checker
- `pkl-formatter` — code formatting

The LSP likely uses some shared types but has its own parsing infrastructure (tree-sitter) and its own analysis pipeline.

### LS Features

From the README:
- ✓ Diagnostics (WIP)
- ✓ Hover
- ✓ Go to definition
- ✓ Auto complete (WIP: definition level access still needed)
- ✓ Project syncing
- ✓ Package downloading
- ✓ Type checking
- ❏ Rename, Find references, Code lens, Formatting, Quick fixes (not yet)

### Architecture

```
┌──────────────────────────────────────────────────┐
│              pkl-lsp (Kotlin)                    │
│         (separate repo, 98.9% Kotlin)            │
├──────────────────────────────────────────────────┤
│       tree-sitter parser (for LSP)               │
│  (NOT the pkl-core hand-rolled parser)           │
├──────────────────────────────────────────────────┤
│    Type checking / analysis (LSP-internal)        │
├──────────────────────────────────────────────────┤
│   pkl-core types (shared definitions?)           │
└──────────────────────────────────────────────────┘

Separate:
┌──────────────────────────────────────────────────┐
│           pkl-core (Java/Kotlin)                 │
│  Hand-rolled parser → Evaluator → Type checker   │
└──────────────────────────────────────────────────┘
```

### LS Codebase Size

The LSP repo (`apple/pkl-lsp`) has 9 contributors, 76 stars. The main compiler (`apple/pkl`) is 65.5% Java / 28.7% Kotlin with 71 contributors and 11.3K stars. The LSP is significantly smaller than the compiler.

Also: `apple/pkl-intellij` (JetBrains plugin), `apple/pkl-vscode` (VS Code extension — UI only, delegates to pkl-lsp), `apple/tree-sitter-pkl` (tree-sitter grammar).

**Key repos**: `apple/pkl-lsp`, `apple/pkl`, `apple/pkl-vscode`, `apple/tree-sitter-pkl`

---

## Jsonnet

### Shared Code vs Separate Implementation

**Separate LS.** The Jsonnet language server (`grafana/jsonnet-language-server`) is a Go-based LSP server. The Jsonnet language itself has multiple implementations: `google/jsonnet` (C++), `google/go-jsonnet` (Go). The language server imports `google/go-jsonnet` as a library.

### Compiler APIs Called

```go
import "github.com/google/go-jsonnet"

vm := jsonnet.MakeVM()
// Evaluate (also catches errors)
output, err := vm.EvaluateFile(filename)

// For AST access
import "github.com/google/go-jsonnet/ast"
node, err := jsonnet.SnippetToAST(filename, source)
```

### LS Features

- Jump to definition (go-to-def for variables, imports)
- Error/warning diagnostics (parse errors, evaluation errors)
- Linting diagnostics
- Standard library hover and autocomplete
- Formatting (via jsonnetfmt)

### Diagnostics

Diagnostics come from running `go-jsonnet`'s parser/evaluator and collecting errors with source positions.

### Incremental Recompilation

Jsonnet files are re-parsed and re-evaluated on each change. Jsonnet evaluation is fast for typical file sizes.

### LS Codebase Size

The language server is ~5K lines of Go. `go-jsonnet` is ~15K lines.

**Key repos**: `grafana/jsonnet-language-server`, `google/go-jsonnet`

---

## Dhall

### Shared Code vs Separate Implementation

**Shared codebase.** `dhall-lsp-server` lives in the `dhall-lang/dhall-haskell` monorepo alongside the Dhall compiler/interpreter. It directly imports and calls the Dhall library functions for parsing, type checking, normalization, and import resolution.

### Compiler APIs Called

```haskell
-- Parse
import Dhall.Parser (exprFromText)
ast <- exprFromText "(input)" source

-- Import resolution
import Dhall.Import (load)
resolved <- load ast

-- Type check
import Dhall.TypeCheck (typeOf)
typ <- typeOf resolved

-- Normalize
import Dhall.Core (normalize)
let normal = normalize resolved
```

The LSP source code lives at `src/Dhall/LSP/` within the dhall-haskell monorepo.

### LS Features

- Diagnostics on save (full parse + import resolution + type check pipeline)
- Clickable imports (go-to-definition for import paths)
- Type-on-hover (shows the type of the expression under cursor)
- Code completion:
  - Environment variable names
  - Local import paths (file system completion)
  - Identifiers in scope (let bindings, record fields)
  - Record projections
  - Union constructors
- Formatting (via `dhall format`)
- Linting (via `dhall lint`)
- Annotate let bindings with inferred types
- Freeze imports (pin import hashes)

### Diagnostics

Push model — on each save, the LSP runs the full Dhall pipeline (parse → resolve imports → type check) and pushes diagnostics.

### Incremental Recompilation

No incremental infrastructure documented. Each save triggers a full re-analysis. Dhall files are typically small enough that this is fast.

### State Persistence

The LSP maintains an import cache across requests (Dhall's import resolution is content-addressed and cacheable).

### LS Codebase Size

`dhall-lsp-server` is ~3K lines of Haskell. The Dhall Haskell library is ~30K lines. The LSP is ~10% of the core library. Dependencies: `lsp-2.8`, `lsp-types-2.4` (Haskell LSP framework libraries).

**Key repo**: `dhall-lang/dhall-haskell` (contains `dhall-lsp-server/`)

---

## Starlark

### Shared Code vs Separate Implementation

**No widely-adopted dedicated language server.** Starlark (the configuration language used by Bazel, Buck, etc.) has multiple runtime implementations:
- `bazelbuild/starlark` (spec + Java reference)
- `google/starlark-go` (Go implementation)
- `aspect-build/aspect-cli` uses `starlark-rust` (Rust implementation, formerly `facebookexperimental/starlark-rust`)

Tooling for Starlark is primarily **Buildifier** (`bazelbuild/buildtools`), which provides formatting and linting. For IDE features, the Bazel team relies on Buildifier's analysis + basic editor plugins. There is no production-quality standalone Starlark LSP.

### Existing Tooling

Buildifier provides:
- Formatting (`buildifier`)
- Linting with diagnostics (`buildifier --lint=warn`)
- Code analysis (undefined variables, unused loads, etc.)

The VS Code Bazel extension (`bazelbuild/vscode-bazel`) provides basic Starlark support (syntax highlighting, BUILD file navigation) but does not include a full LSP.

### Why No LS Exists

Starlark is a subset of Python with deliberate limitations (no classes, no exceptions, deterministic evaluation). Most Starlark files are BUILD files or `.bzl` macros that interact heavily with Bazel's rule API. Full IDE support would require understanding Bazel's rule definitions, which are themselves Starlark — creating a bootstrapping problem. The effective "type system" is the set of Bazel rule attributes, which varies per workspace.

### LS Codebase Size

N/A — Buildifier is ~20K lines of Go. `starlark-go` is ~15K lines.

**Key repos**: `bazelbuild/buildtools` (Buildifier), `google/starlark-go`, `bazelbuild/vscode-bazel`
