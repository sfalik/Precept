# Design: Conditional Event Availability via `when` Precondition

**Status: Implemented**
**Date: 2026-03-04**

> **Note:** This design document was written before implementation. Some type names referenced below (e.g. `DslTerminalRule`, `IGuardEvaluator`, `IsDefined`, `IsAccepted`, `Outcome=Undefined`, `Outcome=Blocked`) predate the current API. See [RuntimeApiDesign.md](RuntimeApiDesign.md) for the current type names. The `when` feature is fully implemented in the parser, compiler, runtime, and language server. Outcome names are now `NotApplicable` (when precondition is false), `NotDefined`, `Rejected`, `Accepted`, `AcceptedInPlace` — see `DslOutcomeKind`.

## Motivation

The DSL currently has two ways an event can be "not available" in a given state:

1. **Structurally undefined** — No `from <CurrentState> on <Event>` block exists. `Inspect` returns `IsDefined=false`, `Outcome=Undefined`. The event simply doesn't exist for that state.
2. **Runtime rejected** — The `from` block exists and is entered, but an inner branch rejects. `Inspect` returns `IsDefined=true`, `IsAccepted=false`, `Outcome=Blocked` with a reason. The event appears available but fails on fire.

There is no way to express "this event is structurally defined for this state, but should be treated as unavailable when a field-level condition is false." Consider this real example from [bank-loan.sm](../samples/bank-loan.sm):

```
from UnderReview on VerifyCollateral
    if !CollateralVerified
        set CollateralVerified = true
        no transition
    else
        reject "Collateral has already been verified"
```

Here the author's intent is: "VerifyCollateral should only be an available action when collateral hasn't been verified yet." But the DSL forces the author to use `reject` inside the block, which means:
- Inspect shows the event as `IsDefined=true` before evaluating guards
- The event appears available in the inspector preview
- The user clicks it, and only then gets the rejection — wrong UX signal

The workarounds are both unsatisfying:
- **Use `reject`** — event shows as available, fails on fire. Misleading UI.
- **Split into separate states** — encode the condition structurally (e.g., `UnderReview_NeedsCollateral` vs `UnderReview_CollateralDone`). Adds state noise when VerifyCollateral is a sub-action, not a phase change.

## Proposed Syntax

```
from <State|any> on <Event> when <Guard>
    ...body...
```

The `when <Guard>` clause is an optional precondition appended to the `from ... on` header. It uses the same expression grammar as `if` guards.

### Example — Bank Loan

Before (current workaround):
```
from UnderReview on VerifyCollateral
    if !CollateralVerified
        set CollateralVerified = true
        no transition
    else
        reject "Collateral has already been verified"
```

After (with `when`):
```
from UnderReview on VerifyCollateral when !CollateralVerified
    set CollateralVerified = true
    no transition
```

The `when` guard on the header acts as a precondition: when `CollateralVerified` is already `true`, the block is treated as if it doesn't exist for this state+event combination.

### Additional Examples

```
# Approval requires collateral OR high credit score
from UnderReview on Approve when CollateralVerified || CreditScore >= 700
    if Approve.ApprovedAmount <= RequestedAmount
        set ApprovedAmount = Approve.ApprovedAmount
        set RemainingBalance = Approve.ApprovedAmount
        transition Approved
    else
        reject "Approved amount must not exceed requested amount"

# Disbursement requires an assigned officer
from Approved on Disburse when OfficerName != null
    set DisbursementAccount = Disburse.AccountNumber
    transition Disbursed
```

## Semantic Design

### Core Semantics

The `when` guard is evaluated **before** the block body is entered:

1. **If the `when` guard evaluates to `false`**: the block is treated as if it doesn't exist for that state+event combination. No inner guards, sets, or outcomes execute.
2. **If the `when` guard evaluates to `true`**: the block is entered normally and inner `if`/`else` branches execute as usual.

This is semantically distinct from `if` guards **inside** the block:
- `when` controls whether the block is **applicable** (structural availability).
- `if` controls which branch is **selected** (routing logic within an applicable block).

### Inspect Behavior: A New Outcome

**Recommendation: Introduce `IsApplicable` rather than reuse `IsDefined=false`.**

Reusing `Outcome=Undefined` (`IsDefined=false`) would make a `when`-suppressed event indistinguishable from a completely undeclared event. This conflation is problematic for tooling:
- The inspector preview wants to show `when`-suppressed events as existing but currently not applicable (grayed out, with a tooltip showing the condition).
- Error reporting may want to distinguish "this event doesn't exist at all" from "this event exists but its precondition is currently false."
- The diagram still needs to show edges for structurally-defined-but-preconditioned transitions.

**Proposed outcome model extension:**

| Scenario | `Outcome` | `IsDefined` | `IsAccepted` | `IsApplicable` |
|---|---|---|---|---|
| No `from` block exists | `Undefined` | `false` | `false` | N/A |
| `when` guard is `false` | `NotApplicable` | `true` | `false` | `false` |
| `when` guard is `true`, inner branch accepted | `Enabled` / `NoTransition` | `true` | `true` | `true` |
| `when` guard is `true`, inner branch rejected | `Blocked` | `true` | `false` | `true` |

Add `NotApplicable` to `DslOutcomeKind` and add `IsApplicable` (bool) to `DslInspectionResult`, `DslFireResult`, and `DslInstanceFireResult`.

**Alternative considered: reuse `IsDefined=false`**: Simpler (no new fields), but loses the ability to differentiate "never existed" from "exists but precondition false." Rejected because tooling differentiation is the primary motivation for this feature.

### Multiple `from` Blocks for the Same State+Event

When multiple `from` blocks target the same `(State, Event)` combination, some with `when` and some without:

```
from UnderReview on Approve when CreditScore >= 700
    set ApprovedAmount = Approve.ApprovedAmount
    transition Approved

from UnderReview on Approve when CreditScore < 700 && CollateralVerified
    if Approve.ApprovedAmount <= RequestedAmount * 0.8
        set ApprovedAmount = Approve.ApprovedAmount
        transition Approved
    else
        reject "Without high credit, approved amount capped at 80% of requested"

from UnderReview on Approve
    reject "Neither credit score threshold met nor collateral verified"
```

**Precedence rules:**

1. All `from` blocks for the same `(State, Event)` pair contribute to the same candidate pool, ordered by their `Order` field (declaration order).
2. Candidates with `when` guards are evaluated in order. If a `when` guard evaluates to `false`, that candidate is skipped (as if it doesn't exist).
3. Candidates without `when` guards are always applicable (backward compatible).
4. If **all** candidates for a state+event have `when` guards, and **all** evaluate to `false`, the result is `NotApplicable` (the event is defined but not currently available).
5. If **some** candidates have `when` guards that pass, only those (plus any unguarded candidates) participate in the normal branch-evaluation logic.

This is consistent with the existing model where multiple `from` blocks produce ordered `DslTransition` and `DslTerminalRule` entries that are merged and evaluated in `Order` sequence.

### Event Arguments in `when` Guards

**Recommendation: `when` guards should NOT reference event arguments — only machine fields.**

Reasoning:
- The primary use case for `when` is `Inspect` without arguments — the caller wants to know "is this event available right now?" before any arguments are provided.
- If `when` could reference event args, a `when` guard couldn't be evaluated at discover-time Inspect (no args supplied yet), defeating the purpose.
- Guards that depend on event arguments belong inside the block body as `if` conditions — they are routing logic, not availability logic.

**Enforcement:**
- The parser should validate that `when` expressions reference only declared machine data fields (scalars and collection properties). Event-argument references (`EventName.ArgName`) in a `when` clause should be a parse error: `"Line N: 'when' preconditions cannot reference event arguments. Use 'if' guards inside the block body instead."`

### `when` Guards and `from any` Blocks

`from any on <Event> when <Guard>` should work — the `when` guard applies to every state the `any` expands to. This is natural: `any` expands to N source states, each getting the same `when` guard.

### Fire Behavior

When `Fire()` is called:

1. Evaluate `when` guards on all candidate blocks for `(currentState, eventName)` in order.
2. Skip candidates whose `when` guard evaluates to `false`.
3. If no candidates remain, return `NotApplicable` (same semantics as Inspect).
4. Otherwise, proceed with normal guard evaluation on remaining candidates.

This mirrors the Inspect path. The `when` evaluation is a pre-filter on the candidate pool, before the existing `ResolveTransition` logic runs.

## Architecture Impact: Where the Hook Points Are

### 1. Model Layer — `StateMachineDslModel.cs`

**`DslTransition` record** gains an optional `WhenGuardExpression` field:

```csharp
public sealed record DslTransition(
    string FromState,
    string ToState,
    string EventName,
    string? GuardExpression,
    IReadOnlyList<DslSetAssignment> SetAssignments,
    int Order = 0,
    IReadOnlyList<DslCollectionMutation>? CollectionMutations = null,
    int SourceLine = 0,
    int TargetLine = 0,
    string? WhenGuardExpression = null);  // <-- NEW
```

**`DslTerminalRule` record** gains the same field:

```csharp
public sealed record DslTerminalRule(
    string FromState,
    string EventName,
    DslTerminalKind Kind,
    string? Reason,
    string? GuardExpression = null,
    IReadOnlyList<DslSetAssignment>? SetAssignments = null,
    int Order = 0,
    IReadOnlyList<DslCollectionMutation>? CollectionMutations = null,
    int SourceLine = 0,
    string? WhenGuardExpression = null);  // <-- NEW
```

**`DslOutcomeKind` enum** gains `NotApplicable`:

```csharp
public enum DslOutcomeKind
{
    Undefined,
    Blocked,
    Enabled,
    NoTransition,
    NotApplicable   // <-- NEW
}
```

**Result records** (`DslInspectionResult`, `DslFireResult`, `DslInstanceFireResult`) gain `IsApplicable` field and a new factory method.

### 2. Parser — `StateMachineDslParser.cs`

**`FromOnRegex`** must be extended to capture an optional `when <Guard>`:

Current:
```regex
^from\s+(?<from>any|...)\s+on\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)$
```

New:
```regex
^from\s+(?<from>any|...)\s+on\s+(?<event>[A-Za-z_][A-Za-z0-9_]*)(?:\s+when\s+(?<when>.+))?$
```

**`ParseFromOnBlock`** must:
1. Extract the `when` group from the regex match.
2. Parse the `when` expression via `DslExpressionParser.Parse()`.
3. Validate that the `when` expression does not reference event arguments (only machine data fields are allowed).
4. Pass `whenGuardExpression` through to all generated `DslTransition` and `DslTerminalRule` records.

**Validation in `ValidateReferences`**: Ensure the `when` expression references only declared data fields and collection field properties.

### 3. Runtime — `StateMachineDslRuntime.cs`

**`ResolveTransition`** is the critical method. Currently it:
1. Looks up `_transitionMap[(currentState, eventName)]` and `_terminalRuleMap[(currentState, eventName)]`.
2. If neither exists → `NotDefined`.
3. Merges candidates, sorts by `Order`, evaluates guards in order.

With `when`:
1. Same lookup.
2. **Pre-filter**: Evaluate `WhenGuardExpression` on each candidate (using instance data only, no event args). Remove candidates whose `when` fails.
3. If the original lookup found candidates but **all** were removed by `when` filtering → return `NotApplicable`.
4. If no candidates existed at all → return `NotDefined` (unchanged).
5. Otherwise, proceed with normal guard evaluation on remaining candidates.

Key implementation note: The `when` guard must be evaluated against the same `evaluationData` dictionary, but it should be valid even when event arguments are not provided (since `when` cannot reference event args).

**New `TransitionResolutionKind.NotApplicable`**:

```csharp
private enum TransitionResolutionKind
{
    Accepted,
    Rejected,
    NotDefined,
    NoTransition,
    NotApplicable   // <-- NEW
}
```

**Inspect/Fire methods** must map the new resolution kind to the new `DslOutcomeKind.NotApplicable`.

### 4. Language Server

#### `SmDslAnalyzer.cs`

- **`FromOnRegex`** must match the `when` clause.
- **`GetSemanticDiagnostics`**: The unified candidate list must carry the `WhenGuardExpression`. Validate `when` expressions (type-check: must be boolean, scope-check: no event args).
- **`GetCompletions`**: After `on EventName` and space, suggest `when` as a keyword. After `when`, provide expression completions (data fields, operators) — same as guard completions but without event arg suggestions.
- **Snippet**: Update the `from/on block` snippet to include optional `when`.

#### `SmSemanticTokensHandler.cs`

- **`FromOnRegex`** must capture the `when` keyword and the guard expression.
- **`HighlightNamedSymbols`**: When the `when` group matches, push `when` as a keyword token and highlight identifiers in the guard expression as variables.
- **`KeywordTokens`**: Add `"when"`.

#### `SmPreviewHandler.cs`

- **`BuildSnapshot`**: The event status mapping must handle the new `NotApplicable` outcome:
  - Map `DslOutcomeKind.NotApplicable` → `"notApplicable"` in the `SmPreviewEventStatus.Outcome` string.
- **`HandleInspect`**: Same mapping.
- **Event enumeration**: Continue to list events whose `from` blocks exist for the current state, even if `when` evaluates to false — the event is structurally defined, just not currently applicable.

#### `SmPreviewProtocol.cs`

- **`SmPreviewEventStatus.Outcome`** string gains the `"notApplicable"` value.

### 5. VS Code Extension

#### TextMate Grammar — `state-machine-dsl.tmLanguage.json`

The `fromOnHeader` pattern must be extended to match the optional `when <guard>`:

Current pattern captures: `from <state> on <event>`
New pattern captures: `from <state> on <event>` optionally followed by `when <guard>`

New capture groups:
- `when` keyword → `keyword.control.state-machine-dsl`
- guard expression text → can remain unhighlighted at the TextMate level (semantic tokens handle the fine-grained highlighting)

#### Inspector Preview Webview — `inspector-preview.html`

The webview must handle the new `"notApplicable"` outcome:

1. **`evaluateEvent()`**: Map `"notapplicable"` to a new kind (e.g., `notApplicable`).
2. **CSS class `.notApplicable`**: Style as a muted/grayed-out variant — visible but clearly not actionable.
3. **Behavior**: `notApplicable` events should:
   - Be shown in the event bar (not hidden), so the user sees what events *would* exist if conditions change.
   - Be visually distinct from both `undefined` (never exists) and `blocked` (exists but rejected).
   - Show the `when` condition text as a tooltip or reason, so the user knows *why* it's unavailable.
   - Be non-clickable (like `undefined`).

**Recommended styling:**
- Neutral muted color (not red like blocked/undefined, not green like enabled): dim gray or low-opacity version of the event accent color.
- Glyph: `◌` (dotted circle) or `⊘` (circled slash) to indicate "not applicable."
- Reason area: Show the `when` condition text (e.g., `"Precondition: !CollateralVerified"`).

4. **Diagram edges**: Transitions from `when`-guarded blocks should appear in the diagram as dashed/dimmed edges (structurally defined but conditionally available), distinct from solid edges (always available).

## Open Questions

### Q1: Should `when` be allowed on individual branches within a block, or only on the header?

**Recommendation: Header only.**

The `when` clause is about structural availability of the entire event handler, not branch routing. Allowing `when` on individual branches would conflate it with `if` guards. The header-only position maintains the clear separation: `when` = applicability, `if` = routing.

### Q2: How should the preview render `when`-filtered events in the diagram?

Options:
1. **Hide edges** — transitions whose `when` is false don't appear in the diagram.
2. **Dashed edges** — transitions appear but are visually distinguished.
3. **Dimmed edges** — edges appear at reduced opacity.

**Recommendation: Option 2 (dashed edges)** — provides the most useful information. The user sees the full machine structure, with clear visual distinction between always-available and conditionally-available transitions.

### Q3: Interaction with `from any`

When `from any on Event when <Guard>` is used, the `when` guard applies uniformly to all expanded source states. This is straightforward. But what if the author wants different `when` conditions per state?

```
from any on Cancel when Status != "Finalized"
    transition Cancelled
```

This is fine — the guard references the same machine fields in every state. If per-state preconditions are needed, the author should use separate `from State1 on Cancel when ...` blocks.

### Q4: Interaction with Rules

`when` guards do not interact with rules. Rules are data integrity constraints checked after mutations commit. `when` is a pre-filter evaluated before the block is even entered — it's conceptually at a different level.

### Q5: Compile-time Validation

When a `when` expression can be statically evaluated against default field values (e.g., `when !CollateralVerified` where `CollateralVerified = false`), should the compiler warn that the block is always/never applicable at start?

**Recommendation: Defer.** Useful but low priority. The existing compile-time rule validation pattern could be extended, but the ROI is low for the initial implementation.

### Q6: `when` on a block that also has `from any` expansion — what if some states have their own `from State on Event` blocks?

Existing behavior: when `from any` and `from SpecificState` both target the same event, both sets of transitions are merged and ordered. The same applies with `when` — each block's `when` is evaluated independently. A `from any on Event when X` block produces candidates for all states, each with the same `when` guard. If a specific `from StateA on Event` (without `when`) also exists, its candidates are unconditionally applicable for StateA.

## DSL Syntax Contract Changes

The `from ... on` line would change to:

```text
from <any|StateA[,StateB...]> on <EventName> [when <Guard>]
```

Where `<Guard>` follows the same expression grammar as `if` guards, with the restriction that only machine data fields may be referenced (no event arguments).

## Estimated Complexity

| Component | Complexity | Notes |
|---|---|---|
| `StateMachineDslModel.cs` | Low | Add optional field to 2 records, extend 1 enum, extend 3 result records |
| `StateMachineDslParser.cs` | Medium | Extend regex, extract/validate `when` expression, scope validation |
| `StateMachineDslRuntime.cs` | Medium | Pre-filter logic in `ResolveTransition`, new outcome mapping in Inspect/Fire |
| `SmDslAnalyzer.cs` | Medium | Regex update, `when` diagnostics, completions, scope validation |
| `SmSemanticTokensHandler.cs` | Low | Regex update, keyword addition, expression highlighting |
| `state-machine-dsl.tmLanguage.json` | Low | Extend `fromOnHeader` pattern |
| `SmPreviewHandler.cs` | Low | Map new outcome string |
| `SmPreviewProtocol.cs` | Low | Document new outcome value |
| `inspector-preview.html` | Medium | New status styling, behavior, glyph, diagram edge rendering |
| Tests | High | Parser, runtime, inspect, fire, rules interaction, preview |
| `README.md` | Medium | Syntax reference, cookbook, behavior/exception table |
| `docs/DesignNotes.md` | Medium | Syntax contract update, design decision record |

## Alternatives Considered

### Alternative 1: `available when` instead of `when`

```
from UnderReview on VerifyCollateral available when !CollateralVerified
```

Pro: More explicit that this controls availability, not guard routing.
Con: Adds another keyword (`available`), the `when` keyword alone is already clear in the header position.

### Alternative 2: Block-level `precondition` statement

```
from UnderReview on VerifyCollateral
    precondition !CollateralVerified
    set CollateralVerified = true
    no transition
```

Pro: Doesn't change the header line syntax.
Con: `precondition` is inside the block body, which visually suggests it's at the same level as `if` branches and `set` statements. The design intent is that applicability is a structural concern distinct from block body logic.

### Alternative 3: Top-level event-availability declarations

```
event VerifyCollateral
    available when !CollateralVerified
```

Pro: Centralized — one place to see all availability conditions for an event.
Con: Couples event declarations to instance data in a way that's disconnected from the state context. VerifyCollateral might have different availability conditions in different states.

### Alternative 4: Reuse `IsDefined=false` (no new outcome kind)

Pro: Simplest runtime/model change.
Con: Tooling cannot distinguish "event doesn't exist in this state" from "event exists but precondition is false." The inspector preview loses the ability to show grayed-out events, which is a core UX motivation.

## File-by-File Impact Assessment

| File | Change Description |
|---|---|
| [src/StateMachine/Dsl/StateMachineDslModel.cs](../src/StateMachine/Dsl/StateMachineDslModel.cs) | Add `WhenGuardExpression` to `DslTransition` and `DslTerminalRule`. Add `NotApplicable` to `DslOutcomeKind`. |
| [src/StateMachine/Dsl/StateMachineDslParser.cs](../src/StateMachine/Dsl/StateMachineDslParser.cs) | Extend `FromOnRegex` for `when`. Extract and validate `when` guard in `ParseFromOnBlock`. Add scope validation (no event args). Update `ValidateReferences` for `when` expressions. |
| [src/StateMachine/Dsl/StateMachineDslRuntime.cs](../src/StateMachine/Dsl/StateMachineDslRuntime.cs) | Add `NotApplicable` to `TransitionResolutionKind`. Pre-filter candidates in `ResolveTransition`. Add `NotApplicable` factory methods to `DslInspectionResult`, `DslFireResult`, `DslInstanceFireResult`. Add `IsApplicable` property to result records. |
| [src/StateMachine/Dsl/DslExpressionParser.cs](../src/StateMachine/Dsl/DslExpressionParser.cs) | No change — `when` expressions use the existing expression grammar. |
| [src/StateMachine/Dsl/DslExpressionRuntimeEvaluator.cs](../src/StateMachine/Dsl/DslExpressionRuntimeEvaluator.cs) | No change — `when` expressions evaluate through the existing evaluator. |
| [tools/StateMachine.Dsl.LanguageServer/SmDslAnalyzer.cs](../tools/StateMachine.Dsl.LanguageServer/SmDslAnalyzer.cs) | Extend `FromOnRegex`. Add `when` diagnostics (boolean type, scope). Add `when` keyword to completions. Update `from/on` snippet. Add expression completions after `when`. |
| [tools/StateMachine.Dsl.LanguageServer/SmSemanticTokensHandler.cs](../tools/StateMachine.Dsl.LanguageServer/SmSemanticTokensHandler.cs) | Extend `FromOnRegex`. Add `when` to `KeywordTokens`. Highlight `when` expression identifiers. |
| [tools/StateMachine.Dsl.LanguageServer/SmPreviewHandler.cs](../tools/StateMachine.Dsl.LanguageServer/SmPreviewHandler.cs) | Map `NotApplicable` → `"notApplicable"` in snapshot builder. |
| [tools/StateMachine.Dsl.LanguageServer/SmPreviewProtocol.cs](../tools/StateMachine.Dsl.LanguageServer/SmPreviewProtocol.cs) | Document new `"notApplicable"` outcome value. |
| [tools/StateMachine.Dsl.VsCode/syntaxes/state-machine-dsl.tmLanguage.json](../tools/StateMachine.Dsl.VsCode/syntaxes/state-machine-dsl.tmLanguage.json) | Extend `fromOnHeader` pattern to match `when <guard>`. Add `when` to `controlKeywords`. |
| [tools/StateMachine.Dsl.VsCode/webview/inspector-preview.html](../tools/StateMachine.Dsl.VsCode/webview/inspector-preview.html) | Handle `"notApplicable"` status: CSS class, glyph, behavior (non-clickable, show reason), optional dashed diagram edges. |
| [test/StateMachine.Tests/DslWorkflowTests.cs](../test/StateMachine.Tests/DslWorkflowTests.cs) | Add tests: `when` parsing, inspect/fire with `when` true/false, multiple blocks with/without `when`, `when` + `from any`, `from any` with mixed `when`. |
| [test/StateMachine.Tests/DslSetParsingTests.cs](../test/StateMachine.Tests/DslSetParsingTests.cs) | Add parsing tests for `when` expression extraction, error cases (event arg refs in `when`). |
| [test/StateMachine.Dsl.LanguageServer.Tests/SmDslAnalyzerNullNarrowingTests.cs](../test/StateMachine.Dsl.LanguageServer.Tests/SmDslAnalyzerNullNarrowingTests.cs) | Add tests: `when` null narrowing interaction with inner guards. |
| [test/StateMachine.Dsl.LanguageServer.Tests/SmPreviewRulesTests.cs](../test/StateMachine.Dsl.LanguageServer.Tests/SmPreviewRulesTests.cs) | Add tests: preview snapshot with `when`-suppressed events. |
| [README.md](../README.md) | Update DSL Syntax Reference, DSL Cookbook, behavior/exception table, current-status. |
| [docs/DesignNotes.md](../docs/DesignNotes.md) | Update DSL Syntax Contract with `when` clause. Add design decision record. |
| [samples/bank-loan.sm](../samples/bank-loan.sm) | Refactor `VerifyCollateral` (and potentially `Approve`, `Disburse`) to use `when`. |

## Concerns and Risks

1. **Regex complexity**: The `FromOnRegex` in the parser currently uses `$` to anchor the end. Adding `(?:\s+when\s+(?<when>.+))?` before `$` is straightforward, but the `when` expression is free-form text (can contain operators, parentheses, etc.) — the regex relies on `.+` being greedy and the `$` anchoring end-of-line. This is the same approach used for `if` guard expressions and should work fine.

2. **Interaction with cross-branch null narrowing**: `when` guard false-path narrowing is not needed for inner branches because the `when` false path skips the entire block. If the `when` guard passes, the inner guard narrowing logic already works correctly. No additional narrowing complexity is introduced.

3. **`when` keyword conflict**: `when` is not currently used as an identifier, keyword, or type in the DSL. The risk of collision with user-chosen field/state/event names is low but nonzero. Mitigation: `when` only has keyword meaning in the specific position `from ... on ... when`, so a field named `when` would still parse in other positions.

4. **Inspector preview performance**: Each event now requires evaluating a `when` guard in addition to the existing inspect call. Since `when` guards are simple boolean expressions against instance data (no event args), the overhead is negligible.

5. **Backward compatibility**: Adding `when` is pure additive syntax. Existing `.sm` files without `when` clauses parse and behave identically. The new `NotApplicable` outcome kind is only produced when `when` guards are present.
