# Diagnostic System

> **Status:** Draft
> **Decisions answered:** D4 (diagnostic attribution structure)
> **Survey references:** diagnostic-and-output-design-survey, proof-attribution-witness-design-survey
> **Prototype reference:** `docs/CatalogInfrastructureDesign.md` (3-tier catalog), `Diagnostics.cs`
> **Coherence reference:** PR #133 `docs/ArchitecturalCoherenceDesign.md`, `docs/ComponentAlignmentInventory.md`

## Overview

The Precept compiler pipeline produces diagnostics at five stages: lexing, parsing, type checking, graph analysis, and proof. This document defines:

1. The **Diagnostic** output type — what consumers see
2. The **DiagnosticCode** enum — the closed registry of all diagnostic rules
3. **Diagnostics** — the exhaustive switch that maps codes to metadata
4. The **FaultCode → DiagnosticCode chain** — the structural guarantee that every runtime failure is prevented at compile time

## Design Principles

### Right-sized for Precept

Roslyn's diagnostic system was surveyed as the primary reference. It introduces DiagnosticDescriptor (rule definition), message format templates with argument arrays, effective-vs-default severity overrides, a string→string Properties bag for code-fix providers, and a Category axis orthogonal to pipeline stage. These features serve a general-purpose language with thousands of diagnostic codes, third-party analyzers, localization, and per-line severity configuration. Precept has none of these. A single `.precept` file is typically under 200 lines with 50–100 total diagnostic codes. The system should be as simple as the problem it solves.

### The enumeration surface earns its keep

The prototype already has a diagnostic catalog (`DiagnosticCatalog` + `LanguageConstraint`). The initial draft of this document rejected it as unnecessary Roslyn-ism — the pipeline could just construct flat diagnostics with pre-formatted messages.

That analysis was wrong. The enumeration surface exists because **consumer surfaces need to enumerate all diagnostic rules without compiling anything**:

- **MCP `precept_language`** iterates all rules to tell agents every rule the language enforces (edge E22 in the alignment inventory — zero-drift by construction).
- **LS diagnostic codes** derive from catalog entries (edge E23).
- **Drift tests** verify every registered rule has a triggering test (edge E49) and every `SYNC:CONSTRAINT` comment references a valid catalog entry (edges E19–E21).

The `ArchitecturalCoherenceDesign.md` (PR #133) frames this as part of the broader architectural coherence problem: authoritative registries → pipeline stages → consumer surfaces. The diagnostic catalog is one of seven authoritative registries. See that document for the full 50-edge alignment map and enforcement hierarchy.

### Prevention applies inward

The product prevents invalid entity configurations. The codebase must prevent invalid component configurations. The `FaultCode → DiagnosticCode` chain (described below) ensures that every way the evaluator can fail at runtime has a corresponding compile-time diagnostic — verified by the C# compiler's switch exhaustiveness and a single Roslyn rule. This is Precept's philosophy applied recursively to its own infrastructure.

## The Diagnostic Output Type

```csharp
public readonly record struct Diagnostic(
    Severity        Severity,
    DiagnosticStage Stage,
    string          Code,       // "UndeclaredField" — derived from enum member name
    string          Message,    // pre-formatted, final English string
    SourceSpan      Span
);
```

Five fields. The message is a pre-formatted string — no templates at the output boundary, no argument arrays, no deferred formatting. Consumers never see the registry.

`Diagnostic` is a `readonly record struct` — small, value-typed, zero-allocation-friendly in immutable arrays.

### Source Spans

Diagnostics carry a `SourceSpan` — the same unified location type used on every AST node. This means downstream stages can emit located diagnostics directly from `node.Span` without needing the source text. See the parser doc for the full `SourceSpan` definition.

1-based line/column to align with LSP `Position` (which is 0-based — conversion happens at the LS layer, not in the diagnostic). Source spans are required on every diagnostic — every diagnostic must point somewhere in the source.

### Diagnostic Stages

```csharp
public enum DiagnosticStage
{
    Lex,
    Parse,
    Type,
    Graph,
    Proof
}
```

One value per pipeline stage. The lexer has its own stage — unterminated strings, invalid characters, and unrecognized tokens are `Lex` diagnostics, distinct from `Parse` (structural syntax) errors. This matches the actual pipeline shape: the lexer is a separate stage that can fail independently.

### Severity

```csharp
public enum Severity
{
    Info,
    Warning,
    Error
}
```

Three levels. No `Hidden` (unlike Roslyn) — Precept's diagnostic surface is small enough that every diagnostic is meaningful. No `Fatal` — the pipeline never halts; it always runs to completion (Model A).

## DiagnosticCode Enum — The Registry

```csharp
public enum DiagnosticCode
{
    // ── Lex ──────────────────────────────────────────────
    UnterminatedStringLiteral,
    InvalidCharacter,

    // ── Parse ────────────────────────────────────────────
    ExpectedToken,
    UnexpectedKeyword,

    // ── Type ─────────────────────────────────────────────
    UndeclaredField,
    TypeMismatch,
    NullInNonNullableContext,
    InvalidMemberAccess,
    FunctionArityMismatch,
    FunctionArgConstraintViolation,

    // ── Type: Business-domain ────────────────────────────
    QualifierMismatch,
    DimensionCategoryMismatch,
    CrossCurrencyArithmetic,
    CrossDimensionArithmetic,
    DenominatorUnitMismatch,
    DurationDenominatorMismatch,
    CompoundPeriodDenominator,
    MutuallyExclusiveQualifiers,
    InvalidUnitString,
    InvalidCurrencyCode,
    InvalidDimensionString,
    MaxPlacesExceeded,

    // ── Type: Temporal ────────────────────────────────────
    InvalidDateValue,
    InvalidDateFormat,
    InvalidTimeValue,
    InvalidInstantFormat,
    InvalidTimezoneId,
    UnqualifiedPeriodArithmetic,
    MissingTemporalUnit,
    FractionalUnitValue,

    // ── Type: Collection safety ───────────────────────────
    UnguardedCollectionAccess,
    UnguardedCollectionMutation,

    // ── Graph ────────────────────────────────────────────
    UnreachableState,
    UnhandledEvent,

    // ── Proof ────────────────────────────────────────────
    UnsatisfiableGuard,
    DivisionByZero,
    SqrtOfNegative,
}
```

The enum **is** the complete set of diagnostic rules. It is a closed set — you cannot produce a diagnostic that is not a member. Adding a member without completing the catalog chain causes a build failure (see below).

Member names are descriptive (`UndeclaredField`, not `PRECEPT201`). The string code emitted to LSP and MCP is derived from the member name via `nameof()` — no numbering scheme, no stage-bucketed ranges. The `DiagnosticStage` field on metadata already carries the stage; encoding it again in the code string is redundant.

## Diagnostics — The Exhaustive Switch

```csharp
public sealed record DiagnosticMeta(
    string          Code,
    DiagnosticStage Stage,
    Severity        Severity,
    string          MessageTemplate
);

public static class Diagnostics
{
    public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
    {
        DiagnosticCode.UnterminatedStringLiteral      => new(nameof(DiagnosticCode.UnterminatedStringLiteral),      DiagnosticStage.Lex,   Severity.Error,   "Unterminated string literal"),
        DiagnosticCode.InvalidCharacter               => new(nameof(DiagnosticCode.InvalidCharacter),               DiagnosticStage.Lex,   Severity.Error,   "Invalid character '{0}'"),
        DiagnosticCode.ExpectedToken                  => new(nameof(DiagnosticCode.ExpectedToken),                  DiagnosticStage.Parse, Severity.Error,   "Expected '{0}' but found '{1}'"),
        DiagnosticCode.UnexpectedKeyword              => new(nameof(DiagnosticCode.UnexpectedKeyword),              DiagnosticStage.Parse, Severity.Error,   "Unexpected keyword '{0}' inside {1} block"),
        DiagnosticCode.UndeclaredField                => new(nameof(DiagnosticCode.UndeclaredField),                DiagnosticStage.Type,  Severity.Error,   "Field '{0}' is not declared"),
        DiagnosticCode.TypeMismatch                   => new(nameof(DiagnosticCode.TypeMismatch),                   DiagnosticStage.Type,  Severity.Error,   "Type mismatch: expected '{0}', got '{1}'"),
        DiagnosticCode.NullInNonNullableContext       => new(nameof(DiagnosticCode.NullInNonNullableContext),       DiagnosticStage.Type,  Severity.Error,   "Null value used where non-nullable '{0}' is required"),
        DiagnosticCode.InvalidMemberAccess            => new(nameof(DiagnosticCode.InvalidMemberAccess),            DiagnosticStage.Type,  Severity.Error,   "Member accessor '{0}' is not supported on type '{1}'"),
        DiagnosticCode.FunctionArityMismatch          => new(nameof(DiagnosticCode.FunctionArityMismatch),          DiagnosticStage.Type,  Severity.Error,   "Function '{0}' expects {1} arguments, got {2}"),
        DiagnosticCode.FunctionArgConstraintViolation => new(nameof(DiagnosticCode.FunctionArgConstraintViolation), DiagnosticStage.Type,  Severity.Error,   "Argument {0} to '{1}' violates constraint: {2}"),

        // Business-domain type diagnostics
        DiagnosticCode.QualifierMismatch              => new(nameof(DiagnosticCode.QualifierMismatch),              DiagnosticStage.Type,  Severity.Error,   "Value does not match the '{0}' qualifier on field '{1}'"),
        DiagnosticCode.DimensionCategoryMismatch      => new(nameof(DiagnosticCode.DimensionCategoryMismatch),      DiagnosticStage.Type,  Severity.Error,   "Dimension '{0}' does not match the declared category '{1}' on field '{2}'"),
        DiagnosticCode.CrossCurrencyArithmetic        => new(nameof(DiagnosticCode.CrossCurrencyArithmetic),        DiagnosticStage.Type,  Severity.Error,   "Cannot combine '{0}' ({1}) with '{2}' ({3}) — different currencies"),
        DiagnosticCode.CrossDimensionArithmetic       => new(nameof(DiagnosticCode.CrossDimensionArithmetic),       DiagnosticStage.Type,  Severity.Error,   "Cannot combine '{0}' ({1}) with '{2}' ({3}) — incompatible dimensions"),
        DiagnosticCode.DenominatorUnitMismatch        => new(nameof(DiagnosticCode.DenominatorUnitMismatch),        DiagnosticStage.Type,  Severity.Error,   "Denominator unit '{0}' does not match operand unit '{1}'"),
        DiagnosticCode.DurationDenominatorMismatch    => new(nameof(DiagnosticCode.DurationDenominatorMismatch),    DiagnosticStage.Type,  Severity.Error,   "Duration cannot cancel '{0}' denominator — days, weeks, months, and years have variable length"),
        DiagnosticCode.CompoundPeriodDenominator      => new(nameof(DiagnosticCode.CompoundPeriodDenominator),      DiagnosticStage.Type,  Severity.Error,   "Compound period '{0}' cannot cancel single-unit denominator '{1}' — decompose to a single basis first"),
        DiagnosticCode.MutuallyExclusiveQualifiers    => new(nameof(DiagnosticCode.MutuallyExclusiveQualifiers),    DiagnosticStage.Parse, Severity.Error,   "'in' and 'of' cannot both appear on the same field declaration"),
        DiagnosticCode.InvalidUnitString              => new(nameof(DiagnosticCode.InvalidUnitString),              DiagnosticStage.Type,  Severity.Error,   "'{0}' is not a valid unit — structural characters ('/', '*') are not allowed in an atomic unit value"),
        DiagnosticCode.InvalidCurrencyCode            => new(nameof(DiagnosticCode.InvalidCurrencyCode),            DiagnosticStage.Type,  Severity.Error,   "'{0}' is not a recognized ISO 4217 currency code"),
        DiagnosticCode.InvalidDimensionString         => new(nameof(DiagnosticCode.InvalidDimensionString),         DiagnosticStage.Type,  Severity.Error,   "'{0}' is not a recognized dimension — did you mean a dimension name like 'length' or 'mass' instead of a unit name?"),
        DiagnosticCode.MaxPlacesExceeded              => new(nameof(DiagnosticCode.MaxPlacesExceeded),              DiagnosticStage.Type,  Severity.Error,   "Value has {0} decimal places, but field '{1}' allows at most {2}"),

        // Temporal type diagnostics
        DiagnosticCode.InvalidDateValue               => new(nameof(DiagnosticCode.InvalidDateValue),               DiagnosticStage.Type,  Severity.Error,   "Invalid date: {0} does not exist"),
        DiagnosticCode.InvalidDateFormat              => new(nameof(DiagnosticCode.InvalidDateFormat),              DiagnosticStage.Type,  Severity.Error,   "Dates must be written as YYYY-MM-DD. Use '{0}'"),
        DiagnosticCode.InvalidTimeValue               => new(nameof(DiagnosticCode.InvalidTimeValue),               DiagnosticStage.Type,  Severity.Error,   "Invalid time: {0} must be 0\u201323 for hours, 0\u201359 for minutes and seconds"),
        DiagnosticCode.InvalidInstantFormat           => new(nameof(DiagnosticCode.InvalidInstantFormat),           DiagnosticStage.Type,  Severity.Error,   "Instants must end with Z to indicate UTC. Use '{0}Z'"),
        DiagnosticCode.InvalidTimezoneId              => new(nameof(DiagnosticCode.InvalidTimezoneId),              DiagnosticStage.Type,  Severity.Error,   "'{0}' is not a recognized timezone \u2014 use canonical IANA form like 'America/New_York'"),
        DiagnosticCode.UnqualifiedPeriodArithmetic    => new(nameof(DiagnosticCode.UnqualifiedPeriodArithmetic),    DiagnosticStage.Type,  Severity.Error,   "Period field '{0}' may contain {1} components \u2014 use `period of '{2}'` to constrain it"),
        DiagnosticCode.MissingTemporalUnit            => new(nameof(DiagnosticCode.MissingTemporalUnit),            DiagnosticStage.Type,  Severity.Error,   "A bare number doesn't specify a unit. Use '{0} + ''{1}''' to add {1}"),
        DiagnosticCode.FractionalUnitValue            => new(nameof(DiagnosticCode.FractionalUnitValue),            DiagnosticStage.Type,  Severity.Error,   "Unit values must be whole numbers. Use smaller units for fractions: '{0}'"),

        // Collection safety diagnostics
        DiagnosticCode.UnguardedCollectionAccess       => new(nameof(DiagnosticCode.UnguardedCollectionAccess),       DiagnosticStage.Type,  Severity.Error,   "'{0}' may be empty — guard with `if {0}.count > 0` before accessing `.{1}`"),
        DiagnosticCode.UnguardedCollectionMutation     => new(nameof(DiagnosticCode.UnguardedCollectionMutation),     DiagnosticStage.Type,  Severity.Error,   "'{0}' may be empty — guard with `if {0}.count > 0` before `{1}`"),

        DiagnosticCode.UnreachableState               => new(nameof(DiagnosticCode.UnreachableState),               DiagnosticStage.Graph, Severity.Warning, "State '{0}' is unreachable from initial state '{1}'"),
        DiagnosticCode.UnhandledEvent                 => new(nameof(DiagnosticCode.UnhandledEvent),                 DiagnosticStage.Graph, Severity.Warning, "No transition handles event '{0}' in state '{1}'"),
        DiagnosticCode.UnsatisfiableGuard             => new(nameof(DiagnosticCode.UnsatisfiableGuard),             DiagnosticStage.Proof, Severity.Warning, "Guard '{0}' on event '{1}' is provably unsatisfiable when {2}"),
        DiagnosticCode.DivisionByZero                 => new(nameof(DiagnosticCode.DivisionByZero),                 DiagnosticStage.Proof, Severity.Error,   "Division by zero: '{0}' can be zero when {1}"),
        DiagnosticCode.SqrtOfNegative                 => new(nameof(DiagnosticCode.SqrtOfNegative),                 DiagnosticStage.Proof, Severity.Error,   "sqrt() operand '{0}' can be negative when {1}"),
    };

    public static Diagnostic Create(
        DiagnosticCode code, SourceSpan span, params object?[] args)
    {
        var meta = GetMeta(code);
        return new(meta.Severity, meta.Stage, meta.Code,
            string.Format(meta.MessageTemplate, args), span);
    }

    public static IReadOnlyList<DiagnosticMeta> All { get; } =
        Enum.GetValues<DiagnosticCode>().Select(GetMeta).ToList();
}
```

### Why an exhaustive switch, not attributes + reflection

The prototype uses `[TokenCategory]`, `[TokenDescription]`, `[TokenSymbol]` attributes on `PreceptToken` with reflection-driven derivation at startup. That pattern works but has a structural weakness: **a missing attribute is a runtime failure, not a compile-time failure.** The Roslyn analyzer (PREC003) closes the gap at build time, but then you have two layers doing related work — the analyzer verifying attributes exist, and reflection reading them.

The exhaustive switch pattern uses the C# compiler directly:

- Add a `DiagnosticCode` member without a switch arm → **CS8509** (non-exhaustive switch expression). With `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` → **build fails**.
- No custom Roslyn rule needed for catalog completeness.
- No reflection. No source generator. No registration side effects.
- The switch IS the catalog. The compiler IS the enforcement.

### Pipeline stage usage

```csharp
// Type checker — field not found:
diagnostics.Add(Diagnostics.Create(DiagnosticCode.UndeclaredField, expr.Range, fieldName));
// → Diagnostic(Error, Type, "UndeclaredField", "Field 'amount' is not declared", range)

// Graph analyzer — unreachable state:
diagnostics.Add(Diagnostics.Create(DiagnosticCode.UnreachableState, stateRange, stateName, initialState));
// → Diagnostic(Warning, Graph, "UnreachableState", "State 'Archived' is unreachable from initial state 'Draft'", range)
```

### MCP enumeration (no compilation needed)

```csharp
var constraints = Diagnostics.All
    .Select(m => new ConstraintDto(m.Code, m.Stage.ToString(), m.MessageTemplate))
    .ToList();
```

## FaultCode — The Runtime-to-Compiler Chain

Precept's philosophy: no runtime errors. The evaluator must never produce a failure that the compiler could have prevented. The `FaultCode` enum makes this structurally enforceable.

### The chain

```
Evaluator fails → FaultCode member → [StaticallyPreventable] → DiagnosticCode member → pipeline stage emits it
```

If any link is missing, the build fails.

### FaultCode enum

```csharp
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
    EmptyCollectionAccess,

    [StaticallyPreventable(DiagnosticCode.UnguardedCollectionMutation)]
    EmptyCollectionMutation,
}
```

### Faults — exhaustive switch

```csharp
public sealed record FaultMeta(
    string Code,
    string MessageTemplate
);

public static class Faults
{
    public static FaultMeta GetMeta(FaultCode code) => code switch
    {
        FaultCode.DivisionByZero                 => new(nameof(FaultCode.DivisionByZero),                 "Divisor evaluated to zero"),
        FaultCode.SqrtOfNegative                 => new(nameof(FaultCode.SqrtOfNegative),                 "sqrt() operand evaluated to a negative number"),
        FaultCode.TypeMismatch                   => new(nameof(FaultCode.TypeMismatch),                   "Operator applied to incompatible type"),
        FaultCode.UndeclaredField                => new(nameof(FaultCode.UndeclaredField),                "Referenced field does not exist"),
        FaultCode.UnexpectedNull                 => new(nameof(FaultCode.UnexpectedNull),                 "Null value used in non-nullable context"),
        FaultCode.InvalidMemberAccess            => new(nameof(FaultCode.InvalidMemberAccess),            "Member accessor not supported on this type"),
        FaultCode.FunctionArityMismatch          => new(nameof(FaultCode.FunctionArityMismatch),          "Function called with wrong number of arguments"),
        FaultCode.FunctionArgConstraintViolation => new(nameof(FaultCode.FunctionArgConstraintViolation), "Function argument violates constraint"),
        FaultCode.EmptyCollectionAccess          => new(nameof(FaultCode.EmptyCollectionAccess),          "Accessor called on empty collection"),
        FaultCode.EmptyCollectionMutation        => new(nameof(FaultCode.EmptyCollectionMutation),        "Dequeue/pop called on empty collection"),
    };
}
```

### Evaluator usage

```csharp
// Every failure path uses the enum — no bare string errors:
case BinaryOperator.Divide:
    if (IsZero(right))
        return Fail(FaultCode.DivisionByZero);
    return Success(left / right);
```

### Enforcement: what catches what

| Check | Mechanism | Custom? |
|-------|-----------|---------|
| Catalog completeness — every `DiagnosticCode` has metadata | CS8509 exhaustive switch on `Diagnostics.GetMeta()` | No — C# compiler |
| Fail classification — every `Fail()` uses `FaultCode` | Roslyn **PRECEPT0001** | Yes |
| Chain completeness — every `FaultCode` has `[StaticallyPreventable]` referencing a valid `DiagnosticCode` | Roslyn **PRECEPT0002** | Yes |
| Pipeline stage bypass — every `Diagnostic` is constructed via `Diagnostics.Create()` | Roslyn **PRECEPT0003** | Yes |
| Fault catalog completeness — every `FaultCode` has metadata | CS8509 exhaustive switch on `Faults.GetMeta()` | No — C# compiler |
| Referenced `DiagnosticCode` member exists | Enum type safety | No — C# compiler |
| Fault bypass — every `Fault` is constructed via `Faults.Create()` | Roslyn **PRECEPT0004** | Yes |

### The full enforcement chain — no reflection

```
Add FaultCode.StackUnderflow
  → PRECEPT0001: Fail() must use FaultCode ← Roslyn
  → PRECEPT0002: must have [StaticallyPreventable] ← Roslyn
  → add [StaticallyPreventable(DiagnosticCode.StackUnderflow)]
  → DiagnosticCode.StackUnderflow doesn't exist ← C# compiler
  → add DiagnosticCode.StackUnderflow to the enum
  → Diagnostics.GetMeta() switch is non-exhaustive ← CS8509
  → add the switch arm with stage, severity, message template
  → Faults.GetMeta() switch is non-exhaustive ← CS8509
  → add the switch arm with description
  → build passes
```

Every step is compiler-enforced or Roslyn-enforced. One custom attribute. Two exhaustive switches. Four Roslyn rules. The chain is unbreakable without making the build fail.

### Why PRECEPT0003 and PRECEPT0004 are both needed

`Diagnostic.Code` and `Fault.CodeName` are both `string` fields on public output types. That string is derived from the enum via `nameof()` inside `Diagnostics.Create()` and `Faults.Create()`. Without enforcement, a call site can bypass the factory and pass any arbitrary string:

```csharp
// Bypasses nameof() derivation — PRECEPT0003 catches this
new Diagnostic(Severity.Error, DiagnosticStage.Type, "some-raw-string", "message", range)

// Bypasses nameof() derivation — PRECEPT0004 catches this
new Fault(FaultCode.TypeMismatch, "some-raw-string", "message")
```

The typed `FaultCode Code` field on `Fault` prevents one bypass — you can't use a raw string for the enum. But `string CodeName` remains open. PRECEPT0004 closes it by requiring all `Fault` constructions to go through `Faults.Create()`.

`Fault` is a public output type (returned to MCP `precept_fire`, preview inspector, and external consumers). The rule is justified by the same reasoning as PRECEPT0003: public output types with string identity fields must derive that string from the registry, not from freeform arguments.

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

5. CompilationResult.HasErrors == true → no Precept produced → evaluator never runs
```

## D4 — Diagnostic Attribution

### Decision: Attribution Lives in the Message

At Precept's scale, the message string is the attribution. When a diagnostic says `Field 'amount' is not declared`, the consumer knows the field is `amount`. The message template in `Diagnostics` uses `string.Format` placeholders — the pipeline stage fills them with concrete values at the diagnostic site.

This is sufficient because:

- Precept has no code-fix providers that need to programmatically extract the field name.
- The LS needs the message for display and the range for squiggly placement — it doesn't parse the message.
- MCP agents read the message as natural language.

If a future consumer needs machine-readable attribution, a single additional field can be added — motivated by a real consumer, not speculative infrastructure.

## LS Consumption

### Diagnostic Suppression

The LS suppresses downstream diagnostics when upstream stages produce errors:

```csharp
IEnumerable<Diagnostic> VisibleDiagnostics(CompilationResult result)
{
    if (result.Diagnostics.Any(d => d.Stage == DiagnosticStage.Lex && d.Severity == Severity.Error))
        return result.Diagnostics.Where(d => d.Stage == DiagnosticStage.Lex);

    if (result.Diagnostics.Any(d => d.Stage == DiagnosticStage.Parse && d.Severity == Severity.Error))
        return result.Diagnostics.Where(d => d.Stage <= DiagnosticStage.Parse);

    if (result.Diagnostics.Any(d => d.Stage == DiagnosticStage.Type && d.Severity == Severity.Error))
        return result.Diagnostics.Where(d => d.Stage <= DiagnosticStage.Type);

    return result.Diagnostics;
}
```

This prevents cascading errors from confusing users — if the file doesn't lex, showing parse/type/graph/proof errors is noise.

### LSP Mapping

| Precept | LSP |
|---------|-----|
| `Range` (1-based) | `range` (0-based — subtract 1) |
| `Severity` | `severity` (Info→3, Warning→2, Error→1) |
| `Code` | `code` |
| `Message` | `message` |

### MCP Consumption

MCP `precept_compile` serializes diagnostics to JSON. The flat `Diagnostic` fields map directly. MCP `precept_language` enumerates `Diagnostics.All` to list all rules without compiling.

## Alternatives Considered

### Roslyn DiagnosticDescriptor / Diagnostic separation

Roslyn separates rule definition (DiagnosticDescriptor) from rule instance (Diagnostic) to support third-party analyzer registration, message template localization, per-project severity overrides, and IDE-wide catalog enumeration. Rejected: none of these capabilities are needed at DSL scale.

### Attribute-driven enum + reflection

The pattern already used in the prototype for `PreceptToken` (`[TokenCategory]`, `[TokenDescription]`, `[TokenSymbol]` → reflection at startup). Works, but a missing attribute is a runtime failure. A Roslyn analyzer can close the gap at build time, but then two layers do related work. The exhaustive switch pattern uses the C# compiler directly — no reflection, no analyzer needed for completeness. One fewer thing that can go wrong.

### DiagnosticRule class with Register() side effects

A `static readonly DiagnosticRule` field per rule, registered via `Register()` into a list. The prototype's `Diagnostics` uses this pattern. Rejected: registration is a runtime side effect — if someone adds a field but forgets `Register()`, or the static initializer order changes, the registry is silently incomplete. The enum is a closed set by construction.

### Source generator for catalog derivation

Reads attributes on `DiagnosticCode` at compile time and emits the lookup code directly. Eliminates reflection and catches missing attributes as compile errors. Rejected (for now): adds build infrastructure complexity. The exhaustive switch achieves the same completeness guarantee with zero infrastructure — just plain C#. A source generator could be adopted later if the catalog grows large enough to make switch maintenance burdensome.

### ImmutableDictionary\<string, string\> Properties bag for attribution

Roslyn attaches a string→string properties bag for code-fix providers. Rejected: Precept has no code-fix providers. Attribution is in the message string.

### Effective severity vs. default severity

Roslyn allows per-instance severity overrides via `.editorconfig`. Rejected: Precept has no severity configuration mechanism.

### Per-stage diagnostic types

Rejected: consumers want a single type to iterate and publish.

### Category field orthogonal to stage

Rejected: `DiagnosticStage` is the only classification axis needed at DSL scale.

### PRECEPT-prefixed numeric codes (PRECEPTnnn)

The prototype uses `ToDiagnosticCode()` to produce `PRECEPT`-prefixed numeric strings with stage-bucketed ranges (0xx = Lex, 1xx = Parse, etc.). Rejected: the stage is already carried by `DiagnosticStage` — encoding it in the code string is redundant. Descriptive enum member names (`UndeclaredField`) are more readable than numeric codes (`PRECEPT201`) in every consumer context (LSP, MCP, logs). The string code is derived from the member name via `nameof()`, eliminating an entire numbering scheme to maintain.

## Relationship to Prototype

The prototype's diagnostic system (`DiagnosticCatalog` + `LanguageConstraint` + `ConstraintViolationException`) evolved to mitigate the N-Copy Grammar Sync problem documented in `CatalogInfrastructureDesign.md`. In a codebase without a proper lexer — where 7 components independently recognize keywords through their own regex patterns — centralizing language knowledge as data was the right move.

The new compiler pipeline (Lexer → Parser → TypeChecker → GraphAnalyzer → ProofEngine) eliminates the N-Copy problem structurally. The lexer's token stream replaces all the regex-based keyword recognition. But the enumeration surface for consumer surfaces (MCP, LS, drift tests) remains necessary. The new design retains enumeration via the `DiagnosticCode` enum + `Diagnostics.All`, while replacing the runtime-registered `LanguageConstraint` records with compiler-enforced exhaustive switches. The "catalog" architectural concept is gone — `Diagnostics` is just a static helper class.

The `FaultCode → DiagnosticCode` chain is new — it adds a structural guarantee the prototype does not have: that every evaluator failure mode is covered by a compile-time diagnostic. This is the "prevention applies inward" principle from `ArchitecturalCoherenceDesign.md`.

## Open Questions

- **D5 coupling (proof attribution schema):** If proof results require richer structured output (expression trees, interval ranges, witness values), the proof stage may need a way to link diagnostics to proof-model entries. Deferred until proof engine design.
- **Related locations:** Roslyn's `AdditionalLocations` lets a diagnostic point at multiple source ranges. Precept may need this for "field declared at line X, constraint at line Y" patterns. Deferred until concrete use cases surface.
- **Drift test: diagnostic emission coverage.** For every `DiagnosticCode` referenced by a `[StaticallyPreventable]`, verify that at least one call to `Diagnostics.Create()` with that code exists somewhere in the pipeline. This confirms the compile-time diagnostic isn't just registered — it's actually emitted.
