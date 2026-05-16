# Decision: Slice 12 Complete — Constructor Semantics Docs

**Date:** 2026-05-16  
**Author:** Frank (Lead/Architect)  
**Status:** Done

## What Was Updated

1. **`docs/language/precept-language-spec.md`** — §3A.2 construction outcomes rewritten (removed stale claim that `Transitioned`/`Applied` are valid construction outcomes; now reflects structural exclusion). §3A.5 expanded with: event declaration `initial` modifier syntax, construction row syntax (`on <Event> [when guard] -> actions`), construction vs transition row comparison table, fire-once guarantee section.

2. **`docs/runtime/runtime-api.md`** — Already comprehensive from prior slices. No changes needed — `Create()`, `EventOutcome.Created`, `InitialEvent`, `InitialState`, fire-once enforcement, and `FiredArgs.Empty` were all documented during Slice 8 implementation.

3. **`CHANGELOG.md`** — Created at repo root. Covers language surface, runtime API, compiler enforcement, language server, and MCP tooling.

4. **`docs/Working/constructor-semantics.md`** — Slice summary table updated: all 12 slices (including 8b) marked ✅ Done with completion date 2026-05-16.

5. **`samples/Test.precept`** — Verified compiles clean with current local build (0 diagnostics). No other samples use construction rows — only Test.precept declares `event create initial` + `on create` rows.

## Gaps Found

- **MCP tool stale build:** The `precept_compile` MCP tool reports errors for Test.precept because it's running an older build that doesn't include the Slice 8b changes. The local `dotnet build` + test suite (84 construction tests, 6480 total) is green. This is an ops issue, not a code issue — the MCP server needs a rebuild.

- **Language spec §2.2 (stateless event hook grammar)** still says handlers don't support `when` guards. This is technically still accurate for non-construction stateless event handlers (PRE0014 remains in the diagnostic catalog for those). The construction row guard support is documented separately in §3A.5. No conflict.

## Implementation vs Design Doc

The implementation matches the design doc. Key verification points:
- `EventOutcome.Created(Version Result, FiredArgs Args)` — matches spec
- `Precept.Create(JsonElement? args = null)` — matches spec (typed lane throws NotImplementedException, as expected at spike level)
- Fire-once: `Version.Fire()` returns `UndefinedEvent` for initial events — confirmed in `Version.cs`
- `AvailableEvents` excludes initial event — confirmed in `Version.cs`
- Construction row classification via `resolvedEvent.IsInitial` in type checker — confirmed in `TypeChecker.cs:1160`
- `initial` only on event DECLARATIONS, never on rows (post-8b) — confirmed
