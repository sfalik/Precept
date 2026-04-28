# Fault System

## Status

| Property | Value |
|----------|-------|
| Doc maturity | Draft — type definitions documented; evaluator integration sections pending |
| Implementation state | Partial — `FaultCode`, `FaultMeta`, `Faults`, `Fault`, `FaultSeverity` implemented; `FaultException` and evaluator integration pending |
| Source | `src/Precept/Language/FaultCode.cs`, `src/Precept/Language/Fault.cs`, `src/Precept/Language/Faults.cs` |
| Upstream | `docs/compiler/diagnostic-system.md` — every `FaultCode` member references a `DiagnosticCode` via `[StaticallyPreventable]` |
| Downstream | `docs/runtime/result-types.md` — `FaultException` exits the outcome hierarchy; `docs/runtime/evaluator.md` — the evaluator produces `Fault` values |

## Overview

The runtime fault system classifies and communicates evaluator failure modes. It is the runtime mirror of the compiler's diagnostic system.

| Compiler (pipeline) | Runtime (evaluator) |
|---------------------|---------------------|
| `DiagnosticCode` | `FaultCode` |
| `DiagnosticMeta(Code, Stage, Severity, MessageTemplate)` | `FaultMeta(Code, MessageTemplate, Severity, RecoveryHint)` |
| `Diagnostics` | `Faults` |
| `Diagnostic(Severity, Stage, string Code, Message, Range)` | `Fault(FaultCode Code, string CodeName, Message)` |

The relationship between the two systems is structural, not incidental. Every `FaultCode` member carries a `[StaticallyPreventable(DiagnosticCode.X)]` attribute — a compiler-enforced assertion that there exists a pipeline diagnostic which, if emitted, guarantees this fault can never occur at runtime. See [diagnostic-system.md](../compiler/diagnostic-system.md) for the full enforcement chain.

## Responsibilities and Boundaries

### OWNS

- The closed registry of all evaluator failure modes (`FaultCode` enum)
- `FaultMeta` — per-code message template, severity, and recovery hint
- `Faults` factory — `Create()`, `GetMeta()`, `All` enumeration
- `Fault` output type — the value produced when the evaluator hits a failure path
- `FaultSeverity` enum — currently a single `Fatal` variant
- `StaticallyPreventableAttribute` — the attribute linking each `FaultCode` member to its corresponding `DiagnosticCode`

### Does NOT OWN

- `EventOutcome`, `UpdateOutcome` result hierarchies — see `result-types.md`; faults are outside the normal outcome hierarchy
- How the evaluator throws or returns `Fault` values (`FaultException` shape, result type) — see `evaluator.md` and open question Q1
- `DiagnosticCode` definitions and compiler pipeline diagnostics — see `diagnostic-system.md`
- Roslyn analyzer rules (PRECEPT0001–PRECEPT0004, PRECEPT0016) that enforce the chain — see `src/Precept.Analyzers/`

## Right-Sizing

The fault system mirrors the compiler's diagnostic system but is trimmed for the evaluator's narrower surface.

`DiagnosticMeta` carries `Stage` and a three-level `Severity` (`Info`, `Warning`, `Error`) because the compiler produces diagnostics at five stages with meaningfully different severity levels. The evaluator has no pipeline stages — every fault is fatal (the transition is aborted, no state changes are committed) — so `FaultMeta` replaces the stage axis entirely and uses a single-variant `FaultSeverity.Fatal` for severity. `FaultSeverity` is not collapsed to a boolean because a second variant (e.g. non-aborting `Warning` faults) may be needed as the evaluator design matures.

`FaultMeta` adds two fields `DiagnosticMeta` does not have:

- **`RecoveryHint`** — actionable per-code guidance telling the precept author how to fix the definition to prevent the fault. Recovery hints appear in `precept_fire` output and the VS Code preview inspector. They are omitted from `DiagnosticMeta` because compiler diagnostic messages already include enough structural information for code fixes; the fault system targets runtime operators who may not be reading source.
- **`FaultSeverity Severity`** — placeholder for the second severity variant; currently always `Fatal`.

`Fault` omits `SourceSpan` — at runtime there is no source text in scope; expression context (if needed) will be a separate field tracked in open question Q2.

## Inputs and Outputs

**Inputs to the fault system:**

- A `FaultCode` enum member selected by the evaluator at a failure site
- Zero or more format args (`params object?[]`) for message template interpolation

**Outputs:**

| Output | Consumer |
|--------|----------|
| `Fault` (readonly record struct) | Evaluator → `FaultException` → caller (shape pending Q1) |
| `FaultMeta` | MCP `precept_language` enumerates `Faults.All` to list all evaluator fault codes |
| `Faults.All` | Language server and drift tests enumerate all registered fault codes |

## FaultCode — The Registry

`FaultCode` is the closed set of all evaluator failure modes. Every member carries `[StaticallyPreventable(DiagnosticCode.X)]`, asserting that the named diagnostic, if emitted by the compiler, makes the fault impossible at runtime.

```csharp
// src/Precept/Language/FaultCode.cs
[AttributeUsage(AttributeTargets.Field)]
public sealed class StaticallyPreventableAttribute(DiagnosticCode code) : Attribute
{
    public DiagnosticCode Code { get; } = code;
}

public enum FaultCode
{
    [StaticallyPreventable(DiagnosticCode.DivisionByZero)]
    DivisionByZero,

    [StaticallyPreventable(DiagnosticCode.SqrtOfNegative)]
    SqrtOfNegative,

    [StaticallyPreventable(DiagnosticCode.TypeMismatch)]
    TypeMismatch,

    [StaticallyPreventable(DiagnosticCode.UndeclaredField)]
    UndeclaredField,

    [StaticallyPreventable(DiagnosticCode.NullInNonNullableContext)]
    UnexpectedNull,

    [StaticallyPreventable(DiagnosticCode.InvalidMemberAccess)]
    InvalidMemberAccess,

    [StaticallyPreventable(DiagnosticCode.FunctionArityMismatch)]
    FunctionArityMismatch,

    [StaticallyPreventable(DiagnosticCode.FunctionArgConstraintViolation)]
    FunctionArgConstraintViolation,

    [StaticallyPreventable(DiagnosticCode.UnguardedCollectionAccess)]
    CollectionEmptyOnAccess,

    [StaticallyPreventable(DiagnosticCode.UnguardedCollectionMutation)]
    CollectionEmptyOnMutation,

    [StaticallyPreventable(DiagnosticCode.QualifierMismatch)]
    QualifierMismatch,

    [StaticallyPreventable(DiagnosticCode.NumericOverflow)]
    NumericOverflow,

    [StaticallyPreventable(DiagnosticCode.OutOfRange)]
    OutOfRange,
}
```

## FaultMeta and FaultSeverity

`FaultMeta` is the registry record for a single fault code — message template, severity, and recovery hint. `FaultSeverity` classifies the evaluator's response.

```csharp
// src/Precept/Language/Fault.cs
public enum FaultSeverity
{
    /// <summary>The transition fails immediately; no state changes are committed.</summary>
    Fatal,
}

// src/Precept/Language/Faults.cs
public sealed record FaultMeta(
    string        Code,
    string        MessageTemplate,
    FaultSeverity Severity     = FaultSeverity.Fatal,
    string?       RecoveryHint = null
);
```

Same structural shape as `DiagnosticMeta` at the core: a `Code` string derived via `nameof()` in the exhaustive switch, and a message template for formatting. `Faults.Create()` derives `CodeName` on `Fault` from `meta.Code` — the same `nameof()`-at-the-registry pattern used by `Diagnostics.Create()` for `Diagnostic.Code`.

`RecoveryHint` is optional; example for `DivisionByZero`: `"Guard the transition with 'when Divisor != 0', or apply the 'nonzero' or 'positive' modifier to the divisor field"`. Recovery hints appear in `precept_fire` output and the VS Code preview inspector.

## Faults — The Exhaustive Switch

```csharp
// src/Precept/Language/Faults.cs
public static class Faults
{
    public static FaultMeta GetMeta(FaultCode code) => code switch
    {
        FaultCode.DivisionByZero => new(nameof(FaultCode.DivisionByZero),
            "Divisor evaluated to zero",
            RecoveryHint: "Guard the transition with 'when Divisor != 0', or apply the 'nonzero' or 'positive' modifier to the divisor field"),
        // ... all 13 members, exhaustive, nameof()-derived ...
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    public static Fault Create(FaultCode code, params object?[] args);  // derives CodeName from meta.Code
    public static IReadOnlyList<FaultMeta> All { get; }
}
```

Same shape as `Diagnostics`: exhaustive switch, factory, enumeration. `Create()` derives `Fault.CodeName` from `meta.Code` (which is `nameof(FaultCode.X)` from the switch arm) — not from `code.ToString()`. Roslyn rule **PRECEPT0004** enforces that all `Fault` constructions go through `Create()`, closing the string-field bypass path. Roslyn rule **PRECEPT0016** verifies each switch arm's `nameof()` references the arm's own enum member (not a different member via copy-paste). See [diagnostic-system.md](../compiler/diagnostic-system.md) for the full PREC rule set rationale.

## Fault — The Output Type

```csharp
// src/Precept/Language/Fault.cs
// TODO: Stub — shape will be finalized when the evaluator result type is designed.
// Expected additions: expression context (what was being evaluated), input values that
// triggered the fault, and linkage back to the DiagnosticCode that should have prevented it.
public readonly record struct Fault(
    FaultCode Code,
    string    CodeName,  // nameof-derived via Faults.Create() — stable identity for logging / MCP
    string    Message    // pre-formatted, final English string
);
```

`Fault` is the output type the evaluator produces when a failure path is reached. Currently a minimal stub — three fields matching the simplest consumer needs (identity, human-readable description).

`Fault` is a `readonly record struct` — value-typed, zero-allocation-friendly, matching `Diagnostic`'s struct choice. The fault is a data carrier, not a behavior object; struct semantics fit.

## The StaticallyPreventable Chain

The `FaultCode → DiagnosticCode` chain is the structural guarantee connecting the fault system to the diagnostic system. It enforces that for every way the evaluator can fail at runtime, there exists a compile-time diagnostic that prevents it.

```
Evaluator fails → FaultCode member → [StaticallyPreventable] → DiagnosticCode member → pipeline stage emits it
```

If any link is missing, the build fails. The chain is enforced across five layers:

| Check | Mechanism | Custom? |
|-------|-----------|---------|
| Every `FaultCode` member has `[StaticallyPreventable]` referencing a valid `DiagnosticCode` | Roslyn **PRECEPT0002** | Yes |
| Every `Fail()` uses `FaultCode` | Roslyn **PRECEPT0001** | Yes |
| Every `Fault` is constructed via `Faults.Create()` | Roslyn **PRECEPT0004** | Yes |
| Fault catalog completeness — every `FaultCode` has metadata | CS8509 exhaustive switch on `Faults.GetMeta()` | No — C# compiler |
| `FaultMeta.Code` in each switch arm matches the arm's own enum member | Roslyn **PRECEPT0016** | Yes |
| Referenced `DiagnosticCode` member exists | Enum type safety | No — C# compiler |

### The divide-by-zero example, end to end

```
1. Evaluator: a / b where b == 0
       → Fail(FaultCode.DivisionByZero)

2. FaultCode.DivisionByZero
       → [StaticallyPreventable(DiagnosticCode.DivisionByZero)]

3. DiagnosticCode.DivisionByZero
       → Proof stage, Error, "Division by zero: '{0}' can be zero when {1}"

4. ProofEngine: analyzes every division expression
       → if divisor interval includes zero:
           Diagnostics.Create(DiagnosticCode.DivisionByZero, range, "rate", "rate has no lower bound")

5. Compilation.HasErrors == true → no Precept produced → evaluator never runs
```

The chain means `FaultCode.DivisionByZero` is defense-in-depth: it classifies a failure that should never occur in a correctly compiled precept, but must be handled if data arrives from external sources that bypassed compile-time checking.

For the complete enforcement chain walkthrough (adding a new `FaultCode` member end to end), see [diagnostic-system.md § FaultCode — The Runtime-to-Compiler Chain](../compiler/diagnostic-system.md).

## Design Rationale and Decisions

### Exhaustive switch, not attributes + reflection

The prototype uses `[TokenCategory]`, `[TokenDescription]`, `[TokenSymbol]` attributes on `PreceptToken` with reflection-driven derivation at startup. That pattern works but has a structural weakness: a missing attribute is a runtime failure, not a compile-time failure. The exhaustive switch pattern uses the C# compiler directly:

- Add a `FaultCode` member without a switch arm → **CS8509** (non-exhaustive switch expression). With `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` → **build fails**.
- No custom Roslyn rule needed for catalog completeness.
- No reflection. No source generator. No registration side effects.
- The switch IS the catalog. The compiler IS the enforcement.

See [diagnostic-system.md § Why an exhaustive switch, not attributes + reflection](../compiler/diagnostic-system.md) for the full rationale — both systems made the same choice for the same reasons.

### Recovery hints are on FaultMeta, not Fault

Recovery hints are per-code guidance, not per-instance guidance. Moving `RecoveryHint` onto `Fault` would add it to every `Fault` value and all downstream consumers. Because the hint is the same for every instance of a given `FaultCode`, it belongs on `FaultMeta`, where consumers that need it (MCP `precept_language`, preview inspector) can look it up by code without carrying it through the evaluator's output path.

### Defense-in-depth, not a primary error path

When the compiler emits errors, `Precept.From(Compilation)` cannot produce a `Precept` — the evaluator never runs. `Fault` is therefore a defense-in-depth type: it classifies failures that *should never occur* but must be handled if they do (e.g. data loaded from external sources bypassing the compile-time check, or a proof engine gap). See also Q3 below.

### Relationship to prototype

The prototype evaluator throws `ConstraintViolationException` on rule violations and returns structured errors from `CompileFromTextResult`. There is no equivalent of `FaultCode` — failure modes are unclassified. The `FaultCode → DiagnosticCode` chain is entirely new to the redesign.

## Open Questions / Implementation Notes

### Q1 — Evaluator result type

What does the evaluator return? The fault system's output type depends on this decision.

**Candidates:**

| Option | Shape | Trade-off |
|--------|-------|-----------|
| Exception | `throw FaultException(fault)` | Simple; fault is rare path; loses structured return |
| Discriminated union | `Result<T, Fault>` | Explicit at every call site; C# lacks native DU syntax |
| Out parameter | `bool TryEval(out T value, out Fault? fault)` | Familiar; ugly for nested expressions |
| Nullable return + fault field | `EvalResult<T> { Value?, Fault? }` | Simple record; requires null checks |

The prototype uses exceptions (`ConstraintViolationException`). `result-types.md` currently references `FaultException` as the throw path — faults are outside the `EventOutcome`/`UpdateOutcome` hierarchy. The new design should decide whether `Fault` is a control-flow exception or a structured value before `Fault` is finalized.

### Q2 — Fault context

When a fault occurs, what context is useful to a consumer?

- **Expression context:** which expression was being evaluated (`a / rate` — which subexpression triggered it)?
- **Input values:** what were the concrete values at fault time (`rate = 0`)?
- **DiagnosticCode linkage:** should `Fault` carry a reference back to the `DiagnosticCode` that should have prevented it (via `[StaticallyPreventable]`)?
- **Event context:** which event fire triggered evaluation?

The current stub carries none of this. The answers depend on what MCP `precept_fire` needs to return and what the preview inspector needs to display.

### Q3 — Fault vs. structural impossibility

When the compiler emits errors, `Precept.From(Compilation)` cannot produce a `Precept` — the evaluator never runs. `Fault` is therefore a defense-in-depth type: it classifies failures that *should never occur* but must be handled if they do (e.g. data loaded from external sources bypassing the compile-time check, or a proof engine gap).

This affects how aggressively the evaluator should assert vs. gracefully fault. Decision needed at evaluator design time.

## Deliberate Exclusions

**`FaultException` type** — not yet implemented. Whether faults are thrown as exceptions or returned as structured values is open question Q1. Adding `FaultException` before the evaluator result type is decided would constrain the design.

**`Fault.SourceSpan`** — excluded from the current `Fault` struct. At runtime, there is no source text in scope; the expression being evaluated is an in-memory execution plan, not a syntax tree. If expression context is needed it will be a different field shape (see Q2).

**Non-fatal fault severity** — all faults are currently `FaultSeverity.Fatal`. Informational or warning-level evaluator faults are excluded until a concrete consumer use case emerges. `FaultSeverity` is kept as an enum (not a bool) to leave this open without a breaking change.

**`DiagnosticStage` analog** — the fault system has no pipeline stage axis. The evaluator is a single-stage executor; stage classification would always be the same value and adds no information.

**Fault suppression** — the diagnostic system suppresses downstream diagnostics when upstream pipeline stages fail. The fault system has no equivalent: a fault aborts the current transition immediately, and there is no multi-stage fault pipeline to suppress.

## Cross-References

| Document | Relationship |
|----------|-------------|
| [diagnostic-system.md](../compiler/diagnostic-system.md) | Mirror document — every `FaultCode` member links to a `DiagnosticCode` via `[StaticallyPreventable]`; the full enforcement chain, end-to-end example, and PREC rule rationale are documented there |
| [result-types.md](result-types.md) | Defines `EventOutcome` and `UpdateOutcome`; faults exit this hierarchy via `FaultException` (pending Q1) |
| [evaluator.md](evaluator.md) | The evaluator is the primary consumer of the fault system; calls `Fail(FaultCode.X)` at each failure site |
| [runtime-api.md](runtime-api.md) | Public API surface that exposes fault information to host applications |

## Source Files

| File | Contents |
|------|----------|
| `src/Precept/Language/FaultCode.cs` | `FaultCode` enum + `StaticallyPreventableAttribute` |
| `src/Precept/Language/Fault.cs` | `Fault` readonly record struct + `FaultSeverity` enum |
| `src/Precept/Language/Faults.cs` | `FaultMeta` record + `Faults` static class (`GetMeta`, `Create`, `All`) |
| `src/Precept.Analyzers/Precept0001FailMustUseFaultCode.cs` | PRECEPT0001 — `Fail()` must use `FaultCode` |
| `src/Precept.Analyzers/Precept0002FaultCodeMustHaveStaticallyPreventable.cs` | PRECEPT0002 — every `FaultCode` member must have `[StaticallyPreventable]` |
| `src/Precept.Analyzers/Precept0004FaultMustUseCreate.cs` | PRECEPT0004 — `Fault` construction must go through `Faults.Create()` |
| `src/Precept.Analyzers/Precept0016FaultsCrossRef.cs` | PRECEPT0016 — `FaultMeta.Code` must use `nameof()` matching the arm's enum member |
