# Consistency Review: field-state-guarantees-v2.md

> **Reviewer:** Frank (Lead/Architect)  
> **Date:** 2026-05-12T19:38-04:00  
> **Subject:** Cross-check of v2 design doc against canonical language spec, grammar, evaluator, and runtime API

---

## ✅ Confirmed Consistent

| Item | Spec Section Confirming |
|------|------------------------|
| D133 (WriteToTargetOmittedField) — `set` targeting omit in target state = compile error | §2.2 composition rule #6: "`set` targeting an `omit` field in the target state is a compile error" |
| Parser fix for comma-separated field names — grammar supports `Field { "," Field }*` | §2.2 AccessMode grammar: `in StateTarget ("when" BoolExpr)? modify Field { "," Field }* readonly` |
| Omit cannot be guarded | §2.2 rule 4a + parser diagnostic `OmitDoesNotSupportGuard` (§2.7) |
| `writable` modifier sets editable baseline; absence = read-only (D3 default) | §2.2 composition rules #1, #2 |
| `IsBroadcast` for `all` keyword — grammar shows `FieldTarget := identifier ("," identifier)* \| all` | §2.2 FieldTarget grammar |
| Guarded access mode syntax: `in <State> when <Guard> modify <Field> readonly\|editable` | §2.2 AccessMode grammar, line 925 |
| Guard is a `BoolExpr` (full expression language — can include OR, string equality, etc.) | §2.2 grammar: `("when" BoolExpr)?` |
| Wildcard `any` desugars to all states | §2.2: "comma-delimited `StateTarget` is pure syntactic sugar: the compiler expands it into one independent state-scoped declaration per named state" |
| Redundancy rules for access modes (D43) | §2.2 rules #4b, #4c; §3.8 access mode validation table |
| Conflict rules for access modes (D42) | §2.2 rule #7; §3.8 `ConflictingAccessModes` |
| Self-transition triggers omit-on-entry clearing | §2.2 rule #5: "field value resets to default on any transition into an `omit` state (including self-transitions)" |
| `no transition` does NOT trigger omit clearing | §2.2 rule #5: "does NOT apply to `no transition`" |
| ProofEngine handles proof obligations with structured attribution | §0.6 Proof Engine Design Contract |
| Pipeline architecture (TypeChecker before ProofEngine) | §Status + §0.5/§0.6 contracts |

---

## ⚠️ Inconsistencies / Gaps Found

### B1: D132 (`WriteToReadOnlyField`) directly contradicts spec composition rule #6

- **What the v2 doc says:** D132 is a compile error when a `set` action in a transition or state hook targets a field that is `readonly` in the from-state.
- **What the canonical doc actually says:** §2.2 composition rule #6 (line 935): **"`set` targeting an `omit` field in the target state is a compile error; `readonly`/`editable` do not restrict `set`."** This is an explicit, unambiguous blanket statement: the `readonly`/`editable` access mode system does NOT govern `set` actions. It governs only the Update API (external field patches).
- **Corroborated by:** The evaluator doc §7.1 `Update` path (line 559–597) performs access mode checks. The `Fire` path (line 486–551) has NO access mode check. The runtime API doc outcome type #8 ("Access mode failure") explicitly says "Patch targets a field not editable" — patches, not `set` actions.
- **Severity:** **BLOCKER**
- **Recommended fix:** Remove D132 entirely. `readonly`/`editable` do not restrict event-driven `set` actions — they restrict only the Update (direct patch) surface. This is a fundamental architectural distinction the spec makes deliberately.

### B2: D134 (`UnprovedAccessCondition`) is equally invalid — same root cause as B1

- **What the v2 doc says:** D134 fires when a transition guard doesn't imply the conditional editability guard for a field written by `set`. The entire Phase 2 (ProofEngine conditional enforcement) exists to enforce that `set` actions respect guarded `editable` conditions.
- **What the canonical doc actually says:** Same rule #6 — `readonly`/`editable` do not restrict `set`. If `readonly` doesn't restrict `set`, then conditional `editable` (which is just a guarded upgrade from readonly to editable) also doesn't restrict `set`. The access condition is irrelevant to event-driven writes.
- **Severity:** **BLOCKER**
- **Recommended fix:** Remove D134 and the entire Phase 2 (conditional enforcement via ProofEngine). The `modify when {condition}` guard governs Update (patch) access — not `set` in transitions. The ProofEngine extension (DNF conversion, `AccessConditionProofRequirement`, `TryAccessConditionProof`) is solving a non-problem per the spec.

### B3: D130 (`WriteToOmittedField`) for from-state is not grounded in the spec and is harmful

- **What the v2 doc says:** D130 fires when a transition action writes to a field that is `omit` in the from-state.
- **What the canonical doc actually says:** The spec only explicitly prohibits `set` targeting omit in the **target** state (rule #6). It says nothing about `set` targeting a field omit in the from-state. Furthermore, writing to a field that's omit in the from-state but visible in the target state is a valid and useful pattern: you're populating the field as you transition to a state where it exists.
- **Example of valid code that D130 would incorrectly reject:**
  ```precept
  in Draft omit ApprovedAmount
  from Draft on Approve -> set ApprovedAmount = Approve.Amount -> transition Review
  ```
  Here ApprovedAmount is omit in Draft but exists in Review. The `set` populates it for the target state. This is correct, useful, and the spec doesn't prohibit it.
- **Severity:** **BLOCKER** — D130 would reject valid programs.
- **Recommended fix:** Remove D130 for the general from-state case. The only valid omit-related `set` restriction is D133 (target-state omit). If a "write to invisible field in `no transition` context" warning is desired, it requires separate justification and should be a warning, not an error.

### B4: D131 (`ReadOfOmittedField`) for guards contradicts spec scope rules

- **What the v2 doc says:** D131 fires when a guard expression reads a field that is omit in the from-state.
- **What the canonical doc actually says:** §3.5 Expression Scope, transition row guard context: **"All field names + current event's args (via `EventName.ArgName`)"** — no restriction on omitted fields. The field slot still holds its default value (cleared on entry per rule #5). The guard reads the default. Whether this is *useful* is a separate question — it's not an error per the spec. Dead guards are already caught by the ProofEngine.
- **Severity:** **CONCERN** — This isn't as definitively wrong as B1/B2 (you could argue "structurally absent" means inaccessible everywhere), but the spec's scope rules explicitly include "All field names" without qualification.
- **Recommended fix:** Either (a) remove D131 for guards entirely — the ProofEngine's dead/tautological guard detection already catches the consequence (guard always evaluates the same way), or (b) add this as a spec extension with explicit justification, not as if it's already the spec's intent.

---

## 📋 Spec Gaps (things the design covers that the spec doesn't address)

1. **D131 for action RHS reads.** The spec puts "All field names" in scope for action expressions. Reading an omit field in `set Y = OmittedField + 1` gives the default value — the spec doesn't say this is an error. However, the design decision to flag this is defensible as a "structurally absent means inaccessible" extension. Should be documented as a spec EXTENSION, not as existing spec enforcement.

2. **Wildcard row × omit interaction.** The spec says wildcards desugar to per-state declarations. The v2 design's Q4 decision (single diagnostic listing all affected states) is a tooling UX choice that the spec is silent on. Reasonable but undocumented.

3. **State hook access enforcement direction.** The v2 doc says on-entry hooks use target state access modes and on-exit hooks use source state access modes. The spec at §2.2 only describes state hooks with `(to|from) StateTarget ("when" BoolExpr)? ("->" ActionStatement)*` — it doesn't explicitly state which state's access modes govern hook actions. The v2 design's interpretation is reasonable but ungrounded.

4. **`set` to omit in from-state with `no transition`.** There's a valid argument that writing to a field that's omit in the from-state and staying in that state (via `no transition`) is dead code — the value remains invisible. The spec doesn't prohibit it, but a warning could be justified as a "dead code" diagnostic. This is distinct from the error-severity D130 the v2 proposes.

---

## Verdict

**BLOCKED**

The v2 design has a fundamental architectural misconception at its core: **it conflates the Update (external patch) enforcement surface with the Fire (event-driven `set`) enforcement surface.** The spec draws a deliberate, explicit line between these two worlds at composition rule #6:

> `readonly`/`editable` do not restrict `set`.

This single sentence invalidates:
- **D132** (WriteToReadOnlyField) — entirely
- **D134** (UnprovedAccessCondition) — entirely  
- **Phase 2** (conditional enforcement via ProofEngine) — entirely
- **D130** (WriteToOmittedField in from-state) — for the general case

What DOES survive:
- **D133** (WriteToTargetOmittedField) — explicitly grounded in spec rule #6
- **Parser fix** (multi-field FieldTargetSlot) — correct, needed
- **Omit unification into AccessModes** — architecturally sound
- **BuildFieldAccessLookup** — useful for D133 and Update enforcement
- **D42/D43 emission** — correct, already defined in spec

Shane should NOT sign off on this design as-is. The fix is to **scope the design exclusively to what the spec actually prohibits for `set` actions**: omit-in-target-state. The `readonly`/`editable` enforcement belongs to the Update path (which the evaluator already handles at runtime) — not to transition/hook actions. If the intent is to extend the spec to also restrict `set` by readonly/editable, that's a language design decision that must be made deliberately with full rationale, not smuggled in through an implementation design doc.
