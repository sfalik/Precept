# Verdict Modifiers: Semantic Reframing Analysis

**Research date:** 2026-04-12  
**Author:** Frank (Lead/Architect & Language Designer)  
**Batch:** Expressiveness expansion — conceptual pivot analysis  
**Context:** Shane questioned whether verdict modifiers should declare outcome shape (Option A enforcement via C58/C59) or declare meaning (semantic annotations for tooling and readability). Triggered by rereading issue #58's `error`/`failure` state modifier work.  
**Depends on:** `verdict-modifier-design-options.md` (Option A), `structural-lifecycle-modifiers.md` (#58 taxonomy), `verdict-modifier-ux-perspective.md` (Elaine), `verdict-modifier-runtime-enforceability.md` (George)

This file is durable research. It analyzes a fundamental conceptual question: are verdict modifiers enforcement mechanisms or meaning declarations?

---

## 1. Framing Comparison

### Option A: Outcome-Shape Enforcement (Current Recommendation)

`event Approve success` means: **"all transition rows from this event must produce Transition or NoTransition outcomes."** The compiler enforces this via two new diagnostics:

- **C58:** `"Event <Name> declared success but row produces Rejected"` (error)
- **C59:** `"Event <Name> declared error but row produces Transition"` (error)

The verdict modifier is a **structural constraint** — it restricts what the author can write in transition rows. If you declare an event `success`, the compiler prevents you from writing a row that rejects.

**What it buys:**
- Strong compile-time guarantee: the event's outcome shape matches declared intent
- Fits the "prevention, not detection" philosophy — the compiler prevents outcome-shape contradictions
- Consistent with Precept's style of catching authoring mistakes early

**What it costs:**
- `warning` events have weak semantics — allows mixed outcomes, no enforcement ("warning is allowed but not enforced" per the design options doc)
- Constrains the author's expressive freedom — you cannot write `event Approve success` and then have a guard-dependent reject row (e.g., reject when fraud detected). Real business events often have conditional rejection paths even for "positive" events
- Creates a tension: the enforcement is about **row shapes**, not about **data integrity**. Precept's prevention guarantee is about data configurations. C58/C59 prevent certain row patterns — a different category of prevention
- Extends the philosophy beyond its established territory (data integrity → outcome shape)

### Semantic Model: Meaning Annotation (Shane's Proposed Reframing)

`event Approve success` means: **"this event signifies a positive development in the entity's lifecycle."** It communicates intent to tooling and humans. It does not constrain outcome shapes.

The verdict modifier is an **intent declaration + tooling directive** — modifier roles #2 and #3 from the structural-lifecycle-modifiers taxonomy.

**What it buys:**
- Natural semantics: `event Approve success` reads as "Approve is a success-type event," which is what domain experts actually mean
- No `warning` ambiguity: `event Escalate warning` simply means "escalation is a concern-level event," not "mixed outcomes are allowed"
- No philosophy tension: semantic annotations don't prevent or detect anything — they label. The prevention guarantee is untouched
- Unifies cleanly with #58's `error`/`failure` state modifier concept
- Still enables rich tooling (diagrams, badges, inspector, hover, MCP metadata)
- Permits real business patterns: an `event Approve success` can still have a guard-dependent reject row for fraud cases — the annotation declares authorial *intent*, not a structural *mandate*

**What it costs:**
- No hard C58/C59 enforcement — outcome-shape contradictions are not compile errors
- The modifier is "less powerful" in the enforcement sense — it does not create new structural guarantees
- Requires the tooling story to carry the justification (a keyword that does nothing without tooling could feel like ceremony)

### The Key Distinction

Option A constrains **what the author can write.** The semantic model communicates **what the author means.** 

Precept's constraint vocabulary (`invariant`, `assert`) is about constraining *data*. Option A's C58/C59 would constrain *row patterns* — a category shift. The semantic model keeps verdict modifiers in the declaration/tooling space where modifiers like `initial`, `nullable`, and `default` already live.

---

## 2. What Happens to C58/C59?

Under the semantic model, hard C58/C59 diagnostics do not exist. But weaker consistency checks become available — and they may be more useful than the hard versions.

### The Problem with Hard C58/C59

Hard enforcement creates false positives in legitimate business scenarios:

```precept
# Under Option A, this is a compile error — but it's correct business logic
event Approve success
from UnderReview on Approve 
    when DocumentsVerified and CreditScore >= 680 -> set ApprovedAmount = Approve.Amount -> transition Approved
from UnderReview on Approve -> reject "Approval requirements not met"
```

The second row rejects `Approve`. Under C58, the compiler would flag this as an error: "Event Approve declared success but row produces Rejected." But this is correct — the intent is "Approve is meant to succeed, but it can fail when preconditions aren't met." C58 would force the author to either (a) remove the `success` annotation or (b) restructure the rows to avoid rejection, both of which are worse.

### Soft Consistency Diagnostics (Better Alternative)

Under the semantic model, the compiler can still detect *semantic contradictions* — situations where the annotation and the row structure are so misaligned that the author probably made a mistake. These are warnings, not errors.

| Diagnostic | Trigger | Severity | Rationale |
|---|---|---|---|
| **C58-soft** | Event declared `success` but **every** row from **every** source state produces Rejected | Warning | If 100% of rows reject, the event can never succeed. The `success` annotation is contradicted by the structure. |
| **C59-soft** | Event declared `error` but **every** row from **every** source state produces Transition | Warning | If 100% of rows transition, the event always succeeds. The `error` annotation is contradicted. |
| **C-cross** | All events that reach state S are declared `error`, but state S is declared `success` | Hint | Semantic inconsistency: a success state reachable only via error events. |
| **C-dead-verdict** | Event has verdict but is unreachable (feeds into existing C49 orphan diagnostic) | Hint | Verdict annotation on an event that can never fire. |

**Why soft is better than hard:**

1. **No false positives.** The soft checks fire only on total contradiction (100% of rows), not partial. An `event Approve success` with one guarded reject row is fine — and it should be.
2. **Catches real mistakes.** An event declared `success` where every row rejects is almost certainly an authoring error — either the annotation is wrong or the rows are wrong.
3. **Preserves authoring freedom.** The author can write any row pattern they want. The compiler notes when the annotation seems inconsistent, but never blocks.
4. **Philosophy-safe.** Warnings about semantic consistency are not enforcement — they're assistance. No prevention guarantee is claimed or broken.

### Assessment

Hard C58/C59 are lost. Their replacement (soft diagnostics) is strictly less powerful as enforcement but strictly more useful as guidance — because C58/C59's enforcement domain (row shapes) was never part of Precept's core prevention guarantee anyway.

---

## 3. Unification with #58's Modifier Taxonomy

### The Modifier Role Framework (from structural-lifecycle-modifiers research)

The #58 research established five modifier roles:

1. **Structural constraint** — compiler errors if declared structure violates the property
2. **Intent declaration** — author states purpose; compiler cross-checks
3. **Tooling directive** — modifier changes presentation (diagrams, completions, masking)
4. **Analysis enabler** — modifier unlocks new compiler checks
5. **Feature gate** — modifier enables or disables language features

And the scope rule: *"A modifier must be either compile-time verifiable OR tooling-actionable."*

### Where Verdict Modifiers Land Under Each Model

| Modifier Role | Option A (Enforcement) | Semantic Model |
|---|---|---|
| Structural constraint | ✓ C58/C59 enforce outcome shapes | ✗ No structural enforcement |
| Intent declaration | ✓ Declares expected outcome category | ✓ Declares lifecycle significance |
| Tooling directive | ✓ Colors, badges, MCP metadata | ✓ Colors, badges, MCP metadata |
| Analysis enabler | ✓ C58/C59 + reachability | ✓ Soft consistency + reachability |
| Feature gate | ✗ No gating | ✗ No gating |

Under the semantic model, verdict modifiers are **roles #2 + #3 + partial #4** — exactly the same profile as `error`/`failure` state modifiers from #58's second-pass analysis.

### The Unification

The #58 research concluded about `error`/`failure` on states:

> "Tier 2. Limited structural check. Primary value is semantic annotation and tooling."

Under the semantic model, verdict modifiers on events have THE SAME profile:

> "Tier 2. Limited structural check (soft consistency). Primary value is semantic annotation and tooling."

And verdict modifiers on states:

> "Tier 2. Limited structural check (reachability + consistency). Primary value is semantic annotation and tooling."

**They are the same mechanism.** The verdict vocabulary (`success`, `error`, `warning`) is a unified set of semantic annotations that applies to both states and events. The structural-lifecycle-modifiers research already identified this category and evaluated it favorably. The semantic model makes verdict modifiers a natural extension of that work, not a separate mechanism.

### Does This Simplify or Complicate the Modifier Design Space?

**Simplifies.** Under Option A, verdict modifiers were a DIFFERENT kind of modifier from #58's `error`/`failure` — they were outcome-enforcement (role #1), while `error`/`failure` were semantic annotation (roles #2/#3). Two mechanisms with the same vocabulary but different enforcement profiles. Under the semantic model, they're one mechanism: semantic modifiers that communicate lifecycle significance.

The modifier design space becomes:

| Tier | Examples | Roles | Enforcement |
|---|---|---|---|
| Structural (Tier 1) | `initial`, `terminal`, `entry`, `advancing`, `writeonce` | #1 + #2 + #3 | Hard compile-time checks |
| Semantic (Tier 2) | `success`, `error`, `warning` (on states and events) | #2 + #3 + partial #4 | Soft consistency checks, tooling |
| Tooling-only | `sensitive`, `audit` | #3 only | No checks, pure tooling |

Verdict modifiers are squarely in the Semantic tier — a coherent category with clear precedent from the #58 analysis.

---

## 4. The Non-Blocking Question Resolution

### The Problem Statement

The design options doc flagged this as "THE HARDEST DESIGN QUESTION":

> If verdict modifiers introduce severity levels, and warning-level rules don't block, does Precept's "prevention, not detection" guarantee survive?

George's runtime analysis (Model D) showed it CAN survive with philosophy reframing. But it required:
- New TransitionOutcome values (`TransitionWithWarnings`, `NoTransitionWithWarnings`)
- Splitting constraint evaluation into error-pass and warning-pass
- Rewriting `docs/philosophy.md` from "prevention, not detection" to "error-level rules are PREVENTED; warning-level rules are DETECTED and reported"

### How the Semantic Model Changes This

Under the semantic model, **the non-blocking question dissolves for event and state verdicts.**

Event and state verdicts are semantic annotations — they don't enforce anything. There's nothing to block or not-block. The question "should `event Approve success` be blocking?" is ill-formed under the semantic model, because the annotation doesn't create an enforcement gate.

**The non-blocking question moves entirely to rule severity — a SEPARATE mechanism.**

If Precept also wants warning-level rules (a different feature from verdict modifiers), that's George's Model D analysis. But it's no longer coupled to verdict modifiers. The two features become orthogonal:

| Feature | What it is | Enforcement? | Philosophy impact? |
|---|---|---|---|
| Verdict modifiers (semantic) | `event Approve success`, `state Denied error` | No — annotation only | None — no guarantee claimed |
| Rule severity (enforcement) | `invariant X because "Y" warning` | Yes — changes blocking behavior | Yes — requires philosophy reframing |

**This separation is the single biggest benefit of the semantic model.** It decouples the easy, uncontroversial feature (semantic annotations) from the hard, philosophy-threatening feature (non-blocking warnings). They can ship independently, on different timelines, with different levels of design confidence.

### The "Hardest Design Question" Status

Under Option A, the non-blocking question was entangled with verdict modifiers because Option A treated `warning` as an outcome-enforcement category for events, which naturally led to "what does warning enforcement look like for rules?"

Under the semantic model, verdict modifiers don't raise the question at all. Rule severity is a separate decision that can be evaluated on its own merits, without the pressure of "we need to decide this to ship event verdicts."

**Resolution: dissolved by separation of concerns.** The non-blocking question is still real for rule severity, but it's no longer the verdict modifier team's problem.

---

## 5. Cross-Tier Coherence

Under the semantic model, how do the three verdict surfaces relate?

### Event Verdicts: "This event signifies..."

```precept
event Approve success    # Firing Approve signifies positive progress
event Deny error         # Firing Deny signifies a negative outcome
event Escalate warning   # Firing Escalate signifies a concern
```

The annotation communicates **lifecycle trajectory.** Approve moves the entity toward its happy-path endpoints. Deny moves it toward failure endpoints. Escalate signals something needs attention. The compiler doesn't enforce outcome shapes, but tooling uses the annotation for diagram coloring, edge styling, hover information, and MCP metadata.

### State Verdicts: "Being in this state means..."

```precept
state Approved success   # Being Approved means the entity reached a positive outcome
state Denied error       # Being Denied means the entity reached a negative outcome
state OnHold warning     # Being OnHold means the entity needs attention
```

The annotation communicates **position significance.** The preview diagram renders success states green, error states red, warning states amber. Reachability diagnostics gain significance: "no path from initial to any success state" is now a meaningful warning (C67). This is the same category as `error`/`failure` from #58 — validated by that research.

### Rule Verdicts: Fundamentally Different

```precept
invariant Balance >= 0 because "Balance non-negative"               # error (default)
invariant Notes.length < 500 because "Keep notes concise" warning   # warning
```

Rule verdicts are NOT semantic annotations. They are **enforcement-level declarations** that change runtime behavior — whether constraint violations block the operation. This is a different mechanism entirely, governed by George's Model D analysis and the philosophy reframing question.

### The Asymmetry Is Correct

Event and state verdicts = semantic (meaning annotation, tooling directive).  
Rule verdicts = enforcement (blocking behavior change).

This asymmetry is not a design flaw — it reflects a genuine categorical difference:

- Events and states are **structural entities** in the lifecycle graph. They don't evaluate to true/false. Annotating them with significance is purely descriptive.
- Rules are **executable constraints**. They evaluate to true/false on every operation. Attaching severity to them has behavioral consequences.

The shared vocabulary (`success`, `error`, `warning`) works because the words carry meaning in both contexts, but the *mechanism* differs. A domain expert reads `event Approve success` as "Approve is a positive event" and `invariant X because "Y" warning` as "this rule is advisory" — both intuitive, both correct, despite different enforcement profiles.

### Principle #5 Alignment

Principle #5: *"Data truth vs movement truth — the keyword tells you the category."*

Under the semantic model, the keyword (`success`, `error`, `warning`) tells you the verdict category, and the declaration context (event/state vs. invariant/assert) tells you whether it's semantic or enforcement. This is consistent with Principle #5's pattern: the same concept behaves differently in data-truth vs. movement-truth contexts.

---

## 6. Philosophy Alignment

### The 12 Design Principles Under Each Model

| # | Principle | Option A | Semantic Model |
|---|---|---|---|
| 1 | Deterministic, inspectable | ✓ No runtime change | ✓ No runtime change |
| 2 | English-ish but not English | ✓ `event Approve success` reads well | ✓ Same syntax, same readability |
| 3 | Minimal ceremony | ✓ Simple keyword | ✓ Same |
| 4 | Locality of reference | ✓ Verdict on same line as declaration | ✓ Same |
| 5 | Data truth vs movement truth | ⚠ Verdict becomes a structural constraint ON event rows — blurs the line | ✓ Verdict is a property OF the declaration — clean |
| 6 | Collect-all for validation | ✓ C58/C59 are compile-time | ✓ Soft checks are compile-time |
| 7 | Self-contained rows | ⚠ C58/C59 create a dependency: rows must be consistent with event-level annotation | ✓ Rows are independent — verdict is metadata on the event, not a constraint on rows |
| 8 | Sound, compile-time-first | ✓ C58/C59 catch contradictions | ✓ Soft checks catch total contradictions |
| 9 | Tooling drives syntax | ✓ Both models enable tooling | ✓ Semantic model is MORE tooling-driven (tooling is the primary value) |
| 10 | Consistent prepositions | N/A | N/A |
| 11 | `->` means "do something" | ✓ Verdict doesn't affect `->` | ✓ Same |
| 12 | AI is first-class consumer | ✓ MCP queryable | ✓ MCP queryable — verdict metadata for AI reasoning |
| 13 | Keywords for domain, symbols for math | ✓ `success`/`error`/`warning` are domain keywords | ✓ Same |

**Net assessment:**
- Option A creates minor tensions with Principles #5 and #7 (verdict-as-structural-constraint blurs data truth vs. movement truth, and row independence gets a cross-row dependency via C58/C59)
- Semantic model has zero principle tensions
- Semantic model is MORE aligned with Principle #9 (tooling drives syntax) because the modifier's primary justification IS tooling

### The Philosophy Gap Question

**Option A creates a potential philosophy gap:** C58/C59 are "prevention" in the structural sense — preventing outcome-shape contradictions. But `docs/philosophy.md` defines prevention as preventing invalid data configurations, not preventing inconsistent annotations. Extending "prevention" to cover row-pattern consistency is a category expansion that philosophy.md doesn't describe.

**The semantic model avoids the gap entirely.** Semantic annotations make no prevention claims. They sit in the same category as `initial` (intent declaration + tooling directive) — a well-established modifier precedent. The philosophy document doesn't need to describe or justify them as prevention mechanisms because they aren't.

### Does the Semantic Model Create New Tensions?

One potential tension: **"Why does the keyword exist if it doesn't prevent anything?"**

Answer: The scope rule from #58 research states: *"A modifier must be either compile-time verifiable OR tooling-actionable."* Semantic verdict modifiers are tooling-actionable. They change diagram rendering, preview behavior, inspector display, hover information, completion suggestions, and MCP metadata. This is the same justification as `nullable` and `default` — those modifiers don't prevent anything either; they declare type contracts and initialization values that tooling uses.

The keyword exists because tooling needs an explicit author signal. Without `state Denied error`, the diagram cannot distinguish intentional failure endpoints from transient states. The annotation makes the author's intent machine-readable — which is exactly what Principle #12 (AI as first-class consumer) calls for.

---

## 7. Tooling Story

The semantic model's primary value proposition is tooling. Walk through every surface:

### Diagram Rendering

**States:** Success states get emerald accents (fill, border, or glyph ✓). Error states get rose accents (✕). Warning states get amber accents (⚠). Neutral states remain violet. This is Elaine's "visual governance map" from the UX analysis — instantly communicates lifecycle topology without reading transition rows.

**Events/Edges:** Success events render in emerald edge color. Error events render in rose. Warning events render in amber. Neutral events remain cyan. A viewer sees the happy path (emerald) vs. failure path (rose) at a glance.

**Coverage stat:** "Reachable from current state: 2 success endpoints, 1 error endpoint." Unique to Precept. No other system provides this.

### Preview Panel

**Lifecycle trajectory:** When stepping through events, the preview shows: "Current event: Approve (success) → target: Approved (success)." The verdict chain tells the story.

**Mismatch highlighting:** If a success event produces Rejected at runtime (e.g., guard failed), the preview shows both: authored intent (success, faded) + runtime outcome (rejected, bold). This is Elaine's two-layer model — authoring intent beneath runtime reality.

### Inspector Panel

**State context:** "You are in state Denied (error endpoint). This state signifies a negative outcome."

**Event listing:** "Available events: Appeal (success), Close (success), Reopen (warning)." The verdict annotations in the event list communicate significance before the user clicks.

### Hover / Tooltips

**State hover:** `state Denied error` → "Denied: an error endpoint. Entering this state signifies the entity has reached a failure outcome."

**Event hover:** `event Approve success` → "Approve: a success event. Firing this event signifies positive progress in the entity's lifecycle."

### Completions

**Verdict suggestions:** After typing `state Approved`, completions suggest `success | error | warning` with context-aware ranking. If the state has no outgoing transitions (terminal), `success` or `error` rank highest. If the state has outgoing transitions, `warning` ranks higher (transient concern state).

**Event verdict inference:** After typing `event Approve`, completions suggest `success` if the event name heuristically suggests positive outcomes (Approve, Accept, Fund, Complete). Suggest `error` for negative names (Deny, Reject, Cancel, Decline).

### MCP Tools

**`precept_compile`:** State and event metadata include `verdict` field: `{ "name": "Approved", "verdict": "success" }`.

**`precept_inspect`:** Event inspection includes verdict: `{ "event": "Approve", "verdict": "success", "outcome": "Transition" }`. AI consumers can compare declared verdict to actual outcome.

**`precept_fire`:** Fire response includes `eventVerdict` alongside outcome: `{ "outcome": "Transition", "eventVerdict": "success" }`. AI can flag when intent and outcome diverge.

**`precept_language`:** Vocabulary includes verdict modifiers in the keyword list and modifier documentation.

### Is Tooling Value Sufficient?

**Yes.** The tooling surface is at least as rich as `initial`'s tooling impact (diagram rendering of start state, inspector marking, MCP metadata). And `initial` is one of the most valued existing modifiers despite being primarily a tooling directive — its structural enforcement (C8/C13) is important, but its tooling impact (start-state marking in diagrams) is what users notice first.

Verdict modifiers have a BROADER tooling surface than `initial`:
- `initial` affects 1 state. Verdict modifiers affect every annotated state and event.
- `initial` enables start-state visualization. Verdict modifiers enable the entire "governance map" visualization.
- `initial` has no MCP-specific value. Verdict modifiers give AI consumers intent metadata for reasoning.

The tooling justification is strong. The modifier earns its keyword position.

---

## 8. "Soft" Compile-Time Checks

Even without hard enforcement, the semantic model supports useful diagnostics. These are information-level or warning-level consistency checks — the compiler notes when annotations seem contradicted by structure, but never blocks.

### Proposed Soft Diagnostics

**C58-soft: Total outcome contradiction (event)**

> `"Event <Name> declared <verdict> but every reachable row produces <opposite outcome>"`

Triggers when an event declared `success` has ONLY reject/constraintFailure outcomes across all source states, or an event declared `error` has ONLY transition/noTransition outcomes. This is a near-certain authoring mistake — the annotation and the rows completely disagree.

Severity: Warning. The author may suppress intentionally (annotating future intent on a partially-implemented event).

**C59-soft: Total outcome contradiction (state)**

> `"State <Name> declared <verdict> but is unreachable from initial state"`

Combines with existing C48 (unreachable state). Adding verdict context makes the diagnostic more actionable: "Success state Funded is unreachable" is more urgent than "State Funded is unreachable."

Severity: Warning (inherits from C48).

**C-cross: Verdict chain inconsistency**

> `"State <Name> declared success but reachable only via error-declared events"`

If every event that can reach state Approved is declared `error`, the verdict chain contradicts: an error event producing a success state. This is likely a mistake in either the event annotation or the state annotation.

Severity: Hint. The author may have legitimate reasons (e.g., "error event triggers a corrective transition to a positive state").

**C-match: Verdict alignment confirmation (non-diagnostic)**

Not a diagnostic, but a feature: the inspector and MCP tools report verdict alignment. "Event Approve (success) → State Approved (success): aligned." "Event Deny (error) → State Denied (error): aligned." This positive confirmation reinforces the semantic model's value without enforcement.

### Comparison to Hard C58/C59

| Aspect | Hard C58/C59 | Soft diagnostics |
|---|---|---|
| False positive rate | High — triggers on legitimate mixed-outcome events | Near-zero — triggers only on total contradiction |
| Authoring burden | Must restructure rows or remove annotation | No restructuring needed — just informational |
| Detection power | Catches every row-level mismatch | Catches only complete mismatches |
| Philosophy claim | Prevention (outcome-shape enforcement) | Detection (consistency assistance) |
| Author experience | "The compiler won't let me do this" | "The compiler thinks this might be a mistake" |

The soft diagnostics trade detection power for precision. They catch fewer mismatches but never create false positives — a trade-off aligned with Principle #8: "If the checker can't prove a contradiction, it assumes satisfiable."

---

## 9. Concrete Syntax Examples

### Insurance Claim

**Under Option A (Enforcement):**

```precept
precept InsuranceClaim

state Draft initial
state Submitted
state UnderReview
state Approved success      # C65: compiler verifies reachability to this endpoint
state Denied error          # C65: compiler verifies reachability to this endpoint
state Paid success

event Submit success        # C58: compiler verifies ALL rows produce Transition
event Approve success       # C58: ERROR — the guard-false reject row violates this ✗
event Deny error            # C59: compiler verifies ALL rows produce Rejected
event PayClaim success

# Problem: from UnderReview on Approve has a reject row for unmet conditions:
# from UnderReview on Approve -> reject "Not enough evidence"
# This CONFLICTS with `event Approve success` under C58.
# Author must either remove `success` or remove the reject row.
```

**Under Semantic Model:**

```precept
precept InsuranceClaim

state Draft initial
state Submitted
state UnderReview
state Approved success      # Being Approved means positive resolution
state Denied error          # Being Denied means negative resolution
state Paid success          # Being Paid means completed successfully

event Submit success        # Submitting signifies positive progress
event Approve success       # Approving signifies positive intent (may fail on guards)
event Deny error            # Denying signifies negative intent
event PayClaim success      # Paying out signifies completion

# No conflict. The reject row for unmet conditions is fine:
from UnderReview on Approve 
    when AdjusterName != null and MissingDocuments.count == 0
    -> set ApprovedAmount = Approve.Amount -> transition Approved
from UnderReview on Approve -> reject "Not enough evidence for approval"
# Approve is still a "success" event — it signifies positive intent.
# The reject row represents a precondition failure, not a contradiction.
```

**Assessment:** The semantic model is more natural for InsuranceClaim. The reject row on Approve is standard business logic — "Approve can fail if conditions aren't met." Option A would penalize this correct pattern.

### Hiring Pipeline

**Under Semantic Model:**

```precept
precept HiringPipeline

state Draft initial
state Screening
state InterviewLoop
state Decision
state OfferExtended
state Hired success         # Being Hired is the positive outcome
state Rejected error        # Being Rejected is the negative outcome

event SubmitApplication success   # Submitting starts the positive path
event PassScreen success          # Passing screen is progress
event ExtendOffer success         # Extending offer is positive
event AcceptOffer success         # Accepting is the happy ending
event RejectCandidate error       # Rejecting is the negative signal
event RecordInterviewFeedback warning  # Feedback is notable but neutral

# All existing transition rows unchanged. The reject row on PassScreen
# (must have interviewers assigned) is fine — PassScreen is "success"
# intent that can fail on preconditions.
```

**Assessment:** The verdict annotations read naturally. A reader scans the event list and immediately sees the lifecycle narrative: Submit → Screen → Interview → Offer → Accept is the happy path. Reject is the off-ramp. RecordInterviewFeedback is a concern-level event (triggers attention but doesn't determine outcome).

### Loan Application

**Under Semantic Model:**

```precept
precept LoanApplication

state Draft initial
state UnderReview
state Approved success      # Approved = positive outcome
state Funded success        # Funded = completed lifecycle
state Declined error        # Declined = negative outcome

event Submit success        # Submitting starts the positive path
event VerifyDocuments success  # Verification is progress  
event Approve success       # Approval intent (may fail on creditworthiness)
event Decline error         # Decline is the negative path
event FundLoan success      # Funding completes the lifecycle

# The complex guard on Approve:
from UnderReview on Approve 
    when DocumentsVerified and CreditScore >= 680 
    and AnnualIncome >= ExistingDebt * 2 
    and RequestedAmount < AnnualIncome / 2
    -> set ApprovedAmount = Approve.Amount -> transition Approved
from UnderReview on Approve -> reject "Requirements not met"

# Under Option A, this reject row CONFLICTS with `event Approve success`.
# Under semantic model, it's fine — Approve is a success-intent event
# that has a guard-dependent failure path.
```

**Assessment:** LoanApplication is the canonical example of why Option A's enforcement is problematic. The Approve event has a complex guard and a reject fallback — a completely standard pattern in underwriting workflows. Option A would force the author to either remove the `success` annotation (losing semantic value) or restructure the rows to avoid rejection (losing expressiveness). The semantic model handles this naturally.

---

## 10. Recommendation

**I endorse the semantic model over my original Option A.**

Shane's instinct is correct, and the analysis confirms it from multiple angles:

### The Case for the Pivot

1. **Option A was fighting the wrong battle.** C58/C59 enforce outcome shapes — but Precept's prevention guarantee is about data integrity, not row patterns. Extending "prevention" to cover outcome-shape consistency was a category stretch that created tensions with Principles #5 and #7.

2. **The semantic model is more honest about what verdict modifiers are.** They are intent declarations and tooling directives. That's what `error`/`failure` are in issue #58. That's what `initial` is (among other things). The semantic model calls them what they are instead of inventing enforcement to justify their existence.

3. **The false positive problem is fatal for Option A.** Every non-trivial business event has guard-dependent rejection paths (InsuranceClaim's Approve, LoanApplication's Approve, HiringPipeline's PassScreen). C58 would flag all of these as errors. The author would have to choose between the annotation and the business logic — a choice that should never arise.

4. **The separation of concerns is the decisive benefit.** Decoupling semantic annotations (easy, uncontroversial, ship anytime) from rule severity (hard, philosophy-threatening, needs deliberation) is worth more than any enforcement C58/C59 could provide. Ship verdict annotations in one milestone, resolve rule severity on its own timeline.

5. **Tooling value is sufficient.** The governance map visualization, MCP metadata for AI reasoning, verdict-aware inspectability, and completion scaffolding justify the keyword without enforcement. The tooling story is broader than `initial`'s — and nobody questions `initial`'s value.

6. **Philosophy safety.** Zero philosophy gaps, zero principle tensions, zero need to revise `docs/philosophy.md`. The semantic model fits cleanly into Precept's existing modifier precedent (`initial`, `nullable`, `default`).

### What I'd Preserve from Option A

The soft diagnostics from Section 8 are worth keeping. They provide the consistency guidance that C58/C59 aimed for without the false-positive burden. Specifically:

- **C58-soft** (total outcome contradiction on events) — high value, near-zero false positives
- **C59-soft** (verdict-annotated state unreachable) — combines with existing C48 
- **C-cross** (verdict chain inconsistency) — novel, uniquely enabled by semantic verdicts

These give the compiler something useful to say about verdict annotations without ever blocking the author.

### Recommended Terminology

Drop "verdict modifiers" as the mechanism name for events and states. Call them **semantic modifiers** — the term #58 already established. Reserve "verdict" language for the rule severity mechanism (if pursued separately).

- `state Approved success` — a semantic modifier declaring endpoint significance
- `event Approve success` — a semantic modifier declaring lifecycle trajectory intent
- `invariant X because "Y" warning` — a severity declaration (separate mechanism, separate timeline)

This makes the taxonomy self-documenting: semantic modifiers (intent) vs. severity declarations (enforcement).

### Impact on the Team's Prior Work

- **Elaine's UX analysis:** Fully preserved. Every surface treatment she proposed works identically under the semantic model. The two-layer model (authored intent beneath runtime outcome) is MORE natural under semantic annotations.
- **George's runtime analysis:** Still valid for rule severity, but decoupled from verdict modifiers. Model D remains the recommendation IF/WHEN Precept pursues non-blocking warning rules. It just doesn't need to ship alongside semantic modifiers.
- **Steinbrenner's roadmap:** Event semantic modifiers can move earlier (lower risk, no philosophy gate). Rule severity retains its own milestone gate. State semantic modifiers can ship alongside events since they're the same mechanism.

### Ship Order Under Semantic Model

1. **Event + state semantic modifiers** (one feature, same mechanism) — no philosophy gate, no runtime changes, soft diagnostics only
2. **Rule severity** (separate feature, separate decision) — philosophy gate, runtime changes (Model D), separate milestone

This reverses Steinbrenner's recommended order (events → rules → states) by bundling events and states together, which is correct under the semantic model because they're the same mechanism. Rule severity becomes a fully independent decision.
