# Verdict Modifiers UX Perspective

**Author:** Elaine (UX Designer)  
**Date:** 2026-04-11  
**Relationship:** Complements Frank's linguistic research (`verdict-modifiers.md`) with visual-system and interaction-design analysis  
**Scope:** UX implications across all Precept authoring and preview surfaces

---

## Executive Summary

Verdict modifiers (success/warning/error severity annotations) introduce a second semantic layer to Precept: authored intent (declared by the designer) layered beneath runtime outcomes (determined by execution). This creates novel UX opportunities and risks.

**The core opportunity:** State verdict modifiers are genuinely novel territory. No comparable system annotates states with verdict severity. When authorship declares "state Approved success" and "state Denied error," the diagram tells a visual story — happy paths (emerald territories) versus error paths (rose territories). This is a natural visual differentiator and a powerful governance communication tool.

**The core risk:** Conflating authored intent with runtime outcome will confuse users. The verdict modifier may declare intent (success), but runtime may produce a different outcome (rejection). Both must remain visible and distinct, or the feature becomes a source of false confidence.

**Key UX findings:**
1. **Visual system alignment is critical.** Verdict modifiers must integrate with the semantic visual system (emerald/amber/rose) without replacing it — they layer on top as authored intent, with runtime outcomes as potential overlays.
2. **State verdict is a differentiator.** Combine with preview diagram rendering to show topology coverage: "N% of reachable states are success endpoints." This is unique to Precept.
3. **Event verdict is highest-priority.** Strongest precedent (BPMN, FluentValidation), highest authoring clarity benefit, lowest visual noise. Ship event verdict first.
4. **Interaction burden is real.** Verdict modifiers are optional, so they may feel like "gotchas" if not scaffolded well. Completions, inline hints, and diagnostic messaging must guide authors toward consistent declarations.
5. **Minimum viable treatment.** The visual signal can be minimal (a small badge, a secondary color shade) — the value is in the *meaning*, not the visual noise.

---

## 1. Visual System Alignment

### Current State: Runtime Verdicts as Overlays

Precept's semantic visual system currently treats verdict colors as **runtime-applied overlays**:

- **Emerald `#34D399`** — success: transition completed, constraints satisfied, valid configuration
- **Amber `#FCD34D`** — warning: condition met but flagged as heuristic concern, soft constraint met
- **Rose `#FB7185`** — error: rejection or constraint violation, hard structural problem

These colors are applied dynamically based on `TransitionOutcome` (Transition, NoTransition, Rejected, ConstraintFailure, Unmatched, Undefined) and diagnostic codes (C48–C52). The definition-time authoring surface (the editor) has no way to declare verdict intent — you read the verdict colors only at runtime, in the preview panel.

### Proposed Addition: Authored Intent Layer

Verdict modifiers introduce a second, authored layer **beneath** the runtime layer:

```
┌─────────────────────────────────────────┐
│ Runtime Verdict Overlay                 │
│ (TransitionOutcome determines color)    │
│ Emerald (transition) or Rose (rejected) │
└─────────────────────────────────────────┘
         ↓ (optional, if different)
┌─────────────────────────────────────────┐
│ Authored Verdict Modifier               │
│ (declared by DSL: success/warning/error)│
│ Emerald, Amber, or Rose                 │
└─────────────────────────────────────────┘
         ↓
┌─────────────────────────────────────────┐
│ Base Construct Identity                 │
│ (state name, event name, rule label)    │
└─────────────────────────────────────────┘
```

### Layering Rules

**Rule 1: Authored verdict is the baseline; runtime outcome is the variation.**
- If `state Approved success` and runtime produces `Transition`, render at full emerald saturation (confident match).
- If `state Approved success` but runtime produces `Rejected`, render authored intent at reduced opacity (doubt/mismatch), then overlay runtime outcome at full saturation. Visual suggests "author expected success, but runtime delivered error."

**Rule 2: Base construct identity remains stable.**
- State name (`Approved`) is always rendered in base violet, regardless of verdict modifier.
- Verdict modifier is an *additive* signal, not a *replacement* of identity.
- This prevents verdicts from overwhelming construct identity.

**Rule 3: Authored verdict is visible at definition-time; runtime verdict is visible at execution-time.**
- Definition surface (editor + grammar): verdict modifiers rendered as syntax tokens or inline badges.
- Runtime surfaces (preview diagram, inspector): verdict modifiers rendered as semantic overlays; runtime outcomes rendered atop them.

### Interaction with Existing Signals

The semantic visual system uses four signal channels simultaneously: color, typography, form, and layout. Verdict modifiers must respect channel precedence and combination rules.

**Typography:** Verdict modifiers should NOT introduce new italic/bold treatments. Reserve italic for existing constraint pressure signals (e.g., constrained field names).

**Form:** Verdict modifiers should NOT alter node shapes or borders. Terminal states already use dashed double borders; success/error verdict modifiers complement, not replace, that signal.

**Layout:** Verdict modifiers should NOT alter spatial positioning. Using layout to indicate verdict would lock visual meaning to topology, which breaks cross-surface translation.

**Color:** Verdict modifiers use color, but after construct identity color. Layering in order:
  1. **Primary construct signal** (State = violet, Event = cyan, Rule = gold)
  2. **Runtime outcome or authored verdict** (emerald/amber/rose)
  3. **Constraint pressure** (opacity/saturation variation)

---

## 2. Surface-by-Surface UX Impact

### Surface 1: Code Editor (Definition Surface)

**Current state:** Keywords recognized (state, event, from, on). State names rendered in violet mono. No verdict annotations.

**With verdict modifiers:**
- State declarations: `state Approved success` → state name (violet) + verdict keyword (emerald 30% opacity or badge)
- Event declarations: `on Approve success` → event name (cyan) + verdict keyword
- Rule declarations: `invariant ... because "..." warning` → message (gold) + verdict keyword

**UX approach — minimal inline annotation:**
```
state Approved success          ← verdict keyword at end of declaration
state Denied error              ← color-coded in syntax highlight
state UnderReview warning       ← suggestion: emoji badges vs keywords?
```

**Authoring guidance:**
- Completions suggest verdicts when:
  - Cursor after `state StateName` → suggest `success|warning|error` with ranks (success most common, others context-specific)
  - Cursor after `on EventName` → guess based on transition rows (if all rows lead to transition, suggest `success`; if any reject, suggest `error`)
  - Cursor after `because` → suggest `warning` as lower-severity default; `error` requires explicit opt-in
- Hints (on-hover):
  - `state Approved success` → "This state is a success endpoint. Success states are highlighted in preview diagrams."
  - `invariant ... warning` → "This constraint is advisory. Violations are warned, not blocked."

**Visual approach:**
- Verdict keywords styled with construct-family color + reduced saturation (signal as metadata, not primary)
- Example: `success` rendered in emerald but 60% opacity to signal "authored intent, not runtime"
- Alternative: small pill badge to the right of the declaration, emoji or letter-based (`✓`, `⚠`, `⨯`)

**Key interaction:** The verdict modifier is visible at definition time, guiding the author's thinking about the state or event's role in the business logic.

---

### Surface 2: Preview Webview — State Diagram

**Current state:** Violet hard-rect nodes, event edges in cyan, current state highlighted, terminal states (double border) marked.

**With state verdict modifiers:**

**Node rendering:**
- Approved (success): violet node + emerald fill (20% opacity) or emerald border accent
- Denied (error): violet node + rose fill (20% opacity) or rose border accent
- UnderReview (warning): violet node + amber fill (20% opacity) or amber border accent

**Glyph approach** (stronger signal, less occlusion):
- Success endpoint: small emerald checkmark (✓) badge in top-right corner of node
- Error endpoint: small rose X (✕) badge
- Warning state: small amber triangle (⚠) badge

**Rendering order** (to avoid noise):
- Base violet node (construct identity)
- Verdict modifier badge or fill overlay (authored intent)
- Current state glow or highlight (runtime state)
- Terminal state double border (lifecycle signal)

**Coverage visualization** (Tour-de-Force feature):
- Sidebar or status-bar stat: "Reachable from current state: 2 success, 1 warning, 0 error"
- Hovering over a state shows: "Paths to success: 3 | Paths to error: 2"
- This is unique to Precept and visually powerful for governance communication.

**Preview interaction:**
- When previewing an event, highlight the outcome path with runtime verdict color
- If outcome differs from event's authored verdict (e.g., event is `success` but outcome is `Rejected`):
  - Show the authored verdict as a faint badge
  - Overlay the runtime outcome in bold
  - Combine with tooltip: "Expected: success, Got: rejected (constraint violation)"

**Key insight:** The diagram becomes a governance map — not just topology visualization, but a visual statement of "these are the good paths, these are the bad paths."

---

### Surface 3: Preview Webview — Data/Inspector Panel

**Current state:** Field name/type/value grid, rule violations in red callouts, constraint messages inline.

**With verdict modifiers on rules:**

**Constraint message rendering:**
```
Current rendering (no verdict):
[Rule violation icon] Balance must be ≥ 0

With error verdict (current default):
[✕ icon in rose] Balance must be ≥ 0

With warning verdict (new):
[⚠ icon in amber] Notes should be < 500 characters
```

**Behavior difference:**
- **Error verdict:** Callout in rose, operation is blocked. Same as current "rejection" rendering.
- **Warning verdict:** Callout in amber, operation is allowed but flagged. Stronger visual signal than current "C49 Orphaned Event" diagnostics.

**Format:**
```precept
invariant Balance >= 0 because "Balance must be non-negative" error
invariant NotesLength < 500 because "Keep notes concise" warning
```

**Inspector display:**
```
[rose ✕] Balance must be non-negative — CONSTRAINT VIOLATION (error)
[amber ⚠] Keep notes concise — CONSTRAINT WARNING (advisory)
```

**Edit interaction:**
- Editable fields show validity inline. A `warning`-severity rule violation does NOT block editing (like current behavior).
- Compare states with both error and warning violations:
  - Error violations: field background light red, save is disabled
  - Warning violations: field background light amber, save is allowed but toast warns on commit

**Key interaction:** Warning-verdict constraints become visible during debugging without blocking the simulation — teaching the author about non-critical concerns without operation stoppage.

---

### Surface 4: Hover/Tooltip

**On state node hover:**
```
state Approved {
  Terminal: yes
  Verdict: success
  Reachable from initial: yes
  # of paths from current: 2
  # of paths to terminal: yes (success)
}
```

**On event edge hover:**
```
on Approve
  Verdict: success
  Outcomes: Transition, NoTransition
  Expected: all Transition/NoTransition ✓
  Last fired: (current event trace)
}
```

**On rule violation hover:**
```
Invariant violation: Balance >= 0
Severity: error
Status: BLOCKED (operation not allowed)
Affected fields: Balance
What to fix: Increase Balance
```

**Key insight:** Tooltips make the authored intent explicit without cluttering the baseline rendering.

---

### Surface 5: Language Server Completions

**Event verdicts — highest priority:**
When user types `on EventName` or is inside an on-clause:
1. If the event's transition rows are homogeneous (all lead to Transition/NoTransition):
   - Suggest `success` with highest rank and explanation "All outcomes are transitions"
2. If rows are mixed (some reject/constraint-fail):
   - Suggest `error` with high rank and explanation "Some outcomes are rejections"
3. If unknown:
   - Suggest `success|warning|error` alphabetically; user picks based on intent

**State verdicts — secondary:**
When user types `state StateName`:
1. Test if state is reachable from initial:
   - If unreachable: suggest nothing (unreachable state often indicates error)
   - If reachable: suggest `success|warning|error` without ranking; user picks
2. If state is terminal:
   - Suggest all three with equal rank; terminal states often have explicit intent
3. Context hint: "Terminal states often carry verdict modifiers to indicate success/error endpoints"

**Rule verdicts:**
When user types `because` in an invariant or assert:
1. Suggest `error` (default, most conservative)
2. Offer `warning` with explanation "Advisory constraint — violation flags but does not block"
3. Never suggest `success` for rules (negative statement doesn't make sense)

**Snippet patterns:**
```
Snippet: state X success
Snippet: state X error
Snippet: state X warning
Snippet: on Y success
Snippet: on Y error
Snippet: on Y warning
```

---

### Surface 6: Diagnostics and Error Messages

**New diagnostic code — Verdict Mismatch Warnings:**

When a declared verdict doesn't match the actual behavior:

```
C60: Event verdict mismatch
"Approve" is declared as success event, but contains a rejection outcome.
Hint: All success-intended events must have only Transition or NoTransition outcomes.
Suggestion: Declare as warning or error, or fix the rejection condition.
```

```
C61: State unreachable
"InReview" is declared as success endpoint, but is not reachable from initial state.
Hint: Success endpoints should be reachable or serve as fallback error targets.
```

```
C62: Event outcome type inconsistency
"Escalate" is declared as warning, but has only Transition outcomes (no mixed/reject).
Hint: Warning events typically have mixed outcomes. Declare as success if all transitions.
```

**Current diagnostic appearance:**
- Red squiggly underline (errors block)
- Orange squiggly underline (warnings advise)
- Blue squiggly underline (hints inform)

**With verdict modifiers:**
- Add new diagnostic codes (C60, C61, C62) in the blue-hint tier initially, promote to warning tier if opt-in
- Display verdict mismatches with the construct they describe highlighted
- Example: `on Approve success` with a rejection row → error on `success` keyword with "Expected: success, Got: rejection"

---

## 3. State Verdict as a Visual Differentiator

Frank's research confirms: **No comparable system bakes verdict severity into state declarations.** XState, BPMN, Kubernetes, Roslyn—none of them do this. This is genuine novelty.

### The Visual Narrative

When a Precept precept declares:

```precept
state Submitted
state Approved success
state Denied error
state Escalated warning
```

The preview diagram tells a story without additional prose:

```
[Submitted] ──Approve──> [Approved ✓]    ← emerald/success endpoint
      ├──Deny─────> [Denied ✕]          ← rose/error endpoint
      └──Escalate─> [Escalated ⚠]       ← amber/warning state
```

This visual story communicates **business logic intent** at a glance:
- Approval is a success path (desirable outcome)
- Denial is an error path (final rejection)
- Escalation is a concern path (interim warning state)

### Comparative Value

**Traditional state diagrams (UML, XState):**
- Show topology only — where the entity can go
- No inherent meaning in the shapes or colors
- Require reading the state names and external documentation to understand "which are good, which are bad"

**Precept with state verdicts:**
- Show topology + governance intention
- Visual color immediately groups states by outcome severity
- No additional documentation needed — the diagram *is* the specification
- Enables novel coverage queries: "What % of our reachable states are success endpoints?" (governance health metric)

### Use Cases

**Case 1: Non-technical stakeholder reviews the diagram**
> "I can see three paths from Submitted. Two lead to success (Approved), one leads to error (Denied). Seems fair."
> No technical training needed; the coloring admits the intent.

**Case 2: Developer debugging a rejection path**
> "The event flow leads to an error state (rose). Guard is here [highlighted]. Let me check why it's triggering."
> Verdict color narrows debugging focus.

**Case 3: QA planning test scenarios**
> "Happy path (green): Submitted → Approved → Paid. Error path: Submitted → Denied. Warning path: → Escalated → ..."
> Test plans organize naturally around verdict paths, not arbitrary state groupings.

**Case 4: Compliance audit**
> "Show me all error endpoints." Auditor immediately sees: Denied, Cancelled, Failed. Easier than scanning documentation.

---

## 4. Interaction Patterns

### Pattern 1: Verdict Modifier as Authoring Scaffold

Verdict modifiers should guide authors *toward* consistency, not punish *after*. Early-stage completions and hints are key:

**Design principle:** Make the right choice the easiest choice.

- **Completions suggest verdicts based on syntax analysis.** If the user types `from S on E when Guard -> transition T ...`, the `success` verdict appears pre-filled in completions for event `E`.
- **Inline hints appear on ambiguous declarations.** `state X` (no verdict) shows a lightbulb: "Add a verdict? success/warning/error" with quick-fix suggestions.
- **Status bar shows verdict coverage:** "Verdict coverage: 5/7 states (71%)." Encourages authors to annotate important states even if all states don't require verdicts.

### Pattern 2: Verdict Modifiers as Diagnostic Scaffolding

Diagnostics should educate:

```
Warning: State "UnderReview" has no verdict modifier.
Info: Terminal states often carry verdicts (success or error).
Suggestion: Is "UnderReview" a success or error endpoint? Add modifier: `state UnderReview success`
```

### Pattern 3: Authored vs Runtime Verdict Visual Distinction

When authored verdict and runtime verdict differ:

1. **Before simulation:** Show authored intent badge
2. **After simulation:** Overlay runtime outcome
3. **On mismatch:** Highlight the verdict modifiers with a diagnostic

```
state Approved success
   ↓ (on event fire)
[Event outcome: Rejected]
   ↓ (diagnostic)
C60: Verdict mismatch — Approved was marked success but produced rejection
    Guard condition: Amount > LimitError: Verdict verdict mismatch — Approved was marked success but produced rejection
```

### Pattern 4: Optional Adoption Path

Verdict modifiers are **optional**, so adoption should be low-friction:

- **Phase 1:** Add verdicts only to terminal states (Approved, Denied, Cancelled, etc.)
- **Phase 2:** Add verdicts to events that serve critical business logic (Approve, Reject)
- **Phase 3:** Add verdicts to all rules that enforce critical invariants

Completions and hints guide this natural progression.

---

## 5. UX Risks and Open Questions

### Risk 1: Visual Noise

**Problem:** Adding verdict colors to every surface could feel cluttered, especially for complex precepts with 6+ states.

**Mitigation:**
- Verdicts are optional; most states can remain unadorned
- Use badges (small glyphs) instead of full fill colors — bolder would definitely add noise
- Progressive disclosure: hover to see verdict; main view shows only current state verdict highlight
- Diagram toggle: "Show verdict modifiers" / "Hide verdict modifiers" for different contexts

**Open question:** How many states is too many for the current rendering approach? (Test with HiringPipeline: 7 states, InsuranceClaim: 6 states)

### Risk 2: Precedent Confusion

**Problem:** Users familiar with other state machines (XState, UML) expect no verdict annotations on states. Our innovation could confuse or feel alien.

**Mitigation:**
- Docs lead with the differentiator: "Precept uniquely annotates outcomes with business severity. Here's why this matters..."
- Marketplace tagline: "Govern entity lifecycles with explicit success/error outcomes."
- README example leads with a state verdict diagram to normalize the concept early

**Open question:** What's the onboarding friction cost? (Requires user testing with unfamiliar users.)

### Risk 3: Verdict Mismatch Confusion

**Problem:** When authored verdict differs from runtime outcome, users may misunderstand which is "correct."

**Mitigation:**
- Explicit language in tooltips: "Authored as: success | Runtime produced: rejection"
- Diagnostic message: "C60: Expected success, but operation was rejected. See guard condition at line..."
- Preview UI shows both with temporal sequence: "Author expected success → Runtime produced rejection"

**Open question:** Should mismatches be errors, warnings, or info? (Recommend warnings — they should be visible but not blocking.)

### Risk 4: Authoring Friction if Verdicts Feel Required

**Problem:** If incomplete verdict declarations feel incomplete (missing lint), authors adopt half-heartedly or abandon the feature.

**Mitigation:**
- Verdicts are **not** required; linting only suggests, never demands
- Status bar shows coverage as informational, not prescriptive
- First-time users see a tutorial modal, not a compliance checklist

**Open question:** Should there be a `.squad` setting for "require verdict modifiers on terminal states"? (Probably yes, but not default.)

---

## 6. Interaction Design — The Author's Journey

### Scenario: Author Creates a New Precept Workflow

**Step 1: Author types `state Approved`**
- Language server suggests: `Approved | Approved success | Approved error | Approved warning`
- Pre-ranked: `Approved success` at top (most terminal states are success endpoints)

**Step 2: Author selects `Approved success`**
- Keyword `success` rendered in emerald 60% opacity
- Status bar updates: "Verdict coverage: 1/3 states"

**Step 3: Author types `state Denied`**
- Language server suggests: `Denied error` (analysis: if any rejection rows lead here, suggest error)

**Step 4: Author types `on Approve`**
- Language server suggests: `success` (all rows lead to Transition/NoTransition)
- Hint: "All outcomes are transitions; consider `success` to signal intent"

**Step 5: Author previews**
- Diagram shows emerald checkmark on Approved, rose X on Denied
- Coverage stat: "2/2 terminal states have verdicts"
- Event "Approve" rendered with emerald accent (success event)

**Step 6: During simulation, an event produces an unexpected outcome**
- Author fires Approve
- Expected: Transition to Approved (success)
- Actual: Rejected (constraint violation)
- Preview shows: authored success badge (faded) with runtime rejection overlay (bold rose)
- Diagnostic: `C60: Verdict mismatch — Event expected success but produced rejection`

**Author's reaction:** "Oh! I declared Approve as success, but the guard on the Approved state is blocking it. Let me check the invariant."

---

## 7. Minimum Viable Visual Treatment

To avoid over-engineering, the visual signal can be minimal:

**Badges (preferred):**
- Success: Small emerald checkmark (✓) badge, 16×16px, top-right corner of state node
- Error: Small rose X (✕) badge
- Warning: Small amber triangle (⚠) badge
- Renders in addition to base violet node, no fill occlusion

**Color overlays (fallback if badges too small):**
- Success endpoint: Verb node outline in emerald 40% opacity
- Error endpoint: Verb node outline in rose 40% opacity
- Warning state: Verb node outline in amber 40% opacity

**Typography (for DSL editor only):**
- Verdict keyword (success/warning/error) inherited from construct-family color (emerald/amber/rose) at 60% opacity
- Mono weight, same size as state name

**This minimal treatment avoids visual noise while maintaining semantic clarity.**

---

## 8. Open Design Questions for Shane Review

1. **Should verdict modifiers default to optional or required on terminal states?** (Recommend: optional initially, escalate to strong encouragement after user research)

2. **When does a state count as "terminal"?** (Recommend: structurally terminal -- has no outgoing transitions OR is marked with a `terminal` modifier)

3. **Should event verdict suggestions be auto-populated or offered as completions?** (Recommend: completions only, no auto-population — authors should decide)

4. **How should stateless precepts handle verdicts?** (Recommend: verdict modifiers do not apply to stateless precepts; they're data-only governance)

5. **Should successful paths show edge highlights in the diagram during simulation?** (Recommend: yes, as a follow-on feature after stateverdicts ship)

6. **What's the right diagnostic severity for verdict-mismatch warnings?** (Recommend: hint tier initially, escalate to warning after user research validates the UX value)

---

## 9. Recommended Ship Order

Based on UX analysis:

1. **Event verdicts first** — highest precedent, lowest visual noise, strong authoring clarity benefit
2. **Rule verdicts second** — builds on event semantics, extends constraint messaging
3. **State verdicts third** — highest novelty, highest visual differentiation, but requires more UX validation

This order lets the team iterate on verdict interactions at the event level, prove the value, then bring the innovation to states.

---

## Conclusion

Verdict modifiers are a genuine visual systems innovation for Precept. They layer authored intent beneath runtime outcomes, enabling the diagram to tell a governance story — not just topology, but business logic severity mapping.

The core UX value lies in **making invisible intent visible**: the author's expectation is now legible alongside the runtime truth, enabling faster debugging, better test planning, and stronger governance communication.

The core UX risk is **distinguishing authored from runtime verdicts** — both must be visible, and their difference must be unmistakable.

The recommended path forward: Start with event verdicts (strong precedent, low risk), prove the value and interaction patterns, then expand to states (novel, high differentiation, requires more research).
