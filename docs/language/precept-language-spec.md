# Precept Language Specification (v2)

---

## Status

| Property | Value |
|---|---|
| Doc maturity | Incremental — grows as each compiler stage is designed and implemented |
| Implementation state | §1 Lexer complete; §2 Parser complete; §3 Type Checker complete; §4–5 stubs |
| Grounding | `docs/language/precept-language-vision.md` (target surface) · `docs/PreceptLanguageDesign.md` (v1 spec) |
| Clean room rule | References v1 grammar and keyword inventory; does not import v1 implementation details |

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
| `Writable` | `writable` | Field writable-baseline modifier — marks a non-computed field as directly editable by default across all states (v2) |
| `Because` | `because` | Reason clause |
| `Initial` | `initial` | Initial state marker |

#### Keywords: Prepositions

| Token | Text | Context |
|-------|------|---------|
| `In` | `in` | State-scoped scope preposition (`in State ensure ...`, `in State write|read|omit ...`) |
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
| `Write` | `write` | State-scoped write access mode (`in <State> write …`) and root-level `write all` sugar (stateless precepts) |
| `Read` | `read` | State-scoped read access mode (`in <State> read …`) |
| `Omit` | `omit` | State-scoped omit access mode (`in <State> omit …`) |

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
| `All` | `all` | Universal quantifier / `write all` (stateless precepts), `read all` / `omit all` (state-scoped) |
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
| `CaseInsensitiveEquals` | `~=` | Case-insensitive comparison (string-only) |
| `CaseInsensitiveNotEquals` | `!~` | Case-insensitive not-equals (string-only) |
| `Tilde` | `~` | Case-insensitive collection inner type prefix (`set of ~string`) |

**Scan order for operators:** Multi-character operators must be attempted before their single-character prefixes: `!~` before `!=` before `!` (if ever reintroduced), `~=` before `~`, `->` before `-`, `==` before `=`, `>=` before `>`, `<=` before `<`. A standalone `~` is only valid immediately before `string` in a collection inner type position — elsewhere it is a lexer error.

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

See [§1.3 Literal Syntax](#13-literal-syntax) for full rules. See `docs/compiler/literal-system.md` for the complete literal system design.

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
precept  field  as  default  optional  writable  rule  because
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

**v2 additions** (not in v1): `optional`, `writable`, `write`, `read`, `omit`, `clear`, `nonzero`, `is`, `integer`, `decimal`, `choice`, `maxplaces`, `ordered`, `terminal`, `required`, `irreversible`, `success`, `warning`, `error`, `date`, `time`, `instant`, `duration`, `period`, `timezone`, `zoneddatetime`, `datetime`, `money`, `currency`, `quantity`, `unitofmeasure`, `dimension`, `price`, `exchangerate`.

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

Typed constants are delimited by single quotes. The type is determined by expression context (not by content) — the lexer treats the content opaquely.

**Without interpolation:** `'2026-04-23'` → single `TypedConstant` token.

**With interpolation:** Same decomposition rules as strings, producing `TypedConstantStart`, `TypedConstantMiddle`, `TypedConstantEnd` tokens.

**Escape sequences:** `\'` (single quote), `\\` (backslash), `{{` (literal brace), `}}` (literal brace).

**Content words are not keywords.** Words that appear inside typed constant content — such as `days`, `hours`, `minutes`, `seconds`, `months`, `years`, `weeks`, `USD`, `kg` — are not language keywords. They are validated by the type checker against the context-determined type, not by the lexer. This keeps the reserved keyword set stable as new types are added.

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
| After `set of`, `queue of`, `stack of` | Collection inner type | `field Tags as set of string`, `field Labels as set of ~string` |
| After a domain type in a field declaration | Dimension family qualifier | `field Distance as quantity of 'length'` |

Type qualifiers narrow the value domain of the field — they are part of the type annotation, not a declaration modifier. `in '<unit>'` pins to a specific unit or currency. `of '<family>'` constrains to a dimension or component family. A field may use `in` or `of`, never both — with one exception: `price` allows `in` (currency-only) combined with `of` (denominator dimension), because price has two independent axes. When `in` specifies a compound `'currency/unit'` value, `of` is rejected. The preceding token (always a type keyword or collection keyword) makes the type-qualifier role unambiguous at LL(1).

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

In all cases the invalid source span still produces a diagnostic with the correct `SourceSpan`. Post-recovery tokens are emitted normally so the parser and downstream stages can report additional errors.

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

| Precedence | Token(s) | Role | Associativity |
|:----------:|----------|------|:-------------:|
| 10 | `or` | logical disjunction | left |
| 20 | `and` | logical conjunction | left |
| 25 (prefix) | `not` | logical negation | right (prefix) |
| 30 | `==` `!=` `~=` `!~` `<` `>` `<=` `>=` | comparison | non-associative |
| 40 | `contains` | collection membership | left |
| 40 | `is` (`is set` / `is not set`) | presence test | left |
| 50 | `+` `-` (infix) | additive arithmetic | left |
| 60 | `*` `/` `%` | multiplicative arithmetic | left |
| 65 (prefix) | `-` (unary) | negation | right (prefix) |
| 80 | `.` | member access | left |
| 80 | `(` (postfix) | function/method call | left |

**Non-associative comparisons:** `A == B == C` is a parse error. The parser detects when the left operand is already a comparison expression and emits a `NonAssociativeComparison` diagnostic. (The right-binding power of 31 prevents right-associativity; the explicit left-operand check prevents left-associative chaining.)

*Implementation note:* The expression parser uses Pratt parsing (top-down operator precedence). `ParseExpression(int minBp)` parses a complete expression, stopping when it encounters a token whose left-binding power is ≤ `minBp`.

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
| `==` `!=` `~=` `!~` `<` `>` `<=` `>=` | `BinaryExpression(op, ParseExpression(31))` |
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
| `write` | `AccessModeDeclaration` (root-level `write all` only — stateless precepts) |
| `in` | `StateEnsureDeclaration` or `AccessModeDeclaration` |
| `to` | `StateEnsureDeclaration` or `StateActionDeclaration` |
| `from` | `TransitionRowDeclaration`, `StateEnsureDeclaration`, or `StateActionDeclaration` |
| `on` | `EventEnsureDeclaration` or `EventHandlerDeclaration` |
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
| `in` | `ensure` | state ensure (scoped to `in`) |
| `in` | `write`/`read`/`omit` | access mode (state-scoped) |
| `to` | `ensure` | state ensure (scoped to `to`) |
| `to` | `->` | state action (entry hook) |
| `from` | `on` | transition row |
| `from` | `ensure` | state ensure (scoped to `from`) |
| `from` | `->` | state action (exit hook) |

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

Each action and the outcome are introduced by `->`. The `->` arrow is deliberately overloaded to create a visual pipeline that reads top-to-bottom: each step in a transition — guard, actions, outcome — flows through the same arrow. The parser loops consuming `->` followed by an action keyword, and breaks out when the token after `->` is an outcome keyword.

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
(to|from) StateTarget ("when" BoolExpr)?
("->" ActionStatement)*
```

State actions support an optional `when` guard between the state target and the action chain. The guard is passed through to the AST node.

#### Access mode

```
in StateTarget ("when" BoolExpr)? (write|read|omit) FieldTarget   ← state-scoped
write all                                                           ← root-level (stateless precepts only)
```

**Two-layer access mode composition model:**

- **Layer 1 — field-level baseline (`writable` modifier):** `writable` on a field declaration sets that field's baseline access mode to writable across all states. Fields without `writable` default to read-only.
- **Layer 2 — state-level override (`in <State> write|read|omit`):** State-scoped declarations override the field's baseline for a specific (field, state) pair only. State-level always wins over the field-level baseline.
- **Undeclared (field, state) pairs** use the field's baseline: `read` for fields without `writable`, `write` for fields with `writable`.

Root-level `write all` is valid for stateless precepts only — it is sugar for marking all non-computed fields writable with no state restriction. Root-level `write <FieldName>` (bare field list form) is **not valid syntax** — use the `writable` modifier on the field declaration instead. Root-level `read` and `omit` are not valid syntax: `read` is the default (declaring it globally would be redundant), and `omit` globally would make a field structurally absent in every state, rendering its declaration pointless.

State-scoped access modes (`in StateTarget`) support all three verbs. The field target is either `all` or a comma-separated list of field names.

**Composition rules:**
1. **Field baseline** — `writable` modifier on a field declaration sets the field's default to `write` across all states.
2. **D3 default** — fields without `writable` default to `read` for every (field, state) pair unless overridden by a state-scoped declaration.
3. **State-level override always wins** — an explicit `in <State> write|read|omit` declaration overrides the field's baseline for that (field, state) pair only.
4. **Guarded `write` is the only guarded access mode** — `read` and `omit` cannot have guards.
5. **`omit` clears on state entry** — field value resets to default on any transition into an `omit` state (including self-transitions); does NOT apply to `no transition`.
6. **`set` targeting an `omit` field in the target state** is a compile error; `read`/`write` do not restrict `set`.
7. **Conflicting modes** on the same (field, state) pair is a compile error.
8. **`writable` on a computed field** is a compile error (`ComputedFieldNotWritable`).
9. **`writable` on an event argument** is a compile error (`WritableOnEventArg`).

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
| `writable` | flag | Field baseline is directly editable across all states (unless overridden per-state); invalid on computed fields and event args |
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

`ParseExpression(0)` terminates naturally at `StringMiddle`/`StringEnd`/`TypedConstantMiddle`/`TypedConstantEnd` because these token kinds have no binding power in the expression parser. This is the depth-unaware reassembly property: because `}` always ends an interpolation hole and has no meaning in the expression grammar, the parser stops naturally without tracking nesting depth.

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

| Condition | Diagnostic Code | Severity | Description |
|-----------|-----------------|----------|-------------|
| Expected token not found | `ExpectedToken` | Error | "Expected {0} here, but found '{1}'" |
| Unrecognized keyword in declaration position | `UnexpectedKeyword` | Error | "'{0}' cannot appear inside a {1}" |
| Chained comparison (`A == B == C`) | `NonAssociativeComparison` | Error | "Comparisons like == and < cannot be chained — {0}" |
| Non-callable expression followed by `(` | `InvalidCallTarget` | Error | "Only built-in functions can be called this way — '{0}' is not a function name" |

---

## 3. Type Checker

The type checker transforms a `SyntaxTree` into a `SemanticIndex` — a flat collection of symbol tables, resolved declarations, and diagnostics. It always produces a result, even on broken input (the pipeline's resilient contract).

```
SyntaxTree  →  TypeChecker.Check  →  SemanticIndex
```

The public surface is a static method:

```csharp
public static SemanticIndex Check(SyntaxTree tree)
```

### 3.1 Processing Model

The type checker makes two passes over the declaration list:

1. **Registration pass.** Walk all declarations, build symbol tables: field names → types, state names, event names → argument types. No expression checking. This pass ensures all names are available regardless of declaration order.
2. **Checking pass.** Walk all declarations again, resolve expressions, validate types, emit diagnostics.

Declaration order does not matter. A rule at line 5 can reference a field declared at line 50. A transition row can reference states declared after it.

### 3.2 Type Widening Rules

Implicit widening is lossless and one-directional. Only two implicit widenings exist:

```
integer  →  decimal     (implicit — lossless)
integer  →  number      (implicit — lossless, direct)
```

- `integer` widens to `decimal` — every integer is exactly representable in base-10.
- `integer` widens to `number` — every integer within safe integer range is exactly representable as an IEEE 754 double. This is a direct widening, not transitive through `decimal`.
- `decimal` does **NOT** implicitly widen to `number` in any context. The conversion is lossy — IEEE 754 cannot exactly represent all base-10 values (`0.1 + 0.2 ≠ 0.3`). Use `approximate(decimalValue)` to explicitly convert `decimal → number`, or `round(numberValue, places)` to convert `number → decimal`. See §3.7 for bridge function signatures.
- No implicit narrowing. A `number` value cannot be assigned to an `integer` or `decimal` field without an explicit bridge function (`round`, `floor`, `ceil`, `truncate`).

**Widening applies in these contexts:**

| Context | Example |
|---------|---------|
| Assignment | `set IntegerField = ...` where the RHS is `integer` and the field is `decimal` |
| Binary operators | `IntegerField + DecimalField` — `integer` widens to `decimal`, result is `decimal` |
| Function arguments | `min(IntegerExpr, DecimalExpr)` — `integer` widens to `decimal` |
| Default values | `field X as decimal default 42` — `42` widens to `decimal` |
| Comparison | `IntegerField > DecimalField` — `integer` widens, comparison is valid |

For the complete conversion map — including the context-by-context matrix, bridge function catalog, and rationale for why comparisons also require bridging — see [Primitive Types · Numeric Lane Rules](primitive-types.md#numeric-lane-rules).

### 3.3 Context-Sensitive Type Resolution

Multiple literal forms produce tokens whose specific type cannot be determined at lex time. The type checker resolves these uniformly using expression context: field type, assignment target, binary operator peer, function argument position, constraint value position, default value position.

**Uniform rule:** If no context is available for any context-dependent literal, the type checker emits a diagnostic and assigns `ErrorType`. No implicit fallback.

#### Numeric literals

A `NumberLiteral` token does not carry an inherent numeric lane. Context determines the type.

| Literal form | Valid target types | Resolution rule |
|---|---|---|
| Whole number (`42`) | `integer`, `decimal`, `number` | Context determines. If target is `integer`, resolves as `integer`. If `decimal`, resolves as `decimal`. If `number`, resolves as `number`. If no context, diagnostic + `ErrorType`. |
| Fractional (`3.14`) | `decimal`, `number` | If target is `decimal`, resolves as `decimal`. If `number`, resolves as `number`. If no context, diagnostic + `ErrorType`. Type error if target is `integer`. |
| Exponent (`1.5e2`) | `number` only | Always `number`. Type error if target is `integer` or `decimal`. |

No literal suffix syntax exists — context is the sole resolution mechanism.

#### Typed constants

A `TypedConstant` token's content is opaque to the lexer. The type checker resolves the type using context-born resolution — the same model as numeric literals:

1. **Context determines the type.** The expression context (field type, operator peer, function signature, comparison operand) propagates an expected type inward. This is the same top-down inference that resolves `42` to `integer`, `decimal`, or `number`.
2. **Content is validated against the expected type.** The content is parsed and validated as a value of the context-determined type. Invalid content is a compile error.
3. **No context → compile error.** A typed constant in a position with no type expectation is a compile error.

**Content validation table** — given context-determined type, valid content patterns:

| Expected type | Valid content | Examples |
|---|---|---|
| `date` | `YYYY-MM-DD` | `'2026-04-15'` |
| `time` | `HH:MM:SS` or `HH:MM` | `'14:30:00'` |
| `instant` | ISO 8601 with `T`, trailing `Z` | `'2026-04-15T14:30:00Z'` |
| `datetime` | ISO 8601 with `T`, no zone | `'2026-04-15T14:30:00'` |
| `zoneddatetime` | ISO 8601 with `T`, `[Zone]` bracket | `'2026-04-15T14:30:00[America/New_York]'` |
| `timezone` | `Word/Word` IANA identifier | `'America/New_York'` |
| `duration` | `<integer> <temporal-unit>` (with optional `+`) | `'72 hours'` |
| `period` | `<integer> <temporal-unit>` (with optional `+`) | `'30 days'`, `'2 years + 6 months'` |
| `money` | `<number> <ISO-4217-code>` | `'100 USD'` |
| `quantity` | `<number> <unit-name>` | `'5 kg'` |
| `price` | `<number> <currency>/<unit>` | `'4.17 USD/each'` |
| `exchangerate` | `<number> <currency>/<currency>` | `'1.08 USD/EUR'` |
| `currency` | `<ISO-4217-code>` (3-letter) | `'USD'` |
| `unitofmeasure` | Unit name | `'kg'` |
| `dimension` | Dimension name (UCUM registry) | `'mass'` |

### 3.4 Name Resolution

All names are registered in the first pass. The checking pass validates every reference against the symbol tables.

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Duplicate field name | Two `field` declarations declare the same name | `DuplicateFieldName` |
| Duplicate state name | Two state entries have the same name | `DuplicateStateName` |
| Duplicate event name | Two `event` declarations share a name | `DuplicateEventName` |
| Duplicate event arg | Two args in the same event have the same name | `DuplicateArgName` |
| Undeclared field reference | `IdentifierExpression` in expression context does not match a field name (or in-scope event arg) | `UndeclaredField` |
| Undeclared state reference | State name in `from`/`to`/`in` target or `transition` outcome does not match a declared state | `UndeclaredState` |
| Undeclared event reference | Event name in `from ... on`, `on` ensure, or event handler does not match a declared event | `UndeclaredEvent` |
| Multiple initial states | More than one state entry has `initial` | `MultipleInitialStates` |
| No initial state | Stateful precept (has states) but none is marked `initial` | `NoInitialState` |

### 3.5 Scope Rules

Precept has a small, well-defined scope model. There are no nested scopes, no imports, no modules.

#### Global scope

Fields, states, and events are all declared at the top level. They are visible everywhere in the precept body. Order of declaration does not matter (see §3.1 — registration pass).

#### Expression scope

| Context | What's in scope |
|---------|----------------|
| Rule condition / guard | All field names |
| Ensure condition / guard | All field names |
| Transition row guard | All field names + current event's args (via `EventName.ArgName`) |
| Transition row actions (RHS of `set`, value of `add`/`enqueue`/`push`) | All field names + current event's args |
| State action guard / actions | All field names |
| Event handler actions | All field names + current event's args |
| Default value expression | Field names declared **before** this field (no self-reference, no forward reference) |
| Computed expression (`field X as T -> Expr`) | All field names except those that would form a dependency cycle (no self-reference, no mutual cycles) |
| Modifier value expressions (`min N`, `max N`, etc.) | Only literal values — no field references |

#### Event arg access

Event args are accessed via dotted notation: `EventName.ArgName`. The type checker resolves this by:

1. Checking if the object of a `MemberAccessExpression` is an `IdentifierExpression` that matches a declared event name.
2. If so, the member is resolved against the event's arg declarations.
3. Event arg access is only valid in contexts where an event is in scope (transition rows, event ensures, event handlers).

### 3.6 Expression Typing Rules

#### Binary operators

**Core scalar operators:**

| Operator | Left type | Right type | Result type | Widening? |
|----------|-----------|------------|-------------|-----------|
| `+` `-` `*` `/` `%` | numeric | numeric | common numeric type | Yes — widen to common |
| `+` | `string` | `string` | `string` | No (concatenation) |
| `==` `!=` | any T | same T | `boolean` | Yes — `integer` widens to `decimal` or `number`; `decimal` vs `number` is a type error (see §3.2) |
| `~=` `!~` | `string` | `string` | `boolean` | No — case-insensitive ordinal comparison (`OrdinalIgnoreCase`); type error on non-string operands |
| `<` `>` `<=` `>=` | numeric | numeric | `boolean` | Yes — `integer` widens to `decimal` or `number`; `decimal` vs `number` is a type error (see §3.2) |
| `<` `>` `<=` `>=` | `string` | `string` | `boolean` | No (lexicographic) |
| `<` `>` `<=` `>=` | `choice` (ordered) | `choice` (ordered, same set) | `boolean` | No (ordinal) |
| `and` `or` | `boolean` | `boolean` | `boolean` | No |

**Common numeric type:** When two numeric operands have different lanes, the result is the wider type: `integer op decimal` → `decimal`; `integer op number` → `number`. However, `decimal op number` is a **type error** — the author must use an explicit bridge function (`approximate(decimalValue)` to convert to `number`, or `round(numberValue, places)` to convert to `decimal`). There is no implicit `decimal → number` widening in any context — the conversion is lossy. See [Primitive Types · Numeric Lane Rules](primitive-types.md#numeric-lane-rules) for the complete conversion map and §3.7 for bridge function signatures.

**Temporal operators** — see the [temporal type system](temporal-type-system.md#semantic-rules) for the full per-type operator matrix. Summary:

| Left | Op | Right | Result | Notes |
|------|----|-------|--------|-------|
| `date` | `±` | `period of 'date'` | `date` | Unconstrained period → `UnqualifiedPeriodArithmetic`. |
| `date` | `-` | `date` | `period` | Calendar distance. |
| `date` | `+` | `time` | `datetime` | Composition. Commutative. |
| `time` | `±` | `period of 'time'` | `time` | Unconstrained period → `UnqualifiedPeriodArithmetic`. |
| `time` | `±` | `duration` | `time` | Sub-day bridging. Wraps at midnight. |
| `time` | `-` | `time` | `period` | |
| `instant` | `-` | `instant` | `duration` | |
| `instant` | `±` | `duration` | `instant` | |
| `datetime` | `±` | `period` | `datetime` | Accepts all period components. |
| `datetime` | `-` | `datetime` | `period` | |
| `duration` | `±` | `duration` | `duration` | |
| `duration` | `*` `/` | `integer` or `number` | `duration` | Scaling. `decimal` is a type error. Commutative for `*`. |
| `duration` | `/` | `duration` | `number` | Ratio. |
| `period` | `±` | `period` | `period` | |
| `zoneddatetime` | `±` | `duration` | `zoneddatetime` | Timeline arithmetic. |
| `zoneddatetime` | `-` | `zoneddatetime` | `duration` | |

**Temporal comparison:** `date`, `time`, `instant`, `duration`, `datetime` support all comparison operators. `period`, `timezone`, and `zoneddatetime` support only `==`/`!=` — ordering operators are type errors. Cross-type temporal comparison is always a type error.

**Business-domain operators** — see the [business-domain types](business-domain-types.md) for the full per-type operator matrix with cancellation rules. Summary:

| Left | Op | Right | Result | Notes |
|------|----|-------|--------|-------|
| `money` | `±` | `money` | `money` | Same currency required. |
| `money` | `*` `/` | `decimal` | `money` | Commutative for `*`. `number` is a type error. |
| `money` | `/` | `money` (same curr.) | `decimal` | Dimensionless ratio. |
| `money` | `/` | `money` (diff. curr.) | `exchangerate` | |
| `money` | `/` | `quantity` / `period` / `duration` | `price` | Price derivation. |
| `quantity` | `±` | `quantity` | `quantity` | Same dimension required. |
| `quantity` | `*` `/` | `decimal` | `quantity` | Commutative for `*`. |
| `quantity` | `/` | `quantity` (same dim.) | `decimal` | |
| `quantity` | `/` | `quantity` (diff. dim.) | `quantity` (compound) | |
| `price` | `*` | `quantity` / `period` / `duration` | `money` | Dimensional cancellation. Commutative. |
| `price` | `*` `/` | `decimal` | `price` | Commutative for `*`. |
| `price` | `±` | `price` | `price` | Same currency and unit required. |
| `exchangerate` | `*` | `money` | `money` | Currency conversion. Commutative. |
| `exchangerate` | `*` `/` | `decimal` | `exchangerate` | Commutative for `*`. |

**Business-domain comparison:** `money`, `quantity`, `price` support all comparison operators (same currency/dimension/unit required). `exchangerate`, `currency`, `unitofmeasure`, `dimension` support only `==`/`!=` — ordering operators are type errors.

#### Unary operators

| Operator | Operand type | Result type |
|----------|-------------|-------------|
| `not` | `boolean` | `boolean` |
| `-` (negate) | numeric | same numeric type |
| `-` (negate) | `duration` | `duration` |
| `-` (negate) | `money` | `money` (preserves currency) |
| `-` (negate) | `quantity` | `quantity` (preserves unit/dimension) |
| `-` (negate) | `price` | `price` (preserves currency/unit) |

#### `contains`

| Collection type | Value type | Result |
|-----------------|-----------|--------|
| `set of T` | `T` (or widens to `T`) | `boolean` |
| `queue of T` | `T` | `boolean` |
| `stack of T` | `T` | `boolean` |
| non-collection | — | type error |

#### `is set` / `is not set`

| Operand | Valid? | Result |
|---------|--------|--------|
| `optional` field | Yes | `boolean` |
| Non-optional field | Type error — field always has a value | — |

#### Conditional (`if ... then ... else ...`)

The `then` and `else` branches must have compatible types (same type, or one widens to the other). The result type is the common type.

#### Member access (`.`)

**Collection and core accessors:**

| Object type | Member | Result type |
|-------------|--------|-------------|
| `set of T` | `count` | `integer` |
| `set of T` (T orderable) | `min` | `T` |
| `set of T` (T orderable) | `max` | `T` |
| `queue of T` | `count` | `integer` |
| `queue of T` | `peek` | `T` |
| `stack of T` | `count` | `integer` |
| `stack of T` | `peek` | `T` |
| `string` | `length` | `integer` |
| Event arg reference (`EventName.ArgName`) | — | arg's declared type |

**Temporal accessors** — see the [temporal type system](temporal-type-system.md) for the full per-type accessor tables. Summary: `date` has `.year`, `.month`, `.day`, `.dayOfWeek` → `integer`. `time` has `.hour`, `.minute`, `.second` → `integer`. `instant` has only `.inZone(tz)` → `zoneddatetime` (no skip-level accessors). `duration` has `.totalDays`, `.totalHours`, `.totalMinutes`, `.totalSeconds` → `number`. `period` has `.years`, `.months`, `.weeks`, `.days`, `.hours`, `.minutes`, `.seconds` → `integer`; `.hasDateComponent`, `.hasTimeComponent` → `boolean`; `.basis` → `string`; `.dimension` → `dimension`. `zoneddatetime` has `.instant`, `.timezone`, `.datetime`, `.date`, `.time` and integer component accessors. `datetime` has `.date`, `.time`, `.inZone(tz)`, and integer component accessors.

**Business-domain accessors** — see the [business-domain types](business-domain-types.md#accessors-per-type) for the full accessor table. Summary: `money` has `.amount` → `decimal`, `.currency` → `currency`. `quantity` has `.amount` → `decimal`, `.unit` → `unitofmeasure`, `.dimension` → `dimension`. `price` has `.amount` → `decimal`, `.currency` → `currency`, `.unit` → `unitofmeasure`, `.dimension` → `dimension`. `exchangerate` has `.amount` → `decimal`, `.from`/`.to` → `currency`. `unitofmeasure` has `.dimension` → `dimension`. `period` also has `.basis` → `string` and `.dimension` → `dimension` for its `in`/`of` qualification system.

| _other_ | — | `InvalidMemberAccess` diagnostic |

#### Function calls

See §3.7 for the complete built-in function catalog.

#### Parenthesized expressions

Type is the type of the inner expression. Transparent.

#### String interpolation

Each `{expr}` inside `"..."` is type-checked independently. Any scalar type is coercible to string. Collections are a type error inside string interpolation.

#### Typed constant interpolation

Each `{expr}` inside `'...'` is type-checked independently. After interpolation expressions are typed, the full content is validated against the context-determined type as described in §3.3.

### 3.7 Built-in Function Catalog

Functions are validated against a closed catalog. There are no user-defined functions, no registration mechanism, no extension point.

| Function | Signature | Return type | Constraints |
|----------|-----------|-------------|-------------|
| `min(a, b)` | `(numeric, numeric) → numeric` | Common numeric type of args | — |
| `max(a, b)` | `(numeric, numeric) → numeric` | Common numeric type of args | — |
| `abs(value)` | `(numeric) → numeric` | Same numeric type as input | — |
| `clamp(value, lo, hi)` | `(numeric, numeric, numeric) → numeric` | Common numeric type | — |
| `floor(value)` | `(decimal\|number) → integer` | `integer` | — |
| `ceil(value)` | `(decimal\|number) → integer` | `integer` | — |
| `truncate(value)` | `(decimal\|number) → integer` | `integer` | — |
| `round(value)` | `(decimal\|number) → integer` | `integer` | Banker's rounding |
| `round(value, places)` | `(numeric, integer) → decimal` | `decimal` | `places` must be non-negative integer; **explicit bridge: number→decimal** |
| `approximate(value)` | `(decimal) → number` | `number` | **Explicit bridge: decimal→number**; makes precision loss visible |
| `pow(base, exp)` | `(numeric, integer) → numeric` | Same numeric type as `base` | `exp` must be non-negative for integer lane |
| `sqrt(value)` | `(numeric) → number` | `number` | Number-lane only; proof engine checks non-negativity |
| `trim(value)` | `(string) → string` | `string` | — |
| `startsWith(s, prefix)` | `(string, string) → boolean` | `boolean` | Case-sensitive prefix test |
| `endsWith(s, suffix)` | `(string, string) → boolean` | `boolean` | Case-sensitive suffix test |
| `toLower(s)` | `(string) → string` | `string` | Lowercase (invariant culture) |
| `toUpper(s)` | `(string) → string` | `string` | Uppercase (invariant culture) |
| `left(s, n)` | `(string, integer) → string` | `string` | Leftmost N code units (clamped to string length) |
| `right(s, n)` | `(string, integer) → string` | `string` | Rightmost N code units (clamped to string length) |
| `mid(s, start, length)` | `(string, integer, integer) → string` | `string` | 1-indexed substring (clamped); `start` and `length` must be positive `integer` |
| `now()` | `() → instant` | `instant` | — |

**Lane bridge functions.** Two functions are the sole explicit bridges between numeric lanes: `approximate(decimal) → number` and `round(value, places) → decimal`. The rounding family (`floor`, `ceil`, `truncate`, `round` with no places) provide `decimal|number → integer`. No other mechanism crosses lane boundaries — `decimal * NumberField` without `approximate()` is a type error (see type-checker.md §4.2a).

**Function validation checks:**

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Unknown function name | `foo(...)` where `foo` is not in the catalog | `UndeclaredFunction` |
| Wrong arity | `min(a)` or `min(a, b, c)` | `FunctionArityMismatch` |
| Arg type mismatch | `min("a", "b")` — strings to numeric function | `TypeMismatch` |
| Arg constraint violation | `round(x, -1)` — negative places | `FunctionArgConstraintViolation` |

### 3.8 Semantic Checks

#### Type compatibility

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Assignment type mismatch | `set Field = Expr` where `Expr`'s type is not assignable to `Field`'s type (after widening) | `TypeMismatch` |
| Guard not boolean | `when Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| Rule condition not boolean | `rule Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| Ensure condition not boolean | `ensure Expr` where `Expr`'s type is not `boolean` | `TypeMismatch` |
| Message not string | `because Expr` or `reject Expr` where `Expr` is not `string` | `TypeMismatch` |
| Binary operator type error | Operator applied to incompatible types (e.g., `string + boolean`) | `TypeMismatch` |
| Comparison on unordered choice | `<` / `>` / `<=` / `>=` on a `choice` field without the `ordered` modifier | `TypeMismatch` |
| Conditional branch mismatch | `if ... then A else B` where A and B have no common type | `TypeMismatch` |
| Default value type mismatch | `default Expr` where `Expr`'s type is incompatible with the field type | `TypeMismatch` |
| Collection element type mismatch | `add Field Expr` where `Expr`'s type doesn't match the collection's element type | `TypeMismatch` |
| Numeric literal incompatible | Fractional literal in `integer` context, or exponent literal in `integer`/`decimal` context | `TypeMismatch` |

#### Modifier validation

Modifiers are constraints on field/arg values. The type checker validates applicability:

| Modifier | Applicable to | Error when applied to |
|----------|---------------|----------------------|
| `writable` | any non-computed field type (field declarations only) | computed fields (`ComputedFieldNotWritable`); event arguments (`WritableOnEventArg`) |
| `nonnegative` | `integer`, `decimal`, `number` | `string`, `boolean`, `choice`, collections, temporal, domain |
| `positive` | `integer`, `decimal`, `number` | (same as above) |
| `nonzero` | `integer`, `decimal`, `number` | (same as above) |
| `notempty` | `string` | `number`, `integer`, `decimal`, `boolean`, `choice`, collections |
| `min` / `max` | `integer`, `decimal`, `number` | `string`, `boolean`, collections |
| `minlength` / `maxlength` | `string` | `number`, `integer`, `decimal`, `boolean`, collections |
| `mincount` / `maxcount` | `set`, `queue`, `stack` | scalars |
| `maxplaces` | `decimal` | `integer`, `number`, `string`, `boolean`, collections |
| `ordered` | `choice` | all non-choice types |
| `optional` | any field type | — (always valid) |

**Modifier value validation:**

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| `min` > `max` | `min` value exceeds `max` value on the same field | `InvalidModifierBounds` |
| `minlength` > `maxlength` | `minlength` exceeds `maxlength` | `InvalidModifierBounds` |
| `mincount` > `maxcount` | `mincount` exceeds `maxcount` | `InvalidModifierBounds` |
| Negative count/length/places | `minlength`/`maxlength`/`mincount`/`maxcount`/`maxplaces` is negative | `InvalidModifierValue` |
| `maxplaces` not integer | Decimal places must be a whole number | `InvalidModifierValue` |
| Duplicate modifier | Same modifier applied twice to one field | `DuplicateModifier` |
| Redundant modifier | `nonnegative` and `positive` on the same field (`positive` subsumes `nonnegative`) | `RedundantModifier` (warning) |

#### Action statement validation

| Action | Field type required | Value type required | Additional checks |
|--------|--------------------|--------------------|-------------------|
| `set F = Expr` | Any scalar | Assignable to field type | Field must not be computed |
| `add F Expr` | `set of T` | `T` | — |
| `remove F Expr` | `set of T` | `T` | — |
| `enqueue F Expr` | `queue of T` | `T` | — |
| `dequeue F (into G)?` | `queue of T` | — | If `into G`, `G` must be type `T`. Requires emptiness proof (`UnguardedCollectionMutation`) |
| `push F Expr` | `stack of T` | `T` | — |
| `pop F (into G)?` | `stack of T` | — | If `into G`, `G` must be type `T`. Requires emptiness proof (`UnguardedCollectionMutation`) |
| `clear F` | Any collection | — | — |

Type errors: applying a set operation to a non-set field, a queue operation to a non-queue field, etc.

#### Access mode validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Field not declared | Access mode names a field that doesn't exist | `UndeclaredField` |
| State not declared | Access mode scoped to a state that doesn't exist | `UndeclaredState` |
| Computed field in write mode | A computed field is listed in a `write` access mode declaration | `ComputedFieldNotWritable` |
| `writable` on computed field | A computed field carries the `writable` modifier | `ComputedFieldNotWritable` |
| `writable` on event arg | An event argument carries the `writable` modifier | `WritableOnEventArg` |
| Conflicting access modes | Same field has both `write` and `omit` in the same state | `ConflictingAccessModes` |

#### Computed field validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Self-reference | Computed expression references its own field | `CircularComputedField` |
| Transitive cycle | Computed fields form a dependency cycle (A→B→A, or A→B→C→A, etc.) | `CircularComputedField` |
| Expression type mismatch | Computed expression type doesn't match field type | `TypeMismatch` |
| Computed with default | Field has both `->` and `default` | `ComputedFieldWithDefault` |
| Computed as write target | `set` action targets a computed field | `ComputedFieldNotWritable` |

#### Choice type validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Duplicate choice value | `choice("a", "a")` | `DuplicateChoiceValue` |
| Empty choice | `choice()` — no values | `EmptyChoice` |
| Non-string choice value | `choice(42)` | `TypeMismatch` |

#### List literal validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Element type mismatch | List element type doesn't match collection element type | `TypeMismatch` |
| List in non-default position | List literal used outside a `default` clause | `ListLiteralOutsideDefault` |
| Empty list as default | Valid — empty collection | — |

#### Transition outcome validation

| Check | Fires when | Diagnostic |
|-------|-----------|------------|
| Undeclared target state | `transition StateName` where `StateName` is not declared | `UndeclaredState` |
| Reject message not string | `reject Expr` where `Expr` is not string | `TypeMismatch` |

#### Stateless/stateful cross-validation

A precept that contains both `EventHandlerDeclaration` nodes (`on Event -> actions`) and any `state` declarations is an error. In a stateful precept, event handlers are redundant with `from any on Event -> no transition` followed by rules. Mixing the two creates ambiguity about execution order.

A stateless precept (no states, no `from`, no transitions) that uses only event hooks is valid.

### 3.9 Error Recovery

#### ErrorType propagation

When the type checker encounters an unresolvable expression (missing node, undeclared name, type error in a sub-expression), it assigns `ErrorType`. `ErrorType` is compatible with every other type for the purpose of further checking — it suppresses all downstream type errors that would cascade from the original failure.

**Rules:**

1. Any operation involving `ErrorType` produces `ErrorType`.
2. `ErrorType` satisfies any type constraint — no further diagnostics are emitted for expressions that already carry `ErrorType`.
3. `ErrorType` never appears in a valid program. It only exists in the presence of other diagnostics.

#### Handling `IsMissing` AST nodes

| Node category | Recovery behavior |
|---|---|
| Declaration with `IsMissing` name | Skip — do not add to symbol table. Parser already emitted a diagnostic. |
| Expression with `IsMissing` | Assign `ErrorType`. No diagnostic emitted (parser already reported it). |
| TypeRef with `IsMissing` | Resolve to `ErrorType`. Fields with error types still appear in the symbol table but their type is `ErrorType`. |
| Guard with `IsMissing` subexpression | Guard is assigned `ErrorType`. The transition row is still processed — other checks continue. |
| Missing state/event name tokens | Skip the containing declaration. |

#### One diagnostic per root cause

The type checker emits diagnostics for root causes only. When `ErrorType` is flowing through an expression tree, the type checker stays silent. The first diagnostic emitted for a given expression chain is the root cause; all subsequent type mismatches involving `ErrorType` are symptoms.

### 3.10 Diagnostic Catalog

#### Existing codes (already in `DiagnosticCode.cs`)

| Code | Severity | Message template | Fires when |
|------|----------|------------------|------------|
| `UndeclaredField` | Error | "Field '{0}' is not declared" | Identifier in expression context doesn't match any field |
| `TypeMismatch` | Error | "Expected a {0} value here, but got '{1}'" | Type incompatibility in any expression context |
| `NullInNonNullableContext` | Error | "'{0}' requires a value and cannot be empty here" | Optional field used where value is required |
| `InvalidMemberAccess` | Error | "'.{0}' is not available on {1} fields" | Dot access on unsupported type |
| `FunctionArityMismatch` | Error | "'{0}' takes {1} inputs, but {2} were provided" | Wrong number of function arguments |
| `FunctionArgConstraintViolation` | Error | "Value {0} for '{1}' is not valid: {2}" | Function arg violates constraint |

#### New codes

| Code | Severity | Message template | Fires when |
|------|----------|------------------|------------|
| `DuplicateFieldName` | Error | "Field '{0}' is already declared" | Two field declarations with same name |
| `DuplicateStateName` | Error | "State '{0}' is already declared" | Duplicate state entry |
| `DuplicateEventName` | Error | "Event '{0}' is already declared" | Duplicate event declaration |
| `DuplicateArgName` | Error | "Argument '{0}' is already declared on event '{1}'" | Duplicate arg in same event |
| `UndeclaredState` | Error | "State '{0}' is not declared" | Reference to non-existent state |
| `UndeclaredEvent` | Error | "Event '{0}' is not declared" | Reference to non-existent event |
| `UndeclaredFunction` | Error | "'{0}' is not a recognized function" | Unknown function name in call |
| `MultipleInitialStates` | Error | "Only one state can be marked 'initial' — '{0}' and '{1}' both are" | Two or more initial states |
| `NoInitialState` | Error | "This precept has states but none is marked 'initial'" | Stateful precept without initial |
| `InvalidModifierForType` | Error | "The '{0}' constraint does not apply to {1} fields" | Modifier on inapplicable type |
| `InvalidModifierBounds` | Error | "{0} ({1}) cannot exceed {2} ({3})" | min > max, minlength > maxlength, etc. |
| `InvalidModifierValue` | Error | "The value for '{0}' must be {1}" | Negative count/length, non-integer maxplaces |
| `DuplicateModifier` | Error | "The '{0}' constraint is already applied to this field" | Same modifier twice |
| `RedundantModifier` | Warning | "'{0}' is unnecessary — '{1}' already implies it" | nonnegative + positive |
| `ComputedFieldNotWritable` | Error | "Field '{0}' is computed and cannot be assigned" | `set` targeting computed field, `write` access mode on computed field, or `writable` modifier on computed field |
| `ComputedFieldWithDefault` | Error | "Field '{0}' is computed and cannot have a default value" | Both `->` and `default` |
| `CircularComputedField` | Error | "Computed field '{0}' has a circular dependency: {1}" | Self-reference or transitive cycle in computed field dependency graph |
| `ConflictingAccessModes` | Error | "Field '{0}' has conflicting access modes in state '{1}'" | write + omit same field same state |
| `WritableOnEventArg` | Error | "The 'writable' modifier cannot appear on event argument '{0}'" | `writable` on an event arg declaration |
| `ListLiteralOutsideDefault` | Error | "List values can only appear in default clauses" | `[...]` outside default position |
| `DuplicateChoiceValue` | Error | "Choice value '{0}' is duplicated" | Repeated string in choice set |
| `EmptyChoice` | Error | "A choice type must have at least one value" | `choice()` with no args |
| `CollectionOperationOnScalar` | Error | "'{0}' is a {1} operation, but '{2}' is not a {1}" | add/remove on non-set, etc. |
| `ScalarOperationOnCollection` | Error | "'{0}' cannot be used with collection field '{1}'" | set = on collection field |
| `IsSetOnNonOptional` | Error | "'{0}' always has a value — 'is set' only works on optional fields" | is set / is not set on required field |
| `EventArgOutOfScope` | Error | "Event '{0}' arguments are not accessible here" | Event.Arg access outside transition/ensure/stateless event |
| `InvalidInterpolationCoercion` | Error | "A {0} value cannot appear inside a text interpolation" | Collection in `{...}` inside string |
| `UnresolvedTypedConstant` | Error | "Cannot determine the type of '{0}' — the content does not match any known value pattern" | Typed constant shape doesn't match any family |
| `AmbiguousTypedConstant` | Error | "'{0}' could be a {1} or {2} — add context to disambiguate" | Multi-member family, no context to narrow |
| `DefaultForwardReference` | Error | "Default value for '{0}' cannot reference '{1}', which is declared later" | Field default references later field |
| `EventHandlerInStatefulPrecept` | Error | "Event handlers cannot appear in a precept with state declarations" | `on Event ->` mixed with `state` declarations |

---

## 4. Graph Analyzer

> **Status:** Stub — to be written when the graph analyzer is designed and implemented.

---

## 5. Proof Engine

> **Status:** Stub — to be written when the proof engine is designed and implemented.

---

## Open Questions / Implementation Notes

_TBD — open questions will be captured here as later pipeline stages are designed._

---

## Cross-References

| Document | Relationship |
|---|---|
| [Compiler and Runtime Design](../compiler-and-runtime-design.md) | Pipeline architecture; stage contracts |
| [Catalog System](catalog-system.md) | Machine-readable language definition that feeds all pipeline stages |
| [Lexer](../compiler/lexer.md) | §1 implementation detail |
| [Parser](../compiler/parser.md) | §2 implementation detail |
| [Type Checker](../compiler/type-checker.md) | §3 implementation detail |
| [Graph Analyzer](../compiler/graph-analyzer.md) | §4 implementation detail |
| [Proof Engine](../compiler/proof-engine.md) | §5 implementation detail |
| [Language Vision](precept-language-vision.md) | Target language surface — this spec tracks what's implemented |
