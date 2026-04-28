# Compilation Result Types & Snapshot Patterns — External Survey

Raw research collection. No interpretation, no conclusions, no recommendations.

---

## Roslyn (C#)

### Compilation result type

The central type is `Microsoft.CodeAnalysis.Compilation` — an abstract class subclassed by `CSharpCompilation` and `VisualBasicCompilation`.

```csharp
// src/Compilers/Core/Portable/Compilation/Compilation.cs
public abstract partial class Compilation
{
    public string? AssemblyName { get; }
    public CompilationOptions Options { get { return CommonOptions; } }
    public IEnumerable<SyntaxTree> SyntaxTrees { get; }
    public ImmutableArray<MetadataReference> ExternalReferences { get; }
    public IAssemblySymbol Assembly { get; }
    public IModuleSymbol SourceModule { get; }
    public INamespaceSymbol GlobalNamespace { get; }
    
    // SemanticModel is per-tree, obtained from the Compilation
    public SemanticModel GetSemanticModel(SyntaxTree syntaxTree, bool ignoreAccessibility = false);
    
    // TypeChecker access
    // (implicit — TypeChecker is internal, exposed via SemanticModel)
    
    // Emit produces the final binary output
    public EmitResult Emit(Stream peStream, ...);
    
    // Diagnostics at each stage
    public abstract ImmutableArray<Diagnostic> GetParseDiagnostics(...);
    public abstract ImmutableArray<Diagnostic> GetDeclarationDiagnostics(...);
    public abstract ImmutableArray<Diagnostic> GetMethodBodyDiagnostics(...);
    public abstract ImmutableArray<Diagnostic> GetDiagnostics(...);
}
```

Source: [dotnet/roslyn — Compilation.cs](https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Compilation/Compilation.cs)

### Immutability

`Compilation` is immutable. The class doc states:

> "The compilation object is an immutable representation of a single invocation of the compiler. Although immutable, a compilation is also on-demand, and will realize and cache data as necessary. A compilation can produce a new compilation from existing compilation with the application of small deltas."

All mutation methods (`AddSyntaxTrees`, `RemoveSyntaxTrees`, `WithOptions`, `WithReferences`, `ReplaceSyntaxTree`) return a **new** `Compilation` instance. Internal caches are lazily populated but externally the object appears frozen.

### Diagnostics: inline or separate?

Separate. Diagnostics are **not stored on the Compilation** — they are computed on demand via dedicated methods:

- `GetParseDiagnostics()` — syntax errors
- `GetDeclarationDiagnostics()` — declaration-level issues
- `GetMethodBodyDiagnostics()` — body analysis
- `GetDiagnostics()` — all phases combined

```csharp
public interface Diagnostic {
    DiagnosticCategory category;  // Warning, Error, Suggestion, Message
    int code;
    SourceFile? file;
    int? start;
    int? length;
    string | DiagnosticMessageChain messageText;
}
```

`EmitResult` also carries its own diagnostics:

```csharp
public struct EmitResult {
    public bool emitSkipped;
    public readonly ImmutableArray<Diagnostic> diagnostics;
    public string[]? emittedFiles;
}
```

### Consumer access: CLI vs IDE vs API

| Consumer | Access pattern |
|----------|---------------|
| **CLI** (`csc.exe`) | Creates `Compilation`, calls `GetDiagnostics()`, then `Emit()`. One-shot. |
| **IDE** (VS, OmniSharp) | Creates `Compilation`, then calls `GetSemanticModel(tree)` per file. SemanticModel provides symbol lookup, type info, completions. The `Workspace` layer manages incremental `Compilation` snapshots via `Solution` → `Project` → `Document` chain. |
| **Analyzer API** | Receives `Compilation` via `CompilationStartAnalysisContext`. Can call `GetSemanticModel()`, inspect symbols, register callbacks. |
| **Source generators** | Receive `Compilation` in `GeneratorExecutionContext.Compilation`. Can inspect types, add new syntax trees. |

### SemanticModel

`SemanticModel` is a **per-syntax-tree** view of the compilation. It exposes:

```csharp
public abstract class SemanticModel {
    public abstract SyntaxTree SyntaxTree { get; }
    public abstract Compilation Compilation { get; }
    
    // Symbol resolution
    public ISymbol? GetDeclaredSymbol(SyntaxNode node);
    public SymbolInfo GetSymbolInfo(ExpressionSyntax expression);
    public TypeInfo GetTypeInfo(ExpressionSyntax expression);
    
    // Flow analysis
    public DataFlowAnalysis AnalyzeDataFlow(StatementSyntax statement);
    public ControlFlowAnalysis AnalyzeControlFlow(StatementSyntax first, StatementSyntax last);
    
    // Diagnostics scoped to this tree
    public abstract ImmutableArray<Diagnostic> GetDiagnostics(...);
}
```

Source: [dotnet/roslyn — SemanticModel.cs](https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Compilation/SemanticModel.cs)

### Metadata traveling with the result

The `Compilation` carries:

- **Symbol tables**: `Assembly`, `SourceModule`, `GlobalNamespace` — the full symbol graph
- **Type information**: via `SemanticModel.GetTypeInfo()`
- **Source maps**: tracked internally for emit; `SourceMapEmitResult` returned from emit
- **References**: `ExternalReferences`, `DirectiveReferences`, plus resolved assembly identity mapping
- **Syntax trees**: the full set of parsed trees

### Partial success model

Yes. A `Compilation` with errors is fully valid — you can still:
- Query `SemanticModel` (best-effort type information)
- Call `Emit()` — returns `EmitResult` with `emitSkipped = true` if errors prevent codegen, but `diagnostics` always populated
- Inspect symbol tables (error type symbols exist as `IErrorTypeSymbol`)

`EmitResult` has three exit states:
```csharp
enum ExitStatus {
    Success = 0,
    DiagnosticsPresent_OutputsSkipped = 1,
    DiagnosticsPresent_OutputsGenerated = 2,
    InvalidProject_OutputsSkipped = 3,
    ProjectReferenceCycle_OutputsSkipped = 4,
}
```

### Caching and reuse

- `Compilation` supports **incremental reuse** via `AddSyntaxTrees`, `ReplaceSyntaxTree`, etc. — the new compilation reuses internal data from the old one.
- Internal lazy caches: `_getTypeCache` (ConcurrentCache), `_getTypesCache`, bound reference manager, etc.
- `SemanticModel` instances are cached by `SemanticModelProvider`.
- `Workspace` / `Solution` objects manage snapshots with version tracking.

### Public API vs internal state

| Public | Internal |
|--------|----------|
| `Compilation`, `SemanticModel`, `SyntaxTree`, `ISymbol` hierarchy, `Diagnostic`, `EmitResult` | `TypeChecker` (internal), `NodeLinks`, `EmitResolver`, `CommonPEModuleBuilder`, `SymbolMatcher` for EnC, `DeltaMetadataWriter`, `EmitStream` |

The TypeChecker is not directly exposed. Consumers interact with it through `SemanticModel`.

---

## TypeScript

### Compilation result type

TypeScript has two primary result types depending on the consumer:

**1. `ts.Program`** — the CLI/batch compilation result:

```typescript
// src/compiler/types.ts
export interface Program extends ScriptReferenceHost {
    getRootFileNames(): readonly string[];
    getSourceFiles(): readonly SourceFile[];
    getTypeChecker(): TypeChecker;
    
    // Diagnostics at every level
    getOptionsDiagnostics(cancellationToken?: CancellationToken): readonly Diagnostic[];
    getGlobalDiagnostics(cancellationToken?: CancellationToken): readonly Diagnostic[];
    getSyntacticDiagnostics(sourceFile?: SourceFile, ...): readonly DiagnosticWithLocation[];
    getSemanticDiagnostics(sourceFile?: SourceFile, ...): readonly Diagnostic[];
    getDeclarationDiagnostics(sourceFile?: SourceFile, ...): readonly DiagnosticWithLocation[];
    
    // Emit
    emit(targetSourceFile?: SourceFile, writeFile?: WriteFileCallback, ...): EmitResult;
    
    // Resolution
    getResolvedModule(f: SourceFile, moduleName: string, mode: ResolutionMode):
        ResolvedModuleWithFailedLookupLocations | undefined;
}
```

**2. `ts.LanguageService`** — the IDE/editor result facade:

```typescript
export interface LanguageService {
    getCompletionsAtPosition(fileName: string, position: number, ...): CompletionInfo | undefined;
    getCompletionEntryDetails(...): CompletionEntryDetails | undefined;
    getQuickInfoAtPosition(fileName: string, position: number): QuickInfo | undefined;
    getDefinitionAtPosition(fileName: string, position: number): readonly DefinitionInfo[] | undefined;
    getReferencesAtPosition(fileName: string, position: number): ReferenceEntry[] | undefined;
    getSemanticDiagnostics(fileName: string): Diagnostic[];
    getSyntacticDiagnostics(fileName: string): DiagnosticWithLocation[];
    // ... many more IDE-oriented methods
}
```

Source: [microsoft/TypeScript — types.ts](https://github.com/microsoft/TypeScript/blob/main/src/compiler/types.ts)

### Immutability

`Program` is **effectively immutable once created**. It is created via `ts.createProgram(options)` with an optional `oldProgram` parameter that enables structural reuse (`StructureIsReused` enum: `Not`, `SafeModules`, `Completely`).

The `TypeChecker` associated with a `Program` is mutable internally (it populates caches lazily) but presents an immutable interface. The doc warns:

> "Depending on the operation performed, it may be appropriate to throw away the checker if the cancellation token is triggered."

### Diagnostics: inline or separate?

Separate. `Program` exposes multiple diagnostic retrieval methods, each for a different compilation phase:

```typescript
export interface EmitResult {
    emitSkipped: boolean;
    diagnostics: readonly Diagnostic[];  // declaration emit diagnostics
    emittedFiles?: string[];
    /** @internal */ sourceMaps?: SourceMapEmitResult[];
}

export interface Diagnostic {
    category: DiagnosticCategory;  // Warning, Error, Suggestion, Message
    code: number;
    file: SourceFile | undefined;
    start: number | undefined;
    length: number | undefined;
    messageText: string | DiagnosticMessageChain;
}

export interface DiagnosticWithLocation extends Diagnostic {
    file: SourceFile;
    start: number;
    length: number;
}
```

Diagnostics are computed on-demand and may be cached internally.

### Consumer access: CLI vs IDE vs API

| Consumer | Access pattern |
|----------|---------------|
| **CLI** (`tsc`) | Creates `Program`, calls `getPreEmitDiagnostics()`, then `emit()`. |
| **Editor** (VS Code via tsserver) | `LanguageService` wraps a `LanguageServiceHost` which provides file contents. Internally maintains a `Program` that is rebuilt incrementally. `LanguageService` methods take `(fileName, position)` tuples. |
| **API** (programmatic) | Direct `createProgram()` + `getTypeChecker()` for symbol/type queries. Or `createLanguageService()` for richer IDE features. |
| **Build mode** (`tsc -b`) | `SolutionBuilder` manages multiple `Program` instances for project references. |

### TypeChecker

The `TypeChecker` is the central analysis engine, obtained from `Program.getTypeChecker()`:

```typescript
export interface TypeChecker {
    getTypeOfSymbolAtLocation(symbol: Symbol, node: Node): Type;
    getTypeAtLocation(node: Node): Type;
    getSymbolAtLocation(node: Node): Symbol | undefined;
    typeToString(type: Type, ...): string;
    signatureToString(signature: Signature, ...): string;
    
    // Type construction
    getStringType(): Type;
    getNumberType(): Type;
    getBooleanType(): Type;
    getUnionType(types: Type[], ...): Type;
    
    // Relationship
    isTypeAssignableTo(source: Type, target: Type): boolean;
    
    // Internal diagnostics
    getDiagnostics(sourceFile?: SourceFile, ...): Diagnostic[];
    getGlobalDiagnostics(): Diagnostic[];
}
```

### Metadata traveling with the result

- **Type graph**: Full `Type` hierarchy (interfaces, unions, intersections, literals, etc.) accessible via `TypeChecker`
- **Symbol tables**: `Symbol` objects with `SymbolFlags`, declarations, exports
- **Source files**: Each `SourceFile` carries `imports`, `referencedFiles`, `typeReferenceDirectives`, `lineMap`, `commentDirectives`
- **Module resolution**: `resolvedModules` map per file, `resolvedTypeReferenceDirectiveNames`
- **Emit transformers**: `CustomTransformers` pipeline for code generation

### Partial success model

Yes. TypeScript explicitly supports partial success:

- A `Program` with errors can still emit (warnings about errors are added to `EmitResult.diagnostics`)
- `TypeChecker` returns error types (`ErrorType`) for unresolvable references
- `EmitResult.emitSkipped` indicates whether output was suppressed
- The `noEmitOnError` compiler option controls whether errors block emit

### Caching and reuse

- **Incremental builds**: `oldProgram` parameter to `createProgram()`. `StructureIsReused` tracks reuse level.
- **TypeChecker caching**: Internal maps for `NodeLinks`, resolved types, `inferenceContext`, etc.
- **Module resolution cache**: `ModuleResolutionCache` shared across incremental builds
- **Build info**: `BuildInfo` type serialized for `--incremental` / `--tsBuildInfoFile`
- **Project references**: Resolved once, cached across solution builds

### Public API vs internal state

Many TypeScript types have `/** @internal */` members that are not part of the public API:

| Public | Internal |
|--------|----------|
| `Program`, `TypeChecker`, `SourceFile`, `Symbol`, `Type`, `Diagnostic`, `EmitResult`, `LanguageService` | `NodeLinks`, `EmitResolver`, `TransformFlags`, `FlowNode`, `InferenceContext`, `TypeMapper`, `EvaluatorResult`, `SerializedTypeEntry` |

---

## Rust (rustc)

### Compilation result type

Rust's compiler uses `TyCtxt` (type context) as the central compilation database. There is no single "Compilation" object — instead, `TyCtxt` is a query-driven database.

```rust
// compiler/rustc_middle/src/ty/context.rs
pub struct TyCtxt<'tcx> {
    gcx: &'tcx GlobalCtxt<'tcx>,
}

pub struct GlobalCtxt<'tcx> {
    pub arena: &'tcx WorkerLocal<Arena<'tcx>>,
    pub hir_arena: &'tcx WorkerLocal<hir::Arena<'tcx>>,
    interners: CtxtInterners<'tcx>,
    pub sess: &'tcx Session,
    
    // Query system
    pub(crate) cstore: Box<CrateStoreDyn>,
    pub(crate) untracked: Untracked,
    
    // Dep graph for incremental compilation
    pub(crate) dep_graph: DepGraph,
    
    pub queries: &'tcx dyn query::QueryEngine<'tcx>,
    pub query_caches: query::QueryCaches<'tcx>,
    pub(crate) query_kinds: &'tcx [DepKindStruct<'tcx>],
}
```

Source: [rust-lang/rust — compiler/rustc_middle/src/ty/context.rs](https://github.com/rust-lang/rust/blob/master/compiler/rustc_middle/src/ty/context.rs)

### Query system

Results are obtained through **queries** rather than method calls. Each query is memoized:

```rust
// Example queries (compiler/rustc_middle/src/query/mod.rs)
query type_of(def_id: DefId) -> Ty<'tcx>;
query predicates_of(def_id: DefId) -> GenericPredicates<'tcx>;
query fn_sig(def_id: DefId) -> PolyFnSig<'tcx>;
query adt_def(def_id: DefId) -> AdtDef<'tcx>;
query check_well_formed(def_id: LocalDefId) -> Result<(), ErrorGuaranteed>;
query optimized_mir(def_id: DefId) -> &'tcx Body<'tcx>;
query codegen_unit(def_id: DefId) -> &'tcx CodegenUnit<'tcx>;
```

### Immutability

`TyCtxt` is **structurally immutable** — all types, regions, and constants are **interned** in arena allocators. Once created, a `Ty<'tcx>` is a thin pointer (`*const TyS<'tcx>`) into the arena. Types are compared by pointer equality.

```rust
// compiler/rustc_middle/src/ty/mod.rs
pub type Ty<'tcx> = &'tcx TyS<'tcx>;

pub struct TyS<'tcx> {
    kind: TyKind<'tcx>,
    flags: TypeFlags,
    outer_exclusive_binder: DebruijnIndex,
}
```

Query results are cached in `query_caches` and never recomputed within a session.

### Diagnostics: inline or separate?

Separate — diagnostics flow through the `Session`:

```rust
// compiler/rustc_session/src/session.rs
pub struct Session {
    pub dcx: DiagCtxt,
    // ...
}

pub struct DiagCtxt {
    inner: Lock<DiagCtxtInner>,
}

struct DiagCtxtInner {
    flags: DiagCtxtFlags,
    err_guars: Vec<ErrorGuaranteed>,
    lint_err_guars: Vec<ErrorGuaranteed>,
    delayed_bugs: Vec<(DelayedBug, DelayedBugBacktrace)>,
    deduplicated_err_count: usize,
    deduplicated_warn_count: usize,
    emitter: Box<DynEmitter>,
    // ...
}
```

`ErrorGuaranteed` is a zero-size proof token that an error was emitted. Query results use `Result<T, ErrorGuaranteed>` to propagate errors.

### Consumer access

| Consumer | Access pattern |
|----------|---------------|
| **CLI** (`rustc`) | `rustc_driver::RunCompiler` orchestrates the full pipeline through `TyCtxt` queries. |
| **IDE** (rust-analyzer) | Does **not** use `rustc`'s `TyCtxt`. Has its own analysis engine with `salsa`-based incremental computation. Separate `hir::Semantics`, `TyLoweringContext`, `InferenceResult`. |
| **Clippy/lints** | Receives `TyCtxt` via lint pass callbacks (`LateLintPass`, `EarlyLintPass`). |
| **Miri** (interpreter) | Uses `TyCtxt` + `InterpCx` to interpret MIR directly. |

### Metadata traveling with the result

- **Interned types**: All types, regions, consts live in `TyCtxt` arenas
- **DefId mapping**: Every item has a `DefId` that maps to its definition
- **MIR**: `Body<'tcx>` — mid-level IR for each function
- **HIR**: `hir::Crate` — desugared AST
- **Crate metadata**: `.rmeta` files contain serialized query results for cross-crate access

### Partial success model

Yes, via `ErrorGuaranteed`. When type checking fails, `TyCtxt` creates a special error type (`TyKind::Error(ErrorGuaranteed)`) that propagates without generating cascading errors. Compilation continues as far as possible.

```rust
pub enum TyKind<'tcx> {
    // ...
    Error(ErrorGuaranteed),  // error type that suppresses further errors
}
```

### Caching and reuse

- **Query system**: All query results are memoized. `DepGraph` tracks dependencies for incremental recompilation.
- **Incremental compilation**: Changed queries are re-executed; unchanged results are loaded from on-disk cache (`.fingerprint` files + `dep-graph` serialization).
- **Interning**: `TyCtxt::mk_ty()` deduplicates types in the arena.

### Public API vs internal state

rustc has no stable public API for compiler internals. All types (`TyCtxt`, `Ty`, `DefId`, etc.) are internal. The stable interface is the command-line invocation. `rustc_private` is an unstable feature gate for linking against compiler internals.

---

## Kotlin K2

### Compilation result type

K2 (the new Kotlin compiler frontend) uses a **FIR (Frontend Intermediate Representation)** tree as its compilation result. The central types are:

```kotlin
// compiler/fir/tree/src/org/jetbrains/kotlin/fir/FirSession.kt
abstract class FirSession(
    val sessionProvider: FirSessionProvider?,
    val kind: Kind,
) : FirSessionComponent {
    val registeredComponents: Map<KClass<out FirSessionComponent>, FirSessionComponent>
    
    enum class Kind {
        Source,
        Library
    }
}

// compiler/fir/resolve/src/org/jetbrains/kotlin/fir/resolve/FirProvider.kt
abstract class FirProvider : FirSessionComponent {
    abstract fun getFirFilesByPackage(fqName: FqName): List<FirFile>
    abstract fun getFirClassifierByFqName(classId: ClassId): FirClassLikeDeclaration?
}
```

Source: [JetBrains/kotlin — compiler/fir/](https://github.com/JetBrains/kotlin/tree/master/compiler/fir)

### Resolution phases

FIR elements go through resolution phases tracked by `FirResolvePhase`:

```kotlin
// compiler/fir/tree/src/org/jetbrains/kotlin/fir/declarations/FirResolvePhase.kt
enum class FirResolvePhase {
    RAW_FIR,
    IMPORTS,
    SUPER_TYPES,
    SEALED_CLASS_INHERITORS,
    TYPES,
    STATUS,
    EXPECT_ACTUAL_MATCHING,
    CONTRACTS,
    IMPLICIT_TYPES_BODY_RESOLVE,
    BODY_RESOLVE,
    ANNOTATION_ARGUMENTS,
    COMPILER_REQUIRED_ANNOTATIONS;
}
```

Each declaration carries its resolution state:

```kotlin
interface FirDeclaration {
    val resolvePhase: FirResolvePhase
    val resolveState: FirResolveState
}
```

### Immutability

FIR trees are **mutable during resolution** (phases mutate declarations in-place to fill in resolved types, supertypes, etc.) but become effectively frozen once the final phase completes. The `FirResolvePhase` serves as a monotonic progress marker.

### Diagnostics: inline or separate?

Separate. Diagnostics are collected by `DiagnosticReporter`:

```kotlin
// compiler/fir/checkers/src/org/jetbrains/kotlin/fir/analysis/diagnostics/FirDiagnostic.kt
class FirDiagnostic<out E : PsiElement>(
    val element: E,
    val severity: Severity,
    val factory: AbstractFirDiagnosticFactory,
    val positioningStrategy: SourceElementPositioningStrategy,
)
```

Checkers run after resolution and report to a collector, not onto the FIR tree itself.

### Consumer access

| Consumer | Access pattern |
|----------|---------------|
| **CLI** (`kotlinc`) | Full pipeline: `RawFirBuilder` → phase-by-phase resolution → `Fir2IrConverter` → backend codegen. |
| **IDE** (IntelliJ) | Analysis API (`KaSession`) provides a stable abstraction over FIR. `analyze(element) { ... }` block obtains a session. |
| **Compiler plugins** | FIR extensions (`FirDeclarationGenerationExtension`, `FirAdditionalCheckersExtension`) operate on the FIR tree at defined points. |

### Metadata traveling with the result

- **FIR tree**: Full declaration graph with resolved types (`ConeKotlinType`), resolved supertypes, resolved function bodies
- **Symbol tables**: `FirSymbolProvider` gives access to `FirClassSymbol`, `FirFunctionSymbol`, etc.
- **Type aliases**: Expanded and tracked
- **Contracts**: Resolved as part of `CONTRACTS` phase

### Partial success model

Yes. FIR resolution continues past errors — unresolvable references get `ConeErrorType` or `FirErrorTypeRef`. The tree is always structurally complete; checkers run even in the presence of errors.

### Caching and reuse

- **Session-level caching**: `FirSession` components cache resolved declarations
- **IDE incremental analysis**: Module-level invalidation via `KaSession` lifecycle
- **Lazy resolution**: Declarations are resolved on-demand up to the needed phase

---

## Swift

### Compilation result type

Swift's compiler produces results via `SourceFile` and the type checker:

```swift
// lib/AST/Module.cpp / include/swift/AST/SourceFile.h
class SourceFile : public FileUnit {
    // All top-level declarations
    ArrayRef<Decl *> getTopLevelDecls() const;
    
    // Diagnostics
    // (routed through DiagnosticEngine, not stored on SourceFile)
    
    // Type-checked status
    enum ASTStage_t { Parsing, NameBinding, TypeChecked };
    ASTStage_t ASTStage = Parsing;
};

// include/swift/AST/ASTContext.h
class ASTContext {
    DiagnosticEngine &Diags;
    SearchPathOptions &SearchPathOpts;
    SourceManager &SourceMgr;
    
    // Interned types
    CanType TheEmptyTupleType;
    CanType TheNativeObjectType;
    // ...
    
    // Module loader
    ModuleDecl *getStdlibModule(bool loadIfAbsent = false);
};
```

Source: [apple/swift — include/swift/AST/](https://github.com/apple/swift/tree/main/include/swift/AST)

### Immutability

AST nodes are **mutable during compilation** — type checking annotates declarations in-place. After type checking, the AST is effectively frozen. `ASTContext` owns all interned types and is long-lived.

### Diagnostics: inline or separate?

Separate. All diagnostics flow through `DiagnosticEngine`:

```swift
// include/swift/AST/DiagnosticEngine.h
class DiagnosticEngine {
    SmallVector<DiagnosticConsumer *, 2> Consumers;
    
    InFlightDiagnostic diagnose(SourceLoc Loc, Diagnostic &&D);
    InFlightDiagnostic diagnose(const Decl *D, ...);
    
    bool hadAnyError() const;
};
```

### Consumer access

| Consumer | Access pattern |
|----------|---------------|
| **CLI** (`swiftc`) | `CompilerInstance` runs the pipeline. Frontend produces typed AST → SIL → LLVM IR. |
| **SourceKit-LSP** | Uses `sourcekitd` which talks to the compiler via XPC. Requests are file-path + offset based. Internally creates `ASTContext` for analysis. |
| **Swift Package Manager** | Invokes `swiftc` as a subprocess or uses `libSwiftDriver`. |

### Metadata traveling with the result

- **Typed AST**: Every `Expr` and `Decl` carries its resolved `Type`
- **SIL (Swift Intermediate Language)**: Lowered representation with ownership semantics
- **Module interface**: `.swiftinterface` files are human-readable module descriptions
- **`.swiftmodule`**: Binary serialized module for consumption by importers

### Partial success model

Yes. Swift's type checker assigns `ErrorType` to unresolvable expressions and continues. SIL generation may abort on certain errors, but diagnostics are always collected.

### Caching and reuse

- **Module caching**: Built modules cached in `ModuleCache` directory
- **Incremental compilation**: `.swiftdeps` files track cross-file dependencies; only changed files are recompiled
- **Frontend caching**: `ASTContext` caches interned types, conformances, module lookups

---

## Go

### Compilation result type

Go's type checker produces `types.Info` as the compilation result:

```go
// go/types/api.go
type Info struct {
    // Types maps expressions to their types.
    Types map[ast.Expr]TypeAndValue
    
    // Instances maps identifiers denoting generic instances to their type arguments.
    Instances map[*ast.Ident]Instance
    
    // Defs maps identifiers to the objects they define.
    Defs map[*ast.Ident]Object
    
    // Uses maps identifiers to the objects they denote.
    Uses map[*ast.Ident]Object
    
    // Implicits maps nodes to their implicitly declared objects.
    Implicits map[ast.Node]Object
    
    // Selections maps selector expressions to their corresponding selections.
    Selections map[*ast.SelectorExpr]*Selection
    
    // Scopes maps ast.Nodes to the scopes they define.
    Scopes map[ast.Node]*Scope
    
    // InitOrder is the list of package-level initializers in the order in which they must be executed.
    InitOrder []*Initializer
    
    // FileVersions maps *ast.File nodes to their Go version string.
    FileVersions map[*ast.File]string
}

// go/types/api.go
type Config struct {
    GoVersion string
    Error     func(err error)
    Importer  Importer
    Sizes     Sizes
}
```

Type checking is invoked via:

```go
func (conf *Config) Check(path string, fset *token.FileSet, files []*ast.File, info *Info) (*Package, error)
```

Source: [golang/go — src/go/types/api.go](https://github.com/golang/go/blob/master/src/go/types/api.go)

### Immutability

`types.Info` is a **mutable struct** — the caller pre-allocates the maps and the type checker populates them during `Check()`. After `Check()` returns, the maps are effectively frozen (by convention, not enforcement).

`types.Package` returned from `Check()` is also mutable during checking but frozen after.

### Diagnostics: inline or separate?

Separate. Errors are reported through a callback:

```go
type Config struct {
    Error func(err error)  // called for each error during checking
}
```

The `error` values are typically `*types.Error`:

```go
type Error struct {
    Fset *token.FileSet
    Pos  token.Pos
    Msg  string
    Soft bool  // if true, error is "soft" (e.g., unused import)
}
```

`Check()` also returns an `error` for the first hard error encountered.

### Consumer access

| Consumer | Access pattern |
|----------|---------------|
| **CLI** (`go build`) | `go/types.Config.Check()` called per package. Results flow to compiler backend. |
| **gopls** (LSP) | Uses `golang.org/x/tools/go/packages` which returns `Package` structs with `Types *types.Package`, `TypesInfo *types.Info`, `Fset`, `Syntax`. Incremental via `go/analysis` framework. |
| **Static analysis** | `go/analysis.Analyzer` framework passes `*analysis.Pass` containing `TypesInfo`, `Pkg`, `Fset`. |

### Metadata traveling with the result

- **Type map**: `Info.Types` — every expression → its type and value
- **Object map**: `Info.Defs` / `Info.Uses` — identifier → declared/used object
- **Scope tree**: `Info.Scopes` — AST node → scope
- **Package**: `*types.Package` with exported type information
- **Initialization order**: `Info.InitOrder`

### Partial success model

Yes. `Config.Error` is called for each error but type checking continues. The returned `(*Package, error)` gives a valid `Package` even when errors exist — the `types.Info` maps are populated for everything that could be resolved.

### Caching and reuse

- **Export data**: `.a` archive files contain serialized type information for import
- **go/packages**: Caches loaded packages in memory
- **gopls**: Maintains a cache of type-checked packages with invalidation on file change
- **Build cache**: `$GOPATH/pkg/` and `$GOCACHE` cache compiled packages

---

## CEL (Common Expression Language)

### Compilation result type

CEL has a multi-stage result pipeline:

```go
// cel/env.go

// Ast representing the checked or unchecked expression, its source, and
// related metadata such as source position information.
type Ast struct {
    source Source
    impl   *celast.AST
}

func (ast *Ast) IsChecked() bool           // whether type-checked
func (ast *Ast) OutputType() *Type          // result type (DynType if unchecked)
func (ast *Ast) Source() Source             // original source

// Env encapsulates the context necessary to perform parsing, type checking,
// or generation of evaluable programs for different expressions.
type Env struct {
    Container       *containers.Container
    variables       []*decls.VariableDecl
    functions       map[string]*decls.FunctionDecl
    macros          []Macro
    // ...
}

// Issues defines methods for inspecting the error details of parse and check calls.
type Issues struct {
    errs *common.Errors
    info *celast.SourceInfo
}

func (i *Issues) Err() error              // non-nil if fatal errors
func (i *Issues) Errors() []*Error        // granular error list
func (i *Issues) String() string          // display string

// Program is an evaluable view of an Ast.
type Program interface {
    Eval(any) (ref.Val, *EvalDetails, error)
    ContextEval(context.Context, any) (ref.Val, *EvalDetails, error)
}

// EvalDetails holds additional information observed during Eval().
type EvalDetails struct {
    state       interpreter.EvalState
    costTracker *interpreter.CostTracker
}
```

Source: [google/cel-go — cel/env.go](https://github.com/google/cel-go/blob/master/cel/env.go), [cel/program.go](https://github.com/google/cel-go/blob/master/cel/program.go)

### Three-stage pipeline

```go
// Parse only
ast, iss := env.Parse(txt)              // returns *Ast (unchecked), *Issues

// Parse + Check
ast, iss := env.Compile(txt)            // returns *Ast (checked), *Issues
// or separately:
ast, iss := env.Parse(txt)
checked, iss := env.Check(ast)

// Program (evaluable)
prg, err := env.Program(ast, opts...)   // returns Program, error
val, details, err := prg.Eval(vars)     // evaluation
```

### Immutability

- `Ast` is immutable once created by `Parse()` or `Check()`
- `Env` is immutable after `configure()` — extension is via `Env.Extend()` which creates a new `Env`
- `Program` is immutable after creation
- `Issues` is mutable (supports `Append`, `ReportErrorAtID`)

### Diagnostics: inline or separate?

Separate — returned as `*Issues`:

```go
type Issues struct {
    errs *common.Errors
    info *celast.SourceInfo
}
```

The `Ast` does not carry errors. If `Check()` fails, it returns `(nil, *Issues)`. If non-fatal issues exist, it returns `(*Ast, *Issues)`.

### Consumer access

| Consumer | Access pattern |
|----------|---------------|
| **CLI/service** | `env.Compile()` → `env.Program()` → `prg.Eval()` |
| **Policy engines** (e.g., Kubernetes admission) | Pre-compile policies to `Program`, evaluate per-request |
| **Cost estimation** | `env.EstimateCost(ast, estimator)` for pre-execution cost analysis |

### Metadata traveling with the result

- **Type information**: `Ast.OutputType()` returns the expression's type; checked AST has types on every node via `ast.GetType(id)`
- **Source info**: `SourceInfo` with position mappings, macro call tracking
- **Residual AST**: `env.ResidualAst()` produces a reduced AST after partial evaluation

### Partial success model

Yes:
- Parse returns `(*Ast, *Issues)` — both can be non-nil
- Check returns `(*Ast, *Issues)` — non-nil Ast does NOT imply valid
- Issues has `.Err()` which is non-nil only if there are fatal errors
- Doc says: "It is possible to have both non-nil Ast and Issues values returned from this call; however, the mere presence of an Ast does not imply that it is valid for use."

### Caching and reuse

- `Env` caches the `checker.Env` lazily (`chkOnce sync.Once`)
- `Program` pre-computes the interpretable plan; can be reused across evaluations
- `Env.Extend()` carries forward validated declarations from the parent `Env`
- Activation pooling: `activationPool` and `ctxActivationPool` reuse allocation

---

## Rego (OPA)

### Compilation result type

OPA's Rego compiler produces `ast.Compiler`:

```go
// ast/compile.go
type Compiler struct {
    Modules    map[string]*Module
    ModuleTree *ModuleTreeNode
    RuleTree   *TreeNode
    
    // Errors accumulated during compilation
    Errors Errors
    
    // Type environment
    TypeEnv *TypeEnv
    
    // After compilation, the comprehension index
    comprehensionIndices map[*Term]*ComprehensionIndex
    
    // Rule graph for dependency analysis
    Graph *Graph
    
    // Stage tracking
    stage string
}

// ast/policy.go
type Module struct {
    Package    *Package
    Imports    []*Import
    Annotations []*Annotations
    Rules      []*Rule
    Comments   []*Comment
}
```

Source: [open-policy-agent/opa — ast/compile.go](https://github.com/open-policy-agent/opa/blob/main/ast/compile.go)

### Immutability

`ast.Compiler` is **mutable** during compilation — `Compile()` populates fields in-place through multiple passes. After `Compile()` returns, the struct is effectively frozen.

### Diagnostics: inline or separate?

Inline — `Compiler.Errors` is populated during compilation:

```go
type Errors []*Error

type Error struct {
    Code     string
    Message  string
    Location *Location
    Details  interface{}
}
```

Also accessible via `Compiler.Failed()` bool.

### Consumer access

| Consumer | Access pattern |
|----------|---------------|
| **CLI** (`opa eval`) | `ast.CompileModules()` → `rego.New().PrepareForEval()` → `pq.Eval()` |
| **Library** (Go SDK) | `rego.Rego` builder assembles query + modules, calls `PrepareForEval()` returning `PreparedEvalQuery` |
| **Bundle system** | `bundle.Bundle` carries compiled modules + data; loaded into `storage.Store` |

### Partial evaluation

OPA supports partial evaluation, returning a `PartialQueries` result:

```go
type PartialQueries struct {
    Queries  []Body
    Support  []*Module
}
```

This is a compilation result that represents a residual program — queries that couldn't be fully evaluated given the available data.

### Metadata traveling with the result

- **Type environment**: `TypeEnv` maps rules to their inferred types
- **Rule graph**: Dependency graph between rules
- **Module tree**: Package hierarchy
- **Comprehension indices**: Optimization metadata for set/object comprehensions
- **Annotations**: Schema annotations on rules

### Partial success model

No — `Compiler.Failed()` returns true if any error occurred, and downstream consumers check this before proceeding. However, the `Errors` slice contains all accumulated errors, not just the first one.

### Caching and reuse

- **Bundle caching**: Compiled bundles can be persisted and loaded
- **Prepared queries**: `PreparedEvalQuery` is a pre-compiled query that can be evaluated multiple times
- **Partial compilation cache**: Wasm/IR compilation results cached for repeated evaluation

---

## Dhall

### Compilation result type

Dhall's compilation result is a **normalized expression** — the fully evaluated, canonical form of the input:

```haskell
-- dhall/src/Dhall/Core.hs
data Expr s a
    = Const Const           -- Type, Kind, Sort
    | Var Var               -- Variable
    | Lam (Maybe Text) (Expr s a) (Expr s a)   -- Lambda
    | Pi (Maybe Text) (Expr s a) (Expr s a)    -- Pi type
    | App (Expr s a) (Expr s a)                -- Application
    | Let (Binding s a) (Expr s a)             -- Let binding
    | Annot (Expr s a) (Expr s a)              -- Type annotation
    | Bool | BoolLit Bool
    | Natural | NaturalLit Natural
    | Integer | IntegerLit Integer
    | Double | DoubleLit DhallDouble
    | Text | TextLit (Chunks s a)
    | List | ListLit (Maybe (Expr s a)) (Seq (Expr s a))
    | Record (Map Text (RecordField s a))
    | RecordLit (Map Text (RecordField s a))
    | Union (Map Text (Maybe (Expr s a)))
    | ...
    | Import (Import s a)                      -- Before resolution
    | Note s (Expr s a)                        -- Source annotation
    | Embed a                                  -- Resolved import
```

The pipeline is:

```haskell
-- 1. Parse
parse :: Text -> Either ParseError (Expr Src Import)

-- 2. Resolve imports
resolve :: Expr Src Import -> IO (Expr Src Void)

-- 3. Type check
typeOf :: Expr Src Void -> Either TypeError (Expr Src Void)

-- 4. Normalize
normalize :: Expr s a -> Expr s a
```

Source: [dhall-lang/dhall-haskell — dhall/src/Dhall/](https://github.com/dhall-lang/dhall-haskell/tree/master/dhall/src/Dhall)

### Immutability

All `Expr` values are immutable (Haskell data). Each transformation produces a new `Expr`.

### Diagnostics: inline or separate?

Separate — errors are returned as `Left` values in `Either`:

```haskell
data TypeError = TypeError
    { context     :: Context (Expr Src Void)
    , current     :: Expr Src Void
    , typeMessage :: TypeMessage Src Void
    }
```

### Consumer access

Single pipeline — Dhall doesn't have separate CLI/IDE modes. The `input` function combines all phases:

```haskell
input :: Decoder a -> Text -> IO a
-- equivalent to: parse → resolve → typecheck → normalize → decode
```

### Metadata traveling with the result

- **`Src` annotations**: Source position information (stripped during normalization unless preserved)
- **Type**: `typeOf` returns the type as another `Expr`
- **Normalized form**: The canonical representation after beta-normalization and alpha-normalization

### Partial success model

No — each phase either succeeds or fails. There's no "expression with errors" representation.

### Caching and reuse

- **Import caching**: Resolved imports are cached by their hash in `~/.cache/dhall/` (content-addressed by SHA256)
- **Semantic hash**: `Dhall.hash` computes a canonical hash of a normalized expression for integrity checking
- **Freeze**: Imports can be "frozen" with their hash to enable caching and integrity verification

---

## Jsonnet

### Compilation result type

Jsonnet evaluates to a JSON value. The result type depends on the implementation:

**C++ (google/jsonnet):**

```cpp
// core/vm.h
class Interpreter {
public:
    // Evaluate to a JSON string
    std::string evaluate(const AST *ast, ...);
    
    // Evaluate to multiple files
    std::map<std::string, std::string> evaluateMulti(const AST *ast, ...);
    
    // Evaluate to a stream of JSON values
    std::vector<std::string> evaluateStream(const AST *ast, ...);
};
```

**Go (google/go-jsonnet):**

```go
// vm.go
type VM struct { ... }

func (vm *VM) EvaluateAnonymousSnippet(filename string, snippet string) (string, error)
func (vm *VM) EvaluateFile(filename string) (string, error)

// For AST access:
func SnippetToAST(filename string, snippet string) (ast.Node, error)
```

Source: [google/jsonnet](https://github.com/google/jsonnet), [google/go-jsonnet](https://github.com/google/go-jsonnet)

### Immutability

The AST (`ast.Node`) is immutable after parsing. The `VM` is mutable (configurable with ext vars, native functions, etc.) but evaluation produces immutable string output.

### Diagnostics: inline or separate?

Separate — errors returned as `error` (Go) or exceptions (C++). Jsonnet has no warning concept; it either succeeds or fails.

### Consumer access

Single-mode: parse → evaluate → JSON string output. No separate IDE-specific result type. Language servers (e.g., `jsonnet-language-server`) re-parse and walk the AST for completions/diagnostics.

### Metadata traveling with the result

Minimal — the result is a JSON string. No type information, no source maps. The AST carries source locations but they are lost in the output.

### Partial success model

No — evaluation either produces a JSON string or an error. There's no "partial JSON" result.

### Caching and reuse

- **Import caching**: `VM` caches imported files
- **AST reuse**: The AST can be parsed once and evaluated multiple times with different ext vars

---

## CUE

### Compilation result type

CUE's universal result type is `cue.Value`:

```go
// cue/types.go
type Value struct {
    idx     *runtime.Runtime
    v       *adt.Vertex
    parent_ *parent
}

// Kind determines the underlying type
func (v Value) Kind() Kind
func (v Value) IncompleteKind() Kind
func (v Value) IsConcrete() bool
func (v Value) Exists() bool
func (v Value) Err() error

// Evaluation
func (v Value) Eval() Value
func (v Value) Default() (Value, bool)
func (v Value) Unify(w Value) Value

// Traversal
func (v Value) LookupPath(p Path) Value
func (v Value) Fields(opts ...Option) (*Iterator, error)
func (v Value) List() (Iterator, error)

// Conversion
func (v Value) MarshalJSON() ([]byte, error)
func (v Value) Syntax(opts ...Option) ast.Node
func (v Value) Validate(opts ...Option) error

// Reference tracking
func (v Value) ReferencePath() (root Value, p Path)
func (v Value) Expr() (Op, []Value)

// Subsumption
func (v Value) Subsume(w Value, opts ...Option) error
```

Source: [cue-lang/cue — cue/types.go](https://github.com/cue-lang/cue/blob/master/cue/types.go)

### Key design: Value carries type + value + constraints

CUE's `Value` is unique because it simultaneously represents:
1. A **concrete value** (e.g., `42`)
2. A **type constraint** (e.g., `int`)
3. A **partially evaluated constraint** (e.g., `>=0 & <=100`)
4. An **error** (bottom value)

The internal representation is `adt.Vertex`:

```go
// internal/core/adt/expr.go
type Vertex struct {
    Parent      *Vertex
    Label       Feature
    BaseValue   Value        // the evaluated value
    Arcs        []*Vertex    // child fields
    Conjuncts   []Conjunct   // contributing constraints
    
    // Structural properties
    ClosedNonRecursive bool
    ClosedRecursive    bool
    HasEllipsis        bool
    IsData             bool
    
    // Pattern constraints
    PatternConstraints *PatternConstraints
}
```

### Immutability

`Value` is considered immutable:

> "A Value is considered immutable: methods may be called concurrently."

Internally, `adt.Vertex` is mutable during evaluation (`Finalize()` fills in `BaseValue`) but is frozen after finalization.

### Diagnostics: inline or separate?

**Inline** — errors are part of the value lattice. A `Value` with `Kind() == BottomKind` IS the error:

```go
func (v Value) Err() error  // returns error if v is bottom

// Errors can be structural
func (v Value) Validate(opts ...Option) error  // recursive validation
```

`BottomKind` carries error details:

```go
type Bottom struct {
    Code ErrorCode
    Err  errors.Error
    // ...
}
```

### Consumer access

| Consumer | Access pattern |
|----------|---------------|
| **CLI** (`cue eval`, `cue export`) | `cue.Context.BuildInstance()` → `Value` → `MarshalJSON()` or `Syntax()` |
| **Go API** | `cue.Context.CompileString()` or `BuildInstance()` → work with `Value` directly |
| **Validation** | `Value.Validate(Concrete(true))` checks for completeness |

### Metadata traveling with the result

- **Type + value unified**: Every `Value` simultaneously carries its type constraints and concrete value
- **Source positions**: `Value.Pos()` returns source position; `Value.Source()` returns AST node
- **Path information**: `Value.Path()` returns the structural path from root
- **Reference tracking**: `Value.ReferencePath()` follows references
- **Documentation**: `Value.Doc()` returns associated comments
- **Attributes**: CUE attributes on fields

### Partial success model

Yes — by design. CUE values exist on a lattice from `_` (top/any) to `_|_` (bottom/error). A value can be partially concrete — some fields resolved, others still open constraints. `IsConcrete()` checks if fully resolved; `IncompleteKind()` reports potential kinds for non-concrete values.

### Caching and reuse

- **Runtime interning**: `runtime.Runtime` interns labels and manages module loading
- **Vertex sharing**: Internal structure sharing via `adt.Vertex` graph
- **Finalization caching**: `Vertex.Finalize()` is idempotent; called lazily on access

---

## Starlark

### Compilation result type

Starlark (the Python-like configuration language used by Bazel) evaluates to a **globals dictionary**:

```go
// starlark-go: starlark/eval.go
func ExecFile(thread *Thread, filename string, src interface{}, predeclared StringDict) (StringDict, error)

// StringDict is the result — a simple map of name → value
type StringDict map[string]Value

// Thread is the execution context
type Thread struct {
    Name string
    
    // Print function
    Print func(thread *Thread, msg string)
    
    // Load callback for loading modules
    Load func(thread *Thread, module string) (StringDict, error)
    
    // Call stack
    callStack []callFrame
    
    // Profiling
    // ...
}
```

Source: [google/starlark-go — starlark/eval.go](https://github.com/google/starlark-go/blob/master/starlark/eval.go)

### Immutability

The returned `StringDict` is mutable (it's a Go map). Individual `Value` objects may be mutable (lists, dicts) or immutable (strings, ints, tuples) depending on whether they've been "frozen" via `Value.Freeze()`.

Starlark's freeze mechanism:
```go
type Value interface {
    String() string
    Type() string
    Freeze()       // marks value as immutable
    Truth() Bool
    Hash() (uint32, error)
}
```

### Diagnostics: inline or separate?

Separate — errors returned as Go `error`:

```go
type EvalError struct {
    Msg       string
    CallStack CallStack
    cause     error
}
```

No warning mechanism exists. Parse errors are `syntax.Error`:

```go
type Error struct {
    Pos token.Position
    Msg string
}
```

### Partial success model

No — `ExecFile` either returns `(StringDict, nil)` on success or `(nil, error)` on failure.

### Caching and reuse

- **Module loading**: `Thread.Load` callback is responsible for caching loaded modules
- **Frozen values**: Frozen values can be safely shared across threads

---

## Pkl

### Compilation result type

Pkl evaluates modules to typed value representations:

```kotlin
// pkl-core/src/main/java/org/pkl/core/ModuleOutput.kt (approximate)
class ModuleOutput {
    val moduleUri: URI
    val result: PObject  // the evaluated module as a typed object
}

// Value types
sealed class PklValue
class PObject(val classInfo: PClassInfo, val properties: Map<String, Any?>) : PklValue()
class PString(val value: String) : PklValue()
class PInt(val value: Long) : PklValue()
class PFloat(val value: Double) : PklValue()
class PBoolean(val value: Boolean) : PklValue()
class PNull : PklValue()
class PList(val elements: List<Any?>) : PklValue()
class PMap(val entries: Map<Any?, Any?>) : PklValue()
class PDuration(val value: Double, val unit: DurationUnit) : PklValue()
class PDataSize(val value: Double, val unit: DataSizeUnit) : PklValue()
```

Source: [apple/pkl](https://github.com/apple/pkl)

The evaluator API:

```kotlin
// Main evaluation entry point
class Evaluator {
    fun evaluateModule(moduleUri: URI): ModuleOutput
    fun evaluateExpression(moduleUri: URI, expression: String): Any?
    fun evaluateOutputText(moduleUri: URI): String
    fun evaluateOutputValue(moduleUri: URI): Any?
    fun evaluateOutputFiles(moduleUri: URI): Map<String, String>
}
```

### Immutability

Evaluated `PObject` values are immutable. The evaluator itself is stateful during evaluation but results are frozen.

### Diagnostics: inline or separate?

Separate — errors are thrown as `PklException` or `PklBugException`. No partial-error model.

### Metadata traveling with the result

- **Class info**: `PObject.classInfo` carries the type/class metadata of the evaluated object
- **Property types**: Available through the class schema
- **Module URI**: Origin tracking

### Caching and reuse

- **Module caching**: Evaluated modules cached by URI
- **Project dependencies**: `PklProject` resolves and caches dependencies

---

## Dafny

### Compilation result type

Dafny has a multi-stage pipeline with distinct result types:

```csharp
// Source/DafnyCore/DafnyDriver.cs (approximate)
public class DafnyDriver {
    // Pipeline stages
    public Program Parse(string filename);
    public void Resolve(Program program);
    public bool Verify(Program program);
    public bool Compile(Program program);
}

// Source/DafnyCore/AST/Program.cs
public class Program {
    public readonly string Name;
    public readonly ModuleDecl DefaultModule;
    public readonly BuiltIns BuiltIns;
    public ErrorReporter Reporter;
    
    // Resolved types, methods, functions
    public readonly Dictionary<TopLevelDecl, Dictionary<string, MemberDecl>> Members;
    
    // Verification results
    public readonly List<VerificationResult> VerificationResults;
}

// Verification result
public class VerificationResult {
    public readonly string MethodName;
    public readonly VCGenOutcome Outcome;  // Correct, Errors, TimedOut, OutOfMemory, etc.
    public readonly List<Counterexample> Counterexamples;
    public readonly TimeSpan Duration;
}
```

Source: [dafny-lang/dafny](https://github.com/dafny-lang/dafny)

### Verification conditions

Dafny generates verification conditions (VCs) that are discharged by Boogie/Z3:

```csharp
public enum VCGenOutcome {
    Correct,
    Errors,
    TimedOut,
    OutOfMemory,
    Inconclusive
}

// Each VC maps to a source location
public class Counterexample {
    public readonly List<Block> Trace;
    public readonly Model Model;  // Z3 model showing why verification failed
}
```

### Immutability

`Program` is **mutable** — resolution and type checking modify it in-place. Verification results are appended to the program.

### Diagnostics: inline or separate?

Both:
- **Parse/resolve errors**: collected by `ErrorReporter` (separate)
- **Verification results**: attached to `Program.VerificationResults` (inline on the program)

### Consumer access

| Consumer | Access pattern |
|----------|---------------|
| **CLI** (`dafny verify`) | Full pipeline: parse → resolve → verify → report |
| **VS Code extension** | LSP server runs verification in background, reports diagnostics |
| **Library** | Dafny can be used as a library to build custom verifiers |

### Partial success model

Yes — verification is per-method. Some methods may verify while others fail. The `Program` object carries per-method `VerificationResult` values with individual outcomes.

### Caching and reuse

- **Incremental verification**: Only re-verify methods that changed
- **Verification cache**: Boogie can cache VC results across runs
- **Proof dependencies**: Tracked to determine what needs re-verification

---

## Boogie

### Compilation result type

Boogie's core result types:

```csharp
// Source/Core/AST/Program.cs
public class Program {
    public List<Declaration/*!*/> TopLevelDeclarations;
    
    // After resolution
    public readonly Dictionary<string, Function> functionMap;
    public readonly Dictionary<string, Procedure> procedureMap;
    public readonly Dictionary<string, GlobalVariable> globalVariableMap;
}

// Verification condition generation
public class VCGen {
    public enum Outcome {
        Correct,
        Errors,
        TimedOut,
        OutOfMemory,
        Inconclusive,
        ReachedBound
    }
    
    // Generate VCs for a procedure implementation
    public Outcome VerifyImplementation(
        Implementation impl,
        out List<Counterexample> errors);
}

// Counterexample carries the failing trace
public class Counterexample {
    public readonly List<Block> Trace;
    public readonly Model Model;
    public readonly List<AssignedExpression> AssignedExpressions;
    
    // Map back to source
    public IToken FailingNode;
}
```

Source: [boogie-org/boogie](https://github.com/boogie-org/boogie)

### Immutability

`Program` is mutable — resolution modifies declarations in-place. VC generation creates new objects.

### Diagnostics: inline or separate?

Separate — `VCGen.VerifyImplementation()` returns an `Outcome` enum and an output list of `Counterexample` objects.

### Verification results mapped to source

Boogie maps verification failures back to source via `IToken`:

```csharp
public interface IToken {
    int kind { get; set; }
    string filename { get; set; }
    int pos { get; set; }
    int col { get; set; }
    int line { get; set; }
    string val { get; set; }
}
```

Each `Counterexample` contains a `FailingNode` token that points to the assertion/postcondition that was violated.

### Consumer access

| Consumer | Access pattern |
|----------|---------------|
| **Dafny** | Creates Boogie `Program`, calls `VCGen.VerifyImplementation()` per procedure |
| **SMACK** (C verifier) | Translates LLVM IR to Boogie, uses same verification pipeline |
| **Corral** (bounded verifier) | Extends Boogie with bounded model checking |

### Partial success model

Yes — verification is per-implementation. Each has an independent `Outcome`. The program-level result aggregates individual outcomes.

### Caching and reuse

- **Verification condition caching**: Z3 can reuse learned lemmas across queries
- **Incremental solving**: `push`/`pop` on the solver state
- **Snapshot verification**: Boogie supports comparing old vs new VCs to skip unchanged procedures

---

## Summary of Structural Patterns (raw data, not interpretation)

| System | Result type | Immutable? | Errors inline? | Partial success? |
|--------|------------|------------|----------------|------------------|
| Roslyn | `Compilation` | Yes | No (separate methods) | Yes |
| TypeScript | `Program` | Yes (effectively) | No (separate methods) | Yes |
| Rust | `TyCtxt` (query DB) | Yes (interned) | No (via `Session`) | Yes (`ErrorGuaranteed`) |
| Kotlin K2 | `FirSession` + FIR tree | No (mutated in phases) | No (via `DiagnosticReporter`) | Yes (`ConeErrorType`) |
| Swift | `ASTContext` + `SourceFile` | No (mutated by type checker) | No (via `DiagnosticEngine`) | Yes (`ErrorType`) |
| Go | `types.Info` | No (caller-allocated maps) | No (via callback) | Yes |
| CEL | `Ast` → `Program` | Yes | No (separate `Issues`) | Yes (Ast + Issues both non-nil) |
| Rego (OPA) | `ast.Compiler` | No (mutated during compile) | Yes (`Compiler.Errors`) | No (`Failed()` is binary) |
| Dhall | `Expr` (normalized) | Yes (Haskell data) | No (`Either`) | No |
| Jsonnet | JSON string | Yes | No (`error`) | No |
| CUE | `cue.Value` | Yes | Yes (bottom is a value) | Yes (lattice-based) |
| Starlark | `StringDict` | No (Go map) | No (`error`) | No |
| Pkl | `PObject` | Yes | No (exception) | No |
| Dafny | `Program` + `VerificationResult[]` | No (mutated) | Both | Yes (per-method) |
| Boogie | `Program` + `VCGen.Outcome` | No (mutated) | No (return value) | Yes (per-procedure) |
