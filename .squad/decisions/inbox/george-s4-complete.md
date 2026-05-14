# Slice 4 Complete — PRE0092 EventHandlerInStatefulPrecept

**By:** George
**Date:** 2025-07-14

## What was wired

Added PRE0092 emission in `TypeChecker.Validation.Structural.cs` → `ValidateStructural`. The check fires when a precept has both `state` declarations (`ctx.States.Count > 0`) and event handlers (`ctx.EventHandlers.Count > 0`), emitting one diagnostic per handler with the handler's event name in the message.

## Tests added

Three tests in `TypeCheckerStructuralTests.cs`:
1. `EventHandler_InStatefulPrecept_EmitsEventHandlerInStatefulPrecept` — single handler in stateful precept fires PRE0092
2. `EventHandler_InStatelessPrecept_NoDiagnostic` — handler in stateless precept is clean
3. `EventHandler_MultipleHandlers_InStatefulPrecept_EmitsForEach` — two handlers produce two diagnostics

## Anomalies

- **Design spec uses `ctx.Emit(...)`** but the actual `CheckContext` API is `ctx.Diagnostics.Add(...)`. Used the real API.
- **Allow-list file (`DiagnosticCoverageAllowLists.cs`) does not exist yet** (Slice 0 hasn't shipped). Added a `// TODO(allow-list)` comment at the emission site per task instructions.
- **No regression impact on samples.** The `on Event ensure ...` constructs in sample files are *not* `EventHandler` constructs — they're ensure/because clauses (a different `ConstructKind`). Only `on Event -> action` patterns are event handlers, and no sample file uses that pattern in a stateful precept.

## Validation

- `dotnet build` — clean (0 warnings, 0 errors)
- `dotnet test test/Precept.Tests/` — 5370 passed, 0 failed
