# Catalog System

> **Status:** Draft
> **Implemented in:** `src/Precept.Next/Pipeline/` (Tokens, Diagnostics), `src/Precept.Next/Runtime/` (Faults)
> **Related:** `docs.next/compiler/diagnostic-system.md` (Diagnostics catalog detail), `docs.next/runtime/fault-system.md` (Faults catalog detail), `docs.next/compiler/pipeline-artifacts-and-consumer-contracts.md` (pipeline stage model)

## Overview

Several of Precept's core enums serve as **authoritative registries** — closed vocabularies whose members must be individually described, enumerated by external surfaces, and consumed by multiple independent components. The catalog pattern is the structural convention that governs how these registries are defined, how their metadata is accessed, and how the C# compiler enforces completeness.

This document defines the pattern itself, the criteria for when an enum earns a catalog, the naming conventions, and the inventory of all catalogs in the system.

## The Problem the Pattern Solves

Precept has consumer surfaces — MCP tools, language server, TextMate grammar — that need to answer questions like:

- *What are all the keywords in the language?* (MCP `precept_language` vocabulary)
- *What are all the diagnostic rules?* (MCP `precept_language` constraints, LS diagnostic codes)
- *What semantic token type does this token map to?* (LS semantic tokens)
- *What hover text should this function show?* (LS hover)

These questions require **per-member metadata** — not just the enum value, but a description, a text representation, a classification, a message template. Without a structured pattern, each consumer builds its own parallel copy of this metadata. Parallel copies drift — a new keyword added to the enum but missing from one consumer's hardcoded list is invisible until a user hits the gap.

The catalog pattern eliminates parallel copies by making the metadata a single static artifact that consumers read from, rather than independently maintain.

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

An immutable record holding all metadata for a single enum member. The shape varies per catalog — each Meta type carries exactly the fields its consumers need, no more. Shared fields across all catalogs:

| Field | Purpose |
|-------|---------|
| The kind enum value | Identity — which member this metadata describes |
| A string code or text | Stable string identity for serialization (MCP, LS) |
| A description or message template | Human-readable explanation |

Domain-specific fields are added as needed. `TokenMeta` has `Categories` and nullable `Text`. `DiagnosticMeta` has `Stage` and `Severity`. `FaultMeta` has only `Code` and `MessageTemplate`. The meta type is right-sized for its consumers.

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

    public static Diagnostic Create(DiagnosticCode code, SourceRange range, params object?[] args)
    {
        var meta = GetMeta(code);
        return new(meta.Severity, meta.Stage, meta.Code,
            string.Format(meta.MessageTemplate, args), range);
    }
}
```

This is the core of the pattern. Three members:

| Member | Purpose | Present in all catalogs |
|--------|---------|------------------------|
| `GetMeta(Kind)` | Exhaustive switch expression mapping every enum member to its metadata | Yes — this is the catalog |
| `All` | `IReadOnlyList<Meta>` built from `Enum.GetValues<Kind>().Select(GetMeta)` | Yes — MCP enumeration surface |
| `Create(...)` | Factory that formats a runtime output value from the metadata | When the catalog has an associated output type |

Domain-specific members are added as needed. `Tokens` has a `Keywords` frozen dictionary for lexer keyword lookup. These are derived from `All` or `GetMeta`, never independently maintained.

### 4. Output value type — what the pipeline produces

```csharp
public readonly record struct Diagnostic(
    Severity        Severity,
    DiagnosticStage Stage,
    string          Code,
    string          Message,
    SourceRange     Range
);
```

A `readonly record struct` — the type that pipeline stages and the runtime produce as output. Constructed exclusively through the catalog's `Create()` factory, which derives fields from the meta. Not all catalogs have an output type — `Tokens.All` is consumed directly as metadata, with `Token` being the lexer's output type rather than a catalog-produced value.

## Why Exhaustive Switch, Not Attributes + Reflection

An alternative approach is to decorate enum members with attributes (`[TokenCategory]`, `[TokenDescription]`) and read them via reflection at startup. That works, but has a structural weakness: **a missing attribute is a runtime failure, not a compile-time failure.** Closing the gap requires a custom Roslyn analyzer to enforce attribute presence at build time — two layers doing related work.

The exhaustive switch uses the C# compiler directly:

- Add an enum member without a switch arm → **CS8509** (non-exhaustive switch expression). With `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` → **build fails**.
- No custom Roslyn rule needed for catalog completeness.
- No reflection. No source generator. No startup cost. No registration side effects.
- The switch IS the catalog. The compiler IS the enforcement.

The trade-off: the switch is more verbose than attributes (one line per member vs. one attribute per member). At Precept's scale (50–170 enum members per catalog), this is acceptable — the switch is a single, greppable, self-contained artifact.

## Roslyn Enforcement Layer

The exhaustive switch enforces **catalog completeness** — every enum member must have metadata. But catalogs have a second invariant: **construction discipline**. Output values (`Diagnostic`, `Fault`) carry string fields derived from the metadata via `nameof()` in the switch. If code constructs these types directly (bypassing the `Create()` factory), it can introduce arbitrary strings that escape the registry. Roslyn analyzers enforce this at build time.

### Implemented Rules

| Rule | Invariant | What it prevents |
|------|-----------|-----------------|
| **PRECEPT0001** | `Fail()` must pass a `FaultCode` as its first argument | Unclassified evaluator failure paths — a bare `Fail("some message")` bypasses the FaultCode chain |
| **PRECEPT0002** | Every `FaultCode` member must carry `[StaticallyPreventable(DiagnosticCode.X)]` | A runtime fault with no corresponding compile-time diagnostic — breaks the guarantee that every fault is preventable |
| **PRECEPT0003** | `Diagnostic` must be constructed via `Diagnostics.Create()` | Direct `new Diagnostic(...)` construction with arbitrary string codes that escape the registry |
| **PRECEPT0004** | `Fault` must be constructed via `Faults.Create()` | Direct `new Fault(...)` construction with arbitrary string codes that escape the registry |

All four are `DiagnosticSeverity.Error` with `isEnabledByDefault: true`. Combined with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, they are build-breaking.

### Two-Layer Enforcement Model

The catalog pattern uses two enforcement layers, each covering a different class of invariant:

| Layer | Mechanism | What it enforces | Example failure |
|-------|-----------|-----------------|-----------------|
| **Compiler** | CS8509 (exhaustive switch) | Every enum member has a metadata entry | Add `DiagnosticCode.NewRule` without a `GetMeta()` arm → build fails |
| **Roslyn** | PRECEPT0001–PRECEPT0004 | Output values go through the factory; cross-catalog linkage is present | `new Diagnostic(...)` bypassing `Diagnostics.Create()` → build fails |

The compiler layer is free — it comes from the language. The Roslyn layer is the cost of the pattern: four analyzers, four test files, one analyzer project. At Precept's scale this is proportional — each rule covers a single invariant on a single type.

### Why Token Has No Construction Rule

`Token` is produced by the lexer from scan state (line, column, offset, length) — there is no metadata-derived string field that could be bypassed. The token catalog's role is metadata lookup and keyword classification (`Tokens.GetMeta()`, `Tokens.Keywords`), not construction. The lexer creates `Token` values directly, and that's correct.

### Future Rules

When the Functions and Operators catalogs land, they will likely need dispatch-enforcement rules:

| Planned rule | Invariant |
|-------------|-----------|
| Functions: evaluator must dispatch through catalog | Evaluator calls the catalog's evaluation delegate, not a hand-coded function switch |
| Operators: evaluator must dispatch through catalog | Evaluator calls the catalog's operator dispatch table, not independent type×operator logic |

These cannot be written until the evaluator and the catalogs exist. The rule shape will depend on whether dispatch is via delegate lookup (data-driven — Roslyn rule enforces no direct `switch` on function name) or via interface call (structural — no rule needed if the dispatch point is the only call site).

## When an Enum Earns a Catalog

Not every enum needs a catalog. The criteria:

| Criterion | Required? | Why |
|-----------|-----------|-----|
| **Multiple external consumers** need per-member metadata | Yes | One consumer can just switch locally. Two or more consumers means the metadata is shared infrastructure. |
| **MCP or LS enumeration** — a consumer iterates all members to present them | Yes | Without `All`, each consumer builds its own list. |
| **Per-member metadata** beyond the enum name | Yes | If the only thing consumers need is the enum value itself, a switch in the consumer suffices. |

If all three are true, the enum earns a catalog. If not, it stays a bare enum with local switches where needed.

### Enums that do NOT earn catalogs

| Enum | Why bare |
|------|----------|
| `DiagnosticStage` | 5 values, no per-member metadata, consumers reference them directly |
| `Severity` | 3 values, no per-member metadata |
| `TokenCategory` | 17 values. Consumed as a classification axis, not individually described. No consumer iterates categories to present them — they iterate tokens grouped by category. |
| `TypeKind` (future) | Internal type-inference currency. No external surface enumerates type kinds. |

## Naming Convention

| Part | Convention | Examples |
|------|-----------|----------|
| Kind enum | `{Thing}Kind` or `{Thing}Code` | `TokenKind`, `DiagnosticCode`, `FaultCode` |
| Meta record | `{Thing}Meta` | `TokenMeta`, `DiagnosticMeta`, `FaultMeta` |
| Catalog class | `{Thing}s` (plural) | `Tokens`, `Diagnostics`, `Faults` |
| Output value | `{Thing}` (singular) | `Token`, `Diagnostic`, `Fault` |

**Kind vs Code:** Use `Kind` when the enum classifies a *kind of thing* (tokens have kinds). Use `Code` when the enum identifies a *rule or failure mode* (diagnostics have codes, faults have codes). The distinction is semantic — both serve as the catalog's primary key.

## File Layout

Each part is a separate file, co-located with the pipeline stage or runtime layer it belongs to:

```
src/Precept.Next/
  Pipeline/
    TokenKind.cs          # enum
    TokenCategory.cs      # supporting enum (not a catalog)
    Token.cs              # TokenMeta record + Token output struct
    Tokens.cs             # static catalog class

    DiagnosticCode.cs     # enum
    Diagnostic.cs         # DiagnosticMeta record + supporting types + Diagnostic output struct
    Diagnostics.cs        # static catalog class

  Runtime/
    FaultCode.cs          # enum
    Fault.cs              # Fault output struct
    Faults.cs             # FaultMeta record + static catalog class
```

Catalogs live alongside their pipeline stage, not in a separate folder. The token catalog lives with the lexer. The diagnostic catalog lives with the compiler pipeline. The fault catalog lives with the evaluator/runtime. When `Pipeline/` grows beyond ~30 files, the right split is by stage (`Pipeline/Lexing/`, `Pipeline/Analysis/`), not by file shape.

## Catalog Inventory

| Catalog | Kind Enum | Stage | Consumers | Status |
|---------|-----------|-------|-----------|--------|
| **Tokens** | `TokenKind` | Lexer | MCP vocabulary, LS semantic tokens, LS completions, lexer keyword lookup | ✅ Implemented |
| **Diagnostics** | `DiagnosticCode` | All pipeline stages | MCP constraints, LS diagnostic codes, drift tests | ✅ Implemented |
| **Faults** | `FaultCode` | Runtime | MCP fire/inspect, runtime outcome reporting | ✅ Implemented |
| **Functions** | `FunctionKind` | Type checker + Evaluator | MCP vocabulary, LS completions, LS hover, type checker overload resolution, evaluator dispatch | ⏳ Blocked on `TypeKind` |
| **Operators** | `OperatorKind` | Type checker + Evaluator | Type checker inference, evaluator dispatch, MCP vocabulary | ⏳ Blocked on `TypeKind` |

### Functions (planned)

The function catalog serves the most consumers of any planned catalog. Six surfaces need function knowledge: the type checker (overload resolution and argument validation), the evaluator (runtime dispatch), MCP vocabulary (function list and descriptions), LS completions (function names in expression context), LS hover (function documentation), and the TextMate grammar (syntax highlighting for function calls).

The catalog will carry: name, description, parameter types (via `TypeKind`), return type, overloads, and an evaluation delegate. The evaluation delegate eliminates a parallel copy in the evaluator — the evaluator dispatches through the catalog rather than maintaining its own independent function switch.

Blocked on `TypeKind` — you can't define `abs(number) → number` without a type system.

### Operators (planned)

The operator catalog closes the boundary between the type checker's operator rules and the evaluator's operator dispatch. Without it, these are independent implementations that must agree on which operator × type combinations are legal — but have no structural link ensuring they do.

The catalog will map `(TokenKind operator, TypeKind lhs, TypeKind rhs) → (TypeKind result, EvalDelegate)`. Both the type checker and evaluator consume the same dispatch table. The type checker reads it to infer result types; the evaluator reads it to execute.

Blocked on `TypeKind` for the same reason as Functions.

### Constructs (different shape)

Parser constructs (grammar forms with examples and descriptions) will exist but likely won't follow the `Enum + Meta + Static` pattern. Constructs are registered by the parser at parse time, not predefined as a closed enum — new grammar rules add construct entries through a registration API. This is a catalog in the informal sense (an enumerable registry consumed by MCP and LS) but not in the formal pattern this document defines.

## Design Principle: Derive, Never Duplicate

The catalog system exists to enforce a single principle: **the language definition is the single source of truth, and every component that needs language knowledge must derive from it.**

The five catalogs (Tokens, Diagnostics, Faults, Functions, Operators) cover the vocabulary, rules, failure modes, function library, and operator table. The exhaustive switch is the enforcement — the C# compiler refuses to build if any member is missing. The `All` property is the enumeration surface — MCP and LS iterate it rather than maintaining their own lists. The `Create()` factory is the derivation path — pipeline stages produce output values from the catalog rather than constructing them ad hoc.

Enums that don't meet the catalog threshold (`DiagnosticStage`, `Severity`, `TokenCategory`, `TypeKind`) stay bare — consumers switch on them locally. The pattern is reserved for registries where parallel copies would otherwise drift.
