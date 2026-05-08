# Lexer

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Implemented |
| Source | `src/Precept/Pipeline/Lexer.cs`, `src/Precept/Pipeline/TokenStream.cs`, `src/Precept/Pipeline/SourceSpan.cs` |
| Upstream | Source text (`string`) |
| Downstream | Parser, Language Server lexical features, MCP compile tool |

---

## 2. Overview

The lexer is the first stage of the Precept compilation pipeline. It transforms raw source text into a flat sequence of `Token` values — the atomic units that every downstream stage consumes. The lexer has no knowledge of grammar, scoping, or semantics; it recognizes character patterns and classifies them using metadata from the Tokens catalog.

Every token the lexer emits carries its kind, text, and precise source location:

```csharp
public readonly record struct Token(TokenKind Kind, string Text, SourceSpan Span);
```

The `SourceSpan` carries both coordinate systems — offset/length for source slicing, line/column for LSP positions — as a single value:

```csharp
public readonly record struct SourceSpan(
    int Offset,      // 0-based character offset
    int Length,      // number of characters spanned
    int StartLine,   // 1-based line number of first character
    int StartColumn, // 1-based column of first character
    int EndLine,     // 1-based line number of last character (inclusive)
    int EndColumn);  // 1-based column one past the last character (exclusive, like LSP)
```

The lexer's output is a `TokenStream` — an immutable record carrying the token sequence and any diagnostics accumulated during scanning:

```csharp
public sealed record class TokenStream(
    ImmutableArray<Token>      Tokens,
    ImmutableArray<Diagnostic> Diagnostics);
```

The lexer runs once per source text and produces the complete stream before any downstream stage begins. It is a pure function: no instance state, no DI, no configuration.

---

## 3. Responsibilities and Boundaries

**OWNS:**
- Token classification (keywords, identifiers, literals, operators, punctuation)
- Source span attribution (offset, length, line, column for every token)
- Interpolation mode tracking (mode stack for `{expr}` nesting in literals)
- Literal boundary detection (string, typed constant start/middle/end segmentation)
- Keyword lookup (catalog-derived `FrozenDictionary` recognition)
- Operator/punctuation scanning (catalog-derived single/two-char tables)

**Does NOT OWN:**
- Grammar structure (Parser)
- Name resolution (TypeChecker)
- Numeric interpretation — integer vs. decimal, range validation (TypeChecker)
- Typed-constant content validation — unit names, date formats, currency codes (TypeChecker)
- Comment content (discarded after emitting `TokenKind.Comment`)

---

## 4. Right-Sizing

The lexer is intentionally narrow: one pass, pure function, no state carried across calls. It recognizes patterns, not meaning.

**64KB source ceiling.** The limit is 10–13× the largest realistic Precept file. Input beyond this is almost certainly adversarial. The check is the first thing `Lex()` does — no allocation, no scanning, immediate return with `InputTooLarge`. This bound makes the full lex pass sub-millisecond; no streaming or incremental API is needed.

**Fixed-size mode stack (depth 8).** The interpolation mode stack covers 4 full nesting levels (string → interpolation → typed constant → interpolation), well beyond realistic use. A `ModeState[8]` array with a depth counter replaces a general-purpose `Stack<T>`, avoiding heap allocation per scan.

**Bounded work guarantee.** The lexer advances at least one character per loop iteration. Combined with the source size limit, this guarantees bounded execution time and memory usage regardless of input content.

**No trivia.** Whitespace is consumed silently. Precept has no formatting/refactoring requirements that need whitespace round-trip fidelity. This keeps the token stream lean and downstream consumers simple.

---

## 5. Inputs and Outputs

```
string source  →  Lexer.Lex  →  TokenStream
```

**Signature:**

```csharp
public static TokenStream Lex(string source)
```

`Lexer.Lex(string source)` is a static pure function. No instance, no DI, no configuration. This matches the pipeline pattern used by all six stages (`Lexer.Lex`, `Parser.Parse`, `NameBinder.Bind`, `TypeChecker.Check`, `GraphAnalyzer.Analyze`, `ProofEngine.Prove`). Tests call the method directly and assert on the output.

**Input families:**

| Family | Description |
|--------|-------------|
| Primitive tokens | Identifiers, keywords, numeric literals, operators, punctuation, whitespace-adjacent tokens (newlines, comments) |
| String literals | `"..."` with `\"`, `\\`, `\n`, `\t` escapes and `{expr}` interpolation |
| Typed constant literals | `'...'` with `\'`, `\\` escapes and `{expr}` interpolation |

Typed constant content is opaque to the lexer. The lexer marks the boundaries (`TypedConstant`, `TypedConstantStart`, `TypedConstantMiddle`, `TypedConstantEnd`) but does not interpret what's inside. Type resolution and content validation happen in the type checker.

**Output:**

The `TokenStream` is the `Compilation.Tokens` field — it is part of the tooling surface and queryable by the language server for span-based operations. The stream always ends with `TokenKind.EndOfSource`.

---

## 6. Architecture

### Catalog-Driven Design

The lexer is a character-pattern scanner. It does not encode domain knowledge — it reads domain knowledge from catalogs. When a new keyword, operator, or punctuation token is added to the Tokens catalog, the lexer recognizes it without code changes. When the Diagnostics catalog gains a new lex-stage code, the lexer can emit it without new switch branches.

### The Tokens Catalog as Source of Truth

The Tokens catalog (`Tokens.All`) is the exhaustive inventory of every token the lexer can produce. Each entry is a `TokenMeta` record carrying:

| Field | Purpose |
|-------|---------|
| `Kind` | The `TokenKind` enum value |
| `Text` | Surface text for keywords/operators/punctuation (null for dynamic tokens like `Identifier`, `NumberLiteral`) |
| `Categories` | Classification tags (`TokenCategory[]`) used by tooling and table derivation |
| `Description` | Human-readable description for MCP/tooling |
| `TextMateScope` | Syntax highlighting scope for grammar generation |
| `SemanticTokenType` | LSP semantic token type |
| `ValidAfter` | Completion context metadata — which tokens may precede this one |
| `IsAccessModeAdjective` | True for `readonly`, `editable` (access-mode contextual keywords) |
| `IsValidAsMemberName` | True for `min`, `max` (keywords valid after `.` as member names) |

The lexer derives its keyword lookup table, operator scan tables, and punctuation scan table from this catalog at static initialization. No parallel copies of token membership exist anywhere in the lexer.

### Derived Lookup Tables

| Table | Type | Derived From | Purpose |
|-------|------|--------------|---------|
| `Tokens.Keywords` | `FrozenDictionary<string, TokenKind>` | Entries with keyword-bearing categories | Keyword vs. identifier classification |
| `Tokens.TwoCharOperators` | `FrozenDictionary<(char, char), (TokenKind, string)>` | Entries with 2-char text and `Operator` category | Two-character operator recognition |
| `Tokens.SingleCharOperators` | `FrozenDictionary<char, (TokenKind, string)>` | Entries with 1-char text and `Operator` category | Single-character operator recognition |
| `Tokens.TwoCharOperatorStarters` | `FrozenSet<char>` | First char of each two-char operator | Guard before tuple lookup |
| `Tokens.PunctuationChars` | `FrozenDictionary<char, (TokenKind, string)>` | Entries with 1-char text and `Punctuation` category | Punctuation recognition |

### No Parallel Copies

If the lexer were to switch on token kinds to apply per-token behavior, that behavior would belong in catalog metadata — not in lexer code. `TryScanOperator` and `TryScanPunctuation` perform table lookups against `FrozenDictionary` tables derived from `Tokens.All`. Adding a new operator or punctuation token requires only a catalog entry — no scan-method update.

### Scanner Structure

All mutable scanning state lives in a private `Scanner` struct instantiated inside `Lex()` and discarded after:

```csharp
public static TokenStream Lex(string source)
{
    if (source.Length > MaxSourceLength)
    {
        return new TokenStream(
            ImmutableArray.Create(new Token(TokenKind.EndOfSource, "", new SourceSpan(0, 0, 1, 1, 1, 1))),
            ImmutableArray.Create(Diagnostics.Create(DiagnosticCode.InputTooLarge, new SourceSpan(0, 0, 1, 1, 1, 1))));
    }

    var scanner = new Scanner(source);
    scanner.ScanAll();
    return scanner.Build();
}
```

`Scanner` is stack-allocated. The only heap allocations per scan are the `ImmutableArray.Builder` instances, a resizable `char[]` content buffer for literals, and the final `TokenStream` output.

---

## 7. Component Mechanics

### Main Loop

`ScanAll()` dispatches on the current mode. In `Normal` mode, the main loop tries in order:

1. Whitespace (space, tab) — consume silently
2. Newline (`\n`, `\r\n`, `\r`) — emit `NewLine` token
3. Comment (`#`) — scan to EOL, emit `Comment`
4. Letter → identifier or keyword
5. Digit → number literal
6. `"` → enter String mode
7. `'` → enter TypedConstant mode
8. Operator character → `TryScanOperator`
9. Punctuation character → `TryScanPunctuation`
10. Otherwise → `InvalidCharacter` diagnostic, skip character

Each branch either emits a token or a diagnostic, advances position, and returns to the loop.

### Interpolation Mode Stack

Interpolation creates a nesting problem: `{expr}` inside `"..."` or `'...'` re-enters Normal-like scanning, which may encounter new literals. The solution is a mode stack with four modes:

| Mode | Enum Value | Description |
|------|------------|-------------|
| `Normal` | 0 | Top-level scanning — keywords, identifiers, operators, punctuation |
| `String` | 1 | Inside `"..."` — accumulates literal characters, handles escapes and `{`/`}` |
| `TypedConstant` | 2 | Inside `'...'` — accumulates literal characters, handles escapes and `{`/`}` |
| `Interpolation` | 3 | Inside `{...}` inside a literal — scans as Normal, ends at matching `}` |

The stack is a fixed-size `ModeState[8]` array with a depth counter:

```csharp
private struct ModeState
{
    public LexerMode Mode;
    public int SegmentIndex;      // 0 = first segment, 1 = second, etc.
    public int SegStartOffset;    // Span origin for current segment
    public int SegStartLine;
    public int SegStartColumn;
}
```

Maximum nesting of 8 supports 4 full nesting levels (string → interpolation → typed constant → interpolation), well beyond realistic use. When the depth limit is reached, the lexer emits `UnterminatedInterpolation` and recovers.

Each stack entry carries the mode, segment index, and the source position of the delimiter that began the current segment. This ensures correct span attribution for `StringStart`/`StringMiddle`/`TypedConstantStart`/`TypedConstantMiddle` tokens.

### Keyword and Identifier Recognition

The lexer reads a word (letters, digits, `_`), then checks `Tokens.Keywords` for a match:

```csharp
_keywordLookup = Tokens.Keywords.GetAlternateLookup<ReadOnlySpan<char>>();
var kind = _keywordLookup.TryGetValue(span, out var kw) ? kw : TokenKind.Identifier;
```

`GetAlternateLookup<ReadOnlySpan<char>>()` accepts a span directly, avoiding a `new string(...)` allocation for every identifier candidate. `FrozenDictionary` uses a perfect hash at construction time.

**Keyword categories selected for `Tokens.Keywords`:**
- Declaration, Preposition, Control, Action, Outcome, AccessMode
- LogicalOperator, Membership, Quantifier, StateModifier
- Constraint, Type, Literal

`SetType` is explicitly excluded — `set` always emits as `TokenKind.Set`; the parser disambiguates contextually.

### Operator Recognition

**Maximal munch** is enforced by table structure: `TryScanOperator` consults `TwoCharOperators` before `SingleCharOperators`.

```csharp
private bool TryScanOperator()
{
    var c = Current;
    if (Tokens.TwoCharOperatorStarters.Contains(c))
    {
        if (Tokens.TwoCharOperators.TryGetValue((c, PeekNext), out var two))
        {
            Advance(); Advance();
            _tokens.Add(new Token(two.Kind, two.Text, ...));
            return true;
        }
    }

    if (Tokens.SingleCharOperators.TryGetValue(c, out var one))
    {
        Advance();
        _tokens.Add(new Token(one.Kind, one.Text, ...));
        return true;
    }

    return false;
}
```

The two-char check is guarded by a `TwoCharOperatorStarters.Contains(c)` test — a `FrozenSet` lookup — before constructing the `(c, next)` tuple, avoiding tuple allocation on characters that cannot start a two-char operator.

**Two-character operators:** `->`, `!~`, `~=`, `==`, `!=`, `>=`, `<=`

**Single-character operators:** `=`, `>`, `<`, `+`, `-`, `*`, `/`, `%`, `~`

### Punctuation Recognition

`TryScanPunctuation` performs a single `PunctuationChars.TryGetValue(Current, ...)` lookup. If the current character is in the table, one character is consumed and the token is emitted.

**Delimiter characters** (`{`, `}`, `"`, `'`) are absent from the punctuation table — they trigger mode transitions in `ScanToken` and are never emitted as punctuation tokens.

**Punctuation characters:** `.`, `,`, `(`, `)`, `[`, `]`

### String Literal Scanning

A `"` in Normal or Interpolation mode pushes `String` mode. The scanner accumulates characters into a reusable `char[]` content buffer.

**Recognized escapes:**
| Escape | Decoded |
|--------|---------|
| `\"` | `"` |
| `\\` | `\` |
| `\n` | newline |
| `\t` | tab |

**Brace escapes:**
| Input | Effect |
|-------|--------|
| `{{` | Literal `{` in content |
| `}}` | Literal `}` in content |
| `{` (single) | Push Interpolation mode, emit `StringStart` or `StringMiddle` |
| `}` (single) | Emit `UnescapedBraceInLiteral` diagnostic, preserve in content |

**Token emission:**

| Condition | Token Kind |
|-----------|------------|
| No interpolation, closing `"` found | `StringLiteral` |
| Interpolation present, first segment before `{` | `StringStart` |
| Interpolation present, segment between `}` and `{` | `StringMiddle` |
| Interpolation present, final segment after `}` to `"` | `StringEnd` |

### Typed Constant Scanning

A `'` in Normal or Interpolation mode pushes `TypedConstant` mode. Same structural pattern as strings, with `'` delimiters.

**Recognized escapes:**
| Escape | Decoded |
|--------|---------|
| `\'` | `'` |
| `\\` | `\` |

Note: `\n` and `\t` are **not** recognized in typed constants — they emit `UnrecognizedTypedConstantEscape`.

Content is opaque — the lexer does not validate unit names, date formats, or currency codes. The type checker handles that.

### Number Literal Scanning

The lexer scans:
1. Integer part: one or more digits
2. Optional decimal part: `.` followed by digits (only if digit follows `.`)
3. Optional exponent: `e`/`E` with optional `+`/`-` followed by digits

```csharp
// Decimal part: '.' only if followed by a digit
if (!IsAtEnd && Current == '.' && _offset + 1 < _source.Length && IsDigit(_source[_offset + 1]))
{
    Advance(); // '.'
    while (!IsAtEnd && IsDigit(Current))
        Advance();
}
```

All numeric tokens emit as `TokenKind.NumberLiteral` with `Token.Text` carrying the raw digit characters. Numeric interpretation (integer vs. decimal, range validation) is the type checker's responsibility.

### Comment Scanning

`#` begins a line comment. The lexer scans to the end of the line and emits `TokenKind.Comment` with `Token.Text` carrying the full text including the leading `#`. The newline is left unconsumed for the next iteration.

### Whitespace Handling

Spaces and tabs are consumed silently in a loop. No trivia tokens are emitted.

### Newline Handling

`\n`, `\r\n`, and bare `\r` emit `TokenKind.NewLine`. Newlines are structurally significant to the grammar (statement termination) and must be preserved as tokens for the parser.

```csharp
private void EmitNewLine(int length)
{
    int startLine = _line, startCol = _column, startOff = _offset;
    for (int i = 0; i < length; i++)
        Advance();
    _tokens.Add(new Token(TokenKind.NewLine, "", ...));
    _line++;
    _column = 1;
}
```

### Token.Text for Quoted Literals

`Token.Text` carries the **decoded content** — escape sequences resolved, delimiters excluded. `Token.Span.Offset` + `Token.Span.Length` spans the full source extent including delimiters. This means `Token.Span.Length != Token.Text.Length` for quoted literals.

### Dual-Use Token Strategy

#### set / SetType

`set` always emits as `TokenKind.Set`. The parser determines from syntactic context whether it introduces a `set<T>` type annotation (field declaration) or a `set` action (event body). `SetType` is excluded from `Tokens.Keywords` — it exists only as a synthetic kind the parser produces.

```precept
field Tags : set<string>       # set → TokenKind.Set; parser sees set<T> type context → SetType
set Status to "active"          # set → TokenKind.Set; parser sees assignment context → set action
when Status is set              # set → TokenKind.Set; parser sees guard context → null-narrowing
when Status is not set          # set → TokenKind.Set; parser sees negated guard → null-narrowing
```

#### min / max

`min` and `max` each have one `TokenKind` entry. Their dual role as constraint keywords and aggregation functions is resolved by the parser based on syntactic context. The Tokens catalog classifies them as `TokenCategory.Constraint` only — not `TokenCategory.Function` — because the lexer emits them; the parser interprets them. They are marked with `IsValidAsMemberName: true` to allow `x.min` member access.

```precept
field Score : integer min 0 max 100    # min/max → constraint keywords (field bounds)
computed Total = max(LineAmount)        # max → aggregation function (expression context)
```

### What Stays Hand-Written

Not everything is catalog-driven. The following remain hand-written because they are lexer-internal mechanics, not domain knowledge:

| Component | Why Hand-Written |
|-----------|------------------|
| Character-class routing dispatch | Entry points for letter/digit/string/operator/punctuation scanning |
| Mode stack push/pop logic | Structural scanning behavior for interpolation nesting |
| Content buffer accumulation | Character-by-character scanning with escape resolution |
| Number literal scanning | Digit/dot/exponent pattern recognition |
| Comment and whitespace scanning | Simple character-class checks |
| Error recovery logic | How to advance state after each error condition |

These are the lexer's implementation, not its vocabulary. They change when the scanning algorithm changes, not when the language surface changes.

---

## 8. Dependencies and Integration Points

### Catalog Dependencies

| Catalog | How Lexer Uses It |
|---------|-------------------|
| `Tokens.All` | Source of truth for all token metadata |
| `Tokens.Keywords` | Keyword vs. identifier classification (span-based lookup) |
| `Tokens.TwoCharOperators` | Two-character operator recognition |
| `Tokens.SingleCharOperators` | Single-character operator recognition |
| `Tokens.TwoCharOperatorStarters` | Guard set for two-char operator lookahead |
| `Tokens.PunctuationChars` | Punctuation character recognition |
| `Diagnostics.Create` | Diagnostic message factory (lexer passes codes, not strings) |

### Downstream Consumers

| Consumer | What It Reads |
|----------|---------------|
| Parser | `TokenStream.Tokens` — iterates through token sequence, resolves dual-use tokens contextually |
| Language Server (semantic tokens) | `TokenStream.Tokens` — maps each token to LSP semantic token type via `TokenMeta.SemanticTokenType` |
| Language Server (diagnostics) | `TokenStream.Diagnostics` — surfaces lex-phase errors in Problems panel |
| MCP compile tool | Full `Compilation` including `TokenStream` |

### Grammar Generation

The TextMate grammar (`tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`) is generated from catalog metadata. Each `TokenMeta.TextMateScope` value drives grammar pattern emission. When a token is added to the catalog with a `TextMateScope`, it appears in syntax highlighting without grammar edits.

### Language Server Completions

Keyword completions are derived from `Tokens.All`. `TokenMeta.ValidAfter` controls which completions appear after which tokens — the language server reads this metadata, not a hardcoded completion table.

### MCP Language Tool

The `precept_language` MCP tool serializes the Tokens catalog (among others) as part of its vocabulary output. The tool reads catalog entries directly — it has no parallel keyword list.

---

## 9. Failure Modes and Recovery

The lexer always scans to EOF, accumulating all diagnostics. No exception is thrown; no scanning is abandoned.

### Diagnostic Codes

| Diagnostic Code | Trigger | Recovery Action |
|-----------------|---------|-----------------|
| `InputTooLarge` | Source exceeds 64KB | Immediate return: `EndOfSource` + diagnostic (no scanning) |
| `UnterminatedStringLiteral` | `"` unclosed at EOF or newline | Pop String mode, emit segment token, flush content buffer |
| `UnterminatedTypedConstant` | `'` unclosed at EOF or newline | Pop TypedConstant mode, emit segment token, flush content buffer |
| `UnterminatedInterpolation` | `{` unclosed at EOF/newline, or depth limit exceeded | Pop Interpolation mode, resume enclosing literal |
| `InvalidCharacter` | Unrecognized character in Normal/Interpolation mode | Emit diagnostic with character display, skip character, continue |
| `UnrecognizedStringEscape` | `\X` where X is not `"`, `\`, `n`, or `t` | Emit diagnostic, skip `\X` (unless X is newline/EOF), continue |
| `UnrecognizedTypedConstantEscape` | `\X` where X is not `'` or `\` | Emit diagnostic, skip `\X` (unless X is newline/EOF), continue |
| `UnescapedBraceInLiteral` | Lone `}` in string/typed constant (not `}}`) | Emit diagnostic, preserve `}` in content, continue |

### Recovery Strategies

**Newline in interpolation:** Emits `UnterminatedInterpolation`, pops back to enclosing literal mode, leaves newline unconsumed. The literal scanner then fires its own unterminated diagnostic on the same character.

**No synthetic closing tokens:** Missing `StringEnd`/`TypedConstantEnd` is a structural signal. The parser handles incomplete literals — the lexer does not emit fake closing tokens.

**Escape recovery guard:** After `\`, if the next character is a newline or EOF, the lexer does not double-advance. The newline triggers the unterminated-literal diagnostic instead.

**Lone `}` preservation:** Content is not altered — the author sees their own text in error messages.

**Depth limit recovery:** When interpolation depth exceeds 8, the lexer emits `UnterminatedInterpolation`, scans forward for a `}` at depth 0 (or EOL), and resumes in the enclosing literal mode.

### EOF Cleanup

At end of source, `Build()` walks the mode stack from innermost to outermost and emits unterminated diagnostics for any open String, TypedConstant, or Interpolation modes:

```csharp
for (int i = _modeDepth - 1; i >= 1; i--)
{
    ref var s = ref _modeStack[i];
    var code = s.Mode switch
    {
        LexerMode.Interpolation  => DiagnosticCode.UnterminatedInterpolation,
        LexerMode.String         => DiagnosticCode.UnterminatedStringLiteral,
        LexerMode.TypedConstant  => DiagnosticCode.UnterminatedTypedConstant,
        _                        => DiagnosticCode.UnterminatedStringLiteral,
    };
    _diagnostics.Add(Diagnostics.Create(code, span));
}
```

### Diagnostic Audience

All diagnostic messages are written for the **domain author** — a business analyst or domain expert, not a .NET developer. The lexer passes `DiagnosticCode` values to `Diagnostics.Create()`; the Diagnostics catalog owns the message templates. Diagnostic vocabulary is deliberately non-compiler: "Text value opened with `"` is missing its closing quote" rather than "Unterminated string literal."

---

## 10. Contracts and Guarantees

### Token Span Coverage

`Token.Span.Offset` + `Token.Span.Length` spans the full source extent including delimiters:

| Token Kind | Span Coverage |
|------------|---------------|
| `StringLiteral` | Opening `"` through closing `"` (inclusive) |
| `StringStart` | Opening `"` through and including the `{` |
| `StringMiddle` | The `}` through and including the next `{` |
| `StringEnd` | The `}` through closing `"` (inclusive) |
| `TypedConstant` | Opening `'` through closing `'` (inclusive) |
| `TypedConstantStart/Middle/End` | Same rules as string variants, with `'` delimiters |
| `NumberLiteral` | All numeric characters including decimal point and exponent |
| Keywords, identifiers | The word characters only |
| Operators, punctuation | The operator/punctuation characters |
| `NewLine` | The `\n` or `\r\n` characters |
| `Comment` | From `#` through the last character before the line break |

### Stream Guarantees

1. **Terminal token:** The stream always ends with exactly one `EndOfSource` token.
2. **Non-empty:** The stream always contains at least one token (`EndOfSource`).
3. **Contiguous source coverage:** Every character in the source is covered by exactly one token span (except horizontal whitespace — spaces and tabs — which is consumed silently; newlines emit `NewLine` tokens).
4. **Line/column consistency:** Line numbers are 1-based. Column numbers are 1-based. After a newline, line increments and column resets to 1.
5. **Offset/length consistency:** For every token, `source.Substring(token.Span.Offset, token.Span.Length)` returns the original source text (including delimiters for quoted literals).

### Mode Stack Invariants

1. **Normal is always at depth 0:** The mode stack always has `Normal` as its base (never popped).
2. **Alternating parity:** Literal modes (`String`, `TypedConstant`) can only push `Interpolation`. `Interpolation` can only push literal modes. This alternation is enforced by the scanning logic.
3. **Bounded depth:** Maximum depth is 8. The lexer emits a diagnostic and recovers if this limit is reached.

### Content Buffer Contract

For quoted literals (`StringLiteral`, `StringStart`, `StringMiddle`, `StringEnd`, `TypedConstant`, `TypedConstantStart`, `TypedConstantMiddle`, `TypedConstantEnd`):

1. **Delimiters excluded:** `Token.Text` does not include the opening/closing `"` or `'`.
2. **Escapes resolved:** `\"` → `"`, `\\` → `\`, `\n` → newline, `\t` → tab, `\'` → `'`, `{{` → `{`, `}}` → `}`.
3. **Interpolation boundaries excluded:** `Token.Text` does not include the `{` or `}` that mark interpolation boundaries.

---

## 11. Design Rationale and Decisions

### One-Pass Pure Function

**Decision:** The lexer is a static pure function with no instance state between calls.

**Rationale:** Pure functions are directly testable — tests call `Lexer.Lex(source)` and assert on the output without setup or teardown. No DI, no configuration, no hidden state. This matches the pipeline pattern used by all six compilation stages.

### Catalog-Derived Vocabulary

**Decision:** The lexer derives all keyword/operator/punctuation recognition tables from `Tokens.All` at static initialization. No hardcoded keyword table exists in lexer code.

**Rationale:** Adding a new keyword or operator to the language should not require lexer code changes. The Tokens catalog is the single source of truth; the lexer is generic machinery that reads it. This eliminates the "forgot to update the lexer" class of bugs entirely.

### FrozenDictionary + Span Lookup

**Decision:** Keyword lookup uses `FrozenDictionary<string, TokenKind>` with `GetAlternateLookup<ReadOnlySpan<char>>()`.

**Rationale:** Zero allocation for keyword identification. Every identifier candidate is checked against the keyword table; allocating a `new string(...)` for each check would dominate lexer allocations. `FrozenDictionary` uses a perfect hash at construction time, making lookups O(1) with minimal cache misses. `ReadOnlySpan<char>` lookup avoids copying the identifier text before checking.

### Fixed-Size Mode Stack

**Decision:** The interpolation mode stack is a `ModeState[8]` array with a depth counter, not a `Stack<T>`.

**Rationale:** Bounded allocation. The stack is allocated once per scan (stack-allocated inside the `Scanner` struct). No per-interpolation heap allocation. The depth limit of 8 supports 4 full nesting levels — more than any realistic program needs.

### 64KB Source Limit

**Decision:** Sources larger than 65,536 characters are rejected immediately with `InputTooLarge`.

**Rationale:** The limit is 10–13× the largest realistic Precept file. Input beyond this is almost certainly adversarial or a bug in the calling code. The bound makes the lexer's execution time and memory usage predictable. No streaming or incremental API is needed.

### No Trivia

**Decision:** Whitespace (spaces, tabs) is consumed silently. No trivia tokens are emitted.

**Rationale:** Precept has no formatting or refactoring tools that need whitespace round-trip fidelity. Keeping the token stream lean simplifies every downstream consumer. If trivia becomes necessary in the future, it can be added without breaking changes (additional fields, not changed semantics).

### Newline as Token

**Decision:** Newlines emit `TokenKind.NewLine` tokens rather than being consumed silently like whitespace.

**Rationale:** Newlines are structurally significant to the grammar — they terminate statements. The parser needs to see them. This is different from C-family languages where newlines are whitespace and semicolons terminate statements.

### Dual-Use Tokens (set, min, max)

**Decision:** `set`, `min`, and `max` each emit a single `TokenKind`. The parser resolves their dual-use contextually.

**Rationale:** The lexer recognizes patterns, not meaning. Whether `set` introduces a type or an action depends on syntactic context (e.g., `field X : set<string>` vs. `set X to ...`). Pushing this to the parser keeps the lexer simple and context-free. `SetType` exists as a synthetic kind for the parser to produce — it is never emitted by the lexer.

### Separate Escape Tables for String vs. Typed Constant

**Decision:** String literals support `\"`, `\\`, `\n`, `\t`. Typed constants support only `\'`, `\\`.

**Rationale:** Typed constants carry domain values (dates, currencies, units) that are often pasted from external sources. Supporting `\n` and `\t` would create surprising behavior when a user pastes a value containing a backslash. The minimal escape set (`\'`, `\\`) is sufficient for embedding quotes and backslashes.

---

## 12. Innovation

### Catalog-Driven Token Recognition

Traditional lexers maintain a hardcoded keyword table as a switch statement, array, or dictionary defined in lexer code. Adding a keyword means editing the lexer. Precept's lexer reads its vocabulary from the same `Tokens.All` catalog that drives:

- TextMate grammar generation (`TextMateScope`)
- Language server semantic tokens (`SemanticTokenType`)
- Completion filtering (`ValidAfter`)
- MCP vocabulary output
- Parser dual-use resolution

Adding a keyword to the Tokens catalog automatically makes it lexable, highlightable, and completable with no lexer code change. The lexer has no vocabulary ownership.

### Zero-Allocation Keyword Identification

The combination of `FrozenDictionary` (perfect hash) and `GetAlternateLookup<ReadOnlySpan<char>>()` (span-based lookup) means keyword identification allocates nothing. Every identifier candidate — which in typical Precept code is most words — is checked against the keyword table without creating a temporary string.

### Metadata-Driven Maximal Munch

The two-character operator table (`TwoCharOperators`) and single-character operator table (`SingleCharOperators`) are both derived from catalog entries. The lexer's maximal-munch logic is just:

1. Check if current char is in `TwoCharOperatorStarters`
2. If yes, try `TwoCharOperators[(c, next)]`
3. If no match, try `SingleCharOperators[c]`

Adding a new two-character operator (e.g., a hypothetical `<>`) requires only a catalog entry. The lexer's table-driven structure handles it automatically.

### Fixed-Depth Mode Stack

Most lexers handle string interpolation with a recursive scanner or a heap-allocated stack. Precept's fixed `ModeState[8]` array eliminates per-interpolation allocation while supporting nested interpolation 4 levels deep. The parity invariant (literal ↔ interpolation ↔ literal) makes the depth bound tight.

---

## 13. Open Questions / Implementation Notes

**Implementation Notes:**

1. The interpolation depth limit (8) has been validated against the sample corpus in `samples/`. No realistic program exceeds 4 nesting levels (string → interpolation → typed constant → interpolation).

2. `SetType` dual-use disambiguation is fully handled in the parser. The lexer always emits `TokenKind.Set`; the parser recognizes `set<T>` type context and internally treats it as `SetType`.

3. `min` and `max` member access (`x.min`, `x.max`) is enabled by `TokenMeta.IsValidAsMemberName: true`. The parser allows these keywords after `.` in member-access expressions.

**Potential Future Considerations:**

- If Unicode identifiers become necessary, the `IsLetter`/`IsWordChar` functions would need to expand beyond ASCII. Current implementation is ASCII-only (`a-z`, `A-Z`, `0-9`, `_`).
- If multi-line strings are added, the literal scanning logic would need a mode for raw/verbatim strings that don't terminate at newlines.

---

## 14. Deliberate Exclusions

| Exclusion | Rationale |
|-----------|-----------|
| **No semantic knowledge** | The lexer recognizes patterns, not meaning. Type resolution, name binding, and interpretation are downstream stages' responsibility. |
| **No streaming/incremental** | The full source is lexed in one pass. At the 64KB ceiling, the lex pass is sub-millisecond; incremental lexing would add complexity for no measurable benefit. |
| **No trivia attachment** | Whitespace is consumed silently; no trivia nodes are emitted. Precept has no formatting tools that need whitespace fidelity. |
| **No Unicode identifiers** | Identifiers are ASCII-only (`a-z`, `A-Z`, `0-9`, `_`). Precept is a DSL for domain modeling, not general-purpose programming; ASCII suffices for its identifier needs. |
| **No raw/verbatim strings** | All string literals terminate at newlines. Multi-line content is rare in Precept; if needed, concatenation or typed constants can be used. |
| **No nested block comments** | Comments are line-only (`#` to EOL). Block comments add parsing complexity for minimal benefit in a DSL context. |
| **No heredocs** | The language has no need for embedded multi-line text blocks. Typed constants (`'...'`) serve the role of domain-specific literals. |

---

## 15. Cross-References

| Topic | Document |
|-------|----------|
| Tokens catalog — metadata structure, category definitions, derived tables | `docs/language/catalog-system.md` |
| Parser — TokenStream consumption, dual-use token disambiguation | `docs/compiler/parser.md` |
| Diagnostics catalog — lex-phase error codes, message templates | `docs/compiler/diagnostic-system.md` |
| Literal system — string/typed constant segmentation, escape tables | `docs/compiler/literal-system.md` |
| Pipeline overview — stage ordering, artifact types | `docs/compiler/pipeline-overview.md` |
| Language spec — `set` disambiguation, interpolation syntax | `docs/language/precept-language-spec.md` §1.7, §1.8 |
| Type system — typed constant validation, numeric interpretation | `docs/language/type-system.md` |

---

## 16. Source Files

| File | Purpose |
|------|---------|
| `src/Precept/Pipeline/Lexer.cs` | Lexer implementation — `Lexer` static class, `Scanner` struct, `ModeState` struct, `LexerMode` enum (~687 lines) |
| `src/Precept/Pipeline/TokenStream.cs` | `TokenStream` record — immutable token sequence and diagnostics artifact |
| `src/Precept/Pipeline/SourceSpan.cs` | `SourceSpan` record struct — offset/length/line/column span type |
| `src/Precept/Language/TokenKind.cs` | `TokenKind` enum — all token kind values |
| `src/Precept/Language/Token.cs` | `Token` record struct — kind, text, span; `TokenMeta` record — catalog entry with tooling metadata; `TokenCategory` enum |
| `src/Precept/Language/Tokens.cs` | `Tokens` static class — `All` catalog property, `Keywords`/`TwoCharOperators`/`SingleCharOperators`/`TwoCharOperatorStarters`/`PunctuationChars` derived tables |
| `src/Precept/Language/Diagnostics.cs` | `Diagnostics.Create` — diagnostic message factory |
| `src/Precept/Language/DiagnosticCode.cs` | `DiagnosticCode` enum — includes all lex-stage codes |
