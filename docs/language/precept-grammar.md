# Precept Grammar Design Reference

> **Audience:** Precept developers and language designers — people working **on** the language
> and runtime. This document explains the grammar's design principles, building blocks, and
> invariants so you can evaluate new constructs, extend the language confidently, and understand
> why the grammar is shaped the way it is.
>
> This is **not** a tutorial for `.precept` authors.

---

## Status

| Property | Value |
|---|---|
| Doc purpose | Design reference — grammar principles, structure, and invariants |
| Primary source | `docs/language/catalog-system.md` (constructs, slots, disambiguation) |
| Related | `docs/compiler/parser.md` · `docs/language/precept-language-spec.md` |
| Updated | 2026-05-03 |

---

## §1. Grammar Design Philosophy

### What Precept's Grammar Is NOT

Before explaining what the grammar is, it helps to rule out what it deliberately isn't:

| Pattern | Description | Precept's stance |
|---------|-------------|------------------|
| **Expression-based** | Code is a tree of expressions; statements are expressions | ✗ Not this. Expressions exist only *inside* slots. A `.precept` file is a list of constructs, not a tree of expressions. |
| **Indentation-sensitive** | Layout defines scope (Python, YAML, Haskell) | ✗ Not this. Whitespace is cosmetic. Block scope is declared by keywords, not indentation. |
| **Context-dependent** | A token's meaning depends on surrounding text | ✗ Not this. Every construct opens with a keyword that unambiguously identifies its kind (with at most one additional lookahead token for disambiguation). |
| **Recursive descent** | Productions nest recursively; grammar is a tree | ✗ Not this. Constructs are flat. Nesting occurs only inside *slot* expressions, not at construct level. |
| **General-purpose** | Designed for programmers with PL background | ✗ Not this. Designed for domain experts and AI agents who think in business terms. |

### The Three Core Design Choices

#### 1. Flat constructs

Every `.precept` file is a flat sequence of constructs. Constructs do not nest inside other constructs (with the limited exception of block bodies, which are explicitly permitted per construct definition — not implied by indentation).

```
┌─────────────────────────────────────────────────┐
│  precept LoanApplication                        │ ← Construct 1
│  field RequestedAmount as number default 0      │ ← Construct 2
│  field ApprovedAmount as number default 0       │ ← Construct 3
│  rule ApprovedAmount <= RequestedAmount ...     │ ← Construct 4
│  state Draft initial                            │ ← Construct 5
│  from Draft on Submit -> ... -> transition ...  │ ← Construct 6
│  ...                                            │
└─────────────────────────────────────────────────┘
         No nesting. No hierarchy. A list.
```

**Why flatness?** The primary author of a `.precept` file is a domain expert, not a programmer. Flat, sequential declarations mimic the structure of a business specification document: "the entity has these fields; these constraints; these states; these transitions." Nested expressions would require understanding call stacks, scope chains, and evaluation order — none of which are natural to a business-domain reader.

Flatness also makes the language mechanically tractable: the parser dispatches on a leading token, walks a fixed slot sequence, and emits a complete construct. No recursive descent, no precedence climbing at the construct level.

#### 2. Keyword anchoring

Every construct begins with a distinguishing keyword (its *leading token*). The first token of any line tells you — with at most one additional peek — exactly what kind of construct follows.

```
field  →  FieldDeclaration
state  →  StateDeclaration
event  →  EventDeclaration
rule   →  RuleDeclaration
from   →  TransitionRow | StateEnsure | StateAction   (disambiguated by 2nd keyword: on | ensure | ->)
in     →  StateEnsure | AccessMode | OmitDeclaration  (disambiguated by 2nd keyword: ensure | modify | omit)
on     →  EventEnsure | EventHandler                  (disambiguated by 2nd keyword: ensure | ->)
to     →  StateEnsure | StateAction                   (disambiguated by 2nd keyword: ensure | ->)
```

**Why keyword anchoring?** Three reasons:

- **Intellisense:** An IDE knows exactly which tokens are valid next because every slot position is catalog-defined. Completion requires no lookahead into semantic context.
- **AI authoring:** An AI agent producing valid Precept needs only to match the keyword-anchored pattern. No deep context or tree structure is required.
- **Error recovery:** The parser synchronizes on leading keywords. If it encounters a syntax error, it skips forward to the next keyword it recognizes. Error recovery is trivially bounded.

#### 3. Named slots

Within a construct, each piece of information occupies a **named, typed position** defined by the construct's `ConstructMeta.Slots` entry in the Constructs catalog. Slots are never positional in the C-style sense — slot markers are keywords (`as`, `when`, `because`, `<-`) that make each slot's role self-evident in the source text.

```
field  ClaimAmount  as  decimal  default 0  nonnegative  maxplaces 2
  │       │          │     │        │           │            │
  │    [Name]     [Slot  [Type]  [Modifier] [Modifier]   [Modifier+value]
  │              marker]        slot item   slot item      slot item
  │
[Leading token]
```

**Why named slots?** Named slots make the language read left-to-right as a natural English sentence. The slot markers (`as`, `when`, `because`) are semantic connectives that a business domain reader can parse without training. They also decouple slot order from slot meaning — an author can understand each piece of the declaration independently without counting positions.

### The Target Audience Constraint

Precept targets two distinct authoring audiences, and the grammar must serve both:

| Audience | What they need from the grammar |
|----------|--------------------------------|
| **Business domain developers** | Readable, English-ish declarations that mirror how they think about the business rules. No PL theory. No operator precedence tables. No abstract syntax. |
| **AI agents** | Predictable, keyword-anchored patterns. Every valid next token is catalog-derivable. Authoring valid Precept = following the keyword-anchored slot sequence for the chosen construct. |

These audiences converge on the same grammar requirements. Both are served by flatness, keyword anchoring, and named slots. That convergence is not accidental — it reflects the design principle that **a language readable to business experts is also structurally predictable for automated tools**.

---

## §2. Grammar Hierarchy

The grammar is organized in four nested levels:

```
File
└── Constructs  (flat sequence of keyword-anchored declarations)
    └── Slots   (named, typed positions within each construct)
        └── Expressions  (computed values within expression-typed slots only)
```

### What each level owns

```
┌────────────────────────────────────────────────────────────────────────────┐
│  FILE                                                                      │
│  • A flat ordered list of constructs.                                      │
│  • No nesting, no scope, no indentation rules.                             │
│  • Begins with a PreceptHeader construct; all others follow.               │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  CONSTRUCT                                                           │  │
│  │  • A single declaration sentence, anchored by a leading keyword.     │  │
│  │  • Maps to a ConstructMeta entry in the Constructs catalog.          │  │
│  │  • Has a flat, ordered sequence of slots defined by that catalog.    │  │
│  │                                                                      │  │
│  │  ┌────────────────────────────────────────────────────────────────┐  │  │
│  │  │  SLOT                                                          │  │  │
│  │  │  • A named, typed position within a construct.                 │  │  │
│  │  │  • May be required or optional; may be single or multi-valued. │  │  │
│  │  │  • Kind determined by ConstructSlotKind enum.                  │  │  │
│  │  │                                                                │  │  │
│  │  │  ┌──────────────────────────────────────────────────────────┐  │  │  │
│  │  │  │  EXPRESSION                                              │  │  │  │
│  │  │  │  • A computed value — appears only in expression-typed   │  │  │  │
│  │  │  │    slots (ComputeExpression, GuardClause, EnsureClause,  │  │  │  │
│  │  │  │    RuleExpression, ActionChain, Outcome).                │  │  │  │
│  │  │  │  • Never a top-level statement.                          │  │  │  │
│  │  │  │  • Parsed by Pratt parser; precedence from Operators     │  │  │  │
│  │  │  │    catalog.                                              │  │  │  │
│  │  │  └──────────────────────────────────────────────────────────┘  │  │  │
│  │  └────────────────────────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────────────┘
```

The critical property of this hierarchy: **the construct level is flat**. Expressions are values inside slots. Slots are positions inside constructs. Constructs are items in a list. The tree terminates at the expression level — expressions have internal tree structure, but constructs do not nest inside each other.

---

## §3. Constructs — The Primary Grammar Unit

### What a construct is

A construct is a complete declaration — a "sentence" in the DSL. There are 12 construct kinds, each mapped to a `ConstructMeta` entry in the Constructs catalog:

| Construct | Leading keyword | Description |
|-----------|----------------|-------------|
| `PreceptHeader` | `precept` | Names the governed entity |
| `FieldDeclaration` | `field` | Declares a stored or computed field |
| `StateDeclaration` | `state` | Declares lifecycle state(s) |
| `EventDeclaration` | `event` | Declares an event with typed arguments |
| `RuleDeclaration` | `rule` | Declares a global data constraint |
| `TransitionRow` | `from` + `on` | Declares one transition in the state machine |
| `StateEnsure` | `in`/`to`/`from` + `ensure` | Declares a state-scoped constraint |
| `AccessMode` | `in` + `modify` | Declares field editability in a state |
| `OmitDeclaration` | `in` + `omit` | Declares field omission in a state |
| `StateAction` | `to`/`from` + `->` | Declares a state entry or exit action hook |
| `EventEnsure` | `on` + `ensure` | Declares an event precondition |
| `EventHandler` | `on` + `->` | Declares a stateless event handler |

### The flat model

Every construct is a single-line statement, except where a block body is explicitly permitted by its `ConstructMeta` definition. Block bodies (`from ... -> ... -> transition ...`) are explicitly permitted constructs — their multi-line nature is declared, not inferred from indentation.

### Construct anatomy

```
 ┌── Leading keyword (required, identifies construct kind)
 │      ┌── Disambiguation token (optional, resolves ConstructFamily)
 │      │        ┌── Name slot (typically: entity name)
 │      │        │       ┌── Additional slots (type, modifier, expression, reference...)
 │      │        │       │                    ┌── Block body (if permitted by ConstructMeta)
 ↓      ↓        ↓       ↓                    ↓
keyword [disambig] name  slot*             [block?]
```

Concrete anatomy — slot and routing archetypes:

These examples are chosen to cover every distinct slot shape and routing pattern in the construct catalog. Together they span:

- the file-opening header construct (`precept`)
- a direct declaration with type + modifier slots (`field`, stored form)
- a direct declaration with a computed-expression slot (`field`, computed form)
- a list-shaped declaration with construct-specific modifiers (`state`)
- a direct declaration with a parenthesized argument list (`event`)
- a direct declaration with rule expression, optional guard, and mandatory reason (`rule`)
- a family-routed declaration with field target and access mode adjective (`in ... modify`)
- a family-routed declaration with reasoned constraint (`in ... ensure`)
- a state hook declaration via disambiguation token (`to ... ->` / `from ... ->`)
- an event-scoped action handler (`on ... ->`)
- the richest routed declaration with guard, action chain, and terminal outcome (`from ... on`)

`OmitDeclaration` is omitted — its slot shape is covered by `AccessMode`. `EventEnsure` is shown explicitly because its `EnsureClause` + `BecauseClause` split mirrors `StateEnsure`. See §4 for construct family routing and disambiguation details.

```
field  ClaimAmount  as  decimal  default 0  nonnegative  maxplaces 2
  │       │          ↑     │        │           │            │
 [1]     [2]      slot    [3]      [4]          [4]         [4+val]
                 marker

[1] Leading token: `field`
[2] IdentifierList slot — the field name
[3] TypeExpression slot — `as` is the slot marker; `decimal` is the type
[4] ModifierList slot — zero or more modifier keywords, some with values
```

```
field  LineTotal  as  number  <-  TaxableAmount + TaxAmount  nonnegative
  │       │         ↑    │      ↑              │               │
 [1]     [2]      slot  [3]   slot            [4]             [5]
                  marker      marker
[1] Leading token: `field`
[2] IdentifierList slot — the field name
[3] TypeExpression slot — `as` is the slot marker; `number` is the type
[4] ComputeExpression slot — `<-` is the slot marker; the expression computes the field value
[5] ModifierList slot — optional trailing modifiers still apply to computed fields
```

```
state  Draft  initial
  │      │       │
 [1]    [2]─────[2] (continued)
[1] Leading token: `state`
[2] StateEntryList slot — comma-separated (name modifier*) entries; `initial` is a
    state modifier within this slot, not a separate slot
```

```
event  Submit  (Amount as number, Note as string optional)
  │       │                     │
 [1]     [2]                  [3]                           [4] (if present)
[1] Leading token: `event`
[2] IdentifierList slot — the event name
[3] ArgumentList slot (optional) — the parenthesized typed parameter list
[4] InitialMarker slot (optional) — `initial` keyword, marks the event as the entry-point event
```

```
in  Approved  ensure  ApprovedAmount > 0  because  "…"
 │     │        ↑           │               ↑        │
[1]   [2]   disambig.      [3]             slot     [4]
            token                          marker
[1] Leading token: `in`   ← shared with AccessMode, OmitDeclaration
[2] StateTarget slot — the anchor state name
[3] EnsureClause slot — the expression condition (`ensure <expression>`)
[4] BecauseClause slot — the mandatory explanatory reason
```

```
on  Submit  ensure  Amount > 0  because  "…"
 │    │       ↑          │          ↑        │
[1]  [2]  disambig.     [3]        slot     [4]
          token                     marker
[1] Leading token: `on`   ← shared with EventHandler
[2] EventTarget slot — the anchor event name
[3] EnsureClause slot — the expression condition (`ensure <expression>`)
[4] BecauseClause slot — the mandatory explanatory reason
```

```
from  Draft  on  Submit  when  DocumentsVerified  ->  set ApplicantName = Submit.Applicant  ->  transition Submitted
  │     │     ↑    │      ↑          │             ↑         │                                  ↑         │
 [1]   [2]  slot  [3]   slot        [4]          slot       [5]                               slot       [6]
            mark       mark                      mark                                         mark
[1] Leading token: `from`
[2] StateTarget slot — the source state (or `any`)
[3] EventTarget slot — the event name
[4] GuardClause slot — optional `when` expression
[5] ActionChain slot — zero or more `-> action` pairs
[6] Outcome slot — terminal disposition (`-> transition State`, `-> reject "…"`, `-> no transition`)
```

```
precept  Claim
   │       │
  [1]     [2]
[1] Leading token: `precept`
[2] IdentifierList slot — the governed entity name
```

```
rule  ApprovedAmount >= 0  when  IsActive  because  "Approved amount cannot be negative"
  │            │             ↑       │        ↑                    │
 [1]          [2]          slot     [3]     slot                 [4]
                           marker           marker
[1] Leading token: `rule`
[2] RuleExpression slot — the boolean condition that must hold globally
[3] GuardClause slot (optional) — `when` expression that scopes when the rule applies
[4] BecauseClause slot — the mandatory explanatory reason
```

```
in  Draft  modify  ClaimAmount  editable  when  DocumentsVerified
 │    │       ↑         │           │       ↑            │
[1]  [2]  disambig.    [3]         [4]    slot         [5]
            token                             marker
[1] Leading token: `in`   ← shared with StateEnsure, OmitDeclaration
[2] StateTarget slot — the anchor state name
[3] FieldTarget slot — the field affected in that state
[4] AccessModeKeyword slot — `editable` or `readonly`
[5] GuardClause slot — optional `when` expression
```

```
to  Approved  ->  set ApprovedAmount = ClaimAmount
 │     │       ↑                │
[1]   [2]  disambig.           [3]
            token
[1] Leading token: `to`   ← shared with StateEnsure
[2] StateTarget slot — the state whose entry hook is being declared
[3] ActionChain slot — one or more `-> action` steps; `from` uses the same shape for exit hooks
```

```
on  Submit  ->  set ClaimAmount = Submit.Amount
 │    │       ↑             │
[1]  [2]  disambig.        [3]
           token
[1] Leading token: `on`   ← shared with EventEnsure
[2] EventTarget slot — the event whose handler is being declared
[3] ActionChain slot — one or more `-> action` steps for the event-scoped handler
```

---

## §4. Construct Families and Disambiguation

### What a family is

A **construct family** is a group of constructs that share the same leading keyword. Because the leading keyword alone doesn't distinguish them, the parser peeks at the next one or two tokens to select the correct `ConstructMeta`.

Four routing families exist, organized by how the parser identifies them:

```
ROUTING FAMILIES
────────────────────────────────────────────────────────────────────────────────────
FAMILY       LEADING TOKEN    DISAMBIGUATION     CONSTRUCTS
────────────────────────────────────────────────────────────────────────────────────
Header       precept          none (unique)      PreceptHeader
Direct       field            none               FieldDeclaration
             state            none               StateDeclaration
             event            none               EventDeclaration
             rule             none               RuleDeclaration
StateScoped  in               peek at next kwd   StateEnsure (ensure)
                                                 AccessMode  (modify)
                                                 OmitDeclaration (omit)
             from             peek at next kwd   TransitionRow (on)
                                                  StateEnsure (ensure)
                                                  StateAction (->)
             to               peek at next kwd   StateEnsure (ensure)
                                                  StateAction (->)
EventScoped  on               peek at next kwd   EventEnsure (ensure)
                                                 EventHandler (→)
────────────────────────────────────────────────────────────────────────────────────
```

> **Terminology note:** "Routing family" classifies all 12 constructs by parse scope. "ConstructFamily" (in the catalog) refers to the narrower subset of StateScoped and EventScoped constructs where the leading keyword is *shared* and requires disambiguation. Header and Direct constructs have unique leading tokens and carry no ConstructFamily entry.

### How disambiguation works

```
Parser sees token T
        │
        ↓
Constructs.ByLeadingToken[T]
        │
  ┌─────┴──────────────────────┐
  │ one candidate              │ multiple candidates
  │ (Header or Direct)         │ (StateScoped/EventScoped family)
  ↓                            ↓
parse immediately         peek(offset) → disambiguationToken
                                    │
                          ┌─────────┴──────────────────┐
                          │ matches candidate A          │ matches candidate B
                          ↓                              ↓
                    parse construct A              parse construct B
```

Disambiguation reads `DisambiguationEntry.DisambiguationTokens` from the construct's catalog entry. The offset and token set are metadata — the parser carries no hardcoded disambiguation logic.

**The one-lookahead guarantee:** Disambiguation requires at most one additional token beyond the leading token. A grammar requiring two or more additional tokens to disambiguate breaks the invariant that completion tools can predict the next valid token at every position.

### Key families in detail

#### The `in` family (StateScoped)

```
in  [AnchorState]  ensure  Expr  because  "..."                    → StateEnsure
in  [AnchorState]  modify  Field  [readonly|editable]  [when Guard] → AccessMode
in  [AnchorState]  omit  Field                                     → OmitDeclaration
```

The second keyword (`ensure`, `modify`, `omit`) is the disambiguation token. `readonly` and `editable` are access-mode adjectives inside the `AccessModeKeyword` slot, not family-level disambiguation tokens.

#### The `on` family (EventScoped)

```
on  EventName  ensure  Expr  because  "..."        → EventEnsure
on  EventName  ->  actions                         → EventHandler
```

The second keyword (`ensure` vs `->`) is the disambiguation token.

#### The `from` family (StateScoped)

```
from  [AnchorState]  on  EventName  [when Guard]  -> ActionChain -> Outcome  → TransitionRow
from  [AnchorState]  ensure  Expr  because  "..."                             → StateEnsure
from  [AnchorState]  ->  actions                                              → StateAction
```

The second keyword (`on`, `ensure`, or `->`) is the disambiguation token. `on` leads `TransitionRow`; `ensure` leads `StateEnsure` (exit constraint); `->` leads `StateAction` (exit hook).

#### The `to` family (StateScoped)

```
to  [AnchorState]  ensure  Expr  because  "..."    → StateEnsure
to  [AnchorState]  ->  actions                     → StateAction
```

The second keyword (`ensure` vs `->`) is the disambiguation token. `ensure` leads `StateEnsure` (entry constraint); `->` leads `StateAction` (entry hook).

#### The `field` family (Direct — not a ConstructFamily)

While `field` is a single Direct construct, it has three surface forms distinguished by slot content:

```
field  Name  as  Type  [modifiers]           → stored field
field  Name  as  Type  <-  Expr              → computed field
field  Name  as  Type  <-  Expr  [modifiers] → computed field with constraints
```

These are all `FieldDeclaration`; the distinction is in slot values, not construct kind. The parser sees one `ConstructMeta`, walks all slots, and the type checker interprets the presence of a `ComputeExpression` slot to classify the field.

---

## §5. Slots — Named Positions Within Constructs

### What a slot is

A slot is a named, typed position in a construct's grammar, defined by `ConstructMeta.Slots` in the Constructs catalog. Each slot has:

- A **kind** (`ConstructSlotKind`) — determines what the slot contains and how the parser fills it
- A **required flag** — whether the slot must appear
- An optional **description** — for tooling and documentation

### Slot kinds

The 17 `ConstructSlotKind` values cover every distinct slot type in the language:

| Kind | What it holds | Where it appears |
|------|---------------|-----------------|
| `IdentifierList` | One or more names (field, state, event) | `field Name`, `event Submit` |
| `TypeExpression` | A type reference (type keyword + qualifiers) | `as decimal`, `as set of string` |
| `ModifierList` | Zero or more modifier keywords (some with values) | `nonnegative maxplaces 2 default 0` |
| `StateEntryList` | Comma-separated (name modifier*) pairs for state declarations | `Draft initial, Submitted, Approved terminal success` |
| `ArgumentList` | Typed parameter list | `(Amount as number, Note as string optional)` |
| `ComputeExpression` | Computed field body expression | `<- UnitPrice * Quantity` |
| `GuardClause` | Optional `when` condition expression | `when DocumentsVerified and CreditScore >= 680` |
| `ActionChain` | Sequence of `-> action` steps | `-> set ApprovedAmount = ...` |
| `Outcome` | Terminal transition outcome | `-> transition Approved` / `-> reject "..."` / `-> no transition` |
| `StateTarget` | A state name reference | `from Draft`, `to Approved` |
| `EventTarget` | An event name reference | `on Submit` |
| `EnsureClause` | Constraint expression | `ensure ApprovedAmount > 0` |
| `BecauseClause` | Mandatory reason string literal | `because "Approved amount must be positive"` |
| `AccessModeKeyword` | Access mode for a field | `editable`, `readonly` |
| `FieldTarget` | A field name reference | `modify DecisionNote` |
| `RuleExpression` | The rule's boolean expression | `rule amount > 0` |
| `InitialMarker` | Optional `initial` keyword on event declarations | `event Submit initial` |

### Slot positions in a real construct

Here is the complete slot sequence for a `TransitionRow`:

```
from   Draft   on   Submit   when   Expr   -> action* -> Outcome
  │      │      │     │        │      │         │            │
 [1]    [2]    [3]   [4]      [5]   [6]        [7]          [8]

Slot # │ Kind             │ Required │ Notes
───────┼──────────────────┼──────────┼────────────────────────────────────────
  [1]  │ (leading token)  │  yes     │ Structural marker, not a slot
  [2]  │ StateTarget      │  yes     │ Source state name (or `any`)
  [3]  │ (slot marker)    │  yes     │ `on` keyword — slot delimiter
  [4]  │ EventTarget      │  yes     │ Event name
  [5]  │ (slot marker)    │  no      │ `when` keyword — slot delimiter
  [6]  │ GuardClause      │  no      │ Guard expression
  [7]  │ ActionChain      │  no      │ Mutation verbs (zero or more)
  [8]  │ Outcome          │  yes     │ Terminal disposition
```

### Named positions vs. positional arguments

Precept slots are named positions, not positional arguments. The slot markers (`as`, `when`, `because`, `<-`, `on`) appear in the source text and make each slot's role explicit. An author reading:

```
field RequestedAmount as number default 0 nonnegative
```

can parse each piece independently:
- `as number` = type declaration
- `default 0` = default value modifier
- `nonnegative` = constraint modifier

No counting positions. No consulting a function signature. **The keyword markers are the grammar made visible.**

---

## §6. Expressions — Computed Values in Slots

### Where expressions appear

Expressions appear **only** in expression-typed slots. The six expression-typed slot kinds are:

| Slot kind | Where it appears | Example |
|-----------|-----------------|---------|
| `ComputeExpression` | Computed field body | `field Subtotal as number <- UnitPrice * Quantity` |
| `GuardClause` | Transition row guard, rule scope | `when DocumentsVerified and CreditScore >= 680` |
| `EnsureClause` | State/event constraint expression | `ensure ApprovedAmount > 0` |
| `RuleExpression` | Rule body | `rule ExistingDebt <= AnnualIncome * 3` |
| `ActionChain` | Action assignments | `-> set ApprovedAmount = min(Approve.Amount, RequestedAmount)` |
| `Outcome` | Terminal transition outcome | `-> transition Approved` / `-> reject "reason"` |

### Expression as a value, not a statement

This is the critical design constraint: **an expression is a value produced by computation, not a statement that can stand alone at the construct level.** There are no expression statements in Precept. You cannot write:

```
# ✗ Not valid — expressions cannot be top-level constructs
CreditScore * 0.5 + AnnualIncome
```

An expression is always embedded inside a slot, inside a construct. This constraint enforces the flat construct model — a file is a list of declarations, not a list of computed expressions.

### Expression kinds

The 13 `ExpressionFormKind` values (from the ExpressionForms catalog) cover the full expression grammar:

| Category | Kinds | Examples |
|----------|-------|---------|
| **Atom** | `Literal`, `Identifier`, `Grouped` | `0`, `true`, `"text"`, `CreditScore`, `(a + b)` |
| **Composite** | `BinaryOperation`, `UnaryOperation`, `MemberAccess`, `Conditional`, `PostfixOperation` | `a + b`, `-x`, `Submit.Amount`, `if x then y else z`, `x is set` |
| **Invocation** | `FunctionCall`, `MethodCall`, `CIFunctionCall` | `min(a, b)`, `amount.currency`, `startswith~("val")` |
| **Collection** | `ListLiteral` | `[1, 2, 3]` |
| **Quantifier** | `Quantifier` | `each item in Items satisfies item > 0` |

The expression grammar is parsed by a Pratt parser (operator-precedence parsing) using the Operators catalog for precedence and associativity metadata.

### Expression structure within a slot

```
field  TaxAmount  as  number  <-  TaxableAmount  *  TaxRate  /  100
                              ↑   │               │    │      │   │
                           slot  [Identifier]  [Op]  [Id]  [Op] [Literal]
                           mark                           └── BinaryOperation (right sub-expr)
                                └───────── BinaryOperation (full expression tree) ─────────┘
```

The `<-` is the slot marker for a `ComputeExpression` slot. Everything to its right is the expression — parsed as a tree, but contained entirely within the slot.

### The open design question on expression trees

Expression-carrying slots currently carry only a `SourceSpan` (source location) in the parser's output. Full expression tree representation is deferred pending design work on the `ExpressionNode` hierarchy. See `docs/compiler/parser.md` § Expression Tree Design for the open design question and context.

### Why expressions stay inside slots

If expressions could appear as top-level constructs, the language would require a recursive-descent grammar with arbitrary nesting depth. Keeping expressions inside slots means:

1. The construct level remains flat — the parser dispatches on a leading token, walks a slot sequence, done.
2. Every slot position is catalog-defined — the tools can predict what's valid next.
3. Expression purity is enforceable — expressions cannot trigger side effects because they only appear in positions where the runtime knows they are being evaluated as values.

---

## §7. The Linguistic Model

Precept's grammar maps to linguistic roles that a business-domain reader already understands. This is not coincidence — it is a deliberate design choice that makes the language accessible without training in formal grammar notation.

### Roles

| Linguistic role | What plays this role in Precept | Examples |
|-----------------|--------------------------------|---------|
| **Nouns** | Entity names — field names, state names, event names | `LoanApplication`, `CreditScore`, `Draft`, `Submit` |
| **Verbs** | Event names (domain operations), action keywords (mutations) | `Submit`, `Approve`, `FundLoan`; `set`, `transition`, `reject` |
| **Adjectives/Modifiers** | Constraint keywords, state qualifiers | `nonnegative`, `optional`, `initial`, `terminal`, `required` |
| **Prepositions** | Structural connectives that mark slot positions | `as`, `of`, `from`, `to`, `on`, `when`, `because`, `in` |

### Annotated example

```precept
from  UnderReview  on  Approve  when  DocumentsVerified and CreditScore >= 680
 │         │        │     │      │               │
PREP     NOUN     PREP  VERB    PREP           ADJECTIVE/NOUN composition
(anchor) (state)       (event) (guard)
    -> set ApprovedAmount = min(Approve.Amount, RequestedAmount)
        │       │                    │
       VERB   NOUN                 NOUN (expression: function over nouns)
    -> transition Approved
        │            │
       VERB        NOUN (destination state)
```

Reading this construct aloud in English: *"From [the UnderReview state], when [Approve] happens and [documents are verified and credit is strong enough], set [the ApprovedAmount] and move to [Approved]."*

The grammar mirrors the business analyst's natural description of the business rule.

### Why the linguistic model matters

**For business domain developers:** The language uses English connectives they already know. `field X as Y` reads as "field X [typed] as Y." `in Draft ensure Z because "…"` reads as "in Draft, ensure Z, because [reason]." The prepositions carry semantic weight — `as` introduces a type, `when` introduces a condition, `because` introduces a justification.

**For AI agents:** AI language models understand natural language structure deeply. A grammar built around English linguistic patterns is easier for AI agents to author correctly because the patterns match what the model has already been trained to understand. Keyword anchoring (`from`, `on`, `when`, `because`) gives the AI unambiguous anchor points for each slot.

**For language evolution:** New constructs that fit the linguistic model will feel natural to authors. A new construct that breaks the pattern — using verb-first ordering, or positional rather than named slots — will feel alien and be harder to learn. The linguistic model is a design constraint that keeps the language coherent across additions.

---

## §8. Grammar Invariants for Language Evolution

These are the rules any future language enhancement MUST respect. They are not stylistic preferences — each invariant protects a specific property of the language that would break if violated.

### Invariant 1: Every construct has a unique keyword anchor (or a unique disambiguation path)

**The rule:** Every construct begins with a leading token that identifies it (or a leading token plus at most two additional lookahead tokens in a construct family).

**Why it exists:** The parser dispatches via `Constructs.ByLeadingToken`. Adding a construct with no distinguishing leading token would require semantic context to parse — breaking the catalog-driven dispatch model and destroying error recovery.

**What breaks if violated:** The parser cannot dispatch without executing semantic analysis. Intellisense cannot predict valid constructs at a cursor position. AI agents cannot produce valid syntax without understanding runtime context.

---

### Invariant 2: Disambiguation requires at most 2 lookahead tokens

**The rule:** When a leading token is shared by multiple constructs (a `ConstructFamily`), the disambiguation token must be identifiable by peeking at most 2 tokens ahead (offset 1 or 2 from the leading token).

**Why it exists:** Completion tools predict valid tokens at position N by knowing tokens 0..N-1. If disambiguation requires more than 2 tokens of lookahead, the completion tool cannot suggest the right tokens at the disambiguation position — it can't know which construct the author is writing until too late.

**What breaks if violated:** Intellisense completions become wrong or absent at the disambiguation position. The parser may require backtracking. Error recovery becomes unpredictable.

---

### Invariant 3: Slot order is defined by `ConstructMeta` — not inferred from position

**The rule:** The order of slots in any construct is declared in `ConstructMeta.Slots`. The parser walks slots in that order. Consumers access slots by kind, not by index.

**Why it exists:** Named slots make the grammar readable. If slot order could vary by context or be inferred heuristically, the grammar would be ambiguous. An author would need to know "what position does `when` go in?" rather than "where is the `when` keyword?"

**What breaks if violated:** Authors must memorize positional ordering rather than learning slot markers. Tooling loses its ability to predict valid tokens at each slot position. Consumer code (type checker, evaluator) breaks when slots move.

---

### Invariant 4: Expressions appear only in expression-typed slots

**The rule:** An expression (computed value, boolean condition, arithmetic formula) is valid only inside a slot of kind `ComputeExpression`, `GuardClause`, `EnsureClause`, `RuleExpression`, `ActionChain`, or `Outcome`. Expressions are never standalone top-level constructs.

**Why it exists:** Keeping expressions inside slots maintains the flat construct model. It also enforces expression purity — expressions cannot have side effects because they only appear in value-producing positions.

**What breaks if violated:** The language becomes expression-oriented. The flat dispatch model breaks down — the parser must now handle arbitrary expressions at the construct level. Compile-time proof reasoning becomes harder (expressions as statements can be control flow).

---

### Invariant 5: Block bodies are explicitly permitted — not implied by indentation

**The rule:** Multi-line construct bodies (like the action chain in a `TransitionRow`) are declared as explicitly permitted in the construct's `ConstructMeta`. The block boundary is a keyword (`->`, `from`), not a layout rule.

**Why it exists:** Indentation-sensitive languages have known failure modes: reformatting tools produce semantically different code; copy-paste corrupts structure; AI agents produce layout-dependent errors. Keyword-bounded blocks are immune to these problems.

**What breaks if violated:** The language becomes brittle to formatting, reformatting, and code generation. AI agents produce correct-looking but semantically broken code when they use wrong indentation.

---

### Invariant 6: New constructs must fit an existing family or start a new family with a fresh keyword

**The rule:** A new construct either:
- Shares a leading token with an existing family (and adds a disambiguation token), or
- Introduces a new unique leading token.

It cannot reuse an existing leading token with disambiguation that conflicts with an existing family member's tokens.

**Why it exists:** Disambiguation must remain unambiguous. If two constructs in the same family have overlapping disambiguation token sets, the parser cannot route to a unique `ConstructMeta` entry.

**What breaks if violated:** Parse ambiguity. Two constructs match the same leading-token + disambiguation-token sequence. Error recovery becomes unpredictable.

---

## §9. The Catalog as Grammar Specification

### The 13 catalogs as a grammar

The Constructs catalog is not the only catalog that defines the grammar — it is the entry point. The 13 catalogs together form a complete, machine-readable grammar specification:

```
CATALOG                 GRAMMAR ROLE
────────────────────────────────────────────────────────────────────────────────
Tokens                  The lexical vocabulary — every keyword, operator,
                        punctuation, and identifier kind.

Constructs              The construct inventory — 12 construct kinds with leading
                        tokens, slot sequences, and disambiguation entries.
                        Constructs.ByLeadingToken is the parser's dispatch table.

ExpressionForms         The expression grammar — 13 node kinds covering atoms,
                        composites, invocations, and quantifiers.

Types                   What type names are valid in TypeExpression slots.

Operators               What operators are valid in expression-typed slots;
                        their precedence and associativity for the Pratt parser.

Operations              What (operator, lhs type, rhs type) combinations are
                        legal and what result type they produce.

Functions               What function names are valid in expression invocations.

Modifiers               What modifier keywords are valid in ModifierList slots,
                        per type and per construct context.

Actions                 What action verbs are valid in ActionChain slots.

Constraints             What constraint forms are valid in EnsureClause and
                        RuleExpression slots.

ProofRequirements       What proof obligations must hold before expressions in
                        specific slots can safely evaluate.

Diagnostics             What compile-time errors the pipeline can produce —
                        the rules that enforce all of the above.

Faults                  What runtime errors the evaluator can produce —
                        defensive coverage for paths the compiler proved safe.
────────────────────────────────────────────────────────────────────────────────
```

### The catalog is the grammar — not a reflection of it

This is the architectural identity claim of the Precept catalog system. In traditional compilers (Roslyn, GCC, TypeScript), grammar knowledge is scattered across parser source code, type checker implementations, and per-construct AST node definitions. In Precept:

> **The catalog IS the grammar specification in machine-readable form. Pipeline stages are generic machinery that reads it.**

The parser does not contain per-construct parsing logic. It reads `Constructs.ByLeadingToken` to dispatch, walks `ConstructMeta.Slots` to build `ParsedConstruct` nodes, and consults `DisambiguationEntry.DisambiguationTokens` to resolve families. Adding a new construct means adding a catalog entry — not modifying parser source code.

```
Traditional model:
  Parser source code ──── encodes ────▶ Grammar rules (scattered)
  Type checker code  ──── encodes ────▶ Type rules (scattered)
  AST node types     ──── encode  ────▶ Construct shapes (N×M explosion)

Precept model:
  Catalogs ──── declare ────▶ Grammar, types, rules, shapes
    │
    ├──▶ Parser reads Constructs → dispatch + slot walking
    ├──▶ Type checker reads Types, Operations, Modifiers → type rules
    ├──▶ IDE completions read all catalogs → context-sensitive suggestions
    ├──▶ TextMate grammar reads Tokens, Constructs → syntax highlighting
    └──▶ MCP precept_language reads all catalogs → AI grounding
```

### Implication for language evolution

Adding a new construct to Precept:

1. **Add a `ConstructKind` enum member** — the new construct's identity.
2. **Add a `ConstructMeta` entry** — leading token, slot sequence, disambiguation token (if in a family), description, usage example.
3. **If a new slot kind is needed**, add a `ConstructSlotKind` member and a corresponding slot sub-parser.
4. **If a new keyword is needed**, add a `TokenKind` and `TokenMeta` entry.

The parser, completions, TextMate grammar, MCP vocabulary, and AI grounding all derive the new construct automatically. No per-construct parser methods. No per-construct AST node classes. No parallel copies to update.

**This is what "the catalog IS the grammar" means in practice.**

---

## Appendix: Quick Reference

### Construct-to-family mapping

```
Header     ─► precept
Direct     ─► field  │  state  │  event  │  rule
StateScoped─► from [state]   on [event] → TransitionRow
            ─► in  [state]   ensure     → StateEnsure
            ─► to  [state]   ensure     → StateEnsure
            ─► from [state]  ensure     → StateEnsure
            ─► in  [state]   modify     → AccessMode
            ─► in  [state]   omit       → OmitDeclaration
            ─► to  [state]   -> …       → StateAction
            ─► from [state]  -> …       → StateAction
EventScoped─► on  [event]   ensure     → EventEnsure
            ─► on  [event]   -> …       → EventHandler
```

### Slot-kind to expression-type mapping

```
ConstructSlotKind         Expression-typed?
─────────────────────────────────────────────
IdentifierList            no  — names only
TypeExpression            no  — type keyword + qualifiers
ModifierList              no  — modifier keywords + values
StateEntryList            no  — (name modifier*) pairs
InitialMarker             no  — keyword only
ArgumentList              no  — name:type pairs
StateTarget               no  — state name
EventTarget               no  — event name
BecauseClause             no  — string literal only
AccessModeKeyword         no  — keyword only
FieldTarget               no  — field name
ComputeExpression         YES — arbitrary expression
GuardClause               YES — boolean expression
EnsureClause              YES — boolean expression
RuleExpression            YES — boolean expression
Outcome / ActionChain     YES — expressions within action assignments
```

### Grammar invariants at a glance

```
1. Every construct has a unique keyword anchor (or unique disambiguation path)
2. Disambiguation ≤ 2 lookahead tokens
3. Slot order defined by ConstructMeta — never inferred positionally
4. Expressions in expression-typed slots only — never standalone constructs
5. Block bodies explicitly permitted by ConstructMeta — not implied by indentation
6. New constructs join an existing family (new disambig token) or start a new one (new keyword)
```
