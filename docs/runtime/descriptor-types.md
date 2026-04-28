# Descriptor Types

## Status

| Property | Value |
|---|---|
| Doc maturity | Stub |
| Implementation state | `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `FaultSiteDescriptor` defined in `Descriptors.cs`; `ConstraintDescriptor` in `SharedTypes.cs`. Builder population pending (R3/R6). |
| Source | `src/Precept/Runtime/Descriptors.cs`, `src/Precept/Runtime/SharedTypes.cs` |
| Upstream | Precept Builder (constructs descriptors), SemanticIndex (source shapes) |
| Downstream | All runtime operations, Precept structural query API, Version API, MCP DTOs |

---

## Overview

Descriptor types are the runtime face of declarations. They represent declared program elements (fields, states, events, args, constraints, fault sites) as first-class sealed types — not string aliases. Every runtime API surface routes through descriptor identity. Descriptors are produced by the Precept Builder from `SemanticIndex` declarations and survive in the `Precept` executable model.

---

## Responsibilities and Boundaries

**OWNS:** Identity representation for all declared program elements; structural query return types; the data contract for descriptor-keyed API surfaces.

**Does NOT OWN:** Semantic resolution (TypeChecker produces `TypedXxx` shapes that the Builder converts); execution planning (builder domain); evaluation (evaluator domain).

---

## Right-Sizing

Descriptors are data — sealed records with no behavior. They are the bridge between compile-time identity and runtime execution. Keeping them as pure data types (no methods that encode runtime logic) maintains the boundary between analysis and execution. The descriptor types are intentionally minimal: only the fields that callers (evaluator, LS, MCP) actually need.

---

## Inputs and Outputs

**Input:** Precept Builder reads `SemanticIndex` typed declarations and resolves them into descriptor shapes.

**Output:** Descriptor instances stored in the `Precept` executable model; returned by structural query APIs (`Precept.Fields`, `Precept.States`, `Precept.Events`); used in outcome and inspection types.

---

## Production Lifecycle

Descriptors are produced in the Precept Builder's descriptor pass — the first transformation pass. Each `TypedField` in the `SemanticIndex` produces one `FieldDescriptor`; each `TypedState` produces one `StateDescriptor`; etc. Descriptors are immutable and shared across all evaluator calls — no per-operation allocation.

---

## Descriptor Shapes and Runtime API Integration

### Descriptor Shapes

```csharp
// Field descriptor — carries slot index for evaluator access
sealed record FieldDescriptor(
    string Name,
    TypeKind Type,
    int SlotIndex,
    IReadOnlyList<ModifierKind> Modifiers,
    string? DefaultExpression,
    bool IsComputed,
    int SourceLine);

// State descriptor — carries modifier set for dispatch decisions
sealed record StateDescriptor(
    string Name,
    IReadOnlyList<ModifierKind> Modifiers,
    int SourceLine);

// Event descriptor — carries arg list for arg resolution
sealed record EventDescriptor(
    string Name,
    IReadOnlyList<ModifierKind> Modifiers,
    IReadOnlyList<ArgDescriptor> Args,
    int SourceLine);

// Arg descriptor — carries type and optionality for validation
sealed record ArgDescriptor(
    string Name,
    TypeKind Type,
    bool IsOptional,
    string? DefaultExpression,
    int SourceLine);

// FaultSiteDescriptor — planted by builder, read by evaluator backstops
sealed record FaultSiteDescriptor(
    FaultCode FaultCode,
    DiagnosticCode PreventedBy,  // derived from [StaticallyPreventable] at Precept Builder time
    int SourceLine);
```

`ConstraintDescriptor` exists in `SharedTypes.cs`: expression text, `ConstraintKind` anchor, `because` text, guard metadata, source lines, scope targets. `ReferencedFields` is currently a provisional flat string list; a typed target hierarchy (TODO G1/G9) will replace it when the constraint evaluation attribution model is implemented.

### API Surfaces Using Descriptors

All structural query surfaces now return typed descriptors:

- `Precept.States` → `IReadOnlyList<StateDescriptor>`
- `Precept.Fields` → `IReadOnlyList<FieldDescriptor>`
- `Precept.Events` → `IReadOnlyList<EventDescriptor>`
- `Precept.InitialState` → `StateDescriptor?`
- `Precept.InitialEvent` → `EventDescriptor?`
- `Version.AvailableEvents` → `IReadOnlyList<EventDescriptor>`
- `Version.RequiredArgs(EventDescriptor)` → `IReadOnlyList<ArgDescriptor>`
- `SharedTypes.FieldAccessInfo` → `FieldDescriptor Field` (replaces string name + type)

String-keyed `Fire`, `Update`, `InspectFire`, `InspectUpdate` remain string-keyed until the Evaluator is implemented (R3/R5 work).

### MCP DTO Updates

MCP DTOs in `tools/Precept.Mcp/Tools/` currently use string representations. These will need updating when the evaluator is wired and descriptors are populated by the builder.

---

## Dependencies and Integration Points

- **Precept Builder** (upstream): constructs descriptors from `SemanticIndex` typed declarations
- **SemanticIndex** (upstream via Builder): `TypedField`, `TypedState`, `TypedEvent`, `TypedArg` → descriptor shapes
- **Evaluator** (downstream): reads `FieldDescriptor.SlotIndex` for slot array access; reads `StateDescriptor.Modifiers` for dispatch; uses `FaultSiteDescriptor` for backstop routing
- **Precept structural query API** (downstream): `Precept.Fields`, `Precept.States`, `Precept.Events` return descriptor lists
- **Version API** (downstream): `Version.AvailableEvents` returns `EventDescriptor` list; `RequiredArgs` returns `ArgDescriptor` list
- **MCP DTOs** (downstream): serialized descriptor content exposed via MCP tools

---

## Failure Modes and Recovery

Descriptor types are pure data — no failure modes. Descriptor construction failures are builder failures (see `docs/runtime/precept-builder.md`). Descriptor lists are populated in the builder pass; if a declaration has no descriptor equivalent, it is a builder bug, not a descriptor type bug.

---

## Contracts and Guarantees

- Every descriptor is a sealed immutable record — no mutation after construction.
- Every `FieldDescriptor` has a unique `SlotIndex` within `[0, FieldCount)`.
- Every `FaultSiteDescriptor.PreventedBy` corresponds to the `DiagnosticCode` in the `[StaticallyPreventable]` attribute of `FaultSiteDescriptor.FaultCode`.
- `ConstraintDescriptor` exists and is stable — do not change its shape without updating all consumers.

---

## Design Rationale and Decisions

Descriptors are the "no string aliasing" principle applied at the runtime API boundary. String-keyed APIs (e.g., `Fire("Submit", new { Approver = "Alice" })`) are brittle — typos, casing issues, and refactoring misses are all invisible until runtime. Descriptor-keyed APIs (e.g., `version.Fire(submitEvent, new { [approverArg] = "Alice" })`) make invalid calls unrepresentable. This is the structural guarantee applied to the runtime API itself.

---

## Innovation

- **First-class runtime identity:** Declarations become typed, named values rather than string tokens. This is the fundamental shift from "dynamic dispatch on strings" to "statically typed entity lifecycle management."
- **Descriptor-keyed API surfaces:** Every runtime API route goes through descriptor identity. This makes the API refactoring-safe and IDE-navigable.
- **`FaultSiteDescriptor` as the proof/runtime link:** The connection between compile-time proof obligations and runtime backstops is reified as a first-class type, not a comment or convention.

---

## Open Questions / Implementation Notes

1. `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `FaultSiteDescriptor` do NOT yet exist as types — only `ConstraintDescriptor` is defined.
2. Create `src/Precept/Runtime/Descriptors.cs` with all 5 missing sealed records.
3. Update `Precept.cs`: `States` → `IReadOnlyList<StateDescriptor>`, `Fields` → `IReadOnlyList<FieldDescriptor>`, `Events` → `IReadOnlyList<EventDescriptor>`, `InitialState` → `StateDescriptor?`, `InitialEvent` → `EventDescriptor?`.
4. Update `Version.cs`: `AvailableEvents` → `IReadOnlyList<EventDescriptor>`, `RequiredArgs(EventDescriptor)` → `IReadOnlyList<ArgDescriptor>`.
5. Update `SharedTypes.cs`: `FieldAccessInfo` — replace `string FieldName` + `string FieldType` with `FieldDescriptor Field`; remove `ArgInfo` (replaced by `ArgDescriptor`).
6. Update MCP DTOs in `tools/Precept.Mcp/Tools/` once descriptors are defined.

---

## Deliberate Exclusions

- **No behavior on descriptor types:** They are pure data records.
- **No semantic resolution:** Descriptors represent post-resolution facts, not resolution inputs.

---

## Cross-References

| Topic | Document |
|---|---|
| Descriptor type shapes in the Precept Builder | `docs/compiler-and-runtime-design.md §10` |
| Precept Builder constructs descriptors from SemanticIndex | `docs/runtime/precept-builder.md` |
| Runtime API surfaces that use descriptors | `docs/runtime/runtime-api.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `src/Precept/Runtime/Descriptors.cs` | Planned — all 5 missing descriptor sealed records |
| `src/Precept/Runtime/SharedTypes.cs` | Contains existing `ConstraintDescriptor` |
| `src/Precept/Runtime/Precept.cs` | Structural query surfaces to update to descriptor types |
| `src/Precept/Runtime/Version.cs` | API surfaces to update to descriptor types |
