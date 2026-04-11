# Precept Language Design

Date: 2026-03-05

This document captures the surface syntax and semantics for the Precept DSL — a parser/tooling-friendly grammar that does **not** rely on indentation/offside rules. The language described here is fully implemented and is the current DSL.

The current surface was shaped in part around the March 2026 move to a Superpower-based parser. Flat, keyword-led statements and whitespace-insensitive structure were chosen deliberately so the grammar maps cleanly to a token-stream/combinator parser and the tooling built on the same token stream, without indentation-sensitive or end-marker-heavy workaround logic.

Legacy documentation (`docs/archive/DesignNotes-legacy.md`, `docs/archive/README-legacy.md`) describes the earlier regex/imperative parser and is archived.

---

## Goals

- **No indentation-based structure**: blocks/attachments must be explicit and line-oriented. This is a language-design choice, not just a formatting preference: the grammar is intentionally compatible with a straightforward Superpower parser without offside-rule handling.
- **Tooling-friendly**: keyword-anchored statements, deterministic parse, predictable IntelliSense.
- **Keyword-anchored flat statements**: every statement begins with a recognizable keyword — no section headers, no indentation. The parser and language server rely on keyword anchoring alone.
- **Explicit nullability**: use `nullable` rather than punctuation-based null markers.
- **Compile-time-first semantics**: catch authoring mistakes as early as possible. Type, scope, nullability, and structural workflow errors should fail at compile time whenever the compiler can prove them soundly.

## Design Philosophy

These principles have driven every syntax and semantics decision:

1. **Deterministic, inspectable model.** Fire/inspect always produces the same result for the same inputs. All validation evaluates against the "proposed world" (post-mutation, pre-commit). No hidden state, no side effects.

2. **English-ish but not English.** Keywords like `with`, `because`, `from`, `on` read naturally but don't attempt full sentences. The language should be learnable by reading examples, not by studying a grammar.

3. **Minimal ceremony.** No colons, no curly braces, no semicolons. `because` is the sentinel between expressions and reasons. Keyword anchoring replaces punctuation for structure.

4. **Locality of reference.** Rules live near the things they describe — invariants near fields, state asserts near states, event asserts near events. Cross-cutting auditing is a tooling concern, not a syntax concern.

5. **Data truth vs movement truth.** `invariant` = static data constraints (always hold). `assert` = movement constraints (checked when something happens — an event fires, a state is entered or exited). The keyword tells you the category.

6. **Collect-all for validation, first-match for routing.** Validation (invariants, asserts) reports every failure. Transition rows are evaluated top-to-bottom, first match wins. These are different problems with different evaluation strategies.

7. **Self-contained rows.** Each transition row is independently readable. No shared context with sibling rows — if a mutation appears in two rows, it's explicit in both.

8. **Sound, compile-time-first static analysis.** Compile-time checking is a product feature, not an implementation detail. The DSL should reject real semantic mistakes early, but never guess. If the checker can't prove a contradiction, it assumes satisfiable. Exotic or data-dependent cases get a pass; the inspector catches the rest via simulation.

9. **Tooling drives syntax.** IntelliSense, diagnostics, and preview are first-class design constraints. Keyword anchoring enables predictable suggestions. The language server uses semantic ordering to prioritize completions based on what has already been declared. The grammar is also intentionally kept friendly to the Superpower token/combinator parser that underpins the runtime and tooling: syntax that would require indentation-aware parsing or workaround-heavy block balancing is rejected even if it is more concise.

10. **Consistent prepositions.** `from`, `to`, `in`, `on` carry the same meaning everywhere they appear. `from` = leaving a state. `to` = entering a state. `in` = while in a state. `on` = when an event fires. The token after the identifier (`assert`, `->`, `on`, body keywords) determines the kind of statement — the preposition's meaning never changes.

11. **`->` means "do something."** The arrow introduces an action — a mutation, an outcome, or a side effect. It separates the *context* (what state, what event, what condition) from the *action* (what to do about it). Sequential execution, read-your-writes. Uniform across transition rows and state entry/exit actions.

12. **AI is a first-class consumer.** The DSL is readable by humans and writable by hand, but its properties are chosen to make AI authoring reliable. Deterministic semantics (no hidden state, no side effects) mean an AI can reason about outcomes. Keyword-anchored flat statements mean an AI can parse and generate without tracking indentation context. Structured tool APIs (MCP) return typed JSON that an AI can validate, audit, and iterate against without human feedback. The language reference itself is queryable as data (`precept_language`), so the AI never relies on training-data recall for syntax. The intended workflow: a domain expert describes intent, the AI authors the precept, and the toolchain closes the correctness loop.

13. **Keywords for domain, symbols for math.** Precept uses English keywords for domain concepts, structural anchors, actions, and logical operators (`and`, `or`, `not`). Symbols are reserved for universal mathematical notation (`+`, `-`, `==`, `!=`) and the one structural exception `->`. This line is drawn deliberately — see the [Keyword vs Symbol Design Framework](#keyword-vs-symbol-design-framework-locked).

## Non-Goals

- Perfect parity with the current DSL syntax.
- Adding new runtime features beyond syntax/structure changes (unless explicitly called out as “New”).

---
## Preposition Convention (Locked)

Four prepositions are used throughout the language to dereference named entities. Each preposition carries the same meaning regardless of which section it appears in:

| Preposition | Meaning | Dereferences | Used in |
|---|---|---|---|
| `in` | While residing in a state | State name | `in <State> assert ...`, `in <State> edit ...` |
| `to` | Crossing into a state | State name | `to <State> assert ...`, `to <State> -> ...` |
| `from` | Crossing out of a state | State name | `from <State> assert ...`, `from <State> -> ...`, `from <State> on ...` |
| `on` | When an event fires | Event name | `on <Event> assert ...`, `from ... on <Event>` |

**Design principle:** The preposition always means the same thing — `from` always means "leaving this state," `on` always means "when this event fires." What follows the preposition+identifier determines the *kind* of statement:

- `from Draft assert ...` → exit gate (assert context)
- `from Draft on SubmitOrder` → transition routing (transition context)
- `on SubmitOrder assert ...` → event arg validation (assert context)
- `in Open edit Notes, Priority` → editable field declaration (edit context)

**Disambiguation:** The token after the identifier resolves any ambiguity:
- `assert` → constraint/validation statement
- `edit` → editable field declaration (continues with field name list)
- `on` → transition header (continues with event name)
- Transition body keywords (`set`, `transition`, `if`, etc.) → transition body

This is unambiguous at the token level — no lookahead beyond the keyword following the identifier is needed.

**Multi-target shorthand:** State prepositions (`in`, `to`, `from`) accept comma-separated state names or `any` (expands to all declared states). Event prepositions (`on`) do **not** — event asserts are arg-scoped, and different events have different arg shapes.

```precept
# Comma-separated — one assert per listed state internally
in Open, InProgress, Blocked assert Assignee != null because "Must have an assignee"

# any — expands to all declared states
from any assert AuditLog.count > 0 because "Must have audit trail to leave any state"
```

---
## Keyword vs Symbol Design Framework (Locked)

Precept sits between Python and SQL on the keyword-symbol spectrum — keyword-dominant for structure and domain concepts, symbolic only for universal mathematical notation. This is not an inherited default from a host language; it is a deliberate position grounded in Precept's multi-audience design.

### The spectrum

```
APL → Haskell → C → Rust → C# → TypeScript → Python → PRECEPT → SQL → COBOL → Gherkin
                          ←— more symbolic              more keyword —→
```

More keyword-dominant than C#/TypeScript, but without the verbosity of COBOL or Gherkin. Principle #3 (minimal ceremony) prevents keyword bloat — `and` is 3 characters, not `PERFORM VARYING`.

### Where the line is drawn

**Structure and domain concepts use keywords.** Precept's architecture depends on keyword anchoring — the Superpower parser, IntelliSense completions, and semantic tokens all use keywords as parse points. Every statement starts with a recognizable keyword (`field`, `state`, `from`, `on`, `set`, `transition`). Domain-semantic operators like `contains` are keywords, not symbols. Every comparable external DSL with non-programmer audiences (Cedar, Drools, Gherkin) draws this same line.

**Mathematical and comparison operators use symbols.** Arithmetic (`+`, `-`, `*`, `/`, `%`) and comparisons (`==`, `!=`, `>`, `<`, `>=`, `<=`) are universal mathematical notation. These symbols are learned once and recognized instantly by all audiences. Nobody benefits from spelling out `plus` or `greater than or equal to`.

**Logical operators use keywords.** `and`, `or`, `not` — not `&&`, `||`, `!`. This is the one category where languages fundamentally disagree (Python and SQL use keywords; C# and Java use symbols), and Precept sides with keywords for three reasons:

1. **Audience.** Precept's primary readers include domain experts validating business rules. `when not IsPremium and Score >= 680` reads as English that a business analyst can verify. `when !IsPremium && Score >= 680` reads as code that requires developer training.
2. **Internal consistency.** `contains` is already a keyword operator. `and`, `or`, `not` as keywords unify all domain-semantic operators under one form.
3. **The asymmetry with `!=` is natural, not problematic.** Unary negation (`not X`) and binary comparison (`X != Y`) are cognitively distinct — different operations, different arity, different linguistic roles. Every keyword-for-logic system (SQL, Python, Alloy, DMN) retains `!=` as a symbol without creating pressure to replace it. 50+ years of SQL and 33+ years of Python confirm: keyword `not` coexists with symbolic `!=` without confusion. Languages that went fully symbolic (APL) excluded non-programmer audiences. Languages that went fully keyword (COBOL) created verbosity. The keyword-for-logic, symbol-for-math split is the proven balance.

**`->` is the exception — a structural symbol.** The arrow is the one structural symbol in Precept. It is justified by Principle #11: `→` is universal notation in state machines and formal logic, it creates clear visual separation between context and action, and every keyword alternative is weaker (`then` is taken by `if...then...else`; `do` sounds imperative, not declarative). The arrow's visual distinctness — a sigil that the eye tracks differently from keywords — makes it a stronger separator than any word.

### Decision matrix for new syntax

| Category | Rule | Examples |
|----------|------|----------|
| **Structural anchors** (start-of-statement, delimiters) | ALWAYS keywords | `field`, `state`, `from`, `on`, `set`, `transition` |
| **Domain concepts** (ubiquitous language) | ALWAYS keywords | `invariant`, `assert`, `because`, `contains` |
| **Logical operators** (boolean connectives) | Keywords | `and`, `or`, `not` |
| **Math/comparison operators** | Symbols | `+`, `-`, `*`, `/`, `%`, `==`, `!=`, `>`, `<`, `>=`, `<=` |
| **Structural separators** | Symbols only if universally understood | `->` (state machine notation), `.` (member access) |
| **Punctuation/grouping** | Symbols | `(`, `)`, `[`, `]`, `,`, `"`, `#` |

**Tiebreaker:** Default to keyword. Symbols require memorized association; keywords are self-documenting. Only use a symbol when universal notation makes it more recognizable than any keyword alternative.

### Current balance

| Category | Count | Form |
|----------|-------|------|
| Keywords | ~50 | Structure, domain concepts, actions, types, logical operators |
| Symbols | ~23 | Math, comparison, assignment, `->`, punctuation |
| Ratio | ~2 : 1 | Keyword-dominant by design |

### Logical operator migration (Implemented — [#31](https://github.com/sfalik/Precept/issues/31))

| Symbolic form | Keyword form | Status |
|--------------|-------------|--------|
| `!` (unary NOT) | `not` | Implemented |
| `&&` (logical AND) | `and` | Implemented |
| `\\|\\|` (logical OR) | `or` | Implemented |
| `!=` (inequality) | `!=` (no change) | Stays as-is |

The runtime accepts `not`, `and`, `or` as keywords. `!`, `&&`, `||` have been removed.

---
## File Structure (Locked)

A `.precept` file is a flat sequence of keyword-anchored statements:

- `precept <Name>` header (required, first non-comment line)
- Then any number of statements in any order: `field`, `invariant`, `state`, `in`/`to`/`from` (asserts and actions), `event`, `on` (event asserts), `from ... on` (transition rows)
- No section headers — the leading keyword determines statement kind

### Recommended ordering

While the parser accepts statements in any order, the recommended convention is:

1. Fields and invariants
2. States, state asserts, and state entry/exit actions
3. Events and event asserts
4. Transition rows

The language server uses this ordering for IntelliSense: it prioritizes completions for constructs that typically appear next based on what has already been declared (e.g., after fields are defined, `state` and state-related keywords are suggested first). All statement-starting keywords are valid at any point, but the LS re-ranks them based on semantic context.

### Comments and whitespace

- Lines starting with `#` are comments.
- Blank lines are ignored.
- Inline comments are allowed after a statement: `#` followed by any text to end-of-line. The tokenizer strips comments before parsing.

---

## Grammar (Informal; Locked Parts)

This is an **informal EBNF-ish** grammar meant to make the current decisions concrete for parser/tooling work. It only covers the parts we have locked so far.

```
PreceptFile        := PreceptHeader Statement*

PreceptHeader      := "precept" Identifier

Statement          := FieldDecl | Invariant | StateDecl | StateAssert | StateAction
                    | EditDecl | EventDecl | EventAssert | TransitionRow
                    | Comment | Blank

FieldDecl          := "field" Identifier ("," Identifier)* "as" TypeRef NullableOpt DefaultOpt
NullableOpt        := ("nullable")?
DefaultOpt         := ("default" LiteralOrList)?

Invariant          := "invariant" BoolExpr "because" StringLiteral

StateDecl          := "state" StateNameEntry ("," StateNameEntry)*
StateNameEntry     := Identifier InitialOpt
InitialOpt         := ("initial")?

StateAssert        := StateInAssert | StateToAssert | StateFromAssert
StateInAssert      := "in" StateTarget "assert" BoolExpr "because" StringLiteral
StateToAssert      := "to" StateTarget "assert" BoolExpr "because" StringLiteral
StateFromAssert    := "from" StateTarget "assert" BoolExpr "because" StringLiteral
StateTarget        := "any" | Identifier ("," Identifier)*

StateAction        := StateToAction | StateFromAction
StateToAction      := "to" StateTarget ActionChain
StateFromAction    := "from" StateTarget ActionChain
ActionChain        := ("->" Action)+
Action             := SetAction | CollectionAction
SetAction          := "set" Identifier "=" Expr
CollectionAction   := AddAction | RemoveAction | EnqueueAction | DequeueAction
                    | PushAction | PopAction | ClearAction
AddAction          := "add" Identifier Expr
RemoveAction       := "remove" Identifier Expr
EnqueueAction      := "enqueue" Identifier Expr
DequeueAction      := "dequeue" Identifier ("into" Identifier)?
PushAction         := "push" Identifier Expr
PopAction          := "pop" Identifier ("into" Identifier)?
ClearAction        := "clear" Identifier

EditDecl           := StateEditDecl | RootEditDecl
StateEditDecl      := "in" StateTarget "edit" FieldTarget
RootEditDecl       := "edit" FieldTarget
FieldTarget        := "all" | Identifier ("," Identifier)*

EventDecl          := "event" Identifier ("," Identifier)* ("with" ArgList)?
ArgList            := ArgDecl ("," ArgDecl)*
ArgDecl            := Identifier "as" TypeRef NullableOpt DefaultOpt

EventAssert        := "on" Identifier "assert" BoolExpr "because" StringLiteral

TransitionRow      := "from" StateTarget "on" Identifier WhenOpt ActionChain? "->" Outcome
WhenOpt            := ("when" BoolExpr)?
Outcome            := TransitionOutcome | NoTransition | RejectOutcome
TransitionOutcome  := "transition" Identifier
NoTransition       := "no" "transition"
RejectOutcome      := "reject" StringLiteral

TypeRef            := ScalarType | CollectionType
ScalarType         := "string" | "number" | "boolean"
CollectionType     := ("set" | "queue" | "stack") "of" ScalarType

LiteralOrList      := ScalarLiteral | ListLiteral
ScalarLiteral      := NumberLiteral | BooleanLiteral | StringLiteral | NullLiteral
ListLiteral        := "[" (ScalarLiteral ("," ScalarLiteral)*)? "]"

BoolExpr           := <expression grammar; carried forward>
Identifier         := <token>
StringLiteral      := '"' ... '"'
Comment            := "#" ... end-of-line
Blank              := (whitespace only)
```

Notes:
- `Invariant` = data truth. Always holds, checked post-commit. May reference any declared fields.
- `StateInAssert` = state-scoped field invariant. Checked post-commit on any transition where the resulting state is the named state (entry **and** NoTransition).
- `StateToAssert` = entry gate. Checked post-commit only when crossing **into** the named state from a different state.
- `StateFromAssert` = exit gate. Checked post-commit only when crossing **out of** the named state to a different state.
- All three state assert forms evaluate against the **proposed world** (post-mutation, pre-commit). Fields-scoped.
- `EventAssert` = movement truth. Checked pre-transition when the named event fires. Scoped to that event's args only.
- Whether `EventAssert` must appear after its `EventDecl` is not locked yet; recommended to keep asserts near the related event for readability.
- **Compile-time error:** duplicate `in` + `to` on the same state with the same expression (syntactic identity after whitespace normalization). `in` already subsumes `to`.
- **Compile-time error:** duplicate assert — same preposition + same state + same expression appearing more than once.
- **Compile-time error:** `in` on the initial state where default field values violate the expression. Both defaults and initial state are statically known.
- **Compile-time error (contradiction):** multiple asserts with the same preposition on the same state whose conjoined per-field domains are empty (e.g. two `in Open` asserts that require contradictory values for the same field).
- **Compile-time error (deadlock):** `in`/`to` vs `from` asserts on the same state whose conjoined per-field domains are empty — the state is provably unexitable.
- All domain checks use interval/set analysis on the expression AST. Expressions involving `contains` or cross-field relationships that cannot be reduced to per-field domains are assumed satisfiable (no false positives).
- **Stateless precept** — a precept with no `state` declarations. Only `field`, `invariant`, and root-level `edit` declarations are valid. C12 requires at least one `field` or `state`. C55 rejects root-level `edit` when states are declared. C49 warns per event declared in a stateless precept (events have no transition surface).

---

## Stateless Precepts (Locked)

A precept without any `state` declarations is a **stateless precept** — it represents a domain object governed by data rules and editability constraints, but without a lifecycle or routing surface.

### When to use stateless precepts

Use a stateless precept when the entity has fields and integrity rules but no meaningful lifecycle phases. Common cases: configuration objects, profile records, pricing tables, stored payment methods, and any object where "what the data must be" matters but "what stage the process is in" does not.

### Syntax

A stateless precept omits all `state`, `event`, and `from ... on ...` declarations. Valid declarations are:

- `field` — scalar and collection field declarations
- `invariant` — data integrity rules (always enforced)
- `edit` (root-level) — which fields are directly editable; see [Root-level editability](#root-level-editability-stateless-precepts)

```precept
precept CustomerProfile

field Name as string default ""
field Email as string default ""
field Phone as string nullable
field PreferredContactMethod as string default "email"
field MarketingOptIn as boolean default false

invariant Name != "" because "Name cannot be empty"

edit all
```

### Constraints

- **C12 (parse):** At least one `field` or `state` must be declared. A `precept` header alone is not valid.
- **C55 (compile):** Root-level `edit` is not valid when states are declared. Use `in <State> edit` instead.
- **C49 (compile, Warning):** Event declared in a stateless precept is unreachable — no transition surface exists.
- C13 ("Exactly one state must be initial") is suppressed for stateless precepts — no states means no initial state required.

### Runtime behavior

- `PreceptEngine.IsStateless` is `true`; `InitialState` is `null`.
- `CreateInstance(data?)` — works for both stateful and stateless. For stateless, creates an instance with `CurrentState = null`.
- `CreateInstance(state, data?)` — throws `ArgumentException` for stateless precepts.
- `Fire` on a stateless instance — returns `Undefined` outcome. Events have no transition surface.
- `Inspect(instance, event)` on stateless — returns `Undefined` outcome.
- `Inspect(instance)` on stateless — returns all events as `Undefined`; `EditableFields` reflects root-editable fields.
- `Update` on stateless — applies to root-editable fields; `CurrentState` is `null`.

---

## `when` Preconditions (Locked)

`WhenOpt` in the grammar is an optional precondition on a `from ... on ...` row header:

```
from <State|any> on <Event> when <Guard> -> ...
```

### Semantics

`when` expresses **conditional availability** — whether the row is applicable to the current instance at all. This is distinct from rejection, which is an explicit outcome for an applicable row.

- `when` == `false` → the row is skipped entirely. No mutations, no rejection message. The outcome is `Unmatched`, which is distinct from `Rejected`.
- `when` == `true` → the row body is entered and actions execute.

**`when` is the only conditional routing mechanism in the DSL.** Precept uses flat rows with `when` guards and first-match evaluation instead of nested branching. When multiple outcomes are needed for the same `(State, Event)` pair, write multiple rows — each with its own `when` guard — and let first-match routing select the appropriate one.

Use `when` when the intent is "this action isn't meaningful right now." Use `reject` in a catch-all row (without `when`) when the intent is "this action was attempted but the conditions for success were not met."

### Authoring patterns

**Boolean flag — event only available under a condition:**

```precept
field IsVip as boolean default false
field DiscountCode as string nullable

from Active on ApplyDiscount when IsVip
  -> set DiscountCode = ApplyDiscount.Code
  -> no transition
```

When `IsVip` is `false`, `ApplyDiscount` is `Unmatched`. The branches never evaluate.

**Structural ceiling — event exhausts its allowed uses:**

```precept
field ReopenCount as number default 0
  invariant ReopenCount >= 0 because "Reopen count cannot be negative"

from Resolved on Reopen when ReopenCount < 3
  -> set ReopenCount = ReopenCount + 1
  -> transition Reopened
```

Once `ReopenCount` reaches 3, `Reopen` becomes `Unmatched` for this instance — the option structurally ceases to exist, rather than being visible but rejectable.

**Nullable precondition — nullable narrowing inside the row body:**

When the `when` expression constrains a nullable field (e.g. `when OfficerName != null`), the language server narrows that field to non-nullable inside all statements of the row body. No redundant null check needed inside.

### Scope restriction

`when` expressions may only reference declared instance data fields and their properties. Event argument references (`EventName.ArgName`) in a `when` expression are a parse error. Arguments are not yet available when availability is evaluated — use additional rows with argument-dependent `when` guards for argument-dependent routing.

### Multiple rows for the same state+event

When multiple rows target the same `(State, Event)` pair, rows are evaluated in declaration order. Each row's `when` guard is checked independently. The first row whose `when` evaluates to `true` (or which has no `when`) is entered. If all rows have `when` guards that evaluate to `false`, the outcome is `Unmatched`.

---

## Identifiers, Keywords, and Strings

- Identifiers are case-sensitive and compared ordinally.
- String literals use double quotes: `"like this"`.
- Reasons on `on ... assert`/`invariant` are **required** and must be quoted strings.

### Reserved keywords (Locked)

Keywords are **strictly lowercase**. Identifiers are case-sensitive. `From` and `from` are different tokens — `From` is a valid identifier, `from` is not.

Full reserved keyword list:

`precept`, `field`, `as`, `nullable`, `default`, `invariant`, `because`,
`state`, `initial`, `event`, `with`, `assert`, `edit`,
`in`, `to`, `from`, `on`, `when`, `any`, `all`, `of`,
`set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `into`,
`transition`, `no`, `reject`,
`string`, `number`, `boolean`, `true`, `false`, `null`, `contains`,
`and`, `or`, `not`

### Dual-use: `set`

`set` is both a collection type name (`set of string`) and a mutation action keyword (`set Field = expr`). These are context-disambiguated at LL(1):
- After `as` → type (`field Tags as set of string`)
- After `->` → action (`-> set Balance = 0`)

No ambiguity in the token stream — the parser knows which meaning applies from the preceding keyword.

---

## Types (Locked)

### Scalar types

- `string`
- `number`
- `boolean`

### Collection types

Collections are declared using “English-ish” forms, not angle-bracket generics:

- `set of <Scalar>`
- `queue of <Scalar>`
- `stack of <Scalar>`

Constraints:
- Inner types are scalar only.
- No nullable inner types.
- No nested collections.

### Collection semantics

Collections are part of the DSL's deterministic behavior contract, not just a storage detail.

- `set of <Scalar>` is sorted ascending and unique.
- `queue of <Scalar>` is FIFO and preserves insertion order.
- `stack of <Scalar>` is LIFO and preserves insertion order.
- Collections default to empty when no explicit default is supplied.

Mutation statements are explicit actions, not expressions:

| Statement | Valid on | Behavior |
|---|---|---|
| `add <Collection> <Expr>` | `set of <Scalar>` | Inserts the value; duplicate adds are no-ops. |
| `remove <Collection> <Expr>` | `set of <Scalar>` | Removes by value; removing a missing value is a no-op. |
| `enqueue <Collection> <Expr>` | `queue of <Scalar>` | Appends to the back of the queue. |
| `dequeue <Collection>` | `queue of <Scalar>` | Removes the front element; fails if empty. |
| `dequeue <Collection> into <Field>` | `queue of <Scalar>` | Copies the front element into a scalar field, then removes it; fails if empty. |
| `push <Collection> <Expr>` | `stack of <Scalar>` | Pushes onto the top of the stack. |
| `pop <Collection>` | `stack of <Scalar>` | Removes the top element; fails if empty. |
| `pop <Collection> into <Field>` | `stack of <Scalar>` | Copies the top element into a scalar field, then removes it; fails if empty. |
| `clear <Collection>` | All collection kinds | Removes all elements; clearing an empty collection is a no-op. |

Read operations use the existing expression grammar:

- `<Collection>.count` is valid for all collection kinds.
- `<Collection> contains <Expr>` is valid for all collection kinds.
- `<Collection>.min` and `<Collection>.max` are valid only on `set of <Scalar>`.
- `<Collection>.peek` is valid only on `queue of <Scalar>` and `stack of <Scalar>`.
- Element-returning reads (`.min`, `.max`, `.peek`) fail on empty collections.

Design rule: writes are lenient where they can be safely idempotent; reads that require an element are strict. This keeps authoring concise without hiding missing-data failures.

`dequeue ... into ...` and `pop ... into ...` are statement-only forms. They are not valid inside expressions or `set` right-hand sides.

---

### String accessors

`string` fields expose a single parameterless accessor:

- `<Field>.length` — returns the **UTF-16 code unit count** of the string value as `number`. This matches .NET's `string.Length` and is O(1). Note: characters outside the Basic Multilingual Plane (e.g. emoji) count as 2 code units. Example: `"💀".length == 2`.

**Scope:** valid in `invariant`, `in`/`to`/`from` state assert, `when` guard, and `set` RHS — the same scopes as collection accessors.

**Null handling:** `.length` does not coerce `null` to `0`. Using `.length` on a nullable `string` field without first narrowing it to non-null is a type error (diagnostic `C56`). Null-check before access using one of:

```
# Non-null minimum length check (invariant)
invariant Name.length >= 2 because "Names require at least 2 characters"

# Nullable max length (field may be null or short)
invariant Note == null or Note.length <= 500 because "Notes cannot exceed 500 characters"

# Post-narrowing in a when guard (AccessReason narrowed to non-null by prior condition)
from Draft on Submit when EmployeeName != null and AccessReason != null and AccessReason.length >= 5 -> transition Submitted
```

---

## Fields (Mostly locked)

### Field declarations

Forms:

- `field <Name> as <Type> [nullable] [default <Literal>]`
- `field <Name>, <Name>, ... as <Type> [nullable] [default <Literal>]`

Multi-name declarations declare multiple fields sharing the same type, nullability, and default value. The type, `nullable`, and `default` clauses apply uniformly to every name in the list.

Defaults:
- Non-nullable scalar fields must declare a `default ...`.
- Nullable scalar fields default to `null` when `default ...` is omitted.
- Collection fields default to empty when `default ...` is omitted.
- Collection defaults may be expressed using a list literal, e.g. `default ["a", "b"]` (exact literal rules TBD).

Constraints:
- No duplicate field names (within a single declaration or across declarations).

Examples:

- `field Balance as number default 0`
- `field Email as string nullable`
- `field Tags as set of string default ["priority", "vip"]`
- `field MinAmount, MaxAmount as number default 0`
- `field FirstName, LastName as string nullable`

Focused example:

```precept
field Balance as number default 0
field Email as string nullable
field Tags as set of string default ["priority", "vip"]

# Multi-name form — shared type and default
field MinAmount, MaxAmount as number default 0
field FirstName, LastName, MiddleName as string nullable
```

### Field invariants (Locked)

Data integrity rules live adjacent to fields.

Form:

- `invariant <BoolExpr> because "<Reason>"`

Notes:
- Evaluated after a successful transition/mutation commit; if false, the event is rejected with the given reason.
- Scope: `<BoolExpr>` may reference any declared field names (including collection fields, via their accessors like `.count`).

Example:

- `invariant Balance >= 0 because "Balance cannot go negative"`
- `invariant MaxAmount >= MinAmount because "MaxAmount must be >= MinAmount"`

Focused example:

```precept
field Balance as number default 0
invariant Balance >= 0 because "Balance cannot go negative"

field MinAmount as number default 0
field MaxAmount as number default 100
invariant MaxAmount >= MinAmount because "MaxAmount must be >= MinAmount"
```

### Nullability and narrowing (Locked)

Appending `nullable` to a field or event argument declaration makes it nullable. The language server enforces that nullable values are never used directly in comparisons, arithmetic, string concatenation, or assignments that require a concrete type — the offending expression is flagged as a compile-time diagnostic. This prevents null from silently propagating to runtime.

Authors must prove non-null before use. There are three standard patterns.

**Pattern 1 — Inline `and` (test and use in the same guard):**

```precept
from Active on Evaluate
  -> if Score != null and Score >= 80
       -> set RiskTier = "Low"
       -> transition Approved
     else
       -> reject "Score unavailable or below threshold"
```

The right-hand side of `and` is only reached when the left side is `true`. The language server narrows `Score` to non-nullable for `Score >= 80` and for any `set` expressions in that branch. Removing the `Score != null and` prefix causes a diagnostic on `Score >= 80`.

**Pattern 2 — Inline `or` (short-circuit the null case):**

```precept
from Active on Retry
  -> if RetryCount == null or RetryCount > 0
       -> transition Retry
     else
       -> reject "Retry limit reached"
```

The right-hand side of `or` is only reached when the left side is `false` (i.e. `RetryCount != null`). The language server narrows `RetryCount` to non-nullable for the `RetryCount > 0` comparison.

**Pattern 3 — Early-exit null rejection, then use freely across all following branches:**

```precept
from Active on Retry
  -> if RetryCount == null
       -> reject "RetryCount unavailable"
     else if RetryCount > 0
       -> set Attempts = RetryCount
       -> transition Active
     else
       -> reject "No retries remaining"
```

After the first branch rejects on `null`, the language server knows every subsequent `else if` and `else` is only reachable when `RetryCount` is non-null. Using `RetryCount` in `RetryCount > 0` or in `set Attempts = RetryCount` requires no additional null check. Forgetting the early-exit and going straight to `RetryCount > 0` produces the same diagnostic as Pattern 1 — the squiggle is the same signal either way.

**`when` narrowing:** A `when` expression also narrows. `from Active on Fire when OfficerName != null` makes `OfficerName` non-nullable inside the entire row body. See the `when` Preconditions section.

**Rules:** Rules inherit the same strict null model. A nullable field used in a rule expression without an explicit null guard is a compile-time error in the rule. See [docs/RulesDesign.md](RulesDesign.md#Nullable-Behaviour-in-Rules) for rule-specific examples.


---

## States (Locked)

### State declarations

Forms:

- `state <Name> [initial]`
- `state <Name> [initial], <Name> [initial], ...`

Multi-name declarations declare multiple states on a single line, separated by commas. Each name may optionally be followed by `initial`. The comma-separated form communicates the intended workflow progression — left to right reads as the expected lifecycle sequence.

Constraints:
- Exactly one state across the entire precept must be marked `initial`. The `initial` keyword may appear after any name in the list.
- No duplicate state names (within a single declaration or across declarations).

Examples:

```precept
# Single-name form (unchanged)
state Draft initial

# Multi-name form — workflow progression reads left to right
state UnderReview, Approved, Funded, Declined

# initial can appear on any name in the list
state Draft, UnderReview initial, Approved, Funded, Declined

# Mixing single-name and multi-name in the same precept is valid
state Draft initial
state UnderReview, Approved, Funded, Declined
```

### State asserts (Locked)

State asserts are checked post-mutation, pre-commit. They may reference any declared fields. All three forms evaluate against the **proposed world** (after mutations apply but before commit finalizes).

Three forms, each with a distinct temporal scope:

| Preposition | Meaning | When checked |
|---|---|---|
| `in <State>` | State-scoped field invariant | After **any** transition resulting in `<State>` (entry **and** NoTransition) |
| `to <State>` | Entry gate | Only when crossing **into** `<State>` from a different state |
| `from <State>` | Exit gate | Only when crossing **out of** `<State>` to a different state |

Forms:

- `in <StateTarget> assert <BoolExpr> because "<Reason>"`
- `to <StateTarget> assert <BoolExpr> because "<Reason>"`
- `from <StateTarget> assert <BoolExpr> because "<Reason>"`

Where `<StateTarget>` is one of:
- A single state name: `Open`
- Comma-separated state names: `Open, InProgress, Blocked`
- The keyword `any` (expands to all declared states at parse time)

Multi-state and `any` are syntactic sugar — the parser expands them into one assert per state internally. Compile-time checks (subsumption, duplication, domain analysis) apply per-state after expansion.

Semantics:
- `invariant` = static data truth (always holds)
- `assert` = movement truth (something is happening — an event fires, a state is entered, or a state is exited)
- `in <State>` is strictly stronger than `to <State>`: if data must hold while *in* the state, you can never enter violating it either
- `to <State>` is entry-only: a condition must hold on arrival but may change later via in-place mutations
- `from <State>` is exit-only: you can't leave until the condition is met
- If false, the transition is **rolled back** and the outcome is `Rejected` with the provided reason

Compile-time checks (Locked):

| # | Check | Condition | Severity |
|---|---|---|---|
| 1 | `in` + `to` subsumption | Same state, same expression (syntactic identity) | Error |
| 2 | Duplicate assert | Same preposition + state + expression | Error |
| 3 | `in` on initial state vs defaults | Default field values violate `in <InitialState>` expression | Error |
| 4 | Same-preposition contradiction | Multiple asserts with same preposition on same state, conjoined per-field domains are empty | Error |
| 5 | Cross-preposition deadlock | `in`/`to` vs `from` on same state, conjoined per-field domains are empty → unexitable | Error |

All domain checks (#3–#5) use per-field interval/set analysis on the expression AST. Expressions involving `contains` or cross-field relationships that cannot be reduced to per-field domains are assumed satisfiable (sound — no false positives).

Focused example:

```precept
state Triage initial
state Open
state InProgress
state Blocked
state Review
state Resolved
state Closed

# Exit gates — can't leave until conditions are met
from Draft assert Email != null because "Must provide email before leaving draft"
from Review assert ApproverCount > 0 because "Need at least one approval to leave review"

# Entry gates — must hold on arrival, can change later
to Escalated assert Priority == "High" because "Must be high priority to escalate"

# State-scoped invariants — must hold while in the state (entry + in-place)
in Open, InProgress, Blocked assert Assignee != null because "Must have an assignee"
in Blocked assert BlockReason != null because "Blocked requires a block reason"
in Resolved assert Resolution != null because "Resolved requires a resolution"

# Entry/exit actions (New) — mutations that fire automatically on state change
to Open -> set SubmittedCount = SubmittedCount + 1
from Draft -> set IsDraft = false
```

### State entry/exit actions (Locked)

State actions are automatic mutations that fire when entering or leaving a state. They use the `->` pipeline and may reference any declared fields (no event args — they run regardless of which event caused the transition).

Forms:

- `to <StateTarget> -> <ActionChain>` — entry action, fires when crossing **into** the state
- `from <StateTarget> -> <ActionChain>` — exit action, fires when crossing **out of** the state

Constraints:
- No `in <State> ->` actions (in-place mutations on every event would be surprising and dangerous)
- No outcomes (`transition`, `no transition`, `reject`) — actions are mutations only
- Multi-state and `any` supported (same as asserts)
- Fields-scoped only (no event args available)

Execution order:
1. Exit actions (`from <SourceState> ->`) run first
2. Transition row mutations (`->` chain) run second
3. Entry actions (`to <TargetState> ->`) run third
4. Validation (asserts, invariants) runs last

For `no transition` / NoTransition: no exit or entry actions fire (you didn't leave or arrive).

Disambiguation:
- `to Open assert ...` — validation (next token: `assert`)
- `to Open -> ...` — entry action (next token: `->`)

---

## Editable Fields (Locked)

Editable field declarations specify which fields can be modified directly (via the runtime `Update` API) while residing in a state. They use the `in` preposition because editability is about what you can do **while in** a state — consistent with `in <State> assert` (state-scoped invariants).

### Syntax

Form:

- `in <StateTarget> edit <FieldList>`

Where `<FieldList>` is a comma-separated list of declared field names. `<StateTarget>` follows the same rules as state asserts: a single state, comma-separated states, or `any`.

Examples:

```precept
# Notes and Priority are editable in any state
in any edit Notes, Priority

# Description and Tags are editable while work is active
in Open, InProgress edit Description, Tags

# Resolution summary is editable only when resolved
in Resolved edit ResolutionSummary
```

### Disambiguation

The `in` preposition followed by a state target is disambiguated by the next keyword:

- `in Open assert ...` → state-scoped invariant (next token: `assert`)
- `in Open edit ...` → editable field declaration (next token: `edit`)

This is LL(2) at most — the parser sees `in` → state list → `assert` or `edit`.

### Semantics

- **Additive across declarations:** Multiple `in ... edit` statements matching the same state are unioned. The effective editable set for a state is the union of all matching declarations.
- **`in any edit`** expands to all declared states at parse time (same as `in any assert`).
- **Independence from events:** Edit declarations and event transitions are orthogonal. A field can be both editable and modified by event `set` assignments.
- **No terminal state exclusion:** `in any edit` includes terminal states. To exclude specific states, list states explicitly.

### Root-level editability (stateless precepts)

Stateless precepts (no `state` declarations) use a root-level `edit` form without the `in <StateTarget>` prefix:

```
edit all
edit Field1, Field2
```

- `edit all` — declares all declared fields as editable. The `all` sentinel is stored as `["all"]` in `FieldNames` and expanded to all scalar and collection field names at engine construction via `ExpandEditFieldNames()`.
- `edit Field1, Field2` — declares specific named fields as editable.

Root-level `edit` is only valid on stateless precepts. Using it alongside `state` declarations produces **C55 (Error)**: `"Root-level \`edit\` is not valid when states are declared. Use \`in any edit all\` or \`in <State> edit <Fields>\` instead."`

At runtime, `Update` on a stateless instance pulls the editable field set from `_rootEditableFields` (the internal set built from root edit blocks). The `BuildEditableFieldInfosForStateless()` method is used by `Inspect(instance)` to surface root-editable fields for stateless instances.

### Compile-time checks

| Check | Severity |
|---|---|
| Field name not declared | Error |
| State name not declared | Error |
| Duplicate field in same `edit` statement | Warning |
| Empty field list | Error |
| Root-level `edit` while states are declared (C55) | Error |

### Model

The parser produces one `DslEditBlock` per state after expansion:

```csharp
public sealed record DslEditBlock(
    string State,
    IReadOnlyList<string> FieldNames,
    int SourceLine = 0);
```

`DslWorkflowModel` gains an optional `IReadOnlyList<DslEditBlock>? EditBlocks` property.

### Runtime

The runtime `Update` API, `IUpdatePatchBuilder`, validation pipeline, and inspect integration are fully implemented. See `docs/EditableFieldsDesign.md` for the design.

---

## Events (Locked: event-level asserts are arg-only)

### Event declarations

Forms:

- `event <Name>`
- `event <Name> with <ArgList>`
- `event <Name>, <Name>, ...`
- `event <Name>, <Name>, ... with <ArgList>`

Where each arg is:

- `<ArgName> as <Type> [nullable] [default <Literal>]`

Multi-name declarations declare multiple events on a single line, separated by commas. When `with` is present on a multi-name declaration, every declared event receives the same argument list. The comma-separated form communicates the expected event ordering — left to right reads as the intended invocation sequence.

Constraints:
- No duplicate event names (within a single declaration or across declarations).
- No duplicate argument names within an event.

Examples:

```precept
# Single-name forms (unchanged)
event SubmitOrder with items as set of string, paymentToken as string nullable
event Cancel with reason as string

# Multi-name form — bare events, lifecycle order reads left to right
event Submit, Review, Approve, Fund

# Multi-name with shared args — both events get the same signature
event Approve, Reject with Note as string

# Mixing styles in the same precept is valid
event Submit with Applicant as string, Amount as number
event Approve, Reject with Note as string
event Cancel, Archive
```

### Event asserts (Locked)

Event asserts are **event-only** (not state-specific) and **arg-only**.

Form:

- `on <EventName> assert <BoolExpr> because "<Reason>"`

Uniqueness:
- Multiple `on <EventName> assert` statements are allowed for the same event — all are evaluated, and any failure rejects the event.

Scope (Locked):
- `<BoolExpr>` may reference **only** that event’s argument identifiers.
- Dotted access on args is permitted if the underlying expression language supports it (e.g., `items.count`).
- Referencing any non-arg identifier (including fields) is a parse/validation error.
- Validation that combines event args with field state belongs in `when` guards on transition rows, not in event asserts. Event asserts answer "is this event well-formed?" — `when` guards answer "does this event apply given the current state?"

Semantics (Locked ordering):
- Event asserts run **before** transition selection.
- If an event assert is false, the fire/inspect outcome is `Rejected` with the provided reason.

Example:

- `on SubmitOrder assert items.count > 0 and paymentToken != null because "Order must include items and payment"`

Focused example:

```precept
event Cancel with reason as string
on Cancel assert reason != "" because "Cancel requires a reason"
```

---

## Expressions (Locked)

The expression language supports:

- arithmetic: `+`, `-`, `*`, `/`, `%`
- unary: `-` (numeric negation), `not` (logical not)
- logical: `and`, `or`
- comparisons: `==`, `!=`, `>`, `>=`, `<`, `<=`
- membership: `contains`
- parentheses
- identifier expressions with optional dotted member access (`Name.member`)

Collection accessor members carried forward conceptually:
- `.count`
- `.min`, `.max` (sets)
- `.peek` (queue/stack)

Exact operator precedence and literal forms should align with the runtime expression parser.

See [Keyword vs Symbol Design Framework](#keyword-vs-symbol-design-framework-locked) for the rationale behind using keywords for logical operators and symbols for math/comparison.

---

## Transitions (Locked)

Transitions encode **state × event** behavior as flat, self-contained rows.

### Row structure

Each transition is a row with the form:

```
from <StateTarget> on <EventName> [when <BoolExpr>] [-> <Action>]* -> <Outcome>
```

- `from` + `on` — routing: which state(s) and event this row applies to
- `when` — optional guard; if false, this row is skipped
- `->` — pipeline of mutations (zero or more), followed by exactly one outcome
- Whitespace is cosmetic — rows may span multiple lines; `from` starts a new row

### Outcomes

| Outcome | Meaning |
|---|---|
| `transition <State>` | Move to the named state |
| `no transition` | Accept the event, apply mutations, stay in current state (NoTransition) |
| `reject "<reason>"` | Reject the event with the given reason |

### Multi-state and `any`

- `from Open, InProgress on Close -> transition Closed`
- `from any on Prioritize -> set Priority = Prioritize.Level -> no transition`

Same expansion as state asserts — one row per state internally.

### First-match evaluation

Multiple rows for the same `(state, event)` pair are evaluated **top-to-bottom, first match wins:**

```precept
# Row 1: guarded — only matches when condition is true
from Submitted on Cancel when Cancel.reason == "fraud"
    -> set Balance = 0
    -> transition Canceled

# Row 2: unguarded — catches everything else
from Submitted on Cancel
    -> reject "Only fraud cancellation allowed from Submitted"
```

If Row 1's `when` is true, it wins. Otherwise Row 2 (no `when`) catches the rest.

### Resolution outcomes

| Condition | Result |
|---|---|
| No rows exist for `(state, event)` | `Undefined` |
| Rows exist but no `when` guard matches | `Unmatched` |
| A row matches and its outcome is `transition` | `Transition` |
| A row matches and its outcome is `no transition` | `NoTransition` |
| A row matches and its outcome is `reject` | `Rejected` |
| A row matches but post-commit validation fails | `ConstraintFailure` (rolled back) |

`Unmatched` is **terminal** — the Inspect API uses it to signal "this event doesn't apply right now."

#### Reject vs Undefined: when to use which (Locked)

`reject` is for **conditional denial** — the event has transition rows for this state, some paths succeed, but a specific input combination is rejected with a reason:

```precept
from Submitted on Cancel when Cancel.reason == "fraud"
    -> set Balance = 0 -> transition Canceled
from Submitted on Cancel
    -> reject "Only fraud cancellation allowed from Submitted"
```

`reject` is **not** for declaring that an event is structurally unavailable in a state. If an event can never succeed from a state regardless of input, the correct design is to have **no transition rows** for that (state, event) pair — letting the result be `Undefined`. Writing `from Draft on Approve -> reject "Cannot approve from Draft"` is an anti-pattern: it is strictly worse than omitting the row, because it adds maintenance burden, clutters the transition table, obscures the event's actual reachability, and delivers no functional value over `Undefined`.

The analyzer enforces this:
- **C51 (Warning):** A (state, event) pair where every row ends in `reject` — the event can structurally never succeed from that state. Remove the rows and let `Undefined` handle it.
- **C52 (Warning):** An event where every reachable state either has no rows or all rows reject for that event — the event can never succeed from any reachable state.

### Actions (mutation vocabulary)

| Action | Form |
|---|---|
| Scalar assignment | `set <Field> = <Expr>` |
| Set add | `add <SetField> <Expr>` |
| Set remove | `remove <SetField> <Expr>` |
| Queue enqueue | `enqueue <QueueField> <Expr>` |
| Queue dequeue | `dequeue <QueueField> [into <Field>]` |
| Stack push | `push <StackField> <Expr>` |
| Stack pop | `pop <StackField> [into <Field>]` |
| Clear collection | `clear <CollectionField>` |

Actions execute left-to-right with read-your-writes — each action sees the results of prior actions.

### Expression scope in transitions (Locked)

- `when` guards: fields + event args (dotted form only: `EventName.ArgName`)
- `set` / mutation RHS: fields + event args (dotted form only: `EventName.ArgName`)
- Bare arg names (e.g., `reason` instead of `Cancel.reason`) are **not valid** in transition rows — they could collide with field names and are ambiguous when multiple events share arg names.
- Bare arg names are valid only inside event asserts (`on <Event> assert`), where scope is arg-only and no collision is possible.
- Narrowing applies to the exact symbol form used. In transition guards, `when Cancel.reason != null` narrows the dotted key `Cancel.reason`; no cross-form mirroring is needed.
- Cross-event arg references are invalid: a row for `on EventA` cannot reference `EventB.Arg`.

### Execution pipeline

When a transition row matches:

1. **Event asserts** (`on <Event> assert`) — arg-only, pre-transition. If false → `Rejected`.
2. **`when` guard** — if false, skip this row and try next.
3. **Exit actions** (`from <SourceState> ->`) — automatic mutations on leaving source state.
4. **Row mutations** (`-> set ...`, `-> add ...`, etc.) — the row's own pipeline.
5. **Entry actions** (`to <TargetState> ->`) — automatic mutations on entering target state.
6. **Validation** — invariants, state asserts (`in`/`to`/`from`), field rules. Collect-all. If any fail → full rollback, `ConstraintFailure` (see `ConstraintViolationDesign.md` for the `Rejected` vs `ConstraintFailure` distinction — `Rejected` is reserved for author-explicit `reject` outcomes and event assert failures).

For `no transition`: steps 3 and 5 are skipped (no state change). `in <State>` asserts still run (the resulting state is the current state).

### Compile-time checks

| Check | Condition | Severity | ID |
|---|---|---|---|
| Unreachable row | A row follows an unguarded row for the same `(state, event)` | Error | C25 |
| Identical-guard duplicate | A row has the same guard as a prior row for the same `(state, event)` | Error | C47 |
| Non-boolean rule position | `when` guard, `invariant`, or `assert` expression does not produce a boolean | Error | C46 |
| Missing outcome | Row has no outcome (`transition`, `no transition`, or `reject`) | Error | C10 |
| Unknown state/event | `from` references undeclared state; `transition` references undeclared state | Error | C54 |
| Unknown field | `set` targets undeclared field; mutation targets non-collection field | Error | — |
| Unreachable state | State cannot be reached from the initial state by any transition path | Warning | C48 |
| Orphaned event | Event declared but never referenced in any `from … on` transition row | Warning | C49 |
| Dead-end state | Non-terminal state where all outgoing transitions reject or no-transition | Hint | C50 |
| Reject-only pair | Every row for a (state, event) pair ends in `reject` — event can never succeed from this state | Warning | C51 |
| Event never succeeds | Event has rows but every reachable state either has no rows or all rows reject for that event | Warning | C52 |
| Empty precept | Precept declares no events | Hint | C53 |

Checks C48–C53 are graph-level structural analysis, evaluated after parsing succeeds as part of structured compile validation. They do not block compilation unless an error-severity diagnostic is also present — they are advisory diagnostics that surface structural quality issues in the VS Code Problems panel and in MCP tool output. See Phase I in `PreceptLanguageImplementationPlan.md` for implementation details.

No catch-all row is required — if all guarded rows fail, the result is `Unmatched`.

### Diagnostic severity policy (Locked)

All diagnostics follow a three-tier severity model:

| Severity | Meaning | Examples |
|---|---|---|
| **Error** | Provably wrong — the checker can prove a contradiction from types, null-flow, or structural rules. Blocks compilation. | Type mismatches (C39–C41), null-flow violations (C42), unknown identifiers (C38), non-boolean rule positions (C46), identical-guard duplicates (C47), unreachable rows, missing outcomes |
| **Warning** | Probably wrong — structural quality issue that is almost certainly a mistake but could be intentional. Does not block compilation. | Reject-only (state, event) pairs (C51), events that never succeed (C52), unreachable states (C48), orphaned events (C49) |
| **Hint** | Informational — observation that may or may not indicate a problem. | Dead-end states (C50), empty precept (C53) |

The rule: if the checker can **prove** it, it’s an **error**. If the analyzer can **observe** a structural concern, it’s a **warning** or **hint**. The checker never guesses — uncertain cases are left to the inspector.

### Validation pipeline (Locked)

Compile-time validation is fully structured:

1. Parse text into a `PreceptDefinition` plus parse diagnostics.
2. Run shared compile validation, which collects type diagnostics, compile-time default-data/assert diagnostics, and graph diagnostics in one pass.
3. Only construct `PreceptEngine` when no error-severity diagnostics exist.

Expected author mistakes are reported as diagnostics, not as exception-driven control flow. `Compile()` may still throw as a convenience wrapper, but tooling surfaces (`CompileFromText`, language server, MCP) consume structured diagnostics directly.

### Focused example

```precept
# Simple unguarded
from Draft on Submit
    -> set Items = Submit.items
    -> transition Submitted

# Guarded with fallback (first-match)
from Submitted on Cancel when Cancel.reason == "fraud"
    -> set Balance = 0
    -> transition Canceled

from Submitted on Cancel
    -> reject "Only fraud cancellation allowed"

# Multi-state, no mutations
from Open, InProgress on Close -> transition Closed

# any + in-place mutation
from any on Prioritize -> set Priority = Prioritize.Level -> no transition

# Complex with collection mutations
from Signing on RecordSignature
    when PendingSignatories contains RecordSignature.SignatoryName
        and PendingSignatories.count == 1
    -> remove PendingSignatories RecordSignature.SignatoryName
    -> add CollectedSignatures RecordSignature.SignatoryName
    -> transition FullySigned

from Signing on RecordSignature
    when PendingSignatories contains RecordSignature.SignatoryName
    -> remove PendingSignatories RecordSignature.SignatoryName
    -> add CollectedSignatures RecordSignature.SignatoryName
    -> no transition

from Signing on RecordSignature
    -> reject "Signatory is not on the pending list or has already signed"
```

---

## Minimal Example

```precept
precept Order

field Balance as number default 0
field Email as string nullable
field Items as set of string default []

invariant Balance >= 0 because "Balance cannot go negative"

state Draft initial
state Submitted
state Canceled

from Draft assert Email != null because "Must provide email before submitting"
to Submitted assert Items.count > 0 because "Must have items to submit"
in Submitted assert Email != null because "Email must remain set while submitted"

event SubmitOrder with items as set of string, paymentToken as string nullable
on SubmitOrder assert items.count > 0 and paymentToken != null because "Order must include items and payment"

event Cancel with reason as string
on Cancel assert reason != "" because "Cancel requires a reason"

from Draft on SubmitOrder
    -> set Items = SubmitOrder.items
    -> set Balance = Balance - 10
    -> transition Submitted

from Submitted on Cancel when Cancel.reason == "fraud"
    -> set Balance = 0
    -> transition Canceled

from Submitted on Cancel
    -> reject "Only fraud cancellation allowed from Submitted"

from Draft on Cancel
    -> transition Canceled

# Data editing — fields editable without events
in any edit Notes
in Draft edit Email
```

---

## Compile-Time Type Checking (Locked)

The compiler performs expression-level type checking as a compile-blocking phase. All expression positions are validated against a `StaticValueKind` type system (`string`, `number`, `boolean`, `null`, and unions thereof).

### Why this is a key DSL strength

Precept is not meant to be a thin syntax wrapper over runtime behavior. A major part of the DSL's value is that workflows can be validated *before* they are fired:

- invalid identifier references should be rejected before runtime
- incompatible operator usage should be rejected before runtime
- nullable-to-non-nullable flows should be rejected unless the DSL makes the narrowing explicit
- scope mistakes should be rejected where they are authored, not discovered during preview or fire

This matches the broader design direction of explicit null handling and deterministic tooling. Authors should be able to trust that a green compile means the workflow is semantically coherent within the set of rules the compiler claims to understand.

### What is checked

| Expression position | Expected type | Symbol scope |
|---|---|---|
| `when` guard | boolean | data fields + event args + collection accessors + string `.length` |
| `set` assignment RHS | target field type | data fields + event args + collection accessors + string `.length` (narrowed by prior `when`) |
| `add`/`remove`/`push`/`enqueue` value | collection inner type | data fields + event args + collection accessors + string `.length` |
| `dequeue`/`pop into` target | collection inner type assignable to target field | data fields only |
| `invariant` expression | boolean | data fields + collection accessors + string `.length` |
| `in`/`to`/`from` state assert | boolean | data fields + collection accessors + string `.length` |
| `on` event assert | boolean | event args only |
| State action `set`/mutations | same as transition rows | data fields + collection accessors + string `.length` (no event args) |

### Null-flow narrowing

Nullable fields (`string nullable`, `number nullable`) carry a `T|null` union kind. Assigning a nullable value to a non-nullable target is a type error unless the value has been narrowed. Accessing `.length` on a nullable string without narrowing is also a type error (C56).

Narrowing sources:
- **`when` guard:** `when Field != null` narrows `Field` from `T|null` to `T` for the row's action scope.
- **Cross-row negation:** An unguarded row following a `when Field != null` row inherits the negation — `Field` remains `T|null`.
- **State assert propagation:** `in State assert Field != null` narrows `Field` to `T` for all `from State on ...` transition rows and `to`/`from State -> ...` state actions.

### Equality and null-comparison policy (Locked)

Equality is compile-checked strictly. `==` and `!=` are valid only for **compatible** operand kinds. Precept does not perform implicit coercions for equality.

Compatible operand rules:

1. Both operands are from the same scalar family (`string`, `number`, or `boolean`), with `null` optionally present on either side.
2. One operand is `null` and the other operand is a nullable scalar.
3. Both operands are `null`.

Rejected at compile time:

- cross-family comparisons such as `number == string`, `boolean == number`, or `string == boolean`
- comparisons between `null` and a non-nullable scalar
- any equality comparison where one side cannot be resolved to a known compatible kind

This means:

- `Name == "x"` is valid when `Name` is `string` or `string nullable`
- `Count == 0` is valid when `Count` is `number` or `number nullable`
- `IsOpen == true` is valid when `IsOpen` is `boolean` or `boolean nullable`
- `Name == null` is valid only when `Name` is `string nullable`
- `Count == null` is valid only when `Count` is `number nullable`
- `IsOpen == null` is valid only when `IsOpen` is `boolean nullable`
- `Balance == "0"` is invalid
- `Flag == 1` is invalid
- `NonNullableName == null` is invalid

Runtime semantics must match the compile-time rule:

- same-family non-null values compare by value
- nullable comparisons are allowed only when the types are compatible
- no string/number/boolean coercion is ever performed

### `from any` expansion

Type checking for `from any` rows expands to per-state checking. Each state may have different assert-derived narrowings, so a row that type-checks in one state may fail in another. Diagnostics report the specific state that has the problem.

### Constraint codes

| Code | Diagnostic |
|---|---|
| C38 / PRECEPT038 | Unknown identifier in expression |
| C39 / PRECEPT039 | Expression type mismatch (expected vs actual kind) |
| C40 / PRECEPT040 | Unary operator type error (`!` on non-boolean, `-` on non-number) |
| C41 / PRECEPT041 | Binary operator type error (includes `contains` RHS mismatch) |
| C42 / PRECEPT042 | Null-flow violation (assigning `T\|null` to `T` without narrowing) |
| C43 / PRECEPT043 | Collection `pop`/`dequeue into` target type mismatch |

### Design principle

Per philosophy #8, compile-time type checks never produce false positives. If the checker reports an error, it is a real type violation. This is achieved by conservative narrowing — narrowing only applies when the guard condition structurally guarantees the refinement.

### Implementation boundary

The type checker is internal infrastructure (`PreceptTypeChecker`), not a public semantic model. It produces diagnostics and a `TypeContext` consumed by the compiler, language server, and MCP tools. There is no caching layer or incremental analysis — the checker runs from scratch on each invocation. This is sufficient for the current DSL scale and avoids premature abstraction.

### Planned expansion areas (not yet locked)

The current type-checking surface is intentionally conservative and already compile-blocking, but there are additional areas where the language likely wants stronger early validation.

| Area | Current behavior | Likely direction |
|---|---|---|
| Event-arg scope and narrowing (Locked) | Transition rows require dotted form (`Event.Arg`); event asserts use bare arg names. Narrowing applies to exact symbol form. | No design change needed — enforce dotted-only in transition row symbol tables and add tests |
| Event-assert scope isolation (Locked) | Event asserts are arg-only; field references are a compile-time error (C14/C15/C16) | No change needed — arg+state validation belongs in `when` guards, not event asserts |
| Boolean-only rule positions | `when`, `assert`, and `invariant` are expected to produce boolean | Continue hardening so every non-boolean rule position is rejected uniformly |
| Collection mutation contracts (Locked) | Inner-type and `into` checks exist; nullable values require explicit narrowing | No change needed — existing C42 null-flow violation enforces this consistently across all collection verbs |
| Provable impossible conditions (Locked) | Type and null-flow reasoning catches non-nullable null comparisons and post-narrowing contradictions; identical-guard duplicate rows are detectable | No cross-field or arithmetic reasoning. Inspector handles data-dependent impossibility. Add identical-guard duplicate detection as a compile-time error. |

### Design questions to finalize before widening the checker

All policy questions for the current compile-time expansion wave have been locked. No open design questions remain for Phases D–G.

### Diagnostic code policy (Locked)

Overload an existing code when the new condition is the same conceptual error category. Introduce a new code only for genuinely new categories. Existing codes C38–C45 are stable — the message text within each code provides specificity. New codes start at C46.

---

## Status

Locked in this discussion:
- Sectionless flat-statement design: all statements are keyword-anchored, no section headers. The recommended ordering (fields → states → events → transitions) is a convention enforced by language server IntelliSense prioritization, not by grammar.
- `invariant` = static data truth (fields); checked post-commit, always
- `assert` = movement truth (states + events); checked when something happens
  - `in <State> assert` — state-scoped field invariant; entry **and** NoTransition
  - `to <State> assert` — entry gate; only on cross-state entry
  - `from <State> assert` — exit gate; only on cross-state exit
  - `on <Event> assert` — event-fire constraint, arg-only
- State entry/exit actions: `to <State> -> <actions>`, `from <State> -> <actions>`; fields-scoped, no outcomes, no `in` actions
- All state asserts/actions evaluate against the proposed world (post-mutation, pre-commit); fields-scoped
- Multi-state and `any` for state asserts, state actions, and transition rows; not for event asserts
- `in X` strictly subsumes `to X`; duplicate = compile-time error
- Cross-checking `in`/`to` vs `from` on same state for contradictions via per-field domain analysis; provably unexitable state = compile-time error
- Event declarations use `with` instead of parentheses: `event Submit with items as set of string`
- No colon delimiter; `because` is the sentinel between expression and reason
- Noun declarations (`field`/`state`/`event`) have no punctuation; expression-carrying statements use keyword anchoring
- Transition rows: `from <StateTarget> on <Event> [when <Guard>] -> <actions> -> <outcome>`
- `->` pipeline: sequential, read-your-writes, separates context from actions
- First-match evaluation for multiple rows on same `(state, event)`; no catch-all required
- `when` = applicability precondition → `Unmatched` if no row matches
- Execution order: event asserts → when guard → exit actions → row mutations → entry actions → validation
- Coverage warning (not error) for reachable `(state, event)` pairs without transition rows
- Unreachable row after unguarded row = compile-time error
- Editable field declarations: `in <StateTarget> edit <FieldList>` — flat comma-separated syntax, `in` preposition (consistent with "while residing in"), additive across declarations, `any` support. Syntax and model included in language redesign; runtime `Update` API deferred (see `docs/EditableFieldsDesign.md`).
- Collection mutation nullability: `add`/`enqueue`/`push`/`remove` with a `T|null` value into a non-nullable collection require prior narrowing. The shared type checker enforces this via C42 (null-flow violation). Guard narrowing (`when Value != null`) and cross-branch narrowing (prior row handles null case) both satisfy the requirement.
- Event-arg reference form: transition rows (guards, set RHS, mutation values) require the dotted form (`EventName.ArgName`). Bare arg names are valid only inside event asserts (`on <Event> assert`), where scope is arg-only. Narrowing applies to the exact symbol form used — no cross-form mirroring. Cross-event arg references (`EventB.Arg` in a row for `EventA`) are invalid.
- Event-assert scope: permanently arg-only. Field references in event asserts are a compile-time error (C14/C15/C16). Validation combining event args with field state belongs in `when` guards, not event asserts.
- Static impossibility boundary: the compile-time checker proves contradictions only from type information and null-flow narrowing (single-symbol, local reasoning). Additionally, identical-guard duplicate rows for the same `(state, event)` are a compile-time error. No cross-field arithmetic reasoning — the inspector handles data-dependent impossibility.
- Diagnostic severity: three-tier model. **Error** = provably wrong (blocks compilation). **Warning** = structural quality concern (does not block). **Hint** = informational observation. The checker never guesses; uncertain cases are left to the inspector.
- Diagnostic codes: overload existing codes for same conceptual category; new codes only for genuinely new categories. C38–C45 are stable and will not be split. New codes start at C46.

Not yet locked:
- Full EBNF and tokenization rules
