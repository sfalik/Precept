# Rules Design Notes

Date: 2026-03-03

Status: **Implemented.** Parser, compiler, runtime, and language server all support rules. See implementation in `src/StateMachine/Dsl/StateMachineDslParser.cs`, `StateMachineDslRuntime.cs`, and `tools/StateMachine.Dsl.LanguageServer/SmDslAnalyzer.cs`. Covered by `test/StateMachine.Tests/DslRulesTests.cs`.

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

A field-indented rule may only reference the field it is declared under, its dotted properties (e.g., `.count`), and literal constants. If the expression references any other field identifier, the parser rejects it with a clear error: *"Field rule may only reference its own field; use a top-level rule for cross-field constraints."*

What is permitted in a field rule expression:
- The declaring field itself (`Balance`, `Tags`)
- Dotted properties of the declaring field (`.count`, `contains`)
- Literal constants (`0`, `"Admin"`, `true`, `null`)

What is rejected:
- Any other field identifier

Examples:

```text
number Balance = 0
  rule Balance >= 0 "Must be non-negative"                   # ✓ own field + literal

set<string> RequiredRoles
  rule RequiredRoles contains "Admin" "Must include Admin"   # ✓ own field + literal
  rule RequiredRoles.count <= 10 "Too many roles"            # ✓ own field property + literal
  rule RequiredRoles.count <= MaxRoles "..."                 # ✗ references another field
```

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

Collection field rules (indented under the collection declaration) follow the same scope restriction as scalar field rules — they may reference the declaring collection, its properties, and literal constants:

```text
set<string> Approvers
  rule Approvers contains "Admin" "Must include Admin approver"  # ✓ field rule
  rule Approvers.count >= 1 "Need at least one approver"         # ✓ field rule

queue<string> ApprovalChain
rule ApprovalChain.count <= 10 "Too many approvers"              # ✓ top-level rule
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

### Compile-time validation of event argument defaults

Event arguments may declare literal defaults. If an event has rules, the compiler evaluates those rules against the default values for any defaulted arguments:

```text
event Submit
  number Priority = 0
  rule Priority > 0 "Priority must be positive"
# Compile error: default value 0 violates event rule
```

Non-defaulted required arguments cannot be checked at compile time (their values are not known until fire-time).

### Compile-time validation of collection rules at creation

Collections always start empty (locked design decision). The compiler knows `.count` is 0 and `contains X` is false for any X at creation. Collection rules are evaluated against this known initial state:

```text
set<string> Approvers
  rule Approvers.count >= 1 "Need at least one approver"
# Compile error: collection starts empty, rule is violated at creation
```

### Compile-time validation of literal set assignments

When a `set` assignment's right-hand side is a literal, the compiler can check whether the resulting value would violate any field rule or top-level rule:

```text
number Balance = 100
  rule Balance >= 0 "Must be non-negative"

from Active on Reset
  set Balance = -1
  transition Active
# Compile error: literal -1 violates rule "Must be non-negative" on Balance
```

This applies only to literal RHS values — expressions like `set Balance = Balance - Amount` cannot be statically resolved. But literal assignments (`set X = 0`, `set X = null`, `set X = ""`) are common enough that catching violations at compile time is valuable.

### Compile-time detection of untargeted states with entry rules

If a state has entry rules but no `transition` in the entire machine targets it, the rules are dead code. The compiler emits a warning:

```text
state Completed
  rule AmountPaid == TotalDue "Must be fully paid"
# Warning: no transition targets Completed — entry rules are never checked
```

This is a warning, not an error — the state might be targeted in a future edit.

### Compile-time detection of tautological rules

A rule expression that is trivially always true provides no protection. The compiler emits a hint when a rule is constant-foldable to `true`:

```text
boolean IsActive = true
  rule IsActive == true || IsActive == false "..."   # Always true — hint
```

The existing expression evaluator can detect these via constant folding over literal-only expressions.

### Rules do not have access to current state

Rule expressions cannot reference the current state of the machine. State-awareness is expressed through state rule *attachment* (indenting under a state declaration), not through a variable or identifier.

If you need "when in state X, field Y must be Z", use a state rule on state X — don't try to express it as a top-level rule with a state reference. This keeps the separation clean: data rules constrain data, state rules constrain entry.

### Rules and `from any` transitions

`from any on Event` expands to every declared state. If the transition targets a state with entry rules, those rules apply regardless of whether the transition was declared as `from any` or `from SpecificState`. The pipeline checks the *target state*, not the source of the transition declaration.

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

### Preview snapshot includes rule metadata

The `SmPreviewSnapshot` record (in `SmPreviewProtocol.cs`) carries two optional fields populated by `BuildSnapshot` in `SmPreviewHandler.cs`:

- **`ActiveRuleViolations`** (`IReadOnlyList<string>?`): flat list of all rule reason strings currently violated by the live instance's data and current state. Populated by `DslWorkflowDefinition.EvaluateCurrentRules(instance)`, which internally calls `EvaluateDataRules` (field + top-level rules) and `EvaluateStateRules` (current state entry rules). `null` when no rules are violated.
- **`RuleDefinitions`** (`IReadOnlyList<SmPreviewRuleInfo>?`): all rule declarations in the machine — field rules (scope `"field:<Name>"`), top-level rules (scope `"topLevel"`), state rules (scope `"state:<StateName>"`), and event rules (scope `"event:<EventName>"`). `null` when the machine has no rules.

`SmPreviewResponse` also carries `Errors` (`IReadOnlyList<string>?`): when a fire is rejected, `Errors` contains the full list of all failure reasons (not just the first). The existing `Error` field is preserved with the first reason for backward compatibility.

The webview uses `activeRuleViolations` to render an amber violations banner above the data table, `ruleDefinitions` to annotate data rows with field rule icons and tooltips and to show state rule badges, and `errors` to display all fire failure reasons in the feedback toast.

## Nullable Behaviour in Rules

Rules inherit the general compile-time null safety model (cross-branch narrowing, strict null checking across all expression sites). No rules-specific null semantics are needed — the same infrastructure applies.

### How it works for each rule position

**Field rules** — the declaring field's nullability is known. If the field is nullable, the rule expression must handle null explicitly:

```text
number? Balance
  rule Balance == null || Balance >= 0 "Balance must be null or non-negative"  # ✓
  rule Balance >= 0 "..."                                                       # ✗ compile-time error
```

The language server flags the second form at parse/analysis time because `Balance` is `number?` and `>=` requires numeric operands.

**Top-level rules** — same analysis. All referenced fields' nullability is known from their declarations:

```text
number? Paid
number TotalDue = 0
rule Paid == null || Paid <= TotalDue "Cannot overpay"   # ✓
rule Paid <= TotalDue "..."                               # ✗ compile-time error (Paid is nullable)
```

**State rules** — same analysis against instance data field declarations.

**Event rules** — same analysis against event argument declarations:

```text
event Submit
  number? Priority
  rule Priority == null || Priority > 0 "Priority must be positive if provided"  # ✓
  rule Priority > 0 "..."                                                         # ✗ compile-time error
```

### Compile-time vs. runtime

The language server catches nullable-without-narrowing errors at **compile time** (as diagnostics in the editor). At **runtime**, if a nullable field is null and a rule expression evaluates it in a non-null-safe operation, the expression fails and the rule violation reason is reported — consistent with guard and set expression failure semantics.

### Practical guidance

The DSL already pushes authors toward non-nullable fields with defaults. For fields that genuinely need to be nullable, the `== null ||` pattern in rules is the correct and only way to express "null is acceptable." This is intentional — the author must decide whether null is a valid state for the constraint.

### Error reporting: line numbers and precise spans

Every diagnostic produced by the parser, compiler, or language server for a rule violation must include:

- The **1-based source line number** of the `rule` statement.
- A **character-precise range** (start column, end column) so the editor renders squigglies under the specific offending token or sub-expression — not the entire line.

For scope violations (e.g., a field rule referencing another field), the squiggly must underline the **offending identifier**, not the whole rule line. For nullable-without-narrowing errors, the squiggly must underline the **nullable identifier** at the expression site where it is used unsafely. For compile-time default-value violations, the squiggly must underline the **rule keyword or expression** on the rule's source line.

The `DslRule` model record must store enough position metadata (line number, expression start/end columns, reason string start/end columns) to support precise span reporting. The parser must capture these spans during tokenisation of the `rule` line.

This is consistent with how existing diagnostics work for guards, `set` expressions, and other DSL constructs — every error should point the author to the exact location, never just a line.

## Future: Static Analysis Opportunities

The rule model enables language-server analysis that can be explored in future work:

- **Warn** when a transition targets a state whose entry rules cannot possibly be satisfied by the transition's `set` assignments
- **Warn** when a `set` assignment can provably violate a field rule or top-level rule
- **Hint** when a guard already covers what a rule would catch (redundant but harmless)
- **Detect contradictory rules** on the same scope (e.g., `rule X > 100` + `rule X < 0` on the same field). Simple single-variable bound contradictions are tractable; compound expression contradiction detection is not worth pursuing initially.

These are tooling enhancements, not runtime behavior, and do not need to be designed or implemented with the initial rule system.

## Implementation Prompt

The following prompt can be pasted into a new session to implement the rules feature:

---

Implement the rules feature for the state machine DSL as specified in docs/RulesDesign.md. This is a full-stack implementation across parser, model, compiler, runtime, language server, and documentation. Read docs/RulesDesign.md thoroughly before starting — it is the complete design spec.

Summary of what rules are: declarative boolean constraints using the keyword "rule" with syntax "rule BooleanExpr ReasonString". They use the existing expression grammar (same operators, same evaluator as guards and set expressions). They protect data integrity so authors declare constraints once rather than repeating guards in every transition.

There are four rule positions. Field rules are indented under a scalar or collection field declaration, may only reference the declaring field, its dotted properties, and literal constants, and are checked after fire commits all sets. Top-level rules are unindented statements placed after all referenced fields are declared, see all instance data fields declared above, and are checked after fire commits all sets. State rules are indented under a state declaration, see all instance data fields, and are checked on entry to the state including self-transitions but not on no-transition. Event rules are indented under an event declaration, see event arguments only (not instance data), and are checked before guard evaluation.

The evaluation pipeline order is: event rules, then guard evaluation, then set execution, then field and top-level rules, then state rules. If any rules fail at their respective stage, the outcome is Blocked with all violated rule reasons collected in the Reasons list. Field/top-level rule and state rule violations cause atomic rollback of all set mutations, consistent with existing batch semantics.

Key constraints to implement. Field rule scope restriction: parser rejects field rules that reference any identifier other than the declaring field, its properties, or literals. Event rule scope restriction: parser rejects event rules that reference instance data fields. Top-level rules enforce no forward references (same validation as from-on blocks). State rules fire on entry only — no-transition does not trigger them, but self-transitions do. Null handling inherits the general strict null model — nullable in a rule without explicit null check is a compile-time error. Multiple violations are collected into the Reasons list, not short-circuited. Outcome kind is Blocked, no new outcome kind.

Compile-time checks to implement. Validate field rules and top-level rules against field default values at compile time. Validate initial state entry rules against default data at compile time. Validate event rules against event argument defaults at compile time. Validate collection rules against known empty initial state (count is 0, contains is false) at compile time. Validate literal set assignment RHS values against field and top-level rules at compile time. Warn when a state has entry rules but no transition in the machine targets it. Hint when a rule expression is tautological (constant-foldable to true).

Inspect semantics: event rules are checked during inspect. If inspect simulates set assignments on scratch data, field/top-level/state rules should be checked against the simulated result for full preview.

Implementation layers. Model: add a DslRule record to StateMachineDslModel.cs to hold expression, reason string, source line, and position metadata (expression start/end columns, reason start/end columns) sufficient for precise diagnostic spans. Extend DslMachine, DslFieldContract, DslCollectionFieldContract, DslEvent, and state declarations to carry rule lists. Parser: extend StateMachineDslParser.cs to parse "rule Expr Reason" lines in all four positions (field-indented, top-level, state-indented, event-indented). Capture character-precise column spans for the expression and reason string during tokenisation. Enforce scope restrictions at parse time. Compiler: extend DslWorkflowCompiler to store rules on the compiled DslWorkflowDefinition. Implement all compile-time validations (defaults, collection empty state, event arg defaults, literal set assignments, untargeted states, tautologies). Runtime: extend DslWorkflowDefinition.Fire and DslWorkflowDefinition.Inspect to evaluate rules at the correct pipeline stages. Implement atomic rollback on rule violation. Language server: extend SmDslAnalyzer to validate rule expressions with correct scope and null checking. All diagnostics must report line numbers and character-precise ranges so the editor renders squigglies on the exact offending token or sub-expression, not the whole line. Add completions and semantic tokens for rule keyword and expressions.

Tests: add comprehensive tests covering each rule position, scope restrictions, compile-time validations, runtime fire behavior with rule violations, inspect behavior with rules, null handling in rules, collection rules, self-transition state rule triggering, no-transition not triggering state rules, multiple violation collection, and from-any with state rules.

Documentation: update docs/DesignNotes.md DSL Syntax Contract section to include rule syntax. Update README.md DSL Syntax Reference, DSL Cookbook, and Status sections. Update docs/RulesDesign.md status from design phase to implemented. Update `tools/StateMachine.Dsl.VsCode/syntaxes/state-machine-dsl.tmLanguage.json` to add/update grammar patterns for any new keywords or constructs.

Intelligence sync (non-negotiable — do not skip): apply the Intellisense Sync Checklist from `.github/copilot-instructions.md` for every new keyword or construct introduced by this feature. At minimum: add `rule` to `KeywordItems` and `KeywordTokens`; add a context-specific completion branch for `^\s*rule\s+` that offers field/arg identifiers and operators (same scope as `if` guards); ensure `ExpressionLineRegex` in `SmSemanticTokensHandler` matches `rule` lines so identifier references in rule expressions receive `variable` token coloring.

Build with dotnet build from repo root. Run tests in test/StateMachine.Tests/ and test/StateMachine.Dsl.LanguageServer.Tests/. Make sure all existing tests still pass.
