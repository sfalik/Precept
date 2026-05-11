# Inspector / Preview Surface Freshness Review

**Reviewer:** Elaine (UX Designer)  
**Requested by:** Shane  
**Date:** 2026-04-05  
**Reference spec:** `design/system/foundations/semantic-visual-system.html`  
**Compared against:** `tools/Precept.VsCode/webview/inspector-preview.html`, `tools/Precept.VsCode/mockups/interactive-inspector-mockup.html`, `docs/archive/InteractiveInspectorMockup.md`

---

## Freshness verdict

**Verdict: partially stale — refresh required before using this as PRD input.**

The old review was right about the **brand drift**. It is no longer current about the **surface scope**. The implementation is now a combined preview/inspector webview with a diagram, in-canvas data lane, event dock, edit workflow, runtime banners, and host messaging. A PRD should use this refreshed audit, not the prior color-only review, as the starting point.

---

## What is still current

These findings still hold and should stay in the PRD:

- **Custom palette drift remains unresolved.** The webview still defines off-system colors in `:root` (`--state: #6D7F9B`, `--event: #8573A8`, `--ok: #1FFF7A`, `--err: #FF2A57`, `--muted: #59657A`) instead of the locked preview and inspector guidance in `design/system/foundations/semantic-visual-system.html`.
- **Typography drift remains unresolved.** `body` still uses `"Segoe UI", Arial, sans-serif` instead of the locked Cascadia Cove-based mono treatment for field data.
- **Field hierarchy still needs brand mapping.** Field names still read as plain white/inherited text, read-only values still lean on the blue-gray muted color, and inline violation text still uses error red instead of the spec's gold message treatment.
- **Field types are still not surfaced.** The runtime already carries `fieldType` for editable fields, but the panel does not render type information in the data lane.

---

## What was stale in the old review

### 1. The reference spec was outdated

The old review cited `brand/visual-surfaces-draft.html`. That is no longer the right source of truth. The live reference is now:

- `design/system/foundations/semantic-visual-system.html`

That matters because the color and semantics guidance has been tightened since the draft surface writeup.

### 2. The review described too small a surface

The current implementation is not just a field list with edit inputs. It is a **combined preview shell** with three UX zones:

1. **Header shell** — title, source file name, follow/lock mode indicator, Edit / Save / Cancel / Reset actions  
2. **Diagram canvas** — animated state diagram with runtime verdict overlays  
3. **Inspector controls** — in-canvas data lane plus bottom event dock with inline arguments and row feedback

The archived mockup and the current implementation largely agree on this shape. The old review only really covered the data list.

### 3. Several implemented behaviors were missing entirely

The current surface includes behavior the old review did not mention:

- rule-violations banner in the data lane
- state-rules badge with tooltip
- draft-validation banner during edit mode
- field-level rule icons
- nullable-field toggle in edit mode
- transient `before → after` value toasts after fire/save
- row-level event feedback in the dock
- inline event argument validation
- preview follow/lock status in the header
- structured host request/response messaging between the webview and extension host

For PRD work, these are not edge cases. They are part of the baseline interaction model.

### 4. The accessibility read was too blunt

The old review implied a semantic table would be the preferred destination. That is too simplistic for the current surface.

The inspector now lives inside an in-canvas overlay with constrained width and internal scrolling. A table **may** still be the right accessibility answer, but it is not automatically better than the current list. The real requirement is:

- clear field/value pairing
- strong keyboard behavior
- screen-reader-announced structure
- non-color signaling for violations

The PRD should evaluate structure from those goals, not assume "table" by default.

### 5. The AI-first recommendation needs reframing

The old review recommended a DOM-level `getInspectorState()` helper. That recommendation is stale as written.

The current surface already participates in a structured host protocol (`previewRequest` / `previewResponse`, plus snapshot, inspect, update, fire, reset flows). For PRD purposes, the AI-first question is not "should the DOM expose a helper?" It is:

- what structured state contract must the preview host guarantee?
- what data must remain stable for both human UX and agent consumption?

That is a better framing than adding a webview-only helper method.

---

## Current surface snapshot

### Layout that exists today

- **Header:** `State Diagram` title, file name, follow/lock mode label, Edit / Save / Cancel / Reset
- **Main canvas:** scrollable SVG diagram
- **Data lane:** inspector list rendered inside the diagram canvas, with rule and draft banners above it
- **Event dock:** current-state event list with inline args, microstatus glyphs, inline reasons, and row feedback

### Interaction patterns that match the archived mockup

These patterns are implemented and should be treated as real baseline behavior:

- diagram-first layout with data inside the canvas and events in a bottom dock
- inline event args beside the relevant event row
- transient `before → after` data feedback
- row-anchored fire feedback
- hover/focus-driven event emphasis
- keyboard event navigation
- reset flow

### Important implementation additions beyond the archived mockup

- explicit field edit mode with Save / Cancel
- live draft validation via host round-trips
- state-rule and rule-violation callouts
- file-following vs locked preview mode in the shell

---

## PRD implications

### Keep as baseline

- the **diagram + in-canvas data lane + bottom event dock** triad
- inline event arguments and row-level feedback
- transient field-change feedback after fire/save
- edit mode as a deliberate workflow, not inline freeform mutation everywhere

### Redesign focus areas

1. **Brand alignment**
   - migrate the webview off the legacy palette
   - apply the locked inspector typography and data hierarchy

2. **Shell clarity**
   - decide whether current state needs an explicit textual label in the header in addition to the diagram
   - clarify how file name, follow/lock status, and actions are visually prioritized

3. **Inspector readability**
   - decide whether field types should be visible
   - improve field/value/violation scanning without losing the compact overlay model

4. **Accessibility**
   - strengthen violation redundancy at the field level
   - validate keyboard flow across event rows, inline args, edit mode, and overlay scrolling
   - choose the right semantic structure for the data lane based on assistive-tech behavior, not assumption

5. **AI-first contract**
   - define the stable structured state the preview host must expose
   - avoid tying agent consumption to fragile DOM scraping

---

## Concise recommendation

**Use this file as PRD input, not the older review framing.**  
The redesign should assume the current preview surface is **functionally rich but visually inconsistent**: interaction model mostly worth preserving, brand system and information hierarchy still in need of a proper pass.
