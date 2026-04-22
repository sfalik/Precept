# Compiler Pipeline Architecture Survey

External research: how different compiler/DSL systems organize their pipeline stages, intermediate representations, error handling, and public API surfaces.

**Scope:** Raw collection of facts about each system's architecture. No interpretation for Precept, no conclusions, no recommendations.

---

## Roslyn (C# / Visual Basic)

### Pipeline Stages

Roslyn exposes four explicit pipeline stages, each a separate component:

1. **Parse** — Source text is tokenized and parsed into syntax that follows the language grammar. Produces a `SyntaxTree`.
2. **Declaration** — Declarations from source and imported metadata are analyzed to form named symbols. Produces a hierarchical symbol table.
3. **Bind** — Identifiers in the code are matched to symbols. Produces a `SemanticModel` that exposes the compiler's semantic analysis.
4. **Emit** — All information built up by the compiler is emitted as an assembly. Produces IL byte codes via the Emit API.

Each phase surfaces a corresponding object model. The parsing phase → syntax tree; declaration phase → symbol table; binding phase → semantic model; emit phase → IL output.

Source: [Roslyn Overview — Compiler Pipeline Functional Areas](https://github.com/dotnet/roslyn/wiki/Roslyn-Overview)

### Types at Each Stage Boundary

```
Stage 1 (Parse):
  Input:  string (source text), SourceText
  Output: SyntaxTree (containing SyntaxNode, SyntaxToken, SyntaxTrivia)

Stage 2 (Declarations):
  Input:  SyntaxTree[]
  Output: Symbol table (INamespaceSymbol, ITypeSymbol, IMethodSymbol, etc.)
  
Stage 3 (Bind):
  Input:  SyntaxTree + Symbol table
  Output: SemanticModel (binds syntax nodes to symbols, resolves types)
  
Stage 4 (Emit):
  Input:  Compilation (aggregates all of the above)
  Output: PE stream, IL byte codes, PDB
```

The key aggregation type is `Compilation`:

```csharp
// Simplified public API
public abstract class Compilation
{
    public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
    public ImmutableArray<MetadataReference> References { get; }
    public SemanticModel GetSemanticModel(SyntaxTree tree);
    public INamespaceSymbol GlobalNamespace { get; }
    public ImmutableArray<Diagnostic> GetDiagnostics();
    public EmitResult Emit(Stream peStream);
}
```

### Public vs. Internal

Roslyn is designed as a "compiler as a platform" — the public surface is extensive:

- **Public:** `SyntaxTree`, `SyntaxNode`, `SyntaxToken`, `SyntaxTrivia`, `Compilation`, `SemanticModel`, `ISymbol` hierarchy, `Diagnostic`, `Emit` API, `Workspace` API, all `CSharpSyntaxKind`/`VisualBasicSyntaxKind` enums.
- **Internal:** Binder implementation details, flow analysis internals, lowering passes (converting C# constructs to simpler IL patterns), the actual code generation backend.

The entire IDE feature set (IntelliSense, refactoring, Find All References, Go to Definition, formatting, outlining) is built on the public API layer. This was a design constraint: "To ensure that the public Compiler APIs are sufficient for building world-class IDE features, the language services [...] have been rebuilt using them."

### Error Handling

**Collect model.** Roslyn does NOT short-circuit on errors. The parser produces a full syntax tree even for malformed code, using two techniques:

1. **Missing tokens** — If the parser expects a token but doesn't find it, it inserts a missing token (empty span, `IsMissing == true`).
2. **Skipped tokens** — If the parser encounters unexpected tokens, it skips them and attaches them as `SkippedTokensTrivia`.

Syntax trees are always:
- **Full fidelity** — every piece of source information is preserved
- **Round-trippable** — `tree.GetText().ToString()` reproduces the original source exactly
- **Immutable and thread-safe** — snapshot semantics

Diagnostics are collected as `ImmutableArray<Diagnostic>` and can be retrieved at each stage: `SyntaxTree.GetDiagnostics()`, `Compilation.GetDiagnostics()`, `SemanticModel.GetDiagnostics()`.

### Compile Result

The caller gets a `Compilation` object, which is immutable. From it, they can:

```csharp
var compilation = CSharpCompilation.Create("MyAssembly")
    .AddSyntaxTrees(tree)
    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

// Get diagnostics (errors/warnings)
var diagnostics = compilation.GetDiagnostics();

// Get semantic info for a specific tree
var semanticModel = compilation.GetSemanticModel(tree);

// Emit IL
var emitResult = compilation.Emit(stream);
// emitResult.Success, emitResult.Diagnostics
```

### Intermediate Representations

- **SyntaxTree (AST):** Concrete syntax tree. Full fidelity, round-trippable. Red-green tree design: green nodes are immutable/shared, red nodes carry parent/position information on demand.
- **Bound tree (internal):** After binding, an internal "bound tree" represents the semantically resolved form. Not publicly exposed.
- **Lowered tree (internal):** Further desugaring (e.g., foreach → while, async → state machine). Not publicly exposed.
- **IL:** The final output.

The red-green tree pattern is notable: green nodes are reusable across edits (for incremental parsing), while red nodes are created lazily on demand for navigation.

### Codebase Size

- Repository: `dotnet/roslyn` on GitHub
- Approximately **4–5 million lines of C#** across the compiler, workspaces, IDE features, and tests
- ~10,000+ source files
- Test suite is extremely comprehensive, approaching ~100,000 test cases

### Entry Points

Multiple paths:

- `CSharpCompilation.Create(...)` → full compilation pipeline
- `CSharpSyntaxTree.ParseText(...)` → parse only
- `compilation.GetSemanticModel(tree)` → lazy binding for a single file
- `Workspace` API → solution/project level, used by IDE tooling
- Scripting API → `CSharpScript.EvaluateAsync(...)` for REPL scenarios

---

## TypeScript

### Pipeline Stages

TypeScript's compiler (`tsc`) has the following stages:

1. **Scanner/Lexer** (`scanner.ts`) — Tokenizes source text into a stream of `SyntaxKind` tokens.
2. **Parser** (`parser.ts`) — Produces an AST of `Node` objects (`SourceFile` is the root).
3. **Binder** (`binder.ts`) — Walks the AST and creates `Symbol` objects, establishing the scope chain and flow containers.
4. **Type Checker** (`checker.ts`) — Resolves types, performs type inference, checks assignability. This is the largest single file in the codebase (~50,000+ lines).
5. **Emitter** (`emitter.ts`) — Produces JavaScript output (and optionally `.d.ts` declaration files).
6. **Transformer** (`transformer.ts` and many transform files) — Between checking and emitting, TypeScript applies transforms to downlevel syntax (e.g., async/await, decorators, JSX).

### Types at Each Stage Boundary

```
Scanner → Parser:
  Token stream (SyntaxKind enum values + text positions)

Parser → Binder:
  SourceFile (extends Node): the root AST node
  Node interface: kind, pos, end, parent, flags

Binder → Checker:
  SourceFile with Symbol objects attached
  Symbol: name, flags, declarations[], valueDeclaration, members, exports

Checker outputs:
  Type objects (resolved): ObjectType, UnionType, IntersectionType, 
  TypeParameter, IndexedAccessType, ConditionalType, etc.

Emitter input:
  SourceFile (possibly transformed)
  
Emitter output:
  JavaScript text, source maps, declaration files
```

Key interfaces (from `types.ts`):

```typescript
interface Node {
    kind: SyntaxKind;
    flags: NodeFlags;
    pos: number;
    end: number;
    parent: Node;
}

interface Symbol {
    flags: SymbolFlags;
    escapedName: __String;
    declarations?: Declaration[];
    valueDeclaration?: Declaration;
    members?: SymbolTable;
    exports?: SymbolTable;
}

interface Type {
    flags: TypeFlags;
    symbol: Symbol;
    // ... varies by type kind
}
```

### Lazy Evaluation

TypeScript uses **lazy/on-demand evaluation** extensively:

- `Program.getTypeChecker()` returns a `TypeChecker` that resolves types on demand.
- `checker.getTypeOfSymbolAtLocation(symbol, node)` — types are computed when queried, not eagerly for the whole program.
- The type checker caches results aggressively. Once a symbol's type is resolved, it's memoized.
- `SourceFile` objects are cached by the `Program` — if a file hasn't changed, its parsed AST is reused.

The `Program` object is the top-level coordinator:

```typescript
interface Program {
    getRootFileNames(): readonly string[];
    getSourceFile(fileName: string): SourceFile | undefined;
    getTypeChecker(): TypeChecker;
    getSemanticDiagnostics(sourceFile?: SourceFile): readonly Diagnostic[];
    getSyntacticDiagnostics(sourceFile?: SourceFile): readonly Diagnostic[];
    emit(targetSourceFile?: SourceFile): EmitResult;
}
```

### Public vs. Internal

- **Public API (`typescript.d.ts`):** `Program`, `SourceFile`, `TypeChecker`, `Symbol`, `Type`, `Diagnostic`, `CompilerOptions`, `LanguageService`, `LanguageServiceHost`. Used by editors, build tools, and lint tools.
- **Internal:** The scanner, parser, binder, and emitter are largely internal implementation. `checker.ts` has a massive internal surface. Many internal utility functions exist that plugins cannot access without private API hacks.

The `LanguageService` API is the primary interface for IDE integration, sitting above the compiler API.

### Error Handling

**Collect model.** Like Roslyn, TypeScript always produces a full AST:

- Parse errors produce nodes with the `NodeFlags.ThisNodeHasError` flag.
- Missing tokens are represented with zero-width nodes.
- The parser uses error recovery heuristics to continue parsing after errors.
- Diagnostics are categorized: `getSyntacticDiagnostics()` (parse errors), `getSemanticDiagnostics()` (type errors), `getDeclarationDiagnostics()` (declaration emit errors).

Each `Diagnostic` includes: `file`, `start`, `length`, `messageText`, `category` (Error/Warning/Suggestion/Message), `code`.

### Compile Result

```typescript
interface EmitResult {
    emitSkipped: boolean;
    diagnostics: readonly Diagnostic[];
    emittedFiles?: string[];
}
```

The caller can also get just diagnostics without emitting:

```typescript
const program = ts.createProgram(fileNames, options);
const diagnostics = [
    ...program.getSyntacticDiagnostics(),
    ...program.getSemanticDiagnostics(),
    ...program.getGlobalDiagnostics(),
];
```

### Codebase Size

- Repository: `microsoft/TypeScript` on GitHub
- `checker.ts` alone is ~50,000–55,000 lines (one of the largest single files in any open-source project)
- Total compiler source: ~300,000–400,000 lines of TypeScript
- Test suite: ~80,000+ test cases (baselines, conformance, fourslash tests)

### Entry Points

- `ts.createProgram(files, options)` — batch compilation
- `ts.createLanguageService(host)` — IDE mode with incremental updates
- `ts.transpileModule(source, options)` — single-file transpilation (no type checking)
- Watch mode via `ts.createWatchProgram()` — file system watching with incremental rebuilds

---

## Rust (rustc)

### Pipeline Stages

Rustc's pipeline is organized as a series of intermediate representations, increasingly driven by the **query system** for on-demand computation:

1. **Lexing** (`rustc_lexer`) — Tokenizes source to a stream of tokens. Low-level, no dependencies on the rest of the compiler.
2. **Parsing** (`rustc_parse`) — Recursive descent parser, produces an AST (`rustc_ast`).
3. **Macro Expansion + Name Resolution** (`rustc_expand`, `rustc_resolve`) — Expands macros, resolves names, performs early linting.
4. **AST Lowering → HIR** (`rustc_hir`) — Desugars AST into High-Level Intermediate Representation. Loops, `async fn`, etc. are lowered.
5. **Type Checking** (on HIR) — Type inference (`rustc_infer`), trait solving (`rustc_trait_selection`), type checking (`rustc_hir_typeck`). Produces `Ty<'tcx>`.
6. **THIR Construction** — Typed HIR for pattern/exhaustiveness checking.
7. **MIR Lowering** (`rustc_mir_build`) — Control-flow graph representation.
8. **Borrow Checking** (`rustc_borrowck`) — MIR-level analysis.
9. **MIR Optimizations** (`rustc_mir_transform`) — Inlining, constant propagation, etc.
10. **Monomorphization** — Generic code is specialized for concrete types.
11. **Code Generation** (`rustc_codegen_ssa`, `rustc_codegen_llvm`) — MIR → LLVM-IR → machine code.

### The Query System

Rustc is NOT organized as sequential passes. Instead, it uses a **demand-driven query system**:

- All major steps between HIR and LLVM-IR are organized as queries.
- Queries call each other on demand.
- Results are cached (on disk for incremental compilation).
- If a user changes code, only queries whose inputs changed need to re-execute.

Example query flow:
```
codegen_crate
  → collect_and_partition_mono_items
    → optimized_mir(def_id)
      → mir_borrowck(def_id)
        → mir_built(def_id)
          → typeck(def_id)
            → type_of(def_id)
```

Queries that are NOT yet query-fied (as of recent rustc): lexing, parsing, name resolution, macro expansion — these run eagerly for the whole crate.

### TyCtxt — The Central Context

```rust
pub struct TyCtxt<'tcx> {
    // The arena where all interned types live
    // All queries are methods on TyCtxt
    // In-memory query cache stored here
}
```

`TyCtxt` is the "god object" of the Rust compiler. Convention: variables named `tcx`, lifetime `'tcx`. All interned values (types, constants, predicates) are tied to the `TyCtxt` lifetime.

### Types at Each Stage Boundary

```
Source text
  ↓ Lexer
Token stream (rustc_lexer::Token)
  ↓ Parser
AST (rustc_ast::Crate, Expr, Pat, Ty, Stmt, Item)
  ↓ Lowering
HIR (rustc_hir::Crate — desugared, lifetimes explicit)
  ↓ Type checking
Typed HIR + Ty<'tcx> (the compiler's internal type representation)
  ↓ THIR
THIR (fully typed, more desugared)
  ↓ MIR building
MIR (rustc_middle::mir::Body — CFG with basic blocks, typed statements)
  ↓ Optimizations + Borrow checking
Optimized MIR
  ↓ Codegen
LLVM-IR
  ↓ LLVM
Machine code (ELF, Mach-O, PE, WASM)
```

### Public vs. Internal

Almost everything is **internal** — rustc's API is not designed for external consumption:

- **Public (stable):** Only the CLI interface (`rustc` binary) and Cargo integration.
- **Semi-public (unstable):** `rustc_driver` API for embedding, used by Clippy, Miri, rust-analyzer. Marked `#![feature(rustc_private)]`.
- **Internal:** All IRs, the query system, `TyCtxt`, type representations. These change frequently.

The Language Server Protocol is served by `rust-analyzer`, a separate project that reimplements parsing and type inference for IDE use, rather than using `rustc` directly.

### Error Handling

**Collect model with recovery.** Rustc uses the `Diag` API for structured diagnostics:

- The parser recovers by parsing a superset of Rust's grammar.
- Error recovery inserts missing tokens or skips unexpected ones.
- Diagnostics include spans, suggestions, error codes, and multi-part messages.
- Type checking continues past errors where possible, using error types (`TyKind::Error`) as placeholders.
- The `ErrorGuaranteed` type ensures that if code proceeds past an error point, it carries proof that an error was reported.

### Compile Result

From the CLI: exit code + diagnostic output + output files (binary, rlib, etc.).

From the driver API: `rustc_interface::Queries` provides access to intermediate results:

```rust
// Pseudocode for the interface
queries.parse()?;                    // → AST
queries.expansion()?;                // → Expanded AST
queries.global_ctxt()?.enter(|tcx| {
    tcx.analysis(())?;              // → Type checking, borrow checking
    tcx.codegen_and_link(());       // → Output files
});
```

### Codebase Size

- Repository: `rust-lang/rust` on GitHub
- Compiler source (`compiler/` directory): approximately **600,000–800,000 lines of Rust** across ~30+ crates
- Total repository (including standard library, tests, tools): several million lines
- Test suite: ~20,000+ test files in `tests/` directory

### Intermediate Representations Summary

| IR | Crate | Purpose | Key property |
|---|---|---|---|
| Token stream | `rustc_lexer` | Lexical analysis | Flat, no structure |
| AST | `rustc_ast` | Syntactic structure | Close to source, pre-expansion |
| HIR | `rustc_hir` | Desugared, compiler-friendly | Fully expanded, has DefIds |
| THIR | `rustc_mir_build` | Typed, exhaustiveness checking | Bridge between HIR and MIR |
| MIR | `rustc_middle::mir` | Control-flow graph | Borrow checking, optimizations |
| LLVM-IR | (LLVM) | Low-level typed assembly | LLVM optimizations |

---

## Go (gc)

### Pipeline Stages

Go's compiler (`gc`, in `cmd/compile`) follows a relatively simple, traditional pipeline:

1. **Lexing/Parsing** (`syntax` package) — Hand-written recursive descent parser. Produces a syntax tree.
2. **Type Checking** (`types2` package, based on `go/types`) — Full type checking, including generics (since Go 1.18).
3. **IR Construction (Noding)** — Converts the syntax tree + type info into the compiler's internal IR (`ir.Node`).
4. **Middle-end passes** — Escape analysis, inlining, devirtualization, closure rewriting, order of evaluation.
5. **SSA Generation** (`ssa` package) — Converts IR to Static Single Assignment form.
6. **SSA Optimizations** — Dead code elimination, common subexpression elimination, register allocation.
7. **Machine Code Generation** — Architecture-specific code generation (amd64, arm64, etc.).
8. **Object File Writing** — Produces `.o` files.

### Package-at-a-Time Compilation

Go compiles **one package at a time**. This is fundamental to Go's fast compilation:

- Each package is compiled independently.
- Dependencies are compiled first, producing export data (type information).
- A package reads the export data of its imports — it never sees import source code during compilation.
- This means the compile time for a package depends only on its own source and the size of its import declarations, not the transitive closure.

### Types at Each Stage Boundary

```
Source text (per package)
  ↓ Parser (syntax package)
syntax.File — AST nodes: syntax.Expr, syntax.Stmt, syntax.Decl
  ↓ Type Checker (types2)
types2.Package — type-checked package
  types2.Info — maps syntax nodes to types, objects, scopes
  ↓ Noding
ir.Node — compiler-internal IR
  ↓ SSA
ssa.Func, ssa.Block, ssa.Value — SSA form
  ↓ Codegen
obj.Prog — machine instructions
```

Key public types from `go/types` (the standard library equivalent of `types2`):

```go
type Package struct { /* ... */ }
type Info struct {
    Types      map[ast.Expr]TypeAndValue
    Defs       map[*ast.Ident]Object
    Uses       map[*ast.Ident]Object
    Implicits  map[ast.Node]Object
    Selections map[*ast.SelectorExpr]*Selection
    Scopes     map[ast.Node]*Scope
}
type Object interface {
    Name() string
    Type() Type
    // ...
}
```

### Public vs. Internal

Go has a clear split:

- **Public (stable standard library):** `go/ast`, `go/parser`, `go/types`, `go/token`, `go/scanner` — these are user-facing packages for building Go tools. They mirror the internal packages but are separately maintained.
- **Internal (compiler):** `cmd/compile/internal/syntax`, `cmd/compile/internal/types2`, `cmd/compile/internal/ir`, `cmd/compile/internal/ssa` — these are the actual compiler internals, under `internal/` and not importable by external code.

The `go/types` package is explicitly designed as a public API for type checking Go code, used by tools like `gopls`, `staticcheck`, `golangci-lint`.

### Error Handling

**Collect model.** The parser produces a complete AST even with errors. Error lists are accumulated:

```go
type Error struct {
    Pos token.Position
    Msg string
}
type ErrorList []*Error
```

The type checker similarly collects errors via a callback:

```go
conf := &types.Config{
    Error: func(err error) {
        // Collect or display error
    },
}
```

### Compile Result

- From the CLI: object file (`.o`) or executable binary.
- From `go/types` API:

```go
conf := types.Config{Importer: importer.Default()}
pkg, err := conf.Check("mypkg", fset, files, &info)
// pkg: *types.Package with all type information
// info: *types.Info with expression types, identifier bindings, etc.
// err: first error (but callback may have received more)
```

### Codebase Size

- Go compiler (`cmd/compile`): approximately **200,000–250,000 lines of Go**
- `go/types` standard library package: approximately **30,000 lines**
- Very lean compared to Roslyn or rustc

### Entry Points

- `go build` / `go run` — full compilation via `cmd/compile`
- `go/types.Config.Check()` — programmatic type checking
- `go/parser.ParseFile()` — parse only
- `go vet` / `analysis` framework — static analysis using `go/types`

---

## Kotlin K2 (FIR)

### Pipeline Stages — The New Frontend

Kotlin K2 replaced the old PSI-based frontend with **FIR (Frontend Intermediate Representation)**:

1. **Parsing** — Produces a PSI tree (retained for IDE compatibility) or lightweight tree for CLI.
2. **Raw FIR Construction** — PSI is converted to raw FIR. This is a tree-like IR that is syntactically resolved but not semantically resolved. Declarations have unresolved type references, bodies are not analyzed.
3. **FIR Resolution** — Multiple resolution phases transform raw FIR into resolved FIR:
   - **Phase 1: Super types** — Resolve super type references.
   - **Phase 2: Status** — Resolve modifiers, visibility.
   - **Phase 3: Types** — Resolve return types, property types (but not bodies).
   - **Phase 4: Implicit types** — Resolve types that require body analysis.
   - **Phase 5: Bodies** — Full body resolution (type inference, overload resolution).
   - **Phase 6: Checkers** — Run diagnostic checkers.
4. **FIR → Backend IR** — Resolved FIR is lowered to the Kotlin backend IR (shared with the JVM, JS, and Native backends).
5. **Backend Code Generation** — IR → JVM bytecode, JavaScript, or LLVM bitcode (for Kotlin/Native).

### FIR Architecture Details

FIR elements (in `compiler/fir/tree/`):

```kotlin
// Key FIR node types
sealed class FirElement
class FirFile : FirElement
class FirRegularClass : FirClass
class FirSimpleFunction : FirFunction
class FirProperty : FirVariable
class FirExpression : FirElement
class FirResolvedTypeRef : FirTypeRef  // post-resolution
class FirUserTypeRef : FirTypeRef      // pre-resolution (raw FIR)
```

The resolution transform is **in-place** — FIR nodes are mutated during resolution (not immutable like Roslyn trees). This is a deliberate performance trade-off. The `FirResolvePhase` enum tracks what phase each element has been resolved to.

### Public vs. Internal

- **Public (official):** The Kotlin compiler API (`kotlin-compiler-embeddable`) provides some access, but the FIR API is largely considered internal and unstable.
- **Public for plugins:** The Kotlin Compiler Plugin API and the K2 Analysis API (for IDE plugins) provide a stable surface over FIR.
- **Internal:** FIR tree, resolution phases, checker infrastructure, backend IR.

The Analysis API (`analysis-api/`) is the intended stable interface for tools, providing:

```kotlin
interface KaSession {
    fun KtExpression.getKaType(): KaType
    fun KtDeclaration.getSymbol(): KaDeclarationSymbol
    // etc.
}
```

### Error Handling

**Collect model.** FIR checkers produce diagnostics without halting resolution:

- Diagnostics are collected as `FirDiagnostic` objects with source locations.
- Resolution continues even in the presence of errors — error types (`FirErrorTypeRef`) are used as placeholders.
- The checker phase (Phase 6) runs after full resolution, catching semantic errors.

### Compile Result

From the CLI: `.class` files, `.js` files, or native binaries.

From the embedded compiler API:

```kotlin
val result = K2JVMCompiler().exec(messageCollector, Services.EMPTY, arguments)
// result: ExitCode (OK, COMPILATION_ERROR, INTERNAL_ERROR, etc.)
// Diagnostics collected via messageCollector
```

### Codebase Size

- Repository: `JetBrains/kotlin` on GitHub
- FIR frontend (`compiler/fir/`): approximately **200,000+ lines of Kotlin**
- Total compiler: approximately **1,000,000+ lines** (across frontend, backends, standard library)
- The K2 compiler was a multi-year rewrite effort

---

## Swift

### Pipeline Stages

Swift's compiler (`swiftc`) pipeline:

1. **Lexing/Parsing** (`lib/Parse/`) — Produces a Swift AST.
2. **Semantic Analysis (Sema)** (`lib/Sema/`) — Name lookup, type checking, type inference. Uses a **request evaluator** pattern.
3. **SIL Generation** (`lib/SILGen/`) — AST → raw SIL (Swift Intermediate Language).
4. **SIL Guaranteed Transformations** — Mandatory passes: diagnostics (e.g., move-only type checking, definite initialization, flow-sensitive diagnostics).
5. **SIL Optimizations** — Optional optimization passes (inlining, devirtualization, ARC optimization, generic specialization).
6. **IRGen** (`lib/IRGen/`) — SIL → LLVM-IR.
7. **LLVM** — LLVM-IR → machine code.

### The Request Evaluator Pattern

Swift 5.1+ introduced a **request evaluator** for semantic analysis, similar in spirit to rustc's query system:

- Type checking is driven by "requests" (e.g., `TypeCheckFunctionBodyRequest`, `InterfaceTypeRequest`).
- Requests are evaluated on demand and cached.
- Requests can depend on other requests, forming a dependency graph.
- Cycle detection is built in — if a request cycle is detected, it's broken with an error.

```cpp
// Conceptual C++ interface
class Request {
    using Output = /* result type */;
    bool isCached() const;
    Output evaluate(Evaluator &evaluator) const;
};

// Example: get the interface type of a declaration
class InterfaceTypeRequest {
    using Output = Type;
    ValueDecl *decl;
    Type evaluate(Evaluator &evaluator) const;
};
```

### SIL — Swift Intermediate Language

SIL is a high-level, type-preserving intermediate representation unique to Swift:

- **Raw SIL:** Produced by SILGen, may contain diagnostics to be emitted.
- **Canonical SIL:** After mandatory passes, suitable for optimization.
- **Lowered SIL:** After address lowering (values → memory addresses).

SIL preserves Swift's type system (generics, protocols, value types, reference semantics) and is where Swift-specific analyses happen (definite initialization, exclusivity checking, ownership analysis).

```
sil @function_name : $@convention(thin) (Int) -> Int {
bb0(%0 : $Int):
  %1 = integer_literal $Builtin.Int64, 1
  %2 = struct_extract %0 : $Int, #Int._value
  %3 = builtin "sadd_with_overflow_Int64"(%2 : $Builtin.Int64, %1 : $Builtin.Int64) : ...
  return %result : $Int
}
```

### Types at Each Stage Boundary

```
Source text
  ↓ Parser
AST: Decl, Expr, Stmt, Pattern, TypeRepr
  ↓ Sema (request evaluator)
Type-checked AST + Type objects (StructType, FunctionType, GenericType, etc.)
  ↓ SILGen
Raw SIL: SILFunction, SILBasicBlock, SILInstruction, SILType
  ↓ Mandatory passes
Canonical SIL
  ↓ Optimizations
Optimized SIL
  ↓ IRGen
LLVM-IR (llvm::Module, llvm::Function, llvm::Value)
  ↓ LLVM
Machine code
```

### Public vs. Internal

- **Public (stable):** Only the CLI and the Swift Package Manager integration. `lib/Syntax` (SwiftSyntax) is a public library for parsing.
- **Semi-public:** SwiftSyntax (used by swift-format, swift-lint, macros). SourceKit (LSP-like interface for IDEs).
- **Internal:** AST, Sema, SIL, IRGen — all internal to the compiler.

`SwiftSyntax` is a separate, public library for Swift syntax trees:

```swift
import SwiftSyntax
import SwiftParser

let source = "let x = 42"
let tree = Parser.parse(source: source)
// tree: SourceFileSyntax
```

### Error Handling

**Collect model.** Swift's diagnostic engine:

- Produces `Diagnostic` objects with fix-its (suggested corrections).
- Resolution continues with error types/error expressions.
- Diagnostics have severity: error, warning, note, remark.
- The request evaluator handles cycles gracefully by reporting a diagnostic.

### Codebase Size

- Repository: `swiftlang/swift` on GitHub
- Compiler (`lib/`): approximately **500,000+ lines of C++**
- SIL definitions + passes: substantial portion of the codebase
- Standard library: ~100,000 lines of Swift

---

## CEL (Common Expression Language)

### Overview

CEL is a non-Turing-complete expression language designed for safety, speed, and portability. It evaluates linearly with respect to expression size (when macros are disabled). A CEL "program" is a single expression.

Source: [cel-spec](https://github.com/google/cel-spec), [cel-go](https://github.com/google/cel-go)

### Pipeline Stages

CEL has a clean three-stage pipeline:

1. **Parse** — Source text → AST (protocol buffer representation `google.api.expr.v1alpha1.Expr`).
2. **Check (optional)** — Type checking against a declared environment. Produces a checked expression with type annotations.
3. **Evaluate** — Execute the expression against runtime data.

The `Compile` method in cel-go combines Parse and Check:

```go
env, err := cel.NewEnv(
    cel.Variable("name", cel.StringType),
    cel.Variable("group", cel.StringType),
)

// Parse + type-check in one step
ast, issues := env.Compile(`name.startsWith("/groups/" + group)`)
if issues != nil && issues.Err() != nil {
    log.Fatalf("type-check error: %s", issues.Err())
}

// Create a thread-safe, cacheable program
prg, err := env.Program(ast)

// Evaluate
out, details, err := prg.Eval(map[string]interface{}{
    "name":  "/groups/acme.co/documents/secret-stuff",
    "group": "acme.co",
})
```

### The Env / Program / AST Model

Three key types:

- **`Env` (Environment):** Declares variables, functions, and macros available to CEL expressions. Created once, reused across many compilations.
- **`Ast`:** The parsed (and optionally type-checked) expression. Contains the expression tree and type/source info.
- **`Program`:** A compiled, ready-to-evaluate form of an AST bound to an environment. Stateless, thread-safe, cacheable. This is the "compile result."

```go
// Environment setup (done once)
env, _ := cel.NewEnv(
    cel.Variable("request", cel.ObjectType("google.rpc.context.AttributeContext.Request")),
)

// Compilation (done once per expression)  
ast, issues := env.Compile(expr)

// Program construction (done once per expression)
prg, _ := env.Program(ast)

// Evaluation (done many times with different inputs)
out, _, _ := prg.Eval(activation)
```

### Types at Each Stage Boundary

```
Source string
  ↓ Parse
cel.Ast (wraps google.api.expr.v1alpha1.ParsedExpr)
  Contains: Expr protobuf (tree of expression nodes)
  Each node: id, union of {Ident, Select, Call, CreateList, CreateStruct, Const, Comprehension}
  
  ↓ Check (optional)
cel.Ast (wraps google.api.expr.v1alpha1.CheckedExpr)
  Contains: Expr + type_map (id → Type) + reference_map (id → Reference)
  Type: union of {Primitive, WellKnown, ListType, MapType, MessageType, TypeParam, Dyn, Error, Null}

  ↓ Program construction
cel.Program (internal: interpretable + activation interface)

  ↓ Eval
ref.Val (CEL value: IntValue, StringValue, BoolValue, ListValue, MapValue, etc.)
```

### How Type Checking Works

CEL is **gradually typed** — type checking is optional but encouraged:

- Variables and functions must be declared in the `Env` with their types.
- The type checker attempts to resolve types for all sub-expressions.
- `dyn` type is used for dynamically typed values (union of all types).
- If all types are known statically, overload resolution happens at check time.
- If some types are `dyn`, resolution is deferred to runtime.

CEL's type system includes: `int`, `uint`, `double`, `bool`, `string`, `bytes`, `list(A)`, `map(K,V)`, `null_type`, message types, `type`, `dyn`.

### Error Handling

**Mixed model:**

- Parse/check errors: collected as `cel.Issues`, which is a list of errors with source positions. Error messages include source pointers:
  ```
  ERROR: <input>:1:40: undefined field 'undefined'
      | TestAllTypes{single_int32: 1, undefined: 2}
      | .......................................^
  ```
- Runtime errors: `no_matching_overload`, `no_such_field`. Generally terminate evaluation, EXCEPT for logical operators (`&&`, `||`) which use commutative short-circuiting — if one operand determines the result, runtime errors in the other operand are ignored.

### Partial Evaluation

CEL supports **partial evaluation** for distributed systems:

- If some variables are unknown at evaluation time, the expression can still be partially evaluated.
- `cel.EvalOptions(cel.OptTrackState)` enables tracking intermediate values.
- The `interpreter.Prune` function generates a residual expression containing only the unresolved parts.
- Example: if `group` is known but `name` is not, the residual might be `name.startsWith("/groups/acme.co")`.

### Compile Result

The caller gets:
1. `cel.Ast` — the parsed/checked expression (serializable as protobuf)
2. `cel.Issues` — any parse/check errors
3. `cel.Program` — the executable form (if no errors)

### Codebase Size

- `google/cel-go`: approximately **30,000–40,000 lines of Go**
- `google/cel-spec`: specification + conformance tests
- `google/cel-cpp`: C++ implementation
- `google/cel-java`: Java implementation (used in Android, Google Cloud)

Very compact for what it does — designed to be embeddable.

---

## Rego (Open Policy Agent)

### Overview

Rego is a declarative policy language for OPA (Open Policy Agent). It's used for authorization, admission control, and general policy enforcement.

Source: [open-policy-agent/opa](https://github.com/open-policy-agent/opa)

### Pipeline Stages

1. **Parsing** (`ast/parser.go`) — Source text → AST. Rego has a Prolog-inspired syntax with rules, queries, and comprehensions.
2. **Compilation** (`ast/compile.go`) — Multiple passes over the AST:
   - **Module resolution** — Resolve imports and package references.
   - **Rule indexing** — Build indexes for efficient rule lookup.
   - **Type checking** (`ast/check.go`) — Infer and check types (Rego is dynamically typed but has optional type checking).
   - **Safety checking** — Ensure all variables in rule heads are bound in rule bodies (a Datalog-like safety condition).
   - **Graph analysis** — Detect cycles, compute rule dependencies.
   - **Optimization** — Constant folding, partial evaluation.
3. **Evaluation** (`topdown/eval.go` or `rego/rego.go`) — Top-down evaluation of queries against data and policy.
4. **Partial Evaluation** (`rego/rego.go`) — Evaluate what's possible with known data, leaving unknowns as residual queries.
5. **WASM Compilation** (optional, `internal/planner/`, `internal/wasm/`) — Compile to WebAssembly for edge deployment.

### Types at Each Stage Boundary

```go
// AST types (ast package)
type Module struct {
    Package    *Package
    Imports    []*Import
    Rules      []*Rule
    Comments   []*Comment
    Annotations []*Annotations
}

type Rule struct {
    Head    *Head   // name + key + value + args
    Body    Body    // conjunction of expressions
    Else    *Rule   // else clause chain
}

type Expr struct {
    // Can be: Term, Every, Not, With
    Terms interface{}
    // ...
}

// Compiler output
type Compiler struct {
    Modules    map[string]*Module  // all compiled modules
    Rules      *RuleSet            // indexed rules
    TypeEnv    *TypeEnv            // type environment
    // many internal indexes
}

// Evaluation result
type ResultSet []Result
type Result struct {
    Expressions []*ExpressionValue
    Bindings    Bindings
}
```

### Partial Evaluation

Partial evaluation is a distinctive feature of Rego. It allows pre-computing parts of a policy:

```go
rego := rego.New(
    rego.Query("data.example.allow"),
    rego.Module("example.rego", module),
    rego.Unknowns([]string{"input"}),
)
pq, err := rego.Partial(ctx)
// pq.Queries: residual queries that must be true for allow
// pq.Support: additional rules needed by residual queries
```

This is used to push policy evaluation to edge caches, databases (as SQL WHERE clauses), or other systems.

### Error Handling

**Collect model.** The compiler collects errors:

```go
compiler := ast.NewCompiler()
compiler.Compile(modules)
if compiler.Failed() {
    for _, err := range compiler.Errors {
        // err has Location, Message, Code
    }
}
```

Evaluation errors are also collected rather than short-circuiting.

### Compile Result

```go
// The Compiler itself is the result
compiler := ast.NewCompiler()
compiler.Compile(modules)
// compiler.Modules — resolved modules
// compiler.Rules — indexed rules  
// compiler.Failed() — whether compilation succeeded
// compiler.Errors — list of errors
```

For the `rego` package (higher-level API):

```go
r := rego.New(
    rego.Query("data.example.allow"),
    rego.Module("example.rego", policy),
)
query, err := r.PrepareForEval(ctx)
// query: PreparedEvalQuery — cached, reusable
rs, err := query.Eval(ctx, rego.EvalInput(input))
```

### Codebase Size

- Repository: `open-policy-agent/opa` on GitHub
- Approximately **200,000–300,000 lines of Go**
- `ast/` package: ~30,000 lines (parser, compiler, type checker)
- `topdown/` package: ~20,000 lines (evaluation engine)

---

## Dhall

### Overview

Dhall is a programmable configuration language with a strong type system, guaranteed termination, and a standardized import system. It's designed to be a "total" language — all programs terminate.

Source: [dhall-lang/dhall-haskell](https://github.com/dhall-lang/dhall-haskell), [dhall-lang.org](https://dhall-lang.org)

### Pipeline Stages

1. **Parsing** — Source text → concrete syntax tree (CST) / AST (`Dhall.Syntax.Expr`).
2. **Import Resolution** — Resolve imports (URLs, file paths, environment variables). Imports are content-addressed (SHA-256 integrity checks).
3. **Type Checking** — Bidirectional type checking with dependent types. Dhall's type system is very expressive: dependent function types, union types, record types, Natural number type with built-in folds.
4. **Normalization (β-normalization)** — Reduce the expression to normal form. Since Dhall is total, normalization always terminates.
5. **Alpha-normalization** (optional) — Rename all bound variables to canonical names for semantic comparison.

### Types at Each Stage Boundary

```haskell
-- The core expression type (simplified)
data Expr s a
    = Const Const          -- Type, Kind, Sort
    | Var Var              -- variable reference  
    | Lam Text (Expr s a) (Expr s a)     -- λ(x : A) → b
    | Pi  Text (Expr s a) (Expr s a)     -- ∀(x : A) → B
    | App (Expr s a) (Expr s a)          -- function application
    | Let (Binding s a) (Expr s a)       -- let binding
    | Annot (Expr s a) (Expr s a)        -- x : T
    | Bool | Natural | Text | List | ...  -- built-in types
    | BoolLit Bool | NaturalLit Natural | TextLit ... -- literals
    | Record (Map Text (RecordField s a))
    | Union (Map Text (Maybe (Expr s a)))
    | Merge (Expr s a) (Expr s a) (Maybe (Expr s a))
    | ...
    | Note s (Expr s a)    -- source annotation
    | Embed a              -- embedded import

-- After import resolution, 'a' becomes 'Void' (no remaining imports)
-- After type checking, the expression has a verified type
-- After normalization, the expression is in β-normal form
```

The `s` parameter carries source span information; `a` carries the import type. After import resolution, `a = Void`.

### Import Resolution

Dhall's import system is distinctive:

- Imports can be: local files, URLs, environment variables, or the `missing` keyword.
- Each import can have an integrity check: `https://example.com/config.dhall sha256:abc123...`
- Import resolution is cached based on content hash.
- Imports form a DAG — circular imports are rejected.
- Remote imports can be "frozen" with their hash for reproducibility.

### Error Handling

**Short-circuit per phase.** Each phase can fail:

- Parse errors halt parsing.
- Import resolution errors halt (missing import, integrity check failure, import cycle).
- Type errors halt type checking — Dhall reports the first type error with context.
- Normalization cannot fail (totality guarantee).

However, Dhall's error messages are notably detailed, showing the full context of type mismatches.

### Compile Result

Dhall doesn't "compile" in the traditional sense. The result is a **normalized, type-checked expression**:

```haskell
-- Type-check and normalize
input :: Text -> IO (Expr Src Void)
input src = do
    expr     <- throws (exprFromText "(input)" src)
    resolved <- loadRelativeTo "." UseSemanticCache expr
    typed    <- throws (typeOf resolved)
    return (normalize resolved)
```

The output can be:
- A Dhall value (printed as Dhall text)
- Converted to JSON/YAML
- Converted to a language-specific type (via code generation)
- Encoded as CBOR binary

### Codebase Size

- `dhall-lang/dhall-haskell`: approximately **50,000–60,000 lines of Haskell** (core + JSON/YAML/Bash/Nix integrations)
- Standard: the Dhall standard is formally specified at `dhall-lang/dhall-lang`
- Multiple implementations: Haskell, Rust, Go, Clojure

---

## Jsonnet

### Overview

Jsonnet is a data templating language — a superset of JSON with variables, conditionals, functions, imports, and more. It evaluates to JSON.

Source: [google/jsonnet](https://github.com/google/jsonnet), [google/go-jsonnet](https://github.com/google/go-jsonnet)

### Pipeline Stages

1. **Lexing** — Source text → tokens.
2. **Parsing** — Tokens → AST.
3. **Desugaring** — AST → desugared AST. Syntactic sugar is expanded:
   - Object comprehensions → `std.foldl` + concatenation
   - Array comprehensions → `std.flatMap`
   - String formatting → `std.format`
   - `assert` → conditional error
   - `import`/`importstr`/`importbin` → import nodes
4. **Static Analysis** — Variable binding, unused variable detection.
5. **Evaluation** — Lazy evaluation of the desugared AST.
6. **Manifestation** — Convert the evaluated value to JSON (or other formats: YAML, INI, etc.).

### Lazy Evaluation

Jsonnet uses **lazy evaluation** — expressions are only evaluated when their value is needed:

- Object fields are thunks (deferred computations).
- Self-references and super work because of lazy evaluation.
- This enables natural inheritance patterns: `base { field: newValue }`.
- Errors are only raised when an erroring path is actually demanded.

### Types at Each Stage Boundary

```
Source text
  ↓ Lexer
Tokens
  ↓ Parser
AST nodes: Local, Object, Array, Binary, Apply, Function, etc.
  ↓ Desugaring
Simplified AST (fewer node types, sugar expanded)
  ↓ Evaluation
Values: null, bool, number, string, array, object, function
  ↓ Manifestation
JSON text (or YAML, etc.)
```

In the Go implementation (`go-jsonnet`):

```go
// AST nodes
type Node interface { /* ... */ }
type Object struct {
    Fields []ObjectField
    // ...
}
type Apply struct {
    Target Node
    Arguments Arguments
    // ...
}

// Runtime values (internal)
type value interface{}
type valueString struct { /* ... */ }
type valueObject struct { /* ... */ }
type valueArray struct { /* ... */ }
type valueFunction struct { /* ... */ }
```

### Error Handling

**Short-circuit (on demand).** Because of lazy evaluation:

- Parse errors are reported immediately.
- Runtime errors (type mismatch, assertion failure, missing field) only occur when a value is demanded.
- Stack traces show the evaluation path to the error.
- The `error` keyword produces a user-defined runtime error.

### Compile Result

Jsonnet doesn't have a separate "compile" step. The API is:

```go
vm := jsonnet.MakeVM()
output, err := vm.EvaluateFile("config.jsonnet")
// output: string (JSON)
// err: error (with source location + stack trace)
```

In the C++ implementation:

```cpp
std::string output;
bool success = jsonnet_evaluate_file(vm, filename, &output, &err_msg);
```

### Codebase Size

- `google/jsonnet` (C++): approximately **15,000–20,000 lines**
- `google/go-jsonnet` (Go): approximately **15,000–20,000 lines**
- Compact — intentionally simple language

---

## CUE

### Overview

CUE is a constraint-based configuration language derived from logic programming (specifically, typed feature structures). Values and types are unified — there's no distinction between a type constraint and a value.

Source: [cue-lang/cue](https://github.com/cue-lang/cue), [CUE spec](https://cuelang.org/docs/reference/spec/)

### Pipeline Stages

1. **Lexing/Parsing** (`cue/scanner`, `cue/parser`) — Source text → AST.
2. **Compilation** — AST → internal representation. Resolves references within a file.
3. **Unification** — The core operation. Combines (unifies) values according to the lattice:
   - Top (`_`) is the most general value.
   - Bottom (`_|_`) is the least (error/contradiction).
   - Unification always produces a unique result (greatest lower bound in the lattice).
4. **Evaluation** — Reduces expressions to their normal form. Lazy: incomplete expressions remain as constraints until more information arrives.
5. **Export/Manifestation** — Convert to JSON, YAML, or other formats.

### The Unification Model

CUE's core is **lattice-based unification**:

```
// Unification: & operator
{a: int} & {a: 1}          → {a: 1}
{a: >=1 & <=7} & {a: >=5}  → {a: >=5 & <=7}
{a: 1} & {a: 2}            → _|_ (error: conflicting values)

// Disjunction: | operator
"tcp" | "udp"               → "tcp" | "udp" (choice)
*"tcp" | "udp"              → "tcp" (default marked with *)
```

Properties:
- Unification is **commutative, associative, and idempotent** → order of evaluation doesn't matter.
- This is fundamental to CUE's design: configurations can be split across files and combined in any order.
- Types and values are the same thing — `string` is just a constraint that unifies with any string.

### Types at Each Stage Boundary

```go
// Public AST types (cue/ast package)
type File struct {
    Decls []Decl  // top-level declarations
}
type Field struct {
    Label Label
    Value Expr
}
// Expr: Ident, BasicLit, BinaryExpr, UnaryExpr, SelectorExpr, IndexExpr,
//       ListLit, StructLit, Comprehension, etc.

// Internal value representation (cue/types package - public API)
type Value struct {
    // Represents a CUE value in the lattice
}
func (v Value) Unify(w Value) Value
func (v Value) Kind() Kind  // null, bool, int, float, string, bytes, struct, list
func (v Value) MarshalJSON() ([]byte, error)
func (v Value) Validate(opts ...Option) error
```

### Public vs. Internal

- **Public:** `cue` package (top-level API), `cue/ast`, `cue/parser`, `cue/token`, `cue/format` — these are stable.
- **Internal:** The evaluation engine (`internal/core/adt/`), the unifier, constraint propagation — these are under `internal/`.

```go
// Public API usage
var ctx *cue.Context = cuecontext.New()
val := ctx.CompileString(`{a: int, b: "hello"}`)
err := val.Validate()
json, _ := val.MarshalJSON()
```

### Error Handling

**Collect model (via bottom values).** CUE doesn't use exceptions:

- Errors are represented as bottom (`_|_`) values in the lattice.
- A conflicting unification produces bottom, not an exception.
- Bottom propagates: `{a: _|_}.a` is `_|_`.
- `Value.Validate()` collects all bottom values and returns them as errors.
- Implementations may associate error messages with different bottom instances.

### Compile Result

```go
ctx := cuecontext.New()
v := ctx.CompileString(source)
// v is a cue.Value
// v.Err() — first error, if any
// v.Validate() — comprehensive validation
// v.MarshalJSON() — export to JSON
// v.LookupPath(cue.ParsePath("a.b")) — navigate the value
```

### Codebase Size

- Repository: `cue-lang/cue` on GitHub
- Approximately **200,000+ lines of Go**
- Core evaluation engine (`internal/core/`): complex, lattice-based

---

## Starlark

### Overview

Starlark (formerly Skylark) is a Python dialect designed for build system configuration. Used by Bazel, Buck, and other build tools. Deterministic, hermetic, no I/O, no global state mutation.

Source: [google/starlark-go](https://github.com/google/starlark-go), [bazelbuild/starlark](https://github.com/bazelbuild/starlark)

### Pipeline Stages

1. **Scanning** (`syntax/scan.go`) — Source text → tokens.
2. **Parsing** (`syntax/parse.go`) — Tokens → AST (`syntax.File`).
3. **Name Resolution** (`resolve/resolve.go`) — Resolve variable references: local, free, global, predeclared, universal. Detect errors (e.g., use of undefined names).
4. **Compilation to Bytecode** (`starlark/compile.go` in Go implementation) — AST → bytecode for a stack-based VM.
5. **Execution** (`starlark/eval.go`) — Bytecode interpretation.

### Types at Each Stage Boundary

```go
// AST (syntax package)
type File struct {
    Stmts []Stmt
}
type Stmt interface { stmt() }  // DefStmt, IfStmt, ForStmt, ReturnStmt, etc.
type Expr interface { expr() }  // Ident, Literal, BinaryExpr, CallExpr, etc.

// After name resolution, identifiers are annotated with binding info:
type Ident struct {
    Name    string
    Binding interface{}  // *resolve.Binding after resolution
}

// Compiled form (internal)
type Program struct {
    Loads     []Binding    // imports
    Names     []string     // global names
    Constants []interface{} // constant pool
    Functions []*Funcode   // compiled function bytecodes
    Globals   []Binding
    Toplevel  *Funcode     // top-level code
}

// Runtime values
type Value interface {
    String() string
    Type() string
    Freeze()
    Truth() Bool
    Hash() (uint32, error)
}
// Concrete: String, Int, Float, Bool, *List, *Dict, *Function, Tuple, *Set, NoneType
```

### Public vs. Internal

- **Public:** `starlark.ExecFile()`, `starlark.Eval()`, `syntax.Parse()`, `resolve.File()`, `starlark.Value` interface, `starlark.StringDict` (global bindings).
- **Internal:** Bytecode format, VM internals.

```go
// Public API
thread := &starlark.Thread{Name: "main"}
globals, err := starlark.ExecFile(thread, "config.star", nil, predeclared)
// globals: starlark.StringDict — all top-level bindings
```

### Error Handling

**Collect at parse time, short-circuit at runtime.**

- Parse errors are collected and returned.
- Name resolution errors are collected and returned.
- Runtime errors are exceptions that unwind the call stack with a stack trace.

### Compile Result

```go
// Parse + resolve
f, err := syntax.Parse(filename, src, 0)
if err != nil { /* syntax errors */ }
if err := resolve.File(f, isPredeclared, isUniversal); err != nil { /* resolve errors */ }

// Execute
globals, err := starlark.ExecFile(thread, filename, src, predeclared)
// globals: map of name → Value
```

### Codebase Size

- `google/starlark-go`: approximately **15,000–20,000 lines of Go**
- `bazelbuild/starlark` (Java implementation): approximately **30,000 lines of Java**
- Intentionally small — designed for embedding

---

## Pkl

### Overview

Pkl (pronounced "pickle") is Apple's configuration-as-code language. It features a rich module system, structural typing, type checking, and IDE support.

Source: [apple/pkl](https://github.com/apple/pkl)

### Pipeline Stages

Pkl is implemented in Kotlin/JVM:

1. **Lexing** — Source text → tokens.
2. **Parsing** — Tokens → AST. Pkl uses an ANTLR-generated parser.
3. **Module Resolution** — Resolve `import` and `amend` references. Modules can come from files, packages, or URIs.
4. **Type Checking** — Structural type checking. Pkl has a rich type system: classes, type aliases, union types, nullable types, function types, constrained types.
5. **Evaluation** — Lazy evaluation of module properties. Properties are evaluated on demand.
6. **Rendering** — Output as JSON, YAML, plist, Java properties, or Pkl text.

### Module System

Pkl's module system is central to its architecture:

```pkl
// Module declaration
module myapp.Config

import "package://pkg.pkl-lang.org/pkl-pantry/pkl.toml@1.0.0#/toml.pkl"

class DatabaseConfig {
    host: String
    port: UInt16
    name: String
}

database: DatabaseConfig = new {
    host = "localhost"
    port = 5432
    name = "mydb"
}
```

- Modules are the unit of compilation.
- `amend` allows modifying a module (like inheritance).
- Packages provide versioned distribution.

### Types at Each Stage Boundary

```
Source text
  ↓ ANTLR Parser
Parse tree (ANTLR-generated context objects)
  ↓ AST construction
Internal AST (PklNode hierarchy)
  ↓ Type checking
Type-annotated AST (VmType system)
  ↓ Evaluation
VmObject, VmList, VmMap, VmNull, primitive values
  ↓ Rendering
JSON/YAML/plist/properties text
```

### Error Handling

**Collect model.** Pkl produces rich error messages with source locations:

```
– Error: Expected value of type `UInt16`, but got value of type `Int`
  at myconfig.pkl:5:10
```

Type errors, constraint violations, and runtime errors are all reported with locations and context.

### Codebase Size

- `apple/pkl`: approximately **100,000+ lines** (Kotlin + Gradle build)
- Includes CLI, language server, code generators for Swift/Go/Java/Kotlin

---

## Dafny

### Overview

Dafny is a verification-aware programming language. Programs include specifications (pre/postconditions, invariants, assertions) that are verified by an automated theorem prover. Dafny compiles to C#, Java, JavaScript, Go, and Python.

Source: [dafny-lang/dafny](https://github.com/dafny-lang/dafny)

### Pipeline Stages

1. **Parsing** — Source text → Dafny AST (`Program`, `ModuleDecl`, `ClassDecl`, `Method`, `Function`, etc.).
2. **Name Resolution + Type Checking** — Resolve names, check types, resolve generics.
3. **Ghost Erasure** — Identify ghost (specification-only) code vs. compiled code.
4. **Translation to Boogie** — Dafny → Boogie intermediate verification language. This is where proof obligations (verification conditions) are generated:
   - Method preconditions become `requires` clauses.
   - Postconditions become assertions at method exit.
   - Loop invariants become assertions at loop head.
   - Assertions become verification conditions.
5. **Verification (via Boogie → Z3)** — Boogie generates verification conditions (VCs) and passes them to the Z3 SMT solver.
6. **Target Language Compilation** — Dafny AST → target language (C#, Java, Go, JS, Python). This is a separate path from verification.

```
Source → Parse → Resolve → Type Check → Boogie Translation → Z3 Verification
                                      ↘ Target Compilation → C#/Java/Go/JS/Python
```

### How Proof Obligations Flow

The critical stage is Dafny → Boogie translation:

```dafny
method Max(a: int, b: int) returns (c: int)
    ensures c >= a && c >= b
    ensures c == a || c == b
{
    if a >= b { c := a; } else { c := b; }
}
```

This generates Boogie code with explicit verification conditions:

```boogie
procedure Max(a: int, b: int) returns (c: int)
    ensures c >= a && c >= b;
    ensures c == a || c == b;
{
    if (a >= b) {
        c := a;
    } else {
        c := b;
    }
    // VC: prove postconditions hold at return
}
```

Boogie then generates VCs for Z3:

- Each assertion/postcondition becomes a logical formula.
- Z3 attempts to prove the formula is valid (or find a counterexample).
- If Z3 can prove it → verification succeeds.
- If Z3 finds a counterexample → verification error with source location.
- If Z3 times out → inconclusive.

### Types at Each Stage Boundary

```
Source text
  ↓ Parser
Dafny AST: Program, ModuleDecl, ClassDecl, Method, Function, 
           Statement, Expression, Type
  ↓ Resolver + Type Checker
Resolved AST (names bound to declarations, types resolved)
  ↓ Boogie Translation
Boogie AST: Declaration, Procedure, Implementation, Variable, 
            Cmd (Assign, Assert, Assume, Havoc, Call), Expr
  ↓ VC Generation
Verification Conditions: logical formulas (SMT-LIB format)
  ↓ Z3
Sat/Unsat/Timeout
```

### Error Handling

**Mixed model:**

- Parse/type errors: collected, short-circuit compilation.
- Verification errors: collected per method/function. Each method is verified independently. A verification failure in one method doesn't prevent checking others.
- Verification errors include: assertion violation, postcondition violation, loop invariant violation, termination failure.
- Timeout handling: if Z3 times out, the method is reported as "not verified" with a timeout message.

### Compile Result

From the CLI:

```bash
dafny verify program.dfy         # Verify only
dafny build -t:cs program.dfy    # Verify + compile to C#
dafny run program.dfy            # Verify + compile + run
```

Programmatically, the `DafnyDriver` class orchestrates the pipeline.

### Codebase Size

- Repository: `dafny-lang/dafny` on GitHub
- Approximately **300,000+ lines of C#** (compiler + verifier integration)
- Plus Boogie dependency
- Test suite: extensive, including verification regression tests

---

## Boogie

### Overview

Boogie is an intermediate verification language — it's not meant for humans to write directly (though they can). It receives verification conditions from higher-level languages (Dafny, VCC, HAVOC, Corral) and passes them to SMT solvers.

Source: [boogie-org/boogie](https://github.com/boogie-org/boogie)

### Pipeline Stages

1. **Parsing** — Boogie source text → AST (`Program`, `Declaration`, `Procedure`, `Implementation`).
2. **Name Resolution / Type Checking** — Resolve names, check types in the simple Boogie type system.
3. **Monomorphization** (optional) — Expand polymorphic procedures.
4. **VC Generation** — Convert imperative Boogie code into logical formulas:
   - **Passification** — Convert mutable variables to SSA-like form.
   - **Weakest precondition** (or strongest postcondition) calculation.
   - **VC encoding** — Produce a single logical formula per procedure.
5. **SMT Solving** — Send VCs to Z3 (or other SMT solver). Check satisfiability of the negation (if unsatisfiable → verification succeeds).

### How It Receives and Processes VCs

Boogie's input language is simple by design:

```boogie
type Ref;
var Heap: [Ref, Field]int;

procedure Increment(this: Ref)
    modifies Heap;
    requires Heap[this, value] >= 0;
    ensures Heap[this, value] == old(Heap[this, value]) + 1;
{
    Heap[this, value] := Heap[this, value] + 1;
}
```

Key constructs for verification:
- `requires` / `ensures` — pre/postconditions
- `assert` — inline verification condition
- `assume` — add an assumption (trusted, not verified)
- `havoc` — make a variable unknown (model non-determinism)
- `invariant` — loop invariant
- `modifies` — frame condition (what can change)

VC generation:
1. Each `assert` becomes a proof obligation.
2. Each `ensures` becomes a proof obligation at procedure exit.
3. Each `requires` at a call site becomes a proof obligation.
4. `assume` adds to the path condition without proof.
5. Loops are handled via invariants (not unrolled).

### Types at Each Stage Boundary

```
Boogie source (or generated by Dafny/other tools)
  ↓ Parser
Boogie AST: Program containing:
  - TypeCtorDecl, TypeSynonymDecl (type declarations)
  - GlobalVariable, Constant (global state)
  - Axiom (logical axioms)
  - Function (uninterpreted/interpreted functions)
  - Procedure + Implementation (imperative code with specs)
  
  ↓ Resolution + Type Checking
Resolved AST (names bound, types checked)
  
  ↓ VC Generation
VCExpr: logical formula trees
  - VCExprLiteral, VCExprVar, VCExprNAry (operators)
  - Quantifiers: VCExprQuantifier (forall, exists)
  
  ↓ SMT Encoding
SMT-LIB text (sent to Z3)
  
  ↓ Z3
ProverOutcome: Valid | Invalid (with counterexample) | Timeout | OutOfMemory
```

### Error Handling

**Per-procedure model:**

- Each procedure is verified independently.
- Verification failures include the failing assertion/postcondition with source location.
- Counterexample models (from Z3) are propagated back to help diagnosis.
- Timeouts are handled per procedure.

### Codebase Size

- Repository: `boogie-org/boogie` on GitHub
- Approximately **80,000–100,000 lines of C#**
- Focused: it does one thing (verification) well

---

## Summary Tables

### Pipeline Stage Count

| System | Explicit stages | Key characteristic |
|---|---|---|
| Roslyn | 4 (Parse → Declare → Bind → Emit) | Each stage has public API |
| TypeScript | 5+ (Scan → Parse → Bind → Check → Transform → Emit) | Lazy type checking |
| Rust (rustc) | 10+ (Lex → Parse → Expand → HIR → THIR → MIR → LLVM) | Query system, demand-driven |
| Go (gc) | 7 (Lex → Parse → TypeCheck → IR → SSA → Optimize → Codegen) | Package-at-a-time |
| Kotlin K2 | 6 (Parse → Raw FIR → Multi-phase resolution → IR → Codegen) | Phased FIR resolution |
| Swift | 7 (Parse → Sema → SILGen → Mandatory → Optimize → IRGen → LLVM) | Request evaluator, SIL |
| CEL | 3 (Parse → Check → Eval) | Expression-only, gradual typing |
| Rego | 3+ (Parse → Compile → Eval + Partial Eval) | Partial evaluation |
| Dhall | 5 (Parse → Import → TypeCheck → Normalize → AlphaNorm) | Total, content-addressed imports |
| Jsonnet | 5 (Lex → Parse → Desugar → Eval → Manifest) | Lazy evaluation |
| CUE | 4 (Parse → Compile → Unify → Export) | Lattice-based unification |
| Starlark | 5 (Scan → Parse → Resolve → Compile → Execute) | Bytecode VM |
| Pkl | 6 (Lex → Parse → ModuleResolve → TypeCheck → Eval → Render) | Module-centric, ANTLR |
| Dafny | 6 (Parse → Resolve → Translate → Verify → Compile) | Verification via Boogie/Z3 |
| Boogie | 5 (Parse → Resolve → VCGen → SMT → Result) | Intermediate verification language |

### Error Strategy

| System | Strategy | Details |
|---|---|---|
| Roslyn | Always collect | Full AST on error, missing/skipped tokens |
| TypeScript | Always collect | Error flags on nodes, error types |
| rustc | Collect with recovery | Error types (`TyKind::Error`), `ErrorGuaranteed` |
| Go | Collect | Error callback, continues checking |
| Kotlin K2 | Collect | Error type refs, phases continue |
| Swift | Collect | Fix-its, request evaluator handles cycles |
| CEL | Collect (parse/check), mixed (eval) | Commutative short-circuit in `&&`/`||` |
| Rego | Collect | Error list on compiler |
| Dhall | Short-circuit per phase | First error halts current phase |
| Jsonnet | Short-circuit (runtime) | Lazy: errors only when demanded |
| CUE | Collect (via bottom) | Errors are lattice bottom values |
| Starlark | Collect (parse), short-circuit (runtime) | Stack-unwinding exceptions |
| Pkl | Collect | Rich error messages with locations |
| Dafny | Mixed | Parse/type: short-circuit; verification: per-method |
| Boogie | Per-procedure | Each procedure independent |

### Approximate Codebase Size

| System | Language | LOC (compiler) | Notes |
|---|---|---|---|
| Roslyn | C# | ~4–5M | Includes IDE features |
| TypeScript | TypeScript | ~300–400K | checker.ts alone ~50K |
| rustc | Rust | ~600–800K | ~30+ crates |
| Go (gc) | Go | ~200–250K | Lean |
| Kotlin K2 | Kotlin | ~200K+ (FIR) / ~1M+ (total) | Multi-year rewrite |
| Swift | C++ | ~500K+ | Plus SwiftSyntax |
| CEL (Go) | Go | ~30–40K | Minimal |
| Rego | Go | ~200–300K | Plus WASM compiler |
| Dhall | Haskell | ~50–60K | Plus standard |
| Jsonnet | Go/C++ | ~15–20K each | Intentionally small |
| CUE | Go | ~200K+ | Complex evaluator |
| Starlark | Go | ~15–20K | Embeddable |
| Pkl | Kotlin | ~100K+ | Plus tooling |
| Dafny | C# | ~300K+ | Plus Boogie dep |
| Boogie | C# | ~80–100K | Focused |
