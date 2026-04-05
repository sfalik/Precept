# Expression Language Audit — Precept DSL

**Author:** George (Runtime Dev)  
**Date:** 2026-05-01  
**Status:** Complete (v1)  
**Audience:** GitHub proposal authors, Shane (approval gate)  
**Input artifacts:** `docs/PreceptLanguageDesign.md`, `docs/RulesDesign.md`, `src/Precept/Dsl/`, `samples/` (all 21 files), `docs/research/language/expressiveness/README.md`

---

## Purpose

This audit catalogues what Precept's expression language can and cannot express today. The goal is not to propose implementations — that requires an approved design doc — but to give proposal authors a rigorous, implementation-grounded inventory of where the expression system is limiting real business logic, so GitHub proposal issues are built on facts, not intuition.

---

## 1. Current Expression Surface

### 1.1 Expression Tree Nodes

The evaluator (`PreceptExpressionRuntimeEvaluator`) recognises five node types:

| Node | Description |
|---|---|
| `PreceptLiteralExpression` | Literal value: `string`, `number`, `boolean`, `null` |
| `PreceptIdentifierExpression` | Named reference: bare `Name` or dotted `Name.Member` |
| `PreceptParenthesizedExpression` | Grouping: `(expr)` |
| `PreceptUnaryExpression` | Prefix operator: `!`, unary `-` |
| `PreceptBinaryExpression` | Infix operator: all arithmetic, comparison, logical, `contains` |

There is no function-call node, no ternary node, no range node, no lambda or named-rule reference node.

### 1.2 Operators

**Arithmetic** (both operands must be `number`; or `+` with both `string`):

| Operator | Result type | Notes |
|---|---|---|
| `+` | `number` or `string` | `string + string` = concatenation; `number + number` = addition. No mixed. |
| `-` | `number` | |
| `*` | `number` | |
| `/` | `number` | No divide-by-zero guard. Silent NaN/infinity at runtime. |
| `%` | `number` | |

**Comparison** (operands must be `number`, except `==`/`!=` which accept any matching type pair):

| Operator | Result type | Notes |
|---|---|---|
| `==`, `!=` | `boolean` | Works across all scalar types. Null comparison allowed only with nullable types. |
| `>`, `>=`, `<`, `<=` | `boolean` | Numbers only. String ordering not supported. |

**Logical** (operands must be `boolean`):

| Operator | Notes |
|---|---|
| `&&` | Short-circuit; left narrowing applied to right-operand type check. |
| `\|\|` | Short-circuit; left negation-narrowing applied to right-operand type check. |
| `!` | Prefix only; requires `boolean` operand. |

**Membership:**

| Operator | Signature | Notes |
|---|---|---|
| `contains` | `Collection contains Value` | Collection field on left; value of inner type on right. Collection must be `set`, `queue`, or `stack`. Does **not** work on strings. |

### 1.3 Collection Accessors

These are treated as dotted identifier reads (`Name.member`) by the evaluator; they are injected into the symbol table by the type checker:

| Accessor | Available on | Returns |
|---|---|---|
| `Name.count` | `set`, `queue`, `stack` | `number` |
| `Name.min` | `set` only | inner type |
| `Name.max` | `set` only | inner type |
| `Name.peek` | `queue`, `stack` only | inner type |

### 1.4 Expression Scopes

| Scope | Visible identifiers |
|---|---|
| `invariant` expression | All data fields, collection accessors |
| `in`/`to`/`from` state assert | All data fields, collection accessors |
| `on <Event> assert` | **Event args only** (`ArgName` or `EventName.ArgName`). Data fields are NOT visible. |
| `when` guard | All data fields, `EventName.ArgName`, collection accessors |
| `set` RHS | All data fields (read-your-writes), `EventName.ArgName`, collection accessors |
| `to`/`from` state action `set` RHS | All data fields, collection accessors. Event args NOT available (no event in scope). |

### 1.5 Type System

- Three scalar types: `string`, `number`, `boolean`
- Three collection types with inner type: `set<T>`, `queue<T>`, `stack<T>` where T ∈ {string, number, boolean}
- Nullable modifier: `nullable` on field or event arg declaration
- Null narrowing: `&&` narrows nullable identifiers to non-null on the right-hand side of a compound check containing `X != null`
- No compound types, no enumerations, no date/time, no structured types

---

## 2. Limitations — Categorised by Severity

### 2.1 CRITICAL — Business Logic You Cannot Express At All

---

#### L1 — No ternary / conditional expression in `set` values

**What is missing:** A conditional value expression of the form `Condition ? ValueIfTrue : ValueIfFalse`.

**Impact:** When two transition rows differ only in a single computed value, the entire mutation chain must be duplicated — action by action — across two rows. This is the most frequently-occurring duplication pattern in the sample corpus.

**Sample evidence — `hiring-pipeline.precept` lines 57–58:**

```precept
# These two rows share identical mutations; only the outcome differs.
# Adding conditional value selection would collapse them to one row.

from InterviewLoop on RecordInterviewFeedback when PendingInterviewers contains RecordInterviewFeedback.Interviewer && PendingInterviewers.count == 1
    -> remove PendingInterviewers RecordInterviewFeedback.Interviewer
    -> set FeedbackCount = FeedbackCount + 1
    -> transition Decision

from InterviewLoop on RecordInterviewFeedback when PendingInterviewers contains RecordInterviewFeedback.Interviewer
    -> remove PendingInterviewers RecordInterviewFeedback.Interviewer
    -> set FeedbackCount = FeedbackCount + 1
    -> no transition
```

Hypothetical ternary would collapse this to one row (outcome stays separate — see Steinbrenner's rejected "conditional outcome" proposal which is the right call; ternary applies only to `set` values).

**Further examples:**

- `event-registration.precept` line 56: `set AmountDue = UpdateSeats.Seats * 25` — hard-coded price because there's no `Submit.PricePerSeat` in scope at `UpdateSeats` time. With ternary, an event arg could select between price tiers.
- `subscription-cancellation-retention.precept`: Cannot express "set discount to either standard or premium tier depending on plan length" without a separate transition row per case.
- `insurance-claim.precept` line 61: Two rows share identical mutations; only the guard on `Approve.Amount <= ClaimAmount` differs.

**Hypothetical DSL:**

```precept
from InterviewLoop on RecordInterviewFeedback
    when PendingInterviewers contains RecordInterviewFeedback.Interviewer
    -> remove PendingInterviewers RecordInterviewFeedback.Interviewer
    -> set FeedbackCount = FeedbackCount + 1
    -> (PendingInterviewers.count == 0 ? transition Decision : no transition)
```

> Note: This specific form — conditional outcome inside `->` — was correctly rejected by Steinbrenner's research as conflicting with the first-match mental model. Ternary as a **`set` RHS value expression** is the safe path and the one with precedent.

**Implementation notes:**  
Requires a new `PreceptTernaryExpression(Condition, ThenExpr, ElseExpr)` AST node. Parser: add a `? ... :` infix level above `||`. Type checker: infer kind from `then`/`else` branches; they must produce the same type family. Evaluator: short-circuit evaluation of the active branch. Scope: valid in `set` RHS only initially (not in `invariant`/`assert`/`when` positions — those are boolean expressions; ternary yielding boolean is technically valid but adds complexity without clear use cases at this stage).

**Verdict: `feasible`**  
Clean additive change. No semantic conflict with existing constructs. Has strong cross-language precedent (every mainstream language, LINQ projections, Zod refinements).

---

#### L2 — No `string.length` accessor

**What is missing:** A `.length` property on string fields and event args, analogous to `.count` on collections.

**Impact:** String length constraints are entirely inexpressible. `null`/empty checks work (`Name != null`, `Name != ""`), but minimum and maximum string length cannot be expressed. This is a fundamental data-quality constraint category.

**What you have to do today:**

```precept
# The closest you can get — checks non-empty but cannot enforce max length.
invariant Description != null because "Description is required"
invariant Description != "" because "Description cannot be empty"
# There is NO way to write: invariant Description.length <= 500
```

**What you'd want:**

```precept
invariant Description.length >= 1 because "Description is required"
invariant Description.length <= 500 because "Description must not exceed 500 characters"
on Submit assert Submit.Comment.length <= 1000 because "Comments are limited to 1000 characters"
```

**Affected domains:** Any sample with a freeform text field (all 21 samples have at least one `string` field). Insurance claims, work orders, helpdesk tickets, and reimbursement notes are specifically affected because they accept user-authored text that real implementations would cap.

**Implementation notes:**  
Mirror the collection accessor pattern exactly. In the type checker, inject `FieldName.length` as `StaticValueKind.Number` for every `string` and `string | null` field in `BuildSymbolKinds` and `ValidateRules`. In the evaluator, add a `.length` branch to the `EvaluateIdentifier` method alongside the collection property switch. The evaluator looks up the string value from context and returns its `.Length` cast to `double`. For nullable strings: return `0` when null (mirrors `.count` returning `0` for empty collections) or fail — this is a type policy decision, but `0` is more useful for guards like `.length >= 1`. No new AST node needed; `PreceptIdentifierExpression` with `Member = "length"` is already parseable; only the evaluator and type checker symbol tables need updating.

**Verdict: `feasible`**  
Lowest implementation cost of any gap in this audit. One evaluator branch, symbol table injection. Steinbrenner has already marked this Priority 3.

---

#### L3 — No named / reusable rule expressions

> **Final term:** `rule <Name> when <BoolExpr>`

**What is missing:** A mechanism to name a boolean expression once and reference it by name in `when` clauses, `invariant` expressions, and `in/to/from <State> assert` expressions.

**Impact:** Multi-condition eligibility conditions must be written out verbatim in every transition row that uses them. A change to shared eligibility logic requires touching every row. This is a maintainability and correctness risk, not just verbosity.

**Sample evidence — `loan-application.precept` lines 52–53:**

```precept
# The approval eligibility condition: 5 conditions, 93 characters.
# If this same eligibility check were needed in a Reconsider or AppealApprove event,
# it would have to be re-typed here in full — with no compile-time guarantee of consistency.

from UnderReview on Approve
    when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2 && RequestedAmount < AnnualIncome / 2 && Approve.Amount <= RequestedAmount
    -> set ApprovedAmount = Approve.Amount -> set DecisionNote = Approve.Note -> transition Approved
```

**`apartment-rental-application.precept` line 47:**

```precept
# Three-condition approval condition. If PayDeposit or SignLease also needed eligibility gating,
# each would repeat this condition with no reuse mechanism.
from Submitted on Approve
    when MonthlyIncome >= RequestedRent * 3 && CreditScore >= 650 && HouseholdSize < 8
    -> set ReviewerNote = Approve.Note -> transition Approved
```

**Hypothetical DSL:**

```precept
rule LoanEligible when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2 && RequestedAmount < AnnualIncome / 2

from UnderReview on Approve when LoanEligible && Approve.Amount <= RequestedAmount -> ...
from UnderReview on Reconsider when LoanEligible -> ...
from UnderReview on AppealApprove when LoanEligible -> ...
```

**Implementation notes:**  
New top-level declaration keyword `rule`. Parser: `rule <Name> when <BoolExpr>` statement; collected into `PreceptDefinition.Rules` (new field: `IReadOnlyList<PreceptNamedRule>`). Type checker: resolve rule names in `when` clause positions, `invariant` expressions, and `in/to/from <State> assert` expressions — expand the named rule expression in-place before type-checking the enclosing expression. This avoids a new expression node: the rule reference is resolved at name-lookup time, not at evaluation time. Evaluator: same — expand in place. Scope: rule expressions see data fields only (not event args), because rules must be reusable across events. Rules that reference event args by `EventName.ArgName` would be event-specific and lose their reuse value — this constraint should be enforced by the type checker at rule declaration time.

**Valid reuse positions:**
- `when` position in a transition row — rule contributes the field-side condition; event-arg conditions combine via `&&` in the row's inline expression.
- `invariant` expression — field scope only, exact match with rule body scope.
- `in/to/from <State> assert` expression — field scope only, exact match with rule body scope.

**Invalid reuse position — `on <Event> assert`:** Event asserts run at Stage 1 of the fire pipeline before instance data is loaded. Their scope is event args only. Rule bodies reference data fields — a disjoint scope. The type checker must reject rule names in `on <Event> assert` positions with a diagnostic explaining why. This is a pipeline architecture constraint, not a parser limitation.

**Verdict: `feasible-with-caveats`**  
Rule reuse is valid in `when` clause, `invariant`, and `in/to/from <State> assert` — all share field-and-collection scope, which is a superset of rule body scope. `on <Event> assert` is explicitly excluded (pipeline architecture: Stage 1 runs before data is loaded; event-arg-only scope is disjoint from rule scope). The type checker must validate rule expressions in a data-only scope at declaration time, and must emit a clear diagnostic when a rule name appears in an event assert. Circular rule references remain impossible given the rule-to-rule-reference ban.

**Philosophy fit:** strong, provided rules stay as top-level named boolean expressions only. `rule <Name> when <Expr>` preserves keyword anchoring, keeps first-match routing untouched, and avoids violating the self-contained-row principle because actions and outcomes remain explicit in each row. It also keeps the DSL feeling like configuration/scripting rather than a helper-heavy programming language. The main product risk is social rather than semantic: if authors start naming trivial one-clause checks, rules become abstraction tax instead of clarity. Proposal wording and examples should steer usage toward repeated multi-clause business rules.

---

#### L4 — `on <Event> assert` scope excludes data fields

**What is missing:** The ability to reference data fields in `on <Event> assert` expressions.

**Impact:** Cross-field validation between event args and existing data state is inexpressible at the event-assertion level. It must be moved into `when` guards inside every transition row that handles that event — duplicating the check N times across rows.

**Example of what you cannot write:**

```precept
# This is illegal today — RequestedAmount is a data field, not an event arg.
on Approve assert Approve.Amount <= RequestedAmount because "Cannot approve more than was requested"
```

**What you're forced to do instead:**

```precept
# The guard must appear in every transition row that uses Approve in UnderReview.
from UnderReview on Approve when Approve.Amount <= RequestedAmount -> ...
from UnderReview on Approve -> reject "Cannot approve more than was requested"
# If Approve is ever valid from a second state, the guard must be repeated there too.
```

**Affected samples:** `loan-application`, `refund-request`, `insurance-claim`, `travel-reimbursement`, `apartment-rental-application`, `subscription-cancellation-retention` — any sample where event arg validity depends on comparing to current data state.

**Implementation notes:**  
The scope isolation is intentional per `PreceptLanguageDesign.md` and `ConstraintViolationDesign.md`: event asserts check arg validity independently of data state, so they run in Stage 1 of the fire pipeline *before* row selection. Making them data-aware would require data to be passed into Stage 1, which currently doesn't happen. This is a pipeline design constraint, not just a parser limitation. **Full integration** (making `on <Event> assert` data-aware) would be a breaking change to the fire-pipeline contract. A less invasive alternative: a new `from any on <Event> assert` form that runs in the `when`-evaluation phase and is scoped to data + event args — functionally equivalent to a named reusable rule that rejects rather than blocks.

**Verdict: `feasible-with-caveats`**  
The `on <Event> assert` scope isolation is a documented design choice with good reasons (args can be validated before the instance is loaded). Changing it risks breaking the pipeline stage contract. The `from any on <Event> assert` alternative form is safer and more additive. Needs a design doc with Frank before any implementation.

---

#### L5 — No `abs()` or numeric math functions

**What is missing:** Built-in numeric functions: `abs(x)`, `floor(x)`, `ceil(x)`, `round(x)`, and potentially `min(a, b)` / `max(a, b)` over scalars.

**Impact:** Range constraints that are symmetric around a value are inexpressible correctly. One-sided comparisons are used as approximations.

**Sample evidence — `maintenance-work-order.precept` line 66:**

```precept
# This guard only catches overage in one direction.
# It will not catch if ActualHours is somehow *less* than estimated by more than 4 (underrun).
# The intent is "within 4 hours of estimate" — a symmetric range — but only one side is checkable.
from InProgress on Complete when ActualHours <= EstimatedHours + 4 -> ...
```

The semantically correct guard would be:

```precept
from InProgress on Complete when abs(ActualHours - EstimatedHours) <= 4 -> ...
```

**Further examples:**

- Fee rounding: `set Fee = round(Miles * Rate)` — currently impossible; decimals accumulate.
- Capped discount application: `set FinalPrice = max(BasePrice - DiscountAmount, 0)` — currently forces an invariant or a guard row.
- `travel-reimbursement.precept`: Mileage is computed as `Miles * Rate` with no rounding — fine for demo purposes but not production-accurate.

**Hypothetical DSL:**

```precept
invariant abs(ActualHours - EstimatedHours) <= 4 because "Hours must be within 4 of estimate"
set Fee = round(Miles * MileageRate)
set FinalPrice = max(BasePrice - DiscountAmount, 0)
```

**Implementation notes:**  
Requires a new `PreceptFunctionCallExpression(Name, Args)` AST node. Parser: recognise `Name(expr, ...)` atom form. Type checker: a static function registry with name, arity, argument types, and return type. Evaluator: dispatch on function name, call the C# implementation. Functions to add initially: `abs(number) → number`, `floor(number) → number`, `ceil(number) → number`, `round(number) → number`, `min(number, number) → number`, `max(number, number) → number`. The grammar change is moderately invasive because `Name(` currently cannot appear in expression position — the parser must backtrack or use lookahead.

**Verdict: `feasible-with-caveats`**  
The parser change for `Name(...)` syntax is the biggest risk: currently all atoms are literals, identifiers, or parenthesized expressions. A function call looks like `Identifier(`, which the parser does not currently reach in the atom combinator. The `.Try()` composition in Superpower makes this additive, but a new atom alternative for function calls must be inserted before `DottedIdentifier` in the `Atom` combinator, with lookahead on `(` to avoid misreading `Name` followed by a paren in a different context (e.g., `Name != (expr)` — but that's a comparison, not a call, so the grammar is unambiguous). Start with `abs`, `min`, `max` and ship others as follow-ons.

---

### 2.2 SIGNIFICANT — Painful Workarounds Required

---

#### L6 — `contains` works only on collection fields, not strings

**What is missing:** Substring membership test on `string` fields/args.

**Impact:** Format validation for email, URL, path, code patterns is entirely inexpressible. The only available string checks are equality (`==`, `!=`) and length (once L2 is fixed). There is no way to verify that a field contains a specific character or substring.

**What you cannot write today:**

```precept
# Impossible — 'contains' requires a collection field on the left.
on Submit assert Submit.Email contains "@" because "Email must include an @ sign"
invariant SerialNumber contains "-" because "Serial numbers must be hyphenated"
```

**Implementation notes:**  
Two options: (a) overload the existing `contains` operator to also accept `string contains string` — lowest grammar cost, but `contains` currently requires the left operand to be a collection field at the type-checker level; that check would need relaxation. (b) Add a separate `string.contains(substring)` method form once function calls (L5) are supported. Option (b) is cleaner but depends on L5. Option (a) can be done independently. The evaluator already handles `contains` as a special case; adding a string branch to `EvaluateContains` is minimal. The type checker would need to allow `string contains string` as a valid binary expression returning `boolean`.

**Verdict: `feasible`**  
Low evaluator cost if implemented as an overload of the existing `contains` operator. Type checker needs one additional branch. No new AST node.

---

#### L7 — No null-coalescing operator (`??`)

**What is missing:** A `??` operator that returns the left operand if non-null, otherwise the right operand. Familiar from C#, TypeScript, SQL `COALESCE`.

**Impact:** Default-value substitution for nullable fields in `set` expressions requires either a `when` guard row or a separate post-mutation state action. Computed string fields that combine a nullable component with a fallback cannot be expressed.

**What you cannot write today:**

```precept
# Set a display name to the provided name or "Anonymous" if null.
set DisplayName = Submit.Name ?? "Anonymous"

# Keep existing note if new one is null.
set DecisionNote = Approve.Note ?? DecisionNote
```

**What you're forced to do instead:**

```precept
# Two separate rows — identical except for the Note assignment:
from UnderReview on Approve when Approve.Note != null -> set DecisionNote = Approve.Note -> ...
from UnderReview on Approve -> ...   # DecisionNote unchanged
```

**Sample evidence:** The `note == null || note != ""` pattern appears in 8 of 21 samples for blank-note validation. These would benefit from a null-coalescing default in the `set` action, reducing the need for separate "was a note supplied?" guard rows.

**Implementation notes:**  
New binary operator `??`. Parser: add as a new precedence level above `||` (lower precedence than everything else, so it binds loosely). Type checker: left operand must be nullable; result type is the non-nullable version of the left type (or the right type, which must be assignable to the non-null left type). Evaluator: return right operand if left is null, otherwise left. The type narrowing model already handles `nullable → non-nullable` patterns; `??` is the mutation-side complement. This operator is type system-impactful and interacts with the narrowing model — validate carefully that the type checker produces correct `StaticValueKind` flags for the result.

**Verdict: `feasible-with-caveats`**  
The interaction with the nullability type flags (`StaticValueKind.Null`) in the type checker is the risk. Confirm that the result type strips the `Null` bit correctly in all cases.

---

#### L8 — No set-membership test for inline constant sets

**What is missing:** An expression of the form `Field in ["Value1", "Value2", "Value3"]` to test whether a scalar field equals any member of a constant set.

**Impact:** Multi-value membership tests degrade to verbose `||` chains that cannot be statically validated as exhaustive or mutually exclusive.

**What you're forced to do today:**

```precept
# Checking whether a priority field is one of the valid values:
invariant Priority == "Low" || Priority == "Medium" || Priority == "High" || Priority == "Critical"
    because "Priority must be a known tier"
```

**What you'd want:**

```precept
invariant Priority in ["Low", "Medium", "High", "Critical"]
    because "Priority must be a known tier"
```

**Affected domains:** Status enumerations, tier classifications, priority levels, category codes — any bounded string or number value set. This pattern appears informally in helpdesk ticket, insurance claim, and travel reimbursement samples.

**Implementation notes:**  
New binary operator `in` with signature `Scalar in [literal, literal, ...]`. The right operand is a literal set (not a collection field). Parser: new alternative in the comparison level: `Term in ListLiteral`. Type checker: right operand must be an array of literals assignable to the left operand's type. Evaluator: iterate the literal list checking equality. Risk: `in` already appears as a preposition keyword (`in Open assert`). The tokenizer would need to distinguish `in` as a comparison operator (appears within an expression) from `in` as a preposition (appears at statement start). This is a lexical ambiguity that requires context-aware tokenization or a separate keyword token — moderate implementation risk.

**Verdict: `feasible-with-caveats`**  
The lexical conflict between `in` as a preposition and `in` as a membership operator is the primary risk. One mitigation: use a different token (`one of ["...", "..."]`, `any of [...]`, or `matches [...]) to avoid the keyword collision entirely.

---

### 2.3 MODERATE — Quality-of-Life Gaps

---

#### L9 — No `any`/`all` collection-condition expressions

**What is missing:** Quantified boolean expressions over collection element values: `Collection.any(element == "value")` or `Collection.all(element != null)`.

**Impact:** Cannot express constraints on *what* is in a collection, only on *how many* items are in it. The only collection condition helpers available are `.count`, `.min`, `.max`, and `contains` for specific values.

**Example of what you cannot write:**

```precept
# All repair steps must be non-empty strings.
invariant RepairSteps.all(step => step != "") because "Repair steps cannot be blank"

# At least one interviewer has submitted feedback.
in Decision assert PendingInterviewers.count == 0 because "All interviews must be completed"
# ↑ This is expressible. But:
# "At least one interviewer name starts with 'ext-'" — not expressible.
```

**Implementation notes:**  
New `PreceptQuantifierExpression(CollectionName, Quantifier, Condition)` AST node. Requires a lambda-like sub-expression with a bound element variable — this is a significant grammar extension. The evaluator would iterate collection elements, binding each to the element variable and evaluating the condition. The type checker would need to introduce a lambda scope with the element variable typed to the collection's inner type. This is the most complex item on this list — comparable to introducing closures. Not recommended as a first expression expansion.

**Verdict: `not recommended` (for now)**  
High grammar and implementation complexity. The lambda scoping requirement is a significant departure from the current flat identifier model. The cases that genuinely require it (property-based collection validation) are rare in current samples. Revisit when the simpler gaps (L1–L5) are resolved.

---

#### L10 — String comparison operators (`>`, `<`, `>=`, `<=`) unsupported

**What is missing:** Lexicographic string ordering.

**Impact:** Cannot enforce alphabetical constraints (`MinState <= CurrentState` for ordered status progression modelled as strings), version string ordering, or any string sort constraint. Limited practical impact in current samples.

**What you cannot write today:**

```precept
invariant LastName >= "A" because "Names must be alphabetic"
```

**Implementation notes:**  
In the evaluator, the `>`, `>=`, `<`, `<=` operators currently delegate to `TryToNumber`. Adding a string branch with `string.Compare(ordinal)` is a one-line evaluator change. The type checker currently requires both operands to be `Number` for these operators. Relaxing to `(Number, Number) or (String, String)` with matching-type enforcement is a minor type checker change.

**Verdict: `feasible`**  
Low cost. Limited but real use cases. Alphabetical sorting constraints in audit logs, name validation, code series validation.

---

#### L11 — Division by zero is unguarded at runtime

**What is missing:** Either a compile-time warning for potential division by zero, or a `safe-division` expression form.

**Impact:** Expressions like `AnnualIncome / TripDays` in `travel-reimbursement.precept` produce `double.NaN` or `double.PositiveInfinity` at runtime if the divisor is 0. There is no way for the author to express "only valid when denominator is non-zero" as an expression — only as a separate `when` guard.

**Sample evidence — `travel-reimbursement.precept` line 47:**

```precept
# If Submit.Days were 0, this division would silently produce infinity.
# The on-Submit assert checks Days > 0, but there is no expression-level safeguard.
from Draft on Submit when Submit.Lodging / Submit.Days <= 350 -> ...
```

The author relies on the event assert (`on Submit assert Days > 0`) to prevent this, but that relationship is implicit. An AI agent authoring a new precept might not know to add the precautionary assert.

**Implementation notes:**  
Two approaches: (a) Compile-time warning (new diagnostic code) when a division expression cannot be statically proven non-zero — this requires the type checker to track constant vs. variable denominators, which is complex. (b) Runtime: the evaluator currently returns `double.NaN` silently; changing it to return `EvaluationResult.Fail` when dividing by zero would surface the problem as a `ConstraintFailure` with a clear message rather than a silent NaN propagating through subsequent comparisons. Option (b) is a low-cost evaluator improvement that makes the bug visible without a language change.

**Verdict: `feasible`** (for option b — runtime guard)  
Change the evaluator to return `EvaluationResult.Fail($"division by zero in '{expression}'")` when the right operand is exactly 0. No grammar change. Improves debuggability for existing and future precepts.

---

#### L12 — Nullable string `+` concatenation blocked by type checker

**What is missing:** String concatenation when one operand is nullable string.

**Current behavior:** The type checker requires both operands of `+` to be exactly `StaticValueKind.String` (no `Null` flag). A `string nullable` field has kind `String | Null`, which fails the `IsExactly(leftKind, StaticValueKind.String)` check.

**Impact:** String interpolation patterns with nullable fields — `"Case #" + CaseId` when `CaseId` is nullable — are rejected at compile time even when the author has not narrowed the field.

**What you cannot write today:**

```precept
# If CaseId is nullable, this is rejected even in a context where it's likely non-null:
set DisplayRef = "REF-" + CaseId
```

**Implementation notes:**  
The fix is contextual: (a) relax `+` to permit `(String | Null) + String` and produce `String | Null` — maintains safety but allows the expression; or (b) require the author to narrow first (`when CaseId != null`), relying on the existing null-narrowing pass. Option (b) is already available in practice. The real gap is that the type checker produces an error rather than a warning when the author writes the expression in an unnarrowed context — making the diagnosis feel incorrect. Adding a specific diagnostic message that says "CaseId is nullable; use a null check or ?? to safely concatenate" would be more helpful than the generic "type mismatch" error.

**Verdict: `feasible`** (diagnostic improvement)  
Improve C39/C41 diagnostic message quality for nullable string concatenation cases. No language change; no semantic change.

---

## 3. Impact on Event Argument Ingestion (Verbosity Pattern)

The `internal-verbosity-analysis.md` identifies event argument ingestion (`set Field = EventName.Arg` × N) as the #1 verbosity pattern in the corpus. This is a **statement-count** problem, not strictly an expression-language problem — the expression system supports it fine; the DSL just has no shorthand for "copy all matched-name args to fields."

This audit does not propose an `absorb` shorthand (that's a statement-level change, not an expression change). But L7 (null-coalescing) and L1 (ternary) together reduce the verbosity of computed-value ingestion rows where values need defaults or conditional mapping. Addressed separately.

---

## 4. Promising Directions — Ranked Shortlist

| # | Feature | Severity | Verdict | Estimated scope |
|---|---|---|---|---|
| 1 | Ternary expression in `set` RHS | Critical | `feasible` | New AST node, parser level, type checker branch, evaluator branch |
| 2 | `string.length` accessor | Critical | `feasible` | Type checker symbol injection, evaluator branch. No new AST node. |
| 3 | Named rule declarations | Critical | `feasible-with-caveats` | New declaration form, name resolution, cycle detection |
| 4 | `abs()` / scalar math functions | Critical | `feasible-with-caveats` | New AST node (function call), static function registry, parser change |
| 5 | `contains` for strings (substring) | Significant | `feasible` | Type checker operator overload, evaluator branch |
| 6 | Null-coalescing `??` | Significant | `feasible-with-caveats` | New operator, type checker nullability interaction |
| 7 | `in [literal, ...]` membership | Significant | `feasible-with-caveats` | Lexical conflict with `in` preposition; mitigate with alternate token |
| 8 | Division-by-zero runtime guard | Moderate | `feasible` | One evaluator change; no grammar change |
| 9 | String ordering operators | Moderate | `feasible` | One evaluator branch, one type checker relaxation |
| 10 | `any`/`all` collection conditions | Moderate | `not recommended` | Lambda scoping is a major grammar extension |
| 11 | Nullable `+` diagnostic improvement | Moderate | `feasible` | Diagnostic message improvement only |

---

## 5. Technical Cross-Cutting Notes for Frank's Proposal

**On parser risk:**  
Items 1 (ternary), 4 (functions), and 7 (set-membership) introduce new grammar levels or atom forms. The Superpower-based parser uses `.Try()` composition — new alternatives must be inserted at the correct precedence level. Test coverage for expression parsing is the best regression safety net here; verify the test project covers all existing expression forms before touching the parser.

**On type checker risk:**  
Items 6 (null-coalescing), 7 (set-membership), and 4 (functions) interact with the `StaticValueKind` flags. The type checker is correct-by-construction for existing types — any new kind-inference branch needs to respect the `Null` flag propagation rules (see `TryInferBinaryKind` in `PreceptTypeChecker.cs`). Nullable propagation bugs are silent — they manifest as false-positive compile diagnostics, not runtime errors.

**On evaluator risk:**  
Item 8 (division guard) and item 6 (null-coalescing) change evaluator behavior for existing expressions. Both must be backward-compatible: `divide(a, b)` where b is always provably non-zero must not regress; `??` must short-circuit correctly for both `null` and non-null left operands.

**On scope risk:**  
Items 1 (ternary) and 4 (functions) should initially be scoped to `set` RHS and `when` guards only — not to `invariant` or `assert` positions. This limits blast radius and gives tooling (language server) time to catch up. The spec should clearly document which expression positions accept which new constructs.

**On the keyword conflict for `in`:**  
The `PreceptToken.In` token is already a keyword (`in Open assert...`). Any `in`-based set-membership syntax would conflict at the lexer level unless the `in` comparison operator uses a distinct token or the tokenizer becomes context-sensitive. Recommend `one of [...]` or `any of [...]` syntax to avoid this entirely.

**On AI legibility:**  
All proposed features follow the existing `Identifier.member` and operator patterns that AI agents can read from the `precept_language` tool output. The named rule feature (`rule Name when expr`) is particularly AI-friendly: it provides a single canonical definition point that an AI can verify, update, and reference by name without tracking usage sites. Prioritise features that have enumerable, declarative representations over features that require procedural reasoning.

---

## 6. References and Prior Work

- `docs/research/language/expressiveness/README.md` — Steinbrenner's PM-level gap analysis with cross-library precedent
- `docs/research/language/expressiveness/internal-verbosity-analysis.md` — Uncle Leo's statement-count audit
- `src/Precept/Dsl/PreceptExpressionEvaluator.cs` — runtime evaluator (ground truth for what executes)
- `src/Precept/Dsl/PreceptTypeChecker.cs` — type checker (ground truth for what compiles)
- `src/Precept/Dsl/PreceptParser.cs` — expression parser (ground truth for what parses)
- `samples/loan-application.precept`, `samples/hiring-pipeline.precept`, `samples/maintenance-work-order.precept` — canonical examples of affected patterns


