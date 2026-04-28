# Lexer

## Status

| Property | Value |
|---|---|
| Doc maturity | Draft |
| Implementation state | Implemented |
| Source | `src/Precept/Pipeline/Lexer.cs`, `src/Precept/Pipeline/TokenStream.cs`, `src/Precept/Pipeline/SourceSpan.cs` |
| Upstream | Source text (string) |
| Downstream | Parser, LS lexical features, MCP compile |

---

## Overview

The lexer is the first stage of the Precept compilation pipeline. It transforms raw source text into a flat sequence of `Token` values — the atomic units that every downstream stage consumes. The lexer has no knowledge of grammar, scoping, or semantics; it recognizes character patterns and classifies them using metadata from the Tokens catalog.

Every token the lexer emits carries its kind, text, and precise source location:

```csharp
public readonly record struct Token(TokenKind Kind, string Text, SourceSpan Span)
{
    public int Line => Span.StartLine;
    public int Column => Span.StartColumn;
    public int Offset => Span.Offset;
    public int Length => Span.Length;
}
```

The `SourceSpan` carries both coordinate systems (offset/length for source slicing, line/column for LSP positions) as a single value. The convenience forwarding properties (`Line`, `Column`, `Offset`, `Length`) preserve the flat-field API that downstream consumers expect.

The lexer's output is a `TokenStream` — an immutable, indexable sequence of tokens that the parser, type checker, and tooling all read from. The lexer runs once per source text and produces the complete stream before any downstream stage begins.

---

## Responsibilities and Boundaries

**OWNS:** Token classification, source span attribution, interpolation mode tracking, literal boundary detection, keyword lookup, operator/punctuation scan.

**Does NOT OWN:** Grammar structure (Parser), name resolution (TypeChecker), numeric interpretation (TypeChecker), typed-constant content validation (TypeChecker), comment content (discarded).

---

## Right-Sizing

The lexer is intentionally narrow: one pass, pure function, no state carried across calls. It was sized to recognize patterns, not meaning. The 64KB source ceiling means the full lex pass is sub-millisecond — no streaming or incremental API is needed. The interpolation mode stack (max depth 8) covers all realistic nesting without a general-purpose recursive scanner.

---

## Inputs and Outputs

```
string source  →  Lexer.Lex  →  TokenStream
```

`Lexer.Lex(string source)` is a static pure function. No instance, no DI, no configuration. This matches the pipeline pattern used by all five stages (`Lexer.Lex`, `Parser.Parse`, `TypeChecker.Check`, `GraphAnalyzer.Analyze`, `ProofEngine.Prove`). Tests call the method directly and assert on the output.

The lexer handles three families of input:

- **Primitive tokens** — identifiers, keywords, numeric literals, operators, punctuation, whitespace-adjacent tokens (newlines, comments)
- **String literals** — `"..."` with `\"`, `\\`, `\n`, `\t` escapes and `{expr}` interpolation
- **Typed constant literals** — `'...'` with `\'`, `\\` escapes and `{expr}` interpolation

Typed constant content is opaque to the lexer. The lexer marks the boundaries (`TypedConstant`, `TypedConstantStart`, `TypedConstantMiddle`, `TypedConstantEnd`) but does not interpret what's inside. Type resolution and content validation happen in the type checker.

The `TokenStream` is the `Compilation.Tokens` field — it is part of the tooling surface and queryable by the language server for span-based operations.

---

## Catalog-Driven Design

### The Lexer Is Generic Machinery

The lexer is a character-pattern scanner. It does not encode domain knowledge — it reads domain knowledge from catalogs. When a new keyword, operator, or punctuation token is added to the Tokens catalog, the lexer recognizes it without code changes. When the Diagnostics catalog gains a new lex-stage code, the lexer can emit it without new switch branches.

### Catalog as Source of Truth

The Tokens catalog (`Tokens.All`) is the exhaustive inventory of every token the lexer can produce. Each entry carries `TokenKind`, surface text (for keywords/operators/punctuation), categories, description, `TextMateScope`, `SemanticTokenType`, and `ValidAfter`. The lexer derives its keyword lookup table, operator scan tables, and punctuation scan table from this catalog at static initialization. No parallel copies of token membership exist anywhere in the lexer.

### No Parallel Copies

If the lexer switches on token kinds to apply per-token behavior, that behavior belongs in catalog metadata — not in lexer code. `TryScanOperator` and `TryScanPunctuation` perform table lookups against `FrozenDictionary` tables derived from `Tokens.All`. Adding a new operator or punctuation token requires only a catalog entry — no scan-method update.

---

## Scanning Rules

### Catalog Integration

#### Keywords

Keywords are stored in `Tokens.Keywords`, a `FrozenDictionary<string, TokenKind>` derived from `Tokens.All` at startup. The filter selects entries whose categories include keyword-bearing categories (Declaration, Preposition, Control, Action, Outcome, AccessMode, LogicalOperator, Membership, Quantifier, StateModifier, Constraint, Type, Literal). `SetType` is explicitly excluded — `set` always emits as `TokenKind.Set`; the parser disambiguates contextually.

At scan time, after reading a word:

```csharp
_keywordLookup = Tokens.Keywords.GetAlternateLookup<ReadOnlySpan<char>>();
var kind = _keywordLookup.TryGetValue(span, out var kw) ? kw : TokenKind.Identifier;
```

`GetAlternateLookup<ReadOnlySpan<char>>()` accepts a span directly, avoiding a `new string(...)` allocation for every identifier candidate. `FrozenDictionary` uses a perfect hash at construction time.

#### Operators

The lexer derives two `FrozenDictionary` tables from `Tokens.All` at startup:

- **`Tokens.TwoCharOperators`** — `FrozenDictionary<(char, char), (TokenKind, string)>` for all two-character operators (`->`, `!~`, `~=`, `==`, `!=`, `>=`, `<=`). Keyed on the character pair; value is the `(TokenKind, text)` pair.
- **`Tokens.SingleCharOperators`** — `FrozenDictionary<char, (TokenKind, string)>` for all single-character operators (`=`, `>`, `<`, `+`, `-`, `*`, `/`, `%`, `~`).
- **`Tokens.TwoCharOperatorStarters`** — `FrozenSet<char>` of the first characters that begin a two-char operator. Used as a cheap guard before the tuple lookup.

`TryScanOperator` checks `TwoCharOperatorStarters` first; if the current character is a starter, it attempts a `TwoCharOperators` lookup on the `(current, next)` pair. If that succeeds, it advances twice and emits. Otherwise it falls through to a `SingleCharOperators` lookup on the current character.

#### Punctuation

The lexer derives **`Tokens.PunctuationChars`** — `FrozenDictionary<char, (TokenKind, string)>` — from `Tokens.All` entries whose `Text` is one character and whose categories include `TokenCategory.Punctuation`. `TryScanPunctuation` does a single dictionary lookup on the current character. Delimiter characters (`{`, `}`, `"`, `'`) are absent from this table — they have dedicated mode-transition handling in `ScanToken` and are never emitted as punctuation tokens. The Tokens catalog classifies all punctuation tokens with `TokenCategory.Punctuation`.

#### Diagnostics

The lexer emits diagnostics via `Diagnostics.Create(DiagnosticCode, SourceSpan)`. The Diagnostics catalog owns the message templates — the lexer passes codes, not strings. All lex-stage codes: `InputTooLarge`, `UnterminatedStringLiteral`, `UnterminatedTypedConstant`, `UnterminatedInterpolation`, `InvalidCharacter`, `UnrecognizedStringEscape`, `UnrecognizedTypedConstantEscape`, `UnescapedBraceInLiteral`.

---

### Scan Strategy

#### Main Loop

All mutable scanning state lives in a private `Scanner` struct instantiated inside `Lex()` and discarded after:

```csharp
public static TokenStream Lex(string source)
{
    var scanner = new Scanner(source);
    scanner.ScanAll();
    return scanner.Build();
}
```

`Scanner` is stack-allocated. The only heap allocations per scan are the `ImmutableArray.Builder` instances and the final `TokenStream` output.

`ScanAll()` dispatches on the current mode. In `Normal` mode, the main loop tries in order: whitespace/newline, comment (`#`), identifier/keyword, number, string literal (`"`), typed constant (`'`), operator, punctuation, then falls through to `InvalidCharacter`. Each branch either emits a token or a diagnostic, advances position, and returns to the loop.

#### Interpolation Mode Stack

Interpolation creates a nesting problem: `{expr}` inside `"..."` or `'...'` re-enters Normal-like scanning, which may encounter new literals. The solution is a mode stack with four modes:

| Mode | Description |
|------|-------------|
| `Normal` | Top-level scanning — keywords, identifiers, operators, punctuation |
| `String` | Inside `"..."` — accumulates literal characters, handles escapes and `{`/`}` |
| `TypedConstant` | Inside `'...'` — accumulates literal characters, handles escapes and `{`/`}` |
| `Interpolation` | Inside `{...}` inside a literal — scans as Normal, ends at matching `}` |

The stack is a fixed-size `ModeState[8]` with a depth counter — no `Stack<T>` heap allocation. Maximum nesting of 8 supports 4 full nesting levels (string → interpolation → typed constant → interpolation), well beyond realistic use. When the depth limit is reached, the lexer emits `UnterminatedInterpolation` and recovers.

Each stack entry is a `ModeState` struct carrying the mode, segment index, and the source position of the delimiter that began the current segment. This ensures correct span attribution for `StringStart`/`StringMiddle`/`TypedConstantStart`/`TypedConstantMiddle` tokens.

---

### Keyword and Identifier Recognition

The lexer reads a word (letters, digits, `_`), then checks `Tokens.Keywords` for a match. If found, the token kind is the keyword's `TokenKind`; otherwise it is `TokenKind.Identifier`.

`set` always emits as `TokenKind.Set` — it is present in `Keywords`. The parser determines from context whether `set` introduces a `set<T>` type annotation or a `set` action. `SetType` is excluded from the keyword dictionary entirely.

`min` and `max` always emit as their respective keyword `TokenKind` values. Their dual-use role (constraint keyword vs. aggregation function) is resolved by the parser based on syntactic context.

---

### Operator and Punctuation Recognition

#### Operators

Maximal munch is enforced by table structure: `TryScanOperator` consults `TwoCharOperators` before `SingleCharOperators`. The two-char check is guarded by a `TwoCharOperatorStarters.Contains(c)` test — a `FrozenSet` lookup — before constructing the `(c, next)` tuple, avoiding tuple allocation on characters that cannot start a two-char operator. If the tuple is found in `TwoCharOperators`, two characters are consumed; otherwise the single-char table is tried. Arrow (`->`) is included in `TwoCharOperators` because it is a two-character structural token recognized by the same mechanism. The Tokens catalog classifies comparison operators with `TokenCategory.Operator`; arrow is `TokenCategory.Structural` — both are selected by the two-char table filter.

#### Punctuation

`TryScanPunctuation` performs a single `PunctuationChars.TryGetValue(Current, ...)` lookup. If the current character is in the table, one character is consumed and the token is emitted. Delimiter characters (`{`, `}`, `"`, `'`) are absent from the table — they trigger mode transitions in `ScanToken` and are never emitted as punctuation tokens. The Tokens catalog classifies all punctuation tokens with `TokenCategory.Punctuation`.

---

### Literal Scanning

#### String Literals

A `"` in Normal or Interpolation mode pushes `String` mode. The scanner accumulates characters into a reusable `char[]` content buffer. Recognized escapes (`\"`, `\\`, `\n`, `\t`) are decoded. `{` (not `{{`) pushes `Interpolation` mode and emits `StringStart` or `StringMiddle`. `}` returns control to the enclosing interpolation. The closing `"` pops String mode and emits `StringLiteral` (no interpolation) or `StringEnd`.

#### Typed Constant Literals

A `'` in Normal or Interpolation mode pushes `TypedConstant` mode. Same structural pattern as strings, with `'` delimiters and only `\'`, `\\` as recognized escapes. Content is opaque — the lexer does not validate unit names, date formats, or currency codes. The type checker handles that.

#### Token.Text for Quoted Literals

`Token.Text` carries the **decoded content** — escape sequences resolved, delimiters excluded. `Token.Offset` + `Token.Length` spans the full source extent including delimiters. This means `Token.Length != Token.Text.Length` for quoted literals.

---

### Number Literals

The lexer scans digits, optional decimal point, optional exponent (`e`/`E` with optional `+`/`-`). All numeric tokens emit as `TokenKind.NumberLiteral` with `Token.Text` carrying the raw digit characters. Numeric interpretation (integer vs. decimal, range validation) is the type checker's responsibility.

---

### Comments and Whitespace

#### Comments

`#` begins a line comment. The lexer scans to the end of the line and emits `TokenKind.Comment` with `Token.Text` carrying the full text including the leading `#`. The newline is left unconsumed for the next iteration.

#### Whitespace

Spaces and tabs are consumed silently. No trivia tokens are emitted. Precept has no formatting/refactoring requirements that need whitespace round-trip fidelity.

#### Newlines

`\n`, `\r\n`, and bare `\r` emit `TokenKind.NewLine`. Newlines are structurally significant to the grammar (statement termination) and must be preserved as tokens for the parser.

---

### Dual-Use Token Strategy

#### set / SetType

`set` always emits as `TokenKind.Set`. The parser determines from syntactic context whether it introduces a `set<T>` type annotation (field declaration) or a `set` action (event body). `SetType` is excluded from `Tokens.Keywords` — it exists only as a synthetic kind the parser produces.

```precept
field Tags : set<string>       # set → TokenKind.Set; parser sees set<T> type context → SetType
set Status to "active"          # set → TokenKind.Set; parser sees assignment context → set action
when Status is set              # set → TokenKind.Set; parser sees guard context → null-narrowing
when Status is not set           # set → TokenKind.Set; parser sees negated guard → null-narrowing
```

#### min / max

`min` and `max` each have one `TokenKind` entry. Their dual role as constraint keywords and aggregation functions is resolved by the parser based on syntactic context. The Tokens catalog classifies them as `TokenCategory.Constraint` only — not `TokenCategory.Function` — because the lexer emits them; the parser interprets them.

```precept
field Score : integer min 0 max 100    # min/max → constraint keywords (field bounds)
computed Total = max(LineAmount)        # max → aggregation function (expression context)
```

---

### What Stays Hand-Written

Not everything should be catalog-driven. The following remain hand-written because they are lexer-internal mechanics, not domain knowledge:

- **Mode stack push/pop logic** — structural scanning behavior for interpolation nesting
- **Content buffer accumulation** — character-by-character scanning with escape resolution
- **Number literal scanning** — digit/dot/exponent pattern recognition
- **Comment and whitespace scanning** — simple character-class checks
- **Error recovery logic** — how to advance state after each error condition

These are the lexer's implementation, not its vocabulary. They change when the scanning algorithm changes, not when the language surface changes.

---

## Dependencies and Integration Points

### Grammar Generation

The TextMate grammar (`tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`) is generated from catalog metadata. Each `TokenMeta.TextMateScope` value drives grammar pattern emission. When a token is added to the catalog with a `TextMateScope`, it appears in syntax highlighting without grammar edits.

### Language Server Semantic Tokens

`TokenMeta.SemanticTokenType` drives semantic token classification. The language server maps each token's catalog-declared type to LSP semantic token types. No per-token switch in the language server.

### Completions

Keyword completions are derived from `Tokens.All`. `TokenMeta.ValidAfter` controls which completions appear after which tokens — the language server reads this metadata, not a hardcoded completion table.

### MCP Language Tool

The `precept_language` MCP tool serializes the Tokens catalog (among others) as part of its vocabulary output. The tool reads catalog entries directly — it has no parallel keyword list.

---

## Failure Modes and Recovery

The lexer always scans to EOF, accumulating all diagnostics. No exception is thrown; no scanning is abandoned.

| Error condition | Diagnostic code | Recovery action |
|---|---|---|
| Source exceeds 64KB | `InputTooLarge` | Immediate return: `EndOfSource` + diagnostic |
| `"` unclosed at EOF or newline | `UnterminatedStringLiteral` | Pop String mode, flush content buffer |
| `'` unclosed at EOF or newline | `UnterminatedTypedConstant` | Pop TypedConstant mode, flush content buffer |
| `{` unclosed at EOF or newline | `UnterminatedInterpolation` | Pop Interpolation mode, resume enclosing literal |
| `\X` unrecognized escape | `UnrecognizedStringEscape` / `UnrecognizedTypedConstantEscape` | Skip `\X`, continue scanning |
| Lone `}` in literal | `UnescapedBraceInLiteral` | Preserve in `Text`, continue |
| Unrecognized character | `InvalidCharacter` | Skip character, continue |

### Recovery Strategy

- **Newline in interpolation**: Emits `UnterminatedInterpolation`, pops back to enclosing literal mode, leaves newline unconsumed for that mode's own unterminated diagnostic.
- **No synthetic closing tokens**: Missing `StringEnd`/`TypedConstantEnd` is a structural signal. The parser handles it — no fake tokens.
- **Escape recovery guard**: After `\`, if the next character is a newline or EOF, the lexer does not double-advance. The newline triggers the unterminated-literal diagnostic.
- **Lone `}` preservation**: Content is not altered — the author sees their own text in error messages.

### Diagnostic Catalog Integration

All diagnostic messages are written for the **domain author** — a business analyst or domain expert, not a .NET developer. The lexer passes `DiagnosticCode` values to `Diagnostics.Create()`; the Diagnostics catalog owns the message templates. Diagnostic vocabulary is deliberately non-compiler: "Text value opened with `"` is missing its closing quote" rather than "Unterminated string literal."

---

## Contracts and Guarantees

`Token.Offset` + `Token.Length` spans the full source extent including delimiters:

| Token kind | Span coverage |
|------------|---------------|
| `StringLiteral` | Opening `"` through closing `"` (inclusive) |
| `StringStart` | Opening `"` through and including the `{` |
| `StringMiddle` | The `}` through and including the next `{` |
| `StringEnd` | The `}` through closing `"` (inclusive) |
| `TypedConstant` | Opening `'` through closing `'` (inclusive) |
| `TypedConstantStart/Middle/End` | Same rules as string variants, with `'` delimiters |
| `NumberLiteral` | All numeric characters including decimal point and exponent |
| Keywords, identifiers | The word characters only |
| Operators, punctuation | The operator characters |
| `NewLine` | The `\n` or `\r\n` characters |
| `Comment` | From `#` through the last character before the line break |

---

## Design Rationale and Decisions

Design decisions are cataloged in `docs/compiler-and-runtime-design.md §4`. Key decisions:

- One-pass, pure-function design — no state between calls, directly testable
- Catalog-derived vocabulary — no hardcoded keyword table in lexer code
- `FrozenDictionary` + span lookup — zero allocation for keyword identification
- Fixed-size mode stack — bounded allocation for interpolation nesting
- 64KB source limit — bounded execution time and memory, adversarial input protection

---

## Innovation

- **Catalog-driven token recognition:** `TokenKind` derives from catalog metadata, not a parallel enum. Adding a keyword to the Tokens catalog automatically makes it lexable, highlightable, and completable with no lexer code change.
- **No vocabulary ownership:** Traditional lexers maintain a hardcoded keyword table. Precept's lexer reads its vocabulary from the same metadata that drives every other consumer.
- **`FrozenDictionary` + span lookup:** Keyword identification uses `GetAlternateLookup<ReadOnlySpan<char>>()` to avoid string allocation for every identifier candidate.

---

## Open Questions / Implementation Notes

1. Validate interpolation max depth (8) against real sample corpus — confirm no realistic program exceeds 4 nesting levels.
2. Confirm `SetType` dual-use disambiguation is correctly handled in all parser contexts before parser implementation.

---

## Operational Constraints

### Source Size Limit

```csharp
private const int MaxSourceLength = 65_536;
```

A 64KB limit is 10–13× the largest realistic Precept file. Input beyond this is almost certainly adversarial. The check is the first thing `Lex()` does — no allocation, no scanning, immediate return with `InputTooLarge`.

### Bounded Work Guarantee

The lexer advances at least one character per loop iteration. Combined with the source size limit, this guarantees bounded execution time and memory usage regardless of input content.

---

## Deliberate Exclusions

- **No semantic knowledge:** The lexer does not resolve meaning, only pattern.
- **No streaming/incremental:** The full source is lexed in one pass; incremental lexing is not needed at the 64KB ceiling.
- **No trivia attachment:** Whitespace is consumed silently; no trivia nodes are emitted.

---

## Cross-References

| Topic | Document |
|-------|----------|
| Tokens catalog drives keyword/operator/punctuation lookup tables | `docs/language/catalog-system.md` |
| Parser consumes TokenStream; resolves dual-use token disambiguation | `docs/compiler/parser.md` |
| DiagnosticCode for lex-phase errors | `docs/compiler/diagnostic-system.md` |
| String/typed constant segmentation, escape tables, mode stack modes | `docs/compiler/literal-system.md` |
| Pipeline stage ordering, artifact types | `docs/compiler-and-runtime-design.md` |
| `set` disambiguation | `docs/language/precept-language-spec.md` § 1.7 |

---

## Source Files

| File | Purpose |
|------|---------|
| `src/Precept/Pipeline/Lexer.cs` | Lexer implementation — `Lexer` static class, `Scanner` struct, `ModeState` struct, `LexerMode` enum (~815 lines) |
| `src/Precept/Pipeline/TokenStream.cs` | `TokenStream` — immutable token sequence artifact |
| `src/Precept/Pipeline/SourceSpan.cs` | `SourceSpan` — span type used by all tokens |
| `src/Precept/Language/TokenKind.cs` | `TokenKind` enum — all token kind values |
| `src/Precept/Language/Token.cs` | `Token` record struct — kind, text, line, column, offset, length; `TokenMeta` record with catalog metadata |
| `src/Precept/Language/Tokens.cs` | `Tokens.All` — exhaustive token catalog; `Tokens.Keywords` — `FrozenDictionary<string, TokenKind>` |
| `src/Precept/Language/Diagnostics.cs` | `Diagnostics.Create` — diagnostic message factory (domain-author messages) |
| `src/Precept/Language/DiagnosticCode.cs` | `DiagnosticCode` enum — includes all lex-stage codes |
