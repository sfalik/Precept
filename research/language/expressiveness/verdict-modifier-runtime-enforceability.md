# Verdict Modifier Runtime Enforceability Analysis

**Date:** 2026-04-11
**Author:** George (Runtime Dev)
**Context:** Shane asked: "If we are going to do this, then non-error rules should become non-blocking. Will we be able to enforce this strictly in keeping with Precept philosophy?"
**Depends on:** `research/language/expressiveness/verdict-modifiers.md` (Frank's externally grounded research)
**Answer:** **YES** — under Model D (Structural Non-Blocking) with mandatory philosophy reframing.

---

## 1. Current Constraint Enforcement Architecture

### Fire Pipeline (Transition Branch)

The `PreceptEngine.Fire()` method in `PreceptRuntime.cs` executes a strict sequential pipeline with full rollback on any failure:

| Stage | Method | What it checks | On failure |
|-------|--------|----------------|------------|
| 1. Compatibility | `CheckCompatibility()` | Workflow name, state membership, data contract, existing rule satisfaction | Returns `Undefined` |
| 2. Event argument validation | `TryValidateEventArguments()` | Type contract against event arg declarations | Returns `Rejected` |
| 3. Event asserts | `EvaluateEventAssertions()` | `on <Event> assert` rules against args-only context | Returns `Rejected` |
| 4. Guard evaluation | `ResolveTransition()` | First-match `if`/`else if`/`else` clauses | Returns `Unmatched` or `Rejected` |
| 5. Exit actions → Row mutations → Entry actions | `ExecuteStateActions()`, `ExecuteRowMutations()` | `set` assignments, collection mutations, state entry/exit actions | Returns `Rejected` on expression failure |
| 6. Post-mutation validation | `CollectConstraintViolations()` | Invariants + state asserts against post-mutation data | Returns `ConstraintFailure` |

Stage 6 is where constraint enforcement happens. Everything prior is routing and mutation.

### How Constraints Evaluate

**`EvaluateInvariants(data)`** — iterates all `PreceptInvariant` records. For each: evaluates the expression against post-mutation data via `PreceptExpressionRuntimeEvaluator.Evaluate()`. If the result is not `true`, produces a `ConstraintViolation` with `InvariantSource` and field-derived `ConstraintTarget`s.

**`EvaluateStateAssertions(anchor, state, data)`** — looks up `(AssertAnchor, State)` pairs in `_stateAssertMap`. For each matching assertion: evaluates the expression. If not `true`, produces a `ConstraintViolation` with `StateAssertionSource`.

**`CollectConstraintViolations(sourceState, targetState, data)`** — orchestrates both:
- Always evaluates invariants.
- For self-transitions: evaluates `to` + `in` asserts on target.
- For state transitions: evaluates `from` source + `to` target + `in` target.
- Collects ALL violations (collect-all semantics per Design Principle #6).

### What Produces ConstraintFailure

The `ConstraintFailure` outcome is produced ONLY when `CollectConstraintViolations()` returns a non-empty list. This happens in exactly two places in the Fire pipeline:

1. **Transition branch** (line ~752): after exit actions → row mutations → entry actions → commit collections.
2. **No-transition branch** (line ~719-720): after row mutations → commit collections, evaluates invariants + `in` asserts for current state.

The Update pipeline has an analogous stage 4 that evaluates invariants + `in` asserts.

### Key Observation: Constraint Evaluation is Already Isolated

The constraint evaluation code is cleanly separated from mutation code. `EvaluateInvariants()` and `EvaluateStateAssertions()` are pure evaluation functions — they take data, return violations. They do not mutate anything. This separation is critical: it means we can partition constraint evaluation into two passes (error-pass and warning-pass) without restructuring the mutation pipeline.

---

## 2. Four Models for Non-Blocking Warnings

### Model A: Success with Metadata

**Mechanism:** Warning-level constraint failures don't block. Operation succeeds. Warning violations are attached as metadata on the success result.

**TransitionOutcome changes:** None. `Transition` and `NoTransition` carry optional warning annotations.

**API change:**
```csharp
public sealed record FireResult(
    TransitionOutcome Outcome,
    // ... existing fields ...
    IReadOnlyList<ConstraintViolation> Violations,  // errors only (empty on success)
    IReadOnlyList<ConstraintViolation> Warnings,    // NEW: warning violations
    PreceptInstance? UpdatedInstance)
```

**Engine change:** After collecting all violations, partition into errors and warnings based on rule severity. If errors exist → `ConstraintFailure`. If only warnings → `Transition`/`NoTransition` with `Warnings` populated.

**Evaluation:**
- (+) No new outcome enum values. Minimal API surface change.
- (+) Success is still success — callers who don't check `Warnings` work unchanged.
- (−) **Violates "prevention, not detection."** A warning constraint evaluates to false, but the data mutates anyway. The invalid configuration exists. The engine allowed it.
- (−) Callers must opt in to checking `Warnings`. No forcing function.
- (−) `IsSuccess` returns `true` even though constraints were violated. Semantically misleading.

**Philosophy verdict:** Weak. The engine knowingly produces a configuration where declared rules are violated. The "prevention" guarantee no longer holds for warning rules.

### Model B: Severity-Tagged Failure (FluentValidation Model)

**Mechanism:** Warning-level constraint failures still block (produce `ConstraintFailure`). The `ConstraintViolation` record carries severity metadata. Caller inspects severity to decide what to do.

**TransitionOutcome changes:** None.

**API change:**
```csharp
public sealed record ConstraintViolation(
    string Message,
    ConstraintSource Source,
    IReadOnlyList<ConstraintTarget> Targets,
    ConstraintSeverity Severity)   // NEW: Error or Warning

// ConstraintSeverity is already in the codebase (used by DiagnosticCatalog)
```

**Evaluation:**
- (+) No engine behavior change. `ConstraintFailure` still means "constraints violated."
- (+) Mirrors FluentValidation exactly — proven model with wide adoption.
- (+) No philosophy tension: ALL constraints are prevented. Severity is metadata, not enforcement.
- (−) **Doesn't answer Shane's question.** Shane asked for non-blocking warnings. This model makes warnings blocking. The consumer has to implement non-blocking behavior themselves.
- (−) Moves enforcement decisions to the consumer, breaking the "one file, complete rules" guarantee. If the consumer ignores warning-severity violations and retries without those constraints, the `.precept` file is no longer the authoritative contract.

**Philosophy verdict:** Strong on prevention guarantee. But it punts the enforcement question to the consumer — the `.precept` file says "warning" but the engine treats it identically to "error." The semantic distinction has no runtime effect.

### Model C: New Outcome — ConstraintWarning

**Mechanism:** Warning-level constraint failures produce a new `ConstraintWarning` outcome distinct from `ConstraintFailure`. Both block — but the caller gets different enum values to distinguish severity.

**TransitionOutcome changes:**
```csharp
public enum TransitionOutcome
{
    Transition,
    NoTransition,
    Rejected,
    ConstraintFailure,     // error-level rules violated
    ConstraintWarning,     // NEW: warning-level rules violated (still blocks)
    Unmatched,
    Undefined
}
```

**Evaluation:**
- (+) Distinguishes error-level and warning-level blocking in the outcome enum.
- (+) Callers can pattern-match on `ConstraintWarning` vs `ConstraintFailure`.
- (−) **Still blocks.** Same fundamental problem as Model B — doesn't answer Shane's question.
- (−) What if both error AND warning constraints fail? Need precedence rules. `ConstraintFailure` should win (errors supersede warnings).
- (−) Adds outcome enum values but provides no new capability beyond Model B's severity metadata.

**Philosophy verdict:** Equivalent to Model B with more API surface. Prevention guaranteed. Non-blocking not achieved.

### Model D: Structural Non-Blocking (RECOMMENDED)

**Mechanism:** Error-level rules block. Warning-level rules evaluate post-mutation but don't block. The engine partitions constraint evaluation into an error pass and a warning pass. Error precedence: if ANY error-level rule fails, the entire operation blocks — warning evaluation doesn't happen.

**TransitionOutcome changes:**
```csharp
public enum TransitionOutcome
{
    // Pure success
    Transition,
    NoTransition,

    // Success with advisory violations
    TransitionWithWarnings,    // NEW
    NoTransitionWithWarnings,  // NEW

    // Failure
    Rejected,
    ConstraintFailure,
    Unmatched,
    Undefined
}
```

**API change:**
```csharp
public sealed record FireResult(
    TransitionOutcome Outcome,
    string? PreviousState,
    string EventName,
    string? NewState,
    IReadOnlyList<ConstraintViolation> Violations,    // error violations (empty on success)
    IReadOnlyList<ConstraintViolation> Warnings,      // NEW: warning violations (may be populated on success)
    PreceptInstance? UpdatedInstance)
{
    public bool IsSuccess => Outcome is TransitionOutcome.Transition
                          or TransitionOutcome.NoTransition
                          or TransitionOutcome.TransitionWithWarnings
                          or TransitionOutcome.NoTransitionWithWarnings;
}
```

**Engine change (Stage 6 rewrite):**
```
1. Collect ALL constraint violations (unchanged evaluation)
2. Partition: errors = violations where rule severity == Error
              warnings = violations where rule severity == Warning
3. If errors.Count > 0 → return ConstraintFailure (unchanged behavior)
4. If warnings.Count > 0 → return TransitionWithWarnings / NoTransitionWithWarnings
     (commit the mutation, attach warnings)
5. If neither → return Transition / NoTransition (unchanged behavior)
```

**Error precedence rule:** If a transition violates error rule E1 AND warning rule W1, the result is `ConstraintFailure` with E1 in `Violations`. W1 is not reported — the operation didn't proceed far enough to commit, so warning evaluation is moot. This is identical to how ESLint exits with code 1 when errors exist regardless of warnings, and how Kubernetes `Deny` supersedes `Warn`.

**Evaluation:**
- (+) **Directly answers Shane's question.** Warning rules don't block. Error rules block.
- (+) The engine makes the enforcement decision, not the consumer. The `.precept` file is still the complete contract.
- (+) Error precedence is clean and intuitive: errors are blocking, warnings are advisory.
- (+) `IsSuccess` correctly returns `true` for `TransitionWithWarnings` — the operation committed.
- (+) Warnings are structurally reported — callers get explicit `Warnings` on the result and a distinct outcome enum value. No silent swallowing.
- (+) Backwards compatible for consumers who only check `IsSuccess`: warning-only results are still success.
- (−) **Requires philosophy reframing** (see section 3).
- (−) Two new `TransitionOutcome` values. Touches every consumer that switches on `TransitionOutcome`.
- (−) `UpdatedInstance` is non-null even though warning constraints are violated. The entity's data does NOT satisfy all declared rules. This is the core tension.

**Philosophy verdict:** Requires reframing. See section 3.

---

## 3. Philosophy Analysis

### The Core Tension

`docs/philosophy.md` states:

> **Prevention, not detection.** Invalid entity configurations — combinations of lifecycle position and field values that violate declared rules — cannot exist. They are structurally prevented before any change is committed, not caught after the fact.

Under Model D, an entity can exist in a configuration where warning-level rules evaluate to false. The engine committed the mutation anyway. By the current philosophy's definition, this is an "invalid configuration" that was allowed to exist.

### Proposed Reframing

The reframing does not weaken the guarantee — it makes it more precise.

**Current:** "All constraint violations are prevented."
**Proposed:** "Error-level constraints are PREVENTED. Warning-level constraints are DETECTED and reported."

The `.precept` file remains the single, complete specification. It now specifies TWO categories of rules:
- **Error rules** (default) — boundaries that cannot be crossed. Prevention guarantee. Invalid configurations involving error-rule violations are structurally impossible.
- **Warning rules** (opt-in) — advisory rules that flag concerns. Detection guarantee. The engine evaluates them on every operation, reports violations, and returns a distinct outcome — but does not block.

**Why this strengthens the contract, not weakens it:**

1. **One-file completeness is improved.** Today, advisory business rules that shouldn't block operations live OUTSIDE the `.precept` file — in service-layer code, logging middleware, or separate validation passes. With warning-level rules, those advisory rules move INTO the `.precept` file. The file becomes more complete, not less.

2. **The engine still evaluates every rule on every operation.** Warning rules aren't skipped — they are evaluated and reported. The evaluation guarantee is unchanged. What changes is the consequence: error rules prevent mutation; warning rules flag it.

3. **The author explicitly declares the severity boundary.** The default is `error` — every rule blocks unless the author deliberately opts it into `warning`. The `.precept` file doesn't just declare WHAT rules exist — it declares WHICH are critical vs advisory. This is strictly more information than today's "all rules are equal."

4. **Determinism is preserved.** Same definition, same data → same outcome, same warnings. Nothing is hidden.

5. **Inspectability is preserved.** MCP `inspect` reports warning constraints alongside error constraints. The preview panel shows warning violations. Every warning is surfaced, never swallowed.

### Precedent for the Reframing

- **BPMN:** Error events terminate the scope. Escalation events continue. Both are declared in the process definition. Both are evaluated by the engine. The consequence differs.
- **Kubernetes:** `Deny` blocks the request. `Warn` allows it with a warning header. Both are evaluated by the same CEL expression. The consequence differs based on the binding.
- **ESLint:** `error` exits with code 1. `warn` exits with code 0. Both evaluate the same rule. The consequence differs based on configuration.

In all three, the system evaluates every rule — it just differentiates the consequence. Precept would do the same.

### What the Philosophy Section Would Say

> **Prevention and detection.** Precept constraints divide into two enforcement tiers. **Error-level rules** (the default) are structurally prevented — invalid configurations involving error-rule violations cannot exist. **Warning-level rules** (author opt-in) are structurally detected — the engine evaluates them on every operation, reports violations in the result, and surfaces them through inspection. The author declares the boundary between what must be blocked and what must be flagged. The `.precept` file specifies both.

**Note:** This is a PROPOSED reframing for analysis purposes. Per copilot-instructions.md, `docs/philosophy.md` is not edited without explicit owner approval. This analysis surfaces the gap and the proposed resolution — it does not resolve it.

---

## 4. Enforceability Assessment (Model D)

### 4a. Type Checker Enforcement

The type checker (`PreceptTypeChecker.cs`) already validates every constraint expression at compile time. Adding severity to rule declarations creates new static analysis opportunities:

**Default to error.** Every `invariant`, `assert`, and `rule` is severity `error` unless explicitly annotated `warning`. This is the same ergonomic pattern as `nullable` — the restrictive form is the default, the permissive form requires opt-in. The type checker enforces: if no severity annotation, assume error.

**Proposed new diagnostics (C60–C62 range, using next available after C59):**

| Code | Trigger | Severity | Rationale |
|------|---------|----------|-----------|
| **C60** | Warning constraint that can never trigger — expression is always `true` at compile time | Warning | Dead warning rule, probably a mistake |
| **C61** | Same expression used as both `error` and `warning` on overlapping scope | Error | Contradictory: same rule can't be both blocking and non-blocking |
| **C62** | Warning-only event — ALL constraints reachable from this event are `warning` severity | Warning | Informational: this event can never produce `ConstraintFailure`, only `TransitionWithWarnings` |

C61 is the strongest new diagnostic. If an invariant says `Balance >= 0 because "..." error` and another says `Balance >= 0 because "..." warning`, the author has declared a contradiction — the engine can't both block and allow the same violation.

### 4b. Engine Partitioning

The engine change is clean because constraint evaluation is already isolated. The proposed change to `CollectConstraintViolations`:

```
Current:
  violations = EvaluateInvariants(data) + EvaluateStateAssertions(anchor, state, data)
  if violations.Count > 0 → ConstraintFailure

Proposed:
  allViolations = EvaluateInvariants(data) + EvaluateStateAssertions(anchor, state, data)
  errors = allViolations.Where(v => v.Severity == Error)
  warnings = allViolations.Where(v => v.Severity == Warning)
  if errors.Count > 0 → ConstraintFailure (with errors only)
  if warnings.Count > 0 → success with warnings
  else → clean success
```

This requires `ConstraintViolation` to carry severity — which means the underlying `PreceptInvariant` and `StateAssertion` model records need a `Severity` field propagated from parsing.

**Implementation path:**
1. Parser: recognize `warning` keyword after `because "reason"` on `invariant` and `assert` lines. Default to `error`.
2. Model: add `Severity` field to `PreceptInvariant`, `StateAssertion`, `EventAssertion`.
3. TypeChecker: propagate severity; emit C60/C61/C62.
4. Engine: partition violations by severity in `CollectConstraintViolations` and the no-transition constraint check.
5. Results: add `Warnings` to `FireResult`, `UpdateResult`, `EventInspectionResult`.
6. Outcome enum: add `TransitionWithWarnings`, `NoTransitionWithWarnings`.

### 4c. Error Precedence

The error precedence rule is strict and unambiguous:

**If ANY error-level rule fails, the ENTIRE operation blocks.** Warning-level violations are not reported on a blocked operation — they are irrelevant because the mutation didn't commit. This prevents confusion: the caller never sees "warnings on a failed operation."

The evaluation itself runs ALL constraints (both error and warning) in a single pass — collect-all semantics are preserved. Partitioning happens after evaluation, not during. This means warning rules are always evaluated, even when errors exist — the diagnostic value of the warning evaluation is available to tooling even if not returned in the runtime result.

**For Inspect:** The inspection API reports BOTH error and warning violations because it's non-mutating — there's no "committed vs not committed" distinction. The caller sees the full picture.

### 4d. Update Pipeline

The `Update` method follows the same pattern. Stage 4 (rules evaluation) would partition identically:

```
Current:
  violations = EvaluateInvariants(updatedData) + EvaluateStateAssertions(...)
  if violations.Count > 0 → ConstraintFailure

Proposed:
  allViolations = EvaluateInvariants(updatedData) + EvaluateStateAssertions(...)
  errors = allViolations.Where(v => v.Severity == Error)
  warnings = allViolations.Where(v => v.Severity == Warning)
  if errors > 0 → ConstraintFailure
  if warnings > 0 → success with warnings (new UpdateOutcome: UpdateWithWarnings)
  else → clean success
```

`UpdateOutcome` would gain `UpdateWithWarnings`.

### 4e. Compatibility Check

`CheckCompatibility` currently evaluates invariants and `in` state asserts to verify a deserialized instance is valid. Under Model D, the compatibility check must also partition:

- Error-level violations → `IsCompatible = false` (unchanged behavior for error rules).
- Warning-level violations → `IsCompatible = true` but with a new `Warnings` list on the result.

This is important: an instance that was created with warnings should still be loadable. The compatibility check gates on structural errors, not advisory warnings.

---

## 5. Recommendation

**Model D (Structural Non-Blocking) with mandatory philosophy reframing.**

### Answer to Shane

**YES, non-blocking warnings can be strictly enforced while maintaining governed integrity — under Model D.** The key requirements:

1. **Philosophy reframing is mandatory and must come first.** The change from "prevention" to "prevention and detection" is not cosmetic — it's a real expansion of the product's guarantee model. Shane must sign off on the philosophy update before implementation begins.

2. **Default to error is non-negotiable.** Every constraint is error-level unless explicitly annotated `warning`. This preserves the existing behavior: all current `.precept` files continue to work identically. Zero breaking changes.

3. **Error precedence is strict.** Any error-level failure blocks the entire operation. Warning evaluation is a secondary concern — errors always win.

4. **The engine, not the consumer, enforces severity.** The `.precept` file declares what's error vs warning. The engine enforces it. The consumer reads the result. No consumer-side enforcement decisions.

5. **Warnings are never swallowed.** Every warning violation is reported in the result with a distinct outcome enum value. Callers who only check `IsSuccess` see warnings as success (correct — the mutation committed). Callers who check `Outcome` see `TransitionWithWarnings` (the advisory signal is explicit).

### Implementation Complexity Assessment

| Component | Effort | Risk |
|-----------|--------|------|
| Parser (severity keyword after `because`) | Low | Low — additive, no ambiguity |
| Model records (add Severity field) | Low | Low — additive |
| TypeChecker (new diagnostics C60–C62) | Medium | Low — additive static analysis |
| Engine (partition violations) | Medium | Medium — touches the commit/rollback boundary |
| Result types (add Warnings, new outcomes) | Medium | Medium — touches all consumers |
| MCP tools (expose warnings in DTOs) | Low | Low — thin wrappers |
| Philosophy reframing | N/A | **High — product identity decision** |

The runtime change itself is tractable. The philosophy reframing is the hard part — not technically, but as a product decision.

### What This Does NOT Cover

- **Event verdicts** (`on Approve success`). That's a separate feature from Frank's Tier 1 recommendation. Rule severity and event verdict are orthogonal and can be implemented independently.
- **State verdicts** (`state Approved success`). Frank correctly identified this as novel territory with zero precedent. This analysis doesn't address it.
- **Syntax specifics.** Whether the keyword is `warning` after `because`, a modifier before `invariant`, or something else — that's a language design decision downstream of the enforceability question.

---

## Key References

- [verdict-modifiers.md](verdict-modifiers.md) — Frank's externally grounded research (Pattern 1: severity is always metadata)
- [PreceptRuntime.cs](../../../src/Precept/Dsl/PreceptRuntime.cs) — Fire pipeline, constraint evaluation
- [PreceptTypeChecker.cs](../../../src/Precept/Dsl/PreceptTypeChecker.cs) — Static analysis, expression validation
- [RuntimeApiDesign.md](../../../docs/RuntimeApiDesign.md) — TransitionOutcome enum, API contract
- [PreceptLanguageDesign.md](../../../docs/PreceptLanguageDesign.md) — 12 design principles
- [philosophy.md](../../../docs/philosophy.md) — Core guarantees ("prevention, not detection")
- [DiagnosticCatalog.cs](../../../src/Precept/Dsl/DiagnosticCatalog.cs) — Existing diagnostics C1–C59
