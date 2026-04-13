# Architectural Patterns for Runtime Inspectability in Business Tools

**Author:** Frank (Lead/Architect & Language Designer)  
**Date:** 2026-04-12  
**Requested by:** Shane  
**Purpose:** External research on how business software architecturally surfaces runtime state, rules, and transition reasoning to end users. Findings inform Precept's inspector panel and preview surface design.

---

## Executive Summary

This research surveys five categories of business software to understand how they architecturally expose runtime state, rules, and transition reasoning to their UI layers. The key finding is a **universal pattern of progressive disclosure** — every mature system separates "what's happening now" (summary) from "why it's happening" (detail), and the best systems make the "why" as accessible as the "what."

The systems cluster into two fundamentally different architectural approaches:

1. **Snapshot-centric** (XState, Precept): The runtime emits a single structured object representing the entire inspectable state at a moment in time. The UI consumes this one object and distributes its contents across panels.
2. **Event-stream-centric** (Temporal, Camunda, AWS Step Functions): The runtime emits a chronological stream of lifecycle events. The UI reconstructs current state by replaying or querying the event history.

Precept's `InspectResult` is firmly in category 1, which is architecturally superior for form/task UIs where the user needs to see "what can I do right now" — not "what happened over time." The event-stream approach is better suited for monitoring and debugging long-running processes.

---

## 1. State Machine Runtimes with UI Layers

### 1.1 XState v5 + Stately Inspector

**Inspection data model:**

XState's inspection API emits four structured event types to an observer:

| Event Type | Payload | Purpose |
|---|---|---|
| `@xstate.actor` | `actorRef`, `rootId` | Actor creation notification |
| `@xstate.event` | `actorRef`, `event`, `sourceRef`, `rootId` | Inter-actor communication |
| `@xstate.snapshot` | `actorRef`, `snapshot`, `event`, `rootId` | State change notification |
| `@xstate.microstep` | `value`, `event`, `transitions[]` | Step-by-step transition trace |

The **snapshot** is the key object for UI consumption. It contains:
- `value` — current state value (string for simple states, nested object for hierarchical)
- `context` — extended state data (the equivalent of Precept's field values)
- `meta` — metadata keyed by state node ID (can carry UI hints like form views)
- `status` — `'active'` | `'done'` | `'error'`
- `output` — final output when status is `'done'`
- `children` — spawned/invoked child actors

**Guard evaluation visibility:**

XState exposes a critical API: `state.can(event)` — a synchronous method that evaluates whether an event would cause a state change, including running all guard functions. This is directly analogous to Precept's `Inspect` reporting which events are available.

However, XState does NOT expose *why* a guard failed. `state.can()` returns `true` or `false`, not `{ result: false, reason: "isValid guard failed because feedback.length === 0" }`. This is a significant gap compared to Precept's `InspectResult`, which includes guard evaluation details.

**Microstep events** are the closest XState comes to transition reasoning traces — they show intermediate states during eventless (`always`) transitions with the specific transition objects (including `eventType` and `target` arrays). But these are developer-facing debugging tools, not end-user-facing.

**How the UI consumes it:**

Stately Inspector operates as a **separate panel/window** that receives inspection events via postMessage (browser) or WebSocket (Node.js). The Inspector auto-generates:
- **State machine diagram** — highlights current state, available transitions
- **Sequence diagram** — shows actor communication over time

The UI is entirely separate from the application UI — it's a developer tool, not an end-user tool. The application itself doesn't have a built-in inspector panel.

**Progressive disclosure:**
- Level 1: Visual state machine with current state highlighted
- Level 2: Sequence diagram showing event flow
- Level 3: Raw snapshot/event data

**Architectural lessons for Precept:**
- The snapshot-centric model (`getSnapshot()`) maps directly to Precept's `InspectResult` approach — this is the right architecture for "current state" UIs
- XState's `state.can(event)` is a useful pattern but Precept already goes further by explaining *why* events are available or unavailable
- XState's `meta` property on state nodes (arbitrary metadata per state) is an interesting pattern — it lets machine authors attach UI hints at design time. Precept could potentially support state-level metadata for similar purposes
- The separation of Inspector from the application UI is a development tool pattern, not an end-user pattern — Precept needs both

### 1.2 Temporal + Web UI

**Inspection data model:**

Temporal's inspectability is fundamentally **event-history-centric**. The Web UI consumes:

- **Workflow execution metadata** — Start Time, Close Time, Duration, Run ID, Workflow Type, Task Queue, State Transitions count, SDK version
- **Event History** — A chronological log of ~40 different event types (WorkflowExecutionStarted, ActivityTaskScheduled, ActivityTaskCompleted, TimerFired, etc.)
- **Input and Results** — Function arguments and return values (available after workflow completes)
- **Pending Activities** — Summary of active/pending activity executions
- **Workers** — Currently polling workers with count
- **Call Stack** — Captured from `__stack_trace` query (requires running worker)
- **Custom Search Attributes** — User-defined metadata for filtering and visibility

**How the UI consumes it:**

The Web UI presents four views of the event history:
1. **Timeline** — Chronological event list with summaries
2. **All** — Complete event detail
3. **Compact** — Logical grouping of Activities, Signals, Timers
4. **JSON** — Raw event history

The **Relationships** tab shows parent/child workflow hierarchy as a tree. This is a structural inspection tool rather than rule-evaluation inspection.

**Progressive disclosure:**
- Level 1: Workflow list with status badges (Running, Completed, Failed, Timed Out)
- Level 2: Workflow detail with metadata + current position in the process
- Level 3: Full event history with per-event detail drill-down
- Level 4: Raw JSON download

**Guard/transition reasoning:**

Temporal has **no concept of guard evaluation explanation**. The workflow either advances or it doesn't. Failures surface as "incidents" — the error message from code execution. There is no declarative guard system to introspect.

**Architectural lessons for Precept:**
- Temporal's **Visibility** subsystem (SQL-like filtering of workflow executions) is useful for fleet-level monitoring but irrelevant to Precept's single-entity inspection model
- The **4-view pattern** (timeline/all/compact/JSON) is a good progressive disclosure hierarchy — Precept could offer summary/detail/raw views of `InspectResult`
- Temporal's **User Metadata** (static Summary + dynamic Current Details) is interesting — it's human-readable annotation layered on top of machine state. Precept's state descriptions could serve a similar role
- The event-stream architecture is wrong for Precept's use case — Precept's point-in-time snapshot is more appropriate for form UIs

### 1.3 AWS Step Functions + Console

**Inspection data model:**

AWS Step Functions' API provides:

- `DescribeExecution` — Returns execution status, input, output, start/stop times, state machine ARN
- `GetExecutionHistory` — Returns ordered list of `HistoryEvent` objects, each with a type (e.g., `TaskStateEntered`, `TaskSucceeded`, `ChoiceStateEntered`, `ExecutionFailed`), timestamp, and event-specific detail structure
- Per-state input/output inspection — For each state in the execution, the console shows input received, output produced, and any error

**How the UI consumes it:**

The Step Functions console provides:
- **Graph inspector** — Visual state machine with current state highlighted (green = succeeded, blue = running, red = failed)
- **Step detail panel** — For each state: input JSON, output JSON, exception details, timestamp, duration
- **Execution event history** — Chronological event list (similar to Temporal)

The **Graph inspector** is the most distinctive feature: it overlays execution status directly onto the state machine definition graph, so users can see exactly where execution is and which path was taken.

**Guard/transition reasoning:**

For `Choice` states (Step Functions' equivalent of guarded transitions), the console shows:
- Which choice rule was evaluated
- The input data that was tested
- Which branch was taken

This is a **rudimentary explanation facility** — it shows "this condition was true so this path was taken" but doesn't show all evaluated conditions or why others were false.

**Architectural lessons for Precept:**
- The **graph overlay pattern** (execution state overlaid on definition graph) is directly applicable to Precept's preview diagram — highlighting the current state and coloring transition paths
- The per-state **input/output/error trio** is a useful structure for Precept's event fire trace
- Choice state reasoning (showing which branch was taken and why) maps to Precept's guard evaluation trace in `InspectResult`

### 1.4 Camunda Operate + Tasklist

**Inspection data model:**

Camunda's architecture separates concerns across two tools:

**Operate** (monitoring/debugging):
- Process instance detail with BPMN diagram overlay showing current position
- Variable inspection and live editing per process instance
- Incident diagnosis with human-readable error messages (e.g., "Expected to evaluate condition 'orderValue>=100' successfully, but failed because: Cannot compare values of different types: STRING and INTEGER")
- Lifecycle event log with intents: `ELEMENT_ACTIVATING`, `ELEMENT_ACTIVATED`, `ELEMENT_COMPLETING`, `ELEMENT_COMPLETED`, `SEQUENCE_FLOW_TAKEN`

**Tasklist** (end-user task forms):
- Assigned tasks with context about what the user needs to do
- Extends via custom task applications and the Orchestration Cluster API
- Does NOT show "why" a task was assigned or full process context

**DMN Decision Tables:**
- Decision evaluation results show which rules (rows) in the decision table matched
- Hit policy determines whether first match, unique match, or all matches are returned
- The evaluation trace shows input → rule match → output

**Progressive disclosure:**
- Level 1 (Tasklist): "You have a task to do" — minimal context
- Level 2 (Operate dashboard): Process instance counts by status, incident counts
- Level 3 (Operate detail): BPMN diagram with execution position, variables, incidents
- Level 4 (Operate incidents): Full error diagnosis with variable editing and retry

**Guard/transition reasoning:**

Camunda's incident messages are the closest to guard evaluation explanation: they show what condition was being evaluated and why it failed. However, this only happens on *failure* — there's no proactive "here's why this transition is available" explanation.

**Architectural lessons for Precept:**
- The **BPMN overlay pattern** (execution state on process diagram) is the same as Step Functions and directly applicable
- The **incident diagnosis pattern** — showing the exact expression that failed, the data types involved, and providing inline variable editing + retry — is the most sophisticated "why it failed" UI in this survey and extremely relevant to Precept
- The **Operate/Tasklist split** reflects a real architectural tension: monitoring tools show full context, task forms show minimal context. Precept's inspector panel needs to serve both roles
- Variable editing in Operate maps to Precept's editable fields — showing which fields are editable, what constraints apply, and allowing direct manipulation

---

## 2. Business Rules Engines with Explanation Facilities

### 2.1 Drools (Apache KIE)

**Inspection data model:**

Drools provides runtime inspectability through two mechanisms:

**1. Event Listeners (programmatic):**

```java
public interface AgendaEventListener {
    void matchCreated(MatchCreatedEvent event);
    void matchCancelled(MatchCancelledEvent event);
    void beforeMatchFired(BeforeMatchFiredEvent event);
    void afterMatchFired(AfterMatchFiredEvent event);
    void agendaGroupPopped(AgendaGroupPoppedEvent event);
    void agendaGroupPushed(AgendaGroupPushedEvent event);
    // ... ruleflow group events
}

public interface RuleRuntimeEventListener {
    void objectInserted(ObjectInsertedEvent event);
    void objectUpdated(ObjectUpdatedEvent event);
    void objectDeleted(ObjectDeletedEvent event);
}
```

These listeners fire during rule evaluation and provide a **stream of events** about what the engine is doing: which rules matched, which facts changed, which rules fired. But they are API-level hooks, not structured explanation objects.

**2. Queries and Live Queries:**

Drools supports declarative queries that retrieve fact sets based on patterns:
```java
QueryResults results = ksession.getQueryResults("people under the age of 21");
for (QueryResultsRow row : results) {
    Person person = (Person) row.get("person");
}
```

Live queries maintain open views with change listeners — `rowAdded`, `rowUpdated`, `rowRemoved` — enabling reactive UIs that track rule evaluation results over time.

**How the UI consumes it:**

Drools historically provided KIE Workbench (now part of Apache KIE) with:
- Rule authoring UI
- Test scenarios
- Audit logging via event listeners
- Decision table editor

The audit trail is fundamentally a **log stream** — a chronological record of rule matches and firings, not a structured "here's why this decision was made" object.

**Guard/transition reasoning:**

Drools has **inference and truth maintenance** — the engine can explain *logical insertions* by tracing back through the chain of rules that justified a fact. When a justifying rule's conditions become false, the logically inserted fact is automatically retracted. This is the closest thing to "explanation" in Drools, but it's an internal engine mechanism, not an end-user API.

**Architectural lessons for Precept:**
- Drools' event listener model is a low-level instrumentation API — Precept's `InspectResult` is architecturally superior because it's a structured, point-in-time "here's everything you need to know" object rather than a stream of events you have to reassemble
- Drools' **Live Queries** pattern (reactive views with change listeners) is interesting for UI consumption — if Precept ever supports subscriptions to state changes, this pattern is relevant
- The **truth maintenance** concept (automatically retracting facts when their justification disappears) is philosophically aligned with Precept's guard system — but Precept's approach of preventing invalid states rather than cleaning up after them is stronger
- Drools lacks a structured "explanation API" — this is a gap that Precept fills with its guard evaluation results in `InspectResult`

### 2.2 IBM ODM (Operational Decision Manager)

**Inspection data model:**

IBM ODM's Decision Center provides an **execution trace** capability:

- **Decision trace** — Records which rules were evaluated, which rules fired, the order of execution, input/output data for each rule, and the decision service parameters
- **Rule execution trace** — For each fired rule: the rule name, the agenda it belonged to, the bound variables/objects, and the resulting actions
- **Decision table trace** — For decision tables: which rows matched, the input values tested against each cell, the hit policy applied, and the output values produced

The trace is returned as a structured object (XML/JSON) alongside the decision result.

**How the UI consumes it:**

Decision Center presents:
- **Test suite results** with pass/fail indicators
- **Execution trace viewer** that shows the decision flow with rule-by-rule detail
- **Decision table trace** showing highlighted matching rows

The trace is typically consumed in a **testing/validation context** rather than in a production runtime UI. Business analysts use it to verify that rules behave correctly.

**Progressive disclosure:**
- Level 1: Decision outcome (approved/rejected/etc.)
- Level 2: Which rules fired and in what order
- Level 3: Per-rule detail: bound objects, condition evaluation, action taken

**Architectural lessons for Precept:**
- The **structured trace object** returned alongside the decision result is the closest external analog to Precept's approach — a single structured response containing both the result and the explanation
- The testing-vs-production distinction matters: ODM's traces are primarily for testing, not for production UIs. Precept's `InspectResult` is designed for real-time production use, which is architecturally more ambitious
- The **decision table trace** (showing which rows matched) maps directly to Precept's transition row evaluation in `InspectResult` — showing which `from/on` rows had their guards evaluate to true

### 2.3 FICO Blaze Advisor / InRule

**Inspection data model:**

Both systems provide **rule tracing** capabilities:

- **FICO Blaze Advisor**: Execution audit trail with rule-by-rule activation records, fact modification history, and decision tree path tracing
- **InRule**: Rule tracing API that captures rule execution paths, condition evaluations (true/false per condition), and data changes. The trace can be configured at different verbosity levels

InRule's approach is notable for allowing **selective tracing** — you can enable tracing for specific rules or rule sets rather than everything, which reduces performance overhead.

**Architectural lessons for Precept:**
- **Selective verbosity** is a useful pattern — Precept's `InspectResult` is already "all at once" but a future API version might benefit from detail level parameters
- The condition evaluation detail (true/false per guard condition) is what Precept already provides in guard evaluation results
- These systems confirm that structured explanation objects (not just log streams) are the right architectural direction

---

## 3. Workflow Platforms' Task Forms

### 3.1 Pega (Case Portal)

**Inspection data model:**

Pega's Case Management architecture provides:

- **Case summary** — Current stage, status, urgency, owner, key data fields
- **Case history** — Audit trail of all actions taken
- **Flow action forms** — Dynamic forms generated from the case type definition, showing only the fields relevant to the current step
- **Decision strategies** — Pega's decisioning engine (Pega Decision Hub) can explain AI-driven decisions by showing which strategy components contributed to the outcome

**How the UI consumes it:**

The data contract between Pega's workflow engine and the task form is:
- The case type definition determines which fields appear on the form
- The current stage/step determines which fields are editable vs. read-only
- Validation rules are evaluated client-side and server-side
- The form shows **just what's needed for the current step** — not the full case context

**Progressive disclosure:**
- Level 1 (Task form): "Fill in these fields" — minimal context
- Level 2 (Case summary): Current stage, status, key metrics
- Level 3 (Case history): Full audit trail
- Level 4 (Decision explanation): Why this case was routed this way

**Architectural lessons for Precept:**
- Pega's **stage-driven form rendering** (showing different fields at different lifecycle stages) maps to Precept's state-dependent editable fields — the form should show what's relevant now, not everything all at once
- The **progressive context disclosure** from "do this task" to "here's the full history" is the right pattern for Precept's inspector panel

### 3.2 Appian / OutSystems / Mendix

**Inspection data model:**

These low-code workflow platforms share a common pattern:

- **Process model** defines the workflow stages and transitions
- **Interface/form** is a separate artifact that connects to the process model
- **Data contract** between process and form is typically: current task metadata + entity data + permitted actions
- **Validation** is declarative (field-level rules) rather than explained

**How the UI consumes it:**

The typical data contract for a task form is:
```
{
  task: { id, name, assignee, dueDate, priority },
  processInstance: { id, currentStage, status },
  entityData: { /* current field values */ },
  permissions: { canApprove: true, canReject: true, canEdit: ["field1", "field2"] }
}
```

This is a "do this next" contract, not a "here's why" contract. The form knows what the user can do but not why those permissions exist.

**Architectural lessons for Precept:**
- The **permissions object** (which fields can be edited, which actions are available) is a simplified version of Precept's `InspectResult` — Precept adds the "why" layer on top
- These platforms confirm that task-form consumers want: (1) current data, (2) available actions, (3) editable fields. Precept's `InspectResult` covers all three plus guard/constraint explanations

---

## 4. Low-Code Platform Inspector Panels

### 4.1 Retool

**Inspection data model:**

Retool provides a **left panel data inspector** at design time:
- Component tree showing all components and their current values
- Query inspector showing data source queries, their parameters, and results
- State inspector showing temporary state variables and their values
- Transformer inspector showing data transformation pipelines

At runtime, Retool provides:
- A **debugger** that shows query execution logs, errors, and network requests
- Component-level property inspection (what data is bound where)

**Design time vs. runtime:** The full inspector is available only at design time. At runtime, end users see the rendered form — not the inspector. This is the standard pattern for low-code tools.

### 4.2 Budibase

**Inspection data model:**

Budibase provides:
- **Binding explorer panel** — Shows all available bindings (data sources, component values, URL parameters, app state) with their current values
- **State explorer interface** — Shows app state variables with read/write capabilities
- **Conditional UI** — Components can be conditionally shown/hidden based on data bindings, with the binding logic visible in the design panel

Budibase's binding system uses Handlebars syntax: `{{ Query 1.Customer.name }}` — bindings are visible and editable inline.

### 4.3 ToolJet

**Inspection data model:**

ToolJet provides:
- Design-time component inspector with property panels
- Query debugger showing execution results
- Application state viewer

**Common pattern across low-code tools:**

All three platforms share:
1. **Design-time full inspection** — All state, bindings, query results visible
2. **Runtime minimal inspection** — End users see rendered UI only
3. **No "why" explanation** — These tools show current state but don't explain validation rules or transition guards

**Architectural lessons for Precept:**
- Low-code platforms confirm that **the inspector is a development/admin tool, not an end-user tool** in their world. Precept's unique value proposition is making inspection available to domain users, not just developers
- The **binding explorer** pattern (showing what data feeds into what component) is relevant to Precept's field → constraint → display mapping
- The **design-time vs. runtime** split is important: Precept's preview is the "design-time" view, while the form runtime should surface appropriate inspection data to business users

---

## 5. Runtime Inspection API Comparison

### Inspection Response Shape Comparison

| System | Response Shape | Contents | Real-time | Explanation Depth |
|---|---|---|---|---|
| **Precept** `InspectResult` | Single structured object | Current state, available events (with guard results), editable fields (with constraint status), transition targets | Yes (point-in-time) | Deep — per-guard, per-constraint |
| **XState** `snapshot` | Single structured object | Current state value, context, meta, status, children | Yes (reactive) | Shallow — `can()` returns bool only |
| **Temporal** Event History | Ordered event stream | Lifecycle events, metadata, search attributes | Yes (streaming) | None — no declarative guards |
| **AWS Step Functions** History | Ordered event stream | Per-state I/O, choice evaluation, timestamps | Near-real-time | Medium — choice branch reasoning |
| **Camunda** Operate data | Event stream + variable store | Lifecycle events, variables, incidents | Near-real-time | Medium — incident diagnosis |
| **Drools** Listener events | Event stream | Rule match/fire/cancel, fact insert/update/delete | Yes (listener callbacks) | Low — which rules fired, not why conditions match |
| **IBM ODM** Trace | Structured trace object | Decision flow, per-rule evaluation, bound objects | Post-execution | Deep — per-rule evaluation detail |

### Key Architectural Patterns Identified

**Pattern 1: Snapshot vs. Stream**
- Snapshot (XState, Precept, ODM): Returns "here's the current state and what you can do." Best for form/task UIs.
- Stream (Temporal, Camunda, Step Functions, Drools): Returns "here's what happened." Best for monitoring/debugging UIs.

**Pattern 2: Explanation Depth**
- Boolean availability (XState `can()`): "You can/can't do this."
- Structural explanation (Precept `InspectResult`): "You can't do this because guard X evaluated to false given field Y = Z."
- Incident diagnosis (Camunda): "This failed because of type mismatch in expression E."
- Decision trace (ODM): "Rule R fired because condition C matched object O."

**Pattern 3: Progressive Disclosure**
Every system uses 3-4 levels:
1. **Status badge** — Running/Failed/Completed (one icon or color)
2. **Summary panel** — Current state + available actions + key data
3. **Detail panel** — Full state context, guard evaluations, constraint status
4. **Raw/trace view** — Complete execution history or JSON dump

**Pattern 4: Graph Overlay**
Systems with visual process/state diagrams (Step Functions, Camunda Operate, XState Inspector) all overlay execution state onto the definition graph: highlighting current position, coloring completed/failed paths, showing available transitions.

**Pattern 5: Separation of Monitoring and Task UIs**
Every workflow platform separates the "admin monitoring" view (full state, full history, edit anything) from the "end-user task" view (just what you need for this step). Precept's inspector panel needs to support both modes or be clearly scoped to one.

---

## 6. Architectural Lessons for Precept

### What Precept Already Does Better Than Most

1. **Structured explanation in a single response** — `InspectResult` returns guard evaluations, constraint status, and available actions in one object. Only IBM ODM's execution trace comes close, and that's post-execution, not real-time.
2. **Guard-level explanation** — Most systems say "this event is available" or "this event is not available." Precept says *why* for each guard on each transition row.
3. **Constraint status on editable fields** — No surveyed system provides this at the API level. Camunda lets you edit variables but doesn't tell you what constraints apply.

### What Precept Can Learn

1. **Progressive disclosure hierarchy** — Implement 3 levels in the inspector panel:
   - **Summary**: Current state badge + count of available events + count of editable fields
   - **Detail**: Per-event availability with guard results + per-field constraint status
   - **Trace**: Step-by-step fire pipeline output for debugging

2. **Graph overlay pattern** — The preview diagram should highlight current state, color available transitions, and visually indicate guard pass/fail on transition arrows. This is universal across Step Functions, Camunda, and XState.

3. **Incident diagnosis pattern** — When an event is rejected or a constraint is violated, the explanation should be as rich as Camunda's incident messages: show the exact expression that failed, the data that was tested, and what would need to change.

4. **Design-time vs. runtime split** — The preview/inspector is a design-time tool. A future "form runtime" consumer of `InspectResult` would show less — just available actions and editable fields, with explanation available on demand.

5. **Metadata annotation** — XState's `meta` property on states (arbitrary key-value data for UI hints) and Temporal's User Metadata (human-readable workflow summaries) suggest that Precept state declarations could carry metadata for UI consumption (form labels, section headers, help text).

6. **Live query / subscription pattern** — Drools' ViewChangedEventListener pattern (reactive views with rowAdded/rowUpdated/rowRemoved) is relevant if Precept ever supports real-time UI bindings to state changes.

### Recommended Inspector Panel Architecture

Based on this research, the inspector panel should consume `InspectResult` in a **distributed layout** rather than a single monolithic panel:

```
┌─────────────────────────────────────────────┐
│  State Badge  │  Available Events Summary   │
├─────────────────────────────────────────────┤
│                                             │
│  Form Fields (editable/locked per state)    │
│  ├─ Field 1: value [editable] ✓ valid      │
│  ├─ Field 2: value [locked]                │
│  └─ Field 3: value [editable] ✗ min > 0   │
│                                             │
├─────────────────────────────────────────────┤
│  Event Buttons                              │
│  ├─ [Submit] available (all guards pass)    │
│  ├─ [Reject] disabled (guard: isReviewer)   │
│  └─ [Cancel] available                      │
│                                             │
├─────────────── On Demand ───────────────────┤
│  Inspector Panel (expandable)               │
│  ├─ Guard evaluation details                │
│  ├─ Constraint violation details            │
│  └─ Transition target diagram               │
└─────────────────────────────────────────────┘
```

This distributes `InspectResult` data across the form (fields inline, events as buttons, explanation on demand) rather than showing it all in a separate panel — matching how Camunda Tasklist and Pega Case Portal present task context.

---

## Sources

- XState Inspection API: https://stately.ai/docs/inspection
- XState States & Guards: https://stately.ai/docs/states, https://stately.ai/docs/guards
- Stately Inspector: https://stately.ai/docs/inspector, https://github.com/statelyai/inspect
- Temporal Web UI: https://docs.temporal.io/web-ui
- Temporal Visibility: https://docs.temporal.io/visibility
- Camunda Operate: https://docs.camunda.io/docs/components/operate/
- Camunda Tasklist: https://docs.camunda.io/docs/components/tasklist/
- Camunda Process Lifecycles: https://docs.camunda.io/docs/components/zeebe/technical-concepts/process-lifecycles/
- Camunda DMN: https://docs.camunda.io/docs/components/modeler/dmn/
- Drools Rule Engine: https://docs.drools.org/latest/drools-docs/drools/rule-engine/index.html
- AWS Step Functions Console (general knowledge — docs returned 403)
- IBM ODM Decision Center (general knowledge — docs returned 404; based on IBM ODM 8.x/9.x documentation)
- InRule Docs: https://docs.inrule.com/
- Budibase Bindings: https://docs.budibase.com/docs/bindings
- Pega Documentation: https://docs.pega.com/ (404 on specific pages; based on Pega 22.x documentation)
