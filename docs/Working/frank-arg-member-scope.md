# Architectural Decision — TextMate Scope for Event-Arg Member References

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-09T11:17:00-04:00
**Area:** TextMate grammar generation, catalog-driven scope assignment

---

## Problem

The grammar generator emits `variable.other.property.precept` for both:
1. **Collection member access** — `Queue.count`, `Stack.peek` (capture group 3 in `collectionMemberAccess`)
2. **Event-arg member access** — `LoadParcel.Recipient` (capture group 3 in `eventArgReference`)

These are semantically distinct. Event-arg members belong to the **parameter axis** (soft cyan, `#9AD8E8`), not the **field/property axis** (indigo, `#A5B4FC`). The grammar must express this distinction with a dedicated scope so the theme can target it with a simple, non-compound selector.

## Decision

**Chosen scope:** `variable.parameter.property.precept`

### Rationale

| Option | Verdict | Reason |
|--------|---------|--------|
| `variable.other.property.event-arg-ref.precept` | Rejected | Stays in `variable.other.*` — semantically wrong axis. Event args are parameters, not "other" variables. |
| `variable.parameter.property.precept` | **Chosen** | Sits under `variable.parameter.*`, aligning with `variable.parameter.precept` (the arg name scope). The `.property` segment distinguishes member access from the arg name itself. Follows TextMate convention: general → specific → language suffix. |
| `variable.other.arg-member.precept` | Rejected | Invents a non-standard category. `arg-member` has no TextMate precedent. |
| Compound selector (theme workaround) | Rejected | Theme-layer hack. Semantic distinctions belong in grammar scopes, not compound selectors. |

**Why `variable.parameter.*`?**

- `variable.parameter.precept` already scopes **arg names** (the event-arg declaration: `Recipient as text`). The arg member reference (`LoadParcel.Recipient`) is accessing a property on that same parameter — it belongs on the same semantic axis.
- TextMate convention: `variable.parameter` = function/event parameters. Adding `.property` narrows it to member access on a parameter. This is exactly the right semantic slot.
- Default theme behavior: most VS Code themes already style `variable.parameter` distinctly from `variable.other`, so even without a custom theme rule, the token gets reasonable default coloring.

## Implementation Spec

### 1. Grammar Generator Change

**File:** `tools/Precept.GrammarGen/Program.cs`
**Location:** Lines 767–784, the `eventArgReference` repository entry.

**Change capture group 3 from:**
```csharp
["3"] = new JsonObject { ["name"] = "variable.other.property.precept" }
```

**To:**
```csharp
["3"] = new JsonObject { ["name"] = "variable.parameter.property.precept" }
```

This is the **only** site. The scope is emitted by the grammar generator's structural pattern for `eventArgReference`, not derived from a `TokenMeta.TextMateScope` catalog entry. No catalog enum change is required — this is a structural pattern scope (like `meta.event-arg-ref.precept`, `punctuation.accessor.precept`, `entity.name.function.event.precept`) that lives in the generator's hardcoded structural section.

### 2. Regenerate the Grammar

After modifying the generator, regenerate:
```bash
dotnet run --project tools/Precept.GrammarGen -- --output tools/Precept.VsCode/syntaxes/precept.tmLanguage.json
```

Verify the output: line 910 of the generated `precept.tmLanguage.json` should read `"name": "variable.parameter.property.precept"` inside the `eventArgReference` pattern.

### 3. Theme Rule

**File:** `tools/Precept.VsCode/package.json` (theme contribution)

Kramer adds:
```json
{
  "scope": "variable.parameter.property.precept",
  "settings": { "foreground": "#9AD8E8" }
}
```

This is a simple, non-compound selector. It targets event-arg member references directly.

### 4. Revert the Compound Selector Workaround

**Yes — revert Kramer-7's compound selector entirely.** Once `variable.parameter.property.precept` is live:

- The compound rule `meta.event-arg-ref.precept variable.other.property.precept → #9AD8E8` becomes dead code — no token will ever match it because the scope it targets (`variable.other.property.precept` inside `meta.event-arg-ref.precept`) will no longer be emitted.
- Dead theme rules are maintenance hazards. Remove it in the same commit that adds the proper scope rule.

### 5. No Catalog Change Required

The `eventArgReference` pattern is a **structural pattern** in the grammar generator — it matches a syntactic shape (`Identifier.Identifier`) and assigns scopes based on position, not token kind. The `Tokens` catalog's `TextMateScope` property drives keyword/operator scope groups, not structural dot-access patterns. No `TokenMeta` entry needs modification.

## Verification

After implementation, inspect `LoadParcel.Recipient` in VS Code:
- `LoadParcel` → scope `entity.name.function.event.precept` (event color)
- `.` → scope `punctuation.accessor.precept`
- `Recipient` → scope `variable.parameter.property.precept` (arg color `#9AD8E8`)

The `meta.event-arg-ref.precept` wrapper scope remains — it's structurally useful for other consumers (e.g., semantic tokens, potential future folding). Only the leaf scope on capture group 3 changes.

## Summary

Semantic distinctions belong in grammar scopes, not theme hacks. `variable.parameter.property.precept` is the correct scope because event-arg member references are on the parameter axis, not the field axis. One line changes in the generator, one theme rule replaces the compound selector.
