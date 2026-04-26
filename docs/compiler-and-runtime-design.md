# Compiler & Runtime Design

## Overview

The Precept system has two major surfaces produced from a single source text:

- **CompilationResult** — the tooling surface, consumed by the language server and MCP tools. Produced on every compilation, including broken input.
- **Precept** — the runtime surface, consumed by fire/inspect/update operations. Only produced when the compilation has no errors.

These are distinct types with distinct API contracts, produced from the same pipeline session.

---

## Compiler Pipeline

The compiler runs five stages in a fixed, unconditional order. Every stage runs on every compilation — there is no short-circuiting. Each stage returns a degraded artifact on bad input rather than throwing.

| Stage | Static Class | Input | Output |
|-------|-------------|-------|--------|
| Lexer | `Lexer.Lex` | `string source` | `TokenStream` |
| Parser | `Parser.Parse` | `TokenStream` | `SyntaxTree` |
| Type Checker | `TypeChecker.Check` | `SyntaxTree` | `TypedModel` |
| Graph Analyzer | `GraphAnalyzer.Analyze` | `TypedModel` | `GraphResult` |
| Proof Engine | `ProofEngine.Prove` | `TypedModel` + `GraphResult` | `ProofModel` |

Lexer → Parser → TypeChecker form a linear chain. GraphAnalyzer takes only the typed model. ProofEngine takes both the typed model and graph result, running in series after GraphAnalyzer.

Each stage is a static class with no state, no DI, and no substitution point. Stages are pure transformations: same input always produces same output. Tests call stages directly and assert on the output. If a stage later needs configuration, a parameter is added to the stage method before reaching for an instance class.

All stages always run because, at Precept's DSL scale, the full pipeline runs in microseconds. No consumer needs fewer stages — they all need the complete diagnostic picture. The only variable is which artifacts they query afterward.

GraphAnalyzer and ProofEngine run in series because ProofEngine needs GraphAnalyzer's `ReachableStates` for dead-guard attribution, and modifier verdicts must be graph-structural only to avoid a cycle.

---

## CompilationResult

`CompilationResult` is a sealed, immutable snapshot of the entire pipeline's output. It holds every stage artifact and the merged diagnostic collection.

```csharp
public sealed record class CompilationResult(
    TokenStream                Tokens,
    SyntaxTree                 SyntaxTree,
    TypedModel                 Model,
    GraphResult                Graph,
    ProofModel                 Proof,
    ImmutableArray<Diagnostic> Diagnostics,
    bool                       HasErrors
);
```

All artifacts are always present — every stage always runs and produces output, even on broken input. Diagnostics are merged into a single `ImmutableArray<Diagnostic>`, with each `Diagnostic` carrying a `DiagnosticStage` field for filtering. `HasErrors` is pre-computed to avoid repeated iteration.

### Consumers

| Consumer | What it reads |
|----------|--------------|
| Language server — diagnostics | `Diagnostics` (publishes merged list, may filter by stage) |
| Language server — semantic tokens | (1) keyword tokens classified from `TokenKind` metadata alone; (2) identifier tokens resolved against `Model.Fields`, `Model.States`, `Model.Events` |
| Language server — completions | `Model` (what's in scope at cursor) |
| Language server — hover | `Model` (type of expression under cursor) |
| Language server — go-to-definition | `Model` (symbol resolution) |
| Language server — preview | `Precept` (produced from `CompilationResult` when `!HasErrors`) |
| MCP `precept_compile` | `Diagnostics` + `Model` (typed structure + errors) |

`TypedModel` is the workhorse — most LS features only need it. `GraphResult` and `ProofModel` are needed primarily to surface their diagnostics.

---

## Precept (Runtime Surface)

`Precept` is the executable form of a precept definition, produced from a `CompilationResult` only when there are no errors.

```csharp
namespace Precept.Runtime;

public sealed class Precept
{
    public static Precept From(CompilationResult compilation);
    public Version From(string state, ImmutableDictionary<string, object?> data);
}
```

`Precept.From(compilation)` constructs the runtime artifact — dispatch tables, slot-indexed expression trees, scope-indexed constraints. The verb `From` is used consistently: a `Precept` is produced *from* a compilation; a `Version` is produced *from* a precept + state + data.

---

## Version (Entity State)

`Version` is an immutable snapshot of a specific entity running through a precept at a point in time.

```csharp
namespace Precept.Runtime;

public sealed record class Version(
    Precept                              Precept,
    string                               State,
    ImmutableDictionary<string, object?> Data
)
{
    public Version Fire(string eventName, ImmutableDictionary<string, object?>? args = null);
    public UpdateOutcome Update(IReadOnlyDictionary<string, object?> fields);
}
```

Every operation returns a new `Version`. There is no mutation. The old version remains valid. Properties on `Version` provide the inspectable surface — there is no separate inspect API.

### Consumers

| Consumer | Usage |
|----------|-------|
| MCP `precept_fire` | `version.Fire(event, args)` → new `Version` |
| MCP `precept_update` | `version.Update(fields)` → `UpdateOutcome` |
| MCP `precept_inspect` | Read properties from `Version` (state, data, available events) |
| Application runtime | All of the above |

---

## Artifact Relationship

```
string source
    │
    ▼
Compiler.Compile(source) ──► CompilationResult
    │                            │
    │                            ├── Tokens
    │                            ├── SyntaxTree
    │                            ├── Model (TypedModel)
    │                            ├── Graph (GraphResult)
    │                            ├── Proof (ProofModel)
    │                            ├── Diagnostics
    │                            └── HasErrors
    │
    ▼ (only when !HasErrors)
Precept.From(compilation) ──► Precept
    │
    ▼
precept.From(state, data) ──► Version
    │
    ├── version.Fire(event) ──► Version (new)
    └── version.Update(fields) ──► UpdateOutcome
```

---

## Immutability

All types in both surfaces are deeply immutable:

- **Record types** use `init`-only properties. No property reassignment after construction.
- **Collections** use `ImmutableArray<T>` and `ImmutableDictionary<TK,TV>` internally. No mutable types are exposed.
- **Stage artifacts** (`TokenStream`, `SyntaxTree`, `TypedModel`, `GraphResult`, `ProofModel`) follow the same contract.

This is required for the atomic swap pattern in the LS — a handler thread reading the old snapshot must not see torn state when a new compilation replaces the reference.

---

## Language Server Integration

### Full Recompile, Atomic Swap

On every file edit, the LS runs the full pipeline (`Compiler.Compile(source)`) and atomically replaces its held `CompilationResult` reference.

At Precept's DSL scale, the full pipeline runs in microseconds. Incremental compilation infrastructure (Roslyn's red-green trees, rust-analyzer's salsa DB) solves a GP-scale problem that doesn't exist here. The surveyed DSL-scale systems (Regal/OPA, CEL, Pkl) all use full recompile on edit.

The swap is safe for concurrent LSP requests because `CompilationResult` is fully immutable. A handler that read the old reference sees a consistent snapshot. No locks needed beyond `Interlocked.Exchange` on the reference.

### Same Codebase, Direct API Call

The language server calls `Compiler.Compile(source)` directly. No process boundary, no published NuGet package, no serialization.

This is the dominant pattern across language tooling. LS-to-compiler code ratio at DSL scale is 1:3 to 1:10. Single-process means no serialization overhead, no IPC latency, no version-mismatch risk.

```csharp
// Inside the language server's document change handler
CompilationResult result = Compiler.Compile(document.Text);
Interlocked.Exchange(ref _currentCompilation, result);
PublishDiagnostics(result.Diagnostics);
```

Each LSP handler reads from `_currentCompilation`. The reference is swapped atomically on each edit.

---

## Type Strategy

| Type | Kind | Why |
|------|------|-----|
| `CompilationResult` | `sealed record class` | Immutable snapshot, value equality for tests |
| `TokenStream` | `sealed record class` | Stage artifact, immutable |
| `SyntaxTree` | `sealed record class` | Stage artifact, immutable |
| `TypedModel` | `sealed record class` | Stage artifact, immutable |
| `GraphResult` | `sealed record class` | Stage artifact, immutable |
| `ProofModel` | `sealed record class` | Stage artifact, immutable |
| `Diagnostic` | `readonly record struct` | Small, value-typed, zero-allocation in collections |
| `DiagnosticStage` | `enum` | Lex, Parse, Type, Graph, Proof |
| `Severity` | `enum` | Info, Warning, Error |
| `Precept` | `sealed class` | Runtime definition — has factory methods, not a data bag |
| `Version` | `sealed record class` | Immutable entity snapshot, value equality |

No interfaces. No abstract classes. One implementation of each type. Interfaces are added only when a second implementation appears or a consumer needs substitution.
