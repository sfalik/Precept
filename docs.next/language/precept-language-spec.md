# Precept Language Specification (v2)

> **Status:** Incremental — grows per compiler stage as design decisions are locked
> **Scope:** This document specifies the Precept DSL syntax and semantics for the v2 compiler pipeline. Each section is added when the corresponding compiler stage is designed and implemented.
> **Grounding:** `docs.next/language/precept-language-vision.md` (target language surface), `docs/PreceptLanguageDesign.md` (v1 implemented spec)
> **Clean room rule:** This spec references the v1 language grammar, keyword inventory, and disambiguation rules. It does NOT import v1 implementation details (Superpower combinators, token attributes, class hierarchies).

---

## 1. Lexer

The lexer transforms source text into a flat token stream. It produces `TokenStream` containing an `ImmutableArray<Token>` and an `ImmutableArray<Diagnostic>` for lexer-level errors (unterminated strings, unrecognized characters).

The lexer enforces a hard source-size ceiling of **65,536 characters (64 KB)** as a **security guardrail**. The limit exists to bound lexer work and memory usage on adversarial input; it is not a language expressiveness rule.

### 1.1 Token Vocabulary

Every token the lexer can produce. Organized by category to match the `TokenKind` enum.

#### Keywords: Declaration

| Token | Text | Context |
|-------|------|---------|
| `Precept` | `precept` | Precept header declaration |
| `Field` | `field` | Field declaration |
| `State` | `state` | State declaration |
| `Event` | `event` | Event declaration |
| `Rule` | `rule` | Named rule / invariant declaration |
| `Ensure` | `ensure` | State/event assertion keyword |
| `As` | `as` | Type annotation (`field X as number`) |
| `Default` | `default` | Default value modifier |
| `Optional` | `optional` | Field optionality modifier (v2) |
| `Because` | `because` | Reason clause |
| `Initial` | `initial` | Initial state marker |

#### Keywords: Prepositions

| Token | Text | Context |
|-------|------|---------|
| `In` | `in` | State-scoped ensure/edit (`in State ensure ...`) |
| `To` | `to` | Entry-gate ensure (`to State ensure ...`) |
| `From` | `from` | Exit-gate ensure or transition source (`from State ...`) |
| `On` | `on` | Event trigger (`on Event ensure ...`, `from State on Event ...`) |
| `Of` | `of` | Collection inner type (`set of string`) |
| `Into` | `into` | Dequeue/pop target (`dequeue Queue into Field`) |

#### Keywords: Control

| Token | Text | Context |
|-------|------|---------|
| `When` | `when` | Guard clause |
| `If` | `if` | Conditional expression |
| `Then` | `then` | Conditional expression |
| `Else` | `else` | Conditional expression |

#### Keywords: Actions

| Token | Text | Context |
|-------|------|---------|
| `Set` | `set` | Field assignment action / set collection type (dual-use) |
| `Add` | `add` | Set add action |
| `Remove` | `remove` | Set remove action |
| `Enqueue` | `enqueue` | Queue enqueue action |
| `Dequeue` | `dequeue` | Queue dequeue action |
| `Push` | `push` | Stack push action |
| `Pop` | `pop` | Stack pop action |
| `Clear` | `clear` | Collection clear action |

#### Keywords: Outcomes

| Token | Text | Context |
|-------|------|---------|
| `Transition` | `transition` | State transition outcome |
| `No` | `no` | Prefix for `no transition` |
| `Reject` | `reject` | Rejection outcome |

#### Keywords: Access Modes (v2)

| Token | Text | Context |
|-------|------|---------|
| `Write` | `write` | Field write access mode |
| `Read` | `read` | Field read access mode |
| `Omit` | `omit` | Field omit access mode |

#### Keywords: Logical Operators

| Token | Text | Context |
|-------|------|---------|
| `And` | `and` | Logical conjunction |
| `Or` | `or` | Logical disjunction |
| `Not` | `not` | Logical negation |

#### Keywords: Membership

| Token | Text | Context |
|-------|------|---------|
| `Contains` | `contains` | Collection membership test |
| `Is` | `is` | Multi-token operator prefix (`is set`, `is not set`) |

#### Keywords: Quantifiers / Modifiers

| Token | Text | Context |
|-------|------|---------|
| `All` | `all` | Universal quantifier / `edit all` |
| `Any` | `any` | State wildcard (`in any`, `from any`) |

#### Keywords: State Modifiers (v2)

| Token | Text | Context |
|-------|------|---------|
| `Terminal` | `terminal` | Structural: no outgoing transitions |
| `Required` | `required` | Structural: all initial→terminal paths visit this state (dominator) |
| `Irreversible` | `irreversible` | Structural: no path back to any ancestor state |
| `Success` | `success` | Semantic: marks a success outcome state |
| `Warning` | `warning` | Semantic: marks a warning outcome state |
| `Error` | `error` | Semantic: marks an error outcome state |

#### Keywords: Constraints

| Token | Text | Context |
|-------|------|---------|
| `Nonnegative` | `nonnegative` | Number/integer constraint: value >= 0 |
| `Positive` | `positive` | Number/integer constraint: value > 0 |
| `Nonzero` | `nonzero` | Number/integer constraint: value != 0 |
| `Notempty` | `notempty` | String constraint: non-empty |
| `Min` | `min` | Numeric minimum constraint / built-in function (dual-use) |
| `Max` | `max` | Numeric maximum constraint / built-in function (dual-use) |
| `Minlength` | `minlength` | String minimum length constraint |
| `Maxlength` | `maxlength` | String maximum length constraint |
| `Mincount` | `mincount` | Collection minimum count constraint |
| `Maxcount` | `maxcount` | Collection maximum count constraint |
| `Maxplaces` | `maxplaces` | Decimal maximum decimal places constraint |
| `Ordered` | `ordered` | Choice ordinal comparison constraint |

#### Keywords: Types

| Token | Text | Context |
|-------|------|---------|
| `StringType` | `string` | Scalar type |
| `BooleanType` | `boolean` | Scalar type |
| `IntegerType` | `integer` | Scalar type (v2: explicit integer, separate from number) |
| `DecimalType` | `decimal` | Scalar type (v2: exact base-10) |
| `NumberType` | `number` | Scalar type (general numeric) |
| `ChoiceType` | `choice` | Constrained string value set type |
| `SetType` | `set` | Set collection type (dual-use with action keyword) |
| `QueueType` | `queue` | Queue collection type |
| `StackType` | `stack` | Stack collection type |

#### Keywords: Temporal Types (v2)

| Token | Text | Context |
|-------|------|---------|
| `DateType` | `date` | Temporal: calendar date |
| `TimeType` | `time` | Temporal: time of day |
| `InstantType` | `instant` | Temporal: UTC point in time |
| `DurationType` | `duration` | Temporal: elapsed time quantity |
| `PeriodType` | `period` | Temporal: calendar quantity |
| `TimezoneType` | `timezone` | Temporal: timezone identity |
| `ZonedDateTimeType` | `zoneddatetime` | Temporal: date+time+timezone |
| `DateTimeType` | `datetime` | Temporal: local date+time |

#### Keywords: Business-Domain Types (v2)

| Token | Text | Context |
|-------|------|---------|
| `MoneyType` | `money` | Business: monetary amount |
| `CurrencyType` | `currency` | Business: currency identity |
| `QuantityType` | `quantity` | Business: measured quantity |
| `UnitOfMeasureType` | `unitofmeasure` | Business: unit identity |
| `DimensionType` | `dimension` | Business: dimension family identity |
| `PriceType` | `price` | Business: compound money/quantity rate |
| `ExchangeRateType` | `exchangerate` | Business: compound currency/currency rate |

#### Keywords: Literals

| Token | Text | Context |
|-------|------|---------|
| `True` | `true` | Boolean literal |
| `False` | `false` | Boolean literal |

#### Operators

| Token | Text | Precedence note |
|-------|------|-----------------|
| `DoubleEquals` | `==` | Comparison |
| `NotEquals` | `!=` | Comparison |
| `GreaterThanOrEqual` | `>=` | Comparison (scanned before `>`) |
| `LessThanOrEqual` | `<=` | Comparison (scanned before `<`) |
| `GreaterThan` | `>` | Comparison |
| `LessThan` | `<` | Comparison |
| `Assign` | `=` | Assignment (scanned after `==`) |
| `Plus` | `+` | Arithmetic |
| `Minus` | `-` | Arithmetic / unary negation |
| `Star` | `*` | Arithmetic |
| `Slash` | `/` | Arithmetic |
| `Percent` | `%` | Arithmetic (modulo) |
| `Arrow` | `->` | Action chain / outcome separator |

**Scan order for operators:** Multi-character operators must be attempted before their single-character prefixes: `->` before `-`, `==` before `=`, `!=` before `!` (if ever reintroduced), `>=` before `>`, `<=` before `<`.

#### Punctuation

| Token | Text |
|-------|------|
| `Dot` | `.` |
| `Comma` | `,` |
| `LeftParen` | `(` |
| `RightParen` | `)` |
| `LeftBracket` | `[` |
| `RightBracket` | `]` |

#### Literals

| Token | Produced when |
|-------|---------------|
| `NumberLiteral` | Digit sequence, optionally with one `.` followed by more digits |
| `StringLiteral` | `"..."` with no `{` interpolation (emitted as a single token) |
| `StringStart` | `"...{` — text before the first interpolation opening |
| `StringMiddle` | `}...{` — text between interpolation segments |
| `StringEnd` | `}..."` — text after the last interpolation closing |
| `TypedConstant` | `'...'` with no `{` interpolation (emitted as a single token) |
| `TypedConstantStart` | `'...{` — typed constant before first interpolation |
| `TypedConstantMiddle` | `}...{` — typed constant between interpolation segments |
| `TypedConstantEnd` | `}...'` — typed constant after last interpolation |

**`Token.Text` contract for quoted literals:** `Text` contains the semantic content — delimiters stripped, escape sequences resolved. `StringLiteral` for `"hello"` → `Text = "hello"`. `StringStart` for `"Hello {` → `Text = "Hello "`. `StringEnd` for `} world"` → `Text = " world"`. `TypedConstant` for `'2026-04-23'` → `Text = "2026-04-23"`. A zero-length segment (e.g. `"{Name}"` where nothing precedes the first `{`) produces an empty `Text` — the token is still emitted.

**`Token.Offset` and `Token.Length` contract for quoted literals:** `Offset` and `Length` span the full raw source range including opening and closing delimiters. `StringLiteral` for `"hello"` has `Length = 7` (both quotes included). `StringStart` for `"Hello {` spans through the `{`. This allows tools to highlight or replace the exact source text without having to re-infer delimiter positions. `Text` (content-only) and `Offset`/`Length` (raw-source span) are complementary.

See [§1.3 Literal Syntax](#13-literal-syntax) for full rules. See `docs.next/compiler/literal-system.md` for the complete literal system design.

#### Identifiers

| Token | Produced when |
|-------|---------------|
| `Identifier` | A word matching the identifier grammar that is not a reserved keyword |

```
Identifier  :=  Letter (Letter | Digit | '_')*
Letter      :=  [a-zA-Z]
Digit       :=  [0-9]
```

Identifiers are case-sensitive. Leading underscores are not permitted. Examples: `Balance`, `applicantName`, `Phase1`, `line_item_total`.

#### Structure

| Token | Produced when |
|-------|---------------|
| `Comment` | `#` through end of line |
| `NewLine` | Line terminator (LF, CRLF, or CR) |
| `EndOfSource` | Sentinel appended after all source text is consumed |

### 1.2 Reserved Keywords

Keywords are **strictly lowercase**. Identifiers are case-sensitive: `From` is a valid identifier, `from` is a reserved keyword.

The complete v2 reserved keyword set:

```
precept  field  as  default  optional  rule  because
state  initial  terminal  required  irreversible  event  ensure
success  warning  error
in  to  from  on  when  any  all  of
set  add  remove  enqueue  dequeue  push  pop  clear  into
transition  no  reject
write  read  omit
string  number  boolean  integer  decimal  choice  maxplaces  ordered
date  time  instant  duration  period  timezone  zoneddatetime  datetime
money  currency  quantity  unitofmeasure  dimension  price  exchangerate
true  false
and  or  not  contains  is
if  then  else
nonnegative  positive  nonzero  notempty
min  max  minlength  maxlength  mincount  maxcount
```

**v2 additions** (not in v1): `optional`, `write`, `read`, `omit`, `clear`, `nonzero`, `is`, `integer`, `decimal`, `choice`, `maxplaces`, `ordered`, `terminal`, `required`, `irreversible`, `success`, `warning`, `error`, `date`, `time`, `instant`, `duration`, `period`, `timezone`, `zoneddatetime`, `datetime`, `money`, `currency`, `quantity`, `unitofmeasure`, `dimension`, `price`, `exchangerate`.

**v1 removals:** `nullable`, `null`, and `edit` are not reserved in v2. `optional` replaces `nullable`. `write` replaces `edit`. The `null` literal is removed entirely — `optional` fields use `is set`/`is not set` for presence testing and `clear` for value removal. All three are ordinary identifiers in v2; no special parser recognition is needed.

### 1.3 Literal Syntax

The language has two quoted literal forms with distinct roles. Double-quoted strings (`"..."`) always produce `string`. Single-quoted typed constants (`'...'`) produce non-primitive values — the lexer treats their content opaquely and the type checker determines the specific type (see [§3.3 Context-Sensitive Type Resolution](#33-context-sensitive-type-resolution)). There is no constructor-call syntax for non-primitive types (e.g., no `date(...)` or `period(...)`) — typed constants are the sole mechanism for constructing non-primitive literal values. Both quoted forms support `{expr}` interpolation. Numeric and boolean literals remain bare tokens.

#### Numeric literals

A numeric literal is a sequence of decimal digits, optionally followed by a decimal part and/or an exponent part. No leading `+` or `-` (unary minus is a separate operator token). No underscores or grouping separators.

```
NumberLiteral  :=  Digits ('.' Digits)? (('e' | 'E') ('+' | '-')? Digits)?
Digits         :=  [0-9]+
```

Examples: `0`, `42`, `3.14`, `0.5`, `100.00`, `1.5e2`, `1e-5`, `3.0E+10`

The lexer produces a single `NumberLiteral` token for all numeric forms. The type checker determines the specific numeric type based on context (see [§3.3 Context-Sensitive Type Resolution](#33-context-sensitive-type-resolution)).

**Exponent notation and numeric types:** Exponent notation (`e`/`E`) is only valid for the `number` type. It is a type error to use exponent notation in a context that requires `integer` or `decimal` — `integer` is whole numbers only, and `decimal` is exact base-10 representation where exponent form would be semantically misleading. The lexer accepts all forms; the type checker enforces the restriction.

#### String literals (`"..."`)

String literals are delimited by double quotes. They always produce `string` type values.

**Without interpolation:** `"hello world"` → single `StringLiteral` token.

**With interpolation:** The lexer decomposes the string into segments at `{` and `}` boundaries:

```
"Hello {Name}, your balance is {Balance}"
```
→ `StringStart("Hello ")`, `Identifier(Name)`, `StringMiddle(", your balance is ")`, `Identifier(Balance)`, `StringEnd("")`

Interpolation is always-on — `{` inside a string always opens an interpolation expression. To include a literal `{`, escape it as `{{`. To include a literal `}`, escape it as `}}`.

**Empty interpolation:** `"{}"` is lexically valid. The lexer emits `StringStart("")`, then immediately sees `}` and emits `StringEnd("")` with no expression tokens between them. The parser rejects empty interpolation as a syntax error (expected expression). Zero-length `Text` on `StringStart`/`StringEnd` is normal and expected — the lexer always emits the boundary token even when the content is empty.

**Escape sequences in strings:** `\"` (double quote), `\\` (backslash), `\n` (newline), `\t` (tab), `{{` (literal brace), `}}` (literal brace).

#### Typed constants (`'...'`)

Typed constants are delimited by single quotes. The type is inferred from the content shape (date, time, duration, etc.) by the type checker — the lexer treats the content opaquely.

**Without interpolation:** `'2026-04-23'` → single `TypedConstant` token.

**With interpolation:** Same decomposition rules as strings, producing `TypedConstantStart`, `TypedConstantMiddle`, `TypedConstantEnd` tokens.

**Escape sequences:** `\'` (single quote), `\\` (backslash), `{{` (literal brace), `}}` (literal brace).

**Content words are not keywords.** Words that appear inside typed constant content — such as `days`, `hours`, `minutes`, `seconds`, `months`, `years`, `weeks`, `USD`, `kg` — are not language keywords. They are validated by the type checker against the inferred type family, not by the lexer. This keeps the reserved keyword set stable as new literal families are added.

#### List literals

List literals are delimited by `[` and `]` with comma-separated scalar values. The lexer produces individual punctuation and value tokens — list structure is assembled by the parser.

```
[1, 2, 3]       → LeftBracket, NumberLiteral, Comma, NumberLiteral, Comma, NumberLiteral, RightBracket
["a", "b"]       → LeftBracket, StringLiteral, Comma, StringLiteral, RightBracket
[]               → LeftBracket, RightBracket
```

### 1.4 Comments and Whitespace

Precept source has no indentation significance. Indentation is purely cosmetic — the lexer treats leading spaces and tabs identically to inter-token whitespace.

**Comments:** `#` begins a comment that extends to the end of the line. Comments can appear standalone on a line or after code on the same line. The lexer emits a `Comment` token (the parser may discard or preserve it).

```
# This is a standalone comment
field Balance as number  # This is an inline comment
```

**Whitespace:** Spaces and tabs between tokens are consumed silently (no token emitted). They serve only as delimiters between keyword/identifier tokens.

**Newlines:** Line terminators (LF `\n`, CRLF `\r\n`, CR `\r`) produce `NewLine` tokens. The parser uses newlines to determine statement boundaries — Precept is a line-oriented language with continuation via `->` chains.

### 1.5 Operator and Punctuation Scanning

Operators and punctuation are scanned after attempting keyword/identifier matches. Multi-character operators are tried before their single-character prefixes to avoid false matches.

**Scan priority (highest first):**

1. `->` (Arrow)
2. `==` (DoubleEquals)
3. `!=` (NotEquals)
4. `>=` (GreaterThanOrEqual)
5. `<=` (LessThanOrEqual)
6. `=` (Assign)
7. `>` (GreaterThan)
8. `<` (LessThan)
9. `+`, `-`, `*`, `/`, `%` (Arithmetic)
10. `.`, `,`, `(`, `)`, `[`, `]` (Punctuation)

### 1.6 Dual-Use Token Disambiguation

Three tokens serve double duty. The lexer emits a single token kind for each; the parser disambiguates by syntactic context.

#### `set` — Collection Type and Action Keyword

| Preceding context | Interpretation | Example |
|-------------------|---------------|---------|
| After `as` or `of` (type position) | `SetType` | `field Tags as set of string` |
| After `->` (action position) | `Set` (action) | `-> set Balance = 0` |

A third use exists: `set` as an adjective in the presence operators `is set` / `is not set` (for `optional` fields). This is not a lexer disambiguation concern — the lexer emits separate `Is`, `Not`, and `Set` tokens, and the parser composes the multi-token operator.

**Lexer strategy (locked):** The lexer always emits `TokenKind.Set` for the word `set`. `TokenKind.SetType` is never produced by the lexer — it is a parser-synthesized token kind used in the AST to represent `set` in a type position. The `Tokens.Keywords` dictionary maps `"set"` to `TokenKind.Set` only. The parser reinterprets the `Set` token as `SetType` when the preceding token is `As` or `Of`.

#### `min` / `max` — Constraint Keyword and Built-in Function

| Following context | Interpretation | Example |
|-------------------|---------------|---------|
| Followed by a number literal (constraint zone) | Constraint | `field Score as number min 0 max 100` |
| Followed by `(` (expression context) | Function call | `set Amount = min(Requested, Available)` |

The `(` disambiguates: constraint keywords are never followed by `(`, function calls always are.

#### `in` / `of` — Preposition and Type Qualifier

`in` and `of` each serve two roles: routing/scoping prepositions and type-position qualifiers.

| Context | Role | Example |
|---------|------|---------|
| After a state name (`in Draft ensure ...`) | Routing preposition | `in Draft ensure Amount > 0` |
| After a domain type in a field declaration | Type qualifier | `field Amount as money in 'USD'` |
| After `set of`, `queue of`, `stack of` | Collection inner type | `field Tags as set of string` |
| After a domain type in a field declaration | Dimension family qualifier | `field Distance as quantity of 'length'` |

Type qualifiers narrow the value domain of the field — they are part of the type annotation, not a declaration modifier. `in '<unit>'` pins to a specific unit or currency basis. `of '<family>'` constrains to a dimension or component family. A field may use `in` or `of`, never both. The preceding token (always a type keyword or collection keyword) makes the type-qualifier role unambiguous at LL(1).

The lexer uses a mode stack to handle nested interpolation in string and typed-constant literals. This ensures `{expr}` inside a literal correctly lexes the expression tokens and then returns to the literal context.

**Modes:**

| Mode | Active when |
|------|-------------|
| `Normal` | Default mode — scanning keywords, identifiers, operators, literals |
| `String` | Inside a `"..."` literal, scanning text and looking for `{` or `"` |
| `TypedConstant` | Inside a `'...'` literal, scanning text and looking for `{` or `'` |
| `Interpolation` | Inside `{...}` within a literal, scanning expression tokens and looking for `}` |

**Transitions:**

| From | On | To | Emits |
|------|----|----|-------|
| Normal | `"` | String | (begins string scanning) |
| String | `{` | Interpolation (push) | `StringStart` or `StringMiddle` |
| String | `"` | Normal | `StringEnd` or `StringLiteral` |
| Normal | `'` | TypedConstant | (begins typed constant scanning) |
| TypedConstant | `{` | Interpolation (push) | `TypedConstantStart` or `TypedConstantMiddle` |
| TypedConstant | `'` | Normal | `TypedConstantEnd` or `TypedConstant` |
| Interpolation | `}` | (pop to String or TypedConstant) | (resumes enclosing literal scanning) |
| Interpolation | `"` | String (push) | (nested string inside interpolation) |
| Interpolation | `'` | TypedConstant (push) | (nested typed constant inside interpolation) |

Nesting is fully supported: a string interpolation expression can contain a typed constant, and vice versa. The mode stack has a maximum depth of **8**. If a push would exceed this limit, the lexer emits an `UnterminatedInterpolation` diagnostic and resumes using the recovery rule for unterminated interpolations. Realistic nesting depth is 3 or fewer; the limit exists to prevent unbounded stack growth on adversarial input.

### 1.8 Lexer Diagnostics

The lexer emits diagnostics for malformed input. These are collected alongside tokens in the `TokenStream`.

The `InputTooLarge` diagnostic is different from the other entries in this table: it is a security failure, not a syntax failure. Once the source crosses the 65,536-character ceiling, lexing aborts immediately and returns only the `EndOfSource` sentinel so downstream stages never process the hostile input.

| Condition | Diagnostic Code | Severity | Description |
|-----------|-----------------|----------|-------------|
| Input too large | `InputTooLarge` | Error | Source exceeds 65536 characters (64 KB), which is the lexer security limit; lexing is aborted and the token stream contains only the `EndOfSource` sentinel |
| Unterminated string literal | `UnterminatedStringLiteral` | Error | `"hello` with no closing `"` before end of line/source |
| Unterminated typed constant | `UnterminatedTypedConstant` | Error | `'2026-01-01` with no closing `'` before end of line/source |
| Unterminated interpolation | `UnterminatedInterpolation` | Error | `"hello {Name` with no closing `}` before end of line |
| Unrecognized character | `InvalidCharacter` | Error | Character that is not part of any valid token |
| Unrecognized string escape | `UnrecognizedStringEscape` | Error | `\X` inside `"..."` where X is not `"`, `\`, `n`, or `t` |
| Unrecognized typed constant escape | `UnrecognizedTypedConstantEscape` | Error | `\X` inside `'...'` where X is not `'` or `\` (note: `\n` and `\t` are also invalid here) |
| Unescaped `}` in literal | `UnescapedBraceInLiteral` | Error | A lone `}` inside a string or typed constant that was not doubled (`}}`) |

The lexer continues scanning after diagnostics to maximize token recovery for downstream error reporting.

#### Recovery rules

Each error condition has a defined recovery boundary:

| Condition | Recovery boundary | Rationale |
|-----------|-------------------|-----------|
| Unterminated string literal | Scan to end of current line; resume in `Normal` mode on the next line | Prevents the rest of the file from being consumed as string content |
| Unterminated typed constant | Scan to end of current line; resume in `Normal` mode on the next line | Same as above for `'...'` literals |
| Unterminated interpolation | Scan forward for a `}` at depth 0; if none found before end of current line, resume in the enclosing literal mode at the next line | Recovers the enclosing literal context where possible |
| Unrecognized character | Skip the single character; resume scanning at the next character | Minimal disruption — one bad character should not invalidate surrounding tokens |
| Unrecognized string escape | Skip the `\` and the following character; continue in `String` mode | Preserves surrounding content; the bad sequence is omitted from `Text` |
| Unrecognized typed constant escape | Skip the `\` and the following character; continue in `TypedConstant` mode | Same as above |
| Unescaped `}` in literal | Preserve the `}` as literal content in the token's `Text`; continue scanning | Recovers the character so surrounding content remains intact |

In all cases the invalid source span still produces a diagnostic with the correct `SourceRange`. Post-recovery tokens are emitted normally so the parser and downstream stages can report additional errors.

---

## 2. Parser

The parser transforms the flat `TokenStream` into a `SyntaxTree` — an abstract syntax tree representing the semantic structure of the precept definition. The parser is a hand-written recursive descent parser with a Pratt expression parser for operator precedence. It produces an AST (not a CST) — comments and whitespace are consumed silently.

```
TokenStream  →  Parser.Parse  →  SyntaxTree
```

The public surface is a static class `Parser` with a single method:

```csharp
public static SyntaxTree Parse(TokenStream tokens)
```

The parser always runs to end-of-source. On malformed input it emits diagnostics and inserts `IsMissing` nodes or skips to sync points, ensuring downstream stages receive a structurally coherent tree.

### 2.1 Expression Precedence

The expression parser uses Pratt parsing (top-down operator precedence). `ParseExpression(int minBp)` parses a complete expression, stopping when it encounters a token whose left-binding power is ≤ `minBp`.

| Precedence | Token(s) | Role | Associativity |
|:----------:|----------|------|:-------------:|
| 10 | `or` | logical disjunction | left |
| 20 | `and` | logical conjunction | left |
| 25 (prefix) | `not` | logical negation | right (prefix) |
| 30 | `==` `!=` `<` `>` `<=` `>=` | comparison | non-associative |
| 40 | `contains` | collection membership | left |
| 40 | `is` (`is set` / `is not set`) | presence test | left |
| 50 | `+` `-` (infix) | additive arithmetic | left |
| 60 | `*` `/` `%` | multiplicative arithmetic | left |
| 65 (prefix) | `-` (unary) | negation | right (prefix) |
| 80 | `.` | member access | left |
| 80 | `(` (postfix) | function/method call | left |

**Non-associative comparisons:** `A == B == C` is a parse error. The right-binding power of comparison operators is 31, which prevents chaining. The parser emits a `NonAssociativeComparison` diagnostic.

#### Null-denotation (atoms and prefix)

| Token | Production |
|-------|------------|
| `Identifier` | `IdentifierExpression` |
| `NumberLiteral` | `NumberLiteralExpression` |
| `True` / `False` | `BooleanLiteralExpression` |
| `StringLiteral` | `StringLiteralExpression` |
| `StringStart` | `InterpolatedStringExpression` (reassembly loop) |
| `TypedConstant` | `TypedConstantExpression` |
| `TypedConstantStart` | `InterpolatedTypedConstantExpression` (reassembly loop) |
| `LeftBracket` | `ListLiteralExpression` |
| `LeftParen` | `ParenthesizedExpression` |
| `Not` | `UnaryExpression(Not, ParseExpression(25))` |
| `Minus` | `UnaryExpression(Negate, ParseExpression(65))` |
| `If` | `ConditionalExpression` (`if` Expr `then` Expr `else` Expr) |
| _other_ | missing `IdentifierExpression` + diagnostic |

#### Left-denotation (infix and postfix)

| Token | Production |
|-------|------------|
| `Or` | `BinaryExpression(Or, ParseExpression(10))` |
| `And` | `BinaryExpression(And, ParseExpression(20))` |
| `==` `!=` `<` `>` `<=` `>=` | `BinaryExpression(op, ParseExpression(31))` |
| `Contains` | `ContainsExpression(left, ParseExpression(40))` |
| `Is` | `IsSetExpression` — consumes optional `Not`, then `Set` |
| `+` `-` (infix) | `BinaryExpression(op, ParseExpression(50))` |
| `*` `/` `%` | `BinaryExpression(op, ParseExpression(60))` |
| `.` (Dot) | `MemberAccessExpression(left, Identifier)` |
| `(` (LeftParen) | If `left` is `MemberAccessExpression` → `MethodCallExpression`; if `IdentifierExpression` → `CallExpression`; else → diagnostic |

### 2.2 Declaration Grammar

After the `precept <Name>` header, the parser enters a loop that dispatches on the current non-trivia token to select a declaration production.

#### Top-level dispatch

| Leading token | Production |
|---------------|-----------|
| `field` | `FieldDeclaration` |
| `state` | `StateDeclaration` |
| `event` | `EventDeclaration` |
| `rule` | `RuleDeclaration` |
| `write` | `AccessModeDeclaration` (root-level, no state scope) |
| `in` | `StateEnsureDeclaration` or `AccessModeDeclaration` |
| `to` | `StateEnsureDeclaration` or `StateActionDeclaration` |
| `from` | `TransitionRowDeclaration`, `StateEnsureDeclaration`, or `StateActionDeclaration` |
| `on` | `EventEnsureDeclaration` or `StatelessEventHookDeclaration` |
| `EndOfSource` | exit loop |
| _anything else_ | diagnostic + sync-point resync |

#### `field` declaration

```
field Identifier ("," Identifier)* as TypeRef FieldModifier* ("->" Expr)?
```

Multi-name shorthand: `field A, B, C as string` declares three fields of the same type. Modifiers appear before the computed expression arrow. The `->` introduces a computed expression.

#### `state` declaration

```
state StateEntry ("," StateEntry)*
StateEntry  :=  Identifier ("initial")? StateModifier*
StateModifier  :=  terminal | required | irreversible | success | warning | error
```

#### `event` declaration

```
event Identifier ("," Identifier)* ("(" ArgList ")")? ("initial")?
ArgList  :=  ArgDecl ("," ArgDecl)*
ArgDecl  :=  Identifier as TypeRef FieldModifier*
```

Event arguments use parenthesized syntax. The `initial` keyword follows the argument list.

#### `rule` declaration

```
rule BoolExpr ("when" BoolExpr)? because StringExpr
```

The optional `when` guard scopes the rule to states where the guard is true.

#### `in` / `to` / `from` dispatch

These preposition keywords parse a state target, then look ahead to select the production:

| Preposition | Following verb | Production |
|-------------|---------------|-----------|
| `in` | `ensure` | `StateEnsureDeclaration` (Anchor=In) |
| `in` | `write`/`read`/`omit` | `AccessModeDeclaration` |
| `to` | `ensure` | `StateEnsureDeclaration` (Anchor=To) |
| `to` | `->` | `StateActionDeclaration` (Anchor=To) |
| `from` | `on` | `TransitionRowDeclaration` |
| `from` | `ensure` | `StateEnsureDeclaration` (Anchor=From) |
| `from` | `->` | `StateActionDeclaration` (Anchor=From) |

All three support an optional `when` guard between the state target and the verb (except `from ... on`, where the guard is inside the transition row after the event name).

#### Transition row

```
from StateTarget on Identifier ("when" BoolExpr)?
("->" ActionStatement)*
"->" Outcome

ActionStatement  :=  set Identifier "=" Expr
                  |  add Identifier Expr
                  |  remove Identifier Expr
                  |  enqueue Identifier Expr
                  |  dequeue Identifier ("into" Identifier)?
                  |  push Identifier Expr
                  |  pop Identifier ("into" Identifier)?
                  |  clear Identifier

Outcome  :=  transition Identifier
          |  no transition
          |  reject StringExpr
```

Each action and the outcome are introduced by `->`. The parser loops consuming `->` followed by an action keyword, and breaks out when the token after `->` is an outcome keyword.

#### State/event ensure

```
(in|to|from) StateTarget ("when" BoolExpr)? ensure BoolExpr because StringExpr
on Identifier ("when" BoolExpr)? ensure BoolExpr because StringExpr
```

#### Stateless event hook

```
on Identifier
("->" ActionStatement)*
```

Event hooks without a `when`/`ensure` continuation are parsed as stateless event hooks with an arrow-prefixed action chain.

#### State action

```
(to|from) StateTarget
("->" ActionStatement)*
```

#### Access mode

```
(in StateTarget ("when" BoolExpr)?)? (write|read|omit) FieldTarget
write FieldTarget
```

Root-level `write` has no state scope. State-scoped access modes support `write`, `read`, and `omit` with a field target that is either `all` or a comma-separated list of field names.

### 2.3 Type References

```
TypeRef  :=  ScalarType TypeQualifier?
          |  CollectionType
          |  ChoiceType

ScalarType  :=  string | number | integer | decimal | boolean
             |  date | time | instant | duration | period
             |  timezone | zoneddatetime | datetime
             |  money | currency | quantity | unitofmeasure
             |  dimension | price | exchangerate

CollectionType  :=  (set | queue | stack) of ScalarType TypeQualifier?
ChoiceType      :=  choice "(" StringExpr ("," StringExpr)* ")"
TypeQualifier   :=  (in | of) Expr
```

Type qualifiers narrow the value domain: `in '<unit>'` pins to a specific unit or currency, `of '<family>'` constrains to a dimension family. A field may use `in` or `of`, not both.

**`set` disambiguation:** The lexer always emits `TokenKind.Set`. In `ParseTypeRef()`, when followed by `of`, the parser treats it as the collection type. Outside type position, `set` is the action keyword.

### 2.4 Field Modifiers

Field modifiers appear after the type reference and before any computed expression.

| Modifier | Syntax | Category |
|----------|--------|----------|
| `optional` | flag | Field is nullable; use `is set`/`is not set` for presence |
| `ordered` | flag | Choice field supports ordinal comparison |
| `nonnegative` | flag | Value ≥ 0 |
| `positive` | flag | Value > 0 |
| `nonzero` | flag | Value ≠ 0 |
| `notempty` | flag | String is non-empty |
| `default` _Expr_ | value | Default value |
| `min` _Expr_ | value | Minimum value |
| `max` _Expr_ | value | Maximum value |
| `minlength` _Expr_ | value | Minimum string length |
| `maxlength` _Expr_ | value | Maximum string length |
| `mincount` _Expr_ | value | Minimum collection count |
| `maxcount` _Expr_ | value | Maximum collection count |
| `maxplaces` _Expr_ | value | Maximum decimal places |

### 2.5 Interpolation Reassembly

The parser reassembles interpolated literals from the segmented token stream the lexer produced. Both `ParseInterpolatedString()` and `ParseInterpolatedTypedConstant()` use the same loop:

1. Consume `Start` token → `TextSegment`
2. `ParseExpression(0)` → `ExpressionSegment`
3. If `Middle` → `TextSegment`, go to step 2
4. If `End` → `TextSegment`, done

`ParseExpression(0)` terminates naturally at `StringMiddle`/`StringEnd`/`TypedConstantMiddle`/`TypedConstantEnd` because these token kinds have no binding power in the expression parser. This is the depth-unaware reassembly property — no depth tracking is needed.

### 2.6 Error Recovery

The parser uses two complementary mechanisms:

#### Missing-node insertion

When an expected token is absent, the parser emits a diagnostic and creates a synthetic token with `IsMissing = true` and a zero-length span at the current position. The resulting AST node is structurally complete. Used for: missing identifiers, missing keywords (`as`, `because`, `ensure`), missing expression atoms.

#### Sync-point resync

When the parser is structurally lost at the top level, it scans forward for a sync token:

| Sync token | Keyword |
|------------|---------|
| `Precept` | `precept` |
| `Field` | `field` |
| `State` | `state` |
| `Event` | `event` |
| `Rule` | `rule` |
| `From` | `from` |
| `To` | `to` |
| `In` | `in` |
| `On` | `on` |

These are unambiguous top-level declaration starters. Continuation tokens (`when`, `->`, `set`, `transition`, `ensure`, `because`) are never sync points — they appear mid-production and would cause the parser to skip valid content.

### 2.7 Parser Diagnostics

| Condition | Diagnostic Code | Severity |
|-----------|-----------------|----------|
| Expected token not found | `ExpectedToken` | Error |
| Unrecognized keyword in declaration position | `UnexpectedKeyword` | Error |
| Chained comparison (`A == B == C`) | `NonAssociativeComparison` | Error |
| Non-callable expression followed by `(` | `InvalidCallTarget` | Error |

---

## 3. Type Checker

> **Status:** Stub — to be written when the type checker is designed and implemented.

### 3.1 Type Widening Rules

> Carried forward from v1: integer widens to number and decimal (lossless).

### 3.2 Diagnostic Catalog

> To be specified. See `docs.next/compiler/literal-system.md` for literal-system-specific type checker contracts.

### 3.3 Context-Sensitive Type Resolution

> **Status:** Stub — to be written when the type checker is designed and implemented.

Multiple literal forms produce tokens whose specific type cannot be determined at lex time. The type checker resolves these uniformly using expression context (field type, assignment target, binary operator peer, function argument position, constraint value position, default value position).

**Numeric literals.** A `NumberLiteral` token does not carry an inherent numeric lane. Context determines whether the value is `integer`, `decimal`, or `number`. Whole-number literals in integer context resolve as `integer`. Fractional literals resolve as `decimal` or `number` based on the target type. When no context is available, fractional literals default to `decimal` (the exact lane is the safer default for business-domain use). No literal suffix syntax exists — context is the sole resolution mechanism.

**Typed constants.** A `TypedConstant` token's content is opaque to the lexer. The type checker resolves the type in two steps: (1) content shape identifies a type family, (2) context narrows within that family. The admission rules are:

1. **Content shape determines a type family.** The content identifies which family of types it could belong to. Some shapes map to exactly one type (unambiguous); others match a multi-member family.
2. **Context narrows within that family.** The surrounding expression context selects the specific type.
3. **No content shape may belong to two unrelated families.** Each shape is claimed by at most one family.
4. **If context cannot narrow a multi-member family, compilation fails.** The type checker reports the ambiguity — it never guesses.

**Content shape examples:**

| Typed constant | Shape resolves to | Narrowing needed? |
|---|---|---|
| `'2026-06-01'` | `date` | No — unambiguous |
| `'14:30:00'` | `time` | No — unambiguous |
| `'2026-04-13T14:30:00Z'` | `instant` | No — unambiguous (Z suffix) |
| `'2026-04-13T09:00:00'` | `datetime` | No — unambiguous (no zone) |
| `'2026-04-13T14:30:00[America/New_York]'` | `zoneddatetime` | No — unambiguous (zone bracket) |
| `'America/New_York'` | `timezone` | No — unambiguous |
| `'30 days'` | `duration` or `period` | Yes — context selects |
| `'100 USD'` | `money` | No — unambiguous |
| `'5 kg'` | `quantity` | No — unambiguous |
| `'24.50 USD/kg'` | `price` | No — unambiguous (compound unit) |

---

## 4. Graph Analyzer

> **Status:** Stub — to be written when the graph analyzer is designed and implemented.

---

## 5. Proof Engine

> **Status:** Stub — to be written when the proof engine is designed and implemented.
