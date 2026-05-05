### 2026-05-04T03:26:10Z: CC#25 Q7 dictionary convenience lane closed

**By:** Shane (via Copilot)

**Status:** Recorded from inbox closeout.

**Merged source:** `copilot-cc25-q7-dict-extension-obsolete.md`.

- `IReadOnlyDictionary<string, object?>` convenience overloads and extension methods are fully obsolete. They are not part of the main API, not a test-only helper lane, and not a future convenience surface.
- Wire-format callers use `JsonElement`; in-process typed callers use the fluent builders. No third ingress lane remains.
