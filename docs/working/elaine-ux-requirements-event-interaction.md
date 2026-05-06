# Elaine: UX Requirements — Event Interaction and Real-Time Inspection

**Author:** Elaine (UX Designer)
**Date:** 2026-05-06
**Status:** Draft — pending Shane's calls on OQ-1 through OQ-7
**Informs:** VS Code extension implementation, CC#12, CC#23, CC#24

---

## 0. Grounding

This document derives from Shane's two seed ideas for the event interaction surface:

> "A user is viewing a current version — they should see all events that are defined, with some of them grayed out, some of them potentially clickable but need args, and others directly clickable."

> "A user picks one of those available events, and starts filling out args (if there are args defined). As the user types, and before the event is fired, inspection should provide feedback on the args as well as the potential outcome(s)."

Shane explicitly asked for depth beyond the seeds. This document completes the picture: missing states, edge cases, accessibility requirements, AI agent concerns, and open design questions where I need a call.

The runtime contract I'm designing against is the `EventInspection` shape proposed in `docs/working/event-inspection-proposal.md`:
- `EventName`, `OverallProspect`, `DeclaredArgs`, `ArgErrors`, `CurrentFields`, `Transitions`, `EventEnsures`
- `TransitionInspection`: `Prospect`, `TransitionKind` (Transition/Apply/Reject), `TargetState?`, `RejectReason?`, `Constraints`, `PostFields`

My earlier review (`docs/working/elaine-ux-review-event-inspection.md`) recommended `ArgErrorKind` on `ArgError`, sealed DU for `RowEffect`, and per-row `EventEnsures`. Those recommendations inform the requirements below.

---

## 1. User Personas and Context

### 1.1 Developer: DSL Author / Runtime Tester

**Who they are:** A software engineer who has written or inherited a `.precept` file and is verifying that it behaves as intended. They are typically in one of three modes:

- **Authoring mode:** Just wrote a new event or transition row. Does it route correctly? Do the guards work?
- **Debugging mode:** Something fired that shouldn't have, or didn't fire that should have. Why?
- **Exploratory mode:** Building familiarity with an unfamiliar precept definition. What states can this entity reach? What does each event actually do?

**Mental model:** This user thinks in terms of the precept language: states, guards, args, field mutations, constraints. They want to see _exactly_ what the engine computes — which row matched, what the guard evaluated to, which constraints fired. They are not afraid of detail.

**What they need:**
- The full transition row breakdown, not just the headline result
- Per-row guard and constraint resolution visibility
- The field diff at the row level (which fields change, to what values)
- Feedback that makes it clear whether the event _cannot fire_ vs. _might fire_ vs. _definitely fires_
- The ability to change entity state/field values and immediately see how the event landscape shifts

**Their failure mode:** They misread `Possible` as "it will probably fire" and don't realize the guard might eliminate the event. They need the distinction between `Certain` and `Possible` to be visually clear, not just labeled.

---

### 1.2 AI Agent (via MCP)

**Who they are:** An AI agent (Copilot, another LLM, an automation script) calling `precept_inspect` to reason about what transitions are available and what they'd do. The agent may be:

- Helping a developer understand what to do next
- Executing a multi-step workflow (inspect → decide → fire → inspect again)
- Debugging why an event failed

**Mental model:** The agent doesn't "see" a UI. It reads JSON. It needs every field to be self-describing. It needs `ArgErrors` to carry structured kind information so it can reason about how to fix bad args without parsing prose. It needs `OverallProspect` to quickly filter the event landscape to actionable events.

**What they need:**
- The full landscape in a single call (via `InspectUpdate` with no patch, returning `Events`)
- Per-event `OverallProspect` to filter to `Certain` and `Possible` events
- `DeclaredArgs` to know what inputs an event needs
- `ArgErrors` with `ArgErrorKind` to dispatch on kind, not parse strings
- `TransitionInspection.Prospect` to identify the winning row
- `ConstraintResult` with field attribution to understand what's blocking
- `RejectReason` from `TransitionInspection` to explain business prohibition

**Their failure mode:** They call `precept_inspect` with partial args and get back an `OverallProspect = Possible` without understanding which rows are possible and why. They need the `Transitions` array with per-row detail to reason about first-match routing.

---

### 1.3 Domain Expert / Product Owner

**Who they are:** A non-technical stakeholder who understands the business rules but doesn't read Precept DSL. They may be using a future host application that surfaces Precept-governed entities, or observing a developer's VS Code session. They want to understand: "Can this loan be approved right now? If not, why not?"

**Mental model:** They think in business terms: "the application is under review," "the adjuster hasn't been assigned," "the amount exceeds the fraud cap." They do not think in terms of guards, args, or first-match routing.

**What they need:**
- Business-language labels, not technical field names
- The headline outcome in plain language: "This will be rejected: Required documents must be complete"
- Constraint reasons (the `because` clause from the DSL) as the primary explanation, not the constraint expression
- No exposure to `Prospect` values by name — translate to "Will fire", "Might fire", "Cannot fire in this state"

**In scope for V1?** The developer persona drives the VS Code extension implementation. The domain expert persona informs _how_ information is labeled and worded in the UI — the `because` clause from constraints and reject reasons should always be the primary user-visible text, not the field/expression name. This is a writing principle, not a separate UI surface, and it's in scope now.

---

## 2. Event Landscape View — User Workflow

### 2.1 Entry Point and Layout

The user opens the preview panel (VS Code webview) on a `.precept` file. The panel shows:

1. **Entity state selector** at the top — a dropdown or segmented control showing all defined states, with the current selected state highlighted. Stateless precepts skip this row.
2. **Field value panel** — current field values, editable. Changes here trigger a re-inspection and update the event landscape in real time.
3. **Event landscape panel** — the full list of events for this precept definition, each rendered as an event card.

The field value panel and event landscape panel are the primary surfaces. They should be co-visible without scrolling on a typical 1080p screen in a split VS Code layout. If space is tight, the event landscape is the priority surface — it is the interactive focus.

---

### 2.2 The Five Visual States of an Event Card

Shane named three states. I am naming five. The distinction matters because each state requires different affordances and communicates different information.

#### State 1: Unavailable (grayed out, non-interactive)

**When:** The event has no transition rows defined for the current state (i.e., `Transitions` is empty for this state — equivalent to `EventOutcome.UndefinedEvent`).

**Visual treatment:**
- Text: 50% opacity, no hover affordance
- Icon: no icon (absence of an indicator is intentional)
- Cursor: `default` (not `pointer`)

**What the card shows:**
- Event name only
- No prospect indicator
- Subtext: "Not available in [current state]" (generated from current state selection)

**Rationale:** These events genuinely do not exist in this state. They are not "blocked" — they are simply not defined here. The user should understand this is a structural fact, not a runtime condition that might change with different field values.

---

#### State 2: Blocked (defined but cannot fire)

**When:** The event has rows defined for this state but `OverallProspect = Impossible` — every row's guard evaluated to `Impossible`, or a constraint would be violated regardless of args. No `ArgErrors` (args are valid or absent; the impossibility is from the domain logic, not the input).

**Visual treatment:**
- Icon: ⛔ or a filled red circle (distinct from warning)
- Text: full opacity, muted accent (not the interactive blue/green)
- Cursor: `pointer` (expandable to see why)
- The card is expandable — clicking it reveals the explanation

**What the card shows:**
- Event name
- Prospect indicator: ⛔ "Cannot fire"
- Subtext: the `RejectReason` if a reject row matched, or "Guard conditions not met" if a guard failed
- On expand: the transition row breakdown showing which rows evaluated to `Impossible` and why

**Rationale:** "Blocked" is different from "Unavailable." The event is defined here; the business rules prevent it from firing with the current data. The user needs to know this is a data/constraint problem, not a missing definition. In debugging scenarios, this is the most important state to explain clearly.

---

#### State 3: Needs Input (interactive, arg input required)

**When:** The event has `DeclaredArgs.Length > 0` AND `OverallProspect != Impossible` with no args provided yet (or args are incomplete). This is the entry condition — before the user has interacted.

**Visual treatment:**
- Icon: ✏️ or pencil/input icon (amber/accent color)
- Text: full opacity
- Cursor: `pointer`
- Badge: arg count hint — "2 args required" or "1 required, 1 optional"

**What the card shows:**
- Event name
- Prospect indicator: "Awaiting input" (do not show Certain/Possible until args are provided — the outcome is not meaningful without args)
- Arg count badge
- On click: expands inline arg form (see §3)

**Note:** Once args are partially or fully filled, this state transitions to one of the "Filling" states described in §3. The card in the landscape view updates accordingly.

---

#### State 4: Ready — Certain (directly clickable, green)

**When:** `DeclaredArgs.Length == 0` AND `OverallProspect = Certain`. The event will fire with no input required.

**Visual treatment:**
- Icon: ▶ or right-arrow (green)
- Text: full opacity
- Cursor: `pointer`
- Fire button or chevron indicating "click to fire"

**What the card shows:**
- Event name
- Prospect indicator: ✅ "Will fire"
- Target state (if `TransitionKind = Transition`): "→ [TargetState]"
- Field change count: "3 fields will change" (count from PostFields diff)

**On hover:** A tooltip shows the winning transition row detail — target state, top changed fields.

**Rationale:** The user should be able to fire this event with a single click. The hover preview means they can confirm the outcome before committing.

---

#### State 5: Ready — Uncertain (clickable with caution, amber)

**When:** `DeclaredArgs.Length == 0` AND `OverallProspect = Possible`. The event can be fired without args but the outcome is not guaranteed — guard conditions depend on field values that evaluate ambiguously, or multiple rows could match.

**Visual treatment:**
- Icon: ⚡ or similar "conditional" indicator (amber/yellow)
- Text: full opacity
- Cursor: `pointer`
- No immediate fire button — requires expansion first

**What the card shows:**
- Event name
- Prospect indicator: ⚡ "Might fire"
- Subtext: "Outcome depends on current field values"

**On click:** Expands to show the transition row breakdown. The user sees which rows are `Possible` and which fields are driving the ambiguity. From there they can choose to fire (with a confirmation that acknowledges uncertainty) or investigate further.

**Rationale:** Shane didn't name this state, but it's real. `VerifyDocuments` on a LoanApplication in `UnderReview` is a good example — it always fires with no args and no transition, but if there were a guard condition, it could be `Possible`. The user needs to know the outcome is not certain before they click fire. Silently letting them fire a `Possible` event without acknowledgment is a UX error.

---

### 2.3 What an Event Card Shows in the Landscape View

All event cards in the closed (non-expanded) state show:

| Element | Content |
|---------|---------|
| **Event name** | `EventName` from `EventInspection` |
| **Prospect indicator** | Icon + label per the five states above |
| **Args badge** | "N args" if `DeclaredArgs.Length > 0`, hidden otherwise |
| **Target hint** | "→ [TargetState]" when `Certain` and `TransitionKind = Transition` |
| **Field change count** | "N fields change" when `Certain` (computed from PostFields diff) |

These four elements fit on a single line for each event. No wrapping, no truncation issues in typical VS Code panel widths.

---

### 2.4 Hover Behavior on Event Cards

Hovering any event card (except Unavailable) shows a tooltip containing:

- **Arg signature**: `Submit(Applicant: string, Amount: number, Score: number, ...)`
- **Transition row count**: "3 transition rows"
- **Applicable constraint count**: "2 event ensures"
- For `Ready — Certain`: the winning row's target state and top 3 field changes (truncated with "+ N more")
- For `Ready — Uncertain`: all candidate rows (max 3 shown) with their `Prospect` indicators

The tooltip is rich but not interactive — no links, no forms. Interaction requires clicking the card.

---

### 2.5 How the Event Landscape Updates

The event landscape is driven by `Version.InspectUpdate(fields: currentFields)`. The response contains an `Events` array with `EventInspection` for every event defined in the current state.

**Update triggers:**
1. User selects a different state in the state selector → re-call `InspectUpdate` with the new state and current fields
2. User edits a field value in the field panel → debounced at 300ms, re-call `InspectUpdate`
3. Precept source file changes (recompilation) → re-call `InspectUpdate` with same state/fields

**During update:** Show a subtle loading shimmer on the prospect indicators only — not the entire panel. Event names and arg counts don't change; only `OverallProspect` and outcome hints can shift. Shimmer just the right side of each card.

**When update fails:** Keep the previous landscape visible. Show a banner: "Inspection outdated — [reason]". Add a manual refresh button. Do not blank the panel.

---

## 3. Arg Input and Real-Time Inspection — User Workflow

### 3.1 Selection and Expansion

When the user clicks a "Needs Input" event card, the card expands inline — no modal, no new panel. The expansion sits between the clicked card and the next card in the list. This keeps the full event landscape visible (above the expansion), which is important for debugging scenarios where the user may want to compare this event against adjacent ones.

The expansion contains:
- **Arg form** (top): one input field per `DeclaredArgs` entry
- **Outcome preview** (bottom): live-updating based on the current arg values
- **Action bar** (bottom): Cancel button, Fire button (state-dependent)

**Focus management:** On expansion, focus moves to the first arg input field. Pressing Escape collapses the form without firing.

---

### 3.2 Arg Form Rendering

Each `ArgDescriptor` entry in `DeclaredArgs` renders as a labeled form field:

| ArgDescriptor property | UI rendering |
|------------------------|--------------|
| `Name` | Field label (title case, camelCase split: "OrderQuantity" → "Order Quantity") |
| `Type` | Displayed as a small type badge next to the label: `number`, `string`, `boolean` |
| `IsOptional` | Required fields: label has a red asterisk. Optional fields: label has "(optional)" suffix in muted text |
| `SlotIndex` | Controls display order (ascending) |
| `Description` (if present) | Shown as inline help text below the input in muted/small text |

**Input type mapping:**
- `number`, `integer`, `decimal`, `money` → numeric text input (`type="number"` equivalent in webview)
- `string` → text input; `notempty` constraint → required field indicator
- `boolean` → checkbox (tri-state: unchecked, checked, not-set if optional)
- `date`, `datetime` → date picker or text input with date pattern
- `set of T`, `list of T` → multi-value input (comma-separated or tag input) — see OQ-4

All inputs are in a `<form>` with `autocomplete="off"`. The form does not submit on Enter by default — Enter in a text field does not fire the event.

---

### 3.3 The Real-Time Feedback Loop

This is the core of Shane's request. The feedback loop works as follows:

**Inspection call trigger:**
- Debounced 300ms after any arg field change
- Immediately on field blur (no debounce)
- Immediately when all required args are filled (completed state)

**What triggers the call:** `Version.InspectFire(eventName, currentArgs)` where `currentArgs` is whatever the user has typed so far, possibly incomplete.

**During the inspection call (latency):**
- The outcome preview area shows a subtle pulsing/shimmer animation
- The arg inputs remain interactive (do not disable)
- The fire button remains in its previous state (don't flip it to enabled/disabled until the result arrives)
- If the call takes > 1s, show a small spinner in the outcome preview area with text "Inspecting..."

**After the inspection call returns**, render based on the combination of `OverallProspect` and `ArgErrors`:

---

#### 3.3a: ArgErrors present (Scenario 3)

**Condition:** `ArgErrors.Length > 0`. `OverallProspect` will be `Impossible`.

**Per-field inline errors:** Each `ArgError` maps to its input field via `ArgName`. Below the input field, show:
- Error icon (red, ⚠)
- Error text: `ArgError.Reason` (the prose message from the runtime)
- Error kind indicator (if `ArgErrorKind` is present): `TypeMismatch` → show type hint; `UnknownArg` → show "Unexpected argument"; `ValueInvalid` → show constraint hint

**Fire button:** Disabled. Label: "Cannot fire — fix input errors"

**Outcome preview:** Do not show transition rows. Show a plain summary: "This event cannot be evaluated because of invalid argument values. Fix the errors above."

**Rationale:** Arg errors precede evaluation. Showing a partial outcome alongside arg errors would be misleading — the runtime doesn't evaluate guards or constraints when args are invalid. The feedback surface should stay strictly on the input layer.

---

#### 3.3b: Partial args, no ArgErrors (Scenario 2)

**Condition:** Some required args are not yet filled. `OverallProspect = Possible`. No `ArgErrors`.

**Arg form:** Unfilled required fields show a neutral placeholder (no error styling — the user hasn't submitted yet). Optional unfilled fields show their "(optional)" label.

**Outcome preview: "Partial preview"**

Show the transition row breakdown with rows labeled by their `Prospect`:
- `Certain` rows (if any): highlighted in green — "This row will match"
- `Possible` rows: amber — "This row might match (depends on missing args)"
- `Impossible` rows: muted/gray — "This row cannot match"

For `Possible` rows, show `PostFields` with `IsResolved = false` fields rendered as `?` in the diff table. Do not hide them — show the row exists but some values are pending. Label them: "[pending — depends on {argName}]"

**Fire button:** Disabled. Label: "Fill in required args to fire". This is non-negotiable — firing with partial args is not allowed from the UI. (The runtime accepts it, but the UI enforces completeness before fire.)

---

#### 3.3c: All args filled, no ArgErrors, OverallProspect = Certain (Scenario 1)

**Condition:** All required args provided, no errors, exactly one row is `Certain`.

**Outcome preview: "Certain outcome"**

The winning transition row is displayed prominently:

- **Row type badge**: "Transition → [TargetState]" (green, state name bold) OR "Apply (no state change)" OR "Reject: [reason]"
- **Field diff table**: Two-column table, "Before" and "After". Fields that change are highlighted (green background for after-value). Fields that don't change are shown in muted style.
  - Before values: from `CurrentFields`
  - After values: from the winning row's `PostFields`
  - Fields that don't appear in `PostFields` didn't change — show them at the bottom in a collapsed "Unchanged fields" section
- **Constraint results**: Below the diff table, if `Constraints.Length > 0`, show a "Constraints" section. `Satisfied` constraints: show in muted green with checkmark. `Violated` constraints: show in red with the `because` text prominently.

**Fire button:** Enabled, green. Label: "Fire [EventName]"

---

#### 3.3d: All args filled (or no args), OverallProspect = Possible (multiple candidate rows)

**Condition:** Args are complete and valid (or event has no args), but multiple rows could match because guards are ambiguous at the field level, or no single row is `Certain`.

**Outcome preview: "Ambiguous outcome"**

Show all non-`Impossible` rows in an ordered list, each with:
- **Row number** (1-indexed, matching first-match routing order)
- **Row prospect badge**: `Certain` (green) or `Possible` (amber)
- **Row type**: "Transition → [TargetState]" / "Apply" / "Reject: [reason]"
- **Guard summary**: A human-readable summary of the guard condition (see OQ-5 for whether the runtime provides this)
- **PostFields diff** (collapsed by default, expandable): same before/after diff as §3.3c

**Which row is "best guess"?** The first non-`Impossible` row in `Transitions` order. This row is shown with a "Best guess" badge and its field diff is shown expanded by default. The rationale: first-match routing means the first non-`Impossible` row is the most likely winner if the ambiguity resolves in the simplest direction.

**If `BestGuessRowIndex` is provided** (see my earlier recommendation): use that index rather than first-non-Impossible heuristic.

**Fire button:** Enabled, amber. Label: "Fire [EventName] (outcome uncertain)". Fire requires an additional confirmation click — a popover saying: "The exact outcome of this event depends on the current field state. The most likely outcome is shown above. Proceed?" with Cancel and Confirm options.

---

#### 3.3e: Args filled (or no args), OverallProspect = Impossible — all rows blocked

**Condition:** Args are valid (or absent) but every transition row evaluated to `Impossible`. The event is defined for this state but cannot fire.

**Outcome preview: "Blocked"**

This case needs the most diagnostic clarity. Show:

1. **Headline**: "This event cannot fire in the current state." (with ⛔ icon)
2. **Reason breakdown** — for each row in `Transitions`:
   - Row identifier (row number or guard summary)
   - Why it's `Impossible`:
     - If `RejectReason` is non-null: "This row is a rejection: [reason]"
     - If `Constraints` has `Violated` entries: show the constraint's `because` text
     - If guard failed (no `Violated` constraints, no `RejectReason`): "Guard condition not met"
3. The `because` clause from the DSL is always the user-visible explanation. The constraint expression is secondary (collapsible "technical details").

**Fire button:** Disabled. Label: "Cannot fire"

---

### 3.4 Reject Row Display

A `Reject` row (`TransitionKind = Reject`) is a business prohibition authored in the precept, not a technical error. It must be rendered differently from a constraint violation:

- **Icon**: 🚫 (prohibition, not ⚠ warning)
- **Label**: "This event will be rejected" (not "error" or "failed")
- **Reason text**: `RejectReason` in a prominent, styled block — this is the business language
- **Color**: orange/amber, not red. Red is for arg errors and constraint violations. Reject is a legitimate business decision, not a system error.

**Example rendering:**
```
🚫 Will be rejected
"Approval requires verified documents, strong credit, and affordable debt load"
```

This distinction matters for the domain expert persona. They understand business prohibition. They get confused by constraint error UI applied to authoritative business rules.

---

### 3.5 Post-Fire Feedback

When the user fires an event (via the fire button):

**Calling the runtime:** `Version.Fire(eventName, args)` is called. This is a commit — it actually changes the entity state.

**On success (`Transitioned`, `Applied`):**
- The event landscape panel shows a brief success flash on the fired event card
- The state selector updates to the new state (if `Transitioned`)
- The field values update to the post-fire values
- `InspectUpdate` is called automatically with the new state/fields, refreshing the landscape
- A toast notification: "Fired [EventName] → [new state]" (or "→ same state" for `Applied`)

**On `Rejected`:**
- Show the rejection in-place on the event card: 🚫 "[RejectReason]"
- Do NOT change state or field values (none occurred — the entity is unchanged)
- The toast notification: "Event was rejected — [reason]"

**On `ConstraintsFailed`:**
- This should not happen if inspection showed `Certain` — it indicates a race condition (fields changed between inspect and fire)
- Show an error banner: "Constraint violation occurred — the entity has changed since inspection. Refreshing..."
- Re-run `InspectUpdate` automatically
- Do not show a fire button until the new inspection result arrives

**On `InvalidArgs`:**
- This should not happen if arg form validation is working correctly
- Show an error banner in the arg form: "Argument error — [reason]"
- Re-enable the arg form for editing

**On `Unmatched`:**
- Similar to `ConstraintsFailed` — race condition between inspect and fire
- Show an error banner: "No matching transition found — the entity may have changed. Refreshing..."

---

## 4. Edge Cases and Error States

### 4.1 Event with no args, always Certain

The simplest case. Event card is in State 4 (Ready — Certain). Clicking the card fires immediately (single click, no expansion, no confirmation). The fire happens inline — the card shows a brief "firing..." state then updates to post-fire.

**What the user needs to see before clicking:** Enough to be confident. The hover tooltip shows the winning row's outcome. If they hover before clicking, they get the preview for free without any expansion.

**Accessibility:** This single-click-to-fire pattern must have keyboard support. When the card has focus and the user presses Enter or Space, the fire executes. There should be a visible focus ring (not just a browser default).

---

### 4.2 Event defined in this state but all rows Impossible regardless of args

This is State 2 (Blocked). The event card shows ⛔ "Cannot fire" and is expandable. The expansion shows every row and why it's impossible.

**Critical distinction:** The user might change field values to unblock this event. The card's prospect indicator should update in real time as fields change. If the user fixes the blocking condition (e.g., sets `DocumentsVerified = true` on a LoanApplication, enabling the Approve guard), the card transitions from Blocked to Needs Input without a page reload.

**The connection between field editing and event availability must be live.** This is the key interaction that makes the inspector feel like a true debugging tool, not a static view.

---

### 4.3 Event with multiple competing transition rows

This is the Ambiguous Outcome case (§3.3d). The critical question: **how does the user understand why one row wins over another?**

The answer is first-match routing — the engine takes the first row in document order whose guard evaluates to `Certain`. The UI must make this explicit:

- Rows are shown in their **authored order** (document order, which equals evaluation order)
- Row numbers are shown: "Row 1", "Row 2", etc.
- A visual "first match wins" annotation: the first non-`Impossible` row has a ▶ indicator; subsequent rows have a "⊘ Skipped (row 1 matched)" annotation

**Example:** LoanApplication `Approve` event in `UnderReview` state:
- Row 1: `when DocumentsVerified and CreditScore >= 680 and ...` → `Prospect = Possible` (guard depends on field values)
- Row 2: `→ reject "..."` → `Prospect = Certain` (no guard, always matches as fallback)

The UI shows Row 1 as the "best guess" but flags Row 2 as the fallback reject row. The user understands: "If the conditions in row 1 aren't all met, the event will be rejected."

---

### 4.4 Event with `on<event>` ensures constraints

`on<event>` ensures are constraints that evaluate against the post-mutation working copy, per the proposal. These appear as `EventEnsures` on `EventInspection` (if event-level) or within `TransitionInspection.Constraints` (if row-level, per my OQ-4 recommendation in the earlier review).

**Rendering:**
- Show ensures in the outcome preview, after the field diff
- Label: "Event constraints" (not "ensures" — that's DSL vocabulary, not user vocabulary)
- `Satisfied`: ✅ "[because text]" in green, collapsed by default
- `Violated`: ⚠️ "[because text]" in red/orange, expanded by default
- `Unresolvable`: ⏳ "[because text]" in amber, with note "Outcome depends on field values"

**The constraint's `because` clause is always the primary text.** The constraint expression is secondary and collapsible under "Show details."

---

### 4.5 Event with a reject row

Covered in §3.4. The key addition here is: when a reject row is the _only_ defined row (making `OverallProspect = Certain` but `TransitionKind = Reject`), the event card shows in State 2 (Blocked) rather than State 4 (Ready — Certain). The user should never be presented with a "click to fire" affordance for an event that will certainly be rejected.

**Wait, is this right?** A `Certain` reject is still an actionable fire — it commits the rejection to the entity. Some UX patterns want this visible. But from a user perspective, a "`Certain → Rejected`" event is not a useful action in most workflows. **This is OQ-1 below.**

---

### 4.6 Field values that affect event availability

Fully covered in §4.2 and the landscape update model in §2.5. The connection is live. Changing `DocumentsVerified` from `false` to `true` on a LoanApplication should immediately:

1. Re-run `InspectUpdate` with the updated fields
2. Shift the `Approve` event from Blocked → Needs Input (or possibly Ready-Uncertain)
3. The prospect indicator on the event card updates with a brief animation (fade transition, not instant snap, to draw attention to the change)

---

### 4.7 Large arg input

Some event args may be free-text strings, long notes, or even structured JSON (for future arg types). The arg form handles this by:

- Text inputs are single-line by default
- When the input value exceeds 60 characters, the field expands to a multi-line textarea (max 4 rows visible, scrollable)
- The debounce on large inputs is extended to 500ms (not 300ms) to avoid thrashing the inspection call on each keystroke

For `set of string` or `list of T` args: render as a multi-value input (comma-separated or tag-style pills). Each individual value is validated as it's added. The total value is serialized as a JSON array for the inspection call.

---

### 4.8 Error recovery — inspection call failure

The inspection call can fail:
- Network timeout (LS IPC failure)
- Runtime exception (e.g., `FaultException` from a definition bug)
- Stale state (entity was mutated externally)

**Handling:**
- Show the last valid inspection result with an amber "Stale — [reason]" banner
- Provide a "Retry" button
- If the error is a `FaultException` (definition bug), show it prominently: "Precept definition error — inspection unavailable. Check the Problems panel for errors."
- Never show an empty/blank outcome preview. The stale result is better than nothing.

---

## 5. MCP / AI Agent Workflow

### 5.1 Getting the Event Landscape

**Call:** `precept_inspect(text, currentState, data, eventArgs: null)`

When `eventArgs` is null, the tool calls `Version.InspectUpdate(fields: data)` which returns `UpdateInspection`. The `Events` array contains one `EventInspection` per event defined in the current state.

**Agent reads:**

```json
{
  "events": [
    {
      "eventName": "Approve",
      "overallProspect": "Possible",
      "declaredArgs": [
        { "name": "Amount", "type": "decimal", "isOptional": false, "slotIndex": 0 },
        { "name": "Note", "type": "string", "isOptional": true, "slotIndex": 1 }
      ],
      "argErrors": [],
      "currentFields": [...],
      "transitions": [...]
    }
  ]
}
```

**Agent reasoning:**
1. Filter `events` where `overallProspect != "Impossible"` — these are potentially actionable
2. For events with `declaredArgs.length > 0`, check which args are required (`isOptional: false`)
3. For events with no required args, check `overallProspect` — `Certain` means fire is guaranteed, `Possible` means review transitions

### 5.2 Understanding Why an Event is Blocked

**Agent pattern:** Call `precept_inspect(text, currentState, data, eventArgs: { "Amount": 5000 })` with candidate args. Read `OverallProspect`. If `Impossible`:

1. Check `argErrors` — if non-empty, fix the args per `ArgErrorKind`
2. If no `argErrors`, walk `transitions`:
   - For each row where `prospect = "Impossible"`:
     - Check `constraints` for `Violated` entries → read the `because` text
     - If no violated constraints: the guard failed (no direct guard text from the runtime; infer from transitions that the guard condition was not met)
     - If `transitionKind = "Reject"`: the event is deliberately prohibited; `rejectReason` explains why

**What the MCP response needs to make this reasoning tractable:**

```json
{
  "eventName": "Approve",
  "overallProspect": "Impossible",
  "argErrors": [],
  "transitions": [
    {
      "prospect": "Impossible",
      "transitionKind": "Transition",
      "targetState": "Approved",
      "rejectReason": null,
      "constraints": [
        {
          "status": "Violated",
          "fieldNames": ["DocumentsVerified"],
          "constraint": {
            "because": "Approval requires verified documents, strong credit, and affordable debt load"
          }
        }
      ],
      "postFields": [...]
    },
    {
      "prospect": "Certain",
      "transitionKind": "Reject",
      "targetState": null,
      "rejectReason": "Approval requires verified documents, strong credit, and affordable debt load",
      "constraints": [],
      "postFields": []
    }
  ]
}
```

The agent can reason: "Row 1 is blocked by a DocumentsVerified constraint. Row 2 is a reject row and would fire if row 1 doesn't. To reach Approved, I need to ensure DocumentsVerified is true first."

### 5.3 ArgError Detail Requirements for Agents

An agent receiving `ArgErrors` needs to know the kind, not just the prose reason. With `ArgErrorKind`:

```json
{
  "argErrors": [
    {
      "argName": "Amount",
      "kind": "TypeMismatch",
      "reason": "Expected decimal, got string"
    }
  ]
}
```

The agent dispatches on `kind`:
- `TypeMismatch` → coerce the value to the correct type before retrying
- `UnknownArg` → remove the unexpected field from args
- `ValueInvalid` → adjust the value to meet the constraint

Without `ArgErrorKind`, the agent must parse the prose `reason` string. This is fragile — it breaks on any wording change and is not language-agnostic.

**This is a hard requirement for MCP usability.** See my earlier review's OQ-2 position: add `ArgErrorKind` now.

### 5.4 Stateless Multi-Step Agent Pattern

The agent owns the entity lifecycle. The stateless MCP tool contract means the agent carries state itself:

```
1. precept_inspect(text, null, {}) → get initial landscape
2. Decide which event to fire and with what args
3. precept_inspect(text, null, {}, eventArgs: {...}) → verify outcome before committing
4. If OverallProspect = "Certain": precept_fire(text, null, {}, event, args)
5. Receive new state + fields from fire result
6. precept_inspect(text, newState, newFields) → get updated landscape
7. Repeat
```

The agent can implement this with full understanding of what each step does. The key contract properties that make this pattern work:
- Every `EventInspection` is self-describing (`EventName` on every item)
- `OverallProspect` is a simple string enum — easy to compare
- `DeclaredArgs` gives the complete arg contract without a second lookup
- The fire result carries the new `state` and field values that bootstrap the next `InspectUpdate`

---

## 6. Derived UX Requirements

The following requirements are implementation-ready. Each maps to the `EventInspection` runtime contract. Priority is from the user's perspective.

---

```
UXR-1: The event landscape panel shall display all events defined in the current state,
        grouped into exactly five visual states: Unavailable, Blocked, Needs Input,
        Ready–Certain, Ready–Uncertain. Events not defined for the current state (empty
        Transitions array) are rendered as Unavailable.
Source: §2.2 — Event landscape visual states
Priority: Must Have
Depends on: EventInspection.OverallProspect, EventInspection.Transitions (count),
            EventInspection.DeclaredArgs (count)
```

```
UXR-2: Each event card in the landscape view shall display the event name, a prospect
        indicator (icon + label), an arg count badge when DeclaredArgs.Length > 0, and
        a target state hint when OverallProspect = Certain and TransitionKind = Transition.
Source: §2.3 — Event card contents
Priority: Must Have
Depends on: EventInspection.EventName, EventInspection.OverallProspect,
            EventInspection.DeclaredArgs, TransitionInspection.TargetState,
            TransitionInspection.TransitionKind
```

```
UXR-3: The event landscape shall update automatically when the user changes the selected
        state or edits a field value. State changes shall trigger an immediate landscape
        refresh; field edits shall trigger a debounced refresh (300ms). During refresh,
        the prospect indicators shall show a shimmer animation; event names and arg badges
        shall not change.
Source: §2.5 — Landscape update triggers
Priority: Must Have
Depends on: UpdateInspection.Events (full re-inspection on InspectUpdate)
```

```
UXR-4: Hovering a non-Unavailable event card shall show a tooltip with the event's arg
        signature, transition row count, and (for Ready–Certain) the winning row's target
        state and top 3 changed fields. Tooltips shall not be interactive.
Source: §2.4 — Hover behavior
Priority: Should Have
Depends on: EventInspection.DeclaredArgs, EventInspection.Transitions,
            TransitionInspection.TargetState, TransitionInspection.PostFields
```

```
UXR-5: Clicking a Needs Input event card shall expand an inline arg form below the card.
        Focus shall move to the first arg input field on expansion. Pressing Escape shall
        collapse the form without firing. The rest of the event landscape shall remain
        visible above the expansion.
Source: §3.1 — Selection and expansion
Priority: Must Have
Depends on: EventInspection.DeclaredArgs
```

```
UXR-6: The arg form shall render one input field per DeclaredArgs entry. The label shall
        show the arg name (camelCase-split to title case), the type as a badge, and a
        required indicator (asterisk) or "(optional)" suffix based on IsOptional. Args
        shall be ordered by SlotIndex.
Source: §3.2 — Arg form rendering
Priority: Must Have
Depends on: ArgDescriptor.Name, ArgDescriptor.Type, ArgDescriptor.IsOptional,
            ArgDescriptor.SlotIndex
```

```
UXR-7: If ArgDescriptor.Description is non-null, it shall be rendered as inline help text
        below the input field in muted/small styling.
Source: §3.2 — Arg form rendering, my earlier recommendation on Description field
Priority: Should Have
Depends on: ArgDescriptor.Description (see S-1 in earlier review)
```

```
UXR-8: The inspection call (InspectFire) shall be triggered at most 300ms after the last
        arg field change (debounced), and immediately on field blur. While the inspection
        call is in flight, the outcome preview shall show a pulsing shimmer. If the call
        takes > 1s, a spinner with "Inspecting..." text shall appear in the outcome preview
        area. Arg inputs shall remain interactive during the inspection call.
Source: §3.3 — Real-time feedback loop, latency handling
Priority: Must Have
Depends on: (timing/UX contract, not runtime contract)
```

```
UXR-9: When ArgErrors is non-empty, each ArgError shall be rendered as an inline error
        below its corresponding arg input field, identified by ArgName. The error shall
        show an error icon, the Reason text, and a kind-specific hint based on ArgErrorKind
        (TypeMismatch shows expected type; UnknownArg shows "unexpected"; ValueInvalid
        shows constraint hint). The fire button shall be disabled with label "Cannot fire
        — fix input errors". No transition row output shall be shown.
Source: §3.3a — ArgErrors present
Priority: Must Have
Depends on: ArgError.ArgName, ArgError.Reason, ArgError.ArgErrorKind (Must Have),
            EventInspection.ArgErrors
```

```
UXR-10: When args are partially filled (some required args absent, no ArgErrors), the
         outcome preview shall show the transition row breakdown with each row's Prospect
         indicator. PostFields entries with IsResolved = false shall render as "?" in the
         diff table with a tooltip "[pending — depends on {argName}]". The fire button
         shall be disabled with label "Fill in required args to fire."
Source: §3.3b — Partial args
Priority: Must Have
Depends on: TransitionInspection.Prospect, FieldSnapshot.IsResolved,
            EventInspection.OverallProspect (= Possible)
```

```
UXR-11: When all args are filled, no ArgErrors, and OverallProspect = Certain, the outcome
         preview shall show: the winning row's type badge (Transition/Apply/Reject), a
         before/after field diff table sourced from CurrentFields (before) and the winning
         row's PostFields (after), with changed fields highlighted. Unchanged fields shall
         be shown in a collapsed "Unchanged fields" section. The fire button shall be
         enabled and green with label "Fire [EventName]."
Source: §3.3c — Certain outcome
Priority: Must Have
Depends on: EventInspection.CurrentFields, TransitionInspection.PostFields,
            TransitionInspection.TransitionKind, TransitionInspection.TargetState,
            EventInspection.OverallProspect (= Certain)
```

```
UXR-12: When OverallProspect = Possible (args complete, no errors, or no args), the
         outcome preview shall show all non-Impossible transition rows in document order
         with their row number, Prospect badge, type, and a collapsed field diff. The
         first non-Impossible row (or BestGuessRowIndex if provided) shall be labeled
         "Best guess" and shown expanded by default. The fire button shall be enabled
         in amber with label "Fire [EventName] (outcome uncertain)". Firing shall require
         a secondary confirmation with text explaining the uncertainty.
Source: §3.3d — Possible outcome, §3.3 fire confirmation
Priority: Must Have
Depends on: TransitionInspection.Prospect, TransitionInspection.PostFields,
            EventInspection.OverallProspect (= Possible), BestGuessRowIndex (Should Have)
```

```
UXR-13: When OverallProspect = Impossible, no ArgErrors, the outcome preview shall show
         a "Cannot fire" headline, followed by a per-row explanation: Reject rows show
         "Will be rejected: [RejectReason]"; rows with Violated constraints show the
         constraint's because text; rows whose guard failed (no violated constraints, no
         reject) show "Guard condition not met." The fire button shall be disabled with
         label "Cannot fire."
Source: §3.3e — Impossible outcome, §4.5 reject row
Priority: Must Have
Depends on: TransitionInspection.Prospect (= Impossible), TransitionInspection.TransitionKind,
            TransitionInspection.RejectReason, ConstraintResult.Status,
            ConstraintDescriptor.Because
```

```
UXR-14: A Reject transition row (TransitionKind = Reject) shall be rendered with a 🚫
         prohibition icon and amber/orange styling, distinct from constraint violations
         (red) and arg errors (red). The RejectReason text shall be the primary displayed
         content, rendered in a styled block. The word "error" shall not be used for
         reject rows — the label shall be "Will be rejected" or "Rejected."
Source: §3.4 — Reject row display
Priority: Must Have
Depends on: TransitionInspection.TransitionKind (= Reject),
            TransitionInspection.RejectReason
```

```
UXR-15: EventEnsures constraints and per-row ensures shall be rendered in an "Event
         constraints" section below the field diff table. Satisfied constraints shall
         show a checkmark and the because text in muted green, collapsed by default.
         Violated constraints shall show a warning icon and the because text in red,
         expanded by default. Unresolvable constraints shall show an amber clock icon
         with "Outcome depends on field values." The constraint expression shall be in
         a collapsible "Show details" section, not the primary visible content.
Source: §4.4 — EventEnsures rendering
Priority: Must Have
Depends on: ConstraintResult.Status, ConstraintDescriptor.Because,
            EventInspection.EventEnsures (or TransitionInspection.Constraints)
```

```
UXR-16: The constraint's "because" clause shall always be the primary user-visible
         explanation text for constraint results, rejections, and blocked events.
         The constraint expression or guard condition shall only appear as secondary
         content in a "Show details" collapsed section.
Source: §1.3 domain expert persona, §3.3e, §4.4
Priority: Must Have
Depends on: ConstraintDescriptor.Because (the "because" clause from the DSL)
```

```
UXR-17: When the user fires an event successfully, the entity state and field values
         shall update to reflect the post-fire result, the event landscape shall
         automatically refresh via InspectUpdate, and a toast notification shall
         appear with the fired event name and new state. The fired event card shall
         briefly flash green before the landscape re-renders.
Source: §3.5 — Post-fire feedback
Priority: Must Have
Depends on: EventOutcome.Transitioned.Result, EventOutcome.Applied.Result
```

```
UXR-18: When a fire call returns ConstraintsFailed or Unmatched (race condition between
         inspect and fire), an error banner shall appear explaining that the entity state
         changed since inspection, and an automatic InspectUpdate refresh shall be
         triggered. The fire button shall not be re-enabled until the refresh completes.
Source: §3.5 — Post-fire error handling
Priority: Must Have
Depends on: EventOutcome.ConstraintsFailed, EventOutcome.Unmatched
```

```
UXR-19: When an inspection call fails (IPC timeout, FaultException, etc.), the last
         valid inspection result shall remain displayed with an amber "Stale — [reason]"
         banner and a manual Retry button. When the failure is a FaultException (definition
         bug), the banner shall instead read "Precept definition error — check the Problems
         panel." The outcome preview shall never be blank.
Source: §4.8 — Error recovery
Priority: Must Have
Depends on: (error handling contract, not runtime shape)
```

```
UXR-20: Transition rows in the outcome preview shall be displayed in document (first-match)
         order with 1-indexed row numbers. The first non-Impossible row shall have a ▶
         indicator. Subsequent rows shall have a "⊘ Skipped (row N matched)" annotation
         when a prior Certain row precedes them.
Source: §4.3 — Multiple competing rows
Priority: Must Have
Depends on: TransitionInspection.Prospect, ordering from Transitions array
```

```
UXR-21: All event cards and arg inputs shall support full keyboard navigation. Tab order
         shall flow: state selector → field value inputs → event cards (by prospect
         priority: Certain first, then Possible, then Needs Input, then Blocked, then
         Unavailable). Enter or Space on a focusable event card shall activate it (fire
         for Certain, expand for others). The arg form shall trap focus within the
         expansion (focus loop between inputs and action buttons). Escape collapses.
Source: §4.1 — Keyboard navigation, accessibility
Priority: Must Have
Depends on: (focus management contract, not runtime shape)
```

```
UXR-22: All event prospect indicators shall use both color and icon (not color alone) to
         communicate status. Color combinations shall meet WCAG 2.1 AA contrast requirements
         (4.5:1 minimum for text, 3:1 for UI components). Prospect states shall be
         announced to screen readers via aria-label: "Approve event — will fire",
         "Decline event — requires 1 argument", "Submit event — cannot fire in current
         state."
Source: §4.1 — Accessibility
Priority: Must Have
Depends on: EventInspection.EventName, EventInspection.OverallProspect
```

```
UXR-23: The MCP precept_inspect tool, when called without eventArgs, shall return an
         UpdateInspection containing the Events array, where each EventInspection entry
         is self-describing (EventName on each entry). Consumers shall not need to
         correlate EventInspection entries back to a separate event list.
Source: §5.1 — MCP landscape call
Priority: Must Have
Depends on: EventInspection.EventName, UpdateInspection.Events
```

```
UXR-24: The MCP precept_inspect response shall include ArgErrorKind on each ArgError
         entry, with values TypeMismatch, UnknownArg, or ValueInvalid. AI agent consumers
         shall be able to dispatch on Kind without parsing the Reason string.
Source: §5.3 — ArgError kind for agents
Priority: Must Have
Depends on: ArgError.ArgErrorKind (requires adoption of my OQ-2 recommendation)
```

```
UXR-25: The MCP precept_inspect response shall include RejectReason on TransitionInspection
         entries where TransitionKind = Reject. AI agent consumers shall be able to
         distinguish a business rejection (RejectReason non-null) from a guard failure
         (RejectReason null, constraints empty) from a constraint violation (constraints
         with Violated entries) without additional API calls.
Source: §5.2 — Agent debugging pattern
Priority: Must Have
Depends on: TransitionInspection.TransitionKind, TransitionInspection.RejectReason,
            ConstraintResult.Status
```

```
UXR-26: When a text arg input value exceeds 60 characters, the input shall automatically
         expand to a multi-line textarea (max 4 visible rows, scrollable). The debounce
         delay for inspection calls shall extend to 500ms for inputs longer than 60
         characters.
Source: §4.7 — Large arg input
Priority: Should Have
Depends on: (UI behavior, not runtime contract)
```

```
UXR-27: When the user changes a field value in the field panel, the event landscape shall
         update in real time (debounced 300ms). If a previously Blocked event becomes
         available, or a previously available event becomes Blocked, the prospect indicator
         shall animate with a fade transition (not an instant snap) to draw attention to
         the state change.
Source: §4.6 — Field changes affecting event availability
Priority: Should Have
Depends on: UpdateInspection.Events (re-inspection on InspectUpdate with new field values)
```

```
UXR-28: An event whose only defined row is a Reject row (making OverallProspect = Certain,
         TransitionKind = Reject) shall be rendered as Blocked (State 2: ⛔ "Cannot fire"),
         not as Ready–Certain. The fire button shall be disabled. Expanding the card shall
         show the reject row and its RejectReason. See OQ-1 for the design call on this.
Source: §4.5 — Certain-reject edge case
Priority: Must Have (pending OQ-1 resolution)
Depends on: TransitionInspection.TransitionKind (= Reject),
            EventInspection.OverallProspect (= Certain)
```

```
UXR-29: The fire button shall never be the only affordance for understanding an event's
         outcome. Before any fire action is available, the user shall have had access to
         the outcome preview. Single-click fire (State 4 Ready–Certain) satisfies this
         requirement only because the hover tooltip provides a preview before the click.
Source: §4.1 — Single-click fire + hover preview
Priority: Must Have
Depends on: (UX contract, not runtime shape)
```

```
UXR-30: When an event has no DeclaredArgs and OverallProspect = Certain, clicking the
         event card shall fire the event immediately (single click). No expansion or
         confirmation is required. The card shall show a brief "Firing..." loading state
         (< 200ms visual) before the post-fire refresh.
Source: §4.1 — No-args Certain case
Priority: Must Have
Depends on: EventInspection.DeclaredArgs (empty), EventInspection.OverallProspect (= Certain)
```

---

## 7. Open Questions for Shane

These are design decisions I cannot resolve from first principles. I've named the tradeoff and my lean.

---

**OQ-1: Should a Certain-reject event be rendered as Blocked or as Ready?**

A reject row is a legitimate business operation — it changes the entity (to a rejected state in some precepts, or records the rejection reason). An event with one `Certain` reject row is unambiguously deterministic: fire it and you get a rejection. This is different from an event that _might_ reject.

- **Option A (my lean): Render as Blocked.** The UX goal of "directly clickable" implies "this fires and does something useful." A certain rejection is a useful business operation in some contexts (e.g., explicitly declining a claim) but it's never a frictionless path forward. Treating it as Blocked ensures the user sees the rejection message before committing. Fire is still possible (via expansion), just not single-click.
- **Option B:** Render as Ready–Certain, fire button enabled, with the reject outcome clearly shown in the outcome preview. This respects the user's intent if they're deliberately firing a reject event. The rejection outcome is fully visible before they fire.

**My lean:** Option A for the event landscape card (never show a ▶ fire button for a certain-reject). But Option B for the fire button within the expansion — once the user has opened the event and seen the rejection details, the fire button should be enabled. The friction is knowing, not preventing.

---

**OQ-2: Should "outcome depends on field values" be shown differently from "outcome depends on missing args"?**

Currently both cases produce `OverallProspect = Possible`. The user's experience is different:
- **No args:** "Fill in the args and you'll know the outcome"
- **Guard ambiguity (no args involved):** "The outcome depends on the current field data that I've already given you"

The second case is potentially confusing — the user has provided all the data, but the outcome is still `Possible`. They might think they need to do something more. In reality, it's the guard logic itself that's underdetermined at the Kleene level (e.g., a guard that compares two derived fields where the derivation is uncertain).

**My lean:** Differentiate the label. When `DeclaredArgs.Length == 0` and `OverallProspect = Possible`: label as "Outcome depends on field values" (not "Awaiting input"). When `DeclaredArgs.Length > 0` and args are incomplete: "Awaiting args." These two Possible states are meaningfully different to the user.

But this requires knowing _why_ it's Possible (args-absent vs. field-ambiguity). I cannot distinguish these from the `EventInspection` shape alone — both produce `OverallProspect = Possible`. Do I need a flag from the runtime, or should the UI just apply the heuristic: "if DeclaredArgs.Length == 0 and OverallProspect = Possible, it's field-dependent"?

The heuristic is almost always right. Call it.

---

**OQ-3: How are `set of string` and `list of T` args rendered in the arg form?**

The DSL supports collection types. An event arg of type `set of string` (e.g., a set of document names) would need a multi-value input. Options:
- **Tag input** (pills with an X to remove): clean UX but complex to implement in a webview
- **Comma-separated text field**: simple to implement, parses on inspection call; but ambiguous for values that contain commas
- **Line-by-line textarea**: one value per line, unambiguous

**My lean:** Comma-separated for strings without commas (simple case, covers most args). Fall back to textarea when the arg's type hint suggests long values. But this is an implementation detail I'd leave to Kramer with guidance: "comma-separated by default, textarea option available."

**The question for Shane:** Is this a V1 concern? If collection-type event args aren't in any current sample precept, we could defer the collection-input UI and treat it as unsupported in V1 with a clear error state.

---

**OQ-4: Should the inspect panel show the field diff as a table or as a list of changed fields only?**

Two options for the field diff display:
- **Full table (before/after all fields):** Shows every field, changed highlighted. Gives complete picture but can be overwhelming for entities with 15+ fields.
- **Changed fields only (delta view):** Shows only fields that differ between `CurrentFields` and `PostFields`. Compact, but the user has to scroll elsewhere to see the full post-state.

**My lean:** Changed fields as the default view, with a "Show all fields" toggle that reveals the full table. The delta is almost always what the user cares about.

**The question:** Should "unchanged fields" be shown in a collapsed section at the bottom of the diff table, or in a separate "Full state" tab? My lean is collapsed section (simpler).

---

**OQ-5: Should the UI attempt to render guard conditions in human-readable form?**

In §4.3, I noted that showing "which guard drove this row's Impossible" helps the user understand why one row wins over another. The runtime `TransitionInspection` doesn't carry a parsed guard summary — it carries only `Prospect` and `Constraints`.

To show "Guard condition not met," the UI would need either:
- A guard summary string from the runtime (not currently in the contract)
- The ability to read the source DSL and extract the guard expression (fragile)
- A generic fallback: "Guard condition not met (see precept source)"

**My lean:** Generic fallback for V1. The constraint `because` text and reject reason cover the cases that matter most. Guard failure without a constraint violation is the least common path and least useful to explain in isolation. Defer guard expression rendering to V2.

**The question:** Should the generic fallback include a "go to definition" link that jumps to the relevant transition row in the source file? This would be high-value for the developer persona and is within LSP capabilities. Kramer could implement it if the runtime includes the transition row's source span. I recommend yes, but this needs a call on whether source spans belong in `TransitionInspection`.

---

**OQ-6: Does the event interaction surface need a "what does this event do" description, separate from the outcome preview?**

The `ArgDescriptor.Description` field (which I recommended adding in my earlier review) would let the precept author annotate event args with help text. But there's no equivalent for the event itself — no `EventDescriptor.Description`.

The developer persona probably doesn't need this (they wrote the DSL). The domain expert persona would benefit enormously ("Submit — starts the loan application review process").

**My lean:** For V1, the event name and the `because` clauses of its transition rows are sufficient description. For V2, add `EventDescriptor.Description` (an optional authored annotation in the DSL) that surfaces as the first line of the event card.

**The question:** Should V1 generate a description from the winning transition row's target state? E.g., "Submit → Transitions to UnderReview." This is computable from the runtime contract today. My lean: yes, show this for Certain events in the hover tooltip.

---

**OQ-7: Should the arg form persist across event selections?**

If the user opens the arg form for `Approve`, types values, then clicks away to look at another event, should the `Approve` arg values be preserved when they come back?

**My lean:** Persist within a session (until the precept recompiles or the state changes). Losing typed values mid-investigation is frustrating, especially when the user is comparing what happens with different arg values across multiple events.

**The question:** What's the session scope? My lean: per-document-session in the VS Code extension. Precept source file recompilation clears all preserved arg forms (new entity definition = new arg contracts). State change clears all forms. Field value change does NOT clear forms (the user may be testing whether changing a field unblocks an event that already has args ready).

---

*Document complete. Ready for Shane's calls on OQ-1 through OQ-7.*
