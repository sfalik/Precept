# Type-requirement family — cells

**Status**: Draft — 2026-07-23.

This folder holds the cells for the **type-requirement** obligation family, which the owner separated from the fault family on 2026-07-21.

## Why this family exists

Of the thirteen `ProofRequirementKind` members, most discharge a `FaultCode` — they prevent a runtime fault, and the philosophy's fault list is specific: no division by zero, no overflow, no empty-collection access, no result outside a declared bound.

Two do not. `Dimension` requires that a period operand carries a required time dimension. `Modifier` requires that a field declares a required modifier, such as `ordered` before an ordinal comparison. Both prevent a *type or qualifier* mismatch, which is type well-formedness rather than a runtime fault. Calling them fault obligations would stretch "fault" past both the philosophy list and the fault-code registry, and blur the guarantee the word names.

They are **not dropped**. They are genuine prove-or-reject obligations that get populated under their own family — which is this one. The owner ruling records the name as provisional.

## What is here

| File | Kind | Cells | State |
|---|---|---|---|
| `Dimension.cells.json` | `ProofRequirementKind.Dimension` | 7 | authored during the fault-family pass; re-homed here 2026-07-23 |
| `Modifier.cells.json` | `ProofRequirementKind.Modifier` | 5 | authored during the fault-family pass; re-homed here 2026-07-23 |

Both files were written while the fault-family pass was running, before the family split was ruled. They carry their content but not yet the cell schema's shape, and neither has been reviewed as a type-requirement cell. Re-homing them is a move, not a re-authoring: what they say is preserved, where they count is corrected.

## Known work owed

- **Schema conformance.** `Modifier.cells.json` uses its own key vocabulary (`cellId`, `occasion`, `dischargeContractInstance`, `witness`) rather than the cell schema's, and its family respellability verdict is free text where the matrix makes the verdict binary. `Dimension.cells.json` conforms in shape but seven of its cells carry no `citations`.
- **Discharge contracts with no validity argument.** Nine of `Dimension.cells.json`'s contract entries name no validity argument. Under the matrix's rule-validity gate no cell ratifies while citing an argument-less rule, so this family owes its arguments before it can ratify — the same gate the fault family was held to.
- **A denominator.** The fault family has a generated coordinate map; this family does not yet. It needs one before any completeness claim is made about it.
- **Its own slice.** The owner ruling anticipates this family being populated as its own slice rather than riding along with the fault pass.

## Reading order

1. `docs/Working/obligation-discharge-matrix-2026-07-19.md` § Vocabulary, *Obligation family* — where this family is defined and why the split was made.
2. The 2026-07-21 family-scope ruling in the same document — the four-leg rationale for separating these two kinds.
3. The two cell files here.
