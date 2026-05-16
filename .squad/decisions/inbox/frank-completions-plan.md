# Implementation Plan: Catalog-Driven Completions Refactor

**Author:** Frank (Lead Architect)  
**Date:** 2026-05-16T09:00:00-04:00  
**Status:** Executing (Slice 1 in progress)  
**Decision source:** `.squad/decisions/inbox/frank-completions-architecture.md` — Option (b) greenlit by Shane.

---

## Overview

Replace the procedural `SlotContext` routing layer in completions with a catalog-driven `SlotPositionResolver` that reads construct slot metadata to determine cursor position and vocabulary. Three slices, each independently testable.

---

## Slice 1 — Enrich `ConstructSlotMeta` with Completion Vocabulary Metadata

**Status:** THIS SESSION  
**Goal:** Every `ConstructSlot` instance declares what vocabulary populates it, whether it's a list, and what token introduces continuation items. Purely additive — no consumer changes.

### New Types

**File:** `src/Precept/Language/ConstructSlot.cs`

```csharp
/// <summary>
/// Declares what completion vocabulary a slot offers.
/// Drives CompletionHandler dispatch once SlotPositionResolver ships (Slice 3).
/// </summary>
public enum SlotVocabulary
{
    /// <summary>No completions (identifier slots, because-clause text, initial marker).</summary>
    None = 0,
    /// <summary>Declared state names from the semantic index.</summary>
    StateNames = 1,
    /// <summary>Declared event names from the semantic index.</summary>
    EventNames = 2,
    /// <summary>Declared field names from the semantic index.</summary>
    FieldNames = 3,
    /// <summary>Action verb keywords from the Actions catalog.</summary>
    ActionVerbs = 4,
    /// <summary>Type keywords from the Types catalog.</summary>
    TypeKeywords = 5,
    /// <summary>Modifier keywords (context-sensitive to construct's ModifierDomain).</summary>
    Modifiers = 6,
    /// <summary>Expression context: field refs, functions, literals, operators.</summary>
    Expression = 7,
    /// <summary>Top-level construct keywords.</summary>
    TopLevel = 8,
    /// <summary>Outcome keywords (transition, no transition).</summary>
    OutcomeKeywords = 9,
    /// <summary>Access mode keywords (readonly, editable).</summary>
    AccessModes = 10,
    /// <summary>State entry names with optional modifiers (state declaration body).</summary>
    StateEntryNames = 11,
    /// <summary>Reject clause: string literal for refusal reason.</summary>
    RejectReason = 12,
}
```

### New Fields on `ConstructSlot`

**File:** `src/Precept/Language/ConstructSlot.cs`

Add three new properties to the existing record:

```csharp
public sealed record ConstructSlot(
    ConstructSlotKind Kind,
    bool              IsRequired = true,
    string?           Description = null,
    TokenKind[]?      TerminationTokens = null,
    // ── NEW: completion vocabulary metadata ──
    bool              IsList = false,
    bool              IsChainable = false,
    TokenKind?        ItemIntroducerToken = null,
    SlotVocabulary    Vocabulary = SlotVocabulary.None);
```

### Population — Slot Instance Updates

**File:** `src/Precept/Language/Constructs.cs` — update each `private static readonly ConstructSlot` instance.

| Slot Instance | IsList | IsChainable | ItemIntroducerToken | Vocabulary |
|---|---|---|---|---|
| `SlotIdentifierList` | true | false | Comma | None |
| `SlotTypeExpression` | false | false | null | TypeKeywords |
| `SlotModifierList` | true | false | null | Modifiers |
| `SlotStateEntryList` | true | false | Comma | StateEntryNames |
| `SlotArgumentList` | false | false | null | None |
| `SlotComputeExpression` | false | false | null | Expression |
| `SlotGuardClause` | false | false | null | Expression |
| `SlotPreVerbGuardEnsure` | false | false | null | Expression |
| `SlotPreVerbGuardArrow` | false | false | null | Expression |
| `SlotPreVerbGuardModify` | false | false | null | Expression |
| `SlotActionChain` | false | true | Arrow | ActionVerbs |
| `SlotOutcome` | false | false | null | OutcomeKeywords |
| `SlotStateTarget` | true | false | Comma | StateNames |
| `SlotOptStateTarget` | true | false | Comma | StateNames |
| `SlotEventTarget` | false | false | null | EventNames |
| `SlotEnsureClause` | false | false | null | Expression |
| `SlotBecauseClause` | false | false | null | None |
| `SlotOptBecauseClause` | false | false | null | None |
| `SlotAccessModeKeyword` | false | false | null | AccessModes |
| `SlotFieldTarget` | true | false | Comma | FieldNames |
| `SlotRuleExpression` | false | false | null | Expression |
| `SlotInitialMarker` | false | false | null | None |
| `SlotRejectClause` | false | false | null | RejectReason |
| `SlotSuccessOutcome` | false | false | null | OutcomeKeywords |

### Tests

**File:** `test/Precept.Tests/Language/SlotVocabularyMetadataTests.cs` (new file)

- Assert `SlotStateTarget` → IsList=true, Vocabulary=StateNames, ItemIntroducerToken=Comma
- Assert `SlotActionChain` → IsChainable=true, ItemIntroducerToken=Arrow, Vocabulary=ActionVerbs
- Assert `SlotTypeExpression` → Vocabulary=TypeKeywords, IsList=false
- Assert `SlotModifierList` → IsList=true, Vocabulary=Modifiers
- Assert `SlotFieldTarget` → IsList=true, Vocabulary=FieldNames, ItemIntroducerToken=Comma
- Assert `SlotEventTarget` → Vocabulary=EventNames, IsList=false
- Assert `SlotGuardClause` → Vocabulary=Expression
- Assert `SlotOutcome` → Vocabulary=OutcomeKeywords
- Assert `SlotRejectClause` → Vocabulary=RejectReason
- Assert `SlotSuccessOutcome` → Vocabulary=OutcomeKeywords
- Parameterized test: every slot with `Vocabulary != None` is covered
- Parameterized test: every slot with `IsList=true` has `ItemIntroducerToken != null`

### Verification

```bash
dotnet build        # additive — no breaks
dotnet test         # all existing tests pass + new tests green
```

---

## Slice 2 — Build `SlotPositionResolver` as a Parallel Path

**Status:** NEXT SESSION  
**Depends on:** Slice 1 (needs `IsList`, `IsChainable`, `ItemIntroducerToken`, `Vocabulary` on slots)

### Class Location & Signature

**File:** `tools/Precept.LanguageServer/SlotPositionResolver.cs` (new file)

```csharp
namespace Precept.LanguageServer;

/// <summary>
/// Catalog-driven cursor position resolver.
/// Determines which construct/slot/phase the cursor occupies
/// by reading ConstructMeta.Slots metadata.
/// </summary>
internal static class SlotPositionResolver
{
    /// <summary>
    /// Resolve the cursor position to a structural slot location.
    /// Returns null if the cursor is outside any known construct slot
    /// (treated as TopLevel by CompletionHandler).
    /// </summary>
    internal static ResolvedSlotPosition? Resolve(Compilation compilation, Position position);
}

internal readonly record struct ResolvedSlotPosition(
    ConstructKind    Construct,
    ConstructSlotKind SlotKind,
    SlotPhase        Phase);

internal enum SlotPhase
{
    /// <summary>Cursor after the slot's introducer keyword.</summary>
    LeadingToken,
    /// <summary>Cursor after a comma in a list-capable slot.</summary>
    InList,
    /// <summary>Cursor after an arrow in a chain-capable slot.</summary>
    InChain,
    /// <summary>Slot is complete; cursor between this slot and the next.</summary>
    AfterSlot,
    /// <summary>Inside an expression-bearing slot (guard, ensure, compute, rule).</summary>
    InExpression,
}
```

### Resolution Algorithm

1. **Find enclosing construct:** Walk the parsed construct tree to find which `ConstructKind` the cursor line belongs to (reuse existing `GetRelevantConstruct` logic from `SlotContextResolver`).

2. **Walk `ConstructMeta.Slots` in order:** For each slot in `GetMeta(construct).Slots`, determine the token span that slot occupies (using slot leading tokens and `TerminationTokens`). If the cursor falls within that span, the slot is identified.

3. **Determine `SlotPhase`:**
   - If the previous significant token is a comma AND `slot.IsList` → `SlotPhase.InList`
   - If the previous significant token is an arrow AND `slot.IsChainable` → `SlotPhase.InChain`
   - If the previous significant token is a `TerminationToken` of the current slot → `SlotPhase.AfterSlot` (advance to next slot)
   - If the slot's `Vocabulary` is `Expression` → `SlotPhase.InExpression`
   - Otherwise → `SlotPhase.LeadingToken`

4. **List-continuation handling (the first bug class):** When `slot.IsList && previousToken.Kind == TokenKind.Comma`, return `Phase=InList` with the slot's `Vocabulary`. This eliminates all `TryGet*ListContinuationToken` methods generically.

5. **Chain-continuation handling (the second bug class):** When `slot.IsChainable && previousToken.Kind == slot.ItemIntroducerToken`, return `Phase=InChain` with the slot's `Vocabulary`. This eliminates all `IsImplicit*ContinuationPosition` methods generically.

### Shadow-Run Validation Strategy

**File:** `test/Precept.LanguageServer.Tests/SlotPositionResolverTests.cs` (new file)

Strategy: Run both `SlotContextResolver.GetCursorContext()` and `SlotPositionResolver.Resolve()` against the same (compilation, position) pairs. Assert equivalence under a defined mapping:

```csharp
private static SlotContext? MapToLegacyContext(ResolvedSlotPosition? pos)
{
    if (pos is null) return SlotContext.TopLevel;
    return (pos.Value.SlotKind, pos.Value.Phase) switch
    {
        (ConstructSlotKind.StateTarget, SlotPhase.LeadingToken or SlotPhase.InList) => SlotContext.InStateTarget,
        (ConstructSlotKind.StateTarget, SlotPhase.AfterSlot) => SlotContext.AfterStateTarget,
        (ConstructSlotKind.EventTarget, SlotPhase.LeadingToken) => SlotContext.InEventTarget,
        (ConstructSlotKind.EventTarget, SlotPhase.AfterSlot) => SlotContext.AfterEventTarget,
        (ConstructSlotKind.FieldTarget, _) => SlotContext.InFieldTarget,
        (ConstructSlotKind.ActionChain, _) => SlotContext.InActionVerb,
        (ConstructSlotKind.TypeExpression, _) => SlotContext.InTypePosition,
        (ConstructSlotKind.ModifierList, _) => SlotContext.InModifierPosition,
        (_, SlotPhase.InExpression) => SlotContext.InExpression,
        (ConstructSlotKind.StateEntryList, _) => SlotContext.InStateDeclarationName,
        _ => SlotContext.AfterKeyword,
    };
}
```

Test sources:
- Extract all cursor positions from existing completion integration tests
- Generate synthetic positions at every token boundary in the `samples/` files
- Assert `MapToLegacyContext(Resolve(...)) == GetCursorContext(...)` for each position

**Known divergences to document (not bugs — improvements):**
- List continuations that currently fall through to `TopLevel` should resolve to the correct slot
- Chain continuations that currently fall through should resolve to `ActionChain`
- These are intentional fixes (the whole point of the refactor)

### Tests

- 30+ position assertions covering all `SlotContext` values
- Parameterized test running both resolvers against sample files
- Explicit tests for list-continuation and chain-continuation positions (the bug class)

---

## Slice 3 — Cut Over and Delete `SlotContext`

**Status:** NEXT SESSION  
**Depends on:** Slice 2 (needs `SlotPositionResolver` validated against existing behavior)

### Swap `CompletionHandler` Dispatch

**File:** `tools/Precept.LanguageServer/CompletionHandler.cs`

Replace:
```csharp
var context = SlotContextResolver.GetCursorContext(compilation, position);
return context switch { ... };
```

With:
```csharp
var slotPosition = SlotPositionResolver.Resolve(compilation, position);
if (slotPosition is null)
    return GetTopLevelItems(compilation);

var slot = GetSlotMeta(slotPosition.Value);
return slot.Vocabulary switch
{
    SlotVocabulary.StateNames      => GetStateTargetItems(compilation, slotPosition.Value.Phase),
    SlotVocabulary.EventNames      => GetEventItems(compilation),
    SlotVocabulary.FieldNames      => GetFieldTargetItems(compilation, slotPosition.Value.Phase),
    SlotVocabulary.ActionVerbs     => GetActionItems(compilation, slotPosition.Value.Phase),
    SlotVocabulary.TypeKeywords    => GetTypeItems(),
    SlotVocabulary.Modifiers       => GetModifierItems(compilation, slotPosition.Value),
    SlotVocabulary.Expression      => GetExpressionItems(compilation, position),
    SlotVocabulary.OutcomeKeywords => GetOutcomeItems(compilation),
    SlotVocabulary.AccessModes     => GetAccessModeItems(),
    SlotVocabulary.StateEntryNames => GetStateEntryItems(compilation),
    SlotVocabulary.RejectReason    => [], // string literal — no vocabulary completions
    SlotVocabulary.None            => [],
    _ => [],
};
```

### Helper Method

```csharp
private static ConstructSlot GetSlotMeta(ResolvedSlotPosition pos)
{
    var meta = Constructs.GetMeta(pos.Construct);
    return meta.Slots.First(s => s.Kind == pos.SlotKind);
}
```

### Deletion List

| File | What to delete |
|---|---|
| `tools/Precept.LanguageServer/SlotContext.cs` | `enum SlotContext` (entire enum, 16 values) |
| `tools/Precept.LanguageServer/SlotContext.cs` | `class SlotContextResolver` (all ~1,400 lines) |
| Inline in `SlotContextResolver` | `TryGetStateTargetListContinuationToken` |
| Inline in `SlotContextResolver` | `TryGetFieldTargetListContinuationToken` |
| Inline in `SlotContextResolver` | `TryGetEventTargetContext` |
| Inline in `SlotContextResolver` | `IsImplicitActionContinuationPosition` |
| Inline in `SlotContextResolver` | `IsImplicitChainContinuationPosition` |
| Inline in `SlotContextResolver` | `GetPostStateTargetContext` |
| Inline in `SlotContextResolver` | `GetPostEventTargetContext` |
| Inline in `SlotContextResolver` | ~8 more `TryGet*`/`Is*` private methods |

The entire `SlotContext.cs` file is deleted. `CompletionHandler` import of `SlotContext` is removed.

### Item Generator Consolidation

Several existing `Get*Items` methods in `CompletionHandler` remain but their signatures simplify:
- `GetStateTargetItems` no longer needs to detect "am I in a list continuation?" — it receives `SlotPhase.InList` directly
- `GetActionItems` no longer needs `IsAfterArrowInChain` detection — it receives `SlotPhase.InChain`
- After-slot items use the generic `GetNextSlotIntroducerItems()` helper

### Tests

- **Regression suite:** Run the full existing completion test suite (all tests in `test/Precept.LanguageServer.Tests/` that exercise completions). Zero failures = ship.
- **Delete shadow-run tests:** The Slice 2 parallel-validation tests can be removed or kept as documentation.
- **New generic tests:** Test `GetNextSlotIntroducerItems()` for construct `TransitionRow` at `AfterStateTarget` → expects `on`, `when`, `->` keyword items.

---

## Dependency Graph

```
Slice 1 (catalog metadata)
    ↓
Slice 2 (parallel resolver)
    ↓
Slice 3 (cut over + delete)
```

Each slice is independently committable and testable. Slice 2 can validate without modifying live behavior. Slice 3 is a clean swap with immediate regression detection.

---

## Risk Mitigations

| Risk | Mitigation |
|---|---|
| Resolver doesn't handle error-recovery (incomplete constructs) | Slice 2 falls back to `null` (→ TopLevel) for unresolvable positions; shadow-run reveals mismatches |
| Expression sub-positions (set assignment, arg default) lose context | Slice 3 preserves `InExpression` phase; deeper sub-position detection remains in `GetExpressionItems` |
| New grammar additions between slices | Any new construct/slot added between slices must populate the new fields — enforced by the "all slots have vocabulary" invariant test |
