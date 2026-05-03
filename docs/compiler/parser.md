# Parser

## Status

**Stage:** Design Complete  
**Implementation:** Stub — `Parser.Parse` returns empty arrays  
**Blocking:** Expression tree design for expression-carrying slots

---

## Overview

The parser transforms a token stream into a `ConstructManifest` containing `ParsedConstruct` nodes. It implements a **catalog-driven generic interpreter** architecture: the parser does not contain per-construct parsing logic encoded in source code. Instead, construct metadata in the `Constructs` catalog drives a single generic slot-walking engine.

### Output Shape

The parser produces exactly one output type:

```csharp
public sealed record ParsedConstruct(
    ConstructMeta Meta,
    ImmutableArray<SlotValue> Slots,
    SourceSpan Span);
```

- **Meta** — The catalog entry describing this construct's kind, slots, and routing
- **Slots** — Parsed values in declaration order matching `Meta.Slots`
- **Span** — Source location from first to last consumed token

There are no per-construct AST node types. The traditional N construct kinds × M consumers explosion is eliminated: every consumer works with `ParsedConstruct` uniformly.

### SlotValue: 17-Subtype Discriminated Union

`SlotValue` is an abstract record with 17 sealed subtypes, one per `ConstructSlotKind`:

> **Open Question (unresolved):** catalog-system.md shows 15 `ConstructSlotKind` members; this doc lists 17 (`RuleExpressionSlot` and `InitialMarkerSlot` are missing from the catalog). Which count is correct?

| Subtype | Carries |
|---------|---------|
| `IdentifierListSlot` | `ImmutableArray<string>` — field/event names |
| `TypeExpressionSlot` | `TypeMeta` — resolved type reference |
| `ModifierListSlot` | `ImmutableArray<ModifierKind>` |
| `StateEntryListSlot` | `ImmutableArray<(string Name, ImmutableArray<ModifierKind> Modifiers)>` |
| `ArgumentListSlot` | `ImmutableArray<(string Name, TypeMeta Type)>` |
| `ComputeExpressionSlot` | `SourceSpan` (expression tree deferred) |
| `GuardClauseSlot` | `SourceSpan` (expression tree deferred) |
| `ActionChainSlot` | `ImmutableArray<ActionKind>` |
| `OutcomeSlot` | `SourceSpan` (expression tree deferred) |
| `StateTargetSlot` | `string?` — target state name |
| `EventTargetSlot` | `string?` — target event name |
| `EnsureClauseSlot` | `SourceSpan` (expression tree deferred) |
| `BecauseClauseSlot` | `string` — diagnostic message |
| `AccessModeSlot` | `SourceSpan` — access mode span |
| `FieldTargetSlot` | `string?` — target field name |
| `RuleExpressionSlot` | `SourceSpan` (expression tree deferred) |
| `InitialMarkerSlot` | `bool` — presence of `initial` keyword |

> **Open Question (unresolved):** Four slot subtypes contradict type-checker.md: `TypeExpressionSlot` (TypeMeta here vs SourceSpan there), `ModifierListSlot` (ModifierKind here vs TokenKind there), `AccessModeSlot` (SourceSpan here vs TokenKind there), `BecauseClauseSlot` (string here vs SourceSpan there). Which shapes are canonical?

Expression-carrying slots(`ComputeExpressionSlot`, `GuardClauseSlot`, `OutcomeSlot`, `EnsureClauseSlot`, `RuleExpressionSlot`) currently carry only `SourceSpan`. Full expression tree representation is deferred pending design work.

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

The lexer produces `Token` records with `TokenKind`, `Lexeme`, and `SourceSpan`. The parser consumes this stream. Token classification (keyword vs identifier, operator precedence) is fully determined by catalog metadata.

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
ImmutableArray<Token> tokens
```

Token stream from lexer, including trivia tokens (whitespace, comments) for span calculations.

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
| `StateScoped` | Shared leading token, disambiguation by peek | `from`/`in`/`to` constructs |
| `EventScoped` | Shared leading token, disambiguation by peek | `on` constructs |

For `StateScoped` and `EventScoped`, the parser consults `DisambiguationEntry.DisambiguationTokens` via the construct's `Entries` property to select the correct construct.

### Disambiguation Protocol

When multiple constructs share a leading token:

```
candidates := constructs with this leading token
for each candidate:
    disambiguationTokens := candidate.Meta.Entries.DisambiguationTokens
    if peek(offset) in disambiguationTokens:
        return parseConstruct(candidate)
report ambiguous construct
```

The offset and token set come from catalog metadata — the parser contains no hardcoded disambiguation logic.

> **Open Question (unresolved):** The disambiguation `peek(offset)` doesn't specify the offset value. Is it always 1? If it varies per construct, `DisambiguationEntry` needs an `Offset` field in the catalog.

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
| `ComputeExpression` | Capture span, defer expression tree |
| `GuardClause` | Capture `when` clause span |
| `ActionChain` | Parse action keywords via `Actions` catalog |
| `Outcome` | Capture `=>` expression span |
| `StateTarget` | Parse optional state name after `to` |
| `EventTarget` | Parse optional event name after `fire` |
| `EnsureClause` | Capture `ensure` expression span |
| `BecauseClause` | Parse `because "message"` |
| `AccessModeKeyword` | Capture access mode span |
| `FieldTarget` | Parse field name after `for` |
| `RuleExpression` | Capture rule body span |
| `InitialMarker` | Check for `initial` keyword presence |

### Expression Parsing (Deferred)

Expression-carrying slots currently capture only `SourceSpan`. The eventual expression parser will be a Pratt parser (operator-precedence) using `Operators` catalog for precedence and associativity metadata. This is the irreducible algorithmic core — expressions require precedence climbing, which cannot be eliminated by catalog-driven dispatch.

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
| `Operators` catalog | Operator precedence (future) |

### Downstream Consumers

| Component | What It Receives |
|-----------|------------------|
| Type Checker | `ConstructManifest` with typed slots |
| Language Server | Spans for diagnostics, hover, go-to-definition |

> **Open Question (unresolved):** Graph analyzer consumes `SemanticIndex`, not `ConstructManifest`. Should the old "Graph Analyzer" row be removed, or does tooling need a ConstructManifest → graph-analyzer edge?

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

1. **Slot count matches metadata** — `construct.Slots.Length == construct.Meta.Slots.Length`
2. **Slot kinds match metadata** — `construct.Slots[i]` is the subtype for `construct.Meta.Slots[i].Kind`
3. **Spans are valid** — All spans reference positions within the source
4. **No construct type explosion** — Output is always `ParsedConstruct`, never subtypes

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

### Why Defer Expression Trees?

**Decision:** Expression-carrying slots hold `SourceSpan` only for now.

**Rationale:**
- Expression tree design requires careful precedent research (Roslyn, TypeScript, etc.)
- Runtime currently needs only span for error messages
- Deferred design allows parallel progress on construct parsing

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

### Expression Tree Design

Expression-carrying slots need proper tree representation. Options under consideration:

1. **Roslyn-style**: Per-expression-kind node types with full fidelity
2. **S-expression**: Uniform `(op, args...)` structure
3. **Span + lazy parse**: Keep span, parse on demand

This requires precedent research and design review before implementation.

### Error Recovery Granularity

Current design synchronizes at construct boundaries. Finer-grained recovery (per-slot) may improve diagnostics but adds complexity. Needs implementation experience to decide.

### Trivia Preservation

Current design discards trivia. For formatting tools, trivia attachment may be needed. Not blocking for runtime but noted for tooling.

---

## Deliberate Exclusions

### No Per-Construct Node Types

Traditional parsers produce `FieldDeclarationSyntax`, `EventDeclarationSyntax`, etc. Precept deliberately avoids this pattern. Consumers that need construct-specific handling match on `ConstructKind` and index into slots.

### No Symbol Table in Parser

Name resolution happens in the type checker with full context. The parser only captures names as strings; it does not build or consult symbol tables.

### No Semantic Validation

Whether a field name is unique, a state is reachable, or a type exists are semantic questions. The parser accepts syntactically valid constructs regardless of semantic validity.

### No Expression Evaluation

Expressions are structural data. The evaluator interprets them against runtime state. The parser builds structure only.

---

## Cross-References

| Document | Relationship |
|----------|--------------|
| [Lexer](./lexer.md) | Upstream — produces token stream |
| [Type Checker](./type-checker.md) | Downstream — validates semantics |
| [Catalog System](../language/catalog-system.md) | Defines construct metadata architecture |
| [Precept Language Spec](../language/precept-language-spec.md) | Grammar this parser implements |

---

## Source Files

| File | Purpose |
|------|---------|
| `src/Precept/Pipeline/Parser.cs` | Parser entry point (currently stub) |
| `src/Precept/Pipeline/ParsedConstruct.cs` | Output type definition |
| `src/Precept/Pipeline/SlotValue.cs` | 17-subtype discriminated union |
| `src/Precept/Pipeline/ConstructManifest.cs` | Parser output container |
| `src/Precept/Language/Constructs.cs` | Construct catalog and indexes |
| `src/Precept/Language/Construct.cs` | `ConstructMeta`, `ConstructKind`, `RoutingFamily` |
| `src/Precept/Language/ConstructSlot.cs` | `ConstructSlotKind`, `ConstructSlot` |
| `src/Precept/Language/DisambiguationEntry.cs` | Disambiguation metadata |
