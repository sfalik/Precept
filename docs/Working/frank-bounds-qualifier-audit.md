# Frank audit: bounds qualifier comparability

## Verdict

The concern is confirmed and worse than suspected. `samples/Test.precept` line 3 (`field test as quantity max '5 kg'`) reveals two issues:
1. `quantity` without `in`/`of` has no comparison basis, so `max` is semantically undefined.
2. Even with a qualifier, typed constants like `'5 kg'` are not extracted into comparable bound values, so the interval-containment proof does not fire.

Result: the bound is accepted but ignored.

## Evidence

- `samples/Test.precept:3` — `field test as quantity max '5 kg'` compiles with no diagnostics and no `in`/`of`.
- `src/Precept/Pipeline/TypeChecker.Validation.Modifiers.cs:211-226` — `TryGetComparableModifierValue` handles only `NumberLiteral` and unary-minus number literals; typed constants return `null`.
- `src/Precept/Pipeline/TypeChecker.cs:378-383` — `DeclaredMin`/`DeclaredMax` are set from `TryGetComparableModifierValue`, so they are `null` for typed-constant bounds.
- `src/Precept/Pipeline/ProofEngine.Intervals.cs:110-111` — `GetFieldBounds` reads `DeclaredMin`/`DeclaredMax`; when `null`, interval is unbounded and no containment obligation is emitted.
- Catalog coverage includes bounded qualified types:
  - `src/Precept/Language/Types.cs` and `src/Precept/Language/Modifiers.cs` show `min`/`max` on `money`, `quantity`, `price`.

## Proposed rule set

1. **Bounds require qualifier for qualified types**
   - For `money`, `quantity`, `price`: declaring `min`/`max` requires matching qualifier context (`in` for money/price; `in` or `of` for quantity).
2. **Typed-constant bound extraction**
   - Extend bound extraction so typed constants (e.g., `'5 kg'`, `'100 USD'`) populate `DeclaredMin`/`DeclaredMax`.
3. **Bounds qualifier consistency**
   - Enforce compatibility between field qualifier and bound qualifier (reject mismatch or normalize with explicit rules).

## Suggested diagnostics

| Name | Stage | Trigger |
|---|---|---|
| `BoundsRequireQualifier` | Type checker | `min`/`max` on `money`/`quantity`/`price` without required `in`/`of` qualifier |
| `BoundsQualifierMismatch` | Type checker | Bound qualifier conflicts with field qualifier |
| `UnextractableBound` (transitional) | Type checker | Bound expression cannot be converted into a comparable value |

## Test.precept line 3 outcome

Under the proposed sound rule set, line 3 should be rejected. A legal form would be:

`field test as quantity in 'kg' max '5 kg'`
