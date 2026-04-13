# Verdict Modifiers in the Precept Language

**Research date:** 2026-04-11 (externally grounded rewrite)
**Author:** Frank (Lead/Architect & Language Designer)
**Batch:** Expressiveness expansion — proposal stage exploration
**Relevance:** Semantic annotations on entities and rules that declare authored intent about outcome severity (success/enabled, warning, error/blocked), enabling stronger diagnostics, visual styling, and tooling-driven behavior without requiring runtime tracking

This file is durable research, not a proposal body. It explores the full design space of verdict modifiers — comparative systems grounded in actual external documentation, compile-time provability across application surfaces, architectural options, philosophy alignment, and interaction with existing diagnostics and TransitionOutcome semantics. Every external claim is sourced from fetched documentation.

---

## Background and Problem

### The semantic visual system context

Precept's semantic visual system (design/system/foundations/semantic-visual-system-notes.md) defines three runtime verdict colors:
- **Emerald `#34D399` (enabled/success):** Entity is well-formed, constraints satisfied, valid state
- **Rose `#FB7185` (error/blocked):** Constraint violation, transition rejected, data invalid
- **Amber `#FCD34D` (warning):** Heuristic concern, guideline not met, risky configuration

These colors are currently runtime-applied overlays — diagnostics (C48–C52) and runtime outcomes (Rejected, ConstraintFailure) trigger the visual treatment, but the authoring surface has no way to declare "this entity's success state is important" or "this rule violation should be a warning, not an error."

The `TransitionOutcome` enum in the runtime already carries implicit verdict categories:

```csharp
public enum TransitionOutcome
{
    Undefined,           // (no outcome)
    Unmatched,          // (warning — event not routed)
    Rejected,           // (error — explicit prohibition)
    ConstraintFailure,  // (error — rule violated)
    Transition,         // (success — state change)
    NoTransition        // (success — data mutation only)
}
```

A transition outcome is a **runtime verdict** — the actual result of an event. A verdict modifier would be an **authored intent** — declaring in advance what kind of outcome we expect or consider critical.

### Prior research and the gap

Frank's structural-lifecycle-modifiers research (2026-04-10) identified `error` and `failure` as Tier 2 state modifier candidates with the note: "Limited structural check. Primary value is semantic annotation and tooling." That research established the modifier role framework (structural constraint, intent declaration, tooling directive, feature gate) and the scope rule: a modifier must be either structurally verifiable or tooling-actionable.

**The gap:** Precept can declare `terminal` (lifecycle boundary), `advancing` (state-change event intent), `writeonce` (field mutation), but has **no way to declare**:
- "This state is a success endpoint" vs "a failure endpoint"
- "This rule violation is a warning-level concern" vs "an error-level blocker"
- "This field's value matters for compliance" vs "routine operational data"

### Modifier roles (from structural-lifecycle-modifiers research)

1. **Structural constraint** — the compiler errors if declared structure violates the property
2. **Intent declaration** — the author states purpose; compiler cross-checks
3. **Tooling directive** — modifier changes presentation (diagrams, completions, masking)
4. **Feature gate** — modifier enables or disables language features

Verdict modifiers are fundamentally **intent declarations + tooling directives**. They declare what kind of outcome the author expects or considers significant. They do NOT create new structural constraints — their value is pedagogical (authoring clarity) and tactical (diagnostics, preview rendering). This is similar to existing `nullable` and `default` field modifiers (intent/contract, not verification).

---

## Design Space: Where Verdict Modifiers Could Apply

### Surface 1: States — Endpoint categorization

**Hypothetical syntax:**
```precept
state Approved success
state Denied error
state OnHold warning
```

**What it declares:** Terminal (or keystone) states carry business significance. Approved is a success endpoint; Denied is failure; OnHold is a concern state.

**Structural check:** Thin (are terminal states classified? Do non-terminals lack endpoint modifiers?). Primarily intent-driven.

**Tooling value:** Diagram renders success states green, error states red, warning states amber. Preview shows path counts to each endpoint. MCP includes verdict category in state metadata.

**Critical finding from external research:** **No comparable system puts severity on state declarations.** See Pattern 5 in the Cross-System Synthesis section. This is genuinely novel territory requiring higher justification.

### Surface 2: Events — Outcome intent

**Hypothetical syntax:**
```precept
on Approve success
on Deny error
on Escalate warning
```

**What it declares:** Event carries an outcome-intent category. Approve aims for success (Transition/NoTransition only); Deny aims for error (Rejected/ConstraintFailure only); Escalate signals a concern.

**Structural check:** STRONG — can verify outcome shapes at compile time:
- `success` event: all outcomes must be Transition or NoTransition (never Rejected, ConstraintFailure)
- `error` event: all outcomes must be Rejected or ConstraintFailure (never pure Transition)
- `warning` event: suggests mixed outcomes but not structurally verifiable

**Tooling value:** Diagram colors edges by outcome intent. Preview highlights success vs error paths. MCP fire includes event verdict in outcome response. Completions suggest expected outcomes. Linting warns if outcome shapes don't match declared intent.

### Surface 3: Fields — Significance categorization

**Hypothetical syntax:**
```precept
field CreditScore number error min 300
field ExpirationDate string warning
```

**What it declares:** Field values carry significance levels. CreditScore is error-level (if wrong, it's a structural problem); ExpirationDate is warning-level (if missed, it's a concern).

**Structural check:** Weak — no meaningful compile-time verification.

### Surface 4: Rules — Severity levels

**Approach A: Severity on constraint outcome**
```precept
invariant Balance >= 0 because "Balance non-negative"
invariant NotesLength < 500 because "Notes too long" warning
to Submitted assert Items.count > 0 because "Must have items" warning
```

**Approach B: Severity on explicit outcome**
```precept
from InReview on Approve
    when ApproversSignedOff.count == Required
    -> transition Approved "Approved" success
    | -> reject "Not approved" error
```

**Structural check:** Approach B has stronger checks (consistency between outcome kind and declared severity). Approach A is weaker but broader.

### Surface 5: Entity-level verdict

**Hypothetical syntax:**
```precept
precept WireTransfer success
```

**Structural check:** Weak — purely heuristic. Better handled as runtime aggregation.

---

## Comparable Systems Survey (Externally Grounded)

Every claim in this section is grounded in actual documentation fetched from the cited URLs. No claims are inferred from training data.

### FluentValidation — Per-rule severity

**Source:** docs.fluentvalidation.net/en/latest/severity.html

FluentValidation provides three severity levels: `Severity.Error` (default), `Severity.Warning`, and `Severity.Info`. Severity is declared per-rule using the `.WithSeverity()` fluent method:

```csharp
RuleFor(x => x.Surname)
    .NotNull()
    .WithSeverity(Severity.Warning);
```

Since v9, a callback form is also supported for dynamic severity:

```csharp
RuleFor(person => person.Surname)
    .NotNull()
    .WithSeverity(person => Severity.Warning);
```

**Critical behavioral detail:** Severity does NOT affect `IsValid`. The validation result is still `false` even when all failures are Warning severity. The `ValidationFailure.Severity` property is set as metadata, but the binary validity determination is unchanged. Severity is purely informational — it tells the consumer how important the failure is, not whether validation passed.

The global default can be overridden:

```csharp
ValidatorOptions.Global.Severity = Severity.Info;
```

**Precept implications:**
- Strong precedent for **rule-level severity in constraint languages**. Severity is metadata on failure, not enforcement differentiator.
- Default-to-error is the sensible convention — authors opt in to lower severity.
- FluentValidation's model is **declaration-level** — severity is baked into the rule definition, not applied externally.
- The `IsValid`-is-still-false behavior validates the principle that severity is orthogonal to enforcement. This is Pattern 1 (see synthesis).

### ESLint — Configuration-level severity

**Source:** eslint.org/docs/latest/use/configure/rules

ESLint provides three severity levels: `"off"` (0), `"warn"` (1), `"error"` (2). Severity is configured per-rule in the configuration file, external to the rule definition:

```javascript
// eslint.config.js
export default [
    {
        rules: {
            "no-unused-vars": "warn",
            eqeqeq: "error"
        }
    }
];
```

Inline overrides are also supported:

```javascript
/* eslint eqeqeq: "off", curly: "error" */
```

**Critical design:** Severity is EXTERNAL to rule definition. The rules themselves don't declare their own severity — they evaluate to pass/fail. The configuration layer determines what happens with the result. This is a fundamentally different architecture from FluentValidation.

**Exit code behavior:** `"warn"` does not affect the exit code (CI passes). `"error"` causes exit code 1 (CI fails). This maps directly to "warning continues but flags" (Pattern 4).

**Typical use:** `"warn"` for rules being introduced that will eventually become `"error"`, or rules with potential false positives that shouldn't block CI.

**Precept implications:**
- ESLint represents the **configuration-level** camp — same rule, different severity depending on project context.
- The progression model (`warn` → `error` over time) is relevant: it suggests severity should be adjustable without changing the underlying rule.
- ESLint's exit code behavior is the clearest precedent for "warning continues, error blocks."

### XState v5 — No severity on states or events

**Source:** stately.ai/docs/states

XState has **no concept of state severity or event severity**. States have no success/error/warning annotation.

XState provides two metadata mechanisms:

**Tags** — arbitrary string arrays on state nodes:
```javascript
states: {
    submitting: {
        tags: ['loading']
    }
}
```
Checked via `state.hasTag('loading')`. Tags group states semantically for UI queries but carry no enforcement and no severity semantics.

**Meta** — arbitrary metadata objects on state nodes:
```javascript
states: {
    question: {
        meta: {
            question: 'How was your experience?'
        }
    }
}
```
Accessible via `state.getMeta()`. Pure metadata with no behavioral impact.

**Final states** (`type: 'final'`) mark lifecycle completion but are structural, not verdict-bearing. A final state is not "success" or "error" — it's "done."

**`state.can(event)`** provides read-only inspection of whether an event would cause a transition — similar to Precept's inspect. No severity involved.

**Precept implications:**
- **No precedent for verdict-as-severity on states in the most relevant comparable system.** XState has tags and meta for tooling-driven metadata, but deliberately chose NOT to introduce severity.
- This is significant: XState is the closest comparable to Precept in the state machine domain, and it has no state severity. The gap is genuine novelty, not a missed opportunity.

### BPMN 2.0 — Error Events vs Escalation Events (three-tier model)

**Source:** Camunda documentation on BPMN error events and escalation events

BPMN provides the clearest real-world precedent for a three-level severity model through its Error Event / Escalation Event distinction.

**Error Events** are ALWAYS interrupting:
- Boundary error events terminate the scope they're attached to.
- Error end events end the process flow.
- Errors propagate up the scope hierarchy until caught.
- Unhandled errors raise an incident — they cannot be ignored.
- Error codes are string-based, matched for catch routing. Catch-all events (no error code) catch everything.

**Escalation Events** are NON-CRITICAL by design:
- Escalation throw events continue execution — outgoing sequence flows are still taken.
- Both the throw path and the catch path execute concurrently.
- Escalation can optionally be configured as interrupting, but the default is non-interrupting.

**The three-tier mapping:**

| BPMN concept | Behavior | Precept verdict analogue |
|---|---|---|
| Error Event | Interrupting — terminates scope, propagates | **Error** — blocks, constraint failure |
| Escalation Event | Non-critical — continues, flags | **Warning** — flags concern, allows continuation |
| Normal flow | Success — proceeds as designed | **Success** — transition completes |

**Critical distinction:** BPMN distinguishes "business error" (modeled in the process) from "technical error" (retries/incidents). Business errors are authored intent — the process designer declares which outcomes are errors. Technical errors are infrastructure failures.

**Precept implications:**
- BPMN's error/escalation split is the **strongest precedent for a three-level severity model** in workflow systems.
- The error/escalation split applies to **events** (error end events, escalation intermediate events), not to **states**. BPMN does not annotate states with severity.
- BPMN's model is **declaration-level** — the event type determines severity, baked into the process definition.

### Kubernetes Validating Admission Policies — Evaluation-enforcement separation

**Source:** kubernetes.io documentation on Validating Admission Policies

Kubernetes provides three validation actions: `Deny`, `Warn`, and `Audit`. These are applied at the **binding level**, not at the rule level:

```yaml
apiVersion: admissionregistration.k8s.io/v1
kind: ValidatingAdmissionPolicyBinding
metadata:
  name: "demo-binding"
spec:
  policyName: "demo-policy"
  validationActions: [Deny]
```

or:

```yaml
  validationActions: [Warn, Audit]
```

**Critical architectural separation:** CEL expressions evaluate to true/false (the rule). `validationActions` determine what happens with the result (the enforcement). These are completely orthogonal. The same CEL expression can be `Deny` in one binding and `Warn` in another.

`Deny` and `Warn` cannot coexist on the same binding (redundant — Deny subsumes Warn). `Audit` adds observability metadata without enforcement.

**`failurePolicy`** determines what happens when the CEL expression itself errors: `Fail` (treat as Deny) or `Ignore` (treat as pass). This is error-handling policy, not severity.

**Precept implications:**
- **Strongest precedent for separating EVALUATION from ENFORCEMENT.** Kubernetes demonstrates that the same rule can have different severity in different deployment contexts.
- This is the purest **configuration-level** model — severity is never part of the rule definition.
- The `validationActions` mechanism maps to the fundamental architectural question for Precept: should severity be on the rule declaration (FluentValidation model) or on the enforcement layer (Kubernetes model)?

### Cedar Policy Language — Binary authorization, non-semantic annotations

**Source:** docs.cedarpolicy.com

Cedar policies are strictly binary: `permit` or `forbid`. There are no severity levels.

```cedar
permit(
    principal == User::"alice",
    action == Action::"view",
    resource == Album::"jane_vacation"
);
```

Cedar provides annotations — key-value pairs that have **no impact on policy evaluation**:

```cedar
@advice("Contact the album owner to request access")
@id("policy0")
forbid(principal, action, resource);
```

The documentation is explicit: "An annotation has no impact on policy evaluation." Annotations are pure metadata for tooling and documentation.

**`forbid` always wins:** If ANY policy says `forbid`, the authorization result is Deny regardless of how many policies say `permit`. This is a binary model — there is no "permit with warning" or "soft deny."

**Precept implications:**
- Cedar deliberately chose NOT to introduce severity. For authorization (binary allow/deny), severity is unnecessary.
- Cedar's annotations confirm Pattern 1: metadata is kept separate from evaluation.
- Cedar is not directly comparable to Precept's graduated constraint model — authorization is simpler than business rule governance. But Cedar's binary simplicity is instructive: severity levels add complexity, and they must justify that complexity.

### .NET / Roslyn Code Analysis — Six-level diagnostic severity

**Source:** learn.microsoft.com documentation on .NET code analysis configuration

Roslyn provides the most granular severity model of any system surveyed: six levels.

| Level | Build behavior | IDE behavior |
|---|---|---|
| `error` | Build fails | Red squiggle |
| `warning` | Build warns | Yellow squiggle |
| `suggestion` | No build impact | Gray dots (IDE only) |
| `silent` | No build impact | Hidden but active (code fixes available) |
| `none` | Suppressed entirely | Nothing shown |
| `default` | Use rule's built-in default | Rule-defined |

Configuration is per-rule in `.editorconfig`, external to the rule definition:

```ini
# Single rule
dotnet_diagnostic.CA1822.severity = error

# Category-level
dotnet_analyzer_diagnostic.category-performance.severity = warning

# Global-level
dotnet_analyzer_diagnostic.severity = suggestion
```

**Precedence cascade:** Single rule > Category > Global. More specific overrides less specific.

**Critical design:** Like ESLint and Kubernetes, Roslyn's model is **configuration-level**. Rules have built-in defaults, but severity is externally overridable. The same rule (CA1822) can be `error` in one project and `suggestion` in another.

**The six-level model** reveals a spectrum that Precept's three-level model (success/warning/error) simplifies:
- `error` and `warning` map directly.
- `suggestion` introduces an "IDE-only" tier — visible but passive.
- `silent` and `none` address suppression, which Precept doesn't need (rules either exist or don't).

**Precept implications:**
- Roslyn's cascade model (rule > category > global) is the most sophisticated precedent for severity configuration.
- The `error` vs `warning` vs `suggestion` distinction maps to build/CI behavior — a dimension Precept doesn't have (Precept's equivalent: constraint failure blocks vs warns vs informs).
- Roslyn confirms that **the default should be the strictest level** — deviation from default requires explicit opt-in to a lower severity.

---

## Cross-System Synthesis

Five universal patterns emerge from the seven-system survey. These patterns are the evidence base for Precept's architectural decisions.

### Pattern 1: Severity is ALWAYS metadata, not structural constraint

Every system keeps severity separate from the boolean evaluation of the rule itself.

- **FluentValidation:** `IsValid` is `false` regardless of whether failures are `Severity.Error` or `Severity.Warning`. The severity is metadata on the `ValidationFailure` object — it doesn't change the evaluation result.
- **ESLint:** Rules fire regardless of their configured level. `"warn"` and `"error"` rules both report — the difference is in the exit code.
- **Kubernetes:** CEL expressions evaluate to true/false independent of `validationActions`. The same expression produces the same boolean result whether the binding says `Deny`, `Warn`, or `Audit`.
- **Cedar:** Annotations have "no impact on policy evaluation" — explicit documentation.
- **Roslyn:** Analyzer rules evaluate the same way regardless of configured severity. `error` vs `warning` changes the build outcome, not the analysis result.

**Implication for Precept:** If Precept adds verdict modifiers, they must NOT change constraint evaluation. An `invariant` marked `warning` must still evaluate the expression and report the result. The verdict determines how the result is surfaced (block vs flag vs inform), not whether the constraint is checked.

### Pattern 2: Two architectural camps — declaration-level vs configuration-level

The seven systems split into two fundamentally different approaches to where severity lives:

| Camp | Systems | Where severity lives | Flexibility |
|---|---|---|---|
| **Declaration-level** | FluentValidation, BPMN | Baked into the rule/event definition | Fixed by author |
| **Configuration-level** | ESLint, Kubernetes, Roslyn | Applied externally; same rule, different severity | Context-dependent |
| **Binary (no severity)** | Cedar, XState | No severity concept | N/A |

**Declaration-level** (FluentValidation `.WithSeverity()`, BPMN error event type):
- Severity is part of the authoring act — the rule author decides importance.
- One-file completeness: reading the rule tells you its severity.
- Less flexible: downgrading a rule requires editing the definition.

**Configuration-level** (ESLint `.eslintrc`, Kubernetes `validationActions`, Roslyn `.editorconfig`):
- Severity is deployment context — the same rule can be `error` in production and `warn` in development.
- Requires a second artifact (config file, binding spec, `.editorconfig`).
- More flexible: severity is tunable without changing the rules.

**This is THE key architectural decision for Precept.** The two camps represent fundamentally different design philosophies. Choosing between them is not a syntax question — it's a product identity question.

### Pattern 3: The three-tier model is universal

Error/Warning/Info (or Error/Warn/Off, or Deny/Warn/Audit) appears across all systems that have severity:

| System | Error tier | Warning tier | Info/off tier |
|---|---|---|---|
| FluentValidation | `Severity.Error` | `Severity.Warning` | `Severity.Info` |
| ESLint | `"error"` (exit 1) | `"warn"` (exit 0) | `"off"` (disabled) |
| BPMN | Error Event (interrupting) | Escalation Event (non-critical) | Normal flow |
| Kubernetes | `Deny` | `Warn` | `Audit` |
| Roslyn | `error` (build fails) | `warning` (build warns) | `suggestion` / `silent` / `none` |

Cedar and XState are exceptions (binary only), but both serve narrower domains — authorization and state machines without rule evaluation, respectively.

**Implication for Precept:** Three tiers (error/warning/success or error/warning/info) is the natural model. Precept's existing verdict color system (rose/amber/emerald) already maps to this.

### Pattern 4: Warning means "continues but flags"

In every system that has a warning tier:
- **Error** blocks, fails, denies, or interrupts.
- **Warning** allows continuation but reports/flags/logs.
- **Info** is observational only.

| System | Error behavior | Warning behavior |
|---|---|---|
| FluentValidation | `IsValid` false, failures collection | Same (`IsValid` still false), severity metadata differs |
| ESLint | Exit code 1, CI fails | Exit code 0, CI passes, still reported |
| BPMN | Scope terminated, propagates | Execution continues, catch path also executes |
| Kubernetes | Request denied | Request allowed, warning header returned to client |
| Roslyn | Build fails | Build succeeds, warning shown |

**Critical nuance — FluentValidation:** FluentValidation is the outlier where even `Severity.Warning` failures still make `IsValid == false`. The consumer must check severity programmatically. This is an explicit design choice: binary validity is maintained regardless of severity metadata.

**Implication for Precept:** If Precept introduces constraint severity, the question is: does a `warning`-level constraint failure block the operation (FluentValidation model) or allow it to proceed (ESLint/Kubernetes/BPMN model)? This is a second architectural decision, downstream of the declaration-vs-configuration choice. It directly touches Precept's "prevention, not detection" core guarantee.

### Pattern 5: NO system bakes verdict severity into state declarations

This is the strongest single finding of the survey.

| System | State severity? | What states have instead |
|---|---|---|
| **XState** | NO | `tags` (arbitrary strings), `meta` (arbitrary objects), `type: 'final'` (structural) |
| **BPMN** | NO | Event types (Error End Event, Escalation Event) — severity is on events, not states |
| **Step Functions** | NO | `Succeed`/`Fail` state types — structural, not severity annotations |
| **UML** | NO | `FinalState` pseudostate — structural completion, not verdict |
| **Kubernetes** | N/A | No states |
| **Cedar** | N/A | No states |
| **Roslyn** | N/A | No states |

In every system with states (XState, BPMN, Step Functions, UML), verdict-like concepts attach to **events** or **outcomes**, not to **states**. States are structural containers — they represent where the entity IS, not whether that position is good or bad.

The closest pattern is Step Functions' `Succeed` and `Fail` state types, but these are structural node types with behavioral consequences (they terminate execution), not severity annotations on otherwise-normal states.

**Implication for Precept:** State verdict modifiers (`state Approved success`, `state Denied error`) are **genuinely novel territory with zero precedent in any comparable system**. This doesn't make them wrong — but it raises the bar for justification significantly. The burden of proof is on the feature: why should Precept innovate where no other system has found this necessary?

---

## The Architectural Decision: Declaration-Level vs Configuration-Level

Pattern 2 identifies this as THE key design choice. Both camps have strong precedent. Here is the analysis for Precept.

### Option A: Declaration-level severity (FluentValidation / BPMN model)

```precept
# Severity baked into the rule definition
invariant Balance >= 0 because "Balance non-negative"
invariant NotesLength < 500 because "Notes too long" warning
on Approve success
on Deny error
```

**Strengths:**
- **One-file completeness** (Philosophy): Reading the `.precept` file tells you everything — including which rules are critical vs advisory. No second artifact needed.
- **Principle #4 (locality of reference):** Severity lives with the rule it describes.
- **AI authoring** (Principle #12): The AI writes severity once, in the rule definition. No need to manage a separate config file.
- **Principle #1 (inspectability):** MCP compile returns severity as part of the typed structure. MCP inspect includes verdict intent in every event preview.

**Weaknesses:**
- Less flexible: cannot downgrade a critical rule to warning for a specific deployment without editing the definition.
- Couples authoring concern (what the rule is) with deployment concern (how strictly it's enforced).

### Option B: Configuration-level severity (ESLint / Kubernetes / Roslyn model)

```precept
# Rules are severity-free
invariant Balance >= 0 because "Balance non-negative"
invariant NotesLength < 500 because "Notes too long"
```
```json
// External config (e.g., .precept.config.json)
{
    "severity": {
        "NotesLength < 500": "warning"
    }
}
```

**Strengths:**
- Same rules, different enforcement per environment.
- Separation of concerns: rule author writes rules, deployment manager configures severity.
- Kubernetes and Roslyn demonstrate this works well at scale.

**Weaknesses:**
- **Breaks one-file completeness** — the defining Precept guarantee. A `.precept` file is supposed to be the complete contract. A second config file fragments the source of truth.
- Requires infrastructure for matching rules to config entries (rule naming/identification system).
- AI must manage two artifacts instead of one.
- Less inspectable: MCP compile output doesn't include severity unless config is also loaded.

### Recommendation: Option A (declaration-level) for Precept

Option A is the right fit for Precept because:

1. **One-file completeness is non-negotiable.** `docs/philosophy.md` positions "one file, complete rules" as a core differentiator. A second configuration file — even a small one — fragments the source of truth and opens a path to scattered governance.

2. **Precept's audience model favors declaration-level.** FluentValidation (declaration-level) targets rule authors who own both the rules and their importance. ESLint (configuration-level) targets separate rule authors (plugin developers) and rule consumers (project teams). Precept's precept authors own both the domain rules and their significance — the declaration-level model matches this ownership boundary.

3. **AI authoring is cleaner.** A single-file, declaration-level model means the AI generates severity in one pass. No need to manage, load, or coordinate a second artifact.

4. **Defaults handle the flexibility concern.** If severity defaults to `error` (following FluentValidation's precedent), most rules need no annotation at all. The author only annotates exceptions — the same ergonomics as `nullable` (default is non-null, opt-in to nullable).

---

## Compile-Time Provability Assessment

| Surface | Verification question | Compile check strength | Precedent systems | Rating |
|---|---|---|---|---|
| **Event verdict** | Do outcome shapes match declared intent? | Very strong (outcome shapes fully provable from transition rows) | FluentValidation per-rule, BPMN event types, Roslyn rule severity | **High** |
| **Rule verdict** | Does severity classification make structural sense? | Strong for error; medium for warning (enforcement behavior needs design) | FluentValidation `.WithSeverity()`, ESLint per-rule, Roslyn per-rule | **Medium** |
| **State verdict** | Is success endpoint reachable? Is error endpoint classified? | Weak (reachability exists but verdict-check is thin) | **Zero precedent** — no system annotates states with severity | **Low** |
| **Field verdict** | Does significance match constraint presence? | Weak (no meaningful structural verification) | None (no system has field-level severity) | **Low** |
| **Entity verdict** | Overall success/error ratio? | Very weak (heuristic only) | None | **Very Low** |

**Key finding:** Event verdict has **the strongest compile-time provability AND the broadest external precedent**. You can verify: "Event declared `success` but all outcomes are Rejected" is a compile error — the declared intent contradicts the declared structure.

State verdict has the **weakest combination**: weak compile-time provability AND zero external precedent. It is the riskiest surface to pursue.

---

## Interaction with Existing Diagnostics

### Implicit verdicts in TransitionOutcome

| Outcome | Verdict tint |
|---|---|
| Transition, NoTransition | **Success** |
| Unmatched | **Warning** |
| Rejected, ConstraintFailure | **Error** |

### Diagnostic reweighting with verdicts

| Code | Current | With event verdict | With state verdict |
|---|---|---|---|
| **C48** (unreachable) | Warning | No change | Escalate if success state is unreachable |
| **C50** (dead-end) | Warning | No change | Suppress if state marked error (intentional cessation) |
| **C51** (reject-only pair) | Warning | Escalate if event marked success | No change |
| **C52** (never succeeds) | Warning | Escalate if event marked success | No change |

### New diagnostics enabled by event verdict

| Code | Trigger | Severity |
|---|---|---|
| **C59** | Event marked `success` but produces Rejected/ConstraintFailure outcome | Error |
| **C60** | Event marked `error` but produces Transition/NoTransition outcome | Warning |
| **C61** | Success state marked but unreachable from initial | Warning |

C59 is the strongest new diagnostic: it detects a provable contradiction between declared intent and declared structure.

---

## Philosophy Alignment

Against the 12 design principles:

| Principle | Verdict modifier fit | Rating |
|---|---|---|
| 1. Deterministic, inspectable | Verdicts are deterministic and inspectable | ✓ |
| 2. English-ish | Keywords `success`, `error`, `warning` are domain language | ✓ |
| 3. Minimal ceremony | No new punctuation; keyword modifier position | ✓ |
| 4. Locality of reference | Declaration-level keeps severity near the rule | ✓ |
| 5. Data vs movement truth | Orthogonal — verdicts classify, don't create new truth categories | ✓ |
| 6. Collect-all for validation | Unchanged | ✓ |
| 7. Self-contained rows | Declaration-level modifiers | ✓ |
| 8. Sound static analysis | Event verdict adds sound checks (C59); others add heuristics | ✓ (events) / ⚠ (states) |
| 9. Tooling drives syntax | **Verdicts are specifically tooling-driven** | ✓✓ |
| 10. Consistent prepositions | Unchanged | ✓ |
| 11. `->` means do something | Unchanged | ✓ |
| 12. AI first-class | Verdict intent helps AI understand and author correctly | ✓✓ |

**Philosophy filter summary:**
- **Prevent, not detect:** Verdicts don't prevent — they classify outcomes. Semi-compatible for states/fields; strong for events (C59 prevents intent-structure contradictions). ⚠
- **One file, complete rules:** Declaration-level verdicts STRENGTHEN this guarantee — the file is more complete with severity metadata. ✓
- **Full inspectability:** MCP reports verdicts, preview shows them, diagnostics group by severity. ✓
- **Compile-time structural checking:** Event verdict adds real compile-time checks. Other surfaces add heuristic diagnostics. ⚠ for non-event surfaces.

**Verdict on philosophy alignment:** Strong fit for event verdicts (principles 1–12 aligned, adds real compile-time checks). Moderate fit for rule severity (useful but enforcement behavior needs design). Weak fit for state and field verdicts (intent-only, no precedent, minimal compile checks).

---

## Recommendation Tiers

### Tier 1 — Recommend for proposal (strong candidates)

| Modifier | Entity | Compile check | External precedent | Value |
|---|---|---|---|---|
| **Event verdict** | Event | Very strong | FluentValidation, BPMN, Roslyn | Highest — strong compile check, diagram impact, AI clarity |

**Syntax:** `on Approve success` / `on Deny error` / `on Escalate warning`

**Rationale:** Event verdict is the only surface with both strong compile-time provability AND broad external precedent. Outcome shapes are fully provable from declared transition rows. C59 provides a sound new diagnostic. Unambiguous semantics. Lowest design risk.

### Tier 2 — Explore further (conditional)

| Modifier | Entity | Compile check | External precedent | Blocker |
|---|---|---|---|---|
| **Rule verdict** | Rule/constraint | Strong for error; medium for warning | FluentValidation `.WithSeverity()`, ESLint per-rule, Roslyn | Enforcement question: does warning-severity failure block the operation? |
| **State verdict** | State | Weak | **Zero** — no comparable system does this | Must be justified from Precept-specific principles, not precedent. Novel territory. |

**State verdict note:** The absence of ANY external precedent for state-level severity is the single strongest finding of this research. The original v1 analysis ranked state verdict as Tier 2 based on tooling value alone. This externally-grounded rewrite retains Tier 2 but with a significantly upgraded caution: novel territory demands novel justification, and "useful for diagrams" is not sufficient justification for inventing a concept no other system has needed.

### Tier 3 — Defer or reject

| Modifier | Entity | Reason |
|---|---|---|
| **Field verdict** | Field | Weak compile check; no external precedent; overlaps with existing field modifiers |
| **Entity verdict** | Precept | Too high-level; better as runtime aggregation or external reporting |

---

## Concrete Recommendation

**Architecture:** Declaration-level (Option A). One-file completeness is non-negotiable.

**If proceeding:**

1. **Start with event verdict only** (Tier 1)
   - Syntax: `on EventName success` / `on EventName error` / `on EventName warning`
   - Compile check: verify outcome shapes match declared intent (C59, C60)
   - Default: no verdict (unclassified event) — fully backwards compatible
   - Tooling impact: diagram colors, preview rendering, MCP metadata, completions hints

2. **Defer rule verdict** (Tier 2) until event verdict feedback is gathered and the enforcement behavior question is resolved (does warning block or allow?)

3. **Approach state verdict with extreme caution** (Tier 2) — novel territory with zero precedent requires Precept-specific justification that goes beyond "useful for diagrams"

4. **Reject field and entity verdicts** (Tier 3) until clearer use case emerges

**Specific questions for Shane:**

1. **Should verdict modifiers proceed at all?** (vs. defer indefinitely)
2. **Start with event verdict alone?** (highest confidence, lowest risk)
3. **For rule severity (if explored later):** does a `warning`-level constraint failure block the operation (FluentValidation model) or allow it (Kubernetes/ESLint model)?
4. **For state verdict (if explored later):** what is the Precept-specific justification for innovating where no comparable system has?
5. **Timeline:** Add to M2 or M3 horizon?

---

## Key References

- [Structural Lifecycle Modifiers](structural-lifecycle-modifiers.md) — modifier taxonomy, role framework, and scope rule
- [semantic-visual-system-notes.md](../../../design/system/foundations/semantic-visual-system-notes.md) — verdict color definitions and visual system context
- [PreceptLanguageDesign.md](../../../docs/PreceptLanguageDesign.md) — 12 design principles
- [RuntimeApiDesign.md](../../../docs/RuntimeApiDesign.md) — TransitionOutcome enum
- [philosophy.md](../../../docs/philosophy.md) — product philosophy grounding

### External Documentation Sources

| System | URL | Date accessed |
|---|---|---|
| FluentValidation | docs.fluentvalidation.net/en/latest/severity.html | 2026-04-11 |
| ESLint | eslint.org/docs/latest/use/configure/rules | 2026-04-11 |
| XState v5 | stately.ai/docs/states | 2026-04-11 |
| BPMN 2.0 (Camunda) | Camunda BPMN documentation (error events, escalation events) | 2026-04-11 |
| Kubernetes | kubernetes.io (Validating Admission Policies) | 2026-04-11 |
| Cedar | docs.cedarpolicy.com | 2026-04-11 |
| Roslyn / .NET | learn.microsoft.com (.NET code analysis configuration) | 2026-04-11 |
