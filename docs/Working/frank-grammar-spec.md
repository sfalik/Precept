# Precept TextMate Grammar — Authoritative Specification

**Date:** 2026-05-08
**Author:** Frank
**Status:** DRAFT — pending review

**Source material reviewed:**
- `design/system/semantic-visual-system-manifest.md` — primary visual system design
- `design/system/semantic-visual-system-notes.md` — supplementary notes
- `design/system/README.md` — design system ownership
- `design/brand/brand-decisions.md` — brand palette and typography locked direction
- `design/brand/philosophy.md` — redirects to `docs/philosophy.md`
- `src/Precept/Language/Tokens.cs` (515 lines) — complete token catalog with TextMateScope assignments
- `src/Precept/Language/TokenKind.cs` (205 lines) — 139 token kinds
- `src/Precept/Language/Types.cs` — type catalog (37.4 KB)
- `src/Precept/Language/TypeKind.cs` — 32 type kinds
- `src/Precept/Language/Modifiers.cs` (260 lines) — 29 modifier kinds across 5 DU subtypes
- `src/Precept/Language/ModifierKind.cs` — modifier enum
- `src/Precept/Language/Actions.cs` (222 lines) — 15 action kinds
- `src/Precept/Language/ActionKind.cs` — action enum
- `src/Precept/Language/Operators.cs` (206 lines) — 21 operator kinds
- `src/Precept/Language/OperatorKind.cs` — operator enum
- `src/Precept/Language/Constructs.cs` (199 lines) — 12 construct kinds
- `src/Precept/Language/Functions.cs` — 21 built-in function kinds
- `src/Precept/Language/FunctionKind.cs` — function enum
- `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` (457 lines) — hand-authored grammar
- `tools/Precept.GrammarGen/Program.cs` (537 lines) — grammar generator scaffold
- `docs/tooling/extension.md` — extension architecture
- All 28 `.precept` sample files in `samples/`

---

## Executive Summary

The hand-authored `precept.tmLanguage.json` is **severely incomplete and stale**. It covers roughly 40% of the current language surface, uses at least 3 retired keywords (`nullable`, `invariant`, `assert`), a retired syntax form (`event Name with Arg` instead of parenthesized args), and classifies tokens into only 4 flat keyword groups that collapse the 14 semantic categories the catalog defines. The grammar generator (`GrammarGen/Program.cs`) correctly derives keyword alternation patterns from catalog metadata but carries the same 2 stale structural patterns (`with`-syntax events, `assert` keyword) and omits 8 construct-level patterns and the gold-colored message-string pattern that the visual system design requires. This spec defines the complete grammar that the generator must produce to replace the hand-authored file at parity-or-better.

---

## 1. Design System → TextMate Scope Mapping

The brand decisions (`brand-decisions.md`) lock 8 authoring-time color families plus comments. TextMate scopes must enable theme rules to target each family independently. The catalog (`Tokens.cs`) already assigns a `TextMateScope` to every token. This table maps visual system roles to catalog scopes and notes misalignments.

| # | Design Role | Brand Color | Typography | Catalog TextMateScope(s) | Notes |
|---|-------------|------------|------------|--------------------------|-------|
| 1 | Structure · Semantic | `#4338CA` | **bold** | `keyword.declaration.precept` | Declaration/behavioral keywords: `precept`, `field`, `state`, `event`, `rule`, `ensure`, `as`, `default`, `optional`, `writable`, `because`, `initial`, `ascending`, `descending` |
| 2 | Structure · Grammar | `#6366F1` | normal | `keyword.control.precept` | Prepositions and control flow: `in`, `to`, `from`, `on`, `of`, `into`, `when`, `if`, `then`, `else`, `by`, `at`, `for` |
| 3 | Structure · Grammar (actions) | `#6366F1` | normal | `keyword.other.action.precept` | Action verbs: `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `append`, `insert`, `put` |
| 4 | Structure · Grammar (outcomes) | `#6366F1` | normal | `keyword.other.outcome.precept` | Outcome keywords: `transition`, `no`, `reject` |
| 5 | Structure · Grammar (access) | `#6366F1` | normal | `keyword.other.access-mode.precept` | Access mode: `modify`, `readonly`, `editable`, `omit` |
| 6 | Structure · Grammar (quantifiers) | `#6366F1` | normal | `keyword.other.quantifier.precept` | Quantifiers: `all`, `any`, `each` |
| 7 | Structure · Grammar (constraints) | `#6366F1` | normal | `keyword.other.constraint.precept` | Field constraints: `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`, `ordered` |
| 8 | Structure · Grammar (operators) | `#6366F1` | normal | `keyword.operator.precept`, `keyword.operator.arrow.precept` | Symbol operators (`==`, `!=`, `~=`, `!~`, `>=`, `<=`, `>`, `<`, `=`, `+`, `-`, `*`, `/`, `%`) and arrows (`->`, `<-`) |
| 9 | Structure · Grammar (logical) | `#6366F1` | normal | `keyword.operator.logical.precept` | Keyword operators: `and`, `or`, `not` |
| 10 | Structure · Grammar (membership) | `#6366F1` | normal | `keyword.operator.membership.precept` | Membership: `contains`, `is` |
| 11 | States | `#A898F5` | normal (italic if constrained — semantic tokens only) | `entity.name.type.state.precept` | State names in declarations, `from`/`in`/`to` targets, `transition` targets |
| 12 | Events | `#30B8E8` | normal (italic if constrained — semantic tokens only) | `entity.name.function.event.precept` | Event names in declarations, `on` targets, dot-access prefix |
| 13 | Data · Names | `#B0BEC5` | normal (italic if guarded — semantic tokens only) | `variable.other.field.precept`, `variable.parameter.precept`, `variable.other.property.precept` | Field names, event argument names, property accessors after dot |
| 14 | Data · Types | `#9AA8B5` | normal | `storage.type.precept` | All type keywords. Also `storage.modifier.state.precept` for state modifiers (separate from types but same visual family in brand) |
| 15 | Data · Values | `#84929F` | normal | `constant.numeric.precept`, `constant.language.boolean.precept`, `string.quoted.double.precept`, `string.quoted.single.precept` | Literals: numbers, booleans, strings, typed constants |
| 16 | Rules · Messages | `#FBBF24` | normal | `string.quoted.double.message.precept` | **ONLY** in `because "msg"` and `reject "msg"` positions. Must be distinguished from regular `string.quoted.double.precept` |
| 17 | Comments | `#9096A6` | *italic* | `comment.line.number-sign.precept` | `#` line comments |
| 18 | State modifiers | (brand: same as types) | normal | `storage.modifier.state.precept` | `terminal`, `required`, `irreversible`, `success`, `warning`, `error` |
| 19 | Precept name | (brand: identity) | normal | `entity.name.precept.message.precept` | The precept name after `precept` keyword |
| 20 | Punctuation | `#6366F1` | normal | `punctuation.precept`, `punctuation.separator.comma.precept`, `punctuation.accessor.precept` | `.`, `,`, `(`, `)`, `[`, `]` |
| 21 | Member names | (brand: data names) | normal | `keyword.other.precept` | Special member accessors: `countof`, `peekby` |

### Brand-to-Catalog Misalignment Notes

The brand decisions doc (`brand-decisions.md`) lists specific keywords under "Structure · Semantic" that the catalog assigns to different scope categories:

| Keyword | Brand says | Catalog scope | Resolution |
|---------|-----------|--------------|------------|
| `from`, `on`, `in`, `to` | Structure · Semantic (bold) | `keyword.control.precept` | **Catalog wins.** These are prepositions/control flow. Theme can still bold them if desired. |
| `set` | Structure · Semantic (bold) | `storage.type.precept` (dual-use: action AND type) | **Catalog wins.** `set` is context-dependent — TextMate can't distinguish action vs type usage. Semantic tokens handle this. |
| `transition`, `reject`, `no` | Structure · Semantic (bold) | `keyword.other.outcome.precept` | **Catalog wins.** Dedicated outcome scope enables finer theme control. |
| `when` | Structure · Semantic (bold) | `keyword.control.precept` | **Catalog wins.** Control flow keyword. |
| `write` | Structure · Semantic (bold) | RETIRED (B4 2026-04-28) | **Remove from brand doc.** Replaced by `writable` field modifier. |
| `nullable` | Structure · Grammar | RETIRED | **Remove from brand doc.** Replaced by `optional`. |

**Action:** Brand doc keyword lists need a sync pass to match catalog reality. This is a brand-doc defect, not a grammar defect.

---

## 2. Language Surface Inventory

Complete enumeration of every token/construct type from the catalog, with canonical TextMateScope.

### 2.1 Keywords — Declaration (`keyword.declaration.precept`)

| Token | Text | Source |
|-------|------|--------|
| Precept | `precept` | TokenKind.Precept (=1) |
| Field | `field` | TokenKind.Field (=2) |
| State | `state` | TokenKind.State (=3) |
| Event | `event` | TokenKind.Event (=4) |
| Rule | `rule` | TokenKind.Rule (=5) |
| Ensure | `ensure` | TokenKind.Ensure (=6) |
| As | `as` | TokenKind.As (=7) |
| Default | `default` | TokenKind.Default (=8) |
| Optional | `optional` | TokenKind.Optional (=9) |
| Writable | `writable` | TokenKind.Writable (=10) |
| Because | `because` | TokenKind.Because (=11) |
| Initial | `initial` | TokenKind.Initial (=12) |
| Ascending | `ascending` | TokenKind.Ascending (=130) |
| Descending | `descending` | TokenKind.Descending (=131) |

### 2.2 Keywords — Prepositions/Control (`keyword.control.precept`)

| Token | Text | Source |
|-------|------|--------|
| In | `in` | TokenKind.In (=13) |
| To | `to` | TokenKind.To (=14) |
| From | `from` | TokenKind.From (=15) |
| On | `on` | TokenKind.On (=16) |
| Of | `of` | TokenKind.Of (=17) |
| Into | `into` | TokenKind.Into (=18) |
| When | `when` | TokenKind.When (=19) |
| If | `if` | TokenKind.If (=20) |
| Then | `then` | TokenKind.Then (=21) |
| Else | `else` | TokenKind.Else (=22) |
| By | `by` | TokenKind.By (=128) |
| At | `at` | TokenKind.At (=129) |
| For | `for` | TokenKind.For (=136) |

### 2.3 Keywords — Actions (`keyword.other.action.precept`)

| Token | Text | Source |
|-------|------|--------|
| Add | `add` | TokenKind.Add (=24) |
| Remove | `remove` | TokenKind.Remove (=25) |
| Enqueue | `enqueue` | TokenKind.Enqueue (=26) |
| Dequeue | `dequeue` | TokenKind.Dequeue (=27) |
| Push | `push` | TokenKind.Push (=28) |
| Pop | `pop` | TokenKind.Pop (=29) |
| Clear | `clear` | TokenKind.Clear (=30) |
| Append | `append` | TokenKind.Append (=132) |
| Insert | `insert` | TokenKind.Insert (=133) |
| Put | `put` | TokenKind.Put (=134) |

**Note:** `set` (TokenKind.Set =23) is dual-use (action AND collection type). Catalog assigns `storage.type.precept`. Appears in both action chains and type positions.

### 2.4 Keywords — Outcomes (`keyword.other.outcome.precept`)

| Token | Text | Source |
|-------|------|--------|
| Transition | `transition` | TokenKind.Transition (=31) |
| No | `no` | TokenKind.No (=32) |
| Reject | `reject` | TokenKind.Reject (=33) |

### 2.5 Keywords — Access Modes (`keyword.other.access-mode.precept`)

| Token | Text | Source |
|-------|------|--------|
| Modify | `modify` | TokenKind.Modify (=34) |
| Readonly | `readonly` | TokenKind.Readonly (=35) |
| Editable | `editable` | TokenKind.Editable (=36) |
| Omit | `omit` | TokenKind.Omit (=37) |

### 2.6 Keywords — Logical Operators (`keyword.operator.logical.precept`)

| Token | Text | Source |
|-------|------|--------|
| And | `and` | TokenKind.And (=38) |
| Or | `or` | TokenKind.Or (=39) |
| Not | `not` | TokenKind.Not (=40) |

### 2.7 Keywords — Membership (`keyword.operator.membership.precept`)

| Token | Text | Source |
|-------|------|--------|
| Contains | `contains` | TokenKind.Contains (=41) |
| Is | `is` | TokenKind.Is (=42) |

### 2.8 Keywords — Quantifiers (`keyword.other.quantifier.precept`)

| Token | Text | Source |
|-------|------|--------|
| All | `all` | TokenKind.All (=43) |
| Any | `any` | TokenKind.Any (=44) |
| Each | `each` | TokenKind.Each (=135) |

### 2.9 Keywords — State Modifiers (`storage.modifier.state.precept`)

| Token | Text | Source |
|-------|------|--------|
| Terminal | `terminal` | TokenKind.Terminal (=45) |
| Required | `required` | TokenKind.Required (=46) |
| Irreversible | `irreversible` | TokenKind.Irreversible (=47) |
| Success | `success` | TokenKind.Success (=48) |
| Warning | `warning` | TokenKind.Warning (=49) |
| Error | `error` | TokenKind.Error (=50) |

### 2.10 Keywords — Constraints (`keyword.other.constraint.precept`)

| Token | Text | Source |
|-------|------|--------|
| Nonnegative | `nonnegative` | TokenKind.Nonnegative (=51) |
| Positive | `positive` | TokenKind.Positive (=52) |
| Nonzero | `nonzero` | TokenKind.Nonzero (=53) |
| Notempty | `notempty` | TokenKind.Notempty (=54) |
| Min | `min` | TokenKind.Min (=55) |
| Max | `max` | TokenKind.Max (=56) |
| Minlength | `minlength` | TokenKind.Minlength (=57) |
| Maxlength | `maxlength` | TokenKind.Maxlength (=58) |
| Mincount | `mincount` | TokenKind.Mincount (=59) |
| Maxcount | `maxcount` | TokenKind.Maxcount (=60) |
| Maxplaces | `maxplaces` | TokenKind.Maxplaces (=61) |
| Ordered | `ordered` | TokenKind.Ordered (=62) |

### 2.11 Keywords — Type Names (`storage.type.precept`)

| Token | Text | Family | Source |
|-------|------|--------|--------|
| StringType | `string` | Scalar | TokenKind.StringType (=63) |
| BooleanType | `boolean` | Scalar | TokenKind.BooleanType (=64) |
| IntegerType | `integer` | Scalar | TokenKind.IntegerType (=65) |
| DecimalType | `decimal` | Scalar | TokenKind.DecimalType (=66) |
| NumberType | `number` | Scalar | TokenKind.NumberType (=67) |
| ChoiceType | `choice` | Scalar | TokenKind.ChoiceType (=68) |
| Set | `set` | Collection | TokenKind.Set (=23) — dual-use |
| QueueType | `queue` | Collection | TokenKind.QueueType (=70) |
| StackType | `stack` | Collection | TokenKind.StackType (=71) |
| BagType | `bag` | Collection | TokenKind.BagType (=124) |
| ListType | `list` | Collection | TokenKind.ListType (=125) |
| LogType | `log` | Collection | TokenKind.LogType (=126) |
| LookupType | `lookup` | Collection | TokenKind.LookupType (=127) |
| DateType | `date` | Temporal | TokenKind.DateType (=72) |
| TimeType | `time` | Temporal | TokenKind.TimeType (=73) |
| InstantType | `instant` | Temporal | TokenKind.InstantType (=74) |
| DurationType | `duration` | Temporal | TokenKind.DurationType (=75) |
| PeriodType | `period` | Temporal | TokenKind.PeriodType (=76) |
| TimezoneType | `timezone` | Temporal | TokenKind.TimezoneType (=77) |
| ZonedDateTimeType | `zoneddatetime` | Temporal | TokenKind.ZonedDateTimeType (=78) |
| DateTimeType | `datetime` | Temporal | TokenKind.DateTimeType (=79) |
| MoneyType | `money` | Business | TokenKind.MoneyType (=80) |
| CurrencyType | `currency` | Business | TokenKind.CurrencyType (=81) |
| QuantityType | `quantity` | Business | TokenKind.QuantityType (=82) |
| UnitOfMeasureType | `unitofmeasure` | Business | TokenKind.UnitOfMeasureType (=83) |
| DimensionType | `dimension` | Business | TokenKind.DimensionType (=84) |
| PriceType | `price` | Business | TokenKind.PriceType (=85) |
| ExchangeRateType | `exchangerate` | Business | TokenKind.ExchangeRateType (=86) |

### 2.12 Literals

| Token | Scope | Description |
|-------|-------|-------------|
| True (`true`) | `constant.language.boolean.precept` | Boolean literal |
| False (`false`) | `constant.language.boolean.precept` | Boolean literal |
| NumberLiteral | `constant.numeric.precept` | Integer and decimal numbers |
| StringLiteral | `string.quoted.double.precept` | Double-quoted strings |
| TypedConstant | `string.quoted.single.precept` | Single-quoted typed constants (`'USD'`, `'kg'`) |

**Note:** `null` is NOT a keyword in the token catalog. The hand-authored grammar includes it in `booleanNull` — this is stale. Precept uses `is set`/`is not set` for presence, not `null`.

### 2.13 Symbol Operators (`keyword.operator.precept`)

| Token | Text | Description |
|-------|------|-------------|
| DoubleEquals | `==` | Equality |
| NotEquals | `!=` | Inequality |
| CaseInsensitiveEquals | `~=` | Case-insensitive equals |
| CaseInsensitiveNotEquals | `!~` | Case-insensitive not-equals |
| Tilde | `~` | CI collection inner-type prefix |
| GreaterThanOrEqual | `>=` | Comparison |
| LessThanOrEqual | `<=` | Comparison |
| GreaterThan | `>` | Comparison |
| LessThan | `<` | Comparison |
| Assign | `=` | Assignment |
| Plus | `+` | Addition |
| Minus | `-` | Subtraction/negation |
| Star | `*` | Multiplication |
| Slash | `/` | Division |
| Percent | `%` | Modulo |

### 2.14 Arrow Operators (`keyword.operator.arrow.precept`)

| Token | Text | Description |
|-------|------|-------------|
| Arrow | `->` | Action chain / outcome separator |
| BackArrow | `<-` | Computed field derivation |

### 2.15 Punctuation (`punctuation.precept`)

| Token | Text | Description |
|-------|------|-------------|
| Dot | `.` | Member access |
| Comma | `,` | List separator |
| LeftParen | `(` | Open paren |
| RightParen | `)` | Close paren |
| LeftBracket | `[` | Open bracket |
| RightBracket | `]` | Close bracket |

### 2.16 Member-Name Tokens (`keyword.other.precept`)

| Token | Text | Description |
|-------|------|-------------|
| Countof | `countof` | Bag element count accessor |
| Peekby | `peekby` | Priority queue ordering-key peek |

### 2.17 Built-in Functions (21 total — not keywords, scoped as identifiers)

Functions are parsed as identifier + `(` + arguments + `)`. They are NOT lexer keywords. In TextMate, they match as generic identifiers unless a function-call pattern highlights them. The grammar SHOULD have a pattern for known function names followed by `(`.

| Function | Name |
|----------|------|
| Min | `min` |
| Max | `max` |
| Abs | `abs` |
| Clamp | `clamp` |
| Floor | `floor` |
| Ceil | `ceil` |
| Truncate | `truncate` |
| Round | `round` |
| Approximate | `approximate` |
| Pow | `pow` |
| Sqrt | `sqrt` |
| Trim | `trim` |
| StartsWith | `startsWith` |
| EndsWith | `endsWith` |
| ToLower | `toLower` |
| ToUpper | `toUpper` |
| Left | `left` |
| Right | `right` |
| Mid | `mid` |
| Now | `now` |
| ~startsWith | `~startsWith` (CI variant) |
| ~endsWith | `~endsWith` (CI variant) |

### 2.18 Constructs (12 — from `Constructs.cs`)

| # | ConstructKind | Leading Token(s) | Disambiguation | Example |
|---|---------------|-------------------|----------------|---------|
| 1 | PreceptHeader | `precept` | — | `precept LoanApplication` |
| 2 | FieldDeclaration | `field` | — | `field amount as money nonnegative` |
| 3 | StateDeclaration | `state` | — | `state Draft initial, Submitted, Approved terminal success` |
| 4 | EventDeclaration | `event` | — | `event Submit(approver as string)` |
| 5 | RuleDeclaration | `rule` | — | `rule amount > 0 because "..."` |
| 6 | TransitionRow | `from` + `on` | Disambiguated by `on` | `from Draft on Submit -> ... -> transition Submitted` |
| 7 | StateEnsure | `in`/`to`/`from` + `ensure` | Disambiguated by `ensure` | `in Approved ensure amount > 0 because "..."` |
| 8 | AccessMode | `in` + `modify` | Disambiguated by `modify` | `in Draft modify Amount editable` |
| 9 | OmitDeclaration | `in` + `omit` | Disambiguated by `omit` | `in Draft omit InternalNotes` |
| 10 | StateAction | `to`/`from` + `->` | Disambiguated by `->` | `to Confirmed -> set PaymentReceived = true` |
| 11 | EventEnsure | `on` + `ensure` | Disambiguated by `ensure` | `on Submit ensure Amount > 0 because "..."` |
| 12 | EventHandler | `on` + `->` | Disambiguated by `->` | `on UpdateName -> set name = newName` |

---

## 3. Hand-Authored Grammar Audit

### 3.1 Coverage Gap Table

| # | Language Construct / Token | In Hand Grammar? | Scope Assignment | Gap / Issue |
|---|---------------------------|:---:|------------------|-------------|
| G1 | `rule` keyword | ❌ NO | — | Missing. `invariant` exists at L276 but `rule` replaced it. |
| G2 | `ensure` keyword | ❌ NO | — | Missing entirely. Used in StateEnsure, EventEnsure constructs. |
| G3 | `optional` keyword | ❌ NO | — | Missing. L366 has stale `nullable` instead. |
| G4 | `writable` keyword | ❌ NO | — | Missing. New field modifier (B4). |
| G5 | `modify` keyword | ❌ NO | — | Missing. Access mode construct (B4). |
| G6 | `readonly` keyword | ❌ NO | — | Missing. Access mode adjective (B4). |
| G7 | `editable` keyword | ❌ NO | — | Missing. Access mode adjective (B4). |
| G8 | `omit` keyword (construct) | ❌ NO | — | Missing. Omit declaration construct. |
| G9 | State modifiers: `terminal`, `required`, `irreversible`, `success`, `warning`, `error` | ❌ NO | — | Only `initial` handled in state declaration (L101). 6 modifiers missing. |
| G10 | Type keywords: `integer`, `decimal`, `choice` | ❌ NO | — | L373-377 only has `string\|number\|boolean\|set\|queue\|stack`. |
| G11 | Temporal types (8): `date` through `datetime` | ❌ NO | — | All 8 temporal types missing. |
| G12 | Business-domain types (7): `money` through `exchangerate` | ❌ NO | — | All 7 business types missing. |
| G13 | Collection types: `bag`, `list`, `log`, `lookup` | ❌ NO | — | Missing from type keywords and collection field pattern. |
| G14 | Constraint keywords (12): `nonnegative` through `ordered` | ❌ NO | — | None present in grammar. |
| G15 | Access mode keywords | ❌ NO | — | `modify`, `readonly`, `editable` missing. |
| G16 | Quantifier `each` | ❌ NO | — | Missing. |
| G17 | Prepositions `by`, `at`, `for` | ❌ NO | — | Missing. |
| G18 | Control `then` | ❌ NO | — | Missing from L358 (has `if`/`else` but not `then`). |
| G19 | Action keywords: `append`, `insert`, `put` | ❌ NO | — | Missing from L381-385. |
| G20 | Operators: `~=`, `!~`, `~` | ❌ NO | — | Case-insensitive operators missing from L413-429. |
| G21 | Typed constants (`'...'`) | ❌ NO | — | No single-quoted string pattern. |
| G22 | Parenthesized event args | ❌ NO | — | `event Name(Arg as type)` syntax not matched. Grammar uses retired `with` syntax (L148-188). |
| G23 | RuleDeclaration construct | ❌ NO | — | No pattern for `rule Expr because "msg"`. |
| G24 | StateEnsure construct | ❌ NO | — | No pattern for `in/to/from State ensure Expr because "msg"`. |
| G25 | EventEnsure construct | ❌ NO | — | No pattern for `on Event ensure Expr because "msg"`. |
| G26 | AccessMode construct | ❌ NO | — | No pattern for `in State modify Field editable`. |
| G27 | OmitDeclaration construct | ❌ NO | — | No pattern for `in State omit Field`. |
| G28 | StateAction construct | ❌ NO | — | No pattern for `to/from State -> action chain`. |
| G29 | EventHandler construct | ❌ NO | — | No pattern for `on Event -> action chain`. |
| G30 | Computed field syntax | ❌ NO | — | `field X as type <- expr` not specifically highlighted. `<-` is in `arrowOperator` but no construct pattern. |
| G31 | Function calls | ❌ NO | — | `min(...)`, `round(...)` etc. — no function-name highlighting. |
| G32 | Parentheses/brackets | Partial | `punctuation.precept` | Parentheses exist in code but no explicit grammar pattern matches `(` or `)`. |
| G33 | Choice type with options | ❌ NO | — | `choice of string("a","b","c")` not matched. |
| G34 | Ascending/descending | ❌ NO | — | Sort order modifiers missing. |
| G35 | `is set` / `is not set` operators | ❌ NO | — | Multi-token presence operators not highlighted. |

### 3.2 Stale / Incorrect Patterns

| # | Pattern | Line | Issue |
|---|---------|------|-------|
| S1 | `declarationKeywords` → `nullable` | L366 | STALE. Should be `optional`. `nullable` is not in TokenKind. |
| S2 | `declarationKeywords` → `invariant` | L366 | STALE. Should be `rule`. `invariant` is not in TokenKind. |
| S3 | `declarationKeywords` → `with` | L366 | STALE. `with` is not in TokenKind. Retired event-arg syntax. |
| S4 | `declarationKeywords` → `assert` | L366 | STALE. Should be `ensure`. `assert` is not in TokenKind. |
| S5 | `booleanNull` → `null` | L436 | STALE. `null` is not a keyword in the token catalog. Precept uses `is set`/`is not set`. |
| S6 | `eventWithArgsDeclaration` | L146-188 | STALE. Uses `event Name with Arg as type` syntax. Current syntax is `event Name(Arg as type)`. |
| S7 | `invariantStatement` | L276-286 | STALE. Uses `invariant` keyword. Should be `rule`. |
| S8 | `assertStatement` | L288-300 | STALE. Uses `on EventName assert`. Should be `on EventName ensure`. |
| S9 | `controlKeywords` mix | L356-361 | INCORRECT. Mixes declaration keywords (`precept`, `state`, `event`) with control flow (`if`, `when`). Should use catalog-derived scope groups. |
| S10 | `actionKeywords` mix | L380-385 | INCORRECT. Mixes actions (`set`, `add`), prepositions (`into`), membership (`contains`), and logical operators (`and`, `or`, `not`) into one scope. |

### 3.3 Scope Assignment Errors

| # | Token | Grammar Scope | Catalog Scope | Visual System Role |
|---|-------|--------------|---------------|--------------------|
| E1 | `precept` | `keyword.control.precept` (L359) | `keyword.declaration.precept` | Structure · Semantic |
| E2 | `state` | `keyword.control.precept` (L359) | `keyword.declaration.precept` | Structure · Semantic |
| E3 | `event` | `keyword.control.precept` (L359) | `keyword.declaration.precept` | Structure · Semantic |
| E4 | `field` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |
| E5 | `as` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |
| E6 | `because` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |
| E7 | `default` | `keyword.other.precept` (L366) | `keyword.declaration.precept` | Structure · Semantic |
| E8 | `and`, `or`, `not` | `keyword.other.precept` (L383) | `keyword.operator.logical.precept` | Logical operators |
| E9 | `contains` | `keyword.other.precept` (L383) | `keyword.operator.membership.precept` | Membership |
| E10 | `into` | `keyword.other.precept` (L383) | `keyword.control.precept` | Preposition |
| E11 | `all`, `any` | `keyword.control.precept` (L359) | `keyword.other.quantifier.precept` | Quantifier |
| E12 | `of` | `keyword.control.precept` (L359) | `keyword.control.precept` | ✓ Correct |
| E13 | `set` (action) | `keyword.other.precept` (L383) | `storage.type.precept` | Dual-use |
| E14 | `transition` | `keyword.other.precept` (L394) | `keyword.other.outcome.precept` | Outcome |
| E15 | `reject` | `keyword.other.precept` (L394/L68) | `keyword.other.outcome.precept` | Outcome |
| E16 | `edit` | `keyword.other.precept` (L367) | Not a TokenKind! | `edit` appears in root-edit pattern but is not in the token catalog. The construct is `RuleDeclaration`, not `edit`. Actually, `edit` is used for `rootEditDeclaration` — but the TokenKind enum doesn't have an Edit token. Checking... `edit` may be a stale surface concept. The Constructs catalog does not have a root-level `edit` construct. This needs verification. |

**Note on E16:** Looking at the Constructs catalog, there is no `edit` construct. The `OmitDeclaration` and `AccessMode` constructs handle field access. The `rootEditDeclaration` pattern in the hand-authored grammar (`edit all | edit Field1, Field2`) may be stale — I need to verify whether root-level `edit` still exists. Looking at the sample files: `customer-profile.precept` uses `writable` modifier on fields, not `edit`. `fee-schedule.precept` uses `writable`. No sample uses `edit all`. This pattern appears stale. **However**, the `rootEditDeclaration` pattern is in both the hand-authored grammar AND the generator, so it may still be valid for backward compatibility. Needs owner clarification.

---

## 4. Generator Audit

### 4.1 Generator Strengths

1. **Catalog-driven keyword emission** (L38-77): Reads `Tokens.All`, groups by `TextMateScope`, emits one alternation pattern per scope. This correctly picks up all 139 tokens. ✓
2. **Typed constants** (L134-146): Handles single-quoted `'...'` strings. Hand-authored grammar doesn't. ✓
3. **Collection member access** (L453-470): Includes `countof` and `peekby`. ✓

### 4.2 Generator Gap Table

| # | Language Construct / Feature | Generator Pattern? | Correct Scope? | Gap |
|---|-----------------------------|----|----|----|
| GG1 | Message strings (`because "msg"`, `reject "msg"`) | ❌ NO | — | **Critical.** Visual system reserves gold for message payloads. Without this, all strings get `string.quoted.double.precept` — no visual interrupt for rules. |
| GG2 | Parenthesized event args `event Name(Arg as type)` | ❌ NO | — | Generator's `eventWithArgsDeclaration` (L218-258) uses stale `with` syntax. |
| GG3 | State modifiers beyond `initial` | ❌ NO | — | `stateDeclaration` (L180-215) only matches `initial`. Missing: `terminal`, `required`, `irreversible`, `success`, `warning`, `error`. These ARE emitted as catalog keywords under `storage.modifier.state.precept`, but the structural pattern doesn't recognize them in state declaration context. |
| GG4 | RuleDeclaration construct | ❌ NO | — | No `rule Expr because "msg"` pattern. |
| GG5 | StateEnsure constructs | ❌ NO | — | No `in/to/from State ensure Expr because "msg"` pattern. |
| GG6 | EventEnsure construct | ❌ NO | — | No `on Event ensure Expr because "msg"` pattern. |
| GG7 | AccessMode construct | ❌ NO | — | No `in State modify Field editable/readonly` pattern. |
| GG8 | OmitDeclaration construct | ❌ NO | — | No `in State omit Field` pattern. |
| GG9 | StateAction construct | ❌ NO | — | No `to/from State -> action chain` pattern. |
| GG10 | EventHandler construct | ❌ NO | — | No `on Event -> action chain` (stateless). |
| GG11 | Computed field declaration | ❌ NO | — | No `field X as type <- expr` structural pattern. |
| GG12 | Function call highlighting | ❌ NO | — | `min(...)`, `round(...)` etc. not highlighted as function names. |
| GG13 | Choice type with options | ❌ NO | — | `choice of string("a","b","c")` not matched. |
| GG14 | `assertStatement` uses stale `assert` | ✅ Present | ❌ Wrong keyword | L416-432: Uses `assert` instead of `ensure`. |
| GG15 | `no transition` compound keyword | ❌ NO | — | Two-word outcome not specially highlighted. The individual words are catalog-derived, but the compound meaning is lost. |
| GG16 | `is set` / `is not set` operators | ❌ NO | — | Multi-token presence operators. |
| GG17 | `ScopeToRepositoryKey` naming | — | — | Appends "Keywords" to scope, producing confusing repo keys like `keyword.declaration.preceptKeywords`. Should use descriptive names (e.g., `declarationKeywords`). |
| GG18 | `eventWithArgsDeclaration` broken `$ref` | ❌ Broken | — | L244: Uses `["$ref"] = "#/repository/storage.type.precept"` — TextMate doesn't support `$ref`. Should be `["include"] = "#storage.type.preceptKeywords"`. |
| GG19 | `fieldCollectionDeclaration` scope error | — | ❌ Wrong | L296: Uses `keyword.declaration.precept` for `field` keyword but the repo key in catalog patterns uses the same scope. Creates conflict with `keyword.declaration.preceptKeywords` repo entry — both claim `keyword.declaration.precept`. |
| GG20 | Missing punctuation patterns | ❌ NO | — | No explicit patterns for `(`, `)`, `[`, `]`. Catalog assigns them `punctuation.precept`. |

---

## 5. Authoritative Grammar Specification

### Spec Section 1: Scope Vocabulary

Every TextMate scope used in the Precept grammar, with semantic meaning and visual system role.

| # | TextMate Scope | Semantic Meaning | Visual System Role | Brand Color |
|---|---------------|------------------|-------------------|-------------|
| S1 | `comment.line.number-sign.precept` | Line comment starting with `#` | Comments | `#9096A6` italic |
| S2 | `keyword.declaration.precept` | Declaration and behavioral keywords | Structure · Semantic | `#4338CA` **bold** |
| S3 | `keyword.control.precept` | Prepositions and control flow | Structure · Grammar | `#6366F1` normal |
| S4 | `keyword.other.action.precept` | Action verbs in action chains | Structure · Grammar | `#6366F1` normal |
| S5 | `keyword.other.outcome.precept` | Transition, rejection, no-transition outcomes | Structure · Grammar | `#6366F1` normal |
| S6 | `keyword.other.access-mode.precept` | Access mode declarations | Structure · Grammar | `#6366F1` normal |
| S7 | `keyword.other.quantifier.precept` | Universal/existential quantifiers | Structure · Grammar | `#6366F1` normal |
| S8 | `keyword.other.constraint.precept` | Field constraint modifiers | Structure · Grammar | `#6366F1` normal |
| S9 | `keyword.operator.logical.precept` | `and`, `or`, `not` | Structure · Grammar | `#6366F1` normal |
| S10 | `keyword.operator.membership.precept` | `contains`, `is` | Structure · Grammar | `#6366F1` normal |
| S11 | `keyword.operator.precept` | Symbol operators (`==`, `!=`, `+`, `-`, etc.) | Structure · Grammar | `#6366F1` normal |
| S12 | `keyword.operator.arrow.precept` | `->` and `<-` arrows | Structure · Grammar | `#6366F1` normal |
| S13 | `storage.type.precept` | Type keywords (all scalar, temporal, business, collection types) | Data · Types | `#9AA8B5` normal |
| S14 | `storage.modifier.state.precept` | State lifecycle modifiers | Data · Types | `#9AA8B5` normal |
| S15 | `entity.name.type.state.precept` | State names | States | `#A898F5` normal |
| S16 | `entity.name.function.event.precept` | Event names | Events | `#30B8E8` normal |
| S17 | `entity.name.precept.message.precept` | Precept name (in header) | Identity | `#A898F5` normal |
| S18 | `variable.other.field.precept` | Field names | Data · Names | `#B0BEC5` normal |
| S19 | `variable.parameter.precept` | Event argument names (in declarations) | Data · Names | `#B0BEC5` normal |
| S20 | `variable.other.property.precept` | Property accessor after dot | Data · Names | `#B0BEC5` normal |
| S21 | `variable.other.precept` | Catch-all identifier reference | Data · Names | `#B0BEC5` normal |
| S22 | `constant.numeric.precept` | Number literals | Data · Values | `#84929F` normal |
| S23 | `constant.language.boolean.precept` | `true`, `false` | Data · Values | `#84929F` normal |
| S24 | `string.quoted.double.precept` | Double-quoted strings (non-message) | Data · Values | `#84929F` normal |
| S25 | `string.quoted.double.message.precept` | Message strings in `because`/`reject` | Rules · Messages | `#FBBF24` normal |
| S26 | `string.quoted.single.precept` | Single-quoted typed constants | Data · Values | `#84929F` normal |
| S27 | `constant.character.escape.precept` | Escape sequences in strings | Data · Values | `#84929F` normal |
| S28 | `punctuation.precept` | `.`, `,`, `(`, `)`, `[`, `]` | Structure · Grammar | `#6366F1` normal |
| S29 | `punctuation.separator.comma.precept` | Comma separator (in lists) | Structure · Grammar | `#6366F1` normal |
| S30 | `punctuation.accessor.precept` | Dot accessor (in member access) | Structure · Grammar | `#6366F1` normal |
| S31 | `keyword.other.precept` | Special member names (`countof`, `peekby`) | Data · Names | `#B0BEC5` normal |
| S32 | `support.function.precept` | Built-in function names | Data · Names | `#B0BEC5` normal |
| S33 | `meta.declaration.precept.precept` | Precept header construct (meta) | — | — |
| S34 | `meta.declaration.state.precept` | State declaration construct (meta) | — | — |
| S35 | `meta.declaration.event.precept` | Event declaration construct (meta) | — | — |
| S36 | `meta.field-declaration.precept` | Field declaration construct (meta) | — | — |
| S37 | `meta.transition.header.precept` | Transition row header (meta) | — | — |
| S38 | `meta.ensure.state.precept` | State ensure construct (meta) | — | — |
| S39 | `meta.ensure.event.precept` | Event ensure construct (meta) | — | — |
| S40 | `meta.access-mode.precept` | Access mode construct (meta) | — | — |
| S41 | `meta.omit.precept` | Omit declaration construct (meta) | — | — |
| S42 | `meta.action.state.precept` | State action construct (meta) | — | — |
| S43 | `meta.handler.event.precept` | Event handler construct (meta) | — | — |
| S44 | `meta.rule.precept` | Rule declaration construct (meta) | — | — |
| S45 | `meta.message.precept` | Message string context (meta) | — | — |
| S46 | `meta.computed-field.precept` | Computed field declaration (meta) | — | — |
| S47 | `meta.transition.target.precept` | Transition target (meta) | — | — |
| S48 | `meta.event-arg-ref.precept` | Event.arg dot access (meta) | — | — |
| S49 | `meta.collection-member.precept` | Collection.property access (meta) | — | — |

### Spec Section 2: Repository Patterns (Complete Enumeration)

#### 2.1 Comment

- **Key:** `comment`
- **Type:** `match`
- **Scope:** `comment.line.number-sign.precept`
- **Regex:** `#.*$`
- **Covers:** Line comments

#### 2.2 Message Strings

- **Key:** `messageStrings`
- **Type:** `match` (two patterns)
- **Scope:** captures `keyword.declaration.precept` for keyword, `string.quoted.double.message.precept` for message
- **Regex pattern 1:** `\b(because)(\s+)("(?:\\.|[^"\\])*")`
- **Regex pattern 2:** `\b(reject)(\s+)("(?:\\.|[^"\\])*")`
- **Covers:** Gold message payload in `because "..."` and `reject "..."` positions
- **Priority:** MUST precede generic `strings` pattern to prevent message strings from being consumed as regular strings
- **Visual system:** This is the **only** pattern that produces `string.quoted.double.message.precept` — the gold visual interrupt

#### 2.3 Strings

- **Key:** `strings`
- **Type:** `begin/end`
- **Scope:** `string.quoted.double.precept`
- **Begin:** `"`   End: `"`
- **Inner pattern:** `constant.character.escape.precept` for `\\.`
- **Covers:** All non-message double-quoted strings

#### 2.4 Typed Constants

- **Key:** `typedConstants`
- **Type:** `begin/end`
- **Scope:** `string.quoted.single.precept`
- **Begin:** `'`   End: `'`
- **Covers:** Single-quoted typed constants (`'USD'`, `'kg'`, `'2026-01-15'`)

#### 2.5 Precept Header

- **Key:** `preceptHeader`
- **Type:** `match`
- **Scope:** `meta.declaration.precept.precept`
- **Regex:** `^(\s*)(precept)(\s+)([A-Za-z_][A-Za-z0-9_]*)`
- **Captures:** `2` → `keyword.declaration.precept`, `4` → `entity.name.precept.message.precept`
- **Covers:** `precept LoanApplication`

#### 2.6 State Declaration

- **Key:** `stateDeclaration`
- **Type:** `match`
- **Scope:** `meta.declaration.state.precept`
- **Regex:** `^(\s*)(state)(\s+)(.*)`
- **Captures:** `2` → `keyword.declaration.precept`, `4` → sub-patterns:
  - State modifiers from catalog: `\b(initial|terminal|required|irreversible|success|warning|error)\b` → `storage.modifier.state.precept` (for `terminal`/`required`/`irreversible`/`success`/`warning`/`error`) and `keyword.declaration.precept` (for `initial`)
  - State names: `\b[A-Za-z_][A-Za-z0-9_]*\b` → `entity.name.type.state.precept`
  - Comma: `,` → `punctuation.separator.comma.precept`
- **Covers:** `state Draft initial, Submitted, Approved terminal success`
- **Critical change from current:** Must recognize ALL 7 state modifiers, not just `initial`

#### 2.7 Event Declaration (Parenthesized Args)

- **Key:** `eventDeclaration`
- **Type:** `match`
- **Scope:** `meta.declaration.event.precept`
- **Regex:** `^(\s*)(event)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*)(\s*\(.*)?`
- **Captures:**
  - `2` → `keyword.declaration.precept`
  - `4` → sub-patterns for event names (`entity.name.function.event.precept`) and commas
  - `5` → sub-patterns for parenthesized args:
    - `initial` keyword → `keyword.declaration.precept`
    - Argument name before `as`: `\b([A-Za-z_][A-Za-z0-9_]*)(?=\s+as\b)` → `variable.parameter.precept`
    - `as` keyword → `keyword.declaration.precept`
    - Type keywords → include `#typeKeywords`
    - Constraint keywords → include `#constraintKeywords`
    - Default values → include `#numbers`, `#strings`, `#booleanLiterals`
    - Commas → `punctuation.separator.comma.precept`
    - Parentheses → `punctuation.precept`
- **Covers:** `event Submit(Applicant as string notempty, Amount as number)`
- **Critical change:** Replaces stale `eventWithArgsDeclaration` (used `with` syntax)

#### 2.8 Field Declaration (Scalar)

- **Key:** `fieldScalarDeclaration`
- **Type:** `match`
- **Scope:** `meta.field-declaration.precept`
- **Regex:** `^(\s*)(field)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*)(\s+)(as)(\s+)(string|number|integer|decimal|boolean|choice|date|time|instant|duration|period|timezone|zoneddatetime|datetime|money|currency|quantity|unitofmeasure|dimension|price|exchangerate)(.*)`
- **Captures:**
  - `2` → `keyword.declaration.precept`
  - `4` → field names (`variable.other.field.precept`) + commas
  - `6` → `keyword.declaration.precept`
  - `8` → `storage.type.precept`
  - `9` → sub-patterns: constraint keywords, `optional`, `writable`, `default`, numbers, strings, typed constants, `<-` for computed
- **Covers:** All scalar field declarations including temporal and business-domain types
- **Note:** Type name list MUST be derived from the catalog (`Tokens.All` where category is `Type` and text is not null)

#### 2.9 Field Declaration (Collection)

- **Key:** `fieldCollectionDeclaration`
- **Type:** `match`
- **Scope:** `meta.field-declaration.precept`
- **Regex:** `^(\s*)(field)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*)(\s+)(as)(\s+)(set|queue|stack|bag|list|log|lookup)(\s+)(of)(\s+)(~?(?:string|number|integer|decimal|boolean))(.*)`
- **Captures:**
  - `2` → `keyword.declaration.precept`
  - `4` → field names + commas
  - `6` → `keyword.declaration.precept`
  - `8` → `storage.type.precept` (collection type)
  - `10` → `keyword.control.precept` (`of`)
  - `12` → `storage.type.precept` (inner type, with optional `~` prefix)
  - `13` → sub-patterns for constraint keywords, modifiers
- **Covers:** `field Tags as set of string`, `field Items as bag of ~string`

#### 2.10 Computed Field Declaration

- **Key:** `computedFieldDeclaration`
- **Type:** `match`
- **Scope:** `meta.computed-field.precept`
- **Regex:** `^(\s*)(field)(\s+)([A-Za-z_][A-Za-z0-9_]*)(\s+)(as)(\s+)(string|number|integer|decimal|boolean|..types..)(\s+.*)?(<-)(\s+.*)`
- **Note:** This is hard to capture in a single regex because the `<-` can appear after optional modifiers. Recommend a separate pattern that matches `<-` preceded by field context, or handle via the existing field declaration patterns plus the arrow operator pattern.
- **Alternative approach:** The `<-` operator is already in the catalog. The constraint keywords and type keywords are already catalog-derived. A computed field declaration is just a field declaration that happens to contain `<-`. The structural pattern can be the same as `fieldScalarDeclaration` if the tail sub-patterns include the `<-` operator and expression patterns.

#### 2.11 Root Edit Declaration

- **Key:** `rootEditDeclaration`
- **Type:** `match`
- **Scope:** `meta.declaration.edit.root.precept`
- **Status:** **NEEDS VERIFICATION.** The `edit` keyword is not in the `TokenKind` enum. No sample file uses root-level `edit`. This pattern may be stale. If confirmed stale, remove. If still valid, add `edit` to TokenKind.

#### 2.12 Transition Row Header

- **Key:** `fromOnHeader`
- **Type:** `match`
- **Scope:** `meta.transition.header.precept`
- **Regex:** `^(\s*)(from)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*(?:\s*,\s*[A-Za-z_][A-Za-z0-9_]*)*)(\s+)(on)(\s+)([A-Za-z_][A-Za-z0-9_]*)`
- **Captures:**
  - `2` → `keyword.control.precept` (`from`)
  - `4` → `entity.name.type.state.precept` (source state(s)) — `any` should get `keyword.other.quantifier.precept`
  - `6` → `keyword.control.precept` (`on`)
  - `8` → `entity.name.function.event.precept` (event name)
- **Covers:** `from Draft on Submit`, `from any on Cancel`
- **Note:** `any` in state position should get quantifier scope, not state scope. Needs sub-pattern.

#### 2.13 State Ensure

- **Key:** `stateEnsure`
- **Type:** `match`
- **Scope:** `meta.ensure.state.precept`
- **Regex:** `^(\s*)(in|to|from)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(ensure)\b`
- **Captures:**
  - `2` → `keyword.control.precept` (anchor preposition)
  - `4` → `entity.name.type.state.precept` (state name) — `any` → `keyword.other.quantifier.precept`
  - `6` → `keyword.declaration.precept` (`ensure`)
- **Covers:** `in Approved ensure amount > 0 because "..."`

#### 2.14 Event Ensure

- **Key:** `eventEnsure`
- **Type:** `match`
- **Scope:** `meta.ensure.event.precept`
- **Regex:** `^(\s*)(on)(\s+)([A-Za-z_][A-Za-z0-9_]*)(\s+)(ensure)\b`
- **Captures:**
  - `2` → `keyword.control.precept` (`on`)
  - `4` → `entity.name.function.event.precept` (event name)
  - `6` → `keyword.declaration.precept` (`ensure`)
- **Covers:** `on Submit ensure Amount > 0 because "..."`

#### 2.15 Access Mode

- **Key:** `accessMode`
- **Type:** `match`
- **Scope:** `meta.access-mode.precept`
- **Regex:** `^(\s*)(in)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(modify)(\s+)((?:[A-Za-z_][A-Za-z0-9_]*\s*,\s*)*[A-Za-z_][A-Za-z0-9_]*|all)(\s+)(editable|readonly)`
- **Captures:**
  - `2` → `keyword.control.precept`
  - `4` → `entity.name.type.state.precept` (state name)
  - `6` → `keyword.other.access-mode.precept` (`modify`)
  - `8` → `variable.other.field.precept` (field names) or `keyword.other.quantifier.precept` (`all`)
  - `10` → `keyword.other.access-mode.precept` (`editable`/`readonly`)
- **Covers:** `in Draft modify Amount editable`, `in UnderReview modify AdjusterName editable when not FraudFlag`

#### 2.16 Omit Declaration

- **Key:** `omitDeclaration`
- **Type:** `match`
- **Scope:** `meta.omit.precept`
- **Regex:** `^(\s*)(in)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(omit)(\s+)([A-Za-z_][A-Za-z0-9_]*)`
- **Captures:**
  - `2` → `keyword.control.precept`
  - `4` → `entity.name.type.state.precept`
  - `6` → `keyword.other.access-mode.precept` (`omit`)
  - `8` → `variable.other.field.precept`
- **Covers:** `in Draft omit InternalNotes`

#### 2.17 State Action

- **Key:** `stateAction`
- **Type:** `match`
- **Scope:** `meta.action.state.precept`
- **Regex:** `^(\s*)(to|from)(\s+)(any|[A-Za-z_][A-Za-z0-9_]*)(\s+)(->)`
- **Captures:**
  - `2` → `keyword.control.precept` (anchor preposition)
  - `4` → `entity.name.type.state.precept` (state name)
  - `6` → `keyword.operator.arrow.precept` (`->`)
- **Covers:** `to Confirmed -> set PaymentReceived = true`
- **Note:** Must precede `stateEnsure` in pattern order since both start with `to`/`from`. Disambiguated by `->` vs `ensure`.

#### 2.18 Event Handler

- **Key:** `eventHandler`
- **Type:** `match`
- **Scope:** `meta.handler.event.precept`
- **Regex:** `^(\s*)(on)(\s+)([A-Za-z_][A-Za-z0-9_]*)(\s+)(->)`
- **Captures:**
  - `2` → `keyword.control.precept` (`on`)
  - `4` → `entity.name.function.event.precept` (event name)
  - `6` → `keyword.operator.arrow.precept` (`->`)
- **Covers:** `on UpdateName -> set name = newName` (stateless precepts)
- **Note:** Must precede `eventEnsure` in pattern order since both start with `on`.

#### 2.19 Rule Declaration

- **Key:** `ruleDeclaration`
- **Type:** `match`
- **Scope:** `meta.rule.precept`
- **Regex:** `^(\s*)(rule)\b`
- **Captures:** `2` → `keyword.declaration.precept`
- **Covers:** `rule amount > 0 because "..."`
- **Note:** Only needs to capture the `rule` keyword. The rest of the line is handled by included patterns (operators, identifiers, message strings, etc.)

#### 2.20 Transition Target

- **Key:** `transitionTarget`
- **Type:** `match`
- **Scope:** `meta.transition.target.precept`
- **Regex:** `\b(transition)(\s+)([A-Za-z_][A-Za-z0-9_]*)`
- **Captures:**
  - `1` → `keyword.other.outcome.precept`
  - `3` → `entity.name.type.state.precept`
- **Covers:** `transition Approved`

#### 2.21 No Transition

- **Key:** `noTransition`
- **Type:** `match`
- **Scope:** (captures only)
- **Regex:** `\b(no)(\s+)(transition)\b`
- **Captures:**
  - `1` → `keyword.other.outcome.precept`
  - `3` → `keyword.other.outcome.precept`
- **Covers:** `no transition`

#### 2.22 Event Arg Reference (Dot Access)

- **Key:** `eventArgReference`
- **Type:** `match`
- **Scope:** `meta.event-arg-ref.precept`
- **Regex:** `\b([A-Za-z_][A-Za-z0-9_]*)(\.)([A-Za-z_][A-Za-z0-9_]*)`
- **Captures:**
  - `1` → `entity.name.function.event.precept` (event name)
  - `2` → `punctuation.accessor.precept`
  - `3` → `variable.other.property.precept` (arg/property name)
- **Covers:** `Submit.Amount`, `Approve.Note`
- **Note:** This pattern is ambiguous — it also matches `Collection.count`. The `collectionMemberAccess` pattern must precede this one.

#### 2.23 Collection Member Access

- **Key:** `collectionMemberAccess`
- **Type:** `match`
- **Regex:** `\b([A-Za-z_][A-Za-z0-9_]*)(\.)(\bcount|countof|min|max|peek|peekby\b)`
- **Captures:**
  - `1` → `variable.other.field.precept` (collection field name)
  - `2` → `punctuation.accessor.precept`
  - `3` → `variable.other.property.precept` (member name)
- **Covers:** `MissingDocuments.count`, `Queue.peek`
- **Priority:** Must precede `eventArgReference` to prevent `Collection.count` from being highlighted as event.arg.

#### 2.24 Function Calls

- **Key:** `functionCalls`
- **Type:** `match`
- **Scope:** (captures only)
- **Regex:** `\b(min|max|abs|clamp|floor|ceil|truncate|round|approximate|pow|sqrt|trim|startsWith|endsWith|toLower|toUpper|left|right|mid|now)(\s*\()`
- **Captures:**
  - `1` → `support.function.precept`
  - `2` → `punctuation.precept`
- **Covers:** `min(x, y)`, `round(amount, 2)`, `trim(name)`, `now()`
- **Note:** Function name list MUST be derived from `Functions.All` (via the function catalog). CI variants `~startsWith` and `~endsWith` need a separate pattern: `(~)(startsWith|endsWith)(\s*\()`.

#### 2.25 Catalog-Derived Keyword Groups

These are generated automatically by reading `Tokens.All`, grouping by `TextMateScope`, and emitting one alternation pattern per scope. The generator already does this (L38-77 of `Program.cs`).

**Repository keys** (use descriptive names, not scope-suffixed):

| Key | Scope | Tokens |
|-----|-------|--------|
| `declarationKeywords` | `keyword.declaration.precept` | `as`, `ascending`, `because`, `default`, `descending`, `ensure`, `event`, `field`, `initial`, `optional`, `precept`, `rule`, `state`, `writable` |
| `controlKeywords` | `keyword.control.precept` | `at`, `by`, `else`, `for`, `from`, `if`, `in`, `into`, `of`, `on`, `then`, `to`, `when` |
| `actionKeywords` | `keyword.other.action.precept` | `add`, `append`, `clear`, `dequeue`, `enqueue`, `insert`, `pop`, `push`, `put`, `remove` |
| `outcomeKeywords` | `keyword.other.outcome.precept` | `no`, `reject`, `transition` |
| `accessModeKeywords` | `keyword.other.access-mode.precept` | `editable`, `modify`, `omit`, `readonly` |
| `logicalOperators` | `keyword.operator.logical.precept` | `and`, `not`, `or` |
| `membershipOperators` | `keyword.operator.membership.precept` | `contains`, `is` |
| `quantifierKeywords` | `keyword.other.quantifier.precept` | `all`, `any`, `each` |
| `stateModifiers` | `storage.modifier.state.precept` | `error`, `irreversible`, `required`, `success`, `terminal`, `warning` |
| `constraintKeywords` | `keyword.other.constraint.precept` | `max`, `maxcount`, `maxlength`, `maxplaces`, `min`, `mincount`, `minlength`, `nonnegative`, `nonzero`, `notempty`, `ordered`, `positive` |
| `typeKeywords` | `storage.type.precept` | `bag`, `boolean`, `choice`, `currency`, `date`, `datetime`, `decimal`, `dimension`, `duration`, `exchangerate`, `instant`, `integer`, `list`, `log`, `lookup`, `money`, `number`, `period`, `price`, `quantity`, `queue`, `set`, `stack`, `string`, `time`, `timezone`, `unitofmeasure`, `zoneddatetime` |
| `booleanLiterals` | `constant.language.boolean.precept` | `false`, `true` |
| `symbolOperators` | `keyword.operator.precept` | `!=`, `!~`, `%`, `*`, `+`, `-`, `/`, `<`, `<=`, `==`, `>`, `>=`, `=`, `~`, `~=` |
| `arrowOperators` | `keyword.operator.arrow.precept` | `->`, `<-` |
| `memberNameKeywords` | `keyword.other.precept` | `countof`, `peekby` |

#### 2.26 Numbers

- **Key:** `numbers`
- **Type:** `match`
- **Scope:** `constant.numeric.precept`
- **Regex:** `\b\d+(?:\.\d+)?\b`

#### 2.27 Punctuation

- **Key:** `punctuation`
- **Type:** `match`
- **Scope:** `punctuation.precept`
- **Regex:** `[()[\].,]` (individual captures for finer scoping optional)

#### 2.28 Identifier Reference (Catch-All)

- **Key:** `identifierReference`
- **Type:** `match`
- **Scope:** `variable.other.precept`
- **Regex:** `\b[A-Za-z_][A-Za-z0-9_]*\b`
- **Priority:** LAST in pattern order. This is the catch-all.

### Spec Section 3: Top-Level Pattern Ordering

Ordered from most-specific to least-specific to prevent false matches.

```json
{
  "patterns": [
    { "include": "#comment" },
    { "include": "#messageStrings" },
    { "include": "#strings" },
    { "include": "#typedConstants" },
    { "include": "#preceptHeader" },
    { "include": "#stateDeclaration" },
    { "include": "#eventDeclaration" },
    { "include": "#fieldCollectionDeclaration" },
    { "include": "#fieldScalarDeclaration" },
    { "include": "#ruleDeclaration" },
    { "include": "#stateAction" },
    { "include": "#stateEnsure" },
    { "include": "#eventHandler" },
    { "include": "#eventEnsure" },
    { "include": "#accessMode" },
    { "include": "#omitDeclaration" },
    { "include": "#fromOnHeader" },
    { "include": "#noTransition" },
    { "include": "#transitionTarget" },
    { "include": "#functionCalls" },
    { "include": "#collectionMemberAccess" },
    { "include": "#eventArgReference" },
    { "include": "#arrowOperators" },
    { "include": "#symbolOperators" },
    { "include": "#logicalOperators" },
    { "include": "#membershipOperators" },
    { "include": "#stateModifiers" },
    { "include": "#constraintKeywords" },
    { "include": "#typeKeywords" },
    { "include": "#declarationKeywords" },
    { "include": "#controlKeywords" },
    { "include": "#actionKeywords" },
    { "include": "#outcomeKeywords" },
    { "include": "#accessModeKeywords" },
    { "include": "#quantifierKeywords" },
    { "include": "#memberNameKeywords" },
    { "include": "#booleanLiterals" },
    { "include": "#numbers" },
    { "include": "#punctuation" },
    { "include": "#identifierReference" }
  ]
}
```

**Ordering rationale:**
1. Comments first — `#` to end of line must be captured before anything else
2. Message strings before regular strings — `because "msg"` must get gold scope before `"msg"` gets consumed as a regular string
3. Typed constants — `'USD'` before identifiers
4. Construct-level patterns (most-specific) — declaration headers capture entire lines with contextual scoping
5. `stateAction` before `stateEnsure` — both start with `to`/`from`, disambiguated by `->` vs `ensure`
6. `eventHandler` before `eventEnsure` — both start with `on`, disambiguated by `->` vs `ensure`
7. `noTransition` before `transitionTarget` — `no transition` is a compound keyword
8. Dot-access patterns — `collectionMemberAccess` before `eventArgReference` to prevent `F.count` → event scope
9. `functionCalls` — before identifierReference catch-all
10. Operator patterns — arrows first (longest match), then symbol, then keyword operators
11. Keyword groups from catalog (most-specific scope to least-specific)
12. Literals and numbers
13. Catch-all identifier last

---

## 6. Coverage Gaps (Current Grammar)

### Gaps in the hand-authored grammar (35 items from audit section 3.1, G1–G35)

See Section 3.1 above for the complete gap table. Summary of critical gaps:

1. **35 missing language constructs/tokens** (G1–G35)
2. **10 stale/incorrect patterns** (S1–S10) — 3 retired keywords, 2 retired syntax forms, 5 scope misassignments
3. **16 scope assignment errors** (E1–E16) — tokens assigned to wrong semantic category

### Gaps in the grammar generator (20 items from audit section 4.2, GG1–GG20)

See Section 4.2 above. Summary of critical gaps:

1. **GG1: Missing message strings** — most critical for visual system compliance
2. **GG2: Stale event arg syntax** — uses `with` instead of parenthesized args
3. **GG3-GG13: 11 missing construct patterns** — rules, ensures, access modes, state actions, handlers, computed fields, function calls
4. **GG14: Stale `assert` keyword** — should be `ensure`
5. **GG17: Bad repo key naming** — confusing scope-suffixed names
6. **GG18: Broken `$ref`** — TextMate doesn't support JSON `$ref`

---

## 7. Generator Completion Requirements

Numbered list keyed to spec entries above.

### Must-Fix (blocks parity with hand-authored grammar + visual system compliance)

1. **Add `messageStrings` pattern (Spec §2.2).** This is the single most important pattern for visual system compliance. Without it, message payloads are indistinguishable from regular strings — destroying the gold visual interrupt that the brand mandates. Emit TWO match patterns: one for `because "..."`, one for `reject "..."`. Captures must assign `keyword.declaration.precept` to the keyword and `string.quoted.double.message.precept` to the string.

2. **Replace `eventWithArgsDeclaration` with parenthesized-arg syntax (Spec §2.7).** Current pattern (L218-258) matches `event Name with Arg as type`. Replace with pattern matching `event Name(Arg as type, ...)`. Remove the `with` keyword from the structural pattern.

3. **Expand `stateDeclaration` to recognize all 7 state modifiers (Spec §2.6).** Current pattern (L180-215) only matches `initial`. Add sub-patterns for `terminal`, `required`, `irreversible`, `success`, `warning`, `error` with scope `storage.modifier.state.precept`.

4. **Replace `assertStatement` with `eventEnsure` and `stateEnsure` (Spec §2.13-2.14).** Current pattern (L416-432) uses stale `assert`. Replace with two patterns: `on Event ensure Expr` and `in/to/from State ensure Expr`.

5. **Add `ruleDeclaration` pattern (Spec §2.19).** Match `rule` keyword at line start.

6. **Add `accessMode` pattern (Spec §2.15).** Match `in State modify Field editable/readonly`.

7. **Add `omitDeclaration` pattern (Spec §2.16).** Match `in State omit Field`.

8. **Add `stateAction` pattern (Spec §2.17).** Match `to/from State -> action chain`.

9. **Add `eventHandler` pattern (Spec §2.18).** Match `on Event -> action chain`.

10. **Add `noTransition` pattern (Spec §2.21).** Match `no transition` as a compound keyword.

11. **Add `functionCalls` pattern (Spec §2.24).** Match known function names followed by `(`. Derive function name list from `Functions.All` catalog.

12. **Fix `ScopeToRepositoryKey` naming (Spec §2.25).** Replace scope-suffixed keys with descriptive names. Current: `keyword.declaration.preceptKeywords`. Proposed: `declarationKeywords`.

13. **Fix broken `$ref` in `eventWithArgsDeclaration` (GG18).** L244 uses `["$ref"]` which TextMate doesn't support. Replace with `["include"]`.

14. **Expand `fieldScalarDeclaration` type list (Spec §2.8).** Current pattern (L322) lists `string|number|integer|decimal|boolean|choice`. Must include all 27 type keywords from catalog. Derive from `Tokens.All` where category is `Type`.

15. **Expand `fieldCollectionDeclaration` to include all collection types (Spec §2.9).** Current pattern (L293) includes `set|queue|stack|bag|list|log|lookup` ✓. Inner type list needs expansion to include `integer`, `decimal`.

16. **Update top-level pattern ordering (Spec §3).** Current ordering (L491-524) must be restructured per spec. `messageStrings` must come before `strings`. New construct patterns must be inserted at correct priority.

### Should-Fix (improves correctness and visual system alignment)

17. **Add `any` quantifier sub-pattern in state position.** In `fromOnHeader`, `stateEnsure`, `accessMode`, etc., `any` should get `keyword.other.quantifier.precept`, not `entity.name.type.state.precept`.

18. **Add `punctuation` patterns for parentheses and brackets (Spec §2.27).** Explicit patterns for `(`, `)`, `[`, `]` with `punctuation.precept`.

19. **Verify `rootEditDeclaration` validity (Spec §2.11).** If `edit` is not in `TokenKind` and no sample uses it, remove. If still valid, add to catalog first.

20. **Add `computedFieldDeclaration` context (Spec §2.10).** At minimum, the `<-` operator pattern is sufficient. Consider whether a dedicated structural pattern is needed.

21. **Handle `choice of string("a","b","c")` syntax.** The parenthesized choice options need string highlighting within the type declaration. Currently the strings would be captured by the generic `strings` pattern, which is acceptable.

### Won't-Fix in Grammar (semantic tokens only)

22. **Italic for constrained states/events.** TextMate cannot apply `fontStyle: italic` based on semantic context (whether a state participates in `ensure` rules). This requires the semantic token provider, which already exists in the language server.

23. **Context-dependent `set` scoping.** `set` as action verb vs collection type. TextMate can't disambiguate. Semantic tokens handle this.

24. **`null` removal.** `null` is not a keyword. The hand-authored grammar has it but the generator doesn't. No action needed — the generator is correct.

---

## Appendix A: Brand Doc Sync Items

The following items in `design/brand/brand-decisions.md` need updating to match catalog reality:

1. Replace `nullable` with `optional` in the Structure · Grammar keyword list
2. Remove `write` from the Structure · Semantic keyword list (retired B4)
3. Add `rule` to Structure · Semantic keyword list
4. Add `ensure` to Structure · Semantic keyword list
5. Add `writable` to Structure · Semantic keyword list (or Grammar — decision needed)
6. Add `optional` to Structure · Semantic keyword list (or Grammar — decision needed)
7. The brand doc's 2-tier keyword split (Semantic vs Grammar) doesn't map 1:1 to the catalog's 14-category scope model. Consider updating the brand doc to reference catalog categories or accept that the theme mediates between the two.

## Appendix B: Theme Configuration Requirements

For the visual system to work as designed, the VS Code theme must include rules mapping scopes to colors and styles:

```json
{
  "editor.tokenColorCustomizations": {
    "textMateRules": [
      { "scope": "keyword.declaration.precept", "settings": { "foreground": "#4338CA", "fontStyle": "bold" } },
      { "scope": "keyword.control.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.action.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.outcome.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.access-mode.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.quantifier.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.constraint.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.operator.logical.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.operator.membership.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.operator.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.operator.arrow.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "storage.type.precept", "settings": { "foreground": "#9AA8B5" } },
      { "scope": "storage.modifier.state.precept", "settings": { "foreground": "#9AA8B5" } },
      { "scope": "entity.name.type.state.precept", "settings": { "foreground": "#A898F5" } },
      { "scope": "entity.name.function.event.precept", "settings": { "foreground": "#30B8E8" } },
      { "scope": "entity.name.precept.message.precept", "settings": { "foreground": "#A898F5" } },
      { "scope": "variable.other.field.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "variable.parameter.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "variable.other.property.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "variable.other.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "support.function.precept", "settings": { "foreground": "#B0BEC5" } },
      { "scope": "constant.numeric.precept", "settings": { "foreground": "#84929F" } },
      { "scope": "constant.language.boolean.precept", "settings": { "foreground": "#84929F" } },
      { "scope": "string.quoted.double.precept", "settings": { "foreground": "#84929F" } },
      { "scope": "string.quoted.double.message.precept", "settings": { "foreground": "#FBBF24" } },
      { "scope": "string.quoted.single.precept", "settings": { "foreground": "#84929F" } },
      { "scope": "comment.line.number-sign.precept", "settings": { "foreground": "#9096A6", "fontStyle": "italic" } },
      { "scope": "punctuation.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "punctuation.separator.comma.precept", "settings": { "foreground": "#6366F1" } },
      { "scope": "punctuation.accessor.precept", "settings": { "foreground": "#6366F1" } }
    ]
  }
}
```

## Appendix C: Catalog-Driven Generation Principle

The grammar generator MUST derive all keyword lists, type names, function names, operator symbols, and constraint keywords from the catalog source of truth (`Tokens.All`, `Functions.All`, etc.). No hardcoded token sets in the generator. If a new keyword is added to the catalog, the generator's output must automatically include it without manual changes.

The generator's current approach (L38-77) is architecturally correct for catalog-derived keyword patterns. The structural patterns (construct-level) are hand-written in the generator but MUST reference catalog-derived keyword lists where they enumerate token alternatives (e.g., type names in field declarations, state modifier names in state declarations).

---

*End of specification.*
