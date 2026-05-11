# Typed Literal Completions — Bug Tracker

**Feature:** Typed literal autocomplete (Kramer's implementation, `be2afdde`)  
**Spec:** `docs/Working/elaine-typed-literal-autocomplete-ux.md`  
**Prior review:** Frank-6 — spec compliance BLOCKED (F1, F2)  
**Triage:** Frank-7 — root-cause triage in progress  
**Status:** 8 open bugs

---

## Bugs

### B1 — `default '` → no completions

| | |
|---|---|
| **Trigger** | `field q as quantity default '` |
| **Expected** | Unit completions |
| **Actual** | Nothing |
| **Status** | ✅ Fixed — Kramer (kramer-6) |

**Root cause (Frank-7):** Two cooperating failures in `CompletionHandler.cs`. (1) When `triggerCharacter == "'"` (line 70), `TryGetTypedConstantContext` (line 716) needs `context == SlotContext.InExpression` to reach path #5 (line 755), which calls `TryGetEnclosingField`. But `SlotContextResolver.GetCursorContext` (line 65) sees the `TypedConstant` literal token and returns `SlotContext.TopLevel` — `Literal` matches no keyword/declaration/preposition/control category (SlotContext.cs lines 52–97). (2) `TryGetTypedConstantContext` has no `TopLevel` path for typed constants — paths 5 and 6 (lines 755–765) are skipped, method returns `false`. Result: empty completion list.

**Fix:** Force `innerContext = InExpression` in the `'` trigger branch (line 70) before calling `GetTypedConstantItems` on line 73 — same coercion pattern as the Ctrl+Space branch at line 104. Once context is `InExpression`, `TryGetTypedConstantContext` reaches `TryGetEnclosingField` and returns the field's qualifier metadata.

**Risk:** Low. The `'` trigger is exclusively a typed-constant entry point. No outer-grammar completions affected.

---

### B2 — `' ` → only 3 hardcoded units

| | |
|---|---|
| **Trigger** | `'` then space |
| **Expected** | Full unit catalog |
| **Actual** | Only `kg`, `m/s^2`, `mg/dL` |
| **Status** | 🟢 Fixed — `GetQuantitySlotItems` now calls `UcumCatalog.BrowseTier1()` for full catalog; dimension-filtered when `of '<dim>'` qualifier present |

**Root cause (Frank-7):** `GetQuantitySlotItems` (lines 624–641) falls back to `Types.GetMeta(TypeKind.UnitOfMeasure).ContentValidation?.Examples` when no qualifier is present (line 638–640). This examples list contains only ~3 representative units — not the full UCUM catalog. `GetMoneySlotItems` (line 616) correctly queries `CurrencyCatalog.All.Keys` for the full ISO 4217 set; the quantity equivalent never got the same treatment. (The `IsInsideTypedConstantToken` boundary issue — see B3 — may also affect when this path is reached, but the stub catalog is the primary symptom.)

**Fix:** Replace the examples fallback in `GetQuantitySlotItems` with a full catalog query — model on `GetMoneySlotItems`: query `UnitCatalog.All.Keys` (or equivalent) to enumerate all UCUM tier-1 entries. Verify the catalog is enumerable and returns user-facing unit codes before wiring.

**Risk:** Low-medium. Need to confirm `UnitCatalog` is enumerable and that catalog key format matches what users type as a unit suffix.

---

### B3 — `'5 ` → DSL keywords instead of units

| | |
|---|---|
| **Trigger** | `'5` then space |
| **Expected** | Unit completions |
| **Actual** | `event`, `from`, `field`, `in`, `on`, … (DSL keywords) |
| **Status** | ✅ Fixed — Kramer (kramer-6) |

**Root cause (Frank-7):** `IsInsideTypedConstantToken` (CompletionHandler.cs line 77) calls `Contains(token.Span, position)` (line 1294–1295). `Contains` uses a strict exclusive upper bound: `character < span.EndColumn` (line 1351). When the cursor is at the end of an unterminated `'5 ` token, it sits at exactly `EndColumn` — the check returns `false`, so the space trigger doesn't enter the typed-constant path. Control falls through to the `context switch` (line 112), which dispatches to `GetTopLevelItems()` and returns DSL keywords.

**Fix:** `IsInsideTypedConstantToken` must treat cursor-at-`EndColumn` as "inside" for **unterminated** typed constants (no closing quote). For terminated tokens, `EndColumn` is past the closing quote and should remain "outside". Add a `ContainsOrAtEnd` helper used only in `IsInsideTypedConstantToken` — do NOT modify the shared `Contains` method globally, as it's also used by `TryGetTypedConstantSlotPhase` and other callers.

**Risk:** Medium. Changing `Contains` globally would affect slot-phase detection and other consumers. Scope the fix to a targeted helper. Test terminated vs. unterminated boundary cases carefully.

---

### B4 — `quantity in '` → no completions

| | |
|---|---|
| **Trigger** | `field q as quantity in '` |
| **Expected** | Unit completions |
| **Actual** | Nothing |
| **Status** | ✅ Fixed — Kramer (kramer-9) |

**Root cause (Frank-7):** Kramer fixed B1 and then declared victory where he had not earned it. B4 is **not** a default-expression site. At `field q as quantity in '¦`, `SlotContextResolver.GetCursorContext` does **not** report `TopLevel`; it reports the declaration-side qualifier site (`InModifierPosition`). The `'` trigger branch in `CompletionHandler.cs` (lines 70–75) then bulldozes that context to `InExpression`. From there `TryGetTypedConstantContext` has no qualifier-slot path, so it falls into the enclosing-field fallback at lines 760–763 and returns `TypedConstantContext.FromField(exprField)` — expected type `Quantity`, not `UnitOfMeasure`. `GetTypedConstantItems` line 283 then routes to `GetQuantityLiteralItems`, which is the wrong subsystem entirely. Current tests passed only because `CompletionHandlerTests.cs` lines 658–672 assert “non-empty” and “no keywords”; quantity examples satisfy that weak check, so the regression suite lied.

**Fix:** Stop coercing qualifier literals to expression literals. Add a qualifier-site resolver ahead of the enclosing-field expression fallback: inspect the enclosing declared field/arg type (`QualifiedTypeReference` / `ParsedQualifier` / qualifier-shape metadata), detect that the active qualifier axis is `Unit`, and return qualifier-slot context for `UnitOfMeasure` instead of the outer field type `Quantity`. Then route the `'` completion path to unit-value completions for that slot — and for B4 that means the real UCUM catalog, not the 3-example `GetUnitOfMeasureItems()` stub.

**Risk:** Medium. The structural fix touches all declaration-side qualifier literals. Also: a naive “just return `UnitOfMeasure`” patch still leaves B4 half-broken because `GetUnitOfMeasureItems()` currently serves only example stubs (`kg`, `m/s^2`, `mg/dL`) rather than the full unit catalog.

---

### B5 — `quantity of '` → no completions

| | |
|---|---|
| **Trigger** | `field q as quantity of '` |
| **Expected** | Unit completions |
| **Actual** | Nothing |
| **Status** | ✅ Fixed — Kramer (kramer-9) |

**Root cause (Frank-7):** Same structural defect as B4, different target slot. At `field q as quantity of '¦`, `SlotContextResolver.GetCursorContext` reports `InTypePosition`, which is exactly what a declaration-side `of` qualifier should look like. Kramer’s `'` branch ignores that signal and coerces the site to `InExpression`. `TryGetTypedConstantContext` then misses the qualifier site, falls through to `TryGetEnclosingField` at lines 760–763, and returns the outer field type `Quantity`. `GetTypedConstantItems` line 283 therefore dispatches to `GetQuantityLiteralItems` instead of `GetDimensionItems` (lines 379–382). So the `of` qualifier never gets its own value-domain completions. Again, the regression test at `CompletionHandlerTests.cs` lines 676–690 is too weak to catch this; it only checks that the list is non-empty and keyword-free.

**Fix:** Same architectural repair as B4: add qualifier-slot resolution before the enclosing-field expression fallback. When the active qualifier axis is `Dimension`, return `Dimension` as the expected slot type and route to `GetDimensionItems`, not quantity-literal examples. The test must use the post-insertion source (`field q as quantity of '¦`) and assert real dimension labels (`mass`, `length`, `count`, …), not merely “non-empty”.

**Risk:** Medium. Same qualifier-site resolver change as B4. Lower catalog risk than B4 because the dimension path already points at the real catalog once it is actually reached.

---

### B6 — Money qualifier not filtered in expression context

| | |
|---|---|
| **Trigger** | Field declared `money in 'USD'`, then `rule balance >= '100 ` + space |
| **Expected** | Only `USD` shown |
| **Actual** | All currencies shown |
| **Status** | 🔴 Open |

**Root cause (Frank-7):** `TryGetTypedConstantContext` resolution path #1 (`FindAtPosition`) fails for incomplete literals — the type checker emits `TypedErrorExpression` (not `TypedTypedConstant`) for `'100 `, and `FindAtPosition` filters to `TypedTypedConstant` only. Path #5 (`TryGetEnclosingField`) then fails because the rule construct's `Syntax` reference doesn't match any `compilation.Semantics.Fields` entry (the match at line 1152 uses reference equality). Control reaches path #6: `TryGetBinaryPeerOperandType` (line 761), which resolves `balance` as a `money` type and returns `TypedConstantContext.FromType(peerType)` — with **empty qualifiers**. The `in 'USD'` qualifier declared on the field is never propagated.

**Fix:** When `TryGetBinaryPeerOperandType` resolves a peer operand that is a direct field reference, propagate that field's `DeclaredQualifiers` into the returned `TypedConstantContext`. Change line 763 from `TypedConstantContext.FromType(peerType)` to a variant that also carries the source field's qualifiers. This requires `TryGetBinaryPeerOperandType` to return the source field (or its qualifiers), not just the type. Fix must only propagate when the LHS is a direct field reference — not a computed expression — to avoid over-constraining completions.

**Risk:** Medium. Binary-peer resolution is used for any binary expression with a typed constant RHS. This is the most structurally complex fix — touches the type resolution infrastructure in `TryGetTypedConstantContext`.

---

### B7 — Semantic tokens delta crash

| | |
|---|---|
| **Trigger** | Typing inside a typed literal (changes token stream) |
| **Expected** | No crash |
| **Actual** | `ArgumentOutOfRangeException: Parameter 'length'` in `SemanticTokensDocument.GetSemanticTokensEdits()` |
| **Stack** | `ImmutableArray.Create[T](items, start, length)` → `SemanticTokensDocument.GetSemanticTokensEdits()` → `SemanticTokensHandlerBase.Handle` |
| **Priority** | 🔥 Highest — crash |
| **Status** | 🟢 Fixed — Kramer (kramer-b7-fix, 2026-05-11) |

**Root cause (Frank-7):** `SemanticTokensHandler.cs` registers `Full.Delta = true` (line 46). `Tokenize` pushes merged tokens via `ProjectMergedTokens`, using `token.Span.Length` as the semantic token length (line 108). When the user types inside a typed literal, the `TypedConstant` token's span changes on every keystroke. `NormalizeMergedTokens` (lines 121–171) can also truncate overlapping tokens (line 160), causing the token array layout to shift significantly between successive calls. OmniSharp's `GetSemanticTokensEdits()` diffs the old and new token data arrays; when the layout shifts dramatically, the diff algorithm produces a `SemanticTokensEdit` with an invalid `start` or `deleteCount`, which `ImmutableArray.Create[T](items, start, length)` rejects with `ArgumentOutOfRangeException`.

**Fix:** Invalidate the cached `SemanticTokensDocument` in the `_documents` ConcurrentDictionary (line 29) whenever a typed-constant token's span changes, forcing a full refresh instead of a delta. Option 2 (from Frank): override the delta response handler to catch `ArgumentOutOfRangeException` and fall back to full refresh. Either avoids the crash; option 2 (cache invalidation) is cleaner — forces full refresh only on typed-constant edits rather than swallowing exceptions.

**Risk:** Low. Full refresh is a correctness-preserving degradation. No completion behavior changes.

---

### B8 — `'1 lb'` → Unrecognized UCUM atom 'lb'

| | |
|---|---|
| **Trigger** | `'1 lb'` typed literal value |
| **Expected** | Accepted as valid unit (pound-mass) |
| **Actual** | Error: "Unrecognized UCUM atom 'lb'" |
| **Hypothesis** | UCUM atom catalog/validator missing `lb` |
| **Status** | 🔴 Open |

---

### F1 — Ctrl+Space at `NumberTyping` phase → empty

| | |
|---|---|
| **Trigger** | Ctrl+Space while cursor is in the `NumberTyping` phase of a temporal literal |
| **Expected** | Phase 0 starter completions |
| **Actual** | Empty list |
| **Source** | Frank-6 spec review |
| **Status** | 🔴 Open |

**Root cause (Frank-7):** `GetTemporalSlotItems` (lines 542–553) dispatches on phase with a `switch`. The `NumberTyping` case falls into `_ => []`. The same gap exists in `GetMoneySlotItems` (line 613) and `GetQuantitySlotItems` (line 629): the phase guard `phase is not (AfterNumberSpace or UnitTyping)` means `NumberTyping` returns `[]` for all structured types. The UX spec (Phase 1, Ctrl+Space) requires Phase 0 starter completions here.

**Fix:** Add `NumberTyping or AfterPlusNumber or AfterPlus` → Phase 0 starters branch in the temporal/money/quantity slot handlers. For temporal: `NumberTyping or AfterPlusNumber or AfterPlus => GetTemporalPhase0Starters(tcContext, compilation)`. Apply the equivalent to money and quantity.

**Risk:** Low. Adding completions for a phase that currently returns nothing cannot break existing behavior.

---

### F2 — Ctrl+Space at `AfterPlus` phase → empty

| | |
|---|---|
| **Trigger** | Ctrl+Space while cursor is in the `AfterPlus` phase of a temporal literal |
| **Expected** | Phase 0 starter completions |
| **Actual** | Empty list |
| **Source** | Frank-6 spec review |
| **Status** | 🔴 Open |

**Root cause (Frank-7):** Same switch in `GetTemporalSlotItems` — `AfterPlus` falls into `_ => []`. The UX spec (Phase 4, Ctrl+Space at `+ `) requires segment starter examples such as `30 minutes`, `1 hour`, `15 seconds`.

**Fix:** Covered by the same fix as F1 — `AfterPlus` is included in the Phase 0 starters branch added for F1. Only temporal needs this; money and quantity have no `+` continuation.

**Risk:** Low. Same reasoning as F1.

---

### B9 — Bare integer assignable to `quantity` field (type checker gap)

| | |
|---|---|
| **Trigger** | `field q as quantity of 'mass'` then `set q = 12` |
| **Expected** | Compile error — `12` is not a typed quantity literal |
| **Actual** | Compiles without errors (`hasErrors: false`, no diagnostics) |
| **Repro** | `precept Test` / `field q as quantity of 'mass'` / `event xyz (qq as quantity of 'mass') initial` / `on xyz -> set q = 12` |
| **Component** | Type checker (`TypeChecker`) — integer not widened/rejected correctly when assigned to `quantity` field |
| **Note** | This is a runtime correctness bug, not a completions bug — separate from B1–B8/F1–F2 |
| **Status** | 🔴 Open |

---

### B10 — Qualifier dimension mismatch not caught (mass assigned to length field)

| | |
|---|---|
| **Trigger** | `field q as quantity of 'length'`, then `set q = '5 kg'` |
| **Expected** | Compile error — `kg` is a mass unit; `q` requires a length unit |
| **Actual** | Compiles without errors (`hasErrors: false`, no diagnostics) |
| **Repro** | `field q as quantity of 'length'` / `event xyz (qq as quantity of 'mass') initial` / `on xyz -> set q = '5 kg'` |
| **Component** | Type checker — qualifier dimension is not validated against field declaration |
| **Note** | Together with B9, this confirms the type checker performs **no qualifier-aware quantity validation** — neither unit presence nor dimensional category is enforced |
| **Status** | 🔴 Open |

---

### B11 — Dimension mismatch in field default value not caught

| | |
|---|---|
| **Trigger** | `field q as quantity of 'length' default '5 kg'` |
| **Expected** | Compile error — `'5 kg'` is a mass unit; field requires a length unit |
| **Actual** | Compiles without errors (`hasErrors: false`, no diagnostics) |
| **Repro** | `precept Test` / `field q as quantity of 'length' default '5 kg'` |
| **Component** | Type checker — field declaration default value not validated against qualifier dimension |
| **Note** | Distinct code path from B9/B10 (field default validation vs. `set` action assignment) |
| **Status** | 🔴 Open |

---

### B12 — Typed parameter dimension mismatch not caught in `set` action

| | |
|---|---|
| **Trigger** | `field q as quantity of 'length'`, event arg `qq as quantity of 'mass'`, then `set q = qq` |
| **Expected** | Compile error — `qq` is `quantity of 'mass'`; `q` requires `quantity of 'length'` |
| **Actual** | Compiles without errors (`hasErrors: false`, no diagnostics) |
| **Repro** | `field q as quantity of 'length' default '5 kg'` / `event xyz (qq as quantity of 'mass') initial` / `on xyz -> set q = qq` |
| **Component** | Type checker — typed variable-to-field assignment not validated against qualifier dimension |
| **Note** | B10 covers literal assignment (`set q = '5 kg'`); this covers typed-parameter assignment (`set q = qq`) — different resolution path in the type checker |
| **Status** | 🔴 Open |

---



| File | Relevance |
|---|---|
| `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` | All completion logic — B1–B6, F1–F2 |
| `tools/Precept.LanguageServer/SlotContext.cs` | `GetCursorContext` — B4, B5 (`in`/`of` qualifier routing) |
| `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` | B7 crash |
| `src/Precept/` | B8 — UCUM atom catalog/validator |
| `src/Precept/Pipeline/TypeChecker.cs` | B9 — integer → quantity assignment not rejected |

---

## Triage Notes

**Triaged by:** Frank-7  
**Source file:** `.squad/decisions/inbox/frank-triage-completions-bugs.md`

**Confirmed shared root causes:**

| Root Cause | Bugs |
|---|---|
| `'` trigger doesn't coerce inner context to `InExpression` | B1 |
| Declaration-side qualifier literals are being coerced to enclosing-field expression literals | B4, B5 |
| `IsInsideTypedConstantToken` boundary exclusion at `EndColumn` | B3 (+ partially B2) |
| `Get*SlotItems` don't handle `NumberTyping`/`AfterPlus` phases | F1, F2 |

**B8** (UCUM atom `lb` rejected): Frank did not cover — separate catalog/validator issue in `src/Precept/`.  
**B9/B10/B11/B12** (type checker quantity validation gaps): Frank did not cover — surfaced after Frank was already running; separate issue in `TypeChecker.cs`.

**Priority order (Kramer):**
1. B7 — crash
2. B1 — quote-trigger expression/default context
3. B4/B5 — qualifier-site routing (B4 also needs real unit catalog items)
4. B3 — boundary fix (careful with `Contains`)
5. B2 — catalog stub replacement
6. F1/F2 — Ctrl+Space fallback
7. B6 — qualifier propagation in binary expressions
8. B8 — UCUM atom catalog gap

---

## Resolution Log

_Updates will be added here as bugs are fixed._

---

### B13 — UCUM unit completions show cryptic codes instead of human-readable labels

| | |
|---|---|
| **Trigger** | Autocomplete after `'5 ` in a quantity field |
| **Expected** | Labels like `oz` (avoirdupois ounce), `lb` (pound avoirdupois) |
| **Actual** | Labels like `[oz_av]`, `[lb_av]` |
| **Status** | ✅ Fixed — Kramer (kramer-10) |

**Root cause:** `GetQuantitySlotItems` used `u.Code` as the completion label. `UcumAtom` had no `PrintSymbol` field. Tier-1 list included troy (`[oz_tr]`, `[pwt_tr]`) and apothecary (`[oz_ap]`, `[lb_ap]`) variants that are never relevant for business users.

**Fix:** Added `PrintSymbol` field to `UcumAtom` (parsed from the UCUM XML `printSymbol` element/attribute). Changed completion label to `printSymbol ?? code`, detail to `u.Name`, insertText to `u.Code` (always — UCUM codes are what get typed). Removed troy/apothecary units from tier-1 while keeping `[gr]` because the embedded XML classifies it as `avoirdupois`. Enhanced hover to show unit name alongside code.

---

### B14 — Semantic tokens delta crash: ArgumentOutOfRangeException in ImmutableArray.Create

| | |
|---|---|
| **Trigger** | Editing a `.precept` file while the language server is running |
| **Expected** | Semantic tokens updated incrementally without error |
| **Actual** | `ArgumentOutOfRangeException: Specified argument was out of the range of valid values (Parameter 'length')` in `SemanticTokensDocument.GetSemanticTokensEdits()` |
| **Status** | ✅ Fixed — Kramer (kramer-11) |

**Stack trace:** `ImmutableArray.Create[T](items, start, length)` → `SemanticTokensDocument.GetSemanticTokensEdits()` → `SemanticTokensDeltaPipeline.Handle`

**Root cause (Kramer):** OmniSharp's `SemanticTokensDocument` keeps a single per-document `Id`, so the framework cannot tell whether a client `PreviousResultId` refers to the latest semantic-token baseline or an older one. After one successful delta request, `_prevData` stays armed inside the framework document; a later delta against a stale client result can therefore diff against the wrong baseline and produce an invalid `start`/`length` slice in `ImmutableArray.Create(...)`. This is not related to the UCUM display-label work — `SemanticTokensHandler` never touches `UcumAtom`, `PrintSymbol`, or quantity-completion metadata.

**Fix:** The language server now stamps its own client-visible semantic-token result IDs on every full or delta response, tracks the latest `(resultId, SemanticTokensDocument.Id)` per URI, and falls back to a full semantic-tokens response whenever the client requests delta from a stale baseline or after typed-constant span invalidation replaced the framework document. Added handler-level regression tests for stale result IDs and typed-constant span changes, and validation passed with `dotnet build tools/Precept.LanguageServer/Precept.LanguageServer.csproj --artifacts-path temp/dev-language-server` plus `dotnet test test/Precept.LanguageServer.Tests/`.
