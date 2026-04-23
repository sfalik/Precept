# Catalog System

> **Status:** Draft
> **Grounding:** Implemented catalogs in `src/Precept.Next/Pipeline/` (Tokens, Diagnostics) and `src/Precept.Next/Runtime/` (Faults). Prototype reference: `docs/CatalogInfrastructureDesign.md` (v1 3-tier catalog). Coherence reference: PR #133 `docs/ArchitecturalCoherenceDesign.md`, `docs/ComponentAlignmentInventory.md`.
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

These questions require **per-member metadata** — not just the enum value, but a description, a text representation, a classification, a message template. Without a structured pattern, each consumer builds its own parallel copy of this metadata. The component alignment inventory (PR #133) documents 50 alignment edges in v1 and identifies parallel copies as the primary source of drift.

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

The v1 prototype uses `[TokenCategory]`, `[TokenDescription]`, `[TokenSymbol]` attributes on enum members, with reflection-driven derivation at startup. That pattern works, and it was the right call for v1 — it allowed incremental adoption without restructuring the enum. But it has a structural weakness: **a missing attribute is a runtime failure, not a compile-time failure.** The v1 solution was a Roslyn analyzer (PREC003) to enforce attribute presence at build time — two layers doing related work.

The exhaustive switch uses the C# compiler directly:

- Add an enum member without a switch arm → **CS8509** (non-exhaustive switch expression). With `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` → **build fails**.
- No custom Roslyn rule needed for catalog completeness.
- No reflection. No source generator. No startup cost. No registration side effects.
- The switch IS the catalog. The compiler IS the enforcement.

The trade-off: the switch is more verbose than attributes (one line per member vs. one attribute per member). At Precept's scale (50–170 enum members per catalog), this is acceptable — the switch is a single, greppable, self-contained artifact.

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

The function catalog closes the highest-drift edges in the v1 alignment inventory (E13–E16). Today, 6 consumers independently maintain function knowledge: `FunctionRegistry` (source of truth), TextMate grammar (hardcoded alternation), LS completions (hardcoded list), LS hover (hardcoded `FunctionHoverContent` dictionary), evaluator (hand-coded function switch), and type checker (reads registry — already structural).

The v2 catalog will carry: name, description, parameter types (via `TypeKind`), return type, overloads, and an evaluation delegate. The evaluation delegate is what eliminates the evaluator's parallel copy — the evaluator dispatches through the catalog, not through its own switch. This is the v2 equivalent of what the v1 coherence design calls DD24 (registry-driven evaluation).

Blocked on `TypeKind` — you can't define `abs(number) → number` without a type system.

### Operators (planned)

The operator catalog closes E18 — the zero-enforcement boundary between the type checker's operator rules and the evaluator's operator dispatch. Today these are independent implementations that must agree but have no structural link.

The v2 catalog will map `(TokenKind operator, TypeKind lhs, TypeKind rhs) → (TypeKind result, EvalDelegate)`. Both the type checker and evaluator consume the same data. This is DD25 (operator registry) from the v1 coherence design.

Blocked on `TypeKind` for the same reason as Functions.

### Constructs (different shape)

Parser constructs (grammar forms with examples and descriptions) will exist in v2 but likely won't follow the `Enum + Meta + Static` pattern. Constructs are registered by the parser at parse time, not predefined as a closed enum — new grammar rules add construct entries through a registration API. The v1 `ConstructCatalog` uses this approach and it works well. This is a catalog in the informal sense (an enumerable registry) but not in the formal pattern this document defines.

## Relationship to Coherence Architecture

The component alignment inventory (PR #133) identifies seven "authoritative registries" in v1. Three of these map directly to v2 catalogs:

| v1 Registry | v2 Catalog | Alignment edges eliminated |
|-------------|------------|--------------------------|
| `PreceptToken` + attributes | Tokens | E1–E4 (vocabulary derivation) |
| `DiagnosticCatalog` | Diagnostics | E22–E23 (MCP constraints, LS codes) |
| `FunctionRegistry` | Functions (planned) | E13–E16 (grammar, LS, evaluator) |

The remaining v1 registries (`StaticValueKind`, `PreceptModel`, `ConstructCatalog`, `ConstraintViolation`) are internal types or registration-based collections — they serve the pipeline but don't need the full catalog pattern.

The coherence design doc's thesis is that the language definition is the single source of truth and every component must derive from it. Catalogs are the mechanism for the vocabulary, diagnostic, fault, function, and operator layers. The exhaustive switch is the enforcement. The `All` property is the enumeration surface. The `Create()` factory is the derivation path. Together they eliminate the parallel-copy drift that the alignment inventory documents.
