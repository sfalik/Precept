# Temporal and Business-Unit Literal Completions — UX Proposal

> **Status:** Design complete — pending Shane sign-off for implementation  
> **Design:** Frank (APPROVED WITH CONDITIONS, 2026-05-16)  
> **Tooling cost:** Kramer (reviewed, 2026-05-16)  
> **Author:** Elaine (UX)  
> **Date:** 2026-05-16  
> **Scope:** VS Code completion UX for typed constants unless explicitly noted

---

## 1. Problem Statement

Precept's typed constants are doing too much authoring work in raw text.

Today, the user often has to remember punctuation-heavy literal formats before the editor becomes helpful:

- `date` requires `YYYY-MM-DD`
- `datetime` requires a literal `T`
- `instant` requires a trailing `Z`
- `zoneddatetime` requires bracketed timezone syntax: `2026-04-15T14:30:00[America/New_York]`
- `duration` / `period` require word-based quantity syntax: `2 hours + 30 minutes`
- `money` requires amount-first, code-second ordering: `0.00 USD`
- `quantity` requires a valid UCUM unit after a space: `5 kg`

The current completion experience helps only after the user has already typed enough of the structure correctly:

- `GetStructuredExampleItems` gives plain example values for `date`, `time`, `instant`, `datetime`, and `zoneddatetime`.
- `duration`, `money`, and `quantity` get slot help only after the author has already typed the first number and a space.
- snippet support exists in the language server (`CreateItem(..., snippetTemplate: ...)`), but typed-constant completions mostly do not use it.

That creates three UX failures:

1. **Format opacity** — users know the meaning they want, but not the exact literal surface.
2. **Construction friction** — the editor helps late, after the user has already guessed the format.
3. **Interpolation friction** — Precept already supports typed-constant interpolation, but authors still have to hand-build the braces and surrounding literal skeleton.

This is below bar for a language that wants to feel inspectable, deterministic, and premium.

---

## 2. User Research

### What the user knows vs. what the system requires

| Domain | What the user usually knows | What Precept currently requires them to type | Primary pain |
|---|---|---|---|
| Date | “May 16, 2026” | `2026-05-16` | separator/order lookup |
| Time | “2:30 PM” | `14:30` or `14:30:00` | 24-hour form, optional seconds |
| Instant | “2:30 PM UTC” | `2026-05-16T14:30:00Z` | `T` + trailing `Z` |
| DateTime | “local date/time” | `2026-05-16T14:30:00` | exact punctuation |
| ZonedDateTime | “2:30 in New York” | `2026-05-16T14:30:00[America/New_York]` | bracket syntax + zone lookup |
| Duration | “3 hours 30 minutes” | `3 hours + 30 minutes` | exact unit words + ` + ` separators |
| Period | “30 days” / “1 year 6 months” | `30 days` or `1 year + 6 months` | same as duration, plus calendar/time distinction |
| Money | “$1.50” / “1.50 dollars” | `1.50 USD` | order is amount then code, never symbol-first |
| Quantity | “5 kilograms” / “5 each” | `5 kg` / `5 each` | valid UCUM code lookup |

### Findings from the current implementation

1. **Temporal structured types are still example-driven, not template-driven.** `date`, `time`, `instant`, `datetime`, and `zoneddatetime` all route through `GetStructuredExampleItems`.
2. **Timezone is lookup-heavy.** The runtime metadata still shows only two examples (`America/New_York`, `UTC`), but the completion layer now exposes the full TZDB catalog. That solves lookup breadth, not authoring guidance.
3. **Time is especially opaque.** `Types.cs` documents `HH:mm:ss`, but `TemporalParser` accepts both `HH:mm` and `HH:mm:ss`. The valid surface is broader than the example surface.
4. **Duration and period are conceptually builder types.** The parser wants `<integer> <unit> [+ <integer> <unit>]*`, which is exactly the kind of structure snippets are good at teaching.
5. **Money and quantity are qualifier-aware already.** `TypedConstantContext` carries declared qualifiers, and current completions already filter currency/unit suggestions. That is the right foundation for premium pre-filled templates.
6. **Typed-constant interpolation already exists in the DSL.** The language spec explicitly allows `{expr}` inside `'...'`, samples use it (`'0.00 {CatalogCurrency}'`, `'1 {StockingUnit}/{PurchaseUnit}'`), and the LS already has typed hole completions for magnitude/currency/unit/whole-value slots.

### User mental model

The author is not thinking in parser tokens. They are thinking:

- “Give me a date literal.”
- “Give me a duration builder.”
- “This money field is already `in USD`; stop making me remember the suffix.”
- “This quantity uses the stocking unit; let me point at that field instead of retyping the literal.”

The premium move is to let completions author the skeleton and let the user fill the semantic parts.

---

## 3. Proposed Experience

### 3.1 Experience principles

1. **Teach the format by insertion, not by prose alone.**
2. **Make the first useful item a valid template, not just a raw example.**
3. **Use qualifier context aggressively.** If the field already declares the currency or unit, the completion should prefill it.
4. **Treat interpolation as first-class authoring.** The user should be able to choose a template that already contains `{...}` holes.
5. **Stay honest.** Do not invent syntax the runtime does not support.

### 3.2 Temporal types

### Date

| Completion label | Insert text (snippet) | Notes |
|---|---|---|
| `date — YYYY-MM-DD` | `'${1:2026}-${2:05}-${3:16}'` | Primary starter |
| `date — today-shaped example` | `'2026-05-16'` | Plain example/reuse remains useful |

**Recommendation:** replace the current plain-example-first experience with a real date template at the top.

### Time

| Completion label | Insert text (snippet) | Notes |
|---|---|---|
| `time — HH:mm` | `'${1:09}:${2:00}'` | Match the parser's accepted short form |
| `time — HH:mm:ss` | `'${1:09}:${2:00}:${3:00}'` | Explicit-seconds variant |

**Recommendation:** show both. The runtime accepts both, so the UX should stop pretending seconds are mandatory.

### Instant

| Completion label | Insert text (snippet) | Notes |
|---|---|---|
| `instant — UTC timestamp` | `'${1:2026}-${2:05}-${3:16}T${4:14}:${5:30}:${6:00}Z'` | Teaches both `T` and `Z` |

### DateTime

| Completion label | Insert text (snippet) | Notes |
|---|---|---|
| `date-time — local` | `'${1:2026}-${2:05}-${3:16}T${4:14}:${5:30}:${6:00}'` | Primary local datetime template |
| `date-time — midnight` | `'${1:2026}-${2:05}-${3:16}T00:00:00'` | Good for due-date style business use |

### ZonedDateTime

| Completion label | Insert text (snippet) | Notes |
|---|---|---|
| `zoned date-time — explicit zone` | `'${1:2026}-${2:05}-${3:16}T${4:14}:${5:30}:${6:00}[${7:America/New_York}]'` | Teaches bracket syntax |
| `zoned date-time — timezone field` | `'${1:2026}-${2:05}-${3:16}T${4:14}:${5:30}:${6:00}[{${7:LocalTimezone}}]'` | Uses existing Precept interpolation |
| `zoned date-time — UTC-style starter` | `'${1:2026}-${2:05}-${3:16}T${4:14}:${5:30}:${6:00}[UTC]'` | Good for teams that normalize around UTC |

**Recommendation:** make the bracketed timezone structure visible from the first completion item. Right now the user has to discover `[...]` on their own.

### Duration

| Completion label | Insert text (snippet) | Notes |
|---|---|---|
| `duration — hours + minutes` | `'${1:2} hours + ${2:30} minutes'` | Best primary builder |
| `duration — hours only` | `'${1:4} hours'` | Common SLA case |
| `duration — minutes only` | `'${1:30} minutes'` | Common short-delay case |
| `duration — whole value field` | `'{${1:ExistingDuration}}'` | For reuse/composition |

**Recommendation:** keep the existing slot-aware unit flow after insertion, but promote builder snippets above raw examples.

### Period

| Completion label | Insert text (snippet) | Notes |
|---|---|---|
| `period — days` | `'${1:30} days'` | Most common business deadline case |
| `period — weeks` | `'${1:2} weeks'` | Secondary common case |
| `period — years + months` | `'${1:1} year + ${2:6} months'` | Good for policy/contract lanes |
| `period — whole value field` | `'{${1:GracePeriod}}'` | Reuse existing period value |

### Timezone

Timezone is less a snippet problem and more a lookup problem.

**Recommendation:** for `timezone`-typed fields, keep catalog completions, but rank them like this:

1. reused values in the current file,
2. pinned common values (`UTC`, `America/New_York`, `Europe/London`),
3. then the full TZDB list.

That is a premium lookup experience without inventing fake structure.

### 3.3 Business units

### Money

#### Unqualified money

| Completion label | Insert text (snippet) | Notes |
|---|---|---|
| `money — amount + currency` | `'${1:0.00} ${2:USD}'` | Primary generic template |
| `money — whole value field` | `'{${1:AmountDue}}'` | Reuse existing money value |
| `money — amount + currency field` | `'${1:0.00} {${2:CatalogCurrency}}'` | Dynamic currency path |

#### Money with qualifier

If the field is `money in 'USD'`, the top item should not make the user re-decide the currency.

| Context | Completion label | Insert text (snippet) |
|---|---|---|
| `money in 'USD'` | `money — USD` | `'${1:0.00} USD'` |
| `money in '{CatalogCurrency}'` | `money — declared currency field` | `'${1:0.00} {CatalogCurrency}'` |

**Recommendation:** when the qualifier already fixes the suffix, the UX should collapse to amount-entry, not keep pretending the suffix is open-ended.

### Quantity

#### Quantity with exact `in` qualifier

| Context | Completion label | Insert text (snippet) |
|---|---|---|
| `quantity in 'kg'` | `quantity — kg` | `'${1:0} kg'` |
| `quantity in '{StockingUnit}'` | `quantity — declared unit field` | `'${1:0} {StockingUnit}'` |

#### Quantity with dimension-only `of` qualifier

When the field says `quantity of 'mass'`, the editor knows the dimension family but not the exact unit.

**Recommendation:** show a filtered starter set from valid UCUM units in that dimension, e.g. for mass:

| Completion label | Insert text (snippet) |
|---|---|
| `quantity — kg` | `'${1:0} kg'` |
| `quantity — g` | `'${1:0} g'` |
| `quantity — [lb_av]` | `'${1:0} [lb_av]'` |

That is materially better than forcing the user to browse a global unit catalog.

#### Interpolated compound-unit quantity

For cases like the inventory sample, offer explicit dynamic templates:

| Completion label | Insert text (snippet) | Notes |
|---|---|---|
| `quantity — unit field` | `'${1:0} {${2:StockingUnit}}'` | Single unit hole |
| `quantity — numerator / denominator fields` | `'${1:1} {${2:StockingUnit}}/{${3:PurchaseUnit}}'` | Matches sample authoring pattern |
| `quantity — whole value field` | `'{${1:Qty}}'` | Reuse existing quantity value |

### 3.4 Interpolation recommendation

### Recommendation

Treat interpolation as **tooling UX**, not as a new DSL feature request.

Precept already supports typed-constant interpolation. The premium opportunity is to make it easy to author by combining two things:

1. **Snippet tab stops** for the static skeleton.
2. **Existing Precept `{expr}` interpolation** for the dynamic segments.

That means the editor should support both of these flows well:

- static authoring: `'${1:2026}-${2:05}-${3:16}'`
- dynamic authoring: `'${1:0.00} {${2:CatalogCurrency}}'`

### Explicit non-recommendation

Do **not** turn this proposal into a new partial-literal DSL feature. The language already has interpolation. The gap is that completions do not currently author the surrounding literal structure for the user.

### Premium interpolation behavior

1. Add `{` as a typed-constant completion trigger so field/arg suggestions appear immediately inside a hole.
2. Offer interpolation-bearing snippets in the initial completion list, not just after the user has hand-built the braces.
3. Keep current slot-typed hole completions as the precision layer once the caret is inside `{...}`.

---

## 4. Design Options

### Option A — Better examples only

**What:** keep the current architecture, but improve labels/details/documentation for example items.

**Pros**
- lowest implementation cost
- minimal test churn
- no new insertion behavior to tune

**Cons**
- still lookup-heavy
- does not solve format opacity well
- still makes interpolation a manual power-user feature

**Trade-off:** cheap, but not premium.

### Option B — Template-first snippets + existing slot completions (**recommended**)

**What:** add snippet completion items for structured temporal/business literals, keep current slot-aware unit/code completions, and make qualifier-aware templates first-class.

**Pros**
- directly teaches correct format
- uses LS capabilities that already exist
- tooling-only for the main value
- composes naturally with existing hole completions

**Cons**
- more completion items to curate
- requires ranking work so menus do not get noisy
- some context-sensitive templates depend on qualifier/source-field threading being solid

**Trade-off:** the best user value for reasonable cost.

### Option C — Stateful literal builder / wizard

**What:** treat certain typed constants as a mini workflow: choose domain, then walk segment by segment with custom completion state.

**Pros**
- highest possible polish
- could feel extremely guided for durations, zoned datetimes, and compound quantities

**Cons**
- highest LS complexity
- more custom state to maintain across edits
- harder to keep deterministic and unsurprising
- greater risk of feeling “smart” instead of honest

**Trade-off:** premium in theory, expensive and easier to overdesign.

**Recommendation:** ship Option B first. It is the premium version that still feels like an editor, not a wizard.

---

## 5. Context Sensitivity

### 5.1 Qualifier-aware behavior

The completion surface should adapt to qualifier shape before ranking anything else.

| Context | Proposed behavior |
|---|---|
| `money in 'USD'` | prefill `USD`; primary template is `'${1:0.00} USD'` |
| `money in '{CatalogCurrency}'` | prefill interpolation; primary template is `'${1:0.00} {CatalogCurrency}'` |
| `quantity in 'kg'` | primary template is `'${1:0} kg'`; unit catalog stays hard-filtered to `kg` |
| `quantity in '{StockingUnit}'` | primary template is `'${1:0} {StockingUnit}'` |
| `quantity of 'mass'` | filter starter units to mass-family UCUM units only |
| `period in 'days'` | show day-based templates first; keep unit slot filtered to `day` / `days` |
| `duration` | show time-based builders only; no calendar-unit templates |

### 5.2 Scope-aware interpolation

If a relevant in-scope field or arg exists, the completion list should offer a dynamic template, not just a static one.

Examples:

- `CatalogCurrency as currency` in scope -> show `money — amount + currency field`
- `StockingUnit as unitofmeasure` in scope -> show `quantity — unit field`
- `LocalTimezone as timezone` in scope -> show `zoned date-time — timezone field`

### 5.3 Zoned date-time qualifier note

The runtime has a `Timezone` qualifier axis, but `Types.cs` does not currently advertise a qualifier shape for `zoneddatetime` itself. So this context-sensitive case needs confirmation before implementation:

- if `zoneddatetime in 'America/New_York'` is an intended surface, prefill it
- if not, keep the premium zone-aware behavior at the snippet/template level only

---

## 6. Impact Assessment

| Feature | Tooling cost (Kramer) | Runtime cost (George) | Scope |
|---|---|---|---|
| Template-first snippets for date/time/instant/datetime/zoneddatetime | `CompletionHandler.cs` item generation + tests; uses existing `snippetTemplate` support | none | single PR candidate |
| Duration/period builder snippets | same file/tests; likely no parser work | none | same PR candidate |
| Qualifier-aware prefilled money/quantity snippets | new helper logic in `CompletionHandler.cs` to map `DeclaredQualifierMeta` into snippet strings; more tests | none | same PR or immediate follow-up |
| `{` trigger for typed-constant holes | registration trigger update + hole routing tests | none | small follow-up, could ride same PR |
| Scope-aware interpolation templates (`{CatalogCurrency}`, `{StockingUnit}`) | needs symbol/qualifier-aware ranking logic in completion handler | none | medium follow-up |
| Dimension-filtered quantity starters | likely reuse existing catalog/dimension metadata; ranking/curation work in LS | none if catalog can already support the shortlist; otherwise policy question, not parser work | maybe second PR |
| ZonedDateTime qualifier-aware prefills | depends on whether that qualifier surface is already intended | maybe none, maybe language-surface clarification | gated |

### Tooling notes

- Main implementation home is still `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`.
- The LS already supports snippet insertion via `InsertTextFormat.Snippet`; this proposal is primarily about using that capability for typed constants.
- No new `SlotVocabulary` looks necessary for the base proposal.
- Completion tests should grow around snippet labels, `InsertText`, `InsertTextFormat`, ranking, qualifier-aware prefills, and `{` trigger behavior.

### Runtime notes

For the recommended path, this is overwhelmingly tooling-only.

The one possible runtime/architecture question is not parser syntax — it is whether any new context-sensitive behavior depends on qualifier surfaces that are not actually intended on certain types yet (notably `zoneddatetime`).

### Scope recommendation

Treat this as **two slices**:

1. **Slice A — premium static snippets**
   - temporal templates
   - money/quantity templates
   - qualifier-aware prefills
2. **Slice B — premium interpolation flow**
   - `{` trigger inside typed constants
   - scope-aware interpolation templates
   - polish/ranking around in-scope dynamic fields

That keeps the first PR valuable even if the interpolation polish follows immediately after.

---

## 7. Open Questions — Status

1. **Should the UX prefer the shortest valid time form or the most explicit one?** ~~The parser accepts `HH:mm`, but current metadata teaches `HH:mm:ss`.~~
   **CLOSED (Frank):** Show both forms, labeled explicitly (`time — HH:mm` and `time — HH:mm:ss`). Both are valid per `TemporalParser.ParseTime`. Additionally, `TimeValidation` metadata in `Types.cs` should be updated to reflect both accepted forms — file a follow-up for George to add `"14:30"` to the examples array.
2. **Should premium completions normalize around “builder templates first” for all structured typed constants, or only temporal/business-unit lanes?** ~~My recommendation is yes for all structured lanes over time, but this proposal is scoped narrower.~~
   **CLOSED (Frank):** Yes as a principle. This proposal is correctly scoped to temporal + business-unit lanes. The pattern will generalize naturally once it ships and proves out.
3. **For `quantity of <dimension>`, do we want a curated starter set or the full filtered catalog?** ~~My UX vote is curated starters first, full catalog below.~~
   **CLOSED (Frank):** Full filtered catalog, ranked. Curate by sort order, not by exclusion. The first 3–5 most common units in a dimension family get priority sort weight via `UcumCatalog.BrowseTier1()` filtered by `DimensionVector`, but the full dimension-filtered set must be available. The ranking heuristic: base units first, prefixed variants after. This is the catalog-driven architecture — no hardcoded shortlists.
4. **Is `zoneddatetime in <timezone>` intended surface area?** ~~If yes, the completion UX should prefill it; if not, we should not imply a qualifier contract that the type surface has not actually declared.~~
   **CLOSED (Frank):** NOT current surface. `TypeKind.ZonedDateTime` has no `QualifierShape` — there is no `in <timezone>` qualifier axis on this type. No qualifier-aware prefill for `zoneddatetime`. If we want it in the future, that is a separate language proposal with its own design review.
5. **Should `{` become an automatic completion trigger inside typed constants?** ~~I think yes — it is the premium interpolation move — but it does change trigger behavior globally for the LS registration.~~
   **OPEN — deferred to Shane.** Frank: architecturally fine. The `{` trigger must be guarded via `IsInsideTypedConstantToken` to fire only inside typed-constant spans. Implementation is Slice B, not Slice A. But adding `{` to `TriggerCharacters` (line 47) affects all completion contexts globally — Shane must confirm scope.
6. **Do Shane and Frank want this first implementation limited to `date`, `time`, `instant`, `datetime`, `zoneddatetime`, `duration`, `period`, `money`, and `quantity`, or should `price` / `exchangerate` join the same pass?** ~~I would keep V1 focused unless Kramer says the incremental cost is trivial.~~
   **OPEN — deferred to Shane.** Frank recommends deferring `price`/`exchangerate` to V2. Their compound qualifier shapes (`In`+`Of` and `In`+`To`) are more complex. Get the simpler types right first, then extend.

---

## Recommendation Summary

Ship **template-first typed-constant completions** for temporal and business-unit literals.

The win is straightforward:

- stop making authors memorize punctuation-heavy literal formats,
- use qualifier context to remove redundant typing,
- and make existing typed-constant interpolation feel intentional instead of hidden.

That is the premium path without inventing new language surface.

---

## 8. Design

> **Design author:** Frank (Lead/Architect)  
> **Date:** 2026-05-16

### 8.1 Runtime impact

**No runtime changes required.**

Option B is tooling-only — confirmed after reading the proposal, the review, and the source. Specifically:

- **No parser changes.** All temporal, money, and quantity literal formats are already parsed. The proposal adds no new syntax — only better editor scaffolding for existing syntax.
- **No type checker changes.** `TypedConstantContext`, `DeclaredQualifierMeta`, and qualifier threading are already complete. The type checker already populates `DeclaredQualifierMeta.Currency`, `.Unit`, `.Dimension` with `SourceFieldName` for interpolated qualifiers. No new type-level validation is needed.
- **No new DSL surface.** The `{expr}` interpolation inside typed constants already exists and is well-exercised in samples. The proposal merely surfaces it through completions.
- **`zoneddatetime in <timezone>` is explicitly out of scope.** `TypeKind.ZonedDateTime` (Types.cs line 461–479) has no `QualifierShape`. There is no `in <timezone>` qualifier axis for this type. If added in the future, that is a separate language proposal. No runtime work is needed for this proposal.
- **`TimezoneValidation.Examples` fix is already shipped.** `GetTimezoneItems` (commit `c35e6032`) queries `DateTimeZoneProviders.Tzdb.Ids` directly — the validation examples gap is irrelevant to completions now.

### 8.2 Tooling impact (primary)

This is the core of the work. All changes live in `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` and its test file `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs`.

**Blocker: `AppendToInsertText` hardcodes `InsertTextFormat.PlainText` (line 1024).**

When the `'` trigger fires and `appendClosingQuote = true`, every completion item passes through `AppendToInsertText` (line 1014). This method unconditionally sets `InsertTextFormat = InsertTextFormat.PlainText` (line 1024), silently destroying any snippet tab stops. All `${1:...}` placeholders would render as literal text in the editor. **This must be fixed before any snippet template ships.** It is Slice 0 — the prerequisite for everything else.

**Completion handler changes per slice:**

| Change | Method(s) | Slice |
|---|---|---|
| Fix `AppendToInsertText` to preserve `InsertTextFormat.Snippet` | `AppendToInsertText` (line 1019) | 0 |
| Date/time/instant/datetime/zoneddatetime snippet templates | `GetStructuredExampleItems` → new per-type snippet methods or inline in the `TypeKind` switch (lines 1003–1004) | A |
| Duration/period builder snippets | `GetTemporalLiteralItems` (line 1045) — prepend builder snippets before examples in the empty-phase branch | A |
| Qualifier-aware money templates | `GetMoneyLiteralItems` (line 1061) — read `tcContext.Qualifiers.OfType<DeclaredQualifierMeta.Currency>()`, emit conditional `snippetTemplate` strings | A |
| Qualifier-aware quantity templates | `GetQuantityLiteralItems` (line 1118) — read `DeclaredQualifierMeta.Unit` and `.Dimension`, emit conditional templates | A |
| Dimension-filtered quantity starters | Reuse `GetQuantitySlotItems` dimension-filter pattern (lines 1377–1381) for initial items | A |
| `{` trigger registration | `TriggerCharacters` (line 47) — add `"{"` | B |
| `{` trigger guard | New branch in `GetCompletions` (after line 83) — `if (triggerCharacter == "{" && IsInsideTypedConstantToken(...))` dispatching to `GetHoleItems` | B |
| Scope-aware interpolation templates | New method querying `compilation.Semantics.Fields` for type-matching fields (pattern from `GetHoleFieldsOfTypes`, line 2969) | B |

**Catalog-driven constraint: dimension-filtered quantity starters.**

Dimension-filtered quantity starters MUST derive from catalog metadata, not hardcoded lists. The exact API path:

1. `UcumCatalog.BrowseTier1()` → delegates to `UcumAtomCatalog.BrowseTier1()` → returns `IReadOnlyList<UcumAtom>` (~100 curated atoms across all dimensions)
2. `DimensionCatalog.All[dimensionName]` → returns `DimensionAlias` with `.Vector` (`DimensionVector`)
3. Filter: `BrowseTier1().Where(atom => atom.Vector == dimAlias.Vector)` — this is exactly the pattern already used in `GetQuantitySlotItems` (lines 1379–1381)
4. Each `UcumAtom` has `.Code` (UCUM code), `.Name` (human-readable), `.PrintSymbol` (display), `.Vector` (dimension classification)

The slot-phase handler (`GetQuantitySlotItems`, line 1360) already does this filtering for the `AfterNumberSpace` phase. The initial-items handler (`GetQuantityLiteralItems`) must reuse the same pattern for the `Empty` phase, emitting snippet templates like `'${1:0} kg'` instead of bare unit codes.

### 8.3 MCP impact

**MCP tools are unaffected.**

- `precept_compile` DTOs are unchanged — typed-constant completions are a language-server concern, not a compilation concern.
- `precept_language` vocabulary is unchanged — no new keywords, types, operators, or constructs are added.
- `precept_types` / `precept_syntax` output is unchanged.
- The MCP server does not expose completion items.

---

## 9. Implementation Plan

> **Plan author:** Frank (Lead/Architect)  
> **Date:** 2026-05-16  
> **Quality bar:** Vertical slices with method-level specificity, exact file paths, tests per slice, regression anchors, dependency ordering.

### Slice 0 — `InsertTextFormat` prerequisite

**Blocks:** Slices A and B. No snippet template works correctly until this ships.

**File:** `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`

**What:** Fix `AppendToInsertText` (line 1019–1029) to preserve `InsertTextFormat.Snippet` when the source item was already a snippet. Current code unconditionally sets `InsertTextFormat = InsertTextFormat.PlainText` (line 1024).

**Implementation:** Change `AppendToInsertText` to check the source item's `InsertTextFormat`. When the source item has `InsertTextFormat.Snippet`, the closing `'` suffix must be appended after the last tab stop — either as a literal `'` appended to the snippet string (the snippet engine treats non-`$` text as literal), or by inserting `$0'` at the end (placing the final cursor before the closing quote). The `InsertTextFormat` must be preserved as `Snippet` in this case. When the source item has `PlainText`, behavior is unchanged.

Recommended approach:
```csharp
private static CompletionItem AppendToInsertText(CompletionItem item, string suffix) =>
    new()
    {
        Label = item.Label,
        InsertText = (item.InsertText ?? item.Label) + suffix,
        InsertTextFormat = item.InsertTextFormat,  // preserve original format
        Documentation = item.Documentation,
        SortText = item.SortText,
        Detail = item.Detail,
        Kind = item.Kind,
    };
```

**Tests:**

| Test | What it verifies |
|---|---|
| `Completions_TypedConstant_SingleQuoteTrigger_PlainTextItem_AppendsClosingQuote` | Existing plain-text items still get `'` appended with `InsertTextFormat.PlainText` (regression anchor) |
| `Completions_TypedConstant_SingleQuoteTrigger_SnippetItem_PreservesFormat` | A snippet item with `${1:...}` retains `InsertTextFormat.Snippet` after `AppendToInsertText` and has `'` appended to the snippet string |

**File:** `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs`

---

### Slice A — Premium static snippet templates

**Depends on:** Slice 0.

#### Sub-slice A1: Temporal types (date, time, instant, datetime, zoneddatetime)

**File:** `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`

**What:** Replace the `GetStructuredExampleItems` dispatch for `TypeKind.Date`, `.Time`, `.Instant`, `.DateTime`, `.ZonedDateTime` (lines 1003–1004) with a new method `GetTemporalSnippetItems` (or per-type methods) that emits snippet templates before plain examples.

**Method change:** In the `tcContext.ExpectedType switch` block (line 998), change:
```csharp
TypeKind.Date or TypeKind.Time or TypeKind.Instant or TypeKind.DateTime
    or TypeKind.ZonedDateTime => GetStructuredExampleItems(compilation, tcContext),
```
to dispatch to a new method that yields snippet templates first (using `CreateItem(..., snippetTemplate: ...)`) followed by the existing reused-values and example items from `GetStructuredExampleItems`.

**Snippet templates per type:**

| Type | Label | `snippetTemplate` |
|---|---|---|
| `Date` | `date — YYYY-MM-DD` | `${1:2026}-${2:05}-${3:16}` |
| `Time` | `time — HH:mm` | `${1:09}:${2:00}` |
| `Time` | `time — HH:mm:ss` | `${1:09}:${2:00}:${3:00}` |
| `Instant` | `instant — UTC timestamp` | `${1:2026}-${2:05}-${3:16}T${4:14}:${5:30}:${6:00}Z` |
| `DateTime` | `date-time — local` | `${1:2026}-${2:05}-${3:16}T${4:14}:${5:30}:${6:00}` |
| `DateTime` | `date-time — midnight` | `${1:2026}-${2:05}-${3:16}T00:00:00` |
| `ZonedDateTime` | `zoned date-time — explicit zone` | `${1:2026}-${2:05}-${3:16}T${4:14}:${5:30}:${6:00}[${7:America/New_York}]` |
| `ZonedDateTime` | `zoned date-time — UTC` | `${1:2026}-${2:05}-${3:16}T${4:14}:${5:30}:${6:00}[UTC]` |

**Note:** No `zoneddatetime in <timezone>` qualifier-aware prefill — explicitly out of scope. The `zoned date-time — timezone field` interpolation template (`[{${7:LocalTimezone}}]`) is Slice B scope-aware work.

**Tests (file: `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs`):**

| Test | What it verifies |
|---|---|
| `Completions_TypedConstant_Date_ShowsSnippetTemplate` | Date completion list includes `date — YYYY-MM-DD` with `InsertTextFormat.Snippet` and correct `InsertText` |
| `Completions_TypedConstant_Time_ShowsBothForms` | Time completion list includes both `HH:mm` and `HH:mm:ss` variants |
| `Completions_TypedConstant_Instant_ShowsUTCTemplate` | Instant template includes trailing `Z` |
| `Completions_TypedConstant_DateTime_ShowsLocalTemplate` | DateTime template includes `T` separator |
| `Completions_TypedConstant_ZonedDateTime_ShowsBracketedZone` | ZDT template includes `[...]` bracket syntax |
| `Completions_TypedConstant_Date_SnippetBeforeExamples` | Snippet templates sort before plain examples (verify `SortText` ordering) |

#### Sub-slice A2: Duration and period builder snippets

**File:** `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`

**What:** In `GetTemporalLiteralItems` (line 1045), in the `Empty` phase branch (when `TryGetTypedConstantSlotPhase` returns false or phase is `Empty`), prepend builder snippets before the existing reused-values and example items.

**Snippet templates:**

| Type | Label | `snippetTemplate` |
|---|---|---|
| `Duration` | `duration — hours + minutes` | `${1:2} hours + ${2:30} minutes` |
| `Duration` | `duration — hours only` | `${1:4} hours` |
| `Duration` | `duration — minutes only` | `${1:30} minutes` |
| `Period` | `period — days` | `${1:30} days` |
| `Period` | `period — weeks` | `${1:2} weeks` |
| `Period` | `period — years + months` | `${1:1} year + ${2:6} months` |

**Qualifier threading:** Duration/period qualifiers (`DeclaredQualifierMeta.TemporalUnit` / `.TemporalDimension`) are already read by `GetTemporalSlotItems` / `BuildTemporalUnitItems`. For the initial items, use the same qualifier to filter which builder templates appear (e.g., if the field is `duration in 'hours'`, show hours-based templates first). Read `tcContext.Qualifiers.OfType<DeclaredQualifierMeta.TemporalUnit>()` — if present, filter/prioritize templates matching the declared unit.

**Tests:**

| Test | What it verifies |
|---|---|
| `Completions_TypedConstant_Duration_ShowsBuilderSnippets` | Duration empty-phase shows `hours + minutes` builder with `InsertTextFormat.Snippet` |
| `Completions_TypedConstant_Period_ShowsDaysBuilder` | Period shows `days` builder |
| `Completions_TypedConstant_Duration_SnippetBeforeExamples` | Builder snippets sort before plain reused/example items |

#### Sub-slice A3: Qualifier-aware money templates

**File:** `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`

**What:** In `GetMoneyLiteralItems` (line 1061), in the `Empty` phase branch, read qualifier context from `tcContext.Qualifiers` and emit conditional snippet templates.

**Logic:**

```
var currencyQualifier = tcContext.Qualifiers.OfType<DeclaredQualifierMeta.Currency>().FirstOrDefault();
if (currencyQualifier is not null)
{
    if (currencyQualifier.SourceFieldName is not null)
    {
        // Interpolated qualifier: money in '{CatalogCurrency}'
        // Emit: '${1:0.00} {CatalogCurrency}'
        yield return CreateItem(
            label: $"money — {currencyQualifier.SourceFieldName} currency",
            snippetTemplate: $"${{1:0.00}} {{{currencyQualifier.SourceFieldName}}}",
            ...);
    }
    else
    {
        // Literal qualifier: money in 'USD'
        // Emit: '${1:0.00} USD'
        yield return CreateItem(
            label: $"money — {currencyQualifier.CurrencyCode}",
            snippetTemplate: $"${{1:0.00}} {currencyQualifier.CurrencyCode}",
            ...);
    }
}
else
{
    // Unqualified money: generic template
    yield return CreateItem(
        label: "money — amount + currency",
        snippetTemplate: "${1:0.00} ${2:USD}",
        ...);
}
```

**Then** fall through to existing reused-values and example items.

**Tests:**

| Test | What it verifies |
|---|---|
| `Completions_TypedConstant_Money_Unqualified_ShowsGenericTemplate` | Unqualified money shows `${1:0.00} ${2:USD}` snippet |
| `Completions_TypedConstant_Money_QualifiedUSD_PrefillsCurrency` | `money in 'USD'` shows `${1:0.00} USD` with currency prefilled |
| `Completions_TypedConstant_Money_InterpolatedQualifier_ShowsFieldName` | `money in '{CatalogCurrency}'` shows `${1:0.00} {CatalogCurrency}` |
| `Completions_TypedConstant_Money_QualifiedUSD_NoGenericCurrencyTab` | When currency is fixed, no `${2:USD}` tab stop — the currency is literal |

#### Sub-slice A4: Qualifier-aware quantity templates + dimension-filtered starters

**File:** `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`

**What:** In `GetQuantityLiteralItems` (line 1118), in the `Empty` phase branch, read qualifier context and emit conditional snippet templates. For dimension-qualified quantities, derive the unit list from `UcumCatalog.BrowseTier1()` filtered by `DimensionVector`.

**Logic for exact-unit qualifier (`quantity in 'kg'`):**

```
var unitQualifier = tcContext.Qualifiers.OfType<DeclaredQualifierMeta.Unit>().FirstOrDefault();
if (unitQualifier is not null)
{
    if (unitQualifier.SourceFieldName is not null)
    {
        // Interpolated: quantity in '{StockingUnit}'
        yield return CreateItem(
            label: $"quantity — {unitQualifier.SourceFieldName}",
            snippetTemplate: $"${{1:0}} {{{unitQualifier.SourceFieldName}}}",
            ...);
    }
    else
    {
        // Literal: quantity in 'kg'
        yield return CreateItem(
            label: $"quantity — {unitQualifier.UnitCode}",
            snippetTemplate: $"${{1:0}} {unitQualifier.UnitCode}",
            ...);
    }
}
```

**Logic for dimension qualifier (`quantity of 'mass'`):**

```
var dimQualifier = tcContext.Qualifiers.OfType<DeclaredQualifierMeta.Dimension>().FirstOrDefault();
if (dimQualifier is not null && DimensionCatalog.All.TryGetValue(dimQualifier.DimensionName, out var dimAlias))
{
    // Full filtered catalog, ranked — base units from BrowseTier1 first
    var tier1Units = UcumCatalog.BrowseTier1()
        .Where(atom => atom.Vector == dimAlias.Vector);
    foreach (var atom in tier1Units)
    {
        yield return CreateItem(
            label: $"quantity — {atom.Code}",
            snippetTemplate: $"${{1:0}} {atom.Code}",
            ...);
    }
}
```

**Catalog API chain (non-negotiable):**
- `DimensionCatalog.All` (`src/Precept/Language/Ucum/DimensionCatalog.cs`) — keyed by dimension name → returns `DimensionAlias` with `.Vector`
- `UcumCatalog.BrowseTier1()` (`src/Precept/Language/Ucum/UcumCatalog.cs`) → delegates to `UcumAtomCatalog.BrowseTier1()` → returns curated `IReadOnlyList<UcumAtom>`
- Filter: `atom.Vector == dimAlias.Vector`
- Each `UcumAtom` (`src/Precept/Language/Ucum/UcumAtom.cs`): `.Code`, `.Name`, `.Vector` (`DimensionVector`), `.PrintSymbol`

This is the same pattern already proven in `GetQuantitySlotItems` (lines 1377–1381). No new catalog API needed.

**Tests:**

| Test | What it verifies |
|---|---|
| `Completions_TypedConstant_Quantity_InKg_PrefillsUnit` | `quantity in 'kg'` shows `${1:0} kg` snippet |
| `Completions_TypedConstant_Quantity_InterpolatedUnit_ShowsFieldName` | `quantity in '{StockingUnit}'` shows `${1:0} {StockingUnit}` |
| `Completions_TypedConstant_Quantity_OfMass_ShowsDimensionFiltered` | `quantity of 'mass'` shows dimension-filtered units (kg, g, etc.) from `BrowseTier1` |
| `Completions_TypedConstant_Quantity_OfMass_NoLengthUnits` | `quantity of 'mass'` does NOT include length units (m, cm, etc.) |
| `Completions_TypedConstant_Quantity_Unqualified_ShowsGenericTemplate` | Unqualified quantity shows `${1:0} ${2:each}` or similar generic template |

---

### Slice B — Premium interpolation flow

**Depends on:** Slice A.

#### Sub-slice B1: `{` trigger registration + guard

**File:** `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`

**What:**

1. Add `"{"` to `TriggerCharacters` (line 47):
   ```csharp
   TriggerCharacters = new Container<string>(" ", "'", ".", ">", "~", "{"),
   ```

2. Add a new trigger branch in `GetCompletions` (after the `" "` trigger block at line 83), guarded to fire only inside typed-constant spans:
   ```csharp
   if (triggerCharacter == "{" && IsInsideTypedConstantToken(compilation.Tokens.Tokens, position))
   {
       return CreateCompletionList(GetHoleItems(compilation, position));
   }
   ```

   `IsInsideTypedConstantToken` (used by the `" "` trigger at line 83 and Ctrl+Space at line 135) is the existing guard that inspects raw token kind. Outside a typed constant span, `{` falls through to normal completion routing (which returns empty or irrelevant items for `{` in structural positions).

**Tests:**

| Test | What it verifies |
|---|---|
| `Completions_BraceTrigger_InsideTypedConstant_ShowsHoleItems` | `{` inside `'...'` shows field/arg items from `GetHoleItems` |
| `Completions_BraceTrigger_OutsideTypedConstant_NoFalsePositive` | `{` in a state body or guard expression does NOT produce typed-constant completions |
| `Completions_BraceTrigger_InMoneyLiteral_ShowsCurrencyFields` | `{` inside `'0.00 {` in a money field shows currency-typed fields |

#### Sub-slice B2: Scope-aware interpolation templates

**File:** `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`

**What:** In the initial completion items for money, quantity, and zoneddatetime, detect in-scope fields whose type matches the expected qualifier axis and offer dynamic interpolation templates.

**Mechanism:** Use `compilation.Semantics.Fields` (the flat field list) filtered by `ResolvedType` — same pattern as `GetHoleFieldsOfTypes` (line 2969). For the first pass, all fields in the precept are "in scope" (no cursor-position narrowing — fields are globally visible within a precept definition).

**For money:** Find fields where `field.ResolvedType == TypeKind.Currency`. For each, offer a template: `'${1:0.00} {FieldName}'` with label `money — amount + {FieldName} currency`.

**For quantity:** Find fields where `field.ResolvedType == TypeKind.UnitOfMeasure`. For each, offer templates:
- `'${1:0} {FieldName}'` — single unit hole
- `'${1:1} {FieldName1}/{FieldName2}'` — numerator/denominator (only when ≥2 UoM fields exist)

**For zoneddatetime:** Find fields where `field.ResolvedType == TypeKind.Timezone`. For each, offer: `'${1:2026}-${2:05}-${3:16}T${4:14}:${5:30}:${6:00}[{FieldName}]'`

**Ranking:** Dynamic (scope-aware) templates sort before static generic templates, after qualifier-prefilled templates. Use `CompletionSortGroup.TypedConstant` with appropriate `SortText` prefix.

**Tests:**

| Test | What it verifies |
|---|---|
| `Completions_TypedConstant_Money_ScopeAware_CurrencyFieldInScope` | When a `currency` field exists, shows dynamic `{FieldName}` template |
| `Completions_TypedConstant_Money_ScopeAware_NoCurrencyField_NoTemplate` | When no currency field exists, no scope-aware template appears |
| `Completions_TypedConstant_Quantity_ScopeAware_UoMFieldInScope` | When a `unitofmeasure` field exists, shows dynamic unit template |
| `Completions_TypedConstant_ZonedDateTime_ScopeAware_TimezoneFieldInScope` | When a `timezone` field exists, shows dynamic zone template |

---

### File inventory

| File | Change type | Slice |
|---|---|---|
| `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` | Modify | 0, A, B |
| `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs` | Modify | 0, A, B |

No new files. No files outside the language server and its test project.

### Regression anchors

The following existing tests must stay green across all slices:

| Test | What it covers |
|---|---|
| `Completions_TypedConstant_UseTypeExamples` (line 805) | Existing typed-constant example items still appear |
| `Completions_TypedConstant_SuggestPreviouslyUsedDocumentValues` (line 820) | Reused-value items still appear |
| `Completions_TypedConstant_NoExpectedType_ReturnsEmpty` (line 835) | No spurious items for unresolved types |
| `Completions_TypedConstant_RuleComparison_UsesPeerOperandType` (line 851) | Typed-constant context from rule comparisons |
| `Completions_TypedConstant_InvokedInsideEmptyDefaultLiteral_UsesTypedConstantValues` (line 946) | Ctrl+Space inside `''` default values |
| `Completions_TypedConstant_InvokedInsideEmptyExpressionLiteral_UsesTypedConstantValues` (line 979) | Ctrl+Space inside `''` expression literals |
| `Completions_TypedConstant_NoKeywordsInsideTypedConstantSpan` (line 1012) | No keyword contamination inside typed constants |

### Tooling / MCP sync assessment

- **MCP tools:** Unaffected. No `precept_compile` DTO changes, no `precept_language` vocabulary changes. The MCP server does not expose completion items.
- **TextMate grammar:** Unaffected. No new token types. Typed constants use existing `TypedConstant` / `TypedConstantStart` / `TypedConstantMiddle` / `TypedConstantEnd` tokens.
- **Semantic tokens:** Unaffected. No new semantic token types or modifiers.
- **Hover:** Unaffected. No new hover content.
- **Plugin / agents / skills:** Unaffected. No changes to `.github/agents/` or `.github/skills/`.