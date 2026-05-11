# Typed Literal Completions — Bug Tracker

**Feature:** Typed literal autocomplete (Kramer's implementation, `be2afdde`)  
**Spec:** `docs/Working/elaine-typed-literal-autocomplete-ux.md`  
**Prior review:** Frank-6 — spec compliance BLOCKED (F1, F2)  
**Triage:** Frank-7 — root-cause triage in progress  
**Status:** 8 open bugs + 2 crashes (X1 P0, X2 P2) + 3 interpolation bugs (I1–I3) + 1 catalog gap (C1 ✅ Fixed)

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
| **Status** | ✅ Fixed (`65badacb`) |

**Root cause (Frank-7):** `TryGetTypedConstantContext` resolution path #1 (`FindAtPosition`) fails for incomplete literals — the type checker emits `TypedErrorExpression` (not `TypedTypedConstant`) for `'100 `, and `FindAtPosition` filters to `TypedTypedConstant` only. Path #5 (`TryGetEnclosingField`) then fails because the rule construct's `Syntax` reference doesn't match any `compilation.Semantics.Fields` entry (the match at line 1152 uses reference equality). Control reaches path #6: `TryGetBinaryPeerOperandType` (line 761), which resolves `balance` as a `money` type and returns `TypedConstantContext.FromType(peerType)` — with **empty qualifiers**. The `in 'USD'` qualifier declared on the field is never propagated.

**Fix:** When `TryGetBinaryPeerOperandType` resolves a peer operand that is a direct field reference, propagate that field's `DeclaredQualifiers` into the returned `TypedConstantContext`. Change line 763 from `TypedConstantContext.FromType(peerType)` to a variant that also carries the source field's qualifiers. This requires `TryGetBinaryPeerOperandType` to return the source field (or its qualifiers), not just the type. Fix must only propagate when the LHS is a direct field reference — not a computed expression — to avoid over-constraining completions.

**Risk:** Medium. Binary-peer resolution is used for any binary expression with a typed constant RHS. This is the most structurally complex fix — touches the type resolution infrastructure in `TryGetTypedConstantContext`.

---

#### B6 Triage — Frank (2026-05-11)

**Root cause — exact location:**

The defect is entirely in `CompletionHandler.cs` (LS side). The call chain is:

1. `TryGetTypedConstantContext` (line 728) exhausts paths #1–#5, reaching path #6 at line 778.
2. `TryGetBinaryPeerOperandType` (line 947) walks tokens left to find the binary operator, then calls `TryResolveExpressionTypeEndingAtToken` (line 971) on the LHS.
3. `TryResolveExpressionTypeEndingAtToken` (line 974) dispatches to `TryResolveIdentifierType` (line 999) for the `balance` identifier.
4. `TryResolveIdentifierType` (line 1064) looks up `compilation.Semantics.FieldsByName["balance"]` at line 1079, retrieves the `TypedField` — which carries both `ResolvedType: Money` **and** `DeclaredQualifiers: [Currency("USD")]` — but returns only `field.ResolvedType` (line 1081). The qualifiers are right there but discarded.
5. Back at line 780, the context is created via `TypedConstantContext.FromType(peerType)`, which produces empty qualifiers.
6. `GetMoneySlotItems` (line 618) and `GetCurrencyCodeItems` (line 365) both check `tcContext.Qualifiers.OfType<DeclaredQualifierMeta.Currency>()` for filtering. With empty qualifiers, no filtering occurs — all 156 ISO 4217 currencies are shown.

The downstream consumer code is already correct. Only the data-assembly path is broken.

**Fix location:** Kramer — LS side only (`CompletionHandler.cs`). No pipeline changes needed.

**Interaction with B9-B12 Slices 1–5:**

Slice 4 (commit `cdec821f`) added `DeclaredQualifiers` to `TypedConstantContext` (in `TypedConstantParseResult.cs` line 24) and created the `FromField` / `FromArg` factory methods on the LS-local `TypedConstantContext` record (lines 26–30). The B6 fix has a **much cleaner path** now:

- `TypedConstantContext` already knows how to carry qualifiers (the record has the `Qualifiers` field).
- `FromField` already extracts `field.DeclaredQualifiers` — so if the binary peer resolution can return a `TypedField` instead of a bare `TypeKind`, the existing factory does all the work.
- `GetMoneySlotItems` and `GetCurrencyCodeItems` already consume qualifiers for filtering — no downstream changes needed.

Before Slice 4, this bug would have required adding qualifier support to `TypedConstantContext` itself. Now it's purely a plumbing fix in the binary peer resolution helpers.

**Proposed fix approach:**

1. **Widen `TryGetBinaryPeerOperandType` signature** — change from `out TypeKind expectedType` to also return `out ImmutableArray<DeclaredQualifierMeta> qualifiers` (or return a `TypedConstantContext` directly). A second `out` parameter is simpler and lower-risk than changing the return type.

2. **Widen `TryResolveExpressionTypeEndingAtToken`** — same signature change, threading the qualifiers through the dispatch.

3. **Widen `TryResolveIdentifierType`** — when the identifier resolves to a field (line 1079), also return `field.DeclaredQualifiers`. When it resolves to an event arg (line 1071), return `arg.DeclaredQualifiers` (Slice 4 added qualifiers to `TypedArg` too). For non-field/non-arg resolution (computed expressions, function calls, parenthesized expressions), return empty qualifiers — this is correct because computed values don't have declared qualifiers.

4. **Update line 780** — change from `TypedConstantContext.FromType(peerType)` to `new TypedConstantContext(peerType, qualifiers)` using the newly-returned qualifier data.

5. **No changes to `GetMoneySlotItems`, `GetCurrencyCodeItems`, or any other downstream consumer** — they already filter on qualifiers.

The same fix also benefits `quantity` binary peers: `total + '5 ` where `total` is `quantity in 'kg'` would now filter to mass-dimensional units. This is free from the same plumbing change.

**Risk:** Low-medium. The change is additive — all existing paths that return empty qualifiers today continue to do so for computed expressions. Only direct field-ref and arg-ref peers gain qualifier propagation. Regression risk is limited to:

- Binary expressions where the LHS is a field with qualifiers — verify the qualifier appears in completions.
- Binary expressions where the LHS is a computed expression — verify qualifiers remain empty (no false constraint).
- Money and quantity types are both affected — test both axes.

**Scope estimate:** **S (Small).** Four method signatures gain a qualifier out-parameter, one call site at line 780 uses it. No new infrastructure, no pipeline changes, no new diagnostic codes. The hard part (getting qualifiers into `TypedConstantContext`) was already done in Slice 4.

---

### B7 — Semantic tokens delta crash

| | |
|---|---|
| **Trigger** | Typing inside a typed literal (changes token stream) |
| **Expected** | No crash |
| **Actual** | `ArgumentOutOfRangeException: Parameter 'length'` in `SemanticTokensDocument.GetSemanticTokensEdits()` |
| **Stack** | `ImmutableArray.Create[T](items, start, length)` → `SemanticTokensDocument.GetSemanticTokensEdits()` → `SemanticTokensHandlerBase.Handle` |
| **Priority** | 🔥 Highest — crash |
| **Status** | ✅ Fixed — Kramer (kramer-11, commit ef7374dd) |

**Root cause:** OmniSharp framework result IDs allowed stale baselines; fix: server mints own result IDs, detects stale baselines, returns full payload when delta is unsafe.

**Fix:** Server mints its own result IDs, detects stale baselines, and returns a full payload when delta is unsafe.

**Risk:** Low. Full refresh is a correctness-preserving degradation. No completion behavior changes.
---

### B8 — `'1 lb'` → Unrecognized UCUM atom 'lb'

| | |
|---|---|
| **Trigger** | `'1 lb'` typed literal value |
| **Expected** | Accepted as valid unit (pound-mass) |
| **Actual** | Error: "Unrecognized UCUM atom 'lb'" |
| **Hypothesis** | UCUM atom catalog/validator missing `lb` |
| **Status** | ✅ Covered — UCUM display fix (kramer-10) — autocomplete now inserts `[lb_av]` via `lb` printSymbol label, so users reach valid UCUM without typing bare `lb` |

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
| **Status** | ✅ Fixed — Slice 2 (`8a601e0d`) |

**Root cause (Frank):** `ResolveAction` in `TypeChecker.Expressions.cs` (lines 810–822, `AssignAction` case) resolves the assignment value by calling `Resolve(assign.Value, ctx, fieldType)` with `fieldType = Quantity`, but **never validates the resolved expression's `ResultType` against the target `fieldType`**. The `expectedType` parameter is only an advisory hint — it propagates through `ResolveLiteral` → `ResolveNumericLiteral` (line 161), which checks `IsAssignable(Integer, Quantity)`. Integer does NOT widen to Quantity (`IntegerWidens = [Decimal, Number]` — Types.cs line 54), so the integer literal keeps its `TypeKind.Integer` result type. But since `ResolveAction` never checks `value.ResultType != fieldType`, the mismatch goes undetected. The `TypedInputAction` record is created with `FieldType = Quantity` and `InputExpression.ResultType = Integer` — contradictory types accepted silently.

**Fix:** Add a post-resolution type compatibility check in the `AssignAction` case of `ResolveAction`. After `var value = Resolve(...)`, check `!IsAssignable(value.ResultType, fieldType)` and emit `DiagnosticCode.TypeMismatch` if incompatible. Suppress when either side is `TypeKind.Error` (already handled by `IsAssignable`). This is the same gap for all action kinds that assign values to fields — the check should apply uniformly.

**Risk:** Low. `IsAssignable` already handles error suppression. The check is a straightforward guard. Must verify no existing tests depend on silent integer→quantity acceptance.

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
| **Status** | ✅ Fixed — Slices 3+4 (`94b38f96`, `cdec821f`) |

**Root cause (Frank):** Two independent gaps combine. (1) `QuantityValidator.Validate` (`QuantityValidator.cs` lines 11–32) validates that the literal is syntactically `<number> <UCUM-unit>` and that the unit is UCUM-valid, but is **structurally blind to the field's declared qualifier** — it never receives the field's `DeclaredQualifiers` and never checks whether the unit's dimension matches the declared dimension. The `TypedConstantContext` parameter exists but is unused. (2) Same as B9 — `ResolveAction` never validates the resolved expression against the target field's qualifiers. Even if `QuantityValidator` returned `TypeKind.Quantity` successfully, no post-resolution check compares the literal's unit dimension against the field's `DeclaredQualifiers[Dimension]`. The diagnostic codes exist: `DimensionCategoryMismatch` (PRE0069) and `QualifierMismatch` (PRE0068) — they are just never emitted in this path.

**Fix:** Two-layer fix. (1) Thread the field's `DeclaredQualifiers` into `QuantityValidator.Validate` via the existing `TypedConstantContext` parameter. After UCUM parsing succeeds, derive the literal's dimension from the parsed unit (same logic as `DeriveUnitDimensionName` in TypeChecker.cs line 188), compare it against the context's declared dimension qualifier, and emit `DimensionCategoryMismatch` if they differ. (2) Add the post-resolution assignment-level qualifier check from the B9 fix — this catches the case even when validation is bypassed or deferred.

**Risk:** Medium. Threading qualifier context into the validator requires plumbing `DeclaredQualifiers` from `TypedField` into `ResolveTypedConstant` → `TypedConstantValidation.Validate` → `QuantityValidator.Validate`. The `TypedConstantContext` parameter already exists for this purpose but is currently a stub.

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
| **Status** | ✅ Fixed — Slices 4+5 (`cdec821f`, this slice) |

**Root cause (Frank):** Same validator-level gap as B10, different call site. `ResolveFieldExpressions` in `TypeChecker.cs` (line 452) calls `Resolve(defaultMod.Value, ctx, typedField.ResolvedType)`, which routes to `ResolveTypedConstant` → `QuantityValidator.Validate`. As with B10, the validator succeeds because it only checks UCUM syntax, not dimension compatibility. The field's `DeclaredQualifiers` are already populated (line 296, `ExtractQualifiers`), but they are never passed to the validator. There is also no post-resolution qualifier check in `ResolveFieldExpressions` — the resolved expression is stored directly at line 453 without any compatibility validation.

**Fix:** Same two-layer approach as B10. (1) Thread `DeclaredQualifiers` from the owning `TypedField` into `ResolveTypedConstant` so the validator can check dimension at parse time. (2) Add a post-resolution qualifier check in `ResolveFieldExpressions` after line 452 — compare the resolved literal's unit dimension against `typedField.DeclaredQualifiers` and emit `DimensionCategoryMismatch` if they differ. The `ResolveFieldExpressions` fix is structurally simpler than the `ResolveAction` path because the target field is directly available.

**Risk:** Low. `ResolveFieldExpressions` has the `TypedField` in scope — no additional plumbing. Same validator enhancement as B10.

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
| **Status** | ✅ Fixed — Slice 3 (`94b38f96`) |

**Root cause (Frank):** `ResolveIdentifier` in `TypeChecker.Expressions.cs` (line 549) resolves event arg `qq` to `TypedArgRef(ResultType: Quantity, ...)`. The `TypedArgRef` record carries only `ResultType` (a `TypeKind`) — it has **no qualifier metadata**. The arg's `DeclaredQualifiers` are computed and stored on `TypedArg` (line 412, `ExtractQualifiers`), but the `TypedArgRef` expression node strips them. So even if the `ResolveAction` post-resolution check from the B9 fix were added, it would only compare `TypeKind.Quantity == TypeKind.Quantity` — a match — and miss the qualifier dimension mismatch entirely. This is the deepest of the four bugs: fixing it requires carrying qualifier information through the expression tree, not just checking types.

**Fix:** (1) Extend `TypedArgRef` (or `TypedExpression` more broadly) to carry `DeclaredQualifiers` alongside `ResultType`. When resolving an event arg at `ResolveIdentifier` line 549, propagate `arg.DeclaredQualifiers` into the expression node. (2) The post-resolution assignment check in `ResolveAction` then compares not just `IsAssignable(value.ResultType, fieldType)` but also validates qualifier compatibility between the expression's qualifiers and the target field's qualifiers. For `Dimension` qualifiers, require value match; for `Unit` qualifiers, derive the source unit's dimension and compare. (3) The same qualifier-carrying extension applies to `TypedFieldRef` (line 571) for field-to-field assignments — `TypedFieldRef` also strips qualifiers today.

**Risk:** Medium-high. This is a structural change to the expression tree model — `TypedExpression` subclasses gain a qualifier dimension. All existing consumers of the expression tree need auditing to ensure they tolerate the new metadata. However, the data is optional (nullable) and additive, so backward compatibility risk is contained.

---

### X1 — D26 assertion crash: interpolated typed constant kills language server

| | |
|---|---|
| **Trigger** | Opening or editing any `.precept` file containing an interpolated typed constant (e.g., `'1 {s}'` assigned to a quantity field) |
| **Expected** | Error diagnostic (interpolated typed constants not yet supported) without crashing |
| **Actual** | Fixed — the stub now emits an Error diagnostic and returns normally instead of tripping D26 / `FailFast`. |
| **Severity** | **P0** — server-killing, blocks all editing |
| **Status** | ✅ Fixed |
| **Owner** | Unassigned |

**Root cause (Frank):** `ResolveLiteral` in `TypeChecker.Expressions.cs:141` has a deferred stub for `TypedConstantStart` tokens:
```csharp
TokenKind.TypedConstantStart => new TypedErrorExpression(lit.Span), // Interpolated typed constants deferred
```
This returns `TypedErrorExpression` without emitting an Error-severity diagnostic. D26 requires that every `TypedErrorExpression` in the semantic index has a corresponding Error diagnostic. The assertion at `TypeChecker.cs:592` detects the violation and aborts the process.

**Parser path:** `ParseInterpolatedTypedConstant()` (Parser.Expressions.cs:444) creates a `LiteralExpression(TokenKind.TypedConstantStart, ...)` — a placeholder that skips interpolation segments — which then hits the deferred stub in the type checker.

**Fix:** `ResolveLiteral` now routes `TypedConstantStart` through a lightweight TC diagnostic path before returning `TypedErrorExpression`, reusing `DiagnosticCode.TypeMismatch` as the existing Error-severity self-contained code. The message explicitly identifies the unsupported interpolated typed constant stub, satisfying D26 and preventing the crash while leaving full interpolation support deferred.

**Related:** C2 design conflict (interpolation in typed constants) — frank-12's interpolation triage. Long-term fix is implementing interpolated typed constant support; short-term fix is the diagnostic emission.

---

### X2 — Extension JS `this.task is not a function` on LS crash

| | |
|---|---|
| **Trigger** | LS process crash (X1) during pending document change delivery |
| **Expected** | Graceful client-side error recovery |
| **Actual** | `TypeError: this.task is not a function` at `extension.js:35:44428` — logged as `[Error] Delivering pending changes failed` |
| **Severity** | **P2** — secondary symptom, no independent trigger |
| **Status** | ✅ Closed — no independent fix needed; trigger eliminated by X1 |
| **Owner** | N/A — `vscode-languageclient` library internal |

**Root cause (Frank):** The crash occurs inside the bundled `vscode-languageclient` library (v9.x), not in our extension TypeScript. The minified code shows a debounced document-sync `Delayer` pattern where `this.task` is a scheduled callback. When the LS process crashes abnormally, the pending promise resolves into the callback path after `this.task` was already cleared during connection teardown. This is a race condition in the library triggered by abnormal LS termination.

**Fix approach:** No action needed. Fixing X1 eliminates the LS crash that triggers X2. If defense-in-depth is desired, upstream the race fix to `vscode-languageclient`, but this is not actionable on our side.

---

| File | Relevance |
|---|---|
| `src/Precept/Pipeline/TypeChecker.Expressions.cs` | **X1** — `ResolveLiteral` line 141 deferred `TypedConstantStart` stub returns `TypedErrorExpression` without diagnostic; B9, B10, B12 — `ResolveAction` (lines 810–822) missing post-resolution type/qualifier check; `ResolveIdentifier` (line 549) strips qualifier metadata from `TypedArgRef` |
| `src/Precept/Pipeline/TypeChecker.cs` | **X1** — D26 assertion at `PopulateEventHandlers` line 592; B10, B11 — `ResolveFieldExpressions` (line 452) missing qualifier check on default values; `ExtractQualifiers` (line 130) correctly builds qualifier metadata but it's never consumed downstream |
| `src/Precept/Pipeline/Parser.Expressions.cs` | **X1** — `ParseInterpolatedTypedConstant` line 444 creates `LiteralExpression(TypedConstantStart)` placeholder |
| `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` | All completion logic — B1–B6, F1–F2 |
| `tools/Precept.LanguageServer/SlotContext.cs` | `GetCursorContext` — B4, B5 (`in`/`of` qualifier routing) |
| `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` | B7 crash |
| `src/Precept/` | B8 — UCUM atom catalog/validator |
| `src/Precept/Language/QuantityValidator.cs` | B10, B11 — validates UCUM syntax only, dimension-blind — `TypedConstantContext` parameter unused |
| `src/Precept/Pipeline/SemanticIndex.cs` | B12 — `TypedArgRef`, `TypedFieldRef` lack qualifier metadata on expression nodes |
| `src/Precept/Language/Types.cs` | B9 — `IntegerWidens` does not include `Quantity` (correct behavior, but exposed the missing assignment check) |

---

## Triage Notes

**Triaged by:** Frank-7  
**Source file:** `.squad/decisions/inbox/frank-triage-completions-bugs.md`

**Confirmed shared root causes:**

| Root Cause | Bugs |
|---|---|
| `ResolveLiteral` returns `TypedErrorExpression` for `TypedConstantStart` without emitting a diagnostic | X1 (D26 crash) |
| `vscode-languageclient` debounced task race on abnormal LS termination | X2 (secondary) |
| `'` trigger doesn't coerce inner context to `InExpression` | B1 |
| Declaration-side qualifier literals are being coerced to enclosing-field expression literals | B4, B5 |
| `IsInsideTypedConstantToken` boundary exclusion at `EndColumn` | B3 (+ partially B2) |
| `Get*SlotItems` don't handle `NumberTyping`/`AfterPlus` phases | F1, F2 |

**B8** (UCUM atom `lb` rejected): Frank did not cover — separate catalog/validator issue in `src/Precept/`.  
**B9/B10/B11/B12** (type checker quantity validation gaps): Triaged by Frank. **Shared root cause:** the type checker performs no post-resolution type or qualifier compatibility check on assignment targets — `ResolveAction` and `ResolveFieldExpressions` both resolve values with an `expectedType` hint but never verify the result matches. Additionally, `QuantityValidator` is dimension-blind, and expression nodes (`TypedArgRef`, `TypedFieldRef`) strip qualifier metadata. See individual bug entries for specifics.

**Priority order (Kramer):**
1. **X1 — D26 assertion crash (P0)** — server-killing, blocks all editing
2. B7 — crash
3. B1 — quote-trigger expression/default context
4. B4/B5 — qualifier-site routing (B4 also needs real unit catalog items)
5. B3 — boundary fix (careful with `Contains`)
6. B2 — catalog stub replacement
7. F1/F2 — Ctrl+Space fallback
8. B6 — qualifier propagation in binary expressions
9. B8 — UCUM atom catalog gap
10. X2 — resolves automatically when X1 is fixed

---

### C1 — Count/logistics unit catalog gap

| | |
|---|---|
| **Trigger** | `field qty as quantity of 'count' default '10 ` + space → no business count units in completions |
| **Expected** | Units like `each`, `pkg`, `box`, `case`, `pallet`, `doz`, `pair`, etc. |
| **Actual** | Only scientific count units: `1` (unity), `%`, `[ppm]`, `[ppb]`, `[ppth]`, `[pptr]`, `[pH]`, `[iU]`, `[arb'U]`, `[USP'U]`, `[CFU]`, `dB` |
| **Component** | `UcumAtomCatalog.cs` (Tier1Codes array + SeedIntrinsicAtoms), `UnitDimensionHelper.cs` (CountQualifierUnitCodes) |
| **Status** | ✅ Fixed — George (C1) — commit `9a790d81` |

**Problem:** The tier-1 UCUM catalog contains ~150 curated units spanning physical measurement (length, mass, volume, temperature, etc.) but has zero business/logistics count units. The only count-adjacent entry is `each`, which exists as a synthetic intrinsic atom in `SeedIntrinsicAtoms` (line 300) but is **not in the `Tier1Codes` array** — so it is not browsable via `BrowseTier1()` and does not appear in completions. A user writing `quantity of 'count'` gets no useful units. Standard UCUM is oriented toward scientific and physical measurement — it does not define units for packages, boxes, cartons, or pallets.

**Design options considered:**

- **Option A — UCUM arbitrary units** (`{each}`, `{pkg}`): Technically UCUM-compliant, but curly-brace syntax is unintuitive for business users. No `PrintSymbol`, no `Name` in the UCUM XML. Completions would show `{each}` — users won't recognize it.
- **Option B — Curated business units as Precept-defined tier-1 atoms** ✅ **RECOMMENDED**: Define synthetic atoms in `SeedIntrinsicAtoms` with clean codes (`each`, `pkg`, `box`, etc.), human-readable `Name`, and `PrintSymbol`. Add to `Tier1Codes`. Add to `CountQualifierUnitCodes`. Completions show `each (each)`, `pkg (package)`, `box (box)` — exactly what a business user expects. Precedent: `each` already follows this pattern.
- **Option C — UCUM annotations with metadata overlay** (`{each}` canonical, `each` display): Unnecessary complexity. Curly braces leak into typed literals (`'10 {each}'`), error messages, and UCUM validation paths. Option B gives the same UX without the indirection.
- **Option D — New `count of '...'` qualifier tier**: Requires a language construct change and design gate. Overkill for what is a data gap in the catalog.

**Recommended approach: Option B.**

Rationale:
1. **Precedent exists.** `each` is already a synthetic Precept-defined atom in `SeedIntrinsicAtoms`. Adding more follows the identical pattern — no architectural change.
2. **Catalog-driven architecture is satisfied.** These are catalog entries with `Code`, `Name`, `PrintSymbol`, `Vector: DimensionVector.None`, `Dimension: count`. All downstream surfaces (completions, hover, validation) derive from the catalog automatically.
3. **No language construct change.** This is purely new catalog data. No design gate required.
4. **Business-user UX.** Users type `'10 each'`, `'5 pkg'`, `'20 box'` — clean, readable, no curly braces.

**Proposed tier-1 count/logistics units:**

| Code | Name | PrintSymbol | Dimension |
|------|------|-------------|-----------|
| `each` | each | ea | count |
| `piece` | piece | pc | count |
| `unit` | unit | U | count |
| `item` | item | item | count |
| `pair` | pair | pr | count |
| `dozen` | dozen | doz | count |
| `gross` | gross | gr | count |
| `set` | set | set | count |
| `bundle` | bundle | bdl | count |
| `pkg` | package | pkg | count |
| `box` | box | box | count |
| `case` | case | cs | count |
| `carton` | carton | ctn | count |
| `pallet` | pallet | plt | count |
| `bag` | bag | bag | count |
| `drum` | drum | drm | count |
| `container` | container | ctr | count |
| `tote` | tote | tote | count |
| `bin` | bin | bin | count |
| `roll` | roll | roll | count |
| `sheet` | sheet | sht | count |
| `tablet` | tablet | tab | count |
| `capsule` | capsule | cap | count |
| `dose` | dose | dose | count |

**Implementation scope:** S (Small) — catalog data only.

**Changes required (3 files, read-only for this triage):**

1. `src/Precept/Language/Ucum/UcumAtomCatalog.cs`:
   - Add 24 synthetic atoms to `SeedIntrinsicAtoms` (same pattern as existing `each` at line 300).
   - Add all 24 codes to the `Tier1Codes` array in the `COUNT / DIMENSIONLESS` section (line 164).
   - Note: `each` is already in `SeedIntrinsicAtoms` but must also be added to `Tier1Codes`.

2. `src/Precept/Language/UnitDimensionHelper.cs`:
   - Add all 24 codes to `CountQualifierUnitCodes` (line 7) so `DeriveUnitDimensionName` returns `"count"` for them.

3. MCP tool sync: `precept_domains` output (units section) will automatically pick up the new tier-1 entries via `BrowseTier1()` — no manual MCP change needed.

**No design gate needed.** This is catalog data addition following an established pattern.

**Implementation owner:** Kramer — catalog data + completions verification.

---

### C2 — User-defined unit extensibility assessment

| | |
|---|---|
| **Question** | Can users define custom unit codes (e.g., `quantity in 'widget'`) beyond UCUM-standard and Precept-shipped atoms? |
| **Date** | 2026-05-11 |
| **Author** | Frank |
| **Status** | 🟡 Deferred — ship C1, revisit extensibility post-V1 |

**Design assessment:**

The question is whether `quantity in '...'` should accept user-defined codes — not just UCUM and Precept-shipped atoms — with dimensional enforcement. The answer is: **yes, it's feasible and architecturally clean, but it's premature to ship before C1 lands and the type checker's qualifier enforcement is sound (B9–B12).**

**Feasibility:** If a user writes `quantity in 'widget'`, the system treats `widget` as an opaque custom dimension. `DeriveUnitDimensionName` currently returns a dimension string from catalog metadata. For an unknown code, the only derivable dimension is the code itself — so `widget` gets dimension `"widget"` and `sprocket` gets dimension `"sprocket"`. Assignment between them would trigger `DimensionCategoryMismatch` (PRE0069). This works cleanly with no special-casing.

**Enforcement model:** Custom units are self-dimensioned. `field w as quantity in 'widget'` and `field x as quantity in 'sprocket'` are dimensionally incompatible. `set w = x` fails with PRE0069. Two fields both declared `quantity in 'widget'` are compatible. This is the correct behavior — the dimension system treats unknown codes as distinct singletons.

**Recommended approach: D — Not now. Ship C1 first, revisit user extensibility post-V1.**

Rationale:
1. **C1 covers the immediate need.** 24 logistics units solve the actual user pain (no business count units). User-defined units solve a hypothetical need that no current user has hit.
2. **B9–B12 must land first.** Qualifier enforcement is currently broken — the type checker is dimension-blind on assignments. Building extensibility atop a broken enforcement layer is architectural malpractice.
3. **Typo risk is real and unsolved.** `quantity in 'widgit'` silently creates a distinct dimension. Without a declaration mechanism, the system cannot warn about likely typos. This is a footgun that contradicts Precept's "prevention, not detection" philosophy.
4. ~~**When we do ship it, Approach B (UCUM arbitrary unit syntax) is the winner.** `quantity in '{widget}'` for user-defined, `quantity in 'kg'` for built-ins. Visual distinction at the surface, UCUM-precedented, no new language constructs.~~ **⚠️ INVALIDATED (2026-05-11): Approach B has a fatal conflict with string/typed-constant interpolation.** Precept uses `{expr}` inside both `"..."` and `'...'` for interpolation (spec §1.3, §1.7, §2.5). Writing `quantity in '{widget}'` is ambiguous: the lexer interprets `{widget}` as an interpolation hole containing the identifier `widget`, not as a UCUM arbitrary-unit curly-brace code. This is not a tooling gap — it's a lexer-level ambiguity that cannot be resolved without either changing the interpolation syntax or using a different delimiter for user-defined units. **Approach B is dead.**
5. **Approach A (implicit acceptance of unknowns) is rejected.** Silent acceptance with no declaration violates discoverability and prevention principles. A typo becoming a valid distinct dimension is the opposite of what Precept guarantees.
6. **Approach C (explicit declaration construct) is overkill for V1** but may be the right long-term answer if user-defined units proliferate across precepts in a project.

**Alternative approach candidates (post-Approach-B invalidation):**
- **Approach E — Bracketed custom units:** `quantity in '[widget]'`. Square brackets are already used in UCUM for annotation codes (e.g., `[lb_av]`), and the lexer does not give `[` special meaning inside typed constants. Visually distinct from catalog codes, no interpolation conflict. Requires validation that `[...]` inside a typed constant isn't confused with list-literal syntax (it wouldn't be — list literals are expression-level, not inside quotes).
- **Approach F — Prefix marker:** `quantity in 'custom:widget'`. A namespace-style prefix. No delimiter conflict. Less elegant but unambiguous.
- **Approach C (revisited):** Explicit declaration at the precept level (`unit widget`) with plain use in qualifiers (`quantity in 'widget'`). The declaration prevents typos (the original Approach A problem) and avoids any delimiter conflict. May be the strongest candidate if the typo-prevention argument is weighty.

**Future design direction (post-V1):**
- ~~Approach B as the language surface~~ — invalidated; see above.
- Evaluate Approaches E, F, and C-revisited as replacements. All three avoid the interpolation conflict and address typo risk differently.
- Lint diagnostic: consistency check across fields regardless of which approach wins.
- Completions: user-defined codes harvested from the current precept and offered alongside catalog entries, visually distinguished.

**Design gate:** Track A when we revisit. Whichever approach replaces B, the enforcement and tooling additions warrant a design review.

**Decision filed:** `.squad/decisions/inbox/frank-user-defined-units.md`

---

### I1 — Interpolated typed constants silently compile without type checking (P1 blocker)

| | |
|---|---|
| **Trigger** | `field q as quantity in 'each'` / `field s as string` / `-> set q = '1 {s}'` |
| **Expected** | Type error: `s` is `string`, cannot interpolate into a `quantity` typed constant |
| **Actual** | Fixed-safe-stub — the compiler now emits an Error diagnostic for the unsupported interpolated typed constant instead of compiling silently or crashing |
| **Status** | ✅ Fixed — safe stub only; full interpolation support remains future work |
| **Severity** | P1 — silent type-safety hole; contradicts Precept's "prevention, not detection" guarantee |
| **Scope** | Medium — parser + type checker |
| **Owner** | Frank (architecture-level, spans parser → type checker) |

**Root cause — two layers:**

1. **Parser (`Parser.Expressions.cs`, line 444, `ParseInterpolatedTypedConstant`):** Unlike `ParseInterpolatedString()` (line 399) which properly calls `ParseExpression()` for each `{expr}` hole and builds `InterpolatedStringExpression` with `HoleSegment` nodes, `ParseInterpolatedTypedConstant()` **skips all tokens** between `TypedConstantStart` and `TypedConstantEnd` via a bare `Advance()` loop. No expression nodes are created. It returns a flat `LiteralExpression` with kind `TypedConstantStart`.

2. **Type checker (`TypeChecker.Expressions.cs`, line 141):** `TypedConstantStart => new TypedErrorExpression(lit.Span)` with comment "Interpolated typed constants deferred". Because the parser never created expression nodes for the interpolation holes, there are no expressions to type-check. The `TypedErrorExpression` is then silently accepted because the assignment path (`ResolveAction`) has no post-resolution type validation (the B9–B12 gap).

**The spec IS clear** — `docs/language/precept-language-spec.md` §2.5 says `ParseInterpolatedTypedConstant()` should use the same reassembly loop as `ParseInterpolatedString()`, and §3.6 says each `{expr}` inside `'...'` should be type-checked independently with the full content validated against the context type.

**Fix (safe stub):**
1. `ResolveLiteral` now emits an Error-severity TC diagnostic before returning `TypedErrorExpression` for `TypedConstantStart`, reusing `DiagnosticCode.TypeMismatch` as the nearest existing self-contained code.
2. This closes the silent-compile/crash behavior by surfacing a clear unsupported-feature error and satisfying D26.
3. Full `ParseInterpolatedTypedConstant()` / typed-hole implementation remains future work; this fix intentionally does **not** implement interpolation semantics.

---

### I2 — No completions inside interpolation holes of typed constants (P3)

| | |
|---|---|
| **Trigger** | Type `{` inside a typed constant, e.g., `'1 {` — no completion list |
| **Expected** | Variable completions (fields, event args in scope) |
| **Actual** | Nothing |
| **Status** | 🔴 Open |
| **Severity** | P3 — UX gap, not a correctness issue |
| **Scope** | Small-medium — `CompletionHandler.cs` |
| **Owner** | Kramer |

**Root cause:** The `CompletionHandler` recognizes typed-constant context via `IsTypedConstantToken()` (which includes `TypedConstantStart`, `TypedConstantMiddle`, `TypedConstantEnd`) and routes to typed-constant slot items. But when the cursor is inside an interpolation hole (i.e., the lexer is in `Interpolation` mode, between `{` and `}`), the surrounding tokens are expression tokens — `Identifier`, operators, etc. — not typed-constant tokens. The handler doesn't detect that it's inside an interpolation hole of a typed constant and should offer expression completions (variable names). This is the same gap that would exist for string interpolation holes but is more impactful in typed constants because the expressions have type constraints.

**Proposed fix:** Detect when the cursor is between a `TypedConstantStart`/`TypedConstantMiddle` and the corresponding `TypedConstantEnd`/`TypedConstantMiddle`, in expression context (not literal-text context), and offer standard expression completions. Depends on I1 landing first (the parser needs to produce expression nodes for the completion handler to reason about). Alternatively, a token-walk approach could detect interpolation context without AST support.

---

### I3 — No semantic highlighting inside interpolation holes of typed constants (P3)

| | |
|---|---|
| **Trigger** | `'1 {s}'` — `s` inside braces has no syntax highlighting |
| **Expected** | `s` highlighted as a variable/field reference |
| **Actual** | No semantic token emitted for `s` |
| **Status** | 🔴 Open |
| **Severity** | P3 — cosmetic, but inconsistent with string interpolation behavior |
| **Scope** | Small — `SemanticTokensHandler.cs` |
| **Owner** | Kramer |

**Root cause:** The semantic tokens handler iterates typed expression trees (`EnumerateTypedExpressions`) looking for `TypedFieldRef`, `TypedArgRef`, etc. to emit variable/field semantic tokens. Since I1 means interpolated typed constants produce `TypedErrorExpression` (no sub-expressions), there are no expression nodes for the handler to walk. The handler does handle `TypedInterpolatedString` (line 644), but there's no `TypedInterpolatedTypedConstant` equivalent because the type checker never creates one.

**Proposed fix:** Downstream of I1 — once the parser and type checker produce proper expression nodes for interpolated typed constants, the semantic tokens handler needs a parallel case for `TypedInterpolatedTypedConstant` (or whatever the new node type is named), identical to the existing `TypedInterpolatedString` case at line 644.

**Dependency:** Blocked on I1.

---

**Interpolation bug priority order:**
1. **I1** (P1) — the type-safety hole. Must fix first. Parser + type checker.
2. **I2** (P3) — completions. Partially depends on I1 for full solution, but a token-walk fallback could work independently.
3. **I3** (P3) — semantic tokens. Fully blocked on I1.

**Relationship to B9–B12:** I1 is orthogonal — B9–B12 fix assignment-level validation (defense-in-depth for ALL expressions), while I1 fixes the fact that interpolated typed constant expressions aren't parsed or type-checked at all. Both are needed.

**Decision filed:** `.squad/decisions/inbox/frank-interpolation-triage.md`

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
| **Status** | ✅ Fixed — Kramer (kramer-11, commit ef7374dd) |

**Stack trace:** `ImmutableArray.Create[T](items, start, length)` → `SemanticTokensDocument.GetSemanticTokensEdits()` → `SemanticTokensDeltaPipeline.Handle`

**Root cause:** OmniSharp framework result IDs allowed stale baselines; fix: server mints own result IDs, detects stale baselines, returns full payload when delta is unsafe.

**Fix:** The language server now stamps its own client-visible semantic-token result IDs on every full or delta response, tracks the latest `(resultId, SemanticTokensDocument.Id)` per URI, and falls back to a full semantic-tokens response whenever the client requests delta from a stale baseline or after typed-constant span invalidation replaced the framework document. Added handler-level regression tests for stale result IDs and typed-constant span changes, and validation passed with `dotnet build tools/Precept.LanguageServer/Precept.LanguageServer.csproj --artifacts-path temp/dev-language-server` plus `dotnet test test/Precept.LanguageServer.Tests/`.

---

## B9-B12 Implementation Plan

**Author:** Frank  
**Date:** 2026-05-11  
**Status:** ✅ Complete  
**Scope:** Type checker qualifier-aware validation for quantity, money, and domain-typed assignments

### Problem Statement

The type checker performs no post-resolution type or qualifier validation on assignment targets. Four bugs share this root cause:

| Bug | Symptom | Core Gap |
|-----|---------|----------|
| B9 | Bare integer assignable to `quantity` field | ✅ Fixed — Slice 2 (`8a601e0d`): post-resolution `IsAssignable` check in `ResolveAction` |
| B10 | `kg` literal assigned to `quantity of 'length'` field | ✅ Fixed — Slices 3+4 (`94b38f96`, `cdec821f`): validator + assignment-level qualifier checks |
| B11 | `default '5 kg'` on `quantity of 'length'` field | ✅ Fixed — Slices 4+5 (`cdec821f`, this slice): validator + `ResolveFieldExpressions` safety net |
| B12 | `set q = qq` where `q: length`, `qq: mass` | ✅ Fixed — Slice 3 (`94b38f96`): qualifier metadata carried on expression refs |

Three structural deficits combine:
1. **No post-resolution type check** — `ResolveAction` (line 813) and `ResolveFieldExpressions` (line 452) both call `Resolve(value, ctx, fieldType)` but never verify `value.ResultType` against `fieldType`.
2. **Dimension-blind validator** — `QuantityValidator.Validate` (lines 11–32) validates UCUM syntax only. The `TypedConstantContext` parameter exists (line 15) but carries no qualifier metadata.
3. **Qualifier-stripped expression nodes** — `TypedArgRef` (lines 28–33 of `SemanticIndex.cs`) and `TypedFieldRef` (lines 20–25) carry only `ResultType: TypeKind`. The `DeclaredQualifiers` computed on `TypedArg` (line 412 of `TypeChecker.cs`) and `TypedField` (line 296) are inaccessible from the expression tree.

All diagnostic codes exist and are wired: `TypeMismatch` (PRE0018), `QualifierMismatch` (PRE0068), `DimensionCategoryMismatch` (PRE0069). None are emitted in these paths.

---

### File Inventory

| File | Changes |
|------|---------|
| `src/Precept/Pipeline/SemanticIndex.cs` | Add `DeclaredQualifiers?` to `TypedArgRef` and `TypedFieldRef` records |
| `src/Precept/Pipeline/TypeChecker.Expressions.cs` | `ResolveIdentifier`: propagate qualifiers into `TypedArgRef`/`TypedFieldRef`. `ResolveAction`: add post-resolution type + qualifier check. `ResolveMemberAccess`: propagate qualifiers into `TypedArgRef` at line 1334 |
| `src/Precept/Pipeline/TypeChecker.cs` | `ResolveFieldExpressions`: add post-resolution type + qualifier check for default/min/max values |
| `src/Precept/Language/TypedConstantParseResult.cs` | Extend `TypedConstantContext` with `DeclaredQualifiers` |
| `src/Precept/Language/QuantityValidator.cs` | Add dimension check after UCUM parse using context qualifiers |
| `test/Precept.Tests/TypeChecker/TypeCheckerExpressionTests.cs` | B9, B12 regression tests |
| `test/Precept.Tests/TypeChecker/TypeCheckerTypedConstantTests.cs` | B10 typed constant dimension validation tests |
| `test/Precept.Tests/TypeChecker/TypeCheckerSymbolTests.cs` | B11 field default qualifier tests; B12 qualifier-on-expression-node tests |
| `test/Precept.Tests/TypeChecker/MoneyQuantityModifierRegressionTests.cs` | Update gap-documenting tests to assert diagnostics once gaps are fixed |

---

### Slice 1: Carry `DeclaredQualifiers` on expression nodes (`TypedArgRef`, `TypedFieldRef`)

**Dependency:** None (structural foundation — all other slices depend on this)

**What to modify:**

1. **`src/Precept/Pipeline/SemanticIndex.cs`** — `TypedArgRef` record (lines 28–33) and `TypedFieldRef` record (lines 20–25):

   Add an optional `DeclaredQualifiers` parameter to both records:
   ```
   TypedFieldRef(TypeKind ResultType, string FieldName, bool IsCaseInsensitive,
       ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers, SourceSpan Span)

   TypedArgRef(TypeKind ResultType, string EventName, string ArgName,
       ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers, SourceSpan Span)
   ```
   Use `ImmutableArray<DeclaredQualifierMeta>?` (nullable) so existing callers can pass `null` for non-qualified types, and to avoid breaking the expression tree walker and pattern matching.

2. **`src/Precept/Pipeline/TypeChecker.Expressions.cs`** — `ResolveIdentifier` method (lines 533–579):

   - **Line 541** (quantifier binding path): Pass `DeclaredQualifiers: null` — quantifier bindings don't carry qualifiers.
   - **Line 549** (event arg path): Change from `new TypedArgRef(arg.ResolvedType, arg.EventName, arg.Name, id.Span)` to `new TypedArgRef(arg.ResolvedType, arg.EventName, arg.Name, arg.DeclaredQualifiers, id.Span)`.
   - **Lines 571–572** (field path): Change from `new TypedFieldRef(field.ResolvedType, field.Name, ctx.CIFields.Contains(field.Name), id.Span)` to `new TypedFieldRef(field.ResolvedType, field.Name, ctx.CIFields.Contains(field.Name), field.DeclaredQualifiers, id.Span)`.

3. **`src/Precept/Pipeline/TypeChecker.Expressions.cs`** — `ResolveMemberAccess` method (line 1334):

   Change from `new TypedArgRef(arg.ResolvedType, ev.Name, arg.Name, expr.Span)` to `new TypedArgRef(arg.ResolvedType, ev.Name, arg.Name, arg.DeclaredQualifiers, expr.Span)`.

4. **All other `TypedFieldRef`/`TypedArgRef` construction sites**: Audit and add `DeclaredQualifiers: null` where qualifier metadata is not available. The `CollectFieldRefs` walker and all `is TypedFieldRef`/`is TypedArgRef` pattern matches continue to work because new positional params are appended and C# record patterns ignore trailing members.

**Tests:**

- `TypeCheckerSymbolTests.cs`:
  - `ArgRef_CarriesQualifiers_WhenDeclared`: Build context with `event E (a as quantity of 'mass')`, resolve identifier `a` → assert `TypedArgRef.DeclaredQualifiers` contains `Dimension("mass")`.
  - `FieldRef_CarriesQualifiers_WhenDeclared`: Build context with `field w as quantity in 'kg'`, resolve identifier `w` → assert `TypedFieldRef.DeclaredQualifiers` contains `Unit("kg", "mass")`.
  - `ArgRef_NullQualifiers_WhenUnqualified`: Build context with `event E (n as integer)`, resolve `n` → assert `DeclaredQualifiers` is empty or null.

**Regression anchors:** All existing `TypeCheckerExpressionTests`, `TypeCheckerTypedConstantTests`, `TypeCheckerSymbolTests` must pass unchanged. The new parameter is additive — existing pattern matches on `TypedFieldRef`/`TypedArgRef` don't destructure the new field.

---

### Slice 2: Post-resolution type check in `ResolveAction` (B9)

**Dependency:** None strictly (Slice 1 is independent for the type-kind check). But implementing Slice 2 after Slice 1 avoids revisiting the same method.

**What to modify:**

1. **`src/Precept/Pipeline/TypeChecker.Expressions.cs`** — `ResolveAction` method, `AssignAction` case (lines 810–822):

   After line 813 (`var value = Resolve(assign.Value, ctx, fieldType != TypeKind.Error ? fieldType : null)`), before the `return new TypedInputAction(...)` at line 815, insert:

   ```csharp
   // B9: Post-resolution type check — verify resolved value is assignable to target field
   if (value is not TypedErrorExpression
       && fieldType != TypeKind.Error
       && !IsAssignable(value.ResultType, fieldType))
   {
       ctx.Diagnostics.Add(
           Diagnostics.Create(DiagnosticCode.TypeMismatch, assign.Value.Span,
               Types.GetMeta(fieldType).DisplayName, Types.GetMeta(value.ResultType).DisplayName));
   }
   ```

   The check uses existing `IsAssignable` (line 1162) which already handles `Error` propagation. Guard on `value is not TypedErrorExpression` to avoid duplicate diagnostics when resolution itself failed.

**Tests:**

- `TypeCheckerExpressionTests.cs`:
  - `SetAction_IntegerToQuantityField_EmitsTypeMismatch`: Precept with `field q as quantity of 'mass' default '0 kg'`, event with `set q = 5` → assert `DiagnosticCode.TypeMismatch`.
  - `SetAction_IntegerToMoneyField_EmitsTypeMismatch`: Same for `money in 'USD'` + `set m = 42`.
  - `SetAction_MatchingType_NoDiagnostic`: `field n as integer`, `set n = 5` → no diagnostic.
  - `SetAction_WidenedType_NoDiagnostic`: `field d as decimal`, `set d = 5` → no diagnostic (integer widens to decimal).

**Regression anchors:** All existing `ResolveAction` tests. The `MoneyQuantityModifierRegressionTests.Min_OnMoneyField_QualifierMismatch_NoDiagnostic_PreExistingGap` (line 138) may now emit a diagnostic — update expected behavior.

---

### Slice 3: Post-resolution qualifier check in `ResolveAction` (B10, B12)

**Dependency:** Slice 1 (needs `DeclaredQualifiers` on expression nodes for B12). Slice 2 (type check goes first; qualifier check supplements it).

**What to modify:**

1. **`src/Precept/Pipeline/TypeChecker.Expressions.cs`** — `ResolveAction` method, `AssignAction` case (lines 810–822):

   After the Slice 2 type check, add qualifier comparison. Extract a helper method `ValidateAssignmentQualifiers` to keep `ResolveAction` clean:

   ```csharp
   private static void ValidateAssignmentQualifiers(
       TypedExpression value,
       string fieldName,
       ImmutableArray<DeclaredQualifierMeta> targetQualifiers,
       SourceSpan valueSpan,
       CheckContext ctx)
   ```

   Logic:
   - If `targetQualifiers.IsEmpty` → no qualifier constraint, skip.
   - If `value` is `TypedTypedConstant` with `ResultType == Quantity` → the literal's qualifier comes from the parsed UCUM unit (Slice 4 will enrich this; for now, this path defers to the validator).
   - If `value` is `TypedFieldRef { DeclaredQualifiers: not null }` or `TypedArgRef { DeclaredQualifiers: not null }` → compare source qualifiers against target qualifiers on matching axes:
     - **Dimension axis:** target has `DeclaredQualifierMeta.Dimension(targetDim)`. Source has `DeclaredQualifierMeta.Dimension(sourceDim)` or `DeclaredQualifierMeta.Unit(_, sourceDimFromUnit)`. If dimension names differ → emit `DimensionCategoryMismatch`.
     - **Unit axis:** target has `DeclaredQualifierMeta.Unit(targetUnit, _)`. Source has same. If unit codes differ → emit `QualifierMismatch`.
     - **Currency axis:** target has `DeclaredQualifierMeta.Currency(targetCurrency)`. Source has same. If currency codes differ → emit `QualifierMismatch`.
   - If source expression has no qualifiers and target does → this is a type-level gap (likely caught by Slice 2's type check already). Don't double-emit.

2. **Call site**: In the `AssignAction` case, after the Slice 2 type check:

   ```csharp
   if (value is not TypedErrorExpression && ctx.FieldLookup.TryGetValue(fieldName, out var targetField))
       ValidateAssignmentQualifiers(value, fieldName, targetField.DeclaredQualifiers, assign.Value.Span, ctx);
   ```

**Tests:**

- `TypeCheckerExpressionTests.cs`:
  - `SetAction_MassDimensionToLengthField_EmitsDimensionCategoryMismatch`: `field q as quantity of 'length'`, `event E (qq as quantity of 'mass')`, `set q = qq` → `DiagnosticCode.DimensionCategoryMismatch`.
  - `SetAction_MatchingDimension_NoDiagnostic`: `field q as quantity of 'mass'`, `event E (qq as quantity of 'mass')`, `set q = qq` → no diagnostic.
  - `SetAction_USDToEURField_EmitsQualifierMismatch`: `field m as money in 'USD'`, `event E (p as money in 'EUR')`, `set m = p` → `DiagnosticCode.QualifierMismatch`.
  - `SetAction_FieldToFieldDimensionMismatch_EmitsDimensionCategoryMismatch`: field-to-field cross-dimension assignment.

**Regression anchors:** All existing action resolution tests. `MoneyQuantityModifierRegressionTests` gap-documenting tests may need updating.

---

### Slice 4: Dimension-aware `QuantityValidator` (B10, B11)

**Dependency:** Slice 1 for data shape. Independent of Slices 2–3 for the validator itself, but Slices 2–3 provide the assignment-level safety net.

**What to modify:**

1. **`src/Precept/Language/TypedConstantParseResult.cs`** — `TypedConstantContext` record (line 19):

   Extend with qualifier metadata:
   ```csharp
   public sealed record TypedConstantContext(
       TypeKind? PeerType = null,
       OperatorKind? Operator = null,
       ImmutableArray<DeclaredQualifierMeta>? DeclaredQualifiers = null);
   ```
   The new property is nullable and defaults to null, so all existing call sites (binary op context retry, temporal validator) are unaffected.

2. **`src/Precept/Language/QuantityValidator.cs`** — `Validate` method (lines 11–32):

   After UCUM parse succeeds (line 24, `unitResult.IsValid`), before returning the success result (line 30), add dimension check:

   ```csharp
   // Dimension compatibility check against declared qualifiers
   if (context?.DeclaredQualifiers is { } qualifiers)
   {
       var literalDimension = DeriveUnitDimensionName(unitResult.Unit!);
       foreach (var qual in qualifiers)
       {
           if (qual is DeclaredQualifierMeta.Dimension { DimensionName: var declaredDim }
               && !string.IsNullOrEmpty(declaredDim)
               && !string.Equals(literalDimension, declaredDim, StringComparison.OrdinalIgnoreCase))
           {
               return TypedConstantParseResult.Failed(
                   validation.FormatDescription,
                   new TypedConstantDiagnostic("TC_DIM", $"Unit '{unitResult.Unit!.CanonicalCode}' is '{literalDimension}' but field requires '{declaredDim}'"));
           }
           if (qual is DeclaredQualifierMeta.Unit { DimensionName: var declaredUnitDim }
               && !string.IsNullOrEmpty(declaredUnitDim)
               && !string.Equals(literalDimension, declaredUnitDim, StringComparison.OrdinalIgnoreCase))
           {
               return TypedConstantParseResult.Failed(
                   validation.FormatDescription,
                   new TypedConstantDiagnostic("TC_DIM", $"Unit '{unitResult.Unit!.CanonicalCode}' is '{literalDimension}' but field's unit '{qual}' is '{declaredUnitDim}'"));
           }
       }
   }
   ```

   **Note:** `DeriveUnitDimensionName` currently lives in `TypeChecker.cs` (line 188) as a `private static` method. It must be extracted to a shared location (e.g., a `public static` method on a helper, or inlined into `QuantityValidator` with access to `UcumParsedUnit`). The simplest approach: make `DeriveUnitDimensionName` `internal static` on `TypeChecker` and call it from `QuantityValidator`, or duplicate the minimal dimension-derivation logic. Kramer should prefer extracting to a shared utility (`QuantityDimensionHelper` or adding it to `UcumParsedUnit` as a property).

3. **`src/Precept/Pipeline/TypeChecker.Expressions.cs`** — `ResolveTypedConstant` method (lines 192–225):

   Thread qualifiers into the context. Currently line 212 calls `TypedConstantValidation.Validate(cv, rawText, targetType)` with no context. Change to pass context with qualifiers when available:

   The challenge: `ResolveTypedConstant` doesn't currently know which field it's resolving for. The `expectedType` is a `TypeKind`, not a field reference. Qualifiers must be threaded through the `Resolve` call chain.

   **Approach:** Add an optional `ImmutableArray<DeclaredQualifierMeta>?` parameter to the `Resolve` method signature:
   ```csharp
   private static TypedExpression Resolve(
       ParsedExpression expr, CheckContext ctx,
       TypeKind? expectedType = null,
       ImmutableArray<DeclaredQualifierMeta>? qualifiers = null)
   ```
   Pass it through `ResolveLiteral` → `ResolveTypedConstant` → `TypedConstantValidation.Validate` via `TypedConstantContext`. Call sites that know the field (`ResolveAction` with `fieldName` → `ctx.FieldLookup[fieldName].DeclaredQualifiers`, `ResolveFieldExpressions` with `typedField.DeclaredQualifiers`) pass it; other call sites pass `null`.

   In `ResolveTypedConstant`, change line 212:
   ```csharp
   var tcContext = qualifiers is not null ? new TypedConstantContext(DeclaredQualifiers: qualifiers) : null;
   var result = TypedConstantValidation.Validate(cv, rawText, targetType, tcContext);
   ```

4. **`src/Precept/Pipeline/TypeChecker.Expressions.cs`** — `ResolveAction`, `AssignAction` case (line 813):

   Thread field qualifiers into `Resolve`:
   ```csharp
   var fieldQualifiers = ctx.FieldLookup.TryGetValue(fieldName, out var targetFieldMeta)
       ? targetFieldMeta.DeclaredQualifiers
       : (ImmutableArray<DeclaredQualifierMeta>?)null;
   var value = Resolve(assign.Value, ctx,
       fieldType != TypeKind.Error ? fieldType : null,
       fieldQualifiers);
   ```

5. **`src/Precept/Pipeline/TypeChecker.cs`** — `ResolveFieldExpressions`, default value resolution (line 452):

   Thread field qualifiers into `Resolve`:
   ```csharp
   var resolved = Resolve(defaultMod.Value, ctx, typedField.ResolvedType, typedField.DeclaredQualifiers);
   ```
   Same for min (line 464) and max (line 472) modifier values.

**Tests:**

- `TypeCheckerTypedConstantTests.cs`:
  - `QuantityLiteral_WrongDimension_EmitsInvalidContent`: Context with `quantity of 'length'` field, resolve `'5 kg'` → `DiagnosticCode.InvalidTypedConstantContent` (because the validator itself returns `Failed`).
  - `QuantityLiteral_MatchingDimension_Succeeds`: Context with `quantity of 'mass'`, resolve `'5 kg'` → `TypedTypedConstant` with no diagnostic.
  - `QuantityLiteral_NoDeclaredDimension_Succeeds`: Context with bare `quantity`, resolve `'5 kg'` → succeeds (no dimension constraint).

- `TypeCheckerSymbolTests.cs` or new test class:
  - `FieldDefault_WrongDimension_EmitsDiagnostic`: Full-pipeline test: `field q as quantity of 'length' default '5 kg'` → assert diagnostic.
  - `FieldDefault_MatchingDimension_NoDiagnostic`: `field q as quantity of 'mass' default '5 kg'` → no diagnostic.

**Regression anchors:** All existing `TypeCheckerTypedConstantTests` (quantity valid/invalid scenarios). All existing `QuantityValidator` unit tests. All `MoneyQuantityModifierRegressionTests`.

---

### Slice 5: Post-resolution qualifier check in `ResolveFieldExpressions` (B11)

**Dependency:** Slice 1 (qualifier data), Slice 4 (validator-level catches most B11 cases, but this provides the assignment-level safety net).

**What to modify:**

1. **`src/Precept/Pipeline/TypeChecker.cs`** — `ResolveFieldExpressions` method (lines 438–500):

   After default value resolution (line 452–454), add a post-resolution type + qualifier check mirroring Slice 2 and Slice 3:

   ```csharp
   // B11: Post-resolution type + qualifier check on default value
   if (resolved is not TypedErrorExpression && typedField.ResolvedType != TypeKind.Error)
   {
       if (!IsAssignable(resolved.ResultType, typedField.ResolvedType))
       {
           ctx.Diagnostics.Add(
               Diagnostics.Create(DiagnosticCode.TypeMismatch, defaultMod.Value.Span,
                   Types.GetMeta(typedField.ResolvedType).DisplayName,
                   Types.GetMeta(resolved.ResultType).DisplayName));
       }
       else
       {
           ValidateAssignmentQualifiers(resolved, typedField.Name,
               typedField.DeclaredQualifiers, defaultMod.Value.Span, ctx);
       }
   }
   ```

   Apply same pattern to min/max modifier values (lines 460–475).

   **Note:** `ValidateAssignmentQualifiers` is defined in `TypeChecker.Expressions.cs` (Slice 3). Both files are `partial class TypeChecker`, so the helper is accessible.

**Tests:**

- `TypeCheckerSymbolTests.cs`:
  - `FieldDefault_IntegerForQuantity_EmitsTypeMismatch`: `field q as quantity default 5` → `DiagnosticCode.TypeMismatch`.
  - `FieldDefault_ArgRefCrossDimension_EmitsDimensionMismatch`: (Only testable via computed field or event-arg default — verify test feasibility.)
  - `MinMax_WrongDimension_EmitsDiagnostic`: `field q as quantity of 'mass' min '0 m'` → diagnostic.

**Regression anchors:** All existing `ResolveFieldExpressions` tests. `MoneyQuantityModifierRegressionTests` gap tests get updated assertions.

---

### Execution Order Summary

```
Slice 1 ──→ Slice 2 ──→ Slice 3 ──→ Slice 5
   │                        ↑
   └──→ Slice 4 ────────────┘
```

1. **Slice 1** (structural): Extend `TypedArgRef`/`TypedFieldRef` with `DeclaredQualifiers`. Independently testable.
2. **Slice 2** (B9): Post-resolution `IsAssignable` check. Independently testable after Slice 1.
3. **Slice 3** (B10+B12 assignment-level): Qualifier comparison on assignments. Depends on Slice 1 for variable-to-field; Slice 2 for type-level guard.
4. **Slice 4** (B10+B11 validator-level): Dimension-aware `QuantityValidator`. Depends on Slice 1 for data shape.
5. **Slice 5** (B11 safety net): Post-resolution check in `ResolveFieldExpressions`. Depends on Slices 1, 3 (reuses helper), 4 (validator provides first-pass catches).

Each slice should be committed separately with all tests green before proceeding.

---

### MCP Sync Assessment

- **`precept_compile` output**: Yes — new diagnostics (PRE0018, PRE0068, PRE0069) will appear in definitions that previously compiled clean. This is correct behavior (bugs becoming visible). The MCP `precept_compile` tool wraps `TypeChecker.Check`, so it gets the new diagnostics automatically. No DTO changes needed.
- **MCP DTOs**: No changes required. `TypedField` and `TypedArg` already carry `DeclaredQualifiers` in the `SemanticIndex`. The expression tree additions (`TypedArgRef.DeclaredQualifiers`, `TypedFieldRef.DeclaredQualifiers`) are internal and not serialized by MCP tools.
- **Language server**: No changes. Diagnostics flow through the existing pipeline. Semantic tokens are unaffected (no new token kinds).

---

### Known Risks

1. **`DeriveUnitDimensionName` extraction**: Currently `private static` in `TypeChecker.cs`. Must be made accessible to `QuantityValidator`. Cleanest: make it `internal static` on `TypeChecker` or extract to a shared utility. Risk: minimal, but the method depends on `CountQualifierUnitCodes` and `NonCountDimensionlessUnitCodes` (also `TypeChecker` privates). May need to move these to a shared home or pass dimension derivation as a delegate.

2. **Positional parameter ordering**: `TypedArgRef` and `TypedFieldRef` are positional records. Adding `DeclaredQualifiers` before `Span` changes positional construction order. All existing construction sites must be updated. Pattern matches using named parameters or just `ResultType` are safe; positional destructuring will break. **Mitigation:** Audit all construction sites before committing Slice 1. The grep above shows ~15 sites.

3. **Double diagnostics**: Both the validator-level check (Slice 4) and the assignment-level check (Slices 3/5) can fire on the same literal. For typed constant literals, the validator will return `Failed`, which causes `ResolveTypedConstant` to emit `InvalidTypedConstantContent` and return `TypedErrorExpression`. The assignment-level check guards on `value is not TypedErrorExpression`, so it won't double-fire. For variable references (B12), only the assignment-level check fires. This should be verified in integration tests.

4. **Money fields**: The same gaps apply to `money in 'USD'` — the qualifier check is type-agnostic. Slices 2–3–5 handle money automatically because `ValidateAssignmentQualifiers` works on `DeclaredQualifierMeta` generically. Slice 4 is quantity-specific, but `MoneyValidator` has its own currency validation. Verify money gap tests update correctly.

5. **Computed field expressions**: `ResolveFieldExpressions` also resolves computed expressions (lines 477–499). The post-resolution qualifier check should apply there too, but computed fields reference other fields via `TypedFieldRef` (which now carries qualifiers after Slice 1). The Slice 5 check should cover the computed path with the same `ValidateAssignmentQualifiers` call.

6. **Existing gap tests**: `MoneyQuantityModifierRegressionTests.Min_OnMoneyField_QualifierMismatch_NoDiagnostic_PreExistingGap` (line 138) explicitly documents the gap with "no diagnostic." After these fixes, diagnostics will be emitted. These tests must be updated to assert the new diagnostic instead of asserting no diagnostic.
