# Outcome Taxonomy Survey: Result Types and Typed Error Models

**Date:** 2026-04-19
**Author:** George (Runtime Dev)
**Research Angle:** Typed error model literature — Railway-Oriented Programming, Rust Result<T,E>, ADT theory, C# pattern matching
**Purpose:** Evaluate whether Precept's 8-outcome taxonomy is complete and formally grounded, with specific focus on the Rejected/ConstraintFailure and Undefined/Unmatched distinctions.

---

## Executive Summary

Precept's outcome model is **structurally sound** and formally well-grounded as an ADT. Every observable outcome of every operation maps to exactly one variant. The two key distinctions (`Rejected` vs. `ConstraintFailure`, `Undefined` vs. `Unmatched`) are **principled**, each cleanly separating causes that require different diagnostic responses. A functional type theorist would recognize the model as a typed sum type over operation results — richer than a binary `Result<T,E>` but still correct by the same reasoning.

One **semantic overloading problem** exists and warrants attention: `TransitionOutcome.Rejected` is used in Fire for both authored domain prohibitions (explicit `reject` rows) and argument validation failures (stage 2: unknown keys, wrong argument types; stage 3: event ensure violations). These are causally distinct — the former is a definition-authored prohibition, the latter is a caller-provided invalid input — yet they produce the same outcome value. The equivalent for `Update` (`UpdateOutcome.InvalidInput`) is correctly separated. The inconsistency means Fire callers cannot distinguish "you called incorrectly" from "this action is designed to be prohibited."

A secondary overloading affects `Undefined`: it covers both routing gaps (no rows defined for this event in this state) and engine-compatibility failures (instance produced by an incompatible engine version). These are structurally unrelated.

Neither overloading introduces a missing outcome — the full outcome space is covered — but both reduce diagnostic fidelity in exactly the way the taxonomy's own design philosophy argues against.

---

## Survey Results

### 1. Railway-Oriented Programming (Scott Wlaschin, F# for Fun and Profit)

**Source:** https://fsharpforfunandprofit.com/rop/ (TLS cert error — synthesized from knowledge of canonical source)

Railway-Oriented Programming (ROP) is Scott Wlaschin's widely cited articulation of typed error flow, grounded in F# discriminated unions. The core metaphor: every function is a "two-track" railroad. The happy path is the success track; any failure switches to the failure track. Once on the failure track, downstream functions are bypassed — the error propagates without executing intermediate steps.

Key claims relevant to Precept:

- **Errors should be typed, not stringly typed.** Each failure mode should be a named union variant so callers can exhaustively match all cases. A bare `bool IsError` or `string ErrorMessage` is not enough.
- **Multiple error variants are expected and encouraged.** The `Err` side of the ROP railway is typically a discriminated union with as many variants as there are distinguishable failure modes. Collapsing two causally distinct failures into one variant is a diagnostic loss.
- **The "switch function" composition model:** functions of type `Input → TwoTrack<Output, Error>` compose cleanly when both the input type and the error type match. This makes the case for operation-specific result types — `FireResult` and `UpdateResult` do not need to unify because they are not composed.
- **Verdict for Precept:** The `Rejected` / `ConstraintFailure` distinction is exactly what ROP advocates. Using separate named variants for "designed prohibition" vs. "constraint violation" is idiomatic ROP. The one problem ROP would flag: `Rejected` carrying two semantically different causes (designed prohibition AND invalid argument) is a one-variant conflation ROP explicitly discourages.

---

### 2. Rust `Result<T, E>` (doc.rust-lang.org/std/result)

**Source:** https://doc.rust-lang.org/std/result/ *(fetched)*

Rust's `Result<T, E>` is a binary enum: `Ok(T)` on success, `Err(E)` on failure. The critical design choice is that `E` itself is a full type — typically an enum with multiple variants. The `#[must_use]` attribute means the compiler warns if a `Result` is ignored, enforcing explicit handling.

Key claims relevant to Precept:

- **`Result<T, E>` is binary at the top level, but `E` is a sum type.** Rust idiom for a function that can fail in multiple ways is `Result<T, MyError>` where `MyError` is an enum: `enum MyError { InvalidInput(String), PermissionDenied, NotFound }`. The granularity of error diagnosis lives in the error type, not in separate Result variants.
- **Exhaustive match is enforced by the compiler.** A `match` on a `Result` that doesn't handle all `Err` variants produces a compile warning/error. This is the same guarantee that C#'s switch expression provides for enums.
- **Recoverable vs. unrecoverable errors are structurally separated.** Rust uses `Result` for errors a caller can reasonably handle; `panic!` for logic errors that indicate a broken invariant. Precept's `Undefined` (no routing defined) is closer to the "logic error" category — it signals a misuse of the API rather than a predictable operational failure.

Mapping Precept to Rust idiom:
```rust
enum FireError {
    Undefined,
    Unmatched,
    Rejected,      // currently overloaded: authored prohibition + invalid args
    ConstraintFailure(Vec<ConstraintViolation>),
}
type FireResult = Result<PreceptInstance, FireError>;
```
This mapping is clean for all variants except `Rejected`. A strict Rust idiom would split `Rejected` into `Prohibited` (explicit reject row) and `InvalidArguments(String)` (bad caller input).

---

### 3. Rust Error Handling Philosophy (doc.rust-lang.org/book/ch09-00-error-handling.html)

**Source:** https://doc.rust-lang.org/book/ch09-00-error-handling.html *(fetched)*

The Rust Book's error handling chapter establishes a categorical distinction: **recoverable errors** (file not found, bad input) use `Result<T, E>`; **unrecoverable errors** (index out of bounds, invariant violation) use `panic!`. This is not just a stylistic choice — the two categories imply fundamentally different caller behavior.

Relevant to Precept's `Undefined`:

- `Undefined` returned from `Fire` when no transition rows exist for `(currentState, eventName)` is closer to an unrecoverable / programming error than to an operational failure. The entity is valid, the state is valid, but the caller asked for an event that does not exist in that state. In Rust's categorization, this is a usage error — something a developer should have prevented by checking the definition first (which `Inspect` enables).
- `Undefined` returned from `CheckCompatibility` failure is also a programming-class error — the caller has presented an instance to an incompatible engine.
- Both are currently folded into the same outcome value. The Rust-aligned design would surface compatibility failure through a distinct mechanism — a precondition assertion or a separate error type.

Implication: Precept's treatment of `Undefined` as a first-class `TransitionOutcome` alongside operational failures (`ConstraintFailure`, `Rejected`) is defensible for ergonomic reasons (callers handle a single result type), but is philosophically a conflation of two error classes.

---

### 4. Elm Error Handling

**Source:** https://elm-lang.org/docs/error-handling *(404 — synthesized from knowledge of Elm's type system)*

Elm guarantees no runtime exceptions through exhaustive union types. Every function returning a value of type `Result err ok` forces the caller to handle both the `Ok` and `Err` branches. The `err` type is a full union type, enabling multiple named failure modes. Elm also provides `Maybe` (Option) as a specialized form for the `null / Some` case.

Key claims relevant to Precept:

- **Every failure mode is a named constructor.** Elm would express Precept's outcome as a custom union: `type Outcome = Transitioned PreceptInstance | NotTransitioned PreceptInstance | Rejected String | ConstraintFailure (List Violation) | Unmatched | Undefined`. Each constructor carries exactly the data relevant to that failure, and no more.
- **No implicit conflation.** Elm's design philosophy considers it a bug to use the same constructor for two structurally different situations. Using `Rejected` for both authored prohibitions and argument errors would be flagged by idiomatic Elm design review.
- **The exhaustiveness enforcement at the call site** is the key value: callers writing `case outcome of` must handle every constructor. This is the same guarantee Precept achieves through C# switch expressions on `TransitionOutcome`/`UpdateOutcome` enums.

---

### 5. C# Pattern Matching (learn.microsoft.com)

**Source:** https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching *(fetched)*

C# switch expressions from C# 8+ provide the same exhaustiveness checking that Haskell/Rust/Elm provide for ADTs. A `switch` expression on a `TransitionOutcome` enum that omits any case generates a compiler warning (`CS8509`). This makes `TransitionOutcome` and `UpdateOutcome` behave as discriminated unions for C# consumers.

Key claims relevant to Precept:

- **Enum + switch expression is the C# idiom for sum types.** C# does not have sealed union types in the ML/Haskell sense, but the enum + switch pattern achieves the same exhaustiveness contract for well-behaved callers.
- **The compiler's discard arm (`_`) is a trap.** Adding `_ => throw new InvalidOperationException(...)` silences exhaustiveness warnings and hides future missing cases. Callers who use this pattern lose the compile-time guarantee that every outcome is handled.
- **Result records (`FireResult`, `UpdateResult`) with `IsSuccess` shortcut properties follow a well-established C# pattern.** The `IsSuccess` computed property is equivalent to Rust's `is_ok()` / `is_err()` convenience methods.
- **Sealed classes as discriminated unions:** C# 9+ sealed records with abstract base classes (the `OneOf` pattern) provide stronger exhaustiveness than enums because adding a new subtype forces all `switch` expressions to add a new arm. Precept uses enums, which are weaker — new values can be added without immediately breaking callers. This is a known tradeoff in C# ADT design.

---

### 6. Algebraic Data Types (Wikipedia)

**Source:** https://en.wikipedia.org/wiki/Algebraic_data_type *(fetched)*

A sum type (tagged union / disjoint union) is a type whose values can be exactly one of several named variants. Each variant can carry typed data. The set of all possible values of a sum type is the disjoint union of the value sets of its variants. Pattern matching on a sum type is exhaustive-checked by the compiler — all variants must be handled.

Key claims relevant to Precept:

- **Precept's outcome model is a sum type.** `TransitionOutcome` and `UpdateOutcome` are C# enums that represent tagged unions over the outcome space.
- **Product types carry per-variant data.** `FireResult` and `UpdateResult` are product types wrapping the outcome enum alongside additional payload (violations, instance, state names). This is the standard ADT design — the sum type identifies the variant; the product type carries the variant's payload.
- **Exhaustiveness checking is the central value proposition.** ADTs prevent the "forgot to handle this case" class of bugs. C# enums approximate this through compiler warnings on non-exhaustive switch expressions.
- **Constructor specificity.** In well-designed ADTs, each constructor corresponds to exactly one semantically distinct situation. Using one constructor for two situations (e.g., `Rejected` for authored prohibition and for invalid arguments) is a constructor overloading smell — the same `match` arm must handle two causally different cases with one piece of code, typically resorting to inspecting `Violations` or message strings to distinguish them.

---

## Synthesis: Classifying Precept's Outcome Model

Precept's outcome model occupies a specific, identifiable position in the typed error model literature:

**It is a domain-semantic sum type, not a binary result type.** It is neither a bare `Result<T,E>` (which has exactly two constructors) nor a stringly-typed error (which collapses all failure information into a message string). It is richer than both: a sum type with 6 variants on the Fire/Inspect path and 4 variants on the Update path, where each variant has a distinct causal interpretation and requires a distinct diagnostic response.

**It is not a Railway-Oriented Programming railway in the narrow sense,** because ROP assumes all functions compose into a single success/failure pipeline. Precept's outcomes are not composed — they are terminal results returned from atomic operations. The ROP *principle* applies, however: each failure variant should be causally distinct, and callers should be able to handle each variant without inspecting string messages.

**It is equivalent to `Result<PreceptInstance, FireError>` where `FireError` is a 4-variant enum** — `Undefined`, `Unmatched`, `Rejected`, `ConstraintFailure` — with the `Violations` list attached to `ConstraintFailure`. The two success variants (`Transition`, `NoTransition`) are equivalent to two differently-tagged `Ok` variants carrying the same payload, or alternatively a `Result<TransitionKind * PreceptInstance, FireError>` where `TransitionKind = Transition | NoTransition`.

**The two-enum split** (`TransitionOutcome` for Fire/Inspect, `UpdateOutcome` for Update) is correct and idiomatic. Fire and Update are not the same operation; they do not need a unified result type. The split mirrors Rust's practice of defining operation-specific error enums rather than a global error type.

**Outcome completeness score: high.** Every observable outcome of every operation has a named, typed variant. There is no "other" bucket, no boolean fallback, no stringly-typed catch-all.

---

## Verdict on the Rejected/ConstraintFailure Distinction

**The distinction is formally justified and principled.** This is the correct separation from every framework surveyed.

The two variants differ in *causal locus*:

| | `Rejected` | `ConstraintFailure` |
|---|---|---|
| **Where the failure is authored** | In the precept definition (`reject` row) | In the precept definition (rule, ensure) |
| **When it fires** | A row with `reject` outcome matched | After mutations are applied, constraints fail |
| **What it means for the entity** | The action is structurally prohibited — by design, regardless of data values | The action is structurally permitted but the data result would violate a rule |
| **Caller response** | Surface why the action is not available in this state | Surface which fields/values need to change |
| **Analogous to** | HTTP 403 Forbidden | HTTP 422 Unprocessable Entity |

A functional type theorist would express these as two different constructors of the error enum:
```haskell
data FireError
  = Prohibited  -- authored reject row: this action is by design not permitted
  | DataInvalid [ConstraintViolation]  -- mutations would violate rules
  | GuardFailed -- all guards failed / unmatched
  | NoRouting   -- no rows defined
```

The distinction is not merely semantic pedantry. A UI built on Precept needs to know: "was this action blocked because it is *never allowed* in this context, or because *the current data doesn't satisfy* the conditions for it to succeed?" The two cases require different messages and different remediation paths. Collapsing them would force callers to inspect the `Violations` list to distinguish them, which is string-inspection fallback — the thing ADTs exist to prevent.

**One caveat:** As documented in the RuntimeApiDesign.md Fire stages, `Rejected` is also returned for argument validation failures (stage 2: unknown keys, wrong argument types) and event ensure violations (stage 3). These are caller-side errors, not authored prohibitions. The causal locus is different from a `reject` row. The current EngineDesign.md description of `Rejected` ("Explicit `reject` row — designed prohibition") does not cover these cases. This is the one place where the `Rejected/ConstraintFailure` distinction — otherwise excellent — is undercut by `Rejected` being used for a third, caller-error category alongside its two domain-error uses.

**Verdict:** The distinction is well-grounded and should be preserved. The documentation for `Rejected` should be expanded to acknowledge the argument validation and event ensure cases, or those cases should be separated into a distinct outcome (e.g., `InvalidArguments`, mirroring `UpdateOutcome.InvalidInput`).

---

## Verdict on the Undefined/Unmatched Distinction

**The distinction is formally justified and arguably more important than the Rejected/ConstraintFailure distinction.**

The two variants differ in *what kind of problem they diagnose*:

| | `Undefined` | `Unmatched` |
|---|---|---|
| **What exists in the definition** | No rows for this event in this state | Rows exist; all guards failed |
| **Root cause** | Developer/caller error — this event doesn't apply here | Data condition — entity not in right shape for this event |
| **Fix lives in** | The precept definition (or the caller's event selection) | The entity's field values |
| **Analogous to** | HTTP 404 Not Found | HTTP 412 Precondition Failed |

This is precisely the distinction Rust's Book makes between unrecoverable errors (program logic errors) and recoverable errors (predictable operational failures). `Undefined` is a programming-class error: the developer called `Fire` with an event name that doesn't apply in the current state. `Unmatched` is an operational failure: the event is defined and designed to be applicable, but the entity's current data doesn't satisfy the guard conditions.

The practical importance:
- An `Undefined` result in production likely means either a stale client (definition was updated, client is using old event names) or a developer bug (wrong event for this state).
- An `Unmatched` result in production means the user needs to change some data before this action becomes available — a normal operational condition.

Folding these into a single outcome (e.g., `NotApplicable`) would require callers to inspect secondary data to distinguish them. Keeping them distinct means callers can immediately branch on the outcome value.

**One caveat:** `Undefined` is currently overloaded to also cover engine-compatibility failures (`CheckCompatibility` returns `Undefined` when the instance was produced by an incompatible engine version). This is a genuine semantic overloading: the instance's existence is not "undefined" in the same sense as a routing gap. An instance-compatibility failure is a different class of error — it concerns the relationship between the instance and the engine, not the relationship between the event and the state. Ideally this would produce a distinct outcome or be handled at the `CheckCompatibility` call boundary before any operation result is produced.

**Verdict:** The distinction is formally well-grounded and should be preserved. The `Undefined` documentation should clarify its dual use (routing gap vs. compatibility failure), and future API evolution should consider separating compatibility failures from routing gaps.

---

## Missing Outcomes?

The taxonomy is **functionally complete**: every observable runtime path terminates in exactly one named outcome. There are no "other" buckets. This is the central requirement for a typed outcome model, and Precept satisfies it.

However, three outcome refinements are worth recording as potential future evolution:

### 1. `InvalidArguments` for Fire (or `InvalidInput` parity)

`UpdateOutcome.InvalidInput` correctly separates caller errors (type mismatch, unknown field, patch conflict, empty patch) from domain errors (`ConstraintFailure`, `UneditableField`) in the Update path. The Fire path has no equivalent — it folds argument validation failures and event ensure violations into `Rejected`. This asymmetry is the one genuine gap relative to the literature. A `TransitionOutcome.InvalidArguments` (or renaming the overloaded subset of `Rejected` cases) would give Fire callers the same diagnostic fidelity that Update callers already have.

**Priority:** Medium. This becomes more important as the MCP tool surface grows, where callers passing malformed arguments to `Fire` need to distinguish "you passed bad arguments" from "this action is prohibited by the definition."

### 2. `Incompatible` for engine-compatibility failures

`CheckCompatibility` currently injects compatibility failures as `Undefined` outcomes from `Fire` and `Inspect`. A dedicated `TransitionOutcome.Incompatible` (or `Undefined` subvariants) would make the distinction explicit. In practice, compatibility failures should be caught and handled at API entry, not discovered through outcome matching — so this is low priority.

**Priority:** Low. The current behavior is safe and the documentation acknowledges it.

### 3. Soft constraint / warning outcomes

Some domain integrity systems distinguish between hard constraints (must pass to commit) and soft constraints (advisory, allow override). Precept has no `Warning` or `ConstraintWarning` outcome — all constraints are hard. This is not a gap but a deliberate design position: Precept's philosophy is prevention, not advisory. Introducing a soft-constraint outcome would require a separate `commit with override` operation and would complicate the clean binary `IsSuccess` contract.

**Priority:** Not recommended. Inconsistent with Precept's prevention-first philosophy.

---

## Implications for RuntimeApiDesign.md and EngineDesign.md

### RuntimeApiDesign.md

1. **Fire stage 2 and 3 produce `Rejected` for caller errors**, not just authored prohibitions. The `TransitionOutcome.Rejected` enum comment reads `// Explicit reject outcome in transition row` but stage 2 (argument validation) and stage 3 (event ensures) also return `Rejected`. The comment should be updated to acknowledge both uses, or the design should be evolved to produce a distinct outcome for caller errors.

2. **`UpdateOutcome.InvalidInput` exists but `TransitionOutcome` has no equivalent.** Document this asymmetry explicitly. If the design intent is that Fire argument errors are folded into `Rejected`, say so explicitly so callers know they must inspect message content to distinguish `Rejected-authored` from `Rejected-badargs`.

### EngineDesign.md

1. **§Outcome Taxonomy `Rejected` description** reads "Explicit `reject` row — designed prohibition." This is accurate for the semantic intent but incomplete given the Fire pipeline's actual behavior. The full description should cover all three causal origins of `Rejected` in Fire: authored `reject` rows, argument validation failures (stage 2), and event ensure violations (stage 3).

2. **`Undefined` dual use** (routing gap vs. compatibility failure) should be noted in §Outcome Taxonomy as a documented design fact, consistent with how §Documented Assumptions flags other behavioral details.

3. **Philosophy footnote 5** ("The 6-value taxonomy is the minimum resolution that preserves diagnostic fidelity") is accurate for the six core outcomes but understates the resolution available through the `Violations` list, which is already used to distinguish within-category failures. The footnote could be updated to: "The 6-value taxonomy on the Fire/Inspect path is the minimum resolution for *cause-level* diagnostic fidelity; within-variant diagnostic detail is carried in the `Violations` list."

---

## References

1. **Wlaschin, Scott. "Railway-Oriented Programming."** https://fsharpforfunandprofit.com/rop/ (TLS cert error; synthesized from canonical published content). The primary articulation of typed error flow as a two-track railway. Central claim: each error variant should correspond to one distinguishable cause.

2. **Rust Reference Team. "Module std::result."** https://doc.rust-lang.org/std/result/ (fetched 2026-04-19). Defines `Result<T, E>` with `#[must_use]` enforcement. Demonstrates that error granularity belongs in the `E` type, not in the top-level `Result` constructor.

3. **Rust Reference Team. "Error Handling."** https://doc.rust-lang.org/book/ch09-00-error-handling.html (fetched 2026-04-19). Establishes the recoverable (`Result`) vs. unrecoverable (`panic!`) dichotomy. Relevant to evaluating whether `Undefined` belongs in the operational result type.

4. **Elm Language. "Error Handling."** https://elm-lang.org/docs/error-handling (404 at fetch time; synthesized from knowledge of Elm's `Result err ok` and `Maybe` types). Elm's "no runtime exceptions" guarantee via exhaustive union types is the strongest version of the exhaustiveness argument.

5. **Microsoft. "Pattern Matching Overview."** https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching (fetched 2026-04-19). Documents C# switch expression exhaustiveness checking on enums. Confirms that `TransitionOutcome` and `UpdateOutcome` achieve the ADT exhaustiveness contract in the .NET ecosystem.

6. **Wikipedia. "Algebraic Data Type."** https://en.wikipedia.org/wiki/Algebraic_data_type (fetched 2026-04-19). Canonical theoretical background: sum types as disjoint unions, pattern matching, exhaustiveness checking, constructor specificity norm.

7. **Precept. "Engine Design."** `docs/EngineDesign.md` §Outcome Taxonomy and §Philosophy-Rooted Design Principles. Canonical source for the 8-outcome taxonomy, the `Rejected/ConstraintFailure` and `Undefined/Unmatched` distinction rationale, and the "6-value minimum resolution" claim.

8. **Precept. "Architecture Design."** `docs/ArchitectureDesign.md` §Outcomes table. Canonical cross-reference for outcome-to-operation mapping.

9. **Precept. "Runtime API Design."** `docs/RuntimeApiDesign.md` §Result Types, `TransitionOutcome`, `UpdateOutcome`, §Fire evaluation stages. Source for the argument validation / `Rejected` overloading finding.
