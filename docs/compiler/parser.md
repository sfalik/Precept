# Parser

## Status

**Stage:** Design Complete  
**Implementation:** Complete — Slices 1–4  
**Blocking:** None — all expression slots carry `ParsedExpression` via Pratt parser.

---

## Contents

- [Overview](#overview)
  - [Output Shape](#output-shape)
  - [SlotValue: 17-Subtype Discriminated Union](#slotvalue-17-subtype-discriminated-union)
- [Responsibilities and Boundaries](#responsibilities-and-boundaries)
  - [What the Parser Does](#what-the-parser-does)
  - [What the Parser Does NOT Do](#what-the-parser-does-not-do)
  - [Boundary with Lexer](#boundary-with-lexer)
  - [Boundary with Type Checker](#boundary-with-type-checker)
- [Right-Sizing](#right-sizing)
- [Inputs and Outputs](#inputs-and-outputs)
  - [Input](#input)
  - [Output](#output)
- [Architecture](#architecture)
  - [Catalog-Driven Dispatch](#catalog-driven-dispatch)
  - [RoutingFamily-Based Routing](#routingfamily-based-routing)
  - [Disambiguation Protocol](#disambiguation-protocol)
  - [Slot Walking](#slot-walking)
  - [Slot Sub-Parsers](#slot-sub-parsers)
  - [Expression Parsing (Implemented)](#expression-parsing-implemented)
- [Component Mechanics](#component-mechanics)
  - [Token Stream Interface](#token-stream-interface)
  - [Vocabulary Recognition](#vocabulary-recognition)
  - [Construct Boundaries](#construct-boundaries)
- [Dependencies and Integration Points](#dependencies-and-integration-points)
  - [Upstream Dependencies](#upstream-dependencies)
  - [Downstream Consumers](#downstream-consumers)
- [Failure Modes and Recovery](#failure-modes-and-recovery)
  - [Unexpected Token](#unexpected-token)
  - [Missing Expected Token](#missing-expected-token)
  - [Unclosed Constructs](#unclosed-constructs)
  - [Disambiguation Failure](#disambiguation-failure)
- [Contracts and Guarantees](#contracts-and-guarantees)
  - [Invariants](#invariants)
  - [Preconditions](#preconditions)
  - [Postconditions](#postconditions)
- [Design Rationale and Decisions](#design-rationale-and-decisions)
  - [Why Generic ParsedConstruct?](#why-generic-parsedconstruct)
  - [Why Catalog-Driven Dispatch?](#why-catalog-driven-dispatch)
  - [Why SlotValue as Discriminated Union?](#why-slotvalue-as-discriminated-union)
  - [Expression Tree Design (RESOLVED)](#expression-tree-design-resolved)
- [Innovation](#innovation)
  - [Metadata-as-Grammar](#metadata-as-grammar)
  - [Slot-Indexed Output](#slot-indexed-output)
- [Open Questions / Implementation Notes](#open-questions--implementation-notes)
  - [~~Expression Tree Design~~ (RESOLVED)](#expression-tree-design-resolved)
  - [Error Recovery Granularity](#error-recovery-granularity)
  - [Trivia Preservation](#trivia-preservation)
- [Deliberate Exclusions](#deliberate-exclusions)
  - [No Per-Construct Node Types](#no-per-construct-node-types)
  - [No Symbol Table in Parser](#no-symbol-table-in-parser)
  - [No Semantic Validation](#no-semantic-validation)
  - [No Expression Evaluation](#no-expression-evaluation)
- [Cross-References](#cross-references)
- [Source Files](#source-files)

## Overview

The parser transforms a token stream into a `ConstructManifest` containing `ParsedConstruct` nodes. It implements a **catalog-driven generic interpreter** architecture: the parser does not contain per-construct parsing logic encoded in source code. Instead, construct metadata in the `Constructs` catalog drives a single generic slot-walking engine.

### Output Shape

The parser produces exactly one output type:

```csharp
public sealed record ParsedConstruct(
    ConstructMeta             Meta,
    ImmutableArray<SlotValue> Slots,
    SourceSpan                Span,
    TokenKind?                LeadingTokenKind = null);
```

- **Meta** — The catalog entry describing this construct's kind, slots, and routing
- **Slots** — Parsed values in declaration order matching `Meta.Slots`
- **Span** — Source location from first to last consumed token
- **LeadingTokenKind** — The anchor scope keyword (`in`/`to`/`from`/`on`) consumed by the parser, preserved for downstream stages that need it (e.g., the TypeChecker's construct-kind determination). `null` for non-scoped constructs.

There are no per-construct AST node types. The traditional N construct kinds × M consumers explosion is eliminated: every consumer works with `ParsedConstruct` uniformly.

### SlotValue: 17-Subtype Discriminated Union

`SlotValue` is an abstract record with 17 sealed subtypes, one per `ConstructSlotKind`:

> **Decision (2026-05-06):** `SlotValue` subtype inventory is **17** — the `ConstructSlotKind` enum in code (`src/Precept/Language/ConstructSlot.cs`) is authoritative. All 17 members are declared, including `RuleExpression` (16) and `InitialMarker` (17). catalog-system.md was corrected in Wave 4 to reflect this count. No discrepancy remains.

| Subtype | Carries |
|---------|---------|
| `IdentifierListSlot` | `ImmutableArray<string>` — field/event names |
| `TypeExpressionSlot` | `TypeMeta` — resolved type reference |
| `ModifierListSlot` | `ImmutableArray<ModifierKind>` |
| `StateEntryListSlot` | `ImmutableArray<(string Name, ImmutableArray<ModifierKind> Modifiers)>` |
| `ArgumentListSlot` | `ImmutableArray<(string Name, TypeMeta Type)>` |
| `ComputeExpressionSlot` | `ParsedExpression` (typed DU) |
| `GuardClauseSlot` | `ParsedExpression` (typed DU) |
| `ActionChainSlot` | `ImmutableArray<ActionKind>` |
| `OutcomeSlot` | `ParsedOutcome` (typed DU) |
| `StateTargetSlot` | `ImmutableArray<string>` + `ImmutableArray<SourceSpan>` — target state names with per-name spans (`any` remains a single wildcard entry) |
| `EventTargetSlot` | `string?` — target event name |
| `EnsureClauseSlot` | `ParsedExpression` (typed DU) |
| `BecauseClauseSlot` | `string` — diagnostic message |
| `AccessModeSlot` | `TokenKind` — resolved access mode token |
| `FieldTargetSlot` | `string?` — target field name |
| `RuleExpressionSlot` | `ParsedExpression` (typed DU) |
| `InitialMarkerSlot` | `bool` — presence of `initial` keyword |

> **Decision (2026-05-06):** `TypeExpressionSlot` carries `TypeMeta Type` — the parser resolves the type reference via the `Types` catalog at parse time. The type checker receives an already-resolved `TypeMeta` object (not a span to re-resolve). Rationale: type keywords are a small closed set defined in the `Types` catalog; the parser already consults this catalog for vocabulary recognition, so resolution is trivial and eliminates a re-lookup. The `SourceSpan` on the base record remains available for diagnostics.

> **Decision (2026-05-06):** `ModifierListSlot` carries `ImmutableArray<ModifierKind> Modifiers` — the parser normalizes modifier tokens to their semantic `ModifierKind` identity via the `Modifiers` catalog. Rationale: the parser already consults the `Modifiers` catalog for vocabulary recognition; lifting `TokenKind` → `ModifierKind` in the same pass is zero-cost and gives downstream consumers a semantically typed value rather than a raw token to switch on.

> **Decision (2026-05-06):** `AccessModeSlot` carries `TokenKind AccessMode` — the parser resolves the access mode keyword to its `TokenKind` at parse time. Rationale: access modes are a two-member closed set (`readonly`/`editable`). The parser already identifies which token it consumed; forcing the TC to re-read a span to recover this information is wasteful and violates the principle of resolving closed-vocabulary tokens at their recognition site.

> **Decision (2026-05-06):** `BecauseClauseSlot` carries `string Message` — the parser extracts the string literal content at parse time. Rationale: the `because` clause is syntactically a string literal; the parser already validates the token kind and has the lexeme. Downstream consumers (constraint diagnostics, MCP output, LS hover) all need the text, never the raw span. Deferring extraction would require every consumer to re-read source.

Expression-carrying slots (`ComputeExpressionSlot`, `GuardClauseSlot`, `EnsureClauseSlot`, `RuleExpressionSlot`) now carry `ParsedExpression` — a sealed abstract record DU with 15 subtypes: 14 per-form sealed subtypes (one per `ExpressionFormKind` member) plus `MissingExpression` (a sentinel for absent or failed expression parsing that reuses `ExpressionFormKind.Literal`). The parser produces these typed expression nodes; the type checker resolves them into `TypedExpression`. Note: `OutcomeSlot` carries `ParsedOutcome`, not `ParsedExpression` — outcomes are a separate 4-member DU.

---

## Responsibilities and Boundaries

### What the Parser Does

1. **Construct dispatch** — Route to construct parsing based on leading token and disambiguation
2. **Slot walking** — For each slot in `ConstructMeta.Slots`, invoke the corresponding slot sub-parser
3. **Vocabulary recognition** — Recognize keywords and operators via catalog-derived frozen sets
4. **Error recovery** — Synchronize on construct boundaries after errors; produce partial constructs where possible
5. **Span tracking** — Record source spans for every construct and slot value
6. **Diagnostic emission** — Report syntax errors with precise locations

### What the Parser Does NOT Do

- **Semantic validation** — Name resolution, type compatibility, state reachability happen downstream
- **Expression evaluation** — The parser builds structure; the evaluator interprets meaning
- **Cross-construct analysis** — Field references, state existence, event signatures validated by type checker
- **Slot semantics** — Whether `initial` is valid on a given state is a type-check concern, not parsing

### Boundary with Lexer

The lexer produces `Token` records with `TokenKind`, `Text`, and `SourceSpan`. The parser consumes this stream. Token classification (keyword vs identifier, operator precedence) is fully determined by catalog metadata.

### Boundary with Type Checker

The parser guarantees syntactic well-formedness: slots are populated according to catalog structure. The type checker validates semantic correctness: names resolve, types match, constraints hold.

---

## Right-Sizing

The parser is deliberately minimal. Its job is:

1. **Dispatch to construct** — Use `Constructs.ByLeadingToken` to find candidate constructs
2. **Walk slots** — Call slot sub-parsers in declaration order
3. **Emit construct** — Package `ConstructMeta`, slot values, and span

The parser does not:
- Maintain symbol tables
- Validate cross-references
- Evaluate expressions
- Check constraint satisfiability

These responsibilities live in later pipeline stages where full context is available.

---

## Inputs and Outputs

### Input

```csharp
TokenStream tokens
```

The `TokenStream` record from the lexer, carrying `ImmutableArray<Token> Tokens` and `ImmutableArray<Diagnostic> Diagnostics`. Whitespace is consumed silently by the lexer and does not appear in the token stream; comments are emitted as `TokenKind.Comment` tokens.

### Output

```csharp
public sealed record ConstructManifest(
    ImmutableArray<ParsedConstruct> Constructs,
    ImmutableArray<Diagnostic> Diagnostics);
```

- **Constructs** — Parsed construct nodes in source order
- **Diagnostics** — Syntax errors encountered during parsing

---

## Architecture

### Catalog-Driven Dispatch

The parser uses a single dispatch loop driven by `Constructs.ByLeadingToken`:

```
loop:
    token := peek()
    candidates := Constructs.ByLeadingToken[token.Kind]
    
    if candidates is empty:
        report unexpected token
        skip to next construct boundary
        continue
    
    if candidates.Count == 1:
        construct := parseConstruct(candidates.Single())
    else:
        construct := disambiguateAndParse(candidates)
    
    emit construct
```

### RoutingFamily-Based Routing

The `RoutingFamily` enum classifies how constructs are identified:

| Family | Description | Example |
|--------|-------------|---------|
| `Header` | Unique in preamble position | `precept` |
| `Direct` | Unique leading token | `field`, `state`, `event`, `rule` |
| `StateScoped` | Shared leading token, disambiguation by scanned family verb | `from`/`in`/`to` constructs |
| `EventScoped` | Shared leading token, disambiguation by scoped family verb | `on` constructs |

For `StateScoped` and `EventScoped`, the parser consults `DisambiguationEntry.DisambiguationTokens` via the construct's `Entries` property to select the correct construct.

### Disambiguation Protocol

When multiple constructs share a leading token:

```
candidates := constructs with this leading token
disambiguationToken := resolveDisambiguationToken(candidates)
for each candidate:
    disambiguationTokens := candidate.Meta.Entries.DisambiguationTokens
    if disambiguationToken.Kind in disambiguationTokens:
        return parseConstruct(candidate)
report ambiguous construct
```

The disambiguation token set comes from catalog metadata. The parser computes where to look based on routing family and slot shape: EventScoped constructs still resolve at fixed offset 2, while StateScoped constructs scan past the full `StateTarget` (`any` or `Identifier ("," Identifier)*`) before considering the family verb.

> **Decision (2026-05-12): StateScoped disambiguation is variable-offset; EventScoped disambiguation remains offset-2.** No `Offset` field is needed on `DisambiguationEntry` because:
>
> 1. Every EventScoped construct's first slot is `EventTarget` (a single identifier), so its disambiguation token remains `peek(2)`.
> 2. Every StateScoped construct's first slot is `StateTarget`, which may be `any`, a single identifier, or a comma-delimited identifier list.
> 3. The parser can derive the initial disambiguation offset by scanning the authored `StateTarget`, then—when applicable—scan forward past pre-verb `when <expr>` to the first family verb.
> 4. The token *set* still comes from `DisambiguationEntry.DisambiguationTokens`; only the StateScoped offset is computed rather than fixed.
>
> Evidence: `Parser.GetDisambiguationTokenOffset(...)` scans comma-delimited state targets, and `Parser.ResolveDisambiguationToken(...)` scans past optional pre-verb guards to the family verb (`on`, `ensure`, `modify`, `omit`, `->`).

### Slot Walking

Once a construct is selected, the parser walks its slots:

```
slots := []
for each slot in meta.Slots:
    value := parseSlot(slot.Kind)
    slots.append(value)
return ParsedConstruct(meta, slots, span)
```

Each `ConstructSlotKind` maps to exactly one slot sub-parser.

### Slot Sub-Parsers

| SlotKind | Sub-Parser Behavior |
|----------|---------------------|
| `IdentifierList` | Parse comma-separated identifiers |
| `TypeExpression` | Parse type reference, resolve via `Types` catalog |
| `ModifierList` | Parse modifier keywords via `Modifiers` catalog |
| `StateEntryList` | Parse `Name [Modifiers]` entries |
| `ArgumentList` | Parse `(name: Type, ...)` |
| `ComputeExpression` | Parse `<-` expression via Pratt parser → `ParsedExpression` |
| `GuardClause` | Parse `when` expression via Pratt parser → `ParsedExpression` |
| `ActionChain` | Parse action keywords via `Actions` catalog |
| `Outcome` | Parse `-> transition/no transition/reject` → `ParsedOutcome` |
| `StateTarget` | Parse `any` or one/more comma-delimited state names after a scoped preposition |
| `EventTarget` | Parse event name after `on` |
| `EnsureClause` | Parse `ensure` expression via Pratt parser → `ParsedExpression` |
| `BecauseClause` | Parse `because "message"` |
| `AccessModeKeyword` | Capture access mode span |
| `FieldTarget` | Parse field name after `for` |
| `RuleExpression` | Parse rule body via Pratt parser → `ParsedExpression` |
| `InitialMarker` | Check for `initial` keyword presence |

### Expression Parsing (Implemented)

Expression-carrying slots produce `ParsedExpression` — a sealed abstract record DU with 15 subtypes: 14 per-form sealed subtypes (one per `ExpressionFormKind` member) plus `MissingExpression` (a sentinel for absent or failed expression parsing). The expression parser is a Pratt parser (operator-precedence) implemented in `Parser.Expressions.cs`, using the `Operators` catalog for precedence and associativity metadata. This is the irreducible algorithmic core — expressions require precedence climbing, which cannot be eliminated by catalog-driven dispatch.

`ParsedExpression` is a closed, strongly-typed DU. Adding a new expression form requires a C# code change (new subtype + update all consumer switches). Exhaustiveness is enforced by sealed hierarchy + Roslyn analyzer test.

---

## Component Mechanics

### Token Stream Interface

```csharp
Token Peek(int offset = 0);   // Look ahead without consuming
Token Advance();              // Consume and return current token
bool Match(TokenKind kind);   // Consume if match, return success
void Expect(TokenKind kind);  // Consume or emit diagnostic
```

### Vocabulary Recognition

The parser does not maintain keyword sets. Instead:

- **Construct leading tokens**: `Constructs.LeadingTokens` (derived from catalog)
- **Modifiers**: `Modifiers.All.Select(m => m.Token)` 
- **Actions**: `Actions.All.Select(a => a.Token)`
- **Types**: `Types.All.Select(t => t.Token)`
- **Operators**: `Operators.All` with precedence/associativity

All vocabulary sets are `FrozenSet<TokenKind>` computed once at startup.

### Construct Boundaries

Constructs are delimited by:
- Leading tokens (`field`, `state`, `event`, `rule`, `from`, `in`, `to`, `on`)
- End of file
- The `precept` keyword (header is first construct only)

Error recovery synchronizes on these boundaries.

---

## Dependencies and Integration Points

### Upstream Dependencies

| Component | What Parser Uses |
|-----------|------------------|
| `Lexer` | Token stream |
| `Constructs` catalog | `ConstructMeta`, `ByLeadingToken`, `LeadingTokens` |
| `Modifiers` catalog | Modifier vocabulary |
| `Actions` catalog | Action vocabulary |
| `Types` catalog | Type vocabulary |
| `Operators` catalog | Operator precedence and associativity |

### Downstream Consumers

| Component | What It Receives |
|-----------|------------------|
| Name Binder | `ConstructManifest` — collects declarations, resolves references |
| Type Checker | `ConstructManifest` (+ `SymbolTable` from NameBinder) with typed slots |
| Language Server | Spans for diagnostics, hover, go-to-definition |

> **Open Question:** `ConstructManifest` as a graph-analyzer input
> The documented dependency table still shows a parser-to-graph edge even though graph-analyzer.md consumes `SemanticIndex`, not `ConstructManifest`. The pipeline overview needs to decide whether that edge is obsolete or whether some tooling path still legitimately reads parser output directly.
> *Flagged: 2026-05-04*

---

## Failure Modes and Recovery

### Unexpected Token

When no construct matches the current token:
1. Emit diagnostic with token location
2. Skip tokens until a construct boundary (leading token or EOF)
3. Resume parsing

### Missing Expected Token

When a slot expects a specific token that's absent:
1. Emit diagnostic at current position
2. Synthesize placeholder `SlotValue` 
3. Continue with next slot

### Unclosed Constructs

When EOF arrives mid-construct:
1. Emit diagnostic
2. Complete partial construct with available slots
3. Return partial `ConstructManifest`

### Disambiguation Failure

When multiple constructs match and disambiguation fails:
1. Emit "ambiguous construct" diagnostic
2. Select first candidate (deterministic behavior)
3. Continue parsing

---

## Contracts and Guarantees

### Invariants

1. **Variable-length slot arrays** — `construct.Slots` contains only present slots. Required slots always appear; absent optional slots are omitted. Slot count equals the number of present slots, not the catalog slot count.
2. **Slots appear in catalog order** — `construct.Slots` preserves the relative order defined in `Constructs.GetMeta(kind).Slots`; absent optionals create no gaps.
3. **Locate slots by Kind, not by index** — downstream consumers (type checker, evaluator) must find slots by `slot.Kind`, not by catalog index position, because indices depend on which optional slots are present.
4. **Spans are valid** — All spans reference positions within the source
5. **No construct type explosion** — Output is always `ParsedConstruct`, never subtypes

### Preconditions

- Token stream is well-formed (no overlapping spans)
- Token stream contains at least EOF

### Postconditions

- Every successfully parsed region produces a `ParsedConstruct`
- Every syntax error produces a `Diagnostic`
- Parser always terminates (no infinite loops on malformed input)

---

## Design Rationale and Decisions

### Why Generic ParsedConstruct?

**Decision:** Single output type with `ConstructMeta` + `SlotValue[]` instead of per-construct AST nodes.

**Rationale:** 
- Eliminates N×M complexity where N constructs × M consumers each need handling code
- New constructs require only catalog entries, not new types
- Consumers pattern-match on `ConstructKind` when needed, walk slots generically otherwise
- Aligns with Precept's metadata-driven architecture

### Why Catalog-Driven Dispatch?

**Decision:** Parser reads `Constructs.ByLeadingToken` rather than hardcoding leading token checks.

**Rationale:**
- Single source of truth for construct vocabulary
- Grammar generator, completions, and parser all derive from same metadata
- Adding a construct requires catalog entry only, not parser modification

### Why SlotValue as Discriminated Union?

**Decision:** 17 sealed subtypes rather than object bags or union types.

**Rationale:**
- Type safety — each slot carries exactly its required data
- Exhaustive matching — missing cases are compile errors
- Self-documenting — subtype names describe slot semantics

### Expression Tree Design (RESOLVED)

**Decision:** Expression-carrying slots carry `ParsedExpression` — a sealed abstract record DU with per-form sealed subtypes.

**Design:**
- `ParsedExpression` = sealed abstract record base + 15 subtypes: 14 per-form sealed subtypes (one per `ExpressionFormKind` member declared in the `ExpressionForms` catalog) + `MissingExpression` sentinel
- The type checker resolves these into `TypedExpression` (same DU shape with resolved type information)
- The set is **closed by design** — new expression form = new catalog entry + new DU subtype + update all consumer switches
- **Exhaustiveness enforcement:** sealed hierarchy for compiler-level checking + Roslyn analyzer test for build-time verification of all switch arms

**Rationale:** 14 expression forms is still a bounded, catalogable set. Strongly-typed DU eliminates an entire class of runtime errors. The closed set is intentional — expression additions are rare, deliberate language changes that SHOULD require global updates.

---

## Innovation

### Metadata-as-Grammar

The `Constructs` catalog is a machine-readable grammar specification. The parser is a generic interpreter of this grammar. This inverts the traditional compiler model where grammar knowledge is encoded in parser source code.

### Slot-Indexed Output

Traditional ASTs have per-node-type properties (`FieldDeclaration.Name`, `EventDeclaration.Args`). `ParsedConstruct` has positional slots indexed by `ConstructSlotKind`. This:
- Enables generic traversal
- Eliminates property duplication across similar constructs
- Makes slot addition a metadata change, not a type change

---

## Open Questions / Implementation Notes

### ~~Expression Tree Design~~ (RESOLVED)

Expression tree design resolved. Expression-carrying slots carry `ParsedExpression` (sealed DU, 15 subtypes: 14 per-form + MissingExpression sentinel). See § Expression Tree Design (RESOLVED) above.

### Error Recovery Granularity

Current design synchronizes at construct boundaries. Finer-grained recovery (per-slot) may improve diagnostics but adds complexity. Needs implementation experience to decide.

### Trivia Preservation

Current design discards trivia. For formatting tools, trivia attachment may be needed. Not blocking for runtime but noted for tooling.

---

## Deliberate Exclusions

### No Per-Construct Node Types

Traditional parsers produce `FieldDeclarationSyntax`, `EventDeclarationSyntax`, etc. Precept deliberately avoids this pattern. Consumers that need construct-specific handling match on `ConstructKind` and index into slots.

### No Symbol Table in Parser

Name resolution happens in the NameBinder (declaration collection + reference resolution) and then the TypeChecker (semantic validation). The parser only captures names as strings; it does not build or consult symbol tables.

### No Semantic Validation

Whether a field name is unique, a state is reachable, or a type exists are semantic questions. The parser accepts syntactically valid constructs regardless of semantic validity.

### No Expression Evaluation

Expressions are structural data. The evaluator interprets them against runtime state. The parser builds structure only.

---

## Cross-References

| Document | Relationship |
|----------|--------------|
| [Lexer](./lexer.md) | Upstream — produces token stream |
| [Name Binder](./name-binder.md) | Downstream — collects declarations, resolves references |
| [Type Checker](./type-checker.md) | Downstream — validates semantics |
| [Catalog System](../language/catalog-system.md) | Defines construct metadata architecture |
| [Precept Language Spec](../language/precept-language-spec.md) | Grammar this parser implements |

---

## Source Files

| File | Purpose |
|------|---------|
| `src/Precept/Pipeline/Parser.cs` | Parser entry point |
| `src/Precept/Pipeline/ParsedConstruct.cs` | Output type definition |
| `src/Precept/Pipeline/SlotValue.cs` | 17-subtype discriminated union |
| `src/Precept/Pipeline/ConstructManifest.cs` | Parser output container |
| `src/Precept/Language/Constructs.cs` | Construct catalog and indexes |
| `src/Precept/Language/Construct.cs` | `ConstructMeta`, `ConstructKind`, `RoutingFamily` |
| `src/Precept/Language/ConstructSlot.cs` | `ConstructSlotKind`, `ConstructSlot` |
| `src/Precept/Language/DisambiguationEntry.cs` | Disambiguation metadata |
