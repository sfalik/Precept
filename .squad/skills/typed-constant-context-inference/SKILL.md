---
name: "typed-constant-context-inference"
description: "Infer typed-constant completion types from local expression context when declaration scope is unavailable"
domain: "language-server"
confidence: "high"
source: "earned — fixing empty typed-constant completions in rule comparisons in the Precept language server"
---

## Context

Apply this when completion is triggered by `'` inside an expression site that has no enclosing field or arg-default owner, such as `rule ApprovedAmount > `, `ensure Submit.Amount >= `, or a typed function/accessor argument.

## Patterns

- Do not assume every typed-constant completion inside `InExpression` belongs to an enclosing field declaration.
- Check active call context first; if the current parameter type is unambiguous across overloads, use it as the expected type.
- If no call parameter applies, inspect the neighboring significant tokens around the cursor.
- When the previous significant token is a binary operator, infer the missing operand's type from the peer operand.
- Resolve peer operand types from semantic symbols and catalog metadata (fields, event args, fixed-return accessors, and unambiguous function returns) instead of LS-local hardcoded literal lists.
- Only fall back to declaration-scope inference after local expression inference fails.

## Examples

- `rule ApprovedAmount > ` + trigger `'` → infer `money` from `ApprovedAmount`
- `on Submit ensure Submit.Amount >= ` + trigger `'` → infer `money` from `Submit.Amount`
- `round(, 2)` + trigger `'` in a typed overload slot → use the active parameter type when overloads agree

## Anti-Patterns

- Treating `InExpression` as if it always maps to an enclosing field
- Returning an empty typed-constant completion list when a peer operand or active parameter already determines the type
- Building typed-constant suggestions from hardcoded per-type lists instead of reusing catalog metadata and in-document values
