# Inspector Panel Design Review
**Reviewer:** Elaine (UX Designer)  
**Developer:** Kramer (Tooling Dev)  
**Date:** [Review Session]  
**Reference Spec:** `brand/visual-surfaces-draft.html § Inspector Panel`

---

## Executive Summary

The inspector panel implementation is **functionally complete and structurally sound**, but uses a **custom color palette** that diverges significantly from the established brand color system. The panel successfully renders field data, constraint violations, and edit workflows — but the current colors don't align with the semantic color vocabulary drafted for Precept's visual surfaces.

This review identifies specific color gaps and proposes a migration path to bring the implementation into alignment with brand principles while preserving the solid UX patterns already in place.

---

## What Was Found

### Implementation Location
- **File:** `tools/Precept.VsCode/webview/inspector-preview.html`
- **CSS Variables (`:root`):**
  ```css
  --state: #6D7F9B;      /* Current state label color */
  --event: #8573A8;      /* Event name color */
  --ok: #1FFF7A;         /* Success/enabled indicator */
  --err: #FF2A57;        /* Error/blocked indicator */
  --muted: #59657A;      /* Secondary text */
  --text: #FFFFFF;       /* Primary text */
  --border: #59657A;     /* UI chrome borders */
  ```

### Rendering Structure
The panel renders field data in a **list-based layout** (`<ul class="data-list">`):
- **Field names** render in `.data-key` spans (no explicit color; inherits from parent)
- **Field values** render in `.data-value-stack` spans with `.muted` class applied to read-only values
- **Constraint violation messages** render in `.data-edit-violation-msg` spans with `color: var(--err)`
- **Edit inputs** use `.data-edit-shell` wrappers with `.violation` class toggled on constraint errors

### Typography
- Font family: `"Segoe UI", Arial, sans-serif` (system UI font, **not** Cascadia Cove monospace)
- Font size: 12px for field data (aligns with spec)
- Line height: Not explicitly set for data list items (defaults to ~1.4)

---

## What Aligns with Brand Principles

### ✅ Structural Layout
The list-based field rendering is clean and scannable. The `.data-key` / `.data-value-stack` split creates clear visual hierarchy between field names and values.

### ✅ Constraint Violation Workflow
Violations are marked with **both color and icon redundancy** — the `.violation` class applies border color, and violation messages display below the field. This matches the accessibility principle from the spec.

### ✅ Edit Mode UX
The edit/view mode toggle and field-level editing with draft validation is well-implemented. The live validation feedback (showing constraint violations as the user types) is exactly what an inspector panel should do.

### ✅ AI-First Structure
The panel renders structured data with clear field/value separation and violation messages. An AI agent parsing the DOM can extract field names, current values, and constraint violations without ambiguity.

---

## What Doesn't Align with Brand Principles

### ❌ Color System Mismatch

| Element | Current Color | Brand Spec Color | Gap |
|---------|---------------|------------------|-----|
| **Field names** | No explicit color (inherits white `--text`) | Slate `#B0BEC5` | Field names should be muted slate, not pure white |
| **Field values (read-only)** | `--muted: #59657A` | Slate `#84929F` | Current muted color is too blue-gray; spec calls for warmer slate |
| **Current state label** | `--state: #6D7F9B` | Violet `#A898F5` | State label should match syntax highlighting violet |
| **Event names** | `--event: #8573A8` | Cyan `#30B8E8` | Event names should match syntax highlighting cyan |
| **Success indicator** | `--ok: #1FFF7A` (bright green) | Enabled `#34D399` | Current green is too neon; spec uses softer emerald |
| **Error indicator** | `--err: #FF2A57` (pink-red) | Blocked `#F87171` | Current red is cooler; spec uses warmer rose-red |
| **Constraint messages** | `--err: #FF2A57` | Gold `#FBBF24` | Violation messages should use gold (human explanation color), not error red |

**Key Finding:** The panel uses a **five-color custom palette** (`state`, `event`, `ok`, `err`, `muted`) instead of the **locked 8+3 brand system** (8 authoring shades + 3 verdict colors). This creates visual discontinuity between the syntax editor, diagram, and inspector — colors that mean one thing in the editor mean something else in the panel.

### ❌ Typography Choice

| Element | Current Font | Brand Spec Font |
|---------|--------------|-----------------|
| Field names/values | `"Segoe UI", Arial, sans-serif` | Cascadia Cove monospace |

The spec calls for **Cascadia Cove monospace** to match code appearance. Field names and values are identifiers from the `.precept` file — using a monospace font maintains visual continuity with the syntax editor.

### ⚠️ Missing Field Type Display

The spec describes field types as "Slate (#9AA8B5). Type info is secondary to the name and value." The current implementation **does not render field types** in the inspector panel (only field names and values are shown). This is a minor gap — type info is useful for debugging but not critical for core functionality.

---

## Accessibility Check

### ✅ Color + Icon Redundancy
Constraint violations use **both color (border + message) and text content** (the violation message itself). A color-blind developer can still identify violated fields by reading the inline error text.

**Recommendation:** Add a **visual symbol** (e.g., `✗` or `⚠`) before the violation message text to strengthen non-color signaling. The spec suggests "Color + icon redundancy" — the current implementation has color + text, but adding an icon would be belt-and-suspenders.

### ⚠️ Table Structure
The current implementation uses a `<ul>` list with `<li>` items. The spec recommends "proper `<th>` headers and `<tr>/<td>` semantics" for screen reader accessibility.

**Status:** The list structure is acceptable (screen readers handle lists well), but a semantic table (`<table>`) with column headers would be more robust for assistive tech. This is a **nice-to-have**, not a blocker.

---

## AI-First Check

### ✅ Structured Output
The panel renders field data in a consistent, parseable structure:
- Field names in `.data-key` spans
- Values in `.data-value-stack` spans
- Violation messages in `.data-edit-violation-msg` spans with `data-field` attributes

An AI agent parsing the inspector HTML can extract:
- Current state (via `currentState` variable in JS)
- Field names and values (via `.data-key` and value rendering)
- Constraint violations (via `.violation` classes and violation message elements)

**Recommendation:** Ensure the inspector panel can **export JSON** (not just render HTML). The spec says "The inspector panel is JSON-serializable state data" — the JS code should expose a `getInspectorState()` method that returns a structured object with `{ currentState, fields, violations }` for AI consumption.

---

## Recommended Next Steps

### Priority 1: Color System Migration (High Impact, Medium Effort)

**Goal:** Replace the custom 5-color palette with the locked brand color system.

**Changes:**
1. **Update CSS variables** in `inspector-preview.html`:
   ```css
   :root {
     /* Brand structural colors */
     --indigo: #6366F1;           /* Structure grammar */
     --indigo-semantic: #4338CA;  /* Semantic structure keywords */
     --violet: #A898F5;           /* State names */
     --cyan: #30B8E8;             /* Event names */
     
     /* Brand data shades (slate family) */
     --slate-names: #B0BEC5;      /* Field names */
     --slate-types: #9AA8B5;      /* Field types */
     --slate-values: #84929F;     /* Field values */
     
     /* Brand verdict colors */
     --enabled: #34D399;          /* Success/enabled */
     --blocked: #F87171;          /* Error/blocked */
     --warning: #FDE047;          /* Warning */
     
     /* Brand message color */
     --gold: #FBBF24;             /* Human explanation text */
     
     /* Neutral UI colors */
     --text: #FFFFFF;
     --border: #59657A;
     --muted: #9096A6;            /* Dusk indigo for read-only indicators */
   }
   ```

2. **Remap element colors:**
   - Field names (`.data-key`): Apply `color: var(--slate-names);`
   - Field values (`.muted`): Change to `color: var(--slate-values);`
   - Current state label: Change to `color: var(--violet);`
   - Event labels: Change to `color: var(--cyan);`
   - Constraint violation messages (`.data-edit-violation-msg`): Change to `color: var(--gold);`
   - Success indicators: Change `--ok` to `--enabled: #34D399;`
   - Error indicators: Change `--err` to `--blocked: #F87171;`

**Rationale:** This brings the inspector panel into alignment with the syntax editor and diagram, creating visual continuity across all three surfaces. A developer seeing violet state names in the editor will immediately recognize the same violet in the inspector.

---

### Priority 2: Typography Correction (Medium Impact, Low Effort)

**Goal:** Switch from system UI font to Cascadia Cove monospace.

**Changes:**
1. Update the `body` font declaration:
   ```css
   body {
     font-family: "Cascadia Cove", "Cascadia Code", "Consolas", monospace;
     /* ... */
   }
   ```

2. Verify monospace rendering:
   - Field names and values should appear in monospace
   - UI chrome (buttons, headers) can remain in system font (add explicit `font-family` overrides to `.header`, `.actions`, etc.)

**Rationale:** Field names like `orderTotal` or `isApproved` are code identifiers — monospace rendering maintains continuity with the syntax editor where those same identifiers appear.

---

### Priority 3: Add Field Type Display (Low Impact, Medium Effort)

**Goal:** Show field types as secondary info next to field names.

**Changes:**
1. Modify the `dataList.innerHTML` rendering to include type info:
   ```html
   <span class="data-key">
     ${fieldName}
     <span class="data-type">${fieldType}</span>
     ${ruleIcon}
   </span>
   ```

2. Add CSS for `.data-type`:
   ```css
   .data-type {
     color: var(--slate-types);
     font-size: 10px;
     margin-left: 4px;
     opacity: 0.75;
   }
   ```

**Rationale:** Type info helps developers debug type mismatches (e.g., "Why is `count` showing 'abc'? Oh, it's a string field, not a number."). Secondary to the core workflow but useful for clarity.

---

### Priority 4: Strengthen Accessibility (Low Impact, Low Effort)

**Goal:** Add visual symbols to violation messages.

**Changes:**
1. Prepend a symbol to violation messages:
   ```html
   <span class="data-edit-violation-msg" data-field="${canonicalFieldName}">
     ✗ ${violationText}
   </span>
   ```

**Rationale:** Belt-and-suspenders redundancy — color + icon + text ensures no developer misses a constraint violation, regardless of visual ability or terminal theme.

---

### Optional: JSON Export for AI Agents

**Goal:** Expose a structured API for AI tools to extract inspector state.

**Changes:**
1. Add a `getInspectorState()` function in the JS:
   ```js
   function getInspectorState() {
     return {
       currentState: currentState,
       fields: Object.entries(data).map(([key, value]) => ({
         name: key,
         value: value,
         editable: Boolean(getEditableFieldForUiKey(key)),
         violations: /* extract from currentEditableFields */
       })),
       violations: /* extract from draftFieldErrors */
     };
   }
   ```

2. Expose this method to the host extension (via `postMessage` or similar).

**Rationale:** The spec says "The inspector is both a human debugging tool and an API contract for AI tools." JSON export ensures AI agents can consume inspector state programmatically, not just by parsing HTML.

---

## Summary

Kramer's inspector panel implementation is **structurally excellent** — the edit workflow, constraint validation, and layout are well-designed and fully functional. The **primary gap** is the color system: the panel uses a custom palette that doesn't align with the locked brand colors from `visual-surfaces-draft.html`.

**Recommendation:** Migrate to the brand color system (Priority 1) and switch to Cascadia Cove monospace (Priority 2) in the next iteration. These changes preserve all existing functionality while creating visual continuity across the syntax editor, diagram, and inspector.

The panel is **production-ready as-is** for functional testing, but should be brought into brand alignment before public release.

---

## Next Steps for Shane

1. **Review this report** and confirm the color/typography migration approach.
2. **Decide priority:** Should Kramer address Priority 1 + 2 immediately, or defer to a polish pass after feature completion?
3. **Clarify JSON export:** Is AI-first JSON export a requirement, or is the current HTML structure sufficient?

Once priorities are confirmed, Kramer can execute the color migration with confidence — the changes are CSS-only and don't require rework of the core rendering logic.
