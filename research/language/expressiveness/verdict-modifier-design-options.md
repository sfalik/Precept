# Verdict Modifier Design Options — All Tiers

**Research date:** 2026-04-11  
**Author:** Frank (Lead/Architect & Language Designer)  
**Batch:** Expressiveness expansion — design options exploration  
**Context:** Shane's decisions (proceed=yes, scope=all-tiers, non-blocking-warnings question open, state-verdict=opportunity, timeline=research-mode)  
**Relevance:** Concrete design options for introducing verdict modifiers (success/warning/error intent) at events, rules, and states. Explores syntax, compile-time checks, runtime behavior, philosophy alignment, and cross-tier interaction to ground proposal discussion.

This file presents 2-3 concrete design options for each tier, with trade-off analysis and philosophy grounding. It is distinct from the background research (`verdict-modifiers.md`) — this document answers "what would it look like?" not "how do comparable systems do it?"

---

## 1. Design Options for Event Verdicts

Events are the **strongest candidate** for verdict modifiers. All three options below are compile-time provable from declared transition rows.

### Option A: Outcome-shape declaration (Recommended Foundation)

Declare the expected outcome category on the event keyword:

```precept
event Approve success
event Deny error
event Escalate warning

# Transition rows must produce consistent outcomes:
from Draft on Approve -> transition Approved  # ✓ Transition = success
from Open on Deny -> reject "Denied early"    # ✓ Rejected = error
from Pending on Escalate 
    if urgent -> transition InReview           # ✗ Mixed: success+warning invalid
```

**Syntax:**
- `event <Name> success | error | warning` — declares expected outcome severity
- Default: none (no declared intent, no compile check)

**Compile-time checks:**
- `success` event: all transition rows must produce `Transition` or `NoTransition` outcomes
- `error` event: all transition rows must produce `Rejected` or `ConstraintFailure` outcomes  
- `warning` event: allows mixed outcomes (no enforcement — warning is transitional intent)
- Diagnostic: **NEW C58: "Event <Name> declared success but row produces <outcome>"** (error)
- Diagnostic: **NEW C59: "Event <Name> declared error but row produces <outcome>"** (error)

**Runtime behavior:** No change. Verdicts are authoring-time intent only.

**Tooling impact:**
- Diagram: color edges emerald (success), rose (error), amber (warning) by event verdict + actual outcome
- Completions: suggest expected outcome shapes based on event verdict
- MCP `precept_fire`: include `eventVerdict` in response metadata for tracing authoring intent
- Linting: warn if diagnostic code suppressed (deliberately ignoring intent mismatch)

**Philosophy alignment:**
- **Principle #1 (Determinism):** No new runtime behavior — verdict is purely declared intent without changing evaluation ✓
- **Principle #3 (Minimal ceremony):** Simple keyword placement, no new block structure ✓
- **Principle #11 (Arrow clarity):** Verdict sits on event, not arrow; keeps context/action separation clear ✓
- **Principle #12 (AI authoring):** Catalog-driven; predictable structure; MCP queryable ✓

**Trade-offs:**
- ✓ Strong precedent: FluentVation, BPMN, Roslyn all have event-level severity
- ✓ Enables strongest diagnostic category C58/C59
- ✓ Supports intent discovery — readers see at a glance what kind of event this is
- ✗ Requires author discipline — no warning if row is forgotten (covered by C50 separately)
- ✗ Mixed-outcome events (`warning`) have weak semantics — warning is "allowed but not enforced"

**Variant A2: No default (strict mode)**  
If desired, require every event to be declared with a verdict. **Rejected:** Adds mandatory ceremony; plain events in samples become clutter.

---

### Option B: Per-row outcome override (External Configuration Model)

Separate event declaration from outcome enforcement via a second configuration artifact (like ESLint's `.eslintrc` model):

```precept
event Approve
event Deny
event Escalate

# In precept definition: all rows treated as neutral — no verdict declared
from Draft on Approve -> transition Approved
from Open on Deny -> reject "Denied" when !hasExemption
```

External configuration (hypothetical `.preceptrc`):
```yaml
eventVerdicts:
  Approve: success
  Deny:
    when: hasExemption
    verdict: warning    # Warn instead of block if exemption exists
    else: error
  Escalate: warning
```

**Syntax:** No DSL changes; configuration file driven.

**Compile-time checks:** None — verdicts are runtime-applied.

**Runtime behavior:** 
- Event fire still produces TransitionOutcome (Transition, Rejected, ConstraintFailure, etc.)
- Engine looks up configured verdict; if binding exists, overlays it for tooling/diagnostics
- Non-blocking warning: configured verdict=warning means event fire succeeds even if row would reject

**Tooling impact:**
- Diagram: colors driven by configuration, not declaration
- Completions: no verdict hints in editor
- MCP: `precept_fire` includes both `actualOutcome` and `configuredVerdict`

**Philosophy alignment:**
- **Principle #1 (Determinism):** Adds runtime context (configuration) — violates determinism unless state is captured ✗
- **Principle #2 (English-ish):** Configuration file is not English-ish ✗
- **Principle #9 (Tooling drives syntax):** Good fit for global tuning, but Precept targets non-programmers — YAML breaks immersion ✗

**Trade-offs:**
- ✓ Deployment flexibility (same event, different verdicts in dev/prod)
- ✓ Cleanest non-blocking warning model (configuration layer decides)
- ✗ One-file incompleteness (configuration split across two artifacts)
- ✗ Not suitable for Precept's non-programmer audience

**Verdict:** REJECTED for Precept. See "Architectural Decision" section.

---

### Option C: Outcome-shape suggestion (Weak guidance mode)

Declare event verdict as a hint, not an error:

```precept
event Approve success
event Deny error

from Open on Deny -> transition ToClosing  # ⚠ Mismatch: "Deny error" but row says transition
```

**Compile-time checks:**
- Mismatch produces **C60 (hint):** "Event declared <verdict> but row produces <outcome>" — non-blocking suggestion
- Intent still queryable by tooling; author can suppress with comment

**Runtime behavior:** No change.

**Trade-offs:**
- ✓ No broken samples during rollout
- ✗ Weak signal — hints are easy to ignore or accidentally suppress
- ✗ Philosophy gap: declaration becomes documentation, not contract

**Verdict:** REJECTED. Precept's philosophy demands strong checking; weak hints don't fit "prevention, not detection."

---

**RECOMMENDED: Option A (Outcome-shape declaration).** Provides the strongest combination of compile-time provability, one-file completeness, and non-programmer clarity. Precedent is broad; philosophy alignment is strong.

---

## 2. Design Options for Rule Verdicts

Rules (invariants, asserts, and field constraints) present a different challenge than events: they evaluate to true/false, and the question of severity forces a fundamental architectural choice. **This is where the non-blocking warning question becomes critical.**

### The Non-Blocking Warning Question (THE HARDEST DESIGN QUESTION)

**Shane's statement:** "If we are going to do this, then non-error rules should become non-blocking."

**The tension:** Precept's core guarantee is "prevention, not detection" — invalid configurations cannot exist. But if a warning-level rule fails, do we:

1. **Block anyway (FluentValidation/Kubernetes model):** Warning failed? The operation still rejects. Severity is metadata for display, not enforcement. ✓ Maintains "prevention" guarantee, ✗ Makes "warning" confusing (warnings block).
2. **Allow through (ESLint/BPMN model):** Warning failed? The operation succeeds with a flag. ✗ Violates "prevention" — invalid config exists, ✗ Breaks philosophy, ✓ Makes "warning" intuitive.
3. **New outcome category (neither Rejected nor success):** Operation produces a third result kind (Warning outcome). ✗ Triples the outcome space, ✓ Precise semantics. 

**This is genuinely hard because Precept's philosophy was written with binary (prevent/not) in mind, not graduated severity.**

---

### Option A1: Constraint-level severity (Declaration-level, blocking model)

```precept
# Error-level constraints (default)
invariant Balance >= 0 because "Balance must be non-negative"

# Warning-level constraints (explicit)
invariant Notes.length < 500 because "Notes should be concise" warning
to Submitted assert Items.count > 0 because "Must have items" warning

field CreditScore number min 300  # Error-level by default
field ExpirationDate string warning  # Warning-level
```

**Semantics: Warnings BLOCK (maintain prevention guarantee)**

When a warning-level constraint fails:
- Engine still rejects the operation (like error-level)
- But diagnostics show it as a warning/concern-level issue, not a critical failure
- Tooling renders it in amber instead of rose
- Preview shows "Warning: operation blocked" instead of "Error: operation rejected"

**Syntax:**
- `invariant <expr> because <msg> warning` — optional `warning` suffix
- `to <State> assert <expr> because <msg> warning` — same pattern
- Field constraints: `field Name type warning` modifier (same as `writeonce`, `nullable`)

**Compile-time checks:**
- **C61 (consistency):** "Field <Name> has warning constraint but event <Event> row doesn't handle warning outcome" — guides author to inspect expected behavior
- No blocking forces for warnings; they coexist with errors in the same operation

**Runtime behavior:**
- Same as current: constraint fails → operation rejected, no commit
- Severity attached to failure result; tooling reads it to color the error

**Tooling impact:**  
- Preview: show warnings in amber overlay, errors in rose
- Inspector: separate "Warnings" and "Errors" sections in UI
- MCP `precept_fire`: ConstraintFailure now carries severity metadata (`severity: "error" | "warning"`)

**Philosophy alignment:**
- **Principle #1 (Prevention):** Still prevention — warnings block operation ✓
- **Core guarantee maintained:** Invalid configurations still structurally impossible ✓  
- **Principle #6 (Collect-all for validation):** Warnings still collected with errors ✓

**Trade-offs:**
- ✓ No philosophy gap — prevention guarantee survives
- ✓ Fits "warning" intuition for domain experts
- ✓ Warnings are author intent, not runtime behavior twist
- ✗ "Warning that blocks" is semantically odd (conflates intent-level with enforcement-level)
- ✗ Two constraint types (error/warning) require separate authoring but same outcome

**Non-blocking question resolution: RESOLVED via blocking semantics.** Warnings don't cause non-blocking behavior; they cause same blocking with different severity labeling.

---

### Option A2: Constraint-level severity (Declaration-level, non-blocking model — CONTROVERSIAL)

```precept
# Same syntax as A1, but different semantics
invariant Balance >= 0 because "Balance must be non-negative"  # Blocks
invariant Notes.length < 500 because "Notes concise" warning   # Allows through with flag

to Submitted 
    assert Items.count > 0 because "Need items" warning    # Non-blocking
```

**Semantics: Warnings DO NOT BLOCK (violate prevention guarantee)**

When a warning-level constraint fails:
- Engine allows the operation to succeed (no rejection)
- Result is `Success` (or new `SuccessWithWarnings` outcome kind)
- Failure is recorded as metadata/annotation
- Client can inspect warnings post-success

**Syntax:** Same as A1 (just different semantics).

**Compile-time checks:**
- **C62 (path analysis):** "Warning constraint <X> can fail on all paths in state <S>" — inform, not error
- **C63 (coverage):** "No warning handler for <constraint>" — optional checker

**Runtime behavior:**
- Constraint fails: check severity
- If error: reject (current behavior)
- If warning: still execute, append warning metadata to result

**Philosophy alignment:**
- **Principle #1 (Prevention):** VIOLATED ✗ — invalid configurations can exist
- **Core guarantee:** BROKEN ✗ — "invalid configurations structurally impossible" becomes false for warnings
- **Philosophical question:** Can we reframe Precept's core guarantee to allow graduated severity? This is a PHILOSOPHY-LEVEL DECISION, not a design-level one.

**Trade-offs:**
- ✓ Non-blocking warnings are intuitive (BPMN, ESLint, Kubernetes all do this)
- ✓ Fits "escalation" use case: hint-level issues that should be visible but not blocking
- ✗ BREAKS core philosophy — prevents the product's primary guarantee
- ✗ Requires substantial runtime changes (new outcome kind or field on existing outcome)
- ✗ Forces a philosophy refresh decision before implementation

**Non-blocking question resolution: EXPLICIT PHILOSOPHY GAP.** This option works technically but requires Shane/owner sign-off that we're OK weakening "prevention, not detection." 

**RECOMMENDATION: FLAG FOR PHILOSOPHY DISCUSSION.** This is too big for architectural review. If Shane wants non-blocking warnings, Precept's philosophy document needs to be revised first to justify the guarantee downgrade.

---

### Option B: Shared constraint with per-guard severity (No new syntax)

Keep the current single `invariant`/`assert` surface but add a severity layer to *guard conditions only*:

```precept
# Current (no change):
invariant Balance >= 0 because "Balance must be non-negative"

# New optional guard severity:
invariant Balance >= 0 when MinimumAccountAge > 6months because "Mature accounts strictly non-negative" warning
invariant Balance >= 0 because "Balance must be non-negative"  # When no guard: error-level (default)
```

**Semantics:** Constraint severity = severity of its guard (if present), else error-level. This lets the author attach "this rule is optional/warning-level only for entities matching <condition>."

**Syntax:** No new keywords; uses existing `when` guard with implicit severity hierarchy.

**Compile-time checks:** None new (uses existing C42/C43 guard evaluation).

**Runtime behavior:** 
- If guard is false → constraint skipped (not evaluated)
- If guard is true and main expression false → block with error severity (not warning)
- The guard itself cannot be warning-level (it gates whether the constraint applies)

This does NOT solve the non-blocking question; it just lets you make constraints conditional on status without using separate keywords.

**Trade-offs:**
- ✓ Minimal syntax — no new keywords needed
- ✗ Doesn't actually provide warning semantics — guards skip evaluation, don't downgrade severity
- ✗ Conflates "don't apply this constraint" (guard false) with "apply as warning" (guard true)

**Verdict:** NOT ADEQUATE. This is a workaround, not a solution. Doesn't answer the non-blocking question.

---

**CRITICAL DECISION GATE:** The choice between A1 (warnings block) and A2 (warnings don't block) determines whether Precept's core philosophy survives intact. This is the "non-blocking warning question" that Shane flagged as open and critical.

**RECOMMENDATION: A1 (blocking warnings) is the safe path that maintains philosophy.** A2 requires philosophy refresh. If Shane wants A2, flag the decision to philosophy review before proceeding.

---

## 3. Design Options for State Verdicts

States are **genuinely novel territory.** No comparable system annotates states with success/failure severity. This is both opportunity and risk.

### Option A: Endpoint categorization (Structural + Visual)

```precept
state Approved success
state Denied error  
state Closed success
state OnHold warning

state Draft         # No modifier = neutral/transient state
state InProgress    # No modifier = neutral/transient state
state Cancelled error
```

**What it declares:**
- Success states are lifecycle endpoints that represent positive outcomes
- Error states are endpoints that represent failures/terminal rejection
- Warning states are endpoints with concerns/attention needed
- Unmodified states are transient positions (not endpoints)

**Syntax:** 
- `state <Name> success | error | warning` — optional endpoint verdict modifier
- Default: no modifier (neutral, transient state)

**Compile-time checks:**
- **C64 (structure):** "State declared success but has outgoing transition rows" (warning, not error — terminal states may have administrative exits)
- **C65 (reachability):** "Success state <S> unreachable from initial state" (warning)
- **C66 (coverage for stateful):** "Precept has transitions but no declared success or error states" (hint)
- **C67 (path analysis):** "No path from initial to any success state" (warning) — can you succeed?

**Runtime behavior:** No behavioral change. Verdicts are purely authoring intent + tooling.

**Tooling impact:**
- **Diagram:** 
  - Success states: green border, filled interior (emerald)
  - Error states: red border, filled interior (rose)
  - Warning states: amber border, cross-hatch or fill (amber)
  - Transient states: violet border, hollow interior (neutral)
- **Inspector:** Show reachability to success/error endpoints explicitly
- **MCP `precept_language`:** Include `verdict` field on state metadata
- **Timeline view:** Show path to success/error/warning endpoints with color coding
- **Narrative docs:** Automatically highlight terminal states in .precept documentation

**Philosophy alignment:**
- **Principle #1 (Determinism):** No new runtime behavior ✓
- **Principle #3 (One-file completeness):** All intent visible in `.precept` ✓
- **Novel territory:** Zero comparable systems do this. Opportunity for differentiation, but requires strong justification ⚠

**Trade-offs:**
- ✓ Differentiator: No other state machine DSL provides this — genuinely novel
- ✓ Pedagogical value: Readers see lifecycle shape at a glance
- ✓ Path validation: C65–C67 enable new compile-time analysis (reachability to endpoints)
- ✗ No structural verification: Can't prove "every path leads to success or error" from declared graph alone (guard-dependent)
- ✗ Doesn't interact with events: How does a `success` event arriving at an `error` state behave? Diagnostic only?

**Value proposition for endpoint categorization:**
- **Readability:** Readers see lifecycle intent without reading all rows
- **Diagnostics:** New reachability checks specific to endpoint strategy
- **Visualization:** Diagrams become more semantically legible (endpoints are visually distinct)
- **But:** Does NOT enable new runtime behavior or prevention guarantees. Purely visual/diagnostic.

---

### Option B: State-as-guard on constraint evaluation (Runtime Severity)

```precept
state Approved success
state Denied error

invariant Balance >= 0 because "Non-negative balance required for success" for Approved success
invariant Balance >= 0 because "Non-negative also required for failed cases" for Denied error
```

Extended syntax (hypothetical): Constraint can declare which states it "concerns":

```precept
#NEW: Advanced form — constraint metadata (not yet in grammar)
invariant Balance >= 0 because "Core invariant" applies-to success states
```

**Semantics:** Same constraint, but engine tracks which states trigger it:
- When evaluating at Approved: treat as `success`-level (if it fails, Approved transition was wrong)
- When evaluating at Denied: treat as `error`-level (if it fails, Denied state violation)

**Syntax:** Constraint gains optional `for <StateVerdict>` clause directing severity interpretation.

**Compile-time checks:** 
- **C68:** "Constraint marked for success but evaluates true at error state" (warning)

**Runtime behavior:**
- Verdict lookup: when constraint fails at state S, check if S has success/error verdict
- Applies verdict to ConstraintFailure (ConstraintFailure now carries state-context)

**Trade-offs:**
- ✓ Couples constraints to state verdicts (strong semantic binding)
- ✗ Adds constraint clauses (ceremony increase)
- ✗ Unclear semantics: is this "constraint only applies at success states" or "constraint severity changes based on state"? Confusing.
- ✗ Violates Principle #7 (self-contained rows) if constraints become state-aware

**Verdict:** TOO COMPLEX. Option A provides the same intent-declaration benefit without coupling constraints to states.

---

### Option C: Terminal success vs terminal error (Structural shorthand)

```precept
state Approved terminal success
state Denied terminal error
state OnHold terminal warning

state InProgress  # Transient, not terminal; implicit no verdict
```

**Shorthand:** Combine two modifiers (`terminal` from structural modifiers + new `success`/`error`/`warning`).

**Semantics:** Terminal + verdict means "lifecycle endpoint with explicit outcome category."

**Compile-time checks:** Already covered by structural-modifiers research + new endpoint checks.

**Trade-offs:**
- ✓ Aligns with `terminal` structural modifier (familiar pattern)
- ✓ Concise syntax
- ✗ Not all endpoints are terminal; some warn but allow navigation
- ✗ Mixes structural concerns with semantic intent

**Verdict:** COMBINATION APPROACH POSSIBLE (see cross-tier section).

---

**RECOMMENDED: Option A (Endpoint categorization).** Provides maximum pedagogical value + new diagnostic opportunities without runtime complexity. Treat state verdict as orthogonal from structural modifiers (can combine as needed in Option C form).

---

## 4. Cross-Tier Interaction

How do event, rule, and state verdicts compose?

### Interaction 1: Event Verdict + Rule Verdict

**Scenario:** Event declared `success` but a rule violation occurs during the transition.

```precept
event Approve success        # "Approve should succeed"
from Draft on Approve 
    -> transition Approved

invariant Balance >= 0 because "Balance non-negative"
```

**Behavior:**
- Rule still evaluates (collect-all for validation)
- Rule fails with error severity
- Event's declared success verdict is advisory—diagnostics note the mismatch (C69: "Event Approve declared success but triggered rule failure")
- No override: rule failure still rejects

**Diagnostic code:** **C69: "Event <Event> with declared <outcome> verdict produced constraint violation"** (warning, not error).

**Philosophy:** Event verdict doesn't override rule enforcement. It's intent metadata, not law.

---

### Interaction 2: State Verdict + Event Verdict

**Scenario:** Event produces `success` but target state is declared `error`.

```precept
event Approve success
state Denied error

from OnReview on Approve -> transition Denied  # Success event → error state
```

**Compile-time check:** **C70: "Event Approve declared success but transitions to error state"** (warning). Mismatch is flagged; author may have intended different state verdict or event verdict.

**Philosophy:** State verdict and event verdict should align but don't enforce strict pairing. They're both intent declarations; misalignment is informative but not blocking.

---

### Interaction 3: Rule Context + State Verdict

**Scenario:** Constraint scoped to a state with particular verdict.

```precept
state Approved success
state OnHold warning

in Approved assert Items.count > 0 because "Approved must have items"  # Success context
in OnHold assert Items.count > 0 because "OnHold should review items" warning  # Warning context
```

Current grammar doesn't support per-assert severity, but future work might:

```precept
in Approved assert Items.count > 0 because "..." for success
in OnHold assert Items.count > 0 because "..." for warning
```

This is ADVANCED (future work, not Tier 1).

---

### Consistency Constraints

**Principle:** Event and state verdicts should tell a coherent story. Mismatches are warnings, not errors.

| Event | Target State | Consistency | Diagnostic |
|---|---|---|---|
| success | success | ✓ Aligned | None |
| success | error | ⚠ Mismatch | C70 (warning) |
| error | success | ⚠ Mismatch | C70 (warning) |
| error | error | ✓ Aligned | None |
| warning | any | ⚠ Advisory | C71 (hint) |

**Philosophy:** Verdicts are separate declarations. Inconsistency doesn't block; it informs.

---

## 5. The Critical Non-Blocking Warnings Issue (Phil Summary)

**Shane's question:** Should non-error rules be non-blocking?

**The philosophical problem:** Precept's core guarantee is "invalid configurations cannot exist." If a warning-level constraint fails but the operation succeeds, then an "invalid" configuration (by the constraint's own expression) exists.

**Three ways to resolve:**

**Path 1 (A1 Recommended): Warnings still block, but are styled as concerns.**
- Warning constraint fails → operation rejected (same as error)
- Severity is purely visual/diagnostic metadata
- Prevention guarantee intact
- "Warning" means "less critical error"
- ✓ Maintains philosophy, ✓ Simple, ✗ Confusing semantics

**Path 2 (A2 Controversial): Warnings don't block; we revise the philosophy.**
- Warning constraint fails → operation can succeed with annotation
- Requires philosophy refresh: reframe guarantee as "error-level prevention, warning-level guidance"
- Non-blocking semantics are intuitive (BPMN, Kubernetes precedent)
- ✗ Breaks existing guarantee, ✓ More flexible, ⚠ Requires owner decision

**Path 3 (New outcome kind): Introduce third outcome category.**
- Warning constraint fails → new outcome (not Rejected, not Success, but "SuccessWithWarnings")
- Client can inspect warnings post-call
- Unproven in Precept (would double runtime complexity)
- ✓ Precise semantics, ✗ Significant runtime investment

**CRITICAL DECISION GATE:** Path 1 vs Path 2 is a PHILOSOPHY DECISION, not architectural. Frank recommends Path 1 (maintain philosophy). If Shane prefers Path 2, philosophy refresh is a prerequisite before design can finalize.

---

## 6. Recommended Research Next Steps

Before verdict modifiers can become a proposal, the following must be resolved:

### Immediate (blocking for proposal completion):

1. **Non-blocking warning semantics.** Shane to decide: Path 1 (warnings block, styled as concerns) or Path 2 (warnings don't block, philosophy refresh)?
   - If Path 1: Proceed to design finalization
   - If Path 2: Route to philosophy team for "graduated severity" refresh before design proceeds

2. **State verdict value prop.** Validate that endpoint categorization (Option A) provides enough value to justify implementation:
   - Gather sample use cases where state success/error clarity would improve readability
   - Model one realistic precept with and without state verdicts; compare comprehension
   - Define reachability analysis C65–C67 in detail (what exactly do we check?)

3. **Event verdict diagnostic criteria.** Define exact conditions for C58–C59 (outcome-shape mismatch):
   - If event row can have guard that produces different outcome, is that a violation? (Probably not — guards make it conditional)
   - Clarify: is it the outcome *type* (Transition/Rejected/ConstraintFailure) or outcome *kind* (success-aligned vs error-aligned)?

### Secondary (foundation for iteration):

4. **Terminal + verdict composition.** If `terminal` structural modifier and verdict modifier both exist, how do they interact?
   - `state Approved terminal success` — what's the difference from just `success`?
   - Can a non-terminal state have a verdict? (E.g., `state InProgress success` — "in this step, success is still being pursued")

5. **Stateless precept verdicts.** How do verdicts apply in data-only precepts without states?
   - Can you declare event verdict in a stateless precept? (No events have transitions, so what does "success event" mean?)
   - Can you declare state verdict? (No states exist, so what do you declare?)
   - Design limitation or orthogonal concern?

6. **Non-blocking warning implementation strategy.** If Path 2 (non-blocking) is chosen:
   - Timeline: when does philosophy refresh happen relative to design finalization?
   - Does runtime need new outcome kind or can we add metadata field to existing ConstraintFailure?
   - How does this affect MCP tools (precept_inspect, precept_fire return shapes)?

7. **Rollout strategy.** Verdict modifiers affect many surfaces:
   - Diagnostics: C58–C70+ new codes
   - MCP DTOs: state/event metadata changes
   - Semantic tokens: color Css classes changes  
   - VS Code extension: new diag renderers
   - Samples: all need vamp for clarity (or stay as-is if verdicts are optional?)
   - Phased rollout plan needed

---

## Appendix: Architecture Decision Summary

### Declaration-level vs Configuration-level (LOCKED for Precept)

Frank recommends **Declaration-level** for Precept. Verdict sits in the `.precept` file, not in a separate config artifact.

**Why:**
- **One-file completeness:** Precept principle. Verdicts visible in authoring surface.
- **Non-programmer audience:** YAML/config files break immersion; keyword modifiers are English-ish.
- **Precedent:** FluentValidation, BPMN (event types), Roslyn defaults all use declaration-level.
- **Downside:** Less deployment flexibility (Kubernetes model). But Precept is not a platform; flexibility is secondary.

**Rejected:** Configuration-level model (ESLint, Kubernetes style). Violates one-file completeness and is less suitable for domain experts.

---

## Summary: Recommended Design Path

| Tier | Option | Rationale |
|---|---|---|
| **Events** | A (Outcome-shape declaration) | Strongest precedent; compile-time provable; one-file complete; no philosophy gap |
| **Rules** | A1 (Blocking warnings) | Maintains prevention guarantee; warning means "less critical error"; philosophy coherent |
| **States** | A (Endpoint categorization) | Novel but valuable; no runtime changes; enables reachability diagnostics; visual clarity |

**Critical open gate:** Non-blocking warnings (A2) requires philosophy refresh. Flag for Shane/owner decision before proceeding.

All three options are **declaration-level** (verdicts in `.precept` file) and **metadata-based** (no runtime behavior change, styling/diagnostics/tooling impact only).
