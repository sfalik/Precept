# Precept Language Design (Proposed)

Date: 2026-03-05

This document captures the **proposed** next-step surface syntax and semantics for the Precept DSL, based on the ongoing redesign discussion (goal: a parser/tooling-friendly grammar that does **not** rely on indentation/offside rules).

This is **not** the current implemented DSL. The currently implemented language (regex/imperative parser) remains documented in `docs/DesignNotes.md` and `README.md`.

Backwards compatibility is **not** a requirement for this design.

---

## Goals

- **No indentation-based structure**: blocks/attachments must be explicit and line-oriented.
- **Tooling-friendly**: keyword-anchored statements, deterministic parse, predictable IntelliSense.
- **Keyword-anchored flat statements**: every statement begins with a recognizable keyword — no section headers, no indentation. The parser and language server rely on keyword anchoring alone.
- **Explicit nullability**: use `nullable` rather than punctuation-based null markers.

## Design Philosophy

These principles have driven every syntax and semantics decision:

1. **Deterministic, inspectable model.** Fire/inspect always produces the same result for the same inputs. All validation evaluates against the "proposed world" (post-mutation, pre-commit). No hidden state, no side effects.

2. **English-ish but not English.** Keywords like `with`, `because`, `from`, `on` read naturally but don't attempt full sentences. The language should be learnable by reading examples, not by studying a grammar.

3. **Minimal ceremony.** No colons, no curly braces, no semicolons. `because` is the sentinel between expressions and reasons. Keyword anchoring replaces punctuation for structure.

4. **Locality of reference.** Rules live near the things they describe — invariants near fields, state asserts near states, event asserts near events. Cross-cutting auditing is a tooling concern, not a syntax concern.

5. **Data truth vs movement truth.** `invariant` = static data constraints (always hold). `assert` = movement constraints (checked when something happens — an event fires, a state is entered or exited). The keyword tells you the category.

6. **Collect-all for validation, first-match for routing.** Validation (invariants, asserts) reports every failure. Transition rows are evaluated top-to-bottom, first match wins. These are different problems with different evaluation strategies.

7. **Self-contained rows.** Each transition row is independently readable. No shared context with sibling rows — if a mutation appears in two rows, it's explicit in both.

8. **Sound static analysis only.** Compile-time checks never produce false positives. If the checker can't prove a contradiction, it assumes satisfiable. Exotic cases get a pass; the inspector catches the rest via simulation.

9. **Tooling drives syntax.** IntelliSense, diagnostics, and preview are first-class design constraints. Keyword anchoring enables predictable suggestions. The language server uses semantic ordering to prioritize completions based on what has already been declared. Syntax choices that degrade the tooling experience are rejected even if they're more concise.

10. **Consistent prepositions.** `from`, `to`, `in`, `on` carry the same meaning everywhere they appear. `from` = leaving a state. `to` = entering a state. `in` = while in a state. `on` = when an event fires. The token after the identifier (`assert`, `->`, `on`, body keywords) determines the kind of statement — the preposition's meaning never changes.

11. **`->` means "do something."** The arrow introduces an action — a mutation, an outcome, or a side effect. It separates the *context* (what state, what event, what condition) from the *action* (what to do about it). Sequential execution, read-your-writes. Uniform across transition rows and state entry/exit actions.

## Non-Goals

- Perfect parity with the current DSL syntax.
- Adding new runtime features beyond syntax/structure changes (unless explicitly called out as “New”).

---
## Preposition Convention (Locked)

Four prepositions are used throughout the language to dereference named entities. Each preposition carries the same meaning regardless of which section it appears in:

| Preposition | Meaning | Dereferences | Used in |
|---|---|---|---|
| `in` | While residing in a state | State name | `in <State> assert ...` |
| `to` | Crossing into a state | State name | `to <State> assert ...`, `to <State> -> ...` |
| `from` | Crossing out of a state | State name | `from <State> assert ...`, `from <State> -> ...`, `from <State> on ...` |
| `on` | When an event fires | Event name | `on <Event> assert ...`, `from ... on <Event>` |

**Design principle:** The preposition always means the same thing — `from` always means "leaving this state," `on` always means "when this event fires." What follows the preposition+identifier determines the *kind* of statement:

- `from Draft assert ...` → exit gate (assert context)
- `from Draft on SubmitOrder` → transition routing (transition context)
- `on SubmitOrder assert ...` → event arg validation (assert context)
- `from Draft on SubmitOrder` → transition routing (transition context)

**Disambiguation:** The token after the identifier resolves any ambiguity:
- `assert` → constraint/validation statement
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
- Inline comments are allowed after a statement (exact lexical rule TBD, but intended to match the current implementation’s “strip inline comment” behavior).

---

## Grammar (Informal; Locked Parts)

This is an **informal EBNF-ish** grammar meant to make the current decisions concrete for parser/tooling work. It only covers the parts we have locked so far.

```
PreceptFile        := PreceptHeader Statement*

PreceptHeader      := "precept" Identifier

Statement          := FieldDecl | Invariant | StateDecl | StateAssert | StateAction
                    | EventDecl | EventAssert | TransitionRow
                    | Comment | Blank

FieldDecl          := "field" Identifier "as" TypeRef NullableOpt DefaultOpt
NullableOpt        := ("nullable")?
DefaultOpt         := ("default" LiteralOrList)?

Invariant          := "invariant" BoolExpr "because" StringLiteral

StateDecl          := "state" Identifier InitialOpt
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

EventDecl          := "event" Identifier ("with" ArgList)?
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
- `StateInAssert` = state-scoped field invariant. Checked post-commit on any transition where the resulting state is the named state (entry **and** AcceptedInPlace).
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

---

## Identifiers, Keywords, and Strings

- Identifiers are case-sensitive and compared ordinally.
- String literals use double quotes: `"like this"`.
- Reasons on `on ... assert`/`invariant` are **required** and must be quoted strings.

### Reserved keywords (Locked)

Keywords are **strictly lowercase**. Identifiers are case-sensitive. `From` and `from` are different tokens — `From` is a valid identifier, `from` is not.

Full reserved keyword list:

`precept`, `field`, `as`, `nullable`, `default`, `invariant`, `because`,
`state`, `initial`, `event`, `with`, `assert`,
`in`, `to`, `from`, `on`, `when`, `any`, `of`,
`set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `into`,
`transition`, `no`, `reject`,
`string`, `number`, `boolean`, `true`, `false`, `null`, `contains`

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

---

## Fields (Mostly locked)

### Field declarations

Form:

- `field <Name> as <Type> [nullable] [default <Literal>]`

Defaults:
- Non-nullable scalar fields must declare a `default ...`.
- Nullable scalar fields default to `null` when `default ...` is omitted.
- Collection fields default to empty when `default ...` is omitted.
- Collection defaults may be expressed using a list literal, e.g. `default ["a", "b"]` (exact literal rules TBD).

Examples:

- `field Balance as number default 0`
- `field Email as string nullable`
- `field Tags as set of string default ["priority", "vip"]`

Focused example:

```precept
field Balance as number default 0
field Email as string nullable
field Tags as set of string default ["priority", "vip"]
```

### Field invariants (Proposed; reasons required)

We keep data integrity rules adjacent to fields.

Form:

- `invariant <BoolExpr> because "<Reason>"`

Notes:
- Intended meaning: evaluated after a successful transition/mutation commit; if false, the event is rejected with the given reason.
- Scope (what identifiers are allowed inside `<BoolExpr>`) is proposed as:
  - identifiers may reference any declared field names (including collection fields, via their accessors like `.count`).

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

Open question:
- Should invariants be required to appear *after* all field declarations (for simpler parsing and clearer “declaration then constraints” flow), or can they be interleaved freely?

---

## States (Locked)

### State declarations

Form:

- `state <Name> [initial]`

Constraints:
- Exactly one state must be marked `initial`.
- No duplicate state names.

### State asserts (Locked)

State asserts are checked post-mutation, pre-commit. They may reference any declared fields. All three forms evaluate against the **proposed world** (after mutations apply but before commit finalizes).

Three forms, each with a distinct temporal scope:

| Preposition | Meaning | When checked |
|---|---|---|
| `in <State>` | State-scoped field invariant | After **any** transition resulting in `<State>` (entry **and** AcceptedInPlace) |
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

For `no transition` / AcceptedInPlace: no exit or entry actions fire (you didn't leave or arrive).

Disambiguation:
- `to Open assert ...` — validation (next token: `assert`)
- `to Open -> ...` — entry action (next token: `->`)

---

## Events (Locked: event-level asserts are arg-only)

### Event declarations

Form:

- `event <Name>`
- `event <Name> with <ArgList>`

Where each arg is:

- `<ArgName> as <Type> [nullable] [default <Literal>]`

Constraints:
- No duplicate event names.
- No duplicate argument names within an event.

Example:

- `event SubmitOrder with items as set of string, paymentToken as string nullable`
- `event Cancel with reason as string`

Focused example:

```precept
event SubmitOrder with items as set of string, paymentToken as string nullable
event Cancel with reason as string
```

### Event asserts (Locked)

Event asserts are **event-only** (not state-specific) and **arg-only**.

Form:

- `on <EventName> assert <BoolExpr> because "<Reason>"`

Uniqueness:
- At most one `on <EventName> assert` per event name.

Scope (Locked):
- `<BoolExpr>` may reference **only** that event’s argument identifiers.
- Dotted access on args is permitted if the underlying expression language supports it (e.g., `items.count`).
- Referencing any non-arg identifier is a parse/validation error.

Semantics (Locked ordering):
- Event asserts run **before** transition selection.
- If an event assert is false, the fire/inspect outcome is `Rejected` with the provided reason.

Example:

- `on SubmitOrder assert items.count > 0 && paymentToken != null because "Order must include items and payment"`

Focused example:

```precept
event Cancel with reason as string
on Cancel assert reason != "" because "Cancel requires a reason"
```

---

## Expressions (Carry-forward; details TBD)

The intent is to reuse the existing expression language shape:

- logical: `&&`, `||`, `!`
- comparisons: `==`, `!=`, `>`, `>=`, `<`, `<=`
- membership: `contains`
- parentheses
- identifier expressions with optional dotted member access (`Name.member`)

Collection accessor members carried forward conceptually:
- `.count`
- `.min`, `.max` (sets)
- `.peek` (queue/stack)

Exact operator precedence and literal forms should align with the runtime expression parser.

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
| `no transition` | Accept the event, apply mutations, stay in current state (AcceptedInPlace) |
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
| No rows exist for `(state, event)` | `NotDefined` |
| Rows exist but no `when` guard matches | `NotApplicable` |
| A row matches and its outcome is `transition` | `Accepted` |
| A row matches and its outcome is `no transition` | `AcceptedInPlace` |
| A row matches and its outcome is `reject` | `Rejected` |
| A row matches but post-commit validation fails | `Rejected` (rolled back) |

`NotApplicable` is **terminal** — the Inspect API uses it to signal "this event doesn't apply right now."

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

### Expression scope in transitions

- `when` guards: fields + event args (`EventName.ArgName`)
- `set` / mutation RHS: fields + event args (`EventName.ArgName`)
- Event arg access via dotted form: `Submit.items`, `Cancel.reason`

### Execution pipeline

When a transition row matches:

1. **Event asserts** (`on <Event> assert`) — arg-only, pre-transition. If false → `Rejected`.
2. **`when` guard** — if false, skip this row and try next.
3. **Exit actions** (`from <SourceState> ->`) — automatic mutations on leaving source state.
4. **Row mutations** (`-> set ...`, `-> add ...`, etc.) — the row's own pipeline.
5. **Entry actions** (`to <TargetState> ->`) — automatic mutations on entering target state.
6. **Validation** — invariants, state asserts (`in`/`to`/`from`), field rules. Collect-all. If any fail → full rollback, `Rejected`.

For `no transition`: steps 3 and 5 are skipped (no state change). `in <State>` asserts still run (the resulting state is the current state).

### Compile-time checks

| Check | Condition | Severity |
|---|---|---|
| Coverage | Reachable `(state, event)` pair has no transition rows | Warning |
| Unreachable row | A row follows an unguarded row for the same `(state, event)` | Error |
| Missing outcome | Row has no outcome (`transition`, `no transition`, or `reject`) | Error |
| Unknown state/event | `from` references undeclared state; `on` references undeclared event | Error |
| Unknown field | `set` targets undeclared field; mutation targets non-collection field | Error |

No catch-all row is required — if all guarded rows fail, the result is `NotApplicable`.

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
        && PendingSignatories.count == 1
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

## Minimal Example (Proposed)

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
on SubmitOrder assert items.count > 0 && paymentToken != null because "Order must include items and payment"

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
```

---

## Status

Locked in this discussion:
- Sectionless flat-statement design: all statements are keyword-anchored, no section headers. The recommended ordering (fields → states → events → transitions) is a convention enforced by language server IntelliSense prioritization, not by grammar.
- `invariant` = static data truth (fields); checked post-commit, always
- `assert` = movement truth (states + events); checked when something happens
  - `in <State> assert` — state-scoped field invariant; entry **and** AcceptedInPlace
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
- `when` = applicability precondition → `NotApplicable` if no row matches
- Execution order: event asserts → when guard → exit actions → row mutations → entry actions → validation
- Coverage warning (not error) for reachable `(state, event)` pairs without transition rows
- Unreachable row after unguarded row = compile-time error

Not yet locked:
- Full EBNF and tokenization rules
- Arithmetic operators in expressions (`+`, `-`, `*`, `/`, `%`) — carry-forward TBD
