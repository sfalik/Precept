# How Products Communicate Inspectability to Users

> Research for Precept's visual system: how business products make "why" visible without feeling technical.

**Date:** 2026-04-12  
**Author:** J. Peterman (Brand/DevRel)  
**Requested by:** Shane  
**Context:** Precept's core promise is "full inspectability" — the engine exposes the complete reasoning behind every possible action. The challenge: how do you surface rule evaluation, guard conditions, state transitions, and constraint status in a way that feels like a polished business tool, not a debugger?

---

## 1. Transparency as a Product Feature

### Products that explicitly market visibility

**Stripe** markets transparency through *exhaustive status reporting with structured decline reasons.* Every failed payment returns a specific decline code — not "payment failed" but "The card has insufficient funds to complete the purchase" or "The transaction requires authentication such as 3D Secure." Each code includes a human-readable explanation and a next-step recommendation. Their documentation frames this as empowering merchants: "Learn about Stripe decline codes and how to resolve them when a charge fails." The pattern: *machine-readable code + human explanation + actionable next step.*

> Stripe decline code pattern: `insufficient_funds` → "The card has insufficient funds to complete the purchase." → "The customer needs to use an alternative payment method."  
> — Source: [docs.stripe.com/declines/codes](https://docs.stripe.com/declines/codes)

Stripe also uses `PaymentIntent` status as a first-class concept. The payment lifecycle is fully inspectable — `requires_payment_method`, `requires_action`, `processing`, `succeeded`, `canceled` — and the `last_payment_error` property gives structured access to *why* a payment failed. The pattern is: **status is never implicit; the system's current position is always named and queryable.**

**Datadog** frames visibility as the core product value: *"See it all in one place."* Their product page leads with: "Your servers, your clouds, your metrics, your apps, your team. Together." Every feature bullet connects visibility to action: "Trace requests from end to end across distributed systems" / "Navigate seamlessly between logs, metrics, and request traces." The framing is not "we monitor" but "we make visible."

> "See across systems, apps, and services."  
> "Get full visibility into modern applications."  
> "Analyze and explore log data in context."  
> — Source: [datadoghq.com/product/](https://www.datadoghq.com/product/)

**Honeycomb** positions observability as answering questions you didn't know you'd ask. Their definition: "Observability is the ability to investigate a system by asking any question about its behavior without needing to predict those questions in advance." They distinguish this from monitoring (predefined thresholds) — observability is *open-ended inspection.*

Key Honeycomb language:
- "Reveals both **why** a problem is happening and **who** specifically is impacted"
- "Automatically detecting hidden patterns with BubbleUp"
- "Speed up debugging by... revealing the hidden attributes that are statistically unique to your selection"
- Logs provide the "what and why"; metrics the "how much and when"; traces the "flow and where"

> "A system is considered observable when you can understand what's happening inside based solely on its external outputs, without needing to add new instrumentation each time a new question comes up."  
> — Source: [honeycomb.io/what-is-observability](https://www.honeycomb.io/what-is-observability)

**Linear** communicates system reasoning through explicit *blocking/blocked relationships.* Issues marked "blocking" show a red flag; issues marked "blocked by" show an orange flag. When the blocking issue resolves, the flag turns green and moves to "Related." The status of the *dependency* is communicated through color, position, and automatic state change — not a log or a message, but a persistent visual indicator on the entity itself.

> "Once the blocking issue has been resolved, the blocked issue flag turns green and moves under Related."  
> — Source: [linear.app/docs/issue-relations](https://linear.app/docs/issue-relations)

### Language patterns across transparency-forward products

| Product | How they frame transparency | Key vocabulary |
|---------|---------------------------|----------------|
| Stripe | Structured error codes with explanations and next steps | "decline code," "reason," "next steps" |
| Datadog | "See it all in one place" — visibility is the product | "visibility," "full context," "in context" |
| Honeycomb | Open-ended questioning of system behavior | "observe," "investigate," "why," "who" |
| Linear | Dependencies as persistent visual indicators on entities | "blocked," "blocking," "relations" |
| Plaid | Financial data transparency — structured categories for transactions | "category," "confidence," "detailed" |

### Relevance to Precept

Precept's Inspect operation is structurally closest to Honeycomb's observability model: open-ended, query-without-prediction, always-available. But the *communication* should follow Stripe's pattern: **named status + human reason + actionable context.** Every constraint violation already carries a mandatory `reason`; the visual system should expose that reason with the same structure Stripe uses for decline codes.

---

## 2. Progressive Disclosure in Business UX

### The UX principle (Nielsen Norman Group)

Jakob Nielsen's canonical definition:

> "Progressive disclosure defers advanced or rarely used features to a secondary screen, making applications easier to learn and less error-prone."  
> — Source: [nngroup.com/articles/progressive-disclosure/](https://www.nngroup.com/articles/progressive-disclosure/)

Key findings from NNG research:
- Progressive disclosure improves **learnability, efficiency of use, and error rate** — three of usability's five components
- "The very fact that something appears on the initial display tells users that it's important"
- Research shows users understand systems *better* when they can prioritize features — progressive disclosure doesn't create limiting mental models, it creates accurate ones
- **Two levels** (primary/secondary) work well; three or more cause users to get lost
- Labels for progression must set clear expectations: strong "information scent"

**Staged disclosure** (wizards) is a variant: linear steps through a sequence, each showing a subset. Key difference: in staged disclosure, users access *all* levels; in progressive disclosure, most users stay on the initial display.

### How automation tools implement progressive disclosure

**Zapier** uses a three-level disclosure model for execution history:
1. **Zap runs list** — Status badge per run (success/error/filtered/held/stopped), Zap name, timestamp. High-level scan.
2. **Zap run details** — Click a run to see step-by-step execution: which trigger fired, what data came in, what each action did, version number used.
3. **Raw data** — Expandable payload per step showing input/output data.

> "Each Zap run in the list displays the Zap run status so you can see whether your Zap ran successfully or not."  
> — Source: [help.zapier.com](https://help.zapier.com/hc/en-us/articles/8496291148685)

The pattern: **badge-level status → step-level trace → raw data.** Users can stop at any level.

**Make.com (Integromat)** has a similar execution-history model but adds a visual execution trace overlaid on the scenario diagram — you see which modules executed and which errored, rendered on the same graph you designed in. This is the closest analogy to Precept's state-diagram preview: *the inspection surface is the authoring surface.*

### Three-tier disclosure model for business rules

Synthesizing across products, the pattern that emerges:

| Tier | What's shown | Who needs it | Example |
|------|-------------|-------------|---------|
| **Summary** | Status badge + one-line verdict | Everyone, every time | ✅ "Approved" / ⛔ "Blocked: Missing required fields" |
| **Detail** | Which rules applied, what passed/failed, which transitions are available | Business analyst, support agent | "Guard `amount > 100` failed (amount = 75). Transition to Approved is blocked." |
| **Full trace** | Complete evaluation chain — every guard, invariant, assertion evaluated | Developer, domain modeler | Step-by-step: guard → mutation → invariant check → assertion check → result |

### Relevance to Precept

Precept's Inspect API already returns the full trace (tier 3). The visual system needs to present tiers 1 and 2 *by default* and tier 3 on demand. The summary tier should use the mandatory `reason` strings from guard/constraint declarations. The detail tier should show the condition expression and its evaluated result. The full trace is only for "show me everything."

---

## 3. "Why" Messaging in Business Apps

### Products that explicitly tell users "why"

**Stripe** is the gold standard. Every decline code answers "why did this fail?" with a specific reason and "what should I do about it?" with a next step. The dual structure — *reason + recommended action* — is the pattern that builds confidence rather than frustration.

**Jira / Atlassian** uses inline messages that follow a strict content pattern:
- **Warning messages**: "Appear before we request people to take action... clearly communicate what will happen if they proceed, and provide an alternative where possible."
- **Error messages**: "Explain the problem and provide people with a next step or an alternative. Keep the message simple and direct, and avoid confusing people with technical details."
- **Key principle**: "Avoid blame and accept if something is our fault — 'we're having trouble connecting' rather than 'you're having connection issues.'"

> "Let people know what's causing the error, rather than writing a general error message that works for a number of things."  
> — Source: [atlassian.design/components/inline-message/usage](https://atlassian.design/components/inline-message/usage)

**Linear's blocking model** communicates "why can't I work on this" through persistent visual annotations — the blocked-by relationship is always visible on the entity, not buried in a log. This is *structural why*, not *temporal why.*

**IBM Carbon** classifies notifications by their communicative purpose:
- **Informational**: "Provide additional information to users that may not be tied to their current action"
- **Success**: "Confirm a task was completed as expected"
- **Warning**: "Inform users that they are taking actions that are not desirable or might have unexpected results"
- **Error**: "Inform users of an error or critical failure and optionally block the user from proceeding until the issue has been resolved"

Carbon's callout component is specifically designed for *preemptive explanation* — "used to highlight important information contextually within the contents of the page" that "cannot be dismissed." This maps directly to Precept's invariant display: constraints that are always true and always visible.

> Carbon callout: "Designed to draw attention to important information that can prevent a negative user experience. Additionally, it can be used to emphasize details that guide users toward making informed decisions."  
> — Source: [carbondesignsystem.com/components/notification/usage](https://carbondesignsystem.com/components/notification/usage/)

### Patterns of "why" communication across business apps

| Pattern | Example products | Communication structure |
|---------|-----------------|----------------------|
| **Structured decline codes** | Stripe, Plaid | Machine code + human text + next step |
| **Blocking relationships** | Linear, Jira | Visual indicator on entity showing what depends on what |
| **Preemptive callouts** | IBM Carbon, Atlassian | Non-dismissible contextual explanation before action |
| **Validation with reason** | Gusto, Rippling (HR tools) | Field-level "This field is required because [reason]" |
| **Decision trace** | Zapier, Make.com | Step-by-step what happened and why |

### The "because" pattern

The most effective "why" messages follow a consistent grammatical structure:

- "**This action is blocked because** [reason from the domain, not the system]"
- "**This field is required because** [business rule, not validation rule]"
- "**The status changed because** [event that fired + what the guard evaluated to]"

The key distinction: **domain language, not system language.** "Amount must exceed $100 for automatic approval" is a business reason. "Guard predicate `amount > 100` evaluated to false" is a debugger message. Both convey the same fact; only the first builds trust.

### Relevance to Precept

Precept's mandatory `reason` strings on invariants and assertions are exactly the "business reason" slot. The visual system should surface these reasons in the "because" pattern: "This transition is blocked **because** [reason string from the guard or invariant]." The expression itself (tier 2) should be available on hover or expand, and the evaluation trace (tier 3) on explicit request.

---

## 4. Trust Calibration

### How products build trust by showing their work

**Nielsen Norman Group's Visibility of System Status (Heuristic #1)**:

> "When, in a real-life relationship with a person, that person withholds information from us or makes decisions unilaterally, we start losing trust and feel that the relationship is no longer on equal footing. The same thing happens when we interact with a system."  
> — Aurora Harley, NNG, 2018

> "Sites and apps should clearly communicate to users what the system's state is — no action with consequences to users should be taken without informing them."  
> — Source: [nngroup.com/articles/visibility-system-status/](https://www.nngroup.com/articles/visibility-system-status/)

Key NNG insight on trust: The worst experience is when items *silently disappear* with no explanation. The best: "explicitly communicate the current system's status — which items are no longer available — and then allow the user to either remove them from the list or keep them visible for future reference." Applied to Precept: when a transition is blocked, *don't just hide the button* — show it as blocked with a reason.

**Credit score explanations** (Credit Karma, Experian, FICO) use a documented pattern called "adverse action factors" — legally required explanations of what's hurting your score. The trust pattern:
- **The score**: one number, highly visible (summary tier)
- **The factors**: 4-5 specific items ranked by impact — "High credit utilization," "Too many recent inquiries" (detail tier)
- **The explanation per factor**: what it means, how to improve it, historical trend (trace tier)

This is the same three-tier model, applied to trust-critical financial data. Users trust the score *because* they can see what's behind it.

**TurboTax / Wealthsimple Tax** build trust through *showing the calculation.* Every line item links back to the input that generated it. Users can trace from "You owe $X" back through "Federal tax on $Y taxable income" → "Taxable income = gross income — deductions" → "These were your deductions." The entire chain is navigable.

**Lemonade** (insurance) markets transparency as a brand differentiator — their "Transparency Chronicle" publishes what percentage of premiums goes to claims, overhead, and reinsurance. In-product, they show claim processing status with timestamps and reasons for each state change. The pattern: *open the books, not just the result.*

### What builds trust vs. what overwhelms

| Builds trust | Overwhelms |
|-------------|-----------|
| Named status with a reason in domain language | Raw evaluation logs |
| Progressive detail on demand | All detail up front |
| Persistent indicators on the entity itself | Transient notifications that disappear |
| "Because" explanations that reference business rules | "Because" explanations that reference code |
| Stable, predictable UI positions for status info | Status info that moves around |
| Summary first, detail on drill-down | Flat list of everything that happened |

### Relevance to Precept

Precept's visual system should follow the credit-score pattern: **always show the verdict, always offer the factors, make the full calculation available.** The mandatory `reason` strings are the "factors." The guard expressions are the "calculation." The three-tier disclosure model maps directly:

1. **Verdict**: Event button badge — ✅ available / ⛔ blocked / ⚠ conditional
2. **Factors**: Hover/panel — "Blocked because: [reason from invariant]" / "Available when: [guard summary]"
3. **Calculation**: Expand — full guard expression, data values, evaluation result

---

## 5. Design System Documentation of Status/State Patterns

### Atlassian Design System

**Three components for status communication**, layered by severity and persistence:

| Component | Persistence | Severity | Use case |
|-----------|------------|----------|----------|
| **Banner** | Non-dismissible, top of screen | Critical (warnings/errors only) | System-wide issues |
| **Inline message** | Persistent, in-context | All levels | Contextual information near the relevant element |
| **Flag** | Dismissible, bottom-left overlay | All levels | Event-driven confirmations and alerts |

Key Atlassian content guidelines:
- "Always be clear, concise and, where possible, give follow up actions to help people resolve the issue"
- "For warning and error messages, always try to avoid dead ends and provide people with information on how to proceed to resolve the issue"
- "Avoid blame and accept if something is our fault"
- Flag titles "should always summarize the reason for the flag"

> Source: [atlassian.design/components/flag/usage](https://atlassian.design/components/flag/usage), [atlassian.design/components/inline-message/usage](https://atlassian.design/components/inline-message/usage), [atlassian.design/components/banner/usage](https://atlassian.design/components/banner/usage)

### IBM Carbon Design System

**Status indicator pattern** — the most comprehensive status documentation of any public design system:

- **Four indicator types**: Icon indicators (high attention), shape indicators (compact), badge indicators (counts), differential indicators (deltas)
- **Three severity levels**: High attention (immediate action required), medium attention (feedback, no immediate action), low attention (informational)
- **Seven-color status palette**: Red (danger/error), green (success/normal), orange (serious warning), yellow (regular warning), blue (informational/progress), purple (outlier/undefined), gray (draft/not started)
- **Accessibility requirement**: "Status indicators should rely on at least two of the following elements: color, shape, or symbol" — never color alone
- **Cognitive load warning**: "Avoid using status indicators when no user action is required... having more than five or six indicators can overwhelm users"

> "When multiple statuses are consolidated, use the highest-attention color to represent the group."  
> — Source: [carbondesignsystem.com/patterns/status-indicator-pattern/](https://carbondesignsystem.com/patterns/status-indicator-pattern/)

Carbon's **callout notification** is unique — non-dismissible, loads with page content, provides preemptive explanation. "Used to highlight important information that loads with the contents of the page, is placed contextually, and cannot be dismissed." Only uses informational and warning statuses (no success/error — those are reactive).

### GitHub Primer

**Banner component** with tone variants:
- Info, Warning, Critical, Success
- Can be dismissible or persistent
- Supports primary and secondary action buttons
- Title can be visually hidden while remaining accessible

> Source: [primer.style/components/banner](https://primer.style/components/banner)

### Salesforce Lightning Design System

Salesforce uses **scoped notifications** (inline to a component) and **global notifications** (top of page). Their Flow Builder includes debug mode with step-by-step execution trace — the closest enterprise precedent to Precept's inspect preview.

### Cross-system patterns

| Pattern | Atlassian | Carbon | Primer | Salesforce |
|---------|-----------|--------|--------|------------|
| Non-dismissible critical alerts | Banner | High-contrast notification | Critical Banner | Global alert |
| In-context explanation | Inline message | Callout | — | Scoped notification |
| Status badges | Lozenge component | Shape/icon indicators | Label/StateLabel | Badge |
| Blocked/disabled states | Grayed + tooltip | Disabled pattern | Disabled states | Conditional visibility |
| Progressive detail | Expand/collapse sections | Accordion pattern | Details component | Expandable sections |

### Relevance to Precept

Precept's visual system should draw from:

1. **Carbon's callout** for invariant display — non-dismissible, contextual, preemptive. Invariants are always in effect; their display should be always-present, not reactive. Callouts are the only notification type designed for this exact use case.

2. **Carbon's status indicator hierarchy** for event button states — icon indicators for high-attention states (blocked transitions), shape indicators for scanning large sets of events.

3. **Atlassian's inline message** for per-field constraint feedback — in-context, persistent, with the ability to expand for more detail.

4. **Carbon's severity levels** as a model for Precept's visual priority — high attention for blocked/rejected, medium for conditional/guarded, low for available/satisfied.

5. **Carbon's consolidation rule** ("use the highest-attention color to represent the group") for summarizing multiple constraint results into a single entity-level status.

---

## Summary: Communication Patterns for Precept

### The three-tier model

Every inspectable surface in Precept should support three tiers of disclosure:

| Tier | Name | Content | Default visibility |
|------|------|---------|-------------------|
| 1 | **Verdict** | Status badge + one-line summary using mandatory `reason` strings | Always visible |
| 2 | **Factors** | Which guards/invariants/assertions apply, their conditions, pass/fail status | On hover or panel open |
| 3 | **Calculation** | Full evaluation: expression text, data values substituted, boolean result per step | On explicit expand |

### The "because" grammar

All blocking/rejection messages should follow the pattern:

> **[Action] is [status] because [reason in domain language].**

Examples:
- "Submit is **blocked** because the claim amount exceeds the policy limit."
- "Amount is **locked** because the status is Approved."
- "Cancel is **available** because the deadline has not passed."

### The four messaging types

Mapped to Precept's constructs:

| Type | Precept construct | Communication pattern | Reference model |
|------|------------------|----------------------|----------------|
| **Always-on constraints** | Invariants | Non-dismissible callout (Carbon callout pattern) | "This rule is always in effect: [reason]" |
| **Blocked transitions** | Failed guards / rejected events | Status badge + inline reason (Stripe decline pattern) | "Blocked because: [reason]" |
| **Available actions** | Passing guards | Action affordance with summary (Linear model) | Button/badge showing "available" with conditions |
| **State context** | Current state + active assertions | Persistent status indicator (Carbon status indicator) | Named state with its active rule set |

### What to avoid

1. **Don't hide blocked actions** — show them as blocked with a reason (NNG trust principle)
2. **Don't show everything at once** — progressive disclosure, two tiers max for most users (NNG guideline)
3. **Don't use system language at tier 1** — "Guard predicate evaluated to false" is tier 3; "Amount must exceed $100" is tier 1
4. **Don't use color alone** — Carbon's accessibility rule: at least two of color, shape, symbol (WCAG compliance)
5. **Don't make status transient** — use persistent indicators on the entity, not toast notifications that disappear (NNG trust principle)
6. **Don't exceed 5-6 indicators** on a single view without consolidation (Carbon cognitive load guideline)

---

## Sources

| Source | URL | What was used |
|--------|-----|---------------|
| Stripe decline codes | [docs.stripe.com/declines/codes](https://docs.stripe.com/declines/codes) | Structured reason + next-step pattern |
| Stripe payment status | [docs.stripe.com/payments/payment-intents/verifying-status](https://docs.stripe.com/payments/payment-intents/verifying-status) | Named lifecycle states, status inspection |
| Datadog product page | [datadoghq.com/product/](https://www.datadoghq.com/product/) | "Visibility" as product framing |
| Honeycomb observability | [honeycomb.io/what-is-observability](https://www.honeycomb.io/what-is-observability) | Open-ended inspectability definition, "why" framing |
| Linear issue relations | [linear.app/docs/issue-relations](https://linear.app/docs/issue-relations) | Blocking/blocked visual indicators |
| NNG progressive disclosure | [nngroup.com/articles/progressive-disclosure/](https://www.nngroup.com/articles/progressive-disclosure/) | Two-tier disclosure model, staged vs progressive |
| NNG visibility of system status | [nngroup.com/articles/visibility-system-status/](https://www.nngroup.com/articles/visibility-system-status/) | Trust through transparency, "don't blindfold your users" |
| Atlassian Design (flag) | [atlassian.design/components/flag/usage](https://atlassian.design/components/flag/usage) | Content guidelines for status messaging |
| Atlassian Design (inline message) | [atlassian.design/components/inline-message/usage](https://atlassian.design/components/inline-message/usage) | In-context explanation patterns |
| Atlassian Design (banner) | [atlassian.design/components/banner/usage](https://atlassian.design/components/banner/usage) | Critical non-dismissible alerts |
| IBM Carbon status indicators | [carbondesignsystem.com/patterns/status-indicator-pattern/](https://carbondesignsystem.com/patterns/status-indicator-pattern/) | Indicator hierarchy, severity levels, color palette, accessibility |
| IBM Carbon notifications | [carbondesignsystem.com/components/notification/usage/](https://carbondesignsystem.com/components/notification/usage/) | Callout pattern for preemptive explanation |
| GitHub Primer Banner | [primer.style/components/banner](https://primer.style/components/banner) | Tone variants, dismissibility |
| Zapier Zap history | [help.zapier.com](https://help.zapier.com/hc/en-us/articles/8496291148685) | Three-tier execution history (list → detail → raw) |
