# Implementation Plan: Typed Literal Autocomplete

**For:** Kramer
**Reviewed by:** Frank (Architect)
**UX Spec:** `docs/Working/elaine-typed-literal-autocomplete-ux.md`
**Review:** `.squad/decisions/inbox/frank-typed-literal-completion-review.md`

---

## Summary

Transform the typed-constant completion surface from "show examples + reused values for the expected type" to a slot-aware, type-driven, qualifier-filtered mini-mode. After this work, typing inside `'...'` always shows type-appropriate items, space triggers show enumerable vocabulary for the current slot position, compound temporal literals guide the author through the full `<number> <unit> [+ <number> <unit>]*` cycle, and qualifier-constrained fields hard-filter candidates to the declared qualifier values.

## Prerequisites

Read and understand:
- Elaine's UX spec: `docs/Working/elaine-typed-literal-autocomplete-ux.md`
- Frank's review: `.squad/decisions/inbox/frank-typed-literal-completion-review.md`
- Current handler: `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`
- Slot context: `tools/Precept.LanguageServer/SlotContext.cs`
- Type catalog: `src/Precept/Language/Types.cs` and `src/Precept/Language/Type.cs`
- Temporal units: `src/Precept/Language/Time/TemporalUnits.cs`
- Currency catalog: `src/Precept/Language/CurrencyCatalog.cs`
- Temporal quantity parser: `src/Precept/Language/Time/TemporalQuantityParser.cs`
- Lexer typed constant scanning: `src/Precept/Pipeline/Lexer.cs` lines 315–643
- Qualifier metadata: `src/Precept/Language/DeclaredQualifierMeta.cs`
- Semantic model: `src/Precept/Pipeline/SemanticIndex.cs` (TypedField, TypedArg)

## Ordering Constraints

Slices must be implemented in order. Slice 1 establishes the type-branching framework; Slice 2 adds slot detection that Slice 4 depends on; Slice 3 adds qualifier metadata threading; Slice 4 builds on Slices 2+3 for compound temporal; Slice 5 is tests throughout but has a final integration pass.

Each slice includes its own unit tests. Commit after each slice.

---

## Slice 1 — Type-Branching on `'` Trigger

**What:** Replace the current uniform `GetTypedConstantItems` with type-specific item generation. Currently (line 221–241), it returns `CollectByType(...)` + `ContentValidation.Examples` as flat `Constant`-kind items for every type identically. After this slice, the opening `'` shows type-appropriate items: boolean shows `true`/`false` as a closed set; temporal shows single-segment starters + compound examples; money shows full-literal examples; free-form types (`text`, `integer`, `decimal`) show nothing (or reused values only).

### Modify:

**`GetTypedConstantItems(Compilation, Position, SlotContext)` (~line 221) in `CompletionHandler.cs`:**

Change the method body to branch on `expectedType`:

```csharp
private static IEnumerable<CompletionItem> GetTypedConstantItems(
    Compilation compilation, Position position, SlotContext context)
{
    if (!TryGetExpectedTypedConstantType(compilation, position, context, out var expectedType))
        return [];

    return expectedType switch
    {
        TypeKind.Boolean => GetBooleanLiteralItems(),
        TypeKind.Duration or TypeKind.Period => GetTemporalLiteralItems(compilation, expectedType, position),
        TypeKind.Money => GetMoneyLiteralItems(compilation, expectedType, position),
        TypeKind.Date or TypeKind.Time or TypeKind.Instant or TypeKind.DateTime
            or TypeKind.ZonedDateTime or TypeKind.Timezone
            => GetStructuredExampleItems(compilation, expectedType),
        TypeKind.Currency => GetCurrencyItems(),
        TypeKind.UnitOfMeasure => GetUnitOfMeasureItems(),
        TypeKind.Dimension => GetDimensionItems(),
        TypeKind.Quantity => GetQuantityLiteralItems(compilation, expectedType, position),
        _ => GetFreeFormItems(compilation, expectedType),
    };
}
```

### Create (all in `CompletionHandler.cs`):

**`GetBooleanLiteralItems()` → `IEnumerable<CompletionItem>` (~5 lines):**
Return exactly two items: `true` and `false`, kind `Value`, detail `"boolean literal"`, sortGroup `TypedConstant`. No other items.

**`GetTemporalLiteralItems(Compilation, TypeKind, Position)` → `IEnumerable<CompletionItem>` (~20 lines):**
- Check if cursor is inside an existing typed constant token (use `IsInsideTypedConstantToken`). If yes, delegate to `GetTemporalSlotItems(...)` (Slice 2).
- If at opening quote (Phase 0): return single-segment starters from `TemporalUnits.AllEntries` (e.g., `"30 days"`, `"1 hour"`, `"2 weeks"`, `"6 months"`, `"1 year"`) plus compound examples from `ContentValidation.Examples` (which already include `"2 hours + 30 minutes"`). Use `CollectByType(...)` for reused values, concat, then `DistinctByLabel`.
- Kind: `Snippet` for examples, `Value` for reused values.
- Detail: `"temporal literal"` / `"example format"`.

**`GetMoneyLiteralItems(Compilation, TypeKind, Position)` → `IEnumerable<CompletionItem>` (~15 lines):**
- If at opening quote (Phase 0): return full-literal examples from `ContentValidation.Examples` (e.g., `"100 USD"`, `"50.25 EUR"`) plus `CollectByType(...)` reused values.
- If inside an existing token, delegate to `GetMoneySlotItems(...)` (Slice 2).
- Kind: `Snippet` for examples, `Value` for reused.

**`GetStructuredExampleItems(Compilation, TypeKind)` → `IEnumerable<CompletionItem>` (~10 lines):**
Returns `ContentValidation.Examples` + `CollectByType(...)` for types where examples are the right UX (date, time, instant, datetime, zoneddatetime, timezone). Same pattern as current code but with appropriate `Kind` and `Detail`.

**`GetCurrencyItems()` → `IEnumerable<CompletionItem>` (~8 lines):**
Return all codes from `CurrencyCatalog.All.Keys`, kind `Unit`, detail `"ISO 4217 currency code"`.

**`GetUnitOfMeasureItems()` → `IEnumerable<CompletionItem>` (~5 lines):**
Return examples from `ContentValidation.Examples`, kind `Unit`, detail `"UCUM unit"`.

**`GetDimensionItems()` → `IEnumerable<CompletionItem>` (~5 lines):**
Return all from `DimensionCatalog.All.Keys`, kind `Unit`, detail `"dimension family"`.

**`GetQuantityLiteralItems(Compilation, TypeKind, Position)` → `IEnumerable<CompletionItem>` (~10 lines):**
Like `GetMoneyLiteralItems` but for quantity. Phase 0: examples + reused. Inside token: delegate to slot detection (Slice 2).

**`GetFreeFormItems(Compilation, TypeKind)` → `IEnumerable<CompletionItem>` (~8 lines):**
For `text`, `integer`, `decimal`, and anything else: return `CollectByType(...)` reused values only. If empty, return `ContentValidation?.Examples ?? []` as `Snippet` items. For `text`, prefer returning empty (no auto-popup noise).

### Modify `CompletionSortGroup` enum (~line 980):

Add `TypedConstantSegment = 3` between `TypedConstant` and `Type` for segment items (temporal units, money codes).

### Tests (in `CompletionHandlerTests.cs`):

- `TypedConstant_Boolean_ShowsTrueAndFalse` — `[Fact]`: trigger `'` on `field Active as boolean default ¦`, expect exactly `["true", "false"]`
- `TypedConstant_Temporal_ShowsStarters` — `[Fact]`: trigger `'` on `field Delay as duration default ¦`, expect items containing `"72 hours"` and `"2 hours + 30 minutes"` (from ContentValidation.Examples)
- `TypedConstant_Money_ShowsExamples` — `[Fact]`: trigger `'` on `field Cost as money default ¦`, expect `"100 USD"` and `"50.25 EUR"`
- `TypedConstant_Text_NoAutoPopup` — `[Fact]`: trigger `'` on `field Name as text default ¦`, expect empty or reused-only
- `TypedConstant_Currency_ShowsAllCodes` — `[Fact]`: trigger `'` on `field BaseCurrency as currency default ¦`, expect `"USD"`, `"EUR"`, `"GBP"` present

### Regression Anchors:
- `Completions_TypedConstant_UseTypeExamples` — must still pass (date examples)
- `Completions_TypedConstant_SuggestPreviouslyUsedDocumentValues` — must still pass
- `Completions_TypedConstant_NoExpectedType_ReturnsEmpty` — must still pass
- `Completions_TypedConstant_RuleComparison_UsesPeerOperandType` — must still pass
- `Completions_TypedConstant_InvokedInsideEmptyDefaultLiteral_UsesTypedConstantValues` — must still pass
- `Completions_TypedConstant_InvokedInsideEmptyExpressionLiteral_UsesTypedConstantValues` — must still pass
- `Completions_TypedConstant_NoKeywordsInsideTypedConstantSpan` — must still pass

**Files:** `CompletionHandler.cs`

- [ ] Branch on expectedType in GetTypedConstantItems
- [ ] Create per-type item generators
- [ ] Add CompletionSortGroup.TypedConstantSegment
- [ ] Tests pass including all regression anchors

---

## Slice 2 — Space Trigger Slot Detection

**What:** When space fires inside a typed constant, detect the cursor's slot position within the token text to show the right vocabulary. Currently space inside a typed constant falls through to the same `GetTypedConstantItems` which shows full-literal examples — wrong for `'100 |` where the user needs money codes, not full examples.

### Key Insight: Lexer Model

The lexer emits `'2 hours + 30 minutes'` as a **single** `TypedConstant` token with raw text `2 hours + 30 minutes`. The `+` is part of the token text, not a separate operator. This means slot detection must:

1. Find the typed constant token under the cursor
2. Compute the cursor's character offset within the token's raw text
3. Parse the text prefix up to the cursor to determine the current phase

### Create (in `CompletionHandler.cs`):

**`TryGetTypedConstantSlotPhase(ImmutableArray<Token> tokens, Position position, out TypedConstantPhase phase, out string textBeforeCursor)` → `bool` (~25 lines):**

```csharp
private enum TypedConstantPhase
{
    Empty,           // '' or just after '
    NumberTyping,    // '3  or '100  (digits with no space yet)
    AfterNumberSpace,// '3 | or '100 | (number then space — show vocabulary)
    UnitTyping,      // '3 d| (partial unit — filter vocabulary)
    SegmentComplete, // '3 days| (full segment — show + continuation)
    AfterPlus,       // '3 days + | (after + — number expected, no popup)
    AfterPlusNumber, // '3 days + 30| (number after + — no popup)
    AfterPlusNumberSpace, // '3 days + 30 | (show vocabulary again)
}
```

Logic:
1. Find the TypedConstant token containing the cursor using `FindTokenAtOrBeforeCursor` + `Contains`.
2. The token's `Value` is the raw text inside the quotes. Compute cursor offset: `position.Character - token.Span.StartColumn` (adjust for the opening `'` character — token span includes the opening quote, so offset 0 = opening quote, offset 1 = first content char).
3. Extract `textBeforeCursor = token.Value[..cursorContentOffset]`.
4. Parse `textBeforeCursor`:
   - Empty → `Phase.Empty`
   - Matches `^\d+$` → `Phase.NumberTyping`
   - Matches `^\d+\s$` → `Phase.AfterNumberSpace`
   - Matches `^\d+\s[A-Za-z]+$` → check if the alpha part is a complete temporal unit → `SegmentComplete` or `UnitTyping`
   - Contains `+` → split on last `+`, analyze the part after it using the same rules, prefix with `AfterPlus*`

For checking "complete temporal unit": use `TemporalUnits.TryGet(unitText, out _)`.

**`GetTemporalSlotItems(Compilation, TypeKind, string textBeforeCursor, TypedConstantPhase phase)` → `IEnumerable<CompletionItem>` (~30 lines):**

Phase-dependent:
- `AfterNumberSpace` or `AfterPlusNumberSpace` → return temporal unit items from `TemporalUnits.AllEntries`. For each entry, create a `CompletionItem` with label = `entry.Plural`, kind = `Unit`, detail = `"temporal unit"`. Both singular and plural forms should be available; the label shows the plural, but `filterText` includes both.
- `UnitTyping` → same temporal unit list; VS Code's prefix matching handles the filtering.
- `SegmentComplete` → return exactly one item: `+`, kind = `Operator`, detail = `"continue temporal literal"`, insertText = `" + "`.
- All other phases → return `[]` (no completions for number-typing, empty state handled by Slice 1, after-plus-no-space handled by returning nothing).

**`GetMoneySlotItems(Compilation, TypeKind, string textBeforeCursor, TypedConstantPhase phase)` → `IEnumerable<CompletionItem>` (~15 lines):**

- `AfterNumberSpace` → return all currency codes from `CurrencyCatalog.All.Keys`, kind = `Unit`, detail = `"money code"`.
- Otherwise → return `[]`.

**`GetQuantitySlotItems(Compilation, TypeKind, string textBeforeCursor, TypedConstantPhase phase)` → `IEnumerable<CompletionItem>` (~10 lines):**

- `AfterNumberSpace` → return examples from `ContentValidation.Examples` for unit portion, or UCUM unit examples.
- Otherwise → return `[]`.

### Modify:

**`GetCompletions(Compilation, Position, string?)` (~line 45):**

After the `triggerCharacter == "'"` check (line 51), add handling for `triggerCharacter == " "` inside a typed constant:

```csharp
if (triggerCharacter == " " && IsInsideTypedConstantToken(compilation.Tokens.Tokens, position))
{
    if (TryGetExpectedTypedConstantType(compilation, position, context, out var tcType)
        && TryGetTypedConstantSlotPhase(compilation.Tokens.Tokens, position, out var phase, out var textBefore))
    {
        var slotItems = tcType switch
        {
            TypeKind.Duration or TypeKind.Period
                => GetTemporalSlotItems(compilation, tcType, textBefore, phase),
            TypeKind.Money
                => GetMoneySlotItems(compilation, tcType, textBefore, phase),
            TypeKind.Quantity
                => GetQuantitySlotItems(compilation, tcType, textBefore, phase),
            _ => Enumerable.Empty<CompletionItem>(),
        };
        return CreateCompletionList(slotItems);
    }
    // Fall through to existing space handling for non-typed-constant contexts
}
```

This must come BEFORE the existing `context switch` at line 69 to intercept space inside typed constants.

**Update `GetTemporalLiteralItems` and `GetMoneyLiteralItems` (from Slice 1):**

Wire in the slot-detection call for Ctrl+Space inside an existing typed constant. The invoked-completion path (line 61) already detects "inside typed constant" — it calls `GetTypedConstantItems` which now branches by type (Slice 1). For temporal/money, if we're inside an existing token, we need to call the slot-phase parser and delegate to the slot-specific methods instead of showing Phase 0 starters.

### Tests (in `CompletionHandlerTests.cs`):

- `TypedConstant_Temporal_SpaceAfterNumber_ShowsUnits` — `[Fact]`: space trigger at `'3 ¦` in `field Delay as duration default '3 ¦'`, expect temporal unit labels
- `TypedConstant_Temporal_SegmentComplete_ShowsPlusContinuation` — `[Fact]`: Ctrl+Space at `'3 days¦` → expect `+` item
- `TypedConstant_Temporal_SpaceAfterPlusAndNumber_ShowsUnits` — `[Fact]`: space trigger at `'3 days + 30 ¦`, expect temporal units again
- `TypedConstant_Money_SpaceAfterAmount_ShowsCurrencyCodes` — `[Fact]`: space trigger at `'100 ¦` in `field Cost as money default '100 ¦'`, expect `"USD"`, `"EUR"` present
- `TypedConstant_Text_SpaceInside_NoCompletions` — `[Fact]`: space trigger at `'hello ¦` in `field Name as text default 'hello ¦'`, expect empty
- `TypedConstant_Temporal_NumberOnly_NoUnits` — `[Fact]`: Ctrl+Space at `'3¦` → expect Phase 0 starters, NOT temporal units

### Regression Anchors:
- All Slice 1 tests
- Existing space-trigger tests for non-typed-constant contexts must still work

**Files:** `CompletionHandler.cs`

- [ ] Create TypedConstantPhase enum
- [ ] Create TryGetTypedConstantSlotPhase
- [ ] Create GetTemporalSlotItems
- [ ] Create GetMoneySlotItems
- [ ] Create GetQuantitySlotItems
- [ ] Intercept space trigger inside typed constants
- [ ] Wire Ctrl+Space inside existing token to slot detection
- [ ] Tests pass

---

## Slice 3 — Qualifier-Aware Filtering

**What:** Thread `DeclaredQualifierMeta` from the resolved field/arg through to the completion provider, then hard-filter candidate lists before they're returned.

### Key Infrastructure Gap

`TryGetExpectedTypedConstantType` (line 309) returns `TypeKind` via `out` parameter. It resolves the expected type through several paths but never exposes qualifier metadata. The `TypedField` and `TypedArg` records both carry `ImmutableArray<DeclaredQualifierMeta> DeclaredQualifiers`. We need to thread this through.

### Create (in `CompletionHandler.cs`):

**`readonly record struct TypedConstantContext(TypeKind ExpectedType, ImmutableArray<DeclaredQualifierMeta> Qualifiers)` (~3 lines):**

A lightweight context struct that carries both the expected type and qualifier metadata.

### Modify:

**`TryGetExpectedTypedConstantType` → rename to `TryGetTypedConstantContext` (~line 309):**

Change signature:
```csharp
private static bool TryGetTypedConstantContext(
    Compilation compilation,
    Position position,
    SlotContext context,
    out TypedConstantContext tcContext)
```

Each resolution path must also extract qualifiers:

1. **`FindAtPosition` path (line 315–320):** `TypedTypedConstant` has `ResultType` but no qualifier info. Walk back to the field/arg that owns this expression to get qualifiers. Or, accept that this path returns empty qualifiers (the typed constant already exists and has a type — qualifier filtering is less critical for re-opened completions).

2. **`InArgDefault` path (line 322–325):** `TryGetCurrentEventArgType` returns `TypeKind`. Extend it (or add a companion) to also return `TypedArg.DeclaredQualifiers`.

3. **`TryGetCallParameterType` path (line 327–330):** No qualifier info for function parameters — return empty qualifiers.

4. **`GetCurrentActionTargetField` path (line 332–337):** `TypedField` is already returned. Extract `targetField.DeclaredQualifiers`.

5. **`TryGetEnclosingFieldType` path (line 339–341):** The enclosing field IS a `TypedField`. Extend `TryGetEnclosingFieldType` to also return the field, or add a `TryGetEnclosingField` variant.

6. **`TryGetBinaryPeerOperandType` path (line 343–346):** Peer operand inference resolves a `TypeKind`. No qualifier info available at this level — return empty qualifiers.

**Update all callers of `TryGetExpectedTypedConstantType`:**
- `GetTypedConstantItems` (line 223) — now receives `TypedConstantContext`
- Pass qualifiers through to the type-specific item generators

**Add qualifier filtering to each item generator:**

For `GetMoneySlotItems` / `GetMoneyLiteralItems`:
```csharp
var currencyQualifier = tcContext.Qualifiers
    .OfType<DeclaredQualifierMeta.Currency>()
    .FirstOrDefault();
if (currencyQualifier is not null)
{
    // Hard-filter: only show the declared currency code
    codes = codes.Where(c => c.Equals(currencyQualifier.CurrencyCode, StringComparison.OrdinalIgnoreCase));
}
```

For `GetTemporalSlotItems`:
```csharp
var temporalUnitQualifier = tcContext.Qualifiers
    .OfType<DeclaredQualifierMeta.TemporalUnit>()
    .FirstOrDefault();
if (temporalUnitQualifier is not null)
{
    // Hard-filter to the declared temporal unit
    units = units.Where(u => u.Singular.Equals(temporalUnitQualifier.UnitName, StringComparison.OrdinalIgnoreCase));
}

var temporalDimQualifier = tcContext.Qualifiers
    .OfType<DeclaredQualifierMeta.TemporalDimension>()
    .FirstOrDefault();
if (temporalDimQualifier is not null && temporalDimQualifier.Value != PeriodDimension.Any)
{
    // Filter to units in the declared dimension (calendar or clock)
    units = units.Where(u => MatchesPeriodDimension(u, temporalDimQualifier.Value));
}
```

For `GetQuantitySlotItems`: filter by `DeclaredQualifierMeta.Unit` or `DeclaredQualifierMeta.Dimension`.

**"No completions" rule:** If qualifiers exist but the LS can't resolve them (empty `DeclaredQualifiers` on a field whose `QualifierShape` requires qualifiers), return `[]`.

### Tests (in `CompletionHandlerTests.cs`):

- `TypedConstant_Money_QualifierInUSD_FiltersToUSD` — `[Fact]`: `field Cost as money in 'USD' default ¦` → trigger `'` → only `USD` money codes (or examples containing `USD`)
- `TypedConstant_Money_NoQualifier_ShowsAllCodes` — `[Fact]`: `field Cost as money default ¦` → trigger `'` at `'100 |` → all currency codes
- `TypedConstant_Temporal_QualifierInDays_FiltersUnits` — `[Theory]`: `field Grace as period in 'days' default ¦` → space at `'30 |` → only `day`/`days`
- `TypedConstant_Quantity_QualifierInKg_FiltersUnits` — `[Fact]`: `field Weight as quantity in 'kg' default ¦` → only `kg` in unit slot

### Regression Anchors:
- All Slice 1 and Slice 2 tests
- `Completions_TypedConstant_RuleComparison_UsesPeerOperandType` — peer operand path must still work (empty qualifiers)

**Files:** `CompletionHandler.cs`, `SlotContext.cs` (if `TryGetEnclosingField` variant needed)

- [ ] Create TypedConstantContext record
- [ ] Rename/extend TryGetExpectedTypedConstantType → TryGetTypedConstantContext
- [ ] Thread qualifiers through all resolution paths
- [ ] Add qualifier hard-filtering to each item generator
- [ ] Tests pass

---

## Slice 4 — Compound Temporal Full Cycle (V1)

**What:** Complete the compound temporal experience: after a temporal unit segment, show `+` continuation; after `+`, re-enter the number → unit cycle; the full `<number> <unit> [+ <number> <unit>]*` pattern works end-to-end. Also handle the Ctrl+Space recovery path at every phase.

### Key Lexer Fact

`'2 hours + 30 minutes'` lexes as one `TypedConstant` token with `Value = "2 hours + 30 minutes"`. The `TemporalQuantityParser.Parse()` splits on `+` to get segments `["2 hours", "30 minutes"]`. The slot-phase parser from Slice 2 already handles multi-segment detection. This slice ensures the completion items are correct for each phase within a multi-segment literal.

### Modify:

**`GetTemporalSlotItems` (from Slice 2) — extend for compound phases:**

- `Phase.SegmentComplete` → show exactly `[+]` item, kind `Operator`, detail `"continue temporal literal"`, insertText `" + "`.
- `Phase.AfterPlus` → show nothing (user must type a number first). On `Ctrl+Space`, show segment starter examples like `"30 minutes"`, `"1 hour"`.
- `Phase.AfterPlusNumberSpace` → same as `AfterNumberSpace`: show temporal units.

**Ensure `TryGetTypedConstantSlotPhase` handles multi-segment text:**

The regex/parsing logic must handle text like `"2 hours + 30 "` → `AfterPlusNumberSpace`, and `"2 hours + "` → `AfterPlus`.

**Insert text for temporal unit items:**

When a temporal unit is selected, insertText should be JUST the unit word (e.g., `"days"`), NOT including the closing quote. This keeps the caret inside the literal for compound continuation. Set `item.InsertTextFormat = InsertTextFormat.PlainText`. Do NOT include a closing `'` in the insertText — the UX spec says temporal unit selection keeps the caret inside the quote.

**Insert text for `+` continuation:**

InsertText is `" + "` (space-plus-space). After insertion, the cursor is at `'2 hours + |` — ready for the next number.

**Insert text for full-literal starters (Phase 0):**

When a Phase 0 starter like `"30 days"` is selected: insert the text plus the closing `'` if missing. Use a simple check: if there's no closing quote after the cursor position, append `'`.

### Tests (in `CompletionHandlerTests.cs`):

- `TypedConstant_Temporal_CompoundAfterUnit_ShowsPlus` — `[Fact]`: Ctrl+Space at `'2 hours¦` → exactly one item `+`
- `TypedConstant_Temporal_CompoundAfterPlus_ShowsNothing` — `[Fact]`: space at `'2 hours + ¦` → empty (user must type number)
- `TypedConstant_Temporal_CompoundAfterPlusAndNumberSpace_ShowsUnits` — `[Fact]`: space at `'2 hours + 30 ¦` → temporal units
- `TypedConstant_Temporal_CompoundMultiSegment_WorksEndToEnd` — `[Fact]`: verify that at each phase of `'2 hours + 30 minutes + 15 seconds¦`, the right items appear

### Regression Anchors:
- All Slice 1, 2, 3 tests

**Files:** `CompletionHandler.cs`

- [ ] Extend GetTemporalSlotItems for compound phases
- [ ] Verify TryGetTypedConstantSlotPhase handles multi-segment
- [ ] Insert text rules: no closing quote for units, closing quote for full starters
- [ ] Tests pass

---

## Slice 5 — Integration Tests and Edge Cases

**What:** Final integration pass covering edge cases from Elaine's spec, cross-type verification, and Ctrl+Space recovery in every context.

### Tests (in `CompletionHandlerTests.cs`):

**Edge case tests:**
- `TypedConstant_UnknownExpectedType_ReturnsEmpty` — already exists; verify still passing
- `TypedConstant_SpaceInsideText_NoCompletions` — space trigger inside `'hello ¦'` on `text` field → empty
- `TypedConstant_CtrlSpaceInsidePartialTemporal_ShowsUnitsForCurrentSlot` — Ctrl+Space at `'3 da¦ys'` → temporal units (prefix filter handles `da`)
- `TypedConstant_BooleanInRule_SameBehaviorAsDefault` — `[Fact]`: trigger `'` on boolean in `when Active == ¦` → `["true", "false"]` (same as boolean default)
- `TypedConstant_MoneyQualifier_SpaceAfterAmount_ShowsOnlyDeclaredCurrency` — space at `'100 ¦` on `money in 'USD'` → exactly `["USD"]`
- `TypedConstant_NoKeywordsInAnyTypedConstant` — `[Theory]` with multiple types: Ctrl+Space inside `''` never returns keyword items

**Ctrl+Space recovery tests:**
- `TypedConstant_CtrlSpace_EmptyBoolean_ShowsTrueFalse` — `[Fact]`
- `TypedConstant_CtrlSpace_EmptyTemporal_ShowsStarters` — `[Fact]`
- `TypedConstant_CtrlSpace_EmptyMoney_ShowsExamples` — `[Fact]`
- `TypedConstant_CtrlSpace_PartialMoneyCode_ShowsFilteredCodes` — `[Fact]`: Ctrl+Space at `'100 U¦` → currency codes (VS Code filters by prefix)

**Files:** `CompletionHandlerTests.cs`

- [ ] All edge case tests pass
- [ ] All Ctrl+Space recovery tests pass
- [ ] Full regression suite green

---

## File Inventory

| File | Slices |
|------|--------|
| `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` | 1, 2, 3, 4 |
| `tools/Precept.LanguageServer/SlotContext.cs` | 3 (if TryGetEnclosingField added) |
| `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs` | 1, 2, 3, 4, 5 |

## Tooling/MCP Sync Assessment

- **Syntax highlighting:** No changes needed — typed constant tokens already highlighted correctly.
- **Semantic tokens:** No changes needed — no new token types.
- **Completions:** This IS the completions change.
- **MCP tools:** No changes needed — MCP tools don't expose completion items.
- **Grammar (tmLanguage):** No changes needed — no new syntax forms.
- **Hover:** No changes needed — hover already shows typed constant info.
