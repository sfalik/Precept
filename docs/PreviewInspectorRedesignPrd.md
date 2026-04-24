# Preview / Inspector Redesign PRD

> **Authority boundary:** This file lives in `docs/`, the repository's legacy/current reference set. Use it for the implemented v1 surface, current product reference, or historical context. If you are designing or implementing `src/Precept.Next` / the v2 clean-room pipeline, start in [docs.next/README.md](../docs.next/README.md) instead.

**Author:** Copilot  
**Date:** 2026-04-05  
**Project board:** `Precept Preview Panel Redesign`  
**Primary inputs:** `tools/Precept.VsCode/webview/inspector-preview.html`, `tools/Precept.VsCode/src/extension.ts`, `brand/inspector-panel-review.md`, `brand/brand-spec.html`, `docs/archive/InteractiveInspectorMockup.md`

---

## 1. Product Summary

Redesign the Precept preview / inspector panel as a single, coherent developer surface inside the VS Code extension. The redesign should preserve the strong interaction model already implemented today while bringing the surface into alignment with brand guidance, clarifying information hierarchy, and strengthening the AI-first contract.

This PRD treats the **preview panel** and **inspector panel** as one product surface:

- the **preview** is the diagram and event execution experience
- the **inspector** is the state/data/rule visibility and editing experience

They should be designed together rather than as separate features.

---

## 2. Why This Work Exists

The current implementation is already feature-rich and functionally useful, but it has drifted away from the design inputs that describe it:

1. The shipped webview is more capable than the existing inspector review document describes.
2. The visual treatment still uses a custom palette and typography that diverge from the brand system.
3. Some key product decisions are still implicit in code rather than explicit in requirements, especially around current-state visibility, AI-readable output, and the relationship between diagram, events, and data editing.

The goal of this PRD is to turn the current implementation plus historical mockup intent into an explicit, current source of truth for redesign work.

---

## 3. Current State Summary

The shipped surface is implemented primarily in `tools/Precept.VsCode/webview/inspector-preview.html` and coordinated by `tools/Precept.VsCode/src/extension.ts`.

### What exists today

- Header with title, source file name, preview mode indicator, and Edit / Save / Cancel / Reset actions
- SVG state diagram with animated transition feedback
- In-canvas data lane showing field/value pairs
- Vertical current-state event dock with inline arguments, live status, and keyboard execution
- Edit mode with live draft validation
- Rule violation banner, draft validation banner, state-rules indicator, and field-level rule affordances
- Follow-active-editor vs locked-preview behavior

### What is working well

- Event execution and inspection are tightly integrated
- Edit mode is useful and immediate rather than modal and opaque
- The diagram and event dock communicate runtime status clearly
- The surface is already meaningfully AI-friendly because its structure is consistent and data-rich

### Main current gaps

- Brand color system is not applied
- Typography does not match the visual surface spec
- Field types are not surfaced
- Current state is visually clear in the diagram but not explicitly restated in text
- AI-readable export is implied by DOM structure rather than provided as a first-class contract

---

## 4. Goals

### Primary goals

1. **Align the surface with brand guidance** for color, typography, and semantic consistency.
2. **Preserve and refine the current interaction model** rather than replacing it with a simpler but less capable mockup.
3. **Make state, data, rules, and event outcomes easier to scan** during live debugging.
4. **Define an explicit AI-first contract** for inspector state and event/result data.
5. **Document a stable redesign scope** that can be tracked on the project board and implemented incrementally.

### Non-goals

1. Replacing the current preview architecture or message protocol from scratch
2. Reworking the runtime or MCP model for this effort
3. Building save/load instance workflows as part of this redesign
4. Splitting preview and inspector into separate VS Code surfaces

---

## 5. Users and Core Jobs

### Primary user

A developer authoring or debugging a `.precept` file inside VS Code.

### Core jobs

1. Understand the current state and what transitions are available
2. Inspect current data and active rule pressure
3. Try events with arguments and immediately understand why they succeed, reject, or block
4. Edit allowed fields safely and see validation feedback before committing changes
5. Understand the machine without constantly mapping between editor, runtime errors, and diagram output

### Secondary user

An AI agent or Copilot workflow consuming the preview surface as structured product state.

### Secondary jobs

1. Read current state, fields, editable fields, and violations without brittle DOM scraping
2. Understand next-action affordances and event outcomes programmatically
3. Use the preview surface as a trustworthy contract rather than a purely visual artifact

---

## 6. Product Requirements

### 6.1 Surface framing

- The product shall continue to present the preview/inspector as a **single integrated panel**.
- The panel shall preserve the current three-part structure:
  - header
  - diagram plus data lane
  - event dock
- The redesign shall clarify this hierarchy visually instead of redefining it structurally.

### 6.2 Current-state visibility

- The current state must be unambiguous even when the diagram is visually dense.
- The redesign shall surface current state in **both**:
  - the diagram
  - a textual label in the chrome or metadata area

Rationale: the current implementation relies primarily on diagram emphasis, which is strong visually but weak for quick scanning, screenshots, and AI extraction.

### 6.3 Brand system alignment

- The redesign shall adopt the semantic color vocabulary defined in `brand/brand-spec.html § 2.3 Inspector Panel`.
- State names, event names, values, warnings, enabled states, and blocked states shall use colors that mean the same thing across editor, diagram, and inspector surfaces.
- The redesign shall move field names and values away from the current white / blue-gray treatment to the brand slate system.
- Constraint explanation text shall use the brand-approved explanation/warning treatment rather than sharing the same red as blocked controls.

### 6.4 Typography

- Field names and values shall use the typography guidance from `brand/brand-spec.html § 2.3 Inspector Panel`.
- Code-like identifiers must read as identifiers, not generic app chrome.
- UI chrome may use a different face if needed, but the information-bearing data surface must visually align with the rest of the Precept authoring experience.

### 6.5 Data lane clarity

- The inspector shall continue to show field name/value pairs in a scan-friendly format.
- The redesign shall add **field type visibility** as secondary metadata.
- The redesign shall preserve support for:
  - rule violation banner
  - draft validation banner
  - state-rules indicator
  - field-level rule affordances
  - editable vs read-only distinction
  - nullable-field controls
- Validation and rule messaging shall remain near the affected data instead of being pushed into a detached toast-only model.

### 6.6 Event dock clarity

- The event dock shall remain the primary interaction surface for runtime actions.
- The redesign shall preserve:
  - inline argument entry
  - live event status updates
  - inline reason text for blocked/undefined events
  - keyboard navigation
  - transient execution feedback
- Event affordances may be visually restyled, but the redesign must not lose the current density of interaction or the immediate inspect/fire loop.

### 6.7 Diagram behavior

- The diagram shall remain read-first and execution-aware.
- Transition emphasis, evaluated outcomes, and successful-fire animation shall remain part of the surface.
- The redesign may simplify visual styling, but it shall not regress the ability to understand:
  - current state
  - possible destinations
  - active path during execution
  - blocked vs enabled behavior

### 6.8 Accessibility

- Color must not be the only signal for success, blocked, warning, or violation states.
- The redesign shall preserve or strengthen redundant signaling through text, glyphs, borders, or icons.
- Keyboard navigation through the event dock must remain supported.
- Data presentation shall remain screen-reader-friendly even if the underlying structure changes.

### 6.9 AI-first contract

- The redesign shall define a structured inspector-state contract that can be consumed without DOM parsing.
- At minimum, the contract shall expose:
  - current state
  - current fields and values
  - field types
  - editable fields
  - active violations
  - current event statuses
- The preview host/webview contract shall expose this data through a stable structured interface rather than requiring HTML scraping.

### 6.10 Technical continuity

- The redesign shall build on the current extension-host / webview architecture.
- Existing follow-active-editor and locked-preview behaviors shall remain supported.
- The redesign shall avoid unnecessary churn in runtime-facing behavior when the problem is presentation and clarity.

---

## 7. Explicit Scope Decisions

### In scope

- Visual redesign of the integrated preview/inspector surface
- Information architecture and hierarchy improvements
- Brand color and typography migration
- Current-state textual visibility
- Field type display
- AI-readable inspector-state contract
- Accessibility refinements

### Out of scope

- Save/load instance workflows
- Multi-panel or detached inspector architecture
- Runtime semantic changes to Precept execution
- New persistence model for preview sessions

---

## 8. Success Criteria

The redesign is successful when:

1. A developer can identify the current state, relevant data, and actionable events at a glance.
2. The visual semantics match the rest of the Precept product surface instead of using a one-off palette.
3. Edit mode remains powerful but feels more intentional and legible.
4. The inspector review and implementation no longer disagree about what the product does.
5. An AI consumer can read the panel state through a structured contract rather than inferring it from rendered HTML.

---

## 9. Open Implementation Notes

- The current implementation is richer than the archived mockup and should be treated as the baseline for capability, not as a problem to simplify away.
- The archived mockup is still useful for interaction intent, especially around direct event execution, reason visibility, and read-first diagram behavior.
- The redesign should preserve the current strengths while making the surface feel deliberate, unified, and explicitly specified.

---

## 10. Source-of-Truth References

| Purpose | File |
| --- | --- |
| Current webview implementation | `tools/Precept.VsCode/webview/inspector-preview.html` |
| Extension host and message flow | `tools/Precept.VsCode/src/extension.ts` |
| Current review doc | `brand/inspector-panel-review.md` |
| Brand surface guidance | `brand/brand-spec.html § 2.3 Inspector Panel` |
| Historical interaction spec | `docs/archive/InteractiveInspectorMockup.md` |
| Early mockup artifact | `tools/Precept.VsCode/mockups/interactive-inspector-mockup.html` |

