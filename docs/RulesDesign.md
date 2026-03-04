# Rules Design Notes

Date: 2026-03-03

Status: **Design phase — not yet implemented.**

## Overview

Rules are declarative boolean constraints that protect data integrity across a state machine. They use the existing expression grammar (`&&`, `||`, `!`, comparisons, arithmetic, parentheses) and are checked automatically — authors declare constraints once rather than repeating guards in every transition.

## Motivation

The current protection model is event-scoped guard expressions. Every transition that modifies a field must independently guard it. This scales poorly: add a new event that debits `Balance`, and the author must remember to add the guard. If they forget, the invariant "Balance must not go negative" is silently violated.

Rules elevate data contracts from per-transition guards to declarations. Guards remain for **routing logic** (which branch fires). Rules handle **data integrity** (what must always hold).

## Rule Positions

One keyword (`rule`), one expression grammar, four attachment positions:

| Position | Scope (what identifiers are visible) | When checked | Purpose |
|---|---|---|---|
| **Field rule** (indented under a scalar field declaration) | The declaring field only | After fire commits all sets | Single-field value bounds |
| **Top-level rule** (unindented, after field declarations) | All instance data fields declared above | After fire commits all sets | Cross-field data invariants |
| **State rule** (indented under a state declaration) | All instance data fields | On entry to the state (including self-transitions) | State entry contracts |
| **Event rule** (indented under an event declaration) | Event arguments only | Before guard evaluation | Input validation |

### Syntax

```text
rule <BooleanExpr> "<Reason>"
```

Identical in all four positions. The expression grammar is the same as guards and `set` expressions — no new operators, no new syntax.

### Examples

```text
machine OrderWorkflow

# Field rules — single-field bounds, indented under the field
number Balance = 0
  rule Balance >= 0 "Balance must not go negative"
number Quantity = 0
  rule Quantity >= 0 "Quantity must be non-negative"

number UnitPrice = 0
number TotalPrice = 0

# Top-level rules — cross-field invariants, placed after referenced fields
rule Quantity * UnitPrice == TotalPrice "Price must be consistent"

# State rules — entry contracts
state Draft initial
state Paid
  rule Balance == 0 "Must have zero balance to be Paid"

# Event rules — input validation (event args only)
event Checkout
  number PaymentAmount
  rule PaymentAmount > 0 "Payment must be positive"

from Draft on Checkout
  set Balance = Balance - Checkout.PaymentAmount
  transition Paid
```

## Locked Decisions

### Expression grammar

Rules use the same expression grammar as guards and `set` expressions. All operators work: `&&`, `||`, `!`, `==`, `!=`, `<`, `<=`, `>`, `>=`, `+`, `-`, `*`, `/`, `%`, parentheses. One grammar, one evaluator, four scopes.

### Field rule scope restriction

A field-indented rule may only reference the field it is declared under. If the expression references any other identifier, the parser rejects it with a clear error: *"Field rule may only reference its own field; use a top-level rule for cross-field constraints."*

Rationale: a rule indented under a field implies it is *about* that field. Referencing other fields from that position is misleading. Cross-field constraints belong in top-level rules where the multi-field nature is visible.

### Top-level rule placement

Top-level rules may appear anywhere in the file after all the fields they reference are declared. No forward references. The parser enforces this with the same reference validation used for `from ... on ...` blocks.

Authors choose placement by locality — a single-field rule can be written as a field rule or a top-level rule immediately after the field. Cross-field rules are placed after the last field they reference.

### Event rule scope restriction

Event rules may only reference event argument identifiers. They cannot see instance data fields. This keeps event rules focused on input validation ("is this call well-formed?") and avoids overlap with guards ("which branch fires given the current state?").

Rationale: if event rules could reference instance data, there would be two places to put a condition like `Amount <= Balance` — as an event rule or as a guard — violating the single-obvious-style principle. With the scope restriction, event rules validate *inputs*, guards evaluate *routing*, and field/top-level rules protect *results*.

### State rules and self-transitions

State rules check on **entry**, not on remaining. A `no transition` outcome does not trigger state rule checks — the machine is not entering the state, it's staying.

A **self-transition** (e.g., `transition Active` when already in `Active`) **does** trigger state rule checks. The `transition` keyword explicitly targets the state, so entry rules apply.

### Null handling

Rules follow the same strict null semantics as the existing expression evaluator:

- `null >= 0` → expression failure → rule violation
- `null + 1` → expression failure → rule violation
- `null && true` → expression failure → rule violation

No special null-skipping behavior. If a nullable field is `null` and a rule references it in a comparison or arithmetic expression, the rule fails.

Authors who want nullable fields to pass a rule must handle null explicitly:

```text
number? Balance
  rule Balance == null || Balance >= 0 "Balance must be null or non-negative"
```

This is consistent with the DSL's "strict over permissive" principle and the existing null handling in guards and `set` expressions. The system already pushes authors toward non-nullable fields with defaults; rules inherit that same posture.

### Multiple violations — collect all

If multiple rules fail on a single fire, **all** violated rules are reported in the `Reasons` list, not just the first. This gives the caller a complete picture rather than requiring iterative fix-and-retry.

Consistent with the existing `Reasons` collection on inspection/fire results and with the determinism contract.

### Outcome kind

Rule violations produce `Blocked` outcome with the rule's reason string. No new outcome kind.

From the caller's perspective, "the system won't let me do this" is the same whether it's a guard rejection or a rule violation. The reason text distinguishes them when debugging. Adding a new outcome kind would force every consumer to handle another case for the same semantic result.

### Collection fields in rules

Rules may reference collection properties that are valid in guard expressions:

- `.count` (all collection types, returns number) — valid in rules
- `contains` (infix boolean operator) — valid in rules

```text
queue<string> ApprovalChain
rule ApprovalChain.count <= 10 "Too many approvers"
```

Element-returning properties (`.min`, `.max`, `.peek`) are **excluded** from rules, consistent with their exclusion from guard expressions. Failure on empty is ambiguous in rule context.

### Compile-time validation of default values

Field defaults are literals — fully known at compile time. The compiler evaluates all top-level rules and field rules against default values and fails compilation if any rule is violated:

```text
number Balance = -5
  rule Balance >= 0 "Must be non-negative"
# Compile error: rule "Must be non-negative" violated by default value
```

For state rules, the compiler knows the initial state and checks its entry rules against the default data:

```text
number AmountPaid = 0
state Paid initial
  rule AmountPaid > 0 "Must have paid something"
# Compile error: state rule on initial state "Paid" violated by default data
```

Caller-supplied overrides at `CreateInstance` are validated at runtime.

## Evaluation Pipeline

```text
Event rules  →  Guard evaluation  →  Set execution  →  Field/top-level rules  →  State rules
```

1. **Event rules** — checked first, before any guard evaluation. If any event rule fails, fire is rejected immediately. Cheapest rejection point.
2. **Guard evaluation** — existing branch routing logic. Unchanged.
3. **Set execution** — existing atomic batch execution on working copy. Unchanged.
4. **Field rules and top-level rules** — checked against the post-set working copy. If any fail, all set mutations are rolled back (consistent with atomic batch semantics). All violations collected.
5. **State rules** — if the outcome is a state transition (including self-transition), the target state's entry rules are checked against the post-set data. If any fail, rollback. Not checked for `no transition`.

### Inspect semantics

`Inspect` already simulates guard evaluation on scratch data. With rules:

- Event rules are checked (event args are available to inspect)
- If inspect simulates set assignments on a scratch copy, field/top-level/state rules can be checked against the simulated result
- This gives a full preview: "would this fire succeed or fail, and why?"

## Compile-Time Null Safety (Cross-Cutting — Not Rules-Specific)

### Principle

**Nullable means "you must prove it's not null before using it in any operation that doesn't accept null."**

The only operations that accept null are `== null`, `!= null`, and assignment to a nullable target. Everything else — arithmetic, comparison, boolean logic, string concat, collection mutation — requires the author to narrow the value first. This applies uniformly across all expression sites: guards, `set` expressions, rules, and collection mutations.

This is the same principle as C#'s nullable reference types or TypeScript's strict null checks, applied to the DSL's expression surface.

### Current state

The language server already implements null-narrowing for guards:
- `if RetryCount != null && RetryCount > 0` — narrowed correctly, no diagnostic
- `if RetryCount > 0` — flagged: `operator '>' requires numeric operands` (because `RetryCount` is `number?`)
- `set Value = RetryCount` inside a `if RetryCount != null` branch — accepted (narrowed by guard)
- `set Value = RetryCount` without guard — flagged: type mismatch (nullable → non-nullable)

Cross-branch narrowing (applying guard negation to `else`/`else if` branches) is a known gap.

### Goal: uniform enforcement everywhere

Every expression site should apply the same null analysis:

| Site | Expression type | Check |
|---|---|---|
| Guard (`if`/`else if`) | Boolean | Nullable in comparison/arithmetic/boolean without narrowing → error |
| `set` RHS | Value | Nullable in arithmetic/concat without narrowing → error; nullable → non-nullable target → error |
| Field rule | Boolean | Nullable without null handling → error |
| Top-level rule | Boolean | Nullable without null handling → error |
| State rule | Boolean | Nullable without null handling → error |
| Event rule | Boolean | Nullable event arg without null handling → error |
| Collection mutation value | Value | Nullable value into non-nullable-inner-type collection → error |
| `contains` operand | Value | Nullable is valid here (`contains null` is a legitimate question) |

### Error vs. warning classification

**Error-level (provably wrong at runtime):**
- Nullable in arithmetic (`+`, `-`, `*`, `/`, `%`) without narrowing
- Nullable in comparison (`<`, `<=`, `>`, `>=`) without narrowing
- Nullable in boolean logic (`&&`, `||`) as a non-check operand without narrowing
- Nullable in string concat without narrowing
- Nullable assigned to non-nullable `set` target without narrowing
- Nullable value in `add`/`enqueue`/`push` on a non-nullable-inner-type collection
- Nullable in rule expression without explicit null handling

**Warning-level (likely unintentional):**
- Rule on a nullable field without `== null ||` escape — the rule will fail whenever the field is null, which may be surprising

### Correct patterns

```text
number? RetryCount

# Guard — narrow before use
if RetryCount != null && RetryCount > 0        # ✓
if RetryCount > 0                               # ✗ error

# Set — narrow by guard context
if RetryCount != null
  set Value = RetryCount                        # ✓ (narrowed by guard)
set Value = RetryCount                          # ✗ error (no guard)

# Field rule — explicit null handling
number? Balance
  rule Balance == null || Balance >= 0 "..."    # ✓
  rule Balance >= 0 "..."                       # ✗ error

# Event rule — same pattern
event Submit
  number? Priority
  rule Priority == null || Priority > 0 "..."  # ✓
  rule Priority > 0 "..."                       # ✗ error
```

### Implementation scope

The null safety principle is **cross-cutting** — it applies to guards, sets, rules, and collection mutations uniformly. It should be implemented as a shared analysis pass in the language server, not as separate logic per expression site. The expression evaluator and static analyzer share the same narrowing infrastructure.

## Future: Static Analysis Opportunities

The rule model enables language-server analysis that can be explored in future work:

- **Warn** when a transition targets a state whose entry rules cannot possibly be satisfied by the transition's `set` assignments
- **Warn** when a `set` assignment can provably violate a field rule or top-level rule
- **Hint** when a guard already covers what a rule would catch (redundant but harmless)
- Cross-branch null narrowing (applying guard negation to `else`/`else if` branches and across if/else-if chains)

These are tooling enhancements, not runtime behavior, and do not need to be designed or implemented with the initial rule system.
