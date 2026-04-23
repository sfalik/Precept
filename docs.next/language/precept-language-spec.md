# Precept Language Specification (v2)

> **Status:** Incremental — grows per compiler stage as design decisions are locked
> **Scope:** This document specifies the Precept DSL syntax and semantics for the v2 compiler pipeline. Each section is added when the corresponding compiler stage is designed and implemented.
> **Grounding:** `docs.next/language/precept-language-vision.md` (target language surface), `docs/PreceptLanguageDesign.md` (v1 implemented spec)
> **Clean room rule:** This spec references the v1 language grammar, keyword inventory, and disambiguation rules. It does NOT import v1 implementation details (Superpower combinators, token attributes, class hierarchies).

---

## 1. Lexer

The lexer transforms source text into a flat token stream. It produces `TokenStream` containing an `ImmutableArray<Token>` and an `ImmutableArray<Diagnostic>` for lexer-level errors (unterminated strings, unrecognized characters).

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
| `With` | `with` | Event argument list (`event Submit with ...`) |
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
state  initial  terminal  required  irreversible  event  with  ensure
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

A numeric literal is a sequence of decimal digits, optionally containing one `.` followed by more digits. No leading `+` or `-` (unary minus is a separate operator token). No underscores or grouping separators. No exponent notation.

```
NumberLiteral  :=  Digits ('.' Digits)?
Digits         :=  [0-9]+
```

Examples: `0`, `42`, `3.14`, `0.5`, `100.00`

The lexer produces a single `NumberLiteral` token. The type checker determines the specific numeric type (integer, decimal, or number) based on context (see [§3.3 Context-Sensitive Type Resolution](#33-context-sensitive-type-resolution)).

#### String literals (`"..."`)

String literals are delimited by double quotes. They always produce `string` type values.

**Without interpolation:** `"hello world"` → single `StringLiteral` token.

**With interpolation:** The lexer decomposes the string into segments at `{` and `}` boundaries:

```
"Hello {Name}, your balance is {Balance}"
```
→ `StringStart("Hello ")`, `Identifier(Name)`, `StringMiddle(", your balance is ")`, `Identifier(Balance)`, `StringEnd("")`

Interpolation is always-on — `{` inside a string always opens an interpolation expression. To include a literal `{`, escape it as `{{`. To include a literal `}`, escape it as `}}`.

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

At the lexer level, `set` may produce either `Set` or `SetType` — or the lexer may produce a unified token and let the parser distinguish. The key requirement is that no ambiguity exists at LL(1): the preceding token always determines the meaning.

#### `min` / `max` — Constraint Keyword and Built-in Function

| Following context | Interpretation | Example |
|-------------------|---------------|---------|
| Followed by a number literal (constraint zone) | Constraint | `field Score as number min 0 max 100` |
| Followed by `(` (expression context) | Function call | `set Amount = min(Requested, Available)` |

The `(` disambiguates: constraint keywords are never followed by `(`, function calls always are.

### 1.7 Lexer Mode Stack (Interpolation)

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

Nesting is fully supported: a string interpolation expression can contain a typed constant, and vice versa. The mode stack depth is bounded by practical nesting limits.

### 1.8 Lexer Diagnostics

The lexer emits diagnostics for malformed input. These are collected alongside tokens in the `TokenStream`.

| Condition | Severity | Description |
|-----------|----------|-------------|
| Unterminated string literal | Error | `"hello` with no closing `"` before end of line/source |
| Unterminated typed constant | Error | `'2026-01-01` with no closing `'` before end of line/source |
| Unterminated interpolation | Error | `"hello {Name` with no closing `}` |
| Unrecognized character | Error | Character that is not part of any valid token |

The lexer continues scanning after diagnostics to maximize token recovery for downstream error reporting.

---

## 2. Parser

> **Status:** Stub — to be written when the parser is designed and implemented.

The parser transforms the flat token stream into an abstract syntax tree (AST). The v1 grammar in `docs/PreceptLanguageDesign.md` § Grammar is the baseline specification. The v2 parser will extend it with typed constants, interpolation reassembly, and any new declaration forms.

### 2.1 Expression Precedence (Locked)

Carried forward from v1. The expression grammar uses standard precedence:

```
or < and < not < comparison < contains < arithmetic < unary
```

Atoms: literals, identifiers, parenthesized expressions, function calls (`min(...)`, `max(...)`, `round(...)`, `clamp(...)`), conditional expressions (`if/then/else`), dotted member access (`Event.Arg`, `Collection.count`, `Field.length`).

### 2.2 Statement Grammar

> To be specified when the parser is implemented. The v1 EBNF in `docs/PreceptLanguageDesign.md` serves as the starting grammar.

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
