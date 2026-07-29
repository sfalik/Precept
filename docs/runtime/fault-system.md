# Fault System

## Status

| Property | Value |
|----------|-------|
| Doc maturity | Draft — type definitions documented; what, if anything, produces a `Fault` is undecided |
| Implementation state | Partial — `FaultCode`, `FaultMeta`, `Faults`, `Fault`, `FaultSeverity` implemented; nothing constructs a `Fault` |
| Source | `src/Precept/Language/FaultCode.cs`, `src/Precept/Language/Fault.cs`, `src/Precept/Language/Faults.cs` |
| Upstream | `docs/compiler/diagnostic-system.md` — every `FaultCode` member references a `DiagnosticCode` via `[StaticallyPreventable]` |
| Downstream | `docs/runtime/result-types.md` — the `EventOutcome.Faulted(Fault)` variant |

## Contents

- [Overview](#overview)
- [Responsibilities and Boundaries](#responsibilities-and-boundaries)
  - [OWNS](#owns)
  - [Does NOT OWN](#does-not-own)
- [Right-Sizing](#right-sizing)
- [Inputs and Outputs](#inputs-and-outputs)
- [FaultCode — The Registry](#faultcode-the-registry)
- [FaultMeta and FaultSeverity](#faultmeta-and-faultseverity)
- [Faults — The Exhaustive Switch](#faults-the-exhaustive-switch)
- [Fault — The Output Type](#fault-the-output-type)
- [The StaticallyPreventable Chain](#the-staticallypreventable-chain)
  - [The divide-by-zero example, end to end](#the-divide-by-zero-example-end-to-end)
- [Design Rationale and Decisions](#design-rationale-and-decisions)
  - [Exhaustive switch, not attributes + reflection](#exhaustive-switch-not-attributes-reflection)
  - [Recovery hints are on FaultMeta, not Fault](#recovery-hints-are-on-faultmeta-not-fault)
  - [Relationship to prototype](#relationship-to-prototype)
- [Open Questions / Implementation Notes](#open-questions-implementation-notes)
  - [Q1 — Evaluator result type](#q1-evaluator-result-type)
  - [Q2 — Fault context](#q2-fault-context)
  - [Q3 — How far the evaluator asserts about its own state](#q3-how-far-the-evaluator-asserts-about-its-own-state)
- [Deliberate Exclusions](#deliberate-exclusions)
- [Cross-References](#cross-references)
- [Source Files](#source-files)

## Overview

The fault system names the classes of failure the compiler is responsible for preventing, and pairs each one with the diagnostic that prevents it. It is shaped as a mirror of the compiler's diagnostic system, and the pairing it carries is checked when the project is built.

| Diagnostic system | Fault system |
|-------------------|--------------|
| `DiagnosticCode` | `FaultCode` |
| `DiagnosticMeta(Code, Stage, Severity, MessageTemplate)` | `FaultMeta(Code, MessageTemplate, Severity, RecoveryHint)` |
| `Diagnostics` | `Faults` |
| `Diagnostic(Severity, Stage, string Code, Message, Range)` | `Fault(FaultCode Code, string CodeName, Message)` |

The relationship between the two systems is structural, not incidental. Every `FaultCode` member carries a `[StaticallyPreventable(DiagnosticCode.X)]` attribute — a compiler-enforced assertion that there exists a pipeline diagnostic responsible for making that class of failure impossible. Nothing checks the pairing at evaluation time; the check runs when the project is built. See [diagnostic-system.md](../compiler/diagnostic-system.md) for the full enforcement chain.

## Responsibilities and Boundaries

### OWNS

- The closed registry of the failure classes the compiler must prevent (`FaultCode` enum)
- `FaultMeta` — per-code message template, severity, and recovery hint
- `Faults` factory — `Create()`, `GetMeta()`, `All` enumeration
- `Fault` output type — the name for a breach of one of the engine's own invariants
- `FaultSeverity` enum — currently a single `Fatal` variant
- `StaticallyPreventableAttribute` — the attribute linking each `FaultCode` member to its corresponding `DiagnosticCode`

### Does NOT OWN

- `EventOutcome`, `UpdateOutcome` result hierarchies — see `result-types.md`; the `EventOutcome.Faulted(Fault)` variant is defined there
- `DiagnosticCode` definitions and compiler pipeline diagnostics — see `diagnostic-system.md`
- Roslyn analyzer rules (PRECEPT0001–PRECEPT0004, PRECEPT0016) that enforce the chain — see `src/Precept.Analyzers/`

## Right-Sizing

The fault system mirrors the compiler's diagnostic system but is trimmed to what a classification without an evaluation-time path needs.

`DiagnosticMeta` carries `Stage` and a three-level `Severity` (`Info`, `Warning`, `Error`) because the compiler produces diagnostics at six stages with meaningfully different severity levels. The fault classes have no pipeline stages to distinguish and every one of them is fatal — an operation that returned `Faulted` did not finish, so its working copy is discarded and nothing it would have changed is committed. `FaultMeta` therefore replaces the stage axis entirely and uses a single-variant `FaultSeverity.Fatal` for severity. `FaultSeverity` is not collapsed to a boolean because a second variant (e.g. non-aborting `Warning` faults) may be needed as the evaluator design matures.

`FaultMeta` adds two fields `DiagnosticMeta` does not have:

- **`RecoveryHint`** — actionable per-code guidance telling the precept author how to fix the definition so the fault class cannot arise. Recovery hints appear in `precept_fire` output and the VS Code preview inspector.

  > **Open — where this guidance belongs.** The field was added because a fault could be reached at evaluation time and an operator would need to be told what to do about it. Nothing is reached at evaluation time now, so the hint's only remaining reader is the precept author, and authoring guidance already lives on the diagnostic. Keeping it here, moving it to the diagnostic, and deleting it are all open; each changes a catalog and the MCP formatters. The field is left as it is until that is decided.
- **`FaultSeverity Severity`** — placeholder for the second severity variant; currently always `Fatal`.

`Fault` omits `SourceSpan` — at runtime there is no source text in scope; expression context (if needed) will be a separate field tracked in open question Q2.

## Inputs and Outputs

**Inputs to the fault system:**

- A `FaultCode` enum member naming a failure class
- Zero or more format args (`params object?[]`) for message template interpolation, should anything ever construct a `Fault` (Q3)

**Outputs:**

| Output | Consumer |
|--------|----------|
| `Fault` (readonly record struct) | The `EventOutcome.Faulted(Fault)` variant → caller |
| `FaultMeta` | MCP `precept_proofs` enumerates `Faults.All` to list all fault codes; `precept_diagnostic <CODE>` returns per-code detail |
| `Faults.All` | Language server and drift tests enumerate all registered fault codes |

## FaultCode — The Registry

`FaultCode` is the closed set of failure classes the compiler is responsible for preventing. Every member carries `[StaticallyPreventable(DiagnosticCode.X)]`, naming the diagnostic that prevents that class.

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

    [StaticallyPreventable(DiagnosticCode.UnprovedPresenceRequirement)]
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

    [StaticallyPreventable(DiagnosticCode.LengthBoundViolation)]
    LengthBoundViolation,

    [StaticallyPreventable(DiagnosticCode.CountBoundViolation)]
    CountBoundViolation,
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
        // ... all 15 members, exhaustive, nameof()-derived ...
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
// TODO: Stub — the shape is not final and waits on what, if anything, constructs a Fault.
public readonly record struct Fault(
    FaultCode Code,
    string    CodeName,  // nameof-derived via Faults.Create() — stable identity for logging / MCP
    string    Message    // pre-formatted, final English string
);
```

`Fault` is the name for a breach of one of the engine's own invariants — a defect in the compiler, the builder or the evaluator. Currently a minimal stub — three fields matching the simplest consumer needs (identity, human-readable description). Nothing constructs one; whether anything ever will is undecided (Q3).

`Fault` is a `readonly record struct` — value-typed, zero-allocation-friendly, matching `Diagnostic`'s struct choice. The fault is a data carrier, not a behavior object; struct semantics fit.

## The StaticallyPreventable Chain

The `FaultCode → DiagnosticCode` chain is the structural guarantee connecting the fault system to the diagnostic system. It enforces that for every class of failure named here, there exists a compile-time diagnostic that prevents it.

```
FaultCode member → [StaticallyPreventable] → DiagnosticCode member → pipeline stage emits it
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

Take a definition containing `a / b`, where the compiler cannot show `b` is non-zero.

```
1. FaultCode.DivisionByZero — the failure class
       → [StaticallyPreventable(DiagnosticCode.DivisionByZero)]

2. DiagnosticCode.DivisionByZero
       → Proof stage, Error, "Division is unsafe: '{0}' can be zero{1}"

3. ProofEngine: analyzes every division expression
       → if divisor interval includes zero:
           Diagnostics.Create(DiagnosticCode.DivisionByZero, range, "rate", " in the computed expression for field 'Average'")

4. Compilation.HasErrors == true → no Precept produced → evaluator never runs
```

Step 4 is the point of the example. `FaultCode.DivisionByZero` names a class of failure and names the diagnostic that prevents it; it does not name anything that happens while an event is being evaluated. A definition whose divisor cannot be shown non-zero never becomes an executable model, and one that does become an executable model carries no divisor test.

For the complete enforcement chain walkthrough (adding a new `FaultCode` member end to end, and what that rules out), see [diagnostic-system.md § FaultCode → DiagnosticCode Chain](../compiler/diagnostic-system.md).

## Design Rationale and Decisions

### Exhaustive switch, not attributes + reflection

The prototype uses `[TokenCategory]`, `[TokenDescription]`, `[TokenSymbol]` attributes on `PreceptToken` with reflection-driven derivation at startup. That pattern works but has a structural weakness: a missing attribute is a runtime failure, not a compile-time failure. The exhaustive switch pattern uses the C# compiler directly:

- Add a `FaultCode` member without a switch arm → **CS8509** (non-exhaustive switch expression). With `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` → **build fails**.
- No custom Roslyn rule needed for catalog completeness.
- No reflection. No source generator. No registration side effects.
- The switch IS the catalog. The compiler IS the enforcement.

See [diagnostic-system.md § Why an exhaustive switch, not attributes + reflection](../compiler/diagnostic-system.md) for the full rationale — both systems made the same choice for the same reasons.

### Recovery hints are on FaultMeta, not Fault

Recovery hints are per-code guidance, not per-instance guidance. Moving `RecoveryHint` onto `Fault` would add it to every `Fault` value and all downstream consumers. Because the hint is the same for every instance of a given `FaultCode`, it belongs on `FaultMeta`, where consumers that need it (MCP `precept_proofs` / `precept_diagnostic`, preview inspector) can look it up by code without carrying it through the evaluator's output path.

### Relationship to prototype

The prototype evaluator throws `ConstraintViolationException` on rule violations and returns structured errors from `CompileFromTextResult`. There is no equivalent of `FaultCode` — failure classes are unclassified and unpaired. The `FaultCode → DiagnosticCode` chain is entirely new to the redesign.

## Open Questions / Implementation Notes

### Q1 — Evaluator result type (resolved)

**Resolved:** should anything ever construct a `Fault` (Q3), it is returned as the `EventOutcome.Faulted(Fault)` variant — a structured value inside the outcome hierarchy, never thrown. `Version.Fire` surfaces it exactly like any other outcome; callers pattern-match `Faulted` alongside `Transitioned`, `Applied`, and the rest. There is no `FaultException` control-flow path.

The options weighed before settling on the outcome-variant shape:

| Option | Shape | Trade-off |
|--------|-------|-----------|
| Outcome variant (chosen) | `EventOutcome.Faulted(Fault)` | Structured, pattern-matched at the one call site that already switches on `EventOutcome`; no hidden control flow |
| Exception | `throw FaultException(fault)` | Simple; fault is a rare path; loses structured return and adds a hidden escape path |
| Discriminated union | `Result<T, Fault>` | Explicit at every call site; C# lacks native DU syntax |
| Out parameter | `bool TryEval(out T value, out Fault? fault)` | Familiar; ugly for nested expressions |

The prototype used exceptions (`ConstraintViolationException`); the redesign does not — `Fault` is a structured value carried by `EventOutcome.Faulted`, consistent with the outcome-based model the rest of the runtime uses.

### Q2 — Fault context

If anything ever constructs a `Fault` (Q3), what context should it carry? A `Fault` names a breach of one of the engine's own invariants, so the useful context is about what the engine was doing, not about the author's data:

- **Where in the plan** the breach was detected — which execution row or opcode the evaluator had reached?
- **What the engine's own state was** — the slot index, stack depth or dispatch entry the invariant was asserted over?
- **Which operation** was running when it was detected?

The current stub carries none of this. What it should carry depends on what MCP `precept_fire` needs to return and what the preview inspector needs to display — and there is nothing to decide until Q3 settles, because if nothing constructs a `Fault` there is no context to fill in.

### Q3 — How far the evaluator asserts about its own state

The half of this question that was about the author's data is closed: a fault-prone operation is proven safe or the definition is refused, so there is no evaluation-time fault check and nothing to weigh against an assertion.

What remains is about the engine's own state rather than the author's. A slot-index range assertion, or a stack-depth assertion, is not a check about a divisor or a bound — it is the evaluator checking that it has not been handed a model that violates its own construction invariants. Two accounts of that already exist and they do not agree: `precept-builder.md` § Builder Invariant Violations and `evaluator.md` § No Unclassified Exceptions both say a corrupted model is an unrecoverable exception, while `compiler-and-runtime-design.md` names the evaluator as where an engine-invariant breach would surface as a `Fault`. Whether the evaluator asserts, faults, or does neither — and therefore whether anything ever constructs a `Fault` — is undecided.

This is a design decision rather than something a build settles: the two accounts above have to be reconciled before either can be implemented, and it belongs with the work that builds the evaluator.

## Deliberate Exclusions

**`FaultException` type** — deliberately excluded. Faults are returned as the `EventOutcome.Faulted(Fault)` variant, never thrown (Q1, resolved), so no fault-carrying exception type exists in the design.

**`Fault.SourceSpan`** — excluded from the current `Fault` struct. At runtime, there is no source text in scope; the expression being evaluated is an in-memory execution plan, not a construct manifest. If expression context is needed it will be a different field shape (see Q2).

**Non-fatal fault severity** — all fault classes are currently `FaultSeverity.Fatal`. Informational or warning-level fault classes are excluded until a concrete consumer use case emerges. `FaultSeverity` is kept as an enum (not a bool) to leave this open without a breaking change.

**`DiagnosticStage` analog** — the fault system has no pipeline stage axis. The evaluator is a single-stage executor; stage classification would always be the same value and adds no information.

**Fault suppression** — the diagnostic system suppresses downstream diagnostics when upstream pipeline stages fail. The fault system has no equivalent: a `Faulted` outcome ends the operation where it stands, and there is no multi-stage fault pipeline to suppress.

## Cross-References

| Document | Relationship |
|----------|-------------|
| [diagnostic-system.md](../compiler/diagnostic-system.md) | Mirror document — every `FaultCode` member links to a `DiagnosticCode` via `[StaticallyPreventable]`; the full enforcement chain, end-to-end example, and PREC rule rationale are documented there |
| [result-types.md](result-types.md) | Defines `EventOutcome` and `UpdateOutcome`; a fault is returned as the `EventOutcome.Faulted(Fault)` variant within this hierarchy |
| [evaluator.md](evaluator.md) | Declares `Fail(FaultCode)` and states that a compiled definition has no fault path at evaluation time |
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
