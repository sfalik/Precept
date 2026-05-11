# Preview Concepts — Deep Analysis

**Author:** Elaine (UX Designer)
**Date:** 2026-05-02
**Status:** Research complete; awaiting Shane review

This document deepens the analysis of all 11 preview concepts with grounded research — internal (language semantics, runtime behavior, MCP tool surface, 21 sample files) and external (XState/Stately, Redux DevTools, DMN decision table editors, Observable notebooks, Elm debugger, VS Code webview best practices). It also introduces Concept 12, born directly from the research.

---

## Research Foundation

### What the Preview Must Visualize

From the language reference, runtime API, and MCP tool outputs, the preview surface needs to handle:

| Dimension | Range (across 21 samples) | Preview implication |
|---|---|---|
| States | 3–7 | Layout must scale from 3-column kanban to 7-node graphs |
| Events | 2–9 | Event lists, matrix columns, or edge labels must handle density |
| Scalar fields | 3–9 | Data tables or panels need scrolling or progressive disclosure |
| Collection fields | 0–2 (set, queue, stack) | Collections need specialized rendering: ordered vs unordered, with peek/count display |
| Invariants | 0–5 | Global constraint panels or indicators |
| State asserts (in/to/from) | 0–6 across types | Scoped to states; need contextual display |
| Event asserts | 0–9 | Scoped to events; need pre-fire validation display |
| Edit declarations | 0–2 | Editable fields need visual distinction and inline editing UX |
| Transition rows | 6–12 | Guard complexity ranges from simple boolean to 5-condition compound expressions |
| Guard complexity | Single boolean → 5-clause AND/OR with arithmetic | Guard text display needs wrapping or abbreviation strategy |
| Collection mutations | add/remove/enqueue/dequeue/push/pop/clear | Mutation chains need sequential rendering |
| Entry/exit actions | 0–4 per state | Auto-mutations are invisible without explicit pipeline display |
| Outcome kinds | 6 types (Transition, NoTransition, Rejected, ConstraintFailure, Undefined, Unmatched) | Each needs distinct visual treatment |

### What the Mockups Currently Show vs. What Exists

**Critical gap: all 11 mockups use the Subscription sample** — 3 states, 2 events, 2 fields, 1 invariant, 0 collections. This is the *simplest tier* of the 21 samples. The mockups have never been stress-tested against:

- Collection mutations (set add/remove in insurance-claim, queue peek/dequeue in it-helpdesk-ticket)
- Entry/exit actions (to/from state → set chains in building-access-badge, vehicle-service)
- Compound guards with arithmetic (loan-application: `DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2`)
- 7-state machines with 12 transition rows (hiring-pipeline)
- Multiple events with shared guards but different outcomes
- All 6 outcome kinds displayed simultaneously

**This is the single biggest weakness in the current concept set.** Every concept *looks* clean at 3×2, but several would break or become unreadable at 7×9 with collections.

### Fire Pipeline — The Invisible Machine

The 6-stage fire pipeline is the execution engine's core logic:

1. **Event asserts** — Validate event args (`on <Event> assert`)
2. **Row selection** — Evaluate `when` guards top-to-bottom, first match wins
3. **Exit actions** — Run `from <State> →` mutations
4. **Row mutations** — Execute matched row's `→ set/add/remove` chain
5. **Entry actions** — Run `to <State> →` mutations
6. **Validation** — Check invariants + state asserts; rollback on failure

**No existing concept visualizes this pipeline.** Concepts show the *outcome* of firing (transition, data changes) but not the *process* (which guard matched, which mutations ran in which order, which validation passed or failed). This is the gap that motivates Concept 12.

### MCP Tool Surface — The Data Contract

The inspect tool returns exactly the data model any preview concept must render:

```json
{
  "currentState": "UnderReview",
  "data": { ... 8 fields including collections ... },
  "events": [
    { "event": "Approve", "outcome": "Transition", "resultState": "Approved", "violations": [], "requiredArgs": ["Amount"] },
    { "event": "ReceiveDocument", "outcome": "Rejected", "violations": [{ "message": "...", "source": { "kind": "transition-rejection", ... }, "targets": [...] }] }
  ],
  "editableFields": [{ "name": "FraudFlag", "type": "boolean", "nullable": false, "currentValue": false }]
}
```

Key: violations carry structured `source` (invariant / state-assertion / event-assertion / transition-rejection) and `targets` (field / event-arg / event / state / definition). Any concept that shows "why something failed" should leverage this — not flatten to a string.

---

## External Research Findings

### XState / Stately.ai

**Key pattern:** Bidirectional code↔visual. You can build the state machine visually and generate code, or write code and see the diagram update. Stately now offers real-time simulation with event triggers and state highlighting.

**Applicable insight:** Precept's preview should feel like a *live simulation*, not a *static document*. The XState pattern of "click a transition to simulate it" is exactly what the fire API enables. The Subscription mockups show this but don't demonstrate it with real guard evaluation or collection mutations.

**Inapplicable:** XState's visual *authoring* (drag-and-drop state creation). Precept's DSL-first philosophy means the `.precept` file is always the source of truth. The preview is read-simulate, not write.

### Redux DevTools / Elm Debugger

**Key pattern:** Action timeline + state diff + skip/replay. The gold standard for time-travel debugging:
- Chronological action list on the left
- State inspector (tree or diff view) on the right
- Ability to jump to any point, skip actions, or replay sequences
- Delta compression for performance

**Applicable insight:** Concept 01 (Timeline) captures the timeline axis but misses skip/replay and delta diffs. Redux DevTools' strength is its *paired panels* — action list + state diff — which Concept 01 has but doesn't emphasize enough. The action timeline should show not just "what happened" but "what changed" at each step.

**Applicable insight:** Elm debugger's **delta snapshots** — store only changes, not full state copies — is a performance pattern Kramer should implement regardless of which concept ships.

### DMN Decision Table Editors (Camunda, Trisotech)

**Key pattern:** Truth-table format where rows = input conditions, columns = outputs. Business users can read and edit rules without understanding the execution engine.

**Applicable insight:** Concept 03 (Decision Matrix) maps naturally to this. DMN editors add a **Decision Requirements Diagram (DRD)** showing how tables relate to each other — for Precept, this is the relationship between guards, invariants, and state asserts. The matrix should link to a rule dependency view (connecting to Concept 07).

**Applicable insight:** DMN tables use **hit policies** (first, unique, collect, priority) which parallel Precept's first-match-wins guard evaluation. The matrix should clearly show guard evaluation order, not just outcomes.

### Observable Notebooks

**Key pattern:** Reactive cells with inline results. Code and output are co-located. Change an input → all dependent cells re-render. Inputs (sliders, dropdowns) provide instant parameter adjustment.

**Applicable insight:** Concept 05 (Notebook) captures the card-based layout but misses Observable's *reactivity*. The notebook should be reactive: change a field value in the data card → the events card instantly re-evaluates all outcomes. This is what `engine.Inspect(instance, hypotheticalPatch)` enables.

**Applicable insight:** Observable's cell-collapse pattern is more sophisticated than the Notebook mockup's expand/collapse — Observable pins cells you care about and collapses the rest, with visible output summaries even when collapsed.

### VS Code Webview Constraints

**Key pattern:** Theme adaptation via CSS variables, Webview UI Toolkit components, lazy loading, message passing between extension host and webview.

**Critical constraint:** VS Code webview panels are typically 300–600px wide in a side panel, or 800–1200px as an editor tab. Most concepts assume editor-tab width. The preview needs to work in *both* placements, which several concepts (Matrix, Dashboard, Kanban, Graph Canvas) would struggle with at sidebar width.

**Applicable insight:** The best VS Code webviews (Draw.io, Thunder Client) use a primary/detail pattern — a compact overview that expands on interaction. This favors Notebook (scrolling cards) and Timeline (compact strip + detail) over Matrix (fixed-width grid) or Dashboard (multi-widget) at narrow widths.

---

## Concept-by-Concept Deep Analysis

### Concept 01: Timeline Debugger ⭐ PRIMARY RECOMMENDATION

**Metaphor:** Debugger stepping through event history.

**Strengths (research-confirmed):**
- Directly maps to Redux DevTools' proven action-timeline + state-diff pattern
- The only concept that answers "how did I get here?" — the primary debugging question
- Timeline strip scales naturally: horizontal scroll handles long histories
- Data diff panel (what changed) is the fastest path to understanding mutations
- AI agents can consume timeline data as structured JSON sequences

**Weaknesses (research-identified):**
- Current mockup shows only 3 steps; needs to demonstrate deep history (20+ events)
- Missing: skip/replay controls from Redux DevTools
- Missing: guard evaluation detail — which row matched and why
- Missing: collection mutation display (add/remove in the data diff)
- At sidebar width (~400px), the horizontal timeline strip needs to collapse to a vertical list

**What the mockup needs:**
- A complex-precept example (insurance-claim or hiring-pipeline) showing collection diffs, guard matches, and constraint failures alongside the happy path
- Branch-point indicators: "at this step, Cancel was also available but wasn't fired"
- Pipeline stage indicators in the detail panel for each step

**Precept-specific fit: EXCELLENT.** The fire API returns `fromState`, `toState`, `data` — exactly what each timeline node needs. The inspect API at each step shows alternative paths. History is reconstructable by chaining fire calls.

**Scaling assessment:** Handles 7-state/12-row machines well because it shows one step at a time. The timeline strip needs smart labeling when events repeat (e.g., multiple Activate fires).

---

### Concept 02: Conversational REPL

**Metaphor:** Terminal/REPL with structured output.

**Strengths:**
- Text-first format is AI-agent native — an AI can read and produce REPL commands
- Lowest visual overhead; works at any viewport width
- Natural for exploratory debugging: "what happens if I fire X?"
- Command history gives implicit timeline
- Grep-friendly: search the log for specific events or field names

**Weaknesses (research-identified):**
- Current mockup only shows success cases; needs rejection, constraint failure, and unmatched outcomes
- No example of collection mutations in output (how does `add MissingDocuments "Tax Return"` render?)
- Sidebar state display is static; needs to show edit-mode interaction for `in <State> edit` fields
- Long logs lose context — needs anchoring or log-level filtering
- Quick-fire chips should show required args before allowing fire

**What the mockup needs:**
- A constraint-failure example with structured violation output (source, targets, expression)
- An `update` command example showing direct field editing with editability checks
- A "no args provided" example showing the requiredArgs hint from inspect
- Log-level filtering: show/hide data diffs, show/hide rule evaluations

**Precept-specific fit: GOOD, with caveats.** The MCP tool surface maps directly to REPL commands (`fire Activate Plan="Gold" Price=199` ≈ `precept_fire`). But the REPL loses spatial context that humans need — there's no "where am I in the machine?" without mental model. Best for AI agents and power users.

**Scaling assessment:** Scales infinitely in length. Struggles with *breadth* — seeing all available events simultaneously requires scrolling up to the last inspect output.

---

### Concept 03: Decision Matrix ⭐ STRONG SECONDARY

**Metaphor:** Truth table / DMN decision table.

**Strengths (research-confirmed by DMN editor patterns):**
- Only concept showing the *complete contract* at once — every (state, event) combination
- Maps directly to DMN decision-table UX patterns (Camunda, Trisotech) which are proven for business rule comprehension
- Cell colors give instant coverage: green = transition, gray = undefined, red = rejected
- Completeness review: "is there a hole in my machine?" is answered by scanning for gray cells
- Compile-time-only view (no runtime state needed) makes it the fastest-to-render concept

**Weaknesses (research-identified):**
- Current mockup uses 3×2 matrix; a 7×9 matrix (hiring-pipeline) would be 63 cells — needs dense rendering or filtering
- Guard branching within cells: when multiple rows exist for the same (state, event) with different guards, what does the cell show? Current mockup ignores this
- DMN editors solved this with *sub-tables* inside cells — Precept could show guard → outcome chains
- No collection-aware cells: a cell where the outcome depends on collection state (contains, count) needs dynamic evaluation, not a static outcome
- Right-panel detail is thin: "Steps" list should show the full fire pipeline, not just a flat list

**What the mockup needs:**
- A multi-guard example: `from UnderReview on Approve` has 2 rows (one guarded, one fallback reject). The cell should show both branches
- A 6×7 matrix example showing how density is handled (abbreviation, hover-to-expand, scrolling)
- Hit-policy indicator: show that Precept uses first-match-wins ordering within a cell

**Precept-specific fit: EXCELLENT for contract review, POOR for runtime exploration.** The matrix answers "what does this machine do?" not "what can I do right now?" Best as a secondary mode alongside Timeline or Notebook.

**Scaling assessment:** Struggles past ~6 states × ~6 events without pagination or column hiding. But for most real precepts (3–7 states, 2–9 events), it fits.

---

### Concept 04: Focus / Spotlight

**Metaphor:** Zen-mode — one state, large and clear, with radial context.

**Strengths:**
- Highest signal-to-noise ratio for a single state
- "What can I do right now?" is answered instantly by the path cards
- Minimal cognitive load: nothing competes for attention
- Beautiful on first impression

**Weaknesses (research-identified):**
- Path cards only show *available* transitions. Rejected/undefined events disappear — the user can't see what they *can't* do, which is critical for debugging
- Current mockup doesn't show what happens with 5+ available events from one state (hiring-pipeline's InterviewLoop has 3+ events with guards)
- Data "atoms" orbiting below are pretty but scale poorly: 9 scalar fields + 2 collections = 11 items. Orbit breaks
- Mode tabs (Explore/Edit/History) are claimed but not shown. The "Edit" mode needs to demonstrate in-state field editing
- Guard conditions on path cards are abbreviated to the point of uselessness for complex guards
- No collection visualization at all

**What the mockup needs:**
- A rejected-event path card (dimmed, with rejection reason)
- A complex-state example with 4+ path cards showing how they wrap
- Collection field display (how does a `set<string>` with 3 items appear as a data atom?)

**Precept-specific fit: MODERATE.** Beautiful for simple precepts, breaks for complex ones. The radial layout is fundamentally bounded — it doesn't scroll, it fans. Works as a "home view" that links to deeper exploration, not as a standalone concept.

**Scaling assessment:** Breaks at 5+ events or 6+ fields. Could work as a landing page within a hybrid approach.

---

### Concept 05: Notebook / Report ⭐ PRIMARY RECOMMENDATION

**Metaphor:** Live, scrollable document with expandable sections.

**Strengths (research-confirmed by Observable patterns):**
- Progressive disclosure solves the density problem: collapse what you don't need
- Card-based layout scales to any complexity — just add cards
- The only concept that could render a *complete* precept specification inline
- Printable / shareable — useful for documentation and review
- Observable's reactive model applies: change data → cards re-evaluate

**Weaknesses (research-identified):**
- Current mockup expands all cards by default — needs better collapsed-state summaries (Observable's "pinned cell" pattern)
- Missing: collection field rendering (how does a `queue<string>` with items appear in the data card?)
- Missing: edit-mode interaction (clicking the "edit" badge should inline an input)
- Missing: constraint failure display with structured violation detail
- Mini diagram at the bottom is nice but conflates two concepts — it should be optional and collapsible
- Cards are passive in the mockup; Observable shows that *each card should have actions* (fire, edit, inspect)

**What the mockup needs:**
- Reactive behavior: a "What If" toggle that lets the user change field values and see all cards re-evaluate
- Collection-aware data card showing set/queue/stack with item lists and count badges
- Entry/exit action card: a section showing `to Active → set ActivatedAt = ...` type auto-mutations
- Constraint failure card with structured violation output (source kind, expression, targets)

**Precept-specific fit: EXCELLENT.** The card structure maps 1:1 to the precept's declaration structure — fields card, states card, events card, rules card. Progressive disclosure handles the 3-state-to-7-state range gracefully. Reactive evaluation mirrors the inspect tool's hypothetical-patch API.

**Scaling assessment:** The best-scaling concept. Cards scroll; sections collapse; content is linear. Works at sidebar width (single-column cards) and editor-tab width (wider cards with inline detail).

---

### Concept 06: Dual-Pane Diff

**Metaphor:** Code diff for state-machine snapshots.

**Strengths:**
- Directly answers "what's different between two states?" — useful for understanding transitions
- Side-by-side comparison is a proven pattern (git diff, VS Code's built-in diff editor)
- Data-field comparison with color highlighting is clear and fast to scan

**Weaknesses (research-identified):**
- Comparison-only; no firing, no editing. A passive view
- Only compares *named states*, not *runtime snapshots at different points in history*
- Doesn't show *why* the differences exist (which events cause the transition from A to B)
- At sidebar width, the dual pane doesn't fit — needs a different layout
- Limited to contract-time comparison; can't compare two different data scenarios for the same state

**What the mockup needs:**
- History-aware comparison: diff two points in a Timeline, not just two named states
- Transition path between the two states: "to get from Draft to Active, fire Submit then Approve"
- Side-by-side collection comparison (set A has items X, Y; set B has items X, Z)

**Precept-specific fit: MODERATE as standalone; STRONG as a feature within Timeline or Storyboard.** Dual-pane diff is a utility, not a product shape. It should be a mode accessible from Concepts 01 or 09.

**Scaling assessment:** Fine for field comparison. Struggles when comparing many events across two states — the event lists can be long.

---

### Concept 07: Rule Pressure Map ⭐ NOTABLE SECONDARY

**Metaphor:** Governance dashboard — constraints as the primary lens.

**Strengths (research-confirmed by DMN/BRMS patterns):**
- The only concept that organizes around *business rules* instead of states or events
- Inverts the mental model: "are my rules holding?" instead of "which state am I in?"
- Pressure bars (margin-to-violation) are a genuinely novel visualization for Precept
- Links rules to driving fields and touching events — makes cross-cutting dependencies visible
- Directly serves the compliance/audit use case

**Weaknesses (research-identified):**
- "Pressure" is a metaphor that needs careful definition. For `MonthlyPrice >= 0` with current value 99, the "pressure" is 99 units of margin. For a boolean invariant like `PoliceReportRequired || MissingDocuments.count == 0`, what's the pressure? Binary pass/fail — the bar metaphor breaks
- Current mockup only shows 3 rules; insurance-claim has 3 invariants + 1 state assert + 8 event asserts = 12 rules. Needs tile density handling
- No example of a *violated* rule (red state) — the most critical UX scenario is missing
- Event asserts are contextual (only evaluated when that event fires with specific args). The pressure map would need to show them differently from always-evaluated invariants
- Filter tabs (Invariants/Assertions/Rejections) are correct categorization but miss entry/exit actions

**What the mockup needs:**
- A violated-rule example with structured violation detail and "fix suggestion" (which field to change)
- Pressure bar definition: margin = distance from current value to violation threshold. For numeric invariants this is calculable; for boolean/string expressions, show pass/fail badge instead of bar
- A 12-rule example showing tile density and scrolling
- Rule dependency graph: "invariant X depends on fields A, B; event E modifies B; firing E from state S would pressure X"

**Precept-specific fit: STRONG for complex precepts with many business rules (loan-application, insurance-claim). WEAK for simple lifecycle precepts (trafficlight).** Best as a mode/tab, not a primary shape.

**Scaling assessment:** Tiles scale well with scrolling and filtering. The right-panel detail handles one rule at a time. Works at sidebar width if tiles stack vertically.

---

### Concept 08: Graph Canvas

**Metaphor:** Full-bleed interactive diagram (Miro for state machines).

**Strengths:**
- Spatial layout is the most natural representation of a state machine
- Direct manipulation: click nodes, click edges, pan, zoom
- Data overlay is toggleable — doesn't clutter the graph
- Self-loops, rejected edges, and terminal states have distinct visual treatments

**Weaknesses (research-identified):**
- "Click a node to enter it" is described but never shown — the interaction model is undefined
- Edge labels with inline fire buttons become unreadable at 12+ transitions
- SVG connection routing for 7 nodes with crossing edges needs a layout algorithm (none described)
- At sidebar width, pan/zoom becomes the primary interaction — too much friction
- VS Code webview performance with large SVG graphs is a concern
- Minimap is mentioned but not shown; unclear what it adds for 3–7 node graphs
- ReactFlow / JointJS / Rete.js are proven libraries for this pattern in browser, but within VS Code webview message-passing constraints, canvas performance may degrade

**What the mockup needs:**
- A 6+ state graph showing how layout handles crossing edges
- Node-enter interaction: clicking Active shows available events as expandable cards inside the node
- Edge-click interaction: clicking an edge shows guard conditions, required args, and a fire button
- Sidebar-width alternative: vertical node stack with edge indicators

**Precept-specific fit: MODERATE.** The existing preview already has a state diagram. Making it the *entire* interface risks losing structured data display. Best as an enhanced version of the current diagram, not a replacement for the full preview.

**Scaling assessment:** Graph layout is an unsolved general problem. For 3–5 states it's manageable; for 7 states with many edges it needs force-directed or hierarchical layout. Libraries help but add bundle weight.

---

### Concept 09: Storyboard / Scenarios ⭐ STRONG SECONDARY

**Metaphor:** Test harness with scenario library and coverage tracking.

**Strengths (research-confirmed):**
- The only concept that frames the preview as a *verification tool*
- Named scenarios are replayable — "Happy Path", "Edge: Rejected from Cancelled"
- Coverage metrics (states visited, events fired, transitions exercised) are unique to this concept
- Step cards with data snapshots give full audit trail
- Scenario comparison answers "what's different between these two paths?"

**Weaknesses (research-identified):**
- Scenario library shows state paths but not event names — two scenarios with the same state path but different events look identical
- "Replay" behavior is undefined: re-execute silently? Show step-by-step animation?
- Coverage metrics are aggregate (2/3 states, 1/2 events) but don't show *which* specific transitions are uncovered in a scannable way
- No example of branching: "from this step, what if I fired Cancel instead of Activate?"
- Missing: how to add args to scenario steps (the add-step chips don't show arg forms)
- Missing: collection mutation display in step snapshots

**What the mockup needs:**
- Scenario steps should show event names prominently: "Step 2: fire Activate(Plan=Enterprise, Price=149)" not just "Activate ✓"
- Coverage gap visualization: a mini matrix or checklist showing uncovered (state, event) pairs
- Branch-from-step interaction: right-click a step → "Branch here" → creates a new scenario diverging from this point
- Replay mode: step-by-step with auto-advance and pause, showing pipeline stages at each step

**Precept-specific fit: STRONG.** Precept's deterministic semantics mean scenarios are perfectly reproducible — fire the same events with the same args, get the same result every time. This is the ideal foundation for a scenario library. The fire API's stateless design (text + state + data → result) means any step can be replayed independently.

**Scaling assessment:** Step cards scale linearly with scenario length. Scenario library sidebar needs grouping/folders for many scenarios. Coverage metrics scale well — they're always a fixed-size summary.

---

### Concept 10: Dashboard / Control Room

**Metaphor:** Multi-widget instrument panel.

**Strengths:**
- Maximum information density: state, events, data, constraints, and activity all visible at once
- Event heatmap is a compact version of the Decision Matrix
- Data sparklines give historical context at a glance
- Activity feed gives timeline-like history in a compact widget

**Weaknesses (research-identified):**
- Five widgets competing for a 300–600px sidebar panel. At sidebar width, this is unusable
- Heatmap cells are described as clickable but the interaction is undefined
- Data sparklines require step-by-step history tracking — not something the preview currently stores
- No constraint-failure display (yellow indicator with no drill-down)
- The concept is monitoring-oriented (watching a running system) but Precept instances are simulated, not live
- Widgets are sized by CSS grid, not by content priority — the least useful widget (sparklines) gets equal space

**What the mockup needs:**
- Priority-based widget sizing: state summary and event list get more space; sparklines collapse to a compact row
- Widget-click interaction: clicking the heatmap cell opens a detail overlay (don't navigate away)
- Constraint-failure drill-down: clicking a yellow/red indicator shows the structured violation
- Responsive layout: sidebar mode collapses to stacked widgets with collapse/expand

**Precept-specific fit: MODERATE.** Monitoring makes sense for long-running systems, but Precept instances are manually stepped. The dashboard's strength (everything at once) is also its weakness (everything competes). Best for users who've already understood the precept and want to run many scenarios quickly.

**Scaling assessment:** The heatmap widget breaks first at 7×9. Other widgets scale reasonably. At sidebar width, the whole concept needs to become a vertical scroll.

---

### Concept 11: Kanban Board

**Metaphor:** States as columns, entity as a movable card.

**Strengths:**
- Spatial position instantly answers "where am I in the lifecycle?"
- Guard conditions on connectors make the constraint surface visible without drilling in
- Ghost cards show history without a separate timeline
- Terminal states as empty columns are self-documenting
- Left-to-right flow matches natural reading direction for lifecycle understanding

**Weaknesses (research-identified):**
- Current mockup shows 3 columns. At 7 states (hiring-pipeline), the horizontal scroll becomes unwieldy
- Connector arrows between lanes need to handle non-adjacent transitions (e.g., Active → Draft skip-backs)
- Entity card in the lane needs to handle many fields: at 9 fields + 2 collections, the card is very tall
- Self-loop representation (↻ within a lane) works for 1 self-loop; with 2+ self-loops (multiple no-transition events from the same state), the lane becomes cluttered
- No arg input on connectors — hovering shows "▶ fire" but no way to provide event arguments
- Ghost cards should show *when* (step number) the entity was there, not just that it was
- Backward transitions (e.g., Cancelled → Active if reactivation existed) create visual chaos with crossing connectors

**What the mockup needs:**
- A 5+ state example showing horizontal scroll and non-adjacent transitions
- Connector arg forms: clicking a connector shows an arg panel before firing
- Multi-self-loop display: multiple ↻ indicators in a lane, stacked or listed
- Entity card with collection fields: show `MissingDocuments: {"Tax Return", "ID Scan"}` inline

**Precept-specific fit: STRONG for linear lifecycles (Draft → Submitted → Approved → Paid). MODERATE for cyclic machines (trafficlight) where the flow isn't left-to-right. WEAK for machines with many back-transitions.**

**Scaling assessment:** Horizontal space is the constraint. 5 columns is comfortable; 7 is manageable with scroll; beyond 7 is unlikely in practice. The real constraint is non-linear transitions creating crossing connectors.

---

## New Concept: 12 — Execution Trace / Pipeline Debugger

### Motivation

Every existing concept shows the *result* of firing an event (transition, data changes, violations). None shows the *process* — the 6-stage pipeline that determines the result. This matters because:

- When an event is rejected, the user needs to know *which stage* rejected it (event assert? guard? invariant?)
- When data changes, the user needs to know *which stage* changed it (exit action? row mutation? entry action?)
- When a guard doesn't match, the user needs to see *which guards were evaluated and why each failed*
- The fire pipeline's stage ordering matters: exit actions run before row mutations, which run before entry actions. This order affects outcomes.

The XState simulator shows simulation steps but not internal evaluation. Redux DevTools shows action replay but not reducer internals. The Elm debugger shows state deltas but not update function internals. **No external tool visualizes the internal evaluation pipeline of a state machine engine.** This is an opportunity unique to Precept's deterministic, inspectable design.

### Design

**Layout:** Vertical pipeline visualization for a single event fire.

| Stage | Display | Status |
|---|---|---|
| 1. Event Asserts | Expression + result for each `on <Event> assert` | ✓ passed / ✗ failed (stops here if any fail) |
| 2. Row Selection | Each `from <State> on <Event> [when <Guard>]` row with guard expression + evaluation result | ✓ matched (highlight) / – skipped / ✗ all failed = Unmatched |
| 3. Exit Actions | `from <State> → set/add/remove` chain with before→after values | ✓ executed (show mutations) |
| 4. Row Mutations | `→ set/add/remove` chain from matched row with before→after values | ✓ executed (show mutations) |
| 5. Entry Actions | `to <State> → set/add/remove` chain with before→after values | ✓ executed (show mutations) |
| 6. Validation | Each invariant + state assert with post-mutation evaluation | ✓ all passed / ✗ failed = ConstraintFailure + rollback |

**Interaction:** Select an event from a dropdown + provide args → the pipeline renders all 6 stages. Stages that don't apply (no exit actions, no entry actions) collapse to a single "—" row. Failed stages are highlighted in rose with the specific violation detail.

**Key UX insight:** This is the "microscope" to Timeline's "telescope." Timeline zooms out to show event history; Pipeline Debugger zooms in to show what happens *inside* one event fire.

### Precept-Specific Fit: UNIQUELY STRONG

This concept is impossible without a deterministic, inspectable execution engine. Precept's design principle — "deterministic, inspectable model; no hidden state, no side effects" — makes this concept feasible and valuable. The fire result + inspect result together provide all the data needed:
- Violations carry structured `source` (which constraint, which expression, which line)
- Data diffs show which fields changed
- Guard evaluation order is determined by declaration order (first-match-wins)

No other DSL tool offers this level of pipeline visibility.

### Scaling Assessment

Works identically for simple and complex precepts — it always shows 6 stages for one event. More stages light up for complex precepts (exit/entry actions), but the structure is always bounded.

### When to Use

- Debugging why an event was rejected: "Stage 2 shows guard X evaluated to false because field Y was null"
- Understanding mutation order: "Exit action cleared the flag, then row mutation set the new value"
- Verifying constraint satisfaction: "Stage 6 shows all 3 invariants passed after the mutation"
- Learning the language: "So *that's* what happens when I fire an event" — the pipeline is self-teaching

---

## Revised Recommendations

### Tier 1 — Primary Shapes (build one of these as the main preview)

| Concept | Why | Best for |
|---|---|---|
| **01 Timeline Debugger** | Proven pattern (Redux DevTools), answers the debugging question, scales well | Power users, debugging, understanding history |
| **05 Notebook / Report** | Best scaling, progressive disclosure, printable, reactive potential | All users, documentation, review, learning |

**Recommendation: Build both as switchable modes.** Timeline for debugging, Notebook for understanding. They serve different primary questions.

### Tier 2 — Strong Secondary Modes (add these as tabs/modes)

| Concept | Why | Best for |
|---|---|---|
| **03 Decision Matrix** | Complete contract view, proven DMN pattern, fast coverage scan | Contract review, completeness verification |
| **09 Storyboard** | Only verification/test concept, coverage tracking, replayable scenarios | Testing, scenario planning, quality assurance |
| **12 Execution Trace** | Only pipeline-visibility concept, unique to Precept, self-teaching | Debugging, learning, understanding engine behavior |

### Tier 3 — Valuable Modes for Specific Use Cases

| Concept | Why | When |
|---|---|---|
| **07 Rule Pressure Map** | Novel constraint-first lens, audit-friendly | Complex precepts with many business rules |
| **11 Kanban Board** | Spatial lifecycle clarity, intuitive flow | Simple linear lifecycles |

### Tier 4 — Features, Not Standalone Concepts

| Concept | Better as... |
|---|---|
| **06 Dual-Pane Diff** | A feature within Timeline or Storyboard ("compare two steps") |
| **08 Graph Canvas** | An enhanced version of the current diagram, not a replacement for the full preview |
| **10 Dashboard** | A compact summary widget, not a full-screen concept |

### Tier 5 — Situational

| Concept | When |
|---|---|
| **02 Conversational REPL** | AI-agent-primary interfaces; power-user CLI-style interaction |
| **04 Focus/Spotlight** | Landing page / home view within a hybrid approach; never standalone |

---

## Cross-Cutting Issues

### 1. All Mockups Need a Complex Sample

The Subscription sample (3/2/2/1) is fine for first impressions but fails to test real scaling. Every concept should be demonstrated with *both* Subscription (simple) and InsuranceClaim or HiringPipeline (complex) to validate that the layout works across the range.

### 2. Collection Fields Are Invisible

None of the 11 mockups render set, queue, or stack fields. 11 of 21 samples use collections. This is a critical gap. Every concept needs a collection-field rendering pattern:
- **Sets:** `{ "Tax Return", "ID Scan", "Police Report" }` with count badge
- **Queues:** `[ Alice → Bob → Charlie ]` with front/back indicators
- **Stacks:** `[ (top) Charlie | Bob | Alice ]` with top indicator

### 3. All 6 Outcome Kinds Need Visual Treatment

Mockups typically show 3 outcomes (Transition, NoTransition, Rejected). The full set is:
- **Transition** — ✓ green, state changed
- **NoTransition** — ↻ cyan/teal, state unchanged but data may change
- **Rejected** — ✗ rose, explicit rejection by the authored workflow
- **ConstraintFailure** — ⚠ gold, matched but rolled back due to constraint violation
- **Undefined** — — gray, no transition rows exist for this (state, event) pair
- **Unmatched** — ? amber, transition rows exist but no guard matched

### 4. Violation Detail Is Rich — Use It

The runtime returns structured violations with source (kind, expression, reason, line) and targets (field, event-arg, event, state). No mockup leverages this structure. The "why did it fail?" UX should show:
- **Source kind** as a category badge (invariant, state-assertion, event-assertion, transition-rejection)
- **Expression text** as readable code
- **Reason** as the because-message
- **Target fields** as clickable links that highlight the relevant data field

### 5. Edit Declarations Need First-Class UX

`in <State> edit <Field>` is a unique Precept feature — fields are only editable in specific states. The inspect tool returns `editableFields` per state. Mockups show an "edit" badge but never demonstrate the editing interaction:
- Editable fields should have visible input affordance (underline, edit icon)
- Non-editable fields should be visually locked
- Editing should trigger inline validation via the hypothetical-patch inspect API
- The update tool's structured response (Updated, UneditableField, ConstraintFailure) needs distinct UX

---

## External References

| Source | Key Insight | Concepts Affected |
|---|---|---|
| [Stately.ai / XState Visualizer](https://stately.ai/) | Bidirectional code↔visual; real-time simulation | 08, 01 |
| [Redux DevTools](https://github.com/reduxjs/redux-devtools) | Action timeline + state diff + skip/replay | 01, 09 |
| [Camunda DMN Editor](https://docs.camunda.io/docs/components/modeler/dmn/) | Truth-table format for business rules; hit policies | 03, 07 |
| [Observable Notebooks](https://observablehq.com/) | Reactive cells with inline results; cell collapse | 05 |
| [Elm Debugger](https://guide.elm-lang.org/effects/debugger.html) | Delta snapshots; inline diffs; event labeling | 01, 12 |
| [VS Code Webview UI Toolkit](https://github.com/microsoft/vscode-webview-ui-toolkit) | Theme adaptation; message passing; responsive layout | All |
| [Precept Language Expressiveness Research](../../research/language/expressiveness/README.md) | Guard complexity; collection patterns; field constraints | 03, 07, 12 |

---

## Next Steps

1. **Shane review** — confirm Flow Cards (13) as the primary direction and approval to proceed with implementation planning
2. **Flow Cards refinement** — add expand/collapse interaction for non-current state cards, diagram-mode toggle, and sidebar-width variant
3. **Concept 12 HTML mockup** — build the Pipeline Debugger mockup to the same fidelity as the existing concepts
4. **Responsive audit** — test Flow Cards at 400px (sidebar) and 1000px (editor tab) widths
5. **Collection rendering pattern** — refine the shared component for set/queue/stack display (first demonstrated in Concept 13)

---

## Addendum: Concept 13 — Flow Cards (Recombination Pass)

**Added:** 2026-05-02 — after Shane's feedback that Timeline/history is stronger for production UI than dev preview.

### Re-evaluation: Timeline in Dev Preview

The original Tier 1 ranking placed Timeline (01) and Notebook (05) as co-primary shapes. Shane's observation is correct: **in a dev preview, the developer is manually simulating — stepping through events one at a time.** This means:

- History is shallow (typically 2–5 steps in a session, not 20+)
- The "how did I get here?" question has a trivial answer (the user literally just did it)
- Timeline's strength — scrubbing through deep event history with diffs — doesn't activate
- The Redux DevTools analogy breaks down: Redux apps generate hundreds of actions automatically; Precept preview generates actions one at a time manually

**Timeline is reclassified as a production-UI pattern** — where an entity is driven through many states by real business events over time, and the audit trail matters. In a production dashboard, Timeline would be the clear primary shape.

**For dev preview, the core question is different:** "What does this machine look like, where am I in it, and what can I do?" This is a *contract understanding* question, not a *history debugging* question.

### Design: Concept 13 — Flow Cards

**Origin:** Takes the large-card aesthetic from Concept 04 (Focus/Spotlight) — the big state name, the radial path cards, the generous spacing, the minimal chrome — and expands it to show the *entire lifecycle* instead of just the current state.

**Layout:** Vertical scroll of state cards connected by transition edges.

| Element | Treatment |
|---|---|
| Current state card | Spotlight: 36px state name, violet glow, full data grid, all events with args/fire buttons, rule chips |
| Other state cards | Compact: 22px state name, deemphasized, one-line subtitle, expandable on click |
| Transition edges | Vertical connectors between cards with event name chips and guard summaries |
| Self-loops | Dashed chips inside the originating card (↻ RequestDocument) |
| Branches | Labeled branch sections below the main flow (e.g., "Branch: UnderReview → Denied") |
| Left rail | Compact mini-diagram: circles for states, lines for edges, current state highlighted |
| Data grid | CSS grid of datum tiles inside current-state card, including collections |
| Status bar | Invariant health, precept stats, breadcrumb path |

**Sample used:** InsuranceClaim — 6 states, 7 events, 8 fields (including `set<string>`), 3 invariants, 1 edit declaration, compound guards with collection conditions. **This is the first mockup using a complex sample.**

### Why Flow Cards Is the Best Combined Direction

1. **Answers the dev-preview question directly.** "What does this machine look like?" — you see every state and transition in a scrollable flow. "Where am I?" — the current state card is visually dominant. "What can I do?" — events with args and fire buttons are right there.

2. **Inherits Focus/Spotlight's best quality** — clarity. One state gets the spotlight treatment with generous spacing and large typography. The developer's eye goes to the right place immediately.

3. **Fixes Focus/Spotlight's worst weakness** — context blindness. Concept 04 hides everything except the current state. Flow Cards shows the whole machine, so the developer sees what's upstream (how did this entity get here in the lifecycle?) and downstream (where can it go next?).

4. **Scales to real complexity.** Demonstrated with InsuranceClaim (6 states, 7 events). Non-current cards collapse to a single line, so the vertical scroll stays manageable. The left rail provides spatial context without scrolling.

5. **Handles collections.** The data grid includes a collection tile for `MissingDocuments: set<string>` with item chips and count — the first mockup to render collection fields.

6. **Handles rejected events.** The Approve event shows "✗ Rejected" with the guard condition and rejection message — the first mockup to show a blocked event with full context.

7. **Handles branching.** The Denied terminal state is shown as a labeled branch below the main flow, not hidden.

8. **Works at sidebar width.** The `@media (max-width: 500px)` responsive rule hides the left rail and stacks cards in a single column.

### Precept-Specific Fit: EXCELLENT

- State-centric organization matches how developers think about precepts: "I'm in state X"
- Vertical flow matches the `.precept` file's declaration order (states are typically declared top-to-bottom in lifecycle order)
- The fire-button-per-event interaction maps directly to the MCP `precept_fire` tool
- The data grid maps to the `precept_inspect` result's `data` object
- The rule chips map to the `precept_inspect` result's `violations` array
- The edit badge on FraudFlag maps to the `editableFields` from inspect

### Scaling Assessment

| Dimension | Behavior |
|---|---|
| 3 states (Subscription) | Fits without scrolling; all cards visible at once |
| 6 states (InsuranceClaim) | Comfortable scroll; current state is always the visual anchor |
| 7 states (HiringPipeline) | Works with scroll; non-current cards collapse |
| 9 events from one state | Event rows stack vertically inside the card; scroll within the card section |
| 9 scalar fields + 2 collections | Data grid wraps; collections get full-width tiles |
| Sidebar (400px) | Rail hidden; cards stack; data grid goes single-column |

### Revised Tier Ranking

| Tier | Concept | Role |
|---|---|---|
| **Tier 1 — Primary (dev preview)** | **13 Flow Cards** | Default view: whole-machine flow with interactive spotlight on current state |
| **Tier 1 — Strong secondary** | **05 Notebook** | Alternative "full report" mode organized by facet instead of state |
| **Tier 2 — Specialized modes** | **03 Decision Matrix** | Contract completeness review |
| **Tier 2 — Specialized modes** | **12 Execution Trace** | Fire pipeline debugging (uniquely Precept-specific) |
| **Tier 2 — Specialized modes** | **09 Storyboard** | Scenario building and coverage tracking |
| **Tier 3 — Valuable modes** | **07 Rule Pressure Map** | Constraint-centric view for rule-heavy precepts |
| **Tier 3 — Valuable modes** | **11 Kanban** | Spatial lifecycle view for simple linear precepts |
| **Production UI (not preview)** | **01 Timeline** | Event history + state diffs for long-running entities |
| **Features, not shapes** | **06 Diff**, **08 Graph**, **10 Dashboard** | Utility features within other shapes |
| **Situational** | **02 REPL**, **04 Focus/Spotlight** | AI-agent CLI; superseded by 13 for human use |
