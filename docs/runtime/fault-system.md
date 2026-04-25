# Fault System

> **Status:** Stub — runtime design is not yet started
> **Relates to:** `docs/compiler/diagnostic-system.md` (FaultCode → DiagnosticCode chain)
> **Implemented in:** `src/Precept.Next/Runtime/`

## Overview

The runtime fault system classifies and communicates evaluator failure modes. It is the runtime mirror of the compiler's diagnostic system.

| Compiler (pipeline) | Runtime (evaluator) |
|---------------------|---------------------|
| `DiagnosticCode` | `FaultCode` |
| `DiagnosticMeta(Code, Stage, Severity, MessageTemplate)` | `FaultMeta(Code, MessageTemplate)` |
| `Diagnostics` | `Faults` |
| `Diagnostic(Severity, Stage, string Code, Message, Range)` | `Fault(FaultCode Code, string CodeName, Message)` |

The relationship between the two systems is structural, not incidental. Every `FaultCode` member carries a `[StaticallyPreventable(DiagnosticCode.X)]` attribute — a compiler-enforced assertion that there exists a pipeline diagnostic which, if emitted, guarantees this fault can never occur at runtime. See `diagnostic-system.md` for the full enforcement chain.

## Current Stubs

### Fault

```csharp
// src/Precept.Next/Runtime/Fault.cs
public readonly record struct Fault(
    FaultCode Code,
    string    CodeName,  // code.ToString() — stable identity for logging / MCP
    string    Message    // pre-formatted, final English string
);
```

`Fault` is the output type the evaluator produces when a failure path is reached. Currently a minimal stub — three fields matching the simplest consumer needs (identity, human-readable description).

### FaultMeta

```csharp
// src/Precept.Next/Runtime/Faults.cs
public sealed record FaultMeta(
    string Code,
    string MessageTemplate
);
```

Same shape as `DiagnosticMeta`: a `Code` string derived via `nameof()` in the exhaustive switch, and a message template for formatting. `Faults.Create()` derives `CodeName` on `Fault` from `meta.Code` — the same `nameof()`-at-the-registry pattern used by `Diagnostics.Create()` for `Diagnostic.Code`.

### Faults

```csharp
public static class Faults
{
    public static FaultMeta GetMeta(FaultCode code) => code switch { /* exhaustive, nameof()-derived */ };
    public static Fault Create(FaultCode code, params object?[] args);  // derives CodeName from meta.Code
    public static IReadOnlyList<FaultMeta> All { get; }
}
```

Same shape as `Diagnostics`: exhaustive switch, factory, enumeration. `Create()` derives `Fault.CodeName` from `meta.Code` (which is `nameof(FaultCode.X)` from the switch arm) — not from `code.ToString()`. Roslyn rule **PRECEPT0004** enforces that all `Fault` constructions go through `Create()`, closing the string-field bypass path. See `diagnostic-system.md` for the full PREC rule set rationale.

## Open Design Questions

### Q1 — Evaluator result type

What does the evaluator return? The fault system's output type depends on this decision.

**Candidates:**

| Option | Shape | Trade-off |
|--------|-------|-----------|
| Exception | `throw FaultException(fault)` | Simple; fault is rare path; loses structured return |
| Discriminated union | `Result<T, Fault>` | Explicit at every call site; C# lacks native DU syntax |
| Out parameter | `bool TryEval(out T value, out Fault? fault)` | Familiar; ugly for nested expressions |
| Nullable return + fault field | `EvalResult<T> { Value?, Fault? }` | Simple record; requires null checks |

The prototype uses exceptions (`ConstraintViolationException`). The new design should decide whether Fault is a control-flow exception or a structured value before `Fault` is finalized.

### Q2 — Fault context

When a fault occurs, what context is useful to a consumer?

- **Expression context:** which expression was being evaluated (`a / rate` — which subexpression triggered it)?
- **Input values:** what were the concrete values at fault time (`rate = 0`)?
- **DiagnosticCode linkage:** should `Fault` carry a reference back to the `DiagnosticCode` that should have prevented it (via `[StaticallyPreventable]`)?
- **Event context:** which event fire triggered evaluation?

The current stub carries none of this. The answers depend on what MCP `precept_fire` needs to return and what the preview inspector needs to display.

### Q3 — Fault vs. structural impossibility

When the compiler emits errors, `Precept.From(CompilationResult)` returns null — the evaluator never runs. `Fault` is therefore a defense-in-depth type: it classifies failures that *should never occur* but must be handled if they do (e.g. data loaded from external sources bypassing the compile-time check, or a proof engine gap).

This affects how aggressively the evaluator should assert vs. gracefully fault. Decision needed at evaluator design time.

## Relationship to Prototype

The prototype evaluator throws `ConstraintViolationException` on rule violations and returns structured errors from `CompileFromTextResult`. There is no equivalent of `FaultCode` — failure modes are unclassified. The `FaultCode → DiagnosticCode` chain is entirely new to the redesign.
