# Catalog System

> **Status:** Draft — updated 2026-04-25 after full team review (10-item metadata-driven design review, owner sign-off)
> **Implemented in:** `src/Precept/` — all 10 catalogs implemented
> **Related:** `docs/compiler/diagnostic-system.md`, `docs/runtime/fault-system.md`, `docs/compiler-and-runtime-design.md`

## Overview

The catalog system is the **authoritative machine-readable definition of the Precept language.** Twelve catalogs — ten describing what the language IS, two describing how it reports failures — form a closed, compiler-enforced registry. This document defines the catalog pattern, the twelve-catalog inventory, their shapes, cross-catalog derivation relationships, and future opportunities.

## Vision: Metadata for the Entire Language

Every aspect of Precept — its keywords, types, functions, operators, operations, modifiers, actions, grammar forms, constraints, proof requirements, diagnostics, and faults — is defined as structured metadata in a static, compiler-enforced catalog. Twelve catalogs cover the complete language surface. Their union IS the language specification in machine-readable form.

Every consumer reads from these catalogs:

| Consumer | What it reads |
|----------|---------------|
| MCP `precept_language` | All keywords, types, operators, operations, functions, constraints, grammar forms |
| TextMate grammar | Token keyword alternations, type name alternations, construct slot patterns |
| LS completions | Types, functions, modifiers, actions — context-dependent |
| LS hover | Type documentation, function signatures, operator descriptions |
| LS semantic tokens | Token categories |
| Type checker | Modifier applicability, function signatures, operation legality |
| AI grounding | All 12 catalogs — complete language knowledge |
| Reference docs | All 8 language definition catalogs |

No consumer maintains its own parallel copy. Adding a language feature to an enum is the single atomic act that propagates it to every surface. The compiler refuses to build if any member is missing metadata.

## Completeness Principle

> If something is part of the Precept language, it gets cataloged.

The test: **if I enumerated every catalog's `All` property, would I have a complete description of Precept?** The catalogs needed are those whose union covers the entire language surface.

Twelve catalogs in two groups.

**Language Definition (what the language IS):**

| # | Catalog | What it covers |
|---|---------|----------------|
| 1 | **Tokens** | Lexical vocabulary |
| 2 | **Types** | Type system families |
| 3 | **Functions** | Built-in function library |
| 4 | **Operators** | Operator symbols — precedence, associativity, arity |
| 5 | **Operations** | Typed operator combinations — what (op, lhs, rhs) triples are legal |
| 6 | **Modifiers** | Declaration-attached modifiers — field constraints, state lifecycle, event modifiers, access modes, anchors (DU with 5 subtypes) |
| 7 | **Actions** | State-machine action verbs |
| 8 | **Constructs** | Grammar forms / declaration shapes |
| 9 | **Constraints** | Constraint declaration forms — invariant, state-anchored, event precondition (DU as identity) |
| 10 | **ProofRequirements** | Proof obligation kinds — numeric, presence, dimension, modifier, qualifier compatibility (DU as identity) |

**Failure Modes (how it tells you what's wrong):**

| # | Catalog | What it covers |
|---|---------|----------------|
| 11 | **Diagnostics** | Compile-time rules |
| 12 | **Faults** | Runtime failure modes |

If a thirteenth aspect of the language emerges that isn't covered by these twelve, it needs a catalog. The system is complete when the catalogs are.

### Enums that remain bare

Not every enum becomes a catalog. Supporting enums stay bare:

| Enum | Why bare |
|------|----------|
| `DiagnosticStage` | 5 values, classification axis for diagnostics, no per-member metadata |
| `Severity` | 3 values, no per-member metadata |
| `TokenCategory` | ~17 values. Grouping key — consumers iterate tokens grouped by category, not categories themselves |

These are internal classification axes. They organize catalog members or AST nodes but don't independently describe the language surface.

**Previously bare, now absorbed by the Modifiers DU:** `StateModifierKind`, `AccessMode`, `EnsureAnchor`, `StateActionAnchor` — these are first-class language surface members as `StateModifierMeta`, `AccessModifierMeta`, and `AnchorModifierMeta` subtypes in `Modifiers.All`.

## Architectural Identity: Metadata-Driven

Precept's compiler and runtime follow a **metadata-driven architecture.** Domain knowledge is declared as structured metadata in catalogs. Pipeline stages are generic machinery that reads it.

This inverts the traditional compiler model:

| | Traditional (Roslyn, GCC, TypeScript) | Precept |
|---|---|---|
| Where domain knowledge lives | Scattered across pipeline stage implementations | Declared in metadata catalogs |
| What a pipeline stage is | A domain-expert that knows the language | Generic machinery that reads metadata |
| How you add a language feature | Touch dozens of files across the pipeline | Add an enum member, fill the exhaustive switch — propagation is automatic |
| What the compiler refuses to build | Code that doesn't compile | Code with incomplete metadata (CS8509 on every exhaustive switch) |
| What tests verify | Implementation behavior | Metadata completeness and correctness |
| What consumers read | Their own parallel copies | The single source of truth |

The twelve catalogs are expressions of this principle — not the principle itself. The principle is: **if something is domain knowledge, it is metadata; if it is metadata, it has a declared shape; if shapes vary by kind, the shape is a discriminated union.** Pipeline stages, tooling, and consumers derive from the metadata — they never maintain parallel copies or encode domain knowledge in their own logic.

### The decision framework

When evaluating whether something belongs in a catalog:

1. **Is it language surface?** Does it appear in `.precept` files, carry semantics that consumers need, or represent a concept that would appear in a complete description of the Precept language?
   - No → bare enum, internal classification axis (e.g., `DiagnosticStage`, `Severity`)
   - Yes → it gets cataloged. Continue:
2. **Do all members share the same metadata shape?** → flat `sealed record`
3. **Do members have varying metadata by kind?** (e.g., field modifiers need `ApplicableTo` but state modifiers need `AllowsOutgoing`) → DU: `abstract record` base + `sealed` subtypes, each carrying exactly its consumers' metadata
4. **No per-member metadata beyond identity?** → still catalog, minimal record `(Kind, Keyword, Description)`

**Anti-pattern: "small enum = bare."** Size is irrelevant. `AccessMode` has 3 values but each has distinct behavioral semantics (`IsPresent`, `IsWritable`) that consumers need. The question is never "how many members?" — it's "do consumers hardcode per-member knowledge that should be metadata?"

**Anti-pattern: "flat record with inapplicable fields."** If a flat metadata record has fields that are meaningless for some members, that's a signal for a DU — not a signal to reject cataloging. The DU ensures each subtype carries exactly the fields its consumers need.

### Enforcement

The exhaustive switch is the enforcement — the C# compiler refuses to build if any member is missing metadata (CS8509). The `All` property is the enumeration surface — MCP and LS iterate it rather than maintaining their own lists. The `Create()` factory is the derivation path — pipeline stages produce output values from the catalog rather than constructing them ad hoc.

### Derive, never duplicate

The twelve catalogs cover vocabulary, types, functions, operators, operations, modifiers, actions, grammar constructs, constraints, proof requirements, compile-time rules, and runtime failure modes. Their union is the language. Every downstream artifact — grammar, completions, hover, MCP output, documentation — derives from catalog metadata. No consumer maintains a parallel copy. Adding a language feature to an enum is the single atomic act that propagates it to every surface.

## Pattern Definition

A catalog has four parts:

### 1. Kind enum — the closed set

```csharp
public enum TokenKind
{
    // ── Keywords: Declaration ───────────────────────────
    Precept,
    Field,
    State,
    ...
}
```

A plain C# `enum`. One member per distinct entry. No attributes — metadata lives in the switch, not on the enum. Grouped by section with comment headers for readability.

### 2. Meta record — what consumers need to know

```csharp
public sealed record TokenMeta(
    TokenKind                      Kind,
    string?                        Text,
    IReadOnlyList<TokenCategory>   Categories,
    string                         Description
);
```

`TokenMeta` carries no cross-references to other catalogs. The bridge direction is **unidirectional upward**: downstream catalogs (Types, Operators, Modifiers, Actions) point up to Tokens via `TokenMeta Token` object references. Consumers that need the reverse direction (token → catalog entry) use **derived frozen indexes** built at startup:

```csharp
// Derived at startup from the catalog that owns the relationship
FrozenDictionary<TokenKind, TypeMeta>     TypesByToken     = Types.All.ToFrozenDictionary(t => t.Token.Kind);
FrozenDictionary<TokenKind, OperatorMeta> OperatorsByToken = Operators.All.ToFrozenDictionary(o => o.Token.Kind);
```

This avoids cross-ref fields on `TokenMeta` that would be inapplicable for most members (only ~25 of 90+ tokens are type keywords, ~16 are operators). It also avoids the dual-use problem — `Set` is both an action and a type, `Min`/`Max` are both modifiers and functions — which would require either multiple nullable fields or a DU wrapper. Derived indexes handle dual-use naturally: each catalog builds its own index from its own `All` property.

The LS never needs token → catalog entry lookups — it works from AST nodes and resolved symbols, which already carry the typed `Kind`. MCP iterates each catalog's `All` directly. The reverse index exists for any consumer that needs it, derived from the source of truth (the downstream catalog's `Token` field).

An immutable record holding all metadata for a single enum member. The shape varies per catalog — each Meta type carries exactly the fields its consumers need, no more. Shared fields across all catalogs:

| Field | Purpose |
|-------|---------|
| The kind enum value | Identity — which member this metadata describes |
| A string code or text | Stable string identity for serialization (MCP, LS) |
| A description or message template | Human-readable explanation |

Domain-specific fields are added as needed. `TokenMeta` has `Categories` and nullable `Text`. `DiagnosticMeta` has `Stage` and `Severity`. `FaultMeta` has only `Code` and `MessageTemplate`. The meta type is right-sized for its consumers.

### Meta record shape: flat record vs discriminated union

When all catalog members share the same metadata shape, the meta type is a **flat sealed record**. When members fall into groups with genuinely different fields, the meta type is a **discriminated union** — an abstract record base with sealed subtypes.

**DU with different fields** — the subtype carries fields that only make sense for that group. `ModifierMeta` and `OperationMeta` use this pattern:

```csharp
// FieldModifierMeta carries ApplicableTo, Subsumes — inapplicable to StateModifierMeta
public sealed record FieldModifierMeta(..., TypeTarget[] ApplicableTo, ...) : ModifierMeta(...);
// StateModifierMeta carries AllowsOutgoing, RequiresDominator — inapplicable to FieldModifierMeta
public sealed record StateModifierMeta(..., bool AllowsOutgoing, ...) : ModifierMeta(...);
```

**DU as identity** — subtypes carry no unique fields, but the type IS the semantic signal. `ConstraintMeta` and `ProofRequirementMeta` use this pattern. Consumers pattern-match exhaustively; the compiler catches unhandled cases when new members are added:

```csharp
public abstract record ConstraintMeta(ConstraintKind Kind, string Description)
{
    public sealed record Invariant()        : ConstraintMeta(...);
    public abstract record StateAnchored(ConstraintKind Kind, string Description) : ConstraintMeta(Kind, Description);
    public sealed record StateResident()    : StateAnchored(...);
    // ...
}

// Consumer: type IS the signal — no field check needed
var plan = meta switch
{
    ConstraintMeta.Invariant         => alwaysBucket,
    ConstraintMeta.StateAnchored     => stateBucket,  // matches all three state kinds
    ConstraintMeta.EventPrecondition => eventBucket,
};
```

This is **not** an enum in disguise. The DU provides:
1. **Compile-time exhaustiveness** — a new enum member without a matching subtype is a build error at every pattern-match site
2. **Type IS the semantic signal** — `meta is StateAnchored` is structurally safer than `meta.Scope == ConstraintScope.State`; no field value can be misassigned
3. **Hierarchy encodes grouping** — the intermediate `StateAnchored` abstract layer expresses that three kinds share a scope axis without needing an extra `ConstraintScope` enum field
4. **Extensibility** — adding a field to one subtype later is non-breaking; flat records require all-member changes

The rule: if you find yourself adding a `ScopeKind`, `SubjectArity`, or similar classification field whose values map 1:1 to subsets of enum members, that field is a sign the meta should be a DU instead.

### 3. Static catalog class — the exhaustive switch

```csharp
public static class Diagnostics
{
    public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
    {
        DiagnosticCode.UnterminatedStringLiteral => new(nameof(...), ...),
        DiagnosticCode.InvalidCharacter          => new(nameof(...), ...),
        ...
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    public static IReadOnlyList<DiagnosticMeta> All { get; } =
        Enum.GetValues<DiagnosticCode>().Select(GetMeta).ToList();

    public static Diagnostic Create(DiagnosticCode code, SourceSpan span, params object?[] args)
    {
        var meta = GetMeta(code);
        return new(meta.Severity, meta.Stage, meta.Code,
            string.Format(meta.MessageTemplate, args), span);
    }
}
```

Three members:

| Member | Purpose | Present in all catalogs |
|--------|---------|------------------------|
| `GetMeta(Kind)` | Exhaustive switch — every enum member maps to its metadata | Yes — this is the catalog |
| `All` | `IReadOnlyList<Meta>` built from `Enum.GetValues<Kind>().Select(GetMeta)` | Yes — MCP enumeration surface |
| `Create(...)` | Factory that formats a runtime output value from the metadata | When the catalog has an associated output type |

Domain-specific members are added as needed. `Tokens` has a `Keywords` frozen dictionary for lexer keyword lookup, derived from `All`.

### 4. Output value type — what the pipeline produces

```csharp
public readonly record struct Diagnostic(
    Severity        Severity,
    DiagnosticStage Stage,
    string          Code,
    string          Message,
    SourceSpan      Span
);
```

Constructed exclusively through the catalog's `Create()` factory, which derives fields from the meta. Not all catalogs have an output type — `Tokens.All` is consumed directly as metadata, with `Token` being the lexer's own output type.

## Why Exhaustive Switch, Not Attributes + Reflection

An alternative approach: decorate enum members with attributes (`[TokenCategory]`, `[TokenDescription]`) and read them via reflection at startup. That works, but: **a missing attribute is a runtime failure, not a compile-time failure.** Closing the gap requires a custom Roslyn analyzer — two layers doing related work.

The exhaustive switch uses the C# compiler directly:

- Add an enum member without a switch arm → **CS8509** (non-exhaustive switch expression). With `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` → **build fails**.
- No custom Roslyn rule needed for catalog completeness.
- No reflection. No source generator. No startup cost.
- The switch IS the catalog. The compiler IS the enforcement.

The trade-off: the switch is more verbose than attributes. At Precept's scale (50–170 enum members per catalog), this is acceptable.

## Roslyn Enforcement Layer

The exhaustive switch enforces **catalog completeness** — every enum member must have metadata. Two of the eleven catalogs — Diagnostics and Faults — also produce output values (`Diagnostic`, `Fault`) with string fields derived from metadata via `nameof()` in the switch. If code bypasses the `Create()` factory for these types, it introduces arbitrary strings that escape the registry. Roslyn analyzers enforce this **construction discipline** for the catalogs that need it.

### Implemented Rules

| Rule | Invariant | What it prevents |
|------|-----------|-----------------|
| **PRECEPT0001** | `Fail()` must pass a `FaultCode` as its first argument | Unclassified evaluator failure paths |
| **PRECEPT0002** | Every `FaultCode` member must carry `[StaticallyPreventable(DiagnosticCode.X)]` | A runtime fault with no corresponding compile-time diagnostic |
| **PRECEPT0003** | `Diagnostic` must be constructed via `Diagnostics.Create()` | Direct `new Diagnostic(...)` with arbitrary string codes |
| **PRECEPT0004** | `Fault` must be constructed via `Faults.Create()` | Direct `new Fault(...)` with arbitrary string codes |

All four are `DiagnosticSeverity.Error` with `isEnabledByDefault: true`. Combined with `<TreatWarningsAsErrors>`, they are build-breaking.

### Two-Layer Enforcement Model

| Layer | Mechanism | What it enforces | Scope |
|-------|-----------|-----------------|-------|
| **Compiler** | CS8509 (exhaustive switch) | Every enum member has a metadata entry | All 12 catalogs |
| **Roslyn** | PRECEPT0001–PRECEPT0004 | Output values go through the factory; cross-catalog linkage is present | Diagnostics and Faults only |

Most catalogs (Tokens, Types, Functions, Operators, Operations, Modifiers, Actions, Constructs, Constraints, ProofRequirements) have no output type with metadata-derived strings — `GetMeta()` and `All` are their entire surface. The compiler layer alone covers them. The Roslyn layer is specific to the two failure-mode catalogs whose output types carry strings that must stay within the registry.

### Future Rules

When the Functions and Operators catalogs land, they will likely need dispatch-enforcement rules:

| Planned rule | Invariant |
|-------------|-----------|
| Functions: evaluator must dispatch through catalog | Evaluator calls the catalog's evaluation delegate, not a hand-coded function switch |
| Operators: evaluator must dispatch through OperatorTable | Evaluator calls the dispatch table, not independent type×operator logic |
| **PRECEPT0005** | `ParamSubject.Parameter` must be reference-equal to a `ParameterMeta` instance declared in the same overload's or operation's parameter list. Prevents stale references after parameter changes. |
| **PRECEPT0006** | `ParamSubject` must not appear in `TypeAccessor.ProofRequirements` or `ActionMeta.ProofRequirements`. `SelfSubject` must not appear in `BinaryOperationMeta.ProofRequirements`. Subject type must be valid for the containing catalog entry. `DimensionProofRequirement` is only valid on `BinaryOperationMeta.ProofRequirements` — a build error if placed on `FunctionOverload`, `TypeAccessor`, or `ActionMeta`. |

## Naming Convention

| Part | Convention | Examples |
|------|-----------|----------|
| Kind enum | `{Thing}Kind` or `{Thing}Code` | `TokenKind`, `DiagnosticCode`, `FaultCode` |
| Meta record | `{Thing}Meta` | `TokenMeta`, `DiagnosticMeta`, `FaultMeta` |
| Catalog class | `{Thing}s` (plural) | `Tokens`, `Diagnostics`, `Faults` |
| Output value | `{Thing}` (singular) | `Token`, `Diagnostic`, `Fault` |

**Kind vs Code:** Use `Kind` when the enum classifies a *kind of thing* (tokens have kinds, types have kinds). Use `Code` when the enum identifies a *rule or failure mode* (diagnostics have codes, faults have codes).

## Catalog Inventory

### Language Definition Catalogs

These ten catalogs describe what the Precept language IS.

#### 1. Tokens (✅ Implemented)

The lexical vocabulary. 90+ members spanning keywords, operators, punctuation, literals, identifiers, and structural tokens.

| Part | Type |
|------|------|
| Kind enum | `TokenKind` |
| Meta record | `TokenMeta(Kind, Text?, Categories[], Description, TextMateScope?, SemanticTokenType?, ValidAfter[]?)` |
| Catalog class | `Tokens` — `GetMeta()`, `All`, `Keywords` (frozen dictionary for lexer lookup) |
| Output type | `Token` (produced by lexer from scan state, not via `Create()`) |

**Consumers:** MCP vocabulary, LS semantic tokens, LS completions, lexer keyword lookup, TextMate grammar keyword alternations.

#### 2. Types (✅ Implemented)

The type system's family taxonomy. Each member represents a type *family*.

| Part | Type |
|------|------|
| Kind enum | `TypeKind` (26 members) |
| Meta record | `TypeMeta` — full shape with `Traits`, `WidensTo`, `ImpliedModifiers`, `Accessors` (see below) |
| Catalog class | `Types` — `GetMeta()`, `All` |
| Output type | `Type` — `abstract record` hierarchy with qualifier payloads (e.g., `MoneyType(string? Currency)`, `PriceType(string? Currency, string? Unit, string? Dimension)`) |

`TypeKind` is the catalog key. `Type` is the type checker's working type — an `abstract record` hierarchy where each sealed variant carries qualifier payloads. `Type.Kind` bridges from one to the other, same as `Token.Kind` bridges `Token` to `TokenKind`. The `abstract Kind` property is compiler-enforced: adding a new `Type` variant without implementing it fails to build.

**Members (1:1 with `Type` variants in `SemanticIndex.cs`):**

| Category | Members |
|----------|---------|
| Scalar | `String`, `Boolean`, `Integer`, `Decimal`, `Number`, `Choice` |
| Temporal | `Date`, `Time`, `Instant`, `Duration`, `Period`, `Timezone`, `ZonedDateTime`, `DateTime` |
| Business-domain | `Money`, `Currency`, `Quantity`, `UnitOfMeasure`, `Dimension`, `Price`, `ExchangeRate` |
| Collection | `Set`, `Queue`, `Stack` |
| Special | `Error`, `StateRef` |

**Consumers:** MCP type vocabulary (field types in `precept_compile` output), LS hover (type documentation), LS completions (type names in expression context), function catalog (parameter/return type signatures), operator lane definitions (lhs/rhs/result types), modifier applicability matrix.

##### TypeMeta — full shape

```csharp
public record TypeMeta(
    TypeKind                     Kind,
    TokenMeta?                   Token,           // object reference to Tokens catalog entry; null for special types (Error, StateRef)
    string                       Description,
    TypeCategory                 Category,
    string                       DisplayName,     // required — human-readable type name (e.g., "zoned date-time", "money")
    QualifierShape?              QualifierShape   = null,
    TypeTrait                    Traits           = TypeTrait.None,
    IReadOnlyList<TypeKind>?     WidensTo         = null,
    ModifierKind[]?              ImpliedModifiers = null,
    IReadOnlyList<TypeAccessor>? Accessors        = null,
    string?                      HoverDescription = null,
    string?                      UsageExample     = null
);
```

The `Token` field holds a direct reference to the `TokenMeta` instance from the Tokens catalog (nullable for special types like `Error` and `StateRef` that have no surface keyword). Consumers access the keyword text via `typeMeta.Token.Text` — no string duplication, no cross-catalog lookup. The Tokens catalog initializes first; all other catalogs reference its instances.

`DisplayName` is required — every type must have a human-readable name. Single-word types use their keyword (e.g., `"money"`); multi-word types use the human form (e.g., `"zoned date-time"`, `"unit of measure"`). Omitting it is a compile error.

##### TypeTrait flags enum

```csharp
[Flags]
public enum TypeTrait
{
    None               = 0,
    Orderable          = 1 << 0,
    EqualityComparable = 1 << 1,
}
```

Orderable types: `integer`, `decimal`, `number`, `date`, `time`, `instant`, `duration`, `datetime`, `money`, `quantity`, `price`. `period` is NOT orderable (ambiguous calendar arithmetic). `zoneddatetime` is NOT orderable (timezone-dependent comparison). `choice` is NOT orderable at the type level — orderable is field-level via the `ordered` modifier.

EqualityComparable types: all surfaced types except collections. The `EqualityComparable` trait is set on every type that supports `==` and `!=` operations in the Operations catalog.

##### TypeAccessor discriminated union

```csharp
public record TypeAccessor(
    string    Name,
    string    Description,
    TypeKind? ParameterType    = null,
    TypeTrait RequiredTraits   = TypeTrait.None,
    ProofRequirement[] ProofRequirements = []
);

public enum QualifierAxis
{
    None,
    Currency,
    Unit,
    Dimension,
    FromCurrency,
    ToCurrency,
    Timezone,
    TemporalDimension,  // period of 'date' / period of 'time' (category)
    TemporalUnit,       // period in 'days' / period in 'months' (specific unit)
}
```

##### QualifierShape — the in/of qualification system

```csharp
public sealed record QualifierSlot(TokenKind Preposition, QualifierAxis Axis);

public sealed record QualifierShape(
    IReadOnlyList<QualifierSlot> Slots,
    bool InOfExclusive = false
);
```

`QualifierShape` defines which qualifiers a type accepts. Each `QualifierSlot` pairs a preposition keyword (`in`, `of`, `to`) with a semantic axis. `InOfExclusive` declares whether `in` and `of` are mutually exclusive (only one can appear) or can coexist.

Shared shapes in the Types catalog:

| Shape | Slots | InOfExclusive | Used by |
|-------|-------|---------------|----------|
| `QS_Currency` | `In(Currency)` | n/a (single slot) | `money` |
| `QS_UnitOrDimension` | `In(Unit)`, `Of(Dimension)` | true | `quantity` |
| `QS_TemporalUnitOrDimension` | `In(TemporalUnit)`, `Of(TemporalDimension)` | true | `period` |
| `QS_CurrencyAndDimension` | `In(Currency)`, `Of(Dimension)` | false | `price` |
| `QS_ExchangeRate` | `In(FromCurrency)`, `To(ToCurrency)` | n/a | `exchangerate` |

public sealed record FixedReturnAccessor(
    string    Name,
    TypeKind  Returns,
    string    Description,
    TypeKind? ParameterType    = null,
    TypeTrait RequiredTraits   = TypeTrait.None,
    ProofRequirement[] ProofRequirements = [],
    QualifierAxis ReturnsQualifier = QualifierAxis.None
) : TypeAccessor(Name, Description, ParameterType, RequiredTraits, ProofRequirements);
```

- `TypeAccessor` base = inner-type return (`.peek`, `.min`, `.max`). Absence of `Returns` is the declaration.
- `FixedReturnAccessor` = fixed return (`.count`, `.currency`, `.amount`, `.inZone(tz)`).
- `RequiredTraits` on base — checked against the collection's inner type for inner-type accessors, and against the owner type for fixed-return accessors.
- `ReturnsQualifier != None` means the accessor returns the qualifier value itself on the named axis — the result type carries the same qualifier as the owner field's qualifier on that axis. Examples: `.currency` on `money` → `ReturnsQualifier: QualifierAxis.Currency`; `.amount` on `money` → `ReturnsQualifier: QualifierAxis.None`. LS hover uses this to display "returns the currency of this field."

##### WidensTo — implicit widening

`TypeMeta.WidensTo` declares lossless implicit widening targets per type. The type checker uses these to allow narrower types where wider types are expected without explicit conversion. Declared per type in the exhaustive switch — no hardcoded widening logic in the type checker.

Only two widening edges in the language: `integer → [Decimal, Number]`. `decimal → number` is NOT implicit — requires `approximate()`. All other types have `WidensTo = []`.

#### 3. Functions (✅ Implemented)

The built-in function library. 21 functions defined in the language spec (§3.7).

| Part | Type |
|------|------|
| Kind enum | `FunctionKind` (21 members) |
| Meta record | `FunctionMeta(Kind, Name, Description, Overloads[], Category, UsageExample?, SnippetTemplate?, HoverDescription?)` — `FunctionOverload` uses `ParameterMeta[]` (see below) |
| Catalog class | `Functions` — `GetMeta()`, `All` |
| Output type | None — functions are evaluated inline |

**Members (from `precept-language-spec.md` §3.7):**

| Category | Members |
|----------|---------|
| Numeric | `Min`, `Max`, `Abs`, `Clamp`, `Floor`, `Ceil`, `Truncate`, `Round`, `RoundPlaces`, `Approximate`, `Pow`, `Sqrt` |
| String | `Trim`, `StartsWith`, `EndsWith`, `ToLower`, `ToUpper`, `Left`, `Right`, `Mid` |
| Temporal | `Now` |

(`Round` and `RoundPlaces` are listed separately because they are distinct overloads with different return types: `round(value) → integer` vs `round(value, places) → decimal`.)

**Consumers:** MCP vocabulary, LS completions (function names in expression context), LS hover (function documentation), type checker (overload resolution and argument validation), evaluator (runtime dispatch).

**Rationale:** The function catalog serves the most consumers of any planned catalog. The evaluation delegate eliminates a parallel copy in the evaluator — the evaluator dispatches through the catalog rather than maintaining its own function switch.

##### ParameterMeta and FunctionOverload shapes

```csharp
public sealed record ParameterMeta(TypeKind Kind, string? Name = null);

public sealed record FunctionOverload(
    IReadOnlyList<ParameterMeta> Parameters,
    TypeKind                     ReturnType,
    QualifierMatch?              QualifierMatch    = null,
    ProofRequirement[]           ProofRequirements = []
);

public sealed record FunctionMeta(
    FunctionKind                    Kind,
    string                          Name,
    string                          Description,
    IReadOnlyList<FunctionOverload> Overloads,
    FunctionCategory                Category,
    string?                         UsageExample     = null,
    string?                         SnippetTemplate  = null,
    string?                         HoverDescription = null
);
```

Parameters are declared as named statics so `ParamSubject` can reference them by object identity — see Proof Obligations.

Note: `FunctionMeta.Name` stays as a `string` because functions are identifiers (`min`, `round`), not keyword tokens. There is no `TokenKind` for functions. `FunctionCategory` groups functions by semantic domain (`Numeric`, `String`, `Temporal`) for completions and MCP vocabulary presentation.

##### Business-type overloads and QualifierMatch

Functions like `abs`, `min`, `max`, `clamp`, `round` have overloads for `money` and `quantity` in addition to numeric types. `FunctionOverload.QualifierMatch` declares how the result's qualifier is derived from the operands' qualifiers. Most overloads use `null` (no qualifier reasoning needed). Business-type overloads use `QualifierMatch.Same` (result carries the same qualifier as the input).

Note: `QualifierMatch` on `FunctionOverload` is `QualifierMatch?` — `null` means no qualifier reasoning needed, distinct from the `Any/Same/Different` values used by `OperationMeta`.

`ParameterMeta` is shared between `FunctionOverload.Parameters` and `BinaryOperationMeta.Lhs`/`Rhs` — see Proof Obligations for why it uses object references rather than `TypeKind`.

#### 4. Operators (✅ Implemented)

Operator symbols — the `+`, `-`, `*`, `/`, `==`, etc. Each member is an operator symbol with its own metadata.

| Part | Type |
|------|------|
| Kind enum | `OperatorKind` (18 members: `Or`, `And`, `Not`, `Equals`, `NotEquals`, `CaseInsensitiveEquals`, `CaseInsensitiveNotEquals`, `LessThan`, `GreaterThan`, `LessThanOrEqual`, `GreaterThanOrEqual`, `Contains`, `Plus`, `Minus`, `Times`, `Divide`, `Modulo`, `Negate`) |
| Meta record | `OperatorMeta(Kind, Token, Description, Arity, Associativity, Precedence, Family, IsKeywordOperator, HoverDescription?, UsageExample?)` — `Token` is a `TokenMeta` object reference |
| Supporting enums | `Arity { Unary, Binary }`, `OperatorFamily { Arithmetic, Comparison, Logical, Membership }`, `Associativity { Left, Right, NonAssociative }` |
| Catalog class | `Operators` — `GetMeta()`, `All` |
| Output type | None |

**Consumers:** MCP vocabulary (operator symbols via `Token.Text` and descriptions), LS hover (operator documentation), TextMate grammar (operator alternations), parser (precedence and associativity).

**Rationale:** `BinaryOp` and `UnaryOp` are currently bare parser-internal enums. The Operators catalog promotes them to first-class language surface with per-member metadata — symbol text, human-readable description, precedence, associativity. Consumers no longer need hardcoded operator lists.

#### 5. Operations (✅ Implemented)

Typed operator combinations — each member is one legal `(operator, lhs TypeKind, rhs TypeKind) → result TypeKind` triple. The catalog is the **source of truth** for what the language can do with specific type combinations — every consumer (type checker, doc generation, MCP, LS hover, AI grounding) derives from these entries.

| Part | Type |
|------|------|
| Kind enum | `OperationKind` (~200 members: `NumberPlusNumber`, `MoneyPlusMoney`, `DatePlusPeriod`, `MoneyTimesDecimal`, `MoneyDivideMoneySameCurrency`, `MoneyDivideMoneyCrossCurrency`, ...) |
| Meta record | `OperationMeta` — abstract DU with `UnaryOperationMeta` and `BinaryOperationMeta` sealed subtypes (see below) |
| Discriminator enum | `QualifierMatch { Any, Same, Different }` |
| Catalog class | `Operations` — `GetMeta()`, `All`, `FindCandidates(OperatorKind, TypeKind, TypeKind) → ReadOnlySpan<BinaryOperationMeta>`, `Resolve(OperatorKind, Type, Type) → OperationMeta?` |
| Output type | None |

##### OperationMeta discriminated union

```csharp
public abstract record OperationMeta(
    OperationKind Kind,
    OperatorKind  Op,
    TypeKind      Result,
    string        Description
);

public sealed record UnaryOperationMeta(
    OperationKind Kind,
    OperatorKind  Op,
    ParameterMeta Operand,
    TypeKind      Result,
    string        Description
) : OperationMeta(Kind, Op, Result, Description);

public sealed record BinaryOperationMeta(
    OperationKind      Kind,
    OperatorKind       Op,
    ParameterMeta      Lhs,
    ParameterMeta      Rhs,
    TypeKind           Result,
    string             Description,
    bool               BidirectionalLookup = false,
    QualifierMatch     Match               = QualifierMatch.Any,
    ProofRequirement[] ProofRequirements   = []
) : OperationMeta(Kind, Op, Result, Description);
```

`BidirectionalLookup = true` marks operations where `(op, lhs, rhs)` and `(op, rhs, lhs)` are the same entry — the index registers both key orderings so type-checking commutative operations doesn't require two entries (e.g., `money * decimal` and `decimal * money`).

`Operations.All` is `IReadOnlyList<OperationMeta>`. Two internal indexes: `FrozenDictionary<(OperatorKind, TypeKind), UnaryOperationMeta>` keyed by `(Op, Operand.Kind)`, and `FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta[]>` keyed by `(Op, Lhs.Kind, Rhs.Kind)`. The index key uses `ParameterMeta.Kind` for the `TypeKind` component.

`Lhs`, `Rhs`, and `Operand` are `ParameterMeta` (not `TypeKind`) so `ParamSubject` can hold a direct reference to the instance — see Proof Obligations.

**Unary operations (9 total):** `-integer`, `-decimal`, `-number`, `-money`, `-quantity`, `-price`, `-exchangerate`, `-duration`, `not boolean`. Result type is always the same as the operand type.

##### QualifierMatch — conditional result types

Two operations in the language produce different result types depending on whether the operands' qualifiers match:

| Operation | Same qualifier | Different qualifier |
|-----------|---------------|---------------------|
| `money / money` | `decimal` (dimensionless ratio) | `exchangerate` (currency pair) |
| `quantity / quantity` | `decimal` (dimensionless ratio) | `quantity` (compound unit) |

Rather than hiding this branching in the type checker, the catalog declares it explicitly via the `QualifierMatch` discriminator:

```csharp
enum QualifierMatch { Any, Same, Different }
```

- `Any` — default. No qualifier inspection needed. Used by ~95% of entries.
- `Same` — entry applies when operand qualifiers are equal (same currency, same dimension).
- `Different` — entry applies when operand qualifiers differ.

The four entries that use it:

```csharp
new(MoneyDivideMoneySameCurrency,      Divide, Money,    Money,    Decimal,      "Same-currency ratio",       Same),
new(MoneyDivideMoneyCrossCurrency,     Divide, Money,    Money,    ExchangeRate, "Cross-currency derivation", Different),
new(QuantityDivideQuantitySameDim,     Divide, Quantity, Quantity, Decimal,      "Same-dimension ratio",      Same),
new(QuantityDivideQuantityCrossDim,    Divide, Quantity, Quantity, Quantity,     "Compound unit derivation",  Different),
```

Every other entry has `Match = QualifierMatch.Any` and stands alone for its `(Op, Lhs, Rhs)` triple.

This keeps the catalog as the single source of truth: doc generation, MCP output, and the type checker all derive from the same entries. A doc generator groups by `(Op, Lhs, Rhs)`, detects multi-entry groups, and labels them by `Match`. An AI consumer sees both entries with their `qualifierMatch` discriminator and understands the branching without reading source code.

##### Resolution

The internal index groups entries by `(Op, Lhs TypeKind, Rhs TypeKind)`. Most triples have one entry; the two branching operations have two.

`FindCandidates` returns all entries for a given triple — the raw catalog data, usable by doc generators and MCP serialization.

`Resolve` is the type-checker-facing method. It takes full `Type` objects (with qualifiers) and selects the correct entry:

```csharp
public static class Operations
{
    private static readonly FrozenDictionary<(OperatorKind, TypeKind), UnaryOperationMeta> _unaryIndex =
        All.OfType<UnaryOperationMeta>()
           .ToFrozenDictionary(m => (m.Op, m.Operand.Kind));

    private static readonly FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta[]> _binaryIndex =
        All.OfType<BinaryOperationMeta>()
           .GroupBy(m => (m.Op, m.Lhs.Kind, m.Rhs.Kind))
           .ToFrozenDictionary(g => g.Key, g => g.ToArray());

    public static ReadOnlySpan<BinaryOperationMeta> FindCandidates(
        OperatorKind op, TypeKind lhs, TypeKind rhs)
        => _binaryIndex.TryGetValue((op, lhs, rhs), out var entries)
            ? entries.AsSpan() : ReadOnlySpan<BinaryOperationMeta>.Empty;

    public static OperationMeta? Resolve(OperatorKind op, Type lhs, Type rhs)
    {
        var candidates = FindCandidates(op, lhs.Kind, rhs.Kind);
        if (candidates.IsEmpty) return null;        // illegal combination
        if (candidates.Length == 1) return candidates[0];  // fast path — vast majority

        // Multiple candidates → qualifier dispatch
        bool? match = lhs.QualifierEquals(rhs);
        if (match == true)  return candidates.Single(c => c.Match == QualifierMatch.Same);
        if (match == false) return candidates.Single(c => c.Match == QualifierMatch.Different);
        return null;  // unknown qualifiers — can't resolve statically
    }
}
```

When `Resolve` returns `null` for a multi-candidate triple (qualifiers unknown), the type checker either:
- Uses assignment target context to disambiguate (assigning to `decimal` field → pick `Same`; assigning to `exchangerate` field → pick `Different`)
- Emits a diagnostic: "Cannot determine result type — add a `when` guard or declare `in` constraints"
- Flags a proof obligation for the proof engine

The catalog returns `OperationMeta?`, not `Type?`. The catalog doesn't know qualifier values — it can't construct a fully-qualified result type. The type checker reads `meta.Result` and constructs the result `Type` with appropriate qualifiers via the qualifier propagation patterns (see type checker design).

##### Type.QualifierEquals — the qualifier comparison contract

Each type with qualifiers implements `QualifierEquals(Type) → bool?` (three-valued: true/false/null for unknown):

- `MoneyType`: compares `.Currency`
- `QuantityType`: compares `.Dimension` (not specific unit — `km` and `m` are both `length` → `true`)
- `PriceType`, `ExchangeRateType`, `PeriodType`: compare their respective qualifier axes

Types without qualifiers (`DecimalType`, `IntegerType`, `DateType`, etc.) always return `true`.

**Consumers:** Type checker (legal combinations and result types via `Resolve`), doc generation (complete operation table including conditional branches), MCP vocabulary ("what operations are legal and what do they produce?"), LS hover (per-combination documentation), evaluator dispatch, AI grounding (full operation surface).

**Relationship to Operators catalog:** Each `OperationMeta` references an `OperatorKind`. You can query "what operations use `Plus`?" by filtering `Operations.All` where `Op == OperatorKind.Plus`. The Operators catalog describes the symbols; the Operations catalog describes what those symbols can do with specific types.

**Replaces `OperatorTable`:** The existing `OperatorTable.ResolveBinary(BinaryOp, Type, Type) → Type?` is absorbed by `Operations.Resolve`. The `OperatorTable` class becomes redundant once the Operations catalog is implemented.

#### 6. Modifiers (✅ Implemented)

All declaration-attached modifiers across the language surface — field constraints, state lifecycle modifiers, event modifiers, access modes, and ensure/action anchors. The Modifiers catalog uses a **discriminated union with 5 sealed subtypes**, each carrying exactly the metadata its consumers need.

| Part | Type |
|------|------|
| Kind enum | `ModifierKind` (28 members across 5 subtypes) |
| Meta record | `ModifierMeta` — abstract DU base with `FieldModifierMeta`, `StateModifierMeta`, `EventModifierMeta`, `AccessModifierMeta`, `AnchorModifierMeta` sealed subtypes (see below) |
| Supporting enums | `ModifierCategory`, `GraphAnalysisKind`, `AnchorScope`, `AnchorTarget` |
| Catalog class | `Modifiers` — `GetMeta()`, `All` |
| Output type | None |

**Members by subtype:**

| Subtype | Members | Count |
|---------|---------|-------|
| `FieldModifierMeta` | `optional`, `default`, `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`, `ordered` | 14 |
| `StateModifierMeta` | `initial` (state), `terminal`, `required`, `irreversible`, `success`, `warning`, `error` | 7 |
| `EventModifierMeta` | `initial` (event) | 1 |
| `AccessModifierMeta` | `write`, `read`, `omit` | 3 |
| `AnchorModifierMeta` | `in`, `to`, `from` | 3 (with `AnchorTarget` disambiguating ensure vs state-action) |

**Consumers:** MCP vocabulary, LS completions (modifier names after type, state modifiers in state declarations, access modes in state blocks), LS hover, type checker (modifier applicability per TypeKind, access mode enforcement, state modifier graph analysis), graph analyzer (structural modifier validation).

**Rationale:** The language vision (`precept-language-vision.md` §Modifier System Expansion) defines modifiers across 5 declaration surfaces — fields, states, events, rules, and potentially the precept itself — with 3 modifier categories (structural, semantic, severity). A flat `ModifierMeta` record would carry many inapplicable fields per subtype (e.g., `ApplicableTo` is meaningless for state modifiers; `AllowsOutgoing` is meaningless for field modifiers). The DU ensures each subtype carries exactly its consumers' metadata, and the C# type system prevents accessing inapplicable fields.

The DU also absorbs 4 bare enums (`StateModifierKind`, `AccessMode`, `EnsureAnchor`, `StateActionAnchor`) that were previously classified as internal classification axes. With proper subtypes, these are now first-class language surface members in `Modifiers.All`.

##### ModifierMeta — discriminated union

```csharp
// ── Base ──────────────────────────────────────────────────
public abstract record ModifierMeta(
    ModifierKind      Kind,
    TokenMeta         Token,                       // object reference to Tokens catalog entry
    string            Description,
    ModifierCategory  Category,                    // Structural, Semantic, Severity
    ModifierKind[]?   MutuallyExclusiveWith = null // at most one of the group may appear on a declaration
);

// ── Field modifiers (14) ─────────────────────────────────
public sealed record FieldModifierMeta(
    ModifierKind     Kind,
    TokenMeta        Token,
    string           Description,
    ModifierCategory Category,
    TypeTarget[]     ApplicableTo,
    bool             HasValue          = false,
    ModifierKind[]   Subsumes          = [],
    string?          HoverDescription  = null,
    string?          UsageExample      = null,
    string?          SnippetTemplate   = null,
    ModifierKind[]?  MutuallyExclusiveWith = null
) : ModifierMeta(Kind, Token, Description, Category, MutuallyExclusiveWith);

// ── State modifiers (7) ─────────────────────────────────
public sealed record StateModifierMeta(
    ModifierKind     Kind,
    TokenMeta        Token,
    string           Description,
    ModifierCategory Category,
    bool             AllowsOutgoing    = true,   // terminal = false
    bool             RequiresDominator  = false,  // required = true
    bool             PreventsBackEdge   = false   // irreversible = true
) : ModifierMeta(Kind, Token, Description, Category);

// ── Event modifiers (1) ─────────────────────────────────
public sealed record EventModifierMeta(
    ModifierKind       Kind,
    TokenMeta          Token,
    string             Description,
    ModifierCategory   Category,
    GraphAnalysisKind  RequiredAnalysis = GraphAnalysisKind.None
) : ModifierMeta(Kind, Token, Description, Category);

// ── Access modes (3: write, read, omit) ─────────────────
public sealed record AccessModifierMeta(
    ModifierKind     Kind,
    TokenMeta        Token,
    string           Description,
    ModifierCategory Category,
    bool             IsPresent    = true,    // false = omit (structurally absent)
    bool             IsWritable   = true     // false = read-only
) : ModifierMeta(Kind, Token, Description, Category);
// write: IsPresent=true,  IsWritable=true
// read:  IsPresent=true,  IsWritable=false
// omit:  IsPresent=false, IsWritable=false

// ── Ensure/action anchors (in, to, from) ────────────────
public sealed record AnchorModifierMeta(
    ModifierKind     Kind,
    TokenMeta        Token,
    string           Description,
    ModifierCategory Category,
    AnchorScope      Scope,          // InState, OnEntry, OnExit
    AnchorTarget     Target          // Ensure, StateAction
) : ModifierMeta(Kind, Token, Description, Category);
```

`Token` replaces the `string Keyword` field — consumers access keyword text via `modifier.Token.Text`. `MutuallyExclusiveWith` declares modifier exclusion groups on the base; consumers (type checker, LS) enforce the constraint without hardcoding group membership. Modifiers with no runtime validation (e.g., `ordered` is compile-time only) have no inline delegate — execution is handled by the evaluator's pass over `FieldModifierMeta.ApplicableTo` entries.

##### ModifierCategory

Three categories from the language vision (issues #58 and #86):

```csharp
public enum ModifierCategory { Structural, Semantic, Severity }
```

- **Structural:** Compile-time-provable properties — lifecycle shape, one-write behavior, entry behavior, terminality. Requires graph analysis for validation.
- **Semantic:** Intent and tooling meaning — success, error, sensitive, audit, deprecated. No graph analysis needed.
- **Severity:** Language-level control over how declarations surface as warnings vs hard invariants.

##### GraphAnalysisKind (for EventModifierMeta)

Maps each event modifier to the graph reasoning the compiler must perform:

```csharp
public enum GraphAnalysisKind { None, IncomingEdge, OutcomeType, Reachability, InitialEventCompatibility }
```

| Event modifier | GraphAnalysisKind | What the compiler checks |
|---|---|---|
| `initial` (event) | `None` | Keyword match only — no graph analysis |
| `entry` | `IncomingEdge` | Event fires only from the initial state |
| `advancing` | `OutcomeType` | Every successful outcome is a state transition |
| `settling` | `OutcomeType` | Every successful outcome is no-transition |
| `completing` | `OutcomeType` | Transitions only to terminal states |
| `absorbing` | `OutcomeType` | Event handlers never transition out |
| `guarded` | `IncomingEdge` | All incoming transitions have guards |
| `isolated` | `IncomingEdge` | Event fires from exactly one state |
| `universal` | `Reachability` | Event fires from every reachable non-terminal state |

Future event modifiers are deferred but the `GraphAnalysisKind` enum is shaped to accommodate them.

##### AnchorScope and AnchorTarget

```csharp
public enum AnchorScope  { InState, OnEntry, OnExit }
public enum AnchorTarget { Ensure, StateAction }
```

Anchors (`in`, `to`, `from`) appear in both ensure and state-action contexts. 3 `ModifierKind` values (`In`, `To`, `From`) with `AnchorTarget` disambiguating ensure vs state-action.

##### `initial` keyword resolution

The keyword `initial` appears on both states and events with different semantics. Resolution: two `ModifierKind` values — `InitialState` and `InitialEvent` — same keyword text `"initial"`, different subtypes (`StateModifierMeta` vs `EventModifierMeta`), different metadata.

##### Field modifier applicability

The applicability matrix is currently validated by ad-hoc logic in the type checker. The catalog makes it explicit: `nonnegative` applies to `Integer`, `Decimal`, `Number`; `notempty` applies to `String`; `mincount`/`maxcount` apply to `Set`, `Queue`, `Stack`; `maxplaces` applies to `Decimal` only; `ordered` applies to `Choice` only. The `HasValue` flag distinguishes value-carrying modifiers (`min 0`) from bare flags (`nonnegative`). `ApplicableTo` uses `TypeTarget[]` (see Supporting Types) for modifier-sensitive applicability.

Field modifiers also apply in event arg positions (e.g., `event Submit(amount: money nonnegative)`). The modifier catalog declares type-level applicability; the *position* where a modifier can appear (field declaration vs event arg) is a parser/construct-level concern handled by the Constructs catalog.

##### Modifier subsumption

`Subsumes` declares which weaker modifiers this modifier makes redundant. When a field has `positive`, it already implies `nonzero` and `nonnegative` — these need not be declared. The type checker uses `Subsumes` to detect redundant modifier declarations and emit a diagnostic. Roslyn analyzer enforces that `Subsumes` entries are always drawn from the correct subsumption chain (a modifier cannot claim to subsume something it doesn't structurally imply).

Static relationships: `positive.Subsumes = [Nonnegative, Nonzero]`. All other modifiers: `Subsumes = []`.

##### Implied modifiers (TypeMeta.ImpliedModifiers)

Some types carry implied modifiers intrinsically. `currency` and `unitofmeasure` fields are always `notempty`. These are declared on `TypeMeta.ImpliedModifiers` — the type checker merges them with declared modifiers before validation. Roslyn analyzer rule: every entry in `ImpliedModifiers` must be present in the subsumption chain of at least one existing `ModifierMeta.Subsumes` entry — prevents phantom modifier implications.

##### State modifier graph analysis

State modifiers that are structural (`terminal`, `required`, `irreversible`) require graph analysis at compile time. The `StateModifierMeta` boolean fields declare the graph property each modifier asserts:

- `AllowsOutgoing = false` → terminal. Compiler validates no outgoing transition rows exist.
- `RequiresDominator = true` → required. Compiler performs dominator analysis (Lengauer-Tarjan, O(V+E)) to verify all initial→terminal paths visit this state.
- `PreventsBackEdge = true` → irreversible. Compiler performs reverse-reachability to verify no path from this state back to any ancestor in the initial→forward ordering.

Semantic state modifiers (`success`, `warning`, `error`) require no graph analysis — they are intent declarations for tooling and documentation.

#### 7. Actions (✅ Implemented)

State-machine action verbs — the keywords that appear after `->` in transition rows and state action hooks.

| Part | Type |
|------|------|
| Kind enum | `ActionKind` (8 members) |
| Meta record | `ActionMeta(Kind, Token, Description, ApplicableTo TypeTarget[], ValueRequired bool, IntoSupported bool, ProofRequirements[], AllowedIn ConstructKind[])` — `Token` is a `TokenMeta` object reference; see full shape below |
| Catalog class | `Actions` — `GetMeta()`, `All` |
| Output type | None |

**Members (from `ActionKind.cs`):**

| Action | ApplicableTo (`TypeTarget[]`) | Value | Into | AllowedIn |
|--------|-------------------------------|-------|------|----------|
| `set` | any `TypeKind` (empty = caller validates) | required (`= Expr`) | no | `[ConstructKind.EventDeclaration]` |
| `add` | `[TypeTarget(Set)]` | required (`Expr`) | no | `[ConstructKind.EventDeclaration]` |
| `remove` | `[TypeTarget(Set)]` | required (`Expr`) | no | `[ConstructKind.EventDeclaration]` |
| `enqueue` | `[TypeTarget(Queue)]` | required (`Expr`) | no | `[ConstructKind.EventDeclaration]` |
| `dequeue` | `[TypeTarget(Queue)]` | no value | yes (`into Field`) | `[ConstructKind.EventDeclaration]` |
| `push` | `[TypeTarget(Stack)]` | required (`Expr`) | no | `[ConstructKind.EventDeclaration]` |
| `pop` | `[TypeTarget(Stack)]` | no value | yes (`into Field`) | `[ConstructKind.EventDeclaration]` |
| `clear` | `[TypeTarget(Set), TypeTarget(Queue), TypeTarget(Stack), ModifiedTypeTarget(null, [Optional])]` | no value | no | `[ConstructKind.EventDeclaration]` |

`clear` on optional scalars: `ModifiedTypeTarget(Kind: null, RequiredModifiers: [ModifierKind.Optional])` — matches any field with the `Optional` modifier, regardless of type kind. `TargetCollectionKind?` is replaced by `ApplicableTo TypeTarget[]` uniformly across the catalog.

##### ActionMeta — full shape

```csharp
public sealed record ActionMeta(
    ActionKind         Kind,
    TokenMeta          Token,             // object reference to Tokens catalog entry
    string             Description,
    TypeTarget[]       ApplicableTo      = [],
    bool               ValueRequired     = false,
    bool               IntoSupported     = false,
    ProofRequirement[] ProofRequirements = [],
    ConstructKind[]    AllowedIn         = []
);
```

Consumers access the action keyword text via `action.Token.Text`. Execution delegates on ActionMeta are deferred until the evaluator's working copy API is designed — the delegate signature depends on how mutation is represented.

**Consumers:** MCP vocabulary, LS completions (action verbs after `->` in event bodies), LS hover, parser validation, type checker (target type compatibility per `precept-language-spec.md` §3.8).

#### 8. Constructs (✅ Implemented)

Grammar forms / declaration shapes.

| Part | Type |
|------|------|
| Kind enum | `ConstructKind` (11 members) |
| Meta record | `ConstructMeta(Kind, Name, Description, UsageExample, AllowedIn[], Slots[], LeadingToken, SnippetTemplate?)` — see full shape below |
| Supporting types | `ConstructSlot(Kind, IsRequired, Description?)`, `ConstructSlotKind` (15-member enum) |
| Catalog class | `Constructs` — `GetMeta()`, `All` |
| Output type | None |

**Members (from `precept-language-spec.md` §2.2 top-level dispatch):**

`PreceptHeader`, `FieldDeclaration`, `StateDeclaration`, `EventDeclaration`, `RuleDeclaration`, `TransitionRow`, `StateEnsure`, `EventEnsure`, `AccessMode`, `StateAction`, `EventHandler`

**Consumers:** MCP vocabulary (grammar reference), LS completions (context-sensitive construct suggestions), TextMate grammar (derivable from slot arrays), reference documentation, parser validation tests.

##### ConstructMeta — full shape

```csharp
public sealed record ConstructMeta(
    ConstructKind                Kind,
    string                       Name,
    string                       Description,
    string                       UsageExample,
    ConstructKind[]              AllowedIn,           // empty = valid at precept body level (top-level)
    IReadOnlyList<ConstructSlot> Slots,
    TokenKind                    LeadingToken,
    string?                      SnippetTemplate = null
);

public sealed record ConstructSlot(
    ConstructSlotKind Kind,
    bool              IsRequired  = true,
    string?           Description = null
);

// 15-member enum: IdentifierList, TypeExpression, ModifierList, StateModifierList,
// ArgumentList, ComputeExpression, GuardClause, ActionChain, Outcome,
// StateTarget, EventTarget, EnsureClause, BecauseClause, AccessModeKeyword, FieldTarget
public enum ConstructSlotKind { ... }
```

`AllowedIn` declares where a construct can appear: empty means the construct is valid at precept body level (top-level declarations); populated means the construct is only valid nested inside one of the listed parent construct kinds. `LeadingToken` identifies the keyword that starts the construct — used by the grammar generator to emit keyword-anchored rules from catalog metadata. LS completions use `AllowedIn` to filter context-sensitive suggestions: "which constructs have the current cursor's parent construct kind in their `AllowedIn`?"

#### 9. Constraints (✅ Implemented)

The five constraint declaration forms. Each form has a distinct activation shape: invariants are always active; state-anchored constraints activate on state entry, residency, or exit; event preconditions fire before an event executes.

| Part | Type |
|------|------|
| Kind enum | `ConstraintKind` (5 members: `Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`) |
| Meta record | `ConstraintMeta` — DU as identity (see below) |
| Catalog class | `Constraints` — `GetMeta()`, `All` |
| Output type | `ConstraintDescriptor` (in `Precept.Runtime`) — instance carrying `Kind`, `ScopeTarget`, `ExpressionText`, `Because`, `ReferencedFields`, `HasGuard`, `SourceLine` |

**Meta shape — DU as identity:**

```csharp
public abstract record ConstraintMeta(ConstraintKind Kind, string Description)
{
    public sealed record Invariant()         : ConstraintMeta(ConstraintKind.Invariant, ...);

    // Abstract intermediate — groups the three state-anchored kinds
    public abstract record StateAnchored(ConstraintKind Kind, string Description)
        : ConstraintMeta(Kind, Description);
    public sealed record StateResident()     : StateAnchored(ConstraintKind.StateResident, ...);
    public sealed record StateEntry()        : StateAnchored(ConstraintKind.StateEntry, ...);
    public sealed record StateExit()         : StateAnchored(ConstraintKind.StateExit, ...);

    public sealed record EventPrecondition() : ConstraintMeta(ConstraintKind.EventPrecondition, ...);
}
```

The `StateAnchored` intermediate layer allows consumers to check `meta is ConstraintMeta.StateAnchored` for any state-scoped constraint without individually testing three kinds.

**Consumers:** plan router (constraint bucket assignment), evaluator (activation timing), MCP vocabulary, LS hover.

#### 10. ProofRequirements (✅ Implemented)

The five proof obligation kinds that catalog entries can declare. Used by the proof engine to determine what must be proven before an operation, function, accessor, or action can execute.

| Part | Type |
|------|------|
| Kind enum | `ProofRequirementKind` (5 members: `Numeric`, `Presence`, `Dimension`, `Modifier`, `QualifierCompatibility`) |
| Meta record | `ProofRequirementMeta` — DU as identity (see below) |
| Catalog class | `ProofRequirements` — `GetMeta()`, `All` |
| Instance values | `ProofRequirement` abstract record + 5 sealed subtypes — per-use obligation instances carried in `ActionMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`, etc. |

**Meta shape — DU as identity:**

```csharp
public abstract record ProofRequirementMeta(ProofRequirementKind Kind, string Description)
{
    public sealed record Numeric()                : ProofRequirementMeta(...);
    public sealed record Presence()               : ProofRequirementMeta(...);
    public sealed record Dimension()              : ProofRequirementMeta(...);
    public sealed record Modifier()               : ProofRequirementMeta(...);
    public sealed record QualifierCompatibility() : ProofRequirementMeta(...);  // dual-subject
}
```

`QualifierCompatibility` is the only dual-subject kind — its obligation instances carry both `LeftSubject` and `RightSubject`. Consumers can check `meta is ProofRequirementMeta.QualifierCompatibility` rather than inspecting a `SubjectArity` field.

**Instance vs meta separation:** The `ProofRequirementMeta` DU describes the KIND statically (`ProofRequirements.All` enumerates them). The `ProofRequirement` instance record hierarchy carries per-use data — specific subjects, thresholds, comparisons — and lives in the catalog entries that declare obligations (`ActionMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`, etc.). The base `ProofRequirement` record carries `Kind` (catalog membership) and `Description`; subtypes carry kind-specific subjects.

**Consumers:** proof engine (obligation dispatch), type checker, Roslyn analyzers (PRECEPT0005/0006), MCP vocabulary.

### Failure Mode Catalogs

#### 11. Diagnostics (✅ Implemented)

Compile-time rules — every error and warning the pipeline can produce. Currently 78 members across Lex (8), Parse (5), Type (35+16+2), Graph (2), and Proof (3) stages.

| Part | Type |
|------|------|
| Kind enum | `DiagnosticCode` |
| Meta record | `DiagnosticMeta(Code, Stage, Severity, Category, MessageTemplate)` |
| Supporting enums | `DiagnosticStage { Lex, Parse, Type, Graph, Proof }`, `Severity { Info, Warning, Error }`, `DiagnosticCategory { Naming, TypeSystem, Temporal, BusinessDomain, Structure, Safety, Proof }` |
| Catalog class | `Diagnostics` — `GetMeta()`, `All`, `Create()` |
| Output type | `Diagnostic(Severity, Stage, Code, Message, Span)` |

`DiagnosticCategory` describes *what* a diagnostic is about, complementing `DiagnosticStage` which describes *when* it fires. Used by the language server for filtering, documentation generation, and AI grounding.

#### 12. Faults (✅ Implemented)

Runtime failure modes — every fault the evaluator can produce. Currently 13 members. Each `FaultCode` carries a `[StaticallyPreventable(DiagnosticCode)]` attribute linking it to the compile-time rule that should prevent that site.

| Part | Type |
|------|------|
| Kind enum | `FaultCode` (13 members: `DivisionByZero`, `SqrtOfNegative`, `TypeMismatch`, `UndeclaredField`, `UnexpectedNull`, `InvalidMemberAccess`, `FunctionArityMismatch`, `FunctionArgConstraintViolation`, `CollectionEmptyOnAccess`, `CollectionEmptyOnMutation`, `QualifierMismatch`, `NumericOverflow`, `OutOfRange`) |
| Meta record | `FaultMeta(Code, MessageTemplate)` |
| Catalog class | `Faults` — `GetMeta()`, `All`, `Create()` |
| Output type | `Fault(Code, CodeName, Message)` — `CodeName` is the `nameof`-derived stable identity string used for logging and MCP reporting |

**Consumers:** MCP fire/inspect, runtime outcome reporting.

---

## Supporting Types

These record types are shared across multiple catalogs. They are not catalogs themselves — they have no `Kind` enum or `All` property — but they are part of the catalog system's vocabulary.

### TypeTarget discriminated union

Used in `ModifierMeta.ApplicableTo` and `ActionMeta.ApplicableTo`. Replaces `TypeKind[]` wherever a catalog entry needs to declare type applicability with optional modifier requirements.

```csharp
public record TypeTarget(TypeKind Kind);

public sealed record ModifiedTypeTarget(
    TypeKind?      Kind,
    ModifierKind[] RequiredModifiers
) : TypeTarget(Kind ?? TypeKind.Error);
```

- `TypeTarget(Kind)` — applies to fields of the given type, no modifier requirement.
- `ModifiedTypeTarget(Kind, RequiredModifiers)` — applies when the field has the given type AND all listed modifiers. `Kind = null` means "any type."
- List = OR semantics. Each entry in an `ApplicableTo` array is checked independently. Each `ModifiedTypeTarget.RequiredModifiers` array is an AND condition.

```csharp
public static bool IsApplicable(TypeTarget target, Type fieldType, IReadOnlyList<ModifierKind> fieldModifiers)
    => target switch
    {
        ModifiedTypeTarget m => (m.Kind == null || fieldType.Kind == m.Kind)
                                && m.RequiredModifiers.All(fieldModifiers.Contains),
        TypeTarget t         => fieldType.Kind == t.Kind,
    };
```

---

## Qualifier Propagation

The Operations catalog declares **what** each operation produces, including conditional results via `QualifierMatch`. The type checker implements **how** qualifiers propagate through expressions — taking the catalog's result `TypeKind` and constructing the fully-qualified result `Type`.

### The split

| Concern | Owner | Needs qualifier values? |
|---------|-------|------------------------|
| Operation legality + result TypeKind | Operations catalog | No — `QualifierMatch` discriminator handles branching |
| Qualifier compatibility (same-currency check, same-dimension check) | Type checker | Yes |
| Result qualifier construction (which currency/unit does the result carry?) | Type checker | Yes |
| Diagnostic emission (`CrossCurrencyArithmetic`, etc.) | Type checker | Yes |
| Proof obligations for unknown qualifiers | Type checker → proof engine | Yes |

The catalog is the source of truth for result types. The type checker is the source of truth for qualifier-level reasoning. These are complementary, not competing.

### Qualifier propagation patterns

All qualifier-bearing operations follow one of four patterns. The type checker applies these after the catalog lookup succeeds:

**Pattern A — Homogeneous ±:** Both operands must have compatible qualifiers. Result inherits the resolved qualifier. (`money ± money`, `quantity ± quantity`, `price ± price`)

**Pattern B — Scalar scaling:** The qualified operand's qualifiers pass through unchanged. (`money * decimal`, `quantity / decimal`, etc.)

**Pattern C — Dimensional cancellation / derivation:** Result qualifiers are derived from input qualifiers. Operation-specific: `price * quantity → money` takes currency from price and cancels unit; `exchangerate * money → money` verifies pair alignment. (`price * quantity → money`, `money / quantity → price`, `exchangerate * money → money`)

**Pattern D — Same-type ratio:** Qualifiers must be compatible (same as Pattern A), but result is dimensionless — `DecimalType()` with no qualifier. (`money / money` same currency, `quantity / quantity` same dimension)

### Five qualifier-bearing types

| Type | Qualifier axes | Pattern A ops | Pattern B ops | Pattern C ops | Pattern D ops |
|------|---------------|---------------|---------------|---------------|---------------|
| `money` | Currency | `money ± money` | `money * decimal` | `exchangerate * money`, `price * quantity` | `money / money` (same) |
| `quantity` | Unit, Dimension | `quantity ± quantity` | `quantity * decimal` | `price * quantity` (cancel), `money / quantity` | `quantity / quantity` (same) |
| `period` | Unit, Dimension | `period ± period` | — | `price * period` (cancel) | — |
| `price` | Currency, Unit, Dimension | `price ± price` | `price * decimal` | `money / quantity → price` | — |
| `exchangerate` | FromCurrency, ToCurrency | — | `exchangerate * decimal` | `money / money → exchangerate` (diff) | — |

### Unknown qualifier handling

When qualifiers are statically unknown (open field, no `in` constraint, no guard), the type checker follows the three-tier enforcement model from the business-domain-types spec:

- **Tier 1 (compile time):** Both qualifiers known → resolve immediately
- **Tier 2 (proof engine):** One or both unknown → emit proof obligation ("prove these currencies match at this execution point")
- **Tier 3 (runtime boundary):** Qualifier validated at fire/update boundary before engine runs

---

## Proof Obligations

Catalog entries declare proof obligations they impose on the type checker and proof engine. The proof layer reads these from catalog metadata — no hardcoded obligation lists in the proof engine.

### ProofSubject discriminated union

```csharp
public abstract record ProofSubject;

// References a parameter by object identity — no index, no string.
// Must be reference-equal to one of the ParameterMeta instances in the
// containing overload's Parameters list. Enforced by Roslyn analyzer.
public sealed record ParamSubject(ParameterMeta Parameter) : ProofSubject;

// References the receiver of an accessor or action.
// Accessor is a TypeAccessor reference (e.g. the count accessor) — not a string.
// Null Accessor means "the field itself" — used for PresenceProofRequirement.
public sealed record SelfSubject(TypeAccessor? Accessor = null) : ProofSubject;
```

### ProofRequirement discriminated union

```csharp
public abstract record ProofRequirement(ProofSubject Subject, string Description);

// Numeric interval proof: subject comparison threshold must hold
public sealed record NumericProofRequirement(
    ProofSubject Subject,
    OperatorKind Comparison,
    decimal      Threshold,
    string       Description
) : ProofRequirement(Subject, Description);

// Presence proof: optional field must be set before access
public sealed record PresenceProofRequirement(
    ProofSubject Subject,
    string       Description
) : ProofRequirement(Subject, Description);

// Dimension proof: period operand must have a specific time dimension.
// Valid only on BinaryOperationMeta.ProofRequirements.
public enum PeriodDimension { Any, Date, Time }

public sealed record DimensionProofRequirement(
    ProofSubject    Subject,
    PeriodDimension RequiredDimension,
    string          Description
) : ProofRequirement(Subject, Description);
```

`OperatorKind` is reused directly — no parallel enum.

### Valid subjects and requirement types per catalog entry

| Catalog entry | Valid proof requirement types |
|---|---|
| `FunctionOverload.ProofRequirements` | `NumericProofRequirement`, `PresenceProofRequirement` |
| `BinaryOperationMeta.ProofRequirements` | `NumericProofRequirement`, `DimensionProofRequirement` |
| `TypeAccessor.ProofRequirements` | `NumericProofRequirement`, `PresenceProofRequirement` |
| `ActionMeta.ProofRequirements` | `NumericProofRequirement` |

Enforced by Roslyn analyzer — see PRECEPT0005/PRECEPT0006.

### ParameterMeta — object-reference safety

`ParameterMeta` is shared across `FunctionOverload.Parameters` and `BinaryOperationMeta.Lhs`/`Rhs`. Parameters are declared as named statics; `ParamSubject` holds a direct reference to the `ParameterMeta` instance. The Roslyn analyzer verifies `ParamSubject.Parameter` is reference-equal to one of the containing overload/operation's parameter instances — compile-time referential integrity with no strings.

### Complete proof obligation inventory

| Obligation | Catalog entry | Requirement type | Subject | Condition |
|---|---|---|---|---|
| Divisor safety | `BinaryOperationMeta` `/` and `%` | `NumericProofRequirement` | `ParamSubject(Rhs)` | `!= 0` |
| Sqrt non-negative | `FunctionOverload` sqrt | `NumericProofRequirement` | `ParamSubject(Input)` | `>= 0` |
| Pow exponent non-negative | `FunctionOverload` pow(integer,integer) | `NumericProofRequirement` | `ParamSubject(Exponent)` | `>= 0` |
| Collection accessor (peek, min, max) | `TypeAccessor` | `NumericProofRequirement` | `SelfSubject(countAccessor)` | `> 0` |
| Dequeue / Pop | `ActionMeta` | `NumericProofRequirement` | `SelfSubject(countAccessor)` | `> 0` |
| Optional field access | `TypeAccessor` on optional field | `PresenceProofRequirement` | `SelfSubject()` | is set |
| Date + period dimension | `BinaryOperationMeta` date±period | `DimensionProofRequirement` | `ParamSubject(Period)` | Date dimension |
| Time + period dimension | `BinaryOperationMeta` time±period | `DimensionProofRequirement` | `ParamSubject(Period)` | Time dimension |

---

## Construct Slot Model

Precept has **no brace-delimited blocks** — all constructs are line-oriented declarations or arrow chains. This means the grammar is structurally simpler than languages with nested block syntax.

Each construct is modeled as an ordered sequence of **slots**:

```csharp
public record ConstructSlot(
    SlotKind  Kind,
    string    TextMateScope,
    string?   Pattern,
    bool      Required,
    bool      Repeatable
);
```

**SlotKind** values: `Keyword`, `Identifier`, `TypeRef`, `Expression`, `Modifier`, `Arrow`, `StateTarget`, `Separator`

Example — `FieldDeclaration`:

```
field Identifier ("," Identifier)* as TypeRef Modifier* ("->" Expr)?
```

Modeled as:
```
Keyword("field")  Identifier  Separator(",", repeatable)  Keyword("as")  TypeRef  Modifier(repeatable)  Arrow("->", optional)  Expression(optional)
```

**Why this matters:** Because Precept is entirely line-oriented, construct slot arrays can model 100% of the grammar. Slots reference other catalogs — `TypeRef` slots enumerate `Types.All` for type alternation, `Keyword` slots reference `Tokens`. The entire TextMate grammar is derivable from: Constructs (slot patterns) × Tokens (keyword alternations) × Types (type name alternations).

---

## Qualifier Registries (Not Catalogs)

Currency codes and measurement units are validated at type-check time, but they are NOT catalogs. They are **reference data registries**:

| Registry | Shape | Size | Source |
|----------|-------|------|--------|
| ISO 4217 currencies | `FrozenSet<string>` | ~180 codes | Static list of active currency codes |
| UCUM units | Compositional grammar | Unbounded | UCUM defines a compositional syntax for unit expressions |

**Why not catalogs:** These are not aspects of the *language* — they are aspects of the *data domain*. Adding a new currency code doesn't change the language; adding a new type keyword does. Currency codes don't have per-member metadata that consumers need; they have a single validation predicate ("is this a valid code?"). Catalogs describe the language surface; registries validate domain values.

---

## Syntax Reference

The 10 catalogs cover the language's *vocabulary* exhaustively. The language also has *grammar meta-rules* — singular facts about how source text is structured — that consumers need but that have no per-member enum. These are language-level constants, not catalogs.

`SyntaxReference` is a static class with typed properties, part of the same metadata-driven source of truth:

```csharp
public static class SyntaxReference
{
    public static string GrammarModel       => "line-oriented";
    public static string CommentSyntax      => "# to end of line";
    public static string IdentifierRules    => "Starts with letter, alphanumeric + underscore, case-sensitive";
    public static string StringLiteralRules => "Double-quoted, \\\" escape only, no interpolation";
    public static string NumberLiteralRules => "Integers (42), decimals (3.14), no hex/scientific/underscore separators";
    public static string WhitespaceRules    => "Not significant — indentation is cosmetic, line breaks separate declarations";
    public static string NullNarrowing      => "if Field is set narrows to non-nullable in the then branch";
    public static IReadOnlyList<string> ConventionalOrder => ["header", "fields", "rules", "states", "ensures", "accessModes", "events", "event ensures", "transitions", "state actions"];
}
```

**Consumers:**

| Consumer | How it reads |
|----------|-------------|
| MCP `precept_language` | Serializes to a `syntaxReference` JSON object in the response |
| Human reference docs | Generates a "Grammar Basics" section from the same properties |
| LS hover | Tooltip text for identifier tokens, comment tokens, etc. |
| AI grounding | Reads alongside catalog data for complete language understanding |

This is not a catalog — there is no enum, no `GetMeta()`, no `All`. It is structured metadata about the grammar as a whole, derived from the same codebase. No hand-written docs page that drifts from the implementation.

---

## Cross-Catalog Derivation

The test of completeness: every cell should trace back to a catalog, never to hardcoded consumer logic.

| Consumer surface | Catalogs read | How |
|------------------|---------------|-----|
| **TextMate grammar** | Constructs → Tokens → Types | **Generated** from catalog metadata. Construct slot arrays generate patterns; token keywords generate keyword alternations; type keywords via `Types.All` filtered by `Token.Text`. No hand-maintained alternation lists. Tests verify the generator produces correct output. |
| **MCP `precept_language`** | All 12 catalogs' `.All` + `SyntaxReference` | Union of all catalog enumerations IS the language spec. MCP tool iterates each and serializes. `SyntaxReference` adds grammar meta-rules. |
| **LS completions** | Tokens + Types + Functions + Modifiers + Actions | **Generated** from catalog metadata. Context-filtered: type position → `Types.All`; expression → `Functions.All`; after type → `Modifiers.All` filtered by `ApplicableTo`; event body → `Actions.All`. No hand-maintained completion lists. |
| **LS hover** | Types + Functions + Operators + Operations | Per-member descriptions from catalog metadata via `Token.Text` and `Description` |
| **LS semantic tokens** | Tokens (via `TokenMeta.Categories`) | Token categories map directly to semantic token types |
| **Type checker validation** | Types + Functions + Operations + Modifiers + Actions | Catalog lookups replace hand-coded validation logic: modifier applicability → `Modifiers.GetMeta().ApplicableTo`; function signatures → `Functions.GetMeta()`; operation legality → `Operations.Resolve()`; type keyword resolution → `Types.All` frozen dictionary keyed by `Token.Kind` |
| **Parser vocabulary** | Operators + Types + Modifiers + Actions + Constructs | Frozen dictionaries derived from catalogs at startup: `Operators.All` → precedence table; `Types.All` → type keyword mapping; `Modifiers.All` → recognition sets; `Actions.All` → action keywords. No hand-maintained vocabulary tables. |
| **Evaluator dispatch** | Functions + Operations | Evaluator dispatches by operation kind and function kind. No parallel dispatch tables. Execution delegate design is deferred pending working copy API design. |
| **Runtime boundary validation** | Modifiers | `FieldModifierMeta.ApplicableTo` and `HasValue` drive boundary checks. No `switch` on `ModifierKind`. |
| **Reference documentation** | All 10 language definition catalogs + `SyntaxReference` | **Generated** from catalog metadata. Tables, syntax sections, grammar reference all derived from `All` properties. |
| **AI grounding** | All 12 catalogs + `SyntaxReference` | Complete, always-accurate language reference — AI grounded on catalog output cannot hallucinate features |

No consumer surface maintains its own parallel list. Every fact comes from a catalog `All` property, `GetMeta()` call, or `SyntaxReference` property. TextMate grammar, LS completions, and reference documentation are **generated** from catalogs — not hand-maintained. Tests verify the generators produce correct output.

---

## Future Opportunities

These are enabled by the catalog system but not part of the initial implementation:

1. **Error message enrichment** — diagnostics that cross-reference catalog entries (e.g., "did you mean `MoneyType`?" suggestions from `Types.All`).
2. **Quick fixes / code actions** — LS code actions derived from catalog metadata (e.g., suggest valid modifiers for a type from `Modifiers.All` filtered by `ApplicableTo`).
3. **Parser validation** — construct slot arrays as test oracle. Generate exhaustive parser test inputs from slot permutations.
4. **Version diffing** — catalog snapshots as changelog. Diff two versions of `All` to produce a human-readable changelog of language surface changes.
5. **Playground / explorer UI** — all catalog-derived. Browse the language interactively from the catalog data.

---

## Test Strategy

Catalogs are the language specification in machine-readable form. Tests verify that the specification is correct, complete, and that all generated artifacts match.

### Non-negotiable rules

1. **No catalog without snapshot test.** Every catalog's `All` property is snapshotted to a golden file. Any change to the catalog requires explicit snapshot update. This catches unintended metadata changes.

2. **Exhaustive matrix green before old logic deleted.** When a catalog replaces hand-coded logic (e.g., `Operations` replaces `OperatorTable`), the new catalog-driven path must have complete test coverage before the old procedural path is removed. Clean replacement — not parallel old+new.

3. **Property-based test generation from catalogs.** The catalogs ARE the test oracle. Tests are generated from catalog metadata:
   - Every `(OperatorKind × TypeKind × TypeKind)` combination tested for legality against `Operations.All`
   - Every `(ModifierKind × TypeKind)` combination tested for applicability against `Modifiers.All`
   - Every `FunctionOverload` tested with valid and invalid argument types
   - Every `TypeAccessor` tested for return type correctness
   - Every cross-catalog reference validated (e.g., every `TypeMeta.Token.TypeKind` equals `TypeMeta.Kind`)
   - Projected: ~15,500+ auto-generated test cases

4. **Generated artifact tests.** TextMate grammar, LS completions, and MCP vocabulary are generated from catalogs. Tests verify the generators produce correct output — not that hand-maintained copies match. There are no hand-maintained copies to drift.

### Cross-catalog integrity tests

| Invariant | Test |
|-----------|------|
| Every type has a valid token | `Types.All.All(t => t.Token.Categories.Contains(TokenCategory.Type))` |
| Every operator has a valid token | `Operators.All.All(o => o.Token.Categories.Contains(TokenCategory.Operator))` |
| Token→Type index is complete | `Types.All.Select(t => t.Token.Kind).Distinct().Count() == Types.All.Count` (no two types share a token) |
| Widening acyclicity | No circular chains in `TypeMeta.WidensTo` |
| Subsumption acyclicity | No circular chains in `FieldModifierMeta.Subsumes` |
| `ParamSubject` referential integrity | Every `ParamSubject.Parameter` is reference-equal to a `ParameterMeta` in the containing overload/operation |
| Proof requirement type validity | `DimensionProofRequirement` only on `BinaryOperationMeta`, etc. (per PRECEPT0006 rules) |

---

## Pipeline Stage Impact

As catalogs are implemented, each pipeline stage gets thinner — domain knowledge migrates from hand-coded logic into metadata, and stages become generic machinery that reads catalog data.

| Stage | Current state | Catalog impact |
|-------|--------------|----------------|
| **Lexer** | Already uses `Tokens.Keywords` for keyword classification | Minimal further impact. Operator scan priority derivable from `Operators.All` sorted by `Token.Text.Length` descending. |
| **Parser** | Hand-coded vocabulary tables + recursive descent grammar | Vocabulary tables — operator precedence, type keyword mappings, modifier/action recognition sets (~40–50% of language knowledge decisions) — migrate to catalog-derived frozen dictionaries at startup. Grammar productions stay hand-written. Construct slots enable test generation and LS completions. When a new type, modifier, operator, or action is added to a catalog, the parser adapts automatically — no parser edit needed. |
| **TypeChecker** | Hand-coded modifier validation, function dispatch, operator dispatch | Significant: modifier applicability → `Modifiers.GetMeta().ApplicableTo`, function validation → `Functions.GetMeta()`, operation legality → `Operations.Resolve()`, type keyword resolution → `Types.All` frozen dictionary. The type checker's `switch` forests shrink to catalog lookups. |
| **GraphAnalyzer** | Hand-coded state reachability, modifier semantics | Moderate: state modifier structural semantics (`AllowsOutgoing`, `RequiresDominator`, `PreventsBackEdge`) are catalog metadata on `StateModifierMeta`. Graph algorithms (reachability, dominator trees, SCC) remain generic machinery. |
| **ProofEngine** | Hand-coded proof obligations per operator | Significant: `ProofRequirement[]` on `BinaryOperationMeta`, `FunctionOverload`, `TypeAccessor`, and `ActionMeta` carry all proof obligations as metadata. Proof engine reads catalog entries — no hardcoded obligation lists. |
| **Evaluator** | Not yet implemented | Execution delegate design deferred pending working copy API design. When implemented: operation execution dispatches via `Operations.Resolve()`; function execution dispatches via `Functions.GetMeta()`; modifier boundary validation reads `FieldModifierMeta.ApplicableTo`/`HasValue`. Action execution delegates deferred until working copy API designed. The evaluator's core loop (expression tree walking, working copy, atomicity) remains hand-written. |

Pattern: domain knowledge → metadata. Stages → generic machinery that reads catalogs.
