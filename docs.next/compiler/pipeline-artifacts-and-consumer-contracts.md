# Pipeline Artifacts & Consumer Contracts

> **Status:** Draft
> **Decisions answered:** D1 (pipeline artifact split), D6 (LS incremental strategy), D7 (LS coupling model)
> **Survey references:** compilation-result-type-survey, language-server-integration-survey, compiler-result-to-runtime-survey

## Overview

The Precept compiler pipeline produces two distinct surfaces from a single source text:

- **CompilationResult** — the tooling surface, consumed by the language server and MCP tools. Produced on every compilation, including broken input.
- **Precept** — the runtime surface, consumed by fire/inspect/update operations. Only produced when the compilation has no errors.

These are distinct types with distinct API contracts, produced from the same pipeline session. This document defines both artifacts, the pipeline that produces them, the consumer contracts for each, and the LS integration model.

## Pipeline Stages

The compiler runs five stages in a fixed, unconditional order. Every stage runs on every compilation — there is no short-circuiting. Each stage returns a degraded artifact on bad input rather than throwing (Model A — resilient).

| Stage | Static Class | Input | Output |
|-------|-------------|-------|--------|
| Lexer | `Lexer.Lex` | `string source` | `TokenStream` |
| Parser | `Parser.Parse` | `TokenStream` | `SyntaxTree` |
| Type Checker | `TypeChecker.Check` | `SyntaxTree` | `TypedModel` |
| Graph Analyzer | `GraphAnalyzer.Analyze` | `TypedModel` | `GraphResult` |
| Proof Engine | `ProofEngine.Prove` | `TypedModel` + `GraphResult` | `ProofModel` |

Lexer → Parser → TypeChecker form a linear chain. GraphAnalyzer takes only the typed model. ProofEngine takes both the typed model and graph result, running in series after GraphAnalyzer.

Each stage is a static class with no state, no DI, and no substitution point. Stages are pure transformations: same input always produces same output. Tests call stages directly and assert on the output.

### Rationale: Why static classes

- No state to construct or carry between calls.
- No substitution needed — the pipeline is not a plugin point.
- Tests feed inputs and assert outputs directly; no mocking needed.
- `TypeChecker.Check(tree)` reads as a direct statement of what's happening.

If a specific stage later needs configuration (e.g., dialect options), a parameter is added to the stage method before reaching for an instance class.

### Rationale: Why all stages always run

At Precept's DSL scale, the full pipeline runs in microseconds. No legitimate consumer needs fewer stages — they all need the complete diagnostic picture. The only variable is which artifacts they query afterward. Always running all stages is simpler than managing partial results.

### Rationale: Why stages run in series (GraphAnalyzer → ProofEngine)

1. GraphAnalyzer runs in microseconds at DSL scale — no performance gain from parallelism.
2. ProofEngine needs GraphAnalyzer's `ReachableStates` before finalizing dead-guard attribution.
3. Modifier verdicts must be graph-structural only to avoid a cycle.
4. State-entry proof narrowing (Phase 2) requires ProofEngine to be restartable after GraphAnalyzer.

## D1 — Pipeline Artifact Split

### CompilationResult (Tooling Surface)

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

**Design decisions:**

- **Sealed record class.** Immutable by construction. Value equality useful in tests. `with` expressions useful for creating test variants. No subclassing needed.
- **All artifacts present.** `CompilationResult` holds `Tokens`, `SyntaxTree`, `Model`, `Graph`, and `Proof`. No nullability — every stage always runs and produces output (Model A).
- **Flat diagnostic list.** Diagnostics are merged into a single `ImmutableArray<Diagnostic>`. Each `Diagnostic` carries a `DiagnosticStage` field for LS suppression logic. No per-stage collections on `CompilationResult` — filtering by stage is done via LINQ on the flat list.
- **`HasErrors` is pre-computed.** Avoids repeated iteration over the diagnostic list.
- **`ExecutableModel` is not held here.** The runtime artifact is produced on demand, not stored in the compilation result.

**Consumers:**

| Consumer | What it reads |
|----------|--------------|
| Language server — diagnostics | `Diagnostics` (publishes merged list, may filter by stage) |
| Language server — semantic tokens | `Model` (distinguishes field/state/event names) |
| Language server — completions | `Model` (what's in scope at cursor) |
| Language server — hover | `Model` (type of expression under cursor) |
| Language server — go-to-definition | `Model` (symbol resolution) |
| Language server — preview | `Precept` (produced from `CompilationResult` when `!HasErrors`) |
| MCP `precept_compile` | `Diagnostics` + `Model` (typed structure + errors) |

`TypedModel` is the workhorse — most LS features only need it. `GraphResult` and `ProofModel` are needed primarily to surface their diagnostics.

### Precept (Runtime Surface)

`Precept` is the executable form of a precept definition. It is produced from a `CompilationResult` only when there are no errors.

```csharp
namespace Precept.Runtime;

public sealed class Precept
{
    public static Precept From(CompilationResult compilation);
    public Version From(string state, ImmutableDictionary<string, object?> data);
}
```

`Precept.From(compilation)` replaces the earlier `ExecutableModel` / `Emitter` naming. The verb `From` is used consistently: a `Precept` is produced *from* a compilation; a `Version` is produced *from* a precept + state + data.

**Rationale: Why not "Emitter"**

"Emitter" carries traditional compiler baggage — it implies producing target output (machine code, IL, JS). Precept's step builds an in-memory runtime structure. CEL uses `env.Program(ast)`, OPA uses `rego.PrepareForEval()`. `Precept.From(compilation)` aligns with the domain: the runtime artifact *is* a Precept.

### Version (Bound Entity State)

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
    public Version Edit(string field, object? value);
}
```

Every operation returns a new `Version`. There is no mutation. The old version remains valid. This replaces the prior `Inspect()` concept — you interrogate properties directly on the version rather than calling a separate inspect API.

**Consumers:**

| Consumer | Usage |
|----------|-------|
| MCP `precept_fire` | `version.Fire(event, args)` → returns new `Version` |
| MCP `precept_update` | `version.Edit(field, value)` → returns new `Version` |
| MCP `precept_inspect` | Read properties from `Version` (state, data, available events) |
| Application runtime | All of the above |

### Relationship Between Artifacts

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
    └── version.Edit(field) ──► Version (new)
```

### Immutability Invariant

All types in both surfaces are deeply immutable:

- **Record types** use `init`-only properties (record default). No property reassignment after construction.
- **Collections** use `ImmutableArray<T>` and `ImmutableDictionary<TK,TV>` internally. Exposed as `IReadOnlyList<T>` or immutable collection types. No mutable types are exposed.
- **Stage artifacts** (`TokenStream`, `SyntaxTree`, `TypedModel`, `GraphResult`, `ProofModel`) follow the same immutability contract.

This is required for the atomic swap pattern in the LS — a handler thread reading the old snapshot must not see torn state when a new compilation replaces the reference.

## D6 — LS Incremental Strategy

**Decision: Full recompile, atomic swap.**

On every file edit, the LS runs the full pipeline (`Compiler.Compile(source)`) and atomically replaces its held `CompilationResult` reference.

**Rationale:**

- At Precept's DSL scale, the full pipeline runs in microseconds. Incremental compilation infrastructure (Roslyn's red-green trees, rust-analyzer's salsa DB, gopls's persistent maps) solves a GP-scale problem that doesn't exist here.
- The surveyed DSL-scale systems (Regal/OPA, CEL, Pkl) all use full recompile on edit.
- "Atomic swap" means: run the full pipeline, produce a new `CompilationResult`, replace the LS's reference with a single pointer assignment. No structural sharing, no clone, no incremental machinery.
- The `CompilationResult` is immutable. The old reference remains valid until no handler holds it, then is garbage collected.

**What "atomic" means here:**

The swap is safe for concurrent LSP requests because `CompilationResult` is fully immutable. A handler that read the old reference sees a consistent snapshot. A new edit triggers a new compile; the new result replaces the reference. No locks needed beyond `Interlocked.Exchange` on the reference.

## D7 — LS Coupling Model

**Decision: Same codebase, direct API call.**

The language server calls `Compiler.Compile(source)` directly. No process boundary, no published NuGet package, no serialization.

**Rationale:**

- The dominant pattern across surveyed systems. Roslyn, TypeScript/tsserver, rust-analyzer, gopls, CUE, Regal — all call the compiler/checker directly from the LS process.
- LS-to-compiler code ratio at DSL scale is 1:3 to 1:10 (Dhall 1:10, Regal/OPA 1:8, Jsonnet 1:3). The LS is thin; a process boundary would add complexity disproportionate to the code size.
- Single-process means no serialization overhead, no IPC latency, no version-mismatch risk between LS and compiler.
- The current Precept architecture already uses this model — the language server calls runtime APIs directly.

**LS integration sketch:**

```csharp
// Inside the language server's document change handler
CompilationResult result = Compiler.Compile(document.Text);
Interlocked.Exchange(ref _currentCompilation, result);
PublishDiagnostics(result.Diagnostics);
```

Each LSP handler reads from `_currentCompilation`. The reference is swapped atomically on each edit.

## Type Strategy Summary

| Type | Kind | Why |
|------|------|-----|
| `CompilationResult` | `sealed record class` | Immutable snapshot, value equality for tests |
| `TokenStream` | `sealed record class` | Stage artifact, immutable |
| `SyntaxTree` | `sealed record class` | Stage artifact, immutable |
| `TypedModel` | `sealed record class` | Stage artifact, immutable |
| `GraphResult` | `sealed record class` | Stage artifact, immutable |
| `ProofModel` | `sealed record class` | Stage artifact, immutable |
| `Diagnostic` | `readonly record struct` | Small, value-typed, zero-allocation in collections |
| `DiagnosticStage` | `enum` | Parse, Type, Graph, Proof |
| `Severity` | `enum` | Info, Warning, Error |
| `Precept` | `sealed class` | Runtime definition, not a data bag — has factory methods |
| `Version` | `sealed record class` | Immutable entity snapshot, value equality |

No interfaces. No abstract classes. One implementation of each type. Interfaces are added only when a second implementation appears or a consumer needs substitution.

## Alternatives Considered

### Fluent Builder / Step Builder for the pipeline

The Step Builder pattern (each step returns a different interface, terminal `Compile()` call) was evaluated. It fits structurally — ordered mandatory steps, heterogeneous return types, terminal call. Rejected because:

- The pipeline is internal to `Compiler.Compile()` — no external caller sees or composes the steps.
- Tests that need intermediate artifacts call stages directly (`Parser.Parse(tokens)`).
- 6 step interfaces + a Steps class for a single internal call site is unjustified infrastructure.

### Per-stage diagnostic collections on CompilationResult

Separate `ParseDiagnostics`, `TypeDiagnostics`, `GraphDiagnostics`, `ProofDiagnostics` properties were evaluated. Rejected in favor of a flat list with `DiagnosticStage` on each diagnostic:

- Single source of truth. No five collections to keep in sync.
- LS suppression logic (`d.Stage == DiagnosticStage.Type`) works via LINQ on the flat list.
- Matches Roslyn's approach (diagnostic category on each diagnostic, not separate collections per analyzer).

### Nullable stage artifacts (Model B — short circuit)

If TypeChecker fails, GraphAnalyzer doesn't run; `Graph` and `Proof` are null. Rejected because:

- Every LS feature that queries these objects needs null-checks, degrading to Model B behavior anyway.
- Model A (always present, possibly degraded) allows the LS to query unconditionally — fewer results, not no results.
- Go's `go/types` and `go/parser` demonstrate that error recovery makes Model A essentially free when the stages are designed for it.

### Emitter as the name for Precept.From()

"Emitter" implies producing target output (machine code, IL, JS). Precept builds an in-memory runtime structure. Replaced with `Precept.From(compilation)`, matching the domain and avoiding traditional compiler baggage.

### PreceptInstance as the name for Version

Evaluated `PreceptInstance`, `PreceptEntity`, `PreceptContext`. `Version` was chosen because it captures the immutable snapshot semantics — each `Fire` / `Edit` produces a new version of the entity's state. The `Precept.Runtime` namespace disambiguates from `System.Version`.

## Open Questions

- **What is inside `Precept`?** Dispatch tables, slot-indexed expression trees, scope-indexed constraints — deferred to D8 (Emitter contract, Phase 4).
- **Version chain / linked list.** Discussed and deferred. Each `Version` could hold a `Previous` reference for undo/replay/audit. Not needed for Phase 1.
- **`Version` interrogation API.** Properties like `AvailableEvents`, `CanFire(event)`, `PreviewFire(event)` are natural but not yet specified.
- **Persistence boundary.** Applications serialize state + data externally and reconstruct via `precept.From(state, data)`. No persistence API on `Version` for now.
