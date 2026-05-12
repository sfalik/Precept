# Diagnostic System

## Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Implemented |
| Source | `src/Precept/Language/Diagnostic.cs`, `src/Precept/Language/DiagnosticCode.cs`, `src/Precept/Language/Diagnostics.cs` |
| Upstream | Lexer, Parser, NameBinder, TypeChecker, GraphAnalyzer, ProofEngine |
| Downstream | Language Server, MCP (`precept_compile`, `precept_language`), drift tests |

---

## Overview

The Precept compiler pipeline produces diagnostics at six stages: lexing, parsing, name binding, type checking, graph analysis, and proof. This document defines:

1. The **Diagnostic** output type — what consumers see
2. The **DiagnosticCode** enum — the closed registry of all diagnostic rules
3. **Diagnostics** — the exhaustive switch that maps codes to metadata
4. The **FaultCode → DiagnosticCode chain** — the structural guarantee that every runtime failure is prevented at compile time

---

## Responsibilities and Boundaries

**OWNS:**
- The `Diagnostic` output type — `Severity`, `DiagnosticStage`, `Code`, `Message`, `Span`, captured `Args`, and optional `RelatedSpans`
- The `DiagnosticCode` enum — the closed, exhaustive set of all diagnostic rule identifiers
- The `Diagnostics` static class — exhaustive switch mapping every `DiagnosticCode` to `DiagnosticMeta`
- The `DiagnosticMeta` record — stage, severity, message template, category, fix hint, related codes, fault prevention link
- The `Severity` and `DiagnosticStage` enums
- The `DiagnosticCategory` enum — thematic grouping orthogonal to stage
- The `FaultCode` enum and `[StaticallyPreventable]` attribute — the runtime-to-compiler linkage
- The `Faults` static class — exhaustive switch mapping every `FaultCode` to `FaultMeta`
- The `Diagnostics.All` enumeration surface — consumed by MCP and LS without compilation

**Does NOT OWN:**
- Diagnostic emission sites — each pipeline stage (`Lexer`, `Parser`, `NameBinder`, `TypeChecker`, `GraphAnalyzer`, `ProofEngine`) owns its calls to `Diagnostics.Create()`
- LSP-level filtering, range conversion (0-based), and severity mapping — owned by the Language Server
- Downstream diagnostic suppression logic (upstream-error cascading) — owned by the Language Server
- MCP serialization format — owned by the MCP tool layer
- Fix application — Precept has no code-fix providers
- Roslyn rules PRECEPT0001–PRECEPT0026 — owned by the analyzer project (`src/Precept.Analyzers/`)

---

## Right-Sizing

Roslyn's diagnostic system was surveyed as the primary reference. It introduces DiagnosticDescriptor (rule definition), message format templates with argument arrays, effective-vs-default severity overrides, a string→string Properties bag for code-fix providers, and a Category axis orthogonal to pipeline stage. These features serve a general-purpose language with thousands of diagnostic codes, third-party analyzers, localization, and per-line severity configuration. Precept has none of these. A single `.precept` file is typically under 200 lines with 50–100 total diagnostic codes. The system should be as simple as the problem it solves.

---

## Inputs and Outputs

**Inputs:**
- Pipeline stage call sites — each stage calls `Diagnostics.Create(DiagnosticCode, SourceSpan, args...)` to produce located diagnostics
- `FaultCode` — evaluator failure paths call `Fail(FaultCode)`, linked via `[StaticallyPreventable]` to the corresponding `DiagnosticCode` member
- `DiagnosticCode` enum members — the closed registry that feeds the exhaustive switch

**Outputs:**
- `Diagnostic` value structs collected in `Compilation.Diagnostics` — consumed by the Language Server, MCP `precept_compile`, and downstream callers
- `DiagnosticMeta` records enumerated by `Diagnostics.All` — consumed by MCP `precept_language` to list all rules without compiling
- `string Code` derived from `nameof(DiagnosticCode.XYZ)` — used as the LSP `code` field and MCP rule identifier
- `Fault` output records (from the evaluator) — consumed by MCP `precept_fire` and the preview inspector when a runtime fault occurs

---

## The Diagnostic Output Type

```csharp
public readonly record struct RelatedSpan(SourceSpan Span, string Message);

public readonly record struct Diagnostic(
    Severity              Severity,
    DiagnosticStage       Stage,
    string                Code,       // "UndeclaredField" — derived from enum member name
    string                Message,    // pre-formatted, final English string
    SourceSpan            Span,
    ImmutableArray<string> Args = default)
{
    public ImmutableArray<RelatedSpan> RelatedSpans { get; init; } = ImmutableArray<RelatedSpan>.Empty;
}
```

The core shape is severity, stage, code, message, and primary span. `Args` preserves the formatted arguments captured at emission time, and `RelatedSpans` carries optional secondary locations for multi-location diagnostics without breaking existing construction sites.

`Diagnostic` is a `readonly record struct` — small, value-typed, zero-allocation-friendly in immutable arrays.

### Source Spans

Diagnostics carry a `SourceSpan` — the same unified location type used on every AST node. This means downstream stages can emit located diagnostics directly from `node.Span` without needing the source text. See the parser doc for the full `SourceSpan` definition.

1-based line/column to align with LSP `Position` (which is 0-based — conversion happens at the LS layer, not in the diagnostic). Source spans are required on every diagnostic — every diagnostic must point somewhere in the source.

### Related Spans

`RelatedSpans` exists for diagnostics that need more than one source location to explain the problem. Use it when the author needs to see a primary offending site plus supporting context elsewhere — for example, duplicate declarations (diagnostic on the later declaration, related span on the original declaration).

Keep `RelatedSpans` empty when no second concrete location exists. An undeclared reference still points at the reference site as its primary `Span`; the diagnostic message explains that no declaration was found, but there is no synthetic "missing declaration" span to attach.

Each `RelatedSpan` carries both a `SourceSpan` and a short message. This lets downstream consumers surface multi-location diagnostics without inventing their own per-consumer side tables.

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

One value per pipeline stage. The lexer has its own stage — unterminated strings, invalid characters, and unrecognized tokens are `Lex` diagnostics, distinct from `Parse` (structural syntax) errors. This matches the actual pipeline shape: the lexer is a separate stage that can fail independently. NameBinder diagnostics use `DiagnosticStage.Type` — name binding is a pipeline stage but not a diagnostic stage. Adding a `Bind` stage would break the upstream-error suppression model in the LS.

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

---

## DiagnosticCode Registry

```csharp
public enum DiagnosticCode
{
    // ── Lex (8 codes) ────────────────────────────────────
    InputTooLarge                      =   1,
    UnterminatedStringLiteral          =   2,
    UnterminatedTypedConstant          =   3,
    UnterminatedInterpolation          =   4,
    InvalidCharacter                   =   5,
    UnrecognizedStringEscape           =   6,
    UnrecognizedTypedConstantEscape    =   7,
    UnescapedBraceInLiteral            =   8,

    // ── Parse (9 codes) ──────────────────────────────────
    ExpectedToken                      =   9,
    NonAssociativeComparison           =  10,
    UnexpectedKeyword                  =  11,
    InvalidCallTarget                  =  12,
    OmitDoesNotSupportGuard            =  13,
    EventHandlerDoesNotSupportGuard    =  14,
    PreEventGuardNotAllowed            =  15,
    ExpectedOutcome                    =  16,
    AssignmentInExpressionContext      = 127,

    // ── Type (core) ──────────────────────────────────────
    UndeclaredField                    =  17,
    TypeMismatch                       =  18,
    NullInNonNullableContext           =  19,
    InvalidMemberAccess                =  20,
    FunctionArityMismatch              =  21,
    FunctionArgConstraintViolation     =  22,
    MutuallyExclusiveQualifiers        =  23,
    DuplicateFieldName                 =  24,
    DuplicateStateName                 =  25,
    DuplicateEventName                 =  26,
    DuplicateArgName                   =  27,
    UndeclaredState                    =  28,
    UndeclaredEvent                    =  29,
    UndeclaredFunction                 =  30,
    MultipleInitialStates              =  31,
    NoInitialState                     =  32,
    InvalidModifierForType             =  33,
    InvalidModifierBounds              =  34,
    InvalidModifierValue               =  35,
    DuplicateModifier                  =  36,
    RedundantModifier                  =  37,
    ComputedFieldNotWritable           =  38,
    ComputedFieldWithDefault           =  39,
    CircularComputedField              =  40,
    WritableOnEventArg                 =  41,
    ConflictingAccessModes             =  42,
    RedundantAccessMode                =  43,
    ListLiteralOutsideDefault          =  44,
    DuplicateChoiceValue               =  45,
    EmptyChoice                        =  46,
    CollectionOperationOnScalar        =  47,
    ScalarOperationOnCollection        =  48,
    IsSetOnNonOptional                 =  49,
    EventArgOutOfScope                 =  50,
    InvalidInterpolationCoercion       =  51,
    UnresolvedTypedConstant            =  52,
    InvalidTypedConstantContent        =  53,
    DefaultForwardReference            =  54,

    // ── Type (temporal) ──────────────────────────────────
    InvalidDateValue                   =  55,
    InvalidDateFormat                  =  56,
    InvalidTimeValue                   =  57,
    InvalidInstantFormat               =  58,
    InvalidTimezoneId                  =  59,
    UnqualifiedPeriodArithmetic        =  60,
    MissingTemporalUnit                =  61,
    FractionalUnitValue                =  62,

    // ── Type (collection safety) ─────────────────────────
    UnguardedCollectionAccess          =  63,
    UnguardedCollectionMutation        =  64,
    NonOrderableCollectionExtreme      =  65,
    CaseInsensitiveFieldRequiresTildeEquals = 66,

    // ── Type (business-domain) ───────────────────────────
    MaxPlacesExceeded                  =  67,
    QualifierMismatch                  =  68,
    DimensionCategoryMismatch          =  69,
    CrossCurrencyArithmetic            =  70,
    CrossDimensionArithmetic           =  71,
    DenominatorUnitMismatch            =  72,
    DurationDenominatorMismatch        =  73,
    CompoundPeriodDenominator          =  74,
    InvalidUnitString                  =  75,
    InvalidCurrencyCode                =  76,
    InvalidDimensionString             =  77,

    // ── Runtime / value safety ───────────────────────────
    NumericOverflow                    =  78,
    OutOfRange                         =  79,

    // ── Graph ────────────────────────────────────────────
    UnreachableState                   =  80,
    UnhandledEvent                     =  81,
    DeadEndState                       = 108,
    TerminalStateHasOutgoingEdges      = 109,
    IrreversibleStateHasBackEdge       = 110,
    RequiredStateDoesNotDominateTerminal = 111,
    StructuralSinkState                = 119,

    // ── Proof ────────────────────────────────────────────
    UnsatisfiableGuard                 =  82,
    DivisionByZero                     =  83,
    SqrtOfNegative                     =  84,

    // ── Type (choice) ────────────────────────────────────
    NonChoiceAssignedToChoice          =  85,
    ChoiceLiteralNotInSet              =  86,
    ChoiceArgOutsideFieldSet           =  87,
    ChoiceElementTypeMismatch          =  88,
    ChoiceRankConflict                 =  89,
    ChoiceMissingElementType           =  90,

    // ── Type (lifecycle validation) ──────────────────────
    AmbiguousTypedConstant             =  91,
    EventHandlerInStatefulPrecept      =  92,
    RequiredFieldsNeedInitialEvent     =  93,
    InitialEventMissingAssignments     =  94,

    // ── Type (CI enforcement) ────────────────────────────
    CaseInsensitiveFieldRequiresTildeNotEquals  =  95,
    CaseInsensitiveValueInCaseSensitiveContains =  96,
    CaseInsensitiveFieldRequiresTildeStartsWith =  97,
    CaseInsensitiveFieldRequiresTildeEndsWith   =  98,

    // ── Type (collection safety — additional) ────────────
    KeyPresenceSafety               =  99,
    IndexBoundsGuard                = 100,
    KeyUniquenessGuard              = 101,
    InvalidQuantifierTarget         = 102,
    BindingShadowsField             = 103,
    MissingOrderingKey              = 104,
    CollectionInnerTypeError        = 105,
    QuantifierPredicateNotBoolean   = 106,

    // ── NameBinder ───────────────────────────────────────
    UndeclaredArg                   = 107,
}
```

**115 total diagnostic codes** across 5 diagnostic stages: 8 Lex, 11 Parse, 83 Type (including NameBinder codes that use `DiagnosticStage.Type`), 6 Graph, 7 Proof. Note: `AmbiguousDispatch` from the original proof-engine design was replaced by richer per-domain diagnostics during TypeChecker implementation.

The enum **is** the complete set of diagnostic rules. It is a closed set — you cannot produce a diagnostic that is not a member. Adding a member without completing the catalog chain causes a build failure (see the FaultCode → DiagnosticCode Chain section below).

Member names are descriptive (`UndeclaredField`, not `PRECEPT201`). The string code emitted to LSP and MCP is derived from the member name via `nameof()` — no numbering scheme, no stage-bucketed ranges. The `DiagnosticStage` field on metadata already carries the stage; encoding it again in the code string is redundant.

**Code 66 — `~string` operator reassignment.** `CaseInsensitiveStringOnNonCollection` (ordinal 66) was reserved in anticipation of scalar `~string` but was never emitted. When scalar `~string` ships, ordinal 66 is reassigned to `CaseInsensitiveFieldRequiresTildeEquals`. The numeric value is retained; no ordinals shift. Existing source references to `DiagnosticCode.CaseInsensitiveStringOnNonCollection` will not compile — update them to `DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals`.

---

## DiagnosticMeta and the Exhaustive Switch

```csharp
public sealed record DiagnosticMeta(
    string              Code,
    DiagnosticStage     Stage,
    Severity            Severity,
    string              MessageTemplate,
    DiagnosticCategory  Category,
    DiagnosticCode[]?   RelatedCodes      = null,
    string?             FixHint           = null,
    FaultCode?          PreventsFault     = null,
    SuggestionSource[]? SuggestionSources = null
);

public static class Diagnostics
{
    public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
    {
        // 115 arms — one per DiagnosticCode member.
        // Each arm maps the code to its stage, severity, message template, category,
        // and optional related codes, fix hints, fault prevention links, and suggestion sources.
        // Representative examples:
        DiagnosticCode.InputTooLarge                 => new(..., DiagnosticCategory.Safety),
        DiagnosticCode.UnterminatedStringLiteral     => new(..., DiagnosticCategory.Structure),
        DiagnosticCode.UndeclaredField               => new(..., DiagnosticCategory.Naming, SuggestionSources: [SuggestionSource.UserFields]),
        DiagnosticCode.UnreachableState              => new(..., DiagnosticCategory.Safety),
        DiagnosticCode.DivisionByZero                => new(..., DiagnosticCategory.Proof, PreventsFault: FaultCode.DivisionByZero),
        // ... (all 115 arms present in source)
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

`DiagnosticCategory` is a thematic grouping orthogonal to pipeline stage:

```csharp
public enum DiagnosticCategory
{
    Naming         = 1,  // Duplicate names, undeclared references
    TypeSystem     = 2,  // Type mismatches, invalid operations
    Temporal       = 3,  // Date/time validation
    BusinessDomain = 4,  // Qualifier, currency, dimension, unit validation
    Structure      = 5,  // Syntax, unterminated literals, expected tokens
    Safety         = 6,  // Collection safety, input limits, CI enforcement
    Proof          = 7,  // Guard satisfiability, division by zero, ambiguity
}
```

`SuggestionSource` identifies the symbol namespace for "did you mean?" suggestions:

```csharp
public enum SuggestionSource
{
    UserFields      = 1,
    UserStates      = 2,
    UserEvents      = 3,
    FunctionCatalog = 4,
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

---

## FaultCode → DiagnosticCode Chain

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
    DivisionByZero             =  1,

    [StaticallyPreventable(DiagnosticCode.SqrtOfNegative)]
    SqrtOfNegative             =  2,

    [StaticallyPreventable(DiagnosticCode.TypeMismatch)]
    TypeMismatch               =  3,

    [StaticallyPreventable(DiagnosticCode.UndeclaredField)]
    UndeclaredField            =  4,

    [StaticallyPreventable(DiagnosticCode.NullInNonNullableContext)]
    UnexpectedNull             =  5,

    [StaticallyPreventable(DiagnosticCode.InvalidMemberAccess)]
    InvalidMemberAccess        =  6,

    [StaticallyPreventable(DiagnosticCode.FunctionArityMismatch)]
    FunctionArityMismatch      =  7,

    [StaticallyPreventable(DiagnosticCode.FunctionArgConstraintViolation)]
    FunctionArgConstraintViolation =  8,

    [StaticallyPreventable(DiagnosticCode.UnguardedCollectionAccess)]
    CollectionEmptyOnAccess    =  9,

    [StaticallyPreventable(DiagnosticCode.UnguardedCollectionMutation)]
    CollectionEmptyOnMutation  = 10,

    [StaticallyPreventable(DiagnosticCode.QualifierMismatch)]
    QualifierMismatch          = 11,

    [StaticallyPreventable(DiagnosticCode.NumericOverflow)]
    NumericOverflow            = 12,

    [StaticallyPreventable(DiagnosticCode.OutOfRange)]
    OutOfRange                 = 13,
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
        FaultCode.CollectionEmptyOnAccess        => new(nameof(FaultCode.CollectionEmptyOnAccess),        "Accessor called on empty collection"),
        FaultCode.CollectionEmptyOnMutation      => new(nameof(FaultCode.CollectionEmptyOnMutation),      "Dequeue/pop called on empty collection"),
        FaultCode.QualifierMismatch              => new(nameof(FaultCode.QualifierMismatch),              "Value does not match required qualifier"),
        FaultCode.NumericOverflow                => new(nameof(FaultCode.NumericOverflow),                "Numeric operation overflowed"),
        FaultCode.OutOfRange                     => new(nameof(FaultCode.OutOfRange),                     "Value is out of the allowed range"),
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
| Catalog cross-reference integrity | Roslyn **PRECEPT0005–0017** (per-catalog cross-ref analyzers) | Yes |
| Semantic enum zero-slot protection | Roslyn **PRECEPT0018** | Yes |
| Pipeline coverage exhaustiveness — every catalog member handled | Roslyn **PRECEPT0019** | Yes |
| Operator token/precedence collision detection | Roslyn **PRECEPT0020** | Yes |
| Token text uniqueness enforcement | Roslyn **PRECEPT0021** | Yes |
| Operator inline-token validation | Roslyn **PRECEPT0022** | Yes |
| Operator DU shape invariants | Roslyn **PRECEPT0023** | Yes |
| Anti-mirroring enforcement — no parallel copies of catalog data | Roslyn **PRECEPT0024** | Yes |

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

Every step is compiler-enforced or Roslyn-enforced. One custom attribute. Two exhaustive switches. 24 Roslyn rules (PRECEPT0001–PRECEPT0024). The chain is unbreakable without making the build fail.

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

5. Compilation.HasErrors == true → no Precept produced → evaluator never runs
```

---

## Language Server Consumption

### Diagnostic Suppression

The LS suppresses downstream diagnostics when upstream stages produce errors:

```csharp
IEnumerable<Diagnostic> VisibleDiagnostics(Compilation result)
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

---

## Design Rationale and Decisions

### The enumeration surface earns its keep

The prototype already has a diagnostic catalog (`DiagnosticCatalog` + `LanguageConstraint`). The initial draft of this document rejected it as unnecessary Roslyn-ism — the pipeline could just construct flat diagnostics with pre-formatted messages.

That analysis was wrong. The enumeration surface exists because **consumer surfaces need to enumerate all diagnostic rules without compiling anything**:

- **MCP `precept_language`** iterates all rules to tell agents every rule the language enforces (edge E22 in the alignment inventory — zero-drift by construction).
- **LS diagnostic codes** derive from catalog entries (edge E23).
- **Drift tests** verify every registered rule has a triggering test (edge E49) and every `SYNC:CONSTRAINT` comment references a valid catalog entry (edges E19–E21).

The `ArchitecturalCoherenceDesign.md` (PR #133) frames this as part of the broader architectural coherence problem: authoritative registries → pipeline stages → consumer surfaces. The diagnostic catalog is one of seven authoritative registries. See that document for the full 50-edge alignment map and enforcement hierarchy.

### Prevention applies inward

The product prevents invalid entity configurations. The codebase must prevent invalid component configurations. The `FaultCode → DiagnosticCode` chain ensures that every way the evaluator can fail at runtime has a corresponding compile-time diagnostic — verified by the C# compiler's switch exhaustiveness and a single Roslyn rule. This is Precept's philosophy applied recursively to its own infrastructure.

### D4 — Diagnostic Attribution

At Precept's scale, the message string is the attribution. When a diagnostic says `Field 'amount' is not declared`, the consumer knows the field is `amount`. The message template in `Diagnostics` uses `string.Format` placeholders — the pipeline stage fills them with concrete values at the diagnostic site.

This is sufficient because:

- Precept has no code-fix providers that need to programmatically extract the field name.
- The LS needs the message for display and the range for squiggly placement — it doesn't parse the message.
- MCP agents read the message as natural language.

If a future consumer needs machine-readable attribution, a single additional field can be added — motivated by a real consumer, not speculative infrastructure.

### Relationship to Prototype

The prototype's diagnostic system (`DiagnosticCatalog` + `LanguageConstraint` + `ConstraintViolationException`) evolved to mitigate the N-Copy Grammar Sync problem documented in `CatalogInfrastructureDesign.md`. In a codebase without a proper lexer — where 7 components independently recognize keywords through their own regex patterns — centralizing language knowledge as data was the right move.

The new compiler pipeline (Lexer → Parser → NameBinder → TypeChecker → GraphAnalyzer → ProofEngine) eliminates the N-Copy problem structurally. The lexer's token stream replaces all the regex-based keyword recognition. But the enumeration surface for consumer surfaces (MCP, LS, drift tests) remains necessary. The new design retains enumeration via the `DiagnosticCode` enum + `Diagnostics.All`, while replacing the runtime-registered `LanguageConstraint` records with compiler-enforced exhaustive switches. The "catalog" architectural concept is gone — `Diagnostics` is just a static helper class.

The `FaultCode → DiagnosticCode` chain is new — it adds a structural guarantee the prototype does not have: that every evaluator failure mode is covered by a compile-time diagnostic. This is the "prevention applies inward" principle from `ArchitecturalCoherenceDesign.md`.

---

## Open Questions / Implementation Notes

- **D5 coupling (proof attribution schema):** If proof results require richer structured output (expression trees, interval ranges, witness values), the proof stage may need a way to link diagnostics to proof-model entries. Deferred until proof engine design.
> **✅ Resolved (CC#20) — Diagnostic Related Spans**
> `Diagnostic` carries `ImmutableArray<RelatedSpan> RelatedSpans { get; init; } = ImmutableArray<RelatedSpan>.Empty;`. The additive init-only property keeps every existing `Diagnostics.Create(...)` call site compiling unchanged while giving pipeline stages a first-class place to attach secondary source locations and per-location messages.
> *Resolved: 2026-05-06 — CC#20*
- **Drift test: diagnostic emission coverage.** For every `DiagnosticCode` referenced by a `[StaticallyPreventable]`, verify that at least one call to `Diagnostics.Create()` with that code exists somewhere in the pipeline. This confirms the compile-time diagnostic isn't just registered — it's actually emitted.

---

## Deliberate Exclusions

- **Roslyn DiagnosticDescriptor / Diagnostic separation:** Roslyn separates rule definition (DiagnosticDescriptor) from rule instance (Diagnostic) to support third-party analyzer registration, message template localization, per-project severity overrides, and IDE-wide catalog enumeration. Rejected: none of these capabilities are needed at DSL scale.

- **Attribute-driven enum + reflection:** The pattern used in the prototype for `PreceptToken` (`[TokenCategory]`, `[TokenDescription]`, `[TokenSymbol]` → reflection at startup). Works, but a missing attribute is a runtime failure. A Roslyn analyzer can close the gap at build time, but then two layers do related work. The exhaustive switch pattern uses the C# compiler directly — no reflection, no analyzer needed for completeness. One fewer thing that can go wrong.

- **DiagnosticRule class with Register() side effects:** A `static readonly DiagnosticRule` field per rule, registered via `Register()` into a list. The prototype's `Diagnostics` uses this pattern. Rejected: registration is a runtime side effect — if someone adds a field but forgets `Register()`, or the static initializer order changes, the registry is silently incomplete. The enum is a closed set by construction.

- **Source generator for catalog derivation:** Reads attributes on `DiagnosticCode` at compile time and emits the lookup code directly. Eliminates reflection and catches missing attributes as compile errors. Rejected (for now): adds build infrastructure complexity. The exhaustive switch achieves the same completeness guarantee with zero infrastructure — just plain C#. A source generator could be adopted later if the catalog grows large enough to make switch maintenance burdensome.

- **ImmutableDictionary\<string, string\> Properties bag for attribution:** Roslyn attaches a string→string properties bag for code-fix providers. Rejected: Precept has no code-fix providers. Attribution is in the message string.

- **Effective severity vs. default severity:** Roslyn allows per-instance severity overrides via `.editorconfig`. Rejected: Precept has no severity configuration mechanism.

- **Per-stage diagnostic types:** Rejected: consumers want a single type to iterate and publish.

- **Category field orthogonal to stage:** Rejected: `DiagnosticStage` is the only classification axis needed at DSL scale.

- **PRECEPT-prefixed numeric codes (PRECEPTnnn):** The prototype uses `ToDiagnosticCode()` to produce `PRECEPT`-prefixed numeric strings with stage-bucketed ranges (0xx = Lex, 1xx = Parse, etc.). Rejected: the stage is already carried by `DiagnosticStage` — encoding it in the code string is redundant. Descriptive enum member names (`UndeclaredField`) are more readable than numeric codes (`PRECEPT201`) in every consumer context (LSP, MCP, logs). The string code is derived from the member name via `nameof()`, eliminating an entire numbering scheme to maintain.

---

## Cross-References

| Topic | Document |
|---|---|
| Architectural coherence / 50-edge alignment map | `docs/ArchitecturalCoherenceDesign.md` (PR #133) |
| Component alignment inventory (edges E19–E23, E49) | `docs/ComponentAlignmentInventory.md` |
| 3-tier catalog prototype context | `docs/CatalogInfrastructureDesign.md` |
| Decisions answered | D4 (diagnostic attribution structure) |
| Survey references | `diagnostic-and-output-design-survey`, `proof-attribution-witness-design-survey` |
| Lexer (Lex-stage diagnostics) | `docs/compiler/lexer.md` |
| Parser (Parse-stage diagnostics) | `docs/compiler/parser.md` |
| Type checker (Type-stage diagnostics) | `docs/compiler/type-checker.md` |
| Graph analyzer (Graph-stage diagnostics) | `docs/compiler/graph-analyzer.md` |
| Proof engine (Proof-stage diagnostics) | `docs/compiler/proof-engine.md` |
| LS diagnostic surfacing | `docs/compiler/tooling-surface.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `src/Precept/Language/Diagnostic.cs` | `Diagnostic` record struct, `DiagnosticStage`, `Severity`, `DiagnosticCategory` enums |
| `src/Precept/Language/DiagnosticCode.cs` | `DiagnosticCode` enum — the closed registry of all diagnostic rule identifiers |
| `src/Precept/Language/Diagnostics.cs` | `DiagnosticMeta` record, `Diagnostics` static class (exhaustive switch + `All` enumeration), `FaultMeta`, `Faults` |