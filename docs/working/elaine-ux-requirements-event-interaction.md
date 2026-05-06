# Elaine: UX Requirements — Event Interaction and Real-Time Inspection

**Author:** Elaine (UX Designer)
**Date:** 2026-05-06
**Status:** Draft — pending Shane's calls on OQ-8, OQ-9 (OQ-1, OQ-2, OQ-3, OQ-4, OQ-5, OQ-6, OQ-7, OQ-10 closed)
**Revision:** Elaine-32 (2026-05-06) — ArgErrorKind removal: aligned arg error display to field edit pattern (string Reason only); removed ArgErrorKind from UXR-9 and §5.3; removed UXR-24.
**Informs:** VS Code extension implementation, CC#12, CC#23, CC#24

---

## 0. Grounding

This document derives from Shane's two seed ideas for the event interaction surface:

> "A user is viewing a current version — they should see all events that are defined, with some of them grayed out, some of them potentially clickable but need args, and others directly clickable."

> "A user picks one of those available events, and starts filling out args (if there are args defined). As the user types, and before the event is fired, inspection should provide feedback on the args as well as the potential outcome(s)."

Shane explicitly asked for depth beyond the seeds. This document completes the picture: missing states, edge cases, accessibility requirements, AI agent concerns, and open design questions where I need a call.

The runtime contract I'm designing against is the `EventInspection` shape proposed in `docs/working/event-inspection-proposal.md`:
- `EventName`, `OverallProspect`, `DeclaredArgs`, `ArgErrors`, `CurrentFields`, `Transitions`, `EventEnsures`
- `TransitionInspection`: `Prospect`, `Effect` (RowEffect.TransitionTo / RowEffect.NoTransition / RowEffect.Rejection), `Constraints`, `PostFields`

My earlier review (`docs/working/elaine-ux-review-event-inspection.md`) recommended `ArgErrorKind` on `ArgError`, sealed DU for `RowEffect`, and per-row `EventEnsures`. The `ArgErrorKind` recommendation was not adopted — CC#8 OQ-2 is resolved: `ArgError` uses string `Reason` only, mirroring `ConstraintViolation.Because`. The remaining recommendations inform the requirements below.

---

## 0.5 Mental Model — The Precept Instance as a Business Record

**Read this section first.** Every design decision in this document flows from understanding what a precept instance actually is.

### What it's like

A precept instance is a **governed business record**. Think of it as:

- A **loan application** at a bank: it has fields (amount, credit score, income, documents-verified flag), states (Draft → UnderReview → Approved → Funded), events that advance it (Submit, VerifyDocuments, Approve, Decline), and business rules that hold at every step. The `Approve` event won't fire unless documents are verified, credit score is sufficient, and debt-to-income ratios are within policy — all enforced by the engine, not checked by scattered service code.
- An **insurance claim**: it carries data (claim amount, fraud flag, missing documents set), moves through states (Submitted → UnderReview → Approved → Paid), and has events (RequestDocument, AssignAdjuster, Approve, Deny). The `Approve` event won't fire until outstanding documents are cleared — the engine prevents it structurally.
- A **CRM opportunity**: it has a current stage, fields relevant at each stage, and actions that advance it or block it when requirements aren't met.

The critical difference from these analogies: **governed integrity**. In Salesforce or a CRM, you can often click past warnings and save invalid data. A precept instance cannot be placed in an invalid configuration. The rules are enforced structurally — on every operation, including direct field edits.

### What a precept instance presents to a user

**Current state** (when lifecycle exists): Where is the entity in its lifecycle? State is not just a label — it is an active rule-activator. The state determines which rules apply, which fields are editable, and which events are available. An `InsuranceClaim` in `UnderReview` has different editable fields, different rules in force, and different available events than one in `Draft`.

**Current field values**: The data the entity holds right now. Some fields are editable in the current state (`writable` globally, or `in State modify X editable`). Some are conditionally editable (guarded: `editable when Guard`). Computed fields are derived from others and are never editable. Omitted fields structurally don't exist in the current state.

**Available events**: What can happen next? Some events fire with certainty (no args, all guards met). Some need input (args required before they can be evaluated). Some are blocked (guard conditions fail, or constraints would be violated with current data). Some are defined in the DSL but don't exist in the current state at all.

**History** (host-provided): What events fired in the past, what data changed. The runtime doesn't maintain history — the host application provides this if the Event Timeline surface is used.

### The three interaction modes the panel supports

Every interaction with a Precept-governed entity through the inspector panel falls into exactly one of three modes:

1. **Instance creation** — firing the constructor event (an "initial event," if defined) to bring a new governed entity into existence. This is a first-class panel interaction, not delegated to the host application. If the constructor event accepts arguments, valid args must be supplied before the instance can be created. The panel's state before this action is "no instance yet" — a distinct mode, not just an empty Data Form. Until instance creation is complete, the event firing and field editing interactions below do not apply.

2. **Event firing** (change path) — firing lifecycle events on an existing instance to drive it through its state machine. This is the primary mechanism for advancing or redirecting the entity's governed state. Uses `Version.Fire()`.

3. **Data field editing** (change path) — directly mutating fields on an existing instance via the Data Form. Subject to access mode rules and constraint enforcement. Never transitions state. Uses `Version.Update()`.

Instance creation only applies before the instance exists. Event firing and field editing apply to an existing instance. These are three meaningfully distinct states for the panel — not one surface with different content.

### The two-path model of change

Every change to a precept instance takes exactly one of two paths:

1. **Event path** — `Version.Fire()`: an event fires, guards are evaluated, the action chain runs, constraints are checked, and the entity either commits to a new state/fields or rejects. May transition state. This is the `from State on Event -> ...` machinery.

2. **Direct edit path** — `Version.Update()`: fields are patched directly, subject to access mode rules (which fields are editable in the current state) and constraint enforcement. Never transitions state. This is the `writable` / `in State modify X editable` machinery.

These paths are **mutually exclusive** and structurally distinct. An `Update` never triggers a transition. A `Fire` never patches fields outside its declared action chain. A user editing a field in the Data Form is on the direct edit path. A user clicking "Fire Approve" is on the event path.

### The most important interaction: data shapes event availability

Changing `DocumentsVerified` from `false` to `true` on a `LoanApplication` doesn't fire an event — it is a direct field edit via `Update()`. But it changes which events are available: the `Approve` event was Blocked because its guard requires `DocumentsVerified`. After the edit, `Approve` may become Possible or Certain.

The Data Form (field values) and the event actions region (event cards) are not independent surfaces — they are two views of the same governed entity. The Data Form shows what is true now; the event actions region shows what can happen next. Changing one reshapes the other, in real time.

This bidirectional relationship is the central UX interaction in the entire preview surface.

---

## 1. User Personas and Context

### 1.1 Business Analyst / Domain Expert

**Who they are:** A domain expert who owns, validates, or governs the business rules encoded in a `.precept` file — but is not a software engineer. They have analyst-tier technical capability: comfortable writing SQL queries, building Power BI dashboards, reading structured data, and reasoning about logic. They do not read or write code, and they do not author Precept DSL from scratch.

**Authoring mode:** This persona's primary authoring path is AI-assisted. An AI agent (via MCP) designs the precept definition — generating fields, states, events, and rules — and the analyst reviews, validates, and refines the output. They are the domain authority: they know whether the rule is right; they rely on the AI to express it correctly in DSL.

This is a spectrum, not a mandate. Precept places no requirement on how a definition was produced. Some analysts will author definitions directly after reading a few examples; others will always work through an AI layer. Both are first-class uses. The language's readability must serve both: a human authoring from scratch needs clear, low-ceremony syntax, and a human reviewing AI output needs language that reads as a faithful expression of business intent.

**Mental model:** This user thinks in business terms: "Can the claim be approved right now? If not, why not? What has to be true before the Approve event can fire?" They do not reason about guards, first-match routing, or arg schemas — but they understand the governed entity's rules and lifecycle because they wrote those rules as business policy, even if someone else translated them into DSL.

From `docs/philosophy.md`: Precept's core offer is **governed integrity** — the entity's data satisfies its declared rules at every moment, making invalid configurations structurally impossible. This persona is the domain authority on what "invalid" means.

**What they need:**
- A legible, reviewable rendering of the governed entity's current state and available actions
- Business-language outcome descriptions — the `because` clause from constraints, not expression names
- Confidence that what they see in the inspector faithfully represents the rules they specified
- The ability to validate AI-generated precept output by testing it against real business scenarios

**Their failure mode:** They accept an AI-generated precept as correct without verifying edge cases, because the inspector surface doesn't make it easy to test boundary conditions. The UX must make it ergonomic to walk through representative scenarios, not just the happy path.

---

### 1.2 AI Agent (via MCP)

**Who they are:** An AI agent (Copilot, another LLM, an automation script) calling `precept_inspect` to reason about what transitions are available and what they'd do. The agent may be:

- Helping a developer understand what to do next
- Executing a multi-step workflow (inspect → decide → fire → inspect again)
- Debugging why an event failed

**Mental model:** The agent doesn't "see" a UI. It reads JSON. It needs every field to be self-describing. It needs `ArgErrors` with `Reason` strings to understand why args are invalid and how to fix them. It needs `OverallProspect` to quickly filter the Event Timeline to actionable events.

**What they need:**
- The full landscape in a single call (via `InspectUpdate` with no patch, returning `Events`)
- Per-event `OverallProspect` to filter to `Certain` and `Possible` events
- `DeclaredArgs` to know what inputs an event needs
- `ArgErrors` with `Reason` strings to understand why args failed
- `TransitionInspection.Prospect` to identify the winning row
- `ConstraintResult` with field attribution to understand what's blocking
- `RowEffect.Rejection.Reason` via `TransitionInspection.Effect` to explain business prohibition

**Their failure mode:** They call `precept_inspect` with partial args and get back an `OverallProspect = Possible` without understanding which rows are possible and why. They need the `Transitions` array with per-row detail to reason about first-match routing.

---

### 1.3 End-User

**Who they are:** Someone interacting with a Precept-governed business object inside a running application. They have no DSL awareness whatsoever — they do not know what a precept is, what a transition row is, or that the engine exists at all. They are using a product that happens to be built on top of Precept.

**What they care about:**
- What can I do right now? Which actions are available to me?
- Why is this action blocked? What's preventing me from proceeding?
- What will happen if I do X? What changes when I click this button?

**Mental model:** Entirely in domain terms. "The loan application is under review." "The Approve button is grayed out — what's missing?" "I set the adjuster — now what?" They do not think about states, guards, args, or constraint expressions. They think about the business object and the actions it affords.

**UX implications:** Every label, message, and explanation visible to this persona must speak in the domain's language. Constraint `because` clauses are written by the domain author and surface directly as the user-facing explanation. Runtime or precept terminology — "transition," "guard," "constraint," "prospect," "event fires" — must never appear. Say "This cannot be submitted yet: Required documents must be complete," not "Event prospect is Impossible due to guard evaluation."

**In scope for V1?** This persona is not the primary driver of the VS Code extension implementation — that surface is author/agent-facing. But the principle that business-language explanations take priority over technical labels is in scope at every tier. The `because` clause from constraints is the explanation. This is a writing principle that applies now.

**Note:** This persona is not authoring, not testing, and not validating a precept definition. They are using a product built on top of Precept. The inspector surface in VS Code does not serve this persona directly — but the UX patterns it establishes for how to surface governed-entity information will propagate into the host application's UI.

---

## 2. Event Landscape View — User Workflow

### 2.1 Entry Point and Layout

The user opens the preview panel (VS Code webview) on a `.precept` file. The panel shows the **Data Form** — the configuration-reading surface. The Data Form contains:

1. **Entity state selector** at the top — a dropdown or segmented control showing all defined states, with the current selected state highlighted. Stateless precepts skip this row.
2. **Fields region** — current field values, with editability signals per field. Changes here trigger a re-inspection and update the event actions region in real time.
3. **Event actions region** — the full list of events for this precept definition, each rendered as an event card.

The fields region and event actions region together form the Data Form. Both should be co-visible without scrolling on a typical 1080p screen in a split VS Code layout. The Data Form and Event Timeline are **peer surfaces** — neither is architecturally primary over the other. If a layout constraint forces a choice between surfaces (e.g., constrained viewport, single-column layout), the Data Form takes priority over the Event Timeline.

> **Terminology note**: "Inspector" is not a surface name in this product. The canonical term is **Data Form**. The VS Code webview container is the "preview panel"; what it displays is the Data Form for the current precept instance.

---

### 2.2 The Four Visual States of an Event Card

Shane named three states. I am naming four. The distinction matters because each state requires different affordances and communicates different information.

#### State 1: Unavailable (grayed out, non-interactive)

**When:** The event IS defined in the precept for this state, but is currently blocked or non-interactive — present in this state's definition but not operable. This state covers events that exist in the precept for this state but cannot be acted upon in the current context.

> **`undefined` events — not rendered.** An event that does not exist at all for the current state (EventOutcome.UndefinedEvent) is **not rendered** in the Event Timeline — no grayed-out card, no placeholder, no entry of any kind. `undefined` means the event has no presence here; hiding it entirely is the correct UX. Only events that exist in the precept for this state but are currently non-interactive receive the Unavailable treatment.

**Visual treatment:**
- Text: `--support-disabled-text: rgba(158,171,190,0.42)`, no hover affordance
- Icon: no icon (absence of an indicator is intentional)
- Cursor: `default` (not `pointer`)
- Card surface: `--support-disabled-surface: #0d1014`; control: `--support-disabled-controls: #1f2a39`
- Event name: mono text in `--support-disabled-text: rgba(158,171,190,0.42)` — the semantic disabled token, not a tint of the event color (`--event`)

**What the card shows:**
- Event name only
- No prospect indicator
- Subtext: "Not available in [current state]" (generated from current state selection)

**Rationale:** This event exists in the precept for this state but cannot currently be acted upon. It is not a guard failure (that is State 2, Blocked) — it is a structural non-interactivity. The user should understand this is a structural fact about the current state's definition, not a data condition that might change with different field values.

---

#### State 2: Blocked (defined but cannot fire)

**When:** The event has rows defined for this state but `OverallProspect = Impossible` — every row's guard evaluated to `Impossible`, or a constraint would be violated regardless of args. No `ArgErrors` (args are valid or absent; the impossibility is from the domain logic, not the input).

**Visual treatment:**
- Icon: ⛔ or a filled circle in violated color (distinct from warning)
- Text: full opacity, event name in event color (`--event: #30B8E8`)
- Verdict indicator: violated color (`--violated: #F87171`) — dashed border on the card (semantic visual system: dashed border = blocked/violated, exclusive signal)
- Cursor: `pointer` (expandable to see why)
- The card is expandable — clicking it reveals the explanation

**What the card shows:**
- Event name
- Prospect indicator: ⛔ "Cannot fire"
- Subtext: the `RowEffect.Rejection.Reason` (from `TransitionInspection.Effect`) if `Effect` is `RowEffect.Rejection`, or "Guard conditions not met" if a guard failed
- On expand: the transition row breakdown showing which rows evaluated to `Impossible` and why

**Rationale:** "Blocked" is different from "Unavailable." The event is defined here; the business rules prevent it from firing with the current data. The user needs to know this is a data/constraint problem, not a missing definition. In debugging scenarios, this is the most important state to explain clearly.

---

#### State 3: Needs Input (interactive, arg input required)

**When:** The event has `DeclaredArgs.Length > 0` AND `OverallProspect != Impossible` with no args provided yet (or args are incomplete). This is the entry condition — before the user has interacted.

**Visual treatment:**
- Icon: ✏️ or pencil/input icon (warning color, `--warn: #FDE047`)
- Text: full opacity, event name in event color (`--event: #30B8E8`)
- Cursor: `pointer`
- Badge: arg count hint — "2 args required" or "1 required, 1 optional"

**What the card shows:**
- Event name
- Prospect indicator: "Awaiting input" (do not show Certain/Possible until args are provided — the outcome is not meaningful without args)
- Arg count badge
- On click: opens the arg input dialog (see §3)

**Note:** Once args are partially or fully filled, this state transitions to one of the "Filling" states described in §3. The card in the landscape view updates accordingly.

---

#### State 4: Ready — Certain (directly clickable)

**When:** `DeclaredArgs.Length == 0` AND `OverallProspect = Certain`. The event will fire with no input required.

**Visual treatment:**
- Icon: ▶ or right-arrow (enabled/satisfied verdict color, `--valid: #34D399`)
- Text: full opacity, event name in event color (`--event: #30B8E8`)
- Cursor: `pointer`
- The card is the fire trigger — clicking fires the event directly. No separate fire button inside the card. The ▶ indicator signals readiness; the card's pointer cursor and active/hover styling are the only affordance needed.

**What the card shows:**
- Event name
- Prospect indicator: ✅ "Will fire"
- Target state (if `Effect` is `RowEffect.TransitionTo`): "→ [TargetState]"
- Field change count: "3 fields will change" (count from PostFields diff)

**On hover:** A tooltip shows the winning transition row detail — target state, top changed fields.

**Rationale:** The user should be able to fire this event with a single click. The hover preview means they can confirm the outcome before committing.

---

### 2.3 What an Event Card Shows in the Landscape View

All event cards in the closed (non-expanded) state show:

| Element | Content |
|---------|---------|
| **Event name** | `EventName` from `EventInspection` |
| **Prospect indicator** | Icon + label per the four states above |
| **Args badge** | "N args" if `DeclaredArgs.Length > 0`, hidden otherwise |
| **Target hint** | "→ [TargetState]" when `Certain` and `Effect` is `RowEffect.TransitionTo` |
| **Field change count** | "N fields change" when `Certain` (computed from PostFields diff) |

These four elements fit on a single line for each event. No wrapping, no truncation issues in typical VS Code panel widths.

---

### 2.4 Hover Behavior on Event Cards

Hovering any event card (except Unavailable) shows a tooltip containing:

- **Arg signature**: `Submit(Applicant: string, Amount: number, Score: number, ...)`
- **Transition row count**: "3 transition rows"
- **Applicable constraint count**: "2 event ensures"
- For `Ready — Certain`: the winning row's target state and top 3 field changes (truncated with "+ N more")

The tooltip is rich but not interactive — no links, no forms. Interaction requires clicking the card.

---

### 2.5 How the Event Landscape Updates

The event actions region is driven by `Version.InspectUpdate(fields: currentFields)`. The response contains an `Events` array with `EventInspection` for every event defined in the current state.

**Update triggers:**
1. User selects a different state in the state selector → re-call `InspectUpdate` with the new state and current fields
2. User edits a field value in the field panel → debounced at 300ms, re-call `InspectUpdate`
3. Precept source file changes (recompilation) → re-call `InspectUpdate` with same state/fields

**During update:** Show a subtle loading shimmer on the prospect indicators only — not the entire panel. Event names and arg counts don't change; only `OverallProspect` and outcome hints can shift. Shimmer just the right side of each card.

**When update fails:** Keep the previous landscape visible. Show a banner: "Inspection outdated — [reason]". Add a manual refresh button. Do not blank the panel.

---

## 3. Arg Input and Real-Time Inspection — User Workflow

### 3.1 Selection and Dialog

When the user clicks a "Needs Input" event card, an arg input **dialog** opens — not an inline expansion. The dialog is a modal overlay; it does not push other event cards, collapse the event actions region, or alter the Data Form behind it.

The dialog contains:
- **Header**: event name in event color (`--event: #30B8E8`) on the left; target state hint in state color (`--state: #A898F5`) at the top right when the event has a deterministic target state
- **Body — Arg form**: one input field per `DeclaredArgs` entry (see §3.2)
- **Body — Outcome preview**: live-updating based on current arg values (see §3.3)
- **Footer**: Cancel button (left), OK / Fire button (right, state-dependent — see §3.3 for OK button behavior)

**OK button state model** (canonical — from the semantic visual system HTML):
- **Inactive / pristine**: before required args are supplied — non-interactive; uses disabled styling
- **Active**: once all required args are filled and no ArgErrors — takes the event color (`--event: #30B8E8`), becomes interactive
- **Arg violation active**: field gets dashed border + violated color (`--violated: #F87171`) inline error text; OK stays inactive
- **Cancel**: dismisses dialog, discards all arg values; no transition occurs

**Focus management:** On dialog open, focus moves to the first arg input field. Pressing Escape dismisses the dialog without firing.

---

### 3.2 Arg Form Rendering

Each `ArgDescriptor` entry in `DeclaredArgs` renders as a labeled form field:

| ArgDescriptor property | UI rendering |
|------------------------|--------------|
| `Name` | Field label (title case, camelCase split: "OrderQuantity" → "Order Quantity") |
| `Type` | Displayed as a small type badge next to the label: `number`, `string`, `boolean` |
| `IsOptional` | Required fields: label has a required marker. Optional fields: label has "(optional)" suffix in muted text |
| `SlotIndex` | Controls display order (ascending) |
| `Description` (if present) | Shown as inline help text below the input in muted/small text |

**Input type mapping:**
- `number`, `integer`, `decimal`, `money` → numeric text input (`type="number"` equivalent in webview)
- `string` → text input; `notempty` constraint → required field indicator
- `boolean` → checkbox (tri-state: unchecked, checked, not-set if optional)
- `date`, `datetime` → date picker or text input with date pattern
- `set of T`, `list of T` → tag input

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
- The dialog OK button remains in its previous state (don't flip it to active/inactive until the result arrives)
- If the call takes > 1s, show a small spinner in the outcome preview area with text "Inspecting..."

**After the inspection call returns**, render based on the combination of `OverallProspect` and `ArgErrors`:

---

#### 3.3a: ArgErrors present (Scenario 3)

**Condition:** `ArgErrors.Length > 0`. `OverallProspect` will be `Impossible`.

**Per-field inline errors:** Each `ArgError` maps to its input field via `ArgName`. Below the input field, show:
- Error icon (violated color `--violated: #F87171`, ⚠)
- Error text: `ArgError.Reason` (the prose message from the runtime)

**Dialog OK button:** Inactive. Label: "Cannot fire — fix input errors"

**Outcome preview:** Do not show transition rows. Show a plain summary: "This event cannot be evaluated because of invalid argument values. Fix the errors above."

**Rationale:** Arg errors precede evaluation. Showing a partial outcome alongside arg errors would be misleading — the runtime doesn't evaluate guards or constraints when args are invalid. The feedback surface should stay strictly on the input layer.

---

#### 3.3b: Partial args, no ArgErrors (Scenario 2)

**Condition:** Some required args are not yet filled. `OverallProspect = Possible`. No `ArgErrors`.

**Arg form:** Unfilled required fields show a neutral placeholder (no error styling — the user hasn't submitted yet). Optional unfilled fields show their "(optional)" label.

**Outcome preview: "Partial preview"**

Show the transition row breakdown with rows labeled by their `Prospect`:
- `Certain` rows (if any): highlighted in enabled/satisfied verdict color (`--valid: #34D399`) — "This row will match"
- `Possible` rows: warning color (`--warn: #FDE047`) — "This row might match (depends on missing args)"
- `Impossible` rows: muted/gray — "This row cannot match"

For `Possible` rows, show `PostFields` with `IsResolved = false` fields rendered as `?` in the diff table. Do not hide them — show the row exists but some values are pending. Label them: "[pending — depends on {argName}]"

**Dialog OK button:** Inactive. Label: "Fill in required args to fire". This is non-negotiable — firing with partial args is not allowed from the UI. (The runtime accepts it, but the UI enforces completeness before fire.)

---

#### 3.3c: All args filled, no ArgErrors, OverallProspect = Certain (Scenario 1)

**Condition:** All required args provided, no errors, exactly one row is `Certain`.

**Outcome preview: "Certain outcome"**

The winning transition row is displayed prominently:

- **Row type badge**: if `Effect` is `RowEffect.TransitionTo` → "Transition → [TargetState]" (enabled/satisfied verdict color `--valid: #34D399`, state name in bold mono); if `RowEffect.NoTransition` → "Apply (no state change)"; if `RowEffect.Rejection` → "Reject: [reason]"
- **Field diff table**: Two-column table, "Before" and "After". Fields that change are highlighted (accent in `--valid: #34D399` for after-value). Fields that don't change are shown in muted style.
  - Before values: from `CurrentFields`
  - After values: from the winning row's `PostFields`
  - Fields that don't appear in `PostFields` didn't change — show them at the bottom in a collapsed "Unchanged fields" section
- **Constraint results**: Below the diff table, if `Constraints.Length > 0`, show a "Constraints" section. `Satisfied` constraints: show in enabled/satisfied verdict color (`--valid: #34D399`) with checkmark. `Violated` constraints: show in violated color (`--violated: #F87171`) with the `because` text prominently.

**Dialog OK button:** Active, takes the event color (`--event: #30B8E8`). Label: "Fire [EventName]". The outcome preview shows the certain row's verdict context in `--valid: #34D399`.

---

#### 3.3d: All args filled, OverallProspect = Possible (multiple candidate rows)

**Condition:** Args are complete and valid, but multiple rows could match because guards are ambiguous at the field level, or no single row is `Certain`.

> **Structural constraint:** `OverallProspect = Possible` requires at least one declared arg that contributes to guard evaluation. If an event has no declared args (`DeclaredArgs.Length == 0`), all guard-relevant information comes from current field values and evaluation is deterministic — the outcome is always `Certain` or `Impossible`, never `Possible`. This scenario (§3.3d) therefore only applies to events with `DeclaredArgs.Length > 0`.

**Outcome preview: "Ambiguous outcome"**

Show all non-`Impossible` rows in an ordered list, each with:
- **Row number** (1-indexed, matching first-match routing order)
- **Row prospect badge**: `Certain` (`--valid: #34D399`) or `Possible` (`--warn: #FDE047`)
- **Row type**: if `Effect` is `RowEffect.TransitionTo` → "Transition → [TargetState]"; if `RowEffect.NoTransition` → "Apply"; if `RowEffect.Rejection` → "Reject: [reason]"
- **Guard summary**: A human-readable summary of the guard condition (see OQ-5 for whether the runtime provides this)
- **PostFields diff** (collapsed by default, expandable): same before/after diff as §3.3c

**Which row is "best guess"?** The first non-`Impossible` row in `Transitions` order. This row is shown with a "Best guess" badge and its field diff is shown expanded by default. The rationale: first-match routing means the first non-`Impossible` row is the most likely winner if the ambiguity resolves in the simplest direction.

**If `BestGuessRowIndex` is provided** (see my earlier recommendation): use that index rather than first-non-Impossible heuristic.

**Dialog OK button:** Active, takes the event color (`--event: #30B8E8`). Label: "Fire [EventName] (outcome uncertain)". Fire requires an additional confirmation — a secondary popover saying: "The exact outcome of this event depends on the current field state. The most likely outcome is shown above. Proceed?" with Cancel and Confirm options. The confirmation step uses warning styling (`--warn: #FDE047`) to signal uncertainty.

---

#### 3.3e: Args filled, OverallProspect = Impossible — all rows blocked

**Condition:** Args are valid but every transition row evaluated to `Impossible`. The event is defined for this state but cannot fire.

**Outcome preview: "Blocked"**

This case needs the most diagnostic clarity. Show:

1. **Headline**: "This event cannot fire in the current state." (with ⛔ icon)
2. **Reason breakdown** — for each row in `Transitions`:
   - Row identifier (row number or guard summary)
   - Why it's `Impossible`:
     - If `Effect` is `RowEffect.Rejection`: "This row is a rejection: [reason]"
     - If `Constraints` has `Violated` entries: show the constraint's `because` text
     - If guard failed (no `Violated` constraints, `Effect` is not `RowEffect.Rejection`): "Guard condition not met"
3. The `because` clause from the DSL is always the user-visible explanation. The constraint expression is secondary (collapsible "technical details").

**Dialog OK button:** Inactive. Label: "Cannot fire"

---

### 3.4 Reject Row Display

A row whose `Effect` is `RowEffect.Rejection` is a business prohibition authored in the precept, not a technical error. It must be rendered differently from a constraint violation:

- **Icon**: 🚫 (prohibition, not ⚠ warning)
- **Label**: "This event will be rejected" (not "error" or "failed")
- **Reason text**: `RowEffect.Rejection.Reason` in a prominent, styled block — this is the business language; rendered in warning amber (`--warn: #FDE047`) as a verdict signal. Do not use rule gold (`--rule: #FBBF24`) here — that token belongs to DSL rule construct identity, not to verdict output.
- **Color**: warning/amber (`--warn: #FDE047`) border on the row, NOT the violated red (`--violated: #F87171`). Reject is a legitimate business decision, not a constraint failure. Red is reserved for arg errors and constraint violations.

**Example rendering:**
```
🚫 Will be rejected
"Approval requires verified documents, strong credit, and affordable debt load"
```

This distinction matters for the domain expert persona. They understand business prohibition. They get confused by constraint error UI applied to authoritative business rules.

---

### 3.5 Post-Fire Feedback

When the user fires an event (via the event card for no-args Certain events, or via the dialog OK button for events with args):

**Calling the runtime:** `Version.Fire(eventName, args)` is called. This is a commit — it actually changes the entity state.

**On success (`Transitioned`, `Applied`):**
- The event actions region shows a brief success flash on the fired event card
- The state selector updates to the new state (if `Transitioned`)
- The field values update to the post-fire values
- `InspectUpdate` is called automatically with the new state/fields, refreshing the landscape
- A toast notification: "Fired [EventName] → [new state]" (or "→ same state" for `Applied`)

**On `Rejected`:**
- Show the rejection in-place on the event card: 🚫 "[RowEffect.Rejection.Reason]"
- Do NOT change state or field values (none occurred — the entity is unchanged)
- The toast notification: "Event was rejected — [reason]"

**On `ConstraintsFailed`:**
- This should not happen if inspection showed `Certain` — it indicates a race condition (fields changed between inspect and fire)
- Show an error banner: "Constraint violation occurred — the entity has changed since inspection. Refreshing..."
- Re-run `InspectUpdate` automatically
- Do not re-enable the dialog OK button until the new inspection result arrives

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

**The connection between field editing and event availability must be live.** This is the key interaction that makes the Data Form feel like a true debugging tool, not a static view.

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
- `Satisfied`: ✅ "[because text]" in enabled/satisfied verdict color (`--valid: #34D399`), collapsed by default
- `Violated`: ⚠️ "[because text]" in violated color (`--violated: #F87171`), expanded by default — the field row referenced by the violation gets a dashed border (semantic visual system: dashed border = blocked/violated)
- `Unresolvable`: ⏳ "[because text]" in warning color (`--warn: #FDE047`), with note "Outcome depends on field values"

**The constraint's `because` clause is always the primary text.** The constraint expression is secondary and collapsible under "Show details."

---

### 4.5 Event with a reject row

Covered in §3.4. The key addition here is: when a reject row is the _only_ defined row (making `OverallProspect = Certain` but `Effect` is `RowEffect.Rejection`), the event card shows in State 2 (Blocked) rather than State 4 (Ready — Certain). The user should never be presented with a "click to fire" affordance for an event that will certainly be rejected.

**Wait, is this right?** A `Certain` reject is still an actionable fire — it commits the rejection to the entity. Some UX patterns want this visible. But from a user perspective, a "`Certain → Rejected`" event is not a useful action in most workflows. **This is OQ-1 below.**

---

### 4.6 Field values that affect event availability

Fully covered in §4.2 and the landscape update model in §2.5. The connection is live. Changing `DocumentsVerified` from `false` to `true` on a LoanApplication should immediately:

1. Re-run `InspectUpdate` with the updated fields
2. Shift the `Approve` event from Blocked → Needs Input (or Ready-Certain once all guard conditions are met)
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
3. For events with no required args, `overallProspect` will be `Certain` or `Impossible` — never `Possible`. If all guard-relevant information comes from current field values (no args), evaluation is deterministic.

### 5.2 Understanding Why an Event is Blocked

**Agent pattern:** Call `precept_inspect(text, currentState, data, eventArgs: { "Amount": 5000 })` with candidate args. Read `OverallProspect`. If `Impossible`:

1. Check `argErrors` — if non-empty, fix the args based on each error's `Reason` string
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

### 5.3 ArgError Display for Agents

An agent receiving `ArgErrors` reads the `Reason` string on each error to understand what to fix. The API shape is:

```json
{
  "argErrors": [
    {
      "argName": "Amount",
      "reason": "Expected decimal, got string"
    }
  ]
}
```

The `Reason` is the runtime's prose explanation — the same pattern as `ConstraintViolation.Because` and `InvalidFields.Reason`. Agents use it as guidance text, not as a dispatch key. **No `ArgErrorKind` field exists or will be added** — CC#8 OQ-2 is resolved.

The agent uses `Reason` to understand the failure and adjust its next call accordingly.

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

## 6. Data Edit Workflows — Direct Field Editing

This section documents the user workflows for the **direct edit path**: editing field values without firing an event. The runtime operation is `Version.Update(fields)`. The corresponding inspection operation is `Version.InspectUpdate(fields?)`, which previews the outcome of a patch without committing it.

These workflows are distinct from event firing. No state transition occurs from a successful field edit. The entity stays in its current state; only field values change.

---

### 6.1 The Direct Edit Path: When Events Don't Apply

Not all changes to a precept instance go through events. Some data is managed through direct field editing:

- A CRM contact's phone number is updated when the customer calls with a new one — no lifecycle event, just a field change.
- An adjuster's name is assigned to an insurance claim in `UnderReview` — this is gated by `in UnderReview modify AdjusterName editable`, not by firing an event.
- A `CustomerProfile`'s marketing opt-in flag is toggled — a stateless precept with `writable` fields and no events at all.
- A `FeeSchedule`'s `BaseFee` is adjusted — `writable`, no lifecycle, no event.

**What the DSL author declares for direct editability:**

| DSL declaration | Effect |
|----------------|--------|
| `field X as T writable` | Field is editable in all states (globally editable baseline) |
| `in State modify X editable` | Field is editable only in this specific state |
| `in State modify X editable when Guard` | Field is conditionally editable — only when the guard evaluates to true |
| `in State modify X readonly` | Field is forced read-only in this state, even if globally writable |
| `in State modify all editable` | All non-computed fields are editable in this state |
| `in State modify all readonly` | All fields are locked in this state |
| `in State omit X` | Field is structurally absent in this state — not shown |
| (no declaration, not `writable`) | Field is read-only by default |

Computed fields are never editable, regardless of declarations.

The runtime exposes current editability via `Version.FieldAccess`, which returns `FieldAccessInfo` for each non-omitted field: `Field` (descriptor), `Mode` (Readonly | Editable), and `CurrentValue`. This is a structural query — zero evaluation cost.

---

### 6.2 Field Status Signals in the Data Form

Each field row in the Data Form communicates its current status clearly. The semantic visual system specifies how each channel signals field status:

| Field status | Meaning | Visual signal |
|-------------|---------|---------------|
| **Editable** | User can edit this field in current state | Edit cursor; input affordance (pencil icon on hover) |
| **Readonly** | Locked in current state | Muted label; no input affordance; no cursor change |
| **Conditionally locked** | Field is guarded — locked because a condition isn't met | Muted label + explanation text: "locked — [reason from guard]" |
| **Omitted** | Structurally absent in this state | Not rendered |
| **Under constraint pressure** | A rule or ensure references this field | **Italic field name label** (semantic visual system: italic = constraint pressure on identity label, no other italic use) |
| **Constraint currently violated** | A rule is currently violated and this field is implicated | Dashed border on field row (semantic visual system: dashed border = blocked/violated, exclusive signal); violated color (`--violated: #F87171`) on the reason text |
| **Pending edit — valid** | User has typed a new value, no constraint violation | Edited value shown in pending style (data value color, mono) |
| **Pending edit — violation** | User's pending value would violate a constraint | Dashed border + violated color reason message (per §8.5b) |
| **Constraint satisfied (post-edit)** | Pending edit resolves a violation | Enabled/satisfied verdict indicator |

**Typography rules** (semantic visual system):
- Field names, values, and type annotations: **monospace** throughout
- Field labels (captions, grouping text): sans-serif
- Italic = constraint pressure on the field name label ONLY — no other italic use in the Data Form
- Dashed border = violated/blocked — no other use for dashed borders

---

### 6.3 Inline Editing — Single-Field Pattern

The Data Form uses click-to-edit inline editing for individual field values.

**Resting state:** Field rows show: field name (italic if under constraint pressure), type badge (small, muted), current value (mono). Editable fields show a subtle affordance on hover (pencil icon or focus ring). Readonly fields show no affordance.

**Activated state:** User clicks an editable field row. The value becomes an input control appropriate to the field's type:

| Precept type | Input control |
|-------------|--------------|
| `string`, `text` | Text input |
| `number`, `integer`, `decimal`, `money` | Numeric input (type="number" equivalent) |
| `boolean` | Toggle / checkbox |
| `date`, `datetime`, `instant` | Date picker or ISO-format text input |
| `choice of string(...)` | Dropdown (vocabulary locked to declared choices) |
| `set of T`, `list of T` | Multi-value tag input (pills with ✕) |

Other fields remain visible (no modal, no separate screen). The currently-editing field is visually distinguished (focus ring, input control visible).

**Conditional editability:** Some fields become editable only when a guard condition is met. Example from `InsuranceClaim`: `in UnderReview modify AdjusterName editable when not FraudFlag`. If `FraudFlag = true`, the `AdjusterName` field is locked even in `UnderReview`.

Show this as: "AdjusterName — locked: [guard condition not met]" in muted text. If the user edits `FraudFlag` to `false` (in the same editing session), `AdjusterName` should unlock in real time — the live `InspectUpdate` preview will show the updated editability. **Runtime contract confirmed (OQ-10 closed):** `InspectUpdate` returns updated `FieldAccessInfo` modes reflecting the hypothetical patch, so this real-time unlock is fully supported without a separate API call.

**Escape to cancel:** While a field is in edited state (before commit), pressing Escape restores the previous value and deactivates the field. No commit occurs.

---

### 6.4 Save Semantics — Buffered Atomic Commit

**The core design decision: buffered atomic commit.**

The runtime's `Version.Update(patch)` is atomic — all-or-nothing per call. Buffering multiple field edits and committing them together as one `Update()` call preserves atomic semantics and avoids partial-save failures from cross-field constraints.

**How buffering works:**

1. User edits field A → change is added to the **pending patch buffer** (client-side only, not yet committed).
2. User edits field B → also added to the pending patch buffer.
3. During editing, `Version.InspectUpdate(pendingPatch)` runs continuously (debounced 300ms) to show live constraint feedback and updated event availability.
4. User clicks **Save** (or Tab out of last field, or presses Enter in a single-field form) → `Version.Update(pendingPatch)` is called.
5. On success (`UpdateOutcome.Updated`): the entity advances to the new `Version`. Fields update to committed values. `InspectUpdate()` refreshes the event actions region.
6. On failure: see §6.6.

**Pending state visual signal:** Fields with uncommitted edits show their pending value in a visually distinct state (e.g., a subtle pending marker on the value). This tells the user "this value is not yet saved."

**Save affordance:** A "Save changes" button is enabled when the pending patch is non-empty. A "Discard changes" button is always available while there are pending edits.

**Per-field immediate commit (accelerated path):** For simple single-field edits with no cross-field constraints, Tab-out or Enter can commit immediately (single-field `Update()` call). This is an accelerated path for fields that don't interact with other fields. Recommendation: enable this for `writable` fields on stateless precepts; use buffered save for fields with cross-field rules.

**Field-level discard:** While a field is in edit state (active input), Escape discards just that field's pending change. The buffer retains any other pending edits.

**Record-level discard:** The "Discard changes" button reverts all pending edits. The form returns to the last committed `Version`'s values.

---

### 6.5 Real-Time Constraint Validation Feedback

As the user types, `Version.InspectUpdate(pendingPatch)` runs continuously (debounced 300ms). This returns:
- All constraint results (which rules are satisfied or violated with the pending values)
- Updated `EventInspection` for every event — which events would become available or blocked

**Feedback states during editing:**

#### 6.5a: Pending edit is valid — no violations

All constraints `Satisfied`. No error signals shown. The field value shows the pending value in pending style (not yet committed but valid).

The event actions region updates to show the "after-save" landscape: events that would become available or blocked if this edit were committed. Label these as "After save: [N events change" summary). The user sees forward: "saving this change will unlock Approve."

**Do not show a fire button for the post-edit landscape.** The user hasn't saved yet. Never offer to fire an event against an uncommitted field state.

#### 6.5b: Pending edit violates a constraint

One or more constraints `Violated` from `InspectUpdate`.

**Per-field inline feedback:**
- The field row with the violation gets a **dashed border** (semantic visual system: dashed border = blocked/violated, exclusive signal)
- Below the field, show the violated constraint's `because` text in violated color (`--violated: #F87171`)
- If the constraint references another field, that field's name label becomes italic (constraint pressure signal)

**Form-level summary:**
- A banner above the Save button: "This change would violate [N] rule(s)" with the rule messages listed
- Save button is **disabled** while violations exist. Label: "Resolve rule violations to save"
- Discard button remains enabled

The `because` clause is always the primary text. The constraint expression (the `ExpressionText` from `ConstraintDescriptor`) is secondary and collapsible ("Show constraint details").

#### 6.5c: Pending edit satisfies a previously-violated constraint

When the entity's current state has a violated constraint, and the user's pending edit would satisfy it:

- Show a brief positive signal on the constraint: ✅ "[because text] — would be satisfied after save"
- This acknowledges that the user is fixing something, not just changing a value

#### 6.5d: Pending edit changes event availability

When the pending edit would change which events are available (the most important feedback scenario):

- Update the event actions region with an "if saved" indicator
- Events that would become newly available: show with an "Unlocks after save" annotation and a fade-in animation preview (but no fire button)
- Events that would become blocked: show with "Becomes blocked after save" annotation
- If `DocumentsVerified` going from `false` to `true` would unlock `Approve`: the `Approve` card changes from Blocked to Possible/Certain with an "After save" label

**Example flow:**
1. `LoanApplication` is in `UnderReview`. `DocumentsVerified = false`. `Approve` event card shows ⛔ Blocked.
2. User edits `DocumentsVerified` → `true` in the fields region.
3. `InspectUpdate({DocumentsVerified: true})` runs. `Approve` event now shows `OverallProspect = Possible`.
4. `Approve` event card shows: "Will unlock after save" with the new prospect badge.
5. User saves. `Version.Update({DocumentsVerified: true})` commits. `Approve` card transitions to Possible (with animation).

---

### 6.6 What Happens When a Field Edit Is Rejected (Post-Save)

When the user commits via Save, `Version.Update(patch)` runs. Possible outcomes:

#### `UpdateOutcome.Updated`
Commit succeeded. Update displayed field values to the new `Version`. Trigger a full `InspectUpdate()` to refresh the event actions region. Show a brief, low-noise success signal (not a toast — field saves are frequent).

#### `UpdateOutcome.ConstraintsFailed`
One or more constraints violated. The patch was rejected atomically — no field values changed.

Show the violations in the same visual pattern as the live preview (§6.5b). Additionally:
- Show a banner: "Save failed — [N] rule(s) violated"
- Keep the pending values in the form so the user can see what they tried to save
- The user can fix the violations or discard

The `ConstraintViolation.Because` text is the primary explanation. `FailingSubexpression` and `FailingValue` are in a collapsible "Show details" section.

#### `UpdateOutcome.FieldNotEditable`
A field in the patch is not editable in the current state. This should not happen if the UI correctly reflects `FieldAccess` — it indicates a race condition (state changed between inspection and commit).

Show a banner: "One or more fields are no longer editable — the entity state may have changed. Refreshing..." Then re-call `InspectUpdate()` and refresh field access states.

#### `UpdateOutcome.InvalidFields`
A field name was invalid or a value had the wrong type. This is a UI programming error. Log it and show a generic "Something went wrong — please try again." Do not expose the internal error details.

---

### 6.7 State-Dependent Editability — Key Scenarios

**Scenario: Fields unlock when entering a new state (post-fire)**

After firing an event that transitions to a new state, fields that were locked may become editable. Example: `InsuranceClaim` transitions to `UnderReview` via `AssignAdjuster`. The `FraudFlag` field becomes editable (per `in UnderReview modify FraudFlag editable`).

When the state changes (post-fire), refresh `Version.FieldAccess`. Fields that newly become editable should animate into their editable state. Fields that become locked should animate to the locked state. This visual transition signals the state change's effect on data governance.

**Scenario: Conditional editability unlocking during a session**

`in UnderReview modify AdjusterName editable when not FraudFlag`. If `FraudFlag` changes from `true` to `false` (by user edit), `AdjusterName` unlocks in real time — the `InspectUpdate` preview reflects this before the `FraudFlag` edit is even committed. The form updates immediately.

**Scenario: All fields locked in a terminal state**

Terminal states (like `Paid`, `Declined`, `Funded`) typically have no outgoing events and locked fields. The Data Form shows all fields as readonly with the state label clearly visible. The event actions region may show only Blocked or Unavailable cards, or may be empty if no events are defined for this state (undefined events are not rendered).

**Scenario: Stateless precept (no lifecycle)**

For stateless precepts (`CustomerProfile`, `FeeSchedule`, `PaymentMethod`), there are no states. Editability is purely `writable` vs. non-`writable`, with global rules as constraints. The state selector is hidden. The Data Form shows all fields; `writable` fields have edit affordances, others do not.

---

### 6.8 The Critical Distinction: Edit → Save → No Transition vs. Edit → Constraint Satisfied → Event Unlocked

This is the most important workflow relationship in the product. Get this right.

**Scenario A: Edit and save, nothing else changes**

User edits `Nickname` on `PaymentMethod` (`writable` field). Saves. `Nickname` changes. No state transition. No event impact (no events on this precept). Simple data maintenance. The entity is valid; the rule `MarketingOptIn == false or Email is set` is unaffected.

**Scenario B: Edit satisfies a rule that was previously violated**

User adds an email address to `CustomerProfile` where `MarketingOptIn = true`. The rule `MarketingOptIn == false or Email is set` was violated. After the edit (and save), the rule is satisfied. The violated rule indicator disappears. The entity is now in a valid configuration.

**Scenario C: Edit unlocks a guarded event**

User sets `DocumentsVerified = true` on `LoanApplication` in `UnderReview`. The `Approve` event guard includes `DocumentsVerified`. Before the edit: `Approve` is Blocked. After the edit: `Approve` may become Possible or Certain.

- **Live preview**: `InspectUpdate({DocumentsVerified: true}).Events` shows `Approve` with `OverallProspect = Possible`. The event card shows "Will unlock after save."
- **After commit**: `Version.Update({DocumentsVerified: true})` succeeds. `Approve` card transitions to Possible with a fade animation.

**Scenario D: Edit changes what event ensures will evaluate**

Some constraints are `on<event>` ensures. Editing a field value doesn't directly trigger these ensures (they evaluate at Fire time). But the edit may change whether the event guard passes, which determines whether the event is available to fire at all. The user sees this through the event prospect changing (§6.5d).

**The UX principle:** Always show the full forward path. When an edit unlocks an event, surface that explicitly. The user should never have to manually notice that the Event Timeline changed — the Data Form should tell them.

---

### 6.9 Undo and Redo

**Within a session, the user can undo/redo both field edits and fired events.**

**What the runtime enables:** `Version` is an immutable snapshot. Every operation (`Update`, `Fire`) returns a new `Version`. The UI can maintain a stack of previous `Version` instances as an undo history.

**Undo scope:**
- Pending field edits (before save): Undo reverts the pending patch buffer to the previous value(s). No runtime call needed.
- Committed field saves (`Update()` succeeded): Undo reverts to the pre-update `Version`. The UI calls no API — it simply presents the previous snapshot.
- Fired events (`Fire()` succeeded): Undo reverts to the pre-fire `Version`. This is significant: if the user fires `Submit` accidentally and the entity transitions to `UnderReview`, they can undo back to `Draft`. Surface this as a visible "Undo last action" affordance.

**What undo cannot do:**
- Undo cannot reverse operations that have been persisted by the host application (outside the preview panel). The preview panel's undo stack is session-scoped — it exists only in the webview session.
- There is no multi-user collaborative undo. Each user's session has its own undo stack.

**Undo stack limit:** Retain the last 10 `Version` snapshots. Older history is dropped. Show the user what can be undone: "Undo: [event name] or [field name change]."

**Keyboard shortcuts:** Ctrl+Z / Ctrl+Y (Cmd+Z / Cmd+Y on Mac), active within the webview context. Do not rely on VS Code's native undo/redo — the webview must intercept these for its own history stack.

---

## 7. Derived UX Requirements

The following requirements are implementation-ready. Each maps to the `EventInspection` runtime contract. Priority is from the user's perspective.

---

```
UXR-1: The event actions region shall display all events defined in the current state,
        grouped into exactly four visual states: Unavailable, Blocked, Needs Input,
        Ready–Certain. Events not defined for the current state are not rendered —
        no grayed-out placeholder, no entry of any kind (undefined events are absent
        from the Event Timeline entirely).
Source: §2.2 — Event landscape visual states
Priority: Must Have
Depends on: EventInspection.OverallProspect, EventInspection.Transitions (count),
            EventInspection.DeclaredArgs (count)
```

```
UXR-2: Each event card in the landscape view shall display the event name, a prospect
        indicator (icon + label), an arg count badge when DeclaredArgs.Length > 0, and
        a target state hint when OverallProspect = Certain and Effect is RowEffect.TransitionTo.
Source: §2.3 — Event card contents
Priority: Must Have
Depends on: EventInspection.EventName, EventInspection.OverallProspect,
            EventInspection.DeclaredArgs, TransitionInspection.Effect
```

```
UXR-3: The Event Timeline shall update automatically when the user changes the selected
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
            TransitionInspection.Effect, TransitionInspection.PostFields
```

```
UXR-5: Clicking a Needs Input event card shall open an arg input dialog. Focus shall
        move to the first arg input field on dialog open. Pressing Escape shall
        dismiss the dialog without firing. The dialog header shall display the event
        name (event color, `--event: #30B8E8`) and, where deterministic, the target
        state (state color, `--state: #A898F5`) at the top right. A Cancel button in
        the dialog footer dismisses and discards all arg values.
Source: §3.1 — Selection and dialog
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
        show an error icon and the ArgError.Reason text (the prose message from the
        runtime) in violated color (--violated: #F87171). No kind-specific hint or
        ArgErrorKind branching. Pattern mirrors field edit constraint violation display.
        The dialog OK button shall be inactive with label "Cannot fire — fix input errors".
        No transition row output shall be shown.
Source: §3.3a — ArgErrors present
Priority: Must Have
Depends on: ArgError.ArgName, ArgError.Reason, EventInspection.ArgErrors
```

```
UXR-10: When args are partially filled (some required args absent, no ArgErrors), the
         outcome preview shall show the transition row breakdown with each row's Prospect
         indicator. PostFields entries with IsResolved = false shall render as "?" in the
         diff table with a tooltip "[pending — depends on {argName}]". The dialog OK button
         shall be inactive with label "Fill in required args to fire."
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
         be shown in a collapsed "Unchanged fields" section. The dialog OK button shall be
         active (event color, `--event: #30B8E8`) with label "Fire [EventName]."
Source: §3.3c — Certain outcome
Priority: Must Have
Depends on: EventInspection.CurrentFields, TransitionInspection.PostFields,
            TransitionInspection.Effect,
            EventInspection.OverallProspect (= Certain)
```

```
UXR-12: When OverallProspect = Possible (args complete, no errors), the
         outcome preview shall show all non-Impossible transition rows in document order
         with their row number, Prospect badge, type, and a collapsed field diff. The
         first non-Impossible row (or BestGuessRowIndex if provided) shall be labeled
         "Best guess" and shown expanded by default. The dialog OK button shall be active
         (event color, `--event: #30B8E8`) with label "Fire [EventName] (outcome uncertain)". Firing shall require
         a secondary confirmation with text explaining the uncertainty; the confirmation step uses warning styling (`--warn: #FDE047`).
Source: §3.3d — Possible outcome, §3.3 fire confirmation
Priority: Must Have
Depends on: TransitionInspection.Prospect, TransitionInspection.PostFields,
            EventInspection.OverallProspect (= Possible), BestGuessRowIndex (Should Have)
```

```
UXR-13: When OverallProspect = Impossible, no ArgErrors, the outcome preview shall show
         a "Cannot fire" headline, followed by a per-row explanation: Reject rows show
         "Will be rejected: [Reason]" (from RowEffect.Rejection.Reason); rows with Violated constraints show the
         constraint's because text; rows whose guard failed (no violated constraints, no
         reject) show "Guard condition not met." The dialog OK button shall be inactive with
         label "Cannot fire."
Source: §3.3e — Impossible outcome, §4.5 reject row
Priority: Must Have
Depends on: TransitionInspection.Prospect (= Impossible), TransitionInspection.Effect,
            ConstraintResult.Status,
            ConstraintDescriptor.Because
```

```
UXR-14: A transition row whose Effect is RowEffect.Rejection shall be rendered with a 🚫
         prohibition icon and warning (`--warn: #FDE047`) styling, distinct from constraint violations
         (`--violated: #F87171`) and arg errors (`--violated: #F87171`). The RowEffect.Rejection.Reason text shall be the primary displayed
         content, rendered in a styled block. The word "error" shall not be used for
         reject rows — the label shall be "Will be rejected" or "Rejected."
Source: §3.4 — Reject row display
Priority: Must Have
Depends on: TransitionInspection.Effect (= RowEffect.Rejection)
```

```
UXR-15: EventEnsures constraints and per-row ensures shall be rendered in an "Event
         constraints" section below the field diff table. Satisfied constraints shall
         show a checkmark and the because text in enabled/satisfied verdict color (`--valid: #34D399`), collapsed by default.
         Violated constraints shall show a warning icon and the because text in violated color (`--violated: #F87171`),
         expanded by default. Unresolvable constraints shall show a warning clock icon
         in warning color (`--warn: #FDE047`) with "Outcome depends on field values." The constraint expression shall be in
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
         shall update to reflect the post-fire result, the Event Timeline shall
         automatically refresh via InspectUpdate, and a toast notification shall
         appear with the fired event name and new state. The fired event card shall
         briefly flash green before the landscape re-renders.
Source: §3.5 — Post-fire feedback
Priority: Must Have
Depends on: EventOutcome.Transitioned.Result, EventOutcome.Applied.Result
```

```
UXR-18: When a fire call returns EventOutcome.ConstraintsFailed or EventOutcome.Unmatched (race condition between
         inspect and fire), an error banner shall appear explaining that the entity state
         changed since inspection, and an automatic InspectUpdate refresh shall be
         triggered. The dialog OK button shall not be re-enabled until the refresh completes.
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
         priority: Certain first, then Needs Input, then Blocked, then
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
UXR-25: The MCP precept_inspect response shall include Effect on TransitionInspection
         entries. AI agent consumers shall be able to
         distinguish a business rejection (Effect is RowEffect.Rejection) from a guard failure
         (Effect is RowEffect.TransitionTo or RowEffect.NoTransition, constraints empty) from a
         constraint violation (constraints with Violated entries) without additional API calls.
Source: §5.2 — Agent debugging pattern
Priority: Must Have
Depends on: TransitionInspection.Effect,
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
UXR-27: When the user changes a field value in the field panel, the Event Timeline shall
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
         Effect is RowEffect.Rejection) shall be rendered as Blocked (State 2: ⛔ "Cannot fire"),
         not as Ready–Certain. No direct-click fire affordance shall be shown on the card.
         Opening the event (via the dialog) shall show the reject row and its RowEffect.Rejection.Reason,
         and the dialog OK button shall be active to allow deliberate commit of the rejection.
         Certain-reject → Blocked (OQ-1 closed).
Source: §4.5 — Certain-reject edge case
Priority: Must Have
Depends on: TransitionInspection.Effect (= RowEffect.Rejection),
            EventInspection.OverallProspect (= Certain)
```

```
UXR-29: The fire action shall never be the only affordance for understanding an event's
         outcome. Before any fire action is available, the user shall have had access to
         the outcome preview. Single-click fire (State 4 Ready–Certain) satisfies this
         requirement only because the hover tooltip provides a preview before the click.
         For dialog-based fire (events with args), the outcome preview in the dialog body
         always precedes the dialog OK button becoming active.
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

```
UXR-31: The Data Form shall display all non-omitted fields for the current entity state,
         with `FieldAccessInfo.Mode` determining whether each field row is rendered as
         editable (input affordance on hover) or read-only (no affordance, muted label).
         Omitted fields shall not be rendered.
Source: §6.2 — Field Status Signals
Priority: Must Have
Depends on: Version.FieldAccess (returns FieldAccessInfo per non-omitted field)
```

```
UXR-32: Field names in the Data Form shall use monospace typography. Field names that are
         referenced by at least one active rule or ensure constraint shall be rendered
         in italic. No other italic typography shall appear in the Data Form.
Source: §6.2 — constraint pressure signal; semantic visual system typography rules
Priority: Must Have
Depends on: ConstraintDescriptor.RelevantFields (to identify constrained fields)
```

```
UXR-33: When a field has an uncommitted pending edit, the Data Form shall display the
         pending value in a visually distinct state (pending marker on the value cell).
         A "Save changes" button shall become enabled. A "Discard changes" button shall
         remain always available while any pending edits exist.
Source: §6.4 — buffered atomic commit, pending state visual signal
Priority: Must Have
Depends on: Client-side pending patch buffer
```

```
UXR-34: While the user is editing fields (pending patch buffer is non-empty), the Data Form
         shall call InspectUpdate(pendingPatch) debounced at 300ms after the last edit.
         The result shall update: (a) constraint violation signals on field rows,
         (b) the event actions region with an "After save" preview (no fire buttons
         while edits are pending).
Source: §6.5 — real-time constraint validation feedback
Priority: Must Have
Depends on: Version.InspectUpdate(patch)
```

```
UXR-35: When InspectUpdate(pendingPatch) returns one or more ConstraintResult results,
         each implicated field row shall display a dashed border and the violated
         constraint's "because" text in violated color (--violated: #F87171).
         A form-level banner shall summarize "This change would violate [N] rule(s)."
         The Save button shall be disabled until all violations are resolved or discarded.
Source: §6.5b — pending edit violates a constraint
Priority: Must Have
Depends on: Version.InspectUpdate(patch).Constraints, ConstraintResult.Constraint.Because
```

```
UXR-36: When the user clicks "Save" and Update() returns UpdateOutcome.Updated, the
         Data Form shall commit all pending edits, refresh FieldAccess, and re-call
         InspectUpdate() to update the event actions region. A low-noise success signal
         (not a toast) shall briefly acknowledge the successful save.
Source: §6.6 — UpdateOutcome.Updated
Priority: Must Have
Depends on: Version.Update(patch), UpdateOutcome.Updated
```

```
UXR-37: When Update() returns UpdateOutcome.ConstraintsFailed, the Data Form shall
         NOT clear the pending edits. The constraint violations from the response shall
         be shown on the relevant field rows (dashed border, --violated: #F87171 text).
         A banner shall read: "Save failed — [N] rule(s) violated."
         The constraint's "because" text is the primary explanation; FailingSubexpression
         and FailingValue appear in a collapsible "Show details" section.
Source: §6.6 — UpdateOutcome.ConstraintsFailed
Priority: Must Have
Depends on: Version.Update(patch), UpdateOutcome.ConstraintsFailed.Violations
```

```
UXR-38: When Update() returns UpdateOutcome.FieldNotEditable (race condition: field is
         no longer editable in current state), the Data Form shall show a banner:
         "One or more fields are no longer editable — the entity state may have changed.
         Refreshing..." and immediately re-call InspectUpdate() to restore correct state.
Source: §6.6 — UpdateOutcome.FieldNotEditable
Priority: Must Have
Depends on: Version.Update(patch), UpdateOutcome.FieldNotEditable
```

---

## 8. Open Questions for Shane

These are design decisions I cannot resolve from first principles. I've named the tradeoff and my lean.

---

**OQ-1: Should a Certain-reject event be rendered as Blocked or as Ready?** *(✅ Closed — visual system spec)*

A reject row is a legitimate business operation — it changes the entity (to a rejected state in some precepts, or records the rejection reason). An event with one `Certain` reject row is unambiguously deterministic: fire it and you get a rejection. This is different from an event that _might_ reject.

- **Option A (my lean): Render as Blocked.** The UX goal of "directly clickable" implies "this fires and does something useful." A certain rejection is a useful business operation in some contexts (e.g., explicitly declining a claim) but it's never a frictionless path forward. Treating it as Blocked ensures the user sees the rejection message before committing. Fire is still possible (via expansion), just not single-click.
- **Option B:** Render as Ready–Certain, dialog OK button active, with the reject outcome clearly shown in the outcome preview. This respects the user's intent if they're deliberately firing a reject event. The rejection outcome is fully visible before they fire.

**My lean:** Option A for the Event Timeline card (never show a ▶ direct-click affordance for a certain-reject). But Option B for the dialog OK button — once the user has opened the event and seen the rejection details in the dialog body, the OK button should be active. The friction is knowing, not preventing.

**Ruling:** Option A. The semantic visual system manifest explicitly normalizes rejection outcomes as Blocked (§ Event Semantics, lines 143–144). A certain-reject event card renders as Blocked with the rejection reason visible. The dialog OK button is active once the user opens the dialog — friction is knowing, not preventing.

---

**OQ-2: Should "outcome depends on field values" be shown differently from "outcome depends on missing args"?** *(✅ Closed — superseded by structural ruling)*

**Ruling (supersedes prior decision):** The scenario this OQ was designed to distinguish — `DeclaredArgs.Length == 0` AND `OverallProspect = Possible` — is **structurally impossible in Precept**. If an event has no declared args, all guard-relevant information comes from current field values, making evaluation deterministic. `OverallProspect = Possible` requires at least one declared arg that affects guard evaluation. Therefore the labeling distinction between "outcome depends on field values" and "outcome depends on missing args" does not arise: no-arg events can only be `Certain` or `Impossible`, never `Possible`. The prior heuristic decision is moot.

---

**OQ-3: How are `set of T` and `list of T` args rendered in the arg form?** *(✅ Closed — tag input)*

**Ruling:** Tag input (pills with ✕ to remove) for collection-type args in V1. This is not deferred. Kramer implements pill-based tag input for `set of T` and `list of T` arg types.

---

**OQ-4: Should the inspect panel show the field diff as a table or as a list of changed fields only?** *(✅ Closed — delta view decision)*

Two options for the field diff display:
- **Full table (before/after all fields):** Shows every field, changed highlighted. Gives complete picture but can be overwhelming for entities with 15+ fields.
- **Changed fields only (delta view):** Shows only fields that differ between `CurrentFields` and `PostFields`. Compact, but the user has to scroll elsewhere to see the full post-state.

**Decision:** Delta view (changed fields only) is the default. Unchanged fields go in a collapsed "Unchanged fields (N)" section at the bottom of the diff. A "Expand all" toggle shows the full table. The delta is almost always what the user cares about; the collapsed section satisfies completeness without overwhelming the view. No separate "Full state" tab needed.

---

**OQ-5: Should the UI attempt to render guard conditions in human-readable form?** *(✅ Closed — runtime provides guard summary)*

In §4.3, I noted that showing "which guard drove this row's Impossible" helps the user understand why one row wins over another. The runtime `TransitionInspection` doesn't carry a parsed guard summary — it carries only `Prospect` and `Constraints`.

**Ruling:** The runtime will provide a guard summary string on `TransitionInspection` (e.g., `GuardSummary: string?`). When present, the UI renders it directly. This eliminates the need for DSL parsing or a generic fallback. The event-inspection-proposal.md must be updated to add this field. The "go to definition" link question is deferred until the summary field is implemented — source span may follow in a future iteration.

---

**OQ-6: Does the event surface need a "what does this event do" description?** *(✅ Closed — V1: event name only)*

The `ArgDescriptor.Description` field (which I recommended adding in my earlier review) would let the precept author annotate event args with help text. But there's no equivalent for the event itself — no `EventDescriptor.Description`.

The developer persona probably doesn't need this (they wrote the DSL). The domain expert persona would benefit enormously ("Submit — starts the loan application review process").

**Ruling:** V1 renders the event name as the sole label. No authored description, no auto-generated transition summary, no hover tooltip text beyond the name. V2 may add a `description` annotation to the Precept language itself, surfaced as event card copy. Deferred.

---

**OQ-7: Should the arg form persist across event selections?** *(✅ Closed — session-scoped persistence decision)*

If the user opens the arg form for `Approve`, types values, then clicks away to look at another event, should the `Approve` arg values be preserved when they come back?

**Decision:** Persist within a session (until the precept recompiles or the state changes). Losing typed values mid-investigation is frustrating, especially when the user is comparing what happens with different arg values across multiple events.

**Session scope:** Per-document-session in the VS Code extension. Precept source file recompilation clears all preserved arg forms (new entity definition = new arg contracts). State change clears all forms. Field value change does NOT clear forms (the user may be testing whether changing a field unblocks an event that already has args ready).

---

**OQ-8: Should the Data Form use buffered commit or per-field immediate commit?**

As documented in §6.4, the recommendation is buffered atomic commit. But per-field immediate commit (Tab-out or Enter commits a single field) is more ergonomic for simple cases and is appropriate when there are no cross-field constraints.

**The question:** Should the commit mode be determined by the presence of cross-field constraints (smart/adaptive), or should there be a single consistent pattern? Smart mode is better UX but requires the UI to detect whether a field has cross-field rule involvement at author time — this may be possible from the `ConstraintDescriptor.RelevantFields` if the UI can precompute it. Single consistent pattern is simpler to reason about.

**My lean:** Per-field commit for `writable` fields on stateless precepts (no events, simple maintenance UX). Buffered commit for state-scoped editable fields and any field participating in a multi-field constraint. Shane needs to call whether the runtime makes this distinction queryable.

---

**OQ-9: Should uncommitted pending field edits be reflected in the Event Timeline preview?**

§6.5d specifies that `InspectUpdate(pendingPatch)` drives the Event Timeline while edits are pending. This means the event cards reflect a hypothetical state that has not been committed. The user sees "Approve will unlock after saving."

**The concern:** Showing a hypothetical Event Timeline before save could confuse users who try to fire the event before saving. The doc (§6.5a) addresses this by prohibiting fire buttons on the post-edit landscape preview.

**The question:** Is the live Event Timeline preview (updated while edits are pending) worth the complexity and potential confusion? Or should the event actions region only update after a successful `Update()` call?

**My lean:** Yes, show the live preview — this is the product's inspectability promise. The constraint is that fire buttons are disabled while there are uncommitted edits. This is clear and not confusing. But Shane should confirm this is the right UX direction before we spec the animation details.

---

**OQ-10: Does `InspectUpdate` return updated `FieldAccess` modes for conditionally-editable fields?** *(✅ Closed — runtime contract confirmed)*

**Shane's ruling:** `InspectUpdate` MUST return updated `FieldAccessInfo` modes reflecting the hypothetical field patch. When `Version.InspectUpdate({FraudFlag: false})` is called, the response must include updated `FieldAccessInfo` for `AdjusterName` showing `Mode = Editable` (reflecting the guarded editability condition `editable when not FraudFlag`).

**UX impact:** §6.3 (real-time conditional field unlock) is **unblocked**. The conditional editability scenario described there — where `AdjusterName` unlocks in real time as the user edits `FraudFlag` — is fully supported by the runtime contract. No new API surface is required.

---

*Document complete. Ready for Shane's calls on OQ-8, OQ-9. OQ-1, OQ-2, OQ-3, OQ-4, OQ-5, OQ-6, OQ-7, OQ-10 closed above.*

---

## ⚠️ Design Conflicts for Shane

The following conflicts were identified during the visual system reconciliation pass (this session). Each needs a deliberate call before final implementation spec is written.

---

### Conflict A: Surface Priority vs. Peer Surfaces Rule *(✅ Resolved — Elaine-29)*

**Was in this document (§2.1):**
> "If space is tight, the Event Timeline is the priority surface."

**In the semantic visual system (locked):**
> "The three runtime surfaces are peer surfaces. No surface among Diagram, Data Form, and Timeline is primary over the others."

**Resolution (Shane ruling, Elaine-29):** §2.1 has been updated to reflect the peer-surfaces rule. The Data Form and Event Timeline are peers — neither is architecturally primary. If a layout constraint forces a choice between surfaces, the Data Form takes priority over the Event Timeline. "Events first" framing removed.

---

### Conflict B: "Event landscape" terminology vs. "Event Timeline" surface name

**In this document:** "Event landscape" was used to refer to the list of available events in the current state (a component of the Data Form surface).

**In the semantic visual system:** "Event Timeline" is one of the four canonical surfaces — it refers to the historical record of past fired events.

These are different things, but the naming overlap is dangerous. "Event landscape" sounds like a variation of "Event Timeline." A reader unfamiliar with the distinction could conflate the two.

**Resolution applied in this doc:** The event actions region within the Data Form is now called "event actions region" in §2.1, §3, and §6. The four surfaces remain Code · State Diagram · Data Form · Event Timeline. No overlap.

**Request:** Shane to confirm this rename is complete and no other documentation refers to an "Event Timeline" panel as if it were a standalone surface.

---

### Conflict C: Runtime gap — does `InspectUpdate` return updated `FieldAccess` modes? *(✅ Resolved — Elaine-29)*

**See OQ-10 above (closed).** Shane has confirmed: `InspectUpdate` MUST return updated `FieldAccessInfo` modes reflecting the hypothetical field patch. The UX requirement in §6.3 (real-time conditional field unlock) is fully supported by the runtime contract. No new API surface is required.
