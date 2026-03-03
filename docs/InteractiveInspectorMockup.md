# Interactive Inspector Mockup (Option 4)

Goal: validate UX for executing `.sm` transitions from a VS Code side panel before building runtime integration.

## Panel Layout (concept)

- Header
  - Machine file: `trafficlight.sm`
  - Session state: `Connected`
  - Actions: `Reload`, `Reset Instance`, `Save Instance`
- Main surface
  - State diagram (read-first; no click interaction required)
  - `Data` is rendered inside the diagram canvas in a dedicated non-overlapping lane
  - Bottom event dock (vertical event list)
  - States are laid out in Mermaid-like horizontal flow (primary cycle left-to-right, side phases above/below)
  - Multi-outcome branches use repeated event labels and guard tags (standard FSM style)
  - Bottom dock is controls-only and drives interaction with a vertical event list for the **current state only**
  - Event arguments (when required) are shown inline next to that event button in the bottom row
  - After a successful event execution, inline event-arg inputs reset to empty values
  - Hovering an event button highlights only matching lines that originate from the current state
  - Event selection is transient and hover/focus-driven (no sticky selection when pointer/focus leaves)
  - Leaving inline arg focus also clears transient event selection when focus exits the event row
  - Each event button includes a compact microstatus glyph (`✔`, `✖`, `∅`)
  - Microstatus glyphs update live as inline event args change
  - Undefined (`∅`) events use a disabled-style affordance (`not-allowed` cursor + reduced opacity)
  - Fired events show a transient row-anchored result chip in the event list as the execution feedback channel (kept visible long enough to read reason text)
  - Blocked/undefined (red) events always show reason text inline beneath the event row, and that line clears immediately when the event becomes green
  - Inline reason text remains visible for all red events but is slightly dimmed for non-selected rows to preserve focus
  - Hovering an event row temporarily applies selected-level inline reason emphasis
  - Hovering/selection also increases event-label contrast for clearer row focus
  - Keyboard flow: `ArrowUp/ArrowDown` moves event focus, `Enter` executes selected event, `Tab` reaches inline args

## Example Interaction

1. Open `.sm` file and start inspector session.
2. Panel shows current state `Red` and selectable transition lines in the diagram.
3. Hover `Emergency` in the bottom event list to highlight emergency paths.
4. Enter event args directly in the inline fields beside `Emergency`:
   - `AuthorizedBy = Dispatcher`
   - `Reason = Accident`
5. Click `Emergency` (event buttons execute directly; no separate fire action).
6. Current data shows:
   - `EmergencyReason: null -> "Dispatcher: Accident"`

## Final Behavior Contract

- Bottom dock event list is the primary interaction surface; it is state-scoped and ordered by machine definition order.
- Data is shown inside the diagram canvas in a dedicated non-overlapping lane with internal scrolling.
- Event buttons execute directly (no separate fire action); hover/focus drives transient selection and inspect-style preview.
- Inline args are shown beside `Emergency`; editing args immediately re-evaluates event status and diagram semantics.
- `Emergency` inline args reset after successful fire.
- When data changes on fire, each changed field temporarily shows transient inline `before → after` text (not a chip) instead of the current value; after timeout, the normal current (`after`) value is shown again.
- Event status semantics: green = enabled, red = blocked/reject, red/disabled-style = undefined (`∅`).
- Red events always show inline reason text; reason line clears as soon as the event re-evaluates to green.
- Fire feedback uses row-anchored transient result chips (success/error).
- Successful fires trigger one coherent timeline animation: source-state semantics fade out while destination-state semantics fade in, with a runner moving along the accepted transition path.
- Hover-driven event emphasis is suppressed until the transition timeline completes.
- During the transition timeline, the source state starts as active, destination emphasis appears progressively, and final destination active-state fill is committed at animation end.
- During this timeline, all non-focused/de-emphasized edges and nodes remain de-emphasized; only the accepted transition path and source→destination state handoff animate.
- Pre-click semantic highlights (including red alternatives/reject paths for the fired event) fade out over the timeline instead of disappearing instantaneously.
- On hover, the actual selected destination is full-bright (matching current-state emphasis) but remains hollow; alternate/blocked destinations remain dimmer.
- On click/transition, destination nodes retain their frame-0 hover intensity and then animate smoothly into committed current-state fill at timeline end.
- Transition visuals are layered for smoother motion: soft beam halo + core beam + moving sweep + runner trail, with subtle source-collapse/destination-arrival handoff rings.

Animation contract (simplified):
1. Hover phase: selected event semantics are emphasized; non-selected semantics are de-emphasized.
2. Click/transition phase: freeze and fade pre-click semantics, animate only accepted path + runner, keep destination dim, suppress hover.
3. Commit phase: destination becomes the current state, normal hover-driven semantics resume.
- `Reload` resets the mockup session to the initial sample state/data and clears transient UI state (hover, args, toasts, and active animations).
- `Reload` uses event-button-like affordances (`hover` emphasis and short pressed click feedback) so invocation is visually confirmed.
- Current-state outgoing transitions are always semantically visible.
- Edge and node emphasis model:
  - At rest: semantic visuals are muted.
  - On hover: hovered-event paths get slight emphasis; non-hovered paths are strongly muted.
  - Destination node outlines are green/red by evaluated outcome.
  - Destination labels use matching semantic colors (green/red) and the same muting intensity behavior.
  - Current state remains solid-filled; only hovered event’s actual destination becomes solid green.
- Chips/sidecars follow the same semantic color + muting model as edges/outlines.
- Arrowheads and stroke widths are normalized (fixed-size markers, tight width range) to reduce visual jitter.
- Diagram remains read-first; chips/lines are visual affordances rather than required click targets.
- Resize behavior:
  - Main pane uses a single stack: diagram (with in-canvas data lane) above events.
  - Diagram and in-canvas data no longer use a shared outer panel wrapper.
  - Data lane also renders without its own inner panel wrapper.
  - Diagram keeps stable geometry and uses horizontal scrolling fallback when width is constrained.
  - On narrower widths, the in-canvas data lane moves below the diagram area while staying inside the canvas.
  - Data panel scrolls internally instead of forcing page growth.
  - Vertical layout is viewport-bounded; the shell flexes to available height.
  - Diagram height and dock internals shrink to defined floors at short-height breakpoints before collapsing layout.
  - Short-height breakpoints compact spacing/typography for event rows and controls.

## Minimal Command Contract (future build)

- `StartSession(filePath, instancePath?)`
- `ReloadMachine()`
- `Inspect(eventName, args)`
- `Fire(eventName, args)`
- `GetSnapshot()`
- `ResetInstance()`
- `SaveInstance(path?)`

## MVP Boundaries

- Single active session per workspace.
- Sidecar instance default: `<machine>.instance.json`.
- No graph visualization in MVP.
- No notebooks/webviews beyond one inspector panel.
