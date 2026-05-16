# Slice 11 MCP DTO — Newman Decision Record

**Date:** 2026-05-16T09:08:43.445-04:00
**Author:** Newman (MCP/AI Dev)
**Branch:** spike/Precept-V2-Radical

---

## What Changed

### DTO shape change

`CompileResultDto` gained a new field:

```csharp
CompileEventRowDto[] EventHandlers
```

New DTO record added to `tools/Precept.Mcp/Dtos/CompileToolDtos.cs`:

```csharp
public sealed record CompileEventRowDto(
    string EventName,
    bool IsConstruction);
```

JSON surface (camelCase via Web serializer defaults):

```json
{
  "eventName": "Create",
  "isConstruction": true
}
```

### Mapping

`CompileTool.cs` gains `MapEventRow`:

```csharp
private static CompileEventRowDto MapEventRow(TypedEventRow row)
    => new(row.EventName, row.IsConstruction);
```

Called from `Compile()`:

```csharp
var eventHandlers = compilation.Semantics.EventHandlers.Select(MapEventRow).ToArray();
```

---

## DU Mapping

The core model uses a discriminated union for event rows:

- `TypedEventRow` — abstract base with `EventName`, `Guard`, `IsConstruction`
- `TypedEventRowSuccess : TypedEventRow` — carries `Actions`
- `TypedEventRowReject : TypedEventRow` — carries `RejectReason`

`IsConstruction` is defined on the **base** record, so the DTO mapping reads directly from the base without any DU dispatch. No pattern matching needed.

`IsConstruction` is set in the TypeChecker (`TypeChecker.cs` line 1160):
```csharp
bool isConstruction = resolvedEvent?.IsInitial ?? false;
```

This means:
- Construction rows (`event X initial` + `on X -> ...`) → `IsConstruction = true`
- Regular event rows (`on X -> ...` where `X` is not `initial`) → `IsConstruction = false`
- Both `ConstructionRow` (kind 19) and `EventRow` (kind 12) construct kinds are normalized into `TypedEventRow`; both correctly carry the flag.

---

## Drift Observed

None. `TypedEventRow.IsConstruction` was added in Slice 8b (commit `c72db9b0`) and was available for direct DTO mapping. No gaps between core model and existing DTOs found.

---

## Tests

| Test | Source | Assertion |
|---|---|---|
| `CompileTool_EventRow_IsConstruction_True` | `event Create initial` + `on Create -> set Count = 1` | `eventHandlers[0].isConstruction == true` |
| `CompileTool_EventRow_IsConstruction_False` | `event Increment` (no `initial`) + `on Increment -> set Count = 1` | `eventHandlers[0].isConstruction == false` |

`Compile_UsesExpectedJsonShape` updated to include `"eventHandlers"` in the expected field order.

All 46 MCP tests pass. 5781 core tests pass (pre-existing LanguageServer build errors are unrelated to this slice).
