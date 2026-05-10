# Precept Toolchain Bugs

Bugs discovered during exploratory testing using the MCP tools (`precept_compile`,
`precept_syntax`, `precept_types`, `precept_operations`, `precept_proofs`,
`precept_patterns`, `precept_diagnostic`, `precept_quickstart`, `precept_domains`)
and by cross-checking results against `docs/language/precept-language-spec.md`.

**Total confirmed bugs: 57 (BUG-001 through BUG-057)**
- Compiler bugs: ~27
- MCP-definition bugs: ~15
- MCP-docs bugs: ~4

**Bug classes:**
- **Compiler** — parser, type checker, or proof engine rejects valid spec-documented syntax/behaviour
- **MCP-definition** — `precept_compile` definition output omits or mis-serializes information
- **MCP-docs** — language reference tool (`precept_proofs`, `precept_diagnostic`, etc.) gives incorrect or incomplete information

Each entry includes: what the spec says, what the tool reports, and a minimal repro.

---

## Status Tracker

> **Track 2 — Bug Fixes.** Updated as fixes land. Statuses: `Open` · `In Progress` · `Fixed` · `Skipped`

| Bug | Title | Category | Status | Assignee | Notes |
|-----|-------|----------|--------|----------|-------|
| BUG-001 | `any` state wildcard not recognized in any position | Compiler | **Fixed** | — | Retested 2026-05-10: `from any on E` compiles cleanly; definition shows `fromStates:["*"]` ✅ |
| BUG-002 | `contains` operator rejected in all expression positions | Compiler | Open | — | — |
| BUG-003 | `and` / `or` / `not` compound boolean expressions rejected in... | Compiler | Open | — | — |
| BUG-004 | `default` modifier rejected on event argument declarations | Compiler | **Fixed** | `e68008d0` | Retested 2026-05-10: `event E(arg as number default 1)` now parses and compiles cleanly ✅ |
| BUG-005 | Comma-separated field list rejected in `in S modify` declarations | Compiler | **Fixed** | `e68008d0` | Retested 2026-05-10: `in S modify A, B editable` compiles cleanly ✅ |
| BUG-006 | `min(a, b)` and `max(a, b)` not recognized as function calls... | Compiler | **Fixed** | — | Retested 2026-05-10: `min(A, B)`, `max(A, B)`, and `min(max(X, 0), 100)` all compile cleanly ✅ |
| BUG-007 | Arithmetic operators have lower precedence than comparison... | Compiler | Open | — | — |
| BUG-008 | `pop` and `dequeue` proof obligations use unnamed internal... | Compiler | Open | — | — |
| BUG-009 | `for` operator resolves to key type instead of value type | Compiler | Open | — | — |
| BUG-010 | `choice` literal not typed as choice in comparison position | Compiler | Open | — | — |
| BUG-011 | State entry/exit hook actions not serialized in MCP... | MCP-definition | Open | — | — |
| BUG-012 | Stateless event handler actions not serialized in MCP... | MCP-definition | Open | — | — |
| BUG-013 | `sqrt(abs(x))` proof obligation uses `<unknown>` — abs()... | Compiler | **Fixed** | — | Retested 2026-05-10: `sqrt(abs(X))` compiles cleanly ✅ |
| BUG-014 | `precept_proofs` CollectionEmptyOnMutation recovery hint is... | MCP-docs | Open | — | — |
| BUG-015 | `precept_diagnostic` PRE0083 description covers only... | MCP-docs | Open | — | — |
| BUG-016 | Guarded rule `when` clause not serialized in MCP definition... | MCP-definition | Open | — | — |
| BUG-017 | `~string` (case-insensitive string) qualifier lost in MCP... | MCP-definition | Open | — | — |
| BUG-018 | Collection element types lost in MCP definition output | MCP-definition | Open | — | — |
| BUG-019 | Typed constants (single-quoted strings) not resolved from... | Compiler | **Fixed** | `e68008d0` | Retested 2026-05-10: typed constants in default/action expression positions parse and compile cleanly ✅ |
| BUG-020 | Guarded ensures (`when` guard) not parsed in any position | Compiler | **Fixed** | `e68008d0` | Retested 2026-05-10: `in State when G ensure ...` compiles cleanly ✅ |
| BUG-021 | `append by P`, `enqueue by P` v3 action forms not parsed | Compiler | **Fixed** | — | Retested 2026-05-10: `append Tasks Label by Priority` and `enqueue Jobs Job by Rank` both compile cleanly ✅ |
| BUG-022 | Event ensures (`on Event ensure`) not serialized in MCP... | MCP-definition | Open | — | — |
| BUG-023 | `because` clause includes keyword in serialized value | MCP-definition | Open | — | — |
| BUG-024 | `omit` declarations not reflected in MCP definition output | MCP-definition | Open | — | — |
| BUG-025 | Keyword-named member accessors rejected by parser | Compiler | **Fixed** | — | Retested 2026-05-10: `.count`, `.peek` on list/queue/stack all compile ✅ |
| BUG-026 | `in State modify all readonly` treats `all` as field name | Compiler | **Fixed** | — | Retested 2026-05-10: `in Draft modify all readonly` compiles cleanly ✅ |
| BUG-027 | `choice of T(...)` type not valid in event arg declarations | Compiler | **Fixed** | `e68008d0` | Retested 2026-05-10: event args accept `choice of T(...)` via full type parser ✅ |
| BUG-028 | `RedundantModifier` fires wrong diagnostic code with garbled... | Compiler | Open | — | — |
| BUG-029 | `InvalidModifierBounds` check not enforced | Compiler | **Fixed** | — | Retested 2026-05-10: PRE0034 now fires correctly for `min > max` ✅ |
| BUG-030 | Computed field forward references rejected; wrong error... | Compiler | **Fixed** | `e68008d0` | Retested 2026-05-10: computed forward references compile; cycles emit `CircularComputedField` ✅ |
| BUG-031 | String interpolation not supported in `reject`, `because`,... | Compiler | **Fixed** | `e68008d0` | Retested 2026-05-10: interpolated `because`/`reject` messages parse cleanly ✅ |
| BUG-032 | `reject` outcomes not serialized in MCP definition rows | MCP-definition | Open | — | — |
| BUG-033 | Event arg `optional` modifier not reflected in MCP definition | MCP-definition | Open | — | — |
| BUG-034 | Per-state access mode overrides not in MCP definition output | MCP-definition | Open | — | — |
| BUG-035 | Choice element type and member values lost in MCP definition... | MCP-definition | Open | — | — |
| BUG-036 | `no transition` and `reject` outcomes indistinguishable in... | MCP-definition | Open | — | — |
| BUG-037 | `in State modify all` and `in State omit all` both reject... | Compiler | **Fixed** | — | Retested 2026-05-10: `modify all editable` and `omit all` both compile cleanly ✅ |
| BUG-038 | `InvalidModifierBounds` not enforced for... | Compiler | **Fixed** | — | Retested 2026-05-10: PRE0034 fires for `minlength > maxlength` and `mincount > maxcount` ✅ |
| BUG-039 | `list.at(N)` method call rejected due to `at` keyword collision | Compiler | **Fixed** | — | Retested 2026-05-10 (round 5): spec updated — `count > 0` proof now documented for `at(N)`; without `notempty` correctly emits PRE0063 ✅; with `notempty` compiles clean ✅ |
| BUG-040 | `queue.peekby(P)` not implemented | Compiler | Open | — | — |
| BUG-041 | `UnexpectedNull` runtime fault recovery hint uses invalid... | MCP-docs | Open | — | — |
| BUG-042 | Modifier bound values not serialized in MCP definition output | MCP-definition | Open | — | — |
| BUG-043 | String default values include surrounding DSL quotes in... | MCP-definition | Open | — | — |
| BUG-044 | Guarded state actions (`from State when G -> action`) not... | Compiler | **Fixed** | `e68008d0` | Retested 2026-05-10: guarded state hooks (`to/from State when ... ->`) compile cleanly ✅ |
| BUG-045 | `ascending`/`descending` modifiers not recognized in log type... | Compiler | **Fixed** | `e68008d0` | Retested 2026-05-10: `log of T by P ascending|descending` parses cleanly ✅ |
| BUG-046 | CI enforcement not applied to quantifier binding variables | Compiler | Open | — | — |
| BUG-047 | Stateless event hook actions not serialized in MCP definition... | MCP-definition | Open | — | — |
| BUG-048 | `by` keyword not recognized in `append`/`enqueue` priority... | Compiler | **Fixed** | — | Retested 2026-05-10: same fix as BUG-021; `enqueue … by` compiles cleanly ✅ |
| BUG-049 | `insert`/`remove at` actions fail due to `at` keyword ambiguity | Compiler | **Fixed** | — | Retested 2026-05-10: `remove Steps at Position` fixed in `a65c9fed`; `insert Steps NewStep at Position` fixed in `f2d1dece` ✅ |
| BUG-050 | `dequeue`/`pop` trigger false PRE0083 "Division by zero" | Compiler | Open | — | — |
| BUG-051 | `min(a, b)` and `max(a, b)` function calls fail due to... | Compiler | **Fixed** | — | Retested 2026-05-10: same fix as BUG-006; `min()`, `max()`, and chained `min(max(...))` all compile cleanly ✅ |
| BUG-052 | `contains` keyword unusable in expression position | Compiler | Open | — | — |
| BUG-053 | `and`/`or` binary boolean operators fail in all expression positions | Compiler | Open | — | — |
| BUG-054 | `ensure` clause not supported in stateless event hooks | Compiler | **Fixed** | — | Retested 2026-05-10: `on Event ensure BoolExpr because "..."` compiles cleanly ✅ |
| BUG-055 | PRE0097 `exampleAfter` shows wrong fix — removes `~string` instead of switching to `~startsWith` | MCP-docs | Open | — | — |
| BUG-056 | PRE0081 false positive fires on stateless-hook-only events in stateful precepts | Compiler | Open | — | — |
| BUG-057 | `date + period` and `date - period` arithmetic unusable — PRE0113 fires on all period field forms | Compiler | Open | — | — |

---

## BUG-001 — `any` state wildcard not recognized in any position **[Compiler]**

### Status
Open

### Description
The `any` keyword is documented as a state wildcard usable in ALL state-target positions:
transition rows (`from any on E`), state ensures (`in any ensure`, `from any ensure`),
and access modes (`in any modify F`). The name binder rejects it with PRE0028 "State 'any'
is not declared" in every position, treating `any` as a literal state name lookup.

The error fires **twice** for each occurrence (suggesting two resolution passes).

### Spec reference
- `precept-language-spec.md` §Keywords table (line 279):
  `Any | any | Quantifier keyword (any item in Coll (pred)) / state wildcard (in any, from any)`
- `precept-language-spec.md` line 469: "`any` also appears as state wildcard (`in any`, `from any`)"
- `precept-language-spec.md` line 1589 (example): `from any on Event -> no transition`

### Errors reported
```
PRE0028  State 'any' is not declared  (fires twice per occurrence)
```

### Positions tested — all fail:
| Form | Result |
|------|--------|
| `from any on E -> no transition` | PRE0028 ❌ |
| `in any ensure E because "..."` | PRE0028 ❌ |
| `from any ensure E because "..."` | PRE0028 ❌ |
| `to any -> set F = V` | PRE0028 ❌ |

### Minimal repro
```
precept BugRepro001

field Flag as boolean default false

state A initial
state B
state C

event Toggle
from any on Toggle
    -> set Flag = true
    -> no transition

event Finish
from A on Finish -> transition C
from B on Finish -> transition C
```

### Affected samples
- `samples/trafficlight.precept` — `from any on VehiclesArrive`, `from any on LeftTurnRequest`, `from any on Emergency`
- `samples/restaurant-waitlist.precept` — `from any on CloseService`
- `samples/crosswalk-signal.precept` — `from any on PedestrianRequest`
- `samples/it-helpdesk-ticket.precept` — comment references `from any` as the intent

---

## BUG-002 — `contains` operator rejected in all expression positions

### Status
Open

### Description
The `contains` operator is a documented infix operator for collection membership tests.
The compilation pipeline rejects it everywhere — in `when` guards, `ensure` constraints,
and `rule` expressions — with a misleading error message that suggests the type checker
may have the operand order inverted, or that `contains` is not wired into the expression
parser at all.

### Spec reference
- `precept-language-spec.md` line 697 (precedence table):
  `contains | collection membership | non-associative` — precedence 40, result type `boolean`
- `precept-language-spec.md` §`contains` (line 1283–1287):
  `set of T contains T (or widens to T) → boolean`
- `precept-language-spec.md` line 1467:
  "`contains` … enforcement fires in all expression positions … including `when` guards,
  `rule` expressions, `ensure` expressions, and quantifier predicates."

### Errors reported
```
PRE0018  Expected a set value here, but got 'string'
```
Note: the error message is itself suspicious. `Items` (type `set of string`) is the left
operand and `"foo"` (type `string`) is the right operand. The spec says the result is
`boolean`. The error saying the result is `'string'` suggests the type checker may have
the operand order reversed, resolving the type of the whole expression from the RHS
literal instead of the operator's documented return type.

### Minimal repro
```
precept BugRepro002

field Items as set of string

state Draft initial
state Done terminal

event Finish
from Draft on Finish -> transition Done

# Fails in when guard:
event Remove(Item as string notempty)
from Draft on Remove when Items contains "placeholder"
    -> remove Items "placeholder"
    -> no transition
```

Expected: compiles cleanly — `set of string contains string` → `boolean` is valid in a guard.
Actual:
```
PRE0018  Error  Expected a set value here, but got 'string'
```

Also fails in `ensure`:
```
in Draft ensure Items contains "placeholder" because "Must contain placeholder"
```
And with a field reference on the RHS:
```
field Target as string optional
from Draft on Remove when Items contains Target
```

### Expected behavior
`Collection contains Element` evaluates to `boolean` and is valid in `when` guards,
`ensure` constraints, and `rule` expressions per the spec.

### Affected samples
- `samples/vehicle-service-appointment.precept` — `when RecommendedServices contains RemoveServiceRecommendation.ServiceName`
- `samples/utility-outage-report.precept` — `when AffectedBlocks contains RemoveBlock.BlockName`
- `samples/insurance-claim.precept` — `when MissingDocuments contains ReceiveDocument.Name`
- `samples/hiring-pipeline.precept` — `when PendingInterviewers contains RecordInterviewFeedback.Interviewer` (×2)
- `samples/building-access-badge-request.precept` — `when RequestedFloors contains RemoveFloor.Floor`

---

## BUG-003 — `and` / `or` / `not` compound boolean expressions rejected in all positions

### Status
Open

### Description
The logical operators `and`, `or`, and `not` are documented as first-class expression
operators with defined precedence and Pratt parser bindings. The compilation pipeline
rejects any compound boolean expression — in `when` guards, `rule` expressions, and
`ensure` expressions — with a self-contradictory error message that says the expression
is `boolean` but is still rejected where a boolean is required.

### Spec reference
- `precept-language-spec.md` §2.1 Expression Precedence (lines 693–695):
  `or` — precedence 10, left; `and` — precedence 20, left; `not` — precedence 25, right prefix
- `precept-language-spec.md` §Left-denotation table (lines 734–735):
  `Or → BinaryExpression(Or, ParseExpression(10))`
  `And → BinaryExpression(And, ParseExpression(20))`
- `precept-language-spec.md` line 723 (null-denotation):
  `Not → UnaryExpression(Not, ParseExpression(25))`
- `precept-language-spec.md` line 864 (guard grammar):
  `("when" BoolExpr)?` — no restriction on expression structure; any boolean expression is valid
- `precept-language-spec.md` line 1444 (type check):
  "Guard not boolean | `when Expr` where `Expr`'s type is not `boolean`" — check is on
  result type only, not expression structure

### Errors reported
```
PRE0018  Expected a boolean value here, but got 'boolean'
```
The error message is self-contradictory: the compound expression evaluates to `boolean`,
which is exactly what is required. The diagnostic fires regardless of expression position
(`when`, `rule`, `ensure`).

### Minimal repros

**In a `when` guard:**
```
precept BugRepro003a
field Verified as boolean default false
field Urgent as boolean default false
state Active initial
state Done terminal
event Start
from Active on Start when Verified and not Urgent -> transition Done
from Active on Start -> reject "Not ready"
```
Expected: compiles cleanly — `Verified and not Urgent` is `boolean and not boolean` = `boolean`.
Actual: `PRE0018  Expected a boolean value here, but got 'boolean'`

**In a `rule` expression:**
```
precept BugRepro003b
field MarketingOptIn as boolean default false writable
field Email as string optional writable
rule MarketingOptIn == false or Email is set because "Opt-in requires email"
```
Expected: compiles cleanly — `bool or bool` = `boolean`.
Actual: `PRE0018  Expected a boolean value here, but got 'boolean'`

### Expected behavior
`A and B`, `A or B`, and `not A` produce `boolean` and are valid in all boolean
expression positions per the spec. Compound guards like `when A and B and C` should
compile without error.

### Affected samples
- `samples/customer-profile.precept` — `rule MarketingOptIn == false or Email is set`
- `samples/library-book-checkout.precept` — `when RenewalCount < 2 and not LostReported and CurrentDay <= DueDay`
- `samples/maintenance-work-order.precept` — `when not Urgent or PartsApproved`
- `samples/subscription-cancellation-retention.precept` — `when not SaveOfferAccepted and RetentionDiscount == 0`
- `samples/it-helpdesk-ticket.precept` — `ensure Triage.Level >= 1 and Triage.Level <= 5`
- `samples/insurance-claim.precept` — `when (not PoliceReportRequired or MissingDocuments.count == 0) and Approve.Amount <= ClaimAmount`
- `samples/loan-application.precept` — `when DocumentsVerified and CreditScore >= 680 and AnnualIncome >= ExistingDebt * 2 ...`
- `samples/apartment-rental-application.precept` — `when MonthlyIncome >= RequestedRent * 3 and CreditScore >= 650 and HouseholdSize < 8`
- `samples/library-hold-request.precept` — `when AdvanceDay.DayNumber > CurrentDay and AdvanceDay.DayNumber <= PickupExpiryDay` (×2)
- `samples/hiring-pipeline.precept` — `when PendingInterviewers contains ... and PendingInterviewers.count == 1`

---

## BUG-004 — `default` modifier rejected on event argument declarations

### Status
Open

### Description
The spec grammar defines `ArgDecl := Identifier as TypeRef FieldModifier*`, meaning
event arguments accept the same value-modifier set as field declarations. `default` is a
value modifier. The current grammar still spells that family `FieldModifier`, but the
canonical rename target for the metadata surface is `ValueModifier`. The only modifier
explicitly excluded from event args is `writable`
(which produces `WritableOnEventArg`). The compiler rejects `default` on event args
with a generic parse error, indicating the `ArgDecl` parser is not wired up to handle
`default` in that position.

### Spec reference
- `precept-language-spec.md` line 785:
  `ArgDecl  :=  Identifier as TypeRef FieldModifier*`
- `precept-language-spec.md` line 980 (`FieldModifier` table):
  `default Expr | value | Default value` — no exclusion for event args noted
- `precept-language-spec.md` line 1486 (modifier validation table):
  `writable` is the only modifier explicitly listed as invalid on event arguments
  (`WritableOnEventArg`); `default` is not excluded

### Errors reported
```
PRE0009  Expected declaration keyword here, but found '<default-value>'
```
The parser appears to recognise `default` as a keyword but then fails to parse the
value expression that follows it in arg position.

### Minimal repro
```
precept BugRepro004

state Draft initial
state Done terminal

event Submit(Days as number default 14)
from Draft on Submit -> transition Done
```
Expected: compiles cleanly — `default 14` is a valid value modifier on an event arg.
Actual:
```
PRE0009  Error  Expected declaration keyword here, but found '14'
```

### Affected samples
- `samples/library-book-checkout.precept` — `LoanDays as number default 14`, `ExtraDays as number default 7`
- `samples/trafficlight.precept` — `Count as number default 5`
- `samples/travel-reimbursement.precept` — `Rate as number default 0.67`
- `samples/subscription-cancellation-retention.precept` — `DiscountPercent as number default 10`
- `samples/it-helpdesk-ticket.precept` — `Level as number default 3`
- `samples/event-registration.precept` — `Seats as number default 1`, `PricePerSeat as number default 25`
- `samples/apartment-rental-application.precept` — `Household as number default 1`
- `samples/insurance-claim.precept` — `RequiresPoliceReport as boolean default false`

---

## BUG-005 — Comma-separated field list rejected in `in S modify` declarations

### Status
Open

### Description
The spec grammar explicitly defines `FieldTarget := identifier ("," identifier)* | all`
and shows `in StateTarget modify Field { "," Field }* editable` as a "comma-separated
shorthand." The compiler rejects the comma after the first field name, treating the
next field name as the access adjective position.

### Spec reference
- `precept-language-spec.md` line 887:
  `FieldTarget  :=  identifier ("," identifier)* | all`
- `precept-language-spec.md` lines 890–892:
  ```
  in StateTarget modify Field readonly ("when" BoolExpr)?              ← singular
  in StateTarget modify Field editable ("when" BoolExpr)?              ← singular
  in StateTarget modify Field { "," Field }* readonly ("when" BoolExpr)?  ← comma-separated shorthand
  ```

### Errors reported
```
PRE0009  Expected readonly or editable here, but found ','
```
The parser consumes the first field name, then expects the access adjective (`readonly`
or `editable`) immediately — it does not continue to parse the `("," Field)*` suffix
defined in the grammar.

### Minimal repro
```
precept BugRepro005
field A as number default 0 nonnegative writable
field B as number default 0 nonnegative writable
state Active initial
state Done terminal
in Active modify A, B readonly
event Finish
from Active on Finish -> transition Done
```
Expected: compiles cleanly — `modify A, B readonly` is the documented comma-separated shorthand.
Actual:
```
PRE0009  Error  Expected readonly or editable here, but found ','
```

### Affected samples
- `samples/trafficlight.precept` — `in Red modify VehiclesWaiting, LeftTurnQueued editable`
- `samples/library-book-checkout.precept` — `in Overdue modify DueDay, FineAmount editable`
- `samples/event-registration.precept` — `in PendingPayment modify ContactEmail, SeatsReserved editable`
- `samples/clinic-appointment-scheduling.precept` — `in Scheduled modify ScheduledDay, ScheduledMinute editable`
- `samples/maintenance-work-order.precept` — `in Draft modify Location, IssueSummary, Urgent editable`
- `samples/building-access-badge-request.precept` — `in Draft modify EmployeeName, Department, AccessReason, RequestedFloors editable`

---

## BUG-006 — `min(a, b)` and `max(a, b)` not recognized as function calls in expression position

### Status
Open

### Description
`min` and `max` are documented as dual-use tokens: constraint modifiers when followed
by a number literal, and built-in function calls when followed by `(`. The spec
explicitly states the `(` disambiguates the two roles. The compiler does not perform
this disambiguation — it rejects `min` in expression position with a parse error,
treating it only as a constraint keyword.

### Spec reference
- `precept-language-spec.md` lines 587–594:
  ```
  min / max — Constraint Keyword and Built-in Function

  Following context         | Interpretation | Example
  Followed by number literal | Constraint     | field Score as number min 0 max 100
  Followed by (              | Function call  | set Amount = min(Requested, Available)

  The ( disambiguates: constraint keywords are never followed by (, function calls always are.
  ```
- `precept-language-spec.md` line 302–303 (keyword table):
  `` `Min` | `min` | Numeric minimum constraint / built-in function (dual-use) ``
- `precept-language-spec.md` §3.7 Built-in Function Catalog (line 1386):
  `` `min(a, b)` | (integer|decimal|number, integer|decimal|number) → common numeric type ``

### Errors reported
```
PRE0009  Expected expression here, but found 'min'
```
The parser hits `min` in expression position and does not look ahead for `(` to
select the function call production — it fails immediately.

### Minimal repro
```
precept BugRepro006
field Requested as number default 0 nonnegative writable
field Available as number default 0 nonnegative writable
field Approved as number default 0 nonnegative
state Active initial
state Done terminal
event Approve
from Active on Approve
    -> set Approved = min(Requested, Available)
    -> transition Done
```
Expected: compiles cleanly — `min(Requested, Available)` is a documented function call.
Actual:
```
PRE0009  Error  Expected expression here, but found 'min'
```

### Note
`round(value, places)` and `trim(value)` are also in the built-in function catalog.
`trim` appears to work (used in `samples/insurance-claim.precept`). It is unclear
whether only `min`/`max` are affected or whether the issue is specific to their
dual-use keyword status causing the parser to skip the function-call path entirely.

### Affected samples
- `samples/travel-reimbursement.precept` — `set ApprovedTotal = min(Approve.Amount, RequestedTotal)`
- `samples/loan-application.precept` — `set ApprovedAmount = min(Approve.Amount, RequestedAmount)`
- `samples/insurance-claim.precept` — `set ApprovedAmount = ... min(Approve.Amount, ClaimAmount / 2)`

---

## BUG-007 — Arithmetic operators have lower precedence than comparison operators (Pratt parser binding powers inverted)

### Status
Open

### Description
The spec's Pratt parser table assigns:
- Comparison operators (`<`, `>`, `<=`, `>=`, `==`, `!=`) — left-binding power **30**, right operand parsed with `ParseExpression(31)`
- Additive arithmetic (`+`, `-`) — left-binding power **50**

Since 50 > 31, `+` must be consumed *inside* the right operand of a comparison. Therefore
`A > B + C` must parse as `A > (B + C)` per the spec.

The implementation has the binding powers reversed: `+`/`-` appear to have lbp ≤ 30,
causing `A > B + C` to parse as `(A > B) + C`. The type of `(A > B)` is `boolean`; adding
a numeric operand yields an expression the type-checker resolves to `number`, causing the
enclosing `rule`/`when`/`ensure` context to report PRE0018 "Expected a boolean value here,
but got 'number'".

This affects every boolean expression position — `when` guards on transition rows,
top-level `rule` declarations, `in`/`to`/`from` state ensures, `on Event ensure`,
and the inline `ensure` at the end of event hook action chains. Any expression of
the form `A comparison B arithmetic C` fails.

### Spec reference
- `precept-language-spec.md` line 691–704 — precedence table:
  | Precedence | Token(s) |
  |:----------:|----------|
  | **30** | `==` `!=` `~=` `!~` `<` `>` `<=` `>=` |
  | **50** | `+` `-` (infix) |
  | **60** | `*` `/` `%` |
- `precept-language-spec.md` line 736 — Pratt left-denotation table:
  `==` `!=` `~=` `!~` `<` `>` `<=` `>=` → `BinaryExpression(op, ParseExpression(31))`
- `precept-language-spec.md` line 740:
  `+` `-` (infix) → `BinaryExpression(op, ParseExpression(50))`

Since 50 > 31, `+` is consumed when parsing the right operand of `>` — the spec is
unambiguous.

### Errors reported
```
PRE0018  Expected a boolean value here, but got 'number'
```
The overall expression type is `number` (not `boolean`), which can only happen if the
arithmetic operator was consumed *outside* the comparison, yielding `(A > B) + C`.

### Minimal repro
```
precept BugRepro007
field Total as number default 100 positive writable
field Tax as number default 10 positive writable
field Fee as number default 5 positive writable

rule Total > Tax + Fee because "Total must exceed combined tax and fee"
```
Expected: compiles cleanly — `Total > Tax + Fee` is `Total > (Tax + Fee)` → `number > number` → `boolean`, valid as a top-level `rule`.
Actual:
```
PRE0018  Error  Expected a boolean value here, but got 'number'
```

Also fails in `in State ensure` and `on Event ensure` positions:
```
in Open ensure Balance >= Requested + Fee because "Balance must cover request"
on Submit ensure Amount >= Base * Rate because "Amount must meet base rate"
```
Both produce the same PRE0018 with "got 'number'".

### Affected samples
- `samples/sum-on-rhs-rule.precept` — `rule Total > Tax + Fee` (likely a targeted test case for this exact pattern)
- `samples/loan-application.precept` — `when AnnualIncome >= ExistingDebt * 2` (multiplicative variant, same root cause)
- `samples/apartment-rental-application.precept` — `when MonthlyIncome >= RequestedRent * 3`
- `samples/library-hold-request.precept` — `when AdvanceDay.DayNumber > CurrentDay` (simpler form; may or may not be affected)
- Any sample using arithmetic in the RHS of a comparison inside `when`/`rule`/`ensure`

---

## BUG-008 — `pop` and `dequeue` proof obligations use unnamed internal variable — unsatisfiable

### Status
Open

### Description
`pop` and `dequeue` require an "emptiness proof" (`UnguardedCollectionMutation`) per
the spec (lines 1522, 1524). The proof engine generates this obligation with an unnamed
synthetic variable (`<unknown>`) instead of the named collection field. As a result, no
declared evidence — neither a `when Field.count > 0` transition row guard nor a `notempty`
field modifier — can match and discharge the obligation. PRE0083 fires unconditionally for
any `pop` or `dequeue` action regardless of what proofs the author supplies.

`peek` (read-only accessor) works correctly: when a `when Field.count > 0` guard is present,
the proof obligation is generated against the named field and the guard discharges it cleanly.
The bug is specific to the mutation operations `pop` and `dequeue`.

### Spec reference
- `precept-language-spec.md` line 1524:
  `pop F (into G)?` — "Requires emptiness proof (`UnguardedCollectionMutation`)"
- `precept-language-spec.md` line 1522:
  `dequeue F (into G)?` — "Requires emptiness proof (`UnguardedCollectionMutation`)"
- `precept-language-spec.md` line 1498:
  "`notempty` on collections: equivalent to `mincount 1`. It statically discharges `.min`/`.max`/`.peek`/`.first`/`.last` access obligations"
  (Note: `pop`/`dequeue` are not in this list, but `when count > 0` guard should satisfy by equivalent reasoning)
- `precept-language-spec.md` line 144: "The compiler requires a provable non-negative path — via `nonnegative` constraint, a rule, an ensure, or a guard — before accepting the expression."

### Errors reported
```
PRE0083  Error  Division by zero: '<unknown>' can be zero when event 'X' in state 'Y'
```
The `<unknown>` in the error message is the diagnostic signal — it indicates the proof
obligation was not generated against the named field. Compare to `peek` without a guard:
"Division by zero: **'Items'** can be zero…" — the field is named, the guard can match it.

**Note on root cause ambiguity:** It is not yet clear whether `<unknown>` originates in the
proof engine (the obligation is generated against a genuinely unnamed synthetic variable) or
in the MCP diagnostic serialization layer (the proof engine has a proper internal name but
it is not being mapped through to the diagnostic message). Either is possible:
- If the proof engine owns it: the obligation for `pop`/`dequeue` is built differently from
  `peek` internally, and the fix is in the proof engine's obligation generation.
- If the MCP serialization owns it: the diagnostic formatter is dropping the variable name
  that the proof engine does have, and the fix is small and isolated to the formatter.

Investigating where `<unknown>` is introduced — proof engine or diagnostic serialization —
should be the first step when triaging this bug.

### Evidence

| Variant | Error |
|---------|-------|
| `pop Steps into LastStep` (no guard) | PRE0083 `'<unknown>'` |
| `pop Steps into LastStep` with `when Steps.count > 0` guard | PRE0083 `'<unknown>'` (unchanged) |
| `pop Steps into LastStep` with `notempty` on field | PRE0083 `'<unknown>'` (unchanged) |
| `dequeue SimpleQ into Popped` with `when SimpleQ.count > 0` guard | PRE0083 `'<unknown>'` (unchanged) |
| `dequeue PriQ` (no into) with `when PriQ.count > 0` guard | PRE0083 `'<unknown>'` (unchanged) |
| `set TopItem = Items.peek` (no guard) | PRE0083 `'Items'` |
| `set TopItem = Items.peek` with `when Items.count > 0` guard | **Clean** ✓ |

All `dequeue` variants (with `into`, without `into`, simple queue, priority queue) exhibit the
same `<unknown>` proof obligation failure. The bug is not specific to the `into G` form.

### Minimal repro
```
precept BugRepro008
field Steps as stack of string
field LastStep as string optional
state Active initial
state Done terminal
event Pop
from Active on Pop
    when Steps.count > 0
    -> pop Steps into LastStep
    -> transition Done
```
Expected: compiles cleanly — `when Steps.count > 0` is a valid proof that the stack
is non-empty, satisfying the `UnguardedCollectionMutation` obligation.
Actual:
```
PRE0083  Error  Division by zero: '<unknown>' can be zero when event 'Pop' in state 'Active'
```
Same result even with `field Steps as stack of string notempty`.

### Affected samples
- `samples/warranty-repair-request.precept` — `pop RepairSteps into LastReversedStep` with `when RepairSteps.count > 0`
- `samples/parcel-locker-pickup.precept` — `Items.peek` after `push` in same row (note: `peek` satisfies proof via guard; affected if a `pop` exists)
- `samples/library-hold-request.precept` — `dequeue` with compound `and` guard
- Any sample using `pop` or `dequeue`

---

## BUG-009 — `for` operator resolves to key type instead of value type **[Compiler]**

### Status
Open

### Description
The `for` infix operator retrieves a value from a `lookup of K to V` field: `F for K`
should produce a result of type `V`. The type checker instead resolves the expression
to type `K` (the key type), causing assignment type errors whenever the key and value
types differ.

The error fires regardless of what the RHS key expression is — a string literal, a field
reference, or an event argument — confirming the bug is in the `for` expression's result
type resolution, not in key expression handling.

### Spec reference
- `precept-language-spec.md` line 1356:
  "To retrieve the value for key `K` from a `lookup of K to V` field `F`, use the
  infix `for` operator: `F for K` — result type is `V`."
- `precept-language-spec.md` line 738 (Pratt left-denotation table):
  `` `For` | `BinaryExpression(LookupAccess, ParseExpression(41))` — lookup field access;
  left operand is the `lookup of K to V` field, right is the key expression `K`;
  result type is `V` ``

### Errors reported
```
PRE0018  Expected a lookup value here, but got 'string'
```
In `set SelectedPrice = Prices for "standard"` where `Prices` is `lookup of string to number`
and `SelectedPrice` is `number`: the type checker resolves `Prices for "standard"` as
`string` (the key type) rather than `number` (the value type), then fails the assignment
type check. The error message "Expected a lookup value here" is itself misleading — it
appears to say that the SET target expects a "lookup value" when in fact it expects a `number`.

### Minimal repro
```
precept BugRepro009
field Prices as lookup of string to number
field SelectedPrice as number default 0
state Open initial
state Done terminal
event Lookup
from Open on Lookup
    -> set SelectedPrice = Prices for "standard"
    -> transition Done
```
Expected: compiles cleanly — `Prices for "standard"` returns `number` (the value type).
Actual:
```
PRE0018  Error  Expected a lookup value here, but got 'string'
```
Same error with a field key (`Prices for Category`) and an event arg key (`Prices for Lookup.Key`).

### Affected samples
- Any sample using `lookup of K to V` with the `for` operator

---

## BUG-010 — `choice` literal not typed as choice in comparison position **[Compiler]**

### Status
Open

### Description
Literals are accepted in assignment position for choice fields
(e.g., `set Priority = "High"` works; invalid choices produce `ChoiceLiteralNotInSet`).
However, the same literal in a comparison position is rejected with PRE0018 "Expected a
choice value here, but got '...'". This affects ALL choice element types — `choice of string`
with string literals, and `choice of integer` with integer literals.

The `precept_operations` catalog confirms that choice comparison operators are
`ChoiceEqualsChoice` (both sides must be `Choice` type). The type checker applies this
strictly, treating all literals as their raw scalar type regardless of context. As a result
there is no way to compare a `choice` field to a specific declared value in any boolean
expression position (`when`, `rule`, `ensure`).

### Spec reference
- `precept-language-spec.md` line 1564–1566 (choice validation):
  - "Non-choice assigned to choice: `set Priority = someStringVar` → `NonChoiceAssignedToChoice`"
  - "Choice literal not in set: `set Priority = "Unknown"` → `ChoiceLiteralNotInSet`"
  The existence of `ChoiceLiteralNotInSet` proves the type checker has a concept of
  "string literal in a choice context". Comparison position should be treated identically
  to assignment position.

### Errors reported
```
PRE0018  Expected a choice value here, but got 'string'    (choice of string)
PRE0018  Expected a choice value here, but got 'integer'   (choice of integer)
```

### Confirmed failing cases
| Field type | Comparison | Error |
|------------|-----------|-------|
| `choice of string("Low","High")` | `Priority == "High"` | PRE0018 ❌ |
| `choice of string(...) ordered` | `Priority >= "Low"` | PRE0018 ❌ |
| `choice of integer(1, 2, 3)` | `Severity == 2` | PRE0018 ❌ |

### Minimal repro
```
precept BugRepro010
field Priority as choice of string("Low", "Medium", "High") default "Low" writable
state Open initial
state Done terminal
event Escalate
from Open on Escalate
    when Priority == "High"
    -> transition Done
```
Expected: compiles cleanly — `"High"` is a valid declared choice value and should be
typed as `choice` in comparison context.
Actual:
```
PRE0018  Error  Expected a choice value here, but got 'string'
```

### Affected samples
- `samples/hiring-pipeline.precept` — likely uses choice status comparisons in guards
- Any sample guarding on a `choice` field value

---

## BUG-011 — State entry/exit hook actions not serialized in MCP definition output **[MCP-definition]**

### Status
Open

### Description
`to State -> action` (entry hook) and `from State -> action` (exit hook) declarations
compile without errors, but the actions they declare do not appear anywhere in the
`precept_compile` definition output. The states are present, the events are present,
but the hook actions are silently absent.

### Spec reference
- `precept-language-spec.md` line 808: `to | -> | state action (entry hook)`
- `precept-language-spec.md` line 811: `from | -> | state action (exit hook)`
- `precept-language-spec.md` lines 861–868: state action grammar:
  ```
  (to|from) StateTarget ("when" BoolExpr)?
  ("->" ActionStatement)*
  ```

### Minimal repro
```
precept BugRepro011
field Status as string default "created"
state Draft initial
state Active
state Closed terminal
to Active
    -> set Status = "active"
from Active
    -> set Status = "leaving"
event Activate
from Draft on Activate -> transition Active
event Close
from Active on Close -> transition Closed
```
Expected: definition output includes the `to Active` and `from Active` action hooks.
Actual: definition shows states and events but no trace of hook actions.

### Note
The `precept_compile` definition DTO has fields for `fields`, `states`, `events`, and
`rules` — there is no `stateActions` or equivalent collection. This appears to be a
DTO design gap rather than silent discard at compile time, since the precept compiles
without warnings.

---

## BUG-012 — Stateless event handler actions not serialized in MCP definition output **[MCP-definition]**

### Status
Open

### Description
`on Event -> actions` (stateless event handler) declarations compile without errors
but the handler actions do not appear in the `precept_compile` definition output.
The event appears in the `events` array with `args` populated, but `rows` is always `[]`.

### Spec reference
- `precept-language-spec.md` lines 851–859: stateless event hook grammar:
  ```
  on Identifier
  ("->" ActionStatement)*
  ("ensure" BoolExpr)?
  ```

### Minimal repro
```
precept BugRepro012
field Balance as number default 0 nonnegative writable
event Deposit(Amount as number)
on Deposit
    -> set Balance = Balance + Deposit.Amount
```
Expected: definition output includes the handler actions under the `Deposit` event.
Actual:
```json
"events": [{"name":"Deposit","args":[{"name":"Amount","type":"number"}],"rows":[]}]
```
`rows` is empty; `set Balance = Balance + Deposit.Amount` is absent.

### Note
Same DTO gap as BUG-011 — the definition DTO `rows` field is populated from transition
rows only; stateless handler action chains have no equivalent serialization path.

---

## BUG-013 — `sqrt(abs(x))` proof obligation uses `<unknown>` — abs() return range not tracked **[Compiler]**

### Status
Open

### Description
The proof engine does not track that `abs(x)` always returns a non-negative value.
When `abs(x)` is the argument to `sqrt()`, the proof engine generates the
`SqrtOfNegative` obligation against an unnamed variable (`<unknown>`) rather than
against a value it knows to be non-negative. No guard or modifier can satisfy this
obligation, making `sqrt(abs(x))` unusable.

A plain field with a `when Value >= 0` guard works cleanly for `sqrt(Value)`, confirming
the proof engine can propagate field-level non-negativity. The gap is that it does not
propagate the non-negativity of `abs()`'s return value.

### Spec reference
- `precept-language-spec.md` line 1392:
  `abs(value)` → `(integer|decimal|number) → same type`
  Mathematically, `abs` always returns a non-negative value. The proof engine should
  record this as a proof fact on the result.
- `precept-language-spec.md` line 1407:
  `sqrt(value)` — "Proof engine checks non-negativity."

### Errors reported
```
PRE0084  Error  sqrt() requires a non-negative value, but '<unknown>' can be negative
```
The `<unknown>` (vs. a named field in the error for `sqrt(plainField)`) is the diagnostic
signal — same pattern as BUG-008.

### Minimal repro
```
precept BugRepro013
field Value as number default 0 writable
field Result as number default 0 nonnegative
state Open initial
state Done terminal
event Compute
from Open on Compute
    -> set Result = sqrt(abs(Value))
    -> transition Done
```
Expected: compiles cleanly — `abs(Value)` is always non-negative.
Actual:
```
PRE0084  Error  sqrt() requires a non-negative value, but '<unknown>' can be negative
```
Contrast: `sqrt(Value)` with `when Value >= 0` guard compiles cleanly.

---

## BUG-014 — `precept_proofs` CollectionEmptyOnMutation recovery hint is incorrect **[MCP-docs]**

### Status
Open

### Description
The `precept_proofs` tool lists the `CollectionEmptyOnMutation` runtime fault with the
recovery hint: "Guard the mutation action with 'when CollectionField.count > 0' in the
transition or event condition." This advice does not work — BUG-008 confirms that
`when CollectionField.count > 0` does NOT satisfy the `UnguardedCollectionMutation`
proof obligation for `pop` or `dequeue`. The obligation is generated against `<unknown>`
and cannot be discharged by any guard or modifier.

### Tool output (incorrect)
```
CollectionEmptyOnMutation:
  "Guard the mutation action with 'when CollectionField.count > 0' in the transition or event condition"
```

### What actually works
Nothing currently works (BUG-008). The recovery hint should note that this is an open
compiler bug and not suggest a workaround that doesn't function.

---

## BUG-015 — `precept_diagnostic` PRE0083 description covers only division-by-zero, not collection mutation **[MCP-docs]**

### Status
Open

### Description
`precept_diagnostic` for PRE0083 describes only the division-by-zero scenario:
- `messageTemplate`: "Division by zero: '{0}' can be zero when {1}"
- `fixHint`: "Add a guard that ensures the divisor is nonzero, e.g., 'when Divisor != 0'"
- `recoverySteps`: guard with `Divisor != 0`, or apply `positive`/`min 1` modifier

PRE0083 also fires for collection mutation operations (`pop`, `dequeue`) with the same
code but a different semantic ("collection can be empty"). The recovery advice for division
(guard the divisor field) is entirely wrong for the collection case. The diagnostic
description gives no indication that PRE0083 has a dual role, nor does it mention
collection operations.

Additionally, the suggested guard for the collection case (`when CollectionField.count > 0`)
does not work anyway (see BUG-008), so the true advice for PRE0083 in collection context
has no valid resolution currently.

### Affected tool
`precept_diagnostic` lookup for code `PRE0083`.

---

## BUG-016 — Guarded rule `when` clause not serialized in MCP definition output **[MCP-definition]**

### Status
Open

### Description
A `rule BoolExpr when GuardExpr because "..."` declaration compiles cleanly, but the
`when GuardExpr` guard is absent from the `precept_compile` definition output. The rule
appears in the `rules` array with `expression` and `because` fields only; the guard is
silently dropped.

### Spec reference
- `precept-language-spec.md` line 793: `rule BoolExpr ("when" BoolExpr)? because StringExpr`
- The optional `when` guard scopes the rule — omitting it from the DTO means consumers
  of the definition cannot reconstruct the rule's actual scope.

### Minimal repro
```
precept BugRepro016
field Amount as number default 0 nonnegative writable
field IsPremium as boolean default false writable
rule Amount >= 1000 when IsPremium because "Premium accounts require minimum balance"
state Active initial
state Closed terminal
event Close
from Active on Close -> transition Closed
```
Expected definition output:
```json
"rules": [{"expression":"Amount >= 1000", "when":"IsPremium", "because":"..."}]
```
Actual:
```json
"rules": [{"expression":"Amount >= 1000", "because":"..."}]
```
The `when` field is absent entirely.

---

## BUG-017 — `~string` (case-insensitive string) qualifier lost in MCP definition output **[MCP-definition]**

### Status
Open

### Description
Fields declared as `~string` (case-insensitive string) appear in the `precept_compile`
definition output with `"type":"string"`, discarding the CI qualifier. Consumers reading
the definition cannot distinguish `string` from `~string` fields.

The `~string` type compiles and behaves correctly (CI operators `~=`, `!~`, `~startsWith`,
`~endsWith` are enforced). The loss is purely in the serialized DTO.

### Spec reference
- `precept-language-spec.md` line 1455–1469: `~string` enforcement — the CI qualifier
  is a semantic property of the field that affects which operators are legal. Losing it
  in the DTO makes the definition output semantically incomplete.

### Minimal repro
```
precept BugRepro017
field Email as ~string default "" writable
```
Expected: `{"name":"Email","type":"~string",...}` or `{"name":"Email","type":"string","caseInsensitive":true,...}`
Actual: `{"name":"Email","type":"string",...}`

---

## BUG-018 — Collection element types lost in MCP definition output **[MCP-definition]**

### Status
Open

### Description
Every collection field in the `precept_compile` definition output drops its element type
and appears with only the collection kind name. For `lookup of K to V`, both key and value
types are lost. Consumers of the definition output cannot determine what a collection
contains.

### Affected collection types

| Declared type | Definition output `type` |
|---------------|--------------------------|
| `set of string` | `"set"` |
| `queue of string` | `"queue"` |
| `stack of string` | `"stack"` |
| `log of string` | `"log"` |
| `list of string` | `"list"` |
| `bag of integer` | `"bag"` |
| `lookup of string to number` | `"lookup"` (key AND value type absent) |

`money in 'USD'` correctly shows `"qualifier":"in 'USD'"` — so the qualifier model works
for scalar domain types. The same qualifier model should be applied to collection element
types.

### Minimal repro
```
precept BugRepro018
field Tags as set of string
field Tasks as queue of string by integer
field Prices as lookup of string to number
state Active initial
```
Expected: `{"name":"Tags","type":"set of string",...}` or a structured `elementType` field.
Actual: `{"name":"Tags","type":"set",...}` — element type completely absent.

---

## BUG-019 — Typed constants (single-quoted strings) not resolved from type context **[Compiler]**

### Status
Open

### Description
Typed constants (`'2026-01-01'`, `'2 hours'`, `'100 USD'`, etc.) are documented as
context-resolved literals — the surrounding expression type determines how the constant
is interpreted. In practice, typed constants are rejected in every tested position:

1. **In `default` modifier position** — PRE0009 "Expected declaration keyword here,
   but found `'2 hours'`". The parser does not recognize typed constant tokens as valid
   expression atoms in the `default Expr` slot.

2. **In `set` action position** — PRE0052 "Cannot determine the type of `'2026-12-31T00:00:00Z'`"
   even when the SET target is `field ExpiresAt as instant` — type context is unambiguously
   `instant` but is not propagated to the constant resolver.

3. **In rule comparison position** — PRE0052 "Cannot determine the type of `'2026-01-01'`"
   when the comparison LHS is `field StartDate as date` — again, context is deterministic
   but unused.

The `precept_diagnostic` lookup for PRE0052 confirms the trigger: "A single-quoted typed
constant appears where the surrounding context provides no type information." The diagnostic
description is incorrect — type context IS available in all three positions above; the
resolver simply does not use it.

All typed temporal, money, duration, period, and timezone constants are affected. This
makes the following practically unusable without workarounds:
- Temporal default values (`default '2026-01-01'`)
- Typed constants in rule comparisons (`rule StartDate < '2026-01-01' because "..."`)
- Typed constants in action assignments (`set ExpiresAt = '2026-12-31T00:00:00Z'`)

### Spec reference
- `precept-language-spec.md` lines 1119–1143: Typed constant content validation table.
  Context-determined types include `date`, `time`, `instant`, `datetime`, `zoneddatetime`,
  `timezone`, `duration`, `period`, `money`, `quantity`, `price`, `exchangerate`.
- `precept-language-spec.md` lines 1120–1122:
  "1. **Context determines the type.** The expression context propagates an expected type inward."

### Errors reported
- In `default` position: PRE0009 "Expected declaration keyword here, but found `'2 hours'`"
- In expression positions: PRE0052 "Cannot determine the type of `'...'` — the content does not match any known value pattern"

### Minimal repros

**In default position:**
```
precept BugRepro019a
field EventDate as date default '2026-01-01'
state Active initial
```
Expected: compiles cleanly — `'2026-01-01'` resolves to `date` from field type context.
Actual:
```
PRE0009  Error  Expected declaration keyword here, but found '2026-01-01'
```

**In set action position:**
```
precept BugRepro019b
field EventDate as date
field ExpiresAt as instant
field MeetingDuration as duration
state Active initial
event Schedule(D as date)
from Active on Schedule
    -> set ExpiresAt = '2026-12-31T00:00:00Z'
    -> set MeetingDuration = '2 hours'
    -> no transition
```
Expected: compiles cleanly — types are deterministic from the SET targets.
Actual:
```
PRE0052  Error  Cannot determine the type of '2026-12-31T00:00:00Z' — the content does not match any known value pattern
PRE0052  Error  Cannot determine the type of '2 hours' — the content does not match any known value pattern
```

**In rule comparison position:**
```
precept BugRepro019c
field StartDate as date
rule StartDate < '2026-01-01' because "Must be a past date"
state Active initial
```
Expected: compiles cleanly — LHS type (`date`) determines constant type.
Actual:
```
PRE0052  Error  Cannot determine the type of '2026-01-01' — the content does not match any known value pattern
```

---

## BUG-020 — Guarded ensures (`when` guard) not parsed in any position **[Compiler]**

### Status
Open

### Description
The spec documents that state ensures and event ensures support an optional `when` guard.
The compiler rejects `when` in every tested position for ensures:

- `in State when Guard ensure Expr because "..."` (guard before verb) → PRE0009
- `in State ensure Expr when Guard because "..."` (guard after expression) → PRE0009
- `on Event when Guard ensure Expr because "..."` (event ensure, guard before verb) → PRE0009
- `on Event ensure Expr when Guard because "..."` (event ensure, guard after expression) → PRE0009

All three produce "Expected disambiguation keyword here, but found 'when'" — the parser
does not handle the `when` guard in ensures at all.

This means conditional constraint scoping (the guard-based precision described in §3A.1
as a key governance feature) is completely broken for ensures. Every `ensure` must apply
unconditionally.

### Spec reference
- `precept-language-spec.md` line 813:
  "All three [preposition keywords] support an optional `when` guard between the state
  target and the verb (except `from ... on`, where the guard is inside the transition row)"
- `precept-language-spec.md` lines 847–849:
  ```
  (in|to|from) StateTarget ensure BoolExpr ("when" BoolExpr)? because StringExpr
  ```
- `precept-language-spec.md` lines 1686–1689 (§3A.1 examples):
  ```
  in Open when Escalated ensure Priority >= 3 because "Escalated tickets must be high priority"
  on Submit when Submit.Type == "payment" ensure Submit.Amount > 0 because "Payment amounts must be positive"
  ```

### Errors reported
```
PRE0009  Error  Expected disambiguation keyword here, but found 'when'
PRE0009  Error  Expected expression here, but found 'when'
PRE0009  Error  Expected declaration keyword here, but found '<GuardIdentifier>'
```

### Minimal repro
```
precept BugRepro020
field Escalated as boolean default false
field Priority as number default 0
state Active initial
state Closed terminal
in Active when Escalated ensure Priority >= 3 because "Escalated must have high priority"
event Close
from Active on Close -> transition Closed
```
Expected: compiles cleanly — guarded ensure is a core spec feature.
Actual: three PRE0009 errors at the `when` token.

Also fails in the guard-after form:
```
in Active ensure Priority >= 3 when Escalated because "..."
```
Same PRE0009 at the `when` token after the expression.

---

## BUG-021 — `append by P`, `enqueue by P` v3 action forms not parsed **[Compiler]**

### Status
Open

### Description
The v3 action forms for ordered collections — `append F Expr by Expr` (for `log of T by P`)
and `enqueue F Expr by Expr` (for `queue of T by P`) — are rejected by the parser. The `by`
keyword terminates the action statement prematurely, leaving the action without an outcome
and producing cascading parse errors.

Similarly, `insert F Expr at N` and `remove F at N` (also v3) fail because `at` is treated
as an unexpected keyword.

Note: `put F K = V` (also marked v3 in the spec) IS implemented and works correctly,
making these omissions inconsistent with an otherwise partially-implemented v3 action set.

### Spec reference
- `precept-language-spec.md` lines 1521–1528 (action validation table):
  - `append F Expr by Expr` → `log of T by P` — v3
  - `enqueue F Expr by Expr` → `queue of T by P` — v3
  - `insert F Expr at N` → `list of T` — v3
  - `remove F at N` → `list of T` — v3
- `precept-language-spec.md` lines 831–835 (action grammar):
  ```
  enqueue Identifier Expr "by" Expr
  append Identifier Expr "by" Expr
  insert Identifier Expr "at" Expr
  remove Identifier "at" Expr
  ```

### Errors reported
For `append F Expr by Expr` / `enqueue F Expr by Expr`:
```
PRE0016  Error  Expected a transition outcome ... but none was found
PRE0009  Error  Expected declaration keyword here, but found 'by'
```

For `insert F Expr at N` / `remove F at N`:
```
PRE0009  Error  Expected expression here, but found 'at'
PRE0016  Error  Expected a transition outcome ... but none was found
```

### Minimal repros

**`append by P` (`log of T by P`):**
```
precept BugRepro021a
field Audit as log of string by integer
state Active initial
event Log(Entry as string, Key as integer)
from Active on Log
    -> append Audit Log.Entry by Log.Key
    -> no transition
```
Expected: compiles cleanly.
Actual: PRE0016 + PRE0009 at the `by` token.

**`enqueue by P` (`queue of T by P`):**
```
precept BugRepro021b
field Tasks as queue of string by integer
state Active initial
event Enqueue(Task as string, Priority as integer)
from Active on Enqueue
    -> enqueue Tasks Enqueue.Task by Enqueue.Priority
    -> no transition
```
Expected: compiles cleanly.
Actual: PRE0016 + PRE0009 at the `by` token.

**`dequeue into G by H` (`queue of T by P`):**
```
precept BugRepro021c
field Tasks as queue of string by integer notempty
field CurrentTask as string optional
state Active initial
state Done terminal
event Process(Priority as integer)
from Active on Process
    when Tasks.count > 0
    -> dequeue Tasks into CurrentTask by Process.Priority
    -> no transition
event Finish
from Active on Finish -> transition Done
```
Expected: compiles cleanly.
Actual: PRE0016 + PRE0009 at the `by` token (plus PRE0083 from BUG-008).

**Note:** The `precept_syntax` actions catalog correctly documents all these forms — the documentation is accurate. The bug is implementation-only in the compiler's action parser.

---

## BUG-022 — Event ensures (`on Event ensure`) not serialized in MCP definition output **[MCP-definition]**

### Status
Open

### Description
`on Event ensure BoolExpr because "..."` declarations compile successfully (PRE0081
warning if the event has no transition rows, but no errors). However, the ensure
constraint does not appear anywhere in the `precept_compile` definition output. The
event entry in the `events` array contains `args` and `rows` but no `constraints`
or `ensures` collection.

This is a DTO gap: the definition schema has no slot for event-scoped ensures.

### Spec reference
- `precept-language-spec.md` line 848: `on Identifier ensure BoolExpr ("when" BoolExpr)? because StringExpr`
- `precept-language-spec.md` §3A.1: event ensures validate caller-supplied arguments.

### Minimal repro
```
precept BugRepro022
field Amount as number default 0
state Active initial
state Closed terminal
event Deposit(NewAmount as number)
on Deposit ensure Deposit.NewAmount > 0 because "Deposit must be positive"
event Close
from Active on Close -> transition Closed
```
Expected definition output includes the ensure on Deposit:
```json
"events": [{"name":"Deposit","args":[...],"rows":[],"constraints":[{"expression":"Deposit.NewAmount > 0","because":"..."}]}]
```
Actual:
```json
"events": [{"name":"Deposit","args":[{"name":"NewAmount","type":"number"}],"rows":[]}]
```
The ensure is completely absent.

---

## BUG-023 — `because` clause includes keyword in serialized value **[MCP-definition]**

### Status
Open

### Description
In the `precept_compile` definition output, the `because` field on rules and ensures
includes the `because` keyword as part of the serialized string value. Every rule and
ensure shows `"because":"because \"...\""` rather than `"because":"..."`. The keyword
is redundant in the value and prevents consumers from extracting the message without
string manipulation.

### Observed in
All rule and ensure serialization. Examples:
```json
{"expression":"Score >= 0","because":"because \"Score must be non-negative\""}
{"kind":"StateResident","anchor":"Active","expression":"Balance >= 0","because":"because \"Balance must be non-negative\""}
```

Expected:
```json
{"expression":"Score >= 0","because":"Score must be non-negative"}
```

This is consistent across all constraint kinds (global rules, state ensures, transition
ensures) — the `because` value always includes the keyword prefix.

---

## BUG-024 — `omit` declarations not reflected in MCP definition output **[MCP-definition]**

### Status
Open

### Description
`in State omit Field` declarations compile cleanly (no errors) but are completely absent
from the `precept_compile` definition output. Fields subject to `omit` in a specific
state still appear in the global field list with no indication of their per-state
exclusion. Consumers of the definition cannot determine which fields are structurally
absent in which states.

### Spec reference
- `precept-language-spec.md` line 897: `in StateTarget omit Field` — structural exclusion
- `precept-language-spec.md` line 924: "`omit` clears on state entry — field value resets
  to default on any transition into an `omit` state"

### Minimal repro
```
precept BugRepro024
field Amount as number
field InternalCode as string
state Draft initial
state Published
in Draft omit InternalCode
event Publish
from Draft on Publish -> transition Published
```
Expected: definition shows `InternalCode` is omitted in `Draft` (e.g., per-state visibility map).
Actual: `InternalCode` appears in the global fields list with no omit information; the Draft
state entry has no reference to the omit declaration.

---

## BUG-025 — Keyword-named member accessors rejected by parser **[Compiler]**

### Status
Open

### Description
The member access parser (`MemberAccessExpression`) requires the member name to be an
`Identifier` token. When a member's name is a reserved keyword in Precept, the parser
produces "Expected member name here, but found `'keyword'`" and then cascades to
PRE0020 "`.` is not available on `<type>` fields".

This affects a substantial set of documented accessors on business-domain and temporal
types whose member names collide with type keywords:

| Accessor | Type keyword? | Status |
|----------|-------------|--------|
| `money.amount` | No | ✅ Works |
| `money.currency` | Yes (`currency` is a type) | ❌ Fails |
| `quantity.amount` | No | ✅ Works |
| `quantity.unit` | No† | ✅ Works |
| `quantity.dimension` | Yes (`dimension` is a type) | ❌ Fails |
| `datetime.date` | Yes (`date` is a type) | ❌ Fails |
| `datetime.time` | Yes (`time` is a type) | ❌ Fails |
| `zoneddatetime.instant` | Yes (`instant` is a type) | ❌ (not yet tested) |
| `zoneddatetime.timezone` | Yes (`timezone` is a type) | ❌ (not yet tested) |
| `zoneddatetime.datetime` | Yes (`datetime` is a type) | ❌ (not yet tested) |
| `period.basis` | No | ✅ (not yet tested) |
| `period.dimension` | Yes (`dimension` is a type) | ❌ (not yet tested) |
| `price.currency` | Yes (`currency` is a type) | ❌ (not yet tested) |
| `exchangerate.from` | No | ✅ (not yet tested) |
| `exchangerate.to` | No | ✅ (not yet tested) |

(† `unit` is not in the spec keyword table; `unitofmeasure` is the type keyword.)

This bug renders several domain types partially or fully unusable as computed-field
sources. `zoneddatetime` in particular has only keyword-named accessors (`.instant`,
`.timezone`, `.datetime`, `.date`, `.time`) — all of them are expected to fail,
making `zoneddatetime` fields essentially read-only and undecomposable.

### Spec reference
- `precept-language-spec.md` line 1358–1360 (member access table):
  `money` → `.amount → decimal`, `.currency → currency`;
  `quantity` → `.amount → decimal`, `.unit → unitofmeasure`, `.dimension → dimension`;
  `datetime` → `.date`, `.time`, `.inZone(tz)`, and integer component accessors;
  `zoneddatetime` → `.instant`, `.timezone`, `.datetime`, `.date`, `.time`.

### Errors reported
```
PRE0009  Error  Expected member name here, but found 'currency'
PRE0009  Error  Expected declaration keyword here, but found 'currency'
PRE0020  Error  '.' is not available on money fields
```

### Minimal repro
```
precept BugRepro025
field Price as money in 'USD'
field PriceAmount as decimal <- Price.amount
field PriceCurrency as currency <- Price.currency
field DT as datetime
field DTDate as date <- DT.date
field DTTime as time <- DT.time
state Active initial
```
Expected: all four computed fields resolve successfully using documented accessors.
Actual:
- `Price.amount` ✅ compiles (not a keyword)
- `Price.currency` ❌ PRE0009 + PRE0020 (`currency` is a keyword)
- `DT.date` ❌ PRE0009 + PRE0020 (`date` is a keyword)
- `DT.time` ❌ PRE0009 + PRE0020 (`time` is a keyword)

### Severity note for `zoneddatetime`
`zoneddatetime`'s documented accessors are `.instant`, `.timezone`, `.datetime`, `.date`,
`.time` — every one of them is a type keyword. The integer component accessors (`.year`,
`.month`, etc.) are inherited via the `.datetime` decomposition path. Because ALL
first-level accessors are keyword-named, a `zoneddatetime` field stored in an action
(`set ZD = instant.inZone(tz)`) is effectively inaccessible for any computed field or
guard that needs its components. Tested and confirmed:
- `ZD.instant` ❌ PRE0009 + PRE0020
- `ZD.timezone` ❌ PRE0009 + PRE0020

### Updated accessor status table (all tested)

| Accessor | Type keyword? | Status |
|----------|-------------|--------|
| `money.amount` | No | ✅ Works |
| `money.currency` | Yes (`currency` is a type) | ❌ Fails |
| `quantity.amount` | No | ✅ Works |
| `quantity.unit` | No† | ✅ Works |
| `quantity.dimension` | Yes (`dimension` is a type) | ❌ Fails |
| `datetime.date` | Yes (`date` is a type) | ❌ Fails |
| `datetime.time` | Yes (`time` is a type) | ❌ Fails |
| `zoneddatetime.instant` | Yes (`instant` is a type) | ❌ Fails |
| `zoneddatetime.timezone` | Yes (`timezone` is a type) | ❌ Fails |
| `zoneddatetime.datetime` | Yes (`datetime` is a type) | ❌ Fails |
| `zoneddatetime.date` | Yes (`date` is a type) | ❌ Fails |
| `zoneddatetime.time` | Yes (`time` is a type) | ❌ Fails |
| `period.basis` | No | ✅ Works |
| `period.hasDateComponent` | No | ✅ Works |
| `period.hasTimeComponent` | No | ✅ Works |
| `period.years` | No | ✅ Works |
| `period.months` | No | ✅ Works |
| `price.currency` | Yes (`currency` is a type) | ❌ Fails |
| `exchangerate.amount` | No | ✅ Works |
| `exchangerate.from` | Yes (`from` is a keyword) | ❌ Fails |
| `exchangerate.to` | Yes (`to` is a keyword) | ❌ Fails |

(† `unit` is not in the spec keyword table; `unitofmeasure` is the type keyword.)

---

## BUG-026 — `in State modify all readonly` treats `all` as field name **[Compiler]**

### Status
Open

### Description
The `all` broadcast keyword in access mode declarations (`in State modify all readonly/editable`)
is not recognised by the name binder. It is treated as a field name lookup, producing
PRE0017 "Field 'all' is not declared". Named-field forms (`in State modify Amount readonly`)
compile correctly — the bug is specific to the `all` keyword in FieldTarget position.

The error fires twice (two identical PRE0017 diagnostics) suggesting the broadcast form
traverses two resolution paths and fails both.

### Spec reference
- `precept-language-spec.md` line 887: `FieldTarget := identifier ("," identifier)* | all`
- `precept-language-spec.md` line 894: `in StateTarget modify all readonly ("when" BoolExpr)?`
- `precept-language-spec.md` line 923: "`all` forms … are exempt" from redundancy checks —
  explicitly documented as valid.

### Errors reported
```
PRE0017  Error  Field 'all' is not declared
PRE0017  Error  Field 'all' is not declared  (fires twice)
```

### Minimal repro
```
precept BugRepro026

field Name as string writable
field Amount as number
field Notes as string

state Active initial
state Closed terminal

in Closed modify all readonly

event Close
from Active on Close -> transition Closed
```
Expected: compiles cleanly — all three fields become read-only in Closed state.
Actual:
```
PRE0017  Error  Field 'all' is not declared  (line 11, twice)
```
Named-field form compiles without error: `in Closed modify Amount readonly`.

---

## BUG-027 — `choice of T(...)` type not valid in event arg declarations **[Compiler]**

### Status
Open

### Description
When a `choice` type is used as the type of an event argument, the parser produces
PRE0009 at the `of` token. The spec grammar says `ArgDecl := Identifier as TypeRef` and
`TypeRef := ScalarType | CollectionType | ChoiceType`, making choice types valid in all
TypeRef positions including event arguments. The parser's event arg type parsing appears
to use a restricted TypeRef production that excludes ChoiceType.

### Spec reference
- `precept-language-spec.md` line 785: `ArgDecl := Identifier as TypeRef FieldModifier*`
- `precept-language-spec.md` line 933: `TypeRef := ScalarType TypeQualifier? | CollectionType | ChoiceType`

### Errors reported
```
PRE0009  Error  Expected declaration keyword here, but found 'of'
```

### Minimal repro
```
precept BugRepro027

field Priority as choice of string("Low", "Medium", "High") default "Low" writable

state Active initial
state Done terminal

event Prioritize(Level as choice of string("Low", "Medium", "High"))
from Active on Prioritize
    -> set Priority = Prioritize.Level
    -> no transition

event Finish
from Active on Finish -> transition Done
```
Expected: compiles cleanly — `Level as choice of string(...)` is a valid arg declaration.
Actual: PRE0009 at `of` (col 34) on the event declaration line.

Note: `choice of T(...)` as a field type declaration compiles correctly. The restriction
is arg-declaration-specific.

---

## BUG-028 — `RedundantModifier` fires wrong diagnostic code with garbled message **[Compiler]**

### Status
Open

### Description
When `nonnegative` and `positive` are applied to the same field, the spec says a single
`RedundantModifier` Warning (PRE0037) should fire. Instead, two diagnostics fire:
- PRE0033 (`InvalidModifierForType`) at Error severity — wrong code, wrong severity
- PRE0037 (`RedundantModifier`) at Warning severity — correct

PRE0033 fires with a garbled message template: "The 'nonnegative' constraint does not apply
to it conflicts with 'positive' fields". This looks like two separate message templates have
been incorrectly concatenated: the `InvalidModifierForType` template ("does not apply to {T}
fields") and a partial `RedundantModifier` message ("it conflicts with 'Y'").

The Error-severity PRE0033 masks the correct Warning — consumers see an error when the spec
says the condition is a warning at most.

### Spec reference
- `precept-language-spec.md` line 1510: "Redundant modifier | `nonnegative` and `positive`
  on the same field (`positive` subsumes `nonnegative`) | `RedundantModifier` (warning)"
- PRE0033 is `InvalidModifierForType` — "A field constraint modifier applied to a field type
  that does not support it" — `nonnegative` DOES apply to `number`; this code should not fire.

### Errors reported
```
PRE0033  Error    The 'nonnegative' constraint does not apply to it conflicts with 'positive' fields
PRE0037  Warning  'nonnegative' is unnecessary — 'positive' already implies it
```

### Minimal repro
```
precept BugRepro028

field Score as number nonnegative positive default 0

state Active initial
```
Expected:
```
PRE0037  Warning  'nonnegative' is unnecessary — 'positive' already implies it
```
Actual: both PRE0033 (Error) and PRE0037 (Warning) fire. PRE0033 message is garbled.

---

## BUG-029 — `InvalidModifierBounds` check not enforced **[Compiler]**

### Status
Open

### Description
The spec requires a compile error when `min` exceeds `max` (or `minlength` > `maxlength`,
or `mincount` > `maxcount`) on the same field. The `InvalidModifierBounds` diagnostic is
documented but not enforced — `min 100 max 50` on a `number` field compiles without any error
or warning. Semantically contradictory bounds are accepted silently.

### Spec reference
- `precept-language-spec.md` line 1504: "| `min` > `max` | `min` value exceeds `max` value on
  the same field | `InvalidModifierBounds` |"
- `precept-language-spec.md` line 1505: "`minlength` > `maxlength` | `InvalidModifierBounds`"
- `precept-language-spec.md` line 1506: "`mincount` > `maxcount` | `InvalidModifierBounds`"

### Minimal repro
```
precept BugRepro029

field Score as number min 100 max 50

state Active initial
```
Expected:
```
InvalidModifierBounds  Error  'min' value (100) exceeds 'max' value (50)
```
Actual: compiles cleanly (only PRE0108 warning about no terminal state).

Also untested but likely affected: `minlength`/`maxlength` on `string`, `mincount`/`maxcount`
on collections.

---

## BUG-030 — Computed field forward references rejected; wrong error message and missing cycle diagnostic **[Compiler]**

### Status
Open

### Description
The name binder processes fields in declaration order and fails to resolve any field name
that appears later in the source, even in computed expressions (`<-`). The spec says computed
expressions can reference "all field names except those that would form a dependency cycle"
— forward references are explicitly allowed for computed fields.

Three problems:

1. **Forward references fail**: A computed expression referencing a field declared later
   produces PRE0017 "Field 'X' is not declared", even though X is declared in the same precept.

2. **Wrong error message**: The companion error PRE0054 says "Default value for 'F' cannot
   reference 'Y', which is declared later" — but the field uses `<-` (computed expression),
   not `default`. The message incorrectly calls it a "Default value".

3. **`CircularComputedField` never fires**: When a genuine cycle exists (`A <- B` and `B <- A`),
   the expected `CircularComputedField` diagnostic doesn't fire — instead PRE0017 fires because
   the second field isn't registered yet when the first is checked. The cycle detector is
   effectively bypassed.

### Spec reference
- `precept-language-spec.md` §3.6 (Computed field validation table, line 1548–1554):
  - Self-reference → `CircularComputedField`
  - Transitive cycle → `CircularComputedField`
  - "Computed expression: All field names except those that would form a dependency cycle"
- `precept-language-spec.md` line 1554: "Computed with default | Field has both `<-` and `default` | `ComputedFieldWithDefault`"
  (distinct from forward-reference restriction which applies only to `default`)

### Errors reported
```
PRE0017  Error  Field 'Name' is not declared
PRE0054  Error  Default value for 'Summary' cannot reference 'Name', which is declared later
```
(Message says "Default value" but Summary uses `<-` computed syntax.)

### Minimal repro — forward reference
```
precept BugRepro030a

field Summary as string <- Name + " (" + Score + ")"
field Name as string default "unknown" writable
field Score as number default 0 writable

state Active initial
```
Expected: compiles cleanly — Summary references Name and Score which are declared later
but form no cycle.
Actual: PRE0017 + PRE0054 for both Name and Score.

### Minimal repro — cycle detection
```
precept BugRepro030b

field A as number <- B + 1
field B as number <- A + 1

state Active initial
```
Expected: `CircularComputedField` error on both A and B (mutual cycle).
Actual: PRE0017 "Field 'B' is not declared" (because B hasn't been registered yet when A
is checked) — the cycle is never detected, only the forward reference failure is reported.

---

## BUG-031 — String interpolation not supported in `reject`, `because`, and `rule` positions **[Compiler]**

### Status
Open

### Description
The spec defines string interpolation (`{expr}`) for all double-quoted string literals
(§1.3, line 479): "Both quoted forms support `{expr}` interpolation." The Outcome grammar
says `reject StringExpr`, `rule BoolExpr because StringExpr`, and `ensure BoolExpr because StringExpr`
— where `StringExpr` admits interpolated strings.

The lexer correctly tokenizes interpolated strings: `"Hello {Name}"` produces a
`StringStart("Hello ")` token, then `Identifier(Name)`, then `StringEnd("")`. However, the
parser's handlers for `reject`, `because`, and `rule` read a single `StringLiteral` token
and don't handle the `StringStart`/`StringMiddle`/`StringEnd` token sequence. When the
lexer produces `StringStart`, the parser sees a token it doesn't expect and emits PRE0016
("Expected a transition outcome … but none was found") and PRE0009 ("Expected declaration
keyword here, but found '…'") — the truncated string prefix is the content of the
`StringStart` token.

### Spec reference
- `precept-language-spec.md` line 479: "Both quoted forms support `{expr}` interpolation"
- `precept-language-spec.md` line 502–508: Lexer decomposition of interpolated strings into
  `StringStart`/`Identifier`/`StringMiddle`/`StringEnd` token sequence
- `precept-language-spec.md` line 839: `Outcome := ... | reject StringExpr`
- `precept-language-spec.md` line 793: `rule BoolExpr ("when" BoolExpr)? because StringExpr`
- `precept-language-spec.md` line 847: `(in|to|from) StateTarget ensure BoolExpr … because StringExpr`

### Errors reported
```
PRE0016  Error  Expected a transition outcome ('-> transition State', '-> no transition', or '-> reject Message') but none was found
PRE0009  Error  Expected declaration keyword here, but found 'Insufficient funds: balance is '
```
The truncated string prefix in PRE0009 is the `StringStart` token content up to the first `{`.

### Minimal repro
```
precept BugRepro031

field Balance as number default 0 nonnegative writable

state Active initial
state Closed terminal

event Withdraw(Amount as number)
from Active on Withdraw
    -> reject "Insufficient funds: balance is {Balance}"

event Close
from Active on Close -> transition Closed
```
Expected: compiles cleanly — `{Balance}` interpolates the Balance field value into the
reject message.
Actual: PRE0016 + PRE0009. Plain `reject "Insufficient funds"` (no interpolation) compiles
correctly.

Plain-string `reject` works; the bug is specific to interpolated string expressions.

`because` clauses exhibit the same failure — `because "Balance for {Name} is low"` produces
PRE0009 "Expected string literal here, but found 'Balance for '" (a more specific error message
than the reject case, confirming the `because` parser also reads a single `StringLiteral` and
doesn't handle `StringStart` sequences). Plain `because "..."` works correctly.

---

## BUG-032 — `reject` outcomes not serialized in MCP definition rows **[MCP-definition]**

### Status
Open

### Description
Transition rows with `-> reject "..."` outcomes appear in the `precept_compile` definition
output as `{"fromStates":[...],"actions":[]}` — with no `toState` (correct, reject has no
target), but also with no `outcome`, `rejectMessage`, or equivalent field. The reject
outcome is completely invisible to consumers of the definition. Both the outcome kind and
the message string are lost.

### Spec reference
- `precept-language-spec.md` line 839: `Outcome := transition Identifier | no transition | reject StringExpr`
- Three distinct outcome kinds; all three should be distinguishable in the definition output.

### Evidence
Definition output for a row with `-> reject "Insufficient funds"`:
```json
{"fromStates":["Active"],"actions":[]}
```
Compare to `-> no transition` row (also no `toState`):
```json
{"fromStates":["Active"],"actions":[]}
```
Both rows are indistinguishable. Only `-> transition StateName` adds a `toState` field.
Neither `no transition` nor `reject` carry any distinguishing property.

### Minimal repro
```
precept BugRepro032

field Balance as number default 0 nonnegative writable

state Active initial
state Closed terminal

event Withdraw(Amount as number)
from Active on Withdraw
    when Withdraw.Amount <= Balance
    -> set Balance = Balance - Withdraw.Amount
    -> no transition
from Active on Withdraw
    -> reject "Insufficient funds"

event Close
from Active on Close -> transition Closed
```
Expected definition rows for Withdraw:
```json
[
  {"fromStates":["Active"],"guard":"Withdraw.Amount <= Balance","actions":["set Balance = ..."],"outcome":"no transition"},
  {"fromStates":["Active"],"actions":[],"outcome":"reject","rejectMessage":"Insufficient funds"}
]
```
Actual:
```json
[
  {"fromStates":["Active"],"guard":"Withdraw.Amount <= Balance","actions":["set Balance = ..."]},
  {"fromStates":["Active"],"actions":[]}
]
```

---

## BUG-033 — Event arg `optional` modifier not reflected in MCP definition **[MCP-definition]**

### Status
Open

### Description
Event argument entries in the `precept_compile` definition output do not carry an `isOptional`
property, even when the arg is declared `optional`. Field entries correctly include
`"isOptional":true` when the `optional` modifier is present. The arg serialization uses
a stripped-down DTO that drops this property.

### Spec reference
- `precept-language-spec.md` line 785: `ArgDecl := Identifier as TypeRef FieldModifier*`
  — `optional` is a valid field modifier on event args.

### Evidence
Field with `optional`: `{"name":"Nickname","type":"string","isOptional":true,...}` ✅
Arg with `optional`: `{"name":"Alias","type":"string"}` — `isOptional` absent ❌

### Minimal repro
```
precept BugRepro033

field Name as string
field Nickname as string optional

state Active initial

event Update(NewName as string, Alias as string optional)
from Active on Update
    -> set Name = Update.NewName
    -> set Nickname = Update.Alias
    -> no transition
```
Expected definition:
```json
"events": [{"name":"Update","args":[
  {"name":"NewName","type":"string"},
  {"name":"Alias","type":"string","isOptional":true}
],"rows":[...]}]
```
Actual:
```json
"args":[{"name":"NewName","type":"string"},{"name":"Alias","type":"string"}]
```

---

## BUG-034 — Per-state access mode overrides not in MCP definition output **[MCP-definition]**

### Status
Open

### Description
`in State modify F editable/readonly` declarations compile cleanly but are completely absent
from the `precept_compile` definition output. State entries have no `accessModes`,
`overrides`, or equivalent slot. Consumers reading the definition cannot determine per-state
field mutability overrides.

This is distinct from BUG-024 (`omit` not in definition) — both access mode declarations
and omit declarations are invisible, but they are different constructs with different
semantics.

### Spec reference
- `precept-language-spec.md` line 905–926: Two-layer access mode composition model —
  per-state overrides change effective mutability for a (field, state) pair; consuming
  code needs this to enforce the correct access policy per state.

### Evidence
```
in Draft modify Amount editable
in Active modify Name readonly
```
Both compile cleanly (`hasErrors: false`, no diagnostics). The definition output for each
state has only `{"name":"...","modifiers":[...],"constraints":[]}` — no reference to the
per-state access mode override.

### Minimal repro
```
precept BugRepro034

field Name as string writable
field Amount as number

state Draft initial
state Active
state Closed terminal

in Draft modify Amount editable
in Active modify Name readonly

event Activate
from Draft on Activate -> transition Active

event Close
from Active on Close -> transition Closed
```
Expected: state entries include access mode info, e.g.:
```json
{"name":"Draft","modifiers":["initial"],"constraints":[],"accessModes":[{"field":"Amount","mode":"editable"}]}
```
Actual: `{"name":"Draft","modifiers":["initial"],"constraints":[]}` — no access mode info.

---

## BUG-035 — Choice element type and member values lost in MCP definition output **[MCP-definition]**

### Status
Open

### Description
`choice of T(v1, v2, ...)` fields appear in the `precept_compile` definition as `"type":"choice"`
only — the element type (`string`, `integer`, etc.) and the declared literal values are both
absent. Consumers cannot reconstruct the valid value set or enforce type-appropriate comparisons
without re-parsing the source.

This parallels BUG-018 (collection element types lost) — both use a bare type-kind string
where a richer representation is required.

Additionally, `defaultValue` for string-choice fields includes escaped inner quotes:
`"defaultValue":"\"Open\""` instead of `"defaultValue":"Open"`. Integer-choice defaults
serialize correctly (`"defaultValue":"1"`). The inconsistency is because the string literal's
surrounding quotes are being included in the serialized value.

### Minimal repro
```
precept BugRepro035

field Status as choice of string("Open", "Closed") default "Open" writable
field Level as choice of integer(1, 2, 3) default 1 writable

state Active initial
```
Expected definition:
```json
{"name":"Status","type":"choice","elementType":"string","values":["Open","Closed"],"defaultValue":"Open",...}
{"name":"Level","type":"choice","elementType":"integer","values":[1,2,3],"defaultValue":"1",...}
```
Actual:
```json
{"name":"Status","type":"choice","defaultValue":"\"Open\"",...}
{"name":"Level","type":"choice","defaultValue":"1",...}
```
Element type absent; values absent; string default includes surrounding quotes in value.

---

## BUG-036 — `no transition` and `reject` outcomes indistinguishable in MCP definition **[MCP-definition]**

### Status
Open

### Description
Transition rows with `-> no transition` and rows with `-> reject "..."` both serialize as
`{"fromStates":[...],"actions":[]}` — identical representations. Neither carries an `outcome`
field, so consumers cannot tell which outcome applies. This is a companion to BUG-032
(reject message not serialized) — even the outcome KIND is missing.

Only `-> transition StateName` is distinguishable: it adds `"toState":"StateName"`.

### Evidence
From a precept with three distinct outcome kinds:
```json
{"fromStates":["Active"],"guard":"...","actions":["set Balance = ..."]},  // -> no transition
{"fromStates":["Active"],"actions":[]},                                   // -> reject "..."
{"fromStates":["Active"],"actions":[]}                                    // another -> no transition (Ping)
```
All three rows without `toState` are structurally identical. A consumer cannot determine:
- Whether the row ends the operation without a state change (`no transition`)
- Whether the row explicitly rejects the event with a message (`reject`)
- What the rejection message says

### Minimal repro
```
precept BugRepro036

field Balance as number default 100 nonnegative writable

state Active initial
state Closed terminal

event Withdraw(Amount as number)
from Active on Withdraw
    when Withdraw.Amount <= Balance
    -> set Balance = Balance - Withdraw.Amount
    -> no transition
from Active on Withdraw
    -> reject "Insufficient funds"

event Ping
from Active on Ping -> no transition

event Close
from Active on Close -> transition Closed
```
Expected rows for Withdraw and Ping to carry `"outcome":"no transition"` or `"outcome":"reject"`.
Actual: all non-transition rows are `{"fromStates":[...],"actions":[]}`.

---

## BUG-037 — `in State modify all` and `in State omit all` both reject `all` keyword **[Compiler]**

### Status
Open

### Description
BUG-026 established that `in State modify all readonly` fails with PRE0017 "Field 'all' is
not declared". Testing confirms the same bug applies to `in State omit all` — the `all`
keyword in FieldTarget position is not recognized in either verb's parsing context.

This means both the mutability broadcast form AND the structural exclusion broadcast form
are broken. Only named-field forms (`modify F readonly`, `omit F`) work.

### Update to original BUG-026
BUG-026 is superseded by this entry. Original BUG-026 covered `modify all` only;
this entry documents that BOTH verbs (`modify` and `omit`) fail with `all`.

### Minimal repro — `omit all`
```
precept BugRepro037

field Name as string
field InternalCode as string
field AuditRef as string

state Draft initial
state Published terminal

in Draft omit all

event Publish
from Draft on Publish -> transition Published
```
Expected: compiles cleanly — all three fields are structurally absent in Draft.
Actual: `PRE0017  Error  Field 'all' is not declared`

---

## BUG-038 — `InvalidModifierBounds` not enforced for `minlength`/`maxlength` and `mincount`/`maxcount` **[Compiler]**

### Status
Open

### Description
BUG-029 established that `min > max` on numeric fields is not enforced. Testing confirms
the same gap exists for `minlength > maxlength` on `string` and `mincount > maxcount` on
collection fields. All three `InvalidModifierBounds` variants compile silently with
contradictory bounds.

### Evidence
`field Code as string minlength 10 maxlength 5` — compiles without error.
`field Tags as set of string mincount 5 maxcount 2` — compiles without error.

BUG-029 covers `min`/`max` on numeric types. BUG-038 covers `minlength`/`maxlength` and
`mincount`/`maxcount`. The root cause is the same missing validation pass.

### Spec reference
- `precept-language-spec.md` line 1505: "`minlength` > `maxlength` | `InvalidModifierBounds`"
- `precept-language-spec.md` line 1506: "`mincount` > `maxcount` | `InvalidModifierBounds`"

---

## BUG-039 — `list.at(N)` method call rejected due to `at` keyword collision **[Compiler]**

### Status
**Fixed** — Retested 2026-05-10 (round 5)

### Resolution
The `at` keyword collision was resolved in a prior build. The spec was also updated: the Member
Access table now lists `count > 0` as the proof requirement for `list.at(N)` (same policy as
`.first`, `.last`, `.peek`). PRE0063 fires correctly when the list is not guarded `notempty`,
and compiles clean with `notempty`.

### Verified behaviour
- Without `notempty`: PRE0063 fires — "Steps may be empty — guard with `if Steps.count > 0`" ✅ (correct per updated spec)
- With `notempty`: compiles clean ✅

### Description (historical)
`list.at(N)` is a v3 accessor that retrieves the element at zero-based index `N`. The parser
rejected the member access because `at` is a reserved keyword (used in `insert F Expr at N`
action form). The member access parser required the member name to be an `Identifier` token,
and `at` was tokenized as a keyword.

This was the same root cause as BUG-025 (keyword-named member accessors rejected), extended
to the v3 `list.at` method.

### Spec reference
- v3 accessor table (spec line 1351): `list.at(N)` → `T`, Proof: `count > 0`

### Errors reported (historical)
```
PRE0009  Error  Expected member name here, but found 'at'
PRE0009  Error  Expected declaration keyword here, but found 'at'
PRE0020  Error  '.' is not available on list fields
```

### Minimal repro (historical)
```
precept BugRepro039

field Steps as list of string notempty
field ThirdStep as string optional <- Steps.at(2)

state Active initial
```
Expected: compiles cleanly — `Steps.at(2)` retrieves the element at index 2.
Actual (before fix): PRE0009 × 2 + PRE0020.

---

## BUG-040 — `queue.peekby(P)` not implemented **[Compiler]**

### Status
Open

### Description
`queue.peekby(P)` is a v3 accessor that peeks at the highest-priority entry in a
`queue of T by P` without removing it. The compiler reports PRE0020 "'.peekby' is not
available on queue fields", indicating the accessor is not in the type system for queue types.

Unlike BUG-039 (`list.at` — keyword collision), `peekby` is NOT a keyword — the accessor
simply hasn't been implemented in the type checker.

### Spec reference
- v3 accessor: `queue.peekby(P)` on `queue of T by P` fields — returns the entry whose
  priority key matches `P` without dequeuing it

### Errors reported
```
PRE0020  Error  '.peekby' is not available on queue fields
```

### Minimal repro
```
precept BugRepro040

field Tasks as queue of string by integer notempty
field NextTask as string <- Tasks.peekby(1)

state Active initial
```
Expected: compiles cleanly — `Tasks.peekby(1)` returns the entry with priority key 1.
Actual: PRE0020.

**Correction on parameter form:** Testing confirmed `Tasks.peekby` (without argument) ALSO
produces PRE0020. The catalog describes `peekby` as a parameterless accessor (returns the
ordering value of the front item). The implementation simply doesn't recognize `peekby` at all
on `queue of T by P` fields — neither the parameterless nor the parameterized form works.

**Catalog accuracy:** The `precept_types` tool correctly lists `peekby` as an accessor on the
`QueueBy` type with `proofRequirements: ["self.count > 0"]`. The documentation is accurate;
the type checker implementation is missing.

---

## BUG-041 — `UnexpectedNull` runtime fault recovery hint uses invalid Precept syntax **[MCP-docs]**

### Status
Open

### Description
The `precept_proofs` tool's `UnexpectedNull` runtime fault entry suggests:
```
"when Field != null"
```
as a guard to prevent null access. However, Precept does not have a `null` literal or a
`!= null` operator — these were explicitly removed in v2. Optional fields use `is set` /
`is not set` for presence testing.

The correct guard for an optional field access is `when Field is set`, not `when Field != null`.
The hint as written would produce a PRE0009 or PRE0018 compile error if followed literally.

### Spec reference
- `precept-language-spec.md` line 475: "`nullable`, `null`, and `edit` are not reserved in v2.
  `optional` replaces `nullable`. ... The `null` literal is removed entirely — `optional` fields
  use `is set`/`is not set` for presence testing and `clear` for value removal."

### Incorrect hint (from `precept_proofs`)
```json
{
  "code": "UnexpectedNull",
  "recoveryHint": "Add the 'optional' modifier to allow null values, or guard with 'when Field != null' before use"
}
```

### Correct guidance
```
Add the 'optional' modifier to the field declaration,
or guard access with 'when Field is set' before use.
```

### Minimal repro to confirm `!= null` is invalid
```
precept BugRepro041

field Name as string optional

rule Name != null because "check"

state Active initial
```
Expected error: any compile error (since `null` is not valid Precept syntax).
Also applies to: any guard using `Field != null` instead of `Field is set`.

---

## BUG-042 -- Modifier bound values not serialized in MCP definition output [MCP-definition]

### Status
Open

### Description
Numeric modifier bounds -- min, max, minlength, maxlength, mincount, maxcount, and maxplaces
-- appear in the modifiers array of the definition output as bare strings without their
associated values. Consumers cannot determine the actual bounds without re-parsing source.
Only defaultValue receives special treatment. All bound modifiers should have named
properties (minValue, maxValue, etc.) or a structured representation.

### Evidence
field Score as number min 0 max 100 default 50
-> "modifiers":["min","max","default"],"defaultValue":"50"
Values for min/max absent. Same for minlength/maxlength and mincount/maxcount.

### Minimal repro
precept BugRepro042
field Code as string minlength 3 maxlength 20
field Score as number min 0 max 100 default 50
field Tags as set of string mincount 1 maxcount 10
state Active initial

---

## BUG-043 -- String default values include surrounding DSL quotes in serialized form [MCP-definition]

### Status
Open

### Description
String-typed field defaults are serialized with DSL token surrounding double quotes included
as part of the value. field Name as string default "hello" produces defaultValue:"\"hello\""
instead of defaultValue:"hello". The serializer uses raw DSL token text including delimiters
rather than the extracted string value. Same bug affects choice string defaults (BUG-035).
Non-string scalar types (number, integer, decimal, boolean) are serialized correctly.

### Evidence table
| Default declaration | Actual defaultValue | Expected |
|---------------------|---------------------|---------|
| default "" (string) | "\"\"" | "" |
| default "hello" | "\"hello\"" | "hello" |
| default "Open" (choice) | "\"Open\"" | "Open" |
| default 0 (number) | "0" OK | "0" |
| default false (boolean) | "false" OK | "false" |
| default 1 (choice int) | "1" OK | "1" |

### Minimal repro
precept BugRepro043
field First as string default "" writable
field Last as string default "unknown" writable
state Active initial
-> First shows defaultValue:"\"\"" instead of ""
-> Last shows defaultValue:"\"unknown\"" instead of "unknown"

---

## BUG-044 -- Guarded state actions (`from State when G -> action`) not supported [Compiler]

### Status
Open

### Description
The spec defines that state action declarations (`to State -> action`, `from State -> action`)
support an optional `when` guard: "All three support an optional `when` guard between the
state target and the verb." The compiler rejects the guard with PRE0009 "Expected
disambiguation keyword here, but found 'when'" and double-fires the error (same double-firing
pattern as BUG-001, BUG-037).

Unguarded state actions (`to State -> action`, `from State -> action`) compile correctly.
Only the guarded form (`from State when G -> action`) fails.

### Spec reference
- `precept-language-spec.md` line 813: "All three [state action forms] support an optional
  `when` guard between the state target and the verb."

### Errors reported
PRE0009 Expected disambiguation keyword here, but found 'when'  (fires twice)

### Minimal repro
```
precept BugRepro044
field Balance as number default 0 nonnegative writable
state Active initial
state Closed terminal
from Active when Balance > 100 -> set Balance = 0
event Close
from Active on Close -> transition Closed
```

---

## BUG-045 -- `ascending`/`descending` modifiers not recognized in log type declarations [Compiler]

### Status
Open

### Description
`log of T by P ascending` and `log of T by P descending` are documented as valid field
type declarations. The `ascending` and `descending` keywords appear in the reserved keyword
list and are documented as log ordering modifiers. The parser rejects them with PRE0009
"Expected declaration keyword here, but found 'ascending'/'descending'".

### Spec reference
- `precept-language-spec.md` keywords table: `ascending descending` listed as reserved keywords
- `precept_types` reference confirms `log` type with ordering modifiers

### Errors reported
PRE0009  Expected declaration keyword here, but found 'ascending'
PRE0009  Expected declaration keyword here, but found 'descending'

### Minimal repro
```
precept BugRepro045
field AscLog as log of string by integer ascending
field DescLog as log of string by integer descending
state Active initial
```

---

## BUG-046 -- CI enforcement not applied to quantifier binding variables [Compiler]

### Status
Open

### Description
When iterating over a `set of ~string` (CI collection) with a quantifier (`each`/`any`/`no`),
the binding variable inherits the `~string` type from the element type. The compiler should
require `~=` for comparisons on the binding variable and reject `==` with
`CaseInsensitiveFieldRequiresTildeEquals`. Instead, `each T in CISet (T == "value")` silently
compiles without any warning or error. The `~=` form also compiles (correctly), meaning the
fix does not produce false positives -- the compiler just does not enforce CI in this position.

### Spec reference
- `precept-language-spec.md` CI enforcement: when a `~string` value is compared with `==`
  instead of `~=`, `CaseInsensitiveFieldRequiresTildeEquals` must fire.

### Evidence
```
field Tags as set of ~string notempty
rule each T in Tags (T == "required") because "..."   -- should fail, silently passes
rule each T in Tags (T ~= "required") because "..."   -- correct form, also passes
```

---

## BUG-047 -- Stateless event hook actions not serialized in MCP definition output [MCP-definition]

### Status
Open

### Description
Stateless event hooks (`on EventName -> ActionChain`) compile correctly and `"isStateless":true`
is correctly set in the definition. However, the event's `rows` array is always empty (`[]`)
even when the hook declares mutations. The actions in stateless hooks are silently absent.

State-scoped transition rows (`from State on Event -> actions`) are correctly serialized.

### Evidence
```
event UpdateScore(Points as integer)
on UpdateScore -> set Score = Points
```
Definition output:
```json
{"name":"UpdateScore","args":[{"name":"Points","type":"integer"}],"rows":[]}
```
The `set Score = Points` action is absent. `rows` should contain a row with `"actions":["set Score = Points"]`.

---

## BUG-048 -- `by` keyword not recognized in `append`/`enqueue` priority actions [Compiler]

### Status
Open

### Description
The `by` keyword used to specify the sort key in priority collection actions is not recognized
by the parser. Both `append Collection Value by Key` (sorted log) and `enqueue Collection Value
by Key` (priority queue) fail with PRE0016 and PRE0009 "Expected declaration keyword here,
but found 'by'". The parser treats `by` as the start of the next declaration rather than a
continuation of the action.

The non-keyed forms (`append Collection Value`, `enqueue Collection Value`) compile correctly.
This affects all log and priority-queue fields that require a sort key.

Note: `dequeue` failures are covered by BUG-008. This bug covers the write-side `by` forms.

### Spec reference
- `precept-language-spec.md` line 833: `append Identifier Expr "by" Expr`
- `precept-language-spec.md` line 827: `enqueue Identifier Expr "by" Expr`

### Errors reported
PRE0016  Expected a transition outcome but none was found
PRE0009  Expected declaration keyword here, but found 'by'

### Minimal repro
```
precept BugRepro048
field Notes as log of string by integer
field PriQ as queue of string by integer
state Active initial
state Done terminal
event AddNote(Note as string, Seq as integer)
from Active on AddNote -> append Notes Note by Seq -> no transition
event Enq(Item as string, Pri as integer)
from Active on Enq -> enqueue PriQ Item by Pri -> no transition
event Finish
from Active on Finish -> transition Done
```

---

## BUG-049 -- `insert`/`remove at` actions fail due to `at` keyword ambiguity [Compiler]

### Status
**Fixed** — fully closed 2026-05-10

### Resolution
- `remove Collection at Index` was fixed in `a65c9fed`.
- `insert Collection Value at Index` was fixed in `f2d1dece`: `FixedReturnAccessor.ReturnNonnegative` now lets Strategy 2 discharge `count >= 0` trivially through the unified `Types.CollectionCountAccessor`.

### Description
`insert Collection Value at Index` and `remove Collection at Index` both failed because the `at`
keyword used as an action delimiter was consumed by the expression parser as the list-element
accessor infix operator (`list.at(N)`). The parser saw `Value at Index` as a single expression
(list access on Value), leaving no `at` keyword for the action grammar.

The insert path also produced a spurious PRE0084 "sqrt() requires a non-negative
value, but '<unknown>'" because the proof engine had no accessor-level way to know collection
counts cannot be negative.

The non-indexed forms (`remove Collection Value` by value for sets/bags) compile correctly.

### Spec reference
- `precept-language-spec.md` line 834: `insert Identifier Expr "at" Expr`
- `precept-language-spec.md` line 825: `remove Identifier "at" Expr`

### Errors reported
PRE0009  Expected expression here, but found 'at'  (for remove)
PRE0084  sqrt() requires a non-negative value, but '<unknown>'  (for insert -- spurious)

### Minimal repro
```
precept BugRepro049
field Items as list of string
state Active initial
state Done terminal
event Insert(Item as string, Pos as integer)
from Active on Insert -> insert Items Item at Pos -> no transition
event RemoveAt(Pos as integer)
from Active on RemoveAt -> remove Items at Pos -> no transition
event Finish
from Active on Finish -> transition Done
```

---

## BUG-050 -- `dequeue`/`pop` trigger false PRE0083 "Division by zero" [Compiler]

### Status
Open

### Description
`dequeue Collection` and `pop Collection` trigger PRE0083 "Division by zero: '<unknown>'
can be zero" even when the collection has a `notempty` constraint. The proof engine appears
to be generating a spurious division-by-zero proof obligation for collection removal actions.
These operations are not arithmetic -- they do not divide. The `notempty` modifier (which
should discharge the non-empty proof obligation for access/removal) does not help.

`push`, `enqueue` (without `by`), `add`, `remove`, and `clear` all compile without this
false positive.

### Spec reference
- `precept-language-spec.md` §5 (proof obligations): collection access is a proof obligation
  (`EmptyCollectionAccess`), not division. PRE0083 is `DivisionByZero` -- wrong diagnostic.

### Errors reported
PRE0083  Division by zero: '<unknown>' can be zero when event '...' in state '...'

### Evidence
| Action | `notempty`? | Result |
|--------|-------------|--------|
| `dequeue Q` | no | PRE0083 ❌ |
| `dequeue Q` | yes | PRE0083 ❌ |
| `pop Stack` | no | PRE0083 ❌ |
| `pop Stack` | yes | PRE0083 ❌ |
| `push Stack Item` | n/a | ✅ no error |
| `enqueue Q Item` | n/a | ✅ no error |

### Minimal repro
```
precept BugRepro050
field Q as queue of string notempty
state Active initial
state Done terminal
event Deq
from Active on Deq -> dequeue Q -> no transition
event Finish
from Active on Finish -> transition Done
```

---

## BUG-051 -- `min(a, b)` and `max(a, b)` function calls fail due to reserved keyword conflict [Compiler]

### Status
Open

### Description
`min` and `max` are documented as built-in numeric functions in the `precept_types` catalog
(with usage examples `min(score, 100)` and `max(score, 0)`). However, they are also reserved
keywords (used in `min 0` and `max 100` field modifiers). The expression parser treats `min`
and `max` as keywords and cannot dispatch them to function calls, resulting in PRE0009
"Expected expression here, but found 'min'/'max'".

`clamp(value, lo, hi)` works and provides similar functionality, but min/max are unusable.

### Spec reference
- `precept_types` functions catalog: `min(a, b)` documented with overloads for Integer/Decimal/Number/Money/Quantity
- Reserved keyword list includes `min max` (field modifier position)
- `precept-language-spec.md` §1.6: "The `(` disambiguates: constraint keywords are never followed by `(`,
  function calls always are." — this disambiguation is NOT implemented.

### Errors reported
PRE0009  Expected expression here, but found 'min'
PRE0009  Expected expression here, but found 'max'

### Cross-impact
The `precept_diagnostic PRE0030` (UndeclaredFunction) `exampleAfter` uses `min(max(X, 0), 100)` —
which is broken code because of this bug. The recovery hint for PRE0030 guides users to write
code that itself has a compile error.

### Minimal repro
```
precept BugRepro051
field A as number default 5 nonnegative writable
field B as number default 10 nonnegative writable
field Smaller as number <- min(A, B)
field Larger as number <- max(A, B)
state Active initial
```

---

## BUG-052 -- `contains` keyword unusable in expression position [Compiler]

### Status
Open

### Description
`contains` is a reserved keyword documented as a collection membership and string substring
operator. It fails in ALL expression positions:
- Infix form `Tags contains "required"` (set membership) -- PRE0018 wrong type errors
- Infix form `Name contains "Corp"` (string substring) -- PRE0018 "Expected a string, got 'string'"
- Function-call form `contains(Tags, "required")` -- PRE0009 "Expected expression here, but found 'contains'"

The string-vs-string case is especially confusing: `Name contains "Corp"` where both operands
are `string` type fails with "Expected a string value here, but got 'string'" -- both the
expected and actual types are 'string' but the compiler still rejects it.

`startsWith(str, prefix)` and `endsWith(str, suffix)` work correctly as alternatives for
prefix/suffix checks. There is no working alternative for contains-check or set-membership.

### Errors reported
PRE0018  Expected a set value here, but got 'string'     (set contains element)
PRE0018  Expected a string value here, but got 'string'  (string contains substring -- self-contradictory)
PRE0009  Expected expression here, but found 'contains'  (function-call form)

### Minimal repro
```
precept BugRepro052
field Name as string default "" writable
field Tags as set of string
rule Name contains "Corp" because "Name must contain Corp"
rule Tags contains "required" because "required tag required"
state Active initial
```

---

## BUG-053 -- `and`/`or` binary boolean operators fail in all expression positions [Compiler]

### Status
Open

### Description
`and` and `or` binary boolean operators fail in EVERY expression position with PRE0018
"Expected a boolean value here, but got 'boolean'" -- both the expected and actual types
are 'boolean' but the type checker still rejects the compound expression. Affected positions:

- Computed field: `field Both as boolean <- A and B`
- When guard: `when Score > 0 and Score < 100`
- Rule: `rule A and B because "..."`
- Ensure: `in Active ensure X > 0 and X <= 100 because "..."`
- If condition: `if A and B then 1 else 0`

`not` (unary negation) works correctly in all these positions.
Simple binary comparisons (`Score > 0`, `Flag == true`) work correctly.
Quantifiers (`each`, `any`, `no`) work correctly.
The error occurs for both `and` AND `or` -- both binary connectives are broken.

The self-contradictory error message ("Expected boolean, got boolean") suggests the type
checker computes the correct result type but fails an internal check on the compound
expression node itself.

### Spec reference
- Reserved keyword list: `and or not` listed as expression-level operators
- `precept-language-spec.md` §3.5: BoolExpr grammar includes `and`/`or`/`not` connectives

### Errors reported
PRE0018  Expected a boolean value here, but got 'boolean'  (for both `and` and `or`)

### Minimal repro
```
precept BugRepro053
field A as boolean default false writable
field B as boolean default false writable
field Both as boolean <- A and B
field Either as boolean <- A or B
state Active initial
state Done terminal
event Check
from Active on Check when A and B -> no transition
rule A and B because "Both must be true"
event Finish
from Active on Finish -> transition Done
```

---

## BUG-054 -- `ensure` clause not supported in stateless event hooks [Compiler]

### Status
Open

### Description
The spec documents that stateless event hooks support an `ensure` post-condition clause:
"The optional `ensure` clause at the end of the action chain declares a post-condition guard
-- a boolean expression that must hold after all mutations in the handler are applied."

The compiler rejects `ensure` in this position with PRE0009 "Expected declaration keyword
here, but found 'ensure'". The `ensure` keyword IS supported in state-scoped ensures
(`in State ensure ...`, `to State ensure ...`, `from State ensure ...`) and in transition
row ensure clauses (`-> ensure E because "..."` after actions). Only the stateless
hook form (`on Event -> actions ensure E because "..."`) fails.

### Spec reference
- `precept-language-spec.md` (stateless precepts section): "Event hooks without a when/ensure
  continuation... The optional ensure clause at the end of the action chain..."

### Errors reported
PRE0009  Expected declaration keyword here, but found 'ensure'

### Minimal repro
```
precept BugRepro054
field Balance as number default 100 positive writable
event Debit(Amount as number)
on Debit
    -> set Balance = Balance - Amount
    ensure Balance >= 0
```
Expected: compiles cleanly — the stateless hook fires and the post-condition is checked.
Actual: PRE0009 "Expected declaration keyword here, but found 'ensure'".

Note: The spec grammar for stateless hooks (`("ensure" BoolExpr)?`) has no `because` clause.
The form with `because` also fails, so the rejection is of the `ensure` keyword itself regardless
of what follows it.

---

## BUG-055 — PRE0097 `exampleAfter` shows wrong fix **[MCP-docs]**

### Status
Open

### Description
The `precept_diagnostic` tool's PRE0097 (`CaseInsensitiveFieldRequiresTildeStartsWith`) entry
includes an `exampleAfter` that corrects the issue by **removing the `~string` qualifier**
instead of **replacing `startsWith` with `~startsWith`**. This is the wrong fix: it discards
the intentional case-insensitive semantics of the field.

The `fixHint` correctly says: "Replace startsWith with ~startsWith for case-insensitive prefix
matching." But the `exampleAfter` shows a plain `string` field with unmodified `startsWith` —
not a `~string` field with `~startsWith`. The hint and the example contradict each other.

### Spec reference
- PRE0097 trigger: "`startsWith` used on a `~string` field — use `~startsWith` instead"
- The correct repair is: keep `~string`, change `startsWith` → `~startsWith`
- The documented repair is: change `~string` → `string` (wrong — removes CI semantics)

### Incorrect output (from `precept_diagnostic PRE0097`)
```json
{
  "fixHint": "Replace startsWith with ~startsWith for case-insensitive prefix matching",
  "exampleBefore": "field Name as ~string default \"\"\nrule startsWith(Name, \"Admin\") because \"must start with Admin\"",
  "exampleAfter":  "field Name as string default \"\"\nrule startsWith(Name, \"Admin\") because \"must start with Admin\""
}
```
`exampleAfter` changes `~string` → `string` and leaves `startsWith` unchanged.

### Correct `exampleAfter`
```
field Name as ~string default ""
rule ~startsWith(Name, "Admin") because "must start with Admin"
```

---

## BUG-056 — PRE0081 false positive on stateless-hook-only events **[Compiler]**

### Status
Open

### Description
When a stateful precept declares an event that has **only** a stateless hook (`on EventName ->
actions`) and no state-scoped transition rows (`from State on EventName ...`), the graph
analyzer emits PRE0081 "Event '...' has no transition rows in any state — it can never be
fired successfully." This is a false positive.

Stateless hooks on a stateful precept fire outside the state-transition pipeline — they are
explicitly designed to fire in any state (or in a stateless context). The absence of state-
scoped transition rows does not make the event unfirable: it just means the event is fired
globally, not from a specific state.

PRE0081 does NOT fire if the event also has at least one state-scoped transition row.

### Spec reference
- `precept-language-spec.md` (stateless event hooks): "on a stateful precept, stateless hooks
  can fire in any state"
- PRE0081 trigger condition: "Event fires from no state" — incorrect for stateless hook events

### Errors reported
```
PRE0081  Warning  Event 'Init' has no transition rows in any state — it can never be fired successfully
```

### Minimal repro
```
precept BugRepro056

field Tags as list of string writable

state Active initial

event Init
on Init
    -> set Tags = ["hello", "world"]
```
Expected: no PRE0081 — `Init` is a stateless hook and is fireable.
Actual: PRE0081 warns that `Init` can never be fired.

---

## BUG-057 — `date + period` and `date - period` arithmetic unusable — PRE0113 fires on all forms **[Compiler]**

### Status
Open

### Description
The `date + period → date` and `date - period → date` operations exist in the operations
catalog with a proof requirement: "period has date temporal dimension". However, no declaration
form for `period` fields can satisfy this proof requirement — the proof engine always reports
PRE0113 "Operand requires Date dimension but has unknown" regardless of qualifiers used.

The `precept_types` catalog documents `period` as supporting `in` (TemporalUnit) and `of`
(TemporalDimension) qualifiers. Neither form satisfies PRE0113:
- `field P as period writable` → PRE0113 (no qualifier)
- `field P as period in 'month' writable` → PRE0113 (TemporalUnit qualifier not sufficient)
- `field P as period of 'date' writable` → PRE0113 (TemporalDimension qualifier not sufficient)

Typed constant period literals (`'30 days'`) also fail as expression arguments due to BUG-019
(PRE0052 "Cannot determine the type").

The inverse operation `date - date → period` ✅ works correctly (no proof requirement).

### Operations catalog
- `DatePlusPeriod`: `date + period → date` — proofRequirements: `["period has date temporal dimension"]`
- `DateMinusPeriod`: `date − period → date` — proofRequirements: `["period has date temporal dimension"]`
- `DateMinusDate`: `date − date → period` — no proof requirements ✅

### Spec reference
- `precept_types` for `period`: qualifierShape lists `in` (TemporalUnit) and `of` (TemporalDimension)
- `precept_types` usageExample: `field GracePeriod as period default '30 days'` — this example is
  broken due to both this bug and BUG-019

### Errors reported
```
PRE0113  Error  Operand 'P' requires Date dimension but has unknown in field '...' computed expression
```

### Minimal repro
```
precept BugRepro057

field StartDate as date writable
field DatePeriod as period of 'date' writable   # qualifier matches required dimension
field ExtendedDate as date <- StartDate + DatePeriod
```
Expected: compiles cleanly — `period of 'date'` has the Date temporal dimension.
Actual: PRE0113 "Operand 'DatePeriod' requires Date dimension but has unknown".

### Workaround
None known. Date calendar arithmetic with variable periods is entirely blocked. Only
`date - date → period` (distance, no proof obligation) works.


