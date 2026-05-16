# Constructor Semantics — Consolidated Design

> **Status:** Revised draft — reject mutual-exclusion locked; remaining open questions noted inline  
> **Author:** Frank (Lead/Architect)  
> **Date:** 2026-05-15  
> **Last revision:** 2026-05-15 — OQ8 locked: construction renders via entry pseudo-node arrow  
> **Source analyses:** Terminal constructor constraint, `on <Event>` syntax unification, guard restriction removal  
> **Precedent research:** [`docs/working/research-conditional-construction.md`](research-conditional-construction.md)

---

## 1. The Problem

Four issues compound into a single semantic defect in the current construction model:

1. **Keyword overload.** `initial` on a state means "graph position where entities begin." `initial` on an event means "construction mechanism." These are orthogonal — one names a topology node, the other names an operation category. When the same keyword means two unrelated things, one meaning must be wrong or both must be vague.

2. **False uniformity via `from <InitialState> on <InitialEvent>`.** Construction rows currently use the same `from State on Event` syntax as post-construction transition rows. This teaches a lie: construction is NOT a state-dispatched transition. The entity does not yet exist in a state when the initial event fires — the state is SET as part of construction, not dispatched FROM.

3. **Construction-time transitions.** The spec currently allows `transition <OtherState>` in an initial event's action chain, producing `EventOutcome.Transitioned`. This makes `initial` on the state meaningless — if construction can route elsewhere, "initial" doesn't mean "where entities begin." It means "where the dispatch table starts and then immediately leaves."

4. **`EventRowDoesNotSupportGuard` as an artifact restriction.** The parser rejects `when` guards on `on Event -> action` forms (PRE0014). This prevents guarded construction rows and state-agnostic handler guards. The restriction has no semantic justification — it was a parser simplification that became a language limitation.

---

## 2. Design Decisions

### Decision 1: Structural Exclusion of Transition from Construction

**What:** Construction rows use asymmetric grammar naming: `EventRow` for the unmarked success path and `EventRowReject` for the explicitly-marked reject path. `EventRow` carries the work path (`on Event [when Guard] -> actions`). `EventRowReject` carries the refusal path (`on Event [when Guard] -> reject "reason"`). Neither construct includes the generic `Outcome` slot, so `transition` and `no transition` are structurally impossible in construction. The initial event cannot be fired on an existing entity (fire-once enforcement).

**Why:** When construction is structurally terminal, `initial` on both state and event means "origin." The state is where existence begins; the event is the act of beginning existence. D94 simplifies to a single-target check. The construction guarantee becomes: the entity is in the initial state, with all required fields assigned, constraints satisfied. Structural exclusion is superior to type-checker rejection: you cannot write what the grammar does not express. The same principle now applies to every reject-bearing surface, not just construction.

**Enforcement mechanism:** Precept uses one language-wide rule for reject-bearing forms: the success path keeps the base construct name, while the reject path gets an explicit reject variant. For construction this means `EventRow` uses `[SlotEventTarget, SlotPreVerbGuardArrow, SlotActionChain]`, while `EventRowReject` uses `[SlotEventTarget, SlotPreVerbGuardArrow, SlotRejectClause]`. The guard slot uses `SlotPreVerbGuardArrow` (terminates only at Arrow) rather than the generic `SlotGuardClause` (which also terminates at `Because` — inapplicable here).

**Alternatives rejected:**
- *Allow construction-time transitions (status quo):* Technically sound but semantically confusing. Empirically unused (0/30 samples). Makes `initial` state meaningless as a lifecycle anchor.
- *Rename `initial` to `constructor` on events:* Adds a new keyword without resolving the transition ambiguity. Structural exclusion is the actual fix; rename is cosmetic.
- *Single `EventRow` construct + type-checker mutual-exclusion diagnostic:* Makes the invalid mixed form writable and then rejects it downstream. Violates Precept's structural-prevention philosophy.

**Tradeoff accepted:** Reject-bearing surfaces pay a construct-count cost (`mutation` + `reject` variants), but the invalid hybrid row is unwritable. That is the correct trade.

---

### Decision 2: `on <Event>` Syntax for Construction Rows

**What:** Construction rows in both stateful and stateless precepts use `on <EventName>` (not `from <InitialState> on <EventName>`). The `initial` modifier on the event declaration is the semantic anchor that identifies the row as construction. No `from` prefix is needed or permitted on construction rows.

**Why:** `from State on Event` means "when the entity IS IN this state and this event fires." At construction time, the entity is not yet in any state — it is being brought into existence. The `from` prefix was misinformation. Removing it makes the syntax honest: `on Create -> ...` means "when the act of creation happens, do this."

**Alternatives rejected:**
- *Keep `from <InitialState> on <InitialEvent>` (status quo):* False uniformity — dresses unconditional genesis as state-dispatched transition.
- *Use `construct on <Event>` or a new keyword:* Unnecessary new syntax. `on Event` is already the event-row form. The `initial` modifier on the event declaration is sufficient disambiguation.

**Tradeoff accepted:** Construction rows visually differ from transition rows. This is a feature — they ARE different. Authors see immediately that construction is not "just another transition."

---

### Decision 3: Guard Restriction Removal

**What:** Remove `EventRowDoesNotSupportGuard` (PRE0014). All `on <Event>` forms support `when` guards. This applies to construction rows (guarded construction paths) and state-agnostic handlers in stateful precepts.

**Why:** `on Event when condition -> actions` is semantically coherent. For construction: multiple guarded construction rows provide first-match routing at intake — directly analogous to Swift's `guard ... else { return nil }` inside `init?`, but declarative and exhaustive rather than imperative. For state-agnostic handlers: field-value conditions discriminate behavior without binding to a specific state. The restriction was a parser simplification with no semantic backing.

**Alternatives rejected:**
- *Keep restriction, add separate guarded-construction syntax:* Adds language surface for no gain.
- *Allow guards on construction rows only:* Arbitrary distinction between construction and state-agnostic uses of the same syntactic form.

**Tradeoff accepted:** Parser complexity increases slightly — `EventRowDeclaration` gains an optional `WhenClause`. The grammar remains unambiguous.

---

### Decision 4: `initial` Keyword Semantics (No Rename)

**What:** Keep `initial` on both states and events. Both now mean "origin." The state marks where existence begins. The event marks the act of beginning existence. The keyword rename to `constructor` is explicitly rejected.

**Why:** With structural exclusion in place, the overload resolves naturally. `initial state Draft` = "entities begin here." `initial event Create(...)` = "the act that begins an entity." Same concept, same word, coherent semantics. A rename adds vocabulary without adding clarity.

**Alternatives rejected:**
- *Rename event modifier to `constructor`:* Introduces a new keyword. With terminal construction, `initial` is no longer overloaded — it's double-duty with aligned meaning.

**Tradeoff accepted:** Authors must understand that `initial` means "origin" in both contexts. This is teachable and consistent once structural exclusion makes it true.

---

### Decision 5: Non-Initial `on <Event>` Excluded from Stateful Precepts

**Closed decision.** `on Event -> actions` is not allowed in stateful precepts for non-initial events. This is not a deferred proposal — it is a deliberate, grounded exclusion. The grammar unification in this document (guard removal + `on Event` form for construction rows) does not change this; the exclusion remains in force via the type checker.

**Rationale:** Three independent reasons, any one of which is sufficient:

1. **Redundancy.** The construct `from any on Event -> no transition` already covers the state-agnostic case in stateful precepts — it handles an event identically regardless of current state with no transition side-effect. A bare `on Event` handler would be syntactic sugar for something already expressible.

2. **Execution-order ambiguity.** Mixing `on Event` handlers with `from State on Event` transition rows in the same precept creates an ambiguous execution model: does the bare handler fire before, after, or instead of the matched transition row? No resolution rule is obvious. The type checker rejects this to prevent the ambiguity from being an authoring trap.

3. **Pseudo-lifecycle antipattern.** If bare event handlers can mutate fields without participating in state topology, state becomes advisory — the lifecycle is underdeclared and enforcement is weakened. Stateful precepts derive their guarantees from explicit state transitions; blurring that with side-channel handlers undermines the core model.

The construction row exception (`initial` events via `on <Event>`) is justified because there is no pre-existing state to `from` from — the restriction's rationale does not apply at construction time.

---

## 2A. Precedent

Cross-language survey (12+ languages) confirms: **conditional construction is the established norm.** No dissenting school exists. The "constructors shouldn't fail" position is a ghost — a misreading of C++ exception-safety advice that no modern language community holds.

| Language | Mechanism | Precedent for Precept's `reject` |
|----------|-----------|-----------------------------------|
| Swift | `init?` / `init throws` | Failable initializer with `guard ... else { return nil }` — closest syntactic analogue to guarded construction rows |
| Rust | `TryFrom` / `new() -> Result<T,E>` | Fallibility as a type-level property of construction |
| Haskell/ML | Smart constructors returning `Maybe`/`Either` | The settled functional idiom — construction IS validation |
| Go | `New*() (*T, error)` | The *only* construction idiom — always conditional |
| C++ | Throw from constructor (Core Guidelines C.42) | "If you can't establish invariants, throw" |
| DDD | Evans/Vernon: factory must refuse invalid aggregates | Domain-level mandate, mechanism-agnostic |

**Verdict:** Precept's `reject` outcome in construction rows has strong multi-language precedent. Every surveyed language either has first-class syntax for failable construction (Swift, Rust) or a universal convention for it (all others).

**Precept's innovation over all surveyed mechanisms:** The construction decision matrix is *declarative*, *exhaustive*, and *provably complete*. Swift's `init?` is imperative (you write guard logic in the body). Rust's `TryFrom` is imperative. Precept's `when` guards + `reject` outcome make the construction decision space inspectable, analyzable, and formally verifiable — which is what a domain-integrity engine should provide over a general-purpose language.

**`reject` closes the `init?` vs `init throws` gap.** Swift added `init throws` because `init?` (returning nil) gives no reason for refusal. Precept's `reject` takes a message string (`-> reject "Claims require a positive amount"`), making it equivalent to `init throws` in error-context richness while retaining the declarative structure of `init?`. The gap that split Swift's failable construction into two mechanisms does not exist in Precept.

**"Construction must always be possible" has universal precedent.** Swift requires every `init?` to have at least one success path (the compiler warns on `init?` that always returns nil). C++ Core Guidelines C.42 says throw when you can't establish invariants — but the corollary is that if you CAN'T establish them on ANY path, the type is broken. Precept's requirement that at least one construction row can produce `Created` is the structural version of this universal principle: forced exhaustiveness over construction outcomes.

---

## 3. Language Surface

### 3.1 Stateful Precept with Construction Event

**Before:**
```precept
precept LoanApplication

state Draft initial
state UnderReview

event Create(Name as string, Amount as money in 'USD') initial

from Draft on Create
    -> set ApplicantName = Create.Name
    -> set RequestedAmount = Create.Amount
    -> transition UnderReview
```

**After:**
```precept
precept LoanApplication

state Draft initial
state UnderReview

event Create(Name as string, Amount as money in 'USD') initial

on Create
    -> set ApplicantName = Create.Name
    -> set RequestedAmount = Create.Amount
```

Key changes:
- `from Draft` prefix removed — construction is not state-dispatched
- `transition UnderReview` removed — construction always terminates in initial state (Draft)

---

### 3.2 Stateful Precept with Guarded Construction Rows

**After (new capability):**
```precept
precept InsuranceClaim

state Draft initial
state UnderReview

field ClaimAmount as money in 'USD' nonnegative
field TrackingLevel as string

event FileClaim(Amount as money in 'USD', Priority as string) initial
event Escalate
event Approve

on FileClaim when FileClaim.Amount <= '1000.00 USD' and FileClaim.Priority == "low"
    -> set ClaimAmount = FileClaim.Amount
    -> set TrackingLevel = "fast"

on FileClaim when FileClaim.Amount > '1000.00 USD'
    -> set ClaimAmount = FileClaim.Amount
    -> set TrackingLevel = "standard"

on FileClaim
    -> reject "Claims require a positive amount"

from Draft on Escalate
    -> transition UnderReview
```

First-match semantics: rows are evaluated top-to-bottom, first matching guard wins. All construction paths terminate in initial state (Draft). No transitions permitted in construction rows. The `from Draft on Escalate` row demonstrates the post-construction lifecycle — once the entity exists in Draft, normal transition rows govern its progression.

---

### 3.3 Stateless Precept

**No change.** Stateless precepts with initial events already use `on Event -> actions` form:

```precept
precept FeeSchedule

field BaseFee as decimal default 0 nonnegative writable

event Initialize(Fee as decimal) initial

on Initialize
    -> set BaseFee = Initialize.Fee
```

The structural exclusion is vacuously satisfied (no states exist to transition to). Guard restriction removal enables guarded stateless construction rows identically.

---

### 3.4 Stateful Precept Without Construction Event

**No change.** Precepts without an `initial` event use parameterless `Create()`. All fields must have defaults or be optional (enforced by `RequiredFieldsNeedInitialEvent`). No construction rows exist.

```precept
precept EventRegistration

state Draft initial
state Confirmed

field RegistrantName as string optional

in Draft modify RegistrantName editable

event Register
from Draft on Register when RegistrantName is set
    -> transition Confirmed
```

---

## 4. Semantic Specification

### 4.1 Construction Context

**Definition:** The construction context is active when `Precept.Create(args)` fires the initial event. The entity does not yet exist — it is being brought into existence. The construction context differs from post-construction event firing in three ways:

1. **No source state dispatch.** The event is not dispatched from any state. Row matching uses `on <InitialEvent>` rows only.
2. **Terminal in initial state.** The action chain cannot include `transition`. The resulting entity is always in the declared initial state.
3. **Fire-once.** The initial event cannot be fired again after construction completes.

### 4.2 `on <EventName>` When Event Is `initial`

When an event declaration carries the `initial` modifier, `on <EventName>` rows are **construction rows**. Semantics:

- **Row matching:** First-match over all `on <InitialEvent>` rows in declaration order. Guards (`when`) are evaluated against: (a) event args, (b) field defaults already applied to the hollow version.
- **Row shape:** Construction rows are not a single construct with optional work/reject slots. `EventRow` carries the `ActionChain` lane and produces `Created`. `EventRowReject` carries `RejectClause` and produces `Rejected`. A single row cannot mix mutations with reject because the grammar omits the impossible slot from each variant.
- **Outcome:** `Created` (construction succeeded, entity in initial state), `Rejected` (intake refused via authored reject row, or all guards failed with no unconditional fallback), `ConstraintsFailed` (post-mutation constraint violations — collect-all semantics), `InvalidArgs` (wrong type, unknown key, or missing required argument — arg validation fails before row matching), or `Faulted` (evaluator impossible path / runtime fault during evaluation). The outcomes `Transitioned`, `Unmatched`, and `UndefinedEvent` are **structurally impossible** from `Create()` — annotated as such in the match block (§6).
- **Constraint evaluation:** After action chain executes on the working copy: arg ensures → field constraints → global rules → entry ensures (`to <InitialState>`) → residency ensures (`in <InitialState>`). Collect-all violation semantics.

### 4.3 Grammar-Level Mutual Exclusion for Reject Paths

**Locked decision:** Wherever the DSL offers a "do work OR reject" path, the grammar expresses the fork as distinct constructs. Mutual exclusion is not a type-checker rule. The impossible hybrid row is omitted from the grammar.

**Audit finding:** In the shipped language surface today, `TransitionRow` is the only construct that accepts `reject`, and it does so through `ConstructSlotKind.Outcome`. `EventRow` currently has no reject lane at all, and no other construct in `Constructs.cs` carries `Outcome` or an equivalent reject slot. The language spec's transition-row grammar (`("->" ActionStatement)* "->" Outcome`) therefore still permits `-> set X = Y -> reject "reason"` even though the samples already author rejection as separate fallback rows. This design closes that gap and applies the same rule to the planned construction-row reject surface.

**Token role:** `TokenKind.Reject` remains an after-arrow outcome keyword in `Tokens.cs` (`ValidAfter: VA_AfterArrow`, `IsMessagePosition: true`). The token does not change role; the construct routing around it does.

**Construction rows:** Keep `EventRow` as the success-path shape and add `EventRowReject` as the explicit reject-path shape. `EventRow` allows the action chain lane only. `EventRowReject` allows `-> reject StringExpr` only.

**Transition rows:** Retire the old undifferentiated `TransitionRow` shape in favor of the asymmetric pair `TransitionRow` and `TransitionRowReject`; the base name is reclaimed by the unmarked success path. `TransitionRow` carries `[SlotStateTarget, SlotEventTarget, SlotGuardClause, SlotActionChain, SlotSuccessOutcome]`, where `SlotSuccessOutcome` is narrowed to `transition <State>` or `no transition`. `TransitionRowReject` is the explicit reject-path construct; it carries `[SlotStateTarget, SlotEventTarget, SlotGuardClause, SlotRejectClause]` and omits `ActionChain` entirely.

**Parser commitment:** After the shared prefix, the parser commits based on the first post-`->` token. For `on Event [when Guard]`, `reject` selects `EventRowReject`; an action keyword or end-of-row selects `EventRow` (the unmarked success path). For `from State on Event [when Guard]`, `reject` selects `TransitionRowReject`; an action keyword, `transition`, or `no` selects `TransitionRow` (the unmarked success path). Once committed, the omitted slot does not exist. A row cannot start mutating and then reject because there is no grammar production for that hybrid.

**Semantic guarantee:** After successful construction (`EventOutcome.Created`), `version.State == precept.InitialState.Name`. Always. No exceptions.

**Implementation-gap finding (2026-05-15):** Audited `Constructs.cs`, `Outcomes.cs`, and `Parser.Expressions.cs` against this locked decision. The shipped code still has a single `ConstructKind.TransitionRow` with slot composition `[SlotStateTarget, SlotEventTarget, SlotGuardClause, SlotActionChain, SlotOutcome]`. `SlotOutcome` (`ConstructSlotKind.Outcome`) is a combined slot dispatching all three outcome forms (`transition`, `no transition`, `reject`) via a single `ParseOutcome` method. Because `SlotActionChain` is `IsRequired: false` and precedes `SlotOutcome`, the grammar structurally permits the hybrid `-> set X = Y -> reject "reason"` form. The two-construct split prescribed above has not yet landed in implementation. Zero samples use the hybrid form — the split is safe to implement without migration. This is tracked as an implementation task, not a new design decision.

### 4.4 Fire-Once Constraint

**Compile-time enforcement:** The initial event cannot appear in any `from <State> on <InitialEvent>` row. If it does, diagnostic: `InitialEventInTransitionRow`. The initial event's dispatch space is construction-only.

**Runtime enforcement:** After construction, the initial event is excluded from the entity's event space. `version.Fire(initialEventName, args)` returns `EventOutcome.UndefinedEvent`. No new mechanism needed — the event simply does not appear in the post-construction dispatch table.

### 4.5 `on <EventName>` When Event Is NOT `initial`

Not applicable in stateful precepts for non-initial events (see Decision 5 — closed exclusion). Currently, `on Event` mutation/reject rows are allowed only for the initial event in stateful precepts. The shared `on <Event>` construct family is still disambiguated semantically via the event's `initial` modifier. The restriction is deliberate — redundancy with `from any on Event -> no transition`, execution-order ambiguity, and pseudo-lifecycle risk. The construction row exception for `initial` events is justified because there is no prior state: the restriction's rationale does not apply to construction.

### 4.6 Guard Semantics on `on <Event>` Forms

With PRE0014 removed, `on Event when <guard> -> actions` is valid. Semantics:

- **First-match:** Multiple `on Event when ...` rows for the same event are evaluated in declaration order. First satisfied guard wins.
- **Fallback:** An `on Event` row without `when` is the unconditional fallback (must appear last if present).
- **Guard expressions:** Same expression language as `from State on Event when ...` guards. Can reference event args and field values.
- **Construction guards specifically:** Evaluated against the hollow version (defaults applied, initial state set, omitted fields absent). Event args are bound. Field reads observe defaults or structural absence.

### 4.7 Construction Must Always Be Possible

**Rule:** A precept with an initial event MUST have at least one construction row that can produce `Created`. If all construction rows unconditionally `reject`, or if guards provably exclude all cases such that only `reject` paths are reachable, the compiler MUST emit an error.

**Rationale:** If no construction row can ever succeed, the entity type is unconstructible — a definition-level defect. This is not a style concern (warning-level) — it is a structural impossibility that makes the precept useless. The analogous check in Swift: a `class` whose every `init?` always returns `nil` is diagnosed at compile time.

**Existing diagnostic:** `AlwaysRejecting` (PRE0125) already fires (graph analyzer) when every row for an event has a `reject` outcome. However, it currently fires as a **Warning** for all events generically. For initial events specifically, this diagnostic MUST be promoted to **Error** severity — an unconstructible precept is not merely suspicious, it is broken.

**Implementation requirement:** This is a REQUIRED compiler error, not an eventual enhancement. The graph analyzer already detects the condition; the change is severity promotion for initial events.

**Scope of the "always constructible" guarantee:**

The guarantee is specifically about the *existence of a success path* in the definition. The following cases are distinguished:

| Scenario | Classification | Enforcement |
|----------|---------------|-------------|
| All rows are `reject` rows (no mutation rows at all) | Compile error | PRE0125 severity promotion (Error for initial events) |
| Only reject rows + conditional mutation rows with unsatisfiable guards | Compile error (where provable) | Graph analyzer / proof engine must detect tautologically-false guards |
| Two or more `reject` rows (no mutation rows) | Same as "all rows are reject" — still compile error | PRE0125 covers it |
| Conditional mutation rows where all guards CAN be false at runtime, but no unconditional fallback | NOT a compile error — this is runtime `Rejected` | Runtime produces `Rejected("No construction row matched arguments")` |
| Conditional mutation rows with at least one satisfiable guard + reject fallback | Valid — the rejection path is intentional intake refusal | No diagnostic |

The compiler is not required to solve arbitrary satisfiability. The minimum viable detection is: zero mutation rows → Error. Richer detection (provably-unsatisfiable guards) is a quality improvement layered on top.

### 4.7A Multiple Initial Events

**Rule:** A precept MUST NOT declare more than one event with the `initial` modifier. `Precept.InitialEvent` is singular — the semantic model assumes exactly zero or one initial event.

**Diagnostic:** `MultipleInitialEvents`
- **Stage:** Type checker
- **Trigger:** Two or more event declarations carry the `initial` modifier in the same precept.
- **Message:** `"Only one event may be marked 'initial' — '{name}' conflicts with '{firstInitialEventName}'"`
- **Category:** Structure
- **Severity:** Error

> ❓ **Open Question — Shane's input needed:** Is `MultipleInitialEvents` a new diagnostic that needs a PRE code assigned, or does an existing diagnostic already cover this? If new, assign the next available code.

### 4.8 Hollow-Context Field Read Validation

**Rule:** Any expression evaluated while the entity is still hollow MUST be validated against the hollow availability set, not just `set` RHS expressions.

**Rationale:** At construction time, the entity does not yet exist. The hollow version has only: (a) the initial state set, (b) `default` values applied to fields that declare them, (c) event args, and (d) any fields established earlier in the same construction action chain. A required field with no default has NO value until one of those mechanisms establishes it.

**Current implementation coverage:**
- `UninitializedFieldReadInInitialAssignment` (PRE0142) and `UninitializedCrossFieldReadInInitialAssignment` (PRE0144) walk `set` action primary/secondary expressions only.
- Within those `set` expressions, the walker does recurse through ordinary nested forms (binary, unary, function call, conditional, quantifier, member access, interpolated string, list literal). The nested-expression lane is NOT the problem.

**Observed gaps:**
- `when <guard>` on construction rows is uncovered. `GetInitialConstructionActionChains(...)` drops guards entirely, and `ValidateInitialAssignmentSelfReads(...)` never visits them.
- Other expression-carrying construction actions (`add`, `remove`, `append`, `appendBy`, `insert`, `removeAt`, `put`, `enqueue`, `push`, etc.) are uncovered because the validator is gated by `IsSetAction(...)`.
- Interpolated typed-constant holes are uncovered even inside `set` expressions because `CollectFieldRefsFromExpression(...)` does not descend into `InterpolatedTypedConstant`.
- `reject "..."` is NOT an expression lane today. `RejectOutcome` stores a raw string, and parser interpolation holes are flattened to `{}` before type checking. There is nothing there for D142/D144 to walk.

**Other hollow contexts:**
- Field `default` expressions are another hollow-context lane. `ResolveFieldExpressions(...)` evaluates them in `PriorFieldsOnly` scope, but that scope only enforces declaration order (`DefaultForwardReference`). It does NOT reject reads of earlier required fields that themselves have no default.
- State entry/exit hooks are NOT hollow contexts. They run on an already-existing entity after a transition.

**Recommendation:** This is broader than a construction-guard-only fix. The correct repair is a shared hollow-context validator that walks every relevant expression position in construction rows and field-default evaluation with one availability model. A guard-only diagnostic is too narrow.

> ❓ **Open Question — Shane's input needed:** Is the hollow-context validator a **ship gate** (feature does NOT ship without it) or **deferred** (feature ships with documented risk)?
>
> **Option A — Ship gate:** The validator is required before construction semantics ship. Minimum viable subset: construction guards + all expression-carrying construction actions. Field defaults can be deferred (they are a pre-existing gap not introduced by this feature). Risk if deferred: an author writing `on Create when Amount > 0` where `Amount` has no default gets undefined runtime behavior (field is absent, comparison faults).
>
> **Option B — Ship with documented risk + interim mitigation:** Feature ships without the validator. Interim mitigation: emit a **Warning** on any field read in a construction guard that references a non-defaulted field (conservative — may flag correct code that references event args). Validator ships as fast-follow. Risk: authors CAN write guards that read uninitialized fields; runtime behavior is a fault rather than a compile error.
>
> The scope of the validator (per George B5) is materially larger than "just guards" — `GetInitialConstructionActionChains` drops guards, non-`set` actions are skipped, `InterpolatedTypedConstant` is unwalked, and field defaults only get declaration-order checking. The ship gate decision needs to account for this full scope.

---

## 5. D94 Simplification

### Before (Current)

D94 (`InitialEventMissingAssignments`) checks per-construction-row completeness: every guarded path through the initial event must assign all required fields. The target state of each row determines which fields are required (because `omit` declarations vary by state). When construction-time transitions are allowed, D94 must resolve the target state per row — which may differ across guarded paths.

### After (This Design)

**D94 becomes a single-target check.** The target state is always the initial state (compile-time-known, invariant across all construction rows). The check simplifies to:

1. Identify all fields that are required in the initial state (non-optional, no default, not `omit` in initial state).
2. For each `on <InitialEvent>` row (each guarded path), verify the action chain assigns all identified fields.
3. If any row fails, emit `InitialEventMissingAssignments` on the failing row.

**`Transitioned` removed from construction outcomes.** The proof engine no longer needs to reason about "which state did construction land in?" — it's always the initial state.

**D132 unchanged.** `OmittedFieldMaterialization` still covers post-construction transitions: when a transition moves from a state where a field is `omit` to a state where it is present, the transition's action chain must assign it. This is orthogonal to construction.

---

## 6. Runtime API Impact

### EventOutcome Changes for Construction

**Before:**
```csharp
Version version = outcome switch
{
    EventOutcome.Transitioned t      => t.Result,     // initial event transitioned to another state
    EventOutcome.Applied a           => a.Result,     // stayed in initial state
    EventOutcome.Rejected r          => ...,
    EventOutcome.ConstraintsFailed f => ...,
    EventOutcome.Unmatched           => ...,
    EventOutcome.UndefinedEvent      => ...,
};
```

**After:**
```csharp
Version version = outcome switch
{
    EventOutcome.Created c           => c.Result,     // construction succeeded — entity now exists in initial state
    EventOutcome.Rejected r          => ...,          // authored reject row matched, or all guards failed
    EventOutcome.ConstraintsFailed f => ...,          // post-mutation constraint violations
    EventOutcome.InvalidArgs ia      => ...,          // wrong type, unknown key, or missing required arg
    EventOutcome.Faulted fault       => ...,          // evaluator impossible path / runtime fault
    // Transitioned — structurally impossible (no Outcome slot on construction rows)
    // UndefinedEvent — structurally impossible (compiler ensures initial event rows exist; Create() targets a known event)
    // Unmatched — structurally impossible (see below)
    // Applied — not a Create() outcome (see below)
};
```

**Valid construction outcomes:** `Created`, `Rejected`, `ConstraintsFailed`, `InvalidArgs`, `Faulted`.

- **`InvalidArgs`** is reachable when the caller passes wrong types, unknown keys, or omits required args. Arg validation happens before row matching — if args are invalid, no row is attempted.
- **`Faulted`** is reachable if the evaluator encounters an impossible path or runtime fault during guard evaluation or action execution. This is the "something went wrong at runtime that the compiler didn't prevent" safety net.

**`Created` is the construction success outcome.** `Create()` brings an entity into existence — semantically distinct from `Fire()` applying mutations to an existing entity. `Created` carries `Version Result` (same shape as `Applied`) but communicates construction rather than application. `Fire()` continues to produce `Applied` or `Transitioned`; `Create()` produces `Created`.

**`Unmatched` is NOT a valid construction outcome.** The compiler guarantees at least one construction row exists for the initial event (§4.7). `Unmatched` semantically means "event not recognized in this context" — but the initial event IS recognized; rows ARE defined. If all `when` guards fail at runtime, no row accepted the invocation — the author failed to provide unconditional fallback coverage. That is a rejection of this particular construction attempt, not an undefined-event scenario. The runtime produces `Rejected` with a diagnostic reason indicating no construction row matched.

**`Transitioned` is never a construction outcome.** The grammar prevents it (structural exclusion — no Outcome slot on EventRow). Runtime never produces it for `Create()`.

### `Create()` Without an Initial Event

When a precept has no `initial` event declared, `Create()` is parameterless and does not fire any event. `docs/runtime/runtime-api.md` currently says this returns `Applied`. With this design:

- **`Create()` with no initial event returns `Created`** (not `Applied`). The semantic distinction: `Created` means "an entity was brought into existence." Whether that creation involved firing an initial event or just applying defaults is an implementation detail — the outcome category is the same.
- **`Applied` is NOT a `Create()` outcome in any path.** `Applied` means "mutations were applied to an existing entity without a state transition" — this is exclusively a `Fire()` outcome. Callers match `Created` from `Create()` regardless of whether an initial event existed.
- **Update `docs/runtime/runtime-api.md`** to reflect this: `Create()` always returns `Created` on success, whether or not an initial event fires.

### `EventOutcome.Rejected` Reason Contract

`EventOutcome.Rejected` carries a reason string. The design distinguishes two provenance categories:

- **Authored rejection:** The `reject "reason"` clause in a construction row supplies the reason. Example: `Rejected("Claims require a positive amount")`. The reason string is the author's exact text.
- **Synthesized no-match rejection:** When all `when` guards fail and no unconditional fallback row exists, the runtime produces `Rejected` with a stable standard message: `"No construction row matched arguments"`.

Preview, MCP inspection, and the inspector MUST preserve this provenance distinction. The `Rejected.Reason` field carries both forms, but tooling should render them differently:
- Authored: show the author's reason text directly
- Synthesized: render as a system-generated message (e.g., italicized or prefixed with "[system]")

UI copy in preview/inspector should show `"Created in {initial state name}"` as the human-readable label for `Created` outcomes (confirmed — Elaine G1).

### Breaking Change

**Callers who previously matched `Applied` on `Create()` must now match `Created`.** Although `Create()` is currently a stub returning `Unmatched`, `docs/runtime/runtime-api.md` documents the intended contract as returning `Applied`. That contract changes:

| API | Before | After |
|-----|--------|-------|
| `Create()` success with initial event | `Applied` | `Created` |
| `Create()` success without initial event | `Applied` | `Created` |
| `Fire()` success without transition | `Applied` | `Applied` (unchanged) |

Any exhaustive `EventOutcome` switch that does not include a `Created` arm will fail to compile after this change. This is intentional — consumers MUST distinguish construction from mutation.

### Fire-Once Enforcement

`version.Fire(initialEventName, args)` on an existing entity returns `EventOutcome.UndefinedEvent`. The initial event is not in the post-construction event space. No new API surface — `UndefinedEvent` already exists for this purpose.

### Discovery Surface

```csharp
public sealed class Precept
{
    public EventDescriptor? InitialEvent { get; }   // unchanged — null if no initial event
    public IReadOnlyList<EventDescriptor> Events { get; }  // includes initial event in full catalog
}
```

`precept.Events` continues to include the initial event in the definition-level catalog (for introspection). `version.AvailableEvents` (if/when exposed) excludes it post-construction.

---

## 7. New Diagnostics

### `ConstructionTransitionNotAllowed` — REMOVED (structural exclusion)

- **Action:** This diagnostic is not needed. The construction-row constructs (`EventRow` and `EventRowReject`) omit the transition-outcome lane entirely — `transition` and `no transition` cannot be expressed in construction grammar. There is nothing to diagnose because there is nothing to write.
- **Replacement:** None. The grammar makes the invalid form impossible. Completions will not offer `transition` in construction context. If an author types `-> transition` in an `on Event` row, parse recovery should produce a targeted syntax error.
- **Rationale for deletion over reframing as parse error:** Precept's philosophy is "impossible by construction." A diagnostic for something the grammar cannot express is noise. The language server's completions and syntax highlighting already communicate what is valid in each context. A dedicated diagnostic explaining why you can't write something that doesn't exist in the grammar adds complexity without value.

### `InitialEventInTransitionRow`

- **Stage:** Type checker
- **Trigger:** A `from <State> on <InitialEvent>` row references the initial event.
- **Message:** `"The initial event '{name}' cannot appear in transition rows — it is reserved for construction. Use 'on {name}' for construction rows."`
- **Fix hint:** `"Use 'on {name}' for construction"`
- **Category:** Structure
- **Severity:** Error

### `EventRowDoesNotSupportGuard` — REMOVED

- **Action:** Delete PRE0014 from the diagnostic catalog. Remove the parser check that emits it.
- **Replacement:** None. Guards on `on Event` forms are now valid.

### `UnreachableRow`

- **Stage:** Type checker
- **Trigger:** A construction row or transition row is ordered after a prior row that always matches first, making the later row unreachable.
- **Message:** Context-sensitive. For construction: `"Unreachable construction row — a prior unconditional 'on {name}' row above always matches first"`. For transitions: `"Unreachable transition row — a prior unconditional 'from {state} on {name}' row above always matches first"`.
- **Fix hint:** `"Move the unconditional row to the end, or add a 'when' guard to it"`
- **Category:** Structure
- **Severity:** Warning

> ✅ **Locked decision (2026-05-15):** Use a single `UnreachableRow` diagnostic code for both construction rows and transition rows ordered after an always-matching row. The structure is identical in both cases — a later row can never be reached because an earlier row always wins — so one code should represent the single concept. The accepted tradeoff is message text that changes by context (`construction row` vs `transition row`) while the code stays unified.

### Reject-path mutual-exclusion diagnostics — NOT ADDED (grammar split)

- **Action:** Do not add `ConstructionRowMutualExclusion` or a transition-row analog. The grammar split into `EventRow` / `EventRowReject` and `TransitionRow` / `TransitionRowReject` makes mixed mutation+reject rows inexpressible.
- **Replacement:** None. Parser routing selects the correct construct from the first post-arrow token. There is no valid AST shape with both an action chain and a reject clause.
- **Recovery:** If an author types a hybrid sequence, parse-error recovery may give targeted guidance, but that is syntax recovery, not semantic mutual-exclusion validation.
- **Rationale:** Precept's job is structural prevention. A diagnostic that explains why an impossible hybrid row is invalid would never fire in valid code and should not exist.

### `ZeroConstructionRows`

- **Stage:** Type checker
- **Trigger:** A precept declares an `initial` event but has zero `on <InitialEvent>` construction rows.
- **Message:** `"Initial event '{name}' has no construction rows — at least one 'on {name}' row is required"`
- **Fix hint:** `"Add at least one 'on {name} -> set ...' construction row"`
- **Category:** Structure
- **Severity:** Error
- **Note:** This is distinct from PRE0125 (`AlwaysRejecting`), which detects all-reject. `ZeroConstructionRows` detects the absence of ANY rows for the initial event. The check must inspect `EventRow` constructs (not just `TransitionRow` constructs) — the existing `EmitAlwaysRejecting` in `GraphAnalyzer.cs:721-744` only inspects `TransitionRows` and would miss this case.

### `TransitionInConstructionRowParseError` (UX guidance)

- **Stage:** Parser (error recovery)
- **Trigger:** The token sequence `-> transition` appears after a construction row's action chain, where the parser cannot consume it.
- **Current behavior:** Generic parse error at construct boundary (no recovery guidance).
- **Required improvement:** The parse-error recovery path should detect the `-> transition` pattern in `EventRow` context and produce a targeted message: `"'transition' is not valid in construction rows — construction always terminates in the initial state"`
- **Fix hint:** `"Remove '-> transition {state}' — the entity is automatically placed in the initial state"`
- **Category:** Syntax
- **Severity:** Error

### `AlwaysRejecting` Severity Promotion for Initial Events

- **Stage:** Graph analyzer (existing)
- **Diagnostic code:** PRE0125 (`AlwaysRejecting`)
- **Current behavior:** Fires as **Warning** when every row for an event has a `reject` outcome across all states.
- **Required change:** When the flagged event carries the `initial` modifier, severity MUST be **Error**. An unconstructible precept is not a style concern — it is a definition-level defect that makes the entity type permanently unusable.
- **Message (when initial event):** `"Initial event '{0}' always rejects — the entity type is unconstructible. At least one construction row must be able to produce 'Created'"`
- **Message (when non-initial, unchanged):** `"Event '{0}' always rejects on every path — if this event is not applicable in any state, remove all rows for it"`
- **Note:** The existing GraphAnalyzer `EmitAlwaysRejecting` method already detects the condition. Implementation adds a severity override when `semantics.EventsByName[eventName].IsInitial`.

### Hollow-Context Uninitialized Read Validation

- **Stage:** Type checker
- **Scope:** Every expression position evaluated before construction has established a full entity: construction guards, all expression-carrying construction actions, and field `default` expressions.
- **Current gap:** PRE0142/PRE0144 only cover `set` action primary/secondary expressions. They do NOT cover construction guards, non-`set` input actions, interpolated typed-constant holes, or field defaults.
- **Implementation direction:** Add one shared validator in `TypeChecker.Validation.FieldState.cs` that evaluates field availability in hollow context (`event args` + `defaulted fields` + `prior construction assignments`, while treating presence checks as non-reads). Context-specific diagnostics may still be emitted, but the walker/availability model should be shared.

---

## 8. Implementation Scope

### Grammar / Token Changes

- **Audit result:** `Constructs.cs` currently exposes `reject` only through `TransitionRow`'s `Outcome` slot. `EventRow` has no reject lane today, and no other construct accepts rejection. `docs/language/precept-language-spec.md` mirrors that shape in the transition-row grammar.
- **New slot kind: `SlotRejectClause`** — a restricted production that accepts `-> reject StringExpr` only. This becomes the dedicated rejection lane for any construct family that supports authored refusal.
- **New slot kind: `SlotSuccessOutcome`** — a narrowed success-only outcome production that accepts `-> transition State` or `-> no transition`. `TokenKind.Reject` stays exactly where `Tokens.cs` already places it: valid after `->`, message position unchanged.
- **Construct split:** Keep `EventRow` as the success-path construct and add `EventRowReject` as the reject-path construct; reclaim `TransitionRow` as the unmarked success-path construct and add `TransitionRowReject` as the explicit reject-path construct. Slot shapes:
  - `EventRow` → `[SlotEventTarget, SlotPreVerbGuardArrow, SlotActionChain]`
  - `EventRowReject` → `[SlotEventTarget, SlotPreVerbGuardArrow, SlotRejectClause]`
  - `TransitionRow` → `[SlotStateTarget, SlotEventTarget, SlotGuardClause, SlotActionChain, SlotSuccessOutcome]`
  - `TransitionRowReject` → `[SlotStateTarget, SlotEventTarget, SlotGuardClause, SlotRejectClause]`
- **No new tokens.** `reject` already exists as `TokenKind.Reject`.
- **Construct descriptions update:** Remove the stateless-only wording from the current event-row description, and update transition-row wording to distinguish the unmarked success path from the explicit reject path.

### Parser Changes

- **Remove PRE0014 guard check** in `EventRow`-family parsing. Allow `when` guards on `on Event` forms.
- **Add `RejectClause` parser and narrowed `SuccessOutcome` parser.** `RejectClause` parses `-> reject StringExpr`. `SlotSuccessOutcome` parses only `transition <State>` and `no transition`.
- **Construction-row routing:** After `on Event [when Guard]`, route to `EventRowReject` when the first post-arrow token is `reject`; otherwise route to `EventRow`.
- **Transition-row routing:** After `from State on Event [when Guard]`, route to `TransitionRowReject` when the first post-arrow token is `reject`; route to `TransitionRow` when it is an action keyword, `transition`, or `no`. The current spec/grammar gap — action chain followed by `reject` — closes here.
- **Disambiguation update:** `on` / `from` leading-token disambiguation still uses the same shared prefix; the new routing happens after the shared prefix, at the first post-arrow token. No token invention or secondary keyword scheme is required.
- **Error recovery for hybrid rows:** If a mutation row later encounters `reject` where `SlotSuccessOutcome` is required, emit a targeted syntax error explaining that reject must be authored as its own row.
- **Files:** `src/Precept/Pipeline/Parser.cs`, `src/Precept/Pipeline/Parser.Expressions.cs` (or equivalent parser partials handling outcome/reject routing)

### Type Checker Changes

- **Add `InitialEventInTransitionRow`:** Validate that no `from State on Event` reject or mutation row references an event with the `initial` modifier.
- **No action/reject mutual-exclusion check:** Neither construction rows nor transition rows need a semantic exclusivity diagnostic. The grammar split owns that invariant.
- **Add `ZeroConstructionRows` check:** If a precept declares an `initial` event but has zero `on <InitialEvent>` construction rows, emit error. This check must inspect both construction variants — the existing `EmitAlwaysRejecting` in `GraphAnalyzer.cs:721-744` only inspects `TransitionRows` and would miss this case.
- **Add `MultipleInitialEvents` check:** If two or more events carry the `initial` modifier, emit error.
- **Fix PRE0092 exception for initial-event construction rows:** `TypeChecker.Validation.Structural.cs:16-27` currently emits PRE0092 (`EventRowInStatefulPrecept`) for EVERY `EventRow` in a stateful precept (`ctx.States.Count > 0`). The exception logic must apply to both construction constructs (`EventRow` and `EventRowReject`) when the event target resolves to the precept's initial event. Non-initial `on Event` forms in stateful precepts remain rejected (Decision 5).
- **Simplify D94 (`InitialEventMissingAssignments`):** Target state is always the initial state. Remove per-row target-state resolution. **Note:** D94 is in `TypeChecker.Validation.FieldState.cs:340-385`, not the proof engine.
- **`ConstructionTransitionNotAllowed` NOT NEEDED:** The grammar structurally excludes transition outcomes from construction-row constructs. No type-checker validation required.
- **Files:** `src/Precept/Pipeline/TypeChecker.cs`, `src/Precept/Pipeline/TypeChecker.Validation.Structural.cs`, `src/Precept/Pipeline/TypeChecker.Validation.FieldState.cs`

### Semantic Model Changes

- **`TypedEventRow` DU redesign required.** Current shape in `SemanticIndex.cs:443-448` is `(EventName, Actions, Syntax)` — no place to store guard, reject reason, or row kind. The design requires a discriminated union:

```csharp
public abstract record TypedEventRow(string EventName, TypedExpression? Guard, SyntaxNode Syntax);
public sealed record TypedEventRowResolution(string EventName, TypedExpression? Guard, IReadOnlyList<TypedAction> Actions, SyntaxNode Syntax) : TypedEventRow(EventName, Guard, Syntax);
public sealed record TypedEventRowReject(string EventName, TypedExpression? Guard, string RejectReason, SyntaxNode Syntax) : TypedEventRow(EventName, Guard, Syntax);
```

- **`TypedTransitionRow` DU redesign required.** Current shape in `SemanticIndex.cs:380-405` is a flat bag (`Actions`, `Outcome`, `RejectReason`) that still models the forbidden hybrid. Replace it with a discriminated union:

```csharp
public abstract record TypedTransitionRow(string? FromState, string EventName, TypedExpression? Guard, SourceSpan RowSpan, ParsedConstruct Syntax);
public sealed record TypedTransitionResolutionRow(string? FromState, string EventName, TypedExpression? Guard, IReadOnlyList<TypedAction> Actions, TransitionSuccessOutcome Outcome, string? TargetState, SourceSpan RowSpan, ParsedConstruct Syntax) : TypedTransitionRow(FromState, EventName, Guard, RowSpan, Syntax);
public sealed record TypedTransitionRejectRow(string? FromState, string EventName, TypedExpression? Guard, string RejectReason, SourceSpan RowSpan, ParsedConstruct Syntax) : TypedTransitionRow(FromState, EventName, Guard, RowSpan, Syntax);
```

- **Rationale:** Catalog compliance. Guard/actions/reject/outcome shape is variant data, not nullable-bag data. Downstream consumers switch on subtype, not on nullable fields or enum identity.
- **Files:** `src/Precept/Pipeline/SemanticIndex.cs`, transition/event normalization paths in `src/Precept/Pipeline/TypeChecker.cs`

### Graph Analysis Changes

- **Construction `EventRow` constructs must not trigger false positives.** `UnhandledEvent`, event coverage, and `GraphEvent.IsInitial` are derived from transition-row edges only (`GraphAnalyzer.cs:149-158`, `208-213`, `299-367`, `560-586`). Construction rows in the `EventRow` family will be falsely reported as unhandled/non-initial unless GraphAnalyzer explicitly handles them:
  - `IsInitial` derivation must recognize construction `EventRow` rows as initial-event handling
  - Event coverage analysis must count construction rows toward the initial event's coverage
  - `UnhandledEvent` must not fire for the initial event when construction rows exist (even though no `TransitionRow` handles it)
- **`EmitAlwaysRejecting` must inspect `EventRow` constructs:** Currently inspects only `TransitionRows`. Must extend to check `EventRow` constructs for the initial event when evaluating "all paths reject."
- **File:** `src/Precept/Pipeline/GraphAnalyzer.cs`

### Proof Engine Changes

- **D94 simplification:** Single-target analysis. Remove any branching logic that resolved per-row target states for construction. **Note:** D94 is in `TypeChecker.Validation.FieldState.cs:340-385`, not the proof engine — but the proof engine's flow-narrowing strategies must still be updated.
- **Remove `Transitioned` from construction proof obligations.**
- **Guard-aware proof strategies for construction rows:** Current guard-aware proof strategies read guards only from `TransitionRowContext` and `StateHookContext` (`ProofEngine.Strategies.cs:513-518`, `ProofEngine.Intervals.cs:390-395`). `EventRowContext` has no guard because `TypedEventRow` currently has no guard field. After the semantic model DU redesign, guarded construction rows WILL have guards — the proof engine must read them to preserve flow-narrowing / interval proof power.
- **Files:** `src/Precept/Pipeline/ProofEngine.cs`, `src/Precept/Pipeline/ProofEngine.Strategies.cs`, `src/Precept/Pipeline/ProofEngine.Intervals.cs`, `src/Precept/Pipeline/ProofLedger.cs`

### Runtime Changes

- **Fire-once enforcement:** Initial event excluded from post-construction dispatch table. `Fire(initialEventName)` returns `UndefinedEvent` on existing versions.
- **Construction outcome space:** `Create()` returns `Created` on success (both with and without initial event). Never returns `Transitioned` or `Applied`. Evaluator construction path must produce `Created`, `Rejected`, `ConstraintsFailed`, `InvalidArgs`, or `Faulted`.
- **`EventOutcome.Created` DU case:** Add `Created` to the `EventOutcome` discriminated union (`Runtime/EventOutcome.cs:11-40`). Same shape as `Applied` (carries `Version Result`) but semantically distinct.
- **`Precept.Create()` stub update:** Currently returns `Unmatched` (stub). Must be updated to implement: arg validation → row matching → action execution → constraint evaluation → `Created`/`Rejected`/`ConstraintsFailed`/`InvalidArgs`/`Faulted`.
- **`Version.AvailableEvents` stub:** Currently stubbed. Must exclude the initial event post-construction.
- **`Evaluator`:** Almost entirely TODO. The construction path through the evaluator is net-new implementation, not a modification of existing working code.
- **Scope note:** The runtime implementation scope is materially larger than "modify existing dispatch." `Create()`, `AvailableEvents`, and the evaluator are stubs/TODOs. Implementation planning must account for this.
- **`Rejected` reason contract:** `EventOutcome.Rejected` requires a reason string. Authored reject: use the row's `reject "..."` string. Synthesized no-match: use the stable string `"No construction row matched arguments"`.
- **File:** `src/Precept/Runtime/EventOutcome.cs`, `src/Precept/Runtime/Precept.cs`, `src/Precept/Runtime/Version.cs`, `src/Precept/Runtime/Evaluator.cs`

### Diagnostics Catalog

- **Add:** `InitialEventInTransitionRow`, `ZeroConstructionRows`, `MultipleInitialEvents`, `TransitionInConstructionRowParseError` (parse-error recovery)
- **Remove:** `EventRowDoesNotSupportGuard` (PRE0014)
- **NOT added (structural exclusion):** `ConstructionTransitionNotAllowed`, `ConstructionRowMutualExclusion`, and any transition-row mutual-exclusion diagnostic — unnecessary because grammar prevents the invalid forms
- **Files:** `src/Precept/Language/DiagnosticCode.cs`, `src/Precept/Language/Diagnostics.cs`

### Language Spec Update

- **§3A.5 Entity Construction + transition-row grammar:** Rewrite to reflect `on <Event>` construction syntax, fire-once enforcement, guard allowance, `EventRow` / `EventRowReject`, and `TransitionRow` / `TransitionRowReject`.
- **File:** `docs/language/precept-language-spec.md`

### Runtime API Doc Update

- **Construction section:** Remove `Transitioned` from construction outcome examples. Add `Created` as the success outcome. Document `Create()` always returns `Created` (not `Applied`) for both initial-event and no-initial-event cases. Document fire-once behavior. Add `InvalidArgs` and `Faulted` to the outcome space.
- **File:** `docs/runtime/runtime-api.md`

### Sample Files That Need Updating

Only `samples/Test.precept` currently declares an `initial` event. Review:

| File | Change needed |
|------|---------------|
| `samples/Test.precept` | Has `event start initial` with `from running on start`. This is `from running` (not from initial state `off`), meaning it's a post-construction use of the initial event — which is now illegal (`InitialEventInTransitionRow`). Must restructure: either remove `initial` from the event, or replace with a proper construction row. |

All other samples (30 files) do NOT declare initial events. They use the form-fill pattern (editable fields in initial state → regular event triggers transition). **No changes needed** for samples without `initial` events.

### Language Server

- **Completions — `when` keyword:** Remove logic that suppresses `when` after `on Event`. Allow guard completions in the `EventRow` / `EventRowReject` family. **Critical:** `tools/Precept.LanguageServer/SlotContext.cs` currently keys `AfterEventTarget` off constructs with an outcome lane; the split event-row family still needs explicit `AfterEventTarget` support even though it has no success-outcome slot.
- **Completions — `reject` / success keywords:** Surface `reject` only at the first post-arrow position for reject-bearing families. In `EventRow` context, offer action verbs + `reject` immediately after the shared prefix, then drop `reject` once an action commits the row to `EventRow`. In `TransitionRow` context, offer action verbs, `transition`, `no`, and `reject` at the first post-arrow position; after any action, only `transition` / `no transition` remain.
- **Completions — event name filtering:** After bare `on` in a stateful precept, current LS offers all events. With Decision 5 closed, tooling should prioritize (or filter to) only the `initial` event in stateful precept context. Non-initial events in `on Event` form are rejected by the type checker — completions should not offer them.
- **Hover — `initial` modifier:** Update hover text to reflect terminal/fire-once semantics: "Marks this event as the construction mechanism. The entity is always created in the initial state. This event fires exactly once (at construction) and cannot be fired on an existing entity."
- **Hover — `on <InitialEvent>` row form:** Add hover text for the construction row form itself (not just the modifier): "Construction row — when this row matches, the entity is created in the initial state with the specified field assignments. Construction rows use first-match semantics."
- **Hover — `-> reject` in construction row:** `TryCreateRejectHover` currently only handles transition rows. Add hover for reject in construction context: "Intake refusal — this construction attempt is rejected with the specified reason. The entity is not created."
- **Semantic tokens:** `SemanticTokensHandler`, `SemanticExpressionLocator`, and `TypedConstantCollector` only walk handler actions today. `when` guards in construction rows will have incomplete semantic token / typed-constant coverage. These files must be updated to walk guard expressions in `EventRow` constructs.
- **Highlighting:** `on <InitialEvent>` and `from ... on <Event>` use the same highlighting (same token kinds, same semantic token types). No distinct coloring for construction rows — they are syntactically event handlers and should look like event handlers. State this explicitly.
- **Diagnostics:** New diagnostics surface automatically via the diagnostic catalog.
- **Files:** `tools/Precept.LanguageServer/SlotContext.cs`, `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`, `tools/Precept.LanguageServer/Handlers/HoverHandler.cs`, `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs`, `tools/Precept.LanguageServer/SemanticExpressionLocator.cs`, `tools/Precept.LanguageServer/TypedConstantCollector.cs`

### Grammar Generator

- **`tools/Precept.GrammarGen/Program.cs`** currently hardcodes a single `eventRow` form and a single transition-row form. It must emit `EventRow` / `EventRowReject` and `TransitionRow` / `TransitionRowReject`, including `when` guards and the narrowed success-outcome lane.
- **File:** `tools/Precept.GrammarGen/Program.cs`

### MCP

- **`precept_compile` DTO changes:** Current `CompileResultDto` only exposes diagnostics/proofs/summary. It does NOT expose `PreceptDefinition`, initial-event flag, construction rows, guards, or rejects. The summary mislabels construction rows as "transitions." Required changes:
  - Add `initialEvent` field to the definition summary (event name + args, or null)
  - Add `constructionRows` to the event detail (list of rows with guard expression + outcome type)
  - Fix summary text to distinguish "N construction rows" from "N transition rows"
- **`EventOutcome.Created` is a wire-contract change:** The planned runtime MCP spec enumerates outcomes exhaustively. Adding `Created` breaks any exhaustive consumer switch that doesn't include it. Document in MCP changelog when implemented.
- **File:** `tools/Precept.Mcp/Tools/CompileTool.cs`

> **Decision:** `docs/tooling/mcp.md` is the single canonical MCP contract document. `docs/McpServerDesign.md` has been consolidated into it and archived. Sync language-surface MCP changes against the live registered tools in `tools/Precept.Mcp/Tools/`, as listed in `docs/tooling/mcp.md`.

> **Decision — Locked (2026-05-15): Add `precept_create` as a new MCP tool (Option A).** Construction should be callable through MCP via a dedicated tool rather than by overloading `precept_fire`.
>
> - `precept_create` accepts precept definition text (or a loaded definition reference), construction event name, and construction arguments as key-value pairs.
> - It returns the construction outcome (`Created`, `Rejected`, or `NoMatchingRow`), the resulting initial state when created, the reject reason when rejected, and the matched construction row.
> - `precept_fire` remains the tool for existing entities only; `precept_create` owns initial construction.

> **Decision — Locked (2026-05-15): Add `InspectCreate` to core and expose it via `precept_inspect` (Option A).** Construction inspection should sit on the same runtime-facing MCP surface as existing-entity inspection.
>
> - `precept_inspect` will cover both `InspectFire()` for existing entities and `InspectCreate()` for construction previews.
> - Construction inspection must show which row would match, which guards pass or fail, what actions would execute, and whether the predicted outcome is `Created` or `Rejected`.

### MCP Tool Surface Sync

Language surface changes require updates to the following live MCP tools (NOT `LanguageTool.cs` which was removed):

- **`precept_syntax`:** The syntax reference must show `on <Event>` construction as split mutation/reject forms, and must rewrite the transition-row grammar so `reject` is its own row shape rather than a terminal member of the success-path construct.
- **`precept_quickstart`:** If it currently shows `from <InitialState> on <InitialEvent>`, update to show `on <Event>` construction form.
- **`precept_types`:** The `initial` modifier semantics changed (now means fire-once + terminal construction). The modifier catalog entry's description must be updated.
- **`precept_patterns`:** See dedicated section below.

### MCP Pattern Catalog

- **`precept_patterns` update required:** Add a constructor-pattern entry before this feature ships. The entry MUST include:
  - **Good pattern:** `event X(...) initial` declaration, guarded `on X when ...` mutation rows, final unconditional `on X -> reject "reason"` fallback, explicit absence of `from`/`transition`
  - **Anti-pattern:** `from InitialState on X -> transition OtherState` (the old way — now illegal)
  - **Runtime side:** `Create(args)` → match on `Created | Rejected | ConstraintsFailed | InvalidArgs | Faulted`
  - **Hollow-context anti-pattern (Elaine G2):** Bad: `on Create when Amount > 0` where `Amount` has no default (reads uninitialized field). Good: `on Create when Create.Amount > 0` (reads event arg, always available).
- **File:** `tools/Precept.Mcp/Tools/PatternsTool.cs` (or equivalent)

### Catalog Syntax Reference Updates

- **Non-defaulted field restriction:** `docs/language/precept-language-spec.md` and/or `docs/language/catalog-system.md` must document the restriction that construction row `when` guards and `-> action` chains cannot reference fields without declared `default` values. Currently not documented in either spec.
- **`on <Event>` construction syntax + transition-row reject split:** The language spec's description of the initial event form and transition-row grammar must be updated to reflect the new `on <Event>` construction row syntax plus the `TransitionRow` / `TransitionRowReject` split. The current transition grammar still permits action-chain + reject hybrids.
- **`AlwaysRejecting` severity for initial events:** The diagnostics catalog entry for PRE0125 needs severity differentiation documentation (Warning for general events, Error for initial events).

### Diagram / Preview Rendering

> 🔒 **Locked decision (OQ8):** Option A — Entry arrow from pseudo-node.  
> State diagram shows a filled `●` pseudo-node with a labeled edge to the initial state; the event-name label is optional when there is only one constructor.  
> Construction rows are NOT rendered as separate state-machine elements in the diagram.  
> Inspector/preview shows construction rows in a distinct section above transition rows.  
> Rationale: matches UML initial-pseudostate convention, feels familiar, and keeps the diagram uncluttered.

### Hollow-Context Validation Implementation

- **Shared hollow-context validator:** Add one check in `TypeChecker.Validation.FieldState.cs` that walks construction guards, every expression-carrying construction action, and field `default` expressions against a single availability model. Do NOT extend PRE0142/PRE0144 one expression slot at a time again.
- **Diagnostic surfacing:** Guard-specific or assignment-specific diagnostics may still exist, but they should be emitted by the shared validator rather than bespoke per-slot walkers.
- **`AlwaysRejecting` severity promotion:** Modify `GraphAnalyzer.EmitAlwaysRejecting` to emit `Severity.Error` (instead of `Severity.Warning`) when the flagged event is the initial event. Minor change — the detection logic already exists. Note: `EmitAlwaysRejecting` must also inspect `EventRow` constructs (not just `TransitionRows`) when checking the initial event.

### File Inventory

| File | Change type | Description |
|------|-------------|-------------|
| `src/Precept/Language/ConstructKind.cs` | Modify | Split `EventRow` and `TransitionRow` into mutation/reject construct kinds |
| `src/Precept/Language/ConstructSlot.cs` | Modify | Add `RejectClause` and `SuccessOutcome` slot kinds |
| `src/Precept/Language/Constructs.cs` | Modify | Define split reject-aware constructs and slot shapes |
| `src/Precept/Language/DiagnosticCode.cs` | Modify | Add new diagnostic codes; do **not** add mutual-exclusion codes |
| `src/Precept/Language/Diagnostics.cs` | Modify | Add/remove diagnostic entries and keep reject mutual exclusion grammar-owned |
| `src/Precept/Pipeline/Parser.cs` | Modify | Remove PRE0014 check; route shared prefixes into mutation vs. reject constructs |
| `src/Precept/Pipeline/Parser.Expressions.cs` | Modify | Parse `SlotSuccessOutcome` vs. `SlotRejectClause`; hybrid-row recovery |
| `src/Precept/Pipeline/TypeChecker.cs` | Modify | New validation rules; normalize split transition/event-row constructs |
| `src/Precept/Pipeline/TypeChecker.Validation.Structural.cs` | Modify | PRE0092 exception for initial-event construction variants |
| `src/Precept/Pipeline/TypeChecker.Validation.FieldState.cs` | Modify | D94 simplification, hollow-context validator, zero-row check |
| `src/Precept/Pipeline/SemanticIndex.cs` | Modify | `TypedEventRow` + `TypedTransitionRow` DU redesign |
| `src/Precept/Pipeline/GraphAnalyzer.cs` | Modify | Construction row awareness; inspect reject variants for always-rejecting analysis |
| `src/Precept/Pipeline/ProofEngine.cs` | Modify | Remove `Transitioned` from construction proofs |
| `src/Precept/Pipeline/ProofEngine.Strategies.cs` | Modify | Guard-aware strategies for construction-row variants |
| `src/Precept/Pipeline/ProofEngine.Intervals.cs` | Modify | Guard-aware intervals for construction-row variants |
| `src/Precept/Pipeline/ProofLedger.cs` | Modify | Construction row proof entries |
| `src/Precept/Runtime/EventOutcome.cs` | Modify | Add `Created` DU case |
| `src/Precept/Runtime/Precept.cs` | Modify | `Create()` implementation |
| `src/Precept/Runtime/Version.cs` | Modify | `AvailableEvents` excludes initial event |
| `src/Precept/Runtime/Evaluator.cs` | Modify | Construction evaluation path |
| `tools/Precept.GrammarGen/Program.cs` | Modify | Emit split construction/transition reject forms |
| `tools/Precept.LanguageServer/SlotContext.cs` | Modify | Shared-prefix completion context for reject-bearing families |
| `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` | Modify | First-post-arrow `reject` / success completion rules |
| `tools/Precept.LanguageServer/Handlers/HoverHandler.cs` | Modify | Construction row hover, reject hover |
| `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` | Modify | Walk guard expressions in construction-row variants |
| `tools/Precept.LanguageServer/SemanticExpressionLocator.cs` | Modify | Guard expression coverage |
| `tools/Precept.LanguageServer/TypedConstantCollector.cs` | Modify | Guard typed-constant coverage |
| `tools/Precept.Mcp/Tools/CompileTool.cs` | Modify | DTO changes for construction rows and split transition/reject reporting |
| `docs/language/precept-language-spec.md` | Modify | §3A.5 + transition-row grammar rewrite |
| `docs/runtime/runtime-api.md` | Modify | Construction outcome updates |
| `samples/Test.precept` | Modify | Fix illegal initial-event usage |

---

## 9. Out of Scope

1. **Stateless handler allowance in stateful precepts.** The grammar enables it (shared `EventRowDeclaration` + guard support), but the type-checker rule deciding when a non-initial event can use `on Event` form in a stateful precept is a separate design decision.

2. **Migration tooling.** No migration path needed. The samples are the only Precept code in existence and will be updated directly as part of implementation.

3. **`no transition` semantics in stateless handlers.** Related parser/grammar question from the earlier critique. Separate concern.

4. **D132 materialization refinements.** Self-referential RHS validation (proposed D143) and secondary-expression checks (proposed D144) are separate diagnostic improvements.

5. **Wildcard `from any` interaction with construction.** `from any on <InitialEvent>` is covered by `InitialEventInTransitionRow` — it's a transition row that references the initial event, so it's rejected. No special wildcard logic needed.

6. **C# hover for `EventOutcome.Created` in consuming code.** Hover text for the `Created` case in C# match blocks is a docs/runtime API concern, not language server work. The LS only governs `.precept` files.

---

*End of design.*

---

## 10. Runtime Documentation Deliverables

> **Added 2026-05-15 — Frank (Lead/Architect)**
>
> This section tracks runtime API documentation as a first-class implementation deliverable, not post-implementation cleanup.

### Sections Updated (Design Phase)

The following `docs/runtime/runtime-api.md` sections were updated as part of this design work to reflect locked constructor semantics:

1. **§ Executable Model → Initial Entity** — Complete rewrite. Replaced `Applied`/`Transitioned` construction examples with `Created`. Documented full construction outcome space table. Added fire-once enforcement, hollow-context guard rules, `AlwaysRejecting` severity promotion, and new compiler diagnostics.
2. **§ Precept Design Decisions** — Updated `Construction mirrors operations` and `InitialEvent nullable` bullets to reflect `Created` (not `Applied`) as the construction success outcome.
3. **§ Stateless Precepts — CreateInitialVersion** — Updated contract to reference `Created` instead of `Applied` for construction success.
4. **§ Fire — Row dispatch model** — Added description of `TransitionRow`/`TransitionRowReject` evaluation semantics. Documented how row shape determines outcome at runtime.
5. **§ Inputs and Outputs** — Updated `EventOutcome` variant count from 7 to 9 (accounts for `Faulted` per CC#12 + `Created` per this design).

### Sections Requiring Implementation-Time Updates

Implementers MUST verify and update these runtime doc sections when the corresponding code lands:

| Section | Required update | Gate |
|---------|----------------|------|
| `docs/runtime/result-types.md` | Add `Created` DU variant definition (`sealed record Created(Version Result) : EventOutcome`). Update variant count. Add `Created` to the hierarchy table. | Before `EventOutcome.Created` PR merges |
| `docs/runtime/runtime-api.md` § Fire | If `Fire()` behavior changes due to `TransitionRow`/`TransitionRowReject` split, verify the Fire pipeline description. | Before parser split PR merges |
| `docs/runtime/evaluator.md` | Document construction evaluation path through evaluator (currently evaluator is entirely TODO/stub). | When evaluator construction path is implemented |
| `docs/runtime/runtime-api.md` § Inspection | `InspectCreate` must be verified to show construction-row landscape (which guard matched, which row would fire, what `Created` vs `Rejected` outcome). | When `InspectCreate` is implemented |
| MCP changelog | `EventOutcome.Created` is a wire-contract breaking change for any exhaustive consumer switch. Document in MCP changelog. | Before MCP-facing PR merges |

### Implementer Verification Rule

**No implementation slice is complete until the runtime docs are verified accurate.** Specifically:

- Before merging any PR that adds `EventOutcome.Created`: verify `result-types.md` includes it.
- Before merging any PR that changes `Create()` behavior: verify `runtime-api.md` § Construction matches actual behavior.
- Before merging any PR that changes row dispatch: verify `runtime-api.md` § Fire and the row dispatch model paragraph are accurate.
- Before merging any PR that implements fire-once enforcement: verify the fire-once paragraph in `runtime-api.md` matches the mechanism used.


## 11. Implementation Plan

### 11.1 Implementation Strategy

This feature ships in **12 vertical slices**. Each slice is a shippable increment — it adds value, passes tests, and leaves the build green. Slices are ordered by dependency: earlier slices create the foundation later slices consume.

**Guiding principles:**

- **One thing at a time.** Each slice touches a focused set of files. Minimize cross-cutting changes.
- **Tests first.** Write the failing test, then the code that makes it pass.
- **Regression anchors.** Every slice explicitly lists which existing tests must continue passing.
- **Self-review before done.** Before marking a slice complete, run `dotnet test` and verify no regressions.

---

### 11.2 Review Gate Protocol

Before marking any slice **done**:

1. `dotnet build` — no errors, no new warnings.
2. `dotnet test` — all tests pass, including regression anchors listed for that slice.
3. Self-review diff — no unintended changes, no leftover TODOs.
4. PR body updated — summary and checklist reflect current state.

---

### 11.3 Slice Summary Table

| # | Slice | Files | New Tests | Dependencies | Status |
|---|-------|-------|-----------|--------------|--------|
| 1 | Grammar Foundations | 3 | 0 | — | ✅ Done |
| 2 | Parser Routing | 2 | 6 | 1 | ✅ Done |
| 3 | Semantic Model DU | 4 | 0 | 1 | ✅ Done |
| 4 | Type Checker Structural | 2 | 8 | 2, 3 | ✅ Done |
| 5 | Type Checker Field State | 2 | 6 | 4 | ✅ Done |
| 6 | Graph Analyzer | 2 | 4 | 3, 4 | ✅ Done |
| 7 | Proof Engine | 3 | 4 | 3 |
| 8 | Runtime | 4 | 8 | 3, 6 |
| 9 | Language Server | 3 | 3 | 3 |
| 10 | Grammar Generator | 1 | 2 | 1 |
| 11 | MCP DTO | 1 | 2 | 3 |
| 12 | Docs and Samples | 4 | 0 | all |

**Total: ~43 new tests across 12 slices.**

---

### 11.4 Per-Slice Detail

---

#### Slice 1: Grammar Foundations

**Status:** ✅ Completed (2026-05-16)

**Goal:** Split `ConstructKind` and `ConstructSlotKind` to distinguish construction rows from transition rows, and success outcomes from reject clauses.

**Files:**

| File | Changes |
|------|---------|
| `src/Precept/Language/ConstructKind.cs` | Add `ConstructionRow = 19`, `ConstructionRowReject = 20`, `TransitionRowReject = 21`; rename `EventHandler` → `EventRow` (keep value 12) |
| `src/Precept/Language/ConstructSlot.cs` | Add `RejectClause = 14`, `SuccessOutcome = 15` |
| `src/Precept/Language/ConstructMeta.cs` | Update `Entries` to include new kinds with appropriate `Parent`, `Slots`, `DisambiguationTokens` |

**Methods to add/modify:**

- `ConstructKind` enum — add 3 members, rename 1
- `ConstructSlotKind` enum — add 2 members
- `ConstructMeta.Entries` — add `ConstructMeta` entries for new kinds

**Named tests:** None (enum changes are validated by downstream slices).

**Regression anchors:**

- All existing parser tests in `test/Precept.Tests/Parser/`
- All existing type checker tests in `test/Precept.Tests/TypeChecker/`

---

#### Slice 2: Parser Routing

**Status:** ✅ Completed (2026-05-16)

**Goal:** Remove PRE0014 guard rejection; route `event ... initial` to construction-row constructs; route `reject` clauses to `RejectClause` slot.

**Files:**

| File | Changes |
|------|---------|
| `src/Precept/Pipeline/Parser.cs` | Remove PRE0014 emission (~line 351); detect `initial` modifier and emit `ConstructionRow`/`ConstructionRowReject` kinds; split `Outcome` slot into `RejectClause` vs `SuccessOutcome` |
| `src/Precept/Language/DiagnosticCode.cs` | Retain `EventHandlerDoesNotSupportGuard = 14` but mark obsolete (parser no longer emits it) |

**Methods to add/modify:**

- `Parser.ParseEventHandler()` → detect `initial` modifier, choose `ConstructionRow` vs `EventRow`
- `Parser.ParseOutcome()` → emit `RejectClause` slot for `reject`, `SuccessOutcome` for others
- `Parser.ParseTransitionRow()` → emit `TransitionRowReject` for reject-only rows

**Named tests:**

| Test | Assertion |
|------|-----------|
| `ConstructionRow_EmitsCorrectKind` | `event start initial { ... }` parses to `ConstructKind.ConstructionRow` |
| `ConstructionRowReject_EmitsCorrectKind` | `event start initial when ... reject ...` parses to `ConstructKind.ConstructionRowReject` |
| `ConstructionRow_AllowsGuard` | `event start initial when amount > 0 { ... }` parses without PRE0014 |
| `EventRow_NoInitial_EmitsEventRow` | `event pause { ... }` parses to `ConstructKind.EventRow` |
| `TransitionRowReject_EmitsCorrectKind` | `from idle on start when ... reject ...` parses to `ConstructKind.TransitionRowReject` |
| `RejectClause_EmitsCorrectSlot` | `reject "msg"` outcome parses to `ConstructSlotKind.RejectClause` |

**Regression anchors:**

- `ParserEventHandlerTests` — existing tests that expect `EventHandler` kind (update expectations)
- `ParserTransitionTests` — existing transition row tests

---

#### Slice 3: Semantic Model DU

**Status:** ✅ Completed (2026-07-17)

**Goal:** Redesign `TypedEventRow` and `TypedTransitionRow` as discriminated unions with success/reject subtypes.

**Files:**

| File | Changes |
|------|---------|
| `src/Precept/Pipeline/Model/TypedEventRow.cs` | Convert to abstract record with `TypedEventRowSuccess` and `TypedEventRowReject` subtypes; add `IsConstruction` property |
| `src/Precept/Pipeline/Model/TypedTransitionRow.cs` | Convert to abstract record with `TypedTransitionRowSuccess` and `TypedTransitionRowReject` subtypes |
| `src/Precept/Pipeline/Model/TypedOutcome.cs` | Add `TypedRejectClause` subtype if not present |
| `src/Precept/Pipeline/TypeChecker.cs` | Update row construction to emit correct DU subtypes based on `ConstructKind` |

**Methods to add/modify:**

- `TypedEventRow` → abstract base with `Event`, `Guard`, `IsConstruction` properties
- `TypedEventRowSuccess : TypedEventRow` → adds `Actions`, `Outcome` (success-only)
- `TypedEventRowReject : TypedEventRow` → adds `RejectMessage`
- `TypedTransitionRow` → abstract base with `FromState`, `Event`, `Guard` properties
- `TypedTransitionRowSuccess : TypedTransitionRow` → adds `Actions`, `ToState`, `Outcome`
- `TypedTransitionRowReject : TypedTransitionRow` → adds `RejectMessage`
- `TypeChecker.BuildEventRow()` → switch on `ConstructKind` to emit correct subtype
- `TypeChecker.BuildTransitionRow()` → switch on `ConstructKind` to emit correct subtype

**Named tests:** None (DU shape is validated by downstream slices).

**Regression anchors:**

- All existing `TypeCheckerEventHandlerTests`
- All existing `TypeCheckerTransitionTests`

---

#### Slice 4: Type Checker Structural

**Goal:** Add structural validation for construction semantics — PRE0092 initial-event exception, `InitialEventInTransitionRow`, `ZeroConstructionRows`, `MultipleInitialEvents`.

**Files:**

| File | Changes |
|------|---------|
| `src/Precept/Language/DiagnosticCode.cs` | Add `InitialEventInTransitionRow = 145`, `ZeroConstructionRows = 146`, `MultipleInitialEvents = 147` |
| `src/Precept/Pipeline/TypeChecker.Validation.Structural.cs` | Add initial-event exception to PRE0092 (~line 16-27); add `ValidateConstructionRowStructure()` method |

**Methods to add/modify:**

- `DiagnosticCode` enum — add 3 members
- `TypeChecker.ValidateStatelessEventOnNonStatelessPrecept()` — add exception for `IsConstruction` rows
- `TypeChecker.ValidateConstructionRowStructure()` — emit `InitialEventInTransitionRow` if initial event appears in `TransitionRow`; emit `ZeroConstructionRows` if stateful precept has no construction rows; emit `MultipleInitialEvents` if multiple distinct initial events declared

**Named tests:**

| Test | Assertion |
|------|-----------|
| `PRE0092_AllowsInitialEventOnStatefulPrecept` | `event start initial { ... }` on stateful precept does NOT emit PRE0092 |
| `PRE0145_InitialEventInTransitionRow` | `from idle on start { ... }` where `start` is initial emits PRE0145 |
| `PRE0146_ZeroConstructionRows_Emitted` | Stateful precept with no `event ... initial` emits PRE0146 |
| `PRE0146_ZeroConstructionRows_NotEmitted` | Stateful precept with `event start initial { ... }` does NOT emit PRE0146 |
| `PRE0147_MultipleInitialEvents_Emitted` | `event a initial { ... }` + `event b initial { ... }` emits PRE0147 |
| `PRE0147_MultipleInitialEvents_NotEmitted_SameEvent` | Multiple rows for same initial event does NOT emit PRE0147 |
| `PRE0147_MultipleInitialEvents_NotEmitted_SingleEvent` | Single initial event does NOT emit PRE0147 |
| `ConstructionRow_AllowsGuard_NoError` | `event start initial when x > 0 { ... }` emits no guard-related error |

**Regression anchors:**

- `TypeCheckerStatelessTests.PRE0092_*` — existing PRE0092 tests
- `TypeCheckerConstructionTests` — D93, D94, D142, D144 tests

---

#### Slice 5: Type Checker Field State ✅

**Goal:** Simplify D94 to single-target; add hollow-context validator as ship gate.

**Files:**

| File | Changes |
|------|---------|
| `src/Precept/Pipeline/TypeChecker.Validation.FieldState.cs` | Simplify D94 logic (~lines 340-385) to require single-target; add `ValidateConstructionGuardFieldAccess()` for hollow-context |
| `src/Precept/Language/DiagnosticCode.cs` | Add `ConstructionGuardReadsUninitializedField = 148` |

**Methods to add/modify:**

- `DiagnosticCode` enum — add 1 member
- `TypeChecker.ValidateConstructionGuarantees()` / `GetInvalidRequiredFieldAssignments()` — simplify D94 to single-target validation (every execution path sets each required field exactly once)
- `TypeChecker.ValidateConstructionGuardFieldAccess()` — for each construction `TypedEventRow` (success or reject), check guard expression for field reads; emit PRE0148 if guard reads any field (all fields are uninitialized at construction guard evaluation time)

**Named tests:**

| Test | Assertion |
|------|-----------|
| `D94_SingleTarget_Success` | Construction row with `set total = amount` satisfies D94 |
| `D94_SingleTarget_Failure` | Construction row with `set total = amount` in one branch and no `set total` in another fails D94 |
| `PRE0148_ConstructionGuardReadsField_Emitted` | `event start initial when total > 0 { ... }` emits PRE0148 |
| `PRE0148_ConstructionGuardReadsField_NotEmitted` | `event start initial when amount > 0 { ... }` (payload, not field) does NOT emit PRE0148 |
| `PRE0148_ConstructionGuardReadsField_NotEmitted_NoGuard` | `event start initial { ... }` (no guard) does NOT emit PRE0148 |
| `PRE0148_NotEmitted_TransitionRow` | `from idle on pause when total > 0 { ... }` (transition, not construction) does NOT emit PRE0148 |

**Regression anchors:**

- `TypeCheckerConstructionTests.D94_*` — existing D94 tests (may need adjustment for single-target)
- `TypeCheckerConstructionTests.D93_*` — existing D93 tests

---

#### Slice 6: Graph Analyzer

**Goal:** Make graph analyzer aware of construction rows; promote `AlwaysRejecting` to error for construction rows.

**Files:**

| File | Changes |
|------|---------|
| `src/Precept/Pipeline/GraphAnalyzer.cs` | Extend `EmitAlwaysRejecting()` (~lines 721-747) to cover `TypedEventRowSuccess`/`TypedEventRowReject` where `IsConstruction`; promote severity from warning to error for construction rows |
| `src/Precept/Language/DiagnosticCode.cs` | (No new codes — reuse `AlwaysRejecting` with severity promotion) |

**Methods to add/modify:**

- `GraphAnalyzer.EmitAlwaysRejecting()` — extend pattern match to include `TypedEventRowSuccess` and `TypedEventRowReject` where `IsConstruction`; use `DiagnosticSeverity.Error` for construction rows (unprovable guard means precept can never be created)
- `GraphAnalyzer.AnalyzeReachability()` — ensure construction rows are included in reachability analysis

**Named tests:**

| Test | Assertion |
|------|-----------|
| `AlwaysRejecting_ConstructionRow_IsError` | `event start initial when false { ... }` emits `AlwaysRejecting` with `Severity.Error` |
| `AlwaysRejecting_ConstructionRow_NotEmitted` | `event start initial when amount > 0 { ... }` does NOT emit `AlwaysRejecting` |
| `AlwaysRejecting_TransitionRow_IsWarning` | `from idle on pause when false { ... }` emits `AlwaysRejecting` with `Severity.Warning` (existing behavior) |
| `ConstructionRow_IncludedInReachability` | Construction row with actions is included in reachability graph |

**Regression anchors:**

- `GraphAnalyzerAlwaysRejectingTests` — existing `AlwaysRejecting` tests
- `GraphAnalyzerReachabilityTests` — existing reachability tests

---

#### Slice 7: Proof Engine

**Goal:** Add guard-aware proof strategies for construction-row DU.

**Files:**

| File | Changes |
|------|---------|
| `src/Precept/Pipeline/ProofEngine.cs` | Add `EventRowContext` to context types handled by proof strategies |
| `src/Precept/Pipeline/ProofEngine.Strategies.cs` | Extend guard reading to handle `EventRowContext` |
| `src/Precept/Pipeline/ProofEngine.Intervals.cs` | Extend interval extraction to handle `EventRowContext` |

**Methods to add/modify:**

- `ProofEngine.BuildContext()` — add case for `TypedEventRowSuccess` → `EventRowContext`
- `ProofEngine.Strategies.ReadGuardConstraints()` — extend pattern match to include `EventRowContext`
- `ProofEngine.Intervals.ExtractIntervals()` — extend to handle `EventRowContext` guard expressions

**Named tests:**

| Test | Assertion |
|------|-----------|
| `ProofEngine_ConstructionRow_GuardExtractsConstraints` | `event start initial when amount > 0 { ... }` extracts `amount > 0` constraint |
| `ProofEngine_ConstructionRow_GuardIntervalsCorrect` | `event start initial when amount >= 10 and amount <= 100 { ... }` extracts interval `[10, 100]` |
| `ProofEngine_ConstructionRow_NoGuard_NoConstraints` | `event start initial { ... }` extracts no constraints |
| `ProofEngine_ConstructionRow_IntegrationWithValidator` | End-to-end: construction guard constraint flows to proof obligation validation |

**Regression anchors:**

- `ProofEngineStrategyTests` — existing strategy tests
- `ProofEngineIntervalTests` — existing interval tests

---

#### Slice 8: Runtime

**Goal:** Implement `EventOutcome.Created`, `Precept.Create()`, fire-once enforcement, and `Version.AvailableEvents` filtering.

**Files:**

| File | Changes |
|------|---------|
| `src/Precept/Runtime/EventOutcome.cs` | Add `Created` DU case |
| `src/Precept/Runtime/Precept.cs` | Implement `Create()` method (~line 53) |
| `src/Precept/Runtime/Version.cs` | Implement `AvailableEvents` filtering (~line 42) |
| `src/Precept/Runtime/Evaluator.cs` | Add fire-once enforcement for initial events |

**Methods to add/modify:**

- `EventOutcome` DU — add `Created : EventOutcome` record
- `Precept.Create(EventPayload)` → evaluate construction rows for matching initial event; return `EventOutcome.Created` on success, `EventOutcome.Rejected` on guard failure
- `Version.AvailableEvents` → exclude initial events from post-construction versions
- `Evaluator.Fire()` → for initial events, check if already fired and reject; update internal state to track fired initial events

**Named tests:**

| Test | Assertion |
|------|-----------|
| `EventOutcome_Created_Exists` | `EventOutcome.Created` is a valid DU case |
| `Precept_Create_ReturnsCreated` | `Precept.Create(payload)` returns `EventOutcome.Created` on success |
| `Precept_Create_ReturnsRejected` | `Precept.Create(payload)` returns `EventOutcome.Rejected` when guard fails |
| `Precept_Create_SetsInitialState` | After `Create()`, precept is in initial state with fields set |
| `Version_AvailableEvents_ExcludesInitial` | `version.AvailableEvents` does not include initial events |
| `Version_AvailableEvents_IncludesNonInitial` | `version.AvailableEvents` includes non-initial events |
| `Fire_InitialEvent_FireOnce_Enforced` | Firing initial event twice returns `Rejected` |
| `Fire_InitialEvent_FireOnce_FirstSucceeds` | First firing of initial event succeeds |

**Regression anchors:**

- `RuntimeFireTests` — existing fire tests
- `RuntimeVersionTests` — existing version tests
- `EvaluatorTests` — existing evaluator tests

---

#### Slice 9: Language Server

**Goal:** Update completions, hover, and semantic tokens for construction row syntax.

**Files:**

| File | Changes |
|------|---------|
| `tools/Precept.LanguageServer/Completions/CompletionProvider.cs` | Add `initial` modifier completion after `event` keyword |
| `tools/Precept.LanguageServer/Hover/HoverProvider.cs` | Add hover text for `initial` modifier |
| `tools/Precept.LanguageServer/SemanticTokens/SemanticTokensProvider.cs` | Add semantic token type for `initial` modifier |

**Methods to add/modify:**

- `CompletionProvider.GetEventCompletions()` — include `initial` modifier in completion list
- `HoverProvider.GetHoverText()` — return construction-row explanation when hovering over `initial`
- `SemanticTokensProvider.ClassifyToken()` — classify `initial` as modifier semantic token

**Named tests:**

| Test | Assertion |
|------|-----------|
| `Completions_Event_IncludesInitial` | Completions after `event foo ` include `initial` |
| `Hover_InitialModifier_ReturnsText` | Hover over `initial` in `event foo initial` returns descriptive text |
| `SemanticTokens_InitialModifier_Classified` | `initial` in `event foo initial` is classified as modifier |

**Regression anchors:**

- `CompletionProviderTests` — existing completion tests
- `HoverProviderTests` — existing hover tests
- `SemanticTokensProviderTests` — existing semantic token tests

---

#### Slice 10: Grammar Generator

**Goal:** Update grammar generator to emit construction row patterns.

**Files:**

| File | Changes |
|------|---------|
| `tools/Precept.VsCode/GrammarGen/Program.cs` | Add emission for `event ... initial` pattern |

**Methods to add/modify:**

- `GrammarGen.EmitEventHandlerPatterns()` — add pattern for `event <name> initial` and `event <name> initial when <guard>`

**Named tests:**

| Test | Assertion |
|------|-----------|
| `Grammar_EventInitial_Highlighted` | `event start initial` is syntax-highlighted correctly |
| `Grammar_EventInitialGuard_Highlighted` | `event start initial when x > 0` is syntax-highlighted correctly |

**Regression anchors:**

- `GrammarGenTests` — existing grammar generation tests
- Manual verification: open `.precept` file in VS Code, verify highlighting

---

#### Slice 11: MCP DTO

**Goal:** Update MCP compile tool DTOs to expose construction row information.

**Files:**

| File | Changes |
|------|---------|
| `tools/Precept.Mcp/Tools/CompileTool.cs` | Add `isConstruction` property to event row DTO |

**Methods to add/modify:**

- `CompileTool.EventRowDto` → add `bool IsConstruction` property
- `CompileTool.MapEventRow()` → populate `IsConstruction` from `TypedEventRow.IsConstruction`

**Named tests:**

| Test | Assertion |
|------|-----------|
| `CompileTool_EventRow_IsConstruction_True` | `event start initial { ... }` returns `isConstruction: true` |
| `CompileTool_EventRow_IsConstruction_False` | `event pause { ... }` returns `isConstruction: false` |

**Regression anchors:**

- `CompileToolTests` — existing MCP compile tests

---

#### Slice 12: Docs and Samples

**Goal:** Update documentation and fix sample file.

**Files:**

| File | Changes |
|------|---------|
| `docs/language/precept-language-spec.md` | Add construction row syntax section |
| `docs/runtime/runtime-api.md` | Document `EventOutcome.Created`, `Precept.Create()`, fire-once behavior |
| `samples/Test.precept` | Fix illegal post-construction use of initial event |
| `CHANGELOG.md` | Add constructor semantics feature entry |

**Methods to add/modify:** N/A (documentation only).

**Named tests:** None (documentation changes).

**Regression anchors:**

- `samples/Test.precept` should parse without errors after fix
- All documentation should render correctly in markdown viewers

---

### 11.5 Dependency Graph

```
Slice 1 (Grammar Foundations)
    │
    ├──► Slice 2 (Parser Routing)
    │        │
    │        └──► Slice 4 (Type Checker Structural)
    │                 │
    │                 └──► Slice 5 (Type Checker Field State)
    │
    └──► Slice 3 (Semantic Model DU)
             │
             ├──► Slice 4 (Type Checker Structural)
             │
             ├──► Slice 6 (Graph Analyzer)
             │        │
             │        └──► Slice 8 (Runtime)
             │
             ├──► Slice 7 (Proof Engine)
             │
             ├──► Slice 9 (Language Server)
             │
             └──► Slice 11 (MCP DTO)

Slice 1 ──► Slice 10 (Grammar Generator)

All Slices ──► Slice 12 (Docs and Samples)
```

---

### 11.6 File Inventory Verification

Cross-reference against §8 Implementation Scope:

| §8 File | Covered In |
|---------|------------|
| `ConstructKind.cs` | Slice 1 |
| `ConstructSlot.cs` | Slice 1 |
| `ConstructMeta.cs` | Slice 1 |
| `DiagnosticCode.cs` | Slices 2, 4, 5 |
| `Parser.cs` | Slice 2 |
| `TypedEventRow.cs` | Slice 3 |
| `TypedTransitionRow.cs` | Slice 3 |
| `TypedOutcome.cs` | Slice 3 |
| `TypeChecker.cs` | Slice 3 |
| `TypeChecker.Validation.Structural.cs` | Slice 4 |
| `TypeChecker.Validation.FieldState.cs` | Slice 5 |
| `GraphAnalyzer.cs` | Slice 6 |
| `ProofEngine.cs` | Slice 7 |
| `ProofEngine.Strategies.cs` | Slice 7 |
| `ProofEngine.Intervals.cs` | Slice 7 |
| `EventOutcome.cs` | Slice 8 |
| `Precept.cs` | Slice 8 |
| `Version.cs` | Slice 8 |
| `Evaluator.cs` | Slice 8 |
| `CompletionProvider.cs` | Slice 9 |
| `HoverProvider.cs` | Slice 9 |
| `SemanticTokensProvider.cs` | Slice 9 |
| `GrammarGen/Program.cs` | Slice 10 |
| `CompileTool.cs` | Slice 11 |
| `precept-language-spec.md` | Slice 12 |
| `runtime-api.md` | Slice 12 |
| `samples/Test.precept` | Slice 12 |

**All files from §8 are covered.**

---

### 11.7 Open Item Resolutions

#### MultipleInitialEvents Diagnostic Code

**Decision:** Assign `MultipleInitialEvents = 147` (new code).

**Rationale:** `MultipleInitialStates` (PRE0031) covers states, not events. Initial events are a distinct concept — a precept can have multiple construction rows for the same initial event (with different guards), but cannot have rows for multiple distinct initial events. This requires a dedicated diagnostic code.

**Related codes assigned:**
- `InitialEventInTransitionRow = 145`
- `ZeroConstructionRows = 146`
- `MultipleInitialEvents = 147`
- `ConstructionGuardReadsUninitializedField = 148`

#### Hollow-Context Validator Ship Gate

**Decision:** **Option A (ship gate)** — the hollow-context validator is a non-negotiable ship gate.

**Rationale:**

1. **Core guarantee:** The feature's value proposition is compile-time construction safety. If construction guards can read uninitialized fields, the precept silently evaluates `default(T)` values at runtime — violating the guarantee.

2. **Runtime fault:** Reading an uninitialized field in a construction guard is a runtime fault, not merely a style issue. The proof engine cannot reason about field values that don't exist yet.

3. **No partial safety:** Shipping construction semantics without this validator would be like shipping null safety with a hole. Users would reasonably expect that if Precept accepts their construction row, the guard is safe.

4. **Implementation cost:** The validator is straightforward — walk the guard expression AST, collect field references, emit PRE0148 for each. Estimated: ~50 lines of code.

**Conclusion:** Slice 5 includes the hollow-context validator. The feature does not ship without it.

---

### 11.8 Implementer Checklist

Before marking the feature **complete**:

- [ ] All 12 slices marked done
- [ ] `dotnet test` passes with 0 failures
- [ ] No new compiler warnings
- [ ] PR body `## Summary` and `## Implementation Plan` are current
- [ ] `samples/Test.precept` parses without errors
- [ ] Language spec documents construction row syntax
- [ ] Runtime API documents `EventOutcome.Created` and `Precept.Create()`
- [ ] MCP compile tool exposes `isConstruction` on event rows
- [ ] Grammar generator emits correct patterns for `event ... initial`
