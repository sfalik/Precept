# Catalog Infrastructure Design

Date: 2025-03-05
Status: Design complete — implementation tracked in `docs/PreceptLanguageImplementationPlan.md`

---

## Problem: N-Copy Grammar Sync

Language knowledge today is scattered across multiple components, each with its own copy of the grammar:

| Component | What it knows | How it knows |
|---|---|---|
| Parser (`PreceptParser.cs`) | Keywords, statement forms, constraints | 20+ compiled regexes, imperative code |
| Expression parser (`PreceptExpressionParser.cs`) | Operators, precedence, identifier rules | Hand-written lexer + recursive descent |
| Runtime (`PreceptRuntime.cs`) | Constraint enforcement, fire pipeline | Imperative validation code, ad-hoc error strings |
| Semantic tokens (`PreceptSemanticTokensHandler.cs`) | Keyword lists, operator patterns | 12+ regex patterns per line |
| Completions (`PreceptAnalyzer.cs`) | Keywords by context, identifier scoping | 8+ regex patterns for context detection |
| TextMate grammar (`precept.tmLanguage.json`) | Keyword alternations, operator patterns | JSON regex patterns |
| Docs (`DesignNotes.md`, `README.md`) | Full syntax contract, constraint list | Prose |

Adding a keyword requires touching **7 files** and hoping nothing drifts. The MCP `precept_language` tool would add an 8th copy — or worse, a hand-maintained JSON file that drifts from all the others.

---

## Architecture: Three-Tier Catalog in Core

The solution is to make the language knowledge **data** that lives in one place (`src/Precept/`) and is **consumed** by every component that needs it. Three tiers, from cheapest to richest:

```
┌─────────────────────────────────────────────────────┐
│  Tier 1: Vocabulary                                 │
│  PreceptToken enum + [TokenCategory/Description/    │
│  Symbol] attributes                                 │
│  → tokenizer keyword dict, semantic tokens,         │
│    completions grouping, MCP vocabulary              │
├─────────────────────────────────────────────────────┤
│  Tier 2: Constructs                                 │
│  ConstructCatalog — parser combinators register      │
│  syntax templates + descriptions + examples          │
│  → parser errors, LS hovers, LS completions,         │
│    MCP constructs                                    │
├─────────────────────────────────────────────────────┤
│  Tier 3: Semantics                                  │
│  ConstraintCatalog — enforcement points register     │
│  ID + phase + rule + message template + severity     │
│  → error messages, LS diagnostics (with codes),      │
│    MCP constraints                                   │
└─────────────────────────────────────────────────────┘
```

**Key principle:** All three tiers live in `src/Precept/Dsl/` — they are core infrastructure, not MCP-specific. The MCP server serializes them; the language server consumes them for hovers, completions, and diagnostics; the parser and runtime use them for error messages. Deleting the MCP project (`tools/Precept.Mcp/`) removes only the serialization layer — every other benefit remains.

---

## Tier 1: Token Attributes

Each member of the `PreceptToken` enum carries three attributes:

### `[TokenCategory]`

Classifies each token into a semantic group.

```csharp
public enum TokenCategory
{
    Control,       // precept, state, initial, from, on, when, any, in, to, of, with
    Declaration,   // field, as, nullable, default, invariant, because, event, assert
    Action,        // set, add, remove, enqueue, dequeue, push, pop, clear, into
    Outcome,       // transition, no, reject
    Type,          // string, number, boolean
    Literal,       // true, false, null
    Operator,      // ==, !=, >, >=, <, <=, &&, ||, !, contains, =
    Punctuation,   // ->, comma, dot, (, ), [, ]
    Structure,     // comment, newline
    Value          // identifier, string literal, number literal
}
```

**Consumers:**
- **MCP `precept_language`:** Groups vocabulary by category in the JSON response
- **Language server completions:** Groups keyword suggestions semantically instead of alphabetically
- **Semantic tokens:** Maps categories to VS Code token types (Control/Declaration → `keyword`, Type → `type`, etc.)

### `[TokenDescription]`

Human-readable purpose of each token. One sentence, no period.

```csharp
[TokenCategory(TokenCategory.Declaration)]
[TokenDescription("Declares a data field")]
Field,
```

**Consumers:**
- **MCP `precept_language`:** Included in vocabulary entries
- **Language server completions:** Detail text alongside keyword suggestions
- **Parser error messages:** `"Expected: 'field' (Declares a data field)"`

### `[TokenSymbol]`

The text representation of the token as it appears in source code. This bridges the gap between the enum member name (which is a valid C# identifier like `DoubleEquals`) and the actual source text (`==`).

```csharp
[TokenSymbol("==")]
DoubleEquals,

[TokenSymbol("field")]
Field,

[TokenSymbol("->")]
Arrow,

[TokenSymbol("contains")]
Contains,
```

**Derivation rules:**
- Every keyword token's symbol is its lowercase text (`Field` → `"field"`, `GreaterThan` → `">"`)
- Operator and punctuation tokens use their actual symbol (`DoubleEquals` → `"=="`, `Arrow` → `"->"`)
- Value tokens (`Identifier`, `StringLiteral`, `NumberLiteral`) and structure tokens (`Comment`, `NewLine`) either omit the attribute or use a descriptive placeholder

**Consumers:**
- **Tokenizer:** The keyword recognition dictionary can be built by reflecting `[TokenSymbol]` attributes instead of a hand-maintained dictionary. Adding a keyword to the enum automatically adds it to the tokenizer — zero drift.
- **Semantic tokens:** The symbol-driven mapping replaces hardcoded string comparisons
- **MCP `precept_language`:** Operator symbols and keyword text come directly from the attribute
- **Error messages:** `"Expected '==' (Equality comparison)"` — symbol from attribute, description from `[TokenDescription]`

### Attribute-Driven Derivation

With all three attributes, several currently hand-maintained data structures become **derived at startup via reflection**:

| Data structure | Currently | With attributes |
|---|---|---|
| Tokenizer keyword dictionary | Hand-written `Dict<string, PreceptToken>` | Reflect `[TokenSymbol]` on all keyword-category tokens |
| Semantic token type mapping | `switch` expression with hardcoded cases | Reflect `[TokenCategory]` → VS Code token type mapping |
| Completion keyword grouping | Manual lists per context | Filter `PreceptToken` members by `[TokenCategory]` |
| MCP vocabulary JSON | Hand-maintained or hand-assembled | Reflect all three attributes → serialize |

---

## Tier 2: Construct Catalog

### Registry

**File:** `src/Precept/Dsl/ConstructCatalog.cs`

```csharp
public sealed record ConstructInfo(
    string Name,
    string Form,
    string Context,
    string Description,
    string Example);

public static class ConstructCatalog
{
    private static readonly List<ConstructInfo> _constructs = [];
    public static IReadOnlyList<ConstructInfo> Constructs => _constructs;

    public static TokenListParser<PreceptToken, T> Register<T>(
        this TokenListParser<PreceptToken, T> parser, ConstructInfo info)
    {
        _constructs.Add(info);
        return parser;
    }
}
```

### Registration Pattern

Each parser combinator registers itself with its syntax template, context, description, and a **parseable** working example:

```csharp
static readonly TokenListParser<PreceptToken, DslField> FieldDecl =
    ( /* combinator chain */ )
    .Register(new ConstructInfo(
        "field-declaration",
        "field <Name> as <Type> [nullable] [default <Value>]",
        "top-level",
        "Declares a scalar data field tracked across events.",
        "field Priority as number default 3"));
```

The example must parse successfully — this is validated by a test (see Drift Defense below).

### Consumer Wiring

| Consumer | How it uses ConstructCatalog |
|---|---|
| **Parser error messages** | On parse failure for a specific combinator, the error message includes the construct's `Form`: `"Invalid field declaration. Expected: field <Name> as <Type> [nullable] [default <Value>]"` |
| **Language server hovers** | When the cursor is on a keyword that starts a construct, show the `Form` + `Description` as hover content |
| **Language server completions** | Statement-starting keyword suggestions include `Description` as detail text. The keyword list is derived from constructs whose `Context` is `"top-level"` |
| **MCP `precept_language`** | Serializes `ConstructCatalog.Constructs` directly into the `constructs` array of the JSON response |

---

## Tier 3: Constraint Catalog

### Registry

**File:** `src/Precept/Dsl/ConstraintCatalog.cs`

```csharp
public sealed record LanguageConstraint(
    string Id,
    string Phase,
    string Rule,
    string MessageTemplate,
    ConstraintSeverity Severity);

public enum ConstraintSeverity
{
    Error,
    Warning
}

public static class ConstraintCatalog
{
    private static readonly List<LanguageConstraint> _constraints = [];
    public static IReadOnlyList<LanguageConstraint> Constraints => _constraints;

    public static LanguageConstraint Register(
        string id, string phase, string rule,
        string messageTemplate, ConstraintSeverity severity = ConstraintSeverity.Error)
    {
        var c = new LanguageConstraint(id, phase, rule, messageTemplate, severity);
        _constraints.Add(c);
        return c;
    }
}
```

### `MessageTemplate`

Each constraint carries a message template with placeholders for contextual values. The enforcement code fills in the placeholders — the constraint description (`Rule`) and the error message come from the **same data**, eliminating duplication:

```csharp
// SYNC:CONSTRAINT:C7
static readonly LanguageConstraint C7 = ConstraintCatalog.Register(
    "C7", "parse",
    "Non-nullable fields without 'default' are a parse error.",
    "Field '{fieldName}' is non-nullable and has no default value.",
    ConstraintSeverity.Error);

if (!field.HasDefault && !field.IsNullable)
    throw new ParseException(C7.FormatMessage(new { fieldName = field.Name }));
```

The `Rule` property describes the constraint in general terms (for documentation and MCP). The `MessageTemplate` property produces the specific error message for a violation (for parser/compiler/engine diagnostics).

### `Severity`

Most constraints are `Error` (hard failures). The `Severity` field allows future `Warning`-level constraints (e.g., "unreachable transition row" could be a warning rather than an error). The language server maps severity to LSP `DiagnosticSeverity`.

### Registration Pattern

Each enforcement point is marked with a `// SYNC:CONSTRAINT:Cnn` comment and registers the constraint as a static field:

```csharp
// SYNC:CONSTRAINT:C9
static readonly LanguageConstraint C9 = ConstraintCatalog.Register(
    "C9", "parse",
    "Each (state, event) pair may only appear in transition rows that share compatible guards — no duplicate unguarded rows.",
    "Duplicate unguarded transition row for ({stateName}, {eventName}).",
    ConstraintSeverity.Error);
```

### Consumer Wiring

| Consumer | How it uses ConstraintCatalog |
|---|---|
| **Parser/compiler/engine error messages** | Error messages are derived from `MessageTemplate` with contextual values filled in. The constraint `Rule` is available as supplementary context. |
| **Language server diagnostics** | Each diagnostic carries the constraint ID as a diagnostic code (e.g., `PRECEPT007` from `C7`). Severity maps to LSP `DiagnosticSeverity`. The `Rule` text becomes the diagnostic message or detail. |
| **MCP `precept_language`** | Serializes `ConstraintCatalog.Constraints` into the `constraints` array (ID, phase, rule). `MessageTemplate` and `Severity` are available but may be omitted from the MCP response for brevity. |
| **`copilot-instructions.md`** | The existence of SYNC comments is documented as a rule — Copilot knows to maintain them when editing enforcement code. |

### Diagnostic Code Derivation

Constraint IDs map to LSP diagnostic codes with a simple transform:

```
C7  → PRECEPT007
C13 → PRECEPT013
```

This gives every parser/compiler/runtime error a stable, searchable code. The language server produces:

```json
{
  "range": { "start": { "line": 5, "character": 0 }, "end": { "line": 5, "character": 25 } },
  "severity": 1,
  "code": "PRECEPT007",
  "source": "precept",
  "message": "Field 'Email' is non-nullable and has no default value."
}
```

---

## Consumer Matrix

Summary of which components consume which catalog tier:

| Component | Tier 1: Vocabulary | Tier 2: Constructs | Tier 3: Constraints |
|---|---|---|---|
| Tokenizer | **Keyword dictionary** (derived from `[TokenSymbol]`) | — | — |
| Parser | (tokens are its input) | **Error messages** (construct forms) | **Error messages** (constraint templates) |
| Compiler | — | — | **Error messages** (constraint templates) |
| Runtime (engine) | — | — | **Error messages** (constraint templates) |
| Semantic tokens | **Token type mapping** (from `[TokenCategory]`) | — | — |
| Completions | **Keyword groups** (from `[TokenCategory]`) + **descriptions** (from `[TokenDescription]`) | **Hover content** (forms + descriptions) | — |
| Diagnostics | — | — | **Diagnostic codes** + **severity** + **messages** |
| MCP `precept_language` | **Vocabulary JSON** (all three attributes) | **Constructs JSON** (full entries) | **Constraints JSON** (ID + phase + rule) |
| TextMate grammar | (independent — regex patterns, synced by copilot-instructions rules) | — | — |

---

## Drift Defense

Three complementary layers prevent catalog information from going stale:

### Layer 1: SYNC Comments (Copilot-Visible)

Every enforcement point is marked with `// SYNC:CONSTRAINT:Cnn`. Copilot sees these comments and knows to maintain them when editing enforcement code. The pattern is documented in `copilot-instructions.md`.

### Layer 2: Tests (CI-Enforced)

| Test | What it validates |
|---|---|
| **Construct examples parse** | Every `ConstructInfo.Example` in the catalog parses successfully via the parser. If a construct form changes, the example must be updated — or the test fails. |
| **Constraint enforcement exists** | Every registered `LanguageConstraint` has a matching `// SYNC:CONSTRAINT:Cnn` comment in the source. And vice versa — every SYNC comment has a catalog entry. |
| **Constraint triggers** | Every registered constraint has a `[Theory]` test case with a violating input that produces the expected error. |
| **Token attributes complete** | Every `PreceptToken` member has `[TokenCategory]` and `[TokenDescription]` attributes. Keyword/operator/punctuation members also have `[TokenSymbol]`. |
| **Documentation constraints match** | The constraint list in `docs/DesignNotes.md § DSL Syntax Contract` matches `ConstraintCatalog.Constraints` — same IDs, same rules. |
| **Reference sample coverage** | At least one `.precept` sample file uses every construct registered in `ConstructCatalog`. |

### Layer 3: Copilot Instructions (Behavioral)

`copilot-instructions.md` includes explicit rules:
- When adding a new keyword, update the token enum with all three attributes
- When adding a new enforcement point, register the constraint with `ConstraintCatalog.Register()` and add a `// SYNC:CONSTRAINT:Cnn` comment
- When adding a new parser combinator, register it with `ConstructCatalog.Register()` with a parseable example
- SYNC comments, tests, and catalog registrations must all agree

---

## Removability

The catalog infrastructure is designed so that the MCP server is fully separable:

- **If MCP is removed:** Delete `tools/Precept.Mcp/`. Core catalogs remain in `src/Precept/Dsl/`. Parser error messages, language server features (hovers, completions, diagnostic codes), and drift-defense tests all continue to work exactly as before.
- **If catalogs are removed:** (Not recommended.) Revert to hand-written error strings, manual keyword lists in the language server, and prose-only constraint documentation. No other component depends on catalogs for correctness — they're an ergonomic and consistency improvement.
- **If the language server is removed:** Catalogs still improve parser/runtime error messages and MCP responses. No LS-specific code in the catalog files.

Each consumer is independently optional. The catalogs are a **data layer** with no behavioral coupling to any specific consumer.
