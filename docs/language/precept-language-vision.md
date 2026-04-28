# Precept Language Vision

**Date:** 2026-04-20
**Status:** Working future-state language summary for clean-room compiler design.
**Purpose:** Define the intended Precept language surface and static semantics in one place, combining the implemented language with the latest canonical proposal branches and issue design work.

---

## Scope

This document is the language-only source of truth for the next compiler effort.

It is intentionally limited to:

1. The source language authors write.
2. The static meaning of that language.
3. The proof and validation obligations the language implies.

It intentionally excludes:

1. Compiler architecture.
2. Runtime API shape.
3. Component boundaries.
4. Tooling implementation.
5. Current prototype implementation details.

Where the language has both implemented and proposed material, this document states the combined target language that the new compiler pipeline should serve. Where a mechanism is directionally clear but not fully syntax-locked, the document records the language contract the compiler must be able to support.

## Grounding

This summary is grounded in:

1. Product philosophy.
2. The current MCP language surface.
3. The canonical current design docs for language, type checking, proof reasoning, constraints, and architecture framing.
4. Issue #111 proof-engine language enhancements.
5. The latest remote design branch for issue #95.
6. The latest PR head for issue #107.
7. Issues #65, #58, and #86 for planned language surface growth.
8. Issue #115 evaluator redesign: semantic fidelity and lane integrity — establishing the numeric lane system, bridge functions, static completeness guarantee, and evaluator contract.
9. Issue #134 null-reduction and per-state field access modes — establishing `optional` (replacing `nullable`), `is set`/`is not set` presence operators, `clear` action keyword, `null` removal from expressions, and `modify`/`readonly`/`editable`/`omit` per-state field access mode declarations (replacing `edit`, subsequently replacing `write`/`read`).

---

## Design Principles

The language must preserve these principles. They are non-negotiable — any redesign that violates one has departed from Precept's identity.

1. **Prevention, not detection.** Invalid entity configurations — combinations of lifecycle position and field values that violate declared rules — cannot exist. They are structurally prevented before any change is committed, not caught after the fact. This is the defining commitment: the contract prevents them. A compiler that validates on request is doing detection; a compiler that makes invalid configurations structurally impossible is doing prevention.

2. **One file, complete rules.** Every field, rule, ensure, and transition lives in the `.precept` definition. There is no scattered logic across service layers, validators, event handlers, or ORM interceptors. All proof facts, all type information, all constraint scope, all routing — everything needed to understand and enforce the contract — derives from a single file. No imports, no cross-file references, no external rule injection, no ambient configuration.

3. **Deterministic semantics.** Same definition, same data, same outcome — always. No non-deterministic solvers, no timing-dependent analysis, no culture-dependent operations, no stochastic reasoning. This is what makes the engine trustworthy as a business rules host and auditable as an AI agent tool.

4. **Full inspectability.** The language must make the engine's reasoning fully exposable. At any point, you can preview every possible action and its outcome without executing anything. The engine exposes the complete reasoning: conditions evaluated, branches taken, constraints applied. You see not only what would happen, but why. Nothing is hidden. Inspectability extends to proof reasoning — proven ranges, source attribution, and what the engine could not prove must all be surfaceable through diagnostics, hover, and tooling.

5. **Keyword-anchored readability.** Structure is explicit and line-oriented, not indentation-sensitive. Statement kind is identified by its opening keyword sequence. The language is AI-safe: regular enough that humans and agents can author it reliably without layout-sensitive syntax traps.

6. **Explicit domain meaning over primitive convenience.** When a value has real domain identity (money, date, quantity, currency), the language should name it as a distinct type with its own operator rules and compile-time enforcement. Primitive types are the storage mechanism; domain types are the meaning mechanism.

7. **Compile-time-first static checking.** The compiler proves what it can, rejects what it can prove invalid, and does not guess. Compile-time structural checking catches unreachable states, type mismatches, constraint contradictions, divisor safety issues, and more — before runtime, before any entity instance exists. This is where the "contract" metaphor becomes literal: the compile step validates the definition's structural soundness before any instance exists.

8. **Approximation honesty.** The language does not present approximation as exactness. If a value or operation is inherently approximate, that fact must be explicit in the contract. Exact-value lanes remain exact. Silent approximation inside an exact-looking path weakens the user's ability to reason about outcomes. The line between exact and approximate behavior must be visible in the type system and the language surface.

9. **Mandatory rationale.** Every constraint carries a mandatory reason. The engine requires not just the rule, but its rationale. This is a language requirement, not a convention — the `because` clause is syntactically required on every rule and ensure.

10. **Totality.** Every expression evaluates to a result or a definite error. The language does not produce silent `NaN`, `Infinity`, or `null` from numeric operations. Division by zero produces a definite error. Overflow produces a definite error. An empty collection's `.min`/`.max`/`.peek` produces a definite error. The evaluation surface is total — no expression silently corrupts state.

11. **Static completeness.** If a precept compiles without diagnostics, it does not produce type errors at runtime. The type checker catches all type errors at compile time. Runtime type checks exist only as defensive redundancy, never as the primary enforcement mechanism. This is the bridge between the compiler and the evaluator: the compiler's job is to make the evaluator's error paths unreachable.

---

## Language Model

A precept defines a governed business entity.

The entity may be:

1. Stateful: lifecycle positions, transitions, state-scoped rules, and event routing.
2. Stateless: data integrity, editability, and event-driven mutation without a state machine.

The governing concepts are:

1. Fields: stored or computed data.
2. Rules: global data truth.
3. Ensures: contextual truth tied to state or event context.
4. Events: typed triggers.
5. Actions: mutations.
6. Transitions: movement truth.
7. Editability: direct mutation permissions.
8. Modifiers: declaration-attached structural, semantic, or severity intent.

The language protects configurations, not isolated values. In a stateful precept, a configuration is current state plus current field data. In a stateless precept, it is the current field data alone.

---

## Governance, Not Validation

This distinction is fundamental to the language's identity and governs how every constraint surface works.

**Validation** checks data at a moment in time, when called. A validator runs when you invoke it. Code paths that don't call the validator bypass the rules. There is always a window where invalid state can exist.

**Governance** declares what the data is allowed to become and enforces that declaration structurally, on every operation, with no code path that bypasses the contract. The language makes certain configurations structurally impossible — not checked, not caught, but prevented.

This distinction shapes the language at every level:

1. Rules are not assertions called at a checkpoint. They are declarations that hold structurally on every operation where they apply.
2. Guarded rules do not weaken the guarantee. They make it precise — the rule applies exactly where the domain says it should, and the engine ensures it cannot be bypassed.
3. State is not a passive label. It is an active rule-activator. An entity in `Approved` has different data requirements than the same entity in `Draft` — the state defines what must be true about the data there.
4. Mutations are atomic. All mutations execute on a working copy. Constraints are evaluated against the working copy. If all constraints pass, the working copy is promoted. If any constraint fails, the working copy is discarded. There is no window where a partially-committed mutation with a violated rule exists.
5. An invalid definition cannot produce an engine. The compile-time gate is a structural boundary — not a convention, not a best practice. If the definition has errors, no engine exists to run.

The full guarantee is about configurations: the pair of (current lifecycle position, current field values) for stateful entities, or simply current field values for stateless entities. Invalid configurations are structurally impossible. A valid entity is simply one where every constraint holds for its current configuration.

---

## Execution Model Properties

The following properties of Precept's execution model are language design choices, not implementation accidents. They are what make tractable compile-time reasoning possible, and any future compiler must preserve them.

1. **No loops.** The language has no iteration constructs. Expression trees are finite and acyclic. This eliminates the need for fixpoint computation and widening operators.

2. **No control-flow branches.** A transition row is a flat sequence: evaluate a guard, execute assignments left-to-right, check rules and ensures. There are no `if` statements that split execution into paths that later reconverge. Conditional *expressions* (`if/then/else`) produce a single value — both branches are type-checked, exactly one is evaluated — but they do not create control-flow divergence.

3. **No reconverging flow.** Because there are no loops or branches, there is no join point where two different states must be merged. Each assignment in a row sees the state left by all preceding assignments. This makes sequential flow analysis a linear walk, not a dataflow graph.

4. **Closed type vocabulary.** The language has a fixed set of types. No user-defined types, no parametric polymorphism, no open type hierarchies. This makes exhaustive type checking possible over a finite, fully-known vocabulary.

5. **Finite state space.** States, events, fields, and rules are declared statically. Every transition row, state action, and rule can be enumerated exhaustively. No symbolic execution over unbounded domains is needed.

6. **Expression purity.** Expressions cannot mutate entity state, trigger side effects, or observe anything outside their evaluation context (current field values and event arguments). This is a language semantic, not a caller convention. It is what makes inspection safe — you can always ask what an expression would produce without affecting anything.

7. **No separate compilation.** Each `.precept` file is self-contained. No imports, no cross-file references. The compiler processes the entire definition in one pass. All proof facts derive from the single file.

These properties are the reason the language can support tractable compile-time proofs. Standard interval arithmetic, bounded relational closure, and single-pass validation are directly applicable without the lattice infrastructure that general-purpose analyzers require. The absence of widening is a feature: in general-purpose analyzers, widening is the primary source of precision loss.

---

## Source Form

Precept source is a flat sequence of keyword-led statements.

### Structural properties

1. No indentation significance.
2. No section headers required for parsing.
3. Statement kind is identified by its opening keyword sequence.
4. Comments use `#`.
5. Blank lines are ignored.
6. The language remains line-oriented and tooling-friendly.

### Core declaration inventory

The language envelope includes these top-level forms:

| Form | Meaning |
|---|---|
| `precept <Name>` | Declares the governed entity |
| `field <Name>[, <Name>, ...] as <Type> [<modifiers>]` | Declares stored data |
| `field <Name> as <Type> -> <Expr>` | Declares computed data |
| `rule <Expr> [when <Guard>] because "..."` | Declares global data constraints |
| `state ...` | Declares lifecycle states |
| `event ...` | Declares triggers and typed arguments |
| `event <Name>(...) initial` | Declares the construction event (initial event) |
| `in/to/from <State> [when <Guard>] ensure <Expr> because "..."` | Declares state-scoped constraints |
| `on <Event> [when <Guard>] ensure <Expr> because "..."` | Declares event-scoped arg constraints |
| `to/from <State> -> ...` | Declares state entry or exit actions |
| `in <State> modify <Fields> readonly\|editable [when <Guard>]` | Declares state-scoped field access mode overrides |
| `field X as T writable` | Sets field X's baseline access mode to writable across all states |
| `from <State> on <Event> ... -> ...` | Declares transition routing and mutation |
| `on <Event> -> ...` | Declares stateless event action hooks |

### Preposition discipline

The language keeps a strict preposition model:

1. `in` means while residing in a state, or within a specific unit context when attached to typed fields.
2. `to` means entering a state.
3. `from` means leaving a state.
4. `on` means when an event fires.
5. `of` means membership in a dimension or category family.

The same preposition should not change meaning from one construct to another.

---

## Lexical Layer

The tokenizer-facing surface must support the following classes of tokens.

### Keywords

The language is keyword-dominant for structure and domain meaning.

Keyword families:

1. Declaration keywords: `precept`, `field`, `state`, `event`, `rule`, `ensure`, `modify`, `writable`.
2. Structural prepositions: `in`, `to`, `from`, `on`, `of`, `with`, `into`.
3. Control keywords: `when`, `if`, `then`, `else`.
4. Action keywords: `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `transition`, `reject`, `no`.
5. Constraint keywords: `optional`, `default`, `because`, `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`, `ordered`.
6. Literal keywords: `true`, `false`.
7. Modifier keywords: `initial`, `all`, `any`, and future modifier vocabulary attached to declarations.
8. Access mode keywords: `omit`, `readonly`, `editable`.
9. Presence operators: `is set`, `is not set`.

### Operators

The core operator families remain:

1. Arithmetic: `+`, `-`, `*`, `/`, `%`.
2. Comparison: `==`, `!=`, `>`, `>=`, `<`, `<=`.
3. Case-insensitive comparison: `~=`, `!~` — string-only, ordinal ignore-case.
4. Logical: `and`, `or`, `not`.
5. Membership: `contains`.
6. Structural arrow: `->`.
7. Member access: `.`.

### Delimiters and punctuation

The language surface includes:

1. Parentheses `(` and `)`.
2. Brackets `[` and `]`.
3. Comma `,`.
4. Double quotes `"` for strings.
5. Single quotes `'` for typed constants.
6. Braces `{` and `}` for interpolation inside quoted literals.

### Important lexical shift

Time-unit words such as `days`, `hours`, `minutes`, `seconds`, `months`, `years`, and `weeks` are no longer language keywords in the future literal system. They are validated content inside typed constants. That keeps the language keyword set from expanding every time a new literal family appears.

---

## Literal System

The language has two quoted literal forms — strings and typed constants — plus primitive and list literals.

### Primitive literals

Primitive literals remain bare tokens:

1. Numeric literals.
2. Boolean literals.

The language has no `null` literal. Optional fields use `is set`/`is not set` for presence testing and `clear` for value removal.

### String literals

String literals use `"..."` and always produce `string`.

They support interpolation:

```precept
rule Score >= 680 because "Score {Score} is below the minimum"
```

### Typed constants

Typed constants use `'...'` and produce non-primitive values. Like numeric literals, typed constants are **context-born** — the expression context determines the type, and the content is then validated against that type.

The resolution model:

1. Context determines the type (field type, operator peer, function signature).
2. Content is validated against the expected type.
3. No context → compile error.
4. The delimiter is type-agnostic — it does not infer type by content shape.

Examples:

| Typed constant | Resolved type (from context) |
|---|---|
| `'2026-06-01'` | `date` |
| `'14:30:00'` | `time` |
| `'2026-04-13T14:30:00Z'` | `instant` |
| `'2026-04-13T09:00:00'` | `datetime` |
| `'2026-04-13T14:30:00[America/New_York]'` | `zoneddatetime` |
| `'America/New_York'` | `timezone` |
| `'30 days'` | `period` or `duration`, determined by context |
| `'100 USD'` | `money` |
| `'5 kg'` | `quantity` |
| `'24.50 USD/kg'` | `price` |

Typed constants also support interpolation:

```precept
field GracePeriod as period default '{DefaultGraceDays} days'
field HourlyRate as price in 'USD/hours' default '{StartingRate} USD/hours'
```

### Lists

List literals remain bracketed scalar lists used in default clauses for collection fields.

### Zero constructor rule

The language does not rely on constructor-style literal syntax such as `date(...)`, `period(...)`, or freestanding quantity constructors. Typed constants are the non-primitive literal mechanism.

---

## Type System

The type system includes four layers.

### 1. Primitive scalar types

1. `string`
2. `boolean`
3. `integer`
4. `number`
5. `decimal`
6. `choice(...)`

#### Numeric lane system

The three numeric types — `integer`, `decimal`, and `number` — are distinct semantic lanes, not interchangeable representations of "a number."

| Lane | Backing type | Semantics | Use case |
|---|---|---|---|
| `integer` | exact whole number | Discrete counts, indices, positions | `.count`, `.length`, loop-free integer arithmetic |
| `decimal` | exact base-10 fractional | Financial, rate, tax, any domain requiring exactness | Money, percentages, business arithmetic |
| `number` | IEEE 754 floating-point | Approximate computation, scientific, scoring | Calculations where approximation is acceptable |

Lane rules:

1. **Homogeneous arithmetic stays in lane.** `integer + integer → integer`, `decimal * decimal → decimal`, `number + number → number`. No silent lane crossing.
2. **Integer widens losslessly.** `integer` may widen to `decimal` (exact) or `number` (range-preserving) in mixed-lane arithmetic.
3. **Decimal and number never mix implicitly.** Every operation combining `decimal` and `number` — arithmetic, assignment, comparison, function arguments, default values — is a type error. The author must explicitly bridge via `approximate()` or `round(value, places)`. No exceptions. See [Primitive Types · Numeric Lane Rules](primitive-types.md#numeric-lane-rules) for the full conversion map.
4. **Integer-shaped surfaces produce `integer`.** `.count`, `.length`, and rounding functions (`floor`, `ceil`, `truncate`, `round` with no places argument) return `integer`.
5. **Explicit bridges only.** Two explicit bridge functions cross lane boundaries: `approximate(decimal) → number` and `round(value, places) → decimal`. No implicit coercion paths exist for arithmetic or assignment.

#### Context-sensitive literal typing

Fractional numeric literals (e.g., `0.1`, `3.14`, `1.08`) do not have an inherent lane. Their lane is determined by expression context:

1. **Field assignment:** `set Price = 0.1` where `Price` is `decimal` → literal resolves as `decimal`. Where `Score` is `number` → literal resolves as `number`.
2. **Binary expression:** `Price * 1.08` where `Price` is `decimal` → `1.08` resolves as `decimal`.
3. **Comparison:** `Score > 50.0` where `Score` is `number` → `50.0` resolves as `number`.
4. **Function argument:** `min(Price, 100.00)` where `Price` is `decimal` → `100.00` resolves as `decimal`.
5. **Field constraint:** `field Price as decimal min 0.01 max 999.99` → constraint values resolve as `decimal`.
6. **Default value:** `field Price as decimal = 0.0` → default resolves as `decimal`.

Every fractional literal resolves to exactly one lane — no ambiguity. If no context is available, the literal defaults to `decimal` (the exact lane is the safer default for business-domain use). The parser preserves the original literal text; the type checker resolves the lane at type-check time.

No literal suffix syntax (e.g., `0.1m`, `0.1d`) is part of the language. Context-sensitive typing is the chosen mechanism.

#### Choice type

`choice` supports ordered and unordered variants:

- **Unordered choice:** `field Status as choice("draft", "active", "closed") default "draft"` — equality comparison only (`==`, `!=`).
- **Ordered choice:** `field Priority as choice("low", "medium", "high") ordered default "low"` — enables ordinal comparison (`<`, `<=`, `>`, `>=`) based on declaration order. The first value has the lowest rank.
- **Ordinal rank is field-local.** Ordinal comparison is valid only between a choice field and a literal from its own set. Comparing two choice fields is a compile-time error — ordinal rank is defined per-declaration, not globally.
- **Literal validation:** Every literal assigned to or compared against a choice field must be a member of that field's declared set. This applies in both assignment contexts (e.g., `set Status = "bogus"`) and comparison contexts (e.g., `when Priority > "bogus"`). Non-member literals are compile-time errors.

#### Type operator contracts

Each primitive type has a defined operator surface. Operations outside this surface are type errors.

| Type | Equality (`==`, `!=`) | CI Equality (`~=`, `!~`) | Relational (`<`, `<=`, `>`, `>=`) | Arithmetic (`+`, `-`, `*`, `/`, `%`) | Logical (`and`, `or`, `not`) |
|---|---|---|---|---|---|
| `string` | Ordinal (case-sensitive) | ✓ Ordinal ignore-case | Type error | `+` is concatenation (both operands must be string; no implicit coercion) | Type error |
| `boolean` | Yes | Type error | Type error | Type error | Yes (short-circuit) |
| `integer` | Yes | Type error | Yes | Yes (stays integer, checked overflow) | Type error |
| `decimal` | Yes (exact base-10) | Type error | Yes (exact base-10) | Yes (stays decimal, overflow is error) | Type error |
| `number` | Yes (IEEE 754) | Type error | Yes (IEEE 754) | Yes (stays number) | Type error |
| `choice` (unordered) | Yes (ordinal) | Type error | Type error | Type error | Type error |
| `choice` (ordered) | Yes (ordinal) | Type error | Yes (declaration-position rank) | Type error | Type error |

There is no truthy/falsy coercion. `0` and `""` are not boolean. Conditions (`when`, `if`, `rule`, `ensure`) require `boolean`-typed expressions.

String concatenation requires both operands to be `string`. Mixed-type concatenation (`string + number`, `string + boolean`) is a type error — the language does not implicitly coerce non-string operands to string. String relational comparison is not available on plain strings — only on `choice` fields with the `ordered` constraint.

`.length` on an unset `optional` string field produces an error, not a silent `0`. The type checker enforces a presence guard (`is set`) for optional string fields before `.length` access.

### 2. Collection types

1. `set of <T>`
2. `queue of <T>`
3. `stack of <T>`

The language remains explicit and English-like rather than generic-bracket-based.

Collections preserve element type identity through storage, retrieval, comparison, and serialization. A `set of decimal` stores exact decimal values and compares them via exact decimal comparison — no intermediate floating-point conversion. Elements added to a collection are coerced to the collection's declared inner type using the same lane rules as field assignment.

### 3. Temporal types

The temporal surface is:

1. `date`
2. `time`
3. `instant`
4. `duration`
5. `period`
6. `timezone`
7. `zoneddatetime`
8. `datetime`

Their semantic model is:

1. `date`, `time`, `datetime`, `zoneddatetime`, and `instant` are point-like types.
2. `duration` is elapsed timeline quantity.
3. `period` is calendar quantity.
4. `timezone` is a first-class identity type.
5. `instant` has restricted semantics and must mediate through `.inZone(tz)` before local calendar access.

The strict hierarchy is part of the language contract:

```text
instant -> zoneddatetime -> datetime -> date/time
```

Skip-level access is a compile-time error.

### 4. Business-domain types

The business-domain surface is:

1. `money`
2. `currency`
3. `quantity`
4. `unitofmeasure`
5. `dimension`
6. `price`
7. `exchangerate`

These types exist so the compiler can reason about domain meaning rather than primitive storage shape.

#### Qualification system

Type qualifiers are distinct from declaration modifiers. A **modifier** asserts a structural or semantic property of the declaration itself (`terminal`, `optional`, `required`). A **type qualifier** is part of the type annotation — it narrows the set of values the field may hold. The distinction matters: modifiers affect compile-time analysis and diagnostics; type qualifiers constrain the value domain.

Two mutually exclusive type qualifiers apply to relevant field types:

```precept
field Amount as money in 'USD'
field Weight as quantity in 'kg'
field Distance as quantity of 'length'
field GracePeriod as period of 'date'
```

1. `in '<specific-unit>'` pins a field to a specific unit or currency basis.
2. `of '<dimension-family>'` constrains a field to a category family.
3. A field may use `in` or `of`, never both.

#### Decimal-first business arithmetic

Business-domain arithmetic is exact-value-oriented:

1. `decimal` is the scalar backbone.
2. `integer` widens losslessly.
3. `number` is not the preferred business scalar.
4. Cross-currency arithmetic is never implicit.
5. Currency conversion requires explicit `exchangerate` use.

#### Compound cancellation

The language must support compound types such as:

1. `price in 'USD/kg'`
2. `exchangerate in 'USD/EUR'`
3. `quantity in 'kg/hour'`

Cancellation rules are part of the language meaning:

1. `price × quantity -> money` when units cancel.
2. `money / quantity -> price`.
3. `exchangerate × money -> money` when the currency pair matches.
4. Time-denominator compounds cancel against `duration` for fixed-length time units.
5. Time-denominator compounds cancel against `period` where calendar-length semantics are intended.

---

## Fields

### Stored fields

Stored fields declare named data, type, nullability, default, and attached constraints.

The language supports multi-name field declarations where several fields share a type and modifiers:

```precept
field FirstName, LastName, MiddleName as string default ""
field Width, Height, Depth as number default 0 nonnegative
```

Multi-name declarations cannot have derived expressions — computed fields require individual declarations.

### Computed fields

Computed fields remain part of the language. They are declarative expressions over other values and participate in rule enforcement as first-class fields.

Computed fields imply a dependency graph. The compiler must:

1. Determine topological evaluation order from the dependency graph.
2. Detect and reject dependency cycles at compile time.
3. Scope-restrict computed fields — they cannot reference event arguments, only stored field values.
4. Transitively expand computed field references when attributing constraint violations — a rule that references a computed field is semantically about the stored fields that feed it.

### Constraint surface

The constraint surface includes at least:

1. `optional`
2. `default`
3. `nonnegative`
4. `positive`
5. `nonzero`
6. `notempty`
7. `min`
8. `max`
9. `minlength`
10. `maxlength`
11. `mincount`
12. `maxcount`
13. `maxplaces`
14. `ordered`

The compiler must treat these as language-level constraints, not just syntactic sugar. Some may desugar to rules conceptually, but the language meaning includes their distinct diagnostics, proof behavior, and hover vocabulary.

### Period qualification direction

The language unifies period restrictions under the same qualification system as domain types:

1. `period in 'days'` or another unit basis where exact basis matters.
2. `period of 'date'` or `period of 'time'` where component family matters.

That is the combined direction implied by the temporal and quantity/UOM design work.

---

## Expressions

The expression language is a pure, deterministic expression system.

### Core expression forms

1. Literals.
2. Identifier references.
3. Dotted member access.
4. Parenthesized expressions.
5. Unary operators.
6. Binary operators.
7. Conditional expressions: `if <condition> then <value> else <value>`. The condition must evaluate to `boolean` — no truthy/falsy coercion. Exactly one branch is evaluated; the unselected branch is never evaluated. This is a short-circuit guarantee, not an optimization — expressions like `if Count > 0 then Total / Count else 0` rely on the else branch not evaluating a division-by-zero. Conditional expressions nest arbitrarily.
8. Built-in function calls.

### Operator families

1. Arithmetic: `+`, `-`, `*`, `/`, `%`.
2. Comparison: `==`, `!=`, `>`, `>=`, `<`, `<=`.
3. Case-insensitive comparison: `~=`, `!~` — string-only, ordinal ignore-case.
4. Logical: `and`, `or`, `not` — deterministic left-to-right short-circuit.
5. Membership: `contains` — collection membership only. Not available for substring testing on strings; use `startsWith` and `endsWith` for prefix/suffix testing.
6. Assignment: `=` (in `set` actions only).
7. Presence: `is set`, `is not set` — boolean presence test for `optional` fields.

### Dotted access

Dotted access is a major part of the language surface.

Families include:

1. Collection accessors: `.count` (→ `integer`), `.min`, `.max` (→ inner type, set-only, requires orderable T), `.peek` (→ inner type, queue/stack only). The `.min`, `.max`, and `.peek` accessors require a conditional emptiness guard — the type checker rejects bare accessor use outside a conditional expression whose condition tests `.count > 0` (`UnguardedCollectionAccess`). The idiomatic pattern is `if Items.count > 0 then Items.min else 0`. `.min`/`.max` on a `set of T` where T is non-orderable (e.g., `set of boolean`, `set of period`) is a `NonOrderableCollectionExtreme` type error. The same guard requirement applies to `dequeue` and `pop` mutations (`UnguardedCollectionMutation`).
2. String accessors: `.length` (→ `integer`). Returns the code-unit count of the string.
3. Event argument access such as `EventName.ArgName` in mixed-scope contexts. Dotted event-arg forms also support `.length`: `EventName.ArgName.length` → `integer`.
4. Temporal accessors such as `.year`, `.month`, `.date`, `.time`, `.instant`, `.timezone`, and `.inZone(tz)` where valid.
5. Domain accessors for identity-bearing types, such as dimension and unit metadata.

### Built-in functions

The language includes a closed, static function library. Functions use prefix call syntax: `functionName(arg1, arg2, ...)`. No user-defined functions. No dynamic dispatch. No varargs except where noted.

#### Numeric functions

| Function | Signature | Description |
|---|---|---|
| `abs(x)` | `number → number` (type-preserving: integer→integer, decimal→decimal) | Absolute value |
| `ceil(x)` | `decimal\|number → integer` | Round toward positive infinity |
| `floor(x)` | `decimal\|number → integer` | Round toward negative infinity |
| `truncate(x)` | `decimal\|number → integer` | Truncate toward zero |
| `round(x)` | `decimal\|number → integer` | Banker's rounding to nearest integer |
| `round(x, places)` | `number × integer → decimal` | **Explicit bridge**: round to N decimal places; normalizes number→decimal |
| `min(a, b, ...)` | `number × number → number` (variadic, type-preserving) | Smallest of two or more values |
| `max(a, b, ...)` | `number × number → number` (variadic, type-preserving) | Largest of two or more values |
| `clamp(x, lo, hi)` | `number × number × number → number` (type-preserving) | Constrain value to range [lo, hi] |
| `pow(base, exp)` | `number × integer → number` (type-preserving) | Raise to integer power; exponent must be non-negative in the integer lane |
| `sqrt(x)` | `number → number` | Square root; number-lane only; requires compile-time non-negative proof |
| `approximate(x)` | `decimal → number` | **Explicit bridge**: decimal→number; makes the precision loss visible |

**Function lane integrity rule.** A function keeps its decimal overload if and only if the mathematical operation is closed over finite decimals — meaning decimal input always produces a result exactly representable as a finite decimal. Functions that are inherently approximate (`sqrt`, and future functions such as `log`, `sin`, `cos`, `exp`) live exclusively in the number lane. Authors reach them via `approximate()`.

**Bridge summary:**

| Direction | Bridge | Reads as |
|---|---|---|
| number → decimal | `round(value, places)` | "Round this to N places" |
| decimal → number | `approximate(value)` | "Approximate this value" |

#### String functions

| Function | Signature | Description |
|---|---|---|
| `startsWith(s, prefix)` | `string × string → boolean` | Case-sensitive prefix test |
| `endsWith(s, suffix)` | `string × string → boolean` | Case-sensitive suffix test |
| `toLower(s)` | `string → string` | Lowercase (invariant culture) |
| `toUpper(s)` | `string → string` | Uppercase (invariant culture) |
| `trim(s)` | `string → string` | Remove leading and trailing whitespace |
| `left(s, n)` | `string × integer → string` | Leftmost N code units (clamped to string length) |
| `right(s, n)` | `string × integer → string` | Rightmost N code units (clamped to string length) |
| `mid(s, start, length)` | `string × integer × integer → string` | Substring, 1-indexed (clamped); start and length must be `integer` |

The language prefers dot access for parameterless type-owned properties (`.length`, `.count`) and freestanding functions for operations that take arguments or are not scoped to a single receiver type.

### Scope model

Expression scope is declaration-dependent.

1. Rules, state ensures, and write guards operate in field scope.
2. Event ensures operate in event-arg scope.
3. Transition guards and transition actions operate in mixed field plus event-arg scope.
4. Stateless event hooks operate in mixed field plus event-arg scope.
5. Mixed-scope positions use explicit dotted event-arg access to avoid ambiguity.

---

## Constraint Semantics

The language distinguishes categories of truth.

### Rules

`rule` expresses global data truth — constraints that must hold after every mutation.

Rules support optional guards:

```precept
rule Score >= 680 because "Credit score too low"
rule DownPayment >= RequestedAmount * 0.20 when LoanType == "conventional" because "Conventional loans require 20% down"
```

A guarded rule applies only when its guard is true. This is conditional constraint scoping — the rule is precise about where the domain says it should apply — not a weakening of the guarantee. The engine ensures the rule holds in every configuration where its guard is satisfied.

Rules operate in field scope. They cannot reference event arguments — this ensures reusability across all events.

### Ensures

`ensure` expresses contextual truth — constraints scoped to a specific state or event.

Ensures also support optional guards:

```precept
in Review ensure Reviewers.count >= 2 because "Review requires at least two reviewers"
in Open when Escalated ensure Priority >= 3 because "Escalated tickets must be high priority"
on Submit when Submit.Type == "payment" ensure Submit.Amount > 0 because "Payment amounts must be positive"
```

The surface includes these anchors:

1. `in <State> ensure ...` — residency truth.
2. `to <State> ensure ...` — entry truth.
3. `from <State> ensure ...` — exit truth.
4. `on <Event> ensure ...` — event-argument truth.

### Rejections

`reject` is authored prohibition, not failed data truth. This is a designed prohibition in the definition, not a constraint violation — it means the author deliberately forbade this outcome.

### Guards

`when` guards are routing logic, not constraints. They do not produce violations. A guard that evaluates to false simply means the row doesn't match — the runtime moves to the next row. Only the row that actually fires (or an explicit `reject` fallback) produces outcomes. This is a fundamental language semantic: guards select; constraints enforce.

### Collect-all vs first-match

The language continues to make a semantic distinction between:

1. Validation surfaces, which are collect-all in spirit. Rules and ensures are evaluated exhaustively — every applicable constraint is checked, and all violations are reported.
2. Routing surfaces, which are first-match and order-sensitive. Transition rows are evaluated in order — the first matching guard wins, and remaining rows are not evaluated.

---

## Lifecycle and Routing

### Stateful precepts

Stateful precepts continue to use explicit state and event routing:

```precept
from Draft on Submit when Submit.Score >= 680 -> transition Approved
```

The routing model remains:

1. Rows are state and event scoped.
2. Guards discriminate rows.
3. First matching row wins.
4. Actions execute left-to-right.
5. Outcomes are explicit.

State is not a passive label — it is an active rule-activator. An entity in `Approved` has different data requirements than the same entity in `Draft`. The state defines what must be true about the data there. State activates the constraint set appropriate to that position, authorizes which fields can be mutated directly, and gates which transitions are available.

### Entity construction

Construction is modeled as an **initial event** — the precept's constructor. This solves the fundamental problem that entities with required fields (non-optional, no default) cannot be constructed parameterlessly: the author would be forced to either invent nonsense defaults or make things optional that shouldn't be.

```precept
event Create(ApplicantName as string, Amount as currency in USD, CreditScore as integer) initial
```

The `initial` modifier on an event designates it as the construction event. The runtime's `Create(args)` operation fires this event atomically as part of entity creation:

1. Build a hollow version (defaults applied, initial state set, omitted fields structurally absent).
2. Fire the initial event with the caller's args through the standard pipeline — same guards, same mutations, same ensures, same constraint checking as any other event.
3. Return the `EventOutcome` — same 7 variants the caller uses for every `Fire`.

If the precept does not declare an initial event, `Create()` is parameterless and always succeeds (the compiler guarantees all fields have defaults or are optional — enforced by C59/C86).

**Construction-time constraint composition.** When the initial event fires, constraints compose naturally with no new language surface:

1. **Arg ensures** (`on Create ensure ...`) — pre-assignment validation of caller-provided args.
2. **Field constraints** (rules, field-level ensures) — post-assignment truth.
3. **Global rules** (`rule ...`) — always evaluated.
4. **Entry ensures** (`to <InitialState> ensure ...`) — construction-specific truth. These are the same entry ensures that fire on any transition into the initial state, but at construction time they serve as the intake invariant — what must be true about data when the entity first exists.
5. **Residency ensures** (`in <InitialState> ensure ...`) — while-in-state truth.

No special "construction constraint" form is needed. `to <InitialState> ensure` is the natural construction-time rule: it fires when the entity enters the initial state, which is exactly what construction does.

**Compiler enforcement:**
- **`RequiredFieldsNeedInitialEvent`:** Precept has required fields (non-optional, no default) but does not declare an initial event — construction cannot produce a valid initial version.
- **`InitialEventMissingAssignments`:** Initial event does not assign all required fields that lack defaults — post-construction state may violate constraints.

**Design rationale:** Construction goes through the full event pipeline because entities must satisfy their constraints from the moment they exist. A parameterless construction path cannot enforce business invariants at intake. By modeling construction as an event, the language reuses all existing machinery — guards can discriminate construction routing, ensures validate args, `reject` can refuse intake, and the caller uses the same pattern matching they use for every event.

### Inspection as a first-class operation

Inspection is not a reporting layer — it is a fundamental language operation. It must have the same depth as event execution: guard evaluation, exit actions, mutations, entry actions, computed field recomputation, and constraint evaluation — all executed on a working copy without committing. The answer to "what would happen?" must always be available, from any state, for any event, and must be honest. Inspectability is what makes the governance contract trustworthy — you can always ask, and the language guarantees the answer matches what execution would do.

### State entry and exit actions

`to <State> -> ...` and `from <State> -> ...` remain part of the language for automatic state-bound mutation.

### Stateless precepts

Stateless precepts are first-class.

They may declare:

1. Fields.
2. Rules.
3. Root editability.
4. Events.
5. Stateless event hooks.

### Stateless event hooks

The language includes:

```precept
on Deposit -> set Balance = Balance + Deposit.Amount
```

This form gives stateless entities meaningful event-driven behavior without inventing a fake state machine.

### Field Access Modes

The language declares field access mode through a two-layer composition model:

**Layer 1 — field-level baseline (`writable` modifier):**

`writable` is a trailing flag modifier on field declarations (same slot as `optional`, `nonnegative`, etc.). It marks the field as directly editable by default across all states. Fields without `writable` default to read-only (D3 baseline preserved).

```precept
field Amount as money writable
```

`writable` on a computed field is a compile error. `writable` on an event argument is a compile error.

**Layer 2 — state-level overrides (`in <State> modify|omit`):**

The two-verb system governs per-state field visibility:

| Verb | Meaning | Update API | Fire pipeline (`set`) |
|------|---------|------------|----------------------|
| `omit` | Field structurally absent from the state's data shape | Not accessible | Blocked (compile error) |
| `modify ... readonly` | Field present and readonly | Read only | Allowed |
| `modify ... editable` | Field present and editable | Read + write | Allowed |

State-level always wins over the field-level baseline for the specific (field, state) pair.

The surface includes:

1. `field X as T writable` — sets X's baseline to writable across all states.
2. `in <State> modify <Fields> readonly|editable [when Guard]` — overrides the baseline for specific (field, state) pairs.
3. `in <State> omit <Fields>` — structurally excludes fields from a state (no guard, no adjective).
4. Root-level access mode declarations are not valid syntax — use `writable` on the field declaration.

#### D3 per-pair baseline

For any (field, state) pair without an explicit state-level declaration, the effective access mode is the field's baseline: read-only for fields without `writable`, editable for fields with `writable`. This per-pair resolution never turns off. Authors declare field baselines with `writable` and state-level exceptions with `in <State> modify|omit` — no boilerplate required for the common case.

#### Composition rules

1. **Field baseline** — `writable` on a field sets its default to editable for all (field, state) pairs.
2. **D3 default** — fields without `writable` default to read-only for every (field, state) pair unless overridden.
3. **State-level override always wins** — `in <State> modify|omit` overrides the field's baseline for that pair only.
4a. **`editable` and `readonly` are the only guarded access modes** — guarded `editable` upgrades a read-only baseline to editable when the guard holds; guarded `readonly` downgrades a writable baseline to read-only when the guard holds; in both cases the field is always structurally present. `omit` cannot be guarded because conditional structural presence breaks static per-state field maps.
4b. **Guarded `readonly` requires a `writable` baseline** — a guarded `readonly` on a field without `writable` is a compile error (`RedundantAccessMode`); both branches would otherwise resolve to read-only, making the guard vacuous.
4c. **Declarations must change the effective mode** — `in <State> modify F editable` where `F` has `writable` and `in <State> modify F readonly` where `F` lacks `writable` are both compile errors (`RedundantAccessMode`). A declaration that resolves to the field's baseline changes nothing — it is dead code. This mirrors `RedundantModifier`: Precept refuses declarations that have no effect. `omit` is exempt — it changes structural presence, not mutability, and is never redundant on that axis. `all` forms are exempt — their effective change depends on the current field population, and punishing a broadcast declaration for being partially vacuous would create brittleness as the field set evolves.
5. **`omit` clears on state entry** — value reset to default on any transition into an `omit` state (including cycles); does NOT apply to `no transition`.
6. **`set` validation against target state** — `set` targeting a field `omit`ted in the target state is a compile error; `readonly`/`editable` do NOT restrict `set`.
7. **Contradiction detection** — same (field, state) pair with conflicting modes is a compile error.
8. **`writable` on computed fields or event args** is a compile error.

#### Guarded access modes

`editable` and `readonly` are the two conditional access modes. Guarded `editable` upgrades a read-only baseline to editable when the guard holds; guarded `readonly` downgrades a writable baseline to read-only when the guard holds. In both cases the field is always structurally present — only mutability varies. `omit` cannot be guarded because conditional structural presence (field sometimes exists, sometimes doesn't) breaks static per-state field maps, form rendering, and integration contracts.

```precept
in UnderReview modify AdjusterName editable when not FraudFlag
in Processing modify Amount readonly when Locked
```

When the guard on `editable` is true, the field is editable; when false, it falls back to D3 (read-only). When the guard on `readonly` is true, the field is read-only; when false, it falls back to the field's `writable` baseline. A guarded `readonly` on a field without the `writable` modifier is a compile error (`RedundantAccessMode`) — both branches would resolve to read-only, making the guard vacuous.

#### `modify`/`readonly`/`editable` replaces `write`/`read`

The B4 vocabulary decision (2026-04-28) replaced the `write`/`read` access mode verbs with a verb/adjective split: `modify` is the constraint verb, `readonly` and `editable` are the access mode adjectives. This is a hard break from the original I/O-rooted vocabulary.

**Why `modify`:** `modify` means "to change the form or qualities of." In `in Draft modify Amount readonly`, the declaration changes the access quality of the field in this state. `modify` is a configuration verb parallel to `omit` — both are verbs, both take field targets, both describe an operation on a field's access properties. The verb/adjective separation fixes B2's `editable`-as-verb awkwardness: the verb (`modify`) and the adjective (`readonly`/`editable`) each do exactly one job.

**Why `readonly`/`editable`:** `readonly` is paradox-free with `modify` ("modify to be readonly" — coherent), unlike `fixed` which creates a modify-to-fix tension. `editable` is the natural disposition adjective for writable access — it already appears as the `writable` modifier on field declarations, creating a vocabulary family connection.

**What was replaced:** `write` → `modify ... editable`, `read` → `modify ... readonly`, `omit` → unchanged.

---

## Actions and Mutation Vocabulary

The mutation vocabulary:

1. `set <Field> = <Expr>`
2. `add <CollectionField> <Expr>`
3. `remove <CollectionField> <Expr>`
4. `enqueue <QueueField> <Expr>`
5. `dequeue <QueueField> [into <Field>]`
6. `push <StackField> <Expr>`
7. `pop <StackField> [into <Field>]`
8. `clear <CollectionField>` — empties a collection
9. `clear <Field>` — resets an `optional` field to unset; resets a non-optional field with a `default` to its declared default value; compile error on non-optional fields without a default

Actions are sequenced and read prior writes in the same chain. Each assignment sees the state produced by all preceding assignments in the same row. When a field is reassigned, proof facts about its prior value are invalidated before the new facts are recorded.

That sequencing model must also apply to stateless event hooks and any future modifier-triggered action surfaces.

### Mutation atomicity

All mutations execute on a working copy. Constraints are evaluated against the working copy after all mutations complete. If every constraint passes, the working copy is promoted. If any constraint fails, the working copy is discarded and the caller's state is unchanged. An invalid configuration never exists, even transiently. There is no window between mutation and constraint checking where a partially-committed state with violated rules can be observed.

---

## Outcomes and Semantic Verdicts

The language implies a stable semantic verdict space even though this document does not define the runtime API.

The future compiler and runtime model must be able to distinguish at least:

1. **Successful transition.** Event fired; state changed; instance updated.
2. **Successful no-transition event.** Event fired; in-place mutations committed; no state change. This is a deliberate design allowing in-place data changes to be event-driven without triggering entry/exit actions.
3. **Explicit rejection.** An authored `reject` row matched — a designed prohibition, not a data constraint failure.
4. **Constraint failure.** Mutations would violate a rule or ensure; rolled back.
5. **Unmatched routed event.** Transition rows exist for the event but all guards failed — an instance data condition.
6. **Undefined event surface.** No transition rows defined for this event in the current state — a definition gap.
7. **Successful direct update.** Field write committed.
8. **Access mode failure.** Patch targets a field not editable in the current state — either `readonly` (read-only) or `omit` (structurally absent).
9. **Invalid input failure.** Patch is structurally malformed.

These distinctions are semantically significant, not just diagnostic convenience:

- **Rejection vs constraint failure.** Rejection is an authored decision; constraint failure is a data truth violation. They require different responses from callers and different diagnostic framing.
- **Unmatched vs undefined.** Undefined means no routing surface exists (a definition gap the author should address); unmatched means routing exists but the current data doesn't satisfy any guard (an instance data condition the caller can address).
- **Transition vs no-transition.** Both are successes. No-transition events execute mutations without state change — a meaningful event-driven pattern, not a degenerate case.

**Construction outcomes.** Entity construction via the initial event produces the same outcome space. All event outcomes are valid at construction: `Transitioned` (construction-time routing to a different state), `Applied` (stayed in initial state), `Rejected` (business rejection at intake), `ConstraintsFailed` (data truth violation at intake), `Unmatched` (guarded initial rows, none matched). `UndefinedEvent` cannot occur — the compiler guarantees the initial event exists. The caller uses the same pattern matching for construction that they use for every event.

**Restoration outcomes.** Entity restoration from persisted data produces a distinct outcome space: successful restoration (data valid, constraints passed), constraint failure (persisted data violates current definition's rules/ensures), or invalid input (structural mismatch — undefined state, unknown fields, type mismatch). Restoring an entity in an invalid state is not allowed — the governance guarantee applies from the moment an entity is loaded, not just when it is mutated. Future migration logic runs before constraint evaluation, transforming persisted data to conform to the current definition.

### Constraint violation subject attribution

When a constraint is violated, the language must support semantic subject attribution — not just the violation message, but what the violation is about.

Every constraint has both **semantic subjects** (the fields or args the constraint references) and a **scope** (why this constraint exists — which rule, which state ensure, which event ensure).

The four constraint kinds have distinct attribution:

1. **Event ensures** target event arguments plus the event scope. The user provided those args.
2. **Rules** target directly referenced fields plus the definition scope. The runtime does not reverse-map through mutations — if the author wants arg-level feedback, they write an event ensure.
3. **State ensures** target directly referenced fields plus the state scope (with anchor: in, to, or from).
4. **Transition rejections** target the event as a whole — this is an authored routing rule, not a data constraint.

Computed fields referenced in constraints are also targets, with transitive expansion to the concrete stored fields they depend on.

This attribution model is a language-level requirement because it flows from how the language distinguishes constraint scopes. The consumer decides rendering; the language provides the semantic structure.

---

## Modifier System

The modifier system is a major language expansion.

### Current precedent

The language already has declaration-attached modifiers in two areas:

**State modifiers:**
- `initial` — marks the starting state. `state Draft initial`.

**Event modifiers:**
- `initial` — marks the construction event (v2). `event Create(...) initial`.

**Field modifiers:**
- `optional` — field may have no value (v2, replaces `nullable`).
- `default <value>` — default value when not explicitly set.
- Constraint keywords: `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`, `ordered`.

Field modifiers are already a rich surface — constraints are declaration-attached structural properties that the compiler enforces. The modifier system expansion extends this pattern to states and events with structural, semantic, and severity modifiers.

### Planned direction

The language supports declaration-attached modifiers across:

1. States.
2. Events.
3. Fields.
4. Rules.
5. Potentially the precept declaration itself.

### Modifier categories

The combined direction from issues #58 and #86 is:

1. Structural modifiers: compile-time-provable properties such as lifecycle shape, one-write behavior, entry behavior, or terminality.
2. Semantic modifiers: intent and tooling meaning such as success, error, sensitive, audit, or deprecated.
3. Severity modifiers: language-level control over how certain declarations surface as warnings versus hard invariants.

### State graph analysis requirement

Many structural modifiers are not simple keyword checks — they require the compiler to build and reason over the **full state transition graph** at compile time. This is a first-class language requirement, not an optional optimization.

The graph is constructed from declared states, events, and transition rows. The compiler must support at least these graph reasoning capabilities:

1. **BFS/DFS reachability from initial.** Required to detect unreachable states (C48) and to define the reachable state set that other modifiers reason over. `initial` provides the root.
2. **Terminal state identification.** States with no outgoing transition rows. Required to anchor path analysis and to validate `terminal` modifier declarations.
3. **Dead-end state detection.** Non-terminal states where all outgoing rows reject or produce no-transition. These have transition machinery that never succeeds — likely authoring mistakes (C50).
4. **Incoming/outgoing edge analysis.** Per-state: which events fire into this state, which events fire out. Required for `guarded` (all incoming transitions have guards), `entry` (event fires only from initial), `isolated` (event fires from exactly one state), `universal` (event fires from every reachable non-terminal state).
5. **Dominator analysis.** Required for `required`/`milestone` — the modifier asserts that all initial→terminal paths must visit this state. Dominator analysis (O(V+E) via Lengauer-Tarjan) determines whether a state is on every such path.
6. **Reverse-reachability.** Required for `irreversible` (no path from this state back to any ancestor state in the initial→forward ordering) and `sealed after <State>` (no mutation after the named state is entered — requires reachability analysis from the named state forward).
7. **Row-partition analysis.** Required for `writeonce` (field set at most once across all reachable transition rows) and `sealed after` (no row reachable after the named state assigns to the field).
8. **Outcome-type analysis.** Per (state, event) pair: do all rows produce `transition`? `no transition`? All `reject`? Required for `advancing` (every success is a state transition), `settling` (every success is no-transition), `completing` (transitions only to terminal states), `absorbing` (event handlers never transition out), and for existing diagnostics like C51 (reject-only pairs) and C52 (events that never succeed).

**Overapproximation rule.** Structural graph analysis treats all edges as traversable regardless of `when` guards — it overapproximates reachability. This is sound: structural guarantees cannot account for guard-dependent path selection because guard satisfaction depends on runtime data. A modifier that claims "all paths visit this state" means all *structurally declared* paths, not all guard-satisfiable paths. This is the correct tradeoff for compile-time analysis.

**Interaction with existing diagnostics.** The graph analysis that modifiers require is an extension of the analysis the compiler already performs for C48 (unreachable states), C49 (orphaned events), C50 (dead-end states), C51 (reject-only pairs), and C52 (events that never succeed). Modifiers do not replace these diagnostics — they make them stronger by adding author-declared intent that the compiler can cross-check against the graph structure.

### v2 initial modifiers

The following modifiers are in scope for the v2 initial release. All apply to states.

**Structural:**

1. `initial` on states — marks the starting state. Already implemented in v1.
2. `initial` on events — marks the construction event. Fires atomically during `Create`. Decided for v2 (see Entity construction).
3. `terminal` — lifecycle exit; no outgoing transitions.
4. `required` — all initial→terminal paths must visit this state (dominator analysis).
5. `irreversible` — no path from this state back to any ancestor state in the initial→forward ordering. Once entered, the lifecycle can only move forward.

**Semantic:**

6. `success` — marks a state as a success outcome.
7. `warning` — marks a state as a warning outcome.
8. `error` — marks a state as an error outcome.

### Future modifiers (deferred)

The following modifiers are planned but deferred beyond the initial v2 release.

**Structural — event modifiers:**

- `entry` — event fires only from the initial state.
- `advancing` — every successful outcome is a state transition.
- `settling` — every successful outcome is no-transition.
- `completing` — transitions only to terminal states.
- `absorbing` — event handlers never transition out.
- `guarded` — all incoming transitions have guards.
- `isolated` — event fires from exactly one state.
- `universal` — event fires from every reachable non-terminal state.

**Structural — field modifiers:**

- `writeonce` — field set at most once across the lifecycle.
- `sealed after <State>` — no mutation after the named state is entered.

**Semantic:**

- `sensitive` — PII/security marking on fields.
- `audit` — audit trail marking on fields/events.
- `deprecated` — migration signal on any declaration.

**Severity (on rules):**

- Severity modifiers that control whether rule violations surface as warnings versus hard errors. Planned as a future concern.

The language supports declaration-attached modifier metadata, validation, and composition rules as a first-class concern.

---

## Static Language Responsibilities

Any compiler implementation must satisfy these language responsibilities.

### Tokenizer responsibilities

The lexical layer must:

1. Tokenize keyword-anchored statements deterministically.
2. Recognize comments and whitespace without indentation semantics.
3. Distinguish string literals, typed constants, list literals, operators, and delimiters.
4. Preserve interpolation boundaries inside quoted literals.
5. Support the future modifier vocabulary without destabilizing existing token families.

### Parser responsibilities

The parser must:

1. Parse all top-level declaration forms described in this document.
2. Parse stateful and stateless precepts in one language.
3. Parse typed constants by shape and hand them to later semantic narrowing.
4. Parse declaration qualifiers such as `in` and `of` without ambiguity.
5. Parse guarded declarations and guarded routing.
6. Parse computed fields and action chains.
7. Parse declaration-attached modifiers when they land.
8. Parse `optional` and `writable` as field modifiers.
9. Parse `is set` / `is not set` as multi-word presence operators in expression contexts.
10. Parse `clear <Field>` as an action in transition rows and hooks.
11. Parse `modify`/`omit` with field targets (singular, comma-separated list, or `all`) after `in <State>`, plus optional `readonly`/`editable` adjective and `when` guard for `modify`.
12. Parse root-level `modify <FieldName>` and reject with diagnostic — use `writable` modifier on the field declaration instead.
13. Reject `null` as a literal or keyword (removed from the language).
14. Reject `nullable` and `edit` with migration diagnostics.

### Typechecker responsibilities

The typechecker must:

1. Resolve declaration identity and scope.
2. Enforce field, event, state, and transition legality.
3. Enforce `optional` field presence rules and `is set`/`is not set` operator restrictions (target must be `optional`; non-optional fields are always set).
4. Enforce collection semantics and inner-type legality.
5. Enforce computed-field dependency legality.
6. Enforce temporal operator compatibility.
7. Enforce strict temporal hierarchy and mediation rules.
8. Enforce business-domain operator compatibility, qualification compatibility, currency pairing, and unit commensurability.
9. Enforce the typed constant admission and narrowing model.
10. Enforce stateless event hook legality.
11. Enforce per-state field access mode rules: two-layer composition model (field `writable` baseline + state-level `in <State> modify|omit` overrides), contradiction detection (conflicting modes on same field/state pair), `set`-into-`omit` validation (compile error), root-level `modify`/`omit` rejection, guarded `omit` rejection, `writable` on computed fields rejected (`ComputedFieldNotWritable`), `writable` on event args rejected (`WritableOnEventArg`).
12. Enforce modifier compatibility and contradiction rules.
13. Enforce numeric lane integrity: reject cross-lane arithmetic (decimal + number), require explicit bridges, and resolve context-sensitive literal types.
14. Enforce collection emptiness guards: reject bare `.min`/`.max`/`.peek` outside a conditional expression that tests `.count > 0` (`UnguardedCollectionAccess`); reject `dequeue`/`pop` without the same proof (`UnguardedCollectionMutation`). Proof sources: conditional guard or `mincount` constraint.
15. Enforce choice literal membership in ordered choice comparisons.
16. Enforce integer-lane requirement for string slicing parameters (`left`, `right`, `mid`).
17. Enforce non-negative exponent for `pow(integer, integer)` when the exponent is a literal.
18. Enforce `sqrt` number-lane restriction: reject `sqrt(decimal)` — authors use `sqrt(approximate(value))`.
19. Enforce `clear` target legality: `optional` field resets to unset; non-optional field with `default` resets to default; non-optional field without `default` is a compile error.

### Graph analysis responsibilities

The compiler must build and reason over the state transition graph to support structural diagnostics and modifier enforcement:

1. BFS/DFS reachability from the initial state.
2. Terminal, dead-end, and unreachable state identification.
3. Orphaned event detection and reject-only pair analysis.
4. Incoming/outgoing edge analysis per state.
5. Dominator analysis for path-obligation modifiers.
6. Reverse-reachability for irreversibility and seal-point modifiers.
7. Row-partition analysis for write-constraint modifiers.
8. Outcome-type analysis per (state, event) pair.

The graph analysis surface overapproximates reachability — all declared edges are treated as traversable regardless of guard conditions. This is sound and correct for compile-time structural reasoning.

### Proof-system responsibilities

The proof layer must be able to support the language's proof-bearing claims.

That includes:

1. **Numeric interval reasoning.** Field constraints, rules, and guards contribute provable numeric ranges. The proof system tracks these ranges through assignment chains.
2. **Relational reasoning** over numeric expressions involving multiple fields.
3. **Divisor safety.** Two-tier: proven-zero divisors are hard errors; divisors with no compile-time nonzero proof are obligation diagnostics requiring the author to supply a constraint (e.g., `nonzero`, `positive`, a rule, or a guard).
4. **Non-negative proof obligations** such as `sqrt` inputs and `pow(integer, integer)` exponents. The compiler requires a provable non-negative path — via `nonnegative` constraint, a rule, an ensure, or a guard — before accepting the expression.
5. **Assignment range impossibility.** An assignment expression provably outside the target field's constraint range is a compile-time error.
6. **Contradictory rule detection.** When two rules' ranges are provably incompatible — no value can satisfy both simultaneously — the compiler reports the contradiction.
7. **Vacuous rule detection.** When a rule is provably always true given field constraints, the compiler reports it as tautological.
8. **Dead guard detection.** A guard provably always false means the row or block can never execute.
9. **Tautological guard detection.** A guard provably always true means the `when` clause has no effect.
10. **Compile-time rule enforcement against defaults.** Rules and initial-state ensures are checked against default field values at compile time. A definition where default values violate a declared rule is rejected before any instance exists.
11. **Sharpening of reachability and routing diagnostics** from proven-dead guards.
12. **Structured proof attribution** suitable for hover, diagnostics, and agent consumption — every proven range carries the constraints and rules that contributed to it.

### Proof philosophy

The proof layer is governed by these requirements, which are language-level commitments, not implementation preferences:

1. **Soundness over completeness.** The proof layer must never claim an expression is safe when it is not. Every proof path must return a provably correct result or conservatively decline. False negatives (missed proofs) cause author friction — the author must supply additional constraints. False positives (wrong "safe" claims) cause runtime failures. The language always chooses the safe direction.

2. **Proven violations only.** The language reports what is definitively broken, not what might be broken. Flagging possible violations turns the compiler into a nag that trains authors to ignore warnings. Flagging only proven violations makes it a trusted guide — when it speaks, it is right.

3. **Opaque solvers are rejected on principle.** The language's proof reasoning must be legible — to authors, to tooling, and to AI agents. Proof witnesses must be structured data, not opaque solver traces. If the compiler cannot prove safety, it says so explicitly — the author is never confronted with an unexplainable verdict. This is why SMT/Z3 solvers are excluded even when they could prove more: opaque proof witnesses violate the inspectability commitment.

4. **One file, complete proof facts.** All proof facts derive from the `.precept` definition. No external oracle, no hidden configuration, no side channel. The proof engine's knowledge boundary is the file boundary.

5. **Truth-based diagnostic classification.** Proof outcomes are classified into three categories: *proved dangerous* (the compiler can demonstrate a violation), *proved safe* (the compiler can demonstrate correctness), and *unresolved* (the compiler cannot determine either). These categories map to distinct author actions: fix a proven violation, rely on proven safety, or supply additional constraints to help the compiler. Diagnostics are classified by proof outcome, not by syntax shape.

6. **Proof attribution is required, not optional.** Every proven range must carry its source attribution — the field constraints, rules, and guards that contributed. Authors must see what the engine proved, what it could not prove, and why. Proof results flow as structured data, not parsed prose — tooling and agents consume the proof model directly, never by parsing diagnostic message text.

7. **Sequential proof flow.** Actions in a chain are sequenced — each subsequent action sees the proof state left by all preceding actions. When a field is reassigned, prior proof facts about that field are invalidated before the new assignment's facts are stored. This is a language semantic that ensures proof reasoning tracks the actual mutation sequence.

---

## What This Means For The Future Compiler

Any clean-room compiler for Precept must be able to serve a language with all of the following characteristics at once:

1. A flat keyword-anchored grammar with no indentation semantics.
2. Stateful and stateless entity definitions in the same language.
3. Two quoted literal forms (strings and typed constants) with interpolation.
4. A mixed primitive, temporal, and business-domain type system with a closed type vocabulary.
5. Domain-sensitive operator tables with unit cancellation.
6. Explicit contextual truth via rules and ensures with mandatory rationale.
7. First-match routing plus collect-all validation as distinct semantic surfaces.
8. Proof-backed diagnostics for proven semantic impossibilities — sound, inspectable, and attribution-bearing.
9. A growing declaration modifier system with structural, semantic, and severity categories.
10. An execution model with no loops, no branches, and no reconverging flow — enabling single-pass analysis.
11. Atomic mutation semantics where invalid configurations never exist, even transiently.
12. Expression purity as a language guarantee, not a caller convention.
13. Full inspectability: every operation is previewable, every proof is explainable, every verdict carries its reasoning.
14. Constraint violation subject attribution that semantically identifies what a violation is about.
15. Semantic distinction between rejection (authored prohibition), constraint failure (data truth violation), unmatched routing (instance data), and undefined routing (definition gap).
16. A three-lane numeric type system (integer, decimal, number) with explicit bridge functions, context-sensitive literal typing, and compile-time lane integrity enforcement.
17. Static completeness: if a precept compiles without diagnostics, no type errors occur at runtime.
18. Totality: every expression evaluates to a result or a definite error — no silent NaN, Infinity, or null propagation.
19. No `null` literal — `optional` fields, `is set`/`is not set` presence operators, and `clear` replace all null-based patterns.
20. Per-state field access modes (`modify`/`readonly`/`editable`/`omit`) and field-level `writable` baseline modifier — two-layer composition model: `writable` sets the field's default across all states; `in <State> modify F readonly|editable` or `in <State> omit F` overrides per-pair; D3 (read-only) is the fallback for fields without `writable`; `writable` on computed fields or event args is a compile error. Both `modify` and `omit` support singular, comma-separated, and `all` field targets.

That is the language target. The compiler architecture may change completely, but the language contract above is what the new system must honor.

---

## Out Of Scope For This Document

This document does not decide:

1. Public runtime API names.
2. Internal model types.
3. Pipeline staging.
4. Tooling payload shapes.
5. Engine execution internals.
6. Persistence or serialization APIs.

It only states what the language is and what the compiler must be able to understand, validate, and prove.