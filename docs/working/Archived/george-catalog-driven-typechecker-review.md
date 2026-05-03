# George's Catalog-Driven Type Checker Review

**Author:** George (Runtime Dev)
**Date:** 2026-05-02T18:18:39-04:00
**Requested by:** Shane

---

## Framing

My job here is adversarial: find the places where implementing the current design as written would cause me — the person writing the code — to reach for per-member `switch` statements, hardcoded lookup tables, or knowledge the checker "just knows" about specific catalog members. I've read the design, read the catalog sources, and read the parser. Here's what I found.

---

## 1. Implementation Smell Forecast

### 1a. ActionSyntaxShape → TypedAction DU dispatch (Slice 5) — GENUINE SMELL

**Design section:** § Typed Actions (3-Shape DU), Gap 3

The design says Gap 3 is "acceptable as checker logic" because the 3-arm switch is "stable." But when I look at `ActionSyntaxShape` with its 9 members against the 3 DU shapes, the mapping is NOT a clean 3-arm partition:

| ActionSyntaxShape | TypedAction shape | SecondaryRole |
|---|---|---|
| `FieldOnly` | Base (no operand) | — |
| `CollectionInto`, `CollectionIntoBy` | TypedBindingAction | — |
| `AssignValue`, `CollectionValue` | TypedInputAction | null |
| `InsertAt` | TypedInputAction | `Index` |
| `CollectionValueBy`, `PutKeyValue` | TypedInputAction | `Key` |
| `RemoveAtIndex` | ??? | Index? |

`RemoveAtIndex` is the trap. It's "verb field at expr (remove at index: positional, no element)." There is no value expression — only an index. If I map it to TypedInputAction with `InputExpression = indexExpr`, the evaluator can't distinguish "index expression in a remove-at-index" from "index expression as secondary in an insert-at." The `ActionSecondaryRole` enum was designed for the secondary position; `RemoveAtIndex` needs the index in what would normally be the primary slot.

Without catalog metadata, my Slice 5 implementation contains this:

```csharp
ActionSyntaxShape.FieldOnly         => new TypedAction(...),
ActionSyntaxShape.CollectionInto or
  CollectionIntoBy                  => new TypedBindingAction(..., into),
ActionSyntaxShape.InsertAt          => new TypedInputAction(..., SecondaryRole: Index),
ActionSyntaxShape.CollectionValueBy or
  PutKeyValue                       => new TypedInputAction(..., SecondaryRole: Key),
ActionSyntaxShape.RemoveAtIndex     => // ??? forced special case
_ /* everything else */             => new TypedInputAction(...),
```

I'm hardcoding which `ActionSyntaxShape` values use which DU shape and which `SecondaryRole` — per-member dispatch on enum identity. That's the smell. The design's claim that "new values naturally fall into existing categories" is uncheckable because the catalog carries no `TypedShape` field. Nothing prevents the next action developer from introducing a value that the checker silently miscategorizes.

**Proposed fix (see § 6 Concrete Proposals).**

### 1b. ContentValidation dispatch (Slice 4) — SMELL, LOCKED DESIGN PENDING

**Design section:** § Catalog Gaps, Gap 1

Without the `ContentValidation DU` on `TypeMeta`, Slice 4 resolves typed constants via:

```csharp
TypeKind.Date     => validateAsDatePattern(value),
TypeKind.Money    => validateAsMoneyLiteral(value),
TypeKind.Currency => validateAsCurrencyCode(value),
...
```

This is a per-`TypeKind` switch encoding domain knowledge (date literals look like `YYYY-MM-DD`, currency is ISO 4217) that belongs in catalog metadata. The design already knows this (Gap 1 is HIGH), and the locked shape is good (`RegexValidation | NodaTimeValidation | ClosedSetValidation`). The only implementation-level note is: **if this isn't landed before Slice 4 starts, the hardcoded dispatch table becomes debt that grows with every new typed constant format.** The `ContentValidation?` field doesn't yet exist on `TypeMeta` in code.

### 1c. Literal range validation (Slice 4) — SMELL, NOT YET IDENTIFIED AS A GAP

**Design section:** § Slice 4

The design says Slice 4 validates out-of-range numeric literals against "the type's representable range (`integer` → Int64 bounds, `decimal` → Decimal.MaxValue/MinValue, `number` → Double range with precision loss warning for integers > 2^53) — ~10 lines sourced from `TypeMeta` range metadata."

I searched `TypeMeta` in the source. There is no `RepresentableRange`, `LiteralRange`, or any bounds field on `TypeMeta`. The catalog has no range metadata. That means the "~10 lines sourced from `TypeMeta`" will actually be a hardcoded per-TypeKind switch in Slice 4:

```csharp
TypeKind.Integer => (long.MinValue, long.MaxValue, null),
TypeKind.Decimal => (decimal.MinValue, decimal.MaxValue, null),
TypeKind.Number  => (double.MinValue, double.MaxValue, "Integers > 2^53 lose precision"),
```

This is a hidden catalog gap. The design language implies the metadata exists; it doesn't.

### 1d. Modifier bounds-pair validation (Slice 7) — SMELL, NOT IDENTIFIED

**Design section:** § Slice 7

Slice 7 validates "bounds validation (min > max, negative counts)." To validate that `min` is less than `max`, the checker needs to find the `max` modifier on the same field given a `min` modifier, and vice versa. There's no `CounterpartBound` or equivalent field on `FieldModifierMeta`. The checker will hardcode:

```csharp
ModifierKind.Min      → counterpart = ModifierKind.Max,
ModifierKind.Minlength → counterpart = ModifierKind.Maxlength,
ModifierKind.Mincount  → counterpart = ModifierKind.Maxcount,
```

Three hardcoded pairs that the catalog currently doesn't express. If a `Minduration` / `Maxduration` modifier ever lands, this table would need an explicit update. Without catalog metadata, the compiler won't tell me.

### 1e. What is NOT a smell (validating correct patterns)

**Accessor return-type resolution switch (§ Accessor Return-Type Resolution):**
```csharp
resolvedAccessor switch
{
    FixedReturnAccessor f      => (f.Returns, f.ParameterType),
    ElementParameterAccessor e => (TypeKind.Integer, owningField.ElementType!.Value),
    TypeAccessor a             => (owningField.ElementType!.Value, null),
}
```
This switches on DU **subtype** — not enum identity. The subtype IS the metadata shape. This is the correct catalog-driven pattern.

**Qualifier disambiguation (~15 lines):** This is structural logic (qualifier identity requires knowing actual field qualifiers at the expression site), not per-catalog-member dispatch. Correct.

**FieldScopeMode / CurrentFieldIndex / QuantifierBindings:** Pure structural machinery. No catalog knowledge embedded. Correct.

---

## 2. Missing Catalog Metadata

### Gap A: `TypedActionShape` on `ActionMeta` — MEDIUM

**What the type checker needs to know:** For each `ActionKind`, what TypedAction DU shape should the checker produce, and what `ActionSecondaryRole` (if any) applies?

**Which catalog should carry it:** `Actions` catalog — `ActionMeta`.

**Proposed metadata shape:**

```csharp
public enum TypedActionShape
{
    NoOperand,          // FieldOnly → TypedAction base
    ExpressionOperand,  // most inputs → TypedInputAction
    IndexedInsert,      // InsertAt → TypedInputAction with SecondaryRole.Index
    KeyedInsert,        // CollectionValueBy, PutKeyValue → TypedInputAction with SecondaryRole.Key
    IndexedRemove,      // RemoveAtIndex → TypedInputAction where the expr IS the index
    BindingOperand,     // CollectionInto, CollectionIntoBy → TypedBindingAction
}

// Added to ActionMeta:
public TypedActionShape TypedShape { get; init; }
```

This gives the checker a table-driven dispatch:
```csharp
Actions.GetMeta(actionKind).TypedShape switch
{
    TypedActionShape.NoOperand      => new TypedAction(...),
    TypedActionShape.BindingOperand => new TypedBindingAction(..., into),
    TypedActionShape.IndexedInsert  => new TypedInputAction(..., SecondaryRole: Index),
    TypedActionShape.KeyedInsert    => new TypedInputAction(..., SecondaryRole: Key),
    TypedActionShape.IndexedRemove  => new TypedInputAction(indexExpr, null, null),
    TypedActionShape.ExpressionOperand => new TypedInputAction(valueExpr, null, null),
}
```

The evaluator is now reading typed action DU shape without back-referencing `ActionKind`. New actions automatically require a `TypedShape` value — the exhaustive switch enforces coverage.

### Gap B: `LiteralRange?` on `TypeMeta` — MEDIUM

**What the type checker needs to know:** What are the representable bounds for a numeric literal of this type?

**Which catalog should carry it:** `Types` catalog — `TypeMeta`.

**Proposed metadata shape:**

```csharp
public sealed record LiteralRange(
    decimal Min,
    decimal Max,
    string? PrecisionWarning = null   // e.g., "integers > 2^53 lose precision"
);

// Added to TypeMeta:
public LiteralRange? LiteralRange { get; init; }
```

`Integer` → `LiteralRange(long.MinValue, long.MaxValue)`, `Decimal` → `LiteralRange(decimal.MinValue, decimal.MaxValue)`, `Number` → `LiteralRange(double.MinValue (decimal cast), double.MaxValue, "precision loss warning")`. Scalar numeric types carry it; non-numeric types get `null`. Checker in Slice 4:

```csharp
if (Types.GetMeta(resolvedType).LiteralRange is { } range)
    ValidateLiteralInRange(value, range);
```

Zero per-TypeKind dispatch.

### Gap C: `CounterpartBound` on `FieldModifierMeta` — LOW

**What the type checker needs to know:** Which modifier forms a bounds pair with this one?

**Which catalog should carry it:** `Modifiers` catalog — `FieldModifierMeta`.

**Proposed metadata shape:**

```csharp
// Added to FieldModifierMeta:
public ModifierKind? CounterpartBound { get; init; }
// Min.CounterpartBound = Max, Minlength.CounterpartBound = Maxlength, etc.
```

Checker: `if (meta.CounterpartBound is { } counterpart && field has both) ValidateBoundOrder(minVal, maxVal)`. Adding a new bounds pair automatically propagates to the validation — no checker update.

### Confirming Gap 1 (ContentValidation DU) is real and HIGH

Already designed with locked shape (`RegexValidation | NodaTimeValidation | ClosedSetValidation`). The concern is landing it. Without it, Slice 4 is guaranteed to produce a hardcoded per-TypeKind dispatch table. It should be treated as a **blocking dependency for Slice 4**, not just "high priority."

---

## 3. Right-Sizing Opportunities

### 3a. Widening: already right-sized

The single-hop `WidensTo` design is correct at Precept's scale. `IntegerWidens = [Decimal, Number]` captures all reachable targets without transitive resolution. The left-first → right-first → both nested loop is at most 9 `FindCandidates` calls for a type with 2 widening targets. No type lattice infrastructure needed.

### 3b. Function overload resolution: already right-sized

The scoring algorithm (exact=0, widened=count_of_widened_args, lowest-wins) is correct. With ~25 functions having typically 3-5 overloads each, there's no ambiguity scenario where this fails. No specificity trees, no tiebreaker chains. 

### 3c. ConditionalExpression unification: already right-sized

"Branch types must unify" is ~5 lines using `IsAssignable` bidirectionally. No need for a full LUB (least-upper-bound) algorithm. The only type hierarchy is Integer → Decimal → Number; any two branches either match directly, one widens to the other, or it's an error. That's the complete algorithm.

### 3d. SemanticIndex: flat inventory is correct

The justified divergence from Roslyn-style lazy resolution is right. At ~500 declarations, the flat inventory is less complex and faster. No query system overhead.

### 3e. Where the design IS correctly sized (nothing to simplify)

The 2-pass architecture (registration → checking) is not over-engineering — Pass 1 is required because Precept allows forward field references in guards and action contexts (just not in default value expressions). Without Pass 1, you'd have to two-pass anyway or special-case every forward reference.

---

## 4. Resolve() Shape Analysis

### Is 250-350 lines the right shape?

Yes and no. The line count is defensible — 16 arms × 15-20 lines each = 240-320 lines, which checks out. But the more important question is whether the function's internal structure signals "catalog-driven" or "knows things."

**What's correct:** Every arm delegates to catalogs for per-type/per-operation behavior. No arm says "if type is money, apply this special rule." The arms dispatch on *AST structure*, not on *TypeKind identity*.

**What I'd structure differently:** The 16 arms could be grouped by `ExpressionFormMeta.Category` to make the catalog relationship explicit:

```csharp
TypedExpression Resolve(Expression expr, TypeKind? expectedType) => expr switch
{
    // ── Atoms (nud) — ExpressionCategory.Atom ─────────────────────────────────────
    LiteralExpression lit                        => ResolveAtom(lit, expectedType),
    IdentifierExpression id                      => ResolveIdentifier(id),
    ParenthesizedExpression paren                => Resolve(paren.Inner, expectedType),

    // ── Composites (led + unary) — ExpressionCategory.Composite ───────────────────
    BinaryExpression bin                         => ResolveBinary(bin, expectedType),
    UnaryExpression unary                        => ResolveUnary(unary),
    MemberAccessExpression access                => ResolveMemberAccess(access),
    ConditionalExpression cond                   => ResolveConditional(cond, expectedType),
    IsSetExpression or IsNotSetExpression postfix => ResolvePostfix(postfix),

    // ── Invocations — ExpressionCategory.Invocation ───────────────────────────────
    CallExpression call                          => ResolveFunctionCall(call, expectedType),
    MethodCallExpression method                  => ResolveMethodCall(method),
    CIFunctionCallExpression ci                  => ResolveCIFunctionCall(ci),

    // ── Collections — ExpressionCategory.Collection ───────────────────────────────
    ListLiteralExpression list                   => ResolveListLiteral(list),

    // ── Quantifiers ────────────────────────────────────────────────────────────────
    QuantifierExpression quantifier              => ResolveQuantifier(quantifier),

    _ => Stub(expr),
};
```

This top-level Resolve() is ~30 lines — a catalog-annotated router. The real logic is in per-category helpers (ResolveBinary calls `Operations.FindCandidates`; ResolveFunctionCall calls `Functions.ByName`; ResolveMemberAccess calls `TypeMeta.Accessors`). The structure makes the catalog integration points explicit rather than buried at column 200 inside a monolith.

The `ResolveAtom` helper handles the Literal sub-forms (StringLiteral, NumberLiteral, TypedConstant, InterpolatedString, True/False) as a nested pattern match — this is the one place where sub-dispatch makes sense because all sub-forms have the same `[HandlesCatalogMember(ExpressionFormKind.Literal)]` ownership.

**Net result:** Resolve() stays at ~250-350 total lines across the top-level + helpers. The difference is the top-level function becomes a catalog-annotated 30-line router, and the per-arm complexity is isolated in helpers that are independently testable.

---

## 5. Parser Pattern Replication

### Patterns the checker SHOULD replicate

**Pattern 1: Static class-load derived sets**

The parser builds `ModifierKeywords`, `TypeKeywords`, `ActionKeywords`, `CICapableFunctionNames` etc. as `static readonly FrozenSet<TokenKind>` at class load time. These are one-time derivations from catalog data that make the relationship explicit: "this set comes from the Modifiers catalog, OfType<FieldModifierMeta>."

The checker should do the same for the sets it queries repeatedly:

```csharp
// Derived at class load from Types catalog — never hardcoded
internal static readonly FrozenSet<TypeKind> CollectionTypeKinds =
    Types.All.Where(t => t.Category == TypeCategory.Collection)
             .Select(t => t.Kind)
             .ToFrozenSet();

internal static readonly FrozenSet<TypeKind> OrderableTypeKinds =
    Types.All.Where(t => t.Traits.HasFlag(TypeTrait.Orderable))
             .Select(t => t.Kind)
             .ToFrozenSet();

internal static readonly FrozenSet<FunctionKind> CIVariantFunctionKinds =
    Functions.All.Where(f => f.CIVariantOf != null)
                 .Select(f => f.Kind)
                 .ToFrozenSet();
```

These are used in: quantifier resolution (collection? check), `IsSet`/`IsNotSet` (optional check is structural, but collection membership uses `CollectionTypeKinds`), CI enforcement (Slice 8), choice `ordered` modifier applicability. Without these derived sets, the checker writes inline `Types.GetMeta(type).Category == TypeCategory.Collection` checks at each call site — which works but doesn't make the catalog derivation relationship visible.

**Pattern 2: O(1) modifier lookup via `Modifiers.ByFieldToken`**

The parser uses `Modifiers.ByFieldToken` for O(1) field-modifier resolution. This already exists. The checker in Slice 7 should use it for modifier applicability lookup rather than calling `Modifiers.All.OfType<FieldModifierMeta>().First(m => m.Kind == kind)`.

**Pattern 3: `HandlesCatalogExhaustively` / `HandlesCatalogMember` ownership chain**

The parser's completeness guarantee comes from `ConstructKind` coverage tests. The checker has `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` with PRECEPT0019. This is a direct replication of the parser pattern and it's already in the stub. The critical discipline is the per-slice migration protocol: don't leave `[HandlesCatalogMember]` on the dead stub method longer than its slice requires. The parser taught us that dead coverage annotations on stub methods are silent lies.

**Pattern 4: `Constructs.ByLeadingToken` → checker analog: `Actions.ByTokenKind`**

The parser never hardcodes token→construct routing; it queries `Constructs.ByLeadingToken`. The checker's Slice 5 action dispatch should route `ActionKind` to TypedAction shape through `Actions.GetMeta(kind).TypedShape` (once Gap A is filled), not through a hardcoded switch on `ActionKind` or `ActionSyntaxShape` values.

### Patterns that DON'T transfer (and why)

**Parser's AmbiguousQualifierPrepositions complex index:** The checker doesn't parse tokens, so disambiguating qualifier prepositions from construct leaders is irrelevant. No equivalent needed.

**Parser's ExpressionBoundaryTokens / StructuralBoundaryTokens:** These terminate Pratt loops. The checker doesn't have a Pratt loop. No equivalent needed.

---

## 6. Concrete Proposals

### Proposal 1: Add `TypedShape` to `ActionMeta` (Gap A) — BEFORE SLICE 5

**Status:** Not in current design. New proposal.

Add `TypedActionShape TypedShape` to `ActionMeta` in `Action.cs`. Fill it in the Actions catalog for all 9 `ActionSyntaxShape` values. The checker's Slice 5 action dispatch becomes a single `Actions.GetMeta(kind).TypedShape switch` with no per-ActionKind knowledge in the checker.

`RemoveAtIndex` resolves the ambiguity: it gets `TypedActionShape.IndexedRemove` — TypedInputAction where `InputExpression` is the index expression and `SecondaryExpression` is null. The evaluator reads the `TypedShape` discriminator (not `ActionKind`) to know "this is a positional remove."

**Risk if skipped:** Slice 5 hardcodes a 6-arm switch on `ActionSyntaxShape` enum identity. `RemoveAtIndex` requires a special case the design doesn't currently acknowledge. The evaluator back-references `ActionKind` to interpret the expression meaning.

### Proposal 2: Add `LiteralRange?` to `TypeMeta` (Gap B) — BEFORE SLICE 4

**Status:** Not in current design. New proposal.

Add `LiteralRange?` to `TypeMeta` with `(decimal Min, decimal Max, string? PrecisionWarning)`. Integer, Decimal, and Number get it; everything else is null. Slice 4's range check becomes a 2-line catalog lookup. The alternative is a per-TypeKind switch that's invisible to the catalog system.

**Risk if skipped:** Slice 4 contains a hardcoded 3-entry switch that looks like:
```csharp
TypeKind.Integer => /* Int64 bounds */
TypeKind.Decimal => /* Decimal bounds */
TypeKind.Number  => /* double bounds + precision warning */
```
Three constants the catalog system can't verify or propagate. This is the same kind of parallel knowledge the architecture explicitly bans.

### Proposal 3: Land `ContentValidation DU` as a Slice 4 blocker (Gap 1)

**Status:** Design locked (HIGH in doc). Needs to be treated as blocking, not high-priority.

Mark `ContentValidation DU` as a **hard prerequisite for Slice 4**. The locked shape (`RegexValidation | NodaTimeValidation | ClosedSetValidation`) is good. The `ContentValidation?` field needs to be added to `TypeMeta` and populated in `Types.cs` before Slice 4 starts. Otherwise Slice 4 contains a long-lived hardcoded dispatch table that silently diverges from the catalog.

### Proposal 4: Add `CounterpartBound?` to `FieldModifierMeta` (Gap C)

**Status:** Not in current design. New proposal.

Add `ModifierKind? CounterpartBound` to `FieldModifierMeta`. Populate it: `Min → Max`, `Max → Min`, `Minlength → Maxlength`, `Maxlength → Minlength`, `Mincount → Maxcount`, `Maxcount → Mincount`. Checker's Slice 7 bounds validation: find both counterparts, compare values, no hardcoded pairs.

**Priority:** LOW — the current 3 pairs are stable. But this is the right shape for the catalog.

### Proposal 5: Structure Resolve() as a 30-line catalog-annotated router

**Status:** Implementation guidance, not a catalog change.

Implement the top-level `Resolve()` as a ~30-line match router grouping by `ExpressionFormMeta.Category` (as shown in § 4). Each arm delegates to a per-category helper. This keeps `Resolve()` readable as a catalog-structure document and makes the per-catalog-API calls isolated in testable helpers.

This is implementation guidance, not a design change. Frank should weigh in on whether this structure is enforced or left to implementation discretion.

---

## Summary

The design is sound at ~70% catalog-driven. The three genuine implementation smells that will produce per-member dispatch without intervention:

1. **ActionSyntaxShape → TypedAction DU mapping** doesn't fit the 3-shape partition cleanly (`RemoveAtIndex` is structurally ambiguous). Needs `TypedActionShape` on `ActionMeta`.
2. **Literal range validation** has no backing catalog metadata — `TypeMeta.LiteralRange` doesn't exist yet.
3. **ContentValidation DU** is designed but not in TypeMeta source — will create per-TypeKind dispatch in Slice 4 unless landed first.

The modifier bounds-pair issue (Gap C) is lower-priority but real. The parser pattern recommendations (static derived sets, `ByFieldToken` usage) are implementation hygiene, not blockers.

Frank should decide whether Proposals 1-3 are blocking the slice plan or deferred debt. My read: Proposals 2 and 3 are genuine Slice 4 blockers because they directly cause hardcoded per-TypeKind switches in Slice 4 itself. Proposal 1 is a Slice 5 blocker for the same reason.
