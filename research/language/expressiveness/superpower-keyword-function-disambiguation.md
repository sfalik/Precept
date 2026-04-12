# Superpower Keyword/Function Token Disambiguation Research

**Date:** 2026-04-12
**Author:** George (Runtime Dev)
**Requested by:** Shane
**Issue:** #16 — Built-in Functions
**Problem:** `min` and `max` are tokenized as constraint keywords but need to also work as function names in expression position.

---

## 1. Superpower Capabilities for Token Matching

Superpower 3.1.0 provides several mechanisms for matching tokens in the parser combinator layer:

### 1.1 `Token.EqualTo(kind)`

Matches a single token of exactly the specified kind. Returns the matched `Token<TKind>`:

```csharp
Token.EqualTo(PreceptToken.Min)  // matches a Min keyword token
```

### 1.2 `Token.EqualToValue(kind, value)`

Matches a token of a given kind AND whose underlying text span equals a specific string:

```csharp
// Source: datalust/superpower src/Superpower/Parsers/Token.cs:66-79
public static TokenListParser<TKind, Token<TKind>> EqualToValue<TKind>(TKind kind, string value)
{
    return EqualTo(kind).Where(t => t.Span.EqualsValue(value)).Named(Presentation.FormatLiteral(value));
}
```

Useful when a single token kind covers multiple text values (e.g. matching a specific identifier).

### 1.3 `Token.Matching(predicate, name)`

Matches a token whose kind satisfies an arbitrary predicate:

```csharp
// Source: datalust/superpower src/Superpower/Parsers/Token.cs:94-109
public static TokenListParser<TKind, Token<TKind>> Matching<TKind>(Func<TKind, bool> predicate, string name)
```

This enables matching *any of several* token kinds in a single combinator:

```csharp
Token.Matching<PreceptToken>(k => k == PreceptToken.Identifier || k == PreceptToken.Min || k == PreceptToken.Max, "function name")
```

### 1.4 `.Or()` chaining

Standard alternative combinator. Multiple `Token.EqualTo()` calls chained:

```csharp
Token.EqualTo(PreceptToken.Identifier)
    .Try().Or(Token.EqualTo(PreceptToken.Min))
    .Try().Or(Token.EqualTo(PreceptToken.Max))
```

### 1.5 `.Try()` for backtracking

When `.Try()` wraps a parser, a failed match resets the input position. **This is the standard Superpower mechanism for disambiguation** — try the more specific parse first, backtrack on failure. No allocation overhead when it succeeds:

```csharp
// Source: datalust/superpower src/Superpower/Combinators.cs:968-990
public static TokenListParser<TKind, T> Try<TKind, T>(this TokenListParser<TKind, T> parser)
{
    return input =>
    {
        var rt = parser(input);
        if (rt.HasValue)
            return rt;
        rt.Backtrack = true;
        return rt;
    };
}
```

### 1.6 `Parse.Not()` for negative lookahead

Tests that a parser does NOT match, without consuming input. Used by Serilog to distinguish property references from function calls:

```csharp
// Serilog pattern: "this is NOT a function call, so it's a property"
from notFunction in Parse.Not(Token.EqualTo(Identifier).IgnoreThen(Token.EqualTo(LParen)))
from p in Token.EqualTo(Identifier).Select(...)
select p
```

### 1.7 `TokenizerBuilder.requireDelimiters`

The `requireDelimiters: true` flag ensures keywords like `min` don't match inside longer identifiers like `minimum`. Precept already uses this for all keyword registrations. **This is tokenizer-level only** — it doesn't help with parser-level disambiguation.

### 1.8 Key Limitation

Superpower's `TokenizerBuilder` assigns **one token kind per matched text**. When the tokenizer sees `min`, it produces `PreceptToken.Min` — always. There is no built-in mechanism for context-sensitive tokenization in `TokenizerBuilder`. Disambiguation between keyword and function usage **must happen at the parser level**.

---

## 2. How Existing Projects Solve It

### 2.1 Serilog Expressions — The Gold Standard

Serilog Expressions (built by the same author as Superpower) uses a **hand-written tokenizer** that classifies all alphabetic words then resolves them in the parser.

**Tokenizer strategy:** All letter-sequences are checked against a keyword list. Matches become keyword tokens; non-matches become `Identifier`:

```csharp
// Source: serilog/serilog-expressions ExpressionTokenizer.cs:21-42
readonly ExpressionKeyword[] _keywords =
[
    new("and", ExpressionToken.And),
    new("in", ExpressionToken.In),
    new("not", ExpressionToken.Not),
    new("or", ExpressionToken.Or),
    new("true", ExpressionToken.True),
    new("false", ExpressionToken.False),
    new("null", ExpressionToken.Null),
    new("if", ExpressionToken.If),
    new("then", ExpressionToken.Then),
    new("else", ExpressionToken.Else),
    // ...
];
```

**Critical design choice:** Serilog **does not tokenize function names as keywords**. All function names tokenize as `Identifier`:

```csharp
// Source: serilog/serilog-expressions ExpressionTokenParsers.cs:83-89
static readonly TokenListParser<ExpressionToken, Expression> Function =
    (from name in Token.EqualTo(ExpressionToken.Identifier)
        from lparen in Token.EqualTo(ExpressionToken.LParen)
        from expr in Parse.Ref(() => Expr!).ManyDelimitedBy(Token.EqualTo(ExpressionToken.Comma))
        from rparen in Token.EqualTo(ExpressionToken.RParen)
        from ci in Token.EqualTo(ExpressionToken.CI).Value(true).OptionalOrDefault()
        select (Expression)new CallExpression(ci, name.ToStringValue(), expr)).Named("function");
```

**How property vs. function is distinguished:** Serilog uses `Parse.Not()` as a negative lookahead to ensure that `identifier(` is NOT matched as a property reference:

```csharp
// Source: serilog/serilog-expressions ExpressionTokenParsers.cs:136-140
static readonly TokenListParser<ExpressionToken, Expression> RootProperty =
    (from notFunction in Parse.Not(Token.EqualTo(ExpressionToken.Identifier)
                                       .IgnoreThen(Token.EqualTo(ExpressionToken.LParen)))
        from p in Token.EqualTo(ExpressionToken.Identifier).Select(t => ...)
        select p).Named("property");
```

**Serilog avoids the collision entirely** because function names like `Contains`, `Substring`, `Length` are never keywords — they're just identifiers followed by `(`.

### 2.2 Superpower's JSON Sample

The JSON parser sample tokenizes `true`, `false`, and `null` as a generic `Identifier` token, then distinguishes them at the parser level via `Token.EqualToValue()`:

```csharp
// Source: datalust/superpower sample/JsonParser/Program.cs:62-68
// "it's useful for the tokenizer to be very permissive - it's more
// informative to generate an error later at the parsing stage"
Identifier,
```

This reinforces the Superpower idiom: **tokenize broadly, disambiguate narrowly in the parser**.

### 2.3 `requireDelimiters` Test

Superpower's own test suite demonstrates that `requireDelimiters: true` handles prefix/suffix overlap at the tokenizer level:

```csharp
// Source: datalust/superpower test/Superpower.Tests/Tokenizers/TokenizerBuilderTests.cs:27-41
var tokenizer = new TokenizerBuilder<bool>()
    .Ignore(Span.WhiteSpace)
    .Match(Span.EqualTo("is"), true, requireDelimiters: true)
    .Match(Character.Letter.AtLeastOnce(), false, requireDelimiters: true)
    .Build();

// "is isnot is notis ins not is" → 7 tokens, 3 of which are `true` (keyword)
```

But this only solves "is" vs "isnot" — it doesn't help when the *same word* appears in two grammatical roles.

---

## 3. How Precept Already Handles `round`

This is the **key finding** of the research.

### `round` is NOT a keyword token

Despite being a built-in function, `round` does **not** appear in the `PreceptToken` enum with a `[TokenSymbol]` attribute. It tokenizes as `PreceptToken.Identifier`. The parser matches it by checking the text value:

```csharp
// Source: PreceptParser.cs:315-332
// round(expr, N) — built-in rounding function; recognized by identifier name, not a keyword
private static readonly TokenListParser<PreceptToken, PreceptExpression> RoundAtom =
    (from kw in Token.EqualTo(PreceptToken.Identifier).Where(t => t.ToStringValue() == "round")
     from _lp in Token.EqualTo(PreceptToken.LeftParen)
     from val in Superpower.Parse.Ref(BoolExprRef)
     from _c in Token.EqualTo(PreceptToken.Comma)
     from n in Token.EqualTo(PreceptToken.NumberLiteral)
     from _rp in Token.EqualTo(PreceptToken.RightParen)
     let raw = n.ToNumericLiteralValue()
     let places = raw is long l ? (int)l : (int)(double)raw
     select (PreceptExpression)new PreceptRoundExpression(val, places))
    .Try();
```

**Why this works for `round`:** The word `round` is never used as a keyword in any other grammar position — it only appears in expression position as `round(...)`. So there's no collision.

### `min`/`max` are different

Unlike `round`, `min` and `max` ARE keyword tokens (`PreceptToken.Min`, `PreceptToken.Max`) because they serve a distinct grammatical role in constraint position:

```precept
field Score as number min 0 max 100
```

The tokenizer will always produce `PreceptToken.Min` for the text "min" — it cannot also produce `PreceptToken.Identifier`. This is the collision.

### The existing `AnyMemberToken` pattern

Precept **already handles** the `min`/`max` dual-role problem in one place — dotted member access for collection accessors like `Tags.min`, `Tags.max`:

```csharp
// Source: PreceptParser.cs:273-278
// Member tokens: identifiers plus 'min'/'max' which are keywords but also valid
// dotted member names (e.g. Tags.min, Tags.max on set fields).
private static readonly TokenListParser<PreceptToken, Token<PreceptToken>> AnyMemberToken =
    Token.EqualTo(PreceptToken.Identifier)
        .Try().Or(Token.EqualTo(PreceptToken.Min))
        .Try().Or(Token.EqualTo(PreceptToken.Max));
```

**This is exactly the same pattern we need for function names.** The parser explicitly accepts keyword tokens in a non-keyword position.

### The `Atom` precedence chain

The expression atom parser has a defined priority order:

```csharp
// Source: PreceptParser.cs:334-343
private static readonly TokenListParser<PreceptToken, PreceptExpression> Atom =
    NumberAtom
        .Try().Or(StringAtom)
        .Try().Or(TrueAtom)
        .Try().Or(FalseAtom)
        .Try().Or(NullAtom)
        .Try().Or(ParenExpr)
        .Try().Or(RoundAtom)       // ← function call, tried BEFORE identifier
        .Or(DottedIdentifier);      // ← catch-all identifier
```

`RoundAtom` appears **before** `DottedIdentifier` so the function-call pattern is attempted first. The `.Try()` ensures backtracking on failure.

---

## 4. Option Analysis

### Option A: Parser-Only Disambiguation (keyword tokens, match in parser)

**Approach:** Keep `min`/`max` as `PreceptToken.Min`/`PreceptToken.Max` keyword tokens. Create function-call combinators that match these keyword tokens followed by `(`:

```csharp
private static readonly TokenListParser<PreceptToken, PreceptExpression> MinFunctionAtom =
    (from kw in Token.EqualTo(PreceptToken.Min)
     from _lp in Token.EqualTo(PreceptToken.LeftParen)
     from args in Superpower.Parse.Ref(BoolExprRef)
         .AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma))
     from _rp in Token.EqualTo(PreceptToken.RightParen)
     select (PreceptExpression)new PreceptFunctionCallExpression("min", args))
    .Try();
```

**Pros:**
- Zero tokenizer changes — lowest risk of breaking existing behavior
- Constraint parsing (`min 0`) is unaffected because it matches `Min` followed by `NumberLiteral`, not `(`
- Proven in Precept: `AnyMemberToken` already successfully matches keyword tokens in non-keyword positions

**Cons:**
- Each keyword-that-is-also-a-function needs a separate function-call combinator (or a shared `AnyFunctionName` combinator — see Option B)
- Adding future functions that collide with keywords requires manually adding them to the combinator

**Risk:** Low. The constraint parser matches `min <NumberLiteral>` and the function parser matches `min(`. These are unambiguous with one token of lookahead.

**Superpower idiomaticness:** Good. `.Try().Or()` chains are the standard Superpower pattern.

### Option B: Unified `AnyFunctionName` Token Set (recommended)

**Approach:** Keep `min`/`max` as keyword tokens. Create a single combinator that matches any token eligible to be a function name, then use it in one generic function-call atom:

```csharp
// Generalization of the existing AnyMemberToken pattern
static readonly TokenListParser<PreceptToken, string> AnyFunctionName =
    Token.EqualTo(PreceptToken.Identifier).Select(t => t.ToStringValue())
        .Try().Or(Token.EqualTo(PreceptToken.Min).Value("min"))
        .Try().Or(Token.EqualTo(PreceptToken.Max).Value("max"));
    // Future: .Try().Or(Token.EqualTo(PreceptToken.SomeKeyword).Value("name"))

// Single generic function-call atom replaces RoundAtom + individual function atoms
static readonly TokenListParser<PreceptToken, PreceptExpression> FunctionCallAtom =
    (from name in AnyFunctionName
     from _lp in Token.EqualTo(PreceptToken.LeftParen)
     from args in Superpower.Parse.Ref(BoolExprRef)
         .AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma))
     from _rp in Token.EqualTo(PreceptToken.RightParen)
     select (PreceptExpression)new PreceptFunctionCallExpression(name, args))
    .Try();
```

**Pros:**
- Zero tokenizer changes — same low risk as Option A
- One function-call combinator handles ALL functions (identifiers + keyword-name functions)
- Natural generalization of the existing `AnyMemberToken` pattern (lines 273-278)
- Extensible: adding a new keyword-that-is-also-a-function = one `.Or()` line
- `round` can be migrated into this same combinator (currently special-cased via `Identifier.Where`)
- Matches Serilog's approach (single `Function` parser) adapted for Precept's token collision

**Cons:**
- `AnyFunctionName` must be maintained when new keyword/function collisions arise — but this is a one-line change per collision
- Slightly less explicit than individual atoms — all function dispatch happens at evaluation time, not parse time

**Risk:** Low. Same disambiguation logic as Option A — the `(` lookahead separates function calls from constraint keywords.

**Superpower idiomaticness:** Excellent. This is the exact pattern `AnyMemberToken` already uses, and matches how Serilog structures its parser.

### Option C: Retokenize `min`/`max` as Identifiers

**Approach:** Remove `Min` and `Max` from keyword registration. They tokenize as `PreceptToken.Identifier`. The constraint parser matches them via `Token.EqualToValue(PreceptToken.Identifier, "min")`:

```csharp
// Constraint parsing would change from:
Token.EqualTo(PreceptToken.Min)
// To:
Token.EqualToValue(PreceptToken.Identifier, "min")
```

**Pros:**
- Cleanest conceptual model — `min`/`max` are always identifiers
- No `AnyFunctionName` combinator needed — everything is an identifier

**Cons:**
- **Breaking change to constraint parsing:** Every `Token.EqualTo(PreceptToken.Min)` and `Token.EqualTo(PreceptToken.Max)` call must be replaced (found in `ConstraintSuffix`, `AnyMemberToken`, and possibly tests)
- **Syntax highlighting regression:** `min`/`max` are currently highlighted as constraint keywords. Demoting them to identifiers degrades the editing experience — authors lose visual distinction between `field X as number min 0` (constraint keyword) and `min(A, B)` (function name)
- **Semantic token regression:** `min`/`max` carry `[TokenCategory(Constraint)]` which drives semantic token classification. Removing the token means losing this classification
- **MCP language tool regression:** `precept_language` reports constraint keywords from the token enum. Removing `Min`/`Max` tokens would drop them from the constraint keyword list
- **Error messages degrade:** Superpower reports "expected keyword `min`" for constraint errors. With identifiers, it would report "expected identifier" — less helpful

**Risk:** High. Touches tokenizer, parser, syntax highlighting, semantic tokens, MCP output, and error messages. Multiple integration test failures likely.

**Superpower idiomaticness:** Acceptable in isolation, but goes against the Superpower ethos of "more specific tokens for better error messages" (README: "unexpected identifier `frm`, expected keyword `from`").

---

## 5. Recommendation: Option B — Unified `AnyFunctionName` Token Set

**Option B is the right choice.** Here's why:

### It's the existing Precept pattern

Precept already does this. `AnyMemberToken` (line 273-278) exists specifically to accept `Min`/`Max` keyword tokens in non-keyword (dotted member) position. Option B applies the same pattern to function-call position. It's not novel — it's generalization.

### It matches how `round` already works, but better

`round` currently uses `Identifier.Where(t == "round")` — a name-check on a generic identifier token. This works because `round` isn't a keyword. For `min`/`max` (which ARE keywords), `AnyFunctionName` handles the same intent through token-kind matching rather than string comparison. Both patterns can coexist, and eventually `round` can be migrated into `AnyFunctionName` for consistency.

### Zero tokenizer risk

The tokenizer is unchanged. `min` and `max` remain keyword tokens. Constraint parsing (`min 0`, `max 100`) is completely unaffected. Dotted member access (`Tags.min`, `Tags.max`) via `AnyMemberToken` is unaffected. The only change is adding a new parser combinator in expression-atom position.

### Disambiguation is trivially unambiguous

| Pattern | What follows `min` | Parser that matches |
|---------|-------------------|-------------------|
| Constraint | `min 0` — number literal | `ConstraintSuffix` |
| Function call | `min(A, B)` — left paren | `FunctionCallAtom` |
| Dotted member | `Tags.min` — preceded by dot | `AnyMemberToken` |

One token of lookahead (`NumberLiteral` vs `LeftParen`) is sufficient. No deep backtracking needed.

### Implementation sketch

```csharp
// 1. AnyFunctionName combinator (alongside AnyMemberToken)
static readonly TokenListParser<PreceptToken, string> AnyFunctionName =
    Token.EqualTo(PreceptToken.Identifier).Select(t => t.ToStringValue())
        .Try().Or(Token.EqualTo(PreceptToken.Min).Value("min"))
        .Try().Or(Token.EqualTo(PreceptToken.Max).Value("max"));

// 2. Generic function-call atom
static readonly TokenListParser<PreceptToken, PreceptExpression> FunctionCallAtom =
    (from name in AnyFunctionName
     from _lp in Token.EqualTo(PreceptToken.LeftParen)
     from args in Superpower.Parse.Ref(BoolExprRef)
         .AtLeastOnceDelimitedBy(Token.EqualTo(PreceptToken.Comma))
     from _rp in Token.EqualTo(PreceptToken.RightParen)
     select (PreceptExpression)new PreceptFunctionCallExpression(name, args))
    .Try()
    .Register(/* construct catalog info */);

// 3. Update Atom to include FunctionCallAtom before DottedIdentifier
private static readonly TokenListParser<PreceptToken, PreceptExpression> Atom =
    NumberAtom
        .Try().Or(StringAtom)
        .Try().Or(TrueAtom)
        .Try().Or(FalseAtom)
        .Try().Or(NullAtom)
        .Try().Or(ParenExpr)
        .Try().Or(FunctionCallAtom)    // ← replaces/subsumes RoundAtom
        .Or(DottedIdentifier);
```

### Migration path for `round`

`round` can optionally be migrated into `FunctionCallAtom`:
- Add `"round"` to a validation set in the function evaluator
- Remove `RoundAtom` as a separate combinator
- `round(x, 2)` would parse as `FunctionCallExpression("round", [x, 2])` instead of `PreceptRoundExpression`

This is a separate decision, but the infrastructure supports it naturally.

---

## Summary

| Criterion | Option A | Option B | Option C |
|-----------|----------|----------|----------|
| Tokenizer changes | None | None | Significant |
| Parser changes | Per-function atoms | One generic atom | Constraint parser rewrite |
| Breaking risk | Low | Low | High |
| Matches existing pattern | Partial (AnyMemberToken) | Direct generalization | New pattern |
| Extensibility | Manual per-collision | One-line per collision | Automatic |
| Syntax highlighting | Preserved | Preserved | Degraded |
| Error messages | Preserved | Preserved | Degraded |
| Superpower idiom | Good | Excellent | Acceptable |

**Recommendation: Option B.** It generalizes Precept's existing `AnyMemberToken` pattern, requires zero tokenizer changes, subsumes the current `RoundAtom` special case, and scales cleanly to future keyword/function collisions.
