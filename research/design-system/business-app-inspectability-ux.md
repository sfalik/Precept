# Business Application Inspectability UX: External Research

**Author:** Elaine (UX Designer)  
**Date:** 2026-04-12  
**Purpose:** Catalog how real-world business applications surface system reasoning, state, rules, and constraints to end users — and extract patterns relevant to Precept's preview panel and event bar.

---

## 1. Workflow / BPM Platforms

### Salesforce — Path Component

**Pattern: Horizontal Progress Rail**

Salesforce's "Path" is a horizontal progress-indicator bar displayed at the top of a record page. Each step corresponds to a picklist value (e.g., Qualification → Needs Analysis → Proposal → Closed Won). The current step is highlighted in dark blue; completed steps are green; future steps are gray.

Key affordances:
- **Key Fields per Step** — admins configure which fields are "key fields" for each step. When a user clicks a step on the rail, a coaching panel slides down showing those fields and guidance text ("Guidance for Success"). This is progressive disclosure of context-relevant requirements.
- **Mark as Complete** — a button advances the record to the next step. There is no "blocked" state surfaced to the user; the button is always available. Validation happens on click, not preemptively.
- **Error State** — the `lightning-progress-indicator` component supports `has-error` on the current step, rendering the step icon as an error icon. But this is rare in production deployments.

**What works well:** The rail is always visible, lightweight, and communicates "where am I in the lifecycle?" at a glance. The per-step coaching panel is a clean progressive-disclosure mechanic.

**What fails:** Path does not explain *why* a transition is blocked. If a validation rule prevents stage advancement, the error appears as a red toast message after the user clicks — not inline on the Path itself. Users must hunt for which field failed validation.

**Relevance to Precept:** The Path rail maps directly to Precept's state badge. The coaching-panel-per-step is analogous to Precept's per-event guard reason display. Precept can do better by showing guard status *before* interaction, not after.

---

### ServiceNow — Activity Stream + State Controls

**Pattern: Prominent State Dropdown + Activity Timeline**

ServiceNow incident/change records display the current state as a dropdown selector in the form header. Available transitions are the dropdown options (e.g., "New → In Progress → Resolved → Closed"). The activity stream at the bottom of the form shows a chronological feed of all state changes, field updates, and comments.

Key affordances:
- **Workflow context bar** — activities and approval status are shown in the form footer, below the field section.
- **UI Policies** — admin-configured rules that dynamically show/hide/make-mandatory certain fields based on the current state. Users experience this as fields appearing or disappearing as they change the state selector.
- **Client scripts** — can inject alert dialogs when a user attempts a transition that requires additional conditions.

**What works well:** The state dropdown is always visible and actionable. UI Policies give the form a "living" feel — the form shapeshifts based on state, which naturally teaches users what's relevant at each stage.

**What fails:** The "why is this blocked?" experience is poor. If a mandatory field is empty when transitioning, the error appears as a red banner at the top of the page with a list of field names — but no context about why the field is required or which business rule demands it. The activity stream is comprehensive but overwhelming; it interleaves system-generated noise with human activity.

**Relevance to Precept:** ServiceNow's UI Policies are the closest analog to Precept's `edit` declarations that govern which fields are editable per state. The key gap in ServiceNow — and Precept's opportunity — is explaining *why* a field is required/locked at each stage.

---

### Pega — Case Lifecycle Visualization

**Pattern: Visual Case Lifecycle Bar**

Pega displays a horizontal "case lifecycle" visualization at the top of case forms. Each stage is a labeled segment. Within each stage, steps (tasks, approvals, parallel branches) are shown as sub-items. The visualization uses color coding: green for completed, blue for active, gray for future.

Key affordances:
- **Hover reveals step detail** — hovering over a step in the lifecycle bar shows its name, assignment, and status in a tooltip.
- **Parallel stages** — branches are visually rendered as forking paths in the lifecycle bar.
- **Urgency indicators** — SLA warnings are overlaid on the lifecycle as color shifts (yellow → red).

**What works well:** The lifecycle visualization provides a genuine "map of the process" at the top of every case, without requiring users to open a separate diagram. The user always knows where they are.

**What fails:** The lifecycle bar becomes cluttered for cases with many sub-stages. Complex branching logic is hard to parse in the horizontal format. Guard conditions are not surfaced — users only see "what's next," not "what must be true for next to happen."

**Relevance to Precept:** Validates the value of always-visible lifecycle position. Precept's state diagram + state badge fills this role. Pega's weakness — not surfacing guard logic — is exactly what Precept's event bar should solve.

---

### Camunda Tasklist

**Pattern: Task-Centric Work Queue**

Camunda's Tasklist presents human tasks as a worklist/inbox. Each task shows: task name, assignee, creation date, due date, and form variables. The user claims a task, fills in the form, and completes it. The broader BPMN process context is not shown in the task UI.

**What works well:** Radical simplicity for task workers — they see exactly one thing to do with no lifecycle complexity.

**What fails:** Zero inspectability. The user has no idea where this task sits in the larger process, what comes next, or why they're being asked to do this. This is the opposite extreme from Precept's goal.

**Relevance to Precept:** Anti-pattern. Camunda proves that hiding all process context creates a "keyhole" experience. Precept should never strip lifecycle context from the form view.

---

### Temporal — Workflow Execution History

**Pattern: Event History Timeline**

Temporal's Web UI shows workflow executions as a vertical timeline of events. Each event is a row: timestamp, event type (WorkflowExecutionStarted, ActivityTaskScheduled, ActivityTaskCompleted, TimerFired), and expandable payload details.

Key affordances:
- **Compact/JSON toggle** — each event row expands to show its full payload in JSON or a summary view.
- **Event-type filtering** — users can filter the timeline to show only specific event types.
- **Pending Activities panel** — a separate panel shows currently running activities with their attempt count and last failure reason.

**What works well:** Full historical transparency. Every decision the system made is traceable. For developers debugging workflow behavior, this is ideal.

**What fails:** This is a developer tool, not a business user tool. The event names are technical (ActivityTaskScheduled), the payloads are raw JSON, and there's no abstraction layer that translates system events into business language. Business users cannot use this interface.

**Relevance to Precept:** Temporal proves the value of full event-level traceability. Precept's `precept_fire` and `precept_inspect` tools provide the same level of detail but in structured business language (field names, state names, guard expressions). The preview panel should surface this structured traceability, not raw event logs.

---

## 2. Form Builders and Low-Code Tools

### PowerApps — Conditional Visibility with If()

**Pattern: Rule-Driven Form Composition**

PowerApps uses the `If()` function extensively in form control properties: `Visible`, `DisplayMode`, `Required`, `DefaultValue`. A field's visibility and editability are expressions evaluated against the app's data context.

Key affordances:
- **Dynamic `Visible` property** — `If(ThisItem.Status = "Active", true, false)` hides irrelevant fields.
- **`DisplayMode` property** — `If(ThisItem.Status = "Closed", DisplayMode.Disabled, DisplayMode.Edit)` locks fields without hiding them. Locked fields are grayed out.
- **Inline validation** — the `IsMatch()` and `IsBlank()` functions drive per-field validation indicators.

**What works well:** The conditional composition creates forms that feel tailored to the current state without any explicit "state awareness" chrome. Users simply see the right fields at the right time.

**What fails:** There's no mechanism to tell the user *why* a field is hidden or locked. If a field is invisible because the record is in "Closed" status, nothing communicates that. Users must infer the relationship between state and field visibility from repeated use. PowerApps has zero "inspectability" beyond the maker's design surface.

**Relevance to Precept:** PowerApps validates that conditional field visibility (Precept's `edit` declarations) is the right mechanism. Precept's advantage: the language explicitly names *which state* governs *which fields*, and the preview panel can surface that relationship. PowerApps hides it inside formulas; Precept can make it visible.

---

### Appian — Interface Expressions

**Pattern: Declarative Form Rules**

Appian's SAIL (Self-Assembling Interface Layer) uses `if()`, `a!match()`, and `choose()` to drive component properties. Components have `showWhen`, `readOnly`, and `required` parameters that accept expressions.

Key affordances:
- **`showWhen` parameter** — controls component visibility based on rule evaluation.
- **`readOnly` parameter** — dynamically locks components. Uses `labelPosition: "JUSTIFIED"` for read-only display vs. `"ABOVE"` for editable.
- **Validation groups** — validators fire when a user submits, showing red messages below the offending field.

**What works well:** The validation messaging is per-field and contextual. Error messages appear directly below the field that failed validation, with human-readable text configured by the app designer.

**What fails:** Same as PowerApps — the *rules* that drive visibility/editability are completely hidden from end users. There's no "why is this field locked?" affordance.

**Relevance to Precept:** The per-field validation message placement (below the field, in context) is the right pattern for Precept's assert/reject messages. The gap in Appian (no rule transparency) is Precept's differentiator.

---

### Airtable — Field Configuration + View Filters

**Pattern: Transparent Schema + Flexible Views**

Airtable surfaces field types, descriptions, and validation rules in the field configuration panel. Each field shows its type (Single select, Number, Formula, etc.) and optional description explaining its purpose.

Key affordances:
- **Field descriptions** — visible on hover in the column header, providing context.
- **Conditional coloring** — cells can be colored based on rules (e.g., red if overdue).
- **Form views** — Airtable's form view automatically includes field descriptions as helper text below each input.

**What works well:** The field description as always-available context is simple and effective. Users can hover any column header to understand what a field means and what values are valid.

**What fails:** Airtable doesn't have a workflow/state concept, so there's no transition logic to inspect. Its "inspectability" is purely structural (schema, types, descriptions) not behavioral.

**Relevance to Precept:** Airtable's field descriptions map to what Precept could surface as field-level metadata in the preview form: field name, type, current constraints, and (in Precept's case) which state controls editability.

---

## 3. Issue Trackers and Project Management

### Jira — Workflow Conditions and Transition Screens

**Pattern: Action Buttons with Transition Conditions**

Jira renders available workflow transitions as buttons at the top of the issue view. Each button triggers a transition. Transitions can have:
- **Conditions** — rules that determine whether the transition button appears at all (e.g., "Only assignee can transition").
- **Validators** — rules that check data on transition (e.g., "Resolution must be set").
- **Transition screens** — modal dialogs that collect required fields before completing the transition.

Key affordances:
- **Visible transitions** — only permitted transitions appear as buttons. If a condition fails, the button simply doesn't render.
- **Transition screens** — pop up a form collecting required data before the transition completes.

**What works well:** The transition button approach is clean — users see available actions and click one. Transition screens collect required data at exactly the right moment.

**What fails badly:** When a condition hides a transition button, users get zero explanation of why that action isn't available. The button simply doesn't exist. There's no "You can't do X because Y" feedback — the action just vanishes. This is worse than a disabled button because users don't even know the action *could* exist. Validators that fail on transition produce a red error banner, but the error messages are often technical ("Field X is required") without business context.

**Relevance to Precept:** Jira's pattern of hiding vs. disabling transitions is a critical anti-pattern for Precept. **Precept should always show all declared events, including blocked ones, with their guard reason.** Hiding blocked events destroys inspectability — users can't reason about what's possible if they can't see what's not currently possible.

---

### Linear — Status Sidebar with Keyboard Shortcuts

**Pattern: Status as a Prominent Sidebar Property**

Linear shows issue status as a colored dot + label in the issue sidebar. Clicking it opens a dropdown of all statuses with keyboard shortcut hints. Transitions are not constrained — any status can be set from any other status.

Key affordances:
- **Status dropdown** — lists all statuses with visual grouping (Backlog, Started, Completed, Cancelled).
- **Keyboard shortcuts** — pressing a number key directly changes status.
- **No blocked states** — Linear deliberately avoids workflow constraints. Any transition is always available.

**What works well:** Extreme simplicity. The status dropdown is fast and predictable. Users never wonder "why can't I do X?" because they always can.

**What fails:** Zero enforcement of business rules. Linear trusts users entirely. This works for dev teams but fails for regulated workflows where certain transitions must be blocked.

**Relevance to Precept:** Linear demonstrates the UX ideal that Precept must approximate: making every action feel immediately available and transparent. Where Precept adds guard constraints (which Linear deliberately omits), it must compensate by making the constraint reasoning equally transparent.

---

### GitHub Issues + Projects

**Pattern: Custom Fields + Kanban Board with Status Automation**

GitHub Projects renders issues on a Kanban board where columns represent status values (e.g., Todo, In Progress, Done). Moving a card between columns changes its status.

Key affordances:
- **Drag-and-drop** — spatial position communicates status. Moving a card is the transition.
- **Built-in automations** — "When an issue is closed, move to Done" provides automatic transitions.
- **Custom fields** — metadata fields (priority, iteration, etc.) appear in the issue side panel.
- **No transition constraints** — any card can be moved to any column.

**What works well:** Spatial encoding of state is immediately legible. The board view provides a "whole lifecycle at a glance" that individual issue views cannot.

**What fails:** Like Linear, GitHub Projects deliberately avoids enforcing transition rules. No concept of guards, conditions, or blocked transitions.

**Relevance to Precept:** The Kanban board view is the strongest argument for Precept's concept #11 (Kanban board preview). Spatial position answers "where am I?" faster than any text label. But Precept must augment the spatial metaphor with guard visibility — something no existing Kanban tool provides.

---

## 4. Document / Approval Workflows

### DocuSign — Envelope Status Tracking

**Pattern: Step-by-Step Signer Progress**

DocuSign shows document (envelope) status as a series of signer steps: Sent → Delivered → Viewed → Signed → Completed. Each signer is a row with their own status progression.

Key affordances:
- **Per-recipient status** — each signer has an independent progress indicator: Needs to Sign, Signed, Declined.
- **Timeline events** — a detailed history shows when the envelope was sent, when each recipient opened it, authentication events, and signing events.
- **Action required banner** — recipients who need to act see a prominent "Review Document" banner.
- **Completion certificate** — a tamper-proof audit trail is generated upon completion.

**What works well:** The per-recipient progress breakdown is excellent inspectability — the sender knows exactly which signer is blocking completion and when they last interacted. The audit trail provides full historical transparency.

**What fails:** For the signer (not the sender), there's limited context. The signer sees "Sign here" tags but doesn't understand the broader workflow. Why is their signature needed? What happens after they sign? The signer experience is a keyhole view.

**Relevance to Precept:** DocuSign's per-recipient status row is analogous to an event row in Precept's event bar. Each row answers: who needs to act, what's their status, what's blocking. Precept's event rows should answer: what's the event, what's the guard status, what's the target state.

---

### Ironclad — Contract Workflow Visualization

**Pattern: Workflow Builder + Status Dashboard**

Ironclad shows contract workflows as a visual pipeline: stages like Draft → Internal Review → Legal Review → Counterparty Review → Signed → Executed. Each stage has configurable approval steps.

Key affordances:
- **Visual pipeline** — a horizontal bar with stage names, color-coded by completion status.
- **Bottleneck highlighting** — stages where the contract has been waiting longest are called out.
- **Approval status** — within each stage, individual approvers are listed with their Approved/Pending/Rejected status.

**What works well:** The combination of macro-level pipeline view and micro-level approval detail gives users appropriate context at both zoom levels.

**What fails:** The approval rules themselves (e.g., "requires VP approval if contract value > $100K") are not surfaced to the user viewing the workflow. Users see that VP approval is needed but not *why*.

**Relevance to Precept:** Validates the pattern of surfacing both lifecycle position (Precept's state badge) and per-action detail (guard reasons). Ironclad's gap — not showing the *rule* behind the approval requirement — is exactly what Precept can solve.

---

## 5. Insurance / Claims / Legal Case Management

### Guidewire ClaimCenter — Claims Lifecycle Management

**Pattern: Wizard-Driven Intake + Workplan-Guided Processing**

Guidewire ClaimCenter governs the entire claims lifecycle: Intake → Assignment → Evaluation → Reserve/Pay → Negotiation → Closure. The UI surfaces this through:

Key affordances:
- **Workplans** — structured task checklists that guide adjusters through required claim-handling steps. Each task has a status (Open, Complete, Skipped) and may have dependencies on other tasks.
- **Business rules for closure** — "Dynamic business rules ensure all appropriate steps are taken before claim closure." Users attempting to close a claim with incomplete required tasks are blocked with a summary of what's missing.
- **Activity streams** — chronological history of all actions taken on the claim.
- **Wizard-based intake** — "response-driven questions" where the next question depends on the answer to the previous one. This is staged disclosure driven by business rules.

**What works well:** The workplan is the strongest inspectability pattern in this category. It makes the system's expected process visible as an explicit checklist. Users can see what the system requires before attempting to close the claim. The closure-blocking summary ("you can't close because tasks X, Y, Z are incomplete") is exactly the "why is this blocked?" pattern that most tools lack.

**What fails:** Workplans are configured by admins, not derived from the claim's actual rule set. If a business rule prevents closure for a reason not captured in the workplan (e.g., a reserve threshold check), the user gets a generic error, not a workplan-visible explanation.

**Relevance to Precept:** Guidewire's workplan is the closest real-world analog to Precept's guard-aware event bar. A workplan task = a guard condition. The "closure blocked because requirements X, Y, Z are not met" = showing guard failures before the user clicks the event button. Precept should aim for Guidewire's workplan-level transparency but with rules derived automatically from the precept definition, not manually configured.

---

### Clio — Legal Case Management

**Pattern: Status Pipeline + Task Templates**

Clio (legal practice management) shows matters (cases) with a status pipeline: Open → In Progress → Pending → Closed. Matters have task templates — preset lists of tasks that must be completed.

Key affordances:
- **Status-driven task visibility** — different task templates are associated with different matter stages.
- **Calendar-integrated deadlines** — tasks have due dates that appear on the calendar, providing temporal urgency cues.
- **Practice area templates** — reusable workflows for common case types.

**What works well:** Task templates give lawyers a clear picture of what needs to happen at each stage. The calendar integration provides temporal pressure without additional UI.

**What fails:** Like many legal tools, Clio doesn't enforce transitions — any status can be set at any time. Task completion is advisory, not blocking. The system tracks but doesn't prevent.

**Relevance to Precept:** Validates Precept's approach of having the DSL define what's required (guards, constraints), not just track what happened. Clio tracks; Precept enforces.

---

## 6. Banking / Fintech

### Stripe Dashboard — Payment Lifecycle States

**Pattern: Explicit State Machine with Lifecycle Documentation**

Stripe's PaymentIntent has a well-documented state machine: `requires_payment_method` → `requires_confirmation` → `requires_action` → `processing` → `succeeded` / `canceled`. This lifecycle is surfaced in the Dashboard as:

Key affordances:
- **Status pill** — each payment shows a colored status badge (green for succeeded, yellow for processing, red for failed).
- **Timeline panel** — clicking a payment reveals a chronological event timeline: "PaymentIntent created," "Payment method attached," "3D Secure authentication initiated," "Payment succeeded."
- **Failure diagnosis** — failed payments show `last_payment_error` with a human-readable decline reason (e.g., "Your card was declined" with a decline code like `insufficient_funds`).
- **Next actions** — the `next_action` property on the PaymentIntent tells the integration (and the dashboard) exactly what step is needed next.
- **Dispute panel** — unresolved disputes surface as notifications on the Home page.

**What works well:** Stripe's state machine is the gold standard for developer-facing inspectability. Every state has a clear name, every transition has a documented trigger, and every failure has a structured reason. The Dashboard translates this into visual status pills and event timelines that are legible to operations staff, not just developers.

**What fails:** The Dashboard is designed for operations teams familiar with payment processing, not end consumers. A cardholder sees "Your card was declined" — they don't see the state machine. The inspectability is one-sided: the operator sees everything; the consumer sees almost nothing.

**Relevance to Precept:** Stripe's state machine documentation approach (explicit states, named transitions, structured failure reasons) is Precept's exact model. The Dashboard's event timeline is the implementation of Precept's concept #01 (Timeline Debugger). Stripe proves that this approach works at massive scale for operations users.

---

### nCino — Loan Origination Workflow

**Pattern: Stage-Gated Pipeline with Checklist Requirements**

nCino (built on Salesforce) manages loan origination through a pipeline: Application → Processing → Underwriting → Approval → Closing → Funding. Each stage has a set of requirements that must be met before advancing.

Key affordances:
- **Pipeline view** — loans are displayed in a Kanban-style board grouped by stage.
- **Stage requirements checklist** — each stage shows a list of required documents, data fields, and approvals with completion indicators.
- **Compliance rules** — regulatory requirements are surfaced as checklist items with explanations of why they're needed.

**What works well:** The stage requirements checklist is the most inspectable pattern in banking UX. Users know exactly what's needed to advance because the system shows them a concrete list: "Upload W-2," "Verify employment," "Credit check: complete." This is declarative and transparent.

**What fails:** The checklist items are statically configured per-stage, not dynamically derived from the loan's actual data. Edge cases where a loan needs unusual requirements aren't automatically surfaced.

**Relevance to Precept:** nCino's per-stage checklist is another real-world analog to Precept's guard conditions. The key insight: presenting guards as a checklist (items with check/uncheck status) is more legible than presenting them as boolean expressions.

---

## 7. State Machine Visualization Tools

### Stately.ai / XState — Visual Editor + Inspector

**Pattern: Interactive Statechart Diagram with Live Simulation**

Stately Studio renders XState machines as interactive statecharts. States are rounded rectangles, transitions are arrows labeled with event names, guards appear as "IF" labels on transitions.

Key affordances:
- **Visual guard annotations** — guarded transitions show "IF guardName" on the arrow. Multiple guarded transitions from the same event show "IF guard1" and "ELSE" labels, making branching visible.
- **Simulate mode** — users can click events to advance the machine, watching the active state highlight move through the diagram. Disabled events (those with failing guards) are not clickable.
- **Inspector** — the Stately Inspector connects to running applications and shows real-time state changes as a combination of statechart diagram (current state highlighted) and sequence diagram (event history as a message sequence chart).
- **Context display** — the current machine context (data) is displayed alongside the diagram.

**What works well:** The "IF / ELSE" annotation on guarded transitions is the most elegant guard-visibility pattern I found in any tool. It uses the universal programming concept of conditional branching, rendered visually on the transition arrow. Users can read the diagram and understand: "When event X happens, IF guard Y is true, go to state A; ELSE go to state B."

**What fails:** Guard conditions are displayed as names (`isValid`, `sentimentGood`) not as evaluated boolean expressions. In Simulate mode, you can't see why a guard is failing — only that the transition didn't fire. The Inspector shows events and state changes but not guard evaluation details. This is a developer tool — the diagram is too technical for business users.

**Relevance to Precept:** Stately's guard annotation is directly relevant. Precept's event bar should show each event's guard status with the same clarity: event name, guard condition, and guard evaluation result (true/false with the actual expression). Precept already has the structured data to go further than Stately — showing not just the guard name but its evaluated reason text.

---

### AWS Step Functions Console

**Pattern: Execution Graph with Step-Level Status**

AWS Step Functions renders state machine executions as a directed graph. Each state is a box, transitions are arrows. During execution:

Key affordances:
- **Per-step status coloring** — green for succeeded, blue for in-progress, red for failed, gray for not-yet-reached.
- **Step detail panel** — clicking a step shows its input, output, start time, end time, and error information.
- **Execution history table** — a companion view shows all state transitions as a timestamped table.
- **Error details** — failed steps show the error name, cause, and stack trace.

**What works well:** The combination of graph view (spatial, at-a-glance) and detail panel (deep dive on demand) is the right two-level progressive disclosure. Users get the "where am I?" from the graph and the "what happened?" from the panel.

**What fails:** Like Temporal, this is a developer/ops tool. The graph renders technical state names, the detail panel shows raw JSON, and error messages are stack traces. No business-language abstraction.

**Relevance to Precept:** Step Functions validates the graph-plus-detail-panel layout that Precept's state diagram + inspector panel already uses. The key Precept advantage: Precept's data is in business language by default (field names, state names, guard expressions in the DSL), not in technical implementation language.

---

### Node-RED — Flow Editor

**Pattern: Visual Dataflow with Node Status Indicators**

Node-RED's flow editor shows processing nodes connected by wires. Each node has a status indicator (small colored dot) showing its current state: gray (idle), green (connected/OK), yellow (warning), red (error).

Key affordances:
- **Per-node status** — nodes show their state with a small colored dot and optional short text label below the node.
- **Debug sidebar** — a dedicated debug panel shows message payloads flowing through debug nodes.
- **Node configuration** — double-clicking a node opens its configuration panel.

**What works well:** The per-node status indicator is a simple, lightweight inspectability pattern. At a glance, users can see which nodes are active, idle, or errored. The debug sidebar provides deep payload inspection on demand.

**What fails:** Status indicators are node-level, not transition-level. You can see that a node errored but not which specific message or condition caused the error without checking the debug sidebar.

**Relevance to Precept:** Node-RED's per-node status dot is analogous to per-event status indicators in Precept's event bar. Green = event available, red = blocked. The simplicity of the colored-dot pattern is worth preserving.

---

## 8. Cross-Category Pattern Analysis

### How Disabled/Unavailable Actions Communicate "Why"

| Approach | Products Using It | User Experience | Precept Relevance |
|----------|------------------|-----------------|-------------------|
| **Hide the action entirely** | Jira (conditions), Camunda | Worst — users don't know the action exists | Anti-pattern. Precept must show all events. |
| **Disable with no explanation** | Most form builders, many BPM tools | Poor — users know something's wrong but not what | Current Precept state (disabled button + title tooltip). Insufficient. |
| **Disable with tooltip on hover** | Some Salesforce customizations, Sandrina Pereira's pattern | Mediocre — requires hover, invisible to keyboard/touch users | Acceptable as *supplementary* pattern; not sufficient alone. |
| **Disable with adjacent text** | Smashing Magazine's recommended pattern, Guidewire workplan | Good — explanation always visible near the button | **Precept's target.** Inline guard reason below/beside event button. |
| **Keep enabled, validate on click** | Salesforce Path, Linear, most modern form patterns | Good for simple cases — errors appear immediately. Fails for complex multi-condition blocks. | Useful for events with many guards, but Precept should show guard status pre-click when possible. |
| **Checklist of requirements** | Guidewire, nCino, DocuSign (per-signer) | Best — users see a concrete list of what's needed/missing | **Precept's aspiration.** Present guards as a checklist where each guard is an item with pass/fail status. |

### How Target State / Outcome Is Previewed

| Approach | Products Using It | User Experience |
|----------|------------------|-----------------|
| **Label on button** | Jira ("In Progress"), Linear status dropdown | Minimal — target state is the button label |
| **Arrow notation on transition** | Stately.ai, Step Functions graph | Clear for diagram views but requires spatial literacy |
| **Inline text below button** | Precept Variant E (Inline Outcome Strip) | Best for forms — always visible, zero interaction cost |
| **Hover-triggered preview** | Precept Variant D (State Badge Preview) | Good progressive enhancement — extends existing badge mental model |
| **Side-by-side diff** | Precept Concept #06 (Dual-Pane Diff) | Best for complex field changes but high screen real estate cost |

### Progressive Disclosure Tiers for Inspectability

Based on NNGroup's progressive disclosure research and the patterns observed across these tools, the recommended disclosure tiers for Precept's preview panel are:

**Tier 1 — Always Visible (Zero Interaction)**
- Current state badge
- All event names with enabled/blocked status indicator (green/rose dot)
- Target state label per event ("→ TargetState")
- Guard failure summary (short text: "Blocked: FieldX is required")

**Tier 2 — On Hover/Focus (Low-Cost Interaction)**
- Full guard expression text
- State badge preview (Variant D hover effect)
- Field changes that would result from the transition

**Tier 3 — On Click/Expand (Deliberate Investigation)**
- Complete transition trace (from/event/guards/actions/to)
- Historical timeline of past transitions
- Full constraint evaluation details

### Validation and Error Surfacing

| Pattern | Where Observed | Effectiveness |
|---------|---------------|---------------|
| **Toast/banner after action** | Salesforce, ServiceNow | Poor — ephemeral, no spatial association with the cause |
| **Red border on offending field** | Appian, PowerApps, most form tools | Good for field-level validation but doesn't explain *why* |
| **Per-field message below input** | Appian validation groups | Good — spatial proximity makes cause-effect obvious |
| **Constraint message in dedicated zone** | Precept's gold constraint area (from mockup) | Good — separates business-rule messages from field-level validation |
| **Guard reason inline with action** | Precept Variant E, Guidewire workplan | Best — answers "why can't I do this?" at the exact decision point |

---

## 9. Key Findings and Recommendations for Precept

### Finding 1: The "Why" Gap Is Universal

Across every category, the most common failure is **not explaining why an action is blocked**. Jira hides blocked transitions entirely. Salesforce shows errors only after clicking. ServiceNow shows field-level errors without business context. Even sophisticated tools like Stately only show that a guard failed, not why. **This is Precept's single biggest differentiator opportunity**: because guards are declared in the DSL with human-readable expressions, Precept can always show the "why."

### Finding 2: Checklists Beat Boolean Expressions

When users need to understand what's blocking an action, the most effective pattern is a **checklist of requirements with pass/fail status** (Guidewire workplans, nCino stage requirements, DocuSign per-signer status). This is more legible than showing a boolean expression like `PickupWindowOpen == false`. Precept should consider rendering guards as a requirements checklist where each guard condition is a line item with ✓ or ✗.

### Finding 3: Always-Visible Beats Hover/Click

NNGroup's research on progressive disclosure, combined with the evidence from Smashing Magazine's disabled buttons research, confirms: **information that users need for their primary decision should be always visible, not hidden behind hover or click**. For Precept's event bar, this means: event name, blocked/enabled status, guard reason summary, and target state should all be visible at rest — not hidden behind tooltips. This validates Variant E (Inline Outcome Strip) as the correct baseline.

### Finding 4: Form Shape-Shifting Is a Powerful Teaching Tool

ServiceNow's UI Policies and PowerApps' conditional Visible/DisplayMode demonstrate that **forms that change shape based on state naturally teach users what's relevant at each stage**. Users don't need a separate explanation of "why is this field locked?" if the field only appears when it's editable. Precept already does this with `edit` declarations — the preview panel should make the field set visibly change when previewing target states.

### Finding 5: State Machine Visualization + Form Is the Winning Combination

No tool in the research combines state machine visualization with form-level inspectability in a single view. Stately has diagrams without forms. ServiceNow has forms without diagrams. Guidewire has workplans without state graphs. **Precept's combined state diagram + form + event bar is genuinely novel.** The preview panel's three-zone layout (diagram, fields, events) is the correct architecture — the question is only how to maximize the information density in each zone without overwhelming users.

### Finding 6: Stripe's Event Timeline Is the Gold Standard for Developer Inspectability

Stripe's PaymentIntent lifecycle documentation + Dashboard timeline panel is the most complete inspectability implementation found. Every state has a name, every transition has a trigger, every failure has a structured reason. Precept should aim for this level of structured traceability, translated into business language rather than payment processing jargon.

---

## 10. Pattern Glossary for Precept Implementation

| Pattern Name | Description | Source Products | Precept Application |
|-------------|-------------|-----------------|---------------------|
| **Progress Rail** | Horizontal bar showing lifecycle stages with current position highlighted | Salesforce Path, Pega, Ironclad | State diagram or horizontal state rail in preview header |
| **Guard Checklist** | List of requirements with pass/fail indicators | Guidewire workplan, nCino | Event detail expansion showing each guard as a checklist item |
| **Inline Outcome Strip** | Always-visible target state and guard summary below each action button | Original (our Variant E) | Default event bar rendering |
| **State Badge Preview** | Hover-triggered state badge swap showing destination state | Original (our Variant D) | Progressive enhancement on hover |
| **Event Timeline** | Chronological list of state changes and events | Stripe Dashboard, Temporal, DocuSign | Concept #01 (Timeline Debugger) as secondary view |
| **Transition Buttons** | Available actions rendered as buttons; blocked actions hidden or disabled | Jira, Salesforce, ServiceNow | Event bar buttons — but always shown, never hidden |
| **Per-Field Validation Message** | Error/warning text below the offending input field | Appian, PowerApps | Assert/reject messages inline with field section |
| **Form Shape-Shifting** | Fields appear/disappear/lock based on current state | ServiceNow UI Policies, PowerApps | `edit` declarations governing field editability per state |
| **IF/ELSE Guard Annotation** | Visual annotation on transition arrows showing guard conditions | Stately.ai | State diagram edge labels showing guard text |
| **Failure Diagnosis Panel** | Structured error details with error code, human-readable message, and suggested action | Stripe (decline reasons), Step Functions (error details) | Reject/assert message panel with constraint text |
