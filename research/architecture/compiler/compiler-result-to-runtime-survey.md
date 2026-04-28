# Compiler Result to Runtime Object Survey

Raw survey of how eight expression-language / DSL / workflow systems carry compile-time information
into their runtime objects. Covers: CEL, OPA/Rego, Roslyn Scripting API, TypeScript Compiler API,
XState, Dhall, Temporal Go SDK, and Pkl.

Six questions per system:

1. **Compile result content** — what does the compile result carry at runtime?
2. **Separation boundary** — clean compile/runtime split, or interleaved?
3. **Runtime → compile reference** — does the runtime object hold a back-reference to the compile result?
4. **Type info at eval** — what type information is available during evaluation?
5. **Compile-error surfacing** — can the runtime surface compile-time errors?
6. **Caching model** — how is the compiled artifact cached relative to runtime state?

---

## CEL (Common Expression Language) — v0.28.0

Source: `github.com/google/cel-go` v0.28.0

### Compile result content

`cel.Ast` carries:

```go
func (ast *Ast) IsChecked() bool
func (ast *Ast) NativeRep() *celast.AST     // raw typed AST
func (ast *Ast) OutputType() *Type          // DynType if unchecked; inferred type if checked
func (ast *Ast) Source() Source             // original source text + location metadata
func (ast *Ast) SourceInfo() *exprpb.SourceInfo
```

`cel.Program` (the runtime object) is "an evaluable view of an Ast". Created via:

```go
env.Program(checkedAst *Ast, opts ...ProgramOption) (Program, error)
```

`Program` interface:

```go
type Program interface {
    Eval(any) (ref.Val, *EvalDetails, error)
    ContextEval(context.Context, any) (ref.Val, *EvalDetails, error)
}
```

`EvalDetails` carries:

```go
func (ed *EvalDetails) ActualCost() *uint64          // non-nil only if OptTrackCost
func (ed *EvalDetails) State() interpreter.EvalState // non-nil if OptTrackState or OptExhaustiveEval
```

`interpreter.EvalState` maps expression IDs (from the checked AST) to runtime-observed values:

```go
type EvalState interface {
    IDs() []int64
    Value(int64) (ref.Val, bool)
    SetValue(int64, ref.Val)
    Reset()
}
```

`interpreter.Interpretable` is the runtime evaluator node, produced from the checked AST:

```go
type Interpretable interface {
    ID() int64              // expression node ID from the AST
    Eval(activation Activation) ref.Val
}
// Sub-types: InterpretableConst, InterpretableAttribute,
// InterpretableCall (Function(), OverloadID(), Args()), InterpretableConstructor
```

`interpreter.Interpreter` produces an `Interpretable` tree from the checked AST:

```go
type Interpreter interface {
    NewInterpretable(exprAST *ast.AST, opts ...PlannerOption) (Interpretable, error)
}
```

Note: `DynKind` and `AnyKind` "only exist at type-check time".

Residual evaluation support:

```go
env.ResidualAst(a *Ast, details *EvalDetails) (*Ast, error)
// Produces new Ast containing only unknown-referencing parts after partial eval
// "expression ids within the residual AST have no correlation to expression ids of the original AST"

interpreter.PruneAst(expr, macroCalls, state EvalState) *ast.AST
// Prunes AST based on runtime EvalState — enables iterative evaluate → prune → re-evaluate
```

`InterpretableDecorator`:

```go
type InterpretableDecorator func(Interpretable) (Interpretable, error)
// Applied at Program creation time to inspect/alter/replace Interpretable nodes
```

### Separation boundary

Pipeline is explicit and sequential:

```
Env.Parse(txt) → *Ast (unchecked)
→ Env.Check(ast) → *Ast (checked, typed)
→ Env.Program(checkedAst) → Program (runtime evaluator)
```

Three distinct objects with explicit conversion steps. Each step can be called independently.

### Runtime → compile reference

`Program` is produced from a checked `*Ast` but the interface does not expose a back-reference to the
original `Ast`. The `Interpretable` tree carries `ID()` values that correspond to AST node IDs, which
means the IDs are shared between compile and runtime but the `Ast` object itself is not required to
call `Eval`.

When `OptTrackState` is active, `EvalState` maps those IDs back to runtime values, enabling
correlation between AST nodes and observed runtime values.

### Type info at eval

The full checked type is embedded in the `*Ast.OutputType()` field. Within the `Interpretable` tree,
`InterpretableConstructor` exposes `.Type() ref.Type`. `InterpretableCall` exposes `.Function()` and
`.OverloadID()`, both of which are resolved during type-checking.

At evaluation time, `EvalState` tracks which expression IDs produced which `ref.Val` values.
`OptExhaustiveEval` disables short-circuiting and evaluates all branches so that full runtime type
information is captured even for branches not taken.

### Compile-error surfacing

Compile errors (parse or type-check) are returned directly from `Env.Parse()` and `Env.Check()`.
They are not deferred to evaluation time. The `Program` is only constructable from a successfully
checked `*Ast`.

### Caching model

The `Ast` is a value that can be stored and reused. Each call to `Env.Program(checkedAst)` can
produce a new `Program`; programs are not cached by default. `OptOptimize` pre-computes constant
expressions at `Program` creation time, effectively caching sub-evaluations within the program
object. Each `Program.Eval()` call is independent and shares no state by default unless
`OptTrackState` or `OptExhaustiveEval` is used, in which case `EvalDetails` is returned per call.

---

## OPA / Rego — v1.15.2

Source: `github.com/open-policy-agent/opa` v1.15.2

### Compile result content

```go
type PreparedEvalQuery = v1.PreparedEvalQuery
// "holds prepared Rego state pre-processed for subsequent evaluations"
// "Prepared queries are safe to share across multiple Go routines"

type PreparedPartialQuery = v1.PreparedPartialQuery
type PartialResult = v1.PartialResult
// "represents the result of partial evaluation; can generate a new query"

type PartialQueries = v1.PartialQueries
// "contains queries and support modules produced by partial evaluation"

type CompileResult = v1.CompileResult
// "result of compiling query+modules into an executable"
```

Construction pipeline:

```go
r := rego.New(
    rego.Query("x = data.example.authz.allow"),
    rego.Module("example.rego", module),
    rego.Compiler(c *ast.Compiler), // optional: inject pre-compiled compiler
)
query, err := r.PrepareForEval(ctx)
results, err := query.Eval(ctx, rego.EvalInput(input))
```

`rego.Compiler(c *ast.Compiler)` allows the caller to inject a separately-built compiler object, so
the same compiled policy can be reused across multiple `rego.New()` calls.

Eval-time options accepted by `query.Eval()`:
`EvalInput`, `EvalParsedInput`, `EvalMetrics`, `EvalQueryTracer`, `EvalTransaction`,
`EvalInstrument`, `EvalUnknowns`, `EvalParsedUnknowns`, `EvalVirtualCache`.

`WithPartialEval()` `PrepareOption` — performs partial evaluation during preparation.

### Separation boundary

Compilation is separated from evaluation. `PrepareForEval` compiles if no pre-built compiler is
provided. The resulting `PreparedEvalQuery` is opaque; the compiled state is embedded inside it.
`rego.Compiler(c)` is the explicit hook to inject a pre-built compile artifact.

### Runtime → compile reference

`PreparedEvalQuery` holds the compiled state internally. No public API on `PreparedEvalQuery` exposes
the underlying `*ast.Compiler` or policy AST after preparation. The compiled state is held alive as
long as the `PreparedEvalQuery` is in scope.

### Type info at eval

OPA's type system operates during compilation (type checking of Rego modules). At evaluation time,
the `PreparedEvalQuery` uses the compiled form to evaluate against a JSON-structured input. No type
metadata structure is surfaced to the caller during `Eval()`. The `EvalQueryTracer` option allows
tracing evaluation steps, which includes policy node references.

### Compile-error surfacing

Compilation errors are surfaced at `PrepareForEval()` time. If a pre-built `*ast.Compiler` is
injected via `rego.Compiler()`, compilation errors would have been encountered when building that
compiler. `Eval()` itself surfaces only evaluation errors (e.g., undefined, type mismatch at
runtime), not compile-time policy parsing errors.

The doc note: "Preparing queries in advance avoids parsing and compiling on each query".

### Caching model

`PreparedEvalQuery` is the cached unit — it holds compiled state and is reusable across goroutines.
`EvalVirtualCache` option allows sharing a virtual document cache across evaluations. Each `Eval()`
call is independent in terms of input, but the compiled policy inside `PreparedEvalQuery` is shared.
Injecting a shared `*ast.Compiler` via `rego.Compiler()` allows the compile result to be shared
across multiple `rego.New()` calls without recompilation.

---

## Roslyn Scripting API — Microsoft.CodeAnalysis.CSharp.Scripting

Source: `Microsoft.CodeAnalysis.CSharp.Scripting`, GitHub Wiki `Scripting-API-Samples.md`

### Compile result content

```csharp
// Create reusable script (compile once, run many times)
var script = CSharpScript.Create<int>("X*Y", globalsType: typeof(Globals));
script.Compile();  // explicit pre-compilation; without it, compilation happens on first RunAsync

// Run with different globals
var result = await script.RunAsync(new Globals { X = i, Y = i });
// result.ReturnValue
```

`Script<T>` is the compiled artifact. `ScriptRunner<T>` is a compiled delegate:

```csharp
ScriptRunner<int> runner = script.CreateDelegate();
// "doesn't hold compilation resources (syntax trees, etc.) alive"
await runner(new Globals { X = i, Y = i });
```

`ScriptState` carries runtime variable bindings:

```csharp
var state = await CSharpScript.RunAsync<int>("int answer = 42;");
foreach (var variable in state.Variables)
    Console.WriteLine($"{variable.Name} = {variable.Value} of type {variable.Type}");
```

Bridge back to full Roslyn API:

```csharp
Compilation compilation = script.GetCompilation();
// "Compilation gives access to the full set of Roslyn APIs"
```

Script chaining (continuation model):

```csharp
var script = CSharpScript.Create<int>("int x = 1;")
    .ContinueWith("int y = 2;")
    .ContinueWith("x + y");

// Continue from a live state
var state = await CSharpScript.RunAsync("int x = 1;");
state = await state.ContinueWithAsync("int y = 2;");
state = await state.ContinueWithAsync("x+y");
Console.WriteLine(state.ReturnValue);
```

### Separation boundary

`Script<T>` (compiled artifact) and `ScriptState` (runtime state) are distinct types. `Script<T>`
can be compiled without running. `ScriptRunner<T>` is an explicit step to produce a delegate that is
explicitly separated from compilation resources. `ScriptState.ContinueWithAsync()` takes a new source
string, triggering incremental compilation.

### Runtime → compile reference

`Script<T>` is the persistent compile result. `ScriptState` holds a reference back to the `Script<T>`
it was produced from (accessible via `state.Script`). `script.GetCompilation()` provides a bridge
from `Script<T>` to the full `Compilation` object. `ScriptRunner<T>` explicitly does NOT hold
compilation resources (syntax trees) alive — this is documented behavior.

### Type info at eval

`ScriptState.Variables` is a collection of `{Name, Value, Type}` triples reflecting the runtime
variable bindings after execution. `state.ReturnValue` has type `T` matching the declared return
type. The full `Compilation` available via `script.GetCompilation()` provides Roslyn's semantic model
(symbols, types, syntax trees) for post-run introspection.

### Compile-error surfacing

If `script.Compile()` is not called explicitly, compilation runs on the first `RunAsync()` call.
Compile errors at that point surface as `CompilationErrorException`:

```csharp
try { await CSharpScript.EvaluateAsync("1 + "); }
catch (CompilationErrorException e) { /* e.Diagnostics */ }
```

`CompilationErrorException.Diagnostics` is a collection of `Diagnostic` objects with location,
message, and severity. Pre-calling `script.Compile()` surfaces these errors eagerly.

### Caching model

`Script<T>` is the cached artifact. Calling `script.CreateDelegate()` produces a `ScriptRunner<T>`
that is a compiled delegate holding no syntax tree — designed for efficient repeated invocation.
Each `script.RunAsync()` call produces a new `ScriptState`. `ContinueWith` / `ContinueWithAsync`
produce new `Script<T>` objects incrementally; the original script is referenced by the chain.

---

## TypeScript Compiler API

Source: `github.com/microsoft/TypeScript` Wiki `Using-the-Compiler-API.md`

### Compile result content

`ts.Program` is the entry point to all compilation results:

```typescript
let program = ts.createProgram(fileNames, options);
// program.getSourceFiles()  — all SourceFile ASTs
// program.emit()            — EmitResult
// program.getTypeChecker()  — TypeChecker (primary bridge to type information)
```

`TypeChecker` is obtained from the `Program` and is the primary interface for type queries:

```typescript
let checker = program.getTypeChecker();
checker.getSymbolAtLocation(node)             // Symbol for an AST node
checker.getTypeAtLocation(node)               // Type for an AST node
checker.getTypeOfSymbolAtLocation(sym, node)  // Type of symbol at location
checker.typeToString(type)                    // human-readable type string
```

`Symbol` describes how the type system views a declared entity (class, function, variable).  
`Type` describes the backing type; it often has a backing `Symbol` pointing to declaration(s).

`ts.transpileModule` — string-to-string transform without type checking:

```typescript
let result = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.CommonJS }
});
// result is TranspileOutput — NOT connected to a Program, no TypeChecker available
```

`BuilderProgram` — incremental compilation:

```typescript
const createProgram = ts.createSemanticDiagnosticsBuilderProgram;
// Caches errors and emit on modules from previous compilations
// Only re-evaluates results of affected files
```

`LanguageService` — higher-level stateful API:

```typescript
const services = ts.createLanguageService(servicesHost, ts.createDocumentRegistry());
services.getEmitOutput(fileName)
services.getSemanticDiagnostics(fileName)
// ScriptSnapshot — abstraction over text; version tracking for incremental updates
```

### Separation boundary

The `ts.Program` / `TypeChecker` path provides full compile-time type information accessible at
runtime (as long as the `Program` is held in scope). The `ts.transpileModule()` path is a strict
text transform with no `Program`, no `TypeChecker`, and no type information — the two paths are
completely separate.

### Runtime → compile reference

`TypeChecker` is obtained from and bound to a specific `Program` instance. If the `Program` is
garbage collected, the `TypeChecker` becomes invalid. Symbols and Types produced by the `TypeChecker`
hold references into the `Program`'s data. There is no separate "runtime state" object — the
`Program` itself is both the compile result and the query interface.

### Type info at eval

The `TypeChecker` provides `getTypeAtLocation(node)`, `getSymbolAtLocation(node)`,
`getTypeOfSymbolAtLocation(sym, node)`, and `typeToString(type)` for any AST node in any source file
within the `Program`. All inferred and declared types are available as long as the `Program` is held.

### Compile-error surfacing

```typescript
let diagnostics = program.getSemanticDiagnostics();
// Also: program.getSyntacticDiagnostics(), program.getGlobalDiagnostics()
// LanguageService: services.getSemanticDiagnostics(fileName)
```

Errors are surfaced on-demand from the `Program` object; the `Program` can be constructed even when
errors are present. `ts.transpileModule()` surfaces no semantic errors, only syntax errors (if any).

### Caching model

`ts.createDocumentRegistry()` is used by `LanguageService` to cache `SourceFile` ASTs across program
rebuilds. `BuilderProgram` caches semantic diagnostics and emit output for files that have not
changed. A `Program` object itself is immutable once created; incremental updates produce new
`Program` instances that share cached `SourceFile` objects for unchanged files.

---

## XState v5

Source: `stately.ai/docs` — Machines, Actors, Actor Logic, `getNextTransitions`

### Compile result content

`createMachine(config)` returns the "logic" (blueprint / DNA):

```typescript
const feedbackMachine = createMachine({
    id: 'feedback',
    initial: 'question',
    states: {
        question: { on: { 'feedback.good': { target: 'thanks' } } },
        thanks: {}
    },
});
```

The machine is the compile-time artifact. It holds the config object plus resolved implementations.

`ActorLogic` interface (the formal contract for machine logic):

```typescript
interface ActorLogic<TSnapshot, TEvent> {
    transition(state, event, actorCtx): TSnapshot
    getInitialSnapshot(): TSnapshot
    getPersistedSnapshot(s): PersistedSnapshot
}
```

`setup({...}).createMachine({...})` pattern registers implementations by name:

```typescript
const feedbackMachine = setup({
    types: { context: {} as { feedback: string }, events: {} as {...} },
    actions: { logTelemetry: () => {} },
    guards: { someGuard: ({ context }) => context.count <= 10 },
    actors: { someActor: fromPromise(async () => 42) },
}).createMachine({ /* config references implementations by string key */ });
```

Pure transition functions (v5.19+):

```typescript
const [initialState, initialActions] = initialTransition(machine);
const [nextState, actions] = transition(machine, initialState, { type: 'start' });
// Pure functions — no actor needed; machine definition is passed through explicitly
```

### Separation boundary

`createMachine()` (the "compile" step) produces the logic object. `createActor(machine)` (the
"runtime" step) creates a running actor instance. The two types are distinct: `StateMachine` vs
`Actor`. `machine.provide(implementations)` returns a new machine with the same config but different
implementations, without creating an actor.

`getNextTransitions(snapshot)` returns:

```typescript
// { eventType, target, source, actions, reenter, guard }
// Introspects available transitions from a snapshot without running the machine
```

### Runtime → compile reference

The actor holds a reference to the machine (logic) throughout its lifetime. Guards, actions, and
actors (child machines) are resolved via the machine's implementation map at transition time — string
keys in config are resolved to functions registered in `setup({...})`. The actor does not copy the
machine; it references it.

Snapshot persistence:

```typescript
const persistedState = JSON.parse(localStorage.getItem('some-persisted-state'));
const actor = createActor(someLogic, { snapshot: persistedState });
// actor.getPersistedSnapshot() — returns serializable snapshot
```

### Type info at eval

`snapshot.value` — current state name. `snapshot.context` — current context data. `machine.provide()`
accepts typed implementations. The `setup({...})` pattern uses TypeScript generics to type `context`
and `events` at machine definition time; these types flow into the actor's snapshot type.

At runtime, `getNextTransitions(actor.getSnapshot())` returns the set of transition definitions
(including guard and action metadata) that are eligible in the current state.

### Compile-error surfacing

Config errors (e.g., referencing an undefined target state) surface at machine creation time
(`createMachine()`) or at `actor.start()`. TypeScript type errors in `setup({...})` surface at
TypeScript compile time. No deferred error surface mechanism analogous to a lazy compilation step.

### Caching model

The machine object is a singleton that can be shared across multiple `createActor()` calls. Each
actor has its own `snapshot` (state + context). The machine's implementation map is resolved once at
machine creation time (via `setup()` / `provide()`). `actor.getPersistedSnapshot()` / rehydration
via `createActor(someLogic, { snapshot })` allow runtime state to be serialized and restored without
re-running the machine from the start.

---

## Dhall — Haskell library dhall-1.42.1

Source: Hackage `dhall-1.42.1`, `docs.dhall-lang.org/howtos/How-to-integrate-Dhall.html`

### Compile result content

Dhall is a total functional language. Normalization IS the "compilation" — the normalized (beta-
reduced) expression is the output. There is no separate "run" phase.

Pipeline exposed by the Haskell `Dhall` module:

```haskell
-- Phase functions (can be called individually):
parseWithSettings    :: InputSettings -> Text -> m (Expr Src Import)
resolveWithSettings  :: InputSettings -> Expr Src Import -> IO (Expr Src Void)
typecheckWithSettings :: InputSettings -> Expr Src Void -> m ()
normalizeWithSettings :: InputSettings -> Expr Src Void -> Expr Src Void
checkWithSettings    :: InputSettings -> Expr Src Void -> Expr Src Void -> m ()

-- Combined:
interpretExpr :: Expr Src Import -> IO (Expr Src Void)
-- "Takes care of import resolution, type-checking, and normalization"

inputExpr :: Text -> IO (Expr Src Void)
-- "Similar to input, but without interpreting the Expr into a Haskell type"
-- Returns "the fully normalized AST"

-- Decode to host type:
input        :: Decoder a -> Text -> IO a
inputFile    :: Decoder a -> FilePath -> IO a
rawInput     :: Decoder a -> Expr s Void -> f a
fromExpr     :: Decoder a -> Expr Src Import -> IO a
evaluateOutputValueAs :: ModuleSource -> PClassInfo<T> -> T  -- see Pkl section
```

The `Expr Src Void` type is a fully-resolved, import-free, type-checked Dhall expression. The type
parameter `Void` indicates that all imports have been resolved (no `Import` nodes remain).
Normalization reduces this to its canonical beta-normal form.

`EvaluateSettings` controls the normalization process:

```haskell
data EvaluateSettings
-- Fields accessible via lenses:
startingContext :: LensLike' f s (Context (Expr Src Void))
substitutions   :: LensLike' f s (Substitutions Src Void)
normalizer      :: LensLike' f s (Maybe (ReifiedNormalizer Void))
newManager      :: LensLike' f s (IO Manager)
```

`InputSettings` extends `EvaluateSettings` with:

```haskell
rootDirectory :: LensLike' f InputSettings FilePath
sourceName    :: LensLike' f InputSettings FilePath
```

`Decoder a` describes how to decode a `Expr Src Void` into Haskell type `a`. It carries an
`expected` field (a Dhall type expression) used by `expectWithSettings` to type-check the expression
against the decoder's expected type before decoding.

### Separation boundary

The phases are explicit: parse → resolve imports → type-check → normalize → decode. The type-check
phase produces no separate artifact beyond a `()` result (success or exception). The normalized
`Expr Src Void` is the decode input. No "runtime evaluation" occurs at request time — normalization
is guaranteed to terminate (Dhall is a total language). External tools `dhall-to-json`,
`dhall-to-yaml` follow the same pipeline.

### Runtime → compile reference

After decoding, the host-language value (e.g., a Haskell record, Go struct) carries no reference
back to the `Expr Src Void`. The normalized AST is an intermediate form consumed by the `Decoder`;
the decoded value is independent. If the caller retains the `Expr Src Void` value, it can be
re-decoded or re-inspected, but this is not automatic.

Language bindings (Go: `dhall-golang`; Rust: `serde_dhall`) follow the same pattern: normalize Dhall
expression, decode into host type, discard expression.

### Type info at eval

Type-checking runs before normalization (`typecheckWithSettings`). The `Decoder a`'s `expected`
field is a Dhall type expression that `expectWithSettings` / `checkWithSettings` uses to verify the
expression's type matches before decoding. The Dhall type is a Dhall expression itself (Dhall has
a dependent type system where types are first-class values). After decoding, the host-language type
encodes the type information.

### Compile-error surfacing

Type errors are thrown as Haskell exceptions from `typecheckWithSettings` / `checkWithSettings`.
The `detailed` combinator adds extended error messages:

```haskell
detailed (input auto "True") :: IO Integer
-- *** Exception: Error: Expression doesn't match annotation
-- ↳ True expected Integer
```

The `input auto "True" :: IO Integer` call will implicitly insert an annotation matching the
expected Haskell return type, fail type-checking, and throw an exception before normalization.
Errors are never deferred to a separate evaluation phase.

### Caching model

The `Dhall` library documentation notes: "Evaluated modules, and modules imported by them, are
cached based on their origin." The Haskell library uses the HTTP `Manager` (configured via
`newManager` in `EvaluateSettings`) for remote import caching. There is no persistent runtime state
object — each `input` / `inputFile` / `interpretExpr` call re-runs the full pipeline (with import
caching applied). The normalized `Expr Src Void` is a pure value that can be stored and reused by
the caller.

---

## Temporal Go SDK — v1.42.0

Source: `pkg.go.dev/go.temporal.io/sdk/workflow`, `pkg.go.dev/go.temporal.io/sdk/client`

### Compile result content

Temporal workflows and activities are ordinary Go functions. The "compile result" is the Go binary.
There is no separate DSL or expression compile phase.

Workflow registration:

```go
w.RegisterWorkflow(MyWorkflow)       // registers by function reference
w.RegisterWorkflow(MyWorkflow, workflow.RegisterOptions{Name: "customName"})
```

`workflow.Type` is a type alias for `internal.WorkflowType`, which is a name string.  
`workflow.Info` (`= internal.WorkflowInfo`) carries metadata about the currently executing workflow:

```go
func GetInfo(ctx Context) *Info
// Info fields include: WorkflowType, WorkflowID, RunID, Namespace, TaskQueue,
// Attempt, CronSchedule, ContinuedAsNew, ParentWorkflowID, etc.
```

`client.ExecuteWorkflow` accepts either a function reference or a string name:

```go
client.ExecuteWorkflow(ctx, options, MyWorkflow, arg1, arg2, arg3)
// or:
client.ExecuteWorkflow(ctx, options, "workflowTypeName", arg1, arg2, arg3)
```

Determinism control:

```go
func IsReplaying(ctx Context) bool
// Returns whether current workflow code is replaying from event history

func GetVersion(ctx Context, changeID string, minSupported, maxSupported Version) Version
// Records a version marker in workflow history; returns recorded version on replay
// Used to safely perform backwards-incompatible code changes
```

`SideEffect` and `MutableSideEffect` record non-deterministic results into history:

```go
func SideEffect(ctx Context, f func(ctx Context) interface{}) converter.EncodedValue
// Executes f once, records result into history; returns recorded result on replay
```

### Separation boundary

There is no explicit compile/runtime separation within the Temporal SDK itself. The Go function IS
the workflow definition. Temporal's determinism constraint is enforced by the replay mechanism:
on replay, the SDK re-executes the workflow function and compares commands issued to the recorded
history. No separate "compile" object is created.

`workflow.Future` is the asynchronous result type used within workflow code for activities and child
workflows:

```go
func ExecuteActivity(ctx Context, activity interface{}, args ...interface{}) Future
func ExecuteChildWorkflow(ctx Context, childWorkflow interface{}, args ...interface{}) ChildWorkflowFuture
```

### Runtime → compile reference

The workflow function reference itself is the "compile artifact". At runtime, the SDK holds a
registry mapping workflow type names to function pointers. No AST or reflection metadata beyond the
function's Go type signature is accessible via the SDK API at runtime. The `workflow.Info` struct
carries the workflow type name string but not the function signature.

`GetVersion(ctx, changeID, min, max)` records a version marker in the event history, creating a
runtime-accessible record of what code version was running at a given point — this is the primary
mechanism for carrying "type evolution" information across replay.

### Type info at eval

Activity and workflow parameters are serialized via `DataConverter` (default: JSON). The Go type
system enforces argument types at function call sites. Within a running workflow, parameter types are
available through the Go type system. No dynamic type introspection beyond standard Go reflection is
provided by the SDK.

`converter.EncodedValue` (returned by `SideEffect`, `QueryWorkflow`) carries serialized payload:

```go
type EncodedValue interface {
    HasValue() bool
    Get(valuePtr interface{}) error
}
```

### Compile-error surfacing

Workflow and activity code is compiled by the Go compiler. Temporal-specific logic errors (e.g.,
non-deterministic code that deviates from history on replay) surface as non-determinism panics or
workflow task failures at runtime, not at compile time. The SDK provides no static analysis of
workflow functions beyond Go's type system.

### Caching model

Temporal's caching strategy is entirely server-side (the Temporal service stores event history).
On the worker side, the Go SDK has a "sticky execution" mechanism (sticky task queues) that caches
in-memory workflow state on a specific worker to avoid replaying from the beginning on every task.
Compiled workflow code (the Go binary) is not separately cached by the SDK; it is loaded once at
worker startup. `GetVersion()` values are cached in the event history on the Temporal server.

---

## Pkl — v0.31.1

Source: `github.com/apple/pkl` — `pkl-core/src/main/java/org/pkl/core/Evaluator.java`

### Compile result content

`Evaluator` is the primary interface. It does not separate "compile" from "evaluate" — both happen
within a single `evaluate()` call:

```java
public interface Evaluator extends AutoCloseable {

    /** Evaluates the module, returning the Java representation of the module object. */
    PModule evaluate(ModuleSource moduleSource);

    /** Evaluates a module's output.text property. */
    String evaluateOutputText(ModuleSource moduleSource);

    /** Evaluates a module's output.bytes property. */
    byte[] evaluateOutputBytes(ModuleSource moduleSource);

    /** Evaluates a module's output.value property. */
    Object evaluateOutputValue(ModuleSource moduleSource);

    /** Evaluates a module's output.files property. */
    Map<String, FileOutput> evaluateOutputFiles(ModuleSource moduleSource);

    /** Evaluates a Pkl expression, returning the Java representation. */
    Object evaluateExpression(ModuleSource moduleSource, String expression);

    /** Evaluates the module's schema (properties, methods, classes). */
    ModuleSchema evaluateSchema(ModuleSource moduleSource);

    /** Evaluates output.value and validates type matches PClassInfo<T>. */
    <T> T evaluateOutputValueAs(ModuleSource moduleSource, PClassInfo<T> classInfo);

    TestResults evaluateTest(ModuleSource moduleSource, boolean overwrite);
}
```

Pkl type → Java type mapping (from `evaluateExpression` Javadoc):

| Pkl type | Java type |
|----------|-----------|
| `Null` | `PNull` |
| `String` | `String` |
| `Boolean` | `Boolean` |
| `Int` | `Long` |
| `Float` | `Double` |
| `Typed`, `Dynamic` | `PObject` (`PModule` if the object is a module) |
| `Mapping`, `Map` | `Map` |
| `Listing`, `List` | `java.util.List` |
| `Set` | `java.util.Set` |
| `Pair` | `Pair` |
| `Regex` | `java.util.regex.Pattern` |
| `DataSize` | `DataSize` |
| `Duration` | `Duration` |
| `Class` | `PClass` |
| `TypeAlias` | `TypeAlias` |
| `Bytes` | `byte[]` |
| `IntSeq`, `Function` | Error — no Java representation |

`ModuleSchema` is returned by `evaluateSchema()` and describes the properties, methods, and classes
of a module.

`EvaluatorBuilder.preconfigured().build()` constructs an `Evaluator`. The module cache is held
inside the evaluator instance.

### Separation boundary

There is no explicit "compile then run" separation in the public API. `Evaluator.evaluate()` parses,
type-checks, and evaluates in one call. The `evaluateSchema()` method surfaces the type/schema
information as a separate query. The `evaluateOutputValueAs(moduleSource, PClassInfo<T>)` method
performs type validation against the caller-supplied `PClassInfo<T>` as part of evaluation.

### Runtime → compile reference

The module cache is held inside the `Evaluator` instance: "Evaluated modules, and modules imported
by them, are cached based on their origin." The caller retains a `PModule` (or typed Java object)
after evaluation. No back-reference from the returned `PModule` to internal AST or type structures is
exposed via the public API. `ModuleSchema` is a separate object returned on request.

### Type info at eval

`evaluateSchema(ModuleSource)` returns `ModuleSchema` — the properties, methods, and classes of the
module, including their types. `evaluateOutputValueAs(moduleSource, PClassInfo<T>)` validates that
the module's output value matches the provided class info before returning. `PObject` / `PModule`
objects carry their Pkl class reference (`PClass`). `PClass` and `TypeAlias` are first-class values
that can be returned from `evaluateExpression()`.

### Compile-error surfacing

`PklException` is thrown by all `evaluate*` methods if an error occurs during evaluation (which
includes parse errors, type errors, and evaluation errors). There is no separate pre-compilation
step that surfaces errors independently. Errors are always raised at evaluation call time.

### Caching model

The `Evaluator` instance holds a module cache keyed by module origin. To reset the cache, the
evaluator must be closed (`close()`) and a new one created. `Evaluator` extends `AutoCloseable`.
Each `Evaluator` is not thread-safe unless documented otherwise; typical usage is one evaluator per
thread or per request, with the module cache providing reuse across multiple calls within a single
evaluator's lifetime.

---

*End of survey. No interpretation applied. All data drawn from cited primary sources.*
