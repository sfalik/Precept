# Kramer B2/B3 Complete

**Date:** 2026-05-12T19:26:05.9065969-04:00
**Owner:** Kramer
**Requester:** Shane
**Scope:** B2 construct routing fix + B3 mutability honesty

---

## What shipped

- `tools/Precept.LanguageServer/Handlers/HoverHandler.cs`
  - State identifiers now route to the rich state hover before row/constraint construct hovers can mask them.
  - Rich construct routing now runs before generic operator/function/accessor help, so construct cards win where §4 requires.
  - Identifier fallback now reuses the rich field/state/event/arg cards instead of the older generic identifier markdown.

- `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs`
  - Added shared rich hover constructors for field/state/event/arg symbol paths so routing stays consistent.
  - Field mutability summaries now render `✏️` for unconditional writable states and `🔒` for locked / structurally absent states.
  - Guarded access declarations are excluded from V1 writable summaries; omit states still land in the locked bucket via the state complement.
  - State-card writable summaries also ignore guarded access entries for the same V1 honesty reason.

- `test/Precept.LanguageServer.Tests/HoverHandlerTests.cs`
  - Added regression coverage for state-reference routing to the rich state card.
  - Locked reject-over-transition and qualifier-over-symbol precedence explicitly.
  - Added guarded-access mutability coverage and updated routing expectations for generic token-help interactions.

---

## Validation

- `dotnet test test/Precept.LanguageServer.Tests/ --nologo` ✅ (`271/271`)
- `dotnet build tools/Precept.LanguageServer/Precept.LanguageServer.csproj --artifacts-path temp/dev-language-server --nologo` ✅

---

## Notes

- No `src/Precept/` files were modified.
- B1 proof-card wording / badge vocabulary was left intact.
- B4 state missing-path narrative remains deferred; this pass only fixed routing and mutability honesty.
