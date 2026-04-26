# Lexer

> **Status:** Draft
> **Decisions answered:** L1 (hand-written vs. generated), L2 (static pure function), L3 (Scanner struct isolation), L4 (mode stack for interpolation), L5 (array-backed stack), L6 (ModeState segment origin fields), L7 (keyword lookup strategy), L8 (content buffer allocation), L9 (security size limit), L10 (token span contract), R1 (EOF cleanup), R2 (newline in Interpolation mode), R3 (no synthetic closing tokens), R4 (escape recovery), R5 (lone `}` in literal), D1 (diagnostic audience)
> **Survey references:** compiler-pipeline-architecture-survey
> **Grounding:** `docs/compiler-and-runtime-design.md` § Compiler Pipeline

## Overview

The lexer is the first stage of the Precept compiler pipeline. It transforms a raw source string into a `TokenStream` — an ordered, immutable sequence of `Token` values, each carrying kind, text content, source position, and byte span.

```
string source  →  Lexer.Lex  →  TokenStream
```

The lexer's job is pure character-to-token conversion. It has no knowledge of grammar, types, or semantics. Its output is consumed directly by `Parser.Parse`.

The lexer handles three families of input:

- **Primitive tokens** — identifiers, keywords, numeric literals, operators, punctuation, whitespace-adjacent tokens (newlines, comments)
- **String literals** — `"..."` with `\"`, `\\`, `\n`, `\t` escapes and `{expr}` interpolation
- **Typed constant literals** — `'...'` with `\'`, `\\` escapes and `{expr}` interpolation

Typed constant content is opaque to the lexer. The lexer marks the boundaries (`TypedConstant`, `TypedConstantStart`, `TypedConstantMiddle`, `TypedConstantEnd`) but does not interpret what's inside. Type resolution (context-born) and content validation happen in the type checker.

The `TokenStream` is the `CompilationResult.Tokens` field — it is part of the tooling surface and queryable by the language server for span-based operations.

---

## Design Principles

### Right-sized for Precept's scale

The lexer is hand-written. The keyword set is bounded and stable. A parser-generator (ANTLR, FsLex, Superpower) adds a dependency, a build step, a generator grammar to maintain, and indirection that costs more in complexity than it saves in effort. A hand-written lexer for this grammar is roughly 700 lines, is directly readable, and gives full control over diagnostic quality and recovery behavior. The architecture planning notes this explicitly: "A parser-generator adds complexity for little gain."

Rejected alternative: _ANTLR 4_ — surveyed and dismissed. The generated lexer would be larger, less readable, and would remove the ability to write per-code diagnostics with custom messages for the domain-author audience.

### Same input always produces same output

The lexer is a static class with a single public method: `Lexer.Lex(string source) → TokenStream`. No instance, no DI, no configuration, no substitution point. This matches the pipeline pattern used by all five stages (`Lexer.Lex`, `Parser.Parse`, `TypeChecker.Check`, `GraphAnalyzer.Analyze`, `ProofEngine.Prove`). Tests call the method directly and assert on the output — no mocking, no setup, no teardown.

### Never stop scanning

The lexer always runs to EOF, even after encountering errors. It accumulates all diagnostics in a single pass. This is the pipeline's Model A (resilient) contract: every stage produces a degraded-but-complete artifact on bad input. Short-circuiting on the first error would produce incomplete `TokenStream` artifacts and deny the parser (and LS) the token context they need to continue.

### Separation of concerns at the stage boundary

The lexer resolves nothing beyond tokenization:
- It does not resolve identifier types — that is the type checker's job
- It does not validate typed constant content — that is the type checker's job
- It does not manage state graph structure — that is the graph analyzer's job
- It does not synthesize `SetType` — `set` always emits as `TokenKind.Set`; the parser disambiguates contextually

---

## Architecture

### Static class + private Scanner struct

The public surface is a static class `Lexer` with a single method `Lex`. All mutable scanning state lives in a private `Scanner` struct instantiated inside `Lex()` and discarded after:

```csharp
public static TokenStream Lex(string source)
{
    var scanner = new Scanner(source);
    scanner.ScanAll();
    return scanner.Build();
}
```

This design keeps the public surface allocation-free — `Lexer` has no fields. The `Scanner` struct is stack-allocated (up to the runtime's threshold for struct-on-stack promotion) and carries all per-scan state. The only heap allocations per scan are the `ImmutableArray.Builder` instances and the final `TokenStream` output.

Rejected alternative: _instance class `Lexer`_ — adds a constructor, instance fields, and a lifecycle to manage with no benefit. No caller needs to configure or reuse a lexer instance.

### Mode stack for interpolation

Interpolation creates a nesting problem: a `{expr}` inside `"..."` or `'...'` re-enters Normal/Interpolation scanning, which may itself encounter a new `"..."` or `'...'`. Precept uses the same delimiters at every nesting level — there is no alternate-quote syntax.

The solution is a mode stack with four modes:

| Mode | Description |
|------|-------------|
| `Normal` | Top-level scanning — keywords, identifiers, operators, punctuation |
| `String` | Inside `"..."` — accumulates literal characters, handles `\"`, `{`, `}` |
| `TypedConstant` | Inside `'...'` — accumulates literal characters, handles `\'`, `{`, `}` |
| `Interpolation` | Inside `{...}` inside a literal — scans as Normal, ends at matching `}` |

Push/pop rules:
- Normal can push String (on `"`) or TypedConstant (on `'`)
- String or TypedConstant can push Interpolation (on `{`, after `{{` is ruled out)
- Interpolation can push String or TypedConstant (nested literals inside interpolated expressions)
- Interpolation pops on `}`; String pops on `"` or newline/EOF; TypedConstant pops on `'` or newline/EOF
- Normal is always at depth 0 and is never popped

This enables `"text {SomeField} more {'unit value'} end"` — a string interpolation that contains a typed constant — without requiring alternate delimiter syntax.

### Array-backed mode stack — no heap allocation

The mode stack is a fixed-size `ModeState[]` with a depth counter `_modeDepth`, not a `Stack<T>`. Maximum depth is `MaxModeStackDepth = 8`. The array is allocated once with the `Scanner` struct and reused for all push/pop operations.

`Stack<T>` would require a heap-allocated object per scan with internal array resizing. The fixed array approach avoids this entirely. A stack depth of 8 supports 4 nesting levels (string → interpolation → typed constant → interpolation), which is far beyond any realistic authoring scenario.

When the depth limit is reached, the lexer emits `UnterminatedInterpolation` and calls `RecoverFromUnterminatedInterpolation()` — it scans forward for `}` on the current line to resume in the enclosing literal mode, or pops the enclosing mode if no `}` is found before the line break. This avoids a double-diagnostic cascade at the depth boundary.

### `ModeState` struct — segment origin fields

Each stack entry is a `ModeState` struct:

```csharp
private struct ModeState
{
    public LexerMode Mode;
    public int SegmentIndex;
    public int SegStartOffset;
    public int SegStartLine;
    public int SegStartColumn;
}
```

The `SegStart*` fields record the source position of the delimiter that began the current segment:
- On the first segment: the position of the opening `"` or `'`
- On subsequent segments (after an interpolation closes): the position of the `}`

This is a **design review blocker that was fixed** (B1): the original implementation did not store delimiter position on the mode state. Spans on `StringStart`/`StringMiddle`/`TypedConstantStart`/`TypedConstantMiddle` tokens were computed from the wrong origin, producing incorrect span attribution for language server hover and diagnostics. Storing the segment start position explicitly on the `ModeState` fixes this structurally — the position is updated when a `}` closes an interpolation and the enclosing mode resumes.

`SegmentIndex` tracks which segment within the literal we are on (0 for the first segment, 1 for the segment after the first `{...}`, etc.). This determines whether to emit `StringLiteral` vs. `StringStart`/`StringMiddle`/`StringEnd` — the distinction the parser needs to reconstruct interpolated literal AST nodes.

### Keyword recognition — `FrozenDictionary` with span lookup

Keywords are stored in `Tokens.Keywords`, a `FrozenDictionary<string, TokenKind>` populated at startup. At scan time, after reading a word, keyword lookup uses:

```csharp
_keywordLookup = Tokens.Keywords.GetAlternateLookup<ReadOnlySpan<char>>();
// ...
var kind = _keywordLookup.TryGetValue(span, out var kw) ? kw : TokenKind.Identifier;
```

The `GetAlternateLookup<ReadOnlySpan<char>>()` method returns a handle that accepts a `ReadOnlySpan<char>` directly, avoiding the `new string(...)` allocation for every identifier candidate that turns out not to be a keyword. `FrozenDictionary` was specifically designed for read-only, high-frequency lookup — it uses a perfect hash at construction time.

`Tokens.Keywords` **explicitly excludes `SetType`** — `set` always maps to `TokenKind.Set` regardless of context. The parser determines from surrounding context whether `set` introduces a `set<T>` type annotation (inside a field declaration) or a `set` action keyword (inside an event body). Trying to resolve this ambiguity in the lexer would require look-ahead that belongs to the parser's job.

### Content buffer — reusable `char[]`

Quoted literal content is accumulated in `_contentBuffer`, a `char[]` field on the `Scanner` struct:

```csharp
private char[] _contentBuffer;
private int _contentLength;
```

Each segment begins with `ResetContent()` (sets `_contentLength = 0`). Characters are appended with `AppendContent(char c)`, which grows the buffer by doubling if needed. The token's `Text` field is produced by `new string(_contentBuffer.AsSpan(0, _contentLength))` only at the moment the token is emitted.

This avoids building intermediate strings or `StringBuilder` objects for every character during scanning. The buffer lives on the Scanner struct and is reused across all segments in the scan. The only string allocation per segment is the final `Text` value.

Rejected alternative: _`StringBuilder`_ — allocates a managed object per segment, requires clearing between segments, and produces allocations proportional to segment count rather than a single reused buffer.

---

## Security

### 65,536-character source length limit

```csharp
private const int MaxSourceLength = 65_536;
```

This is a **security guardrail**, not a language expressiveness rule. When source exceeds the limit, the lexer returns immediately:

```csharp
if (source.Length > MaxSourceLength)
{
    return new TokenStream(
        ImmutableArray.Create(new Token(TokenKind.EndOfSource, "", 1, 1, 0, 0)),
        ImmutableArray.Create(Diagnostics.Create(
            DiagnosticCode.InputTooLarge,
            new SourceSpan(0, 0, 1, 1, 1, 1))));
}
```

The returned `TokenStream` contains only `EndOfSource` and the `InputTooLarge` diagnostic. No partial tokenization is emitted. The pipeline continues with this degenerate stream — the parser will produce an empty tree, the type checker will find nothing to check, and the result is a clean `CompilationResult` with one diagnostic.

**Why 64KB:** A typical Precept file is under 200 lines (~5KB). 64KB is 10–13× the largest realistic file. Any input larger than 64KB is almost certainly adversarial — a fuzzing payload, a mistaken file attachment, or malicious input submitted through a network-exposed MCP tool endpoint. Bounding the work here bounds lexer time and memory usage without constraining legitimate use.

**Why not enforce this as a parse or type error:** Lexer-level enforcement prevents the lexer itself from being used as a denial-of-service vector. The check is the first thing `Lex()` does — no allocation, no scanning, immediate return.

---

## Error Recovery

The lexer uses a **continue-to-EOF** recovery model. Every error emits a diagnostic and advances state enough to continue scanning. No exception is thrown; no scanning is abandoned.

### EOF with open modes

When `ScanAll()` returns control to `Build()`, the method walks the mode stack from innermost to outermost (depths `_modeDepth - 1` down to 1), emitting one unterminated diagnostic per open mode:

| Open mode at EOF | Diagnostic code |
|------------------|----------------|
| `Interpolation` | `UnterminatedInterpolation` |
| `String` | `UnterminatedStringLiteral` |
| `TypedConstant` | `UnterminatedTypedConstant` |

The segment origin stored on each `ModeState` provides the span: from the delimiter that opened the segment to the current (EOF) position. Any accumulated content in the buffer has already been flushed as a token before `Build()` is called (the content scanners flush on newline/EOF).

### Newline in Interpolation mode

A newline inside an interpolation (`{...}` inside `"..."` or `'...'`) emits `UnterminatedInterpolation` and pops back to the enclosing literal mode, leaving the newline **unconsumed**:

```csharp
if ((c == '\n' || c == '\r') && CurrentMode == LexerMode.Interpolation)
{
    // emit UnterminatedInterpolation with span from { to here
    PopMode(); // back to String or TypedConstant — newline left unconsumed
    return;
}
```

On the next iteration, the enclosing literal mode's content scanner sees the newline and emits its own `UnterminatedStringLiteral` or `UnterminatedTypedConstant`. This produces two diagnostics for one logical problem — each is structurally precise about the mode that was unclosed. The parser receives clean, correctly attributed diagnostic information for both the interpolation and the containing literal.

### No synthetic closing tokens

When an interpolated literal is unterminated, the lexer does **not** emit synthetic `StringEnd` or `TypedConstantEnd` tokens. The parser receives `StringStart`/`StringMiddle` (or `TypedConstantStart`/`TypedConstantMiddle`) without a matching `End`. This was a deliberate team decision: synthetic closing tokens create a false structural impression that can mislead downstream error recovery. The parser is responsible for handling the missing `End` as a structural error in its own recovery logic.

### Escape recovery

Unrecognized escape sequences (`\X` where `X` is not a known escape character) emit a diagnostic and skip forward:

```csharp
if (c == '\\')
{
    // emit UnrecognizedStringEscape or UnrecognizedTypedConstantEscape
    Advance(); // skip \
    if (!IsAtEnd && Current != '\n' && Current != '\r')
        Advance(); // skip the unrecognized char — but not if it's a line break
    continue;
}
```

The guard `!= '\n' && != '\r'` is a **critical recovery detail**: if the char after `\` is a newline or EOF, the lexer must not double-advance past it. The newline is the trigger for the unterminated-literal diagnostic on the next character check. Consuming it here would skip the unterminated diagnostic entirely, producing a scan that appears to succeed while silently dropping the error.

Known string escapes: `\"`, `\\`, `\n`, `\t`.
Known typed constant escapes: `\'`, `\\`.
(`\n` and `\t` are valid in strings but not typed constants — typed constant content is positional and whitespace-significant to the content validator.)

### Lone `}` in literal

A `}` that is not part of `}}` (escaped brace) inside a string or typed constant emits `UnescapedBraceInLiteral`. The character is **preserved in the segment's `Text`**:

```csharp
_diagnostics.Add(Diagnostics.Create(DiagnosticCode.UnescapedBraceInLiteral, ...));
AppendContent(c); // preserve in segment text for recovery
Advance();
```

Dropping the character would alter the semantic content the author wrote, making the diagnostic harder to act on. Preserving it means the domain author sees their own text reflected back in error messages and previews.

---

## Diagnostic Audience

All lexer diagnostic messages are written for the **domain author** persona — a business analyst or domain expert who understands the business process being modeled, not a .NET developer. Messages use plain language that describes the problem and implies the fix.

Examples:

| Code | Message (domain-author voice) |
|------|-------------------------------|
| `UnterminatedStringLiteral` | `Text value opened with " is missing its closing quote` |
| `UnterminatedTypedConstant` | `Typed value opened with ' is missing its closing quote` |
| `UnterminatedInterpolation` | `Interpolated expression opened with { is missing its closing }` |
| `UnrecognizedStringEscape` | `\X is not a recognized escape sequence inside a text value. Use \" for a quote, \\\\ for a backslash, \\n for a newline, \\t for a tab, or {{ for a literal {` |
| `UnrecognizedTypedConstantEscape` | `\X is not a recognized escape sequence inside a typed value. Use \\' for a quote or \\\\ for a backslash` |
| `UnescapedBraceInLiteral` | `A lone } inside a text or typed value must be written as }}` |
| `InputTooLarge` | `Source file exceeds the maximum supported size` |

The `InvalidCharacter` case was not split into multiple codes for the top-level scanner, but the literal-interior codes (`UnrecognizedStringEscape`, `UnrecognizedTypedConstantEscape`, `UnescapedBraceInLiteral`) were explicitly separated because each represents a structurally distinct authoring mistake that calls for a different correction.

Diagnostic vocabulary is deliberately separate from compiler vocabulary. "Unterminated string literal" is meaningful to a compiler developer; "Text value opened with `"` is missing its closing quote" is meaningful to an author who has never written a compiler.

---

## Token Span Contract

`Token.Offset` + `Token.Length` spans the full source extent including delimiters:

| Token kind | Span coverage |
|------------|---------------|
| `StringLiteral` | Opening `"` through closing `"` (inclusive) |
| `StringStart` | Opening `"` through and including the `{` that started the interpolation |
| `StringMiddle` | The `}` that ended the previous interpolation through and including the `{` that started the next |
| `StringEnd` | The `}` that ended the last interpolation through closing `"` (inclusive) |
| `TypedConstant` | Opening `'` through closing `'` (inclusive) |
| `TypedConstantStart/Middle/End` | Same rules as String variants, with `'` delimiters |
| `NumberLiteral` | All numeric characters including decimal point and exponent |
| Keywords, identifiers | The word characters only |
| Operators, punctuation | The operator characters |
| `NewLine` | The `\n` or `\r\n` characters |
| `Comment` | From `#` through the last character before the line break |

The `Text` field carries the **decoded content** for quoted literals (escape sequences are resolved, delimiter characters are excluded from `Text` but included in the span). This means `Token.Length` is not `Token.Text.Length` for quoted literals. Consumers that need the original source characters for any reason should use `source.Substring(token.Offset, token.Length)`.

---

## Design Decisions

Decision rationales are discussed inline in the sections where they arise. This catalog provides a single auditable index.

| ID | Decision | Section |
|---|---|---|
| L1 | Hand-written lexer — no parser-generator dependency | Design Principles § Right-sized for Precept's scale |
| L2 | Static pure function — `Lexer.Lex(string) → TokenStream`, no instance | Design Principles § Same input always produces same output |
| L3 | `Scanner` struct isolation — all mutable state stack-allocated, discarded after `Lex()` | Architecture § Static class + private Scanner struct |
| L4 | Mode stack for interpolation — four modes (Normal, String, TypedConstant, Interpolation) with push/pop | Architecture § Mode stack for interpolation |
| L5 | Array-backed mode stack — fixed `ModeState[8]`, no `Stack<T>` heap allocation | Architecture § Array-backed mode stack |
| L6 | `ModeState` segment origin fields — stores delimiter position for correct span attribution | Architecture § `ModeState` struct |
| L7 | `FrozenDictionary` keyword lookup with `ReadOnlySpan<char>` — zero-allocation per-word check | Architecture § Keyword recognition |
| L8 | Reusable `char[]` content buffer — single buffer across all segments, no `StringBuilder` | Architecture § Content buffer |
| L9 | 64KB source length limit — security guardrail, immediate return before scanning | Security |
| L10 | Token span contract — `Offset + Length` spans full source extent including delimiters | Token Span Contract |
| R1 | EOF cleanup — walk mode stack innermost-to-outermost, one diagnostic per open mode | Error Recovery § EOF with open modes |
| R2 | Newline in Interpolation pops mode, leaves newline unconsumed for enclosing literal | Error Recovery § Newline in Interpolation mode |
| R3 | No synthetic closing tokens — missing `End` is a structural signal for the parser | Error Recovery § No synthetic closing tokens |
| R4 | Escape recovery — skip `\X`, guard against consuming line breaks | Error Recovery § Escape recovery |
| R5 | Lone `}` preserved in segment text with diagnostic — content not altered | Error Recovery § Lone `}` in literal |
| D1 | Diagnostic audience is the domain author — plain language, no compiler jargon | Diagnostic Audience |

---

## Deliberate Exclusions

The lexer intentionally does NOT:

- **Resolve types.** Typed constant content (`'30 days'`) is opaque — the lexer marks the boundaries but does not parse or validate the interior. Type resolution is the type checker's responsibility.
- **Validate typed constant content.** Words like `days`, `hours`, `USD` are not keywords — they are content. The lexer does not know the unit vocabulary; the type checker does.
- **Manage indentation.** Precept has no indentation significance. The lexer emits `NewLine` tokens and discards leading whitespace per-line. Indentation-sensitive parsing is not a design goal.
- **Synthesize `SetType`.** `set` always emits as `TokenKind.Set`. The parser determines from context whether it introduces a `set<T>` type annotation or a `set` action. This distinction requires knowing whether the `set` appears in a field declaration context or an event body — knowledge the parser has but the lexer does not.
- **Short-circuit on errors.** The lexer always scans to EOF to produce maximal diagnostics. Partial token streams are valid `TokenStream` values and do not cause downstream failures.
- **Preserve trivia.** Whitespace (spaces, tabs) is consumed silently — no trivia tokens are emitted. Comments are emitted as `Comment` tokens. This is a deliberate right-sizing: Precept has no formatting/refactoring requirements that need whitespace round-trip fidelity.

---

## Cross-References

| Topic | Document |
|-------|----------|
| String/typed constant segmentation, escape tables, mode stack modes | [docs/compiler/literal-system.md](../compiler/literal-system.md) § Pipeline Stage Contracts → Lexer |
| `TokenStream` shape and consumer contracts | [docs/compiler-and-runtime-design.md](../compiler-and-runtime-design.md) § Compiler Pipeline |
| Diagnostic codes, messages, and attribution | [docs/compiler/diagnostic-system.md](../compiler/diagnostic-system.md) |
| All lexer diagnostic codes | [docs/language/precept-language-spec.md](../language/precept-language-spec.md) § 1.8 |
| `set` disambiguation | [docs/language/precept-language-spec.md](../language/precept-language-spec.md) § 1.7 |
| Pipeline stage ordering, artifact types | [docs/compiler-and-runtime-design.md](../compiler-and-runtime-design.md) |

---

## Source Files

| File | Purpose |
|------|---------|
| `src/Precept.Next/Pipeline/Lexer.cs` | Lexer implementation — `Lexer` static class, `Scanner` struct, `ModeState` struct, `LexerMode` enum (~730 lines) |
| `src/Precept.Next/Pipeline/TokenKind.cs` | `TokenKind` enum — all token kind values |
| `src/Precept.Next/Pipeline/Token.cs` | `Token` record struct — kind, text, line, column, offset, length |
| `src/Precept.Next/Pipeline/Tokens.cs` | `Tokens.Keywords` — `FrozenDictionary<string, TokenKind>` keyword catalog |
| `src/Precept.Next/Pipeline/TokenStream.cs` | `TokenStream` — lexer output type (immutable tokens + diagnostics) |
| `src/Precept.Next/Pipeline/DiagnosticCode.cs` | `DiagnosticCode` enum — Lex-stage codes: `InputTooLarge`, `UnterminatedStringLiteral`, `UnterminatedTypedConstant`, `UnterminatedInterpolation`, `InvalidCharacter`, `UnrecognizedStringEscape`, `UnrecognizedTypedConstantEscape`, `UnescapedBraceInLiteral` |
| `src/Precept.Next/Pipeline/Diagnostics.cs` | `Diagnostics.Create` — diagnostic message factory (domain-author messages) |
