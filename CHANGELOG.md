# Changelog

All notable changes to the Precept project will be documented in this file.

---

## [Unreleased] — spike/Precept-V2-Radical

### Added

#### Constructor Semantics (Slices 1–12)

**Language surface:**
- `event <Name> initial` — declares an event as the entity's construction trigger. Only one event per precept may carry the `initial` modifier.
- `on <InitialEvent> [when <guard>] -> actions` — construction rows. Set initial field values at entity creation. No `from <State>` prefix (entity does not yet exist in a state). No `transition` or `no transition` (grammar structurally excludes them).
- `on <InitialEvent> [when <guard>] -> reject "reason"` — construction rejection rows. Refuse entity creation with an authored reason.
- Guards on construction rows enable conditional intake routing (first-match semantics).

**Runtime API:**
- `Precept.Create(JsonElement? args)` / `Precept.Create(Action<IArgBuilder>? args)` — construction entry point. Fires the initial event atomically and returns `EventOutcome`.
- `EventOutcome.Created(Version Result, FiredArgs Args)` — new DU case for successful construction. Semantically distinct from `Applied` (which is for mutations on existing entities).
- `Precept.InspectCreate(args?)` — progressive inspection of construction (same model as `InspectFire`).
- `Precept.InitialEvent` / `Precept.InitialState` — definition-level discovery.
- Fire-once guarantee: `Version.Fire(initialEventName)` returns `UndefinedEvent` post-construction. `Version.AvailableEvents` excludes the initial event.
- `FiredArgs.Empty` — canonical no-args sentinel.

**Compiler enforcement:**
- `InitialEventInTransitionRow` — error if initial event appears in `from State on Event` rows.
- `ZeroConstructionRows` — error if initial event has no construction rows.
- `MultipleInitialEvents` — error if more than one event is marked `initial`.
- `InitialEventMissingAssignments` — error if construction rows don't assign all required fields.
- `AlwaysRejecting` promoted to Error for initial events (unconstructible precept).

**Language server:**
- `initial` modifier completions after event declarations.
- Hover text explaining the `initial` modifier semantics.
- Semantic token classification for `initial` in event declaration position.
- Grammar generator updated for `event ... initial` highlighting.

**MCP tooling:**
- `isConstruction: bool` added to `CompileEventRowDto` in `precept_compile` output.

**Documentation:**
- `docs/language/precept-language-spec.md` — construction row syntax and semantics (§3A.5).
- `docs/runtime/runtime-api.md` — full `Create()` API documentation.
- `docs/Working/constructor-semantics.md` — consolidated design record.
