# Diagnostic Message UX Review: UnprovedQualifierCompatibility

**Author:** Elaine (UX Designer)
**Date:** 2026-05-11
**Diagnostic Code:** C114 — `UnprovedQualifierCompatibility`
**Stage:** Proof

---

## The Message Under Review

```
Operands '<unknown>' and '<unknown>' have incompatible Unit qualifiers in ensure in ReceiveShipment
```

Produced by template: `"Operands '{0}' and '{1}' have incompatible {2} qualifiers{3}"`

---

## Problem 1: `<unknown>` × 2 = Zero Signal

**Both** operand slots render as `<unknown>`. The user sees the same placeholder twice with no way to distinguish which operand is which. The message names a problem but gives the user nothing to act on — they don't know *which* operands are incompatible.

**Root cause:** `GetFieldName()` in ProofEngine.cs (line 315) only resolves `TypedFieldRef`, `TypedArgRef`, and `TypedMemberAccess { Object: TypedFieldRef }`. Any other expression shape (literal, nested function call, complex expression) returns `null`, which falls through to `?? "<unknown>"`.

**Human UX impact:** The user stares at the message and has to manually search the `ensure` clause for anything that carries a unit qualifier. In a complex expression with multiple qualified operands, this is a guessing game.

**AI agent UX impact:** An AI agent receives `<unknown>` as a structured `Args[0]` and `Args[1]`. It cannot programmatically identify which fields or expressions to fix. The diagnostic is effectively a boolean "something is wrong" with no structured resolution path.

### Recommendation

1. **Never emit `<unknown>` as a rendered diagnostic argument.** If the proof engine cannot resolve a named subject, fall back to a source-text excerpt from the expression span — the raw source code is always more informative than a placeholder.

2. **Revised message when names are available:**
   ```
   Incompatible Unit qualifiers: 'Weight' (kg) vs 'Distance' (m) [ReceiveShipment.ensure]
   ```

3. **Revised message when names are NOT available (fallback to source text):**
   ```
   Incompatible Unit qualifiers in 'item.Weight + route.Distance' [ReceiveShipment.ensure]
   ```

4. **Worst-case fallback (source text also unavailable):**
   ```
   Incompatible Unit qualifiers [ReceiveShipment.ensure]
   ```
   Drops the operand clause entirely — no apology, just the fact and the location.

---

## Problem 2: No Actual Qualifier Values Shown

The message says the qualifiers are "incompatible" but never says *what* they are. The user knows something is wrong but not what the conflict is. The `QualifierCompatibilityProofRequirement` has axis information but the actual qualifier values (e.g., `kg` vs `m`) are not surfaced.

**Human UX impact:** The user has to mentally look up both sides' declared qualifiers to understand the conflict. For a simple two-field expression this is manageable; for a chain or computed expression it's not.

**AI agent UX impact:** Without the actual values, an AI agent cannot propose a specific fix. It can say "check your qualifiers" but cannot say "change `m` to `kg`."

### Recommendation

If the proof engine can resolve the qualifier values, include them:

```
Incompatible Unit qualifiers: 'Weight' (kg) vs 'Distance' (m) [ReceiveShipment.ensure]
```

The values (`kg`, `m`) appear inline next to the operand names — no separate clause needed.

---

## Problem 3: The Context Suffix Is Structurally Weak

The suffix `in ensure in ReceiveShipment` is a flat string built by `$" in {contextDesc}"`. It reads naturally for one level of nesting but is not machine-parseable. An AI agent cannot reliably extract the construct kind (`ensure`) and the event name (`ReceiveShipment`) from a sentence fragment.

### Recommendation

For human readability, use bracketed dot-path context instead of prepositional chains:

```
Incompatible Unit qualifiers: 'Weight' (kg) vs 'Distance' (m) [ReceiveShipment.ensure]
```

For AI/machine consumption, the `Args` array should carry structured values:
- `Args[0]`: left operand name or source excerpt
- `Args[1]`: right operand name or source excerpt
- `Args[2]`: qualifier axis name
- `Args[3]`: left qualifier value (or empty)
- `Args[4]`: right qualifier value (or empty)
- `Args[5]`: context construct kind
- `Args[6]`: context event/state name

This lets AI agents do structured extraction without regex parsing of natural-language fragments.

---

## Problem 4: No Fix Direction

The message says what's wrong but not what to do. The `FixHint` in the `DiagnosticMeta` says "Ensure both operands have matching qualifier values" — but this doesn't appear in the rendered diagnostic message, only in the metadata.

### Recommendation

The `FixHint` metadata ("Ensure both operands have matching qualifier values") should remain available to tooling via hover or code actions — but does not need to be appended to the diagnostic line itself. The message should state the problem; the fix hint is a separate affordance.

---

## Summary: Revised Message Spectrum

| Scenario | Proposed |
|----------|----------|
| Names + values resolved | `Incompatible Unit qualifiers: 'Weight' (kg) vs 'Distance' (m) [ReceiveShipment.ensure]` |
| Names resolved, no values | `Incompatible Unit qualifiers: 'Weight' vs 'Distance' [ReceiveShipment.ensure]` |
| Names unresolved, source available | `Incompatible Unit qualifiers in 'item.Weight + route.Distance' [ReceiveShipment.ensure]` |
| Worst case | `Incompatible Unit qualifiers [ReceiveShipment.ensure]` |

**Revised template:** `"Incompatible {2} qualifiers: '{0}' vs '{1}' [{3}]"`

Where `{3}` uses dot-path context (`EventName.construct`) instead of the old `in construct in EventName` suffix. This eliminates the double-"in" structurally — the context is a bracketed location tag, not a prepositional phrase chain.

Worst-case fallback (both operands unresolvable) drops the operand clause entirely: `"Incompatible {2} qualifiers [{3}]"`. No apology, no "cannot identify" — just the fact and the location.

---

## Implementation Notes

The fix has two layers:

1. **Message template change** (cosmetic, low risk): Update the template in `Diagnostics.cs` line 748 and the fallback string in `ProofEngine.cs` lines 1203–1204. This is a string-only change.

2. **`GetFieldName` enrichment** (behavioral, medium risk): Extend the pattern match at line 317 to handle more expression shapes, or add a `GetSourceExcerpt()` fallback that extracts the raw source text from the expression span. This requires access to the source text at the proof stage, which may need threading through the obligation context.

Layer 1 should ship immediately. Layer 2 is the higher-value fix but needs George's assessment of what the proof engine can resolve.

---

## Verdict

This diagnostic fails both audiences. A human developer gets a message with two identical placeholders and no actionable detail. An AI agent gets unstructured `<unknown>` tokens that block programmatic reasoning. The fix is straightforward: never render `<unknown>`, always show actual qualifier values when available, and append fix direction to the message.
